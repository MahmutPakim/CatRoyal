using System.Collections.Generic;
using CatRoyal.Cekirdek.Dinamik;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Etkiler;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Rastgelelik;

namespace CatRoyal.Cekirdek.Hamle
{
    public enum HamleSonucu
    {
        /// <summary>Hamle kabul edildi (GDD 2.3.3, 2.3.5).</summary>
        Gecerli,

        /// <summary>Eşleşme yok ve özel taş yok: taşlar geri döner, tahta değişmez (GDD 2.3.4).</summary>
        Gecersiz,

        /// <summary>Kaydırılamayan ya da dokunulamayan taş: tahta değişmez (GDD 2.3.2, 6.2.7).</summary>
        Imkansiz,

        /// <summary>
        /// Dokunma hiçbir şey yapmadı ve hamle sayılmaz: özel taş olmayan hücre ya da hedefleyecek taşı
        /// olmayan renk küresi (GDD 2.3.6).
        /// </summary>
        EtkisizDokunma,

        /// <summary>Bölüm "oynanıyor" durumunda değil; hamle kabul edilmedi (GDD 2.3.7).</summary>
        Reddedildi,
    }

    /// <summary>Bir hamlenin akışının ne kadarının çalışacağı (GDD 5.7).</summary>
    public enum HamleKapsami
    {
        /// <summary>Yalnızca patlama adımı (5.7 adım 1–7): düşme, doldurma, kaskad ve karıştırma yok.</summary>
        YalnizPatlama,

        /// <summary>Tahta durulana kadar (5.7 adım 1–8); durulunca karıştırma denetimi yapılmaz.</summary>
        Durulma,

        /// <summary>Tam akış (5.7 adım 1–9): durulunca olası hamle yoksa karıştırma.</summary>
        Tam,

        /// <summary>
        /// Yalnızca hamlenin kendi dalgası; durulma çağıranın adımlarıyla sürer (GDD 2.3.9). Bekleyen renk küreleri
        /// ve yayılmalar verilen motorda kalır.
        /// </summary>
        Adimli,
    }

    /// <summary>
    /// Oyuncu hamlesinin akışı (GDD 2.3, 5.7 adım 1–9): geçerlilik, patlama, tahtanın durulması, karıştırma.
    /// Hamle sayacı, hedefler ve kazanma/kaybetme 6. aşamada bu akışın çevresine eklenir.
    /// </summary>
    public static class HamleIsleyici
    {
        /// <summary>
        /// Oyuncu <paramref name="cikilan"/> hücredeki taşı <paramref name="gidilen"/> hücreye kaydırır (GDD 0.5).
        /// </summary>
        /// <param name="baglam">Bölüm bağlamı.</param>
        /// <param name="cikilan">Oyuncunun tuttuğu taşın hücresi.</param>
        /// <param name="gidilen">Taşın kaydırıldığı hücre.</param>
        /// <param name="olaylar">Üretilen olayların ekleneceği liste.</param>
        /// <param name="kapsam">Akışın ne kadarının çalışacağı; varsayılan tam akış.</param>
        /// <param name="gecerliOlunca">
        /// Hamle geçerli sayıldığı anda, patlamalardan önce çağrılır: hamle sayacı burada azalır (GDD 7.2.1).
        /// </param>
        public static HamleSonucu Kaydir(
            OyunBaglami baglam,
            Konum cikilan,
            Konum gidilen,
            List<Olay> olaylar,
            HamleKapsami kapsam = HamleKapsami.Tam,
            System.Action? gecerliOlunca = null,
            EtkiMotoru? surenMotor = null)
        {
            var tahta = baglam.Tahta;

            // GDD 2.3.1, 2.3.2. Yorum: gidilen hücre dik komşu değilse ya da oradaki taş kaydırılamıyorsa
            // (boş, oynanmayan, kilitli) hamle yine "imkânsız"dır; GDD yalnız kaynak taşı anar.
            if (!cikilan.DikKomsuMu(gidilen) || !tahta.Kaydirilabilir(cikilan) || !tahta.Kaydirilabilir(gidilen))
            {
                olaylar.Add(new ImkansizHamle(cikilan, gidilen));
                return HamleSonucu.Imkansiz;
            }

            var kaynak = tahta[cikilan];
            var hedef = tahta[gidilen];
            var suruklenen = kaynak.Tas!;
            var diger = hedef.Tas!;
            var motor = surenMotor ?? new EtkiMotoru(baglam, olaylar);

            YerDegistir(kaynak, hedef);

            if (suruklenen.OzelMi && diger.OzelMi)
            {
                // GDD 3.1.5, 4.1.3: iki özel taş — kombinasyon; geçerli hamle.
                olaylar.Add(new TaslarYerDegistirdi(cikilan, gidilen));
                gecerliOlunca?.Invoke();
                motor.KombinasyonBaslat(cikilan, gidilen);
                HamleyiBitir(baglam, motor, olaylar, kapsam);
                return HamleSonucu.Gecerli;
            }

            if (suruklenen.OzelMi || diger.OzelMi)
            {
                // GDD 2.3.3 (b), 4.1.4: bir özel taş — özel taş kendini etkinleştirir.
                olaylar.Add(new TaslarYerDegistirdi(cikilan, gidilen));
                gecerliOlunca?.Invoke();
                var ozelHucre = suruklenen.OzelMi ? gidilen : cikilan;
                var digerHucre = suruklenen.OzelMi ? cikilan : gidilen;
                var digerTas = suruklenen.OzelMi ? diger : suruklenen;

                // GDD 11.2.29: diğer taşın yeni yerinde eşleşme oluşursa, özel taşın etkisinden önce patlar.
                var yanGruplar = EslesmeBulucu.Bul(tahta, baglam.Bayraklar, new[] { digerHucre });
                motor.GruplariPatlat(
                    yanGruplar,
                    grup => DogumHucresi.OyuncuHamlesi(grup, cikilan, gidilen),
                    cikilan: cikilan,
                    gidilen: gidilen);

                // GDD 3.5.1, 3.5.5: renk küresi diğer taşın rengini hedefler; rengi yoksa en yaygın renk seçilir.
                int? renk = digerTas.Tur == TasTuru.Normal ? digerTas.Renk : (int?)null;
                motor.OzelTasiEtkinlestir(ozelHucre, Tetik.Kaydirma, renk);
                HamleyiBitir(baglam, motor, olaylar, kapsam);
                return HamleSonucu.Gecerli;
            }

            // GDD 2.4.4: oyuncu hamlesinde eşleşme, kaydırılan iki hücreyi içeren gruplarda aranır.
            var gruplar = EslesmeBulucu.Bul(tahta, baglam.Bayraklar, new[] { cikilan, gidilen });
            if (gruplar.Count == 0)
            {
                YerDegistir(kaynak, hedef);
                olaylar.Add(new GecersizHamle(cikilan, gidilen));
                return HamleSonucu.Gecersiz;
            }

            olaylar.Add(new TaslarYerDegistirdi(cikilan, gidilen));
            gecerliOlunca?.Invoke();
            motor.GruplariPatlat(
                gruplar,
                grup => DogumHucresi.OyuncuHamlesi(grup, cikilan, gidilen),
                cikilan: cikilan,
                gidilen: gidilen);
            HamleyiBitir(baglam, motor, olaylar, kapsam);
            return HamleSonucu.Gecerli;
        }

        /// <summary>Oyuncu bir özel taşa dokunarak onu yerinde etkinleştirir (GDD 2.3.5).</summary>
        public static HamleSonucu Dokun(
            OyunBaglami baglam,
            Konum konum,
            List<Olay> olaylar,
            HamleKapsami kapsam = HamleKapsami.Tam,
            System.Action? gecerliOlunca = null,
            EtkiMotoru? surenMotor = null)
        {
            var tahta = baglam.Tahta;
            var tas = tahta.TasAl(konum);
            if (tas == null || !tas.OzelMi)
            {
                return HamleSonucu.EtkisizDokunma;
            }

            // GDD 6.2.7: dokunmayı engelleyen katmanın altındaki özel taşa dokunmak imkânsız hamledir.
            if (!tahta.Kaydirilabilir(konum))
            {
                olaylar.Add(new ImkansizHamle(konum, konum));
                return HamleSonucu.Imkansiz;
            }

            var motor = surenMotor ?? new EtkiMotoru(baglam, olaylar);

            // GDD 2.3.6: hedefleyecek taşı olmayan renk küresine dokunmak hiçbir şey yapmaz, hamle sayılmaz.
            if (tas.Tur == TasTuru.RenkKuresi && !motor.EnYayginRenk().HasValue)
            {
                return HamleSonucu.EtkisizDokunma;
            }

            gecerliOlunca?.Invoke();
            motor.OzelTasiEtkinlestir(konum, Tetik.Dokunma);
            HamleyiBitir(baglam, motor, olaylar, kapsam);
            return HamleSonucu.Gecerli;
        }

        /// <summary>
        /// Kaskadın eşleşme adımı: tahtadaki bütün eşleşmeler bulunur ve patlatılır (GDD 5.5.1, 5.5.3).
        /// </summary>
        /// <returns>Patlayan grup sayısı.</returns>
        public static int KaskadEslesmeleriniPatlat(
            OyunBaglami baglam,
            List<Olay> olaylar,
            IRastgele? dogumEsitlikRastgelesi = null)
        {
            var gruplar = EslesmeBulucu.Bul(baglam.Tahta, baglam.Bayraklar);
            new EtkiMotoru(baglam, olaylar).GruplariPatlat(
                gruplar,
                grup => DogumHucresi.Kaskad(baglam.Tahta, grup, baglam.Bayraklar, dogumEsitlikRastgelesi));
            return gruplar.Count;
        }

        /// <summary>
        /// GDD 5.7 adım 8–9: tahta durulana kadar düşme, doldurma ve kaskad; durulunca olası hamle yoksa karıştırma.
        /// </summary>
        private static void HamleyiBitir(OyunBaglami baglam, EtkiMotoru motor, List<Olay> olaylar, HamleKapsami kapsam)
        {
            if (kapsam == HamleKapsami.Adimli)
            {
                motor.DalgayiBitir();
                return;
            }

            TahtaDongusu.Durult(baglam, motor, olaylar, dusme: kapsam != HamleKapsami.YalnizPatlama);
            if (kapsam == HamleKapsami.Tam)
            {
                Karistirici.GerekirseKaristir(baglam, olaylar);
            }
        }

        private static void YerDegistir(Hucre a, Hucre b)
        {
            var gecici = a.Tas;
            a.Tas = b.Tas;
            b.Tas = gecici;
        }
    }
}
