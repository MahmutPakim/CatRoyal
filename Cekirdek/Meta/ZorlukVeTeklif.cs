using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Oyuncu becerisi düzeyleri (GDD 9.11.3).</summary>
    public enum BeceriDuzeyi
    {
        Becerisiz,
        Baslangic,
        Orta,
        Usta,
    }

    /// <summary>Yarı dinamik zorluk (GDD 9.11).</summary>
    public sealed class YariDinamikZorluk
    {
        private readonly ParametreTablosu _p;
        private readonly Dictionary<int, int> _denemeler = new Dictionary<int, int>();

        public YariDinamikZorluk(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        public BeceriDuzeyi Beceri { get; set; } = BeceriDuzeyi.Orta;

        /// <summary>Bölümün deneme sayacı: art arda başarısızlık sayısı (GDD 9.11.1).</summary>
        public int DenemeSayaci(int bolum) => _denemeler.TryGetValue(bolum, out int sayac) ? sayac : 0;

        /// <summary>Beceri ölçümünde sayılan toplam deneme (GDD 9.11.3): ölçüm bölümlerindeki kazanım ve başarısızlıklar.</summary>
        public int OlcumDenemesi { get; private set; }

        /// <summary>Beceri ölçümünde kazanılan bölüm sayısı.</summary>
        public int OlcumBolumu { get; private set; }

        /// <summary>Ölçülen bölüm başına ortalama deneme; henüz ölçüm bölümü kazanılmadıysa null.</summary>
        public double? OrtalamaDeneme => OlcumBolumu > 0 ? (double)OlcumDenemesi / OlcumBolumu : (double?)null;

        /// <summary>Her başarısızlıkta bir artar ve bir üst sınırda durur (parametre: deneme_sayaci_azami).</summary>
        /// <param name="bolum">Bölüm numarası.</param>
        /// <param name="ilkOynayis">Oyuncu bu bölümü ilk kez mi geçmeye çalışıyor? Yalnızca bunlar beceri ölçümüne girer.</param>
        public void BasarisizlikKaydet(int bolum, bool ilkOynayis = false)
        {
            _denemeler[bolum] = Math.Min(_p.TamSayi(MetaParametreAdlari.DenemeSayaciAzami), DenemeSayaci(bolum) + 1);
            if (Olculur(bolum, ilkOynayis))
            {
                OlcumDenemesi++;
            }
        }

        /// <summary>Bölüm kazanılınca deneme sayacı sıfırlanır; ölçüm bölümüyse beceri yeniden hesaplanır.</summary>
        public void KazanimKaydet(int bolum, bool ilkOynayis = false)
        {
            _denemeler.Remove(bolum);
            if (Olculur(bolum, ilkOynayis))
            {
                OlcumDenemesi++;
                OlcumBolumu++;
                Beceri = BeceriHesapla(OrtalamaDeneme!.Value);
            }
        }

        /// <summary>GDD 9.11.3: beceri ilk bölümlerde ölçülür (parametre: beceri_olcum_son_bolum).</summary>
        private bool Olculur(int bolum, bool ilkOynayis) =>
            ilkOynayis && bolum <= _p.TamSayi(MetaParametreAdlari.BeceriOlcumSonBolum);

        /// <summary>
        /// Bölüm başına ortalama deneme sayısından beceri düzeyi (GDD 9.11.3, 11.2.20).
        /// parametre: beceri_esikleri serbest metindir (ada özel ayrıştırıcı):
        /// "≤1.2 usta; ≤1.8 orta; ≤3.0 başlangıç; üstü becerisiz". Eşikler sırayla denenir; ilk uyan düzey seçilir.
        /// </summary>
        public BeceriDuzeyi BeceriHesapla(double ortalamaDeneme)
        {
            string metin = _p.Metin(MetaParametreAdlari.BeceriEsikleri);
            foreach (string parca in metin.Split(';'))
            {
                string[] kelimeler = parca.Trim().Split(new[] { ' ' }, 2);
                if (kelimeler.Length != 2)
                {
                    throw new ParametreHatasi($"'{MetaParametreAdlari.BeceriEsikleri}' beklenen biçimde değil: '{parca.Trim()}'.");
                }

                var duzey = DuzeyCoz(kelimeler[1].Trim());
                if (kelimeler[0] == "üstü")
                {
                    return duzey;
                }

                if (!kelimeler[0].StartsWith("≤", StringComparison.Ordinal)
                    || !double.TryParse(kelimeler[0].Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double esik))
                {
                    throw new ParametreHatasi($"'{MetaParametreAdlari.BeceriEsikleri}': '{kelimeler[0]}' bir eşik değil.");
                }

                if (ortalamaDeneme <= esik)
                {
                    return duzey;
                }
            }

            throw new ParametreHatasi($"'{MetaParametreAdlari.BeceriEsikleri}' son düzeyi ('üstü …') vermiyor.");
        }

        private static BeceriDuzeyi DuzeyCoz(string ad)
        {
            switch (ad)
            {
                case "usta": return BeceriDuzeyi.Usta;
                case "orta": return BeceriDuzeyi.Orta;
                case "başlangıç": return BeceriDuzeyi.Baslangic;
                case "becerisiz": return BeceriDuzeyi.Becerisiz;
                default: throw new ParametreHatasi($"'{MetaParametreAdlari.BeceriEsikleri}': bilinmeyen düzey '{ad}'.");
            }
        }

        internal Dictionary<string, object?> DurumuYaz()
        {
            var denemeler = new Dictionary<string, object?>();
            foreach (var cift in _denemeler.OrderBy(c => c.Key))
            {
                denemeler[cift.Key.ToString(CultureInfo.InvariantCulture)] = cift.Value;
            }

            return new Dictionary<string, object?>
            {
                ["beceri"] = Beceri.ToString(),
                ["olcum_denemesi"] = OlcumDenemesi,
                ["olcum_bolumu"] = OlcumBolumu,
                ["denemeler"] = denemeler,
            };
        }

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            if (d.TryGetValue("beceri", out object? beceri)
                && Enum.TryParse(beceri as string, out BeceriDuzeyi duzey)
                && Enum.IsDefined(typeof(BeceriDuzeyi), duzey))
            {
                Beceri = duzey;
            }

            OlcumBolumu = Math.Max(0, KayitAlani.TamSayi(d, "olcum_bolumu", 0));
            OlcumDenemesi = Math.Max(OlcumBolumu, KayitAlani.TamSayi(d, "olcum_denemesi", 0));
            _denemeler.Clear();
            int azami = _p.TamSayi(MetaParametreAdlari.DenemeSayaciAzami);
            foreach (var cift in KayitAlani.Nesne(d, "denemeler"))
            {
                if (int.TryParse(cift.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int bolum))
                {
                    int sayac = Math.Max(0, Math.Min(azami, KayitAlani.TamSayi(cift.Value, cift.Key)));
                    if (sayac > 0) _denemeler[bolum] = sayac;
                }
            }
        }

        /// <summary>
        /// GDD 9.11.5: zorluk ağırlıklı üretecin ağırlığı; bölümün zorluk etiketi ve oyuncu becerisi belirler.
        /// parametre: zorluk_rastgelelik_agirligi serbest metindir (ada özel ayrıştırıcı):
        /// "normal 0; zor 0.2; çok zor 0.35; aşırı zor 0.5; becerisiz oyuncuda 0.1 eksik".
        /// </summary>
        public double RastgelelikAgirligi(ZorlukEtiketi etiket)
        {
            string metin = _p.Metin(MetaParametreAdlari.ZorlukRastgelelikAgirligi);
            var degerler = new Dictionary<string, double>();
            foreach (string parca in metin.Split(';'))
            {
                var kelimeler = parca.Trim().Split(' ');
                int sayiYeri = Array.FindIndex(
                    kelimeler,
                    k => double.TryParse(k, NumberStyles.Float, CultureInfo.InvariantCulture, out _));
                if (sayiYeri < 1)
                {
                    throw new ParametreHatasi($"'{MetaParametreAdlari.ZorlukRastgelelikAgirligi}' beklenen biçimde değil: '{parca.Trim()}'.");
                }

                degerler[string.Join(" ", kelimeler.Take(sayiYeri))] =
                    double.Parse(kelimeler[sayiYeri], CultureInfo.InvariantCulture);
            }

            string anahtar = etiket == ZorlukEtiketi.Zor ? "zor"
                : etiket == ZorlukEtiketi.CokZor ? "çok zor"
                : etiket == ZorlukEtiketi.AsiriZor ? "aşırı zor"
                : "normal";
            double agirlik = degerler.TryGetValue(anahtar, out double deger) ? deger : 0;
            if (Beceri == BeceriDuzeyi.Becerisiz && degerler.TryGetValue("becerisiz oyuncuda", out double eksik))
            {
                agirlik -= eksik;
            }

            return Math.Max(0, Math.Min(1, agirlik));
        }
    }

    /// <summary>Ek hamle teklifi (GDD 7.4.2, 9.12).</summary>
    public sealed class EkHamleTeklifi
    {
        private readonly ParametreTablosu _p;

        public EkHamleTeklifi(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        /// <summary>Aynı bölümde art arda yapılan alım sayısı; kazanılınca ya da bırakılınca sıfırlanır (GDD 9.12.2).</summary>
        public int AlimSayisi { get; private set; }

        public void AlimKaydet() => AlimSayisi++;

        public void Sifirla() => AlimSayisi = 0;

        /// <summary>
        /// Sıradaki alımın fiyatı (GDD 9.12.2): aynı bölümde art arda alımlarda fiyat basamaklanır; yüksek fiyat
        /// düzeyinde parametre: yuksek_fiyat_carpani uygulanır (9.3.7). Son kademeden sonra fiyat sabit kalır.
        /// </summary>
        public int Fiyat(bool yuksekFiyatDuzeyi)
        {
            var kademeler = _p.TamSayiListesi(MetaParametreAdlari.EkHamleFiyatKademeleri);
            double fiyat = kademeler[Math.Min(AlimSayisi, kademeler.Count - 1)];
            if (yuksekFiyatDuzeyi)
            {
                fiyat *= _p.Sayi(MetaParametreAdlari.YuksekFiyatCarpani);
            }

            return (int)Math.Floor(fiyat);
        }

        /// <summary>
        /// Teklifin verdiği hamle (GDD 9.12.1, 9.11.4): varsayılan parametre: ek_hamle_sayisi; aynı bölümde art arda
        /// başarısızlıklardan sonra daha fazla. Yorum: başarısızlık sayısı eşiğe ulaşınca, eşikten sınıra kadar her
        /// başarısızlık için parametre: ek_hamle_artis_miktari eklenir.
        /// </summary>
        public int Hamle(int artArdaBasarisizlik)
        {
            int esik = _p.TamSayi(MetaParametreAdlari.EkHamleArtisEsigi);
            int sinir = _p.TamSayi(MetaParametreAdlari.EkHamleArtisSiniri);
            int kademe = Math.Max(0, Math.Min(artArdaBasarisizlik, sinir) - esik + 1);
            return _p.TamSayi(MetaParametreAdlari.EkHamleSayisi) + (kademe * _p.TamSayi(MetaParametreAdlari.EkHamleArtisMiktari));
        }

        /// <summary>
        /// Üst kademelerde ek hamleyle birlikte verilen özel taşlar (GDD 9.12.3); tahtaya başlangıç takviyesi gibi konur.
        /// parametre: ek_hamle_paket_icerikleri serbest metindir (ada özel ayrıştırıcı):
        /// "1. kademe: yok; 2.: 1 bomba; 3.: 1 güdümlü ve 1 bomba; …".
        /// </summary>
        public IReadOnlyList<TasTuru> PaketTaslari()
        {
            var kademeler = _p.Metin(MetaParametreAdlari.EkHamlePaketIcerikleri).Split(';');
            string kademe = kademeler[Math.Min(AlimSayisi, kademeler.Length - 1)];
            string icerik = kademe.Substring(kademe.IndexOf(':') + 1).Trim();
            var taslar = new List<TasTuru>();
            if (icerik == "yok")
            {
                return taslar;
            }

            foreach (string oge in icerik.Split(new[] { " ve " }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parcalar = oge.Trim().Split(new[] { ' ' }, 2);
                if (parcalar.Length != 2 || !int.TryParse(parcalar[0], out int adet))
                {
                    throw new ParametreHatasi($"'{MetaParametreAdlari.EkHamlePaketIcerikleri}': '{oge.Trim()}' okunamadı.");
                }

                TasTuru tur;
                switch (parcalar[1].Trim())
                {
                    case "bomba": tur = TasTuru.Bomba; break;
                    case "güdümlü": tur = TasTuru.Gudumlu; break;
                    case "renk küresi": tur = TasTuru.RenkKuresi; break;
                    case "çizgi": tur = TasTuru.Cizgi; break;
                    default: throw new ParametreHatasi($"'{MetaParametreAdlari.EkHamlePaketIcerikleri}': bilinmeyen taş '{parcalar[1]}'.");
                }

                taslar.AddRange(Enumerable.Repeat(tur, adet));
            }

            return taslar;
        }

        /// <summary>
        /// GDD 9.12.5: belirli bir bölüme kadar, oyuncu teklifi reddederse kaybetmeden önce ikinci bir uyarı gösterilir.
        /// </summary>
        public bool IkinciUyariGosterilir(int bolum) => bolum <= _p.TamSayi(MetaParametreAdlari.IkinciUyariSonBolum);
    }
}
