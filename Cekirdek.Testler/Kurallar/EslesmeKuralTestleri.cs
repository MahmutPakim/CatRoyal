using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>GDD 2'nin test_vakalari.md'de doğrudan vakası olmayan kuralları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class EslesmeKuralTestleri
    {
        [Test]
        [Property("GDD", "2.3.3, 2.3.4")]
        public void Eslesme_kurmayan_kaydirma_gecersizdir_ve_tahta_degismez()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · K M · ·
                3  · · · · ·");
            var once = test.Tahta.TumKonumlar().Select(k => test.Tahta[k].Tas).ToList();

            var d = HamleDenemesi.Kaydir(test, H(2, 2), H(2, 3));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecersiz));
            Assert.That(d.Gruplar, Is.Empty);
            Assert.That(test.Tahta.TumKonumlar().Select(k => test.Tahta[k].Tas), Is.EqualTo(once));
        }

        [Test]
        [Property("GDD", "2.3.1, 2.3.2, 6.2.6")]
        public void Komsu_olmayan_bos_oynanmayan_ya_da_kilitli_hucreyle_kaydirma_imkansizdir()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  K K M K X
                2  · · _ · ·
                3  · · K · ·");
            test.Tahta[1, 1].Kilitli = true;

            Assert.That(HamleDenemesi.Kaydir(test, H(1, 4), H(1, 2)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(HamleDenemesi.Kaydir(test, H(1, 4), H(2, 3)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(HamleDenemesi.Kaydir(test, H(1, 3), H(2, 3)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(HamleDenemesi.Kaydir(test, H(1, 4), H(1, 5)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(HamleDenemesi.Kaydir(test, H(1, 1), H(2, 1)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(HamleDenemesi.Kaydir(test, H(1, 4), H(1, 6)).Sonuc, Is.EqualTo(HamleSonucu.Imkansiz));
        }

        [Test]
        [Property("GDD", "2.2.3")]
        public void Ozel_tas_renk_eslesmesine_girmez()
        {
            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4 5
                1  K K B K K
                2  · · · · ·");

            Assert.That(d.Gruplar, Is.Empty);
        }

        [Test]
        [Property("GDD", "2.2.4")]
        public void Dusmekte_olan_tas_eslesme_aramasina_alinmaz()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  K K K · ·
                2  · · · · ·");
            test.Tahta[1, 2].Tas!.Oturdu = false;

            Assert.That(HamleDenemesi.KaskadAdimi(test).Gruplar, Is.Empty);
        }

        [TestCase(Varyant.A, 6)]
        [TestCase(Varyant.B, 5)]
        [Property("GDD", "2.5.9")]
        [Property("AK", "AK-10")]
        public void Altili_sirada_renk_kuresi_olusur_altinci_tas_AK10_a_baglidir(Varyant ak10, int patlayan)
        {
            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4 5 6 7
                1  K K K K K K ·
                2  · · · · · · ·", AkBayraklari.Varsayilan.Ile(Ak.UzatmaAdimi, ak10));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.Duz5));
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.RenkKuresi));
            Assert.That(d.PatlayanHucreler, Has.Count.EqualTo(patlayan));
            Assert.That(d.Tahta[1, 6].Tas == null, Is.EqualTo(ak10 == Varyant.A));
        }

        [TestCase(Varyant.A, 1)]
        [TestCase(Varyant.B, 2)]
        [Property("GDD", "2.6.5")]
        [Property("AK", "AK-06")]
        public void Iki_ust_uste_kare_AK06_A_da_ustteki_B_de_alttaki_kazanir(Varyant ak06, int ustSatir)
        {
            // AK-06 örnek durumu: aynı renkte 2×3 blok, iki çakışan kare.
            var bayraklar = AkBayraklari.Varsayilan
                .Ile(Ak.EsitOncelikSecimi, ak06)
                .Ile(Ak.UzatmaAdimi, Varyant.B);

            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4
                1  K K · ·
                2  K K · ·
                3  K K · ·
                4  · · · ·", bayraklar);

            // Dikey 3'lüler karelerden düşük önceliklidir; kareyle çakıştıkları için atlanır.
            var grup = d.Gruplar.Single();
            Assert.That(grup.Sekil, Is.EqualTo(Sekil.Kare));
            Assert.That(grup.Hucreler.Min(k => k.Satir), Is.EqualTo(ustSatir));
        }

        [TestCase(Varyant.A, 5)]
        [TestCase(Varyant.B, 2)]
        [Property("GDD", "2.7.4")]
        [Property("AK", "AK-01")]
        public void Blok_halinde_dusen_dikey_dortlude_cizgi_tasi_AK01_ucunda_dogar(Varyant ak01, int satir)
        {
            // AK-01 örnek durumu: dört taş birlikte düşer, hepsi aynı adımda oturur.
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  · · ·
                2  · K ·
                3  · K ·
                4  · K ·
                5  · K ·
                6  · · ·");
            for (int s = 2; s <= 5; s++) test.Tahta[s, 2].Tas!.OturmaAdimi = 4;

            var d = HamleDenemesi.KaskadAdimi(test, AkBayraklari.Varsayilan.Ile(Ak.KaskadDogumHucresi, ak01));

            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(satir, 2)));
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Yatay));
        }

        [Test]
        [Property("GDD", "2.7.4")]
        public void Kaskadda_en_gec_oturan_tas_AK01_ucundaki_tasin_onune_gecer()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  · K ·
                2  · K ·
                3  · K ·
                4  · K ·
                5  · · ·");
            test.Tahta[2, 2].Tas!.OturmaAdimi = 7;

            var d = HamleDenemesi.KaskadAdimi(test);

            Assert.That(d.DoganOzelTaslar.Single().Konum, Is.EqualTo(H(2, 2)));
        }

        [Test]
        [Property("GDD", "2.7.4")]
        public void Yatay_grupta_esitlik_surerse_etkisiz_agirlikla_en_soldaki_secilir()
        {
            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4 5
                1  · K K K K
                2  · · · · ·");

            Assert.That(d.DoganOzelTaslar.Single().Konum, Is.EqualTo(H(1, 2)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(11)]
        [TestCase(97)]
        [Property("GDD", "2.7.4, 2.7.7")]
        public void Yatay_grupta_esitlik_rastgele_bozulursa_secim_grubun_icinde_kalir_ve_tohumla_tekrarlanir(int tohum)
        {
            const string cizim = @"
                   1 2 3 4 5
                1  · K K K K
                2  · · · · ·";
            var ilk = TahtaKurucu.Kur(cizim).Tahta;
            var ikinci = TahtaKurucu.Kur(cizim).Tahta;
            var grup = EslesmeBulucu.Bul(ilk, AkBayraklari.Varsayilan).Single();

            var secim1 = DogumHucresi.Kaskad(ilk, grup, AkBayraklari.Varsayilan, new TohumluRastgele(tohum));
            var secim2 = DogumHucresi.Kaskad(ikinci, grup, AkBayraklari.Varsayilan, new TohumluRastgele(tohum));

            Assert.That(grup.Hucreler, Does.Contain(secim1!.Value));
            Assert.That(secim2, Is.EqualTo(secim1));
        }

        [Test]
        [Property("GDD", "2.7.5")]
        public void Kilitli_hucre_dogum_hucresi_olamaz()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · K K K K
                2  · · · · ·");
            test.Tahta[1, 2].Kilitli = true;
            test.Tahta[1, 3].Tas!.OturmaAdimi = 0;

            var d = HamleDenemesi.KaskadAdimi(test);

            Assert.That(d.DoganOzelTaslar.Single().Konum, Is.EqualTo(H(1, 3)));
        }

        [Test]
        [Property("GDD", "2.6.3")]
        public void Ortusmeyen_farkli_renkli_gruplar_kaskadda_birlikte_kabul_edilir()
        {
            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4 5
                1  K K K · ·
                2  · · · · ·
                3  · M M M M");

            Assert.That(d.Gruplar.Select(g => g.Sekil), Is.EquivalentTo(new[] { Sekil.Duz3, Sekil.YatayDortlu }));
            Assert.That(d.DoganOzelTaslar, Has.Count.EqualTo(1));
        }

        // --- Özellik testleri: rastgele tahtalar, birden çok tohum ---

        private static readonly int[] Tohumlar = Enumerable.Range(1, 60).ToArray();

        private static Tahta RastgeleTahta(int tohum)
        {
            var rastgele = new TohumluRastgele(tohum);
            int renkSayisi = ProjeDosyalari.Parametreler.TamSayi(ParametreAdlari.RenkSayisiStandart);
            var tahta = new Tahta(7, 7);
            foreach (var konum in tahta.TumKonumlar())
            {
                // Bol eşleşme çıksın diye standarttan bir eksik renk.
                tahta[konum].Tas = tahta.NormalTasUret(rastgele.Sonraki(renkSayisi - 1));
            }

            return tahta;
        }

        private static IEnumerable<Konum[]> HamPencereler(Tahta tahta)
        {
            foreach (var k in tahta.TumKonumlar())
            {
                int renk = tahta.EslesmeRengi(k);
                var yatay = new[] { k, k.Sag, k.Sag.Sag };
                var dikey = new[] { k, k.Asagi, k.Asagi.Asagi };
                var kare = new[] { k, k.Sag, k.Asagi, k.Asagi.Sag };
                foreach (var pencere in new[] { yatay, dikey, kare })
                {
                    if (pencere.All(h => tahta.EslesmeRengi(h) == renk))
                    {
                        yield return pencere;
                    }
                }
            }
        }

        [Test]
        [Property("GDD", "2.4.1, 2.4.2, 2.6.2, 2.6.6")]
        public void Ozellik_gruplar_ayriktir_tek_renklidir_ve_her_eslesme_penceresine_dokunur(
            [ValueSource(nameof(Tohumlar))] int tohum,
            [Values(Varyant.A, Varyant.B)] Varyant ak06,
            [Values(Varyant.A, Varyant.B)] Varyant ak10)
        {
            var tahta = RastgeleTahta(tohum);
            var bayraklar = AkBayraklari.Varsayilan
                .Ile(Ak.EsitOncelikSecimi, ak06)
                .Ile(Ak.UzatmaAdimi, ak10);

            var gruplar = EslesmeBulucu.Bul(tahta, bayraklar);

            var hepsi = gruplar.SelectMany(g => g.TumHucreler).ToList();
            Assert.That(hepsi.Distinct().Count(), Is.EqualTo(hepsi.Count), "Bir hücre iki gruba ait olamaz.");
            foreach (var grup in gruplar)
            {
                Assert.That(grup.TumHucreler.Select(tahta.EslesmeRengi), Is.All.EqualTo(grup.Renk));
            }

            var sahipli = new HashSet<Konum>(hepsi);
            foreach (var pencere in HamPencereler(tahta))
            {
                if (ak10 == Varyant.A)
                {
                    // Uzatma açık: bir eşleşme penceresindeki her hücre bir grupla patlar.
                    Assert.That(pencere.All(sahipli.Contains), Is.True);
                }
                else
                {
                    // Uzatma kapalı: her pencere ya kabul edilmiştir ya da kabul edilmiş bir grupla çakışır.
                    Assert.That(pencere.Any(sahipli.Contains), Is.True);
                    Assert.That(gruplar.SelectMany(g => g.UzatmaHucreleri), Is.Empty);
                }
            }
        }

        [Test]
        [Property("GDD", "2.5, 2.6.1")]
        public void Ozellik_grubun_sekli_hucre_sayisiyla_ve_urettigi_tasla_tutarlidir(
            [ValueSource(nameof(Tohumlar))] int tohum)
        {
            var tahta = RastgeleTahta(tohum);
            var olaylar = new List<Olaylar.Olay>();

            HamleIsleyici.KaskadEslesmeleriniPatlat(HamleDenemesi.BaglamKur(tahta), olaylar);

            foreach (var patlama in olaylar.OfType<Olaylar.GrupPatladi>())
            {
                var grup = patlama.Grup;
                int beklenenHucre = grup.Sekil == Sekil.Duz3 ? 3
                    : grup.Sekil == Sekil.YatayDortlu || grup.Sekil == Sekil.DikeyDortlu || grup.Sekil == Sekil.Kare ? 4
                    : 5;
                Assert.That(grup.Hucreler, Has.Count.EqualTo(beklenenHucre));
                Assert.That(patlama.DogumHucresi.HasValue, Is.EqualTo(grup.Sekil != Sekil.Duz3));
                if (patlama.DogumHucresi.HasValue)
                {
                    Assert.That(grup.Hucreler, Does.Contain(patlama.DogumHucresi.Value));
                    var ozel = tahta[patlama.DogumHucresi.Value].Tas!;
                    Assert.That(ozel.Tur, Is.EqualTo(grup.Sekil.UretilenTur()));
                    Assert.That(ozel.CizgiYonu, Is.EqualTo(grup.Sekil.UretilenCizgiYonu()));
                }
            }

            Assert.That(
                olaylar.OfType<Olaylar.OzelTasDogdu>().Count(),
                Is.EqualTo(olaylar.OfType<Olaylar.GrupPatladi>().Count(o => o.Grup.Sekil != Sekil.Duz3)));
        }
    }
}
