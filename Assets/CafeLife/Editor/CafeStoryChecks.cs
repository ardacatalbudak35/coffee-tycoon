using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CafeLife
{
    // Logs/RunCafeStoryChecks.flag: hikâye sistemlerini (günlük, Nermin ipuçları, bölüm kartı, ışık dili, hırsız) dener; Logs/StoryChecks.txt + Logs/SC-*.png
    [InitializeOnLoad]
    public static class CafeStoryChecks
    {
        const string Flag="Logs/RunCafeStoryChecks.flag";
        static double nextAt; static CafeGame g;
        static CafeStoryChecks(){ EditorApplication.update+=Tick; }
        static void Tick()
        {
            if(!SessionState.GetBool("SC",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("SC",true); SessionState.SetInt("SCIdx",0); File.WriteAllText("Logs/StoryChecks.txt","started\n"); EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("SCIdx",0)>0)SessionState.SetBool("SC",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null||g.ghosts==null)return;
            var steps=Steps(); int i=SessionState.GetInt("SCIdx",0);
            if(i>=steps.Count){ Log("done"); EditorApplication.isPlaying=false; return; }
            SessionState.SetInt("SCIdx",i+1);
            try{ steps[i].Item2(); }catch(Exception ex){ Log("FAIL step "+i+": "+ex); EditorApplication.isPlaying=false; return; }
            nextAt=EditorApplication.timeSinceStartup+steps[i].Item1;
        }
        static void Log(string s){ File.AppendAllText("Logs/StoryChecks.txt",s+"\n"); }
        static void Shot(string n){ ScreenCapture.CaptureScreenshot("Logs/SC-"+n+".png"); }
        static void Place(Vector3 pos,float yaw){ var p=g.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=0; p.view.transform.localRotation=Quaternion.identity; p.body.enabled=true; }
        static void Down(Key key){ var kb=Keyboard.current; if(kb==null){Log("klavye yok");return;} InputSystem.QueueStateEvent(kb,new KeyboardState(key)); InputSystem.Update(); Log("tuş "+key+" basılı="+kb[key].isPressed+" odak="+Application.isFocused); var f=typeof(CafeStory).GetField("pressed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); if(f!=null&&(key==Key.E))f.SetValue(g.story,true); }
        static void Up(){ var kb=Keyboard.current; if(kb==null)return; InputSystem.QueueStateEvent(kb,new KeyboardState()); InputSystem.Update(); }
        static void Field(object target,string name,object value){ target.GetType().GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(target,value); }
        static object Call(object target,string method,params object[] args){ var m=target.GetType().GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance); return m.Invoke(target,args); }
        static List<Tuple<double,Action>> Steps()
        {
            var s=new List<Tuple<double,Action>>(); void Add(double w,Action a){ s.Add(Tuple.Create(w,a)); }
            Add(1,()=>{ g.testing=true; g.suppressSaveOnExit=true; g.story.allowInTest=false; g.NewGame(); });
            Add(2.5,()=>{ g.story.allowInTest=true; var d=g.data; d.storyStage=4; d.day=7; d.dayTime=200; d.shadowSeen=true; d.jumpSeen=true; d.finale=true; d.bookRead=true; d.tables=4; int k=0; foreach(var r in CafeResidents.All){ var st=CafeResidents.State(g,r); st.met=k++<7; st.cups=3+r.kahve_bagimliligi; st.bond=k%5; } g.expansion.Data.open=true; Log("setup day="+d.day+" active="+CafeStory.Active); Place(new Vector3(0,.08f,-8f),0); });
            // günlük
            Add(.5,()=>Log("0.5s sonra Active="+CafeStory.Active+" Busy="+CafeStory.Busy+" Paused="+g.Paused+" started="+g.started));
            Add(.3,()=>{ g.story.AddJournal("not","Hasan Dede'nin notu","Bu odayı sen gelmeden hazırladım. Dükkânı sakın satma."); g.story.AddJournal("anı_ayse1","Ayşe Yılmaz · anı","Hasan Dede bana her sabah gazete ayırırdı."); g.story.AddJournal("haber1_0","Haber · SON DAKİKA: Kahve tüketimi rekor kırdı","Şehirde kişi başı günlük kahve tüketimi 19 fincana ulaştı."); Log("journal giriş sayısı="+g.data.journal.Count); Call(g.story,"OpenJournal"); });
            Add(.8,()=>{ Log("günlük açık busy="+CafeStory.Busy); Shot("journal-notes"); Field(g.story,"jTab",1); Call(g.story,"RenderJournal"); });
            Add(.5,()=>{ Shot("journal-residents"); Field(g.story,"jTab",2); Call(g.story,"RenderJournal"); });
            Add(.5,()=>{ Shot("journal-news"); g.story.StopAllCoroutines(); var jg=(GameObject)typeof(CafeStory).GetField("journalGo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(g.story); if(jg)jg.SetActive(false); CafeStory.Busy=false; g.StoryPause(false); });
            Add(.6,()=>Log("günlük zorla kapatıldı busy="+CafeStory.Busy));
            // Nermin'in odası
            Add(.3,()=>{ Place(new Vector3(.4f,3.78f,1.0f),0); g.story.ReadNermin(1); });
            Add(1.2,()=>{ Shot("nermin-photo"); Log("nermin diyalog busy="+CafeStory.Busy); Down(Key.E); });
            Add(.2,()=>Up());
            Add(.8,()=>{ Down(Key.E); });
            Add(.2,()=>Up());
            Add(.8,()=>{ Down(Key.E); });
            Add(.2,()=>Up());
            Add(.8,()=>{ Down(Key.E); });
            Add(.2,()=>Up());
            Add(.8,()=>{ Log("nermin diyalog sonrası busy="+CafeStory.Busy+" nermin bayrağı="+g.data.nermin); g.story.StopAllCoroutines(); CafeStory.Busy=false; g.StoryPause(false); g.story.StartCoroutine((IEnumerator)Call(g.story,"ChapterCard","BÖLÜM 2","Borç Defteri","")); });
            Add(1.3,()=>Shot("chapter-card"));
            Add(3.5,()=>Log("bölüm kartı bitti"));
            // ışık dili: Sayan oyuncunun yakınında
            Add(.3,()=>{ g.ghosts.ClearAll(); g.ghosts.Spawn(3,CafeGhosts.Kind.Sayan); g.ghosts.DebugSetCount(14); var gp=g.ghosts.GhostPos; Place(gp+new Vector3(gp.x>0?-4.5f:4.5f,.08f,gp.z>0?-1f:1f),gp.x>0?270f:90f); });
            Add(.5,()=>Log("ışık dili tint başlangıç="+g.ghosts.Tint));
            Add(3.5,()=>{ Log("ışık dili tint 3.5s sonra="+g.ghosts.Tint+" · yönetilen ışık temel="+g.ghosts.SampleBase+" şimdi="+g.ghosts.SampleLight+" müzik çarpanı="+g.musicMul); Shot("light-blue"); g.ghosts.ClearAll(); });
            // hırsız
            Add(.3,()=>{ g.data.day=3; g.data.dayTime=100; g.expansion.Data.open=true; Place(new Vector3(-6f,.08f,-3f),0); var f=typeof(CafeThief).GetField("cooldown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); f.SetValue(g.thief,0f); Log("hırsız cooldown sıfırlandı para="+g.data.money); });
            Add(3,()=>Log("hırsız aktif="+g.thief.Active+" nesne="+(GameObject.Find("Hırsız")?"VAR":"YOK")));
            Add(14,()=>{ Log("hırsız 17s sonra aktif="+g.thief.Active+" para="+g.data.money); Shot("thief"); });
            Add(.3,()=>{ int before=g.data.money; g.thief.Use(); Log("hırsız yakalandı: para "+before+"→"+g.data.money+" aktif="+g.thief.Active); });
            return s;
        }
    }
}
