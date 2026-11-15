using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>Karıştırma (GDD 5.6) ve "olası hamle yok" akışı (7.5.1, AK-13).</summary>
    public static class Karistirici
    {
        /// <summary>GDD 5.6.1: tahta durulduğunda olası hamle yoksa karıştırma başlar.</summary>
        public static KaristirmaSonucu GerekirseKaristir(OyunBaglami baglam, List<Olay> olaylar) =>
            OlasiHamleBulucu.Var(baglam.Tahta) ? KaristirmaSonucu.Gerekmedi : Karistir(baglam, olaylar);

        /// <summary>
        /// Karıştırır: önce yer değiştirme denemeleri (5.6.2), sonra renk değiştirme (5.6.6), sonra AK-13 akışı.
        /// Karıştırma hamle harcamaz (5.6.4).
        /// </summary>
        public static KaristirmaSonucu Karistir(OyunBaglami baglam, List<Olay> olaylar)
        {
            var tahta = baglam.Tahta;

            // GDD 5.6.3: yalnızca kaydırılabilir normal taşlar yer değiştirir.
            var hucreler = tahta.TumKonumlar()
                .Where(k => tahta.Kaydirilabilir(k) && tahta[k].Tas!.Tur == TasTuru.Normal)
                .ToList();
            var ozgun = hucreler.Select(k => tahta[k].Tas!).ToList();

            // GDD 5.6.2: hazır eşleşme kalmayana ve en az bir olası hamle çıkana kadar tekrarlanır.
            int deneme = baglam.Parametreler.TamSayi(ParametreAdlari.KaristirmaDenemeSayisi);
            for (int i = 0; i < deneme && hucreler.Count > 1; i++)
            {
                var karisik = ozgun.ToList();
                baglam.Rastgele.Karistir(karisik);
                Yerlestir(tahta, hucreler, karisik);
                if (Gecerli(tahta))
                {
                    return Bildir(olaylar, KaristirmaSonucu.Karistirildi);
                }
            }

            Yerlestir(tahta, hucreler, ozgun);

            // GDD 5.6.6: yer değiştirme başarısız olursa bazı taşların rengi değiştirilerek olası hamle kurulur.
            if (baglam.Bayraklar.KaristirmaRenkDegisimi)
            {
                var kurulan = HamleKur(baglam, hucreler, ozgun);
                if (kurulan.HasValue)
                {
                    return Bildir(olaylar, kurulan.Value);
                }

                Yerlestir(tahta, hucreler, ozgun);
            }

            // AK-13: taşlar eski yerlerine döner ve "olası hamle yok" akışı çalışır.
            olaylar.Add(new OlasiHamleYok());
            if (baglam.Bayraklar[Ak.KaristirmaBasarisizligi] == Varyant.B)
            {
                return Bildir(olaylar, KaristirmaSonucu.Kaybedildi);
            }

            // AK-13 A: bölüm kaybı sayılmadan tahta yeniden üretilir; hamle sayacı korunur.
            int yenidenKurma = baglam.Parametreler.TamSayi(ParametreAdlari.YenidenKurmaDenemeSayisi);
            for (int i = 0; i < yenidenKurma && hucreler.Count > 0 && baglam.Renkler.Count > 0; i++)
            {
                BaslangicTahtasi.RenkleriDagit(baglam, hucreler, null);
                if (Gecerli(tahta))
                {
                    return Bildir(olaylar, KaristirmaSonucu.YenidenKuruldu);
                }
            }

            Yerlestir(tahta, hucreler, ozgun);
            return Bildir(olaylar, KaristirmaSonucu.BastanBaslatilmali);
        }

        private static KaristirmaSonucu Bildir(List<Olay> olaylar, KaristirmaSonucu sonuc)
        {
            olaylar.Add(new TahtaKaristirildi(sonuc));
            return sonuc;
        }

        private static bool Gecerli(Tahta tahta) =>
            !OlasiHamleBulucu.HazirEslesmeVar(tahta) && OlasiHamleBulucu.Var(tahta);

        private static void Yerlestir(Tahta tahta, List<Konum> hucreler, List<Tas> taslar)
        {
            for (int i = 0; i < hucreler.Count; i++)
            {
                tahta[hucreler[i]].Tas = taslar[i];
            }
        }

        /// <summary>
        /// GDD 5.6.6: olası bir hamle kurar. Bir kaydırmayla 3'lü tamamlayacak üç hücre seçilir ve bu hücrelere aynı
        /// renkte taş getirilir. Renk dağılımı mümkün olduğunca korunur: o renkten yeterince taş varsa yalnızca
        /// yerleri değişir; yoksa eksik kalan hücrelerin rengi değiştirilir.
        /// </summary>
        private static KaristirmaSonucu? HamleKur(OyunBaglami baglam, List<Konum> hucreler, List<Tas> ozgun)
        {
            var tahta = baglam.Tahta;
            var serbest = new HashSet<Konum>(hucreler);
            var desenler = Desenler(tahta, serbest);
            baglam.Rastgele.Karistir(desenler);

            var renkler = ozgun
                .GroupBy(t => t.Renk)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
                .ToList();

            foreach (var desen in desenler)
            {
                foreach (int renk in renkler)
                {
                    Yerlestir(tahta, hucreler, ozgun);
                    bool renkDegisti = false;

                    foreach (var hedef in desen)
                    {
                        if (tahta[hedef].Tas!.Renk == renk)
                        {
                            continue;
                        }

                        var verici = hucreler.FirstOrDefault(k => !desen.Contains(k) && tahta[k].Tas!.Renk == renk);
                        if (hucreler.Any(k => !desen.Contains(k) && tahta[k].Tas!.Renk == renk))
                        {
                            var gecici = tahta[hedef].Tas;
                            tahta[hedef].Tas = tahta[verici].Tas;
                            tahta[verici].Tas = gecici;
                        }
                        else
                        {
                            var yeni = tahta.NormalTasUret(renk);
                            yeni.OturmaAdimi = tahta[hedef].Tas!.OturmaAdimi;
                            tahta[hedef].Tas = yeni;
                            renkDegisti = true;
                        }
                    }

                    if (Gecerli(tahta))
                    {
                        return renkDegisti ? KaristirmaSonucu.RenkDegistirildi : KaristirmaSonucu.Karistirildi;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Bir kaydırmayla düz 3'lü kuran hücre üçlüleri: kaydırılan taşın çıktığı hücre ile gittiği hücrenin
        /// aynı sıradaki iki komşusu. Üç hücre de serbest normal taş hücresi olmalıdır.
        /// </summary>
        private static List<Konum[]> Desenler(Tahta tahta, HashSet<Konum> serbest)
        {
            var desenler = new List<Konum[]>();
            foreach (var cikilan in serbest)
            {
                foreach (var gidilen in cikilan.DikKomsular())
                {
                    if (!serbest.Contains(gidilen))
                    {
                        continue;
                    }

                    foreach (var adim in new[] { (0, 1), (1, 0) })
                    {
                        foreach (var cift in new[] { (-2, -1), (-1, 1), (1, 2) })
                        {
                            var a = gidilen.Otele(adim.Item1 * cift.Item1, adim.Item2 * cift.Item1);
                            var b = gidilen.Otele(adim.Item1 * cift.Item2, adim.Item2 * cift.Item2);
                            if (a != cikilan && b != cikilan && serbest.Contains(a) && serbest.Contains(b))
                            {
                                desenler.Add(new[] { cikilan, a, b });
                            }
                        }
                    }
                }
            }

            return desenler
                .OrderBy(d => d[0], TaramaSirasi.SolUsttenSatirSatir)
                .ThenBy(d => d[1], TaramaSirasi.SolUsttenSatirSatir)
                .ThenBy(d => d[2], TaramaSirasi.SolUsttenSatirSatir)
                .ToList();
        }
    }
}
