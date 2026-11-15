namespace CatRoyal.Cekirdek.Model
{
    public enum TasTuru
    {
        /// <summary>Tek renkli, eşleşmeye giren taş (GDD 2.2.1).</summary>
        Normal,

        /// <summary>Çizgi taşı (GDD 3.2).</summary>
        Cizgi,

        /// <summary>Bomba taşı (GDD 3.3).</summary>
        Bomba,

        /// <summary>Güdümlü taş (GDD 3.4).</summary>
        Gudumlu,

        /// <summary>Renk küresi (GDD 3.5).</summary>
        RenkKuresi,

        /// <summary>Hücreyi dolduran engel (GDD 6.2.1); davranışı <see cref="EngelTanimi"/> belirler.</summary>
        Engel,
    }

    public enum Yon
    {
        Yatay,
        Dikey,
    }

    /// <summary>
    /// Tahtadaki bir taş. Kimlik, taşı düşme ve yer değiştirme boyunca izlemek içindir
    /// (sunum katmanı ve testler kullanır).
    /// </summary>
    public sealed class Tas
    {
        /// <summary>Rengi olmayan taşların renk değeri (GDD 2.2.3).</summary>
        public const int RenkYok = -1;

        internal Tas(int kimlik, TasTuru tur, int renk, Yon? cizgiYonu, EngelTanimi? engel = null, int katman = 0)
        {
            Kimlik = kimlik;
            Tur = tur;
            Renk = renk;
            CizgiYonu = cizgiYonu;
            Engel = engel;
            Katman = katman;
            Oturdu = true;
        }

        public int Kimlik { get; }
        public TasTuru Tur { get; }

        /// <summary>Normal taşın rengi; özel taşlarda <see cref="RenkYok"/>.</summary>
        public int Renk { get; }

        /// <summary>Yalnızca çizgi taşında dolu.</summary>
        public Yon? CizgiYonu { get; }

        /// <summary>Yalnızca engelde dolu.</summary>
        public EngelTanimi? Engel { get; }

        /// <summary>Engelin kalan katmanı (GDD 6.1.1); engel olmayan taşlarda 0.</summary>
        public int Katman { get; internal set; }

        /// <summary>Özel taşın rengi yoktur ve renk eşleşmesine girmez (GDD 2.2.3).</summary>
        public bool OzelMi => Tur != TasTuru.Normal && Tur != TasTuru.Engel;

        /// <summary>
        /// Özel taş tetiklendi ama etkisi henüz bitmedi (ör. zincirle vurulmuş, rengini bekleyen renk küresi; AK-09).
        /// Tetiklenmiş taş ikinci kez tetiklenmez (GDD 3.1.2).
        /// </summary>
        public bool Tetiklendi { get; internal set; }

        /// <summary>Taşın yerine oturduğu mantıksal düşme adımı (GDD 5.1.7).</summary>
        public long OturmaAdimi { get; set; }

        /// <summary>Düşmekte olan taş oturana kadar eşleşme aramasına alınmaz (GDD 2.2.4).</summary>
        public bool Oturdu { get; set; }

        public override string ToString() =>
            Tur == TasTuru.Normal ? $"Normal#{Kimlik}(renk {Renk})"
            : Tur == TasTuru.Cizgi ? $"Cizgi#{Kimlik}({CizgiYonu})"
            : Tur == TasTuru.Engel ? $"{Engel!.Ad}#{Kimlik}(katman {Katman})"
            : $"{Tur}#{Kimlik}";
    }
}
