using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Tasarim;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Sunucu
{
    /// <summary>
    /// Sunucu isteğinin yanıtı. İstek başarısız olabilir (bağlantı yok, olay kapalı); çağıran yeniden dener ya da
    /// eldeki son bilgiyle sürer (GDD 10.1.7'deki deneme kuralı buna dayanır).
    /// </summary>
    public sealed class SunucuYaniti<T>
    {
        private readonly T _deger;

        private SunucuYaniti(bool basarili, T deger)
        {
            Basarili = basarili;
            _deger = deger;
        }

        public bool Basarili { get; }

        /// <summary>Yanıtın değeri; başarısız yanıtta okunamaz.</summary>
        public T Deger => Basarili ? _deger : throw new InvalidOperationException("Başarısız sunucu yanıtının değeri yoktur.");

        public static SunucuYaniti<T> Tamam(T deger) => new SunucuYaniti<T>(true, deger);

        public static SunucuYaniti<T> Basarisiz() => new SunucuYaniti<T>(false, default!);
    }

    /// <summary>Olay türleri (GDD 10).</summary>
    public enum OlayTuru
    {
        BasamakliToplama,
        RiskBasamaklari,
        HizYarisi,
        TakimYarisi,
        HaritaKesif,
        OrtakliAtis,

        /// <summary>Hafta sonu çarpanı gibi kısa süreli etkinlik: puan formülüne yalnızca çarpan ekler (GDD 10.9.6).</summary>
        CarpanEtkinligi,
    }

    /// <summary>Sunucu takvimindeki bir olay (GDD 10.0.1, 10.0.2, 9.4.8).</summary>
    public sealed class OlayTakvimKaydi
    {
        public OlayTakvimKaydi(string kimlik, OlayTuru tur, DateTime baslangic, DateTime bitis)
        {
            if (bitis <= baslangic)
            {
                throw new ArgumentException("Olayın bitişi başlangıcından sonra olmalı.", nameof(bitis));
            }

            Kimlik = kimlik;
            Tur = tur;
            Baslangic = baslangic;
            Bitis = bitis;
        }

        /// <summary>Sunucunun verdiği olay kimliği (GDD 10.0.1).</summary>
        public string Kimlik { get; }

        public OlayTuru Tur { get; }
        public DateTime Baslangic { get; }
        public DateTime Bitis { get; }

        /// <summary>Sunucu her olayı ayrı ayrı kapatabilir (GDD 10.0.2).</summary>
        public bool Kapali { get; set; }

        /// <summary>Bu olayı engelleyen olayların kimlikleri (GDD 9.4.8, 10.0.4).</summary>
        public List<string> EngelleyenOlaylar { get; } = new List<string>();

        /// <summary>Açıkken bu olayın alınmamış ödüllerini zorla aldıran olayların kimlikleri (GDD 9.4.8, 10.0.4).</summary>
        public List<string> OdulunuZorlaAldiranOlaylar { get; } = new List<string>();

        /// <summary>Basamak tablosu (GDD 10.9.1); verilmezse olay türünün varsayılan tablosu kullanılır.</summary>
        public IReadOnlyList<int>? BasamakHedefleri { get; set; }

        /// <summary>Basamak ödülleri, basamak sırasıyla (GDD 10.0.7, 10.9.1); ödülü olmayan basamak null.</summary>
        public List<OdulPaketi?> Oduller { get; } = new List<OdulPaketi?>();

        /// <summary>
        /// Basamaklı toplama olayının tür sırası, 0 tabanlı (GDD 10.9.1): n. tür, ilk toplama olayının açılışından
        /// n × parametre: olay_turu_acilis_araligi bölüm sonra açılır.
        /// </summary>
        public int TurSirasi { get; set; }

        /// <summary>Süre bitince kalan toplanabilirler aynı türün bir sonraki olayına aktarılır mı (GDD 10.0.6)?</summary>
        public bool ToplanabilirAktarilir { get; set; }

        /// <summary>Çarpan etkinliğinin katı (GDD 10.9.6); verilmezse parametre: carpan_bileti_kati.</summary>
        public int? CarpanKati { get; set; }

        /// <summary>Harita keşif olayının haritası, ödülleri ve teklifleri (GDD 10.4); yalnız o olayda verilir.</summary>
        public OlayIskeletleri.HaritaKesifYapilandirmasi? HaritaKesif { get; set; }

        public bool Suruyor(DateTime simdi) => !Kapali && simdi >= Baslangic && simdi < Bitis;
    }

    /// <summary>
    /// Sunucu yapılandırması (GDD 0.6): istemcinin sunucudan beklediği ayarlar. Sürümlüdür; istemci sürüm
    /// değişmediyse yeniden uygulamak zorunda değildir.
    /// </summary>
    public sealed class SunucuYapilandirmasi
    {
        public int Surum { get; set; } = 1;

        /// <summary>Sunucu günlük ödülü tamamen kapatabilir (GDD 9.5.9).</summary>
        public bool GunlukOdulKapali { get; set; }

        /// <summary>Sunucu saat hilesi korumasını kapatabilir (GDD 9.5.8).</summary>
        public bool HileKorumasiAcik { get; set; } = true;

        /// <summary>Fiyat düzeyini deney grubu belirler (GDD 9.3.7).</summary>
        public bool YuksekFiyatDuzeyi { get; set; }

        /// <summary>Yayımlanmış en yüksek bölüm (GDD 8.11.1); sunucu bildirmediyse null.</summary>
        public int? YayimlananEnYuksekBolum { get; set; }

        /// <summary>Deney grubunun öne çektiği kapılar: açılış parametresi → bölüm (GDD 9.4.1).</summary>
        public Dictionary<string, int> OneCekilenKapilar { get; } = new Dictionary<string, int>();

        /// <summary>Süren sezon (GDD 9.8.11); sunucu bildirmediyse null.</summary>
        public DonemBilgisi? Sezon { get; set; }

        /// <summary>Süren hafta (GDD 9.9.7); sunucu bildirmediyse null.</summary>
        public DonemBilgisi? Hafta { get; set; }

        /// <summary>Süren albüm sezonu (GDD 9.7.1); sunucu bildirmediyse null.</summary>
        public DonemBilgisi? AlbumSezonu { get; set; }

        /// <summary>
        /// Deney grubunda ilk kurtarma bölümlerinin çekildiği ilerleme bölümleri (GDD 10.2.6); boşsa olağan ritim.
        /// </summary>
        public List<int> OneCekilenKurtarmaBolumleri { get; } = new List<int>();

        /// <summary>Sunucunun değiştirdiği parametre değerleri: ad → değer.</summary>
        public Dictionary<string, string> ParametreDegerleri { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Sunucunun değiştirdiği parametreleri tabloya uygular. Oyuncu kurulmadan önce çağrılır; tabloda olmayan
        /// adlar atlanır (eski istemci yeni parametreyi tanımaz).
        /// </summary>
        public ParametreTablosu ParametreleriUygula(ParametreTablosu tablo)
        {
            foreach (var cift in ParametreDegerleri)
            {
                if (tablo.Var(cift.Key))
                {
                    tablo = tablo.Degistir(cift.Key, cift.Value);
                }
            }

            return tablo;
        }
    }

    /// <summary>Yarış grubundaki bir katılımcı (GDD 10.1.2, 10.3.2).</summary>
    public sealed class GrupUyesi
    {
        public GrupUyesi(string kimlik, int puan, bool bot)
        {
            Kimlik = kimlik;
            Puan = puan;
            Bot = bot;
        }

        public string Kimlik { get; }
        public int Puan { get; }

        /// <summary>Sunucu grubu botla doldurabilir (GDD 10.1.8).</summary>
        public bool Bot { get; }
    }

    /// <summary>Grup ve sıralaması; üyeler birinciden sonuncuya sıralıdır (GDD 10.1.4, 10.1.8).</summary>
    public sealed class OlayGrubu
    {
        public OlayGrubu(string grupKimligi, IReadOnlyList<GrupUyesi> siralama)
        {
            GrupKimligi = grupKimligi;
            Siralama = siralama;
        }

        public string GrupKimligi { get; }
        public IReadOnlyList<GrupUyesi> Siralama { get; }

        /// <summary>Katılımcının 1 tabanlı sırası; grupta yoksa 0.</summary>
        public int Sira(string kimlik)
        {
            for (int i = 0; i < Siralama.Count; i++)
            {
                if (Siralama[i].Kimlik == kimlik)
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// İstemcinin sunucudan beklediği veri (GDD 0.6): zaman, yapılandırma, zorluk etiketi tablosu, olay takvimi,
    /// grup ve sıralama. Mantığı sunucuda çalışan kısımların ("sunucu tarafı, kendi tasarımın") tek kapısıdır.
    ///
    /// Yöntemler eşzamanlıdır ve başarısız olabilir. Gerçek uygulama ağ isteğini kendi içinde yürütür; çekirdek
    /// yalnızca yanıtı ve başarısızlığı bilir.
    /// </summary>
    public interface ISunucu : ISaat
    {
        SunucuYaniti<SunucuYapilandirmasi> Yapilandirma();

        /// <summary>Sunucunun değiştirdiği zorluk etiketleri: bölüm → etiket (GDD 8.8.1a).</summary>
        SunucuYaniti<IReadOnlyDictionary<int, ZorlukEtiketi>> EtiketDegisiklikleri();

        /// <summary>Olay takvimi (GDD 10.0.1, 10.0.2): geçmiş, süren ve yaklaşan olaylar.</summary>
        SunucuYaniti<IReadOnlyList<OlayTakvimKaydi>> OlayTakvimi();

        /// <summary>Sunucudaki bölüm paketleri: paket numarası → sürüm (GDD 8.11.1).</summary>
        SunucuYaniti<IReadOnlyDictionary<int, int>> PaketSurumleri();

        /// <summary>Bölüm paketini indirir (GDD 8.11.2).</summary>
        SunucuYaniti<BolumPaketiVerisi> PaketIndir(int paket);

        /// <summary>
        /// Oyuncuyu olayın bir grubuna koyar (GDD 10.1.2). Aynı oyuncu yeniden isterse aynı grubu alır. Olay kimliğine
        /// "#n" eklenirse aynı olayın n. katılımı için ayrı bir grup istenir (aşamalı olaylar, 10.1.5).
        /// </summary>
        SunucuYaniti<OlayGrubu> GrubaKatil(string olayKimligi, string oyuncuKimligi, int grupBoyutu);

        /// <summary>Oyuncunun kazandığı puanı ekler ve güncel listeyi döndürür (GDD 10.1.4).</summary>
        SunucuYaniti<OlayGrubu> PuanGonder(string olayKimligi, string oyuncuKimligi, int puan);

        /// <summary>Grubun güncel sıralaması; olay bittikten sonra son listedir (GDD 10.1.7).</summary>
        SunucuYaniti<OlayGrubu> Siralama(string olayKimligi, string oyuncuKimligi);
    }

    /// <summary>Sunucudan gelen ayarları oyuncuya ve etiket tablosuna uygular.</summary>
    public static class SunucuEsitleyici
    {
        /// <summary>
        /// Yapılandırmayı ve etiket değişikliklerini uygular: günlük ödülün kapatılması ve hile koruması
        /// (GDD 9.5.8, 9.5.9), fiyat düzeyi (9.3.7), öne çekilen kapılar (9.4.1), zorluk etiketleri (8.8.1a), sezon,
        /// hafta ve albüm sezonu dönemleri (9.7.1, 9.8.11, 9.9.7).
        /// </summary>
        /// <returns>Sunucuya ulaşılamadıysa false; o durumda hiçbir şey değişmez.</returns>
        /// <param name="sunucu">Sunucu.</param>
        /// <param name="oyuncu">Eşitlenen oyuncu.</param>
        /// <param name="etiketler">Sunucu değişikliklerinin uygulanacağı zorluk etiketi tablosu.</param>
        /// <param name="sosyal">Sosyal sunucu; verilirse takım durumu ve gelen kutusu da eşitlenir (GDD 10.7, 9.1.8).</param>
        public static bool Uygula(ISunucu sunucu, Oyuncu oyuncu, ZorlukEtiketTablosu etiketler, ISosyalSunucu? sosyal = null)
        {
            var yapilandirma = sunucu.Yapilandirma();
            var degisiklikler = sunucu.EtiketDegisiklikleri();
            if (!yapilandirma.Basarili || !degisiklikler.Basarili)
            {
                return false;
            }

            var y = yapilandirma.Deger;
            oyuncu.GunlukOdul.SunucuKapatti = y.GunlukOdulKapali;
            oyuncu.GunlukOdul.HileKorumasiAcik = y.HileKorumasiAcik;
            oyuncu.Magaza.YuksekFiyatDuzeyi = y.YuksekFiyatDuzeyi;
            foreach (var cift in y.OneCekilenKapilar)
            {
                oyuncu.Kapilar.OneCek(cift.Key, cift.Value);
            }

            foreach (var cift in degisiklikler.Deger)
            {
                etiketler.Degistir(cift.Key, cift.Value);
            }

            // Dönemler sunucu saatiyle eşitlenir; biten dönemlerin ödülleri verilir ve pencereleri kuyruğa girer.
            var simdi = sunucu.SimdiUtc;
            oyuncu.SezonuEsitle(y.Sezon, simdi);
            oyuncu.HaftayiEsitle(y.Hafta, simdi);
            oyuncu.AlbumSezonunuEsitle(y.AlbumSezonu, simdi);

            // Olay takvimi (GDD 10.0.1): alınamadıysa kurulu olaylar sürer, yalnızca süresi dolanlar biter.
            var takvim = sunucu.OlayTakvimi();
            oyuncu.Olaylar.TakvimiUygula(takvim.Basarili ? takvim.Deger : null, simdi);
            if (sosyal != null)
            {
                oyuncu.Takim.Esitle(sosyal);
                oyuncu.GelenKutusu.Esitle(sosyal);
            }

            oyuncu.Olaylar.Ilerle(sunucu, simdi, sosyal);

            return true;
        }
    }
}
