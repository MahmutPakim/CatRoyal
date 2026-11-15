using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;
using UnityEngine;

namespace CatRoyal.Sunum
{
    /// <summary>
    /// Oyunun çalışma anında okuduğu veriler: parametreler.csv, AK bayrakları ve bölüm dosyaları.
    /// Dosyalar StreamingAssets/CatRoyal altındadır; editörde VeriEsitleyici onları depo kökünden kopyalar.
    /// </summary>
    public sealed class OyunVerisi
    {
        private OyunVerisi(ParametreTablosu parametreler, AkBayraklari bayraklar, List<string> bolumDosyalari)
        {
            Parametreler = parametreler;
            Bayraklar = bayraklar;
            BolumDosyalari = bolumDosyalari;
        }

        public ParametreTablosu Parametreler { get; }
        public AkBayraklari Bayraklar { get; }

        /// <summary>Bölüm dosyalarının tam yolları, ada göre sıralı.</summary>
        public IReadOnlyList<string> BolumDosyalari { get; }

        private static string Kok => Path.Combine(Application.streamingAssetsPath, "CatRoyal");

        public static OyunVerisi Yukle()
        {
            var parametreler = ParametreTablosu.CsvdenOku(File.ReadAllText(Path.Combine(Kok, "parametreler.csv")));

            string bayrakYolu = Path.Combine(Kok, "veri", "ak_bayraklari.json");
            var bayraklar = File.Exists(bayrakYolu)
                ? AkBayraklari.JsondanOku(File.ReadAllText(bayrakYolu))
                : AkBayraklari.Varsayilan;

            string bolumKlasoru = Path.Combine(Kok, "veri", "bolumler");
            var bolumler = Directory.Exists(bolumKlasoru)
                ? Directory.GetFiles(bolumKlasoru, "bolum_*.json").OrderBy(yol => yol).ToList()
                : new List<string>();

            return new OyunVerisi(parametreler, bayraklar, bolumler);
        }

        /// <summary>
        /// Zorluk etiketi tablosu (GDD 8.8.1a): bölüm dosyasından ayrı tutulur; dosya yoksa bütün bölümler normaldir.
        /// </summary>
        public CatRoyal.Cekirdek.Meta.ZorlukEtiketTablosu EtiketTablosu()
        {
            string yol = Path.Combine(Kok, "veri", "zorluk_etiketleri.json");
            return File.Exists(yol)
                ? CatRoyal.Cekirdek.Meta.ZorlukEtiketTablosu.JsondanOku(File.ReadAllText(yol))
                : new CatRoyal.Cekirdek.Meta.ZorlukEtiketTablosu();
        }

        public BolumVerisi BolumOku(int sira) =>
            BolumYukleyici.JsondanOku(File.ReadAllText(BolumDosyalari[sira]), Parametreler);
    }
}
