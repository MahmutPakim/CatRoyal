using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Pencere türleri, akışa eklenme sırasıyla (GDD 10.0.3).</summary>
    public enum PencereTuru
    {
        Tanitim,
        Baslangic,
        Bilgi,
        OdulAlma,
        Otomatik,
    }

    /// <summary>Akışta sırasını bekleyen bir pencere (GDD 9.4.5, 10.0.3). İçeriği ve görünüşü sunumdadır.</summary>
    public sealed class Pencere
    {
        /// <summary>Engel tanıtımlarının önceliği: aynı bölümde özellik tanıtımından önce gösterilir (GDD 8.6.7).</summary>
        public const int EngelOnceligi = 0;

        public const int OzellikOnceligi = 1;

        public Pencere(
            string kimlik,
            PencereTuru tur,
            string kaynak,
            bool birKez = true,
            IReadOnlyList<string>? adimlar = null,
            int oncelik = OzellikOnceligi)
        {
            Kimlik = kimlik;
            Tur = tur;
            Kaynak = kaynak;
            BirKez = birKez;
            Adimlar = adimlar ?? Array.Empty<string>();
            Oncelik = oncelik;
        }

        /// <summary>Pencerenin tekil kimliği; bir kez gösterilen pencereler bu kimlikle hatırlanır.</summary>
        public string Kimlik { get; }

        public PencereTuru Tur { get; }

        /// <summary>Pencereyi isteyen özellik ya da olay; aynı kaynağın pencereleri tür sırasıyla gösterilir.</summary>
        public string Kaynak { get; }

        /// <summary>Bir kez gösterilen pencere, gösterildikten sonra yeniden kuyruğa girmez.</summary>
        public bool BirKez { get; }

        /// <summary>Tanıtım atlanabilir (GDD 9.4.5).</summary>
        public bool Atlanabilir => Tur == PencereTuru.Tanitim;

        /// <summary>Çok adımlı tanıtımın adımları (ör. sezon hattı tanıtımı, GDD 9.8.1); tek adımlı pencerede boş.</summary>
        public IReadOnlyList<string> Adimlar { get; }

        public int Oncelik { get; }

        /// <summary>İçinde bulunulan adımın sırası (0 tabanlı).</summary>
        public int Adim { get; internal set; }

        public string? GecerliAdim => Adimlar.Count == 0 ? null : Adimlar[Adim];

        /// <summary>Yeni bir engelin tanıtım penceresi (GDD 8.6.2, 8.6.7).</summary>
        public static Pencere EngelTanitimi(string mekanikTuru) =>
            new Pencere("engel_tanitimi:" + mekanikTuru, PencereTuru.Tanitim, mekanikTuru, oncelik: EngelOnceligi);
    }

    /// <summary>
    /// Pencere kuyruğu (GDD 9.4.5, 10.0.3): özellik ve olay pencereleri sırayla gösterilir. Sıra: önce engel
    /// tanıtımları (8.6.7), sonra kaynakların kuyruğa giriş sırası; bir kaynağın pencereleri tanıtım, başlangıç,
    /// bilgi, ödül alma, otomatik sırasıyla gelir. Açık pencere, sonradan eklenenler yüzünden yerini kaybetmez.
    /// </summary>
    public sealed class PencereKuyrugu
    {
        private readonly List<Kayit> _bekleyen = new List<Kayit>();
        private readonly HashSet<string> _gosterilen = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _kaynakSirasi = new Dictionary<string, int>(StringComparer.Ordinal);
        private int _sayac;

        /// <summary>O an gösterilen pencere; yoksa null.</summary>
        public Pencere? Acik { get; private set; }

        /// <summary>Sırasını bekleyen pencere sayısı (açık olan hariç).</summary>
        public int Bekleyen => _bekleyen.Count;

        public bool Gosterildi(string kimlik) => _gosterilen.Contains(kimlik);

        /// <summary>Pencereyi kuyruğa ekler.</summary>
        /// <returns>Aynı pencere zaten bekliyorsa, açıksa ya da bir kez gösterilmişse false.</returns>
        public bool Ekle(Pencere pencere)
        {
            if ((pencere.BirKez && _gosterilen.Contains(pencere.Kimlik))
                || Acik?.Kimlik == pencere.Kimlik
                || _bekleyen.Any(k => k.Pencere.Kimlik == pencere.Kimlik))
            {
                return false;
            }

            if (!_kaynakSirasi.TryGetValue(pencere.Kaynak, out int kaynakSirasi))
            {
                kaynakSirasi = _sayac;
                _kaynakSirasi.Add(pencere.Kaynak, kaynakSirasi);
            }

            _bekleyen.Add(new Kayit(pencere, kaynakSirasi, _sayac++));
            return true;
        }

        /// <summary>Açık pencere yoksa sıradakini açar.</summary>
        /// <returns>Açık pencere; kuyruk boşsa null.</returns>
        public Pencere? Ac()
        {
            if (Acik == null && _bekleyen.Count > 0)
            {
                var ilk = _bekleyen
                    .OrderBy(k => k.Pencere.Oncelik)
                    .ThenBy(k => k.KaynakSirasi)
                    .ThenBy(k => (int)k.Pencere.Tur)
                    .ThenBy(k => k.Sira)
                    .First();
                _bekleyen.Remove(ilk);
                if (_bekleyen.All(k => k.Pencere.Kaynak != ilk.Pencere.Kaynak))
                {
                    _kaynakSirasi.Remove(ilk.Pencere.Kaynak);
                }

                Acik = ilk.Pencere;
                Acik.Adim = 0;
            }

            return Acik;
        }

        /// <summary>Açık pencereyi kapatır; pencere gösterilmiş sayılır.</summary>
        public bool Kapat()
        {
            if (Acik == null)
            {
                return false;
            }

            _gosterilen.Add(Acik.Kimlik);
            Acik = null;
            return true;
        }

        /// <summary>Açık tanıtımı atlar (GDD 9.4.5); atlanan tanıtım da gösterilmiş sayılır.</summary>
        public bool Atla() => Acik != null && Acik.Atlanabilir && Kapat();

        /// <summary>Çok adımlı pencerede bir sonraki adıma geçer; son adımdan sonra pencere kapanır.</summary>
        /// <returns>Pencere hâlâ açıksa true.</returns>
        public bool AdimIlerle()
        {
            if (Acik == null)
            {
                return false;
            }

            if (Acik.Adim < Acik.Adimlar.Count - 1)
            {
                Acik.Adim++;
                return true;
            }

            Kapat();
            return false;
        }

        internal void GosterildiSay(string kimlik) => _gosterilen.Add(kimlik);

        /// <summary>Yalnızca gösterilmiş pencereler kaydedilir; bekleyenler açılışta yeniden hesaplanır.</summary>
        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["gosterilen"] = _gosterilen.OrderBy(k => k, StringComparer.Ordinal).Cast<object?>().ToList(),
        };

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _gosterilen.Clear();
            foreach (string kimlik in KayitAlani.Dizi(d, "gosterilen").OfType<string>())
            {
                _gosterilen.Add(kimlik);
            }
        }

        private sealed class Kayit
        {
            public Kayit(Pencere pencere, int kaynakSirasi, int sira)
            {
                Pencere = pencere;
                KaynakSirasi = kaynakSirasi;
                Sira = sira;
            }

            public Pencere Pencere { get; }
            public int KaynakSirasi { get; }
            public int Sira { get; }
        }
    }

    /// <summary>
    /// Özellik tanıtımları (GDD 9.4.5): açılış bölümüne ulaşılınca özelliğin tanıtım ve başlangıç pencereleri
    /// akışa eklenir. Olayların pencereleri olay yöneticisindedir (10.0.3).
    /// </summary>
    public static class OzellikTanitimlari
    {
        /// <summary>Sezon hattı tanıtımının beş adımı (GDD 9.8.1).</summary>
        public static readonly IReadOnlyList<string> SezonHattiAdimlari =
            new[] { "simge", "ilk_ucretsiz_odul", "ilerleme_cubugu", "ilk_premium_odul", "bitti" };

        /// <summary>Tanıtımı olan özelliklerin açılış parametreleri.</summary>
        private static readonly string[] Ozellikler =
        {
            MetaParametreAdlari.AcilisBaslangicTakviyeleri,
            MetaParametreAdlari.AcilisSatirAraci,
            MetaParametreAdlari.AcilisTekHucreAraci,
            MetaParametreAdlari.AcilisSutunAraci,
            MetaParametreAdlari.AcilisKaristirmaAraci,
            MetaParametreAdlari.AcilisAlanPenceresi,
            MetaParametreAdlari.AcilisGunlukOdul,
            MetaParametreAdlari.AcilisCarpanBileti,
            MetaParametreAdlari.AcilisSezonHatti,
            MetaParametreAdlari.AcilisHaftalikHat,
            MetaParametreAdlari.AcilisMagaza,
            MetaParametreAdlari.AcilisKademeliTeklif,
            MetaParametreAdlari.AcilisCarpanSecimi,
            MetaParametreAdlari.AcilisAlbum,
            MetaParametreAdlari.AcilisCiftRenkTakviyesi,
            MetaParametreAdlari.AcilisSeriTakviyesi,
            MetaParametreAdlari.AcilisDolumGostergesi,
            MetaParametreAdlari.AcilisYedekHamleHakki,
        };

        public static string TanitimKimligi(string acilisParametresi) => "tanitim:" + acilisParametresi;

        public static string BaslangicKimligi(string acilisParametresi) => "baslangic:" + acilisParametresi;

        public static string OtomatikKimligi(string acilisParametresi) => "otomatik:" + acilisParametresi;

        /// <summary>Özelliğin açılışta akışa giren pencereleri.</summary>
        private static IEnumerable<Pencere> Pencereler(string ozellik)
        {
            if (ozellik == MetaParametreAdlari.AcilisAlanPenceresi)
            {
                // GDD 9.6.6: alan penceresi belirli bir bölümde bir kez kendiliğinden açılır.
                yield return new Pencere(OtomatikKimligi(ozellik), PencereTuru.Otomatik, ozellik);
                yield break;
            }

            yield return new Pencere(
                TanitimKimligi(ozellik),
                PencereTuru.Tanitim,
                ozellik,
                adimlar: ozellik == MetaParametreAdlari.AcilisSezonHatti ? SezonHattiAdimlari : null);

            if (ozellik == MetaParametreAdlari.AcilisHaftalikHat)
            {
                // GDD 9.9.1: başlangıç penceresi ve tanıtım bir kez gösterilir.
                yield return new Pencere(BaslangicKimligi(ozellik), PencereTuru.Baslangic, ozellik);
            }
        }

        /// <summary>
        /// Açılmış ama pencereleri henüz gösterilmemiş özelliklerin pencerelerini kuyruğa ekler. Bölüm kazanılınca
        /// ve oyun açılınca çağrılır; aynı pencere iki kez eklenmez.
        /// </summary>
        /// <returns>Kuyruğa eklenen pencere sayısı.</returns>
        public static int KapilariDenetle(Oyuncu oyuncu)
        {
            int eklenen = 0;
            foreach (string ozellik in Ozellikler.OrderBy(o => oyuncu.Kapilar.AcilisBolumu(o)))
            {
                if (!oyuncu.Kapilar.Acik(ozellik, oyuncu.EnYuksekBolum))
                {
                    continue;
                }

                eklenen += Pencereler(ozellik).Count(oyuncu.Pencereler.Ekle);
            }

            return eklenen;
        }

        /// <summary>
        /// Açık olan bütün özelliklerin pencerelerini gösterilmiş sayar. Pencere kaydı olmayan eski bir kayıt
        /// yüklenince kullanılır: ilerlemiş oyuncuya eski tanıtımlar yeniden gösterilmez.
        /// </summary>
        internal static void AcikOlanlariGosterildiSay(Oyuncu oyuncu)
        {
            foreach (string ozellik in Ozellikler.Where(o => oyuncu.Kapilar.Acik(o, oyuncu.EnYuksekBolum)))
            {
                foreach (var pencere in Pencereler(ozellik))
                {
                    oyuncu.Pencereler.GosterildiSay(pencere.Kimlik);
                }
            }
        }
    }

    /// <summary>Olay simgesinin ana ekrandaki durumu (GDD 9.4.3, 9.4.4).</summary>
    public enum SimgeDurumu
    {
        Gizli,
        Kilitli,
        Acik,
    }

    /// <summary>Olay kapılarının ayrıntıları (GDD 9.4.3, 9.4.4, 9.4.6, 9.4.7).</summary>
    public static class OlayKapilari
    {
        /// <summary>
        /// Olay simgesinin durumu. Belirli bir bölümden önce hiçbir olay simgesi gösterilmez (9.4.3). Bir deney
        /// grubunda simge, açılış bölümünden en fazla parametre: kismi_kilit_onizleme bölüm önce kilitli görünür (9.4.4).
        /// </summary>
        public static SimgeDurumu Simge(
            ParametreTablosu p, IlerlemeKapilari kapilar, string acilisParametresi, int enYuksekBolum, bool kilitliOnizleme = false)
        {
            if (!kapilar.Acik(MetaParametreAdlari.AcilisOlaySimgeleri, enYuksekBolum))
            {
                return SimgeDurumu.Gizli;
            }

            int acilis = kapilar.AcilisBolumu(acilisParametresi);
            if (enYuksekBolum >= acilis)
            {
                return SimgeDurumu.Acik;
            }

            return kilitliOnizleme && enYuksekBolum >= acilis - p.TamSayi(MetaParametreAdlari.KismiKilitOnizleme)
                ? SimgeDurumu.Kilitli
                : SimgeDurumu.Gizli;
        }

        /// <summary>
        /// Bölüm öncesi ekranında gösterilecek olaylar: adayların ilk parametre: seviye_oncesi_azami_olay tanesi
        /// (GDD 9.4.6). Adaylar öncelik sırasıyla verilir.
        /// </summary>
        public static IReadOnlyList<T> BolumOncesi<T>(ParametreTablosu p, IEnumerable<T> adaylar) =>
            adaylar.Take(Math.Max(0, p.TamSayi(MetaParametreAdlari.SeviyeOncesiAzamiOlay))).ToList();

        /// <summary>
        /// Oyuncu bir olaya katılmışken ikinci bir olaya katılabilir mi (GDD 9.4.7)? İlk olayın bitimine en az
        /// parametre: ikinci_olay_asgari_sure_dk kalmış olmalıdır. Olaya özel bir katılma eşiği varsa bu kuralın
        /// yerine geçer.
        /// </summary>
        public static bool IkinciOlayaKatilabilir(ParametreTablosu p, DateTime ilkOlayinBitisi, DateTime simdi, double? olayaOzelEsikDk = null)
        {
            double esik = olayaOzelEsikDk ?? p.Sayi(MetaParametreAdlari.IkinciOlayAsgariSureDk);
            return ilkOlayinBitisi - simdi >= TimeSpan.FromMinutes(esik);
        }
    }
}
