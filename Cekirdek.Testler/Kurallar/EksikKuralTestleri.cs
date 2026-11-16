using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Testler.Ortak;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;
using static CatRoyal.Cekirdek.Testler.Ortak.TahtaKurucu;

namespace CatRoyal.Cekirdek.Testler.Kurallar
{
    /// <summary>
    /// Çok ekranlı bölümler (GDD 2.1.7, 8.9), artan hamle gösterisi (7.6.2, 7.6.3) ve düşme sürerken hamle (2.3.9, 11.3.1).
    /// </summary>
    [TestFixture]
    [Category("Kural")]
    public class EksikKuralTestleri
    {
        private static readonly int[] Tohumlar = { 1, 2, 3, 5, 8, 13, 21, 34 };

        private static ParametreTablosu P => ProjeDosyalari.Parametreler;

        // ------------------------------------------------------------------ Çok ekranlı bölümler (GDD 8.9)

        /// <summary>
        /// İki ekran, üçer satır. 1. ekran: (1,4)→(1,3) kaydırması üç kırmızıyı eşler ve ekranın hedefini bitirir;
        /// boşalan hücreler ön tanımlı dolguyla dolar, kaskad oluşmaz. 2. ekranın en üst satırı boş başlar ve
        /// üst ekrandan kalan taşlarla dolar; (1,2)→(2,2) kaydırması kutunun yanında eşleşir ve bölümü bitirir.
        /// </summary>
        private const string IkiEkran = @"{ ""kimlik"": 0, ""genislik"": 5, ""yukseklik"": 6, ""hamle"": 10, ""renkler"": 4,
            ""bayraklar"": { ""cokEkranli"": true },
            ""ekranlar"": [[1, 3], [4, 6]],
            ""engeller"": { ""k"": { ""arketip"": ""katmanli_blok"", ""hedefTuru"": ""kutu"" } },
            ""hucreler"": [
                ""r0 r0 r1 r0 r2"",
                ""r2 r3 r2 r3 r1"",
                ""r3 r2 r3 r1 r3"",
                ""_ _ _ _ _"",
                ""r2 r1 r2 r0 r2"",
                ""k:1 r3 r1 r3 r1""],
            ""onTanimliDolgu"": { ""1"": [""r1""], ""2"": [""r2""], ""3"": [""r3""] },
            ""hedefler"": [{ ""tur"": ""renk:0"", ""adet"": 3, ""ekran"": 1 }, { ""tur"": ""kutu"", ""adet"": 0, ""ekran"": 2 }] }";

        private static BolumOturumu Kur(string json, int tohum, List<Olay> olaylar) =>
            BolumKurucu.Kur(BolumYukleyici.JsondanOku(json, P), AkBayraklari.Varsayilan, P, (ulong)tohum, olaylar);

        [Test]
        [Property("GDD", "2.1.7, 8.9.1")]
        public void Cok_ekranli_bolum_ilk_ekranla_baslar_ve_yalniz_o_ekranin_hedeflerini_gosterir()
        {
            var oturum = Kur(IkiEkran, 1, new List<Olay>());

            Assert.That(oturum.EkranSayisi, Is.EqualTo(2));
            Assert.That(oturum.GecerliEkran, Is.EqualTo(1));
            Assert.That(oturum.Baglam.Tahta.SatirSayisi, Is.EqualTo(3), "Oyuncu bir anda yalnızca bir ekranı oynar.");
            Assert.That(oturum.Hedefler.Turler, Is.EqualTo(new[] { "renk:0" }));
            Assert.That(oturum.Hedefler.Kalan("kutu"), Is.EqualTo(0), "Diğer ekranın hedefi geçerli ekranda sayılmaz.");
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "8.9.1, 8.9.2, 5.3.3, 5.3.4, 5.1.3")]
        public void Ekranin_hedefleri_bitince_sonraki_ekrana_gecilir_ve_kalan_taslar_giris_sirasina_eklenir(int tohum)
        {
            var olaylar = new List<Olay>();
            var oturum = Kur(IkiEkran, tohum, olaylar);
            var ust = oturum.Baglam.Tahta;
            var altSatir = Enumerable.Range(1, 5).Select(s => ust[3, s].Tas!).ToList();
            olaylar.Clear();

            Assert.That(oturum.Kaydir(H(1, 4), H(1, 3), olaylar), Is.EqualTo(HamleSonucu.Gecerli));

            var gecis = olaylar.OfType<EkranDegisti>().Single();
            Assert.That(gecis.Ekran, Is.EqualTo(2));
            Assert.That(gecis.AktarilanTas, Is.EqualTo(15), "Düşebilen bütün taşlar aktarılır.");
            Assert.That(oturum.GecerliEkran, Is.EqualTo(2));
            Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor), "Bölüm bütün ekranlar bitmeden kazanılmaz.");
            Assert.That(oturum.KalanHamle, Is.EqualTo(9), "Hamle sayacı ekranlar arasında sürer.");

            var alt = oturum.Baglam.Tahta;
            Assert.That(alt, Is.Not.SameAs(ust));
            Assert.That(alt.SatirSayisi, Is.EqualTo(3));

            // GDD 8.9.2, 5.1.3: her sütunda üst ekranın en alttaki taşı önce girer.
            for (int sutun = 1; sutun <= 5; sutun++)
            {
                Assert.That(alt[1, sutun].Tas, Is.SameAs(altSatir[sutun - 1]), $"sütun {sutun}");
            }

            int gecisIndisi = olaylar.FindIndex(o => o is EkranDegisti);
            Assert.That(olaylar.Skip(gecisIndisi).OfType<TasGirdi>().Count(), Is.EqualTo(5), "Yeni ekranın girişleri açılır.");

            var kimlikler = alt.TumKonumlar().Select(k => alt[k].Tas!.Kimlik).ToList();
            Assert.That(kimlikler.Distinct().Count(), Is.EqualTo(kimlikler.Count), "Taş kimlikleri ekranlar arasında çakışmaz.");

            Assert.That(oturum.Hedefler.Turler, Is.EqualTo(new[] { "kutu" }));
            Assert.That(oturum.Hedefler.Kalan("kutu"), Is.EqualTo(1), "Adet 0: ekran açılınca tahtadaki hepsi sayılır.");
        }

        [Test]
        [Property("GDD", "8.9.1, 7.3.1")]
        public void Son_ekranin_hedefleri_de_bitince_bolum_kazanilir()
        {
            var olaylar = new List<Olay>();
            var oturum = Kur(IkiEkran, 1, olaylar);
            oturum.Kaydir(H(1, 4), H(1, 3), olaylar);
            Assert.That(olaylar.OfType<KazanmaBasladi>(), Is.Empty);

            Assert.That(oturum.Kaydir(H(1, 2), H(2, 2), olaylar), Is.EqualTo(HamleSonucu.Gecerli));

            Assert.That(oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(olaylar.OfType<KazanmaBasladi>().Count(), Is.EqualTo(1));
            Assert.That(olaylar.OfType<EkranDegisti>().Count(), Is.EqualTo(1));
        }

        private const string CizgiliIkiEkran = @"{ ""kimlik"": 0, ""genislik"": 3, ""yukseklik"": 6, ""hamle"": 10, ""renkler"": 3,
            ""bayraklar"": { ""cokEkranli"": true },
            ""ekranlar"": [[1, 3], [4, 6]],
            ""engeller"": { ""k"": { ""arketip"": ""katmanli_blok"", ""hedefTuru"": ""kutu"" } },
            ""hucreler"": [
                ""| r0 r1"",
                ""r1 r2 r0"",
                ""r2 r1 r2"",
                ""k:1 r0 r1"",
                ""r1 r2 r0"",
                ""r2 r1 r2""],
            ""hedefler"": [{ ""tur"": ""renk:2"", ""adet"": 1, ""ekran"": 1 }, { ""tur"": ""kutu"", ""adet"": 0, ""ekran"": 2 }] }";

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "2.1.7, 8.9.3")]
        public void Sutun_etkisi_gecerli_ekranin_sinirinda_durur(int tohum)
        {
            var olaylar = new List<Olay>();
            var oturum = Kur(CizgiliIkiEkran, tohum, olaylar);
            olaylar.Clear();

            Assert.That(oturum.Dokun(H(1, 1), olaylar), Is.EqualTo(HamleSonucu.Gecerli));

            int gecis = olaylar.FindIndex(o => o is EkranDegisti);
            Assert.That(gecis, Is.GreaterThan(0), "Dikey çizgi 1. ekranın hedefini bitirir.");
            var vuruslar = olaylar.Take(gecis).OfType<HucreVuruldu>().Where(v => v.Tur == VurusTuru.Cizgi).ToList();
            Assert.That(vuruslar, Is.Not.Empty);
            Assert.That(vuruslar.Select(v => v.Konum.Satir), Is.All.LessThanOrEqualTo(3));

            var kutu = oturum.Baglam.Tahta[1, 1].Tas!;
            Assert.That(kutu.Tur, Is.EqualTo(TasTuru.Engel), "Alt ekranın aynı sütunundaki kutu vurulmadı.");
            Assert.That(kutu.Katman, Is.EqualTo(1));
            Assert.That(oturum.Hedefler.Kalan("kutu"), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "2.1.7, 7.1.3, 8.9.1")]
        public void Hedef_sayaclari_yalniz_gecerli_ekranda_duser()
        {
            var tahta = TahtaKurucu.Kur(NotrCizim(3, 3)).Tahta;
            var hedefler = new HedefTakibi(
                new[] { new Hedef("renk:0", 2, 1), new Hedef("renk:0", 1, 2) }, tahta);
            var olaylar = new List<Olay>();

            hedefler.Say("renk:0", olaylar);
            hedefler.Say("renk:0", olaylar);
            Assert.That(hedefler.EkranTamam, Is.True);
            Assert.That(hedefler.HepsiTamam, Is.False);

            hedefler.Say("renk:0", olaylar);
            Assert.That(olaylar.OfType<HedefIlerledi>().Count(), Is.EqualTo(2), "Tamamlanan ekranın fazlası sonraki ekrana sayılmaz.");

            hedefler.SonrakiEkranaGec(tahta);
            Assert.That(hedefler.GecerliEkran, Is.EqualTo(2));
            Assert.That(hedefler.Kalan("renk:0"), Is.EqualTo(1));

            hedefler.Say("renk:0", olaylar);
            Assert.That(hedefler.HepsiTamam, Is.True);
            Assert.That(olaylar.OfType<KazanmaBasladi>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "8.9.1, 8.9.2, 5.4.2, 5.4.4, 5.6.1")]
        public void Ornek_cok_ekranli_bolum_her_tohumda_gecerli_bir_ikinci_ekrana_gecer()
        {
            var veri = BolumYukleyici.JsondanOku(ProjeDosyalari.Oku("veri", "bolumler", "bolum_006.json"), P);
            int gecen = 0;
            foreach (int tohum in Tohumlar)
            {
                var olaylar = new List<Olay>();
                var oturum = BolumKurucu.Kur(veri, AkBayraklari.Varsayilan, P, (ulong)tohum, olaylar);
                for (int hamle = 0; hamle < veri.Hamle && oturum.Durum == BolumDurumu.Oynaniyor && oturum.GecerliEkran == 1; hamle++)
                {
                    var hamleler = OlasiHamleBulucu.Bul(oturum.Baglam.Tahta).Where(h => h.Cikilan.DikKomsuMu(h.Gidilen)).ToList();
                    Assert.That(hamleler, Is.Not.Empty, $"tohum {tohum}");
                    oturum.Kaydir(hamleler[0].Cikilan, hamleler[0].Gidilen, olaylar);
                }

                if (oturum.GecerliEkran != 2)
                {
                    continue;
                }

                gecen++;
                var tahta = oturum.Baglam.Tahta;
                Assert.That(tahta.SatirSayisi, Is.EqualTo(veri.Ekranlar[1].Yukseklik));
                Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Has.None.Null, $"tohum {tohum}");
                Assert.That(OlasiHamleBulucu.HazirEslesmeVar(tahta), Is.False, $"tohum {tohum}");
                Assert.That(OlasiHamleBulucu.Var(tahta), Is.True, $"tohum {tohum}");
                Assert.That(oturum.Hedefler.Kalan("katmanli_blok"), Is.EqualTo(11), "Adet 0: ikinci ekrandaki bütün bloklar.");

                var kimlikler = tahta.TumKonumlar().Select(k => tahta[k].Tas!.Kimlik).ToList();
                Assert.That(kimlikler.Distinct().Count(), Is.EqualTo(kimlikler.Count));

                // GDD 8.9.2: boş başlayan iki satır üst ekrandan kalan taşlarla dolar.
                var gecis = olaylar.OfType<EkranDegisti>().Single();
                Assert.That(gecis.AktarilanTas, Is.EqualTo(49));
            }

            Assert.That(gecen, Is.GreaterThan(0), "En az bir tohumda ikinci ekrana ulaşılmalı.");
        }

        private static string Bolum(string ekleme, int yukseklik = 6, string hedefler = @"[{ ""tur"": ""renk:0"", ""adet"": 3, ""ekran"": 1 }, { ""tur"": ""renk:1"", ""adet"": 3, ""ekran"": 2 }]")
        {
            string satirlar = string.Join(", ", Enumerable.Repeat(@"""? ? ? ?""", yukseklik));
            return $@"{{ ""kimlik"": 0, ""genislik"": 4, ""yukseklik"": {yukseklik}, ""hamle"": 10, ""renkler"": 4,
                {ekleme} ""hucreler"": [{satirlar}], ""hedefler"": {hedefler} }}";
        }

        [Test]
        [Property("GDD", "8.1.1, 8.1.3, 8.9.1, 8.4.2")]
        public void Cok_ekranli_bolum_verisi_dogrulanir()
        {
            const string bayrak = @"""bayraklar"": { ""cokEkranli"": true }, ";
            int standart = P.TamSayi(ParametreAdlari.TahtaYukseklikStandart);
            int azami = P.TamSayi(ParametreAdlari.KaydirmaliTahtaYukseklik);

            Assert.DoesNotThrow(() => BolumYukleyici.JsondanOku(Bolum(bayrak + @"""ekranlar"": [[1, 3], [4, 6]], "), P));
            Assert.DoesNotThrow(
                () => BolumYukleyici.JsondanOku(Bolum(bayrak + $@"""ekranlar"": [[1, {standart}], [{standart + 1}, {standart + 2}]], ", standart + 2), P),
                "Standart yüksekliği aşan tahta çok ekranlı bölümde kullanılabilir (8.1.3).");

            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(bayrak), P), "Ekran tanımı yok.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(bayrak + @"""ekranlar"": [[1, 6]], "), P), "Tek ekran.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(bayrak + @"""ekranlar"": [[1, 2], [4, 6]], "), P), "Boşluk.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(bayrak + @"""ekranlar"": [[1, 4], [4, 6]], "), P), "Çakışma.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(bayrak + @"""ekranlar"": [[1, 3], [4, 5]], "), P), "Kaplanmayan satır.");
            Assert.Throws<BolumVerisiHatasi>(
                () => BolumYukleyici.JsondanOku(Bolum(bayrak + $@"""ekranlar"": [[1, {standart + 1}], [{standart + 2}, {standart + 3}]], ", standart + 3), P),
                "Bir ekran standart yüksekliği aşamaz.");
            Assert.Throws<BolumVerisiHatasi>(
                () => BolumYukleyici.JsondanOku(Bolum(bayrak + $@"""ekranlar"": [[1, 3], [4, {azami + 1}]], ", azami + 1), P),
                "Toplam yükseklik sınırı.");
            Assert.Throws<BolumVerisiHatasi>(
                () => BolumYukleyici.JsondanOku(
                    Bolum(bayrak + @"""ekranlar"": [[1, 3], [4, 6]], ", hedefler: @"[{ ""tur"": ""renk:0"", ""adet"": 3, ""ekran"": 1 }]"), P),
                "Hedefsiz ekran.");
            Assert.Throws<BolumVerisiHatasi>(
                () => BolumYukleyici.JsondanOku(
                    Bolum(bayrak + @"""ekranlar"": [[1, 3], [4, 6]], ", hedefler: @"[{ ""tur"": ""renk:0"", ""adet"": 3, ""ekran"": 1 }, { ""tur"": ""renk:1"", ""adet"": 3, ""ekran"": 3 }]"), P),
                "Olmayan ekran.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(@"""ekranlar"": [[1, 3], [4, 6]], "), P), "Bayraksız ekran listesi.");
            Assert.Throws<BolumVerisiHatasi>(() => BolumYukleyici.JsondanOku(Bolum(string.Empty), P), "Tek ekranlı bölümde 2. ekran hedefi.");
        }

        // ------------------------------------------------------------------ Artan hamle gösterisi (GDD 7.6.2, 7.6.3)

        private const string KazanilanTahta = @"
                1 2 3 4 5
            1   K K · K ·
            2   · · · · ·
            3   · · · · ·
            4   · · · · ·
            5   · · · · ·";

        private static TestOturumu Kazan(int hamle, int tohum = 1, string cizim = KazanilanTahta)
        {
            var o = TestOturumu.Kur(cizim, hamle: hamle, hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 3) }, tohum: tohum);
            o.Oturum.DolumGostergesiAcik = true;
            Assert.That(o.Kaydir(H(1, 4), H(1, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            o.Olaylar.Clear();
            return o;
        }

        [TestCaseSource(nameof(Tohumlar))]
        [Property("GDD", "7.6.2, 7.6.3")]
        public void Kalan_her_hamle_icin_bir_normal_tas_cizgi_tasina_donusur_ve_sirayla_patlar(int tohum)
        {
            var o = Kazan(hamle: 4, tohum: tohum);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(3));

            Assert.That(o.Oturum.ArtanHamleGosterisi(o.Olaylar), Is.True);

            Assert.That(o.Olaylar.First(), Is.InstanceOf<ArtanHamleGosterisiBasladi>());
            Assert.That(((ArtanHamleGosterisiBasladi)o.Olaylar.First()).KalanHamle, Is.EqualTo(3));

            var donusumler = o.Olaylar.OfType<TasDonustu>().ToList();
            Assert.That(donusumler.Count, Is.EqualTo(3));
            Assert.That(donusumler.Select(d => d.EskiTas!.Tur), Is.All.EqualTo(TasTuru.Normal));
            Assert.That(donusumler.Select(d => d.YeniTas.Tur), Is.All.EqualTo(TasTuru.Cizgi));
            Assert.That(donusumler.Select(d => d.Konum).Distinct().Count(), Is.EqualTo(3));

            // Önce hepsi dönüşür, sonra dönüşüm sırasıyla patlar.
            int sonDonusum = o.Olaylar.FindLastIndex(x => x is TasDonustu);
            int ilkEtkinlesme = o.Olaylar.FindIndex(x => x is OzelTasEtkinlesti);
            Assert.That(ilkEtkinlesme, Is.GreaterThan(sonDonusum));
            var etkinlesenler = o.Olaylar.OfType<OzelTasEtkinlesti>().Select(e => e.Tas).ToList();
            Assert.That(etkinlesenler, Is.EquivalentTo(donusumler.Select(d => d.YeniTas)));
            var dogrudan = o.Olaylar.OfType<OzelTasEtkinlesti>().Where(e => e.Tetik != Tetik.Zincir).Select(e => e.Tas).ToList();
            Assert.That(dogrudan, Is.EqualTo(donusumler.Select(d => d.YeniTas).Where(dogrudan.Contains).ToList()));

            Assert.That(o.Olaylar.OfType<HedefIlerledi>(), Is.Empty, "Gösteri patlamaları hedef saymaz.");
            Assert.That(o.Olaylar.OfType<DolumGostergesiDegisti>(), Is.Empty, "Gösteri dolum göstergesini doldurmaz.");

            var bitis = o.Olaylar.Last() as ArtanHamlelerBitti;
            Assert.That(bitis, Is.Not.Null);
            Assert.That(bitis!.KalanHamle, Is.EqualTo(3));
            Assert.That(bitis.Atlandi, Is.False);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(3), "Para ödülü kalan hamleden hesaplanır (7.6.1).");

            var tahta = o.Oturum.Baglam.Tahta;
            Assert.That(tahta.TumKonumlar().Select(k => tahta[k].Tas), Has.None.Null, "Gösteri sonunda tahta durulur.");
            Assert.That(o.Oturum.ArtanHamleGosterisi(o.Olaylar), Is.False, "Gösteri bir kez yapılır.");
        }

        [Test]
        [Property("GDD", "7.6.2, 7.6.3")]
        public void Gosteri_atlanirsa_tahta_degismez_ve_bilgi_yine_gonderilir()
        {
            var o = Kazan(hamle: 4);

            Assert.That(o.Oturum.ArtanHamleGosterisi(o.Olaylar, atla: true), Is.True);

            Assert.That(o.Olaylar.Count, Is.EqualTo(1));
            var bitis = (ArtanHamlelerBitti)o.Olaylar.Single();
            Assert.That(bitis.KalanHamle, Is.EqualTo(3));
            Assert.That(bitis.Atlandi, Is.True);
        }

        [Test]
        [Property("GDD", "7.6.2")]
        public void Normal_tas_yetmezse_olan_kadari_donusur()
        {
            var o = Kazan(hamle: 10, cizim: @"
                    1 2 3 4
                1   K K · K");

            o.Oturum.ArtanHamleGosterisi(o.Olaylar);

            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9));
            Assert.That(o.Olaylar.OfType<TasDonustu>().Count(), Is.EqualTo(4));
        }

        [Test]
        [Property("GDD", "7.6.2, 7.6.3")]
        public void Kalan_hamle_yoksa_ya_da_bolum_kazanilmadiysa_gosteri_olmaz()
        {
            var o = Kazan(hamle: 1);
            Assert.That(o.Oturum.ArtanHamleGosterisi(o.Olaylar), Is.True);
            Assert.That(o.Olaylar.Single(), Is.InstanceOf<ArtanHamlelerBitti>());
            Assert.That(((ArtanHamlelerBitti)o.Olaylar.Single()).KalanHamle, Is.EqualTo(0));

            var suren = TestOturumu.Kur(KazanilanTahta, hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 9) });
            Assert.That(suren.Oturum.ArtanHamleGosterisi(suren.Olaylar), Is.False);
            Assert.That(suren.Olaylar, Is.Empty);
        }

        // ------------------------------------------------------------------ Düşme sürerken hamle (GDD 2.3.9, 11.3.1)

        private const string IkiEslesme = @"
                1 2 3 4 5
            1   · · · · ·
            2   · · · · ·
            3   K K · K ·
            4   · · · · ·
            5   M M · M ·";

        private static void Durult(TestOturumu o)
        {
            int adim = 0;
            while (o.Oturum.Ilerle(o.Olaylar))
            {
                Assert.That(++adim, Is.LessThan(1000), "Durulma sonlanmalı.");
            }
        }

        [Test]
        [Property("GDD", "2.3.9, 11.2.27, 11.3.1, 7.2.1")]
        public void Adimli_durulmada_dusme_surerken_ikinci_hamle_kabul_edilir()
        {
            var o = TestOturumu.Kur(IkiEslesme, hamle: 10);
            o.Oturum.AdimliDurulma = true;

            Assert.That(o.Kaydir(H(3, 4), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Oturum.DurulmaSuruyor, Is.True);
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9), "Sayaç hamle anında düşer; düşmeler beklenmez.");
            Assert.That(o.Olaylar.OfType<TasDustu>(), Is.Empty, "Hamle yalnız kendi dalgasını işletir.");
            Assert.That(o.Tahta[3, 1].Tas, Is.Null);

            // GDD 11.3.1: oyuncu hamlesi önce işlenir; düşme ve kaskad sonraki adımlara kalır.
            Assert.That(o.Kaydir(H(5, 4), H(5, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(8));
            Assert.That(o.Olaylar.OfType<GrupPatladi>().Count(), Is.EqualTo(2));
            Assert.That(o.Olaylar.OfType<TasDustu>(), Is.Empty);

            Durult(o);

            Assert.That(o.Oturum.DurulmaSuruyor, Is.False);
            Assert.That(o.Olaylar.OfType<TasDustu>(), Is.Not.Empty);
            Assert.That(o.Tahta.TumKonumlar().Select(k => o.Tahta[k].Tas), Has.None.Null);
            Assert.That(OlasiHamleBulucu.HazirEslesmeVar(o.Tahta), Is.False);
            Assert.That(o.Oturum.Ilerle(o.Olaylar), Is.False, "Durulmuş tahtada ilerleyecek adım yoktur.");
        }

        [Test]
        [Property("GDD", "2.3.7, 2.3.9, 11.2.27")]
        public void Bayrak_kapaliysa_dusme_surerken_hamle_kabul_edilmez()
        {
            var o = TestOturumu.Kur(IkiEslesme, hamle: 10, bayraklar: AkBayraklari.Varsayilan.DusmeSirasindaHamleIle(false));
            o.Oturum.AdimliDurulma = true;

            Assert.That(o.Kaydir(H(3, 4), H(3, 3)), Is.EqualTo(HamleSonucu.Gecerli));
            Assert.That(o.Kaydir(H(5, 4), H(5, 3)), Is.EqualTo(HamleSonucu.Reddedildi));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9));

            Durult(o);
            Assert.That(o.Kaydir(H(5, 4), H(5, 3)), Is.EqualTo(HamleSonucu.Gecerli), "Tahta durulunca hamle yine kabul edilir.");
        }

        [Test]
        [Property("GDD", "2.3.9, 7.3.1, 7.3.2")]
        public void Adimli_durulmada_kazanma_baslayinca_hamle_kabul_edilmez()
        {
            var o = TestOturumu.Kur(IkiEslesme, hamle: 10, hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 3) });
            o.Oturum.AdimliDurulma = true;

            o.Kaydir(H(3, 4), H(3, 3));
            Assert.That(o.OlaylarTurunde<KazanmaBasladi>().Count(), Is.EqualTo(1));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor), "Sonuç, süren düşmeler tamamlanınca verilir.");

            Assert.That(o.Kaydir(H(5, 4), H(5, 3)), Is.EqualTo(HamleSonucu.Reddedildi));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(9));

            Durult(o);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Kazanildi));
            Assert.That(o.OlaylarTurunde<BolumKazanildi>().Count(), Is.EqualTo(1));
        }

        [Test]
        [Property("GDD", "2.3.9, 7.4.1, 3.9.4")]
        public void Adimli_durulmada_hamle_bitti_akisi_tahta_durulunca_baslar()
        {
            var o = TestOturumu.Kur(IkiEslesme, hamle: 1, hedefler: new[] { new Hedef(Hedef.RenkTuru(Kirmizi), 9) });
            o.Oturum.AdimliDurulma = true;

            o.Kaydir(H(3, 4), H(3, 3));
            Assert.That(o.Oturum.KalanHamle, Is.EqualTo(0));
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.Oynaniyor));
            Assert.That(o.Kaydir(H(5, 4), H(5, 3)), Is.EqualTo(HamleSonucu.Reddedildi), "Hamle kalmadı.");
            Assert.That(o.Arac(AracTuru.TekHucre, H(1, 1)), Is.False, "Araç, tahta durulmadan kullanılmaz.");

            Durult(o);
            Assert.That(o.Oturum.Durum, Is.EqualTo(BolumDurumu.EkHamleTeklifi));
        }

        private static IEnumerable<string> OrnekBolumler() =>
            Directory.GetFiles(Path.Combine(ProjeDosyalari.Kok, "veri", "bolumler"), "bolum_*.json")
                .Select(Path.GetFileName)
                .OrderBy(ad => ad)!;

        private static string Dokum(Tahta tahta) =>
            string.Join(" ", tahta.TumKonumlar().Select(k =>
            {
                var h = tahta[k];
                return h.Tas == null ? "_" : $"{h.Tas.Tur}:{h.Tas.Renk}:{h.Tas.Katman}";
            }));

        [TestCaseSource(nameof(OrnekBolumler))]
        [Property("GDD", "0.4, 2.3.9")]
        public void Adimli_durulma_araya_hamle_girmedikce_tek_cagrili_akisla_ayni_sonucu_verir(string dosya)
        {
            var veri = BolumYukleyici.JsondanOku(ProjeDosyalari.Oku("veri", "bolumler", dosya), P);
            foreach (int tohum in Tohumlar)
            {
                var tam = BolumKurucu.Kur(veri, AkBayraklari.Varsayilan, P, (ulong)tohum, new List<Olay>());
                var adimli = BolumKurucu.Kur(veri, AkBayraklari.Varsayilan, P, (ulong)tohum, new List<Olay>());
                adimli.AdimliDurulma = true;

                for (int hamle = 0; hamle < 6 && tam.Durum == BolumDurumu.Oynaniyor; hamle++)
                {
                    var hamleler = OlasiHamleBulucu.Bul(tam.Baglam.Tahta).Where(h => h.Cikilan.DikKomsuMu(h.Gidilen)).ToList();
                    if (hamleler.Count == 0)
                    {
                        break;
                    }

                    var secilen = hamleler[hamle % hamleler.Count];
                    var olaylar = new List<Olay>();
                    Assert.That(tam.Kaydir(secilen.Cikilan, secilen.Gidilen, olaylar), Is.EqualTo(HamleSonucu.Gecerli));
                    Assert.That(adimli.Kaydir(secilen.Cikilan, secilen.Gidilen, olaylar), Is.EqualTo(HamleSonucu.Gecerli));
                    int adim = 0;
                    while (adimli.Ilerle(olaylar))
                    {
                        Assert.That(++adim, Is.LessThan(5000));
                    }

                    Assert.That(Dokum(adimli.Baglam.Tahta), Is.EqualTo(Dokum(tam.Baglam.Tahta)), $"{dosya} tohum {tohum} hamle {hamle + 1}");
                    Assert.That(adimli.Durum, Is.EqualTo(tam.Durum));
                    Assert.That(adimli.KalanHamle, Is.EqualTo(tam.KalanHamle));
                }
            }
        }
    }
}
