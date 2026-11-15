using System;
using System.Collections.Generic;
using System.Linq;

namespace CatRoyal.Cekirdek.Yapilandirma
{
    /// <summary>Ayarlanabilir kuralın varyantı (GDD 0.3).</summary>
    public enum Varyant
    {
        A,
        B,
    }

    /// <summary>Ayarlanabilir kural kimlikleri (GDD 11.1).</summary>
    public enum Ak
    {
        /// <summary>AK-01 — Kaskadda doğum hücresi ve satır ekseninin yönü (GDD 2.7.4).</summary>
        KaskadDogumHucresi = 1,

        /// <summary>AK-02 — Hamle bitince akışın sırası (GDD 7.4.1).</summary>
        HamleBittiAkisi = 2,

        /// <summary>AK-03 — Çapraz kaymanın koşulları (GDD 5.2.6).</summary>
        CaprazKaymaKosulu = 3,

        /// <summary>AK-04 — Girişi olmayan sütunun dolması (GDD 5.3.1).</summary>
        GirissizSutunDolumu = 4,

        /// <summary>AK-05 — Güdümlü taşın hedef önceliği (GDD 3.4.5).</summary>
        GudumluHedefSecimi = 5,

        /// <summary>AK-06 — Eşit öncelikli adayların çakışması (GDD 2.6.5).</summary>
        EsitOncelikSecimi = 6,

        /// <summary>AK-07 — Renk küresi + güdümlü taşta kürenin hücresi (GDD 4.3.9).</summary>
        RenkGudumluEkTas = 7,

        /// <summary>AK-08 — En yaygın renkte eşitlik (GDD 3.5.2).</summary>
        RenkEsitlikSecimi = 8,

        /// <summary>AK-09 — Zincirle vurulan renk küresinde renk sayımının anı (GDD 3.5.3).</summary>
        RenkKuresiZincirSecimi = 9,

        /// <summary>AK-10 — Öncelikte artan hücreler, uzatma ve 6'lı sıra (GDD 2.6.6).</summary>
        UzatmaAdimi = 10,

        /// <summary>AK-11 — Aynı eşleşmenin birden fazla hücresine değen engel (GDD 6.1.3).</summary>
        YanHasarVurusSayisi = 11,

        /// <summary>AK-12 — Bomba + bomba patlamasının merkezi (GDD 4.3.3).</summary>
        BombaBombaMerkezi = 12,

        /// <summary>AK-13 — Karıştırma başarısızlığı ve "olası hamle yok" akışı (GDD 5.6.5).</summary>
        KaristirmaBasarisizligi = 13,

        /// <summary>AK-14 — Çift renk küresinin ikinci renge geçişi (GDD 3.6.4).</summary>
        CiftRenkGecisi = 14,

        /// <summary>AK-15 — Çift renk küresi kombinasyonlarında renk sayısı (GDD 4.4.1).</summary>
        CiftRenkKombinasyon = 15,
    }

    /// <summary>Bayrak yapılandırması hatası.</summary>
    public sealed class BayrakHatasi : Exception
    {
        public BayrakHatasi(string mesaj) : base(mesaj) { }
    }

    /// <summary>
    /// AK-01…AK-15 bayrakları ve GDD'deki diğer açılıp kapanabilen kurallar. Değişmezdir.
    /// Yeniden derlemeden değiştirilir: düz bir JSON nesnesinden okunur (GDD 0.3).
    /// Varsayılan: bütün AK'ler A.
    /// </summary>
    public sealed class AkBayraklari
    {
        /// <summary>GDD'deki bayrak adları, AK numarasına göre.</summary>
        private static readonly IReadOnlyDictionary<Ak, string> Adlar = new Dictionary<Ak, string>
        {
            { Ak.KaskadDogumHucresi, "kaskad_dogum_hucresi" },
            { Ak.HamleBittiAkisi, "hamle_bitti_akisi" },
            { Ak.CaprazKaymaKosulu, "capraz_kayma_kosulu" },
            { Ak.GirissizSutunDolumu, "girissiz_sutun_dolumu" },
            { Ak.GudumluHedefSecimi, "gudumlu_hedef_secimi" },
            { Ak.EsitOncelikSecimi, "esit_oncelik_secimi" },
            { Ak.RenkGudumluEkTas, "renk_gudumlu_ek_tas" },
            { Ak.RenkEsitlikSecimi, "renk_esitlik_secimi" },
            { Ak.RenkKuresiZincirSecimi, "renk_kuresi_zincir_secimi" },
            { Ak.UzatmaAdimi, "uzatma_adimi" },
            { Ak.YanHasarVurusSayisi, "yan_hasar_vurus_sayisi" },
            { Ak.BombaBombaMerkezi, "bomba_bomba_merkezi" },
            { Ak.KaristirmaBasarisizligi, "karistirma_basarisizligi" },
            { Ak.CiftRenkGecisi, "cift_renk_gecisi" },
            { Ak.CiftRenkKombinasyon, "cift_renk_kombinasyon" },
        };

        private const string AdKaristirmaRenkDegisimi = "karistirma_renk_degisimi";
        private const string AdCizgiOynanmayanHucredeDurur = "cizgi_oynanmayan_hucrede_durur";
        private const string AdDusmeSirasindaHamle = "dusme_sirasinda_hamle";

        private readonly Dictionary<Ak, Varyant> _varyantlar;

        private AkBayraklari(
            Dictionary<Ak, Varyant> varyantlar,
            bool karistirmaRenkDegisimi,
            bool cizgiOynanmayanHucredeDurur,
            bool dusmeSirasindaHamle)
        {
            _varyantlar = varyantlar;
            KaristirmaRenkDegisimi = karistirmaRenkDegisimi;
            CizgiOynanmayanHucredeDurur = cizgiOynanmayanHucredeDurur;
            DusmeSirasindaHamle = dusmeSirasindaHamle;
        }

        /// <summary>Bütün AK'ler A; ek bayraklar GDD varsayılanında.</summary>
        public static AkBayraklari Varsayilan { get; } = new AkBayraklari(
            Adlar.Keys.ToDictionary(ak => ak, _ => Varyant.A),
            karistirmaRenkDegisimi: true,
            cizgiOynanmayanHucredeDurur: false,
            dusmeSirasindaHamle: true);

        /// <summary>Bütün AK kimlikleri, numara sırasıyla.</summary>
        public static IReadOnlyList<Ak> Kimlikler { get; } = Adlar.Keys.OrderBy(ak => (int)ak).ToList();

        /// <summary>Bir AK'nin seçili varyantı.</summary>
        public Varyant this[Ak ak] => _varyantlar[ak];

        /// <summary>
        /// GDD 5.6.6 — karıştırmada yer değiştirme başarısız olunca renk değiştirme adımı. Varsayılan açık.
        /// </summary>
        public bool KaristirmaRenkDegisimi { get; }

        /// <summary>
        /// GDD 3.2.4, 11.2.1 — çizgi kanadı oynanmayan hücrede durur mu? Varsayılan hayır (atlayıp devam eder).
        /// </summary>
        public bool CizgiOynanmayanHucredeDurur { get; }

        /// <summary>
        /// GDD 2.3.9, 11.2.27 — düşme ve kaskad sürerken hamle kabul edilir mi? Varsayılan evet.
        /// </summary>
        public bool DusmeSirasindaHamle { get; }

        /// <summary>GDD'deki bayrak adı, ör. "kaskad_dogum_hucresi".</summary>
        public static string BayrakAdi(Ak ak) => Adlar[ak];

        /// <summary>"AK-01" biçiminde kimlik.</summary>
        public static string KimlikMetni(Ak ak) => "AK-" + ((int)ak).ToString("00");

        /// <summary>Tek bir AK'si değiştirilmiş kopya (B varyantı test kümeleri için).</summary>
        public AkBayraklari Ile(Ak ak, Varyant varyant)
        {
            var kopya = new Dictionary<Ak, Varyant>(_varyantlar) { [ak] = varyant };
            return new AkBayraklari(kopya, KaristirmaRenkDegisimi, CizgiOynanmayanHucredeDurur, DusmeSirasindaHamle);
        }

        public AkBayraklari KaristirmaRenkDegisimiIle(bool acik) =>
            new AkBayraklari(_varyantlar, acik, CizgiOynanmayanHucredeDurur, DusmeSirasindaHamle);

        public AkBayraklari CizgiOynanmayanHucredeDururIle(bool durur) =>
            new AkBayraklari(_varyantlar, KaristirmaRenkDegisimi, durur, DusmeSirasindaHamle);

        public AkBayraklari DusmeSirasindaHamleIle(bool kabul) =>
            new AkBayraklari(_varyantlar, KaristirmaRenkDegisimi, CizgiOynanmayanHucredeDurur, kabul);

        /// <summary>
        /// Düz JSON nesnesinden okur: { "kaskad_dogum_hucresi": "A", "karistirma_renk_degisimi": true }.
        /// Dosyada olmayan bayraklar varsayılanda kalır. Tanınmayan ad ya da geçersiz değer hatadır.
        /// </summary>
        public static AkBayraklari JsondanOku(string json)
        {
            var degerler = DuzJson.Ayristir(json);
            var varyantlar = new Dictionary<Ak, Varyant>(Varsayilan._varyantlar);
            bool renkDegisimi = Varsayilan.KaristirmaRenkDegisimi;
            bool cizgiDurur = Varsayilan.CizgiOynanmayanHucredeDurur;
            bool dusmedeHamle = Varsayilan.DusmeSirasindaHamle;

            foreach (var cift in degerler)
            {
                switch (cift.Key)
                {
                    case AdKaristirmaRenkDegisimi:
                        renkDegisimi = MantiksalCoz(cift.Key, cift.Value);
                        continue;
                    case AdCizgiOynanmayanHucredeDurur:
                        cizgiDurur = MantiksalCoz(cift.Key, cift.Value);
                        continue;
                    case AdDusmeSirasindaHamle:
                        dusmedeHamle = MantiksalCoz(cift.Key, cift.Value);
                        continue;
                }

                var bulunan = Adlar.Where(a => a.Value == cift.Key).ToList();
                if (bulunan.Count == 0)
                {
                    throw new BayrakHatasi($"Tanınmayan bayrak: '{cift.Key}'.");
                }

                varyantlar[bulunan[0].Key] = VaryantCoz(cift.Key, cift.Value);
            }

            return new AkBayraklari(varyantlar, renkDegisimi, cizgiDurur, dusmedeHamle);
        }

        private static Varyant VaryantCoz(string ad, object deger)
        {
            if (deger is string metin)
            {
                if (metin == "A") return Varyant.A;
                if (metin == "B") return Varyant.B;
            }

            throw new BayrakHatasi($"'{ad}' bayrağının değeri \"A\" ya da \"B\" olmalı.");
        }

        private static bool MantiksalCoz(string ad, object deger)
        {
            if (deger is bool mantiksal)
            {
                return mantiksal;
            }

            throw new BayrakHatasi($"'{ad}' bayrağının değeri true ya da false olmalı.");
        }
    }

    /// <summary>
    /// Yalnızca düz bir nesneyi (metin anahtar → metin ya da true/false) okuyan küçük JSON ayrıştırıcı.
    /// Çekirdeğin dış bağımlılığı olmasın diye elle yazıldı.
    /// </summary>
    internal static class DuzJson
    {
        public static Dictionary<string, object> Ayristir(string json)
        {
            var sonuc = new Dictionary<string, object>(StringComparer.Ordinal);
            int i = 0;
            BosluklariGec(json, ref i);
            Bekle(json, ref i, '{');
            BosluklariGec(json, ref i);
            if (i < json.Length && json[i] == '}')
            {
                i++;
            }
            else
            {
                while (true)
                {
                    BosluklariGec(json, ref i);
                    string anahtar = MetinOku(json, ref i);
                    BosluklariGec(json, ref i);
                    Bekle(json, ref i, ':');
                    BosluklariGec(json, ref i);
                    object deger = DegerOku(json, ref i);
                    if (sonuc.ContainsKey(anahtar))
                    {
                        throw new BayrakHatasi($"Bayrak dosyasında '{anahtar}' birden fazla kez geçiyor.");
                    }

                    sonuc.Add(anahtar, deger);
                    BosluklariGec(json, ref i);
                    if (i < json.Length && json[i] == ',')
                    {
                        i++;
                        continue;
                    }

                    Bekle(json, ref i, '}');
                    break;
                }
            }

            BosluklariGec(json, ref i);
            if (i != json.Length)
            {
                throw new BayrakHatasi("Bayrak dosyası: nesneden sonra fazladan içerik var.");
            }

            return sonuc;
        }

        private static object DegerOku(string json, ref int i)
        {
            if (i < json.Length && json[i] == '"')
            {
                return MetinOku(json, ref i);
            }

            if (string.CompareOrdinal(json, i, "true", 0, 4) == 0)
            {
                i += 4;
                return true;
            }

            if (string.CompareOrdinal(json, i, "false", 0, 5) == 0)
            {
                i += 5;
                return false;
            }

            throw new BayrakHatasi($"Bayrak dosyası: {i}. karakterde metin ya da true/false bekleniyordu.");
        }

        private static string MetinOku(string json, ref int i)
        {
            Bekle(json, ref i, '"');
            int baslangic = i;
            while (i < json.Length && json[i] != '"')
            {
                if (json[i] == '\\')
                {
                    throw new BayrakHatasi("Bayrak dosyası: kaçış dizileri desteklenmiyor.");
                }

                i++;
            }

            if (i >= json.Length)
            {
                throw new BayrakHatasi("Bayrak dosyası: kapanmamış tırnak.");
            }

            string metin = json.Substring(baslangic, i - baslangic);
            i++;
            return metin;
        }

        private static void Bekle(string json, ref int i, char beklenen)
        {
            if (i >= json.Length || json[i] != beklenen)
            {
                throw new BayrakHatasi($"Bayrak dosyası: {i}. karakterde '{beklenen}' bekleniyordu.");
            }

            i++;
        }

        private static void BosluklariGec(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
            {
                i++;
            }
        }
    }
}
