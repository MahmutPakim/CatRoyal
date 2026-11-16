using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Testler.Vakalar;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>
    /// Yol haritası Aşama 3: kapı ayrıntıları (GDD 9.4.3–9.4.7), pencere kuyruğu (9.4.5, 10.0.3), alan (9.6.6, 9.6.7),
    /// sezon hattı (9.8.1, 9.8.3, 9.8.10, 9.8.11), haftalık hat (9.9.1, 9.9.6, 9.9.7) ve albüm (9.7).
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama3Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        private static Oyuncu Oyuncu(int enYuksekBolum = 500) => new Oyuncu(P, Simdi) { EnYuksekBolum = enYuksekBolum };

        /// <summary>Kuyruktaki bütün pencereleri sırayla açıp kapatır; kimliklerini gösterim sırasıyla verir.</summary>
        private static List<string> HepsiniGoster(PencereKuyrugu kuyruk)
        {
            var kimlikler = new List<string>();
            while (kuyruk.Ac() is { } pencere)
            {
                kimlikler.Add(pencere.Kimlik);
                kuyruk.Kapat();
            }

            return kimlikler;
        }

        // ------------------------------------------------------------------ 3.1 Kapı ayrıntıları

        [Test]
        [Property("GDD", "9.4.3, 9.4.4")]
        public void Olay_simgesi_acilistan_once_gizlidir_deney_grubunda_kilitli_onizlenir()
        {
            const string olay = "acilis_ortakli_atis";
            var kapilar = new IlerlemeKapilari(P);
            int acilis = P.TamSayi(olay);
            int onizleme = Tam(MetaParametreAdlari.KismiKilitOnizleme);
            int simgeler = Tam(MetaParametreAdlari.AcilisOlaySimgeleri);

            Assert.That(OlayKapilari.Simge(P, kapilar, olay, acilis), Is.EqualTo(SimgeDurumu.Acik));
            Assert.That(OlayKapilari.Simge(P, kapilar, olay, acilis - 1), Is.EqualTo(SimgeDurumu.Gizli));

            Assert.That(OlayKapilari.Simge(P, kapilar, olay, acilis - 1, kilitliOnizleme: true), Is.EqualTo(SimgeDurumu.Kilitli));
            Assert.That(OlayKapilari.Simge(P, kapilar, olay, acilis - onizleme, kilitliOnizleme: true), Is.EqualTo(SimgeDurumu.Kilitli));
            Assert.That(OlayKapilari.Simge(P, kapilar, olay, acilis - onizleme - 1, kilitliOnizleme: true), Is.EqualTo(SimgeDurumu.Gizli));

            // Belirli bir bölümden önce hiçbir olay simgesi gösterilmez: olayın kendisi açılmış olsa bile.
            kapilar.OneCek(olay, 1);
            Assert.That(OlayKapilari.Simge(P, kapilar, olay, simgeler - 1), Is.EqualTo(SimgeDurumu.Gizli));
            Assert.That(OlayKapilari.Simge(P, kapilar, olay, simgeler), Is.EqualTo(SimgeDurumu.Acik));
        }

        [Test]
        [Property("GDD", "9.4.6")]
        public void Bolum_oncesi_ekraninda_sinirli_sayida_olay_gosterilir()
        {
            int azami = Tam(MetaParametreAdlari.SeviyeOncesiAzamiOlay);
            var adaylar = new[] { "a", "b", "c", "d", "e" };

            Assert.That(OlayKapilari.BolumOncesi(P, adaylar), Is.EqualTo(adaylar.Take(azami)));
            Assert.That(OlayKapilari.BolumOncesi(P, adaylar.Take(1)), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        [Property("GDD", "9.4.7")]
        public void Ikinci_olaya_katilmak_icin_ilk_olayda_yeterli_sure_kalmalidir()
        {
            double esik = P.Sayi(MetaParametreAdlari.IkinciOlayAsgariSureDk);
            var bitis = Simdi + TimeSpan.FromMinutes(esik);

            Assert.That(OlayKapilari.IkinciOlayaKatilabilir(P, bitis, Simdi), Is.True);
            Assert.That(OlayKapilari.IkinciOlayaKatilabilir(P, bitis, Simdi + TimeSpan.FromSeconds(1)), Is.False);

            // Olaya özel katılma eşiği bu kuralın yerine geçer.
            Assert.That(OlayKapilari.IkinciOlayaKatilabilir(P, bitis, Simdi, olayaOzelEsikDk: esik + 15), Is.False);
            Assert.That(OlayKapilari.IkinciOlayaKatilabilir(P, bitis, Simdi + TimeSpan.FromMinutes(10), olayaOzelEsikDk: esik - 15), Is.True);
        }

        // ------------------------------------------------------------------ 3.2 Pencere kuyruğu

        [Test]
        [Property("GDD", "10.0.3, 8.6.7")]
        public void Pencereler_once_engel_tanitimi_sonra_kaynak_sirasiyla_ve_tur_sirasiyla_gosterilir()
        {
            var kuyruk = new PencereKuyrugu();
            kuyruk.Ekle(new Pencere("x:odul", PencereTuru.OdulAlma, "x"));
            kuyruk.Ekle(new Pencere("x:baslangic", PencereTuru.Baslangic, "x"));
            kuyruk.Ekle(new Pencere("y:tanitim", PencereTuru.Tanitim, "y"));
            kuyruk.Ekle(new Pencere("x:tanitim", PencereTuru.Tanitim, "x"));
            kuyruk.Ekle(new Pencere("x:otomatik", PencereTuru.Otomatik, "x"));
            kuyruk.Ekle(new Pencere("x:bilgi", PencereTuru.Bilgi, "x"));
            kuyruk.Ekle(Pencere.EngelTanitimi("engel:SertBlok"));

            Assert.That(
                HepsiniGoster(kuyruk),
                Is.EqualTo(new[]
                {
                    "engel_tanitimi:engel:SertBlok", "x:tanitim", "x:baslangic", "x:bilgi", "x:odul", "x:otomatik", "y:tanitim",
                }));
        }

        [Test]
        [Property("GDD", "9.4.5, 10.0.3")]
        public void Acik_pencere_yerini_kaybetmez_ve_bir_kez_gosterilen_pencere_yeniden_eklenmez()
        {
            var kuyruk = new PencereKuyrugu();
            Assert.That(kuyruk.Ac(), Is.Null);

            Assert.That(kuyruk.Ekle(new Pencere("a", PencereTuru.Bilgi, "a")), Is.True);
            Assert.That(kuyruk.Ekle(new Pencere("a", PencereTuru.Bilgi, "a")), Is.False, "Aynı pencere iki kez beklemez.");
            Assert.That(kuyruk.Ac()!.Kimlik, Is.EqualTo("a"));
            Assert.That(kuyruk.Ekle(new Pencere("a", PencereTuru.Bilgi, "a")), Is.False, "Açık pencere yeniden eklenmez.");

            kuyruk.Ekle(Pencere.EngelTanitimi("m"));
            Assert.That(kuyruk.Ac()!.Kimlik, Is.EqualTo("a"), "Sonradan gelen öncelikli pencere açık olanın yerini almaz.");
            Assert.That(kuyruk.Bekleyen, Is.EqualTo(1));

            Assert.That(kuyruk.Kapat(), Is.True);
            Assert.That(kuyruk.Gosterildi("a"), Is.True);
            Assert.That(kuyruk.Ekle(new Pencere("a", PencereTuru.Bilgi, "a")), Is.False);
            Assert.That(kuyruk.Ac()!.Kimlik, Is.EqualTo("engel_tanitimi:m"));
            kuyruk.Kapat();
            Assert.That(kuyruk.Kapat(), Is.False);

            // Yinelenen pencereler (ör. günlük ödül) her seferinde yeniden eklenebilir.
            var gunluk = new Pencere("gunluk", PencereTuru.Otomatik, "g", birKez: false);
            Assert.That(kuyruk.Ekle(gunluk), Is.True);
            HepsiniGoster(kuyruk);
            Assert.That(kuyruk.Ekle(new Pencere("gunluk", PencereTuru.Otomatik, "g", birKez: false)), Is.True);
        }

        [Test]
        [Property("GDD", "9.4.5, 9.8.1")]
        public void Yalniz_tanitim_atlanabilir_ve_cok_adimli_tanitim_adim_adim_ilerler()
        {
            var kuyruk = new PencereKuyrugu();
            kuyruk.Ekle(new Pencere("bilgi", PencereTuru.Bilgi, "a"));
            kuyruk.Ac();
            Assert.That(kuyruk.Atla(), Is.False);
            kuyruk.Kapat();

            kuyruk.Ekle(new Pencere("sezon", PencereTuru.Tanitim, "s", adimlar: OzellikTanitimlari.SezonHattiAdimlari));
            var tanitim = kuyruk.Ac()!;
            Assert.That(OzellikTanitimlari.SezonHattiAdimlari, Has.Count.EqualTo(5), "Tanıtım beş adımdır.");

            var gorulen = new List<string?> { tanitim.GecerliAdim };
            while (kuyruk.AdimIlerle())
            {
                gorulen.Add(tanitim.GecerliAdim);
            }

            Assert.That(gorulen, Is.EqualTo(new[] { "simge", "ilk_ucretsiz_odul", "ilerleme_cubugu", "ilk_premium_odul", "bitti" }));
            Assert.That(kuyruk.Acik, Is.Null);
            Assert.That(kuyruk.Gosterildi("sezon"), Is.True);

            // Yarıda atlanan tanıtım da gösterilmiş sayılır.
            kuyruk.Ekle(new Pencere("sezon2", PencereTuru.Tanitim, "s", adimlar: OzellikTanitimlari.SezonHattiAdimlari));
            kuyruk.Ac();
            kuyruk.AdimIlerle();
            Assert.That(kuyruk.Atla(), Is.True);
            Assert.That(kuyruk.Gosterildi("sezon2"), Is.True);
        }

        [Test]
        [Property("GDD", "9.4.5, 9.5.1, 9.6.6")]
        public void Acilis_bolumune_ulasilinca_ozelligin_pencereleri_akisa_eklenir()
        {
            int gunluk = Tam(MetaParametreAdlari.AcilisGunlukOdul);
            var oyuncu = Oyuncu(gunluk - 2);
            var etiketler = new ZorlukEtiketTablosu();

            // Günlük ödülün açılışından bir önceki bölüme ulaşılır: o ana kadar açılanların pencereleri gösterilir.
            MetaVakalari.BolumuOyna(oyuncu, etiketler, gunluk - 2, kazan: true, Simdi, out _);
            var once = HepsiniGoster(oyuncu.Pencereler);
            Assert.That(once, Does.Contain(OzellikTanitimlari.OtomatikKimligi(MetaParametreAdlari.AcilisAlanPenceresi)), "Alan penceresi bir kez kendiliğinden açılır.");
            Assert.That(once, Does.Not.Contain(OzellikTanitimlari.TanitimKimligi(MetaParametreAdlari.AcilisGunlukOdul)));

            // Açılış bölümüne ulaşılınca yalnızca yeni açılan özelliğin tanıtımı gelir.
            MetaVakalari.BolumuOyna(oyuncu, etiketler, gunluk - 1, kazan: true, Simdi, out _);
            Assert.That(oyuncu.EnYuksekBolum, Is.EqualTo(gunluk));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { OzellikTanitimlari.TanitimKimligi(MetaParametreAdlari.AcilisGunlukOdul) }));

            // Geçilmiş bir bölümü yeniden kazanmak pencere eklemez.
            MetaVakalari.BolumuOyna(oyuncu, etiketler, 1, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));
            Assert.That(OzellikTanitimlari.KapilariDenetle(oyuncu), Is.EqualTo(0));
        }

        [Test]
        [Property("GDD", "9.8.1, 9.9.1")]
        public void Sezon_hatti_bes_adimli_tanitimla_haftalik_hat_tanitim_ve_baslangic_penceresiyle_acilir()
        {
            var oyuncu = Oyuncu(Tam(MetaParametreAdlari.AcilisSezonHatti));
            OzellikTanitimlari.KapilariDenetle(oyuncu);

            var pencereler = new List<Pencere>();
            while (oyuncu.Pencereler.Ac() is { } pencere)
            {
                pencereler.Add(pencere);
                oyuncu.Pencereler.Kapat();
            }

            var sezon = pencereler.Single(p => p.Kimlik == OzellikTanitimlari.TanitimKimligi(MetaParametreAdlari.AcilisSezonHatti));
            Assert.That(sezon.Adimlar, Has.Count.EqualTo(5));
            Assert.That(sezon.Atlanabilir, Is.True);

            var haftalik = pencereler.Where(p => p.Kaynak == MetaParametreAdlari.AcilisHaftalikHat).ToList();
            Assert.That(haftalik.Select(p => p.Tur), Is.EqualTo(new[] { PencereTuru.Tanitim, PencereTuru.Baslangic }));

            // Bir kez gösterilir: kayıttan sonra da yeniden eklenmez.
            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(OzellikTanitimlari.KapilariDenetle(yuklenen), Is.EqualTo(0));
        }

        [Test]
        public void Pencere_kaydi_olmayan_eski_kayitta_gecmis_tanitimlar_gosterilmez()
        {
            var eski = OyuncuKaydi.Yukle("{\"surum\":1,\"en_yuksek_bolum\":100}", P, Simdi);
            Assert.That(OzellikTanitimlari.KapilariDenetle(eski), Is.EqualTo(0));

            // Sonradan açılan özelliğin tanıtımı yine gösterilir.
            eski.EnYuksekBolum = Tam(MetaParametreAdlari.AcilisCiftRenkTakviyesi);
            Assert.That(OzellikTanitimlari.KapilariDenetle(eski), Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ 3.3 Alan

        [Test]
        [Property("GDD", "9.6.7")]
        public void Oynanabilir_alan_sayisi_yayimlanan_bolumden_turetilir_ve_son_alan_bekler()
        {
            int alanBasina = Tam(MetaParametreAdlari.AlanBasinaBolum);
            Assert.That(AlanInsasi.OynanabilirAlanSayisi(P, 0), Is.EqualTo(1));
            Assert.That(AlanInsasi.OynanabilirAlanSayisi(P, alanBasina), Is.EqualTo(1));
            Assert.That(AlanInsasi.OynanabilirAlanSayisi(P, alanBasina + 1), Is.EqualTo(2));
            Assert.That(AlanInsasi.OynanabilirAlanSayisi(P, alanBasina * 3), Is.EqualTo(3));

            var oyuncu = Oyuncu();
            oyuncu.Alan.GorevleriYukle(new[] { new AlanGorevi("tek", 1) });
            oyuncu.Alan.InsaEt("tek", 1);

            // Yayımlanmış tek alan var: sandık açılır ama yeni alan açılmaz.
            var sandik = oyuncu.Alan.SandigiAc(oynanabilirAlanSayisi: 1);
            Assert.That(sandik!.Para, Is.EqualTo(Tam(MetaParametreAdlari.AlanSandigiPara)));
            Assert.That(oyuncu.Alan.Durum, Is.EqualTo(AlanDurumu.Bekliyor));
            Assert.That(oyuncu.Alan.AlanNumarasi, Is.EqualTo(1));
            Assert.That(oyuncu.Alan.SandigiAc(1), Is.Null, "Sandık bir kez açılır.");
            Assert.That(oyuncu.Alan.YeniAlanGeldi(1), Is.False);

            // Bekleme durumu kayıtla korunur; yeni bölümler yayımlanınca sıradaki alan açılır.
            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(yuklenen.Alan.Durum, Is.EqualTo(AlanDurumu.Bekliyor));
            Assert.That(yuklenen.Alan.YeniAlanGeldi(2), Is.True);
            Assert.That(yuklenen.Alan.AlanNumarasi, Is.EqualTo(2));
            Assert.That(yuklenen.Alan.Durum, Is.EqualTo(AlanDurumu.Tamamlandi));
        }

        [Test]
        [Property("GDD", "9.6.6")]
        public void Yapilabilir_gorev_bildirimi_belirli_bir_bolume_kadar_zorla_gosterilir()
        {
            int son = Tam(MetaParametreAdlari.GorevBildirimiSonBolum);
            var alan = new AlanInsasi(P);
            alan.GorevleriYukle(new[] { new AlanGorevi("zemin", 5), new AlanGorevi("duvar", 2, "zemin") });

            Assert.That(alan.GorevBildirimiGosterilir(5, son), Is.True);
            Assert.That(alan.GorevBildirimiGosterilir(4, son), Is.False, "Önkoşulu bitmemiş görev yapılabilir sayılmaz.");
            Assert.That(alan.GorevBildirimiGosterilir(5, son + 1), Is.False);
            Assert.That(alan.YapilabilirGorevVar(5), Is.True, "Bildirim zorlanmasa da görev yapılabilir.");
        }

        // ------------------------------------------------------------------ 3.4 Sezon hattı

        [Test]
        [Property("GDD", "9.8.3, 9.9.2")]
        public void Kilometre_taslari_duzenli_aralikla_gelir()
        {
            var sezon = OdulHatti.Sezon(P);
            int aralik = Tam(MetaParametreAdlari.KilometreTasiAraligi);
            var taslar = Enumerable.Range(0, sezon.BasamakSayisi).Where(sezon.KilometreTasi).ToList();
            Assert.That(taslar, Is.EqualTo(Enumerable.Range(1, sezon.BasamakSayisi / aralik).Select(i => (i * aralik) - 1)));
            Assert.That(taslar, Is.EqualTo(new[] { 5, 11, 17, 23 }), "Öneri değerlerle 6., 12., 18. ve 24. basamak.");

            var haftalik = OdulHatti.Haftalik(P);
            Assert.That(Enumerable.Range(0, haftalik.BasamakSayisi).Where(haftalik.KilometreTasi), Is.EqualTo(new[] { 3, 7 }));
            Assert.That(sezon.KilometreTasi(-1), Is.False);
            Assert.That(sezon.KilometreTasi(sezon.BasamakSayisi + aralik - 1), Is.False);
        }

        private static List<OdulPaketi?> Oduller(int adet, int para, string? kozmetikBasamagi = null)
        {
            var oduller = Enumerable.Range(0, adet).Select(i => (OdulPaketi?)new OdulPaketi { Para = para }).ToList();
            if (kozmetikBasamagi != null)
            {
                oduller[0]!.Kozmetikler.Add(kozmetikBasamagi);
            }

            return oduller;
        }

        [Test]
        [Property("GDD", "9.8.3, 9.8.6, 9.8.7")]
        public void Hat_odulu_icerigiyle_alinir_kozmetik_oyuncuda_kalir_ve_premium_satin_alinca_pencere_acilir()
        {
            var oyuncu = Oyuncu();
            oyuncu.SezonHatti.OdulleriAyarla(Oduller(25, 10, "cerceve_altin"), Oduller(25, 100));
            oyuncu.SezonHatti.PuanEkle(9);
            int para = oyuncu.Cuzdan.Bakiye;
            Assert.That(oyuncu.SezonHatti.Basamak, Is.GreaterThanOrEqualTo(2));

            Assert.That(oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 0, premium: false, Simdi), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 10));
            Assert.That(oyuncu.Kozmetikler, Does.Contain("cerceve_altin"));
            Assert.That(oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 0, premium: false, Simdi), Is.False, "Ödül bir kez alınır.");
            Assert.That(oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 0, premium: true, Simdi), Is.False, "Premium hat alınmadı.");
            Assert.That(oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 24, premium: false, Simdi), Is.False, "Basamak geçilmedi.");

            oyuncu.SezonPremiumSatinAl(Simdi);
            Assert.That(oyuncu.Pencereler.Ac()!.Kimlik, Is.EqualTo("sezon_premium_odulleri"));
            Assert.That(oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 0, premium: true, Simdi), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 110));

            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(yuklenen.Kozmetikler, Does.Contain("cerceve_altin"));
        }

        [Test]
        [Property("GDD", "9.8.10, 9.8.11")]
        public void Sezon_bitince_alinmamis_oduller_toplu_verilir_ve_yeni_sezon_ucretsiz_hatla_baslar()
        {
            var oyuncu = Oyuncu();
            var bitis = Simdi + TimeSpan.FromDays(Tam("sezon_suresi_gun"));
            Assert.That(oyuncu.SezonuEsitle(new DonemBilgisi("s1", bitis), Simdi), Is.Null);
            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.EqualTo("s1"));
            Assert.That(oyuncu.SezonDonemi.Kalan(Simdi), Is.EqualTo(bitis - Simdi));

            oyuncu.SezonHatti.OdulleriAyarla(Oduller(25, 10), Oduller(25, 100));
            oyuncu.SezonHatti.PuanEkle(9);
            oyuncu.SezonPremiumSatinAl(Simdi);
            HepsiniGoster(oyuncu.Pencereler);
            int basamak = oyuncu.SezonHatti.Basamak;
            oyuncu.HatOdulunuAl(oyuncu.SezonHatti, 0, premium: false, Simdi);
            int para = oyuncu.Cuzdan.Bakiye;

            // Sezon sürerken eşitleme hiçbir şey vermez.
            Assert.That(oyuncu.SezonuEsitle(new DonemBilgisi("s1", bitis), bitis - TimeSpan.FromSeconds(1)), Is.Null);

            // Oyuncu sezon değişirken oyunda değildi: önceki sezondan kalan ödüller ilk eşitlemede verilir.
            var sonra = bitis + TimeSpan.FromDays(3);
            var sonuc = oyuncu.SezonuEsitle(new DonemBilgisi("s2", sonra + TimeSpan.FromDays(27)), sonra);

            Assert.That(sonuc!.Kimlik, Is.EqualTo("s1"));
            Assert.That(sonuc.Oduller, Has.Count.EqualTo((basamak * 2) - 1), "Alınmış tek ödül dışındaki bütün ücretsiz ve premium ödüller.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + ((basamak - 1) * 10) + (basamak * 100)));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "sezon_bitti:s1" }), "Bitiş penceresi gösterilir.");

            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.EqualTo("s2"));
            Assert.That(oyuncu.SezonHatti.Basamak, Is.EqualTo(0));
            Assert.That(oyuncu.SezonHatti.Premium, Is.False, "Yeni sezon ücretsiz hatla başlar.");
            Assert.That(oyuncu.Can.Premium, Is.False);
        }

        [Test]
        [Property("GDD", "9.8.10")]
        public void Suresi_dolan_sezon_sunucuya_ulasilamasa_da_biter_ve_ayni_sezon_yeniden_baslamaz()
        {
            var oyuncu = Oyuncu();
            var bitis = Simdi + TimeSpan.FromDays(30);
            var s1 = new DonemBilgisi("s1", bitis);
            oyuncu.SezonuEsitle(s1, Simdi);
            oyuncu.SezonHatti.OdulleriAyarla(Oduller(25, 10), Oduller(25, 100));
            oyuncu.SezonHatti.PuanEkle(2);

            var sonuc = oyuncu.SezonuEsitle(null, bitis);
            Assert.That(sonuc!.Oduller, Has.Count.EqualTo(1));
            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.Null);
            Assert.That(oyuncu.SezonuEsitle(null, bitis + TimeSpan.FromDays(1)), Is.Null, "Ödüller bir kez verilir.");

            // Sunucu hâlâ biten sezonu bildiriyorsa sezon yeniden başlatılmaz.
            Assert.That(oyuncu.SezonuEsitle(new DonemBilgisi("s1", bitis + TimeSpan.FromDays(5)), bitis + TimeSpan.FromDays(1)), Is.Null);
            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.Null);

            // Dönem bilgisi kayıtla korunur.
            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(yuklenen.SezonDonemi.SonBiten, Is.EqualTo("s1"));
            yuklenen.SezonuEsitle(new DonemBilgisi("s2", bitis + TimeSpan.FromDays(40)), bitis + TimeSpan.FromDays(2));
            Assert.That(yuklenen.SezonDonemi.Kimlik, Is.EqualTo("s2"));
            Assert.That(OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(yuklenen), P, Simdi).SezonDonemi.Bitis, Is.EqualTo(bitis + TimeSpan.FromDays(40)));
        }

        [Test]
        [Property("GDD", "9.8.8, 9.8.10")]
        public void Dolan_hazne_sezon_sonunda_verilir_dolmayan_verilmez()
        {
            int hazneHedefi = Tam(MetaParametreAdlari.SezonHazneHedefi);
            int tumBasamaklar = OdulHatti.SezonBasamakHedefleri(P).Sum();

            DonemBitisi Bitir(int haznePuani)
            {
                var oyuncu = Oyuncu();
                oyuncu.SezonuEsitle(new DonemBilgisi("s1", Simdi + TimeSpan.FromDays(30)), Simdi);
                oyuncu.SezonPremiumSatinAl(Simdi);
                oyuncu.SezonHatti.PuanEkle(tumBasamaklar + haznePuani);
                return oyuncu.SezonuEsitle(null, Simdi + TimeSpan.FromDays(30))!;
            }

            int dolu = hazneHedefi / Tam(MetaParametreAdlari.HazneAsamaPuani) * Tam(MetaParametreAdlari.HazneAsamaParasi);
            Assert.That(Bitir(hazneHedefi).Oduller.Sum(o => o.Para), Is.EqualTo(dolu));
            Assert.That(Bitir(hazneHedefi - 1).Oduller, Is.Empty);
        }

        [Test]
        [Property("GDD", "9.8.11, 9.9.7")]
        public void Deney_grubunun_ya_da_oyuncuya_ozel_basamak_tablosu_yeni_donemde_kullanilir()
        {
            var oyuncu = Oyuncu();
            oyuncu.SezonuEsitle(new DonemBilgisi("s1", Simdi + TimeSpan.FromDays(30)) { BasamakHedefleri = new[] { 1, 2, 3 } }, Simdi);
            Assert.That(oyuncu.SezonHatti.BasamakSayisi, Is.EqualTo(3));
            oyuncu.SezonHatti.PuanEkle(3);
            Assert.That(oyuncu.SezonHatti.Basamak, Is.EqualTo(2));

            oyuncu.HaftayiEsitle(
                new DonemBilgisi("h1", Simdi + TimeSpan.FromDays(7)) { BasamakHedefleri = new[] { 5, 5 }, GenelSurum = 3, OyuncuSurumu = 2 },
                Simdi);
            Assert.That(oyuncu.HaftalikHat.BasamakSayisi, Is.EqualTo(2));

            // Dönem sürerken gelen farklı tablo ilerlemeyi bozmaz: bir sonraki dönemde geçerli olur.
            oyuncu.SezonuEsitle(new DonemBilgisi("s1", Simdi + TimeSpan.FromDays(30)) { BasamakHedefleri = new[] { 9, 9 } }, Simdi);
            Assert.That(oyuncu.SezonHatti.BasamakSayisi, Is.EqualTo(3));
        }

        // ------------------------------------------------------------------ 3.5 Haftalık hat

        [Test]
        [Property("GDD", "9.9.6")]
        public void Hafta_sonunda_oduller_toplu_verilir_ve_son_gun_bildirimi_bir_kez_gosterilir()
        {
            var oyuncu = Oyuncu();
            var bitis = Simdi + TimeSpan.FromDays(Tam("haftalik_hat_suresi_gun"));
            var hafta = new DonemBilgisi("h1", bitis);
            oyuncu.HaftayiEsitle(hafta, Simdi);
            oyuncu.HaftalikHat.OdulleriAyarla(Oduller(10, 5), Oduller(10, 50));
            oyuncu.HaftalikHat.PuanEkle(8);
            int basamak = oyuncu.HaftalikHat.Basamak;
            int para = oyuncu.Cuzdan.Bakiye;
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));

            // Son güne girilince bildirim bir kez gelir.
            var sonGun = bitis - TimeSpan.FromHours(23);
            oyuncu.HaftayiEsitle(hafta, bitis - TimeSpan.FromHours(25));
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));
            oyuncu.HaftayiEsitle(hafta, sonGun);
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "hafta_son_gun:h1" }));
            oyuncu.HaftayiEsitle(hafta, sonGun + TimeSpan.FromHours(1));
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));

            // Hafta bitince ücretsiz hattın alınmamış ödülleri verilir ve yeni hafta başlar.
            var sonuc = oyuncu.HaftayiEsitle(new DonemBilgisi("h2", bitis + TimeSpan.FromDays(7)), bitis);
            Assert.That(sonuc!.Oduller, Has.Count.EqualTo(basamak));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + (basamak * 5)));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "hafta_bitti:h1" }));
            Assert.That(oyuncu.HaftaDonemi.Kimlik, Is.EqualTo("h2"));
            Assert.That(oyuncu.HaftalikHat.Basamak, Is.EqualTo(0));
            Assert.That(oyuncu.HaftaDonemi.SonGunBildirildi, Is.False, "Yeni haftanın son günü yeniden bildirilir.");
        }

        [Test]
        [Property("GDD", "9.8.11, 9.9.7, 9.7.1")]
        public void Sunucu_esitlemesi_donemleri_sunucu_saatiyle_uygular()
        {
            var saat = new AyarliSaat(Simdi);
            var sunucu = new YerelSahteSunucu(saat);
            sunucu.Ayarlar.Sezon = new DonemBilgisi("s1", Simdi + TimeSpan.FromDays(30));
            sunucu.Ayarlar.Hafta = new DonemBilgisi("h1", Simdi + TimeSpan.FromDays(7));
            sunucu.Ayarlar.AlbumSezonu = new DonemBilgisi("a1", Simdi + TimeSpan.FromDays(60));
            var oyuncu = Oyuncu();
            oyuncu.AlbumSezonu.GorsellerIndi = true;

            Assert.That(SunucuEsitleyici.Uygula(sunucu, oyuncu, new ZorlukEtiketTablosu()), Is.True);
            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.EqualTo("s1"));
            Assert.That(oyuncu.HaftaDonemi.Kimlik, Is.EqualTo("h1"));
            Assert.That(oyuncu.AlbumSezonu.Kimlik, Is.EqualTo("a1"));

            saat.Ilerlet(TimeSpan.FromDays(8));
            sunucu.Ayarlar.Hafta = new DonemBilgisi("h2", saat.SimdiUtc + TimeSpan.FromDays(6));
            SunucuEsitleyici.Uygula(sunucu, oyuncu, new ZorlukEtiketTablosu());
            Assert.That(oyuncu.HaftaDonemi.Kimlik, Is.EqualTo("h2"));
            Assert.That(oyuncu.SezonDonemi.Kimlik, Is.EqualTo("s1"));
        }

        // ------------------------------------------------------------------ 3.6 Albüm

        private static readonly DateTime AlbumBitisi = Simdi + TimeSpan.FromDays(60);

        /// <summary>Sezona girmiş oyuncu; bütün kartlar eşit ağırlıklı "p" paketiyle.</summary>
        private static Oyuncu AlbumluOyuncu(int paketKartSayisi = 3, ParametreTablosu? parametreler = null)
        {
            var p = parametreler ?? P;
            var oyuncu = new Oyuncu(p, Simdi) { EnYuksekBolum = 500 };
            oyuncu.AlbumSezonu.GorsellerIndi = true;
            var yapilandirma = new AlbumYapilandirmasi(p);
            yapilandirma.Paketler["p"] = new KartPaketiTanimi("p", paketKartSayisi, new double[] { 1, 1, 1 }, new double[] { 1, 1, 1, 1 });
            oyuncu.AlbumSezonu.YapilandirmayiAyarla(yapilandirma);
            Assert.That(oyuncu.AlbumSezonunuEsitle(new DonemBilgisi("a1", AlbumBitisi), Simdi), Is.Null);
            Assert.That(oyuncu.AlbumSezonu.Katildi, Is.True);
            return oyuncu;
        }

        private static void SetiDoldur(Album album, int set, int haric = -1)
        {
            for (int kart = 0; kart < album.SetBasinaKart; kart++)
            {
                if (kart != haric) album.KartEkle(set, kart);
            }
        }

        [Test]
        [Property("GDD", "9.7.1")]
        public void Album_indirme_ve_acilis_kapilarindan_gecer_ve_sezona_yeterli_sure_varsa_girilir()
        {
            int indirme = Tam(MetaParametreAdlari.AcilisAlbumIndirme);
            int acilis = Tam(MetaParametreAdlari.AcilisAlbum);
            var asgari = TimeSpan.FromMinutes(Tam(MetaParametreAdlari.AlbumSezonGirisAsgariDk));
            var oyuncu = Oyuncu(indirme - 1);
            var album = oyuncu.AlbumSezonu;

            Assert.That(album.Durum(indirme - 1, Simdi), Is.EqualTo(AlbumSezonDurumu.Kilitli));
            Assert.That(album.Durum(indirme, Simdi), Is.EqualTo(AlbumSezonDurumu.Indiriliyor));
            album.GorsellerIndi = true;
            Assert.That(album.Durum(indirme, Simdi), Is.EqualTo(AlbumSezonDurumu.Bekliyor));

            Assert.That(album.Katil("a1", AlbumBitisi, acilis - 1, Simdi), Is.False, "Albüm henüz açılmadı.");
            Assert.That(album.Katil("a1", Simdi + asgari - TimeSpan.FromSeconds(1), acilis, Simdi), Is.False, "Sezonun bitimine yeterli süre yok.");
            Assert.That(album.Katil("a1", Simdi + asgari, acilis, Simdi), Is.True);
            Assert.That(album.Durum(acilis, Simdi), Is.EqualTo(AlbumSezonDurumu.Aktif));
            Assert.That(album.Durum(acilis, Simdi + asgari), Is.EqualTo(AlbumSezonDurumu.Bitti));
            Assert.That(album.Katil("a2", AlbumBitisi, acilis, Simdi), Is.False, "Sezondayken ikinci sezona girilmez.");
        }

        [Test]
        [Property("GDD", "9.7.3, 9.7.4")]
        public void Kartlarin_uc_kademesi_ve_yildiz_nadirligi_vardir()
        {
            var y = new AlbumYapilandirmasi(P);
            int azami = Tam(MetaParametreAdlari.KartAzamiYildiz);

            Assert.That(y.SetSayisi, Is.EqualTo(Tam(MetaParametreAdlari.SetSayisi)));
            Assert.That(Enumerable.Range(0, y.SetSayisi).Select(y.Kademe).Distinct(), Is.EqualTo(new[] { KartKademesi.Siradan, KartKademesi.Degerli, KartKademesi.CokDegerli }));
            Assert.That(y.UstKademeSeti(y.SetSayisi - 1), Is.True, "En üst kademedeki setler ayrıca işaretlenir.");
            Assert.That(y.UstKademeSeti(0), Is.False);

            var yildizlar = Enumerable.Range(0, y.SetBasinaKart).Select(k => y.Yildiz(0, k)).ToList();
            Assert.That(yildizlar.Min(), Is.EqualTo(1));
            Assert.That(yildizlar.Max(), Is.EqualTo(azami));
            Assert.That(yildizlar, Is.Ordered);

            // Sunucu kademeyi, yıldızı ve set sayısını değiştirebilir; yıldız azamiyi aşmaz.
            y.SetKademeleri[0] = KartKademesi.CokDegerli;
            y.KartYildizlari[(0, 0)] = 99;
            Assert.That(y.Kademe(0), Is.EqualTo(KartKademesi.CokDegerli));
            Assert.That(y.Yildiz(0, 0), Is.EqualTo(azami));
            Assert.That(new AlbumYapilandirmasi(P, setSayisi: 5).SetSayisi, Is.EqualTo(5));
        }

        [Test]
        [Property("GDD", "9.7.2, 9.7.5")]
        public void Paket_icerigi_agirlik_tablosuyla_secilir()
        {
            var oyuncu = AlbumluOyuncu();
            var album = oyuncu.AlbumSezonu;

            // Yalnızca sıradan kademe ve tek yıldız ağırlık taşıyor: başka kart çıkamaz.
            album.Yapilandirma.Paketler["dar"] = new KartPaketiTanimi("dar", 5, new double[] { 1, 0, 0 }, new double[] { 1, 0, 0, 0 });
            Assert.That(album.PaketEkle("dar", 4, Simdi), Is.True);
            var rastgele = new TohumluRastgele(7);
            var cekilen = Enumerable.Range(0, 4).SelectMany(_ => album.PaketAc("dar", rastgele, Simdi)!).ToList();

            Assert.That(cekilen, Has.Count.EqualTo(20));
            Assert.That(cekilen.Select(k => album.Yapilandirma.Kademe(k.Set)), Is.All.EqualTo(KartKademesi.Siradan));
            Assert.That(cekilen.Select(k => k.Yildiz), Is.All.EqualTo(1));
            Assert.That(cekilen.Count(k => k.Yeni), Is.EqualTo(album.Album.KartSayisi), "Yeni kartlar işaretlenir ve albüme eklenir.");
            Assert.That(album.PaketAc("dar", rastgele, Simdi), Is.Null, "Bekleyen paket kalmadı.");
            Assert.That(album.PaketAc("bilinmeyen", rastgele, Simdi), Is.Null);

            Assert.That(
                () => new KartPaketiTanimi("x", 0, new double[] { 1, 1, 1 }, new double[] { 1 }),
                Throws.ArgumentException);
        }

        [Test]
        [Property("GDD", "9.7.6")]
        public void Acima_kurali_cift_kartlar_arttikca_eksik_karti_getirir()
        {
            // Albümde tek kart eksik; 12 kartlık paket açılıyor.
            int EksikKalan(ParametreTablosu p)
            {
                int kalan = 0;
                for (int tohum = 1; tohum <= 20; tohum++)
                {
                    var oyuncu = AlbumluOyuncu(paketKartSayisi: 12, parametreler: p);
                    var album = oyuncu.AlbumSezonu;
                    for (int set = 0; set < album.Album.SetSayisi; set++)
                    {
                        SetiDoldur(album.Album, set, haric: set == 0 ? 3 : -1);
                    }

                    album.PaketEkle("p", 1, Simdi);
                    album.PaketAc("p", new TohumluRastgele(tohum), Simdi);
                    if (!album.Album.HepsiDolu) kalan++;
                }

                return kalan;
            }

            // Katsayı 0.1 iken en geç on çiftten sonra eksik kart kesin gelir.
            Assert.That(EksikKalan(P), Is.EqualTo(0));
            Assert.That(EksikKalan(P.Degistir(MetaParametreAdlari.AcimaKatsayisi, 0)), Is.GreaterThan(10), "Acıma kuralı olmadan 96 kartta biri nadiren gelir.");
        }

        [Test]
        [Property("GDD", "9.7.8")]
        public void Cift_kartlar_yildiza_donusur_ve_yildizla_cift_kart_sandigi_acilir()
        {
            var oyuncu = AlbumluOyuncu(paketKartSayisi: 40);
            var album = oyuncu.AlbumSezonu;
            for (int set = 0; set < album.Album.SetSayisi; set++)
            {
                SetiDoldur(album.Album, set);
            }

            album.PaketEkle("p", 1, Simdi);
            var cekilen = album.PaketAc("p", new TohumluRastgele(3), Simdi)!;

            Assert.That(cekilen.Select(k => k.Yeni), Is.All.False);
            Assert.That(album.CiftYildizi, Is.EqualTo(cekilen.Sum(k => k.Yildiz) * 2), "Öneri: kartın nadirlik yıldızı × 2.");
            Assert.That(album.ArdisikCift, Is.EqualTo(40));

            var fiyatlar = P.TamSayiListesi(MetaParametreAdlari.CiftKartSandikFiyatlari);
            Assert.That(fiyatlar, Has.Count.EqualTo(3), "Sandıkların üç kademeli fiyatı vardır.");
            int yildiz = album.CiftYildizi;
            Assert.That(album.CiftSandigiAc(0), Is.True);
            Assert.That(album.CiftYildizi, Is.EqualTo(yildiz - fiyatlar[0]));
            Assert.That(album.CiftSandigiAc(3), Is.False);
            Assert.That(album.CiftSandigiAc(2), Is.EqualTo(yildiz - fiyatlar[0] >= fiyatlar[2]));
        }

        [Test]
        [Property("GDD", "9.7.2, 9.7.10, 9.6.5")]
        public void Odulden_gelen_kart_paketi_sezona_gore_degisir_ve_aktif_sezon_yokken_verilmez()
        {
            var odul = new OdulPaketi();
            odul.AlbumEsyalari["olay_paketi"] = 2;
            odul.AlbumEsyalari["p"] = 1;

            var oyuncu = AlbumluOyuncu();
            oyuncu.AlbumSezonu.Yapilandirma.PaketEslemesi["olay_paketi"] = "sezon_ozel";
            oyuncu.OdulVer(odul, Simdi);
            Assert.That(oyuncu.AlbumSezonu.BekleyenPaketler, Is.EquivalentTo(new Dictionary<string, int> { { "sezon_ozel", 2 }, { "p", 1 } }));

            var sezonsuz = Oyuncu();
            sezonsuz.OdulVer(odul, Simdi);
            Assert.That(sezonsuz.AlbumSezonu.BekleyenPaketler, Is.Empty);

            oyuncu.OdulVer(odul, AlbumBitisi);
            Assert.That(oyuncu.AlbumSezonu.BekleyenPaketler["p"], Is.EqualTo(1), "Süresi dolmuş sezona paket eklenmez.");
        }

        [Test]
        [Property("GDD", "9.7.7")]
        public void Set_ve_buyuk_odul_alinir_ve_sezon_rozetli_asamalardan_gecer()
        {
            var oyuncu = AlbumluOyuncu();
            var album = oyuncu.AlbumSezonu;
            album.Yapilandirma.SetOdulleri[0] = new OdulPaketi { Para = 70 };
            album.Yapilandirma.BuyukOdul = new OdulPaketi { Para = 900 };
            int asamaSayisi = Tam(MetaParametreAdlari.RozetliAsamaSayisi);

            Assert.That(album.SetOdulunuAl(0), Is.Null, "Set dolmadı.");
            SetiDoldur(album.Album, 0);
            Assert.That(album.SetOdulunuAl(0)!.Para, Is.EqualTo(70));
            Assert.That(album.SetOdulunuAl(0), Is.Null, "Set ödülü bir kez alınır.");
            Assert.That(album.BuyukOduluAl(), Is.Null);
            Assert.That(album.SonrakiAsama(), Is.False);

            for (int asama = 1; asama <= asamaSayisi; asama++)
            {
                Assert.That(album.Asama, Is.EqualTo(asama));
                for (int set = 0; set < album.Album.SetSayisi; set++)
                {
                    SetiDoldur(album.Album, set);
                }

                Assert.That(album.BuyukOduluAl()!.Para, Is.EqualTo(900));
                Assert.That(album.Durum(500, Simdi), Is.EqualTo(AlbumSezonDurumu.AsamaTamam));
                Assert.That(album.SonrakiAsama(), Is.EqualTo(asama < asamaSayisi));
            }

            Assert.That(album.Asama, Is.EqualTo(asamaSayisi), "Son aşamadan sonra geçilmez.");

            // Yeni aşamada setler boştur ve ödülleri yeniden alınabilir.
            var ikinci = AlbumluOyuncu().AlbumSezonu;
            for (int set = 0; set < ikinci.Album.SetSayisi; set++) SetiDoldur(ikinci.Album, set);
            ikinci.BuyukOduluAl();
            ikinci.SonrakiAsama();
            Assert.That(ikinci.Album.KartSayisi, Is.EqualTo(0));
            Assert.That(ikinci.Durum(500, Simdi), Is.EqualTo(AlbumSezonDurumu.Aktif));
        }

        [Test]
        [Property("GDD", "9.7.11")]
        public void Album_sezonu_bitince_alinmamis_oduller_verilir_ve_yeni_sezon_beklenir()
        {
            var oyuncu = AlbumluOyuncu();
            var album = oyuncu.AlbumSezonu;
            album.Yapilandirma.SetOdulleri[0] = new OdulPaketi { Para = 70 };
            album.Yapilandirma.SetOdulleri[1] = new OdulPaketi { Para = 80 };
            SetiDoldur(album.Album, 0);
            SetiDoldur(album.Album, 1);
            album.SetOdulunuAl(0);
            album.PaketEkle("p", 2, Simdi);
            int para = oyuncu.Cuzdan.Bakiye;

            Assert.That(oyuncu.AlbumSezonunuEsitle(new DonemBilgisi("a1", AlbumBitisi), AlbumBitisi - TimeSpan.FromSeconds(1)), Is.Null);

            var sonuc = oyuncu.AlbumSezonunuEsitle(new DonemBilgisi("a1", AlbumBitisi), AlbumBitisi);
            Assert.That(sonuc!.Kimlik, Is.EqualTo("a1"));
            Assert.That(sonuc.Oduller.Select(o => o.Para), Is.EqualTo(new[] { 80 }), "Alınmış set ödülü yeniden verilmez.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 80));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Does.Contain("album_bitti:a1"));

            Assert.That(album.Durum(500, AlbumBitisi), Is.EqualTo(AlbumSezonDurumu.Bekliyor));
            Assert.That(album.Katildi, Is.False, "Biten sezona yeniden girilmez.");
            Assert.That(album.BekleyenPaketler, Is.Empty);
            Assert.That(album.Album.KartSayisi, Is.EqualTo(0));

            oyuncu.AlbumSezonunuEsitle(new DonemBilgisi("a2", AlbumBitisi + TimeSpan.FromDays(60)), AlbumBitisi);
            Assert.That(album.Kimlik, Is.EqualTo("a2"));
        }

        [Test]
        [Property("GDD", "9.7")]
        public void Album_sezonu_kayitla_korunur_ve_eski_album_kaydi_okunur()
        {
            var oyuncu = AlbumluOyuncu();
            var album = oyuncu.AlbumSezonu;
            SetiDoldur(album.Album, 0);
            album.SetOdulunuAl(0);
            album.Album.KartEkle(2, 5);
            album.PaketEkle("p", 3, Simdi);
            album.PaketAc("p", new TohumluRastgele(11), Simdi);

            string kayit = OyuncuKaydi.Yaz(oyuncu);
            var yuklenen = OyuncuKaydi.Yukle(kayit, P, Simdi);
            var y = yuklenen.AlbumSezonu;

            Assert.That(OyuncuKaydi.Yaz(yuklenen), Is.EqualTo(kayit));
            Assert.That(y.Kimlik, Is.EqualTo("a1"));
            Assert.That(y.Bitis, Is.EqualTo(AlbumBitisi));
            Assert.That(y.GorsellerIndi, Is.True);
            Assert.That(y.BekleyenPaketler["p"], Is.EqualTo(2));
            Assert.That(y.CiftYildizi, Is.EqualTo(album.CiftYildizi));
            Assert.That(y.Album.KartSayisi, Is.EqualTo(album.Album.KartSayisi));
            Assert.That(y.Album.Var(2, 5), Is.True);
            Assert.That(y.SetOduluAlinabilir(0), Is.False, "Alınmış set ödülü kayıttan sonra da alınmıştır.");

            // Albüm sezonu eklenmeden önceki kayıt biçimi.
            var eski = OyuncuKaydi.Yukle("{\"surum\":1,\"album\":{\"setler\":{\"0\":[1,4]},\"cift_kart_sayisi\":2}}", P, Simdi);
            Assert.That(eski.Album.Var(0, 4), Is.True);
            Assert.That(eski.Album.CiftKartSayisi, Is.EqualTo(2));
            Assert.That(eski.AlbumSezonu.Katildi, Is.False);
        }
    }
}
