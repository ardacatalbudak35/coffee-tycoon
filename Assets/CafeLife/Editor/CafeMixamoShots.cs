using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // Logs/RunCafeMixamoShots.flag: Resources/CafeMixamo'daki karakterleri yan yana dizip Logs/Mixamo-*.png çeker.
    [InitializeOnLoad]
    public static class CafeMixamoShots
    {
        const string Flag="Logs/RunCafeMixamoShots.flag";
        static readonly string[] Names={"Remy","James","Leonard","Adam","PeasantMan","Megan","Kate","Louise","Amy","Lola","Kaya","PeasantGirl","Olivia","Skeleton","Vampire","Ninja"};
        static double nextAt; static CafeGame g;
        static CafeMixamoShots(){ EditorApplication.update+=Tick; }
        static void Tick()
        {
            if(!SessionState.GetBool("MixShots",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("MixShots",true); SessionState.SetInt("MixShotsIdx",0); File.WriteAllText("Logs/MixamoShots.txt","started\n"); EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("MixShotsIdx",0)>0)SessionState.SetBool("MixShots",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null)return;
            int i=SessionState.GetInt("MixShotsIdx",0); SessionState.SetInt("MixShotsIdx",i+1);
            try
            {
                switch(i)
                {
                    case 0: g.testing=true; g.suppressSaveOnExit=true; g.NewGame(); nextAt=EditorApplication.timeSinceStartup+2; break;
                    case 1:
                        g.data.shadowSeen=true; g.data.jumpSeen=true; var log="";
                        var ctrl=Resources.Load<RuntimeAnimatorController>("CafeMixamo/CafeMixamo"); log+="controller="+(ctrl?ctrl.name:"YOK")+"\n";
                        for(int k=0;k<Names.Length;k++)
                        {
                            var prefab=Resources.Load<GameObject>("CafeMixamo/"+Names[k]); if(!prefab){log+=Names[k]+": YOK\n";continue;}
                            var o=UnityEngine.Object.Instantiate(prefab); o.name="MX "+Names[k]; int row=k/8,col=k%8; o.transform.position=new Vector3(-5.6f+col*1.6f,.03f,-3.5f+row*-1.4f); o.transform.rotation=Quaternion.Euler(0,180,0);
                            var a=o.GetComponentInChildren<Animator>(); if(a){a.applyRootMotion=false; if(ctrl)a.runtimeAnimatorController=ctrl; log+=Names[k]+": human="+a.isHuman+" ";}
                            var rs=o.GetComponentsInChildren<Renderer>(); if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);log+="h="+b.size.y.ToString("F2")+" mats="+rs[0].sharedMaterial.shader.name+"\n";}
                        }
                        File.AppendAllText("Logs/MixamoShots.txt",log);
                        var p=g.player; p.body.enabled=false; p.transform.position=new Vector3(0,.08f,-10.2f); p.transform.rotation=Quaternion.identity; p.pitch=0; p.view.transform.localRotation=Quaternion.identity; p.body.enabled=true;
                        nextAt=EditorApplication.timeSinceStartup+2; break;
                    case 2: ScreenCapture.CaptureScreenshot("Logs/Mixamo-01.png"); nextAt=EditorApplication.timeSinceStartup+1; break;
                    case 3: { var p2=g.player; p2.body.enabled=false; p2.transform.position=new Vector3(0,.08f,-14.5f); p2.body.enabled=true; } nextAt=EditorApplication.timeSinceStartup+1; break;
                    case 4: ScreenCapture.CaptureScreenshot("Logs/Mixamo-02.png"); nextAt=EditorApplication.timeSinceStartup+1; break;
                    default: File.AppendAllText("Logs/MixamoShots.txt","done\n"); EditorApplication.isPlaying=false; break;
                }
            }
            catch(Exception ex){ File.AppendAllText("Logs/MixamoShots.txt","FAIL "+i+": "+ex+"\n"); EditorApplication.isPlaying=false; }
        }
    }
}
