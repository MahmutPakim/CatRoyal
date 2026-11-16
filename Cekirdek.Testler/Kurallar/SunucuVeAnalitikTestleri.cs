using System;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>Sunucu arayüzü, yerel sahte sunucu ve analitik çıkışı (yol haritası 0.2, 0.3).</summary>
    [TestFixture]
    [Category("Kural")]
    public class SunucuVeAnalitikTestleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static (YerelSahteSunucu Sunucu, AyarliSaat Saat) Kur()
        {
            var saat = new AyarliSaat(Simdi);
            return (new YerelSahteSunucu(saat), saat);
        }

        private static OlayTakvimKaydi Yaris(string kimlik = "yaris_1", double saat = 24) =>
            new OlayTakvimKaydi(kimlik, OlayTuru.HizYarisi, Simdi, Simdi + TimeSpan.FromHours(saat));

        // ------------------------------------------------------------------ Yapılandırma ve etiketler

        [Test]
        [Property("GDD", "8.8.1a, 9.3.7, 9.4.1, 9.5.8, 9.5.9")]
        public void Sunucu_ayarlari_oyuncuya_ve_etiket_tablosuna_uygulanir()
        {
            var (sunucu, _) = Kur();
            sunucu.Ayarlar.GunlukOdulKapali = true;
            sunucu.Ayarlar.HileKorumasiAcik = false;
            sunucu.Ayarlar.YuksekFiyatDuzeyi = true;
            sunucu.Ayarlar.OneCekilenKapilar[MetaParametreAdlari.AcilisSezonHatti] = 1;
            sunucu.EtiketDegistir(3, ZorlukEtiketi.CokZor);

            var oyuncu = new Oyuncu(P, Simdi);
            var etiketler = new ZorlukEtiketTablosu();
            Assert.That(oyuncu.SezonHattiAcik, Is.False);

            Assert.That(SunucuEsitleyici.Uygula(sunucu, oyuncu, etiketler), Is.True);

            Assert.That(oyuncu.GunlukOdul.Acik(500), Is.False);
            Assert.That(oyuncu.GunlukOdul.HileKorumasiAcik, Is.False);
            Assert.That(oyuncu.Magaza.YuksekFiyatDuzeyi, Is.True);
            Assert.That(oyuncu.SezonHattiAcik, Is.True);
            Assert.That(etiketler.Etiket(3), Is.EqualTo(ZorlukEtiketi.CokZor));
            Assert.That(etiketler.Etiket(4), Is.EqualTo(ZorlukEtiketi.Normal));
        }

        [Test]
        public void Sunucuya_ulasilamazsa_hicbir_sey_degismez()
        {
            var (sunucu, _) = Kur();
            sunucu.Ayarlar.YuksekFiyatDuzeyi = true;
            sunucu.EtiketDegistir(3, ZorlukEtiketi.Zor);
            sunucu.OlayEkle(Yaris());
            sunucu.Cevrimdisi = true;

            var oyuncu = new Oyuncu(P, Simdi);
            var etiketler = new ZorlukEtiketTablosu();
            Assert.That(SunucuEsitleyici.Uygula(sunucu, oyuncu, etiketler), Is.False);
            Assert.That(oyuncu.Magaza.YuksekFiyatDuzeyi, Is.False);
            Assert.That(etiketler.Etiket(3), Is.EqualTo(ZorlukEtiketi.Normal));

            Assert.That(sunucu.OlayTakvimi().Basarili, Is.False);
            Assert.That(sunucu.GrubaKatil("yaris_1", "ben", 5).Basarili, Is.False);
            Assert.That(() => sunucu.OlayTakvimi().Deger, Throws.InvalidOperationException);
        }

        [Test]
        public void Sunucunun_degistirdigi_parametreler_tabloya_uygulanir_ve_taninmayan_ad_atlanir()
        {
            var ayarlar = new SunucuYapilandirmasi();
            ayarlar.ParametreDegerleri[MetaParametreAdlari.AzamiCan] = "7";
            ayarlar.ParametreDegerleri["gelecekteki_parametre"] = "1";

            var tablo = ayarlar.ParametreleriUygula(P);
            Assert.That(tablo.TamSayi(MetaParametreAdlari.AzamiCan), Is.EqualTo(7));
            Assert.That(new Oyuncu(tablo, Simdi).Can.Azami, Is.EqualTo(7));
            Assert.That(P.TamSayi(MetaParametreAdlari.AzamiCan), Is.Not.EqualTo(7), "Asıl tablo değişmez.");
        }

        [Test]
        [Property("GDD", "9.5.4")]
        public void Gunluk_odul_sunucu_saatiyle_calisir()
        {
            var (sunucu, saat) = Kur();
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 500 };

            Assert.That(oyuncu.GunlukOdul.Al(sunucu.SimdiUtc, 500), Is.True);
            Assert.That(oyuncu.GunlukOdul.Alinabilir(sunucu.SimdiUtc, 500), Is.False);
            saat.Ilerlet(TimeSpan.FromDays(1));
            Assert.That(oyuncu.GunlukOdul.Alinabilir(sunucu.SimdiUtc, 500), Is.True);
        }

        // ------------------------------------------------------------------ Olay takvimi

        [Test]
        [Property("GDD", "10.0.1, 10.0.2")]
        public void Takvim_olaylari_baslangic_sirasiyla_verir_ve_kapatilan_olay_surmez()
        {
            var (sunucu, saat) = Kur();
            var sonraki = new OlayTakvimKaydi("toplama_2", OlayTuru.BasamakliToplama, Simdi + TimeSpan.FromDays(2), Simdi + TimeSpan.FromDays(5));
            sunucu.OlayEkle(sonraki);
            sunucu.OlayEkle(Yaris());

            var takvim = sunucu.OlayTakvimi().Deger;
            Assert.That(takvim.Select(o => o.Kimlik), Is.EqualTo(new[] { "yaris_1", "toplama_2" }));
            Assert.That(takvim[0].Suruyor(sunucu.SimdiUtc), Is.True);
            Assert.That(takvim[1].Suruyor(sunucu.SimdiUtc), Is.False, "Henüz başlamadı.");

            takvim[0].Kapali = true;
            Assert.That(sunucu.OlayTakvimi().Deger[0].Suruyor(sunucu.SimdiUtc), Is.False);

            saat.Ilerlet(TimeSpan.FromDays(5));
            Assert.That(sonraki.Suruyor(sunucu.SimdiUtc), Is.False, "Bitiş anı sürenin dışındadır.");

            Assert.That(() => sunucu.OlayEkle(Yaris()), Throws.ArgumentException);
            Assert.That(() => new OlayTakvimKaydi("x", OlayTuru.HizYarisi, Simdi, Simdi), Throws.ArgumentException);
        }

        // ------------------------------------------------------------------ Grup ve sıralama

        [Test]
        [Property("GDD", "10.1.2, 10.1.8")]
        public void Gruba_katilan_oyuncu_botlarla_dolu_ayni_grubu_alir()
        {
            var (sunucu, _) = Kur();
            sunucu.OlayEkle(Yaris());

            var grup = sunucu.GrubaKatil("yaris_1", "ben", 5).Deger;
            Assert.That(grup.Siralama.Count, Is.EqualTo(5));
            Assert.That(grup.Siralama.Count(u => u.Bot), Is.EqualTo(4));
            Assert.That(grup.Sira("ben"), Is.EqualTo(1), "Herkes 0 puandayken eşitlik oyuncunun lehine bozulur.");
            Assert.That(grup.Sira("yok"), Is.EqualTo(0));

            var yine = sunucu.GrubaKatil("yaris_1", "ben", 50).Deger;
            Assert.That(yine.GrupKimligi, Is.EqualTo(grup.GrupKimligi));
            Assert.That(yine.Siralama.Count, Is.EqualTo(5), "İkinci istek yeni grup kurmaz.");

            Assert.That(sunucu.GrubaKatil("yok", "ben", 5).Basarili, Is.False);
            Assert.That(sunucu.GrubaKatil("yaris_1", "baskasi", 0).Basarili, Is.False);
        }

        [Test]
        [Property("GDD", "10.1.4")]
        public void Puan_gonderilince_liste_yenilenir_ve_botlar_zamanla_ilerler()
        {
            var (sunucu, saat) = Kur();
            sunucu.OlayEkle(Yaris());
            sunucu.GrubaKatil("yaris_1", "ben", 5);

            saat.Ilerlet(TimeSpan.FromHours(2));
            var liste = sunucu.Siralama("yaris_1", "ben").Deger;
            Assert.That(liste.Siralama.Where(u => u.Bot).All(u => u.Puan >= 2), Is.True, "Her bot saatte en az 1 puan toplar.");
            Assert.That(liste.Sira("ben"), Is.EqualTo(5));
            Assert.That(liste.Siralama.Select(u => u.Puan), Is.Ordered.Descending);

            liste = sunucu.PuanGonder("yaris_1", "ben", 1000).Deger;
            Assert.That(liste.Sira("ben"), Is.EqualTo(1));
            Assert.That(liste.Siralama[0].Puan, Is.EqualTo(1000));

            Assert.That(sunucu.PuanGonder("yaris_1", "katilmayan", 5).Basarili, Is.False);
            Assert.That(sunucu.PuanGonder("yaris_1", "ben", -1).Basarili, Is.False);
        }

        [Test]
        [Property("GDD", "10.1.7")]
        public void Olay_bitince_puan_sayilmaz_botlar_durur_ve_son_liste_alinabilir()
        {
            var (sunucu, saat) = Kur();
            sunucu.OlayEkle(Yaris(saat: 10));
            sunucu.GrubaKatil("yaris_1", "ben", 4);
            sunucu.PuanGonder("yaris_1", "ben", 3);

            saat.Ilerlet(TimeSpan.FromHours(10));
            Assert.That(sunucu.PuanGonder("yaris_1", "ben", 3).Basarili, Is.False);
            Assert.That(sunucu.GrubaKatil("yaris_1", "gec_kalan", 4).Basarili, Is.False);

            var son = sunucu.Siralama("yaris_1", "ben").Deger;
            saat.Ilerlet(TimeSpan.FromDays(3));
            var dahaSonra = sunucu.Siralama("yaris_1", "ben").Deger;
            Assert.That(dahaSonra.Siralama.Select(u => u.Puan), Is.EqualTo(son.Siralama.Select(u => u.Puan)));
            Assert.That(dahaSonra.Siralama.Single(u => !u.Bot).Puan, Is.EqualTo(3));

            // Bağlantı kopukken son liste alınamaz; bağlantı gelince alınır.
            sunucu.Cevrimdisi = true;
            Assert.That(sunucu.Siralama("yaris_1", "ben").Basarili, Is.False);
            sunucu.Cevrimdisi = false;
            Assert.That(sunucu.Siralama("yaris_1", "ben").Basarili, Is.True);
        }

        [Test]
        public void Sahte_sunucu_belirlenimlidir()
        {
            OlayGrubu Calistir()
            {
                var (sunucu, saat) = Kur();
                sunucu.OlayEkle(Yaris());
                sunucu.GrubaKatil("yaris_1", "ben", 8);
                saat.Ilerlet(TimeSpan.FromHours(7));
                return sunucu.PuanGonder("yaris_1", "ben", 12).Deger;
            }

            var a = Calistir();
            var b = Calistir();
            Assert.That(a.GrupKimligi, Is.EqualTo(b.GrupKimligi));
            Assert.That(a.Siralama.Select(u => u.Kimlik + ":" + u.Puan), Is.EqualTo(b.Siralama.Select(u => u.Kimlik + ":" + u.Puan)));
            Assert.That(a.Siralama.Where(u => u.Bot).Select(u => u.Puan).Distinct().Count(), Is.GreaterThan(1), "Botların hızı birbirinden farklıdır.");
        }

        // ------------------------------------------------------------------ Analitik

        [Test]
        [Property("GDD", "9.2.5, 9.2.7")]
        public void Her_harcama_adiyla_analitige_bildirilir()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            var analitik = new BellekAnalitigi();
            oyuncu.Cuzdan.Ekle(10_000);
            using (AnalitikBaglayici.Bagla(oyuncu, analitik))
            {
                Assert.That(oyuncu.Magaza.SatinAl(TakviyeTuru.Bomba), Is.True);
                Assert.That(oyuncu.Cuzdan.Harca(25, "can_dolum_teklifi"), Is.True);

                Assert.That(analitik.Olaylar.Select(o => o.Ad), Is.All.EqualTo(AnalitikOlaylari.ParaHarcandi));
                Assert.That(analitik.Olaylar.Select(o => o.Alanlar["ad"]), Is.EqualTo(new object[] { "takviye:Bomba", "can_dolum_teklifi" }));
                Assert.That(analitik.Olaylar[0].Alanlar["miktar"], Is.EqualTo(oyuncu.Magaza.Fiyat(TakviyeTuru.Bomba)));
                Assert.That(analitik.Olaylar[1].Alanlar["miktar"], Is.EqualTo(25));
                Assert.That(analitik.Olaylar[1].Alanlar["bakiye"], Is.EqualTo(oyuncu.Cuzdan.Bakiye));
            }

            Assert.That(oyuncu.Cuzdan.Harca(1, "baglanti_koptuktan_sonra"), Is.True);
            Assert.That(analitik.Olaylar.Count, Is.EqualTo(2), "Bağlantı koparılınca bildirim durur.");
        }

        [Test]
        [Property("GDD", "9.2.5")]
        public void Yetersiz_bakiyede_harcama_degil_magaza_onerisi_bildirilir()
        {
            var oyuncu = new Oyuncu(P, Simdi);
            var analitik = new BellekAnalitigi();
            AnalitikBaglayici.Bagla(oyuncu, analitik);
            int bakiye = oyuncu.Cuzdan.Bakiye;

            Assert.That(oyuncu.Cuzdan.Harca(bakiye + 40, "pahali"), Is.False);

            Assert.That(analitik.Olaylar.Count, Is.EqualTo(1));
            Assert.That(analitik.Olaylar[0].Ad, Is.EqualTo(AnalitikOlaylari.ParaMagazasiOnerildi));
            Assert.That(analitik.Olaylar[0].Alanlar["eksik"], Is.EqualTo(40));
            Assert.That(analitik.Olaylar[0].Alanlar["bakiye"], Is.EqualTo(bakiye));
        }
    }
}
