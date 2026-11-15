using System;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    public enum CanDurumu
    {
        Dolu,
        Eksik,
        Bos,
        Sinirsiz,
    }

    /// <summary>
    /// Can (GDD 9.1). Can sayısı ayrıca saklanmaz: saklanan değer "canların tamamen dolacağı an"dır; mevcut can
    /// bu andan ve dolum süresinden hesaplanır (9.1.5). Uygulama kapalıyken geçen süre de böylece sayılır.
    /// </summary>
    public sealed class CanSistemi
    {
        private readonly ParametreTablosu _p;
        private DateTime _sinirsizBitis = DateTime.MinValue;
        private TimeSpan? _duraklatilanSinirsiz;

        public CanSistemi(ParametreTablosu parametreler, DateTime simdi)
        {
            _p = parametreler;
            TamDolacagiAn = simdi;
        }

        /// <summary>Sezon ödül hattında premium hat sahibinin azami canı daha yüksektir (GDD 9.1.1, 9.8.9).</summary>
        public bool Premium { get; private set; }

        public int Azami => _p.TamSayi(Premium ? MetaParametreAdlari.AzamiCanPremium : MetaParametreAdlari.AzamiCan);

        public TimeSpan DolumSuresi => TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.CanDolumSuresiDk));

        /// <summary>Saklanan tek değer: canların tamamen dolacağı an (GDD 9.1.5).</summary>
        public DateTime TamDolacagiAn { get; private set; }

        public bool SinirsizAktif(DateTime simdi) => !_duraklatilanSinirsiz.HasValue && simdi < _sinirsizBitis;

        public TimeSpan SinirsizKalan(DateTime simdi) =>
            _duraklatilanSinirsiz ?? (simdi < _sinirsizBitis ? _sinirsizBitis - simdi : TimeSpan.Zero);

        public int Mevcut(DateTime simdi)
        {
            SaatiDuzelt(simdi);
            if (TamDolacagiAn <= simdi)
            {
                return Azami;
            }

            long eksik = ((TamDolacagiAn - simdi).Ticks + DolumSuresi.Ticks - 1) / DolumSuresi.Ticks;
            return (int)Math.Max(0, Azami - eksik);
        }

        public CanDurumu Durum(DateTime simdi)
        {
            if (SinirsizAktif(simdi)) return CanDurumu.Sinirsiz;
            int can = Mevcut(simdi);
            return can >= Azami ? CanDurumu.Dolu : can == 0 ? CanDurumu.Bos : CanDurumu.Eksik;
        }

        /// <summary>Bir sonraki cana kalan süre (GDD 9.1.4); can tamsa null.</summary>
        public TimeSpan? SonrakiCanaKalan(DateTime simdi)
        {
            if (Mevcut(simdi) >= Azami)
            {
                return null;
            }

            long kalan = (TamDolacagiAn - simdi).Ticks % DolumSuresi.Ticks;
            return kalan == 0 ? DolumSuresi : TimeSpan.FromTicks(kalan);
        }

        /// <summary>GDD 9.1.2: bölüme girmek için en az bir can gerekir.</summary>
        public bool BolumeGirebilir(DateTime simdi) => SinirsizAktif(simdi) || Mevcut(simdi) > 0;

        /// <summary>GDD 9.1.3: kaybedilen bölüm bir can düşürür. Sınırsız can aktifken can düşmez (9.1.7).</summary>
        public void BolumKaybi(DateTime simdi)
        {
            if (SinirsizAktif(simdi) || Mevcut(simdi) == 0)
            {
                return;
            }

            TamDolacagiAn = (TamDolacagiAn > simdi ? TamDolacagiAn : simdi) + DolumSuresi;
        }

        /// <summary>Hediye ya da reklam canı (GDD 9.1.6, 9.1.8); azami can aşılmaz.</summary>
        public void CanEkle(int adet, DateTime simdi)
        {
            Mevcut(simdi);
            var yeni = TamDolacagiAn - TimeSpan.FromTicks(DolumSuresi.Ticks * adet);
            TamDolacagiAn = yeni < simdi ? simdi : yeni;
        }

        /// <summary>Can dolum teklifi: canlar tamamen dolar (GDD 9.1.6).</summary>
        public void TamDoldur(DateTime simdi) => TamDolacagiAn = simdi;

        /// <summary>Mevcut canı doğrudan ayarlar (kayıt yükleme, sunucu eşitleme; GDD 9.1.10).</summary>
        public void CanAyarla(int can, DateTime simdi)
        {
            can = Math.Max(0, Math.Min(Azami, can));
            TamDolacagiAn = simdi + TimeSpan.FromTicks(DolumSuresi.Ticks * (Azami - can));
        }

        /// <summary>Premium hat alındığında ya da bittiğinde azami can değişir; mevcut can korunur.</summary>
        public void PremiumAyarla(bool premium, DateTime simdi)
        {
            int can = Mevcut(simdi);
            Premium = premium;
            CanAyarla(can, simdi);
        }

        /// <summary>
        /// Sınırsız can (GDD 9.1.7): süreler üst üste toplanır. Süre bitince canlar doludur (durum tablosu).
        /// </summary>
        public void SinirsizEkle(TimeSpan sure, DateTime simdi)
        {
            if (_duraklatilanSinirsiz.HasValue)
            {
                _duraklatilanSinirsiz += sure;
                return;
            }

            _sinirsizBitis = (_sinirsizBitis > simdi ? _sinirsizBitis : simdi) + sure;
            TamDolacagiAn = simdi;
        }

        /// <summary>Sınırsız can süresi duraklatılabilir; duraklatılmış süre ayrı tutulur (GDD 9.1.7).</summary>
        public void SinirsiziDuraklat(DateTime simdi)
        {
            if (!_duraklatilanSinirsiz.HasValue && simdi < _sinirsizBitis)
            {
                _duraklatilanSinirsiz = _sinirsizBitis - simdi;
            }
        }

        public void SinirsiziSurdur(DateTime simdi)
        {
            if (_duraklatilanSinirsiz.HasValue)
            {
                _sinirsizBitis = simdi + _duraklatilanSinirsiz.Value;
                _duraklatilanSinirsiz = null;
            }
        }

        /// <summary>
        /// GDD 9.1.5: cihaz saati geri alınırsa değer düzeltilir. Dolmasına, bütün canların dolum süresinden daha
        /// uzun bir süre kalmış görünemez.
        /// </summary>
        private void SaatiDuzelt(DateTime simdi)
        {
            var enGec = simdi + TimeSpan.FromTicks(DolumSuresi.Ticks * Azami);
            if (TamDolacagiAn > enGec)
            {
                TamDolacagiAn = enGec;
            }
        }
    }
}
