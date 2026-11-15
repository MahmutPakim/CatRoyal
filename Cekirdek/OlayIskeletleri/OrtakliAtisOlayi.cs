using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Ortaklı atış olayı (GDD 10.6): oyuncu diğer olaylardan ve bölüm kazanımlarından bilye toplar, bilyeleri atış
    /// tahtasında kullanıp puan kazanır; puan ortakların puanıyla birlikte sayılır ve olayın maskotunu büyütür.
    ///
    /// Durumlar: hazır → (olaya girildi) ortak aranıyor → (en az bir ortak) aktif → (süre doldu) son liste
    /// bekleniyor → (liste geldi ya da denemeler bitti) bitti. "Kilitli" durumu olayın kapısından gelir.
    ///
    /// Atış tahtası fizik tabanlıdır ve sunumdadır: bir atışın getirdiği puan istemcide hesaplanır (10.6.3) ve buraya
    /// sonuç olarak verilir. Ortak eşleştirme ve puan doğrulama sunucudadır (10.6.6).
    ///
    /// Yorumlar: ödül sırası "basamak ödülleri, ardından büyük ödül" biçimindedir; büyük ödül maskot son evresine
    /// ulaştıysa verilir.
    /// </summary>
    public sealed class OrtakliAtisOlayi : OlayIskeleti
    {
        /// <summary>Ödül paketlerinde bilyeyi taşıyan olay eşyasının kimliği (GDD 10.6.2, 10.9.5).</summary>
        public const string BilyeEsyasi = "bilye";

        /// <summary>Süre dolduktan sonra son listenin beklenebileceği en uzun süre.</summary>
        public static readonly TimeSpan SonListeBeklemesi = TimeSpan.FromHours(1);

        private readonly ParametreTablosu _p;
        private readonly HashSet<int> _alinanBasamaklar = new HashSet<int>();
        private IReadOnlyList<OdulPaketi?> _oduller = Array.Empty<OdulPaketi?>();
        private List<GrupUyesi> _ortaklar = new List<GrupUyesi>();
        private bool _girildi;
        private bool _bitti;
        private int _gonderilecek;
        private int _deneme;
        private DateTime _sonrakiDeneme;

        public OrtakliAtisOlayi(string kimlik, DateTime bitis, ParametreTablosu p)
            : base(kimlik, MetaParametreAdlari.AcilisOrtakliAtis, bitis)
        {
            _p = p;
        }

        public override OlayTuru Tur => OlayTuru.OrtakliAtis;

        /// <summary>Girilmiş olay, süre dolduktan sonra son liste gelene kadar kapanmaz.</summary>
        protected override DateTime EtkinBitis => _girildi && !_bitti ? Bitis + SonListeBeklemesi : Bitis;

        public int Bilye { get; private set; }

        /// <summary>Oyuncunun kendi atış puanı.</summary>
        public int Puan { get; private set; }

        public IReadOnlyList<GrupUyesi> Ortaklar => _ortaklar;

        /// <summary>Puan ortakların puanıyla birlikte sayılır (GDD 10.6.3).</summary>
        public int ToplamPuan => Puan + _ortaklar.Sum(o => o.Puan);

        /// <summary>Maskotun evre atladığı puan basamakları (GDD 10.6.4).</summary>
        public IReadOnlyList<int> Basamaklar => _p.TamSayiListesi(MetaParametreAdlari.OrtakliAtisPuanBasamaklari);

        /// <summary>Maskotun evresi: geçilen basamak sayısı.</summary>
        public int MaskotEvresi => Basamaklar.Count(b => ToplamPuan >= b);

        public OrtakliAtisDurumu Durum(DateTime simdi) =>
            _bitti ? OrtakliAtisDurumu.Bitti
            : !_girildi ? OrtakliAtisDurumu.Hazir
            : simdi >= Bitis ? OrtakliAtisDurumu.SonListeBekleniyor
            : _ortaklar.Count == 0 ? OrtakliAtisDurumu.OrtakAraniyor
            : OrtakliAtisDurumu.Aktif;

        /// <summary>Ödüller: basamak sırasıyla basamak ödülleri, ardından büyük ödül (GDD 10.6.4, 10.6.5).</summary>
        public override void OdulleriAyarla(IReadOnlyList<OdulPaketi?> oduller) => _oduller = oduller;

        private OdulPaketi? Odul(int sira) => sira >= 0 && sira < _oduller.Count ? _oduller[sira] : null;

        /// <summary>Olaya girer: ortak aranmaya başlanır.</summary>
        public bool Gir(DateTime simdi)
        {
            if (_girildi || simdi >= Bitis)
            {
                return false;
            }

            _girildi = true;
            return true;
        }

        /// <summary>
        /// Takımdan ya da arkadaşlardan ortak davet eder (GDD 10.6.1; parametre: ortakli_atis_azami_ortak). En az bir
        /// ortakla olay aktif olur.
        /// </summary>
        public bool OrtakDavetEt(ISosyalSunucu sunucu, Oyuncu oyuncu, string ortak, DateTime simdi)
        {
            if (!_girildi || simdi >= Bitis || _ortaklar.Count >= _p.TamSayi(MetaParametreAdlari.OrtakliAtisAzamiOrtak))
            {
                return false;
            }

            var yanit = sunucu.OrtakDavetEt(Kimlik, oyuncu.Kimlik, ortak);
            if (yanit.Basarili)
            {
                _ortaklar = yanit.Deger.Ortaklar.ToList();
            }

            return yanit.Basarili;
        }

        /// <summary>Bölüm kazanımı 10.9.2'deki puan kadar bilye verir (GDD 10.6.2).</summary>
        public override void BolumKazanildi(BolumSonucu sonuc, DateTime simdi)
        {
            if (_girildi && simdi < Bitis)
            {
                Bilye += sonuc.OlayPuani;
            }
        }

        /// <summary>
        /// Bir bilye atılır (GDD 10.6.3): bilye harcanır ve atışın istemcide hesaplanan puanı eklenir.
        /// </summary>
        public bool AtisYap(int atisPuani, DateTime simdi)
        {
            if (Durum(simdi) != OrtakliAtisDurumu.Aktif || Bilye <= 0 || atisPuani < 0)
            {
                return false;
            }

            Bilye--;
            Puan += atisPuani;
            _gonderilecek += atisPuani;
            return true;
        }

        public bool BasamakOduluAlinabilir(int basamak) => basamak >= 0 && basamak < MaskotEvresi && !_alinanBasamaklar.Contains(basamak);

        /// <summary>Puan basamağında maskot bir sonraki evresine geçer ve basamak ödülü alınır (GDD 10.6.4).</summary>
        public bool BasamakOdulunuAl(int basamak, Oyuncu oyuncu, DateTime simdi)
        {
            if (!BasamakOduluAlinabilir(basamak))
            {
                return false;
            }

            _alinanBasamaklar.Add(basamak);
            if (Odul(basamak) is { } odul)
            {
                oyuncu.OdulVer(odul, simdi);
            }

            return true;
        }

        /// <summary>
        /// Olayı ilerletir: ödüllerden gelen bilyeler olaya aktarılır (GDD 10.6.2), atış puanı sunucuya gönderilir ve
        /// ortakların puanı yenilenir (10.6.3). Süre dolunca son liste birkaç kez, aralıklarla istenir; gelince ya da
        /// denemeler bitince büyük ödül ve bilye dönüşümü verilir (10.6.5).
        /// </summary>
        public void Ilerle(ISosyalSunucu? sunucu, Oyuncu oyuncu, DateTime simdi)
        {
            if (!_girildi || _bitti)
            {
                return;
            }

            if (simdi < Bitis)
            {
                if (oyuncu.OlayEsyalari.TryGetValue(BilyeEsyasi, out int bekleyen) && bekleyen > 0)
                {
                    Bilye += bekleyen;
                    oyuncu.OlayEsyalari[BilyeEsyasi] = 0;
                }

                if (_gonderilecek > 0)
                {
                    PuaniGonder(sunucu, oyuncu.Kimlik);
                }

                return;
            }

            if (simdi < _sonrakiDeneme)
            {
                return;
            }

            _deneme++;
            if (PuaniGonder(sunucu, oyuncu.Kimlik) || _deneme >= _p.TamSayi(MetaParametreAdlari.SonListeDenemeSayisi))
            {
                foreach (var odul in BitisOdulleri())
                {
                    oyuncu.OdulVer(odul, simdi);
                }
            }
            else
            {
                _sonrakiDeneme = simdi + TimeSpan.FromSeconds(_p.Sayi(MetaParametreAdlari.SonListeDenemeAraligiSn));
            }
        }

        /// <summary>Bekleyen puanı gönderir ve ortak listesini yeniler.</summary>
        private bool PuaniGonder(ISosyalSunucu? sunucu, string oyuncu)
        {
            var yanit = sunucu?.OrtakPuaniGonder(Kimlik, oyuncu, _gonderilecek);
            if (yanit == null || !yanit.Basarili)
            {
                return false;
            }

            _gonderilecek = 0;
            _ortaklar = yanit.Deger.Ortaklar.ToList();
            return true;
        }

        /// <summary>
        /// Kullanılmamış bilyelerin dönüşümü (GDD 10.6.5). parametre: ortakli_atis_bilye_donusumu serbest metindir:
        /// "Her 40 bilye için 1 renk küresi, en fazla 2".
        /// </summary>
        public int BilyeDonusumu(int bilye)
        {
            string metin = _p.Metin(MetaParametreAdlari.OrtakliAtisBilyeDonusumu);
            var sayilar = Regex.Matches(metin, @"\d+").Cast<Match>().Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
            if (sayilar.Count != 3 || sayilar[0] < 1)
            {
                throw new ParametreHatasi($"'{MetaParametreAdlari.OrtakliAtisBilyeDonusumu}' \"Her N bilye için A …, en fazla M\" biçiminde olmalı: '{metin}'.");
            }

            return Math.Min(sayilar[2], bilye / sayilar[0] * sayilar[1]);
        }

        /// <summary>Olay biter: alınmamış basamak ödülleri, büyük ödül ve bilye dönüşümü (GDD 10.6.5, 10.0.8).</summary>
        private List<OdulPaketi> BitisOdulleri()
        {
            var oduller = new List<OdulPaketi>();
            if (!_girildi || _bitti)
            {
                return oduller;
            }

            _bitti = true;
            int evre = MaskotEvresi;
            for (int i = 0; i < evre; i++)
            {
                if (_alinanBasamaklar.Add(i) && Odul(i) is { } odul)
                {
                    oduller.Add(odul);
                }
            }

            if (evre >= Basamaklar.Count && Odul(Basamaklar.Count) is { } buyuk)
            {
                oduller.Add(buyuk);
            }

            int kure = BilyeDonusumu(Bilye);
            if (kure > 0)
            {
                var donusum = new OdulPaketi();
                donusum.Takviyeler[TakviyeTuru.RenkKuresi] = kure;
                oduller.Add(donusum);
            }

            Bilye = 0;
            return oduller;
        }

        /// <summary>Son liste beklenirken süre tükenirse olay eldeki ortak puanlarıyla kapanır.</summary>
        public override List<OdulPaketi> AlinmamislariAl() => BitisOdulleri();

        internal override Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["girildi"] = _girildi,
            ["bitti"] = _bitti,
            ["bilye"] = Bilye,
            ["puan"] = Puan,
            ["gonderilecek"] = _gonderilecek,
            ["deneme"] = _deneme,
            ["sonraki_deneme"] = KayitAlani.Zaman(_sonrakiDeneme),
            ["alinan"] = _alinanBasamaklar.OrderBy(b => b).Cast<object?>().ToList(),
            ["ortaklar"] = _ortaklar.Select(o => (object?)new Dictionary<string, object?>
            {
                ["kimlik"] = o.Kimlik,
                ["puan"] = o.Puan,
                ["bot"] = o.Bot,
            }).ToList(),
            ["oduller"] = _oduller.Select(o => (object?)o?.DurumuYaz()).ToList(),
        };

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            _girildi = KayitAlani.Mantik(d, "girildi", false);
            _bitti = KayitAlani.Mantik(d, "bitti", false);
            Bilye = Math.Max(0, KayitAlani.TamSayi(d, "bilye", 0));
            Puan = Math.Max(0, KayitAlani.TamSayi(d, "puan", 0));
            _gonderilecek = Math.Max(0, KayitAlani.TamSayi(d, "gonderilecek", 0));
            _deneme = Math.Max(0, KayitAlani.TamSayi(d, "deneme", 0));
            _sonrakiDeneme = KayitAlani.Zaman(d, "sonraki_deneme", DateTime.MinValue);
            _alinanBasamaklar.Clear();
            foreach (object? b in KayitAlani.Dizi(d, "alinan"))
            {
                _alinanBasamaklar.Add(KayitAlani.TamSayi(b, "alinan"));
            }

            _ortaklar = KayitAlani.Dizi(d, "ortaklar")
                .OfType<Dictionary<string, object?>>()
                .Where(o => o.TryGetValue("kimlik", out object? k) && k is string)
                .Select(o => new GrupUyesi((string)o["kimlik"]!, KayitAlani.TamSayi(o, "puan", 0), KayitAlani.Mantik(o, "bot", false)))
                .ToList();
            _oduller = KayitAlani.Dizi(d, "oduller")
                .Select(o => o is Dictionary<string, object?> paket ? OdulPaketi.Oku(paket) : null)
                .ToList();
        }
    }
}
