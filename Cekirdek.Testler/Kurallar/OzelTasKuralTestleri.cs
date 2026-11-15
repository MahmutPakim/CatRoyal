using System.Collections.Generic;
using System.Linq;
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
    /// <summary>GDD 3 ve 4'ün test_vakalari.md'de doğrudan vakası olmayan kuralları ve B varyantları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class OzelTasKuralTestleri
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };

        [Test]
        [Property("GDD", "2.3.5, 2.3.6")]
        public void Hedefi_olmayan_renk_kuresine_ya_da_normal_tasa_dokunmak_hamle_sayilmaz()
        {
            var kure = HamleDenemesi.Dokun(@"
                   1 2 3
                1  B R -
                2  G | B", H(1, 2));
            Assert.That(kure.Sonuc, Is.EqualTo(HamleSonucu.EtkisizDokunma));
            Assert.That(kure.Olaylar, Is.Empty);
            Assert.That(kure.Tahta[1, 2].Tas!.Tur, Is.EqualTo(TasTuru.RenkKuresi));

            var normal = HamleDenemesi.Dokun(NotrCizim(3, 3), H(2, 2));
            Assert.That(normal.Sonuc, Is.EqualTo(HamleSonucu.EtkisizDokunma));
            Assert.That(normal.Olaylar, Is.Empty);
        }

        private const string EsitRenkliTahta = @"
               1 2 3 4
            1  S K M R
            2  M K S ·
            3  K M S ·";

        [Test]
        [Property("GDD", "3.5.2")]
        [Property("AK", "AK-08 A")]
        public void En_yaygin_renkte_esitlikte_taramada_ilk_gorulen_renk_secilir()
        {
            var d = HamleDenemesi.Dokun(EsitRenkliTahta, H(1, 4));

            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Sari), "K, M ve S üçer tane; taramada ilk görülen S.");
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.5.2")]
        [Property("AK", "AK-08 B")]
        public void AK08_B_esit_renkler_arasindan_rastgele_secilir(int tohum)
        {
            var bayraklar = AkBayraklari.Varsayilan.Ile(Ak.RenkEsitlikSecimi, Varyant.B);

            var d = HamleDenemesi.Dokun(EsitRenkliTahta, H(1, 4), bayraklar, tohum);
            var tekrar = HamleDenemesi.Dokun(EsitRenkliTahta, H(1, 4), bayraklar, tohum);

            int renk = d.SecilenRenkler.Single().Renk;
            Assert.That(new[] { Kirmizi, Mavi, Sari }, Does.Contain(renk));
            Assert.That(tekrar.SecilenRenkler.Single().Renk, Is.EqualTo(renk), "Aynı tohum aynı sonucu verir.");
            Assert.That(d.PatlayanTaslar, Has.Count.EqualTo(3));
        }

        [Test]
        [Property("GDD", "3.5.2")]
        [Property("AK", "AK-08 B")]
        public void AK08_B_farkli_tohumlar_farkli_renkler_secebilir()
        {
            var bayraklar = AkBayraklari.Varsayilan.Ile(Ak.RenkEsitlikSecimi, Varyant.B);

            var renkler = Enumerable.Range(1, 40)
                .Select(tohum => HamleDenemesi.Dokun(EsitRenkliTahta, H(1, 4), bayraklar, tohum).SecilenRenkler.Single().Renk)
                .Distinct()
                .ToList();

            Assert.That(renkler, Has.Count.GreaterThan(1));
        }

        [Test]
        [Property("GDD", "3.6.1, 3.6.3")]
        [Property("AK", "AK-14 A")]
        public void Cift_renk_kuresi_iki_renk_temizler_ikinci_renk_o_anki_en_yaygin_renktir()
        {
            var d = HamleDenemesi.Dokun(BVakalari.B07Tahtasi, H(3, 3), ciftRenk: true);

            // Dağılım M 9, K 6, Y 5, S 4: önce M, sonra K.
            Assert.That(d.SecilenRenkler.Select(s => s.Renk), Is.EqualTo(new[] { Mavi, Kirmizi }));
            Assert.That(d.PatlayanTaslar.Count(p => p.Tas.Renk == Mavi), Is.EqualTo(9));
            Assert.That(d.PatlayanTaslar.Count(p => p.Tas.Renk == Kirmizi), Is.EqualTo(6));
            Assert.That(d.PatlayanTaslar, Has.Count.EqualTo(15), "Toplam en fazla iki renk temizlenir.");

            // İkinci renk, birinci rengin bütün hedefleri vurulduktan sonra seçilir.
            long sonMavi = d.PatlayanTaslar.Where(p => p.Tas.Renk == Mavi).Max(p => p.Adim);
            Assert.That(d.SecilenRenkler[1].Adim, Is.GreaterThan(sonMavi));
            Assert.That(d.Olaylar.OfType<RenkKuresiTukendi>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "3.6.1")]
        public void Cift_renk_modu_kapaliyken_renk_kuresi_tek_renk_temizler()
        {
            var d = HamleDenemesi.Dokun(BVakalari.B07Tahtasi, H(3, 3));

            Assert.That(d.SecilenRenkler, Has.Count.EqualTo(1));
        }

        [TestCase(Varyant.A, 2)]
        [TestCase(Varyant.B, 1)]
        [Property("GDD", "4.4.1")]
        [Property("AK", "AK-15")]
        public void Cift_renk_modunda_kure_bomba_kombinasyonu_AK15_e_gore_iki_ya_da_tek_renk_donusturur(
            Varyant ak15, int renkSayisi)
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  M K M Y S
                2  K M S M K
                3  M Y R B M
                4  S K M Y S
                5  Y M K M Y",
                H(3, 3),
                H(3, 4),
                AkBayraklari.Varsayilan.Ile(Ak.CiftRenkKombinasyon, ak15),
                ciftRenk: true);

            Assert.That(d.SecilenRenkler, Has.Count.EqualTo(renkSayisi));
            Assert.That(d.SecilenRenkler[0].Renk, Is.EqualTo(Mavi));
            Assert.That(d.SecilenRenkler.Select(s => s.Renk).Distinct().Count(), Is.EqualTo(renkSayisi));
            Assert.That(d.Donusumler.Select(x => x.YeniTas.Tur), Is.All.EqualTo(TasTuru.Bomba));
        }

        [TestCase(false, 4)]
        [TestCase(true, 2)]
        [Property("GDD", "3.2.4, 11.2.1")]
        public void Cizgi_kanadi_oynanmayan_hucreyi_atlar_bayrak_aciksa_orada_durur(bool durur, int vurulan)
        {
            var d = HamleDenemesi.Dokun(@"
                   1 2 3 4 5
                1  - · X · ·
                2  · · · · ·", H(1, 1), AkBayraklari.Varsayilan.CizgiOynanmayanHucredeDururIle(durur));

            Assert.That(d.VurulanHucreler, Has.Count.EqualTo(vurulan));
            Assert.That(d.VurulanHucreler.Contains(H(1, 5)), Is.EqualTo(!durur));
            Assert.That(d.VurulanHucreler, Does.Not.Contain(H(1, 3)));
        }

        [Test]
        [Property("GDD", "5.7, 11.2.29")]
        public void Ozel_tas_normal_tasla_kaydirilinca_normal_tasin_eslesmesi_ozel_tasin_etkisinden_once_patlar()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  K K - · ·
                3  · · K · ·
                4  · · · · ·", H(3, 3), H(2, 3));

            // K (2,3)'e çıkar ve satır 2'de 3'lü kurar; çizgi taşı (3,3)'e iner ve satır 3'ü temizler.
            var olaylar = d.Olaylar.ToList();
            int grup = olaylar.FindIndex(o => o is GrupPatladi);
            int etkinlesme = olaylar.FindIndex(o => o is OzelTasEtkinlesti);
            Assert.That(grup, Is.GreaterThanOrEqualTo(0));
            Assert.That(etkinlesme, Is.GreaterThan(grup));
            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(2, 1), H(2, 2), H(2, 3) }));
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(Enumerable.Range(1, 5).Select(t => H(3, t))));
        }

        [Test]
        [Property("GDD", "3.1.3")]
        public void Ayni_hamlede_olusan_ozel_tas_ayni_hamlenin_patlamasinda_etkinlesebilir()
        {
            var d = HamleDenemesi.Kaydir(@"
                   1 2 3 4 5 6
                1  · · · · · ·
                2  K K K | K ·
                3  · · · K · ·
                4  · · · · · ·", H(3, 4), H(2, 4));

            // K (2,4)'e çıkar: satır 2'de 5'li → (2,4)'te renk küresi doğar. Çizgi taşı (3,4)'e iner,
            // sütun 4'ü temizlerken yeni doğan küreyi vurur ve küre zincire girer.
            Assert.That(d.DoganOzelTaslar.Single().Tas.Tur, Is.EqualTo(TasTuru.RenkKuresi));
            Assert.That(d.Olaylar.OfType<RenkKuresiTetiklendi>().Single().Kure, Is.SameAs(d.DoganOzelTaslar.Single().Tas));
        }

        [Test]
        [Property("GDD", "4.1.1")]
        public void Kombinasyonun_sonucunu_yalniz_iki_tasin_turu_belirler_hangisinin_kaydirildigi_onemsizdir()
        {
            var ileri = HamleDenemesi.Kaydir(NotrCizim(7, 7, (4, 3, 'B'), (4, 4, '-')), H(4, 3), H(4, 4));
            var geri = HamleDenemesi.Kaydir(NotrCizim(7, 7, (4, 3, '-'), (4, 4, 'B')), H(4, 3), H(4, 4));

            Assert.That(geri.Olaylar.OfType<KombinasyonCalisti>().Single().Tur, Is.EqualTo(KombinasyonTuru.BombaCizgi));
            Assert.That(geri.VurulanHucreler, Is.EquivalentTo(ileri.VurulanHucreler));
        }

        [Test]
        [Property("GDD", "4.1.2, 4.3.2, 3.3.3")]
        public void Kenardaki_bomba_cizgi_kombinasyonu_tahta_disindaki_sirayi_yok_sayar()
        {
            var d = HamleDenemesi.Kaydir(NotrCizim(5, 5, (1, 2, 'B'), (1, 1, '|')), H(1, 2), H(1, 1));

            Assert.That(
                d.VurulanHucreler,
                Is.EquivalentTo(d.Tahta.TumKonumlar().Where(k => k.Satir <= 2 || k.Sutun <= 2)));
        }

        // ------------------------------------------------------------------ Güdümlü hedef seçimi

        private static HamleDenemesiSonucu GudumluyeDokun(
            string cizim, Konum gudumlu, AkBayraklari bayraklar, int tohum, ISet<Konum> hedefOgeleri)
        {
            var test = TahtaKurucu.Kur(cizim);
            var baglam = HamleDenemesi.BaglamKur(test.Tahta, bayraklar, tohum);
            baglam.HedefOgesiMi = hedefOgeleri.Contains;
            var olaylar = new List<Olay>();
            HamleIsleyici.Dokun(baglam, gudumlu, olaylar, HamleKapsami.YalnizPatlama);
            return new HamleDenemesiSonucu(olaylar);
        }

        private sealed class HamleDenemesiSonucu
        {
            public HamleDenemesiSonucu(List<Olay> olaylar)
            {
                Olaylar = olaylar;
            }

            public List<Olay> Olaylar { get; }

            public Konum Inis => Olaylar.OfType<GudumluIndi>().Single().Hedef;
        }

        private const string IkiBlokluTahta = @"
               1 2 3 4 5 6 7
            1  · · · · · · 3
            2  · · · · · · ·
            3  · · G · · · ·
            4  · · · · · · ·
            5  1 · · · · · ·";

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.4.4, 3.4.5")]
        [Property("AK", "AK-05 A")]
        public void AK05_A_hedef_oge_hedef_olmayan_engelden_katmani_cok_olan_azdan_once_gelir(int tohum)
        {
            // Hedef yokken: 3 katmanlı blok (40 + 3×10) tek katmanlıdan (40 + 10) yüksek puanlıdır.
            var hedefsiz = GudumluyeDokun(
                IkiBlokluTahta, H(3, 3), AkBayraklari.Varsayilan, tohum, new HashSet<Konum>());
            Assert.That(hedefsiz.Inis, Is.EqualTo(H(1, 7)));

            // Tek katmanlı blok bölüm hedefiyse (100 + 10) öne geçer.
            var hedefli = GudumluyeDokun(
                IkiBlokluTahta, H(3, 3), AkBayraklari.Varsayilan, tohum, new HashSet<Konum> { H(5, 1) });
            Assert.That(hedefli.Inis, Is.EqualTo(H(5, 1)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.4.5")]
        [Property("AK", "AK-05 B")]
        public void AK05_B_hedef_ogeler_arasindan_gudumlu_tasa_en_yakin_olan_secilir(int tohum)
        {
            var bayraklar = AkBayraklari.Varsayilan.Ile(Ak.GudumluHedefSecimi, Varyant.B);

            // İki blok da hedef: A'da 3 katmanlı (1,7) kazanırdı; B'de en yakın olan (5,1) seçilir.
            var d = GudumluyeDokun(
                IkiBlokluTahta, H(3, 3), bayraklar, tohum, new HashSet<Konum> { H(1, 7), H(5, 1) });

            Assert.That(d.Inis, Is.EqualTo(H(5, 1)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.4.6")]
        public void Gudumlu_onceligi_acik_engel_kaldikca_gudumlu_taslar_once_ona_gider(int tohum)
        {
            var test = TahtaKurucu.Kur(IkiBlokluTahta);
            var oncelikli = Arketipler.KatmanliBlok();
            oncelikli.GudumluOnceligi = true;
            test.Tahta[5, 1].Tas = test.Tahta.EngelUret(oncelikli, 1);

            var d = HamleDenemesi.Dokun(test, H(3, 3), tohum: tohum);

            Assert.That(d.Inisler.Single().Hedef, Is.EqualTo(H(5, 1)), "Puanı daha yüksek olan (1,7) yerine öncelikli engel.");
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "3.4.4")]
        public void Gudumlu_tas_hedef_olarak_ozel_tas_bos_ya_da_oynanmayan_hucre_secmez(int tohum)
        {
            var d = HamleDenemesi.Dokun(@"
                   1 2 3 4
                1  G _ X B
                2  _ _ K -", H(1, 1), tohum: tohum);

            Assert.That(d.Inisler.Single().Hedef, Is.EqualTo(H(2, 3)));
            Assert.That(d.Etkinlesenler, Has.Count.EqualTo(1), "Bomba ve çizgi taşı tetiklenmez.");
        }

        [Test]
        [Property("GDD", "3.4.3")]
        public void Tahtada_aday_yoksa_gudumlu_tas_inmez()
        {
            var d = HamleDenemesi.Dokun(@"
                   1 2 3
                1  G _ _
                2  _ _ B", H(1, 1));

            Assert.That(d.Kalkislar.Single().Hedef, Is.Null);
            Assert.That(d.Inisler, Is.Empty);
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "0.4")]
        public void Ozellik_ayni_tohum_ayni_olay_dizisini_uretir(int tohum)
        {
            string Ozet(HamleDenemesi d) => string.Join(
                "|",
                d.Olaylar.Select(o => o is AdimliOlay a ? o.GetType().Name + "@" + a.Adim : o.GetType().Name)
                    .Concat(d.Inisler.Select(i => i.Hedef.ToString())));

            const string cizim = @"
                   1 2 3 4 5
                1  M K M Y S
                2  K M S M K
                3  M Y R G M
                4  S K M Y S
                5  Y M K M Y";

            Assert.That(
                Ozet(HamleDenemesi.Kaydir(cizim, H(3, 3), H(3, 4), tohum: tohum)),
                Is.EqualTo(Ozet(HamleDenemesi.Kaydir(cizim, H(3, 3), H(3, 4), tohum: tohum))));
        }
    }
}
