using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Etkiler
{
    /// <summary>Eşleşme patlaması, vuruş ve hasar (GDD 2.5, 2.7, 3.1.6, 6.1, 6.3).</summary>
    public sealed partial class EtkiMotoru
    {
        /// <summary>Bu hamlede kırılan ve çevresine yayılacak engeller: üretici (6.3.2), yayılan tehlike (6.3.3).</summary>
        private readonly List<KeyValuePair<Konum, EngelTanimi>> _yayilacaklar = new List<KeyValuePair<Konum, EngelTanimi>>();

        // ------------------------------------------------------------------ Eşleşme grupları

        /// <summary>
        /// Kabul edilmiş grupları patlatır: taşlar kalkar, özel taş doğum hücresinde oluşur (GDD 2.5, 2.7),
        /// eşleşen hücrelerin dik komşuları yan eşleşme hasarı alır (6.1.2), yayılan zemin yayılır (6.3.1).
        /// </summary>
        /// <param name="gruplar">Kabul sırasıyla gruplar.</param>
        /// <param name="dogumHucresiSec">Her grup için doğum hücresi; özel taş üretmeyen grupta null döner.</param>
        /// <param name="kaskad">Gruplar kaskadla mı oluştu?</param>
        /// <param name="cikilan">Oyuncu hamlesinde çıkılan hücre; yayılan zeminin başlangıcı için (6.3.1).</param>
        /// <param name="gidilen">Oyuncu hamlesinde gidilen hücre.</param>
        public void GruplariPatlat(
            IReadOnlyList<EslesmeGrubu> gruplar,
            Func<EslesmeGrubu, Konum?> dogumHucresiSec,
            bool kaskad = false,
            Konum? cikilan = null,
            Konum? gidilen = null)
        {
            // Doğum hücreleri patlamadan önce seçilir: seçim taşların oturma adımına bakar (GDD 2.7.4).
            var dogumlar = gruplar.Select(dogumHucresiSec).ToList();

            for (int i = 0; i < gruplar.Count; i++)
            {
                var grup = gruplar[i];
                var hucreler = grup.TumHucreler.ToList();

                // GDD 6.3.1: yayılım, patlamadan önceki zeminli hücrelerden hesaplanır.
                var yayilim = YayilimPlanla(hucreler, cikilan, gidilen);

                // Grup olayı, grubun yol açtığı hasar ve hedef olaylarından önce gelir; patlayan taşların
                // listesi aşağıda dolar.
                var patlayanlar = new List<int>();
                _olaylar.Add(new GrupPatladi(grup, dogumlar[i], patlayanlar, kaskad));

                foreach (var konum in hucreler)
                {
                    var hucre = _t[konum];
                    if (hucre.UstKatman != null)
                    {
                        // GDD 6.1.7, 6.2.5: eşleşme önce üst katmana vurur; katman kırılmadan alttaki taş patlamaz.
                        if (hucre.UstKatman.Tanim.KendiHucresindekiEslesmedenHasar)
                        {
                            KatmanaHasarVer(konum, ustte: true);
                        }

                        continue;
                    }

                    if (hucre.Tas != null)
                    {
                        patlayanlar.Add(hucre.Tas.Kimlik);
                        RenkHedefiniSay(hucre.Tas);
                        hucre.Tas = null;

                        // GDD 6.1.8: hücredeki taş patlayınca alt zemin katmanı bir vuruş alır.
                        if (hucre.AltKatman != null && hucre.AltKatman.Tanim.KendiHucresindekiEslesmedenHasar)
                        {
                            ZemineHasarVer(konum);
                        }
                    }
                }

                var tur = grup.Sekil.UretilenTur();
                if (tur.HasValue && dogumlar[i].HasValue)
                {
                    var ozel = _t.OzelTasUret(tur.Value, grup.Sekil.UretilenCizgiYonu());
                    ozel.OturmaAdimi = _b.Saat;
                    _t[dogumlar[i]!.Value].Tas = ozel;
                    _olaylar.Add(new OzelTasDogdu(dogumlar[i]!.Value, ozel));
                }

                foreach (var cift in yayilim)
                {
                    ZeminiYay(cift.Key, cift.Value);
                }

                YanEslesmeHasariVer(hucreler);
            }
        }

        /// <summary>
        /// GDD 6.3.1: eşleşmenin hücrelerinden en az biri yayılan zeminliyse zemin, zeminli hücrelerden başlayarak
        /// eşleşmenin diğer hücrelerine yayılır; sıra zeminli hücreye uzaklık sırasıdır. Oyuncu hamlesinde kaydırılan
        /// taş zeminli bir hücreden geldiyse yayılım o hücreden başlar.
        /// </summary>
        private List<KeyValuePair<Konum, KatmanTanimi>> YayilimPlanla(List<Konum> hucreler, Konum? cikilan, Konum? gidilen)
        {
            var kaynaklar = hucreler.Where(k => YayilanZemin(k) != null).ToList();

            if (cikilan.HasValue && gidilen.HasValue)
            {
                // Eşleşmeyi tamamlayan taş, grubun içerdiği kaydırma hücresine diğer hücreden gelmiştir.
                Konum? geldigiHucre = hucreler.Contains(gidilen.Value) ? cikilan
                    : hucreler.Contains(cikilan.Value) ? gidilen
                    : null;
                if (geldigiHucre.HasValue && YayilanZemin(geldigiHucre.Value) != null)
                {
                    kaynaklar.Add(geldigiHucre.Value);
                }
            }

            var plan = new List<KeyValuePair<Konum, KatmanTanimi>>();
            if (kaynaklar.Count == 0)
            {
                return plan;
            }

            int Uzaklik(Konum k) => kaynaklar.Min(kaynak => Math.Abs(k.Satir - kaynak.Satir) + Math.Abs(k.Sutun - kaynak.Sutun));

            foreach (var hedef in hucreler
                .Where(k => _t[k].AltKatman == null)
                .OrderBy(Uzaklik)
                .ThenBy(k => k, TaramaSirasi.SolUsttenSatirSatir))
            {
                var enYakin = kaynaklar.OrderBy(kaynak => Math.Abs(hedef.Satir - kaynak.Satir) + Math.Abs(hedef.Sutun - kaynak.Sutun)).First();
                plan.Add(new KeyValuePair<Konum, KatmanTanimi>(hedef, YayilanZemin(enYakin)!));
            }

            return plan;
        }

        /// <summary>Hücrenin altındaki yayılan zeminin tanımı; yoksa null.</summary>
        private KatmanTanimi? YayilanZemin(Konum konum)
        {
            if (!_t.Oynanabilir(konum))
            {
                return null;
            }

            var alt = _t[konum].AltKatman;
            return alt != null && alt.Tanim.Yayilir ? alt.Tanim : null;
        }

        private void ZeminiYay(Konum konum, KatmanTanimi tanim)
        {
            var hucre = _t[konum];
            if (!hucre.Oynanabilir || hucre.AltKatman != null)
            {
                return;
            }

            hucre.AltKatman = new HucreKatmani(tanim, 1);
            _olaylar.Add(new ZeminYayildi(konum, tanim, _simdi));
            _b.Hedefler?.Say(tanim.HedefTuru, _olaylar);
        }

        /// <summary>
        /// GDD 6.1.2: eşleşen her hücrenin 4 dik komşusu bir vuruş alır; çaprazlar almaz. Özel taşlar ve normal
        /// taşlar bu vuruştan etkilenmez (3.1.4); hasarı engelin ya da katmanın tanımı belirler.
        /// Vuruş önce üst katmana gider (6.1.7); üst katman yoksa hücreyi dolduran engele, o da yoksa zemine.
        /// AK-11: A'da bir öğe bir gruptan en fazla 1 vuruş alır, B'de değdiği her hücre için 1 vuruş.
        /// Çoklu vuruş özelliği olan engel her zaman değdiği her hücre için vuruş alır (6.1.4).
        /// </summary>
        private void YanEslesmeHasariVer(List<Konum> grupHucreleri)
        {
            var grup = new HashSet<Konum>(grupHucreleri);
            var vurulan = new HashSet<Konum>();
            bool grupBasinaTek = _b.Bayraklar[Ak.YanHasarVurusSayisi] == Varyant.A;

            foreach (var hucreKonumu in grupHucreleri)
            {
                foreach (var komsu in hucreKonumu.DikKomsular())
                {
                    if (grup.Contains(komsu) || !_t.Oynanabilir(komsu))
                    {
                        continue;
                    }

                    var hucre = _t[komsu];
                    var tas = hucre.Tas;
                    bool ustKatman = hucre.UstKatman != null;
                    bool engel = !ustKatman && tas != null && tas.Tur == TasTuru.Engel;

                    bool hasarAlir = ustKatman ? hucre.UstKatman!.Tanim.YanEslesmeHasari
                        : engel ? tas!.Engel!.YanEslesmeHasari
                        : hucre.AltKatman != null && hucre.AltKatman.Tanim.YanEslesmeHasari && !hucre.AltKatman.Tanim.Yayilir;
                    if (!hasarAlir)
                    {
                        continue;
                    }

                    bool cokluVurus = engel && tas!.Engel!.CokluVurus;
                    bool ilkVurus = vurulan.Add(komsu);
                    if (!ilkVurus && grupBasinaTek && !cokluVurus)
                    {
                        continue;
                    }

                    if (ustKatman)
                    {
                        KatmanaHasarVer(komsu, ustte: true);
                    }
                    else if (engel)
                    {
                        // Engel önceki vuruşla kırıldıysa hücre boşalmıştır.
                        if (ReferenceEquals(hucre.Tas, tas))
                        {
                            EngeleHasarVer(komsu, tas!);
                        }
                    }
                    else if (hucre.AltKatman != null)
                    {
                        ZemineHasarVer(komsu);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Vuruş

        /// <summary>
        /// GDD 3.1.6, 6.1.5: bir etkinin değdiği hücre bir vuruş alır. Vuruş önce üst katmana gider (6.1.7);
        /// üst katman yoksa normal taşı patlatır, engele hasar verir, özel taşı zincire sokar (3.1.2).
        /// Hücredeki öğe patlayınca ya da hücre boşsa alt zemin katmanı da bir vuruş alır (6.1.6, 6.1.8).
        /// </summary>
        /// <param name="konum">Vurulan hücre.</param>
        /// <param name="tur">Vuruşun kaynağı; bağışıklıklar buna göre denetlenir.</param>
        /// <param name="yayilanZemin">Verilirse, vurulan hücreye yayılan zemin de yayılır (6.3.1).</param>
        private void Vur(Konum konum, VurusTuru tur, KatmanTanimi? yayilanZemin = null)
        {
            if (!_t.Oynanabilir(konum))
            {
                return;
            }

            _olaylar.Add(new HucreVuruldu(konum, tur, _simdi));
            var hucre = _t[konum];

            if (hucre.UstKatman != null)
            {
                if (!hucre.UstKatman.Tanim.Bagisikliklar.Contains(tur))
                {
                    KatmanaHasarVer(konum, ustte: true);
                }

                ZeminYayilirsa(konum, yayilanZemin);
                return;
            }

            var tas = hucre.Tas;
            bool zemineUlasir = true;
            if (tas != null)
            {
                if (tas.Tur == TasTuru.Engel)
                {
                    if (tas.Engel!.OzelTasVurusu && !tas.Engel.Bagisikliklar.Contains(tur))
                    {
                        EngeleHasarVer(konum, tas);
                    }

                    // Zemin, üstündeki engel kırılmadan vuruş almaz.
                    zemineUlasir = hucre.Tas == null;
                }
                else if (tas.OzelMi)
                {
                    // GDD 3.1.2: aynı özel taş ikinci kez tetiklenmez. 3.5.7: zincirle vurulan renk küresi
                    // kendi hücresinin altındaki zemine de vurur.
                    if (!tas.Tetiklendi)
                    {
                        Etkinlestir(konum, tas, Tetik.Zincir, null);
                    }
                }
                else
                {
                    hucre.Tas = null;
                    _olaylar.Add(new TasPatladi(konum, tas, tur, _simdi));
                    RenkHedefiniSay(tas);
                }
            }

            if (zemineUlasir && hucre.AltKatman != null && !hucre.AltKatman.Tanim.Bagisikliklar.Contains(tur))
            {
                ZemineHasarVer(konum);
            }

            ZeminYayilirsa(konum, yayilanZemin);
        }

        private void ZeminYayilirsa(Konum konum, KatmanTanimi? yayilanZemin)
        {
            if (yayilanZemin != null)
            {
                ZeminiYay(konum, yayilanZemin);
            }
        }

        private void RenkHedefiniSay(Tas tas)
        {
            if (tas.Tur == TasTuru.Normal)
            {
                _b.Hedefler?.Say(Hedef.RenkTuru(tas.Renk), _olaylar);
            }
        }

        /// <summary>GDD 6.1.1: her geçerli vuruşta katman bir azalır; son katman vurulunca engel kırılır.</summary>
        private void EngeleHasarVer(Konum konum, Tas engel)
        {
            engel.Katman--;
            bool kirildi = engel.Katman <= 0;
            if (kirildi)
            {
                engel.Katman = 0;
                _t[konum].Tas = null;
            }

            _olaylar.Add(new EngelHasarAldi(konum, engel, engel.Katman, _simdi));

            if (kirildi)
            {
                // GDD 6.1.10: hedef öğe kırılınca kendi hedef sayacını düşürür.
                _b.Hedefler?.Say(engel.Engel!.HedefTuru, _olaylar);
                if (engel.Engel!.Yayilma != EngelYayilmasi.Yok)
                {
                    _yayilacaklar.Add(new KeyValuePair<Konum, EngelTanimi>(konum, engel.Engel));
                }
            }
        }

        private void KatmanaHasarVer(Konum konum, bool ustte)
        {
            var hucre = _t[konum];
            var katman = ustte ? hucre.UstKatman! : hucre.AltKatman!;
            katman.Katman--;
            if (katman.Katman <= 0)
            {
                katman.Katman = 0;
                if (ustte)
                {
                    hucre.UstKatman = null;
                }
                else
                {
                    hucre.AltKatman = null;
                }
            }

            _olaylar.Add(new KatmanHasarAldi(konum, katman.Tanim, katman.Katman, _simdi));
            if (katman.Katman == 0)
            {
                _b.Hedefler?.Say(katman.Tanim.HedefTuru, _olaylar);
            }
        }

        /// <summary>
        /// Zemine bir vuruş. Yayılan zemin vuruşla katman kaybetmez (1. aşama, karar 2: 6.2.2 tablosu ile 6.3.1 ve
        /// E07 çelişiyor; vuruş yalnızca yayılmayı tetikler).
        /// </summary>
        private void ZemineHasarVer(Konum konum)
        {
            var alt = _t[konum].AltKatman;
            if (alt == null || alt.Tanim.Yayilir)
            {
                return;
            }

            KatmanaHasarVer(konum, ustte: false);
        }

        // ------------------------------------------------------------------ Oyun içi araçlar

        /// <summary>
        /// GDD 3.9.4: araç, verilen hücrelerin her birine bir vuruş yapar. Etki planlanır;
        /// <see cref="DalgayiBitir"/> çağrılınca çalışır.
        /// </summary>
        public void AracVurusu(IReadOnlyList<Konum> hucreler)
        {
            Planla(_simdi, () =>
            {
                foreach (var konum in hucreler)
                {
                    Vur(konum, VurusTuru.Arac);
                }
            });
        }

        // ------------------------------------------------------------------ Üretici ve yayılan tehlike

        /// <summary>
        /// Kırılan üreticiler çevrelerine katman bırakır (GDD 6.3.2), patlayan tehlikeler çevrelerine yeni tehlike
        /// fırlatır (6.3.3). Kırıldıkları patlama dalgası tamamlandıktan sonra, düşmeler başlamadan çağrılır.
        /// "Çevre" 4 dik komşudur (kullanıcı kararı, 6. aşama).
        /// </summary>
        /// <returns>Tahta değiştiyse true.</returns>
        public bool YayilmalariIsle()
        {
            bool degisti = false;
            var liste = _yayilacaklar.ToList();
            _yayilacaklar.Clear();

            foreach (var cift in liste)
            {
                var merkez = cift.Key;
                var tanim = cift.Value;
                var komsular = merkez.DikKomsular().Where(_t.Oynanabilir).ToList();

                if (tanim.Yayilma == EngelYayilmasi.KatmanBirakir && tanim.BirakilanKatman != null)
                {
                    var katman = tanim.BirakilanKatman;

                    // Uygun hücre: kaplama için üstü açık bir taş, zemin için zemini olmayan hücre.
                    var uygun = komsular.Where(k => katman.Ustte
                        ? _t[k].UstKatman == null && _t[k].Tas != null && _t[k].Tas!.Tur != TasTuru.Engel
                        : _t[k].AltKatman == null).ToList();
                    _b.Rastgele.Karistir(uygun);
                    int adet = _b.Parametreler.TamSayi(ParametreAdlari.UreticiYayilmaHucreSayisi);
                    foreach (var hedef in uygun.Take(adet))
                    {
                        var yeni = new HucreKatmani(katman, 1);
                        if (katman.Ustte)
                        {
                            _t[hedef].UstKatman = yeni;
                        }
                        else
                        {
                            _t[hedef].AltKatman = yeni;
                        }

                        _olaylar.Add(new KatmanBirakildi(merkez, hedef, katman));
                        degisti = true;
                    }
                }
                else if (tanim.Yayilma == EngelYayilmasi.TehlikeFirlatir)
                {
                    // Uygun hücre: üstü açık bir normal taş; taş tehlikeye döner (kullanıcı kararı, 6. aşama).
                    var uygun = komsular
                        .Where(k => _t[k].UstKatman == null && _t[k].Tas != null && _t[k].Tas!.Tur == TasTuru.Normal)
                        .ToList();
                    _b.Rastgele.Karistir(uygun);
                    int adet = _b.Parametreler.TamSayi(ParametreAdlari.TehlikeYayilmaSayisi);
                    foreach (var hedef in uygun.Take(adet))
                    {
                        var yeni = _t.EngelUret(tanim, 1);
                        yeni.OturmaAdimi = _t[hedef].Tas!.OturmaAdimi;
                        _t[hedef].Tas = yeni;
                        _olaylar.Add(new TehlikeYayildi(merkez, hedef, yeni));
                        degisti = true;
                    }
                }
            }

            return degisti;
        }
    }
}
