using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Vakalar
{
    /// <summary>test_vakalari.md — F. Hedef, hamle sayacı, kazanma ve kaybetme.</summary>
    [TestFixture]
    public class FVakalari
    {
        private const string A01Tahtasi = @"
               1 2 3 4 5
            1  · · · · ·
            2  · · K · ·
            3  K K M · ·
            4  · · · · ·";

        private static int EkHamleSayisi => ProjeDosyalari.Parametreler.TamSayi(ParametreAdlari.EkHamleSayisi);

        private static int YedekHamleSayisi => ProjeDosyalari.Parametreler.TamSayi(ParametreAdlari.YedekHamleSayisi);

        [Test]
        [Category("F01")]
        [Property("GDD", "7.2.1, 2.3.8")]
        public void F01_Gecerli_hamlede_sayac_hemen_azalir()
        {
            var o = TestOturumu.Kur(A01Tahtasi, hamle: 20);

            Assert.That(o.Kaydir(H(2, 3), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));

            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(19));
            Assert.That(o.Oturum.YapilanHamle, Is.EqualTo(1));

            // Sayaç, hamle geçerli sayıldığı anda azalır; patlama ve düşmeler beklenmez.
            var sayac = o.OlaylarTurunde<HamleSayaciDegisti>().Single();
            Assert.That(sayac.KalanHamle, Is.EqualTo(19));
            Assert.That(o.Indis<HamleSayaciDegisti>(), Is.LessThan(o.Indis<GrupPatladi>()));
            Assert.That(o.Indis<HamleSayaciDegisti>(), Is.LessThan(o.Indis<TasDustu>()));
        }

        [Test]
        [Category("F02")]
        [Property("GDD", "2.3.3, 2.3.4, 7.2.2")]
        public void F02_Gecersiz_hamle_sayactan_dusmez()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4 5
                1  · · · · ·
                2  · K M · ·
                3  · · · · ·", hamle: 20);
            var once = o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas).ToList();

            Assert.That(o.Kaydir(H(2, 2), H(2, 3)), Is.EqualTo(HamleSonucu.Gecersiz));

            Assert.That(o.OlaylarTurunde<GecersizHamle>().Count(), Is.EqualTo(1), "Taşlar \"yanlış hamle\" ile geri döner.");
            Assert.That(o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas), Is.EqualTo(once));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(20));
            Assert.That(o.Oturum.YapilanHamle, Is.EqualTo(0));
        }

        [Test]
        [Category("F03")]
        [Property("GDD", "7.2.1, 2.3.5")]
        public void F03_Ozel_tasa_dokunmak_hamle_harcar()
        {
            var o = TestOturumu.Kur(NotrCizim(7, 7, (4, 4, 'B')), hamle: 20);

            Assert.That(o.Dokun(H(4, 4)), Is.EqualTo(HamleSonucu.Gecerli));

            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(19));
            Assert.That(o.Indis<HamleSayaciDegisti>(), Is.LessThan(o.Indis<HucreVuruldu>()));
        }

        [Test]
        [Category("F04")]
        [Property("GDD", "7.1.3, 7.3.1, 6.1.10")]
        public void F04_Butun_hedefler_tamamlaninca_kazanma_baslar()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4 5
                1  · · a · ·
                2  · · 1 · ·
                3  K K M · ·
                4  · · K · ·", hamle: 10, hedefler: new[] { new Hedef(KatmanliBlokHedefi, 1) });

            Assert.That(o.Kaydir(H(4, 3), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));

            // Katmanlı blok kırılınca hedef sayacı 0'a iner ve kazanma akışı hemen başlar: düşmeler beklenmez.
            Assert.That(o.Oturum.Hedefler.Kalan(KatmanliBlokHedefi), Is.EqualTo(0));
            var ilerleme = o.OlaylarTurunde<HedefIlerledi>().Single();
            Assert.That(ilerleme.Tur, Is.EqualTo(KatmanliBlokHedefi));
            Assert.That(ilerleme.Kalan, Is.EqualTo(0));
            Assert.That(o.Indis<KazanmaBasladi>(), Is.EqualTo(o.Indis<HedefIlerledi>() + 1));
            Assert.That(o.Indis<KazanmaBasladi>(), Is.LessThan(o.Indis<TasDustu>()));

            // Hamle hakkı kalmış olsa da oyun devam etmez.
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9));
            Assert.That(o.OlaylarTurunde<BolumKazanildi>().Single().KalanHamle, Is.EqualTo(9));
            Assert.That(o.Kaydir(H(1, 1), H(1, 2)), Is.EqualTo(HamleSonucu.Reddedildi));
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>(), Is.Empty);
        }

        [TestCase(Varyant.A, false)]
        [TestCase(Varyant.A, true)]
        [TestCase(Varyant.B, false, Description = "AK-02 B'de de (1)–(2) adımları aynıdır; sonuç değişmez.")]
        [TestCase(Varyant.B, true)]
        [Category("F05")]
        [Property("GDD", "7.3.1, 7.4.1, 5.5.1")]
        [Property("AK", "AK-02")]
        public void F05_Son_hamlenin_kaskadi_hedefi_tamamlarsa_sonuc_kazanmadir(Varyant ak02, bool yedekHak)
        {
            var o = TestOturumu.Kur(
                DVakalari.D06Tahtasi,
                hamle: 1,
                hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 3) },
                bayraklar: AkBayraklari.Varsayilan.Ile(Ak.HamleBittiAkisi, ak02));
            o.Oturum.YedekHamleHakki = yedekHak;

            Assert.That(o.Kaydir(H(5, 3), H(4, 3)), Is.EqualTo(HamleSonucu.Gecerli));

            // Hamle anında kalan hamle 0 olur; ardından gelen kaskadda üç K patlar ve hedef tamamlanır.
            Assert.That(o.OlaylarTurunde<HamleSayaciDegisti>().First().KalanHamle, Is.EqualTo(0));
            var kaskad = o.Olaylar.FindIndex(x => x is GrupPatladi g && g.Kaskad);
            Assert.That(o.Indis<KazanmaBasladi>(), Is.GreaterThan(kaskad));

            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(o.Oturum.YedekHamleHakki, Is.EqualTo(yedekHak), "Yedek hamle hakkı kullanılmaz.");
            Assert.That(o.OlaylarTurunde<YedekHamleKullanildi>(), Is.Empty);
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>(), Is.Empty);
            Assert.That(o.OlaylarTurunde<BolumKaybedildi>(), Is.Empty);
        }

        /// <summary>
        /// F06 tahtası: A01'deki hamle ve ulaşılamaz bir yerde katmanlı blok. Yedek hamle hakkından sonra hamlenin
        /// yeniden bitebilmesi için satır 4, 6 ve 8'de birer hamle daha vardır; üstteki patlama alttakileri bozmaz.
        /// </summary>
        private const string F06Tahtasi = @"
               1 2 3 4 5
            1  · · K · 1
            2  K K · · ·
            3  · · · · ·
            4  M M · M ·
            5  · · · · ·
            6  Y Y · Y ·
            7  · · · · ·
            8  S S · S ·";

        private static TestOturumu F06Oturumu(Varyant ak02, bool yedekHak)
        {
            var o = TestOturumu.Kur(
                F06Tahtasi,
                hamle: 1,
                hedefler: new[] { new Hedef(KatmanliBlokHedefi, 1) },
                bayraklar: AkBayraklari.Varsayilan.Ile(Ak.HamleBittiAkisi, ak02));
            o.Oturum.YedekHamleHakki = yedekHak;
            Assert.That(o.Kaydir(H(1, 3), H(2, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            return o;
        }

        private static void KalanHamleleriBitir(TestOturumu o)
        {
            foreach (int satir in new[] { 4, 6, 8 })
            {
                if (o.Oturum.Durum != BolumDurumu.Oynaniyor)
                {
                    break;
                }

                Assert.That(o.Kaydir(H(satir, 4), H(satir, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            }
        }

        [Test]
        [Category("F06")]
        [Property("GDD", "7.4.1, 7.4.2, 7.4.3, 7.4.4, 9.12.1")]
        [Property("AK", "AK-02 A")]
        public void F06_Yedek_hak_yokken_hamle_bitince_ek_hamle_teklifi_acilir_reddedilirse_kaybedilir()
        {
            var o = F06Oturumu(Varyant.A, yedekHak: false);

            // 1. Tahta durulduktan sonra ek hamle teklifi açılır.
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            var teklif = o.OlaylarTurunde<EkHamleTeklifiAcildi>().Single();
            Assert.That(teklif.TeklifEdilenHamle, Is.EqualTo(EkHamleSayisi));
            Assert.That(o.Indis<EkHamleTeklifiAcildi>(), Is.GreaterThan(o.Olaylar.FindLastIndex(x => x is TasGirdi)));
            Assert.That(o.Kaydir(H(4, 4), H(4, 3)), Is.EqualTo(HamleSonucu.Reddedildi), "Teklif açıkken hamle kabul edilmez.");

            // 2. Oyuncu teklifi reddederse kaybetme akışı başlar. Can düşmesi meta testinde (MT01).
            o.Oturum.EkHamleTeklifiniReddet(o.Olaylar);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kaybedildi));
            Assert.That(o.OlaylarTurunde<BolumKaybedildi>().Single().TeklifSunulmadan, Is.False);
        }

        [Test]
        [Category("F06")]
        [Property("GDD", "7.4.2, 7.4.3")]
        public void F06_Teklif_kabul_edilirse_bolum_ayni_tahtayla_surer()
        {
            var o = F06Oturumu(Varyant.A, yedekHak: false);
            var tahtaOnce = o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas).ToList();

            o.Oturum.EkHamleTeklifiniKabulEt(o.Olaylar);

            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(EkHamleSayisi));
            Assert.That(o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas), Is.EqualTo(tahtaOnce));
            Assert.That(o.Kaydir(H(4, 4), H(4, 3)), Is.EqualTo(HamleSonucu.Gecerli));
        }

        [Test]
        [Category("F06")]
        [Property("GDD", "7.4.1, 3.8.2")]
        [Property("AK", "AK-02 A")]
        public void F06_Yedek_hak_varsa_teklif_acilmadan_once_kullanilir_hamle_yine_biterse_teklif_acilir()
        {
            var o = F06Oturumu(Varyant.A, yedekHak: true);

            // 1. Teklif penceresi açılmadan önce yedek hamle hakkı kullanılır ve oyun sürer.
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(o.OlaylarTurunde<YedekHamleKullanildi>().Single().EklenenHamle, Is.EqualTo(YedekHamleSayisi));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(YedekHamleSayisi));
            Assert.That(o.Oturum.YedekHamleHakki, Is.False);
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>(), Is.Empty);

            // 2. Hamle yine biterse ek hamle teklifi açılır; reddedilirse kaybetme akışı başlar.
            Assume.That(YedekHamleSayisi, Is.LessThanOrEqualTo(3), "Test tahtasında üç hamle daha var.");
            KalanHamleleriBitir(o);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(0));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>().Count(), Is.EqualTo(1));

            o.Oturum.EkHamleTeklifiniReddet(o.Olaylar);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kaybedildi));
        }

        [Test]
        [Category("F06")]
        [Property("GDD", "7.4.1, 3.8.2")]
        [Property("AK", "AK-02 B")]
        public void F06_AK02_B_once_teklif_acilir_reddedilirse_yedek_hak_son_sans_olarak_kullanilir()
        {
            var o = F06Oturumu(Varyant.B, yedekHak: true);

            // Önce ek hamle teklifi açılır.
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            Assert.That(o.OlaylarTurunde<YedekHamleKullanildi>(), Is.Empty);

            // Teklif reddedilirse ve yedek hamle hakkı varsa bu hak son şans olarak kullanılır.
            o.Oturum.EkHamleTeklifiniReddet(o.Olaylar);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(YedekHamleSayisi));
            Assert.That(o.OlaylarTurunde<BolumKaybedildi>(), Is.Empty);

            // Hamle yine biterse kaybetme akışı başlar: teklif açılır ve bu kez ret kaybettirir.
            Assume.That(YedekHamleSayisi, Is.LessThanOrEqualTo(3));
            KalanHamleleriBitir(o);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            o.Oturum.EkHamleTeklifiniReddet(o.Olaylar);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kaybedildi));
        }

        private const string F07Tahtasi = @"
               1 2 3 4
            1  X X X X
            2  X K M X
            3  X X X X";

        [Test]
        [Category("F07")]
        [Property("GDD", "5.6.1, 5.6.2, 5.6.5, 7.5.1")]
        [Property("AK", "AK-13 A")]
        public void F07_Olasi_hamle_yoksa_ve_karistirma_basarisizsa_olasi_hamle_yok_akisi_calisir()
        {
            var o = TestOturumu.Kur(F07Tahtasi, hamle: 12, karistirma: true);
            var kirmizi = o.Tahta[2, 2].Tas;
            var mavi = o.Tahta[2, 3].Tas;

            var sonuc = o.Oturum.OlasiHamleyiDenetle(o.Olaylar);

            // Olası hamle bulunmaz; yer değiştirme ve renk değiştirme başarısız olur; taşlar eski yerlerine döner.
            Assert.That(o.OlaylarTurunde<OlasiHamleYok>().Count(), Is.EqualTo(1));
            Assert.That(o.Tahta[2, 2].Tas, Is.SameAs(kirmizi));
            Assert.That(o.Tahta[2, 3].Tas, Is.SameAs(mavi));

            // Bölüm kaybedilmiş sayılmaz, ek hamle teklifi açılmaz ve hamle sayacı korunur.
            // Bu tahta şekli yeniden kurulduğunda da hamle üretemediği için AK-13 A'nın son adımı çalışır:
            // bölüm, can düşürülmeden baştan başlatılır.
            Assert.That(sonuc, Is.EqualTo(KaristirmaSonucu.BastanBaslatilmali));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.BastanBaslatilmali));
            Assert.That(o.OlaylarTurunde<BolumBastanBaslatilmali>().Count(), Is.EqualTo(1));
            Assert.That(o.OlaylarTurunde<BolumKaybedildi>(), Is.Empty);
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>(), Is.Empty);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(12));
        }

        [Test]
        [Category("F07")]
        [Property("GDD", "7.5.1")]
        [Property("AK", "AK-13 B")]
        public void F07_AK13_B_olasi_hamle_yok_akisi_bolumu_teklif_sunmadan_kaybettirir()
        {
            var o = TestOturumu.Kur(
                F07Tahtasi,
                hamle: 12,
                karistirma: true,
                bayraklar: AkBayraklari.Varsayilan
                    .Ile(Ak.KaristirmaBasarisizligi, Varyant.B)
                    .KaristirmaRenkDegisimiIle(false));

            var sonuc = o.Oturum.OlasiHamleyiDenetle(o.Olaylar);

            Assert.That(sonuc, Is.EqualTo(KaristirmaSonucu.Kaybedildi));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kaybedildi));
            Assert.That(o.OlaylarTurunde<BolumKaybedildi>().Single().TeklifSunulmadan, Is.True);
            Assert.That(o.OlaylarTurunde<EkHamleTeklifiAcildi>(), Is.Empty);
        }
    }
}
