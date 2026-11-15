using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Tasarim
{
    /// <summary>Bölüm tasarımı ve dağıtımının (GDD 8, 10.2) okuduğu parametre adları.</summary>
    public static class TasarimParametreAdlari
    {
        public const string OynanmayanHucrePayi = "oynanmayan_hucre_payi";
        public const string BosBaslayanHucrePayiErken = "bos_baslayan_hucre_payi_erken";
        public const string BosBaslayanHucrePayi = "bos_baslayan_hucre_payi";
        public const string HamleBandiAlt = "hamle_bandi_alt";
        public const string HamleBandiUst = "hamle_bandi_ust";
        public const string AsgariHamle = "asgari_hamle";
        public const string HamleOrtalamaErken = "hamle_ortalama_erken";
        public const string HamleOrtalamaGec = "hamle_ortalama_gec";
        public const string ZorlukKirilmaBolumu = "zorluk_kirilma_bolumu";
        public const string HedefHamleOraniErken = "hedef_hamle_orani_erken";
        public const string HedefHamleOraniGec = "hedef_hamle_orani_gec";
        public const string EngelYogunlugu = "engel_yogunlugu";
        public const string BolumAzamiEngelTuru = "bolum_azami_engel_turu";
        public const string ErkenAzamiHedefTuru = "erken_azami_hedef_turu";
        public const string TanitimIlkBolum = "tanitim_ilk_bolum";
        public const string TanitimAraligiErken = "tanitim_araligi_erken";
        public const string ErkenDonemSonu = "erken_donem_sonu";
        public const string TanitimBloguOrta = "tanitim_blogu_orta";
        public const string AcilisBonusBolumleri = "acilis_bonus_bolumleri";
        public const string BonusAraligiErken = "bonus_araligi_erken";
        public const string BonusAraligiGec = "bonus_araligi_gec";
        public const string BonusGecDonemBaslangici = "bonus_gec_donem_baslangici";
        public const string ZorBolumAraligi = "zor_bolum_araligi";
        public const string ZorBolumHamleFarki = "zor_bolum_hamle_farki";
        public const string AcilisCokEkranli = "acilis_cok_ekranli";
        public const string BolumPaketiUzunlugu = "bolum_paketi_uzunlugu";
        public const string KurtarmaIlkBolum = "kurtarma_ilk_bolum";
        public const string KurtarmaAraligiErken = "kurtarma_araligi_erken";
        public const string KurtarmaAraligiGec = "kurtarma_araligi_gec";
        public const string KurtarmaGecDonemBaslangici = "kurtarma_gec_donem_baslangici";
        public const string KurtarmaTemaSayisi = "kurtarma_tema_sayisi";
        public const string KurtarmaAnaTemaPayi = "kurtarma_ana_tema_payi";
        public const string KurtarmaKazanmaParasi = "kurtarma_kazanma_parasi";
    }

    /// <summary>
    /// Bonus bölümlerinin takvimi (GDD 8.7): bir açılış bölümünden başlar (8.7.3), erken dönemde kısa, sonra uzun
    /// aralıkla gelir (8.7.1).
    /// </summary>
    public sealed class BonusTakvimi
    {
        private readonly int _ilk;
        private readonly int _erkenAralik;
        private readonly int _gecAralik;
        private readonly int _gecBaslangic;

        public BonusTakvimi(ParametreTablosu p)
        {
            _ilk = p.TamSayi(TasarimParametreAdlari.AcilisBonusBolumleri);
            _erkenAralik = Math.Max(1, p.TamSayi(TasarimParametreAdlari.BonusAraligiErken));
            _gecAralik = Math.Max(1, p.TamSayi(TasarimParametreAdlari.BonusAraligiGec));
            _gecBaslangic = p.TamSayi(TasarimParametreAdlari.BonusGecDonemBaslangici);
        }

        /// <summary>Verilen bölüme kadar (dahil) bonus bölümleri, artan sırayla.</summary>
        public IEnumerable<int> Bolumler(int sonBolum)
        {
            for (int bolum = _ilk; bolum <= sonBolum; bolum += bolum < _gecBaslangic ? _erkenAralik : _gecAralik)
            {
                yield return bolum;
            }
        }

        public bool BonusMu(int bolum) => Bolumler(bolum).Contains(bolum);
    }

    /// <summary>
    /// Zor bölüm ritmi (GDD 8.8.3): bölümler sabit uzunlukta bloklara ayrılır ve her bloğun son bölümü diğerlerinden
    /// daha az hamle verir. Ritim bir tasarım aracıdır; zorluk etiketiyle aynı şey değildir (11.2.15).
    /// </summary>
    public sealed class ZorBolumRitmi
    {
        public ZorBolumRitmi(ParametreTablosu p)
        {
            BlokUzunlugu = Math.Max(1, p.TamSayi(TasarimParametreAdlari.ZorBolumAraligi));
            HamleFarki = p.Sayi(TasarimParametreAdlari.ZorBolumHamleFarki);
        }

        public int BlokUzunlugu { get; }

        /// <summary>Zor bölümün aynı bloktaki diğerlerinden eksik verdiği ortalama hamle.</summary>
        public double HamleFarki { get; }

        /// <summary>Bölümün 1 tabanlı blok numarası.</summary>
        public int Blok(int bolum) => ((bolum - 1) / BlokUzunlugu) + 1;

        /// <summary>Bloğun son bölümü mü?</summary>
        public bool ZorMu(int bolum) => bolum >= 1 && bolum % BlokUzunlugu == 0;
    }

    /// <summary>Bir ilerleme bölümüne bağlı kurtarma bölümü (GDD 10.2.1).</summary>
    public sealed class KurtarmaEslemesi
    {
        public KurtarmaEslemesi(int ilerlemeBolumu, int kurtarmaNo, int tema)
        {
            IlerlemeBolumu = ilerlemeBolumu;
            KurtarmaNo = kurtarmaNo;
            Tema = tema;
        }

        /// <summary>Oyuncu bu bölüme gelince normal bölüm yerine kurtarma bölümü yüklenir.</summary>
        public int IlerlemeBolumu { get; }

        /// <summary>Kurtarma bölümünün 1 tabanlı numarası.</summary>
        public int KurtarmaNo { get; }

        /// <summary>Tema numarası: 0 ana tema, 1…parametre: kurtarma_tema_sayisi dönüşümlü temalar (GDD 10.2.3).</summary>
        public int Tema { get; }

        public bool AnaTema => Tema == KurtarmaTakvimi.AnaTema;
    }

    /// <summary>
    /// Kurtarma bölümlerinin eşleme tablosu (GDD 10.2.1–10.2.3, 10.2.6): "ilerleme bölümü → (kurtarma bölümü
    /// numarası, tema)". Tablo parametrelerden üretilir ve uygulama içinde sabittir.
    ///
    /// Ritim (10.2.2): ilk kurtarma bölümü parametre: kurtarma_ilk_bolum'dedir; geç dönem başlayana kadar
    /// parametre: kurtarma_araligi_erken aralıkla, sonra parametre: kurtarma_araligi_gec uzunluğundaki blokların
    /// hep aynı sıradaki bölümünde gelir. Tema (10.2.3): her n. kurtarma bölümü ana temadadır (n, ana tema payının
    /// tersidir); aradakiler dönüşümlü temaları sırayla kullanır.
    /// </summary>
    public sealed class KurtarmaTakvimi
    {
        public const int AnaTema = 0;

        private readonly int _ilk;
        private readonly int _erkenAralik;
        private readonly int _gecAralik;
        private readonly int _gecBaslangic;
        private readonly int _temaSayisi;
        private readonly int _anaTemaAraligi;
        private readonly IReadOnlyList<int> _oneCekilen;

        /// <param name="p">Parametre tablosu.</param>
        /// <param name="oneCekilenBolumler">
        /// Deney grubunda ilk kurtarma bölümlerinin çekildiği ilerleme bölümleri (GDD 10.2.6), artan sırayla; ör.
        /// [2, 5] ilk iki kurtarma bölümünü 2. ve 5. bölüme bağlar. Sonraki kurtarma bölümleri olağan ritimde kalır.
        /// </param>
        public KurtarmaTakvimi(ParametreTablosu p, IReadOnlyList<int>? oneCekilenBolumler = null)
        {
            _ilk = p.TamSayi(TasarimParametreAdlari.KurtarmaIlkBolum);
            _erkenAralik = Math.Max(1, p.TamSayi(TasarimParametreAdlari.KurtarmaAraligiErken));
            _gecAralik = Math.Max(1, p.TamSayi(TasarimParametreAdlari.KurtarmaAraligiGec));
            _gecBaslangic = p.TamSayi(TasarimParametreAdlari.KurtarmaGecDonemBaslangici);
            _temaSayisi = Math.Max(0, p.TamSayi(TasarimParametreAdlari.KurtarmaTemaSayisi));

            double pay = p.Sayi(TasarimParametreAdlari.KurtarmaAnaTemaPayi);
            _anaTemaAraligi = pay <= 0 ? 0 : Math.Max(1, (int)Math.Round(1 / pay));
            if (_temaSayisi == 0 && _anaTemaAraligi != 1)
            {
                // Dönüşümlü tema yoksa bütün kurtarma bölümleri ana temadadır.
                _anaTemaAraligi = 1;
            }

            _oneCekilen = oneCekilenBolumler ?? Array.Empty<int>();
            for (int i = 0; i < _oneCekilen.Count; i++)
            {
                int oncekiSinir = i == 0 ? 0 : _oneCekilen[i - 1];
                if (_oneCekilen[i] <= oncekiSinir || _oneCekilen[i] >= OlaganBolum(i))
                {
                    throw new ArgumentException(
                        "Öne çekilen kurtarma bölümleri artan sırada ve olağan yerlerinden önce olmalı (GDD 10.2.6).",
                        nameof(oneCekilenBolumler));
                }
            }
        }

        /// <summary>Verilen ilerleme bölümüne kadar (dahil) eşlemeler, artan sırayla.</summary>
        public IEnumerable<KurtarmaEslemesi> Eslemeler(int sonBolum)
        {
            for (int i = 0; ; i++)
            {
                int bolum = i < _oneCekilen.Count ? _oneCekilen[i] : OlaganBolum(i);
                if (bolum > sonBolum)
                {
                    yield break;
                }

                yield return new KurtarmaEslemesi(bolum, i + 1, Tema(i));
            }
        }

        /// <summary>Bölüme bağlı kurtarma bölümü; yoksa null.</summary>
        public KurtarmaEslemesi? Bagli(int ilerlemeBolumu) =>
            Eslemeler(ilerlemeBolumu).FirstOrDefault(e => e.IlerlemeBolumu == ilerlemeBolumu);

        /// <summary>0 tabanlı sıradaki kurtarma bölümünün olağan ritimdeki ilerleme bölümü.</summary>
        private int OlaganBolum(int sira)
        {
            int bolum = _ilk;
            for (int i = 0; i < sira; i++)
            {
                if (bolum + _erkenAralik < _gecBaslangic)
                {
                    bolum += _erkenAralik;
                }
                else if (bolum < _gecBaslangic)
                {
                    // Geç döneme geçiş: ilk geç dönem bölümü, sonrakilerin hizalandığı blok sırasını belirler.
                    bolum = Math.Max(bolum + 1, _gecBaslangic);
                }
                else
                {
                    bolum += _gecAralik;
                }
            }

            return bolum;
        }

        private int Tema(int sira)
        {
            if (_anaTemaAraligi == 1)
            {
                return AnaTema;
            }

            if (_anaTemaAraligi == 0)
            {
                return 1 + (sira % _temaSayisi);
            }

            if (sira % _anaTemaAraligi == 0)
            {
                return AnaTema;
            }

            // Ana temalı olmayanlar kendi aralarında sayılır ve dönüşümlü temaları sırayla kullanır.
            int anaTemalilar = (sira / _anaTemaAraligi) + 1;
            return 1 + ((sira - anaTemalilar) % _temaSayisi);
        }
    }

    /// <summary>
    /// Mekanik tanıtım takvimi (GDD 8.6): erken dönemde parametre: tanitim_ilk_bolum'den başlayıp her
    /// parametre: tanitim_araligi_erken bölümde bir (8.6.1, 8.6.7); erken dönem bitince parametre: tanitim_blogu_orta
    /// uzunluğundaki blokların ilk bölümünde (8.6.3).
    ///
    /// Yorum: takvimdeki bölüm bir bonus ya da kurtarma bölümüne denk gelirse tanıtım bir sonraki olağan bölüme
    /// kayar; o bölümler yeni mekanik öğretmeye uygun değildir (8.7.2, 10.2.1). Geç dönem tanıtımları yalnızca
    /// ortalama bir aralıkla tanımlıdır (parametre: tanitim_araligi_gec) ve orta dönemin bittiği bölüm parametre
    /// dosyasında yoktur; takvim orta dönem ritmini sürdürür.
    /// </summary>
    public sealed class TanitimTakvimi
    {
        private readonly int _ilk;
        private readonly int _erkenAralik;
        private readonly int _erkenSon;
        private readonly int _ortaBlok;
        private readonly Func<int, bool> _dolu;

        public TanitimTakvimi(ParametreTablosu p, BonusTakvimi bonus, KurtarmaTakvimi kurtarma)
            : this(p, bolum => bonus.BonusMu(bolum) || kurtarma.Bagli(bolum) != null)
        {
        }

        /// <param name="p">Parametre tablosu.</param>
        /// <param name="dolu">Tanıtım yapılamayan bölümler (bonus, kurtarma).</param>
        public TanitimTakvimi(ParametreTablosu p, Func<int, bool> dolu)
        {
            _ilk = p.TamSayi(TasarimParametreAdlari.TanitimIlkBolum);
            _erkenAralik = Math.Max(1, p.TamSayi(TasarimParametreAdlari.TanitimAraligiErken));
            _erkenSon = p.TamSayi(TasarimParametreAdlari.ErkenDonemSonu);
            _ortaBlok = Math.Max(1, p.TamSayi(TasarimParametreAdlari.TanitimBloguOrta));
            _dolu = dolu;
        }

        /// <summary>Verilen bölüme kadar (dahil) tanıtım bölümleri, artan sırayla.</summary>
        public IEnumerable<int> Bolumler(int sonBolum)
        {
            int onceki = 0;
            foreach (int aday in Adaylar())
            {
                int bolum = Math.Max(aday, onceki + 1);
                while (_dolu(bolum))
                {
                    bolum++;
                }

                if (bolum > sonBolum)
                {
                    yield break;
                }

                onceki = bolum;
                yield return bolum;
            }
        }

        public bool TanitimBolumuMu(int bolum) => Bolumler(bolum).Contains(bolum);

        private IEnumerable<int> Adaylar()
        {
            int bolum = _ilk;
            for (; bolum <= _erkenSon; bolum += _erkenAralik)
            {
                yield return bolum;
            }

            for (bolum = _erkenSon + 1; ; bolum += _ortaBlok)
            {
                yield return bolum;
            }
        }
    }
}
