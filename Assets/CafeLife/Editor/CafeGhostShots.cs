using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // Logs/RunCafeGhostShots.flag: paranormal olayları sırayla tetikler, Logs/Ghost-*.png çeker ve Logs/GhostShots.txt'ye sonuç yazar.
    [InitializeOnLoad]
    public static class CafeGhostShots
    {
        const string Flag="Logs/RunCafeGhostShots.flag";
        static double nextAt; static CafeGame g;
        static CafeGhostShots(){ EditorApplication.update+=Tick; }

        static void Tick()
        {
            if(!SessionState.GetBool("GhostShots",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("GhostShots",true); SessionState.SetInt("GhostShotsIdx",0); File.WriteAllText("Logs/GhostShots.txt","started\n"); EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("GhostShotsIdx",0)>0)SessionState.SetBool("GhostShots",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null||g.cleaning==null)return;
            var steps=Steps(); int i=SessionState.GetInt("GhostShotsIdx",0);
            if(i>=steps.Count){ File.AppendAllText("Logs/GhostShots.txt","done\n"); EditorApplication.isPlaying=false; return; }
            SessionState.SetInt("GhostShotsIdx",i+1);
            try{ steps[i].Item2(); }catch(Exception ex){ File.AppendAllText("Logs/GhostShots.txt","FAIL step "+i+": "+ex+"\n"); EditorApplication.isPlaying=false; return; }
            nextAt=EditorApplication.timeSinceStartup+steps[i].Item1;
        }

        static void Shot(string name){ ScreenCapture.CaptureScreenshot("Logs/Ghost-"+name+".png"); }
        static void Log(string s){ File.AppendAllText("Logs/GhostShots.txt",s+"\n"); }
        static void Place(Vector3 pos,float yaw,float pitch)
        { var p=g.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=pitch; p.view.transform.localRotation=Quaternion.Euler(pitch,0,0); p.body.enabled=true; }
        static CafeParanormal Ghost()=>g.GetComponent<CafeParanormal>();

        static List<Tuple<double,Action>> Steps()
        {
            var s=new List<Tuple<double,Action>>(); void Add(double w,Action a){ s.Add(Tuple.Create(w,a)); }
            Add(1,()=>{ g.testing=true; g.suppressSaveOnExit=true; g.story.allowInTest=false; g.NewGame(); });   // giriş diyaloğu çıkmasın
            Add(2.5,()=>{ g.data.storyStage=4; g.data.day=3; g.data.dayTime=200; g.data.shadowSeen=true; g.data.jumpSeen=true; g.story.allowInTest=true; /* konuşma kutuları testi durdurmasın */ Log("story active="+CafeStory.Active+" busy="+CafeStory.Busy+" paranormal="+(Ghost()!=null)); });
            // 1) gölge: gözün önünde belirir, ona bakınca ~1 sn içinde kaybolmalı
            Add(.3,()=>{ Place(new Vector3(0,.08f,-8f),0,0); Ghost().SpawnShadow(new Vector3(0,0,-1.5f)); });
            Add(.5,()=>Shot("01-shadow-in-view"));
            Add(.5,()=>Log("0.5s: "+Ghost().Info())); Add(.7,()=>Log("1.2s: "+Ghost().Info()));
            Add(1.0,()=>Log("gölge sonra: "+(GameObject.Find("Gölge")?"HÂLÂ VAR":"kayboldu")+" shadowSeen="+g.data.shadowSeen));
            // 2) arkada bir gölge (gözün dışında): yakınlaşmasını beklemeden sadece görüntü
            Add(.3,()=>{ Ghost().SpawnShadow(new Vector3(-7f,0,-3f)); Place(new Vector3(0,.08f,-8f),50,0); });
            Add(.5,()=>Shot("02-shadow-peripheral"));
            Add(.3,()=>{ Place(new Vector3(0,.08f,-8f),0,0); Ghost().Flicker(1.4f); });
            Add(.35,()=>Shot("03-flicker-a")); Add(.2,()=>Shot("04-flicker-b"));
            // 3) sandalye kıpırdar
            Add(1.8,()=>{ var before=new List<string>(); foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))if(t.name=="Döşemeli butik sandalye")before.Add(t.position.ToString("F2")); Place(new Vector3(0,.08f,-8f),180,0); for(int i=0;i<6;i++)Ghost().MoveChair(); int moved=0; var k=0; foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))if(t.name=="Döşemeli butik sandalye"){ if(k<before.Count&&before[k]!=t.position.ToString("F2"))moved++; k++; } Log("sandalye sayısı="+before.Count+" kıpırdayan="+moved); });
            // 4) üst kat: TV kendiliğinden açılır
            Add(.3,()=>{ Place(new Vector3(-.3f,3.78f,1.2f),180,5); Ghost().TvOn("20. FİNCANI\nKİM İÇTİ?"); });
            Add(.5,()=>Shot("05-tv-on")); Add(.9,()=>Shot("06-tv-on-b"));
            Add(.3,()=>{ Place(new Vector3(-2.5f,3.78f,4f),20,0); Ghost().SpawnShadow(new Vector3(-6f,3.7f,.5f)); Place(new Vector3(-3f,3.78f,1.0f),270,0); });
            Add(.6,()=>Shot("07-shadow-upstairs"));
            // 5) arkandan jumpscare: önce gölge sırtında (görünmez), oyuncu dönünce tam önünde belirir
            Add(.3,()=>{ Place(new Vector3(0,.08f,-8f),0,0); Log("behind spawn="+Ghost().BehindScare()); });
            Add(1.0,()=>{ Shot("08-behind-not-visible"); Log("behind info: "+Ghost().Info()); });
            Add(.2,()=>Place(new Vector3(0,.08f,-8f),180,0));
            Add(.12,()=>Shot("09-jumpscare-a")); Add(.35,()=>Shot("10-jumpscare-b")); Add(.3,()=>Log("jumpscare sonrası: "+Ghost().Info()));
            Add(1.2,()=>Log("gölge temizlendi mi: "+(GameObject.Find("Gölge")?"HÂLÂ VAR":"evet")));
            // 6) TV jumpscare
            Add(.3,()=>{ Place(new Vector3(-.3f,3.78f,1.2f),180,5); Ghost().TvOn("ARKANA\nBAKMA",true); });
            Add(3.3,()=>Shot("11-tv-scare-eyes")); Add(1.6,()=>Log("tv bitti"));
            return s;
        }
    }
}
