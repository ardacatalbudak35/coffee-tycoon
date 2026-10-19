using UnityEngine;
namespace CafeLife {
 public sealed partial class CafePolish {
  void FinishNightInteriors(){
   var violet=Surface("NOIR kadife",new Color(.16f,.075f,.19f),.14f);var felt=Surface("Casino koyu yeşil keçe",new Color(.025f,.15f,.11f),.1f);var burgundy=Surface("Casino bordo deri",new Color(.24f,.045f,.065f),.43f);var neon=Surface("NOIR sıcak neon",new Color(.88f,.54f,.32f),.35f);neon.EnableKeyword("_EMISSION");neon.SetColor("_EmissionColor",new Color(.95f,.35f,.12f)*1.2f);
   // All detail is kept against the existing walls or on existing furniture.
   for(int i=0;i<24;i++)Shape("NOIR bar ceviz yiv",PrimitiveType.Cube,scenery,new Vector3(5.1f+i*.165f,.57f,-37.43f),new Vector3(.045f,1.02f,.045f),walnut);
   Shape("NOIR bar taş kaplama",PrimitiveType.Cube,scenery,new Vector3(7,1.24f,-38),new Vector3(4.15f,.08f,1.2f),stone);Rod("NOIR bar pirinç ayaklık",scenery,new Vector3(5,.24f,-37.2f),new Vector3(9,.24f,-37.2f),.025f,brass);
   foreach(float y in new[]{1.9f,2.65f}){Shape("NOIR duvar şişe rafı",PrimitiveType.Cube,scenery,new Vector3(7,y,-40.55f),new Vector3(4.4f,.07f,.48f),walnut);Shape("NOIR raf altı ışık",PrimitiveType.Cube,scenery,new Vector3(7,y-.055f,-40.28f),new Vector3(4.2f,.025f,.025f),neon);for(int i=0;i<7;i++){var bottle=InteriorGroup("NOIR raf şişesi",new Vector3(5.3f+i*.56f,y+.045f,-40.5f));Disc("Şişe gövdesi",bottle,new Vector3(0,.13f,0),new Vector3(.12f,.26f,.12f),i%2==0?brass:violet);Disc("Şişe boynu",bottle,new Vector3(0,.30f,0),new Vector3(.055f,.1f,.055f),steel);}}
   foreach(float x in new[]{5.65f,7f,8.35f}){Rod("NOIR bar taburesi ayağı",scenery,new Vector3(x,.05f,-36.2f),new Vector3(x,.69f,-36.2f),.035f,brass);Disc("NOIR tabure tabanı",scenery,new Vector3(x,.04f,-36.2f),new Vector3(.46f,.055f,.46f),steel);Disc("NOIR kadife tabure",scenery,new Vector3(x,.73f,-36.2f),new Vector3(.48f,.11f,.48f),violet);}
   Shape("NOIR lounge baza",PrimitiveType.Cube,scenery,new Vector3(5.05f,.28f,-30.2f),new Vector3(1.2f,.56f,4.1f),walnut,true);Shape("NOIR lounge minder",PrimitiveType.Cube,scenery,new Vector3(5.05f,.61f,-30.2f),new Vector3(1.22f,.16f,4.0f),violet);Shape("NOIR lounge sırt",PrimitiveType.Cube,scenery,new Vector3(4.48f,1.02f,-30.2f),new Vector3(.15f,.85f,4.1f),violet);foreach(float z in new[]{-29f,-31.4f}){Disc("NOIR lounge sehpa",scenery,new Vector3(6.55f,.55f,z),new Vector3(.75f,.07f,.75f),stone);Rod("NOIR sehpa ayağı",scenery,new Vector3(6.55f,.04f,z),new Vector3(6.55f,.51f,z),.04f,brass);}
   for(int i=0;i<11;i++)Shape("NOIR akustik duvar paneli",PrimitiveType.Cube,scenery,new Vector3(4.18f,2.1f,-25-i*1.35f),new Vector3(.06f,2.3f,.55f),violet);
   foreach(float x in new[]{11f,17f})foreach(float y in new[]{.65f,1.65f}){var cone=Disc("NOIR hoparlör konisi",scenery,new Vector3(x,y,-38.58f),new Vector3(.57f,.04f,.57f),fabric);cone.transform.localRotation=Quaternion.Euler(90,0,0);var center=Disc("NOIR hoparlör toz kapağı",scenery,new Vector3(x,y,-38.54f),new Vector3(.20f,.035f,.20f),steel);center.transform.localRotation=Quaternion.Euler(90,0,0);}
   Shape("DJ mixer kontrol yüzeyi",PrimitiveType.Cube,scenery,new Vector3(14,1.21f,-39),new Vector3(.65f,.05f,.72f),steel);for(int i=0;i<5;i++){Shape("DJ mixer fader yolu",PrimitiveType.Cube,scenery,new Vector3(13.76f+i*.12f,1.24f,-38.9f),new Vector3(.018f,.015f,.35f),paper);Shape("DJ mixer fader düğmesi",PrimitiveType.Cube,scenery,new Vector3(13.76f+i*.12f,1.26f,-38.95f+(i%3)*.08f),new Vector3(.07f,.025f,.07f),brass);}
   InteriorLabel("S M O K E B A N K\nAFTER HOURS",scenery,new Vector3(14,2.85f,-40.82f),.045f,180);foreach(float x in new[]{5f,10f,15f,19f})Shape("NOIR tavan ışık çizgisi",PrimitiveType.Cube,scenery,new Vector3(x,3.88f,-34),new Vector3(.035f,.035f,10),neon);
   // Casino tables: low-profile printed markings, cards, and correctly sized chips.
   foreach(float x in new[]{8f,15f}){
    Disc("Casino keçe iç alan",scenery,new Vector3(x,5.20f,-33),new Vector3(3.45f,.012f,2.2f),felt);
    for(int i=0;i<5;i++){float a=(25+i*32)*Mathf.Deg2Rad;var p=new Vector3(x+Mathf.Cos(a)*1.32f,5.212f,-33+Mathf.Sin(a)*.82f);for(int side=0;side<4;side++){float yaw=side*90;var mark=Shape("Blackjack bahis çizgisi",PrimitiveType.Cube,scenery,p+new Vector3(side==1?.12f:side==3?-.12f:0,0,side==0?.16f:side==2?-.16f:0),new Vector3(side%2==0?.24f:.009f,.006f,side%2==0?.009f:.32f),brass);}}
    for(int i=0;i<4;i++){float px=x-.54f+i*.35f;var card=Shape("Casino kapalı iskambil",PrimitiveType.Cube,scenery,new Vector3(px,5.217f,-33.45f),new Vector3(.18f,.009f,.27f),paper);Shape("Casino kart arka baskı",PrimitiveType.Cube,scenery,new Vector3(px,5.223f,-33.45f),new Vector3(.15f,.004f,.24f),burgundy);}
    for(int stack=0;stack<3;stack++)for(int chip=0;chip<4;chip++){Disc("Casino ince fiş",scenery,new Vector3(x-.6f+stack*.5f,5.216f+chip*.018f,-33),new Vector3(.16f,.014f,.16f),stack==0?brass:burgundy);}
   }
   foreach(Transform t in game.world.transform)if(t.name.StartsWith("Casino fiş yığını"))foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
   foreach(Transform leg in game.world.transform)if(leg.name=="Casino masa ayağı"){leg.position=new Vector3(leg.position.x,4.625f,leg.position.z);leg.localScale=new Vector3(.4f,.425f,.4f);}
   for(int i=0;i<9;i++){float x=5+i*1.7f;Shape("Casino arka kadife panel",PrimitiveType.Cube,scenery,new Vector3(x,5.9f,-40.82f),new Vector3(1.35f,2.7f,.07f),burgundy);Shape("Casino panel pirinç kenarı",PrimitiveType.Cube,scenery,new Vector3(x-.7f,5.9f,-40.75f),new Vector3(.025f,2.75f,.025f),brass);}
   Shape("Casino lounge baza",PrimitiveType.Cube,scenery,new Vector3(5.05f,4.47f,-37.6f),new Vector3(1.25f,.54f,3.3f),walnut,true);Shape("Casino lounge minder",PrimitiveType.Cube,scenery,new Vector3(5.05f,4.8f,-37.6f),new Vector3(1.28f,.15f,3.2f),burgundy);Shape("Casino lounge sırt",PrimitiveType.Cube,scenery,new Vector3(4.48f,5.16f,-37.6f),new Vector3(.15f,.85f,3.3f),burgundy);
  }
 }
}


