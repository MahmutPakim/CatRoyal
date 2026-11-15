# Proje — Test Vakaları

Bu belgedeki vakalar projenin oyun tasarım dokümanındaki (GDD) kurallara bağlıdır. Her vakanın dayanağı GDD bölüm ve kural numarasıyla yazılır. Tahtaların hepsi bu belge için uydurulmuştur.

- Rastgelelik içeren sonuçlar kesin tahta yerine **özellik testi** olarak yazılmıştır.
- Ayarlanabilir bir kurala (AK-xx, GDD 0.3) bağlı vakalar o kuralın **varsayılan varyantını (A)** bekler. B varyantı için aynı vakalar ilgili bayrak B'ye çevrilerek ayrı bir test kümesi olarak çalıştırılır. B varyantında sonucu değişen vakalarda tek satırlık bir "AK-xx B ile:" notu vardır.
- Ekonomi ve ayar değerleri metinde "parametre: ad" biçiminde geçer. Örnek hesaplar parametre dosyasındaki öneri değerle yapılır (GDD 0.4).

## Okuma kuralları

**Vaka biçimi:** Her vakada şu kısımlar var:
- tahta durumu
- hamle
- beklenen sonuç
- dayanak: GDD kural numarası (ör. GDD 2.5.2) ve varsa ayarlanabilir kural (AK-xx)
- güven

**Güven:** Vakanın dayandığı en zayıf kuralın güvenidir. Düzeyler GDD'deki gibi: yüksek / orta / düşük (GDD 0.2). Bir vakanın notunda kendi güveniyle işaretlenmiş bir **ek beklenti** varsa, o beklenti ayrı raporlanır ve vakanın güven etiketine katılmaz.

**Özellik testi:** Rastgelelik içeren sonuçlarda kesin tahta yazılmadı. Bunlarda "şu özelliği sağlamalı" biçiminde bir özellik testi var. Örnek konular: renk seçimi, doldurma, güdümlü taş hedefi.

**Durum testi:** Bazı tahtalar tek bir oyuncu hamlesiyle kurulamaz. Örnek: artı şekli ya da durmuş bir tahtada boş hücre. Bu vakalarda tahta test ortamında doğrudan kurulur ve ilgili adım (eşleşme arama, düşme) çağrılır.

**Koordinatlar:**
- Biçim (satır, sütun). Satır 1 en üstte, sütun 1 en solda.
- Bu yalnızca görsel düzendir. Kodun iç satır ekseninin yönü serbesttir; yönün sonucu etkilediği yer için GDD 0.5 ve AK-01.

**Hamle gösterimi:**
- "(a,b) → (c,d)": oyuncu (a,b)'deki taşı tutup (c,d)'ye kaydırır.
- "Gidilen hücre" (c,d), "çıkılan hücre" (a,b).
- Sıra yalnızca kombinasyonlarda ve doğum hücresinde önemli. Diğer vakalarda iki taşın yer değiştirmesi yeterli.

**Varsayılan kurulum:**
- Giriş hücreleri her sütunun üstünde.
- Kalan hamle sayısı belirtilmedikçe önemsiz.
- Tahtada hedef yok.
- Bütün ayarlanabilir kurallar varsayılan varyantta (A).

## Ortak semboller

Her vakanın başında, o vakada kullanılan semboller ayrıca yazıldı.

**Taşlar ve özel taşlar**

| Sembol | Anlamı |
|---|---|
| K, M, Y, S | Kırmızı, mavi, yeşil, sarı taş |
| · | Nötr taş. Testte değeri önemsiz. Kurulumda hiçbir eşleşmeye katılmayacak ve vakadaki renklerle 3'lü ya da kare oluşturmayacak biçimde seçilir. |
| a, b, c, d, e, f | Takip edilen nötr taş. Rengi önemsiz; yalnızca nereye düştüğü izlenir. |
| - | Yatay çizgi taşı (yatay bir çizgi) |
| \| | Dikey çizgi taşı (dikey bir çizgi) |
| B | Bomba taşı |
| G | Güdümlü taş |
| R | Renk küresi |
| _ | Boş ama oynanabilir hücre (yalnız durum testlerinde) |
| X | Oynanmayan hücre (tahta şekli) |

**Engeller ve katmanlar**

| Sembol | Anlamı |
|---|---|
| 1, 2, 3, 4 | O sayıda katmanı olan katmanlı blok |
| W | Sert blok, 3 katman |
| ~ | Yapışkan kaplamalı taş (altındaki rengin önemi yok) |
| # | Kilitli taş |
| Z | Yayılan zemin; ayrı bir "katman haritası"nda gösterilir |

---

## A. Eşleşme tespiti ve özel taş oluşumu (15 vaka)

### A01 — Düz 3'lü özel taş üretmez
- **Semboller:** K kırmızı, M mavi, · nötr.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · K · ·
  3  K K M · ·
  4  · · · · ·
```
- **Hamle:** (2,3) → (3,3)
- **Beklenen:**
  - (3,1), (3,2), (3,3)'teki üç K patlar.
  - Hiçbir özel taş oluşmaz.
  - (2,3)'e çıkan M yerinde kalır.
- **Dayanak:** GDD 2.4.1, 2.4.3, 2.5.1, 2.7.6
- **Güven:** yüksek

### A02 — Yatay 4'lü dikey çizgi taşı üretir; çizgi taşı kaydırılan taşın indiği hücrede doğar
- **Semboller:** K, M, · ; "|" dikey çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · K · ·
  3  K K M K ·
  4  · · · · ·
```
- **Hamle:** (2,3) → (3,3)
- **Beklenen:**
  - Satır 3'te (3,1)–(3,4) dört K patlar.
  - (3,3)'te bir **dikey** çizgi taşı ("|") oluşur. Çizgi taşının yönü eşleşme sırasına diktir.
  - Yatay çizgi taşı oluşmaz.
- **Dayanak:** GDD 2.5.2, 2.5.10, 2.7.2, 2.7.3
- **Güven:** yüksek

### A03 — Dikey 4'lü yatay çizgi taşı üretir
- **Semboller:** K, M, · ; "-" yatay çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · K · ·
  2  · · K · ·
  3  · K M · ·
  4  · · K · ·
  5  · · · · ·
```
- **Hamle:** (3,2) → (3,3)
- **Beklenen:**
  - Sütun 3'te (1,3)–(4,3) dört K patlar.
  - (3,3)'te bir **yatay** çizgi taşı ("-") oluşur.
  - (3,2)'ye geçen M yerinde kalır.
- **Dayanak:** GDD 2.5.3, 2.5.10, 2.7.2, 2.7.3
- **Güven:** yüksek

### A04 — Düz 5'li renk küresi üretir, çizgi taşı adayları elenir
- **Semboller:** K, M, · ; R renk küresi.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · K · ·
  3  K K M K K
  4  · · · · ·
```
- **Hamle:** (2,3) → (3,3)
- **Beklenen:**
  - Satır 3'ün beş K'si patlar.
  - (3,3)'te tek bir renk küresi oluşur.
  - Aynı hücrelerden çıkabilecek çizgi taşlarının hiçbiri oluşmaz.
- **Dayanak:** GDD 2.5.4, 2.6.1, 2.6.2, 2.7.2, 2.7.3
- **Güven:** yüksek

### A05 — 2×2 kare güdümlü taş üretir
- **Semboller:** K, M, · ; G güdümlü taş.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · K K · ·
  3  · K M K ·
  4  · · · · ·
```
- **Hamle:** (3,4) → (3,3)
- **Beklenen:**
  - (2,2), (2,3), (3,2), (3,3) karesi patlar.
  - (3,3)'te bir güdümlü taş oluşur.
  - Kare aynı zamanda düz 3'lü adayı olarak da kayda giriyor, ama öncelikte kare (güdümlü taş) kazanır.
  - (3,4)'e geçen M yerinde kalır.
- **Dayanak:** GDD 2.4.2, 2.5.8, 2.5.11, 2.7.2, 2.7.3
- **Güven:** yüksek

### A06 — L şekli bomba taşı üretir, köşede (kaydırılan taşın yerinde)
- **Semboller:** K, M, · ; B bomba taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  K · · · ·
  2  K · · · ·
  3  M K K · ·
  4  K · · · ·
```
- **Hamle:** (4,1) → (3,1)
- **Beklenen:**
  - (1,1), (2,1), (3,1), (3,2), (3,3) patlar.
  - (3,1)'de bir bomba taşı oluşur.
- **Dayanak:** GDD 2.5.5, 2.7.2, 2.7.3
- **Güven:** yüksek
- **Not:** Geçerli bir oyuncu hamlesinde kaydırılan taş L ve T şekillerinde her zaman kesişim hücresine düşer. Kolun ucuna düşerse diğer kol önceden hazır eşleşme olur; durgun tahtada bu mümkün değil (GDD 2.7.3).

### A07 — T şekli bomba taşı üretir
- **Semboller:** K, M, · ; B bomba taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · K · · ·
  3  K M K · ·
  4  · K · · ·
  5  · K · · ·
```
- **Hamle:** (2,2) → (3,2)
- **Beklenen:**
  - Satır 3'te (3,1)–(3,3) ve sütun 2'de (3,2)–(5,2) patlar; toplam 5 hücre.
  - (3,2)'de bir bomba taşı oluşur.
- **Dayanak:** GDD 2.5.6, 2.7.2, 2.7.3
- **Güven:** yüksek

### A08 — Artı şekli bomba taşı üretir (durum testi)
- **Semboller:** K, · ; B bomba taşı.
- **Tahta** (eşleşme arama doğrudan çağrılır):
```
     1 2 3 4 5
  1  · · · · ·
  2  · · K · ·
  3  · K K K ·
  4  · · K · ·
  5  · · · · ·
```
- **İşlem:** Tahtadaki bütün eşleşmeleri bul ve patlat (kaskad adımı).
- **Beklenen:**
  - Tek bir grup bulunur: beş K. İki 3'lü orta hücrelerinde, yani (3,3)'te birleşir.
  - Grup bomba taşı üretir.
  - Doğum hücresi bu testte doğrulanmaz, çünkü oyuncu hamlesi yok (kaskad doğum hücresi GDD 2.7.4).
- **Dayanak:** GDD 2.5.7, 2.5 tablosu
- **Güven:** yüksek
- **Not:** Artı şekli tek bir oyuncu hamlesiyle kurulamaz. Merkeze gelen taş kollardan birini boşaltır. Oyunda ancak kaskadla oluşur.

### A09 — Öncelik: 5'li ile dikey 3'lü aynı hücrede kesişirse yalnız renk küresi oluşur
- **Semboller:** K, M, · ; R renk küresi.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · K · ·
  3  K K M K K
  4  · · K · ·
  5  · · K · ·
```
- **Hamle:** (2,3) → (3,3)
- **Beklenen:**
  - Satır 3'te 5'li ve sütun 3'te (3,3)–(5,3) dikey 3'lü oluşur.
  - (3,3)'te **tek bir renk küresi** oluşur.
  - Aynı hamlede bomba taşı ya da çizgi taşı **oluşmaz**.
- **Dayanak:** GDD 2.6.1, 2.6.2
- **Güven:** yüksek
- **Not (ek beklenti, güven düşük; GDD 2.6.6, AK-10):** (4,3) ve (5,3)'teki K'ler atlanan adaydan kalan artık hücrelerdir. AK-10 A (uzatma açık) ile kabul edilen gruba bitişik ve aynı renkte oldukları için gruba eklenir ve grupla birlikte patlar; ayrı özel taş üretmezler.
- **AK-10 B ile:** (4,3) ve (5,3)'teki K'ler yerinde kalır.

### A10 — Öncelik: L, T'den ve 4'lüden önce gelir
- **Semboller:** K, M, · ; B bomba taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · K · · ·
  2  · K · · ·
  3  K M K K ·
  4  · K · · ·
```
- **Hamle:** (4,2) → (3,2)
- **Beklenen:**
  - Satır 3'te (3,1)–(3,4) 4'lü oluşur; sütun 2'de (1,2)–(3,2) 3'lü oluşur.
  - Adaylar:
    - L: (3,2)–(3,4) ve sütun.
    - T: (3,1)–(3,3) ve sütun.
    - Dikey çizgi taşı: 4'lü.
  - Önce L seçilir: (1,2), (2,2), (3,2), (3,3), (3,4) patlar.
  - (3,2)'de **bomba taşı** oluşur. **Çizgi taşı oluşmaz.**
- **Dayanak:** GDD 2.6.1, 2.6.2, 2.6.4, 2.7.2, 2.7.3
- **Güven:** yüksek
- **Not (ek beklenti, güven düşük; GDD 2.6.6, AK-10):** (3,1)'deki K artık hücredir. AK-10 A ile L grubuna bitişik ve aynı renkte olduğu için gruba eklenir ve patlar; grubun şekli ve ürettiği bomba taşı değişmez.
- **AK-10 B ile:** (3,1)'deki K yerinde kalır.

### A11 — Öncelik: kare, kendisiyle çakışan düz 3'lüden önce gelir
- **Semboller:** K, M, · ; G güdümlü taş.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · K K · ·
  3  · K M K ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Kare: (2,2), (2,3), (3,2), (3,3). Düz 3'lü: (3,2)–(3,4).
  - Kare kabul edilir ve (3,3)'te güdümlü taş oluşur.
  - Düz 3'lü, hücreleri kareyle ortak olduğu için ayrı bir grup olarak patlamaz.
- **Dayanak:** GDD 2.5.11, 2.6.1, 2.6.2
- **Güven:** yüksek
- **Not (ek beklenti, güven düşük; GDD 2.6.6, AK-10):** (3,4)'teki K artık hücredir. AK-10 A ile kareye bitişik ve aynı renkte olduğu için gruba eklenir ve patlar; yine yalnız bir güdümlü taş oluşur.
- **AK-10 B ile:** (3,4)'teki K yerinde kalır.

### A12 — Örtüşmeyen iki eşleşme tek hamlede iki özel taş üretir
- **Semboller:** K, M, · ; "-" yatay çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · M K ·
  2  · · M K ·
  3  · · K M ·
  4  · · M K ·
```
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - Sütun 3'te (1,3)–(4,3) dört M, sütun 4'te (1,4)–(4,4) dört K patlar.
  - (3,4)'te bir yatay çizgi taşı oluşur: oraya inen K'nin grubu.
  - (3,3)'te bir yatay çizgi taşı daha oluşur: oraya inen M'nin grubu.
- **Dayanak:** GDD 2.5.3, 2.6.3, 2.7.2
- **Güven:** orta (GDD 2.6.3 orta)

### A13 — Eşleşmeyi karşı taş kurarsa özel taş çıkılan hücrede doğar
- **Semboller:** K, M, · ; "|" dikey çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  M M K M ·
  3  · · M · ·
  4  · · · · ·
```
- **Hamle:** (2,3) → (3,3). Oyuncu K'yi aşağı kaydırır; M yukarı çıkar.
- **Beklenen:**
  - Satır 2'de (2,1)–(2,4) dört M patlar.
  - Dikey çizgi taşı **(2,3)'te**, yani M'nin indiği çıkılan hücrede doğar.
  - (3,3)'e inen K eşleşmeye girmez.
- **Dayanak:** GDD 2.7.2
- **Güven:** yüksek

### A14 — Kaskadla oluşan 4'lüde doğum hücresi (özellik testi)
- **Semboller:** K, M, Y, · ; "|" dikey çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · K · ·
  2  · · Y · ·
  3  · · Y · ·
  4  K K M K ·
  5  · · Y · ·
```
- **Hamle:** (5,3) → (4,3)
- **Beklenen (adım adım):**
  1. Sütun 3'te (2,3)–(4,3) üç Y patlar; özel taş oluşmaz.
  2. (1,3)'teki K, (4,3)'e düşer. Satır 4'te (4,1)–(4,4) dört K kaskadla eşleşir.
  3. Bir dikey çizgi taşı oluşur.
- **Sağlanması gereken özellikler:**
  - Çizgi taşı, grubun dört hücresinden birinde doğar.
  - Çizgi taşı kilitli bir hücrede doğmaz (GDD 2.7.5).
  - Çizgi taşı, gruba en son yerine oturan taşın hücresinde doğar; bu tahtada (4,3). Gruba yeni oturan tek taş (4,3)'e düşen K'dir, bu yüzden eşitlik kuralı devreye girmez.
  - Aynı anda oturan birden fazla taş olsaydı AK-01 A ile görsel olarak en alttaki, yatay grupta eşitlik sürerse en soldaki seçilirdi.
  - Hamle sayacı yalnızca 1 azalır.
- **Dayanak:** GDD 2.7.4, 2.7.5, 5.5.1, 5.5.2, 5.5.3, AK-01
- **Güven:** orta
- **Not:** Yeni dolan taşlar rastgele olduğu için ek kaskadlar çıkabilir. Test yalnızca bu dört K'nin grubunu izler. Kaskadda doğum hücresi seçimine zorluğa bağlı bir rastgelelik eklenebilir (GDD 2.7.7); test bu rastgeleliği etkisiz ağırlıkla çalıştırır.
- **AK-01 B ile:** bu tahtada sonuç aynıdır, (4,3).

### A15 — Eşit öncelikli iki aday çakışırsa yalnız biri kabul edilir (durum testi)
- **Semboller:** K, · ; G güdümlü taş.
- **Tahta** (eşleşme arama doğrudan çağrılır):
```
     1 2 3 4 5
  1  · · · · ·
  2  · K K K ·
  3  · K K K ·
  4  · · · · ·
```
- **İşlem:** Tahtadaki bütün eşleşmeleri bul ve patlat.
- **Sağlanması gereken özellikler:**
  - İki kare adayı çakışıyor: sütun 2–3 ve sütun 3–4. **Tam olarak bir** güdümlü taş oluşur.
  - AK-06 A (tarama sırası; sol-üst hücreden başlayıp satır satır) ile önce bulunan kare kabul edilir: **sütun 2–3'teki kare**, yani (2,2), (2,3), (3,2), (3,3).
  - Satır 2 ve satır 3'teki düz 3'lü adayları kabul edilen kareyle çakıştığı için atlanır.
  - Çizgi taşı ya da bomba taşı oluşmaz.
  - Kabul edilen karenin dışında kalan iki K, (2,4) ve (3,4), ayrı bir özel taş üretmez.
- **Dayanak:** GDD 2.5.8, 2.6.1, 2.6.2, 2.6.5, AK-06
- **Güven:** orta
- **Not (ek beklenti, güven düşük; GDD 2.6.6, AK-10):** AK-10 A ile (2,4) ve (3,4)'teki K'ler kareye bitişik artık hücreler olarak gruba eklenir ve patlar. AK-10 B ile yerinde kalır.
- **AK-06 B ile:** bu bir kaskad adımı olduğu için tarama sırası yine geçerlidir; sonuç aynıdır.

---

## B. Özel taşların tek başına etkisi (10 vaka)

### B01 — Yatay çizgi taşı satırını temizler; engel yolu kesmez
- **Semboller:** K, · ; "-" yatay çizgi taşı; 2 iki katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · · · ·
  3  · - K · 2
  4  · · · · ·
  5  · · · · ·
```
- **Hamle:** (3,2) → (3,3). Çizgi taşı satır 3'te kalır.
- **Beklenen:**
  - Satır 3'ün bütün hücreleri vurulur: (3,1), (3,2)'ye geçen K ve (3,4).
  - (3,5)'teki katmanlı blok 2 katmandan 1'e iner.
  - Çizgi taşı iki yöne, sola ve sağa ilerler ve katmanlı blokta durmaz.
  - Diğer satırlar patlamadan etkilenmez.
  - Hamle geçerlidir, çünkü hamlede özel taş var.
- **Dayanak:** GDD 3.2.1, 3.2.2, 3.1.1, 6.1.1, 2.3.2–2.3.4
- **Güven:** yüksek

### B02 — Dikey çizgi taşı sütununu temizler; tek katmanlı bloğu kırar ve devam eder
- **Semboller:** K, · ; "|" dikey çizgi taşı; 1 tek katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · | · ·
  3  · · K · ·
  4  · · 1 · ·
  5  · · K · ·
```
- **Hamle:** (2,3) → (3,3). Çizgi taşı sütun 3'te kalır.
- **Beklenen:**
  - Sütun 3'ün bütün hücreleri vurulur: (1,3), (2,3)'e geçen K, (4,3) ve (5,3).
  - (4,3)'teki katmanlı blok kırılır.
  - Çizgi taşı bloğun ötesine geçip (5,3)'ü de temizler.
- **Dayanak:** GDD 3.2.1, 3.2.2, 6.1.1
- **Güven:** yüksek

### B03 — Bomba taşı dokunmayla 5×5 kare patlatır
- **Semboller:** · ; B bomba taşı.
- **Tahta:** 7×7, bütün hücreler nötr; (4,4)'te B.
```
     1 2 3 4 5 6 7
  1  · · · · · · ·
  2  · · · · · · ·
  3  · · · · · · ·
  4  · · · B · · ·
  5  · · · · · · ·
  6  · · · · · · ·
  7  · · · · · · ·
```
- **Hamle:** (4,4)'teki bomba taşına dokun.
- **Beklenen:**
  - Satır 2–6 ile sütun 2–6'nın kesişimindeki 25 hücre vurulur. Köşe hücreleri (2,2), (2,6), (6,2), (6,6) de dahil; şekil eşkenar dörtgen değil kare.
  - Satır 1 ve 7, sütun 1 ve 7 vurulmaz.
  - Patlama içten dışa ilerler: merkez, sonra 8 hücrelik iç halka, sonra 16 hücrelik dış halka (parametre: sunum_bomba_baslangic_gecikmesi, sunum_bomba_halka_arasi).
  - Kalan hamle 1 azalır.
- **Dayanak:** GDD 3.3.1, 3.3.2, 3.3.3, 3.1.1 (dokunma: 2.3.5), 7.2.1
- **Güven:** orta (GDD 3.1.1 dokunmayla etkinleştirme orta)

### B04 — Köşedeki bomba taşı tahta dışına taşmaz
- **Semboller:** · ; B bomba taşı.
- **Tahta:** 5×5 nötr; (1,1)'de B.
- **Hamle:** (1,1)'deki bomba taşına dokun.
- **Beklenen:**
  - Yalnızca (1,1)–(3,3) arasındaki 9 hücre vurulur.
  - Tahta dışındaki hücreler yok sayılır; hata oluşmaz.
- **Dayanak:** GDD 3.3.1, 3.3.2, 3.3.3, 3.1.1 (dokunma: 2.3.5)
- **Güven:** orta

### B05 — Güdümlü taş artı alanını patlatır, sonra tek bir hedefe uçar (özellik testi)
- **Semboller:** · ; G güdümlü taş; 1 tek katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · · · ·
  3  · · G · ·
  4  · · · · ·
  5  · · · · 1
```
- **Hamle:** (3,3)'teki güdümlü taşa dokun.
- **Beklenen:**
  - Önce (3,3) ile dört dik komşusu (2,3), (4,3), (3,2), (3,4) patlar. Çaprazlar patlamaz.
  - Ardından güdümlü taş havalanır ve **tam olarak bir** hedefe iner; orayı vurur.
- **Sağlanması gereken özellik:** Hedef, adayların puan sırasında en üst gruptan seçilir; eşitlikte rastgele seçilir. Varsayılan kurulumda bölüm hedefi yoktur. AK-05 A (puan tablosu) ile tek engel olan katmanlı bloğun puanı (parametre: gudumlu_puan_engel ve gudumlu_puan_katman_basina) normal taşların puanından (parametre: gudumlu_puan_normal_tas) yüksektir; bu yüzden (5,5)'teki bloğun seçilmesi beklenir.
- **Dayanak:** GDD 3.4.1, 3.4.3, 3.4.4, 3.4.5, 3.1.1 (dokunma: 2.3.5), AK-05
- **Güven:** orta
- **AK-05 B ile:** hedef öğe olmadığı için A uygulanır; sonuç aynıdır.

### B06 — Renk küresini normal bir taşla yer değiştirmek o rengin bütün taşlarını temizler
- **Semboller:** K kırmızı; · K olmayan, eşleşme oluşturmayan taş; R renk küresi.
- **Tahta:**
```
     1 2 3 4 5
  1  K · · · ·
  2  · · · K ·
  3  · · R K ·
  4  · K · · ·
  5  · · · · K
```
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - Tahtadaki 5 K'nin hepsi patlar. Bunlara (3,3)'e geçen K de dahil.
  - Renk küresi tükenir.
  - Diğer renklerden hiçbir taş ışınla patlamaz.
- **Dayanak:** GDD 3.5.1, 3.5.3
- **Güven:** yüksek

### B07 — Renk küresine dokunmak en yaygın rengi hedefler
- **Semboller:** K, M, Y, S; R renk küresi.
- **Tahta** (hazır eşleşme yok; dağılım M 9, K 6, Y 5, S 4):
```
     1 2 3 4 5
  1  M K M Y S
  2  K M S M K
  3  M Y R K M
  4  S K M Y S
  5  Y M K M Y
```
- **Hamle:** (3,3)'teki renk küresine dokun.
- **Beklenen:**
  - En yaygın renk M seçilir ve 9 M'nin hepsi patlar. Eşitlik olmadığı için AK-08 devreye girmez.
  - K, Y ve S'ye dokunulmaz.
  - Kalan hamle 1 azalır.
- **Dayanak:** GDD 3.5.2, 3.5.3, 7.2.1
- **Güven:** yüksek

### B08 — Ardışık tetikleme: çizgi taşı yolundaki bomba taşını tetikler
- **Semboller:** K, · ; "|" dikey çizgi taşı; B bomba taşı.
- **Tahta:** 7 satır × 5 sütun.
```
     1 2 3 4 5
  1  · · | · ·
  2  · · K · ·
  3  · · · · ·
  4  · · · · ·
  5  · · B · ·
  6  · · · · ·
  7  · · · · ·
```
- **Hamle:** (1,3) → (2,3)
- **Beklenen:**
  - Çizgi taşı sütun 3'ü temizler.
  - Çizgi taşının etkisi (5,3)'e varınca bomba taşı kendi etkisini başlatır: (5,3) merkezli 5×5 alan vurulur.
  - Tahta 5 sütun genişliğinde olduğu için satır 3–7'nin bütün hücreleri vurulur.
  - Sonuçta vurulan hücreler: sütun 3'ün tamamı ve satır 3–7.
  - Aynı bomba taşı ikinci kez tetiklenmez.
- **Dayanak:** GDD 3.1.2, 3.2.1, 3.2.2, 3.3.1, 3.3.3
- **Güven:** orta (GDD 3.1.2 orta)

### B09 — Başka bir patlamayla vurulan renk küresi, dalga durulduktan sonra en yaygın rengi hedefler (özellik testi)
- **Semboller:** K, M, Y, S; "-" yatay çizgi taşı; R renk küresi.
- **Tahta:**
```
     1 2 3 4 5
  1  M K M Y M
  2  K M Y M K
  3  - Y K S R
  4  M S M K M
  5  Y M K M Y
```
- **Hamle:** (3,1) → (3,2). Çizgi taşı satır 3'te kalır.
- **Beklenen:**
  1. Çizgi taşı satır 3'ü temizler ve (3,5)'teki renk küresini vurur.
  2. AK-09 A ile çizgi taşının dalgası tamamlanır ve satır 3 düşme ve doldurmayla durulur; renk küresi ancak bundan sonra en yaygın rengi sayar ve ışınlarını gönderir.
- **Sağlanması gereken özellikler:**
  - AK-09 A ile renk sayımı **tahta durulduktan sonra** yapılır. Satır 3'e düşüp oturan yeni taşlar sayıma girer.
  - Hedef renk, durulmuş tahtadaki en yaygın renk olmalıdır. Sonuç sunum_ parametrelerinin değerinden bağımsızdır: test bu parametreleri iki farklı değerle çalıştırır ve aynı sonucu bekler.
  - Bu tahtada satır 3 dışındaki dağılım M 10, K 5, Y 4, S 1. Satır 3 yeniden dolsa bile en olası hedef M.
  - Seçilen rengin bütün taşları patlar.
- **Dayanak:** GDD 3.5.2, 3.5.3 (üçüncü yol: AK-09), 3.1.2, AK-08, AK-09
- **Güven:** orta
- **AK-09 B ile:** renk sayımı çizgi taşının küreyi vurduğu adımda, satır 3 dolmadan yapılır. Satır 3'ün diğer hücreleri o adımda zaten vurulmuş olduğundan sayım satır 3 dışındaki taşlarla yapılır ve hedef M olur. Hedef liste o anda sabitlenir; satır 3'e sonradan düşen M taşları hedeflenmez.

### B10 — Özel taş yan eşleşme hasarı almaz
- **Semboller:** K, M, · ; B bomba taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · B · ·
  3  K K M · ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te üç K patlar.
  - (2,3)'teki bomba taşı patlamaz; boşalan hücreye düşer ve tahtada bomba taşı olarak kalır.
- **Dayanak:** GDD 3.1.4, 6.1.2
- **Güven:** yüksek

---

## C. Kombinasyonlar: 10 çiftin her biri (10 vaka)

C01–C06 için ortak tahta: 7×7, bütün hücreler nötr. Yalnız iki özel taşın yeri yazıldı.

C07–C10 için ortak tahta: B07'deki 5×5 dağılım. Bu tahtada (3,4)'teki K yerine ikinci özel taş var; dağılım M 9, K 5, Y 5, S 4. Taban tahta:
```
     1 2 3 4 5
  1  M K M Y S
  2  K M S M K
  3  M Y R ? M      ? = vakadaki ikinci özel taş
  4  S K M Y S
  5  Y M K M Y
```

### C01 — Çizgi + çizgi: artı şekli (iki çizgi taşı aynı yönde olsa bile)
- **Semboller:** "-" yatay çizgi taşı.
- **Tahta:** 7×7 nötr; (4,3)'te "-", (4,4)'te "-".
- **Hamle:** (4,3) → (4,4)
- **Beklenen:**
  - Satır 4'ün tamamı ve sütun 4'ün tamamı temizlenir; şekil artı.
  - Gelen çizgi taşının yönü diğerinin tersine çevrilir, bu yüzden iki yatay çizgi taşından da artı çıkar.
  - Merkez, gidilen hücre olan (4,4).
- **Dayanak:** GDD 4.1.1, 4.1.2, 4.3.1
- **Güven:** yüksek

### C02 — Bomba + çizgi: 3 satır ve 3 sütun
- **Semboller:** B bomba taşı, "-" yatay çizgi taşı.
- **Tahta:** 7×7 nötr; (4,3)'te B, (4,4)'te "-".
- **Hamle:** (4,3) → (4,4)
- **Beklenen:**
  - Gidilen hücre (4,4) merkezli olarak satır 3, 4, 5 ve sütun 3, 4, 5'in tamamı temizlenir.
  - Satır 1, 2, 6, 7'nin sütun 3–5 dışındaki hücrelerine dokunulmaz.
- **Dayanak:** GDD 4.1.2, 4.3.2
- **Güven:** yüksek

### C03 — Bomba + bomba: tek bir 9×9 patlama
- **Semboller:** B bomba taşı.
- **Tahta:** 9×9 nötr; (5,5)'te B, (5,6)'da B.
- **Hamle:** (5,6) → (5,5)
- **Beklenen:**
  - Bomba taşlarından biri devre dışı kalır; diğeri yarıçap 4 ile patlar.
  - AK-12 A ile merkez, gidilen hücre olan (5,5). Bu yüzden 9×9 tahtanın **tamamı** vurulur.
  - Patlama içten dışa halkalar halinde ilerler.
  - Güdümlü taş havalanmaz (GDD 3.4.8).
- **Dayanak:** GDD 3.3.4, 4.3.3, 3.4.8, AK-12
- **Güven:** orta (AK-12 orta)
- **AK-12 B ile:** merkez çıkılan hücre (5,6) olur ve sütun 1 vurulmaz. Test bu farkı yakalar.

### C04 — Güdümlü + çizgi: güdümlü taş çizgi taşını en iyi satıra taşır (özellik testi)
- **Semboller:** G güdümlü taş, "-" yatay çizgi taşı.
- **Tahta:** 7×7 nötr; (4,3)'te G, (4,4)'te "-".
- **Hamle:** (4,3) → (4,4)
- **Sağlanması gereken özellikler:**
  - Tam olarak **bir** güdümlü taş havalanır.
  - Güdümlü taş hedefe varınca tahtanın **bir satırının tamamı** temizlenir. Satır, çünkü taşınan çizgi taşı yatay.
  - Temizlenen satır, güdümlü hedef puanlarının (GDD 3.4.5) toplamı en yüksek olan satırdır.
- **Dayanak:** GDD 3.4.8, 4.3.4
- **Güven:** orta

### C05 — Güdümlü + bomba: güdümlü taş bomba taşını en iyi bölgeye taşır, orada 5×5 (özellik testi)
- **Semboller:** G güdümlü taş, B bomba taşı.
- **Tahta:** 7×7 nötr; (4,3)'te G, (4,4)'te B.
- **Hamle:** (4,3) → (4,4)
- **Sağlanması gereken özellikler:**
  - Tam olarak **bir** güdümlü taş havalanır.
  - İndiği yerde 5×5 kare vurulur. Kare tahta sınırında kırpılır; 9×9 değildir.
  - Hedef bölge, 5×5 alanın güdümlü hedef puanları toplamı en yüksek olan bölgedir.
- **Dayanak:** GDD 3.4.8, 4.3.5
- **Güven:** orta

### C06 — Güdümlü + güdümlü: üç güdümlü taş, üç ayrı hedef (özellik testi)
- **Semboller:** G güdümlü taş.
- **Tahta:** 7×7 nötr; (4,3)'te G, (4,4)'te G. Sayımı kolaylaştırmak için tahtaya 5 tane tek katmanlı blok dağıtılır.
- **Hamle:** (4,3) → (4,4)
- **Sağlanması gereken özellikler:**
  - Toplam **3** güdümlü taş havalanır.
  - Her biri ayrı bir hedefe iner; iki güdümlü taş aynı hedefe gitmez.
  - Kalkış açıları birbirinden 120° ayrıktır (görsel).
- **Dayanak:** GDD 3.4.4, 3.4.8, 4.3.6
- **Güven:** orta (sayı yüksek, ayrı hedef ve açılar orta)

### C07 — Renk küresi + çizgi: en yaygın rengin taşları çizgi taşına döner
- **Semboller:** C07–C10 ortak tahtası; ? = "-" (yatay çizgi taşı).
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - En yaygın renk M seçilir. 9 M'nin her biri çizgi taşına dönüşür.
  - Renk küresinin hücresine de bir çizgi taşı konur; toplam 10 çizgi taşı.
  - Yönler dönüşüm sırasına göre bir yatay bir dikey değişir; hem yatay hem dikey vardır.
  - Çizgi taşları dönüşüm sırasıyla birer birer etkinleşir; aralarındaki süre yalnızca sunumdur (parametre: sunum_renk_cizgi_patlama_arasi).
- **Dayanak:** GDD 3.5.2, 4.1.5, 4.3.7
- **Güven:** orta

### C08 — Renk küresi + bomba: en yaygın rengin taşları bomba taşına döner
- **Semboller:** C07–C10 ortak tahtası; ? = B.
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - 9 M'nin her biri bomba taşına dönüşür.
  - Bütün dönüşümler bitince bomba taşları aynı adımda patlar; aradaki süre yalnızca sunumdur (parametre: sunum_renk_bomba_beklemesi).
- **Dayanak:** GDD 3.5.2, 4.1.5, 4.3.8
- **Güven:** orta
- **Not:** Renk küresinin hücresine bomba taşı konup konmadığı bu vakada doğrulanmaz (GDD 11.2.6, düşük güven).

### C09 — Renk küresi + güdümlü: en yaygın rengin taşları güdümlü taşa döner
- **Semboller:** C07–C10 ortak tahtası; ? = G.
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - 9 M'nin her biri güdümlü taşa dönüşür.
  - AK-07 A ile renk küresinin hücresine de bir güdümlü taş konur ve o da uçar: uçan güdümlü taş sayısı = dönüşen taş sayısı + 1 = **10**.
  - Dönüşen güdümlü taşlar kalkıştan önce artı alanlarını patlatmaz (GDD 3.4.2).
  - Güdümlü taşlar dönüşüm sırasıyla birer birer havalanır ve her biri kendi hedefine gider; kalkışlar arasındaki süre yalnızca sunumdur (parametre: sunum_renk_gudumlu_kalkis_arasi).
- **Dayanak:** GDD 3.4.2, 3.4.8, 4.3.9, AK-07
- **Güven:** orta
- **AK-07 B ile:** kürenin hücresine güdümlü taş konmaz; uçan güdümlü taş sayısı 9 olur.

### C10 — Renk küresi + renk küresi: bütün tahta temizlenir
- **Semboller:** C07–C10 ortak tahtası; ? = R.
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - Tahtanın her hücresi vurulur.
  - Patlama merkezden dışa doğru halkalar halinde ilerler: önce merkeze yakın hücreler, en son köşeler (parametre: sunum_tahta_temizleme_halka_arasi).
- **Dayanak:** GDD 4.3.10
- **Güven:** orta
- **Not:** Bölüm çift renk modundaysa tahta ikinci bir tur daha temizlenir (GDD 4.4.2). Bu vaka normal modu test eder.

---

## D. Düşme, çapraz kayma, doldurma, kaskad (10 vaka)

### D01 — Düz düşme sırayı korur, boşluk yukarıdan dolar
- **Semboller:** K, M, · ; a–f takip edilen nötr taşlar.
- **Tahta:**
```
     1 2 3 4 5
  1  · a b c ·
  2  · d e f ·
  3  · K M K ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te (3,2)–(3,4) üç K patlar.
  - d, e, f bir satır düşerek (3,2), (3,3), (3,4)'e iner.
  - a, b, c bir satır düşerek (2,2), (2,3), (2,4)'e iner.
  - Satır 1'in sütun 2–4 hücreleri giriş hücrelerinden gelen yeni taşlarla dolar.
  - (4,3)'teki M yerinde kalır.
  - Sütunlar arasında taş karışmaz.
- **Dayanak:** GDD 5.1.1, 5.1.2, 5.1.3, 5.3.1
- **Güven:** orta (GDD 5.1.2 orta)

### D02 — Yeni taş yalnızca giriş hücresinden girer (özellik testi)
- **Semboller:** K, · ; X oynanmayan hücre.
- **Tahta:** Sütun 1'in üstü kapalı: (1,1) oynanmayan hücre, giriş türü tanımsız. Sütun 2–5'in tepesinde giriş hücresi var. Bölümde çapraz kayma izinli (GDD 5.2.4).
```
     1 2 3 4 5
  1  X · · · ·
  2  · · · · ·
  3  · · · · ·
  4  · · · · ·
```
- **İşlem:** Sütun 1'deki bir hücreyi eşleşmeyle boşalt.
- **Sağlanması gereken özellikler:**
  - Sütun 1'e yukarıdan yeni taş **girmez**; sütun 1'in üstünde giriş hücresi yok.
  - AK-04 A ile sütun 1'deki boşluk yalnızca komşu sütundan çapraz kaymayla dolabilir. Çapraz kayma da mümkün değilse boşluk kalır.
  - Yeni taşların hepsi bölüm verisinde giriş türü tanımlı hücrelerden çıkar.
- **Dayanak:** GDD 5.3.1, 5.2.2, AK-03, AK-04
- **Güven:** orta
- **AK-04 B ile:** sütun 1'in en üstteki oynanabilir hücresi (2,1) örtük giriş hücresidir; yeni taş oradan girer.

### D03 — Çapraz kayma önce sol-yukarıdan yapılır (durum testi)
- **Semboller:** · ; a–d takip edilen taşlar; 1 tek katmanlı blok (düşmez); _ boş hücre.
- **Tahta:** Durgun tahta; (3,3) boş. Bölümde çapraz kayma izinli (GDD 5.2.4).
```
     1 2 3 4 5
  1  · c · d ·
  2  · a 1 b ·
  3  · · _ · ·
```
- **İşlem:** Bir düşme adımı çalıştır.
- **Beklenen:**
  - (3,3)'ün tam üstünde düşmeyen bir katmanlı blok var. Düz düşmeyle taş gelmeyeceği için hücre AK-03 A ile çapraz kaymaya adaydır.
  - (3,3)'e **a** gelir; a sol-yukarıdaki (2,2)'dedir.
  - Ardından (2,2) üstten c ile dolar.
  - b yerinde kalır.
- **Dayanak:** GDD 5.1.1, 5.2.2, 5.2.4, 6.2.1 (katmanlı blok düşmez), 6.2.4, AK-03
- **Güven:** orta
- **Not:** Hücrenin üstü düşmeyen bir engelle kalıcı olarak kapalı olduğu için AK-03 B ile de sonuç aynıdır.

### D04 — Sol-yukarı uygun değilse sağ-yukarıdan kayar (durum testi)
- **Semboller:** D03 ile aynı.
- **Tahta:**
```
     1 2 3 4 5
  1  · c · d ·
  2  · 1 1 b ·
  3  · · _ · ·
```
- **İşlem:** Bir düşme adımı çalıştır.
- **Beklenen:**
  - (2,2) de düşmeyen bir katmanlı blok; sol-yukarıdan taş çekilemez.
  - (3,3)'e **b** gelir; b sağ-yukarıdaki (2,4)'tedir.
  - (2,4) üstten d ile dolar.
- **Dayanak:** GDD 5.2.2, 5.2.4, AK-03
- **Güven:** orta
- **Not:** AK-03 B ile de sonuç aynıdır (üstü kalıcı olarak kapalı boşluk).

### D05 — Düz düşme mümkünse çapraz kayma yapılmaz (durum testi)
- **Semboller:** · ; a, b, c, e takip edilen taşlar; _ boş.
- **Tahta:**
```
     1 2 3 4 5
  1  · · c · ·
  2  · a e b ·
  3  · · _ · ·
```
- **İşlem:** Bir düşme adımı çalıştır.
- **Beklenen:**
  - (3,3)'e tam üstündeki **e** iner; (2,3) üstten c ile dolar.
  - a ve b yerinde kalır; düz düşmeyle taş gelecek hücreye çapraz kayma yapılmaz.
- **Dayanak:** GDD 5.1.1, 5.1.2, 5.1.3, 5.2.3
- **Güven:** orta (GDD 5.1.2 orta)

### D06 — Kaskad kendiliğinden patlar ve hamle harcamaz
- **Semboller:** K, M, Y, · .
- **Tahta** (kalan hamle 10):
```
     1 2 3 4 5
  1  · · K · ·
  2  · · Y · ·
  3  · · Y · ·
  4  K K M · ·
  5  · · Y · ·
```
- **Hamle:** (5,3) → (4,3)
- **Beklenen:**
  1. Sütun 3'te (2,3)–(4,3) üç Y patlar.
  2. (1,3)'teki K, (4,3)'e düşer. Satır 4'te (4,1)–(4,3) üç K, oyuncu bir şey yapmadan patlar.
  3. Kalan hamle 9 olur; kaskad hamle düşmez.
- **Dayanak:** GDD 5.5.1, 5.5.2, 7.2.1
- **Güven:** orta (GDD 5.5.2 orta)

### D07 — Kaskad ancak tahta durulunca tetiklenir (özellik testi)
- **Tahta:** D06 ile aynı.
- **Sağlanması gereken özellikler:**
  - Kaskad eşleşmesi, ilgili taş yerine oturmadan ve devam eden patlama ya da animasyon bitmeden patlamaz.
  - Düşmekte olan bir taş eşleşmenin parçası sayılmaz.
  - Patlama ancak düşme bittikten sonraki bir karede gerçekleşir.
- **Dayanak:** GDD 5.5.1, 2.2.4
- **Güven:** orta

### D08 — Başlangıç tahtasında hazır 3'lü olmaz (özellik testi)
- **Kurulum:** Rastgele renkli hücreler içeren 1000 farklı tohumla bölüm başlangıç tahtası üret. Tahta boyutu standart boyuttan (parametre: tahta_genislik_standart, tahta_yukseklik_standart) ve dar biçimlerinden (parametre: dar_tahta_azami_sutun_eksigi, dar_tahta_azami_satir_eksigi) seçilir (GDD 8.1.2). Renk sayısı parametre: renk_sayisi_standart ile en fazla parametre: kolay_bolum_azami_renk_eksigi kadar eksiği arasında olmalı (GDD 8.2.2).
- **Sağlanması gereken özellik:** Hiçbir başlangıç tahtasında yatay ya da dikey 3 aynı renk yan yana olmaz.
- **Dayanak:** GDD 5.4.1, 5.4.2
- **Güven:** yüksek

### D09 — Başlangıç tahtasında hazır kare olmaz ve en az bir olası hamle bulunur (özellik testi)
- **Kurulum:** D08 ile aynı.
- **Sağlanması gereken özellikler:**
  - Hiçbir başlangıç tahtasında aynı renkli 2×2 kare olmaz.
  - Her tahtada en az bir geçerli hamle bulunur: tek bir yer değiştirmeyle eşleşme oluşturan bir çift.
- **Dayanak:** GDD 5.4.3, 5.4.4
- **Güven:** orta
- **Not:** İzinli renk kalmadığı için kare kısıtının gevşetildiği tahtalar (GDD 5.4.6) kare özelliğinden muaftır. Üreteç gevşetmeyi kaydeder; test o tahtaları yalnız olası hamle özelliğiyle denetler.

### D10 — Olası hamle kalmazsa karıştırma geçerli bir tahta üretir (özellik testi)
- **Kurulum:** Olası hamlesi olmayan, rastgele renkli 100 tahta üret (test üreteciyle aranır) ve karıştırma adımını çalıştır.
- **Sağlanması gereken özellikler:**
  - Karıştırma sonrası tahtada hazır eşleşme yoktur ve en az bir olası hamle vardır.
  - AK-13 A ile önce yer değiştirme denenir (parametre: karistirma_deneme_sayisi). Taş renkleri yalnızca yer değiştirme denemeleri başarısız olursa değiştirilir; renk sayısı dağılımı mümkün olduğunca korunur.
  - Renk değiştirme de başarısız olursa taşlar eski yerlerine döner ve "olası hamle yok" akışı çalışır: bölüm kaybı sayılmadan tahta yeniden kurulur (bkz. F07).
  - Hamle sayacı değişmez.
- **Dayanak:** GDD 5.6.1, 5.6.2, 5.6.5, 7.2.3, AK-13
- **Güven:** orta
- **AK-13 B ile:** renk değiştirme yoktur; yer değiştirme denemeleri başarısız olan tahtalarda bölüm ek hamle teklifi sunulmadan kaybedilir.

---

## E. Engeller: hasar, düşme, kırılma (8 vaka)

### E01 — Katmanlı blok, dik komşusundaki eşleşmeden bir katman kaybeder
- **Semboller:** K, M, · ; 2 iki katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · 2 · ·
  3  K K M · ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te üç K patlar.
  - (2,3)'teki katmanlı blok 2 katmandan 1'e iner, kırılmaz ve yerinde kalır.
- **Dayanak:** GDD 6.1.1, 6.1.2, 6.2.1 (katmanlı blok)
- **Güven:** yüksek

### E02 — Son katman kırılınca hücre boşalır ve üstteki taş düşer
- **Semboller:** K, M, · ; a takip edilen taş; 1 tek katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · a · ·
  2  · · 1 · ·
  3  K K M · ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te üç K patlar; (2,3)'teki katmanlı blok kırılır.
  - a aşağı düşer ve (3,3)'e iner, çünkü (4,3)'te M var.
  - Katmanlı blok düşmediği için hiçbir aşamada yer değiştirmez.
- **Dayanak:** GDD 6.1.1, 6.1.9, 5.1.2, 6.2.1 (katmanlı blok: düşmez)
- **Güven:** orta (GDD 5.1.2 ve 6.1.9 orta)

### E03 — Çapraz komşu yan eşleşme hasarı almaz
- **Semboller:** K, M, · ; 1 tek katmanlı blok.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · · 1 ·
  3  K K M · ·
  4  · · K · ·
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te üç K patlar.
  - (2,4)'teki katmanlı blok yalnızca (3,3)'e çaprazdan değiyor ve **hasar almaz**; 1 katmanı kalır.
- **Dayanak:** GDD 6.1.2 (yalnızca dört dik komşu)
- **Güven:** yüksek

### E04 — Aynı eşleşmenin iki hücresine değen katmanlı blok tek katman kaybeder
- **Semboller:** K, M, · ; 2 iki katmanlı blok; B bomba taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  K · · · ·
  2  K 2 · · ·
  3  M K K · ·
  4  K · · · ·
```
- **Hamle:** (4,1) → (3,1)
- **Beklenen:**
  - L eşleşmesi patlar: (1,1), (2,1), (3,1), (3,2), (3,3). (3,1)'de bomba taşı oluşur.
  - (2,2)'deki katmanlı blok iki eşleşme hücresine değiyor: (2,1) ve (3,2). AK-11 A ile bir eşleşme grubundan en fazla 1 vuruş aldığı için **yalnızca bir** katman kaybeder ve 1 katmanla kalır.
- **Dayanak:** GDD 6.1.2 (iki hücre durumu: 6.1.3, AK-11), 6.2.1 (katmanlı blok: çoklu vuruş yok), 2.5.5
- **Güven:** orta (AK-11 orta)
- **AK-11 B ile:** blok değdiği iki hücre için iki vuruş alır ve kırılır.

### E05 — Yapışkan kaplamalı taş kaydırılamaz; yan eşleşme hasarı kaplamayı kaldırır
- **Semboller:** K, M, · ; "~" yapışkan kaplamalı taş.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · ~ · ·
  3  K K M · ·
  4  · · K · ·
```
- **Hamle 1:** (2,3) → (2,4). Kaplamalı taşı kaydırma girişimi.
- **Beklenen 1:** İmkânsız hamle geri bildirimi oynar. Taşlar yer değiştirmez ve kalan hamle değişmez.
- **Hamle 2:** (4,3) → (3,3)
- **Beklenen 2:** Satır 3'te üç K patlar ve (2,3)'teki yapışkan kaplama kalkar. Kaplama tek katmanlıdır.
- **Dayanak:** GDD 6.2.2 (yapışkan kaplama: kaydırmayı engeller, yan eşleşme hasarı alır, 1 katman), 2.3.2
- **Güven:** yüksek

### E06 — Kilitli taş kaydırılamaz, hamle düşülmez
- **Semboller:** K, M, · ; "#" kilitli taş.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · · · ·
  3  K K # M ·
  4  · · · · ·
```
- **Hamle:** (3,3) → (3,4)
- **Beklenen:**
  - İmkânsız hamle geri bildirimi oynar.
  - Taşlar yer değiştirmez, kalan hamle değişmez.
- **Dayanak:** GDD 6.2.2 (kilit: kaydırmayı engeller), 6.2.6, 2.3.2
- **Güven:** yüksek

### E07 — Yayılan zemin eşleşme boyunca yayılır
- **Semboller:** K, M, · ; Z yayılan zemin (katman haritasında).
- **Tahta (taşlar):**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · · · ·
  3  K K M · ·
  4  · · K · ·
```
- **Katman haritası:**
```
     1 2 3 4 5
  3  Z . . . .
```
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Satır 3'te üç K patlar.
  - Yayılan zemin (3,1)'den eşleşmenin diğer hücrelerine yayılır; (3,2) ve (3,3) de zeminli olur. Yayılma zeminli hücreye uzaklık sırasıyla ilerler.
  - Eşleşmeye girmeyen hücrelere yayılmaz.
- **Dayanak:** GDD 6.3.1
- **Güven:** orta

### E08 — Sert blok yan eşleşme hasarı almaz, özel taş vuruşundan etkilenir
- **Semboller:** K, M, · ; W sert blok (3 katman); "-" yatay çizgi taşı.
- **Tahta:**
```
     1 2 3 4 5
  1  · · · · ·
  2  · · W · -
  3  K K M · ·
  4  · · K · ·
```
- **Hamle 1:** (4,3) → (3,3)
- **Beklenen 1:**
  - Satır 3'te üç K patlar. W (3,3)'e dikten değiyor ama katmanı **değişmez**; 3 katman kalır.
  - W düşmez. (2,5)'teki çizgi taşı da yerinde kalır, çünkü altındaki (3,5) boşalmıyor.
- **Hamle 2** (tahta durulduktan sonra; çizgi taşı satır 2'de kalır): (2,5) → (2,4)
- **Beklenen 2:** Çizgi taşı satır 2'yi iki yöne temizler ve W 3 katmandan 2'ye iner.
- **Dayanak:** GDD 6.1.1, 6.1.2, 6.2.1 (sert blok: yan eşleşme hasarı yok, özel taş vuruşu var, katman 1–3), 3.2.1, 3.2.2
- **Güven:** orta

---

## F. Hedef, hamle sayacı, kazanma ve kaybetme (7 vaka)

### F01 — Geçerli hamlede sayaç hemen azalır
- **Tahta:** A01'deki tahta. Kalan hamle 20.
- **Hamle:** (2,3) → (3,3)
- **Beklenen:**
  - Hamle geçerli sayıldığı anda kalan hamle 19 olur; yapılan hamle 1 artar.
  - Patlama ve düşme animasyonlarının bitmesi beklenmez.
- **Dayanak:** GDD 7.2.1, 2.3.8
- **Güven:** yüksek

### F02 — Geçersiz hamle sayaçtan düşmez
- **Semboller:** K, M, · .
- **Tahta** (kalan hamle 20):
```
     1 2 3 4 5
  1  · · · · ·
  2  · K M · ·
  3  · · · · ·
```
- **Hamle:** (2,2) → (2,3). Hiçbir eşleşme oluşmuyor ve hamlede özel taş yok.
- **Beklenen:**
  - Taşlar yer değiştirip "yanlış hamle" animasyonuyla geri döner.
  - Kalan hamle 20 kalır.
- **Dayanak:** GDD 2.3.3, 2.3.4, 7.2.2
- **Güven:** yüksek

### F03 — Özel taşa dokunmak hamle harcar
- **Tahta:** B03'teki tahta. Kalan hamle 20.
- **Hamle:** (4,4)'teki bomba taşına dokun.
- **Beklenen:** Kalan hamle 19 olur.
- **Dayanak:** GDD 7.2.1, 2.3.5
- **Güven:** yüksek

### F04 — Bütün hedefler tamamlanınca kazanma başlar
- **Tahta:** E02'deki tahta. Hedef: katmanlı blok 1. Kalan hamle 10.
- **Hamle:** (4,3) → (3,3)
- **Beklenen:**
  - Katmanlı blok kırılınca hedef sayacı 0'a iner ve kazanma akışı hemen başlar.
  - Hamle hakkı kalmış olsa da oyun devam etmez.
- **Dayanak:** GDD 7.1.3, 7.3.1, 6.1.10
- **Güven:** yüksek
- **Not:** Kalan 9 hamlenin artan hamle ödülüne gönderilmesi (GDD 7.3.3, 7.6.1) orta güvenli. Bu vakada doğrulanmaz.

### F05 — Son hamlenin kaskadı hedefi tamamlarsa sonuç kazanmadır
- **Tahta:** D06'daki tahta. Hedef: K taşı 3. Kalan hamle 1.
- **Hamle:** (5,3) → (4,3)
- **Beklenen:**
  - Hamle anında kalan hamle 0 olur.
  - Ardından gelen kaskadda üç K patlar ve hedef tamamlanır.
  - AK-02 A ile önce son hamlenin bütün patlamaları, düşmeleri ve kaskadları beklenir; bu sürede hedefler tamamlandığı için sonuç **kazanma** olur. Yedek hamle hakkı kullanılmaz; ek hamle teklifi ve kaybetme akışı başlamaz.
- **Dayanak:** GDD 7.3.1, 7.4.1, 5.5.1, AK-02
- **Güven:** orta
- **Not:** AK-02 B'de de (1)–(2) adımları aynı olduğu için sonuç değişmez.

### F06 — Hamle biter ve hedef kalırsa önce yedek hamle hakkı, sonra ek hamle teklifi, sonra kaybetme gelir
- **Tahta:** A01'deki tahta. Hedef: katmanlı blok 1 (tahtada ulaşılamaz bir yerde). Kalan hamle 1. Bölüm numarası parametre: ikinci_uyari_son_bolum değerinden büyük seçilir.
- **Hamle:** (2,3) → (3,3)
- **Beklenen (oyuncunun yedek hamle hakkı yok):**
  1. Tahta durulduktan sonra **ek hamle teklifi** açılır. Teklif parametre: ek_hamle_sayisi kadar hamle verir; bir deney grubunda daha fazla olabilir (GDD 9.12.1).
     - Fiyat, oyuncunun fiyat düzeyine (GDD 9.3.7) ve art arda alım sırasına göre parametre: ek_hamle_fiyat_kademeleri tablosundan okunur; yüksek düzeyde parametre: yuksek_fiyat_carpani uygulanır.
  2. Oyuncu teklifi reddederse kaybetme akışı başlar ve bir can düşer (meta testi MT01).
- **Beklenen (oyuncunun yedek hamle hakkı var):**
  1. AK-02 A ile teklif penceresi açılmadan önce yedek hamle hakkı kullanılır: parametre: yedek_hamle_sayisi kadar hamle eklenir ve oyun sürer.
  2. Hamle yine biterse ek hamle teklifi açılır; reddedilirse kaybetme akışı başlar.
- **Dayanak:** GDD 7.4.1, 7.4.2, 7.4.3, 7.4.4, 3.8.2, 9.12.1, 9.12.2, AK-02
- **Güven:** orta
- **Not:** Bölüm numarası parametre: ikinci_uyari_son_bolum değerini aşmazsa teklif reddinden sonra kaybetmeden önce ikinci bir uyarı gösterilir (GDD 9.12.5); bu vaka o durumu kapsamaz.
- **AK-02 B ile:** önce ek hamle teklifi açılır; teklif reddedilirse ve yedek hamle hakkı varsa bu hak son şans olarak kullanılır; hamle yine biterse kaybetme akışı başlar.

### F07 — Olası hamle yoksa ve karıştırma başarısızsa "olası hamle yok" akışı çalışır (durum testi)
- **Semboller:** K, M ; X oynanmayan hücre.
- **Tahta:**
```
     1 2 3 4
  1  X X X X
  2  X K M X
  3  X X X X
```
- **İşlem:** Olası hamle kontrolünü ve karıştırmayı çalıştır.
- **Beklenen:**
  - Olası hamle bulunmaz ve karıştırma başlar.
  - Hiçbir yer değiştirme ya da renk değişimi iki hücrede 3'lü oluşturamaz. AK-13 A ile önce yer değiştirme, sonra renk değiştirme denenir; ikisi de başarısız olunca taşlar eski yerlerine döner ve "olası hamle yok" akışı tetiklenir.
  - Akış bilgi penceresini gösterir ve tahtayı yeniden kurar. Bölüm **kaybedilmiş sayılmaz**, can düşmez, ek hamle teklifi açılmaz ve hamle sayacı korunur.
- **Dayanak:** GDD 5.6.1, 5.6.2, 5.6.5, 7.5.1, AK-13
- **Güven:** orta
- **Not:** Bu tahta şekli yeniden kurulduğunda da olası hamle üretemez. Test yalnızca akışın ilk kez tetiklenmesini ve bölüm sonucunu denetler; tekrar eden akışın nasıl sonlanacağı GDD'de tanımlı değildir.
- **AK-13 B ile:** renk değiştirme denenmez; "olası hamle yok" akışı bölümü ek hamle teklifi sunmadan kaybettirir ve bir can düşer.

---

## G. Meta testleri (12 vaka)

Ekonomi ve ayar değerleri parametre adıyla yazıldı (GDD 0.4). Örnek hesaplar parametre dosyasındaki öneri değerle yapılır; öneri değer değişirse beklenen sayı aynı formülle yeniden hesaplanır. Belirsiz değerler "yapılandırmadan oku" olarak geçiyor. Zaman ölçen testler sahte saatle (test saatini ileri alarak) çalıştırılır.

### MT01 — Bölüm kaybı bir can düşürür ve dolum sayacını başlatır
- **Başlangıç:** Can dolu (parametre: azami_can), sınırsız can yok.
- **İşlem:** Bir bölümü kaybet.
- **Beklenen:**
  - Can azami_can − 1 olur.
  - Bir sonraki cana kalan süre parametre: can_dolum_suresi_dk.
  - Saklanan "canların tamamen dolacağı an", şimdiden can_dolum_suresi_dk sonrası olur.
- **Dayanak:** GDD 9.1.1, 9.1.3, 9.1.4, 9.1.5
- **Güven:** orta (GDD 9.1.5 orta)

### MT02 — Canlar dolum süresinde bir dolar ve tavanda durur
- **Başlangıç:** Can azami_can − 3 (parametre dosyasındaki öneri değerle 3/6). Süre birimi d = parametre: can_dolum_suresi_dk.
- **İşlem ve beklenen:** Saati ileri al.
  - +1 × d → azami_can − 2
  - +3 × d (toplam) → azami_can
  - +4 × d (toplam) → yine azami_can. Tavan aşılmaz ve sayaç durur.
  - Uygulama kapalıyken geçen süre de sayılır.
- **Dayanak:** GDD 9.1.1, 9.1.4, 9.1.5
- **Güven:** orta (GDD 9.1.5 orta)

### MT03 — Sezon ödül hattında premium hat sahibinin azami canı yükselir
- **Başlangıç:** Oyuncu sezon ödül hattının premium hattına sahip. Can azami_can.
- **İşlem:** Saati (azami_can_premium − azami_can) × can_dolum_suresi_dk ileri al (parametre dosyasındaki öneri değerle 80 dk).
- **Beklenen:**
  - Azami can parametre: azami_can_premium olarak gösterilir.
  - Zamanla dolum azami_can_premium'a kadar sürer: süre sonunda can azami_can_premium olur.
- **Dayanak:** GDD 9.1.1, 9.1.4, 9.8.9
- **Güven:** orta. Zamanla dolumun da premium tavana kadar sürdüğü GDD 9.1.1 ve 9.1.4'ün birlikte okunmasıdır.

### MT04 — Yetersiz bakiyede satın alma başarısız olur
- **Başlangıç:** Bakiye = parametre: fiyat_cizgi − 1 (öneri değerle 799 para), normal fiyat düzeyi, çizgi taşı başlangıç takviyesi adedi 0.
- **İşlem:** Çizgi taşı başlangıç takviyesi satın almayı dene.
- **Beklenen:**
  - Satın alma başarısız olur ve para mağazası önerilir.
  - Bakiye ve takviye adedi (0) değişmez.
  - Hiçbir harcama kaydı oluşmaz.
- **Dayanak:** GDD 9.3.7, 9.2.5; parametre: fiyat_cizgi
- **Güven:** yüksek

### MT05 — Takviye satın alma para düşer ve envantere ekler
- **Başlangıç:** Bakiye 6000 para, satır aracı adedi 0. Fiyat düzeyi yapılandırmadan okunur: normal ya da yüksek.
- **İşlem:** Bir satır aracı satın al.
- **Beklenen:**
  - Normal düzeyde fiyat parametre: fiyat_satir, yüksek düzeyde fiyat_satir × yuksek_fiyat_carpani.
  - Bakiye normal düzeyde 6000 − fiyat_satir, yüksek düzeyde 6000 − fiyat_satir × yuksek_fiyat_carpani olur (parametre dosyasındaki öneri değerle 3000 ve 1500).
  - Satır aracı adedi 1 olur.
  - Harcama, adı ve miktarıyla birlikte bir kez kaydedilir.
- **Dayanak:** GDD 9.3.7, 9.2.5, 9.3.11
- **Güven:** yüksek

### MT06 — Bölüm kazanma para ödülü zorluğa ve kalan hamleye bağlı
- **Başlangıç:** Bölümün zorluk etiketi bölüm dosyasından ayrı etiket tablosundan okunur; sunucu etiketi değiştirebilir (GDD 8.8.1, 8.8.1a).
- **İşlem:** Bölümü 4 hamle kalmışken bitir (kazanarak).
- **Beklenen:**
  - Taban para zorluk etiketine göre seçilir: parametre: kazanma_parasi_normal, kazanma_parasi_zor, kazanma_parasi_cok_zor, kazanma_parasi_asiri_zor.
  - Kalan her hamle için parametre: kalan_hamle_basina_para eklenir.
  - Toplam = taban + kalan_hamle_basina_para × 4. Normal bölümde parametre dosyasındaki öneri değerle 20 + 4 × 2 = 28 para.
- **Dayanak:** GDD 9.2.2, 7.6.1, 8.8.1, 8.8.2
- **Güven:** orta
- **Not (ek beklenti, güven düşük; GDD 9.2.2):** kalan_hamle_basina_para kesirli bir değere ayarlanırsa toplam aşağı yuvarlanır.

### MT07 — Başlangıç takviyesi ve oyun içi araç açılış kapıları
- **İşlem:** En yüksek bölümü sırayla her açılış parametresinin bir eksiğine ve kendisine getir; takviye durumunu kontrol et.
- **Beklenen:**

  | En yüksek bölüm | Durum |
  |---|---|
  | acilis_baslangic_takviyeleri − 1 | Başlangıç takviyeleri (çizgi taşı, bomba taşı, renk küresi) kilitli |
  | acilis_baslangic_takviyeleri | Başlangıç takviyeleri açık |
  | acilis_tek_hucre_araci | Tek hücre aracı açık |
  | acilis_satir_araci | Satır aracı açık |
  | acilis_sutun_araci | Sütun aracı açık |
  | acilis_karistirma_araci | Karıştırma aracı açık |

  Her kapının bir önceki bölümünde ilgili takviye ya da araç kilitlidir.
- **Dayanak:** GDD 9.3.5, 9.4 (kapı deseni), 9.4.1
- **Güven:** yüksek
- **Not:** Kapının hangi bölüm sayacına göre değerlendirildiği GDD 9.4.2'de düşük güvenlidir. Test, en yüksek bölümü ile bulunduğu bölümü aynı olan bir oyuncuyla çalıştırılır; böylece sonuç bu kurala bağlı kalmaz.

### MT08 — Günlük ödülde gün sınırı sunucu saatinde sabit bir saattir
- **Başlangıç:** En yüksek bölüm parametre: acilis_gunluk_odul ya da üzeri; tanıtım daha önce görülmüş. Sunucu saati parametre: gun_siniri_saati − 1 dakika; o güne ait ödül alınmamış.
- **İşlem ve beklenen:**
  1. Ödülü al → seri 1 artar.
  2. Aynı saatte yeniden almayı dene → alınamaz.
  3. Sunucu saatini gun_siniri_saati'ne getir → yeni ödül alınabilir hale gelir.
  4. En yüksek bölümü acilis_gunluk_odul − 1 olan bir oyuncuda özellik kapalı olmalı.
- **Dayanak:** GDD 9.5.1, 9.5.2, 9.5.4, 9.5.5, 9.4
- **Güven:** yüksek

### MT09 — Haftalık ödül hattında hat puanı zorluğa göre ve çarpanla verilir
- **Başlangıç:** Haftalık ödül hattı aktif. Etkinlik çarpanı ve çarpan bileti kapalı.
- **İşlem:** Sırayla normal, zor, çok zor ve aşırı zor birer bölümü kazanarak bitir.
- **Beklenen:**
  - Hat puanı artışları sırasıyla parametre: hat_puani_normal, hat_puani_zor, hat_puani_cok_zor, hat_puani_asiri_zor.
  - Aynı bölüm çarpanla oynanırsa artış "taban × aktif çarpan" olur. Çarpan değeri yapılandırmadan okunur.
  - "Çarpanla oynandı" işareti bölüme girildiği anda konur; bölüme girerken çarpan aktif değilse çarpan 1'dir.
- **Dayanak:** GDD 9.9.3, 9.8.4, 9.10.1, 9.10.2
- **Güven:** orta (GDD 9.8.4 etiket eşlemesi orta)

### MT10 — Sezon ödül hattında hat puanı aynı tablodan gelir, çarpan uygulanmaz
- **Başlangıç:** Sezon ödül hattı aktif.
- **İşlem:** Bir normal ve bir aşırı zor bölümü kazanarak bitir; ardından bir normal bölümü çarpanla oynayıp kazanarak bitir; son olarak bir bölüm kaybet.
- **Beklenen:**
  - Hat puanı artışları parametre: hat_puani_normal ve hat_puani_asiri_zor.
  - Çarpanla oynanan normal bölümde de artış hat_puani_normal olur; sezon hattı puanına çarpan uygulanmaz.
  - Bölüm kaybında hat puanı eklenmez.
- **Dayanak:** GDD 9.8.4, 9.8.5, 9.10.3
- **Güven:** orta

### MT11 — Ödül hattı basamağı hat puanı hedefi dolunca atlanır
- **Başlangıç:** Basamak hedefleri parametre: sezon_hatti_basamak_hedefleri (haftalık hat için parametre: haftalik_hat_basamak_hedefleri) tablosundan okunur. Oyuncu bir basamağın hedefinin 1 hat puanı altında.
- **İşlem:** Zor etiketli bir bölümü kazanarak bitir; bölüm parametre: hat_puani_zor kadar hat puanı getirir.
- **Beklenen:**
  - Basamak bir ileri gider ve o basamağın ücretsiz ödülü alınabilir hale gelir.
  - Artan hat_puani_zor − 1 puan bir sonraki basamağa sayılır.
  - Premium (haftalık hatta ücretli) hat satın alınınca geçilmiş basamakların premium ödülleri de alınabilir hale gelir.
- **Dayanak:** GDD 9.8.2, 9.8.6, 9.8.7, 9.9.2, 9.9.5
- **Güven:** orta. Artan puanın devri, hedeflerin birikimli olmadığı kuralının (GDD 9.8.2) yorumudur.

### MT12 — Sezon ödül hattının birikim haznesi yalnız son basamakta para biriktirir
- **Başlangıç:** Oyuncu premium hat sahibi. Parametreler: hazne_asama_puani, hazne_asama_parasi, sezon_hazne_hedefi.
- **İşlem ve beklenen:**
  1. Oyuncu son ödül basamağına ulaşmamışken hazne 0 gösterir.
  2. Son basamakta her hazne_asama_puani hat puanı bir aşamadır ve her aşama hazneye hazne_asama_parasi para ekler.
  3. Hazne sezon_hazne_hedefi puana ulaşınca dolar ve alınır. Toplam = (sezon_hazne_hedefi ÷ hazne_asama_puani) × hazne_asama_parasi; parametre dosyasındaki öneri değerle 4800 para.
- **Dayanak:** GDD 9.8.8
- **Güven:** orta (GDD 9.8.8 aşama başına puan orta)

---

## Özet

| Bölüm | Vaka | Yüksek | Orta | Düşük |
|---|---|---|---|---|
| A. Eşleşme ve özel taş oluşumu | 15 | 12 | 3 | 0 |
| B. Özel taşların tek başına etkisi | 10 | 5 | 5 | 0 |
| C. Kombinasyonlar | 10 | 2 | 8 | 0 |
| D. Düşme, çapraz kayma, doldurma, kaskad | 10 | 1 | 9 | 0 |
| E. Engeller | 8 | 4 | 4 | 0 |
| F. Hedef, hamle, kazanma/kaybetme | 7 | 4 | 3 | 0 |
| **Tahta toplamı** | **60** | **28** | **32** | **0** |
| G. Meta | 12 | 4 | 8 | 0 |
| **Genel toplam** | **72** | **32** | **40** | **0** |

Düşük güvenli kurala dayanan vaka yazılmadı. Düşük güvenli kurallara ait beklentiler yalnız "ek beklenti" notu olarak yer alır ve yukarıdaki sayılara katılmaz. Ayarlanabilir kuralların oyun testinde neye bakılarak doğrulanacağı GDD 11.1'de, diğer açık sorular GDD 11.2'de.
