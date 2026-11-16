using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Harcama kaydı (GDD 9.2.5): miktar ve harcamanın adı; analitiğe adıyla raporlanır.</summary>
    public sealed class HarcamaKaydi
    {
        public HarcamaKaydi(int miktar, string ad)
        {
            Miktar = miktar;
            Ad = ad;
        }

        public int Miktar { get; }
        public string Ad { get; }
    }

    /// <summary>Tek yumuşak para birimi (GDD 9.2).</summary>
    public sealed class Cuzdan
    {
        private readonly List<HarcamaKaydi> _harcamalar = new List<HarcamaKaydi>();

        public Cuzdan(ParametreTablosu parametreler)
        {
            // GDD 9.2.1: yeni oyuncu bir başlangıç parasıyla başlar.
            Bakiye = parametreler.TamSayi(MetaParametreAdlari.BaslangicParasi);
        }

        public int Bakiye { get; private set; }

        public IReadOnlyList<HarcamaKaydi> Harcamalar => _harcamalar;

        /// <summary>Para değişince tetiklenir (GDD 9.2.7); arayüz ve görevler dinler.</summary>
        public event Action<int>? ParaDegisti;

        /// <summary>Para harcanınca tetiklenir (GDD 9.2.7).</summary>
        public event Action<HarcamaKaydi>? ParaHarcandi;

        /// <summary>Yetersiz bakiyede para mağazası önerilir (GDD 9.2.5).</summary>
        public event Action<int>? ParaMagazasiOnerildi;

        public void Ekle(int miktar)
        {
            if (miktar < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(miktar), "Eklenen miktar negatif olamaz.");
            }

            if (miktar == 0)
            {
                return;
            }

            Bakiye += miktar;
            ParaDegisti?.Invoke(Bakiye);
        }

        /// <summary>
        /// GDD 9.2.5: önce bakiye kontrol edilir; yetersiz bakiyede harcama başarısız olur, bakiye değişmez,
        /// kayıt oluşmaz ve para mağazası önerilir.
        /// </summary>
        public bool Harca(int miktar, string ad)
        {
            if (miktar < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(miktar), "Harcanan miktar negatif olamaz.");
            }

            if (Bakiye < miktar)
            {
                ParaMagazasiOnerildi?.Invoke(miktar - Bakiye);
                return false;
            }

            Bakiye -= miktar;
            var kayit = new HarcamaKaydi(miktar, ad);
            _harcamalar.Add(kayit);
            ParaDegisti?.Invoke(Bakiye);
            ParaHarcandi?.Invoke(kayit);
            return true;
        }

        /// <summary>Kayıttan yükleme: olay tetiklemez, harcama kaydı oluşturmaz.</summary>
        internal void BakiyeyiYukle(int bakiye) => Bakiye = Math.Max(0, bakiye);

        /// <summary>GDD 9.2.8: sunucu değeri yerel değerin üzerine yazabilir.</summary>
        public void SunucuylaEsitle(int sunucuBakiyesi)
        {
            if (sunucuBakiyesi != Bakiye)
            {
                Bakiye = Math.Max(0, sunucuBakiyesi);
                ParaDegisti?.Invoke(Bakiye);
            }
        }
    }
}
