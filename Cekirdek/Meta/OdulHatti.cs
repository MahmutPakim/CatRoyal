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

        public OdulHatti(IReadOnlyList<int> basamakHedefleri, int hazneAsamaPuani, int hazneAsamaParasi, int hazneHedefi)
        {
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
        public static OdulHatti Sezon(ParametreTablosu p)
        {
            var hedefler = SezonBasamakHedefleri(p);
            int beklenen = p.TamSayi(MetaParametreAdlari.SezonHattiBasamakSayisi);
            if (hedefler.Count != beklenen)
            {
                throw new ParametreHatasi(
                    $"'{MetaParametreAdlari.SezonHattiBasamakHedefleri}' {hedefler.Count} basamak veriyor; "
                    + $"'{MetaParametreAdlari.SezonHattiBasamakSayisi}' {beklenen} bekliyor.");
            }

            return new OdulHatti(
                hedefler,
                p.TamSayi(MetaParametreAdlari.HazneAsamaPuani),
                p.TamSayi(MetaParametreAdlari.HazneAsamaParasi),
                p.TamSayi(MetaParametreAdlari.SezonHazneHedefi));
        }

        /// <summary>Haftalık ödül hattı (GDD 9.9.2); aşama puanı sezon hattıyla ortaktır.</summary>
        public static OdulHatti Haftalik(ParametreTablosu p)
        {
            var hedefler = p.TamSayiListesi(MetaParametreAdlari.HaftalikHatBasamakHedefleri);
            int beklenen = p.TamSayi(MetaParametreAdlari.HaftalikHatBasamakSayisi);
            if (hedefler.Count != beklenen)
            {
                throw new ParametreHatasi(
                    $"'{MetaParametreAdlari.HaftalikHatBasamakHedefleri}' {hedefler.Count} basamak veriyor; "
                    + $"'{MetaParametreAdlari.HaftalikHatBasamakSayisi}' {beklenen} bekliyor.");
            }

            return new OdulHatti(
                hedefler,
                p.TamSayi(MetaParametreAdlari.HazneAsamaPuani),
                p.TamSayi(MetaParametreAdlari.HaftalikHazneAsamaParasi),
                p.TamSayi(MetaParametreAdlari.HaftalikHazneHedefi));
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

        /// <summary>
        /// Bölüme girildiği anda konan "çarpanla oynandı" işareti: bölüm kazanılınca kullanılacak çarpan.
        /// Bölüme girerken çarpan aktif değilse 1'dir (GDD 9.10.1, 9.10.2).
        /// </summary>
        public int BolumeGir(DateTime simdi) => AktifCarpan(simdi);
    }
}
