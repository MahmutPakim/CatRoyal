using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>Tamamlanan görevin bir harita parçasına etkisi (GDD 10.4.4); değer büyüdükçe parça daha açıktır.</summary>
    public enum ParcaDurumu
    {
        Kapali,
        Gorunur,
        YariAcik,
        Acik,
    }

    /// <summary>Harita keşif olayının ödül hatları (GDD 10.4.6); yüksek hat düşük hattı da kapsar.</summary>
    public enum KesifHatti
    {
        Ucretsiz,
        Dusuk,
        Yuksek,
    }

    /// <summary>Bir hat basamağındaki dört ödül yuvası (GDD 10.4.6).</summary>
    public enum KesifOdulu
    {
        Ucretsiz,
        Dusuk,
        Yuksek,
        YuksekEk,
    }

    /// <summary>
    /// Harita görevi (GDD 10.4.3, 10.4.4). Enerji harcanarak tamamlanır; tamamlanınca harita parçalarını açar ve
    /// üzerindeki paket toplanabilir olur. Büyük görevin alt görevleri vardır: büyük görevin kendi maliyeti yoktur,
    /// bütün alt görevleri bitince tamamlanır.
    /// </summary>
    public sealed class HaritaGorevi
    {
        public HaritaGorevi(string kimlik, int maliyet, params string[] onkosullar)
        {
            Kimlik = kimlik;
            Maliyet = Math.Max(0, maliyet);
            Onkosullar = onkosullar;
        }

        public string Kimlik { get; }

        /// <summary>Enerji maliyeti (parametre: harita_kesif_gorev_maliyeti ortalamadır).</summary>
        public int Maliyet { get; }

        public IReadOnlyList<string> Onkosullar { get; }

        /// <summary>Bu görev bir büyük görevin alt göreviyse büyük görevin kimliği.</summary>
        public string? UstGorev { get; set; }

        /// <summary>Haritada ilerleyen iki karakterden hangisinin görevi: 0 ya da 1 (GDD 10.4.3).</summary>
        public int Karakter { get; set; }

        /// <summary>Görev tamamlanınca açılan parçalar: parça kimliği → ulaştığı durum.</summary>
        public Dictionary<string, ParcaDurumu> Parcalar { get; } = new Dictionary<string, ParcaDurumu>(StringComparer.Ordinal);

        /// <summary>Görev tamamlanınca haritada toplanabilir olan para ya da enerji paketi (GDD 10.4.4).</summary>
        public OdulPaketi? Paket { get; set; }
    }

    /// <summary>
    /// Harita keşif olayının sunucudan gelen yapılandırması (GDD 10.4.9): harita yerleşimi, görev maliyetleri,
    /// sandık ve hat ödülleri, günlük teklifler.
    /// </summary>
    public sealed class HaritaKesifYapilandirmasi
    {
        /// <summary>Sürüm değişmedikçe yapılandırma yeniden uygulanmaz.</summary>
        public int Surum { get; set; } = 1;

        public List<HaritaGorevi> Gorevler { get; } = new List<HaritaGorevi>();

        /// <summary>Sandık ödülleri, eşik sırasıyla (GDD 10.4.5).</summary>
        public List<OdulPaketi?> SandikOdulleri { get; } = new List<OdulPaketi?>();

        /// <summary>
        /// Hat basamaklarının hedefleri: o basamak için gereken toplam harcanmış enerji, artan sırayla. Verilmezse
        /// hedefler haritanın toplam maliyetine eşit aralıklarla yayılır.
        /// </summary>
        public List<int> HatHedefleri { get; } = new List<int>();

        /// <summary>Hat ödülleri: basamak → dört yuva (ücretsiz, düşük, yüksek, yüksek hat ek ödülü).</summary>
        public List<Dictionary<KesifOdulu, OdulPaketi>> HatOdulleri { get; } = new List<Dictionary<KesifOdulu, OdulPaketi>>();

        /// <summary>Günlük teklifler, segment sırasıyla: ilki en pahalı, sonuncusu en uygun (GDD 10.4.7).</summary>
        public List<MagazaUrunu> GunlukTeklifler { get; } = new List<MagazaUrunu>();

        /// <summary>Oyuncunun başlangıç segmenti (0 tabanlı).</summary>
        public int BaslangicSegmenti { get; set; }

        /// <summary>"Son şans" durumunun bitişten ne kadar önce başladığı (GDD 10.4.9); verilmezse bir gün.</summary>
        public TimeSpan? SonSansSuresi { get; set; }
    }

    /// <summary>
    /// Harita keşif olayı (GDD 10.4): bölüm kazanarak enerji toplanır, enerjiyle harita görevleri tamamlanır, harita
    /// açılır ve sandıklara ulaşılır. Üç hatlı bir ödül hattı ve günlük teklifi vardır.
    ///
    /// Durumlar: hazır → (başlangıç penceresi görüldü) aktif → (bitime az kaldı) son şans → (süre doldu) bitti.
    /// "Kilitli" durumu olayın kapısından gelir.
    ///
    /// Yorumlar: ilerleme, tamamlanan yaprak görevlerin sayıca oranıdır; hat basamakları harcanan enerjiyle dolar
    /// ("harcanan enerji ayrıca tutulur", 10.4.2); enerji üst sınırı eldeki enerjiyedir.
    /// </summary>
    public sealed class HaritaKesifOlayi : OlayIskeleti
    {
        /// <summary>Ödül paketlerinde enerjiyi taşıyan olay eşyasının kimliği (GDD 10.4.8, 10.9.5).</summary>
        public const string EnerjiEsyasi = "enerji";

        private readonly ParametreTablosu _p;
        private readonly Dictionary<string, HaritaGorevi> _gorevler = new Dictionary<string, HaritaGorevi>(StringComparer.Ordinal);
        private readonly List<string> _gorevSirasi = new List<string>();
        private readonly HashSet<string> _tamamlanan = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _toplananPaketler = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> _acilanSandiklar = new HashSet<int>();
        private readonly HashSet<string> _alinanHatOdulleri = new HashSet<string>(StringComparer.Ordinal);
        private List<OdulPaketi?> _sandikOdulleri = new List<OdulPaketi?>();
        private List<int> _hatHedefleri = new List<int>();
        private List<Dictionary<KesifOdulu, OdulPaketi>> _hatOdulleri = new List<Dictionary<KesifOdulu, OdulPaketi>>();
        private List<MagazaUrunu> _teklifler = new List<MagazaUrunu>();
        private TimeSpan _sonSansSuresi = TimeSpan.FromDays(1);
        private int _yapilandirmaSurumu;
        private bool _basladi;
        private long? _teklifGunu;
        private bool _teklifAlindi;

        public HaritaKesifOlayi(string kimlik, DateTime bitis, ParametreTablosu p)
            : base(kimlik, MetaParametreAdlari.AcilisHaritaKesif, bitis)
        {
            _p = p;
        }

        public override OlayTuru Tur => OlayTuru.HaritaKesif;

        /// <summary>Eldeki enerji.</summary>
        public int Enerji { get; private set; }

        /// <summary>Harcanan enerji ayrıca tutulur (GDD 10.4.2).</summary>
        public int HarcananEnerji { get; private set; }

        /// <summary>Satın alınan hat (GDD 10.4.6).</summary>
        public KesifHatti Hat { get; private set; } = KesifHatti.Ucretsiz;

        /// <summary>Günlük teklifin segmenti, 0 tabanlı (GDD 10.4.7).</summary>
        public int Segment { get; private set; }

        public IReadOnlyList<HaritaGorevi> Gorevler => _gorevSirasi.Select(k => _gorevler[k]).ToList();

        public HaritaKesifDurumu Durum(DateTime simdi) =>
            simdi >= Bitis ? HaritaKesifDurumu.Bitti
            : !_basladi ? HaritaKesifDurumu.Hazir
            : Bitis - simdi <= _sonSansSuresi ? HaritaKesifDurumu.SonSans
            : HaritaKesifDurumu.Aktif;

        private bool Suruyor(DateTime simdi) => _basladi && simdi < Bitis;

        /// <summary>
        /// Başlangıç penceresi görüldü: olay aktif olur (GDD 10.4.1). Yorum: "yeterli süre" için genel katılma eşiği
        /// kullanılır (parametre: ikinci_olay_asgari_sure_dk).
        /// </summary>
        public bool Basla(DateTime simdi)
        {
            if (_basladi || Bitis - simdi < TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.IkinciOlayAsgariSureDk)))
            {
                return false;
            }

            _basladi = true;
            return true;
        }

        /// <summary>Sunucudan gelen yapılandırmayı uygular; aynı sürüm yeniden uygulanmaz, ilerleme korunur.</summary>
        public void YapilandirmayiUygula(HaritaKesifYapilandirmasi y)
        {
            if (y.Surum == _yapilandirmaSurumu)
            {
                return;
            }

            bool ilk = _yapilandirmaSurumu == 0;
            _yapilandirmaSurumu = y.Surum;
            _gorevler.Clear();
            _gorevSirasi.Clear();
            foreach (var gorev in y.Gorevler)
            {
                _gorevler[gorev.Kimlik] = gorev;
                _gorevSirasi.Add(gorev.Kimlik);
            }

            _tamamlanan.RemoveWhere(k => !_gorevler.ContainsKey(k));
            _sandikOdulleri = y.SandikOdulleri.ToList();
            _hatHedefleri = y.HatHedefleri.ToList();
            _hatOdulleri = y.HatOdulleri.ToList();
            _teklifler = y.GunlukTeklifler.ToList();
            _sonSansSuresi = y.SonSansSuresi ?? TimeSpan.FromDays(1);
            if (ilk)
            {
                Segment = Math.Max(0, y.BaslangicSegmenti);
            }
        }

        // ------------------------------------------------------------------ Enerji

        /// <summary>
        /// Bölüm kazanınca gelen enerji 10.9.2'deki puandır (GDD 10.4.2). Toplanabilir enerjinin bir üst sınırı vardır
        /// (parametre: harita_kesif_azami_enerji).
        /// </summary>
        public override void BolumKazanildi(BolumSonucu sonuc, DateTime simdi)
        {
            if (Suruyor(simdi))
            {
                EnerjiEkle(sonuc.OlayPuani);
            }
        }

        /// <returns>Eklenen enerji; üst sınırı aşan kısım eklenmez.</returns>
        public int EnerjiEkle(int enerji)
        {
            int eklenen = Math.Max(0, Math.Min(enerji, _p.TamSayi(MetaParametreAdlari.HaritaKesifAzamiEnerji) - Enerji));
            Enerji += eklenen;
            return eklenen;
        }

        /// <summary>
        /// Olayı ilerletir: ödüllerden gelen enerji olaya aktarılır (GDD 10.4.8) ve son şans durumuna girilince
        /// bildirim penceresi akışa eklenir (10.4.9).
        /// </summary>
        public void Ilerle(Oyuncu oyuncu, DateTime simdi)
        {
            if (!Suruyor(simdi))
            {
                return;
            }

            if (oyuncu.OlayEsyalari.TryGetValue(EnerjiEsyasi, out int bekleyen) && bekleyen > 0)
            {
                int eklenen = EnerjiEkle(bekleyen);
                oyuncu.OlayEsyalari[EnerjiEsyasi] = bekleyen - eklenen;
            }

            if (Durum(simdi) == HaritaKesifDurumu.SonSans)
            {
                oyuncu.Pencereler.Ekle(new Pencere("harita_kesif_son_sans:" + Kimlik, PencereTuru.Bilgi, Kimlik));
            }
        }

        // ------------------------------------------------------------------ Harita

        public bool Tamamlandi(string gorevKimligi) => _tamamlanan.Contains(gorevKimligi);

        private IEnumerable<HaritaGorevi> AltGorevler(string kimlik) => _gorevler.Values.Where(g => g.UstGorev == kimlik);

        private bool BuyukGorev(string kimlik) => AltGorevler(kimlik).Any();

        public bool Yapilabilir(string gorevKimligi, DateTime simdi) =>
            Suruyor(simdi)
            && _gorevler.TryGetValue(gorevKimligi, out var gorev)
            && !_tamamlanan.Contains(gorevKimligi)
            && !BuyukGorev(gorevKimligi)
            && gorev.Onkosullar.All(_tamamlanan.Contains)
            && Enerji >= gorev.Maliyet;

        /// <summary>
        /// Enerji harcayarak görevi tamamlar (GDD 10.4.3). Bütün alt görevleri biten büyük görev de tamamlanır.
        /// </summary>
        public bool GorevYap(string gorevKimligi, DateTime simdi)
        {
            if (!Yapilabilir(gorevKimligi, simdi))
            {
                return false;
            }

            var gorev = _gorevler[gorevKimligi];
            Enerji -= gorev.Maliyet;
            HarcananEnerji += gorev.Maliyet;
            _tamamlanan.Add(gorevKimligi);

            for (var ust = gorev.UstGorev; ust != null && _gorevler.TryGetValue(ust, out var buyuk); ust = buyuk.UstGorev)
            {
                if (_tamamlanan.Contains(ust) || !AltGorevler(ust).All(a => _tamamlanan.Contains(a.Kimlik)))
                {
                    break;
                }

                _tamamlanan.Add(ust);
            }

            return true;
        }

        /// <summary>
        /// Harita parçasının durumu (GDD 10.4.4): tamamlanan görevlerin verdiği en açık durum; hiçbiri dokunmadıysa kapalı.
        /// </summary>
        public ParcaDurumu Parca(string parcaKimligi) =>
            _tamamlanan
                .Select(k => _gorevler[k].Parcalar.TryGetValue(parcaKimligi, out var durum) ? durum : ParcaDurumu.Kapali)
                .DefaultIfEmpty(ParcaDurumu.Kapali)
                .Max();

        public bool PaketToplanabilir(string gorevKimligi) =>
            _tamamlanan.Contains(gorevKimligi) && _gorevler[gorevKimligi].Paket != null && !_toplananPaketler.Contains(gorevKimligi);

        /// <summary>Tamamlanan görevin açtığı para ya da enerji paketini toplar (GDD 10.4.4).</summary>
        public bool PaketTopla(string gorevKimligi, Oyuncu oyuncu, DateTime simdi)
        {
            if (!PaketToplanabilir(gorevKimligi))
            {
                return false;
            }

            _toplananPaketler.Add(gorevKimligi);
            oyuncu.OdulVer(_gorevler[gorevKimligi].Paket!, simdi);
            Ilerle(oyuncu, simdi);
            return true;
        }

        /// <summary>İlerleme oranı, 0–1: tamamlanan yaprak görevlerin payı.</summary>
        public double Ilerleme
        {
            get
            {
                var yapraklar = _gorevler.Keys.Where(k => !BuyukGorev(k)).ToList();
                return yapraklar.Count == 0 ? 0 : (double)yapraklar.Count(_tamamlanan.Contains) / yapraklar.Count;
            }
        }

        // ------------------------------------------------------------------ Sandıklar

        /// <summary>Sandıkların ilerleme eşikleri (GDD 10.4.5; parametre: harita_kesif_sandik_esikleri).</summary>
        public IReadOnlyList<double> SandikEsikleri => _p.SayiListesi(MetaParametreAdlari.HaritaKesifSandikEsikleri);

        public bool SandikAcilabilir(int sandik) =>
            sandik >= 0 && sandik < SandikEsikleri.Count && !_acilanSandiklar.Contains(sandik) && Ilerleme >= SandikEsikleri[sandik] - 1e-9;

        /// <summary>Eşiğine ulaşılan sandığı açar ve ödülünü verir (GDD 10.4.5).</summary>
        public bool SandikAc(int sandik, Oyuncu oyuncu, DateTime simdi)
        {
            if (!SandikAcilabilir(sandik))
            {
                return false;
            }

            _acilanSandiklar.Add(sandik);
            if (sandik < _sandikOdulleri.Count && _sandikOdulleri[sandik] is { } odul)
            {
                oyuncu.OdulVer(odul, simdi);
            }

            return true;
        }

        // ------------------------------------------------------------------ Ödül hattı

        public int HatBasamakSayisi => _p.TamSayi(MetaParametreAdlari.HaritaKesifHatBasamak);

        /// <summary>Basamağın (0 tabanlı) hedefi: gereken toplam harcanmış enerji (GDD 10.4.6).</summary>
        public int HatHedefi(int basamak)
        {
            if (basamak < _hatHedefleri.Count)
            {
                return _hatHedefleri[basamak];
            }

            int toplam = _gorevler.Values.Sum(g => g.Maliyet);
            return (int)Math.Ceiling(toplam * (basamak + 1) / (double)Math.Max(1, HatBasamakSayisi));
        }

        /// <summary>Geçilen hat basamağı sayısı.</summary>
        public int HatBasamagi
        {
            get
            {
                int basamak = 0;
                while (basamak < HatBasamakSayisi && _gorevler.Count > 0 && HarcananEnerji >= HatHedefi(basamak))
                {
                    basamak++;
                }

                return basamak;
            }
        }

        /// <summary>Düşük ya da yüksek hattı satın alır; hat yalnızca yükseltilir (GDD 10.4.6).</summary>
        public bool HatSatinAl(KesifHatti hat, DateTime simdi)
        {
            if (!Suruyor(simdi) || hat <= Hat)
            {
                return false;
            }

            Hat = hat;
            return true;
        }

        private static string HatAnahtari(int basamak, KesifOdulu yuva) => basamak + ":" + yuva;

        private bool YuvaAcik(KesifOdulu yuva) =>
            yuva == KesifOdulu.Ucretsiz || (yuva == KesifOdulu.Dusuk ? Hat >= KesifHatti.Dusuk : Hat == KesifHatti.Yuksek);

        /// <summary>
        /// Hat ödülü alınabilir mi: basamak geçilmiş, yuvanın hattı satın alınmış ve ödül alınmamış. Hat sonradan
        /// satın alınırsa geçilmiş basamakların ödülleri de alınabilir.
        /// </summary>
        public bool HatOduluAlinabilir(int basamak, KesifOdulu yuva) =>
            basamak >= 0 && basamak < HatBasamagi && YuvaAcik(yuva) && !_alinanHatOdulleri.Contains(HatAnahtari(basamak, yuva));

        public bool HatOdulunuAl(int basamak, KesifOdulu yuva, Oyuncu oyuncu, DateTime simdi)
        {
            if (!HatOduluAlinabilir(basamak, yuva))
            {
                return false;
            }

            _alinanHatOdulleri.Add(HatAnahtari(basamak, yuva));
            if (basamak < _hatOdulleri.Count && _hatOdulleri[basamak].TryGetValue(yuva, out var odul))
            {
                oyuncu.OdulVer(odul, simdi);
            }

            return true;
        }

        // ------------------------------------------------------------------ Günlük teklif

        /// <summary>
        /// O günün teklifi (GDD 10.4.7): oyuncunun segmentine göre seçilir. Önceki günün teklifini almayan oyuncuya
        /// ertesi gün bir sonraki, daha uygun segment önerilir. Bugünkü teklif alındıysa ya da teklif yoksa null.
        /// </summary>
        /// <param name="gun">Sunucu gün numarası (<see cref="GunlukOdul.GunNumarasi"/>).</param>
        /// <param name="simdi">Sunucu saati.</param>
        public MagazaUrunu? GunlukTeklif(long gun, DateTime simdi)
        {
            if (!Suruyor(simdi) || _teklifler.Count == 0)
            {
                return null;
            }

            if (_teklifGunu.HasValue && gun > _teklifGunu.Value)
            {
                if (!_teklifAlindi)
                {
                    Segment = Math.Min(Segment + 1, _teklifler.Count - 1);
                }

                _teklifAlindi = false;
            }

            if (!_teklifGunu.HasValue || gun > _teklifGunu.Value)
            {
                _teklifGunu = gun;
            }

            return _teklifAlindi ? null : _teklifler[Math.Min(Segment, _teklifler.Count - 1)];
        }

        /// <summary>Günlük teklifin ödemesi tamamlandı: paketi verilir; teklif o gün yeniden sunulmaz.</summary>
        public bool GunlukTeklifAlindi(long gun, Oyuncu oyuncu, DateTime simdi)
        {
            var teklif = GunlukTeklif(gun, simdi);
            if (teklif == null)
            {
                return false;
            }

            _teklifAlindi = true;
            oyuncu.OdulVer(teklif.Paket, simdi);
            Ilerle(oyuncu, simdi);
            return true;
        }

        // ------------------------------------------------------------------ Bitiş

        /// <summary>
        /// Bitince alınmamış ödüller verilir (GDD 10.4.9, 10.0.8): açılabilir sandıklar, alınabilir hat ödülleri ve
        /// toplanmamış paketler.
        /// </summary>
        public override List<OdulPaketi> AlinmamislariAl()
        {
            var oduller = new List<OdulPaketi>();
            for (int sandik = 0; sandik < SandikEsikleri.Count; sandik++)
            {
                if (SandikAcilabilir(sandik))
                {
                    _acilanSandiklar.Add(sandik);
                    if (sandik < _sandikOdulleri.Count && _sandikOdulleri[sandik] is { } odul) oduller.Add(odul);
                }
            }

            int basamak = HatBasamagi;
            for (int i = 0; i < basamak; i++)
            {
                foreach (KesifOdulu yuva in Enum.GetValues(typeof(KesifOdulu)))
                {
                    if (HatOduluAlinabilir(i, yuva))
                    {
                        _alinanHatOdulleri.Add(HatAnahtari(i, yuva));
                        if (i < _hatOdulleri.Count && _hatOdulleri[i].TryGetValue(yuva, out var odul)) oduller.Add(odul);
                    }
                }
            }

            foreach (string gorev in _gorevSirasi.Where(PaketToplanabilir).ToList())
            {
                _toplananPaketler.Add(gorev);
                oduller.Add(_gorevler[gorev].Paket!);
            }

            return oduller;
        }

        // ------------------------------------------------------------------ Kayıt

        internal override Dictionary<string, object?> DurumuYaz()
        {
            object? Liste(IEnumerable<string> ogeler) => ogeler.OrderBy(o => o, StringComparer.Ordinal).Cast<object?>().ToList();
            return new Dictionary<string, object?>
            {
                ["basladi"] = _basladi,
                ["enerji"] = Enerji,
                ["harcanan"] = HarcananEnerji,
                ["hat"] = Hat.ToString(),
                ["segment"] = Segment,
                ["teklif_gunu"] = _teklifGunu.HasValue ? (object)_teklifGunu.Value : null,
                ["teklif_alindi"] = _teklifAlindi,
                ["surum"] = _yapilandirmaSurumu,
                ["son_sans_tik"] = _sonSansSuresi.Ticks,
                ["tamamlanan"] = Liste(_tamamlanan),
                ["toplanan_paketler"] = Liste(_toplananPaketler),
                ["acilan_sandiklar"] = _acilanSandiklar.OrderBy(s => s).Cast<object?>().ToList(),
                ["alinan_hat_odulleri"] = Liste(_alinanHatOdulleri),
                ["hat_hedefleri"] = _hatHedefleri.Cast<object?>().ToList(),
                ["sandik_odulleri"] = _sandikOdulleri.Select(o => (object?)o?.DurumuYaz()).ToList(),
                ["hat_odulleri"] = _hatOdulleri.Select(b =>
                {
                    var d = new Dictionary<string, object?>();
                    foreach (var cift in b) d[cift.Key.ToString()] = cift.Value.DurumuYaz();
                    return (object?)d;
                }).ToList(),
                ["teklifler"] = _teklifler.Select(t => (object?)new Dictionary<string, object?>
                {
                    ["kimlik"] = t.Kimlik,
                    ["paket"] = t.Paket.DurumuYaz(),
                }).ToList(),
                ["gorevler"] = _gorevSirasi.Select(k => _gorevler[k]).Select(g =>
                {
                    var parcalar = new Dictionary<string, object?>();
                    foreach (var cift in g.Parcalar) parcalar[cift.Key] = cift.Value.ToString();
                    return (object?)new Dictionary<string, object?>
                    {
                        ["kimlik"] = g.Kimlik,
                        ["maliyet"] = g.Maliyet,
                        ["onkosullar"] = g.Onkosullar.Cast<object?>().ToList(),
                        ["ust"] = g.UstGorev,
                        ["karakter"] = g.Karakter,
                        ["parcalar"] = parcalar,
                        ["paket"] = g.Paket?.DurumuYaz(),
                    };
                }).ToList(),
            };
        }

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            _basladi = KayitAlani.Mantik(d, "basladi", false);
            Enerji = Math.Max(0, KayitAlani.TamSayi(d, "enerji", 0));
            HarcananEnerji = Math.Max(0, KayitAlani.TamSayi(d, "harcanan", 0));
            Hat = d.TryGetValue("hat", out object? hat) && Enum.TryParse(hat as string, out KesifHatti okunan) && Enum.IsDefined(typeof(KesifHatti), okunan)
                ? okunan
                : KesifHatti.Ucretsiz;
            Segment = Math.Max(0, KayitAlani.TamSayi(d, "segment", 0));
            _teklifGunu = d.TryGetValue("teklif_gunu", out object? gun) && gun != null ? KayitAlani.UzunSayi(gun, "teklif_gunu") : (long?)null;
            _teklifAlindi = KayitAlani.Mantik(d, "teklif_alindi", false);
            _yapilandirmaSurumu = Math.Max(0, KayitAlani.TamSayi(d, "surum", 0));
            _sonSansSuresi = KayitAlani.Sure(d, "son_sans_tik") ?? TimeSpan.FromDays(1);

            _gorevler.Clear();
            _gorevSirasi.Clear();
            foreach (var g in KayitAlani.Dizi(d, "gorevler").OfType<Dictionary<string, object?>>())
            {
                if (!(g.TryGetValue("kimlik", out object? kimlik) && kimlik is string ad))
                {
                    continue;
                }

                var gorev = new HaritaGorevi(ad, KayitAlani.TamSayi(g, "maliyet", 0), KayitAlani.Dizi(g, "onkosullar").OfType<string>().ToArray())
                {
                    UstGorev = g.TryGetValue("ust", out object? ust) ? ust as string : null,
                    Karakter = KayitAlani.TamSayi(g, "karakter", 0),
                    Paket = g.TryGetValue("paket", out object? paket) && paket is Dictionary<string, object?> p ? OdulPaketi.Oku(p) : null,
                };
                foreach (var cift in KayitAlani.Nesne(g, "parcalar"))
                {
                    if (Enum.TryParse(cift.Value as string, out ParcaDurumu durum) && Enum.IsDefined(typeof(ParcaDurumu), durum))
                    {
                        gorev.Parcalar[cift.Key] = durum;
                    }
                }

                _gorevler[ad] = gorev;
                _gorevSirasi.Add(ad);
            }

            void Kume(HashSet<string> hedef, string ad, Func<string, bool> gecerli)
            {
                hedef.Clear();
                hedef.UnionWith(KayitAlani.Dizi(d, ad).OfType<string>().Where(gecerli));
            }

            Kume(_tamamlanan, "tamamlanan", _gorevler.ContainsKey);
            Kume(_toplananPaketler, "toplanan_paketler", _gorevler.ContainsKey);
            Kume(_alinanHatOdulleri, "alinan_hat_odulleri", _ => true);
            _acilanSandiklar.Clear();
            foreach (object? sandik in KayitAlani.Dizi(d, "acilan_sandiklar"))
            {
                _acilanSandiklar.Add(KayitAlani.TamSayi(sandik, "acilan_sandiklar"));
            }

            _hatHedefleri = KayitAlani.Dizi(d, "hat_hedefleri").Select(h => KayitAlani.TamSayi(h, "hat_hedefleri")).ToList();
            _sandikOdulleri = KayitAlani.Dizi(d, "sandik_odulleri")
                .Select(o => o is Dictionary<string, object?> paket ? OdulPaketi.Oku(paket) : null)
                .ToList();
            _hatOdulleri = KayitAlani.Dizi(d, "hat_odulleri").OfType<Dictionary<string, object?>>().Select(b =>
            {
                var basamak = new Dictionary<KesifOdulu, OdulPaketi>();
                foreach (var cift in b)
                {
                    if (Enum.TryParse(cift.Key, out KesifOdulu yuva) && cift.Value is Dictionary<string, object?> paket)
                    {
                        basamak[yuva] = OdulPaketi.Oku(paket);
                    }
                }

                return basamak;
            }).ToList();
            _teklifler = KayitAlani.Dizi(d, "teklifler")
                .OfType<Dictionary<string, object?>>()
                .Where(t => t.TryGetValue("kimlik", out object? k) && k is string)
                .Select(t => new MagazaUrunu((string)t["kimlik"]!, OdulPaketi.Oku(KayitAlani.Nesne(t, "paket"))))
                .ToList();
        }
    }
}
