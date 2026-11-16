using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;

namespace CatRoyal.Cekirdek.Bolum
{
    /// <summary>Bölüm hedefi (GDD 7.1.1): tür, adet ve ekran numarası.</summary>
    public sealed class Hedef
    {
        public Hedef(string tur, int adet, int ekran = 1)
        {
            if (adet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(adet), "Hedef adedi negatif olamaz.");
            }

            Tur = tur;
            Adet = adet;
            Ekran = ekran;
        }

        /// <summary>Hedef sayacı kimliği: engel ya da katman tanımındaki hedef türü, ya da bir renk (GDD 7.1.2).</summary>
        public string Tur { get; }

        /// <summary>0 yazılırsa o türün tahtadaki bütün örnekleri hedeftir (GDD 7.1.2 d).</summary>
        public int Adet { get; }

        public int Ekran { get; }

        /// <summary>Belirli bir renkten taş toplama hedefinin tür kimliği (GDD 7.1.2 c).</summary>
        public static string RenkTuru(int renk) => "renk:" + renk;
    }

    /// <summary>
    /// Hedef sayaçları (GDD 7.1). Sayaç, toplanan her öğenin kendi davranışından düşer: öğe kırıldığında,
    /// toplandığında ya da patladığı anda (7.1.3).
    /// </summary>
    public sealed class HedefTakibi
    {
        private sealed class Sayac
        {
            public Sayac(Hedef hedef) => Hedef = hedef;

            public Hedef Hedef { get; }
            public int Kalan { get; set; }

            /// <summary>Adet 0 ise sayı, hedefin ekranı açıldığında tahtadan sayılır (GDD 7.1.2 d).</summary>
            public bool Cozuldu { get; set; }
        }

        private readonly List<Sayac> _sayaclar = new List<Sayac>();
        private bool _kazanmaBildirildi;

        public HedefTakibi(IEnumerable<Hedef> hedefler, Tahta tahta)
        {
            foreach (var hedef in hedefler)
            {
                if (hedef.Ekran < 1)
                {
                    throw new ArgumentException($"'{hedef.Tur}' hedefinin ekran numarası 1'den küçük olamaz.");
                }

                if (_sayaclar.Any(s => s.Hedef.Tur == hedef.Tur && s.Hedef.Ekran == hedef.Ekran))
                {
                    throw new ArgumentException($"'{hedef.Tur}' hedefi aynı ekranda birden fazla kez verildi.");
                }

                _sayaclar.Add(new Sayac(hedef) { Kalan = hedef.Adet, Cozuldu = hedef.Adet > 0 });
            }

            EkraniCoz(tahta);
        }

        /// <summary>Oynanan ekran (GDD 2.1.7, 8.9.1); tek ekranlı bölümde hep 1.</summary>
        public int GecerliEkran { get; private set; } = 1;

        /// <summary>Hedef taşıyan en yüksek ekran numarası.</summary>
        public int SonEkran => _sayaclar.Count == 0 ? 1 : _sayaclar.Max(s => s.Hedef.Ekran);

        private IEnumerable<Sayac> Gecerli => _sayaclar.Where(s => s.Hedef.Ekran == GecerliEkran);

        /// <summary>Geçerli ekranın hedef türleri, bölüm verisindeki sırayla.</summary>
        public IReadOnlyList<string> Turler => Gecerli.Select(s => s.Hedef.Tur).ToList();

        public bool HedefVar => _sayaclar.Count > 0;

        /// <summary>Bütün ekranların hedefleri tamamlandı mı (GDD 7.3.1)? Hedefi olmayan bölüm tamamlanmış sayılmaz.</summary>
        public bool HepsiTamam => HedefVar && _sayaclar.All(s => s.Cozuldu && s.Kalan == 0);

        /// <summary>Geçerli ekranın hedefleri tamamlandı mı (GDD 8.9.1)?</summary>
        public bool EkranTamam => Gecerli.Any() && Gecerli.All(s => s.Kalan == 0);

        /// <summary>Geçerli ekrandaki hedefin kalan adedi.</summary>
        public int Kalan(string tur) => Gecerli.FirstOrDefault(s => s.Hedef.Tur == tur)?.Kalan ?? 0;

        /// <summary>GDD 8.9.1: bir sonraki ekrana geçilir; o ekranın "hepsi" hedefleri yeni tahtadan sayılır.</summary>
        public void SonrakiEkranaGec(Tahta tahta)
        {
            GecerliEkran++;
            EkraniCoz(tahta);
        }

        private void EkraniCoz(Tahta tahta)
        {
            foreach (var sayac in Gecerli.Where(s => !s.Cozuldu))
            {
                // GDD 7.1.2 (d): adet 0 ise o türün tahtadaki bütün örnekleri hedeftir.
                sayac.Kalan = TahtadakiAdet(tahta, sayac.Hedef.Tur);
                sayac.Cozuldu = true;
            }
        }

        /// <summary>
        /// Bir öğe kırıldı, toplandı ya da patladı. Sayaç 0'ın altına inmez; hedefi tamamlanmış türün kalan
        /// örnekleri yine kırılabilir (GDD 7.1.4). Sayaç güncellenince bütün hedefler tamamlandıysa kazanma başlar (7.3.1).
        /// Yalnız geçerli ekranın hedefleri sayılır (2.1.7).
        /// </summary>
        public void Say(string? tur, List<Olay> olaylar)
        {
            var sayac = tur == null ? null : Gecerli.FirstOrDefault(s => s.Hedef.Tur == tur);
            if (sayac == null || sayac.Kalan == 0)
            {
                return;
            }

            sayac.Kalan--;
            olaylar.Add(new HedefIlerledi(sayac.Hedef.Tur, sayac.Kalan));

            if (HepsiTamam && !_kazanmaBildirildi)
            {
                _kazanmaBildirildi = true;
                olaylar.Add(new KazanmaBasladi());
            }
        }

        /// <summary>Hücrede, hedefi henüz tamamlanmamış bir öğe var mı (GDD 3.4.5, AK-05)?</summary>
        public bool HedefOgesiMi(Tahta tahta, Konum konum)
        {
            if (!tahta.Oynanabilir(konum))
            {
                return false;
            }

            return HucredekiTurler(tahta[konum]).Any(tur => Kalan(tur) > 0);
        }

        private static int TahtadakiAdet(Tahta tahta, string tur) =>
            tahta.TumKonumlar().Where(k => tahta[k].Oynanabilir).Sum(k => HucredekiTurler(tahta[k]).Count(t => t == tur));

        private static IEnumerable<string> HucredekiTurler(Hucre hucre)
        {
            if (hucre.UstKatman?.Tanim.HedefTuru != null)
            {
                yield return hucre.UstKatman.Tanim.HedefTuru;
            }

            if (hucre.AltKatman?.Tanim.HedefTuru != null)
            {
                yield return hucre.AltKatman.Tanim.HedefTuru;
            }

            var tas = hucre.Tas;
            if (tas == null)
            {
                yield break;
            }

            if (tas.Tur == TasTuru.Engel && tas.Engel!.HedefTuru != null)
            {
                yield return tas.Engel.HedefTuru;
            }
            else if (tas.Tur == TasTuru.Normal)
            {
                yield return Hedef.RenkTuru(tas.Renk);
            }
        }
    }
}
