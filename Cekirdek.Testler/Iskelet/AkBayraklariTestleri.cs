using System.Linq;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Iskelet
{
    [TestFixture]
    [Category("Iskelet")]
    [Property("GDD", "0.3")]
    public class AkBayraklariTestleri
    {
        [Test]
        public void On_bes_ayarlanabilir_kural_vardir_ve_varsayilan_A_dir()
        {
            Assert.That(AkBayraklari.Kimlikler.Count, Is.EqualTo(15));
            Assert.That(AkBayraklari.Kimlikler.Select(AkBayraklari.KimlikMetni).First(), Is.EqualTo("AK-01"));
            Assert.That(AkBayraklari.Kimlikler.Select(AkBayraklari.KimlikMetni).Last(), Is.EqualTo("AK-15"));
            foreach (var ak in AkBayraklari.Kimlikler)
            {
                Assert.That(AkBayraklari.Varsayilan[ak], Is.EqualTo(Varyant.A), AkBayraklari.KimlikMetni(ak));
            }
        }

        [Test]
        public void Ek_bayraklar_GDD_varsayilanindadir()
        {
            var b = AkBayraklari.Varsayilan;

            Assert.That(b.KaristirmaRenkDegisimi, Is.True, "GDD 5.6.6");
            Assert.That(b.CizgiOynanmayanHucredeDurur, Is.False, "GDD 3.2.4, 11.2.1");
            Assert.That(b.DusmeSirasindaHamle, Is.True, "GDD 2.3.9");
        }

        [Test]
        public void Bayrak_adlari_GDD_deki_adlardir()
        {
            Assert.That(AkBayraklari.BayrakAdi(Ak.KaskadDogumHucresi), Is.EqualTo("kaskad_dogum_hucresi"));
            Assert.That(AkBayraklari.BayrakAdi(Ak.UzatmaAdimi), Is.EqualTo("uzatma_adimi"));
            Assert.That(AkBayraklari.BayrakAdi(Ak.CiftRenkKombinasyon), Is.EqualTo("cift_renk_kombinasyon"));
            Assert.That(
                AkBayraklari.Kimlikler.Select(AkBayraklari.BayrakAdi).Distinct().Count(),
                Is.EqualTo(15));
        }

        [Test]
        public void Ile_yalniz_istenen_bayragi_degistirir()
        {
            var b = AkBayraklari.Varsayilan.Ile(Ak.BombaBombaMerkezi, Varyant.B);

            Assert.That(b[Ak.BombaBombaMerkezi], Is.EqualTo(Varyant.B));
            Assert.That(AkBayraklari.Varsayilan[Ak.BombaBombaMerkezi], Is.EqualTo(Varyant.A));
            Assert.That(
                AkBayraklari.Kimlikler.Where(ak => ak != Ak.BombaBombaMerkezi).Select(ak => b[ak]),
                Is.All.EqualTo(Varyant.A));
        }

        [Test]
        public void Jsondan_okunur_eksik_bayraklar_varsayilanda_kalir()
        {
            var b = AkBayraklari.JsondanOku(
                "{ \"uzatma_adimi\": \"B\", \"karistirma_renk_degisimi\": false }");

            Assert.That(b[Ak.UzatmaAdimi], Is.EqualTo(Varyant.B));
            Assert.That(b[Ak.KaskadDogumHucresi], Is.EqualTo(Varyant.A));
            Assert.That(b.KaristirmaRenkDegisimi, Is.False);
            Assert.That(b.DusmeSirasindaHamle, Is.True);
        }

        [Test]
        public void Bos_nesne_varsayilani_verir()
        {
            var b = AkBayraklari.JsondanOku("{}");

            Assert.That(AkBayraklari.Kimlikler.Select(ak => b[ak]), Is.All.EqualTo(Varyant.A));
        }

        [TestCase("{ \"olmayan_bayrak\": \"A\" }")]
        [TestCase("{ \"uzatma_adimi\": \"C\" }")]
        [TestCase("{ \"uzatma_adimi\": true }")]
        [TestCase("{ \"karistirma_renk_degisimi\": \"A\" }")]
        [TestCase("{ \"uzatma_adimi\": \"A\", \"uzatma_adimi\": \"B\" }")]
        [TestCase("{ \"uzatma_adimi\": \"A\" ")]
        [TestCase("{ \"uzatma_adimi\": \"A\" } fazlalik")]
        [TestCase("")]
        public void Gecersiz_yapilandirma_hata_verir(string json)
        {
            Assert.Throws<BayrakHatasi>(() => AkBayraklari.JsondanOku(json));
        }

        [Test]
        public void Depodaki_bayrak_dosyasi_okunur_ve_varsayilana_esittir()
        {
            var b = AkBayraklari.JsondanOku(ProjeDosyalari.Oku("veri", "ak_bayraklari.json"));

            Assert.That(AkBayraklari.Kimlikler.Select(ak => b[ak]), Is.All.EqualTo(Varyant.A));
            Assert.That(b.KaristirmaRenkDegisimi, Is.EqualTo(AkBayraklari.Varsayilan.KaristirmaRenkDegisimi));
            Assert.That(
                b.CizgiOynanmayanHucredeDurur,
                Is.EqualTo(AkBayraklari.Varsayilan.CizgiOynanmayanHucredeDurur));
            Assert.That(b.DusmeSirasindaHamle, Is.EqualTo(AkBayraklari.Varsayilan.DusmeSirasindaHamle));
        }

        [Test]
        public void Depodaki_bayrak_dosyasi_butun_bayraklari_listeler()
        {
            string json = ProjeDosyalari.Oku("veri", "ak_bayraklari.json");

            foreach (var ak in AkBayraklari.Kimlikler)
            {
                Assert.That(json, Does.Contain("\"" + AkBayraklari.BayrakAdi(ak) + "\""));
            }
        }
    }
}
