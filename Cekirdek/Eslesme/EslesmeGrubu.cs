using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Eslesme
{
    /// <summary>Eşleşme şekilleri (GDD 2.5).</summary>
    public enum Sekil
    {
        /// <summary>GDD 2.5.1 — özel taş üretmez.</summary>
        Duz3,

        /// <summary>GDD 2.5.2 — dikey çizgi taşı üretir.</summary>
        YatayDortlu,

        /// <summary>GDD 2.5.3 — yatay çizgi taşı üretir.</summary>
        DikeyDortlu,

        /// <summary>GDD 2.5.4 — renk küresi üretir.</summary>
        Duz5,

        /// <summary>GDD 2.5.5 — bomba taşı üretir.</summary>
        L,

        /// <summary>GDD 2.5.6 — bomba taşı üretir.</summary>
        T,

        /// <summary>GDD 2.5.7 — bomba taşı üretir.</summary>
        Arti,

        /// <summary>GDD 2.5.8 — güdümlü taş üretir.</summary>
        Kare,
    }

    public static class SekilUzantilari
    {
        /// <summary>
        /// Öncelik sırası (GDD 2.6.1); küçük sayı önce işlenir:
        /// düz 5'li > L > T ve artı > dikey 4'lü > yatay 4'lü > 2×2 kare > düz 3'lü.
        /// </summary>
        public static int Oncelik(this Sekil sekil)
        {
            switch (sekil)
            {
                case Sekil.Duz5: return 0;
                case Sekil.L: return 1;
                case Sekil.T: return 2;
                case Sekil.Arti: return 2;
                case Sekil.DikeyDortlu: return 3;
                case Sekil.YatayDortlu: return 4;
                case Sekil.Kare: return 5;
                default: return 6;
            }
        }

        /// <summary>Şeklin ürettiği özel taşın türü (GDD 2.5); düz 3'lüde null.</summary>
        public static TasTuru? UretilenTur(this Sekil sekil)
        {
            switch (sekil)
            {
                case Sekil.Duz5: return TasTuru.RenkKuresi;
                case Sekil.L:
                case Sekil.T:
                case Sekil.Arti: return TasTuru.Bomba;
                case Sekil.YatayDortlu:
                case Sekil.DikeyDortlu: return TasTuru.Cizgi;
                case Sekil.Kare: return TasTuru.Gudumlu;
                default: return null;
            }
        }

        /// <summary>
        /// Eşleşmeden doğan çizgi taşının yönü her zaman eşleşme sırasına diktir (GDD 2.5.10).
        /// </summary>
        public static Yon? UretilenCizgiYonu(this Sekil sekil)
        {
            switch (sekil)
            {
                case Sekil.YatayDortlu: return Yon.Dikey;
                case Sekil.DikeyDortlu: return Yon.Yatay;
                default: return null;
            }
        }
    }

    /// <summary>Kabul edilmiş bir eşleşme grubu (GDD 2.6.2).</summary>
    public sealed class EslesmeGrubu
    {
        private readonly List<Konum> _uzatma = new List<Konum>();

        internal EslesmeGrubu(Sekil sekil, int renk, IReadOnlyList<Konum> hucreler)
        {
            Sekil = sekil;
            Renk = renk;
            Hucreler = hucreler;
        }

        public Sekil Sekil { get; }
        public int Renk { get; }

        /// <summary>Şekli oluşturan hücreler, sol-üstten satır satır sıralı.</summary>
        public IReadOnlyList<Konum> Hucreler { get; }

        /// <summary>
        /// AK-10 A ile gruba eklenen artık hücreler (GDD 2.6.6). Grubun şeklini ve ürettiği özel taşı değiştirmez.
        /// </summary>
        public IReadOnlyList<Konum> UzatmaHucreleri => _uzatma;

        /// <summary>Grupla birlikte patlayan bütün hücreler.</summary>
        public IEnumerable<Konum> TumHucreler => Hucreler.Concat(_uzatma);

        public bool Icerir(Konum konum) => Hucreler.Contains(konum) || _uzatma.Contains(konum);

        internal void UzatmaEkle(Konum konum) => _uzatma.Add(konum);
    }
}
