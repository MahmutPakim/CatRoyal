using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Gerçek parayla satılan bir ürün (GDD 9.14). Fiyat uygulama mağazasından gelir ve çekirdekte tutulmaz;
    /// satın alım doğrudan ödül paketine gider, ayrı bir sert para birimi yoktur (9.14.3).
    /// </summary>
    public sealed class MagazaUrunu
    {
        public MagazaUrunu(string kimlik, OdulPaketi paket)
        {
            Kimlik = kimlik;
            Paket = paket;
        }

        /// <summary>Uygulama mağazasındaki ürün kimliği.</summary>
        public string Kimlik { get; }

        public OdulPaketi Paket { get; }
    }

    /// <summary>
    /// Para mağazası (GDD 9.14.1): belirli bir bölümden sonra açılır ve sabit sayıda ürün gösterir. Ürünler ve
    /// fiyatlar sunucudan gelir; çekirdek yalnız kapıyı, gösterilen ürün sayısını ve ödülün verilmesini bilir.
    /// </summary>
    public sealed class ParaMagazasi
    {
        private readonly ParametreTablosu _p;
        private readonly IlerlemeKapilari _kapilar;
        private List<MagazaUrunu> _urunler = new List<MagazaUrunu>();

        public ParaMagazasi(ParametreTablosu parametreler, IlerlemeKapilari kapilar)
        {
            _p = parametreler;
            _kapilar = kapilar;
        }

        public bool Acik(int enYuksekBolum) => _kapilar.Acik(MetaParametreAdlari.AcilisMagaza, enYuksekBolum);

        /// <summary>Sunucudan gelen ürün listesi, gösterim sırasıyla.</summary>
        public void UrunleriAyarla(IEnumerable<MagazaUrunu> urunler) => _urunler = urunler.ToList();

        /// <summary>Mağaza açıksa ilk parametre: magaza_urun_sayisi ürün; kapalıysa boş liste.</summary>
        public IReadOnlyList<MagazaUrunu> GorunenUrunler(int enYuksekBolum) =>
            Acik(enYuksekBolum)
                ? _urunler.Take(_p.TamSayi(MetaParametreAdlari.MagazaUrunSayisi)).ToList()
                : new List<MagazaUrunu>();

        /// <summary>Ödeme uygulama mağazasında tamamlandı: ürünün ödül paketi oyuncuya verilir (GDD 9.14.3).</summary>
        /// <returns>Ürün gösterilen ürünler arasında değilse false.</returns>
        public bool SatinAlimTamamlandi(Oyuncu oyuncu, string urunKimligi, DateTime simdi)
        {
            var urun = GorunenUrunler(oyuncu.EnYuksekBolum).FirstOrDefault(u => u.Kimlik == urunKimligi);
            if (urun == null)
            {
                return false;
            }

            oyuncu.OdulVer(urun.Paket, simdi);
            return true;
        }
    }

    /// <summary>
    /// Kademeli teklif (GDD 9.14.2): sırayla açılan satın alma basamakları. Bir basamak alınmadan sonraki açılmaz.
    /// Basamakların içeriği sunucudan gelir.
    /// </summary>
    public sealed class KademeliTeklif
    {
        private readonly IlerlemeKapilari _kapilar;
        private readonly List<MagazaUrunu> _basamaklar;

        public KademeliTeklif(IlerlemeKapilari kapilar, IEnumerable<MagazaUrunu> basamaklar)
        {
            _kapilar = kapilar;
            _basamaklar = basamaklar.ToList();
        }

        public int BasamakSayisi => _basamaklar.Count;

        /// <summary>Alınan basamak sayısı; aynı zamanda açık basamağın 0 tabanlı sırası.</summary>
        public int AlinanBasamak { get; private set; }

        public bool Bitti => AlinanBasamak >= _basamaklar.Count;

        public bool Acik(int enYuksekBolum) =>
            !Bitti && _kapilar.Acik(MetaParametreAdlari.AcilisKademeliTeklif, enYuksekBolum);

        /// <summary>O an satın alınabilen tek basamak; teklif kapalıysa ya da bittiyse null.</summary>
        public MagazaUrunu? AcikBasamak(int enYuksekBolum) => Acik(enYuksekBolum) ? _basamaklar[AlinanBasamak] : null;

        /// <summary>Açık basamağın ödemesi tamamlandı: paketi verilir ve sıradaki basamak açılır.</summary>
        public bool SatinAlimTamamlandi(Oyuncu oyuncu, DateTime simdi)
        {
            var basamak = AcikBasamak(oyuncu.EnYuksekBolum);
            if (basamak == null)
            {
                return false;
            }

            oyuncu.OdulVer(basamak.Paket, simdi);
            AlinanBasamak++;
            return true;
        }
    }

    /// <summary>
    /// Dönemsel teklif (GDD 9.14.2): belirli bir zaman aralığında geçerli tek bir ürün. Para mağazasıyla aynı
    /// kapıdan açılır (parametre: acilis_magaza). Aralık ve içerik sunucudan gelir.
    /// </summary>
    public sealed class DonemselTeklif
    {
        private readonly IlerlemeKapilari _kapilar;

        public DonemselTeklif(IlerlemeKapilari kapilar, MagazaUrunu urun, DateTime baslangic, DateTime bitis)
        {
            _kapilar = kapilar;
            Urun = urun;
            Baslangic = baslangic;
            Bitis = bitis;
        }

        public MagazaUrunu Urun { get; }
        public DateTime Baslangic { get; }
        public DateTime Bitis { get; }
        public bool Alindi { get; private set; }

        public bool Gecerli(int enYuksekBolum, DateTime simdi) =>
            !Alindi
            && simdi >= Baslangic
            && simdi < Bitis
            && _kapilar.Acik(MetaParametreAdlari.AcilisMagaza, enYuksekBolum);

        public bool SatinAlimTamamlandi(Oyuncu oyuncu, DateTime simdi)
        {
            if (!Gecerli(oyuncu.EnYuksekBolum, simdi))
            {
                return false;
            }

            oyuncu.OdulVer(Urun.Paket, simdi);
            Alindi = true;
            return true;
        }
    }

    /// <summary>
    /// Geri dönen oyuncu hediyesi (GDD 9.15.1): bir süre oynamayan oyuncuya dönüşte paket verilir. Uzak kalma
    /// süresi ve paket parametre dosyasında yoktur; ikisi de dışarıdan (sunucu) verilir. Yalnız açılış kapısı
    /// parametredir (acilis_karsilama_hediyesi).
    /// </summary>
    public sealed class GeriDonusHediyesi
    {
        private readonly IlerlemeKapilari _kapilar;

        public GeriDonusHediyesi(IlerlemeKapilari kapilar, TimeSpan uzakKalmaSuresi, OdulPaketi paket, DateTime ilkGiris)
        {
            if (uzakKalmaSuresi <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(uzakKalmaSuresi), "Uzak kalma süresi sıfırdan büyük olmalı.");
            }

            _kapilar = kapilar;
            UzakKalmaSuresi = uzakKalmaSuresi;
            Paket = paket;
            SonGiris = ilkGiris;
        }

        public TimeSpan UzakKalmaSuresi { get; }
        public OdulPaketi Paket { get; }

        /// <summary>Oyuncunun oyuna son girdiği an.</summary>
        public DateTime SonGiris { get; private set; }

        /// <summary>
        /// Oyuncu oyuna girdi. Son girişten bu yana uzak kalma süresi dolduysa ve kapı açıksa hediye verilir.
        /// Cihaz saati geri alındıysa hediye verilmez; son giriş yine güncellenir.
        /// </summary>
        /// <returns>Hediye bu girişte verildiyse true.</returns>
        public bool OyunaGirildi(Oyuncu oyuncu, DateTime simdi)
        {
            bool verilir = simdi - SonGiris >= UzakKalmaSuresi
                && _kapilar.Acik(MetaParametreAdlari.AcilisKarsilamaHediyesi, oyuncu.EnYuksekBolum);
            SonGiris = simdi;
            if (verilir)
            {
                oyuncu.OdulVer(Paket, simdi);
            }

            return verilir;
        }
    }
}
