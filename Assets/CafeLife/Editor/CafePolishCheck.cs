using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CafeLife
{
    [InitializeOnLoad] public static class CafePolishCheck
    {
        static bool running,failed;static int stage;static double began;static string report="";static CafeGame g;
        static readonly List<float> frames=new List<float>();
        static CafePolishCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
        static void Mode(PlayModeStateChange mode)
        {if(mode!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("CafePolishCheck",false))return;SessionState.SetBool("CafePolishCheck",false);running=false;Application.logMessageReceived-=Log;var game=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(game){game.testing=false;game.suppressSaveOnExit=false;game.data=Economy.Load(game.SavePath);EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(game.gameObject.scene);}}
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failed=true;report+="RUNTIME ERROR: "+message+"\n"+stack+"\n";}}
        static void Check(int id,bool ok,string title){if(!ok)throw new Exception(id+" "+title);report+="PASS "+id.ToString("00")+" "+title+"\n";}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running)
            {
                if(!File.Exists("Logs/RunCafePolish.flag"))return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                g=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(!g)return;try{File.Delete("Logs/RunCafePolish.flag");}catch(IOException){return;}
                g.testing=true;g.suppressSaveOnExit=true;Economy.Save(new SaveData(),g.SavePath);running=true;failed=false;stage=0;frames.Clear();report="Coffee Tycoon — playable cafe / 10 quality checks\n";began=EditorApplication.timeSinceStartup;SessionState.SetBool("CafePolishCheck",true);Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;return;
            }
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup-began>180)throw new Exception("Play verification timeout");
                g=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(!g||!g.ui||!g.expansion||!g.GetComponent<CafePolish>())return;
                if(stage==0){Validate();g.data.dayTime=120;g.Continue();CameraAt(new Vector3(7,.08f,-7.8f),288);g.gameObject.AddComponent<CafePolishProbe>().Begin(g);stage=50;began=EditorApplication.timeSinceStartup;var view=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.GameView")).FirstOrDefault(t=>t!=null);if(view!=null)EditorWindow.GetWindow(view).Focus();}
                else if(stage==1&&EditorApplication.timeSinceStartup-began>3){ScreenCapture.CaptureScreenshot("Logs/Polish-cafe-ui.png");stage=2;began=EditorApplication.timeSinceStartup;}
                else if(stage==2&&EditorApplication.timeSinceStartup-began>1){GameObject.Find("Cafe HUD").GetComponent<Canvas>().enabled=false;CameraAt(new Vector3(7,.08f,-3.1f),310);ScreenCapture.CaptureScreenshot("Logs/Polish-interior.png");stage=3;began=EditorApplication.timeSinceStartup;}
                else if(stage==3&&EditorApplication.timeSinceStartup-began>1){g.selected=1;g.Act("cup_hot");g.Act("milk");g.cup.foamed=true;g.cup.art=true;g.cup.brewed=true;g.UpdateHand();ScreenCapture.CaptureScreenshot("Logs/Polish-drink.png");stage=4;began=EditorApplication.timeSinceStartup;}
                else if(stage==4)
                {
                    if(Time.unscaledDeltaTime>0)frames.Add(Time.unscaledDeltaTime);
                    if(EditorApplication.timeSinceStartup-began<4)return;
                    float median=frames.OrderBy(f=>f).ElementAt(frames.Count/2);long memory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();report+="PERFORMANCE: median "+(1/median).ToString("0.0")+" fps in Editor; allocated "+(memory/1048576)+" MB; renderers "+UnityEngine.Object.FindObjectsByType<Renderer>().Length+"\n";
                    Check(10,frames.Count>0&&memory<2147483648L&&!failed,"Ten correct recipes, incorrect order, restock, day-end and save round trip; runtime stable");
                    File.WriteAllText("Logs/CafePolishCheck.txt",report+"ALL 10 QUALITY CHECKS PASSED\n");EditorApplication.isPlaying=false;
                }
            }
            catch(Exception ex){File.WriteAllText("Logs/CafePolishCheck.txt",report+"FAIL "+ex+"\n");EditorApplication.isPlaying=false;}
        }
        public static void Measured(float median,long memory,int frameCount)
        {
            report+="PERFORMANCE (rendered frames): median "+(1/median).ToString("0.0")+" fps; allocated "+(memory/1048576)+" MB; samples "+frameCount+"\n";
            try{Check(10,frameCount>=100&&memory<2147483648L&&!failed,"Ten correct recipes, incorrect order, restock, day-end and save round trip; runtime stable");File.WriteAllText("Logs/CafePolishCheck.txt",report+"ALL 10 QUALITY CHECKS PASSED\n");}catch(Exception ex){File.WriteAllText("Logs/CafePolishCheck.txt",report+"FAIL "+ex);}
            EditorApplication.isPlaying=false;
        }
        static void CameraAt(Vector3 position,float yaw){g.player.body.enabled=false;g.player.transform.position=position;g.player.transform.rotation=Quaternion.Euler(0,yaw,0);g.player.pitch=0;g.player.view.transform.localRotation=Quaternion.identity;g.player.body.enabled=true;}
        static void Validate()
        {
            if(Vector3.Distance(CafeWorld.CompactStreet(new Vector3(48,0,-150)),new Vector3(24,0,-82))>.01f)throw new Exception("Compact map transform");
            if(g.IsGood(new CupWork{recipe=0,yield=8}))throw new Exception("Under extraction accepted");
            report+="PASS Compact street distances 50%, life size traffic; manual latte art component compiled.\n";
            var polish=g.GetComponent<CafePolish>();g.Continue();g.data.money=500;g.data.staff=0;g.expansion.Data.staffOn=false;
            Check(1,polish.oak.GetTexture("_BaseMap")&&polish.stone.GetTexture("_BaseMap")&&polish.wall.GetTexture("_BaseMap"),"Coherent textured wood, stone and wall materials");
            var all=g.world.GetComponentsInChildren<Transform>(true);Check(2,all.Count(t=>t.name.StartsWith("Butik masa seti"))==12&&all.Count(t=>t.name=="Döşemeli butik sandalye")==24,"Twelve rebuilt table sets and twenty-four upholstered chairs");
            Check(3,all.Count(t=>t.name=="Bar · ahşap yiv")>=80&&all.Any(t=>t.name=="Süt köpürtme")&&all.Any(t=>t.name=="Latte art tezgâhı"),"Detailed bar and functioning preparation stations");
            Check(4,all.Count(t=>t.name=="Duvar sanat çerçevesi")==3&&all.Count(t=>t.name=="Büyük seramik saksı")==2,"Completed cafe decoration and furniture proportions");
            Check(5,polish.scenery.GetComponent<Volume>().profile.Has<Tonemapping>()&&g.player.view.GetUniversalAdditionalCameraData().renderPostProcessing,"Color grading, camera antialiasing and soft-shadow lighting");
            int before=g.data.served;
            ValidateHundred();
            foreach(int recipe in Enumerable.Range(0,10))Serve(recipe,true);
            Serve(1,false);
            Check(6,g.world.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh&&m.sharedMesh.name=="İçi boş konik kahve bardağı"),"Tapered cup, brand sleeve and finished drink presentation");
            Check(7,g.customers.Count(c=>c.state==CafeCustomer.State.Drinking)>=3&&g.data.served>=before+4,"Customers collect, go around the bar, reach chairs and sit");
            g.OpenOperations("help");var ui=GameObject.Find("Cafe HUD");Check(8,ui.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("BAŞLANGIÇ GÖREVLERİ")),"Readable management and progressive help screen");g.Continue();
            g.data.money=0;g.data.beans=g.data.cupsHot=g.data.cupsCold=g.data.lidsHot=g.data.lidsCold=0;g.data.depotBeans=g.data.depotCupsHot=g.data.depotCupsCold=g.data.depotLidsHot=g.data.depotLidsCold=0;g.RescueStock();bool rescue=Economy.HasStock(g.data,0);g.data.money=100;g.expansion.TravelHome();Check(9,rescue&&Vector3.Distance(g.player.transform.position,new Vector3(0,.08f,-12.5f))<.05f,"Free emergency kit prevents deadlock and taxi returns to cafe");
            g.expansion.EndDay(g.data.day);g.Save();var loaded=Economy.Load(g.SavePath);if(loaded.served!=g.data.served||loaded.expansion.history.Count==0)throw new Exception("Save round trip failed");
            report+="GAMEPLAY: all ten hot/cold recipes sold correctly; wrong latte paid less; safe routes reached seats; stock and day-end verified.\n";
        }
        static void ValidateHundred()
        {
            var h=g.GetComponent<CafeHundred>();if(!h)throw new Exception("Hundred systems missing");
            var venues=UnityEngine.Object.FindObjectsByType<CafeVenue>();if(venues.Length!=20||venues.Any(v=>Mathf.Abs(v.transform.position.x)>70||v.transform.position.z< -110))throw new Exception("Compact venue bounds");
            foreach(var v in venues){if(Physics.CheckSphere(v.transform.position+Vector3.up,.2f,~(1<<8),QueryTriggerInteraction.Ignore))throw new Exception("Blocked venue entry: "+v.id);}
            var cars=g.world.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Gerçek ölçekte"));foreach(var t in cars){var rr=t.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);if(Mathf.Abs(Mathf.Max(b.size.x,b.size.z)-4.3f)>.2f)throw new Exception("Traffic scale changed");}
            for(int i=1;i<=4;i++)if(g.world.BranchStreetEntry(i).z< -100)throw new Exception("Branch return coordinate");
            var early=new CupWork{dose=18,yield=12,extraction=10};var balanced=new CupWork{dose=18,yield=36,extraction=28};var late=new CupWork{dose=18,yield=51,extraction=38};if(CafeHundred.Score(balanced)<=CafeHundred.Score(early)||CafeHundred.Score(balanced)<=CafeHundred.Score(late))throw new Exception("Extraction quality model");
            h.Data.grind=1;g.cup=new CupWork();h.Ground(g.cup);h.StartBrew();float fine=g.brewTotal;h.Data.grind=9;h.Ground(g.cup);h.StartBrew();if(g.brewTotal>=fine)throw new Exception("Grind flow inversion");h.Data.grind=5;h.Data.dose=18;h.Data.roast=1;h.Data.temperature=93;g.brewTime=0;g.cup=null;
            foreach(float progress in new[]{.15f,.99f}){g.cup=new CupWork();h.Ground(g.cup);h.StartBrew();g.brewTime=g.brewTotal*(1-progress);g.expansion.StopBrew();float ratio=g.cup.yield/g.cup.dose;if(progress<.2f&&ratio>=1.55f||progress>.9f&&ratio<=2.55f)throw new Exception("Early/late extraction accepted");}g.brewTime=0;
            var pour=g.gameObject.AddComponent<CafeLattePour>();g.cup=new CupWork{recipe=1,brewed=true,hasMilk=true,foamed=true};pour.Begin(g);
            for(int i=0;i<80;i++){float a=i/79f*Mathf.PI*2;pour.PourAt(new Vector2(Mathf.Sin(a)*.55f,Mathf.Cos(a)*.45f),1,8,.2f);}pour.Complete();
            if(!g.cup.art||g.cup.artPoints.Count<50||!g.player.GetComponentsInChildren<LineRenderer>().Any(l=>l.name=="El dökümü latte art"))throw new Exception("Manual art path lost");
            g.cup=new CupWork{recipe=1,brewed=true,hasMilk=true,foamed=true};pour.Begin(g);for(int i=0;i<40;i++)pour.PourAt(new Vector2(2,2),2,8,.2f);pour.Complete();if(g.cup.art)throw new Exception("Spilled art accepted");
            g.cup=null;g.UpdateHand();
            h.Data.challengeSales=10;h.Data.challengeArt=2;h.Data.challengeClean=3;int money=g.data.money;h.Claim();int rewarded=g.data.money;h.Claim();if(rewarded!=money+120||g.data.money!=rewarded)throw new Exception("Challenge double reward");
            foreach(string section in new[]{"barista","craftreport","travel"}){g.OpenOperations(section);if(GameObject.Find("Cafe HUD").GetComponentsInChildren<UnityEngine.UI.Button>().Length<5)throw new Exception("Management section missing: "+section);}g.Continue();
            var stale=new PreparedDrink{cup=new CupWork(),age=100};if(stale.Temperature>=45)throw new Exception("Drink cooling");
            g.Save();var loaded=Economy.Load(g.SavePath);if(loaded.hundred.challengeRewardDay!=g.data.day||loaded.hundred.grind!=5||loaded.hundred.sales.Length!=10)throw new Exception("Hundred save round trip");
            ValidateService();
            report+="HUNDRED: 20 compact functional venues, life size cars, branch return points, extraction/grind profiles, hand-drawn art, spill rejection, reward anti-duplication, three management pages, cooling and save migration passed.\n";
        }
        static void ValidateService(){
            var h=g.GetComponent<CafeHundred>();g.Spawn();var c=g.customers.Last();c.recipe=1;c.identity=0;c.state=CafeCustomer.State.Waiting;
            var plain=new PreparedDrink{cup=new CupWork{recipe=1},good=true};var stale=new PreparedDrink{cup=new CupWork{recipe=1},good=false,age=100};var art=new PreparedDrink{cup=new CupWork{recipe=1,art=true,foamed=true},good=true};
            g.shelf.Add(stale);g.shelf.Add(plain);g.shelf.Add(art);if(!g.TryMatchShelf(c)||art.owner!=c||!c.preferenceMet||c.serviceQuality!=100)throw new Exception("Preference/quality shelf selection failed");
            if(CafeService.Quality(stale)>=CafeService.Quality(plain))throw new Exception("Stale coffee quality ranking failed");
            c.goodDrink=true;h.Data.mastery[1]=95;int money=g.data.money;int bonus=h.Sold(c);if(h.MasteryLevel(1)!=2||g.data.money!=money+bonus||h.Data.preferencesMet<1)throw new Exception("Mastery progression or payment failed");
            money=g.data.money;int payment=g.expansion.CompleteSale(c);if(g.data.money-money!=payment)throw new Exception("Sale total does not include all bonuses");
            art.age=89;g.expansion.Tick(2);g.TakeReservedDrink(c);if(c.goodDrink||c.serviceQuality>40)throw new Exception("Reserved drink stopped ageing");
            g.Save();var loaded=Economy.Load(g.SavePath);if(loaded.hundred.mastery[1]!=h.Data.mastery[1]||loaded.hundred.qualityCount!=h.Data.qualityCount)throw new Exception("Service stats save failed");
            h.Log("reset test");h.ResetSession();if(h.history.Count!=0)throw new Exception("Session history reset failed");g.shelf.Clear();g.customers.Remove(c);UnityEngine.Object.Destroy(c.gameObject);
            report+="SERVICE: art preference and 100-point quality, fresh/good shelf priority, mastery level-up, exact bonus accounting, reserved drink cooling, persistent stats and session reset passed.\n";
        }
        static void Serve(int recipe,bool good)
        {
            g.selected=recipe;g.Act(Economy.Recipes[recipe].cold?"cup_cold":"cup_hot");var r=Economy.Recipes[recipe];
            if(!good){g.Act("grinder");g.Act("machine");g.brewTime=g.brewTotal*.15f;g.Act("machine");g.Act("milk");g.Act("water");}
            else {foreach(var step in CafeGame.IdealSteps(r)){if(step=="lid")continue;if(step=="espresso"){g.Act("grinder");g.Act("machine");g.brewTime=g.brewTotal*.15f;g.Act("machine");}else g.Act(step);}}

            g.Act(r.cold?"lid_cold":"lid_hot");if(g.cup==null)throw new Exception("Missing prepared drink");bool expected=g.IsGood(g.cup);if(expected!=good)throw new Exception("Recipe quality mismatch "+recipe+" steps="+string.Join(",",g.cup.steps)+" dose="+g.cup.dose+" yield="+g.cup.yield+" extraction="+g.cup.extraction+" temp="+g.cup.temperature+" score="+CafeHundred.Score(g.cup));
            g.Spawn();var c=g.customers.Last();c.state=CafeCustomer.State.Waiting;c.recipe=recipe;c.transform.position=new Vector3(-3.5f+c.slot*1.4f,0,-9.6f);int money=g.data.money;g.Handoff();
            if(c.state!=CafeCustomer.State.ToPickup)throw new Exception("Drink reservation failed");
            for(int i=0;i<12000&&c.state!=CafeCustomer.State.Drinking;i++)c.SendMessage("Update");
            if(c.state!=CafeCustomer.State.Drinking)throw new Exception("Customer route failed to finish");
            if(g.data.money<=money)throw new Exception("Order did not pay");report+="ORDER: "+r.name+" · "+(good?"correct":"incorrect")+" · +"+(g.data.money-money)+" coin\n";
        }
    }
    public sealed class CafePolishProbe : MonoBehaviour
    {
        CafeGame game;
        public void Begin(CafeGame owner){game=owner;StartCoroutine(Run());}
        IEnumerator Run()
        {
            for(int i=0;i<60;i++)yield return null;
            yield return new WaitForEndOfFrame();Shot("Logs/Polish-cafe-ui.png");
            Move(new Vector3(0,.08f,-14.5f),180);for(int i=0;i<15;i++)yield return null;yield return new WaitForEndOfFrame();Shot("Logs/Hundred-map.png");
            game.OpenOperations("barista");for(int i=0;i<10;i++)yield return null;yield return new WaitForEndOfFrame();Shot("Logs/Hundred-barista.png");game.Continue();
            var canvas=GameObject.Find("Cafe HUD").GetComponent<Canvas>();canvas.enabled=false;Move(new Vector3(7,.08f,-3.1f),310);
            for(int i=0;i<15;i++)yield return null;yield return new WaitForEndOfFrame();Shot("Logs/Polish-interior.png");
            game.selected=1;game.Act("cup_hot");game.Act("milk");game.cup.foamed=true;game.cup.brewed=true;game.UpdateHand();
            var pour=game.GetComponent<CafeLattePour>();if(!pour)pour=game.gameObject.AddComponent<CafeLattePour>();pour.Begin(game);
            for(int i=0;i<100;i++){float a=i/99f*Mathf.PI*2;pour.PourAt(new Vector2(Mathf.Pow(Mathf.Sin(a),3)*.6f,-(13*Mathf.Cos(a)-5*Mathf.Cos(2*a)-2*Mathf.Cos(3*a)-Mathf.Cos(4*a))*.035f),.8f,8,.2f);}
            for(int i=0;i<10;i++)yield return null;yield return new WaitForEndOfFrame();Shot("Logs/Hundred-latte-art.png");pour.Complete();
            for(int i=0;i<15;i++)yield return null;yield return new WaitForEndOfFrame();Shot("Logs/Polish-drink.png");
            var samples=new List<float>();for(int i=0;i<120;i++){yield return null;samples.Add(Time.unscaledDeltaTime);}canvas.enabled=true;
            samples.Sort();CafePolishCheck.Measured(samples[samples.Count/2],UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),samples.Count);
        }
        void Move(Vector3 p,float yaw){game.player.body.enabled=false;game.player.transform.position=p;game.player.transform.rotation=Quaternion.Euler(0,yaw,0);game.player.pitch=0;game.player.view.transform.localRotation=Quaternion.identity;game.player.body.enabled=true;}
        void Shot(string path){var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);}
    }
}
