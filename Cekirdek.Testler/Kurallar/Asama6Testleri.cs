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
    /// <summary>
    /// Yol haritası Aşama 6: takımlar (GDD 10.7, 9.1.6, 9.1.8), takım yarışı (10.3), albüm kart gönderme ve isteme
    /// (9.7.9), ortaklı atış (10.6).
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama6Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        /// <summary>Aynı sunucuları paylaşan oyuncular.</summary>
        private sealed class Dunya
        {
            public Dunya(ParametreTablosu? p = null)
            {
                Parametreler = p ?? P;
                Saat = new AyarliSaat(Simdi);
                Sunucu = new YerelSahteSunucu(Saat);
                Sosyal = new YerelSahteSosyalSunucu(Saat, Parametreler);
            }

            public ParametreTablosu Parametreler { get; }
            public AyarliSaat Saat { get; }
            public YerelSahteSunucu Sunucu { get; }
            public YerelSahteSosyalSunucu Sosyal { get; }
            public DateTime An => Saat.SimdiUtc;

            public Oyuncu Oyuncu(string kimlik, int enYuksekBolum = 500)
            {
                var oyuncu = new Oyuncu(Parametreler, Simdi) { EnYuksekBolum = enYuksekBolum, Kimlik = kimlik };
                oyuncu.Cuzdan.Ekle(10_000);
                return oyuncu;
            }

            /// <summary>"lider" bir takım kurar; verilen oyuncular katılır.</summary>
            public Oyuncu TakimKur(bool acik = true, params Oyuncu[] uyeler)
            {
                var lider = Oyuncu("lider");
                Assert.That(lider.Takim.Kur(Sosyal, "Kediler", "pati", acik), Is.True);
                foreach (var uye in uyeler)
                {
                    Assert.That(uye.Takim.Katil(Sosyal, lider.Takim.TakimKimligi!), Is.True);
                }

                return lider;
            }

            public void Ilerle(Oyuncu oyuncu) => oyuncu.Olaylar.Ilerle(Sunucu, An, Sosyal);
        }

        private const string KazanilanBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

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

        // ------------------------------------------------------------------ 6.1 Takımlar

        [Test]
        [Property("GDD", "10.7.1, 10.7.2, 9.2.5")]
        public void Takim_bolumu_acilis_bolumunde_acilir_ve_takim_kurmak_para_ister()
        {
            var d = new Dunya();
            int acilis = Tam(MetaParametreAdlari.AcilisTakim);
            int bedel = Tam(MetaParametreAdlari.TakimKurmaBedeli);

            var erken = d.Oyuncu("erken", acilis - 1);
            Assert.That(erken.Takim.Durum, Is.EqualTo(TakimDurumu.Kilitli));
            Assert.That(erken.Takim.Kur(d.Sosyal, "Erken", "x", true), Is.False);

            var ben = d.Oyuncu("ben", acilis);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Takimsiz));
            int para = ben.Cuzdan.Bakiye;

            // Bağlantı yokken takım kurulmaz ve para harcanmaz.
            d.Sosyal.Cevrimdisi = true;
            Assert.That(ben.Takim.Kur(d.Sosyal, "Kediler", "pati", true), Is.False);
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para));
            d.Sosyal.Cevrimdisi = false;

            Assert.That(ben.Takim.Kur(d.Sosyal, "Kediler", "pati", acik: false), Is.True);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Lider));
            Assert.That(ben.Takim.Uye, Is.True);
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para - bedel));
            Assert.That(ben.Cuzdan.Harcamalar.Single().Ad, Is.EqualTo("takim_kurma"));
            Assert.That(ben.Takim.Kur(d.Sosyal, "İkinci", "x", true), Is.False, "Takımdayken yeni takım kurulmaz.");

            var takim = d.Sosyal.TakimDurumu("ben").Deger.Takim!;
            Assert.That(takim.Ad, Is.EqualTo("Kediler"));
            Assert.That(takim.Simge, Is.EqualTo("pati"));
            Assert.That(takim.Acik, Is.False);

            // Yetersiz bakiye: takım kurulmaz, para mağazası önerilir.
            var pahali = new Dunya(P.Degistir(MetaParametreAdlari.TakimKurmaBedeli, 999_999));
            var yoksul = pahali.Oyuncu("yoksul");
            int onerilenEksik = 0;
            yoksul.Cuzdan.ParaMagazasiOnerildi += eksik => onerilenEksik = eksik;
            Assert.That(yoksul.Takim.Kur(pahali.Sosyal, "Pahalı", "x", true), Is.False);
            Assert.That(onerilenEksik, Is.GreaterThan(0));
            Assert.That(pahali.Sosyal.TakimDurumu("yoksul").Deger.Durum, Is.EqualTo(TakimDurumu.Takimsiz));
        }

        [Test]
        [Property("GDD", "10.7.2")]
        public void Acik_takima_dogrudan_katilinir_kapali_takima_istek_gonderilir_ve_lider_onaylar()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var lider = d.TakimKur(acik: true, ben);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Uye));
            Assert.That(ben.Takim.TakimKimligi, Is.EqualTo(lider.Takim.TakimKimligi));
            Assert.That(ben.Takim.Ayril(d.Sosyal), Is.True);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Takimsiz));
            Assert.That(lider.Takim.Ayril(d.Sosyal), Is.True);

            // Kapalı takım: istek → onay.
            var kapaliLider = d.TakimKur(acik: false);
            string takim = kapaliLider.Takim.TakimKimligi!;
            Assert.That(ben.Takim.Katil(d.Sosyal, takim), Is.True);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.IstekBekliyor));
            Assert.That(ben.Takim.Uye, Is.False);
            Assert.That(d.Sosyal.IstegiYanitla("ben", "ben", true).Basarili, Is.False, "İsteği yalnızca yetkili yanıtlar.");
            Assert.That(d.Sosyal.IstegiYanitla("lider", "ben", true).Basarili, Is.True);
            Assert.That(ben.Takim.Esitle(d.Sosyal), Is.True);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Uye));

            // Ret ve iptal takımsız duruma döndürür.
            var ayse = d.Oyuncu("ayse");
            ayse.Takim.Katil(d.Sosyal, takim);
            d.Sosyal.IstegiYanitla("lider", "ayse", false);
            ayse.Takim.Esitle(d.Sosyal);
            Assert.That(ayse.Takim.Durum, Is.EqualTo(TakimDurumu.Takimsiz));

            ayse.Takim.Katil(d.Sosyal, takim);
            Assert.That(ayse.Takim.IstegiIptalEt(d.Sosyal), Is.True);
            Assert.That(ayse.Takim.Durum, Is.EqualTo(TakimDurumu.Takimsiz));
            Assert.That(ayse.Takim.IstegiIptalEt(d.Sosyal), Is.False);
        }

        [Test]
        [Property("GDD", "10.7.4")]
        public void Liderler_ve_yardimci_liderler_uye_atar_ve_lider_yetki_verir()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var ali = d.Oyuncu("ali");
            var can = d.Oyuncu("can");
            d.TakimKur(acik: true, ben, ali, can);

            Assert.That(d.Sosyal.UyeAt("ben", "ali").Basarili, Is.False, "Üye kimseyi atamaz.");
            Assert.That(d.Sosyal.YetkiVer("ben", "ali", true).Basarili, Is.False, "Yetkiyi yalnızca lider verir.");

            // Yetki alan üye yardımcı lider olur ve üye atabilir.
            Assert.That(d.Sosyal.YetkiVer("lider", "ben", true).Basarili, Is.True);
            d.Sosyal.YetkiVer("lider", "can", true);
            ben.Takim.Esitle(d.Sosyal);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.YardimciLider));
            Assert.That(d.Sosyal.UyeAt("ben", "can").Basarili, Is.False, "Yardımcı lider başka bir yardımcı lideri atamaz.");
            Assert.That(d.Sosyal.UyeAt("ben", "lider").Basarili, Is.False, "Lider atılamaz.");
            Assert.That(d.Sosyal.UyeAt("ben", "ali").Basarili, Is.True);
            ali.Takim.Esitle(d.Sosyal);
            Assert.That(ali.Takim.Durum, Is.EqualTo(TakimDurumu.Takimsiz), "Atılan oyuncu takımsız duruma döner.");

            // Yetkisi alınan üyeye döner; liderlik devredilince lider olur.
            d.Sosyal.YetkiVer("lider", "ben", false);
            ben.Takim.Esitle(d.Sosyal);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Uye));
            Assert.That(d.Sosyal.LiderligiDevret("lider", "ben").Basarili, Is.True);
            ben.Takim.Esitle(d.Sosyal);
            Assert.That(ben.Takim.Durum, Is.EqualTo(TakimDurumu.Lider));
            Assert.That(d.Sosyal.TakimDurumu("lider").Deger.Durum, Is.EqualTo(TakimDurumu.Uye));
        }

        [Test]
        [Property("GDD", "10.7.1, 10.7.5, 10.7.8")]
        public void Takimsiz_oyuncu_takim_arar_ve_onerilen_takimlarda_etkinlik_duzeyini_gorur()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var lider = d.TakimKur(acik: true);
            var gizli = d.Oyuncu("gizli");
            gizli.Takim.Kur(d.Sosyal, "Gizli Pençe", "x", acik: false);

            Assert.That(ben.Takim.Ara(d.Sosyal).Select(t => t.Ad), Is.EqualTo(new[] { "Kediler" }), "Önerilenler yeri olan açık takımlardır.");
            Assert.That(ben.Takim.Ara(d.Sosyal, "pençe").Single().Acik, Is.False);
            Assert.That(ben.Takim.Ara(d.Sosyal).Single().Etkinlik, Is.EqualTo(EtkinlikDuzeyi.Dusuk));

            lider.Takim.SohbetGonder(d.Sosyal, "merhaba");
            lider.Takim.SohbetGonder(d.Sosyal, "kimse var mı");
            var ozet = ben.Takim.Ara(d.Sosyal).Single();
            Assert.That(ozet.Etkinlik, Is.EqualTo(EtkinlikDuzeyi.Yuksek));
            Assert.That(ozet.UyeSayisi, Is.EqualTo(1));

            // Üye sınırı sunucudadır.
            var dar = new Dunya(P.Degistir("takim_azami_uye", 1));
            var darLider = dar.TakimKur();
            Assert.That(dar.Oyuncu("ben").Takim.Katil(dar.Sosyal, darLider.Takim.TakimKimligi!), Is.False);
            Assert.That(dar.Oyuncu("ben").Takim.Ara(dar.Sosyal), Is.Empty);
        }

        [Test]
        [Property("GDD", "10.7.3, 10.7.4, 10.7.7")]
        public void Sohbette_susturulan_oyuncu_gorunmez_ve_ebeveyn_denetimi_sohbeti_kapatir()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var ali = d.Oyuncu("ali");
            var lider = d.TakimKur(acik: true, ben, ali);

            Assert.That(ben.Takim.SohbetGonder(d.Sosyal, "selam"), Is.True);
            ali.Takim.SohbetGonder(d.Sosyal, "ben de buradayım");
            Assert.That(lider.Takim.Sohbet(d.Sosyal).Select(m => m.Metin), Is.EqualTo(new[] { "selam", "ben de buradayım" }));

            lider.Takim.Sustur("ali");
            Assert.That(lider.Takim.Sohbet(d.Sosyal).Select(m => m.Gonderen), Is.EqualTo(new[] { "ben" }));
            lider.Takim.SusturmayiKaldir("ali");
            Assert.That(lider.Takim.Sohbet(d.Sosyal), Has.Count.EqualTo(2));

            ben.Takim.SohbetKapali = true;
            Assert.That(ben.Takim.Sohbet(d.Sosyal), Is.Empty);
            Assert.That(ben.Takim.SohbetGonder(d.Sosyal, "yazamam"), Is.False);
            Assert.That(d.Oyuncu("yabanci").Takim.SohbetGonder(d.Sosyal, "takımım yok"), Is.False);
        }

        [Test]
        [Property("GDD", "9.1.6, 9.1.8, 10.7.3")]
        public void Can_istegine_yardim_edene_para_isteyenin_gelen_kutusuna_can_gider()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var ali = d.Oyuncu("ali");
            d.TakimKur(acik: true, ben, ali);
            ben.Can.BolumKaybi(Simdi);
            ben.Can.BolumKaybi(Simdi);
            int can = ben.Can.Mevcut(Simdi);

            Assert.That(ben.Takim.CanIste(d.Sosyal, Simdi), Is.True);
            var istek = ali.Takim.Sohbet(d.Sosyal).Single(m => m.Tur == SohbetMesajiTuru.CanIstegi);
            Assert.That(istek.Kalan, Is.EqualTo(Tam("can_istegi_azami_yardim")));

            int para = ali.Cuzdan.Bakiye;
            Assert.That(ali.Takim.CanYardimEt(d.Sosyal, istek.Kimlik), Is.True);
            Assert.That(ali.Cuzdan.Bakiye, Is.EqualTo(para + Tam(MetaParametreAdlari.CanYardimOdulu)));
            Assert.That(ali.Takim.CanYardimEt(d.Sosyal, istek.Kimlik), Is.False, "Aynı isteğe iki kez yardım edilmez.");
            Assert.That(ben.Takim.CanYardimEt(d.Sosyal, istek.Kimlik), Is.False, "Oyuncu kendi isteğine yardım edemez.");

            // Can gelen kutusuna düşer; toplanınca sayıya eklenir.
            Assert.That(ben.GelenKutusu.Esitle(d.Sosyal), Is.EqualTo(1));
            Assert.That(ben.Can.Mevcut(Simdi), Is.EqualTo(can));
            string hediye = ben.GelenKutusu.Ogeler.Single().Kimlik;
            Assert.That(ben.GelenKutusu.Topla(hediye, Simdi), Is.True);
            Assert.That(ben.GelenKutusu.Topla(hediye, Simdi), Is.False);
            Assert.That(ben.Can.Mevcut(Simdi), Is.EqualTo(can + 1));

            // Azami can aşılmaz.
            d.Sosyal.HediyeGonder("ali", "sistem", new OdulPaketi { Can = 3 });
            ali.GelenKutusu.Esitle(d.Sosyal);
            ali.GelenKutusu.HepsiniTopla(Simdi);
            Assert.That(ali.Can.Mevcut(Simdi), Is.EqualTo(ali.Can.Azami));
        }

        [Test]
        [Property("GDD", "10.7.3, 10.7.4, 9.1.10")]
        public void Can_istegi_aralikla_sinirlidir_azami_yardimla_karsilanir_ve_yardim_meni_uygulanir()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var yardimcilar = Enumerable.Range(1, Tam("can_istegi_azami_yardim") + 1).Select(i => d.Oyuncu("uye" + i)).ToArray();
            d.TakimKur(acik: true, yardimcilar.Prepend(ben).ToArray());
            var aralik = TimeSpan.FromHours(Tam(MetaParametreAdlari.CanIstekAraligiSaat));

            Assert.That(ben.Takim.CanIsteyebilir(Simdi), Is.True);
            Assert.That(ben.Takim.CanIste(d.Sosyal, Simdi), Is.True);
            Assert.That(ben.Takim.CanIste(d.Sosyal, Simdi + aralik - TimeSpan.FromSeconds(1)), Is.False);

            // Bir istek en fazla belirli sayıda yardımla karşılanır.
            long istek = ben.Takim.Sohbet(d.Sosyal).Single().Kimlik;
            var sonuclar = yardimcilar.Select(y => y.Takim.CanYardimEt(d.Sosyal, istek)).ToList();
            Assert.That(sonuclar.Take(sonuclar.Count - 1), Is.All.True);
            Assert.That(sonuclar.Last(), Is.False);
            ben.GelenKutusu.Esitle(d.Sosyal);
            Assert.That(ben.GelenKutusu.Ogeler, Has.Count.EqualTo(Tam("can_istegi_azami_yardim")));

            // Bekleme süresi dolunca yeniden istenir; yardım meni altındaki oyuncu isteyemez.
            d.Saat.Ilerlet(aralik);
            Assert.That(ben.Takim.CanIste(d.Sosyal, d.An), Is.True);
            d.Saat.Ilerlet(aralik);
            d.Sosyal.YardimMeniUygula("ben", TimeSpan.FromHours(1));
            Assert.That(ben.Takim.CanIste(d.Sosyal, d.An), Is.False);
            Assert.That(ben.Takim.CanIsteyebilir(d.An), Is.True, "Sunucu reddettiği için istek gönderilmemiş sayılır.");
            d.Saat.Ilerlet(TimeSpan.FromHours(1));
            Assert.That(ben.Takim.CanIste(d.Sosyal, d.An), Is.True);
        }

        [Test]
        [Property("GDD", "9.1.8")]
        public void Gelen_kutusu_sinirli_sayida_mesaj_alir_fazlasi_sunucuda_bekler()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            int azami = Tam(MetaParametreAdlari.GelenKutusuAzami);
            for (int i = 0; i < azami + 5; i++)
            {
                d.Sosyal.HediyeGonder("ben", "sistem", new OdulPaketi { Para = 1 });
            }

            Assert.That(ben.GelenKutusu.Esitle(d.Sosyal), Is.EqualTo(azami));
            Assert.That(ben.GelenKutusu.Esitle(d.Sosyal), Is.EqualTo(0), "Kutu dolu.");
            int para = ben.Cuzdan.Bakiye;
            Assert.That(ben.GelenKutusu.HepsiniTopla(Simdi), Is.EqualTo(azami));
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + azami));
            Assert.That(ben.GelenKutusu.Esitle(d.Sosyal), Is.EqualTo(5), "Yer açılınca bekleyenler gelir.");
        }

        [Test]
        [Property("GDD", "10.7.6")]
        public void Takim_teklifinden_alinan_paket_takim_uyelerine_de_odul_dagitir()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var ali = d.Oyuncu("ali");
            d.TakimKur(acik: true, ben, ali);
            int para = ben.Cuzdan.Bakiye;

            Assert.That(ben.Takim.TakimTeklifiAlindi(d.Sosyal, new OdulPaketi { Para = 500 }, new OdulPaketi { Para = 50 }, Simdi), Is.True);
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + 500));
            ben.GelenKutusu.Esitle(d.Sosyal);
            Assert.That(ben.GelenKutusu.Ogeler, Is.Empty, "Satın alan kendi dağıtımından pay almaz.");

            ali.GelenKutusu.Esitle(d.Sosyal);
            Assert.That(ali.GelenKutusu.Ogeler.Single().Paket.Para, Is.EqualTo(50));
            Assert.That(d.Oyuncu("yabanci").Takim.TakimTeklifiAlindi(d.Sosyal, new OdulPaketi(), new OdulPaketi(), Simdi), Is.False);
        }

        [Test]
        [Property("GDD", "10.7")]
        public void Takim_uyeligi_ve_gelen_kutusu_kayitla_korunur()
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            d.TakimKur(acik: true, ben);
            ben.Takim.CanIste(d.Sosyal, Simdi);
            ben.Takim.Sustur("ali");
            ben.Takim.SohbetKapali = true;
            d.Sosyal.HediyeGonder("ben", "sistem", new OdulPaketi { Can = 1 });
            ben.GelenKutusu.Esitle(d.Sosyal);

            string kayit = OyuncuKaydi.Yaz(ben);
            var y = OyuncuKaydi.Yukle(kayit, P, Simdi);

            Assert.That(OyuncuKaydi.Yaz(y), Is.EqualTo(kayit));
            Assert.That(y.Takim.Durum, Is.EqualTo(TakimDurumu.Uye));
            Assert.That(y.Takim.TakimKimligi, Is.EqualTo(ben.Takim.TakimKimligi));
            Assert.That(y.Takim.Susturulanlar, Is.EquivalentTo(new[] { "ali" }));
            Assert.That(y.Takim.SohbetKapali, Is.True);
            Assert.That(y.Takim.CanIsteyebilir(Simdi), Is.False, "Son can isteğinin anı korunur.");
            Assert.That(y.GelenKutusu.Ogeler.Single().Paket.Can, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ 6.2 Takım yarışı

        private static OlayTakvimKaydi YarisKaydi(Dunya d)
        {
            var kayit = new OlayTakvimKaydi(
                "ty", OlayTuru.TakimYarisi, Simdi, Simdi + TimeSpan.FromHours(Tam("takim_yarisi_suresi_saat")));
            d.Sunucu.OlayEkle(kayit);
            return kayit;
        }

        private static TakimYarisiOlayi Yaris(Oyuncu oyuncu) => (TakimYarisiOlayi)oyuncu.Olaylar.Bul("ty")!;

        private static (Dunya D, Oyuncu Ben, OlayTakvimKaydi Kayit) YarisKur()
        {
            var d = new Dunya();
            var kayit = YarisKaydi(d);
            var ben = d.Oyuncu("ben");
            d.TakimKur(acik: true, ben);
            ben.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            HepsiniGoster(ben.Pencereler);
            d.Ilerle(ben);
            return (d, ben, kayit);
        }

        [Test]
        [Property("GDD", "10.3.1, 10.3.2")]
        public void Takim_yarisi_yalnizca_takimdaki_oyuncuya_acilir_ve_takim_bir_gruba_konur()
        {
            var d = new Dunya();
            var kayit = YarisKaydi(d);
            var ben = d.Oyuncu("ben");
            ben.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            d.Ilerle(ben);

            Assert.That(Yaris(ben).Durum(ben, Simdi), Is.EqualTo(TakimYarisiDurumu.Takimsiz));
            Kazan(ben, Simdi);
            Assert.That(Yaris(ben).Katki, Is.EqualTo(0), "Takımı olmayan oyuncu katılamaz.");
            Assert.That(d.Oyuncu("erken", Tam(MetaParametreAdlari.AcilisTakimYarisi) - 1).Olaylar.Acik(Simdi), Is.Empty);

            d.TakimKur(acik: true, ben);
            Assert.That(Yaris(ben).Durum(ben, Simdi), Is.EqualTo(TakimYarisiDurumu.Hazir));
            d.Ilerle(ben);

            Assert.That(Yaris(ben).Durum(ben, Simdi), Is.EqualTo(TakimYarisiDurumu.Yarista));
            Assert.That(Yaris(ben).Liste, Has.Count.EqualTo(Tam(MetaParametreAdlari.TakimYarisiGrupBoyutu)));
            Assert.That(Yaris(ben).Liste.Count(t => !t.Bot), Is.EqualTo(1));
            Assert.That(Yaris(ben).Sira, Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "10.3.3")]
        public void Kazanilan_bolum_hem_oyuncunun_hem_takimin_puanina_eklenir_ve_siralama_yerelde_guncellenir()
        {
            var (d, ben, _) = YarisKur();
            var olay = Yaris(ben);
            ben.Carpan.BiletKullan(Simdi);

            Kazan(ben, Simdi);
            Kazan(ben, Simdi, ZorlukEtiketi.AsiriZor);
            int puan = Tam(MetaParametreAdlari.TakimYarisiPuanNormal) + Tam(MetaParametreAdlari.TakimYarisiPuanZor);

            Assert.That(olay.Katki, Is.EqualTo(puan), "Çarpan uygulanmaz.");
            Assert.That(olay.TakimPuani, Is.EqualTo(puan), "Sunucuya gitmeden önce yerelde güncellenir.");
            string takim = ben.Takim.TakimKimligi!;
            Assert.That(d.Sunucu.Siralama("ty", takim).Deger.Siralama.Single(t => !t.Bot).Puan, Is.EqualTo(0));

            d.Ilerle(ben);
            Assert.That(d.Sunucu.Siralama("ty", takim).Deger.Siralama.Single(t => !t.Bot).Puan, Is.EqualTo(puan));
            Assert.That(olay.TakimPuani, Is.EqualTo(puan), "Gönderilen puan iki kez sayılmaz.");
            Assert.That(olay.Liste.Select(t => t.Puan), Is.Ordered.Descending);
        }

        [Test]
        [Property("GDD", "10.3.4, 10.3.7, 10.7.6")]
        public void Bitime_yakin_son_saatler_baslar_ve_yaristayken_ayrilmak_uyari_ister()
        {
            var (d, ben, _) = YarisKur();
            var olay = Yaris(ben);
            var pencere = TimeSpan.FromHours(Tam(MetaParametreAdlari.SonSaatlerPenceresiSaat));

            Assert.That(ben.Takim.AyrilmaUyarisiGerekir(Simdi), Is.True);
            Assert.That(d.Oyuncu("serbest").Takim.AyrilmaUyarisiGerekir(Simdi), Is.False);

            d.Saat.SimdiUtc = olay.Bitis - pencere - TimeSpan.FromMinutes(1);
            d.Ilerle(ben);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.Yarista));
            Assert.That(ben.Pencereler.Bekleyen, Is.EqualTo(0));

            d.Saat.SimdiUtc = olay.Bitis - pencere;
            d.Ilerle(ben);
            d.Ilerle(ben);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.SonSaatler));
            Assert.That(HepsiniGoster(ben.Pencereler), Is.EqualTo(new[] { "takim_yarisi_son_saatler:ty" }), "Uyarı bir kez gösterilir.");

            // Yarışta takımdan ayrılan oyuncu takımsız duruma döner; katkısı silinir.
            Kazan(ben, d.An);
            Assert.That(olay.Katki, Is.GreaterThan(0));
            ben.Takim.Ayril(d.Sosyal);
            d.Ilerle(ben);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.Takimsiz));
            Assert.That(olay.Katki, Is.EqualTo(0));
            Assert.That(ben.Takim.AyrilmaUyarisiGerekir(d.An), Is.False);
        }

        [Test]
        [Property("GDD", "10.3.5, 10.3.6, 10.3.8")]
        public void Olay_bitince_katkisi_yeterli_oyuncu_sirasinin_odulunu_alir()
        {
            var (d, ben, _) = YarisKur();
            var olay = Yaris(ben);
            Kazan(ben, Simdi);
            d.Ilerle(ben);

            Assert.That(olay.SonucuKapat(d.Sosyal, ben, olay.Bitis - TimeSpan.FromSeconds(1)), Is.False, "Olay sürüyor.");
            d.Saat.SimdiUtc = olay.Bitis;
            d.Ilerle(ben);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.Bitti));
            Kazan(ben, d.An);
            Assert.That(olay.Katki, Is.EqualTo(Tam(MetaParametreAdlari.TakimYarisiPuanNormal)), "Süre dolduktan sonra puan sayılmaz.");

            int para = ben.Cuzdan.Bakiye;
            int beklenen = olay.SiraOdulu(olay.Sira)?.Para ?? 0;
            Assert.That(olay.SonucuKapat(d.Sosyal, ben, d.An), Is.True);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.OdulAlindi));
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + beklenen));
            Assert.That(d.Sosyal.YarisKapanislari, Is.EqualTo(new[] { ("ty", "ben", true) }));
            Assert.That(olay.SonucuKapat(d.Sosyal, ben, d.An), Is.False, "Sonuç bir kez kapatılır.");

            // Kapanan olay bir sonraki takvim uygulamasında kaldırılır.
            ben.Olaylar.TakvimiUygula(null, d.An);
            Assert.That(ben.Olaylar.Kurulu, Is.Empty);
        }

        [Test]
        [Property("GDD", "10.3.5")]
        public void Katkisi_yetersiz_oyuncu_icin_olay_odulsuz_kapanir_ve_sunucuya_bildirilir()
        {
            var (d, ben, _) = YarisKur();
            var olay = Yaris(ben);
            int para = ben.Cuzdan.Bakiye;
            d.Saat.SimdiUtc = olay.Bitis;

            Assert.That(olay.SonucuKapat(d.Sosyal, ben, d.An), Is.True);
            Assert.That(olay.Durum(ben, d.An), Is.EqualTo(TakimYarisiDurumu.OdulsuzKapandi));
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para));
            Assert.That(d.Sosyal.YarisKapanislari, Is.EqualTo(new[] { ("ty", "ben", false) }));
        }

        [Test]
        [Property("GDD", "10.3.6")]
        public void Sira_odulleri_varsayilan_listeden_gelir_ve_sunucu_listesi_yerine_gecer()
        {
            var olay = new TakimYarisiOlayi("ty", Simdi + TimeSpan.FromDays(2), P);
            var paralar = P.TamSayiListesi(MetaParametreAdlari.TakimYarisiSiraOdulleri);

            Assert.That(Enumerable.Range(1, paralar.Count).Select(s => olay.SiraOdulu(s)!.Para), Is.EqualTo(paralar));
            Assert.That(paralar, Is.EqualTo(new[] { 5000, 2500, 1500, 1000, 500 }), "Öneri değerler.");
            Assert.That(olay.SiraOdulu(paralar.Count + 1), Is.Null, "Listede karşılığı olmayan sıranın ödülü yoktur.");
            Assert.That(olay.SiraOdulu(0), Is.Null);

            olay.OdulleriAyarla(new OdulPaketi?[] { new OdulPaketi { Para = 9 }, new OdulPaketi { Para = 8 } });
            Assert.That(olay.SiraOdulu(1)!.Para, Is.EqualTo(9));
            Assert.That(olay.SiraOdulu(3), Is.Null);
        }

        [Test]
        [Property("GDD", "10.0.8, 10.3")]
        public void Sonuc_kapatilmazsa_odul_bekleme_suresinden_sonra_kendiliginden_verilir_ve_yaris_kayitla_korunur()
        {
            var (d, ben, _) = YarisKur();
            Kazan(ben, Simdi, ZorlukEtiketi.Zor);

            string kayit = OyuncuKaydi.Yaz(ben);
            ben = OyuncuKaydi.Yukle(kayit, P, Simdi);
            Assert.That(OyuncuKaydi.Yaz(ben), Is.EqualTo(kayit));
            var olay = Yaris(ben);
            Assert.That(olay.Katki, Is.EqualTo(Tam(MetaParametreAdlari.TakimYarisiPuanZor)));
            Assert.That(olay.Durum(ben, Simdi), Is.EqualTo(TakimYarisiDurumu.Yarista));

            Assert.That(ben.Olaylar.TakvimiUygula(null, olay.Bitis), Is.Empty, "Sonuç oyuncuyu bekler.");
            int para = ben.Cuzdan.Bakiye;
            int beklenen = olay.SiraOdulu(olay.Sira)?.Para ?? 0;
            var bitenler = ben.Olaylar.TakvimiUygula(null, olay.Bitis + TakimYarisiOlayi.SonucBeklemesi);
            Assert.That(bitenler.Single().Kimlik, Is.EqualTo("ty"));
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + beklenen));
        }

        // ------------------------------------------------------------------ 6.3 Kart gönderme ve isteme

        private static readonly DateTime AlbumBitisi = Simdi + TimeSpan.FromDays(60);

        private static Oyuncu Albumlu(Dunya d, string kimlik, string sezon = "a1")
        {
            var oyuncu = d.Oyuncu(kimlik);
            oyuncu.AlbumSezonu.GorsellerIndi = true;
            oyuncu.AlbumSezonunuEsitle(new DonemBilgisi(sezon, AlbumBitisi), Simdi);
            Assert.That(oyuncu.AlbumSezonu.Katildi, Is.True);
            return oyuncu;
        }

        [Test]
        [Property("GDD", "9.7.9, 10.7.7")]
        public void Kart_arkadasa_ya_da_takim_uyesine_gonderilir_ve_gunluk_sinir_vardir()
        {
            var d = new Dunya();
            var ben = Albumlu(d, "ben");
            var ali = Albumlu(d, "ali");
            for (int kart = 0; kart < 4; kart++) ben.Album.KartEkle(0, kart);
            int sinir = Tam(MetaParametreAdlari.GunlukKartGonderme);

            Assert.That(ben.Takim.KartGonder(d.Sosyal, "ali", 5, 5, Simdi), Is.False, "Albümde olmayan kart gönderilemez.");
            for (int i = 0; i < sinir; i++)
            {
                Assert.That(ben.Takim.KartGonder(d.Sosyal, "ali", 0, i, Simdi), Is.True);
            }

            Assert.That(ben.Takim.KartGonder(d.Sosyal, "ali", 0, 3, Simdi), Is.False, "Günlük sınır doldu.");
            Assert.That(ben.Takim.KartGonder(d.Sosyal, "ali", 0, 3, Simdi + TimeSpan.FromDays(1)), Is.True, "Ertesi gün yeniden gönderilir.");
            Assert.That(ben.Album.Var(0, 0), Is.True, "Gönderilen kart gönderenin albümünde kalır.");

            // Kart alıcının gelen kutusuna düşer; toplanınca albüme girer.
            ali.GelenKutusu.Esitle(d.Sosyal);
            Assert.That(ali.GelenKutusu.Ogeler, Has.Count.EqualTo(sinir + 1));
            Assert.That(ali.Album.Var(0, 0), Is.False);
            ali.GelenKutusu.HepsiniTopla(Simdi);
            Assert.That(ali.Album.Var(0, 0), Is.True);
            Assert.That(ali.Album.KartSayisi, Is.EqualTo(sinir + 1));

            // Alıcıda zaten olan kart çifttir ve yıldıza dönüşür.
            ben.Takim.KartGonder(d.Sosyal, "ali", 0, 0, Simdi + TimeSpan.FromDays(2));
            ali.GelenKutusu.Esitle(d.Sosyal);
            ali.GelenKutusu.HepsiniTopla(Simdi);
            Assert.That(ali.AlbumSezonu.CiftYildizi, Is.EqualTo(ali.AlbumSezonu.Yapilandirma.Yildiz(0, 0) * 2));

            // Başka sezonun kartı albüme girmez.
            var eski = Albumlu(d, "eski", sezon: "a0");
            eski.Album.KartEkle(1, 1);
            eski.Takim.KartGonder(d.Sosyal, "ali", 1, 1, Simdi);
            ali.GelenKutusu.Esitle(d.Sosyal);
            ali.GelenKutusu.HepsiniTopla(Simdi);
            Assert.That(ali.Album.Var(1, 1), Is.False);
        }

        [Test]
        [Property("GDD", "9.7.9, 10.7.7")]
        public void Kart_takim_sohbetine_gonderilir_ve_takim_sohbetinden_istenir()
        {
            var d = new Dunya();
            var ben = Albumlu(d, "ben");
            var ali = Albumlu(d, "ali");
            var can = Albumlu(d, "can");
            ben.Album.KartEkle(0, 0);
            Assert.That(ben.Takim.KartGonder(d.Sosyal, null, 0, 0, Simdi), Is.False, "Takımı olmayan sohbete gönderemez.");
            d.TakimKur(acik: true, ben, ali, can);

            // Sohbete gönderilen kartı ilk isteyen alır.
            Assert.That(ben.Takim.KartGonder(d.Sosyal, null, 0, 0, Simdi), Is.True);
            long mesaj = ali.Takim.Sohbet(d.Sosyal).Single(m => m.Tur == SohbetMesajiTuru.Kart).Kimlik;
            Assert.That(ben.Takim.SohbettenKartAl(d.Sosyal, mesaj), Is.False, "Gönderen kendi kartını alamaz.");
            Assert.That(ali.Takim.SohbettenKartAl(d.Sosyal, mesaj), Is.True);
            Assert.That(can.Takim.SohbettenKartAl(d.Sosyal, mesaj), Is.False);
            ali.GelenKutusu.Esitle(d.Sosyal);
            ali.GelenKutusu.HepsiniTopla(Simdi);
            Assert.That(ali.Album.Var(0, 0), Is.True);

            // Kart isteği: eksik kart için, belirli aralıkla.
            var aralik = TimeSpan.FromHours(Tam(MetaParametreAdlari.KartIstekSifirlamaSaat));
            Assert.That(ali.Takim.KartIste(d.Sosyal, 0, 0, Simdi), Is.False, "Albümdeki kart istenmez.");
            Assert.That(ali.Takim.KartIste(d.Sosyal, 3, 2, Simdi), Is.True);
            Assert.That(ali.Takim.KartIste(d.Sosyal, 3, 3, Simdi + aralik - TimeSpan.FromSeconds(1)), Is.False);
            Assert.That(ali.Takim.KartIste(d.Sosyal, 3, 3, Simdi + aralik), Is.True, "İstek sınırı aralıkla sıfırlanır.");

            var istek = ben.Takim.Sohbet(d.Sosyal).First(m => m.Tur == SohbetMesajiTuru.KartIstegi);
            Assert.That((istek.Gonderen, istek.Set, istek.Kart), Is.EqualTo(("ali", 3, 2)));

            // Gönderim ve istek sayaçları kayıtla korunur.
            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(ali), P, Simdi);
            Assert.That(yuklenen.AlbumSezonu.KartIsteyebilir(Simdi + aralik + TimeSpan.FromHours(1)), Is.False);
        }

        // ------------------------------------------------------------------ 6.4 Ortaklı atış

        private static (Dunya D, Oyuncu Ben, OrtakliAtisOlayi Olay) AtisKur(bool gir = true, bool ortak = true)
        {
            var d = new Dunya();
            var ben = d.Oyuncu("ben");
            var kayit = new OlayTakvimKaydi("atis", OlayTuru.OrtakliAtis, Simdi, Simdi + TimeSpan.FromDays(3));
            for (int i = 0; i < 4; i++) kayit.Oduller.Add(new OdulPaketi { Para = 10 });
            kayit.Oduller.Add(new OdulPaketi { Para = 3000 });
            ben.Olaylar.TakvimiUygula(new[] { kayit }, Simdi);
            HepsiniGoster(ben.Pencereler);
            var olay = (OrtakliAtisOlayi)ben.Olaylar.Bul("atis")!;
            if (gir)
            {
                Assert.That(olay.Gir(Simdi), Is.True);
            }

            if (gir && ortak)
            {
                Assert.That(olay.OrtakDavetEt(d.Sosyal, ben, "ali", Simdi), Is.True);
            }

            return (d, ben, olay);
        }

        [Test]
        [Property("GDD", "10.6.1")]
        public void Ortakli_atis_girilince_ortak_arar_ve_en_az_bir_ortakla_aktif_olur()
        {
            var (d, ben, olay) = AtisKur(gir: false);
            Assert.That(d.Oyuncu("erken", Tam(MetaParametreAdlari.AcilisOrtakliAtis) - 1).Olaylar.Acik(Simdi), Is.Empty);
            Assert.That(olay.Durum(Simdi), Is.EqualTo(OrtakliAtisDurumu.Hazir));
            Assert.That(olay.OrtakDavetEt(d.Sosyal, ben, "ali", Simdi), Is.False, "Önce olaya girilir.");

            Assert.That(olay.Gir(Simdi), Is.True);
            Assert.That(olay.Durum(Simdi), Is.EqualTo(OrtakliAtisDurumu.OrtakAraniyor));

            int azami = Tam(MetaParametreAdlari.OrtakliAtisAzamiOrtak);
            for (int i = 1; i <= azami; i++)
            {
                Assert.That(olay.OrtakDavetEt(d.Sosyal, ben, "ortak" + i, Simdi), Is.True);
            }

            Assert.That(olay.Durum(Simdi), Is.EqualTo(OrtakliAtisDurumu.Aktif));
            Assert.That(olay.Ortaklar, Has.Count.EqualTo(azami));
            Assert.That(olay.OrtakDavetEt(d.Sosyal, ben, "fazla", Simdi), Is.False, "Azami ortak sayısı aşılmaz.");
        }

        [Test]
        [Property("GDD", "10.6.2, 10.6.3, 10.9.5")]
        public void Bilyeler_kazanimdan_ve_odullerden_gelir_ve_atista_harcanir()
        {
            var (d, ben, olay) = AtisKur(ortak: false);

            // Bölüm kazanımı 10.9.2'deki puan kadar bilye verir.
            var sonuc = Kazan(ben, Simdi, ZorlukEtiketi.Zor);
            Assert.That(olay.Bilye, Is.EqualTo(sonuc.OlayPuani));

            // Diğer olayların basamak ödüllerinden gelen bilyeler olaya aktarılır.
            var odul = new OdulPaketi();
            odul.OlayEsyalari[OrtakliAtisOlayi.BilyeEsyasi] = 5;
            ben.OdulVer(odul, Simdi);
            d.Ilerle(ben);
            Assert.That(olay.Bilye, Is.EqualTo(sonuc.OlayPuani + 5));
            Assert.That(ben.OlayEsyalari[OrtakliAtisOlayi.BilyeEsyasi], Is.EqualTo(0));

            // Ortak yokken atış yapılamaz.
            Assert.That(olay.AtisYap(100, Simdi), Is.False);
            olay.OrtakDavetEt(d.Sosyal, ben, "ali", Simdi);
            int bilye = olay.Bilye;
            Assert.That(olay.AtisYap(1200, Simdi), Is.True);
            Assert.That(olay.Bilye, Is.EqualTo(bilye - 1));
            Assert.That(olay.Puan, Is.EqualTo(1200), "Atışın puanı istemcide hesaplanır.");

            // Puan sunucuya gönderilir; bilye bitince atış yapılamaz.
            d.Ilerle(ben);
            Assert.That(d.Sosyal.OrtakliAtisPuani("atis", "ben"), Is.EqualTo(1200));
            for (int i = 0; i < bilye - 1; i++) olay.AtisYap(0, Simdi);
            Assert.That(olay.AtisYap(100, Simdi), Is.False);
        }

        [Test]
        [Property("GDD", "10.6.3, 10.6.4")]
        public void Puan_ortagin_puaniyla_birlikte_sayilir_ve_basamakta_maskot_buyur()
        {
            var (d, ben, olay) = AtisKur();
            var basamaklar = P.TamSayiListesi(MetaParametreAdlari.OrtakliAtisPuanBasamaklari);
            Assert.That(olay.Basamaklar, Is.EqualTo(basamaklar));
            ben.OdulVer(BilyeOdulu(3), Simdi);
            d.Ilerle(ben);

            olay.AtisYap(basamaklar[0] - 1000, Simdi);
            Assert.That(olay.MaskotEvresi, Is.EqualTo(0));
            Assert.That(olay.BasamakOdulunuAl(0, ben, Simdi), Is.False);

            // Ortak saatte 500 puan toplar: iki saat sonra 1000 puanı toplam puana eklenir.
            d.Saat.Ilerlet(TimeSpan.FromHours(2));
            olay.AtisYap(0, d.An);
            d.Ilerle(ben);
            Assert.That(olay.Ortaklar.Single().Puan, Is.EqualTo(1000));
            Assert.That(olay.ToplamPuan, Is.EqualTo(basamaklar[0]));
            Assert.That(olay.MaskotEvresi, Is.EqualTo(1), "Maskot bir sonraki evresine geçer.");

            int para = ben.Cuzdan.Bakiye;
            Assert.That(olay.BasamakOdulunuAl(0, ben, d.An), Is.True);
            Assert.That(olay.BasamakOdulunuAl(0, ben, d.An), Is.False);
            Assert.That(olay.BasamakOdulunuAl(1, ben, d.An), Is.False);
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + 10));
        }

        private static OdulPaketi BilyeOdulu(int adet)
        {
            var odul = new OdulPaketi();
            odul.OlayEsyalari[OrtakliAtisOlayi.BilyeEsyasi] = adet;
            return odul;
        }

        [TestCase(39, 0)]
        [TestCase(40, 1)]
        [TestCase(79, 1)]
        [TestCase(80, 2)]
        [TestCase(400, 2)]
        [Property("GDD", "10.6.5")]
        public void Kullanilmayan_bilyeler_oranla_takviyeye_donusur(int bilye, int kure)
        {
            Assert.That(new OrtakliAtisOlayi("atis", Simdi, P).BilyeDonusumu(bilye), Is.EqualTo(kure), "Öneri: her 40 bilye için 1 renk küresi, en fazla 2.");
        }

        [Test]
        [Property("GDD", "10.6.5, 10.0.8")]
        public void Olay_bitince_son_liste_alinir_buyuk_odul_ve_bilye_donusumu_verilir()
        {
            var (d, ben, olay) = AtisKur();
            var basamaklar = olay.Basamaklar;
            ben.OdulVer(BilyeOdulu(86), Simdi);
            d.Ilerle(ben);
            olay.AtisYap(basamaklar.Last(), Simdi);
            Assert.That(olay.MaskotEvresi, Is.EqualTo(basamaklar.Count));
            olay.BasamakOdulunuAl(0, ben, Simdi);
            int para = ben.Cuzdan.Bakiye;

            d.Saat.SimdiUtc = olay.Bitis;
            Assert.That(olay.Durum(d.An), Is.EqualTo(OrtakliAtisDurumu.SonListeBekleniyor));
            Assert.That(olay.AtisYap(10, d.An), Is.False);
            d.Ilerle(ben);

            Assert.That(olay.Durum(d.An), Is.EqualTo(OrtakliAtisDurumu.Bitti));
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + (3 * 10) + 3000), "Alınmamış üç basamak ödülü ve büyük ödül.");
            Assert.That(ben.Envanter.Adet(TakviyeTuru.RenkKuresi), Is.EqualTo(2), "85 bilye iki renk küresine dönüşür.");
            Assert.That(olay.Bilye, Is.EqualTo(0));

            // Biten olay bir sonraki takvim uygulamasında kaldırılır; ödül ikinci kez verilmez.
            Assert.That(ben.Olaylar.TakvimiUygula(null, d.An).Single().Oduller, Is.Empty);
            Assert.That(ben.Cuzdan.Bakiye, Is.EqualTo(para + 3030));
        }

        [Test]
        [Property("GDD", "10.6.5, 10.1.7")]
        public void Son_liste_gelmezse_denemelerden_sonra_eldeki_puanlarla_kapanir_ve_olay_kayitla_korunur()
        {
            var (d, ben, olay) = AtisKur();
            ben.OdulVer(BilyeOdulu(2), Simdi);
            d.Ilerle(ben);
            olay.AtisYap(500, Simdi);

            string kayit = OyuncuKaydi.Yaz(ben);
            ben = OyuncuKaydi.Yukle(kayit, P, Simdi);
            Assert.That(OyuncuKaydi.Yaz(ben), Is.EqualTo(kayit));
            olay = (OrtakliAtisOlayi)ben.Olaylar.Bul("atis")!;
            Assert.That(olay.Durum(Simdi), Is.EqualTo(OrtakliAtisDurumu.Aktif));
            Assert.That((olay.Puan, olay.Bilye, olay.Ortaklar.Count), Is.EqualTo((500, 1, 1)));

            d.Sosyal.Cevrimdisi = true;
            d.Saat.SimdiUtc = olay.Bitis;
            int deneme = Tam(MetaParametreAdlari.SonListeDenemeSayisi);
            for (int i = 1; i < deneme; i++)
            {
                d.Ilerle(ben);
                Assert.That(olay.Durum(d.An), Is.EqualTo(OrtakliAtisDurumu.SonListeBekleniyor));
                d.Saat.Ilerlet(TimeSpan.FromSeconds(Tam(MetaParametreAdlari.SonListeDenemeAraligiSn)));
            }

            d.Ilerle(ben);
            Assert.That(olay.Durum(d.An), Is.EqualTo(OrtakliAtisDurumu.Bitti));
            Assert.That(olay.MaskotEvresi, Is.EqualTo(0), "Maskot son evresine ulaşmadı: büyük ödül yok.");
        }
    }
}
