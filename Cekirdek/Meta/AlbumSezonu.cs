using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Kartların üç kademesi (GDD 9.7.4).</summary>
    public enum KartKademesi
    {
        Siradan,
        Degerli,
        CokDegerli,
    }

    /// <summary>Albüm sezonunun durumları (GDD 9.7 durumlar).</summary>
    public enum AlbumSezonDurumu
    {
        Kilitli,
        Indiriliyor,
        Bekliyor,
        Aktif,
        AsamaTamam,
        Bitti,
    }

    /// <summary>
    /// Kart paketi tanımı (GDD 9.7.5): paketten çıkan kart sayısı ve ağırlık tablosu. Ağırlıklar kademeye ve yıldız
    /// sayısına göredir; bir kartın ağırlığı ikisinin çarpımıdır.
    /// </summary>
    public sealed class KartPaketiTanimi
    {
        public KartPaketiTanimi(string kimlik, int kartSayisi, IReadOnlyList<double> kademeAgirliklari, IReadOnlyList<double> yildizAgirliklari)
        {
            if (kartSayisi < 1 || kademeAgirliklari.Count != 3 || yildizAgirliklari.Count == 0
                || kademeAgirliklari.Concat(yildizAgirliklari).Any(a => a < 0))
            {
                throw new ArgumentException("Kart paketi: en az bir kart, üç kademe ağırlığı ve negatif olmayan ağırlıklar gerekir.");
            }

            Kimlik = kimlik;
            KartSayisi = kartSayisi;
            KademeAgirliklari = kademeAgirliklari;
            YildizAgirliklari = yildizAgirliklari;
        }

        public string Kimlik { get; }
        public int KartSayisi { get; }

        /// <summary>Sıradan, değerli, çok değerli kademelerin ağırlıkları.</summary>
        public IReadOnlyList<double> KademeAgirliklari { get; }

        /// <summary>Yıldız sayısına göre ağırlıklar: [0] bir yıldız, [1] iki yıldız, …</summary>
        public IReadOnlyList<double> YildizAgirliklari { get; }

        public double Agirlik(KartKademesi kademe, int yildiz) =>
            KademeAgirliklari[(int)kademe] * (yildiz >= 1 && yildiz <= YildizAgirliklari.Count ? YildizAgirliklari[yildiz - 1] : 0);
    }

    /// <summary>
    /// Albüm sezonunun yapılandırması; sunucudan gelir (GDD 9.7.3–9.7.5, 9.7.7, 9.7.10). Kaydedilmez: her açılışta
    /// yeniden verilir.
    /// </summary>
    public sealed class AlbumYapilandirmasi
    {
        private readonly int _azamiYildiz;

        public AlbumYapilandirmasi(ParametreTablosu p, int? setSayisi = null)
        {
            SetSayisi = setSayisi ?? p.TamSayi(MetaParametreAdlari.SetSayisi);
            SetBasinaKart = p.TamSayi(MetaParametreAdlari.SetBasinaKart);
            _azamiYildiz = Math.Max(1, p.TamSayi(MetaParametreAdlari.KartAzamiYildiz));
        }

        /// <summary>Sezonun set sayısı yapılandırmadan gelir (GDD 9.7.3).</summary>
        public int SetSayisi { get; }

        public int SetBasinaKart { get; }

        /// <summary>Setlerin kademeleri; verilmeyen setler varsayılan dağılımı kullanır.</summary>
        public Dictionary<int, KartKademesi> SetKademeleri { get; } = new Dictionary<int, KartKademesi>();

        /// <summary>Kartların yıldızları: (set, kart) → yıldız; verilmeyen kartlar varsayılan dağılımı kullanır.</summary>
        public Dictionary<(int Set, int Kart), int> KartYildizlari { get; } = new Dictionary<(int, int), int>();

        public Dictionary<string, KartPaketiTanimi> Paketler { get; } = new Dictionary<string, KartPaketiTanimi>(StringComparer.Ordinal);

        /// <summary>
        /// Diğer olayların ödül listelerindeki kart paketi → bu sezonda verilen paket (GDD 9.7.10). Eşlemede olmayan
        /// paket olduğu gibi verilir.
        /// </summary>
        public Dictionary<string, string> PaketEslemesi { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Set ödülleri (GDD 9.7.7): set → ödül paketi.</summary>
        public Dictionary<int, OdulPaketi> SetOdulleri { get; } = new Dictionary<int, OdulPaketi>();

        /// <summary>Bütün setler dolunca alınan büyük ödül (GDD 9.7.7).</summary>
        public OdulPaketi? BuyukOdul { get; set; }

        /// <summary>
        /// Setin kademesi (GDD 9.7.4). Varsayılan dağılım: setlerin ilk üçte biri sıradan, ikinci üçte biri değerli,
        /// kalanı çok değerlidir.
        /// </summary>
        public KartKademesi Kademe(int set) =>
            SetKademeleri.TryGetValue(set, out var kademe) ? kademe : (KartKademesi)Math.Min(2, set * 3 / Math.Max(1, SetSayisi));

        /// <summary>En üst kademedeki setler ayrıca işaretlenir (GDD 9.7.4).</summary>
        public bool UstKademeSeti(int set) => Kademe(set) == KartKademesi.CokDegerli;

        /// <summary>
        /// Kartın yıldız nadirliği, 1 ile parametre: kart_azami_yildiz arası (GDD 9.7.4). Varsayılan dağılım: set
        /// içinde kart sırası ilerledikçe yıldız artar.
        /// </summary>
        public int Yildiz(int set, int kart) =>
            KartYildizlari.TryGetValue((set, kart), out int yildiz)
                ? Math.Max(1, Math.Min(_azamiYildiz, yildiz))
                : 1 + (kart * _azamiYildiz / Math.Max(1, SetBasinaKart));

        public string PaketiCoz(string kimlik) => PaketEslemesi.TryGetValue(kimlik, out string? sezonPaketi) ? sezonPaketi : kimlik;
    }

    /// <summary>Paketten çıkan bir kart.</summary>
    public readonly struct CekilenKart
    {
        public CekilenKart(int set, int kart, int yildiz, bool yeni)
        {
            Set = set;
            Kart = kart;
            Yildiz = yildiz;
            Yeni = yeni;
        }

        public int Set { get; }
        public int Kart { get; }
        public int Yildiz { get; }

        /// <summary>Yeni kartlar işaretlenir (GDD 9.7.2); yeni değilse çifttir ve yıldıza dönüşmüştür (9.7.8).</summary>
        public bool Yeni { get; }
    }

    /// <summary>
    /// Sezonluk albüm (GDD 9.7): indirme ve açılış kapıları, sezona giriş, kart paketleri, acıma kuralı, set ve büyük
    /// ödüller, rozetli aşamalar, çift kart yıldızları ve sezon sonu.
    ///
    /// Durumlar: kilitli → (indirme bölümü) indiriliyor → (görseller indi) bekliyor → (açılış bölümü ve yeterli
    /// süre) aktif → (büyük ödül alındı) aşama tamam → (sonraki aşama) aktif. Süre dolunca bitti; alınmamış ödüller
    /// verilince yeni sezon için bekliyor.
    /// </summary>
    public sealed class AlbumSezonu
    {
        private readonly ParametreTablosu _p;
        private readonly IlerlemeKapilari _kapilar;
        private readonly HashSet<int> _alinanSetOdulleri = new HashSet<int>();
        private readonly Dictionary<string, int> _bekleyenPaketler = new Dictionary<string, int>(StringComparer.Ordinal);
        private string? _sonBitenKimlik;
        private long? _gonderimGunu;
        private int _bugunGonderilen;
        private DateTime? _sonKartIstegi;

        public AlbumSezonu(ParametreTablosu parametreler, IlerlemeKapilari kapilar)
        {
            _p = parametreler;
            _kapilar = kapilar;
            Yapilandirma = new AlbumYapilandirmasi(parametreler);
            Album = new Album(Yapilandirma.SetSayisi, Yapilandirma.SetBasinaKart);
        }

        public AlbumYapilandirmasi Yapilandirma { get; private set; }

        /// <summary>İçinde bulunulan aşamanın kartları.</summary>
        public Album Album { get; private set; }

        /// <summary>Katılınan sezonun kimliği; sezona girilmediyse null.</summary>
        public string? Kimlik { get; private set; }

        public DateTime Bitis { get; private set; }

        /// <summary>Rozetli aşama, 1 tabanlı (GDD 9.7.7; parametre: rozetli_asama_sayisi).</summary>
        public int Asama { get; private set; } = 1;

        public bool BuyukOdulAlindi { get; private set; }

        /// <summary>Sezon görselleri indi mi (GDD 9.7.1)? İndirmeyi sunum yapar ve bitince bildirir.</summary>
        public bool GorsellerIndi { get; set; }

        /// <summary>Çift kartlardan biriken yıldız (GDD 9.7.8); alan yıldızından ayrıdır.</summary>
        public int CiftYildizi { get; private set; }

        /// <summary>Son yeni karttan beri art arda gelen çift kart sayısı; acıma kuralı bunu kullanır (GDD 9.7.6).</summary>
        public int ArdisikCift { get; private set; }

        public IReadOnlyDictionary<string, int> BekleyenPaketler => _bekleyenPaketler;

        public bool Katildi => Kimlik != null;

        /// <summary>Sunucudan gelen sezon yapılandırmasını verir. Set sayısı değişirse albüm yeniden kurulur.</summary>
        public void YapilandirmayiAyarla(AlbumYapilandirmasi yapilandirma)
        {
            bool boyutDegisti = yapilandirma.SetSayisi != Album.SetSayisi || yapilandirma.SetBasinaKart != Album.SetBasinaKart;
            Yapilandirma = yapilandirma;
            if (boyutDegisti)
            {
                Album = new Album(yapilandirma.SetSayisi, yapilandirma.SetBasinaKart);
                _alinanSetOdulleri.Clear();
                BuyukOdulAlindi = false;
            }
        }

        public AlbumSezonDurumu Durum(int enYuksekBolum, DateTime simdi)
        {
            if (!_kapilar.Acik(MetaParametreAdlari.AcilisAlbumIndirme, enYuksekBolum))
            {
                return AlbumSezonDurumu.Kilitli;
            }

            if (!GorsellerIndi)
            {
                return AlbumSezonDurumu.Indiriliyor;
            }

            if (!Katildi)
            {
                return AlbumSezonDurumu.Bekliyor;
            }

            return simdi >= Bitis ? AlbumSezonDurumu.Bitti
                : BuyukOdulAlindi ? AlbumSezonDurumu.AsamaTamam
                : AlbumSezonDurumu.Aktif;
        }

        /// <summary>Kart paketi alınabilir ve açılabilir mi: sezona girilmiş ve süresi dolmamış.</summary>
        public bool Suruyor(DateTime simdi) => Katildi && simdi < Bitis;

        /// <summary>
        /// Sezona girer (GDD 9.7.1): görseller inmiş, albüm açılmış ve sezonun bitimine en az
        /// parametre: album_sezon_giris_asgari_dk kalmış olmalıdır. Bitmiş bir sezona yeniden girilmez.
        /// </summary>
        public bool Katil(string kimlik, DateTime bitis, int enYuksekBolum, DateTime simdi)
        {
            if (Katildi
                || kimlik == _sonBitenKimlik
                || !GorsellerIndi
                || !_kapilar.Acik(MetaParametreAdlari.AcilisAlbumIndirme, enYuksekBolum)
                || !_kapilar.Acik(MetaParametreAdlari.AcilisAlbum, enYuksekBolum)
                || bitis - simdi < TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.AlbumSezonGirisAsgariDk)))
            {
                return false;
            }

            Kimlik = kimlik;
            Bitis = bitis;
            Asama = 1;
            AsamayiSifirla();
            _bekleyenPaketler.Clear();
            CiftYildizi = 0;
            return true;
        }

        /// <summary>Sunucu sezonun bitişini değiştirebilir.</summary>
        internal void BitisiGuncelle(DateTime bitis) => Bitis = bitis;

        // ------------------------------------------------------------------ Kart paketleri

        /// <summary>
        /// Ödülden gelen kart paketini ekler (GDD 9.7.2). Paket sezona göre değiştirilebilir (9.7.10). Aktif bir
        /// sezon yokken gelen paket verilmez (9.6.5).
        /// </summary>
        public bool PaketEkle(string kimlik, int adet, DateTime simdi)
        {
            if (!Suruyor(simdi) || adet < 1)
            {
                return false;
            }

            string paket = Yapilandirma.PaketiCoz(kimlik);
            _bekleyenPaketler[paket] = (_bekleyenPaketler.TryGetValue(paket, out int var) ? var : 0) + adet;
            return true;
        }

        /// <summary>
        /// Bekleyen bir paketi açar: kartlar ağırlık tablosuyla seçilir (GDD 9.7.5) ve setlere eklenir (9.7.2). Çift
        /// kartlar yıldıza dönüşür (9.7.8).
        ///
        /// Acıma kuralı (9.7.6): eksik kart çıkma olasılığı, ağırlıkların verdiği olasılığa her çift kart için
        /// parametre: acima_katsayisi eklenerek bulunur. Yorum: sayılan çiftler, son yeni karttan beri art arda
        /// gelenlerdir; yeni kart çıkınca sayaç sıfırlanır.
        /// </summary>
        /// <returns>Çıkan kartlar; paket yoksa, tanımı bilinmiyorsa ya da sezon sürmüyorsa null.</returns>
        public IReadOnlyList<CekilenKart>? PaketAc(string kimlik, IRastgele rastgele, DateTime simdi)
        {
            if (!Suruyor(simdi)
                || !_bekleyenPaketler.TryGetValue(kimlik, out int adet)
                || !Yapilandirma.Paketler.TryGetValue(kimlik, out var tanim))
            {
                return null;
            }

            if (adet <= 1) _bekleyenPaketler.Remove(kimlik);
            else _bekleyenPaketler[kimlik] = adet - 1;

            var cekilenler = new List<CekilenKart>();
            for (int i = 0; i < tanim.KartSayisi; i++)
            {
                var (set, kart) = KartSec(tanim, rastgele);
                int yildiz = Yapilandirma.Yildiz(set, kart);
                bool yeni = Album.KartEkle(set, kart);
                if (yeni)
                {
                    ArdisikCift = 0;
                }
                else
                {
                    ArdisikCift++;
                    CiftYildizi += yildiz * CiftYildizCarpani();
                }

                cekilenler.Add(new CekilenKart(set, kart, yildiz, yeni));
            }

            return cekilenler;
        }

        private (int Set, int Kart) KartSec(KartPaketiTanimi tanim, IRastgele rastgele)
        {
            var eksik = new List<((int, int) Kart, double Agirlik)>();
            var sahip = new List<((int, int) Kart, double Agirlik)>();
            for (int set = 0; set < Album.SetSayisi; set++)
            {
                for (int kart = 0; kart < Album.SetBasinaKart; kart++)
                {
                    double agirlik = tanim.Agirlik(Yapilandirma.Kademe(set), Yapilandirma.Yildiz(set, kart));
                    if (agirlik > 0)
                    {
                        (Album.Var(set, kart) ? sahip : eksik).Add(((set, kart), agirlik));
                    }
                }
            }

            double eksikToplam = eksik.Sum(k => k.Agirlik);
            double sahipToplam = sahip.Sum(k => k.Agirlik);
            if (eksikToplam + sahipToplam <= 0)
            {
                // Ağırlık tablosu hiçbir kartı seçmiyor: paket boş kalmasın diye bütün kartlar eşit ağırlıkla çekilir.
                return (rastgele.Sonraki(Album.SetSayisi), rastgele.Sonraki(Album.SetBasinaKart));
            }

            double eksikOlasiligi = eksikToplam <= 0 ? 0
                : sahipToplam <= 0 ? 1
                : Math.Min(1, (eksikToplam / (eksikToplam + sahipToplam)) + (_p.Sayi(MetaParametreAdlari.AcimaKatsayisi) * ArdisikCift));

            var havuz = rastgele.SonrakiOran() < eksikOlasiligi ? eksik : sahip;
            double hedef = rastgele.SonrakiOran() * havuz.Sum(k => k.Agirlik);
            foreach (var (kart, agirlik) in havuz)
            {
                hedef -= agirlik;
                if (hedef < 0)
                {
                    return kart;
                }
            }

            return havuz[havuz.Count - 1].Kart;
        }

        /// <summary>
        /// Albüme tek bir kart ekler (gönderilen kart, GDD 9.7.9). Yeni kartsa albüme girer; çiftse yıldıza dönüşür (9.7.8).
        /// </summary>
        /// <returns>Kart yeniyse true; sezon sürmüyorsa ya da kart çiftse false.</returns>
        public bool KartAl(int set, int kart, DateTime simdi)
        {
            if (!Suruyor(simdi) || set < 0 || set >= Album.SetSayisi || kart < 0 || kart >= Album.SetBasinaKart)
            {
                return false;
            }

            if (Album.KartEkle(set, kart))
            {
                ArdisikCift = 0;
                return true;
            }

            CiftYildizi += Yapilandirma.Yildiz(set, kart) * CiftYildizCarpani();
            return false;
        }

        /// <summary>Bugün daha kart gönderilebilir mi (GDD 9.7.9; parametre: gunluk_kart_gonderme)?</summary>
        public bool KartGonderebilir(long gun) =>
            (_gonderimGunu == gun ? _bugunGonderilen : 0) < _p.TamSayi(MetaParametreAdlari.GunlukKartGonderme);

        internal void GonderimiKaydet(long gun)
        {
            _bugunGonderilen = _gonderimGunu == gun ? _bugunGonderilen + 1 : 1;
            _gonderimGunu = gun;
        }

        /// <summary>Kart istenebilir mi: son istekten bu yana parametre: kart_istek_sifirlama_saat geçmiş (GDD 9.7.9)?</summary>
        public bool KartIsteyebilir(DateTime simdi) =>
            !_sonKartIstegi.HasValue
            || simdi - _sonKartIstegi.Value >= TimeSpan.FromHours(_p.Sayi(MetaParametreAdlari.KartIstekSifirlamaSaat));

        internal void IstegiKaydet(DateTime simdi) => _sonKartIstegi = simdi;

        /// <summary>parametre: cift_kart_yildiz_degeri serbest metindir: "kartın nadirlik yıldızı × 2".</summary>
        private int CiftYildizCarpani()
        {
            string metin = _p.Metin(MetaParametreAdlari.CiftKartYildizDegeri);
            int carpi = metin.LastIndexOf('×');
            if (carpi < 0 || !int.TryParse(metin.Substring(carpi + 1).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int carpan))
            {
                throw new ParametreHatasi($"'{MetaParametreAdlari.CiftKartYildizDegeri}' \"… × sayı\" biçiminde olmalı: '{metin}'.");
            }

            return carpan;
        }

        /// <summary>
        /// Çift kart sandığı açar (GDD 9.7.8): üç kademeli fiyat çift yıldızından düşer (parametre:
        /// cift_kart_sandik_fiyatlari). Sandığın içeriği sunucudadır.
        /// </summary>
        /// <param name="kademe">0, 1 ya da 2.</param>
        public bool CiftSandigiAc(int kademe)
        {
            var fiyatlar = _p.TamSayiListesi(MetaParametreAdlari.CiftKartSandikFiyatlari);
            if (kademe < 0 || kademe >= fiyatlar.Count || CiftYildizi < fiyatlar[kademe])
            {
                return false;
            }

            CiftYildizi -= fiyatlar[kademe];
            return true;
        }

        // ------------------------------------------------------------------ Ödüller ve aşamalar

        public bool SetOduluAlinabilir(int set) => Album.SetDolu(set) && !_alinanSetOdulleri.Contains(set);

        /// <summary>Set dolunca set ödülü alınır (GDD 9.7.7).</summary>
        /// <returns>Ödül paketi (içeriği yapılandırmada yoksa boş paket); alınamıyorsa null.</returns>
        public OdulPaketi? SetOdulunuAl(int set)
        {
            if (!SetOduluAlinabilir(set))
            {
                return null;
            }

            _alinanSetOdulleri.Add(set);
            return Yapilandirma.SetOdulleri.TryGetValue(set, out var odul) ? odul : new OdulPaketi();
        }

        public bool BuyukOdulAlinabilir => Album.HepsiDolu && !BuyukOdulAlindi;

        /// <summary>Bütün setler dolunca büyük ödül alınır (GDD 9.7.7).</summary>
        public OdulPaketi? BuyukOduluAl()
        {
            if (!BuyukOdulAlinabilir)
            {
                return null;
            }

            BuyukOdulAlindi = true;
            return Yapilandirma.BuyukOdul ?? new OdulPaketi();
        }

        /// <summary>
        /// Büyük ödül alındıktan sonra sezon bir sonraki rozetli aşamaya geçer: setler boşalır (GDD 9.7.7;
        /// parametre: rozetli_asama_sayisi). Son aşamadan sonra geçilmez.
        /// </summary>
        public bool SonrakiAsama()
        {
            if (!BuyukOdulAlindi || Asama >= _p.TamSayi(MetaParametreAdlari.RozetliAsamaSayisi))
            {
                return false;
            }

            Asama++;
            AsamayiSifirla();
            return true;
        }

        private void AsamayiSifirla()
        {
            Album = new Album(Yapilandirma.SetSayisi, Yapilandirma.SetBasinaKart);
            _alinanSetOdulleri.Clear();
            BuyukOdulAlindi = false;
            ArdisikCift = 0;
        }

        /// <summary>
        /// Sezonu bitirir (GDD 9.7.11): alınmamış set ödülleri ve büyük ödül alınmış sayılır ve döndürülür; albüm yeni
        /// sezon için bekleme durumuna döner. Açılmamış paketler ve çift yıldızları sezonla birlikte biter.
        /// </summary>
        public List<OdulPaketi> Bitir()
        {
            var oduller = new List<OdulPaketi>();
            if (!Katildi)
            {
                return oduller;
            }

            for (int set = 0; set < Album.SetSayisi; set++)
            {
                if (SetOdulunuAl(set) is { } odul)
                {
                    oduller.Add(odul);
                }
            }

            if (BuyukOduluAl() is { } buyuk)
            {
                oduller.Add(buyuk);
            }

            _sonBitenKimlik = Kimlik;
            Kimlik = null;
            Asama = 1;
            AsamayiSifirla();
            _bekleyenPaketler.Clear();
            CiftYildizi = 0;
            return oduller;
        }

        // ------------------------------------------------------------------ Kayıt

        internal Dictionary<string, object?> DurumuYaz()
        {
            var paketler = new Dictionary<string, object?>();
            foreach (var cift in _bekleyenPaketler.OrderBy(c => c.Key, StringComparer.Ordinal))
            {
                paketler[cift.Key] = cift.Value;
            }

            return new Dictionary<string, object?>
            {
                ["kimlik"] = Kimlik,
                ["bitis"] = KayitAlani.Zaman(Bitis),
                ["son_biten"] = _sonBitenKimlik,
                ["asama"] = Asama,
                ["buyuk_odul_alindi"] = BuyukOdulAlindi,
                ["gorseller_indi"] = GorsellerIndi,
                ["cift_yildizi"] = CiftYildizi,
                ["ardisik_cift"] = ArdisikCift,
                ["gonderim_gunu"] = _gonderimGunu.HasValue ? (object)_gonderimGunu.Value : null,
                ["bugun_gonderilen"] = _bugunGonderilen,
                ["son_kart_istegi"] = _sonKartIstegi.HasValue ? KayitAlani.Zaman(_sonKartIstegi.Value) : null,
                ["set_sayisi"] = Album.SetSayisi,
                ["alinan_set_odulleri"] = _alinanSetOdulleri.OrderBy(s => s).Cast<object?>().ToList(),
                ["bekleyen_paketler"] = paketler,
                ["album"] = Album.DurumuYaz(),
            };
        }

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            Kimlik = d.TryGetValue("kimlik", out object? kimlik) ? kimlik as string : null;
            Bitis = KayitAlani.Zaman(d, "bitis", DateTime.MinValue);
            _sonBitenKimlik = d.TryGetValue("son_biten", out object? biten) ? biten as string : null;
            Asama = Math.Max(1, KayitAlani.TamSayi(d, "asama", 1));
            GorsellerIndi = KayitAlani.Mantik(d, "gorseller_indi", false);
            CiftYildizi = Math.Max(0, KayitAlani.TamSayi(d, "cift_yildizi", 0));
            ArdisikCift = Math.Max(0, KayitAlani.TamSayi(d, "ardisik_cift", 0));
            _gonderimGunu = d.TryGetValue("gonderim_gunu", out object? gun) && gun != null ? KayitAlani.UzunSayi(gun, "gonderim_gunu") : (long?)null;
            _bugunGonderilen = Math.Max(0, KayitAlani.TamSayi(d, "bugun_gonderilen", 0));
            _sonKartIstegi = d.TryGetValue("son_kart_istegi", out object? istek) && istek != null
                ? KayitAlani.Zaman(istek, "son_kart_istegi")
                : (DateTime?)null;

            // Sezonun set sayısı yapılandırmadan gelir; yapılandırma gelene kadar kayıttaki sayı kullanılır.
            int setSayisi = Math.Max(1, KayitAlani.TamSayi(d, "set_sayisi", Yapilandirma.SetSayisi));
            if (setSayisi != Yapilandirma.SetSayisi)
            {
                Yapilandirma = new AlbumYapilandirmasi(_p, setSayisi);
            }

            Album = new Album(Yapilandirma.SetSayisi, Yapilandirma.SetBasinaKart);
            Album.DurumuYukle(KayitAlani.Nesne(d, "album"));
            BuyukOdulAlindi = KayitAlani.Mantik(d, "buyuk_odul_alindi", false);

            _alinanSetOdulleri.Clear();
            foreach (object? set in KayitAlani.Dizi(d, "alinan_set_odulleri"))
            {
                _alinanSetOdulleri.Add(KayitAlani.TamSayi(set, "alinan_set_odulleri"));
            }

            _bekleyenPaketler.Clear();
            foreach (var cift in KayitAlani.Nesne(d, "bekleyen_paketler"))
            {
                int adet = KayitAlani.TamSayi(cift.Value, cift.Key);
                if (adet > 0) _bekleyenPaketler[cift.Key] = adet;
            }
        }
    }
}
