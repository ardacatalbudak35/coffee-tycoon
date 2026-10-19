using System.Collections.Generic;
using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeWorld {
  readonly List<Transform> serviceObjects=new List<Transform>();bool rearBarApplied;
  public static Vector3 ServicePoint(Vector3 oldPoint)=>oldPoint+Vector3.forward*11;
  public static Vector3 PickupPoint=>new Vector3(4.65f,0,4.1f);
  public void ApplyRearBarLayout(){if(rearBarApplied)return;rearBarApplied=true;
   if(staff)serviceObjects.Add(staff.transform);if(steam)serviceObjects.Add(steam.transform);
   foreach(Transform root in transform)if(root.name=="Sergi fincanı")serviceObjects.Add(root);
   foreach(var target in GetComponentsInChildren<CafeTarget>(true))if(target.kind=="foam"||target.kind=="art"||target.kind=="clean_machine")serviceObjects.Add(target.transform);
   var moved=new HashSet<Transform>();foreach(var root in serviceObjects){if(!root||moved.Contains(root))continue;bool descendant=false;for(var parent=root.parent;parent&&parent!=transform;parent=parent.parent)if(serviceObjects.Contains(parent)){descendant=true;break;}if(!descendant){root.position=ServicePoint(root.position);moved.Add(root);}}
   var bakery=transform.Find("Display_Counter");if(bakery){bakery.position+=new Vector3(1.1f,0,-1.95f);bakery.rotation=Quaternion.identity;}
   foreach(Transform root in transform)if(root.name=="Pencere doğraması")foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
   Physics.SyncTransforms();
  }
 }
 public sealed partial class CafePolish {
  void RearFloorPlan(){
   var moveNames=new HashSet<string>{"Bar çalışma alanı · taş zemin","Bar · ahşap yiv","Tezgâh kenar çıtası","Fincan altlığı","Çift gruplu espresso makinesi","Hassas kahve değirmeni","Kraft bardak istasyonu","Soğuk bardak istasyonu","Tezgâh tipi buz ünitesi","Barista süt sürahisi","Düzenli arka hazırlık tezgâhı","Barista çalışma paspası","Askı menü metal çubuğu","Askı menü çerçevesi","Askı menü yüzeyi","Latte art çalışma altlığı","Bar odaklı tavan rayı","Bar çalışma spotu"};
   foreach(Transform root in scenery){if(moveNames.Contains(root.name))root.localPosition=CafeWorld.ServicePoint(root.localPosition);else if(root.name.StartsWith("Atölye etiketi")&&root.localPosition.z< -3&&root.localPosition.z> -6.7f)root.localPosition=CafeWorld.ServicePoint(root.localPosition);else if(root.name=="İstasyon etiketi"){if(root.localPosition.z< -3)root.localPosition=CafeWorld.ServicePoint(root.localPosition);else root.gameObject.SetActive(false);}else if(root.name=="Butik pastane vitrini"||root.name=="Pastane seramik servis tabağı"||(root.name=="Croissant"||root.name=="Donut_1")&&root.localPosition.z< -7)root.localPosition+=new Vector3(1.1f,0,-1.95f);}
   game.world.ApplyRearBarLayout();
   Shape("Bar sol dönüş gövdesi",PrimitiveType.Cube,scenery,new Vector3(-5.5f,.52f,7.05f),new Vector3(.4f,1.04f,3.7f),walnut,true);Shape("Bar sol dönüş taş tablası",PrimitiveType.Cube,scenery,new Vector3(-5.5f,1.08f,7.05f),new Vector3(.55f,.13f,3.8f),stone);
   Shape("Barista sağ giriş paspası",PrimitiveType.Cube,scenery,new Vector3(6.5f,.05f,5.5f),new Vector3(1.3f,.025f,1.8f),fabric);InteriorLabel("BARISTA / GİRİŞ",scenery,new Vector3(6.15f,1.7f,6.1f),.028f);
   foreach(float side in new[]{-1f,1f}){float x=side*4;Shape("Giriş yan vitrin camı",PrimitiveType.Cube,scenery,new Vector3(x,1.55f,-10.98f),new Vector3(3.88f,2.4f,.055f),glass,true);foreach(float edge in new[]{-1.95f,1.95f})Shape("Giriş pencere dikmesi",PrimitiveType.Cube,scenery,new Vector3(x+edge,1.6f,-10.98f),new Vector3(.065f,2.6f,.08f),steel);Shape("Açık giriş kapısı",PrimitiveType.Cube,scenery,new Vector3(side*2.05f,1.45f,-10.35f),new Vector3(.05f,2.45f,1.3f),glass,true);Rod("Kapı pirinç tutamağı",scenery,new Vector3(side*2.01f,1.04f,-10.1f),new Vector3(side*2.01f,1.38f,-10.1f),.014f,brass);}
   Shape("Merkez giriş üst çerçevesi",PrimitiveType.Cube,scenery,new Vector3(0,2.73f,-10.98f),new Vector3(4.2f,.1f,.1f),steel);
   foreach(float x in new[]{-.85f,.85f})Shape("Merkez geçiş pirinç şeridi",PrimitiveType.Cube,scenery,new Vector3(x,.045f,-3.8f),new Vector3(.015f,.008f,11),brass);
  }
 }
}
