using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.OlayIskeletleri;

namespace CatRoyal.Cekirdek.Sunucu
{
    /// <summary>Takımın etkinlik düzeyi (GDD 10.7.5).</summary>
    public enum EtkinlikDuzeyi
    {
        Dusuk,
        Orta,
        Yuksek,
    }

    /// <summary>Takım listelerinde görünen özet (GDD 10.7.1, 10.7.2, 10.7.5).</summary>
    public class TakimOzeti
    {
        public TakimOzeti(string kimlik, string ad, string simge, bool acik, int uyeSayisi, EtkinlikDuzeyi etkinlik)
        {
            Kimlik = kimlik;
            Ad = ad;
            Simge = simge;
            Acik = acik;
            UyeSayisi = uyeSayisi;
            Etkinlik = etkinlik;
        }

        public string Kimlik { get; }
        public string Ad { get; }
        public string Simge { get; }

        /// <summary>Açık takıma doğrudan katılınır; kapalı takıma istek gönderilir (GDD 10.7.2).</summary>
        public bool Acik { get; }

        public int UyeSayisi { get; }
        public EtkinlikDuzeyi Etkinlik { get; }
    }

    public sealed class TakimUyesi
    {
        public TakimUyesi(string kimlik, TakimDurumu rol)
        {
            Kimlik = kimlik;
            Rol = rol;
        }

        public string Kimlik { get; }

        /// <summary>Üye, yardımcı lider ya da lider (GDD 10.7.4).</summary>
        public TakimDurumu Rol { get; }
    }

    /// <summary>Takımın üyeleri ve bekleyen katılma istekleriyle birlikte tam bilgisi.</summary>
    public sealed class TakimBilgisi : TakimOzeti
    {
        public TakimBilgisi(
            string kimlik, string ad, string simge, bool acik, EtkinlikDuzeyi etkinlik,
            IReadOnlyList<TakimUyesi> uyeler, IReadOnlyList<string> bekleyenIstekler)
            : base(kimlik, ad, simge, acik, uyeler.Count, etkinlik)
        {
            Uyeler = uyeler;
            BekleyenIstekler = bekleyenIstekler;
        }

        public IReadOnlyList<TakimUyesi> Uyeler { get; }
        public IReadOnlyList<string> BekleyenIstekler { get; }
    }

    /// <summary>Oyuncunun sunucudaki takım durumu: takımsız, istek bekliyor, üye, yardımcı lider ya da lider.</summary>
    public sealed class OyuncuTakimDurumu
    {
        public OyuncuTakimDurumu(TakimDurumu durum, TakimBilgisi? takim)
        {
            Durum = durum;
            Takim = takim;
        }

        public TakimDurumu Durum { get; }

        /// <summary>Üyesi olunan ya da istek gönderilen takım; takımsızken null.</summary>
        public TakimBilgisi? Takim { get; }
    }

    public enum SohbetMesajiTuru
    {
        Metin,
        CanIstegi,
        KartIstegi,

        /// <summary>Takım sohbetine gönderilmiş, ilk isteyenin alacağı kart (GDD 9.7.9).</summary>
        Kart,
    }

    /// <summary>Takım sohbetindeki bir mesaj (GDD 10.7.3, 10.7.7).</summary>
    public sealed class SohbetMesaji
    {
        public SohbetMesaji(long kimlik, string gonderen, SohbetMesajiTuru tur, DateTime zaman, string metin = "")
        {
            Kimlik = kimlik;
            Gonderen = gonderen;
            Tur = tur;
            Zaman = zaman;
            Metin = metin;
        }

        public long Kimlik { get; }
        public string Gonderen { get; }
        public SohbetMesajiTuru Tur { get; }
        public DateTime Zaman { get; }
        public string Metin { get; }

        /// <summary>Can isteğinde: isteğin daha kaç yardım alabileceği (GDD 10.7.3). Kart mesajında: kart alındıysa 0.</summary>
        public int Kalan { get; set; }

        /// <summary>Kart ve kart isteği mesajlarında kartın yeri.</summary>
        public string? Sezon { get; set; }

        public int Set { get; set; }
        public int Kart { get; set; }
    }

    /// <summary>Gelen kutusuna düşen bir hediye (GDD 9.1.8): can, ödül paketi ya da albüm kartı.</summary>
    public sealed class GelenKutusuOgesi
    {
        public GelenKutusuOgesi(string kimlik, string gonderen, OdulPaketi paket)
        {
            Kimlik = kimlik;
            Gonderen = gonderen;
            Paket = paket;
        }

        public string Kimlik { get; }
        public string Gonderen { get; }
        public OdulPaketi Paket { get; }

        /// <summary>Öğe bir albüm kartıysa sezonu ve yeri (GDD 9.7.9); değilse sezon null.</summary>
        public string? KartSezonu { get; set; }

        public int KartSeti { get; set; }
        public int KartNo { get; set; }
    }

    /// <summary>Ortaklı atış olayında oyuncunun ortakları ve puanları (GDD 10.6.1, 10.6.3).</summary>
    public sealed class OrtakGrubu
    {
        public OrtakGrubu(IReadOnlyList<GrupUyesi> ortaklar)
        {
            Ortaklar = ortaklar;
        }

        /// <summary>Oyuncunun kendisi hariç ortaklar.</summary>
        public IReadOnlyList<GrupUyesi> Ortaklar { get; }
    }

    /// <summary>
    /// Sosyal katmanın sunucudan beklediği işlemler (GDD 10.7, 9.7.9, 10.3.5, 10.6): takımlar, sohbet, can ve kart
    /// yardımlaşması, gelen kutusu, ortak eşleştirme. Üye sınırı, istek bekleme süresi, katılma koşulu, öneri
    /// algoritması ve puan doğrulama sunucudadır (10.7.8, 10.6.6); istemci yalnızca sonucu bilir. Kural gereği
    /// reddedilen istek de başarısız yanıt döner.
    /// </summary>
    public interface ISosyalSunucu
    {
        SunucuYaniti<TakimBilgisi> TakimKur(string oyuncu, string ad, string simge, bool acik);

        /// <summary>Takım arar; <paramref name="metin"/> null ise önerilen takımları verir (GDD 10.7.1).</summary>
        SunucuYaniti<IReadOnlyList<TakimOzeti>> TakimAra(string oyuncu, string? metin);

        /// <summary>Açık takıma katılır ya da kapalı takıma istek gönderir (GDD 10.7.2).</summary>
        SunucuYaniti<OyuncuTakimDurumu> TakimaKatil(string oyuncu, string takim);

        SunucuYaniti<OyuncuTakimDurumu> TakimDurumu(string oyuncu);

        SunucuYaniti<bool> IstegiIptalEt(string oyuncu);

        /// <summary>Lider ya da yardımcı lider katılma isteğini onaylar ya da reddeder (GDD 10.7.2).</summary>
        SunucuYaniti<bool> IstegiYanitla(string yetkili, string istekSahibi, bool onay);

        SunucuYaniti<bool> TakimdanAyril(string oyuncu);

        /// <summary>Liderler ve yardımcı liderler üye atabilir (GDD 10.7.4).</summary>
        SunucuYaniti<bool> UyeAt(string yetkili, string hedef);

        /// <summary>Lider bir üyeye yardımcı liderlik yetkisi verir ya da yetkisini alır (GDD 10.7.4).</summary>
        SunucuYaniti<bool> YetkiVer(string lider, string hedef, bool yardimciLider);

        SunucuYaniti<bool> LiderligiDevret(string lider, string hedef);

        SunucuYaniti<bool> SohbetGonder(string oyuncu, string metin);

        SunucuYaniti<IReadOnlyList<SohbetMesaji>> Sohbet(string oyuncu);

        /// <summary>Takımdan can ister (GDD 10.7.3); istek aralığı ve yardım meni sunucudadır (9.1.10).</summary>
        SunucuYaniti<bool> CanIste(string oyuncu);

        /// <summary>Bir can isteğine yardım eder; isteyenin gelen kutusuna can gider (GDD 10.7.3).</summary>
        SunucuYaniti<bool> CanYardimEt(string yardimEden, long mesaj);

        /// <summary>
        /// Oyuncunun bekleyen hediyelerinden en fazla <paramref name="azami"/> tanesini teslim eder; teslim edilenler
        /// sunucudan silinir (GDD 9.1.8).
        /// </summary>
        SunucuYaniti<IReadOnlyList<GelenKutusuOgesi>> GelenKutusu(string oyuncu, int azami);

        /// <summary>
        /// Kart gönderir (GDD 9.7.9): <paramref name="alici"/> bir arkadaş ya da takım üyesidir; null ise kart takım
        /// sohbetine gider ve ilk isteyen alır.
        /// </summary>
        SunucuYaniti<bool> KartGonder(string gonderen, string? alici, string sezon, int set, int kart);

        /// <summary>Takım sohbetinden kart ister (GDD 9.7.9).</summary>
        SunucuYaniti<bool> KartIste(string oyuncu, string sezon, int set, int kart);

        /// <summary>Takım sohbetine gönderilmiş kartı alır; kart oyuncunun gelen kutusuna düşer.</summary>
        SunucuYaniti<bool> SohbettenKartAl(string oyuncu, long mesaj);

        /// <summary>Takım teklifinden satın alınan paket takım üyelerine de ödül dağıtır (GDD 10.7.6).</summary>
        SunucuYaniti<bool> TakimTeklifiDagit(string oyuncu, OdulPaketi uyeOdulu);

        /// <summary>Takım yarışının sonucu kapatılır; ödülü hak etmeyen oyuncu için "ödülsüz kapat" bildirilir (GDD 10.3.5).</summary>
        SunucuYaniti<bool> TakimYarisiniKapat(string olay, string oyuncu, bool odulAlindi);

        /// <summary>Ortaklı atış olayına ortak davet eder (GDD 10.6.1); ortak eşleştirme sunucudadır (10.6.6).</summary>
        SunucuYaniti<OrtakGrubu> OrtakDavetEt(string olay, string oyuncu, string ortak);

        /// <summary>Oyuncunun atış puanını ekler ve ortakların güncel puanlarını verir (GDD 10.6.3).</summary>
        SunucuYaniti<OrtakGrubu> OrtakPuaniGonder(string olay, string oyuncu, int puan);
    }
}
