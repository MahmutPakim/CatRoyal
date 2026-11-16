using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>Bölüm verisi (GDD 8.10), örnek bölümler, dolgu setleri (5.3.4) ve ayarlı dolgu (5.3.6).</summary>
    [TestFixture]
    [Category("Kural")]
    public class BolumVerisiTestleri
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34 };

        private static IEnumerable<string> OrnekBolumler() =>
            Directory.GetFiles(Path.Combine(ProjeDosyalari.Kok, "veri", "bolumler"), "bolum_*.json")
                .Select(Path.GetFileName)
                .OrderBy(ad => ad)!;

        private static BolumVerisi Oku(string dosya) =>
            BolumYukleyici.JsondanOku(ProjeDosyalari.Oku("veri", "bolumler", dosya), ProjeDosyalari.Parametreler);

        private static BolumOturumu Kur(BolumVerisi veri, int tohum, List<Olay> olaylar) =>
            BolumKurucu.Kur(veri, AkBayraklari.Varsayilan, ProjeDosyalari.Parametreler, (ulong)tohum, olaylar);

        [Test]
        [Property("GDD", "8.10")]
        public void Depoda_en_az_dort_ornek_bolum_vardir()
        {
            Assert.That(OrnekBolumler().Count(), Is.GreaterThanOrEqualTo(4));
        }

        [TestCaseSource(nameof(OrnekBolumler))]
        [Property("GDD", "8.10, 8.1.1, 8.3.1, 5.4.2, 5.4.3, 5.4.4")]
        public void Ornek_bolum_yuklenir_ve_gecerli_bir_baslangic_tahtasi_kurar(string dosya)
        {
            var veri = Oku(dosya);
            var p = ProjeDosyalari.Parametreler;
            Assert.That(veri.Hamle, Is.InRange(p.TamSayi("hamle_bandi_alt"), p.TamSayi("hamle_bandi_ust")), "GDD 8.3.1");
            Assert.That(veri.Hedefler, Is.Not.Empty);

            foreach (int tohum in Tohumlar)
            {
                var oturum = Kur(veri, tohum, new List<Olay>());
                var tahta = oturum.Baglam.Tahta;

                Assert.That(tahta.SutunSayisi, Is.EqualTo(veri.Genislik));
                Assert.That(tahta.SatirSayisi, Is.EqualTo(veri.Ekranlar[0].Yukseklik), "Çok ekranlı bölümde ilk ekran oynanır (GDD 2.1.7).");
                Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor), $"tohum {tohum}");
                Assert.That(oturum.KalanHamle, Is.EqualTo(veri.Hamle));
                Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False, $"tohum {tohum}");
                Assert.That(OlasiHamleBulucu.Var(tahta), Is.True, $"tohum {tohum}");
                Assert.That(oturum.Hedefler.Turler.Select(oturum.Hedefler.Kalan), Is.All.GreaterThan(0));
                Assert.That(
                    tahta.TumKonumlar().Where(k => tahta[k].Oynanabilir).Select(k => tahta[k].Tas),
                    Has.None.Null,
                    "Bütün oynanabilir hücreler doludur.");
            }
        }

        [Test]
        [Property("GDD", "7.1.2, 6.1.6")]
        public void Ornek_bolumlerin_hedef_adetleri_tahtadaki_ogelerle_tutarlidir()
        {
            var ikinci = Kur(Oku("bolum_002.json"), 1, new List<Olay>());
            Assert.That(ikinci.Hedefler.Kalan("katmanli_blok"), Is.EqualTo(10), "Adet 0: tahtadaki bütün bloklar.");

            var ucuncu = Kur(Oku("bolum_003.json"), 1, new List<Olay>());
            Assert.That(ucuncu.Hedefler.Kalan("zemin"), Is.EqualTo(19));
            Assert.That(ucuncu.Baglam.CaprazKaymaIzinli, Is.True);
            Assert.That(ucuncu.Baglam.Tahta[1, 1].Oynanabilir, Is.False);
            Assert.That(ucuncu.Baglam.Tahta[1, 2].Giris, Is.True, "Giriş: sütunun en üstteki oynanabilir hücresi.");
            Assert.That(ucuncu.Baglam.Tahta[2, 1].Giris, Is.True);
            Assert.That(ucuncu.Baglam.Tahta[5, 4].Kilitli, Is.True);
        }

        private static string OyunuOyna(string dosya, int tohum)
        {
            var olaylar = new List<Olay>();
            var oturum = Kur(Oku(dosya), tohum, olaylar);
            var secici = new TohumluRastgele(tohum * 31 + 7);

            for (int i = 0; i < 200 && oturum.Durum == BolumDurumu.Oynaniyor; i++)
            {
                var hamleler = OlasiHamleBulucu.Bul(oturum.Baglam.Tahta);
                Assert.That(hamleler, Is.Not.Empty, "Tam akış olası hamle bırakır ya da bölümü bitirir.");
                var hamle = secici.Sec<OlasiHamle>(hamleler);
                var sonuc = hamle.Tur == OlasiHamleTuru.OzelTasDokunma
                    ? oturum.Dokun(hamle.Cikilan, olaylar)
                    : oturum.Kaydir(hamle.Cikilan, hamle.Gidilen, olaylar);
                Assert.That(sonuc, Is.EqualTo(HamleSonucu.Gecerli), $"{dosya}, tohum {tohum}, hamle {i}: {hamle.Tur}");

                var tahta = oturum.Baglam.Tahta;
                if (oturum.Durum == BolumDurumu.Oynaniyor)
                {
                    Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False, "Durulmuş tahtada hazır eşleşme kalmaz.");
                }
            }

            if (oturum.Durum == BolumDurumu.EkHamleTeklifi)
            {
                Assert.That(oturum.KalanHamle, Is.EqualTo(0));
                oturum.EkHamleTeklifiniReddet(olaylar);
            }

            Assert.That(
                oturum.Durum,
                Is.EqualTo(BolumDurumu.Kazanildi).Or.EqualTo(BolumDurumu.Kaybedildi).Or.EqualTo(BolumDurumu.BastanBaslatilmali));
            if (oturum.Durum == BolumDurumu.Kazanildi)
            {
                Assert.That(oturum.Hedefler.HepsiTamam, Is.True);
            }

            return $"{oturum.Durum}/{oturum.YapilanHamle}/{olaylar.Count}";
        }

        [Test]
        [Property("GDD", "5.7, 7.3, 7.4")]
        public void Ozellik_ornek_bolumler_rastgele_hamlelerle_sonuna_kadar_oynanir(
            [ValueSource(nameof(OrnekBolumler))] string dosya,
            [ValueSource(nameof(Tohumlar))] int tohum)
        {
            string sonuc = OyunuOyna(dosya, tohum);

            Assert.That(OyunuOyna(dosya, tohum), Is.EqualTo(sonuc), "Aynı tohum aynı oyunu verir (GDD 0.4).");
        }

        // ------------------------------------------------------------------ Doğrulama

        private static string TekSatirlikBolum(string ekAlanlar, string hucreler = "? ? ?", int genislik = 3) =>
            "{ \"kimlik\": 9, \"genislik\": " + genislik + ", \"yukseklik\": 1, \"hamle\": 20, \"renkler\": 4, "
            + ekAlanlar + " \"hucreler\": [\"" + hucreler + "\"] }";

        [Test]
        [Property("GDD", "8.1.1, 8.2.2, 8.4.1, 8.9, 6.1.1")]
        public void Sinirlari_asan_ya_da_bozuk_bolum_verisi_reddedilir()
        {
            var p = ProjeDosyalari.Parametreler;
            void Reddedilir(string json) => Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(json, p));

            Assert.DoesNotThrow(() => BolumYukleyici.JsondanOku(TekSatirlikBolum(string.Empty), p));

            Reddedilir("{ bozuk");
            Reddedilir(TekSatirlikBolum(string.Empty, "? ?"));
            Reddedilir(TekSatirlikBolum(string.Empty, "? ? Q"));
            Reddedilir(TekSatirlikBolum(string.Empty, "? ? r7"));
            Reddedilir(TekSatirlikBolum(string.Empty, string.Join(" ", Enumerable.Repeat("?", 10)), genislik: 10));
            Reddedilir(TekSatirlikBolum("\"bayraklar\": { \"cokEkranli\": true },"));
            Reddedilir(TekSatirlikBolum(string.Empty).Replace("\"renkler\": 4", "\"renkler\": 9"));
            Reddedilir(TekSatirlikBolum(string.Empty).Replace("\"hamle\": 20", "\"hamle\": 0"));
            Reddedilir(TekSatirlikBolum("\"engeller\": { \"kb\": { \"arketip\": \"katmanli_blok\" } },", "? kb:9 ?"));
            Reddedilir(TekSatirlikBolum("\"engeller\": { \"kb\": { \"arketip\": \"bilinmeyen\" } },"));
            Reddedilir(TekSatirlikBolum(
                "\"hedefler\": [{\"tur\":\"a\"},{\"tur\":\"b\"},{\"tur\":\"c\"},{\"tur\":\"d\"},{\"tur\":\"e\"}],"));
            Reddedilir(TekSatirlikBolum(
                "\"katmanlar\": { \"k\": { \"arketip\": \"kilit\" } }, \"altKatmanlar\": [\". k:1 .\"],"));
        }

        [Test]
        [Property("GDD", "2.1.5")]
        public void Bos_baslayan_hucreler_bolum_basinda_dusmeyle_dolar()
        {
            const string json = @"{ ""kimlik"": 9, ""genislik"": 3, ""yukseklik"": 3, ""hamle"": 20, ""renkler"": 4,
                ""hucreler"": [""_ _ _"", ""? ? ?"", ""? ? ?""] }";
            var olaylar = new List<Olay>();

            var oturum = Kur(BolumYukleyici.JsondanOku(json, ProjeDosyalari.Parametreler), 3, olaylar);

            Assert.That(olaylar.OfType<TasGirdi>().Count(), Is.GreaterThanOrEqualTo(3));
            Assert.That(oturum.Baglam.Tahta.TumKonumlar().Select(k => oturum.Baglam.Tahta[k].Tas), Has.None.Null);
        }

        // ------------------------------------------------------------------ Dolgu setleri ve ayarlı dolgu

        [Test]
        [Property("GDD", "5.3.4, 8.4.5")]
        public void Dolgu_seti_ogesi_azami_adedini_asmaz_ve_pas_sirayi_rastgele_renge_birakir()
        {
            var tahta = new Tahta(1, 1);
            var baglam = HamleDenemesi.BaglamKur(tahta, tohum: 4);
            baglam.Renkler = new[] { Kirmizi, Mavi, Yesil, Sari };
            var dolgu = new RastgeleDolgu();
            var set = new DolguSeti { PasAgirligi = 1 };
            set.Ogeler.Add(new DolguOgesi(t => t.OzelTasUret(TasTuru.Bomba), agirlik: 3, azamiAdet: 5));
            dolgu.Setler.Add(set);

            var uretilen = Enumerable.Range(0, 200).Select(_ => dolgu.Uret(baglam, H(1, 1), H(1, 1))).ToList();

            Assert.That(uretilen.Count(t => t.Tur == TasTuru.Bomba), Is.EqualTo(5));
            Assert.That(uretilen.Where(t => t.Tur != TasTuru.Bomba).Select(t => t.Tur), Is.All.EqualTo(TasTuru.Normal));
            Assert.That(uretilen.Take(20).Any(t => t.Tur == TasTuru.Normal), Is.True, "Pas ağırlığı rastgele renge yer açar.");
        }

        [Test]
        [Property("GDD", "5.3.4")]
        public void Sutuna_atanmis_set_rastgele_dolgu_setlerinden_once_gelir()
        {
            var tahta = new Tahta(1, 2);
            var baglam = HamleDenemesi.BaglamKur(tahta);
            baglam.Renkler = new[] { Kirmizi, Mavi };
            var dolgu = new RastgeleDolgu();
            var sutunSeti = new DolguSeti();
            sutunSeti.Ogeler.Add(new DolguOgesi(t => t.OzelTasUret(TasTuru.Gudumlu), 1));
            dolgu.SutunSetleri.Add(2, sutunSeti);
            var genelSet = new DolguSeti();
            genelSet.Ogeler.Add(new DolguOgesi(t => t.OzelTasUret(TasTuru.Bomba), 1));
            dolgu.Setler.Add(genelSet);

            Assert.That(dolgu.Uret(baglam, H(1, 2), H(1, 2)).Tur, Is.EqualTo(TasTuru.Gudumlu));
            Assert.That(dolgu.Uret(baglam, H(1, 1), H(1, 1)).Tur, Is.EqualTo(TasTuru.Bomba));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "5.3.6")]
        public void Ayarli_dolgu_siki_duzeyde_asagi_sol_ve_sag_komsuyla_ayni_rengi_vermez(int tohum)
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  K _ M
                2  · Y ·");
            var baglam = HamleDenemesi.BaglamKur(
                test.Tahta,
                tohum: tohum,
                parametreler: ProjeDosyalari.Parametreler.Degistir(ParametreAdlari.AyarliDolguSikiOlasiligi, 1));
            baglam.Renkler = new[] { Kirmizi, Mavi, Yesil, Sari };

            for (int i = 0; i < 50; i++)
            {
                Assert.That(RenkSecici.AyarliSec(baglam, H(1, 2)), Is.EqualTo(Sari));
            }
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "5.3.6")]
        public void Ayarli_dolgu_normal_duzeyde_yalniz_uclu_tamamlayacak_rengi_yasaklar(int tohum)
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4
                1  K K _ M
                2  · · Y ·
                3  · · Y ·");
            var baglam = HamleDenemesi.BaglamKur(
                test.Tahta,
                tohum: tohum,
                parametreler: ProjeDosyalari.Parametreler.Degistir(ParametreAdlari.AyarliDolguSikiOlasiligi, 0));
            baglam.Renkler = new[] { Kirmizi, Mavi, Yesil, Sari };

            var secilen = Enumerable.Range(0, 200).Select(_ => RenkSecici.AyarliSec(baglam, H(1, 3))).ToList();

            // Sol iki K ve alt iki Y 3'lü tamamlardı; sağdaki tek M yasak değildir.
            Assert.That(secilen, Has.None.EqualTo(Kirmizi));
            Assert.That(secilen, Has.None.EqualTo(Yesil));
            Assert.That(secilen, Does.Contain(Mavi));
            Assert.That(secilen, Does.Contain(Sari));
        }
    }
}
