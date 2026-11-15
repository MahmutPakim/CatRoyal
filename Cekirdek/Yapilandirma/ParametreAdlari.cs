namespace CatRoyal.Cekirdek.Yapilandirma
{
    /// <summary>
    /// Çekirdeğin okuduğu parametre adları. Ad yazım hatasını derleme anına çekmek için sabit tutulur;
    /// değerlerin kendisi yalnızca parametreler.csv dosyasındadır (GDD 0.4).
    /// Her sabitin dosyada bulunduğu bir testle denetlenir.
    /// </summary>
    public static class ParametreAdlari
    {
        // Tahta ve renk (GDD 2.1, 2.2, 8.1, 8.2)
        public const string TahtaGenislikStandart = "tahta_genislik_standart";
        public const string TahtaYukseklikStandart = "tahta_yukseklik_standart";
        public const string KaydirmaliTahtaYukseklik = "kaydirmali_tahta_yukseklik";
        public const string DarTahtaAzamiSutunEksigi = "dar_tahta_azami_sutun_eksigi";
        public const string DarTahtaAzamiSatirEksigi = "dar_tahta_azami_satir_eksigi";
        public const string RenkSayisiStandart = "renk_sayisi_standart";
        public const string KolayBolumAzamiRenkEksigi = "kolay_bolum_azami_renk_eksigi";
        public const string IstisnaFazlaRenk = "istisna_fazla_renk";
        public const string EkranAzamiHedef = "ekran_azami_hedef";

        // Güdümlü hedef puanları (GDD 3.4.5)
        public const string GudumluPuanHedefOge = "gudumlu_puan_hedef_oge";
        public const string GudumluPuanEngel = "gudumlu_puan_engel";
        public const string GudumluPuanKatmanBasina = "gudumlu_puan_katman_basina";
        public const string GudumluPuanNormalTas = "gudumlu_puan_normal_tas";

        // Dolum göstergesi (GDD 3.7)
        public const string DolumKapasitesi = "dolum_kapasitesi";
        public const string DolumPuanCizgi = "dolum_puan_cizgi";
        public const string DolumPuanBomba = "dolum_puan_bomba";
        public const string DolumPuanGudumlu = "dolum_puan_gudumlu";
        public const string DolumPuanRenkTasBasina = "dolum_puan_renk_tas_basina";
        public const string DolumPuanKombinasyon = "dolum_puan_kombinasyon";
        public const string DolumPuanRenkRenk = "dolum_puan_renk_renk";

        // Yedek hamle hakkı ve ek hamle (GDD 3.8, 7.4)
        public const string YedekHamleEsigi = "yedek_hamle_esigi";
        public const string YedekHamleSayisi = "yedek_hamle_sayisi";
        public const string EkHamleSayisi = "ek_hamle_sayisi";

        // Tahta dinamikleri (GDD 5)
        public const string AyarliDolguSikiOlasiligi = "ayarli_dolgu_siki_olasiligi";
        public const string KaristirmaDenemeSayisi = "karistirma_deneme_sayisi";
        public const string YenidenKurmaDenemeSayisi = "yeniden_kurma_deneme_sayisi";
        public const string ZorlukRastgelelikAgirligi = "zorluk_rastgelelik_agirligi";

        // Engeller (GDD 6)
        public const string UreticiYayilmaHucreSayisi = "uretici_yayilma_hucre_sayisi";
        public const string TehlikeYayilmaSayisi = "tehlike_yayilma_sayisi";
        public const string KatmanAzamiKatmanliBlok = "katman_azami_katmanli_blok";
        public const string KatmanAzamiSertBlok = "katman_azami_sert_blok";
        public const string KatmanAzamiDusenKirilabilir = "katman_azami_dusen_kirilabilir";
        public const string KatmanAzamiCokluVurus = "katman_azami_coklu_vurus";
        public const string KatmanAzamiUretici = "katman_azami_uretici";
        public const string KatmanAzamiZemin = "katman_azami_zemin";
        public const string KatmanAzamiKilit = "katman_azami_kilit";
        public const string KatmanAzamiOrtu = "katman_azami_ortu";
        public const string KatmanAzamiSertKaplama = "katman_azami_sert_kaplama";

        /// <summary>Sunum katmanına ait parametrelerin ön eki. Çekirdek bu parametreleri okumaz (GDD 0.4).</summary>
        public const string SunumOnEki = "sunum_";
    }
}
