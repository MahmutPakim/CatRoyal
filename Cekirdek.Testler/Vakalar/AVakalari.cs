using System.Linq;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Vakalar
{
    /// <summary>test_vakalari.md — A. Eşleşme tespiti ve özel taş oluşumu.</summary>
    [TestFixture]
    public class AVakalari
    {
        private static readonly AkBayraklari UzatmaKapali =
            AkBayraklari.Varsayilan.Ile(Ak.UzatmaAdimi, Varyant.B);

        [Test]
        [Category("A01")]
        [Property("GDD", "2.4.1, 2.4.3, 2.5.1, 2.7.6")]
        public void A01_Duz_uclu_ozel_tas_uretmez()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · K · ·
                3  K K M · ·
                4  · · · · ·", H(2, 3), H(3, 3));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.Duz3));
            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));
            Assert.That(d.DoganOzelTaslar, Is.Empty);

            // Yorum (1. aşama, karar 6): "(2,3)'e çıkan M yerinde kalır" = M patlamaz.
            // Altındaki (3,3) boşaldığı için düşme eklendiğinde M aşağı iner.
            Assert.That(d.PatlayanHucreler, Does.Not.Contain(H(2, 3)));
            Assert.That(d.Tahta[2, 3].Tas!.Renk, Is.EqualTo(Mavi));
        }

        [Test]
        [Category("A02")]
        [Property("GDD", "2.5.2, 2.5.10, 2.7.2, 2.7.3")]
        public void A02_Yatay_dortlu_dikey_cizgi_tasi_uretir_kaydirilan_tasin_indigi_hucrede()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · K · ·
                3  K K M K ·
                4  · · · · ·", H(2, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3), H(3, 4) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Cizgi));
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Dikey));
            Assert.That(d.Tahta[3, 3].Tas, Is.SameAs(dogan.Tas));
        }

        [Test]
        [Category("A03")]
        [Property("GDD", "2.5.3, 2.5.10, 2.7.2, 2.7.3")]
        public void A03_Dikey_dortlu_yatay_cizgi_tasi_uretir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · K · ·
                2  · · K · ·
                3  · K M · ·
                4  · · K · ·
                5  · · · · ·", H(3, 2), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(1, 3), H(2, 3), H(3, 3), H(4, 3) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Cizgi));
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Yatay));
            Assert.That(d.Tahta[3, 2].Tas!.Renk, Is.EqualTo(Mavi), "(3,2)'ye geçen M yerinde kalır.");
        }

        [Test]
        [Category("A04")]
        [Property("GDD", "2.5.4, 2.6.1, 2.6.2, 2.7.2, 2.7.3")]
        public void A04_Duz_besli_renk_kuresi_uretir_cizgi_adaylari_elenir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · K · ·
                3  K K M K K
                4  · · · · ·", H(2, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.Duz5));
            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(Enumerable.Range(1, 5).Select(t => H(3, t))));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.RenkKuresi));
        }

        [Test]
        [Category("A05")]
        [Property("GDD", "2.4.2, 2.5.8, 2.5.11, 2.7.2, 2.7.3")]
        public void A05_Kare_gudumlu_tas_uretir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · K K · ·
                3  · K M K ·
                4  · · · · ·", H(3, 4), H(3, 3));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.Kare));
            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(2, 2), H(2, 3), H(3, 2), H(3, 3) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Gudumlu));
            Assert.That(d.Tahta[3, 4].Tas!.Renk, Is.EqualTo(Mavi), "(3,4)'e geçen M yerinde kalır.");
        }

        [Test]
        [Category("A06")]
        [Property("GDD", "2.5.5, 2.7.2, 2.7.3")]
        public void A06_L_sekli_bomba_tasi_uretir_kosede()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  K · · · ·
                2  K · · · ·
                3  M K K · ·
                4  K · · · ·", H(4, 1), H(3, 1));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.L));
            Assert.That(
                d.PatlayanHucreler,
                Is.EquivalentTo(new[] { H(1, 1), H(2, 1), H(3, 1), H(3, 2), H(3, 3) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 1)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Bomba));
        }

        [Test]
        [Category("A07")]
        [Property("GDD", "2.5.6, 2.7.2, 2.7.3")]
        public void A07_T_sekli_bomba_tasi_uretir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · K · · ·
                3  K M K · ·
                4  · K · · ·
                5  · K · · ·", H(2, 2), H(3, 2));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.T));
            Assert.That(
                d.PatlayanHucreler,
                Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3), H(4, 2), H(5, 2) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 2)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Bomba));
        }

        [Test]
        [Category("A08")]
        [Property("GDD", "2.5.7")]
        public void A08_Arti_sekli_bomba_tasi_uretir_durum_testi()
        {
            var d = HamleDenemesi.KaskadAdimi(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · K · ·
                3  · K K K ·
                4  · · K · ·
                5  · · · · ·");

            var grup = d.Gruplar.Single();
            Assert.That(grup.Sekil, Is.EqualTo(Sekil.Arti));
            Assert.That(
                d.PatlayanHucreler,
                Is.EquivalentTo(new[] { H(2, 3), H(3, 2), H(3, 3), H(3, 4), H(4, 3) }));
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.Bomba));
        }

        private const string A09Tahtasi = @"
               1 2 3 4 5
            1  · · · · ·
            2  · · K · ·
            3  K K M K K
            4  · · K · ·
            5  · · K · ·";

        [Test]
        [Category("A09")]
        [Property("GDD", "2.6.1, 2.6.2")]
        public void A09_Besli_ile_dikey_uclu_kesisirse_yalniz_renk_kuresi_olusur()
        {
            var d = HamleDenemesi.Kaydir(A09Tahtasi, H(2, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().Sekil, Is.EqualTo(Sekil.Duz5));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.RenkKuresi));
        }

        [Test]
        [Category("A09")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 A")]
        [Property("Guven", "düşük (ek beklenti)")]
        public void A09_Ek_beklenti_AK10_A_artik_hucreler_gruba_eklenir_ve_patlar()
        {
            var d = HamleDenemesi.Kaydir(A09Tahtasi, H(2, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().UzatmaHucreleri, Is.EquivalentTo(new[] { H(4, 3), H(5, 3) }));
            Assert.That(d.PatlayanHucreler, Has.Count.EqualTo(7));
        }

        [Test]
        [Category("A09")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 B")]
        public void A09_AK10_B_artik_hucreler_yerinde_kalir()
        {
            var d = HamleDenemesi.Kaydir(A09Tahtasi, H(2, 3), H(3, 3), UzatmaKapali);

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(Enumerable.Range(1, 5).Select(t => H(3, t))));
            Assert.That(d.Tahta[4, 3].Tas!.Renk, Is.EqualTo(Kirmizi));
            Assert.That(d.Tahta[5, 3].Tas!.Renk, Is.EqualTo(Kirmizi));
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.RenkKuresi));
        }

        private const string A10Tahtasi = @"
               1 2 3 4 5
            1  · K · · ·
            2  · K · · ·
            3  K M K K ·
            4  · K · · ·";

        [Test]
        [Category("A10")]
        [Property("GDD", "2.6.1, 2.6.2, 2.6.4, 2.7.2, 2.7.3")]
        public void A10_L_T_den_ve_dortluden_once_gelir()
        {
            var d = HamleDenemesi.Kaydir(A10Tahtasi, H(4, 2), H(3, 2));

            var grup = d.Gruplar.Single();
            Assert.That(grup.Sekil, Is.EqualTo(Sekil.L));
            Assert.That(
                grup.Hucreler,
                Is.EquivalentTo(new[] { H(1, 2), H(2, 2), H(3, 2), H(3, 3), H(3, 4) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 2)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Bomba));
        }

        [Test]
        [Category("A10")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 A")]
        [Property("Guven", "düşük (ek beklenti)")]
        public void A10_Ek_beklenti_AK10_A_artik_hucre_gruba_eklenir()
        {
            var d = HamleDenemesi.Kaydir(A10Tahtasi, H(4, 2), H(3, 2));

            Assert.That(d.Gruplar.Single().UzatmaHucreleri, Is.EquivalentTo(new[] { H(3, 1) }));
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.Bomba));
        }

        [Test]
        [Category("A10")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 B")]
        public void A10_AK10_B_artik_hucre_yerinde_kalir()
        {
            var d = HamleDenemesi.Kaydir(A10Tahtasi, H(4, 2), H(3, 2), UzatmaKapali);

            Assert.That(d.PatlayanHucreler, Does.Not.Contain(H(3, 1)));
            Assert.That(d.Tahta[3, 1].Tas!.Renk, Is.EqualTo(Kirmizi));
        }

        private const string A11Tahtasi = @"
               1 2 3 4 5
            1  · · · · ·
            2  · K K · ·
            3  · K M K ·
            4  · · K · ·";

        [Test]
        [Category("A11")]
        [Property("GDD", "2.5.11, 2.6.1, 2.6.2")]
        public void A11_Kare_kendisiyle_cakisan_duz_ucluden_once_gelir()
        {
            var d = HamleDenemesi.Kaydir(A11Tahtasi, H(4, 3), H(3, 3));

            var grup = d.Gruplar.Single();
            Assert.That(grup.Sekil, Is.EqualTo(Sekil.Kare));
            Assert.That(grup.Hucreler, Is.EquivalentTo(new[] { H(2, 2), H(2, 3), H(3, 2), H(3, 3) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Gudumlu));
        }

        [Test]
        [Category("A11")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 A")]
        [Property("Guven", "düşük (ek beklenti)")]
        public void A11_Ek_beklenti_AK10_A_artik_hucre_gruba_eklenir()
        {
            var d = HamleDenemesi.Kaydir(A11Tahtasi, H(4, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().UzatmaHucreleri, Is.EquivalentTo(new[] { H(3, 4) }));
            Assert.That(d.DoganOzelTaslar, Has.Count.EqualTo(1));
        }

        [Test]
        [Category("A11")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 B")]
        public void A11_AK10_B_artik_hucre_yerinde_kalir()
        {
            var d = HamleDenemesi.Kaydir(A11Tahtasi, H(4, 3), H(3, 3), UzatmaKapali);

            Assert.That(d.PatlayanHucreler, Does.Not.Contain(H(3, 4)));
            Assert.That(d.Tahta[3, 4].Tas!.Renk, Is.EqualTo(Kirmizi));
        }

        [Test]
        [Category("A12")]
        [Property("GDD", "2.5.3, 2.6.3, 2.7.2")]
        public void A12_Ortusmeyen_iki_eslesme_tek_hamlede_iki_ozel_tas_uretir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · M K ·
                2  · · M K ·
                3  · · K M ·
                4  · · M K ·", H(3, 3), H(3, 4));

            Assert.That(d.Gruplar.Select(g => g.Sekil), Is.All.EqualTo(Sekil.DikeyDortlu));
            Assert.That(d.Gruplar, Has.Count.EqualTo(2));
            Assert.That(
                d.PatlayanHucreler,
                Is.EquivalentTo(Enumerable.Range(1, 4).SelectMany(s => new[] { H(s, 3), H(s, 4) })));
            Assert.That(d.DoganOzelTaslar.Select(o => o.Konum), Is.EquivalentTo(new[] { H(3, 3), H(3, 4) }));
            Assert.That(d.DoganOzelTaslar.Select(o => o.Tas.Tur), Is.All.EqualTo(TasTuru.Cizgi));
            Assert.That(d.DoganOzelTaslar.Select(o => o.Tas.CizgiYonu), Is.All.EqualTo(Yon.Yatay));
        }

        [Test]
        [Category("A13")]
        [Property("GDD", "2.7.2")]
        public void A13_Eslesmeyi_karsi_tas_kurarsa_ozel_tas_cikilan_hucrede_dogar()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  M M K M ·
                3  · · M · ·
                4  · · · · ·", H(2, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(2, 1), H(2, 2), H(2, 3), H(2, 4) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(2, 3)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Cizgi));
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Dikey));
            Assert.That(d.Tahta[3, 3].Tas!.Renk, Is.EqualTo(Kirmizi), "(3,3)'e inen K eşleşmeye girmez.");
        }

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B, Description = "AK-01 B ile bu tahtada sonuç aynıdır, (4,3).")]
        [Category("A14")]
        [Property("GDD", "2.7.4, 2.7.5, 5.5.1, 5.5.2, 5.5.3")]
        [Property("AK", "AK-01")]
        public void A14_Kaskadla_olusan_dortlude_dogum_hucresi(Varyant ak01)
        {
            // Dolgu nötrdür: vaka yalnızca dört K'nin grubunu izler; ek kaskad çıkmaz.
            // Zorluk üreteci etkisiz ağırlıkla çalışır (GDD 2.7.7).
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · · K · ·
                2  · · Y · ·
                3  · · Y · ·
                4  K K M K ·
                5  · · Y · ·", H(5, 3), H(4, 3), AkBayraklari.Varsayilan.Ile(Ak.KaskadDogumHucresi, ak01));

            var patlamalar = d.Olaylar.OfType<Olaylar.GrupPatladi>().ToList();
            Assert.That(patlamalar, Has.Count.EqualTo(2));

            // 1. Sütun 3'te üç Y patlar; özel taş oluşmaz.
            Assert.That(patlamalar[0].Kaskad, Is.False);
            Assert.That(patlamalar[0].Grup.Sekil, Is.EqualTo(Sekil.Duz3));
            Assert.That(patlamalar[0].Grup.Hucreler, Is.EquivalentTo(new[] { H(2, 3), H(3, 3), H(4, 3) }));

            // 2. (1,3)'teki K, (4,3)'e düşer; satır 4'te dört K kaskadla eşleşir.
            var dusen = d.Dusmeler.Single(x => x.Nereden == H(1, 3));
            Assert.That(dusen.Nereye, Is.EqualTo(H(4, 3)));
            Assert.That(patlamalar[1].Kaskad, Is.True);
            Assert.That(patlamalar[1].Grup.Sekil, Is.EqualTo(Sekil.YatayDortlu));
            Assert.That(
                patlamalar[1].Grup.Hucreler,
                Is.EquivalentTo(new[] { H(4, 1), H(4, 2), H(4, 3), H(4, 4) }));

            // 3. Dikey çizgi taşı, gruba en son yerine oturan taşın hücresinde doğar.
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Cizgi));
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Dikey));
            Assert.That(dogan.Konum, Is.EqualTo(H(4, 3)));
            Assert.That(patlamalar[1].Grup.Hucreler, Does.Contain(dogan.Konum));
            Assert.That(d.Tahta[dogan.Konum].Kilitli, Is.False);

            // Hamle sayacının yalnızca 1 azalması 6. aşamada doğrulanır; burada tek oyuncu hamlesi vardır.
            Assert.That(d.Olaylar.OfType<Olaylar.TaslarYerDegistirdi>().Count(), Is.EqualTo(1));
        }

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B)]
        [Category("A14")]
        [Property("GDD", "2.7.4")]
        [Property("AK", "AK-01")]
        public void A14_Kismi_dogum_hucresi_gruba_en_son_oturan_tasin_hucresidir(Varyant ak01)
        {
            // A14'ün düşmeden sonraki durumu elle kurulur: (4,3)'teki K gruba en son oturan taştır.
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · · ·
                3  · · · · ·
                4  K K K K ·
                5  · · M · ·");
            test.Tahta[4, 3].Tas!.OturmaAdimi = 3;

            var d = HamleDenemesi.KaskadAdimi(
                test, AkBayraklari.Varsayilan.Ile(Ak.KaskadDogumHucresi, ak01));

            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(4, 3)), "AK-01 B ile de sonuç aynıdır.");
            Assert.That(dogan.Tas.CizgiYonu, Is.EqualTo(Yon.Dikey));
        }

        private const string A15Tahtasi = @"
               1 2 3 4 5
            1  · · · · ·
            2  · K K K ·
            3  · K K K ·
            4  · · · · ·";

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B, Description = "AK-06 B ile sonuç aynıdır.")]
        [Category("A15")]
        [Property("GDD", "2.5.8, 2.6.1, 2.6.2, 2.6.5")]
        [Property("AK", "AK-06")]
        public void A15_Esit_oncelikli_iki_aday_cakisirsa_yalniz_biri_kabul_edilir(Varyant ak06)
        {
            var d = HamleDenemesi.KaskadAdimi(
                A15Tahtasi, AkBayraklari.Varsayilan.Ile(Ak.EsitOncelikSecimi, ak06));

            var grup = d.Gruplar.Single();
            Assert.That(grup.Sekil, Is.EqualTo(Sekil.Kare));
            Assert.That(grup.Hucreler, Is.EquivalentTo(new[] { H(2, 2), H(2, 3), H(3, 2), H(3, 3) }));
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.Gudumlu));
        }

        [Test]
        [Category("A15")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 A")]
        [Property("Guven", "düşük (ek beklenti)")]
        public void A15_Ek_beklenti_AK10_A_karenin_disindaki_iki_tas_gruba_eklenir()
        {
            var d = HamleDenemesi.KaskadAdimi(A15Tahtasi);

            Assert.That(d.Gruplar.Single().UzatmaHucreleri, Is.EquivalentTo(new[] { H(2, 4), H(3, 4) }));
            Assert.That(d.DoganOzelTaslar, Has.Count.EqualTo(1));
        }

        [Test]
        [Category("A15")]
        [Property("GDD", "2.6.6")]
        [Property("AK", "AK-10 B")]
        public void A15_AK10_B_karenin_disindaki_iki_tas_yerinde_kalir()
        {
            var d = HamleDenemesi.KaskadAdimi(A15Tahtasi, UzatmaKapali);

            Assert.That(d.PatlayanHucreler, Has.Count.EqualTo(4));
            Assert.That(d.Tahta[2, 4].Tas!.Renk, Is.EqualTo(Kirmizi));
            Assert.That(d.Tahta[3, 4].Tas!.Renk, Is.EqualTo(Kirmizi));
            Assert.That(d.DoganOzelTaslar, Has.Count.EqualTo(1));
        }
    }
}
