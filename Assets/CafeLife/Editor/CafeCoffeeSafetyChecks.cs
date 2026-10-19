using System;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeCoffeeSafetyChecks {
  public static string Report="";
  static void Check(bool ok,string title){if(!ok)throw new Exception(title);Report+="PASS "+title+"\n";}
  static void Clear(CafeGame g){g.Discard();foreach(var d in g.shelf){if(d.visual){d.visual.SetActive(false);UnityEngine.Object.Destroy(d.visual);}}g.shelf.Clear();}
  public static void Validate(CafeGame g){Report="";g.Continue();Clear(g);g.selected=0;g.data.beans=g.data.milk=g.data.ice=g.data.cupsHot=g.data.cupsCold=g.data.lidsHot=g.data.lidsCold=g.data.caramel=g.data.vanilla=30;
   g.Act("cup_hot");g.Act("grinder");int beans=g.data.beans;g.Act("grinder");Check(g.data.beans==beans,"Repeated grinding consumes no additional beans");g.Act("machine");Check(g.CupOnMachine&&!g.CupInHand,"Placed cup exists only at machine");var brewing=g.cup;int lids=g.data.lidsHot;g.Act("lid_hot");Check(g.brewTime>0&&!g.cup.lidded&&g.data.lidsHot==lids,"Active brewing blocks lid without consuming packaging");brewing.lidded=true;g.Handoff();Check(g.cup==brewing&&g.shelf.Count==0,"In-flight brew cannot be handed off to a different cup");brewing.lidded=false;g.Act("machine");Check(g.brewTime==0&&g.cup.brewed,"Barista can still stop espresso flow normally");Check(g.CupOnMachine&&!g.CupInHand,"Stopped espresso remains at machine");g.Act("machine");Check(!g.CupOnMachine&&g.CupInHand,"E pickup returns cup to hand");g.Act("lid_hot");int steps=g.cup.steps.Count;var stock=new[]{g.data.beans,g.data.milk,g.data.ice,g.data.caramel,g.data.vanilla};foreach(string action in new[]{"milk","water","ice","caramel","vanilla","grinder","machine"})g.Act(action);Check(g.cup.steps.Count==steps&&!g.cup.hasMilk&&!g.cup.hasWater&&stock.SequenceEqual(new[]{g.data.beans,g.data.milk,g.data.ice,g.data.caramel,g.data.vanilla}),"Sealed drink rejects added ingredients and brewing without stock loss");g.Handoff();Check(g.cup==null&&g.shelf.Count==1,"Finished sealed drink remains deliverable");Clear(g);
   g.Act("cup_hot");g.Act("grinder");g.Act("machine");g.Discard();Check(g.cup==null&&g.brewTime==0,"Discard cancels active brew");g.Act("cup_hot");Check(!g.cup.brewed&&g.brewTime==0,"New cup cannot inherit discarded extraction");g.Discard();
   g.cup=new CupWork{recipe=1,hasMilk=true,brewed=true};g.Act("foam");lids=g.data.lidsHot;g.Act("lid_hot");Check(g.expansion.foamTime>0&&!g.cup.lidded&&g.data.lidsHot==lids,"Active milk steaming blocks lid without stock loss");g.Discard();Check(g.expansion.foamTime==0,"Discard cancels milk steaming");
   g.data.cupsHot=0;g.Act("cup_hot");Check(g.cup==null&&g.data.cupsHot==0,"Empty cup stock cannot become negative");g.data.cupsHot=20;g.data.beans=0;g.Act("cup_hot");g.Act("grinder");Check(!g.cup.ground&&g.data.beans==0,"Empty bean stock cannot produce grounds");g.Discard();g.data.beans=30;
   for(int i=0;i<6;i++){g.cup=new CupWork{recipe=0,brewed=true,lidded=true};g.Handoff();}var full=new CupWork{recipe=0,brewed=true,lidded=true};g.cup=full;g.Handoff();Check(g.shelf.Count==6&&g.cup==full,"Full delivery tray preserves seventh drink in hand");Clear(g);
  }
 }
}
