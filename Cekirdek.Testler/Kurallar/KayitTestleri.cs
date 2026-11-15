using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>Oyuncu durumunun kaydedilip yüklenmesi (yol haritası 0.1; GDD 9.1.5, 9.2.8, 9.3.10).</summary>
    [TestFixture]
    [Category("Kural")]
    public class KayitTestleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static TimeSpan Dolum => TimeSpan.FromMinutes(P.Sayi(MetaParametreAdlari.CanDolumSuresiDk));

        /// <summary>Kaydedilen her sisteme dokunulmuş bir oyuncu.</summary>
        private static Oyuncu DoluOyuncu()
        {
            var o = new Oyuncu(P, Simdi) { EnYuksekBolum = 500, Yildiz = 3 };

            var paket = new OdulPaketi { Para = 1234, Yildiz = 20 };
            paket.Takviyeler[TakviyeTuru.Bomba] = 3;
            paket.Takviyeler[TakviyeTuru.Karistirma] = 1;
            paket.TakviyeSinirsizSaniye[TakviyeTuru.Cizgi] = 900;
            o.OdulVer(paket, Simdi);
            o.Envanter.SinirsizEkle(TakviyeTuru.Satir, 600, Simdi);
            o.Envanter.SinirsiziDuraklat(TakviyeTuru.Satir, Simdi + TimeSpan.FromMinutes(1));

            o.Can.BolumKaybi(Simdi);
            o.Can.BolumKaybi(Simdi);
            o.Magaza.YuksekFiyatDuzeyi = true;
            o.Kapilar.OneCek(MetaParametreAdlari.AcilisMagaza, 2);
            o.HesapBagla();

            Assert.That(o.GunlukOdul.Al(Simdi, o.EnYuksekBolum), Is.True);
            Assert.That(o.GunlukOdul.Al(Simdi + TimeSpan.FromDays(1), o.EnYuksekBolum), Is.True);

            o.SezonHatti.PuanEkle(9);
            o.SezonPremiumSatinAl(Simdi);
            Assert.That(o.SezonHatti.UcretsizOduluAl(0), Is.True);
            Assert.That(o.SezonHatti.PremiumOduluAl(1), Is.True);
            o.HaftalikHat.PuanEkle(3);

            o.Carpan.BiletKullan(Simdi);
            o.Zorluk.Beceri = BeceriDuzeyi.Usta;
            o.Zorluk.BasarisizlikKaydet(7);
            o.Zorluk.BasarisizlikKaydet(7);
            o.Zorluk.BasarisizlikKaydet(9);

            o.Alan.GorevleriYukle(new[]
            {
                new AlanGorevi("zemin", 1),
                new AlanGorevi("duvar", 2, "zemin"),
                new AlanGorevi("çatı \"kırmızı\"", 3, "duvar"),
            });
            o.Yildiz -= o.Alan.InsaEt("zemin", o.Yildiz);

            o.Album.KartEkle(0, 0);
            o.Album.KartEkle(0, 1);
            o.Album.KartEkle(2, 3);
            o.Album.KartEkle(2, 3);
            return o;
        }

        [Test]
        [Property("GDD", "9.1.5, 9.2.8, 9.3.10")]
        public void Kaydedilen_oyuncu_yuklenince_ayni_durumdadir()
        {
            var o = DoluOyuncu();
            string kayit = OyuncuKaydi.Yaz(o);
            var y = OyuncuKaydi.Yukle(kayit, P, Simdi + TimeSpan.FromSeconds(5));
            var an = Simdi + TimeSpan.FromSeconds(5);

            Assert.That(OyuncuKaydi.Yaz(y), Is.EqualTo(kayit), "Yükle → yaz aynı metni verir.");

            Assert.That(y.EnYuksekBolum, Is.EqualTo(500));
            Assert.That(y.Yildiz, Is.EqualTo(o.Yildiz));
            Assert.That(y.HesapBagli, Is.True);
            Assert.That(y.Cuzdan.Bakiye, Is.EqualTo(o.Cuzdan.Bakiye));
            Assert.That(y.Magaza.YuksekFiyatDuzeyi, Is.True);
            Assert.That(y.Kapilar.AcilisBolumu(MetaParametreAdlari.AcilisMagaza), Is.EqualTo(2));

            Assert.That(y.Can.Premium, Is.True);
            Assert.That(y.Can.Mevcut(an), Is.EqualTo(o.Can.Mevcut(an)));
            Assert.That(y.Can.TamDolacagiAn, Is.EqualTo(o.Can.TamDolacagiAn));

            Assert.That(y.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(3));
            Assert.That(y.Envanter.Adet(TakviyeTuru.Karistirma), Is.EqualTo(1));
            Assert.That(y.Envanter.SinirsizAktif(TakviyeTuru.Cizgi, Simdi + TimeSpan.FromSeconds(899)), Is.True);
            Assert.That(y.Envanter.SinirsizAktif(TakviyeTuru.Cizgi, Simdi + TimeSpan.FromSeconds(900)), Is.False);

            Assert.That(y.GunlukOdul.HaftalikSeri, Is.EqualTo(2));
            Assert.That(y.GunlukOdul.AylikSeri, Is.EqualTo(2));
            Assert.That(y.GunlukOdul.SonAlinanGun, Is.EqualTo(o.GunlukOdul.SonAlinanGun));
            Assert.That(y.GunlukOdul.Alinabilir(Simdi + TimeSpan.FromDays(1), 500), Is.False, "Aynı gün ikinci kez alınamaz.");

            Assert.That(y.SezonHatti.Basamak, Is.EqualTo(o.SezonHatti.Basamak));
            Assert.That(y.SezonHatti.BasamakPuani, Is.EqualTo(o.SezonHatti.BasamakPuani));
            Assert.That(y.SezonHatti.Premium, Is.True);
            Assert.That(y.SezonHatti.UcretsizOdulAlinabilir(0), Is.False, "Alınmış ödül yeniden alınamaz.");
            Assert.That(y.SezonHatti.UcretsizOdulAlinabilir(1), Is.True);
            Assert.That(y.SezonHatti.PremiumOdulAlinabilir(1), Is.False);
            Assert.That(y.SezonHatti.PremiumOdulAlinabilir(0), Is.True);
            Assert.That(y.HaftalikHat.BasamakPuani + y.HaftalikHat.Basamak, Is.GreaterThan(0));

            Assert.That(y.Carpan.AktifCarpan(an), Is.EqualTo(o.Carpan.AktifCarpan(an)));
            Assert.That(y.Carpan.AktifCarpan(an), Is.GreaterThan(1));
            Assert.That(y.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Usta));
            Assert.That(y.Zorluk.DenemeSayaci(7), Is.EqualTo(2));
            Assert.That(y.Zorluk.DenemeSayaci(9), Is.EqualTo(1));

            Assert.That(y.Alan.Gorevler.Count, Is.EqualTo(3));
            Assert.That(y.Alan.InsaEdildi("zemin"), Is.True);
            Assert.That(y.Alan.InsaEdildi("duvar"), Is.False);
            Assert.That(y.Alan.InsaEt("çatı \"kırmızı\"", 99), Is.EqualTo(0), "Önkoşullar kayıttan sonra da geçerlidir.");
            Assert.That(y.Alan.InsaEt("duvar", 99), Is.EqualTo(2));

            Assert.That(y.Album.CiftKartSayisi, Is.EqualTo(1));
            Assert.That(y.Album.KartEkle(0, 1), Is.False, "Kayıttaki kart yeniden gelirse çift sayılır.");
            Assert.That(y.Album.KartEkle(0, 2), Is.True);
        }

        [Test]
        [Property("GDD", "9.1.5")]
        public void Uygulama_kapaliyken_gecen_sure_canlara_sayilir()
        {
            var o = new Oyuncu(P, Simdi);
            o.Can.BolumKaybi(Simdi);
            o.Can.BolumKaybi(Simdi);
            string kayit = OyuncuKaydi.Yaz(o);

            Assert.That(OyuncuKaydi.Yukle(kayit, P, Simdi).Can.Mevcut(Simdi), Is.EqualTo(o.Can.Azami - 2));
            var sonra = Simdi + Dolum;
            Assert.That(OyuncuKaydi.Yukle(kayit, P, sonra).Can.Mevcut(sonra), Is.EqualTo(o.Can.Azami - 1));
            sonra = Simdi + Dolum + Dolum + Dolum;
            Assert.That(OyuncuKaydi.Yukle(kayit, P, sonra).Can.Mevcut(sonra), Is.EqualTo(o.Can.Azami));
        }

        [Test]
        [Property("GDD", "9.1.7, 9.3.6")]
        public void Sinirsiz_sureler_ve_duraklatilmis_sureler_korunur()
        {
            var o = new Oyuncu(P, Simdi);
            o.Can.SinirsizEkle(TimeSpan.FromMinutes(30), Simdi);
            o.Envanter.SinirsizEkle(TakviyeTuru.Bomba, 600, Simdi);
            o.Envanter.SinirsiziDuraklat(TakviyeTuru.Bomba, Simdi + TimeSpan.FromMinutes(4));

            var y = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(o), P, Simdi);
            Assert.That(y.Can.SinirsizKalan(Simdi + TimeSpan.FromMinutes(10)), Is.EqualTo(TimeSpan.FromMinutes(20)));
            Assert.That(y.Can.SinirsizAktif(Simdi + TimeSpan.FromMinutes(31)), Is.False, "Kapalıyken de süre akar.");

            var gec = Simdi + TimeSpan.FromDays(3);
            Assert.That(y.Envanter.SinirsizAktif(TakviyeTuru.Bomba, gec), Is.False, "Duraklatılmış süre akmaz.");
            y.Envanter.SinirsiziSurdur(TakviyeTuru.Bomba, gec);
            Assert.That(y.Envanter.SinirsizAktif(TakviyeTuru.Bomba, gec + TimeSpan.FromSeconds(359)), Is.True);
            Assert.That(y.Envanter.SinirsizAktif(TakviyeTuru.Bomba, gec + TimeSpan.FromSeconds(360)), Is.False);

            o.Can.SinirsiziDuraklat(Simdi + TimeSpan.FromMinutes(10));
            y = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(o), P, gec);
            Assert.That(y.Can.SinirsizAktif(gec), Is.False);
            Assert.That(y.Can.SinirsizKalan(gec), Is.EqualTo(TimeSpan.FromMinutes(20)));
        }

        [Test]
        public void Eksik_alanli_eski_kayit_varsayilanlarla_yuklenir()
        {
            var y = OyuncuKaydi.Yukle("{\"surum\":1,\"kazanma_serisi\":4,\"en_yuksek_bolum\":12}", P, Simdi);
            var yeni = new Oyuncu(P, Simdi);

            Assert.That(y.KazanmaSerisi, Is.EqualTo(4));
            Assert.That(y.EnYuksekBolum, Is.EqualTo(12));
            Assert.That(y.Cuzdan.Bakiye, Is.EqualTo(yeni.Cuzdan.Bakiye), "Bakiye yoksa başlangıç parası (GDD 9.2.1).");
            Assert.That(y.Can.Mevcut(Simdi), Is.EqualTo(yeni.Can.Azami));
            Assert.That(y.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(0));
            Assert.That(y.SezonHatti.Basamak, Is.EqualTo(0));
        }

        [Test]
        public void Taninmayan_takviye_turu_ve_fazladan_alan_atlanir()
        {
            string kayit = "{\"surum\":1,\"gelecekteki_alan\":[1,2],"
                + "\"envanter\":{\"adet\":{\"Bomba\":2,\"IsinKilici\":5,\"99\":1}}}";
            var y = OyuncuKaydi.Yukle(kayit, P, Simdi);
            Assert.That(y.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(2));
        }

        [Test]
        public void Kayittaki_degerler_parametre_sinirlarina_sigdirilir()
        {
            int azami = P.TamSayi(MetaParametreAdlari.TakviyeAzamiAdet);
            string kayit = "{\"surum\":1,\"bakiye\":-50,\"yildiz\":-3,"
                + "\"envanter\":{\"adet\":{\"Bomba\":" + (azami + 10) + "}},"
                + "\"sezon_hatti\":{\"basamak\":100000,\"alinan_ucretsiz\":[0,999999]}}";
            var y = OyuncuKaydi.Yukle(kayit, P, Simdi);

            Assert.That(y.Cuzdan.Bakiye, Is.EqualTo(0));
            Assert.That(y.Yildiz, Is.EqualTo(0));
            Assert.That(y.Envanter.Adet(TakviyeTuru.Bomba), Is.EqualTo(azami));
            Assert.That(y.SezonHatti.Basamak, Is.EqualTo(y.SezonHatti.BasamakSayisi));
            Assert.That(y.SezonHatti.UcretsizOdulAlinabilir(0), Is.False);
        }

        [TestCase("")]
        [TestCase("{\"surum\":1")]
        [TestCase("[1,2,3]")]
        [TestCase("{}")]
        [TestCase("{\"surum\":999}")]
        [TestCase("{\"surum\":0}")]
        [TestCase("{\"surum\":\"bir\"}")]
        [TestCase("{\"surum\":1,\"bakiye\":\"çok\"}")]
        [TestCase("{\"surum\":1,\"bakiye\":1.5}")]
        [TestCase("{\"surum\":1,\"can\":7}")]
        [TestCase("{\"surum\":1,\"can\":{\"tam_dolacagi_an\":\"dün\"}}")]
        public void Bozuk_ya_da_daha_yeni_surumlu_kayit_reddedilir(string kayit)
        {
            Assert.That(() => OyuncuKaydi.Yukle(kayit, P, Simdi), Throws.TypeOf<KayitHatasi>());
        }

        [Test]
        public void Yukleme_para_olayi_tetiklemez_ve_harcama_kaydi_olusturmaz()
        {
            var o = new Oyuncu(P, Simdi);
            o.Cuzdan.Ekle(500);
            Assert.That(o.Cuzdan.Harca(100, "deneme"), Is.True);

            var y = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(o), P, Simdi);
            Assert.That(y.Cuzdan.Bakiye, Is.EqualTo(o.Cuzdan.Bakiye));
            Assert.That(y.Cuzdan.Harcamalar, Is.Empty);
        }

        [Test]
        public void Json_yazici_ayristiriciyla_cift_yonludur()
        {
            var nesne = new Dictionary<string, object?>
            {
                ["metin"] = "tırnak \" ters \\ satır \n sekme \t zil \u0007 kedi 🐈",
                ["tam"] = -42,
                ["uzun"] = 6_000_000_000L,
                ["kesir"] = 0.1,
                ["evet"] = true,
                ["bos"] = null,
                ["dizi"] = new List<object?> { 1, "iki", new Dictionary<string, object?>() },
            };

            string metin = Json.Yaz(nesne);
            var okunan = (Dictionary<string, object?>)Json.Ayristir(metin)!;

            Assert.That(okunan["metin"], Is.EqualTo(nesne["metin"]));
            Assert.That(okunan["tam"], Is.EqualTo(-42d));
            Assert.That(okunan["uzun"], Is.EqualTo(6e9));
            Assert.That(okunan["kesir"], Is.EqualTo(0.1));
            Assert.That(okunan["evet"], Is.EqualTo(true));
            Assert.That(okunan["bos"], Is.Null);
            Assert.That(((List<object?>)okunan["dizi"]!).Count, Is.EqualTo(3));
            Assert.That(Json.Yaz(okunan["dizi"]), Is.EqualTo("[1,\"iki\",{}]"));
        }
    }
}
