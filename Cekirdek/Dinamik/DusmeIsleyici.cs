using System.Collections.Generic;
using CatRoyal.Cekirdek.Model;
using CatRoyal.Cekirdek.Olaylar;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Dinamik
{
    /// <summary>
    /// Düşme (GDD 5.1), çapraz kayma (5.2) ve giriş hücrelerinden doldurma (5.3).
    ///
    /// Her güncelleme 5.1.1'deki sırayla hesaplanır: düz düşmeler, sonra çapraz kaymalar. Taşlar hedeflerine
    /// mantıkta hemen yerleşir; ne zaman oturdukları oturma adımıyla kaydedilir (5.1.7): oturma adımı, düşmeye
    /// başladığı adım ile düştüğü hücre sayısının toplamıdır. Görsel hız sonucu değiştirmez.
    /// </summary>
    public static class DusmeIsleyici
    {
        /// <summary>Hiçbir taş hareket etmeyene kadar güncellemeleri tekrarlar.</summary>
        /// <returns>En az bir taş hareket ettiyse true.</returns>
        public static bool Durult(OyunBaglami baglam, List<Olay> olaylar)
        {
            bool hareketOldu = false;
            while (Guncelle(baglam, olaylar))
            {
                hareketOldu = true;
            }

            return hareketOldu;
        }

        /// <summary>Tek güncelleme: düz düşmeler, sonra çapraz kaymalar (GDD 5.1.1).</summary>
        /// <returns>En az bir taş hareket ettiyse true.</returns>
        public static bool Guncelle(OyunBaglami baglam, List<Olay> olaylar)
        {
            var tahta = baglam.Tahta;
            long baslangic = baglam.Saat;
            long enGec = baslangic;
            bool hareketOldu = false;
            var hareketEden = new HashSet<Tas>();

            // GDD 5.1.2: tahta sütun sütun ve her sütunda aşağıdan yukarıya taranır.
            for (int sutun = 1; sutun <= tahta.SutunSayisi; sutun++)
            {
                var giristenCikan = new Dictionary<Konum, int>();
                for (int satir = tahta.SatirSayisi; satir >= 1; satir--)
                {
                    var bos = new Konum(satir, sutun);
                    if (!tahta[bos].Oynanabilir || tahta[bos].Tas != null)
                    {
                        continue;
                    }

                    var kaynak = DuzKaynakBul(baglam, bos, out Konum? giris);
                    if (kaynak.HasValue)
                    {
                        // GDD 5.1.3: tam üstteki ilk taş iner; sütundaki sıra korunur.
                        var tas = tahta[kaynak.Value].Tas!;
                        tahta[kaynak.Value].Tas = null;
                        tahta[bos].Tas = tas;
                        tas.OturmaAdimi = baslangic + (bos.Satir - kaynak.Value.Satir);
                        hareketEden.Add(tas);
                        olaylar.Add(new TasDustu(tas, kaynak.Value, bos, baslangic, tas.OturmaAdimi));
                        enGec = System.Math.Max(enGec, tas.OturmaAdimi);
                        hareketOldu = true;
                    }
                    else if (giris.HasValue)
                    {
                        // GDD 5.3.1: sütunda taş kalmadıysa giriş hücresi kullanılır. Girişten her adımda bir taş çıkar.
                        giristenCikan.TryGetValue(giris.Value, out int sira);
                        sira++;
                        giristenCikan[giris.Value] = sira;

                        var tas = baglam.Dolgu.Uret(baglam, giris.Value, bos);
                        tahta[bos].Tas = tas;
                        long girisAdimi = baslangic + sira;
                        tas.OturmaAdimi = girisAdimi + (bos.Satir - giris.Value.Satir);
                        hareketEden.Add(tas);
                        olaylar.Add(new TasGirdi(tas, giris.Value, bos, girisAdimi, tas.OturmaAdimi));
                        enGec = System.Math.Max(enGec, tas.OturmaAdimi);
                        hareketOldu = true;
                    }
                }
            }

            if (baglam.CaprazKaymaIzinli && CaprazKaymalar(baglam, olaylar, hareketEden, baslangic))
            {
                enGec = System.Math.Max(enGec, baslangic + 1);
                hareketOldu = true;
            }

            baglam.Saat = enGec;
            return hareketOldu;
        }

        /// <summary>
        /// GDD 5.1.2: boş hücrenin tam üstünde düşebilecek ilk taş aranır. Düşmeyi engelleyen bir hücreye
        /// (oynanmayan hücre, düşmeyen engel, kilitli taş) gelince arama durur. Taş bulunamazsa ve yol üzerinde
        /// giriş hücresi varsa (boş hücrenin kendisi dahil) <paramref name="giris"/> o hücredir.
        /// </summary>
        private static Konum? DuzKaynakBul(OyunBaglami baglam, Konum bos, out Konum? giris)
        {
            var tahta = baglam.Tahta;
            giris = GirisMi(baglam, bos) ? bos : (Konum?)null;

            for (var konum = bos.Yukari; tahta.Icinde(konum); konum = konum.Yukari)
            {
                var hucre = tahta[konum];
                if (!hucre.Oynanabilir)
                {
                    return null;
                }

                if (hucre.Tas != null)
                {
                    if (tahta.Duser(konum))
                    {
                        giris = null;
                        return konum;
                    }

                    return null;
                }

                if (!giris.HasValue && GirisMi(baglam, konum))
                {
                    giris = konum;
                }
            }

            return null;
        }

        /// <summary>
        /// Giriş hücresi mi (GDD 5.3.1)? AK-04 B'de her sütunun en üstteki oynanabilir hücresi, işaretli olmasa da
        /// örtük giriş hücresidir.
        /// </summary>
        private static bool GirisMi(OyunBaglami baglam, Konum konum)
        {
            var tahta = baglam.Tahta;
            if (tahta[konum].Giris)
            {
                return true;
            }

            if (baglam.Bayraklar[Ak.GirissizSutunDolumu] != Varyant.B)
            {
                return false;
            }

            for (var ust = konum.Yukari; tahta.Icinde(ust); ust = ust.Yukari)
            {
                if (tahta[ust].Oynanabilir)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// GDD 5.2: düz düşme hesabından sonra hâlâ taş gelmeyecek boş hücreler için önce sol-yukarı, o uygun
        /// değilse sağ-yukarı çapraz komşudan taş çekilir. Çapraz kayma bir adım sayılır (5.1.7).
        ///
        /// AK-03: bu modelde bir güncellemede düz düşmeyle taş gelmeyen boşluğun üstü her zaman kalıcı olarak
        /// kapalıdır (oynanmayan hücre, düşmeyen engel, kilitli taş ya da girişi olmayan sütun tepesi); donmuş
        /// hücre (5.1.6) oluşmadığı için A ve B aynı hücreleri aday yapar. Fark, düşme sürerken hamle kabul
        /// edildiğinde (2.3.9) ortaya çıkar.
        /// </summary>
        private static bool CaprazKaymalar(OyunBaglami baglam, List<Olay> olaylar, HashSet<Tas> hareketEden, long baslangic)
        {
            var tahta = baglam.Tahta;
            bool hareketOldu = false;

            // Adaylar, düz düşme hesabından sonra boş kalan hücrelerdir (GDD 5.2.1). Liste kaymalardan önce
            // sabitlenir: bu güncellemede çapraz kayan bir taşın boşalttığı hücre ancak sonraki güncellemede dolar.
            var adaylar = new List<Konum>();
            for (int sutun = 1; sutun <= tahta.SutunSayisi; sutun++)
            {
                for (int satir = tahta.SatirSayisi; satir >= 1; satir--)
                {
                    var bos = new Konum(satir, sutun);
                    var hucre = tahta[bos];
                    if (hucre.Oynanabilir && hucre.Tas == null && !hucre.CaprazKaymaKapali)
                    {
                        adaylar.Add(bos);
                    }
                }
            }

            foreach (var bos in adaylar)
            {
                // GDD 5.2.2: önce sol-yukarı, sonra sağ-yukarı.
                foreach (var kaynak in new[] { bos.Yukari.Sol, bos.Yukari.Sag })
                {
                    if (!CaprazKayabilir(tahta, kaynak) || hareketEden.Contains(tahta[kaynak].Tas!))
                    {
                        continue;
                    }

                    var tas = tahta[kaynak].Tas!;
                    tahta[kaynak].Tas = null;
                    tahta[bos].Tas = tas;
                    tas.OturmaAdimi = baslangic + 1;
                    hareketEden.Add(tas);
                    olaylar.Add(new TasCaprazKaydi(tas, kaynak, bos, tas.OturmaAdimi));
                    hareketOldu = true;
                    break;
                }
            }

            return hareketOldu;
        }

        /// <summary>GDD 5.2.5, 6.2.3: engel arketiplerinin hiçbiri çapraz kaymaz; düşmesi engellenen taş da kaymaz.</summary>
        private static bool CaprazKayabilir(Tahta tahta, Konum konum)
        {
            if (!tahta.Oynanabilir(konum))
            {
                return false;
            }

            var hucre = tahta[konum];
            return hucre.Tas != null && !hucre.DusmeEngelli && hucre.Tas.Tur != TasTuru.Engel;
        }
    }
}
