using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Etkiler
{
    /// <summary>
    /// Patlamaları, özel taş etkilerini ve kombinasyonları uygular (GDD 3, 4, 6.1).
    ///
    /// Dalga (GDD 0.4; 1. aşama, karar 4): bir etkinleşme ile onun zincirlediği bütün etkiler tek dalgadır.
    /// Dalga mantıksal adımlarla ilerler: çizgi her adımda bir hücre uzar (3.2.3), bomba her adımda bir halka
    /// patlatır (3.3.2), güdümlü taş kalkıştan bir adım sonra iner. Adımların süresi yoktur; sonuç yalnızca
    /// adım sırasına bağlıdır. Dalga bitmeden düşme başlamaz.
    /// </summary>
    public sealed partial class EtkiMotoru
    {
        private readonly OyunBaglami _b;
        private readonly Tahta _t;
        private List<Olay> _olaylar;
        private readonly SortedDictionary<long, List<Action>> _plan = new SortedDictionary<long, List<Action>>();
        private readonly Dictionary<Konum, int> _yonelinen = new Dictionary<Konum, int>();
        private long _simdi;

        /// <summary>
        /// Adımlı durulmada (GDD 2.3.9) aynı motor birden çok çağrı boyunca yaşar; her çağrı kendi olay listesini bağlar.
        /// </summary>
        internal void OlaylariBagla(List<Olay> olaylar) => _olaylar = olaylar;

        public EtkiMotoru(OyunBaglami baglam, List<Olay> olaylar)
        {
            _b = baglam;
            _t = baglam.Tahta;
            _olaylar = olaylar;
        }

        // ------------------------------------------------------------------ Dalga zamanlayıcısı

        private void Planla(long adim, Action islem)
        {
            if (!_plan.TryGetValue(adim, out var liste))
            {
                liste = new List<Action>();
                _plan.Add(adim, liste);
            }

            liste.Add(islem);
        }

        /// <summary>Planlanmış bütün etkileri adım sırasıyla çalıştırır; aynı adımdakiler planlanma sırasıyla.</summary>
        public void DalgayiBitir()
        {
            while (_plan.Count > 0)
            {
                var ilk = _plan.First();
                _simdi = ilk.Key;
                var liste = ilk.Value;

                // Liste çalışırken aynı adıma yeni işlem eklenebilir; indisle dolaşılır.
                for (int i = 0; i < liste.Count; i++)
                {
                    liste[i]();
                }

                _plan.Remove(ilk.Key);
            }

            _simdi++;
        }

        // ------------------------------------------------------------------ Tek başına etkinleşme

        /// <summary>
        /// Hücredeki özel taşı etkinleştirir (GDD 3.1.1). Kaydırmayla etkinleşen renk küresinde
        /// <paramref name="renk"/> diğer taşın rengidir (3.5.1); verilmezse en yaygın renk seçilir (3.5.2).
        /// Etkiler planlanır; <see cref="DalgayiBitir"/> çağrılınca çalışır.
        /// </summary>
        public void OzelTasiEtkinlestir(Konum konum, Tetik tetik, int? renk = null)
        {
            var tas = _t[konum].Tas;
            if (tas == null || !tas.OzelMi)
            {
                throw new InvalidOperationException($"{konum} hücresinde özel taş yok.");
            }

            Planla(_simdi, () => Etkinlestir(konum, tas, tetik, renk));
        }

        private void Etkinlestir(Konum konum, Tas tas, Tetik tetik, int? renk)
        {
            // Zincirle tetiklenen taşın hücresi tetikleyen vuruşu zaten almıştır.
            bool kendiHucresiniVur = tetik != Tetik.Zincir;

            switch (tas.Tur)
            {
                case TasTuru.Cizgi:
                    Kaldir(konum, tas, tetik);
                    CizgiEtkisi(konum, tas.CizgiYonu!.Value, kendiHucresiniVur);
                    break;

                case TasTuru.Bomba:
                    Kaldir(konum, tas, tetik);
                    BombaEtkisi(konum, 2, VurusTuru.Bomba, kendiHucresiniVur);
                    break;

                case TasTuru.Gudumlu:
                    // GDD 3.4.1: önce kendi hücresi, sonra 4 dik komşusu; ardından havalanır (3.4.3).
                    Kaldir(konum, tas, tetik);
                    if (kendiHucresiniVur)
                    {
                        Vur(konum, VurusTuru.Gudumlu);
                    }

                    Planla(_simdi + 1, () =>
                    {
                        ArtiAlaniVur(konum);
                        GudumluKaldir(konum, GudumluYuku.Yok, null);
                    });
                    break;

                case TasTuru.RenkKuresi:
                    if (tetik == Tetik.Zincir)
                    {
                        ZincirleVurulanKure(konum, tas);
                    }
                    else
                    {
                        tas.Tetiklendi = true;
                        _olaylar.Add(new OzelTasEtkinlesti(konum, tas, tetik, _simdi));
                        KureFaziBaslat(new KureIslemi(KureModu.Isin, tas, konum, IsinRenkSayisi()) { VerilenRenk = renk });
                    }

                    break;

                default:
                    throw new InvalidOperationException($"{tas} etkinleştirilemez.");
            }
        }

        /// <summary>GDD 3.1.7: etkinleşen özel taş tahtadan kalkar.</summary>
        private void Kaldir(Konum konum, Tas tas, Tetik tetik)
        {
            tas.Tetiklendi = true;
            if (ReferenceEquals(_t[konum].Tas, tas))
            {
                _t[konum].Tas = null;
            }

            _olaylar.Add(new OzelTasEtkinlesti(konum, tas, tetik, _simdi));
        }

        // ------------------------------------------------------------------ Çizgi ve bomba etkileri

        /// <summary>
        /// GDD 3.2.1–3.2.4: etki merkezden iki yöne ilerler; k. adımda iki kanat da k hücre uzaktaki hücreleri vurur.
        /// Engeller yolu kesmez. Oynanmayan hücre atlanır; bayrak açıksa kanat orada durur (11.2.1).
        /// </summary>
        /// <param name="birKez">
        /// Verilirse, birden fazla çizgi etkisi içeren bir kombinasyonda her hücre en fazla bir kez vurulur.
        /// </param>
        private void CizgiEtkisi(Konum merkez, Yon yon, bool merkeziVur, HashSet<Konum>? birKez = null)
        {
            // GDD 6.3.1 (öneri): çizgi taşı yayılan zeminli bir hücreden etkinleşirse zemini alanına yayar.
            var yayilan = YayilanZemin(merkez);

            void Vurus(Konum konum)
            {
                if (birKez == null || birKez.Add(konum))
                {
                    Vur(konum, VurusTuru.Cizgi, yayilan);
                }
            }

            if (merkeziVur)
            {
                Vurus(merkez);
            }

            int satirAdimi = yon == Yon.Dikey ? 1 : 0;
            int sutunAdimi = yon == Yon.Yatay ? 1 : 0;
            int uzunluk = yon == Yon.Yatay ? _t.SutunSayisi : _t.SatirSayisi;
            bool oynanmayandaDur = _b.Bayraklar.CizgiOynanmayanHucredeDurur;
            var durdu = new bool[2];

            for (int k = 1; k < uzunluk; k++)
            {
                int uzaklik = k;
                Planla(_simdi + k, () =>
                {
                    for (int kanat = 0; kanat < 2; kanat++)
                    {
                        if (durdu[kanat])
                        {
                            continue;
                        }

                        int isaret = kanat == 0 ? -1 : 1;
                        var konum = merkez.Otele(isaret * uzaklik * satirAdimi, isaret * uzaklik * sutunAdimi);
                        if (!_t.Icinde(konum))
                        {
                            continue;
                        }

                        if (!_t[konum].Oynanabilir)
                        {
                            durdu[kanat] = oynanmayandaDur;
                            continue;
                        }

                        Vurus(konum);
                    }
                });
            }
        }

        /// <summary>
        /// GDD 3.3.1–3.3.3: merkezli kare alan, içten dışa halka halka; tahta dışı yok sayılır.
        /// </summary>
        private void BombaEtkisi(Konum merkez, int yaricap, VurusTuru tur, bool merkeziVur)
        {
            // GDD 6.3.1 (öneri): bomba taşı yayılan zeminli bir hücreden etkinleşirse zemini alanına yayar.
            var yayilan = YayilanZemin(merkez);

            if (merkeziVur)
            {
                Vur(merkez, tur, yayilan);
            }

            for (int d = 1; d <= yaricap; d++)
            {
                int halka = d;
                Planla(_simdi + d, () =>
                {
                    foreach (var konum in Halka(merkez, halka))
                    {
                        Vur(konum, tur, yayilan);
                    }
                });
            }
        }

        /// <summary>Merkeze satır ya da sütun farkının büyüğü tam <paramref name="uzaklik"/> olan hücreler.</summary>
        private IEnumerable<Konum> Halka(Konum merkez, int uzaklik)
        {
            for (int ds = -uzaklik; ds <= uzaklik; ds++)
            {
                for (int dt = -uzaklik; dt <= uzaklik; dt++)
                {
                    if (Math.Max(Math.Abs(ds), Math.Abs(dt)) != uzaklik)
                    {
                        continue;
                    }

                    var konum = merkez.Otele(ds, dt);
                    if (_t.Icinde(konum))
                    {
                        yield return konum;
                    }
                }
            }
        }

        private void ArtiAlaniVur(Konum merkez)
        {
            foreach (var komsu in merkez.DikKomsular())
            {
                Vur(komsu, VurusTuru.Gudumlu);
            }
        }

        // ------------------------------------------------------------------ Güdümlü taş

        /// <summary>
        /// Güdümlü taş havalanır: hedefini kalkışta seçer (GDD 3.4.4), bir adım sonra iner ve yüküne göre
        /// hedef hücreye vurur, satır/sütun etkisi ya da 5×5 patlama çalıştırır (3.4.3, 4.3.4, 4.3.5).
        /// </summary>
        private void GudumluKaldir(Konum kalkis, GudumluYuku yuk, Yon? cizgiYonu)
        {
            Konum? hedef;
            switch (yuk)
            {
                case GudumluYuku.Cizgi:
                    hedef = EnIyiSiraHedefi(cizgiYonu!.Value);
                    break;
                case GudumluYuku.Bomba:
                    hedef = EnIyiBolgeMerkezi();
                    break;
                default:
                    hedef = TekHedefSec(kalkis);
                    break;
            }

            _olaylar.Add(new GudumluKalkti(kalkis, hedef, yuk, _simdi));
            if (!hedef.HasValue)
            {
                return;
            }

            var inis = hedef.Value;
            if (yuk == GudumluYuku.Yok)
            {
                _yonelinen[inis] = Yonelinen(inis) + 1;
            }

            Planla(_simdi + 1, () =>
            {
                _olaylar.Add(new GudumluIndi(kalkis, inis, yuk, _simdi));
                switch (yuk)
                {
                    case GudumluYuku.Cizgi:
                        CizgiEtkisi(inis, cizgiYonu!.Value, merkeziVur: true);
                        break;
                    case GudumluYuku.Bomba:
                        BombaEtkisi(inis, 2, VurusTuru.Bomba, merkeziVur: true);
                        break;
                    default:
                        _yonelinen[inis] = Yonelinen(inis) - 1;
                        Vur(inis, VurusTuru.Gudumlu);
                        break;
                }
            });
        }

        private int Yonelinen(Konum konum) => _yonelinen.TryGetValue(konum, out int adet) ? adet : 0;

        private bool HedefOgesi(Konum konum) =>
            _b.HedefOgesiMi != null
                ? _b.HedefOgesiMi(konum)
                : _b.Hedefler != null && _b.Hedefler.HedefOgesiMi(_t, konum);

        /// <summary>
        /// GDD 3.4.5, AK-05 A: adayın puanı. Aday değilse null: boş hücre, oynanmayan hücre, özel taş,
        /// vurulamayan öğe ve örtü altındaki hücre (3.4.7).
        /// </summary>
        private double? AdayPuani(Konum konum)
        {
            if (!_t.Oynanabilir(konum))
            {
                return null;
            }

            var hucre = _t[konum];
            var p = _b.Parametreler;
            bool hedefOgesi = HedefOgesi(konum);
            double hedefPuani = p.Sayi(ParametreAdlari.GudumluPuanHedefOge);
            double katmanPuani = p.Sayi(ParametreAdlari.GudumluPuanKatmanBasina);

            if (hucre.UstKatman != null)
            {
                if (hucre.UstKatman.Tanim.GudumluHedefiniEngeller)
                {
                    return null;
                }

                // Üst katman bir engeldir: vuruş önce ona gider (GDD 6.1.7).
                return (hedefOgesi ? hedefPuani : p.Sayi(ParametreAdlari.GudumluPuanEngel))
                    + (katmanPuani * hucre.UstKatman.Katman);
            }

            var tas = hucre.Tas;
            if (tas == null)
            {
                // Boş hücredeki hedef zemin yine hedeftir.
                return hedefOgesi && hucre.AltKatman != null ? hedefPuani + (katmanPuani * hucre.AltKatman.Katman) : (double?)null;
            }

            if (tas.OzelMi)
            {
                return null;
            }

            if (tas.Tur == TasTuru.Engel)
            {
                if (!tas.Engel!.OzelTasVurusu || tas.Engel.Bagisikliklar.Contains(VurusTuru.Gudumlu))
                {
                    return null;
                }

                double taban = hedefOgesi ? hedefPuani : tas.Engel.GudumluHedefPuani ?? p.Sayi(ParametreAdlari.GudumluPuanEngel);
                return taban + (katmanPuani * tas.Katman);
            }

            return hedefOgesi ? hedefPuani : p.Sayi(ParametreAdlari.GudumluPuanNormalTas);
        }

        /// <summary>
        /// GDD 3.4.4–3.4.6, AK-05: tek hedef seçimi. Başka bir uçan güdümlünün yöneldiği ve o vuruşla tamamen
        /// kırılacak hedefler listeden çıkar. Yorum: çıkarmadan sonra hiç aday kalmazsa çıkarma uygulanmaz;
        /// tahtada hiç aday yoksa güdümlü taş inmez.
        /// </summary>
        private Konum? TekHedefSec(Konum kalkis)
        {
            var hepsi = _t.TumKonumlar().Where(k => k != kalkis && AdayPuani(k).HasValue).ToList();
            var adaylar = hepsi.Where(k => Yonelinen(k) < Dayaniklilik(k)).ToList();
            if (adaylar.Count == 0)
            {
                adaylar = hepsi;
            }

            if (adaylar.Count == 0)
            {
                return null;
            }

            // GDD 3.4.6: güdümlü önceliği açık engeller kaldıkça önce onlara gidilir.
            var oncelikli = adaylar
                .Where(k => _t[k].Tas != null && _t[k].Tas!.Tur == TasTuru.Engel && _t[k].Tas!.Engel!.GudumluOnceligi)
                .ToList();
            if (oncelikli.Count > 0)
            {
                adaylar = oncelikli;
            }

            if (_b.Bayraklar[Ak.GudumluHedefSecimi] == Varyant.B)
            {
                // AK-05 B: yalnızca hedef öğeler; en yakını, eşitlikte rastgele. Hedef öğe yoksa A.
                var hedefOgeleri = adaylar.Where(HedefOgesi).ToList();
                if (hedefOgeleri.Count > 0)
                {
                    int enYakin = hedefOgeleri.Min(k => UzaklikKaresi(k, kalkis));
                    return _b.Rastgele.Sec<Konum>(hedefOgeleri.Where(k => UzaklikKaresi(k, kalkis) == enYakin).ToList());
                }
            }

            double enYuksek = adaylar.Max(k => AdayPuani(k)!.Value);
            return _b.Rastgele.Sec<Konum>(adaylar.Where(k => AdayPuani(k)!.Value == enYuksek).ToList());
        }

        private int Dayaniklilik(Konum konum)
        {
            var hucre = _t[konum];
            if (hucre.UstKatman != null)
            {
                return hucre.UstKatman.Katman;
            }

            var tas = hucre.Tas;
            return tas != null && tas.Tur == TasTuru.Engel ? tas.Katman : 1;
        }

        private static int UzaklikKaresi(Konum a, Konum b)
        {
            int ds = a.Satir - b.Satir;
            int dt = a.Sutun - b.Sutun;
            return (ds * ds) + (dt * dt);
        }

        /// <summary>
        /// GDD 4.3.4: güdümlü hedef puanlarının toplamı en yüksek olan satır (yatay çizgi) ya da sütun (dikey çizgi).
        /// Yorum: eşit toplamlar arasında rastgele seçilir; iniş hücresi o sıradaki en yüksek puanlı hücredir.
        /// </summary>
        private Konum? EnIyiSiraHedefi(Yon yon)
        {
            int siraSayisi = yon == Yon.Yatay ? _t.SatirSayisi : _t.SutunSayisi;
            var siralar = new List<List<Konum>>();
            for (int i = 1; i <= siraSayisi; i++)
            {
                int sira = i;
                var hucreler = _t.TumKonumlar()
                    .Where(k => (yon == Yon.Yatay ? k.Satir : k.Sutun) == sira && _t[k].Oynanabilir)
                    .ToList();
                if (hucreler.Count > 0)
                {
                    siralar.Add(hucreler);
                }
            }

            if (siralar.Count == 0)
            {
                return null;
            }

            double enYuksek = siralar.Max(Toplam);
            var secilen = _b.Rastgele.Sec<List<Konum>>(siralar.Where(s => Toplam(s) == enYuksek).ToList());

            double enIyiHucre = secilen.Max(k => AdayPuani(k) ?? 0);
            return _b.Rastgele.Sec<Konum>(secilen.Where(k => (AdayPuani(k) ?? 0) == enIyiHucre).ToList());
        }

        /// <summary>
        /// GDD 4.3.5: 5×5 alanının güdümlü hedef puanları toplamı en yüksek olan merkez. Eşitlikte rastgele.
        /// </summary>
        private Konum? EnIyiBolgeMerkezi()
        {
            var merkezler = _t.TumKonumlar().Where(k => _t[k].Oynanabilir).ToList();
            if (merkezler.Count == 0)
            {
                return null;
            }

            var toplamlar = merkezler.ToDictionary(
                m => m,
                m => Toplam(Enumerable.Range(0, 3).SelectMany(d => d == 0 ? new[] { m } : Halka(m, d))));
            double enYuksek = toplamlar.Values.Max();
            return _b.Rastgele.Sec<Konum>(merkezler.Where(m => toplamlar[m] == enYuksek).ToList());
        }

        private double Toplam(IEnumerable<Konum> hucreler) => hucreler.Sum(k => AdayPuani(k) ?? 0);
    }
}
