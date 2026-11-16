using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Sunucunun bildirdiği süreli bir dönem: sezon, hafta ya da albüm sezonu (GDD 9.7.1, 9.8.11, 9.9.7). Süre
    /// sunucudan bitiş anı olarak gelir.
    /// </summary>
    public sealed class DonemBilgisi
    {
        public DonemBilgisi(string kimlik, DateTime bitis)
        {
            Kimlik = kimlik;
            Bitis = bitis;
        }

        /// <summary>Dönemin kimliği; yeni dönem yeni kimlikle gelir.</summary>
        public string Kimlik { get; }

        public DateTime Bitis { get; }

        /// <summary>Olayın genel yapılandırmasının sürümü (GDD 9.9.7).</summary>
        public int GenelSurum { get; set; } = 1;

        /// <summary>Oyuncuya özel yapılandırmanın sürümü (GDD 9.9.7).</summary>
        public int OyuncuSurumu { get; set; } = 1;

        /// <summary>
        /// Bu oyuncunun basamak tablosu: deney grubunda ya da oyuncuya özel yapılandırmada farklı olabilir
        /// (GDD 9.8.11, 9.9.7). Verilmezse parametre dosyasındaki tablo kullanılır. Yeni dönem başlarken uygulanır.
        /// </summary>
        public IReadOnlyList<int>? BasamakHedefleri { get; set; }
    }

    /// <summary>Biten bir dönemin sonucu: kendiliğinden verilen alınmamış ödüller (GDD 9.7.11, 9.8.10, 9.9.6).</summary>
    public sealed class DonemBitisi
    {
        public DonemBitisi(string kimlik, IReadOnlyList<OdulPaketi> oduller)
        {
            Kimlik = kimlik;
            Oduller = oduller;
        }

        public string Kimlik { get; }
        public IReadOnlyList<OdulPaketi> Oduller { get; }
    }

    /// <summary>Oyuncunun içinde bulunduğu hat dönemi (sezon ya da hafta).</summary>
    public sealed class HatDonemi
    {
        /// <summary>İçinde bulunulan dönemin kimliği; dönem başlamadıysa null.</summary>
        public string? Kimlik { get; internal set; }

        public DateTime Bitis { get; internal set; }

        /// <summary>Ödülleri verilmiş son dönem; aynı dönem yeniden başlatılmaz.</summary>
        public string? SonBiten { get; internal set; }

        /// <summary>Son gün bildirimi bu dönem için gösterildi mi (GDD 9.9.6)?</summary>
        public bool SonGunBildirildi { get; internal set; }

        public bool Suruyor(DateTime simdi) => Kimlik != null && simdi < Bitis;

        public TimeSpan Kalan(DateTime simdi) => Suruyor(simdi) ? Bitis - simdi : TimeSpan.Zero;

        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["kimlik"] = Kimlik,
            ["bitis"] = KayitAlani.Zaman(Bitis),
            ["son_biten"] = SonBiten,
            ["son_gun_bildirildi"] = SonGunBildirildi,
        };

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            Kimlik = d.TryGetValue("kimlik", out object? kimlik) ? kimlik as string : null;
            Bitis = KayitAlani.Zaman(d, "bitis", DateTime.MinValue);
            SonBiten = d.TryGetValue("son_biten", out object? biten) ? biten as string : null;
            SonGunBildirildi = KayitAlani.Mantik(d, "son_gun_bildirildi", false);
        }
    }
}
