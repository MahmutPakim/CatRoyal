using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Oyuncunun meta durumu (GDD 9): can, para, envanter, kapılar, ödül hatları, çarpan, seri.</summary>
    public sealed class Oyuncu
    {
        public Oyuncu(ParametreTablosu parametreler, DateTime simdi)
        {
            Parametreler = parametreler;
            Cuzdan = new Cuzdan(parametreler);
            Can = new CanSistemi(parametreler, simdi);
            Envanter = new Envanter(parametreler);
            Magaza = new TakviyeMagazasi(parametreler, Cuzdan, Envanter);
            Kapilar = new IlerlemeKapilari(parametreler);
            GunlukOdul = new GunlukOdul(parametreler, Kapilar);
            SezonHatti = OdulHatti.Sezon(parametreler);
            HaftalikHat = OdulHatti.Haftalik(parametreler);
            Carpan = new CarpanSistemi(parametreler);
            Zorluk = new YariDinamikZorluk(parametreler);
            Alan = new AlanInsasi(parametreler);
            AlbumSezonu = new AlbumSezonu(parametreler, Kapilar);
            Olaylar = new OlayYoneticisi(this);
            Takim = new TakimUyeligi(this);
            GelenKutusu = new GelenKutusu(this);
        }

        public ParametreTablosu Parametreler { get; }

        /// <summary>Oyuncunun sunucudaki kimliği; grup ve sıralama isteklerinde kullanılır (GDD 10.1.2).</summary>
        public string Kimlik { get; set; } = "yerel";

        public Cuzdan Cuzdan { get; }
        public CanSistemi Can { get; }
        public Envanter Envanter { get; }
        public TakviyeMagazasi Magaza { get; }
        public IlerlemeKapilari Kapilar { get; }
        public GunlukOdul GunlukOdul { get; }
        public OdulHatti SezonHatti { get; private set; }
        public OdulHatti HaftalikHat { get; private set; }
        public CarpanSistemi Carpan { get; }
        public YariDinamikZorluk Zorluk { get; }
        public AlanInsasi Alan { get; }
        /// <summary>Sezonluk albüm (GDD 9.7).</summary>
        public AlbumSezonu AlbumSezonu { get; }

        /// <summary>Albümün içinde bulunulan aşamadaki kartları.</summary>
        public Album Album => AlbumSezonu.Album;

        /// <summary>Özellik ve olay pencerelerinin kuyruğu (GDD 9.4.5, 10.0.3).</summary>
        public PencereKuyrugu Pencereler { get; } = new PencereKuyrugu();

        /// <summary>Oyuncunun kazandığı kozmetikler: çerçeve, isim stili (GDD 9.8.3).</summary>
        public ISet<string> Kozmetikler { get; } = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>İçinde bulunulan sezon (GDD 9.8.11) ve hafta (9.9.7); süreleri sunucudan gelir.</summary>
        public HatDonemi SezonDonemi { get; } = new HatDonemi();

        public HatDonemi HaftaDonemi { get; } = new HatDonemi();

        /// <summary>Takım üyeliği (GDD 10.7).</summary>
        public TakimUyeligi Takim { get; }

        /// <summary>Hediye canların ve gönderilen kartların düştüğü gelen kutusu (GDD 9.1.8).</summary>
        public GelenKutusu GelenKutusu { get; }

        /// <summary>Olay yöneticisi (GDD 10.0): sunucu takviminden kurulan olaylar.</summary>
        public OlayYoneticisi Olaylar { get; }

        /// <summary>
        /// Ödüllerden gelen olay eşyaları: eşya kimliği → adet (bilye, enerji; GDD 10.9.5). Onları harcayan olaylar
        /// buradan düşer.
        /// </summary>
        public Dictionary<string, int> OlayEsyalari { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Oyuncunun ulaştığı en yüksek bölüm; kapılar buna göre değerlendirilir (GDD 9.4.2).</summary>
        public int EnYuksekBolum { get; set; } = 1;

        /// <summary>Art arda kazanılan bölüm sayısı; kayıpta sıfırlanır.</summary>
        public int KazanmaSerisi { get; private set; }

        public int Yildiz { get; set; }

        public bool SezonHattiAcik => Kapilar.Acik(MetaParametreAdlari.AcilisSezonHatti, EnYuksekBolum);

        public bool HaftalikHatAcik => Kapilar.Acik(MetaParametreAdlari.AcilisHaftalikHat, EnYuksekBolum);

        /// <summary>Bölüm öncesinde çarpan düzeyi seçilebilir mi (GDD 9.10.5)?</summary>
        public bool CarpanSecimiAcik => Kapilar.Acik(MetaParametreAdlari.AcilisCarpanSecimi, EnYuksekBolum);

        /// <summary>Yarışmalı bölümler açıldı mı (GDD 9.11.2, 9.4)?</summary>
        public bool YarismaliBolumlerAcik => Kapilar.Acik(MetaParametreAdlari.AcilisYarismaliBolumler, EnYuksekBolum);

        /// <summary>Çift renk takviyesi kazanma serisiyle kazanılır (GDD 3.6.7, 9.3.8).</summary>
        public bool CiftRenkTakviyesiHazir =>
            Kapilar.Acik(MetaParametreAdlari.AcilisCiftRenkTakviyesi, EnYuksekBolum)
            && KazanmaSerisi >= Parametreler.TamSayi(MetaParametreAdlari.CiftRenkSeriEsigi);

        /// <summary>Yedek hamle hakkı kazanma serisiyle kazanılır ve bölüm öncesinde hazırlanır (GDD 3.8.1).</summary>
        public bool YedekHamleHakkiHazir =>
            Kapilar.Acik(MetaParametreAdlari.AcilisYedekHamleHakki, EnYuksekBolum)
            && KazanmaSerisi >= Parametreler.TamSayi(MetaParametreAdlari.YedekHamleSeriEsigi);

        /// <summary>Seri takviyesinin kademesi (GDD 9.3.9): art arda kazanımlarda artar, kayıpta sıfırlanır.</summary>
        public int SeriTakviyesiKademesi =>
            Kapilar.Acik(MetaParametreAdlari.AcilisSeriTakviyesi, EnYuksekBolum)
                ? Math.Min(KazanmaSerisi, Parametreler.TamSayi(MetaParametreAdlari.SeriTakviyesiAzami))
                : 0;

        /// <summary>Premium sezon hattı satın alınır: geçmiş basamakların premium ödülleri açılır, azami can yükselir (GDD 9.8.7, 9.8.9).</summary>
        public void SezonPremiumSatinAl(DateTime simdi)
        {
            SezonHatti.PremiumSatinAl();
            Can.PremiumAyarla(true, simdi);

            // GDD 9.8.7: satın almadan sonra bir ödül gösterim penceresi açılır.
            Pencereler.Ekle(new Pencere("sezon_premium_odulleri", PencereTuru.OdulAlma, MetaParametreAdlari.AcilisSezonHatti, birKez: false));
        }

        /// <summary>
        /// Hattın bir basamağının ödülünü alır ve içeriğini oyuncuya verir (GDD 9.8.6, 9.9.5).
        /// </summary>
        /// <returns>Ödül alınabilir durumda değilse false.</returns>
        public bool HatOdulunuAl(OdulHatti hat, int basamak, bool premium, DateTime simdi)
        {
            if (!(premium ? hat.PremiumOduluAl(basamak) : hat.UcretsizOduluAl(basamak)))
            {
                return false;
            }

            var odul = premium ? hat.PremiumOdul(basamak) : hat.UcretsizOdul(basamak);
            if (odul != null)
            {
                OdulVer(odul, simdi);
            }

            return true;
        }

        /// <summary>
        /// Sezonu sunucuyla eşitler (GDD 9.8.10, 9.8.11). Sezon bittiyse ya da sunucu başka bir sezon bildiriyorsa
        /// alınmamış ödüller toplu verilir, bitiş penceresi kuyruğa girer ve yeni sezon ücretsiz hatla başlar. Oyuncu
        /// sezon değişirken oyunda değilse önceki sezondan kalan ödüller ilk eşitlemede verilir.
        /// </summary>
        /// <param name="bilgi">Sunucunun bildirdiği sezon; sunucuya ulaşılamadıysa null.</param>
        /// <param name="simdi">Sunucu saati.</param>
        /// <returns>Bu çağrıda biten sezonun sonucu; sezon bitmediyse null.</returns>
        public DonemBitisi? SezonuEsitle(DonemBilgisi? bilgi, DateTime simdi) =>
            DonemiEsitle(
                SezonDonemi, bilgi, simdi, "sezon", MetaParametreAdlari.AcilisSezonHatti,
                () => SezonHatti, hedefler =>
                {
                    SezonHatti = OdulHatti.Sezon(Parametreler, hedefler);
                    Can.PremiumAyarla(false, simdi);
                });

        /// <summary>
        /// Haftayı sunucuyla eşitler (GDD 9.9.6, 9.9.7): hafta sonunda alınmamış ödüller toplu verilir ve yeni hafta
        /// başlar. Son gün için ayrıca bir bildirim penceresi kuyruğa girer.
        /// </summary>
        public DonemBitisi? HaftayiEsitle(DonemBilgisi? bilgi, DateTime simdi)
        {
            var bitis = DonemiEsitle(
                HaftaDonemi, bilgi, simdi, "hafta", MetaParametreAdlari.AcilisHaftalikHat,
                () => HaftalikHat, hedefler => HaftalikHat = OdulHatti.Haftalik(Parametreler, hedefler));

            if (HaftalikHatAcik
                && HaftaDonemi.Suruyor(simdi)
                && !HaftaDonemi.SonGunBildirildi
                && HaftaDonemi.Kalan(simdi) <= TimeSpan.FromDays(1))
            {
                HaftaDonemi.SonGunBildirildi = true;
                Pencereler.Ekle(new Pencere(
                    "hafta_son_gun:" + HaftaDonemi.Kimlik, PencereTuru.Bilgi, MetaParametreAdlari.AcilisHaftalikHat));
            }

            return bitis;
        }

        private DonemBitisi? DonemiEsitle(
            HatDonemi donem,
            DonemBilgisi? bilgi,
            DateTime simdi,
            string ad,
            string kaynak,
            Func<OdulHatti> hat,
            Action<IReadOnlyList<int>?> yeniHat)
        {
            DonemBitisi? bitis = null;
            if (donem.Kimlik != null && (simdi >= donem.Bitis || (bilgi != null && bilgi.Kimlik != donem.Kimlik)))
            {
                var oduller = hat().AlinmamislariAl();
                foreach (var odul in oduller)
                {
                    OdulVer(odul, simdi);
                }

                bitis = new DonemBitisi(donem.Kimlik, oduller);
                Pencereler.Ekle(new Pencere(ad + "_bitti:" + donem.Kimlik, PencereTuru.Bilgi, kaynak));
                donem.SonBiten = donem.Kimlik;
                donem.Kimlik = null;
                donem.SonGunBildirildi = false;

                // Yeni dönemin tablosu sunucudan gelene kadar parametredeki tabloyla sürer.
                yeniHat(null);
            }

            if (bilgi != null && bilgi.Kimlik != donem.SonBiten && simdi < bilgi.Bitis)
            {
                if (donem.Kimlik == null)
                {
                    donem.Kimlik = bilgi.Kimlik;
                    if (bilgi.BasamakHedefleri != null && hat().Basamak == 0 && hat().BasamakPuani == 0)
                    {
                        yeniHat(bilgi.BasamakHedefleri);
                    }
                }

                donem.Bitis = bilgi.Bitis;
            }

            return bitis;
        }

        /// <summary>
        /// Albüm sezonunu sunucuyla eşitler (GDD 9.7.1, 9.7.11): süresi dolan sezonun alınmamış ödülleri verilir ve
        /// bitiş penceresi kuyruğa girer; koşullar sağlanıyorsa bildirilen sezona girilir.
        /// </summary>
        public DonemBitisi? AlbumSezonunuEsitle(DonemBilgisi? bilgi, DateTime simdi)
        {
            DonemBitisi? bitis = null;
            var album = AlbumSezonu;
            if (album.Katildi && (simdi >= album.Bitis || (bilgi != null && bilgi.Kimlik != album.Kimlik)))
            {
                string kimlik = album.Kimlik!;
                var oduller = album.Bitir();
                foreach (var odul in oduller)
                {
                    OdulVer(odul, simdi);
                }

                bitis = new DonemBitisi(kimlik, oduller);
                Pencereler.Ekle(new Pencere("album_bitti:" + kimlik, PencereTuru.Bilgi, MetaParametreAdlari.AcilisAlbum));
            }

            if (bilgi != null)
            {
                if (album.Katildi)
                {
                    album.BitisiGuncelle(bilgi.Bitis);
                }
                else
                {
                    album.Katil(bilgi.Kimlik, bilgi.Bitis, EnYuksekBolum, simdi);
                }
            }

            return bitis;
        }

        /// <summary>Sezon bitince yeni sezon ücretsiz hatla başlar (GDD 9.8 durumlar).</summary>
        public void YeniSezon(DateTime simdi)
        {
            SezonHatti = OdulHatti.Sezon(Parametreler);
            Can.PremiumAyarla(false, simdi);
        }

        public void YeniHafta() => HaftalikHat = OdulHatti.Haftalik(Parametreler);

        /// <summary>Ödül paketini oyuncuya ekler; bütün satın almalar ve ödüller aynı yolu kullanır (GDD 9.2.6, 9.3.11).</summary>
        public void OdulVer(OdulPaketi paket, DateTime simdi)
        {
            Cuzdan.Ekle(paket.Para);
            Yildiz += paket.Yildiz;
            if (paket.Can > 0)
            {
                Can.CanEkle(paket.Can, simdi);
            }

            if (paket.SinirsizCanDakika > 0)
            {
                Can.SinirsizEkle(TimeSpan.FromMinutes(paket.SinirsizCanDakika), simdi);
            }

            foreach (var cift in paket.Takviyeler)
            {
                Envanter.Ekle(cift.Key, cift.Value);
            }

            foreach (var cift in paket.TakviyeSinirsizSaniye)
            {
                Envanter.SinirsizEkle(cift.Key, cift.Value, simdi);
            }

            if (paket.CarpanDakika > 0)
            {
                Carpan.EtkinlikCarpaniEkle(Parametreler.TamSayi(MetaParametreAdlari.CarpanBiletiKati), paket.CarpanDakika, simdi);
            }

            // GDD 9.7.2: kart paketleri ödüllerden gelir; aktif albüm sezonu yoksa verilmez (9.6.5).
            foreach (var cift in paket.AlbumEsyalari)
            {
                AlbumSezonu.PaketEkle(cift.Key, cift.Value, simdi);
            }

            foreach (string kozmetik in paket.Kozmetikler)
            {
                Kozmetikler.Add(kozmetik);
            }

            foreach (var cift in paket.OlayEsyalari)
            {
                OlayEsyalari[cift.Key] = (OlayEsyalari.TryGetValue(cift.Key, out int var) ? var : 0) + Math.Max(0, cift.Value);
            }
        }

        // ------------------------------------------------------------------ Reklam ödülleri (GDD 9.13)

        /// <summary>
        /// Ödüllü reklam izlendi: can verilir (GDD 9.1.6, 9.13.1; parametre: reklam_can_odulu). Azami can aşılmaz.
        /// Reklamın gösterilmesi ve izlenip izlenmediği çekirdeğin dışındadır; bu yöntem izleme bitince çağrılır.
        /// </summary>
        public void ReklamCaniVer(DateTime simdi) =>
            Can.CanEkle(Parametreler.TamSayi(MetaParametreAdlari.ReklamCanOdulu), simdi);

        /// <summary>Ödüllü reklam izlendi: para verilir (GDD 9.13.1; parametre: reklam_para_odulu).</summary>
        public void ReklamParasiVer() => Cuzdan.Ekle(Parametreler.TamSayi(MetaParametreAdlari.ReklamParaOdulu));

        // ------------------------------------------------------------------ Diğer meta özellikler (GDD 9.15)

        /// <summary>Sosyal hesap bağlandı mı (GDD 9.15.2)?</summary>
        public bool HesapBagli { get; private set; }

        /// <summary>Sosyal hesap bağlayan oyuncuya bir kerelik para verilir (GDD 9.2.3, 9.15.2).</summary>
        /// <returns>Ödül bu çağrıda verildiyse true; hesap zaten bağlıysa false.</returns>
        public bool HesapBagla()
        {
            if (HesapBagli)
            {
                return false;
            }

            HesapBagli = true;
            Cuzdan.Ekle(Parametreler.TamSayi(MetaParametreAdlari.HesapBaglamaOdulu));
            return true;
        }

        /// <summary>Haftalık liderlik tablosu, arkadaş daveti ve görevlerin kapıları (GDD 9.15.3, 9.4).</summary>
        public bool LiderlikTablosuAcik => Kapilar.Acik(MetaParametreAdlari.AcilisLiderlikTablosu, EnYuksekBolum);

        public bool ArkadasDavetiAcik => Kapilar.Acik(MetaParametreAdlari.AcilisArkadasDaveti, EnYuksekBolum);

        public bool GorevlerAcik => Kapilar.Acik(MetaParametreAdlari.AcilisGorevler, EnYuksekBolum);

        internal void KazanimKaydet() => KazanmaSerisi++;

        /// <summary>Kayıttan yükleme (<see cref="OyuncuKaydi"/>).</summary>
        internal void TemelDurumuYukle(int kazanmaSerisi, bool hesapBagli)
        {
            KazanmaSerisi = Math.Max(0, kazanmaSerisi);
            HesapBagli = hesapBagli;
        }

        internal void KayipKaydet() => KazanmaSerisi = 0;
    }

    /// <summary>Bölüm kazanımının meta sonucu.</summary>
    public sealed class BolumSonucu
    {
        public bool Kazanildi { get; internal set; }
        public int Para { get; internal set; }
        public int Yildiz { get; internal set; }
        public int SezonHatPuani { get; internal set; }
        public int HaftalikHatPuani { get; internal set; }

        /// <summary>Basamaklı toplama olaylarına, harita keşfe ve ortaklı atışa verilen puan (GDD 9.9.4, 10.9.2).</summary>
        public int OlayPuani { get; internal set; }

        /// <summary>Olay puanının çarpansız tabanı; çarpılabilir olmayan olaylar bunu alır (GDD 10.0.5).</summary>
        public int OlayTabanPuani { get; internal set; }

        /// <summary>Bölüm kazanılınca kalan hamle; "artan hamleler bitti" bilgisiyle olaylara iletilir (GDD 7.6.3).</summary>
        public int KalanHamle { get; internal set; }

        /// <summary>Bölüm kaybedildi mi (başlamış bölümden çıkmak dahil, GDD 9.1.3)?</summary>
        public bool Kaybedildi { get; internal set; }

        /// <summary>Bölümün zorluk etiketi; puanı etikete göre veren olaylar kullanır (GDD 10.1.4).</summary>
        public ZorlukEtiketi Etiket { get; internal set; }

        public bool CanDustu { get; internal set; }
    }

    /// <summary>
    /// Bir bölümün meta akışı (GDD 9): bölüme giriş (can, çarpan işareti, takviyeler), bölüm içi satın almalar ve
    /// araçlar, bölüm sonu ödülleri. Çekirdek oturumu (<see cref="BolumOturumu"/>) kuralları yürütür; bu sınıf
    /// çevresindeki ekonomiyi bağlar.
    /// </summary>
    public sealed class BolumAkisi
    {
        private readonly Oyuncu _oyuncu;
        private readonly ZorlukEtiketTablosu _etiketler;
        private readonly EkHamleTeklifi _teklif;
        private int _carpanIsareti = 1;
        private bool _sonucIslendi;
        private bool _ilkOynayis;
        private bool _kurtarma;

        public BolumAkisi(Oyuncu oyuncu, ZorlukEtiketTablosu etiketler, int bolum)
        {
            _oyuncu = oyuncu;
            _etiketler = etiketler;
            _teklif = new EkHamleTeklifi(oyuncu.Parametreler);
            Bolum = bolum;
        }

        public int Bolum { get; }
        public BolumOturumu? Oturum { get; private set; }
        public EkHamleTeklifi Teklif => _teklif;

        /// <summary>Sunucunun işaretlediği özel durumlu bölüm mü (GDD 9.8.4, 11.2.16)?</summary>
        public bool OzelDurum { get; set; }

        /// <summary>Kurtarma bölümünde başarısızlıkta can düşmez (GDD 10.2.8).</summary>
        public bool KayiptaCanDusmez { get; set; }

        /// <summary>
        /// Bölüm bir kurtarma bölümü olarak mı oynanıyor (GDD 10.2)? Kazanılınca etikete bağlı para yerine
        /// parametre: kurtarma_kazanma_parasi verilir (10.2.4) ve normal ilerleme sürer; kayıpta can düşmez (10.2.8).
        /// </summary>
        public bool Kurtarma
        {
            get => _kurtarma;
            set
            {
                _kurtarma = value;
                KayiptaCanDusmez = value;
            }
        }

        /// <summary>
        /// Bonus bölümü mü (GDD 8.7)? Bonus bölümleri zorluk eğrisinin dışındadır: beceri ölçümüne ve zorluğa bağlı
        /// rastgeleliğe girmez (8.7.2).
        /// </summary>
        public bool Bonus { get; set; }

        /// <summary>
        /// Yarışmalı bölümde yarışma sisteminden gelen ve hamle sayısına eklenen değer (GDD 9.11.2). Yalnızca
        /// yarışmalı bölümler açıkken uygulanır; normal ilerleme bölümlerinde null kalır ve hamle sayısı bölüm
        /// verisindeki gibidir. Değerin formülü yarışma sistemindedir (11.2.20).
        /// </summary>
        public int? YarismaHamleEki { get; set; }

        /// <summary>Bu bölüm yarışmalı bölüm olarak mı oynanıyor?</summary>
        public bool Yarismali => YarismaHamleEki.HasValue && _oyuncu.YarismaliBolumlerAcik;

        public ZorlukEtiketi Etiket => _etiketler.Etiket(Bolum);

        /// <summary>Bölüm "çarpanla oynandı" olarak işaretlendiyse çarpan; değilse 1 (GDD 9.10.1).</summary>
        public int CarpanIsareti => _carpanIsareti;

        /// <summary>
        /// Bölümü başlatır. Bölüme girmek için en az bir can gerekir (GDD 9.1.2). Çarpan işareti bölüme girildiği
        /// anda konur (9.10.1). Seçilen başlangıç takviyeleri envanterden düşer ve tahtaya konur (9.3.2, 3.9.1).
        /// </summary>
        /// <returns>Can yoksa null.</returns>
        public BolumOturumu? Baslat(
            BolumVerisi veri,
            AkBayraklari bayraklar,
            ulong tohum,
            DateTime simdi,
            IEnumerable<TakviyeTuru> secilenTakviyeler,
            List<Olay> olaylar)
        {
            if (!_oyuncu.Can.BolumeGirebilir(simdi))
            {
                return null;
            }

            var p = _oyuncu.Parametreler;
            _carpanIsareti = _oyuncu.Carpan.BolumeGir(simdi, _oyuncu.CarpanSecimiAcik);
            _ilkOynayis = Bolum >= _oyuncu.EnYuksekBolum && !Bonus && !Kurtarma;

            var oturum = BolumKurucu.Kur(
                veri, bayraklar, p, tohum, olaylar, Bonus ? 0 : _oyuncu.Zorluk.RastgelelikAgirligi(Etiket),
                Yarismali ? YarismaHamleEki!.Value : 0);
            oturum.DolumGostergesiAcik = _oyuncu.Kapilar.Acik(MetaParametreAdlari.AcilisDolumGostergesi, _oyuncu.EnYuksekBolum);
            oturum.YedekHamleHakki = _oyuncu.YedekHamleHakkiHazir;

            var secilenler = secilenTakviyeler
                .Where(t => _oyuncu.Kapilar.TakviyeAcik(t, _oyuncu.EnYuksekBolum))
                .Distinct()
                .ToList();

            if (_oyuncu.CiftRenkTakviyesiHazir)
            {
                // GDD 3.6.1: bölüm çift renk modunda oynanır. 9.3.8 (öneri): seçilen başlangıç takviyeleri birleşir
                // ve bölüm başında tahtaya tek bir çift renk küresi konur (11.2.28).
                oturum.Baglam.CiftRenkModu = true;
                foreach (var tur in secilenler)
                {
                    _oyuncu.Envanter.Kullan(tur, simdi);
                }

                oturum.TakviyeKoy(TasTuru.RenkKuresi, olaylar);
            }
            else
            {
                foreach (var tur in secilenler)
                {
                    var tas = TasTuruneCevir(tur);
                    if (tas.HasValue && _oyuncu.Envanter.Kullan(tur, simdi))
                    {
                        oturum.TakviyeKoy(tas.Value, olaylar);
                    }
                }
            }

            // GDD 9.3.9: seri takviyesinin her kademesi bölüm başında tahtaya bir çizgi taşı koyar.
            for (int i = 0; i < _oyuncu.SeriTakviyesiKademesi; i++)
            {
                oturum.TakviyeKoy(TasTuru.Cizgi, olaylar);
            }

            Oturum = oturum;
            return oturum;
        }

        private static TasTuru? TasTuruneCevir(TakviyeTuru tur)
        {
            switch (tur)
            {
                case TakviyeTuru.Cizgi: return TasTuru.Cizgi;
                case TakviyeTuru.Bomba: return TasTuru.Bomba;
                case TakviyeTuru.RenkKuresi: return TasTuru.RenkKuresi;
                case TakviyeTuru.Gudumlu: return TasTuru.Gudumlu;
                default: return null;
            }
        }

        /// <summary>
        /// Oyun içi araç kullanır (GDD 9.3.4): adet bir azalır. Araçla seçilemeyen hücrede adet düşmez (3.9.4).
        /// </summary>
        public bool AracKullan(TakviyeTuru tur, Konum? konum, DateTime simdi, List<Olay> olaylar)
        {
            AracTuru? arac = tur == TakviyeTuru.TekHucre ? AracTuru.TekHucre
                : tur == TakviyeTuru.Satir ? AracTuru.Satir
                : tur == TakviyeTuru.Sutun ? AracTuru.Sutun
                : tur == TakviyeTuru.Karistirma ? AracTuru.Karistirma
                : (AracTuru?)null;
            if (!arac.HasValue
                || Oturum == null
                || !_oyuncu.Kapilar.TakviyeAcik(tur, _oyuncu.EnYuksekBolum)
                || !_oyuncu.Envanter.Kullanilabilir(tur, simdi))
            {
                return false;
            }

            if (!Oturum.AracKullan(arac.Value, konum, olaylar))
            {
                return false;
            }

            _oyuncu.Envanter.Kullan(tur, simdi);
            return true;
        }

        /// <summary>Açık ek hamle teklifinin fiyatı ve vereceği hamle (GDD 9.12.1, 9.12.2, 9.11.4).</summary>
        public int TeklifFiyati => _teklif.Fiyat(_oyuncu.Magaza.YuksekFiyatDuzeyi);

        public int TeklifHamlesi => _teklif.Hamle(_oyuncu.Zorluk.DenemeSayaci(Bolum));

        /// <summary>
        /// Ek hamle teklifini satın alır: para düşer, hamle eklenir, üst kademelerde özel taşlar da tahtaya konur
        /// (GDD 7.4.2, 9.12.3). Yetersiz bakiyede teklif açık kalır.
        /// </summary>
        public bool EkHamleSatinAl(List<Olay> olaylar)
        {
            if (Oturum == null || Oturum.Durum != BolumDurumu.EkHamleTeklifi)
            {
                return false;
            }

            if (!_oyuncu.Cuzdan.Harca(TeklifFiyati, "ek_hamle"))
            {
                return false;
            }

            var taslar = _teklif.PaketTaslari();
            Oturum.EkHamleTeklifiniKabulEt(olaylar, TeklifHamlesi);
            foreach (var tas in taslar)
            {
                Oturum.TakviyeKoy(tas, olaylar);
            }

            _teklif.AlimKaydet();
            return true;
        }

        /// <summary>
        /// Reklam izleyerek ek hamle alınır (GDD 9.12.4, 9.13.1; parametre: ek_hamle_reklam_sayisi). Para harcanmaz ve
        /// fiyat kademesi ilerlemez; bölüm aynı tahtayla sürer (7.4.3). İzleme bitince çağrılır.
        /// </summary>
        public bool ReklamlaEkHamleAl(List<Olay> olaylar)
        {
            if (Oturum == null || Oturum.Durum != BolumDurumu.EkHamleTeklifi)
            {
                return false;
            }

            Oturum.EkHamleTeklifiniKabulEt(olaylar, _oyuncu.Parametreler.TamSayi(MetaParametreAdlari.EkHamleReklamSayisi));
            return true;
        }

        /// <summary>
        /// Bölümün meta sonucunu işler. Kazanınca: para (GDD 9.2.2), yıldız (9.6.1), hat puanları (9.8.4, 9.9.3),
        /// seri ve deneme sayacı. Kaybedince: bir can düşer (7.4.4, 9.1.3), seri sıfırlanır, deneme sayacı artar.
        /// Başlamış bir bölümden çıkmak da kayıp sayılır (9.1.3): <paramref name="cikildi"/> true verilir.
        /// </summary>
        public BolumSonucu SonucuIsle(DateTime simdi, bool cikildi = false)
        {
            var sonuc = new BolumSonucu { Etiket = _etiketler.Etiket(Bolum) };
            if (Oturum == null || _sonucIslendi)
            {
                return sonuc;
            }

            var p = _oyuncu.Parametreler;
            if (!cikildi && Oturum.Durum == BolumDurumu.Kazanildi)
            {
                _sonucIslendi = true;
                sonuc.Kazanildi = true;
                sonuc.Para = Kurtarma
                    ? p.TamSayi(Tasarim.TasarimParametreAdlari.KurtarmaKazanmaParasi)
                    : EtiketOdulleri.KazanmaParasi(p, Etiket, Oturum.KalanHamle);
                sonuc.Yildiz = p.TamSayi(MetaParametreAdlari.BolumBasinaYildiz);
                _oyuncu.Cuzdan.Ekle(sonuc.Para);
                _oyuncu.Yildiz += sonuc.Yildiz;

                int taban = EtiketOdulleri.HatPuani(p, Etiket, OzelDurum);

                // GDD 9.8.5, 9.10.3: sezon hattı puanına çarpan uygulanmaz.
                if (_oyuncu.SezonHattiAcik)
                {
                    sonuc.SezonHatPuani = taban;
                    _oyuncu.SezonHatti.PuanEkle(taban);
                }

                // GDD 9.9.3, 9.10.2: haftalık hat ve basamaklı toplama olayları puanı çarpanla alır.
                sonuc.OlayTabanPuani = taban;
                sonuc.KalanHamle = Oturum.KalanHamle;
                sonuc.OlayPuani = taban * _carpanIsareti;
                if (_oyuncu.HaftalikHatAcik)
                {
                    sonuc.HaftalikHatPuani = sonuc.OlayPuani;
                    _oyuncu.HaftalikHat.PuanEkle(sonuc.OlayPuani);
                }

                _oyuncu.KazanimKaydet();
                _oyuncu.Zorluk.KazanimKaydet(Bolum, _ilkOynayis && !Yarismali);
                _teklif.Sifirla();
                if (Bolum >= _oyuncu.EnYuksekBolum)
                {
                    _oyuncu.EnYuksekBolum = Bolum + 1;

                    // GDD 9.4.5: açılış bölümüne ulaşılınca özelliğin pencereleri akışa eklenir.
                    OzellikTanitimlari.KapilariDenetle(_oyuncu);
                }

                _oyuncu.Olaylar.BolumSonucunuDagit(sonuc, Kurtarma, simdi);
                return sonuc;
            }

            if (cikildi || Oturum.Durum == BolumDurumu.Kaybedildi)
            {
                _sonucIslendi = true;
                if (!KayiptaCanDusmez)
                {
                    int once = _oyuncu.Can.Mevcut(simdi);
                    _oyuncu.Can.BolumKaybi(simdi);
                    sonuc.CanDustu = _oyuncu.Can.Mevcut(simdi) < once;
                }

                _oyuncu.KayipKaydet();
                _oyuncu.Zorluk.BasarisizlikKaydet(Bolum, _ilkOynayis && !Yarismali);
                _teklif.Sifirla();
                sonuc.Kaybedildi = true;
                _oyuncu.Olaylar.BolumSonucunuDagit(sonuc, Kurtarma, simdi);
            }

            // AK-13 A: "baştan başlatılmalı" durumunda can düşmez ve kayıp sayılmaz.
            return sonuc;
        }
    }
}
