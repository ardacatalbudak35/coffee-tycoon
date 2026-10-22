using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace CafeLife
{
    [Serializable] public sealed class CupWork
    {
        public int recipe;
        public string productName="";
        public bool foamed,art,perfectBrew; public bool iced, coldCup, ground, brewed, mixed, flavored, lidded, filtered;
        public bool hasMilk,hasWater;
        public float dose=18,yield=36,extraction=28,grind=5,temperature=93,milkTemperature=20; public int roast=1;
        public readonly List<float> artWidths=new List<float>();
        public readonly List<Vector2> artPoints=new List<Vector2>();
        public string syrup="";
        public readonly List<string> steps=new List<string>();
    }
    public sealed class PreparedDrink
    {
        public CupWork cup;
        public float age;
        public float Temperature=>cup.coldCup?Mathf.Lerp(6,22,age/180):Mathf.Lerp(72,22,age/120);
        public bool good;
        public GameObject visual;
        public CafeCustomer owner;
    }
    public sealed class CafeGame : MonoBehaviour
    {
        public GameObject[] prefabs;
        public GameObject[] customerPrefabs;
        public GameObject[] cityPrefabs,homePrefabs;
        public RuntimeAnimatorController customerAnimator;
        public SaveData data;
        public CafePlayer player;
        public CafeUI ui;
        public CafeWorld world; public CafeExpansion expansion;
        public readonly List<CafeCustomer> customers=new List<CafeCustomer>();
        public readonly List<PreparedDrink> shelf=new List<PreparedDrink>();
        public int selected;
        public int orderCounter;
        public CupWork cup;
        public float brewTime,brewTotal;
        public bool Paused { get; private set; } = true;
        public bool started,testing,suppressSaveOnExit;
        public string notice="Kafene hoş geldin!";
        public float noticeUntil;
        private readonly string testSavePath=Path.Combine(Path.GetTempPath(),"CafeLifeTest_"+Guid.NewGuid().ToString("N")+".json");
        public string SavePath => testing ? testSavePath : Path.Combine(Application.persistentDataPath,"CafeLife-v1.json");
        private float spawnTimer=3,saveTimer=30,staffTimer=4;
        private GameObject heldModel;
        private CafeEspressoFlow playerEspresso;
        public bool CupOnMachine => playerEspresso;
        public bool CupInHand => heldModel && heldModel.activeSelf;
        void ClearMachineCup(){if(playerEspresso){playerEspresso.gameObject.SetActive(false);Destroy(playerEspresso.gameObject);}playerEspresso=null;}
        private AudioSource audioSource,music,machineAudio;
        private AudioClip click,sale;
        private bool staffBusy;
        private float branchIncomeTimer=20f;
        public int activeBranchIndex;
        public bool noticePinned;
        private Vector3 branchReturnPosition;
        private Quaternion branchReturnRotation;
        public bool blackjackActive;
        public int blackjackBet,blackjackPlayer,blackjackDealer;
        public string blackjackMessage="Masaya oturup bahis seç.";
        void Start()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-cafe-smoke")>=0){testing=true;suppressSaveOnExit=true;}
            data=Economy.Load(SavePath);
            cup=null; brewTime=brewTotal=0; // Runtime drink state must not come from Unity scene serialization.
            data.branches[0].baristaLevel=data.baristaLevel; data.branches[0].espressoLevel=data.espressoLevel; data.branches[0].paintLevel=data.paintLevel;
            QualitySettings.SetQualityLevel(Mathf.Clamp(data.quality,0,QualitySettings.names.Length-1));
            Application.targetFrameRate=60; Application.runInBackground=true; QualitySettings.vSyncCount=0;
            world=gameObject.AddComponent<CafeWorld>(); world.game=this; world.Build();
            expansion=gameObject.AddComponent<CafeExpansion>(); expansion.game=this; expansion.Setup();
            var polish=gameObject.AddComponent<CafePolish>();polish.game=this;polish.Build();
            gameObject.AddComponent<CafeHundred>().Setup(this);
            world.SetStaffLevel(Mathf.Max(1,data.baristaLevel));
            gameObject.AddComponent<CafeFocusedMode>().Setup(this);
            ui=gameObject.AddComponent<CafeUI>(); ui.game=this; ui.Build();
            story=gameObject.AddComponent<CafeStory>(); story.game=this; gameObject.AddComponent<CafeParanormal>().game=this;
            cleaning=gameObject.AddComponent<CafeCleaning>(); cleaning.Setup(this); thief=gameObject.AddComponent<CafeThief>(); thief.Setup(this); vampire=gameObject.AddComponent<CafeVampire>(); vampire.Setup(this); ghosts=gameObject.AddComponent<CafeGhosts>(); ghosts.Setup(this);
            home=gameObject.AddComponent<CafeHome>(); home.Setup(this);
            world.RearrangeBackBar();   // her şey kurulduktan sonra: bar arkası, filtre makinesi, çeşme, süt dolabı
            audioSource=gameObject.AddComponent<AudioSource>(); click=Tone(560,.09f); sale=Tone(880,.2f);
            music=gameObject.AddComponent<AudioSource>(); music.clip=Music(); music.loop=true; music.volume=.18f; music.Play();
            machineAudio=gameObject.AddComponent<AudioSource>(); machineAudio.clip=Tone(95,1); machineAudio.loop=true; machineAudio.volume=.07f;
            AudioListener.volume=data.volume; ApplyAudio();
            ui.ShowMenu("home");
        }
        public GameObject Model(string name,Vector3 at,float width,float rotation=0,bool solid=true) => world.Model(name,at,width,rotation,solid);
        void Update()
        {
            if(data==null) return;
            if(Paused) return;
            if(!CafeFocusedMode.Enabled)TickBranchIncome(Time.deltaTime);
            float dt=Time.deltaTime;
            if(brewTime>0)
            {
                brewTime=Mathf.Max(0,brewTime-dt);var craft=GetComponent<CafeHundred>();if(craft)craft.BrewTick(dt);
                if(brewTime==0 && cup!=null) { EspressoShotDone(cup); if(craft)craft.FinishBrew();UpdateHand(); Tell("Espresso bardağa eklendi. Sıradaki: "+NextStep(),true); }
            }
            if(filterCup!=null&&filterTime>0)
            {   // filtre makinesi demleniyor; bitince kahve bardağa dolar, ışık yeşile döner
                filterTime-=dt;
                if(filterTime<=0)FilterDone();
            }
            if(world.steam) world.steam.SetActive(brewTime>0);
            if(brewTime>0 && !machineAudio.isPlaying) machineAudio.Play();
            else if(brewTime<=0 && machineAudio.isPlaying) machineAudio.Stop();
            if(data.dayTime>=300 && story && story.NightHold()) { data.dayTime=300; return; } // Gece uyumadan sabah olmaz.
            spawnTimer-=dt;
            if(spawnTimer<=0) { spawnTimer=CafeFocusedMode.Enabled?Mathf.Lerp(14,7,Mathf.Clamp01(data.served/30f)):Mathf.Max(5,20-data.decor+(80-data.reputation)*.08f)*(expansion?expansion.DemandMultiplier:1); Spawn(); }
            data.dayTime+=dt;
            if(data.dayTime>=300) AdvanceDay();
            saveTimer-=dt; if(saveTimer<=0) { Save(); saveTimer=30; }
            staffTimer-=dt;
            if(data.staff>0 && (!expansion||expansion.Data.staffOn) && staffTimer<=0 && !staffBusy) { staffTimer=45; StaffServe(); }
        }
        public CafeStory story; public CafeCleaning cleaning; public CafeHome home;
        public CafeThief thief; public CafeVampire vampire; public CafeGhosts ghosts;
        public string CareLabel(string k)=>k=="cc_mop"||k=="cc_cloth"?cleaning.ToolLabel(k):k=="cc_thief"?(thief?thief.Label():""):k=="cc_vampire"?(vampire?vampire.Label():""):k=="cc_ghost"?(ghosts?ghosts.Label():""):home.Label(k);
        public void CareUse(string k){ if(k=="cc_mop"||k=="cc_cloth")cleaning.UseTool(k); else if(k=="cc_thief"){if(thief)thief.Use();} else if(k=="cc_vampire"){if(vampire)vampire.Use();} else if(k=="cc_ghost"){if(ghosts)ghosts.Use();} else home.Use(k); }
        public void AdvanceDay() { if(expansion)expansion.EndDay(data.day); data.day++; data.dayTime=data.dayTime>=300?data.dayTime-300:0; data.earned=0; int dirty=cleaning?cleaning.Overnight():0; Tell("Yeni gün: "+data.day+(dirty>0?" · gece kafe tozlanmış, "+dirty+" yeni toz var":"")); Save(); }
        public void StoryPause(bool on)
        { if(on) { Paused=true; if(machineAudio) machineAudio.Stop(); player.Lock(false); } else { Paused=false; player.Lock(!CafeFocusedMode.CounterControls); } }
        // Filtre kahve makinesi: bardak makineye konur, 20 sn sonra hazır olur, o sırada eller serbesttir
        public CupWork filterCup; public float filterTime;
        public string FilterLabel()
        {
            if(filterCup!=null) return filterTime>0?"Filtre kahve demleniyor · "+Mathf.CeilToInt(filterTime)+" sn":(cup==null?"Filtre kahveyi makineden al [E]":"Önce elindeki içeceği bırak");
            if(cup==null) return "Filtre kahve makinesi · önce bardak al";
            if(cup.coldCup) return "Filtre kahve yalnızca sıcak bardakla yapılır";
            if(cup.filtered) return "Filtre kahve zaten eklendi";
            return "Bardağı filtre makinesine koy · "+Mathf.RoundToInt(CafeWorld.FilterSeconds)+" sn [E]";
        }
        // Demleme bitti: kahve bardağa dolar, ışık yeşile döner
        public void FilterDone()
        {
            if(filterCup==null)return; filterTime=0; filterCup.filtered=true; filterCup.steps.Add("filter");
            world.SetFilterCup(filterCup); world.SetFilterReady(true); Tell("Filtre kahve hazır! Makineden bardağı al.",true);
        }
        void FilterAct()
        {
            if(filterCup!=null)
            {
                if(filterTime>0){ Tell("Filtre kahve demleniyor: "+Mathf.CeilToInt(filterTime)+" sn kaldı. Bu sırada başka işe bakabilirsin."); return; }
                if(cup!=null){ Tell("Önce elindeki içeceği bırak ya da teslim et."); return; }
                cup=filterCup; filterCup=null; world.ClearFilterCup(); UpdateHand(); Save(); Tell("Filtre kahve hazır. Sıradaki: "+NextStep(),true); return;
            }
            if(cup==null){ Tell("Önce bardak rafından bardak al."); return; }
            if(cup.lidded){ Tell("Kapak takıldı. İçeceği teslim et veya çöpe at."); return; }
            if(cup.coldCup){ Tell("Filtre kahve yalnızca sıcak (karton) bardakla yapılır."); return; }
            if(cup.filtered){ Tell("Filtre kahve zaten eklendi."); return; }
            if(data.beans<=0){ Tell("Çekirdek bitti. Depodan getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
            data.beans--; filterCup=cup; cup=null; filterTime=CafeWorld.FilterSeconds; world.SetFilterCup(filterCup); UpdateHand(); Save();
            Tell("Bardak filtre makinesine kondu. Kahve "+Mathf.RoundToInt(CafeWorld.FilterSeconds)+" saniyede hazır olur; bu sırada başka işe bakabilirsin.",true);
        }
        // Bir espresso shot'ı bitti: tarifteki shot sayısına ulaşılmadıysa bardak yeniden öğütme bekler
        public void EspressoShotDone(CupWork c)
        {
            c.steps.Add("espresso"); int want=Mathf.Max(1,Economy.Recipes[c.recipe].shots); int done=c.steps.Count(s=>s=="espresso");
            c.brewed=done>=want; if(!c.brewed)c.ground=false;
        }
        public void Continue() { started=true; Paused=false; ui.ShowMenu(""); player.Lock(!CafeFocusedMode.CounterControls); }
        public void TogglePause()
        { if(CafeStory.Busy) return; if(Paused && started) Continue(); else { Paused=true; if(machineAudio) machineAudio.Stop(); player.Lock(false); ui.ShowMenu("pause"); Save(); } }
        public CafeVenue activeVenue,trackedVenue;
        public void OpenOperations(string section="business") { Paused=true; if(machineAudio)machineAudio.Stop(); player.Lock(false); ui.operationSection=section; ui.SetStudioSection(section);ui.ShowMenu("operations"); }
        public void OpenDistrict() { if(CafeFocusedMode.Enabled){OpenStreetPage("ranking");return;}Paused=true; player.Lock(false); ui.ShowMenu("district"); }
        public void OpenVenue(CafeVenue venue) { activeVenue=venue; Paused=true; if(machineAudio) machineAudio.Stop(); player.Lock(false); ui.ShowMenu("venue"); }
        public void OpenShop() => OpenOperations("upgrade");
        public void Settings() => OpenOperations("settings");
        public void NewGame()
        {
            StopAllCoroutines(); staffBusy=false; staffTimer=4; branchIncomeTimer=20; orderCounter=0; activeBranchIndex=0;
            blackjackActive=false; blackjackBet=blackjackPlayer=blackjackDealer=0; blackjackMessage="Masaya oturup bahis seç.";
            if(machineAudio)machineAudio.Stop(); if(world){world.StaffClearCup();world.SetStaffStatus("Sipariş bekliyor");}
            if(File.Exists(SavePath)) File.Copy(SavePath,SavePath+".before-new-game",true);
            foreach(var c in customers.ToArray()) if(c) Destroy(c.gameObject);
            customers.Clear(); foreach(var drink in shelf) if(drink.visual) Destroy(drink.visual); shelf.Clear();
            data=new SaveData(); cup=null; brewTime=0; noticePinned=false; filterCup=null; filterTime=0; world.ClearFilterCup();
            if(player) { player.body.enabled=false; player.transform.position=new Vector3(0,.08f,-9.7f); player.transform.rotation=Quaternion.identity; player.body.enabled=true; }
            GetComponent<CafeHundred>()?.ResetSession();UpdateHand(); world.Refresh(); if(expansion)expansion.ResetRun(); selected=0; Save(); Continue();
        }
        public void Tell(string text,bool good=false)
        { notice=text; noticePinned=false; noticeUntil=Time.unscaledTime+5; if(audioSource) audioSource.PlayOneShot(good?sale:click,.25f); }
        public void TellRecipe(string text)
        { notice=text; noticePinned=true; noticeUntil=float.PositiveInfinity; if(audioSource) audioSource.PlayOneShot(sale,.25f); }
        public void Machine() => Act("machine");
        public static string[] IdealSteps(Recipe r)
        {
            var steps=new List<string>(); if(r.cold) steps.Add("ice");
            if(r.syrup!="") steps.Add(r.syrup);
            if(r.cold){if(r.milk)steps.Add("milk");else if(r.water)steps.Add("water");}
            if(r.filter)steps.Add("filter");                         // filtre kahve önce dolar, shotlar üstüne eklenir
            for(int i=0;i<r.shots;i++)steps.Add("espresso");          // her shot ayrı öğütme + demleme
            if(!r.cold){if(r.milk)steps.Add("milk");else if(r.water)steps.Add("water");} steps.Add("lid"); return steps.ToArray();
        }
        public static int RecipeForCup(int selected,bool coldCup)
        {
            selected=Mathf.Clamp(selected,0,Economy.Recipes.Length-1);
            int hotRecipe=selected>=10?selected:selected==9?0:selected>=5?selected-4:selected;   // 10+ yalnızca sıcak servis edilir
            return coldCup?(hotRecipe>=10?hotRecipe:hotRecipe==0?9:hotRecipe+4):hotRecipe;
        }
        public bool IsGood(CupWork work)
        {
            if(work==null || work.recipe<0 || work.recipe>=Economy.Recipes.Length) return false;
            var ideal=IdealSteps(Economy.Recipes[work.recipe]);
            if(work.steps.Count!=ideal.Length) return false;
            for(int i=0;i<ideal.Length;i++) if(work.steps[i]!=ideal[i]) return false;
            return work.yield/Mathf.Max(1,work.dose)>=1.55f && work.yield/work.dose<=2.55f && CafeHundred.Score(work)>=.55f;
        }
        public string NextStep()
        {
            if(cup==null) return filterCup!=null?(filterTime>0?"Filtre kahve demleniyor":"Filtre makinesinden bardağı al"):"Bardak rafı";
            if(CupOnMachine) return brewTime>0?"Espresso demleniyor":"Makineden bardağı al";
            if(cup.lidded) return "Teslim tezgâhı";
            var r=Economy.Recipes[cup.recipe];
            if(r.cold&&!cup.iced) return "Buz kovası";
            if(r.syrup!=""&&cup.syrup=="") return r.syrup=="caramel"?"Karamel şurubu":"Vanilya şurubu";
            if(r.milk&&!cup.hasMilk&&(r.cold||cup.brewed)) return "Süt dolabı";
            if(r.water&&!cup.hasWater&&(r.cold||cup.brewed)) return "Arıtma çeşmesi";
            if(r.filter&&!cup.filtered) return "Filtre kahve makinesi";
            if(r.shots>0){
                if(!cup.ground) return cup.steps.Contains("espresso")?"Değirmen (sıradaki shot)":"Değirmen";
                if(!cup.brewed) return brewTime>0?"Espresso demleniyor":"Espresso makinesi";
            }
            var focused=GetComponent<CafeFocusedMode>();
            var customer=focused?focused.selectedOrder:null;
            if(customer&&customer.recipe==cup.recipe&&CafeService.Preference(customer)==1&&!cup.art){
                if(!cup.foamed)return expansion&&expansion.foamTime>0?"Süt köpürtülüyor":"Süt köpürtme";
                return "Latte art istasyonu";
            }
            if(!cup.lidded) return "Kapak istasyonu";
            return "Teslim tezgâhı";
        }
        public void Act(string kind)
        {
            if(CupOnMachine && kind!="machine" && (kind=="foam"||kind=="art"||kind=="grinder"||kind=="filter"||kind=="milk"||kind=="water"||kind=="ice"||kind=="caramel"||kind=="vanilla"||kind.StartsWith("lid")||kind=="pickup")){Tell("Önce makinedeki bardağı E ile al.");return;}
            if(expansion && expansion.Action(kind))return;
            if(kind=="machine" && expansion && brewTime>0 && expansion.StopBrew())return;
            if(kind=="machine" && CupOnMachine && brewTime<=0){ClearMachineCup();UpdateHand();Tell("Bardak eline alındı. Sıradaki: "+NextStep(),true);return;}
            if(kind=="shop") { OpenShop(); return; }
            if(kind=="trash") { Discard(); return; }
            if(kind=="depot") { Restock(); return; }
            if(kind=="filter") { FilterAct(); return; }
            var r=Economy.Recipes[selected];
            if(kind=="cup"||kind=="cup_hot"||kind=="cup_cold")
            {
                if(cup!=null) { Tell("Önce elindeki içeceği bitir ya da çöpe at."); return; }
                bool cold=kind=="cup_cold" || kind=="cup"&&r.cold;
                if(cold&&selected>=10){ Tell(r.name+" yalnızca sıcak servis edilir. Karton bardak al."); return; }
                int recipe=RecipeForCup(selected,cold);
                r=Economy.Recipes[recipe];
                if(cold ? data.cupsCold<=0 : data.cupsHot<=0) { Tell("Bardak bitti! Depodan bara getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                if(cold) data.cupsCold--; else data.cupsHot--;
                selected=recipe; cup=new CupWork{recipe=recipe,coldCup=cold}; UpdateHand(); Save();
                if(expansion)expansion.Ingredient(kind);
                Tell((cold?"Şeffaf plastik":"Karton")+" bardak alındı."); return;
            }
            if(cup==null) { Tell("Önce bardak rafından bardak al."); return; }
            if(cup.lidded&&kind!="pickup"){Tell("Kapak takıldı. İçeceği teslim et veya çöpe at.");return;}
            r=Economy.Recipes[cup.recipe];
            if(kind=="ice")
            {
                if(!cup.coldCup) { Tell("Soğuk içecek için önce şeffaf plastik bardak al."); return; }
                if(cup.iced) { Tell("Buz zaten eklendi."); return; }
                if(data.ice<=0) { Tell("Barın buzu bitti. Depodan getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                data.ice--; cup.iced=true; cup.steps.Add("ice");
            }
            else if(kind=="grinder")
            {
                if(cup.ground) { Tell("Çekirdek zaten öğütüldü."); return; }
                if(data.beans<=0) { Tell("Çekirdek bitti. Depodan getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                data.beans--; cup.ground=true;var craft=GetComponent<CafeHundred>();if(craft)craft.Ground(cup);
            }
            else if(kind=="machine")
            {
                if(!cup.ground||cup.brewed||brewTime>0) { Tell("Demlemek için önce çekirdek öğüt; her shot için çekirdek yeniden öğütülür."); return; }
                brewTotal=4f/(1+(data.machine-1)*.25f); brewTime=brewTotal;playerEspresso=CafeEspressoFlow.Begin(this);UpdateHand();
                if(expansion)expansion.BrewStarted();var craft=GetComponent<CafeHundred>();if(craft)craft.StartBrew(); Tell("Espresso demleniyor..."); Save(); return;
            }
            else if(kind=="milk"||kind=="water")
            {
                if(kind=="milk"?cup.hasMilk:cup.hasWater) { Tell("Bu malzeme zaten eklendi."); return; }
                if(kind=="milk"&&data.milk<=0) { Tell("Süt dolabı boş. Depodan süt getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                if(kind=="milk") { data.milk--; cup.hasMilk=true; } else cup.hasWater=true;
                cup.mixed=true; cup.steps.Add(kind);
            }
            else if(kind=="caramel"||kind=="vanilla")
            {
                if(cup.syrup!="") { Tell("Şurup zaten eklendi."); return; }
                if(kind=="caramel" ? data.caramel<=0 : data.vanilla<=0) { Tell("Şurup bitti. Depodan getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                if(kind=="caramel")data.caramel--; else data.vanilla--; cup.flavored=true; cup.syrup=kind; cup.steps.Add(kind);
            }
            else if(kind=="lid"||kind=="lid_hot"||kind=="lid_cold")
            {
                if(CupOnMachine){Tell("Önce makinedeki bardağı E ile al.");return;}
            if(brewTime>0||(expansion&&expansion.foamTime>0)){Tell("Hazırlama bitmeden kapak takılamaz.");return;}
                if(kind!="lid"&&cup.coldCup!=(kind=="lid_cold")) { Tell(kind=="lid_cold"?"Bu içecek için sıcak karton kapak kullan.":"Bu içecek için soğuk plastik kapak kullan."); return; }
                if(cup.lidded) { Tell("Kapak zaten takıldı."); return; }
                if(r.cold?data.lidsCold<=0:data.lidsHot<=0) { Tell("Kapak bitti. Depodan getir. Menü → Tedarik: paket al veya rafları tamamla."); return; }
                if(r.cold)data.lidsCold--; else data.lidsHot--; cup.lidded=true; cup.steps.Add("lid");
            }
            else if(kind=="pickup") { Handoff(); return; }
            else return;
            if(expansion)expansion.Ingredient(kind); UpdateHand(); Save(); Tell("Eklendi. "+(cup.lidded?(IsGood(cup)?"Tarif doğru.":"Tarif veya sıra hatalı; ürün kötü."):"Sıradaki: "+NextStep()),true);
        }
        public void UpdateHand()
        {
            if(heldModel){heldModel.SetActive(false);Destroy(heldModel);heldModel=null;}
            if(cup==null)ClearMachineCup();
            if(cup==null||CupOnMachine||player==null||world==null) return;
            heldModel=world.CupModel(cup,Economy.Recipes[cup.recipe],player.view.transform); if(expansion)heldModel.AddComponent<CafeCupMotion>().expansion=expansion;if(CafeFocusedMode.CounterControls){heldModel.transform.localPosition=new Vector3(.02f,-.07f,.75f);heldModel.transform.localRotation=Quaternion.Euler(-20,0,0);}
        }
        public void Discard()
        { if(cup==null) return;GetComponent<CafeLattePour>()?.Cancel();var craft=GetComponent<CafeHundred>();if(craft)craft.Waste(); if(expansion)expansion.CancelWork(); cup=null; brewTime=0; UpdateHand(); Tell("İçecek atıldı; kullanılan malzemeler geri gelmez."); }
        public void Handoff()
        {
            if(CupOnMachine){Tell("Önce makinedeki bardağı E ile al.");return;}
            if(brewTime>0||(expansion&&expansion.foamTime>0)){Tell("İçecek hâlâ hazırlanıyor. Önce hazırlamayı bitir.");return;}
            if(cup==null||!cup.lidded) { Tell("Teslim için bitmiş, kapaklı içecek gerekli."); return; }
            if(shelf.Count>=6) { Tell("Teslim tezgâhı dolu. Önce müşteri alsın."); return; }
            cup.recipe=IdentifyRecipe(cup); cup.productName=Economy.Recipes[cup.recipe].name;
            var drink=new PreparedDrink{cup=cup,good=IsGood(cup)};
            drink.visual=world.CupModel(cup,Economy.Recipes[cup.recipe],world.transform,false);
            world.AddProductLabel(drink.visual,cup.productName);
            shelf.Add(drink); RefreshShelf(); cup=null; UpdateHand();
            Tell(drink.cup.productName+" bar sonunda bekliyor: "+(drink.good?"İYİ":"KÖTÜ")+" kalite.",drink.good);
            foreach(var c in customers.ToArray()) if(c && c.state==CafeCustomer.State.Waiting) TryMatchShelf(c);
            Save();
        }
        int IdentifyRecipe(CupWork work)
        {
            int shots=work.steps.Count(s=>s=="espresso"); bool coldDrink=work.coldCup||work.iced;
            if(work.filtered&&!coldDrink){ int idx=shots==0?10:shots==1?12:shots==2?13:14; work.productName=Economy.Recipes[idx].name; return idx; }        // Filtre / Red Eye / Black Eye / Dead Eye
            if(!coldDrink&&work.hasMilk&&!work.hasWater&&work.syrup==""&&shots>=2){ work.productName=Economy.Recipes[11].name; return 11; }              // Flat White
            string baseName=work.hasMilk?(work.hasWater?"Sütlü Americano":"Latte"):(work.hasWater?"Americano":"Espresso");
            if(work.syrup=="caramel") baseName="Karamel "+baseName;
            if(work.syrup=="vanilla") baseName="Vanilyalı "+baseName;
            bool cold=work.coldCup||work.iced;
            work.productName=(work.iced?"Buzlu ":cold?"Soğuk ":"")+baseName;
            if(cold)
            {
                if(work.syrup=="caramel")return 6;
                if(work.syrup=="vanilla")return 7;
                if(work.hasMilk)return 5;
                if(work.hasWater)return 8;
                return 9;
            }
            if(work.syrup=="caramel")return 2;
            if(work.syrup=="vanilla")return 3;
            if(work.hasMilk)return 1;
            if(work.hasWater)return 4;
            return 0;
        }
        public void ReflowShelf()=>RefreshShelf();
        void RefreshShelf()
        { for(int i=0;i<shelf.Count;i++) if(shelf[i].visual) world.PositionPrepared(shelf[i].visual,i); }
        public bool TryMatchShelf(CafeCustomer customer)
        {
            if(customer==null || customer.state!=CafeCustomer.State.Waiting) return false;
            var drink=shelf.Where(d=>d.owner==null && d.cup.recipe==customer.recipe).OrderByDescending(d=>d.good).ThenByDescending(d=>CafeService.Meets(customer,d)).ThenByDescending(CafeService.Quality).ThenBy(d=>d.age).FirstOrDefault();
            if(drink==null) return false;
            if(!customer.accepted){customer.accepted=true;customer.orderNumber=++orderCounter;}
            CafeService.Assess(customer,drink);drink.owner=customer;world.AddProductLabel(drink.visual,"#"+customer.orderNumber+" · "+Economy.Recipes[customer.recipe].name);
            customer.Deliver(false,drink.good);
            Tell(Economy.Recipes[customer.recipe].name+" müşteri tarafından teslimden alınıyor.",drink.good);
            return true;
        }
        public void TakeReservedDrink(CafeCustomer customer)
        {
            var drink=shelf.FirstOrDefault(d=>d.owner==customer);
            if(drink==null)return;
            customer.goodDrink=drink.good;CafeService.Assess(customer,drink);shelf.Remove(drink); if(drink.visual) Destroy(drink.visual); RefreshShelf();
        }
        public bool DiscardPrepared(PreparedDrink drink){if(drink==null||!shelf.Contains(drink)||drink.owner!=null){Tell("Müşteriye ayrılmış içecek korunur.");return false;}shelf.Remove(drink);if(drink.visual)Destroy(drink.visual);RefreshShelf();GetComponent<CafeHundred>()?.Waste();Tell("Sahipsiz içecek atıldı; malzemeler geri verilmez.");Save();return true;}
        public void Restock()
        {
            int moved=0;
            moved+=Transfer(ref data.depotBeans,ref data.beans,20);
            moved+=Transfer(ref data.depotMilk,ref data.milk,12);
            moved+=Transfer(ref data.depotIce,ref data.ice,12);
            moved+=Transfer(ref data.depotCupsHot,ref data.cupsHot,12);
            moved+=Transfer(ref data.depotCupsCold,ref data.cupsCold,12);
            moved+=Transfer(ref data.depotLidsHot,ref data.lidsHot,12);
            moved+=Transfer(ref data.depotLidsCold,ref data.lidsCold,12);
            moved+=Transfer(ref data.depotCaramel,ref data.caramel,10);
            moved+=Transfer(ref data.depotVanilla,ref data.vanilla,10);
            Tell(moved>0 ? moved+" ürün depodan bara taşındı." : "Depo boş veya bardaki raflar dolu.",moved>0); Save();
        }
        static int Transfer(ref int depot,ref int bar,int capacity)
        { int count=Mathf.Min(depot,Mathf.Max(0,capacity-bar)); depot-=count; bar+=count; return count; }
        public void Spawn()
        {
            if(expansion && !expansion.Data.open)return;
            if(CafeFocusedMode.Enabled&&customers.Count(c=>c&&(c.state==CafeCustomer.State.Waiting||c.state==CafeCustomer.State.Entering))>=(data.served<10?2:3))return;
            if(customers.Count>=Mathf.Min(data.tables*2,24)) return;
            int slot=-1;
            for(int s=0;s<24;s++) { bool used=false; foreach(var c in customers) if(c&&c.slot==s) used=true; if(!used){slot=s;break;} }
            if(slot<0)return;
            var go=new GameObject("Müşteri"); var cnew=go.AddComponent<CafeCustomer>();
            cnew.game=this; cnew.slot=slot; customers.Add(cnew); cnew.Setup();
        }
        public bool SpendExpense(int cost) { if(!Economy.Spend(data,cost))return false; if(expansion)expansion.Expense(cost); return true; }
        public bool Buy(string item)
        {
            int cost=0;
            switch(item)
            {
                case "machine": if(data.machine>=5){Tell("Makine en yüksek seviyede.");return false;} cost=data.machine*90;break;
                case "table": if(data.tables>=12){Tell("Tüm masalar açık.");return false;} cost=140+data.tables*30;break;
                case "decor": if(data.decor>=6)return false;cost=50+data.decor*30;break;
                case "stock": cost=35;break;
                case "staff": if(data.staff>0)return false;cost=350;break;
                case "barista": if(data.staff==0){Tell("Önce barista işe al.");return false;} if(data.baristaLevel>=5)return false; cost=450+data.baristaLevel*850;break;
                case "espresso": if(data.espressoLevel>=10)return false; cost=350+data.espressoLevel*400;break;
                case "paint": if(data.paintLevel>=8)return false; cost=200+data.paintLevel*350;break;
                case "outfit":cost=25;break;
                default:return false;
            }
            if(!SpendExpense(cost)){Tell("Yetersiz para.");return false;}
            switch(item)
            {
                case "machine":data.machine++;break;
                case "table":data.tables++;break;
                case "decor":data.decor++;break;
                case "stock":data.depotBeans+=20;data.depotMilk+=12;data.depotIce+=12;data.depotCupsHot+=12;data.depotCupsCold+=12;data.depotLidsHot+=12;data.depotLidsCold+=12;data.depotCaramel+=10;data.depotVanilla+=10;break;
                case "staff":data.staff=1;data.baristaLevel=Mathf.Max(1,data.baristaLevel);staffTimer=2;break;
                case "barista":data.baristaLevel++;world.SetStaffLevel(data.baristaLevel);break;
                case "espresso":data.espressoLevel++;break;
                case "paint":data.paintLevel++;data.reputation=Mathf.Min(100,data.reputation+3);break;
                case "outfit":data.outfit=(data.outfit+1)%4;break;
            }
            data.branches[0].baristaLevel=data.baristaLevel; data.branches[0].espressoLevel=data.espressoLevel; data.branches[0].paintLevel=data.paintLevel;
            world.Refresh();Save();Tell(item=="stock"?"Tedarik depoya geldi. Arka raftan bara taşı!":"Satın alındı!",true);return true;
        }
        public bool Unlock(int index) { Tell("Tüm içecekler başlangıçta açık."); return false; }
        public void Quest()
        {
            int goal=(data.quest+1)*5;
            if(data.served<goal){Tell("Görev: toplam "+goal+" başarılı servis.");return;}
            int reward=60+data.quest*30;data.money+=reward;if(expansion)expansion.Data.dayIncome+=reward;data.xp+=50;data.quest++;Save();Tell("Görev ödülü +"+reward,true);
        }
        public void RescueStock()
        {
            Restock();if(Enumerable.Range(0,Economy.Recipes.Length).Any(i=>Economy.HasStock(data,i))){Tell("Mevcut stokla içecek hazırlayabilirsin.");return;}
            int needed=(data.beans<=0?25:0)+(data.cupsHot<=0||data.lidsHot<=0?18:0);
            if(data.money>=needed){Tell("Tedarik için yeterli paran var. Menü → Tedarik bölümünü kullan.");return;}
            data.depotBeans+=3;data.depotCupsHot+=3;data.depotLidsHot+=3;Restock();Save();Tell("Acil destek: 3 espresso için çekirdek, bardak ve kapak bara geldi.",true);
        }
        static readonly int[] BranchRents={0,2500,12000,50000,180000};
        public void OpenBranch(int index)
        { activeBranchIndex=Mathf.Clamp(index,0,4); Paused=true; player.Lock(false); ui.ShowMenu("branch"); }
        public void EnterBranch(int index)
        {
            if(index<1||index>=data.branches.Length||!data.branches[index].owned){Tell("Önce bu kafeyi kirala.");return;}
            activeBranchIndex=index; branchReturnPosition=player.transform.position; branchReturnRotation=player.transform.rotation;
            var controller=player.body; controller.enabled=false; player.transform.position=world.BranchRoomCenter(index)+new Vector3(0,.08f,-7.2f); player.transform.rotation=Quaternion.identity; controller.enabled=true;
            Paused=false; ui.ShowMenu(""); player.Lock(true);
        }
        public void ExitBranch()
        {
            var controller=player.body; controller.enabled=false;
            player.transform.position=branchReturnPosition==Vector3.zero?world.BranchStreetEntry(activeBranchIndex):branchReturnPosition;
            player.transform.rotation=branchReturnRotation; controller.enabled=true;
        }
        public void OpenCasino() { var table=GetComponent<CafeTableBlackjack>();if(!table)table=gameObject.AddComponent<CafeTableBlackjack>();table.Enter(this); }
        public void UpgradeMainBarista()
        {
            if(data.staff==0) { if(Buy("staff")) data.branches[0].baristaLevel=data.baristaLevel; }
            else UpgradeBranch(0,"barista");
        }
        public void BuyBranch(int index)
        {
            if(index<1||index>=data.branches.Length)return;
            if(data.branches[index].owned){OpenBranch(index);return;}
            if(!data.branches[index-1].owned){Tell("Önce bir önceki kafeyi açmalısın.");return;}
            if(!SpendExpense(BranchRents[index])){Tell("Bu kafeyi kiralamak için daha fazla para gerekiyor.");return;}
            var b=data.branches[index]; b.owned=true; b.baristaLevel=0; b.espressoLevel=1; b.paintLevel=0; data.xp+=80;
            world.RefreshBranch(index); Save(); Tell(b.name+" artık senin! Yönetim panelinden aç.",true); OpenBranch(index);
        }
        public void UpgradeBranch(int index,string upgrade)
        {
            if(index<0||index>=data.branches.Length)return;
            var b=data.branches[index]; if(!b.owned){BuyBranch(index);return;}
            int level=upgrade=="barista"?b.baristaLevel:upgrade=="espresso"?b.espressoLevel:b.paintLevel;
            int max=upgrade=="barista"?5:upgrade=="espresso"?10:8;
            if(level>=max){Tell("Bu geliştirme en yüksek seviyede.");return;}
            int cost=upgrade=="barista"?650+level*1250:upgrade=="espresso"?500+level*650:350+level*600;
            if(!SpendExpense(cost)){Tell("Yetersiz para.");return;}
            if(upgrade=="barista")b.baristaLevel++;
            else if(upgrade=="espresso")b.espressoLevel++;
            else { b.paintLevel++; data.reputation=Mathf.Min(100,data.reputation+2); }
            world.RefreshBranch(index); if(index==0){world.SetStaffLevel(Mathf.Max(1,b.baristaLevel));data.baristaLevel=b.baristaLevel;data.espressoLevel=b.espressoLevel;data.paintLevel=b.paintLevel;}
            Save(); ui.ShowMenu("branch");
        }
        void TickBranchIncome(float delta)
        {
            branchIncomeTimer-=delta; if(branchIncomeTimer>0)return; branchIncomeTimer=20f;
            if(data.branches==null)return;
            int total=0;
            for(int i=0;i<data.branches.Length;i++)
            {
                int earned=Economy.BranchIncome(data.branches[i]);
                data.money+=earned; total+=earned; if(expansion)expansion.Data.dayIncome+=earned;
            }
            if(total>0){Tell("Şubelerin otomatik kazancı: +"+total+" coin",true);Save();}
        }
        CafeCasino Casino {get {var c=GetComponent<CafeCasino>();return c?c:gameObject.AddComponent<CafeCasino>();}}
        public void StartBlackjack(int bet)=>Casino.StartHand(bet);
        public void BlackjackHit()=>Casino.Hit();
        public void BlackjackStand()=>Casino.Stand();
        public void OpenStreetPage(string page){if(page=="arcade"){GetComponent<CafeBasketball>()?.Interact();return;}Paused=true;player.Lock(false);if(machineAudio)machineAudio.Stop();if(page=="arcade"&&!GetComponent<CafeStreetActivities>())gameObject.AddComponent<CafeStreetActivities>();ui.ShowMenu(page);}
        private void StaffServe()
        {
            var first=customers.Where(c=>c&&c.state==CafeCustomer.State.Waiting)
                .OrderBy(c=>c.accepted?c.orderNumber:int.MaxValue).ThenBy(c=>c.slot).FirstOrDefault();
            if(first==null)return;
            if(data.money<3||!Economy.HasStock(data,first.recipe)) { world.SetStaffStatus(data.money<3?"Kasada para yok":"Stok eksik"); return; }
            if(!first.accepted) { first.accepted=true; first.orderNumber=++orderCounter; }
            staffBusy=true; data.money-=3; if(expansion)expansion.Expense(3); Save(); StartCoroutine(StaffPrepare(first));
        }
        IEnumerator StaffPrepare(CafeCustomer customer)
        {
            var r=Economy.Recipes[customer.recipe];
            Economy.StartRecipe(data,customer.recipe);
            if(r.cold){data.cupsCold--;data.lidsCold--;}else{data.cupsHot--;data.lidsHot--;}
            if(r.syrup=="caramel")data.caramel--;if(r.syrup=="vanilla")data.vanilla--;
            var work=new CupWork{recipe=customer.recipe,productName=r.name,coldCup=r.cold};
            world.SetStaffStatus("Bardak alıyor"); yield return world.StaffMoveTo(new Vector3(-4.75f,0,-4.9f));
            yield return StaffSkillWait(1.5f); world.StaffHoldCup(work);
            if(r.cold) { world.SetStaffStatus("Buz koyuyor"); yield return world.StaffMoveTo(new Vector3(-3f,0,-4.9f)); yield return StaffSkillWait(2.2f); work.iced=true; work.steps.Add("ice"); world.StaffHoldCup(work); }
            if(r.syrup!="") { world.SetStaffStatus(r.syrup=="vanilla"?"Vanilya ekliyor":"Karamel ekliyor"); yield return world.StaffMoveTo(new Vector3(r.syrup=="vanilla"?1.7f:.75f,0,-4.9f)); yield return StaffSkillWait(2.1f); work.syrup=r.syrup; work.flavored=true; work.steps.Add(r.syrup); world.StaffHoldCup(work); }
            if(r.cold&&r.milk) { world.SetStaffStatus("Süt ekliyor"); yield return world.StaffMoveTo(new Vector3(-.55f,0,-4.9f)); yield return StaffSkillWait(2.4f); work.hasMilk=true; work.mixed=true; work.steps.Add("milk"); world.StaffHoldCup(work); }
            if(r.cold&&r.water) { world.SetStaffStatus("Su ekliyor"); yield return world.StaffMoveTo(new Vector3(-.9f,0,-4.05f)); yield return StaffSkillWait(2.3f); work.hasWater=true; work.mixed=true; work.steps.Add("water"); world.StaffHoldCup(work); }
            if(r.filter) { world.SetStaffStatus("Filtre kahve dolduruyor"); yield return world.StaffMoveTo(new Vector3(.6f,0,-4.05f)); world.StaffClearCup(); yield return StaffSkillWait(CafeWorld.FilterSeconds); work.filtered=true; work.steps.Add("filter"); world.StaffHoldCup(work); }
            for(int shot=0;shot<r.shots;shot++)
            {
                world.SetStaffStatus("Çekirdek öğütüyor"); yield return world.StaffMoveTo(new Vector3(-1.9f,0,-4.9f)); yield return StaffSkillWait(3f); work.ground=true;
                world.SetStaffStatus("Espresso demleniyor"); yield return world.StaffMoveTo(new Vector3(-.55f,0,-4.9f)); world.StaffClearCup();var pouring=CafeEspressoFlow.Begin(this,true,5f/Mathf.Lerp(1f,3f,Mathf.Clamp01((data.baristaLevel-1)/4f)));yield return pouring.StaffPour(); work.brewed=true; work.steps.Add("espresso"); world.StaffHoldCup(work);
            }
            if(!r.cold&&r.milk) { world.SetStaffStatus("Süt ekliyor"); yield return world.StaffMoveTo(new Vector3(-.55f,0,-4.9f)); yield return StaffSkillWait(2.4f); work.hasMilk=true; work.mixed=true; work.steps.Add("milk"); world.StaffHoldCup(work); }
            if(!r.cold&&r.water) { world.SetStaffStatus("Su ekliyor"); yield return world.StaffMoveTo(new Vector3(-.9f,0,-4.05f)); yield return StaffSkillWait(2.3f); work.hasWater=true; work.mixed=true; work.steps.Add("water"); world.StaffHoldCup(work); }
            world.SetStaffStatus("Kapak takıyor"); yield return world.StaffMoveTo(new Vector3(2.8f,0,-4.9f)); yield return StaffSkillWait(2f); work.lidded=true; work.steps.Add("lid"); world.StaffHoldCup(work);
            world.SetStaffStatus("Siparişi teslim ediyor"); yield return world.StaffMoveTo(new Vector3(4.7f,0,-4.9f));
            if(customer && customer.state==CafeCustomer.State.Waiting)
            {
                var drink=new PreparedDrink{cup=work,good=IsGood(work),owner=customer};
                drink.visual=world.PlaceStaffCup(work,shelf.Count); shelf.Add(drink); RefreshShelf(); customer.Deliver(true,drink.good);
                Tell("Barista siparişi hazırlayıp teslim etti.",drink.good); Save();
            }
            world.StaffClearCup(); world.SetStaffStatus("Sipariş bekliyor"); staffBusy=false; staffTimer=Mathf.Max(5,45-data.baristaLevel*8);
        }
        IEnumerator StaffWait(float seconds)
        { while(seconds>0) { if(!Paused) seconds-=Time.deltaTime; yield return null; } }
        IEnumerator StaffSkillWait(float seconds) => StaffWait(seconds/Mathf.Lerp(1f,3f,Mathf.Clamp01((data.baristaLevel-1)/4f)));
        public float musicMul=1;
        public void ApplyAudio(){if(!expansion)return;if(music)music.volume=.36f*expansion.Data.music*musicMul;if(machineAudio)machineAudio.volume=.12f*expansion.Data.effects;if(audioSource)audioSource.volume=expansion.Data.effects;}
        public void Save()
        { if(data==null)return;try{Economy.Save(data,SavePath);if(expansion){expansion.saveStatus="Kaydedildi";expansion.lastSavedFrame=Time.frameCount;}}catch(Exception e){Debug.LogError("Kayıt başarısız: "+e.Message);Tell("Kayıt yapılamadı; disk alanını kontrol et.");} }
        void OnApplicationPause(bool paused){if(paused&&!suppressSaveOnExit)Save();}
        void OnApplicationQuit(){if(!suppressSaveOnExit)Save();}
        void OnDestroy(){if(click)Destroy(click);if(sale)Destroy(sale);if(music&&music.clip)Destroy(music.clip);if(machineAudio&&machineAudio.clip)Destroy(machineAudio.clip);}
        static AudioClip Tone(float frequency,float duration)
        {
            int count=(int)(22050*duration);float[] samples=new float[count];
            for(int i=0;i<count;i++)samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/22050f)*.2f*(1-i/(float)count);
            var clip=AudioClip.Create("Cafe tone",count,1,22050,false);clip.SetData(samples,0);return clip;
        }
        static AudioClip Music()
        {
            // Original eight-bar café loop: warm chords, soft plucks, bass and light percussion.
            const int rate=22050; const float beat=60f/104f;
            int count=Mathf.RoundToInt(rate*beat*32); var samples=new float[count];
            float[,] chords={{261.63f,329.63f,392f,493.88f},{220f,261.63f,329.63f,392f},
                {293.66f,349.23f,440f,523.25f},{196f,246.94f,349.23f,440f}};
            int[] melody={0,2,1,3,2,1,3,2,0,1,2,3,1,2,0,2};
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate, barTime=t%(beat*4), beatTime=t%beat;
                int bar=((int)(t/(beat*4)))%4, quarter=(int)(barTime/beat), eighth=(int)(t/(beat*.5f));
                float chord=0;
                for(int n=0;n<4;n++) chord+=Mathf.Sin(2*Mathf.PI*chords[bar,n]*t)*(.022f/(n+1));
                float note=chords[bar,melody[eighth%16]]*2f;
                float pluckAge=t%(beat*.5f);
                float pluck=(Mathf.Sin(2*Mathf.PI*note*pluckAge)+.28f*Mathf.Sin(2*Mathf.PI*note*2f*pluckAge))
                    *Mathf.Exp(-pluckAge*10f)*.12f;
                float bass=Mathf.Sin(2*Mathf.PI*chords[bar,0]*.5f*beatTime)*Mathf.Exp(-beatTime*5f)*.095f;
                float kick=quarter%2==0?Mathf.Sin(2*Mathf.PI*(75f-30f*beatTime)*beatTime)*Mathf.Exp(-beatTime*38f)*.11f:0;
                float noise=Mathf.Sin(i*12.9898f)*Mathf.Sin(i*78.233f);
                float hat=noise*Mathf.Exp(-(t%(beat*.5f))*55f)*.015f;
                float clap=quarter%2==1?noise*Mathf.Exp(-beatTime*35f)*.025f:0;
                float edge=Mathf.Min(1f,Mathf.Min(t/.08f,(count-i)/(rate*.08f)));
                samples[i]=Mathf.Clamp((chord+pluck+bass+kick+hat+clap)*edge,-.8f,.8f);
            }
            var clip=AudioClip.Create("Cafe Tycoon - original loop",count,1,rate,false);
            clip.SetData(samples,0); return clip;
        }
    }
}








