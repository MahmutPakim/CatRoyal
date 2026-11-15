using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Etkiler
{
    /// <summary>Kombinasyonlar: iki özel taşın birbiriyle kaydırılması (GDD 4).</summary>
    public sealed partial class EtkiMotoru
    {
        /// <summary>
        /// İki özel taş yer değiştirdikten sonra çağrılır. Sonucu yalnızca iki taşın türü belirler (GDD 4.1.1);
        /// konumla ilgili etkiler gidilen hücreyi merkez alır (4.1.2).
        /// </summary>
        public void KombinasyonBaslat(Konum cikilan, Konum gidilen)
        {
            var a = _t[gidilen].Tas;
            var b = _t[cikilan].Tas;
            if (a == null || b == null || !a.OzelMi || !b.OzelMi)
            {
                throw new InvalidOperationException("Kombinasyon için iki hücrede de özel taş olmalı.");
            }

            Planla(_simdi, () => Kombinasyon(cikilan, gidilen, a, b));
        }

        private static KombinasyonTuru TurBelirle(Tas a, Tas b)
        {
            bool Var(TasTuru tur) => a.Tur == tur || b.Tur == tur;
            bool Ikisi(TasTuru tur) => a.Tur == tur && b.Tur == tur;

            if (Ikisi(TasTuru.RenkKuresi)) return KombinasyonTuru.KureKure;
            if (Var(TasTuru.RenkKuresi))
            {
                return Var(TasTuru.Cizgi) ? KombinasyonTuru.KureCizgi
                    : Var(TasTuru.Bomba) ? KombinasyonTuru.KureBomba
                    : KombinasyonTuru.KureGudumlu;
            }

            if (Ikisi(TasTuru.Gudumlu)) return KombinasyonTuru.GudumluGudumlu;
            if (Var(TasTuru.Gudumlu))
            {
                return Var(TasTuru.Cizgi) ? KombinasyonTuru.GudumluCizgi : KombinasyonTuru.GudumluBomba;
            }

            if (Ikisi(TasTuru.Bomba)) return KombinasyonTuru.BombaBomba;
            return Var(TasTuru.Bomba) ? KombinasyonTuru.BombaCizgi : KombinasyonTuru.CizgiCizgi;
        }

        private void Kombinasyon(Konum cikilan, Konum gidilen, Tas a, Tas b)
        {
            var tur = TurBelirle(a, b);

            // GDD 3.1.5: iki taş tek tek etkinleşmez; ikisi de kombinasyona harcanır.
            Kaldir(gidilen, a, Tetik.Kombinasyon);
            Kaldir(cikilan, b, Tetik.Kombinasyon);

            switch (tur)
            {
                case KombinasyonTuru.CizgiCizgi:
                {
                    // GDD 4.3.1: iki çizgi aynı yönde olsa bile sonuç artıdır.
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    var vurulan = new HashSet<Konum>();
                    CizgiEtkisi(gidilen, Yon.Yatay, merkeziVur: true, birKez: vurulan);
                    CizgiEtkisi(gidilen, Yon.Dikey, merkeziVur: true, birKez: vurulan);
                    break;
                }

                case KombinasyonTuru.BombaCizgi:
                {
                    // GDD 4.3.2: merkez sütun ve iki yanı dikey, merkez satır ve altı-üstü yatay çizgi etkisi.
                    // Yorum: kombinasyon tek etkidir; satır ve sütunların kesiştiği hücreler bir kez vurulur (3.1.6).
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    var vurulan = new HashSet<Konum>();
                    for (int d = -1; d <= 1; d++)
                    {
                        CizgiEtkisi(gidilen.Otele(0, d), Yon.Dikey, merkeziVur: true, birKez: vurulan);
                        CizgiEtkisi(gidilen.Otele(d, 0), Yon.Yatay, merkeziVur: true, birKez: vurulan);
                    }

                    break;
                }

                case KombinasyonTuru.BombaBomba:
                {
                    // GDD 4.3.3: bombalardan biri devre dışı kalır, diğeri yarıçap 4 ile patlar. Merkez: AK-12.
                    var merkez = _b.Bayraklar[Ak.BombaBombaMerkezi] == Varyant.A ? gidilen : cikilan;
                    _olaylar.Add(new KombinasyonCalisti(tur, merkez, _simdi));
                    BombaEtkisi(merkez, 4, VurusTuru.Bomba, merkeziVur: true);
                    break;
                }

                case KombinasyonTuru.GudumluCizgi:
                {
                    // GDD 4.3.4: 1 güdümlü taş çizgi taşını en iyi satıra (yatay) ya da sütuna (dikey) taşır.
                    var yon = (a.Tur == TasTuru.Cizgi ? a : b).CizgiYonu!.Value;
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KalkisOncesiArti(gidilen, () => GudumluKaldir(gidilen, GudumluYuku.Cizgi, yon));
                    break;
                }

                case KombinasyonTuru.GudumluBomba:
                    // GDD 4.3.5: 1 güdümlü taş bombayı en iyi bölgeye taşır; orada varsayılan 5×5.
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KalkisOncesiArti(gidilen, () => GudumluKaldir(gidilen, GudumluYuku.Bomba, null));
                    break;

                case KombinasyonTuru.GudumluGudumlu:
                    // GDD 4.3.6: 3 güdümlü taş, her biri ayrı tek hedefe; uçanlar kendi artı alanlarını patlatmaz.
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KalkisOncesiArti(gidilen, () =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            GudumluKaldir(gidilen, GudumluYuku.Yok, null);
                        }
                    });
                    break;

                case KombinasyonTuru.KureCizgi:
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KureFaziBaslat(new KureIslemi(KureModu.Cizgi, null, gidilen, DonusumRenkSayisi()));
                    break;

                case KombinasyonTuru.KureBomba:
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KureFaziBaslat(new KureIslemi(KureModu.Bomba, null, gidilen, DonusumRenkSayisi()));
                    break;

                case KombinasyonTuru.KureGudumlu:
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    KureFaziBaslat(new KureIslemi(KureModu.Gudumlu, null, gidilen, DonusumRenkSayisi()));
                    break;

                default:
                    // GDD 4.3.10, 4.4.2: bütün tahta; çift renk modunda iki tur.
                    _olaylar.Add(new KombinasyonCalisti(tur, gidilen, _simdi));
                    TahtayiTemizle(gidilen, _b.CiftRenkModu ? 2 : 1);
                    break;
            }
        }

        /// <summary>
        /// GDD 4.3.6 (düşük): kalkıştan önce gidilen hücrede tek bir artı alanı patlar. Yorum: aynı kural
        /// güdümlü + çizgi ve güdümlü + bomba kombinasyonlarına da uygulanır; GDD o ikisi için bir şey yazmıyor.
        /// </summary>
        private void KalkisOncesiArti(Konum merkez, Action kalkis)
        {
            Vur(merkez, VurusTuru.Gudumlu);
            Planla(_simdi + 1, () =>
            {
                ArtiAlaniVur(merkez);
                kalkis();
            });
        }

        /// <summary>
        /// GDD 4.3.10: hücreler merkezden uzaklıklarına göre halkalara ayrılır ve içten dışa patlatılır.
        /// Halka ölçüsü bombadaki gibidir: satır ya da sütun farkının büyüğü. Çift renk modunda ilk tur
        /// bitince ikinci tur çalışır (4.4.2).
        /// </summary>
        private void TahtayiTemizle(Konum merkez, int turSayisi)
        {
            int enUzak = Math.Max(
                Math.Max(merkez.Satir - 1, _t.SatirSayisi - merkez.Satir),
                Math.Max(merkez.Sutun - 1, _t.SutunSayisi - merkez.Sutun));

            for (int tur = 0; tur < turSayisi; tur++)
            {
                long taban = _simdi + (tur * (enUzak + 1));
                for (int d = 0; d <= enUzak; d++)
                {
                    int halka = d;
                    Planla(taban + d, () =>
                    {
                        if (halka == 0)
                        {
                            Vur(merkez, VurusTuru.TahtaTemizleme);
                            return;
                        }

                        foreach (var konum in Halka(merkez, halka))
                        {
                            Vur(konum, VurusTuru.TahtaTemizleme);
                        }
                    });
                }
            }
        }
    }
}
