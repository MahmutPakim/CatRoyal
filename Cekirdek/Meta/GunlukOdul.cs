using System;
using System.Globalization;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Günlük ödül (GDD 9.5). Gün sınırı sunucu saatine göre sabit bir saattedir; gün sayısı, sunucu zamanından
    /// bu saat çıkarılıp bir güne bölünerek bulunur (9.5.4). Ödül içerikleri sunucudan gelir (9.5.9).
    /// </summary>
    public sealed class GunlukOdul
    {
        private readonly ParametreTablosu _p;
        private readonly IlerlemeKapilari _kapilar;

        public GunlukOdul(ParametreTablosu parametreler, IlerlemeKapilari kapilar)
        {
            _p = parametreler;
            _kapilar = kapilar;
        }

        /// <summary>Son ödülün alındığı gün numarası; hiç alınmadıysa null.</summary>
        public long? SonAlinanGun { get; private set; }

        /// <summary>Haftalık ve aylık seri ayrı ayrı sayılır (GDD 9.5.5).</summary>
        public int HaftalikSeri { get; private set; }

        public int AylikSeri { get; private set; }

        /// <summary>Haftalık ödül setleri sırayla döner (GDD 9.5.6).</summary>
        public int SetSirasi { get; private set; }

        /// <summary>Sunucu özelliği tamamen kapatabilir (GDD 9.5.9).</summary>
        public bool SunucuKapatti { get; set; }

        /// <summary>Sunucu saat hilesi korumasını kapatabilir (GDD 9.5.8).</summary>
        public bool HileKorumasiAcik { get; set; } = true;

        public long GunNumarasi(DateTime sunucuZamani) =>
            (long)Math.Floor((sunucuZamani - DateTime.MinValue - GunSiniri()).TotalDays);

        private TimeSpan GunSiniri()
        {
            string metin = _p.Metin(MetaParametreAdlari.GunSiniriSaati);
            if (!TimeSpan.TryParseExact(metin, @"hh\:mm", CultureInfo.InvariantCulture, out var sinir))
            {
                throw new ParametreHatasi($"'{MetaParametreAdlari.GunSiniriSaati}' saat:dakika biçiminde olmalı: '{metin}'.");
            }

            return sinir;
        }

        public bool Acik(int enYuksekBolum) =>
            !SunucuKapatti && _kapilar.Acik(MetaParametreAdlari.AcilisGunlukOdul, enYuksekBolum);

        public bool Alinabilir(DateTime sunucuZamani, int enYuksekBolum) =>
            Acik(enYuksekBolum) && (!SonAlinanGun.HasValue || GunNumarasi(sunucuZamani) > SonAlinanGun.Value);

        /// <summary>Bir sonraki sıfırlamaya kalan süre (GDD 9.5.3).</summary>
        public TimeSpan SifirlamayaKalan(DateTime sunucuZamani)
        {
            var sonrakiGun = DateTime.MinValue + GunSiniri() + TimeSpan.FromDays(GunNumarasi(sunucuZamani) + 1);
            return sonrakiGun - sunucuZamani;
        }

        /// <summary>
        /// Ödülü alır: seri bir artar (GDD 9.5.2). Bir gün kaçırıldıysa haftalık seri sıfırlanır, aylık seri devam
        /// eder (9.5.7). Hafta tamamlanınca bir sonraki ödül setine geçilir (9.5.6).
        /// </summary>
        public bool Al(DateTime sunucuZamani, int enYuksekBolum)
        {
            if (!Alinabilir(sunucuZamani, enYuksekBolum))
            {
                return false;
            }

            long gun = GunNumarasi(sunucuZamani);
            if (SonAlinanGun.HasValue && gun - SonAlinanGun.Value > 1)
            {
                HaftalikSeri = 0;
            }

            int haftaUzunlugu = _p.TamSayi(MetaParametreAdlari.HaftalikSeriUzunlugu);
            if (HaftalikSeri >= haftaUzunlugu)
            {
                HaftalikSeri = 0;
                SetSirasi = (SetSirasi + 1) % _p.TamSayi(MetaParametreAdlari.HaftalikSetSayisi);
            }

            int ayUzunlugu = _p.TamSayi(MetaParametreAdlari.AylikSeriUzunlugu);
            if (AylikSeri >= ayUzunlugu)
            {
                AylikSeri = 0;
            }

            HaftalikSeri++;
            AylikSeri++;
            SonAlinanGun = gun;
            return true;
        }

        /// <summary>
        /// Saat hilesi koruması (GDD 9.5.8): son alınan gün sunucu gününden ilerideyse hile kabul edilir; ilerleme
        /// düzeltilir. Bir tolerans vardır (parametre: hile_toleransi_dk).
        /// </summary>
        /// <returns>Hile tespit edildiyse true; sunum uyarı gösterir.</returns>
        public bool SaatHilesiniDenetle(DateTime sunucuZamani)
        {
            if (!HileKorumasiAcik || !SonAlinanGun.HasValue)
            {
                return false;
            }

            var tolerans = TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.HileToleransiDk));
            if (SonAlinanGun.Value <= GunNumarasi(sunucuZamani + tolerans))
            {
                return false;
            }

            SonAlinanGun = GunNumarasi(sunucuZamani);
            return true;
        }
    }
}
