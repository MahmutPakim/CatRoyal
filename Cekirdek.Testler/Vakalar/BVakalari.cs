using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Vakalar
{
    /// <summary>test_vakalari.md — B. Özel taşların tek başına etkisi.</summary>
    [TestFixture]
    public class BVakalari
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };

        private static IEnumerable<Konum> Satir(int satir, int sutunSayisi) =>
            Enumerable.Range(1, sutunSayisi).Select(t => H(satir, t));

        private static IEnumerable<Konum> Sutun(int sutun, int satirSayisi) =>
            Enumerable.Range(1, satirSayisi).Select(s => H(s, sutun));

        private static IEnumerable<Konum> Kare(int satir1, int satir2, int sutun1, int sutun2) =>
            from s in Enumerable.Range(satir1, satir2 - satir1 + 1)
            from t in Enumerable.Range(sutun1, sutun2 - sutun1 + 1)
            select H(s, t);

        [Test]
        [Category("B01")]
        [Property("GDD", "3.2.1, 3.2.2, 3.1.1, 6.1.1, 2.3.2–2.3.4")]
        public void B01_Yatay_cizgi_tasi_satirini_temizler_engel_yolu_kesmez()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · · ·
                3  · - K · 2
                4  · · · · ·
                5  · · · · ·", H(3, 2), H(3, 3));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli), "Hamlede özel taş var.");
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Satir(3, 5)));

            var blok = d.Tahta[3, 5].Tas!;
            Assert.That(blok.Tur, Is.EqualTo(TasTuru.Engel));
            Assert.That(blok.Katman, Is.EqualTo(1));

            // Çizgi taşı iki yöne ilerler: sol kanat (3,1)'e, sağ kanat bloğa kadar gider.
            Assert.That(d.IlkVurusAdimi(H(3, 2)) - d.IlkVurusAdimi(H(3, 3)), Is.EqualTo(1));
            Assert.That(d.IlkVurusAdimi(H(3, 5)) - d.IlkVurusAdimi(H(3, 3)), Is.EqualTo(2));
        }

        [Test]
        [Category("B02")]
        [Property("GDD", "3.2.1, 3.2.2, 6.1.1")]
        public void B02_Dikey_cizgi_tasi_sutununu_temizler_blogu_kirar_ve_devam_eder()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · | · ·
                3  · · K · ·
                4  · · 1 · ·
                5  · · K · ·", H(2, 3), H(3, 3));

            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Sutun(3, 5)));
            var hasar = d.Olaylar.OfType<EngelHasarAldi>().Single();
            Assert.That(hasar.Konum, Is.EqualTo(H(4, 3)));
            Assert.That(hasar.Kirildi, Is.True);
            Assert.That(d.PatlayanTaslar.Select(p => p.Konum), Does.Contain(H(5, 3)));
        }

        [Test]
        [Category("B03")]
        [Property("GDD", "3.3.1, 3.3.2, 3.3.3, 3.1.1, 2.3.5, 7.2.1")]
        public void B03_Bomba_tasi_dokunmayla_5x5_kare_patlatir()
        {
            var d = HamleDenemesi.Dokun(NotrCizim(7, 7, (4, 4, 'B')), H(4, 4));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Kare(2, 6, 2, 6)));
            Assert.That(d.Vuruslar, Has.Count.EqualTo(25), "Her hücre bir vuruş alır.");

            // İçten dışa: merkez, 8 hücrelik iç halka, 16 hücrelik dış halka.
            long merkez = d.IlkVurusAdimi(H(4, 4));
            Assert.That(d.Vuruslar.Count(v => v.Adim == merkez), Is.EqualTo(1));
            Assert.That(d.Vuruslar.Count(v => v.Adim == merkez + 1), Is.EqualTo(8));
            Assert.That(d.Vuruslar.Count(v => v.Adim == merkez + 2), Is.EqualTo(16));
            Assert.That(d.IlkVurusAdimi(H(2, 2)), Is.EqualTo(merkez + 2), "Köşe hücresi alana dahildir; şekil karedir.");

            // Hamle sayacının azalması F03'te (6. aşama) doğrulanır.
        }

        [Test]
        [Category("B04")]
        [Property("GDD", "3.3.1, 3.3.2, 3.3.3, 3.1.1, 2.3.5")]
        public void B04_Kosedeki_bomba_tasi_tahta_disina_tasmaz()
        {
            var d = HamleDenemesi.Dokun(NotrCizim(5, 5, (1, 1, 'B')), H(1, 1));

            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Kare(1, 3, 1, 3)));
            Assert.That(d.Vuruslar, Has.Count.EqualTo(9));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("B05")]
        [Property("GDD", "3.4.1, 3.4.3, 3.4.4, 3.4.5, 3.1.1, 2.3.5")]
        [Property("AK", "AK-05")]
        public void B05_Gudumlu_tas_arti_alanini_patlatir_sonra_tek_hedefe_ucar(int tohum)
        {
            foreach (var ak05 in new[] { Varyant.A, Varyant.B })
            {
                var d = HamleDenemesi.Dokun(@"
                       1 2 3 4 5
                    1  · · · · ·
                    2  · · · · ·
                    3  · · G · ·
                    4  · · · · ·
                    5  · · · · 1",
                    H(3, 3),
                    AkBayraklari.Varsayilan.Ile(Ak.GudumluHedefSecimi, ak05),
                    tohum);

                var inis = d.Inisler.Single();
                var artiVuruslari = d.Vuruslar.Where(v => v.Adim < inis.Adim).Select(v => v.Konum);
                Assert.That(
                    artiVuruslari,
                    Is.EquivalentTo(new[] { H(3, 3), H(2, 3), H(4, 3), H(3, 2), H(3, 4) }),
                    "Önce kendi hücresi ve dört dik komşusu; çaprazlar patlamaz.");
                Assert.That(d.IlkVurusAdimi(H(3, 3)), Is.LessThan(d.IlkVurusAdimi(H(2, 3))));

                // Tek engel olan bloğun puanı normal taşlardan yüksektir; hedef öğe olmadığı için B'de de A uygulanır.
                Assert.That(d.Kalkislar, Has.Count.EqualTo(1));
                Assert.That(inis.Hedef, Is.EqualTo(H(5, 5)));
                Assert.That(d.Olaylar.OfType<EngelHasarAldi>().Single().Kirildi, Is.True);
                Assert.That(d.Vuruslar, Has.Count.EqualTo(6));
            }
        }

        [Test]
        [Category("B06")]
        [Property("GDD", "3.5.1, 3.5.3")]
        public void B06_Renk_kuresini_normal_tasla_yer_degistirmek_o_rengin_butun_taslarini_temizler()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  K · · · ·
                2  · · · K ·
                3  · · R K ·
                4  · K · · ·
                5  · · · · K", H(3, 3), H(3, 4));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Kirmizi));
            Assert.That(
                d.PatlayanTaslar.Select(p => p.Konum),
                Is.EquivalentTo(new[] { H(1, 1), H(2, 4), H(3, 3), H(4, 2), H(5, 5) }),
                "(3,3)'e geçen K de dahil.");
            Assert.That(d.PatlayanTaslar.Select(p => p.Tas.Renk), Is.All.EqualTo(Kirmizi));
            Assert.That(d.Olaylar.OfType<RenkKuresiTukendi>().Single().Konum, Is.EqualTo(H(3, 4)));
            Assert.That(d.Tahta[3, 4].Tas, Is.Null, "Renk küresi tükenir.");
        }

        public const string B07Tahtasi = @"
               1 2 3 4 5
            1  M K M Y S
            2  K M S M K
            3  M Y R K M
            4  S K M Y S
            5  Y M K M Y";

        [Test]
        [Category("B07")]
        [Property("GDD", "3.5.2, 3.5.3, 7.2.1")]
        public void B07_Renk_kuresine_dokunmak_en_yaygin_rengi_hedefler()
        {
            var d = HamleDenemesi.Dokun(B07Tahtasi, H(3, 3));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Mavi));
            Assert.That(d.PatlayanTaslar, Has.Count.EqualTo(9));
            Assert.That(d.PatlayanTaslar.Select(p => p.Tas.Renk), Is.All.EqualTo(Mavi));
            Assert.That(
                d.Tahta.TumKonumlar().Count(k => d.Tahta[k].Tas != null),
                Is.EqualTo(6 + 5 + 4),
                "K, Y ve S'ye dokunulmaz.");
        }

        [Test]
        [Category("B08")]
        [Property("GDD", "3.1.2, 3.2.1, 3.2.2, 3.3.1, 3.3.3")]
        public void B08_Cizgi_tasi_yolundaki_bomba_tasini_tetikler()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · | · ·
                2  · · K · ·
                3  · · · · ·
                4  · · · · ·
                5  · · B · ·
                6  · · · · ·
                7  · · · · ·", H(1, 3), H(2, 3));

            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Sutun(3, 7).Concat(Kare(3, 7, 1, 5)).Distinct()));

            var bomba = d.Etkinlesenler.Single(e => e.Tas.Tur == TasTuru.Bomba);
            Assert.That(bomba.Tetik, Is.EqualTo(Tetik.Zincir), "Aynı bomba taşı ikinci kez tetiklenmez.");
            Assert.That(bomba.Konum, Is.EqualTo(H(5, 3)));

            // Bomba, çizgi etkisi (5,3)'e vardığı adımda kendi etkisini başlatır.
            Assert.That(bomba.Adim, Is.EqualTo(d.IlkVurusAdimi(H(5, 3))));
            Assert.That(d.IlkVurusAdimi(H(5, 1)), Is.EqualTo(bomba.Adim + 2));
        }

        public const string B09Tahtasi = @"
               1 2 3 4 5
            1  M K M Y M
            2  K M Y M K
            3  - Y K S R
            4  M S M K M
            5  Y M K M Y";

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B)]
        [Category("B09")]
        [Property("GDD", "3.5.2, 3.5.3, 3.1.2")]
        [Property("AK", "AK-09")]
        public void B09_Baska_patlamayla_vurulan_renk_kuresi_en_yaygin_rengi_hedefler(Varyant ak09)
        {
            var d = HamleDenemesi.Kaydir(
                B09Tahtasi, H(3, 1), H(3, 2), AkBayraklari.Varsayilan.Ile(Ak.RenkKuresiZincirSecimi, ak09));

            // Çizgi taşı satır 3'ü temizler ve (3,5)'teki küreyi vurur; küre hemen etkinleşmez.
            Assert.That(d.VurulanHucreler.Where(k => k.Satir == 3), Is.EquivalentTo(Satir(3, 5)));
            var tetik = d.Olaylar.OfType<RenkKuresiTetiklendi>().Single();
            Assert.That(tetik.Konum, Is.EqualTo(H(3, 5)));

            var secim = d.SecilenRenkler.Single();
            Assert.That(secim.Renk, Is.EqualTo(Mavi), "Satır 3 dışındaki dağılım: M 10, K 5, Y 4, S 1.");
            Assert.That(d.PatlayanTaslar.Count(p => p.Tur == VurusTuru.RenkIsini), Is.EqualTo(10));

            // Işınlar çizgi taşının dalgası tamamlandıktan sonra gönderilir.
            long sonCizgiVurusu = d.Vuruslar.Where(v => v.Tur == VurusTuru.Cizgi).Max(v => v.Adim);
            Assert.That(secim.Adim, Is.GreaterThan(sonCizgiVurusu));
            Assert.That(d.Tahta.TumKonumlar().Select(k => d.Tahta[k].Tas?.Tur), Has.None.EqualTo(TasTuru.RenkKuresi));
        }

        [Test]
        [Category("B09")]
        [Property("GDD", "0.4")]
        public void B09_Sonuc_sunum_parametrelerinin_degerinden_bagimsizdir()
        {
            var sonuclar = new List<string>();
            foreach (string bekleme in new[] { "0.1", "0.8" })
            {
                var test = TahtaKurucu.Kur(B09Tahtasi);
                var parametreler = ProjeDosyalari.Parametreler
                    .Degistir("sunum_renk_kuresi_tetik_beklemesi", bekleme)
                    .Degistir("sunum_cizgi_ilerleme_hizi", bekleme == "0.1" ? "10" : "40");
                var baglam = HamleDenemesi.BaglamKur(test.Tahta, parametreler: parametreler);
                var olaylar = new List<Olay>();

                HamleIsleyici.Kaydir(baglam, H(3, 1), H(3, 2), olaylar, HamleKapsami.YalnizPatlama);

                sonuclar.Add(string.Join(
                    "|",
                    olaylar.OfType<AdimliOlay>().Select(o => o.GetType().Name + "@" + o.Adim)
                        .Concat(olaylar.OfType<TasPatladi>().Select(p => p.Konum.ToString()))));
            }

            Assert.That(sonuclar[0], Is.EqualTo(sonuclar[1]));
        }

        /// <summary>Bir olay anında tahtada bulunan normal taşlar: başlangıçtakiler ve girenler, eksi patlayanlar.</summary>
        private static List<Tas> OAndakiNormalTaslar(HamleDenemesi d, List<Tas> baslangictakiler, int olayIndisi)
        {
            var once = d.Olaylar.Take(olayIndisi).ToList();
            var patlayan = new HashSet<int>(once.OfType<TasPatladi>().Select(p => p.Tas.Kimlik)
                .Concat(once.OfType<GrupPatladi>().SelectMany(g => g.PatlayanTasKimlikleri)));
            return baslangictakiler
                .Concat(once.OfType<TasGirdi>().Select(g => g.Tas))
                .Where(t => t.Tur == TasTuru.Normal && !patlayan.Contains(t.Kimlik))
                .ToList();
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("B09")]
        [Property("GDD", "3.5.2, 3.5.3")]
        [Property("AK", "AK-08, AK-09 A")]
        public void B09_AK09_A_renk_sayimi_tahta_durulduktan_sonra_yapilir_yeni_taslar_sayima_girer(int tohum)
        {
            var test = TahtaKurucu.Kur(B09Tahtasi);
            var baslangictakiler = test.Tahta.TumKonumlar().Select(k => test.Tahta[k].Tas!).ToList();

            var d = HamleDenemesi.TamKaydir(test, H(3, 1), H(3, 2), tohum: tohum, notrDolgu: false);
            var olaylar = d.Olaylar.ToList();

            int tetik = olaylar.FindIndex(o => o is RenkKuresiTetiklendi);
            int secimIndisi = olaylar.FindIndex(o => o is RenkSecildi);
            var secim = (RenkSecildi)olaylar[secimIndisi];

            // Çizgi taşının dalgası tamamlanır ve satır 3 düşme ve doldurmayla durulur; küre ancak sonra sayar.
            var arada = olaylar.Skip(tetik + 1).Take(secimIndisi - tetik - 1).ToList();
            Assert.That(arada.OfType<TasGirdi>().Count(), Is.GreaterThanOrEqualTo(4), "Satır 3 yeniden dolar.");
            Assert.That(arada.OfType<GrupPatladi>(), Is.Empty, "Sayım kaskaddan önce, durulmanın hemen ardından yapılır.");

            // Hedef renk, durulmuş tahtadaki en yaygın renktir; satır 3'e düşüp oturan yeni taşlar sayıma girer.
            var oAnki = OAndakiNormalTaslar(d, baslangictakiler, secimIndisi);
            var sayim = oAnki.GroupBy(t => t.Renk).ToDictionary(g => g.Key, g => g.Count());
            Assert.That(sayim[secim.Renk], Is.EqualTo(sayim.Values.Max()));
            Assert.That(secim.Renk, Is.EqualTo(Mavi), "Satır 3 dışında M 10, K 5: satır 3 yeniden dolsa bile hedef M.");

            // Seçilen rengin bütün taşları patlar; durulma sırasında giren M taşları da hedeflenir.
            var isinlaPatlayan = olaylar.Skip(secimIndisi).OfType<TasPatladi>()
                .Where(p => p.Tur == VurusTuru.RenkIsini).Select(p => p.Tas).ToList();
            Assert.That(isinlaPatlayan, Is.EquivalentTo(oAnki.Where(t => t.Renk == secim.Renk)));
            Assert.That(secim.HedefSayisi, Is.EqualTo(isinlaPatlayan.Count));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("B09")]
        [Property("GDD", "3.5.3")]
        [Property("AK", "AK-09 B")]
        public void B09_AK09_B_hedef_liste_tetik_aninda_sabitlenir_sonradan_gelen_taslar_hedeflenmez(int tohum)
        {
            var test = TahtaKurucu.Kur(B09Tahtasi);
            var baslangictakiler = test.Tahta.TumKonumlar().Select(k => test.Tahta[k].Tas!).ToList();

            var d = HamleDenemesi.TamKaydir(
                test,
                H(3, 1),
                H(3, 2),
                AkBayraklari.Varsayilan.Ile(Ak.RenkKuresiZincirSecimi, Varyant.B),
                tohum,
                notrDolgu: false);
            var olaylar = d.Olaylar.ToList();

            int tetik = olaylar.FindIndex(o => o is RenkKuresiTetiklendi);
            int secimIndisi = olaylar.FindIndex(o => o is RenkSecildi);

            // Işınlar dalga tamamlanınca, satır 3 dolmadan gönderilir.
            Assert.That(olaylar.Skip(tetik + 1).Take(secimIndisi - tetik - 1).OfType<TasGirdi>(), Is.Empty);
            Assert.That(((RenkSecildi)olaylar[secimIndisi]).Renk, Is.EqualTo(Mavi));

            var isinlaPatlayan = olaylar.OfType<TasPatladi>().Where(p => p.Tur == VurusTuru.RenkIsini).Select(p => p.Tas).ToList();
            Assert.That(isinlaPatlayan, Has.Count.EqualTo(10));
            Assert.That(isinlaPatlayan, Is.SubsetOf(baslangictakiler), "Sonradan düşen M taşları hedeflenmez.");
        }

        [Test]
        [Category("B10")]
        [Property("GDD", "3.1.4, 6.1.2")]
        public void B10_Ozel_tas_yan_eslesme_hasari_almaz()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · B · ·
                3  K K M · ·
                4  · · K · ·", H(4, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));
            Assert.That(d.Etkinlesenler, Is.Empty, "Bomba taşı patlamaz.");
            Assert.That(d.Vuruslar, Is.Empty);
            Assert.That(
                d.Tahta.TumKonumlar().Count(k => d.Tahta[k].Tas?.Tur == TasTuru.Bomba),
                Is.EqualTo(1),
                "Bomba tahtada bomba taşı olarak kalır.");
        }

        [Test]
        [Category("B10")]
        [Property("GDD", "3.1.4, 5.1.2")]
        public void B10_Bomba_tasi_bosalan_hucreye_duser_ve_bomba_olarak_kalir()
        {
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · B · ·
                3  K K M · ·
                4  · · K · ·", H(4, 3), H(3, 3));

            Assert.That(d.Etkinlesenler, Is.Empty);
            Assert.That(d.Tahta[3, 3].Tas!.Tur, Is.EqualTo(TasTuru.Bomba));
            Assert.That(d.Dusmeler.Single(x => x.Tas.Tur == TasTuru.Bomba).Nereden, Is.EqualTo(H(2, 3)));
        }
    }
}
