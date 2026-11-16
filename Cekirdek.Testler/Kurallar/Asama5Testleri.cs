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
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>Yol haritası Aşama 5: hız yarışı (GDD 10.1) ve harita keşif (10.4) olayları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama5Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        private static TimeSpan Dk(double dakika) => TimeSpan.FromMinutes(dakika);

        private const string KazanilanBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

        /// <summary>Bir bölüm kazanır; 4 hamle kalır.</summary>
        private static BolumSonucu Kazan(Oyuncu oyuncu, DateTime simdi, ZorlukEtiketi etiket = ZorlukEtiketi.Normal)
        {
            var etiketler = new ZorlukEtiketTablosu();
            etiketler.Degistir(12, etiket);
            var akis = new BolumAkisi(oyuncu, etiketler, 12);
            var olaylar = new List<Olay>();
            var oturum = akis.Baslat(
                BolumYukleyici.JsondanOku(KazanilanBolum, P), AkBayraklari.Varsayilan, 7, simdi, Enumerable.Empty<TakviyeTuru>(), olaylar);
            Assert.That(oturum, Is.Not.Null);
            Assert.That(oturum!.Kaydir(H(1, 4), H(1, 3), olaylar), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            return akis.SonucuIsle(simdi);
        }

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

        // ------------------------------------------------------------------ 5.2 Hız yarışı

        private sealed class Yaris
        {
            public Yaris(double sureDk = 24 * 60, int enYuksekBolum = 500)
            {
                Saat = new AyarliSaat(Simdi);
                Sunucu = new YerelSahteSunucu(Saat);
                Oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = enYuksekBolum, Kimlik = "ben" };
                Kayit = new OlayTakvimKaydi("yaris", OlayTuru.HizYarisi, Simdi, Simdi + Dk(sureDk));
                Kayit.Oduller.Add(new OdulPaketi { Para = 200 });
                Kayit.Oduller.Add(new OdulPaketi { Para = 10 });
                Kayit.Oduller.Add(new OdulPaketi { Para = 20 });
                Kayit.Oduller.Add(new OdulPaketi { Para = 30 });
                Sunucu.OlayEkle(Kayit);
                Oyuncu.Olaylar.TakvimiUygula(Sunucu.OlayTakvimi().Deger, Simdi);
                HepsiniGoster(Oyuncu.Pencereler);
            }

            public AyarliSaat Saat { get; }
            public YerelSahteSunucu Sunucu { get; }
            public Oyuncu Oyuncu { get; set; }
            public OlayTakvimKaydi Kayit { get; }
            public HizYarisiOlayi Olay => (HizYarisiOlayi)Oyuncu.Olaylar.Bul("yaris")!;
            public DateTime An => Saat.SimdiUtc;

            public void Ilerle() => Oyuncu.Olaylar.Ilerle(Sunucu, An);

            /// <summary>Katılır, verilen sayıda zor bölüm kazanır ve aşamanın sonuna gelir.</summary>
            public void AsamayiOyna(int zorKazanim)
            {
                Assert.That(Olay.Katil(Sunucu, Oyuncu, An), Is.True);
                for (int i = 0; i < zorKazanim; i++)
                {
                    Kazan(Oyuncu, An, ZorlukEtiketi.Zor);
                }

                Ilerle();
                Saat.Ilerlet(Olay.AsamaSuresi);
                Ilerle();
            }
        }

        [Test]
        [Property("GDD", "10.1.1, 10.1.2, 10.1.3")]
        public void Katilinca_grup_atanir_oyuncuya_ozel_bitis_baslar_ve_sinirsiz_can_verilir()
        {
            var y = new Yaris();
            Assert.That(y.Oyuncu.Olaylar.Acik(Simdi).Single(), Is.SameAs(y.Olay));
            Assert.That(y.Oyuncu.Olaylar.BolumOncesi(Simdi), Does.Contain(y.Olay), "Bölüm öncesi ekranında da gösterilir.");
            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Hazir));
            Assert.That(new Yaris(enYuksekBolum: Tam(MetaParametreAdlari.AcilisHizYarisi) - 1).Oyuncu.Olaylar.Acik(Simdi), Is.Empty);

            Assert.That(y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi), Is.True);

            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Yarista));
            Assert.That(y.Olay.KatilimSayisi, Is.EqualTo(1));
            Assert.That(y.Olay.Liste, Has.Count.EqualTo(Tam(MetaParametreAdlari.HizYarisiGrupBoyutu)));
            Assert.That(y.Olay.OyuncuyaOzelBitis, Is.EqualTo(Simdi + y.Olay.AsamaSuresi));
            Assert.That(y.Oyuncu.Can.SinirsizKalan(Simdi), Is.EqualTo(y.Olay.AsamaSuresi), "Yarış süresince sınırsız can.");
            Assert.That(y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi), Is.False, "Yarıştayken yeniden katılınmaz.");
        }

        [Test]
        [Property("GDD", "10.1.2, 10.1.3")]
        public void Yeterli_sure_yoksa_ya_da_katilma_hatasinda_hazir_durumunda_kalinir()
        {
            double asgari = P.Sayi(MetaParametreAdlari.HizYarisiAsgariKalanDk);

            var gec = new Yaris(sureDk: asgari - 1);
            Assert.That(gec.Olay.Katil(gec.Sunucu, gec.Oyuncu, Simdi), Is.False);
            Assert.That(gec.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Hazir));

            var cevrimdisi = new Yaris();
            cevrimdisi.Sunucu.Cevrimdisi = true;
            Assert.That(cevrimdisi.Olay.Katil(cevrimdisi.Sunucu, cevrimdisi.Oyuncu, Simdi), Is.False);
            Assert.That(cevrimdisi.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Hazir));
            Assert.That(cevrimdisi.Olay.KatilimSayisi, Is.EqualTo(0), "Başarısız istek katılım hakkı harcamaz.");
            Assert.That(cevrimdisi.Oyuncu.Can.SinirsizAktif(Simdi), Is.False);

            // Olayın bitimine aşama süresinden az kalmışsa süre kalan zamana göre hesaplanır.
            var kisa = new Yaris(sureDk: asgari + 5);
            Assert.That(kisa.Olay.Katil(kisa.Sunucu, kisa.Oyuncu, Simdi), Is.True);
            Assert.That(kisa.Olay.OyuncuyaOzelBitis, Is.EqualTo(kisa.Olay.Bitis));
            Assert.That(kisa.Oyuncu.Can.SinirsizKalan(Simdi), Is.EqualTo(Dk(asgari + 5)));
        }

        [Test]
        [Property("GDD", "10.1.4, 10.1.6, 10.0.5")]
        public void Kazanilan_bolum_etikete_gore_puan_getirir_ve_sunucuya_gonderilir()
        {
            var y = new Yaris();
            y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi);
            y.Oyuncu.Carpan.BiletKullan(Simdi);

            Kazan(y.Oyuncu, Simdi);
            Assert.That(y.Olay.Puan, Is.EqualTo(Tam(MetaParametreAdlari.HizYarisiPuanNormal)));
            Kazan(y.Oyuncu, Simdi, ZorlukEtiketi.CokZor);
            int puan = Tam(MetaParametreAdlari.HizYarisiPuanNormal) + Tam(MetaParametreAdlari.HizYarisiPuanZor);
            Assert.That(y.Olay.Puan, Is.EqualTo(puan), "Zor ve üstü aynı puanı alır; çarpan uygulanmaz.");
            Assert.That(y.Olay.KalanHamleToplami, Is.EqualTo(8), "Kalan hamle bilgisi olaya iletilir.");

            // Bağlantı yokken puan bekler; bağlantı gelince bir kez gönderilir ve liste yenilenir.
            y.Sunucu.Cevrimdisi = true;
            y.Ilerle();
            y.Sunucu.Cevrimdisi = false;
            Assert.That(y.Sunucu.Siralama("yaris#1", "ben").Deger.Siralama.Single(u => !u.Bot).Puan, Is.EqualTo(0));
            y.Ilerle();
            y.Ilerle();
            Assert.That(y.Sunucu.Siralama("yaris#1", "ben").Deger.Siralama.Single(u => !u.Bot).Puan, Is.EqualTo(puan));
            Assert.That(y.Olay.Liste.Single(u => u.Kimlik == "ben").Puan, Is.EqualTo(puan));
            Assert.That(y.Olay.Sira("ben"), Is.EqualTo(1));

            // Oyuncunun süresi dolduktan sonra kazanılan bölüm puan getirmez.
            y.Saat.Ilerlet(y.Olay.AsamaSuresi);
            Kazan(y.Oyuncu, y.An);
            Assert.That(y.Olay.Puan, Is.EqualTo(puan));
        }

        [Test]
        [Property("GDD", "10.1.7")]
        public void Sure_dolunca_son_liste_alinir()
        {
            var y = new Yaris();
            y.AsamayiOyna(zorKazanim: 2);

            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.OdulHazir));
            Assert.That(y.Olay.SonListeSunucudan, Is.True);
            Assert.That(y.Olay.Liste.Select(u => u.Puan), Is.Ordered.Descending);
        }

        [Test]
        [Property("GDD", "10.1.7")]
        public void Son_liste_gelmezse_aralikli_denemelerden_sonra_yerel_siralama_kullanilir()
        {
            var y = new Yaris();
            y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi);
            Kazan(y.Oyuncu, Simdi, ZorlukEtiketi.Zor);
            y.Ilerle();
            y.Sunucu.Cevrimdisi = true;
            y.Saat.Ilerlet(y.Olay.AsamaSuresi);
            int deneme = Tam(MetaParametreAdlari.SonListeDenemeSayisi);
            var aralik = TimeSpan.FromSeconds(Tam(MetaParametreAdlari.SonListeDenemeAraligiSn));

            for (int i = 1; i < deneme; i++)
            {
                y.Ilerle();
                Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.SonListeBekleniyor), $"{i}. denemeden sonra.");
                y.Ilerle();
                Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.SonListeBekleniyor), "Aralık dolmadan yeniden denenmez.");
                y.Saat.Ilerlet(aralik);
            }

            y.Ilerle();
            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.OdulHazir), "Denemeler bitti.");
            Assert.That(y.Olay.SonListeSunucudan, Is.False);
            Assert.That(y.Olay.Liste.Single(u => u.Kimlik == "ben").Puan, Is.EqualTo(Tam(MetaParametreAdlari.HizYarisiPuanZor)));
            Assert.That(y.Olay.Sira("ben"), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        [Property("GDD", "10.1.5, 10.1.8")]
        public void Birinci_buyuk_odulu_alir_ve_katilim_hakki_kaldikca_yeni_asama_baslar()
        {
            var y = new Yaris();
            int para = y.Oyuncu.Cuzdan.Bakiye;
            int azami = y.Olay.AzamiKatilim;
            Assert.That(azami, Is.EqualTo(Tam(MetaParametreAdlari.HizYarisiAzamiKatilim)));

            // Botlar 20 dakikada en fazla 2 puan toplar: 4 zor bölüm kazanan birincidir.
            y.AsamayiOyna(zorKazanim: 4);
            Assert.That(y.Olay.Sira("ben"), Is.EqualTo(1));
            Assert.That(y.Olay.HazirOdul("ben")!.Para, Is.EqualTo(200));
            Assert.That(y.Olay.OduluAl(y.Oyuncu, y.An), Is.True);
            Assert.That(y.Oyuncu.Cuzdan.Bakiye - para, Is.EqualTo(200 + (4 * EtiketOdulleri.KazanmaParasi(P, ZorlukEtiketi.Zor, 4))));
            Assert.That(y.Olay.OduluAl(y.Oyuncu, y.An), Is.False);

            // Ödül alındıktan sonra yeni aşamaya başlama penceresi açılır.
            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Hazir));
            Assert.That(HepsiniGoster(y.Oyuncu.Pencereler), Is.EqualTo(new[] { "hiz_yarisi_asama:yaris:2" }));

            for (int katilim = 2; katilim <= azami; katilim++)
            {
                y.AsamayiOyna(zorKazanim: 4);
                Assert.That(y.Olay.KatilimSayisi, Is.EqualTo(katilim));
                Assert.That(y.Olay.Puan, Is.EqualTo(4 * Tam(MetaParametreAdlari.HizYarisiPuanZor)), "Her aşama sıfırdan başlar.");
                Assert.That(y.Olay.OduluAl(y.Oyuncu, y.An), Is.True);
            }

            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Bitti), "Katılım hakkı bitti.");
            Assert.That(y.Olay.Katil(y.Sunucu, y.Oyuncu, y.An), Is.False);
        }

        [Test]
        [Property("GDD", "10.1.8")]
        public void Birinci_olmayan_asama_odulunu_alir()
        {
            var y = new Yaris();
            y.Sunucu.BotAzamiSaatlikPuan = 600;
            int para = y.Oyuncu.Cuzdan.Bakiye;

            y.AsamayiOyna(zorKazanim: 0);

            Assert.That(y.Olay.Sira("ben"), Is.GreaterThan(1));
            Assert.That(y.Olay.OduluAl(y.Oyuncu, y.An), Is.True);
            Assert.That(y.Oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 10), "1. aşamanın ödülü.");
        }

        [Test]
        [Property("GDD", "10.0.8, 10.1.7")]
        public void Olay_biterken_suren_yaris_yerel_siralamayla_kapanir_ve_odulu_verilir()
        {
            var y = new Yaris(sureDk: 15);
            y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi);
            for (int i = 0; i < 4; i++) Kazan(y.Oyuncu, Simdi, ZorlukEtiketi.Zor);
            int para = y.Oyuncu.Cuzdan.Bakiye;

            var bitenler = y.Oyuncu.Olaylar.TakvimiUygula(null, Simdi + Dk(15));

            Assert.That(bitenler.Single().Oduller.Single().Para, Is.EqualTo(200));
            Assert.That(y.Oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 200));
            Assert.That(y.Oyuncu.Olaylar.Kurulu, Is.Empty);
        }

        [Test]
        [Property("GDD", "10.1")]
        public void Hiz_yarisi_kayitla_korunur_ve_sunucu_esitlemesiyle_ilerler()
        {
            var y = new Yaris();
            y.Olay.Katil(y.Sunucu, y.Oyuncu, Simdi);
            Kazan(y.Oyuncu, Simdi, ZorlukEtiketi.Zor);

            string kayit = OyuncuKaydi.Yaz(y.Oyuncu);
            y.Oyuncu = OyuncuKaydi.Yukle(kayit, P, Simdi);
            Assert.That(OyuncuKaydi.Yaz(y.Oyuncu), Is.EqualTo(kayit));
            Assert.That(y.Oyuncu.Kimlik, Is.EqualTo("ben"));
            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.Yarista));
            Assert.That(y.Olay.Puan, Is.EqualTo(Tam(MetaParametreAdlari.HizYarisiPuanZor)));
            Assert.That(y.Olay.OyuncuyaOzelBitis, Is.EqualTo(Simdi + y.Olay.AsamaSuresi));

            // Bekleyen puan kayıttan sonra gönderilir; süre dolunca eşitleme son listeyi alır.
            y.Saat.Ilerlet(y.Olay.AsamaSuresi);
            Assert.That(SunucuEsitleyici.Uygula(y.Sunucu, y.Oyuncu, new ZorlukEtiketTablosu()), Is.True);
            Assert.That(y.Olay.Durum, Is.EqualTo(HizYarisiDurumu.OdulHazir));
            Assert.That(y.Sunucu.Siralama("yaris#1", "ben").Deger.Siralama.Single(u => !u.Bot).Puan, Is.EqualTo(y.Olay.Puan));
            Assert.That(y.Olay.HazirOdul("ben"), Is.Not.Null);
        }

        // ------------------------------------------------------------------ 5.3 Harita keşif

        private static readonly DateTime KesifBitisi = Simdi + TimeSpan.FromDays(7);

        /// <summary>
        /// Harita: a(20) → b(30); büyük görev "kopru" = k1(10) + k2(10); d(40), köprüden sonra. Beş yaprak görev,
        /// toplam 110 enerji.
        /// </summary>
        private static HaritaKesifYapilandirmasi Harita()
        {
            var y = new HaritaKesifYapilandirmasi();
            var a = new HaritaGorevi("a", 20) { Paket = new OdulPaketi { Para = 15 } };
            a.Parcalar["orman"] = ParcaDurumu.Gorunur;
            var b = new HaritaGorevi("b", 30, "a") { Karakter = 1, Paket = new OdulPaketi() };
            b.Paket.OlayEsyalari[HaritaKesifOlayi.EnerjiEsyasi] = 25;
            b.Parcalar["orman"] = ParcaDurumu.YariAcik;
            b.Parcalar["gol"] = ParcaDurumu.Acik;
            y.Gorevler.AddRange(new[]
            {
                a, b,
                new HaritaGorevi("kopru", 0),
                new HaritaGorevi("k1", 10) { UstGorev = "kopru" },
                new HaritaGorevi("k2", 10) { UstGorev = "kopru" },
                new HaritaGorevi("d", 40, "kopru"),
            });
            for (int i = 0; i < 5; i++) y.SandikOdulleri.Add(new OdulPaketi { Para = 100 });
            for (int i = 0; i < 12; i++)
            {
                y.HatOdulleri.Add(new Dictionary<KesifOdulu, OdulPaketi>
                {
                    { KesifOdulu.Ucretsiz, new OdulPaketi { Para = 1 } },
                    { KesifOdulu.Dusuk, new OdulPaketi { Para = 10 } },
                    { KesifOdulu.Yuksek, new OdulPaketi { Para = 100 } },
                    { KesifOdulu.YuksekEk, new OdulPaketi { Para = 1000 } },
                });
            }

            foreach (string segment in new[] { "pahali", "orta", "uygun" })
            {
                y.GunlukTeklifler.Add(new MagazaUrunu("teklif_" + segment, new OdulPaketi { Para = 500 }));
            }

            return y;
        }

        private static (Oyuncu Oyuncu, OlayTakvimKaydi Kayit) KesifKur(bool basla = true, int enerji = 0)
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 500 };
            var kayit = new OlayTakvimKaydi("kesif", OlayTuru.HaritaKesif, Simdi, KesifBitisi) { HaritaKesif = Harita() };
            oyuncu.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            HepsiniGoster(oyuncu.Pencereler);
            if (basla)
            {
                Assert.That(Kesif(oyuncu).Basla(Simdi), Is.True);
            }

            Kesif(oyuncu).EnerjiEkle(enerji);
            return (oyuncu, kayit);
        }

        private static HaritaKesifOlayi Kesif(Oyuncu oyuncu) => (HaritaKesifOlayi)oyuncu.Olaylar.Bul("kesif")!;

        [Test]
        [Property("GDD", "10.4.1, 10.4.9")]
        public void Harita_kesif_baslangic_penceresinden_sonra_aktif_olur_ve_bitime_yakin_son_sansa_girer()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisHaritaKesif);
            var erken = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis - 1 };
            erken.Olaylar.TakvimiUygula(new[] { new OlayTakvimKaydi("kesif", OlayTuru.HaritaKesif, Simdi, KesifBitisi) }, Simdi);
            Assert.That(erken.Olaylar.Acik(Simdi), Is.Empty);

            var (oyuncu, _) = KesifKur(basla: false);
            var olay = Kesif(oyuncu);
            Assert.That(olay.Durum(Simdi), Is.EqualTo(HaritaKesifDurumu.Hazir));

            // Başlamadan kazanım enerji getirmez.
            Kazan(oyuncu, Simdi);
            Assert.That(olay.Enerji, Is.EqualTo(0));

            Assert.That(olay.Basla(Simdi), Is.True);
            Assert.That(olay.Basla(Simdi), Is.False);
            Assert.That(olay.Durum(Simdi), Is.EqualTo(HaritaKesifDurumu.Aktif));

            var sonGun = KesifBitisi - TimeSpan.FromHours(23);
            oyuncu.Olaylar.Ilerle(new YerelSahteSunucu(new AyarliSaat(sonGun)), sonGun - TimeSpan.FromHours(3));
            Assert.That(oyuncu.Pencereler.Bekleyen, Is.EqualTo(0));
            Assert.That(olay.Durum(sonGun), Is.EqualTo(HaritaKesifDurumu.SonSans));
            oyuncu.Olaylar.Ilerle(new YerelSahteSunucu(new AyarliSaat(sonGun)), sonGun);
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Is.EqualTo(new[] { "harita_kesif_son_sans:kesif" }));
            Assert.That(olay.Durum(KesifBitisi), Is.EqualTo(HaritaKesifDurumu.Bitti));

            // Bitime yeterli süre kalmamışsa olay başlatılamaz.
            var (gec, _) = KesifKur(basla: false);
            Assert.That(Kesif(gec).Basla(KesifBitisi - Dk(Tam(MetaParametreAdlari.IkinciOlayAsgariSureDk) - 1)), Is.False);
        }

        [Test]
        [Property("GDD", "10.4.2, 10.4.8")]
        public void Bolum_kazanimi_ve_oduller_enerji_getirir_enerjinin_ust_siniri_vardir()
        {
            var (oyuncu, _) = KesifKur();
            var olay = Kesif(oyuncu);
            int azami = Tam(MetaParametreAdlari.HaritaKesifAzamiEnerji);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);

            oyuncu.Carpan.BiletKullan(Simdi);
            Kazan(oyuncu, Simdi, ZorlukEtiketi.Zor);
            Assert.That(olay.Enerji, Is.EqualTo(EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Zor) * kat), "Zorluk tabanı × çarpan.");

            // Enerji ödül olarak da gelir.
            int once = olay.Enerji;
            var odul = new OdulPaketi();
            odul.OlayEsyalari[HaritaKesifOlayi.EnerjiEsyasi] = 30;
            oyuncu.OdulVer(odul, Simdi);
            oyuncu.Olaylar.Ilerle(new YerelSahteSunucu(new AyarliSaat(Simdi)), Simdi);
            Assert.That(olay.Enerji, Is.EqualTo(once + 30));
            Assert.That(oyuncu.OlayEsyalari[HaritaKesifOlayi.EnerjiEsyasi], Is.EqualTo(0));

            // Üst sınırı aşan kısım eklenmez.
            Assert.That(olay.EnerjiEkle(azami), Is.EqualTo(azami - once - 30));
            Assert.That(olay.Enerji, Is.EqualTo(azami));
            Kazan(oyuncu, Simdi);
            Assert.That(olay.Enerji, Is.EqualTo(azami));
        }

        [Test]
        [Property("GDD", "10.4.3, 10.4.4")]
        public void Gorevler_enerjiyle_tamamlanir_haritayi_acar_ve_paketler_toplanir()
        {
            var (oyuncu, _) = KesifKur(enerji: 75);
            var olay = Kesif(oyuncu);
            int para = oyuncu.Cuzdan.Bakiye;

            Assert.That(olay.GorevYap("b", Simdi), Is.False, "Önkoşulu bitmedi.");
            Assert.That(olay.GorevYap("kopru", Simdi), Is.False, "Büyük görev alt görevleriyle tamamlanır.");
            Assert.That(olay.Parca("orman"), Is.EqualTo(ParcaDurumu.Kapali));

            Assert.That(olay.GorevYap("a", Simdi), Is.True);
            Assert.That(olay.GorevYap("a", Simdi), Is.False);
            Assert.That(olay.Enerji, Is.EqualTo(55));
            Assert.That(olay.HarcananEnerji, Is.EqualTo(20), "Harcanan enerji ayrıca tutulur.");
            Assert.That(olay.Parca("orman"), Is.EqualTo(ParcaDurumu.Gorunur));

            Assert.That(olay.GorevYap("b", Simdi), Is.True);
            Assert.That(olay.Parca("orman"), Is.EqualTo(ParcaDurumu.YariAcik));
            Assert.That(olay.Parca("gol"), Is.EqualTo(ParcaDurumu.Acik));

            // Para ve enerji paketleri haritadan toplanır.
            Assert.That(olay.PaketTopla("k1", oyuncu, Simdi), Is.False);
            Assert.That(olay.PaketTopla("a", oyuncu, Simdi), Is.True);
            Assert.That(olay.PaketTopla("a", oyuncu, Simdi), Is.False);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 15));
            Assert.That(olay.PaketTopla("b", oyuncu, Simdi), Is.True);
            Assert.That(olay.Enerji, Is.EqualTo(25 + 25));

            // Bütün alt görevleri biten büyük görev tamamlanır ve ardındaki görevi açar.
            Assert.That(olay.GorevYap("d", Simdi), Is.False);
            Assert.That(olay.GorevYap("k1", Simdi), Is.True);
            Assert.That(olay.Tamamlandi("kopru"), Is.False);
            Assert.That(olay.GorevYap("k2", Simdi), Is.True);
            Assert.That(olay.Tamamlandi("kopru"), Is.True);
            Assert.That(olay.Enerji, Is.EqualTo(30));
            Assert.That(olay.GorevYap("d", Simdi), Is.False, "Enerji yetmiyor.");
            olay.EnerjiEkle(10);
            Assert.That(olay.GorevYap("d", Simdi), Is.True);
            Assert.That(olay.Ilerleme, Is.EqualTo(1));
            Assert.That(olay.Gorevler.Single(g => g.Kimlik == "b").Karakter, Is.EqualTo(1), "Haritada iki karakter ilerler.");
        }

        [Test]
        [Property("GDD", "10.4.5")]
        public void Ilerleme_esikleri_sandik_acar()
        {
            var (oyuncu, _) = KesifKur(enerji: 200);
            var olay = Kesif(oyuncu);
            int para = oyuncu.Cuzdan.Bakiye;
            Assert.That(olay.SandikEsikleri, Is.EqualTo(new[] { 0.2, 0.4, 0.6, 0.8, 1.0 }), "Öneri değerler.");

            Assert.That(olay.SandikAc(0, oyuncu, Simdi), Is.False);
            olay.GorevYap("a", Simdi);
            Assert.That(olay.Ilerleme, Is.EqualTo(0.2));
            Assert.That(olay.SandikAc(0, oyuncu, Simdi), Is.True);
            Assert.That(olay.SandikAc(0, oyuncu, Simdi), Is.False, "Sandık bir kez açılır.");
            Assert.That(olay.SandikAc(1, oyuncu, Simdi), Is.False);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 100));

            foreach (string gorev in new[] { "b", "k1", "k2", "d" }) olay.GorevYap(gorev, Simdi);
            Assert.That(Enumerable.Range(1, 4).Select(s => olay.SandikAc(s, oyuncu, Simdi)), Is.All.True);
            Assert.That(olay.SandikAc(5, oyuncu, Simdi), Is.False);
        }

        [Test]
        [Property("GDD", "10.4.6")]
        public void Odul_hattinin_uc_hatti_ve_her_basamakta_dort_odulu_vardir()
        {
            var (oyuncu, _) = KesifKur(enerji: 200);
            var olay = Kesif(oyuncu);
            Assert.That(olay.HatBasamakSayisi, Is.EqualTo(Tam(MetaParametreAdlari.HaritaKesifHatBasamak)));
            Assert.That(olay.HatHedefi(olay.HatBasamakSayisi - 1), Is.EqualTo(110), "Harita bitince hat da biter.");
            Assert.That(olay.HatBasamagi, Is.EqualTo(0));

            // 20 enerji harcanınca ilk iki basamak (10 ve 19) geçilir.
            olay.GorevYap("a", Simdi);
            Assert.That(olay.HatBasamagi, Is.EqualTo(2));
            int para = oyuncu.Cuzdan.Bakiye;

            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Ucretsiz, oyuncu, Simdi), Is.True);
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Ucretsiz, oyuncu, Simdi), Is.False);
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Dusuk, oyuncu, Simdi), Is.False, "Düşük hat alınmadı.");
            Assert.That(olay.HatOdulunuAl(2, KesifOdulu.Ucretsiz, oyuncu, Simdi), Is.False, "Basamak geçilmedi.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 1));

            // Hat sonradan alınınca geçilmiş basamakların ödülleri de alınabilir.
            Assert.That(olay.HatSatinAl(KesifHatti.Dusuk, Simdi), Is.True);
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Dusuk, oyuncu, Simdi), Is.True);
            Assert.That(olay.HatOdulunuAl(1, KesifOdulu.Dusuk, oyuncu, Simdi), Is.True);
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Yuksek, oyuncu, Simdi), Is.False);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 21));

            Assert.That(olay.HatSatinAl(KesifHatti.Yuksek, Simdi), Is.True);
            Assert.That(olay.HatSatinAl(KesifHatti.Dusuk, Simdi), Is.False, "Hat yalnızca yükseltilir.");
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.Yuksek, oyuncu, Simdi), Is.True);
            Assert.That(olay.HatOdulunuAl(0, KesifOdulu.YuksekEk, oyuncu, Simdi), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 21 + 1100));
        }

        [Test]
        [Property("GDD", "10.4.7")]
        public void Gunluk_teklif_segmente_gore_secilir_ve_alinmazsa_ertesi_gun_daha_uygunu_onerilir()
        {
            var (oyuncu, _) = KesifKur();
            var olay = Kesif(oyuncu);
            long gun = oyuncu.GunlukOdul.GunNumarasi(Simdi);
            int para = oyuncu.Cuzdan.Bakiye;

            Assert.That(olay.GunlukTeklif(gun, Simdi)!.Kimlik, Is.EqualTo("teklif_pahali"));
            Assert.That(olay.GunlukTeklif(gun, Simdi)!.Kimlik, Is.EqualTo("teklif_pahali"), "Aynı gün aynı teklif.");

            // Alınmadı: ertesi gün bir sonraki segment.
            Assert.That(olay.GunlukTeklif(gun + 1, Simdi)!.Kimlik, Is.EqualTo("teklif_orta"));
            Assert.That(olay.GunlukTeklifAlindi(gun + 1, oyuncu, Simdi), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 500));
            Assert.That(olay.GunlukTeklif(gun + 1, Simdi), Is.Null, "Alınan teklif o gün yeniden sunulmaz.");
            Assert.That(olay.GunlukTeklifAlindi(gun + 1, oyuncu, Simdi), Is.False);

            // Alındı: segment değişmez. Sonra iki gün alınmaz: en uygun segmentte kalır.
            Assert.That(olay.GunlukTeklif(gun + 2, Simdi)!.Kimlik, Is.EqualTo("teklif_orta"));
            Assert.That(olay.GunlukTeklif(gun + 3, Simdi)!.Kimlik, Is.EqualTo("teklif_uygun"));
            Assert.That(olay.GunlukTeklif(gun + 4, Simdi)!.Kimlik, Is.EqualTo("teklif_uygun"));
            Assert.That(olay.GunlukTeklif(gun + 5, KesifBitisi), Is.Null, "Olay bitti.");
        }

        [Test]
        [Property("GDD", "10.4.9, 10.0.8")]
        public void Harita_kesif_bitince_alinmamis_oduller_verilir()
        {
            var (oyuncu, _) = KesifKur(enerji: 200);
            var olay = Kesif(oyuncu);
            olay.GorevYap("a", Simdi);
            olay.GorevYap("b", Simdi);
            olay.HatSatinAl(KesifHatti.Dusuk, Simdi);
            olay.SandikAc(0, oyuncu, Simdi);
            int para = oyuncu.Cuzdan.Bakiye;
            int basamak = olay.HatBasamagi;
            Assert.That(basamak, Is.EqualTo(5), "50 enerji harcandı: 10, 19, 28, 37, 46.");

            var bitenler = oyuncu.Olaylar.TakvimiUygula(null, KesifBitisi);

            // Sandık 2 (ilerleme 0.4), beş basamağın ücretsiz ve düşük ödülleri, a'nın para paketi ve b'nin enerji paketi.
            Assert.That(bitenler.Single().Oduller, Has.Count.EqualTo(1 + (basamak * 2) + 2));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + 100 + (basamak * 11) + 15));
            Assert.That(HepsiniGoster(oyuncu.Pencereler), Does.Contain("olay_bitti:kesif"));
        }

        [Test]
        [Property("GDD", "10.4")]
        public void Harita_kesif_kayitla_korunur_ve_ayni_yapilandirma_ilerlemeyi_bozmaz()
        {
            var (oyuncu, kayit) = KesifKur(enerji: 100);
            var olay = Kesif(oyuncu);
            olay.GorevYap("a", Simdi);
            olay.PaketTopla("a", oyuncu, Simdi);
            olay.SandikAc(0, oyuncu, Simdi);
            olay.HatSatinAl(KesifHatti.Yuksek, Simdi);
            olay.HatOdulunuAl(0, KesifOdulu.YuksekEk, oyuncu, Simdi);
            long gun = oyuncu.GunlukOdul.GunNumarasi(Simdi);
            olay.GunlukTeklif(gun, Simdi);

            // Aynı sürümlü yapılandırma yeniden uygulanınca ilerleme kalır.
            oyuncu.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            Assert.That(olay.Tamamlandi("a"), Is.True);

            string metin = OyuncuKaydi.Yaz(oyuncu);
            var yuklenen = OyuncuKaydi.Yukle(metin, P, Simdi);
            Assert.That(OyuncuKaydi.Yaz(yuklenen), Is.EqualTo(metin));

            var y = Kesif(yuklenen);
            Assert.That(y.Durum(Simdi), Is.EqualTo(HaritaKesifDurumu.Aktif));
            Assert.That(y.Enerji, Is.EqualTo(80));
            Assert.That(y.HarcananEnerji, Is.EqualTo(20));
            Assert.That(y.Hat, Is.EqualTo(KesifHatti.Yuksek));
            Assert.That(y.Tamamlandi("a"), Is.True);
            Assert.That(y.Parca("orman"), Is.EqualTo(ParcaDurumu.Gorunur));
            Assert.That(y.PaketToplanabilir("a"), Is.False);
            Assert.That(y.SandikAcilabilir(0), Is.False);
            Assert.That(y.HatOduluAlinabilir(0, KesifOdulu.YuksekEk), Is.False);
            Assert.That(y.HatOduluAlinabilir(0, KesifOdulu.Yuksek), Is.True);
            Assert.That(y.GunlukTeklif(gun + 1, Simdi)!.Kimlik, Is.EqualTo("teklif_orta"), "Teklif günü ve segment korunur.");

            // Harita kayıttan geri gelir: görevler sürdürülür, ödül içerikleri yerindedir.
            int para = yuklenen.Cuzdan.Bakiye;
            Assert.That(y.GorevYap("b", Simdi), Is.True);
            Assert.That(y.SandikAc(1, yuklenen, Simdi), Is.True);
            Assert.That(yuklenen.Cuzdan.Bakiye, Is.EqualTo(para + 100));
        }
    }
}
