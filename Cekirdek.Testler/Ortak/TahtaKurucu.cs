using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Model;

namespace CatRoyal.Cekirdek.Testler.Ortak
{
    /// <summary>test_vakalari.md'deki sembollerle kurulmuş tahta ve izlenen taşlar.</summary>
    public sealed class TestTahtasi
    {
        private readonly Dictionary<char, Tas> _izlenen;

        internal TestTahtasi(Tahta tahta, Dictionary<char, Tas> izlenen)
        {
            Tahta = tahta;
            _izlenen = izlenen;
        }

        public Tahta Tahta { get; }

        /// <summary>a–f harfleriyle işaretlenmiş izlenen nötr taş.</summary>
        public Tas Izlenen(char harf) => _izlenen[harf];

        /// <summary>İzlenen taşın şu anki konumu; tahtada değilse null.</summary>
        public Konum? IzlenenKonumu(char harf)
        {
            var tas = _izlenen[harf];
            foreach (var konum in Tahta.TumKonumlar())
            {
                if (ReferenceEquals(Tahta[konum].Tas, tas))
                {
                    return konum;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// test_vakalari.md "Ortak semboller" bölümündeki gösterimden tahta kurar.
    /// Yayılan zemin (Z) ayrı katman haritasında verildiği için testte hücreye doğrudan konur.
    /// </summary>
    public static class TahtaKurucu
    {
        public const int Kirmizi = 0;
        public const int Mavi = 1;
        public const int Yesil = 2;
        public const int Sari = 3;

        /// <summary>
        /// Nötr taşların ilk renk kimliği. Her nötr taş ayrı bir renk alır; böylece hiçbir eşleşmeye
        /// katılmaz ve vakadaki renklerle 3'lü ya da kare oluşturmaz.
        /// </summary>
        public const int NotrRenkTabani = 1000;

        /// <summary>Katmanlı blokların hedef türü; yalnızca vaka hedef verdiğinde sayılır.</summary>
        public const string KatmanliBlokHedefi = "katmanli_blok";

        public static readonly EngelTanimi KatmanliBlok = HedefliKatmanliBlok();
        public static readonly EngelTanimi SertBlok = Arketipler.SertBlok();
        public static readonly KatmanTanimi YapiskanKaplama = KatmanArketipleri.YapiskanKaplama();
        public static readonly KatmanTanimi Kilit = KatmanArketipleri.Kilit();
        public static readonly KatmanTanimi YayilanZemin = KatmanArketipleri.YayilanZemin();

        private static EngelTanimi HedefliKatmanliBlok()
        {
            var tanim = Arketipler.KatmanliBlok();
            tanim.HedefTuru = KatmanliBlokHedefi;
            return tanim;
        }

        public static TestTahtasi Kur(string cizim)
        {
            var satirlar = cizim
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(s => s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            if (satirlar.Count < 2)
            {
                throw new ArgumentException("Tahta çizimi başlık ve en az bir satır içermeli.");
            }

            int sutunSayisi = satirlar[0].Length;
            int satirSayisi = satirlar.Count - 1;
            var tahta = new Tahta(satirSayisi, sutunSayisi);
            var izlenen = new Dictionary<char, Tas>();
            int notrRenk = NotrRenkTabani;

            for (int s = 1; s <= satirSayisi; s++)
            {
                var parcalar = satirlar[s];
                if (parcalar.Length != sutunSayisi + 1 || parcalar[0] != s.ToString())
                {
                    throw new ArgumentException($"Tahta çizimi satır {s}: beklenen biçim '{s}  <{sutunSayisi} sembol>'.");
                }

                for (int t = 1; t <= sutunSayisi; t++)
                {
                    string sembol = parcalar[t];
                    if (sembol.Length != 1)
                    {
                        throw new ArgumentException($"Tahta çizimi ({s},{t}): tek karakterli sembol bekleniyordu.");
                    }

                    var hucre = tahta[s, t];
                    char c = sembol[0];
                    switch (c)
                    {
                        case 'K': hucre.Tas = tahta.NormalTasUret(Kirmizi); break;
                        case 'M': hucre.Tas = tahta.NormalTasUret(Mavi); break;
                        case 'Y': hucre.Tas = tahta.NormalTasUret(Yesil); break;
                        case 'S': hucre.Tas = tahta.NormalTasUret(Sari); break;
                        case '·': hucre.Tas = tahta.NormalTasUret(notrRenk++); break;
                        case '-': hucre.Tas = tahta.OzelTasUret(TasTuru.Cizgi, Yon.Yatay); break;
                        case '|': hucre.Tas = tahta.OzelTasUret(TasTuru.Cizgi, Yon.Dikey); break;
                        case 'B': hucre.Tas = tahta.OzelTasUret(TasTuru.Bomba); break;
                        case 'G': hucre.Tas = tahta.OzelTasUret(TasTuru.Gudumlu); break;
                        case 'R': hucre.Tas = tahta.OzelTasUret(TasTuru.RenkKuresi); break;
                        case '_': break;
                        case 'X': hucre.Oynanabilir = false; break;
                        case '~':
                            // Yapışkan kaplamalı taş; altındaki rengin önemi yok.
                            hucre.Tas = tahta.NormalTasUret(notrRenk++);
                            hucre.UstKatman = new HucreKatmani(YapiskanKaplama, 1);
                            break;
                        case '#':
                            // Kilitli taş.
                            hucre.Tas = tahta.NormalTasUret(notrRenk++);
                            hucre.UstKatman = new HucreKatmani(Kilit, 1);
                            break;
                        case 'W':
                            // Sert blok, 3 katman (test_vakalari.md "Ortak semboller").
                            hucre.Tas = tahta.EngelUret(SertBlok, 3);
                            break;
                        default:
                            if (c >= '1' && c <= '4')
                            {
                                // O sayıda katmanı olan katmanlı blok.
                                hucre.Tas = tahta.EngelUret(KatmanliBlok, c - '0');
                                break;
                            }

                            if (c >= 'a' && c <= 'f')
                            {
                                hucre.Tas = tahta.NormalTasUret(notrRenk++);
                                izlenen.Add(c, hucre.Tas);
                                break;
                            }

                            throw new ArgumentException($"Tahta çizimi ({s},{t}): '{c}' sembolü henüz desteklenmiyor.");
                    }
                }
            }

            return new TestTahtasi(tahta, izlenen);
        }

        /// <summary>
        /// Bütün hücreleri nötr olan tahta çizimi; verilen hücrelere istenen semboller konur
        /// (ör. "7×7 nötr; (4,4)'te B").
        /// </summary>
        public static string NotrCizim(int satirSayisi, int sutunSayisi, params (int Satir, int Sutun, char Sembol)[] ozel)
        {
            var metin = new System.Text.StringBuilder();
            metin.Append("   ");
            for (int t = 1; t <= sutunSayisi; t++) metin.Append(' ').Append(t);
            metin.Append('\n');
            for (int s = 1; s <= satirSayisi; s++)
            {
                metin.Append(s).Append("  ");
                for (int t = 1; t <= sutunSayisi; t++)
                {
                    char sembol = '·';
                    foreach (var o in ozel)
                    {
                        if (o.Satir == s && o.Sutun == t) sembol = o.Sembol;
                    }

                    metin.Append(' ').Append(sembol);
                }

                metin.Append('\n');
            }

            return metin.ToString();
        }

        /// <summary>Kısa konum yazımı.</summary>
        public static Konum H(int satir, int sutun) => new Konum(satir, sutun);
    }
}
