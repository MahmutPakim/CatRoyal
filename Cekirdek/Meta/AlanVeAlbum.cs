using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Alan sandığı açılır ve bir sonraki alan açılır (GDD 9.6.4). Sandık alan numarasından bağımsız sabit bir
        /// pakettir (9.6.5); burada yalnızca parası verilir, takviye içeriği sunucu ya da tasarım tablosundan gelir.
        /// </summary>
        public OdulPaketi? SandigiAc()
        {
            if (Durum != AlanDurumu.SandikHazir)
            {
                return null;
            }

            Durum = AlanDurumu.Tamamlandi;
            AlanNumarasi++;
            return new OdulPaketi { Para = _p.TamSayi(MetaParametreAdlari.AlanSandigiPara) };
        }
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
        {
            _setBasinaKart = parametreler.TamSayi(MetaParametreAdlari.SetBasinaKart);
            _setSayisi = parametreler.TamSayi(MetaParametreAdlari.SetSayisi);
        }

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

        /// <summary>Set doldu mu (GDD 9.7.7)?</summary>
        public bool SetDolu(int set) => _setler.TryGetValue(set, out var kartlar) && kartlar.Count == _setBasinaKart;

        /// <summary>Bütün setler dolunca büyük ödül alınır (GDD 9.7.7).</summary>
        public bool HepsiDolu => Enumerable.Range(0, _setSayisi).All(SetDolu);
    }
}
