using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>GDD 6, 7 ve 3.7–3.9'un test_vakalari.md'de doğrudan vakası olmayan kuralları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class EngelVeBolumKuralTestleri
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34 };

        private const string UcluTahtasi = @"
               1 2 3 4 5
            1  · · · · ·
            2  · · · · ·
            3  K K M · ·
            4  · · K · ·";

        private static HucreKatmani Katman(KatmanTanimi tanim, int katman = 1) => new HucreKatmani(tanim, katman);

        // ------------------------------------------------------------------ Hücre katmanları (6.1.6–6.1.8, 6.2.2)

        [Test]
        [Property("GDD", "6.1.7, 6.2.5, 2.7.5")]
        public void Kilitli_tas_esleymeye_girer_eslesme_kilidi_kirar_tas_yerinde_kalir()
        {
            var test = TahtaKurucu.Kur(UcluTahtasi);
            test.Tahta[3, 1].UstKatman = Katman(Kilit);
            var kilitliTas = test.Tahta[3, 1].Tas;

            var d = HamleDenemesi.Kaydir(test, H(4, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().Hucreler, Does.Contain(H(3, 1)));
            Assert.That(d.Olaylar.OfType<KatmanHasarAldi>().Single().Kirildi, Is.True);
            Assert.That(d.Tahta[3, 1].UstKatman, Is.Null);
            Assert.That(d.Tahta[3, 1].Tas, Is.SameAs(kilitliTas), "Üst katman kırılmadan alttaki taş patlamaz.");
            Assert.That(d.Tahta[3, 2].Tas, Is.Null);
        }

        [Test]
        [Property("GDD", "6.2.2")]
        public void Kilit_yandaki_eslesmeden_etkilenmez_ozel_tas_vurusuyla_kirilir()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · # · -
                3  K K M · ·
                4  · · K · ·");
            var o = HamleDenemesi.BaglamKur(test.Tahta);
            var olaylar = new List<Olay>();

            HamleIsleyici.Kaydir(o, H(4, 3), H(3, 3), olaylar, HamleKapsami.YalnizPatlama);
            Assert.That(olaylar.OfType<KatmanHasarAldi>(), Is.Empty);
            Assert.That(test.Tahta[2, 3].Kilitli, Is.True);

            HamleIsleyici.Kaydir(o, H(2, 5), H(2, 4), olaylar, HamleKapsami.YalnizPatlama);
            Assert.That(olaylar.OfType<KatmanHasarAldi>().Single().Konum, Is.EqualTo(H(2, 3)));
            Assert.That(test.Tahta[2, 3].Kilitli, Is.False);
            Assert.That(test.Tahta[2, 3].Tas, Is.Not.Null, "Vuruş kilide gider; taş yerinde kalır.");
        }

        [Test]
        [Property("GDD", "3.5.8, 11.2.2")]
        public void Renk_isini_kilitli_hedef_tasa_degil_kilide_vurur()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3
                1  K R ·
                2  · K ·");
            test.Tahta[2, 2].UstKatman = Katman(Kilit);

            var d = HamleDenemesi.Kaydir(test, H(1, 2), H(1, 1));

            Assert.That(d.PatlayanTaslar, Is.Empty.Or.All.Matches<TasPatladi>(p => p.Konum != H(2, 2)));
            Assert.That(d.Tahta[2, 2].Tas!.Renk, Is.EqualTo(Kirmizi));
            Assert.That(d.Tahta[2, 2].UstKatman, Is.Null);
        }

        [Test]
        [Property("GDD", "6.1.8, 6.1.1, 7.1.2")]
        public void Hucredeki_tas_patlayinca_zemin_bir_katman_kaybeder_son_katmanda_hedef_sayilir()
        {
            var zemin = KatmanArketipleri.Zemin();
            var o = TestOturumu.Kur(
                UcluTahtasi,
                hedefler: new[] { new Hedef("zemin", 0) },
                hazirla: test =>
                {
                    test.Tahta[3, 1].AltKatman = Katman(zemin, 2);
                    test.Tahta[3, 2].AltKatman = Katman(zemin, 1);
                });

            Assert.That(o.Oturum.Hedefler.Kalan("zemin"), Is.EqualTo(2), "Adet 0: tahtadaki bütün örnekler hedeftir.");

            o.Kaydir(H(4, 3), H(3, 3));

            Assert.That(o.Tahta[3, 1].AltKatman!.Katman, Is.EqualTo(1));
            Assert.That(o.Tahta[3, 2].AltKatman, Is.Null);
            Assert.That(o.Oturum.Hedefler.Kalan("zemin"), Is.EqualTo(1));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
        }

        [Test]
        [Property("GDD", "6.2.2")]
        public void Zemin_yandaki_eslesmeden_de_katman_kaybeder_sert_kaplama_kaybetmez()
        {
            var test = TahtaKurucu.Kur(UcluTahtasi);
            test.Tahta[2, 1].AltKatman = Katman(KatmanArketipleri.Zemin(), 2);
            test.Tahta[2, 2].UstKatman = Katman(KatmanArketipleri.SertKaplama(), 2);

            var d = HamleDenemesi.Kaydir(test, H(4, 3), H(3, 3));

            Assert.That(d.Tahta[2, 1].AltKatman!.Katman, Is.EqualTo(1));
            Assert.That(d.Tahta[2, 2].UstKatman!.Katman, Is.EqualTo(2));
        }

        [Test]
        [Property("GDD", "6.2.2, 3.4.7")]
        public void Ortu_altindaki_tas_eslesmeye_girer_ortu_gudumlu_hedefi_olamaz()
        {
            // Kullanıcı kararı (6. aşama): örtü altındaki gizli taş eşleşmeye girer; eşleşme örtüye vurur.
            var test = TahtaKurucu.Kur(UcluTahtasi);
            var ortu = KatmanArketipleri.Ortu();
            test.Tahta[3, 1].UstKatman = Katman(ortu, 2);

            var d = HamleDenemesi.Kaydir(test, H(4, 3), H(3, 3));

            Assert.That(d.Gruplar.Single().Hucreler, Does.Contain(H(3, 1)));
            Assert.That(d.Tahta[3, 1].UstKatman!.Katman, Is.EqualTo(1));
            Assert.That(d.Tahta[3, 1].Tas, Is.Not.Null);

            var gudumlu = TahtaKurucu.Kur(@"
                   1 2 3
                1  G _ _
                2  _ _ 2
                3  _ K _");
            gudumlu.Tahta[2, 3].UstKatman = Katman(ortu, 1);
            var g = HamleDenemesi.Dokun(gudumlu, H(1, 1));
            Assert.That(g.Inisler.Single().Hedef, Is.EqualTo(H(3, 2)), "Örtü altındaki hücre hedef olamaz.");
        }

        [Test]
        [Property("GDD", "6.3.1, 11.2.9")]
        public void Bomba_tasi_yayilan_zeminli_hucreden_etkinlesirse_zemini_alanina_yayar()
        {
            var test = TahtaKurucu.Kur(NotrCizim(5, 5, (3, 3, 'B')));
            test.Tahta[3, 3].AltKatman = Katman(YayilanZemin);

            var d = HamleDenemesi.Dokun(test, H(3, 3));

            Assert.That(d.Tahta.TumKonumlar().Select(k => d.Tahta[k].AltKatman?.Tanim.Yayilir), Is.All.True);
            Assert.That(d.Olaylar.OfType<ZeminYayildi>().Count(), Is.EqualTo(24));
        }

        // ------------------------------------------------------------------ Hücreyi dolduran engeller (6.2.1, 6.3)

        [Test]
        [Property("GDD", "6.1.4")]
        [Property("AK", "AK-11 A")]
        public void Coklu_vurus_blogu_ayni_eslesmenin_degdigi_her_hucre_icin_bir_vurus_alir()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  K · · · ·
                2  K · · · ·
                3  M K K · ·
                4  K · · · ·");
            test.Tahta[2, 2].Tas = test.Tahta.EngelUret(Arketipler.CokluVurusBlogu(), 3);

            var d = HamleDenemesi.Kaydir(test, H(4, 1), H(3, 1));

            Assert.That(d.Olaylar.OfType<EngelHasarAldi>().Count(), Is.EqualTo(2));
            Assert.That(d.Tahta[2, 2].Tas!.Katman, Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "6.2.1, 2.3.3")]
        public void Dusen_kirilabilir_duser_kaydirilir_ama_renk_eslesmesine_girmez()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4
                1  · _ · ·
                2  · _ · ·
                3  · _ K K
                4  · · · ·");
            var kirilabilir = test.Tahta.EngelUret(Arketipler.DusenKirilabilir(), 1);
            test.Tahta[1, 2].Tas = kirilabilir;
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta);
            Dinamik.DusmeIsleyici.Durult(baglam, new List<Olay>());
            Assert.That(test.Tahta[3, 2].Tas, Is.SameAs(kirilabilir), "Normal taş gibi düşer.");

            // Kaydırılabilir, ama eşleşme kurmadığı için hamle geçersizdir.
            var olaylar = new List<Olay>();
            Assert.That(
                HamleIsleyici.Kaydir(baglam, H(3, 2), H(4, 2), olaylar, HamleKapsami.Durulma),
                Is.EqualTo(HamleSonucu.Gecersiz));
            Assert.That(test.Tahta[3, 2].Tas, Is.SameAs(kirilabilir));
        }

        [Test]
        [Property("GDD", "6.1.5, 6.4")]
        public void Vurus_turune_bagisik_engel_o_vurustan_hasar_almaz()
        {
            var bagisik = Arketipler.KatmanliBlok();
            bagisik.Bagisikliklar.Add(VurusTuru.Cizgi);
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  - · K · ·
                2  · · · · ·");
            test.Tahta[1, 3].Tas = test.Tahta.EngelUret(bagisik, 2);

            var cizgi = HamleDenemesi.Dokun(test, H(1, 1));
            Assert.That(cizgi.VurulanHucreler, Does.Contain(H(1, 3)));
            Assert.That(cizgi.Olaylar.OfType<EngelHasarAldi>(), Is.Empty);
            Assert.That(test.Tahta[1, 3].Tas!.Katman, Is.EqualTo(2));

            // Bomba vuruşuna bağışıklık yoktur.
            var test2 = TahtaKurucu.Kur(@"
                   1 2 3
                1  B · ·
                2  · · ·");
            test2.Tahta[1, 2].Tas = test2.Tahta.EngelUret(bagisik, 2);
            var bomba = HamleDenemesi.Dokun(test2, H(1, 1));
            Assert.That(bomba.Olaylar.OfType<EngelHasarAldi>().Single().KalanKatman, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "6.3.2")]
        public void Uretici_kirilinca_dalga_bittikten_sonra_cevresine_kaplama_birakir(int tohum)
        {
            var kaplama = KatmanArketipleri.YapiskanKaplama();
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · · ·
                3  K K M · ·
                4  · · K · ·");
            test.Tahta[2, 2].Tas = test.Tahta.EngelUret(Arketipler.Uretici(kaplama), 1);

            var d = HamleDenemesi.TamKaydir(test, H(4, 3), H(3, 3), tohum: tohum);

            int adet = ProjeDosyalari.Parametreler.TamSayi(ParametreAdlari.UreticiYayilmaHucreSayisi);
            var birakilan = d.Olaylar.OfType<KatmanBirakildi>().ToList();

            // Üreticinin dik komşuları: (1,2), (2,1), (2,3). (3,2) eşleşmeyle boşaldığı için kaplanacak taş yoktur.
            Assert.That(birakilan.Count, Is.EqualTo(System.Math.Min(adet, 3)));
            Assert.That(birakilan.Select(b => b.Konum), Is.SubsetOf(new[] { H(1, 2), H(2, 1), H(2, 3) }));
            Assert.That(birakilan.Select(b => b.Tanim), Is.All.SameAs(kaplama));

            // Düşmeler başlamadan bırakılır.
            var olaylar = d.Olaylar.ToList();
            Assert.That(
                olaylar.FindLastIndex(x => x is KatmanBirakildi),
                Is.LessThan(olaylar.FindIndex(x => x is TasDustu || x is TasGirdi)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "6.3.3")]
        public void Yayilan_tehlike_patlayinca_cevresindeki_normal_taslara_yeni_tehlike_firlatir(int tohum)
        {
            // Kullanıcı kararı (6. aşama): çevre 4 dik komşudur; tehlike normal taşın yerine geçer.
            var tehlike = Arketipler.YayilanTehlike();
            var test = TahtaKurucu.Kur(UcluTahtasi);
            test.Tahta[2, 2].Tas = test.Tahta.EngelUret(tehlike, 1);

            var d = HamleDenemesi.Kaydir(test, H(4, 3), H(3, 3), tohum: tohum);

            int adet = ProjeDosyalari.Parametreler.TamSayi(ParametreAdlari.TehlikeYayilmaSayisi);
            var yayilan = d.Olaylar.OfType<TehlikeYayildi>().ToList();
            Assert.That(yayilan.Count, Is.EqualTo(System.Math.Min(adet, 3)));
            Assert.That(yayilan.Select(y => y.Nereden), Is.All.EqualTo(H(2, 2)));
            Assert.That(yayilan.Select(y => y.Nereye), Is.SubsetOf(new[] { H(1, 2), H(2, 1), H(2, 3) }));
            foreach (var y in yayilan)
            {
                Assert.That(d.Tahta[y.Nereye].Tas!.Engel, Is.SameAs(tehlike));
            }

            Assert.That(d.Tahta[2, 2].Tas, Is.Null);
        }

        [Test]
        [Property("GDD", "6.3.5, 6.2.1, 7.1.2")]
        public void Asagi_inen_toplanabilir_toplama_hucresine_inince_toplanir_vurulmaz()
        {
            var toplanabilir = Arketipler.AsagiInenToplanabilir();
            toplanabilir.HedefTuru = "toplanabilir";
            var o = TestOturumu.Kur(
                @"
                   1 2 3 4 5
                1  · · · · -
                2  · · · · ·
                3  K K M · ·
                4  · · K · ·",
                hedefler: new[] { new Hedef("toplanabilir", 1) },
                hazirla: test => test.Tahta[1, 1].Tas = test.Tahta.EngelUret(toplanabilir, 1));
            var oge = o.Tahta[1, 1].Tas;

            // Çizgi taşı satır 1'i temizler; toplanabilir vuruştan etkilenmez.
            Assert.That(o.Kaydir(H(1, 5), H(1, 4)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Tahta[1, 1].Tas, Is.SameAs(oge));
            Assert.That(o.OlaylarTurunde<EngelHasarAldi>(), Is.Empty);

            // Altı boşaldıkça iner; işaret yoksa sütunun en alt oynanabilir hücresi toplama hücresidir.
            Assert.That(o.Kaydir(H(4, 3), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Tahta[2, 1].Tas, Is.SameAs(oge));
            Assert.That(o.OlaylarTurunde<OgeToplandi>(), Is.Empty);

            // Sütun 1'in altını çizgi taşıyla boşaltınca en alta iner ve toplanır.
            o.Tahta[4, 2].Tas = o.Tahta.OzelTasUret(TasTuru.Cizgi, Yon.Dikey);
            o.Tahta[3, 2].Tas = o.Tahta.NormalTasUret(Sari);
            Assert.That(o.Kaydir(H(4, 2), H(4, 1)), Is.EqualTo(HamleSonucu.Gecerli));

            var toplanan = o.OlaylarTurunde<OgeToplandi>().Single();
            Assert.That(toplanan.Oge, Is.SameAs(oge));
            Assert.That(toplanan.Konum, Is.EqualTo(H(4, 1)));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
        }

        private sealed class SayanDavranis : IHamleSonuDavranisi
        {
            public int Cagri { get; private set; }

            public bool Uygula(OyunBaglami baglam, Konum konum, List<Olay> olaylar)
            {
                Cagri++;
                return false;
            }
        }

        [Test]
        [Property("GDD", "6.3.4, 5.7")]
        public void Hamle_sonu_hareketli_her_gecerli_oyuncu_hamlesinden_sonra_bir_kez_guncellenir()
        {
            var davranis = new SayanDavranis();
            var o = TestOturumu.Kur(
                UcluTahtasi,
                hazirla: test => test.Tahta[1, 5].Tas = test.Tahta.EngelUret(
                    Arketipler.HamleSonuHareketli(davranis, yanEslesmeHasari: false, ozelTasVurusu: false), 1));

            o.Kaydir(H(1, 1), H(1, 2));
            Assert.That(davranis.Cagri, Is.EqualTo(0), "Geçersiz hamle sayılmaz.");

            o.Kaydir(H(4, 3), H(3, 3));
            Assert.That(davranis.Cagri, Is.EqualTo(1));

            o.Arac(AracTuru.TekHucre, H(2, 2));
            Assert.That(davranis.Cagri, Is.EqualTo(1), "Araç kullanımı oyuncu hamlesi değildir.");
        }

        // ------------------------------------------------------------------ Hedefler ve hamle sayacı (7.1, 7.2)

        [Test]
        [Property("GDD", "7.1.4")]
        public void Hedef_sayaci_sifirin_altina_inmez()
        {
            var o = TestOturumu.Kur(
                @"
                   1 2 3 4 5
                1  · · · · ·
                2  · · K · ·
                3  K K M K K
                4  · · · · ·",
                hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 2), new Hedef(Hedef.RenkTuru(Mavi), 9) });

            o.Kaydir(H(2, 3), H(3, 3));

            Assert.That(o.Oturum.Hedefler.Kalan(Hedef.RenkTuru(Kirmizi)), Is.EqualTo(0));
            Assert.That(o.OlaylarTurunde<HedefIlerledi>().Count(), Is.EqualTo(2));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor), "Mavi hedefi sürüyor.");
        }

        [Test]
        [Property("GDD", "5.5.2, 7.2.3")]
        public void Kaskad_hamle_dusurmez()
        {
            var o = TestOturumu.Kur(Vakalar.DVakalari.D06Tahtasi, hamle: 10);

            o.Kaydir(H(5, 3), H(4, 3));

            Assert.That(o.OlaylarTurunde<GrupPatladi>().Count(g => g.Kaskad), Is.EqualTo(1));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9), "D06: kalan hamle 9 olur; kaskad hamle düşmez.");
            Assert.That(o.OlaylarTurunde<HamleSayaciDegisti>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "2.3.6, 7.2.2")]
        public void Hedefi_olmayan_renk_kuresine_dokunmak_sayactan_dusmez()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3
                1  B R -", hamle: 5);

            Assert.That(o.Dokun(H(1, 2)), Is.EqualTo(HamleSonucu.EtkisizDokunma));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(5));
        }

        [Test]
        [Property("GDD", "3.8.2")]
        public void Yedek_hamle_esigi_sifirdan_buyukse_hak_oyun_surerken_bir_kez_kullanilir()
        {
            var parametreler = ProjeDosyalari.Parametreler.Degistir(ParametreAdlari.YedekHamleEsigi, 2);
            var o = TestOturumu.Kur(UcluTahtasi, hamle: 3, parametreler: parametreler);
            o.Oturum.YedekHamleHakki = true;
            int eklenen = parametreler.TamSayi(ParametreAdlari.YedekHamleSayisi);

            o.Kaydir(H(4, 3), H(3, 3));

            // Kalan hamle 3'ten 2'ye, yani eşiğe indi: hak hemen kullanılır.
            Assert.That(o.OlaylarTurunde<YedekHamleKullanildi>().Single().EklenenHamle, Is.EqualTo(eklenen));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(2 + eklenen));
            Assert.That(o.Oturum.YedekHamleHakki, Is.False);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
        }

        // ------------------------------------------------------------------ Araçlar, dolum göstergesi, takviyeler

        [Test]
        [Property("GDD", "3.9.4, 3.9.5, 7.2.4")]
        public void Oyun_ici_araclar_vurur_ve_hamle_harcamaz()
        {
            var o = TestOturumu.Kur(NotrCizim(5, 5, (2, 2, '2'), (4, 4, '1')), hamle: 7);

            Assert.That(o.Arac(AracTuru.TekHucre, H(2, 2)), Is.True);
            Assert.That(o.OlaylarTurunde<HucreVuruldu>().Single().Tur, Is.EqualTo(VurusTuru.Arac));
            Assert.That(o.Tahta[2, 2].Tas!.Katman, Is.EqualTo(1));

            int ilk = o.Olaylar.Count;
            Assert.That(o.Arac(AracTuru.Satir, H(4, 1)), Is.True);
            Assert.That(
                o.Olaylar.Skip(ilk).OfType<HucreVuruldu>().Select(v => v.Konum),
                Is.EquivalentTo(Enumerable.Range(1, 5).Select(t => H(4, t))));
            Assert.That(o.Olaylar.Skip(ilk).OfType<EngelHasarAldi>().Single().Kirildi, Is.True);

            ilk = o.Olaylar.Count;
            Assert.That(o.Arac(AracTuru.Sutun, H(1, 2)), Is.True);
            Assert.That(
                o.Olaylar.Skip(ilk).OfType<HucreVuruldu>().Select(v => v.Konum),
                Is.EquivalentTo(Enumerable.Range(1, 5).Select(s => H(s, 2))));

            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(7));
            Assert.That(o.OlaylarTurunde<HamleSayaciDegisti>(), Is.Empty);
        }

        [Test]
        [Property("GDD", "3.9.4, 6.4")]
        public void Aracla_secilemeyen_hucre_hedef_olarak_isaretlenemez()
        {
            var secilemez = Arketipler.KatmanliBlok();
            secilemez.AraclaSecilebilir = false;
            var o = TestOturumu.Kur(
                @"
                   1 2 3
                1  · · X
                2  · · ·",
                hazirla: test => test.Tahta[1, 1].Tas = test.Tahta.EngelUret(secilemez, 2));

            Assert.That(o.Arac(AracTuru.TekHucre, H(1, 1)), Is.False);
            Assert.That(o.Arac(AracTuru.TekHucre, H(1, 3)), Is.False);
            Assert.That(o.Arac(AracTuru.Satir), Is.False);
            Assert.That(o.Olaylar, Is.Empty);
            Assert.That(o.Tahta[1, 1].Tas!.Katman, Is.EqualTo(2));
        }

        [Test]
        [Property("GDD", "3.7.2, 3.7.3, 3.7.6")]
        public void Dolum_gostergesi_ozel_tas_patlamalariyla_dolar_dolunca_tek_renk_kuresi_firlatir_ve_sifirlanir()
        {
            var p = ProjeDosyalari.Parametreler;
            double bombaPuani = p.Sayi(ParametreAdlari.DolumPuanBomba);
            var parametreler = p.Degistir(ParametreAdlari.DolumKapasitesi, bombaPuani * 2);
            var o = TestOturumu.Kur(NotrCizim(9, 9, (2, 2, 'B'), (8, 8, 'B')), parametreler: parametreler);
            o.Oturum.DolumGostergesiAcik = true;

            o.Dokun(H(2, 2));
            Assert.That(o.Oturum.DolumDegeri, Is.EqualTo(bombaPuani));
            Assert.That(o.OlaylarTurunde<RenkKuresiFirlatildi>(), Is.Empty);

            var ikinciBomba = o.Tahta.TumKonumlar().Single(k => o.Tahta[k].Tas?.Tur == TasTuru.Bomba);
            o.Dokun(ikinciBomba);

            var firlatma = o.OlaylarTurunde<RenkKuresiFirlatildi>().Single();
            Assert.That(o.Tahta[firlatma.Konum].Tas, Is.SameAs(firlatma.Kure));
            Assert.That(firlatma.Kure.Tur, Is.EqualTo(TasTuru.RenkKuresi));
            Assert.That(o.Oturum.DolumDegeri, Is.EqualTo(0), "Gösterge sıfırlanıp yeniden dolmaya başlar.");
            Assert.That(o.Tahta.TumKonumlar().Count(k => o.Tahta[k].Tas?.Tur == TasTuru.RenkKuresi), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "3.7.1")]
        public void Dolum_gostergesi_kapaliyken_dolmaz()
        {
            var o = TestOturumu.Kur(NotrCizim(5, 5, (3, 3, 'B')));

            o.Dokun(H(3, 3));

            Assert.That(o.Oturum.DolumDegeri, Is.EqualTo(0));
            Assert.That(o.OlaylarTurunde<DolumGostergesiDegisti>(), Is.Empty);
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.9.1, 3.9.2, 3.9.3")]
        public void Takviye_tasi_hedeflere_en_yakin_normal_tas_hucresine_konur_cizgi_yonu_temizleme_puanina_gore_secilir(int tohum)
        {
            // Satır 5 engellerle dolu; hedef (5,5)'teki blok. Satırın temizleme puanı sütununkinden yüksek.
            var o = TestOturumu.Kur(
                NotrCizim(5, 5, (5, 1, '1'), (5, 2, '1'), (5, 3, '1'), (5, 4, '1'), (5, 5, '1')),
                hedefler: new[] { new Hedef(KatmanliBlokHedefi, 0) },
                tohum: tohum);

            var konum = o.Oturum.TakviyeKoy(TasTuru.Cizgi, o.Olaylar);

            Assert.That(konum.HasValue, Is.True);
            Assert.That(konum!.Value.Satir, Is.EqualTo(4), "Hedeflere en yakın normal taş hücresi.");
            var tas = o.Tahta[konum.Value].Tas!;
            Assert.That(tas.Tur, Is.EqualTo(TasTuru.Cizgi));
            Assert.That(tas.CizgiYonu, Is.EqualTo(Yon.Dikey), "Satır 4'te engel yok; sütunda bir hedef blok var.");
            Assert.That(o.OlaylarTurunde<TakviyeKondu>().Single().Tas, Is.SameAs(tas));
        }

        [Test]
        [Property("GDD", "2.3.7, 7.3.2")]
        public void Bolum_bittikten_sonra_hamle_ve_arac_kabul_edilmez()
        {
            var o = TestOturumu.Kur(
                UcluTahtasi, hamle: 5, hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 3) });

            o.Kaydir(H(4, 3), H(3, 3));

            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(o.Kaydir(H(1, 1), H(1, 2)), Is.EqualTo(HamleSonucu.Reddedildi));
            Assert.That(o.Dokun(H(1, 1)), Is.EqualTo(HamleSonucu.Reddedildi));
            Assert.That(o.Arac(AracTuru.TekHucre, H(1, 1)), Is.False);
        }
    }
}
