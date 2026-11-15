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
        private readonly Dictionary<string, int> _kalan = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _sira = new List<string>();
        private bool _kazanmaBildirildi;

        public HedefTakibi(IEnumerable<Hedef> hedefler, Tahta tahta)
        {
            foreach (var hedef in hedefler)
            {
                if (_kalan.ContainsKey(hedef.Tur))
                {
                    throw new ArgumentException($"'{hedef.Tur}' hedefi birden fazla kez verildi.");
                }

                // GDD 7.1.2 (d): adet 0 ise o türün tahtadaki bütün örnekleri hedeftir.
                _kalan.Add(hedef.Tur, hedef.Adet > 0 ? hedef.Adet : TahtadakiAdet(tahta, hedef.Tur));
                _sira.Add(hedef.Tur);
            }
        }

        /// <summary>Hedef türleri, bölüm verisindeki sırayla.</summary>
        public IReadOnlyList<string> Turler => _sira;

        public bool HedefVar => _sira.Count > 0;

        /// <summary>Bütün hedefler tamamlandı mı (GDD 7.3.1)? Hedefi olmayan bölüm tamamlanmış sayılmaz.</summary>
        public bool HepsiTamam => HedefVar && _kalan.Values.All(kalan => kalan == 0);

        public int Kalan(string tur) => _kalan.TryGetValue(tur, out int kalan) ? kalan : 0;

        /// <summary>
        /// Bir öğe kırıldı, toplandı ya da patladı. Sayaç 0'ın altına inmez; hedefi tamamlanmış türün kalan
        /// örnekleri yine kırılabilir (GDD 7.1.4). Sayaç güncellenince bütün hedefler tamamlandıysa kazanma başlar (7.3.1).
        /// </summary>
        public void Say(string? tur, List<Olay> olaylar)
        {
            if (tur == null || !_kalan.TryGetValue(tur, out int kalan) || kalan == 0)
            {
                return;
            }

            _kalan[tur] = kalan - 1;
            olaylar.Add(new HedefIlerledi(tur, kalan - 1));

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
