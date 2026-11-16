using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Tasarim
{
    /// <summary>
    /// Bölüm paketlerinin numaralandırması (GDD 8.11.2): bölümler sabit uzunlukta paketler halinde gruplanır
    /// (parametre: bolum_paketi_uzunlugu). Her paketin kendi harita görseli vardır.
    /// </summary>
    public sealed class PaketDuzeni
    {
        public PaketDuzeni(ParametreTablosu p)
        {
            Uzunluk = Math.Max(1, p.TamSayi(TasarimParametreAdlari.BolumPaketiUzunlugu));
        }

        public int Uzunluk { get; }

        /// <summary>Bölümün 1 tabanlı paket numarası.</summary>
        public int Paket(int bolum) => ((Math.Max(1, bolum) - 1) / Uzunluk) + 1;

        public int IlkBolum(int paket) => ((paket - 1) * Uzunluk) + 1;

        public int SonBolum(int paket) => paket * Uzunluk;

        /// <summary>Paketin harita görselinin kimliği; görselin kendisi sunumdadır.</summary>
        public static string HaritaGorseli(int paket) => "harita_" + paket.ToString("000", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Bir bölüm paketinin içeriği (GDD 8.11): bölüm dosyaları, pakete ait kurtarma bölümleri (10.2.1) ve
    /// varsayılan zorluk etiketleri (8.8.1a). Uygulamayla gelen ve sunucudan indirilen paketler aynı yapıdadır.
    /// </summary>
    public sealed class BolumPaketiVerisi
    {
        public BolumPaketiVerisi(int paket, int surum)
        {
            if (paket < 1 || surum < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(paket), "Paket numarası ve sürümü en az 1 olmalı.");
            }

            Paket = paket;
            Surum = surum;
        }

        public int Paket { get; }

        /// <summary>Paketin düzenleme sürümü; sunucu var olan bölümleri güncelleyince artar (GDD 8.11.1).</summary>
        public int Surum { get; }

        /// <summary>Bölüm numarası → bölüm dosyasının metni.</summary>
        public Dictionary<int, string> Bolumler { get; } = new Dictionary<int, string>();

        /// <summary>Kurtarma bölümü numarası → bölüm dosyasının metni.</summary>
        public Dictionary<int, string> KurtarmaBolumleri { get; } = new Dictionary<int, string>();

        public Dictionary<int, ZorlukEtiketi> Etiketler { get; } = new Dictionary<int, ZorlukEtiketi>();

        public string HaritaGorseli => PaketDuzeni.HaritaGorseli(Paket);

        /// <summary>Paketi tek bir metne çevirir; indirilen paket diske böyle yazılır.</summary>
        public string Yaz()
        {
            Dictionary<string, object?> Sozluk<T>(Dictionary<int, T> kaynak, Func<T, object?> deger)
            {
                var d = new Dictionary<string, object?>();
                foreach (var cift in kaynak.OrderBy(c => c.Key))
                {
                    d[cift.Key.ToString(CultureInfo.InvariantCulture)] = deger(cift.Value);
                }

                return d;
            }

            return Json.Yaz(new Dictionary<string, object?>
            {
                ["paket"] = Paket,
                ["surum"] = Surum,
                ["bolumler"] = Sozluk(Bolumler, metin => metin),
                ["kurtarmaBolumleri"] = Sozluk(KurtarmaBolumleri, metin => metin),
                ["etiketler"] = Sozluk(Etiketler, etiket => etiket.ToString()),
            });
        }

        /// <exception cref="BolumVerisiHatasi">Metin bir paket değilse.</exception>
        public static BolumPaketiVerisi Oku(string json)
        {
            Dictionary<string, object?> kok;
            try
            {
                kok = Json.Ayristir(json) as Dictionary<string, object?> ?? throw new BolumVerisiHatasi("Paket: kök bir nesne olmalı.");
            }
            catch (JsonHatasi hata)
            {
                throw new BolumVerisiHatasi("Paket okunamadı: " + hata.Message);
            }

            int Tam(string ad) =>
                kok.TryGetValue(ad, out object? deger) && deger is double sayi && sayi >= 1 && Math.Floor(sayi) == sayi
                    ? (int)sayi
                    : throw new BolumVerisiHatasi($"Paket: '{ad}' 1 ya da daha büyük bir tam sayı olmalı.");

            IEnumerable<KeyValuePair<int, string>> Metinler(string ad)
            {
                if (!kok.TryGetValue(ad, out object? deger) || deger == null)
                {
                    yield break;
                }

                var sozluk = deger as Dictionary<string, object?> ?? throw new BolumVerisiHatasi($"Paket: '{ad}' bir nesne olmalı.");
                foreach (var cift in sozluk)
                {
                    if (!int.TryParse(cift.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int no) || !(cift.Value is string metin))
                    {
                        throw new BolumVerisiHatasi($"Paket: '{ad}.{cift.Key}' okunamadı.");
                    }

                    yield return new KeyValuePair<int, string>(no, metin);
                }
            }

            var paket = new BolumPaketiVerisi(Tam("paket"), Tam("surum"));
            foreach (var cift in Metinler("bolumler")) paket.Bolumler[cift.Key] = cift.Value;
            foreach (var cift in Metinler("kurtarmaBolumleri")) paket.KurtarmaBolumleri[cift.Key] = cift.Value;
            foreach (var cift in Metinler("etiketler"))
            {
                if (!Enum.TryParse(cift.Value, out ZorlukEtiketi etiket) || !Enum.IsDefined(typeof(ZorlukEtiketi), etiket))
                {
                    throw new BolumVerisiHatasi($"Paket: bilinmeyen zorluk etiketi '{cift.Value}'.");
                }

                paket.Etiketler[cift.Key] = etiket;
            }

            return paket;
        }
    }

    /// <summary>Bir bölümün oynanabilirliği (GDD 8.11.1).</summary>
    public enum BolumErisimi
    {
        Hazir,

        /// <summary>Bölüm yayımlanmış ama paketi henüz indirilmemiş.</summary>
        PaketBekleniyor,

        /// <summary>Bölüm henüz yayımlanmamış: oyuncu içeriğin sonuna geldi.</summary>
        Yayimlanmadi,
    }

    /// <summary>
    /// İstemcideki bölüm kitaplığı (GDD 8.11): uygulamayla gelen ve sunucudan indirilen paketleri tutar, bölümleri
    /// ve zorluk etiketlerini verir. Sunucu yeni paket ekleyebilir ve var olanları güncelleyebilir; yayımlanmış en
    /// yüksek bölüm sunucudan gelir (8.11.1).
    /// </summary>
    public sealed class BolumKitapligi
    {
        private readonly ParametreTablosu _p;
        private readonly Dictionary<int, BolumPaketiVerisi> _paketler = new Dictionary<int, BolumPaketiVerisi>();
        private int? _yayimlanan;

        public BolumKitapligi(ParametreTablosu parametreler)
        {
            _p = parametreler;
            Duzen = new PaketDuzeni(parametreler);
        }

        public PaketDuzeni Duzen { get; }

        /// <summary>Kurulu paket numaraları → sürümleri.</summary>
        public IReadOnlyDictionary<int, int> KuruluPaketler => _paketler.ToDictionary(c => c.Key, c => c.Value.Surum);

        /// <summary>
        /// Yayımlanmış en yüksek bölüm: sunucu bildirdiyse o, bildirmediyse kurulu bölümlerin en yükseği.
        /// </summary>
        public int YayimlananEnYuksekBolum =>
            _yayimlanan ?? (_paketler.Count == 0 ? 0 : _paketler.Values.SelectMany(p => p.Bolumler.Keys).DefaultIfEmpty(0).Max());

        /// <summary>
        /// Paketi kurar. Aynı numaralı kurulu paketin sürümü aynı ya da daha yeniyse dokunulmaz. Paketteki her bölüm
        /// okunup doğrulanır; biri bile okunamıyorsa paket kurulmaz ve eski paket yerinde kalır.
        /// </summary>
        /// <returns>Paket kurulduysa true.</returns>
        /// <exception cref="BolumVerisiHatasi">Paket kendi aralığının dışında bölüm taşıyorsa ya da bir bölümü bozuksa.</exception>
        public bool Kur(BolumPaketiVerisi paket)
        {
            if (_paketler.TryGetValue(paket.Paket, out var kurulu) && kurulu.Surum >= paket.Surum)
            {
                return false;
            }

            foreach (var cift in paket.Bolumler)
            {
                if (Duzen.Paket(cift.Key) != paket.Paket)
                {
                    throw new BolumVerisiHatasi($"Paket {paket.Paket}: bölüm {cift.Key} bu paketin aralığında değil (GDD 8.11.2).");
                }

                var veri = BolumYukleyici.JsondanOku(cift.Value, _p);
                if (veri.Kimlik != cift.Key)
                {
                    throw new BolumVerisiHatasi($"Paket {paket.Paket}: {cift.Key}. bölümün dosyası {veri.Kimlik} kimliğini taşıyor.");
                }
            }

            foreach (var cift in paket.KurtarmaBolumleri)
            {
                BolumYukleyici.JsondanOku(cift.Value, _p);
            }

            _paketler[paket.Paket] = paket;
            return true;
        }

        public BolumErisimi Erisim(int bolum)
        {
            if (bolum < 1 || bolum > YayimlananEnYuksekBolum)
            {
                return BolumErisimi.Yayimlanmadi;
            }

            return BolumMetni(bolum) != null ? BolumErisimi.Hazir : BolumErisimi.PaketBekleniyor;
        }

        /// <summary>Bölüm dosyasının metni; bölüm kurulu değilse null.</summary>
        public string? BolumMetni(int bolum) =>
            _paketler.TryGetValue(Duzen.Paket(bolum), out var paket) && paket.Bolumler.TryGetValue(bolum, out string? metin) ? metin : null;

        public BolumVerisi? Bolum(int bolum)
        {
            string? metin = BolumMetni(bolum);
            return metin == null ? null : BolumYukleyici.JsondanOku(metin, _p);
        }

        /// <summary>Kurtarma bölümü; hiçbir kurulu pakette yoksa null.</summary>
        public BolumVerisi? KurtarmaBolumu(int kurtarmaNo)
        {
            foreach (var paket in _paketler.Values.OrderBy(p => p.Paket))
            {
                if (paket.KurtarmaBolumleri.TryGetValue(kurtarmaNo, out string? metin))
                {
                    return BolumYukleyici.JsondanOku(metin, _p);
                }
            }

            return null;
        }

        /// <summary>Kurulu paketlerin varsayılan etiket tablosu (GDD 8.8.1a); sunucu sonradan değiştirebilir.</summary>
        public ZorlukEtiketTablosu EtiketTablosu()
        {
            var tablo = new ZorlukEtiketTablosu();
            foreach (var cift in _paketler.Values.OrderBy(p => p.Paket).SelectMany(p => p.Etiketler))
            {
                tablo.Degistir(cift.Key, cift.Value);
            }

            return tablo;
        }

        /// <summary>
        /// Sunucuyla eşitler (GDD 8.11.1): yayımlanmış en yüksek bölümü alır; sunucudaki sürümü daha yeni olan ya da
        /// kurulu olmayan paketleri indirip kurar. İndirilemeyen ya da bozuk gelen paket atlanır, eskisi kalır.
        /// </summary>
        /// <returns>Bu çağrıda kurulan paketler; sunum bunları diske yazar. Sunucuya ulaşılamadıysa boş liste.</returns>
        public IReadOnlyList<BolumPaketiVerisi> Esitle(Sunucu.ISunucu sunucu)
        {
            var kurulan = new List<BolumPaketiVerisi>();
            var yapilandirma = sunucu.Yapilandirma();
            var surumler = sunucu.PaketSurumleri();
            if (!yapilandirma.Basarili || !surumler.Basarili)
            {
                return kurulan;
            }

            _yayimlanan = yapilandirma.Deger.YayimlananEnYuksekBolum;
            foreach (var cift in surumler.Deger.OrderBy(c => c.Key))
            {
                if (_paketler.TryGetValue(cift.Key, out var kurulu) && kurulu.Surum >= cift.Value)
                {
                    continue;
                }

                var yanit = sunucu.PaketIndir(cift.Key);
                if (!yanit.Basarili)
                {
                    continue;
                }

                try
                {
                    if (Kur(yanit.Deger))
                    {
                        kurulan.Add(yanit.Deger);
                    }
                }
                catch (BolumVerisiHatasi)
                {
                    // Bozuk paket kurulmaz; kurulu sürümle devam edilir.
                }
            }

            return kurulan;
        }
    }
}
