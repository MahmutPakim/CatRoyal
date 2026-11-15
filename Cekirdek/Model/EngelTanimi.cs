using System.Collections.Generic;
using CatRoyal.Cekirdek.Olaylar;

namespace CatRoyal.Cekirdek.Model
{
    /// <summary>Engel arketipleri (GDD 6.2.1).</summary>
    public enum EngelArketipi
    {
        KatmanliBlok,
        SertBlok,
        DusenKirilabilir,
        AsagiInenToplanabilir,
        CokluVurusBlogu,
        KaydirilamayanDusenBlok,
        ParaTasi,
        Uretici,
        YayilanTehlike,
        HamleSonuHareketli,
    }

    /// <summary>GDD 6.4 "Yayılma ya da üretme".</summary>
    public enum EngelYayilmasi
    {
        Yok,

        /// <summary>Üretici: kırılınca çevresine katman bırakır (GDD 6.3.2).</summary>
        KatmanBirakir,

        /// <summary>Yayılan tehlike: patlayınca çevresine yeni tehlike fırlatır (GDD 6.3.3).</summary>
        TehlikeFirlatir,
    }

    /// <summary>
    /// Hamle sonu davranışı (GDD 6.3.4). Kesin kurallar her engel türü için ayrı tasarlanır (11.2.10);
    /// çekirdek yalnızca her oyuncu hamlesi ve kaskadı bitince bu kancayı çağırır.
    /// </summary>
    public interface IHamleSonuDavranisi
    {
        /// <returns>Tahta değiştiyse true; çekirdek ardından tahtayı yeniden durultur.</returns>
        bool Uygula(OyunBaglami baglam, Konum konum, List<Olay> olaylar);
    }

    /// <summary>
    /// Hücreyi dolduran engelin tanımı (GDD 6.4). Kod engel davranışını yalnızca bu alanlardan okur.
    /// </summary>
    public sealed class EngelTanimi
    {
        public EngelTanimi(string ad, EngelArketipi arketip)
        {
            Ad = ad;
            Arketip = arketip;
        }

        public string Ad { get; }
        public EngelArketipi Arketip { get; }

        /// <summary>GDD 6.4 "Düşer".</summary>
        public bool Duser { get; set; }

        /// <summary>GDD 6.4 "Kaydırılır".</summary>
        public bool Kaydirilir { get; set; }

        /// <summary>GDD 6.4 "Çapraz kaymayı engeller" (5.2.5, 6.2.3).</summary>
        public bool CaprazKaymayiEngeller { get; set; }

        /// <summary>GDD 6.1.2, 6.4 "Yan eşleşme hasarı".</summary>
        public bool YanEslesmeHasari { get; set; }

        /// <summary>GDD 6.1.5 — özel taş, kombinasyon ve araç vuruşundan hasar alır mı?</summary>
        public bool OzelTasVurusu { get; set; } = true;

        /// <summary>GDD 6.4 "Özel taş vuruşu bağışıklıkları"; boş liste varsayılandır (11.2.7).</summary>
        public ISet<VurusTuru> Bagisikliklar { get; } = new HashSet<VurusTuru>();

        /// <summary>GDD 6.1.4 "Çoklu vuruş".</summary>
        public bool CokluVurus { get; set; }

        /// <summary>Azami katmanı veren parametrenin adı (katman_azami_*); tanım <see cref="AzamiKatman"/> ile ezebilir.</summary>
        public string? AzamiKatmanParametresi { get; set; }

        /// <summary>GDD 6.4 "Azami katman": arketipin parametresini ezer.</summary>
        public int? AzamiKatman { get; set; }

        /// <summary>GDD 6.4 "Hedef türü": hedef sayacı kimliği ya da null (7.1).</summary>
        public string? HedefTuru { get; set; }

        /// <summary>GDD 3.4.6 "Güdümlü önceliği"; varsayılan hayır.</summary>
        public bool GudumluOnceligi { get; set; }

        /// <summary>GDD 6.4 "Güdümlü hedef puanı"; boşsa gudumlu_puan_engel parametresi kullanılır (3.4.5).</summary>
        public double? GudumluHedefPuani { get; set; }

        /// <summary>GDD 6.4 "Oyun içi araçla seçilebilir"; varsayılan evet (3.9.4).</summary>
        public bool AraclaSecilebilir { get; set; } = true;

        /// <summary>GDD 6.4 "Hamle sonu davranışı" (6.3.4).</summary>
        public IHamleSonuDavranisi? HamleSonuDavranisi { get; set; }

        /// <summary>GDD 6.4 "Yayılma ya da üretme" (6.3).</summary>
        public EngelYayilmasi Yayilma { get; set; }

        /// <summary>Üreticinin kırılınca çevresine bıraktığı katman (GDD 6.3.2).</summary>
        public KatmanTanimi? BirakilanKatman { get; set; }

        /// <summary>GDD 6.4 "Tanıtım": ilk göründüğü bölüm (8.6).</summary>
        public int? TanitimBolumu { get; set; }

        public override string ToString() => Ad;
    }

    /// <summary>GDD 6.2.1 arketiplerinin varsayılan bayrakları.</summary>
    public static class Arketipler
    {
        /// <summary>Düşmez, kaydırılmaz; yandaki eşleşme ve özel taş vuruşuyla katman kaybeder.</summary>
        public static EngelTanimi KatmanliBlok() => new EngelTanimi("Katmanlı blok", EngelArketipi.KatmanliBlok)
        {
            YanEslesmeHasari = true,
            AzamiKatmanParametresi = "katman_azami_katmanli_blok",
        };

        /// <summary>Yandaki eşleşmeden etkilenmez; yalnızca özel taş ve araç vuruşlarıyla katman kaybeder.</summary>
        public static EngelTanimi SertBlok() => new EngelTanimi("Sert blok", EngelArketipi.SertBlok)
        {
            YanEslesmeHasari = false,
            AzamiKatmanParametresi = "katman_azami_sert_blok",
        };

        /// <summary>Normal taş gibi düşer ve kaydırılır ama renk eşleşmesine girmez.</summary>
        public static EngelTanimi DusenKirilabilir() => new EngelTanimi("Düşen kırılabilir", EngelArketipi.DusenKirilabilir)
        {
            Duser = true,
            Kaydirilir = true,
            YanEslesmeHasari = true,
            AzamiKatmanParametresi = "katman_azami_dusen_kirilabilir",
        };

        /// <summary>Vurulmaz; sütunundaki toplama hücresine inince toplanır (GDD 6.3.5).</summary>
        public static EngelTanimi AsagiInenToplanabilir() =>
            new EngelTanimi("Aşağı inen toplanabilir", EngelArketipi.AsagiInenToplanabilir)
            {
                Duser = true,
                Kaydirilir = true,
                YanEslesmeHasari = false,
                OzelTasVurusu = false,
            };

        /// <summary>Aynı olaydan birden fazla vuruş alabilir (GDD 6.1.4).</summary>
        public static EngelTanimi CokluVurusBlogu() => new EngelTanimi("Çoklu vuruş bloğu", EngelArketipi.CokluVurusBlogu)
        {
            YanEslesmeHasari = true,
            CokluVurus = true,
            AzamiKatmanParametresi = "katman_azami_coklu_vurus",
        };

        /// <summary>Düşer ama kaydırılamaz; çapraz kaymayı engeller. Yan eşleşme hasarı koşulludur.</summary>
        public static EngelTanimi KaydirilamayanDusenBlok(bool yanEslesmeHasari) =>
            new EngelTanimi("Kaydırılamayan düşen blok", EngelArketipi.KaydirilamayanDusenBlok)
            {
                Duser = true,
                Kaydirilir = false,
                CaprazKaymayiEngeller = true,
                YanEslesmeHasari = yanEslesmeHasari,
                AzamiKatman = 1,
            };

        /// <summary>Bonus bölümlerinin hedefi (GDD 8.7).</summary>
        public static EngelTanimi ParaTasi() => new EngelTanimi("Para taşı", EngelArketipi.ParaTasi)
        {
            Duser = true,
            Kaydirilir = true,
            YanEslesmeHasari = true,
            AzamiKatman = 1,
            HedefTuru = "para_tasi",
        };

        /// <summary>Kırılınca çevresine kaplama ya da zemin bırakır (GDD 6.3.2).</summary>
        public static EngelTanimi Uretici(KatmanTanimi birakilanKatman) => new EngelTanimi("Üretici", EngelArketipi.Uretici)
        {
            YanEslesmeHasari = true,
            AzamiKatmanParametresi = "katman_azami_uretici",
            Yayilma = EngelYayilmasi.KatmanBirakir,
            BirakilanKatman = birakilanKatman,
        };

        /// <summary>Patlayınca çevresine yayılır (GDD 6.3.3).</summary>
        public static EngelTanimi YayilanTehlike() => new EngelTanimi("Yayılan tehlike", EngelArketipi.YayilanTehlike)
        {
            YanEslesmeHasari = true,
            AzamiKatman = 1,
            Yayilma = EngelYayilmasi.TehlikeFirlatir,
        };

        /// <summary>Her hamlenin sonunda hareket eder ya da durum değiştirir (GDD 6.3.4). Vuruş bayrakları koşulludur.</summary>
        public static EngelTanimi HamleSonuHareketli(
            IHamleSonuDavranisi davranis, bool yanEslesmeHasari, bool ozelTasVurusu) =>
            new EngelTanimi("Hamle sonu hareketli", EngelArketipi.HamleSonuHareketli)
            {
                YanEslesmeHasari = yanEslesmeHasari,
                OzelTasVurusu = ozelTasVurusu,
                HamleSonuDavranisi = davranis,
            };
    }
}
