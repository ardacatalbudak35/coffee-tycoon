using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafeTrayManagerChecks {
  public static string Report="";static void Check(bool ok,string text){if(!ok)throw new Exception(text);Report+="PASS "+text+"\n";}
  public static void Validate(CafeGame g){Report="";g.Continue();g.Discard();var unowned=new PreparedDrink{cup=new CupWork{recipe=0,lidded=true},good=false};unowned.visual=g.world.PlaceStaffCup(unowned.cup,0);g.Spawn();var customer=g.customers.Last();customer.state=CafeCustomer.State.ToPickup;customer.accepted=true;customer.orderNumber=++g.orderCounter;var reserved=new PreparedDrink{cup=new CupWork{recipe=1,lidded=true,hasMilk=true},owner=customer};reserved.visual=g.world.PlaceStaffCup(reserved.cup,1);g.shelf.Add(unowned);g.shelf.Add(reserved);int money=g.data.money,beans=g.data.beans;g.OpenOperations("delivery");var buttons=GameObject.Find("Cafe HUD").GetComponentsInChildren<Button>().Where(b=>b.GetComponentInChildren<Text>()?.text=="İÇECEĞİ AT"||b.GetComponentInChildren<Text>()?.text=="AYRILDI").ToArray();Check(buttons.Length==2&&buttons.Count(b=>!b.interactable)==1,"Delivery manager exposes discard only for unclaimed drink");Check(!g.DiscardPrepared(reserved)&&g.shelf.Contains(reserved),"Reserved drink rejects direct discard");buttons.First(b=>b.interactable).onClick.Invoke();Check(!g.shelf.Contains(unowned)&&g.shelf.Contains(reserved)&&g.data.money==money&&g.data.beans==beans,"Discard UI frees slot without refund or reserved loss");Check(!g.DiscardPrepared(unowned)&&g.shelf.Count==1,"Repeated discard cannot remove another drink");g.TakeReservedDrink(customer);g.customers.Remove(customer);UnityEngine.Object.Destroy(customer.gameObject);g.Continue();g.Tell("");}
 }
}
