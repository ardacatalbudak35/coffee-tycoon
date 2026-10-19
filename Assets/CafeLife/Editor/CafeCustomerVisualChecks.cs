using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace CafeLife {
 public static class CafeCustomerVisualChecks {
  public static string Report="";
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);Report+="PASS "+message+"\n";}
  public static void Validate(CafeGame g){Report="";g.Continue();var c=g.customers.First(x=>x&&x.state==CafeCustomer.State.Drinking);var sip=c.GetComponent<CafeSip>();sip.SendMessage("Update");var cup=(GameObject)typeof(CafeSip).GetField("cup",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sip);var b=cup.transform.Find("Bardak").GetComponent<Renderer>().bounds;var table=g.world.TableCenter(c.seat/2);Check(cup.transform.parent==g.world.transform&&Vector2.Distance(new Vector2(cup.transform.position.x,cup.transform.position.z),new Vector2(table.x,table.z))<.5f&&b.min.y>.80f&&b.min.y<.85f,"Customer drink rests on table at realistic height");var original=c.transform.position;var fixedCup=cup.transform.position;c.transform.position+=Vector3.up*2;sip.SendMessage("Update");Check(Vector3.Distance(fixedCup,cup.transform.position)<.001f,"Seated animation offsets cannot move table drink");c.transform.position=original;
   var state=c.state;c.state=CafeCustomer.State.Waiting;c.transform.position=c.QueuePosition;CafeBoulevardChecks.At(g,new Vector3(0,.1f,-9.7f));c.SendMessage("Update");var label=c.GetComponentsInChildren<TextMesh>(true).First(x=>x.gameObject.name=="Sipariş balonu");Check(!label.gameObject.activeSelf,"Distant customer labels stay hidden");CafeBoulevardChecks.At(g,c.QueuePosition+Vector3.back*2);c.SendMessage("Update");Check(label.gameObject.activeSelf&&label.characterSize<=.0141f&&label.GetComponent<CafeDepthLabel>(),"Nearby order label is compact and respects depth");c.state=state;c.transform.position=original;CafeBoulevardChecks.At(g,new Vector3(0,.1f,-9.7f));
  }
 }
}
