using System.Collections.Generic;
using CatRoyal.Cekirdek.Eslesme;
using CatRoyal.Cekirdek.Etkiler;
using CatRoyal.Cekirdek.Olaylar;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>
    /// Bir hamleden sonra tahtanın durulması (GDD 5.7 adım 8): dalgalar, düşme ve doldurma, kaskad; hepsi
    /// tahta durulana kadar tekrarlanır (5.5.1).
    /// </summary>
    public static class TahtaDongusu
    {
        /// <param name="baglam">Bölüm bağlamı.</param>
        /// <param name="motor">Hamlenin etki motoru; planlanmış etkileri ve bekleyen renk küreleri olabilir.</param>
        /// <param name="olaylar">Üretilen olayların ekleneceği liste.</param>
        /// <param name="dusme">
        /// False ise yalnızca dalgalar ve bekleyen renk küreleri işlenir; düşme, doldurma ve kaskad çalışmaz.
        /// Bir hamlenin patlama adımını tek başına incelemek için kullanılır.
        /// </param>
        public static void Durult(OyunBaglami baglam, EtkiMotoru motor, List<Olay> olaylar, bool dusme = true)
        {
            motor.DalgayiBitir();

            while (true)
            {
                // AK-09 B: rengi tetik anında kilitlenen küre, dalga tamamlanınca ışınlarını gönderir.
                if (Baslat(motor, EtkiMotoru.KureBeklemesi.Dalga))
                {
                    continue;
                }

                // GDD 6.3.2, 6.3.3: kırılan üretici ve patlayan tehlike, dalga tamamlandıktan sonra ve düşmeler
                // başlamadan çevresine yayılır.
                motor.YayilmalariIsle();

                // Dalga bitmeden düşme başlamaz; dalga bitince boşalan hücreler düşme ve doldurmayla durulur.
                if (dusme)
                {
                    DusmeIsleyici.Durult(baglam, olaylar);

                    // GDD 6.3.5: toplama hücresine inen toplanabilir toplanır; boşalan hücre yeniden dolar.
                    if (Toplayici.Topla(baglam, olaylar))
                    {
                        continue;
                    }
                }

                // AK-09 A: zincirle vurulan küre rengini tahta durulduktan sonra sayar; ışınlar hemen ardından gider.
                if (Baslat(motor, EtkiMotoru.KureBeklemesi.Durulma))
                {
                    continue;
                }

                // GDD 5.5.1: süren bir işlem yoksa tahtadaki bütün eşleşmeler bulunur ve patlatılır.
                if (dusme)
                {
                    var gruplar = EslesmeBulucu.Bul(baglam.Tahta, baglam.Bayraklar);
                    if (gruplar.Count > 0)
                    {
                        // GDD 2.7.4, 2.7.7: doğum hücresinde eşitlik zorluk ağırlıklı üreteçle bozulur;
                        // etkisiz ağırlıkta tarama sırası geçerlidir.
                        var esitlik = baglam.Zorluk.Etkisiz ? null : baglam.Zorluk;
                        motor.GruplariPatlat(
                            gruplar,
                            grup => DogumHucresi.Kaskad(baglam.Tahta, grup, baglam.Bayraklar, esitlik),
                            kaskad: true);
                        continue;
                    }
                }

                // AK-14 B: çift renk küresinin ikinci rengi, düşme ve kaskad durulduktan sonra seçilir.
                if (Baslat(motor, EtkiMotoru.KureBeklemesi.KaskadSonu))
                {
                    continue;
                }

                break;
            }
        }

        private static bool Baslat(EtkiMotoru motor, EtkiMotoru.KureBeklemesi bekleme)
        {
            if (!motor.BekleyenVar(bekleme))
            {
                return false;
            }

            motor.BekleyenleriBaslat(bekleme);
            motor.DalgayiBitir();
            return true;
        }
    }
}
