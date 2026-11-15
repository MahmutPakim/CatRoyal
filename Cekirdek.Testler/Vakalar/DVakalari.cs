using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Vakalar
{
    /// <summary>test_vakalari.md — D. Düşme, çapraz kayma, doldurma, kaskad.</summary>
    [TestFixture]
    public class DVakalari
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };

        [Test]
        [Category("D01")]
        [Property("GDD", "5.1.1, 5.1.2, 5.1.3, 5.3.1")]
        public void D01_Duz_dusme_sirayi_korur_bosluk_yukaridan_dolar()
        {
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · a b c ·
                2  · d e f ·
                3  · K M K ·
                4  · · K · ·", H(4, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 2), H(3, 3), H(3, 4) }));

            Assert.That(d.Test.IzlenenKonumu('d'), Is.EqualTo(H(3, 2)));
            Assert.That(d.Test.IzlenenKonumu('e'), Is.EqualTo(H(3, 3)));
            Assert.That(d.Test.IzlenenKonumu('f'), Is.EqualTo(H(3, 4)));
            Assert.That(d.Test.IzlenenKonumu('a'), Is.EqualTo(H(2, 2)));
            Assert.That(d.Test.IzlenenKonumu('b'), Is.EqualTo(H(2, 3)));
            Assert.That(d.Test.IzlenenKonumu('c'), Is.EqualTo(H(2, 4)));

            // Satır 1'in sütun 2–4 hücreleri giriş hücrelerinden gelen yeni taşlarla dolar.
            Assert.That(d.Girisler.Select(g => g.Nereye), Is.EquivalentTo(new[] { H(1, 2), H(1, 3), H(1, 4) }));
            Assert.That(d.Girisler.Select(g => g.Giris), Is.EquivalentTo(new[] { H(1, 2), H(1, 3), H(1, 4) }));
            Assert.That(d.Tahta.TumKonumlar().Select(k => d.Tahta[k].Tas), Has.None.Null);

            Assert.That(d.Tahta[4, 3].Tas!.Renk, Is.EqualTo(Mavi), "(4,3)'teki M yerinde kalır.");
            Assert.That(d.Dusmeler.All(x => x.Nereden.Sutun == x.Nereye.Sutun), Is.True, "Sütunlar arasında taş karışmaz.");
            Assert.That(d.CaprazKaymalar, Is.Empty);
        }

        private const string D02Tahtasi = @"
               1 2 3 4 5
            1  X · · · ·
            2  K · · · ·
            3  K · · · ·
            4  · K · · ·";

        [TestCaseSource(nameof(Tohumlar))]
        [Category("D02")]
        [Property("GDD", "5.3.1, 5.2.2")]
        [Property("AK", "AK-03, AK-04 A")]
        public void D02_Yeni_tas_yalnizca_giris_hucresinden_girer(int tohum)
        {
            // (4,2)'deki K sola kayar ve sütun 1'de dikey 3'lü kurar; sütun 1'in rows 2–4 hücreleri boşalır.
            var test = TahtaKurucu.Kur(D02Tahtasi);
            var d = HamleDenemesi.TamKaydir(test, H(4, 2), H(4, 1), tohum: tohum, capraz: true, notrDolgu: false);

            Assert.That(d.PatlayanHucreler, Is.SupersetOf(new[] { H(2, 1), H(3, 1), H(4, 1) }));

            // Sütun 1'e yukarıdan yeni taş girmez; yeni taşların hepsi giriş türü tanımlı hücrelerden çıkar.
            Assert.That(d.Girisler, Is.Not.Empty);
            Assert.That(d.Girisler.Where(g => g.Giris.Sutun == 1 || g.Nereye.Sutun == 1), Is.Empty);
            Assert.That(d.Girisler.Select(g => d.Tahta[g.Giris].Giris), Is.All.True);

            // Sütun 1'deki boşluk yalnızca komşu sütundan çapraz kaymayla dolar.
            var sutun1eGelenler = d.CaprazKaymalar.Where(c => c.Nereye.Sutun == 1).ToList();
            Assert.That(sutun1eGelenler, Is.Not.Empty);
            Assert.That(sutun1eGelenler.Select(c => c.Nereden.Sutun), Is.All.EqualTo(2));
            Assert.That(d.Dusmeler.Where(x => x.Nereye.Sutun == 1 && x.Nereden.Sutun != 1), Is.Empty);
        }

        [Test]
        [Category("D02")]
        [Property("GDD", "5.3.1")]
        [Property("AK", "AK-04 A")]
        public void D02_Capraz_kayma_da_mumkun_degilse_bosluk_kalir()
        {
            var d = HamleDenemesi.TamKaydir(D02Tahtasi, H(4, 2), H(4, 1), capraz: false);

            Assert.That(d.Tahta[2, 1].Tas, Is.Null);
            Assert.That(d.Tahta[3, 1].Tas, Is.Null);
            Assert.That(d.Tahta[4, 1].Tas, Is.Null);
            Assert.That(d.Girisler.Where(g => g.Nereye.Sutun == 1), Is.Empty);
        }

        [Test]
        [Category("D02")]
        [Property("GDD", "5.3.1")]
        [Property("AK", "AK-04 B")]
        public void D02_AK04_B_sutunun_en_ustteki_oynanabilir_hucresi_ortuk_giris_hucresidir()
        {
            var d = HamleDenemesi.TamKaydir(
                D02Tahtasi,
                H(4, 2),
                H(4, 1),
                AkBayraklari.Varsayilan.Ile(Ak.GirissizSutunDolumu, Varyant.B),
                capraz: false);

            var sutun1 = d.Girisler.Where(g => g.Nereye.Sutun == 1).ToList();
            Assert.That(sutun1, Has.Count.EqualTo(3));
            Assert.That(sutun1.Select(g => g.Giris), Is.All.EqualTo(H(2, 1)));
            Assert.That(d.Tahta[2, 1].Tas, Is.Not.Null);
            Assert.That(d.Tahta[4, 1].Tas, Is.Not.Null);
        }

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B, Description = "Üstü kalıcı olarak kapalı boşluk: AK-03 B ile de sonuç aynıdır.")]
        [Category("D03")]
        [Property("GDD", "5.1.1, 5.2.2, 5.2.4, 6.2.1, 6.2.4")]
        [Property("AK", "AK-03")]
        public void D03_Capraz_kayma_once_sol_yukaridan_yapilir(Varyant ak03)
        {
            var d = HamleDenemesi.Dusur(@"
                   1 2 3 4 5
                1  · c · d ·
                2  · a 1 b ·
                3  · · _ · ·", AkBayraklari.Varsayilan.Ile(Ak.CaprazKaymaKosulu, ak03), capraz: true);

            Assert.That(d.Test.IzlenenKonumu('a'), Is.EqualTo(H(3, 3)));
            Assert.That(d.CaprazKaymalar.Single().Nereden, Is.EqualTo(H(2, 2)));
            Assert.That(d.Test.IzlenenKonumu('c'), Is.EqualTo(H(2, 2)), "Ardından (2,2) üstten c ile dolar.");
            Assert.That(d.Test.IzlenenKonumu('b'), Is.EqualTo(H(2, 4)), "b yerinde kalır.");
            Assert.That(d.Test.IzlenenKonumu('d'), Is.EqualTo(H(1, 4)));
            Assert.That(d.Tahta[2, 3].Tas!.Tur, Is.EqualTo(TasTuru.Engel), "Katmanlı blok düşmez.");
        }

        [TestCase(Varyant.A)]
        [TestCase(Varyant.B)]
        [Category("D04")]
        [Property("GDD", "5.2.2, 5.2.4")]
        [Property("AK", "AK-03")]
        public void D04_Sol_yukari_uygun_degilse_sag_yukaridan_kayar(Varyant ak03)
        {
            var d = HamleDenemesi.Dusur(@"
                   1 2 3 4 5
                1  · c · d ·
                2  · 1 1 b ·
                3  · · _ · ·", AkBayraklari.Varsayilan.Ile(Ak.CaprazKaymaKosulu, ak03), capraz: true);

            Assert.That(d.Test.IzlenenKonumu('b'), Is.EqualTo(H(3, 3)));
            Assert.That(d.CaprazKaymalar.Single().Nereden, Is.EqualTo(H(2, 4)));
            Assert.That(d.Test.IzlenenKonumu('d'), Is.EqualTo(H(2, 4)), "(2,4) üstten d ile dolar.");
            Assert.That(d.Test.IzlenenKonumu('c'), Is.EqualTo(H(1, 2)));
        }

        [TestCase(true)]
        [TestCase(false)]
        [Category("D05")]
        [Property("GDD", "5.1.1, 5.1.2, 5.1.3, 5.2.3")]
        public void D05_Duz_dusme_mumkunse_capraz_kayma_yapilmaz(bool capraz)
        {
            var d = HamleDenemesi.Dusur(@"
                   1 2 3 4 5
                1  · · c · ·
                2  · a e b ·
                3  · · _ · ·", capraz: capraz);

            Assert.That(d.Test.IzlenenKonumu('e'), Is.EqualTo(H(3, 3)));
            Assert.That(d.Test.IzlenenKonumu('c'), Is.EqualTo(H(2, 3)));
            Assert.That(d.Test.IzlenenKonumu('a'), Is.EqualTo(H(2, 2)));
            Assert.That(d.Test.IzlenenKonumu('b'), Is.EqualTo(H(2, 4)));
            Assert.That(d.CaprazKaymalar, Is.Empty);
        }

        public const string D06Tahtasi = @"
               1 2 3 4 5
            1  · · K · ·
            2  · · Y · ·
            3  · · Y · ·
            4  K K M · ·
            5  · · Y · ·";

        [Test]
        [Category("D06")]
        [Property("GDD", "5.5.1, 5.5.2, 7.2.1")]
        public void D06_Kaskad_kendiliginden_patlar()
        {
            var d = HamleDenemesi.TamKaydir(D06Tahtasi, H(5, 3), H(4, 3));

            var patlamalar = d.Olaylar.OfType<GrupPatladi>().ToList();
            Assert.That(patlamalar, Has.Count.EqualTo(2));

            Assert.That(patlamalar[0].Kaskad, Is.False);
            Assert.That(patlamalar[0].Grup.Hucreler, Is.EquivalentTo(new[] { H(2, 3), H(3, 3), H(4, 3) }));

            // (1,3)'teki K, (4,3)'e düşer; satır 4'teki üç K oyuncu bir şey yapmadan patlar.
            Assert.That(patlamalar[1].Kaskad, Is.True);
            Assert.That(patlamalar[1].Grup.Hucreler, Is.EquivalentTo(new[] { H(4, 1), H(4, 2), H(4, 3) }));
            Assert.That(d.Dusmeler.First().Nereden, Is.EqualTo(H(1, 3)));
            Assert.That(d.Dusmeler.First().Nereye, Is.EqualTo(H(4, 3)));

            // Kalan hamlenin 9 olması (kaskad hamle düşmez) hamle sayacıyla birlikte 6. aşamada doğrulanır.
            Assert.That(d.Olaylar.OfType<TaslarYerDegistirdi>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Category("D07")]
        [Property("GDD", "5.5.1, 2.2.4")]
        public void D07_Kaskad_ancak_tahta_durulunca_tetiklenir()
        {
            var d = HamleDenemesi.TamKaydir(D06Tahtasi, H(5, 3), H(4, 3));
            var olaylar = d.Olaylar.ToList();

            int ilkPatlama = olaylar.FindIndex(o => o is GrupPatladi);
            int kaskad = olaylar.FindIndex(o => o is GrupPatladi g && g.Kaskad);
            var arada = olaylar.Skip(ilkPatlama + 1).Take(kaskad - ilkPatlama - 1).ToList();

            // İlk patlama ile kaskad arasında yalnızca düşme ve doldurma vardır; kaskad, düşme bittikten sonra patlar.
            Assert.That(arada, Is.Not.Empty);
            Assert.That(arada, Is.All.InstanceOf<TasDustu>().Or.InstanceOf<TasGirdi>());

            // Kaskad grubunun bütün taşları patlamadan önce yerine oturmuştur.
            var kTasi = arada.OfType<TasDustu>().Single(x => x.Nereye == H(4, 3));
            long sonOturma = arada.Max(o => o is TasDustu x ? x.OturmaAdimi : ((TasGirdi)o).OturmaAdimi);
            Assert.That(kTasi.OturmaAdimi, Is.LessThanOrEqualTo(sonOturma));
            Assert.That(kTasi.Tas.Oturdu, Is.True);
            Assert.That(((GrupPatladi)olaylar[kaskad]).PatlayanTasKimlikleri, Does.Contain(kTasi.Tas.Kimlik));
        }

        // ------------------------------------------------------------------ D08, D09: başlangıç tahtası

        private static (OyunBaglami Baglam, BaslangicSonucu Sonuc) BaslangicTahtasiUret(int tohum)
        {
            var p = ProjeDosyalari.Parametreler;
            var secim = new TohumluRastgele(tohum * 7919);

            // GDD 8.1.2: standart boyut ya da dar biçimleri; GDD 8.2.2: standart renk sayısı ya da eksiği.
            int sutun = p.TamSayi(ParametreAdlari.TahtaGenislikStandart)
                - secim.Sonraki(p.TamSayi(ParametreAdlari.DarTahtaAzamiSutunEksigi) + 1);
            int satir = p.TamSayi(ParametreAdlari.TahtaYukseklikStandart)
                - secim.Sonraki(p.TamSayi(ParametreAdlari.DarTahtaAzamiSatirEksigi) + 1);
            int renk = p.TamSayi(ParametreAdlari.RenkSayisiStandart)
                - secim.Sonraki(p.TamSayi(ParametreAdlari.KolayBolumAzamiRenkEksigi) + 1);

            var tahta = new Tahta(satir, sutun);
            var baglam = HamleDenemesi.BaglamKur(tahta, tohum: tohum);
            baglam.Renkler = Enumerable.Range(0, renk).ToList();
            var sonuc = BaslangicTahtasi.Uret(baglam, tahta.TumKonumlar().ToList());
            return (baglam, sonuc);
        }

        [Test]
        [Category("D08")]
        [Property("GDD", "5.4.1, 5.4.2")]
        public void D08_Baslangic_tahtasinda_hazir_uclu_olmaz()
        {
            for (int tohum = 1; tohum <= 1000; tohum++)
            {
                var (baglam, sonuc) = BaslangicTahtasiUret(tohum);
                var tahta = baglam.Tahta;

                Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Has.None.Null, $"tohum {tohum}");
                Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas!.Renk), Is.All.InRange(0, baglam.Renkler.Count - 1));
                Assert.That(sonuc.UcluKisitiGevsetildi, Is.False, $"tohum {tohum}");

                foreach (var k in tahta.TumKonumlar())
                {
                    int renk = tahta.EslesmeRengi(k);
                    bool yatay = tahta.EslesmeRengi(k.Sag) == renk && tahta.EslesmeRengi(k.Sag.Sag) == renk;
                    bool dikey = tahta.EslesmeRengi(k.Asagi) == renk && tahta.EslesmeRengi(k.Asagi.Asagi) == renk;
                    Assert.That(yatay || dikey, Is.False, $"tohum {tohum}, hücre {k}");
                }
            }
        }

        [Test]
        [Category("D09")]
        [Property("GDD", "5.4.3, 5.4.4")]
        public void D09_Baslangic_tahtasinda_hazir_kare_olmaz_ve_en_az_bir_olasi_hamle_bulunur()
        {
            int gevsetilen = 0;
            for (int tohum = 1; tohum <= 1000; tohum++)
            {
                var (baglam, sonuc) = BaslangicTahtasiUret(tohum);
                var tahta = baglam.Tahta;

                // Kare kısıtının gevşetildiği tahtalar (GDD 5.4.6) kare özelliğinden muaftır.
                if (sonuc.KareKisitiGevsetildi)
                {
                    gevsetilen++;
                }
                else
                {
                    foreach (var k in tahta.TumKonumlar())
                    {
                        int renk = tahta.EslesmeRengi(k);
                        bool kare = tahta.EslesmeRengi(k.Sag) == renk
                            && tahta.EslesmeRengi(k.Asagi) == renk
                            && tahta.EslesmeRengi(k.Asagi.Sag) == renk;
                        Assert.That(kare, Is.False, $"tohum {tohum}, hücre {k}");
                    }
                }

                // Tek bir yer değiştirmeyle eşleşme oluşturan bir çift.
                var hamleler = OlasiHamleBulucu.Bul(tahta);
                Assert.That(hamleler.Any(h => h.Tur == OlasiHamleTuru.Eslesme), Is.True, $"tohum {tohum}");
            }

            TestContext.Out.WriteLine($"Kare kısıtı gevşetilen tahta: {gevsetilen} / 1000");
        }

        // ------------------------------------------------------------------ D10: karıştırma

        /// <summary>Test üreteci: hazır eşleşmesi ve olası hamlesi olmayan rastgele renkli tahtalar arar.</summary>
        private static List<(Tahta Tahta, int RenkSayisi)> HamlesizTahtalar(int adet)
        {
            var bulunan = new List<(Tahta, int)>();
            var rastgele = new TohumluRastgele(20260101);
            while (bulunan.Count < adet)
            {
                int boyut = 3 + rastgele.Sonraki(3);
                int renkSayisi = 5 + rastgele.Sonraki(4);
                var tahta = new Tahta(boyut, boyut);
                foreach (var k in tahta.TumKonumlar())
                {
                    tahta[k].Tas = tahta.NormalTasUret(rastgele.Sonraki(renkSayisi));
                }

                if (!OlasiHamleBulucu.HazirEslesmeVar(tahta) && !OlasiHamleBulucu.Var(tahta))
                {
                    bulunan.Add((tahta, renkSayisi));
                }
            }

            return bulunan;
        }

        private static Dictionary<int, int> RenkSayimi(Tahta tahta) =>
            tahta.TumKonumlar()
                .Where(k => tahta[k].Tas != null)
                .GroupBy(k => tahta[k].Tas!.Renk)
                .ToDictionary(g => g.Key, g => g.Count());

        [Test]
        [Category("D10")]
        [Property("GDD", "5.6.1, 5.6.2, 5.6.5, 7.2.3")]
        [Property("AK", "AK-13 A")]
        public void D10_Olasi_hamle_kalmazsa_karistirma_gecerli_bir_tahta_uretir()
        {
            var sonuclar = new Dictionary<KaristirmaSonucu, int>();
            int sira = 0;
            foreach (var (tahta, renkSayisi) in HamlesizTahtalar(100))
            {
                sira++;
                var baglam = HamleDenemesi.BaglamKur(tahta, tohum: sira);
                baglam.Renkler = Enumerable.Range(0, renkSayisi).ToList();
                var once = RenkSayimi(tahta);
                var onceTaslar = tahta.TumKonumlar().Select(k => tahta[k].Tas!).ToList();
                var olaylar = new List<Olay>();

                var sonuc = Karistirici.GerekirseKaristir(baglam, olaylar);
                sonuclar[sonuc] = sonuclar.TryGetValue(sonuc, out int n) ? n + 1 : 1;

                Assert.That(sonuc, Is.Not.EqualTo(KaristirmaSonucu.Gerekmedi));
                Assert.That(olaylar.OfType<TahtaKaristirildi>().Single().Sonuc, Is.EqualTo(sonuc));

                if (sonuc == KaristirmaSonucu.BastanBaslatilmali)
                {
                    // Hiçbir yol hamle kuramadı: taşlar eski yerlerinde.
                    Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Is.EqualTo(onceTaslar));
                    continue;
                }

                // Karıştırma sonrası tahtada hazır eşleşme yoktur ve en az bir olası hamle vardır.
                Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False, $"tahta {sira}");
                Assert.That(OlasiHamleBulucu.Var(tahta), Is.True, $"tahta {sira}");

                var sonra = RenkSayimi(tahta);
                switch (sonuc)
                {
                    case KaristirmaSonucu.Karistirildi:
                        // Yalnızca yer değiştirme: renk sayıları aynen korunur, taşlar aynı taşlardır.
                        Assert.That(sonra, Is.EquivalentTo(once));
                        Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Is.EquivalentTo(onceTaslar));
                        Assert.That(olaylar.OfType<OlasiHamleYok>(), Is.Empty);
                        break;

                    case KaristirmaSonucu.RenkDegistirildi:
                        // Renk yalnızca yer değiştirme başarısız olunca değişir; dağılım mümkün olduğunca korunur:
                        // bir 3'lü kurmak için en fazla üç taşın rengi değişir.
                        int degisen = once.Keys.Union(sonra.Keys)
                            .Sum(r => System.Math.Max(0, (once.TryGetValue(r, out int a) ? a : 0) - (sonra.TryGetValue(r, out int b) ? b : 0)));
                        Assert.That(degisen, Is.InRange(1, 3), $"tahta {sira}");
                        Assert.That(olaylar.OfType<OlasiHamleYok>(), Is.Empty);
                        break;

                    case KaristirmaSonucu.YenidenKuruldu:
                        // Renk değiştirme de başarısız: "olası hamle yok" akışı, bölüm kaybı sayılmadan tahtayı yeniden kurar.
                        Assert.That(olaylar.OfType<OlasiHamleYok>().Count(), Is.EqualTo(1));
                        break;

                    default:
                        Assert.Fail($"AK-13 A ile beklenmeyen sonuç: {sonuc}");
                        break;
                }
            }

            TestContext.Out.WriteLine(string.Join(", ", sonuclar.Select(s => $"{s.Key}: {s.Value}")));
            Assert.That(sonuclar.Keys, Does.Contain(KaristirmaSonucu.Karistirildi));
        }

        [Test]
        [Category("D10")]
        [Property("GDD", "5.6.5, 5.6.6")]
        [Property("AK", "AK-13 B")]
        public void D10_AK13_B_renk_degistirme_yoktur_basarisiz_tahtada_bolum_kaybedilir()
        {
            // 1. aşama, karar 3: AK-13 B kümesi renk değişimi bayrağını da kapatır (GDD'de ayrı bayrak).
            var bayraklar = AkBayraklari.Varsayilan
                .Ile(Ak.KaristirmaBasarisizligi, Varyant.B)
                .KaristirmaRenkDegisimiIle(false);

            int sira = 0;
            int kaybedilen = 0;
            foreach (var (tahta, renkSayisi) in HamlesizTahtalar(100))
            {
                sira++;
                var baglam = HamleDenemesi.BaglamKur(tahta, bayraklar, tohum: sira);
                baglam.Renkler = Enumerable.Range(0, renkSayisi).ToList();
                var once = RenkSayimi(tahta);
                var onceTaslar = tahta.TumKonumlar().Select(k => tahta[k].Tas!).ToList();
                var olaylar = new List<Olay>();

                var sonuc = Karistirici.GerekirseKaristir(baglam, olaylar);

                Assert.That(sonuc, Is.EqualTo(KaristirmaSonucu.Karistirildi).Or.EqualTo(KaristirmaSonucu.Kaybedildi));
                Assert.That(RenkSayimi(tahta), Is.EquivalentTo(once), "Renk değiştirme yoktur.");
                if (sonuc == KaristirmaSonucu.Kaybedildi)
                {
                    kaybedilen++;
                    Assert.That(olaylar.OfType<OlasiHamleYok>().Count(), Is.EqualTo(1));
                    Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Is.EqualTo(onceTaslar));
                }
                else
                {
                    Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False);
                    Assert.That(OlasiHamleBulucu.Var(tahta), Is.True);
                }
            }

            TestContext.Out.WriteLine($"Kaybedilen: {kaybedilen} / 100");
        }
    }
}
