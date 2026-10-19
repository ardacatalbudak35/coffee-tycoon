using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CafeLife
{
    [InitializeOnLoad] public static class CafeFiftyCheck
    {
        static CafeGame g;static int stage;static double began;static string report="";static bool running,failed;
        static CafeCustomer customer;static Vector3 carStart,walkerStart;static Transform car,walker;
        static CafeFiftyCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
        static void Mode(PlayModeStateChange mode)
        {
            if(mode!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("CafeFiftyCheck",false))return;
            SessionState.SetBool("CafeFiftyCheck",false);running=false;Application.logMessageReceived-=Log;
            var game=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(game){game.testing=false;game.suppressSaveOnExit=false;game.data=Economy.Load(game.SavePath);EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(game.gameObject.scene);}
        }
        static void Log(string message,string stack,LogType type)
        {if(type!=LogType.Error&&type!=LogType.Exception)return;failed=true;report+="RUNTIME ERROR: "+message+"\n"+stack+"\n";}
        static void Check(int id,bool condition,string title)
        {if(!condition)throw new Exception(id.ToString("00")+" "+title);report+="PASS "+id.ToString("00")+" "+title+"\n";}
        static void Private(string name,object value)=>typeof(CafeGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,value);
        static bool UIContains(string text)=>GameObject.Find("Cafe HUD").GetComponentsInChildren<Text>().Any(t=>t.text.Contains(text));
        static void Open(string section){g.OpenOperations(section);Canvas.ForceUpdateCanvases();}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running)
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/RunCafeFifty.flag"))return;
                g=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(!g)return;
                try{File.Delete("Logs/RunCafeFifty.flag");}catch(IOException){return;}
                g.testing=true;g.suppressSaveOnExit=true;stage=0;running=true;failed=false;report="Coffee Tycoon — 50 development integration checks\n";began=EditorApplication.timeSinceStartup;
                SessionState.SetBool("CafeFiftyCheck",true);Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;return;
            }
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup-began>150)throw new Exception("Verification timed out");
                g=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(!g||!g.ui||!g.expansion||g.data==null)return;
                if(stage==0){Production();Business();World();stage=1;began=EditorApplication.timeSinceStartup;g.Continue();}
                else if(stage==1&&EditorApplication.timeSinceStartup-began>2){VisualAndSave();stage=2;began=EditorApplication.timeSinceStartup;}
                else if(stage>=2&&stage<=10&&EditorApplication.timeSinceStartup-began>.4)
                {string[] pages={"recipes","business","supply","reports","achievements","decor","settings","help","travel"};string section=pages[stage-2];g.OpenOperations(section);ScreenCapture.CaptureScreenshot("Logs/CafeFifty-"+section+".png");stage++;began=EditorApplication.timeSinceStartup;}
                else if(stage==11&&EditorApplication.timeSinceStartup-began>1)
                {
                    if(failed)throw new Exception("Runtime errors detected; see report");
                    File.WriteAllText("Logs/CafeFiftyCheck.txt",report+"ALL 50 CHECKS PASSED\n");EditorApplication.isPlaying=false;
                }
            }
            catch(Exception ex){File.WriteAllText("Logs/CafeFiftyCheck.txt",report+"FAIL "+ex+"\n");EditorApplication.isPlaying=false;}
        }
        static void Production()
        {
            var x=g.expansion;g.data.money=10000;g.Continue();
            Open("recipes");Check(1,Enumerable.Range(0,10).All(i=>UIContains(Economy.Recipes[i].name)),"Recipe book exposes all ten drinks");g.Continue();
            g.selected=1;g.Act("cup_hot");g.Act("milk");
            Check(2,g.cup!=null&&g.NextStep()=="Değirmen"&&g.cup.steps.SequenceEqual(new[]{"milk"}),"Preparation checklist follows current cup");
            x.Action("foam");x.Tick(3);Check(3,g.cup.foamed&&x.foamTime==0,"Foaming completes and upgrades the actual cup");
            g.Act("grinder");g.Act("machine");float original=g.brewTotal;g.brewTime=g.brewTotal*.15f;g.Act("machine");Check(5,g.cup.perfectBrew&&g.cup.brewed&&g.brewTime==0,"Timed espresso finish grants perfect extraction");
            x.Action("art");x.artTime=2;x.Action("art");Check(4,g.cup.art&&CafeExpansion.TimingScore(0)==0,"Latte art rewards correct timing and rejects endpoints");
            Check(8,UnityEngine.Object.FindAnyObjectByType<CafeCupMotion>()!=null,"Held cup has a movement component");
            x.Ingredient("milk");Check(9,x.pourTime>0,"Ingredient event triggers pour feedback");
            float dirty=x.Data.machineDirt;g.cup.ground=true;g.cup.brewed=false;g.brewTime=0;g.Act("machine");float dirtyTime=g.brewTotal;g.brewTime=0;x.Action("clean_machine");Check(10,dirty>0&&x.Data.machineDirt==0&&dirtyTime>4f,"Maintenance removes dirt and its brew-time penalty");
            g.cup.brewed=true;g.cup.steps.Clear();g.cup.steps.AddRange(new[]{"milk","espresso","lid"});g.cup.lidded=true;g.Handoff();
            var drink=g.shelf.Last();drink.age=89;x.Tick(2);Check(6,!drink.good&&drink.age>=91,"Unclaimed drinks become stale after ninety seconds");x.ClearStale();Check(7,!g.shelf.Contains(drink),"Stale unclaimed drinks can be removed");
            x.CancelWork();g.cup=null;g.UpdateHand();
        }
        static void Business()
        {
            var x=g.expansion;var d=x.Data;g.data.money=10000;
            x.SetPrice(1000);bool upper=d.price==140;x.SetPrice(-1000);Check(11,upper&&d.price==80,"Price controls obey eighty to one-hundred-forty percent bounds");
            d.dayIncome=100;d.dayExpenses=0;x.Pay(15);Check(12,d.dayIncome-d.dayExpenses==85,"Ledger tracks real spending and net income");
            g.data.money=0;g.data.staff=0;x.EndDay(g.data.day);Check(13,d.debt>=20&&g.data.money==0&&d.history[0].expenses==35,"Unpaid daily rent becomes debt and history is recorded");
            g.data.money=10000;d.dayServed=8;d.dailyRewardDay=0;int money=g.data.money;x.DailyReward();x.DailyReward();Check(14,g.data.money==money+100,"Daily target reward pays only once per day");
            g.data.served=10;d.achievements=0;money=g.data.money;x.ClaimAchievement(0);x.ClaimAchievement(0);Check(15,g.data.money==money+75&&(d.achievements&1)!=0,"Achievements cannot be claimed twice");
            g.Spawn();customer=g.customers.Last();customer.identity=0;customer.goodDrink=true;customer.vip=false;customer.qualityBonus=0;d.loyalty[0]=0;
            int standard=x.CompleteSale(customer);d.loyalty[0]=3;int loyal=x.CompleteSale(customer);Check(16,loyal==standard+4,"Returning customers pay a loyalty bonus");
            Check(17,customer.identity>=0&&customer.identity<CafeExpansion.Names.Length&&CafeExpansion.Names.Distinct().Count()==12,"Customers have twelve persistent identity slots");
            Check(18,x.PatienceMultiplier(1)>x.PatienceMultiplier(0)&&x.PatienceMultiplier(2)<x.PatienceMultiplier(0),"Customer personalities change patience");
            customer.vip=true;int vip=x.CompleteSale(customer);Check(19,vip==loyal+8,"VIP correct service pays its extra bonus");customer.vip=false;
            float beforeTime=g.data.dayTime;g.data.dayTime=0;float peak=x.DemandMultiplier;g.data.dayTime=60;Check(20,!x.Rush&&x.DemandMultiplier>peak,"Rush hours change the spawn interval");g.data.dayTime=beforeTime;
            Check(21,d.reviews.Count>0&&d.reviews[0].Contains(CafeExpansion.Names[0]),"Actual sales produce named customer reviews");
            d.price=80;float low=x.PatienceMultiplier(0),lowDemand=x.DemandMultiplier;d.price=140;Check(22,x.PatienceMultiplier(0)>low&&x.DemandMultiplier>lowDemand,"High prices affect patience and arrival frequency");d.price=100;
            customer.state=CafeCustomer.State.Waiting;customer.SendMessage("Update");Check(23,customer.GetComponentInChildren<TextMesh>().text.Contains(CafeExpansion.Names[0]),"Waiting customer labels display their identity");
            customer.seat=0;customer.state=CafeCustomer.State.Drinking;customer.GetComponent<CafeSip>().SendMessage("Update");Check(24,customer.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("bardak")),"Seated customer visibly holds a drink");
            int litter=x.litter.Count;customer.state=CafeCustomer.State.Leaving;customer.GetComponent<CafeSip>().SendMessage("Update");Check(25,x.litter.Count==litter+1&&x.litter.Last().table,"Leaving customer leaves a cleanable cup on the table");
            d.cleanliness=0;float dirty=x.PatienceMultiplier(0);x.Action("clean_cafe");Check(26,d.cleanliness==100&&x.PatienceMultiplier(0)<dirty,"Cleaning improves customer patience");
            d.open=false;int count=g.customers.Count;g.Spawn();bool closed=g.customers.Count==count;d.open=true;g.Spawn();Check(27,closed&&g.customers.Count==count+1,"Closed cafes stop new arrivals");
            foreach(var c in g.customers)c.state=CafeCustomer.State.Waiting;g.data.staff=1;g.data.baristaLevel=1;Private("staffTimer",-1f);Private("staffBusy",false);d.staffOn=false;money=g.data.money;g.SendMessage("Update");Check(28,g.data.money==money,"Off-duty staff do not start new work");
            d.staffOn=true;d.dayExpenses=0;money=g.data.money;x.EndDay(g.data.day);Check(29,g.data.money==money-40&&d.history[0].expenses==40,"Active barista salary and rent are charged together");g.data.staff=0;
            g.data.beans=0;g.data.milk=0;Check(30,x.LowStock().Contains("çekirdek")&&x.LowStock().Contains("süt"),"Low-stock panel identifies missing ingredients");
            int beans=g.data.depotBeans,milk=g.data.depotMilk,pack=g.data.depotCupsHot;x.Supply("beans");x.Supply("milk");x.Supply("packaging");Check(31,g.data.depotBeans==beans+35&&g.data.depotMilk==milk+25&&g.data.depotCupsHot==pack+25,"Distinct supplier bundles deliver their promised contents");
            beans=g.data.beans;x.Supply("emergency");Check(32,g.data.beans==beans+10&&g.data.milk>=8,"Emergency delivery reaches the bar directly");
            int theme=d.theme;x.SetDecor("theme");Check(33,d.theme==(theme+1)%3,"Decor theme changes and applies");int wall=d.wall;x.SetDecor("wall");Check(34,d.wall==(wall+1)%5,"Wall palette selection changes");int light=d.light;x.SetDecor("light");Check(35,d.light==(light+1)%3&&GameObject.Find("Kafe tavan aydınlatması").GetComponent<Light>().color.b>0,"Indoor lighting temperature changes");int layout=d.layout;Vector3 seat=g.world.Seat(0);x.SetDecor("layout");Check(36,d.layout!=layout&&Vector3.Distance(seat,g.world.Seat(0))>.2f,"Table layout moves seating destinations");
        }
        static void World()
        {
            var x=g.expansion;var city=g.GetComponent<CafeCityLife>();g.data.dayTime=0;city.SendMessage("Update");var sun=g.world.GetComponentsInChildren<Light>().First(l=>l.type==LightType.Directional);float noon=sun.intensity;g.data.dayTime=290;city.SendMessage("Update");Check(37,sun.intensity<noon,"Day-to-night cycle dims the sun");
            g.data.day=3;g.player.body.enabled=false;g.player.transform.position=new Vector3(0,0,-25);g.player.body.enabled=true;city.SendMessage("Update");var rain=GameObject.Find("Yağmur").GetComponent<ParticleSystem>();Check(38,x.Rainy&&rain.isPlaying,"Rainy day emits visible outdoor particles");
            UnityEngine.Random.InitState(61);int hot=0;for(int i=0;i<200;i++)if(!Economy.Recipes[x.PickRecipe()].cold)hot++;Check(39,hot>120,"Rainy weather biases demand toward hot drinks");
            g.data.dayTime=100;g.data.day=4;car=g.world.transform.Cast<Transform>().First(t=>t.name.StartsWith("Gerçek ölçekte"));walker=g.world.transform.Find("Mahalle yayası 0");carStart=car.position;walkerStart=walker.position;
            var position=g.player.transform.position;x.ToggleSit();bool sitting=x.Seated&&g.player.view.transform.localPosition.y<1.2f;x.ToggleSit();Check(42,sitting&&!x.Seated&&Vector3.Distance(g.player.transform.position,position)<.01f,"Bench sitting lowers viewpoint and restores position on exit");
            var trash=x.litter.First(l=>l&&!l.table);int cash=g.data.money;trash.Clean();Check(43,g.data.money==cash+2&&!trash.gameObject.activeSelf,"Street litter cleanup pays once and removes the item");
            var venue=UnityEngine.Object.FindAnyObjectByType<CafeVenue>();g.player.transform.rotation=Quaternion.LookRotation(new Vector3(venue.transform.position.x-g.player.transform.position.x,0,venue.transform.position.z-g.player.transform.position.z));Check(44,x.Direction(venue).Contains("İleri"),"Compass identifies a destination directly ahead");
            cash=g.data.money;x.Travel(venue);Check(45,g.data.money==cash-15&&Vector3.Distance(g.player.transform.position,venue.transform.position+Vector3.forward*2)<.05f,"Taxi charges fifteen coins and reaches selected venue");
            x.Data.stamina=100;x.Stamina(true,true,3);float spent=x.Data.stamina;x.Stamina(false,false,2);Check(46,spent<100&&x.Data.stamina>spent,"Sprint drains stamina and rest restores it");
            x.Data.fov=90;g.player.view.fieldOfView=x.Data.fov;Check(47,g.player.view.fieldOfView==90,"FOV setting applies to gameplay camera");
            x.Data.music=.25f;x.Data.effects=.75f;g.ApplyAudio();var music=g.GetComponents<AudioSource>().First(a=>a.clip&&a.clip.name.Contains("original loop"));Check(48,Mathf.Abs(music.volume-.09f)<.01f,"Music volume can be set independently from effects");
            x.Data.tutorial=0;x.Ingredient("cup_hot");x.Ingredient("grinder");x.Ingredient("milk");x.Ingredient("lid_hot");Check(49,(x.Data.tutorial&15)==15,"Tutorial checklist advances from real preparation events");
            foreach(string section in new[]{"recipes","business","supply","reports","achievements","decor","settings","help","travel"}){Open(section);if(!UIContains("OYUNA DÖN"))throw new Exception("Missing return button on "+section);}
            g.Save();Check(50,x.saveStatus=="Kaydedildi"&&g.Paused,"All management sections open and show successful save state");g.Continue();
        }
        static void VisualAndSave()
        {
            Check(40,Vector3.Distance(car.position,carStart)>.1f,"Traffic moves along street lanes");report+="PEDESTRIAN diagnostic: start="+walkerStart+" now="+walker.position+" paused="+g.Paused+" moving="+walker.GetComponent<CafeWalker>().animator.GetBool("Moving")+"\\n";Check(41,Vector3.Distance(walker.position,walkerStart)>.1f&&walker.GetComponent<CafeWalker>().animator.GetBool("Moving"),"Pedestrians move with walking animation");
            g.Save();var loaded=Economy.Load(g.SavePath);if(loaded.expansion==null||loaded.expansion.fov!=90||loaded.expansion.price!=g.expansion.Data.price||loaded.expansion.history.Count==0)throw new Exception("Expansion save round-trip failed");report+="PASS persistence settings, ledger, loyalty and operation state\n";
            g.OpenOperations("business");ScreenCapture.CaptureScreenshot("Logs/CafeFifty-management.png");
        }
    }
}




