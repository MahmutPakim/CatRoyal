using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>Alan inşası görevi (GDD 9.6.2): yıldız fiyatı ve önkoşul görevleri; görev ağacı doğrusal değildir.</summary>
    public sealed class AlanGorevi
    {
        public AlanGorevi(string kimlik, int yildizFiyati, params string[] onkosullar)
        {
            Kimlik = kimlik;
            YildizFiyati = yildizFiyati;
            Onkosullar = onkosullar;
        }

        public string Kimlik { get; }
        public int YildizFiyati { get; }
        public IReadOnlyList<string> Onkosullar { get; }
    }

    public enum AlanDurumu
    {
        Insaatta,
        SandikHazir,
        Tamamlandi,

        /// <summary>Sandık açıldı ama yayımlanmış yeni alan yok; son alan bekliyor (GDD 9.6.7).</summary>
        Bekliyor,
    }

    /// <summary>
    /// Alan inşası (GDD 9.6): yıldızlarla görev tamamlanan dekorasyon ilerlemesi. Görev listeleri, sayıları ve
    /// sandık içeriği kendi tasarımındır; burada yalnızca yapı vardır.
    /// </summary>
    public sealed class AlanInsasi
    {
        private readonly ParametreTablosu _p;
        private readonly HashSet<string> _insaEdilen = new HashSet<string>();
        private List<AlanGorevi> _gorevler = new List<AlanGorevi>();

        public AlanInsasi(ParametreTablosu parametreler)
        {
            _p = parametreler;
        }

        public int AlanNumarasi { get; private set; } = 1;
        public AlanDurumu Durum { get; private set; } = AlanDurumu.Insaatta;
        public IReadOnlyList<AlanGorevi> Gorevler => _gorevler;

        /// <summary>Alanın görevlerini yükler; fiyat verilmeyen görevlerde parametre: gorev_yildiz_fiyati kullanılır.</summary>
        public void GorevleriYukle(IEnumerable<AlanGorevi> gorevler)
        {
            _gorevler = gorevler.ToList();
            _insaEdilen.Clear();
            Durum = _gorevler.Count == 0 ? AlanDurumu.SandikHazir : AlanDurumu.Insaatta;
        }

        public int VarsayilanGorevFiyati => _p.TamSayi(MetaParametreAdlari.GorevYildizFiyati);

        public bool InsaEdildi(string kimlik) => _insaEdilen.Contains(kimlik);

        public bool InsaEdilebilir(AlanGorevi gorev, int yildiz) =>
            Durum == AlanDurumu.Insaatta
            && !_insaEdilen.Contains(gorev.Kimlik)
            && gorev.Onkosullar.All(_insaEdilen.Contains)
            && yildiz >= gorev.YildizFiyati;

        /// <summary>Yeterli yıldız varsa görev inşa edilir (GDD 9.6.2). Son görevde sandık açılmaya hazır olur (9.6.4).</summary>
        /// <returns>Harcanan yıldız; inşa edilemediyse 0.</returns>
        public int InsaEt(string kimlik, int yildiz)
        {
            var gorev = _gorevler.FirstOrDefault(g => g.Kimlik == kimlik);
            if (gorev == null || !InsaEdilebilir(gorev, yildiz))
            {
                return 0;
            }

            _insaEdilen.Add(kimlik);
            if (_insaEdilen.Count == _gorevler.Count)
            {
                Durum = AlanDurumu.SandikHazir;
            }

            return gorev.YildizFiyati;
        }

        /// <summary>Yeterli yıldız varsa kalan bütün görevler tek seferde inşa edilebilir (GDD 9.6.3).</summary>
        /// <returns>Harcanan toplam yıldız.</returns>
        public int HepsiniInsaEt(int yildiz)
        {
            int harcanan = 0;
            bool ilerledi = true;
            while (ilerledi)
            {
                ilerledi = false;
                foreach (var gorev in _gorevler)
                {
                    int bedel = InsaEt(gorev.Kimlik, yildiz - harcanan);
                    if (bedel > 0)
                    {
                        harcanan += bedel;
                        ilerledi = true;
                    }
                }
            }

            return harcanan;
        }

        /// <summary>Alanın görevleri de kaydedilir: yüklenen kayıt, görev listesi yeniden verilmeden kaldığı yerden sürer.</summary>
        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["alan_numarasi"] = AlanNumarasi,
            ["durum"] = Durum.ToString(),
            ["gorevler"] = _gorevler.Select(g => (object?)new Dictionary<string, object?>
            {
                ["kimlik"] = g.Kimlik,
                ["yildiz_fiyati"] = g.YildizFiyati,
                ["onkosullar"] = g.Onkosullar.Cast<object?>().ToList(),
            }).ToList(),
            ["insa_edilen"] = _insaEdilen.OrderBy(k => k, StringComparer.Ordinal).Cast<object?>().ToList(),
        };

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            AlanNumarasi = Math.Max(1, KayitAlani.TamSayi(d, "alan_numarasi", 1));
            _gorevler = new List<AlanGorevi>();
            foreach (object? oge in KayitAlani.Dizi(d, "gorevler"))
            {
                if (!(oge is Dictionary<string, object?> g) || !(g.TryGetValue("kimlik", out object? kimlik) && kimlik is string ad))
                {
                    throw new KayitHatasi("Kayıt: alan görevi okunamadı.");
                }

                _gorevler.Add(new AlanGorevi(
                    ad,
                    KayitAlani.TamSayi(g, "yildiz_fiyati", VarsayilanGorevFiyati),
                    KayitAlani.Dizi(g, "onkosullar").OfType<string>().ToArray()));
            }

            _insaEdilen.Clear();
            foreach (string kimlik in KayitAlani.Dizi(d, "insa_edilen").OfType<string>())
            {
                if (_gorevler.Any(g => g.Kimlik == kimlik)) _insaEdilen.Add(kimlik);
            }

            Durum = d.TryGetValue("durum", out object? durum)
                && Enum.TryParse(durum as string, out AlanDurumu okunan)
                && Enum.IsDefined(typeof(AlanDurumu), okunan)
                    ? okunan
                    : AlanDurumu.Insaatta;
        }

        /// <summary>
        /// Alan sandığı açılır ve bir sonraki alan açılır (GDD 9.6.4). Sandık alan numarasından bağımsız sabit bir
        /// pakettir (9.6.5); burada yalnızca parası verilir, takviye içeriği sunucu ya da tasarım tablosundan gelir.
        /// </summary>
        /// <param name="oynanabilirAlanSayisi">
        /// Yayımlanmış bölümlerin karşıladığı alan sayısı (<see cref="OynanabilirAlanSayisi"/>). Verilirse ve bu alan
        /// sonuncuysa bir sonraki alan açılmaz; alan "bekliyor" durumunda kalır (GDD 9.6.7).
        /// </param>
        public OdulPaketi? SandigiAc(int? oynanabilirAlanSayisi = null)
        {
            if (Durum != AlanDurumu.SandikHazir)
            {
                return null;
            }

            if (oynanabilirAlanSayisi.HasValue && AlanNumarasi >= oynanabilirAlanSayisi.Value)
            {
                Durum = AlanDurumu.Bekliyor;
            }
            else
            {
                Durum = AlanDurumu.Tamamlandi;
                AlanNumarasi++;
            }

            return new OdulPaketi { Para = _p.TamSayi(MetaParametreAdlari.AlanSandigiPara) };
        }

        /// <summary>
        /// Oynanabilir alan sayısı, yayımlanmış en yüksek bölümden türetilir (GDD 9.6.7; parametre: alan_basina_bolum).
        /// Yorum: ilk bölümü yayımlanmış olan alan oynanabilir sayılır; en az bir alan vardır.
        /// </summary>
        public static int OynanabilirAlanSayisi(ParametreTablosu p, int yayimlananEnYuksekBolum)
        {
            int alanBasina = Math.Max(1, p.TamSayi(MetaParametreAdlari.AlanBasinaBolum));
            return Math.Max(1, (yayimlananEnYuksekBolum + alanBasina - 1) / alanBasina);
        }

        /// <summary>Yeni bölümler yayımlandı: bekleyen alan varsa bir sonraki alan açılır (GDD 9.6.7).</summary>
        /// <returns>Yeni alan açıldıysa true; görevleri <see cref="GorevleriYukle"/> ile verilir.</returns>
        public bool YeniAlanGeldi(int oynanabilirAlanSayisi)
        {
            if (Durum != AlanDurumu.Bekliyor || AlanNumarasi >= oynanabilirAlanSayisi)
            {
                return false;
            }

            AlanNumarasi++;
            Durum = AlanDurumu.Tamamlandi;
            return true;
        }

        /// <summary>Eldeki yıldızla inşa edilebilecek bir görev var mı?</summary>
        public bool YapilabilirGorevVar(int yildiz) => _gorevler.Any(g => InsaEdilebilir(g, yildiz));

        /// <summary>
        /// Yapılabilir görev bildirimi zorla gösterilir mi (GDD 9.6.6)? Yalnızca parametre: gorev_bildirimi_son_bolum'e
        /// kadar ve inşa edilebilecek bir görev varken.
        /// </summary>
        public bool GorevBildirimiGosterilir(int yildiz, int enYuksekBolum) =>
            enYuksekBolum <= _p.TamSayi(MetaParametreAdlari.GorevBildirimiSonBolum) && YapilabilirGorevVar(yildiz);
    }

    /// <summary>
    /// Sezonluk albüm (GDD 9.7), yalnızca yapı: kartlar setlere eklenir, set dolunca set ödülü alınabilir,
    /// çift kartlar sayılır. Paket içeriği, acıma kuralı, takas ve sezon süresi sunucu tarafıdır.
    /// </summary>
    public sealed class Album
    {
        private readonly Dictionary<int, HashSet<int>> _setler = new Dictionary<int, HashSet<int>>();
        private readonly int _setBasinaKart;
        private readonly int _setSayisi;

        public Album(ParametreTablosu parametreler)
            : this(parametreler.TamSayi(MetaParametreAdlari.SetSayisi), parametreler.TamSayi(MetaParametreAdlari.SetBasinaKart))
        {
        }

        /// <summary>Sezonun set sayısı yapılandırmadan gelir (GDD 9.7.3).</summary>
        public Album(int setSayisi, int setBasinaKart)
        {
            _setSayisi = setSayisi;
            _setBasinaKart = setBasinaKart;
        }

        public int SetSayisi => _setSayisi;
        public int SetBasinaKart => _setBasinaKart;

        /// <summary>Kart albümde var mı?</summary>
        public bool Var(int set, int kart) => _setler.TryGetValue(set, out var kartlar) && kartlar.Contains(kart);

        /// <summary>Albümdeki kart sayısı.</summary>
        public int KartSayisi => _setler.Values.Sum(k => k.Count);

        public int CiftKartSayisi { get; private set; }

        /// <summary>Kartı setine ekler; yeni kartsa true, çiftse false (çift kartlar yıldıza dönüşür, GDD 9.7.8).</summary>
        public bool KartEkle(int set, int kart)
        {
            if (set < 0 || set >= _setSayisi || kart < 0 || kart >= _setBasinaKart)
            {
                throw new ArgumentOutOfRangeException(nameof(kart), "Set ya da kart numarası albümün dışında.");
            }

            if (!_setler.TryGetValue(set, out var kartlar))
            {
                kartlar = new HashSet<int>();
                _setler.Add(set, kartlar);
            }

            if (kartlar.Add(kart))
            {
                return true;
            }

            CiftKartSayisi++;
            return false;
        }

        internal Dictionary<string, object?> DurumuYaz()
        {
            var setler = new Dictionary<string, object?>();
            foreach (var cift in _setler.OrderBy(c => c.Key))
            {
                setler[cift.Key.ToString(CultureInfo.InvariantCulture)] = cift.Value.OrderBy(k => k).Cast<object?>().ToList();
            }

            return new Dictionary<string, object?> { ["cift_kart_sayisi"] = CiftKartSayisi, ["setler"] = setler };
        }

        /// <summary>Albüm kayıttan sonra küçüldüyse dışarıda kalan kartlar atlanır.</summary>
        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            CiftKartSayisi = Math.Max(0, KayitAlani.TamSayi(d, "cift_kart_sayisi", 0));
            _setler.Clear();
            foreach (var cift in KayitAlani.Nesne(d, "setler"))
            {
                if (!int.TryParse(cift.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int set)
                    || set >= _setSayisi
                    || !(cift.Value is List<object?> kartlar))
                {
                    continue;
                }

                var okunan = new HashSet<int>();
                foreach (object? kart in kartlar)
                {
                    int no = KayitAlani.TamSayi(kart, "setler");
                    if (no >= 0 && no < _setBasinaKart) okunan.Add(no);
                }

                if (okunan.Count > 0) _setler[set] = okunan;
            }
        }

        /// <summary>Set doldu mu (GDD 9.7.7)?</summary>
        public bool SetDolu(int set) => _setler.TryGetValue(set, out var kartlar) && kartlar.Count == _setBasinaKart;

        /// <summary>Bütün setler dolunca büyük ödül alınır (GDD 9.7.7).</summary>
        public bool HepsiDolu => Enumerable.Range(0, _setSayisi).All(SetDolu);
    }
}
