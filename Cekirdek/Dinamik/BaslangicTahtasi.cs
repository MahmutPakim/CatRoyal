using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>Başlangıç tahtası üretiminin kaydı (GDD 5.4.6; D09 gevşetilen tahtaları ayrı denetler).</summary>
    public sealed class BaslangicSonucu
    {
        /// <summary>İzinli renk kalmadığı için kare kısıtı en az bir hücrede kaldırıldı (GDD 5.4.6).</summary>
        public bool KareKisitiGevsetildi { get; internal set; }

        /// <summary>İzinli renk kalmadığı için 3'lü kısıtı da en az bir hücrede kaldırıldı (GDD 5.4.6).</summary>
        public bool UcluKisitiGevsetildi { get; internal set; }

        /// <summary>Olası hamle çıkana kadar tahtanın kaç kez üretildiği (GDD 5.4.4).</summary>
        public int UretimSayisi { get; internal set; }

        /// <summary>Üretimden sonra karıştırma gerekti mi (GDD 5.4.4, 5.4.5)?</summary>
        public bool Karistirildi { get; internal set; }
    }

    /// <summary>Başlangıç tahtası (GDD 5.4).</summary>
    public static class BaslangicTahtasi
    {
        /// <summary>
        /// Bölüm verisinde rastgele bırakılan hücreleri renklendirir. Hazır 3'lü (5.4.2) ve hazır kare (5.4.3)
        /// oluşmaz; en az bir olası hamle bulunur (5.4.4).
        /// </summary>
        public static BaslangicSonucu Uret(OyunBaglami baglam, IReadOnlyList<Konum> rastgeleHucreler)
        {
            var sonuc = new BaslangicSonucu();
            int deneme = baglam.Parametreler.TamSayi(ParametreAdlari.YenidenKurmaDenemeSayisi);

            // GDD 5.4.4: olası hamle yoksa tahta yeniden üretilir; o da olmazsa karıştırılır.
            for (int i = 0; i < deneme; i++)
            {
                sonuc.KareKisitiGevsetildi = false;
                sonuc.UcluKisitiGevsetildi = false;
                RenkleriDagit(baglam, rastgeleHucreler, sonuc);
                sonuc.UretimSayisi++;
                if (OlasiHamleBulucu.Var(baglam.Tahta))
                {
                    break;
                }
            }

            var hamleler = OlasiHamleBulucu.Bul(baglam.Tahta);

            // GDD 5.4.5: başlangıçta tek olası hamle bir renk küresi hamlesiyse tahta karıştırılır.
            if (hamleler.Count == 0 || hamleler.All(h => h.RenkKuresiHamlesi))
            {
                Karistirici.Karistir(baglam, new List<Olay>());
                sonuc.Karistirildi = true;
            }

            return sonuc;
        }

        /// <summary>
        /// Verilen hücrelere sırayla renk verir (GDD 5.4.1–5.4.3, 5.4.6). Hücreler sol-üstten satır satır doldurulur;
        /// her karenin son dolan hücresi kontrol edilir.
        /// </summary>
        internal static void RenkleriDagit(OyunBaglami baglam, IReadOnlyList<Konum> hucreler, BaslangicSonucu? sonuc)
        {
            var tahta = baglam.Tahta;
            foreach (var konum in hucreler)
            {
                tahta[konum].Tas = null;
            }

            foreach (var konum in hucreler.OrderBy(k => k, TaramaSirasi.SolUsttenSatirSatir))
            {
                var uclusuz = baglam.Renkler.Where(renk => !UcluYapar(tahta, konum, renk)).ToList();
                var izinli = uclusuz.Where(renk => !KareYapar(tahta, konum, renk)).ToList();

                if (izinli.Count == 0)
                {
                    // GDD 5.4.6: izinli renk kalmazsa önce kare kısıtı, gerekirse 3'lü kısıtı da kaldırılır.
                    izinli = uclusuz;
                    if (sonuc != null) sonuc.KareKisitiGevsetildi = true;
                }

                if (izinli.Count == 0)
                {
                    izinli = baglam.Renkler.ToList();
                    if (sonuc != null) sonuc.UcluKisitiGevsetildi = true;
                }

                var tas = tahta.NormalTasUret(RenkSecici.Sec(baglam, izinli));
                tas.OturmaAdimi = baglam.Saat;
                tahta[konum].Tas = tas;
            }
        }

        /// <summary>GDD 5.4.2: renk, dört yönden herhangi birinde aynı renkte iki komşuyla düz 3'lü oluşturur mu?</summary>
        private static bool UcluYapar(Tahta tahta, Konum konum, int renk)
        {
            foreach (var adim in new[] { (0, 1), (1, 0) })
            {
                foreach (var cift in new[] { (-2, -1), (-1, 1), (1, 2) })
                {
                    if (tahta.EslesmeRengi(konum.Otele(adim.Item1 * cift.Item1, adim.Item2 * cift.Item1)) == renk
                        && tahta.EslesmeRengi(konum.Otele(adim.Item1 * cift.Item2, adim.Item2 * cift.Item2)) == renk)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>GDD 5.4.3: renk, hücreyi içeren bir 2×2 kareyi tamamlar mı?</summary>
        private static bool KareYapar(Tahta tahta, Konum konum, int renk)
        {
            foreach (int ds in new[] { -1, 1 })
            {
                foreach (int dt in new[] { -1, 1 })
                {
                    if (tahta.EslesmeRengi(konum.Otele(ds, 0)) == renk
                        && tahta.EslesmeRengi(konum.Otele(0, dt)) == renk
                        && tahta.EslesmeRengi(konum.Otele(ds, dt)) == renk)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
