using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>Giriş hücresinden çıkacak taşı belirler (GDD 5.3.4).</summary>
    public interface IDolguKaynagi
    {
        /// <param name="baglam">Bölüm bağlamı.</param>
        /// <param name="giris">Taşın girdiği giriş hücresi.</param>
        /// <param name="hedef">Taşın ineceği hücre; ayarlı dolgu komşularına bakar (5.3.6).</param>
        Tas Uret(OyunBaglami baglam, Konum giris, Konum hedef);
    }

    /// <summary>
    /// GDD 5.3.4 adım 1, 8.9.2: üst ekrandan kalan taşlar alt ekranın giriş sırasına eklenir ve o sütunun
    /// girişinden her şeyden önce çıkar. Sıra bitince asıl dolgu kaynağı kullanılır.
    /// </summary>
    public sealed class UstEkranKuyrugu : IDolguKaynagi
    {
        private readonly IDolguKaynagi _asil;
        private readonly Dictionary<int, Queue<Tas>> _sira;

        public UstEkranKuyrugu(IDolguKaynagi asil, Dictionary<int, Queue<Tas>> sira)
        {
            _asil = asil;
            _sira = sira;
        }

        /// <summary>Kuyruğun sardığı asıl dolgu kaynağı.</summary>
        public IDolguKaynagi Asil => _asil;

        /// <summary>Sütunun sırasında bekleyen taş sayısı.</summary>
        public int Bekleyen(int sutun) => _sira.TryGetValue(sutun, out var sira) ? sira.Count : 0;

        public Tas Uret(OyunBaglami baglam, Konum giris, Konum hedef) =>
            _sira.TryGetValue(giris.Sutun, out var sira) && sira.Count > 0
                ? sira.Dequeue()
                : _asil.Uret(baglam, giris, hedef);
    }

    /// <summary>Bir dolgu setinin öğesi: sabit bir taş üreticisi ya da rastgele renk.</summary>
    public sealed class DolguOgesi
    {
        /// <summary>Rastgele renk öğesi; rengi ayarlı dolgu seçer (GDD 5.3.6).</summary>
        public static DolguOgesi RastgeleRenk(double agirlik, int? azamiAdet = null) =>
            new DolguOgesi(null, agirlik, azamiAdet);

        public DolguOgesi(Func<Tahta, Tas>? uretici, double agirlik, int? azamiAdet = null)
        {
            Uretici = uretici;
            Agirlik = agirlik;
            KalanAdet = azamiAdet;
        }

        /// <summary>Null ise öğe rastgele renkli normal taştır.</summary>
        public Func<Tahta, Tas>? Uretici { get; }

        public double Agirlik { get; }

        /// <summary>
        /// Bu öğeden en fazla kaç tane çıkacağı; null ise sınırsız. Hedef adedini tahtadaki öğelerle tutarlı
        /// tutmak için kullanılır (GDD 8.4.5).
        /// </summary>
        public int? KalanAdet { get; set; }
    }

    /// <summary>
    /// Dolgu seti (GDD 5.3.2, 5.3.4): ağırlıklı öğeler. <see cref="PasAgirligi"/>, setin bu taş için bir şey
    /// vermeyip sırayı 5.3.4'teki bir sonraki adıma bırakma ağırlığıdır.
    /// </summary>
    public sealed class DolguSeti
    {
        public List<DolguOgesi> Ogeler { get; } = new List<DolguOgesi>();

        public double PasAgirligi { get; set; }
    }

    /// <summary>
    /// GDD 5.3.4: giriş hücresinden çıkacak taş şu öncelikle belirlenir: (2) sütun için önceden tanımlanmış taş,
    /// (3) sütuna atanmış hedef seti, (4) bölümün rastgele dolgu setleri (engeller dahil), (5) rastgele renk.
    /// (1) üst ekrandan kalan taşlar <see cref="UstEkranKuyrugu"/> ile bu kaynağın önüne eklenir.
    /// Set yolundan gelen renkli taşlar ayarlı dolgudan geçer (5.3.6); düz rastgele renk yolu geçmez (5.3.7).
    /// </summary>
    public sealed class RastgeleDolgu : IDolguKaynagi
    {
        private readonly Dictionary<int, Queue<Func<Tahta, Tas>>> _onTanimli = new Dictionary<int, Queue<Func<Tahta, Tas>>>();

        /// <summary>Sütuna atanmış hedef setleri (GDD 5.3.4 adım 3).</summary>
        public Dictionary<int, DolguSeti> SutunSetleri { get; } = new Dictionary<int, DolguSeti>();

        /// <summary>Bölümün rastgele dolgu setleri (GDD 5.3.4 adım 4); sırayla denenir.</summary>
        public List<DolguSeti> Setler { get; } = new List<DolguSeti>();

        /// <summary>Bir sütunun giriş sırasına önceden tanımlı taş ekler (GDD 5.3.4 adım 2).</summary>
        public void OnTanimliEkle(int sutun, Func<Tahta, Tas> uretici)
        {
            if (!_onTanimli.TryGetValue(sutun, out var sira))
            {
                sira = new Queue<Func<Tahta, Tas>>();
                _onTanimli.Add(sutun, sira);
            }

            sira.Enqueue(uretici);
        }

        public Tas Uret(OyunBaglami baglam, Konum giris, Konum hedef)
        {
            if (_onTanimli.TryGetValue(giris.Sutun, out var sira) && sira.Count > 0)
            {
                return sira.Dequeue()(baglam.Tahta);
            }

            if (SutunSetleri.TryGetValue(giris.Sutun, out var sutunSeti))
            {
                var tas = SettenUret(baglam, sutunSeti, hedef);
                if (tas != null)
                {
                    return tas;
                }
            }

            foreach (var set in Setler)
            {
                var tas = SettenUret(baglam, set, hedef);
                if (tas != null)
                {
                    return tas;
                }
            }

            return baglam.Tahta.NormalTasUret(RenkSecici.Sec(baglam, baglam.Renkler));
        }

        private static Tas? SettenUret(OyunBaglami baglam, DolguSeti set, Konum hedef)
        {
            var secenekler = new List<DolguOgesi?>();
            foreach (var oge in set.Ogeler)
            {
                if (oge.Agirlik > 0 && (!oge.KalanAdet.HasValue || oge.KalanAdet.Value > 0))
                {
                    secenekler.Add(oge);
                }
            }

            if (set.PasAgirligi > 0)
            {
                secenekler.Add(null);
            }

            if (secenekler.Count == 0)
            {
                return null;
            }

            var secilen = baglam.Zorluk.AgirlikliSec(secenekler, o => o == null ? set.PasAgirligi : o.Agirlik);
            if (secilen == null)
            {
                return null;
            }

            if (secilen.KalanAdet.HasValue)
            {
                secilen.KalanAdet--;
            }

            return secilen.Uretici != null
                ? secilen.Uretici(baglam.Tahta)
                : baglam.Tahta.NormalTasUret(RenkSecici.AyarliSec(baglam, hedef));
        }
    }

    /// <summary>Renk oranlarına göre ağırlıklı renk seçimi (GDD 5.3.5, 5.4.1).</summary>
    public static class RenkSecici
    {
        public static int Sec(OyunBaglami baglam, IReadOnlyList<int> izinliRenkler)
        {
            if (izinliRenkler.Count == 0)
            {
                throw new InvalidOperationException("Bölümün renk listesi boş (GDD 2.2.2).");
            }

            return baglam.Zorluk.AgirlikliSec(izinliRenkler, renk => Agirlik(baglam, renk));
        }

        /// <summary>
        /// Ayarlı dolgu (GDD 5.3.6): zorluk zarı iki düzeyden birini seçer (parametre: ayarli_dolgu_siki_olasiligi).
        /// Sıkı düzeyde yeni taş aşağı, sol ve sağ komşularından hiçbiriyle aynı renkte olamaz. Normal düzeyde bu üç
        /// yönden birinde iki komşuyla 3'lü tamamlayacak renk yasaklanır. Yorum: bütün renkler yasaklanırsa kısıt uygulanmaz.
        /// </summary>
        public static int AyarliSec(OyunBaglami baglam, Konum hedef)
        {
            var tahta = baglam.Tahta;
            double sikiOlasiligi = baglam.Parametreler.Sayi(Yapilandirma.ParametreAdlari.AyarliDolguSikiOlasiligi);
            bool siki = baglam.Zorluk.SonrakiOran() < sikiOlasiligi;

            var yasak = new HashSet<int>();
            foreach (var yon in new[] { (1, 0), (0, -1), (0, 1) })
            {
                int birinci = tahta.EslesmeRengi(hedef.Otele(yon.Item1, yon.Item2));
                if (birinci == Tas.RenkYok)
                {
                    continue;
                }

                if (siki || tahta.EslesmeRengi(hedef.Otele(2 * yon.Item1, 2 * yon.Item2)) == birinci)
                {
                    yasak.Add(birinci);
                }
            }

            var izinli = new List<int>();
            foreach (int renk in baglam.Renkler)
            {
                if (!yasak.Contains(renk))
                {
                    izinli.Add(renk);
                }
            }

            return Sec(baglam, izinli.Count > 0 ? izinli : baglam.Renkler);
        }

        private static double Agirlik(OyunBaglami baglam, int renk)
        {
            if (baglam.RenkAgirliklari == null)
            {
                return 1;
            }

            for (int i = 0; i < baglam.Renkler.Count && i < baglam.RenkAgirliklari.Count; i++)
            {
                if (baglam.Renkler[i] == renk)
                {
                    return baglam.RenkAgirliklari[i];
                }
            }

            return 1;
        }
    }
}
