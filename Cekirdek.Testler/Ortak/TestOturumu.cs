using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Testler.Ortak
{
    /// <summary>Vaka tahtası üzerinde kurulmuş bölüm oturumu ve biriken olayları.</summary>
    public sealed class TestOturumu
    {
        private TestOturumu(TestTahtasi test, BolumOturumu oturum)
        {
            Test = test;
            Oturum = oturum;
        }

        public TestTahtasi Test { get; }
        public BolumOturumu Oturum { get; }
        public Tahta Tahta => Test.Tahta;
        public List<Olay> Olaylar { get; } = new List<Olay>();

        /// <summary>
        /// Varsayılan kurulum (test_vakalari.md): giriş hücreleri her sütunun üstünde, nötr dolgu.
        /// Vaka tahtalarındaki nötr taşlar olası hamle kurmadığı için karıştırma denetimi varsayılan olarak
        /// kapalıdır; <paramref name="karistirma"/> ile açılır.
        /// </summary>
        public static TestOturumu Kur(
            string cizim,
            int hamle = 20,
            IEnumerable<Hedef>? hedefler = null,
            AkBayraklari? bayraklar = null,
            int tohum = 1,
            bool karistirma = false,
            bool notrDolgu = true,
            ParametreTablosu? parametreler = null,
            System.Action<TestTahtasi>? hazirla = null)
        {
            var test = TahtaKurucu.Kur(cizim);
            hazirla?.Invoke(test);
            var baglam = HamleDenemesi.TamBaglamKur(test.Tahta, bayraklar, tohum, notrDolgu: notrDolgu);
            if (parametreler != null)
            {
                baglam = new OyunBaglami(test.Tahta, baglam.Bayraklar, parametreler, baglam.Rastgele)
                {
                    Renkler = baglam.Renkler,
                    Dolgu = baglam.Dolgu,
                    CaprazKaymaIzinli = baglam.CaprazKaymaIzinli,
                };
            }

            var oturum = new BolumOturumu(baglam, hamle, hedefler ?? Enumerable.Empty<Hedef>())
            {
                Kapsam = karistirma ? HamleKapsami.Tam : HamleKapsami.Durulma,
            };
            return new TestOturumu(test, oturum);
        }

        public HamleSonucu Kaydir(Konum cikilan, Konum gidilen) => Oturum.Kaydir(cikilan, gidilen, Olaylar);

        public HamleSonucu Dokun(Konum konum) => Oturum.Dokun(konum, Olaylar);

        public bool Arac(AracTuru tur, Konum? konum = null) => Oturum.AracKullan(tur, konum, Olaylar);

        public IEnumerable<T> OlaylarTurunde<T>() where T : Olay => Olaylar.OfType<T>();

        public int Indis<T>() where T : Olay => Olaylar.FindIndex(o => o is T);
    }
}
