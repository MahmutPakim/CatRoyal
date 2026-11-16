using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CatRoyal.Cekirdek.Veri
{
    /// <summary>JSON ayrıştırma hatası.</summary>
    public sealed class JsonHatasi : Exception
    {
        public JsonHatasi(string mesaj) : base(mesaj) { }
    }

    /// <summary>
    /// Küçük JSON ayrıştırıcı. Çekirdeğin dış bağımlılığı olmasın diye elle yazıldı.
    /// Nesne → Dictionary&lt;string, object?&gt;, dizi → List&lt;object?&gt;, sayı → double, metin → string,
    /// true/false → bool, null → null.
    /// </summary>
    public static class Json
    {
        public static object? Ayristir(string metin)
        {
            int i = 0;
            object? deger = DegerOku(metin, ref i);
            BosluklariGec(metin, ref i);
            if (i != metin.Length)
            {
                throw new JsonHatasi($"JSON: {i}. karakterde fazladan içerik var.");
            }

            return deger;
        }

        /// <summary>
        /// Değeri JSON metnine çevirir. Nesne → IDictionary&lt;string, object?&gt; (anahtar sırası korunur), dizi →
        /// IEnumerable, sayı → int/long/double, metin → string, bool, null. <see cref="Ayristir"/> ile çift yönlüdür.
        /// </summary>
        public static string Yaz(object? deger)
        {
            var metin = new StringBuilder();
            DegerYaz(metin, deger);
            return metin.ToString();
        }

        private static void DegerYaz(StringBuilder m, object? deger)
        {
            switch (deger)
            {
                case null: m.Append("null"); break;
                case string s: MetinYaz(m, s); break;
                case bool b: m.Append(b ? "true" : "false"); break;
                case int i: m.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: m.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case double d:
                    if (double.IsNaN(d) || double.IsInfinity(d))
                    {
                        throw new JsonHatasi("JSON: sonsuz ya da sayı olmayan değer yazılamaz.");
                    }

                    m.Append(d.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case IDictionary<string, object?> nesne:
                    m.Append('{');
                    bool ilk = true;
                    foreach (var cift in nesne)
                    {
                        if (!ilk) m.Append(',');
                        ilk = false;
                        MetinYaz(m, cift.Key);
                        m.Append(':');
                        DegerYaz(m, cift.Value);
                    }

                    m.Append('}');
                    break;
                case System.Collections.IEnumerable dizi:
                    m.Append('[');
                    bool ilkOge = true;
                    foreach (object? oge in dizi)
                    {
                        if (!ilkOge) m.Append(',');
                        ilkOge = false;
                        DegerYaz(m, oge);
                    }

                    m.Append(']');
                    break;
                default:
                    throw new JsonHatasi($"JSON: '{deger.GetType().Name}' türü yazılamaz.");
            }
        }

        private static void MetinYaz(StringBuilder m, string s)
        {
            m.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': m.Append("\\\""); break;
                    case '\\': m.Append("\\\\"); break;
                    case '\n': m.Append("\\n"); break;
                    case '\r': m.Append("\\r"); break;
                    case '\t': m.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            m.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            m.Append(c);
                        }

                        break;
                }
            }

            m.Append('"');
        }

        private static object? DegerOku(string s, ref int i)
        {
            BosluklariGec(s, ref i);
            if (i >= s.Length)
            {
                throw new JsonHatasi("JSON: beklenmeyen son.");
            }

            char c = s[i];
            if (c == '{') return NesneOku(s, ref i);
            if (c == '[') return DiziOku(s, ref i);
            if (c == '"') return MetinOku(s, ref i);
            if (Eslesir(s, i, "true")) { i += 4; return true; }
            if (Eslesir(s, i, "false")) { i += 5; return false; }
            if (Eslesir(s, i, "null")) { i += 4; return null; }
            return SayiOku(s, ref i);
        }

        private static Dictionary<string, object?> NesneOku(string s, ref int i)
        {
            var nesne = new Dictionary<string, object?>(StringComparer.Ordinal);
            i++;
            BosluklariGec(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return nesne;
            }

            while (true)
            {
                BosluklariGec(s, ref i);
                if (i >= s.Length || s[i] != '"')
                {
                    throw new JsonHatasi($"JSON: {i}. karakterde anahtar bekleniyordu.");
                }

                string anahtar = MetinOku(s, ref i);
                BosluklariGec(s, ref i);
                Bekle(s, ref i, ':');
                if (nesne.ContainsKey(anahtar))
                {
                    throw new JsonHatasi($"JSON: '{anahtar}' anahtarı birden fazla kez geçiyor.");
                }

                nesne.Add(anahtar, DegerOku(s, ref i));
                BosluklariGec(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }

                Bekle(s, ref i, '}');
                return nesne;
            }
        }

        private static List<object?> DiziOku(string s, ref int i)
        {
            var dizi = new List<object?>();
            i++;
            BosluklariGec(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return dizi;
            }

            while (true)
            {
                dizi.Add(DegerOku(s, ref i));
                BosluklariGec(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }

                Bekle(s, ref i, ']');
                return dizi;
            }
        }

        private static string MetinOku(string s, ref int i)
        {
            var metin = new StringBuilder();
            i++;
            while (true)
            {
                if (i >= s.Length)
                {
                    throw new JsonHatasi("JSON: kapanmamış tırnak.");
                }

                char c = s[i++];
                if (c == '"')
                {
                    return metin.ToString();
                }

                if (c != '\\')
                {
                    metin.Append(c);
                    continue;
                }

                if (i >= s.Length)
                {
                    throw new JsonHatasi("JSON: yarım kaçış dizisi.");
                }

                char k = s[i++];
                switch (k)
                {
                    case '"': metin.Append('"'); break;
                    case '\\': metin.Append('\\'); break;
                    case '/': metin.Append('/'); break;
                    case 'b': metin.Append('\b'); break;
                    case 'f': metin.Append('\f'); break;
                    case 'n': metin.Append('\n'); break;
                    case 'r': metin.Append('\r'); break;
                    case 't': metin.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length
                            || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int kod))
                        {
                            throw new JsonHatasi("JSON: geçersiz \\u kaçışı.");
                        }

                        metin.Append((char)kod);
                        i += 4;
                        break;
                    default:
                        throw new JsonHatasi($"JSON: geçersiz kaçış '\\{k}'.");
                }
            }
        }

        private static double SayiOku(string s, ref int i)
        {
            int baslangic = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
            {
                i++;
            }

            if (i == baslangic
                || !double.TryParse(s.Substring(baslangic, i - baslangic), NumberStyles.Float, CultureInfo.InvariantCulture, out double sayi))
            {
                throw new JsonHatasi($"JSON: {baslangic}. karakterde değer bekleniyordu.");
            }

            return sayi;
        }

        private static bool Eslesir(string s, int i, string kelime) =>
            string.CompareOrdinal(s, i, kelime, 0, kelime.Length) == 0;

        private static void Bekle(string s, ref int i, char beklenen)
        {
            if (i >= s.Length || s[i] != beklenen)
            {
                throw new JsonHatasi($"JSON: {i}. karakterde '{beklenen}' bekleniyordu.");
            }

            i++;
        }

        private static void BosluklariGec(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }
        }
    }
}
