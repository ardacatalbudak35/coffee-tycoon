using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeWorld {
  void BuildBoulevard(){
   Color asphalt=new Color(.09f,.11f,.14f),paving=new Color(.38f,.39f,.4f),dark=new Color(.055f,.045f,.09f),gold=new Color(.85f,.62f,.26f);
   Block("Tek cadde · asfalt",new Vector3(0,-.12f,-18),new Vector3(72,.24f,6),asphalt);
   Block("Karşı kaldırım",new Vector3(0,-.12f,-22),new Vector3(72,.24f,2),paving);
   for(int i=-8;i<=8;i++)Block("Cadde şerit çizgisi",new Vector3(i*4,.012f,-18),new Vector3(2,.018f,.12f),gold);
   for(int i=0;i<6;i++)Block("Yaya geçidi",new Vector3(0,.02f,-15.5f-i),new Vector3(3,.018f,.45f),Color.white);
   foreach(float x in new[]{-35f,35f}){Block("Cadde sonu duvarı",new Vector3(x,1,-27),new Vector3(.4f,2,34),dark);for(int z=-20;z<=-16;z+=2){Block("Cadde sonu çiçeklik",new Vector3(x, .45f,z),new Vector3(1,.9f,1),paving);}}
   Block("Cadde arka sınırı",new Vector3(0,1,-44),new Vector3(70,2,.3f),dark);
   Shell("Cadde marketi",new Vector3(-14,0,-29),new Color(.17f,.32f,.3f),false,12,12);
   Sign("GECE MARKETİ",new Vector3(-14,3,-22.7f),.075f,new Color(.65f,1,.8f),180);
   var market=Model("Modern Shelves",new Vector3(-14,0,-31),2.4f,0);Target(market,"boulevard_market");
   Sign("TEDARİK / KAFE YÖNETİMİ",new Vector3(-14,1.9f,-30.6f),.035f,Color.white,180);
   var board=Block("Cadde lig panosu",new Vector3(-10,1.5f,-13.8f),new Vector3(2.4f,2.4f,.18f),dark);Target(board,"boulevard_ranking");Sign("Cadde Ligi\nEn iyi kafeler",new Vector3(-10,1.65f,-13.65f),.055f,gold,180,new Vector2(1.95f,1.15f));
   Block("Basket noktası · saha",new Vector3(-28,.015f,-30),new Vector3(10,.03f,14),new Color(.15f,.28f,.3f));
   Block("Basket pota direği",new Vector3(-28,1.9f,-36),new Vector3(.14f,3.8f,.14f),dark);Block("Basket pota levhası",new Vector3(-28,3.45f,-35.9f),new Vector3(2,1.25f,.12f),new Color(.8f,.82f,.79f));
   Vector3 hoopCenter=new Vector3(-28,3.05f,-35.5f);var ringRoot=new GameObject("Fiziksel basket çemberi");ringRoot.transform.SetParent(transform,false);
   for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;Vector3 start=hoopCenter+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.28f,end=hoopCenter+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*.28f;var segment=GameObject.CreatePrimitive(PrimitiveType.Capsule);segment.name="Çember parçası";segment.transform.SetParent(ringRoot.transform,false);segment.transform.position=(start+end)*.5f;segment.transform.rotation=Quaternion.FromToRotation(Vector3.up,end-start);segment.transform.localScale=new Vector3(.032f,Vector3.Distance(start,end)*.5f,.032f);segment.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.9f,.28f,.07f));}
   var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="Basket topu";ball.transform.SetParent(transform,false);ball.transform.position=new Vector3(-28,.3f,-27);ball.transform.localScale=Vector3.one*.24f;ball.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.8f,.32f,.08f));ball.layer=8;ball.AddComponent<CafeTarget>().kind="boulevard_arcade";
   var bounce=new PhysicsMaterial("Basket topu kauçuk"){bounciness=.72f,dynamicFriction=.6f,staticFriction=.6f,bounceCombine=PhysicsMaterialCombine.Maximum};ball.GetComponent<SphereCollider>().material=bounce;
   var pickup=new GameObject("Topu alma alanı");pickup.transform.SetParent(ball.transform,false);pickup.layer=8;var pickupTrigger=pickup.AddComponent<SphereCollider>();pickupTrigger.isTrigger=true;pickupTrigger.radius=1.3f;
   var rigid=ball.AddComponent<Rigidbody>();rigid.mass=.62f;rigid.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rigid.interpolation=RigidbodyInterpolation.Interpolate;rigid.linearDamping=.08f;rigid.angularDamping=.1f;
   var basketball=game.GetComponent<CafeBasketball>();if(!basketball)basketball=game.gameObject.AddComponent<CafeBasketball>();basketball.Setup(game,rigid,hoopCenter);
   for(int i=0;i<2;i++){var bench=Model("Chair",new Vector3(-32+i*2,0,-23.6f),.9f,180);Target(bench,"bench");}

   Shell("NOIR gece kulübü",new Vector3(14,0,-32),dark,true);
   Sign("S M O K E B A N K",new Vector3(14,3.1f,-22.7f),.13f,new Color(.9f,.3f,1),180);
   Sign("GECE KULÜBÜ   /   CASINO ÜST KAT",new Vector3(14,2.45f,-22.7f),.035f,new Color(.65f,.8f,1),180);
   Block("Casino üst kat ana döşeme",new Vector3(12,4.08f,-32),new Vector3(16,.24f,18),new Color(.19f,.1f,.12f));
   Block("Merdiven üst sahanlık",new Vector3(22,4.08f,-38.75f),new Vector3(4,.24f,4.5f),new Color(.19f,.1f,.12f));
   for(int i=0;i<21;i++){var stair=Block("Kulüp merdiveni · basamak",new Vector3(22,(i+.5f)*.2f,-26-(i+.5f)*.5f),new Vector3(3,.2f,.5f),new Color(.22f,.2f,.27f));stair.GetComponent<Collider>().enabled=false;}
   var ramp=new GameObject("Kulüp merdiveni · yürünebilir yüzey");ramp.transform.SetParent(transform,false);var mesh=new Mesh();mesh.vertices=new[]{new Vector3(20.5f,0,-26),new Vector3(23.5f,0,-26),new Vector3(20.5f,4.2f,-36.5f),new Vector3(23.5f,4.2f,-36.5f)};mesh.triangles=new[]{0,1,2,1,3,2};mesh.RecalculateNormals();ramp.AddComponent<MeshCollider>().sharedMesh=mesh;
   for(int i=0;i<6;i++)Block("Merdiven korkuluk dikmesi",new Vector3(20.4f,1+i*.7f,-26-i*1.75f),new Vector3(.08f,1.2f,.08f),gold);
   var rail=Block("Merdiven eğimli korkuluk",new Vector3(20.4f,3.05f,-31.25f),new Vector3(.09f,.09f,11.3f),gold);rail.transform.rotation=Quaternion.Euler(21.8f,0,0);
   Block("Üst kat galeri korkuluğu",new Vector3(20,4.8f,-30.5f),new Vector3(.1f,1.2f,13),gold);
   Block("Casino balkon güvenliği",new Vector3(14,4.75f,-23),new Vector3(4,1.1f,.15f),gold);
   Sign("CASINO  ↑",new Vector3(22,2,-25.2f),.055f,gold,180);
   Sign("ROYAL JACKPOT",new Vector3(12,6.8f,-40.7f),.075f,gold,180);
   Sign("BLACKJACK · OYUN İÇİ COIN",new Vector3(12,6.15f,-40.7f),.035f,Color.white,180);
   for(int i=0;i<2;i++){
    float x=8+i*7;var table=Round("Casino blackjack masası",new Vector3(x,5.05f,-33),new Vector3(3.8f,.14f,2.5f),new Color(.025f,.3f,.2f));Round("Casino masa ayağı",new Vector3(x,4.6f,-33),new Vector3(.4f,.9f,.4f),dark);
    for(int j=0;j<3;j++)Round("Casino fiş yığını",new Vector3(x-.6f+j*.5f,5.2f,-33),new Vector3(.2f,.13f,.2f),j==0?gold:new Color(.7f,.1f,.2f));
    var chair=Model("Chair",new Vector3(x,4.2f,-31),.7f,180);Target(chair,"casino");
var playArea=new GameObject("Blackjack masası · geniş etkileşim");playArea.transform.SetParent(transform,false);playArea.transform.position=new Vector3(x,4.2f,-33);playArea.layer=8;var box=playArea.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up*1.1f;box.size=new Vector3(4.1f,1.9f,3.1f);var playTarget=playArea.AddComponent<CafeTarget>();playTarget.kind="casino";var nearby=playArea.AddComponent<CafeActivityArea>();nearby.target=playTarget;nearby.floorY=4.2f;nearby.radius=3.3f;


   }
   Block("NOIR bar tezgâhı",new Vector3(7, .6f,-38),new Vector3(4,1.2f,1.1f),new Color(.2f,.1f,.25f));
   for(int i=0;i<5;i++)Model("bottle",new Vector3(5.5f+i*.65f,1.2f,-38),.18f,0,false);
   Block("DJ platformu",new Vector3(14,.18f,-39),new Vector3(5,.36f,2),dark);
   Block("DJ masası",new Vector3(14,1,-39),new Vector3(3.3f,.35f,1),dark);
   for(int i=-1;i<=1;i+=2){Round("DJ pikap",new Vector3(14+i*.85f,1.2f,-39),new Vector3(.6f,.05f,.6f),gold);Block("Kulüp hoparlörü",new Vector3(14+i*3,1.2f,-39),new Vector3(.9f,2.4f,.8f),dark);}
   var floorMaterial=ColorMaterial(new Color(.25f,.045f,.4f));floorMaterial.EnableKeyword("_EMISSION");floorMaterial.SetColor("_EmissionColor",new Color(.35f,.03f,.55f));
   for(int x=0;x<5;x++)for(int z=0;z<5;z++){var tile=Block("NOIR dans pisti",new Vector3(11+x*1.1f,.025f,-28-z*1.1f),new Vector3(1,.04f,1),dark);if((x+z)%2==0)tile.GetComponent<Renderer>().sharedMaterial=floorMaterial;}
   foreach(var target in GetComponentsInChildren<CafeTarget>())if(target.kind=="boulevard_ranking"||target.kind=="boulevard_market"){var near=target.gameObject.AddComponent<CafeActivityArea>();near.target=target;near.floorY=0;near.radius=2.3f;}
DecorateBoulevard();DecorateClub();
   var life=gameObject.AddComponent<CafeNightlife>();life.Setup(game);
   foreach(var entry in new[]{new Vector3(14,3.3f,-30),new Vector3(7,6.8f,-34),new Vector3(17,6.8f,-34)}){var lamp=new GameObject("NOIR iç aydınlatma");lamp.transform.SetParent(transform,false);lamp.transform.position=entry;var l=lamp.AddComponent<Light>();l.type=LightType.Point;l.range=10;l.intensity=10;l.color=entry.y<4?new Color(.7f,.3f,1):new Color(1,.7f,.35f);}
  }
  void Shell(string title,Vector3 center,Color color,bool tall=false,float width=20,float depth=18){float h=tall?8:4;Block(title+" zemin",center+Vector3.down*.12f,new Vector3(width,.24f,depth),new Color(.13f,.12f,.17f));Block(title+" arka duvar",center+new Vector3(0,h/2,-depth/2),new Vector3(width,h,.25f),color);foreach(int side in new[]{-1,1}){Block(title+" yan duvar",center+new Vector3(side*width/2,h/2,0),new Vector3(.25f,h,depth),color);Block(title+" ön cephe",center+new Vector3(side*(width/4+1),h/2,depth/2),new Vector3((width-4)/2,h,.25f),color);}Block(title+" kapı üstü",center+new Vector3(0,h-.4f,depth/2),new Vector3(4,.8f,.25f),color);Block(title+" tavan",center+new Vector3(0,h+.1f,0),new Vector3(width,.2f,depth),color);}
 }
}
