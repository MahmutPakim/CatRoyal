using System;
using System.Collections.Generic;
using System.Globalization;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Oyuncu kaydı okunamadı: bozuk dosya ya da bu sürümün tanımadığı daha yeni bir kayıt.</summary>
    public sealed class KayitHatasi : Exception
    {
        public KayitHatasi(string mesaj) : base(mesaj) { }

        public KayitHatasi(string mesaj, Exception ic) : base(mesaj, ic) { }
    }

    /// <summary>
    /// Oyuncu durumunun kalıcı kaydı (GDD 9.1.5, 9.2.8, 9.3.10): can anı, cüzdan, envanter ve sınırsız süreler,
    /// yıldız, alan, albüm, ödül hatları, kapılar, seri, günlük ödül, çarpan ve deneme sayaçları.
    ///
    /// Çekirdek yalnızca metne çevirir ve metinden kurar; dosyaya yazmak sunumun işidir. Kayıtta bir sürüm alanı
    /// vardır: eksik alanlar varsayılan değerle yüklenir (eski kayıtlar açılır), daha yeni sürümlü kayıt reddedilir.
    /// Sunucudan gelen ayarlar (özelliğin kapatılması, hile koruması) kaydedilmez; her açılışta sunucudan alınır.
    /// </summary>
    public static class OyuncuKaydi
    {
        /// <summary>Kayıt biçiminin sürümü. Alan eklemek sürümü artırmaz; anlam değişirse artırılır.</summary>
        public const int Surum = 1;

        public static string Yaz(Oyuncu oyuncu)
        {
            var kok = new Dictionary<string, object?>
            {
                ["surum"] = Surum,
                ["kimlik"] = oyuncu.Kimlik,
                ["en_yuksek_bolum"] = oyuncu.EnYuksekBolum,
                ["kazanma_serisi"] = oyuncu.KazanmaSerisi,
                ["yildiz"] = oyuncu.Yildiz,
                ["hesap_bagli"] = oyuncu.HesapBagli,
                ["bakiye"] = oyuncu.Cuzdan.Bakiye,
                ["yuksek_fiyat_duzeyi"] = oyuncu.Magaza.YuksekFiyatDuzeyi,
                ["can"] = oyuncu.Can.DurumuYaz(),
                ["envanter"] = oyuncu.Envanter.DurumuYaz(),
                ["kapilar"] = oyuncu.Kapilar.DurumuYaz(),
                ["gunluk_odul"] = oyuncu.GunlukOdul.DurumuYaz(),
                ["sezon_hatti"] = oyuncu.SezonHatti.DurumuYaz(),
                ["haftalik_hat"] = oyuncu.HaftalikHat.DurumuYaz(),
                ["carpan"] = oyuncu.Carpan.DurumuYaz(),
                ["zorluk"] = oyuncu.Zorluk.DurumuYaz(),
                ["alan"] = oyuncu.Alan.DurumuYaz(),
                ["album_sezonu"] = oyuncu.AlbumSezonu.DurumuYaz(),
                ["sezon_donemi"] = oyuncu.SezonDonemi.DurumuYaz(),
                ["hafta_donemi"] = oyuncu.HaftaDonemi.DurumuYaz(),
                ["pencereler"] = oyuncu.Pencereler.DurumuYaz(),
                ["kozmetikler"] = new List<object?>(oyuncu.Kozmetikler),
                ["olay_esyalari"] = OlayEsyalariniYaz(oyuncu),
                ["olaylar"] = oyuncu.Olaylar.DurumuYaz(),
                ["takim"] = oyuncu.Takim.DurumuYaz(),
                ["gelen_kutusu"] = oyuncu.GelenKutusu.DurumuYaz(),
            };
            return Json.Yaz(kok);
        }

        private static Dictionary<string, object?> OlayEsyalariniYaz(Oyuncu oyuncu)
        {
            var d = new Dictionary<string, object?>();
            foreach (var cift in oyuncu.OlayEsyalari)
            {
                d[cift.Key] = cift.Value;
            }

            return d;
        }

        /// <summary>Kayıt metninden oyuncuyu kurar.</summary>
        /// <exception cref="KayitHatasi">Metin bozuksa ya da kayıt daha yeni bir sürümle yazılmışsa.</exception>
        public static Oyuncu Yukle(string json, ParametreTablosu parametreler, DateTime simdi)
        {
            Dictionary<string, object?> kok;
            try
            {
                kok = Json.Ayristir(json) as Dictionary<string, object?>
                    ?? throw new KayitHatasi("Kayıt: kök bir nesne olmalı.");
            }
            catch (JsonHatasi hata)
            {
                throw new KayitHatasi("Kayıt: dosya bozuk.", hata);
            }

            if (!kok.ContainsKey("surum"))
            {
                throw new KayitHatasi("Kayıt: sürüm alanı yok.");
            }

            int surum = KayitAlani.TamSayi(kok, "surum", 0);
            if (surum < 1 || surum > Surum)
            {
                throw new KayitHatasi($"Kayıt: sürüm {surum} bu uygulamanın tanıdığı sürümden ({Surum}) farklı.");
            }

            var oyuncu = new Oyuncu(parametreler, simdi);
            if (kok.TryGetValue("kimlik", out object? oyuncuKimligi) && oyuncuKimligi is string kimlikMetni && kimlikMetni.Length > 0)
            {
                oyuncu.Kimlik = kimlikMetni;
            }

            oyuncu.EnYuksekBolum = Math.Max(1, KayitAlani.TamSayi(kok, "en_yuksek_bolum", 1));
            oyuncu.Yildiz = Math.Max(0, KayitAlani.TamSayi(kok, "yildiz", 0));
            oyuncu.TemelDurumuYukle(
                KayitAlani.TamSayi(kok, "kazanma_serisi", 0),
                KayitAlani.Mantik(kok, "hesap_bagli", false));
            oyuncu.Cuzdan.BakiyeyiYukle(KayitAlani.TamSayi(kok, "bakiye", oyuncu.Cuzdan.Bakiye));
            oyuncu.Magaza.YuksekFiyatDuzeyi = KayitAlani.Mantik(kok, "yuksek_fiyat_duzeyi", false);

            Bolum(kok, "can", oyuncu.Can.DurumuYukle);
            Bolum(kok, "envanter", oyuncu.Envanter.DurumuYukle);
            Bolum(kok, "kapilar", oyuncu.Kapilar.DurumuYukle);
            Bolum(kok, "gunluk_odul", oyuncu.GunlukOdul.DurumuYukle);
            Bolum(kok, "sezon_hatti", oyuncu.SezonHatti.DurumuYukle);
            Bolum(kok, "haftalik_hat", oyuncu.HaftalikHat.DurumuYukle);
            Bolum(kok, "carpan", oyuncu.Carpan.DurumuYukle);
            Bolum(kok, "zorluk", oyuncu.Zorluk.DurumuYukle);
            Bolum(kok, "alan", oyuncu.Alan.DurumuYukle);
            if (kok.ContainsKey("album_sezonu"))
            {
                Bolum(kok, "album_sezonu", oyuncu.AlbumSezonu.DurumuYukle);
            }
            else
            {
                // Albüm sezonu eklenmeden önceki kayıtlar yalnızca kartları taşır.
                Bolum(kok, "album", oyuncu.Album.DurumuYukle);
            }

            Bolum(kok, "sezon_donemi", oyuncu.SezonDonemi.DurumuYukle);
            Bolum(kok, "hafta_donemi", oyuncu.HaftaDonemi.DurumuYukle);
            foreach (object? kozmetik in KayitAlani.Dizi(kok, "kozmetikler"))
            {
                if (kozmetik is string ad) oyuncu.Kozmetikler.Add(ad);
            }

            foreach (var cift in KayitAlani.Nesne(kok, "olay_esyalari"))
            {
                oyuncu.OlayEsyalari[cift.Key] = Math.Max(0, KayitAlani.TamSayi(cift.Value, cift.Key));
            }

            Bolum(kok, "olaylar", oyuncu.Olaylar.DurumuYukle);
            Bolum(kok, "takim", oyuncu.Takim.DurumuYukle);
            oyuncu.GelenKutusu.DurumuYukle(KayitAlani.Dizi(kok, "gelen_kutusu"));

            if (kok.ContainsKey("pencereler"))
            {
                Bolum(kok, "pencereler", oyuncu.Pencereler.DurumuYukle);
            }
            else
            {
                // Pencere kaydı olmayan eski kayıt: ilerlemiş oyuncuya eski tanıtımlar yeniden gösterilmez.
                OzellikTanitimlari.AcikOlanlariGosterildiSay(oyuncu);
            }

            return oyuncu;
        }

        /// <summary>Kayıtta olmayan bölüm atlanır: o sistem yeni oyuncudaki haliyle kalır.</summary>
        private static void Bolum(Dictionary<string, object?> kok, string ad, Action<Dictionary<string, object?>> yukle)
        {
            if (!kok.TryGetValue(ad, out object? deger) || deger == null)
            {
                return;
            }

            if (!(deger is Dictionary<string, object?> nesne))
            {
                throw new KayitHatasi($"Kayıt: '{ad}' bir nesne olmalı.");
            }

            yukle(nesne);
        }
    }

    /// <summary>Kayıt alanlarını okuyan ve yazan yardımcılar. Tür uyuşmazlığı <see cref="KayitHatasi"/> verir.</summary>
    internal static class KayitAlani
    {
        private static readonly Dictionary<string, object?> BosNesne = new Dictionary<string, object?>();
        private static readonly List<object?> BosDizi = new List<object?>();

        /// <summary>Zaman, tik kaybı olmadan gidiş-dönüş biçiminde yazılır (JSON sayıları tikleri taşıyamaz).</summary>
        public static string Zaman(DateTime an) => an.ToString("o", CultureInfo.InvariantCulture);

        public static DateTime Zaman(object? deger, string ad)
        {
            if (deger is string metin
                && DateTime.TryParseExact(metin, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var an))
            {
                return an;
            }

            throw new KayitHatasi($"Kayıt: '{ad}' bir zaman değil.");
        }

        public static DateTime Zaman(Dictionary<string, object?> d, string ad, DateTime varsayilan) =>
            d.TryGetValue(ad, out object? deger) && deger != null ? Zaman(deger, ad) : varsayilan;

        public static TimeSpan? Sure(Dictionary<string, object?> d, string ad) =>
            d.TryGetValue(ad, out object? deger) && deger != null
                ? TimeSpan.FromTicks(UzunSayi(deger, ad))
                : (TimeSpan?)null;

        public static long UzunSayi(object? deger, string ad)
        {
            if (deger is double sayi && Math.Floor(sayi) == sayi && Math.Abs(sayi) < 9e15)
            {
                return (long)sayi;
            }

            throw new KayitHatasi($"Kayıt: '{ad}' bir tam sayı değil.");
        }

        public static int TamSayi(object? deger, string ad)
        {
            long sayi = UzunSayi(deger, ad);
            if (sayi < int.MinValue || sayi > int.MaxValue)
            {
                throw new KayitHatasi($"Kayıt: '{ad}' aralık dışında.");
            }

            return (int)sayi;
        }

        public static int TamSayi(Dictionary<string, object?> d, string ad, int varsayilan) =>
            d.TryGetValue(ad, out object? deger) && deger != null ? TamSayi(deger, ad) : varsayilan;

        public static bool Mantik(Dictionary<string, object?> d, string ad, bool varsayilan)
        {
            if (!d.TryGetValue(ad, out object? deger) || deger == null)
            {
                return varsayilan;
            }

            return deger is bool b ? b : throw new KayitHatasi($"Kayıt: '{ad}' doğru/yanlış değil.");
        }

        public static Dictionary<string, object?> Nesne(Dictionary<string, object?> d, string ad)
        {
            if (!d.TryGetValue(ad, out object? deger) || deger == null)
            {
                return BosNesne;
            }

            return deger as Dictionary<string, object?> ?? throw new KayitHatasi($"Kayıt: '{ad}' bir nesne olmalı.");
        }

        public static List<object?> Dizi(Dictionary<string, object?> d, string ad)
        {
            if (!d.TryGetValue(ad, out object? deger) || deger == null)
            {
                return BosDizi;
            }

            return deger as List<object?> ?? throw new KayitHatasi($"Kayıt: '{ad}' bir dizi olmalı.");
        }
    }
}
