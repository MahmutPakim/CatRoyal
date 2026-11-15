using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Veri;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatRoyal.Sunum
{
    /// <summary>
    /// Oynanabilir sahne. Boş bir sahnede kendini kurar. Üç ekranı vardır: bölüm öncesi (can, para, takviye seçimi),
    /// oyun (tahta, araçlar, ek hamle teklifi) ve sonuç (ödüller). Oyun kuralı ve ekonomi kuralı içermez: kurallar
    /// çekirdekte (<see cref="BolumOturumu"/>), ekonomi meta katmandadır (<see cref="BolumAkisi"/>, <see cref="Oyuncu"/>).
    ///
    /// Geçici olanlar: gösterge IMGUI ile çizilir; oyuncu durumu kaydedilmez, oyun kapanınca sıfırlanır.
    /// </summary>
    public sealed class OyunSahnesi : MonoBehaviour
    {
        private enum Ekran
        {
            BolumOncesi,
            Oyun,
            Sonuc,
        }

        private static readonly TakviyeTuru[] BaslangicTakviyeleri =
        {
            TakviyeTuru.Cizgi, TakviyeTuru.Bomba, TakviyeTuru.RenkKuresi, TakviyeTuru.Gudumlu,
        };

        private static readonly TakviyeTuru[] Araclar =
        {
            TakviyeTuru.TekHucre, TakviyeTuru.Satir, TakviyeTuru.Sutun, TakviyeTuru.Karistirma,
        };

        private readonly ISaat _saat = new SistemSaati();
        private readonly HashSet<TakviyeTuru> _secilenTakviyeler = new HashSet<TakviyeTuru>();

        private OyunVerisi _veri;
        private Oyuncu _oyuncu;
        private ZorlukEtiketTablosu _etiketler;
        private BolumAkisi _akis;
        private BolumOturumu _oturum;
        private BolumVerisi _bolum;
        private BolumSonucu _sonuc;
        private TahtaGorunumu _gorunum;
        private Camera _kamera;

        private Ekran _ekran = Ekran.BolumOncesi;
        private int _bolumSirasi;
        private string _hata;
        private string _bildirim;
        private float _bildirimBitis;
        private TakviyeTuru? _seciliArac;
        private Konum? _basilanHucre;
        private Vector2 _basilanEkran;
        private float _sonHamleZamani;
        private float _ipucuBeklemesi;
        private bool _ikinciUyari;
        private bool _cikisOnayi;

        private DateTime Simdi => _saat.SimdiUtc;

        /// <summary>Sahnede bu bileşen yoksa, herhangi bir sahne yüklendiğinde kendiliğinden eklenir.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void KendiniKur()
        {
            if (FindAnyObjectByType<OyunSahnesi>() == null)
            {
                new GameObject("CatRoyal Oyun").AddComponent<OyunSahnesi>();
            }
        }

        private void Start()
        {
            try
            {
                _veri = OyunVerisi.Yukle();
                if (_veri.BolumDosyalari.Count == 0)
                {
                    throw new InvalidOperationException("StreamingAssets/CatRoyal/veri/bolumler altında bölüm dosyası yok.");
                }

                _oyuncu = new Oyuncu(_veri.Parametreler, Simdi);
                _etiketler = _veri.EtiketTablosu();
                _ipucuBeklemesi = (float)_veri.Parametreler.Sayi("sunum_ipucu_bekleme_suresi");

                _kamera = Camera.main;
                if (_kamera == null)
                {
                    _kamera = new GameObject("Kamera").AddComponent<Camera>();
                    _kamera.tag = "MainCamera";
                }

                _kamera.orthographic = true;
                _kamera.backgroundColor = new Color(0.09f, 0.10f, 0.14f);
                _kamera.clearFlags = CameraClearFlags.SolidColor;
                _kamera.transform.position = new Vector3(0f, 0f, -10f);

                _gorunum = new GameObject("Tahta").AddComponent<TahtaGorunumu>();
                BolumOncesineGec(0);
            }
            catch (Exception hata)
            {
                _hata = hata.Message;
                Debug.LogException(hata);
            }
        }

        // ------------------------------------------------------------------ Ekran geçişleri

        private void BolumOncesineGec(int sira)
        {
            StopAllCoroutines();
            _bolumSirasi = (sira + _veri.BolumDosyalari.Count) % _veri.BolumDosyalari.Count;
            _ekran = Ekran.BolumOncesi;
            _akis = null;
            _oturum = null;
            _sonuc = null;
            _seciliArac = null;
            _basilanHucre = null;
            _ikinciUyari = false;
            _cikisOnayi = false;
            _gorunum.gameObject.SetActive(false);

            try
            {
                _hata = null;
                _bolum = _veri.BolumOku(_bolumSirasi);
            }
            catch (Exception hata)
            {
                _hata = hata.Message;
                Debug.LogException(hata);
            }
        }

        /// <summary>Bölümü başlatır: can denetimi, çarpan işareti ve seçilen takviyeler meta katmandadır (GDD 9.1.2, 9.3.2).</summary>
        private void BolumuBaslat()
        {
            var olaylar = new List<Olay>();
            var akis = new BolumAkisi(_oyuncu, _etiketler, _bolum.Kimlik);
            BolumOturumu oturum;
            try
            {
                oturum = akis.Baslat(
                    _bolum, _veri.Bayraklar, (ulong)DateTime.Now.Ticks, Simdi, _secilenTakviyeler.ToList(), olaylar);
            }
            catch (Exception hata)
            {
                _hata = hata.Message;
                Debug.LogException(hata);
                return;
            }

            if (oturum == null)
            {
                Bildir("Canın yok. Dolmasını bekle ya da can doldur.");
                return;
            }

            _akis = akis;
            _oturum = oturum;
            _ekran = Ekran.Oyun;
            _secilenTakviyeler.Clear();
            _gorunum.gameObject.SetActive(true);
            _gorunum.Kur(_oturum.Baglam.Tahta, _veri.Parametreler);
            KamerayiAyarla();
            _sonHamleZamani = Time.time;
        }

        /// <summary>Bölümün meta sonucunu işler ve sonuç ekranına geçer (GDD 9.2.2, 9.1.3).</summary>
        private void SonucaGec(bool cikildi = false)
        {
            _sonuc = _akis.SonucuIsle(Simdi, cikildi);
            _ekran = Ekran.Sonuc;
            _gorunum.gameObject.SetActive(false);
            _seciliArac = null;
            _ikinciUyari = false;
            _cikisOnayi = false;
        }

        private void Bildir(string mesaj)
        {
            _bildirim = mesaj;
            _bildirimBitis = Time.time + 2.5f;
        }

        private void KamerayiAyarla()
        {
            var tahta = _oturum.Baglam.Tahta;

            // Tahta, üst ve alt gösterge şeritlerinin arasına sığar.
            float seritPayi = Mathf.Clamp01(((UstSerit + AltSerit) * Olcek) / Screen.height);
            float dikey = ((tahta.SatirSayisi * 0.5f) + 0.4f) / Mathf.Max(1f - seritPayi, 0.2f);
            float yatay = ((tahta.SutunSayisi * 0.5f) + 0.3f) / Mathf.Max(_kamera.aspect, 0.01f);
            _kamera.orthographicSize = Mathf.Max(dikey, yatay);
        }

        // ------------------------------------------------------------------ Girdi

        private void Update()
        {
            if (_ekran != Ekran.Oyun || _oturum == null)
            {
                return;
            }

            KamerayiAyarla();

            if (_gorunum.Mesgul)
            {
                _sonHamleZamani = Time.time;
                return;
            }

            // Bölüm bittiyse (kazanma, kayıp, baştan başlatma) olaylar oynatıldıktan sonra sonuç ekranına geçilir.
            if (_oturum.Durum == BolumDurumu.Kazanildi
                || _oturum.Durum == BolumDurumu.Kaybedildi
                || _oturum.Durum == BolumDurumu.BastanBaslatilmali)
            {
                SonucaGec();
                return;
            }

            if (_oturum.Durum != BolumDurumu.Oynaniyor || _cikisOnayi)
            {
                return;
            }

            IpucunuDenetle();

            var isaretci = Pointer.current;
            if (isaretci == null)
            {
                return;
            }

            Vector2 ekran = isaretci.position.ReadValue();
            if (isaretci.press.wasPressedThisFrame)
            {
                // Göstergenin üstündeki tıklamalar tahtaya geçmez.
                _basilanHucre = GostergeUstunde(ekran) ? null : _gorunum.HucreBul(DunyaNoktasi(ekran));
                _basilanEkran = ekran;
            }
            else if (isaretci.press.wasReleasedThisFrame && _basilanHucre.HasValue)
            {
                var hucre = _basilanHucre.Value;
                _basilanHucre = null;
                HamleyiIslet(hucre, DunyaNoktasi(ekran) - DunyaNoktasi(_basilanEkran));
            }
        }

        private Vector3 DunyaNoktasi(Vector2 ekran)
        {
            var nokta = _kamera.ScreenToWorldPoint(new Vector3(ekran.x, ekran.y, -_kamera.transform.position.z));
            nokta.z = 0f;
            return nokta;
        }

        /// <summary>Sürükleme bir kaydırmadır (GDD 2.3.1); yerinde bırakma bir dokunmadır (2.3.5).</summary>
        private void HamleyiIslet(Konum hucre, Vector3 surukleme)
        {
            var olaylar = new List<Olay>();

            if (_seciliArac.HasValue)
            {
                // GDD 3.9.4, 9.3.4: araç seçilen hücreye uygulanır ve adedi bir azalır; seçilemeyen hücrede harcanmaz.
                if (_akis.AracKullan(_seciliArac.Value, hucre, Simdi, olaylar))
                {
                    _seciliArac = null;
                }
            }
            else if (surukleme.magnitude < 0.35f)
            {
                _oturum.Dokun(hucre, olaylar);
            }
            else
            {
                Konum gidilen = Mathf.Abs(surukleme.x) > Mathf.Abs(surukleme.y)
                    ? (surukleme.x > 0 ? hucre.Sag : hucre.Sol)
                    : (surukleme.y > 0 ? hucre.Yukari : hucre.Asagi);
                _oturum.Kaydir(hucre, gidilen, olaylar);
            }

            OlaylariOynat(olaylar);
        }

        private void OlaylariOynat(List<Olay> olaylar)
        {
            _sonHamleZamani = Time.time;
            if (olaylar.Count > 0)
            {
                StartCoroutine(_gorunum.Oynat(olaylar));
            }
        }

        /// <summary>GDD 5.8.2: oyuncu bir süre hamle yapmazsa olası bir hamle ipucu olarak gösterilir.</summary>
        private void IpucunuDenetle()
        {
            if (Time.time - _sonHamleZamani < _ipucuBeklemesi)
            {
                return;
            }

            _sonHamleZamani = Time.time;
            var hamleler = OlasiHamleBulucu.Bul(_oturum.Baglam.Tahta, yalnizIlki: true);
            if (hamleler.Count > 0)
            {
                _gorunum.IpucuGoster(hamleler[0].Cikilan, hamleler[0].Gidilen);
            }
        }

        // ------------------------------------------------------------------ Gösterge (geçici, IMGUI)

        private const float UstSerit = 108f;
        private const float AltSerit = 56f;

        private GUIStyle _baslik;
        private GUIStyle _metin;
        private GUIStyle _sagMetin;

        /// <summary>Gösterge ölçeği: dikey ekranda genişliğe, yatay ekranda yüksekliğe göre.</summary>
        private static float Olcek => Mathf.Max(1f, Mathf.Min(Screen.width / 420f, Screen.height / 720f));

        private static bool GostergeUstunde(Vector2 ekran) =>
            ekran.y > Screen.height - (UstSerit * Olcek) || ekran.y < AltSerit * Olcek;

        private void OnGUI()
        {
            float olcek = Olcek;
            GUI.matrix = Matrix4x4.Scale(new Vector3(olcek, olcek, 1f));
            float genislik = Screen.width / olcek;
            float yukseklik = Screen.height / olcek;

            _baslik = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            _metin = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            _sagMetin = new GUIStyle(_metin) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };

            if (_hata != null)
            {
                GUI.Label(new Rect(12, 12, genislik - 24, 200), "Hata: " + _hata, _metin);
                return;
            }

            if (_oyuncu == null || _bolum == null)
            {
                return;
            }

            switch (_ekran)
            {
                case Ekran.BolumOncesi:
                    BolumOncesiEkrani(genislik, yukseklik);
                    break;
                case Ekran.Oyun:
                    OyunGostergesi(genislik, yukseklik);
                    break;
                default:
                    SonucEkrani(genislik, yukseklik);
                    break;
            }

            if (_bildirim != null && Time.time < _bildirimBitis)
            {
                GUI.Box(new Rect(12, yukseklik - 108, genislik - 24, 34), _bildirim);
            }
        }

        private string ParaVeCan()
        {
            var can = _oyuncu.Can;
            string canMetni = can.SinirsizAktif(Simdi)
                ? "sınırsız"
                : $"{can.Mevcut(Simdi)}/{can.Azami}";
            var kalan = can.SonrakiCanaKalan(Simdi);
            if (kalan.HasValue && !can.SinirsizAktif(Simdi))
            {
                canMetni += $" (+1: {(int)kalan.Value.TotalMinutes}:{kalan.Value.Seconds:00})";
            }

            return $"Para: {_oyuncu.Cuzdan.Bakiye}    Can: {canMetni}    Yıldız: {_oyuncu.Yildiz}";
        }

        // ------------------------------------------------------------------ Bölüm öncesi ekranı (GDD 9.3.2)

        private void BolumOncesiEkrani(float genislik, float yukseklik)
        {
            float x = 16f;
            float w = genislik - 32f;
            float y = 16f;

            GUI.Label(new Rect(x, y, w, 30), $"Bölüm {_bolum.Kimlik}", _baslik);
            GUI.Label(new Rect(x, y, w, 30), EtiketAdi(_etiketler.Etiket(_bolum.Kimlik)), _sagMetin);
            y += 34f;
            GUI.Label(new Rect(x, y, w, 24), ParaVeCan(), _metin);
            y += 28f;
            GUI.Label(new Rect(x, y, w, 24), $"Hamle: {_bolum.Hamle}    Hedef: {string.Join(", ", _bolum.Hedefler.Select(HedefMetni))}", _metin);
            y += 34f;

            GUI.Label(new Rect(x, y, w, 24), "Başlangıç takviyeleri", _baslik);
            y += 30f;
            foreach (var tur in BaslangicTakviyeleri)
            {
                TakviyeSatiri(tur, x, y, w);
                y += 38f;
            }

            if (_oyuncu.CiftRenkTakviyesiHazir)
            {
                GUI.Label(new Rect(x, y, w, 40), "Çift renk takviyesi hazır: bölüm çift renk modunda başlar.", _metin);
                y += 40f;
            }

            if (_oyuncu.SeriTakviyesiKademesi > 0)
            {
                GUI.Label(new Rect(x, y, w, 24), $"Seri takviyesi: {_oyuncu.SeriTakviyesiKademesi}. kademe", _metin);
                y += 26f;
            }

            if (_oyuncu.SezonHattiAcik)
            {
                var hat = _oyuncu.SezonHatti;
                GUI.Label(new Rect(x, y, w, 24), $"Sezon hattı: basamak {hat.Basamak}/{hat.BasamakSayisi} ({hat.BasamakPuani}/{hat.SiradakiHedef?.ToString() ?? "hazne"})", _metin);
                y += 26f;
            }

            if (_oyuncu.HaftalikHatAcik)
            {
                var hat = _oyuncu.HaftalikHat;
                GUI.Label(new Rect(x, y, w, 24), $"Haftalık hat: basamak {hat.Basamak}/{hat.BasamakSayisi} ({hat.BasamakPuani}/{hat.SiradakiHedef?.ToString() ?? "hazne"})", _metin);
                y += 26f;
            }

            y += 8f;
            bool canVar = _oyuncu.Can.BolumeGirebilir(Simdi);
            GUI.enabled = canVar;
            if (GUI.Button(new Rect(x, y, w, 46), canVar ? "Oyna" : "Can yok"))
            {
                BolumuBaslat();
            }

            GUI.enabled = true;
            y += 54f;

            if (!canVar)
            {
                // GDD 9.1.6: can bitince can dolum teklifi alınabilir.
                int fiyat = _veri.Parametreler.TamSayi(MetaParametreAdlari.CanDolumTeklifiFiyati);
                if (GUI.Button(new Rect(x, y, w, 36), $"Canları doldur ({fiyat} para)"))
                {
                    if (_oyuncu.Cuzdan.Harca(fiyat, "can_dolum_teklifi"))
                    {
                        _oyuncu.Can.TamDoldur(Simdi);
                    }
                    else
                    {
                        Bildir("Yetersiz para.");
                    }
                }

                y += 44f;
            }

            // Geliştirici kısayolları: bölüm seçimi ve kapıları açma. Oyunun parçası değildir.
            float alt = yukseklik - 84f;
            GUI.Label(new Rect(x, alt, w, 22), $"Geliştirici — ulaşılan en yüksek bölüm: {_oyuncu.EnYuksekBolum}", _metin);
            float dugme = (w - 16f) / 3f;
            if (GUI.Button(new Rect(x, alt + 26, dugme, 30), "Önceki bölüm")) BolumOncesineGec(_bolumSirasi - 1);
            if (GUI.Button(new Rect(x + dugme + 8, alt + 26, dugme, 30), "Sonraki bölüm")) BolumOncesineGec(_bolumSirasi + 1);
            if (GUI.Button(new Rect(x + ((dugme + 8) * 2), alt + 26, dugme, 30), "Kapıları aç")) _oyuncu.EnYuksekBolum = 500;
        }

        /// <summary>
        /// Her başlangıç takviyesi ayrı ayrı seçilir (GDD 9.3.2). Envanterde yoksa satın alma sunulur (9.3.3);
        /// açılış bölümünden önce yuva kilitli görünür (9.3.5). Güdümlü taş satılmaz (9.3.1).
        /// </summary>
        private void TakviyeSatiri(TakviyeTuru tur, float x, float y, float w)
        {
            bool acik = _oyuncu.Kapilar.TakviyeAcik(tur, _oyuncu.EnYuksekBolum);
            int adet = _oyuncu.Envanter.Adet(tur);
            bool sinirsiz = _oyuncu.Envanter.SinirsizAktif(tur, Simdi);
            int? fiyat = _oyuncu.Magaza.Fiyat(tur);
            var alan = new Rect(x, y, w, 32);

            if (!acik)
            {
                GUI.enabled = false;
                GUI.Button(alan, $"{TakviyeAdi(tur)} — kilitli");
                GUI.enabled = true;
                return;
            }

            if (adet > 0 || sinirsiz)
            {
                bool secili = _secilenTakviyeler.Contains(tur);
                string ad = $"{TakviyeAdi(tur)} ({(sinirsiz ? "sınırsız" : adet.ToString())}){(secili ? " — seçili" : string.Empty)}";
                if (GUI.Toggle(alan, secili, ad, GUI.skin.button) != secili)
                {
                    if (secili) _secilenTakviyeler.Remove(tur);
                    else _secilenTakviyeler.Add(tur);
                }

                return;
            }

            if (!fiyat.HasValue)
            {
                GUI.enabled = false;
                GUI.Button(alan, $"{TakviyeAdi(tur)} — yok (yalnızca ödül olarak gelir)");
                GUI.enabled = true;
                return;
            }

            if (GUI.Button(alan, $"{TakviyeAdi(tur)} — satın al ({fiyat.Value} para)"))
            {
                SatinAl(tur);
            }
        }

        private bool SatinAl(TakviyeTuru tur)
        {
            if (_oyuncu.Magaza.SatinAl(tur))
            {
                return true;
            }

            // GDD 9.2.5: yetersiz bakiyede harcama başarısız olur. Para mağazası bu sürümde yok.
            Bildir("Yetersiz para.");
            return false;
        }

        // ------------------------------------------------------------------ Oyun göstergesi

        private void OyunGostergesi(float genislik, float yukseklik)
        {
            GUI.Label(new Rect(12, 6, 220, 28), $"Bölüm {_bolum.Kimlik}", _baslik);
            GUI.Label(new Rect(genislik - 212, 10, 200, 24), $"Kalan hamle: {_oturum.KalanHamle}", _sagMetin);

            string hedefler = string.Join("   ", _oturum.Hedefler.Turler.Select(tur => $"{HedefAdi(tur)}: {_oturum.Hedefler.Kalan(tur)}"));
            GUI.Label(new Rect(12, 36, genislik - 24, 24), "Hedef — " + hedefler, _metin);

            GUI.Label(new Rect(12, 66, genislik - 110, 24), ParaVeCan(), _metin);
            if (_oturum.Durum == BolumDurumu.Oynaniyor && !_gorunum.Mesgul && GUI.Button(new Rect(genislik - 92, 64, 80, 28), "Çık"))
            {
                _cikisOnayi = true;
            }

            if (_oturum.DolumGostergesiAcik)
            {
                double kapasite = _veri.Parametreler.Sayi("dolum_kapasitesi");
                GUI.Label(new Rect(12, 88, genislik - 24, 20), $"Dolum göstergesi: {_oturum.DolumDegeri:0}/{kapasite:0}", _metin);
            }

            AracCubugu(genislik, yukseklik);
            OyunPencereleri(genislik, yukseklik);
        }

        /// <summary>Oyun içi araçlar (GDD 3.9.4, 9.3.4, 9.3.5): adet, kilit ve satın alma.</summary>
        private void AracCubugu(float genislik, float yukseklik)
        {
            if (_oturum.Durum != BolumDurumu.Oynaniyor || _cikisOnayi)
            {
                return;
            }

            float dugme = (genislik - 24f - 24f) / 4f;
            float x = 12f;
            float y = yukseklik - 44f;
            foreach (var tur in Araclar)
            {
                var alan = new Rect(x, y, dugme, 32);
                x += dugme + 8f;

                if (!_oyuncu.Kapilar.TakviyeAcik(tur, _oyuncu.EnYuksekBolum))
                {
                    GUI.enabled = false;
                    GUI.Button(alan, "kilitli");
                    GUI.enabled = true;
                    continue;
                }

                int adet = _oyuncu.Envanter.Adet(tur);
                bool sinirsiz = _oyuncu.Envanter.SinirsizAktif(tur, Simdi);
                if (adet == 0 && !sinirsiz)
                {
                    if (GUI.Button(alan, $"{TakviyeAdi(tur)}\n+{_oyuncu.Magaza.Fiyat(tur)}") && !_gorunum.Mesgul)
                    {
                        SatinAl(tur);
                    }

                    continue;
                }

                string ad = $"{TakviyeAdi(tur)} ({(sinirsiz ? "∞" : adet.ToString())})";
                if (tur == TakviyeTuru.Karistirma)
                {
                    if (GUI.Button(alan, ad) && !_gorunum.Mesgul)
                    {
                        var olaylar = new List<Olay>();
                        _akis.AracKullan(tur, null, Simdi, olaylar);
                        OlaylariOynat(olaylar);
                    }

                    continue;
                }

                bool secili = _seciliArac == tur;
                if (GUI.Toggle(alan, secili, ad, GUI.skin.button) != secili)
                {
                    _seciliArac = secili ? (TakviyeTuru?)null : tur;
                }
            }

            if (_seciliArac.HasValue)
            {
                GUI.Label(new Rect(12, y - 26, genislik - 24, 24), "Aracı uygulamak için bir hücreye dokun.", _metin);
            }
        }

        private void OyunPencereleri(float genislik, float yukseklik)
        {
            if (_gorunum.Mesgul)
            {
                return;
            }

            float kutuGenisligi = Mathf.Min(360f, genislik - 24f);
            var kutu = new Rect((genislik - kutuGenisligi) * 0.5f, (yukseklik - 190) * 0.5f, kutuGenisligi, 190);
            var baslikAlani = new Rect(kutu.x + 16, kutu.y + 14, kutu.width - 32, 30);
            var metinAlani = new Rect(kutu.x + 16, kutu.y + 48, kutu.width - 32, 70);
            float dugmeGenisligi = (kutu.width - 44f) * 0.5f;
            var ilkDugme = new Rect(kutu.x + 16, kutu.y + 132, dugmeGenisligi, 40);
            var ikinciDugme = new Rect(kutu.x + 28 + dugmeGenisligi, kutu.y + 132, dugmeGenisligi, 40);

            if (_cikisOnayi)
            {
                // GDD 9.1.3: başlamış bir bölümden çıkmak da kayıp sayılır.
                GUI.Box(kutu, GUIContent.none);
                GUI.Label(baslikAlani, "Bölümden çık?", _baslik);
                GUI.Label(metinAlani, "Çıkarsan bölüm kaybedilmiş sayılır ve bir can gider.", _metin);
                if (GUI.Button(ilkDugme, "Devam et")) _cikisOnayi = false;
                if (GUI.Button(ikinciDugme, "Çık")) SonucaGec(cikildi: true);
                return;
            }

            if (_oturum.Durum != BolumDurumu.EkHamleTeklifi)
            {
                return;
            }

            GUI.Box(kutu, GUIContent.none);
            if (_ikinciUyari)
            {
                // GDD 9.12.5: belirli bir bölüme kadar, teklif reddedilince kaybetmeden önce ikinci bir uyarı gösterilir.
                GUI.Label(baslikAlani, "Emin misin?", _baslik);
                GUI.Label(metinAlani, "Vazgeçersen bölüm kaybedilir ve bir can gider.", _metin);
                if (GUI.Button(ilkDugme, "Geri dön")) _ikinciUyari = false;
                if (GUI.Button(ikinciDugme, "Vazgeç")) TeklifiReddet();
                return;
            }

            // GDD 7.4.2, 9.12: ek hamle teklifi; fiyat art arda alımlarda basamaklanır.
            int hamle = _akis.TeklifHamlesi;
            int fiyat = _akis.TeklifFiyati;
            int tasSayisi = _akis.Teklif.PaketTaslari().Count;
            GUI.Label(baslikAlani, "Hamlen bitti", _baslik);
            GUI.Label(
                metinAlani,
                $"{hamle} ek hamle{(tasSayisi > 0 ? $" ve {tasSayisi} özel taş" : string.Empty)}: {fiyat} para.\nParan: {_oyuncu.Cuzdan.Bakiye}",
                _metin);

            if (GUI.Button(ilkDugme, $"Satın al ({fiyat})"))
            {
                var olaylar = new List<Olay>();
                if (_akis.EkHamleSatinAl(olaylar))
                {
                    OlaylariOynat(olaylar);
                }
                else
                {
                    Bildir("Yetersiz para.");
                }
            }

            if (GUI.Button(ikinciDugme, "Vazgeç"))
            {
                if (_akis.Teklif.IkinciUyariGosterilir(_bolum.Kimlik))
                {
                    _ikinciUyari = true;
                }
                else
                {
                    TeklifiReddet();
                }
            }
        }

        private void TeklifiReddet()
        {
            _ikinciUyari = false;
            var olaylar = new List<Olay>();
            _oturum.EkHamleTeklifiniReddet(olaylar);

            // AK-02 B: ret, yedek hamle hakkını son şans olarak kullandırabilir; o zaman bölüm sürer.
            if (_oturum.Durum == BolumDurumu.Kaybedildi)
            {
                SonucaGec();
            }
        }

        // ------------------------------------------------------------------ Sonuç ekranı

        private void SonucEkrani(float genislik, float yukseklik)
        {
            float x = 16f;
            float w = genislik - 32f;
            float y = Mathf.Max(16f, (yukseklik * 0.5f) - 170f);
            bool bastan = _oturum.Durum == BolumDurumu.BastanBaslatilmali;

            string baslik = _sonuc.Kazanildi ? "Bölüm tamamlandı!"
                : bastan ? "Olası hamle kalmadı"
                : "Bölüm kaybedildi";
            GUI.Label(new Rect(x, y, w, 30), baslik, _baslik);
            y += 38f;

            var satirlar = new List<string>();
            if (_sonuc.Kazanildi)
            {
                satirlar.Add($"Kalan hamle: {_oturum.KalanHamle}");
                satirlar.Add($"Para: +{_sonuc.Para}");
                satirlar.Add($"Yıldız: +{_sonuc.Yildiz}");
                if (_sonuc.SezonHatPuani > 0) satirlar.Add($"Sezon hattı: +{_sonuc.SezonHatPuani} puan");
                if (_sonuc.HaftalikHatPuani > 0) satirlar.Add($"Haftalık hat: +{_sonuc.HaftalikHatPuani} puan");
                satirlar.Add($"Kazanma serisi: {_oyuncu.KazanmaSerisi}");
            }
            else if (bastan)
            {
                satirlar.Add("Bölüm, can düşürülmeden baştan başlatılır.");
            }
            else
            {
                satirlar.Add(_sonuc.CanDustu ? "Bir can gitti." : "Can düşmedi.");
            }

            satirlar.Add(ParaVeCan());
            foreach (string satir in satirlar)
            {
                GUI.Label(new Rect(x, y, w, 24), satir, _metin);
                y += 26f;
            }

            y += 12f;
            if (_sonuc.Kazanildi)
            {
                if (GUI.Button(new Rect(x, y, w, 44), "Sonraki bölüm")) BolumOncesineGec(_bolumSirasi + 1);
                y += 52f;
                if (GUI.Button(new Rect(x, y, w, 36), "Yeniden oyna")) BolumOncesineGec(_bolumSirasi);
            }
            else if (GUI.Button(new Rect(x, y, w, 44), bastan ? "Baştan başlat" : "Yeniden dene"))
            {
                BolumOncesineGec(_bolumSirasi);
            }
        }

        // ------------------------------------------------------------------ Adlar

        private static string HedefMetni(Hedef hedef) =>
            hedef.Adet > 0 ? $"{hedef.Adet} {HedefAdi(hedef.Tur)}" : $"bütün {HedefAdi(hedef.Tur)}";

        private static string HedefAdi(string tur)
        {
            if (tur.StartsWith("renk:", StringComparison.Ordinal))
            {
                string[] adlar = { "kırmızı", "mavi", "yeşil", "sarı", "mor", "turuncu" };
                return int.TryParse(tur.Substring(5), out int renk) && renk < adlar.Length ? adlar[renk] : tur;
            }

            switch (tur)
            {
                case "katmanli_blok": return "blok";
                case "zemin": return "zemin";
                case "para_tasi": return "para taşı";
                case "toplanabilir": return "toplanabilir";
                default: return tur;
            }
        }

        private static string TakviyeAdi(TakviyeTuru tur)
        {
            switch (tur)
            {
                case TakviyeTuru.Cizgi: return "Çizgi taşı";
                case TakviyeTuru.Bomba: return "Bomba taşı";
                case TakviyeTuru.RenkKuresi: return "Renk küresi";
                case TakviyeTuru.Gudumlu: return "Güdümlü taş";
                case TakviyeTuru.TekHucre: return "Tek hücre";
                case TakviyeTuru.Satir: return "Satır";
                case TakviyeTuru.Sutun: return "Sütun";
                default: return "Karıştır";
            }
        }

        private static string EtiketAdi(ZorlukEtiketi etiket)
        {
            switch (etiket)
            {
                case ZorlukEtiketi.Zor: return "Zor";
                case ZorlukEtiketi.CokZor: return "Çok zor";
                case ZorlukEtiketi.AsiriZor: return "Aşırı zor";
                default: return "Normal";
            }
        }
    }
}
