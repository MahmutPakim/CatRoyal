using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;
using UnityEngine;

namespace CatRoyal.Sunum
{
    /// <summary>
    /// Tahtanın görünümü. Çekirdeği yalnızca olay listesi üzerinden izler: bir hamlenin olaylarını sırayla oynatır,
    /// sonunda görünümü tahtayla eşitler. Bütün süreler buradadır ve sunum_ parametrelerinden gelir (GDD 0.4);
    /// oyunun sonucu bu sürelerden hiçbirine bağlı değildir.
    /// </summary>
    public sealed class TahtaGorunumu : MonoBehaviour
    {
        private static readonly Color[] TasRenkleri =
        {
            new Color(0.90f, 0.26f, 0.26f),
            new Color(0.26f, 0.52f, 0.95f),
            new Color(0.30f, 0.78f, 0.38f),
            new Color(0.97f, 0.82f, 0.22f),
            new Color(0.68f, 0.40f, 0.90f),
            new Color(0.98f, 0.58f, 0.20f),
        };

        private readonly Dictionary<int, GameObject> _taslar = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Konum> _tasKonumlari = new Dictionary<int, Konum>();
        private readonly List<GameObject> _hucreSusleri = new List<GameObject>();

        private Tahta _tahta;
        private Material _malzeme;
        private Transform _zeminKoku;
        private Transform _tasKoku;
        private Transform _ustKoku;

        private float _adimSuresi;
        private float _halkaSuresi;
        private float _dusmeHizi;

        /// <summary>Olaylar oynatılıyor mu? Oynatma sürerken girdi alınmaz.</summary>
        public bool Mesgul { get; private set; }

        /// <summary>
        /// Oturumun o an oynanan tahtası. Çok ekranlı bölümde (GDD 8.9) ekran değişince görünüm bu tahtaya geçer.
        /// </summary>
        public System.Func<Tahta> GuncelTahta { get; set; }

        public void Kur(Tahta tahta, ParametreTablosu parametreler)
        {
            _tahta = tahta;

            // Süreler yalnızca sunuma aittir; çekirdek bu parametreleri okumaz.
            _adimSuresi = 1f / (float)parametreler.Sayi("sunum_cizgi_ilerleme_hizi");
            _halkaSuresi = (float)parametreler.Sayi("sunum_bomba_halka_arasi");
            _dusmeHizi = (float)parametreler.Sayi("sunum_dusme_azami_hizi");

            if (_malzeme == null)
            {
                var golgelendirici = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                _malzeme = new Material(golgelendirici);
            }

            _zeminKoku = KokOlustur(_zeminKoku, "Zemin");
            _tasKoku = KokOlustur(_tasKoku, "Taslar");
            _ustKoku = KokOlustur(_ustKoku, "UstKatmanlar");
            Esitle();
        }

        private Transform KokOlustur(Transform eski, string ad)
        {
            if (eski != null)
            {
                Destroy(eski.gameObject);
            }

            var kok = new GameObject(ad).transform;
            kok.SetParent(transform, false);
            return kok;
        }

        // ------------------------------------------------------------------ Koordinatlar

        /// <summary>Hücrenin dünya konumu: satır 1 en üstte, sütun 1 en solda (GDD 0.5).</summary>
        public Vector3 DunyaKonumu(Konum konum) => transform.position + new Vector3(
            (konum.Sutun - 1) - ((_tahta.SutunSayisi - 1) * 0.5f),
            ((_tahta.SatirSayisi - 1) * 0.5f) - (konum.Satir - 1),
            0f);

        public Konum? HucreBul(Vector3 dunya)
        {
            var yerel = dunya - transform.position;
            int sutun = Mathf.RoundToInt(yerel.x + ((_tahta.SutunSayisi - 1) * 0.5f)) + 1;
            int satir = Mathf.RoundToInt(((_tahta.SatirSayisi - 1) * 0.5f) - yerel.y) + 1;
            var konum = new Konum(satir, sutun);
            return _tahta.Oynanabilir(konum) ? konum : (Konum?)null;
        }

        // ------------------------------------------------------------------ Eşitleme

        /// <summary>Görünümü tahtanın o anki durumundan baştan kurar.</summary>
        public void Esitle()
        {
            foreach (var nesne in _taslar.Values)
            {
                if (nesne != null) Destroy(nesne);
            }

            _taslar.Clear();
            _tasKonumlari.Clear();
            HucreSusleriniYenile();

            foreach (var konum in _tahta.TumKonumlar())
            {
                var tas = _tahta[konum].Tas;
                if (_tahta[konum].Oynanabilir && tas != null)
                {
                    TasOlustur(tas, DunyaKonumu(konum), konum);
                }
            }
        }

        /// <summary>Hücre arka planları, zemin ve üst katmanlar tahtadan yeniden çizilir.</summary>
        private void HucreSusleriniYenile()
        {
            foreach (var nesne in _hucreSusleri)
            {
                if (nesne != null) Destroy(nesne);
            }

            _hucreSusleri.Clear();

            foreach (var konum in _tahta.TumKonumlar())
            {
                var hucre = _tahta[konum];
                if (!hucre.Oynanabilir)
                {
                    continue;
                }

                var merkez = DunyaKonumu(konum);
                var zeminRengi = new Color(0.16f, 0.18f, 0.24f);
                if (hucre.AltKatman != null)
                {
                    zeminRengi = hucre.AltKatman.Tanim.Yayilir ? new Color(0.20f, 0.42f, 0.30f) : new Color(0.55f, 0.43f, 0.26f);
                }

                var zemin = Sekil("Hucre", SekilUretici.Kare, zeminRengi, merkez, 0.96f, 0, _zeminKoku);
                _hucreSusleri.Add(zemin);
                if (hucre.AltKatman != null && !hucre.AltKatman.Tanim.Yayilir)
                {
                    NoktalariEkle(zemin.transform, hucre.AltKatman.Katman, new Color(1f, 0.95f, 0.8f, 0.9f), 1, -0.36f);
                }

                if (hucre.UstKatman != null)
                {
                    var ust = Sekil("UstKatman", UstKatmanSekli(hucre.UstKatman.Tanim), UstKatmanRengi(hucre.UstKatman.Tanim), merkez, 0.98f, 20, _ustKoku);
                    NoktalariEkle(ust.transform, hucre.UstKatman.Katman, Color.white, 21, 0.36f);
                    _hucreSusleri.Add(ust);
                }
            }
        }

        private static Sprite UstKatmanSekli(KatmanTanimi tanim) =>
            tanim.Arketip == KatmanArketipi.Kilit ? SekilUretici.Cerceve : SekilUretici.Kare;

        private static Color UstKatmanRengi(KatmanTanimi tanim)
        {
            switch (tanim.Arketip)
            {
                case KatmanArketipi.Kilit: return new Color(0.85f, 0.88f, 0.95f, 1f);
                case KatmanArketipi.YapiskanKaplama: return new Color(1f, 0.55f, 0.75f, 0.55f);
                case KatmanArketipi.Ortu: return new Color(0.20f, 0.26f, 0.45f, 1f);
                default: return new Color(1f, 1f, 1f, 0.55f);
            }
        }

        // ------------------------------------------------------------------ Taş nesneleri

        private GameObject TasOlustur(Tas tas, Vector3 konum, Konum? hucre)
        {
            if (_taslar.TryGetValue(tas.Kimlik, out var eski) && eski != null)
            {
                Destroy(eski);
            }

            GameObject nesne;
            switch (tas.Tur)
            {
                case TasTuru.Normal:
                    nesne = Sekil("Tas", SekilUretici.Daire, TasRenkleri[tas.Renk % TasRenkleri.Length], konum, 0.82f, 10, _tasKoku);
                    break;

                case TasTuru.Cizgi:
                    nesne = Sekil("Cizgi", SekilUretici.Kare, new Color(0.95f, 0.95f, 1f), konum, 0.8f, 10, _tasKoku);
                    var serit = Sekil("Yon", SekilUretici.Serit, new Color(0.15f, 0.2f, 0.45f), konum, 1f, 11, nesne.transform);
                    serit.transform.localScale = Vector3.one;
                    serit.transform.localRotation = Quaternion.Euler(0, 0, tas.CizgiYonu == Yon.Dikey ? 90f : 0f);
                    break;

                case TasTuru.Bomba:
                    nesne = Sekil("Bomba", SekilUretici.Daire, new Color(0.18f, 0.18f, 0.2f), konum, 0.84f, 10, _tasKoku);
                    Sekil("Fitil", SekilUretici.Daire, new Color(1f, 0.4f, 0.2f), konum, 0.4f, 11, nesne.transform).transform.localScale = Vector3.one * 0.4f;
                    break;

                case TasTuru.Gudumlu:
                    nesne = Sekil("Gudumlu", SekilUretici.Elmas, new Color(0.3f, 0.9f, 0.95f), konum, 0.95f, 10, _tasKoku);
                    break;

                case TasTuru.RenkKuresi:
                    nesne = Sekil("RenkKuresi", SekilUretici.Halka, Color.white, konum, 0.9f, 10, _tasKoku);
                    Sekil("Cekirdek", SekilUretici.Daire, new Color(0.95f, 0.3f, 0.85f), konum, 0.45f, 11, nesne.transform).transform.localScale = Vector3.one * 0.45f;
                    break;

                default:
                    nesne = EngelOlustur(tas, konum);
                    break;
            }

            _taslar[tas.Kimlik] = nesne;
            if (hucre.HasValue)
            {
                _tasKonumlari[tas.Kimlik] = hucre.Value;
            }
            else
            {
                _tasKonumlari.Remove(tas.Kimlik);
            }

            return nesne;
        }

        private GameObject EngelOlustur(Tas tas, Vector3 konum)
        {
            Sprite sekil = SekilUretici.Kare;
            Color renk;
            float olcek = 0.9f;
            switch (tas.Engel.Arketip)
            {
                case EngelArketipi.SertBlok: renk = new Color(0.5f, 0.52f, 0.58f); break;
                case EngelArketipi.DusenKirilabilir: renk = new Color(0.25f, 0.62f, 0.6f); olcek = 0.74f; break;
                case EngelArketipi.AsagiInenToplanabilir: renk = new Color(1f, 0.78f, 0.2f); sekil = SekilUretici.Elmas; olcek = 0.86f; break;
                case EngelArketipi.CokluVurusBlogu: renk = new Color(0.62f, 0.36f, 0.3f); break;
                case EngelArketipi.KaydirilamayanDusenBlok: renk = new Color(0.4f, 0.42f, 0.5f); olcek = 0.8f; break;
                case EngelArketipi.ParaTasi: renk = new Color(1f, 0.84f, 0.3f); sekil = SekilUretici.Halka; break;
                case EngelArketipi.Uretici: renk = new Color(0.58f, 0.34f, 0.7f); break;
                case EngelArketipi.YayilanTehlike: renk = new Color(0.18f, 0.4f, 0.22f); break;
                default: renk = new Color(0.58f, 0.42f, 0.28f); break;
            }

            var nesne = Sekil("Engel", sekil, renk, konum, olcek, 10, _tasKoku);
            NoktalariEkle(nesne.transform, tas.Katman, Color.white, 12, 0f);
            return nesne;
        }

        /// <summary>Kalan katman sayısını küçük noktalarla gösterir.</summary>
        private void NoktalariEkle(Transform ust, int adet, Color renk, int sira, float y)
        {
            for (int i = ust.childCount - 1; i >= 0; i--)
            {
                if (ust.GetChild(i).name == "Nokta") Destroy(ust.GetChild(i).gameObject);
            }

            if (adet <= 1)
            {
                return;
            }

            for (int i = 0; i < adet; i++)
            {
                var nokta = Sekil("Nokta", SekilUretici.Daire, renk, ust.position, 1f, sira, ust);
                nokta.transform.localScale = Vector3.one * 0.16f;
                nokta.transform.localPosition = new Vector3((i - ((adet - 1) * 0.5f)) * 0.2f, y, 0f);
            }
        }

        private GameObject Sekil(string ad, Sprite sprite, Color renk, Vector3 konum, float olcek, int sira, Transform ust)
        {
            var nesne = new GameObject(ad);
            nesne.transform.SetParent(ust, false);
            nesne.transform.position = konum;
            nesne.transform.localScale = Vector3.one * olcek;
            var cizici = nesne.AddComponent<SpriteRenderer>();
            cizici.sprite = sprite;
            cizici.color = renk;
            cizici.sortingOrder = sira;
            cizici.sharedMaterial = _malzeme;
            return nesne;
        }

        private GameObject TasNesnesi(int kimlik) =>
            _taslar.TryGetValue(kimlik, out var nesne) && nesne != null ? nesne : null;

        private GameObject HucredekiTas(Konum konum)
        {
            foreach (var cift in _tasKonumlari)
            {
                if (cift.Value == konum)
                {
                    return TasNesnesi(cift.Key);
                }
            }

            return null;
        }

        private void TasiKaldir(int kimlik)
        {
            var nesne = TasNesnesi(kimlik);
            _taslar.Remove(kimlik);
            _tasKonumlari.Remove(kimlik);
            if (nesne != null)
            {
                StartCoroutine(Kucult(nesne));
            }
        }

        // ------------------------------------------------------------------ Olay oynatma

        /// <summary>Bir hamlenin olaylarını sırayla oynatır; bitince görünümü tahtayla eşitler.</summary>
        public IEnumerator Oynat(IReadOnlyList<Olay> olaylar)
        {
            Mesgul = true;
            int i = 0;
            while (i < olaylar.Count)
            {
                var olay = olaylar[i];

                // GDD 8.9.1: ekran değişti. Sonraki olaylar yeni ekranın tahtasına göredir; görünüm doğrudan
                // yeni tahtanın durulmuş hâline geçer.
                if (olay is EkranDegisti && GuncelTahta != null)
                {
                    yield return new WaitForSeconds(0.4f);
                    _tahta = GuncelTahta();
                    break;
                }

                // Ardışık düşme olayları tek dalga olarak birlikte oynar.
                if (DusmeOlayi(olay))
                {
                    int son = i;
                    while (son < olaylar.Count && DusmeOlayi(olaylar[son])) son++;
                    yield return DusmeleriOynat(olaylar.Skip(i).Take(son - i).ToList());
                    i = son;
                    continue;
                }

                // Aynı mantıksal adımdaki olaylar aynı anda olmuş sayılır (GDD 3.2.3, 3.3.2).
                if (olay is AdimliOlay adimli)
                {
                    int son = i;
                    while (son < olaylar.Count && olaylar[son] is AdimliOlay sonraki && sonraki.Adim == adimli.Adim) son++;
                    bool gorunur = false;
                    for (int k = i; k < son; k++)
                    {
                        gorunur |= AdimliOlayiUygula((AdimliOlay)olaylar[k]);
                    }

                    if (gorunur)
                    {
                        yield return new WaitForSeconds(Mathf.Max(_adimSuresi, _halkaSuresi));
                    }

                    i = son;
                    continue;
                }

                yield return TekOlayiOynat(olay);
                i++;
            }

            yield return new WaitForSeconds(0.05f);
            Esitle();
            Mesgul = false;
        }

        private static bool DusmeOlayi(Olay olay) => olay is TasDustu || olay is TasGirdi || olay is TasCaprazKaydi;

        private IEnumerator TekOlayiOynat(Olay olay)
        {
            switch (olay)
            {
                case TaslarYerDegistirdi degisim:
                    yield return YerDegistir(degisim.Cikilan, degisim.Gidilen, geriDon: false);
                    break;

                case GecersizHamle gecersiz:
                    // GDD 2.3.4: taşlar "yanlış hamle" animasyonuyla geri döner.
                    yield return YerDegistir(gecersiz.Cikilan, gecersiz.Gidilen, geriDon: true);
                    break;

                case ImkansizHamle imkansiz:
                    // GDD 2.3.2: "imkânsız hamle" geri bildirimi.
                    yield return Salla(HucredekiTas(imkansiz.Cikilan));
                    break;

                case GrupPatladi grup:
                    foreach (int kimlik in grup.PatlayanTasKimlikleri)
                    {
                        TasiKaldir(kimlik);
                    }

                    yield return new WaitForSeconds(0.14f);
                    break;

                case OzelTasDogdu dogum:
                    yield return Buyut(TasOlustur(dogum.Tas, DunyaKonumu(dogum.Konum), dogum.Konum));
                    break;

                case KatmanBirakildi _:
                case TahtaKaristirildi _:
                    Esitle();
                    yield return new WaitForSeconds(0.2f);
                    break;

                case TehlikeYayildi tehlike:
                    var eski = HucredekiTas(tehlike.Nereye);
                    if (eski != null) Destroy(eski);
                    yield return Buyut(TasOlustur(tehlike.Tehlike, DunyaKonumu(tehlike.Nereye), tehlike.Nereye));
                    break;

                case OgeToplandi toplanan:
                    TasiKaldir(toplanan.Oge.Kimlik);
                    yield return new WaitForSeconds(0.12f);
                    break;

                case RenkKuresiFirlatildi firlatma:
                    var hedeftekiTas = HucredekiTas(firlatma.Konum);
                    if (hedeftekiTas != null) Destroy(hedeftekiTas);
                    yield return Buyut(TasOlustur(firlatma.Kure, DunyaKonumu(firlatma.Konum), firlatma.Konum));
                    break;

                case TakviyeKondu takviye:
                    var yerindeki = HucredekiTas(takviye.Konum);
                    if (yerindeki != null) Destroy(yerindeki);
                    yield return Buyut(TasOlustur(takviye.Tas, DunyaKonumu(takviye.Konum), takviye.Konum));
                    break;
            }
        }

        /// <returns>Olay ekranda bir şey değiştirdiyse true; adım beklemesi yalnızca o zaman yapılır.</returns>
        private bool AdimliOlayiUygula(AdimliOlay olay)
        {
            switch (olay)
            {
                case HucreVuruldu vurus:
                    StartCoroutine(Parlat(vurus.Konum));
                    return true;

                case TasPatladi patlama:
                    TasiKaldir(patlama.Tas.Kimlik);
                    return true;

                case OzelTasEtkinlesti etkinlesme:
                    if (etkinlesme.Tas.Tur != TasTuru.RenkKuresi)
                    {
                        TasiKaldir(etkinlesme.Tas.Kimlik);
                    }

                    return true;

                case RenkKuresiTukendi tukenme:
                    TasiKaldir(tukenme.Kure.Kimlik);
                    return true;

                case EngelHasarAldi hasar:
                    var engel = TasNesnesi(hasar.Engel.Kimlik);
                    if (hasar.Kirildi)
                    {
                        TasiKaldir(hasar.Engel.Kimlik);
                    }
                    else if (engel != null)
                    {
                        NoktalariEkle(engel.transform, hasar.KalanKatman, Color.white, 12, 0f);
                        StartCoroutine(Salla(engel));
                    }

                    return true;

                case TasDonustu donusum:
                    if (donusum.EskiTas != null)
                    {
                        var eski = TasNesnesi(donusum.EskiTas.Kimlik);
                        _taslar.Remove(donusum.EskiTas.Kimlik);
                        _tasKonumlari.Remove(donusum.EskiTas.Kimlik);
                        if (eski != null) Destroy(eski);
                    }

                    TasOlustur(donusum.YeniTas, DunyaKonumu(donusum.Konum), donusum.Konum);
                    return true;

                case GudumluKalkti kalkis:
                    if (kalkis.Hedef.HasValue)
                    {
                        StartCoroutine(Uc(DunyaKonumu(kalkis.Kalkis), DunyaKonumu(kalkis.Hedef.Value), new Color(0.3f, 0.9f, 0.95f)));
                    }

                    return true;

                case IsinGonderildi isin:
                    StartCoroutine(Uc(DunyaKonumu(isin.KureHucresi), DunyaKonumu(isin.Hedef), new Color(1f, 0.6f, 0.95f), 0.25f));
                    return true;

                case KatmanHasarAldi _:
                case ZeminYayildi _:
                    // Katmanlar tahtadan okunur; olay sırasında yeniden çizilir.
                    HucreSusleriniYenile();
                    return true;

                default:
                    return false;
            }
        }

        private IEnumerator DusmeleriOynat(List<Olay> dusmeler)
        {
            var hareketler = new List<(GameObject Nesne, Vector3 Bas, Vector3 Son)>();
            float enUzun = 0f;

            foreach (var olay in dusmeler)
            {
                Tas tas;
                Vector3 bas;
                Konum hedef;
                switch (olay)
                {
                    case TasDustu dusme:
                        tas = dusme.Tas;
                        bas = DunyaKonumu(dusme.Nereden);
                        hedef = dusme.Nereye;
                        break;
                    case TasCaprazKaydi kayma:
                        tas = kayma.Tas;
                        bas = DunyaKonumu(kayma.Nereden);
                        hedef = kayma.Nereye;
                        break;
                    default:
                        var giris = (TasGirdi)olay;
                        tas = giris.Tas;

                        // Yeni taş giriş hücresinin üstünden gelir; girişten sırayla çıkanlar üst üste dizilir.
                        bas = DunyaKonumu(giris.Giris) + (Vector3.up * (1f + (giris.Nereye.Satir - giris.Giris.Satir)));
                        hedef = giris.Nereye;
                        break;
                }

                var nesne = TasNesnesi(tas.Kimlik);
                if (nesne == null)
                {
                    nesne = TasOlustur(tas, bas, hedef);
                }
                else
                {
                    // Aynı dalgada ikinci kez hareket eden taş bulunduğu yerden devam eder.
                    bas = nesne.transform.position;
                }

                _tasKonumlari[tas.Kimlik] = hedef;
                var son = DunyaKonumu(hedef);
                hareketler.Add((nesne, bas, son));
                enUzun = Mathf.Max(enUzun, Vector3.Distance(bas, son));
            }

            float sure = Mathf.Clamp(enUzun / _dusmeHizi, 0.08f, 0.6f);
            for (float t = 0f; t < sure; t += Time.deltaTime)
            {
                float oran = t / sure;
                oran *= oran;
                foreach (var h in hareketler)
                {
                    if (h.Nesne != null) h.Nesne.transform.position = Vector3.Lerp(h.Bas, h.Son, oran);
                }

                yield return null;
            }

            foreach (var h in hareketler)
            {
                if (h.Nesne != null) h.Nesne.transform.position = h.Son;
            }
        }

        // ------------------------------------------------------------------ Küçük animasyonlar

        private IEnumerator YerDegistir(Konum a, Konum b, bool geriDon)
        {
            var birinci = HucredekiTas(a);
            var ikinci = HucredekiTas(b);
            var konumA = DunyaKonumu(a);
            var konumB = DunyaKonumu(b);
            const float sure = 0.12f;

            for (int tur = 0; tur < (geriDon ? 2 : 1); tur++)
            {
                for (float t = 0f; t < sure; t += Time.deltaTime)
                {
                    float oran = tur == 0 ? t / sure : 1f - (t / sure);
                    if (birinci != null) birinci.transform.position = Vector3.Lerp(konumA, konumB, oran);
                    if (ikinci != null) ikinci.transform.position = Vector3.Lerp(konumB, konumA, oran);
                    yield return null;
                }
            }

            if (geriDon)
            {
                if (birinci != null) birinci.transform.position = konumA;
                if (ikinci != null) ikinci.transform.position = konumB;
                yield break;
            }

            if (birinci != null) birinci.transform.position = konumB;
            if (ikinci != null) ikinci.transform.position = konumA;

            int? kimlikA = null;
            int? kimlikB = null;
            foreach (var cift in _tasKonumlari)
            {
                if (cift.Value == a) kimlikA = cift.Key;
                if (cift.Value == b) kimlikB = cift.Key;
            }

            if (kimlikA.HasValue) _tasKonumlari[kimlikA.Value] = b;
            if (kimlikB.HasValue) _tasKonumlari[kimlikB.Value] = a;
        }

        private static IEnumerator Kucult(GameObject nesne)
        {
            var baslangic = nesne.transform.localScale;
            const float sure = 0.12f;
            for (float t = 0f; t < sure && nesne != null; t += Time.deltaTime)
            {
                nesne.transform.localScale = baslangic * (1f - (t / sure));
                yield return null;
            }

            if (nesne != null) Destroy(nesne);
        }

        private static IEnumerator Buyut(GameObject nesne)
        {
            var hedef = nesne.transform.localScale;
            const float sure = 0.14f;
            for (float t = 0f; t < sure && nesne != null; t += Time.deltaTime)
            {
                nesne.transform.localScale = hedef * Mathf.Lerp(0.2f, 1f, t / sure);
                yield return null;
            }

            if (nesne != null) nesne.transform.localScale = hedef;
        }

        private static IEnumerator Salla(GameObject nesne)
        {
            if (nesne == null)
            {
                yield break;
            }

            var merkez = nesne.transform.position;
            const float sure = 0.18f;
            for (float t = 0f; t < sure && nesne != null; t += Time.deltaTime)
            {
                nesne.transform.position = merkez + (Vector3.right * (Mathf.Sin(t * 70f) * 0.06f));
                yield return null;
            }

            if (nesne != null) nesne.transform.position = merkez;
        }

        private IEnumerator Parlat(Konum konum)
        {
            var parlama = Sekil("Parlama", SekilUretici.Kare, new Color(1f, 1f, 1f, 0.7f), DunyaKonumu(konum), 0.96f, 30, _ustKoku);
            var cizici = parlama.GetComponent<SpriteRenderer>();
            const float sure = 0.16f;
            for (float t = 0f; t < sure; t += Time.deltaTime)
            {
                cizici.color = new Color(1f, 1f, 1f, 0.7f * (1f - (t / sure)));
                yield return null;
            }

            Destroy(parlama);
        }

        private IEnumerator Uc(Vector3 bas, Vector3 son, Color renk, float olcek = 0.5f)
        {
            var ucan = Sekil("Ucan", SekilUretici.Elmas, renk, bas, olcek, 40, _ustKoku);
            float sure = Mathf.Max(_adimSuresi, 0.12f);
            for (float t = 0f; t < sure; t += Time.deltaTime)
            {
                ucan.transform.position = Vector3.Lerp(bas, son, t / sure);
                yield return null;
            }

            Destroy(ucan);
        }

        /// <summary>İpucu: olası bir hamlenin iki taşını kısa süre vurgular (GDD 5.8.2).</summary>
        public void IpucuGoster(Konum a, Konum b)
        {
            StartCoroutine(Salla(HucredekiTas(a)));
            StartCoroutine(Salla(HucredekiTas(b)));
        }
    }
}
