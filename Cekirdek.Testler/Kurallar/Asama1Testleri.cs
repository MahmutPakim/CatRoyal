using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Testler.Vakalar;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>
    /// Yol haritası Aşama 1: etikete bağlı ödüller (GDD 7.3.4, 8.8.2), yarışmalı bölümlerde hamle eki ve beceri
    /// (9.11.2, 9.11.3), çarpan düzeyi seçimi (9.10.5), yarışma ödülündeki sınırsız can (9.1.9) ve kodda örtük
    /// kalan çekirdek kurallarının doğrulanması.
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama1Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private static readonly ZorlukEtiketi[] Etiketler =
        {
            ZorlukEtiketi.Normal, ZorlukEtiketi.Zor, ZorlukEtiketi.CokZor, ZorlukEtiketi.AsiriZor,
        };

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        private static Oyuncu Oyuncu(int enYuksekBolum = 500) => new Oyuncu(P, Simdi) { EnYuksekBolum = enYuksekBolum };

        private static ZorlukEtiketTablosu Tablo(int bolum, ZorlukEtiketi etiket)
        {
            var tablo = new ZorlukEtiketTablosu();
            tablo.Degistir(bolum, etiket);
            return tablo;
        }

        // ------------------------------------------------------------------ 1.1 Etikete bağlı ödüller

        [Test]
        [Property("GDD", "7.3.4, 8.8.2")]
        public void Para_hat_puani_ve_olay_puani_etikete_baglidir_yildiz_bagimsizdir()
        {
            var sonuclar = new List<BolumSonucu>();
            foreach (var etiket in Etiketler)
            {
                MetaVakalari.BolumuOyna(Oyuncu(), Tablo(12, etiket), 12, kazan: true, Simdi, out var sonuc);
                sonuclar.Add(sonuc);

                Assert.That(sonuc.Para, Is.EqualTo(EtiketOdulleri.KazanmaParasi(P, etiket, 4)), etiket + " para");
                Assert.That(sonuc.SezonHatPuani, Is.EqualTo(EtiketOdulleri.HatPuani(P, etiket)), etiket + " sezon hattı");
                Assert.That(sonuc.HaftalikHatPuani, Is.EqualTo(EtiketOdulleri.HatPuani(P, etiket)), etiket + " haftalık hat");
                Assert.That(sonuc.OlayPuani, Is.EqualTo(EtiketOdulleri.HatPuani(P, etiket)), etiket + " olay puanı");
                Assert.That(sonuc.Yildiz, Is.EqualTo(Tam(MetaParametreAdlari.BolumBasinaYildiz)), etiket + " yıldız");
            }

            // Etiket zorlaştıkça ödül artar; yıldız hiç değişmez.
            Assert.That(sonuclar.Select(s => s.Para), Is.Ordered.Ascending.And.Unique);
            Assert.That(sonuclar.Select(s => s.OlayPuani), Is.Ordered.Ascending.And.Unique);
            Assert.That(sonuclar.Select(s => s.Yildiz).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "7.3.4, 9.10.2, 9.10.3")]
        public void Carpan_yalniz_olay_ve_haftalik_hat_puanini_katlar_para_ve_yildizi_degistirmez()
        {
            var oyuncu = Oyuncu();
            oyuncu.Carpan.BiletKullan(Simdi);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);

            MetaVakalari.BolumuOyna(oyuncu, Tablo(12, ZorlukEtiketi.Zor), 12, kazan: true, Simdi, out var sonuc);

            int taban = EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Zor);
            Assert.That(sonuc.OlayPuani, Is.EqualTo(taban * kat));
            Assert.That(sonuc.HaftalikHatPuani, Is.EqualTo(taban * kat));
            Assert.That(sonuc.SezonHatPuani, Is.EqualTo(taban));
            Assert.That(sonuc.Para, Is.EqualTo(EtiketOdulleri.KazanmaParasi(P, ZorlukEtiketi.Zor, 4)));
            Assert.That(sonuc.Yildiz, Is.EqualTo(Tam(MetaParametreAdlari.BolumBasinaYildiz)));
        }

        // ------------------------------------------------------------------ 1.2 Yarışmalı bölümler ve beceri

        private const string BesHamleliBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

        private static BolumOturumu Baslat(Oyuncu oyuncu, int bolum, int? hamleEki, out BolumAkisi akis)
        {
            akis = new BolumAkisi(oyuncu, new ZorlukEtiketTablosu(), bolum) { YarismaHamleEki = hamleEki };
            var oturum = akis.Baslat(
                BolumYukleyici.JsondanOku(BesHamleliBolum, P),
                AkBayraklari.Varsayilan,
                7,
                Simdi,
                Enumerable.Empty<TakviyeTuru>(),
                new List<Olay>());
            Assert.That(oturum, Is.Not.Null);
            return oturum!;
        }

        [Test]
        [Property("GDD", "7.2.5, 9.11.2")]
        public void Normal_ilerleme_bolumunde_hamle_sayisi_bolum_verisindeki_gibi_kalir()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisYarismaliBolumler);

            Assert.That(Baslat(Oyuncu(acilis), 12, null, out var normal).KalanHamle, Is.EqualTo(5));
            Assert.That(normal.Yarismali, Is.False);

            // Yarışmalı bölümler açılmadan gelen ek yok sayılır.
            Assert.That(Baslat(Oyuncu(acilis - 1), 12, 3, out var kapali).KalanHamle, Is.EqualTo(5));
            Assert.That(kapali.Yarismali, Is.False);
        }

        [TestCase(3, 8)]
        [TestCase(0, 5)]
        [TestCase(-2, 3)]
        [TestCase(-99, 1)]
        [Property("GDD", "7.2.5, 9.11.2")]
        public void Yarismali_bolumde_yarisma_sisteminden_gelen_deger_hamle_sayisina_eklenir(int ek, int beklenen)
        {
            var oturum = Baslat(Oyuncu(Tam(MetaParametreAdlari.AcilisYarismaliBolumler)), 12, ek, out var akis);

            Assert.That(akis.Yarismali, Is.True);
            Assert.That(oturum.KalanHamle, Is.EqualTo(beklenen));
        }

        [TestCase(1.0, BeceriDuzeyi.Usta)]
        [TestCase(1.2, BeceriDuzeyi.Usta)]
        [TestCase(1.21, BeceriDuzeyi.Orta)]
        [TestCase(1.8, BeceriDuzeyi.Orta)]
        [TestCase(2.5, BeceriDuzeyi.Baslangic)]
        [TestCase(3.0, BeceriDuzeyi.Baslangic)]
        [TestCase(3.01, BeceriDuzeyi.Becerisiz)]
        [TestCase(40, BeceriDuzeyi.Becerisiz)]
        [Property("GDD", "9.11.3, 11.2.20")]
        public void Beceri_duzeyi_ortalama_deneme_sayisindan_esiklerle_bulunur(double ortalama, BeceriDuzeyi beklenen)
        {
            Assert.That(new YariDinamikZorluk(P).BeceriHesapla(ortalama), Is.EqualTo(beklenen), "Öneri eşiklerle.");
        }

        [Test]
        [Property("GDD", "9.11.3")]
        public void Beceri_esikleri_bozuksa_parametre_hatasi_verir()
        {
            Assert.That(
                () => new YariDinamikZorluk(P.Degistir(MetaParametreAdlari.BeceriEsikleri, "≤1.2 usta; ≤1.8 orta")).BeceriHesapla(5),
                Throws.TypeOf<ParametreHatasi>());
            Assert.That(
                () => new YariDinamikZorluk(P.Degistir(MetaParametreAdlari.BeceriEsikleri, "≤1.2 dahi; üstü becerisiz")).BeceriHesapla(1),
                Throws.TypeOf<ParametreHatasi>());
        }

        [Test]
        [Property("GDD", "9.11.3")]
        public void Beceri_ilk_bolumlerde_ilk_oynayislardan_olculur()
        {
            var oyuncu = Oyuncu(1);
            var etiketler = new ZorlukEtiketTablosu();
            Assert.That(oyuncu.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Orta), "Ölçüm yokken orta.");
            Assert.That(oyuncu.Zorluk.OrtalamaDeneme, Is.Null);

            // 1. bölüm ilk denemede: ortalama 1 → usta.
            MetaVakalari.BolumuOyna(oyuncu, etiketler, 1, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Zorluk.OrtalamaDeneme, Is.EqualTo(1));
            Assert.That(oyuncu.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Usta));

            // 2. bölüm üç başarısızlıktan sonra: 5 deneme / 2 bölüm = 2.5 → başlangıç.
            for (int i = 0; i < 3; i++)
            {
                MetaVakalari.BolumuOyna(oyuncu, etiketler, 2, kazan: false, Simdi, out _);
            }

            Assert.That(oyuncu.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Usta), "Beceri bölüm kazanılınca güncellenir.");
            MetaVakalari.BolumuOyna(oyuncu, etiketler, 2, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Zorluk.OrtalamaDeneme, Is.EqualTo(2.5));
            Assert.That(oyuncu.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Baslangic));

            // Geçilmiş bir bölümü yeniden oynamak ölçüme girmez.
            MetaVakalari.BolumuOyna(oyuncu, etiketler, 1, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Zorluk.OlcumDenemesi, Is.EqualTo(5));
            Assert.That(oyuncu.Zorluk.OlcumBolumu, Is.EqualTo(2));

            // Ölçüm kayıtla korunur.
            var yuklenen = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(yuklenen.Zorluk.OrtalamaDeneme, Is.EqualTo(2.5));
            Assert.That(yuklenen.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Baslangic));
        }

        [Test]
        [Property("GDD", "9.11.3")]
        public void Olcum_son_bolumunden_sonraki_bolumler_beceriyi_degistirmez()
        {
            int son = Tam(MetaParametreAdlari.BeceriOlcumSonBolum);
            var oyuncu = Oyuncu(son);
            var etiketler = new ZorlukEtiketTablosu();

            MetaVakalari.BolumuOyna(oyuncu, etiketler, son, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Zorluk.OlcumBolumu, Is.EqualTo(1), "Son ölçüm bölümü dahildir.");

            MetaVakalari.BolumuOyna(oyuncu, etiketler, son + 1, kazan: false, Simdi, out _);
            MetaVakalari.BolumuOyna(oyuncu, etiketler, son + 1, kazan: true, Simdi, out _);
            Assert.That(oyuncu.Zorluk.OlcumDenemesi, Is.EqualTo(1));
            Assert.That(oyuncu.Zorluk.Beceri, Is.EqualTo(BeceriDuzeyi.Usta));
        }

        // ------------------------------------------------------------------ 1.3 Çarpan düzeyi seçimi

        [Test]
        [Property("GDD", "9.10.5, 9.4")]
        public void Carpan_secimi_acilis_bolumunde_acilir_ve_aktif_carpana_kadar_duzey_sunar()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisCarpanSecimi);
            Assert.That(Oyuncu(acilis - 1).CarpanSecimiAcik, Is.False);
            var oyuncu = Oyuncu(acilis);
            Assert.That(oyuncu.CarpanSecimiAcik, Is.True);

            Assert.That(oyuncu.Carpan.SecilebilirDuzeyler(Simdi), Is.EqualTo(new[] { 1 }), "Aktif çarpan yokken yalnız 1.");
            oyuncu.Carpan.BiletKullan(Simdi);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);
            Assert.That(oyuncu.Carpan.SecilebilirDuzeyler(Simdi), Is.EqualTo(Enumerable.Range(1, kat)));

            Assert.That(oyuncu.Carpan.DuzeySec(0), Is.False);
            Assert.That(oyuncu.Carpan.SecilenDuzey, Is.Null);
        }

        [Test]
        [Property("GDD", "9.10.5, 9.10.1, 9.10.2")]
        public void Secilen_carpan_duzeyi_bolume_girerken_isaret_olur()
        {
            var oyuncu = Oyuncu();
            var etiketler = Tablo(12, ZorlukEtiketi.Zor);
            int taban = EtiketOdulleri.HatPuani(P, ZorlukEtiketi.Zor);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);
            oyuncu.Carpan.BiletKullan(Simdi);

            Assert.That(oyuncu.Carpan.DuzeySec(2), Is.True);
            var akis = MetaVakalari.BolumuOyna(oyuncu, etiketler, 12, kazan: true, Simdi, out var sonuc);
            Assert.That(akis.CarpanIsareti, Is.EqualTo(2));
            Assert.That(sonuc.HaftalikHatPuani, Is.EqualTo(taban * 2));
            Assert.That(sonuc.SezonHatPuani, Is.EqualTo(taban), "Sezon hattına çarpan uygulanmaz.");

            // Seçim kalıcıdır ve kayıtla korunur.
            oyuncu = OyuncuKaydi.Yukle(OyuncuKaydi.Yaz(oyuncu), P, Simdi);
            Assert.That(oyuncu.Carpan.SecilenDuzey, Is.EqualTo(2));

            // Aktif çarpanı aşan seçim aktif çarpanla sınırlanır.
            oyuncu.Carpan.DuzeySec(kat + 5);
            Assert.That(MetaVakalari.BolumuOyna(oyuncu, etiketler, 12, kazan: true, Simdi, out _).CarpanIsareti, Is.EqualTo(kat));

            // Seçim kaldırılınca aktif çarpanın tamamı kullanılır.
            oyuncu.Carpan.DuzeySec(1);
            Assert.That(MetaVakalari.BolumuOyna(oyuncu, etiketler, 12, kazan: true, Simdi, out _).CarpanIsareti, Is.EqualTo(1));
            oyuncu.Carpan.DuzeySec(null);
            Assert.That(MetaVakalari.BolumuOyna(oyuncu, etiketler, 12, kazan: true, Simdi, out _).CarpanIsareti, Is.EqualTo(kat));
        }

        [Test]
        [Property("GDD", "9.10.5")]
        public void Secim_kapaliyken_ya_da_carpan_bitince_secilen_duzey_etkisizdir()
        {
            var carpan = new CarpanSistemi(P);
            int kat = Tam(MetaParametreAdlari.CarpanBiletiKati);
            carpan.BiletKullan(Simdi);
            carpan.DuzeySec(2);

            Assert.That(carpan.BolumeGir(Simdi, secimAcik: false), Is.EqualTo(kat));
            Assert.That(carpan.BolumeGir(Simdi, secimAcik: true), Is.EqualTo(2));

            var sonra = Simdi + TimeSpan.FromMinutes(Tam(MetaParametreAdlari.EtkinlikCarpaniSuresiDk));
            Assert.That(carpan.BolumeGir(sonra, secimAcik: true), Is.EqualTo(1), "Çarpan bitince işaret 1'dir.");
        }

        // ------------------------------------------------------------------ 1.4 Yarışma ödülündeki sınırsız can

        [Test]
        [Property("GDD", "9.1.9, 9.1.7")]
        public void Yarisma_odulundeki_sinirsiz_can_suresi_parametre_kadardir()
        {
            var sure = TimeSpan.FromMinutes(Tam(MetaParametreAdlari.HediyeSinirsizCanDk));
            var oyuncu = Oyuncu();
            oyuncu.Can.BolumKaybi(Simdi);

            var paket = new OdulPaketi { Para = 100 }.YarismaSinirsizCaniEkle(P);
            Assert.That(paket.SinirsizCanDakika, Is.EqualTo(sure.TotalMinutes));
            oyuncu.OdulVer(paket, Simdi);

            Assert.That(oyuncu.Can.Durum(Simdi), Is.EqualTo(CanDurumu.Sinirsiz));
            Assert.That(oyuncu.Can.SinirsizKalan(Simdi), Is.EqualTo(sure));
            Assert.That(oyuncu.Can.SinirsizAktif(Simdi + sure), Is.False);

            // İkinci yarışma ödülü süreyi uzatır (9.1.7).
            oyuncu.OdulVer(new OdulPaketi().YarismaSinirsizCaniEkle(P), Simdi);
            Assert.That(oyuncu.Can.SinirsizKalan(Simdi), Is.EqualTo(sure + sure));
        }

        // ------------------------------------------------------------------ 1.5 Örtük kuralların doğrulanması

        [Test]
        [Property("GDD", "2.1.4")]
        public void Oynanmayan_hucre_tas_tasimaz_kaydirilamaz_ve_dolmaz()
        {
            var o = TestOturumu.Kur(@"
                   1 2 3 4
                1  K K M K
                2  · X · ·
                3  · · · ·");

            Assert.That(o.Tahta[2, 2].Oynanabilir, Is.False);
            Assert.That(o.Kaydir(H(1, 2), H(2, 2)), Is.EqualTo(HamleSonucu.Imkansiz));
            Assert.That(o.Kaydir(H(2, 1), H(2, 2)), Is.EqualTo(HamleSonucu.Imkansiz));

            Assert.That(o.Kaydir(H(1, 4), H(1, 3)), Is.EqualTo(HamleSonucu.Gecerli));

            Assert.That(o.Tahta[2, 2].Tas, Is.Null, "Oynanmayan hücreye taş düşmez.");
            Assert.That(
                o.Tahta.TumKonumlar().Where(k => o.Tahta[k].Oynanabilir).All(k => o.Tahta[k].Tas != null),
                Is.True,
                "Oynanabilir her hücre durulmadan sonra bir taş taşır.");
        }

        [Test]
        [Property("GDD", "2.1.4")]
        public void Oynanabilir_hucre_tasin_altinda_ve_ustunde_katman_tasiyabilir()
        {
            var test = Kur(@"
                   1 2 3
                1  K K ·
                2  · · K");
            var hucre = test.Tahta[1, 2];
            hucre.AltKatman = new HucreKatmani(YayilanZemin, 1);
            hucre.UstKatman = new HucreKatmani(Kilit, 1);

            Assert.That(hucre.Oynanabilir, Is.True);
            Assert.That(hucre.Tas, Is.Not.Null);
            Assert.That(hucre.AltKatman.Tanim.Ustte, Is.False);
            Assert.That(hucre.UstKatman.Tanim.Ustte, Is.True);

            // Kilitli taş yerinde eşleşmeye girer: (2,3) yukarı kayınca üç kırmızı eşleşir.
            var d = HamleDenemesi.Kaydir(test, H(2, 3), H(1, 3));
            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
        }

        private const string KureTahtasi = @"
               1 2 3 4 5
            1  M K M Y S
            2  K M S M K
            3  M Y R {0} M
            4  S K M Y S
            5  Y M K M Y";

        /// <summary>Kaydırma ya da dokunmayla tek başına etkinleşen renk küreleri (GDD 3.5.3).</summary>
        private static List<OzelTasEtkinlesti> TekBasinaTetiklenenKureler(HamleDenemesi d) =>
            d.Etkinlesenler
                .Where(e => e.Tas.Tur == TasTuru.RenkKuresi && (e.Tetik == Tetik.Kaydirma || e.Tetik == Tetik.Dokunma))
                .ToList();

        [TestCase('B', KombinasyonTuru.KureBomba, false)]
        [TestCase('B', KombinasyonTuru.KureBomba, true)]
        [TestCase('-', KombinasyonTuru.KureCizgi, false)]
        [TestCase('|', KombinasyonTuru.KureCizgi, true)]
        [TestCase('G', KombinasyonTuru.KureGudumlu, false)]
        [TestCase('G', KombinasyonTuru.KureGudumlu, true)]
        [Property("GDD", "3.5.6, 4.1.1")]
        public void Renk_kuresi_ozel_tasla_kaydirilinca_tek_basina_tetiklenmez_kombinasyon_olur(
            char ozelTas, KombinasyonTuru beklenen, bool ozelTasKureyeKaydirilir)
        {
            string cizim = KureTahtasi.Replace("{0}", ozelTas.ToString());
            var d = ozelTasKureyeKaydirilir
                ? HamleDenemesi.Kaydir(cizim, H(3, 4), H(3, 3))
                : HamleDenemesi.Kaydir(cizim, H(3, 3), H(3, 4));

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.Olaylar.OfType<KombinasyonCalisti>().Single().Tur, Is.EqualTo(beklenen));
            Assert.That(TekBasinaTetiklenenKureler(d), Is.Empty, "Kombinasyon tek başına tetiklenme değildir.");
        }

        [TestCase('B', KombinasyonTuru.KureBomba)]
        [TestCase('-', KombinasyonTuru.KureCizgi)]
        [TestCase('G', KombinasyonTuru.KureGudumlu)]
        [TestCase('R', KombinasyonTuru.KureKure)]
        [Property("GDD", "3.6.2, 3.6.6, 4.4.3")]
        public void Cift_renk_kuresi_kombinasyon_ve_tur_kontrollerinde_renk_kuresi_sayilir(
            char digerTas, KombinasyonTuru beklenen)
        {
            var d = HamleDenemesi.Kaydir(KureTahtasi.Replace("{0}", digerTas.ToString()), H(3, 3), H(3, 4), ciftRenk: true);

            Assert.That(d.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(d.Olaylar.OfType<KombinasyonCalisti>().Single().Tur, Is.EqualTo(beklenen));
        }

        [Test]
        [Property("GDD", "3.6.1, 3.6.2, 4.4.3")]
        public void Cift_renk_kuresi_ayri_bir_tas_turu_degildir_bolum_genelinde_moddur()
        {
            // Ayrı bir tür olmadığı için çift renk küresiyle normal renk küresi aynı tahtada bulunamaz (4.4.3):
            // küreyi çift renk yapan, taşın kendisi değil bölümün modudur.
            Assert.That(Enum.GetNames(typeof(TasTuru)).Count(ad => ad.Contains("Kure")), Is.EqualTo(1));

            var tek = HamleDenemesi.Dokun(BVakalari.B07Tahtasi, H(3, 3));
            var cift = HamleDenemesi.Dokun(BVakalari.B07Tahtasi, H(3, 3), ciftRenk: true);
            Assert.That(tek.Tahta[3, 3].Tas, Is.Null);
            Assert.That(tek.SecilenRenkler, Has.Count.EqualTo(1));
            Assert.That(cift.SecilenRenkler, Has.Count.EqualTo(2));
        }

        [Test]
        [Property("GDD", "3.6.5, 3.5.3")]
        public void Cift_renk_kuresi_normal_kureyle_ayni_yollarla_tek_basina_tetiklenir()
        {
            // Dokunma.
            var dokunma = HamleDenemesi.Dokun(BVakalari.B07Tahtasi, H(3, 3), ciftRenk: true);
            Assert.That(dokunma.Sonuc, Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(TekBasinaTetiklenenKureler(dokunma).Single().Tetik, Is.EqualTo(Tetik.Dokunma));

            // Normal taşla kaydırma: iki modda da küre tetiklenir ve ilk renk aynı yolla seçilir.
            string cizim = KureTahtasi.Replace("{0}", "Y");
            var tek = HamleDenemesi.Kaydir(cizim, H(3, 3), H(3, 4));
            var cift = HamleDenemesi.Kaydir(cizim, H(3, 3), H(3, 4), ciftRenk: true);
            Assert.That(TekBasinaTetiklenenKureler(tek).Single().Tetik, Is.EqualTo(Tetik.Kaydirma));
            Assert.That(TekBasinaTetiklenenKureler(cift).Single().Tetik, Is.EqualTo(Tetik.Kaydirma));
            Assert.That(tek.Olaylar.OfType<KombinasyonCalisti>(), Is.Empty);
            Assert.That(cift.Olaylar.OfType<KombinasyonCalisti>(), Is.Empty);
            Assert.That(cift.SecilenRenkler[0].Renk, Is.EqualTo(tek.SecilenRenkler.Single().Renk));
            Assert.That(cift.SecilenRenkler, Has.Count.EqualTo(2));
        }

        [Test]
        [Property("GDD", "3.7.7")]
        public void Dolum_gostergesinden_inen_kure_siradan_renk_kuresidir_ve_kombinasyona_girer()
        {
            double bombaPuani = P.Sayi(ParametreAdlari.DolumPuanBomba);
            var o = TestOturumu.Kur(
                NotrCizim(9, 9, (2, 2, 'B')), parametreler: P.Degistir(ParametreAdlari.DolumKapasitesi, bombaPuani));
            o.Oturum.DolumGostergesiAcik = true;
            o.Dokun(H(2, 2));

            var firlatma = o.OlaylarTurunde<RenkKuresiFirlatildi>().Single();
            Assert.That(firlatma.Kure.Tur, Is.EqualTo(TasTuru.RenkKuresi));

            // İnen kürenin yanına bir bomba konur ve küre ona kaydırılır.
            var komsu = firlatma.Konum.DikKomsular().First(k => o.Tahta.Oynanabilir(k));
            o.Tahta[komsu].Tas = o.Tahta.OzelTasUret(TasTuru.Bomba);
            o.Olaylar.Clear();

            Assert.That(o.Kaydir(firlatma.Konum, komsu), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.OlaylarTurunde<KombinasyonCalisti>().First().Tur, Is.EqualTo(KombinasyonTuru.KureBomba));
        }

        [Test]
        [Property("GDD", "3.7.8, 3.8.3, 4.1.6")]
        public void Dolum_gostergesi_ve_yedek_hamle_hakki_tahta_tasi_degildir_kombinasyona_girmez()
        {
            // Kombinasyonlar yalnızca dört özel taş türü arasındadır.
            var parcalar = new[] { "Cizgi", "Bomba", "Gudumlu", "Kure" };
            foreach (string ad in Enum.GetNames(typeof(KombinasyonTuru)))
            {
                string kalan = parcalar.Aggregate(ad, (metin, parca) => metin.Replace(parca, string.Empty));
                Assert.That(kalan, Is.Empty, ad);
            }

            Assert.That(
                Enum.GetNames(typeof(TasTuru)).Where(ad => ad.Contains("Dolum") || ad.Contains("Yedek")),
                Is.Empty);

            // İkisi de açıkken tahtaya taş eklenmez ve yedek hakkın kullanılması tahtaya dokunmaz.
            var parametreler = P.Degistir(ParametreAdlari.YedekHamleEsigi, 2);
            var o = TestOturumu.Kur(
                @"
                   1 2 3 4
                1  K K M K
                2  · · · ·
                3  · · · ·",
                hamle: 3,
                parametreler: parametreler);
            int ozelTasSayisi() => o.Tahta.TumKonumlar().Count(k => o.Tahta[k].Tas != null && o.Tahta[k].Tas!.Tur != TasTuru.Normal);
            o.Oturum.DolumGostergesiAcik = true;
            o.Oturum.YedekHamleHakki = true;
            Assert.That(ozelTasSayisi(), Is.EqualTo(0));

            o.Kaydir(H(1, 4), H(1, 3));

            Assert.That(o.OlaylarTurunde<YedekHamleKullanildi>().Count(), Is.EqualTo(1));
            Assert.That(o.OlaylarTurunde<KombinasyonCalisti>(), Is.Empty);
            Assert.That(o.OlaylarTurunde<OzelTasEtkinlesti>(), Is.Empty);
            Assert.That(ozelTasSayisi(), Is.EqualTo(0));
        }

        /// <summary>
        /// (4,4)→(4,3) kaydırması 3. sütunda üç kırmızıyı eşler. Boşalan sütuna (1,3)'teki mavi düşer ve 4. satırda
        /// üç mavi kendiliğinden eşleşir (kaskad). (3,1)'deki tek katmanlı blok yalnızca bu kaskad eşleşmesine komşudur.
        /// </summary>
        private const string KaskadTahtasi = @"
               1 2 3 4
            1  · · M ·
            2  · · K ·
            3  1 · K ·
            4  M M · K";

        private static TestOturumu KaskadOyna()
        {
            var o = TestOturumu.Kur(
                KaskadTahtasi,
                hamle: 5,
                hedefler: new[] { new Hedef(Hedef.RenkTuru(Mavi), 3), new Hedef(KatmanliBlokHedefi, 1) });
            Assert.That(o.Kaydir(H(4, 4), H(4, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            return o;
        }

        [Test]
        [Property("GDD", "5.5.4")]
        public void Kaskad_eslesmesi_yan_esleme_hasari_verir_ve_hedef_sayar()
        {
            var o = KaskadOyna();

            var gruplar = o.OlaylarTurunde<GrupPatladi>().ToList();
            Assert.That(gruplar.Select(g => g.Kaskad), Is.EqualTo(new[] { false, true }));

            var hasar = o.OlaylarTurunde<EngelHasarAldi>().Single();
            Assert.That(hasar.Konum, Is.EqualTo(H(3, 1)));
            Assert.That(hasar.Kirildi, Is.True);
            Assert.That(o.Olaylar.IndexOf(hasar), Is.GreaterThan(o.Olaylar.IndexOf(gruplar[1])), "Hasarı kaskad eşleşmesi verdi.");

            Assert.That(o.Oturum.Hedefler.HepsiTamam, Is.True, "Mavi hedefi de blok hedefi de kaskadla doldu.");
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(4), "Kaskad hamle harcamaz.");
        }

        [Test]
        [Property("GDD", "5.8.1")]
        public void Bir_guncellemede_birimler_sirayla_calisir()
        {
            var o = KaskadOyna();
            var gruplar = o.OlaylarTurunde<GrupPatladi>().ToList();
            int Sira(Olay olay) => o.Olaylar.IndexOf(olay);

            // Hamle girişi → eşleşme → düşme ve doldurma → kaskad → kazanma kontrolü.
            int hamle = o.Indis<TaslarYerDegistirdi>();
            int eslesme = Sira(gruplar[0]);
            int ilkDusme = o.Indis<TasDustu>();
            int kaskad = Sira(gruplar[1]);
            int sonDusme = o.Olaylar.FindLastIndex(x => x is TasDustu || x is TasGirdi);
            int kazanma = o.Indis<BolumKazanildi>();

            Assert.That(hamle, Is.EqualTo(0));
            Assert.That(new[] { hamle, eslesme, ilkDusme, kaskad, sonDusme, kazanma }, Is.Ordered.Ascending.And.Unique);
        }

        [Test]
        [Property("GDD", "5.8.1, 7.5")]
        public void Karistirma_denetimi_dusme_ve_doldurmadan_sonra_calisir()
        {
            // Nötr dolgu olası hamle kurmaz: hamleden sonra tahta durulur, ardından "olası hamle yok" akışı çalışır.
            var o = TestOturumu.Kur(
                @"
                   1 2 3 4
                1  K K M K
                2  · · · ·
                3  · · · ·",
                hamle: 5,
                hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 30) },
                karistirma: true);

            Assert.That(o.Kaydir(H(1, 4), H(1, 3)), Is.EqualTo(HamleSonucu.Gecerli));

            int sonDusme = o.Olaylar.FindLastIndex(x => x is TasDustu || x is TasGirdi);
            int denetim = o.Olaylar.FindIndex(x => x is OlasiHamleYok || x is TahtaKaristirildi);
            Assert.That(sonDusme, Is.GreaterThan(0));
            Assert.That(denetim, Is.GreaterThan(sonDusme));
            Assert.That(o.Olaylar.FindIndex(x => x is GrupPatladi), Is.LessThan(sonDusme));
        }
    }
}
