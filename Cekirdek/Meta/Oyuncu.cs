using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Model;
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
        }

        public ParametreTablosu Parametreler { get; }
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

        /// <summary>Oyuncunun ulaştığı en yüksek bölüm; kapılar buna göre değerlendirilir (GDD 9.4.2).</summary>
        public int EnYuksekBolum { get; set; } = 1;

        /// <summary>Art arda kazanılan bölüm sayısı; kayıpta sıfırlanır.</summary>
        public int KazanmaSerisi { get; private set; }

        public int Yildiz { get; set; }

        public bool SezonHattiAcik => Kapilar.Acik(MetaParametreAdlari.AcilisSezonHatti, EnYuksekBolum);

        public bool HaftalikHatAcik => Kapilar.Acik(MetaParametreAdlari.AcilisHaftalikHat, EnYuksekBolum);

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
        }

        internal void KazanimKaydet() => KazanmaSerisi++;

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
            _carpanIsareti = _oyuncu.Carpan.BolumeGir(simdi);

            var oturum = BolumKurucu.Kur(veri, bayraklar, p, tohum, olaylar, _oyuncu.Zorluk.RastgelelikAgirligi(Etiket));
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
        /// Bölümün meta sonucunu işler. Kazanınca: para (GDD 9.2.2), yıldız (9.6.1), hat puanları (9.8.4, 9.9.3),
        /// seri ve deneme sayacı. Kaybedince: bir can düşer (7.4.4, 9.1.3), seri sıfırlanır, deneme sayacı artar.
        /// Başlamış bir bölümden çıkmak da kayıp sayılır (9.1.3): <paramref name="cikildi"/> true verilir.
        /// </summary>
        public BolumSonucu SonucuIsle(DateTime simdi, bool cikildi = false)
        {
            var sonuc = new BolumSonucu();
            if (Oturum == null || _sonucIslendi)
            {
                return sonuc;
            }

            var p = _oyuncu.Parametreler;
            if (!cikildi && Oturum.Durum == BolumDurumu.Kazanildi)
            {
                _sonucIslendi = true;
                sonuc.Kazanildi = true;
                sonuc.Para = EtiketOdulleri.KazanmaParasi(p, Etiket, Oturum.KalanHamle);
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
                sonuc.OlayPuani = taban * _carpanIsareti;
                if (_oyuncu.HaftalikHatAcik)
                {
                    sonuc.HaftalikHatPuani = sonuc.OlayPuani;
                    _oyuncu.HaftalikHat.PuanEkle(sonuc.OlayPuani);
                }

                _oyuncu.KazanimKaydet();
                _oyuncu.Zorluk.KazanimKaydet(Bolum);
                _teklif.Sifirla();
                if (Bolum >= _oyuncu.EnYuksekBolum)
                {
                    _oyuncu.EnYuksekBolum = Bolum + 1;
                }

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
                _oyuncu.Zorluk.BasarisizlikKaydet(Bolum);
                _teklif.Sifirla();
            }

            // AK-13 A: "baştan başlatılmalı" durumunda can düşmez ve kayıp sayılmaz.
            return sonuc;
        }
    }
}
