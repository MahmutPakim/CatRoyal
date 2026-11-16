using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Tasarim;

namespace CatRoyal.Cekirdek.Sunucu
{
    /// <summary>Elle ayarlanan saat; sahte sunucu ve testler kullanır.</summary>
    public sealed class AyarliSaat : ISaat
    {
        public AyarliSaat(DateTime simdi)
        {
            SimdiUtc = simdi;
        }

        public DateTime SimdiUtc { get; set; }

        public void Ilerlet(TimeSpan sure) => SimdiUtc += sure;
    }

    /// <summary>
    /// <see cref="ISunucu"/>'nun yerel sahte uygulaması: ağ kullanmaz, her şeyi bellekte tutar. Gerçek sunucu
    /// gelene kadar oyun ve testler bununla çalışır. Davranışı belirlenimlidir: aynı girdiler aynı grubu ve aynı
    /// bot puanlarını verir.
    ///
    /// Botlar sabit bir hızla puan toplar; hız olay ve grup kimliğinden türetilir. Sıralamada eşitlik oyuncunun
    /// lehine bozulur. Bunlar sahte uygulamanın kendi seçimleridir; gerçek sıralama kuralı sunucudadır (GDD 10.1.8).
    /// </summary>
    public sealed class YerelSahteSunucu : ISunucu
    {
        private readonly ISaat _saat;
        private readonly List<OlayTakvimKaydi> _takvim = new List<OlayTakvimKaydi>();
        private readonly Dictionary<int, ZorlukEtiketi> _etiketler = new Dictionary<int, ZorlukEtiketi>();
        private readonly Dictionary<string, Grup> _gruplar = new Dictionary<string, Grup>();
        private readonly Dictionary<int, BolumPaketiVerisi> _paketler = new Dictionary<int, BolumPaketiVerisi>();

        public YerelSahteSunucu(ISaat saat)
        {
            _saat = saat;
        }

        public DateTime SimdiUtc => _saat.SimdiUtc;

        /// <summary>Bağlantı yokmuş gibi davranır: bütün istekler başarısız döner.</summary>
        public bool Cevrimdisi { get; set; }

        /// <summary>Sahte sunucunun yapılandırması; doğrudan değiştirilir.</summary>
        public SunucuYapilandirmasi Ayarlar { get; } = new SunucuYapilandirmasi();

        /// <summary>Botların saatte topladığı en yüksek puan; her botun hızı 1 ile bu değer arasındadır.</summary>
        public int BotAzamiSaatlikPuan { get; set; } = 6;

        public void OlayEkle(OlayTakvimKaydi kayit)
        {
            if (_takvim.Any(o => o.Kimlik == kayit.Kimlik))
            {
                throw new ArgumentException($"'{kayit.Kimlik}' kimlikli olay takvimde zaten var.", nameof(kayit));
            }

            _takvim.Add(kayit);
        }

        public void EtiketDegistir(int bolum, ZorlukEtiketi etiket) => _etiketler[bolum] = etiket;

        /// <summary>Sunucuya bölüm paketi koyar; aynı numaralı paketin yerine geçer.</summary>
        public void PaketYayimla(BolumPaketiVerisi paket) => _paketler[paket.Paket] = paket;

        public SunucuYaniti<IReadOnlyDictionary<int, int>> PaketSurumleri() =>
            Cevrimdisi
                ? SunucuYaniti<IReadOnlyDictionary<int, int>>.Basarisiz()
                : SunucuYaniti<IReadOnlyDictionary<int, int>>.Tamam(_paketler.ToDictionary(c => c.Key, c => c.Value.Surum));

        public SunucuYaniti<BolumPaketiVerisi> PaketIndir(int paket) =>
            !Cevrimdisi && _paketler.TryGetValue(paket, out var veri)
                ? SunucuYaniti<BolumPaketiVerisi>.Tamam(veri)
                : SunucuYaniti<BolumPaketiVerisi>.Basarisiz();

        public SunucuYaniti<SunucuYapilandirmasi> Yapilandirma() =>
            Cevrimdisi ? SunucuYaniti<SunucuYapilandirmasi>.Basarisiz() : SunucuYaniti<SunucuYapilandirmasi>.Tamam(Ayarlar);

        public SunucuYaniti<IReadOnlyDictionary<int, ZorlukEtiketi>> EtiketDegisiklikleri() =>
            Cevrimdisi
                ? SunucuYaniti<IReadOnlyDictionary<int, ZorlukEtiketi>>.Basarisiz()
                : SunucuYaniti<IReadOnlyDictionary<int, ZorlukEtiketi>>.Tamam(new Dictionary<int, ZorlukEtiketi>(_etiketler));

        public SunucuYaniti<IReadOnlyList<OlayTakvimKaydi>> OlayTakvimi() =>
            Cevrimdisi
                ? SunucuYaniti<IReadOnlyList<OlayTakvimKaydi>>.Basarisiz()
                : SunucuYaniti<IReadOnlyList<OlayTakvimKaydi>>.Tamam(_takvim.OrderBy(o => o.Baslangic).ToList());

        public SunucuYaniti<OlayGrubu> GrubaKatil(string olayKimligi, string oyuncuKimligi, int grupBoyutu)
        {
            // "#n" eki aynı olayın n. katılımını belirtir; takvimde olayın kendi kimliği aranır.
            int ek = olayKimligi.IndexOf('#');
            string takvimKimligi = ek < 0 ? olayKimligi : olayKimligi.Substring(0, ek);
            var olay = _takvim.FirstOrDefault(o => o.Kimlik == takvimKimligi);
            if (Cevrimdisi || olay == null || grupBoyutu < 1)
            {
                return SunucuYaniti<OlayGrubu>.Basarisiz();
            }

            string anahtar = Anahtar(olayKimligi, oyuncuKimligi);
            if (!_gruplar.TryGetValue(anahtar, out var grup))
            {
                // Katılım yalnızca olay sürerken olur; var olan gruba dönmek olay bittikten sonra da serbesttir.
                if (!olay.Suruyor(SimdiUtc))
                {
                    return SunucuYaniti<OlayGrubu>.Basarisiz();
                }

                grup = new Grup(olay, olayKimligi, oyuncuKimligi, grupBoyutu, SimdiUtc, Math.Max(1, BotAzamiSaatlikPuan));
                _gruplar.Add(anahtar, grup);
            }

            return SunucuYaniti<OlayGrubu>.Tamam(grup.Liste(SimdiUtc));
        }

        public SunucuYaniti<OlayGrubu> PuanGonder(string olayKimligi, string oyuncuKimligi, int puan)
        {
            if (Cevrimdisi || puan < 0 || !_gruplar.TryGetValue(Anahtar(olayKimligi, oyuncuKimligi), out var grup))
            {
                return SunucuYaniti<OlayGrubu>.Basarisiz();
            }

            // Olay bittikten ya da kapatıldıktan sonra gelen puan sayılmaz.
            if (!grup.Olay.Suruyor(SimdiUtc))
            {
                return SunucuYaniti<OlayGrubu>.Basarisiz();
            }

            grup.OyuncuPuani += puan;
            return SunucuYaniti<OlayGrubu>.Tamam(grup.Liste(SimdiUtc));
        }

        public SunucuYaniti<OlayGrubu> Siralama(string olayKimligi, string oyuncuKimligi) =>
            !Cevrimdisi && _gruplar.TryGetValue(Anahtar(olayKimligi, oyuncuKimligi), out var grup)
                ? SunucuYaniti<OlayGrubu>.Tamam(grup.Liste(SimdiUtc))
                : SunucuYaniti<OlayGrubu>.Basarisiz();

        private static string Anahtar(string olayKimligi, string oyuncuKimligi) => olayKimligi + "\n" + oyuncuKimligi;

        /// <summary>Süreçten sürece değişmeyen özet (string.GetHashCode her çalıştırmada farklıdır).</summary>
        private static uint Ozet(string metin)
        {
            uint ozet = 2166136261;
            foreach (char c in metin)
            {
                ozet = (ozet ^ c) * 16777619;
            }

            return ozet;
        }

        private sealed class Grup
        {
            private readonly string _oyuncu;
            private readonly DateTime _katilim;
            private readonly List<(string Kimlik, int SaatlikPuan)> _botlar = new List<(string, int)>();

            public Grup(OlayTakvimKaydi olay, string grupAdi, string oyuncu, int boyut, DateTime katilim, int botAzamiSaatlikPuan)
            {
                Olay = olay;
                _oyuncu = oyuncu;
                _katilim = katilim;
                Kimlik = grupAdi + ":" + Ozet(grupAdi + "\n" + oyuncu).ToString("x8");
                for (int i = 1; i < boyut; i++)
                {
                    string bot = "bot_" + i;
                    _botlar.Add((bot, 1 + (int)(Ozet(Kimlik + "\n" + bot) % (uint)botAzamiSaatlikPuan)));
                }
            }

            public OlayTakvimKaydi Olay { get; }
            public string Kimlik { get; }
            public int OyuncuPuani { get; set; }

            public OlayGrubu Liste(DateTime simdi)
            {
                // Botlar olay bitince durur.
                var an = simdi < Olay.Bitis ? simdi : Olay.Bitis;
                double saat = Math.Max(0, (an - _katilim).TotalHours);
                var uyeler = _botlar
                    .Select(b => new GrupUyesi(b.Kimlik, (int)Math.Floor(b.SaatlikPuan * saat), true))
                    .ToList();
                uyeler.Add(new GrupUyesi(_oyuncu, OyuncuPuani, false));
                return new OlayGrubu(
                    Kimlik,
                    uyeler
                        .OrderByDescending(u => u.Puan)
                        .ThenBy(u => u.Bot)
                        .ThenBy(u => u.Kimlik, StringComparer.Ordinal)
                        .ToList());
            }
        }
    }
}
