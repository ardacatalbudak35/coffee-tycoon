using UnityEngine;
namespace CafeLife {
 public sealed partial class CafePolish {
  void NaturalCafeDetails(){
   foreach(var label in scenery.GetComponentsInChildren<TextMesh>(true)){
    bool packaging=label.text.Contains("SYRUP /")||label.text=="A\nCOFFEE";
    if(!packaging&&(label.name.StartsWith("Atölye etiketi")||label.name=="İstasyon etiketi"||label.name=="Kafe sanat yazısı"||label.name=="İç cephe marka tabelası"))label.gameObject.SetActive(false);
   }
   foreach(Transform part in scenery)if(part.name=="Duvar sanat çerçevesi"||part.name=="Duvar sanat zemini"||part.name=="Arşiv baskı kağıdı"||part.name=="Baskı geometrik güneş"||part.name.StartsWith("Askı menü"))part.gameObject.SetActive(false);
   var photo=Resources.Load<Texture2D>("CafeArt/CoffeePhotography");if(!photo)throw new System.InvalidOperationException("Cafe photography atlas missing");
   for(int i=0;i<3;i++){
    float x=-5.7f+i*5.7f;var frame=InteriorGroup("Fotoğraf çerçevesi "+i,new Vector3(x,2.17f,8.73f));
    Shape("İnce ceviz çerçeve",PrimitiveType.Cube,frame,Vector3.zero,new Vector3(1.38f,1.78f,.045f),walnut);
    Shape("Fotoğraf krem paspartu",PrimitiveType.Cube,frame,new Vector3(0,0,-.026f),new Vector3(1.30f,1.70f,.012f),paper);
    var print=Surface("Mat fotoğraf baskısı "+i,Color.white,.12f,0,photo);print.SetTextureScale("_BaseMap",new Vector2(1f/3,1));print.SetTextureOffset("_BaseMap",new Vector2(i/3f,0));
    var image=Shape("Gerçekçi kahve fotoğrafı",PrimitiveType.Quad,frame,new Vector3(0,0,-.035f),new Vector3(1.18f,1.57f,1),print);image.transform.localRotation=Quaternion.identity;
   }
  }
 }
}
