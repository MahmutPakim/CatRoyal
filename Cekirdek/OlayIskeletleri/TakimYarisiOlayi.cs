using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Sunucu;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.OlayIskeletleri
{
    /// <summary>
    /// Takım yarışı olayı (GDD 10.3): takımlar birbirine karşı yarışır; üyelerin bölüm kazanımları takım puanına
    /// eklenir ve sıralamaya göre ödül alınır. Yalnızca bir takımdaki oyuncu katılır (10.3.1).
    ///
    /// Durumlar: takımsız → (takıma katıldı) hazır → (gruba atandı) yarışta → (bitime son saatler kaldı) son saatler
    /// → (süre doldu) bitti → katkı yeterliyse ödül alındı, yoksa ödülsüz kapandı. Yarışta takımdan ayrılan oyuncu
    /// takımsız duruma döner ve katkısı silinir. "Kilitli" durumu olayın kapısından gelir.
    ///
    /// Yorumlar: takım, grup isteğinde katılımcı olarak geçer (grup ve sıralama <see cref="ISunucu"/> üzerinden);
    /// puan kazanılan bölüm sayısına dayandığı için çarpanla katlanmaz; sonuç oyuncu kapatana kadar bir süre bekler,
    /// kapatılmazsa ödül kendiliğinden verilir (10.0.8).
    /// </summary>
    public sealed class TakimYarisiOlayi : OlayIskeleti
    {
        /// <summary>Süre dolduktan sonra sonucun oyuncuyu beklediği süre.</summary>
        public static readonly TimeSpan SonucBeklemesi = TimeSpan.FromDays(3);

        private readonly ParametreTablosu _p;
        private IReadOnlyList<OdulPaketi?> _oduller = Array.Empty<OdulPaketi?>();
        private List<GrupUyesi> _liste = new List<GrupUyesi>();
        private Asama _asama = Asama.Hazir;
        private string? _takim;
        private int _gonderilecek;

        private enum Asama
        {
            Hazir,
            Yarista,
            OdulAlindi,
            OdulsuzKapandi,
        }

        public TakimYarisiOlayi(string kimlik, DateTime bitis, ParametreTablosu p)
            : base(kimlik, MetaParametreAdlari.AcilisTakimYarisi, bitis)
        {
            _p = p;
        }

        public override OlayTuru Tur => OlayTuru.TakimYarisi;

        public override bool Carpilabilir => false;

        /// <summary>Yarışan oyuncunun sonucu, süre dolduktan sonra da bir süre kapatılmayı bekler.</summary>
        protected override DateTime EtkinBitis => _asama == Asama.Yarista ? Bitis + SonucBeklemesi : Bitis;

        /// <summary>Oyuncunun kendi katkısı (GDD 10.3.3, 10.3.5).</summary>
        public int Katki { get; private set; }

        /// <summary>Gruptaki takımlar, birinciden sonuncuya; son bilinen liste yerelde güncellenir (GDD 10.3.3).</summary>
        public IReadOnlyList<GrupUyesi> Liste => _liste;

        public int TakimPuani => _liste.FirstOrDefault(t => t.Kimlik == _takim)?.Puan ?? 0;

        /// <summary>Takımın 1 tabanlı sırası; yarışta değilse 0.</summary>
        public int Sira => _liste.FindIndex(t => t.Kimlik == _takim) + 1;

        public bool Yarisiyor(DateTime simdi) => _asama == Asama.Yarista && simdi < Bitis;

        public TakimYarisiDurumu Durum(Oyuncu oyuncu, DateTime simdi)
        {
            if (_asama == Asama.OdulAlindi) return TakimYarisiDurumu.OdulAlindi;
            if (_asama == Asama.OdulsuzKapandi) return TakimYarisiDurumu.OdulsuzKapandi;
            if (_asama == Asama.Yarista && simdi >= Bitis) return TakimYarisiDurumu.Bitti;
            if (!oyuncu.Takim.Uye) return TakimYarisiDurumu.Takimsiz;
            if (_asama == Asama.Hazir) return TakimYarisiDurumu.Hazir;
            return Bitis - simdi <= TimeSpan.FromHours(_p.Sayi(MetaParametreAdlari.SonSaatlerPenceresiSaat))
                ? TakimYarisiDurumu.SonSaatler
                : TakimYarisiDurumu.Yarista;
        }

        /// <summary>Sıra ödülleri, birinciden başlayarak; sunucu listesi istemcideki varsayılanın yerine geçer (GDD 10.3.6).</summary>
        public override void OdulleriAyarla(IReadOnlyList<OdulPaketi?> oduller) => _oduller = oduller;

        /// <summary>
        /// Sıranın ödülü (GDD 10.3.6): sunucu listesi verildiyse o, verilmediyse parametre:
        /// takim_yarisi_sira_odulleri içindeki para. Listede karşılığı olmayan sıranın ödülü yoktur.
        /// </summary>
        public OdulPaketi? SiraOdulu(int sira)
        {
            if (sira < 1)
            {
                return null;
            }

            if (_oduller.Count > 0)
            {
                return sira <= _oduller.Count ? _oduller[sira - 1] : null;
            }

            var paralar = _p.TamSayiListesi(MetaParametreAdlari.TakimYarisiSiraOdulleri);
            return sira <= paralar.Count ? new OdulPaketi { Para = paralar[sira - 1] } : null;
        }

        /// <summary>
        /// Yarışı ilerletir: takımdaki oyuncunun takımı gruba konur (GDD 10.3.2), bekleyen puan gönderilir (10.3.3),
        /// son saatlere girilince uyarı penceresi akışa eklenir (10.3.4). Takımdan ayrılan oyuncu yarıştan düşer.
        /// </summary>
        public void Ilerle(ISunucu sunucu, Oyuncu oyuncu, DateTime simdi)
        {
            if (_asama == Asama.Yarista && simdi < Bitis && (!oyuncu.Takim.Uye || oyuncu.Takim.TakimKimligi != _takim))
            {
                _asama = Asama.Hazir;
                _takim = null;
                Katki = 0;
                _gonderilecek = 0;
                _liste = new List<GrupUyesi>();
            }

            if (_asama == Asama.Hazir && oyuncu.Takim.Uye && simdi < Bitis)
            {
                string takim = oyuncu.Takim.TakimKimligi!;
                var yanit = sunucu.GrubaKatil(Kimlik, takim, _p.TamSayi(MetaParametreAdlari.TakimYarisiGrupBoyutu));
                if (yanit.Basarili)
                {
                    _takim = takim;
                    _asama = Asama.Yarista;
                    ListeyiAl(yanit.Deger);
                }
            }

            if (_asama != Asama.Yarista)
            {
                return;
            }

            if (_gonderilecek > 0)
            {
                var yanit = sunucu.PuanGonder(Kimlik, _takim!, _gonderilecek);
                if (yanit.Basarili)
                {
                    _gonderilecek = 0;
                    ListeyiAl(yanit.Deger);
                }
            }

            if (Durum(oyuncu, simdi) == TakimYarisiDurumu.SonSaatler)
            {
                oyuncu.Pencereler.Ekle(new Pencere("takim_yarisi_son_saatler:" + Kimlik, PencereTuru.Bilgi, Kimlik));
            }
        }

        /// <summary>
        /// Kazanılan bölüm takım puanı getirir (GDD 10.3.3): puan hem oyuncunun hem takımının toplamına eklenir ve
        /// sıralama yerelde de güncellenir.
        /// </summary>
        public override void BolumKazanildi(BolumSonucu sonuc, DateTime simdi)
        {
            if (!Yarisiyor(simdi))
            {
                return;
            }

            int puan = _p.TamSayi(sonuc.Etiket == ZorlukEtiketi.Normal
                ? MetaParametreAdlari.TakimYarisiPuanNormal
                : MetaParametreAdlari.TakimYarisiPuanZor);
            Katki += puan;
            _gonderilecek += puan;
            Sirala(_liste.Select(t => t.Kimlik == _takim ? new GrupUyesi(t.Kimlik, t.Puan + puan, t.Bot) : t));
        }

        /// <summary>Sunucunun listesi henüz gönderilmemiş puanı içermez; yerelde eklenir.</summary>
        private void ListeyiAl(OlayGrubu grup) =>
            Sirala(grup.Siralama.Select(t => t.Kimlik == _takim ? new GrupUyesi(t.Kimlik, t.Puan + _gonderilecek, false) : t));

        private void Sirala(IEnumerable<GrupUyesi> takimlar) =>
            _liste = takimlar
                .OrderByDescending(t => t.Puan)
                .ThenBy(t => t.Kimlik == _takim ? 0 : 1)
                .ThenBy(t => t.Kimlik, StringComparer.Ordinal)
                .ToList();

        private bool KatkiYeterli => Katki >= _p.TamSayi(MetaParametreAdlari.TakimYarisiAsgariKatki);

        /// <summary>
        /// Olay bitince sonuç kapatılır (GDD 10.3.5): katkısı yeterli oyuncu sırasının ödülünü alır; yetersizse olay
        /// ödülsüz kapanır ve sunucuya "ödülsüz kapat" bildirilir.
        /// </summary>
        /// <returns>Kapatılacak bir sonuç yoksa false.</returns>
        public bool SonucuKapat(ISosyalSunucu? sosyal, Oyuncu oyuncu, DateTime simdi)
        {
            if (_asama != Asama.Yarista || simdi < Bitis)
            {
                return false;
            }

            bool odulAlindi = KatkiYeterli;
            if (odulAlindi && SiraOdulu(Sira) is { } odul)
            {
                oyuncu.OdulVer(odul, simdi);
            }

            _asama = odulAlindi ? Asama.OdulAlindi : Asama.OdulsuzKapandi;
            sosyal?.TakimYarisiniKapat(Kimlik, oyuncu.Kimlik, odulAlindi);
            return true;
        }

        /// <summary>Sonuç kapatılmadan bekleme süresi dolarsa hak edilen ödül kendiliğinden verilir (GDD 10.0.8).</summary>
        public override List<OdulPaketi> AlinmamislariAl()
        {
            var oduller = new List<OdulPaketi>();
            if (_asama != Asama.Yarista)
            {
                return oduller;
            }

            if (KatkiYeterli && SiraOdulu(Sira) is { } odul)
            {
                oduller.Add(odul);
            }

            _asama = KatkiYeterli ? Asama.OdulAlindi : Asama.OdulsuzKapandi;
            return oduller;
        }

        internal override Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["asama"] = _asama.ToString(),
            ["takim"] = _takim,
            ["katki"] = Katki,
            ["gonderilecek"] = _gonderilecek,
            ["liste"] = _liste.Select(t => (object?)new Dictionary<string, object?>
            {
                ["kimlik"] = t.Kimlik,
                ["puan"] = t.Puan,
                ["bot"] = t.Bot,
            }).ToList(),
            ["oduller"] = _oduller.Select(o => (object?)o?.DurumuYaz()).ToList(),
        };

        internal override void DurumuYukle(Dictionary<string, object?> d)
        {
            _asama = d.TryGetValue("asama", out object? asama) && Enum.TryParse(asama as string, out Asama okunan) && Enum.IsDefined(typeof(Asama), okunan)
                ? okunan
                : Asama.Hazir;
            _takim = d.TryGetValue("takim", out object? takim) ? takim as string : null;
            Katki = Math.Max(0, KayitAlani.TamSayi(d, "katki", 0));
            _gonderilecek = Math.Max(0, KayitAlani.TamSayi(d, "gonderilecek", 0));
            _liste = KayitAlani.Dizi(d, "liste")
                .OfType<Dictionary<string, object?>>()
                .Where(t => t.TryGetValue("kimlik", out object? k) && k is string)
                .Select(t => new GrupUyesi((string)t["kimlik"]!, KayitAlani.TamSayi(t, "puan", 0), KayitAlani.Mantik(t, "bot", false)))
                .ToList();
            _oduller = KayitAlani.Dizi(d, "oduller")
                .Select(o => o is Dictionary<string, object?> paket ? OdulPaketi.Oku(paket) : null)
                .ToList();
        }
    }
}
