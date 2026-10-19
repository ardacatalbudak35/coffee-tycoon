using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeWorld {
  void DecorateBoulevard(){Color gold=new Color(.65f,.45f,.23f),ivory=new Color(.8f,.78f,.7f),dark=new Color(.07f,.08f,.1f);
   var violet=ColorMaterial(new Color(.55f,.12f,.8f));violet.EnableKeyword("_EMISSION");violet.SetColor("_EmissionColor",new Color(1.1f,.05f,2));
   foreach(float x in new[]{4.25f,11.8f,16.2f,23.75f}){var strip=Block("NOIR cephe neon çizgisi",new Vector3(x,4,-22.82f),new Vector3(.05f,7.7f,.05f),dark);strip.GetComponent<Renderer>().sharedMaterial=violet;}
   var top=Block("NOIR saçak ışık bandı",new Vector3(14,8,-22.8f),new Vector3(20,.08f,.1f),dark);top.GetComponent<Renderer>().sharedMaterial=violet;
   foreach(float x in new[]{7f,20.5f}){Block("NOIR üst kat vitrin çerçevesi",new Vector3(x,5.8f,-22.8f),new Vector3(4.8f,2.3f,.15f),gold);Block("NOIR üst kat vitrin camı",new Vector3(x,5.8f,-22.68f),new Vector3(4.6f,2.1f,.06f),new Color(.1f,.16f,.23f));for(int i=0;i<3;i++)Block("Vitrin dikey kayıt",new Vector3(x-1.5f+i*1.5f,5.8f,-22.61f),new Vector3(.035f,2.1f,.04f),gold);}
   for(int i=0;i<6;i++){float x=5+i*.9f;Block("NOIR zemin cephe yivi",new Vector3(x,1.3f,-22.8f),new Vector3(.045f,2.4f,.1f),gold);}
   Block("Market saçak",new Vector3(-14,3.3f,-22.5f),new Vector3(12,.18f,1.2f),new Color(.22f,.4f,.33f));
   foreach(float x in new[]{-18f,-10f}){Block("Market vitrin çerçevesi",new Vector3(x,1.7f,-22.85f),new Vector3(3,2.3f,.1f),ivory);Block("Market vitrin",new Vector3(x,1.7f,-22.75f),new Vector3(2.8f,2.1f,.06f),new Color(.22f,.34f,.3f));}
   for(int i=0;i<18;i++){float x=-33+i*4;Block("Karşı kaldırım derzi",new Vector3(x,.008f,-22),new Vector3(.025f,.02f,2),dark);Block("Kafe kaldırım derzi",new Vector3(x,.008f,-13),new Vector3(.025f,.02f,4),dark);}
   foreach(float z in new[]{-15f,-21f})Block("Cadde bordür hattı",new Vector3(0,-.025f,z),new Vector3(70,.05f,.12f),ivory);
   foreach(float x in new[]{-5f,28f}){Model("Flower_Pot",new Vector3(x,0,-22),1.15f,0,false);Model("Flower_Pot",new Vector3(x,0,-13.5f),1.15f,0,false);}
   for(int i=0;i<3;i++){float x=7+i*7;var light=new GameObject("NOIR vitrin ışığı");light.transform.SetParent(transform,false);light.transform.position=new Vector3(x,3.5f,-21);var l=light.AddComponent<Light>();l.type=LightType.Point;l.range=8;l.intensity=8;l.color=i==1?new Color(.8f,.4f,1):new Color(1,.65f,.4f);}
   var marketLight=new GameObject("Market vitrin ışığı");marketLight.transform.SetParent(transform,false);marketLight.transform.position=new Vector3(-14,3,-24);var ml=marketLight.AddComponent<Light>();ml.type=LightType.Point;ml.range=10;ml.intensity=9;ml.color=new Color(.8f,1,.85f);
   foreach(float x in new[]{8f,15f}){Block("Casino halı alanı",new Vector3(x,4.225f,-33),new Vector3(5,.025f,5),new Color(.22f,.04f,.09f));for(int j=0;j<3;j++){var card=Block("Casino açık masa kartı",new Vector3(x-.4f+j*.3f,5.14f,-32.8f),new Vector3(.22f,.018f,.34f),ivory);card.transform.rotation=Quaternion.Euler(0,j*8,0);}for(int j=0;j<4;j++)Round("İstifli casino fişi",new Vector3(x+.8f,5.16f+j*.035f,-33),new Vector3(.18f,.025f,.18f),j%2==0?gold:new Color(.7f,.1f,.16f));}
   for(int i=0;i<4;i++)Block("Casino duvar altın şeridi",new Vector3(4.17f+i*5,6,-40.8f),new Vector3(.04f,3.2f,.08f),gold);
   var old=transform.Find("Basket çemberi");if(old)old.gameObject.SetActive(false);
   for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;var net=new GameObject("Basket filesi");net.transform.SetParent(transform,false);var n=net.AddComponent<LineRenderer>();n.useWorldSpace=true;n.positionCount=2;n.startWidth=n.endWidth=.009f;n.sharedMaterial=ColorMaterial(ivory);n.SetPosition(0,new Vector3(-28+Mathf.Cos(a)*.265f,3.04f,-35.5f+Mathf.Sin(a)*.265f));n.SetPosition(1,new Vector3(-28+Mathf.Cos(a)*.17f,2.55f,-35.5f+Mathf.Sin(a)*.17f));}
   foreach(float x in new[]{-32.8f,-23.2f})Block("Basket saha yan çizgisi",new Vector3(x,.045f,-30),new Vector3(.06f,.025f,13.5f),ivory);
  }
 }
}