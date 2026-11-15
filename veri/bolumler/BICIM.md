# Bölüm dosyası biçimi

GDD 8.10'daki bilgilerin JSON karşılığı. Biçim projenin kendi kararıdır; okuyan kod `Cekirdek/Veri/BolumVerisi.cs`.
Dosya adı `bolum_NNN.json`. Zorluk etiketi bölüm dosyasında değil, `veri/zorluk_etiketleri.json` tablosundadır (GDD 8.8.1a).

| Alan | Zorunlu | Açıklama |
|---|---|---|
| `kimlik`, `surum` | kimlik | Bölüm numarası ve düzenleme sürümü. |
| `genislik`, `yukseklik` | evet | Tahta boyutu. Üst sınırlar: `tahta_genislik_standart`, `tahta_yukseklik_standart`; çok ekranlı bölümde yükseklik sınırı `kaydirmali_tahta_yukseklik`. |
| `hamle` | evet | Hamle sayısı (GDD 8.3). |
| `renkler` | evet | Renk sayısı. Renkler göreli kimlikle anılır: `r0`, `r1`, … |
| `renkOranlari` | hayır | Renk başına dolgu ağırlığı (GDD 5.3.5). |
| `hucreler` | evet | Satır başına bir metin; hücre kodları boşlukla ayrılır. |
| `altKatmanlar`, `ustKatmanlar` | hayır | Aynı düzende katman kodları; `.` katman yok. |
| `girisler` | hayır | Aynı düzende `1` / `0`. Verilmezse her sütunun en üstteki oynanabilir hücresi giriştir. |
| `toplamaHucreleri` | hayır | `[satır, sütun]` listesi (GDD 6.3.5). Verilmezse her sütunun en alt hücresi. |
| `onTanimliDolgu` | hayır | Sütun numarası → giriş sırasıyla hücre kodları (GDD 5.3.4 adım 2). |
| `sutunSetleri` | hayır | Sütun numarası → dolgu seti (GDD 5.3.4 adım 3). |
| `dolguSetleri` | hayır | Dolgu setleri listesi (GDD 5.3.4 adım 4). |
| `hedefler` | hayır | `{ "tur", "adet", "ekran" }`. `adet` 0 = tahtadaki hepsi. Renk hedefi: `renk:N`. |
| `engeller`, `katmanlar` | hayır | Ad → tanım (GDD 6.4). Hücre kodlarında bu adlar kullanılır. |
| `bayraklar` | hayır | `caprazKayma`, `cokEkranli`, `delik`. |
| `ekranlar` | çok ekranlıda | `[ilk satır, son satır]` listesi, yukarıdan aşağıya (GDD 8.9). Yalnız `cokEkranli` bayrağıyla verilir. |
| `ekSinirlar` | hayır | `{ "tur", "adet" }` listesi. |

## Hücre kodları

| Kod | Anlamı |
|---|---|
| `X` | Oynanmayan hücre |
| `_` | Boş başlayan hücre |
| `?` | Rastgele renk |
| `rN` | Sabit renk |
| `-` `\|` `B` `G` `R` | Yatay çizgi, dikey çizgi, bomba, güdümlü, renk küresi |
| `ad:katman` | Engel; `ad`, `engeller` sözlüğünden |

Katman kodu da `ad:katman` biçimindedir; `ad`, `katmanlar` sözlüğünden gelir.

## Tanımlar

Engel arketipleri: `katmanli_blok`, `sert_blok`, `dusen_kirilabilir`, `toplanabilir`, `coklu_vurus`,
`kaydirilamayan_dusen`, `para_tasi`, `uretici` (`birakilanKatman` ister), `yayilan_tehlike`.

Katman arketipleri: `zemin`, `yayilan_zemin`, `yapiskan_kaplama`, `kilit`, `ortu`, `sert_kaplama`.

Ezilebilen alanlar: `hedefTuru`, `azamiKatman`, `bagisikliklar` (`cizgi`, `bomba`, `gudumlu`, `renk_isini`,
`tahta_temizleme`, `arac`); engellerde ayrıca `gudumluOnceligi`, `gudumluHedefPuani`, `araclaSecilebilir`,
`yanEslesmeHasari`, `tanitim`.

## Dolgu seti

`{ "pas": 12, "ogeler": [ { "kod": "pt:1", "agirlik": 1, "azami": 4 } ] }`

Her taş için öğeler ağırlıklarıyla çekilir. `pas`, setin bir şey vermeyip sırayı bir sonraki adıma bırakma
ağırlığıdır. `azami`, o öğeden en fazla kaç tane çıkacağıdır. `?` kodu rastgele renktir ve ayarlı dolgudan geçer (GDD 5.3.6).

## Çok ekranlı bölümler

`bayraklar.cokEkranli` açıkken `ekranlar` zorunludur: en az iki ekran, tahtayı yukarıdan aşağıya boşluksuz ve
çakışmadan kaplar; her ekran en fazla `tahta_yukseklik_standart` satırdır ve en az bir hedef taşır (hedefin
`ekran` alanı). `hucreler`, katman haritaları, `girisler` ve `toplamaHucreleri` bütün tahtanın satırlarıyla
yazılır. Oyunda her ekran kendi tahtasıdır: olaylardaki konumlar o ekrana göredir, satır 1 ekranın en üst satırıdır.
`girisler` verilmezse her ekranda sütunun en üstteki oynanabilir hücresi giriştir.
