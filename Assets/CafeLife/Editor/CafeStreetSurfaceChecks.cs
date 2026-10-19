using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace CafeLife {
 public static class CafeStreetSurfaceChecks {
  public static string Report="";
  static void Check(bool value,string message){if(!value)throw new Exception(message);Report+="PASS "+message+"\n";}
  public static void Validate(CafeGame game){
   Report="";Physics.SyncTransforms();var renderers=game.world.GetComponentsInChildren<MeshRenderer>();
   var asphalt=renderers.Single(r=>r.name=="Tek cadde · asfalt");
   var walk=renderers.Single(r=>r.name=="Kaldırım");var farWalk=renderers.Single(r=>r.name=="Karşı kaldırım");
   Check(Mathf.Abs(asphalt.GetComponent<Collider>().bounds.max.y)<.0005f&&Mathf.Abs(walk.GetComponent<Collider>().bounds.max.y)<.0005f&&Mathf.Abs(farWalk.GetComponent<Collider>().bounds.max.y)<.0005f,"Asphalt and pavement collision heights remain unchanged");
   var curbs=renderers.Where(r=>r.name=="Cadde bordür hattı").ToArray();
   Check(curbs.Length==2&&curbs.All(r=>r.bounds.max.y-asphalt.bounds.max.y>=.02f&&Mathf.Abs(r.GetComponent<Collider>().bounds.max.y)<.0005f),"Curb render caps are separated from ground while their collision remains unchanged");
   Check(renderers.Where(r=>r.name=="Şehir asfaltı"||r.name=="Yol kesikli çizgi").All(r=>!r.enabled),"Buried duplicate road and old markings do not render coincident side faces");
   var paint=renderers.Where(r=>r.name=="Cadde şerit çizgisi"||r.name=="Yaya geçidi").ToArray();
   Check(paint.Length==23&&paint.All(r=>r.enabled&&r.bounds.size.y<.0005f&&r.bounds.min.y>asphalt.bounds.max.y+.01f&&r.shadowCastingMode==ShadowCastingMode.Off),"Road paint is flat, separated from asphalt and does not cast false edge shadows");
   Check(paint.All(r=>Mathf.Abs(r.GetComponent<Collider>().bounds.max.y-(r.name=="Yaya geçidi"?.029f:.021f))<.0005f),"Existing marking collision geometry remains unchanged");
   var nearJoints=renderers.Where(r=>r.name=="Kafe kaldırım derzi").ToArray();
   Check(nearJoints.Length==18&&nearJoints.Count(r=>r.enabled)==10&&nearJoints.Where(r=>r.enabled).All(r=>r.bounds.min.x>=walk.bounds.min.x&&r.bounds.max.x<=walk.bounds.max.x),"Pavement joints outside the cafe sidewalk do not float over the lower ground");
   Check(renderers.Where(r=>r.enabled&&(r.name=="Kafe kaldırım derzi"||r.name=="Karşı kaldırım derzi")).All(r=>r.bounds.size.y<.0005f&&r.shadowCastingMode==ShadowCastingMode.Off),"Visible pavement joints are flat and do not cast raised-strip shadows");
   var court=renderers.Single(r=>r.name=="Basket noktası · saha");
   var sidelines=renderers.Where(r=>r.name=="Basket saha yan çizgisi").ToArray();
   Check(sidelines.Length==2&&sidelines.All(r=>r.enabled&&r.bounds.size.y<.0005f&&Mathf.Abs(r.bounds.min.y-.036f)<.0005f&&r.bounds.min.y-court.bounds.max.y>.005f&&r.shadowCastingMode==ShadowCastingMode.Off),"Basketball sidelines are flat paint above the court without false edge shadows");
   Check(Mathf.Abs(court.GetComponent<Collider>().bounds.max.y-.03f)<.0005f&&sidelines.All(r=>Mathf.Abs(r.GetComponent<Collider>().bounds.max.y-.0575f)<.0005f),"Basketball court and sideline collision geometry remains unchanged");
  }
 }
}
