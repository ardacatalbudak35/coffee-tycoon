using System;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeRearBarChecks {
  public static string Report="";
  static void Check(bool ok,string title){if(!ok)throw new Exception(title);Report+="PASS "+title+"\n";}
  static void Walk(CafeGame g,Vector3 destination){for(int i=0;i<1800;i++){var delta=destination-g.player.transform.position;delta.y=0;if(delta.magnitude<.15f)return;var local=g.player.transform.InverseTransformDirection(delta.normalized);g.player.Move(new Vector2(local.x,local.z),.02f);}throw new Exception("Rear bar route blocked at "+g.player.transform.position+" toward "+destination+" nearby: "+string.Join(" | ",Physics.OverlapSphere(g.player.transform.position+Vector3.up*.8f+Vector3.forward*.5f,.7f).Where(c=>!c.isTrigger).Select(c=>c.name+" @ "+c.bounds.center+" size "+c.bounds.size)));}
  public static void Validate(CafeGame g){Report="";g.Continue();Check(g.world.transform.Find("Kahve barı - ahşap gövde").position.z>5,"Bar counter relocated to rear wall zone");CafeBoulevardChecks.At(g,new Vector3(0,.1f,-13.2f));Walk(g,new Vector3(0,0,-9.7f));Walk(g,new Vector3(0,0,2));Check(g.player.transform.position.z>1.8f,"Clear walk through new central doorway and seating spine");Walk(g,new Vector3(6.5f,0,2));Walk(g,new Vector3(6.5f,0,6.2f));Walk(g,new Vector3(-4.8f,0,6.2f));Check(g.player.transform.position.x< -4.6f,"Right-hand barista entrance reaches full preparation aisle");
   foreach(string kind in new[]{"cup_hot","cup_cold","ice","grinder","machine","foam","art","caramel","vanilla","lid_hot","lid_cold","pickup","milk","water","depot","clean_machine"}){var target=g.world.GetComponentsInChildren<CafeTarget>().First(t=>t.kind==kind);Check(target.transform.position.z>4&&target.transform.position.z<9,"Rear bar station and interaction relocated "+kind);}
   foreach(var t in g.world.GetComponentsInChildren<Transform>())if(t.name=="Çift gruplu espresso makinesi"||t.name=="Hassas kahve değirmeni"||t.name=="Tezgâh tipi buz ünitesi")Check(Vector3.Dot(t.forward,Vector3.back)>.99f,"Equipment faces barista aisle "+t.name);Check(!GameObject.Find("Cafe HUD").GetComponentsInChildren<Transform>(true).First(t=>t.name=="Hidden legacy status header").gameObject.activeSelf,"Wide status header removed");
   Check(Vector3.Dot(g.world.transform.Find("refrigerator_1").forward,Vector3.back)>.99f,"Fridge doors face preparation aisle");
   var delivery=g.world.transform.Find("Tray");for(int i=0;i<6;i++){g.cup=new CupWork{recipe=0,brewed=true,lidded=true};g.Handoff();}Check(g.shelf.Count==6,"Six actual handoffs remain visible on delivery tray");for(int i=0;i<6;i++){var visual=g.shelf[i].visual;var body=visual.transform.Find("Bardak").GetComponent<Renderer>().bounds;Check(Mathf.Abs(visual.transform.position.x-delivery.position.x)<.3f&&Mathf.Abs(visual.transform.position.z-delivery.position.z)<.2f&&body.min.y>1.14f,"Prepared drink sits above delivery tray slot "+i);}foreach(var d in g.shelf){d.visual.SetActive(false);UnityEngine.Object.Destroy(d.visual);}g.shelf.Clear();
   g.Spawn();var customer=g.customers.Last();for(int i=0;i<18000&&customer.state==CafeCustomer.State.Entering;i++)customer.SendMessage("Update");Check(customer.state==CafeCustomer.State.Waiting&&Vector3.Distance(customer.transform.position,customer.QueuePosition)<.1f,"Customer enters central door and reaches the relocated order queue");g.customers.Remove(customer);UnityEngine.Object.Destroy(customer.gameObject);CafeBoulevardChecks.At(g,new Vector3(0,.1f,-9.7f));}
 }
}




