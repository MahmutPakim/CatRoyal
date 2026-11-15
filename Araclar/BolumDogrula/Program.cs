using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CatRoyal.Cekirdek.Tasarim;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Araclar
{
    /// <summary>
    /// Bir klasördeki bölüm dosyalarını (bolum_NNN.json, kurtarma_NNN.json) tasarım kurallarına karşı denetler.
    /// Çıkış kodu: 0 hata yok, 1 hata var, 2 araç çalışamadı.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] argumanlar)
        {
            bool ozet = argumanlar.Contains("--olculer");
            var yollar = argumanlar.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
            string? kok = DepoKoku();
            string klasor = yollar.Count > 0 ? yollar[0] : kok == null ? string.Empty : Path.Combine(kok, "veri", "bolumler");
            string parametreYolu = yollar.Count > 1 ? yollar[1] : kok == null ? string.Empty : Path.Combine(kok, "docs", "parametreler.csv");

            if (!Directory.Exists(klasor) || !File.Exists(parametreYolu))
            {
                Console.Error.WriteLine("Kullanım: BolumDogrula [bölüm klasörü] [parametreler.csv] [--olculer]");
                Console.Error.WriteLine("Verilmezse depodaki veri/bolumler ve docs/parametreler.csv kullanılır.");
                return 2;
            }

            var p = ParametreTablosu.CsvdenOku(File.ReadAllText(parametreYolu));
            var sorunlar = new List<BolumSorunu>();
            var bolumler = Oku(klasor, "bolum_*.json", p, sorunlar);
            var kurtarma = Oku(klasor, "kurtarma_*.json", p, sorunlar).ToDictionary(b => b.Kimlik);

            sorunlar.AddRange(BolumDogrulayici.PaketiDogrula(bolumler, p, kurtarma));

            if (ozet)
            {
                Console.WriteLine("bölüm  hamle  hedef  oran   yoğunluk  oynanmayan  boş     engel türleri");
                foreach (var bolum in bolumler.OrderBy(b => b.Kimlik))
                {
                    var o = BolumDogrulayici.Olc(bolum);
                    Console.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0,5}  {1,5}  {2,5}  {3,5:0.00}  {4,8:0.00}  {5,10:0.00}  {6,6:0.00}  {7}",
                        bolum.Kimlik, o.Hamle, o.HedefAdedi, o.HedefHamleOrani, o.EngelYogunlugu, o.OynanmayanPayi, o.BosBaslayanPayi,
                        string.Join(", ", o.EngelTurleri)));
                }

                Console.WriteLine();
            }

            foreach (var sorun in sorunlar)
            {
                Console.WriteLine(sorun);
            }

            int hata = sorunlar.Count(s => s.Duzey == SorunDuzeyi.Hata);
            Console.WriteLine($"{bolumler.Count} bölüm, {kurtarma.Count} kurtarma bölümü: {hata} hata, {sorunlar.Count - hata} uyarı.");
            return hata > 0 ? 1 : 0;
        }

        private static List<BolumVerisi> Oku(string klasor, string desen, ParametreTablosu p, List<BolumSorunu> sorunlar)
        {
            var okunan = new List<BolumVerisi>();
            foreach (string yol in Directory.GetFiles(klasor, desen).OrderBy(y => y, StringComparer.Ordinal))
            {
                try
                {
                    okunan.Add(BolumYukleyici.JsondanOku(File.ReadAllText(yol), p));
                }
                catch (BolumVerisiHatasi hata)
                {
                    sorunlar.Add(new BolumSorunu(SorunDuzeyi.Hata, Path.GetFileName(yol), "8.10", hata.Message));
                }
            }

            return okunan;
        }

        private static string? DepoKoku()
        {
            for (var klasor = new DirectoryInfo(Directory.GetCurrentDirectory()); klasor != null; klasor = klasor.Parent)
            {
                if (File.Exists(Path.Combine(klasor.FullName, "docs", "parametreler.csv")))
                {
                    return klasor.FullName;
                }
            }

            return null;
        }
    }
}
