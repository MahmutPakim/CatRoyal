using System.Collections.Generic;
using CatRoyal.Cekirdek.Olaylar;

namespace CatRoyal.Cekirdek.Model
{
    /// <summary>Hücre katmanı arketipleri (GDD 6.2.2).</summary>
    public enum KatmanArketipi
    {
        Zemin,
        YayilanZemin,
        YapiskanKaplama,
        Kilit,
        Ortu,
        SertKaplama,
    }

    /// <summary>
    /// Hücre katmanının tanımı (GDD 6.4, "yalnız katmanlarda" alanları). Katman taşa değil hücreye bağlıdır;
    /// taşın altında (zemin) ya da üstünde (kaplama, kilit, örtü) bulunur (6.1.6).
    /// </summary>
    public sealed class KatmanTanimi
    {
        public KatmanTanimi(string ad, KatmanArketipi arketip, bool ustte)
        {
            Ad = ad;
            Arketip = arketip;
            Ustte = ustte;
        }

        public string Ad { get; }
        public KatmanArketipi Arketip { get; }

        /// <summary>True: taşın üstünde; false: taşın altında (zemin).</summary>
        public bool Ustte { get; }

        /// <summary>GDD 6.2.2 "Kendi hücresindeki eşleşmeden hasar".</summary>
        public bool KendiHucresindekiEslesmedenHasar { get; set; } = true;

        /// <summary>GDD 6.2.2 "Yan eşleşme hasarı".</summary>
        public bool YanEslesmeHasari { get; set; }

        /// <summary>GDD 6.2.2 "Düşmeyi engeller".</summary>
        public bool DusmeyiEngeller { get; set; }

        /// <summary>GDD 6.2.2 "Kaydırmayı ve dokunmayı engeller".</summary>
        public bool KaydirmayiEngeller { get; set; }

        /// <summary>GDD 6.2.2 "Güdümlü hedefini engeller" (3.4.7).</summary>
        public bool GudumluHedefiniEngeller { get; set; }

        /// <summary>GDD 6.2.2 "Görüşü engeller" (yalnızca sunum).</summary>
        public bool GorusuEngeller { get; set; }

        /// <summary>Hücre doğum hücresi olamaz (GDD 2.7.5; kilit arketipi).</summary>
        public bool DogumHucresiniEngeller { get; set; }

        /// <summary>
        /// Yayılan zemin (GDD 6.3.1): eşleşmeler boyunca hücreden hücreye yayılır. Yorum (1. aşama, karar 2):
        /// vuruşla katman kaybetmez; vuruş yalnızca yayılmayı tetikler.
        /// </summary>
        public bool Yayilir { get; set; }

        /// <summary>GDD 6.4 "Özel taş vuruşu bağışıklıkları"; boş liste varsayılandır.</summary>
        public ISet<VurusTuru> Bagisikliklar { get; } = new HashSet<VurusTuru>();

        /// <summary>Azami katmanı veren parametrenin adı (katman_azami_*).</summary>
        public string? AzamiKatmanParametresi { get; set; }

        public int? AzamiKatman { get; set; }

        /// <summary>GDD 6.4 "Hedef türü": hedef sayacı kimliği ya da null (7.1).</summary>
        public string? HedefTuru { get; set; }

        public override string ToString() => Ad;
    }

    /// <summary>Bir hücredeki katman ve kalan katman sayısı (GDD 6.1.1).</summary>
    public sealed class HucreKatmani
    {
        public HucreKatmani(KatmanTanimi tanim, int katman)
        {
            if (katman < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(katman), "Katman sayısı en az 1 olmalı.");
            }

            Tanim = tanim;
            Katman = katman;
        }

        public KatmanTanimi Tanim { get; }
        public int Katman { get; internal set; }
    }

    /// <summary>GDD 6.2.2 arketiplerinin varsayılan bayrakları.</summary>
    public static class KatmanArketipleri
    {
        /// <summary>Hücrede patlama olunca katman kaybeder; sık bir hedef türü.</summary>
        public static KatmanTanimi Zemin() => new KatmanTanimi("Zemin katmanı", KatmanArketipi.Zemin, ustte: false)
        {
            YanEslesmeHasari = true,
            AzamiKatmanParametresi = "katman_azami_zemin",
            HedefTuru = "zemin",
        };

        /// <summary>Eşleşmeler boyunca hücreden hücreye yayılır (GDD 6.3.1).</summary>
        public static KatmanTanimi YayilanZemin() => new KatmanTanimi("Yayılan zemin", KatmanArketipi.YayilanZemin, ustte: false)
        {
            YanEslesmeHasari = true,
            Yayilir = true,
            AzamiKatman = 1,
        };

        /// <summary>Taşı kaplar; kaplı taş kaydırılamaz. Yandaki eşleşme kaplamayı kaldırır.</summary>
        public static KatmanTanimi YapiskanKaplama() =>
            new KatmanTanimi("Yapışkan kaplama", KatmanArketipi.YapiskanKaplama, ustte: true)
            {
                YanEslesmeHasari = true,
                KaydirmayiEngeller = true,
                GorusuEngeller = true,
                AzamiKatman = 1,
            };

        /// <summary>
        /// Taşı yerine bağlar: kaydırılamaz ve düşmez. Yalnızca kendi hücresindeki eşleşme ya da özel taş vuruşu kırar.
        /// </summary>
        public static KatmanTanimi Kilit() => new KatmanTanimi("Kilit", KatmanArketipi.Kilit, ustte: true)
        {
            YanEslesmeHasari = false,
            DusmeyiEngeller = true,
            KaydirmayiEngeller = true,
            DogumHucresiniEngeller = true,
            AzamiKatmanParametresi = "katman_azami_kilit",
        };

        /// <summary>Altındaki taşları gizler; vurulunca altındakini açığa çıkarır.</summary>
        public static KatmanTanimi Ortu() => new KatmanTanimi("Örtü", KatmanArketipi.Ortu, ustte: true)
        {
            YanEslesmeHasari = true,
            DusmeyiEngeller = true,
            KaydirmayiEngeller = true,
            GudumluHedefiniEngeller = true,
            GorusuEngeller = true,
            AzamiKatmanParametresi = "katman_azami_ortu",
        };

        /// <summary>Yandaki eşleşmeden etkilenmez; yalnızca kendi hücresindeki patlamayla katman kaybeder.</summary>
        public static KatmanTanimi SertKaplama() => new KatmanTanimi("Sert kaplama", KatmanArketipi.SertKaplama, ustte: true)
        {
            YanEslesmeHasari = false,
            KaydirmayiEngeller = true,
            GorusuEngeller = true,
            AzamiKatmanParametresi = "katman_azami_sert_kaplama",
        };
    }
}
