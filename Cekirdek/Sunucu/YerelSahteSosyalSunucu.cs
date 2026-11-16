using System;
using System.Collections.Generic;
using System.Linq;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.OlayIskeletleri;
using CatRoyal.Cekirdek.Yapilandirma;

namespace CatRoyal.Cekirdek.Sunucu
{
    /// <summary>
    /// <see cref="ISosyalSunucu"/>'nun yerel sahte uygulaması: takımları, sohbeti, gelen kutularını ve ortak gruplarını
    /// bellekte tutar. Birden fazla oyuncuyu aynı anda taşır; testler başka oyuncuları onların kimliğiyle çağırarak
    /// canlandırır.
    ///
    /// Sunucuya bırakılmış kararlardaki seçimler sahte uygulamanın kendisinindir: etkinlik düzeyi son 24 saatteki
    /// üye başına mesaj sayısından bulunur (2 ve üstü yüksek, 0.5 ve üstü orta); önerilen takımlar yeri olan açık
    /// takımlardır; ayrılan liderin yerine ilk yardımcı lider, yoksa ilk üye geçer; ortaklar saatte sabit puan toplar.
    /// </summary>
    public sealed class YerelSahteSosyalSunucu : ISosyalSunucu
    {
        private readonly ISaat _saat;
        private readonly ParametreTablosu _p;
        private readonly Dictionary<string, Takim> _takimlar = new Dictionary<string, Takim>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<GelenKutusuOgesi>> _kutular = new Dictionary<string, List<GelenKutusuOgesi>>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _sonCanIstegi = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _yardimMeni = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly Dictionary<string, Ortaklik> _ortakliklar = new Dictionary<string, Ortaklik>(StringComparer.Ordinal);
        private long _sayac;

        public YerelSahteSosyalSunucu(ISaat saat, ParametreTablosu parametreler)
        {
            _saat = saat;
            _p = parametreler;
        }

        /// <summary>Bağlantı yokmuş gibi davranır: bütün istekler başarısız döner.</summary>
        public bool Cevrimdisi { get; set; }

        /// <summary>Ortakların saatte topladığı puan; her ortağın hızı bunun 1–3 katıdır.</summary>
        public int OrtakSaatlikPuan { get; set; } = 500;

        /// <summary>Takım yarışı için bildirilen kapanışlar: (olay, oyuncu, ödül alındı mı).</summary>
        public List<(string Olay, string Oyuncu, bool OdulAlindi)> YarisKapanislari { get; } = new List<(string, string, bool)>();

        /// <summary>Kötüye kullanımda yardım men süresi uygular (GDD 10.7.4, 9.1.10).</summary>
        public void YardimMeniUygula(string oyuncu, TimeSpan sure) => _yardimMeni[oyuncu] = _saat.SimdiUtc + sure;

        /// <summary>Oyuncunun gelen kutusuna doğrudan hediye koyar.</summary>
        public void HediyeGonder(string alici, string gonderen, OdulPaketi paket) =>
            Kutu(alici).Add(new GelenKutusuOgesi(YeniKimlik("hediye"), gonderen, paket));

        private static SunucuYaniti<bool> Tamam => SunucuYaniti<bool>.Tamam(true);

        private static SunucuYaniti<bool> Hayir => SunucuYaniti<bool>.Basarisiz();

        private string YeniKimlik(string onEk) => onEk + "_" + (++_sayac);

        private List<GelenKutusuOgesi> Kutu(string oyuncu)
        {
            if (!_kutular.TryGetValue(oyuncu, out var kutu))
            {
                kutu = new List<GelenKutusuOgesi>();
                _kutular.Add(oyuncu, kutu);
            }

            return kutu;
        }

        private Takim? TakimiBul(string oyuncu) => _takimlar.Values.FirstOrDefault(t => t.Uyeler.ContainsKey(oyuncu));

        private Takim? IstekTakimi(string oyuncu) => _takimlar.Values.FirstOrDefault(t => t.Istekler.Contains(oyuncu));

        private bool Menli(string oyuncu) => _yardimMeni.TryGetValue(oyuncu, out var bitis) && _saat.SimdiUtc < bitis;

        private EtkinlikDuzeyi Etkinlik(Takim takim)
        {
            var sinir = _saat.SimdiUtc - TimeSpan.FromHours(24);
            double oran = takim.Uyeler.Count == 0 ? 0 : takim.Sohbet.Count(m => m.Zaman >= sinir) / (double)takim.Uyeler.Count;
            return oran >= 2 ? EtkinlikDuzeyi.Yuksek : oran >= 0.5 ? EtkinlikDuzeyi.Orta : EtkinlikDuzeyi.Dusuk;
        }

        private TakimBilgisi Bilgi(Takim t) => new TakimBilgisi(
            t.Kimlik, t.Ad, t.Simge, t.Acik, Etkinlik(t),
            t.Sira.Select(k => new TakimUyesi(k, t.Uyeler[k])).ToList(),
            t.Istekler.ToList());

        private OyuncuTakimDurumu Durum(string oyuncu)
        {
            if (TakimiBul(oyuncu) is { } takim)
            {
                return new OyuncuTakimDurumu(takim.Uyeler[oyuncu], Bilgi(takim));
            }

            return IstekTakimi(oyuncu) is { } istek
                ? new OyuncuTakimDurumu(OlayIskeletleri.TakimDurumu.IstekBekliyor, Bilgi(istek))
                : new OyuncuTakimDurumu(OlayIskeletleri.TakimDurumu.Takimsiz, null);
        }

        private bool Serbest(string oyuncu) => TakimiBul(oyuncu) == null && IstekTakimi(oyuncu) == null;

        // ------------------------------------------------------------------ Takımlar

        public SunucuYaniti<TakimBilgisi> TakimKur(string oyuncu, string ad, string simge, bool acik)
        {
            if (Cevrimdisi || !Serbest(oyuncu) || string.IsNullOrWhiteSpace(ad))
            {
                return SunucuYaniti<TakimBilgisi>.Basarisiz();
            }

            var takim = new Takim(YeniKimlik("takim"), ad.Trim(), simge, acik);
            takim.Ekle(oyuncu, OlayIskeletleri.TakimDurumu.Lider);
            _takimlar.Add(takim.Kimlik, takim);
            return SunucuYaniti<TakimBilgisi>.Tamam(Bilgi(takim));
        }

        public SunucuYaniti<IReadOnlyList<TakimOzeti>> TakimAra(string oyuncu, string? metin)
        {
            if (Cevrimdisi)
            {
                return SunucuYaniti<IReadOnlyList<TakimOzeti>>.Basarisiz();
            }

            int azami = _p.TamSayi("takim_azami_uye");
            var bulunan = _takimlar.Values
                .Where(t => metin == null
                    ? t.Acik && t.Uyeler.Count < azami
                    : t.Ad.IndexOf(metin, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(t => (int)Etkinlik(t))
                .ThenBy(t => t.Kimlik, StringComparer.Ordinal)
                .Select(t => (TakimOzeti)Bilgi(t))
                .ToList();
            return SunucuYaniti<IReadOnlyList<TakimOzeti>>.Tamam(bulunan);
        }

        public SunucuYaniti<OyuncuTakimDurumu> TakimaKatil(string oyuncu, string takim)
        {
            if (Cevrimdisi
                || !Serbest(oyuncu)
                || !_takimlar.TryGetValue(takim, out var hedef)
                || hedef.Uyeler.Count >= _p.TamSayi("takim_azami_uye"))
            {
                return SunucuYaniti<OyuncuTakimDurumu>.Basarisiz();
            }

            if (hedef.Acik)
            {
                hedef.Ekle(oyuncu, OlayIskeletleri.TakimDurumu.Uye);
            }
            else
            {
                hedef.Istekler.Add(oyuncu);
            }

            return SunucuYaniti<OyuncuTakimDurumu>.Tamam(Durum(oyuncu));
        }

        public SunucuYaniti<OyuncuTakimDurumu> TakimDurumu(string oyuncu) =>
            Cevrimdisi ? SunucuYaniti<OyuncuTakimDurumu>.Basarisiz() : SunucuYaniti<OyuncuTakimDurumu>.Tamam(Durum(oyuncu));

        public SunucuYaniti<bool> IstegiIptalEt(string oyuncu) =>
            !Cevrimdisi && IstekTakimi(oyuncu) is { } takim && takim.Istekler.Remove(oyuncu) ? Tamam : Hayir;

        public SunucuYaniti<bool> IstegiYanitla(string yetkili, string istekSahibi, bool onay)
        {
            var takim = TakimiBul(yetkili);
            if (Cevrimdisi || takim == null || !Yetkili(takim, yetkili) || !takim.Istekler.Contains(istekSahibi))
            {
                return Hayir;
            }

            if (onay && takim.Uyeler.Count >= _p.TamSayi("takim_azami_uye"))
            {
                return Hayir;
            }

            takim.Istekler.Remove(istekSahibi);
            if (onay)
            {
                takim.Ekle(istekSahibi, OlayIskeletleri.TakimDurumu.Uye);
            }

            return Tamam;
        }

        private static bool Yetkili(Takim takim, string oyuncu) =>
            takim.Uyeler[oyuncu] == OlayIskeletleri.TakimDurumu.Lider || takim.Uyeler[oyuncu] == OlayIskeletleri.TakimDurumu.YardimciLider;

        public SunucuYaniti<bool> TakimdanAyril(string oyuncu)
        {
            var takim = TakimiBul(oyuncu);
            if (Cevrimdisi || takim == null)
            {
                return Hayir;
            }

            Cikar(takim, oyuncu);
            return Tamam;
        }

        private void Cikar(Takim takim, string oyuncu)
        {
            bool liderdi = takim.Uyeler[oyuncu] == OlayIskeletleri.TakimDurumu.Lider;
            takim.Uyeler.Remove(oyuncu);
            takim.Sira.Remove(oyuncu);
            if (takim.Uyeler.Count == 0)
            {
                _takimlar.Remove(takim.Kimlik);
            }
            else if (liderdi)
            {
                string yeni = takim.Sira.FirstOrDefault(k => takim.Uyeler[k] == OlayIskeletleri.TakimDurumu.YardimciLider) ?? takim.Sira[0];
                takim.Uyeler[yeni] = OlayIskeletleri.TakimDurumu.Lider;
            }
        }

        public SunucuYaniti<bool> UyeAt(string yetkili, string hedef)
        {
            var takim = TakimiBul(yetkili);
            if (Cevrimdisi || takim == null || yetkili == hedef || !Yetkili(takim, yetkili) || !takim.Uyeler.TryGetValue(hedef, out var rol))
            {
                return Hayir;
            }

            // Lider atılamaz; yardımcı lider yalnızca üyeleri atabilir.
            if (rol == OlayIskeletleri.TakimDurumu.Lider
                || (rol == OlayIskeletleri.TakimDurumu.YardimciLider && takim.Uyeler[yetkili] != OlayIskeletleri.TakimDurumu.Lider))
            {
                return Hayir;
            }

            Cikar(takim, hedef);
            return Tamam;
        }

        public SunucuYaniti<bool> YetkiVer(string lider, string hedef, bool yardimciLider)
        {
            var takim = TakimiBul(lider);
            if (Cevrimdisi
                || takim == null
                || lider == hedef
                || takim.Uyeler[lider] != OlayIskeletleri.TakimDurumu.Lider
                || !takim.Uyeler.ContainsKey(hedef))
            {
                return Hayir;
            }

            takim.Uyeler[hedef] = yardimciLider ? OlayIskeletleri.TakimDurumu.YardimciLider : OlayIskeletleri.TakimDurumu.Uye;
            return Tamam;
        }

        public SunucuYaniti<bool> LiderligiDevret(string lider, string hedef)
        {
            var takim = TakimiBul(lider);
            if (Cevrimdisi
                || takim == null
                || lider == hedef
                || takim.Uyeler[lider] != OlayIskeletleri.TakimDurumu.Lider
                || !takim.Uyeler.ContainsKey(hedef))
            {
                return Hayir;
            }

            takim.Uyeler[hedef] = OlayIskeletleri.TakimDurumu.Lider;
            takim.Uyeler[lider] = OlayIskeletleri.TakimDurumu.Uye;
            return Tamam;
        }

        // ------------------------------------------------------------------ Sohbet, can ve gelen kutusu

        private SohbetMesaji Mesaj(Takim takim, string gonderen, SohbetMesajiTuru tur, string metin = "")
        {
            var mesaj = new SohbetMesaji(++_sayac, gonderen, tur, _saat.SimdiUtc, metin);
            takim.Sohbet.Add(mesaj);
            return mesaj;
        }

        public SunucuYaniti<bool> SohbetGonder(string oyuncu, string metin)
        {
            var takim = TakimiBul(oyuncu);
            if (Cevrimdisi || takim == null || string.IsNullOrWhiteSpace(metin))
            {
                return Hayir;
            }

            Mesaj(takim, oyuncu, SohbetMesajiTuru.Metin, metin);
            return Tamam;
        }

        public SunucuYaniti<IReadOnlyList<SohbetMesaji>> Sohbet(string oyuncu) =>
            !Cevrimdisi && TakimiBul(oyuncu) is { } takim
                ? SunucuYaniti<IReadOnlyList<SohbetMesaji>>.Tamam(takim.Sohbet.ToList())
                : SunucuYaniti<IReadOnlyList<SohbetMesaji>>.Basarisiz();

        public SunucuYaniti<bool> CanIste(string oyuncu)
        {
            var takim = TakimiBul(oyuncu);
            var aralik = TimeSpan.FromHours(_p.Sayi("can_istek_araligi_saat"));
            if (Cevrimdisi
                || takim == null
                || Menli(oyuncu)
                || (_sonCanIstegi.TryGetValue(oyuncu, out var son) && _saat.SimdiUtc - son < aralik))
            {
                return Hayir;
            }

            _sonCanIstegi[oyuncu] = _saat.SimdiUtc;
            Mesaj(takim, oyuncu, SohbetMesajiTuru.CanIstegi).Kalan = _p.TamSayi("can_istegi_azami_yardim");
            return Tamam;
        }

        public SunucuYaniti<bool> CanYardimEt(string yardimEden, long mesaj)
        {
            var takim = TakimiBul(yardimEden);
            var istek = takim?.Sohbet.FirstOrDefault(m => m.Kimlik == mesaj && m.Tur == SohbetMesajiTuru.CanIstegi);
            if (Cevrimdisi
                || takim == null
                || istek == null
                || istek.Gonderen == yardimEden
                || istek.Kalan <= 0
                || Menli(yardimEden)
                || !takim.Yardimlar.Add(mesaj + ":" + yardimEden))
            {
                return Hayir;
            }

            istek.Kalan--;
            Kutu(istek.Gonderen).Add(new GelenKutusuOgesi(YeniKimlik("can"), yardimEden, new OdulPaketi { Can = 1 }));
            return Tamam;
        }

        public SunucuYaniti<IReadOnlyList<GelenKutusuOgesi>> GelenKutusu(string oyuncu, int azami)
        {
            if (Cevrimdisi)
            {
                return SunucuYaniti<IReadOnlyList<GelenKutusuOgesi>>.Basarisiz();
            }

            var kutu = Kutu(oyuncu);
            var teslim = kutu.Take(Math.Max(0, azami)).ToList();
            kutu.RemoveRange(0, teslim.Count);
            return SunucuYaniti<IReadOnlyList<GelenKutusuOgesi>>.Tamam(teslim);
        }

        public SunucuYaniti<bool> TakimTeklifiDagit(string oyuncu, OdulPaketi uyeOdulu)
        {
            var takim = TakimiBul(oyuncu);
            if (Cevrimdisi || takim == null)
            {
                return Hayir;
            }

            foreach (string uye in takim.Sira.Where(u => u != oyuncu))
            {
                Kutu(uye).Add(new GelenKutusuOgesi(YeniKimlik("teklif"), oyuncu, uyeOdulu));
            }

            return Tamam;
        }

        // ------------------------------------------------------------------ Kartlar

        private GelenKutusuOgesi KartOgesi(string gonderen, string sezon, int set, int kart) =>
            new GelenKutusuOgesi(YeniKimlik("kart"), gonderen, new OdulPaketi()) { KartSezonu = sezon, KartSeti = set, KartNo = kart };

        public SunucuYaniti<bool> KartGonder(string gonderen, string? alici, string sezon, int set, int kart)
        {
            if (Cevrimdisi || alici == gonderen)
            {
                return Hayir;
            }

            if (alici != null)
            {
                Kutu(alici).Add(KartOgesi(gonderen, sezon, set, kart));
                return Tamam;
            }

            var takim = TakimiBul(gonderen);
            if (takim == null)
            {
                return Hayir;
            }

            var mesaj = Mesaj(takim, gonderen, SohbetMesajiTuru.Kart);
            mesaj.Sezon = sezon;
            mesaj.Set = set;
            mesaj.Kart = kart;
            mesaj.Kalan = 1;
            return Tamam;
        }

        public SunucuYaniti<bool> KartIste(string oyuncu, string sezon, int set, int kart)
        {
            var takim = TakimiBul(oyuncu);
            if (Cevrimdisi || takim == null)
            {
                return Hayir;
            }

            var mesaj = Mesaj(takim, oyuncu, SohbetMesajiTuru.KartIstegi);
            mesaj.Sezon = sezon;
            mesaj.Set = set;
            mesaj.Kart = kart;
            return Tamam;
        }

        public SunucuYaniti<bool> SohbettenKartAl(string oyuncu, long mesaj)
        {
            var takim = TakimiBul(oyuncu);
            var kart = takim?.Sohbet.FirstOrDefault(m => m.Kimlik == mesaj && m.Tur == SohbetMesajiTuru.Kart);
            if (Cevrimdisi || kart == null || kart.Gonderen == oyuncu || kart.Kalan <= 0)
            {
                return Hayir;
            }

            kart.Kalan = 0;
            Kutu(oyuncu).Add(KartOgesi(kart.Gonderen, kart.Sezon!, kart.Set, kart.Kart));
            return Tamam;
        }

        // ------------------------------------------------------------------ Olaylar

        public SunucuYaniti<bool> TakimYarisiniKapat(string olay, string oyuncu, bool odulAlindi)
        {
            if (Cevrimdisi)
            {
                return Hayir;
            }

            YarisKapanislari.Add((olay, oyuncu, odulAlindi));
            return Tamam;
        }

        public SunucuYaniti<OrtakGrubu> OrtakDavetEt(string olay, string oyuncu, string ortak)
        {
            if (Cevrimdisi || ortak == oyuncu)
            {
                return SunucuYaniti<OrtakGrubu>.Basarisiz();
            }

            var ortaklik = OrtakligiAl(olay, oyuncu);
            if (ortaklik.Ortaklar.ContainsKey(ortak) || ortaklik.Ortaklar.Count >= _p.TamSayi("ortakli_atis_azami_ortak"))
            {
                return SunucuYaniti<OrtakGrubu>.Basarisiz();
            }

            ortaklik.Ortaklar.Add(ortak, _saat.SimdiUtc);
            ortaklik.Sira.Add(ortak);
            return SunucuYaniti<OrtakGrubu>.Tamam(Grup(ortaklik));
        }

        public SunucuYaniti<OrtakGrubu> OrtakPuaniGonder(string olay, string oyuncu, int puan)
        {
            if (Cevrimdisi || puan < 0 || !_ortakliklar.TryGetValue(olay + "\n" + oyuncu, out var ortaklik))
            {
                return SunucuYaniti<OrtakGrubu>.Basarisiz();
            }

            ortaklik.OyuncuPuani += puan;
            return SunucuYaniti<OrtakGrubu>.Tamam(Grup(ortaklik));
        }

        /// <summary>Sunucunun oyuncu için tuttuğu atış puanı (puan doğrulama sunucudadır, GDD 10.6.6).</summary>
        public int OrtakliAtisPuani(string olay, string oyuncu) =>
            _ortakliklar.TryGetValue(olay + "\n" + oyuncu, out var ortaklik) ? ortaklik.OyuncuPuani : 0;

        private Ortaklik OrtakligiAl(string olay, string oyuncu)
        {
            string anahtar = olay + "\n" + oyuncu;
            if (!_ortakliklar.TryGetValue(anahtar, out var ortaklik))
            {
                ortaklik = new Ortaklik();
                _ortakliklar.Add(anahtar, ortaklik);
            }

            return ortaklik;
        }

        private OrtakGrubu Grup(Ortaklik ortaklik)
        {
            var ortaklar = new List<GrupUyesi>();
            for (int i = 0; i < ortaklik.Sira.Count; i++)
            {
                string kimlik = ortaklik.Sira[i];
                double saat = Math.Max(0, (_saat.SimdiUtc - ortaklik.Ortaklar[kimlik]).TotalHours);
                ortaklar.Add(new GrupUyesi(kimlik, (int)Math.Floor(OrtakSaatlikPuan * (1 + (i % 3)) * saat), true));
            }

            return new OrtakGrubu(ortaklar);
        }

        private sealed class Takim
        {
            public Takim(string kimlik, string ad, string simge, bool acik)
            {
                Kimlik = kimlik;
                Ad = ad;
                Simge = simge;
                Acik = acik;
            }

            public string Kimlik { get; }
            public string Ad { get; }
            public string Simge { get; }
            public bool Acik { get; }
            public Dictionary<string, TakimDurumu> Uyeler { get; } = new Dictionary<string, TakimDurumu>(StringComparer.Ordinal);

            /// <summary>Üyelerin katılma sırası.</summary>
            public List<string> Sira { get; } = new List<string>();

            public List<string> Istekler { get; } = new List<string>();
            public List<SohbetMesaji> Sohbet { get; } = new List<SohbetMesaji>();

            /// <summary>Verilen can yardımları: "mesaj:yardım eden"; aynı isteğe iki kez yardım edilmez.</summary>
            public HashSet<string> Yardimlar { get; } = new HashSet<string>(StringComparer.Ordinal);

            public void Ekle(string oyuncu, TakimDurumu rol)
            {
                Uyeler.Add(oyuncu, rol);
                Sira.Add(oyuncu);
            }
        }

        private sealed class Ortaklik
        {
            /// <summary>Ortak → davet anı.</summary>
            public Dictionary<string, DateTime> Ortaklar { get; } = new Dictionary<string, DateTime>(StringComparer.Ordinal);

            public List<string> Sira { get; } = new List<string>();
            public int OyuncuPuani { get; set; }
        }
    }
}
