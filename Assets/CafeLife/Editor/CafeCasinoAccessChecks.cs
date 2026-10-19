using System;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeCasinoAccessChecks {
  public static string Report="";
  static void Check(bool ok,string text){if(!ok)throw new Exception(text);Report+="PASS "+text+"\n";}
  static void Use(CafeGame game,string action){var target=game.world.GetComponentsInChildren<CafeTarget>().First(t=>t.kind=="blackjack_"+action);game.player.view.transform.rotation=Quaternion.LookRotation(target.transform.position-game.player.view.transform.position);game.player.RefreshTarget();Check(game.player.target&&game.player.target.kind==target.kind,"Looking at table action resolves "+action);game.player.Use();}
  public static void Validate(CafeGame g){Report="";g.Continue();CafeBoulevardChecks.At(g,new Vector3(15,4.25f,-30.5f),180);g.player.RefreshTarget();Check(g.player.target&&g.player.target.kind=="casino","Looking at blackjack table resolves interaction");g.player.Use();Check(!g.Paused&&g.GetComponent<CafeTableBlackjack>().Opened,"Blackjack plays at physical table without a menu");var casino=g.GetComponent<CafeCasino>();for(int attempt=0;attempt<20&&!casino.Active;attempt++)Use(g,"10");Check(casino.Active&&casino.player.Count==2,"Physical betting chip deals actual cards");Check(g.world.GetComponentsInChildren<Transform>().Any(t=>t.name=="Kapalı krupiye kartı"),"Dealer hole card stays face down");Use(g,"hit");Check(casino.player.Count>=3,"Table Hit draws an actual card");if(casino.Active)Use(g,"stand");Check(!casino.Active&&!g.world.GetComponentsInChildren<Transform>().Any(t=>t.name=="Kapalı krupiye kartı"),"Stand reveals dealer hand on felt");g.GetComponent<CafeTableBlackjack>().Action("leave");CafeBoulevardChecks.At(g,new Vector3(15,.1f,-31.8f),180);g.player.view.transform.rotation=Quaternion.LookRotation(new Vector3(15,5.3f,-33)-g.player.view.transform.position);g.player.RefreshTarget();Check(!g.player.target||g.player.target.kind!="casino","Casino cannot activate through downstairs floor");}
 }
}
