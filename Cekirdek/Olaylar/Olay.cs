using System.Collections.Generic;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Olaylar
{
    /// <summary>
    /// Çekirdeğin ürettiği olay. Sunum katmanı çekirdeği yalnızca sıralı olay listesi üzerinden izler;
    /// listenin sırası kuraldır, süreler sunuma aittir (GDD 0.4).
    /// </summary>
    public abstract class Olay
    {
    }

    /// <summary>İki taş yer değiştirdi (GDD 2.3.1).</summary>
    public sealed class TaslarYerDegistirdi : Olay
    {
        public TaslarYerDegistirdi(Konum cikilan, Konum gidilen)
        {
            Cikilan = cikilan;
            Gidilen = gidilen;
        }

        public Konum Cikilan { get; }
        public Konum Gidilen { get; }
    }

    /// <summary>Geçersiz kaydırma: taşlar "yanlış hamle" animasyonuyla geri döner (GDD 2.3.4).</summary>
    public sealed class GecersizHamle : Olay
    {
        public GecersizHamle(Konum cikilan, Konum gidilen)
        {
            Cikilan = cikilan;
            Gidilen = gidilen;
        }

        public Konum Cikilan { get; }
        public Konum Gidilen { get; }
    }

    /// <summary>Kaydırılamayan taş seçildi: "imkânsız hamle" geri bildirimi (GDD 2.3.2).</summary>
    public sealed class ImkansizHamle : Olay
    {
        public ImkansizHamle(Konum cikilan, Konum gidilen)
        {
            Cikilan = cikilan;
            Gidilen = gidilen;
        }

        public Konum Cikilan { get; }
        public Konum Gidilen { get; }
    }

    /// <summary>Bir eşleşme grubu patladı (GDD 2.6, 5.5).</summary>
    public sealed class GrupPatladi : Olay
    {
        public GrupPatladi(
            EslesmeGrubu grup, Konum? dogumHucresi, IReadOnlyList<int> patlayanTasKimlikleri, bool kaskad = false)
        {
            Kaskad = kaskad;
            Grup = grup;
            DogumHucresi = dogumHucresi;
            PatlayanTasKimlikleri = patlayanTasKimlikleri;
        }

        public EslesmeGrubu Grup { get; }

        /// <summary>Oyuncu hamlesi olmadan, düşme ve doldurmayla kendiliğinden oluşan eşleşme mi (GDD 5.5)?</summary>
        public bool Kaskad { get; }

        /// <summary>Grubun taşlarının toplandığı hücre (GDD 2.7.1); özel taş üretmeyen grupta null.</summary>
        public Konum? DogumHucresi { get; }

        public IReadOnlyList<int> PatlayanTasKimlikleri { get; }
    }

    /// <summary>Bir eşleşmeden özel taş doğdu (GDD 2.5, 2.7).</summary>
    public sealed class OzelTasDogdu : Olay
    {
        public OzelTasDogdu(Konum konum, Tas tas)
        {
            Konum = konum;
            Tas = tas;
        }

        public Konum Konum { get; }
        public Tas Tas { get; }
    }

    /// <summary>Bir özel taşın nasıl etkinleştiği (GDD 3.1.1).</summary>
    public enum Tetik
    {
        /// <summary>Normal bir taşla kaydırma.</summary>
        Kaydirma,

        /// <summary>Dokunma (GDD 2.3.5).</summary>
        Dokunma,

        /// <summary>Başka bir patlamanın değmesi (GDD 3.1.2).</summary>
        Zincir,

        /// <summary>İki özel taşın birbiriyle kaydırılması (GDD 4).</summary>
        Kombinasyon,

        /// <summary>Renk küresi kombinasyonunda dönüşen taşın etkinleşmesi (GDD 4.3.7–4.3.9).</summary>
        Donusum,
    }

    /// <summary>Vuruşun kaynağı (GDD 3.1.6, 6.1.5).</summary>
    public enum VurusTuru
    {
        Cizgi,
        Bomba,
        Gudumlu,
        RenkIsini,
        TahtaTemizleme,

        /// <summary>Oyun içi araç (GDD 3.9.4).</summary>
        Arac,
    }

    /// <summary>On kombinasyon (GDD 4.2).</summary>
    public enum KombinasyonTuru
    {
        CizgiCizgi,
        BombaCizgi,
        BombaBomba,
        GudumluCizgi,
        GudumluBomba,
        GudumluGudumlu,
        KureCizgi,
        KureBomba,
        KureGudumlu,
        KureKure,
    }

    /// <summary>Güdümlü taşın taşıdığı yük (GDD 3.4.8).</summary>
    public enum GudumluYuku
    {
        Yok,
        Cizgi,
        Bomba,
    }

    /// <summary>
    /// Bir dalganın içinde sıralı olay. <see cref="Adim"/> mantıksal adımdır: aynı adımdaki olaylar
    /// aynı anda olmuş sayılır; adımların süresi sunuma aittir (GDD 0.4, 3.2.3, 3.3.2).
    /// </summary>
    public abstract class AdimliOlay : Olay
    {
        protected AdimliOlay(long adim)
        {
            Adim = adim;
        }

        public long Adim { get; }
    }

    /// <summary>Bir etkinin değdiği hücre bir vuruş aldı (GDD 3.1.6).</summary>
    public sealed class HucreVuruldu : AdimliOlay
    {
        public HucreVuruldu(Konum konum, VurusTuru tur, long adim) : base(adim)
        {
            Konum = konum;
            Tur = tur;
        }

        public Konum Konum { get; }
        public VurusTuru Tur { get; }
    }

    /// <summary>Bir taş vuruşla patladı.</summary>
    public sealed class TasPatladi : AdimliOlay
    {
        public TasPatladi(Konum konum, Tas tas, VurusTuru tur, long adim) : base(adim)
        {
            Konum = konum;
            Tas = tas;
            Tur = tur;
        }

        public Konum Konum { get; }
        public Tas Tas { get; }
        public VurusTuru Tur { get; }
    }

    /// <summary>Bir engel katman kaybetti ya da kırıldı (GDD 6.1.1).</summary>
    public sealed class EngelHasarAldi : AdimliOlay
    {
        public EngelHasarAldi(Konum konum, Tas engel, int kalanKatman, long adim) : base(adim)
        {
            Konum = konum;
            Engel = engel;
            KalanKatman = kalanKatman;
        }

        public Konum Konum { get; }
        public Tas Engel { get; }
        public int KalanKatman { get; }
        public bool Kirildi => KalanKatman == 0;
    }

    /// <summary>Bir özel taş etkinleşti ve tahtadan kalktı (GDD 3.1.7).</summary>
    public sealed class OzelTasEtkinlesti : AdimliOlay
    {
        public OzelTasEtkinlesti(Konum konum, Tas tas, Tetik tetik, long adim) : base(adim)
        {
            Konum = konum;
            Tas = tas;
            Tetik = tetik;
        }

        public Konum Konum { get; }
        public Tas Tas { get; }
        public Tetik Tetik { get; }
    }

    /// <summary>Bir kombinasyon çalıştı (GDD 4).</summary>
    public sealed class KombinasyonCalisti : AdimliOlay
    {
        public KombinasyonCalisti(KombinasyonTuru tur, Konum merkez, long adim) : base(adim)
        {
            Tur = tur;
            Merkez = merkez;
        }

        public KombinasyonTuru Tur { get; }
        public Konum Merkez { get; }
    }

    /// <summary>Bir güdümlü taş havalandı (GDD 3.4.3). Tahtada aday yoksa hedef null olur ve iniş olmaz.</summary>
    public sealed class GudumluKalkti : AdimliOlay
    {
        public GudumluKalkti(Konum kalkis, Konum? hedef, GudumluYuku yuk, long adim) : base(adim)
        {
            Kalkis = kalkis;
            Hedef = hedef;
            Yuk = yuk;
        }

        public Konum Kalkis { get; }
        public Konum? Hedef { get; }
        public GudumluYuku Yuk { get; }
    }

    /// <summary>Bir güdümlü taş hedefine indi (GDD 3.4.3).</summary>
    public sealed class GudumluIndi : AdimliOlay
    {
        public GudumluIndi(Konum kalkis, Konum hedef, GudumluYuku yuk, long adim) : base(adim)
        {
            Kalkis = kalkis;
            Hedef = hedef;
            Yuk = yuk;
        }

        public Konum Kalkis { get; }
        public Konum Hedef { get; }
        public GudumluYuku Yuk { get; }
    }

    /// <summary>Zincirle vurulan renk küresi tetiklendi; rengini AK-09'a göre seçecek (GDD 3.5.3).</summary>
    public sealed class RenkKuresiTetiklendi : AdimliOlay
    {
        public RenkKuresiTetiklendi(Konum konum, Tas kure, long adim) : base(adim)
        {
            Konum = konum;
            Kure = kure;
        }

        public Konum Konum { get; }
        public Tas Kure { get; }
    }

    /// <summary>Bir renk küresi hedef rengini seçti (GDD 3.5.1, 3.5.2).</summary>
    public sealed class RenkSecildi : AdimliOlay
    {
        public RenkSecildi(Konum kureHucresi, int renk, int hedefSayisi, long adim) : base(adim)
        {
            KureHucresi = kureHucresi;
            Renk = renk;
            HedefSayisi = hedefSayisi;
        }

        public Konum KureHucresi { get; }
        public int Renk { get; }
        public int HedefSayisi { get; }
    }

    /// <summary>Renk küresinden bir hedefe ışın gitti (GDD 3.5.4).</summary>
    public sealed class IsinGonderildi : AdimliOlay
    {
        public IsinGonderildi(Konum kureHucresi, Konum hedef, long adim) : base(adim)
        {
            KureHucresi = kureHucresi;
            Hedef = hedef;
        }

        public Konum KureHucresi { get; }
        public Konum Hedef { get; }
    }

    /// <summary>
    /// Renk küresi kombinasyonunda bir hücreye özel taş kondu: normal taş dönüştü ya da kürenin hücresine
    /// ek taş kondu (GDD 4.3.7–4.3.9). Ek taşta <see cref="EskiTas"/> null olur.
    /// </summary>
    public sealed class TasDonustu : AdimliOlay
    {
        public TasDonustu(Konum konum, Tas? eskiTas, Tas yeniTas, long adim) : base(adim)
        {
            Konum = konum;
            EskiTas = eskiTas;
            YeniTas = yeniTas;
        }

        public Konum Konum { get; }
        public Tas? EskiTas { get; }
        public Tas YeniTas { get; }
    }

    /// <summary>Renk küresi bütün renklerini temizledi ve tahtadan kalktı (GDD 3.1.7, 3.6.3).</summary>
    public sealed class RenkKuresiTukendi : AdimliOlay
    {
        public RenkKuresiTukendi(Konum konum, Tas kure, long adim) : base(adim)
        {
            Konum = konum;
            Kure = kure;
        }

        public Konum Konum { get; }
        public Tas Kure { get; }
    }

    /// <summary>
    /// Bir taş düz düşmeyle yeni hücresine indi (GDD 5.1). Taş, <see cref="BaslangicAdimi"/> adımında düşmeye
    /// başlar ve <see cref="OturmaAdimi"/> adımında oturur (5.1.7); görsel hız sunuma aittir.
    /// </summary>
    public sealed class TasDustu : Olay
    {
        public TasDustu(Tas tas, Konum nereden, Konum nereye, long baslangicAdimi, long oturmaAdimi)
        {
            Tas = tas;
            Nereden = nereden;
            Nereye = nereye;
            BaslangicAdimi = baslangicAdimi;
            OturmaAdimi = oturmaAdimi;
        }

        public Tas Tas { get; }
        public Konum Nereden { get; }
        public Konum Nereye { get; }
        public long BaslangicAdimi { get; }
        public long OturmaAdimi { get; }
    }

    /// <summary>Bir giriş hücresinden yeni taş girdi ve hedef hücresine indi (GDD 5.3.1).</summary>
    public sealed class TasGirdi : Olay
    {
        public TasGirdi(Tas tas, Konum giris, Konum nereye, long girisAdimi, long oturmaAdimi)
        {
            Tas = tas;
            Giris = giris;
            Nereye = nereye;
            GirisAdimi = girisAdimi;
            OturmaAdimi = oturmaAdimi;
        }

        public Tas Tas { get; }
        public Konum Giris { get; }
        public Konum Nereye { get; }
        public long GirisAdimi { get; }
        public long OturmaAdimi { get; }
    }

    /// <summary>Bir taş çapraz kaymayla komşu sütundaki boşluğa indi (GDD 5.2).</summary>
    public sealed class TasCaprazKaydi : Olay
    {
        public TasCaprazKaydi(Tas tas, Konum nereden, Konum nereye, long oturmaAdimi)
        {
            Tas = tas;
            Nereden = nereden;
            Nereye = nereye;
            OturmaAdimi = oturmaAdimi;
        }

        public Tas Tas { get; }
        public Konum Nereden { get; }
        public Konum Nereye { get; }
        public long OturmaAdimi { get; }
    }

    /// <summary>Karıştırmanın nasıl sonuçlandığı (GDD 5.6, AK-13).</summary>
    public enum KaristirmaSonucu
    {
        /// <summary>Olası hamle vardı; karıştırma gerekmedi.</summary>
        Gerekmedi,

        /// <summary>Taşlar yer değiştirdi (GDD 5.6.2).</summary>
        Karistirildi,

        /// <summary>Bazı taşların rengi değiştirilerek olası hamle kuruldu (GDD 5.6.6).</summary>
        RenkDegistirildi,

        /// <summary>AK-13 A: "olası hamle yok" akışı tahtayı yeniden üretti; bölüm sürüyor.</summary>
        YenidenKuruldu,

        /// <summary>AK-13 A: yeniden üretim denemeleri de başarısız; bölüm can düşürülmeden baştan başlatılmalı.</summary>
        BastanBaslatilmali,

        /// <summary>AK-13 B: bölüm ek hamle teklifi sunulmadan kaybedildi.</summary>
        Kaybedildi,
    }

    /// <summary>Tahta karıştırıldı ya da yeniden kuruldu (GDD 5.6). Sunum tahtayı yeniden okur.</summary>
    public sealed class TahtaKaristirildi : Olay
    {
        public TahtaKaristirildi(KaristirmaSonucu sonuc)
        {
            Sonuc = sonuc;
        }

        public KaristirmaSonucu Sonuc { get; }
    }

    /// <summary>
    /// Karıştırma başarısız oldu, taşlar eski yerlerine döndü ve "olası hamle yok" akışı çalıştı (GDD 7.5.1, AK-13).
    /// Sunum bilgi penceresini gösterir; akışın sonucu ardından gelen olaydadır.
    /// </summary>
    public sealed class OlasiHamleYok : Olay
    {
    }
}
