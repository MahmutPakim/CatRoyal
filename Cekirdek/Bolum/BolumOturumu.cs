using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Etkiler;
using CatRoyal.Cekirdek.Hamle;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Bolum
{
    public enum BolumDurumu
    {
        /// <summary>Hamle kabul edilir (GDD 2.3.7).</summary>
        Oynaniyor,

        /// <summary>Hamleler bitti; ek hamle teklifi açık (GDD 7.4, AK-02).</summary>
        EkHamleTeklifi,

        /// <summary>Bütün hedefler tamamlandı (GDD 7.3).</summary>
        Kazanildi,

        /// <summary>Bölüm kaybedildi (GDD 7.4.3, AK-13 B).</summary>
        Kaybedildi,

        /// <summary>AK-13 A: bölüm can düşürülmeden baştan başlatılmalı.</summary>
        BastanBaslatilmali,
    }

    /// <summary>
    /// Çok ekranlı bölümde bir sonraki ekranın tahtasını kurar (GDD 8.9). Rastgele renklendirilecek hücreleri
    /// <paramref name="rastgeleHucreler"/> listesine ekler; taş kimlikleri <paramref name="onceki"/> tahtadan sürer.
    /// </summary>
    public delegate Tahta EkranKurucu(int ekran, Tahta onceki, List<Konum> rastgeleHucreler);

    /// <summary>
    /// Bir bölümün oynanışı: hamle sayacı (GDD 7.2), hedefler (7.1), kazanma (7.3), hamle bitti akışı (7.4, AK-02),
    /// "olası hamle yok" akışı (7.5), dolum göstergesi (3.7), yedek hamle hakkı (3.8), oyun içi araçlar ve
    /// başlangıç takviyeleri (3.9). Fiyatlar, can ve envanter meta katmandadır (9).
    /// </summary>
    public sealed class BolumOturumu
    {
        public BolumOturumu(OyunBaglami baglam, int hamleSayisi, IEnumerable<Hedef> hedefler)
        {
            if (hamleSayisi < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hamleSayisi), "Hamle sayısı negatif olamaz.");
            }

            Baglam = baglam;
            KalanHamle = hamleSayisi;
            Hedefler = new HedefTakibi(hedefler, baglam.Tahta);
            baglam.Hedefler = Hedefler;
        }

        public OyunBaglami Baglam { get; }
        public HedefTakibi Hedefler { get; }
        public BolumDurumu Durum { get; private set; } = BolumDurumu.Oynaniyor;

        /// <summary>Kalan hamle (GDD 7.2.5: bölüm verisinden gelir).</summary>
        public int KalanHamle { get; private set; }

        public int YapilanHamle { get; private set; }

        /// <summary>Oyuncunun kullanılmamış yedek hamle hakkı var mı (GDD 3.8.1)? Bölüm öncesinde hazırlanır.</summary>
        public bool YedekHamleHakki { get; set; }

        /// <summary>Dolum göstergesi bu bölümde açık mı (GDD 3.7.1; kapı meta katmanda)?</summary>
        public bool DolumGostergesiAcik { get; set; }

        /// <summary>Dolum göstergesinin o anki puanı (GDD 3.7.2).</summary>
        public double DolumDegeri { get; private set; }

        /// <summary>
        /// Hamle akışının kapsamı; varsayılan tam akış. Karıştırma denetimi olmadan çalıştırmak için
        /// <see cref="HamleKapsami.Durulma"/> verilir.
        /// </summary>
        public HamleKapsami Kapsam { get; set; } = HamleKapsami.Tam;

        /// <summary>Bölümdeki ekran sayısı (GDD 8.9.1); tek ekranlı bölümde 1.</summary>
        public int EkranSayisi { get; set; } = 1;

        /// <summary>Oynanan ekran (GDD 2.1.7).</summary>
        public int GecerliEkran => Hedefler.GecerliEkran;

        /// <summary>Çok ekranlı bölümde sonraki ekranları kuran işlev; bölüm kurucusu verir.</summary>
        public EkranKurucu? EkranKurucu { get; set; }

        /// <summary>
        /// GDD 2.3.9 (öneri, 11.2.27): açıksa hamle yalnız kendi dalgasını işletir; düşme ve kaskad
        /// <see cref="Ilerle"/> çağrılarıyla adım adım sürer ve adımlar arasında yeni hamle kabul edilir.
        /// Kapalıysa her hamle tahta durulana kadar tek çağrıda işlenir.
        /// </summary>
        public bool AdimliDurulma { get; set; }

        /// <summary>Adımlı durulmada tahta henüz durulmadı mı?</summary>
        public bool DurulmaSuruyor => _surenMotor != null;

        /// <summary>Artan hamle gösterisi yapıldı ya da atlandı mı (GDD 7.6.2)?</summary>
        public bool ArtanHamlelerBildirildi { get; private set; }

        private EtkiMotoru? _surenMotor;
        private bool _bekleyenOyuncuHamlesi;

        private ParametreTablosu P => Baglam.Parametreler;

        // ------------------------------------------------------------------ Hamleler

        /// <summary>Kaydırma hamlesi (GDD 2.3.1). Bölüm "oynanıyor" durumunda değilse reddedilir (2.3.7).</summary>
        public HamleSonucu Kaydir(Konum cikilan, Konum gidilen, List<Olay> olaylar) =>
            HamleYap(olaylar, (sayaciDusur, kapsam, motor) =>
                HamleIsleyici.Kaydir(Baglam, cikilan, gidilen, olaylar, kapsam, sayaciDusur, motor));

        /// <summary>Dokunma hamlesi (GDD 2.3.5).</summary>
        public HamleSonucu Dokun(Konum konum, List<Olay> olaylar) =>
            HamleYap(olaylar, (sayaciDusur, kapsam, motor) =>
                HamleIsleyici.Dokun(Baglam, konum, olaylar, kapsam, sayaciDusur, motor));

        private HamleSonucu HamleYap(List<Olay> olaylar, Func<Action, HamleKapsami, EtkiMotoru?, HamleSonucu> calistir)
        {
            if (Durum != BolumDurumu.Oynaniyor)
            {
                return HamleSonucu.Reddedildi;
            }

            int ilkOlay = olaylar.Count;

            // GDD 7.2.1: sayaç geçerli hamle anında bir azalır; patlamalar ve düşmeler beklenmez.
            // 7.2.2, 7.2.3: geçersiz ve imkânsız hamle, kaskad ve karıştırma sayaçtan düşmez.
            void SayaciDusur()
            {
                KalanHamle = Math.Max(0, KalanHamle - 1);
                YapilanHamle++;
                olaylar.Add(new HamleSayaciDegisti(KalanHamle, YapilanHamle));
            }

            if (AdimliDurulma)
            {
                // GDD 7.3.2: kazanma başladıktan sonra oyuncu hamle yapamaz. Sayaç 0'a indiyse hamle kalmamıştır;
                // "hamle bitti" akışı tahta durulunca başlar (7.4.1).
                if (Hedefler.HepsiTamam || KalanHamle == 0)
                {
                    return HamleSonucu.Reddedildi;
                }

                // GDD 2.3.9 bir öneridir (11.2.27): bayrak kapalıysa düşme ve kaskad sürerken hamle kabul edilmez.
                if (DurulmaSuruyor && !Baglam.Bayraklar.DusmeSirasindaHamle)
                {
                    return HamleSonucu.Reddedildi;
                }

                // GDD 11.3.1: oyuncu hamlesi önce işlenir; kaskad bir sonraki adıma kalır.
                var motor = _surenMotor ?? new EtkiMotoru(Baglam, olaylar);
                motor.OlaylariBagla(olaylar);
                var adimliSonuc = calistir(SayaciDusur, HamleKapsami.Adimli, motor);
                if (adimliSonuc == HamleSonucu.Gecerli)
                {
                    _surenMotor = motor;
                    _bekleyenOyuncuHamlesi = true;
                    DolumuDoldur(olaylar, ilkOlay);
                }

                return adimliSonuc;
            }

            var sonuc = calistir(SayaciDusur, Kapsam, null);

            if (sonuc == HamleSonucu.Gecerli)
            {
                HamleSonrasi(olaylar, ilkOlay, oyuncuHamlesi: true);
            }

            return sonuc;
        }

        /// <summary>
        /// Adımlı durulmanın (GDD 2.3.9) bir adımı: bekleyen küreler, tek düşme güncellemesi, toplama ya da kaskad.
        /// Tahta durulduğunda karıştırma denetimi ve hamle sonu akışı (5.7 adım 9–11) çalışır.
        /// </summary>
        /// <returns>Durulma sürüyorsa true; tahta durulduysa ya da süren bir durulma yoksa false.</returns>
        public bool Ilerle(List<Olay> olaylar)
        {
            if (_surenMotor == null)
            {
                return false;
            }

            int ilkOlay = olaylar.Count;
            _surenMotor.OlaylariBagla(olaylar);
            if (TahtaDongusu.Adim(Baglam, _surenMotor, olaylar, dusme: true, tekGuncelleme: true))
            {
                DolumuDoldur(olaylar, ilkOlay);
                return true;
            }

            _surenMotor = null;
            bool oyuncuHamlesi = _bekleyenOyuncuHamlesi;
            _bekleyenOyuncuHamlesi = false;
            if (Kapsam == HamleKapsami.Tam)
            {
                Karistirici.GerekirseKaristir(Baglam, olaylar);
            }

            HamleSonrasi(olaylar, ilkOlay, oyuncuHamlesi);
            return false;
        }

        /// <summary>
        /// GDD 5.6.1, 5.7 adım 9: tahta durulduğunda olası hamle yoksa karıştırma başlar; başarısız olursa
        /// "olası hamle yok" akışı çalışır (7.5.1, AK-13). Bölüm başında ve durum testlerinde doğrudan çağrılır.
        /// </summary>
        public KaristirmaSonucu OlasiHamleyiDenetle(List<Olay> olaylar)
        {
            if (Durum != BolumDurumu.Oynaniyor || DurulmaSuruyor)
            {
                return KaristirmaSonucu.Gerekmedi;
            }

            int ilkOlay = olaylar.Count;
            var sonuc = Karistirici.GerekirseKaristir(Baglam, olaylar);
            BolumBittiMi(olaylar, ilkOlay);
            return sonuc;
        }

        /// <summary>GDD 5.7 adım 7–11: kazanma, "olası hamle yok", hamle sonu hareketliler, hamle bitti akışı.</summary>
        private void HamleSonrasi(List<Olay> olaylar, int ilkOlay, bool oyuncuHamlesi)
        {
            DolumuDoldur(olaylar, ilkOlay);

            if (BolumBittiMi(olaylar, ilkOlay))
            {
                return;
            }

            // GDD 5.7 adım 11, 6.3.4: her oyuncu hamlesi ve onun kaskadı bitince hamle sonu hareketliler güncellenir.
            if (oyuncuHamlesi && HamleSonuHareketlileriGuncelle(olaylar) && BolumBittiMi(olaylar, ilkOlay))
            {
                return;
            }

            // GDD 3.7.3: gösterge dolduysa tahtaya bir renk küresi fırlatılır.
            DolumuFirlat(olaylar);

            int esik = P.TamSayi(ParametreAdlari.YedekHamleEsigi);
            if (KalanHamle == 0)
            {
                // GDD 7.4.1: hamle sayacı 0'a indi, bekleyen işlem kalmadı ve hedefler tamamlanmadı.
                HamleBitti(olaylar);
            }
            else if (esik > 0 && YedekHamleHakki && KalanHamle <= esik)
            {
                // GDD 3.8.2: eşik 0'dan büyükse hak oyun sürerken, kalan hamle eşiğe indiği anda bir kez kullanılır.
                YedekHakkiKullan(olaylar);
            }
        }

        /// <summary>Kazanma (7.3) ve "olası hamle yok" akışının sonucu (7.5, AK-13).</summary>
        private bool BolumBittiMi(List<Olay> olaylar, int ilkOlay)
        {
            EkranlariIlerlet(olaylar);

            // AK-02 (1)–(2): son hamlenin bütün patlamaları, düşmeleri ve kaskadları bitti; bu sürede hedefler
            // tamamlandıysa sonuç kazanmadır. GDD 7.3.3: kalan hamleler artan hamle ödülüne gider.
            if (Hedefler.HepsiTamam)
            {
                Durum = BolumDurumu.Kazanildi;
                olaylar.Add(new BolumKazanildi(KalanHamle));
                return true;
            }

            var karistirma = olaylar.Skip(ilkOlay).OfType<TahtaKaristirildi>().LastOrDefault();
            if (karistirma == null)
            {
                return false;
            }

            if (karistirma.Sonuc == KaristirmaSonucu.Kaybedildi)
            {
                // AK-13 B: bölüm, ek hamle teklifi sunulmadan kaybedilir.
                Durum = BolumDurumu.Kaybedildi;
                olaylar.Add(new BolumKaybedildi(teklifSunulmadan: true));
                return true;
            }

            if (karistirma.Sonuc == KaristirmaSonucu.BastanBaslatilmali)
            {
                // AK-13 A: yeniden üretim denemeleri de başarısız; bölüm can düşürülmeden baştan başlatılır.
                Durum = BolumDurumu.BastanBaslatilmali;
                olaylar.Add(new BolumBastanBaslatilmali());
                return true;
            }

            return false;
        }

        /// <summary>
        /// GDD 8.9.1, 8.9.2: geçerli ekranın hedefleri tamamlanınca bir sonraki ekrana geçilir. Üst ekranda kalan,
        /// düşebilen bütün taşlar (normal, özel, düşen engel) alt ekranın giriş sırasına eklenir; her sütunda alttaki
        /// taş önce girer (5.1.3). Düşmeyen engeller ve katmanlar üst ekranda kalır. Alt ekranda o sütunun girişi
        /// yoksa taşlar kullanılmaz. Yeni ekranın girişleri açılır (5.3.3) ve tahta durulur.
        /// </summary>
        private void EkranlariIlerlet(List<Olay> olaylar)
        {
            while (EkranKurucu != null
                && !Hedefler.HepsiTamam
                && Hedefler.EkranTamam
                && Hedefler.GecerliEkran < EkranSayisi)
            {
                var eski = Baglam.Tahta;
                var sira = new Dictionary<int, Queue<Tas>>();
                int aktarilan = 0;
                for (int sutun = 1; sutun <= eski.SutunSayisi; sutun++)
                {
                    var kuyruk = new Queue<Tas>();
                    for (int satir = eski.SatirSayisi; satir >= 1; satir--)
                    {
                        var konum = new Konum(satir, sutun);
                        if (eski.Duser(konum))
                        {
                            kuyruk.Enqueue(eski[konum].Tas!);
                            aktarilan++;
                        }
                    }

                    sira.Add(sutun, kuyruk);
                }

                var rastgeleHucreler = new List<Konum>();
                var yeni = EkranKurucu(Hedefler.GecerliEkran + 1, eski, rastgeleHucreler);
                Baglam.Tahta = yeni;

                // GDD 5.3.4 adım 1: üst ekrandan kalan taşlar asıl dolgunun önüne geçer.
                var asil = Baglam.Dolgu is UstEkranKuyrugu onceki ? onceki.Asil : Baglam.Dolgu;
                Baglam.Dolgu = new UstEkranKuyrugu(asil, sira);

                // GDD 5.4.1–5.4.3: rastgele hücreler hazır 3'lü ve kare oluşturmadan renklendirilir. Olası hamle
                // denetimi (5.4.4) boş hücreler dolduktan sonra, durulmanın sonunda yapılır.
                BaslangicTahtasi.RenkleriDagit(Baglam, rastgeleHucreler, null);
                Hedefler.SonrakiEkranaGec(yeni);
                olaylar.Add(new EkranDegisti(Hedefler.GecerliEkran, aktarilan));
                TahtayiDurult(new EtkiMotoru(Baglam, olaylar), olaylar);
            }
        }

        // ------------------------------------------------------------------ Artan hamle gösterisi (GDD 7.6.2, 7.6.3)

        /// <summary>
        /// Kazanılan bölümde kalan her hamle için tahtadaki rastgele bir normal taş çizgi taşına dönüşür (yönü
        /// rastgele); sonra taşlar dönüşüm sırasıyla patlar ve tahta bir kez durulur. Normal taş yetmezse olan kadarı
        /// dönüşür. Patlamalar hedef saymaz (hedefler zaten tamamdır) ve dolum göstergesini doldurmaz. Oyuncu gösteriyi
        /// atlarsa (<paramref name="atla"/>) tahta değişmez. Sonunda "artan hamleler bitti" bilgisi gönderilir (7.6.3).
        /// Kalan hamle sayısı değişmez; para ödülü ondan hesaplanır (7.6.1).
        /// </summary>
        /// <returns>Bilgi bu çağrıda gönderildiyse true; bölüm kazanılmadıysa ya da daha önce gönderildiyse false.</returns>
        public bool ArtanHamleGosterisi(List<Olay> olaylar, bool atla = false)
        {
            if (Durum != BolumDurumu.Kazanildi || ArtanHamlelerBildirildi)
            {
                return false;
            }

            ArtanHamlelerBildirildi = true;
            if (!atla && KalanHamle > 0)
            {
                var tahta = Baglam.Tahta;
                olaylar.Add(new ArtanHamleGosterisiBasladi(KalanHamle));

                var adaylar = tahta.TumKonumlar()
                    .Where(k => tahta.Kaydirilabilir(k) && tahta[k].Tas!.Tur == TasTuru.Normal && tahta[k].UstKatman == null)
                    .ToList();
                Baglam.Rastgele.Karistir(adaylar);

                var donusenler = new List<KeyValuePair<Konum, Tas>>();
                foreach (var konum in adaylar.Take(KalanHamle))
                {
                    var eski = tahta[konum].Tas!;
                    var cizgi = tahta.OzelTasUret(TasTuru.Cizgi, Baglam.Rastgele.Sonraki(2) == 0 ? Yon.Yatay : Yon.Dikey);
                    cizgi.OturmaAdimi = eski.OturmaAdimi;
                    tahta[konum].Tas = cizgi;
                    olaylar.Add(new TasDonustu(konum, eski, cizgi, Baglam.Saat));
                    donusenler.Add(new KeyValuePair<Konum, Tas>(konum, cizgi));
                }

                var motor = new EtkiMotoru(Baglam, olaylar);
                foreach (var cift in donusenler)
                {
                    // Önceki bir patlamanın zincirle tetiklediği ya da kırdığı taş yeniden etkinleşmez.
                    if (ReferenceEquals(tahta[cift.Key].Tas, cift.Value) && !cift.Value.Tetiklendi)
                    {
                        motor.OzelTasiEtkinlestir(cift.Key, Tetik.Dokunma);
                        motor.DalgayiBitir();
                    }
                }

                TahtaDongusu.Durult(Baglam, motor, olaylar);
            }

            olaylar.Add(new ArtanHamlelerBitti(KalanHamle, atla));
            return true;
        }

        private bool HamleSonuHareketlileriGuncelle(List<Olay> olaylar)
        {
            var tahta = Baglam.Tahta;
            bool degisti = false;
            foreach (var konum in tahta.TumKonumlar().ToList())
            {
                var tas = tahta[konum].Tas;
                var davranis = tas != null && tas.Tur == TasTuru.Engel ? tas.Engel!.HamleSonuDavranisi : null;
                if (davranis != null && davranis.Uygula(Baglam, konum, olaylar))
                {
                    degisti = true;
                }
            }

            if (degisti)
            {
                TahtayiDurult(new EtkiMotoru(Baglam, olaylar), olaylar);
            }

            return degisti;
        }

        private void TahtayiDurult(EtkiMotoru motor, List<Olay> olaylar)
        {
            TahtaDongusu.Durult(Baglam, motor, olaylar, dusme: Kapsam != HamleKapsami.YalnizPatlama);
            if (Kapsam == HamleKapsami.Tam)
            {
                Karistirici.GerekirseKaristir(Baglam, olaylar);
            }
        }

        // ------------------------------------------------------------------ Hamle bitti (GDD 7.4, AK-02)

        private void HamleBitti(List<Olay> olaylar)
        {
            bool yedekHakAkista = YedekHamleHakki && P.TamSayi(ParametreAdlari.YedekHamleEsigi) == 0;

            // AK-02 A (3): hedef kaldıysa ve yedek hamle hakkı varsa önce bu hak kullanılır; teklif penceresi açılmaz.
            if (Baglam.Bayraklar[Ak.HamleBittiAkisi] == Varyant.A && yedekHakAkista)
            {
                YedekHakkiKullan(olaylar);
                return;
            }

            // AK-02 A (4), B (3): ek hamle teklifi açılır.
            Durum = BolumDurumu.EkHamleTeklifi;
            olaylar.Add(new EkHamleTeklifiAcildi(P.TamSayi(ParametreAdlari.EkHamleSayisi)));
        }

        private void YedekHakkiKullan(List<Olay> olaylar)
        {
            int eklenen = P.TamSayi(ParametreAdlari.YedekHamleSayisi);
            YedekHamleHakki = false;
            KalanHamle += eklenen;
            Durum = BolumDurumu.Oynaniyor;
            olaylar.Add(new YedekHamleKullanildi(eklenen));
            olaylar.Add(new HamleSayaciDegisti(KalanHamle, YapilanHamle));
        }

        /// <summary>
        /// Ek hamle teklifi kabul edildi: bölüm aynı tahtayla sürer (GDD 7.4.3). Verilen hamle sayısı meta katmandan
        /// gelebilir (9.11.4, 9.12.1); verilmezse parametre: ek_hamle_sayisi kullanılır.
        /// </summary>
        public void EkHamleTeklifiniKabulEt(List<Olay> olaylar, int? hamle = null)
        {
            TeklifAcikOlmali();
            KalanHamle += hamle ?? P.TamSayi(ParametreAdlari.EkHamleSayisi);
            Durum = BolumDurumu.Oynaniyor;
            olaylar.Add(new HamleSayaciDegisti(KalanHamle, YapilanHamle));
        }

        /// <summary>Ek hamle teklifi reddedildi (GDD 7.4.3, AK-02).</summary>
        public void EkHamleTeklifiniReddet(List<Olay> olaylar)
        {
            TeklifAcikOlmali();

            // AK-02 B (4): teklif reddedilirse ve yedek hamle hakkı varsa bu hak son şans olarak kullanılır.
            if (Baglam.Bayraklar[Ak.HamleBittiAkisi] == Varyant.B
                && YedekHamleHakki
                && P.TamSayi(ParametreAdlari.YedekHamleEsigi) == 0)
            {
                YedekHakkiKullan(olaylar);
                return;
            }

            Durum = BolumDurumu.Kaybedildi;
            olaylar.Add(new BolumKaybedildi(teklifSunulmadan: false));
        }

        private void TeklifAcikOlmali()
        {
            if (Durum != BolumDurumu.EkHamleTeklifi)
            {
                throw new InvalidOperationException("Açık bir ek hamle teklifi yok.");
            }
        }

        // ------------------------------------------------------------------ Dolum göstergesi (GDD 3.7)

        /// <summary>GDD 3.7.2: hamlenin olaylarından göstergeye puan eklenir.</summary>
        private void DolumuDoldur(List<Olay> olaylar, int ilkOlay)
        {
            if (!DolumGostergesiAcik)
            {
                return;
            }

            double puan = 0;
            foreach (var olay in olaylar.Skip(ilkOlay))
            {
                switch (olay)
                {
                    case OzelTasEtkinlesti e when e.Tetik != Tetik.Kombinasyon:
                        puan += e.Tas.Tur == TasTuru.Cizgi ? P.Sayi(ParametreAdlari.DolumPuanCizgi)
                            : e.Tas.Tur == TasTuru.Bomba ? P.Sayi(ParametreAdlari.DolumPuanBomba)
                            : e.Tas.Tur == TasTuru.Gudumlu ? P.Sayi(ParametreAdlari.DolumPuanGudumlu)
                            : 0;
                        break;
                    case TasPatladi p when p.Tur == VurusTuru.RenkIsini:
                        puan += P.Sayi(ParametreAdlari.DolumPuanRenkTasBasina);
                        break;
                    case KombinasyonCalisti k:
                        puan += k.Tur == KombinasyonTuru.KureKure
                            ? P.Sayi(ParametreAdlari.DolumPuanRenkRenk)
                            : P.Sayi(ParametreAdlari.DolumPuanKombinasyon);
                        break;
                }
            }

            if (puan > 0)
            {
                DolumDegeri += puan;
                olaylar.Add(new DolumGostergesiDegisti(DolumDegeri, P.Sayi(ParametreAdlari.DolumKapasitesi)));
            }
        }

        /// <summary>
        /// GDD 3.7.3–3.7.6: gösterge dolunca tahtaya tek bir renk küresi fırlatılır; gösterge sıfırlanır.
        /// Çift renk modunda küre çift renk küresidir (mod bölüm genelindedir, 3.6.1).
        /// </summary>
        private void DolumuFirlat(List<Olay> olaylar)
        {
            double kapasite = P.Sayi(ParametreAdlari.DolumKapasitesi);
            if (!DolumGostergesiAcik || DolumDegeri < kapasite)
            {
                return;
            }

            var hucre = FirlatmaHucresi();
            if (!hucre.HasValue)
            {
                return;
            }

            var kure = Baglam.Tahta.OzelTasUret(TasTuru.RenkKuresi);
            kure.OturmaAdimi = Baglam.Saat;
            Baglam.Tahta[hucre.Value].Tas = kure;
            DolumDegeri = 0;
            olaylar.Add(new RenkKuresiFirlatildi(hucre.Value, kure));
            olaylar.Add(new DolumGostergesiDegisti(DolumDegeri, kapasite));
        }

        /// <summary>
        /// GDD 3.7.4: hücreler sol-üstten satır satır sıralanır, zorluğa bağlı rastgelelikle karıştırılır ve
        /// "taş üretmeye uygun" ilk hücre seçilir: üstü açık, içinde serbest bir normal taş olan ya da boş hücre.
        /// </summary>
        private Konum? FirlatmaHucresi()
        {
            var tahta = Baglam.Tahta;
            var hucreler = tahta.TumKonumlar().ToList();
            Baglam.Zorluk.Karistir(hucreler);
            foreach (var konum in hucreler)
            {
                var hucre = tahta[konum];
                if (!hucre.Oynanabilir || hucre.UstKatman != null)
                {
                    continue;
                }

                if (hucre.Tas == null || (hucre.Tas.Tur == TasTuru.Normal && tahta.Kaydirilabilir(konum)))
                {
                    return konum;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ Oyun içi araçlar (GDD 3.9.4)

        /// <summary>
        /// Oyun içi araç kullanır. Hamle harcamaz (GDD 3.9.5). Araçla seçilemeyen bir hücre hedef olarak
        /// işaretlenemez; bu durumda false döner ve araç adedi düşmemelidir (adet meta katmandadır).
        /// </summary>
        public bool AracKullan(AracTuru tur, Konum? konum, List<Olay> olaylar)
        {
            if (Durum != BolumDurumu.Oynaniyor || DurulmaSuruyor)
            {
                return false;
            }

            var tahta = Baglam.Tahta;
            int ilkOlay = olaylar.Count;
            var motor = new EtkiMotoru(Baglam, olaylar);

            if (tur == AracTuru.Karistirma)
            {
                olaylar.Add(new AracKullanildi(tur, null));
                Karistirici.Karistir(Baglam, olaylar);
            }
            else
            {
                if (!konum.HasValue || !AraclaSecilebilir(konum.Value))
                {
                    return false;
                }

                var hedefler = tur == AracTuru.TekHucre ? new List<Konum> { konum.Value }
                    : tahta.TumKonumlar()
                        .Where(k => tur == AracTuru.Satir ? k.Satir == konum.Value.Satir : k.Sutun == konum.Value.Sutun)
                        .ToList();
                olaylar.Add(new AracKullanildi(tur, konum));
                motor.AracVurusu(hedefler);
            }

            TahtayiDurult(motor, olaylar);
            HamleSonrasi(olaylar, ilkOlay, oyuncuHamlesi: false);
            return true;
        }

        private bool AraclaSecilebilir(Konum konum)
        {
            var tahta = Baglam.Tahta;
            if (!tahta.Oynanabilir(konum))
            {
                return false;
            }

            var tas = tahta[konum].Tas;
            return tas == null || tas.Tur != TasTuru.Engel || tas.Engel!.AraclaSecilebilir;
        }

        // ------------------------------------------------------------------ Başlangıç takviyeleri (GDD 3.9.1–3.9.3)

        /// <summary>
        /// Tahtaya bir takviye taşı koyar (GDD 3.9.1). Hücre: hedeflere en yakın, olası bir eşleşmeyi bozmayan
        /// normal taş hücresi (3.9.3). Çizgi taşının yönü, satırın ve sütunun temizleme puanı karşılaştırılarak
        /// seçilir; puan, sırada bulunan hedef öğe ve engel sayısıdır (3.9.2).
        /// </summary>
        /// <returns>Taşın konduğu hücre; uygun hücre yoksa null.</returns>
        public Konum? TakviyeKoy(TasTuru tur, List<Olay> olaylar)
        {
            var tahta = Baglam.Tahta;
            var adaylar = tahta.TumKonumlar()
                .Where(k => tahta.Kaydirilabilir(k) && tahta[k].Tas!.Tur == TasTuru.Normal && tahta[k].UstKatman == null)
                .ToList();
            if (adaylar.Count == 0)
            {
                return null;
            }

            // Olası bir eşleşmeyi kuran taşların hepsi korunur: kaydırılan iki taş ve eşleşmeye girecek diğerleri.
            // Başka aday kalmıyorsa kısıt uygulanmaz.
            var korunan = new HashSet<Konum>();
            foreach (var hamle in OlasiHamleBulucu.Bul(tahta).Where(h => h.Tur == OlasiHamleTuru.Eslesme))
            {
                korunan.Add(hamle.Cikilan);
                korunan.Add(hamle.Gidilen);

                var a = tahta[hamle.Cikilan];
                var b = tahta[hamle.Gidilen];
                (a.Tas, b.Tas) = (b.Tas, a.Tas);
                foreach (var grup in Eslesme.EslesmeBulucu.Bul(tahta, Baglam.Bayraklar, new[] { hamle.Cikilan, hamle.Gidilen }))
                {
                    foreach (var hucre in grup.TumHucreler)
                    {
                        // Yer değiştirme geri alınacağı için kaydırılan taşların hücreleri zaten korunuyor.
                        korunan.Add(hucre);
                    }
                }

                (a.Tas, b.Tas) = (b.Tas, a.Tas);
            }
            var serbest = adaylar.Where(k => !korunan.Contains(k)).ToList();
            if (serbest.Count > 0)
            {
                adaylar = serbest;
            }

            var hedefHucreleri = tahta.TumKonumlar().Where(k => Hedefler.HedefOgesiMi(tahta, k)).ToList();
            if (hedefHucreleri.Count > 0)
            {
                int Uzaklik(Konum k) => hedefHucreleri.Min(h => Math.Abs(h.Satir - k.Satir) + Math.Abs(h.Sutun - k.Sutun));
                int enYakin = adaylar.Min(Uzaklik);
                adaylar = adaylar.Where(k => Uzaklik(k) == enYakin).ToList();
            }

            var secilen = Baglam.Rastgele.Sec<Konum>(adaylar);

            Yon? yon = null;
            if (tur == TasTuru.Cizgi)
            {
                int satirPuani = tahta.TumKonumlar().Count(k => k.Satir == secilen.Satir && TemizlemePuaniVar(k));
                int sutunPuani = tahta.TumKonumlar().Count(k => k.Sutun == secilen.Sutun && TemizlemePuaniVar(k));
                yon = satirPuani > sutunPuani ? Yon.Yatay
                    : sutunPuani > satirPuani ? Yon.Dikey
                    : Baglam.Rastgele.Sonraki(2) == 0 ? Yon.Yatay : Yon.Dikey;
            }

            var tas = tahta.OzelTasUret(tur, yon);
            tas.OturmaAdimi = tahta[secilen].Tas!.OturmaAdimi;
            tahta[secilen].Tas = tas;
            olaylar.Add(new TakviyeKondu(secilen, tas));
            return secilen;
        }

        private bool TemizlemePuaniVar(Konum konum)
        {
            var tahta = Baglam.Tahta;
            if (!tahta.Oynanabilir(konum))
            {
                return false;
            }

            var hucre = tahta[konum];
            return Hedefler.HedefOgesiMi(tahta, konum)
                || hucre.UstKatman != null
                || (hucre.Tas != null && hucre.Tas.Tur == TasTuru.Engel);
        }
    }
}
