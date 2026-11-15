using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CatRoyal.Cekirdek.Yapilandirma
{
    /// <summary>
    /// parametreler.csv içeriği (GDD 0.4). Değişmezdir; bütün sayısal ayarlar buradan okunur,
    /// koda sayı gömülmez. Çekirdek "sunum_" ile başlayan parametreleri okumaz.
    /// </summary>
    public sealed class ParametreTablosu
    {
        private static readonly string[] BeklenenBaslik =
        {
            "ad", "tur", "açıklama", "öneri_varsayılan", "aralık", "birim", "güven_notu", "gdd_bölümü",
        };

        private static readonly Regex ParantezDeseni = new Regex(@"\([^)]*\)", RegexOptions.CultureInvariant);

        private readonly Dictionary<string, Parametre> _adaGore;
        private readonly List<Parametre> _sirali;

        private ParametreTablosu(List<Parametre> sirali)
        {
            _sirali = sirali;
            _adaGore = new Dictionary<string, Parametre>(StringComparer.Ordinal);
            foreach (var p in sirali)
            {
                if (_adaGore.ContainsKey(p.Ad))
                {
                    throw new ParametreHatasi($"'{p.Ad}' parametresi dosyada birden fazla kez geçiyor.");
                }

                _adaGore.Add(p.Ad, p);
            }
        }

        /// <summary>Dosya sırasıyla bütün parametreler.</summary>
        public IReadOnlyList<Parametre> Hepsi => _sirali;

        public int Adet => _sirali.Count;

        /// <summary>CSV metnini ayrıştırır. Başlık satırı zorunludur.</summary>
        public static ParametreTablosu CsvdenOku(string csvMetni)
        {
            if (csvMetni.Length > 0 && csvMetni[0] == '﻿')
            {
                csvMetni = csvMetni.Substring(1);
            }

            var satirlar = CsvOkuyucu.Ayristir(csvMetni);
            if (satirlar.Count == 0)
            {
                throw new ParametreHatasi("Parametre dosyası boş.");
            }

            var baslik = satirlar[0].Select(a => a.Trim()).ToArray();
            if (!baslik.SequenceEqual(BeklenenBaslik))
            {
                throw new ParametreHatasi(
                    "Parametre dosyasının başlığı beklenenden farklı: " + string.Join(",", baslik));
            }

            var sirali = new List<Parametre>(satirlar.Count - 1);
            for (int i = 1; i < satirlar.Count; i++)
            {
                var s = satirlar[i];
                if (s.Count != BeklenenBaslik.Length)
                {
                    throw new ParametreHatasi(
                        $"Parametre dosyası satır {i + 1}: {BeklenenBaslik.Length} alan bekleniyordu, {s.Count} bulundu.");
                }

                string ad = s[0].Trim();
                if (ad.Length == 0)
                {
                    throw new ParametreHatasi($"Parametre dosyası satır {i + 1}: ad boş.");
                }

                sirali.Add(new Parametre(
                    ad,
                    Parametre.TurCoz(s[1], ad),
                    s[2].Trim(),
                    s[3].Trim(),
                    s[4].Trim(),
                    s[5].Trim(),
                    s[6].Trim(),
                    s[7].Trim()));
            }

            return new ParametreTablosu(sirali);
        }

        public bool Var(string ad) => _adaGore.ContainsKey(ad);

        public Parametre Al(string ad)
        {
            if (!_adaGore.TryGetValue(ad, out var p))
            {
                throw new ParametreHatasi($"'{ad}' adlı parametre yok.");
            }

            return p;
        }

        /// <summary>Ham değer metni.</summary>
        public string Metin(string ad) => Al(ad).Deger;

        /// <summary>Ondalıklı sayı. Değer sayısal değilse hata verir.</summary>
        public double Sayi(string ad)
        {
            var p = Al(ad);
            if (!Parametre.SayiDene(p.Deger, out double sayi))
            {
                throw new ParametreHatasi($"'{ad}' parametresi sayı değil: '{p.Deger}'.");
            }

            return sayi;
        }

        /// <summary>Tam sayı. Değer kesirliyse hata verir.</summary>
        public int TamSayi(string ad)
        {
            double sayi = Sayi(ad);
            double yuvarlak = Math.Round(sayi);
            if (Math.Abs(sayi - yuvarlak) > 1e-9 || yuvarlak < int.MinValue || yuvarlak > int.MaxValue)
            {
                throw new ParametreHatasi($"'{ad}' parametresi tam sayı değil: '{Al(ad).Deger}'.");
            }

            return (int)yuvarlak;
        }

        /// <summary>
        /// Düz sayı listesi: "800 / 1600 / 2400" ya da "2, 2, 4 (toplam 8)".
        /// Parantez içi açıklamalar atılır; ayırıcı "/" ya da ",".
        /// Serbest metinli değerlerde (ör. sezon_hatti_basamak_hedefleri) hata verir;
        /// onlar için ada özel ayrıştırıcı kullanılır.
        /// </summary>
        public IReadOnlyList<double> SayiListesi(string ad)
        {
            var p = Al(ad);
            string temiz = ParantezDeseni.Replace(p.Deger, string.Empty);
            var parcalar = temiz.Split(new[] { '/', ',' }, StringSplitOptions.None);
            var sonuc = new List<double>(parcalar.Length);
            foreach (var parca in parcalar)
            {
                if (!Parametre.SayiDene(parca, out double sayi))
                {
                    throw new ParametreHatasi($"'{ad}' parametresi düz sayı listesi değil: '{p.Deger}'.");
                }

                sonuc.Add(sayi);
            }

            return sonuc;
        }

        /// <summary>Tam sayı listesi; bkz. <see cref="SayiListesi"/>.</summary>
        public IReadOnlyList<int> TamSayiListesi(string ad)
        {
            var sayilar = SayiListesi(ad);
            var sonuc = new List<int>(sayilar.Count);
            foreach (double sayi in sayilar)
            {
                double yuvarlak = Math.Round(sayi);
                if (Math.Abs(sayi - yuvarlak) > 1e-9)
                {
                    throw new ParametreHatasi($"'{ad}' parametresi tam sayı listesi değil: '{Al(ad).Deger}'.");
                }

                sonuc.Add((int)yuvarlak);
            }

            return sonuc;
        }

        /// <summary>Bir parametrenin değeri değiştirilmiş yeni tablo. Özgün tablo değişmez.</summary>
        public ParametreTablosu Degistir(string ad, string yeniDeger)
        {
            Al(ad);
            return new ParametreTablosu(
                _sirali.Select(p => p.Ad == ad ? p.DegerIle(yeniDeger) : p).ToList());
        }

        /// <summary>Sayısal değer için kısayol; bkz. <see cref="Degistir(string, string)"/>.</summary>
        public ParametreTablosu Degistir(string ad, double yeniDeger) =>
            Degistir(ad, yeniDeger.ToString("R", CultureInfo.InvariantCulture));

        /// <summary>
        /// Sayısal değerleri, aralık sütunu makinece okunabilen satırlarda aralığa karşı denetler.
        /// </summary>
        public IReadOnlyList<ParametreSorunu> Dogrula()
        {
            var sorunlar = new List<ParametreSorunu>();
            foreach (var p in _sirali)
            {
                if (!Parametre.SayiDene(p.Deger, out double deger))
                {
                    continue;
                }

                if (p.AralikDene(out double alt, out double ust) && (deger < alt || deger > ust))
                {
                    sorunlar.Add(new ParametreSorunu(
                        p.Ad, $"değer {p.Deger} aralığın dışında ({p.Aralik})."));
                }
            }

            return sorunlar;
        }
    }
}
