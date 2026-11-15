using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Meta katmanın saati. Zaman ölçen kurallar saati dışarıdan alır; testler sahte saatle çalışır.
    /// Sunucuya bağlı sistemlerde (günlük ödül, olaylar) bu saat sunucu saatidir (GDD 9.5.4).
    /// </summary>
    public interface ISaat
    {
        DateTime SimdiUtc { get; }
    }

    public sealed class SistemSaati : ISaat
    {
        public DateTime SimdiUtc => DateTime.UtcNow;
    }

    /// <summary>Takviye türleri (GDD 9.3.1): başlangıç takviyeleri ve oyun içi araçlar.</summary>
    public enum TakviyeTuru
    {
        Cizgi,
        Bomba,
        RenkKuresi,

        /// <summary>Satılmaz; yalnızca ödül olarak gelir (GDD 9.3.1).</summary>
        Gudumlu,
        TekHucre,
        Satir,
        Sutun,
        Karistirma,
    }

    /// <summary>
    /// Ödül paketi (GDD 9.2.6): bütün satın almalar ve ödüller aynı yapıyı kullanır.
    /// </summary>
    public sealed class OdulPaketi
    {
        public int Para { get; set; }
        public int Can { get; set; }
        public int SinirsizCanDakika { get; set; }
        public int Yildiz { get; set; }
        public Dictionary<TakviyeTuru, int> Takviyeler { get; } = new Dictionary<TakviyeTuru, int>();
        public Dictionary<TakviyeTuru, int> TakviyeSinirsizSaniye { get; } = new Dictionary<TakviyeTuru, int>();

        /// <summary>Etkinlik çarpanı süreleri, dakika (GDD 9.10.4).</summary>
        public int CarpanDakika { get; set; }

        /// <summary>Olay eşyaları: olay kimliği → adet (bilye, enerji, toplanabilir; GDD 10.9.5).</summary>
        public Dictionary<string, int> OlayEsyalari { get; } = new Dictionary<string, int>();

        /// <summary>Albüm eşyaları: kart paketi kimliği → adet (GDD 9.7.2).</summary>
        public Dictionary<string, int> AlbumEsyalari { get; } = new Dictionary<string, int>();

        public List<string> Kozmetikler { get; } = new List<string>();

        /// <summary>
        /// Sıralamalı yarışma ödülü (yarışmalı bölümler ve sıralamalı yarışma olayları) sınırsız can içeriyorsa
        /// süresi parametre: hediye_sinirsiz_can_dk kadardır (GDD 9.1.9). Diğer ödüllerdeki süreler kendi ödül
        /// tablolarında yazılır ve <see cref="SinirsizCanDakika"/> doğrudan verilir.
        /// </summary>
        public OdulPaketi YarismaSinirsizCaniEkle(CatRoyal.Cekirdek.Yapilandirma.ParametreTablosu parametreler)
        {
            SinirsizCanDakika += parametreler.TamSayi(MetaParametreAdlari.HediyeSinirsizCanDk);
            return this;
        }
    }

    /// <summary>Çekirdek parametre tablosunda meta katmanın okuduğu adlar.</summary>
    public static class MetaParametreAdlari
    {
        public const string AzamiCan = "azami_can";
        public const string AzamiCanPremium = "azami_can_premium";
        public const string CanDolumSuresiDk = "can_dolum_suresi_dk";
        public const string CanDolumTeklifiFiyati = "can_dolum_teklifi_fiyati";
        public const string GelenKutusuAzami = "gelen_kutusu_azami";
        public const string BaslangicParasi = "baslangic_parasi";
        public const string KazanmaParasiNormal = "kazanma_parasi_normal";
        public const string KazanmaParasiZor = "kazanma_parasi_zor";
        public const string KazanmaParasiCokZor = "kazanma_parasi_cok_zor";
        public const string KazanmaParasiAsiriZor = "kazanma_parasi_asiri_zor";
        public const string KalanHamleBasinaPara = "kalan_hamle_basina_para";
        public const string FiyatCizgi = "fiyat_cizgi";
        public const string FiyatBomba = "fiyat_bomba";
        public const string FiyatRenkKuresi = "fiyat_renk_kuresi";
        public const string FiyatTekHucre = "fiyat_tek_hucre";
        public const string FiyatSatir = "fiyat_satir";
        public const string FiyatSutun = "fiyat_sutun";
        public const string FiyatKaristirma = "fiyat_karistirma";
        public const string YuksekFiyatCarpani = "yuksek_fiyat_carpani";
        public const string TakviyeAzamiAdet = "takviye_azami_adet";
        public const string AcilisBaslangicTakviyeleri = "acilis_baslangic_takviyeleri";
        public const string AcilisTekHucreAraci = "acilis_tek_hucre_araci";
        public const string AcilisSatirAraci = "acilis_satir_araci";
        public const string AcilisSutunAraci = "acilis_sutun_araci";
        public const string AcilisKaristirmaAraci = "acilis_karistirma_araci";
        public const string AcilisGunlukOdul = "acilis_gunluk_odul";
        public const string AcilisSezonHatti = "acilis_sezon_hatti";
        public const string AcilisHaftalikHat = "acilis_haftalik_hat";
        public const string AcilisCiftRenkTakviyesi = "acilis_cift_renk_takviyesi";
        public const string AcilisDolumGostergesi = "acilis_dolum_gostergesi";
        public const string AcilisYedekHamleHakki = "acilis_yedek_hamle_hakki";
        public const string AcilisSeriTakviyesi = "acilis_seri_takviyesi";
        public const string AcilisCarpanBileti = "acilis_carpan_bileti";
        public const string AcilisOlaySimgeleri = "acilis_olay_simgeleri";
        public const string AcilisIlkToplamaOlayi = "acilis_ilk_toplama_olayi";
        public const string CiftRenkSeriEsigi = "cift_renk_seri_esigi";
        public const string YedekHamleSeriEsigi = "yedek_hamle_seri_esigi";
        public const string SeriTakviyesiAzami = "seri_takviyesi_azami";
        public const string GunSiniriSaati = "gun_siniri_saati";
        public const string HaftalikSeriUzunlugu = "haftalik_seri_uzunlugu";
        public const string AylikSeriUzunlugu = "aylik_seri_uzunlugu";
        public const string HaftalikSetSayisi = "haftalik_set_sayisi";
        public const string HileToleransiDk = "hile_toleransi_dk";
        public const string BolumBasinaYildiz = "bolum_basina_yildiz";
        public const string GorevYildizFiyati = "gorev_yildiz_fiyati";
        public const string AlanSandigiPara = "alan_sandigi_para";
        public const string SetBasinaKart = "set_basina_kart";
        public const string SetSayisi = "set_sayisi";
        public const string SezonHattiBasamakSayisi = "sezon_hatti_basamak_sayisi";
        public const string SezonHattiBasamakHedefleri = "sezon_hatti_basamak_hedefleri";
        public const string HatPuaniNormal = "hat_puani_normal";
        public const string HatPuaniZor = "hat_puani_zor";
        public const string HatPuaniCokZor = "hat_puani_cok_zor";
        public const string HatPuaniAsiriZor = "hat_puani_asiri_zor";
        public const string HatPuaniOzel = "hat_puani_ozel";
        public const string HazneAsamaPuani = "hazne_asama_puani";
        public const string HazneAsamaParasi = "hazne_asama_parasi";
        public const string SezonHazneHedefi = "sezon_hazne_hedefi";
        public const string HaftalikHatBasamakSayisi = "haftalik_hat_basamak_sayisi";
        public const string HaftalikHatBasamakHedefleri = "haftalik_hat_basamak_hedefleri";
        public const string HaftalikHazneHedefi = "haftalik_hazne_hedefi";
        public const string HaftalikHazneAsamaParasi = "haftalik_hazne_asama_parasi";
        public const string CarpanBiletiFiyati = "carpan_bileti_fiyati";
        public const string CarpanBiletiKati = "carpan_bileti_kati";
        public const string EtkinlikCarpaniSuresiDk = "etkinlik_carpani_suresi_dk";
        public const string DenemeSayaciAzami = "deneme_sayaci_azami";
        public const string ZorlukRastgelelikAgirligi = "zorluk_rastgelelik_agirligi";
        public const string EkHamleSayisi = "ek_hamle_sayisi";
        public const string EkHamleFiyatKademeleri = "ek_hamle_fiyat_kademeleri";
        public const string EkHamlePaketIcerikleri = "ek_hamle_paket_icerikleri";
        public const string EkHamleArtisEsigi = "ek_hamle_artis_esigi";
        public const string EkHamleArtisMiktari = "ek_hamle_artis_miktari";
        public const string EkHamleArtisSiniri = "ek_hamle_artis_siniri";
        public const string IkinciUyariSonBolum = "ikinci_uyari_son_bolum";
        public const string ToplamaOlayiBasamakSayisi = "toplama_olayi_basamak_sayisi";
        public const string RiskBasamakHedefleri = "risk_basamak_hedefleri";
        public const string RiskBasamakSansHakki = "risk_basamak_sans_hakki";
        public const string ReklamParaOdulu = "reklam_para_odulu";
        public const string ReklamCanOdulu = "reklam_can_odulu";
        public const string EkHamleReklamSayisi = "ek_hamle_reklam_sayisi";
        public const string AcilisMagaza = "acilis_magaza";
        public const string MagazaUrunSayisi = "magaza_urun_sayisi";
        public const string AcilisKademeliTeklif = "acilis_kademeli_teklif";
        public const string AcilisKarsilamaHediyesi = "acilis_karsilama_hediyesi";
        public const string HesapBaglamaOdulu = "hesap_baglama_odulu";
        public const string AcilisLiderlikTablosu = "acilis_liderlik_tablosu";
        public const string AcilisArkadasDaveti = "acilis_arkadas_daveti";
        public const string AcilisGorevler = "acilis_gorevler";
        public const string AcilisCarpanSecimi = "acilis_carpan_secimi";
        public const string AcilisYarismaliBolumler = "acilis_yarismali_bolumler";
        public const string HediyeSinirsizCanDk = "hediye_sinirsiz_can_dk";
        public const string BeceriOlcumSonBolum = "beceri_olcum_son_bolum";
        public const string BeceriEsikleri = "beceri_esikleri";
    }
}
