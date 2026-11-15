using System;
using System.Collections.Generic;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Rastgelelik;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek
{
    /// <summary>Bir bölümün çekirdek mantığının ihtiyaç duyduğu her şey: tahta, bayraklar, parametreler, rastgelelik.</summary>
    public sealed class OyunBaglami
    {
        public OyunBaglami(Tahta tahta, AkBayraklari bayraklar, ParametreTablosu parametreler, IRastgele rastgele)
        {
            Tahta = tahta;
            Bayraklar = bayraklar;
            Parametreler = parametreler;
            Rastgele = rastgele;
            Zorluk = new ZorlukRastgelesi(rastgele, 0);
            Dolgu = new RastgeleDolgu();
        }

        public Tahta Tahta { get; }
        public AkBayraklari Bayraklar { get; }
        public ParametreTablosu Parametreler { get; }
        public IRastgele Rastgele { get; }

        /// <summary>
        /// Zorluk ağırlıklı üreteç (GDD 9.11.5): kaskadda doğum hücresi, dolgu rengi, başlangıç renkleri.
        /// Varsayılan: <see cref="Rastgele"/> ile aynı akış, etkisiz ağırlık.
        /// </summary>
        public ZorlukRastgelesi Zorluk { get; set; }

        /// <summary>Bölümde kullanılan renkler (GDD 2.2.2).</summary>
        public IReadOnlyList<int> Renkler { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Renk oranları (GDD 5.3.5), <see cref="Renkler"/> ile aynı sırada; boşsa renkler eşit ağırlıklıdır.
        /// </summary>
        public IReadOnlyList<double>? RenkAgirliklari { get; set; }

        /// <summary>Bölüm çapraz kaymaya izin veriyor mu (GDD 5.2.4)?</summary>
        public bool CaprazKaymaIzinli { get; set; }

        /// <summary>Giriş hücrelerinden çıkacak taşların kaynağı (GDD 5.3.4).</summary>
        public IDolguKaynagi Dolgu { get; set; }

        /// <summary>
        /// Mantıksal düşme saati (GDD 5.1.7). Taşların oturma adımı bu saate göre yazılır; yalnızca sıra anlamlıdır.
        /// </summary>
        public long Saat { get; set; }

        /// <summary>Çift renk modu: bölümdeki her renk küresi çift renk küresidir (GDD 3.6.1).</summary>
        public bool CiftRenkModu { get; set; }

        /// <summary>Bölümün hedef sayaçları (GDD 7.1); hedefsiz çalışmada null.</summary>
        public Bolum.HedefTakibi? Hedefler { get; set; }

        /// <summary>
        /// Hücredeki öğe bölüm hedefi mi (GDD 3.4.5, AK-05)? Boşsa <see cref="Hedefler"/> kullanılır; verilirse onun yerine geçer (testler için).
        /// </summary>
        public Func<Konum, bool>? HedefOgesiMi { get; set; }
    }
}
