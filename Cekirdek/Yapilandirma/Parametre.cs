using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CatRoyal.Cekirdek.Yapilandirma
{
    /// <summary>parametreler.csv dosyasındaki "tur" sütunu.</summary>
    public enum ParametreTuru
    {
        Mekanik,
        Denge,
    }

    /// <summary>
    /// parametreler.csv dosyasının tek satırı (GDD 0.4). Değer ham metin olarak saklanır;
    /// türlü okuma <see cref="ParametreTablosu"/> üzerinden yapılır.
    /// </summary>
    public sealed class Parametre
    {
        private static readonly Regex AralikDeseni =
            new Regex(@"^\s*(-?[\d.]+)\s*–\s*(-?[\d.]+)\s*$", RegexOptions.CultureInvariant);

        public Parametre(
            string ad,
            ParametreTuru tur,
            string aciklama,
            string deger,
            string aralik,
            string birim,
            string guvenNotu,
            string gddBolumu)
        {
            Ad = ad;
            Tur = tur;
            Aciklama = aciklama;
            Deger = deger;
            Aralik = aralik;
            Birim = birim;
            GuvenNotu = guvenNotu;
            GddBolumu = gddBolumu;
        }

        public string Ad { get; }
        public ParametreTuru Tur { get; }
        public string Aciklama { get; }

        /// <summary>"öneri_varsayılan" sütunu, ham metin.</summary>
        public string Deger { get; }

        /// <summary>"aralık" sütunu, ham metin.</summary>
        public string Aralik { get; }

        public string Birim { get; }
        public string GuvenNotu { get; }
        public string GddBolumu { get; }

        /// <summary>Değer tek bir sayı olarak okunabiliyor mu?</summary>
        public bool Sayisal => SayiDene(Deger, out _);

        /// <summary>
        /// Aralık sütunu "alt–üst" ya da tek sayı biçimindeyse sınırları verir.
        /// Serbest metin aralıklarda (ör. "her eleman ±0.5") false döner.
        /// </summary>
        public bool AralikDene(out double alt, out double ust)
        {
            var eslesme = AralikDeseni.Match(Aralik);
            if (eslesme.Success
                && SayiDene(eslesme.Groups[1].Value, out alt)
                && SayiDene(eslesme.Groups[2].Value, out ust))
            {
                return true;
            }

            if (SayiDene(Aralik, out alt))
            {
                ust = alt;
                return true;
            }

            alt = ust = 0;
            return false;
        }

        /// <summary>Aynı satırın değeri değiştirilmiş kopyası (test ve ayar denemeleri için).</summary>
        public Parametre DegerIle(string yeniDeger) =>
            new Parametre(Ad, Tur, Aciklama, yeniDeger, Aralik, Birim, GuvenNotu, GddBolumu);

        internal static bool SayiDene(string metin, out double sayi) =>
            double.TryParse(
                metin.Trim(),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out sayi);

        internal static ParametreTuru TurCoz(string metin, string ad)
        {
            switch (metin.Trim())
            {
                case "mekanik": return ParametreTuru.Mekanik;
                case "denge": return ParametreTuru.Denge;
                default:
                    throw new ParametreHatasi($"'{ad}' parametresinin türü tanınmıyor: '{metin}'.");
            }
        }
    }

    /// <summary>Parametre dosyası ya da parametre erişimi hatası.</summary>
    public sealed class ParametreHatasi : Exception
    {
        public ParametreHatasi(string mesaj) : base(mesaj) { }
    }

    /// <summary><see cref="ParametreTablosu.Dogrula"/> bulgusu.</summary>
    public sealed class ParametreSorunu
    {
        public ParametreSorunu(string ad, string mesaj)
        {
            Ad = ad;
            Mesaj = mesaj;
        }

        public string Ad { get; }
        public string Mesaj { get; }

        public override string ToString() => $"{Ad}: {Mesaj}";
    }

    internal static class CsvOkuyucu
    {
        /// <summary>RFC 4180 uyumlu ayrıştırma: tırnaklı alanlar, çift tırnak kaçışı, alan içi satır sonu.</summary>
        public static List<List<string>> Ayristir(string metin)
        {
            var satirlar = new List<List<string>>();
            var satir = new List<string>();
            var alan = new System.Text.StringBuilder();
            bool tirnakIcinde = false;
            bool alanBasladi = false;

            for (int i = 0; i < metin.Length; i++)
            {
                char c = metin[i];
                if (tirnakIcinde)
                {
                    if (c == '"')
                    {
                        if (i + 1 < metin.Length && metin[i + 1] == '"')
                        {
                            alan.Append('"');
                            i++;
                        }
                        else
                        {
                            tirnakIcinde = false;
                        }
                    }
                    else
                    {
                        alan.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        tirnakIcinde = true;
                        alanBasladi = true;
                        break;
                    case ',':
                        satir.Add(alan.ToString());
                        alan.Clear();
                        alanBasladi = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        if (alanBasladi || alan.Length > 0 || satir.Count > 0)
                        {
                            satir.Add(alan.ToString());
                            satirlar.Add(satir);
                        }

                        satir = new List<string>();
                        alan.Clear();
                        alanBasladi = false;
                        break;
                    default:
                        alan.Append(c);
                        break;
                }
            }

            if (tirnakIcinde)
            {
                throw new ParametreHatasi("CSV: kapanmamış tırnak.");
            }

            if (alanBasladi || alan.Length > 0 || satir.Count > 0)
            {
                satir.Add(alan.ToString());
                satirlar.Add(satir);
            }

            return satirlar;
        }
    }
}
