using System;
using UnityEngine;
namespace CafeLife {
 public static class CafeLatteSafetyChecks {
  public static string Report="";
  static void Check(bool ok,string title){if(!ok)throw new Exception(title);Report+="PASS "+title+"\n";}
  static CupWork Cup()=>new CupWork{recipe=1,hasMilk=true,foamed=true,brewed=true};
  static void Draw(CafeLattePour p){for(int i=0;i<50;i++){float a=i/49f*Mathf.PI*2;p.PourAt(new Vector2(Mathf.Sin(a)*.5f,Mathf.Cos(a)*.5f),1,8,.2f);}}
  public static void Validate(CafeGame g){Report="";g.Continue();g.Discard();var p=g.GetComponent<CafeLattePour>();p.Begin(g);Check(!p.Active,"Latte art ignores missing cup");g.cup=Cup();g.cup.lidded=true;p.Begin(g);Check(!p.Active,"Latte art ignores sealed cup");g.Discard();g.cup=Cup();var old=g.cup;p.Begin(g);Draw(p);p.Cancel();Check(!p.Active&&!old.art&&old.artPoints.Count==0&&g.cup==old,"Cancel clears unfinished pattern and preserves drink");p.Begin(g);Draw(p);p.Complete();Check(old.art&&old.artPoints.Count>35,"Cancelled drink can be redrawn successfully");g.Discard();
   g.cup=Cup();old=g.cup;p.Begin(g);Draw(p);g.Discard();Check(!p.Active&&g.cup==null&&old.artPoints.Count==0,"Discard closes latte screen immediately");
   g.cup=Cup();old=g.cup;p.Begin(g);Draw(p);var replacement=Cup();g.cup=replacement;p.Complete();Check(!p.Active&&!old.art&&!replacement.art&&old.artPoints.Count==0,"Complete cannot commit after cup replacement");
   p.Begin(g);Draw(p);g.OpenOperations("business");p.Complete();Check(!p.Active&&!replacement.art&&replacement.artPoints.Count==0&&g.Paused,"Opening menu cancels pattern without closing menu");g.Continue();
   p.Begin(g);Draw(p);p.enabled=false;Check(!p.Active&&replacement.artPoints.Count==0,"Disabling latte component releases unfinished session");p.enabled=true;g.Discard();
  }
 }
}
