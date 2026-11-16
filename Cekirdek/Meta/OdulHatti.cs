using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Ödül hattı (GDD 9.8, 9.9): basamaklı ödül hattı ve en sonda birikim haznesi. Sezon hattı ve haftalık hat
    /// aynı yapıyı kullanır; ödül içerikleri ve süre sunucudan gelir.
    /// </summary>
    public sealed class OdulHatti
    {
        private readonly IReadOnlyList<int> _hedefler;
        private readonly int _hazneAsamaPuani;
        private readonly int _hazneAsamaParasi;
        private readonly int _hazneHedefi;
        private readonly HashSet<int> _alinanUcretsiz = new HashSet<int>();
        private readonly HashSet<int> _alinanPremium = new HashSet<int>();

        public OdulHatti(
            IReadOnlyList<int> basamakHedefleri, int hazneAsamaPuani, int hazneAsamaParasi, int hazneHedefi, int kilometreTasiAraligi = 0)
        {
            KilometreTasiAraligi = Math.Max(0, kilometreTasiAraligi);
            if (basamakHedefleri.Count == 0 || basamakHedefleri.Any(h => h < 1))
            {
                throw new ArgumentException("Basamak hedefleri boş olamaz ve her hedef en az 1 olmalı.");
            }

            _hedefler = basamakHedefleri;
            _hazneAsamaPuani = hazneAsamaPuani;
            _hazneAsamaParasi = hazneAsamaParasi;
            _hazneHedefi = hazneHedefi;
        }

        /// <summary>Sezon ödül hattı (GDD 9.8.2, 9.8.8).</summary>
        /// <param name="p">Parametre tablosu.</param>
        /// <param name="ozelHedefler">Deney grubunun basamak tablosu (GDD 9.8.11); verilmezse parametredeki tablo.</param>
        public static OdulHatti Sezon(ParametreTablosu p, IReadOnlyList<int>? ozelHedefler = null)
        {
            var hedefler = ozelHedefler ?? SezonBasamakHedefleri(p);
            int beklenen = p.TamSayi(MetaParametreAdlari.SezonHattiBasamakSayisi);
            if (ozelHedefler == null && hedefler.Count != beklenen)
            {
                throw new ParametreHatasi(
                    $"'{MetaParametreAdlari.SezonHattiBasamakHedefleri}' {hedefler.Count} basamak veriyor; "
                    + $"'{MetaParametreAdlari.SezonHattiBasamakSayisi}' {beklenen} bekliyor.");
            }

            return new OdulHatti(
                hedefler,
                p.TamSayi(MetaParametreAdlari.HazneAsamaPuani),
                p.TamSayi(MetaParametreAdlari.HazneAsamaParasi),
                p.TamSayi(MetaParametreAdlari.SezonHazneHedefi),
                p.TamSayi(MetaParametreAdlari.KilometreTasiAraligi));
        }

        /// <summary>Haftalık ödül hattı (GDD 9.9.2); aşama puanı sezon hattıyla ortaktır.</summary>
        /// <param name="p">Parametre tablosu.</param>
        /// <param name="ozelHedefler">Oyuncuya özel basamak tablosu (GDD 9.9.7); verilmezse parametredeki tablo.</param>
        public static OdulHatti Haftalik(ParametreTablosu p, IReadOnlyList<int>? ozelHedefler = null)
        {
            var hedefler = ozelHedefler ?? p.TamSayiListesi(MetaParametreAdlari.HaftalikHatBasamakHedefleri);
            int beklenen = p.TamSayi(MetaParametreAdlari.HaftalikHatBasamakSayisi);
            if (ozelHedefler == null && hedefler.Count != beklenen)
            {
                throw new ParametreHatasi(
                    $"'{MetaParametreAdlari.HaftalikHatBasamakHedefleri}' {hedefler.Count} basamak veriyor; "
                    + $"'{MetaParametreAdlari.HaftalikHatBasamakSayisi}' {beklenen} bekliyor.");
            }

            return new OdulHatti(
                hedefler,
                p.TamSayi(MetaParametreAdlari.HazneAsamaPuani),
                p.TamSayi(MetaParametreAdlari.HaftalikHazneAsamaParasi),
                p.TamSayi(MetaParametreAdlari.HaftalikHazneHedefi),
                p.TamSayi(MetaParametreAdlari.HaftalikKilometreTasi));
        }

        /// <summary>
        /// parametre: sezon_hatti_basamak_hedefleri serbest metindir (1. aşama, karar 8: ada özel ayrıştırıcı):
        /// "İlk 5 basamak: 2, 2, 4, 4, 6; sonra beşer basamak: 10, 16, 22, 28 (…)". İlk bölümdeki sayılar birer
        /// basamaktır; "beşer" yazan bölümdeki her sayı beş basamak için geçerlidir.
        /// </summary>
        public static IReadOnlyList<int> SezonBasamakHedefleri(ParametreTablosu p)
        {
            string metin = Regex.Replace(p.Metin(MetaParametreAdlari.SezonHattiBasamakHedefleri), @"\([^)]*\)", string.Empty);
            var hedefler = new List<int>();
            foreach (string bolum in metin.Split(';'))
            {
                int ikiNokta = bolum.IndexOf(':');
                if (ikiNokta < 0)
                {
                    throw new ParametreHatasi($"'{MetaParametreAdlari.SezonHattiBasamakHedefleri}' beklenen biçimde değil.");
                }

                int tekrar = bolum.Substring(0, ikiNokta).Contains("beşer") ? 5 : 1;
                foreach (string parca in bolum.Substring(ikiNokta + 1).Split(','))
                {
                    if (!int.TryParse(parca.Trim(), out int hedef))
                    {
                        throw new ParametreHatasi($"'{MetaParametreAdlari.SezonHattiBasamakHedefleri}': '{parca.Trim()}' sayı değil.");
                    }

                    hedefler.AddRange(Enumerable.Repeat(hedef, tekrar));
                }
            }

            return hedefler;
        }

        public int BasamakSayisi => _hedefler.Count;

        /// <summary>Kilometre taşları arasındaki basamak sayısı (GDD 9.8.3, 9.9.2); 0 ise kilometre taşı yoktur.</summary>
        public int KilometreTasiAraligi { get; }

        /// <summary>
        /// Basamak (0 tabanlı) kilometre taşı mı? Kilometre taşlarında iki hatta da daha büyük paketler ve kozmetikler
        /// verilir (GDD 9.8.3); içerikleri ödül tablosundadır.
        /// </summary>
        public bool KilometreTasi(int basamak) =>
            KilometreTasiAraligi > 0 && basamak >= 0 && basamak < _hedefler.Count && (basamak + 1) % KilometreTasiAraligi == 0;

        private IReadOnlyList<OdulPaketi?> _ucretsizOduller = Array.Empty<OdulPaketi?>();
        private IReadOnlyList<OdulPaketi?> _premiumOduller = Array.Empty<OdulPaketi?>();

        /// <summary>
        /// Basamakların ödül içerikleri; sunucudan gelir (GDD 9.8.11, 9.8.12). Listeler basamak sırasıyladır; ödülü
        /// olmayan basamak null ya da eksik bırakılır. Kaydedilmez: her açılışta yeniden verilir.
        /// </summary>
        public void OdulleriAyarla(IReadOnlyList<OdulPaketi?> ucretsiz, IReadOnlyList<OdulPaketi?> premium)
        {
            _ucretsizOduller = ucretsiz;
            _premiumOduller = premium;
        }

        public OdulPaketi? UcretsizOdul(int basamak) => basamak >= 0 && basamak < _ucretsizOduller.Count ? _ucretsizOduller[basamak] : null;

        public OdulPaketi? PremiumOdul(int basamak) => basamak >= 0 && basamak < _premiumOduller.Count ? _premiumOduller[basamak] : null;

        /// <summary>
        /// Alınabilir durumdaki bütün ödülleri alınmış sayar ve içeriklerini döndürür (GDD 9.8.10, 9.9.6). Dolmuş
        /// hazne de alınır. Yorum: dolmamış hazne "alınmamış ödül" değildir; dönem sonunda verilmez.
        /// </summary>
        public List<OdulPaketi> AlinmamislariAl()
        {
            var oduller = new List<OdulPaketi>();
            for (int i = 0; i < Basamak; i++)
            {
                if (UcretsizOduluAl(i) && UcretsizOdul(i) is { } ucretsiz) oduller.Add(ucretsiz);
                if (PremiumOduluAl(i) && PremiumOdul(i) is { } premium) oduller.Add(premium);
            }

            int hazne = HazneyiAl();
            if (hazne > 0)
            {
                oduller.Add(new OdulPaketi { Para = hazne });
            }

            return oduller;
        }

        /// <summary>Geçilen basamak sayısı; son ödül basamağı geçildiğinde <see cref="BasamakSayisi"/> olur.</summary>
        public int Basamak { get; private set; }

        /// <summary>İçinde bulunulan basamakta biriken puan. Hedefler birikimli değildir (GDD 9.8.2).</summary>
        public int BasamakPuani { get; private set; }

        public int? SiradakiHedef => Basamak < _hedefler.Count ? _hedefler[Basamak] : (int?)null;

        /// <summary>Premium (haftalık hatta ücretli) hat satın alındı mı?</summary>
        public bool Premium { get; private set; }

        public int HaznePuani { get; private set; }

        public bool SonBasamakGecildi => Basamak >= _hedefler.Count;

        /// <summary>
        /// Puan ekler. Basamak atlanınca puan sıfırdan sayılır; hedefi aşan artık puan bir sonraki basamağa devreder,
        /// bir kazanım birden fazla basamak atlatabilir (GDD 9.8.2). Son ödül basamağından sonra kazanılan puanlar
        /// hazneye akar (9.8.8).
        /// </summary>
        public void PuanEkle(int puan)
        {
            if (puan < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(puan), "Puan negatif olamaz.");
            }

            while (puan > 0 && !SonBasamakGecildi)
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

            if (puan > 0)
            {
                HaznePuani = Math.Min(_hazneHedefi, HaznePuani + puan);
            }
        }

        /// <summary>Basamak atlanınca o basamağın ücretsiz ödülü alınabilir hale gelir (GDD 9.8.6). Basamak 0 tabanlıdır.</summary>
        public bool UcretsizOdulAlinabilir(int basamak) => basamak >= 0 && basamak < Basamak && !_alinanUcretsiz.Contains(basamak);

        /// <summary>
        /// Premium hat satın alınınca daha önce geçilmiş basamakların premium ödülleri de alınabilir hale gelir (GDD 9.8.7).
        /// </summary>
        public bool PremiumOdulAlinabilir(int basamak) =>
            Premium && basamak >= 0 && basamak < Basamak && !_alinanPremium.Contains(basamak);

        public bool UcretsizOduluAl(int basamak) => UcretsizOdulAlinabilir(basamak) && _alinanUcretsiz.Add(basamak);

        public bool PremiumOduluAl(int basamak) => PremiumOdulAlinabilir(basamak) && _alinanPremium.Add(basamak);

        /// <summary>Ödüller tek tek ya da toplu alınabilir (GDD 9.8.6): alınabilir bütün basamakları alır.</summary>
        public int TopluAl()
        {
            int alinan = 0;
            for (int i = 0; i < Basamak; i++)
            {
                if (UcretsizOduluAl(i)) alinan++;
                if (PremiumOduluAl(i)) alinan++;
            }

            return alinan;
        }

        public void PremiumSatinAl() => Premium = true;

        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["basamak"] = Basamak,
            ["basamak_puani"] = BasamakPuani,
            ["premium"] = Premium,
            ["hazne_puani"] = HaznePuani,
            ["alinan_ucretsiz"] = _alinanUcretsiz.OrderBy(b => b).Cast<object?>().ToList(),
            ["alinan_premium"] = _alinanPremium.OrderBy(b => b).Cast<object?>().ToList(),
        };

        /// <summary>Basamak tablosu kayıttan sonra kısaldıysa değerler tabloya sığdırılır.</summary>
        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            Basamak = Math.Max(0, Math.Min(_hedefler.Count, KayitAlani.TamSayi(d, "basamak", 0)));
            int puan = Math.Max(0, KayitAlani.TamSayi(d, "basamak_puani", 0));
            BasamakPuani = SonBasamakGecildi ? 0 : Math.Min(puan, _hedefler[Basamak] - 1);
            Premium = KayitAlani.Mantik(d, "premium", false);
            HaznePuani = Math.Max(0, Math.Min(_hazneHedefi, KayitAlani.TamSayi(d, "hazne_puani", 0)));
            _alinanUcretsiz.Clear();
            _alinanPremium.Clear();
            foreach (object? b in KayitAlani.Dizi(d, "alinan_ucretsiz")) _alinanUcretsiz.Add(KayitAlani.TamSayi(b, "alinan_ucretsiz"));
            foreach (object? b in KayitAlani.Dizi(d, "alinan_premium")) _alinanPremium.Add(KayitAlani.TamSayi(b, "alinan_premium"));
            _alinanUcretsiz.RemoveWhere(b => b < 0 || b >= Basamak);
            _alinanPremium.RemoveWhere(b => b < 0 || b >= Basamak);
        }

        /// <summary>
        /// Birikim haznesi (GDD 9.8.8): hazne premium hatta para biriktirir. Para yalnızca oyuncu son basamaktayken
        /// hesaplanır: her parametre: hazne_asama_puani puan bir aşamadır ve her aşama parametre: hazne_asama_parasi
        /// para ekler. Önceki basamaklarda hazne 0 gösterir.
        /// </summary>
        public int HazneParasi =>
            Premium && SonBasamakGecildi ? (HaznePuani / _hazneAsamaPuani) * _hazneAsamaParasi : 0;

        /// <summary>Hazne hedefe ulaşınca dolar ve alınır (GDD 9.8.8).</summary>
        public bool HazneDolu => Premium && SonBasamakGecildi && HaznePuani >= _hazneHedefi;

        /// <returns>Hazneden alınan para; hazne dolu değilse 0.</returns>
        public int HazneyiAl()
        {
            if (!HazneDolu)
            {
                return 0;
            }

            int para = HazneParasi;
            HaznePuani = 0;
            return para;
        }
    }

    /// <summary>
    /// Çarpan sistemi (GDD 9.10). Bir etkinlik çarpanı ya da çarpan bileti aktifken oynanan bölüm "çarpanla
    /// oynandı" olarak işaretlenir; işaret bölüme girildiği anda konur (9.10.1).
    /// </summary>
    public sealed class CarpanSistemi
    {
        private readonly ParametreTablosu _p;
        private DateTime _bitis = DateTime.MinValue;
        private int _kat = 1;

        public CarpanSistemi(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        /// <summary>O an aktif olan çarpan; aktif çarpan yoksa 1.</summary>
        public int AktifCarpan(DateTime simdi) => simdi < _bitis ? _kat : 1;

        /// <summary>Ödül olarak verilen etkinlik çarpanı dakika cinsinden bir süredir (GDD 9.10.4).</summary>
        public void EtkinlikCarpaniEkle(int kat, int dakika, DateTime simdi)
        {
            _bitis = (simdi < _bitis && kat == _kat ? _bitis : simdi) + TimeSpan.FromMinutes(dakika);
            _kat = kat;
        }

        /// <summary>
        /// Çarpan bileti (GDD 9.10.4): parametre: carpan_bileti_kati katında çarpan. Yorum: biletin süresi GDD'de
        /// yazmıyor; etkinlik çarpanının varsayılan süresi (parametre: etkinlik_carpani_suresi_dk) kullanılır.
        /// </summary>
        public void BiletKullan(DateTime simdi) =>
            EtkinlikCarpaniEkle(
                _p.TamSayi(MetaParametreAdlari.CarpanBiletiKati),
                _p.TamSayi(MetaParametreAdlari.EtkinlikCarpaniSuresiDk),
                simdi);

        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["bitis"] = KayitAlani.Zaman(_bitis),
            ["kat"] = _kat,
            ["secilen_duzey"] = SecilenDuzey.HasValue ? (object)SecilenDuzey.Value : null,
        };

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _bitis = KayitAlani.Zaman(d, "bitis", DateTime.MinValue);
            _kat = Math.Max(1, KayitAlani.TamSayi(d, "kat", 1));
            SecilenDuzey = d.TryGetValue("secilen_duzey", out object? duzey) && duzey != null
                ? Math.Max(1, KayitAlani.TamSayi(duzey, "secilen_duzey"))
                : (int?)null;
        }

        /// <summary>
        /// Oyuncunun bölüm öncesinde seçtiği çarpan düzeyi (GDD 9.10.5); null ise aktif çarpanın tamamı kullanılır.
        /// Seçim kalıcıdır: oyuncu değiştirene kadar sonraki bölümlerde de geçerlidir.
        /// </summary>
        public int? SecilenDuzey { get; private set; }

        /// <summary>
        /// Bölüm öncesinde seçilebilen düzeyler: 1'den aktif çarpana kadar. Aktif çarpan yoksa yalnızca 1.
        /// Yorum (9.10.5 "ayrıntılar kendi tasarımın"): oyuncu aktif çarpanı aşamaz, yalnızca daha düşüğünü seçebilir.
        /// </summary>
        public IReadOnlyList<int> SecilebilirDuzeyler(DateTime simdi) => Enumerable.Range(1, AktifCarpan(simdi)).ToList();

        /// <summary>Düzeyi seçer; null seçimi kaldırır. 1'den küçük düzey seçilemez.</summary>
        public bool DuzeySec(int? duzey)
        {
            if (duzey.HasValue && duzey.Value < 1)
            {
                return false;
            }

            SecilenDuzey = duzey;
            return true;
        }

        /// <summary>
        /// Bölüme girildiği anda konan "çarpanla oynandı" işareti: bölüm kazanılınca kullanılacak çarpan.
        /// Bölüme girerken çarpan aktif değilse 1'dir (GDD 9.10.1, 9.10.2). Çarpan seçimi açıksa ve oyuncu bir
        /// düzey seçtiyse işaret, seçilen düzeyle aktif çarpanın küçüğüdür (9.10.5).
        /// </summary>
        public int BolumeGir(DateTime simdi, bool secimAcik = false)
        {
            int aktif = AktifCarpan(simdi);
            return secimAcik && SecilenDuzey.HasValue ? Math.Min(aktif, SecilenDuzey.Value) : aktif;
        }
    }
}
