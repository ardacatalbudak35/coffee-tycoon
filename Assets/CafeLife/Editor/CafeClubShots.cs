using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // Logs/RunCafeClubShots.flag: oyunu izole test kaydıyla açar, kulüp ve casino için Logs/Club-*.png çeker.
    [InitializeOnLoad]
    public static class CafeClubShots
    {
        const string Flag="Logs/RunCafeClubShots.flag";
        static double nextAt; static CafeGame g;
        static CafeClubShots(){ EditorApplication.update+=Tick; }

        static void Tick()
        {
            if(!SessionState.GetBool("ClubShots",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("ClubShots",true); SessionState.SetInt("ClubShotsIdx",0); File.WriteAllText("Logs/ClubShots.txt","started\n"); EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("ClubShotsIdx",0)>0)SessionState.SetBool("ClubShots",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null||g.cleaning==null)return;
            var steps=Steps(); int i=SessionState.GetInt("ClubShotsIdx",0);
            if(i>=steps.Count){ File.AppendAllText("Logs/ClubShots.txt","done\n"); EditorApplication.isPlaying=false; return; }
            SessionState.SetInt("ClubShotsIdx",i+1);
            try{ steps[i].Item2(); }catch(Exception ex){ File.AppendAllText("Logs/ClubShots.txt","FAIL step "+i+": "+ex+"\n"); EditorApplication.isPlaying=false; return; }
            nextAt=EditorApplication.timeSinceStartup+steps[i].Item1;
        }

        static void Shot(string name){ ScreenCapture.CaptureScreenshot("Logs/Club-"+name+".png"); }
        static void Place(Vector3 pos,float yaw,float pitch)
        { var p=g.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=pitch; p.view.transform.localRotation=Quaternion.Euler(pitch,0,0); p.body.enabled=true; }

        static List<Tuple<double,Action>> Steps()
        {
            var s=new List<Tuple<double,Action>>(); void Add(double w,Action a){ s.Add(Tuple.Create(w,a)); }
            Add(1,()=>{ g.testing=true; g.suppressSaveOnExit=true; g.NewGame(); });
            Add(3,()=>{ Place(new Vector3(14,.08f,-24.5f),180,3); });
            Add(1.2,()=>Shot("01-entrance-view"));
            Add(.2,()=>Place(new Vector3(14,.08f,-26.6f),180,10)); Add(.4,()=>Shot("02-dance-floor-a")); Add(.9,()=>Shot("03-dance-floor-b"));
            Add(.2,()=>Place(new Vector3(11.5f,.08f,-33.5f),225,4)); Add(.9,()=>Shot("04-bar"));
            Add(.2,()=>Place(new Vector3(12.5f,.08f,-29.5f),270,4)); Add(.9,()=>Shot("05-lounge"));
            Add(.2,()=>Place(new Vector3(18f,.08f,-28.5f),150,12)); Add(.9,()=>Shot("06-dj-booth"));
            Add(.2,()=>Place(new Vector3(13f,4.28f,-25f),180,2)); Add(.9,()=>Shot("07-casino"));
            Add(.2,()=>Place(new Vector3(10f,4.28f,-30f),270,4)); Add(.9,()=>Shot("08-casino-slots"));
            Add(.2,()=>Place(new Vector3(14f,.08f,-17.5f),180,8)); Add(.9,()=>Shot("09-exterior"));
            return s;
        }
    }
}
