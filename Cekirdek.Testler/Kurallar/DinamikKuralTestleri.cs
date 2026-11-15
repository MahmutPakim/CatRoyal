using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Testler.Vakalar;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>GDD 5'in test_vakalari.md'de doğrudan vakası olmayan kuralları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class DinamikKuralTestleri
    {
        private static readonly int[] Tohumlar = Enumerable.Range(1, 40).ToArray();

        [Test]
        [Property("GDD", "5.1.7")]
        public void Oturma_adimi_dusmeye_baslanan_adim_ile_dusulen_hucre_sayisinin_toplamidir()
        {
            var d = HamleDenemesi.Dusur(@"
                   1 2 3
                1  · a ·
                2  · _ ·
                3  · _ ·
                4  · _ ·
                5  · · ·");

            var dusen = d.Dusmeler.Single();
            Assert.That(dusen.Tas, Is.SameAs(d.Test.Izlenen('a')));
            Assert.That(dusen.OturmaAdimi - dusen.BaslangicAdimi, Is.EqualTo(3));

            // Girişten her adımda bir taş çıkar; sütun bir bütün olarak iner ve hepsi aynı adımda oturur.
            Assert.That(d.Girisler, Has.Count.EqualTo(3));
            Assert.That(d.Girisler.Select(g => g.GirisAdimi - dusen.BaslangicAdimi), Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(d.Girisler.Select(g => g.OturmaAdimi), Is.All.EqualTo(dusen.OturmaAdimi));
            Assert.That(d.Baglam.Saat, Is.EqualTo(dusen.OturmaAdimi));
        }

        [Test]
        [Property("GDD", "5.1.5, 6.2.4")]
        public void Kilitli_tas_dusmez_ve_ustundekilerin_inmesini_keser()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  · a ·
                2  · b ·
                3  · _ ·");
            test.Tahta[2, 2].Kilitli = true;
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta);

            DusmeIsleyici.Durult(baglam, new List<Olay>());

            Assert.That(test.IzlenenKonumu('b'), Is.EqualTo(H(2, 2)));
            Assert.That(test.IzlenenKonumu('a'), Is.EqualTo(H(1, 2)));
            Assert.That(test.Tahta[3, 2].Tas, Is.Null);
        }

        [Test]
        [Property("GDD", "5.2.4")]
        public void Capraz_kaymaya_kapatilmis_hucreye_capraz_kayma_yapilmaz()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · c · d ·
                2  · a 1 b ·
                3  · · _ · ·");
            test.Tahta[3, 3].CaprazKaymaKapali = true;
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta, capraz: true);
            var olaylar = new List<Olay>();

            DusmeIsleyici.Durult(baglam, olaylar);

            Assert.That(olaylar, Is.Empty);
            Assert.That(test.Tahta[3, 3].Tas, Is.Null);
        }

        [Test]
        [Property("GDD", "5.3.4")]
        public void Sutun_icin_onceden_tanimli_tas_rastgele_renkten_once_gelir()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  · _ ·
                2  · _ ·
                3  · _ ·");
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta, notrDolgu: false);
            var dolgu = new RastgeleDolgu();
            dolgu.OnTanimliEkle(2, tahta => tahta.OzelTasUret(TasTuru.Bomba));
            dolgu.OnTanimliEkle(2, tahta => tahta.NormalTasUret(Sari));
            baglam.Dolgu = dolgu;

            DusmeIsleyici.Durult(baglam, new List<Olay>());

            // Girişten ilk çıkan taş en alta iner.
            Assert.That(test.Tahta[3, 2].Tas!.Tur, Is.EqualTo(TasTuru.Bomba));
            Assert.That(test.Tahta[2, 2].Tas!.Renk, Is.EqualTo(Sari));
            Assert.That(baglam.Renkler, Does.Contain(test.Tahta[1, 2].Tas!.Renk), "Sıra bitince rastgele renk.");
        }

        [Test]
        [Property("GDD", "5.3.5")]
        public void Rastgele_dolgu_rengi_renk_oranlarina_gore_agirlikli_secilir()
        {
            var tahta = new Tahta(1, 1);
            var baglam = HamleDenemesi.BaglamKur(tahta, tohum: 7);
            baglam.Renkler = new[] { Kirmizi, Mavi, Yesil };
            baglam.RenkAgirliklari = new[] { 3.0, 1.0, 0.0 };

            var sayim = new Dictionary<int, int> { { Kirmizi, 0 }, { Mavi, 0 }, { Yesil, 0 } };
            for (int i = 0; i < 4000; i++)
            {
                sayim[RenkSecici.Sec(baglam, baglam.Renkler)]++;
            }

            Assert.That(sayim[Yesil], Is.EqualTo(0));
            Assert.That(sayim[Kirmizi] / (double)sayim[Mavi], Is.EqualTo(3.0).Within(0.4));
        }

        [Test]
        [Property("GDD", "5.4.6")]
        public void Izinli_renk_kalmazsa_kisit_gevsetilir_ve_gevsetme_kaydedilir()
        {
            var tahta = new Tahta(4, 4);
            var baglam = HamleDenemesi.BaglamKur(tahta);
            baglam.Renkler = new[] { Kirmizi };

            var sonuc = BaslangicTahtasi.Uret(baglam, tahta.TumKonumlar().ToList());

            Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas!.Renk), Is.All.EqualTo(Kirmizi));
            Assert.That(sonuc.KareKisitiGevsetildi, Is.True);
            Assert.That(sonuc.UcluKisitiGevsetildi, Is.True);
        }

        [Test]
        [Property("GDD", "5.6.3")]
        public void Karistirmada_yalniz_serbest_normal_taslar_yer_degistirir()
        {
            for (int tohum = 1; tohum <= 20; tohum++)
            {
                var test = TahtaKurucu.Kur(@"
                       1 2 3 4
                    1  K M 2 Y
                    2  M K Y M
                    3  X Y M K
                    4  S K Y M");
                var tahta = test.Tahta;
                tahta[4, 1].Kilitli = true;
                var blok = tahta[1, 3].Tas;
                var kilitli = tahta[4, 1].Tas;
                var baglam = HamleDenemesi.BaglamKur(tahta, tohum: tohum);
                baglam.Renkler = new[] { Kirmizi, Mavi, Yesil, Sari };

                var sonuc = Karistirici.Karistir(baglam, new List<Olay>());

                Assert.That(sonuc, Is.EqualTo(KaristirmaSonucu.Karistirildi));
                Assert.That(tahta[1, 3].Tas, Is.SameAs(blok));
                Assert.That(tahta[4, 1].Tas, Is.SameAs(kilitli));
                Assert.That(tahta[3, 1].Tas, Is.Null);
                Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False);
                Assert.That(OlasiHamleBulucu.Var(tahta), Is.True);
            }
        }

        [Test]
        [Property("GDD", "5.6.1, 5.7")]
        public void Tam_akista_tahta_durulunca_olasi_hamle_yoksa_karistirma_calisir()
        {
            // Hamleden sonra kalan taşlarla olası hamle yok; karıştırma K ve M'leri yeniden dizer.
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  K M K M Y
                2  M K M K S
                3  Y Y S Y K
                4  K M Y M M");
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta, notrDolgu: true);
            var olaylar = new List<Olay>();

            var sonuc = HamleIsleyici.Kaydir(baglam, H(4, 3), H(3, 3), olaylar);

            Assert.That(sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            var karistirma = olaylar.OfType<TahtaKaristirildi>().SingleOrDefault();
            if (karistirma != null)
            {
                Assert.That(olaylar.Last(), Is.SameAs(karistirma), "Karıştırma, tahta durulduktan sonra gelir.");
                Assert.That(OlasiHamleBulucu.HazirEslesmeVar(test.Tahta), Is.False);
            }

            Assert.That(
                OlasiHamleBulucu.Var(test.Tahta) || karistirma?.Sonuc == KaristirmaSonucu.BastanBaslatilmali,
                Is.True);
        }

        [Test]
        [Property("GDD", "3.6.4")]
        [Property("AK", "AK-14 B")]
        public void AK14_B_ikinci_renk_dusme_ve_kaskad_durulduktan_sonra_secilir()
        {
            var d = HamleDenemesi.TamKaydir(
                TahtaKurucu.Kur(BVakalari.B07Tahtasi),
                H(3, 3),
                H(3, 4),
                AkBayraklari.Varsayilan.Ile(Ak.CiftRenkGecisi, Varyant.B),
                ciftRenk: true);
            var olaylar = d.Olaylar.ToList();

            var secimler = olaylar.Select((o, i) => (o, i)).Where(x => x.o is RenkSecildi).Select(x => x.i).ToList();
            Assert.That(secimler, Has.Count.EqualTo(2));
            Assert.That(
                olaylar.Skip(secimler[0]).Take(secimler[1] - secimler[0]).OfType<TasGirdi>(),
                Is.Not.Empty,
                "İki renk arasında tahta durulur.");
            Assert.That(d.Olaylar.OfType<RenkKuresiTukendi>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "3.6.4")]
        [Property("AK", "AK-14 A")]
        public void AK14_A_ikinci_renk_durulma_beklenmeden_secilir()
        {
            var d = HamleDenemesi.TamKaydir(
                TahtaKurucu.Kur(BVakalari.B07Tahtasi), H(3, 3), H(3, 4), ciftRenk: true);
            var olaylar = d.Olaylar.ToList();

            int ilk = olaylar.FindIndex(o => o is RenkSecildi);
            int ikinci = olaylar.FindLastIndex(o => o is RenkSecildi);
            Assert.That(ikinci, Is.GreaterThan(ilk));
            Assert.That(olaylar.Skip(ilk).Take(ikinci - ilk).OfType<TasGirdi>(), Is.Empty);
            Assert.That(olaylar.Skip(ilk).Take(ikinci - ilk).OfType<TasDustu>(), Is.Empty);
        }

        // ------------------------------------------------------------------ Özellik testleri

        private static string RastgeleTahtadaIlkHamle(int tohum)
        {
            var tahta = new Tahta(8, 7);
            var baglam = HamleDenemesi.TamBaglamKur(tahta, tohum: tohum, capraz: tohum % 2 == 0, notrDolgu: false);
            BaslangicTahtasi.Uret(baglam, tahta.TumKonumlar().ToList());
            var hamle = OlasiHamleBulucu.Bul(tahta).First(h => h.Tur == OlasiHamleTuru.Eslesme);

            var olaylar = new List<Olay>();
            var sonuc = HamleIsleyici.Kaydir(baglam, hamle.Cikilan, hamle.Gidilen, olaylar);
            Assert.That(sonuc, Is.EqualTo(HamleSonucu.Gecerli));

            string ozet = string.Join("|", tahta.TumKonumlar().Select(k =>
                tahta[k].Tas == null ? "_" : tahta[k].Tas!.Tur + ":" + tahta[k].Tas!.Renk + ":" + tahta[k].Tas!.CizgiYonu))
                + "#" + olaylar.Count;
            return ozet + "#" + (OlasiHamleBulucu.HazirEslesmeVar(tahta) ? "eslesme" : "temiz")
                + "#" + tahta.TumKonumlar().Count(k => tahta[k].Tas == null);
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "5.5.1, 5.7")]
        public void Ozellik_tam_hamleden_sonra_tahta_durulmustur(int tohum)
        {
            string ozet = RastgeleTahtadaIlkHamle(tohum);
            var parcalar = ozet.Split('#');

            Assert.That(parcalar[2], Is.EqualTo("temiz"), "Kaskad döngüsü hazır eşleşme bırakmaz.");
            Assert.That(parcalar[3], Is.EqualTo("0"), "Her sütunun girişi var: boş hücre kalmaz.");
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "0.4")]
        public void Ozellik_ayni_tohum_ayni_tahtayi_ve_ayni_olay_sayisini_uretir(int tohum)
        {
            Assert.That(
                RastgeleTahtadaIlkHamle(tohum),
                Is.EqualTo(RastgeleTahtadaIlkHamle(tohum)));
        }
    }
}
