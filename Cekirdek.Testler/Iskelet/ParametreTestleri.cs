using System.Linq;
using System.Reflection;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Iskelet
{
    [TestFixture]
    [Category("Iskelet")]
    [Property("GDD", "0.4")]
    public class ParametreTestleri
    {
        private static ParametreTablosu Tablo => ProjeDosyalari.Parametreler;

        [Test]
        public void Dosya_245_tekil_parametre_tasir()
        {
            Assert.That(Tablo.Adet, Is.EqualTo(245));
            Assert.That(Tablo.Hepsi.Select(p => p.Ad).Distinct().Count(), Is.EqualTo(245));
        }

        [Test]
        public void Tur_sutunu_mekanik_ve_denge_olarak_ayrilir()
        {
            Assert.That(Tablo.Hepsi.Count(p => p.Tur == ParametreTuru.Mekanik), Is.EqualTo(23));
            Assert.That(Tablo.Hepsi.Count(p => p.Tur == ParametreTuru.Denge), Is.EqualTo(222));
        }

        [Test]
        public void Sayisal_oneri_degerleri_kendi_araliginin_icindedir()
        {
            Assert.That(Tablo.Dogrula(), Is.Empty);
        }

        [Test]
        public void Tam_sayi_ve_ondalikli_degerler_okunur()
        {
            Assert.That(Tablo.TamSayi(ParametreAdlari.TahtaGenislikStandart), Is.EqualTo(9));
            Assert.That(Tablo.TamSayi(ParametreAdlari.RenkSayisiStandart), Is.EqualTo(4));
            Assert.That(Tablo.Sayi(ParametreAdlari.AyarliDolguSikiOlasiligi), Is.EqualTo(0.35).Within(1e-12));
        }

        [Test]
        public void Kesirli_deger_tam_sayi_olarak_okunamaz()
        {
            Assert.Throws<ParametreHatasi>(() => Tablo.TamSayi(ParametreAdlari.AyarliDolguSikiOlasiligi));
        }

        [Test]
        public void Olmayan_parametre_hata_verir()
        {
            Assert.That(Tablo.Var("boyle_bir_parametre_yok"), Is.False);
            Assert.Throws<ParametreHatasi>(() => Tablo.Sayi("boyle_bir_parametre_yok"));
        }

        [Test]
        public void Tirnakli_alanlar_virgulleriyle_okunur()
        {
            Assert.That(Tablo.Metin("alan_sandigi_takviyeler"), Is.EqualTo("2 bomba taşı, 1 güdümlü taş"));
            Assert.That(Tablo.Al("sunum_bomba_halka_arasi").GddBolumu, Is.EqualTo("3.3.2, 4.3.3"));
        }

        [Test]
        public void Duz_sayi_listeleri_okunur()
        {
            Assert.That(
                Tablo.TamSayiListesi("ek_hamle_fiyat_kademeleri"),
                Is.EqualTo(new[] { 800, 1600, 2400, 3200, 4000 }));
            Assert.That(
                Tablo.TamSayiListesi("haftalik_hat_basamak_hedefleri"),
                Is.EqualTo(new[] { 2, 2, 4, 4, 6, 6, 8, 8, 10, 12 }));
            Assert.That(
                Tablo.SayiListesi("harita_kesif_sandik_esikleri"),
                Is.EqualTo(new[] { 0.2, 0.4, 0.6, 0.8, 1.0 }).Within(1e-12));
        }

        [Test]
        public void Serbest_metinli_deger_duz_liste_olarak_okunamaz()
        {
            Assert.Throws<ParametreHatasi>(() => Tablo.SayiListesi("sezon_hatti_basamak_hedefleri"));
            Assert.Throws<ParametreHatasi>(() => Tablo.Sayi("gun_siniri_saati"));
        }

        [Test]
        public void Degistir_yeni_tablo_dondurur_ozgunu_bozmaz()
        {
            var degisik = Tablo.Degistir(ParametreAdlari.KaristirmaDenemeSayisi, 7);

            Assert.That(degisik.TamSayi(ParametreAdlari.KaristirmaDenemeSayisi), Is.EqualTo(7));
            Assert.That(Tablo.TamSayi(ParametreAdlari.KaristirmaDenemeSayisi), Is.EqualTo(20));
            Assert.That(degisik.Adet, Is.EqualTo(Tablo.Adet));
        }

        [Test]
        public void Aralik_disi_deger_dogrulamada_yakalanir()
        {
            var degisik = Tablo.Degistir(ParametreAdlari.RenkSayisiStandart, 9);

            var sorunlar = degisik.Dogrula();

            Assert.That(sorunlar.Select(s => s.Ad), Is.EqualTo(new[] { ParametreAdlari.RenkSayisiStandart }));
        }

        [Test]
        public void Kodun_kullandigi_her_parametre_adi_dosyada_vardir()
        {
            var adlar = typeof(ParametreAdlari)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(a => a.IsLiteral && a.Name != nameof(ParametreAdlari.SunumOnEki))
                .Select(a => (string)a.GetRawConstantValue()!)
                .ToList();

            Assert.That(adlar, Is.Not.Empty);
            Assert.That(adlar.Where(ad => !Tablo.Var(ad)), Is.Empty);
            Assert.That(adlar.Where(ad => ad.StartsWith(ParametreAdlari.SunumOnEki)), Is.Empty,
                "Çekirdek sunum parametrelerini okumaz (GDD 0.4).");
        }

        [Test]
        public void Bozuk_csv_hata_verir()
        {
            Assert.Throws<ParametreHatasi>(() => ParametreTablosu.CsvdenOku(string.Empty));
            Assert.Throws<ParametreHatasi>(() => ParametreTablosu.CsvdenOku("ad,deger\nx,1\n"));

            const string baslik = "ad,tur,açıklama,öneri_varsayılan,aralık,birim,güven_notu,gdd_bölümü\n";
            Assert.Throws<ParametreHatasi>(() => ParametreTablosu.CsvdenOku(baslik + "x,mekanik,a,1,0–2,b,c\n"));
            Assert.Throws<ParametreHatasi>(() => ParametreTablosu.CsvdenOku(baslik + "x,baska,a,1,0–2,b,c,d\n"));
            Assert.Throws<ParametreHatasi>(() => ParametreTablosu.CsvdenOku(
                baslik + "x,mekanik,a,1,0–2,b,c,d\nx,mekanik,a,2,0–2,b,c,d\n"));
        }
    }
}
