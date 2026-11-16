using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Olaylar
{
    /// <summary>Bir hücre katmanı katman kaybetti ya da kırıldı (GDD 6.1.1, 6.1.6).</summary>
    public sealed class KatmanHasarAldi : AdimliOlay
    {
        public KatmanHasarAldi(Konum konum, KatmanTanimi tanim, int kalanKatman, long adim) : base(adim)
        {
            Konum = konum;
            Tanim = tanim;
            KalanKatman = kalanKatman;
        }

        public Konum Konum { get; }
        public KatmanTanimi Tanim { get; }
        public int KalanKatman { get; }
        public bool Kirildi => KalanKatman == 0;
    }

    /// <summary>Yayılan zemin bir hücreye yayıldı (GDD 6.3.1). Sıra, zeminli hücreye uzaklık sırasıdır.</summary>
    public sealed class ZeminYayildi : AdimliOlay
    {
        public ZeminYayildi(Konum konum, KatmanTanimi tanim, long adim) : base(adim)
        {
            Konum = konum;
            Tanim = tanim;
        }

        public Konum Konum { get; }
        public KatmanTanimi Tanim { get; }
    }

    /// <summary>Kırılan bir üretici komşu hücreye katman bıraktı (GDD 6.3.2).</summary>
    public sealed class KatmanBirakildi : Olay
    {
        public KatmanBirakildi(Konum uretici, Konum konum, KatmanTanimi tanim)
        {
            Uretici = uretici;
            Konum = konum;
            Tanim = tanim;
        }

        public Konum Uretici { get; }
        public Konum Konum { get; }
        public KatmanTanimi Tanim { get; }
    }

    /// <summary>Patlayan bir yayılan tehlike komşu hücreye yeni tehlike fırlattı (GDD 6.3.3).</summary>
    public sealed class TehlikeYayildi : Olay
    {
        public TehlikeYayildi(Konum nereden, Konum nereye, Tas tehlike)
        {
            Nereden = nereden;
            Nereye = nereye;
            Tehlike = tehlike;
        }

        public Konum Nereden { get; }
        public Konum Nereye { get; }
        public Tas Tehlike { get; }
    }

    /// <summary>Aşağı inen toplanabilir toplama hücresine indi ve toplandı (GDD 6.3.5).</summary>
    public sealed class OgeToplandi : Olay
    {
        public OgeToplandi(Konum konum, Tas oge)
        {
            Konum = konum;
            Oge = oge;
        }

        public Konum Konum { get; }
        public Tas Oge { get; }
    }

    /// <summary>Bir hedef sayacı düştü (GDD 7.1.3). Sunum öğeyi hedef simgesine uçurur (7.1.5).</summary>
    public sealed class HedefIlerledi : Olay
    {
        public HedefIlerledi(string tur, int kalan)
        {
            Tur = tur;
            Kalan = kalan;
        }

        public string Tur { get; }
        public int Kalan { get; }
    }

    /// <summary>Bütün hedefler tamamlandı; kazanma başladı (GDD 7.3.1). Tahtadaki patlama ve düşmeler tamamlanır (7.3.2).</summary>
    public sealed class KazanmaBasladi : Olay
    {
    }

    /// <summary>Hamle sayacı değişti (GDD 7.2.1).</summary>
    public sealed class HamleSayaciDegisti : Olay
    {
        public HamleSayaciDegisti(int kalanHamle, int yapilanHamle)
        {
            KalanHamle = kalanHamle;
            YapilanHamle = yapilanHamle;
        }

        public int KalanHamle { get; }
        public int YapilanHamle { get; }
    }

    /// <summary>Yedek hamle hakkı kullanıldı ve oyuna ek hamle gönderildi (GDD 3.8.2).</summary>
    public sealed class YedekHamleKullanildi : Olay
    {
        public YedekHamleKullanildi(int eklenenHamle)
        {
            EklenenHamle = eklenenHamle;
        }

        public int EklenenHamle { get; }
    }

    /// <summary>Hamleler bitti ve ek hamle teklifi açıldı (GDD 7.4, AK-02). Fiyat meta katmandadır (9.12).</summary>
    public sealed class EkHamleTeklifiAcildi : Olay
    {
        public EkHamleTeklifiAcildi(int teklifEdilenHamle)
        {
            TeklifEdilenHamle = teklifEdilenHamle;
        }

        public int TeklifEdilenHamle { get; }
    }

    /// <summary>Bölüm kazanıldı. Kalan hamleler artan hamle ödülüne gider (GDD 7.3.3, 7.6).</summary>
    public sealed class BolumKazanildi : Olay
    {
        public BolumKazanildi(int kalanHamle)
        {
            KalanHamle = kalanHamle;
        }

        public int KalanHamle { get; }
    }

    /// <summary>Bölüm kaybedildi (GDD 7.4.3, AK-13 B). Can düşmesi meta katmandadır (7.4.4, 9.1.3).</summary>
    public sealed class BolumKaybedildi : Olay
    {
        public BolumKaybedildi(bool teklifSunulmadan)
        {
            TeklifSunulmadan = teklifSunulmadan;
        }

        /// <summary>AK-13 B: "olası hamle yok" akışı bölümü ek hamle teklifi sunmadan kaybettirdi.</summary>
        public bool TeklifSunulmadan { get; }
    }

    /// <summary>AK-13 A: yeniden üretim denemeleri de başarısız; bölüm can düşürülmeden baştan başlatılmalı.</summary>
    public sealed class BolumBastanBaslatilmali : Olay
    {
    }

    /// <summary>Dolum göstergesi doldu ya da sıfırlandı (GDD 3.7.2, 3.7.6).</summary>
    public sealed class DolumGostergesiDegisti : Olay
    {
        public DolumGostergesiDegisti(double deger, double kapasite)
        {
            Deger = deger;
            Kapasite = kapasite;
        }

        public double Deger { get; }
        public double Kapasite { get; }
    }

    /// <summary>
    /// Dolum göstergesi doldu ve tahtaya bir renk küresi fırlatıldı (GDD 3.7.3). Fırlatma sürerken sunum
    /// dokunmayı kilitler (3.7.5).
    /// </summary>
    public sealed class RenkKuresiFirlatildi : Olay
    {
        public RenkKuresiFirlatildi(Konum konum, Tas kure)
        {
            Konum = konum;
            Kure = kure;
        }

        public Konum Konum { get; }
        public Tas Kure { get; }
    }

    /// <summary>Oyun içi araçlar (GDD 3.9.4).</summary>
    public enum AracTuru
    {
        TekHucre,
        Satir,
        Sutun,
        Karistirma,
    }

    /// <summary>Bir oyun içi araç kullanıldı (GDD 3.9.4); hamle harcamaz (3.9.5).</summary>
    public sealed class AracKullanildi : Olay
    {
        public AracKullanildi(AracTuru tur, Konum? konum)
        {
            Tur = tur;
            Konum = konum;
        }

        public AracTuru Tur { get; }
        public Konum? Konum { get; }
    }

    /// <summary>Tahtaya bir takviye taşı kondu (GDD 3.9.1).</summary>
    public sealed class TakviyeKondu : Olay
    {
        public TakviyeKondu(Konum konum, Tas tas)
        {
            Konum = konum;
            Tas = tas;
        }

        public Konum Konum { get; }
        public Tas Tas { get; }
    }

    /// <summary>
    /// Çok ekranlı bölümde bir sonraki ekrana geçildi (GDD 8.9.1). Bundan sonraki olayların konumları yeni
    /// ekranın tahtasına göredir; satır 1 o ekranın en üst satırıdır.
    /// </summary>
    public sealed class EkranDegisti : Olay
    {
        public EkranDegisti(int ekran, int aktarilanTas)
        {
            Ekran = ekran;
            AktarilanTas = aktarilanTas;
        }

        public int Ekran { get; }

        /// <summary>Üst ekrandan alt ekranın giriş sırasına eklenen taş sayısı (GDD 8.9.2).</summary>
        public int AktarilanTas { get; }
    }

    /// <summary>Artan hamle gösterisi başladı (GDD 7.6.2): kalan hamle kadar normal taş çizgi taşına dönüşecek.</summary>
    public sealed class ArtanHamleGosterisiBasladi : Olay
    {
        public ArtanHamleGosterisiBasladi(int kalanHamle) => KalanHamle = kalanHamle;

        public int KalanHamle { get; }
    }

    /// <summary>Gösteri bitti ya da atlandı (GDD 7.6.3); kalan hamleyi puan olarak kullanan olaylar bunu dinler (10.1.6).</summary>
    public sealed class ArtanHamlelerBitti : Olay
    {
        public ArtanHamlelerBitti(int kalanHamle, bool atlandi)
        {
            KalanHamle = kalanHamle;
            Atlandi = atlandi;
        }

        public int KalanHamle { get; }
        public bool Atlandi { get; }
    }
}
