using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Eslesme
{
    /// <summary>
    /// Eşleşme tespiti (GDD 2.4), şekil belirleme (2.5) ve öncelikle kabul (2.6).
    /// Durum taşımaz; tahtayı değiştirmez.
    /// </summary>
    public static class EslesmeBulucu
    {
        private sealed class Aday
        {
            public Aday(Sekil sekil, int renk, List<Konum> hucreler)
            {
                Sekil = sekil;
                Renk = renk;
                Hucreler = hucreler;
            }

            public Sekil Sekil { get; }
            public int Renk { get; }

            /// <summary>Etkin tarama sırasına göre sıralı.</summary>
            public List<Konum> Hucreler { get; }
        }

        /// <summary>
        /// Tahtadaki eşleşmeleri bulur ve öncelik sırasıyla kabul eder.
        /// </summary>
        /// <param name="tahta">İncelenecek tahta.</param>
        /// <param name="bayraklar">AK-06 (eşit öncelikte seçim) ve AK-10 (uzatma) buradan okunur.</param>
        /// <param name="kaydirilanHucreler">
        /// Oyuncu hamlesinde kaydırılan iki hücre: yalnızca bunlardan birini içeren adaylar aranır (GDD 2.4.4).
        /// Kaskadda null verilir ve bütün tahta aranır.
        /// </param>
        /// <returns>Kabul edilen gruplar, kabul sırasıyla.</returns>
        public static IReadOnlyList<EslesmeGrubu> Bul(
            Tahta tahta,
            AkBayraklari bayraklar,
            IReadOnlyCollection<Konum>? kaydirilanHucreler = null)
        {
            // AK-06: eşit öncelikli adaylarda "önce bulunan" adayı belirleyen tarama sırası.
            IComparer<Konum> tarama = bayraklar[Ak.EsitOncelikSecimi] == Varyant.A
                ? TaramaSirasi.SolUsttenSatirSatir
                : TaramaSirasi.SolAlttanSutunSutun;

            var adaylar = AdaylariTopla(tahta, tarama);
            if (kaydirilanHucreler != null)
            {
                adaylar = adaylar.Where(a => a.Hucreler.Any(kaydirilanHucreler.Contains)).ToList();
            }

            // GDD 2.6.1: öncelik; 2.6.5 / AK-06: eşit öncelikte tarama sırasında önce bulunan.
            adaylar.Sort((a, b) =>
            {
                int oncelik = a.Sekil.Oncelik().CompareTo(b.Sekil.Oncelik());
                return oncelik != 0 ? oncelik : HucreDizisiKarsilastir(a.Hucreler, b.Hucreler, tarama);
            });

            // GDD 2.6.2: hücrelerinden herhangi biri sahiplenilmişse aday tamamen atlanır.
            var sahip = new Dictionary<Konum, EslesmeGrubu>();
            var gruplar = new List<EslesmeGrubu>();
            var artiklar = new List<Konum>();
            foreach (var aday in adaylar)
            {
                if (aday.Hucreler.Any(sahip.ContainsKey))
                {
                    artiklar.AddRange(aday.Hucreler);
                    continue;
                }

                var grup = new EslesmeGrubu(
                    aday.Sekil,
                    aday.Renk,
                    aday.Hucreler.OrderBy(k => k, TaramaSirasi.SolUsttenSatirSatir).ToList());
                gruplar.Add(grup);
                foreach (var hucre in aday.Hucreler)
                {
                    sahip[hucre] = grup;
                }
            }

            if (bayraklar[Ak.UzatmaAdimi] == Varyant.A)
            {
                Uzat(tahta, gruplar, sahip, artiklar);
            }

            return gruplar;
        }

        /// <summary>
        /// AK-10 A (GDD 2.6.6): atlanan adaylardan kalan, kabul edilmiş bir gruba bitişik ve aynı renkteki
        /// hücreler o gruba eklenir. Eklenen hücreye bitişik artıklar da eklenir (A09: 5'linin altındaki iki taş).
        /// </summary>
        private static void Uzat(
            Tahta tahta,
            List<EslesmeGrubu> gruplar,
            Dictionary<Konum, EslesmeGrubu> sahip,
            List<Konum> artiklar)
        {
            var bekleyen = artiklar
                .Where(k => !sahip.ContainsKey(k))
                .Distinct()
                .OrderBy(k => k, TaramaSirasi.SolUsttenSatirSatir)
                .ToList();

            bool degisti = true;
            while (degisti && bekleyen.Count > 0)
            {
                degisti = false;
                for (int i = 0; i < bekleyen.Count; i++)
                {
                    var hucre = bekleyen[i];

                    // Yalnızca aynı renkteki komşu gruba eklenir; farklı renkli bir gruba bitişik olmak yetmez.
                    // Aynı renkte birden fazla gruba bitişikse kabul sırasında önce gelen grup alır.
                    int renk = tahta.EslesmeRengi(hucre);
                    var komsuGrup = hucre.DikKomsular()
                        .Where(sahip.ContainsKey)
                        .Select(k => sahip[k])
                        .Where(g => g.Renk == renk)
                        .OrderBy(gruplar.IndexOf)
                        .FirstOrDefault();
                    if (komsuGrup == null)
                    {
                        continue;
                    }

                    komsuGrup.UzatmaEkle(hucre);
                    sahip[hucre] = komsuGrup;
                    bekleyen.RemoveAt(i);
                    i--;
                    degisti = true;
                }
            }
        }

        /// <summary>
        /// GDD 2.4.3: her hücreden başlayan yatay ve dikey pencereler ile 2×2 kareler bulunur;
        /// şekiller pencerelerin birleşme biçiminden çıkar (2.5).
        /// </summary>
        private static List<Aday> AdaylariTopla(Tahta tahta, IComparer<Konum> tarama)
        {
            var adaylar = new List<Aday>();
            var yatayUcluler = new List<Konum[]>();
            var dikeyUcluler = new List<Konum[]>();

            foreach (var baslangic in tahta.TumKonumlar())
            {
                int renk = tahta.EslesmeRengi(baslangic);
                if (renk == Tas.RenkYok)
                {
                    continue;
                }

                for (int uzunluk = 3; uzunluk <= 5; uzunluk++)
                {
                    var yatay = Pencere(tahta, baslangic, 0, 1, uzunluk, renk);
                    if (yatay != null)
                    {
                        if (uzunluk == 3) yatayUcluler.Add(yatay);
                        adaylar.Add(new Aday(DuzSekil(uzunluk, yatayMi: true), renk, Sirala(yatay, tarama)));
                    }

                    var dikey = Pencere(tahta, baslangic, 1, 0, uzunluk, renk);
                    if (dikey != null)
                    {
                        if (uzunluk == 3) dikeyUcluler.Add(dikey);
                        adaylar.Add(new Aday(DuzSekil(uzunluk, yatayMi: false), renk, Sirala(dikey, tarama)));
                    }
                }

                // GDD 2.4.2: 2×2 kare.
                var kare = new[] { baslangic, baslangic.Sag, baslangic.Asagi, baslangic.Asagi.Sag };
                if (kare.All(k => tahta.EslesmeRengi(k) == renk))
                {
                    adaylar.Add(new Aday(Sekil.Kare, renk, Sirala(kare, tarama)));
                }
            }

            // GDD 2.5.5–2.5.7: farklı yönlerdeki iki 3'lünün ortak hücresi L, T ya da artı verir.
            foreach (var yatay in yatayUcluler)
            {
                foreach (var dikey in dikeyUcluler)
                {
                    int yatayIndis = -1;
                    int dikeyIndis = -1;
                    for (int i = 0; i < 3 && yatayIndis < 0; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (yatay[i] == dikey[j])
                            {
                                yatayIndis = i;
                                dikeyIndis = j;
                                break;
                            }
                        }
                    }

                    if (yatayIndis < 0)
                    {
                        continue;
                    }

                    bool yatayOrta = yatayIndis == 1;
                    bool dikeyOrta = dikeyIndis == 1;
                    Sekil sekil = yatayOrta && dikeyOrta ? Sekil.Arti
                        : yatayOrta || dikeyOrta ? Sekil.T
                        : Sekil.L;

                    var hucreler = yatay.Concat(dikey).Distinct().ToArray();
                    adaylar.Add(new Aday(sekil, tahta.EslesmeRengi(yatay[0]), Sirala(hucreler, tarama)));
                }
            }

            return adaylar;
        }

        private static Konum[]? Pencere(Tahta tahta, Konum baslangic, int satirAdimi, int sutunAdimi, int uzunluk, int renk)
        {
            var hucreler = new Konum[uzunluk];
            for (int i = 0; i < uzunluk; i++)
            {
                var konum = baslangic.Otele(i * satirAdimi, i * sutunAdimi);
                if (tahta.EslesmeRengi(konum) != renk)
                {
                    return null;
                }

                hucreler[i] = konum;
            }

            return hucreler;
        }

        private static Sekil DuzSekil(int uzunluk, bool yatayMi)
        {
            switch (uzunluk)
            {
                case 3: return Sekil.Duz3;
                case 4: return yatayMi ? Sekil.YatayDortlu : Sekil.DikeyDortlu;
                default: return Sekil.Duz5;
            }
        }

        private static List<Konum> Sirala(IEnumerable<Konum> hucreler, IComparer<Konum> tarama) =>
            hucreler.OrderBy(k => k, tarama).ToList();

        /// <summary>
        /// "Tarama sırasında önce bulunan aday": adayların tarama sırasındaki ilk hücreleri karşılaştırılır;
        /// eşitse sonraki hücrelere bakılır. Sonuç tahtaya göre belirlidir, rastgelelik içermez.
        /// </summary>
        private static int HucreDizisiKarsilastir(List<Konum> a, List<Konum> b, IComparer<Konum> tarama)
        {
            int ortak = a.Count < b.Count ? a.Count : b.Count;
            for (int i = 0; i < ortak; i++)
            {
                int fark = tarama.Compare(a[i], b[i]);
                if (fark != 0)
                {
                    return fark;
                }
            }

            return a.Count.CompareTo(b.Count);
        }
    }
}
