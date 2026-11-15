using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Takviye envanteri (GDD 9.3.4, 9.3.6, 9.3.10): takviye başına adet ve sınırsız süre.</summary>
    public sealed class Envanter
    {
        private readonly ParametreTablosu _p;
        private readonly Dictionary<TakviyeTuru, int> _adet = new Dictionary<TakviyeTuru, int>();
        private readonly Dictionary<TakviyeTuru, DateTime> _sinirsizBitis = new Dictionary<TakviyeTuru, DateTime>();
        private readonly Dictionary<TakviyeTuru, TimeSpan> _duraklatilan = new Dictionary<TakviyeTuru, TimeSpan>();

        public Envanter(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        public int Adet(TakviyeTuru tur) => _adet.TryGetValue(tur, out int adet) ? adet : 0;

        /// <summary>GDD 9.3.10: envanterde takviye başına adet tutulur (parametre: takviye_azami_adet).</summary>
        public void Ekle(TakviyeTuru tur, int adet)
        {
            int azami = _p.TamSayi(MetaParametreAdlari.TakviyeAzamiAdet);
            _adet[tur] = Math.Min(azami, Adet(tur) + Math.Max(0, adet));
        }

        public bool SinirsizAktif(TakviyeTuru tur, DateTime simdi) =>
            !_duraklatilan.ContainsKey(tur) && _sinirsizBitis.TryGetValue(tur, out var bitis) && simdi < bitis;

        /// <summary>GDD 9.3.6: süreler saniye olarak eklenir ve üst üste biner.</summary>
        public void SinirsizEkle(TakviyeTuru tur, int saniye, DateTime simdi)
        {
            if (_duraklatilan.ContainsKey(tur))
            {
                _duraklatilan[tur] += TimeSpan.FromSeconds(saniye);
                return;
            }

            var bas = _sinirsizBitis.TryGetValue(tur, out var bitis) && bitis > simdi ? bitis : simdi;
            _sinirsizBitis[tur] = bas + TimeSpan.FromSeconds(saniye);
        }

        /// <summary>GDD 9.3.6: süre duraklatılabilir; duraklatılmış süre ayrı tutulur.</summary>
        public void SinirsiziDuraklat(TakviyeTuru tur, DateTime simdi)
        {
            if (SinirsizAktif(tur, simdi))
            {
                _duraklatilan[tur] = _sinirsizBitis[tur] - simdi;
            }
        }

        public void SinirsiziSurdur(TakviyeTuru tur, DateTime simdi)
        {
            if (_duraklatilan.TryGetValue(tur, out var kalan))
            {
                _sinirsizBitis[tur] = simdi + kalan;
                _duraklatilan.Remove(tur);
            }
        }

        internal Dictionary<string, object?> DurumuYaz()
        {
            var adet = new Dictionary<string, object?>();
            foreach (var cift in _adet) adet[cift.Key.ToString()] = cift.Value;
            var bitis = new Dictionary<string, object?>();
            foreach (var cift in _sinirsizBitis) bitis[cift.Key.ToString()] = KayitAlani.Zaman(cift.Value);
            var duraklatilan = new Dictionary<string, object?>();
            foreach (var cift in _duraklatilan) duraklatilan[cift.Key.ToString()] = cift.Value.Ticks;
            return new Dictionary<string, object?>
            {
                ["adet"] = adet,
                ["sinirsiz_bitis"] = bitis,
                ["duraklatilan"] = duraklatilan,
            };
        }

        /// <summary>Artık var olmayan bir takviye türü kayıtta kalmışsa atlanır.</summary>
        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _adet.Clear();
            _sinirsizBitis.Clear();
            _duraklatilan.Clear();
            int azami = _p.TamSayi(MetaParametreAdlari.TakviyeAzamiAdet);
            foreach (var cift in KayitAlani.Nesne(d, "adet"))
            {
                if (Enum.TryParse(cift.Key, out TakviyeTuru tur) && Enum.IsDefined(typeof(TakviyeTuru), tur))
                {
                    _adet[tur] = Math.Max(0, Math.Min(azami, KayitAlani.TamSayi(cift.Value, cift.Key)));
                }
            }

            foreach (var cift in KayitAlani.Nesne(d, "sinirsiz_bitis"))
            {
                if (Enum.TryParse(cift.Key, out TakviyeTuru tur) && Enum.IsDefined(typeof(TakviyeTuru), tur))
                {
                    _sinirsizBitis[tur] = KayitAlani.Zaman(cift.Value, cift.Key);
                }
            }

            foreach (var cift in KayitAlani.Nesne(d, "duraklatilan"))
            {
                if (Enum.TryParse(cift.Key, out TakviyeTuru tur) && Enum.IsDefined(typeof(TakviyeTuru), tur))
                {
                    _duraklatilan[tur] = TimeSpan.FromTicks(KayitAlani.UzunSayi(cift.Value, cift.Key));
                }
            }
        }

        public bool Kullanilabilir(TakviyeTuru tur, DateTime simdi) => SinirsizAktif(tur, simdi) || Adet(tur) > 0;

        /// <summary>
        /// Takviyeyi kullanır: adet bir azalır; sınırsız süre aktifken adet düşmez (GDD 9.3.4, 9.3.6).
        /// </summary>
        public bool Kullan(TakviyeTuru tur, DateTime simdi)
        {
            if (SinirsizAktif(tur, simdi))
            {
                return true;
            }

            if (Adet(tur) <= 0)
            {
                return false;
            }

            _adet[tur] = Adet(tur) - 1;
            return true;
        }
    }

    /// <summary>Takviye satın alma (GDD 9.3.3, 9.3.7).</summary>
    public sealed class TakviyeMagazasi
    {
        private readonly ParametreTablosu _p;
        private readonly Cuzdan _cuzdan;
        private readonly Envanter _envanter;

        public TakviyeMagazasi(ParametreTablosu parametreler, Cuzdan cuzdan, Envanter envanter)
        {
            _p = parametreler;
            _cuzdan = cuzdan;
            _envanter = envanter;
        }

        /// <summary>İki fiyat düzeyi vardır: normal ve yüksek; düzeyi deney grubu belirler (GDD 9.3.7).</summary>
        public bool YuksekFiyatDuzeyi { get; set; }

        /// <summary>Takviyenin fiyatı; satılmayan takviyede null (güdümlü taş yalnızca ödül olarak gelir, 9.3.1).</summary>
        public int? Fiyat(TakviyeTuru tur)
        {
            string? parametre = FiyatParametresi(tur);
            if (parametre == null)
            {
                return null;
            }

            double fiyat = _p.Sayi(parametre) * (YuksekFiyatDuzeyi ? _p.Sayi(MetaParametreAdlari.YuksekFiyatCarpani) : 1);
            return (int)Math.Floor(fiyat);
        }

        /// <summary>
        /// Satın alır: para düşer, envantere eklenir, harcama adıyla bir kez kaydedilir (GDD 9.2.5, 9.3.11).
        /// Yetersiz bakiyede hiçbir şey değişmez.
        /// </summary>
        public bool SatinAl(TakviyeTuru tur)
        {
            int? fiyat = Fiyat(tur);
            if (!fiyat.HasValue || !_cuzdan.Harca(fiyat.Value, "takviye:" + tur))
            {
                return false;
            }

            _envanter.Ekle(tur, 1);
            return true;
        }

        private static string? FiyatParametresi(TakviyeTuru tur)
        {
            switch (tur)
            {
                case TakviyeTuru.Cizgi: return MetaParametreAdlari.FiyatCizgi;
                case TakviyeTuru.Bomba: return MetaParametreAdlari.FiyatBomba;
                case TakviyeTuru.RenkKuresi: return MetaParametreAdlari.FiyatRenkKuresi;
                case TakviyeTuru.TekHucre: return MetaParametreAdlari.FiyatTekHucre;
                case TakviyeTuru.Satir: return MetaParametreAdlari.FiyatSatir;
                case TakviyeTuru.Sutun: return MetaParametreAdlari.FiyatSutun;
                case TakviyeTuru.Karistirma: return MetaParametreAdlari.FiyatKaristirma;
                default: return null;
            }
        }
    }

    /// <summary>
    /// İlerleme kapıları (GDD 9.4): her meta özelliğin bir açılış bölümü vardır; kapılar oyuncunun ulaştığı
    /// en yüksek bölüme göre değerlendirilir (9.4.2).
    /// </summary>
    public sealed class IlerlemeKapilari
    {
        private readonly ParametreTablosu _p;
        private readonly Dictionary<string, int> _oneCekilen = new Dictionary<string, int>();

        public IlerlemeKapilari(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        /// <summary>Açılış parametresi verilen özelliğin açıldığı bölüm.</summary>
        public int AcilisBolumu(string acilisParametresi) =>
            _oneCekilen.TryGetValue(acilisParametresi, out int bolum) ? bolum : _p.TamSayi(acilisParametresi);

        /// <summary>Özellik açık mı? Açılış bölümüne ulaşılınca açılır (GDD 9.4.5).</summary>
        public bool Acik(string acilisParametresi, int enYuksekBolum) => enYuksekBolum >= AcilisBolumu(acilisParametresi);

        /// <summary>Bir deney grubu bazı kapıları öne çekebilir (GDD 9.4.1).</summary>
        public void OneCek(string acilisParametresi, int bolum) => _oneCekilen[acilisParametresi] = bolum;

        internal Dictionary<string, object?> DurumuYaz()
        {
            var d = new Dictionary<string, object?>();
            foreach (var cift in _oneCekilen) d[cift.Key] = cift.Value;
            return d;
        }

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _oneCekilen.Clear();
            foreach (var cift in d) _oneCekilen[cift.Key] = KayitAlani.TamSayi(cift.Value, cift.Key);
        }

        public bool TakviyeAcik(TakviyeTuru tur, int enYuksekBolum)
        {
            switch (tur)
            {
                case TakviyeTuru.TekHucre: return Acik(MetaParametreAdlari.AcilisTekHucreAraci, enYuksekBolum);
                case TakviyeTuru.Satir: return Acik(MetaParametreAdlari.AcilisSatirAraci, enYuksekBolum);
                case TakviyeTuru.Sutun: return Acik(MetaParametreAdlari.AcilisSutunAraci, enYuksekBolum);
                case TakviyeTuru.Karistirma: return Acik(MetaParametreAdlari.AcilisKaristirmaAraci, enYuksekBolum);
                default: return Acik(MetaParametreAdlari.AcilisBaslangicTakviyeleri, enYuksekBolum);
            }
        }
    }
}
