using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>
    /// Aşağı inen toplanabilirler (GDD 6.3.5): sütunundaki toplama hücresine inen nesne toplanır ve hedef
    /// sayacını düşürür. Tahtada hiç toplama işareti yoksa her sütunun en alt oynanabilir hücresi toplama hücresidir.
    /// </summary>
    public static class Toplayici
    {
        /// <returns>En az bir nesne toplandıysa true.</returns>
        public static bool Topla(OyunBaglami baglam, List<Olay> olaylar)
        {
            var tahta = baglam.Tahta;
            bool isaretVar = tahta.TumKonumlar().Any(k => tahta[k].Toplama);
            bool toplandi = false;

            foreach (var konum in tahta.TumKonumlar())
            {
                var hucre = tahta[konum];
                var tas = hucre.Tas;
                if (!hucre.Oynanabilir
                    || tas == null
                    || tas.Tur != TasTuru.Engel
                    || tas.Engel!.Arketip != EngelArketipi.AsagiInenToplanabilir)
                {
                    continue;
                }

                bool toplamaHucresi = isaretVar ? hucre.Toplama : EnAltOynanabilir(tahta, konum);
                if (!toplamaHucresi)
                {
                    continue;
                }

                hucre.Tas = null;
                olaylar.Add(new OgeToplandi(konum, tas));
                baglam.Hedefler?.Say(tas.Engel.HedefTuru, olaylar);
                toplandi = true;
            }

            return toplandi;
        }

        private static bool EnAltOynanabilir(Tahta tahta, Konum konum)
        {
            for (var alt = konum.Asagi; tahta.Icinde(alt); alt = alt.Asagi)
            {
                if (tahta[alt].Oynanabilir)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
