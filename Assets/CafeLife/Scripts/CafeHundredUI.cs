using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeUI {
  void BuildBarista(){var h=game.GetComponent<CafeHundred>();var d=h.Data;
   Op("Doz: "+d.dose+" g · 14–22 g",0,false,()=>{d.dose=d.dose>=22?14:d.dose+1;game.Save();});
   Op("Öğütme: "+d.grind+" / 9 · ince → kalın",1,false,()=>{d.grind=d.grind>=9?1:d.grind+1;game.Save();});
   Op("Su: "+d.temperature+"°C · 88–98°C",2,false,()=>{d.temperature=d.temperature>=98?88:d.temperature+1;game.Save();});
   Op("Kavrum: "+new[]{"Açık","Orta","Koyu"}[d.roast],3,false,()=>{d.roast=(d.roast+1)%3;game.Save();});
   Op("Otomatik çekirdek: "+(d.autoSupply?"Açık":"Kapalı"),4,false,()=>{d.autoSupply=!d.autoSupply;game.Save();});
   Op("Günlük ustalık ödülü · +120",5,false,()=>h.Claim());
   Op("Harita ölçeği: "+d.mapZoom.ToString("0.0"),0,true,()=>{d.mapZoom=d.mapZoom>=1.9f?.7f:d.mapZoom+.3f;game.Save();});
   Op("Yürüme: "+d.walkSpeed.ToString("0.0")+" m/sn",1,true,()=>{d.walkSpeed=d.walkSpeed>=4.9f?2.5f:d.walkSpeed+.5f;game.Save();});
   Op("Kamera sallanması: "+(d.reduceMotion?"Kapalı":"Açık"),2,true,()=>{d.reduceMotion=!d.reduceMotion;game.Save();});
   Op("Büyük yazı: "+(d.largeText?"Açık":"Kapalı"),3,true,()=>{d.largeText=!d.largeText;game.Save();});
   Op("Yüksek kontrast: "+(d.contrast?"Açık":"Kapalı"),4,true,()=>{d.contrast=!d.contrast;game.Save();});
   Op("Görüş mesafesi: "+d.drawDistance+" m",5,true,()=>{d.drawDistance=d.drawDistance>=180?70:Mathf.Min(180,d.drawDistance+25);h.ApplySettings();game.Save();});
   OperationInfo("Espresso: 1:2 oranını hedefle. 18 g kahve → 36 g çıktı, yaklaşık 25–30 saniye. Açık kavrum daha sıcak su ister.\nOtomatik tedarik: çekirdek 5'in altındayken, depoda 15'ten az ve kasada en az 50 coin varsa 25 coinlik paket alır.");
  }
  void BuildHundredReport(){var h=game.GetComponent<CafeHundred>();BodyText(h.Summary(),false);string recipes="TARİF EKONOMİSİ · maliyet → brüt kazanç\n\n";for(int i=0;i<10;i++){float cost=CafeHundred.UnitCost(i);recipes+=Economy.Recipes[i].name+" · "+cost.ToString("0.0")+" → "+(Economy.Recipes[i].price*game.expansion.Data.price/100f-cost).ToString("0.0")+" · Lv."+h.MasteryLevel(i)+" · Stok "+h.Capacity(i)+"\n";}BodyText(recipes,true);OperationInfo("Malzeme tahmini kasadan ikinci kez düşülmez; stok alımında ödeme yapılır. Günlük hedef: 10 servis + 2 art + 3 temizlik.");}
 }
}
