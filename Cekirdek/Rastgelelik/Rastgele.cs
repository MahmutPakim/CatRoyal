using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Rastgelelik
{
    /// <summary>
    /// Çekirdeğin bütün rastgeleliği bu arayüzden geçer; böylece her sonuç tohumla yeniden üretilebilir.
    /// </summary>
    public interface IRastgele
    {
        /// <summary>[0, ustSinir) aralığında tam sayı. ustSinir en az 1 olmalı.</summary>
        int Sonraki(int ustSinir);

        /// <summary>[0, 1) aralığında ondalıklı sayı.</summary>
        double SonrakiOran();
    }

    /// <summary>
    /// Tohumlanabilir, platformdan bağımsız üreteç (SplitMix64). System.Random çalışma zamanına göre
    /// farklı dizi üretebildiği için kullanılmıyor.
    /// </summary>
    public sealed class TohumluRastgele : IRastgele
    {
        private ulong _durum;

        public TohumluRastgele(ulong tohum)
        {
            _durum = tohum;
        }

        public TohumluRastgele(int tohum) : this(unchecked((ulong)tohum))
        {
        }

        public int Sonraki(int ustSinir)
        {
            if (ustSinir < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ustSinir), "Üst sınır en az 1 olmalı.");
            }

            // Yanlılığı önlemek için reddetme örneklemesi.
            ulong sinir = (ulong)ustSinir;
            ulong esik = (0UL - sinir) % sinir;
            while (true)
            {
                ulong deger = Sonraki64();
                if (deger >= esik)
                {
                    return (int)(deger % sinir);
                }
            }
        }

        public double SonrakiOran() => (Sonraki64() >> 11) * (1.0 / (1UL << 53));

        /// <summary>
        /// Bu üreteçten bağımsız bir alt akış türetir. Farklı alt sistemler (dolgu, hedef seçimi…)
        /// birbirinin çekiş sayısından etkilenmesin diye kullanılır.
        /// </summary>
        public TohumluRastgele Dallan() => new TohumluRastgele(Sonraki64());

        private ulong Sonraki64()
        {
            unchecked
            {
                _durum += 0x9E3779B97F4A7C15UL;
                ulong z = _durum;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }

    public static class RastgeleUzantilari
    {
        /// <summary>Listeyi yerinde karıştırır (Fisher–Yates).</summary>
        public static void Karistir<T>(this IRastgele rastgele, IList<T> liste)
        {
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = rastgele.Sonraki(i + 1);
                T gecici = liste[i];
                liste[i] = liste[j];
                liste[j] = gecici;
            }
        }

        /// <summary>Boş olmayan listeden bir öğe seçer.</summary>
        public static T Sec<T>(this IRastgele rastgele, IReadOnlyList<T> liste)
        {
            if (liste.Count == 0)
            {
                throw new ArgumentException("Boş listeden seçim yapılamaz.", nameof(liste));
            }

            return liste[rastgele.Sonraki(liste.Count)];
        }
    }
}
