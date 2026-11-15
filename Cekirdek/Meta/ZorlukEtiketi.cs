using System;
using System.Collections.Generic;
using System.Globalization;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Bölümün zorluk etiketi (GDD 8.8.1).</summary>
    public enum ZorlukEtiketi
    {
        Normal,
        Zor,
        CokZor,
        AsiriZor,
    }

    /// <summary>
    /// Zorluk etiketi tablosu (GDD 8.8.1a): etiket bölüm dosyasından ayrı tutulur; istemcideki varsayılan tablo
    /// bölüm paketiyle gelir ve sunucu değiştirebilir. Tabloda olmayan bölüm normal sayılır (9.8.4).
    /// </summary>
    public sealed class ZorlukEtiketTablosu
    {
        private readonly Dictionary<int, ZorlukEtiketi> _etiketler = new Dictionary<int, ZorlukEtiketi>();

        public static ZorlukEtiketTablosu JsondanOku(string json)
        {
            var tablo = new ZorlukEtiketTablosu();
            if (!(Json.Ayristir(json) is Dictionary<string, object?> kok))
            {
                throw new BolumVerisiHatasi("Zorluk etiketi tablosunun kökü bir nesne olmalı.");
            }

            foreach (var cift in kok)
            {
                if (!int.TryParse(cift.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int bolum))
                {
                    throw new BolumVerisiHatasi($"Zorluk etiketi tablosu: '{cift.Key}' bir bölüm numarası değil.");
                }

                tablo._etiketler[bolum] = Coz(cift.Value as string);
            }

            return tablo;
        }

        public ZorlukEtiketi Etiket(int bolum) =>
            _etiketler.TryGetValue(bolum, out var etiket) ? etiket : ZorlukEtiketi.Normal;

        /// <summary>Sunucu etiketi değiştirebilir (GDD 8.8.1a).</summary>
        public void Degistir(int bolum, ZorlukEtiketi etiket) => _etiketler[bolum] = etiket;

        private static ZorlukEtiketi Coz(string? metin)
        {
            switch (metin)
            {
                case "normal": return ZorlukEtiketi.Normal;
                case "zor": return ZorlukEtiketi.Zor;
                case "cok_zor": return ZorlukEtiketi.CokZor;
                case "asiri_zor": return ZorlukEtiketi.AsiriZor;
                default: throw new BolumVerisiHatasi($"Bilinmeyen zorluk etiketi: '{metin}' (GDD 8.8.1).");
            }
        }
    }

    /// <summary>Etikete bağlı ödüller: kazanma parası (GDD 9.2.2) ve hat puanı (9.8.4).</summary>
    public static class EtiketOdulleri
    {
        /// <summary>
        /// GDD 9.2.2, 7.6.1: toplam = etikete göre taban + kalan her hamle için parametre: kalan_hamle_basina_para.
        /// Kesirli toplam aşağı yuvarlanır.
        /// </summary>
        public static int KazanmaParasi(ParametreTablosu p, ZorlukEtiketi etiket, int kalanHamle)
        {
            string taban = etiket == ZorlukEtiketi.Zor ? MetaParametreAdlari.KazanmaParasiZor
                : etiket == ZorlukEtiketi.CokZor ? MetaParametreAdlari.KazanmaParasiCokZor
                : etiket == ZorlukEtiketi.AsiriZor ? MetaParametreAdlari.KazanmaParasiAsiriZor
                : MetaParametreAdlari.KazanmaParasiNormal;
            return (int)Math.Floor(p.Sayi(taban) + (p.Sayi(MetaParametreAdlari.KalanHamleBasinaPara) * kalanHamle));
        }

        /// <summary>
        /// GDD 9.8.4: hat puanı tabanı, bölümün zorluk etiketine göre sabit tablodan gelir. Sunucunun işaretlediği
        /// özel durumlu bölümlerde parametre: hat_puani_ozel kullanılır (11.2.16).
        /// </summary>
        public static int HatPuani(ParametreTablosu p, ZorlukEtiketi etiket, bool ozelDurum = false)
        {
            if (ozelDurum)
            {
                return p.TamSayi(MetaParametreAdlari.HatPuaniOzel);
            }

            string ad = etiket == ZorlukEtiketi.Zor ? MetaParametreAdlari.HatPuaniZor
                : etiket == ZorlukEtiketi.CokZor ? MetaParametreAdlari.HatPuaniCokZor
                : etiket == ZorlukEtiketi.AsiriZor ? MetaParametreAdlari.HatPuaniAsiriZor
                : MetaParametreAdlari.HatPuaniNormal;
            return p.TamSayi(ad);
        }
    }
}
