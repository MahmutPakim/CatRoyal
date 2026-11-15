# Proje — Oyun Tasarım Dokümanı (GDD)

Bu belge projenin oyun kurallarının tek kaynağıdır. Kod, test ve bölüm üretimi bu belgedeki bölüm numaralarına göre yapılır. Belge bir eşleştirme bulmacası oyununu tarif eder: oyuncu tahtadaki renkli taşları yer değiştirerek eşleşmeler kurar, özel taşlar üretir, engelleri kırar ve sınırlı hamleyle bölüm hedeflerini tamamlar. Bölümlerin dışında can, para, takviye, ödül hattı ve olay sistemlerinden oluşan bir meta katman vardır.

## İçindekiler

0. Belge kuralları
1. Terimler sözlüğü
2. Çekirdek mekanikler
3. Özel taşlar
4. Kombinasyonlar
5. Tahta dinamikleri
6. Engeller
7. Hedefler, hamle sayacı, kazanma ve kaybetme
8. Bölüm tasarım kuralları
9. Meta sistemler
10. Olay iskeletleri
11. Açık Sorular

---

## 0. Belge kuralları

### 0.1 Kural numaraları

Her kuralın bir numarası vardır: bölüm.alt bölüm.sıra, örneğin **2.5.3**. Test vakaları ve kod açıklamaları bu numaralara başvurur.

### 0.2 Güven etiketleri

Her kuralın yanında bir güven etiketi vardır.

| Etiket | Anlamı | Uygulamada |
|---|---|---|
| **yüksek** | Kural kesindir. | Olduğu gibi uygulanır; testleri kesin sonuç bekler. |
| **orta** | Kural büyük olasılıkla doğrudur ama ayrıntısı oyun testinde doğrulanmalıdır. | Uygulanır; ayrıntı değişirse yalnız ilgili test güncellenir. |
| **düşük** | Kural bir tasarım önerisidir. | Varsayılan olarak uygulanır; ekip serbestçe değiştirebilir. |

### 0.3 AYARLANABİLİR KURAL

Doğrulanmamış ve oynanışı belirgin biçimde değiştiren kurallar **AYARLANABİLİR KURAL** olarak işaretlidir. Biçim:

- Her birinin bir kimliği var: **AK-01** … **AK-15**.
- Kuralın iki makul varyantı yazılır: **A** ve **B**. Biri **varsayılan** olarak seçilmiştir.
- Kodda her ayarlanabilir kural için bir **bayrak** bulunur. Bayrak A ya da B değerini alır ve oyun yeniden derlenmeden değiştirilebilmelidir (yapılandırma dosyası ya da geliştirici menüsü).
- Testler varsayılan varyantı bekler. B varyantı için ayrı bir test kümesi bayrak değiştirilerek çalıştırılır.

### 0.4 Parametreler

Sayısal ayar değerleri (fiyatlar, ödüller, süreler, basamak sayıları ve hedefleri, açılış bölümleri, bölüm tasarımı bantları, animasyon zamanlamaları) bu belgeye yazılmaz. Metinde **parametre: ad** biçiminde başvurulur. Değerler parametreler.csv dosyasındadır. O dosyadaki her satırda ad, açıklama, öneri varsayılan, değer aralığı, güven notu ve ilgili GDD bölümü bulunur.

Çekirdek mantık zamanlamadan bağımsızdır: olayların sırası kuraldır, süreleri sunum katmanına aittir (adı "sunum_" ile başlayan parametreler). Hiçbir kuralın sonucu bir süreye, hıza ya da beklemeye bağlı değildir; sıra, "dalga" ve "durulma" kavramlarıyla ve mantıksal düşme adımlarıyla tanımlanır (5.1.7).

Sunum parametrelerinden ikisi bir kuralın görsel tarafına aittir ve sonucu etkilemez: renk küresinin zincir tetiklenmesindeki görsel bekleme (parametre: sunum_renk_kuresi_tetik_beklemesi; sıra AK-09 ile belirlenir) ve çift renk küresinin renk değiştirme animasyonu (parametre: sunum_cift_renk_gecis_suresi; sıra AK-14 ile belirlenir).

Kuralın kendisini tanımlayan geometrik ve sayısal değerler parametre değildir ve metinde yazılıdır. Örnekler: "4 taş", "5×5 alan", "3 güdümlü taş", "4 dik komşu".

### 0.5 Koordinatlar ve gösterim

- Koordinatlar görsel düzendedir: **(satır, sütun)**; satır 1 en üstte, sütun 1 en solda.
- Kodun iç satır ekseninin yönü serbesttir. Yönün sonucu etkilediği tek yer **AK-01**'dir.
- Kaydırma "(a,b) → (c,d)" diye yazılır: oyuncu (a,b)'deki taşı tutup (c,d)'ye kaydırır. (a,b) **çıkılan hücre**, (c,d) **gidilen hücre**dir.

### 0.6 Sunucu tarafı

Bazı sistemlerin mantığı sunucuda çalışır: sıralama, grup eşleştirme, olay takvimi, mağaza fiyatları gibi. Bu kısımlar **"sunucu tarafı, kendi tasarımın"** diye işaretlidir. Belge orada yalnızca istemcinin beklediği veriyi ve önerilen bir başlangıç tasarımını verir.

---

## 1. Terimler sözlüğü

Bu belgede yalnızca aşağıdaki genel adlar kullanılır.

### 1.1 Tahta ve taşlar

| Terim | Tanım | Bölüm |
|---|---|---|
| Tahta | Bölümün oynandığı hücre ızgarası. | 2.1 |
| Oynanmayan hücre | Tahtanın şeklini veren, hiçbir zaman içerik taşımayan hücre. | 2.1 |
| Giriş hücresi | Yeni taşların tahtaya girdiği hücre. | 5.3 |
| Normal taş | Tek renkli, eşleşmeye giren taş. | 2.2 |
| Özel taş | Eşleşme şekliyle oluşan ve etkisi olan taş. | 3 |
| Çizgi taşı (yatay / dikey) | Kendi satırını ya da sütununu temizleyen özel taş. | 3.2 |
| Bomba taşı | Çevresindeki kare alanı patlatan özel taş. | 3.3 |
| Güdümlü taş | Yakın çevresini patlatıp tek bir hedefe uçan özel taş. | 3.4 |
| Renk küresi | Bir rengin bütün taşlarını temizleyen özel taş. | 3.5 |
| Çift renk küresi | Çift renk modunda renk küresinin iki renk temizleyen biçimi. | 3.6 |
| Çift renk modu | Bölüm boyunca bütün renk kürelerini çift renk küresi yapan mod. | 3.6 |
| Kombinasyon | İki özel taşın birbiriyle kaydırılması. | 4 |
| Doğum hücresi | Bir eşleşmeden oluşan özel taşın yerleştiği hücre. | 2.7 |
| Donmuş hücre | Bir etki sürerken geçici olarak taş kabul etmeyen hücre. | 5.1.6 |
| Kaskad | Oyuncu hamlesi olmadan, düşme ve doldurmayla kendiliğinden oluşan eşleşmeler. | 5.5 |
| Yan eşleşme hasarı | Eşleşen hücrelerin dik komşularındaki engellere verilen hasar. | 6.1 |

### 1.2 Bölüm içi yardımlar

| Terim | Tanım | Bölüm |
|---|---|---|
| Başlangıç takviyesi | Bölüm öncesi seçilen ve bölüm başında tahtaya özel taş olarak konan takviye. | 3.9, 9.3 |
| Tek hücre aracı | Oyun içinde tek bir hücreye vuran araç. | 3.9 |
| Satır aracı | Oyun içinde bir satırın tamamına vuran araç. | 3.9 |
| Sütun aracı | Oyun içinde bir sütunun tamamına vuran araç. | 3.9 |
| Karıştırma aracı | Oyun içinde taşları karıştıran araç. | 3.9 |
| Çift renk takviyesi | Bölümü çift renk modunda başlatan takviye. | 3.6, 9.3 |
| Seri takviyesi | Art arda kazanımlarla biriken başlangıç desteği. | 9.3 |
| Dolum göstergesi | Bölüm içinde dolan ve dolunca tahtaya renk küresi fırlatan gösterge. | 3.7 |
| Yedek hamle hakkı | Kazanma serisiyle kazanılan, hamleler azalınca ek hamle veren hak. | 3.8 |
| Ek hamle teklifi | Hamleler bitince para karşılığı sunulan devam etme seçeneği. | 7.4, 9.12 |
| Artan hamle ödülü | Kazanınca kullanılmayan hamlelerin ödüle çevrilmesi. | 7.6 |

### 1.3 Engel arketipleri

| Terim | Tanım | Bölüm |
|---|---|---|
| Katmanlı blok | Düşmeyen, yandaki eşleşmeyle katman kaybeden blok. | 6.2 |
| Sert blok | Düşmeyen, yalnızca özel taş vuruşuyla katman kaybeden blok. | 6.2 |
| Düşen kırılabilir | Normal taş gibi düşen ve kaydırılan, vurulunca kırılan engel. | 6.2 |
| Aşağı inen toplanabilir | Tahtanın alt kenarına indirilince toplanan nesne. | 6.2 |
| Çoklu vuruş bloğu | Aynı olaydan birden çok vuruş alabilen büyük blok. | 6.2 |
| Zemin katmanı | Hücreye bağlı, hücrede patlama olunca hasar alan katman. | 6.2 |
| Yayılan zemin | Eşleşmeler boyunca hücreden hücreye yayılan zemin katmanı. | 6.2, 6.3 |
| Yapışkan kaplama | Taşın üstünü kaplayan, kaydırmayı engelleyen tek katman. | 6.2 |
| Kilit | Taşı yerine bağlayan, yalnızca kendi hücresindeki eşleşmeyle kırılan katman. | 6.2 |
| Örtü | Altındakini gizleyen; düşmeyi ve kaydırmayı engelleyen katman. | 6.2 |
| Üretici | Kırılınca çevresine kaplama ya da zemin bırakan engel. | 6.2, 6.3 |
| Yayılan tehlike | Patlayınca çevresine yayılan engel. | 6.2, 6.3 |
| Hamle sonu hareketli | Her hamlenin sonunda hareket eden ya da durum değiştiren engel. | 6.2, 6.3 |
| Para taşı | Bonus bölümlerinde toplanan düşen nesne. | 6.2, 8.7 |

### 1.4 Meta ve olaylar

| Terim | Tanım | Bölüm |
|---|---|---|
| Para | Tek yumuşak para birimi. | 9.2 |
| Can | Bölüm denemesi hakkı. | 9.1 |
| Yıldız | Bölüm kazanınca alınan, alan inşasında harcanan birim. | 9.6 |
| Alan inşası | Yıldızlarla görev tamamlanan dekorasyon ilerlemesi. | 9.6 |
| Sezonluk albüm | Paketlerden kart çıkan, setleri tamamlanan koleksiyon. | 9.7 |
| Sezon ödül hattı | Aylık sezon boyunca basamaklı ödül hattı; ücretsiz ve premium hat. | 9.8 |
| Haftalık ödül hattı | Haftalık, kısa ödül hattı; ücretsiz ve ücretli hat. | 9.9 |
| Hat puanı | Ödül hatlarında basamak dolduran puan. | 9.8, 9.9 |
| Birikim haznesi | Ödül hattının son basamağından sonra para biriktiren hazne. | 9.8, 9.9 |
| Çarpan bileti | Olay ve hat puanlarını katlayan bilet. | 9.10 |
| Hız yarışı olayı | Küçük grupta kısa sürede en çok bölüm kazanma yarışı. | 10.1 |
| Kurtarma bölümü | Normal bölüm akışına serpiştirilmiş özel temalı bölüm. | 10.2 |
| Takım yarışı olayı | Takımların bölüm kazanımlarıyla birbirine karşı yarıştığı olay. | 10.3 |
| Harita keşif olayı | Bölüm kazanarak enerji toplayıp harita görevlerini açma olayı. | 10.4 |
| Risk basamakları olayı | Kaybettikçe şans hakkı yitirilen basamaklı toplama olayı. | 10.5 |
| Ortaklı atış olayı | Küçük ortak grubuyla, bilye atarak puan toplanan olay. | 10.6 |
| Fırlat-birleştir mini oyunu | Fizik tabanlı, nesne fırlatıp birleştirme mini oyunu. | 10.8 |
| Basamaklı toplama olayı | Bölüm kazanımıyla puan toplanıp basamak ödüllerinin alındığı genel olay şablonu. | 10.9 |

---

## 2. Çekirdek mekanikler

### 2.1 Tahta ve hücreler

| No | Kural | Güven |
|---|---|---|
| 2.1.1 | Tahta satır ve sütunlardan oluşan dikdörtgen bir ızgaradır. Genişlik ve yükseklik bölüm verisinden gelir (bkz. 8.1; parametre: tahta_genislik_standart, tahta_yukseklik_standart). | yüksek |
| 2.1.2 | Her hücrenin en fazla 8 komşusu vardır: 4 **dik** komşu (yukarı, sağ, aşağı, sol) ve 4 **çapraz** komşu (sol-yukarı, sağ-yukarı, sağ-aşağı, sol-aşağı). Bu belgede "dokunan komşu" yalnızca 4 dik komşudur. | yüksek |
| 2.1.3 | **Oynanmayan hücre** tahtanın şeklini belirler. Hiçbir zaman taş, katman ya da giriş taşımaz. | yüksek |
| 2.1.4 | **Oynanabilir hücre** bir taş ya da engel taşır; altında ve üstünde katmanlar bulunabilir (bkz. 6). | yüksek |
| 2.1.5 | **Boş başlayan hücre** oynanabilir bir hücredir ama bölüm başında boştur. Katman ve giriş taşıyabilir. Bölüm başında düşmeyle dolar. | orta |
| 2.1.6 | **Giriş hücreleri** yeni taşların tahtaya girdiği hücrelerdir. Bölüm verisinde hücre başına işaretlenir (bkz. 5.3). | yüksek |
| 2.1.7 | Çok ekranlı bölümlerde tahta dikeyde birden fazla ekrana bölünür. Oyuncu bir anda yalnızca bir ekranı oynar. Satır ve sütun etkileri o ekranın sınırında durur (bkz. 8.9). | yüksek |

### 2.2 Taşlar

| No | Kural | Güven |
|---|---|---|
| 2.2.1 | **Normal taş** tek renklidir. Kaydırılabilir, düşer ve eşleşmeye girer. | yüksek |
| 2.2.2 | Bir bölümde kullanılan renkler bölüm verisinde listelenir (bkz. 8.2; parametre: renk_sayisi_standart). | yüksek |
| 2.2.3 | **Özel taşın** rengi yoktur ve renk eşleşmesine girmez. Kaydırılabilir ve düşer. | orta |
| 2.2.4 | Hücreye bir taşın eşleşme sayılması için taşın yerine oturmuş olması gerekir. Düşmekte olan bir taş, oturana kadar eşleşme aramasına alınmaz. | orta |

### 2.3 Hamle türleri ve geçerlilik

| No | Kural | Güven |
|---|---|---|
| 2.3.1 | **Kaydırma:** oyuncu bir taşı 4 dik komşusundan biriyle yer değiştirir. | yüksek |
| 2.3.2 | Kaynak hücredeki taş kaydırılabilir olmalıdır. Kaydırılamayan bir taş seçilirse "imkânsız hamle" geri bildirimi oynar ve hamle düşülmez. Hangi öğelerin kaydırılamadığı 6.2'deki tabloda yazılıdır. | yüksek |
| 2.3.3 | Kaydırma şu iki durumdan birinde **geçerlidir**: (a) yer değiştirmeden sonra iki hücreden en az biri bir eşleşmeye girer; (b) yer değiştiren iki taştan en az biri özel taştır. | yüksek |
| 2.3.4 | Geçersiz bir kaydırmada taşlar "yanlış hamle" animasyonuyla geri döner. Hamle düşülmez, tahta değişmez. | yüksek |
| 2.3.5 | **Dokunma:** oyuncu bir özel taşa dokunarak onu yerinde etkinleştirir. Bu bir hamledir. | yüksek |
| 2.3.6 | Dokunulan renk küresinin hedefleyecek taşı yoksa hiçbir şey olmaz ve hamle sayılmaz. | orta |
| 2.3.7 | Hamle yalnızca bölüm "oynanıyor" durumundayken ve tahtayı kilitleyen bir işlem sürmüyorken kabul edilir. Kesin olarak kilitleyen işlem dolum göstergesinin fırlatmasıdır (3.7.5). | orta |
| 2.3.9 | Öneri: düşme ve kaskad sürerken de hamle kabul edilir (11.2.27). | düşük |
| 2.3.8 | Hamle sayacı geçerli hamle anında azalır (bkz. 7.2). | yüksek |

### 2.4 Eşleşme tespiti

| No | Kural | Güven |
|---|---|---|
| 2.4.1 | Yatayda ya da dikeyde art arda dizilmiş, aynı renkte en az 3 normal taş bir eşleşmedir. | yüksek |
| 2.4.2 | Aynı renkte 4 normal taşın oluşturduğu 2×2 kare de bir eşleşmedir. | yüksek |
| 2.4.3 | Tespit önerisi: her hücreden başlayan yatay ve dikey 3'lü pencereler ile 2×2 kareler ayrı ayrı bulunur. Uzun sıralar üst üste binen pencerelerden oluşur: 4'lü sırada 2, 5'li sırada 3 pencere. Şekil, pencerelerin nasıl birleştiğine bakılarak belirlenir (2.5). | yüksek |
| 2.4.4 | Oyuncu hamlesinde eşleşme, kaydırılan iki hücreyi içeren gruplarda aranır. Kaskadda bütün tahtada aranır (bkz. 5.5). | yüksek |

### 2.5 Hangi şekil hangi özel taşı üretir

| No | Şekil | Tanım | Sonuç | Güven |
|---|---|---|---|---|
| 2.5.1 | Düz 3'lü | Aynı sırada 3 taş. | Özel taş yok. | yüksek |
| 2.5.2 | Yatay düz 4'lü | Aynı satırda 4 taş. | **Dikey** çizgi taşı | yüksek |
| 2.5.3 | Dikey düz 4'lü | Aynı sütunda 4 taş. | **Yatay** çizgi taşı | yüksek |
| 2.5.4 | Düz 5'li | Aynı sırada 5 taş. | Renk küresi | yüksek |
| 2.5.5 | L şekli | Farklı yönlerdeki iki 3'lü uç hücrelerinde birleşir. | Bomba taşı | yüksek |
| 2.5.6 | T şekli | Bir 3'lünün ortası diğer 3'lünün ucuna değer. | Bomba taşı | yüksek |
| 2.5.7 | Artı şekli | İki 3'lünün ortaları ortaktır. | Bomba taşı | yüksek |
| 2.5.8 | 2×2 kare | Aynı renkte 4 taş, kare dizilim. | Güdümlü taş | yüksek |
| 2.5.9 | 6'lı ya da daha uzun düz sıra | Doğrudan bir şekil değildir. | Renk küresi; artan hücreler için **AK-10**. | düşük |

| No | Kural | Güven |
|---|---|---|
| 2.5.10 | Eşleşmeden doğan çizgi taşının yönü **her zaman eşleşme sırasına diktir**. Bunun istisnası yoktur. "En iyi yöne bakma" yalnızca eşleşme dışı yollardan gelen çizgi taşlarında kullanılır: başlangıç takviyesi (3.9.2), seri takviyesi (9.3.9) ve çizgi taşı üreten engeller. | yüksek |
| 2.5.11 | 2×2 kare aynı anda düz 3'lü adayı da sayılır. Öncelik sırası nedeniyle kare kazanır (2.6.1). | yüksek |

### 2.6 Aynı anda birden fazla şekil: öncelik

| No | Kural | Güven |
|---|---|---|
| 2.6.1 | Bütün aday şekiller şu öncelik sırasına dizilir: **düz 5'li (renk küresi) > L > T ve artı > dikey 4'lü (yatay çizgi) > yatay 4'lü (dikey çizgi) > 2×2 kare (güdümlü) > düz 3'lü.** | yüksek |
| 2.6.2 | Adaylar bu sırayla tek tek işlenir. Bir adayın hücrelerinden **herhangi biri** daha önce kabul edilmiş bir gruba aitse aday **tamamen atlanır**. Kabul edilen grubun hücreleri sahiplenilmiş sayılır. Örnek: 5'li bir sırada aynı hücrelerden çıkan 4'lü adayları elenir; yalnızca renk küresi oluşur. | yüksek |
| 2.6.3 | Birbiriyle örtüşmeyen şekiller aynı hamlede birlikte kabul edilir ve her biri kendi özel taşını üretir. | orta |
| 2.6.4 | L ve T ayrı önceliktedir; L önce gelir. İkisi de bomba ürettiği için fark yalnızca hangi hücrelerin sahiplenildiğinde görülür. | yüksek |
| 2.6.5 | Aynı öncelikteki iki aday çakışırsa yalnızca biri kabul edilir. Hangisinin kabul edileceği **AK-06**'ya bağlıdır. | orta |
| 2.6.6 | Atlanan bir adayın, kabul edilen grupla örtüşmeyen hücrelerinin akıbeti **AK-10**'a bağlıdır. | düşük |

> **AYARLANABİLİR KURAL AK-06 — Eşit öncelikli adayların çakışması** (güven: orta)
> - **Varyant A (varsayılan):** Tarama sırasında önce bulunan aday kabul edilir. Tarama sol-üst hücreden başlar ve satır satır ilerler.
> - **Varyant B:** Tarama sırasında önce bulunan aday kabul edilir. Tarama sol-alt hücreden başlar ve sütun sütun, her sütunda aşağıdan yukarıya ilerler.
> - **Bayrak:** esit_oncelik_secimi (A / B). Örnek durum: kaskadda aynı renkte 2×3 bir blok oluşur ve iki çakışan kare içerir. A'da sol-üstteki kare, B'de sol-alttaki kare kazanır. Durgun bir tahtada tek kaydırma böyle bir blok kuramaz; oyuncu hamlesinde bu çakışma ancak düşme sürerken yapılan hamlede görülür.

> **AYARLANABİLİR KURAL AK-10 — Öncelikte artan hücreler, uzatma ve 6'lı sıra** (güven: düşük)
> - **Varyant A (varsayılan, uzatma açık):** Kabul edilen bir gruba bitişik olan ve aynı renkteki artık hücreler (atlanan adaylardan kalanlar) gruba eklenir ve grupla birlikte patlar. Grubun şekli ve ürettiği özel taş değişmez. 6'lı sırada renk küresi oluşur, altıncı taş da gruba eklenip patlar.
> - **Varyant B (uzatma kapalı):** Yalnızca kabul edilen grubun hücreleri patlar. Artık hücreler kendi başlarına geçerli bir 3'lü oluşturmuyorsa yerinde kalır. 6'lı sırada renk küresi oluşur, öncelikli pencerenin dışında kalan taş yerinde kalır.
> - **Bayrak:** uzatma_adimi (A / B).

### 2.7 Doğum hücresi

| No | Kural | Güven |
|---|---|---|
| 2.7.1 | Bir eşleşmeden oluşan özel taş, grubun **doğum hücresinde** yerleşir. Grubun diğer taşları bu hücreye doğru toplanarak kaybolur. Toplanma sırası doğum hücresine yakından uzağa doğrudur (görsel). | yüksek (yer), orta (görsel sıra) |
| 2.7.2 | **Oyuncu hamlesinde** grup, gidilen hücreye taşınan taşı içeriyorsa özel taş **gidilen hücrede** doğar. Grup, çıkılan hücreye taşınan taşı içeriyorsa **çıkılan hücrede** doğar. Kısacası özel taş, eşleşmeyi tamamlayan taşın indiği yerde oluşur. | yüksek |
| 2.7.3 | Durgun bir tahtada yapılan geçerli bir hamlede, kaydırılan taş 4'lü sırada iç hücrelerden birine, 5'li sırada ortaya, L ve T şekillerinde kesişim hücresine düşer. Uca düşseydi diğer taşlar hamleden önce zaten bir eşleşme olurdu. Bu yüzden oyuncu hamlesindeki bomba her zaman kesişimde doğar. | yüksek |
| 2.7.4 | **Kaskadda** grubun hücreleri zorluk ağırlıklı rastgele bir sırayla dolaşılır (9.11.5); kilitli hücreler atlanır. Dolaşılan hücre şu iki durumdan birinde yeni aday olur: o ana kadarki adaydan daha geç yerine oturduysa ya da AK-01'de seçilen uç yönünde adaydan daha uçtaysa. Dolaşma bitince son aday doğum hücresidir. Pratik sonuç: yatay grupta en son oturan taşın hücresi seçilir, aynı anda oturanlar arasında seçim rastgeledir. Dikey grupta uçtaki hücre sık kazanır. | orta |
| 2.7.5 | Kilitli hücreler (6.2, kilit arketipi) doğum hücresi olamaz. Seçimde atlanırlar. | yüksek |
| 2.7.6 | Düz 3'lü gruplarda doğum hücresi seçilmez. | yüksek |
| 2.7.7 | Kaskadda adaylar arasında seçim yapılırken zorluğa bağlı bir rastgelelik kullanılabilir (bkz. 9.11.5). | orta |

> **AYARLANABİLİR KURAL AK-01 — Kaskadda doğum hücresi ve satır ekseninin yönü** (güven: orta)
> - **Varyant A (varsayılan):** 2.7.4'teki uç yönü **görsel olarak alttır.** Dikey grupta alttaki hücre, en son oturan taşın önüne geçebilir.
> - **Varyant B:** Uç yönü **görsel olarak üsttür.** Dikey grupta üstteki hücre, en son oturan taşın önüne geçebilir.
> - **Bayrak:** kaskad_dogum_hucresi (A / B). Örnek durum: bir sütunda blok halinde düşen dört taş birlikte dikey bir 4'lü kurar; yatay çizgi taşı A'da grubun en alt hücresinde, B'de en üst hücresinde doğar.

---

## 3. Özel taşlar

### 3.1 Ortak kurallar

| No | Kural | Güven |
|---|---|---|
| 3.1.1 | Çizgi, bomba ve güdümlü taş üç yoldan etkinleşir: (a) normal bir taşla kaydırılınca, (b) dokununca, (c) başka bir patlama değince. Renk küresinin yolları 3.5.3'te. | yüksek (a), orta (b, c) |
| 3.1.2 | Bir patlama bir özel taşa değerse o taş kendi etkisini başlatır. Zincirleme patlamalar bu şekilde oluşur. Aynı patlayıcı aynı özel taşı ikinci kez tetiklemez. | orta |
| 3.1.3 | Yeni oluşmuş, henüz kullanılmamış bir özel taş da zincire girer. Aynı hamlede oluşan bir özel taş, aynı hamlenin başka bir patlamasında etkinleşebilir. | orta |
| 3.1.4 | Özel taşlar yandaki eşleşmeden etkilenmez (yan eşleşme hasarı almaz; 6.1.2). | yüksek |
| 3.1.5 | İki özel taş birbiriyle kaydırılırsa tek tek etkinleşmezler; kombinasyon çalışır (bkz. 4). | yüksek |
| 3.1.6 | Bir özel taşın etkisinin değdiği her hücre bir vuruş alır. Vuruş, hücredeki taşı patlatır ya da engele 6. bölümdeki kurallarla hasar verir. | orta |
| 3.1.7 | Etkinleşen özel taş kendi hücresini de patlatır ve tahtadan kalkar. | yüksek |

### 3.2 Çizgi taşı

| No | Kural | Güven |
|---|---|---|
| 3.2.1 | **Yatay** çizgi taşı kendi **satırını**, **dikey** çizgi taşı kendi **sütununu** temizler. Etki bulunduğu hücreden iki yöne doğru ilerler: sola ve sağa ya da yukarı ve aşağı. | yüksek |
| 3.2.2 | Etki tahtanın kenarına kadar gider. Çok ekranlı bölümlerde geçerli ekranın sınırında durur. Engeller yolu kesmez: yoldaki her hücre bir vuruş alır ve etki devam eder. | yüksek |
| 3.2.3 | Etki adım adım ilerler: k. adımda iki kanat da çizgi taşına k hücre uzaklıktaki hücreleri vurur. Yoldaki bir özel taş vurulduğu adımda zincire girer (3.1.2); zincirlerin sırası bu adım sırasıdır. Adımların süresi yalnızca sunumdur (parametre: sunum_cizgi_ilerleme_hizi). | orta |
| 3.2.4 | Kanat oynanmayan bir hücreye gelince durmaz; o hücreyi atlayıp devam eder. | düşük |

### 3.3 Bomba taşı

| No | Kural | Güven |
|---|---|---|
| 3.3.1 | Bomba taşı kendisini merkez alan **5×5 kare** alanı patlatır (yarıçap 2). Şekil karedir, eşkenar dörtgen değildir: merkeze satır ya da sütun farkının büyüğü 2 veya daha az olan bütün hücreler alana girer. | yüksek |
| 3.3.2 | Patlama içten dışa halkalar halinde ilerler: önce merkez, sonra 1. halka, sonra 2. halka. Zincire giren özel taşların sırası halka sırasıdır. Halkalar arasındaki süre yalnızca sunumdur (parametre: sunum_bomba_baslangic_gecikmesi, sunum_bomba_halka_arasi). | yüksek |
| 3.3.3 | Alanın tahta dışında kalan kısmı yok sayılır. Köşedeki bomba yalnızca tahtada kalan 3×3 kısmı patlatır. | yüksek |
| 3.3.4 | Yarıçap yalnızca bomba + bomba kombinasyonunda değişir (4.3.3). | yüksek |

### 3.4 Güdümlü taş

| No | Kural | Güven |
|---|---|---|
| 3.4.1 | Etkinleşince önce kendi hücresini, sonra **4 dik komşusunu** patlatır (artı şekli). | yüksek |
| 3.4.2 | Bir renk küresi tarafından tetiklendiyse (4.3.9) dik komşularını patlatmaz; yalnızca uçar. | yüksek |
| 3.4.3 | Ardından havalanır, **tek bir hedefe** uçar ve inince hedef hücreye vurur. | yüksek (tek hedef), orta (iniş) |
| 3.4.4 | **Hedef seçimi:** tahtadaki adaylara puan verilir ve en yüksek puanlı aday grubu seçilir. Eşit puanlılar arasından rastgele seçilir. Başka bir uçan patlayıcının zaten yöneldiği ve o vuruşla tamamen kırılacak hedefler aday listesinden çıkarılır. Böylece aynı anda uçan güdümlü taşlar aynı hedefe gitmez. | orta |
| 3.4.5 | Adayların puanı **AK-05**'e ve güdümlü hedef puanı parametrelerine göre belirlenir (parametre: gudumlu_puan_hedef_oge, gudumlu_puan_engel, gudumlu_puan_katman_basina, gudumlu_puan_normal_tas). | orta |
| 3.4.6 | Bazı hedef türlerinde (engel tanımında güdümlü önceliği açık olanlarda; 6.4) güdümlü taşlar, tahtada bu türden öğe kaldıkça önce onlara yönlendirilir. Güdümlü önceliği kapalı hedeflerde seçim 3.4.4, 3.4.5 ve AK-05'e göre yapılır. | orta |
| 3.4.7 | Örtü (6.2) altındaki hücreler güdümlü hedefi olamaz. | orta |
| 3.4.8 | **Uçan güdümlü taş sayısı:** tek güdümlü taş → 1. Güdümlü + güdümlü → 3. Güdümlü + çizgi → 1 (çizgiyi taşır). Güdümlü + bomba → 1 (bombayı taşır). Renk küresi + güdümlü → hedef renkteki her taş için 1 (kürenin kendi hücresindeki ek güdümlü taş için **AK-07**). Bomba + bomba → hiç güdümlü yok. | yüksek (sayılar), orta (AK-07) |

> **AYARLANABİLİR KURAL AK-05 — Güdümlü taşın hedef önceliği** (güven: orta)
> - **Varyant A (varsayılan, puan tablosu):** Her adaya puan verilir: bölüm hedefi olan öğe (parametre: gudumlu_puan_hedef_oge) > hedef olmayan engel (parametre: gudumlu_puan_engel) > normal taş (parametre: gudumlu_puan_normal_tas). Engellerde kalan her katman puana eklenir (parametre: gudumlu_puan_katman_basina). En yüksek puanlılar arasından rastgele seçilir.
> - **Varyant B (en yakın hedef):** Yalnızca bölüm hedefi olan öğeler aday olur. Aralarından güdümlü taşa en yakın olan seçilir; eşitlikte rastgele seçilir. Hedef öğe kalmadıysa Varyant A uygulanır.
> - **Bayrak:** gudumlu_hedef_secimi (A / B).

### 3.5 Renk küresi

| No | Kural | Güven |
|---|---|---|
| 3.5.1 | Normal bir taşla kaydırılırsa **o taşın rengindeki** bütün taşları hedefler. | yüksek |
| 3.5.2 | Renk verilmemişse (dokunma, başka patlamanın değmesi, kombinasyon) tahtada **en çok bulunan rengi** seçer. O sırada başka bir renk küresinin hedeflediği renkler sayıma katılmaz. Eşitlik kuralı **AK-08**'dedir. | yüksek (yöntem), orta (eşitlik) |
| 3.5.3 | **Tek başına tetiklenmenin üç yolu:** (1) Normal taşla **kaydırma:** diğer taşın rengi **hemen** hedeflenir. (2) **Dokunma:** en yaygın renk **hemen** hedeflenir. (3) Başka bir patlamanın **değmesi:** renk küresi hemen etkinleşmez; en yaygın renk, sayımın hangi adımda yapılacağını belirleyen **AK-09**'daki sıraya göre seçilir. | yüksek (üç yol), orta (sayım anı) |
| 3.5.4 | Hedef liste ışın gönderimi başlarken sabitlenir. Işınlar listedeki taşlara karışık bir sırayla gider ve her ışın ulaştığı taşı patlatır. Bu vuruşların açtığı boşluklar ışınlar bitince düşmeyle dolar; sonradan gelen taşlar hedeflenmez (çift renk küresinin ikinci rengi hariç, AK-14). Işınlar arasındaki süre yalnızca sunumdur (parametre: sunum_renk_isin_araligi). | orta |
| 3.5.5 | Renk küresi rengi olmayan kaydırılabilir bir öğeyle kaydırılırsa diğer öğenin rengi "yok" sayılır ve en yaygın renk seçilir. | orta |
| 3.5.6 | Renk küresi başka bir özel taşla kaydırılırsa bu tek başına tetiklenme değildir, kombinasyondur (4). | yüksek |
| 3.5.7 | Kaydırma ve dokunma dışındaki bir tetikleyiciyle vurulan renk küresi ayrıca kendi hücresinin altındaki zemin katmanına da vurur. Altında yayılan zemin varsa onu etkinleştirir (6.3.1). | orta |
| 3.5.8 | Kilit altındaki bir hedef taşa giden ışın kilide bir vuruş yapar; taş yerinde kalır. | düşük |

> **AYARLANABİLİR KURAL AK-08 — En yaygın renkte eşitlik** (güven: orta)
> - **Varyant A (varsayılan):** Tarama sırasında ilk görülen renk seçilir. Tarama sol-üst hücreden başlar ve satır satır ilerler. Eşit sayıdaki renklerden hangisinin taşı bu sırada önce gelirse o renk kazanır.
> - **Varyant B:** Eşit sayıdaki renkler arasından rastgele seçilir.
> - **Bayrak:** renk_esitlik_secimi (A / B).

> **AYARLANABİLİR KURAL AK-09 — Zincirle vurulan renk küresinde renk sayımının anı** (güven: orta)
> - **Varyant A (varsayılan):** Küreyi vuran patlama dalgası önce tamamlanır: o dalganın bütün vuruşları uygulanır ve boşalan hücreler düşme ve doldurmayla durulur. Renk sayımı **tahta durulduktan sonra** yapılır; düşüp oturan yeni taşlar sayıma girer. Işınlar sayımdan hemen sonra gönderilir.
> - **Varyant B:** Renk sayımı **tetik anında**, aynı dalganın kalan vuruşlarından ve düşmelerden **önce** yapılır ve renk kilitlenir. Hedef liste de o anda sabitlenir; sonradan gelen taşlar hedeflenmez. Işınlar dalga tamamlanınca gönderilir.
> - **Varsayılan gerekçesi:** Rengin vurulma anında değil, ertelenmiş bir adımda seçildiği biliniyor; ertelemenin tahta durulana kadar sürüp sürmediği bilinmiyor. A buna daha yakın; güven orta.
> - **Bayrak:** renk_kuresi_zincir_secimi (A / B).

### 3.6 Çift renk modu ve çift renk küresi

| No | Kural | Güven |
|---|---|---|
| 3.6.1 | Çift renk küresi ayrı bir eşleşme şekliyle oluşmaz. **Bölüm genelinde bir moddur.** Oyuncu bölüme çift renk takviyesiyle girerse o bölümde oluşan **her** renk küresi çift renk küresi olur. Takviye kapalıysa hepsi normal renk küresidir. Aynı bölümde ikisi birlikte bulunmaz. | yüksek |
| 3.6.2 | Çift renk küresi, kombinasyon ve tür kontrollerinde renk küresi sayılır. | yüksek |
| 3.6.3 | Normal renk küresinden tek farkı **iki renk** temizlemesidir. İlk renk 3.5'teki gibi seçilir. İkinci renk her zaman o anki en yaygın renktir; o sırada hedeflenen renkler sayılmaz. İkinci renge geçerken görünümü renk değiştirir. Toplam en fazla iki renk temizlenir. | orta |
| 3.6.4 | İkinci renge geçişin hangi adımda olacağı **AK-14**'e bağlıdır. | orta |
| 3.6.5 | Tek başına tetiklenme yolları 3.5.3'tekiyle aynıdır. | orta |
| 3.6.6 | Kombinasyonlardaki farklar 4.4'te. | orta |
| 3.6.7 | Çift renk takviyesi oyuncuya kazanma serisi ilerlemesiyle verilir (parametre: cift_renk_seri_esigi, acilis_cift_renk_takviyesi; bkz. 9.3.8). | orta |

> **AYARLANABİLİR KURAL AK-14 — Çift renk küresinin ikinci renge geçişi** (güven: orta)
> - **Varyant A (varsayılan):** İkinci renk, birinci rengin sabitlenmiş hedef listesindeki (3.5.4) **bütün taşlar vurulduktan hemen sonra**, düşmelerin durulması beklenmeden seçilir. Sayım o anki tahtada yapılır; birinci renk ve o sırada hedeflenen renkler sayılmaz.
> - **Varyant B:** İkinci renk, birinci rengin vuruşlarının açtığı **düşme ve kaskad durulduktan sonra** seçilir. Sayım durulmuş tahtada yapılır; birinci renk ve o sırada hedeflenen renkler sayılmaz.
> - **Varsayılan gerekçesi:** Geçişin ilk renk tükenince olduğu biliniyor; geçiş koşulunun ayrıntısı doğrulanmadı. A buna daha yakın; güven orta.
> - **Bayrak:** cift_renk_gecisi (A / B).

### 3.7 Dolum göstergesi

| No | Kural | Güven |
|---|---|---|
| 3.7.1 | Dolum göstergesi tahtadaki bir taş değildir. Bölüm içinde dolan bir göstergedir. Belirli bir ilerleme bölümünden sonra açılır (parametre: acilis_dolum_gostergesi). | yüksek |
| 3.7.2 | Göstergeyi dolduran olaylar: bir çizgi, bomba ya da güdümlü taşın patlaması (türe göre puan), bir renk küresinin vurduğu her taş, her kombinasyon (türe göre puan). Renk küresi + renk küresi ve çift renk küresi + çift renk küresi ayrı puanlanır. Puanlar ve gösterge kapasitesi parametredir (parametre: dolum_kapasitesi, dolum_puan_cizgi, dolum_puan_bomba, dolum_puan_gudumlu, dolum_puan_renk_tas_basina, dolum_puan_kombinasyon, dolum_puan_renk_renk). | yüksek (yapı); değerler kendi tasarımın |
| 3.7.3 | Gösterge dolunca tahtaya **bir renk küresi** fırlatılır. Çift renk modunda fırlatılan taş çift renk küresidir. | yüksek (renk küresi), orta (çift renk) |
| 3.7.4 | **Fırlatma hücresi:** tahtanın hücreleri sol-üstten başlayıp satır satır sıralanır. Bu sıra zorluğa bağlı bir rastgelelikle karıştırılır (9.11.5) ve "taş üretmeye uygun" ilk hücre seçilir. Uygun hücre normal bir hücredir, başka bir işlem için ayrılmamıştır ve içinde uygun bir normal taş vardır ya da hücre boştur ve oraya düşmekte olan bir taş yoktur. | orta |
| 3.7.5 | Fırlatma sürerken oyuncu dokunamaz ve hamle yapamaz. Renk küresi inince dokunma yeniden açılır. | orta |
| 3.7.6 | Bir dolumda tek renk küresi fırlatılır. Gösterge sıfırlanıp yeniden dolmaya başlar. | orta |
| 3.7.7 | İnen renk küresi sıradan bir renk küresidir; 4. bölümdeki kombinasyonlara aynen girer. | orta |
| 3.7.8 | Göstergenin kendisinin kombinasyonu yoktur. | yüksek |

### 3.8 Yedek hamle hakkı

| No | Kural | Güven |
|---|---|---|
| 3.8.1 | Yedek hamle hakkı tahtadaki bir taş değildir. Kazanma serisi ilerlemesiyle kazanılan ve bölüm öncesinde hazırlanan bir haktır (parametre: yedek_hamle_seri_esigi, acilis_yedek_hamle_hakki). | orta |
| 3.8.2 | Hak varken kalan hamle kritik eşiğe inince oyuna ek hamle gönderilir (parametre: yedek_hamle_esigi, yedek_hamle_sayisi). Ek hamle teklifiyle sırası **AK-02**'dedir. Eşik 0 ise hak AK-02 akışında kullanılır. Eşik 0'dan büyükse hak oyun sürerken, kalan hamle bu eşiğe indiği anda bir kez kullanılır ve AK-02'deki yedek hak adımı atlanır. | orta |
| 3.8.3 | Yedek hamle hakkının kombinasyonu yoktur. | yüksek |

### 3.9 Takviyelerin tahtadaki etkisi

| No | Kural | Güven |
|---|---|---|
| 3.9.1 | Başlangıç takviyeleri (çizgi taşı, bomba taşı, renk küresi) bölüm başında tahtaya özel taş olarak konur. | yüksek |
| 3.9.2 | Takviyeyle gelen çizgi taşının yönü sabit değildir. Satırın ve sütunun "temizleme puanı" karşılaştırılır ve yüksek olan yön seçilir. Puan, satırda ya da sütunda bulunan hedef öğe ve engel sayısıdır. | orta (karşılaştırma), düşük (puanın içeriği) |
| 3.9.3 | Takviye taşının konacağı hücre, tahtadaki olası eşleşmelere bakan bir öncelikle seçilir. Öneri: hedeflere en yakın, olası bir eşleşmeyi bozmayan normal taş hücresi. | düşük |
| 3.9.4 | Oyun içi araçlar: **tek hücre aracı** seçilen tek hücreye bir vuruş yapar. **Satır aracı** seçilen satırın, **sütun aracı** seçilen sütunun bütün hücrelerine birer vuruş yapar. **Karıştırma aracı** tahtadaki taşları karıştırır (5.6'daki kurallarla). Araçla seçilemeyen bir hücre (6.4) hedef olarak işaretlenemez; araç adedi düşmez. | orta |
| 3.9.5 | Oyun içi araç kullanmak hamle harcamaz. | düşük |

---

## 4. Kombinasyonlar

### 4.1 Genel kurallar

| No | Kural | Güven |
|---|---|---|
| 4.1.1 | Kombinasyon, iki özel taşın birbiriyle kaydırılmasıdır. Sonucu yalnızca iki taşın türü belirler; hangisinin kaydırıldığı önemsizdir. Dört özel taş türünün kendi aralarındaki **10 çiftin her biri ayrı bir kombinasyondur.** | yüksek |
| 4.1.2 | Kombinasyonun konumla ilgili etkileri **gidilen hücreyi** merkez alır, aksi yazılmadıkça. | yüksek |
| 4.1.3 | Kombinasyon geçerli bir hamledir; hamle sayacı bir azalır. | yüksek |
| 4.1.4 | Yalnızca biri özel taşsa kombinasyon yoktur; özel taş kendini etkinleştirir (3.1.1). | yüksek |
| 4.1.5 | Kombinasyonlarda renk küresinin hedef rengi her zaman **en yaygın renktir** (3.5.2). | yüksek |
| 4.1.6 | Dolum göstergesi ve yedek hamle hakkı tahta taşı olmadığı için kombinasyona girmez. | yüksek |

### 4.2 Kombinasyon matrisi

Matris simetriktir; yalnızca üst üçgen doludur. Ayrıntılar 4.3'te.

| | **Çizgi taşı** | **Bomba taşı** | **Güdümlü taş** | **Renk küresi** |
|---|---|---|---|---|
| **Çizgi taşı** | 4.3.1 Artı: bir satır + bir sütun. *yüksek* | 4.3.2 3 satır + 3 sütun. *yüksek* | 4.3.4 Güdümlü çizgiyi en iyi satıra ya da sütuna taşır. *orta* | 4.3.7 En yaygın renk çizgiye döner. *orta* |
| **Bomba taşı** | | 4.3.3 Tek 9×9 patlama. *yüksek* | 4.3.5 Güdümlü bombayı en iyi bölgeye taşır; orada 5×5. *orta* | 4.3.8 En yaygın renk bombaya döner. *orta* |
| **Güdümlü taş** | | | 4.3.6 3 güdümlü, 3 ayrı hedef. *yüksek (sayı)* | 4.3.9 En yaygın renk güdümlüye döner. *orta* |
| **Renk küresi** | | | | 4.3.10 Bütün tahta temizlenir. *orta* |

### 4.3 Kombinasyonların etkisi

| No | Çift | Etki | Güven |
|---|---|---|---|
| 4.3.1 | Çizgi + çizgi | Gidilen hücrede **bir satır ve bir sütun** birlikte temizlenir (artı). İki çizgi taşı aynı yönde olsa bile sonuç artıdır: gelen taşın yönü diğerinin tersine çevrilir ve ikisi aynı hücreden etkinleşir. | yüksek |
| 4.3.2 | Bomba + çizgi | Gidilen hücre merkezde olacak şekilde **3 sütun ve 3 satır** temizlenir. Merkez sütun ile iki yanındaki sütunlarda dikey çizgi etkisi, merkez satır ile alt ve üst satırlarda yatay çizgi etkisi çalışır. | yüksek |
| 4.3.3 | Bomba + bomba | Bombalardan biri devre dışı kalır, diğerinin yarıçapı 4'e çıkar: **tek bir 9×9** halkalı patlama. Merkez **AK-12**'ye bağlıdır. | yüksek (9×9), orta (merkez) |
| 4.3.4 | Güdümlü + çizgi | 1 güdümlü taş çizgi taşını taşıyarak uçar. Kaydırılan çizgi taşı yataysa tahtadaki **en iyi satıra**, dikeyse **en iyi sütuna** gider ve orada tam satır ya da sütun etkisi çalışır. "En iyi" satır ya da sütun, güdümlü hedef puanlarının (3.4.5) toplamı en yüksek olandır. | orta |
| 4.3.5 | Güdümlü + bomba | 1 güdümlü taş bomba taşını taşıyarak uçar. Hedef, 5×5 alanın güdümlü hedef puanları toplamı en yüksek olan bölgedir. İnince bomba **varsayılan 5×5** patlamasını yapar. | orta |
| 4.3.6 | Güdümlü + güdümlü | Toplam **3 güdümlü taş** havalanır: kaydırılan iki taş ve kombinasyon anında oluşturulan bir ek taş. Kalkış açıları rastgele bir başlangıçtan eşit aralıklıdır (aralarında 120°). Her biri 3.4.4'e göre ayrı tek bir hedefe gider. Kalkıştan önce gidilen hücrede tek bir artı alanı patlar; uçan taşlar ayrıca kendi artı alanlarını patlatmaz. | yüksek (sayı), orta (açılar), düşük (kalkış patlaması) |
| 4.3.7 | Renk küresi + çizgi | En yaygın rengin bütün taşları **çizgi taşına** dönüşür. Yönler dönüşüm sırasına göre bir yatay bir dikey değişir. Renk küresinin kendi hücresine de bir çizgi taşı konur. Dönüşen taşlar dönüşüm sırasıyla birer birer etkinleşir; aralarındaki süre yalnızca sunumdur (parametre: sunum_renk_cizgi_patlama_arasi). | orta |
| 4.3.8 | Renk küresi + bomba | En yaygın rengin bütün taşları **bomba taşına** dönüşür. Bütün dönüşümler tamamlandıktan sonra hepsi aynı adımda etkinleşir; dönüşüm ile patlama arasındaki süre yalnızca sunumdur (parametre: sunum_renk_bomba_beklemesi). | orta |
| 4.3.9 | Renk küresi + güdümlü | En yaygın rengin bütün taşları **güdümlü taşa** dönüşür. Dönüşüm sırasıyla birer birer havalanır ve her biri kendi hedefine gider; hedefini kalkış sırasına göre seçer (3.4.4). Kalkışlar arasındaki süre yalnızca sunumdur (parametre: sunum_renk_gudumlu_kalkis_arasi). Dönüşen güdümlüler kalkıştan önce artı alanlarını patlatmaz (3.4.2). Renk küresinin hücresindeki ek güdümlü taş için **AK-07**. | orta |
| 4.3.10 | Renk küresi + renk küresi | **Bütün tahta** temizlenir. Hücreler merkezden uzaklıklarına göre halkalara ayrılır ve içten dışa, halka sırasıyla patlatılır; halkalar arasındaki süre yalnızca sunumdur (parametre: sunum_tahta_temizleme_halka_arasi). | orta |

> **AYARLANABİLİR KURAL AK-07 — Renk küresi + güdümlü taşta kürenin hücresi** (güven: orta)
> - **Varyant A (varsayılan):** Renk küresinin bulunduğu hücreye de bir güdümlü taş konur ve o da uçar. Uçan güdümlü sayısı = dönüşen taş sayısı + 1.
> - **Varyant B:** Kürenin hücresine güdümlü konmaz. Uçan güdümlü sayısı = dönüşen taş sayısı.
> - **Bayrak:** renk_gudumlu_ek_tas (A / B).

> **AYARLANABİLİR KURAL AK-12 — Bomba + bomba patlamasının merkezi** (güven: orta)
> - **Varyant A (varsayılan):** Merkez **gidilen hücredir.**
> - **Varyant B:** Merkez **çıkılan hücredir**, yani kaydırılan bombanın başlangıç yeri.
> - **Bayrak:** bomba_bomba_merkezi (A / B). Fark yalnızca tahta kenarına yakın kombinasyonlarda görülür.

### 4.4 Çift renk modunda kombinasyonlar

| No | Çift | Etki | Güven |
|---|---|---|---|
| 4.4.1 | Çift renk küresi + çizgi / bomba / güdümlü | 4.3.7–4.3.9'un aynısıdır. Dönüşen taşların kaç renkten seçileceği **AK-15**'e bağlıdır. | orta |
| 4.4.2 | Çift renk küresi + çift renk küresi | 4.3.10'un aynısıdır, ama tahta **iki tur** temizlenir: ilk tur bitince ikinci bir halkalı tur çalışır. | orta |
| 4.4.3 | Çift renk küresi + normal renk küresi | Oluşmaz. İki tür aynı bölümde bulunmaz (3.6.1). | yüksek |

> **AYARLANABİLİR KURAL AK-15 — Çift renk küresi kombinasyonlarında renk sayısı** (güven: orta)
> - **Varyant A (varsayılan):** **İki rengin** taşları dönüşür. Önce en yaygın rengin taşları özel taşa döner. İlk rengin dönüşümü AK-14'e göre bittiği anda, ilk renk ve o sırada hedeflenen renkler sayılmadan tahtada en çok bulunan rengin taşları döner.
> - **Varyant B:** Normal renk küresi kombinasyonu gibi yalnızca **tek renk** dönüşür; çift renk farkı yalnız tek başına tetiklenmede görülür.
> - **Bayrak:** cift_renk_kombinasyon (A / B).

---

## 5. Tahta dinamikleri

### 5.1 Düşme

| No | Kural | Güven |
|---|---|---|
| 5.1.1 | Düşme her güncellemede aynı sırayla hesaplanır: (1) önceki hedefler temizlenir, (2) **düz** düşmeler, (3) **çapraz** kaymalar, (4) taşların hareketi. | yüksek |
| 5.1.2 | **Düz düşme:** tahta sütun sütun ve her sütunda aşağıdan yukarıya taranır. Her boş hücre için tam üstünde düşebilecek ilk taş aranır. Sütunda taş kalmadıysa o sütunun giriş hücresi kullanılır (5.3). Düşmeyi engelleyen bir hücreye ya da donmuş bir hücreye gelince arama durur. | orta |
| 5.1.3 | Düz düşme taşların sütundaki sırasını korur. | yüksek |
| 5.1.4 | Taş hedef hücreye hücre hücre iner. Görsel hız giderek artar (parametre: sunum_dusme_ivmesi, sunum_dusme_azami_hizi) ve oturunca kısa bir esneme ve zıplama oynar; bunlar yalnızca sunumdur. | orta |
| 5.1.7 | **Mantıksal düşme adımı ve oturma sırası:** düşme, mantıkta her adımda bir hücre ilerleyen ayrık adımlarla işlenir. Bir taşın oturma adımı, düşmeye başladığı adım ile düştüğü hücre sayısının toplamıdır. "Daha geç oturan" (2.7.4) ve "tahta durulana kadar" (5.5) bu adımlara göre değerlendirilir; görsel hız sonucu değiştirmez. Aynı adımda oturan taşlar aynı anda oturmuş sayılır. Bir çapraz kayma bir adım sayılır. | orta |
| 5.1.5 | Hangi öğelerin düştüğü ve hangi hücrelerin düşmeyi engellediği 6.2'deki tablodadır. | yüksek |
| 5.1.6 | **Donmuş hücre:** bir etkinin (kaydırma animasyonu, patlama, kombinasyon, oyun içi araç) sürdüğü ve bu sürede taş kabul etmeyen hücredir. Düşme araması donmuş hücrede durur; etki bitince donma kalkar ve düşme sürer. | orta |

### 5.2 Çapraz kayma

| No | Kural | Güven |
|---|---|---|
| 5.2.1 | Çapraz kayma, düz düşme hesabından sonra **hâlâ taş gelmeyecek** hücreler için yapılır. Oyun testinde doğrulanacak; bkz. AK-03. | orta |
| 5.2.2 | Önce **sol-yukarı** çapraz komşudan, o uygun değilse **sağ-yukarı** çapraz komşudan taş çekilir. | yüksek (yön sırası) |
| 5.2.3 | Düz düşmeyle taş gelecek bir hücreye çapraz kayma yapılmaz. Oyun testinde doğrulanacak; bkz. AK-03. | orta |
| 5.2.4 | Çapraz kayma için: bölüm buna izin vermelidir (bölüm verisinde bayrak), hücre çapraz kaymaya kapatılmamış olmalıdır ve aynı hücreyi birden fazla düşen taş hedeflememelidir. | orta |
| 5.2.5 | Bazı öğeler çapraz kayamaz, bazıları çapraz kaymayı engeller (6.2). | yüksek |
| 5.2.6 | Çapraz kaymanın hangi boşluklarda çalıştığı **AK-03**'e bağlıdır. | orta |

> **AYARLANABİLİR KURAL AK-03 — Çapraz kaymanın koşulları** (güven: orta)
> - **Varyant A (varsayılan):** Düz düşmeyle o güncellemede taş gelmeyecek **her** boş hücre çapraz kaymaya adaydır. Önce sol-yukarı, sonra sağ-yukarı denenir.
> - **Varyant B:** Yalnızca üstü **kalıcı olarak kapalı** boş hücreler adaydır: üstünde oynanmayan hücre ya da düşmeyen bir engel bulunan hücreler. Üstünden henüz taş gelmekte olan bir boşluk beklenir. Yön sırası A ile aynıdır.
> - **Bayrak:** capraz_kayma_kosulu (A / B).

### 5.3 Doldurma

| No | Kural | Güven |
|---|---|---|
| 5.3.1 | Yeni taşlar **yalnızca giriş hücrelerinden** girer. Giriş hücreleri bölüm verisinde hücre başına işaretlenir. Genellikle sütunların tepesindedirler ama "her sütunun tepesi" sabit bir kural değildir. Girişi olmayan bir sütunun nasıl dolduğu **AK-04**'e bağlıdır. | yüksek (kaynak), orta (girişsiz sütun) |
| 5.3.2 | Bir bölümde birden fazla giriş türü olabilir. Her türün kendi dolgu seti vardır; örneğin bir sütun yalnızca belirli engeller üretebilir. | orta |
| 5.3.3 | Giriş sütun bazında açılıp kapatılabilir. Çok ekranlı bölümlerde yeni ekrana geçilince o ekranın girişleri açılır. | orta |
| 5.3.4 | Giriş hücresinden çıkacak taş şu öncelik sırasıyla belirlenir: (1) üst ekrandan kalıp sırada bekleyen taş, (2) bölüm verisinde o sütun için önceden tanımlanmış taş, (3) sütuna atanmış hedef seti, (4) bölümün rastgele dolgu setleri (engeller dahil), (5) rastgele renk. | yüksek (parçalar), orta (kesin sıra) |
| 5.3.5 | Rastgele renk, bölüm setlerinden hesaplanan renk oranlarına göre ağırlıklı seçilir. Seçimde zorluğa göre ayarlanan bir rastgele üreteç kullanılır (9.11.5). | orta |
| 5.3.6 | **Ayarlı dolgu:** set yolundan gelen taşlarda zorluk zarı iki düzeyden birini seçer (parametre: ayarli_dolgu_siki_olasiligi). **Sıkı düzeyde** yeni taş **aşağı, sol ve sağ** komşularından hiçbiriyle aynı renkte olamaz. **Normal düzeyde** bu üç yönden birinde iki komşuyla 3'lü tamamlayacak renk yasaklanır. | orta |
| 5.3.7 | Doldurmada eşleşme olmaması garanti edilmez. Düz rastgele renk yolu ayardan geçmez. Bu yüzden doldurma yeni eşleşmeler, yani kaskadlar üretebilir. | orta |

> **AYARLANABİLİR KURAL AK-04 — Girişi olmayan sütunun dolması** (güven: orta)
> - **Varyant A (varsayılan):** Yeni taş yalnızca işaretli giriş hücrelerinden girer. Girişi olmayan bir sütun yalnızca komşu sütunlardan **çapraz kaymayla** dolar (5.2). Çapraz kayma da mümkün değilse boşluk kalır.
> - **Varyant B:** Her sütunun en üstteki oynanabilir hücresi, bölüm verisinde işaretli olmasa da örtük bir giriş hücresidir.
> - **Bayrak:** girissiz_sutun_dolumu (A / B).

### 5.4 Başlangıç tahtası

| No | Kural | Güven |
|---|---|---|
| 5.4.1 | Bölüm verisinde rastgele bırakılan hücrelerin renkleri, setlerden gelen bir izinli renk listesinden seçilir. Seçim zorluğa göre ağırlıklıdır. | yüksek |
| 5.4.2 | **Başlangıçta hazır 3'lü yoktur.** Bir hücreye, dört yönden herhangi birinde aynı renkte iki komşuyla düz 3'lü oluşturacak renk verilmez. | yüksek |
| 5.4.3 | Başlangıçta hazır 2×2 kare de yoktur. Hücreler sırayla doldurulurken her karenin son dolan hücresi kontrol edilir. | orta |
| 5.4.4 | Başlangıç tahtasında **en az bir olası hamle** bulunur. Yoksa tahta yeniden üretilir ya da karıştırılır. | orta |
| 5.4.5 | Başlangıçta tek olası hamle bir renk küresi hamlesiyse tahta karıştırılır. | düşük |
| 5.4.6 | İzinli renk listesinde hiç renk kalmazsa kısıt gevşetilir: kare kısıtı kaldırılır, gerekirse 3'lü kısıtı da kaldırılır. Sonra 5.4.4 kontrolü yapılır. | düşük |

### 5.5 Kaskad

| No | Kural | Güven |
|---|---|---|
| 5.5.1 | Her güncellemede, sürmekte olan bir işlem (düşme, patlama, kombinasyon animasyonu) yoksa tahtadaki **bütün** eşleşmeler bulunur ve patlatılır. Patlama düşmeyi ve doldurmayı, onlar da yeni eşleşmeleri doğurur. Döngü tahta durulana kadar kendiliğinden sürer. | yüksek (döngü), orta (bekleme koşulu) |
| 5.5.2 | Kaskad eşleşmeleri **hamle harcamaz.** Hamle yalnızca oyuncu hamlesinde düşülür (7.2.1). | orta |
| 5.5.3 | Kaskadda özel taş oluşumu 2.5 ve 2.6'ya, doğum hücresi 2.7.4'e uyar. | orta |
| 5.5.4 | Kaskad eşleşmeleri de yan eşleşme hasarı verir ve hedef sayar. | yüksek |

### 5.6 Karıştırma

| No | Kural | Güven |
|---|---|---|
| 5.6.1 | Tahta durulduğunda olası hamle yoksa karıştırma başlar. | orta |
| 5.6.2 | Karıştırma, hazır eşleşme kalmayana ve en az bir olası hamle çıkana kadar tekrarlanır (parametre: karistirma_deneme_sayisi). | orta |
| 5.6.6 | Yer değiştirme denemeleri başarısız olursa bazı taşların **rengi değiştirilerek** olası bir hamle kurulur; renk dağılımı mümkün olduğunca korunur. Bu adım ayrı bir bayrakla kapatılabilir: karistirma_renk_degisimi (açık / kapalı), varsayılan açık. | orta |
| 5.6.3 | Karıştırmada yalnızca kaydırılabilir normal taşlar yer değiştirir. Özel taşlar, engeller ve kilitli ya da kaplamalı taşlar yerinde kalır. | düşük |
| 5.6.4 | Karıştırma hamle harcamaz. | düşük |
| 5.6.5 | Denemeler başarısız olursa ne olacağı **AK-13**'e bağlıdır. | orta |

> **AYARLANABİLİR KURAL AK-13 — Karıştırma başarısızlığı ve "olası hamle yok" akışı** (güven: orta)
> Yer değiştirme denemeleri ve renk değiştirme adımı (5.6.6) başarısız olursa taşlar eski yerlerine döner ve "olası hamle yok" akışı çalışır. Akışın sonucu:
> - **Varyant A (varsayılan):** Bilgi penceresi gösterilir, bölüm kaybı sayılmadan tahta yeniden üretilir ve hamle sayacı korunur. Yeniden üretilen tahtada da olası hamle yoksa üretim en fazla parametre: yeniden_kurma_deneme_sayisi kez tekrarlanır; hepsi başarısızsa bölüm, can düşürülmeden baştan başlatılır.
> - **Varyant B:** Bölüm, ek hamle teklifi sunulmadan **kaybedilir**.
> - **Bayrak:** karistirma_basarisizligi (A / B).

### 5.7 Bir hamlenin tam akışı

1. Oyuncu bir taşı kaydırır (2.3.1).
2. Kaynak taş kaydırılamıyorsa "imkânsız hamle" geri bildirimi oynar; akış biter (2.3.2).
3. İki taş geçici olarak yer değiştirir ve eşleşme aranır (2.4.4).
4. Eşleşme yoksa ve iki taştan hiçbiri özel taş değilse taşlar geri döner; akış biter, hamle düşülmez (2.3.4).
5. Geçerli hamle: hamle sayacı hemen bir azalır (7.2.1). Doğum hücreleri atanır (2.7.2).
6. Durum ayrımı:
   - İki özel taş: kombinasyon çalışır (4).
   - Bir özel taş ve bir normal taş: özel taş kendini etkinleştirir (3.1.1, 3.5.3). Normal taşın yeni yerinde ayrıca eşleşme oluşursa: 11.2.29.
   - Normal eşleşme: gruplar öncelik sırasıyla kabul edilir (2.6). Gruplar patlar, özel taşlar doğum hücrelerinde oluşur (2.5, 2.7). Yan eşleşme hasarı verilir (6.1.2) ve yayılan zemin yayılır (6.3.1).
7. Patlayan ve toplanan öğeler hedef sayaçlarını düşürür (7.1.3). Bütün hedefler tamamsa kazanma başlar (7.3).
8. Tahta durulana kadar her güncellemede şunlar tekrarlanır: düz düşme ve çapraz kayma (5.1, 5.2), giriş hücrelerinden doldurma (5.3), sürmekte olan işlem yoksa kaskad (5.5). Kaskadlar da hasar verir, hedef düşer ve özel taş üretir.
9. Tahta durulunca olası hamle kontrol edilir. Olası hamle yoksa karıştırma yapılır (5.6).
10. Hamle sayacı 0 ise ve hedefler tamamlanmadıysa hamle bitti akışı çalışır (7.4, AK-02).
11. Her hamle sonunda hamle sonu hareketli engeller güncellenir (6.3.4).

### 5.8 Güncelleme sırası

| No | Kural | Güven |
|---|---|---|
| 5.8.1 | Bir güncelleme içinde birimler şu sırayla çalışır: dokunma ve hamle girişi → eşleşme → özel taş ve kombinasyon etkileri → kaskad → düşme ve doldurma → kazanma/kaybetme kontrolü → karıştırma → ipucu. | orta |
| 5.8.2 | Oyuncu belirli bir süre hamle yapmazsa olası bir hamle ipucu olarak gösterilir (parametre: sunum_ipucu_bekleme_suresi). | orta |

---

## 6. Engeller

### 6.1 Genel kurallar

| No | Kural | Güven |
|---|---|---|
| 6.1.1 | **Katman:** katmanlı bir engelin katman sayısı bölüm verisinden gelir. Her geçerli vuruşta katman bir azalır. Son katman vurulunca engel kırılır. Görünüm kalan katmana göre değişir. | yüksek |
| 6.1.2 | **Yan eşleşme hasarı:** bir eşleşme patlarken eşleşen her hücrenin **4 dik komşusu** bir vuruş alır. **Çapraz komşular** vuruş almaz. Vuruşun hasar verip vermediğini engelin kendisi belirler (6.2'deki tabloda "Yan eşleşme hasarı" sütunu). Özel taşlar bu vuruştan etkilenmez (3.1.4). | yüksek |
| 6.1.3 | Bir engel aynı eşleşmenin birden fazla hücresine değiyorsa kaç vuruş alacağı **AK-11**'e bağlıdır. | orta |
| 6.1.4 | **Çoklu vuruş** özelliği olan engeller aynı olaydan birden fazla vuruş alabilir. Örneğin aynı eşleşmenin değdiği her hücre için bir vuruş. | orta |
| 6.1.5 | **Özel taş vuruşu:** çizgi, bomba, güdümlü taş, renk ışını, kombinasyonlar ve oyun içi araçlar etkiledikleri her hücreye bir vuruş yapar. Bir engelin belirli vuruş türlerine bağışıklığı engel tanımında bayrakla belirtilir (6.4). | orta |
| 6.1.6 | **Hücre katmanları** taşa değil hücreye bağlıdır. Taşın altında (zemin) ya da üstünde (kaplama, kilit, örtü) bulunurlar. Hücrelerinde bir patlama olunca vuruş alırlar. Bazıları düşmeyi, kaydırmayı, güdümlü hedeflemeyi ya da görüşü engeller (6.2.2). | yüksek |
| 6.1.7 | Üst katmanı olan bir hücreye gelen vuruş önce üst katmana gider. Üst katman kırılmadan alttaki taş patlamaz. | düşük |
| 6.1.8 | Bir hücredeki taş patlayınca, o hücrenin alt zemin katmanı bir vuruş alır. | yüksek |
| 6.1.9 | Düşmeyen bir engel kırılınca hücresi boşalır ve üstündeki taşlar düşmeye başlar (5.1). | orta |
| 6.1.10 | Hedef öğeler kırılınca ya da toplanma animasyonu bitince kendi hedef sayaçlarını düşürür (7.1.3). | yüksek |

> **AYARLANABİLİR KURAL AK-11 — Aynı eşleşmenin birden fazla hücresine değen engel** (güven: orta)
> - **Varyant A (varsayılan):** Engel bir eşleşme grubundan **en fazla 1 vuruş** alır. Kaç hücresine değdiği önemsizdir. Çoklu vuruş özelliği olan engeller (6.1.4) bu kuralın dışındadır.
> - **Varyant B:** Engel, eşleşme grubunun kendisine değen **her hücresi için 1 vuruş** alır.
> - **Bayrak:** yan_hasar_vurus_sayisi (A / B). Örnek durum: iki katmanlı bir blok, bir L eşleşmesinin iki hücresine aynı anda değer. A'da bir katman kaybeder, B'de kırılır.

### 6.2 Engel arketipleri

Projede her engel bir arketipten türetilir. Görsel tema, isim ve hedef türü serbesttir; davranış arketipin bayraklarına uyar. Tablolarda **E** evet, **–** hayır, **koşullu** ise engel tanımında ayrıca belirtilecek demektir.

#### 6.2.1 Hücreyi dolduran engeller

Bu engeller bir taşın yerini tutar.

| Arketip | Düşer | Kaydırılır | Yan eşleşme hasarı | Özel taş vuruşu | Çoklu vuruş | Katman | Davranış | Güven |
|---|---|---|---|---|---|---|---|---|
| Katmanlı blok | – | – | E | E | – | parametre: katman_azami_katmanli_blok | Her vuruşta bir katman. Erken bölümlerin temel engeli. | yüksek |
| Sert blok | – | – | – | E | – | parametre: katman_azami_sert_blok | Yandaki eşleşmeden etkilenmez. Yalnızca özel taş ve araç vuruşlarıyla katman kaybeder. | yüksek |
| Düşen kırılabilir | E | E | E | E | – | parametre: katman_azami_dusen_kirilabilir | Normal taş gibi düşer ve kaydırılır ama renk eşleşmesine girmez. | yüksek |
| Aşağı inen toplanabilir | E | E | – | – | – | – | Vurulmaz. Sütunundaki toplama hücresine inince toplanır (6.3.5). | orta |
| Çoklu vuruş bloğu | – | – | E | E | E | parametre: katman_azami_coklu_vurus | Aynı olaydan birden fazla vuruş alabilir (6.1.4). | orta |
| Kaydırılamayan düşen blok | E | – | koşullu | E | – | 1 | Düşer ama kaydırılamaz. Çapraz kaymayı engeller. | orta |
| Para taşı | E | E | E | E | – | 1 | Bonus bölümlerinin hedefi (8.7). | yüksek |
| Üretici | – | – | E | E | – | parametre: katman_azami_uretici | Kırılınca çevresine kaplama ya da zemin bırakır (6.3.2). | orta |
| Yayılan tehlike | – | – | E | E | – | 1 | Patlayınca çevresine yayılır (6.3.3). | orta |
| Hamle sonu hareketli | – | – | koşullu | koşullu | – | – | Her hamlenin sonunda hareket eder ya da durum değiştirir (6.3.4). | düşük |

Ortak kurallar:

| No | Kural | Güven |
|---|---|---|
| 6.2.3 | Arketiplerin hiçbiri kendiliğinden çapraz kaymaz. Kaydırılamayan düşen blok dışındakiler çapraz kaymayı engellemez. | yüksek |
| 6.2.4 | Düşmeyen bir engel, üstündeki taşların düz yoldan inmesini keser. Altındaki boşluk ancak çapraz kaymayla dolar (5.2). | orta |

#### 6.2.2 Hücre katmanları

Bu katmanlar hücreye bağlıdır; hücredeki taşla birlikte bulunurlar.

| Arketip | Konum | Kendi hücresindeki eşleşmeden hasar | Yan eşleşme hasarı | Düşmeyi engeller | Kaydırmayı ve dokunmayı engeller | Güdümlü hedefini engeller | Görüşü engeller | Katman | Davranış | Güven |
|---|---|---|---|---|---|---|---|---|---|---|
| Zemin katmanı | alt | E | E | – | – | – | – | parametre: katman_azami_zemin | Hücrede patlama olunca katman kaybeder. Sık bir hedef türü. | yüksek |
| Yayılan zemin | alt | E | E | – | – | – | – | 1 | Eşleşmeler boyunca hücreden hücreye yayılır (6.3.1). | orta |
| Yapışkan kaplama | üst | E | E | – | E | – | E | 1 | Taşı kaplar. Kaplı taş kaydırılamaz. Yandaki eşleşme kaplamayı kaldırır. | yüksek |
| Kilit | üst | E | – | E | E | – | – | parametre: katman_azami_kilit | Taşı yerine bağlar. Kilitli taş kaydırılamaz ve düşmez. Yalnızca kendi hücresindeki eşleşme ya da özel taş vuruşu kilidi kırar. Doğum hücresi olamaz (2.7.5). | yüksek |
| Örtü | üst | E | E | E | E | E | E | parametre: katman_azami_ortu | Altındaki taşları gizler. Vurulunca altındakini açığa çıkarır. | orta |
| Sert kaplama | üst | E | – | – | E | – | E | parametre: katman_azami_sert_kaplama | Yandaki eşleşmeden etkilenmez. Yalnızca kendi hücresindeki patlamayla katman kaybeder. | orta |

Ortak kurallar:

| No | Kural | Güven |
|---|---|---|
| 6.2.5 | Kaplamalı ya da kilitli bir taş kendi hücresinde eşleşmeye girebilir. Eşleşme katmana vuruş yapar (6.1.7). | orta |
| 6.2.6 | Kilitli bir taşı kaynak olarak seçmek "imkânsız hamle"dir ve hamle düşülmez (2.3.2). | yüksek |
| 6.2.7 | Dokunmayı engelleyen bir katmanın altındaki özel taşa dokunmak imkânsız hamledir; özel taş etkinleşmez, hamle düşülmez (2.3.2). | yüksek |

### 6.3 Özel davranışlar

| No | Kural | Güven |
|---|---|---|
| 6.3.1 | **Yayılan zemin:** bir eşleşmenin hücrelerinden en az biri yayılan zeminliyse zemin, zeminli hücrelerden başlayarak eşleşmenin diğer hücrelerine yayılır. Yayılma, zeminli hücreye uzaklık sırasıyla ilerler. Oyuncu hamlesinde kaydırılan taş zeminli bir hücreden geldiyse yayılım o hücreden başlar. Bazı özel taş patlamaları da zemin yayabilir. Öneri: bomba ve çizgi taşı alanlarına yayar; güdümlü taş ve renk ışınları yaymaz. | orta (eşleşmeyle yayılma), düşük (özel taş önerisi) |
| 6.3.2 | **Üretici:** vurulunca hasar alır. Kırıldığı patlama dalgası tamamlandıktan sonra, düşmeler başlamadan çevresindeki uygun hücrelere kaplama ya da zemin katmanı bırakır (parametre: uretici_yayilma_hucre_sayisi). | orta |
| 6.3.3 | **Yayılan tehlike:** patlayan bir tehlike hücresinden çevredeki uygun hücrelere yeni tehlike fırlatılır. Hedef hücreler rastgele seçilir (parametre: tehlike_yayilma_sayisi). | orta |
| 6.3.4 | **Hamle sonu hareketli:** her oyuncu hamlesi ve onun kaskadı bitince hareket eder ya da durum değiştirir. Örnekler: taşları her hamlede sırayla bir hücre ilerleten düzenek, belirli aralıkla açılıp kapanan kapak. | düşük |
| 6.3.5 | **Aşağı inen toplanabilir:** sütunundaki toplama hücresine indiğinde toplanır ve hedef sayacını düşürür. Toplama hücreleri bölüm verisinde işaretlenir; işaret yoksa her sütunun en alt oynanabilir hücresi toplama hücresidir. | orta |

### 6.4 Yeni engel tanımlama sözleşmesi

Her engel tanımı şu alanları taşır. Kod, engel davranışını yalnızca bu alanlardan okur.

| Alan | Değer | Açıklama |
|---|---|---|
| Arketip | 6.2'deki listeden biri | Varsayılan bayrakları verir. |
| Tür | hücreyi dolduran / alt katman / üst katman | 6.2.1 ya da 6.2.2. |
| Düşer, kaydırılır | evet / hayır | |
| Çapraz kayar, çapraz kaymayı engeller | evet / hayır | 5.2. |
| Yan eşleşme hasarı | evet / hayır / koşullu | 6.1.2. |
| Kendi hücresindeki eşleşmeden hasar | evet / hayır | Yalnız katmanlarda. |
| Özel taş vuruşu bağışıklıkları | vuruş türleri listesi | 6.1.5. Boş liste varsayılandır. |
| Çoklu vuruş | evet / hayır | 6.1.4. |
| Azami katman | sayı | 6.1.1. Arketipin katman_azami_* parametresini ezebilir. |
| Düşmeyi, kaydırmayı, güdümlü hedeflemeyi, görüşü engeller | evet / hayır | Yalnız katmanlarda. |
| Hedef türü | hedef sayacı kimliği ya da yok | 7.1. |
| Güdümlü önceliği | evet / hayır | 3.4.6. Varsayılan hayır. |
| Oyun içi araçla seçilebilir | evet / hayır / koşullu | 3.9.4. Varsayılan evet. |
| Güdümlü hedef puanı | sayı | 3.4.5. |
| Hamle sonu davranışı | yok / hareket / durum değiştirme | 6.3.4. |
| Yayılma ya da üretme | yok / zemin yayar / kaplama bırakır / tehlike fırlatır | 6.3. |
| Tanıtım | ilk göründüğü bölüm | 8.6. |

---

## 7. Hedefler, hamle sayacı, kazanma ve kaybetme

### 7.1 Hedefler

| No | Kural | Güven |
|---|---|---|
| 7.1.1 | Her bölümün bir hedef listesi vardır. Her hedefin bir türü, bir adedi ve çok ekranlı bölümlerde bir ekran numarası vardır (8.10). | yüksek |
| 7.1.2 | Hedef türleri: (a) belirli bir engeli kırmak ya da katmanını temizlemek, (b) aşağı inen toplanabilirleri toplamak, (c) belirli bir renkten taş toplamak, (d) "tahtadaki hepsi": adet 0 yazılırsa o türün tahtadaki bütün örnekleri hedeftir. Renk hedefleri seyrek kullanılır; hedeflerin büyük çoğunluğu engel ve toplama hedefidir (8.4). | yüksek |
| 7.1.3 | Hedef sayacı merkezi bir yerden değil, toplanan her öğenin kendi davranışından düşer: öğe kırıldığında, toplanma animasyonu bittiğinde ya da patladığı anda. | yüksek |
| 7.1.4 | Hedef sayacı 0'ın altına inmez. Hedefi tamamlanmış bir türün kalan örnekleri yine kırılabilir. | düşük |
| 7.1.5 | Hedef öğesi kırılan ya da toplanan öğe, ekrandaki hedef simgesine doğru uçar (görsel). | orta |

### 7.2 Hamle sayacı

| No | Kural | Güven |
|---|---|---|
| 7.2.1 | Hamle sayacı **geçerli bir kaydırma anında** ya da **bir özel taşa dokunma anında** bir azalır. Patlamalar ve düşmeler beklenmez. | yüksek |
| 7.2.2 | Geçersiz kaydırma (2.3.4), imkânsız hamle (2.3.2) ve hedefi olmayan renk küresine dokunma (2.3.6) sayaçtan düşmez. | yüksek (geçersiz kaydırma, imkânsız hamle), orta (hedefi olmayan renk küresi) |
| 7.2.3 | Kaskadlar (5.5.2) ve karıştırma (5.6.4) sayaçtan düşmez. | orta |
| 7.2.4 | Oyun içi araç kullanmak sayaçtan düşmez (3.9.5). | düşük |
| 7.2.5 | Hamle sayısı bölüm verisinden gelir (8.3). Yarışmalı bölümlerde değiştirilebilir (9.11.2). | yüksek |

### 7.3 Kazanma

| No | Kural | Güven |
|---|---|---|
| 7.3.1 | Bir hedef sayacı güncellenince bütün hedefler tamamlandıysa **kazanma** başlar. | yüksek |
| 7.3.2 | Kazanma başladıktan sonra oyuncu hamle yapamaz. Tahtada süren patlama ve düşmeler tamamlanır. | orta |
| 7.3.3 | Kazanınca kalan hamleler artan hamle ödülüne gönderilir (7.6). | orta |
| 7.3.4 | Kazanma ödüllerinden para (9.2.2), hat puanı (9.8, 9.9) ve olay puanları (10.9) bölümün zorluk etiketine bağlıdır. Yıldız (9.6.1) etiketten bağımsızdır. | orta |

### 7.4 Hamleler bitince

| No | Kural | Güven |
|---|---|---|
| 7.4.1 | Hamle sayacı 0'a inince, kazanmayı geciktiren bir işlem kalmadıysa ve hedefler tamamlanmadıysa "hamle bitti" akışı başlar. Akışın sırası **AK-02**'ye bağlıdır. | orta |
| 7.4.2 | Ek hamle teklifi bir miktar hamle verir (parametre: ek_hamle_sayisi). Fiyatı art arda alımlarda basamaklanır (9.12). | yüksek (yapı), değerler kendi tasarımın |
| 7.4.3 | Teklif kabul edilirse bölüm aynı tahtayla sürer. Reddedilirse bölüm kaybedilir. | orta |
| 7.4.4 | Kaybedilen bölüm bir can düşürür (9.1.3). | yüksek |

> **AYARLANABİLİR KURAL AK-02 — Hamle bitince akışın sırası** (güven: orta)
> - **Varyant A (varsayılan):** (1) Son hamlenin bütün patlamaları, düşmeleri ve kaskadları bitene kadar beklenir. (2) Bu sürede hedefler tamamlanırsa sonuç **kazanmadır.** (3) Hedef kaldıysa ve oyuncunun yedek hamle hakkı varsa (3.8) önce bu hak kullanılır: ek hamleler eklenir ve oyun sürer; teklif penceresi açılmaz. (4) Hak yoksa ya da kullanıldıysa ve hamle yine bittiyse **ek hamle teklifi** açılır. (5) Teklif reddedilirse **kaybetme** akışı başlar.
> - **Varyant B:** (1)–(2) A ile aynı. (3) Önce ek hamle teklifi açılır. (4) Teklif reddedilirse ve yedek hamle hakkı varsa bu hak son şans olarak kullanılır. (5) Hamle yine biterse kaybetme akışı başlar.
> - **Ortak not (uyarı):** Belirli bir bölüme kadar teklif reddedilince kaybetmeden önce ikinci bir uyarı gösterilir (9.12.5); bu adım iki varyantta da reddin hemen ardından gelir.
> - **Ortak not:** (1). adımda beklenen işlemler kazanmayı geciktiren işlemlerdir: patlama, düşme, kaskad ve hedef öğe toplama animasyonu. Oyun testinde teklif penceresi bunlar bitmeden açılırsa (1). adım bu listeye göre daraltılır. Teklif açıkken hedef tamamlanırsa pencere kapanır ve sonuç kazanmadır.
> - **Bayrak:** hamle_bitti_akisi (A / B).

### 7.5 Olası hamle yok

| No | Kural | Güven |
|---|---|---|
| 7.5.1 | Olası hamle kalmayıp karıştırma da başarısız olursa "olası hamle yok" akışı çalışır. Sonucu **AK-13**'e bağlıdır. | orta |

### 7.6 Artan hamle ödülü

| No | Kural | Güven |
|---|---|---|
| 7.6.1 | Kazanınca kullanılmayan her hamle para ödülüne eklenir (parametre: kalan_hamle_basina_para). | orta |
| 7.6.2 | Görsel gösteri: kalan her hamle için tahtadaki rastgele bir normal taş çizgi taşına dönüşür ve sırayla patlar. Bu patlamalar hedef saymaz ve dolum göstergesini doldurmaz. Oyuncu ekrana dokunarak gösteriyi atlayabilir. | düşük |
| 7.6.3 | Gösteri bitince olaylara "artan hamleler bitti" bilgisi gönderilir. Kalan hamleyi puan olarak kullanan olaylar bu bilgiyi kullanır (10.1.6). | orta |

---

## 8. Bölüm tasarım kuralları

Bu bölüm bölümlerin nasıl tasarlanacağını **desen** olarak verir. Kesin bölüm numaraları, yerleşimler ve tanıtım sırası bölüm tasarımcısının işidir. Sayısal bantlar parametre dosyasındadır.

### 8.1 Tahta boyutu ve şekli

| No | Kural | Güven |
|---|---|---|
| 8.1.1 | Tahta genişliği bütün bölümlerde sabit bir üst sınırı aşmaz. Standart tahta, ekranı dolduran tek bir boyuttadır ve bölümlerin büyük çoğunluğu bu boyutu kullanır (parametre: tahta_genislik_standart, tahta_yukseklik_standart). | yüksek |
| 8.1.2 | Standarttan biraz daha dar ya da kısa tahtalar şekil vermek için kullanılır (parametre: dar_tahta_azami_sutun_eksigi, dar_tahta_azami_satir_eksigi). | yüksek |
| 8.1.3 | Standart yüksekliği aşan uzun tahtalar yalnızca çok ekranlı (kaydırmalı) bölümlerde kullanılır (parametre: kaydirmali_tahta_yukseklik; bkz. 8.9). | yüksek |
| 8.1.4 | Oynanmayan hücreler tahtaya şekil verir. Bölüm başına ortalama payları küçüktür (parametre: oynanmayan_hucre_payi). Hiç oynanmayan hücresi olmayan bölümler de yaygındır. | yüksek |
| 8.1.5 | Boş başlayan hücreler en çok ilk bölümlerde kullanılır: oyuncu düşme ve doldurmayı ilk hamleden önce görür. Sonraki bölümlerde seyrektir (parametre: bos_baslayan_hucre_payi_erken, bos_baslayan_hucre_payi). | yüksek |
| 8.1.6 | Giriş hücreleri tipik olarak her sütunun tepesinde birer tanedir. Farklı giriş türleri az sayıda bölümde kullanılır. | yüksek |

### 8.2 Renk sayısı

| No | Kural | Güven |
|---|---|---|
| 8.2.1 | Neredeyse bütün bölümler aynı standart renk sayısıyla oynanır (parametre: renk_sayisi_standart). | yüksek |
| 8.2.2 | Kolaylaştırılmış bölümlerde renk eksiltilir (parametre: kolay_bolum_azami_renk_eksigi). Standarttan fazla renk yalnızca istisnai bölümlerde kullanılır (parametre: istisna_fazla_renk). | yüksek |
| 8.2.3 | **Zorluk renk sayısıyla ayarlanmaz.** Renk sayısı bölüm numarasıyla değişmez. | orta |

### 8.3 Hamle sayısı ve zorluk eğrisi

| No | Kural | Güven |
|---|---|---|
| 8.3.1 | Hamle sayıları dar bir bantta tutulur (parametre: hamle_bandi_alt, hamle_bandi_ust). Çoğu bölüm bandın alt yarısındadır. | yüksek |
| 8.3.2 | Bölümler bir hamle tabanının altına inmez (parametre: asgari_hamle). Tabanın altındaki değerler yalnızca en ileri bölümlerde ve istisna olarak kullanılır. | orta |
| 8.3.3 | Zorluk iki kolla artırılır: ortalama hamle sayısı azalır (parametre: hamle_ortalama_erken, hamle_ortalama_gec) ve hedef adedi artar. Hamle ortalamasındaki belirgin düşüş tek bir bölümde başlar (parametre: zorluk_kirilma_bolumu); bu bölüm oyunun ilk üçte birlik kısmındadır. | orta |
| 8.3.4 | Kaba zorluk göstergesi "hedef adedi / hamle" oranıdır. Oran erken dönemde düşük, geç dönemde yüksektir (parametre: hedef_hamle_orani_erken, hedef_hamle_orani_gec). Bu oran katman sayısını ve özel taş katkısını hesaba katmaz; bölüm testinde oynanarak doğrulanır. | orta |
| 8.3.5 | Engel yoğunluğu, yani oynanabilir hücrelerin engel taşıyan payı, bölüm numarasından bağımsız olarak kabaca sabit tutulur (parametre: engel_yogunlugu). İlk bölümlerde biraz daha düşüktür. | yüksek |

### 8.4 Hedefler

| No | Kural | Güven |
|---|---|---|
| 8.4.1 | Bölüm başına hedef türü sayısı oyun ilerledikçe artar: başta az sayıda tür (parametre: erken_azami_hedef_turu), ileride ekran sınırına kadar (parametre: ekran_azami_hedef). | yüksek |
| 8.4.2 | Ekran sınırından fazla hedef yalnızca çok ekranlı bölümlerde kullanılır. Hedefler ekranlara dağıtılır (8.9). | yüksek |
| 8.4.3 | Hedeflerin büyük çoğunluğu engel kırma ve toplama hedefidir. Renk hedefleri seyrektir. | yüksek |
| 8.4.4 | Tanıtım bölümünde yeni engel hedeflerden biridir; istisnalar 8.6.2a'da. | yüksek |
| 8.4.5 | Hedef adedi tahtadaki öğe sayısıyla tutarlıdır. Tahtada önceden yerleşik engeller hedefin tamamını karşılamıyorsa eksik kısım girişlerden gelen dolgu setleriyle sağlanır (5.3.4). | orta |

### 8.5 Engel karışımı

| No | Kural | Güven |
|---|---|---|
| 8.5.1 | İlk tanıtılan engeller (katmanlı blok, zemin katmanı, düşen kırılabilir, çoklu vuruş bloğu) zamanla seyrelir ama kaybolmaz. | yüksek |
| 8.5.2 | Her yeni mekanik eski engellerin payını böler. Bir bölümde aynı anda kullanılan engel türü sayısı sınırlı tutulur (parametre: bolum_azami_engel_turu). | orta |

### 8.6 Mekanik tanıtım ritmi

| No | Kural | Güven |
|---|---|---|
| 8.6.1 | **Erken dönemde** yeni engeller düzenli bir aralıkla tanıtılır: her parametre: tanitim_araligi_erken bölümde bir (erken dönemin sonu: parametre: erken_donem_sonu). | yüksek |
| 8.6.2 | Tanıtım bölümünde yeni engel **hem tahtada hem hedefte** yer alır. Bölüm, yeni mekaniği tek başına öğretecek kadar sadedir. | yüksek |
| 8.6.2a | İstisna: hedef olmayan engelleyici katmanlar (kilit, örtü, sert kaplama) ile başka bir hedefe hizmet eden üretici ya da yardımcı engeller yalnızca tahtada tanıtılabilir. Bu durumda hedef, engelin ürettiği ya da açığa çıkardığı öğedir. | orta |
| 8.6.3 | **Orta dönemde** yeni mekanikler bölüm bloklarının **ilk bölümünde** gelir (parametre: tanitim_blogu_orta). **Geç dönemde** tanıtımlar seyrekleşir (parametre: tanitim_araligi_gec). | yüksek |
| 8.6.4 | Oyun içi araçların açıldığı aralıkta yeni engel tanıtımı azaltılır. Araç öğretimi ile engel öğretimi aynı bölümlere denk getirilmez. | orta |
| 8.6.5 | Özel taşlar önce eşleşmeyle ve başlangıç takviyesi olarak tanınır. Tahtaya hazır yerleştirilmiş özel taşlar daha sonraki bölümlerde gelir. | yüksek |
| 8.6.6 | Meta özellik kapıları (9.4) tanıtım ritminin arasına serpiştirilir. Bir kapı yeni engel tanıtımıyla aynı bölüme denk gelebilir. | orta |
| 8.6.7 | Öneri: aynı bölümde ikisi birden açılıyorsa önce engel tanıtımı, sonra özellik tanıtımı gösterilir. Erken dönemde tanıtım dizisi parametre: tanitim_ilk_bolum ile başlar. | düşük |

### 8.7 Bonus bölümleri

| No | Kural | Güven |
|---|---|---|
| 8.7.1 | Düzenli aralıklarla tek hedefli bir **bonus bölümü** gelir: "tahtadaki bütün para taşlarını topla" (adet 0 = hepsi). Aralık erken dönemde kısa, sonra uzundur (parametre: bonus_araligi_erken, bonus_araligi_gec, bonus_gec_donem_baslangici). | yüksek |
| 8.7.2 | Bonus bölümleri zorluk eğrisinin dışındadır. İstatistiklerde ve zorluk ayarında hesaba katılmaz. | yüksek |
| 8.7.3 | Bonus bölümleri bir açılış bölümünden sonra başlar (parametre: acilis_bonus_bolumleri). | yüksek |

### 8.8 Zorluk etiketleri

| No | Kural | Güven |
|---|---|---|
| 8.8.1 | Her bölümün bir zorluk etiketi vardır: **normal, zor, çok zor, aşırı zor.** | yüksek |
| 8.8.1a | Etiket bölüm dosyasından ayrı bir tabloda tutulur ve sunucu değiştirebilir; istemcideki varsayılan tablo bölüm paketiyle gelir. | kendi tasarımın |
| 8.8.2 | Etiket ödülleri belirler: kazanma parası (9.2.2) ve hat puanı (9.8.4). | yüksek |
| 8.8.3 | Bloklar içinde düzenli bir **zor bölüm ritmi** vardır: her bloğun son bölümü diğerlerinden daha az hamle verir (parametre: zor_bolum_araligi, zor_bolum_hamle_farki). | orta |
| 8.8.4 | Etiket arayüzde gösterilir: bölüm öncesi ekranda ve bölüm içinde zor bölümlerin ayrı bir görünümü vardır. | orta |

### 8.9 Çok ekranlı bölümler

| No | Kural | Güven |
|---|---|---|
| 8.9.1 | Çok ekranlı bölümlerde tahta dikeyde birden fazla ekrandan oluşur. Hedefler ekranlara dağıtılır. Bir ekranın hedefleri tamamlanınca bir sonraki ekrana geçilir. | orta |
| 8.9.2 | Üst ekrandan kalan taşlar alt ekranın giriş hücresi sırasına eklenir (5.3.4 adım 1). Yeni ekrana geçince o ekranın girişleri açılır (5.3.3). | orta |
| 8.9.3 | Satır ve sütun etkileri geçerli ekranın sınırında durur (2.1.7). | yüksek |
| 8.9.4 | Çok ekranlı bölümler oyunun ilerisinde tanıtılır (parametre: acilis_cok_ekranli). | yüksek |

### 8.10 Bölüm verisi

Bölüm dosyası şu bilgileri taşır. Biçim ve alan adları projenin kendi kararıdır.

| Bilgi | Açıklama |
|---|---|
| Kimlik ve sürüm | Bölüm numarası ve düzenleme sürümü. Bölümler yayından sonra güncellenebilir. |
| Boyut | Genişlik, yükseklik. |
| Hücre içerikleri | Hücre başına içerik kodu: oynanmayan, boş başlayan, rastgele renk, sabit renk (göreli renk kimliği), özel taş, engel (arketip ve katman). |
| Hücre katmanları | Hücre başına alt ve üst katmanlar ve katman sayıları. |
| Giriş hücreleri | Hücre başına giriş türü. |
| Toplama hücreleri | Aşağı inen toplanabilirlerin toplandığı hücreler (6.3.5). |
| Önceden tanımlı dolgular | Sütun başına giriş sırası (5.3.4 adım 2). |
| Dolgu setleri | Rastgele dolgu setleri, renk oranları ve sütuna özel hedef setleri. |
| Renk listesi | Bölümde kullanılan renkler. |
| Hedefler | Tür, adet (0 = hepsi) ve ekran numarası. |
| Hamle sayısı | 8.3. |
| Zorluk etiketi | Bölüm dosyasında değil, ayrı etiket tablosunda (8.8.1a). |
| Bayraklar | Çok ekranlı, çapraz kayma izinli, tahtada delik var. |
| Ek sınırlar | İsteğe bağlı sınır listesi (tür ve adet). Örneğin süreli bölümde süre. |

### 8.11 Bölüm dağıtımı

| No | Kural | Güven |
|---|---|---|
| 8.11.1 | Bölümler uygulama içinde paketlenir. Sunucu yeni bölümler ekleyebilir ve var olanları güncelleyebilir. Yayımlanmış en yüksek bölüm sunucudan gelir. | sunucu tarafı, kendi tasarımın |
| 8.11.2 | Bölümler sabit uzunlukta bölüm paketleri halinde gruplanır (parametre: bolum_paketi_uzunlugu). Her paketin kendi harita görseli vardır; yeni bölümler paket paket indirilir. | yüksek |

---

## 9. Meta sistemler

### 9.1 Can

**Amaç:** bölüm denemelerini sınırlamak; oyuncuyu beklemeye, takımından yardım istemeye ya da satın almaya yönlendirmek.

| No | Kural | Güven |
|---|---|---|
| 9.1.1 | Oyuncunun en fazla parametre: azami_can kadar canı olur. Sezon ödül hattında premium hat sahibi olan oyuncunun azami canı daha yüksektir (parametre: azami_can_premium). | yüksek |
| 9.1.2 | Bölüme girmek için en az bir can gerekir. Can yoksa bölüm başlatılamaz ve yeniden başlatılamaz. | yüksek |
| 9.1.3 | Kaybedilen bölüm bir can düşürür. Başlamış bir bölümden çıkmak da kayıp sayılır. | yüksek (kayıp), düşük (çıkış) |
| 9.1.4 | Can tam değilse zamanla birer birer dolar (parametre: can_dolum_suresi_dk). Ekranda bir sonraki cana kalan süre gösterilir. | yüksek |
| 9.1.5 | Can sayısı ayrıca saklanmaz. Saklanan değer "canların tamamen dolacağı an"dır. Mevcut can bu andan ve dolum süresinden hesaplanır. Cihaz saati geri alınırsa değer düzeltilir. | orta |
| 9.1.6 | Can bitince oyuncunun seçenekleri: beklemek, can dolum teklifini almak (parametre: can_dolum_teklifi_fiyati), reklam izlemek (9.13), takımından can istemek (10.7), gelen kutusundaki hediye canları toplamak. | yüksek |
| 9.1.7 | **Sınırsız can:** süre aktifken can düşmez; sayaç yerine kalan süre gösterilir. Süreler dakika ya da saniye olarak eklenir ve üst üste toplanır. Sınırsız can süresi duraklatılabilir; duraklatılmış süre ayrı tutulur. | orta |
| 9.1.8 | Hediye canlar gelen kutusuna düşer. Gelen kutusu en fazla parametre: gelen_kutusu_azami mesaj alır. Gelen kutusundan toplanan can sayıya eklenir; azami can aşılmaz. | yüksek (gelen kutusu sınırı), düşük (azami aşılmaz) |
| 9.1.9 | Sıralamalı yarışma ödüllerinde (yarışmalı bölümler ve sıralamalı yarışma olayları) verilen sınırsız can süresi parametre: hediye_sinirsiz_can_dk kadardır. Diğer ödüllerdeki süreler kendi ödül tablolarında yazılır. | yüksek (yarışma ödülü), kendi tasarımın (diğerleri) |
| 9.1.10 | Can tüketimi sunucuya bildirilir ve can durumu eşitlenir. Takımdan can isteme beklemesi ve kötüye kullanımda yardım men süresi sunucudadır (parametre: can_istek_araligi_saat). | sunucu tarafı, kendi tasarımın |

**Durum tablosu:**

| Durum | Olay | Yeni durum |
|---|---|---|
| Dolu | Bölüm kaybı | Eksik |
| Eksik | Dolum süresi doldu, son can doldu | Dolu |
| Eksik | Hediye can ya da reklamla son can doldu | Dolu |
| Eksik | Bölüm kaybı, son can gitti | Boş |
| Boş | Dolum süresi doldu / hediye can / reklam | Eksik |
| Boş, Eksik | Can dolum teklifi alındı | Dolu |
| Herhangi biri | Sınırsız can ödülü | Sınırsız |
| Sınırsız | Süre bitti | Dolu |

### 9.2 Para

| No | Kural | Güven |
|---|---|---|
| 9.2.1 | Oyunda tek bir yumuşak para birimi vardır. Yeni oyuncu bir başlangıç parasıyla başlar (parametre: baslangic_parasi). | yüksek |
| 9.2.2 | Bölüm kazanınca zorluk etiketine göre para verilir (parametre: kazanma_parasi_normal, kazanma_parasi_zor, kazanma_parasi_cok_zor, kazanma_parasi_asiri_zor). Kalan her hamle için ayrıca para eklenir (parametre: kalan_hamle_basina_para; 7.6.1). Kesirli toplam aşağı yuvarlanır. | orta (yapı), düşük (yuvarlama) |
| 9.2.3 | Diğer kaynaklar: olay ödülleri, günlük ödül, ödül hatları, alan sandığı, albüm set ödülleri, takım arkadaşına can yardımı (parametre: can_yardim_odulu), hesap bağlama (parametre: hesap_baglama_odulu), kurtarma bölümleri (10.2.4) ve gerçek parayla para paketleri (sunucu tarafı, kendi tasarımın). | yüksek |
| 9.2.4 | Harcama yerleri: takviye satın alma (9.3), ek hamle teklifi (9.12), takım kurma (parametre: takim_kurma_bedeli), çarpan bileti (parametre: carpan_bileti_fiyati), mağaza ürünleri (9.14). | yüksek |
| 9.2.5 | Her harcama bir **harcama kaydı** taşır: miktar ve harcamanın adı. Harcama analitiğe adıyla birlikte raporlanır. Önce bakiye kontrol edilir; yetersiz bakiyede harcama başarısız olur ve para mağazası önerilir. | yüksek |
| 9.2.6 | Bütün satın almalar ve ödüller aynı **ödül paketi** yapısını kullanır. Paket şunları içerebilir: para, can, sınırsız can süresi, takviyeler, takviye başına sınırsız süreler, çarpanlar, olay eşyaları, albüm eşyaları, kozmetikler. | yüksek |
| 9.2.7 | Para değişince ve harcanınca ayrı olaylar tetiklenir. Arayüz ve görevler bu olayları dinler. Para kazanımı, bayrağa bağlı olarak görev ilerlemesi sayabilir. | yüksek |
| 9.2.8 | Bakiye sunucuyla eşitlenir; sunucu değeri yerel değerin üzerine yazabilir. | orta |

**Tek harcamanın akışı:** istek → bakiye kontrolü → (yetersiz: para mağazası → satın alma başarılıysa isteğe dön, vazgeçildiyse bitir) → harcandı → ödül paketi eklendi → sunucuyla eşitlendi.

### 9.3 Takviyeler ve envanter

| No | Kural | Güven |
|---|---|---|
| 9.3.1 | **Başlangıç takviyeleri:** çizgi taşı, bomba taşı, renk küresi (3.9.1). **Oyun içi araçlar:** tek hücre aracı, satır aracı, sütun aracı, karıştırma aracı (3.9.4). Güdümlü taş takviyesi satılmaz; yalnızca ödül olarak gelir. Envanterde ayrı tutulur ve başlangıç takviyesi gibi bölüm öncesinde seçilir; seçilirse bölüm başında tahtaya bir güdümlü taş konur (3.9.3). | yüksek (türler), orta (satılmaması), düşük (kullanımı) |
| 9.3.2 | Bölüm öncesi ekranında her başlangıç takviyesi ayrı ayrı seçilir; üçü birden seçilebilir. Seçim oturum boyunca hatırlanır. | yüksek |
| 9.3.3 | Seçilen takviye envanterde yoksa satın alma penceresi açılır ve para harcanır. | yüksek |
| 9.3.4 | Oyun içi araçlar bölüm sırasında araç çubuğundan kullanılır; adet bir azalır. | yüksek |
| 9.3.5 | Açılış: başlangıç takviyeleri ve her araç ayrı bir bölümde açılır (parametre: acilis_baslangic_takviyeleri, acilis_tek_hucre_araci, acilis_satir_araci, acilis_sutun_araci, acilis_karistirma_araci). Açılmadan önce ilgili yuva kilitli görünür. Araçlar birer birer ve aralıklı açılır (8.6.4). | yüksek |
| 9.3.6 | Her takviye türünün ayrı bir **sınırsız süresi** vardır. Süreler saniye olarak eklenir ve üst üste biner. Sınırsız süre aktifken adet düşmez. Süre duraklatılabilir; duraklatılmış süre ayrı tutulur. Öneri: yarışmalı olayların bölümlerinde sınırsız süreler duraklatılır. | orta (yapı), düşük (duraklatma durumu) |
| 9.3.7 | **Fiyat:** her takviyenin bir fiyatı vardır (parametre: fiyat_cizgi, fiyat_bomba, fiyat_renk_kuresi, fiyat_tek_hucre, fiyat_satir, fiyat_sutun, fiyat_karistirma). İki fiyat düzeyi vardır: normal ve yüksek (parametre: yuksek_fiyat_carpani). Oyuncunun düzeyini deney grubu belirler. | yüksek (yapı), sunucu tarafı (grup) |
| 9.3.8 | **Çift renk takviyesi** (3.6) başlangıç takviyeleri yerine geçen güçlü bir moddur. Açılır (parametre: acilis_cift_renk_takviyesi), kazanma serisiyle kazanılır (parametre: cift_renk_seri_esigi). Bir deney grubunda kapı ileriye alınabilir. Öneri: çift renk takviyesi açıkken bölüm öncesinde seçilen başlangıç takviyeleri birleşir ve bölüm başında tahtaya tek bir çift renk küresi konur (11.2.28). | orta (mod), düşük (birleşme) |
| 9.3.9 | **Seri takviyesi:** art arda kazanımlarda kademe kazanan bir başlangıç desteğidir (parametre: acilis_seri_takviyesi, seri_takviyesi_azami). Her kademe bölüm başında tahtaya bir çizgi taşı koyar; yönü 3.9.2'deki gibi seçilir. Kayıpta sıfırlanır. | düşük |
| 9.3.10 | Envanterde takviye başına adet tutulur (parametre: takviye_azami_adet). | orta |
| 9.3.11 | Satın alma ve ödüller aynı envanter yolunu kullanır (9.2.6). | yüksek |

**Başlangıç takviyesinin durumları:** kilitli → (açılış bölümü) boş → (satın alma / ödül) var → (bölüm öncesi seçildi) seçili → (bölüm başladı) adet bir azaldı: var ya da boş. Herhangi bir durumdan sınırsız süre ödülüyle "sınırsız"a geçilir; süre bitince adet varsa "var", yoksa "boş" olur.

### 9.4 İlerleme kapıları

**Amaç:** yeni oyuncuyu sistemlerle tek tek tanıştırmak ve erken bölümlerde ekranı sade tutmak.

| No | Kural | Güven |
|---|---|---|
| 9.4.1 | Her meta özelliğin ve olayın bir açılış bölümü vardır. Kapılar istemcide sabittir. Bir deney grubu bazı kapıları öne çekebilir. | yüksek |
| 9.4.2 | Kapılar oyuncunun ulaştığı en yüksek bölüme göre değerlendirilir. | düşük |
| 9.4.3 | Belirli bir bölümden önce ana ekranda olay simgesi gösterilmez (parametre: acilis_olay_simgeleri). | yüksek |
| 9.4.4 | Bir deney grubunda simge, açılış bölümünden en fazla parametre: kismi_kilit_onizleme bölüm önce kilitli olarak görünür. | orta |
| 9.4.5 | Açılış bölümüne ulaşılınca özelliğin tanıtım ve başlangıç pencereleri akışa eklenir. Tanıtım atlanabilir. | yüksek |
| 9.4.6 | Bölüm öncesi ekranında aynı anda en fazla parametre: seviye_oncesi_azami_olay olay gösterilir. | yüksek |
| 9.4.7 | Oyuncu bir olaya katılmışken ikinci bir olaya katılabilmesi için ilk olayın bitimine en az parametre: ikinci_olay_asgari_sure_dk kalmış olmalıdır. Olaya özel katılma eşikleri (parametre: hiz_yarisi_asgari_kalan_dk, album_sezon_giris_asgari_dk) kendi olaylarında bu kuralın yerine geçer. | düşük |
| 9.4.8 | **Olay bağımlılığı:** bir olay başka bir olay yüzünden engellenebilir ya da ödülünü zorla aldırabilir. Hangi olayın hangisini engellediği sunucu yapılandırmasındadır. | sunucu tarafı, kendi tasarımın |

**Kapı deseni:** özellikler aşağıdaki dönemlere yayılır. Her satırın kesin bölümü parametre dosyasındadır.

| Dönem | Açılan özellikler | Parametreler |
|---|---|---|
| İlk bölümler | Başlangıç takviyeleri; oyun içi araçlar birer birer; alan penceresinin otomatik açılması | acilis_baslangic_takviyeleri, acilis_tek_hucre_araci, acilis_satir_araci, acilis_sutun_araci, acilis_karistirma_araci, acilis_alan_penceresi |
| Erken | Bonus bölümleri; geri dönen oyuncu hediyesi; günlük ödül; takımlar; takım yarışı ve diğer takım olayları | acilis_bonus_bolumleri, acilis_karsilama_hediyesi, acilis_gunluk_odul, acilis_takim, acilis_takim_yarisi |
| Erken-orta | Hız yarışı; seri takviyesi; çarpan bileti; kademeli teklif; sezon ve haftalık ödül hatları; risk basamakları ve ilk basamaklı toplama olayları; albüm | acilis_hiz_yarisi, acilis_seri_takviyesi, acilis_carpan_bileti, acilis_kademeli_teklif, acilis_sezon_hatti, acilis_haftalik_hat, acilis_risk_basamaklari, acilis_ilk_toplama_olayi, acilis_album_indirme, acilis_album |
| Orta | Çarpan seçimi; haftalık liderlik tablosu; yeni basamaklı toplama olayları yaklaşık düzenli aralıklarla; ortaklı atış; harita keşif; arkadaş daveti; çift renk takviyesi; para mağazası ve teklifler; görevler | acilis_carpan_secimi, acilis_liderlik_tablosu, olay_turu_acilis_araligi, acilis_ortakli_atis, acilis_harita_kesif, acilis_arkadas_daveti, acilis_cift_renk_takviyesi, acilis_magaza, acilis_gorevler |
| Geç | Dolum göstergesi ve yedek hamle hakkı; yarışmalı bölümler | acilis_dolum_gostergesi, acilis_yedek_hamle_hakki, acilis_yarismali_bolumler |

**Bir olay kapısının durumları:** gizli → (deney grubunda, bölüm ≥ açılış − önizleme ve bölüm ≥ parametre: acilis_olay_simgeleri) kısmi kilitli. Gizli ya da kısmi kilitli → (bölüm ≥ açılış) açık → (ilk açılış) tanıtım → (tanıtım izlendi ya da atlandı) aktif. Açık → (tanıtım daha önce görüldü) aktif. Aktif → (olay bağımlılığı) engelli → (engelleyen olay bitti) aktif.

### 9.5 Günlük ödül

**Amaç:** her gün oyuna dönmeyi ödüllendirmek ve haftalık ve aylık seriyle düzenli girişi teşvik etmek.

| No | Kural | Güven |
|---|---|---|
| 9.5.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_gunluk_odul). İlk açılışta tanıtım gösterilir. | yüksek |
| 9.5.2 | Oyuncu ana ekrana geldiğinde o günün ödülü alınabilir durumdaysa pencere kendiliğinden açılır. Ödül alınınca seri bir artar. Bazı durumlarda ödül pencere açılmadan doğrudan verilir. | yüksek |
| 9.5.3 | Ödül alınamıyorsa simgede bir sonraki sıfırlamaya kalan süre gösterilir. | yüksek |
| 9.5.4 | Gün sınırı **sunucu saatine** göre sabit bir saattedir (parametre: gun_siniri_saati). Gün sayısı, sunucu zamanından bu saat çıkarılıp bir güne bölünerek bulunur. | yüksek |
| 9.5.5 | Haftalık seri ve aylık seri ayrı ayrı sayılır (parametre: haftalik_seri_uzunlugu, aylik_seri_uzunlugu). | yüksek |
| 9.5.6 | Haftalık ödül setleri sırayla döner (parametre: haftalik_set_sayisi). Hafta tamamlanınca bir sonraki sete geçilir. | orta |
| 9.5.7 | Bir gün kaçırılırsa haftalık seri sıfırlanır, aylık seri devam eder. | düşük |
| 9.5.8 | **Saat hilesi koruması:** son alınan gün sunucu gününden ilerideyse hile kabul edilir. İlerleme düzeltilir ve uyarı gösterilir. Bir tolerans vardır (parametre: hile_toleransi_dk). Sunucu korumayı kapatabilir. | yüksek |
| 9.5.9 | Ödül içerikleri sunucudan gelir ve sunucu özelliği tamamen kapatabilir (parametre: gunluk_odul_icerikleri, öneri içerik). | sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü) tanıtım → alınabilir → (ödül alındı, seri +1) alındı → (gün sınırı geçti) alınabilir. Alındı durumunda saat hilesi tespit edilirse ilerleme düzeltilip alındı durumuna dönülür. Sunucu kapatınca kapalı olur, açınca alınabilir duruma geçer.

### 9.6 Alan inşası

**Amaç:** bölüm kazanmayı uzun vadeli bir hedefe bağlamak: yıldız toplayıp alanlardaki görevleri inşa etmek.

| No | Kural | Güven |
|---|---|---|
| 9.6.1 | Bölüm kazanınca yıldız alınır (parametre: bolum_basina_yildiz). | orta (yapı), kendi tasarımın (miktar) |
| 9.6.2 | Alan penceresinde görevler listelenir. Her görevin bir yıldız fiyatı ve önkoşul görevleri vardır. Görev ağacı doğrusal değildir. Yeterli yıldız varsa görev inşa edilir ve animasyon oynar (parametre: alan_gorev_sayisi, gorev_yildiz_fiyati). | yüksek (yapı), kendi tasarımın (sayılar) |
| 9.6.3 | Yeterli yıldız varsa kalan bütün görevler tek seferde inşa edilebilir. | yüksek |
| 9.6.4 | Alanın bütün görevleri bitince **alan sandığı** açılır ve ödül verilir. Ardından bir sonraki alan açılır. | yüksek |
| 9.6.5 | Alan sandığı alan numarasından bağımsız sabit bir pakettir (parametre: alan_sandigi_para, alan_sandigi_takviyeler). İlk alandan sonra sandığa ek araçlar eklenir (parametre: alan_sandigi_ek_araclar). Belirli bir alandan sonra, aktif bir albüm sezonu varsa sandığa kart paketi eklenir (parametre: alan_sandigi_kart_paketi_alani). Bir deney grubunda ek araçların yerine kısa sınırsız araç süreleri verilir. | orta |
| 9.6.6 | Alan penceresi belirli bir bölümde bir kez kendiliğinden açılır (parametre: acilis_alan_penceresi). Belirli bir bölüme kadar yapılabilir görev bildirimi zorla gösterilir (parametre: gorev_bildirimi_son_bolum). | yüksek |
| 9.6.7 | Oynanabilir alan sayısı yayımlanmış en yüksek bölümden türetilir (parametre: alan_basina_bolum). Yeni alan yoksa son alan "bekliyor" durumunda kalır. | orta |

**Bir alanın durumları:** kilitli → (önceki alan tamamlandı) inşaatta → (görev inşa edildi, yıldız harcandı) inşaatta → (son görev) sandık hazır → (sandık açıldı) tamamlandı → sonraki alan açılır. İnşaatta durumunda yayımlanmış bölüm sınırına gelinirse "bekliyor".

### 9.7 Sezonluk albüm

**Amaç:** olay ödüllerine ikinci bir değer katmanı eklemek: paketlerden kart çıkar, setler tamamlanır, sosyal takas yapılır.

| No | Kural | Güven |
|---|---|---|
| 9.7.1 | Sezon görselleri bir bölümden sonra indirilir (parametre: acilis_album_indirme), albüm daha sonraki bir bölümde açılır (parametre: acilis_album). Sezona girmek için sezonun bitimine en az parametre: album_sezon_giris_asgari_dk kalmış olmalıdır. | yüksek |
| 9.7.2 | Kart paketleri olay, ödül hattı ve mağaza ödüllerinden gelir. Paket açılınca kartlar setlere eklenir; yeni kartlar işaretlenir. | yüksek |
| 9.7.3 | Bir sette en fazla parametre: set_basina_kart kart vardır. Sezonun set sayısı yapılandırmadan gelir (parametre: set_sayisi). | yüksek |
| 9.7.4 | Kartların üç kademesi vardır: sıradan, değerli, çok değerli. Ayrıca bir yıldız nadirliği vardır (parametre: kart_azami_yildiz). En üst kademedeki setler ayrıca işaretlenir. | yüksek |
| 9.7.5 | Paket içeriği bir ağırlık tablosuyla seçilir: kademe ağırlıkları ve yıldız sayısına göre ağırlıklar. | yüksek (yapı) |
| 9.7.6 | **Acıma kuralı:** eksik kartlar daha ağır basar. Oyuncunun çift kart sayısı arttıkça eksik kart çıkma olasılığı yükselir (parametre: acima_katsayisi). | sunucu tarafı, kendi tasarımın |
| 9.7.7 | Set dolunca set ödülü alınır. Bütün setler dolunca büyük ödül alınır ve sezon bir sonraki aşamaya geçebilir (parametre: rozetli_asama_sayisi). | yüksek |
| 9.7.8 | Çift kartlar yıldıza dönüşür. Yıldızlarla çift kart sandıkları açılır; sandıkların üç kademeli fiyatı vardır (parametre: cift_kart_yildiz_degeri, cift_kart_sandik_fiyatlari). | yüksek |
| 9.7.9 | Oyuncu günde en fazla parametre: gunluk_kart_gonderme kart gönderebilir: arkadaşa, takım üyesine ya da takım sohbetine. Takım sohbetinden kart isteyebilir. İstek sınırı belirli aralıkla sıfırlanır (parametre: kart_istek_sifirlama_saat). | yüksek |
| 9.7.10 | Diğer olayların ödül listelerindeki kart paketleri sezona göre değiştirilebilir. Böylece aynı olay farklı sezonlarda farklı paketler verir. | yüksek |
| 9.7.11 | Sezon bitince alınmamış ödüller kendiliğinden verilir ve bitiş penceresi gösterilir. | yüksek |

**Sezonun durumları:** kilitli → (indirme bölümü) indiriliyor → (görseller indi) bekliyor → (açılış bölümü ve yeterli süre) aktif → (bütün setler alındı) aşama tamam → (sonraki aşama) aktif. Aktif ya da aşama tamam durumunda süre dolunca bitti; alınmamış ödüller verilince yeni sezon için bekliyor.

### 9.8 Sezon ödül hattı

**Amaç:** aylık sezon boyunca bölüm kazanmayı basamaklı ödüllere bağlamak. Ücretsiz hat herkese açıktır; premium hat ücretli satın almayla açılır.

| No | Kural | Güven |
|---|---|---|
| 9.8.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_sezon_hatti). Tanıtım beş adımdır: simge, ilk ücretsiz ödül, ilerleme çubuğu, ilk premium ödül, bitti. | yüksek |
| 9.8.2 | Hat, parametre: sezon_hatti_basamak_sayisi ödül basamağı ve en sonda bir **birikim haznesinden** oluşur. Her basamağın kendi puan hedefi vardır (parametre: sezon_hatti_basamak_hedefleri). Hedefler birikimli değildir: basamak atlanınca puan sıfırdan sayılır. Hedefi aşan artık puan bir sonraki basamağa devreder; bir kazanım birden fazla basamak atlatabilir. | yüksek (basamak başına hedef), düşük (artığın devri) |
| 9.8.3 | Her parametre: kilometre_tasi_araligi basamakta bir **kilometre taşı** vardır. Kilometre taşlarında iki hatta da daha büyük paketler ve kozmetikler (çerçeve, isim stili) verilir. | yüksek |
| 9.8.4 | **Hat puanı** her kazanılan bölümde, bölümün zorluk etiketine göre sabit bir tablodan gelir (parametre: hat_puani_normal, hat_puani_zor, hat_puani_cok_zor, hat_puani_asiri_zor, hat_puani_ozel). Etiketi tablo dışında kalan bölümler normal sayılır. Puan, kazanma ekranının animasyonu sırasında eklenir. | yüksek (tablo yapısı), orta (etiket eşlemesi) |
| 9.8.5 | Sezon hattı puanına çarpan uygulanmaz (9.10.3). | düşük |
| 9.8.6 | Basamak atlanınca o basamağın ücretsiz ödülü alınabilir hale gelir. Ödüller tek tek ya da toplu alınabilir. | yüksek |
| 9.8.7 | Premium hat satın alınınca daha önce geçilmiş basamakların premium ödülleri de alınabilir hale gelir. Satın almadan sonra bir ödül gösterim penceresi açılır. | orta |
| 9.8.8 | **Birikim haznesi:** son ödül basamağından sonra kazanılan puanlar hazneye akar. Hazne premium hatta para biriktirir. Para yalnızca oyuncu son basamaktayken hesaplanır: her parametre: hazne_asama_puani puan bir aşamadır ve her aşama parametre: hazne_asama_parasi para ekler. Hazne hedefe ulaşınca dolar ve alınır (parametre: sezon_hazne_hedefi). Önceki basamaklarda hazne 0 gösterir. | yüksek (formül), düşük (aşama başına puan) |
| 9.8.9 | Premium hat sahibinin azami canı yükselir (9.1.1). | yüksek |
| 9.8.10 | Sezon bitince alınmamış ödüller toplu verilir ve bitiş penceresi gösterilir. Önceki sezondan kalan alınmamış ödüller de kontrol edilir. | yüksek |
| 9.8.11 | Sezon süresi sunucudan kalan süre olarak gelir (parametre: sezon_suresi_gun). Premium hattın fiyatı mağaza tarafındadır. Bir deney grubunda farklı bir basamak tablosu kullanılabilir. | sunucu tarafı, kendi tasarımın |
| 9.8.12 | Ödül dağılımı önerisi: ücretsiz hatta küçük paketler, premium hatta daha büyük paketler ve sınırsız süreler (parametre: sezon_hatti_ucretsiz_toplam, sezon_hatti_premium_toplam). | düşük |

**Durumlar:** kilitli → (açılış bölümü) tanıtım → ücretsiz hat → (satın alma) premium hat. İki hatta da bölüm kazanımı puan getirir, basamak atlatır ve ödül açar. Premium hatta son ödül basamağı geçilince hazne; hazne hedefine ulaşınca hazne dolu. Her durumda süre dolunca bitti; alınmamış ödüller verilir ve yeni sezon ücretsiz hatla başlar.

### 9.9 Haftalık ödül hattı

**Amaç:** sezon hattının kısa ve ucuz biçimi. Aynı mantık, daha az basamak.

| No | Kural | Güven |
|---|---|---|
| 9.9.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_haftalik_hat). Başlangıç penceresi ve tanıtım bir kez gösterilir. | yüksek |
| 9.9.2 | Hat, parametre: haftalik_hat_basamak_sayisi ödül basamağı ve bir birikim haznesinden oluşur (parametre: haftalik_hat_basamak_hedefleri, haftalik_hazne_hedefi, haftalik_hazne_parasi). Her parametre: haftalik_kilometre_tasi basamakta bir kilometre taşı vardır. Hazne parası 9.8.8'deki formülle hesaplanır; aşama parası parametre: haftalik_hazne_asama_parasi. | yüksek (yapı) |
| 9.9.3 | **Puan:** bölüm kazanınca verilen puan = zorluk etiketine göre taban değer × çarpan. Taban değer sezon hattıyla aynı tablodan gelir (9.8.4). Bölüm çarpanla oynandıysa aktif çarpan uygulanır, oynanmadıysa çarpan 1'dir (9.10.2). | yüksek |
| 9.9.4 | Aynı puan, bölüm kazanımında bütün basamaklı toplama olaylarına, harita keşif olayına ve ortaklı atış olayına da aynen verilir (10.9.2). | yüksek (toplama olayları), orta (harita keşif, ortaklı atış) |
| 9.9.5 | Ücretsiz ve ücretli hat vardır. Ücretli hat satın alınınca geçmiş basamakların ücretli ödülleri de alınabilir. | orta |
| 9.9.6 | Son basamaktan sonra hazne dolar. Hafta sonunda alınmamış ödüller toplu verilir. Son gün için ayrıca bir bildirim gösterilir. | yüksek |
| 9.9.7 | Sunucu iki yapılandırma tutar: olayın genel yapılandırması ve oyuncuya özel yapılandırma. İkisi de sürümlüdür; basamak tablosu oyuncuya göre değişebilir. Süre sunucudan gelir (parametre: haftalik_hat_suresi_gun). | sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü) başlangıç → (başlangıç penceresi görüldü) ücretsiz → (satın alma) ücretli. Son basamak geçilince hazne. Süre dolunca hafta bitti; ödüller verilince yeni hafta başlangıç durumuyla başlar.

### 9.10 Çarpan sistemi

| No | Kural | Güven |
|---|---|---|
| 9.10.1 | Bir etkinlik çarpanı ya da çarpan bileti aktifken oynanan bölüm "çarpanla oynandı" olarak işaretlenir. İşaret bölüme girildiği anda konur. | yüksek (işaret), orta (anı) |
| 9.10.2 | İşaretli bir bölüm kazanılınca haftalık hat puanı ve basamaklı toplama olaylarının puanı aktif çarpanla çarpılır. İşaretsiz bölümde çarpan 1'dir. | yüksek |
| 9.10.3 | Sezon hattı puanına çarpan uygulanmaz. | düşük |
| 9.10.4 | Çarpan bileti para ile alınır ya da ödül olarak gelir (parametre: carpan_bileti_fiyati, carpan_bileti_kati). Ödül olarak verilen etkinlik çarpanı dakika cinsinden bir süredir (parametre: etkinlik_carpani_suresi_dk). | orta |
| 9.10.5 | Oyuncu bölüm öncesinde çarpan düzeyini seçebilir (parametre: acilis_carpan_secimi). Ayrıntılar kendi tasarımın. | düşük |

### 9.11 Yarı dinamik zorluk

| No | Kural | Güven |
|---|---|---|
| 9.11.1 | Her bölüm için bir **deneme sayacı** tutulur. Her başarısızlıkta bir artar ve bir üst sınırda durur (parametre: deneme_sayaci_azami). Bölüm kazanılınca sıfırlanır. | düşük |
| 9.11.2 | **Hamle değişikliği yalnızca yarışmalı bölümlerde uygulanır.** Yarışmalı bölümde yarışma sisteminden gelen bir değer hamle sayısına eklenir. Normal ilerleme bölümlerinde hamle sayısı bölüm verisindeki gibi kalır. | yüksek (normal bölüm), orta (yarışmalı bölüm) |
| 9.11.3 | Oyuncu becerisi ilk bölümlerde ölçülür (parametre: beceri_olcum_son_bolum). Dört düzey vardır: becerisiz, başlangıç, orta, usta. Eşikler kendi tasarımın (parametre: beceri_esikleri). | orta |
| 9.11.4 | Ek hamle teklifi, aynı bölümde art arda başarısızlıklardan sonra daha fazla hamle verir (parametre: ek_hamle_artis_esigi, ek_hamle_artis_miktari, ek_hamle_artis_siniri). | orta |
| 9.11.5 | **Zorluğa bağlı rastgelelik:** kaskadda doğum hücresi seçimi (2.7.7), rastgele dolgu rengi (5.3.5), ayarlı dolgu (5.3.6), başlangıç renkleri (5.4.1) ve dolum göstergesinin fırlatma hücresi (3.7.4) aynı zorluk ağırlıklı rastgele üreteçten beslenir. Bölümün zorluk etiketi ve oyuncu becerisi üretecin ağırlığını belirler (parametre: zorluk_rastgelelik_agirligi). | orta (varlık), düşük (ağırlık) |

### 9.12 Ek hamle teklifi

| No | Kural | Güven |
|---|---|---|
| 9.12.1 | Teklif varsayılan bir hamle sayısı verir (parametre: ek_hamle_sayisi). Bir deney grubunda daha fazla hamle verilebilir. | yüksek |
| 9.12.2 | Aynı bölümde art arda alımlarda fiyat basamaklanır (parametre: ek_hamle_fiyat_kademeleri). Normal ve yüksek fiyat düzeyleri vardır (9.3.7). Kademe bölüm kazanılınca ya da bırakılınca sıfırlanır. | yüksek (kademe), düşük (sıfırlanma) |
| 9.12.3 | Üst kademelerde ek hamleyle birlikte özel taşlar da verilir; bunlar tahtaya başlangıç takviyesi gibi konur (parametre: ek_hamle_paket_icerikleri). | orta |
| 9.12.4 | Reklam izleyerek ek hamle alınabilir (parametre: ek_hamle_reklam_sayisi). | düşük |
| 9.12.5 | Belirli bir bölüme kadar, oyuncu teklifi reddederse kaybetmeden önce ikinci bir uyarı gösterilir (parametre: ikinci_uyari_son_bolum). | orta |

### 9.13 Reklam ödülleri

| No | Kural | Güven |
|---|---|---|
| 9.13.1 | Ödüllü reklam yalnızca üç ödül türü için tanımlıdır: can, para, ek hamle (parametre: reklam_para_odulu, ek_hamle_reklam_sayisi). | orta |

### 9.14 Mağaza ve teklifler

| No | Kural | Güven |
|---|---|---|
| 9.14.1 | Para mağazası belirli bir bölümden sonra açılır ve sabit sayıda ürün gösterir (parametre: acilis_magaza, magaza_urun_sayisi). Ürünler ve fiyatlar uygulama mağazasından ve sunucudan gelir. | sunucu tarafı, kendi tasarımın |
| 9.14.2 | Dönemsel teklifler, kademeli teklifler (sırayla açılan satın alma basamakları) ve can dolum teklifi vardır. İçerikleri ve fiyatları sunucudadır (parametre: acilis_kademeli_teklif). | sunucu tarafı, kendi tasarımın |
| 9.14.3 | Gerçek para satın alımları doğrudan ödül paketlerine gider. Ayrı bir sert para birimi yoktur. | orta |

### 9.15 Diğer meta özellikler

| No | Kural | Güven |
|---|---|---|
| 9.15.1 | **Geri dönen oyuncu hediyesi:** bir süre oynamayan oyuncuya dönüşte paket verilir (parametre: acilis_karsilama_hediyesi). | orta |
| 9.15.2 | **Hesap bağlama:** sosyal hesap bağlayan oyuncuya bir kerelik para verilir (9.2.3). | yüksek |
| 9.15.3 | **Arkadaş daveti, haftalık liderlik tablosu, görevler:** açılış kapıları 9.4'te. İçerik ve ödüller kendi tasarımın. | sunucu tarafı, kendi tasarımın |

---

## 10. Olay iskeletleri

Olayların sunucuya bağlı kısımları (takvim, grup eşleştirme, sıralama, ödül miktarları) **sunucu tarafı, kendi tasarımın** olarak işaretlidir. Burada verilen sayılar parametre dosyasındaki önerilerdir.

### 10.0 Ortak olay iskeleti

| No | Kural | Güven |
|---|---|---|
| 10.0.1 | Her olayın bir açılış bölümü, sunucunun verdiği bir olay kimliği ve bir bitiş anı vardır. Bazı olaylarda ayrıca oyuncuya özel bir bitiş anı vardır. | yüksek |
| 10.0.2 | Sunucu her olayı ayrı ayrı kapatabilir. | yüksek |
| 10.0.3 | Olay pencereleri sırayla akışa eklenir: tanıtım, başlangıç, bilgi, ödül alma ve otomatik pencere. | yüksek |
| 10.0.4 | Olay bağımlılığı bir olayı engelleyebilir ya da ödülünü zorla aldırabilir (9.4.8). | yüksek |
| 10.0.5 | Olayların çoğu çarpılabilirdir: çarpanla oynanan bölümün kazanımı olay puanını katlar (9.10). | yüksek |
| 10.0.6 | Bazı olaylarda süre bitince kalan toplanabilirler bir sonraki olaya aktarılabilir. | orta |
| 10.0.7 | Ödüller ortak ödül paketi yapısıyla verilir (9.2.6). Ödül listelerindeki kart paketleri albüm sezonuna göre değiştirilebilir (9.7.10). | yüksek |
| 10.0.8 | Olay süresi bitince alınmamış ödüller kendiliğinden verilir ve bitiş penceresi gösterilir. | yüksek |
| 10.0.9 | Bir olayın bölüm öncesi ekranında gösterilip gösterilmeyeceği olay türüne bağlıdır; aynı anda gösterilen olay sayısı sınırlıdır (9.4.6). | yüksek |

### 10.1 Hız yarışı olayı

**Amaç:** kısa süreli, yoğun bir oyun anı. Küçük bir grupta en çok bölümü kazanan ödülü alır. Katılınca olay süresince sınırsız can verilir.

| No | Kural | Güven |
|---|---|---|
| 10.1.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_hiz_yarisi). Bölüm öncesi ekranında da gösterilir. | yüksek |
| 10.1.2 | Oyuncu katılmak isteyince istemci sunucudan bir grup ister (parametre: hiz_yarisi_grup_boyutu). Katılımla birlikte oyuncuya özel bir bitiş anı başlar. Katılmak için olayda yeterli süre kalmış olmalıdır (parametre: hiz_yarisi_asgari_kalan_dk). | yüksek (grup isteği, oyuncuya özel bitiş), orta (asgari kalan süre) |
| 10.1.3 | Katılınca oyuncuya olay süresi boyunca sınırsız can verilir. Süre, kalan zamana göre dakika olarak hesaplanır. | yüksek |
| 10.1.4 | Oyuncu bölüm kazandıkça puanı artar ve sunucuya gönderilir (parametre: hiz_yarisi_puan_normal, hiz_yarisi_puan_zor). Grup listesi belirli aralıklarla ve her kazanımdan sonra yenilenir. | yüksek (gönderim), kendi tasarımın (puan) |
| 10.1.5 | Olay aşamalardan oluşur (parametre: hiz_yarisi_asama_sayisi, hiz_yarisi_asama_suresi_dk). Her aşamanın kendi ödülü vardır. Bir olay döneminde en fazla parametre: hiz_yarisi_azami_katilim kez katılınabilir. Ödül alındıktan sonra yeni bir aşamaya başlama penceresi açılabilir. | orta |
| 10.1.6 | Kalan hamle bilgisi (7.6.3) olaya iletilir. Öneri: eşitlikte bölümü daha çok kalan hamleyle bitiren önde olur. | düşük |
| 10.1.7 | Süre dolunca istemci son sıralamayı birkaç kez, aralıklarla ister (parametre: son_liste_deneme_sayisi, son_liste_deneme_araligi_sn). Liste gelmezse yerel sıralama kullanılır. Sonra ödül alma penceresi açılır. | yüksek |
| 10.1.8 | Sıralama, eşitlik bozma ve botla doldurma sunucudadır. Öneri: yalnızca birinci büyük ödülü alır, diğerleri aşama ödülünü alır (parametre: hiz_yarisi_birincilik_odulu). | sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü ve sunucuda aktif olay) hazır → (katıl) katılıyor → (grup atandı, sınırsız can başladı) yarışta → (oyuncu süresi doldu) son liste bekleniyor → (liste geldi ya da denemeler bitti) ödül hazır → (ödül alındı) yeni aşama varsa hazır, yoksa bitti. Katılma hatasında ya da yeterli süre yoksa hazır durumuna dönülür.

### 10.2 Kurtarma bölümleri

**Amaç:** normal bölüm akışının arasına, ana karakterin tehlikede olduğu özel temalı bölümler eklemek. Görsel çekicilik ve tanıtım kampanyaları için kullanılır.

| No | Kural | Güven |
|---|---|---|
| 10.2.1 | Belirli ilerleme bölümlerine bir kurtarma bölümü bağlanır. Oyuncu o bölüme gelince normal bölüm yerine kurtarma bölümü yüklenir. Eşleme tablosu "ilerleme bölümü → (kurtarma bölümü numarası, tema)" biçimindedir ve uygulama içinde sabittir. | yüksek |
| 10.2.2 | **Ritim:** ilk kurtarma bölümü erken gelir (parametre: kurtarma_ilk_bolum). Erken dönemde birkaç on bölümde bir gelir (parametre: kurtarma_araligi_erken). Daha sonra seyrek ve düzenli aralıklarla, her bloğun aynı sıradaki bölümünde gelir (parametre: kurtarma_araligi_gec, kurtarma_gec_donem_baslangici). | yüksek (desen) |
| 10.2.3 | Temalar sırayla döner. Bir ana tema kurtarma bölümlerinin parametre: kurtarma_ana_tema_payi kadarında kullanılır; diğer temalar sabit bir döngüde sırayla gelir. Tema listesi ve görselleri kendi tasarımın (parametre: kurtarma_tema_sayisi). | yüksek (dönüşüm deseni), kendi tasarımın (temalar) |
| 10.2.4 | Kurtarma bölümü kazanılınca para verilir (parametre: kurtarma_kazanma_parasi) ve normal ilerleme devam eder. | yüksek |
| 10.2.5 | Her temanın kendi sahnesi, geçiş efekti ve müziği vardır. Gerekli tema paketi indirilmemişse bir yedek bölüm kullanılır. | yüksek |
| 10.2.6 | Bir deney grubunda ilk kurtarma bölümleri en baştaki bölümlere çekilir. | orta |
| 10.2.7 | Bazı kurtarma bölümleri fırlat-birleştir mini oyununa bağlanabilir (10.8). | orta |
| 10.2.8 | Kurtarma bölümünde tek hedef vardır (ana karakteri kurtarmak). Başarısızlıkta can düşmez. | düşük |

**Durumlar:** normal akış → (bölüme kurtarma bölümü bağlı) kurtarma bölümü → (paket yok) paket bekleniyor → (paket indi) kurtarma bölümü ya da (yedek kullanıldı) yedek bölüm → (kazanıldı, para) normal akış. Kayıpta aynı kurtarma bölümü tekrarlanır.

### 10.3 Takım yarışı olayı

**Amaç:** takımları birbirine karşı yarıştırmak. Üyelerin bölüm kazanımları takım puanına eklenir; sıralamaya göre bütün takım ödül alır.

| No | Kural | Güven |
|---|---|---|
| 10.3.1 | Bir açılış bölümünden sonra ve yalnızca bir takımdaysa açılır (parametre: acilis_takim_yarisi). Takımı olmayan oyuncu katılamaz. | yüksek |
| 10.3.2 | Olay başlayınca takım, sunucu tarafından başka takımlarla bir gruba konur (parametre: takim_yarisi_grup_boyutu). | yüksek (yapı), sunucu tarafı (boyut) |
| 10.3.3 | Oyuncu bölüm kazandıkça takım puanı toplar (parametre: takim_yarisi_puan_normal, takim_yarisi_puan_zor). Puan hem oyuncunun hem takımının toplamına eklenir. Sıralama yerelde de güncellenir. | yüksek (yapı), kendi tasarımın (puan) |
| 10.3.4 | Bitime belirli bir süre kala "son saatler" durumu başlar: uyarı ve vurgu gösterilir (parametre: son_saatler_penceresi_saat). | yüksek |
| 10.3.5 | Olay bitince sonuç penceresi açılır ve ödül alınır. Ödül alabilmek için oyuncunun katkısı en az parametre: takim_yarisi_asgari_katki puan olmalıdır. Ödülü hak etmeyen oyuncu için sunucuya "ödülsüz kapat" bildirilir. | yüksek |
| 10.3.6 | Ödüller sıralamaya göre bir listeden verilir (parametre: takim_yarisi_sira_odulleri). Sunucu listesi istemcideki varsayılanın yerine geçebilir. Bir deney grubunda takviye ödülleri azaltılır. | yüksek (yapı), orta (sıra eşlemesi) |
| 10.3.7 | Olay sürerken takımdan ayrılmak isteyen oyuncuya uyarı gösterilir. | yüksek |
| 10.3.8 | Olay durumları: sürüyor, bitti, ödül alındı, yeni. Süre ve grup büyüklüğü sunucudadır (parametre: takim_yarisi_suresi_saat). | yüksek (durumlar), sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü) takımsız → (takıma katıldı) hazır → (olay başladı, gruba atandı) yarışta → (bitime son saatler kaldı) son saatler → (süre doldu) bitti → katkı yeterliyse ödül alındı, yoksa ödülsüz kapandı → yeni olayda hazır. Yarışta takımdan ayrılan oyuncu takımsız duruma döner.

### 10.4 Harita keşif olayı

**Amaç:** bölüm kazanarak enerji toplamak, enerjiyle harita görevlerini tamamlamak, haritayı açmak ve sandıklara ulaşmak. Ücretli hattı ve günlük teklifiyle gelir getiren bir olaydır.

| No | Kural | Güven |
|---|---|---|
| 10.4.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_harita_kesif). Başlangıç penceresi ve tanıtım bir kez gösterilir. | yüksek |
| 10.4.2 | Bölüm kazanınca gelen enerji 10.9.2'deki puandır (zorluk tabanı × çarpan). Enerji ödül olarak da verilebilir. Toplanabilir enerjinin bir üst sınırı vardır (parametre: harita_kesif_azami_enerji). Harcanan enerji ayrıca tutulur. | yüksek (yapı) |
| 10.4.3 | Haritada iki karakter ilerler. Oyuncu enerji harcayarak görevleri tamamlar (parametre: harita_kesif_gorev_maliyeti). Büyük görevlerin alt görevleri vardır. | yüksek |
| 10.4.4 | Tamamlanan görevler haritanın parçalarını açar: tam açma, yarı açma ya da görünür yapma. Haritada para ve enerji paketleri toplanır. | yüksek |
| 10.4.5 | İlerleme sandıklara bağlıdır: parametre: harita_kesif_sandik_esikleri listesindeki her eşik bir sandık açar. | orta |
| 10.4.6 | Olayın kendi ödül hattı vardır: ücretsiz, düşük ve yüksek hat (parametre: harita_kesif_hat_basamak). Her basamakta bir hedef ve dört ödül vardır: ücretsiz, düşük, yüksek ve yüksek hat ek ödülü. | yüksek |
| 10.4.7 | Günlük teklif oyuncu segmentine göre seçilir. Teklifi almayan oyuncuya ertesi gün daha uygun bir segment önerilebilir. | orta |
| 10.4.8 | Bazı olayların ödülleri bu olaya enerji besleyebilir (enerji ödülü). | orta |
| 10.4.9 | Süre bitmeden önce "son şans" durumu vardır. Bitince alınmamış ödüller verilir. Harita yerleşimi, görev maliyetleri, süre ve ödüller kendi tasarımın (parametre: harita_kesif_suresi_gun). | sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü ve yeterli süre) hazır → (başlangıç penceresi görüldü) aktif. Aktifken bölüm kazanımı enerji getirir; görev enerji harcar; paket ve sandık alınır. Düşük ya da yüksek hat satın alınınca aynı döngü hat ödülleriyle sürer. Bitime az kalınca son şans; süre dolunca bitti; ödüller verilince yeni olay için hazır.

### 10.5 Risk basamakları olayı

**Amaç:** basamaklı bir toplama olayına risk katmanı eklemek. Oyuncu bölüm kaybettikçe şans hakkı yitirir.

| No | Kural | Güven |
|---|---|---|
| 10.5.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_risk_basamaklari). | yüksek |
| 10.5.2 | Oyuncu bölüm kazandıkça toplanabilir alır ve basamak hedeflerini doldurur (parametre: risk_basamak_hedefleri). Kazanılan miktar 10.9.2'deki puandır. Basamak ödülleri tek tek ya da toplu alınabilir (parametre: risk_basamak_odulleri). Toplanabilirin bir üst sınırı vardır (parametre: risk_basamak_azami_toplanabilir). | yüksek |
| 10.5.3 | Oyuncunun belirli sayıda **şans hakkı** vardır (parametre: risk_basamak_sans_hakki). | yüksek |
| 10.5.4 | **Bölüm kaybı kuralı:** (1) Olay ilerlemiyorsa ya da bütün basamaklar bittiyse hiçbir şey olmaz. (2) Mevcut basamakta henüz toplanabilir yoksa **ceza yoktur**, şans hakkı gitmez. (3) Aksi halde bir şans hakkı gider. (4) Giden hak sonuncuysa **yalnızca mevcut basamağın ilerlemesi sıfırlanır.** Önceki basamaklar ve alınmış ödüller korunur. Ardından şans hakları yeniden dolar. | yüksek (1–4), düşük (yeniden dolma) |
| 10.5.5 | Olay sürerken başlamış bir bölümden çıkmak isteyen oyuncu uyarılır; çıkış da kayıp sayılır (9.1.3). | orta |
| 10.5.6 | Bütün basamaklar bitince olay biter. Süre dolarsa kalan toplanabilirler bir sonraki olaya aktarılabilir (10.0.6). | yüksek |
| 10.5.7 | Kurtarma bölümleri (10.2) bu olayda ayrıca işlenir. Öneri: kurtarma bölümündeki kayıp şans hakkı götürmez. | orta (ayrı işlenme), düşük (öneri) |

**Durumlar:** kilitli → (açılış bölümü) hazır → (olaya girildi) tam haklı aktif. Kazanım toplanabilir ve basamak getirir. Mevcut basamakta ilerleme varken kayıp bir hak götürür. Son hak gidince basamak ilerlemesi sıfırlanır ve haklar dolar. Son basamak bitince tamam. Süre dolunca bitti ve kalan toplanabilir aktarılır.

### 10.6 Ortaklı atış olayı

**Amaç:** küçük bir ortak grubuyla birlikte ilerlenen olay. Oyuncu diğer olaylardan bilye toplar, bilyeleri bir atış tahtasında kullanıp puan kazanır ve puanla olayın maskotunu büyütür.

| No | Kural | Güven |
|---|---|---|
| 10.6.1 | Bir açılış bölümünden sonra açılır (parametre: acilis_ortakli_atis). Oyuncu takımından ya da arkadaşlarından ortak davet eder (parametre: ortakli_atis_azami_ortak). | yüksek |
| 10.6.2 | Bilyeler diğer olayların basamak ödüllerinden gelir. Bölüm kazanımı da 10.9.2'deki puan kadar bilye verir. | yüksek (ödüllerden gelme), orta (kazanım miktarı) |
| 10.6.3 | Oyuncu bilyeleri fizik tabanlı bir atış tahtasına atar ve puan kazanır. Bir bilyenin getirdiği puan atışın sonucuna bağlıdır ve istemcide hesaplanır. Puan ortağın puanıyla birlikte sayılır ve ortağa canlı iletilir. | yüksek (yapı), orta (istemci hesabı) |
| 10.6.4 | Puan basamaklarında maskot bir sonraki evresine geçer ve basamak ödülü alınır (parametre: ortakli_atis_puan_basamaklari). | yüksek |
| 10.6.5 | Olay bitince son liste alınır (10.1.7'deki deneme kuralıyla). Büyük ödül verilir (parametre: ortakli_atis_buyuk_odul). Kullanılmamış bilyeler belirli bir oranla takviyeye dönüştürülür (parametre: ortakli_atis_bilye_donusumu). | yüksek |
| 10.6.6 | Ortak eşleştirme, puan doğrulama ve süre sunucudadır. | sunucu tarafı, kendi tasarımın |

**Durumlar:** kilitli → (açılış bölümü) hazır → (olaya girildi) ortak aranıyor → (en az bir ortak) aktif. Aktifken bilye kazanılır, atılır, ortak puanı gelir ve basamakta maskot büyür. Süre dolunca son liste beklenir; liste gelince büyük ödül ve bilye dönüşümü verilir.

### 10.7 Takımlar

**Amaç:** sosyal bağ: sohbet, can ve kart yardımlaşması; takım olaylarına katılım.

| No | Kural | Güven |
|---|---|---|
| 10.7.1 | Bir açılış bölümünden sonra takım bölümü açılır (parametre: acilis_takim). Takımı olmayan oyuncu takım arar, önerilen takımlara bakar ya da takım kurar. | yüksek |
| 10.7.2 | Takım kurmak para ister (parametre: takim_kurma_bedeli). Kurarken ad, simge ve takım türü (açık ya da kapalı) seçilir. Açık takıma doğrudan katılınır; kapalı takıma istek gönderilir ve lider onaylar. | yüksek (bedel), orta (açık / kapalı) |
| 10.7.3 | Takım içi sohbet canlı bağlantıyla çalışır. Oyuncu can isteyebilir (parametre: can_istek_araligi_saat); diğer üyeler yardım eder. Yardım edene para (parametre: can_yardim_odulu), isteyene gelen kutusuna can gider. Bir istek en fazla belirli sayıda yardımla karşılanır (parametre: can_istegi_azami_yardim). | yüksek (istek ve yardım yapısı, yardım ödülü), kendi tasarımın (azami yardım), sunucu tarafı (istek aralığı) |
| 10.7.4 | Liderler ve yardımcı liderler üye atabilir ve yetki verebilir. Oyuncular diğer kullanıcıları susturabilir. Kötüye kullanımda yardım men süresi uygulanır. | yüksek |
| 10.7.5 | Takımın etkinlik düzeyi gösterilir: yüksek, orta, düşük. | yüksek |
| 10.7.6 | Takımdan ayrılırken sürmekte olan bir takım olayı varsa uyarı gösterilir. Takım teklifinden satın alınan paket takım üyelerine de ödül dağıtır. | yüksek (uyarı), orta (dağıtım) |
| 10.7.7 | Kartlar takım üyesine ve takım sohbetine gönderilebilir (9.7.9). Ebeveyn denetimi sohbeti kapatabilir. | yüksek |
| 10.7.8 | Üye sınırı, istek bekleme süresi, katılma koşulu ve öneri algoritması sunucudadır (parametre: takim_azami_uye). | sunucu tarafı, kendi tasarımın |

**Oyuncunun takım durumları:** kilitli → (açılış bölümü) takımsız → açık takıma katıldı: üye; kapalı takıma istek: istek bekliyor → (onay) üye, (ret ya da iptal) takımsız; takım kurdu: lider. Üye yetki alınca yardımcı lider olur, yetkisi alınınca üyeye döner; liderlik devredilince lider olur. Ayrılan ya da atılan takımsız duruma döner.

### 10.8 Fırlat-birleştir mini oyunu

| No | Kural | Güven |
|---|---|---|
| 10.8.1 | Fizik tabanlı bir mini oyundur. Oyuncu bir sonraki nesneyi yatayda nişanlayıp masaya atar. Aynı seviyedeki nesneler birleşip bir üst seviyeye çıkar. Bir sonraki nesnenin seviyesi önceden gösterilir. | yüksek |
| 10.8.2 | Masanın bir kaybetme bölgesi vardır. Bir nesne orada takılı kalırsa oyun biter. | yüksek |
| 10.8.3 | Bölüm hedefi bir sipariş listesidir: belirli seviyedeki nesneleri üretmek. Gizli nesneler de vardır; ortaya çıkarılmaları gerekir. | yüksek |
| 10.8.4 | Atış sayısı tutulur. Nişan ipuçları ve bekleme süresi dolunca otomatik atış vardır (parametre: mini_oyun_otomatik_atis_sn). | yüksek |
| 10.8.5 | Mini oyunun kendi kazanma/kaybetme akışı ve penceresi vardır. Bazı kurtarma bölümleri bu mini oyuna bağlanır (10.2.7). Bölüm ve nesne verileri kendi tasarımın. | yüksek (yapı) |

### 10.9 Basamaklı toplama olayı şablonu

Birçok olay aynı iskeleti paylaşır: bölüm kazandıkça puan toplanır, puan basamak hedeflerini doldurur, her basamak bir ödül açar. Olaylar birbirinden tema, görsel ve ek mini mekaniklerle ayrılır.

| No | Kural | Güven |
|---|---|---|
| 10.9.1 | Şablondan türetilen her olayın bir açılış bölümü (parametre: acilis_ilk_toplama_olayi, olay_turu_acilis_araligi), bir süresi, bir basamak tablosu ve basamak ödülleri vardır (parametre: toplama_olayi_basamak_sayisi, toplama_olayi_suresi_gun). | yüksek (yapı), kendi tasarımın (sayılar) |
| 10.9.2 | **Bölüm kazanımında verilen puan** = zorluk etiketine göre taban değer (9.8.4'teki tablo) × çarpan. Çarpan yalnızca bölüm çarpanla oynandıysa uygulanır (9.10.2). Aynı puan haftalık ödül hattına da verilir (9.9.4). | yüksek |
| 10.9.3 | Basamaklar sırayla dolar. Ödüller tek tek ya da toplu alınabilir. Bütün basamaklar bitince olay oyuncu için tamamlanır. | yüksek |
| 10.9.4 | Olay temaları için öneriler: bahçe büyütme (her basamak bir çiçek açar), köprü inşası (her basamak bir ayak ekler), orkestra kurma (her basamak bir çalgı ekler), kış kalesi (her basamak bir duvar örer). Ek mini mekanikler kendi tasarımın. | kendi tasarımın |
| 10.9.5 | Bazı şablon olayları başka olaylara ödül besler; örneğin basamak ödülü olarak bilye (10.6.2) ya da enerji ödülü (10.4.8). | orta |
| 10.9.6 | Hafta sonu çarpan olayları gibi kısa süreli etkinlikler, şablonun puan formülüne yalnızca çarpanı ekler (9.10). | orta |

---

## 11. Açık Sorular

Bu bölüm belgedeki belirsizlikleri ve olası çelişkileri bir arada tutar. Her madde bir varsayılanla uygulanır. Oyun testinde netleşen madde, ilgili kural ve test güncellenerek buradan silinir.

### 11.1 Ayarlanabilir kurallar

Bu kuralların hepsi bir bayrakla değiştirilebilir (0.3). Doğrulama sütunu, oyun testinde neye bakılacağını özetler.

| Kimlik | Konu | Bölüm | Varsayılan | Doğrulama |
|---|---|---|---|---|
| AK-01 | Kaskadda doğum hücresi ve satır ekseni | 2.7.4 | A: en son oturan; eşitlikte en alttaki | Blok halinde düşen dikey 4'lüde çizgi taşının doğduğu satır. |
| AK-02 | Hamle bitince akışın sırası | 7.4.1 | A: kaskad bekle → yedek hak → teklif → kayıp | Son hamlenin kaskadı hedefi bitirince sonuç; yedek hak varken pencere sırası. |
| AK-03 | Çapraz kaymanın koşulları | 5.2.6 | A: taş gelmeyecek her boşluk | Üstünden taş gelmekte olan bir boşluğa çapraz kayma olup olmadığı. |
| AK-04 | Girişi olmayan sütunun dolması | 5.3.1 | A: yalnız çapraz kaymayla | Üstü kapalı bir sütunda boşluğun nereden dolduğu. |
| AK-05 | Güdümlü taşın hedef önceliği | 3.4.5 | A: puan tablosu | Hedef engel ile hedef olmayan engel arasında seçim; tekrarlarda dağılım. |
| AK-06 | Eşit öncelikli adayların çakışması | 2.6.5 | A: tarama sırası | 2×3 blokta güdümlü taşın hangi karede doğduğu. |
| AK-07 | Renk küresi + güdümlüde kürenin hücresi | 4.3.9 | A: ek güdümlü var | Uçan güdümlü sayısı ile dönüşen taş sayısının farkı. |
| AK-08 | En yaygın renkte eşitlik | 3.5.2 | A: tarama sırasında ilk görülen | Eşit sayılı iki renkte dokunulan kürenin seçtiği renk. |
| AK-09 | Zincirle vurulan renk küresinde renk sayımının anı | 3.5.3 | A: sayım tahta durulduktan sonra | Vurulma ile ışınlar arasında düşüp oturan taşların sayıma ve hedefe girip girmediği. |
| AK-10 | Öncelikte artan hücreler ve 6'lı sıra | 2.6.6 | A: uzatma açık | Öncelikli grubun dışında kalan aynı renkli bitişik taşların patlayıp patlamadığı. |
| AK-11 | Aynı eşleşmeden çoklu vuruş | 6.1.3 | A: grup başına en fazla 1 vuruş | İki katmanlı bloğa iki hücresiyle değen L eşleşmesinin etkisi. |
| AK-12 | Bomba + bomba merkezi | 4.3.3 | A: gidilen hücre | Kenara yakın kombinasyonda temizlenen en dış sütun. |
| AK-13 | Karıştırma başarısızlığı | 5.6.5 | A: tahtayı yeniden kur, bölüm sürer | Karıştırma öncesi ve sonrası renk sayıları; başarısızlıkta bölümün sonucu. |
| AK-14 | Çift renk küresinin ikinci renge geçişi | 3.6.4 | A: ilk rengin hedefleri vurulunca, durulmadan | İkinci renk sayımına birinci rengin vuruşlarından sonra düşen taşların girip girmediği. |
| AK-15 | Çift renk kombinasyonlarında renk sayısı | 4.4.1 | A: iki renk | Dönüşen taşların kaç renkten geldiği. |

### 11.2 Diğer açık sorular

| No | Konu | Bölüm | Şu anki varsayılan | Güven |
|---|---|---|---|---|
| 11.2.1 | Çizgi taşı oynanmayan bir hücreye gelince durur mu? Etkinin komşu hücre kalmayınca bittiği biliniyor; oynanmayan hücreler komşu sayılmıyorsa çizgi orada durur. Testte iki davranış bir geliştirici seçeneğiyle denenir. | 3.2.4 | Durmaz, atlayıp devam eder. | düşük |
| 11.2.2 | Renk ışını kilitli bir hedef taşa ne yapar? | 3.5.8 | Kilide bir vuruş; taş yerinde kalır. | düşük |
| 11.2.3 | Oyun içi araç kullanmak hamle harcar mı? | 3.9.5, 7.2.4 | Harcamaz. | düşük |
| 11.2.4 | Takviye çizgi taşının yön puanı neyi sayar ve takviye taşı hangi hücreye konur? | 3.9.2, 3.9.3 | Hedef ve engel sayısı; hedeflere yakın, olası eşleşmeyi bozmayan hücre. | düşük |
| 11.2.5 | Güdümlü + güdümlü kombinasyonunda kalkıştan önce hangi alanlar patlar? | 4.3.6 | Gidilen hücrede tek bir artı alanı. | düşük |
| 11.2.6 | Renk küresi + bomba kombinasyonunda kürenin hücresine de bomba konur mu? Diğer iki dönüşüm kombinasyonunda kürenin hücresine taş konduğu biliniyor; bomba için bilinmiyor. | 4.3.8 | Konmaz. | düşük |
| 11.2.7 | Hangi engeller hangi vuruş türlerine bağışıktır? | 6.1.5 | Bağışıklık yok; engel tanımında bayrakla eklenir. | orta |
| 11.2.8 | Üst katmanı olan hücrede vuruş sırası nedir? Katman ile alttaki taş aynı vuruşta birlikte etkilenir mi? | 6.1.7, 6.2.5 | Önce üst katman; taş katman kırılınca etkilenir. | düşük |
| 11.2.9 | Hangi özel taş patlamaları zemin yayar? | 6.3.1 | Bomba ve çizgi yayar; güdümlü ve renk ışını yaymaz. | düşük |
| 11.2.10 | Hamle sonu hareketli engellerin kesin kuralları. | 6.3.4 | Her engel türü için ayrı tasarlanır. | düşük |
| 11.2.11 | Başlangıçta izinli renk kalmazsa ne yapılır? Tek olası hamle renk küresi hamlesiyse ne yapılır? | 5.4.5, 5.4.6 | Kısıt gevşetilir; tek hamle renk küresi hamlesiyse karıştırılır. | düşük |
| 11.2.12 | Karıştırmada hangi taşlar yer değiştirir ve karıştırma hamle harcar mı? | 5.6.3, 5.6.4 | Yalnız serbest normal taşlar; hamle harcamaz. | düşük |
| 11.2.13 | Güncelleme sırası. | 5.8.1 | 5.8.1'deki sıra. | orta |
| 11.2.14 | Başlamış bir bölümden çıkmak kayıp sayılır mı? | 9.1.3, 10.5.5 | Sayılır. | düşük |
| 11.2.15 | Zorluk etiketi nerede tutulur? Zor bölüm ritmi etiketle aynı şey mi? | 8.8.1a, 8.8.3 | Ayrı etiket tablosunda; sunucu değiştirebilir. Ritim ayrı bir tasarım aracıdır. | orta |
| 11.2.16 | Hat puanındaki "özel durum" hangi bölümleri kapsar? | 9.8.4 | Sunucunun işaretlediği etkinlik bölümleri. | düşük |
| 11.2.17 | Sezon hattı puanına çarpan uygulanır mı? Haftalık hatta uygulandığı kesin. Sezon hattı çarpılabilir özellikler arasında işaretli, ama puanın eklendiği yolda çarpan görülmedi; başka bir yerde uygulanıp uygulanmadığı bilinmiyor. | 9.8.5, 9.10.3 | Uygulanmaz. | düşük |
| 11.2.18 | Sınırsız takviye ve can süreleri hangi durumlarda duraklatılır? | 9.1.7, 9.3.6 | Yarışmalı olayların bölümlerinde. | düşük |
| 11.2.19 | Seri takviyesinin içeriği. | 9.3.9 | Her kademe bir çizgi taşı ekler; kayıpta sıfırlanır. | düşük |
| 11.2.20 | Yarışmalı bölümlerde hamleye eklenen değerin formülü ve beceri eşikleri. | 9.11.2, 9.11.3 | Parametre dosyasındaki öneri. | düşük |
| 11.2.21 | Risk basamaklarınde son hak gidince haklar yeniden dolar mı? | 10.5.4 | Dolar. | düşük |
| 11.2.22 | Kurtarma bölümünde kayıp can düşürür mü, hedef yapısı nedir? | 10.2.8 | Can düşmez; tek hedef. | düşük |
| 11.2.23 | Hız yarışında puan nasıl hesaplanır, kalan hamle puana etki eder mi? | 10.1.4, 10.1.6 | Kazanım başına sabit puan; kalan hamle yalnız eşitlik bozar. | düşük |
| 11.2.24 | Yedek hamle hakkının tetiklenme eşiği ve verdiği hamle. | 3.8.2 | Parametre dosyasındaki öneri. | düşük |
| 11.2.25 | Özel taşın renk eşleşmesine girmediği. | 2.2.3 | Girmez. | orta |
| 11.2.26 | Hedefi olmayan renk küresine dokunmak hamle düşürür mü? | 2.3.6 | Düşürmez. | orta |
| 11.2.27 | Düşme ve kaskad sürerken hamle kabul edilir mi? | 2.3.7, 2.3.9 | Edilir. | düşük |
| 11.2.28 | Çift renk modunda başlangıç takviyeleri ayrıca konur mu, yoksa tek bir çift renk küresinde mi birleşir? | 9.3.8 | Birleşir. | düşük |
| 11.2.29 | Özel taş normal taşla kaydırıldığında normal taşın yeni yerinde eşleşme oluşursa ne olur? | 5.7 | Eşleşme aynı hamlede, özel taşın etkisinden önce patlar ve özel taş üretebilir; ardından özel taş etkinleşir. | düşük |
| 11.2.30 | İkinci olay kuralındaki süre katılınacak olaydan mı, mevcut olaydan mı ölçülür? | 9.4.7 | Mevcut (ilk) olaydan. | düşük |

### 11.3 Bilinen gerilimler

| No | Konu | Açıklama | Karar |
|---|---|---|---|
| 11.3.1 | Kaskad bekleme koşulu ile hamle serbestliği | 5.5.1'e göre kaskad yalnızca süren bir işlem yokken çalışır; 2.3.9'daki öneriye göre oyuncu düşme sürerken de hamle yapabilir. İkisi birlikte, oyuncunun hamlesi ile kaskadın aynı karede çakışmasına izin verir. | Oyuncu hamlesi her zaman önce işlenir; kaskad bir sonraki güncellemeye kalır (5.8.1 sırası). |
| 11.3.2 | Başlangıç takviyelerinin açılışı ile özel taşların tanıtımı | Başlangıç takviyeleri, aynı özel taşların tahtaya hazır konduğu ilk bölümlerden önce açılır (8.6.5). Oyuncu bir özel taşı ilk kez takviye olarak görebilir. | Bilinçli tasarım: takviye ekranı her özel taşı kısa bir açıklamayla tanıtır. |
| 11.3.3 | Ek hamle teklifi ile yedek hamle hakkı | İkisi de hamle bitince devreye girer; sıraları AK-02'ye bağlıdır. | Varsayılan A: önce yedek hak. |
| 11.3.4 | Sezon ve haftalık hattın aynı puan tablosunu paylaşması | Haftalık hat ve olaylar puanı çarpanla alır, sezon hattı almaz (9.10). Aynı bölümden iki hatta farklı puan düşer. | Varsayılan; oyun testinde doğrulanacak. Arayüzde iki puan ayrı gösterilir. |
