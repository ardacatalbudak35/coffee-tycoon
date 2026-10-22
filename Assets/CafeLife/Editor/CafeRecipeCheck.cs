using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // Logs/RunCafeRecipeCheck.flag: her tarifi oyun mantığıyla (Act) baştan sona hazırlar, tanınan ürünü ve kaliteyi Logs/RecipeCheck.txt'ye yazar; menü ve filtre makinesi görüntüsü alır.
    [InitializeOnLoad]
    public static class CafeRecipeCheck
    {
        const string Flag="Logs/RunCafeRecipeCheck.flag";
        static double nextAt; static CafeGame g;
        static CafeRecipeCheck(){ EditorApplication.update+=Tick; }

        static void Tick()
        {
            if(!SessionState.GetBool("RecipeCheck",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("RecipeCheck",true); SessionState.SetInt("RecipeCheckIdx",0); File.WriteAllText("Logs/RecipeCheck.txt","started\n"); EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("RecipeCheckIdx",0)>0)SessionState.SetBool("RecipeCheck",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null)return;
            var steps=Steps(); int i=SessionState.GetInt("RecipeCheckIdx",0);
            if(i>=steps.Count){ File.AppendAllText("Logs/RecipeCheck.txt","done\n"); EditorApplication.isPlaying=false; return; }
            SessionState.SetInt("RecipeCheckIdx",i+1);
            try{ steps[i].Item2(); }catch(Exception ex){ File.AppendAllText("Logs/RecipeCheck.txt","FAIL step "+i+": "+ex+"\n"); EditorApplication.isPlaying=false; return; }
            nextAt=EditorApplication.timeSinceStartup+steps[i].Item1;
        }

        static void Log(string s){ File.AppendAllText("Logs/RecipeCheck.txt",s+"\n"); }
        static void Shot(string name){ ScreenCapture.CaptureScreenshot("Logs/Recipe-"+name+".png"); }
        static void Place(Vector3 pos,float yaw,float pitch)
        { var p=g.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=pitch; p.view.transform.localRotation=Quaternion.Euler(pitch,0,0); p.body.enabled=true; }

        // Bir tarifi Act çağrılarıyla hazırlar; son ürünü döndürür
        static string Make(int recipe)
        {
            g.data.beans=60; g.data.milk=60; g.data.ice=60; g.data.cupsHot=60; g.data.cupsCold=60; g.data.lidsHot=60; g.data.lidsCold=60; g.data.caramel=60; g.data.vanilla=60;
            g.cup=null; g.shelf.Clear(); g.selected=recipe; var r=Economy.Recipes[recipe]; int shelfBefore=g.shelf.Count;
            g.Act(r.cold?"cup_cold":"cup_hot"); if(g.cup==null)return "BARDAK ALINAMADI";
            foreach(var step in CafeGame.IdealSteps(r))
            {
                if(step=="espresso"){ g.Act("grinder"); g.Act("machine"); if(!g.expansion.StopBrew())return "DEMLEME BAŞLAMADI"; g.Act("machine"); }   // demle, sonra bardağı makineden al
                else if(step=="filter"){ g.Act("filter"); if(g.filterCup==null)return "FİLTRE MAKİNESİNE KONAMADI"; g.FilterDone(); g.Act("filter"); if(g.cup==null||!g.cup.filtered)return "FİLTRE BARDAĞI ALINAMADI"; }   // makineye koy, 20 sn'yi atla, bardağı geri al
                else if(step=="lid")g.Act(r.cold?"lid_cold":"lid_hot");
                else g.Act(step);
            }
            g.Act("pickup"); if(g.shelf.Count==0)return "TESLİM EDİLEMEDİ (bardak: "+string.Join(",",g.cup?.steps??new List<string>())+")";
            var d=g.shelf.Last(); return d.cup.productName+" · "+(d.good?"İYİ":"KÖTÜ")+" · adımlar: "+string.Join(">",d.cup.steps);
        }

        static List<Tuple<double,Action>> Steps()
        {
            var s=new List<Tuple<double,Action>>(); void Add(double w,Action a){ s.Add(Tuple.Create(w,a)); }
            Add(1,()=>{ g.testing=true; g.suppressSaveOnExit=true; g.story.allowInTest=false; g.NewGame(); });
            Add(2,()=>{
                for(int i=0;i<Economy.Recipes.Length;i++){ string got=Make(i); Log(i+" "+Economy.Recipes[i].name+" → "+got+(got.StartsWith(Economy.Recipes[i].name+" ")?"  [OK]":"  [FARKLI]")); }
                var tiers=new[]{1,2,3,4,5}; foreach(int day in tiers){ int m=CafeStory.MaskForDay(day); Log("gün "+day+" menü: "+string.Join(", ",Enumerable.Range(0,Economy.Recipes.Length).Where(i=>(m&(1<<i))!=0).Select(i=>Economy.Recipes[i].name))); }
            });
            // görüntüler: filtre makinesi (barın üstü) ve menü listesi
            Add(.4,()=>{ g.cup=null; g.filterCup=null; g.world.ClearFilterCup(); g.Act("cup_hot"); g.Act("filter"); Place(new Vector3(3.6f,.08f,3.4f),0,22); });                    // bardak makinede, demleniyor
            Add(.8,()=>{ Shot("1-filter-machine"); Log("filtre demlenirken etiket: "+g.FilterLabel()); });
            Add(.3,()=>{ g.FilterDone(); }); Add(.6,()=>{ Shot("1b-filter-ready"); Log("filtre hazır etiket: "+g.FilterLabel()); g.Act("filter"); Log("alındı mı: "+(g.cup!=null&&g.cup.filtered)); });
            Add(.2,()=>Place(new Vector3(-.2f,.08f,6.3f),0,16)); Add(.8,()=>Shot("2a-back-counter"));                    // arka tezgâh: çeşme + filtre makinesi, boş proplar gitmiş olmalı
            Add(.2,()=>Place(new Vector3(-.55f,.08f,6.75f),180,32)); Add(.8,()=>{ Shot("2b-milk-cabinet"); var tg=UnityEngine.Object.FindObjectsByType<CafeTarget>(FindObjectsInactive.Exclude); Log("hedefler: süt="+tg.Count(t=>t.kind=="milk")+" su="+tg.Count(t=>t.kind=="water")+" filtre="+tg.Count(t=>t.kind=="filter")); });
            Add(.2,()=>Place(new Vector3(-.9f,.08f,6.7f),0,26)); Add(.8,()=>Shot("2c-faucet"));
            Add(.2,()=>Place(new Vector3(3.4f,.08f,6.7f),250,14)); Add(.8,()=>Shot("2d-back-counter-side"));
            Add(.2,()=>Place(new Vector3(1.6f,.08f,3.2f),20,18)); Add(.8,()=>Shot("2-bar-overview"));
            Add(.2,()=>{ g.OpenOperations("recipes"); }); Add(.8,()=>Shot("3-recipe-menu"));
            return s;
        }
    }
}
