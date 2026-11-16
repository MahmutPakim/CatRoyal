using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Analitik çıkışı (GDD 9.2.5, 9.2.7). Çekirdek olayı adıyla ve alanlarıyla bildirir; nereye gönderileceği
    /// (analitik hizmeti, günlük, hiçbir yer) sunumun işidir.
    /// </summary>
    public interface IAnalitik
    {
        /// <param name="ad">Olayın adı, ör. <see cref="AnalitikOlaylari.ParaHarcandi"/>.</param>
        /// <param name="alanlar">Olayın alanları: metin, sayı ya da doğru/yanlış.</param>
        void Bildir(string ad, IReadOnlyDictionary<string, object> alanlar);
    }

    public static class AnalitikOlaylari
    {
        /// <summary>Alanlar: ad (harcamanın adı), miktar, bakiye (harcamadan sonra).</summary>
        public const string ParaHarcandi = "para_harcandi";

        /// <summary>Yetersiz bakiye yüzünden para mağazası önerildi. Alanlar: eksik, bakiye.</summary>
        public const string ParaMagazasiOnerildi = "para_magazasi_onerildi";
    }

    /// <summary>Bildirilen bir analitik olayı.</summary>
    public sealed class AnalitikOlayi
    {
        public AnalitikOlayi(string ad, IReadOnlyDictionary<string, object> alanlar)
        {
            Ad = ad;
            Alanlar = alanlar;
        }

        public string Ad { get; }
        public IReadOnlyDictionary<string, object> Alanlar { get; }
    }

    /// <summary>Olayları bellekte biriktiren analitik; testler ve yerel geliştirme kullanır.</summary>
    public sealed class BellekAnalitigi : IAnalitik
    {
        private readonly List<AnalitikOlayi> _olaylar = new List<AnalitikOlayi>();

        public IReadOnlyList<AnalitikOlayi> Olaylar => _olaylar;

        public void Bildir(string ad, IReadOnlyDictionary<string, object> alanlar) =>
            _olaylar.Add(new AnalitikOlayi(ad, alanlar));
    }

    /// <summary>Oyuncunun para olaylarını analitiğe bağlar.</summary>
    public static class AnalitikBaglayici
    {
        /// <summary>
        /// Her harcamayı adıyla (GDD 9.2.5) ve her para mağazası önerisini analitiğe bildirir.
        /// </summary>
        /// <returns>Bağlantıyı koparan nesne; oyuncu değiştirilirken (yeniden yükleme) çağrılır.</returns>
        public static IDisposable Bagla(Oyuncu oyuncu, IAnalitik analitik)
        {
            var cuzdan = oyuncu.Cuzdan;
            Action<HarcamaKaydi> harcandi = kayit => analitik.Bildir(
                AnalitikOlaylari.ParaHarcandi,
                new Dictionary<string, object> { ["ad"] = kayit.Ad, ["miktar"] = kayit.Miktar, ["bakiye"] = cuzdan.Bakiye });
            Action<int> onerildi = eksik => analitik.Bildir(
                AnalitikOlaylari.ParaMagazasiOnerildi,
                new Dictionary<string, object> { ["eksik"] = eksik, ["bakiye"] = cuzdan.Bakiye });

            cuzdan.ParaHarcandi += harcandi;
            cuzdan.ParaMagazasiOnerildi += onerildi;
            return new Baglanti(() =>
            {
                cuzdan.ParaHarcandi -= harcandi;
                cuzdan.ParaMagazasiOnerildi -= onerildi;
            });
        }

        private sealed class Baglanti : IDisposable
        {
            private Action? _kopar;

            public Baglanti(Action kopar)
            {
                _kopar = kopar;
            }

            public void Dispose()
            {
                _kopar?.Invoke();
                _kopar = null;
            }
        }
    }
}
