using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Ortak olay iskeleti (GDD 10.0). Olayların sunucuya bağlı kısımları (takvim, grup eşleştirme, sıralama,
    /// ödül miktarları) "sunucu tarafı, kendi tasarımın" olarak işaretlidir; burada yalnızca istemcinin beklediği
    /// yapı vardır. Bu klasördeki sınıflar iskelettir: durumlar ve geçişler tanımlı, içerik boştur.
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

        /// <summary>Açılış bölümünü veren parametrenin adı (GDD 10.0.1, 9.4).</summary>
        public string AcilisParametresi { get; }

        /// <summary>Olayın bitiş anı; bazı olaylarda ayrıca oyuncuya özel bir bitiş anı vardır (GDD 10.0.1).</summary>
        public DateTime Bitis { get; protected set; }

        public DateTime? OyuncuyaOzelBitis { get; protected set; }

        /// <summary>Sunucu her olayı ayrı ayrı kapatabilir (GDD 10.0.2).</summary>
        public bool SunucuKapatti { get; set; }

        /// <summary>Olay bağımlılığı bir olayı engelleyebilir (GDD 10.0.4, 9.4.8); hangisinin hangisini engellediği sunucudadır.</summary>
        public bool Engelli { get; set; }

        /// <summary>Çarpanla oynanan bölümün kazanımı olay puanını katlar mı (GDD 10.0.5)?</summary>
        public virtual bool Carpilabilir => true;

        public bool Acik(IlerlemeKapilari kapilar, int enYuksekBolum, DateTime simdi) =>
            !SunucuKapatti && !Engelli && simdi < (OyuncuyaOzelBitis ?? Bitis) && kapilar.Acik(AcilisParametresi, enYuksekBolum);

        public bool SuresiDoldu(DateTime simdi) => simdi >= (OyuncuyaOzelBitis ?? Bitis);

        /// <summary>Bölüm kazanıldı; <paramref name="olayPuani"/> GDD 10.9.2'deki puandır (zorluk tabanı × çarpan).</summary>
        public virtual void BolumKazanildi(int olayPuani)
        {
        }

        /// <summary>Bölüm kaybedildi.</summary>
        public virtual void BolumKaybedildi()
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

        public BasamakliToplamaOlayi(string kimlik, string acilisParametresi, DateTime bitis, IReadOnlyList<int> basamakHedefleri)
            : base(kimlik, acilisParametresi, bitis)
        {
            _hedefler = basamakHedefleri;
        }

        public int BasamakSayisi => _hedefler.Count;

        /// <summary>Tamamlanan basamak sayısı.</summary>
        public int Basamak { get; protected set; }

        public int BasamakPuani { get; protected set; }

        /// <summary>Bütün basamaklar bitince olay oyuncu için tamamlanır (GDD 10.9.3).</summary>
        public bool Tamamlandi => Basamak >= _hedefler.Count;

        /// <summary>Basamaklar sırayla dolar (GDD 10.9.3).</summary>
        public override void BolumKazanildi(int olayPuani)
        {
            int puan = olayPuani;
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

        /// <summary>Ödüller tek tek ya da toplu alınabilir (GDD 10.9.3).</summary>
        public bool OduluAl(int basamak) => basamak >= 0 && basamak < Basamak && _alinan.Add(basamak);
    }

    /// <summary>
    /// Risk basamakları olayı (GDD 10.5): basamaklı toplama olayına risk katmanı ekler; oyuncu bölüm kaybettikçe
    /// şans hakkı yitirir.
    /// </summary>
    public sealed class RiskBasamaklariOlayi : BasamakliToplamaOlayi
    {
        private readonly int _azamiHak;

        public RiskBasamaklariOlayi(string kimlik, DateTime bitis, ParametreTablosu p)
            : base(
                kimlik,
                "acilis_risk_basamaklari",
                bitis,
                p.TamSayiListesi(MetaParametreAdlari.RiskBasamakHedefleri))
        {
            _azamiHak = p.TamSayi(MetaParametreAdlari.RiskBasamakSansHakki);
            SansHakki = _azamiHak;
        }

        public int SansHakki { get; private set; }

        /// <summary>
        /// Bölüm kaybı kuralı (GDD 10.5.4): (1) bütün basamaklar bittiyse hiçbir şey olmaz; (2) mevcut basamakta
        /// henüz toplanabilir yoksa ceza yoktur; (3) aksi halde bir şans hakkı gider; (4) giden hak sonuncuysa
        /// yalnızca mevcut basamağın ilerlemesi sıfırlanır ve şans hakları yeniden dolar.
        /// </summary>
        public override void BolumKaybedildi()
        {
            if (Tamamlandi || BasamakPuani == 0)
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
