using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Testler.Ortak
{
    /// <summary>
    /// Bir hamlenin ya da kaskad adımının sonucu ve ürettiği olaylar üzerinde sorgular.
    /// <see cref="Kaydir(string, Konum, Konum, AkBayraklari, int, bool)"/> ve Dokun yalnızca hamlenin patlama adımını
    /// çalıştırır (düşme yok); tam akış için TamKaydir kullanılır.
    /// </summary>
    public sealed class HamleDenemesi
    {
        private HamleDenemesi(TestTahtasi test, OyunBaglami baglam, HamleSonucu? sonuc, List<Olay> olaylar)
        {
            Test = test;
            Baglam = baglam;
            Sonuc = sonuc;
            Olaylar = olaylar;
        }

        public TestTahtasi Test { get; }
        public OyunBaglami Baglam { get; }
        public Tahta Tahta => Test.Tahta;

        /// <summary>Kaskad adımında null.</summary>
        public HamleSonucu? Sonuc { get; }

        public IReadOnlyList<Olay> Olaylar { get; }

        public IReadOnlyList<EslesmeGrubu> Gruplar =>
            Olaylar.OfType<GrupPatladi>().Select(o => o.Grup).ToList();

        /// <summary>Gruplarla patlayan bütün hücreler (uzatma hücreleri dahil).</summary>
        public ISet<Konum> PatlayanHucreler =>
            new HashSet<Konum>(Gruplar.SelectMany(g => g.TumHucreler));

        public IReadOnlyList<OzelTasDogdu> DoganOzelTaslar => Olaylar.OfType<OzelTasDogdu>().ToList();

        public IReadOnlyList<HucreVuruldu> Vuruslar => Olaylar.OfType<HucreVuruldu>().ToList();

        /// <summary>Özel taş etkileriyle en az bir vuruş alan hücreler.</summary>
        public ISet<Konum> VurulanHucreler => new HashSet<Konum>(Vuruslar.Select(v => v.Konum));

        public IReadOnlyList<TasPatladi> PatlayanTaslar => Olaylar.OfType<TasPatladi>().ToList();

        public IReadOnlyList<OzelTasEtkinlesti> Etkinlesenler => Olaylar.OfType<OzelTasEtkinlesti>().ToList();

        public IReadOnlyList<GudumluKalkti> Kalkislar => Olaylar.OfType<GudumluKalkti>().ToList();

        public IReadOnlyList<GudumluIndi> Inisler => Olaylar.OfType<GudumluIndi>().ToList();

        public IReadOnlyList<TasDonustu> Donusumler => Olaylar.OfType<TasDonustu>().ToList();

        public IReadOnlyList<RenkSecildi> SecilenRenkler => Olaylar.OfType<RenkSecildi>().ToList();

        /// <summary>Bir hücrenin ilk vurulduğu mantıksal adım.</summary>
        public long IlkVurusAdimi(Konum konum) => Vuruslar.First(v => v.Konum == konum).Adim;

        public static OyunBaglami BaglamKur(
            Tahta tahta,
            AkBayraklari? bayraklar = null,
            int tohum = 1,
            bool ciftRenk = false,
            ParametreTablosu? parametreler = null) =>
            new OyunBaglami(
                tahta,
                bayraklar ?? AkBayraklari.Varsayilan,
                parametreler ?? ProjeDosyalari.Parametreler,
                new TohumluRastgele(tohum))
            {
                CiftRenkModu = ciftRenk,
            };

        public static HamleDenemesi Kaydir(
            string cizim, Konum cikilan, Konum gidilen, AkBayraklari? bayraklar = null, int tohum = 1, bool ciftRenk = false) =>
            Kaydir(TahtaKurucu.Kur(cizim), cikilan, gidilen, bayraklar, tohum, ciftRenk);

        public static HamleDenemesi Kaydir(
            TestTahtasi test, Konum cikilan, Konum gidilen, AkBayraklari? bayraklar = null, int tohum = 1, bool ciftRenk = false)
        {
            var baglam = BaglamKur(test.Tahta, bayraklar, tohum, ciftRenk);
            var olaylar = new List<Olay>();
            var sonuc = HamleIsleyici.Kaydir(baglam, cikilan, gidilen, olaylar, HamleKapsami.YalnizPatlama);
            return new HamleDenemesi(test, baglam, sonuc, olaylar);
        }

        public static HamleDenemesi Dokun(
            string cizim, Konum konum, AkBayraklari? bayraklar = null, int tohum = 1, bool ciftRenk = false) =>
            Dokun(TahtaKurucu.Kur(cizim), konum, bayraklar, tohum, ciftRenk);

        public static HamleDenemesi Dokun(
            TestTahtasi test, Konum konum, AkBayraklari? bayraklar = null, int tohum = 1, bool ciftRenk = false)
        {
            var baglam = BaglamKur(test.Tahta, bayraklar, tohum, ciftRenk);
            var olaylar = new List<Olay>();
            var sonuc = HamleIsleyici.Dokun(baglam, konum, olaylar, HamleKapsami.YalnizPatlama);
            return new HamleDenemesi(test, baglam, sonuc, olaylar);
        }

        /// <summary>
        /// test_vakalari.md "Varsayılan kurulum": giriş hücreleri her sütunun üstünde, renkler K, M, Y, S.
        /// Çapraz kayma, vaka ayrıca yazmadıkça kapalıdır (1. aşama, karar 9).
        /// </summary>
        public static OyunBaglami TamBaglamKur(
            Tahta tahta,
            AkBayraklari? bayraklar = null,
            int tohum = 1,
            bool capraz = false,
            bool notrDolgu = true,
            bool ciftRenk = false)
        {
            var baglam = BaglamKur(tahta, bayraklar, tohum, ciftRenk);
            for (int t = 1; t <= tahta.SutunSayisi; t++)
            {
                if (tahta[1, t].Oynanabilir)
                {
                    tahta[1, t].Giris = true;
                }
            }

            baglam.Renkler = new[] { TahtaKurucu.Kirmizi, TahtaKurucu.Mavi, TahtaKurucu.Yesil, TahtaKurucu.Sari };
            baglam.CaprazKaymaIzinli = capraz;
            if (notrDolgu)
            {
                baglam.Dolgu = new NotrDolgu();
            }

            return baglam;
        }

        /// <summary>Kaydırma hamlesinin tam akışı: patlama, düşme, doldurma, kaskad, karıştırma (GDD 5.7).</summary>
        public static HamleDenemesi TamKaydir(
            TestTahtasi test,
            Konum cikilan,
            Konum gidilen,
            AkBayraklari? bayraklar = null,
            int tohum = 1,
            bool capraz = false,
            bool notrDolgu = true,
            bool ciftRenk = false)
        {
            var baglam = TamBaglamKur(test.Tahta, bayraklar, tohum, capraz, notrDolgu, ciftRenk);
            var olaylar = new List<Olay>();
            // Vaka tahtalarındaki nötr taşlar hiçbir zaman olası hamle kurmaz; karıştırma vakayı bozmasın diye
            // akış durulmada biter. Karıştırma kendi testlerinde (D10) çalıştırılır.
            var sonuc = HamleIsleyici.Kaydir(baglam, cikilan, gidilen, olaylar, HamleKapsami.Durulma);
            return new HamleDenemesi(test, baglam, sonuc, olaylar);
        }

        public static HamleDenemesi TamKaydir(
            string cizim,
            Konum cikilan,
            Konum gidilen,
            AkBayraklari? bayraklar = null,
            int tohum = 1,
            bool capraz = false,
            bool notrDolgu = true,
            bool ciftRenk = false) =>
            TamKaydir(TahtaKurucu.Kur(cizim), cikilan, gidilen, bayraklar, tohum, capraz, notrDolgu, ciftRenk);

        /// <summary>Durum testi: tahta durulana kadar düşme adımlarını çalıştır (GDD 5.1–5.3).</summary>
        public static HamleDenemesi Dusur(
            string cizim, AkBayraklari? bayraklar = null, bool capraz = false, bool notrDolgu = true)
        {
            var test = TahtaKurucu.Kur(cizim);
            var baglam = TamBaglamKur(test.Tahta, bayraklar, capraz: capraz, notrDolgu: notrDolgu);
            var olaylar = new List<Olay>();
            Dinamik.DusmeIsleyici.Durult(baglam, olaylar);
            return new HamleDenemesi(test, baglam, null, olaylar);
        }

        public IReadOnlyList<TasDustu> Dusmeler => Olaylar.OfType<TasDustu>().ToList();

        public IReadOnlyList<TasGirdi> Girisler => Olaylar.OfType<TasGirdi>().ToList();

        public IReadOnlyList<TasCaprazKaydi> CaprazKaymalar => Olaylar.OfType<TasCaprazKaydi>().ToList();

        /// <summary>Durum testi: tahtadaki bütün eşleşmeleri bul ve patlat (kaskad adımı).</summary>
        public static HamleDenemesi KaskadAdimi(string cizim, AkBayraklari? bayraklar = null) =>
            KaskadAdimi(TahtaKurucu.Kur(cizim), bayraklar);

        public static HamleDenemesi KaskadAdimi(TestTahtasi test, AkBayraklari? bayraklar = null)
        {
            var baglam = BaglamKur(test.Tahta, bayraklar);
            var olaylar = new List<Olay>();
            HamleIsleyici.KaskadEslesmeleriniPatlat(baglam, olaylar);
            return new HamleDenemesi(test, baglam, null, olaylar);
        }
    }

    /// <summary>
    /// Test dolgusu: girişten çıkan her taş ayrı bir renktedir; böylece doldurma ek kaskad üretmez ve
    /// vaka yalnızca izlediği taşları görür.
    /// </summary>
    public sealed class NotrDolgu : Dinamik.IDolguKaynagi
    {
        private int _renk = 5000;

        public Tas Uret(OyunBaglami baglam, Konum giris, Konum hedef) => baglam.Tahta.NormalTasUret(_renk++);
    }
}
