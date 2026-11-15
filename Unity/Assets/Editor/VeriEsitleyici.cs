using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatRoyal.Sunum.EditorAraclari
{
    /// <summary>
    /// Depo kökündeki tek kaynak dosyalarını (docs/parametreler.csv, veri/) StreamingAssets altına kopyalar.
    /// Parametreler ve bölümler koda ya da Unity varlıklarına gömülmez; oyun bu kopyaları çalışma anında okur.
    /// Editör açıldığında ve Play'e basılmadan önce kendiliğinden çalışır.
    /// </summary>
    [InitializeOnLoad]
    public static class VeriEsitleyici
    {
        private const string HedefKlasor = "Assets/StreamingAssets/CatRoyal";

        static VeriEsitleyici()
        {
            Esitle();
            EditorApplication.playModeStateChanged += durum =>
            {
                if (durum == PlayModeStateChange.ExitingEditMode)
                {
                    Esitle();
                }
            };
        }

        [MenuItem("CatRoyal/Veriyi Eşitle")]
        public static void Esitle()
        {
            string depoKoku = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            bool degisti = false;

            degisti |= Kopyala(
                Path.Combine(depoKoku, "docs", "parametreler.csv"),
                Path.Combine(HedefKlasor, "parametreler.csv"));

            string veriKoku = Path.Combine(depoKoku, "veri");
            if (Directory.Exists(veriKoku))
            {
                foreach (string dosya in Directory.GetFiles(veriKoku, "*.json", SearchOption.AllDirectories))
                {
                    string goreli = dosya.Substring(veriKoku.Length + 1);
                    degisti |= Kopyala(dosya, Path.Combine(HedefKlasor, "veri", goreli));
                }
            }

            if (degisti)
            {
                AssetDatabase.Refresh();
            }
        }

        private static bool Kopyala(string kaynak, string hedef)
        {
            if (!File.Exists(kaynak))
            {
                Debug.LogWarning("CatRoyal: kaynak dosya bulunamadı: " + kaynak);
                return false;
            }

            string icerik = File.ReadAllText(kaynak);
            if (File.Exists(hedef) && File.ReadAllText(hedef) == icerik)
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(hedef));
            File.WriteAllText(hedef, icerik);
            return true;
        }
    }
}
