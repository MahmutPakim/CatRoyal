using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Olay yöneticisi (GDD 10.0): olayları sunucu takviminden kurar, bölüm sonuçlarını (kazanım, kayıp, artan hamle)
    /// açık olaylara dağıtır, olay bağımlılığını ve kapatmayı uygular, süresi dolan olayı bitirir.
    ///
    /// Olayların ilerlemesi oyuncu kaydına yazılır: takvim alınamasa da kurulu olaylar sürer ve süreleri dolunca
    /// biter.
    /// </summary>
    public sealed class OlayYoneticisi
    {
        private readonly Oyuncu _oyuncu;
        private readonly List<OlayIskeleti> _olaylar = new List<OlayIskeleti>();
        private readonly Dictionary<string, List<string>> _zorlaAldiranlar = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _engelleyenler = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly HashSet<string> _bitenler = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _uygulananCarpanlar = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _aktarilan = new Dictionary<string, int>(StringComparer.Ordinal);

        public OlayYoneticisi(Oyuncu oyuncu)
        {
            _oyuncu = oyuncu;
        }

        /// <summary>Kurulu olaylar: başlamış, süresi dolmamış ve ödülleri henüz kapatılmamış olanlar.</summary>
        public IReadOnlyList<OlayIskeleti> Kurulu => _olaylar;

        public OlayIskeleti? Bul(string kimlik) => _olaylar.FirstOrDefault(o => o.Kimlik == kimlik);

        /// <summary>Oyuncuya açık olaylar: kapısı geçilmiş, kapatılmamış, engellenmemiş ve süresi dolmamış.</summary>
        public IReadOnlyList<OlayIskeleti> Acik(DateTime simdi) =>
            _olaylar.Where(o => o.Acik(_oyuncu.Kapilar, _oyuncu.EnYuksekBolum, simdi)).ToList();

        /// <summary>
        /// Bölüm öncesi ekranında gösterilecek olaylar (GDD 10.0.9, 9.4.6): gösterimi olay türüne bağlıdır ve aynı
        /// anda gösterilen olay sayısı sınırlıdır. Bitişi en yakın olan önce gelir.
        /// </summary>
        public IReadOnlyList<OlayIskeleti> BolumOncesi(DateTime simdi) =>
            OlayKapilari.BolumOncesi(
                _oyuncu.Parametreler,
                Acik(simdi).Where(o => o.BolumOncesindeGosterilir).OrderBy(o => o.Bitis).ThenBy(o => o.Kimlik, StringComparer.Ordinal));

        /// <summary>
        /// Başlamış bir bölümden çıkmak isteyen oyuncu uyarılır mı (GDD 10.5.5)? Açık bir risk basamakları olayında
        /// çıkış bir şans hakkı götürecekse evet.
        /// </summary>
        public bool CikisUyarisiGerekir(DateTime simdi) =>
            Acik(simdi).OfType<RiskBasamaklariOlayi>().Any(o => o.CikistaHakGider);

        /// <summary>Aynı türün bir sonraki olayına aktarılmayı bekleyen toplanabilir (GDD 10.0.6).</summary>
        public int AktarilmayiBekleyen(OlayTuru tur) => _aktarilan.TryGetValue(tur.ToString(), out int adet) ? adet : 0;

        // ------------------------------------------------------------------ Takvim

        /// <summary>
        /// Takvimi uygular (GDD 10.0.1–10.0.4, 10.0.6–10.0.8): başlamış olayları kurar, kapatma ve bağımlılığı
        /// günceller, süresi dolan olayları bitirir. Biten olayın alınmamış ödülleri kendiliğinden verilir, bitiş
        /// penceresi kuyruğa girer ve aktarılabilen toplanabilirler bir sonraki olaya saklanır.
        /// </summary>
        /// <param name="takvim">Sunucu takvimi; sunucuya ulaşılamadıysa null (yalnızca süresi dolanlar biter).</param>
        /// <param name="simdi">Sunucu saati.</param>
        /// <returns>Bu çağrıda biten olaylar ve verilen ödülleri.</returns>
        public List<DonemBitisi> TakvimiUygula(IReadOnlyList<OlayTakvimKaydi>? takvim, DateTime simdi)
        {
            var bitenler = new List<DonemBitisi>();
            foreach (var olay in _olaylar.Where(o => o.SuresiDoldu(simdi)).ToList())
            {
                bitenler.Add(Bitir(olay, simdi));
            }

            if (takvim == null)
            {
                PencereleriDenetle(simdi);
                return bitenler;
            }

            foreach (var kayit in takvim.OrderBy(k => k.Baslangic))
            {
                if (kayit.Tur == OlayTuru.CarpanEtkinligi)
                {
                    CarpanEtkinliginiUygula(kayit, simdi);
                    continue;
                }

                var olay = Bul(kayit.Kimlik);
                if (olay == null && !_bitenler.Contains(kayit.Kimlik) && simdi >= kayit.Baslangic && simdi < kayit.Bitis)
                {
                    olay = Kur(kayit.Kimlik, kayit.Tur, kayit.Bitis, kayit.BasamakHedefleri, kayit.TurSirasi);
                    if (olay != null)
                    {
                        _olaylar.Add(olay);

                        // GDD 10.0.6: önceki olaydan kalan toplanabilirler yeni olaya aktarılır.
                        string tur = kayit.Tur.ToString();
                        if (_aktarilan.TryGetValue(tur, out int kalan))
                        {
                            olay.ToplanabilirEkle(kalan);
                            _aktarilan.Remove(tur);
                        }
                    }
                }

                if (olay == null)
                {
                    continue;
                }

                olay.Bitis = kayit.Bitis;
                olay.SunucuKapatti = kayit.Kapali;
                olay.ToplanabilirAktarilir = kayit.ToplanabilirAktarilir;
                _engelleyenler[olay.Kimlik] = kayit.EngelleyenOlaylar.ToList();
                _zorlaAldiranlar[olay.Kimlik] = kayit.OdulunuZorlaAldiranOlaylar.ToList();
                if (kayit.Oduller.Count > 0)
                {
                    olay.OdulleriAyarla(kayit.Oduller.ToList());
                }

                if (olay is HaritaKesifOlayi kesif && kayit.HaritaKesif != null)
                {
                    kesif.YapilandirmayiUygula(kayit.HaritaKesif);
                }
            }

            BagimliliklariUygula(simdi);
            PencereleriDenetle(simdi);
            return bitenler;
        }

        /// <summary>
        /// Zamanla ve sunucuyla ilerleyen olayları ilerletir: hız yarışında puan gönderimi, süre sonu ve son liste
        /// denemeleri (GDD 10.1.4, 10.1.7); harita keşifte ödülden gelen enerji ve son şans bildirimi (10.4.8, 10.4.9).
        /// Düzenli aralıkla ve her bölüm sonucundan sonra çağrılır.
        /// </summary>
        /// <param name="sunucu">Zaman, grup ve sıralama.</param>
        /// <param name="simdi">Sunucu saati.</param>
        /// <param name="sosyal">Takım ve ortak işlemleri; verilmezse <paramref name="sunucu"/> bu işi de görüyorsa o kullanılır.</param>
        public void Ilerle(ISunucu sunucu, DateTime simdi, ISosyalSunucu? sosyal = null)
        {
            sosyal ??= sunucu as ISosyalSunucu;
            foreach (var olay in _olaylar)
            {
                bool acik = olay.Acik(_oyuncu.Kapilar, _oyuncu.EnYuksekBolum, simdi);
                if (olay is HizYarisiOlayi yaris)
                {
                    yaris.Ilerle(sunucu, _oyuncu, simdi);
                }
                else if (olay is HaritaKesifOlayi kesif && acik)
                {
                    kesif.Ilerle(_oyuncu, simdi);
                }
                else if (olay is TakimYarisiOlayi takimYarisi && acik)
                {
                    takimYarisi.Ilerle(sunucu, _oyuncu, simdi);
                }
                else if (olay is OrtakliAtisOlayi atis && acik)
                {
                    atis.Ilerle(sosyal, _oyuncu, simdi);
                }
            }
        }

        /// <summary>Takvim kaydından olayı kurar; türü henüz yazılmamışsa null.</summary>
        private OlayIskeleti? Kur(string kimlik, OlayTuru tur, DateTime bitis, IReadOnlyList<int>? hedefler, int turSirasi)
        {
            var p = _oyuncu.Parametreler;
            switch (tur)
            {
                case OlayTuru.BasamakliToplama:
                    return new BasamakliToplamaOlayi(
                        kimlik, MetaParametreAdlari.AcilisIlkToplamaOlayi, bitis, hedefler ?? VarsayilanToplamaHedefleri())
                    {
                        // GDD 10.9.1: her yeni toplama olayı türü bir öncekinden belirli bir bölüm aralığıyla açılır.
                        AcilisKaymasi = Math.Max(0, turSirasi) * p.TamSayi(MetaParametreAdlari.OlayTuruAcilisAraligi),
                    };
                case OlayTuru.RiskBasamaklari:
                    return new RiskBasamaklariOlayi(kimlik, bitis, p, hedefler);
                case OlayTuru.HizYarisi:
                    return new HizYarisiOlayi(kimlik, bitis, p);
                case OlayTuru.HaritaKesif:
                    return new HaritaKesifOlayi(kimlik, bitis, p);
                case OlayTuru.TakimYarisi:
                    return new TakimYarisiOlayi(kimlik, bitis, p);
                case OlayTuru.OrtakliAtis:
                    return new OrtakliAtisOlayi(kimlik, bitis, p);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Sunucu basamak tablosu vermediyse kullanılan tablo. Parametre dosyasında toplama olayının yalnızca basamak
        /// sayısı vardır (toplama_olayi_basamak_sayisi); yorum: hedefler sezon hattının ilk basamaklarından alınır.
        /// </summary>
        private IReadOnlyList<int> VarsayilanToplamaHedefleri()
        {
            var p = _oyuncu.Parametreler;
            int adet = Math.Max(1, p.TamSayi(MetaParametreAdlari.ToplamaOlayiBasamakSayisi));
            var sezon = OdulHatti.SezonBasamakHedefleri(p);
            return Enumerable.Range(0, adet).Select(i => sezon[Math.Min(i, sezon.Count - 1)]).ToList();
        }

        /// <summary>
        /// Çarpan etkinliği (GDD 10.9.6): puan formülüne yalnızca çarpanı ekler. Etkinlik sürerken, çarpan kapısı açık
        /// oyuncuya etkinliğin bitimine kadar süren bir etkinlik çarpanı verilir; her etkinlik bir kez uygulanır.
        /// </summary>
        private void CarpanEtkinliginiUygula(OlayTakvimKaydi kayit, DateTime simdi)
        {
            if (!kayit.Suruyor(simdi)
                || _uygulananCarpanlar.Contains(kayit.Kimlik)
                || !_oyuncu.Kapilar.Acik(MetaParametreAdlari.AcilisCarpanBileti, _oyuncu.EnYuksekBolum))
            {
                return;
            }

            int kat = kayit.CarpanKati ?? _oyuncu.Parametreler.TamSayi(MetaParametreAdlari.CarpanBiletiKati);
            int dakika = (int)Math.Ceiling((kayit.Bitis - simdi).TotalMinutes);
            _oyuncu.Carpan.EtkinlikCarpaniEkle(kat, dakika, simdi);
            _uygulananCarpanlar.Add(kayit.Kimlik);
        }

        /// <summary>
        /// Olay bağımlılığı (GDD 9.4.8, 10.0.4): açık bir olay başka bir olayı engelleyebilir ya da ödülünü zorla
        /// aldırabilir. Engelleme sırayla değerlendirilir: engellenen bir olay başkasını engellemez.
        /// </summary>
        private void BagimliliklariUygula(DateTime simdi)
        {
            foreach (var olay in _olaylar)
            {
                olay.Engelli = false;
            }

            foreach (var olay in _olaylar)
            {
                olay.Engelli = Bagli(_engelleyenler, olay).Any(k => AcikMi(k, simdi));
            }

            foreach (var olay in _olaylar.Where(o => Bagli(_zorlaAldiranlar, o).Any(k => AcikMi(k, simdi))))
            {
                foreach (var odul in olay.AlinmamislariAl())
                {
                    _oyuncu.OdulVer(odul, simdi);
                }
            }
        }

        private static IEnumerable<string> Bagli(Dictionary<string, List<string>> tablo, OlayIskeleti olay) =>
            tablo.TryGetValue(olay.Kimlik, out var liste) ? liste : Enumerable.Empty<string>();

        private bool AcikMi(string kimlik, DateTime simdi) =>
            Bul(kimlik) is { } olay && olay.Acik(_oyuncu.Kapilar, _oyuncu.EnYuksekBolum, simdi);

        /// <summary>
        /// Olay oyuncuya açılınca tanıtım ve başlangıç pencereleri akışa eklenir (GDD 9.4.5, 10.0.3); her olay için bir kez.
        /// </summary>
        private void PencereleriDenetle(DateTime simdi)
        {
            foreach (var olay in Acik(simdi))
            {
                _oyuncu.Pencereler.Ekle(new Pencere("olay_tanitimi:" + olay.Kimlik, PencereTuru.Tanitim, olay.Kimlik));
                _oyuncu.Pencereler.Ekle(new Pencere("olay_baslangici:" + olay.Kimlik, PencereTuru.Baslangic, olay.Kimlik));
            }
        }

        private DonemBitisi Bitir(OlayIskeleti olay, DateTime simdi)
        {
            // GDD 10.0.8: olay süresi bitince alınmamış ödüller kendiliğinden verilir ve bitiş penceresi gösterilir.
            var oduller = olay.AlinmamislariAl();
            foreach (var odul in oduller)
            {
                _oyuncu.OdulVer(odul, simdi);
            }

            // GDD 10.0.6: kalan toplanabilirler aynı türün bir sonraki olayına aktarılabilir.
            if (olay.ToplanabilirAktarilir && olay.AktarilacakToplanabilir > 0)
            {
                string tur = olay.Tur.ToString();
                _aktarilan[tur] = AktarilmayiBekleyen(olay.Tur) + olay.AktarilacakToplanabilir;
            }

            // Kapısına hiç ulaşılmamış olayın bitişi oyuncuya gösterilmez.
            if (_oyuncu.EnYuksekBolum >= olay.AcilisBolumu(_oyuncu.Kapilar))
            {
                _oyuncu.Pencereler.Ekle(new Pencere("olay_bitti:" + olay.Kimlik, PencereTuru.Bilgi, olay.Kimlik));
            }

            _olaylar.Remove(olay);
            _engelleyenler.Remove(olay.Kimlik);
            _zorlaAldiranlar.Remove(olay.Kimlik);
            _bitenler.Add(olay.Kimlik);
            return new DonemBitisi(olay.Kimlik, oduller);
        }

        // ------------------------------------------------------------------ Bölüm sonuçları ve ödüller

        /// <summary>
        /// Bölüm sonucunu açık olaylara dağıtır (GDD 10.0.5, 10.9.2, 7.6.3): kazanımda olay puanı (çarpılabilir
        /// olaylarda çarpanlı, diğerlerinde taban) ve ardından "artan hamleler bitti" bilgisi; kayıpta kayıp bilgisi.
        /// </summary>
        public void BolumSonucunuDagit(BolumSonucu sonuc, bool kurtarmaBolumu, DateTime simdi)
        {
            foreach (var olay in Acik(simdi))
            {
                if (sonuc.Kazanildi)
                {
                    olay.BolumKazanildi(sonuc, simdi);
                }
                else if (sonuc.Kaybedildi)
                {
                    olay.BolumKaybedildi(kurtarmaBolumu);
                }
            }
        }

        /// <summary>Basamak ödülünü alır ve içeriğini oyuncuya verir (GDD 10.0.7, 10.9.3).</summary>
        public bool OduluAl(string olayKimligi, int basamak, DateTime simdi)
        {
            if (!(Bul(olayKimligi) is BasamakliToplamaOlayi olay) || !olay.OduluAl(basamak))
            {
                return false;
            }

            if (olay.Odul(basamak) is { } odul)
            {
                _oyuncu.OdulVer(odul, simdi);
            }

            return true;
        }

        /// <summary>Olayın alınabilir bütün ödüllerini toplu alır (GDD 10.9.3).</summary>
        /// <returns>Alınan ödül sayısı.</returns>
        public int TopluAl(string olayKimligi, DateTime simdi)
        {
            if (!(Bul(olayKimligi) is BasamakliToplamaOlayi olay))
            {
                return 0;
            }

            int alinan = 0;
            for (int i = 0; i < olay.Basamak; i++)
            {
                if (OduluAl(olayKimligi, i, simdi)) alinan++;
            }

            return alinan;
        }

        // ------------------------------------------------------------------ Kayıt

        internal Dictionary<string, object?> DurumuYaz()
        {
            object? Liste(IEnumerable<string> ogeler) => ogeler.OrderBy(o => o, StringComparer.Ordinal).Cast<object?>().ToList();
            var aktarilan = new Dictionary<string, object?>();
            foreach (var cift in _aktarilan.OrderBy(c => c.Key, StringComparer.Ordinal))
            {
                aktarilan[cift.Key] = cift.Value;
            }

            return new Dictionary<string, object?>
            {
                ["kurulu"] = _olaylar.Select(o => (object?)new Dictionary<string, object?>
                {
                    ["kimlik"] = o.Kimlik,
                    ["tur"] = o.Tur.ToString(),
                    ["bitis"] = KayitAlani.Zaman(o.Bitis),
                    ["acilis_kaymasi"] = o.AcilisKaymasi,
                    ["kapali"] = o.SunucuKapatti,
                    ["aktarilir"] = o.ToplanabilirAktarilir,
                    ["hedefler"] = (o as BasamakliToplamaOlayi)?.Hedefler.Cast<object?>().ToList(),
                    ["engelleyenler"] = Liste(Bagli(_engelleyenler, o)),
                    ["zorla_aldiranlar"] = Liste(Bagli(_zorlaAldiranlar, o)),
                    ["durum"] = o.DurumuYaz(),
                }).ToList(),
                ["bitenler"] = Liste(_bitenler),
                ["uygulanan_carpanlar"] = Liste(_uygulananCarpanlar),
                ["aktarilan"] = aktarilan,
            };
        }

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _olaylar.Clear();
            _engelleyenler.Clear();
            _zorlaAldiranlar.Clear();
            foreach (object? oge in KayitAlani.Dizi(d, "kurulu"))
            {
                if (!(oge is Dictionary<string, object?> o)
                    || !(o.TryGetValue("kimlik", out object? kimlik) && kimlik is string ad)
                    || !(o.TryGetValue("tur", out object? turMetni) && Enum.TryParse(turMetni as string, out OlayTuru tur)))
                {
                    continue;
                }

                var hedefler = KayitAlani.Dizi(o, "hedefler").Select(h => KayitAlani.TamSayi(h, "hedefler")).ToList();
                var olay = Kur(ad, tur, KayitAlani.Zaman(o, "bitis", DateTime.MinValue), hedefler.Count > 0 ? hedefler : null, 0);
                if (olay == null)
                {
                    continue;
                }

                olay.AcilisKaymasi = Math.Max(0, KayitAlani.TamSayi(o, "acilis_kaymasi", 0));
                olay.SunucuKapatti = KayitAlani.Mantik(o, "kapali", false);
                olay.ToplanabilirAktarilir = KayitAlani.Mantik(o, "aktarilir", false);
                olay.DurumuYukle(KayitAlani.Nesne(o, "durum"));
                _engelleyenler[ad] = KayitAlani.Dizi(o, "engelleyenler").OfType<string>().ToList();
                _zorlaAldiranlar[ad] = KayitAlani.Dizi(o, "zorla_aldiranlar").OfType<string>().ToList();
                _olaylar.Add(olay);
            }

            _bitenler.Clear();
            _bitenler.UnionWith(KayitAlani.Dizi(d, "bitenler").OfType<string>());
            _uygulananCarpanlar.Clear();
            _uygulananCarpanlar.UnionWith(KayitAlani.Dizi(d, "uygulanan_carpanlar").OfType<string>());
            _aktarilan.Clear();
            foreach (var cift in KayitAlani.Nesne(d, "aktarilan"))
            {
                int adet = KayitAlani.TamSayi(cift.Value, cift.Key);
                if (adet > 0) _aktarilan[cift.Key] = adet;
            }
        }
    }
}
