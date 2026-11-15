using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Etkiler
{
    /// <summary>Renk küresi: tek başına etkisi (GDD 3.5, 3.6) ve dönüşüm kombinasyonları (4.3.7–4.3.9, 4.4.1).</summary>
    public sealed partial class EtkiMotoru
    {
        /// <summary>Bekleyen bir renk küresi işleminin neyi beklediği.</summary>
        public enum KureBeklemesi
        {
            /// <summary>Yalnızca süren dalganın bitmesini bekler (AK-09 B).</summary>
            Dalga,

            /// <summary>Düşme ve doldurmanın durulmasını bekler (AK-09 A).</summary>
            Durulma,

            /// <summary>Düşme ve kaskadın durulmasını bekler (AK-14 B).</summary>
            KaskadSonu,
        }

        private enum KureModu
        {
            /// <summary>Tek başına: hedef rengin taşlarına ışın gönderir (GDD 3.5.4).</summary>
            Isin,

            /// <summary>Renk küresi + çizgi (GDD 4.3.7).</summary>
            Cizgi,

            /// <summary>Renk küresi + bomba (GDD 4.3.8).</summary>
            Bomba,

            /// <summary>Renk küresi + güdümlü (GDD 4.3.9).</summary>
            Gudumlu,
        }

        /// <summary>Bir renk küresinin (ya da küre kombinasyonunun) renk renk ilerleyen işlemi.</summary>
        private sealed class KureIslemi
        {
            public KureIslemi(KureModu mod, Tas? kure, Konum hucre, int kalanRenk)
            {
                Mod = mod;
                Kure = kure;
                Hucre = hucre;
                KalanRenk = kalanRenk;
            }

            public KureModu Mod { get; }

            /// <summary>Tahtada duran küre taşı; kombinasyonlarda null (küre kombinasyon başında kalkar).</summary>
            public Tas? Kure { get; }

            public Konum Hucre { get; set; }

            /// <summary>Temizlenecek kalan renk sayısı: normalde 1, çift renk modunda 2 (GDD 3.6.3).</summary>
            public int KalanRenk { get; set; }

            /// <summary>Sıradaki fazın rengi önceden belliyse (kaydırma: 3.5.1; AK-09 B: tetik anında kilitlenen renk).</summary>
            public int? VerilenRenk { get; set; }

            /// <summary>AK-09 B: renk tetik anında hedeflenenlere eklendi; faz başında yeniden eklenmez.</summary>
            public bool RenkKilitli { get; set; }

            /// <summary>AK-09 B: tetik anında sabitlenen hedef taşlar.</summary>
            public List<Tas>? SabitHedefler { get; set; }

            public List<int> KullanilanRenkler { get; } = new List<int>();

            /// <summary>Çizgi dönüşümünde yönlerin bir yatay bir dikey değişmesi için sayaç (GDD 4.3.7).</summary>
            public int DonusumSayaci { get; set; }

            public bool IlkFaz { get; set; } = true;

            /// <summary>İşlem sıraya alındıysa neyi bekliyor (AK-09, AK-14)?</summary>
            public KureBeklemesi Bekleme { get; set; }
        }

        private readonly List<int> _hedeflenenRenkler = new List<int>();
        private readonly List<KureIslemi> _bekleyen = new List<KureIslemi>();

        private int IsinRenkSayisi() => _b.CiftRenkModu ? 2 : 1;

        /// <summary>AK-15: çift renk modunda kombinasyon A'da iki, B'de tek renk dönüştürür (GDD 4.4.1).</summary>
        private int DonusumRenkSayisi() =>
            _b.CiftRenkModu && _b.Bayraklar[Ak.CiftRenkKombinasyon] == Varyant.A ? 2 : 1;

        /// <summary>
        /// Tahtada en çok bulunan renk (GDD 3.5.2); o sırada bir renk küresinin hedeflediği renkler sayılmaz.
        /// Hiç normal taş yoksa null.
        /// </summary>
        public int? EnYayginRenk() => EnYayginRenk(Enumerable.Empty<int>());

        private int? EnYayginRenk(IEnumerable<int> haric)
        {
            var sayilmayan = new HashSet<int>(_hedeflenenRenkler.Concat(haric));
            var sayim = new Dictionary<int, int>();
            var gorulmeSirasi = new List<int>();

            foreach (var konum in _t.TumKonumlar())
            {
                var tas = _t[konum].Tas;
                if (!_t[konum].Oynanabilir || tas == null || tas.Tur != TasTuru.Normal || sayilmayan.Contains(tas.Renk))
                {
                    continue;
                }

                if (!sayim.ContainsKey(tas.Renk))
                {
                    sayim[tas.Renk] = 0;
                    gorulmeSirasi.Add(tas.Renk);
                }

                sayim[tas.Renk]++;
            }

            if (gorulmeSirasi.Count == 0)
            {
                return null;
            }

            int enCok = sayim.Values.Max();
            var esitler = gorulmeSirasi.Where(renk => sayim[renk] == enCok).ToList();

            // AK-08: A'da sol-üstten satır satır taramada ilk görülen renk; B'de rastgele.
            return _b.Bayraklar[Ak.RenkEsitlikSecimi] == Varyant.A
                ? esitler[0]
                : _b.Rastgele.Sec<int>(esitler.OrderBy(renk => renk).ToList());
        }

        /// <summary>
        /// GDD 3.5.3 (3): başka bir patlamayla vurulan renk küresi hemen etkinleşmez. AK-09 A: renk, tahta
        /// durulduktan sonra sayılır. AK-09 B: renk ve hedef liste tetik anında kilitlenir, ışınlar dalga
        /// tamamlanınca gönderilir.
        /// </summary>
        private void ZincirleVurulanKure(Konum konum, Tas kure)
        {
            kure.Tetiklendi = true;
            _olaylar.Add(new RenkKuresiTetiklendi(konum, kure, _simdi));

            var islem = new KureIslemi(KureModu.Isin, kure, konum, IsinRenkSayisi());
            if (_b.Bayraklar[Ak.RenkKuresiZincirSecimi] == Varyant.A)
            {
                islem.Bekleme = KureBeklemesi.Durulma;
            }
            else
            {
                islem.Bekleme = KureBeklemesi.Dalga;
                int? renk = EnYayginRenk();
                if (renk.HasValue)
                {
                    islem.VerilenRenk = renk;
                    islem.RenkKilitli = true;
                    islem.SabitHedefler = RenginTaslari(renk.Value).Select(k => _t[k].Tas!).ToList();
                    _hedeflenenRenkler.Add(renk.Value);
                }
            }

            _bekleyen.Add(islem);
        }

        /// <summary>Verilen şeyi bekleyen renk küresi işlemi var mı?</summary>
        public bool BekleyenVar(KureBeklemesi bekleme) => _bekleyen.Any(i => i.Bekleme == bekleme);

        /// <summary>
        /// Bekleyen renk küresi işlemlerini yeni bir dalga olarak başlatır. Beklenen koşulu (durulma, kaskad sonu)
        /// çağıran taraf sağlar (AK-09 A, AK-14 B).
        /// </summary>
        public void BekleyenleriBaslat(KureBeklemesi bekleme)
        {
            var baslayacak = _bekleyen.Where(i => i.Bekleme == bekleme).ToList();
            _bekleyen.RemoveAll(i => i.Bekleme == bekleme);
            foreach (var islem in baslayacak)
            {
                Planla(_simdi, () => KureFaziBaslat(islem));
            }
        }

        private List<Konum> RenginTaslari(int renk) =>
            _t.TumKonumlar()
                .Where(k => _t[k].Oynanabilir && _t[k].Tas != null && _t[k].Tas!.Tur == TasTuru.Normal && _t[k].Tas!.Renk == renk)
                .ToList();

        private Konum? Bul(Tas tas)
        {
            foreach (var konum in _t.TumKonumlar())
            {
                if (ReferenceEquals(_t[konum].Tas, tas))
                {
                    return konum;
                }
            }

            return null;
        }

        private void KureFaziBaslat(KureIslemi islem)
        {
            if (islem.Kure != null)
            {
                // Küre durulma sırasında düşmüş olabilir.
                islem.Hucre = Bul(islem.Kure) ?? islem.Hucre;
            }

            bool kilitli = islem.RenkKilitli;
            int? renk = islem.VerilenRenk ?? EnYayginRenk(islem.KullanilanRenkler);
            islem.VerilenRenk = null;
            islem.RenkKilitli = false;
            if (!renk.HasValue)
            {
                KureBitir(islem);
                return;
            }

            if (!kilitli)
            {
                _hedeflenenRenkler.Add(renk.Value);
            }

            islem.KullanilanRenkler.Add(renk.Value);

            // GDD 3.5.4: hedef liste gönderim başlarken sabitlenir; sıra karışıktır.
            List<Konum> hedefler;
            if (islem.SabitHedefler != null)
            {
                hedefler = islem.SabitHedefler.Select(Bul).Where(k => k.HasValue).Select(k => k!.Value).ToList();
                islem.SabitHedefler = null;
            }
            else
            {
                hedefler = RenginTaslari(renk.Value);
            }

            _b.Rastgele.Karistir(hedefler);
            _olaylar.Add(new RenkSecildi(islem.Hucre, renk.Value, hedefler.Count, _simdi));

            long son;
            switch (islem.Mod)
            {
                case KureModu.Cizgi:
                    son = CizgiyeDonustur(islem, hedefler);
                    break;
                case KureModu.Bomba:
                    son = BombayaDonustur(hedefler);
                    break;
                case KureModu.Gudumlu:
                    son = GudumluyeDonustur(islem, hedefler);
                    break;
                default:
                    son = IsinlariGonder(islem, hedefler);
                    break;
            }

            int secilen = renk.Value;
            Planla(son, () => KureFaziBitti(islem, secilen));
        }

        /// <summary>GDD 3.5.4: ışınlar listedeki taşlara sırayla gider; her ışın ulaştığı taşı patlatır.</summary>
        private long IsinlariGonder(KureIslemi islem, List<Konum> hedefler)
        {
            for (int i = 0; i < hedefler.Count; i++)
            {
                var hedef = hedefler[i];
                var tas = _t[hedef].Tas!;
                Planla(_simdi + 1 + i, () =>
                {
                    // Taş bu arada başka bir etkiyle patladıysa ışının hedefi kalmamıştır.
                    if (!ReferenceEquals(_t[hedef].Tas, tas))
                    {
                        return;
                    }

                    _olaylar.Add(new IsinGonderildi(islem.Hucre, hedef, _simdi));
                    Vur(hedef, VurusTuru.RenkIsini);
                });
            }

            return _simdi + 1 + hedefler.Count;
        }

        /// <summary>
        /// GDD 4.3.7: taşlar çizgi taşına döner; yönler dönüşüm sırasına göre bir yatay bir dikey.
        /// Kürenin hücresine de çizgi taşı konur. Dönüşenler dönüşüm sırasıyla birer birer etkinleşir.
        /// </summary>
        private long CizgiyeDonustur(KureIslemi islem, List<Konum> hedefler)
        {
            var yeniler = new List<KeyValuePair<Konum, Tas>>();
            foreach (var hedef in hedefler)
            {
                yeniler.Add(new KeyValuePair<Konum, Tas>(hedef, Donustur(hedef, TasTuru.Cizgi, SiradakiYon(islem))));
            }

            if (islem.IlkFaz)
            {
                var ek = EkTasKoy(islem.Hucre, TasTuru.Cizgi, SiradakiYon(islem));
                if (ek != null)
                {
                    yeniler.Add(new KeyValuePair<Konum, Tas>(islem.Hucre, ek));
                }
            }

            for (int i = 0; i < yeniler.Count; i++)
            {
                var cift = yeniler[i];
                Planla(_simdi + 1 + i, () => DonuseniEtkinlestir(cift.Key, cift.Value));
            }

            return _simdi + 1 + yeniler.Count;
        }

        private static Yon SiradakiYon(KureIslemi islem) =>
            islem.DonusumSayaci++ % 2 == 0 ? Yon.Yatay : Yon.Dikey;

        /// <summary>
        /// GDD 4.3.8: taşlar bomba taşına döner; bütün dönüşümler tamamlandıktan sonra hepsi aynı adımda etkinleşir.
        /// Kürenin hücresine bomba konmaz (11.2.6).
        /// </summary>
        private long BombayaDonustur(List<Konum> hedefler)
        {
            var yeniler = hedefler
                .Select(h => new KeyValuePair<Konum, Tas>(h, Donustur(h, TasTuru.Bomba, null)))
                .ToList();

            Planla(_simdi + 1, () =>
            {
                foreach (var cift in yeniler)
                {
                    DonuseniEtkinlestir(cift.Key, cift.Value);
                }
            });

            return _simdi + 1;
        }

        /// <summary>
        /// GDD 4.3.9: taşlar güdümlü taşa döner; dönüşüm sırasıyla birer birer havalanır ve artı alanlarını
        /// patlatmaz (3.4.2). AK-07 A: kürenin hücresine de bir güdümlü taş konur ve o da uçar.
        /// </summary>
        private long GudumluyeDonustur(KureIslemi islem, List<Konum> hedefler)
        {
            var yeniler = hedefler
                .Select(h => new KeyValuePair<Konum, Tas>(h, Donustur(h, TasTuru.Gudumlu, null)))
                .ToList();

            if (islem.IlkFaz && _b.Bayraklar[Ak.RenkGudumluEkTas] == Varyant.A)
            {
                var ek = EkTasKoy(islem.Hucre, TasTuru.Gudumlu, null);
                if (ek != null)
                {
                    yeniler.Add(new KeyValuePair<Konum, Tas>(islem.Hucre, ek));
                }
            }

            for (int i = 0; i < yeniler.Count; i++)
            {
                var cift = yeniler[i];
                Planla(_simdi + 1 + i, () =>
                {
                    if (!ReferenceEquals(_t[cift.Key].Tas, cift.Value) || cift.Value.Tetiklendi)
                    {
                        return;
                    }

                    Kaldir(cift.Key, cift.Value, Tetik.Donusum);
                    GudumluKaldir(cift.Key, GudumluYuku.Yok, null);
                });
            }

            return _simdi + 1 + yeniler.Count;
        }

        private Tas Donustur(Konum konum, TasTuru tur, Yon? yon)
        {
            var hucre = _t[konum];
            var eski = hucre.Tas!;
            var yeni = _t.OzelTasUret(tur, yon);
            yeni.OturmaAdimi = eski.OturmaAdimi;
            hucre.Tas = yeni;
            _olaylar.Add(new TasDonustu(konum, eski, yeni, _simdi));
            return yeni;
        }

        private Tas? EkTasKoy(Konum konum, TasTuru tur, Yon? yon)
        {
            if (!_t.Oynanabilir(konum) || _t[konum].Tas != null)
            {
                return null;
            }

            var yeni = _t.OzelTasUret(tur, yon);
            _t[konum].Tas = yeni;
            _olaylar.Add(new TasDonustu(konum, null, yeni, _simdi));
            return yeni;
        }

        private void DonuseniEtkinlestir(Konum konum, Tas tas)
        {
            // Dönüşen taş sırası gelmeden zincirle patlamış olabilir (GDD 3.1.3).
            if (ReferenceEquals(_t[konum].Tas, tas) && !tas.Tetiklendi)
            {
                Etkinlestir(konum, tas, Tetik.Donusum, null);
            }
        }

        private void KureFaziBitti(KureIslemi islem, int renk)
        {
            _hedeflenenRenkler.Remove(renk);
            islem.KalanRenk--;
            islem.IlkFaz = false;

            if (islem.KalanRenk <= 0)
            {
                KureBitir(islem);
                return;
            }

            // AK-14: A'da ikinci renk, ilk rengin hedefleri vurulunca hemen seçilir; B'de tahta durulduktan sonra.
            if (_b.Bayraklar[Ak.CiftRenkGecisi] == Varyant.A)
            {
                KureFaziBaslat(islem);
            }
            else
            {
                islem.Bekleme = KureBeklemesi.KaskadSonu;
                _bekleyen.Add(islem);
            }
        }

        private void KureBitir(KureIslemi islem)
        {
            if (islem.Kure == null)
            {
                return;
            }

            var konum = Bul(islem.Kure);
            if (konum.HasValue)
            {
                _t[konum.Value].Tas = null;
                _olaylar.Add(new RenkKuresiTukendi(konum.Value, islem.Kure, _simdi));
            }
        }
    }
}
