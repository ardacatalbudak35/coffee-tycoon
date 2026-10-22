# Coffee Tycoon (Unity)

Unity 6 (6000.6.2f1), URP, yeni Input System. Birinci şahıs kafe yönetim oyunu: espresso/latte yapımı, müşteri servisi, ekonomi, stok/tedarik, dekor ilerlemesi, şube/genişleme, blackjack ve basketbol. Ayrıntılı geçmiş: `PROJE-NOTLARI.txt`. GitHub: ardacatalbudak35/coffee-tycoon.

## Yapı
- `Assets/CafeLife/Scripts` — asıl oyun (namespace `CafeLife`). `CafeGame` ana kontrolcü ve kayıt yolu, `CafeWorld` (partial class, sahneyi koddan kurar), `CafeEconomy` (SaveData, tarifler, satış, Save/Load), `CafeUI`/`CafeStudioUI`/`CafeOperationsUI` arayüz, `CafeExpansion` şubeler, `CafeDecorProgression` dekor seviyeleri, `CafeStreetArtDirection`/`CafeBoulevard*` cadde.
- `Assets/CafeLife/Editor` — derleme ve otomatik kontrol (`Cafe*Checks.cs`, `CafeReleaseBuild.cs`). Oyun mantığı değil.
- `Assets/CafeLife/Resources` — çalışma anında `Resources.Load` ile yüklenenler (CafeMaterials, yazı tipleri). Derlemeye girmesi gereken malzemeler buraya konur.
- `Assets/CafePrototype` — ESKİ prototip, kullanılmıyor. Dokunma.
- Diğer klasörler (CityCore, Paulisia_S, Toony*, VNB, Stylized Coffee Shop) hazır asset paketleri. Düzenleme.

## Kod stili
- Dosyalar yoğun, çoğu tek satırlık; mevcut stile uy. Sahne nesneleri ve loglar Türkçe adlı (`"Market vitrin"`, `"Pencere doğraması"`); bazı kodlar nesneyi ada göre bulup malzeme atıyor, adı değiştirirken Grep ile ara.
- Dosyalar UTF-8. PowerShell `Set-Content`/`Get-Content` ile yeniden yazma, Türkçe karakterler bozulur. Edit aracını kullan.
- Yeni saydam/özel shader malzemeleri koddan değil, `Resources` altına `.mat` dosyası olarak ekle; yoksa derlemede shader varyantı atılıp opak görünebilir (cam sorunu böyle çözüldü: `CafeMaterials/CafeGlass.mat`).

## Derleme ve test (Unity editörü açıkken)
- Editör `Logs/*.flag` dosyalarını izler. Örn: `Logs/RunCafeBuild.flag` oluşturunca Windows derlemesi alınır, sonuç `Logs/CafeReleaseBuild.txt`. Çıktı: `C:\Users\ardac\Documents\Codex\2026-10-16\kanka-udem\outputs\CoffeeTycoon-Windows-v6\CoffeeTycoon.exe`.
- Derlemeden önce açık oyunu kapat (exe kilitli kalır). Editör arka plandaysa pencereyi öne getirmek gerekebilir.
- Diğer kontroller: `RunCafeUpgradeCheck.flag` ve `Editor/Cafe*Check*.cs` içindeki bayraklar. Sonuçlar `Logs/` altında.

## Kayıt
- Dosya: `C:\Users\ardac\AppData\LocalLow\DefaultCompany\CoffeeTycon\CafeLife-v1.json` (+ `.bak`). JSON, alan `money` vb.
- Oyun parayı bellekte tutar ve kaydederken dosyanın üstüne yazar. Kaydı elle değiştirmeden ÖNCE oyunu kapat, yedek al, sonra yeniden aç.

## Kurallar
- Git'e girmeyenler: Library, Temp, Logs, UserSettings, derleme çıktıları, kişisel kayıt.
- Her oturumda tüm projeyi okuma; ilgili dosyayı bul, sadece onu oku.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.

## Bulut oturumu (PC kapalıyken)
- Bulutta Unity editörü yok: `Logs/*.flag` kontrolleri, `Cafe*Checks` ve exe derlemesi çalışmaz. Değişikliği derlenmemiş say, PR açıklamasına "Unity'de doğrulanmadı" yaz.
- `Resources/CafeMixamo` (Mixamo FBX'leri) git'te yok, sadece yerel PC'de. Bu modellere dayanan kodu bozma, yeni Mixamo varlığı ekleme.
- `main`'e doğrudan push etme; ayrı branch'e commit at ve PR aç. Kullanıcı PC'yi açınca Unity'de deneyip birleştirir.
