# CatRoyal — GDD'de olup henüz gerçekleşmeyenler ve uygulama sırası

Tarih: 2026-11-15. Dayanak: `docs/GDD.md` kural numaraları, `Cekirdek/` ve `Unity/Assets/Sunum/` kodu, `Cekirdek.Testler/`.

## Mevcut durum özeti

- **Çekirdek oynanış (GDD 2–7) büyük ölçüde tamam.** Eşleşme, özel taşlar, 10 kombinasyon, düşme/dolgu/karıştırma, engel arketipleri, hedefler, AK-01…AK-15 bayrakları yazılmış. `dotnet test`: 1167/1167 geçiyor; `test_vakalari.md`'deki bütün vakaların testi var.
- **Meta (GDD 9) çekirdekte büyük ölçüde tamam (Aşama 3.1–3.6):** can, cüzdan, envanter, günlük ödül, alan inşası, sezonluk albüm, sezon/haftalık hat ve dönemleri, çarpan ve düzey seçimi, yarı dinamik zorluk ve beceri ölçümü, ek hamle teklifi, mağaza/teklifler, ilerleme kapıları, pencere kuyruğu. Eksik: reklam ve satın alma SDK'ları (3.7), ekranlar (S.10).
- **Olaylar (GDD 10):** ortak altyapı var (Aşama 4): olay yöneticisi, basamaklı toplama şablonu, risk basamakları, çarpan etkinlikleri. Hız yarışı ve harita keşif (Aşama 5), takımlar, takım yarışı ve ortaklı atış (Aşama 6) yazıldı. Eksik: fırlat-birleştir mini oyunu (Aşama 7).
- **Bölüm içeriği (GDD 8):** doğrulayıcı, tanıtım/bonus/kurtarma takvimleri ve paket dağıtımının mantığı var (Aşama 2.1–2.7). İçerik yok: `veri/bolumler` altındaki 6 bölüm mekanik örneğidir ve tasarım kurallarını bilerek karşılamaz.
- **Kalıcılık ve sunucu arayüzü var (Aşama 0):** `Oyuncu` kaydedilip yükleniyor (`OyuncuKaydi`, Unity'de `OyuncuDeposu`). Sunucu tarafı işaretli kısımlar `ISunucu` üzerinden geçiyor; şimdilik yalnızca yerel sahte uygulaması var, gerçek sunucu yok.
- **Sunum:** Unity tarafı OnGUI prototipi. 13 `sunum_` parametresinin yalnızca 4'ü kullanılıyor. Meta ekranlarının çoğu yok.

---

## Aşama 0 — Altyapı (öteki her şey buna dayanır)

**Durum: tamamlandı (2026-11-15).** Kod: `Cekirdek/Meta/OyuncuKaydi.cs`, `Cekirdek/Sunucu/`, `Cekirdek/Meta/Analitik.cs`, `Unity/Assets/Sunum/OyuncuDeposu.cs`. Testler: `KayitTestleri`, `SunucuVeAnalitikTestleri`. Açık kalanlar: Unity tarafı editörde oynatılarak denenmedi; uygulama zorla kapatılırsa süren bölüm kayıp sayılmıyor; sahte sunucu henüz oyun sahnesine bağlı değil (ilk kullanan Aşama 2–4 bağlayacak).

| # | İş | GDD | Not |
|---|---|---|---|
| 0.1 | Oyuncu durumunu kaydet ve yükle: can anı, cüzdan, envanter, sınırsız süreler, yıldız, alan, albüm, hatlar, kapılar, seri, deneme sayaçları | 9.1.5, 9.2.8, 9.3.10 | Çekirdekte JSON serileştirme, Unity'de `persistentDataPath`. Sürüm alanı olsun. |
| 0.2 | Sunucu arayüzü (`ISunucu`) ve yerel sahte uygulaması: zaman, olay takvimi, etiket tablosu, yapılandırma, grup/sıralama | 0.6, 8.8.1a, 9.5.4, 10.0.1–2 | Gerçek sunucu sonra gelir; şimdilik her şey sahte uygulamayla test edilir. |
| 0.3 | Analitik/harcama kaydı çıkışı (`IAnalitik`) | 9.2.5, 9.2.7 | Harcama kaydı var; dışa raporlama yok. |

## Aşama 1 — Çekirdek oynanışı kapat (küçük işler)

**Durum: tamamlandı (2026-11-15).** Testler: `Asama1Testleri`. 1.5'teki kuralların hepsi kodda zaten doğruydu; yalnızca testleri eklendi. Verilen kararlar ve açık kalanlar:

- **1.2 hamle eki:** parametre dosyasında formül yok (11.2.20 "parametre dosyasındaki öneri" diyor ama satırı yok). Ek, 9.11.2'deki gibi yarışma sisteminden hazır değer olarak alınıyor (`BolumAkisi.YarismaHamleEki`); formül yarışma sistemi yazılırken belirlenecek.
- **1.2 beceri:** ölçü, ilk kez oynanan ölçüm bölümlerindeki toplam deneme / kazanılan bölüm. Düzey her kazanımda yeniden hesaplanıyor.
- **1.3 çarpan seçimi:** oyuncu 1 ile aktif çarpan arasında bir düzey seçebiliyor; aktif çarpanı aşamıyor. GDD ayrıntıyı tasarıma bırakıyor (9.10.5, düşük güven); bu en yalın yorum. Unity'de ekranı yok (S.4).
- **1.4:** `OdulPaketi.YarismaSinirsizCaniEkle`; onu kullanacak yarışma ödülleri Aşama 5–6'da.

| # | İş | GDD |
|---|---|---|
| 1.1 | Kazanma ödüllerinin etikete bağlı olduğunu doğrula ve test et (para, hat puanı, olay puanı; yıldız bağımsız) | 7.3.4, 8.8.2 |
| 1.2 | Yarışmalı bölümlerde hamle eki ve beceri eşikleri | 9.11.2, 9.11.3, 11.2.20 |
| 1.3 | Bölüm öncesi çarpan düzeyi seçimi | 9.10.5 |
| 1.4 | Yarışma ödülündeki sınırsız can süresi | 9.1.9 |
| 1.5 | Örtük kalan kurallara doğrulama testleri: oynanabilir hücre (2.1.4), küre+özel taş kombinasyon sayılır (3.5.6), çift renk küresi tür kontrolü ve kombinasyonları (3.6.2, 3.6.5, 3.6.6, 4.4.3), dolum göstergesi ve yedek hakkın kombinasyonu yok (3.7.7, 3.7.8, 3.8.3, 4.1.6), kaskad hasar verir ve hedef sayar (5.5.4), güncelleme sırası (5.8.1) | ilgili kurallar |

## Aşama 2 — Bölüm içeriği ve dağıtım (GDD 8)

**Durum: 2.1–2.7 tamamlandı (2026-11-16); 2.8 ertelendi.** Kod: `Cekirdek/Tasarim/` (takvimler, doğrulayıcı, paketler), `Cekirdek/Meta/Kurtarma.cs`, `Araclar/BolumDogrula/`. Testler: `Asama2Testleri`.

Doğrulayıcıyı çalıştırma (çıkış kodu: 0 hata yok, 1 hata var):

```bash
dotnet run --project Araclar/BolumDogrula -- veri/bolumler docs/parametreler.csv --olculer
```

Klasördeki `bolum_NNN.json` ve `kurtarma_NNN.json` dosyalarını okur. Yol verilmezse depodaki varsayılanları kullanır; `--olculer` bölüm başına ölçü tablosunu da yazar.

Verilen kararlar (GDD'de yazmayanlar):

- **Toleranslar:** paket ortalamaları parametre değerinden %25'ten fazla saparsa uyarı; ortalamalar en az 10 olağan bölümle denetlenir; renk hedefli bölüm payı %25'i aşarsa uyarı. Sabitler `BolumDogrulayici` içinde.
- **Hata / uyarı ayrımı:** tek bölümün sınırları (boyut, renk, hamle bandı, engel türü, hedef tutarlılığı, tanıtım kuralları) hata; ortalamalar, zor bölüm ritmi ve boş geçen tanıtım sırası uyarı.
- **Tanıtım kayması:** takvimdeki tanıtım bölümü bonus ya da kurtarma bölümüne denk gelirse bir sonraki olağan bölüme kayar. Öneri değerlerle 37. bölümde böyle oluyor (tanıtım 38'e kayar).
- **Orta dönemin sonu:** parametre dosyasında yok; tanıtım takvimi orta dönem ritmini sürdürür, geç dönem denetlenmez.
- **Para taşı** tanıtım ritmine girmez; bonus bölümünün mekaniğidir.
- **Zor ritim ve etiket ayrı** (11.2.15): doğrulayıcı ritmi yalnızca hamle farkıyla denetler, etiket tablosuna bakmaz.
- **Kurtarma teması:** ana tema her n. kurtarma bölümündedir (n = 1 / kurtarma_ana_tema_payi, yuvarlanır); öneri değerle 1., 4., 7. … Ana tema uygulamayla gelir, dönüşümlü temalar indirilir.
- **Yedek bölüm:** o ilerleme bölümünün kendi olağan dosyasıdır ve olağan bölüm gibi oynanır. Yani kurtarma bağlı her ilerleme bölümünün de bir olağan dosyası olmalı.
- **Kurtarma ödülü:** etikete bağlı para yerine `kurtarma_kazanma_parasi`; yıldız ve hat puanı olağan bölümdeki gibi.
- **Öne çekme (10.2.6):** hangi bölümlere çekileceği sunucu yapılandırmasından liste olarak gelir (`OneCekilenKurtarmaBolumleri`).

Açık kalanlar:

- **2.8 içerik ertelendi (karar, 2026-11-16):** ilk 50 bölüm ve kurtarma bölümleri elle tasarlanacak; zorluk ayarı önemli olduğu için üretilmeyecek. Tasarlanan bölümler doğrulayıcıdan geçirilir. Yer kararı açık: örnek bölümler testlerde kullanıldığı için gerçek paket ayrı bir klasöre konmalı.
- **Unity bağlı değil:** oyun sahnesi hâlâ `veri/bolumler` klasöründeki dosyaları sırayla oynatıyor; bölüm kitaplığı, kurtarma akışı ve bonus işareti içerik gelince bağlanacak. İndirilen paketin diske yazılması da o zaman (`BolumPaketiVerisi.Yaz` hazır).
- **Harita görseli** yalnızca kimlik olarak var (`harita_NNN`); görselin kendisi S.9.

| # | İş | GDD |
|---|---|---|
| 2.1 | Bölüm doğrulayıcı (editör/CLI aracı): boyut, renk sayısı, hamle bandı ve tabanı, hedef/hamle oranı, engel yoğunluğu, oynanmayan ve boş başlayan hücre payı, engel türü sınırı, hedef adedi ile tahta tutarlılığı | 8.1.2, 8.1.4, 8.1.5, 8.2, 8.3, 8.4.3–8.4.5, 8.5 |
| 2.2 | Tanıtım takvimi denetimi: erken/orta/geç ritim, tanıtım bölümünde engel hem tahtada hem hedefte (istisnalar), araç açılışlarıyla çakışmama | 8.6.1–8.6.7 |
| 2.3 | Bonus bölümü planlayıcı (para taşı, adet 0) ve istatistik dışı tutma | 8.7.1–8.7.3 |
| 2.4 | Zor bölüm ritmi ve etiket tablosunun genişletilmesi | 8.8.3, 8.8.4 |
| 2.5 | Çok ekranlı bölümlerin açılış kapısı | 8.9.4 |
| 2.6 | Bölüm paketleri, paket başına harita görseli, sunucudan yeni ve güncel bölüm indirme | 8.11.1, 8.11.2 |
| 2.7 | **Kurtarma bölümleri (mantık):** "ilerleme bölümü → (kurtarma bölümü, tema)" eşleme tablosu, ritim, tema döngüsü, para ödülü, tema paketi yoksa yedek bölüm, kayıpta can düşmemesi ve tek hedef, deney grubunda öne çekme | 10.2.1–10.2.6, 10.2.8 |
| 2.8 | İçerik üretimi: ilk bölüm paketini (parametre: bolum_paketi_uzunlugu = 50) doğrulayıcıdan geçen bölümlerle doldurmak; ilk kurtarma bölümü dahil (parametre: kurtarma_ilk_bolum = 12) | 8, 10.2.2 |

> **Neden kurtarma bölümleri burada:** ilk kurtarma bölümü 12. bölümde, yani ilk 50'lik paketin içinde geliyor. Sunucu gerektirmez, yalnızca bölüm akışına bağlanır. Aşama 5'e bırakılırsa ilk paket eksik çıkar.

## Aşama 3 — Meta sistemleri tamamla (GDD 9)

**Durum: 3.1–3.6 çekirdekte tamamlandı (2026-11-16); 3.7 açık.** Kod: `Cekirdek/Meta/Pencereler.cs` (pencere kuyruğu, özellik tanıtımları, olay kapıları), `Donemler.cs`, `AlbumSezonu.cs`; `AlanVeAlbum.cs`, `OdulHatti.cs`, `Oyuncu.cs` eklemeleri. Testler: `Asama3Testleri`. Yeni durumların hepsi oyuncu kaydına yazılıyor.

Verilen kararlar (GDD'de yazmayanlar):

- **Pencere sırası:** önce engel tanıtımları (8.6.7), sonra kaynakların kuyruğa giriş sırası; bir kaynağın pencereleri tanıtım → başlangıç → bilgi → ödül alma → otomatik. Açık pencere sonradan gelen yüzünden yerini kaybetmez. Atlanan tanıtım gösterilmiş sayılır.
- **Hangi özelliklerin tanıtımı var:** liste `OzellikTanitimlari` içinde; olayların pencereleri Aşama 4'te olay yöneticisinden gelecek.
- **Eski kayıt:** pencere kaydı olmayan kayıt yüklenince o an açık olan özelliklerin tanıtımları gösterilmiş sayılır.
- **Oynanabilir alan sayısı (9.6.7):** ilk bölümü yayımlanmış alan oynanabilir; en az bir alan var (yukarı yuvarlama).
- **Dönem sonu:** dolmamış hazne "alınmamış ödül" sayılmaz ve verilmez; dolmuş hazne verilir.
- **Dönem değişimi:** dönem, süresi dolunca ya da sunucu başka bir kimlik bildirince biter. Sunucuya ulaşılamasa da süresi dolan dönem biter. Aynı kimlikli dönem yeniden başlatılmaz.
- **Özel basamak tablosu (9.8.11, 9.9.7):** yalnızca yeni dönem başlarken uygulanır; dönem ortasında gelen tablo ilerlemeyi bozmaz.
- **Albüm kademesi ve yıldızı:** sunucu vermezse varsayılan dağılım: setlerin ilk üçte biri sıradan, ikinci üçte biri değerli, kalanı çok değerli; set içinde kart sırası ilerledikçe yıldız artar.
- **Paket ağırlıkları:** parametre dosyasında yok; paket tanımıyla (sunucu) gelir. Kartın ağırlığı = kademe ağırlığı × yıldız ağırlığı.
- **Acıma kuralı (9.7.6):** eksik kart olasılığı = ağırlıkların verdiği olasılık + acima_katsayisi × çift sayısı. Sayılan çiftler son yeni karttan beri art arda gelenlerdir; toplam çift sayısı kullanılsaydı kural birkaç paketten sonra hep %100 olurdu.
- **Albüm sezonu sonu:** açılmamış paketler ve çift yıldızları sezonla birlikte biter. Sezona koşullar sağlanınca kendiliğinden girilir.
- **Rozetli aşama:** büyük ödül alındıktan sonra bir sonraki aşamaya geçilince setler boşalır.

Açık kalanlar:

- **3.7 reklam ve satın alma SDK'ları** bağlanmadı: mağaza hesapları, ürün kimlikleri ve reklam ağı seçimi gerekiyor. Çekirdek tarafı hazır (`ReklamCaniVer`, `ReklamParasiVer`, `ReklamlaEkHamleAl`, `ParaMagazasi.SatinAlimTamamlandi`).
- **Günlük ödül penceresinin ana ekranda kendiliğinden açılması (9.5.2)** kuyruğa bağlanmadı; ana ekran yok (S.10).
- **Alan sandığının takviye, ek araç ve kart paketi içeriği (9.6.5)** hâlâ yalnızca para veriyor.
- **Ödül içerikleri** (hat basamakları, set ödülleri, paket tanımları) sunucudan gelir ve kaydedilmez; sahte sunucuda bunları taşıyan bir tablo henüz yok.
- **Unity:** bu aşamanın hiçbir ekranı yok (S.10).

| # | İş | GDD |
|---|---|---|
| 3.1 | Kapı ayrıntıları: olay simgesi kapısı, kısmi kilit önizlemesi, bölüm öncesinde azami olay sayısı, ikinci olaya katılma süresi | 9.4.3, 9.4.4, 9.4.6, 9.4.7 |
| 3.2 | Pencere/tanıtım kuyruğu (özellik ve olay tanıtımları sırayla, atlanabilir); günlük ödül tanıtımı da buraya | 9.4.5, 9.5.1, 10.0.3 |
| 3.3 | Alan: penceresinin kendiliğinden açılması, görev bildirimi, alan sayısının yayımlanmış bölümden türetilmesi ve "bekliyor" durumu | 9.6.6, 9.6.7 |
| 3.4 | Sezon hattı: 5 adımlı tanıtım, kilometre taşları ve kozmetikler, sezon sonu toplu ödül ve önceki sezon kontrolü, sunucu süresi/deney tablosu | 9.8.1, 9.8.3, 9.8.10–9.8.12 |
| 3.5 | Haftalık hat: başlangıç penceresi, hafta sonu toplu ödül ve son gün bildirimi, iki sürümlü sunucu yapılandırması | 9.9.1, 9.9.6, 9.9.7 |
| 3.6 | Albüm: indirme/açılış kapısı ve sezona giriş süresi, kart kademeleri ve yıldız nadirliği, ağırlıklı paket içeriği, acıma kuralı, sezona göre değişen kart paketleri, sezon sonu | 9.7.1, 9.7.3–9.7.6, 9.7.10, 9.7.11 |
| 3.7 | Reklam ve uygulama içi satın alma bağlantısı (gerçek SDK) | 9.13, 9.14 |

## Aşama 4 — Ortak olay altyapısı (GDD 10.0, 10.9)

**Durum: çekirdekte tamamlandı (2026-11-16).** Kod: `Cekirdek/OlayIskeletleri/OlayYoneticisi.cs`, `OlayIskeleti.cs` eklemeleri; `Oyuncu.Olaylar`. Testler: `Asama4Testleri`. Olayların ilerlemesi ve ödül içerikleri oyuncu kaydına yazılıyor; sunucuya ulaşılamasa da süresi dolan olay biter ve ödülünü verir.

Verilen kararlar (GDD'de yazmayanlar):

- **Aktarılan toplanabilir (10.0.6):** henüz bir basamağı doldurmamış kısım (içinde bulunulan basamağın puanı). Aynı türün bir sonraki olayına geçer; aktarılıp aktarılmayacağı takvim kaydından gelir.
- **Varsayılan toplama tablosu:** parametre dosyasında toplama olayının yalnızca basamak sayısı var. Sunucu tablo vermezse hedefler sezon hattının ilk basamaklarından alınır.
- **Tür açılışı (10.9.1):** n. toplama olayı türü, ilk toplama olayının açılışından n × olay_turu_acilis_araligi bölüm sonra açılır; tür sırası takvim kaydından gelir.
- **Çarpan etkinliği (10.9.6):** ayrı bir olay kurmaz; çarpan bileti kapısı açık oyuncuya etkinliğin bitimine kadar süren bir etkinlik çarpanı verir, bir kez.
- **Bağımlılık (9.4.8):** engelleyen ve ödülü zorla aldıran olaylar takvim kaydında listelenir; engelleyen olay açıkken engellenen olay puan almaz ve penceresi gösterilmez.
- **Risk olayına giriş:** olay, oyuncu girene kadar ilerlemez (durum tablosundaki "olaya girildi"). Var olan risk testi buna göre güncellendi.
- **Çıkış uyarısı (10.5.5):** yalnızca çıkış bir şans hakkı götürecekse istenir.
- **Bölüm öncesi sırası (10.0.9):** bitişi en yakın olay önce.
- **Kapısına hiç ulaşılmamış olayın** bitiş penceresi gösterilmez.

Açık kalanlar:

- **Risk basamak ödülleri:** `risk_basamak_odulleri` serbest metin bir öneri; ayrıştırılmıyor. Ödüller her olay gibi takvim kaydından gelir.
- **"Artan hamleler bitti" bilgisi** olaylara iletiliyor ama onu kullanan olay (hız yarışı, 10.1.6) Aşama 5'te.
- **Olay eşyaları** (bilye, enerji) oyuncuda birikiyor; onları harcayan olaylar Aşama 5–6'da.
- **Unity:** olay ekranı yok (S.11); oyun sahnesi takvimi uygulamıyor.

| # | İş | GDD |
|---|---|---|
| 4.1 | Olay yöneticisi: sunucu takviminden olayları kurar, kazanım/kayıp/artan hamle bilgisini dağıtır, bağımlılık ve kapatmayı uygular | 10.0.1–10.0.5, 7.6.3 |
| 4.2 | Süre bitince toplanabilir aktarımı, alınmamış ödüllerin otomatik verilmesi ve bitiş penceresi, bölüm öncesi gösterim kuralı | 10.0.6–10.0.9 |
| 4.3 | Basamaklı toplama şablonunun parametrelerle kurulması, tür açılış aralığı, hafta sonu çarpan etkinlikleri | 10.9.1, 10.9.4, 10.9.6 |
| 4.4 | Risk basamaklarının kalanları: açılış, ödüller ve azami toplanabilir, çıkış uyarısı, bitiş ve aktarım, kurtarma bölümünde ceza yok | 10.5.1, 10.5.2, 10.5.5–10.5.7 |

## Aşama 5 — Sosyal gerektirmeyen olaylar

**Durum: çekirdekte tamamlandı (2026-11-16).** Kod: `Cekirdek/OlayIskeletleri/HizYarisiOlayi.cs`, `HaritaKesifOlayi.cs`; `OlayYoneticisi.Ilerle`. Testler: `Asama5Testleri`. İki olay da sahte sunucuyla çalışıyor ve durumları (harita ve ödül içerikleri dahil) oyuncu kaydına yazılıyor. Oyuncuya sunucu kimliği eklendi (`Oyuncu.Kimlik`).

Verilen kararlar (GDD'de yazmayanlar):

- **Hız yarışında aşama = katılım:** her katılım bir aşamadır ve kendi grubunu alır; aşama sayısı azami katılımla sınırlanır.
- **Hız yarışı çarpılabilir değil:** puan kazanılan bölüm sayısına dayanır (normal 1, zor ve üstü 2); çarpan uygulanmaz.
- **Hız yarışı ödül sırası:** takvim kaydında [0] birincilik ödülü, [n] n. aşamanın ödülü. `hiz_yarisi_birincilik_odulu` serbest metin bir öneri; ayrıştırılmıyor.
- **Yerel sıralama:** son bilinen liste, oyuncunun güncel puanıyla yeniden sıralanır; eşitlikte oyuncu önde. Kalan hamle toplamı tutuluyor ama eşitlik bozmada kullanılmıyor (10.1.6 düşük güvenli bir öneri ve sıralama sunucuda).
- **Olay biterken süren yarış** yerel sıralamayla kapanır ve ödülü kendiliğinden verilir.
- **Harita keşifte ilerleme:** tamamlanan yaprak görevlerin sayıca oranı.
- **Hat basamakları harcanan enerjiyle dolar** ("harcanan enerji ayrıca tutulur", 10.4.2). Sunucu hedef vermezse hedefler haritanın toplam maliyetine eşit aralıklarla yayılır.
- **Hatlar kademelidir:** yüksek hat düşük hattın ödüllerini de açar; hat yalnızca yükseltilir.
- **Enerji üst sınırı** eldeki enerjiyedir. Ödülden gelen enerji sınırı aşarsa fazlası oyuncuda bekler.
- **"Yeterli süre" (10.4 durumlar):** genel katılma eşiği kullanılır (`ikinci_olay_asgari_sure_dk`).
- **Son şans süresi:** parametre dosyasında yok; sunucu vermezse bitişten bir gün önce başlar.
- **Günlük teklif:** teklifler pahalıdan uyguna sıralı segmentlerdir; alınmayan günün ertesinde bir sonraki segment önerilir, en uygun segmentte kalınır.

Açık kalanlar:

- **Sınırsız sürelerin yarışmalı olay bölümlerinde duraklatılması** (9.3.6, 11.2.18 önerisi) uygulanmadı.
- **Harita keşifin tanıtımı** ortak olay pencereleriyle geliyor; olaya özel ayrı bir akış yok.
- **Unity:** iki olayın da ekranı yok (S.11); oyun sahnesi `OlayYoneticisi.Ilerle`'yi çağırmıyor.

| # | İş | GDD | Neden bu sırada |
|---|---|---|---|
| 5.1 | Kurtarma bölümleri → **Aşama 2.7'ye taşındı.** Burada yalnızca risk basamaklarıyla etkileşimi kalır (4.4'te). | 10.2, 10.5.7 | — |
| 5.2 | Hız yarışı: grup isteği, oyuncuya özel bitiş, olay boyu sınırsız can, aşamalar, son liste denemeleri | 10.1.1–10.1.8 | Sahte sunucuyla çalışır; kalan hamle bilgisi zaten var. |
| 5.3 | Harita keşif: enerji, görev maliyetleri, harita açma, sandık eşikleri, üç hatlı ödül hattı, günlük teklif, son şans | 10.4.1–10.4.9 | En büyük tek oyunculu olay; hat altyapısını (3.4) yeniden kullanır. |

## Aşama 6 — Sosyal katman

**Durum: çekirdekte tamamlandı (2026-11-16); gerçek sunucu ve fizik tahtası yok.** Kod: `Cekirdek/Sunucu/ISosyalSunucu.cs`, `YerelSahteSosyalSunucu.cs`, `Cekirdek/Meta/Takim.cs` (takım üyeliği, gelen kutusu), `Cekirdek/OlayIskeletleri/TakimYarisiOlayi.cs`, `OrtakliAtisOlayi.cs`; `AlbumSezonu` kart gönderme sayaçları. Testler: `Asama6Testleri`. Hepsi sahte sunucuyla çalışıyor; sahte sunucu birden fazla oyuncuyu aynı anda taşıyor.

Verilen kararlar (GDD'de yazmayanlar):

- **Ayrı sosyal arayüz:** takım, sohbet, gelen kutusu, kart ve ortak işlemleri `ISosyalSunucu`'da; grup ve sıralama `ISunucu`'da kaldı. Takım yarışında takım, grup isteğinde katılımcı olarak geçiyor.
- **Sohbet:** canlı bağlantı yerine istekle okunuyor; canlı iletim gerçek sunucunun işi.
- **Takım kurma bedeli** takım kurulunca harcanır; istek başarısızsa para düşmez.
- **Can isteği bekleme süresi** hem istemcide hem sunucuda denetlenir; sunucu reddederse istek gönderilmemiş sayılır.
- **Gönderilen kart gönderenin albümünde kalır.** Çift kartlar çekildiği anda yıldıza dönüştüğü için (9.7.8) elde ayrı kopya yok; oyuncu sahip olduğu bir kartın kopyasını gönderir. Günlük sınır bunu dengeliyor. Kopyanın eksilmesi isteniyorsa albümün kart başına adet tutması gerekir.
- **Başka sezonun kartı** toplanınca boşa gider.
- **Takım yarışı ve hız yarışı çarpanla katlanmaz.**
- **Takım yarışı sonucu** süre dolduktan sonra üç gün oyuncuyu bekler; kapatılmazsa hak edilen ödül kendiliğinden verilir (sunucuya bildirim gitmeden).
- **Sıra ödülü:** sunucu listesi yoksa `takim_yarisi_sira_odulleri` içindeki para; listede karşılığı olmayan sıranın ödülü yok.
- **Ortaklı atışta ödül sırası:** basamak ödülleri, ardından büyük ödül. Büyük ödül maskot son evresine ulaştıysa verilir.
- **Ortaklı atışta son liste** en çok bir saat beklenir.
- **Sahte sunucunun seçimleri** (etkinlik düzeyi eşikleri, önerilen takımlar, lider devri, ortak puan hızı) `YerelSahteSosyalSunucu` başında yazılı.

Açık kalanlar:

- **Fizik atış tahtası (10.6.3)** çekirdekte yok: `AtisYap` atışın puanını sonuç olarak alır. Tahtanın kendisi Unity'de yazılacak (S.11).
- **Gerçek sunucu:** canlı sohbet, öneri algoritması, katılma koşulu, puan doğrulama (10.7.8, 10.6.6).
- **Arkadaş listesi** yok; kart ve ortak daveti verilen oyuncu kimliğine gider.
- **Unity:** hiçbir ekranı yok (S.10, S.11).

| # | İş | GDD |
|---|---|---|
| 6.1 | Takımlar: kurma bedeli, açık/kapalı takım, sohbet, can isteği ve yardım (gelen kutusu), roller, etkinlik düzeyi, ayrılma uyarısı | 10.7.1–10.7.8, 9.1.6, 9.1.8 |
| 6.2 | Takım yarışı: grup, takım puanı, son saatler, asgari katkı, sıra ödülleri | 10.3.1–10.3.8 |
| 6.3 | Albüm kart gönderme ve isteme | 9.7.9 |
| 6.4 | Ortaklı atış: ortak daveti, bilye kaynakları, fizik atış tahtası, maskot basamakları, bilye dönüşümü | 10.6.1–10.6.6 |

## Aşama 7 — Fırlat-birleştir mini oyunu

| # | İş | GDD |
|---|---|---|
| 7.1 | Fizik tabanlı mini oyun, sipariş listesi, kaybetme bölgesi, otomatik atış; kurtarma bölümüne bağlama | 10.8.1–10.8.5, 10.2.7 |

Bağımsız ve büyük bir iş; en sona kalabilir ya da ayrı bir ekiple paralel yürüyebilir.

---

## Paralel hat — Sunum (Unity)

Kod tarafındaki Aşama 0 ve 1 ile **aynı anda** başlar. Kuralı bitmiş ekranlar şimdi tasarlanır; kuralı yazılmamış ekranlar kendi aşamasını bekler, yoksa yeniden çizilir.

### Şimdi (kuralları bitmiş, değişmeyecek)

| # | İş | GDD |
|---|---|---|
| S.1 | Sanat yönü ve görsel kimlik: renk paleti, taş ve özel taş görselleri, kedi ana karakteri, yazı tipi | — |
| S.2 | OnGUI prototipinden kalıcı UI çatısına geçiş (uGUI ya da UI Toolkit) | — |
| S.3 | Oyun ekranı: tahta, hamle ve hedef göstergesi, dolum göstergesi, araç çubuğu, hedef simgesine uçuş | 2–7, 3.7, 3.9.4, 7.1.5 |
| S.4 | Bölüm öncesi ekranı (takviye seçimi, zorluk etiketi), hamle bitti/ek hamle penceresi, sonuç ekranı, artan hamle gösterisi | 7.4, 7.6.2, 8.8.4, 9.3.2, 9.12 |
| S.5 | Kullanılmayan 9 sunum parametresinin animasyonları: bomba başlangıç gecikmesi, renk ışını aralığı, küre tetik beklemesi, çift renk geçişi, küre+çizgi/bomba/güdümlü zamanlamaları, tahta temizleme halkaları, düşme ivmesi | 0.4, 3.3.2, 3.5.4, 4.3.7–4.3.10, 5.1.4 |
| S.6 | Zor bölüm görünümü (bölüm içinde ayrı görünüm) | 8.8.4 |
| S.7 | **Kurtarma bölümü ana teması:** sahne, karakterin mahsur kaldığı durum, geçiş efekti, müzik; Aşama 2.7 ile birlikte 12. bölüme yetişmeli | 10.2.3, 10.2.5 |

### Sonra (kendi aşamasıyla birlikte)

| # | İş | Aşama | GDD |
|---|---|---|---|
| S.8 | Dönüşümlü kurtarma temaları (parametre: kurtarma_tema_sayisi = 5): her biri için sahne, geçiş efekti, müzik; tema paketlerinin indirilmesi | 2.6 sonrası, ikinci kurtarma bölümünden (≈ 37. bölüm) önce | 10.2.3, 10.2.5 |
| S.9 | Bölüm haritası ve paket görselleri | 2.6 | 8.11.2 |
| S.10 | Meta ekranları: can ve gelen kutusu, günlük ödül, alan, albüm, hatların tam ekranı, para mağazası ve teklifler, tanıtım pencereleri | 3 | 9 |
| S.11 | Olay ekranları | 4–7 | 10 |

## Sıralamanın gerekçesi

1. **Aşama 0 önce gelir:** kalıcılık olmadan meta ve olayların hiçbiri gerçekten denenemez; sunucu arayüzü olmadan olay takvimi, grup ve sıralama yazılamaz.
2. **Aşama 1 ve 2** oynanabilir bir dikey dilim verir: tamamlanmış çekirdek, ilk kurtarma bölümü dahil doğrulanmış bir bölüm paketi. Sunum hattının "şimdi" kısmı bu dilimin görünüşünü verir.
3. **Aşama 3** olaylardan önce gelir: olaylar kapılara, pencere kuyruğuna, ödül hatlarına ve albümün kart paketlerine dayanır.
4. **Olaylar bağımlılık sırasıyla gelir:** ortak altyapı → sunucu gerektirmeyen ya da az gerektiren olaylar → takım altyapısı isteyen sosyal olaylar → bağımsız mini oyun.
