using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Ortak olay iskeleti (GDD 10.0). Olayların sunucuya bağlı kısımları (takvim, grup eşleştirme, sıralama,
    /// ödül miktarları) "sunucu tarafı, kendi tasarımın" olarak işaretlidir; burada yalnızca istemcinin beklediği
    /// yapı vardır. Olayları sunucu takviminden <see cref="OlayYoneticisi"/> kurar.
    /// </summary>
    public abstract class OlayIskeleti
    {
        protected OlayIskeleti(string kimlik, string acilisParametresi, DateTime bitis)
        {
            Kimlik = kimlik;
            AcilisParametresi = acilisParametresi;
            Bitis = bitis;
        }

        /// <summary>Sunucunun verdiği olay kimliği (GDD 10.0.1).</summary>
        public string Kimlik { get; }

        public abstract OlayTuru Tur { get; }

        /// <summary>Açılış bölümünü veren parametrenin adı (GDD 10.0.1, 9.4).</summary>
        public string AcilisParametresi { get; }

        /// <summary>
        /// Açılış bölümüne eklenen bölüm sayısı: aynı parametreyi paylaşan olay türleri sırayla açılır (GDD 10.9.1).
        /// </summary>
        public int AcilisKaymasi { get; set; }

        /// <summary>Olayın bitiş anı; bazı olaylarda ayrıca oyuncuya özel bir bitiş anı vardır (GDD 10.0.1).</summary>
        public DateTime Bitis { get; protected internal set; }

        public DateTime? OyuncuyaOzelBitis { get; protected set; }

        /// <summary>Sunucu her olayı ayrı ayrı kapatabilir (GDD 10.0.2).</summary>
        public bool SunucuKapatti { get; set; }

        /// <summary>Olay bağımlılığı bir olayı engelleyebilir (GDD 10.0.4, 9.4.8); hangisinin hangisini engellediği sunucudadır.</summary>
        public bool Engelli { get; set; }

        /// <summary>Süre bitince kalan toplanabilirler aynı türün bir sonraki olayına aktarılır mı (GDD 10.0.6)?</summary>
        public bool ToplanabilirAktarilir { get; set; }

        /// <summary>Çarpanla oynanan bölümün kazanımı olay puanını katlar mı (GDD 10.0.5)?</summary>
        public virtual bool Carpilabilir => true;

        /// <summary>Olay bölüm öncesi ekranında gösterilir mi? Olay türüne bağlıdır (GDD 10.0.9).</summary>
        public virtual bool BolumOncesindeGosterilir => true;

        /// <summary>Olayın açıldığı bölüm.</summary>
        public int AcilisBolumu(IlerlemeKapilari kapilar) => kapilar.AcilisBolumu(AcilisParametresi) + AcilisKaymasi;

        /// <summary>
        /// Olayın oyuncu için kapandığı an: varsa oyuncuya özel bitiş, yoksa olayın bitişi. Oyuncuya özel bitişten
        /// sonra da süren olaylar (aşamalı hız yarışı) bunu ezer.
        /// </summary>
        protected virtual DateTime EtkinBitis => OyuncuyaOzelBitis ?? Bitis;

        public bool Acik(IlerlemeKapilari kapilar, int enYuksekBolum, DateTime simdi) =>
            !SunucuKapatti && !Engelli && simdi < EtkinBitis && enYuksekBolum >= AcilisBolumu(kapilar);

        public bool SuresiDoldu(DateTime simdi) => simdi >= EtkinBitis;

        /// <summary>Bölüm kazanıldı; <paramref name="olayPuani"/> GDD 10.9.2'deki puandır (zorluk tabanı × çarpan).</summary>
        public virtual void BolumKazanildi(int olayPuani)
        {
        }

        /// <summary>
        /// Kazanılan bölümün sonucu olaya iletilir (GDD 10.0.5, 7.6.3): çarpılabilir olay çarpanlı puanı, diğerleri
        /// tabanı alır; ardından "artan hamleler bitti" bilgisi gelir. Puanı kendi kuralıyla hesaplayan olaylar ezer.
        /// </summary>
        public virtual void BolumKazanildi(BolumSonucu sonuc, DateTime simdi)
        {
            BolumKazanildi(Carpilabilir ? sonuc.OlayPuani : sonuc.OlayTabanPuani);
            ArtanHamlelerBitti(sonuc.KalanHamle);
        }

        /// <summary>Olayın ödül içerikleri; sunucudan gelir (GDD 10.0.7). Sıranın anlamı olay türüne bağlıdır.</summary>
        public virtual void OdulleriAyarla(IReadOnlyList<OdulPaketi?> oduller)
        {
        }

        /// <summary>Bölüm kaybedildi.</summary>
        /// <param name="kurtarmaBolumu">Kaybedilen bölüm bir kurtarma bölümü mü (GDD 10.5.7)?</param>
        public virtual void BolumKaybedildi(bool kurtarmaBolumu = false)
        {
        }

        /// <summary>
        /// "Artan hamleler bitti" bilgisi (GDD 7.6.3): kalan hamleyi puan olarak kullanan olaylar bunu kullanır (10.1.6).
        /// </summary>
        public virtual void ArtanHamlelerBitti(int kalanHamle)
        {
        }

        /// <summary>Alınabilir bütün ödülleri alınmış sayar ve içeriklerini döndürür (GDD 10.0.8).</summary>
        public virtual List<OdulPaketi> AlinmamislariAl() => new List<OdulPaketi>();

        /// <summary>Süre bitince bir sonraki olaya aktarılacak toplanabilir (GDD 10.0.6).</summary>
        public virtual int AktarilacakToplanabilir => 0;

        /// <summary>Önceki olaydan aktarılan toplanabilirleri ekler (GDD 10.0.6).</summary>
        public virtual void ToplanabilirEkle(int adet)
        {
        }

        internal virtual Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>();

        internal virtual void DurumuYukle(Dictionary<string, object?> d)
        {
        }
    }

    /// <summary>
    /// Basamaklı toplama olayı şablonu (GDD 10.9): bölüm kazandıkça puan toplanır, puan basamak hedeflerini doldurur,
    /// her basamak bir ödül açar. Temalar, ek mini mekanikler, süre ve ödüller kendi tasarımındır.
    /// </summary>
    public class BasamakliToplamaOlayi : OlayIskeleti
    {
        private readonly IReadOnlyList<int> _hedefler;
        private readonly HashSet<int> _alinan = new HashSet<int>();
        private IReadOnlyList<OdulPaketi?> _oduller = Array.Empty<OdulPaketi?>();

        public BasamakliToplamaOlayi(string kimlik, string acilisParametresi, DateTime bitis, IReadOnlyList<int> basamakHedefleri)
            : base(kimlik, acilisParametresi, bitis)
        {
            if (basamakHedefleri.Count == 0 || basamakHedefleri.Any(h => h < 1))
            {
                throw new ArgumentException("Basamak hedefleri boş olamaz ve her hedef en az 1 olmalı.", nameof(basamakHedefleri));
            }

            _hedefler = basamakHedefleri;
        }

        public override OlayTuru Tur => OlayTuru.BasamakliToplama;

        public IReadOnlyList<int> Hedefler => _hedefler;

        public int BasamakSayisi => _hedefler.Count;

        /// <summary>Tamamlanan basamak sayısı.</summary>
        public int Basamak { get; protected set; }

        public int BasamakPuani { get; protected set; }

        /// <summary>Olay boyunca toplanan toplanabilir.</summary>
        public int ToplamToplanan { get; protected set; }

        /// <summary>Bütün basamaklar bitince olay oyuncu için tamamlanır (GDD 10.9.3).</summary>
        public bool Tamamlandi => Basamak >= _hedefler.Count;

        /// <summary>Basamaklar sırayla dolar (GDD 10.9.3).</summary>
        public override void BolumKazanildi(int olayPuani) => PuanEkle(olayPuani);

        public override void ToplanabilirEkle(int adet) => PuanEkle(adet);

        protected void PuanEkle(int puan)
        {
            ToplamToplanan += Math.Max(0, puan);
            while (puan > 0 && !Tamamlandi)
            {
                int gereken = _hedefler[Basamak] - BasamakPuani;
                if (puan < gereken)
                {
                    BasamakPuani += puan;
                    return;
                }

                puan -= gereken;
                Basamak++;
                BasamakPuani = 0;
            }
        }

        /// <summary>Basamak ödüllerinin içerikleri, basamak sırasıyla; sunucudan gelir (GDD 10.0.7, 10.9.1).</summary>
        public override void OdulleriAyarla(IReadOnlyList<OdulPaketi?> oduller) => _oduller = oduller;

        public OdulPaketi? Odul(int basamak) => basamak >= 0 && basamak < _oduller.Count ? _oduller[basamak] : null;

        public bool OdulAlinabilir(int basamak) => basamak >= 0 && basamak < Basamak && !_alinan.Contains(basamak);

        /// <summary>Ödüller tek tek ya da toplu alınabilir (GDD 10.9.3).</summary>
        public bool OduluAl(int basamak) => basamak >= 0 && basamak < Basamak && _alinan.Add(basamak);

        public override List<OdulPaketi> AlinmamislariAl()
        {
            var oduller = new List<OdulPaketi>();
            for (int i = 0; i < Basamak; i++)
            {
                if (OduluAl(i) && Odul(i) is { } odul)
                {
                    oduller.Add(odul);
                }
            }

            return oduller;
        }

        /// <summary>
        /// Aktarılacak toplanabilir (GDD 10.0.6, 10.5.6): henüz bir basamağı doldurmamış, yani ödüle dönüşmemiş kısım.
        /// </summary>
        public override int AktarilacakToplanabilir => Tamamlandi ? 0 : BasamakPuani;

        internal override Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["basamak"] = Basamak,
            ["basamak_puani"] = BasamakPuani,
            ["toplam"] = ToplamToplanan,
            ["alinan"] = _alinan.OrderBy(b => b).Cast<object?>().ToList(),
            ["oduller"] = _oduller.Select(o => (object?)o?.DurumuYaz()).ToList(),
        };

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            Basamak = Math.Max(0, Math.Min(_hedefler.Count, KayitAlani.TamSayi(d, "basamak", 0)));
            int puan = Math.Max(0, KayitAlani.TamSayi(d, "basamak_puani", 0));
            BasamakPuani = Tamamlandi ? 0 : Math.Min(puan, _hedefler[Basamak] - 1);
            ToplamToplanan = Math.Max(0, KayitAlani.TamSayi(d, "toplam", 0));
            _alinan.Clear();
            foreach (object? b in KayitAlani.Dizi(d, "alinan"))
            {
                int basamak = KayitAlani.TamSayi(b, "alinan");
                if (basamak >= 0 && basamak < Basamak) _alinan.Add(basamak);
            }

            _oduller = KayitAlani.Dizi(d, "oduller")
                .Select(o => o is Dictionary<string, object?> paket ? OdulPaketi.Oku(paket) : null)
                .ToList();
        }
    }

    /// <summary>
    /// Risk basamakları olayı (GDD 10.5): basamaklı toplama olayına risk katmanı ekler; oyuncu bölüm kaybettikçe
    /// şans hakkı yitirir.
    ///
    /// Durumlar: kilitli → (açılış bölümü) hazır → (olaya girildi) tam haklı aktif. Son basamak bitince tamam; süre
    /// dolunca bitti ve kalan toplanabilir aktarılır.
    /// </summary>
    public sealed class RiskBasamaklariOlayi : BasamakliToplamaOlayi
    {
        private readonly int _azamiHak;
        private readonly int _azamiToplanabilir;

        public RiskBasamaklariOlayi(string kimlik, DateTime bitis, ParametreTablosu p, IReadOnlyList<int>? basamakHedefleri = null)
            : base(
                kimlik,
                MetaParametreAdlari.AcilisRiskBasamaklari,
                bitis,
                basamakHedefleri ?? p.TamSayiListesi(MetaParametreAdlari.RiskBasamakHedefleri))
        {
            _azamiHak = p.TamSayi(MetaParametreAdlari.RiskBasamakSansHakki);
            _azamiToplanabilir = p.TamSayi(MetaParametreAdlari.RiskBasamakAzamiToplanabilir);
            SansHakki = _azamiHak;
        }

        public override OlayTuru Tur => OlayTuru.RiskBasamaklari;

        public int SansHakki { get; private set; }

        /// <summary>Oyuncu olaya girdi mi? Girilmeden olay ilerlemez (GDD 10.5 durumlar, 10.5.4 madde 1).</summary>
        public bool Girildi { get; private set; }

        /// <summary>Olaya girer: şans hakları tamdır.</summary>
        public void Gir() => Girildi = true;

        /// <summary>
        /// Kazanılan miktar 10.9.2'deki puandır; toplanabilirin bir üst sınırı vardır (GDD 10.5.2;
        /// parametre: risk_basamak_azami_toplanabilir).
        /// </summary>
        public override void BolumKazanildi(int olayPuani)
        {
            if (Girildi)
            {
                PuanEkle(Math.Min(olayPuani, Math.Max(0, _azamiToplanabilir - ToplamToplanan)));
            }
        }

        public override void ToplanabilirEkle(int adet) => PuanEkle(Math.Min(adet, Math.Max(0, _azamiToplanabilir - ToplamToplanan)));

        /// <summary>Başlamış bir bölümden çıkmak isteyen oyuncu uyarılır mı (GDD 10.5.5)? Çıkış bir şans hakkı götürecekse evet.</summary>
        public bool CikistaHakGider => Girildi && !Tamamlandi && BasamakPuani > 0;

        /// <summary>
        /// Bölüm kaybı kuralı (GDD 10.5.4): (1) olay ilerlemiyorsa ya da bütün basamaklar bittiyse hiçbir şey olmaz;
        /// (2) mevcut basamakta henüz toplanabilir yoksa ceza yoktur; (3) aksi halde bir şans hakkı gider; (4) giden
        /// hak sonuncuysa yalnızca mevcut basamağın ilerlemesi sıfırlanır ve şans hakları yeniden dolar.
        /// Kurtarma bölümündeki kayıp şans hakkı götürmez (10.5.7).
        /// </summary>
        public override void BolumKaybedildi(bool kurtarmaBolumu = false)
        {
            if (kurtarmaBolumu || !CikistaHakGider)
            {
                return;
            }

            SansHakki--;
            if (SansHakki <= 0)
            {
                BasamakPuani = 0;
                SansHakki = _azamiHak;
            }
        }

        internal override Dictionary<string, object?> DurumuYaz()
        {
            var d = base.DurumuYaz();
            d["sans_hakki"] = SansHakki;
            d["girildi"] = Girildi;
            return d;
        }

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            base.DurumuYukle(d);
            SansHakki = Math.Max(1, Math.Min(_azamiHak, KayitAlani.TamSayi(d, "sans_hakki", _azamiHak)));
            Girildi = KayitAlani.Mantik(d, "girildi", false);
        }
    }

    /// <summary>Hız yarışı olayının durumları (GDD 10.1). Grup, sıralama ve ödüller sunucudadır.</summary>
    public enum HizYarisiDurumu
    {
        Kilitli,
        Hazir,
        Katiliyor,
        Yarista,
        SonListeBekleniyor,
        OdulHazir,
        Bitti,
    }

    /// <summary>Takım yarışı olayının durumları (GDD 10.3).</summary>
    public enum TakimYarisiDurumu
    {
        Kilitli,
        Takimsiz,
        Hazir,
        Yarista,
        SonSaatler,
        Bitti,
        OdulAlindi,
        OdulsuzKapandi,
    }

    /// <summary>Harita keşif olayının durumları (GDD 10.4).</summary>
    public enum HaritaKesifDurumu
    {
        Kilitli,
        Hazir,
        Aktif,
        SonSans,
        Bitti,
    }

    /// <summary>Ortaklı atış olayının durumları (GDD 10.6).</summary>
    public enum OrtakliAtisDurumu
    {
        Kilitli,
        Hazir,
        OrtakAraniyor,
        Aktif,
        SonListeBekleniyor,
        Bitti,
    }

    /// <summary>Oyuncunun takım durumları (GDD 10.7).</summary>
    public enum TakimDurumu
    {
        Kilitli,
        Takimsiz,
        IstekBekliyor,
        Uye,
        YardimciLider,
        Lider,
    }

    /// <summary>Kurtarma bölümü akışının durumları (GDD 10.2).</summary>
    public enum KurtarmaBolumuDurumu
    {
        NormalAkis,
        KurtarmaBolumu,
        PaketBekleniyor,
        YedekBolum,
    }
}
