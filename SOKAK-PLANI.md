# Sokak planı: "Hasan'ın On Dokuz Fincanı" mahallesi

Durum: faz 1-3'ün ilk sürümü yazıldı (`CafeStreetStory.cs`), Unity'de denenmedi. Hazırlayan: bulut oturumu, 23 Ekim 2026.

Yazılanlar: eski kahvehane (iç masa, finale öncesi/sonrası farklı konuşma), saat kulesi plaketi, fincan çeşmesi (12 sakin, durumuna göre renk değiştirir), 6. günden sonra gece mavi lambalar (5 adet). Yapılmayanlar: bakkal perdesi, müzisyen, belediye, iskele, kahvehanenin kendiliğinden açılma animasyonu, faz 4 (gece gezisi).
PC'de ilk bakılacaklar: yerleşim (kahvehane x=-2.5 z=-28.5, saat kulesi x=28, çeşme x=2.2 z=-22) mevcut market/kulüp/dekorla çakışıyor mu; E ipucu çıkıyor mu; çeşme fincanları renk değiştiriyor mu.

## 1. Şu an sokak nasıl (koddan)

- `CafeWorld.BuildBoulevard()` (CafeBoulevard.cs): tek cadde, z=-18 çevresi, 72 birim geniş. Asfalt, şerit çizgileri, yaya geçidi, iki uçta duvar.
- Mekânlar: Gece Marketi (-14,-29), NOIR/SMOKEBANK kulübü (14,-32), basket sahası (-28,-30), Cadde Ligi panosu, taksi durağı (17,-14), iki bank.
- `CafeCityLife`: gündüz/gece ışığı (`dayTime/300`), 6 mahalle yayası (`CafeWalker`), trafik, yağmur parçacığı (`Rainy`, her 3. gün).
- Sorun: sokak hikâyeyle konuşmuyor. Hikâye (20. fincan, gölgeler, Nuri, 1962 anlaşması) hep dükkânın içinde geçiyor. Sokak bir "şehir dekoru", mahalle değil.

## 2. Fikir

Sokak, hikâyedeki **fincan sayacının fiziksel hâli** olsun. Gündüz sıradan, canlı bir mahalle. Gece aynı sokak mavi ışıkla başka bir yere dönüşsün: kapalı dükkânlar, ışığın altında durmayan bir bekçi, 19'da kesilmiş şarkılar.

Üç direk:

1. **İki yüzlü sokak.** Aynı mekânlar gündüz ve gece farklı. Haberlerdeki ipuçları sahnede görünür olsun (mavi lambalar, kendiliğinden açılan kahvehane, perdeli bakkal rafı).
2. **Mekân = karakter.** Her sakinin bir yeri olsun, yer ona bağlı bir hikâye parçası taşısın.
3. **Işık = güvenlik.** Oyunda zaten var: makinenin etrafı güvenli (`SafeRadius`), Nöbetçi ışığa dokunamıyor. Sokakta da aynı kural: lamba adaları güvenli, arası tehlikeli.

## 3. Mekânlar (haberler ve sakinlerle bağlı)

| Mekân | Hikâye bağı | Gündüz | Gece |
|---|---|---|---|
| Eski kahvehane | Nuri ve 1962 anlaşması; "kepenkleri otuz yıldır inik" haberi | Kapalı, tozlu cam | Kendiliğinden açık, içeride tek masa, çaydanlık sıcak |
| Saat kulesi | "Şehir saati bir dakika geri kaldı" haberi | Normal | Saat 19'da takılı, sayma sesi |
| Bakkal | "Kahve rafına perde çekti" haberi | Normal raf | Perde inik, perdenin ardında fincan gölgeleri |
| Meydan müzisyeni | "19. kez çaldı" haberi, Sayan | Şarkı çalıyor | 19'da kesiliyor, 20. nota hiç gelmiyor |
| Belediye | Kemal Aksu (karar 2) | Kapı önünde Kemal | Işık yanık, kimse yok |
| Fincan çeşmesi | Borç Defteri | Meydanda küçük çeşme | 20 küçük fincan, her biri bir sakin; gölgeye dönenin fincanı ters |
| İskele | Balıkçı Mehmet'in haritası, tezgâhın altındaki çarpı | Boş | Fenerle okunan harita parçası |
| Mevcut: Market, NOIR kulübü, basket sahası, taksi | Değişmez | Aynen | Mavi ışık katmanı eklenir |

Fincan çeşmesi planın merkezi: oyuncu sokağa her çıktığında sakinlerin durumunu bir bakışta görür (`ResState.cups`, `goneDay` zaten kayıtta). 19'daki sakinin fincanı kırmızı parlar.

## 4. Gece sokak gezisi (yeni mod)

Akış: kafe kapanır, oyuncu yatağa çıkmak yerine **kapıdan sokağa** da çıkabilir (isteğe bağlı). Uyumadan sabah olmaz kuralı bozulmaz, yatağa dönülünce günlük akış sürer.

- **Amaç:** gölgeye dönen sakini bul (`CafeResidents.GoneToday`), siparişini ver (`siparis` alanı), erken geri getir (+bağ, +30 coin).
- **Risk:** lamba adaları arası karanlık; Yankı yaklaşırsa dur, Nöbetçi ışığa dokunamaz, Sayan 19'da kesilmeli. Bu üçü `CafeGhosts`'ta zaten var; sokağa taşınır.
- **Araç:** el feneri değil **fincan ışığı**: sıcak bir fincan taşırsan 3 m yarıçapta güvende; fincan soğur (30-40 sn), çeşmeden veya kahvehaneden yeniden doldurulur.
- **Ödül:** üçüncü anılar (zaten eklendi, bond>=10), kayıp tarif defteri sayfaları, kahvehanede Nuri'nin hikâyesi.
- **Bitiş:** sabaha karşı otomatik "dönüş", yoksa fincan biterse oyuncu kafeye sürüklenir (ceza: -itibar, ölüm yok).

## 5. Teknik plan (fazlar)

Her faz tek PR; her faz sonunda Unity'de elle doğrulama.

**Faz 1: atmosfer (en düşük risk).**
- Gece mavi ışık katmanı: `CafeCityLife.Update` içinde `phase>.65` iken sokak lambalarını soğuk maviye çevir (haber: "Işıklar mavi yanıyor").
- Sis: `CafeEvents.ApplyWeather` zaten `RenderSettings` sisini yönetiyor; gece yoğunluğunu artır.
- Kabul: gece sokakta lambalar mavi, yağmur ve sis çakışmıyor.

**Faz 2: mekânlar.**
- Yeni `CafeStreetStory.cs` (partial `CafeWorld`), `BuildBoulevard()` sonunda çağrılır.
- Nesneleri Türkçe adla kur (`"Eski kahvehane"`, `"Saat kulesi"`), mevcut adlandırma kuralına uy; ada göre malzeme atan kodları Grep'le.
- Hazır paketlerden (CityCore, Paulisia_S) yalnızca prefab seç, paketi düzenleme.
- Kabul: gündüz ve gece görünümü farklı, hiçbiri oyuncuyu duvara sıkıştırmıyor.

**Faz 3: etkileşim ve çeşme.**
- `CafeTarget.kind` ile yeni türler: `"fountain"`, `"kahvehane"`. Çeşme `ResState`'i okur, yazmaz.
- Kayıt: yeni alan gerekirse `SaveData`'ya `bool` ekle (JsonUtility varsayılanı `false`, eski kayıt bozulmaz).

**Faz 4: gece gezisi.**
- Yeni `CafeNightWalk.cs` (kendi bayrağı, `CafeStory.Busy` ile çakışmasın).
- `CafeGhosts`'u sokağa genişlet; `SafeRadius` mantığını fincan ışığına bağla.
- Kabul: fincan biterken uyarı var, ceza adil, kafeye dönüş hata vermiyor.

**Faz 5: cila.** Ses (adım yankısı, uzak şarkı), 19'da kesilen müzisyen, kahvehane içi.

## 6. Dikkat edilecekler

- `Assets/CafePrototype` eski, dokunma. Hazır asset paketlerini düzenleme.
- Mixamo modelleri yalnızca yerel PC'de; yeni Mixamo varlığı ekleme.
- Saydam/özel shader malzemeleri koddan değil, `Resources/CafeMaterials` altına `.mat` olarak.
- Dosyalar UTF-8; Türkçe karakterli dosyaları PowerShell ile yeniden yazma.
- Performans: sokak 72 birim; lamba sayısını sınırla (URP'de nokta ışık maliyeti), mavi ışığı emissive malzemeyle taklit et.
- Bulutta derleme yok. Her faz "Unity'de doğrulanmadı" ile PR'a gider.

## 7. Senin kararların

1. Gece gezisi isteğe bağlı mı olsun, yoksa 11. günden sonra hikâye gereği zorunlu mu?
2. Sokak mı yeniden kurulsun (mevcut kulüp/market yerinde kalsın mı), yoksa üstüne mekân eklemek mi yeter? Öneri: yerinde kalsınlar, boşluklara ekle.
3. Korku düzeyi: gece gezisinde jumpscare olsun mu? (Ayarlardaki anahtar buraya da uygulanır.)
4. Hangi mekân ilk yapılsın? Öneri: Eski kahvehane, çünkü hem Nuri'nin hem 1962 anlaşmasının görsel bağı.
