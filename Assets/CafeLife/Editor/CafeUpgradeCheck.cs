using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CafeLife
{
    [InitializeOnLoad]
    public static class CafeUpgradeCheck
    {
        static string report=""; static double began; static int stage; static CafeGame game;
        static CafeUpgradeCheck() { EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=Mode; }
        static void Mode(PlayModeStateChange mode)
        {
            if(mode!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("CafeUpgradeChecking",false))return;
            SessionState.SetBool("CafeUpgradeChecking",false);
            var g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(g){g.testing=false;g.suppressSaveOnExit=false; EditorUtility.SetDirty(g);}
        }
        static void Check(bool condition,string name) { if(!condition)throw new Exception(name); report+="PASS "+name+"\n"; }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!SessionState.GetBool("CafeUpgradeChecking",false))
            {
                if(!File.Exists("Logs/RunCafeUpgradeCheck.flag") || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Logs/CafeVisualInstall.txt") || !File.ReadAllText("Logs/CafeVisualInstall.txt").StartsWith("OK"))return;
                
                game=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!game)return;
                try { File.Delete("Logs/RunCafeUpgradeCheck.flag"); } catch(IOException) { return; }
                game.testing=true;game.suppressSaveOnExit=true;stage=0;began=EditorApplication.timeSinceStartup;
                report="Coffee Tycoon upgrade integration checks\n"; SessionState.SetBool("CafeUpgradeChecking",true); EditorApplication.isPlaying=true;return;
            }
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup-began>120)throw new Exception("Timed out");
                game=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!game || !game.ui || !game.world || game.data==null)return;
                if(stage==0)
                {
                    Check(game.testing,"Verification uses temporary save"); game.Continue(); game.Spawn();
                    var venues=UnityEngine.Object.FindObjectsByType<CafeVenue>(FindObjectsSortMode.None);
                    Check(venues.Length>=15,"All district buildings have services: "+venues.Length);
                    Check(venues.Select(v=>v.category).Distinct().Count()==8,"Eight building categories have services");
                    var cars=game.world.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Gerçek ölçekte")).ToArray();
                    Check(cars.Length==10,"Ten cars created");
                    foreach(var car in cars) { var rs=car.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);Check(Mathf.Abs(Mathf.Max(b.size.x,b.size.z)-4.3f)<.05f,"Vehicle length is 4.3 metres"); }
                    Check(game.customerPrefabs.Length==3 && game.customers.Count>0,"Three new character variants bound and spawn");
                    var a=game.customers[0].GetComponentInChildren<Animator>();Check(a && a.isHuman && a.runtimeAnimatorController==game.customerAnimator,"New humanoid character uses animation controller");
                    Check(a.runtimeAnimatorController.animationClips.Any(c=>c.name.Contains("Sitting_Idle")),"Real seated animation bound");
                    var market=venues.First(v=>v.category=="market");game.data.money=500;int stock=game.data.depotBeans;
                    market.Execute();Check(game.data.money==455 && game.data.depotBeans==stock+30,"Market charges and supplies depot");market.Execute();Check(game.data.money==455,"Same-day purchase cannot repeat");
                    game.data.day++;game.data.money=0;market.Execute();Check(game.data.money==0 && game.data.depotBeans==stock+30,"Insufficient funds leaves stock unchanged");
                    var contract=venues.First(v=>v.Contract);contract.Execute();Check(game.data.districtContract==contract.id,"Contract accepted");contract.Execute();Check(game.data.money==0,"Incomplete contract cannot pay");
                    game.data.served+=contract.Goal;contract.Execute();Check(game.data.money==contract.Reward && game.data.districtContract==-1,"Completed contract pays once");
                    var academy=venues.First(v=>v.category=="yellow_house");int xp=game.data.xp;game.data.money=100;academy.Execute();Check(game.data.xp==xp+100 && game.data.money==40,"Training consumes money and grants XP");
                    game.Save();var loaded=Economy.Load(game.SavePath);Check(loaded.venueVisitDay[academy.id]==game.data.day,"District state survives save round trip");
                    game.OpenVenue(market);Check(game.Paused && GameObject.Find("Cafe HUD"),"Venue opens modal UI");game.Continue();
                    stage=1;began=EditorApplication.timeSinceStartup;
                }
                else if(EditorApplication.timeSinceStartup-began>4)
                {
                    var a=game.customers[0].GetComponentInChildren<Animator>();a.SetBool("Moving",false);a.SetBool("Sitting",true);a.cullingMode=AnimatorCullingMode.AlwaysAnimate; for(int i=0;i<5;i++)a.Update(.2f);Check(a.GetCurrentAnimatorStateInfo(0).IsName("Sitting"),"Humanoid enters seated state");
                    File.WriteAllText("Logs/CafeUpgradeCheck.txt",report+"ALL CHECKS PASSED\n"); EditorApplication.isPlaying=false;
                }
            }
            catch(Exception ex){File.WriteAllText("Logs/CafeUpgradeCheck.txt",report+"FAIL "+ex); EditorApplication.isPlaying=false;}
        }
    }
}


