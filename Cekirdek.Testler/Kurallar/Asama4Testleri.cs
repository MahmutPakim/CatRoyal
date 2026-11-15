using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Testler.Vakalar;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>
    /// Yol haritası Aşama 4: olay yöneticisi (GDD 10.0.1–10.0.5, 7.6.3), süre sonu (10.0.6–10.0.9), basamaklı toplama
    /// şablonu ve çarpan etkinlikleri (10.9.1, 10.9.6), risk basamaklarının kalanları (10.5).
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama4Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Gun = TimeSpan.FromDays(1);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        private static Oyuncu Oyuncu(int enYuksekBolum = 500, ParametreTablosu? p = null) =>
            new Oyuncu(p ?? P, Simdi) { EnYuksekBolum = enYuksekBolum };

        private static OlayTakvimKaydi Kayit(
            string kimlik,
            OlayTuru tur = OlayTuru.BasamakliToplama,
            double baslangicGun = 0,
            double sureGun = 3,
            int[]? hedefler = null,
            int odulParasi = 0)
        {
            var kayit = new OlayTakvimKaydi(kimlik, tur, Simdi + TimeSpan.FromDays(baslangicGun), Simdi + TimeSpan.FromDays(baslangicGun + sureGun))
            {
                BasamakHedefleri = hedefler,
            };
            if (odulParasi > 0 && hedefler != null)
            {
                kayit.Oduller.AddRange(hedefler.Select(_ => (OdulPaketi?)new OdulPaketi { Para = odulParasi }));
            }

            return kayit;
        }

        private static BasamakliToplamaOlayi Toplama(Oyuncu oyuncu, string kimlik) => (BasamakliToplamaOlayi)oyuncu.Olaylar.Bul(kimlik)!;

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

        private const string KazanilanBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

        private const string KaybedilenBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 1,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 99 }] }";

        /// <summary>Bölümü oynar; kazanılan bölümde 4 hamle kalır. Yedek hamle hakkı gibi seri özellikleri kapalı bir oyuncuyla kullanılır.</summary>
        private static BolumSonucu Oyna(
            Oyuncu oyuncu, bool kazan, ZorlukEtiketi etiket = ZorlukEtiketi.Normal, bool kurtarma = false, bool cik = false, int bolum = 12)
        {
            var etiketler = new ZorlukEtiketTablosu();
            etiketler.Degistir(bolum, etiket);
            var akis = new BolumAkisi(oyuncu, etiketler, bolum) { Kurtarma = kurtarma };
            var olaylar = new List<Olay>();
            var oturum = akis.Baslat(
                BolumYukleyici.JsondanOku(kazan ? KazanilanBolum : KaybedilenBolum, P),
                AkBayraklari.Varsayilan, 7, Simdi, Enumerable.Empty<TakviyeTuru>(), olaylar);
            Assert.That(oturum, Is.Not.Null);
            if (cik)
            {
                return akis.SonucuIsle(Simdi, cikildi: true);
            }

            Assert.That(oturum!.Kaydir(H(1, 4), H(1, 3), olaylar), Is.EqualTo(HamleSonucu.Gecerli));
            if (!kazan && oturum.Durum == BolumDurumu.EkHamleTeklifi)
            {
                oturum.EkHamleTeklifiniReddet(olaylar);
            }

            return akis.SonucuIsle(Simdi, cikildi: !kazan && oturum.Durum != BolumDurumu.Kaybedildi);
        }

        // ------------------------------------------------------------------ 4.1 Olay yöneticisi

        [Test]
        [Property("GDD", "10.0.1, 10.0.3, 9.4.5")]
        public void Yonetici_baslamis_olaylari_takvimden_kurar_ve_pencerelerini_akisa_ekler()
        {
            var oyuncu = Oyuncu();
            var takvim = new[]
            {
                Kayit("bahce", hedefler: new[] { 4, 6 }),
                Kayit("kopru", baslangicGun: 5, hedefler: new[] { 4, 6 }),
            };

            Assert.That(oyuncu.Olaylar.TakvimiUygula(takvim, Simdi), Is.Empty);

            Assert.That(oyuncu.Olaylar.Kurulu.Select(o => o.Kimlik), Is.EqualTo(new[] { "bahce" }), "Başlamamış olay kurulmaz.");
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Single().Kimlik, Is.EqualTo("bahce"));
            Assert.That(Toplama(oyuncu, "bahce").Bitis, Is.EqualTo(Simdi + (Gun * 3)));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "olay_tanitimi:bahce", "olay_baslangici:bahce" }));

            // Yeniden uygulamak olayı ikinci kez kurmaz ve pencereleri yeniden eklemez.
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + Gun);
            Assert.That(oyuncu.Olaylar.Kurulu, Has.Count.EqualTo(1));
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));

            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + (Gun * 5));
            Assert.That(oyuncu.Olaylar.Kurulu.Select(o => o.Kimlik), Is.EqualTo(new[] { "kopru" }), "Süresi dolan biter, başlayan kurulur.");
        }

        [Test]
        [Property("GDD", "10.0.1, 10.9.1, 9.4")]
        public void Olay_acilis_bolumunden_once_oyuncuya_acik_degildir_ve_turler_aralikla_acilir()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisIlkToplamaOlayi);
            int aralik = Tam(MetaParametreAdlari.OlayTuruAcilisAraligi);
            var ucuncuTur = Kayit("kale", hedefler: new[] { 4 });
            ucuncuTur.TurSirasi = 2;
            var takvim = new[] { Kayit("bahce", hedefler: new[] { 4 }), ucuncuTur };

            var oyuncu = Oyuncu(acilis - 1);
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            Assert.That(oyuncu.Olaylar.Kurulu, Has.Count.EqualTo(2));
            Assert.That(oyuncu.Olaylar.Acik(Simdi), Is.Empty);
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0), "Kapalı olayın penceresi gösterilmez.");

            // Kapalı olaya puan gitmez.
            Oyna(oyuncu, kazan: true);
            Assert.That(Toplama(oyuncu, "bahce").ToplamToplanan, Is.EqualTo(0));

            oyuncu.EnYuksekBolum = acilis;
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Select(o => o.Kimlik), Is.EqualTo(new[] { "bahce" }));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Does.Contain("olay_tanitimi:bahce"));

            Assert.That(oyuncu.Olaylar.Bul("kale")!.AcilisBolumu(oyuncu.Kapilar), Is.EqualTo(acilis + (2 * aralik)));
            oyuncu.EnYuksekBolum = acilis + (2 * aralik);
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Select(o => o.Kimlik), Is.EquivalentTo(new[] { "bahce", "kale" }));
        }

        [Test]
        [Property("GDD", "10.0.5, 10.9.2, 7.6.3")]
        public void Bolum_kazanimi_acik_olaylara_carpanli_puan_ve_kalan_hamle_bilgisi_dagitir()
        {
            var oyuncu = Oyuncu();
            oyuncu.Olaylar.TakvimiUygula(new[] { Kayit("bahce", hedefler: new[] { 1000 }), Kayit("kopru", hedefler: new[] { 1000 }) }, Simdi);
            int taban = EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Zor);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);

            var sonuc = Oyna(oyuncu, kazan: true, ZorlukEtiketi.Zor);
            Assert.That(sonuc.OlayTabanPuani, Is.EqualTo(taban));
            Assert.That(sonuc.KalanHamle, Is.EqualTo(4), "Artan hamle bilgisi olaylara iletilir.");
            Assert.That(Toplama(oyuncu, "bahce").BasamakPuani, Is.EqualTo(taban));
            Assert.That(Toplama(oyuncu, "kopru").BasamakPuani, Is.EqualTo(taban), "Aynı puan bütün toplama olaylarına verilir.");

            // Çarpanla oynanan bölümün kazanımı olay puanını katlar.
            oyuncu.Carpan.BiletKullan(Simdi);
            Oyna(oyuncu, kazan: true, ZorlukEtiketi.Zor);
            Assert.That(Toplama(oyuncu, "bahce").BasamakPuani, Is.EqualTo(taban + (taban * kat)));
            Assert.That(Toplama(oyuncu, "bahce").Carpilabilir, Is.True);

            // Kayıp, toplama olayının ilerlemesini değiştirmez.
            var kayip = Oyna(oyuncu, kazan: false);
            Assert.That(kayip.Kaybedildi, Is.True);
            Assert.That(Toplama(oyuncu, "bahce").BasamakPuani, Is.EqualTo(taban + (taban * kat)));
        }

        [Test]
        [Property("GDD", "10.0.2")]
        public void Sunucu_olayi_tek_basina_kapatabilir()
        {
            var oyuncu = Oyuncu();
            var bahce = Kayit("bahce", hedefler: new[] { 1000 });
            var takvim = new[] { bahce, Kayit("kopru", hedefler: new[] { 1000 }) };
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);

            bahce.Kapali = true;
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Select(o => o.Kimlik), Is.EqualTo(new[] { "kopru" }));

            Oyna(oyuncu, kazan: true);
            Assert.That(Toplama(oyuncu, "bahce").ToplamToplanan, Is.EqualTo(0));
            Assert.That(Toplama(oyuncu, "kopru").ToplamToplanan, Is.GreaterThan(0));

            bahce.Kapali = false;
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            Assert.That(oyuncu.Olaylar.Acik(Simdi), Has.Count.EqualTo(2));
        }

        [Test]
        [Property("GDD", "10.0.4, 9.4.8")]
        public void Olay_bagimliligi_bir_olayi_engeller_ya_da_odulunu_zorla_aldirir()
        {
            // Engelleme: "kopru", "bahce" açıkken engellidir.
            var oyuncu = Oyuncu();
            var kopru = Kayit("kopru", sureGun: 6, hedefler: new[] { 1000 });
            kopru.EngelleyenOlaylar.Add("bahce");
            var takvim = new[] { Kayit("bahce", sureGun: 2, hedefler: new[] { 1000 }), kopru };
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);

            Assert.That(oyuncu.Olaylar.Bul("kopru")!.Engelli, Is.True);
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Select(o => o.Kimlik), Is.EqualTo(new[] { "bahce" }));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Does.Not.Contain("olay_tanitimi:kopru"));

            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + (Gun * 2));
            Assert.That(oyuncu.Olaylar.Bul("kopru")!.Engelli, Is.False, "Engelleyen olay bitince engel kalkar.");
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Does.Contain("olay_tanitimi:kopru"));

            // Zorla ödül aldırma: "final" başlayınca "bahce"nin alınmamış ödülleri kendiliğinden alınır.
            var ikinci = Oyuncu();
            var bahce = Kayit("bahce", sureGun: 6, hedefler: new[] { 1, 1, 1000 }, odulParasi: 50);
            bahce.OdulunuZorlaAldiranOlaylar.Add("final");
            var takvim2 = new[] { bahce, Kayit("final", baslangicGun: 1, hedefler: new[] { 1000 }) };
            ikinci.Olaylar.TakvimiUygula(takvim2, Simdi);
            Oyna(ikinci, kazan: true);
            int para = ikinci.Cuzdan.Bakiye;
            Assert.That(Toplama(ikinci, "bahce").Basamak, Is.EqualTo(2));

            ikinci.Olaylar.TakvimiUygula(takvim2, Simdi + Gun);
            Assert.That(ikinci.Cuzdan.Bakiye, Is.EqualTo(para + 100));
            Assert.That(Toplama(ikinci, "bahce").OdulAlinabilir(0), Is.False);
        }

        [Test]
        [Property("GDD", "10.0.9, 9.4.6")]
        public void Bolum_oncesi_ekraninda_sinirli_sayida_olay_bitisi_yakin_olan_once_gosterilir()
        {
            var oyuncu = Oyuncu();
            oyuncu.Olaylar.TakvimiUygula(
                new[]
                {
                    Kayit("uzun", sureGun: 9, hedefler: new[] { 5 }),
                    Kayit("kisa", sureGun: 1, hedefler: new[] { 5 }),
                    Kayit("orta", sureGun: 4, hedefler: new[] { 5 }),
                },
                Simdi);

            Assert.That(Tam(MetaParametreAdlari.SeviyeOncesiAzamiOlay), Is.EqualTo(2));
            Assert.That(oyuncu.Olaylar.BolumOncesi(Simdi).Select(o => o.Kimlik), Is.EqualTo(new[] { "kisa", "orta" }));
            Assert.That(oyuncu.Olaylar.BolumOncesi(Simdi + (Gun * 2)).Select(o => o.Kimlik), Is.EqualTo(new[] { "orta", "uzun" }));
        }

        // ------------------------------------------------------------------ 4.2 Süre sonu

        [Test]
        [Property("GDD", "10.0.7, 10.0.8, 10.9.3")]
        public void Sure_bitince_alinmamis_oduller_kendiliginden_verilir_ve_bitis_penceresi_gosterilir()
        {
            var oyuncu = Oyuncu();
            var takvim = new[] { Kayit("bahce", hedefler: new[] { 1, 1, 1000 }, odulParasi: 30) };
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            HepsiniGoster(oyuncu.Pencereler);
            Oyna(oyuncu, kazan: true);
            int para = oyuncu.Cuzdan.Bakiye;

            // Ödüller tek tek alınabilir.
            Assert.That(oyuncu.Olaylar.OduluAl("bahce", 0, Simdi), Is.True);
            Assert.That(oyuncu.Olaylar.OduluAl("bahce", 0, Simdi), Is.False);
            Assert.That(oyuncu.Olaylar.OduluAl("bahce", 2, Simdi), Is.False, "Basamak dolmadı.");
            Assert.That(oyuncu.Olaylar.OduluAl("yok", 0, Simdi), Is.False);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 30));

            var bitis = Simdi + (Gun * 3);
            Assert.That(oyuncu.Olaylar.TakvimiUygula(takvim, bitis - TimeSpan.FromSeconds(1)), Is.Empty);
            var bitenler = oyuncu.Olaylar.TakvimiUygula(takvim, bitis);

            Assert.That(bitenler.Single().Kimlik, Is.EqualTo("bahce"));
            Assert.That(bitenler.Single().Oduller.Select(o => o.Para), Is.EqualTo(new[] { 30 }), "Alınmış ödül yeniden verilmez.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 60));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "olay_bitti:bahce" }));
            Assert.That(oyuncu.Olaylar.Kurulu, Is.Empty);

            // Sunucu biten olayı uzatmış gibi bildirse de olay yeniden kurulmaz.
            var uzatilmis = new[] { new OlayTakvimKaydi("bahce", OlayTuru.BasamakliToplama, Simdi, bitis + Gun) };
            Assert.That(oyuncu.Olaylar.TakvimiUygula(uzatilmis, bitis), Is.Empty);
            Assert.That(oyuncu.Olaylar.Kurulu, Is.Empty);
        }

        [Test]
        [Property("GDD", "10.9.3")]
        public void Oduller_toplu_alinabilir()
        {
            var oyuncu = Oyuncu();
            oyuncu.Olaylar.TakvimiUygula(new[] { Kayit("bahce", hedefler: new[] { 1, 1, 1000 }, odulParasi: 30) }, Simdi);
            Oyna(oyuncu, kazan: true);
            int para = oyuncu.Cuzdan.Bakiye;

            Assert.That(oyuncu.Olaylar.TopluAl("bahce", Simdi), Is.EqualTo(2));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 60));
            Assert.That(oyuncu.Olaylar.TopluAl("bahce", Simdi), Is.EqualTo(0));
        }

        [Test]
        [Property("GDD", "10.0.6, 10.5.6")]
        public void Sure_bitince_kalan_toplanabilir_ayni_turun_sonraki_olayina_aktarilir()
        {
            int taban = EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Normal);

            int SonrakiOlayinPuani(bool aktarilir)
            {
                var oyuncu = Oyuncu();
                var ilk = Kayit("bahce1", sureGun: 3, hedefler: new[] { 1, 1000 });
                ilk.ToplanabilirAktarilir = aktarilir;
                var takvim = new[] { ilk, Kayit("bahce2", baslangicGun: 4, hedefler: new[] { 1000 }) };
                oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
                Oyna(oyuncu, kazan: true);
                Assert.That(Toplama(oyuncu, "bahce1").AktarilacakToplanabilir, Is.EqualTo(taban - 1), "Basamağa dönüşmemiş kısım.");

                // Olay biter; sonraki olay henüz başlamamıştır: toplanabilir bekler.
                oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + (Gun * 3));
                Assert.That(oyuncu.Olaylar.AktarilmayiBekleyen(OlayTuru.BasamakliToplama), Is.EqualTo(aktarilir ? taban - 1 : 0));

                // Bekleyen toplanabilir kayıtla korunur ve yeni olaya geçer.
                oyuncu = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
                oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + (Gun * 4));
                Assert.That(oyuncu.Olaylar.AktarilmayiBekleyen(OlayTuru.BasamakliToplama), Is.EqualTo(0));
                return Toplama(oyuncu, "bahce2").BasamakPuani;
            }

            Assert.That(SonrakiOlayinPuani(aktarilir: true), Is.EqualTo(taban - 1));
            Assert.That(SonrakiOlayinPuani(aktarilir: false), Is.EqualTo(0));
        }

        [Test]
        [Property("GDD", "10.0.8")]
        public void Olaylar_kayitla_korunur_ve_sunucuya_ulasilamasa_da_suresi_dolunca_odullerini_verir()
        {
            var oyuncu = Oyuncu();
            var risk = Kayit("risk", OlayTuru.RiskBasamaklari, sureGun: 5);
            risk.EngelleyenOlaylar.Add("yok");
            oyuncu.Olaylar.TakvimiUygula(new[] { Kayit("bahce", hedefler: new[] { 1, 1, 1000 }, odulParasi: 30), risk }, Simdi);
            ((RiskBasamaklariOlayi)oyuncu.Olaylar.Bul("risk")!).Gir();
            Oyna(oyuncu, kazan: true, ZorlukEtiketi.AsiriZor);
            Oyna(oyuncu, kazan: false);
            oyuncu.Olaylar.OduluAl("bahce", 1, Simdi);

            string kayit = OyuncuKaydi.Yaz(oyuncu);
            var yuklenen = OyuncuKaydi.Yukle(kayit, P, Simdi);
            Assert.That(OyuncuKaydi.Yaz(yuklenen), Is.EqualTo(kayit));

            var eski = (RiskBasamaklariOlayi)oyuncu.Olaylar.Bul("risk")!;
            var yeni = (RiskBasamaklariOlayi)yuklenen.Olaylar.Bul("risk")!;
            Assert.That(yeni.Girildi, Is.True);
            Assert.That(yeni.Basamak, Is.EqualTo(eski.Basamak));
            Assert.That(yeni.BasamakPuani, Is.EqualTo(eski.BasamakPuani));
            Assert.That(yeni.SansHakki, Is.EqualTo(eski.SansHakki));
            Assert.That(yeni.SansHakki, Is.LessThan(Tam(MetaParametreAdlari.RiskBasamakSansHakki)));
            Assert.That(yeni.Bitis, Is.EqualTo(Simdi + (Gun * 5)));

            // Takvim alınamıyor: süresi dolan olay yine biter ve kayıttaki ödül içeriğiyle ödülünü verir.
            int para = yuklenen.Cuzdan.Bakiye;
            var bitenler = yuklenen.Olaylar.TakvimiUygula(null, Simdi + (Gun * 3));
            Assert.That(bitenler.Single().Kimlik, Is.EqualTo("bahce"));
            Assert.That(yuklenen.Cuzdan.Bakiye, Is.EqualTo(para + 30));
            Assert.That(yuklenen.Olaylar.Kurulu.Select(o => o.Kimlik), Is.EqualTo(new[] { "risk" }));
        }

        [Test]
        [Property("GDD", "10.0.1")]
        public void Sunucu_esitlemesi_olay_takvimini_de_uygular()
        {
            var saat = new AyarliSaat(Simdi);
            var sunucu = new YerelSahteSunucu(saat);
            sunucu.OlayEkle(Kayit("bahce", hedefler: new[] { 5 }));
            var oyuncu = Oyuncu();

            SunucuEsitleyici.Uygula(sunucu, oyuncu, new ZorlukEtiketTablosu());
            Assert.That(oyuncu.Olaylar.Kurulu.Single().Kimlik, Is.EqualTo("bahce"));

            saat.Ilerlet(Gun * 3);
            SunucuEsitleyici.Uygula(sunucu, oyuncu, new ZorlukEtiketTablosu());
            Assert.That(oyuncu.Olaylar.Kurulu, Is.Empty);
        }

        // ------------------------------------------------------------------ 4.3 Şablon ve çarpan etkinlikleri

        [Test]
        [Property("GDD", "10.9.1")]
        public void Sunucu_tablo_vermezse_toplama_olayi_parametredeki_basamak_sayisiyla_kurulur()
        {
            var oyuncu = Oyuncu();
            oyuncu.Olaylar.TakvimiUygula(new[] { Kayit("bahce"), Kayit("kopru", hedefler: new[] { 3, 3 }) }, Simdi);

            Assert.That(Toplama(oyuncu, "bahce").BasamakSayisi, Is.EqualTo(Tam(MetaParametreAdlari.ToplamaOlayiBasamakSayisi)));
            Assert.That(Toplama(oyuncu, "kopru").BasamakSayisi, Is.EqualTo(2));
            Assert.That(
                () => new BasamakliToplamaOlayi("x", MetaParametreAdlari.AcilisIlkToplamaOlayi, Simdi, new int[0]),
                Throws.ArgumentException);
        }

        [Test]
        [Property("GDD", "10.9.6, 9.10")]
        public void Carpan_etkinligi_puan_formulune_yalnizca_carpan_ekler()
        {
            var oyuncu = Oyuncu();
            var etkinlik = Kayit("hafta_sonu", OlayTuru.CarpanEtkinligi, sureGun: 2);
            etkinlik.CarpanKati = 2;
            var takvim = new[] { etkinlik, Kayit("bahce", sureGun: 5, hedefler: new[] { 1000 }) };
            int taban = EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Normal);

            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi);
            Assert.That(oyuncu.Olaylar.Kurulu.Select(o => o.Kimlik), Is.EqualTo(new[] { "bahce" }), "Çarpan etkinliği ayrı bir olay kurmaz.");
            Assert.That(oyuncu.Carpan.AktifCarpan(Simdi), Is.EqualTo(2));
            Assert.That(oyuncu.Carpan.AktifCarpan(Simdi + (Gun * 2) - TimeSpan.FromMinutes(1)), Is.EqualTo(2));
            Assert.That(oyuncu.Carpan.AktifCarpan(Simdi + (Gun * 2)), Is.EqualTo(1), "Etkinlik bitince çarpan biter.");

            Oyna(oyuncu, kazan: true);
            Assert.That(Toplama(oyuncu, "bahce").BasamakPuani, Is.EqualTo(taban * 2));

            // Aynı etkinlik yeniden uygulanınca süre uzamaz; kayıttan sonra da öyle.
            oyuncu = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            oyuncu.Olaylar.TakvimiUygula(takvim, Simdi + Gun);
            Assert.That(oyuncu.Carpan.AktifCarpan(Simdi + (Gun * 2)), Is.EqualTo(1));

            // Kat verilmezse çarpan biletinin katı kullanılır; çarpan kapısı kapalı oyuncuya etkinlik uygulanmaz.
            var varsayilan = Oyuncu();
            varsayilan.Olaylar.TakvimiUygula(new[] { Kayit("hs", OlayTuru.CarpanEtkinligi) }, Simdi);
            Assert.That(varsayilan.Carpan.AktifCarpan(Simdi), Is.EqualTo(Tam(MetaParametreAdlari.CarpanBiletiKati)));

            var erken = Oyuncu(Tam(MetaParametreAdlari.AcilisCarpanBileti) - 1);
            erken.Olaylar.TakvimiUygula(new[] { Kayit("hs", OlayTuru.CarpanEtkinligi) }, Simdi);
            Assert.That(erken.Carpan.AktifCarpan(Simdi), Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ 4.4 Risk basamakları

        private static RiskBasamaklariOlayi RiskKur(Oyuncu oyuncu, bool gir = true, bool aktarilir = false)
        {
            var kayit = Kayit("risk", OlayTuru.RiskBasamaklari);
            kayit.ToplanabilirAktarilir = aktarilir;
            oyuncu.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            var olay = (RiskBasamaklariOlayi)oyuncu.Olaylar.Bul("risk")!;
            if (gir) olay.Gir();
            return olay;
        }

        [Test]
        [Property("GDD", "10.5.1, 10.5.2, 10.5.4")]
        public void Risk_basamaklari_acilis_bolumunde_acilir_ve_girilmeden_ilerlemez()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisRiskBasamaklari);
            var erken = Oyuncu(acilis - 1);
            RiskKur(erken);
            Assert.That(erken.Olaylar.Acik(Simdi), Is.Empty);

            var oyuncu = Oyuncu(acilis);
            var olay = RiskKur(oyuncu, gir: false);
            Assert.That(oyuncu.Olaylar.Acik(Simdi).Single(), Is.SameAs(olay));
            Assert.That(olay.Hedefler, Is.EqualTo(P.TamSayiListesi(MetaParametreAdlari.RiskBasamakHedefleri)));

            // Olaya girilmeden kazanım toplanabilir getirmez, kayıp hak götürmez.
            Oyna(oyuncu, kazan: true);
            Oyna(oyuncu, kazan: false);
            Assert.That(olay.ToplamToplanan, Is.EqualTo(0));
            Assert.That(olay.SansHakki, Is.EqualTo(Tam(MetaParametreAdlari.RiskBasamakSansHakki)));

            // Girince tam haklı aktif olur; kazanılan miktar 10.9.2'deki puandır.
            olay.Gir();
            Oyna(oyuncu, kazan: true, ZorlukEtiketi.Zor);
            Assert.That(olay.ToplamToplanan, Is.EqualTo(EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Zor)));
        }

        [Test]
        [Property("GDD", "10.5.2")]
        public void Risk_basamaklarinda_toplanabilirin_ust_siniri_vardir()
        {
            var p = P.Degistir(MetaParametreAdlari.RiskBasamakAzamiToplanabilir, 10);
            var olay = new RiskBasamaklariOlayi("risk", Simdi + Gun, p);
            olay.Gir();

            olay.BolumKazanildi(8);
            olay.BolumKazanildi(8);
            Assert.That(olay.ToplamToplanan, Is.EqualTo(10));
            olay.BolumKazanildi(8);
            Assert.That(olay.ToplamToplanan, Is.EqualTo(10));
            Assert.That(Tam(MetaParametreAdlari.RiskBasamakAzamiToplanabilir), Is.EqualTo(5000), "Öneri değer.");
        }

        [Test]
        [Property("GDD", "10.5.4, 10.5.5, 9.1.3")]
        public void Risk_olayinda_bolumden_cikmak_kayip_sayilir_ve_oyuncu_uyarilir()
        {
            var oyuncu = Oyuncu();
            var olay = RiskKur(oyuncu);
            int hak = olay.SansHakki;

            // Mevcut basamakta toplanabilir yokken çıkış hak götürmez; uyarı da gerekmez.
            Assert.That(oyuncu.Olaylar.CikisUyarisiGerekir(Simdi), Is.False);
            Oyna(oyuncu, kazan: false, cik: true);
            Assert.That(olay.SansHakki, Is.EqualTo(hak));

            Oyna(oyuncu, kazan: true);
            Assert.That(olay.BasamakPuani, Is.GreaterThan(0));
            Assert.That(oyuncu.Olaylar.CikisUyarisiGerekir(Simdi), Is.True);

            Oyna(oyuncu, kazan: false, cik: true);
            Assert.That(olay.SansHakki, Is.EqualTo(hak - 1));
            Assert.That(oyuncu.Olaylar.CikisUyarisiGerekir(Simdi + (Gun * 3)), Is.False, "Süresi dolan olay uyarı istemez.");
        }

        [Test]
        [Property("GDD", "10.5.7, 10.2.8")]
        public void Kurtarma_bolumundeki_kayip_sans_hakki_goturmez()
        {
            var oyuncu = Oyuncu();
            var olay = RiskKur(oyuncu);
            Oyna(oyuncu, kazan: true);
            int hak = olay.SansHakki;

            Oyna(oyuncu, kazan: false, kurtarma: true);
            Assert.That(olay.SansHakki, Is.EqualTo(hak));

            Oyna(oyuncu, kazan: false);
            Assert.That(olay.SansHakki, Is.EqualTo(hak - 1));
        }

        [Test]
        [Property("GDD", "10.5.6, 10.0.6")]
        public void Risk_olayi_butun_basamaklar_bitince_tamamlanir_sure_dolunca_kalan_toplanabilir_aktarilir()
        {
            var oyuncu = Oyuncu();
            var olay = RiskKur(oyuncu, aktarilir: true);
            int ilkHedef = olay.Hedefler[0];

            olay.BolumKazanildi(ilkHedef + 3);
            Assert.That(olay.Basamak, Is.EqualTo(1));
            Assert.That(olay.Tamamlandi, Is.False);

            var bitenler = oyuncu.Olaylar.TakvimiUygula(null, Simdi + (Gun * 3));
            Assert.That(bitenler.Single().Kimlik, Is.EqualTo("risk"));
            Assert.That(oyuncu.Olaylar.AktarilmayiBekleyen(OlayTuru.RiskBasamaklari), Is.EqualTo(3));
            Assert.That(oyuncu.Olaylar.AktarilmayiBekleyen(OlayTuru.BasamakliToplama), Is.EqualTo(0), "Aktarım aynı türün olayınadır.");

            // Bütün basamaklar biten olayda kayıp hiçbir şey yapmaz ve aktarılacak toplanabilir kalmaz.
            var tam = new RiskBasamaklariOlayi("r2", Simdi + Gun, P);
            tam.Gir();
            tam.BolumKazanildi(tam.Hedefler.Sum());
            Assert.That(tam.Tamamlandi, Is.True);
            tam.BolumKaybedildi();
            Assert.That(tam.SansHakki, Is.EqualTo(Tam(MetaParametreAdlari.RiskBasamakSansHakki)));
            Assert.That(tam.AktarilacakToplanabilir, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ Ödül paketi

        [Test]
        [Property("GDD", "10.9.5, 9.2.6")]
        public void Olay_esyalari_oyuncuda_birikir_ve_odul_paketi_kayitla_korunur()
        {
            var odul = new OdulPaketi { Para = 5, Can = 1, SinirsizCanDakika = 20, Yildiz = 2, CarpanDakika = 15 };
            odul.Takviyeler[TakviyeTuru.Bomba] = 2;
            odul.TakviyeSinirsizSaniye[TakviyeTuru.Cizgi] = 600;
            odul.OlayEsyalari["bilye"] = 5;
            odul.AlbumEsyalari["p"] = 1;
            odul.Kozmetikler.Add("isim_stili");

            var oyuncu = Oyuncu();
            var kayit = Kayit("bahce", hedefler: new[] { 1, 1000 });
            kayit.Oduller.Add(odul);
            oyuncu.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            Oyna(oyuncu, kazan: true);

            // Ödül içeriği kayıttan geçip aynı kalır.
            oyuncu = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            var okunan = Toplama(oyuncu, "bahce").Odul(0)!;
            Assert.That(okunan.Para, Is.EqualTo(5));
            Assert.That(okunan.SinirsizCanDakika, Is.EqualTo(20));
            Assert.That(okunan.CarpanDakika, Is.EqualTo(15));
            Assert.That(okunan.Takviyeler[TakviyeTuru.Bomba], Is.EqualTo(2));
            Assert.That(okunan.TakviyeSinirsizSaniye[TakviyeTuru.Cizgi], Is.EqualTo(600));
            Assert.That(okunan.AlbumEsyalari["p"], Is.EqualTo(1));
            Assert.That(okunan.Kozmetikler, Is.EqualTo(new[] { "isim_stili" }));

            // Bazı şablon olayları başka olaylara ödül besler: bilye, enerji.
            Assert.That(oyuncu.Olaylar.OduluAl("bahce", 0, Simdi), Is.True);
            oyuncu.OdulVer(odul, Simdi);
            Assert.That(oyuncu.OlayEsyalari["bilye"], Is.EqualTo(10));
            Assert.That(OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi).OlayEsyalari["bilye"], Is.EqualTo(10));
        }
    }
}
