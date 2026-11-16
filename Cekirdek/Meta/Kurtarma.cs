using System;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Tasarim;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Kurtarma bölümü akışı (GDD 10.2): oyuncu kurtarma bölümü bağlı bir ilerleme bölümüne gelince normal bölüm
    /// yerine kurtarma bölümü yüklenir (10.2.1). Temanın paketi indirilmemişse paket beklenir; beklemek istenmezse
    /// yedek bölüm kullanılır (10.2.5).
    ///
    /// Durumlar: normal akış → kurtarma bölümü | paket bekleniyor → (paket indi) kurtarma bölümü | (yedek
    /// kullanıldı) yedek bölüm → (kazanıldı) normal akış. Kayıpta aynı kurtarma bölümü tekrarlanır: ilerleme
    /// bölümü değişmediği için <see cref="BolumeGel"/> yine aynı eşlemeyi verir.
    ///
    /// Yorum: yedek bölüm, o ilerleme bölümünün kendi olağan bölüm dosyasıdır ve olağan bölüm gibi oynanır (olağan
    /// ödül, kayıpta can düşer). Ana tema uygulamayla gelir; yalnızca dönüşümlü temalar indirilir.
    /// </summary>
    public sealed class KurtarmaAkisi
    {
        private readonly KurtarmaTakvimi _takvim;
        private readonly Func<int, bool> _temaPaketiVar;

        /// <param name="takvim">Eşleme tablosu.</param>
        /// <param name="temaPaketiVar">Dönüşümlü temanın paketi indirildi mi? Ana tema için sorulmaz.</param>
        public KurtarmaAkisi(KurtarmaTakvimi takvim, Func<int, bool> temaPaketiVar)
        {
            _takvim = takvim;
            _temaPaketiVar = temaPaketiVar;
        }

        public KurtarmaBolumuDurumu Durum { get; private set; } = KurtarmaBolumuDurumu.NormalAkis;

        /// <summary>Gelinen ilerleme bölümüne bağlı kurtarma bölümü; normal akışta null.</summary>
        public KurtarmaEslemesi? Esleme { get; private set; }

        /// <summary>Kurtarma bölümü mü oynanacak? Yedek bölümde ve normal akışta false.</summary>
        public bool KurtarmaOynanir => Durum == KurtarmaBolumuDurumu.KurtarmaBolumu;

        /// <summary>Temanın indirilen paketinin kimliği; ana temada null.</summary>
        public static string? TemaPaketi(int tema) => tema == KurtarmaTakvimi.AnaTema ? null : "kurtarma_tema_" + tema;

        /// <summary>Oyuncu bir ilerleme bölümüne geldi.</summary>
        public KurtarmaBolumuDurumu BolumeGel(int ilerlemeBolumu)
        {
            Esleme = _takvim.Bagli(ilerlemeBolumu);
            Durum = Esleme == null ? KurtarmaBolumuDurumu.NormalAkis
                : Esleme.AnaTema || _temaPaketiVar(Esleme.Tema) ? KurtarmaBolumuDurumu.KurtarmaBolumu
                : KurtarmaBolumuDurumu.PaketBekleniyor;
            return Durum;
        }

        /// <summary>Beklenen tema paketi indi: kurtarma bölümü oynanır.</summary>
        public bool PaketIndi()
        {
            if (Durum != KurtarmaBolumuDurumu.PaketBekleniyor || !_temaPaketiVar(Esleme!.Tema))
            {
                return false;
            }

            Durum = KurtarmaBolumuDurumu.KurtarmaBolumu;
            return true;
        }

        /// <summary>Paket beklenmeyecek: yedek bölüm oynanır (GDD 10.2.5).</summary>
        public bool YedegiKullan()
        {
            if (Durum != KurtarmaBolumuDurumu.PaketBekleniyor)
            {
                return false;
            }

            Durum = KurtarmaBolumuDurumu.YedekBolum;
            return true;
        }

        /// <summary>Bölüm akışını duruma göre ayarlar: kurtarma bölümünde özel ödül ve can kuralı geçerlidir.</summary>
        public void Uygula(BolumAkisi akis) => akis.Kurtarma = KurtarmaOynanir;
    }
}
