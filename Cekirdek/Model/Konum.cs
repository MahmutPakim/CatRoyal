using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Model
{
    /// <summary>
    /// Hücre koordinatı, GDD 0.5'teki görsel düzende: (satır, sütun); satır 1 en üstte, sütun 1 en solda.
    /// </summary>
    public readonly struct Konum : IEquatable<Konum>
    {
        public Konum(int satir, int sutun)
        {
            Satir = satir;
            Sutun = sutun;
        }

        public int Satir { get; }
        public int Sutun { get; }

        public Konum Yukari => new Konum(Satir - 1, Sutun);
        public Konum Asagi => new Konum(Satir + 1, Sutun);
        public Konum Sol => new Konum(Satir, Sutun - 1);
        public Konum Sag => new Konum(Satir, Sutun + 1);

        /// <summary>4 dik komşu: yukarı, sağ, aşağı, sol (GDD 2.1.2). Tahta sınırı denetlenmez.</summary>
        public IEnumerable<Konum> DikKomsular()
        {
            yield return Yukari;
            yield return Sag;
            yield return Asagi;
            yield return Sol;
        }

        public bool DikKomsuMu(Konum diger) =>
            Math.Abs(Satir - diger.Satir) + Math.Abs(Sutun - diger.Sutun) == 1;

        public Konum Otele(int satirFarki, int sutunFarki) => new Konum(Satir + satirFarki, Sutun + sutunFarki);

        public bool Equals(Konum diger) => Satir == diger.Satir && Sutun == diger.Sutun;

        public override bool Equals(object? nesne) => nesne is Konum diger && Equals(diger);

        public override int GetHashCode() => (Satir * 397) ^ Sutun;

        public static bool operator ==(Konum a, Konum b) => a.Equals(b);

        public static bool operator !=(Konum a, Konum b) => !a.Equals(b);

        public override string ToString() => $"({Satir},{Sutun})";
    }

    /// <summary>Hücrelerin tarama sıraları (AK-06, AK-08).</summary>
    public static class TaramaSirasi
    {
        /// <summary>Sol-üstten başlar, satır satır ilerler.</summary>
        public static readonly IComparer<Konum> SolUsttenSatirSatir =
            Comparer<Konum>.Create((a, b) =>
                a.Satir != b.Satir ? a.Satir.CompareTo(b.Satir) : a.Sutun.CompareTo(b.Sutun));

        /// <summary>Sol-alttan başlar, sütun sütun ve her sütunda aşağıdan yukarıya ilerler.</summary>
        public static readonly IComparer<Konum> SolAlttanSutunSutun =
            Comparer<Konum>.Create((a, b) =>
                a.Sutun != b.Sutun ? a.Sutun.CompareTo(b.Sutun) : b.Satir.CompareTo(a.Satir));
    }
}
