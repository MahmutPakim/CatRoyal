using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Rastgelelik;
using NUnit.Framework;

namespace CatRoyal.Cekirdek.Testler.Iskelet
{
    [TestFixture]
    [Category("Iskelet")]
    public class RastgeleTestleri
    {
        private static readonly int[] Tohumlar = { 0, 1, 2, 42, 1000, -7, int.MaxValue };

        [TestCaseSource(nameof(Tohumlar))]
        public void Ayni_tohum_ayni_diziyi_uretir(int tohum)
        {
            var a = new TohumluRastgele(tohum);
            var b = new TohumluRastgele(tohum);

            var diziA = Enumerable.Range(0, 200).Select(_ => a.Sonraki(1000)).ToList();
            var diziB = Enumerable.Range(0, 200).Select(_ => b.Sonraki(1000)).ToList();

            Assert.That(diziA, Is.EqualTo(diziB));
        }

        [Test]
        public void Farkli_tohum_farkli_dizi_uretir()
        {
            var a = new TohumluRastgele(1);
            var b = new TohumluRastgele(2);

            var diziA = Enumerable.Range(0, 50).Select(_ => a.Sonraki(1000)).ToList();
            var diziB = Enumerable.Range(0, 50).Select(_ => b.Sonraki(1000)).ToList();

            Assert.That(diziA, Is.Not.EqualTo(diziB));
        }

        [Test]
        public void Dizi_platformdan_bagimsiz_sabittir()
        {
            // SplitMix64, tohum 0: bilinen ilk çıktı 0xE220A8397B1DCDAF.
            var r = new TohumluRastgele(0UL);

            Assert.That(r.Sonraki(int.MaxValue), Is.EqualTo((int)(0xE220A8397B1DCDAFUL % int.MaxValue)));
        }

        [TestCaseSource(nameof(Tohumlar))]
        public void Sonraki_sinirlarin_icinde_kalir_ve_butun_degerleri_gorur(int tohum)
        {
            var r = new TohumluRastgele(tohum);
            var gorulen = new HashSet<int>();

            for (int i = 0; i < 2000; i++)
            {
                int deger = r.Sonraki(6);
                Assert.That(deger, Is.InRange(0, 5));
                gorulen.Add(deger);
            }

            Assert.That(gorulen.Count, Is.EqualTo(6));
        }

        [TestCaseSource(nameof(Tohumlar))]
        public void SonrakiOran_sifir_ile_bir_arasindadir(int tohum)
        {
            var r = new TohumluRastgele(tohum);

            for (int i = 0; i < 2000; i++)
            {
                Assert.That(r.SonrakiOran(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test]
        public void Gecersiz_ust_sinir_hata_verir()
        {
            var r = new TohumluRastgele(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => r.Sonraki(0));
        }

        [TestCaseSource(nameof(Tohumlar))]
        public void Karistir_ogeleri_korur_ve_tohumla_tekrarlanir(int tohum)
        {
            var ilk = Enumerable.Range(0, 30).ToList();
            var ikinci = Enumerable.Range(0, 30).ToList();

            new TohumluRastgele(tohum).Karistir(ilk);
            new TohumluRastgele(tohum).Karistir(ikinci);

            Assert.That(ilk, Is.EqualTo(ikinci));
            Assert.That(ilk, Is.EquivalentTo(Enumerable.Range(0, 30)));
        }

        [Test]
        public void Dallanan_akis_ana_akistan_bagimsiz_ilerler()
        {
            var ana1 = new TohumluRastgele(5);
            var dal1 = ana1.Dallan();
            var ana2 = new TohumluRastgele(5);
            var dal2 = ana2.Dallan();

            // Dal üzerinde farklı sayıda çekiş, ana akışın devamını değiştirmez.
            for (int i = 0; i < 10; i++) dal1.Sonraki(100);

            Assert.That(ana1.Sonraki(1000), Is.EqualTo(ana2.Sonraki(1000)));
            Assert.That(dal2.Sonraki(1000), Is.Not.EqualTo(-1));
        }
    }
}
