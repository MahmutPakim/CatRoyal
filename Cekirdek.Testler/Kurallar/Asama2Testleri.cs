using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Tasarim;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>
    /// Yol haritası Aşama 2: bölüm doğrulayıcı (GDD 8.1–8.5), tanıtım takvimi (8.6), bonus bölümleri (8.7), zor
    /// bölüm ritmi (8.8.3), çok ekranlı kapı (8.9.4), bölüm paketleri (8.11) ve kurtarma bölümleri (10.2).
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class Asama2Testleri
    {
        private static readonly DateTime Simdi = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        private const string Blok = @"""kb"": { ""arketip"": ""katmanli_blok"", ""hedefTuru"": ""blok"" }";
        private const string Zemin = @"""z"": { ""arketip"": ""zemin"", ""hedefTuru"": ""zemin"" }";
        private const string Kilit = @"""k"": { ""arketip"": ""kilit"" }";
        private const string RenkHedefi = @"{ ""tur"": ""renk:0"", ""adet"": 20 }";
        private const string BlokHedefi = @"{ ""tur"": ""blok"", ""adet"": 0 }";
        private const string ZeminHedefi = @"{ ""tur"": ""zemin"", ""adet"": 0 }";

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        private static int Tam(string ad) => P.TamSayi(ad);

        // ------------------------------------------------------------------ Bölüm üretici

        /// <summary>Bütün hücreleri aynı kodu taşıyan satırlar; <paramref name="sonSatir"/> verilirse en alt satır o kodu taşır.</summary>
        private static string[] Satirlar(int genislik = 9, int yukseklik = 9, string kod = "?", string? sonSatir = null) =>
            Enumerable.Range(1, yukseklik)
                .Select(s => string.Join(" ", Enumerable.Repeat(s == yukseklik && sonSatir != null ? sonSatir : kod, genislik)))
                .ToArray();

        private static string Json(
            int kimlik,
            int hamle = 25,
            string[]? hucreler = null,
            string hedefler = RenkHedefi,
            string engeller = "",
            string katmanlar = "",
            string[]? altKatmanlar = null,
            string[]? ustKatmanlar = null,
            int renkler = 4,
            string ek = "")
        {
            hucreler ??= Satirlar();
            string Izgara(string ad, string[]? satirlar) =>
                satirlar == null ? string.Empty : $", \"{ad}\": [{string.Join(", ", satirlar.Select(s => "\"" + s + "\""))}]";
            return $"{{ \"kimlik\": {kimlik}, \"genislik\": {hucreler[0].Split(' ').Length}, \"yukseklik\": {hucreler.Length}, "
                + $"\"hamle\": {hamle}, \"renkler\": {renkler}, \"engeller\": {{ {engeller} }}, \"katmanlar\": {{ {katmanlar} }}, "
                + $"\"hedefler\": [{hedefler}]{Izgara("hucreler", hucreler)}{Izgara("altKatmanlar", altKatmanlar)}"
                + $"{Izgara("ustKatmanlar", ustKatmanlar)}{ek} }}";
        }

        private static BolumVerisi Oku(string json) => BolumYukleyici.JsondanOku(json, P);

        /// <summary>Renk hedefli, engelsiz olağan bölüm.</summary>
        private static BolumVerisi Sade(int kimlik, int hamle = 25) => Oku(Json(kimlik, hamle));

        /// <summary>En alt satırı katmanlı blok olan bölüm; hedef bütün bloklar.</summary>
        private static BolumVerisi Bloklu(int kimlik, int hamle = 25) =>
            Oku(Json(kimlik, hamle, Satirlar(sonSatir: "kb:1"), BlokHedefi, Blok));

        /// <summary>En alt satırında zemin katmanı olan bölüm; hedef bütün zemin.</summary>
        private static BolumVerisi Zeminli(int kimlik) =>
            Oku(Json(kimlik, hedefler: ZeminHedefi, katmanlar: Zemin, altKatmanlar: Satirlar(kod: ".", sonSatir: "z:1")));

        private static List<string> Kurallar(IEnumerable<BolumSorunu> sorunlar, SorunDuzeyi duzey = SorunDuzeyi.Hata) =>
            sorunlar.Where(s => s.Duzey == duzey).Select(s => s.Kural).ToList();

        // ------------------------------------------------------------------ 2.1 Tek bölüm kuralları

        [Test]
        [Property("GDD", "8.1–8.5")]
        public void Kurallara_uyan_bolumde_sorun_bulunmaz()
        {
            Assert.That(BolumDogrulayici.Dogrula(Sade(1), P), Is.Empty);
            Assert.That(BolumDogrulayici.Dogrula(Bloklu(5), P), Is.Empty);
        }

        [Test]
        [Property("GDD", "8.1.2")]
        public void Standarttan_cok_dar_ya_da_kisa_tahta_hatadir()
        {
            int asgariGenislik = Tam(ParametreAdlari.TahtaGenislikStandart) - Tam(ParametreAdlari.DarTahtaAzamiSutunEksigi);
            int asgariYukseklik = Tam(ParametreAdlari.TahtaYukseklikStandart) - Tam(ParametreAdlari.DarTahtaAzamiSatirEksigi);

            Assert.That(BolumDogrulayici.Dogrula(Oku(Json(1, hucreler: Satirlar(asgariGenislik, asgariYukseklik))), P), Is.Empty);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(1, hucreler: Satirlar(asgariGenislik - 1))), P)), Is.EqualTo(new[] { "8.1.2" }));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(1, hucreler: Satirlar(9, asgariYukseklik - 1))), P)), Is.EqualTo(new[] { "8.1.2" }));
        }

        [Test]
        [Property("GDD", "8.2.2")]
        public void Renk_sayisi_kolay_bolum_sinirinin_altina_inemez()
        {
            int asgari = Tam(ParametreAdlari.RenkSayisiStandart) - Tam(ParametreAdlari.KolayBolumAzamiRenkEksigi);

            Assert.That(BolumDogrulayici.Dogrula(Oku(Json(1, renkler: asgari)), P), Is.Empty);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(1, renkler: asgari - 1)), P)), Is.EqualTo(new[] { "8.2.2" }));
        }

        [Test]
        [Property("GDD", "8.3.1, 8.3.2")]
        public void Hamle_sayisi_bandin_disina_cikamaz()
        {
            int ust = Tam(TasarimParametreAdlari.HamleBandiUst);
            int taban = Tam(TasarimParametreAdlari.AsgariHamle);
            int kirilma = Tam(TasarimParametreAdlari.ZorlukKirilmaBolumu);

            Assert.That(BolumDogrulayici.Dogrula(Sade(1, ust), P), Is.Empty);
            Assert.That(BolumDogrulayici.Dogrula(Sade(1, taban), P), Is.Empty);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Sade(1, ust + 1), P)), Is.EqualTo(new[] { "8.3.1" }));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Sade(1, taban - 1), P)), Is.EqualTo(new[] { "8.3.2" }));

            // Tabanın altı yalnızca en ileri bölümlerde ve istisna olarak kullanılır: orada uyarıdır.
            var ileri = BolumDogrulayici.Dogrula(Sade(kirilma, taban - 1), P);
            Assert.That(Kurallar(ileri), Is.Empty);
            Assert.That(Kurallar(ileri, SorunDuzeyi.Uyari), Is.EqualTo(new[] { "8.3.2" }));
        }

        [Test]
        [Property("GDD", "8.5.2")]
        public void Bir_bolumde_engel_turu_sayisi_sinirlidir()
        {
            int azami = Tam(TasarimParametreAdlari.BolumAzamiEngelTuru);
            Assert.That(azami, Is.EqualTo(4), "Test beş türle kurulu.");

            string engeller = Blok
                + @", ""sb"": { ""arketip"": ""sert_blok"" }, ""dk"": { ""arketip"": ""dusen_kirilabilir"" }, ""cv"": { ""arketip"": ""coklu_vurus"" }";
            var satirlar = Satirlar();
            satirlar[8] = "kb:1 sb:1 dk:1 cv:1 ? ? ? ? ?";
            var dort = Oku(Json(100, hucreler: satirlar, hedefler: BlokHedefi, engeller: engeller));
            var bes = Oku(Json(
                100, hucreler: satirlar, hedefler: BlokHedefi, engeller: engeller, katmanlar: Kilit,
                ustKatmanlar: Satirlar(kod: ".", sonSatir: ".").Select((s, i) => i == 0 ? "k:1 . . . . . . . ." : s).ToArray()));

            Assert.That(BolumDogrulayici.Olc(dort).EngelTurleri, Has.Count.EqualTo(4));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(dort, P)), Is.Empty);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(bes, P)), Is.EqualTo(new[] { "8.5.2" }));
        }

        [Test]
        [Property("GDD", "8.4.1")]
        public void Erken_donemde_hedef_turu_sayisi_sinirlidir()
        {
            int erkenSon = Tam(TasarimParametreAdlari.ErkenDonemSonu);
            Assert.That(Tam(TasarimParametreAdlari.ErkenAzamiHedefTuru), Is.EqualTo(3), "Test dört hedefle kurulu.");
            string dortHedef = @"{ ""tur"": ""renk:0"", ""adet"": 5 }, { ""tur"": ""renk:1"", ""adet"": 5 }, "
                + @"{ ""tur"": ""renk:2"", ""adet"": 5 }, { ""tur"": ""renk:3"", ""adet"": 5 }";

            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(erkenSon, hedefler: dortHedef)), P)), Is.EqualTo(new[] { "8.4.1" }));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(erkenSon + 1, hedefler: dortHedef)), P)), Is.Empty);
        }

        [Test]
        [Property("GDD", "8.4.5, 7.1.2")]
        public void Hedef_adedi_tahtadaki_ogelerle_tutarli_olmalidir()
        {
            List<string> Hatalar(string hedefler, string ek = "") => Kurallar(BolumDogrulayici.Dogrula(
                Oku(Json(5, hucreler: Satirlar(sonSatir: "kb:1"), hedefler: hedefler, engeller: Blok, ek: ek)), P));

            Assert.That(Hatalar(@"{ ""tur"": ""blok"", ""adet"": 9 }"), Is.Empty, "Tahtada 9 blok var.");
            Assert.That(Hatalar(@"{ ""tur"": ""blok"", ""adet"": 10 }"), Is.EqualTo(new[] { "8.4.5" }));

            // Eksik kısım girişlerden gelen dolgu setleriyle sağlanır (5.3.4).
            string dolgu = @", ""dolguSetleri"": [{ ""pas"": 10, ""ogeler"": [{ ""kod"": ""kb:1"", ""agirlik"": 1 }] }]";
            Assert.That(Hatalar(@"{ ""tur"": ""blok"", ""adet"": 30 }", dolgu), Is.Empty);

            Assert.That(Hatalar(@"{ ""tur"": ""kutu"", ""adet"": 3 }"), Is.EqualTo(new[] { "8.4.5" }), "Böyle bir hedef türü tanımlı değil.");
            Assert.That(Hatalar(@"{ ""tur"": ""renk:0"", ""adet"": 0 }"), Is.EqualTo(new[] { "7.1.2" }));
            Assert.That(Hatalar(@"{ ""tur"": ""renk:9"", ""adet"": 5 }"), Is.EqualTo(new[] { "7.1.2" }));

            // "Hepsi" diyen hedefin tahtada karşılığı yoksa bölüm anında biter.
            Assert.That(
                Kurallar(BolumDogrulayici.Dogrula(Oku(Json(5, hedefler: BlokHedefi, engeller: Blok)), P)),
                Is.EqualTo(new[] { "8.4.5" }));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(5, hedefler: string.Empty)), P)), Is.EqualTo(new[] { "7.1.1" }));
        }

        [Test]
        [Property("GDD", "8.9.4")]
        public void Cok_ekranli_bolum_acilis_bolumunden_once_kullanilamaz()
        {
            var ornek = Oku(ProjeDosyalari.Oku("veri", "bolumler", "bolum_006.json"));
            Assert.That(ornek.CokEkranli, Is.True);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(ornek, P)), Does.Contain("8.9.4"));

            var acik = P.Degistir(TasarimParametreAdlari.AcilisCokEkranli, 6);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(ornek, acik)), Does.Not.Contain("8.9.4"));
        }

        [Test]
        [Property("GDD", "8.6.5")]
        public void Hazir_ozel_tas_baslangic_takviyeleri_acilmadan_uyaridir()
        {
            int acilis = Tam(MetaParametreAdlari.AcilisBaslangicTakviyeleri);
            var satirlar = Satirlar();
            satirlar[4] = "? ? ? ? B ? ? ? ?";

            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Oku(Json(acilis - 1, hucreler: satirlar)), P), SorunDuzeyi.Uyari), Is.EqualTo(new[] { "8.6.5" }));
            Assert.That(BolumDogrulayici.Dogrula(Oku(Json(acilis, hucreler: satirlar)), P), Is.Empty);
        }

        [Test]
        [Property("GDD", "8.1.4, 8.1.5, 8.3.4, 8.3.5")]
        public void Olculer_hucre_paylarini_ve_hedef_hamle_oranini_verir()
        {
            var satirlar = Satirlar(sonSatir: "kb:2");
            satirlar[0] = "X _ _ ? ? ? ? ? X";
            var olcu = BolumDogrulayici.Olc(Oku(Json(
                5, hamle: 20, hucreler: satirlar, hedefler: BlokHedefi + ", " + ZeminHedefi, engeller: Blok, katmanlar: Zemin,
                altKatmanlar: Satirlar(kod: ".", sonSatir: "z:1"))));

            Assert.That(olcu.ToplamHucre, Is.EqualTo(81));
            Assert.That(olcu.OynanmayanHucre, Is.EqualTo(2));
            Assert.That(olcu.BosBaslayanHucre, Is.EqualTo(2));
            Assert.That(olcu.EngelliHucre, Is.EqualTo(9), "Blok ve zemin aynı hücrede bir kez sayılır.");
            Assert.That(olcu.EngelYogunlugu, Is.EqualTo(9 / 79d));
            Assert.That(olcu.HedefAdedi, Is.EqualTo(18), "9 blok + 9 zemin.");
            Assert.That(olcu.HedefHamleOrani, Is.EqualTo(0.9));
            Assert.That(olcu.EngelTurleri, Is.EquivalentTo(new[] { "engel:KatmanliBlok", "katman:Zemin" }));
        }

        // ------------------------------------------------------------------ 2.2 Tanıtım takvimi

        [Test]
        [Property("GDD", "8.6.1, 8.6.3, 8.6.7")]
        public void Tanitim_takvimi_erken_donemde_duzenli_sonra_blok_basinda_gelir()
        {
            var takvim = new TanitimTakvimi(P, _ => false);
            Assert.That(takvim.Bolumler(130), Is.EqualTo(new[] { 5, 13, 21, 29, 37, 45, 53, 61, 69, 77, 81, 121 }), "Öneri değerlerle.");

            // Bonus ya da kurtarma bölümüne denk gelen tanıtım bir sonraki olağan bölüme kayar.
            var gercek = new TanitimTakvimi(P, new BonusTakvimi(P), new KurtarmaTakvimi(P));
            Assert.That(gercek.Bolumler(50), Is.EqualTo(new[] { 5, 13, 21, 29, 38, 45 }), "37. bölüm kurtarma bölümü.");
            Assert.That(gercek.TanitimBolumuMu(38), Is.True);
            Assert.That(gercek.TanitimBolumuMu(37), Is.False);
        }

        private static List<BolumVerisi> Paket(int son, Func<int, BolumVerisi?>? ozel = null) =>
            Enumerable.Range(1, son).Select(i => ozel?.Invoke(i) ?? Sade(i)).ToList();

        [Test]
        [Property("GDD", "8.6.1, 8.6.2")]
        public void Takvime_uyan_tanitimlar_hata_vermez()
        {
            var paket = Paket(20, i => i == 5 ? Bloklu(5) : i == 13 ? Zeminli(13) : i == 14 ? Bloklu(14) : null);

            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(paket, P)), Is.Empty);
        }

        [Test]
        [Property("GDD", "8.6.1")]
        public void Takvim_disinda_gelen_yeni_mekanik_hatadir_bos_gecen_tanitim_uyaridir()
        {
            var sorunlar = BolumDogrulayici.PaketiDogrula(Paket(8, i => i == 6 ? Bloklu(6) : null), P);

            Assert.That(sorunlar.Single(s => s.Duzey == SorunDuzeyi.Hata).Yer, Is.EqualTo("Bölüm 6"));
            Assert.That(Kurallar(sorunlar), Is.EqualTo(new[] { "8.6.1, 8.6.3" }));
            Assert.That(
                sorunlar.Where(s => s.Duzey == SorunDuzeyi.Uyari && s.Kural == "8.6.1, 8.6.3").Select(s => s.Yer),
                Is.EqualTo(new[] { "Bölüm 5" }));
        }

        [Test]
        [Property("GDD", "8.6.2, 8.4.4")]
        public void Tanitim_bolumu_tek_mekanik_ogretir_ve_yeni_engel_hedefler_arasindadir()
        {
            // İki yeni mekanik birden.
            var iki = Oku(Json(
                5, hucreler: Satirlar(sonSatir: "kb:1"), hedefler: BlokHedefi + ", " + ZeminHedefi, engeller: Blok, katmanlar: Zemin,
                altKatmanlar: Satirlar(kod: ".", sonSatir: "z:1")));
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(Paket(5, i => i == 5 ? iki : null), P)), Is.EqualTo(new[] { "8.6.2" }));

            // Yeni engel tahtada ama hedefte değil.
            var hedefsiz = Oku(Json(5, hucreler: Satirlar(sonSatir: "kb:1"), engeller: Blok));
            Assert.That(
                Kurallar(BolumDogrulayici.PaketiDogrula(Paket(5, i => i == 5 ? hedefsiz : null), P)),
                Is.EqualTo(new[] { "8.6.2, 8.4.4" }));
        }

        [Test]
        [Property("GDD", "8.6.2a")]
        public void Engelleyici_katman_ve_uretici_yalnizca_tahtada_tanitilabilir()
        {
            // Kilit hedef olmaz; yalnızca tahtada tanıtılır.
            var kilitli = Oku(Json(5, katmanlar: Kilit, ustKatmanlar: Satirlar(kod: ".", sonSatir: "k:1")));
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(Paket(5, i => i == 5 ? kilitli : null), P)), Is.Empty);

            // Üreticide hedef, ürettiği öğedir.
            var satirlar = Satirlar();
            satirlar[8] = "? ? ? ? u:1 ? ? ? ?";
            var uretici = Oku(Json(
                13, hucreler: satirlar, hedefler: @"{ ""tur"": ""zemin"", ""adet"": 3 }",
                engeller: @"""u"": { ""arketip"": ""uretici"", ""birakilanKatman"": ""z"" }", katmanlar: Zemin));
            var sorunlar = BolumDogrulayici.PaketiDogrula(Paket(13, i => i == 5 ? Zeminli(5) : i == 13 ? uretici : null), P);
            Assert.That(Kurallar(sorunlar), Is.Empty);
        }

        [Test]
        [Property("GDD", "8.6.4")]
        public void Yeni_mekanik_arac_acilis_bolumunde_tanitilamaz()
        {
            var paket = Paket(13, i => i == 5 ? Bloklu(5) : i == 13 ? Zeminli(13) : null);
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(paket, P)), Is.Empty);

            var cakisan = P.Degistir(MetaParametreAdlari.AcilisKaristirmaAraci, 13);
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(paket, cakisan)), Is.EqualTo(new[] { "8.6.4" }));
        }

        [Test]
        [Property("GDD", "8.10, 8.11.2")]
        public void Paketteki_bolum_numaralari_tekrarsiz_ve_araliksiz_olmalidir()
        {
            var eksik = new List<BolumVerisi> { Sade(1), Sade(2), Sade(4) };
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(eksik, P)), Is.EqualTo(new[] { "8.11.2" }));

            var cift = new List<BolumVerisi> { Sade(1), Sade(2), Sade(2) };
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(cift, P)), Is.EqualTo(new[] { "8.10" }));

            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(new List<BolumVerisi>(), P)), Is.EqualTo(new[] { "8.11.2" }));
        }

        // ------------------------------------------------------------------ 2.3 Bonus bölümleri

        [Test]
        [Property("GDD", "8.7.1, 8.7.3")]
        public void Bonus_bolumleri_acilistan_sonra_erken_kisa_gec_uzun_aralikla_gelir()
        {
            var takvim = new BonusTakvimi(P);

            Assert.That(takvim.Bolumler(130), Is.EqualTo(new[] { 25, 50, 75, 100, 125 }), "Öneri değerlerle.");
            Assert.That(takvim.Bolumler(800).Where(b => b >= 450), Is.EqualTo(new[] { 450, 475, 500, 620, 740 }));
            Assert.That(takvim.BonusMu(25), Is.True);
            Assert.That(takvim.BonusMu(24), Is.False);
            Assert.That(takvim.BonusMu(1), Is.False);
        }

        private static BolumVerisi BonusBolumu(int kimlik, string hedefler = @"{ ""tur"": ""para"", ""adet"": 0 }", int hamle = 12) =>
            Oku(Json(kimlik, hamle, Satirlar(sonSatir: "pt:1"), hedefler, @"""pt"": { ""arketip"": ""para_tasi"", ""hedefTuru"": ""para"" }"));

        [Test]
        [Property("GDD", "8.7.1, 8.7.2")]
        public void Bonus_bolumu_tek_hedeflidir_ve_zorluk_egrisinin_disindadir()
        {
            // Hamle bandı bonus bölümüne uygulanmaz.
            Assert.That(BolumDogrulayici.Dogrula(BonusBolumu(25), P, BolumRolu.Bonus), Is.Empty);
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(BonusBolumu(25), P)), Is.EqualTo(new[] { "8.3.2" }), "Olağan bölüm olsaydı hamle tabanın altında.");

            Assert.That(
                Kurallar(BolumDogrulayici.Dogrula(BonusBolumu(25, @"{ ""tur"": ""para"", ""adet"": 5 }"), P, BolumRolu.Bonus)),
                Is.EqualTo(new[] { "8.7.1" }));
            Assert.That(Kurallar(BolumDogrulayici.Dogrula(Sade(25), P, BolumRolu.Bonus)), Is.EqualTo(new[] { "8.7.1" }));
        }

        [Test]
        [Property("GDD", "8.7.1, 8.7.2")]
        public void Pakette_bonus_sirasi_bonus_bolumu_ister_ve_para_tasi_tanitim_ritmine_girmez()
        {
            var tanitimlar = new Dictionary<int, Func<int, BolumVerisi>> { { 5, i => Bloklu(i) }, { 13, i => Zeminli(i) } };
            BolumVerisi? Ozel(int i) => tanitimlar.TryGetValue(i, out var kur) ? kur(i) : null;

            var dogru = Paket(25, i => i == 25 ? BonusBolumu(25) : Ozel(i));
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(dogru, P)), Is.Empty);

            var yanlis = Paket(25, Ozel);
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(yanlis, P)), Is.EqualTo(new[] { "8.7.1" }));
        }

        [Test]
        [Property("GDD", "8.7.2, 9.11.3")]
        public void Bonus_bolumu_beceri_olcumune_girmez()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 25 };
            Oyna(oyuncu, 25, kazan: true, akis => akis.Bonus = true);

            Assert.That(oyuncu.EnYuksekBolum, Is.EqualTo(26));
            Assert.That(oyuncu.Zorluk.OlcumBolumu, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ 2.4 Zor bölüm ritmi

        [Test]
        [Property("GDD", "8.8.3")]
        public void Her_blogun_son_bolumu_zor_bolumdur()
        {
            var ritim = new ZorBolumRitmi(P);
            int aralik = Tam(TasarimParametreAdlari.ZorBolumAraligi);

            Assert.That(ritim.ZorMu(aralik), Is.True);
            Assert.That(ritim.ZorMu(aralik - 1), Is.False);
            Assert.That(ritim.ZorMu(aralik * 7), Is.True);
            Assert.That(ritim.Blok(aralik), Is.EqualTo(1));
            Assert.That(ritim.Blok(aralik + 1), Is.EqualTo(2));
        }

        [Test]
        [Property("GDD", "8.8.3")]
        public void Zor_bolum_blogundaki_digerlerinden_az_hamle_vermezse_uyari_verilir()
        {
            int aralik = Tam(TasarimParametreAdlari.ZorBolumAraligi);
            int fark = Tam(TasarimParametreAdlari.ZorBolumHamleFarki);

            var duz = BolumDogrulayici.PaketiDogrula(Paket(aralik), P);
            Assert.That(duz.Where(s => s.Kural == "8.8.3").Select(s => s.Yer), Is.EqualTo(new[] { "Bölüm " + aralik }));
            Assert.That(Kurallar(duz), Is.Empty, "Ritim uyarıdır, hata değil.");

            var ritimli = BolumDogrulayici.PaketiDogrula(Paket(aralik, i => i == aralik ? Sade(i, 25 - fark) : null), P);
            Assert.That(ritimli.Where(s => s.Kural == "8.8.3"), Is.Empty);
        }

        // ------------------------------------------------------------------ Paket ortalamaları

        [Test]
        [Property("GDD", "8.1.4, 8.3.3, 8.3.5, 8.4.3")]
        public void Paket_ortalamalari_parametre_bantlarina_karsi_denetlenir()
        {
            // 12 engelsiz, renk hedefli, oynanmayan hücresi çok bölüm: dört ortalama da bandın dışında.
            var satirlar = Satirlar();
            satirlar[0] = satirlar[1] = satirlar[2] = "X X X ? ? ? X X X";
            var paket = Enumerable.Range(1, 12).Select(i => Oku(Json(i, hamle: 35, hucreler: satirlar))).ToList();

            var uyarilar = BolumDogrulayici.PaketiDogrula(paket, P).Where(s => s.Yer == "Paket").Select(s => s.Kural).ToList();
            Assert.That(uyarilar, Does.Contain("8.1.4"));
            Assert.That(uyarilar, Does.Contain("8.3.3"));
            Assert.That(uyarilar, Does.Contain("8.3.5"));
            Assert.That(uyarilar, Does.Contain("8.4.3"));

            // Az bölümlü kümede ortalama denetlenmez.
            Assert.That(BolumDogrulayici.PaketiDogrula(paket.Take(4), P).Where(s => s.Yer == "Paket"), Is.Empty);
        }

        // ------------------------------------------------------------------ 2.6 Bölüm paketleri

        [Test]
        [Property("GDD", "8.11.2")]
        public void Bolumler_sabit_uzunlukta_paketlere_ayrilir()
        {
            var duzen = new PaketDuzeni(P);
            int uzunluk = Tam(TasarimParametreAdlari.BolumPaketiUzunlugu);

            Assert.That(duzen.Paket(1), Is.EqualTo(1));
            Assert.That(duzen.Paket(uzunluk), Is.EqualTo(1));
            Assert.That(duzen.Paket(uzunluk + 1), Is.EqualTo(2));
            Assert.That(duzen.IlkBolum(2), Is.EqualTo(uzunluk + 1));
            Assert.That(duzen.SonBolum(2), Is.EqualTo(uzunluk * 2));
            Assert.That(PaketDuzeni.HaritaGorseli(2), Is.EqualTo("harita_002"), "Her paketin kendi harita görseli vardır.");
        }

        private static BolumPaketiVerisi PaketVerisi(int paket, int surum, params int[] bolumler)
        {
            var veri = new BolumPaketiVerisi(paket, surum);
            foreach (int bolum in bolumler)
            {
                veri.Bolumler[bolum] = Json(bolum, hamle: 20 + surum);
            }

            return veri;
        }

        [Test]
        [Property("GDD", "8.11.1, 8.8.1a")]
        public void Paket_metne_cevrilip_geri_okunur()
        {
            var paket = PaketVerisi(1, 3, 1, 2);
            paket.KurtarmaBolumleri[1] = Json(1);
            paket.Etiketler[2] = ZorlukEtiketi.CokZor;

            var okunan = BolumPaketiVerisi.Oku(paket.Yaz());

            Assert.That(okunan.Paket, Is.EqualTo(1));
            Assert.That(okunan.Surum, Is.EqualTo(3));
            Assert.That(okunan.Bolumler, Is.EqualTo(paket.Bolumler));
            Assert.That(okunan.KurtarmaBolumleri, Is.EqualTo(paket.KurtarmaBolumleri));
            Assert.That(okunan.Etiketler, Is.EqualTo(paket.Etiketler));
            Assert.That(okunan.Yaz(), Is.EqualTo(paket.Yaz()));

            Assert.That(() => BolumPaketiVerisi.Oku("{ \"paket\": 1 }"), Throws.TypeOf<BolumVerisiHatasi>());
            Assert.That(() => BolumPaketiVerisi.Oku("bozuk"), Throws.TypeOf<BolumVerisiHatasi>());
            Assert.That(
                () => BolumPaketiVerisi.Oku("{ \"paket\": 1, \"surum\": 1, \"etiketler\": { \"2\": \"imkansiz\" } }"),
                Throws.TypeOf<BolumVerisiHatasi>());
        }

        [Test]
        [Property("GDD", "8.11.1, 8.11.2, 8.8.1a")]
        public void Kitaplik_kurulu_paketlerin_bolumlerini_ve_etiketlerini_verir()
        {
            var kitaplik = new BolumKitapligi(P);
            var paket = PaketVerisi(1, 1, 1, 2, 3);
            paket.Etiketler[3] = ZorlukEtiketi.Zor;
            paket.KurtarmaBolumleri[1] = Json(1, hamle: 19);

            Assert.That(kitaplik.Kur(paket), Is.True);
            Assert.That(kitaplik.Kur(PaketVerisi(1, 1, 1)), Is.False, "Aynı sürüm yeniden kurulmaz.");

            Assert.That(kitaplik.Bolum(2)!.Kimlik, Is.EqualTo(2));
            Assert.That(kitaplik.Bolum(4), Is.Null);
            Assert.That(kitaplik.KurtarmaBolumu(1)!.Hamle, Is.EqualTo(19));
            Assert.That(kitaplik.KurtarmaBolumu(2), Is.Null);
            Assert.That(kitaplik.EtiketTablosu().Etiket(3), Is.EqualTo(ZorlukEtiketi.Zor));
            Assert.That(kitaplik.YayimlananEnYuksekBolum, Is.EqualTo(3), "Sunucu bildirmediyse kurulu en yüksek bölüm.");
            Assert.That(kitaplik.Erisim(3), Is.EqualTo(BolumErisimi.Hazir));
            Assert.That(kitaplik.Erisim(4), Is.EqualTo(BolumErisimi.Yayimlanmadi));
        }

        [Test]
        [Property("GDD", "8.11.2")]
        public void Bozuk_ya_da_araligi_disinda_bolum_tasiyan_paket_kurulmaz()
        {
            var kitaplik = new BolumKitapligi(P);
            kitaplik.Kur(PaketVerisi(1, 1, 1));
            int uzunluk = Tam(TasarimParametreAdlari.BolumPaketiUzunlugu);

            Assert.That(() => kitaplik.Kur(PaketVerisi(1, 2, uzunluk + 1)), Throws.TypeOf<BolumVerisiHatasi>());

            var bozuk = PaketVerisi(1, 2, 1);
            bozuk.Bolumler[2] = "{ \"kimlik\": 2 }";
            Assert.That(() => kitaplik.Kur(bozuk), Throws.TypeOf<BolumVerisiHatasi>());

            var yanlisKimlik = PaketVerisi(1, 2, 1);
            yanlisKimlik.Bolumler[2] = Json(7);
            Assert.That(() => kitaplik.Kur(yanlisKimlik), Throws.TypeOf<BolumVerisiHatasi>());

            Assert.That(kitaplik.KuruluPaketler[1], Is.EqualTo(1), "Eski paket yerinde kalır.");
        }

        [Test]
        [Property("GDD", "8.11.1, 8.11.2")]
        public void Sunucu_yeni_paket_ekler_var_olani_gunceller_ve_yayimlanan_bolumu_bildirir()
        {
            var sunucu = new YerelSahteSunucu(new AyarliSaat(Simdi));
            var kitaplik = new BolumKitapligi(P);
            int uzunluk = Tam(TasarimParametreAdlari.BolumPaketiUzunlugu);
            kitaplik.Kur(PaketVerisi(1, 1, 1, 2));

            // Sunucu ikinci paketin bölümlerini yayımladı ama paket henüz sunucuda yok.
            sunucu.Ayarlar.YayimlananEnYuksekBolum = uzunluk + 2;
            Assert.That(kitaplik.Esitle(sunucu), Is.Empty);
            Assert.That(kitaplik.Erisim(uzunluk + 1), Is.EqualTo(BolumErisimi.PaketBekleniyor));
            Assert.That(kitaplik.Erisim(uzunluk + 3), Is.EqualTo(BolumErisimi.Yayimlanmadi));

            // Yeni paket ve birinci paketin güncellemesi.
            sunucu.PaketYayimla(PaketVerisi(2, 1, uzunluk + 1, uzunluk + 2));
            sunucu.PaketYayimla(PaketVerisi(1, 2, 1, 2));
            var kurulan = kitaplik.Esitle(sunucu);

            Assert.That(kurulan.Select(k => k.Paket), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(kitaplik.Erisim(uzunluk + 1), Is.EqualTo(BolumErisimi.Hazir));
            Assert.That(kitaplik.Bolum(1)!.Hamle, Is.EqualTo(22), "Var olan bölüm güncellendi.");
            Assert.That(kitaplik.Esitle(sunucu), Is.Empty, "Sürümler aynıyken yeniden indirilmez.");
        }

        [Test]
        [Property("GDD", "8.11.1")]
        public void Sunucuya_ulasilamazsa_ya_da_paket_bozuksa_kurulu_paketler_kalir()
        {
            var sunucu = new YerelSahteSunucu(new AyarliSaat(Simdi)) { Cevrimdisi = true };
            var kitaplik = new BolumKitapligi(P);
            kitaplik.Kur(PaketVerisi(1, 1, 1));
            sunucu.PaketYayimla(PaketVerisi(1, 5, 1));

            Assert.That(kitaplik.Esitle(sunucu), Is.Empty);
            Assert.That(kitaplik.KuruluPaketler[1], Is.EqualTo(1));

            sunucu.Cevrimdisi = false;
            var bozuk = PaketVerisi(1, 6, 1);
            bozuk.Bolumler[2] = "bozuk";
            sunucu.PaketYayimla(bozuk);
            Assert.That(kitaplik.Esitle(sunucu), Is.Empty);
            Assert.That(kitaplik.KuruluPaketler[1], Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ 2.7 Kurtarma bölümleri

        [Test]
        [Property("GDD", "10.2.1, 10.2.2")]
        public void Kurtarma_bolumleri_erken_sik_gec_seyrek_ve_ayni_blok_sirasinda_gelir()
        {
            var takvim = new KurtarmaTakvimi(P);

            var erken = takvim.Eslemeler(100).ToList();
            Assert.That(erken.Select(e => e.IlerlemeBolumu), Is.EqualTo(new[] { 12, 37, 62, 87 }), "Öneri değerlerle.");
            Assert.That(erken.Select(e => e.KurtarmaNo), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(takvim.Bagli(37)!.KurtarmaNo, Is.EqualTo(2));
            Assert.That(takvim.Bagli(36), Is.Null);

            int gec = Tam(TasarimParametreAdlari.KurtarmaGecDonemBaslangici);
            int aralik = Tam(TasarimParametreAdlari.KurtarmaAraligiGec);
            var sonrakiler = takvim.Eslemeler(gec + (aralik * 3)).Select(e => e.IlerlemeBolumu).Where(b => b >= gec).ToList();
            Assert.That(sonrakiler, Is.EqualTo(new[] { gec, gec + aralik, gec + (aralik * 2), gec + (aralik * 3) }));
            Assert.That(sonrakiler.Select(b => b % aralik).Distinct().Count(), Is.EqualTo(1), "Her bloğun aynı sıradaki bölümü.");
        }

        [Test]
        [Property("GDD", "10.2.3")]
        public void Ana_tema_payi_kadar_kullanilir_diger_temalar_sirayla_doner()
        {
            var temalar = new KurtarmaTakvimi(P).Eslemeler(400).Select(e => e.Tema).ToList();

            Assert.That(temalar.Take(10), Is.EqualTo(new[] { 0, 1, 2, 0, 3, 4, 0, 5, 1, 0 }), "Öneri değerlerle: pay 0.33, 5 tema.");
            Assert.That(temalar.Count(t => t == 0) / (double)temalar.Count, Is.EqualTo(Tam2(TasarimParametreAdlari.KurtarmaAnaTemaPayi)).Within(0.05));
            Assert.That(temalar.Max(), Is.EqualTo(Tam(TasarimParametreAdlari.KurtarmaTemaSayisi)));

            var anasiz = new KurtarmaTakvimi(P.Degistir(TasarimParametreAdlari.KurtarmaAnaTemaPayi, 0)).Eslemeler(200).Select(e => e.Tema);
            Assert.That(anasiz.Take(6), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 1 }));

            var yalnizAna = new KurtarmaTakvimi(P.Degistir(TasarimParametreAdlari.KurtarmaTemaSayisi, 0)).Eslemeler(200).Select(e => e.Tema);
            Assert.That(yalnizAna, Is.All.EqualTo(KurtarmaTakvimi.AnaTema));
        }

        private static double Tam2(string ad) => P.Sayi(ad);

        [Test]
        [Property("GDD", "10.2.6")]
        public void Deney_grubunda_ilk_kurtarma_bolumleri_bastaki_bolumlere_cekilir()
        {
            var ayarlar = new SunucuYapilandirmasi();
            ayarlar.OneCekilenKurtarmaBolumleri.AddRange(new[] { 2, 5 });
            var takvim = new KurtarmaTakvimi(P, ayarlar.OneCekilenKurtarmaBolumleri);

            var eslemeler = takvim.Eslemeler(100).ToList();
            Assert.That(eslemeler.Select(e => e.IlerlemeBolumu), Is.EqualTo(new[] { 2, 5, 62, 87 }));
            Assert.That(eslemeler.Select(e => e.KurtarmaNo), Is.EqualTo(new[] { 1, 2, 3, 4 }), "Kurtarma bölümleri ve temaları aynı kalır.");
            Assert.That(takvim.Bagli(12), Is.Null);

            Assert.That(() => new KurtarmaTakvimi(P, new[] { 5, 2 }), Throws.ArgumentException);
            Assert.That(() => new KurtarmaTakvimi(P, new[] { 12 }), Throws.ArgumentException, "Öne çekilen bölüm olağan yerinden önce olmalı.");
        }

        [Test]
        [Property("GDD", "10.2.1, 10.2.5")]
        public void Kurtarma_akisi_tema_paketi_yoksa_bekler_ya_da_yedek_bolume_gecer()
        {
            var inenTemalar = new HashSet<int>();
            var akis = new KurtarmaAkisi(new KurtarmaTakvimi(P), inenTemalar.Contains);

            Assert.That(akis.BolumeGel(11), Is.EqualTo(KurtarmaBolumuDurumu.NormalAkis));
            Assert.That(akis.Esleme, Is.Null);

            // 12. bölüm ana temadadır; ana tema uygulamayla gelir.
            Assert.That(akis.BolumeGel(12), Is.EqualTo(KurtarmaBolumuDurumu.KurtarmaBolumu));
            Assert.That(akis.Esleme!.KurtarmaNo, Is.EqualTo(1));
            Assert.That(KurtarmaAkisi.TemaPaketi(akis.Esleme.Tema), Is.Null);

            // 37. bölüm dönüşümlü temadadır; paketi inmemiş.
            Assert.That(akis.BolumeGel(37), Is.EqualTo(KurtarmaBolumuDurumu.PaketBekleniyor));
            Assert.That(KurtarmaAkisi.TemaPaketi(akis.Esleme!.Tema), Is.EqualTo("kurtarma_tema_1"));
            Assert.That(akis.KurtarmaOynanir, Is.False);
            Assert.That(akis.PaketIndi(), Is.False, "Paket hâlâ yok.");

            inenTemalar.Add(1);
            Assert.That(akis.PaketIndi(), Is.True);
            Assert.That(akis.Durum, Is.EqualTo(KurtarmaBolumuDurumu.KurtarmaBolumu));
            Assert.That(akis.YedegiKullan(), Is.False, "Kurtarma bölümü hazırken yedeğe geçilmez.");

            // Paket beklenmezse yedek bölüm oynanır.
            inenTemalar.Clear();
            Assert.That(akis.BolumeGel(37), Is.EqualTo(KurtarmaBolumuDurumu.PaketBekleniyor));
            Assert.That(akis.YedegiKullan(), Is.True);
            Assert.That(akis.Durum, Is.EqualTo(KurtarmaBolumuDurumu.YedekBolum));
            Assert.That(akis.KurtarmaOynanir, Is.False);
        }

        private const string KazanilanBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 5,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3 }] }";

        private const string KaybedilenBolum = @"{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": 3, ""hamle"": 1,
            ""renkler"": 4, ""hucreler"": [""r0 r0 r1 r0"", ""r2 r3 r2 r3"", ""r2 r3 r2 r3""],
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 99 }] }";

        private static BolumSonucu Oyna(Oyuncu oyuncu, int bolum, bool kazan, Action<BolumAkisi> hazirla)
        {
            var akis = new BolumAkisi(oyuncu, new ZorlukEtiketTablosu(), bolum);
            hazirla(akis);
            var olaylar = new List<Olay>();
            var oturum = akis.Baslat(
                Oku(kazan ? KazanilanBolum : KaybedilenBolum), AkBayraklari.Varsayilan, 7, Simdi, Enumerable.Empty<TakviyeTuru>(), olaylar);
            Assert.That(oturum, Is.Not.Null);
            Assert.That(oturum!.Kaydir(H(1, 4), H(1, 3), olaylar), Is.EqualTo(HamleSonucu.Gecerli));
            if (!kazan)
            {
                Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
                oturum.EkHamleTeklifiniReddet(olaylar);
            }

            return akis.SonucuIsle(Simdi);
        }

        [Test]
        [Property("GDD", "10.2.4, 10.2.8")]
        public void Kurtarma_bolumu_kazanilinca_para_verir_ilerleme_surer_kayipta_can_dusmez_ve_tekrarlanir()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 12 };
            var kurtarma = new KurtarmaAkisi(new KurtarmaTakvimi(P), _ => true);
            Assert.That(kurtarma.BolumeGel(oyuncu.EnYuksekBolum), Is.EqualTo(KurtarmaBolumuDurumu.KurtarmaBolumu));
            int can = oyuncu.Can.Mevcut(Simdi);
            int para = oyuncu.Cuzdan.Bakiye;

            // Kayıp: can düşmez, oyuncu aynı bölümde kalır ve aynı kurtarma bölümü yeniden gelir.
            var kayip = Oyna(oyuncu, 12, kazan: false, kurtarma.Uygula);
            Assert.That(kayip.CanDustu, Is.False);
            Assert.That(oyuncu.Can.Mevcut(Simdi), Is.EqualTo(can));
            Assert.That(oyuncu.EnYuksekBolum, Is.EqualTo(12));
            Assert.That(kurtarma.BolumeGel(oyuncu.EnYuksekBolum), Is.EqualTo(KurtarmaBolumuDurumu.KurtarmaBolumu));
            Assert.That(kurtarma.Esleme!.KurtarmaNo, Is.EqualTo(1));

            // Kazanım: sabit para, normal ilerleme devam eder.
            var kazanim = Oyna(oyuncu, 12, kazan: true, kurtarma.Uygula);
            Assert.That(kazanim.Para, Is.EqualTo(Tam(TasarimParametreAdlari.KurtarmaKazanmaParasi)));
            Assert.That(oyuncu.Cuzdan.Bakiye, Is.EqualTo(para + kazanim.Para));
            Assert.That(oyuncu.EnYuksekBolum, Is.EqualTo(13));
            Assert.That(kurtarma.BolumeGel(oyuncu.EnYuksekBolum), Is.EqualTo(KurtarmaBolumuDurumu.NormalAkis));
        }

        [Test]
        [Property("GDD", "10.2.5")]
        public void Yedek_bolum_olagan_bolum_gibi_oynanir()
        {
            var oyuncu = new Oyuncu(P, Simdi) { EnYuksekBolum = 37 };
            var kurtarma = new KurtarmaAkisi(new KurtarmaTakvimi(P), _ => false);
            kurtarma.BolumeGel(37);
            kurtarma.YedegiKullan();

            var kayip = Oyna(oyuncu, 37, kazan: false, kurtarma.Uygula);
            Assert.That(kayip.CanDustu, Is.True);

            var kazanim = Oyna(oyuncu, 37, kazan: true, kurtarma.Uygula);
            Assert.That(kazanim.Para, Is.EqualTo(EtiketOdulleri.KazanmaParasi(P, ZorlukEtiketi.Normal, 4)));
        }

        [Test]
        [Property("GDD", "10.2.1, 10.2.8, 8.6.2")]
        public void Paket_dogrulamasi_kurtarma_bolumlerini_de_denetler()
        {
            var bolumler = Paket(13, i => i == 5 ? Bloklu(5) : i == 13 ? Zeminli(13) : null);

            // 12. bölüme bağlı kurtarma bölümünün dosyası yok.
            Assert.That(Kurallar(BolumDogrulayici.PaketiDogrula(bolumler, P, new Dictionary<int, BolumVerisi>())), Is.EqualTo(new[] { "10.2.1" }));

            // Tek hedefli ve tanıtılmış mekanik kullanan kurtarma bölümü geçer.
            var gecerli = new Dictionary<int, BolumVerisi> { { 1, Bloklu(1) } };
            Assert.That(BolumDogrulayici.PaketiDogrula(bolumler, P, gecerli).Where(s => s.Yer.StartsWith("Kurtarma")), Is.Empty);

            // İki hedef: hata. 13. bölümde tanıtılan zemini 12. bölümde kullanmak: uyarı.
            var ikiHedef = new Dictionary<int, BolumVerisi>
            {
                {
                    1,
                    Oku(Json(
                        1, hucreler: Satirlar(sonSatir: "kb:1"), hedefler: BlokHedefi + ", " + ZeminHedefi, engeller: Blok,
                        katmanlar: Zemin, altKatmanlar: Satirlar(kod: ".", sonSatir: "z:1")))
                },
            };
            var sorunlar = BolumDogrulayici.PaketiDogrula(bolumler, P, ikiHedef).Where(s => s.Yer == "Kurtarma 1").ToList();
            Assert.That(Kurallar(sorunlar), Is.EqualTo(new[] { "10.2.8" }));
            Assert.That(Kurallar(sorunlar, SorunDuzeyi.Uyari), Is.EqualTo(new[] { "8.6.2" }));

            // Pakette karşılığı olmayan kurtarma dosyası: uyarı.
            var fazla = new Dictionary<int, BolumVerisi> { { 1, Bloklu(1) }, { 9, Bloklu(9) } };
            Assert.That(
                BolumDogrulayici.PaketiDogrula(bolumler, P, fazla).Single(s => s.Yer == "Kurtarma 9").Duzey,
                Is.EqualTo(SorunDuzeyi.Uyari));
        }

        [Test]
        [Property("GDD", "0.4")]
        public void Tasarim_katmaninin_kullandigi_her_parametre_adi_dosyada_vardir()
        {
            var adlar = typeof(TasarimParametreAdlari)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(a => a.IsLiteral)
                .Select(a => (string)a.GetRawConstantValue()!)
                .ToList();

            Assert.That(adlar, Is.Not.Empty);
            Assert.That(adlar.Where(ad => !P.Var(ad)), Is.Empty);
        }
    }
}
