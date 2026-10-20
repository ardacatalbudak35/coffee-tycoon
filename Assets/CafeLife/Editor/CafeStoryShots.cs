using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // Logs/RunCafeStoryShots.flag oluşturunca oyunu izole test kaydıyla açar, hikaye/temizlik/daire için Logs/Story-*.png çeker.
    [InitializeOnLoad]
    public static class CafeStoryShots
    {
        const string Flag="Logs/RunCafeStoryShots.flag";
        static double nextAt; static CafeGame g;
        static CafeStoryShots(){ EditorApplication.update+=Tick; }

        static void Tick()
        {
            if(!SessionState.GetBool("StoryShots",false))
            {
                if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                try{File.Delete(Flag);}catch(IOException){return;}
                SessionState.SetBool("StoryShots",true); SessionState.SetInt("StoryShotsIdx",0); File.WriteAllText("Logs/StoryShots.txt","started\n");
                EditorApplication.isPlaying=true; return;
            }
            if(!EditorApplication.isPlaying){ if(SessionState.GetInt("StoryShotsIdx",0)>0)SessionState.SetBool("StoryShots",false); return; }
            if(EditorApplication.timeSinceStartup<nextAt)return;
            g=UnityEngine.Object.FindAnyObjectByType<CafeGame>(); if(!g||g.home==null||g.story==null||g.cleaning==null)return;
            var steps=Steps(); int i=SessionState.GetInt("StoryShotsIdx",0);
            if(i>=steps.Count){ File.AppendAllText("Logs/StoryShots.txt","done\n"); EditorApplication.isPlaying=false; return; }
            SessionState.SetInt("StoryShotsIdx",i+1);
            try{ steps[i].Item2(); }catch(Exception ex){ File.AppendAllText("Logs/StoryShots.txt","FAIL step "+i+": "+ex+"\n"); EditorApplication.isPlaying=false; return; }
            nextAt=EditorApplication.timeSinceStartup+steps[i].Item1;
        }

        static void Shot(string name){ ScreenCapture.CaptureScreenshot("Logs/Story-"+name+".png"); }
        static void Place(Vector3 pos,float yaw,float pitch)
        { var p=g.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=pitch; p.view.transform.localRotation=Quaternion.Euler(pitch,0,0); p.body.enabled=true; }
        // Hedefe, kafe ortasının tarafından bakacak biçimde yerleş.
        static void Look(Vector3 target,float dist,float pitch)
        {
            var dir=new Vector3(0,0,-3)-new Vector3(target.x,0,target.z); dir.y=0; dir=dir.sqrMagnitude<.01f?Vector3.back:dir.normalized;
            var pos=new Vector3(target.x,.08f,target.z)+dir*dist; float yaw=Mathf.Atan2(target.x-pos.x,target.z-pos.z)*Mathf.Rad2Deg; Place(pos,yaw,pitch);
        }
        static void Press(){ typeof(CafeStory).GetField("pressed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.story,true); }

        static List<Tuple<double,Action>> Steps()
        {
            var s=new List<Tuple<double,Action>>();
            void Add(double wait,Action a){ s.Add(Tuple.Create(wait,a)); }
            var eye=new Vector3(0,.08f,-9.7f);
            Add(1,()=>{ g.testing=true; g.suppressSaveOnExit=true; g.story.allowInTest=true; g.NewGame(); });
            Add(0,()=>{ var log="player="+g.player.transform.position+" worldPos="+g.world.transform.position+" scale="+g.world.transform.lossyScale+"\n";
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)){ string n=r.name; if(n=="Kahve barı - ahşap gövde"||n=="Taş çalışma tezgâhı"||n=="Ahşap ön panel"||n=="Giriş paspası"||n.StartsWith("Zemin")||n.Contains("kapı")&&n.Contains("Giriş"))log+=n+" "+r.bounds+"\n"; }
                File.AppendAllText("Logs/StoryShots.txt",log); });
            Add(0,()=>{ var hole=new Bounds(new Vector3(-9.25f,2.2f,4.3f),new Vector3(1.6f,3.2f,4.2f)); var log="HOLE hits:\n";
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)){ if(r.bounds.Intersects(hole)&&r.bounds.max.y>1.5f)log+="  "+r.name+" c="+r.bounds.center+" s="+r.bounds.size+"\n"; }
                var tav=g.world.transform.Find("Tavan"); log+="Tavan="+(tav?tav.GetComponent<Renderer>().bounds.ToString():"null")+"\n"; File.AppendAllText("Logs/StoryShots.txt",log); });
            Add(0,()=>{ var note=GameObject.Find("Hasan Dede'nin notu"); var nr=note?note.GetComponent<Renderer>():null; File.AppendAllText("Logs/StoryShots.txt","NOTE "+(note?note.transform.position.ToString("F2")+" scale="+note.transform.localScale.ToString("F2")+" active="+note.activeInHierarchy+" rendererEnabled="+(nr&&nr.enabled)+" mat="+(nr?nr.sharedMaterial.name+" "+nr.sharedMaterial.color:"-"):"YOK")+"\n");
                var bed=GameObject.Find("VNB 1P Bed"); string log="BED "+(bed?"":"yok")+"\n"; if(bed)foreach(var r in bed.GetComponentsInChildren<Renderer>())log+="  "+r.name+" minY="+r.bounds.min.y.ToString("F2")+" maxY="+r.bounds.max.y.ToString("F2")+" size="+r.bounds.size.ToString("F2")+"\n"; File.AppendAllText("Logs/StoryShots.txt",log); });
            Add(3.5,()=>Shot("01-intro-dialogue"));
            for(int i=0;i<14;i++)Add(.5,Press);
            Add(1,()=>Shot("02-tracker"));
            Add(.8,()=>Place(eye,0,0)); Add(.8,()=>Shot("03-cafe-front"));
            Add(.2,()=>Place(eye,90,0)); Add(.8,()=>Shot("04-cafe-east"));
            Add(.2,()=>Place(eye,270,0)); Add(.8,()=>Shot("05-cafe-west"));
            Add(.2,()=>Place(CafeCleaning.MopSpot+new Vector3(-2.2f,.08f,0),90,10)); Add(.8,()=>Shot("06-mop-stand"));
            Add(.2,()=>Place(CafeCleaning.ClothSpot+new Vector3(-.5f,-.98f,-2.6f),0,30)); Add(.8,()=>Shot("07-cloth"));
            Add(.2,()=>Place(new Vector3(-5.5f,.08f,1.2f),270,12)); Add(.8,()=>Shot("08-stairs-from-cafe"));
            Add(.2,()=>Place(new Vector3(-6.4f,.08f,4.9f),270,8)); Add(.8,()=>Shot("08b-under-stairs"));
            Add(.2,()=>{ var d=g.cleaning.spots.Find(x=>x&&!x.table); if(d){ File.AppendAllText("Logs/StoryShots.txt","floorDirt="+d.transform.position+"\n"); Look(d.transform.position,2.2f,32);
                var asset=Resources.Load<Material>("CafeMaterials/CafeDust"); string info="CafeDust asset="+(asset?asset.shader.name+" q="+asset.renderQueue:"NULL")+"\n";
                foreach(var r in d.GetComponentsInChildren<MeshRenderer>()){ var m=r.sharedMaterial; var tex=m?m.GetTexture("_BaseMap") as Texture2D:null; info+=r.name+" mat="+(m?m.name+" shader="+m.shader.name+" color="+m.GetColor("_BaseColor")+" q="+m.renderQueue+" surface="+m.GetFloat("_Surface"):"null")+" tex="+(tex?tex.width+"x"+tex.height+" "+tex.format:"null")+" verts="+r.GetComponent<MeshFilter>().sharedMesh.vertexCount+"\n"; }
                File.AppendAllText("Logs/StoryShots.txt",info); } }); Add(.8,()=>Shot("09-floor-dirt"));
            Add(.2,()=>{ var d=g.cleaning.spots.Find(x=>x&&x.table); if(d){ File.AppendAllText("Logs/StoryShots.txt","tableDirt="+d.transform.position+"\n"); Look(d.transform.position,1.6f,38); } }); Add(.8,()=>Shot("10-table-dirt"));
            Add(.2,()=>{ g.cleaning.UseTool("cc_mop"); var d=g.cleaning.spots.Find(x=>x&&!x.table); if(d){ Look(d.transform.position,1.8f,34); g.cleaning.UseDirt(d); } }); Add(.9,()=>Shot("11-mop-held-scrub"));
            // Merdivende yürüme testi: tabandan başlayıp ileri yürü, son yüksekliği kaydet (hedef ~3.7)
            Add(.2,()=>{ g.cleaning.UseTool("cc_mop"); g.cleaning.UseTool("cc_mop"); Place(new Vector3(-9.25f,.08f,-.5f),0,0); });
            Add(.3,()=>{ var p=g.player; string log="WALK"; for(int i=0;i<260;i++){ p.Move(new Vector2(0,1),.03f); if(i%40==0)log+=" y="+p.transform.position.y.ToString("F2")+"@z"+p.transform.position.z.ToString("F1"); } log+=" END y="+p.transform.position.y.ToString("F2")+" pos="+p.transform.position+"\n"; File.AppendAllText("Logs/StoryShots.txt",log); });
            Add(.8,()=>Shot("12-after-climb"));
            Add(.2,()=>Place(new Vector3(-5.9f,3.78f,5.4f),180,5)); Add(.8,()=>Shot("13-living-tv"));
            Add(.2,()=>Place(new Vector3(-5f,3.78f,3.5f),270,5)); Add(.8,()=>Shot("14-living-stairwell"));
            Add(.2,()=>Place(new Vector3(-6.5f,3.78f,5.6f),0,5)); Add(.8,()=>Shot("15-kitchen"));
            Add(.2,()=>Place(new Vector3(-2.7f,3.78f,7.3f),90,5)); Add(.8,()=>Shot("16-hasan-room-door"));
            Add(.2,()=>Place(new Vector3(0.9f,3.78f,5.7f),340,8)); Add(.8,()=>Shot("17-hasan-bed"));
            Add(.2,()=>Place(new Vector3(-1.2f,3.78f,0.6f),38,35)); Add(.8,()=>Shot("18-our-room-bed"));
            Add(.2,()=>Place(new Vector3(-2.8f,3.78f,0.4f),90,5)); Add(.8,()=>Shot("19-our-room-desk"));
            Add(.2,()=>Place(new Vector3(-3.0f,3.78f,3.2f),70,5)); Add(.8,()=>Shot("20-bathroom"));
            Add(.2,()=>{ g.story.ReadNote(); }); Add(1.5,()=>Shot("21-note-dialogue")); for(int i=0;i<9;i++)Add(.4,Press);
            Add(.2,()=>{ g.data.storyStage=3; g.data.dayTime=300; });
            Add(.5,()=>Shot("30-night-hold"));
            Add(.2,()=>g.story.UseBed());
            Add(3,()=>Shot("31-tv-news-1"));
            Add(.5,Press); Add(.5,Press); Add(2,()=>Shot("32-tv-news-2"));
            Add(.5,Press); Add(.5,Press); Add(3,()=>Shot("33-morning-dialogue"));
            Add(.5,Press); Add(.5,Press); Add(.5,Press); Add(1.5,()=>{ Shot("34-after-sleep"); File.AppendAllText("Logs/StoryShots.txt","day="+g.data.day+" mask="+g.data.menuMask+" stage="+g.data.storyStage+"\n"); });
            return s;
        }
    }
}
