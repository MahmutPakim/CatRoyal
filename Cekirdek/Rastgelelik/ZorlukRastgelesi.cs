using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Rastgelelik
{
    /// <summary>
    /// Zorluğa bağlı rastgelelik (GDD 9.11.5): kaskadda doğum hücresi seçimi, rastgele dolgu rengi, ayarlı dolgu,
    /// başlangıç renkleri ve dolum göstergesinin fırlatma hücresi aynı üreteçten beslenir.
    ///
    /// Yorum (1. aşama, karar 5): GDD ağırlığın seçimi nasıl kaydırdığını tanımlamıyor. Kaydırma kuralı
    /// tanımlanana kadar seçimler ağırlıktan bağımsız, düzgün dağılımlıdır; ağırlık yalnızca taşınır.
    /// </summary>
    public sealed class ZorlukRastgelesi : IRastgele
    {
        private readonly IRastgele _kaynak;

        public ZorlukRastgelesi(IRastgele kaynak, double agirlik)
        {
            if (agirlik < 0 || agirlik > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(agirlik), "Ağırlık 0 ile 1 arasında olmalı.");
            }

            _kaynak = kaynak;
            Agirlik = agirlik;
        }

        /// <summary>Oyuncu aleyhine kayma payı (parametre: zorluk_rastgelelik_agirligi). 0 etkisizdir.</summary>
        public double Agirlik { get; }

        /// <summary>Ağırlık etkisiz mi? Etkisiz ağırlıkta eşitlikler tarama sırasıyla bozulur (A14).</summary>
        public bool Etkisiz => Agirlik == 0;

        public int Sonraki(int ustSinir) => _kaynak.Sonraki(ustSinir);

        public double SonrakiOran() => _kaynak.SonrakiOran();

        /// <summary>Ağırlıklı seçim: her öğe ağırlığıyla orantılı olasılıkla seçilir.</summary>
        public T AgirlikliSec<T>(IReadOnlyList<T> ogeler, Func<T, double> agirlik)
        {
            if (ogeler.Count == 0)
            {
                throw new ArgumentException("Boş listeden seçim yapılamaz.", nameof(ogeler));
            }

            double toplam = 0;
            foreach (var oge in ogeler)
            {
                toplam += Math.Max(0, agirlik(oge));
            }

            if (toplam <= 0)
            {
                return ogeler[_kaynak.Sonraki(ogeler.Count)];
            }

            double hedef = _kaynak.SonrakiOran() * toplam;
            foreach (var oge in ogeler)
            {
                hedef -= Math.Max(0, agirlik(oge));
                if (hedef < 0)
                {
                    return oge;
                }
            }

            return ogeler[ogeler.Count - 1];
        }
    }
}
