using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeDecorChecks {
  public static string Report="";
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);Report+="PASS "+message+"\n";}
  public static void Validate(CafeGame g){Report="";if(!g.testing)throw new Exception("Decor checks require isolated save");var decor=g.GetComponent<CafeDecorProgression>();Check(decor&&decor.SurfaceCount>=6,"Decor binds textured cafe surfaces before mesh merge");int xp=g.data.xp,selected=g.data.interiorStyle,money=g.data.money;var floor=g.player.transform.position;int surfaces=decor.SurfaceCount;
   try{g.data.xp=0;g.data.interiorStyle=0;decor.Apply();var original=decor.CurrentSeatColor;Check(!decor.Select(1)&&g.data.interiorStyle==0&&g.data.money==money,"Locked decor cannot apply or debit coins");Check(!decor.Select(-1)&&!decor.Select(4),"Invalid decor choices are rejected");
    for(int i=1;i<4;i++){g.data.xp=(CafeDecorProgression.Levels[i]-1)*100-1;Check(!CafeDecorProgression.Unlocked(g.data,i),"Decor stays locked immediately before level "+CafeDecorProgression.Levels[i]);g.data.xp++;Check(decor.Select(i)&&g.data.interiorStyle==i&&g.data.money==money,"Level "+CafeDecorProgression.Levels[i]+" unlocks free decor selection");Check(Vector4.Distance(decor.CurrentSeatColor,CafeDecorProgression.Swatch(i,3))<.001f,"Selected decor changes actual seat material "+i);var merged=g.GetComponent<CafePolish>().scenery.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Birleşik kafe")).ToArray();Check(merged.Any(r=>r.sharedMaterial.name.StartsWith("Kafe tarzı")),"Combined scenery uses live decor materials "+i);Check(g.player.transform.position==floor,"Changing decor does not move player "+i);}
    Check(Economy.Load(g.SavePath).interiorStyle==3,"Decor selection survives save round trip");for(int i=0;i<12;i++)decor.Select(i%4);Check(decor.SurfaceCount==surfaces&&g.data.money==money,"Repeated decor switching reuses materials and stays free");decor.Select(0);Check(Vector4.Distance(original,decor.CurrentSeatColor)<.001f,"Original cafe palette can be restored");
    foreach(var root in g.world.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Dekor tarzı /"))){Check(root.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Decor extras do not block walking: "+root.name);foreach(var r in root.GetComponentsInChildren<Renderer>(true)){Check(r.bounds.size.magnitude<2&&Mathf.Abs(r.bounds.center.x)>8,"Decor extras keep intended size and wall placement");}}
    string path=g.SavePath+".legacy-decor";File.WriteAllText(path,"{\"version\":3,\"money\":777,\"xp\":200}");var legacy=Economy.Load(path);File.Delete(path);Check(legacy.money==777&&legacy.xp==200&&legacy.interiorStyle==0,"Old saves retain economy and default to existing decor");
   }finally{g.data.xp=xp;g.data.interiorStyle=selected;decor.Apply();g.Save();g.Continue();}
  }
 }
}
