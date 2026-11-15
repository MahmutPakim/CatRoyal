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
    /// <summary>test_vakalari.md — E. Engeller: hasar, düşme, kırılma.</summary>
    [TestFixture]
    public class EVakalari
    {
        [Test]
        [Category("E01")]
        [Property("GDD", "6.1.1, 6.1.2, 6.2.1")]
        public void E01_Katmanli_blok_dik_komsusundaki_eslesmeden_bir_katman_kaybeder()
        {
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · 2 · ·
                3  K K M · ·
                4  · · K · ·", H(4, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));

            var hasar = d.Olaylar.OfType<EngelHasarAldi>().Single();
            Assert.That(hasar.Konum, Is.EqualTo(H(2, 3)));
            Assert.That(hasar.KalanKatman, Is.EqualTo(1));
            Assert.That(hasar.Kirildi, Is.False);

            var blok = d.Tahta[2, 3].Tas!;
            Assert.That(blok.Tur, Is.EqualTo(TasTuru.Engel), "Blok kırılmaz ve yerinde kalır.");
            Assert.That(blok.Katman, Is.EqualTo(1));
        }

        [Test]
        [Category("E02")]
        [Property("GDD", "6.1.1, 6.1.9, 5.1.2, 6.2.1")]
        public void E02_Son_katman_kirilinca_hucre_bosalir_ve_ustteki_tas_duser()
        {
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · · a · ·
                2  · · 1 · ·
                3  K K M · ·
                4  · · K · ·", H(4, 3), H(3, 3));

            var hasar = d.Olaylar.OfType<EngelHasarAldi>().Single();
            Assert.That(hasar.Konum, Is.EqualTo(H(2, 3)));
            Assert.That(hasar.Kirildi, Is.True);

            // a aşağı düşer ve (3,3)'e iner, çünkü (4,3)'te M var.
            Assert.That(d.Test.IzlenenKonumu('a'), Is.EqualTo(H(3, 3)));
            Assert.That(d.Tahta[4, 3].Tas!.Renk, Is.EqualTo(Mavi));

            // Katmanlı blok düşmediği için hiçbir aşamada yer değiştirmez.
            Assert.That(d.Dusmeler.Select(x => x.Tas), Has.None.SameAs(hasar.Engel));
            Assert.That(d.CaprazKaymalar, Is.Empty);
        }

        [Test]
        [Category("E03")]
        [Property("GDD", "6.1.2")]
        public void E03_Capraz_komsu_yan_eslesme_hasari_almaz()
        {
            var d = HamleDenemesi.TamKaydir(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · 1 ·
                3  K K M · ·
                4  · · K · ·", H(4, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));
            Assert.That(d.Olaylar.OfType<EngelHasarAldi>(), Is.Empty);
            Assert.That(d.Tahta[2, 4].Tas!.Katman, Is.EqualTo(1));
        }

        private const string E04Tahtasi = @"
               1 2 3 4 5
            1  K · · · ·
            2  K 2 · · ·
            3  M K K · ·
            4  K · · · ·";

        [Test]
        [Category("E04")]
        [Property("GDD", "6.1.2, 6.1.3, 6.2.1, 2.5.5")]
        [Property("AK", "AK-11 A")]
        public void E04_Ayni_eslesmenin_iki_hucresine_degen_katmanli_blok_tek_katman_kaybeder()
        {
            var d = HamleDenemesi.Kaydir(E04Tahtasi, H(4, 1), H(3, 1));

            Assert.That(
                d.PatlayanHucreler,
                Is.EquivalentTo(new[] { H(1, 1), H(2, 1), H(3, 1), H(3, 2), H(3, 3) }));
            var dogan = d.DoganOzelTaslar.Single();
            Assert.That(dogan.Konum, Is.EqualTo(H(3, 1)));
            Assert.That(dogan.Tas.Tur, Is.EqualTo(TasTuru.Bomba));

            // Blok (2,1) ve (3,2)'ye değiyor; bir eşleşme grubundan en fazla 1 vuruş alır.
            Assert.That(d.Olaylar.OfType<EngelHasarAldi>().Count(), Is.EqualTo(1));
            Assert.That(d.Tahta[2, 2].Tas!.Katman, Is.EqualTo(1));
        }

        [Test]
        [Category("E04")]
        [Property("GDD", "6.1.3")]
        [Property("AK", "AK-11 B")]
        public void E04_AK11_B_blok_degdigi_iki_hucre_icin_iki_vurus_alir_ve_kirilir()
        {
            var d = HamleDenemesi.Kaydir(
                E04Tahtasi, H(4, 1), H(3, 1), AkBayraklari.Varsayilan.Ile(Ak.YanHasarVurusSayisi, Varyant.B));

            var hasarlar = d.Olaylar.OfType<EngelHasarAldi>().ToList();
            Assert.That(hasarlar, Has.Count.EqualTo(2));
            Assert.That(hasarlar.Last().Kirildi, Is.True);
            Assert.That(d.Tahta[2, 2].Tas, Is.Null);
        }

        [Test]
        [Category("E05")]
        [Property("GDD", "6.2.2, 2.3.2")]
        public void E05_Yapiskan_kaplamali_tas_kaydirilamaz_yan_eslesme_hasari_kaplamayi_kaldirir()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · ~ · ·
                3  K K M · ·
                4  · · K · ·", hamle: 20);
            var once = o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas).ToList();

            // Hamle 1: kaplamalı taşı kaydırma girişimi.
            Assert.That(o.Kaydir(H(2, 3), H(2, 4)), Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(o.OlaylarTurunde<ImkansizHamle>().Count(), Is.EqualTo(1));
            Assert.That(o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas), Is.EqualTo(once));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(20));

            // Hamle 2: yandaki eşleşme kaplamayı kaldırır; kaplama tek katmanlıdır.
            Assert.That(o.Kaydir(H(4, 3), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            var hasar = o.OlaylarTurunde<KatmanHasarAldi>().Single();
            Assert.That(hasar.Konum, Is.EqualTo(H(2, 3)));
            Assert.That(hasar.Tanim.Arketip, Is.EqualTo(KatmanArketipi.YapiskanKaplama));
            Assert.That(hasar.Kirildi, Is.True);
            Assert.That(o.Tahta[2, 3].UstKatman, Is.Null);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(19));
        }

        [Test]
        [Category("E06")]
        [Property("GDD", "6.2.2, 6.2.6, 2.3.2")]
        public void E06_Kilitli_tas_kaydirilamaz_hamle_dusulmez()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · · ·
                3  K K # M ·
                4  · · · · ·", hamle: 20);
            var once = o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas).ToList();

            Assert.That(o.Kaydir(H(3, 3), H(3, 4)), Is.EqualTo(HamleSonucu.Imkansiz));

            Assert.That(o.OlaylarTurunde<ImkansizHamle>().Count(), Is.EqualTo(1));
            Assert.That(o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas), Is.EqualTo(once));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(20));
            Assert.That(o.OlaylarTurunde<HamleSayaciDegisti>(), Is.Empty);
        }

        [Test]
        [Category("E07")]
        [Property("GDD", "6.3.1")]
        public void E07_Yayilan_zemin_eslesme_boyunca_yayilir()
        {
            var test = TahtaKurucu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · · · ·
                3  K K M · ·
                4  · · K · ·");
            test.Tahta[3, 1].AltKatman = new HucreKatmani(YayilanZemin, 1);

            var d = HamleDenemesi.TamKaydir(test, H(4, 3), H(3, 3));

            Assert.That(d.PatlayanHucreler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));

            // Yayılma, zeminli hücreye uzaklık sırasıyla ilerler.
            Assert.That(
                d.Olaylar.OfType<ZeminYayildi>().Select(z => z.Konum),
                Is.EqualTo(new[] { H(3, 2), H(3, 3) }));

            // (3,2) ve (3,3) de zeminli olur; eşleşmeye girmeyen hücrelere yayılmaz.
            var zeminliler = d.Tahta.TumKonumlar().Where(k => d.Tahta[k].AltKatman != null).ToList();
            Assert.That(zeminliler, Is.EquivalentTo(new[] { H(3, 1), H(3, 2), H(3, 3) }));
            Assert.That(zeminliler.Select(k => d.Tahta[k].AltKatman!.Tanim.Yayilir), Is.All.True);
        }

        [Test]
        [Category("E08")]
        [Property("GDD", "6.1.1, 6.1.2, 6.2.1, 3.2.1, 3.2.2")]
        public void E08_Sert_blok_yan_eslesme_hasari_almaz_ozel_tas_vurusundan_etkilenir()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · · W · -
                3  K K M · ·
                4  · · K · ·");

            // Hamle 1: W, (3,3)'e dikten değiyor ama katmanı değişmez.
            Assert.That(o.Kaydir(H(4, 3), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.OlaylarTurunde<EngelHasarAldi>(), Is.Empty);
            var sertBlok = o.Tahta[2, 3].Tas!;
            Assert.That(sertBlok.Tur, Is.EqualTo(TasTuru.Engel), "W düşmez.");
            Assert.That(sertBlok.Katman, Is.EqualTo(3));
            Assert.That(o.Tahta[2, 5].Tas!.Tur, Is.EqualTo(TasTuru.Cizgi), "(2,5)'teki çizgi taşı yerinde kalır.");

            // Hamle 2 (tahta durulduktan sonra): çizgi taşı satır 2'yi iki yöne temizler ve W 3 katmandan 2'ye iner.
            int ilkOlay = o.Olaylar.Count;
            Assert.That(o.Kaydir(H(2, 5), H(2, 4)), Is.EqualTo(HamleSonucu.Gecerli));
            var hasar = o.Olaylar.Skip(ilkOlay).OfType<EngelHasarAldi>().Single();
            Assert.That(hasar.Engel, Is.SameAs(sertBlok));
            Assert.That(hasar.KalanKatman, Is.EqualTo(2));
            Assert.That(
                o.Olaylar.Skip(ilkOlay).OfType<HucreVuruldu>().Where(v => v.Tur == VurusTuru.Cizgi).Select(v => v.Konum),
                Is.EquivalentTo(Enumerable.Range(1, 5).Select(t => H(2, t))));
        }
    }
}
