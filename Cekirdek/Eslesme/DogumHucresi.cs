using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Eslesme
{
    /// <summary>Bir eşleşmeden oluşan özel taşın yerleşeceği hücrenin seçimi (GDD 2.7).</summary>
    public static class DogumHucresi
    {
        /// <summary>
        /// Oyuncu hamlesi (GDD 2.7.2): özel taş, eşleşmeyi tamamlayan taşın indiği hücrede doğar.
        /// Düz 3'lüde doğum hücresi seçilmez (2.7.6).
        /// </summary>
        public static Konum? OyuncuHamlesi(EslesmeGrubu grup, Konum cikilan, Konum gidilen)
        {
            if (grup.Sekil.UretilenTur() == null)
            {
                return null;
            }

            if (grup.Hucreler.Contains(gidilen)) return gidilen;
            if (grup.Hucreler.Contains(cikilan)) return cikilan;
            if (grup.UzatmaHucreleri.Contains(gidilen)) return gidilen;
            if (grup.UzatmaHucreleri.Contains(cikilan)) return cikilan;
            return null;
        }

        /// <summary>
        /// Kaskad (GDD 2.7.4, AK-01). Yorum (1. aşama, karar 1): sıralı ölçütler —
        /// (1) en geç oturan taşın hücresi, (2) eşitlikte AK-01 uç yönündeki hücre (A: en alt, B: en üst),
        /// (3) eşitlik sürerse zorluk ağırlıklı rastgele seçim; rastgelelik verilmezse (etkisiz ağırlık)
        /// sol-üstten tarama sırasındaki ilk hücre, yani en soldaki (A14).
        /// Kilitli hücreler atlanır (2.7.5); düz 3'lüde doğum hücresi seçilmez (2.7.6).
        /// </summary>
        public static Konum? Kaskad(
            Tahta tahta,
            EslesmeGrubu grup,
            AkBayraklari bayraklar,
            IRastgele? esitlikRastgelesi = null)
        {
            if (grup.Sekil.UretilenTur() == null)
            {
                return null;
            }

            var adaylar = grup.Hucreler.Where(k => !tahta[k].Kilitli).ToList();
            if (adaylar.Count == 0)
            {
                return null;
            }

            long enGec = adaylar.Max(k => OturmaAdimi(tahta, k));
            adaylar = adaylar.Where(k => OturmaAdimi(tahta, k) == enGec).ToList();

            int ucSatir = bayraklar[Ak.KaskadDogumHucresi] == Varyant.A
                ? adaylar.Max(k => k.Satir)
                : adaylar.Min(k => k.Satir);
            adaylar = adaylar
                .Where(k => k.Satir == ucSatir)
                .OrderBy(k => k, TaramaSirasi.SolUsttenSatirSatir)
                .ToList();

            return esitlikRastgelesi != null ? esitlikRastgelesi.Sec<Konum>(adaylar) : adaylar[0];
        }

        private static long OturmaAdimi(Tahta tahta, Konum konum) => tahta[konum].Tas?.OturmaAdimi ?? 0;
    }
}
