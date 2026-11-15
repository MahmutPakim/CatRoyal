using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Veri
{
    /// <summary>Bölüm verisi hatası: bozuk dosya ya da GDD sınırlarını aşan değer.</summary>
    public sealed class BolumVerisiHatasi : Exception
    {
        public BolumVerisiHatasi(string mesaj) : base(mesaj) { }
    }

    /// <summary>Dolgu setindeki bir öğenin verisi (GDD 8.10 "Dolgu setleri").</summary>
    public sealed class DolguOgesiVerisi
    {
        public DolguOgesiVerisi(string kod, double agirlik, int? azami)
        {
            Kod = kod;
            Agirlik = agirlik;
            Azami = azami;
        }

        /// <summary>Hücre içerik kodu; "?" rastgele renktir.</summary>
        public string Kod { get; }
        public double Agirlik { get; }
        public int? Azami { get; }
    }

    public sealed class DolguSetiVerisi
    {
        public List<DolguOgesiVerisi> Ogeler { get; } = new List<DolguOgesiVerisi>();
        public double Pas { get; set; }
    }

    /// <summary>
    /// Bölüm dosyasının içeriği (GDD 8.10). Biçim ve alan adları projenin kendi kararıdır; ayrıntısı
    /// veri/bolumler/BICIM.md dosyasında. Zorluk etiketi bölüm dosyasında değil, ayrı etiket tablosundadır (8.8.1a).
    /// </summary>
    public sealed class BolumVerisi
    {
        /// <summary>Bölüm numarası.</summary>
        public int Kimlik { get; internal set; }

        /// <summary>Düzenleme sürümü; bölümler yayından sonra güncellenebilir.</summary>
        public int Surum { get; internal set; }

        public int Genislik { get; internal set; }
        public int Yukseklik { get; internal set; }

        /// <summary>Hamle sayısı (GDD 8.3).</summary>
        public int Hamle { get; internal set; }

        /// <summary>Renk sayısı; göreli renk kimlikleri 0'dan başlar (GDD 8.2).</summary>
        public int RenkSayisi { get; internal set; }

        public IReadOnlyList<double>? RenkOranlari { get; internal set; }

        /// <summary>Hücre içerik kodları; [satır][sütun], 0 tabanlı.</summary>
        public string[][] Hucreler { get; internal set; } = Array.Empty<string[]>();

        public string[][]? AltKatmanlar { get; internal set; }
        public string[][]? UstKatmanlar { get; internal set; }

        /// <summary>Giriş hücreleri; verilmezse her sütunun en üstteki oynanabilir hücresi giriştir (GDD 8.1.6).</summary>
        public bool[][]? Girisler { get; internal set; }

        public List<Konum> ToplamaHucreleri { get; } = new List<Konum>();

        /// <summary>Sütun başına giriş sırası (GDD 5.3.4 adım 2).</summary>
        public Dictionary<int, List<string>> OnTanimliDolgu { get; } = new Dictionary<int, List<string>>();

        public List<DolguSetiVerisi> DolguSetleri { get; } = new List<DolguSetiVerisi>();
        public Dictionary<int, DolguSetiVerisi> SutunSetleri { get; } = new Dictionary<int, DolguSetiVerisi>();

        public List<Hedef> Hedefler { get; } = new List<Hedef>();

        public Dictionary<string, EngelTanimi> Engeller { get; } = new Dictionary<string, EngelTanimi>(StringComparer.Ordinal);
        public Dictionary<string, KatmanTanimi> Katmanlar { get; } = new Dictionary<string, KatmanTanimi>(StringComparer.Ordinal);

        public bool CaprazKaymaIzinli { get; internal set; }
        public bool CokEkranli { get; internal set; }
        public bool TahtadaDelikVar { get; internal set; }

        /// <summary>İsteğe bağlı sınır listesi (tür ve adet); ör. süreli bölümde süre.</summary>
        public List<KeyValuePair<string, double>> EkSinirlar { get; } = new List<KeyValuePair<string, double>>();
    }

    /// <summary>Bölüm dosyasını okur ve GDD sınırlarına karşı doğrular.</summary>
    public static class BolumYukleyici
    {
        public static BolumVerisi JsondanOku(string json, ParametreTablosu parametreler)
        {
            Dictionary<string, object?> kok;
            try
            {
                kok = Json.Ayristir(json) as Dictionary<string, object?>
                    ?? throw new BolumVerisiHatasi("Bölüm dosyasının kökü bir nesne olmalı.");
            }
            catch (JsonHatasi hata)
            {
                throw new BolumVerisiHatasi("Bölüm dosyası okunamadı: " + hata.Message);
            }

            var veri = new BolumVerisi
            {
                Kimlik = TamSayi(kok, "kimlik"),
                Surum = TamSayi(kok, "surum", 1),
                Genislik = TamSayi(kok, "genislik"),
                Yukseklik = TamSayi(kok, "yukseklik"),
                Hamle = TamSayi(kok, "hamle"),
                RenkSayisi = TamSayi(kok, "renkler"),
            };

            if (kok.TryGetValue("renkOranlari", out var oranlar) && oranlar != null)
            {
                veri.RenkOranlari = Dizi(oranlar, "renkOranlari").Select(o => Sayi(o, "renkOranlari")).ToList();
            }

            if (Nesne(kok, "bayraklar") is { } bayraklar)
            {
                veri.CaprazKaymaIzinli = Mantiksal(bayraklar, "caprazKayma");
                veri.CokEkranli = Mantiksal(bayraklar, "cokEkranli");
                veri.TahtadaDelikVar = Mantiksal(bayraklar, "delik");
            }

            if (Nesne(kok, "katmanlar") is { } katmanlar)
            {
                foreach (var cift in katmanlar)
                {
                    veri.Katmanlar.Add(cift.Key, KatmanTanimiOku(cift.Key, cift.Value));
                }
            }

            if (Nesne(kok, "engeller") is { } engeller)
            {
                foreach (var cift in engeller)
                {
                    veri.Engeller.Add(cift.Key, EngelTanimiOku(cift.Key, cift.Value, veri));
                }
            }

            veri.Hucreler = Izgara(kok, "hucreler", veri, zorunlu: true)!;
            veri.AltKatmanlar = Izgara(kok, "altKatmanlar", veri, zorunlu: false);
            veri.UstKatmanlar = Izgara(kok, "ustKatmanlar", veri, zorunlu: false);

            var girisler = Izgara(kok, "girisler", veri, zorunlu: false);
            if (girisler != null)
            {
                veri.Girisler = girisler.Select(satir => satir.Select(kod => kod == "1").ToArray()).ToArray();
            }

            if (kok.TryGetValue("toplamaHucreleri", out var toplama) && toplama != null)
            {
                foreach (var oge in Dizi(toplama, "toplamaHucreleri"))
                {
                    var cift = Dizi(oge, "toplamaHucreleri");
                    if (cift.Count != 2)
                    {
                        throw new BolumVerisiHatasi("toplamaHucreleri: her öğe [satır, sütun] olmalı.");
                    }

                    veri.ToplamaHucreleri.Add(new Konum((int)Sayi(cift[0], "toplamaHucreleri"), (int)Sayi(cift[1], "toplamaHucreleri")));
                }
            }

            if (Nesne(kok, "onTanimliDolgu") is { } onTanimli)
            {
                foreach (var cift in onTanimli)
                {
                    veri.OnTanimliDolgu.Add(
                        SutunNo(cift.Key, veri),
                        Dizi(cift.Value, "onTanimliDolgu").Select(k => Metin(k, "onTanimliDolgu")).ToList());
                }
            }

            if (kok.TryGetValue("dolguSetleri", out var setler) && setler != null)
            {
                foreach (var set in Dizi(setler, "dolguSetleri"))
                {
                    veri.DolguSetleri.Add(DolguSetiOku(set));
                }
            }

            if (Nesne(kok, "sutunSetleri") is { } sutunSetleri)
            {
                foreach (var cift in sutunSetleri)
                {
                    veri.SutunSetleri.Add(SutunNo(cift.Key, veri), DolguSetiOku(cift.Value));
                }
            }

            if (kok.TryGetValue("hedefler", out var hedefler) && hedefler != null)
            {
                foreach (var oge in Dizi(hedefler, "hedefler"))
                {
                    var hedef = oge as Dictionary<string, object?> ?? throw new BolumVerisiHatasi("hedefler: her öğe bir nesne olmalı.");
                    veri.Hedefler.Add(new Hedef(MetinAl(hedef, "tur"), TamSayi(hedef, "adet", 0), TamSayi(hedef, "ekran", 1)));
                }
            }

            if (kok.TryGetValue("ekSinirlar", out var sinirlar) && sinirlar != null)
            {
                foreach (var oge in Dizi(sinirlar, "ekSinirlar"))
                {
                    var sinir = oge as Dictionary<string, object?> ?? throw new BolumVerisiHatasi("ekSinirlar: her öğe bir nesne olmalı.");
                    veri.EkSinirlar.Add(new KeyValuePair<string, double>(MetinAl(sinir, "tur"), Sayi(sinir["adet"], "ekSinirlar")));
                }
            }

            Dogrula(veri, parametreler);
            return veri;
        }

        // ------------------------------------------------------------------ Doğrulama

        private static void Dogrula(BolumVerisi veri, ParametreTablosu p)
        {
            string on = $"Bölüm {veri.Kimlik}: ";

            if (veri.Hamle < 1)
            {
                throw new BolumVerisiHatasi(on + "hamle sayısı en az 1 olmalı.");
            }

            // GDD 8.1.1: genişlik sabit üst sınırı aşmaz. 8.1.3: standart yüksekliği aşan tahtalar yalnız çok ekranlıdır.
            if (veri.Genislik < 1 || veri.Genislik > p.TamSayi(ParametreAdlari.TahtaGenislikStandart))
            {
                throw new BolumVerisiHatasi(on + "genişlik 1 ile parametre: tahta_genislik_standart arasında olmalı (GDD 8.1.1).");
            }

            if (veri.CokEkranli)
            {
                throw new BolumVerisiHatasi(on + "çok ekranlı bölümler (GDD 8.9) henüz desteklenmiyor.");
            }

            if (veri.Yukseklik < 1 || veri.Yukseklik > p.TamSayi(ParametreAdlari.TahtaYukseklikStandart))
            {
                throw new BolumVerisiHatasi(on + "yükseklik 1 ile parametre: tahta_yukseklik_standart arasında olmalı (GDD 8.1.3).");
            }

            // GDD 8.2.2: standarttan fazla renk yalnız istisnai bölümlerde.
            int azamiRenk = p.TamSayi(ParametreAdlari.RenkSayisiStandart) + p.TamSayi(ParametreAdlari.IstisnaFazlaRenk);
            if (veri.RenkSayisi < 1 || veri.RenkSayisi > azamiRenk)
            {
                throw new BolumVerisiHatasi(on + $"renk sayısı 1 ile {azamiRenk} arasında olmalı (GDD 8.2.2).");
            }

            if (veri.RenkOranlari != null && veri.RenkOranlari.Count != veri.RenkSayisi)
            {
                throw new BolumVerisiHatasi(on + "renkOranlari, renk sayısı kadar öğe taşımalı.");
            }

            // GDD 8.4.1: bir ekranda en fazla parametre: ekran_azami_hedef hedef türü.
            int azamiHedef = p.TamSayi(ParametreAdlari.EkranAzamiHedef);
            if (veri.Hedefler.GroupBy(h => h.Ekran).Any(g => g.Count() > azamiHedef))
            {
                throw new BolumVerisiHatasi(on + $"bir ekranda en fazla {azamiHedef} hedef türü olabilir (GDD 8.4.1).");
            }

            foreach (var tanim in veri.Engeller.Values)
            {
                AzamiKatman(tanim.AzamiKatman, tanim.AzamiKatmanParametresi, p);
            }

            for (int s = 0; s < veri.Yukseklik; s++)
            {
                for (int t = 0; t < veri.Genislik; t++)
                {
                    var konum = new Konum(s + 1, t + 1);
                    var icerik = BolumKurucu.IcerikCoz(veri, veri.Hucreler[s][t], on + $"hücre {konum}");
                    if (icerik.Engel != null)
                    {
                        KatmanSiniri(icerik.Engel.Ad, icerik.Katman, AzamiKatman(icerik.Engel.AzamiKatman, icerik.Engel.AzamiKatmanParametresi, p), on, konum);
                    }

                    foreach (var izgara in new[] { veri.AltKatmanlar, veri.UstKatmanlar })
                    {
                        if (izgara == null || izgara[s][t] == ".")
                        {
                            continue;
                        }

                        var katman = BolumKurucu.KatmanCoz(veri, izgara[s][t], on + $"hücre {konum}");
                        if (katman.Tanim.Ustte != ReferenceEquals(izgara, veri.UstKatmanlar))
                        {
                            throw new BolumVerisiHatasi(on + $"hücre {konum}: '{katman.Tanim.Ad}' yanlış katman haritasında.");
                        }

                        if (icerik.Oynanmayan)
                        {
                            throw new BolumVerisiHatasi(on + $"hücre {konum}: oynanmayan hücre katman taşıyamaz (GDD 2.1.3).");
                        }

                        KatmanSiniri(katman.Tanim.Ad, katman.Katman, AzamiKatman(katman.Tanim.AzamiKatman, katman.Tanim.AzamiKatmanParametresi, p), on, konum);
                    }
                }
            }

            foreach (var kod in veri.OnTanimliDolgu.Values.SelectMany(k => k)
                .Concat(veri.DolguSetleri.Concat(veri.SutunSetleri.Values).SelectMany(s => s.Ogeler).Select(o => o.Kod)))
            {
                var icerik = BolumKurucu.IcerikCoz(veri, kod, on + "dolgu");
                if (icerik.Oynanmayan || icerik.Bos)
                {
                    throw new BolumVerisiHatasi(on + $"dolgu kodu '{kod}' bir taş ya da engel olmalı.");
                }
            }
        }

        private static int? AzamiKatman(int? ezme, string? parametre, ParametreTablosu p) =>
            ezme ?? (parametre != null ? p.TamSayi(parametre) : (int?)null);

        private static void KatmanSiniri(string ad, int katman, int? azami, string on, Konum konum)
        {
            if (azami.HasValue && katman > azami.Value)
            {
                throw new BolumVerisiHatasi(on + $"hücre {konum}: '{ad}' en fazla {azami.Value} katmanlı olabilir (GDD 6.1.1, 6.4).");
            }
        }

        // ------------------------------------------------------------------ Tanımlar

        private static KatmanTanimi KatmanTanimiOku(string ad, object? deger)
        {
            var nesne = deger as Dictionary<string, object?> ?? throw new BolumVerisiHatasi($"katmanlar.{ad}: nesne bekleniyordu.");
            KatmanTanimi tanim;
            switch (MetinAl(nesne, "arketip"))
            {
                case "zemin": tanim = KatmanArketipleri.Zemin(); break;
                case "yayilan_zemin": tanim = KatmanArketipleri.YayilanZemin(); break;
                case "yapiskan_kaplama": tanim = KatmanArketipleri.YapiskanKaplama(); break;
                case "kilit": tanim = KatmanArketipleri.Kilit(); break;
                case "ortu": tanim = KatmanArketipleri.Ortu(); break;
                case "sert_kaplama": tanim = KatmanArketipleri.SertKaplama(); break;
                default: throw new BolumVerisiHatasi($"katmanlar.{ad}: bilinmeyen arketip '{MetinAl(nesne, "arketip")}' (GDD 6.2.2).");
            }

            if (nesne.ContainsKey("hedefTuru")) tanim.HedefTuru = nesne["hedefTuru"] as string;
            if (nesne.ContainsKey("azamiKatman")) tanim.AzamiKatman = TamSayi(nesne, "azamiKatman");
            BagisikliklariOku(nesne, tanim.Bagisikliklar, "katmanlar." + ad);
            return tanim;
        }

        private static EngelTanimi EngelTanimiOku(string ad, object? deger, BolumVerisi veri)
        {
            var nesne = deger as Dictionary<string, object?> ?? throw new BolumVerisiHatasi($"engeller.{ad}: nesne bekleniyordu.");
            bool yanHasar = !nesne.ContainsKey("yanEslesmeHasari") || Mantiksal(nesne, "yanEslesmeHasari");
            EngelTanimi tanim;
            switch (MetinAl(nesne, "arketip"))
            {
                case "katmanli_blok": tanim = Arketipler.KatmanliBlok(); break;
                case "sert_blok": tanim = Arketipler.SertBlok(); break;
                case "dusen_kirilabilir": tanim = Arketipler.DusenKirilabilir(); break;
                case "toplanabilir": tanim = Arketipler.AsagiInenToplanabilir(); break;
                case "coklu_vurus": tanim = Arketipler.CokluVurusBlogu(); break;
                case "kaydirilamayan_dusen": tanim = Arketipler.KaydirilamayanDusenBlok(yanHasar); break;
                case "para_tasi": tanim = Arketipler.ParaTasi(); break;
                case "yayilan_tehlike": tanim = Arketipler.YayilanTehlike(); break;
                case "uretici":
                    string katmanAdi = MetinAl(nesne, "birakilanKatman");
                    if (!veri.Katmanlar.TryGetValue(katmanAdi, out var birakilan))
                    {
                        throw new BolumVerisiHatasi($"engeller.{ad}: '{katmanAdi}' adlı katman tanımı yok.");
                    }

                    tanim = Arketipler.Uretici(birakilan);
                    break;
                default: throw new BolumVerisiHatasi($"engeller.{ad}: bilinmeyen arketip '{MetinAl(nesne, "arketip")}' (GDD 6.2.1).");
            }

            if (nesne.ContainsKey("hedefTuru")) tanim.HedefTuru = nesne["hedefTuru"] as string;
            if (nesne.ContainsKey("azamiKatman")) tanim.AzamiKatman = TamSayi(nesne, "azamiKatman");
            if (nesne.ContainsKey("gudumluOnceligi")) tanim.GudumluOnceligi = Mantiksal(nesne, "gudumluOnceligi");
            if (nesne.ContainsKey("gudumluHedefPuani")) tanim.GudumluHedefPuani = Sayi(nesne["gudumluHedefPuani"], "gudumluHedefPuani");
            if (nesne.ContainsKey("araclaSecilebilir")) tanim.AraclaSecilebilir = Mantiksal(nesne, "araclaSecilebilir");
            if (nesne.ContainsKey("tanitim")) tanim.TanitimBolumu = TamSayi(nesne, "tanitim");
            BagisikliklariOku(nesne, tanim.Bagisikliklar, "engeller." + ad);
            return tanim;
        }

        private static void BagisikliklariOku(Dictionary<string, object?> nesne, ISet<VurusTuru> hedef, string yer)
        {
            if (!nesne.TryGetValue("bagisikliklar", out var deger) || deger == null)
            {
                return;
            }

            foreach (var oge in Dizi(deger, yer + ".bagisikliklar"))
            {
                switch (Metin(oge, yer))
                {
                    case "cizgi": hedef.Add(VurusTuru.Cizgi); break;
                    case "bomba": hedef.Add(VurusTuru.Bomba); break;
                    case "gudumlu": hedef.Add(VurusTuru.Gudumlu); break;
                    case "renk_isini": hedef.Add(VurusTuru.RenkIsini); break;
                    case "tahta_temizleme": hedef.Add(VurusTuru.TahtaTemizleme); break;
                    case "arac": hedef.Add(VurusTuru.Arac); break;
                    default: throw new BolumVerisiHatasi($"{yer}: bilinmeyen vuruş türü '{oge}'.");
                }
            }
        }

        private static DolguSetiVerisi DolguSetiOku(object? deger)
        {
            var nesne = deger as Dictionary<string, object?> ?? throw new BolumVerisiHatasi("dolgu seti: nesne bekleniyordu.");
            var set = new DolguSetiVerisi { Pas = nesne.ContainsKey("pas") ? Sayi(nesne["pas"], "pas") : 0 };
            foreach (var oge in Dizi(nesne.TryGetValue("ogeler", out var ogeler) ? ogeler : null, "ogeler"))
            {
                var o = oge as Dictionary<string, object?> ?? throw new BolumVerisiHatasi("dolgu seti öğesi: nesne bekleniyordu.");
                set.Ogeler.Add(new DolguOgesiVerisi(
                    MetinAl(o, "kod"),
                    o.ContainsKey("agirlik") ? Sayi(o["agirlik"], "agirlik") : 1,
                    o.ContainsKey("azami") ? TamSayi(o, "azami") : (int?)null));
            }

            return set;
        }

        // ------------------------------------------------------------------ JSON yardımcıları

        private static string[][]? Izgara(Dictionary<string, object?> kok, string ad, BolumVerisi veri, bool zorunlu)
        {
            if (!kok.TryGetValue(ad, out var deger) || deger == null)
            {
                if (zorunlu)
                {
                    throw new BolumVerisiHatasi($"'{ad}' alanı zorunlu.");
                }

                return null;
            }

            var satirlar = Dizi(deger, ad);
            if (satirlar.Count != veri.Yukseklik)
            {
                throw new BolumVerisiHatasi($"'{ad}': {veri.Yukseklik} satır bekleniyordu, {satirlar.Count} bulundu.");
            }

            var izgara = new string[veri.Yukseklik][];
            for (int s = 0; s < satirlar.Count; s++)
            {
                var kodlar = Metin(satirlar[s], ad).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (kodlar.Length != veri.Genislik)
                {
                    throw new BolumVerisiHatasi($"'{ad}' satır {s + 1}: {veri.Genislik} hücre bekleniyordu, {kodlar.Length} bulundu.");
                }

                izgara[s] = kodlar;
            }

            return izgara;
        }

        private static int SutunNo(string metin, BolumVerisi veri)
        {
            if (!int.TryParse(metin, NumberStyles.None, CultureInfo.InvariantCulture, out int sutun) || sutun < 1 || sutun > veri.Genislik)
            {
                throw new BolumVerisiHatasi($"'{metin}' geçerli bir sütun numarası değil.");
            }

            return sutun;
        }

        private static Dictionary<string, object?>? Nesne(Dictionary<string, object?> kok, string ad)
        {
            if (!kok.TryGetValue(ad, out var deger) || deger == null)
            {
                return null;
            }

            return deger as Dictionary<string, object?> ?? throw new BolumVerisiHatasi($"'{ad}': nesne bekleniyordu.");
        }

        private static List<object?> Dizi(object? deger, string ad) =>
            deger as List<object?> ?? throw new BolumVerisiHatasi($"'{ad}': dizi bekleniyordu.");

        private static string Metin(object? deger, string ad) =>
            deger as string ?? throw new BolumVerisiHatasi($"'{ad}': metin bekleniyordu.");

        private static string MetinAl(Dictionary<string, object?> nesne, string ad) =>
            nesne.TryGetValue(ad, out var deger) ? Metin(deger, ad) : throw new BolumVerisiHatasi($"'{ad}' alanı zorunlu.");

        private static double Sayi(object? deger, string ad) =>
            deger is double sayi ? sayi : throw new BolumVerisiHatasi($"'{ad}': sayı bekleniyordu.");

        private static bool Mantiksal(Dictionary<string, object?> nesne, string ad)
        {
            if (!nesne.TryGetValue(ad, out var deger) || deger == null)
            {
                return false;
            }

            return deger is bool b ? b : throw new BolumVerisiHatasi($"'{ad}': true ya da false bekleniyordu.");
        }

        private static int TamSayi(Dictionary<string, object?> nesne, string ad, int? varsayilan = null)
        {
            if (!nesne.TryGetValue(ad, out var deger) || deger == null)
            {
                return varsayilan ?? throw new BolumVerisiHatasi($"'{ad}' alanı zorunlu.");
            }

            double sayi = Sayi(deger, ad);
            if (Math.Abs(sayi - Math.Round(sayi)) > 1e-9)
            {
                throw new BolumVerisiHatasi($"'{ad}': tam sayı bekleniyordu.");
            }

            return (int)Math.Round(sayi);
        }
    }
}
