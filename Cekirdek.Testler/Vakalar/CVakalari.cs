using System;
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
    /// <summary>test_vakalari.md — C. Kombinasyonlar: 10 çiftin her biri.</summary>
    [TestFixture]
    public class CVakalari
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };

        /// <summary>C07–C10 ortak tahtası; ? = vakadaki ikinci özel taş. Dağılım: M 9, K 5, Y 5, S 4.</summary>
        private static string OrtakTahta(char ikinciOzelTas) => @"
               1 2 3 4 5
            1  M K M Y S
            2  K M S M K
            3  M Y R ? M
            4  S K M Y S
            5  Y M K M Y".Replace('?', ikinciOzelTas);

        private static IEnumerable<Konum> TumHucreler(int satirSayisi, int sutunSayisi) =>
            from s in Enumerable.Range(1, satirSayisi)
            from t in Enumerable.Range(1, sutunSayisi)
            select H(s, t);

        private static int HalkaUzakligi(Konum a, Konum b) =>
            Math.Max(Math.Abs(a.Satir - b.Satir), Math.Abs(a.Sutun - b.Sutun));

        [Test]
        [Category("C01")]
        [Property("GDD", "4.1.1, 4.1.2, 4.3.1")]
        public void C01_Cizgi_cizgi_arti_sekli_iki_cizgi_ayni_yonde_olsa_bile()
        {
            var d = HamleDenemesi.Kaydir(NotrCizim(7, 7, (4, 3, '-'), (4, 4, '-')), H(4, 3), H(4, 4));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            var kombinasyon = d.Olaylar.OfType<KombinasyonCalisti>().Single();
            Assert.That(kombinasyon.Tur, Is.EqualTo(KombinasyonTuru.CizgiCizgi));
            Assert.That(kombinasyon.Merkez, Is.EqualTo(H(4, 4)), "Merkez gidilen hücredir.");
            Assert.That(
                d.VurulanHucreler,
                Is.EquivalentTo(TumHucreler(7, 7).Where(k => k.Satir == 4 || k.Sutun == 4)));
            Assert.That(d.Etkinlesenler.Select(e => e.Tetik), Is.All.EqualTo(Tetik.Kombinasyon));
            Assert.That(d.Etkinlesenler, Has.Count.EqualTo(2));
        }

        [Test]
        [Category("C02")]
        [Property("GDD", "4.1.2, 4.3.2")]
        public void C02_Bomba_cizgi_uc_satir_ve_uc_sutun()
        {
            var d = HamleDenemesi.Kaydir(NotrCizim(7, 7, (4, 3, 'B'), (4, 4, '-')), H(4, 3), H(4, 4));

            Assert.That(d.Olaylar.OfType<KombinasyonCalisti>().Single().Tur, Is.EqualTo(KombinasyonTuru.BombaCizgi));
            Assert.That(
                d.VurulanHucreler,
                Is.EquivalentTo(TumHucreler(7, 7).Where(k =>
                    (k.Satir >= 3 && k.Satir <= 5) || (k.Sutun >= 3 && k.Sutun <= 5))));
            Assert.That(d.Vuruslar, Has.Count.EqualTo(33), "Yorum: kesişen hücreler bir kez vurulur.");
        }

        [Test]
        [Category("C03")]
        [Property("GDD", "3.3.4, 4.3.3, 3.4.8")]
        [Property("AK", "AK-12 A")]
        public void C03_Bomba_bomba_tek_bir_9x9_patlama()
        {
            var d = HamleDenemesi.Kaydir(NotrCizim(9, 9, (5, 5, 'B'), (5, 6, 'B')), H(5, 6), H(5, 5));

            var kombinasyon = d.Olaylar.OfType<KombinasyonCalisti>().Single();
            Assert.That(kombinasyon.Tur, Is.EqualTo(KombinasyonTuru.BombaBomba));
            Assert.That(kombinasyon.Merkez, Is.EqualTo(H(5, 5)));
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(TumHucreler(9, 9)), "9×9 tahtanın tamamı vurulur.");
            Assert.That(d.Vuruslar, Has.Count.EqualTo(81), "Tek patlama: bombalardan biri devre dışı kalır.");

            // İçten dışa halkalar.
            long merkez = d.IlkVurusAdimi(H(5, 5));
            foreach (var vurus in d.Vuruslar)
            {
                Assert.That(vurus.Adim - merkez, Is.EqualTo(HalkaUzakligi(vurus.Konum, H(5, 5))));
            }

            Assert.That(d.Kalkislar, Is.Empty, "Güdümlü taş havalanmaz.");
        }

        [Test]
        [Category("C03")]
        [Property("GDD", "4.3.3")]
        [Property("AK", "AK-12 B")]
        public void C03_AK12_B_merkez_cikilan_hucredir_sutun_1_vurulmaz()
        {
            var d = HamleDenemesi.Kaydir(
                NotrCizim(9, 9, (5, 5, 'B'), (5, 6, 'B')),
                H(5, 6),
                H(5, 5),
                AkBayraklari.Varsayilan.Ile(Ak.BombaBombaMerkezi, Varyant.B));

            Assert.That(d.Olaylar.OfType<KombinasyonCalisti>().Single().Merkez, Is.EqualTo(H(5, 6)));
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(TumHucreler(9, 9).Where(k => k.Sutun >= 2)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C04")]
        [Property("GDD", "3.4.8, 4.3.4")]
        public void C04_Gudumlu_cizgi_bir_gudumlu_havalanir_ve_bir_satirin_tamami_temizlenir(int tohum)
        {
            var d = HamleDenemesi.Kaydir(
                NotrCizim(7, 7, (4, 3, 'G'), (4, 4, '-')), H(4, 3), H(4, 4), tohum: tohum);

            Assert.That(d.Kalkislar, Has.Count.EqualTo(1));
            Assert.That(d.Kalkislar[0].Yuk, Is.EqualTo(GudumluYuku.Cizgi));

            var inis = d.Inisler.Single();
            var cizgiVuruslari = d.Vuruslar.Where(v => v.Tur == VurusTuru.Cizgi).Select(v => v.Konum).ToList();
            Assert.That(
                cizgiVuruslari,
                Is.EquivalentTo(Enumerable.Range(1, 7).Select(t => H(inis.Hedef.Satir, t))),
                "Taşınan çizgi taşı yatay olduğu için bir satırın tamamı temizlenir.");
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C04")]
        [Property("GDD", "3.4.5, 4.3.4")]
        public void C04_Temizlenen_satir_hedef_puanlari_toplami_en_yuksek_olan_satirdir(int tohum)
        {
            // Satır 7'ye üç blok konur; engel puanı normal taştan yüksek olduğu için en iyi satır 7'dir.
            var d = HamleDenemesi.Kaydir(
                NotrCizim(7, 7, (4, 3, 'G'), (4, 4, '-'), (7, 1, '1'), (7, 4, '1'), (7, 7, '1')),
                H(4, 3),
                H(4, 4),
                tohum: tohum);

            Assert.That(d.Inisler.Single().Hedef.Satir, Is.EqualTo(7));
            Assert.That(d.Olaylar.OfType<EngelHasarAldi>().Count(h => h.Kirildi), Is.EqualTo(3));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C04")]
        [Property("GDD", "4.3.4")]
        public void C04_Dikey_cizgi_tasi_en_iyi_sutuna_tasinir(int tohum)
        {
            var d = HamleDenemesi.Kaydir(
                NotrCizim(7, 7, (4, 3, 'G'), (4, 4, '|'), (1, 7, '1'), (4, 7, '1'), (7, 7, '1')),
                H(4, 3),
                H(4, 4),
                tohum: tohum);

            Assert.That(d.Inisler.Single().Hedef.Sutun, Is.EqualTo(7));
            Assert.That(
                d.Vuruslar.Where(v => v.Tur == VurusTuru.Cizgi).Select(v => v.Konum),
                Is.EquivalentTo(Enumerable.Range(1, 7).Select(s => H(s, 7))));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C05")]
        [Property("GDD", "3.4.8, 4.3.5")]
        public void C05_Gudumlu_bomba_bir_gudumlu_havalanir_indigi_yerde_5x5_vurulur(int tohum)
        {
            // Sol üst köşeye dört blok konur: en iyi bölge bu blokları içine alan 5×5 alandır.
            var d = HamleDenemesi.Kaydir(
                NotrCizim(7, 7, (4, 3, 'G'), (4, 4, 'B'), (1, 1, '1'), (1, 2, '1'), (2, 1, '1'), (2, 2, '1')),
                H(4, 3),
                H(4, 4),
                tohum: tohum);

            Assert.That(d.Kalkislar, Has.Count.EqualTo(1));
            Assert.That(d.Kalkislar[0].Yuk, Is.EqualTo(GudumluYuku.Bomba));

            var inis = d.Inisler.Single();
            var bombaVuruslari = d.Vuruslar.Where(v => v.Tur == VurusTuru.Bomba).Select(v => v.Konum).ToList();
            Assert.That(
                bombaVuruslari,
                Is.EquivalentTo(TumHucreler(7, 7).Where(k => HalkaUzakligi(k, inis.Hedef) <= 2)),
                "5×5 kare tahta sınırında kırpılır; 9×9 değildir.");
            Assert.That(bombaVuruslari.Count, Is.LessThanOrEqualTo(25));
            Assert.That(d.Olaylar.OfType<EngelHasarAldi>().Count(h => h.Kirildi), Is.EqualTo(4));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C06")]
        [Property("GDD", "3.4.4, 3.4.8, 4.3.6")]
        public void C06_Gudumlu_gudumlu_uc_gudumlu_tas_uc_ayri_hedef(int tohum)
        {
            var bloklar = new[] { H(1, 1), H(1, 7), H(7, 1), H(7, 7), H(1, 4) };
            var d = HamleDenemesi.Kaydir(
                NotrCizim(
                    7, 7, (4, 3, 'G'), (4, 4, 'G'), (1, 1, '1'), (1, 7, '1'), (7, 1, '1'), (7, 7, '1'), (1, 4, '1')),
                H(4, 3),
                H(4, 4),
                tohum: tohum);

            Assert.That(d.Kalkislar, Has.Count.EqualTo(3));
            Assert.That(d.Kalkislar.Select(k => k.Yuk), Is.All.EqualTo(GudumluYuku.Yok));
            Assert.That(d.Kalkislar.Select(k => k.Kalkis), Is.All.EqualTo(H(4, 4)));

            var hedefler = d.Inisler.Select(i => i.Hedef).ToList();
            Assert.That(hedefler, Has.Count.EqualTo(3));
            Assert.That(hedefler.Distinct().Count(), Is.EqualTo(3), "İki güdümlü taş aynı hedefe gitmez.");
            Assert.That(hedefler, Is.SubsetOf(bloklar));

            // Kalkıştan önce gidilen hücrede tek bir artı alanı patlar; uçanlar kendi artılarını patlatmaz.
            Assert.That(
                d.Vuruslar.Where(v => v.Tur == VurusTuru.Gudumlu && v.Adim < d.Inisler[0].Adim).Select(v => v.Konum),
                Is.EquivalentTo(new[] { H(4, 4), H(3, 4), H(5, 4), H(4, 3), H(4, 5) }));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C07")]
        [Property("GDD", "3.5.2, 4.1.5, 4.3.7")]
        public void C07_Renk_kuresi_cizgi_en_yaygin_rengin_taslari_cizgi_tasina_doner(int tohum)
        {
            var d = HamleDenemesi.Kaydir(OrtakTahta('-'), H(3, 3), H(3, 4), tohum: tohum);

            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Mavi));

            var donusumler = d.Donusumler;
            Assert.That(donusumler, Has.Count.EqualTo(10), "9 M ve kürenin hücresi.");
            Assert.That(donusumler.Select(x => x.YeniTas.Tur), Is.All.EqualTo(TasTuru.Cizgi));
            Assert.That(donusumler.Count(x => x.EskiTas != null && x.EskiTas.Renk == Mavi), Is.EqualTo(9));
            Assert.That(donusumler.Single(x => x.EskiTas == null).Konum, Is.EqualTo(H(3, 4)));

            // Yönler dönüşüm sırasına göre bir yatay bir dikey değişir.
            Assert.That(
                donusumler.Select(x => x.YeniTas.CizgiYonu!.Value),
                Is.EqualTo(Enumerable.Range(0, 10).Select(i => i % 2 == 0 ? Yon.Yatay : Yon.Dikey)));

            // Her çizgi taşı tam bir kez etkinleşir; sırası gelenler dönüşüm sırasıyla, birer adım arayla.
            var yeniTaslar = donusumler.Select(x => x.YeniTas).ToList();
            var etkinlesmeler = d.Etkinlesenler.Where(e => yeniTaslar.Contains(e.Tas)).ToList();
            Assert.That(etkinlesmeler.Select(e => e.Tas), Is.EquivalentTo(yeniTaslar));

            var sirayla = etkinlesmeler.Where(e => e.Tetik == Tetik.Donusum).ToList();
            Assert.That(sirayla, Is.Not.Empty);
            Assert.That(sirayla.Select(e => yeniTaslar.IndexOf(e.Tas)), Is.Ordered);
            foreach (var e in sirayla)
            {
                Assert.That(e.Adim, Is.EqualTo(donusumler[0].Adim + 1 + yeniTaslar.IndexOf(e.Tas)));
            }
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C08")]
        [Property("GDD", "3.5.2, 4.1.5, 4.3.8")]
        public void C08_Renk_kuresi_bomba_en_yaygin_rengin_taslari_bomba_tasina_doner(int tohum)
        {
            var d = HamleDenemesi.Kaydir(OrtakTahta('B'), H(3, 3), H(3, 4), tohum: tohum);

            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Mavi));

            var donusumler = d.Donusumler;
            Assert.That(donusumler.Count(x => x.EskiTas != null && x.EskiTas.Renk == Mavi), Is.EqualTo(9));
            Assert.That(donusumler.Select(x => x.YeniTas.Tur), Is.All.EqualTo(TasTuru.Bomba));

            // Bütün dönüşümler bitince bomba taşları aynı adımda patlar.
            var yeniTaslar = donusumler.Select(x => x.YeniTas).ToList();
            var etkinlesmeler = d.Etkinlesenler.Where(e => yeniTaslar.Contains(e.Tas)).ToList();
            Assert.That(etkinlesmeler, Has.Count.EqualTo(9));
            Assert.That(etkinlesmeler.Select(e => e.Tetik), Is.All.EqualTo(Tetik.Donusum));
            Assert.That(etkinlesmeler.Select(e => e.Adim).Distinct().Count(), Is.EqualTo(1));
            Assert.That(etkinlesmeler[0].Adim, Is.GreaterThan(donusumler.Max(x => x.Adim)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C09")]
        [Property("GDD", "3.4.2, 3.4.8, 4.3.9")]
        [Property("AK", "AK-07 A")]
        public void C09_Renk_kuresi_gudumlu_en_yaygin_rengin_taslari_gudumlu_tasa_doner(int tohum)
        {
            var d = HamleDenemesi.Kaydir(OrtakTahta('G'), H(3, 3), H(3, 4), tohum: tohum);

            Assert.That(d.SecilenRenkler.Single().Renk, Is.EqualTo(Mavi));
            Assert.That(d.Donusumler.Count(x => x.EskiTas != null && x.EskiTas.Renk == Mavi), Is.EqualTo(9));
            Assert.That(d.Donusumler.Select(x => x.YeniTas.Tur), Is.All.EqualTo(TasTuru.Gudumlu));
            Assert.That(d.Donusumler.Single(x => x.EskiTas == null).Konum, Is.EqualTo(H(3, 4)));

            // Uçan güdümlü sayısı = dönüşen taş sayısı + 1.
            Assert.That(d.Kalkislar, Has.Count.EqualTo(10));

            // Dönüşüm sırasıyla birer birer havalanırlar.
            Assert.That(d.Kalkislar.Select(k => k.Kalkis), Is.EqualTo(d.Donusumler.Select(x => x.Konum)));
            Assert.That(d.Kalkislar.Select(k => k.Adim), Is.Ordered.Ascending);
            Assert.That(d.Kalkislar.Select(k => k.Adim).Distinct().Count(), Is.EqualTo(10));

            // Dönüşen güdümlüler kalkıştan önce artı alanlarını patlatmaz: her güdümlü vuruşu bir iniştir.
            Assert.That(
                d.Vuruslar.Where(v => v.Tur == VurusTuru.Gudumlu).Select(v => v.Konum),
                Is.EqualTo(d.Inisler.Select(i => i.Hedef)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Category("C09")]
        [Property("GDD", "4.3.9")]
        [Property("AK", "AK-07 B")]
        public void C09_AK07_B_kurenin_hucresine_gudumlu_konmaz(int tohum)
        {
            var d = HamleDenemesi.Kaydir(
                OrtakTahta('G'),
                H(3, 3),
                H(3, 4),
                AkBayraklari.Varsayilan.Ile(Ak.RenkGudumluEkTas, Varyant.B),
                tohum);

            Assert.That(d.Donusumler, Has.Count.EqualTo(9));
            Assert.That(d.Kalkislar, Has.Count.EqualTo(9));
        }

        [Test]
        [Category("C10")]
        [Property("GDD", "4.3.10")]
        public void C10_Renk_kuresi_renk_kuresi_butun_tahta_temizlenir()
        {
            var d = HamleDenemesi.Kaydir(OrtakTahta('R'), H(3, 3), H(3, 4));

            Assert.That(d.Olaylar.OfType<KombinasyonCalisti>().Single().Tur, Is.EqualTo(KombinasyonTuru.KureKure));
            Assert.That(d.VurulanHucreler, Is.EquivalentTo(TumHucreler(5, 5)));
            Assert.That(d.Vuruslar, Has.Count.EqualTo(25));
            Assert.That(d.Tahta.TumKonumlar().Select(k => d.Tahta[k].Tas), Is.All.Null);

            // Merkezden dışa doğru halkalar: önce merkeze yakın hücreler, en son köşeler.
            long merkez = d.IlkVurusAdimi(H(3, 4));
            foreach (var vurus in d.Vuruslar)
            {
                Assert.That(vurus.Adim - merkez, Is.EqualTo(HalkaUzakligi(vurus.Konum, H(3, 4))));
            }

            long sonAdim = d.Vuruslar.Max(v => v.Adim);
            Assert.That(d.IlkVurusAdimi(H(1, 1)), Is.EqualTo(sonAdim));
            Assert.That(d.IlkVurusAdimi(H(5, 1)), Is.EqualTo(sonAdim));
        }

        [Test]
        [Category("C10")]
        [Property("GDD", "4.4.2")]
        public void C10_Cift_renk_modunda_tahta_iki_tur_temizlenir()
        {
            var d = HamleDenemesi.Kaydir(OrtakTahta('R'), H(3, 3), H(3, 4), ciftRenk: true);

            Assert.That(d.Vuruslar, Has.Count.EqualTo(50));
            foreach (var hucre in TumHucreler(5, 5))
            {
                Assert.That(d.Vuruslar.Count(v => v.Konum == hucre), Is.EqualTo(2));
            }

            // İkinci tur, ilk tur bitince çalışır.
            long ilkTurSonu = d.Vuruslar.Take(25).Max(v => v.Adim);
            Assert.That(d.Vuruslar.Skip(25).Min(v => v.Adim), Is.GreaterThan(ilkTurSonu));
        }
    }
}
