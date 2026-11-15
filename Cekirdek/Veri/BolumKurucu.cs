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
        /// <param name="hamleEki">
        /// Yarışmalı bölümde yarışma sisteminden gelen ve hamle sayısına eklenen değer (GDD 7.2.5, 9.11.2); negatif
        /// olabilir, hamle sayısı 1'in altına inmez. Normal ilerleme bölümlerinde 0'dır.
        /// </param>
        public static BolumOturumu Kur(
            BolumVerisi veri,
            AkBayraklari bayraklar,
            ParametreTablosu parametreler,
            ulong tohum,
            List<Olay> olaylar,
            double zorlukAgirligi = 0,
            int hamleEki = 0)
        {
            if (veri.Ekranlar.Count == 0)
            {
                throw new BolumVerisiHatasi($"Bölüm {veri.Kimlik}: ekran tanımı yok; veri BolumYukleyici ile okunmalı.");
            }

            // GDD 2.1.7: oyuncu bir anda yalnızca bir ekranı oynar; her ekran kendi tahtasıdır, böylece hiçbir etki,
            // eşleşme ya da düşme ekran sınırını aşmaz (8.9.3).
            var rastgeleHucreler = new List<Konum>();
            var tahta = EkranTahtasiKur(veri, 1, null, rastgeleHucreler);
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

            var oturum = new BolumOturumu(baglam, hamleEki == 0 ? veri.Hamle : Math.Max(1, veri.Hamle + hamleEki), veri.Hedefler);
            if (veri.Ekranlar.Count > 1)
            {
                oturum.EkranSayisi = veri.Ekranlar.Count;
                oturum.EkranKurucu = (ekran, onceki, hucreler) => EkranTahtasiKur(veri, ekran, onceki, hucreler);
            }

            // GDD 2.1.5: boş başlayan hücreler bölüm başında düşmeyle dolar.
            TahtaDongusu.Durult(baglam, new EtkiMotoru(baglam, olaylar), olaylar);
            oturum.OlasiHamleyiDenetle(olaylar);
            return oturum;
        }

        /// <summary>
        /// Bir ekranın tahtasını bölüm verisinin o ekrana düşen satırlarından kurar (GDD 8.9.1). Tahtanın 1. satırı
        /// ekranın en üst satırıdır. Rastgele renklendirilecek hücreler listeye eklenir.
        /// </summary>
        private static Tahta EkranTahtasiKur(BolumVerisi veri, int ekran, Tahta? onceki, List<Konum> rastgeleHucreler)
        {
            var aralik = veri.Ekranlar[ekran - 1];
            int kayma = aralik.IlkSatir - 1;
            var tahta = new Tahta(aralik.Yukseklik, veri.Genislik);
            if (onceki != null)
            {
                tahta.KimlikSayaciniDevral(onceki);
            }

            foreach (var konum in tahta.TumKonumlar())
            {
                int s = kayma + konum.Satir - 1;
                int t = konum.Sutun - 1;
                var hucre = tahta[konum];
                var icerik = IcerikCoz(veri, veri.Hucreler[s][t], $"hücre {new Konum(s + 1, t + 1)}");
                hucre.Oynanabilir = !icerik.Oynanmayan;
                hucre.Tas = icerik.TasUret(tahta);
                if (icerik.RastgeleRenk)
                {
                    rastgeleHucreler.Add(konum);
                }

                string? alt = veri.AltKatmanlar?[s][t];
                if (alt != null && alt != ".")
                {
                    hucre.AltKatman = KatmanCoz(veri, alt, $"hücre {new Konum(s + 1, t + 1)}");
                }

                string? ust = veri.UstKatmanlar?[s][t];
                if (ust != null && ust != ".")
                {
                    hucre.UstKatman = KatmanCoz(veri, ust, $"hücre {new Konum(s + 1, t + 1)}");
                }
            }

            GirisleriIsaretle(veri, tahta, kayma);
            foreach (var konum in veri.ToplamaHucreleri)
            {
                if (konum.Satir >= aralik.IlkSatir && konum.Satir <= aralik.SonSatir)
                {
                    tahta[new Konum(konum.Satir - kayma, konum.Sutun)].Toplama = true;
                }
            }

            return tahta;
        }

        /// <summary>
        /// GDD 2.1.6, 8.1.6: giriş hücreleri bölüm verisinde hücre başına işaretlenir; harita verilmezse her sütunun
        /// (o ekrandaki) en üstteki oynanabilir hücresi giriştir.
        /// </summary>
        private static void GirisleriIsaretle(BolumVerisi veri, Tahta tahta, int kayma)
        {
            if (veri.Girisler != null)
            {
                foreach (var konum in tahta.TumKonumlar())
                {
                    bool giris = veri.Girisler[kayma + konum.Satir - 1][konum.Sutun - 1];
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
