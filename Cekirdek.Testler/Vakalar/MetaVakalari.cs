using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Vakalar
{
    /// <summary>test_vakalari.md — G. Meta testleri. Zaman ölçen testler sahte saatle çalışır.</summary>
    [TestFixture]
    public class MetaVakalari
    {
        private static readonly DateTime Baslangic = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        private static TimeSpan DolumSuresi => TimeSpan.FromMinutes(P.Sayi(MetaParametreAdlari.CanDolumSuresiDk));

        /// <summary>
        /// İlk hamlede kazanılan küçük bölüm: (1,4)→(1,3) üç r0'ı eşler. Hamle 5, hedef 3 r0. Alt satırlar,
        /// seri takviyesi gibi bölüm başında tahtaya konan taşlara yer bırakır; başka olası hamle içermez.
        /// </summary>
        private const string KazanilanBolumJson = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

        /// <summary>Kazanılamayan bölüm: tek hamle, ulaşılamaz hedef.</summary>
        private const string KaybedilenBolumJson = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 1,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 99 }] }";

        internal static BolumAkisi BolumuOyna(
            Oyuncu oyuncu, ZorlukEtiketTablosu etiketler, int bolum, bool kazan, DateTime simdi, out BolumSonucu sonuc)
        {
            var veri = BolumYukleyici.JsondanOku(kazan ? KazanilanBolumJson : KaybedilenBolumJson, P);
            var akis = new BolumAkisi(oyuncu, etiketler, bolum);
            var olaylar = new List<Olay>();
            var oturum = akis.Baslat(veri, AkBayraklari.Varsayilan, 7, simdi, Enumerable.Empty<TakviyeTuru>(), olaylar);
            Assert.That(oturum, Is.Not.Null, "Bölüme girmek için can gerekir.");

            var tahta = oturum!.Baglam.Tahta;
            string dokum = string.Join(" ", tahta.TumKonumlar().Select(k => tahta[k].Tas == null ? "_" : tahta[k].Tas!.Tur + ":" + tahta[k].Tas!.Renk));
            Assert.That(oturum.Kaydir(H(1, 4), H(1, 3), olaylar), Is.EqualTo(Hamle.HamleSonucu.Gecerli), dokum + " seri " + oyuncu.KazanmaSerisi);
            if (kazan)
            {
                Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
                sonuc = akis.SonucuIsle(simdi);
                return akis;
            }

            if (oturum.Durum == BolumDurumu.EkHamleTeklifi)
            {
                oturum.EkHamleTeklifiniReddet(olaylar);
                Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Kaybedildi));
                sonuc = akis.SonucuIsle(simdi);
                return akis;
            }

            // Kazanma serisi yedek hamle hakkını açtıysa hamle bitince teklif yerine hak kullanılır (AK-02 A) ve
            // bölüm sürer. Oyuncu bölümden çıkar: başlamış bir bölümden çıkmak da kayıp sayılır (GDD 9.1.3).
            Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            sonuc = akis.SonucuIsle(simdi, cikildi: true);
            return akis;
        }

        private static Oyuncu IleriOyuncu(int enYuksekBolum = 500) =>
            new Oyuncu(P, Baslangic) { EnYuksekBolum = enYuksekBolum };

        [Test]
        [Category("MT01")]
        [Property("GDD", "9.1.1, 9.1.3, 9.1.4, 9.1.5")]
        public void MT01_Bolum_kaybi_bir_can_dusurur_ve_dolum_sayacini_baslatir()
        {
            var oyuncu = IleriOyuncu();
            int azami = Tam(MetaParametreAdlari.AzamiCan);
            Assert.That(oyuncu.Can.Mevcut(Baslangic), Is.EqualTo(azami));
            Assert.That(oyuncu.Can.SinirsizAktif(Baslangic), Is.False);

            BolumuOyna(oyuncu, new ZorlukEtiketTablosu(), 10, kazan: false, Baslangic, out var sonuc);

            Assert.That(sonuc.Kazanildi, Is.False);
            Assert.That(sonuc.CanDustu, Is.True);
            Assert.That(oyuncu.Can.Mevcut(Baslangic), Is.EqualTo(azami - 1));
            Assert.That(oyuncu.Can.SonrakiCanaKalan(Baslangic), Is.EqualTo(DolumSuresi));
            Assert.That(oyuncu.Can.TamDolacagiAn, Is.EqualTo(Baslangic + DolumSuresi));
        }

        [Test]
        [Category("MT02")]
        [Property("GDD", "9.1.1, 9.1.4, 9.1.5")]
        public void MT02_Canlar_dolum_suresinde_bir_dolar_ve_tavanda_durur()
        {
            int azami = Tam(MetaParametreAdlari.AzamiCan);
            var can = new CanSistemi(P, Baslangic);
            can.CanAyarla(azami - 3, Baslangic);
            Assert.That(can.Mevcut(Baslangic), Is.EqualTo(azami - 3));

            // Saat ileri alınır; uygulama kapalıyken geçen süre de sayılır, çünkü saklanan tek değer dolacağı andır.
            Assert.That(can.Mevcut(Baslangic + DolumSuresi - TimeSpan.FromSeconds(1)), Is.EqualTo(azami - 3));
            Assert.That(can.Mevcut(Baslangic + DolumSuresi), Is.EqualTo(azami - 2));
            Assert.That(can.Mevcut(Baslangic + (3 * DolumSuresi)), Is.EqualTo(azami));
            Assert.That(can.Mevcut(Baslangic + (4 * DolumSuresi)), Is.EqualTo(azami), "Tavan aşılmaz.");
            Assert.That(can.SonrakiCanaKalan(Baslangic + (4 * DolumSuresi)), Is.Null, "Sayaç durur.");
        }

        [Test]
        [Category("MT03")]
        [Property("GDD", "9.1.1, 9.1.4, 9.8.9")]
        public void MT03_Premium_hat_sahibinin_azami_cani_yukselir()
        {
            int azami = Tam(MetaParametreAdlari.AzamiCan);
            int premium = Tam(MetaParametreAdlari.AzamiCanPremium);
            var oyuncu = IleriOyuncu();
            Assert.That(oyuncu.Can.Mevcut(Baslangic), Is.EqualTo(azami));

            oyuncu.SezonPremiumSatinAl(Baslangic);

            Assert.That(oyuncu.Can.Azami, Is.EqualTo(premium));
            Assert.That(oyuncu.Can.Mevcut(Baslangic), Is.EqualTo(azami), "Mevcut can korunur.");

            // Zamanla dolum premium tavana kadar sürer (öneri değerle 80 dk).
            var sure = TimeSpan.FromTicks(DolumSuresi.Ticks * (premium - azami));
            Assert.That(oyuncu.Can.Mevcut(Baslangic + sure - TimeSpan.FromSeconds(1)), Is.EqualTo(premium - 1));
            Assert.That(oyuncu.Can.Mevcut(Baslangic + sure), Is.EqualTo(premium));
            Assert.That(oyuncu.Can.Mevcut(Baslangic + sure + DolumSuresi), Is.EqualTo(premium));
        }

        [Test]
        [Category("MT04")]
        [Property("GDD", "9.3.7, 9.2.5")]
        public void MT04_Yetersiz_bakiyede_satin_alma_basarisiz_olur()
        {
            var oyuncu = IleriOyuncu();
            int fiyat = Tam(MetaParametreAdlari.FiyatCizgi);
            oyuncu.Cuzdan.SunucuylaEsitle(fiyat - 1);
            int onerilen = 0;
            oyuncu.Cuzdan.ParaMagazasiOnerildi += eksik => onerilen = eksik;

            Assert.That(oyuncu.Magaza.SatinAl(TakviyeTuru.Cizgi), Is.False);

            Assert.That(onerilen, Is.EqualTo(1), "Para mağazası önerilir.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(fiyat - 1));
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Cizgi), Is.EqualTo(0));
            Assert.That(oyuncu.Cuzdan.Harcamalar, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("MT05")]
        [Property("GDD", "9.3.7, 9.2.5, 9.3.11")]
        public void MT05_Takviye_satin_alma_para_duser_ve_envantere_ekler(bool yuksekDuzey)
        {
            var oyuncu = IleriOyuncu();
            oyuncu.Cuzdan.SunucuylaEsitle(6000);
            oyuncu.Magaza.YuksekFiyatDuzeyi = yuksekDuzey;
            int beklenenFiyat = (int)(Tam(MetaParametreAdlari.FiyatSatir)
                * (yuksekDuzey ? P.Sayi(MetaParametreAdlari.YuksekFiyatCarpani) : 1));

            Assert.That(oyuncu.Magaza.SatinAl(TakviyeTuru.Satir), Is.True);

            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(6000 - beklenenFiyat));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(yuksekDuzey ? 1500 : 3000), "Öneri değerle 3000 ve 1500.");
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Satir), Is.EqualTo(1));
            var kayit = oyuncu.Cuzdan.Harcamalar.Single();
            Assert.That(kayit.Miktar, Is.EqualTo(beklenenFiyat));
            Assert.That(kayit.Ad, Does.Contain("Satir"));
        }

        [Test]
        [Category("MT06")]
        [Property("GDD", "9.2.2, 7.6.1, 8.8.1, 8.8.2")]
        public void MT06_Bolum_kazanma_para_odulu_zorluga_ve_kalan_hamleye_bagli()
        {
            // Etiket bölüm dosyasından ayrı tablodan okunur; sunucu etiketi değiştirebilir (8.8.1a).
            var etiketler = ZorlukEtiketTablosu.JsondanOku(ProjeDosyalari.Oku("veri", "zorluk_etiketleri.json"));
            Assert.That(etiketler.Etiket(1), Is.EqualTo(ZorlukEtiketi.Normal));
            Assert.That(etiketler.Etiket(3), Is.EqualTo(ZorlukEtiketi.Zor));
            Assert.That(etiketler.Etiket(9999), Is.EqualTo(ZorlukEtiketi.Normal), "Tabloda olmayan bölüm normal sayılır.");

            // Bölüm 4 hamle kalmışken kazanılır: toplam = taban + kalan_hamle_basina_para × 4.
            var oyuncu = IleriOyuncu();
            int once = oyuncu.Cuzdan.Bakiye;
            var akis = BolumuOyna(oyuncu, etiketler, 1, kazan: true, Baslangic, out var sonuc);

            Assert.That(akis.Oturum!.KalanHamle, Is.EqualTo(4));
            Assert.That(sonuc.Para, Is.EqualTo(
                Tam(MetaParametreAdlari.KazanmaParasiNormal) + (4 * Tam(MetaParametreAdlari.KalanHamleBasinaPara))));
            Assert.That(sonuc.Para, Is.EqualTo(28), "Öneri değerle 20 + 4 × 2.");
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(once + 28));

            var tabanlar = new Dictionary<ZorlukEtiketi, string>
            {
                { ZorlukEtiketi.Normal, MetaParametreAdlari.KazanmaParasiNormal },
                { ZorlukEtiketi.Zor, MetaParametreAdlari.KazanmaParasiZor },
                { ZorlukEtiketi.CokZor, MetaParametreAdlari.KazanmaParasiCokZor },
                { ZorlukEtiketi.AsiriZor, MetaParametreAdlari.KazanmaParasiAsiriZor },
            };
            foreach (var cift in tabanlar)
            {
                Assert.That(
                    EtiketOdulleri.KazanmaParasi(P, cift.Key, 4),
                    Is.EqualTo(Tam(cift.Value) + (4 * Tam(MetaParametreAdlari.KalanHamleBasinaPara))));
            }

            // Sunucu etiketi değiştirince ödül de değişir.
            etiketler.Degistir(1, ZorlukEtiketi.AsiriZor);
            Assert.That(etiketler.Etiket(1), Is.EqualTo(ZorlukEtiketi.AsiriZor));
        }

        [Test]
        [Category("MT06")]
        [Property("GDD", "9.2.2")]
        [Property("Guven", "düşük (ek beklenti)")]
        public void MT06_Ek_beklenti_kesirli_toplam_asagi_yuvarlanir()
        {
            var p = P.Degistir(MetaParametreAdlari.KalanHamleBasinaPara, 1.6);

            Assert.That(EtiketOdulleri.KazanmaParasi(p, ZorlukEtiketi.Normal, 3), Is.EqualTo(24), "20 + 4.8 → 24.");
        }

        [Test]
        [Category("MT07")]
        [Property("GDD", "9.3.5, 9.4.1")]
        public void MT07_Baslangic_takviyesi_ve_oyun_ici_arac_acilis_kapilari()
        {
            var kapilar = new IlerlemeKapilari(P);
            var acilislar = new (TakviyeTuru Tur, string Parametre)[]
            {
                (TakviyeTuru.Cizgi, MetaParametreAdlari.AcilisBaslangicTakviyeleri),
                (TakviyeTuru.Bomba, MetaParametreAdlari.AcilisBaslangicTakviyeleri),
                (TakviyeTuru.RenkKuresi, MetaParametreAdlari.AcilisBaslangicTakviyeleri),
                (TakviyeTuru.TekHucre, MetaParametreAdlari.AcilisTekHucreAraci),
                (TakviyeTuru.Satir, MetaParametreAdlari.AcilisSatirAraci),
                (TakviyeTuru.Sutun, MetaParametreAdlari.AcilisSutunAraci),
                (TakviyeTuru.Karistirma, MetaParametreAdlari.AcilisKaristirmaAraci),
            };

            // En yüksek bölümü ile bulunduğu bölümü aynı olan oyuncu; her kapının bir önceki bölümünde kilitlidir.
            foreach (var (tur, parametre) in acilislar)
            {
                int acilis = Tam(parametre);
                Assert.That(kapilar.TakviyeAcik(tur, acilis - 1), Is.False, $"{tur}: bölüm {acilis - 1}");
                Assert.That(kapilar.TakviyeAcik(tur, acilis), Is.True, $"{tur}: bölüm {acilis}");
            }
        }

        [Test]
        [Category("MT08")]
        [Property("GDD", "9.5.1, 9.5.2, 9.5.4, 9.5.5, 9.4")]
        public void MT08_Gunluk_odulde_gun_siniri_sunucu_saatinde_sabit_bir_saattir()
        {
            var kapilar = new IlerlemeKapilari(P);
            var odul = new GunlukOdul(P, kapilar);
            int acilis = Tam(MetaParametreAdlari.AcilisGunlukOdul);
            var sinir = TimeSpan.ParseExact(P.Metin(MetaParametreAdlari.GunSiniriSaati), @"hh\:mm", null);
            var sinirAni = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc) + sinir;
            var sinirdanOnce = sinirAni - TimeSpan.FromMinutes(1);

            // 1. Ödül alınır; seri 1 artar.
            Assert.That(odul.Alinabilir(sinirdanOnce, acilis), Is.True);
            Assert.That(odul.Al(sinirdanOnce, acilis), Is.True);
            Assert.That(odul.HaftalikSeri, Is.EqualTo(1));
            Assert.That(odul.AylikSeri, Is.EqualTo(1));

            // 2. Aynı saatte yeniden alınamaz.
            Assert.That(odul.Al(sinirdanOnce, acilis), Is.False);
            Assert.That(odul.SifirlamayaKalan(sinirdanOnce), Is.EqualTo(TimeSpan.FromMinutes(1)));

            // 3. Sunucu saati gün sınırına gelince yeni ödül alınabilir hale gelir.
            Assert.That(odul.Alinabilir(sinirAni, acilis), Is.True);
            Assert.That(odul.Al(sinirAni, acilis), Is.True);
            Assert.That(odul.HaftalikSeri, Is.EqualTo(2));

            // 4. Açılış bölümünün altındaki oyuncuda özellik kapalıdır.
            var yeni = new GunlukOdul(P, kapilar);
            Assert.That(yeni.Acik(acilis - 1), Is.False);
            Assert.That(yeni.Al(sinirAni, acilis - 1), Is.False);
        }

        [Test]
        [Category("MT09")]
        [Property("GDD", "9.9.3, 9.8.4, 9.10.1, 9.10.2")]
        public void MT09_Haftalik_hatta_hat_puani_zorluga_gore_ve_carpanla_verilir()
        {
            var oyuncu = IleriOyuncu();
            var etiketler = new ZorlukEtiketTablosu();
            etiketler.Degistir(11, ZorlukEtiketi.Normal);
            etiketler.Degistir(12, ZorlukEtiketi.Zor);
            etiketler.Degistir(13, ZorlukEtiketi.CokZor);
            etiketler.Degistir(14, ZorlukEtiketi.AsiriZor);
            var beklenen = new[]
            {
                Tam(MetaParametreAdlari.HatPuaniNormal),
                Tam(MetaParametreAdlari.HatPuaniZor),
                Tam(MetaParametreAdlari.HatPuaniCokZor),
                Tam(MetaParametreAdlari.HatPuaniAsiriZor),
            };

            // Çarpan kapalı: artışlar etiket tablosundaki taban değerlerdir.
            for (int i = 0; i < 4; i++)
            {
                BolumuOyna(oyuncu, etiketler, 11 + i, kazan: true, Baslangic, out var sonuc);
                Assert.That(sonuc.HaftalikHatPuani, Is.EqualTo(beklenen[i]));
            }

            // Aynı bölüm çarpanla oynanırsa artış "taban × aktif çarpan" olur.
            oyuncu.Carpan.BiletKullan(Baslangic);
            int carpan = oyuncu.Carpan.AktifCarpan(Baslangic);
            Assert.That(carpan, Is.EqualTo(Tam(MetaParametreAdlari.CarpanBiletiKati)));
            var carpanli = BolumuOyna(oyuncu, etiketler, 12, kazan: true, Baslangic, out var carpanliSonuc);
            Assert.That(carpanli.CarpanIsareti, Is.EqualTo(carpan));
            Assert.That(carpanliSonuc.HaftalikHatPuani, Is.EqualTo(beklenen[1] * carpan));

            // İşaret bölüme girildiği anda konur: çarpanın süresi dolduktan sonra girilen bölümde çarpan 1'dir.
            var sonra = Baslangic + TimeSpan.FromMinutes(Tam(MetaParametreAdlari.EtkinlikCarpaniSuresiDk) + 1);
            var carpansiz = BolumuOyna(oyuncu, etiketler, 12, kazan: true, sonra, out var carpansizSonuc);
            Assert.That(carpansiz.CarpanIsareti, Is.EqualTo(1));
            Assert.That(carpansizSonuc.HaftalikHatPuani, Is.EqualTo(beklenen[1]));
        }

        [Test]
        [Category("MT10")]
        [Property("GDD", "9.8.4, 9.8.5, 9.10.3")]
        public void MT10_Sezon_hattinda_hat_puani_ayni_tablodan_gelir_carpan_uygulanmaz()
        {
            var oyuncu = IleriOyuncu();
            var etiketler = new ZorlukEtiketTablosu();
            etiketler.Degistir(21, ZorlukEtiketi.Normal);
            etiketler.Degistir(22, ZorlukEtiketi.AsiriZor);

            BolumuOyna(oyuncu, etiketler, 21, kazan: true, Baslangic, out var normal);
            BolumuOyna(oyuncu, etiketler, 22, kazan: true, Baslangic, out var asiriZor);
            Assert.That(normal.SezonHatPuani, Is.EqualTo(Tam(MetaParametreAdlari.HatPuaniNormal)));
            Assert.That(asiriZor.SezonHatPuani, Is.EqualTo(Tam(MetaParametreAdlari.HatPuaniAsiriZor)));

            // Çarpanla oynanan normal bölümde de artış taban değerdir.
            oyuncu.Carpan.BiletKullan(Baslangic);
            BolumuOyna(oyuncu, etiketler, 21, kazan: true, Baslangic, out var carpanli);
            Assert.That(carpanli.SezonHatPuani, Is.EqualTo(Tam(MetaParametreAdlari.HatPuaniNormal)));
            Assert.That(carpanli.HaftalikHatPuani, Is.GreaterThan(carpanli.SezonHatPuani), "Aynı bölümden iki hatta farklı puan (11.3.4).");

            // Bölüm kaybında hat puanı eklenmez.
            int basamak = oyuncu.SezonHatti.Basamak;
            int puan = oyuncu.SezonHatti.BasamakPuani;
            BolumuOyna(oyuncu, etiketler, 21, kazan: false, Baslangic, out var kayip);
            Assert.That(kayip.SezonHatPuani, Is.EqualTo(0));
            Assert.That(oyuncu.SezonHatti.Basamak, Is.EqualTo(basamak));
            Assert.That(oyuncu.SezonHatti.BasamakPuani, Is.EqualTo(puan));
        }

        [Test]
        [Category("MT11")]
        [Property("GDD", "9.8.2, 9.8.6, 9.8.7, 9.9.2, 9.9.5")]
        public void MT11_Odul_hatti_basamagi_hat_puani_hedefi_dolunca_atlanir()
        {
            var hedefler = OdulHatti.SezonBasamakHedefleri(P);
            Assert.That(hedefler.Count, Is.EqualTo(Tam(MetaParametreAdlari.SezonHattiBasamakSayisi)));
            Assert.That(hedefler.Sum(), Is.EqualTo(398), "Parametre dosyasındaki öneri: hazneye kadar toplam 398.");

            int zorPuani = Tam(MetaParametreAdlari.HatPuaniZor);

            // Artan puanın yalnız bir basamak atlatması için, sonraki hedefi artandan büyük olan bir basamak seçilir.
            int secilen = Enumerable.Range(0, hedefler.Count - 1).First(i => hedefler[i + 1] > zorPuani - 1);

            var oyuncu = IleriOyuncu();
            oyuncu.SezonHatti.PuanEkle(hedefler.Take(secilen + 1).Sum() - 1);
            Assert.That(oyuncu.SezonHatti.Basamak, Is.EqualTo(secilen), "Oyuncu basamak hedefinin 1 puan altında.");
            Assert.That(oyuncu.SezonHatti.UcretsizOdulAlinabilir(secilen), Is.False);

            var etiketler = new ZorlukEtiketTablosu();
            etiketler.Degistir(31, ZorlukEtiketi.Zor);
            BolumuOyna(oyuncu, etiketler, 31, kazan: true, Baslangic, out var sonuc);
            Assert.That(sonuc.SezonHatPuani, Is.EqualTo(zorPuani));

            // Basamak bir ileri gider; ücretsiz ödülü alınabilir olur; artan puan bir sonraki basamağa sayılır.
            Assert.That(oyuncu.SezonHatti.Basamak, Is.EqualTo(secilen + 1));
            Assert.That(oyuncu.SezonHatti.UcretsizOdulAlinabilir(secilen), Is.True);
            Assert.That(oyuncu.SezonHatti.BasamakPuani, Is.EqualTo(zorPuani - 1));

            // Premium hat satın alınınca geçilmiş basamakların premium ödülleri de alınabilir hale gelir.
            Assert.That(oyuncu.SezonHatti.PremiumOdulAlinabilir(0), Is.False);
            oyuncu.SezonPremiumSatinAl(Baslangic);
            for (int i = 0; i <= secilen; i++)
            {
                Assert.That(oyuncu.SezonHatti.PremiumOdulAlinabilir(i), Is.True, $"basamak {i}");
            }

            Assert.That(oyuncu.SezonHatti.PremiumOdulAlinabilir(secilen + 1), Is.False);
        }

        [Test]
        [Category("MT11")]
        [Property("GDD", "9.9.2, 9.9.5")]
        public void MT11_Haftalik_hatta_da_basamak_atlanir_ve_ucretli_hat_gecmis_odulleri_acar()
        {
            var hedefler = P.TamSayiListesi(MetaParametreAdlari.HaftalikHatBasamakHedefleri);
            Assert.That(hedefler.Count, Is.EqualTo(Tam(MetaParametreAdlari.HaftalikHatBasamakSayisi)));

            var hat = OdulHatti.Haftalik(P);
            hat.PuanEkle(hedefler[0] + hedefler[1] + hedefler[2] - 1);
            Assert.That(hat.Basamak, Is.EqualTo(2));

            hat.PuanEkle(Tam(MetaParametreAdlari.HatPuaniZor));

            Assert.That(hat.Basamak, Is.EqualTo(3));
            Assert.That(hat.BasamakPuani, Is.EqualTo(Tam(MetaParametreAdlari.HatPuaniZor) - 1));
            Assert.That(hat.UcretsizOdulAlinabilir(2), Is.True);
            hat.PremiumSatinAl();
            Assert.That(Enumerable.Range(0, 3).Select(hat.PremiumOdulAlinabilir), Is.All.True);
            Assert.That(hat.TopluAl(), Is.EqualTo(6));
            Assert.That(hat.UcretsizOdulAlinabilir(2), Is.False);
        }

        [Test]
        [Category("MT12")]
        [Property("GDD", "9.8.8")]
        public void MT12_Sezon_hattinin_birikim_haznesi_yalniz_son_basamakta_para_biriktirir()
        {
            int asamaPuani = Tam(MetaParametreAdlari.HazneAsamaPuani);
            int asamaParasi = Tam(MetaParametreAdlari.HazneAsamaParasi);
            int hedef = Tam(MetaParametreAdlari.SezonHazneHedefi);
            var hedefler = OdulHatti.SezonBasamakHedefleri(P);

            var hat = OdulHatti.Sezon(P);
            hat.PremiumSatinAl();

            // 1. Son ödül basamağına ulaşmamışken hazne 0 gösterir.
            hat.PuanEkle(hedefler.Sum() - 1);
            Assert.That(hat.SonBasamakGecildi, Is.False);
            Assert.That(hat.HazneParasi, Is.EqualTo(0));

            // 2. Son basamakta her hazne_asama_puani hat puanı bir aşamadır.
            hat.PuanEkle(1);
            Assert.That(hat.SonBasamakGecildi, Is.True);
            Assert.That(hat.HazneParasi, Is.EqualTo(0));
            hat.PuanEkle(asamaPuani - 1);
            Assert.That(hat.HazneParasi, Is.EqualTo(0));
            hat.PuanEkle(1);
            Assert.That(hat.HazneParasi, Is.EqualTo(asamaParasi));
            hat.PuanEkle(asamaPuani * 2);
            Assert.That(hat.HazneParasi, Is.EqualTo(asamaParasi * 3));
            Assert.That(hat.HazneDolu, Is.False);

            // 3. Hazne hedefe ulaşınca dolar ve alınır.
            hat.PuanEkle(hedef);
            Assert.That(hat.HazneDolu, Is.True);
            Assert.That(hat.HazneParasi, Is.EqualTo((hedef / asamaPuani) * asamaParasi));
            Assert.That(hat.HazneParasi, Is.EqualTo(4800), "Öneri değerle 4800 para.");
            Assert.That(hat.HazneyiAl(), Is.EqualTo(4800));
            Assert.That(hat.HazneParasi, Is.EqualTo(0));
        }
    }
}
