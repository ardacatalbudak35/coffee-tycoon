using UnityEngine;

namespace CafeLife {
 public sealed partial class CafePolish {
  // All additions are visual. Existing street collision and activity targets stay in place.
  void RefineStreetArtDirection(){
   if(scenery.Find("Cadde mimari ayrıntılar"))return;
   var detail=new GameObject("Cadde mimari ayrıntılar").transform;detail.SetParent(scenery,false);
   var asphalt=Surface("Cadde · ince taneli asfalt",new Color(.09f,.105f,.11f),.035f,0,StreetSurfaceTexture(false));
   var paving=Surface("Cadde · taş kaldırım",new Color(.43f,.44f,.40f),.09f,0,StreetSurfaceTexture(true));
   var trim=Surface("Cadde · açık taş söve",new Color(.60f,.60f,.53f),.08f);
   var metal=Surface("Cadde · boyalı antrasit",new Color(.055f,.075f,.065f),.22f,.25f);
   var market=Surface("Market · adaçayı sıva",new Color(.44f,.50f,.41f),.04f,0,Plaster());
   var canopy=Surface("Market · koyu yeşil saçak",new Color(.075f,.17f,.115f),.08f);
   var glazing=Surface("Market · vitrin camı",new Color(.085f,.13f,.12f),.67f,.18f);
   var club=Surface("NOIR · kömür gri cephe",new Color(.18f,.20f,.20f),.07f,0,Plaster());
   var joint=Surface("Cadde · genleşme derzi",new Color(.31f,.33f,.30f),.02f);
   var paint=Surface("Cadde · yol boyası",new Color(.68f,.64f,.45f),.03f);
   var crossing=Surface("Cadde · yaya çizgileri",new Color(.79f,.79f,.72f),.04f);
   var diffuser=Surface("Cadde · armatür camı",new Color(.84f,.79f,.65f),.2f);
   diffuser.EnableKeyword("_EMISSION");diffuser.SetColor("_EmissionColor",new Color(.55f,.44f,.28f));

   foreach(var renderer in game.world.GetComponentsInChildren<MeshRenderer>()){
    string name=renderer.name;
    if(name=="Tek cadde · asfalt"||name=="Şehir asfaltı"){renderer.sharedMaterial=asphalt;StreetWorldUV(renderer);}
    else if(name=="Karşı kaldırım"||name=="Kaldırım"){renderer.sharedMaterial=paving;StreetWorldUV(renderer);}
    else if(name=="Karşı kaldırım derzi"||name=="Kafe kaldırım derzi")renderer.sharedMaterial=joint;
    else if(name=="Cadde bordür hattı"||name=="Market vitrin çerçevesi")renderer.sharedMaterial=trim;
    else if(name=="Cadde şerit çizgisi"||name=="Yol kesikli çizgi")renderer.sharedMaterial=paint;
    else if(name=="Yaya geçidi")renderer.sharedMaterial=crossing;
    else if(name.StartsWith("Cadde marketi")&&(name.Contains("duvar")||name.EndsWith("ön cephe")||name.EndsWith("kapı üstü")))renderer.sharedMaterial=market;
    else if(name=="Market saçak")renderer.sharedMaterial=canopy;
    else if(name=="Market vitrin")renderer.sharedMaterial=glazing;
    else if(name=="NOIR gece kulübü ön cephe"||name=="NOIR gece kulübü yan duvar")renderer.sharedMaterial=club;
   }

   // A continuous canopy edge, shallow window reveals and a sign backing give the
   // market a legible frontage without adding another sign or narrowing its doorway.
   Shape("Market saçak ön dönüşü",PrimitiveType.Cube,detail,new Vector3(-14,3.17f,-21.92f),new Vector3(12,.18f,.055f),canopy);
   Shape("Market tabela arkalığı",PrimitiveType.Cube,detail,new Vector3(-14,3,-22.79f),new Vector3(4.44f,.40f,.12f),canopy);
   Shape("Market üst silme",PrimitiveType.Cube,detail,new Vector3(-14,4.04f,-22.82f),new Vector3(12.25f,.14f,.23f),trim);
   foreach(float x in new[]{-20.06f,-7.94f}){
    Shape("Market yan saçak silmesi",PrimitiveType.Cube,detail,new Vector3(x,4.04f,-29),new Vector3(.15f,.14f,12.18f),trim);
    Shape("Market köşe sövesi",PrimitiveType.Cube,detail,new Vector3(x,1.95f,-22.81f),new Vector3(.15f,3.78f,.17f),trim);
   }
   foreach(float x in new[]{-18f,-10f}){
    Shape("Market vitrin denizliği",PrimitiveType.Cube,detail,new Vector3(x,.51f,-22.70f),new Vector3(3.18f,.09f,.26f),trim);
    Shape("Market ince vitrin kaydı",PrimitiveType.Cube,detail,new Vector3(x,1.7f,-22.705f),new Vector3(.04f,2.09f,.035f),metal);
    Shape("Market vitrin alt kaydı",PrimitiveType.Cube,detail,new Vector3(x,1.05f,-22.705f),new Vector3(2.79f,.035f,.035f),metal);
    Shape("Market cephe kaidesi",PrimitiveType.Cube,detail,new Vector3(x,.14f,-22.81f),new Vector3(3.97f,.28f,.16f),trim);
   }
   foreach(float x in new[]{-16.07f,-11.93f})Shape("Market giriş sövesi",PrimitiveType.Cube,detail,new Vector3(x,1.48f,-22.79f),new Vector3(.10f,2.96f,.18f),metal);
   foreach(float x in new[]{-19.5f,-8.5f})Rod("Market saçak taşıyıcısı",detail,new Vector3(x,2.94f,-22.78f),new Vector3(x,3.21f,-22.05f),.022f,metal);
   foreach(float y in new[]{4.17f,8.09f})Shape("NOIR ince kat silmesi",PrimitiveType.Cube,detail,new Vector3(14,y,-22.81f),new Vector3(20.18f,.12f,.20f),metal);

   // The source street lights were bare cylinders. Keep those colliders and light
   // components, and complete each post with an arm, housing and diffuser.
   foreach(var lamp in game.world.GetComponentsInChildren<Light>()){
    if(lamp.name!="Sokak ışığı")continue;
    var post=lamp.transform.parent.GetComponent<MeshRenderer>();if(post)post.sharedMaterial=metal;
    Vector3 p=game.world.transform.InverseTransformPoint(lamp.transform.position);
    Shape("Cadde lamba ayak başlığı",PrimitiveType.Cylinder,detail,new Vector3(p.x,.07f,p.z),new Vector3(.16f,.07f,.16f),metal);
    Rod("Cadde lamba yatay kolu",detail,new Vector3(p.x,2.78f,p.z),new Vector3(p.x,2.88f,p.z-.35f),.025f,metal);
    Shape("Cadde lamba armatürü",PrimitiveType.Cube,detail,new Vector3(p.x,2.86f,p.z-.43f),new Vector3(.26f,.085f,.52f),metal);
    Shape("Cadde lamba difüzörü",PrimitiveType.Cube,detail,new Vector3(p.x,2.813f,p.z-.43f),new Vector3(.20f,.012f,.43f),diffuser);
   }
  }

  Texture2D StreetSurfaceTexture(bool paving){
   const int size=128;
   var texture=new Texture2D(size,size,TextureFormat.RGB24,true){name=paving?"Metre ölçekli kaldırım taşı":"İnce asfalt agregası",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
   var pixels=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    uint seed=(uint)x*73856093u^(uint)y*19349663u;seed^=seed>>13;
    float grain=(seed&255)/255f;
    float value=.94f+grain*.06f;
    if(paving){int offset=(y/64%2)*32;bool seam=(x+offset)%64==0||y%64==0;value=seam?.83f:.96f+grain*.04f;}
    pixels[y*size+x]=new Color(value,value,value);
   }
   texture.SetPixels(pixels);texture.Apply();textures.Add(texture);return texture;
  }

  void StreetWorldUV(MeshRenderer renderer){
   var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable)return;
   var mesh=Instantiate(filter.sharedMesh);mesh.name=renderer.name+" · metre ölçekli UV";
   var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
   for(int i=0;i<vertices.Length;i++){
    Vector3 p=renderer.transform.TransformPoint(vertices[i]);Vector3 n=renderer.transform.TransformDirection(normals[i]);
    uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z)*.5f:Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f;
   }
   mesh.uv=uv;filter.sharedMesh=mesh;combinedMeshes.Add(mesh);
  }
 }
}
