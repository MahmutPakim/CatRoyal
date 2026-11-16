using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Sunucu;

namespace CatRoyal.Cekirdek.Meta
{
    /// <summary>
    /// Gelen kutusu (GDD 9.1.8): hediye canlar, takım teklifi ödülleri ve gönderilen kartlar buraya düşer. En fazla
    /// parametre: gelen_kutusu_azami mesaj alır; fazlası yer açılana kadar sunucuda bekler.
    /// </summary>
    public sealed class GelenKutusu
    {
        private readonly Oyuncu _oyuncu;
        private readonly List<GelenKutusuOgesi> _ogeler = new List<GelenKutusuOgesi>();

        public GelenKutusu(Oyuncu oyuncu)
        {
            _oyuncu = oyuncu;
        }

        public IReadOnlyList<GelenKutusuOgesi> Ogeler => _ogeler;

        public int Azami => _oyuncu.Parametreler.TamSayi(MetaParametreAdlari.GelenKutusuAzami);

        /// <summary>Sunucuda bekleyen hediyeleri, kutuda yer olduğu kadar alır.</summary>
        /// <returns>Alınan öğe sayısı; sunucuya ulaşılamadıysa 0.</returns>
        public int Esitle(ISosyalSunucu sunucu)
        {
            int yer = Azami - _ogeler.Count;
            if (yer <= 0)
            {
                return 0;
            }

            var yanit = sunucu.GelenKutusu(_oyuncu.Kimlik, yer);
            if (!yanit.Basarili)
            {
                return 0;
            }

            _ogeler.AddRange(yanit.Deger.Take(yer));
            return Math.Min(yer, yanit.Deger.Count);
        }

        /// <summary>
        /// Öğeyi toplar: içeriği oyuncuya verilir. Toplanan can sayıya eklenir, azami can aşılmaz (GDD 9.1.8). Kart,
        /// sezonu sürüyorsa albüme eklenir (9.7.9); sezonu bitmiş kart boşa gider.
        /// </summary>
        public bool Topla(string kimlik, DateTime simdi)
        {
            var oge = _ogeler.FirstOrDefault(o => o.Kimlik == kimlik);
            if (oge == null)
            {
                return false;
            }

            _ogeler.Remove(oge);
            _oyuncu.OdulVer(oge.Paket, simdi);
            if (oge.KartSezonu != null && oge.KartSezonu == _oyuncu.AlbumSezonu.Kimlik)
            {
                _oyuncu.AlbumSezonu.KartAl(oge.KartSeti, oge.KartNo, simdi);
            }

            return true;
        }

        /// <returns>Toplanan öğe sayısı.</returns>
        public int HepsiniTopla(DateTime simdi) => _ogeler.Select(o => o.Kimlik).ToList().Count(k => Topla(k, simdi));

        internal List<object?> DurumuYaz() => _ogeler.Select(o => (object?)new Dictionary<string, object?>
        {
            ["kimlik"] = o.Kimlik,
            ["gonderen"] = o.Gonderen,
            ["paket"] = o.Paket.DurumuYaz(),
            ["kart_sezonu"] = o.KartSezonu,
            ["kart_seti"] = o.KartSeti,
            ["kart_no"] = o.KartNo,
        }).ToList();

        internal void DurumuYukle(List<object?> liste)
        {
            _ogeler.Clear();
            foreach (var d in liste.OfType<Dictionary<string, object?>>())
            {
                if (!(d.TryGetValue("kimlik", out object? kimlik) && kimlik is string ad))
                {
                    continue;
                }

                _ogeler.Add(new GelenKutusuOgesi(
                    ad,
                    d.TryGetValue("gonderen", out object? gonderen) && gonderen is string g ? g : string.Empty,
                    OdulPaketi.Oku(KayitAlani.Nesne(d, "paket")))
                {
                    KartSezonu = d.TryGetValue("kart_sezonu", out object? sezon) ? sezon as string : null,
                    KartSeti = KayitAlani.TamSayi(d, "kart_seti", 0),
                    KartNo = KayitAlani.TamSayi(d, "kart_no", 0),
                });
            }
        }
    }

    /// <summary>
    /// Oyuncunun takım üyeliği (GDD 10.7). Takımın kendisi (üyeler, sohbet, istekler) sunucudadır; burada oyuncunun
    /// durumu ve istemcide uygulanan kurallar vardır: kurma bedeli, yardım ödülü, susturma, ebeveyn denetimi.
    ///
    /// Durumlar: kilitli → (açılış bölümü) takımsız → açık takıma katıldı: üye; kapalı takıma istek: istek bekliyor →
    /// (onay) üye, (ret ya da iptal) takımsız; takım kurdu: lider. Yetki alan üye yardımcı lider olur; ayrılan ya da
    /// atılan takımsız duruma döner. Başkalarının yaptığı değişiklikler <see cref="Esitle"/> ile öğrenilir.
    /// </summary>
    public sealed class TakimUyeligi
    {
        private readonly Oyuncu _oyuncu;
        private readonly HashSet<string> _susturulanlar = new HashSet<string>(StringComparer.Ordinal);
        private TakimDurumu _durum = TakimDurumu.Takimsiz;
        private DateTime? _sonCanIstegi;

        public TakimUyeligi(Oyuncu oyuncu)
        {
            _oyuncu = oyuncu;
        }

        public bool Acik => _oyuncu.Kapilar.Acik(MetaParametreAdlari.AcilisTakim, _oyuncu.EnYuksekBolum);

        /// <summary>Oyuncunun takım durumu; açılış bölümünden önce kilitli (GDD 10.7.1).</summary>
        public TakimDurumu Durum => Acik ? _durum : TakimDurumu.Kilitli;

        /// <summary>Üyesi olunan ya da istek gönderilen takımın kimliği.</summary>
        public string? TakimKimligi { get; private set; }

        public bool Uye => Acik && (_durum == TakimDurumu.Uye || _durum == TakimDurumu.YardimciLider || _durum == TakimDurumu.Lider);

        /// <summary>Ebeveyn denetimi sohbeti kapatabilir (GDD 10.7.7).</summary>
        public bool SohbetKapali { get; set; }

        public IReadOnlyCollection<string> Susturulanlar => _susturulanlar;

        private string Kimlik => _oyuncu.Kimlik;

        private void Uygula(OyuncuTakimDurumu durum)
        {
            _durum = durum.Durum == TakimDurumu.Kilitli ? TakimDurumu.Takimsiz : durum.Durum;
            TakimKimligi = durum.Takim?.Kimlik;
        }

        /// <summary>
        /// Takım kurar (GDD 10.7.2): para ister (parametre: takim_kurma_bedeli); ad, simge ve takım türü seçilir.
        /// Para, takım kurulunca harcanır.
        /// </summary>
        public bool Kur(ISosyalSunucu sunucu, string ad, string simge, bool acik)
        {
            int bedel = _oyuncu.Parametreler.TamSayi(MetaParametreAdlari.TakimKurmaBedeli);
            if (Durum != TakimDurumu.Takimsiz)
            {
                return false;
            }

            if (_oyuncu.Cuzdan.Bakiye < bedel)
            {
                // Yetersiz bakiye: harcama başarısız olur ve para mağazası önerilir (GDD 9.2.5).
                return _oyuncu.Cuzdan.Harca(bedel, "takim_kurma");
            }

            var yanit = sunucu.TakimKur(Kimlik, ad, simge, acik);
            if (!yanit.Basarili)
            {
                return false;
            }

            _oyuncu.Cuzdan.Harca(bedel, "takim_kurma");
            _durum = TakimDurumu.Lider;
            TakimKimligi = yanit.Deger.Kimlik;
            return true;
        }

        /// <summary>Takım arar; metin verilmezse önerilen takımlar gelir (GDD 10.7.1).</summary>
        public IReadOnlyList<TakimOzeti> Ara(ISosyalSunucu sunucu, string? metin = null)
        {
            var yanit = Acik ? sunucu.TakimAra(Kimlik, metin) : SunucuYaniti<IReadOnlyList<TakimOzeti>>.Basarisiz();
            return yanit.Basarili ? yanit.Deger : new List<TakimOzeti>();
        }

        /// <summary>Açık takıma doğrudan katılır; kapalı takıma istek gönderir (GDD 10.7.2).</summary>
        public bool Katil(ISosyalSunucu sunucu, string takim)
        {
            if (Durum != TakimDurumu.Takimsiz)
            {
                return false;
            }

            var yanit = sunucu.TakimaKatil(Kimlik, takim);
            if (yanit.Basarili)
            {
                Uygula(yanit.Deger);
            }

            return yanit.Basarili;
        }

        public bool IstegiIptalEt(ISosyalSunucu sunucu)
        {
            if (Durum != TakimDurumu.IstekBekliyor || !sunucu.IstegiIptalEt(Kimlik).Basarili)
            {
                return false;
            }

            _durum = TakimDurumu.Takimsiz;
            TakimKimligi = null;
            return true;
        }

        /// <summary>
        /// Takımdan ayrılırken sürmekte olan bir takım olayı varsa uyarı gösterilir (GDD 10.7.6, 10.3.7).
        /// </summary>
        public bool AyrilmaUyarisiGerekir(DateTime simdi) =>
            Uye && _oyuncu.Olaylar.Kurulu.OfType<TakimYarisiOlayi>().Any(o => o.Yarisiyor(simdi));

        public bool Ayril(ISosyalSunucu sunucu)
        {
            if (!Uye || !sunucu.TakimdanAyril(Kimlik).Basarili)
            {
                return false;
            }

            _durum = TakimDurumu.Takimsiz;
            TakimKimligi = null;
            return true;
        }

        /// <summary>
        /// Durumu sunucudan alır: isteğin onayı ya da reddi, yetki değişikliği, atılma (GDD 10.7 durumlar).
        /// </summary>
        /// <returns>Sunucuya ulaşılamadıysa false; durum değişmez.</returns>
        public bool Esitle(ISosyalSunucu sunucu)
        {
            if (!Acik)
            {
                return false;
            }

            var yanit = sunucu.TakimDurumu(Kimlik);
            if (yanit.Basarili)
            {
                Uygula(yanit.Deger);
            }

            return yanit.Basarili;
        }

        // ------------------------------------------------------------------ Sohbet

        /// <summary>
        /// Takım sohbeti (GDD 10.7.3): susturulan oyuncuların mesajları gösterilmez (10.7.4); ebeveyn denetimi sohbeti
        /// kapattıysa liste boştur (10.7.7).
        /// </summary>
        public IReadOnlyList<SohbetMesaji> Sohbet(ISosyalSunucu sunucu)
        {
            if (!Uye || SohbetKapali)
            {
                return new List<SohbetMesaji>();
            }

            var yanit = sunucu.Sohbet(Kimlik);
            return yanit.Basarili ? yanit.Deger.Where(m => !_susturulanlar.Contains(m.Gonderen)).ToList() : new List<SohbetMesaji>();
        }

        public bool SohbetGonder(ISosyalSunucu sunucu, string metin) =>
            Uye && !SohbetKapali && sunucu.SohbetGonder(Kimlik, metin).Basarili;

        /// <summary>Oyuncular diğer kullanıcıları susturabilir (GDD 10.7.4).</summary>
        public void Sustur(string oyuncu) => _susturulanlar.Add(oyuncu);

        public void SusturmayiKaldir(string oyuncu) => _susturulanlar.Remove(oyuncu);

        // ------------------------------------------------------------------ Can yardımlaşması

        /// <summary>Yeni bir can isteği gönderilebilir mi: üye ve son istekten bu yana bekleme süresi dolmuş (GDD 10.7.3)?</summary>
        public bool CanIsteyebilir(DateTime simdi) =>
            Uye
            && (!_sonCanIstegi.HasValue
                || simdi - _sonCanIstegi.Value >= TimeSpan.FromHours(_oyuncu.Parametreler.Sayi(MetaParametreAdlari.CanIstekAraligiSaat)));

        /// <summary>
        /// Takımdan can ister (GDD 9.1.6, 10.7.3). Bekleme süresi ve yardım meni sunucuda da denetlenir (9.1.10);
        /// sunucu reddederse istek gönderilmemiş sayılır.
        /// </summary>
        public bool CanIste(ISosyalSunucu sunucu, DateTime simdi)
        {
            if (!CanIsteyebilir(simdi) || !sunucu.CanIste(Kimlik).Basarili)
            {
                return false;
            }

            _sonCanIstegi = simdi;
            return true;
        }

        /// <summary>
        /// Bir takım arkadaşının can isteğine yardım eder: yardım edene para verilir (GDD 10.7.3;
        /// parametre: can_yardim_odulu), isteyenin gelen kutusuna can gider.
        /// </summary>
        public bool CanYardimEt(ISosyalSunucu sunucu, long mesaj)
        {
            if (!Uye || !sunucu.CanYardimEt(Kimlik, mesaj).Basarili)
            {
                return false;
            }

            _oyuncu.Cuzdan.Ekle(_oyuncu.Parametreler.TamSayi(MetaParametreAdlari.CanYardimOdulu));
            return true;
        }

        /// <summary>
        /// Takım teklifinin ödemesi tamamlandı (GDD 10.7.6): paket oyuncuya verilir ve takım üyelerine de ödül dağıtılır.
        /// </summary>
        public bool TakimTeklifiAlindi(ISosyalSunucu sunucu, OdulPaketi paket, OdulPaketi uyeOdulu, DateTime simdi)
        {
            if (!Uye)
            {
                return false;
            }

            _oyuncu.OdulVer(paket, simdi);
            sunucu.TakimTeklifiDagit(Kimlik, uyeOdulu);
            return true;
        }

        // ------------------------------------------------------------------ Kartlar (GDD 9.7.9)

        /// <summary>
        /// Kart gönderir: arkadaşa, takım üyesine ya da (<paramref name="alici"/> null ise) takım sohbetine. Günde en
        /// fazla parametre: gunluk_kart_gonderme kart gönderilebilir. Yorum: oyuncu albümünde bulunan bir kartı
        /// gönderir ve kart kendi albümünde kalır; çift kartlar yıldıza dönüştüğü için (9.7.8) elde ayrı kopya yoktur.
        /// </summary>
        public bool KartGonder(ISosyalSunucu sunucu, string? alici, int set, int kart, DateTime simdi)
        {
            var album = _oyuncu.AlbumSezonu;
            long gun = _oyuncu.GunlukOdul.GunNumarasi(simdi);
            if (!album.Suruyor(simdi)
                || !album.Album.Var(set, kart)
                || !album.KartGonderebilir(gun)
                || (alici == null && !Uye)
                || !sunucu.KartGonder(Kimlik, alici, album.Kimlik!, set, kart).Basarili)
            {
                return false;
            }

            album.GonderimiKaydet(gun);
            return true;
        }

        /// <summary>
        /// Takım sohbetinden kart ister. İstek sınırı belirli aralıkla sıfırlanır (parametre: kart_istek_sifirlama_saat).
        /// </summary>
        public bool KartIste(ISosyalSunucu sunucu, int set, int kart, DateTime simdi)
        {
            var album = _oyuncu.AlbumSezonu;
            if (!Uye
                || !album.Suruyor(simdi)
                || album.Album.Var(set, kart)
                || !album.KartIsteyebilir(simdi)
                || !sunucu.KartIste(Kimlik, album.Kimlik!, set, kart).Basarili)
            {
                return false;
            }

            album.IstegiKaydet(simdi);
            return true;
        }

        /// <summary>Takım sohbetine gönderilmiş kartı alır; kart gelen kutusuna düşer.</summary>
        public bool SohbettenKartAl(ISosyalSunucu sunucu, long mesaj) => Uye && sunucu.SohbettenKartAl(Kimlik, mesaj).Basarili;

        // ------------------------------------------------------------------ Kayıt

        internal Dictionary<string, object?> DurumuYaz() => new Dictionary<string, object?>
        {
            ["durum"] = _durum.ToString(),
            ["takim"] = TakimKimligi,
            ["son_can_istegi"] = _sonCanIstegi.HasValue ? KayitAlani.Zaman(_sonCanIstegi.Value) : null,
            ["sohbet_kapali"] = SohbetKapali,
            ["susturulanlar"] = _susturulanlar.OrderBy(s => s, StringComparer.Ordinal).Cast<object?>().ToList(),
        };

        internal void DurumuYukle(Dictionary<string, object?> d)
        {
            _durum = d.TryGetValue("durum", out object? durum)
                && Enum.TryParse(durum as string, out TakimDurumu okunan)
                && Enum.IsDefined(typeof(TakimDurumu), okunan)
                && okunan != TakimDurumu.Kilitli
                    ? okunan
                    : TakimDurumu.Takimsiz;
            TakimKimligi = d.TryGetValue("takim", out object? takim) ? takim as string : null;
            _sonCanIstegi = d.TryGetValue("son_can_istegi", out object? son) && son != null
                ? KayitAlani.Zaman(son, "son_can_istegi")
                : (DateTime?)null;
            SohbetKapali = KayitAlani.Mantik(d, "sohbet_kapali", false);
            _susturulanlar.Clear();
            _susturulanlar.UnionWith(KayitAlani.Dizi(d, "susturulanlar").OfType<string>());
        }
    }
}
