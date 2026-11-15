using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Testler.Vakalar;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>GDD 9 ve 10'un test_vakalari.md'de doğrudan vakası olmayan kuralları.</summary>
    [TestFixture]
    [Category("Kural")]
    public class MetaKuralTestleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static TimeSpan Dolum => TimeSpan.FromMinutes(P.Sayi(MetaParametreAdlari.CanDolumSuresiDk));

        [Test]
        [Property("GDD", "0.4")]
        public void Meta_katmanin_kullandigi_her_parametre_adi_dosyada_vardir()
        {
            var adlar = typeof(MetaParametreAdlari)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(a => a.IsLiteral)
                .Select(a => (string)a.GetRawConstantValue()!)
                .ToList();

            Assert.That(adlar, Is.Not.Empty);
            Assert.That(adlar.Where(ad => !P.Var(ad)), Is.Empty);
        }

        // ------------------------------------------------------------------ Can (9.1)

        [Test]
        [Property("GDD", "9.1.2, 9.1.3")]
        public void Can_yoksa_bolume_girilemez_ve_sifirin_altina_inmez()
        {
            var can = new CanSistemi(P, Simdi);
            can.CanAyarla(1, Simdi);
            Assert.That(can.BolumeGirebilir(Simdi), Is.True);

            can.BolumKaybi(Simdi);
            Assert.That(can.Mevcut(Simdi), Is.EqualTo(0));
            Assert.That(can.Durum(Simdi), Is.EqualTo(CanDurumu.Bos));
            Assert.That(can.BolumeGirebilir(Simdi), Is.False);

            can.BolumKaybi(Simdi);
            Assert.That(can.Mevcut(Simdi), Is.EqualTo(0));
            Assert.That(can.Mevcut(Simdi + Dolum), Is.EqualTo(1), "Boş → dolum süresi doldu → Eksik.");
        }

        [Test]
        [Property("GDD", "9.1.7")]
        public void Sinirsiz_can_aktifken_can_dusmez_sureler_toplanir_ve_duraklatilabilir()
        {
            var can = new CanSistemi(P, Simdi);
            can.SinirsizEkle(TimeSpan.FromMinutes(30), Simdi);
            can.SinirsizEkle(TimeSpan.FromMinutes(30), Simdi);

            Assert.That(can.Durum(Simdi), Is.EqualTo(CanDurumu.Sinirsiz));
            Assert.That(can.SinirsizKalan(Simdi), Is.EqualTo(TimeSpan.FromMinutes(60)), "Süreler üst üste toplanır.");
            can.BolumKaybi(Simdi);
            Assert.That(can.Mevcut(Simdi), Is.EqualTo(can.Azami));

            // Duraklatılmış süre ayrı tutulur ve akmaz.
            can.SinirsiziDuraklat(Simdi + TimeSpan.FromMinutes(10));
            Assert.That(can.SinirsizAktif(Simdi + TimeSpan.FromMinutes(20)), Is.False);
            can.SinirsiziSurdur(Simdi + TimeSpan.FromHours(5));
            Assert.That(can.SinirsizKalan(Simdi + TimeSpan.FromHours(5)), Is.EqualTo(TimeSpan.FromMinutes(50)));

            // Süre bitince durum Dolu'dur.
            Assert.That(can.Durum(Simdi + TimeSpan.FromHours(7)), Is.EqualTo(CanDurumu.Dolu));
        }

        [Test]
        [Property("GDD", "9.1.5, 9.1.6, 9.1.8")]
        public void Hediye_can_azamiyi_asmaz_dolum_teklifi_doldurur_saat_geri_alinirsa_deger_duzeltilir()
        {
            var can = new CanSistemi(P, Simdi);
            can.CanAyarla(2, Simdi);

            can.CanEkle(1, Simdi);
            Assert.That(can.Mevcut(Simdi), Is.EqualTo(3));
            can.CanEkle(50, Simdi);
            Assert.That(can.Mevcut(Simdi), Is.EqualTo(can.Azami), "Azami can aşılmaz.");

            can.CanAyarla(0, Simdi);
            can.TamDoldur(Simdi);
            Assert.That(can.Durum(Simdi), Is.EqualTo(CanDurumu.Dolu));

            // Cihaz saati bir gün geri alınır: dolmasına bütün canların dolum süresinden uzun kalamaz.
            can.CanAyarla(0, Simdi);
            var geri = Simdi - TimeSpan.FromDays(1);
            Assert.That(can.Mevcut(geri), Is.EqualTo(0));
            Assert.That(can.TamDolacagiAn, Is.EqualTo(geri + TimeSpan.FromTicks(Dolum.Ticks * can.Azami)));
        }

        // ------------------------------------------------------------------ Para, envanter, mağaza (9.2, 9.3)

        [Test]
        [Property("GDD", "9.2.1, 9.2.7")]
        public void Yeni_oyuncu_baslangic_parasiyla_baslar_ve_para_olaylari_tetiklenir()
        {
            var cuzdan = new Cuzdan(P);
            var degisimler = new List<int>();
            var harcamalar = new List<HarcamaKaydi>();
            cuzdan.ParaDegisti += degisimler.Add;
            cuzdan.ParaHarcandi += harcamalar.Add;
            int baslangic = P.TamSayi(MetaParametreAdlari.BaslangicParasi);
            Assert.That(cuzdan.Bakiye, Is.EqualTo(baslangic));

            cuzdan.Ekle(100);
            Assert.That(cuzdan.Harca(40, "deneme"), Is.True);

            Assert.That(degisimler, Is.EqualTo(new[] { baslangic + 100, baslangic + 60 }));
            Assert.That(harcamalar.Single().Ad, Is.EqualTo("deneme"));
        }

        [Test]
        [Property("GDD", "9.3.1, 9.3.6, 9.3.10")]
        public void Gudumlu_tas_satilmaz_sinirsiz_surede_adet_dusmez_azami_adet_asilmaz()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            Assert.That(oyuncu.Magaza.Fiyat(TakviyeTuru.Gudumlu), Is.Null);
            Assert.That(oyuncu.Magaza.SatinAl(TakviyeTuru.Gudumlu), Is.False);

            oyuncu.Envanter.Ekle(TakviyeTuru.Bomba, 2);
            oyuncu.Envanter.SinirsizEkle(TakviyeTuru.Bomba, 60, Simdi);
            oyuncu.Envanter.SinirsizEkle(TakviyeTuru.Bomba, 60, Simdi);
            Assert.That(oyuncu.Envanter.Kullan(TakviyeTuru.Bomba, Simdi + TimeSpan.FromSeconds(100)), Is.True);
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(2), "Sınırsız süre aktifken adet düşmez.");

            var sonra = Simdi + TimeSpan.FromSeconds(121);
            Assert.That(oyuncu.Envanter.Kullan(TakviyeTuru.Bomba, sonra), Is.True);
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(1));

            oyuncu.Envanter.Ekle(TakviyeTuru.Satir, 100000);
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Satir), Is.EqualTo(P.TamSayi(MetaParametreAdlari.TakviyeAzamiAdet)));
            Assert.That(oyuncu.Envanter.Kullan(TakviyeTuru.Sutun, Simdi), Is.False);
        }

        [Test]
        [Property("GDD", "9.2.6, 9.3.11")]
        public void Odul_paketi_para_can_takviye_ve_sinirsiz_sureleri_ayni_yoldan_ekler()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            oyuncu.Can.CanAyarla(1, Simdi);
            int bakiye = oyuncu.Cuzdan.Bakiye;
            var paket = new OdulPaketi { Para = 300, Can = 2, Yildiz = 3 };
            paket.Takviyeler[TakviyeTuru.Bomba] = 2;
            paket.TakviyeSinirsizSaniye[TakviyeTuru.Cizgi] = 600;

            oyuncu.OdulVer(paket, Simdi);

            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + 300));
            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(3));
            Assert.That(oyuncu.Yildiz, Is.EqualTo(3));
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(2));
            Assert.That(oyuncu.Envanter.SinirsizAktif(TakviyeTuru.Cizgi, Simdi + TimeSpan.FromSeconds(599)), Is.True);
        }

        [Test]
        [Property("GDD", "9.4.1")]
        public void Deney_grubu_bir_kapiyi_one_cekebilir()
        {
            var kapilar = new IlerlemeKapilari(P);
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisSezonHatti);
            Assert.That(kapilar.Acik(MetaParametreAdlari.AcilisSezonHatti, acilis - 5), Is.False);

            kapilar.OneCek(MetaParametreAdlari.AcilisSezonHatti, acilis - 5);

            Assert.That(kapilar.Acik(MetaParametreAdlari.AcilisSezonHatti, acilis - 5), Is.True);
        }

        // ------------------------------------------------------------------ Günlük ödül (9.5)

        [Test]
        [Property("GDD", "9.5.6, 9.5.7, 9.5.8")]
        public void Gun_kacirilinca_haftalik_seri_sifirlanir_aylik_surer_saat_hilesi_duzeltilir()
        {
            var odul = new GunlukOdul(P, new IlerlemeKapilari(P));
            int bolum = P.TamSayi(MetaParametreAdlari.AcilisGunlukOdul);

            for (int gun = 0; gun < 3; gun++)
            {
                Assert.That(odul.Al(Simdi + TimeSpan.FromDays(gun), bolum), Is.True);
            }

            Assert.That(odul.HaftalikSeri, Is.EqualTo(3));

            // Bir gün atlanır.
            Assert.That(odul.Al(Simdi + TimeSpan.FromDays(4), bolum), Is.True);
            Assert.That(odul.HaftalikSeri, Is.EqualTo(1));
            Assert.That(odul.AylikSeri, Is.EqualTo(4));

            // Hafta tamamlanınca bir sonraki ödül setine geçilir.
            int hafta = P.TamSayi(MetaParametreAdlari.HaftalikSeriUzunlugu);
            for (int gun = 5; gun < 4 + hafta + 1; gun++)
            {
                odul.Al(Simdi + TimeSpan.FromDays(gun), bolum);
            }

            Assert.That(odul.SetSirasi, Is.EqualTo(1));

            // Son alınan gün sunucu gününden ilerideyse hile kabul edilir ve ilerleme düzeltilir.
            var gecmis = Simdi - TimeSpan.FromDays(30);
            Assert.That(odul.SaatHilesiniDenetle(gecmis), Is.True);
            Assert.That(odul.SonAlinanGun, Is.EqualTo(odul.GunNumarasi(gecmis)));
            Assert.That(odul.SaatHilesiniDenetle(gecmis), Is.False);

            odul.SunucuKapatti = true;
            Assert.That(odul.Alinabilir(Simdi + TimeSpan.FromDays(60), bolum), Is.False, "Sunucu özelliği kapatabilir.");
        }

        // ------------------------------------------------------------------ Ödül hattı (9.8)

        [Test]
        [Property("GDD", "9.8.2, 9.8.8")]
        public void Bir_kazanim_birden_fazla_basamak_atlatabilir_ve_hazne_premiumsuz_para_biriktirmez()
        {
            var hat = OdulHatti.Sezon(P);
            var hedefler = OdulHatti.SezonBasamakHedefleri(P);

            hat.PuanEkle(hedefler[0] + hedefler[1] + 1);
            Assert.That(hat.Basamak, Is.EqualTo(2));
            Assert.That(hat.BasamakPuani, Is.EqualTo(1));
            Assert.That(hat.SiradakiHedef, Is.EqualTo(hedefler[2]));

            hat.PuanEkle(10000);
            Assert.That(hat.SonBasamakGecildi, Is.True);
            Assert.That(hat.HaznePuani, Is.EqualTo(P.TamSayi(MetaParametreAdlari.SezonHazneHedefi)), "Hazne hedefte dolar.");
            Assert.That(hat.HazneParasi, Is.EqualTo(0), "Hazne premium hatta para biriktirir.");
            Assert.That(hat.HazneyiAl(), Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ Zorluk ve ek hamle (9.11, 9.12)

        [Test]
        [Property("GDD", "9.11.1, 9.11.5")]
        public void Deneme_sayaci_basarisizlikta_artar_kazaninca_sifirlanir_agirlik_etikete_ve_beceriye_baglidir()
        {
            var zorluk = new YariDinamikZorluk(P);
            zorluk.BasarisizlikKaydet(7);
            zorluk.BasarisizlikKaydet(7);
            Assert.That(zorluk.DenemeSayaci(7), Is.EqualTo(2));
            Assert.That(zorluk.DenemeSayaci(8), Is.EqualTo(0));
            zorluk.KazanimKaydet(7);
            Assert.That(zorluk.DenemeSayaci(7), Is.EqualTo(0));

            for (int i = 0; i < 500; i++) zorluk.BasarisizlikKaydet(9);
            Assert.That(zorluk.DenemeSayaci(9), Is.EqualTo(P.TamSayi(MetaParametreAdlari.DenemeSayaciAzami)));

            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.Normal), Is.EqualTo(0));
            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.Zor), Is.EqualTo(0.2).Within(1e-9));
            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.CokZor), Is.EqualTo(0.35).Within(1e-9));
            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.AsiriZor), Is.EqualTo(0.5).Within(1e-9));

            zorluk.Beceri = BeceriDuzeyi.Becerisiz;
            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.AsiriZor), Is.EqualTo(0.4).Within(1e-9));
            Assert.That(zorluk.RastgelelikAgirligi(ZorlukEtiketi.Normal), Is.EqualTo(0), "Ağırlık 0'ın altına inmez.");
        }

        [Test]
        [Property("GDD", "9.12.1, 9.12.2, 9.12.3, 9.12.5, 9.11.4")]
        public void Ek_hamle_fiyati_basamaklanir_ust_kademelerde_ozel_tas_verilir()
        {
            var teklif = new EkHamleTeklifi(P);
            var kademeler = P.TamSayiListesi(MetaParametreAdlari.EkHamleFiyatKademeleri);
            double carpan = P.Sayi(MetaParametreAdlari.YuksekFiyatCarpani);

            Assert.That(teklif.Fiyat(false), Is.EqualTo(kademeler[0]));
            Assert.That(teklif.Fiyat(true), Is.EqualTo((int)(kademeler[0] * carpan)));
            Assert.That(teklif.PaketTaslari(), Is.Empty, "1. kademe: yok.");

            teklif.AlimKaydet();
            Assert.That(teklif.Fiyat(false), Is.EqualTo(kademeler[1]));
            Assert.That(teklif.PaketTaslari(), Is.EqualTo(new[] { TasTuru.Bomba }));

            teklif.AlimKaydet();
            Assert.That(teklif.PaketTaslari(), Is.EquivalentTo(new[] { TasTuru.Gudumlu, TasTuru.Bomba }));

            for (int i = 0; i < 10; i++) teklif.AlimKaydet();
            Assert.That(teklif.Fiyat(false), Is.EqualTo(kademeler.Last()), "Son kademeden sonra fiyat sabit kalır.");
            Assert.That(
                teklif.PaketTaslari(),
                Is.EquivalentTo(new[] { TasTuru.RenkKuresi, TasTuru.RenkKuresi, TasTuru.Bomba }));

            teklif.Sifirla();
            Assert.That(teklif.Fiyat(false), Is.EqualTo(kademeler[0]), "Kademe bölüm kazanılınca ya da bırakılınca sıfırlanır.");

            int taban = P.TamSayi(MetaParametreAdlari.EkHamleSayisi);
            int esik = P.TamSayi(MetaParametreAdlari.EkHamleArtisEsigi);
            int miktar = P.TamSayi(MetaParametreAdlari.EkHamleArtisMiktari);
            int sinir = P.TamSayi(MetaParametreAdlari.EkHamleArtisSiniri);
            Assert.That(teklif.Hamle(0), Is.EqualTo(taban));
            Assert.That(teklif.Hamle(esik - 1), Is.EqualTo(taban));
            Assert.That(teklif.Hamle(esik), Is.EqualTo(taban + miktar));
            Assert.That(teklif.Hamle(sinir + 50), Is.EqualTo(teklif.Hamle(sinir)), "Artış sınırda durur.");

            int sonBolum = P.TamSayi(MetaParametreAdlari.IkinciUyariSonBolum);
            Assert.That(teklif.IkinciUyariGosterilir(sonBolum), Is.True);
            Assert.That(teklif.IkinciUyariGosterilir(sonBolum + 1), Is.False);
        }

        // ------------------------------------------------------------------ Bölüm akışı

        private const string TeklifBolumu = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 1,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 99 }] }";

        private static BolumAkisi TeklifeKadarOyna(Oyuncu oyuncu, List<Olay> olaylar, params TakviyeTuru[] secilen)
        {
            var akis = new BolumAkisi(oyuncu, new ZorlukEtiketTablosu(), 5);
            var oturum = akis.Baslat(
                BolumYukleyici.JsondanOku(TeklifBolumu, P), AkBayraklari.Varsayilan, 3, Simdi, secilen, olaylar);
            Assert.That(oturum, Is.Not.Null);
            return akis;
        }

        [Test]
        [Property("GDD", "7.4.2, 7.4.3, 9.12.2")]
        public void Ek_hamle_satin_alinca_para_duser_hamle_eklenir_ve_bolum_surer()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            var olaylar = new List<Olay>();
            var akis = TeklifeKadarOyna(oyuncu, olaylar);
            akis.Oturum!.Kaydir(TahtaKurucu.H(1, 4), TahtaKurucu.H(1, 3), olaylar);
            Assert.That(akis.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));

            int bakiye = oyuncu.Cuzdan.Bakiye;
            int fiyat = akis.TeklifFiyati;
            Assert.That(akis.EkHamleSatinAl(olaylar), Is.True);

            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye - fiyat));
            Assert.That(oyuncu.Cuzdan.Harcamalar.Single().Ad, Is.EqualTo("ek_hamle"));
            Assert.That(akis.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(akis.Oturum.KalanHamle, Is.EqualTo(P.TamSayi(MetaParametreAdlari.EkHamleSayisi)));
            Assert.That(akis.TeklifFiyati, Is.GreaterThan(fiyat), "Sıradaki alım bir üst kademedendir.");
        }

        [Test]
        [Property("GDD", "9.2.5, 9.12.2")]
        public void Yetersiz_bakiyede_ek_hamle_teklifi_acik_kalir()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            oyuncu.Cuzdan.SunucuylaEsitle(1);
            var olaylar = new List<Olay>();
            var akis = TeklifeKadarOyna(oyuncu, olaylar);
            akis.Oturum!.Kaydir(TahtaKurucu.H(1, 4), TahtaKurucu.H(1, 3), olaylar);

            Assert.That(akis.EkHamleSatinAl(olaylar), Is.False);
            Assert.That(akis.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "9.1.2, 9.1.3, 10.2.8")]
        public void Can_yokken_bolum_baslamaz_cikis_kayip_sayilir_kurtarma_bolumunde_can_dusmez()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            var olaylar = new List<Olay>();

            // Başlamış bir bölümden çıkmak da kayıp sayılır.
            var akis = TeklifeKadarOyna(oyuncu, olaylar);
            int can = oyuncu.Can.Mevcut(Simdi);
            Assert.That(akis.SonucuIsle(Simdi, cikildi: true).CanDustu, Is.True);
            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(can - 1));
            Assert.That(oyuncu.Zorluk.DenemeSayaci(5), Is.EqualTo(1));
            Assert.That(akis.SonucuIsle(Simdi, cikildi: true).CanDustu, Is.False, "Sonuç bir kez işlenir.");

            // Kurtarma bölümünde başarısızlıkta can düşmez.
            var kurtarma = TeklifeKadarOyna(oyuncu, olaylar);
            kurtarma.KayiptaCanDusmez = true;
            Assert.That(kurtarma.SonucuIsle(Simdi, cikildi: true).CanDustu, Is.False);
            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(can - 1));

            oyuncu.Can.CanAyarla(0, Simdi);
            var cansiz = new BolumAkisi(oyuncu, new ZorlukEtiketTablosu(), 5);
            Assert.That(
                cansiz.Baslat(BolumYukleyici.JsondanOku(TeklifBolumu, P), AkBayraklari.Varsayilan, 3, Simdi, new TakviyeTuru[0], olaylar),
                Is.Null);
        }

        [Test]
        [Property("GDD", "9.3.2, 9.3.5, 3.9.1")]
        public void Secilen_baslangic_takviyesi_envanterden_duser_ve_tahtaya_konur_kapisi_kapaliysa_konmaz()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            oyuncu.Envanter.Ekle(TakviyeTuru.Bomba, 2);
            var olaylar = new List<Olay>();

            var akis = TeklifeKadarOyna(oyuncu, olaylar, TakviyeTuru.Bomba, TakviyeTuru.Cizgi);

            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(1));
            var konan = olaylar.OfType<TakviyeKondu>().Single();
            Assert.That(konan.Tas.Tur, Is.EqualTo(TasTuru.Bomba), "Envanterde olmayan çizgi taşı konmaz.");
            Assert.That(akis.Oturum!.Baglam.Tahta[konan.Konum].Tas, Is.SameAs(konan.Tas));

            var yeni = new Oyuncu(P, Simdi) { EnYuksekBolum = 1 };
            yeni.Envanter.Ekle(TakviyeTuru.Bomba, 1);
            var yeniOlaylar = new List<Olay>();
            TeklifeKadarOyna(yeni, yeniOlaylar, TakviyeTuru.Bomba);
            Assert.That(yeniOlaylar.OfType<TakviyeKondu>(), Is.Empty);
            Assert.That(yeni.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "9.3.4, 3.9.4")]
        public void Oyun_ici_arac_kullaninca_adet_bir_azalir_adet_yoksa_kullanilamaz()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            oyuncu.Envanter.Ekle(TakviyeTuru.TekHucre, 1);
            var olaylar = new List<Olay>();
            var akis = TeklifeKadarOyna(oyuncu, olaylar);

            Assert.That(akis.AracKullan(TakviyeTuru.TekHucre, TahtaKurucu.H(3, 1), Simdi, olaylar), Is.True);
            Assert.That(oyuncu.Envanter.Adet(TakviyeTuru.TekHucre), Is.EqualTo(0));
            Assert.That(akis.AracKullan(TakviyeTuru.TekHucre, TahtaKurucu.H(3, 2), Simdi, olaylar), Is.False);
            Assert.That(akis.AracKullan(TakviyeTuru.Satir, TahtaKurucu.H(3, 2), Simdi, olaylar), Is.False);
            Assert.That(akis.Oturum!.KalanHamle, Is.EqualTo(1), "Araç hamle harcamaz.");
        }

        [Test]
        [Property("GDD", "3.6.1, 3.6.7, 9.3.8, 9.3.9, 3.8.1")]
        public void Kazanma_serisi_cift_renk_takviyesini_yedek_hamle_hakkini_ve_seri_takviyesini_acar()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 500 };
            var etiketler = new ZorlukEtiketTablosu();
            Assert.That(oyuncu.CiftRenkTakviyesiHazir, Is.False);
            Assert.That(oyuncu.SeriTakviyesiKademesi, Is.EqualTo(0));

            int esik = Math.Max(
                P.TamSayi(MetaParametreAdlari.CiftRenkSeriEsigi), P.TamSayi(MetaParametreAdlari.YedekHamleSeriEsigi));
            for (int i = 0; i < esik; i++)
            {
                MetaVakalari.BolumuOyna(oyuncu, etiketler, 40 + i, kazan: true, Simdi, out _);
            }

            Assert.That(oyuncu.KazanmaSerisi, Is.EqualTo(esik));
            Assert.That(oyuncu.CiftRenkTakviyesiHazir, Is.True);
            Assert.That(oyuncu.YedekHamleHakkiHazir, Is.True);
            Assert.That(
                oyuncu.SeriTakviyesiKademesi,
                Is.EqualTo(Math.Min(esik, P.TamSayi(MetaParametreAdlari.SeriTakviyesiAzami))));

            // Bölüm çift renk modunda başlar; tahtaya bir renk küresi ve seri kademesi kadar çizgi taşı konur.
            var olaylar = new List<Olay>();
            var akis = TeklifeKadarOyna(oyuncu, olaylar);
            Assert.That(akis.Oturum!.Baglam.CiftRenkModu, Is.True);
            Assert.That(akis.Oturum.YedekHamleHakki, Is.True);
            var konanlar = olaylar.OfType<TakviyeKondu>().Select(t => t.Tas.Tur).ToList();
            Assert.That(konanlar.Count(t => t == TasTuru.RenkKuresi), Is.EqualTo(1));
            Assert.That(konanlar.Count(t => t == TasTuru.Cizgi), Is.EqualTo(oyuncu.SeriTakviyesiKademesi));

            // Kayıpta seri sıfırlanır.
            akis.SonucuIsle(Simdi, cikildi: true);
            Assert.That(oyuncu.KazanmaSerisi, Is.EqualTo(0));
            Assert.That(oyuncu.CiftRenkTakviyesiHazir, Is.False);
        }

        // ------------------------------------------------------------------ Alan, albüm (9.6, 9.7)

        [Test]
        [Property("GDD", "9.6.2, 9.6.3, 9.6.4")]
        public void Alan_gorevleri_onkosul_ve_yildizla_insa_edilir_son_gorevde_sandik_acilir()
        {
            var alan = new AlanInsasi(P);
            int fiyat = alan.VarsayilanGorevFiyati;
            alan.GorevleriYukle(new[]
            {
                new AlanGorevi("zemin", fiyat),
                new AlanGorevi("duvar", fiyat, "zemin"),
                new AlanGorevi("cati", fiyat, "duvar"),
            });

            Assert.That(alan.InsaEt("duvar", 100), Is.EqualTo(0), "Önkoşul tamamlanmadan inşa edilemez.");
            Assert.That(alan.InsaEt("zemin", fiyat - 1), Is.EqualTo(0), "Yeterli yıldız yok.");
            Assert.That(alan.InsaEt("zemin", fiyat), Is.EqualTo(fiyat));
            Assert.That(alan.SandigiAc(), Is.Null);

            Assert.That(alan.HepsiniInsaEt(fiyat * 2), Is.EqualTo(fiyat * 2), "Kalan görevler tek seferde.");
            Assert.That(alan.Durum, Is.EqualTo(AlanDurumu.SandikHazir));

            var sandik = alan.SandigiAc();
            Assert.That(sandik!.Para, Is.EqualTo(P.TamSayi(MetaParametreAdlari.AlanSandigiPara)));
            Assert.That(alan.AlanNumarasi, Is.EqualTo(2));
            Assert.That(alan.SandigiAc(), Is.Null);
        }

        [Test]
        [Property("GDD", "9.7.2, 9.7.3, 9.7.7, 9.7.8")]
        public void Album_setleri_dolar_cift_kartlar_sayilir()
        {
            var album = new Album(P);
            int kart = P.TamSayi(MetaParametreAdlari.SetBasinaKart);

            for (int i = 0; i < kart; i++)
            {
                Assert.That(album.KartEkle(0, i), Is.True);
            }

            Assert.That(album.SetDolu(0), Is.True);
            Assert.That(album.SetDolu(1), Is.False);
            Assert.That(album.KartEkle(0, 0), Is.False, "Çift kart.");
            Assert.That(album.CiftKartSayisi, Is.EqualTo(1));
            Assert.That(album.HepsiDolu, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => album.KartEkle(0, kart));
        }

        // ------------------------------------------------------------------ Olay iskeletleri (10)

        [Test]
        [Property("GDD", "10.0.1, 10.0.2, 10.0.4, 10.9.2, 10.9.3")]
        public void Basamakli_toplama_olayi_puanla_basamak_doldurur_ve_kapisina_suresine_baglidir()
        {
            var bitis = Simdi + TimeSpan.FromDays(3);
            var olay = new BasamakliToplamaOlayi("bahce", MetaParametreAdlari.AcilisIlkToplamaOlayi, bitis, new[] { 4, 6 });
            var kapilar = new IlerlemeKapilari(P);
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisIlkToplamaOlayi);

            Assert.That(olay.Acik(kapilar, acilis - 1, Simdi), Is.False);
            Assert.That(olay.Acik(kapilar, acilis, Simdi), Is.True);
            Assert.That(olay.Acik(kapilar, acilis, bitis), Is.False);
            olay.SunucuKapatti = true;
            Assert.That(olay.Acik(kapilar, acilis, Simdi), Is.False);

            olay.BolumKazanildi(5);
            Assert.That(olay.Basamak, Is.EqualTo(1));
            Assert.That(olay.BasamakPuani, Is.EqualTo(1));
            Assert.That(olay.OduluAl(0), Is.True);
            Assert.That(olay.OduluAl(0), Is.False);
            Assert.That(olay.OduluAl(1), Is.False);

            olay.BolumKazanildi(100);
            Assert.That(olay.Tamamlandi, Is.True);
        }

        [Test]
        [Property("GDD", "10.5.3, 10.5.4")]
        public void Risk_basamaklarinda_kayip_kurali()
        {
            var olay = new RiskBasamaklariOlayi("risk", Simdi + TimeSpan.FromDays(3), P);
            olay.Gir();
            int hak = P.TamSayi(MetaParametreAdlari.RiskBasamakSansHakki);
            var hedefler = P.TamSayiListesi(MetaParametreAdlari.RiskBasamakHedefleri);

            // (2) Mevcut basamakta henüz toplanabilir yoksa ceza yoktur.
            olay.BolumKaybedildi();
            Assert.That(olay.SansHakki, Is.EqualTo(hak));

            // (3) Aksi halde bir şans hakkı gider.
            olay.BolumKazanildi(hedefler[0] + 1);
            Assert.That(olay.Basamak, Is.EqualTo(1));
            olay.BolumKaybedildi();
            Assert.That(olay.SansHakki, Is.EqualTo(hak - 1));
            Assert.That(olay.BasamakPuani, Is.EqualTo(1));

            // (4) Giden hak sonuncuysa yalnızca mevcut basamağın ilerlemesi sıfırlanır; haklar yeniden dolar.
            for (int i = 1; i < hak; i++) olay.BolumKaybedildi();
            Assert.That(olay.BasamakPuani, Is.EqualTo(0));
            Assert.That(olay.Basamak, Is.EqualTo(1), "Önceki basamaklar korunur.");
            Assert.That(olay.SansHakki, Is.EqualTo(hak));

            // (1) Bütün basamaklar bittiyse hiçbir şey olmaz.
            olay.BolumKazanildi(hedefler.Sum());
            Assert.That(olay.Tamamlandi, Is.True);
            olay.BolumKaybedildi();
            Assert.That(olay.SansHakki, Is.EqualTo(hak));
        }

        // ------------------------------------------------------------------ Reklam ödülleri (9.13)

        [Test]
        [Property("GDD", "9.13.1, 9.1.6")]
        public void Reklam_can_ve_para_verir_azami_can_asilmaz()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            int azami = oyuncu.Can.Mevcut(Simdi);
            oyuncu.Can.CanAyarla(1, Simdi);
            int bakiye = oyuncu.Cuzdan.Bakiye;

            oyuncu.ReklamCaniVer(Simdi);
            oyuncu.ReklamParasiVer();

            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(1 + P.TamSayi(MetaParametreAdlari.ReklamCanOdulu)));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + P.TamSayi(MetaParametreAdlari.ReklamParaOdulu)));

            for (int i = 0; i < azami + 2; i++)
            {
                oyuncu.ReklamCaniVer(Simdi);
            }

            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(azami));
        }

        [Test]
        [Property("GDD", "9.12.4, 9.13.1, 7.4.3")]
        public void Reklamla_ek_hamle_alinir_para_harcanmaz_ve_fiyat_kademesi_ilerlemez()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 20 };
            var olaylar = new List<Olay>();
            var akis = TeklifeKadarOyna(oyuncu, olaylar);
            Assert.That(akis.ReklamlaEkHamleAl(olaylar), Is.False, "Açık teklif yokken reklamla hamle alınmaz.");

            akis.Oturum!.Kaydir(TahtaKurucu.H(1, 4), TahtaKurucu.H(1, 3), olaylar);
            Assert.That(akis.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
            int bakiye = oyuncu.Cuzdan.Bakiye;
            int fiyat = akis.TeklifFiyati;

            Assert.That(akis.ReklamlaEkHamleAl(olaylar), Is.True);

            Assert.That(akis.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(akis.Oturum.KalanHamle, Is.EqualTo(P.TamSayi(MetaParametreAdlari.EkHamleReklamSayisi)));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye));
            Assert.That(akis.TeklifFiyati, Is.EqualTo(fiyat));
        }

        // ------------------------------------------------------------------ Mağaza ve teklifler (9.14)

        private static MagazaUrunu Urun(string kimlik, int para) => new MagazaUrunu(kimlik, new OdulPaketi { Para = para });

        [Test]
        [Property("GDD", "9.14.1, 9.14.3")]
        public void Para_magazasi_kapisindan_sonra_acilir_sabit_sayida_urun_gosterir_ve_paketi_dogrudan_verir()
        {
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisMagaza);
            int adet = P.TamSayi(MetaParametreAdlari.MagazaUrunSayisi);
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis - 1 };
            var magaza = new ParaMagazasi(P, oyuncu.Kapilar);
            magaza.UrunleriAyarla(Enumerable.Range(1, adet + 3).Select(i => Urun("urun" + i, i * 100)));

            Assert.That(magaza.Acik(oyuncu.EnYuksekBolum), Is.False);
            Assert.That(magaza.GorunenUrunler(oyuncu.EnYuksekBolum), Is.Empty);
            Assert.That(magaza.SatinAlimTamamlandi(oyuncu, "urun1", Simdi), Is.False);

            oyuncu.EnYuksekBolum = acilis;
            Assert.That(magaza.GorunenUrunler(oyuncu.EnYuksekBolum).Select(u => u.Kimlik),
                Is.EqualTo(Enumerable.Range(1, adet).Select(i => "urun" + i)));

            int bakiye = oyuncu.Cuzdan.Bakiye;
            Assert.That(magaza.SatinAlimTamamlandi(oyuncu, "urun2", Simdi), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + 200));
            Assert.That(oyuncu.Cuzdan.Harcamalar, Is.Empty, "Gerçek para satın alımı oyun parası harcamaz.");
            Assert.That(magaza.SatinAlimTamamlandi(oyuncu, "urun" + (adet + 1), Simdi), Is.False, "Gösterilmeyen ürün alınamaz.");
        }

        [Test]
        [Property("GDD", "9.14.2")]
        public void Kademeli_teklifin_basamaklari_sirayla_acilir()
        {
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisKademeliTeklif);
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis - 1 };
            var teklif = new KademeliTeklif(oyuncu.Kapilar, new[] { Urun("b1", 100), Urun("b2", 300), Urun("b3", 900) });

            Assert.That(teklif.AcikBasamak(oyuncu.EnYuksekBolum), Is.Null);
            Assert.That(teklif.SatinAlimTamamlandi(oyuncu, Simdi), Is.False);

            oyuncu.EnYuksekBolum = acilis;
            int bakiye = oyuncu.Cuzdan.Bakiye;
            foreach (var (kimlik, para) in new[] { ("b1", 100), ("b2", 300), ("b3", 900) })
            {
                Assert.That(teklif.AcikBasamak(oyuncu.EnYuksekBolum)!.Kimlik, Is.EqualTo(kimlik));
                Assert.That(teklif.SatinAlimTamamlandi(oyuncu, Simdi), Is.True);
                bakiye += para;
                Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye));
            }

            Assert.That(teklif.Bitti, Is.True);
            Assert.That(teklif.AcikBasamak(oyuncu.EnYuksekBolum), Is.Null);
            Assert.That(teklif.SatinAlimTamamlandi(oyuncu, Simdi), Is.False);
        }

        [Test]
        [Property("GDD", "9.14.2")]
        public void Donemsel_teklif_yalniz_doneminde_ve_magaza_kapisi_acikken_gecerlidir()
        {
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisMagaza);
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis };
            var teklif = new DonemselTeklif(oyuncu.Kapilar, Urun("donem", 500), Simdi, Simdi.AddDays(3));

            Assert.That(teklif.Gecerli(acilis - 1, Simdi), Is.False);
            Assert.That(teklif.Gecerli(acilis, Simdi.AddSeconds(-1)), Is.False);
            Assert.That(teklif.Gecerli(acilis, Simdi.AddDays(3)), Is.False);
            Assert.That(teklif.Gecerli(acilis, Simdi.AddDays(1)), Is.True);

            int bakiye = oyuncu.Cuzdan.Bakiye;
            Assert.That(teklif.SatinAlimTamamlandi(oyuncu, Simdi.AddDays(1)), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + 500));
            Assert.That(teklif.SatinAlimTamamlandi(oyuncu, Simdi.AddDays(1)), Is.False, "Teklif bir kez alınır.");
        }

        // ------------------------------------------------------------------ Diğer meta özellikler (9.15)

        [Test]
        [Property("GDD", "9.15.1")]
        public void Bir_sure_oynamayan_oyuncuya_donuste_hediye_verilir()
        {
            int acilis = P.TamSayi(MetaParametreAdlari.AcilisKarsilamaHediyesi);
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis };
            var sure = TimeSpan.FromDays(7);
            var hediye = new GeriDonusHediyesi(oyuncu.Kapilar, sure, new OdulPaketi { Para = 300 }, Simdi);
            int bakiye = oyuncu.Cuzdan.Bakiye;

            Assert.That(hediye.OyunaGirildi(oyuncu, Simdi.AddDays(6)), Is.False, "Süre dolmadı.");
            Assert.That(hediye.OyunaGirildi(oyuncu, Simdi.AddDays(12)), Is.False, "Süre son girişten ölçülür.");
            Assert.That(hediye.OyunaGirildi(oyuncu, Simdi.AddDays(19)), Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + 300));
            Assert.That(hediye.OyunaGirildi(oyuncu, Simdi.AddDays(19)), Is.False);

            var yeni = new Oyuncu(P, Simdi) { EnYuksekBolum = acilis - 1 };
            var kapali = new GeriDonusHediyesi(yeni.Kapilar, sure, new OdulPaketi { Para = 300 }, Simdi);
            Assert.That(kapali.OyunaGirildi(yeni, Simdi.AddDays(30)), Is.False, "Kapı açılmadan hediye verilmez.");
        }

        [Test]
        [Property("GDD", "9.15.2, 9.2.3")]
        public void Hesap_baglama_odulu_bir_kez_verilir()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            int bakiye = oyuncu.Cuzdan.Bakiye;

            Assert.That(oyuncu.HesapBagla(), Is.True);
            Assert.That(oyuncu.HesapBagla(), Is.False);

            Assert.That(oyuncu.HesapBagli, Is.True);
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(bakiye + P.TamSayi(MetaParametreAdlari.HesapBaglamaOdulu)));
        }

        [Test]
        [Property("GDD", "9.15.3, 9.4")]
        public void Liderlik_tablosu_arkadas_daveti_ve_gorevler_kendi_kapilarinda_acilir()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 1 };
            Assert.That(new[] { oyuncu.LiderlikTablosuAcik, oyuncu.ArkadasDavetiAcik, oyuncu.GorevlerAcik }, Is.All.False);

            oyuncu.EnYuksekBolum = P.TamSayi(MetaParametreAdlari.AcilisLiderlikTablosu);
            Assert.That(oyuncu.LiderlikTablosuAcik, Is.True);
            oyuncu.EnYuksekBolum = P.TamSayi(MetaParametreAdlari.AcilisArkadasDaveti);
            Assert.That(oyuncu.ArkadasDavetiAcik, Is.True);
            oyuncu.EnYuksekBolum = P.TamSayi(MetaParametreAdlari.AcilisGorevler);
            Assert.That(oyuncu.GorevlerAcik, Is.True);
        }
    }
}
