using System.Collections.Generic;
using UnityEngine;
namespace CafeLife {
 public sealed partial class CafePolish {
  Mesh sleeveMesh;
  Mesh ProfileMesh(string title,Vector2[] rings){var vertices=new List<Vector3>();var triangles=new List<int>();const int n=32;for(int j=0;j<rings.Length;j++)for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;vertices.Add(new Vector3(Mathf.Cos(a)*rings[j].x,rings[j].y,Mathf.Sin(a)*rings[j].x));}for(int j=0;j<rings.Length-1;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+n+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}var mesh=new Mesh{name=title};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();combinedMeshes.Add(mesh);return mesh;}
  GameObject ProfileObject(string title,Transform parent,Mesh mesh,Material material){var o=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=material;return o;}
  void FinishBarEquipment(){
   foreach(Transform group in scenery)if(group.name=="Çift gruplu espresso makinesi"||group.name=="Hassas kahve değirmeni"||group.name=="Tezgâh tipi buz ünitesi"||group.name=="Kraft bardak istasyonu"||group.name=="Soğuk bardak istasyonu"||group.name=="Barista süt sürahisi")group.localRotation=Quaternion.Euler(0,180,0);
   var machine=game.world.transform.Find("Coffee_Machine");if(machine)machine.rotation=Quaternion.Euler(0,180,0);
   foreach(var target in game.world.GetComponentsInChildren<CafeTarget>())if(target.kind=="caramel"||target.kind=="vanilla"){
    HideEquipment(target.transform);bool vanilla=target.kind=="vanilla";var bottle=InteriorGroup(vanilla?"Vanilya pompalı şurup":"Karamel pompalı şurup",target.transform.position);
    var amber=Surface(vanilla?"Vanilya altın cam":"Karamel amber cam",vanilla?new Color(.64f,.40f,.13f):new Color(.32f,.12f,.035f),.82f,.08f);
    var mesh=ProfileMesh("Omuzlu şurup şişesi",new[]{new Vector2(0,0),new Vector2(.065f,0),new Vector2(.073f,.02f),new Vector2(.073f,.28f),new Vector2(.061f,.32f),new Vector2(.026f,.36f),new Vector2(.026f,.42f),new Vector2(0,.42f)});
    ProfileObject("Amber şişe gövdesi",bottle,mesh,amber);Disc("Şişe taban halkası",bottle,new Vector3(0,.018f,0),new Vector3(.15f,.018f,.15f),brass);
    Disc("Şurup pompa bileziği",bottle,new Vector3(0,.414f,0),new Vector3(.068f,.04f,.068f),steel);Rod("Pompa pistonu",bottle,new Vector3(0,.43f,0),new Vector3(0,.48f,0),.009f,brass);Shape("Şurup pompa başlığı",PrimitiveType.Cube,bottle,new Vector3(0,.485f,.047f),new Vector3(.045f,.024f,.13f),steel);Rod("Şurup doz ağzı",bottle,new Vector3(0,.485f,.103f),new Vector3(0,.46f,.103f),.007f,steel);
    foreach(float side in new[]{-1f,1f}){Shape("Şurup basılı etiketi",PrimitiveType.Cube,bottle,new Vector3(0,.19f,side*.074f),new Vector3(.108f,.15f,.004f),paper);InteriorLabel("ARDA'S\n"+(vanilla?"VANILLA":"CARAMEL")+"\nSYRUP / 700 ml",bottle,new Vector3(0,.19f,side*.077f),.0028f,side>0?180:0);}
   }
   foreach(var text in scenery.GetComponentsInChildren<TextMesh>())if(text.text.Contains("SYRUP /"))text.color=new Color(.15f,.12f,.08f);
   var care=System.Array.Find(game.world.GetComponentsInChildren<CafeTarget>(),t=>t.kind=="clean_machine");if(care){HideEquipment(care.transform);care.transform.position=new Vector3(-2.2f,1.02f,7.65f);Shape("Barista bakım bezi",PrimitiveType.Cube,scenery,new Vector3(-2.2f,1.015f,7.65f),new Vector3(.28f,.035f,.23f),fabric);}
   foreach(Transform item in game.world.transform)if(item.name=="Sergi fincanı")item.position=new Vector3(item.position.x-.2f,1.01f,8.05f);
   foreach(Transform item in scenery)if(item.name=="Fincan altlığı")item.localPosition=new Vector3(item.localPosition.x-.2f,.995f,8.05f);
   foreach(Transform station in scenery)if(station.name=="Kraft bardak istasyonu"||station.name=="Soğuk bardak istasyonu"){
    foreach(Transform old in station)if(old.name!="Bardak taşıma tabanı")old.gameObject.SetActive(false);
    bool cold=station.name=="Soğuk bardak istasyonu";for(int i=0;i<4;i++){var cup=game.world.CupModel(new CupWork{coldCup=cold},Economy.Recipes[cold?5:0],station,false);cup.name="İç içe tasarım bardak";cup.transform.localPosition=new Vector3(0,.12f+i*.027f,0);}
   }
  }
  void RefineCup(GameObject root,CupWork work){
   bool cold=work.coldCup||work.iced;
   if(!cold){
    var old=root.transform.Find("Keten marka kılıfı");if(old)old.gameObject.SetActive(false);var logo=root.transform.Find("Bardak markası");if(logo)logo.gameObject.SetActive(false);
    if(!sleeveMesh)sleeveMesh=ProfileMesh("Konik karton marka kılıfı",new[]{new Vector2(.065f,-.06f),new Vector2(.072f,.045f)});
    ProfileObject("Orman yeşili marka kılıfı",root.transform,sleeveMesh,fabric);
    foreach(float side in new[]{-1f,1f}){InteriorLabel("A\nCOFFEE",root.transform,new Vector3(0,-.005f,side*.073f),.0038f,side>0?180:0);}
   }
   Disc("Bardak kıvrımlı ağız halkası",root.transform,new Vector3(0,.144f,0),new Vector3(.162f,.012f,.162f),cold?glass:paper);
   Disc("Bardak taban dikişi",root.transform,new Vector3(0,-.132f,0),new Vector3(.117f,.008f,.117f),cold?glass:paper);
   if(work.lidded){Disc("Kapak oturma kenarı",root.transform,new Vector3(0,.143f,0),new Vector3(.178f,.014f,.178f),cold?glass:steel);if(!cold){Disc("Kapak yükseltilmiş iç yüzey",root.transform,new Vector3(0,.17f,0),new Vector3(.143f,.018f,.143f),steel);Shape("Kapak içme deliği",PrimitiveType.Cube,root.transform,new Vector3(0,.182f,-.05f),new Vector3(.037f,.003f,.017f),coffeeBean);}}
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
  }
 }
}


