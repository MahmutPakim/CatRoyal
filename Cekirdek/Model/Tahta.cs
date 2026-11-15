using System;
using System.Collections.Generic;

namespace CatRoyal.Cekirdek.Model
{
    /// <summary>Tahtanın tek hücresi (GDD 2.1).</summary>
    public sealed class Hucre
    {
        /// <summary>Oynanmayan hücre hiçbir zaman içerik taşımaz (GDD 2.1.3).</summary>
        public bool Oynanabilir { get; set; } = true;

        /// <summary>Hücredeki taş; boşsa null.</summary>
        public Tas? Tas { get; set; }

        /// <summary>Taşın altındaki katman: zemin ya da yayılan zemin (GDD 6.1.6, 6.2.2).</summary>
        public HucreKatmani? AltKatman { get; set; }

        /// <summary>Taşın üstündeki katman: kaplama, kilit, örtü (GDD 6.1.6, 6.2.2).</summary>
        public HucreKatmani? UstKatman { get; set; }

        /// <summary>
        /// Hücre doğum hücresi olamaz mı (GDD 2.7.5)? Kilit arketipinde doğrudur. Atama, hücreye tek katmanlı
        /// bir kilit koyar ya da üst katmanı kaldırır (testler için kısayol).
        /// </summary>
        public bool Kilitli
        {
            get => UstKatman != null && UstKatman.Tanim.DogumHucresiniEngeller;
            set => UstKatman = value ? new HucreKatmani(KatmanArketipleri.Kilit(), 1) : null;
        }

        /// <summary>Üst katman düşmeyi engelliyor mu (GDD 6.2.2)?</summary>
        public bool DusmeEngelli => UstKatman != null && UstKatman.Tanim.DusmeyiEngeller;

        /// <summary>Üst katman kaydırmayı ve dokunmayı engelliyor mu (GDD 6.2.2, 6.2.6, 6.2.7)?</summary>
        public bool KaydirmaEngelli => UstKatman != null && UstKatman.Tanim.KaydirmayiEngeller;

        /// <summary>Toplama hücresi: aşağı inen toplanabilirler burada toplanır (GDD 6.3.5).</summary>
        public bool Toplama { get; set; }

        /// <summary>Giriş hücresi: yeni taşlar tahtaya yalnızca buradan girer (GDD 2.1.6, 5.3.1).</summary>
        public bool Giris { get; set; }

        /// <summary>Hücre çapraz kaymaya kapatılmış mı (GDD 5.2.4)?</summary>
        public bool CaprazKaymaKapali { get; set; }
    }

    /// <summary>Dikdörtgen hücre ızgarası (GDD 2.1.1). Koordinatlar <see cref="Konum"/> düzenindedir.</summary>
    public sealed class Tahta
    {
        private readonly Hucre[,] _hucreler;
        private int _sonrakiKimlik = 1;

        public Tahta(int satirSayisi, int sutunSayisi)
        {
            if (satirSayisi < 1 || sutunSayisi < 1)
            {
                throw new ArgumentException("Tahtanın en az bir satırı ve bir sütunu olmalı.");
            }

            SatirSayisi = satirSayisi;
            SutunSayisi = sutunSayisi;
            _hucreler = new Hucre[satirSayisi, sutunSayisi];
            for (int s = 0; s < satirSayisi; s++)
            {
                for (int t = 0; t < sutunSayisi; t++)
                {
                    _hucreler[s, t] = new Hucre();
                }
            }
        }

        public int SatirSayisi { get; }
        public int SutunSayisi { get; }

        public Hucre this[Konum konum]
        {
            get
            {
                if (!Icinde(konum))
                {
                    throw new ArgumentOutOfRangeException(nameof(konum), $"{konum} tahtanın dışında.");
                }

                return _hucreler[konum.Satir - 1, konum.Sutun - 1];
            }
        }

        public Hucre this[int satir, int sutun] => this[new Konum(satir, sutun)];

        public bool Icinde(Konum konum) =>
            konum.Satir >= 1 && konum.Satir <= SatirSayisi && konum.Sutun >= 1 && konum.Sutun <= SutunSayisi;

        /// <summary>Tahtanın içinde ve oynanabilir mi?</summary>
        public bool Oynanabilir(Konum konum) => Icinde(konum) && this[konum].Oynanabilir;

        /// <summary>Bütün hücreler, sol-üstten satır satır.</summary>
        public IEnumerable<Konum> TumKonumlar()
        {
            for (int s = 1; s <= SatirSayisi; s++)
            {
                for (int t = 1; t <= SutunSayisi; t++)
                {
                    yield return new Konum(s, t);
                }
            }
        }

        /// <summary>
        /// Çok ekranlı bölümde (GDD 8.9.2) taşlar bir ekrandan diğerine taşınır; taş kimlikleri çakışmasın diye
        /// yeni ekranın tahtası sayacı öncekinden devralır.
        /// </summary>
        internal void KimlikSayaciniDevral(Tahta onceki) => _sonrakiKimlik = Math.Max(_sonrakiKimlik, onceki._sonrakiKimlik);

        public Tas? TasAl(Konum konum) => Icinde(konum) ? this[konum].Tas : null;

        public Tas NormalTasUret(int renk)
        {
            if (renk < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(renk), "Renk negatif olamaz.");
            }

            return new Tas(_sonrakiKimlik++, TasTuru.Normal, renk, null);
        }

        public Tas OzelTasUret(TasTuru tur, Yon? cizgiYonu = null)
        {
            if (tur == TasTuru.Normal)
            {
                throw new ArgumentException("Normal taş için NormalTasUret kullanılır.", nameof(tur));
            }

            if ((tur == TasTuru.Cizgi) != cizgiYonu.HasValue)
            {
                throw new ArgumentException("Yön yalnızca çizgi taşında ve zorunlu olarak verilir.", nameof(cizgiYonu));
            }

            return new Tas(_sonrakiKimlik++, tur, Tas.RenkYok, cizgiYonu);
        }

        public Tas EngelUret(EngelTanimi tanim, int katman)
        {
            if (katman < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(katman), "Engelin en az bir katmanı olmalı.");
            }

            return new Tas(_sonrakiKimlik++, TasTuru.Engel, Tas.RenkYok, null, tanim, katman);
        }

        /// <summary>
        /// Hücrenin renk eşleşmesine giren rengi; eşleşmeye giremiyorsa <see cref="Tas.RenkYok"/>.
        /// Yalnızca yerine oturmuş normal taşlar eşleşir (GDD 2.2.3, 2.2.4, 2.4.1).
        /// </summary>
        public int EslesmeRengi(Konum konum)
        {
            if (!Oynanabilir(konum))
            {
                return Tas.RenkYok;
            }

            var tas = this[konum].Tas;
            return tas != null && tas.Tur == TasTuru.Normal && tas.Oturdu ? tas.Renk : Tas.RenkYok;
        }

        /// <summary>Hücredeki öğe düşer mi (GDD 5.1.5, 6.2)? Kilitli taş ve düşmeyen engel düşmez.</summary>
        public bool Duser(Konum konum)
        {
            if (!Oynanabilir(konum))
            {
                return false;
            }

            var hucre = this[konum];
            if (hucre.Tas == null || hucre.DusmeEngelli)
            {
                return false;
            }

            return hucre.Tas.Tur != TasTuru.Engel || hucre.Tas.Engel!.Duser;
        }

        /// <summary>Hücredeki taş oyuncu tarafından kaydırılabilir mi (GDD 2.3.2, 6.2.6)?</summary>
        public bool Kaydirilabilir(Konum konum)
        {
            if (!Oynanabilir(konum))
            {
                return false;
            }

            var hucre = this[konum];
            if (hucre.Tas == null || hucre.KaydirmaEngelli || hucre.Tas.Tetiklendi)
            {
                return false;
            }

            return hucre.Tas.Tur != TasTuru.Engel || hucre.Tas.Engel!.Kaydirilir;
        }
    }
}
