using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;

namespace CafeLife
{
    [InitializeOnLoad]
    public static class CafeLifeBuilder
    {
        static CafeLifeBuilder()
        {
            EditorApplication.delayCall+=ApplyCustomerAssetsToPlayableScene;
            if(SessionState.GetBool("CafeLifeVerify",false))
            {
                report=SessionState.GetString("CafeLifeReport",""); since=EditorApplication.timeSinceStartup;
                Application.logMessageReceived+=OnLog; EditorApplication.update+=Tick;
            }
        }
        public const string ScenePath="Assets/CafeLife/Scenes/CafeLife.unity";
        const string CustomerAnimatorPath="Assets/CafeLife/Customer.controller";
        static GameObject[] LoadCustomerPrefabs()
        {
            return CafeVisualUpgrade.People();
        }
        static GameObject[] LoadCityPrefabs()
        {
            string root="Assets/Paulisia_S/3D Game Kit - Lowpoly city assets/Prefabs/";
            string[] paths={"buildings/bakery.prefab","buildings/market.prefab","buildings/pizzeria.prefab","buildings/coffe_shop.prefab","buildings/blue_house.prefab","buildings/yellow_house.prefab","buildings/cinema.prefab","buildings/green_house.prefab","buildings/hotel.prefab",
                "vehicles/small_car.prefab","vehicles/taxi.prefab","decorations/road.prefab","decorations/road_black.prefab","decorations/road_black_zebra_crossing.prefab","decorations/sidewalk.prefab","decorations/tree_2.prefab","decorations/tree_3.prefab","decorations/lantern.prefab","decorations/hydrant.prefab","decorations/bench.prefab","decorations/bus_stop_sign.prefab"};
            var city=paths.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(root+p)).Where(p=>p).ToList();
            foreach(string p in new[]{"Bench01.prefab","Lamp01.prefab","TrashCan03.prefab"})
            { var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityCore Pack Free/Prefabs/"+p); if(asset)city.Add(asset); }
            return city.ToArray();
        }
        static AnimationClip LoadImportedAnimationClip(string assetPath)
        { return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>().FirstOrDefault(c=>c && !c.name.StartsWith("__preview__")); }
        static RuntimeAnimatorController EnsureCustomerAnimator()
        {
            return CafeVisualUpgrade.Controller();
        }
        static void ApplyCustomerAssetsToPlayableScene()
        {
            if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if(!File.Exists(ScenePath)) return;
            var prefabs=LoadCustomerPrefabs(); if(prefabs.Length==0) return;
            var controller=EnsureCustomerAnimator();
            var alreadyLoaded=SceneManager.GetSceneByPath(ScenePath);
            bool openedHere=!alreadyLoaded.IsValid() || !alreadyLoaded.isLoaded;
            var scene=openedHere?EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive):alreadyLoaded;
            try
            {
                var game=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<CafeGame>(true)).FirstOrDefault();
                if(!game) return;
                var city=LoadCityPrefabs();
                if(game.customerPrefabs==null || !game.customerPrefabs.SequenceEqual(prefabs) || game.customerAnimator!=controller || game.cityPrefabs==null || !game.cityPrefabs.SequenceEqual(city))
                {
                    game.customerPrefabs=prefabs; game.customerAnimator=controller; game.cityPrefabs=city;
                    EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    Debug.Log("Toony Tiny People müşteri modelleri ve yürüyüş animasyonları kafeye bağlandı.");
                }
            }
            finally { if(openedHere && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
        }
        [MenuItem("Cafe Life/Oyunu Hazırla ve Aç")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) { Debug.LogWarning("Önce Play modunu durdur."); return; }
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string[] required={"Floor","Coffee_Machine","Display_Counter","Cake_1","Croissant","Trash_Can","Table_Circle","Chair","Cup_With_Coffee","Flower_Pot","Menu_Board","Welcome_Board","Donut_1",
                "Tray","Jar","fastfood_Cup_1","fastfood_Cup_2","fastfood_Bucket","bottle","waterCooler","refrigerator_1","milk","crate","Modern Shelves"};
            var prefabs=required.Select(name=>{
                var paths=AssetDatabase.FindAssets(name+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileNameWithoutExtension(p)==name).ToArray();
                if(paths.Length==0) throw new InvalidOperationException("İçe aktarılan pakette eksik: "+name);
                return AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
            }).ToArray();
            Directory.CreateDirectory("Assets/CafeLife/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Cafe Life - Oyun").AddComponent<CafeGame>(); game.prefabs=prefabs;
            game.customerPrefabs=LoadCustomerPrefabs(); game.customerAnimator=EnsureCustomerAnimator(); game.cityPrefabs=LoadCityPrefabs();
            EditorSceneManager.SaveScene(scene,ScenePath);
            // Keep existing scenes, put the playable scene first.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            AssetDatabase.SaveAssets();
            Debug.Log("CAFE_LIFE_BUILD_OK: "+prefabs.Length+" prefab; "+ScenePath);
        }
        private static int stage;
        private static double since;
        private static string report="";
        private static Vector3 movementStart;
        private static int baseline;
        private static int inputFrames;
        private static Keyboard testKeyboard;
        private static float movementSince;
        private static CafeCustomer customer;
        private static bool failed;
        public static void BatchVerify()
        {
            try {
                Build(); CoreTests();
                SessionState.SetBool("CafeLifeVerify",true); SessionState.SetString("CafeLifeReport",report);
                var game=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); game.testing=true;
                stage=0; failed=false; since=EditorApplication.timeSinceStartup;
                Application.logMessageReceived+=OnLog;
                EditorApplication.update+=Tick;
                EditorApplication.isPlaying=true;
            } catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Check(bool condition,string text)
        { if(!condition) throw new Exception("FAIL: "+text); report+="PASS: "+text+"\n"; }
        static CupWork FinishedDrink(int recipe)
        {
            var r=Economy.Recipes[recipe]; var cup=new CupWork{recipe=recipe,coldCup=r.cold,iced=r.cold,ground=true,brewed=true,lidded=true,
                hasMilk=r.milk,hasWater=r.water,syrup=r.syrup,flavored=r.syrup!="",mixed=r.milk||r.water};
            if(r.cold)cup.steps.Add("ice"); if(r.syrup!="")cup.steps.Add(r.syrup);
            if(r.milk)cup.steps.Add("milk"); if(r.water)cup.steps.Add("water");
            cup.steps.Add("espresso"); cup.steps.Add("lid"); return cup;
        }
        static void CoreTests()
        {
            var d=new SaveData();
            Check(!Economy.Spend(d,161) && d.money==160,"Yetersiz bakiye değişmiyor");
            Check(Economy.StartRecipe(d,0) && d.beans==19,"Espresso stok tüketiyor");
            Check(Economy.Recipes.Length>=10 && Economy.Recipes[5].cold && Economy.Recipes[9].name=="Buzlu Espresso" && Economy.Recipes[2].syrup=="caramel","Tüm sıcak, buzlu ve şuruplu tarifler tanımlı");
            Check(CafeGame.IdealSteps(Economy.Recipes[0]).SequenceEqual(new[]{"espresso","lid"}) &&
                CafeGame.IdealSteps(Economy.Recipes[1]).SequenceEqual(new[]{"milk","espresso","lid"}) &&
                CafeGame.IdealSteps(Economy.Recipes[3]).SequenceEqual(new[]{"vanilla","milk","espresso","lid"}) &&
                CafeGame.IdealSteps(Economy.Recipes[4]).SequenceEqual(new[]{"water","espresso","lid"}) &&
                CafeGame.IdealSteps(Economy.Recipes[7]).SequenceEqual(new[]{"ice","vanilla","milk","espresso","lid"}),
                "Espresso, latte, vanilyalı latte, americano ve buzlu sıralar doğru");
            Check(Enumerable.Range(1,4).All(i=>CafeGame.RecipeForCup(i,false)==i && CafeGame.RecipeForCup(i,true)==i+4) &&
                CafeGame.RecipeForCup(0,true)==9 && CafeGame.RecipeForCup(9,false)==0,
                "Sıcak/soğuk bardak seçimi latte, şurup, americano ve espressoyu doğru eşliyor");
            d.beans=0; Check(!Economy.StartRecipe(d,0),"Stoksuz üretim engellendi");
            int before=d.money; int payment=Economy.Sale(d,0);
            Check(d.money==before+payment && d.served==1 && d.xp==25,"Satış ödeme ve deneyim veriyor");
            int reputation=Mathf.RoundToInt(d.reputation); int badPayment=Economy.Sale(d,0,false);
            Check(badPayment<Economy.Recipes[0].price && d.reputation<reputation,"Kötü ürün daha az kazandırıyor ve memnuniyeti düşürüyor");
            string path=Path.Combine(Path.GetTempPath(),"CafeLifeEconomyTest_"+Guid.NewGuid().ToString("N")+".json");
            try {
                Economy.Save(d,path); var loaded=Economy.Load(path);
                Check(loaded.money==d.money && loaded.served==2,"Kayıt dönüşü doğru");
                Economy.Save(d,path); File.WriteAllText(path,"invalid JSON"); Check(Economy.Load(path).money==d.money,"Bozuk kayıtta yedek açılıyor");
                var restored=Economy.Load(path); Check(restored.branches.Length==5 && restored.branches[0].owned && restored.branches[4].capacity>restored.branches[1].capacity,"Kafe şubeleri kayıt sonrası ve artan kapasiteyle korunuyor");
            }
            finally { foreach(string file in new[]{path,path+".tmp",path+".bak"}) if(File.Exists(file))File.Delete(file); }
            var branch=new CafeBranchData{owned=true,capacity=24,baristaLevel=2};
            Check(Economy.BranchIncome(branch)==0,"Seviyesi yetersiz barista otomatik kazanç üretmiyor");
            branch.baristaLevel=3; int income=Economy.BranchIncome(branch); branch.capacity=56;
            Check(income>0 && Economy.BranchIncome(branch)>income,"Barista seviyesi ve kafe kapasitesi pasif geliri artırıyor");
            var salesTest=new SaveData(); int baseSale=Economy.Sale(salesTest,0); salesTest.espressoLevel=2;
            int upgradedSale=Economy.Sale(salesTest,0);
            Check(upgradedSale==baseSale+2,"Espresso geliştirmesi satış gelirini yükseltiyor");
        }
        static void OnLog(string message,string stack,LogType type)
        {
            if(type!=LogType.Exception && type!=LogType.Error) return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase")) { report+="EDITOR INDEX WARNING (not gameplay): "+message+"\n"; return; }
            failed=true; report+="UNITY ERROR: "+message+"\n"+stack+"\n";
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since>120) { Finish(false,"Play testi zaman aşımı"); return; }
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try {
                var g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g || g.ui==null || g.player==null) return;
                if(stage==0) {
                    Check(g.Paused,"Ana menü açık başlıyor"); g.NewGame(); g.Spawn();
                    Check(g.data.branches.Length==5 && g.data.branches[4].capacity>g.data.branches[1].capacity,"Şube verileri daha büyük kapasitelere sahip");
                    Check(UnityEngine.Object.FindObjectsByType<CafeTarget>().Count(t=>t.kind=="branch")>=8 && UnityEngine.Object.FindObjectsByType<CafeTarget>().Count(t=>t.kind=="casino")==1,"Şehirde şubeler, her şubenin yönetim noktası ve blackjack girişi var");
                    Check(g.world.transform.Find("Şube 1 iç zemin")!=null && g.world.transform.Find("Şube yönetim paneli 1")!=null,"Kiralık şubelerin mobilyalı iç mekânları ve yerel panelleri kuruluyor");
                    var grinderTarget=UnityEngine.Object.FindObjectsByType<CafeTarget>().FirstOrDefault(t=>t.kind=="grinder");
                    Check(grinderTarget && grinderTarget.GetComponentsInChildren<SphereCollider>().Any(c=>c.isTrigger&&c.gameObject.layer==8),"Değirmenin E etkileşimi geniş ve doğru katmanda");
                    var grinderTrigger=grinderTarget?grinderTarget.GetComponentInChildren<SphereCollider>():null;
                    Check(grinderTrigger && grinderTrigger.bounds.center.y>.8f && grinderTrigger.bounds.center.y<1.8f,"Değirmen etkileşim alanı oyuncunun görüş hizasında");
                    Check(UnityEngine.Object.FindObjectsByType<CafeTarget>().Any(t=>t.kind=="cup_hot")&&UnityEngine.Object.FindObjectsByType<CafeTarget>().Any(t=>t.kind=="cup_cold")&&
                        UnityEngine.Object.FindObjectsByType<CafeTarget>().Any(t=>t.kind=="lid_hot")&&UnityEngine.Object.FindObjectsByType<CafeTarget>().Any(t=>t.kind=="lid_cold"),"Sıcak/soğuk bardaklar ve kapaklar ayrı etkileşim hedeflerine sahip");
                    Check(g.world.transform.Find("Büyük şehir zemini")!=null && UnityEngine.Object.FindObjectsByType<CafeTarget>().Count()>=15,"Büyük şehir alanı ve temel etkileşimler oluşturuldu");
                    g.data.money=5000; g.BuyBranch(1); Check(g.data.branches[1].owned && g.Paused,"İlk şube kiralanabiliyor ve kendi panelini açıyor");
                    var beforeBranch=g.player.transform.position; g.EnterBranch(1); Check(Vector3.Distance(g.player.transform.position,g.world.BranchRoomCenter(1)+new Vector3(0,.08f,-7.2f))<.05f,"Şube yönetiminden mobilyalı kafeye giriliyor");
                    g.ExitBranch(); Check(Vector3.Distance(g.player.transform.position,beforeBranch)<.05f,"Şubeden girilen şehir noktasına geri çıkılıyor"); g.Continue();
                    g.OpenCasino(); Check(g.Paused && g.ui!=null,"Blackjack salonu kendi oyun panelini açıyor"); g.Continue();
                    g.StartBlackjack(10); if(g.blackjackActive)g.BlackjackStand();
                    Check(!g.blackjackActive && g.data.money>=0,"Blackjack eli bahis düşümü ve sonuç ödemesiyle tamamlanıyor"); g.Continue();
                    Check(g.GetComponents<AudioSource>().Any(a=>a.clip && a.clip.name=="Cafe Tycoon - original loop" && a.isPlaying),"Yeni tycoon müziği döngüde çalıyor");
                    Check(g.customers.Count==1,"Müşteri doğuyor"); customer=g.customers[0];
                    movementStart=g.player.transform.position;
                    Physics.SyncTransforms(); g.player.Move(Vector2.up,.1f);
                    Check(Vector3.Distance(movementStart,g.player.transform.position)>.2f,"Başlangıç noktası çarpışmayla kilitlenmiyor");
                    movementStart=g.player.transform.position;
                    Check(Mathf.Abs(g.player.view.transform.position.y-g.player.transform.position.y-1.65f)<.01f,"Kamera göz hizasında");
                    Check(UnityEngine.Object.FindObjectsByType<Camera>().Length==1,"Tek aktif birinci şahıs kamera");
                    testKeyboard=InputSystem.AddDevice<Keyboard>();
                    Application.runInBackground=true;
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    movementSince=Time.realtimeSinceStartup;
                    stage=1;
                } else if(stage==1) {
                    InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));
                    if(Time.realtimeSinceStartup-movementSince<1f) return;
                    InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
                    report+="Movement: "+movementStart+" -> "+g.player.transform.position+"; paused="+g.Paused+"; key="+(Keyboard.current!=null && Keyboard.current.wKey.isPressed)+"\n";
                    Check(Vector3.Distance(movementStart,g.player.transform.position)>.1f,"W tuşu gerçek giriş sistemi üzerinden yürütüyor");
                    movementStart=g.player.transform.position;
                    Physics.SyncTransforms(); g.player.Move(Vector2.right,.1f);
                    Check(Vector3.Distance(movementStart,g.player.transform.position)>.2f,"CharacterController bar boyunca yürüyor");
                    Check(g.player.transform.position.y>-.1f,"Zemin oyuncuyu taşıyor");
                    g.player.body.enabled=false; g.player.transform.position=new Vector3(6.5f,.08f,-4.9f); g.player.transform.rotation=Quaternion.Euler(0,180,0); g.player.body.enabled=true;
                    Physics.SyncTransforms(); for(int n=0;n<10;n++) g.player.Move(Vector2.up,.1f);
                    Check(g.player.transform.position.z<-6.8f,"Öne taşınmış barın açık yanından müşteri alanına geçiliyor");
                    customer.state=CafeCustomer.State.Waiting; customer.recipe=0; customer.Interact(g);
                    Check(customer.accepted && g.noticePinned && g.notice.Contains("Tarif:"),"Müşteriye E basınca tarifi süre sınırı olmadan gösteriyor");
                    g.selected=0; g.Act("cup"); Check(g.cup!=null,"Sıcak karton bardak alınıyor");
                    g.Act("machine"); Check(g.brewTime==0,"Değirmenden önce demleme engelleniyor");
                    g.Act("grinder"); Check(g.cup.ground && g.data.beans==19,"Değirmen çekirdek tüketiyor");
                    g.Machine(); Check(g.brewTime>0,"Makine zamanlı hazırlıyor"); g.brewTime=.02f; stage=2;
                } else if(stage==2 && g.cup!=null && g.cup.brewed) {
                    g.Act("lid"); Check(g.cup.lidded,"Sıcak kapağı takılıyor");
                    baseline=g.data.money; g.Act("pickup"); Check(g.cup==null && customer.state==CafeCustomer.State.ToPickup,"İçecek bar sonuna bırakıldı, müşteri almaya gidiyor");
                    Check(g.shelf.Count==1 && g.shelf[0].visual,"Bardak müşteri alana kadar tezgahta görünüyor");
                    Check(g.shelf[0].cup.productName=="Espresso" && g.shelf[0].good,"Teslim tezgâhındaki ürünün adı tarif içeriğinden geliyor");
                    g.TakeReservedDrink(customer);
                    g.customers.Remove(customer); UnityEngine.Object.Destroy(customer.gameObject);
                    g.data.money=0; Check(!g.Buy("staff"),"Pahalı alım yetersiz bakiyeyle engelleniyor"); g.data.money=1000;
                    Check(g.Buy("machine") && g.data.machine==2,"Yükseltme satın alınıyor");
                    g.Save(); Check(Economy.Load(g.SavePath).machine==2,"Yükseltme kayıtta korunuyor");
                    g.data.money=1000;
                    Check(g.Buy("table") && g.data.tables==7,"Masa kapasitesi satın almayla artıyor");
                    Check(g.Buy("decor") && g.data.decor==1,"Dekor satın alınıyor");
                    g.selected=5; g.Act("cup"); Check(g.cup!=null && Economy.Recipes[g.cup.recipe].cold,"Soğuk içecek plastik bardakla başlıyor");
                    g.Act("ice"); g.Act("milk"); g.Act("grinder"); g.cup.brewed=true; g.cup.steps.Add("espresso"); g.Act("lid");
                    Check(g.cup.iced && g.IsGood(g.cup),"Buz → süt → espresso → kapak sırası iyi içecek yapıyor"); g.Discard();
                    g.selected=5; g.Act("cup"); g.Act("milk"); g.Act("ice"); g.Act("grinder"); g.cup.brewed=true; g.cup.steps.Add("espresso"); g.Act("lid");
                    Check(!g.IsGood(g.cup),"Yanlış malzeme sırası kötü içecek yapıyor"); g.Discard();
                    g.data.beans=0; g.data.depotBeans=20; g.Restock(); Check(g.data.beans>0,"Depodan bara stok taşınıyor");
                    g.Spawn(); var first=g.customers.Last(); first.state=CafeCustomer.State.Waiting; first.recipe=1; first.Interact(g);
                    g.Spawn(); var second=g.customers.Last(); second.state=CafeCustomer.State.Waiting; second.recipe=4; second.Interact(g);
                    g.cup=FinishedDrink(4); g.Handoff();
                    Check(g.shelf.Count==1 && g.shelf[0].cup.productName=="Americano" && g.shelf[0].good,"Americano içeriği doğru ad ve kaliteyle tanınıyor");
                    Check(first.state==CafeCustomer.State.Waiting && second.state==CafeCustomer.State.ToPickup,
                        "Siparişler farklı sırayla servis ediliyor; önceki müşteri beklemeye devam ediyor");
                    g.TakeReservedDrink(second);
                    g.cup=FinishedDrink(3); g.Handoff(); Check(g.shelf.Count==1 && g.shelf[0].owner==null,"Sipariş olmadan hazırlanmış ürün tezgahta bekliyor");
                    g.Spawn(); var future=g.customers.Last(); future.state=CafeCustomer.State.Waiting; future.recipe=3;
                    Check(g.TryMatchShelf(future) && g.shelf.Count==1 && g.shelf[0].owner==future && future.state==CafeCustomer.State.ToPickup,
                        "Sonradan gelen müşteri hazır ürününü doğrudan alıyor");
                    g.TakeReservedDrink(future); Check(g.shelf.Count==0,"Müşteri teslim noktasında bardağı kaldırıyor");
                    g.Spawn(); var impatient=g.customers.Last(); impatient.state=CafeCustomer.State.Waiting; impatient.patience=0;
                    g.Settings(); Check(g.Paused,"Ayarlar oyunu duraklatıyor"); g.Continue();
                    foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>()) foreach(var material in renderer.sharedMaterials) if(material) Check(material.shader && material.shader.isSupported,"Shader: "+material.shader.name);
                    report+="MODEL BOUNDS\n"; foreach(var p in g.prefabs) { var obj=UnityEngine.Object.Instantiate(p); var rs=obj.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds); report+=p.name+": "+b.size+"\n"; UnityEngine.Object.Destroy(obj); }
                    stage=3; since=EditorApplication.timeSinceStartup;
                } else if(stage==3 && EditorApplication.timeSinceStartup-since>3) {
                    RenderPreview(g);
                    Finish(!failed,"Runtime doğrulaması tamamlandı");
                }
            } catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void RenderPreview(CafeGame g)
        {
            g.player.body.enabled=false;
            g.player.transform.position=new Vector3(7,.08f,-7.8f);
            g.player.transform.rotation=Quaternion.Euler(0,288,0);
            g.player.body.enabled=true;
            g.player.view.transform.localRotation=Quaternion.Euler(0,0,0);
            var camera=g.player.view;
            var texture=new RenderTexture(1280,720,24); texture.Create();
            var canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=.3f;
            camera.targetTexture=texture; camera.aspect=1280f/720;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=texture;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
            File.WriteAllBytes("CafeLife-preview.png",pixels.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            UnityEngine.Object.Destroy(pixels); texture.Release(); UnityEngine.Object.Destroy(texture);
        }
        static void Finish(bool success,string message)
        {
            EditorApplication.update-=Tick; Application.logMessageReceived-=OnLog;
            if(testKeyboard!=null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard);
            var testGame=UnityEngine.Object.FindAnyObjectByType<CafeGame>();
            if(testGame && testGame.testing) { testGame.suppressSaveOnExit=true; foreach(string file in new[]{testGame.SavePath,testGame.SavePath+".tmp",testGame.SavePath+".bak"}) if(File.Exists(file))File.Delete(file); }
            SessionState.SetBool("CafeLifeVerify",false);
            File.WriteAllText("CafeLife-verification.txt",(success?"SUCCESS":"FAILED")+"\n"+message+"\n"+report);
            Debug.Log("CAFE_LIFE_VERIFY_"+(success?"OK":"FAILED")+": "+message);
            EditorApplication.Exit(success?0:1);
        }
    }
}

