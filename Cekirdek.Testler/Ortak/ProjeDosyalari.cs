using System;
using System.IO;
using CatRoyal.Cekirdek.Yapilandirma;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Ortak
{
    /// <summary>Testlerin depo kökündeki docs/ ve veri/ dosyalarına erişimi.</summary>
    public static class ProjeDosyalari
    {
        private static readonly Lazy<string> KokYolu = new Lazy<string>(KokuBul);
        private static readonly Lazy<ParametreTablosu> Tablo =
            new Lazy<ParametreTablosu>(() => ParametreTablosu.CsvdenOku(Oku("docs", "parametreler.csv")));

        public static string Kok => KokYolu.Value;

        /// <summary>docs/parametreler.csv, öneri değerleriyle.</summary>
        public static ParametreTablosu Parametreler => Tablo.Value;

        public static string Oku(params string[] parcalar) =>
            File.ReadAllText(Path.Combine(Kok, Path.Combine(parcalar)));

        private static string KokuBul()
        {
            var klasor = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (klasor != null)
            {
                if (File.Exists(Path.Combine(klasor.FullName, "docs", "parametreler.csv")))
                {
                    return klasor.FullName;
                }

                klasor = klasor.Parent;
            }

            throw new InvalidOperationException("docs/parametreler.csv bulunamadı.");
        }
    }
}
