using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Hız yarışı olayı (GDD 10.1): küçük bir grupta, kısa bir sürede en çok bölümü kazanan ödülü alır. Katılınca
    /// yarış süresince sınırsız can verilir. Grup, sıralama ve botla doldurma sunucudadır (10.1.8).
    ///
    /// Durumlar: hazır → (katıl) katılıyor → (grup atandı, sınırsız can başladı) yarışta → (oyuncu süresi doldu) son
    /// liste bekleniyor → (liste geldi ya da denemeler bitti) ödül hazır → (ödül alındı) yeni aşama varsa hazır,
    /// yoksa bitti. Katılma hatasında ya da yeterli süre yoksa hazır durumunda kalınır. "Kilitli" durumu olayın
    /// kapısından gelir (<see cref="OlayIskeleti.Acik"/>).
    ///
    /// Yorumlar: her katılım bir aşamadır (10.1.5); puan kazanılan bölüm sayısına dayandığı için çarpanla katlanmaz;
    /// ödül sırası "birincilik ödülü, 1. aşama ödülü, 2. aşama ödülü, …" biçimindedir.
    /// </summary>
    public sealed class HizYarisiOlayi : OlayIskeleti
    {
        private readonly ParametreTablosu _p;
        private IReadOnlyList<OdulPaketi?> _oduller = Array.Empty<OdulPaketi?>();
        private List<GrupUyesi> _sonListe = new List<GrupUyesi>();
        private int _gonderilecekPuan;
        private int _deneme;
        private DateTime _sonrakiDeneme;
        private bool _sonListeSunucudan;
        private string _oyuncuKimligi = string.Empty;

        public HizYarisiOlayi(string kimlik, DateTime bitis, ParametreTablosu p)
            : base(kimlik, MetaParametreAdlari.AcilisHizYarisi, bitis)
        {
            _p = p;
        }

        public override OlayTuru Tur => OlayTuru.HizYarisi;

        /// <summary>Puan kazanılan bölüm sayısına dayanır; çarpan uygulanmaz.</summary>
        public override bool Carpilabilir => false;

        /// <summary>Oyuncuya özel bitişten sonra da olay sürer: ödül alınır, yeni aşamaya katılınabilir.</summary>
        protected override DateTime EtkinBitis => Bitis;

        public HizYarisiDurumu Durum { get; private set; } = HizYarisiDurumu.Hazir;

        /// <summary>Bu olay dönemindeki katılım sayısı; içinde bulunulan aşamanın 1 tabanlı numarasıdır.</summary>
        public int KatilimSayisi { get; private set; }

        /// <summary>Oyuncunun bu aşamadaki puanı.</summary>
        public int Puan { get; private set; }

        /// <summary>Bu aşamada kazanılan bölümlerde kalan hamlelerin toplamı (GDD 10.1.6, 7.6.3).</summary>
        public int KalanHamleToplami { get; private set; }

        /// <summary>Son bilinen grup listesi, birinciden sonuncuya; oyuncunun kendi puanı güncel haliyledir.</summary>
        public IReadOnlyList<GrupUyesi> Liste => _sonListe;

        /// <summary>Son liste sunucudan mı geldi, yoksa yerel sıralama mı kullanıldı (GDD 10.1.7)?</summary>
        public bool SonListeSunucudan => _sonListeSunucudan;

        public int AzamiKatilim =>
            Math.Min(_p.TamSayi(MetaParametreAdlari.HizYarisiAzamiKatilim), _p.TamSayi(MetaParametreAdlari.HizYarisiAsamaSayisi));

        public TimeSpan AsamaSuresi => TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.HizYarisiAsamaSuresiDk));

        private string GrupAnahtari => Kimlik + "#" + KatilimSayisi;

        /// <summary>Olayın ödülleri: [0] birincilik ödülü, [n] n. aşamanın ödülü (GDD 10.1.5, 10.1.8).</summary>
        public override void OdulleriAyarla(IReadOnlyList<OdulPaketi?> oduller) => _oduller = oduller;

        /// <summary>Katılabilir mi: hazır, katılım hakkı var ve olayda yeterli süre kalmış (GDD 10.1.2, 10.1.5)?</summary>
        public bool Katilabilir(DateTime simdi) =>
            Durum == HizYarisiDurumu.Hazir
            && KatilimSayisi < AzamiKatilim
            && Bitis - simdi >= TimeSpan.FromMinutes(_p.Sayi(MetaParametreAdlari.HizYarisiAsgariKalanDk));

        /// <summary>
        /// Yarışa katılır (GDD 10.1.2, 10.1.3): sunucudan bir grup istenir; grup gelince oyuncuya özel bitiş anı
        /// başlar ve o ana kadar sınırsız can verilir. Katılma hatasında hazır durumuna dönülür.
        /// </summary>
        public bool Katil(ISunucu sunucu, Oyuncu oyuncu, DateTime simdi)
        {
            if (!Katilabilir(simdi))
            {
                return false;
            }

            Durum = HizYarisiDurumu.Katiliyor;
            _oyuncuKimligi = oyuncu.Kimlik;
            KatilimSayisi++;
            var yanit = sunucu.GrubaKatil(GrupAnahtari, oyuncu.Kimlik, _p.TamSayi(MetaParametreAdlari.HizYarisiGrupBoyutu));
            if (!yanit.Basarili)
            {
                KatilimSayisi--;
                Durum = HizYarisiDurumu.Hazir;
                return false;
            }

            var bitis = simdi + AsamaSuresi;
            OyuncuyaOzelBitis = bitis < Bitis ? bitis : Bitis;
            Puan = 0;
            KalanHamleToplami = 0;
            _gonderilecekPuan = 0;
            _deneme = 0;
            _sonListeSunucudan = false;
            ListeyiAl(yanit.Deger, oyuncu.Kimlik);

            // GDD 10.1.3: süre, kalan zamana göre dakika olarak hesaplanır.
            oyuncu.Can.SinirsizEkle(TimeSpan.FromMinutes(Math.Ceiling((OyuncuyaOzelBitis.Value - simdi).TotalMinutes)), simdi);
            Durum = HizYarisiDurumu.Yarista;
            return true;
        }

        /// <summary>
        /// Yarış sürerken kazanılan bölüm puan getirir (GDD 10.1.4): normal bölüm ve zor ve üstü etiketli bölüm ayrı
        /// puanlanır. Kalan hamle bilgisi de olaya iletilir (10.1.6).
        /// </summary>
        public override void BolumKazanildi(BolumSonucu sonuc, DateTime simdi)
        {
            if (Durum != HizYarisiDurumu.Yarista || simdi >= OyuncuyaOzelBitis)
            {
                return;
            }

            int puan = _p.TamSayi(sonuc.Etiket == ZorlukEtiketi.Normal
                ? MetaParametreAdlari.HizYarisiPuanNormal
                : MetaParametreAdlari.HizYarisiPuanZor);
            Puan += puan;
            _gonderilecekPuan += puan;
            KalanHamleToplami += sonuc.KalanHamle;
        }

        /// <summary>
        /// Yarışı ilerletir. Yarışta: bekleyen puan sunucuya gönderilir ve liste yenilenir (GDD 10.1.4); oyuncunun
        /// süresi dolunca son liste beklenir. Son liste bekleniyor: liste birkaç kez, aralıklarla istenir; gelmezse
        /// yerel sıralama kullanılır (10.1.7). Sonra ödül hazırdır.
        /// </summary>
        public void Ilerle(ISunucu sunucu, Oyuncu oyuncu, DateTime simdi)
        {
            if (Durum == HizYarisiDurumu.Yarista)
            {
                PuaniGonder(sunucu, oyuncu.Kimlik);
                if (simdi >= OyuncuyaOzelBitis)
                {
                    Durum = HizYarisiDurumu.SonListeBekleniyor;
                    _deneme = 0;
                    _sonrakiDeneme = simdi;
                }
            }

            if (Durum != HizYarisiDurumu.SonListeBekleniyor || simdi < _sonrakiDeneme)
            {
                return;
            }

            _deneme++;
            bool gonderildi = PuaniGonder(sunucu, oyuncu.Kimlik);
            var yanit = gonderildi ? sunucu.Siralama(GrupAnahtari, oyuncu.Kimlik) : SunucuYaniti<OlayGrubu>.Basarisiz();
            if (yanit.Basarili)
            {
                ListeyiAl(yanit.Deger, oyuncu.Kimlik);
                _sonListeSunucudan = true;
                Durum = HizYarisiDurumu.OdulHazir;
            }
            else if (_deneme >= _p.TamSayi(MetaParametreAdlari.SonListeDenemeSayisi))
            {
                // Liste gelmedi: yerel sıralama kullanılır.
                Durum = HizYarisiDurumu.OdulHazir;
            }
            else
            {
                _sonrakiDeneme = simdi + TimeSpan.FromSeconds(_p.Sayi(MetaParametreAdlari.SonListeDenemeAraligiSn));
            }
        }

        /// <returns>Gönderilecek puan kalmadıysa true.</returns>
        private bool PuaniGonder(ISunucu sunucu, string oyuncuKimligi)
        {
            if (_gonderilecekPuan == 0)
            {
                return true;
            }

            var yanit = sunucu.PuanGonder(GrupAnahtari, oyuncuKimligi, _gonderilecekPuan);
            if (!yanit.Basarili)
            {
                // Gönderilemeyen puan bir sonraki ilerlemede yeniden denenir.
                return false;
            }

            _gonderilecekPuan = 0;
            ListeyiAl(yanit.Deger, oyuncuKimligi);
            return true;
        }

        /// <summary>Sunucunun listesini alır; oyuncunun satırı yerel puanla tutulur ve liste yeniden sıralanır.</summary>
        private void ListeyiAl(OlayGrubu grup, string oyuncuKimligi)
        {
            _sonListe = grup.Siralama
                .Select(u => u.Kimlik == oyuncuKimligi ? new GrupUyesi(u.Kimlik, Math.Max(u.Puan, Puan), false) : u)
                .ToList();
            if (_sonListe.All(u => u.Kimlik != oyuncuKimligi))
            {
                _sonListe.Add(new GrupUyesi(oyuncuKimligi, Puan, false));
            }

            Sirala(oyuncuKimligi);
        }

        /// <summary>Yerel sıralama (GDD 10.1.7): puanı yüksek olan önde; eşitlikte oyuncu önde.</summary>
        private void Sirala(string oyuncuKimligi) =>
            _sonListe = _sonListe
                .OrderByDescending(u => u.Kimlik == oyuncuKimligi ? Math.Max(u.Puan, Puan) : u.Puan)
                .ThenBy(u => u.Kimlik == oyuncuKimligi ? 0 : 1)
                .ThenBy(u => u.Kimlik, StringComparer.Ordinal)
                .Select(u => u.Kimlik == oyuncuKimligi ? new GrupUyesi(u.Kimlik, Math.Max(u.Puan, Puan), false) : u)
                .ToList();

        /// <summary>Oyuncunun son bilinen listedeki 1 tabanlı sırası; liste yoksa 1.</summary>
        public int Sira(string oyuncuKimligi)
        {
            Sirala(oyuncuKimligi);
            int sira = _sonListe.FindIndex(u => u.Kimlik == oyuncuKimligi);
            return sira < 0 ? 1 : sira + 1;
        }

        /// <summary>
        /// Hazır ödül: birinci büyük ödülü, diğerleri aşama ödülünü alır (GDD 10.1.8 öneri). Ödül hazır değilse null.
        /// </summary>
        public OdulPaketi? HazirOdul(string oyuncuKimligi)
        {
            if (Durum != HizYarisiDurumu.OdulHazir)
            {
                return null;
            }

            int sira = Sira(oyuncuKimligi) == 1 ? 0 : KatilimSayisi;
            return (sira < _oduller.Count ? _oduller[sira] : null) ?? new OdulPaketi();
        }

        /// <summary>
        /// Ödülü alır ve oyuncuya verir (GDD 10.1.5). Katılım hakkı ve yeterli süre kaldıysa olay yeni bir aşama için
        /// hazır olur ve başlama penceresi akışa eklenir; yoksa biter.
        /// </summary>
        public bool OduluAl(Oyuncu oyuncu, DateTime simdi)
        {
            var odul = HazirOdul(oyuncu.Kimlik);
            if (odul == null)
            {
                return false;
            }

            oyuncu.OdulVer(odul, simdi);
            Durum = HizYarisiDurumu.Hazir;
            OyuncuyaOzelBitis = null;
            if (Katilabilir(simdi))
            {
                oyuncu.Pencereler.Ekle(new Pencere(
                    "hiz_yarisi_asama:" + Kimlik + ":" + (KatilimSayisi + 1), PencereTuru.Baslangic, Kimlik));
            }
            else
            {
                Durum = HizYarisiDurumu.Bitti;
            }

            return true;
        }

        /// <summary>
        /// Olay bitince (GDD 10.0.8): süren yarış yerel sıralamayla kapanır ve hazır ödül kendiliğinden verilir.
        /// </summary>
        public override List<OdulPaketi> AlinmamislariAl()
        {
            var oduller = new List<OdulPaketi>();
            if (Durum == HizYarisiDurumu.Yarista || Durum == HizYarisiDurumu.SonListeBekleniyor)
            {
                Durum = HizYarisiDurumu.OdulHazir;
            }

            if (Durum == HizYarisiDurumu.OdulHazir)
            {
                oduller.Add(HazirOdul(_oyuncuKimligi)!);
            }

            Durum = HizYarisiDurumu.Bitti;
            return oduller;
        }

        internal override Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["durum"] = Durum.ToString(),
            ["katilim"] = KatilimSayisi,
            ["puan"] = Puan,
            ["kalan_hamle"] = KalanHamleToplami,
            ["gonderilecek"] = _gonderilecekPuan,
            ["deneme"] = _deneme,
            ["sonraki_deneme"] = KayitAlani.Zaman(_sonrakiDeneme),
            ["oyuncu_bitisi"] = OyuncuyaOzelBitis.HasValue ? KayitAlani.Zaman(OyuncuyaOzelBitis.Value) : null,
            ["sunucudan"] = _sonListeSunucudan,
            ["oyuncu"] = _oyuncuKimligi,
            ["liste"] = _sonListe.Select(u => (object?)new Dictionary<string, object?>
            {
                ["kimlik"] = u.Kimlik,
                ["puan"] = u.Puan,
                ["bot"] = u.Bot,
            }).ToList(),
            ["oduller"] = _oduller.Select(o => (object?)o?.DurumuYaz()).ToList(),
        };

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            Durum = d.TryGetValue("durum", out object? durum)
                && Enum.TryParse(durum as string, out HizYarisiDurumu okunan)
                && Enum.IsDefined(typeof(HizYarisiDurumu), okunan)
                && okunan != HizYarisiDurumu.Kilitli
                    ? okunan
                    : HizYarisiDurumu.Hazir;

            // Katılma isteği yarıda kalmışsa oyuncu yeniden katılır.
            if (Durum == HizYarisiDurumu.Katiliyor)
            {
                Durum = HizYarisiDurumu.Hazir;
            }

            KatilimSayisi = Math.Max(0, KayitAlani.TamSayi(d, "katilim", 0));
            Puan = Math.Max(0, KayitAlani.TamSayi(d, "puan", 0));
            KalanHamleToplami = Math.Max(0, KayitAlani.TamSayi(d, "kalan_hamle", 0));
            _gonderilecekPuan = Math.Max(0, KayitAlani.TamSayi(d, "gonderilecek", 0));
            _deneme = Math.Max(0, KayitAlani.TamSayi(d, "deneme", 0));
            _sonrakiDeneme = KayitAlani.Zaman(d, "sonraki_deneme", DateTime.MinValue);
            OyuncuyaOzelBitis = d.TryGetValue("oyuncu_bitisi", out object? bitis) && bitis != null
                ? KayitAlani.Zaman(bitis, "oyuncu_bitisi")
                : (DateTime?)null;
            _sonListeSunucudan = KayitAlani.Mantik(d, "sunucudan", false);
            _oyuncuKimligi = d.TryGetValue("oyuncu", out object? oyuncu) && oyuncu is string kimlik ? kimlik : string.Empty;
            _sonListe = KayitAlani.Dizi(d, "liste")
                .OfType<Dictionary<string, object?>>()
                .Where(u => u.TryGetValue("kimlik", out object? k) && k is string)
                .Select(u => new GrupUyesi((string)u["kimlik"]!, KayitAlani.TamSayi(u, "puan", 0), KayitAlani.Mantik(u, "bot", false)))
                .ToList();
            _oduller = KayitAlani.Dizi(d, "oduller")
                .Select(o => o is Dictionary<string, object?> paket ? OdulPaketi.Oku(paket) : null)
                .ToList();
        }
    }
}
