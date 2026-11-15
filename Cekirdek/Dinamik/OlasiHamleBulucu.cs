using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Dinamik
{
    public enum OlasiHamleTuru
    {
        /// <summary>Yer değiştirince eşleşme kuran iki normal taş (GDD 2.3.3 a).</summary>
        Eslesme,

        /// <summary>Özel taş içeren kaydırma (GDD 2.3.3 b).</summary>
        OzelTasKaydirma,

        /// <summary>Özel taşa dokunma (GDD 2.3.5).</summary>
        OzelTasDokunma,
    }

    public readonly struct OlasiHamle
    {
        public OlasiHamle(OlasiHamleTuru tur, Konum cikilan, Konum gidilen, bool renkKuresiHamlesi)
        {
            Tur = tur;
            Cikilan = cikilan;
            Gidilen = gidilen;
            RenkKuresiHamlesi = renkKuresiHamlesi;
        }

        public OlasiHamleTuru Tur { get; }
        public Konum Cikilan { get; }

        /// <summary>Dokunmada çıkılan hücreyle aynıdır.</summary>
        public Konum Gidilen { get; }

        /// <summary>Hamle bir renk küresi hamlesi mi (GDD 5.4.5)?</summary>
        public bool RenkKuresiHamlesi { get; }
    }

    /// <summary>
    /// Olası hamleleri bulur (GDD 5.4.4, 5.6.1, 5.8.2). Yorum: olası hamle, eşleşme kuran bir kaydırma ya da
    /// kullanılabilir bir özel taştır (2.3.3, 2.3.5; 5.4.5 özel taş hamlelerini olası hamle sayar).
    /// </summary>
    public static class OlasiHamleBulucu
    {
        public static bool Var(Tahta tahta) => Bul(tahta, yalnizIlki: true).Count > 0;

        public static IReadOnlyList<OlasiHamle> Bul(Tahta tahta, bool yalnizIlki = false)
        {
            var hamleler = new List<OlasiHamle>();
            bool normalTasVar = tahta.TumKonumlar().Any(k => tahta.TasAl(k)?.Tur == TasTuru.Normal);

            foreach (var konum in tahta.TumKonumlar())
            {
                if (!tahta.Kaydirilabilir(konum))
                {
                    continue;
                }

                var tas = tahta[konum].Tas!;
                if (tas.OzelMi && (tas.Tur != TasTuru.RenkKuresi || normalTasVar))
                {
                    // GDD 2.3.6: hedefleyecek taşı olmayan renk küresine dokunmak hamle değildir.
                    hamleler.Add(new OlasiHamle(
                        OlasiHamleTuru.OzelTasDokunma, konum, konum, tas.Tur == TasTuru.RenkKuresi));
                    if (yalnizIlki) return hamleler;
                }

                foreach (var komsu in new[] { konum.Sag, konum.Asagi })
                {
                    if (!tahta.Kaydirilabilir(komsu))
                    {
                        continue;
                    }

                    var diger = tahta[komsu].Tas!;
                    if (tas.OzelMi || diger.OzelMi)
                    {
                        hamleler.Add(new OlasiHamle(
                            OlasiHamleTuru.OzelTasKaydirma,
                            konum,
                            komsu,
                            tas.Tur == TasTuru.RenkKuresi || diger.Tur == TasTuru.RenkKuresi));
                        if (yalnizIlki) return hamleler;
                        continue;
                    }

                    if (tas.Tur == TasTuru.Normal && diger.Tur == TasTuru.Normal && tas.Renk == diger.Renk)
                    {
                        continue;
                    }

                    tahta[konum].Tas = diger;
                    tahta[komsu].Tas = tas;
                    bool eslesir = EslesmeIcinde(tahta, konum) || EslesmeIcinde(tahta, komsu);
                    tahta[konum].Tas = tas;
                    tahta[komsu].Tas = diger;

                    if (eslesir)
                    {
                        hamleler.Add(new OlasiHamle(OlasiHamleTuru.Eslesme, konum, komsu, false));
                        if (yalnizIlki) return hamleler;
                    }
                }
            }

            return hamleler;
        }

        /// <summary>Hücre bir düz 3'lünün ya da 2×2 karenin parçası mı (GDD 2.4.1, 2.4.2)?</summary>
        public static bool EslesmeIcinde(Tahta tahta, Konum konum)
        {
            int renk = tahta.EslesmeRengi(konum);
            if (renk == Tas.RenkYok)
            {
                return false;
            }

            int Say(int satirAdimi, int sutunAdimi)
            {
                int adet = 0;
                for (var k = konum.Otele(satirAdimi, sutunAdimi); tahta.EslesmeRengi(k) == renk; k = k.Otele(satirAdimi, sutunAdimi))
                {
                    adet++;
                }

                return adet;
            }

            if (1 + Say(0, -1) + Say(0, 1) >= 3 || 1 + Say(-1, 0) + Say(1, 0) >= 3)
            {
                return true;
            }

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

        /// <summary>Tahtada hazır eşleşme var mı?</summary>
        public static bool HazirEslesmeVar(Tahta tahta) => tahta.TumKonumlar().Any(k => EslesmeIcinde(tahta, k));
    }
}
