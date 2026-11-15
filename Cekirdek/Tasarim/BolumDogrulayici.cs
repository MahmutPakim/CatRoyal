using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Bolum;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Veri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Tasarim
{
    public enum SorunDuzeyi
    {
        /// <summary>Tasarım bandından sapma; bölüm oynanır ama gözden geçirilmelidir.</summary>
        Uyari,

        /// <summary>Kural ihlali; bölüm bu haliyle yayımlanmamalıdır.</summary>
        Hata,
    }

    /// <summary>Doğrulayıcının bulduğu bir sorun.</summary>
    public sealed class BolumSorunu
    {
        public BolumSorunu(SorunDuzeyi duzey, string yer, string kural, string mesaj)
        {
            Duzey = duzey;
            Yer = yer;
            Kural = kural;
            Mesaj = mesaj;
        }

        public SorunDuzeyi Duzey { get; }

        /// <summary>Sorunun yeri: "Bölüm 12", "Kurtarma 1" ya da "Paket".</summary>
        public string Yer { get; }

        /// <summary>İlgili GDD kural numarası.</summary>
        public string Kural { get; }

        public string Mesaj { get; }

        public override string ToString() => $"[{(Duzey == SorunDuzeyi.Hata ? "HATA" : "uyarı")}] {Yer}: {Mesaj} (GDD {Kural})";
    }

    /// <summary>Bölümün tasarım ölçüleri (GDD 8.1–8.5).</summary>
    public sealed class BolumOlculeri
    {
        public int ToplamHucre { get; internal set; }
        public int OynanmayanHucre { get; internal set; }
        public int BosBaslayanHucre { get; internal set; }

        /// <summary>Engel taşı ya da katman taşıyan oynanabilir hücre sayısı (GDD 8.3.5).</summary>
        public int EngelliHucre { get; internal set; }

        /// <summary>Bölümde kullanılan engel ve katman arketipleri (tahta ve dolgu).</summary>
        public ISet<string> EngelTurleri { get; } = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>Tahtaya hazır yerleştirilmiş özel taş sayısı (GDD 8.6.5).</summary>
        public int HazirOzelTas { get; internal set; }

        /// <summary>Hedef adetlerinin toplamı; "adet 0" hedeflerde tahtadaki öğe sayısı.</summary>
        public int HedefAdedi { get; internal set; }

        public int Hamle { get; internal set; }

        public int OynanabilirHucre => ToplamHucre - OynanmayanHucre;
        public double OynanmayanPayi => ToplamHucre == 0 ? 0 : (double)OynanmayanHucre / ToplamHucre;
        public double BosBaslayanPayi => OynanabilirHucre == 0 ? 0 : (double)BosBaslayanHucre / OynanabilirHucre;
        public double EngelYogunlugu => OynanabilirHucre == 0 ? 0 : (double)EngelliHucre / OynanabilirHucre;
        public double HedefHamleOrani => Hamle == 0 ? 0 : (double)HedefAdedi / Hamle;
    }

    /// <summary>Bölümün akıştaki rolü; bazı kurallar role göre değişir.</summary>
    public enum BolumRolu
    {
        Ilerleme,

        /// <summary>Bonus bölümü (GDD 8.7): zorluk eğrisinin dışındadır.</summary>
        Bonus,

        /// <summary>Kurtarma bölümü (GDD 10.2): tek hedeflidir.</summary>
        Kurtarma,
    }

    /// <summary>
    /// Bölüm doğrulayıcı (GDD 8): bölüm dosyalarını tasarım kurallarına ve parametre bantlarına karşı denetler.
    /// <see cref="BolumYukleyici"/> dosyanın okunabilir ve mekanik sınırlar içinde olduğunu denetler; bu sınıf
    /// tasarım desenlerini denetler. Tek bölümün kuralları <see cref="Dogrula"/>, bölümlerin birbirine göre
    /// kuralları (ortalamalar, tanıtım takvimi, zor bölüm ritmi) <see cref="PaketiDogrula"/> ile denetlenir.
    ///
    /// Oran ve ortalama bantlarının toleransları GDD'de yoktur; buradaki sabitler doğrulayıcının kendi seçimidir.
    /// </summary>
    public static class BolumDogrulayici
    {
        /// <summary>Paket ortalamasının parametre değerinden sapabileceği oran.</summary>
        public const double OrtalamaToleransi = 0.25;

        /// <summary>Paket ortalamalarının denetlenmesi için gereken en az bölüm sayısı.</summary>
        public const int OrtalamaIcinAsgariBolum = 10;

        /// <summary>Renk hedefli bölümlerin paketteki en yüksek payı ("renk hedefleri seyrektir", GDD 8.4.3).</summary>
        public const double RenkHedefiAzamiPayi = 0.25;

        // ------------------------------------------------------------------ Ölçüler

        public static BolumOlculeri Olc(BolumVerisi veri)
        {
            var olcu = new BolumOlculeri { ToplamHucre = veri.Genislik * veri.Yukseklik, Hamle = veri.Hamle };
            for (int s = 0; s < veri.Yukseklik; s++)
            {
                for (int t = 0; t < veri.Genislik; t++)
                {
                    var icerik = BolumKurucu.IcerikCoz(veri, veri.Hucreler[s][t], "hücre");
                    if (icerik.Oynanmayan)
                    {
                        olcu.OynanmayanHucre++;
                        continue;
                    }

                    if (icerik.Bos) olcu.BosBaslayanHucre++;
                    if (icerik.Ozel.HasValue) olcu.HazirOzelTas++;

                    bool engelli = false;
                    if (icerik.Engel != null)
                    {
                        engelli = true;
                        olcu.EngelTurleri.Add(TurAdi(icerik.Engel));
                    }

                    foreach (var katman in Katmanlar(veri, s, t))
                    {
                        engelli = true;
                        olcu.EngelTurleri.Add(TurAdi(katman.Tanim));
                    }

                    if (engelli) olcu.EngelliHucre++;
                }
            }

            foreach (var engel in DolguEngelleri(veri))
            {
                olcu.EngelTurleri.Add(TurAdi(engel));
            }

            olcu.HedefAdedi = veri.Hedefler.Sum(h => h.Adet > 0 ? h.Adet : TahtadakiAdet(veri, h));
            return olcu;
        }

        /// <summary>Mekanik türünün adı: engel ya da katman arketipi. Tanıtım takvimi türleri bu adla izler.</summary>
        public static string TurAdi(EngelTanimi tanim) => "engel:" + tanim.Arketip;

        public static string TurAdi(KatmanTanimi tanim) => "katman:" + tanim.Arketip;

        private static IEnumerable<HucreKatmani> Katmanlar(BolumVerisi veri, int s, int t)
        {
            foreach (var izgara in new[] { veri.AltKatmanlar, veri.UstKatmanlar })
            {
                if (izgara != null && izgara[s][t] != ".")
                {
                    yield return BolumKurucu.KatmanCoz(veri, izgara[s][t], "hücre");
                }
            }
        }

        private static IEnumerable<EngelTanimi> DolguEngelleri(BolumVerisi veri) =>
            veri.OnTanimliDolgu.Values.SelectMany(k => k)
                .Concat(veri.DolguSetleri.Concat(veri.SutunSetleri.Values).SelectMany(set => set.Ogeler).Select(o => o.Kod))
                .Select(kod => BolumKurucu.IcerikCoz(veri, kod, "dolgu").Engel)
                .Where(engel => engel != null)
                .Select(engel => engel!);

        /// <summary>Hedefin ekranında, tahtaya önceden yerleşik öğe sayısı (HedefTakibi ile aynı sayım).</summary>
        private static int TahtadakiAdet(BolumVerisi veri, Hedef hedef)
        {
            var ekran = veri.Ekranlar[hedef.Ekran - 1];
            int adet = 0;
            for (int s = ekran.IlkSatir - 1; s < ekran.SonSatir; s++)
            {
                for (int t = 0; t < veri.Genislik; t++)
                {
                    var icerik = BolumKurucu.IcerikCoz(veri, veri.Hucreler[s][t], "hücre");
                    if (icerik.Oynanmayan) continue;
                    if (icerik.Engel?.HedefTuru == hedef.Tur) adet++;
                    if (icerik.Renk.HasValue && Hedef.RenkTuru(icerik.Renk.Value) == hedef.Tur) adet++;
                    adet += Katmanlar(veri, s, t).Count(k => k.Tanim.HedefTuru == hedef.Tur);
                }
            }

            return adet;
        }

        private static bool RenkHedefi(Hedef hedef) => hedef.Tur.StartsWith("renk:", StringComparison.Ordinal);

        /// <summary>Hedef türü oyun sırasında da üretilebiliyor mu: dolgu, üretici ya da yayılma (GDD 8.4.5, 5.3.4)?</summary>
        private static bool OyundaUretilir(BolumVerisi veri, string tur)
        {
            var engeller = veri.Engeller.Values.ToList();
            return DolguEngelleri(veri).Any(e => e.HedefTuru == tur)
                || engeller.Any(e => e.BirakilanKatman?.HedefTuru == tur)
                || engeller.Any(e => e.HedefTuru == tur && e.Yayilma != EngelYayilmasi.Yok)
                || veri.Katmanlar.Values.Any(k => k.HedefTuru == tur && k.Yayilir);
        }

        // ------------------------------------------------------------------ Tek bölüm

        /// <summary>Tek bölümü kendi kurallarına karşı denetler (GDD 8.1–8.5, 8.7.1, 8.9.4, 10.2.8).</summary>
        public static List<BolumSorunu> Dogrula(BolumVerisi veri, ParametreTablosu p, BolumRolu rol = BolumRolu.Ilerleme)
        {
            var sorunlar = new List<BolumSorunu>();
            string yer = (rol == BolumRolu.Kurtarma ? "Kurtarma " : "Bölüm ") + veri.Kimlik;
            void Hata(string kural, string mesaj) => sorunlar.Add(new BolumSorunu(SorunDuzeyi.Hata, yer, kural, mesaj));
            void Uyari(string kural, string mesaj) => sorunlar.Add(new BolumSorunu(SorunDuzeyi.Uyari, yer, kural, mesaj));

            var olcu = Olc(veri);
            int erkenSon = p.TamSayi(TasarimParametreAdlari.ErkenDonemSonu);

            // 8.1.2: standarttan biraz daha dar ya da kısa tahtalar şekil vermek için kullanılır.
            int asgariGenislik = p.TamSayi(ParametreAdlari.TahtaGenislikStandart) - p.TamSayi(ParametreAdlari.DarTahtaAzamiSutunEksigi);
            int asgariYukseklik = p.TamSayi(ParametreAdlari.TahtaYukseklikStandart) - p.TamSayi(ParametreAdlari.DarTahtaAzamiSatirEksigi);
            if (veri.Genislik < asgariGenislik)
            {
                Hata("8.1.2", $"genişlik {veri.Genislik}; en az {asgariGenislik} olmalı.");
            }

            foreach (var ekran in veri.Ekranlar.Where(e => e.Yukseklik < asgariYukseklik))
            {
                Hata("8.1.2", $"{(veri.CokEkranli ? "ekran " : string.Empty)}yüksekliği {ekran.Yukseklik}; en az {asgariYukseklik} olmalı.");
            }

            // 8.2.2: kolaylaştırılmış bölümlerde renk eksiltilir; üst sınırı yükleyici denetler.
            int asgariRenk = p.TamSayi(ParametreAdlari.RenkSayisiStandart) - p.TamSayi(ParametreAdlari.KolayBolumAzamiRenkEksigi);
            if (veri.RenkSayisi < asgariRenk)
            {
                Hata("8.2.2", $"renk sayısı {veri.RenkSayisi}; en az {asgariRenk} olmalı.");
            }

            // 8.9.4: çok ekranlı bölümler oyunun ilerisinde tanıtılır.
            int cokEkranAcilisi = p.TamSayi(TasarimParametreAdlari.AcilisCokEkranli);
            if (veri.CokEkranli && rol != BolumRolu.Kurtarma && veri.Kimlik < cokEkranAcilisi)
            {
                Hata("8.9.4", $"çok ekranlı bölümler {cokEkranAcilisi}. bölümden önce kullanılamaz.");
            }

            HedefleriDenetle(veri, rol, Hata);

            // 8.7.2: bonus bölümleri zorluk eğrisinin dışındadır; hamle bandı ve engel karışımı onlara uygulanmaz.
            if (rol != BolumRolu.Bonus)
            {
                // 8.3.1, 8.3.2: hamle dar bir banttadır ve tabanın altına inmez.
                int ust = p.TamSayi(TasarimParametreAdlari.HamleBandiUst);
                int taban = Math.Min(p.TamSayi(TasarimParametreAdlari.HamleBandiAlt), p.TamSayi(TasarimParametreAdlari.AsgariHamle));
                if (veri.Hamle > ust)
                {
                    Hata("8.3.1", $"hamle {veri.Hamle}; bandın üst ucu {ust}.");
                }
                else if (veri.Hamle < taban)
                {
                    // Tabanın altı yalnızca en ileri bölümlerde ve istisna olarak kullanılır.
                    if (veri.Kimlik >= p.TamSayi(TasarimParametreAdlari.ZorlukKirilmaBolumu))
                    {
                        Uyari("8.3.2", $"hamle {veri.Hamle}; taban {taban}. İleri bölümde istisna olarak kullanılabilir.");
                    }
                    else
                    {
                        Hata("8.3.2", $"hamle {veri.Hamle}; taban {taban}.");
                    }
                }

                // 8.5.2: bir bölümde aynı anda kullanılan engel türü sayısı sınırlıdır.
                int azamiTur = p.TamSayi(TasarimParametreAdlari.BolumAzamiEngelTuru);
                if (olcu.EngelTurleri.Count > azamiTur)
                {
                    Hata("8.5.2", $"{olcu.EngelTurleri.Count} engel türü kullanılıyor; en fazla {azamiTur} olabilir.");
                }
            }

            // 8.4.1: bölüm başına hedef türü sayısı başta azdır.
            int erkenHedef = p.TamSayi(TasarimParametreAdlari.ErkenAzamiHedefTuru);
            if (rol == BolumRolu.Ilerleme && veri.Kimlik <= erkenSon && veri.Hedefler.Count > erkenHedef)
            {
                Hata("8.4.1", $"{veri.Hedefler.Count} hedef türü var; erken dönemde en fazla {erkenHedef} olabilir.");
            }

            // 8.6.5: tahtaya hazır yerleştirilmiş özel taşlar, özel taşlar tanındıktan sonra gelir.
            int takviyeAcilisi = p.TamSayi(MetaParametreAdlari.AcilisBaslangicTakviyeleri);
            if (rol == BolumRolu.Ilerleme && olcu.HazirOzelTas > 0 && veri.Kimlik < takviyeAcilisi)
            {
                Uyari("8.6.5", $"tahtada hazır özel taş var; başlangıç takviyeleri {takviyeAcilisi}. bölümde açılıyor.");
            }

            return sorunlar;
        }

        private static void HedefleriDenetle(BolumVerisi veri, BolumRolu rol, Action<string, string> hata)
        {
            if (veri.Hedefler.Count == 0)
            {
                hata("7.1.1", "hedef yok.");
                return;
            }

            var bilinenTurler = new HashSet<string>(
                veri.Engeller.Values.Select(e => e.HedefTuru)
                    .Concat(veri.Katmanlar.Values.Select(k => k.HedefTuru))
                    .Where(tur => tur != null)
                    .Select(tur => tur!));

            foreach (var hedef in veri.Hedefler)
            {
                if (RenkHedefi(hedef))
                {
                    if (!int.TryParse(hedef.Tur.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out int renk)
                        || renk >= veri.RenkSayisi)
                    {
                        hata("7.1.2", $"'{hedef.Tur}' hedefinin rengi bölümün renk listesinde yok.");
                    }
                    else if (hedef.Adet == 0)
                    {
                        hata("7.1.2", $"'{hedef.Tur}' renk hedefinin adedi yazılmalı; renkler dolguyla sürekli gelir.");
                    }

                    continue;
                }

                if (!bilinenTurler.Contains(hedef.Tur))
                {
                    hata("8.4.5", $"'{hedef.Tur}' hedef türünü taşıyan bir engel ya da katman tanımı yok.");
                    continue;
                }

                // 8.4.5: hedef adedi tahtadaki öğe sayısıyla tutarlıdır; eksik kısım dolguyla sağlanır.
                int tahtada = TahtadakiAdet(veri, hedef);
                bool uretilir = OyundaUretilir(veri, hedef.Tur);
                if (hedef.Adet == 0 && tahtada == 0)
                {
                    hata("8.4.5", $"'{hedef.Tur}' hedefi \"hepsi\" diyor ama {hedef.Ekran}. ekranda bu öğeden yok.");
                }
                else if (hedef.Adet > tahtada && !uretilir)
                {
                    hata("8.4.5", $"'{hedef.Tur}' hedefi {hedef.Adet} istiyor; tahtada {tahtada} var ve dolgu bu öğeyi vermiyor.");
                }
            }

            if (rol == BolumRolu.Bonus)
            {
                // 8.7.1: tek hedef, "tahtadaki bütün para taşlarını topla" (adet 0 = hepsi).
                var paraTurleri = veri.Engeller.Values
                    .Where(e => e.Arketip == EngelArketipi.ParaTasi && e.HedefTuru != null)
                    .Select(e => e.HedefTuru!)
                    .ToList();
                if (veri.Hedefler.Count != 1 || veri.Hedefler[0].Adet != 0 || !paraTurleri.Contains(veri.Hedefler[0].Tur))
                {
                    hata("8.7.1", "bonus bölümünün tek hedefi \"bütün para taşları\" olmalı (adet 0).");
                }
            }

            if (rol == BolumRolu.Kurtarma && veri.Hedefler.Count != 1)
            {
                hata("10.2.8", $"kurtarma bölümünde tek hedef olmalı; {veri.Hedefler.Count} hedef var.");
            }
        }

        // ------------------------------------------------------------------ Paket

        /// <summary>
        /// Bölümleri birlikte denetler: her bölümün kendi kuralları, numaraların sürekliliği, paket ortalamaları
        /// (GDD 8.1.4, 8.1.5, 8.3.3–8.3.5, 8.4.3), tanıtım takvimi (8.6), zor bölüm ritmi (8.8.3) ve kurtarma
        /// bölümleri (10.2).
        /// </summary>
        /// <param name="bolumler">İlerleme bölümleri (bonus bölümleri dahil).</param>
        /// <param name="p">Parametre tablosu.</param>
        /// <param name="kurtarmaBolumleri">
        /// Kurtarma bölümü numarası → bölüm verisi. Verilmezse (null) kurtarma bölümleri denetlenmez; boş sözlük
        /// verilirse bağlı her kurtarma bölümünün eksikliği hata sayılır.
        /// </param>
        /// <param name="oneCekilenKurtarma">Deney grubunun öne çektiği kurtarma bölümleri (GDD 10.2.6).</param>
        public static List<BolumSorunu> PaketiDogrula(
            IEnumerable<BolumVerisi> bolumler,
            ParametreTablosu p,
            IReadOnlyDictionary<int, BolumVerisi>? kurtarmaBolumleri = null,
            IReadOnlyList<int>? oneCekilenKurtarma = null)
        {
            var sirali = bolumler.OrderBy(b => b.Kimlik).ToList();
            var sorunlar = new List<BolumSorunu>();
            void Paket(SorunDuzeyi duzey, string kural, string mesaj) => sorunlar.Add(new BolumSorunu(duzey, "Paket", kural, mesaj));

            var bonus = new BonusTakvimi(p);
            var kurtarma = new KurtarmaTakvimi(p, oneCekilenKurtarma);
            var tanitim = new TanitimTakvimi(p, bonus, kurtarma);
            BolumRolu Rol(BolumVerisi b) => bonus.BonusMu(b.Kimlik) ? BolumRolu.Bonus : BolumRolu.Ilerleme;

            if (sirali.Count == 0)
            {
                Paket(SorunDuzeyi.Hata, "8.11.2", "pakette bölüm yok.");
                return sorunlar;
            }

            // Numaralar tekrarsız ve aralıksızdır.
            for (int i = 1; i < sirali.Count; i++)
            {
                if (sirali[i].Kimlik == sirali[i - 1].Kimlik)
                {
                    Paket(SorunDuzeyi.Hata, "8.10", $"bölüm {sirali[i].Kimlik} birden fazla kez var.");
                }
                else if (sirali[i].Kimlik != sirali[i - 1].Kimlik + 1)
                {
                    Paket(SorunDuzeyi.Hata, "8.11.2", $"bölüm {sirali[i - 1].Kimlik + 1}–{sirali[i].Kimlik - 1} eksik.");
                }
            }

            foreach (var bolum in sirali)
            {
                sorunlar.AddRange(Dogrula(bolum, p, Rol(bolum)));
            }

            int sonBolum = sirali[sirali.Count - 1].Kimlik;
            var tanitilan = TanitimiDenetle(sirali, p, bonus, tanitim, sorunlar);
            if (kurtarmaBolumleri != null)
            {
                KurtarmayiDenetle(sirali, p, kurtarma, kurtarmaBolumleri, tanitilan, sonBolum, sorunlar);
            }

            ZorRitmiDenetle(sirali, p, bonus, sorunlar);
            OrtalamalariDenetle(sirali.Where(b => Rol(b) == BolumRolu.Ilerleme).ToList(), p, Paket);
            return sorunlar;
        }

        /// <returns>Mekanik türü → tanıtıldığı bölüm.</returns>
        private static Dictionary<string, int> TanitimiDenetle(
            List<BolumVerisi> sirali, ParametreTablosu p, BonusTakvimi bonus, TanitimTakvimi tanitim, List<BolumSorunu> sorunlar)
        {
            var tanitilan = new Dictionary<string, int>(StringComparer.Ordinal);
            var aracAcilislari = new[]
            {
                MetaParametreAdlari.AcilisTekHucreAraci, MetaParametreAdlari.AcilisSatirAraci,
                MetaParametreAdlari.AcilisSutunAraci, MetaParametreAdlari.AcilisKaristirmaAraci,
            }.Select(p.TamSayi).ToList();
            string paraTasi = "engel:" + EngelArketipi.ParaTasi;

            foreach (var bolum in sirali)
            {
                string yer = "Bölüm " + bolum.Kimlik;
                void Ekle(SorunDuzeyi duzey, string kural, string mesaj) => sorunlar.Add(new BolumSorunu(duzey, yer, kural, mesaj));

                // Para taşı bonus bölümlerinin mekaniğidir (8.7.1); tanıtım ritmine girmez.
                var turler = Olc(bolum).EngelTurleri.Where(tur => tur != paraTasi).ToList();
                var yeni = turler.Where(tur => !tanitilan.ContainsKey(tur)).ToList();
                bool takvimde = tanitim.TanitimBolumuMu(bolum.Kimlik);

                if (yeni.Count == 0)
                {
                    if (takvimde)
                    {
                        Ekle(SorunDuzeyi.Uyari, "8.6.1, 8.6.3", "tanıtım takvimindeki bölüm yeni mekanik tanıtmıyor.");
                    }

                    continue;
                }

                foreach (string tur in yeni)
                {
                    tanitilan[tur] = bolum.Kimlik;
                }

                if (bonus.BonusMu(bolum.Kimlik))
                {
                    Ekle(SorunDuzeyi.Hata, "8.7.2", $"bonus bölümü yeni mekanik tanıtamaz: {string.Join(", ", yeni)}.");
                    continue;
                }

                if (!takvimde)
                {
                    Ekle(SorunDuzeyi.Hata, "8.6.1, 8.6.3", $"yeni mekanik ({string.Join(", ", yeni)}) tanıtım takvimi dışında geliyor.");
                }

                // 8.6.2: bölüm yeni mekaniği tek başına öğretecek kadar sadedir.
                if (yeni.Count > 1)
                {
                    Ekle(SorunDuzeyi.Hata, "8.6.2", $"tek bölümde birden fazla yeni mekanik tanıtılıyor: {string.Join(", ", yeni)}.");
                }
                else if (turler.Count > 2)
                {
                    Ekle(SorunDuzeyi.Uyari, "8.6.2", $"tanıtım bölümü sade olmalı; {turler.Count} engel türü kullanılıyor.");
                }

                // 8.6.4: araç öğretimi ile engel öğretimi aynı bölüme denk getirilmez.
                if (aracAcilislari.Contains(bolum.Kimlik))
                {
                    Ekle(SorunDuzeyi.Hata, "8.6.4", "yeni mekanik bir oyun içi aracın açıldığı bölümde tanıtılıyor.");
                }

                foreach (string tur in yeni)
                {
                    string? eksik = TanitimHedefiEksigi(bolum, tur);
                    if (eksik != null)
                    {
                        Ekle(SorunDuzeyi.Hata, "8.6.2, 8.4.4", eksik);
                    }
                }
            }

            return tanitilan;
        }

        /// <summary>
        /// GDD 8.6.2: tanıtım bölümünde yeni engel hem tahtada hem hedeftedir. 8.6.2a: hedef olmayan engelleyici
        /// katmanlar (kilit, örtü, sert kaplama) yalnızca tahtada tanıtılabilir; üreticide hedef, ürettiği öğedir.
        /// </summary>
        private static string? TanitimHedefiEksigi(BolumVerisi bolum, string tur)
        {
            var hedefler = new HashSet<string>(bolum.Hedefler.Select(h => h.Tur));
            foreach (var katman in bolum.Katmanlar.Values.Where(k => TurAdi(k) == tur))
            {
                bool istisna = katman.Arketip == KatmanArketipi.Kilit
                    || katman.Arketip == KatmanArketipi.Ortu
                    || katman.Arketip == KatmanArketipi.SertKaplama;
                if (istisna || (katman.HedefTuru != null && hedefler.Contains(katman.HedefTuru)))
                {
                    return null;
                }
            }

            foreach (var engel in bolum.Engeller.Values.Where(e => TurAdi(e) == tur))
            {
                if (engel.HedefTuru != null && hedefler.Contains(engel.HedefTuru))
                {
                    return null;
                }

                if (engel.Arketip == EngelArketipi.Uretici
                    && engel.BirakilanKatman?.HedefTuru != null
                    && hedefler.Contains(engel.BirakilanKatman.HedefTuru))
                {
                    return null;
                }
            }

            return $"yeni tanıtılan '{tur}' hedefler arasında değil.";
        }

        private static void KurtarmayiDenetle(
            List<BolumVerisi> sirali,
            ParametreTablosu p,
            KurtarmaTakvimi takvim,
            IReadOnlyDictionary<int, BolumVerisi> kurtarmaBolumleri,
            Dictionary<string, int> tanitilan,
            int sonBolum,
            List<BolumSorunu> sorunlar)
        {
            int ilkBolum = sirali[0].Kimlik;
            var eslemeler = takvim.Eslemeler(sonBolum).Where(e => e.IlerlemeBolumu >= ilkBolum).ToList();
            var kullanilan = new HashSet<int>();
            foreach (var esleme in eslemeler)
            {
                string yer = "Kurtarma " + esleme.KurtarmaNo;
                kullanilan.Add(esleme.KurtarmaNo);
                if (!kurtarmaBolumleri.TryGetValue(esleme.KurtarmaNo, out var veri))
                {
                    sorunlar.Add(new BolumSorunu(
                        SorunDuzeyi.Hata, yer, "10.2.1", $"{esleme.IlerlemeBolumu}. bölüme bağlı kurtarma bölümünün dosyası yok."));
                    continue;
                }

                sorunlar.AddRange(Dogrula(veri, p, BolumRolu.Kurtarma));

                // Kurtarma bölümü yeni mekanik öğretmez: yalnızca o bölüme kadar tanıtılmış olanları kullanır.
                var erken = Olc(veri).EngelTurleri
                    .Where(tur => !tanitilan.TryGetValue(tur, out int bolum) || bolum > esleme.IlerlemeBolumu)
                    .ToList();
                if (erken.Count > 0)
                {
                    sorunlar.Add(new BolumSorunu(
                        SorunDuzeyi.Uyari, yer, "8.6.2", $"{esleme.IlerlemeBolumu}. bölümden önce tanıtılmamış mekanik kullanıyor: {string.Join(", ", erken)}."));
                }
            }

            foreach (int no in kurtarmaBolumleri.Keys.Where(no => !kullanilan.Contains(no)).OrderBy(no => no))
            {
                sorunlar.Add(new BolumSorunu(SorunDuzeyi.Uyari, "Kurtarma " + no, "10.2.1", "bu paketteki hiçbir ilerleme bölümüne bağlı değil."));
            }
        }

        /// <summary>GDD 8.8.3: her bloğun son bölümü diğerlerinden daha az hamle verir.</summary>
        private static void ZorRitmiDenetle(List<BolumVerisi> sirali, ParametreTablosu p, BonusTakvimi bonus, List<BolumSorunu> sorunlar)
        {
            var ritim = new ZorBolumRitmi(p);
            var farklar = new List<double>();
            foreach (var blok in sirali.Where(b => !bonus.BonusMu(b.Kimlik)).GroupBy(b => ritim.Blok(b.Kimlik)))
            {
                var zor = blok.FirstOrDefault(b => ritim.ZorMu(b.Kimlik));
                var digerleri = blok.Where(b => !ritim.ZorMu(b.Kimlik)).ToList();
                if (zor == null || digerleri.Count == 0)
                {
                    continue;
                }

                double fark = digerleri.Average(b => b.Hamle) - zor.Hamle;
                farklar.Add(fark);
                if (fark < 1)
                {
                    sorunlar.Add(new BolumSorunu(
                        SorunDuzeyi.Uyari,
                        "Bölüm " + zor.Kimlik,
                        "8.8.3",
                        $"bloğun son bölümü olarak daha az hamle vermeli; hamle {zor.Hamle}, bloğun ortalaması {digerleri.Average(b => b.Hamle):0.#}."));
                }
            }

            if (farklar.Count >= 3 && Math.Abs(farklar.Average() - ritim.HamleFarki) > Math.Max(1, ritim.HamleFarki * 0.5))
            {
                sorunlar.Add(new BolumSorunu(
                    SorunDuzeyi.Uyari,
                    "Paket",
                    "8.8.3",
                    $"zor bölümlerin ortalama hamle farkı {farklar.Average():0.#}; beklenen {ritim.HamleFarki:0.#}."));
            }
        }

        private static void OrtalamalariDenetle(List<BolumVerisi> bolumler, ParametreTablosu p, Action<SorunDuzeyi, string, string> paket)
        {
            if (bolumler.Count < OrtalamaIcinAsgariBolum)
            {
                return;
            }

            var olculer = bolumler.ToDictionary(b => b, Olc);
            int erkenSon = p.TamSayi(TasarimParametreAdlari.ErkenDonemSonu);
            int kirilma = p.TamSayi(TasarimParametreAdlari.ZorlukKirilmaBolumu);

            void UstSinir(string kural, string ad, List<BolumVerisi> kume, Func<BolumOlculeri, double> deger, string parametre)
            {
                if (kume.Count < OrtalamaIcinAsgariBolum) return;
                double ortalama = kume.Average(b => deger(olculer[b]));
                double sinir = p.Sayi(parametre) * (1 + OrtalamaToleransi);
                if (ortalama > sinir)
                {
                    paket(SorunDuzeyi.Uyari, kural, $"{ad} ortalaması {ortalama:0.###}; parametre: {parametre} = {p.Sayi(parametre):0.###}.");
                }
            }

            void Bant(string kural, string ad, List<BolumVerisi> kume, Func<BolumOlculeri, double> deger, string parametre)
            {
                if (kume.Count < OrtalamaIcinAsgariBolum) return;
                double ortalama = kume.Average(b => deger(olculer[b]));
                double hedef = p.Sayi(parametre);
                if (Math.Abs(ortalama - hedef) > hedef * OrtalamaToleransi)
                {
                    paket(SorunDuzeyi.Uyari, kural, $"{ad} ortalaması {ortalama:0.###}; parametre: {parametre} = {hedef:0.###}.");
                }
            }

            var erken = bolumler.Where(b => b.Kimlik <= erkenSon).ToList();
            var sonra = bolumler.Where(b => b.Kimlik > erkenSon).ToList();
            var kirilmaOncesi = bolumler.Where(b => b.Kimlik < kirilma).ToList();
            var kirilmaSonrasi = bolumler.Where(b => b.Kimlik >= kirilma).ToList();

            UstSinir("8.1.4", "oynanmayan hücre payı", bolumler, o => o.OynanmayanPayi, TasarimParametreAdlari.OynanmayanHucrePayi);
            UstSinir("8.1.5", "erken dönemde boş başlayan hücre payı", erken, o => o.BosBaslayanPayi, TasarimParametreAdlari.BosBaslayanHucrePayiErken);
            UstSinir("8.1.5", "boş başlayan hücre payı", sonra, o => o.BosBaslayanPayi, TasarimParametreAdlari.BosBaslayanHucrePayi);
            Bant("8.3.3", "erken dönem hamle", kirilmaOncesi, o => o.Hamle, TasarimParametreAdlari.HamleOrtalamaErken);
            Bant("8.3.3", "geç dönem hamle", kirilmaSonrasi, o => o.Hamle, TasarimParametreAdlari.HamleOrtalamaGec);
            Bant("8.3.4", "erken dönem hedef/hamle oranı", kirilmaOncesi, o => o.HedefHamleOrani, TasarimParametreAdlari.HedefHamleOraniErken);
            Bant("8.3.4", "geç dönem hedef/hamle oranı", kirilmaSonrasi, o => o.HedefHamleOrani, TasarimParametreAdlari.HedefHamleOraniGec);
            Bant("8.3.5", "engel yoğunluğu", bolumler, o => o.EngelYogunlugu, TasarimParametreAdlari.EngelYogunlugu);

            // 8.4.3: hedeflerin büyük çoğunluğu engel kırma ve toplama hedefidir; renk hedefleri seyrektir.
            double renkPayi = bolumler.Count(b => b.Hedefler.Any(RenkHedefi)) / (double)bolumler.Count;
            if (renkPayi > RenkHedefiAzamiPayi)
            {
                paket(SorunDuzeyi.Uyari, "8.4.3", $"bölümlerin %{renkPayi * 100:0}'inde renk hedefi var; renk hedefleri seyrek olmalı.");
            }
        }
    }
}
