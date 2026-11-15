using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Etkiler;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Veri
{
    /// <summary>Bölüm verisinden oynanabilir bir bölüm oturumu kurar (GDD 8.10, 5.4).</summary>
    public static class BolumKurucu
    {
        /// <summary>Çözülmüş hücre içerik kodu.</summary>
        internal readonly struct Icerik
        {
            public Icerik(bool oynanmayan, bool bos, bool rastgeleRenk, int? renk, TasTuru? ozel, Yon? yon, EngelTanimi? engel, int katman)
            {
                Oynanmayan = oynanmayan;
                Bos = bos;
                RastgeleRenk = rastgeleRenk;
                Renk = renk;
                Ozel = ozel;
                Yon = yon;
                Engel = engel;
                Katman = katman;
            }

            public bool Oynanmayan { get; }
            public bool Bos { get; }
            public bool RastgeleRenk { get; }
            public int? Renk { get; }
            public TasTuru? Ozel { get; }
            public Yon? Yon { get; }
            public EngelTanimi? Engel { get; }
            public int Katman { get; }

            public Tas? TasUret(Tahta tahta) =>
                Renk.HasValue ? tahta.NormalTasUret(Renk.Value)
                : Ozel.HasValue ? tahta.OzelTasUret(Ozel.Value, Yon)
                : Engel != null ? tahta.EngelUret(Engel, Katman)
                : null;
        }

        /// <summary>
        /// Hücre içerik kodları (GDD 8.10 "Hücre içerikleri"): X oynanmayan, _ boş başlayan, ? rastgele renk,
        /// rN sabit renk (göreli renk kimliği), - | B G R özel taşlar, ad:katman engel (ad, "engeller" sözlüğünden).
        /// </summary>
        internal static Icerik IcerikCoz(BolumVerisi veri, string kod, string yer)
        {
            switch (kod)
            {
                case "X": return new Icerik(true, false, false, null, null, null, null, 0);
                case "_": return new Icerik(false, true, false, null, null, null, null, 0);
                case "?": return new Icerik(false, false, true, null, null, null, null, 0);
                case "-": return new Icerik(false, false, false, null, TasTuru.Cizgi, Yon.Yatay, null, 0);
                case "|": return new Icerik(false, false, false, null, TasTuru.Cizgi, Yon.Dikey, null, 0);
                case "B": return new Icerik(false, false, false, null, TasTuru.Bomba, null, null, 0);
                case "G": return new Icerik(false, false, false, null, TasTuru.Gudumlu, null, null, 0);
                case "R": return new Icerik(false, false, false, null, TasTuru.RenkKuresi, null, null, 0);
            }

            if (kod.Length > 1 && kod[0] == 'r'
                && int.TryParse(kod.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int renk))
            {
                if (renk >= veri.RenkSayisi)
                {
                    throw new BolumVerisiHatasi($"{yer}: '{kod}' rengi bölümün renk listesinde yok.");
                }

                return new Icerik(false, false, false, renk, null, null, null, 0);
            }

            var parcalar = kod.Split(':');
            if (parcalar.Length == 2
                && veri.Engeller.TryGetValue(parcalar[0], out var engel)
                && int.TryParse(parcalar[1], NumberStyles.None, CultureInfo.InvariantCulture, out int katman)
                && katman >= 1)
            {
                return new Icerik(false, false, false, null, null, null, engel, katman);
            }

            throw new BolumVerisiHatasi($"{yer}: '{kod}' içerik kodu tanınmıyor.");
        }

        /// <summary>Katman kodu: ad:katman (ad, "katmanlar" sözlüğünden).</summary>
        internal static HucreKatmani KatmanCoz(BolumVerisi veri, string kod, string yer)
        {
            var parcalar = kod.Split(':');
            if (parcalar.Length == 2
                && veri.Katmanlar.TryGetValue(parcalar[0], out var tanim)
                && int.TryParse(parcalar[1], NumberStyles.None, CultureInfo.InvariantCulture, out int katman)
                && katman >= 1)
            {
                return new HucreKatmani(tanim, katman);
            }

            throw new BolumVerisiHatasi($"{yer}: '{kod}' katman kodu tanınmıyor.");
        }

        /// <summary>
        /// Bölümü kurar: tahta, katmanlar, girişler, dolgu, başlangıç renkleri (GDD 5.4), boş başlayan hücrelerin
        /// düşmeyle dolması (2.1.5) ve ilk olası hamle denetimi (5.4.4).
        /// </summary>
        /// <param name="veri">Bölüm verisi.</param>
        /// <param name="bayraklar">Ayarlanabilir kural bayrakları.</param>
        /// <param name="parametreler">Parametre tablosu.</param>
        /// <param name="tohum">Rastgelelik tohumu; aynı tohum aynı bölümü verir.</param>
        /// <param name="olaylar">Kurulum sırasında üretilen olaylar (ilk düşmeler).</param>
        /// <param name="zorlukAgirligi">Zorluk ağırlıklı üretecin ağırlığı (GDD 9.11.5); 0 etkisizdir.</param>
        public static BolumOturumu Kur(
            BolumVerisi veri,
            AkBayraklari bayraklar,
            ParametreTablosu parametreler,
            ulong tohum,
            List<Olay> olaylar,
            double zorlukAgirligi = 0)
        {
            var tahta = new Tahta(veri.Yukseklik, veri.Genislik);
            var rastgele = new TohumluRastgele(tohum);
            var dolgu = new RastgeleDolgu();
            var baglam = new OyunBaglami(tahta, bayraklar, parametreler, rastgele)
            {
                Zorluk = new ZorlukRastgelesi(rastgele.Dallan(), zorlukAgirligi),
                Renkler = Enumerable.Range(0, veri.RenkSayisi).ToList(),
                RenkAgirliklari = veri.RenkOranlari,
                CaprazKaymaIzinli = veri.CaprazKaymaIzinli,
                Dolgu = dolgu,
            };

            var rastgeleHucreler = new List<Konum>();
            foreach (var konum in tahta.TumKonumlar())
            {
                var hucre = tahta[konum];
                var icerik = IcerikCoz(veri, veri.Hucreler[konum.Satir - 1][konum.Sutun - 1], $"hücre {konum}");
                hucre.Oynanabilir = !icerik.Oynanmayan;
                hucre.Tas = icerik.TasUret(tahta);
                if (icerik.RastgeleRenk)
                {
                    rastgeleHucreler.Add(konum);
                }

                string? alt = veri.AltKatmanlar?[konum.Satir - 1][konum.Sutun - 1];
                if (alt != null && alt != ".")
                {
                    hucre.AltKatman = KatmanCoz(veri, alt, $"hücre {konum}");
                }

                string? ust = veri.UstKatmanlar?[konum.Satir - 1][konum.Sutun - 1];
                if (ust != null && ust != ".")
                {
                    hucre.UstKatman = KatmanCoz(veri, ust, $"hücre {konum}");
                }
            }

            GirisleriIsaretle(veri, tahta);
            foreach (var konum in veri.ToplamaHucreleri)
            {
                tahta[konum].Toplama = true;
            }

            foreach (var cift in veri.OnTanimliDolgu)
            {
                foreach (string kod in cift.Value)
                {
                    var icerik = IcerikCoz(veri, kod, "onTanimliDolgu");
                    dolgu.OnTanimliEkle(cift.Key, t => icerik.TasUret(t)!);
                }
            }

            foreach (var set in veri.DolguSetleri)
            {
                dolgu.Setler.Add(SetKur(veri, set));
            }

            foreach (var cift in veri.SutunSetleri)
            {
                dolgu.SutunSetleri.Add(cift.Key, SetKur(veri, cift.Value));
            }

            // GDD 5.4: rastgele bırakılan hücreler hazır 3'lü ve kare oluşturmadan renklendirilir.
            BaslangicTahtasi.Uret(baglam, rastgeleHucreler);

            var oturum = new BolumOturumu(baglam, veri.Hamle, veri.Hedefler);

            // GDD 2.1.5: boş başlayan hücreler bölüm başında düşmeyle dolar.
            TahtaDongusu.Durult(baglam, new EtkiMotoru(baglam, olaylar), olaylar);
            oturum.OlasiHamleyiDenetle(olaylar);
            return oturum;
        }

        /// <summary>
        /// GDD 2.1.6, 8.1.6: giriş hücreleri bölüm verisinde hücre başına işaretlenir; harita verilmezse her sütunun
        /// en üstteki oynanabilir hücresi giriştir.
        /// </summary>
        private static void GirisleriIsaretle(BolumVerisi veri, Tahta tahta)
        {
            if (veri.Girisler != null)
            {
                foreach (var konum in tahta.TumKonumlar())
                {
                    bool giris = veri.Girisler[konum.Satir - 1][konum.Sutun - 1];
                    if (giris && !tahta[konum].Oynanabilir)
                    {
                        throw new BolumVerisiHatasi($"hücre {konum}: oynanmayan hücre giriş taşıyamaz (GDD 2.1.3).");
                    }

                    tahta[konum].Giris = giris;
                }

                return;
            }

            for (int sutun = 1; sutun <= tahta.SutunSayisi; sutun++)
            {
                for (int satir = 1; satir <= tahta.SatirSayisi; satir++)
                {
                    if (tahta[satir, sutun].Oynanabilir)
                    {
                        tahta[satir, sutun].Giris = true;
                        break;
                    }
                }
            }
        }

        private static DolguSeti SetKur(BolumVerisi veri, DolguSetiVerisi setVerisi)
        {
            var set = new DolguSeti { PasAgirligi = setVerisi.Pas };
            foreach (var oge in setVerisi.Ogeler)
            {
                if (oge.Kod == "?")
                {
                    set.Ogeler.Add(DolguOgesi.RastgeleRenk(oge.Agirlik, oge.Azami));
                    continue;
                }

                var icerik = IcerikCoz(veri, oge.Kod, "dolgu seti");
                set.Ogeler.Add(new DolguOgesi(t => icerik.TasUret(t)!, oge.Agirlik, oge.Azami));
            }

            return set;
        }
    }
}
