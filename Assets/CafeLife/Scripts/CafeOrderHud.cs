using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public sealed partial class CafeUI {
  readonly CafeCustomer[] cardCustomers=new CafeCustomer[2];
  RectTransform orderHud;Text orderCount;readonly Text[] orderNames=new Text[2],orderMeta=new Text[2],orderDetails=new Text[2];readonly Image[] patienceBars=new Image[2];readonly GameObject[] orderRows=new GameObject[2];
  void BuildOrderHud(){
   orderHud=Panel(safe,new Vector2(1,1),new Vector2(1,1),new Vector2(-340,-376),new Vector2(-20,-28),new Color(.055f,.065f,.073f,.95f));orderHud.name="Order cards HUD";
   var heading=Label(orderHud,new Vector2(0,1),Vector2.one,new Vector2(18,-45),new Vector2(-18,-12),16,TextAnchor.MiddleLeft);heading.text="Siparişler";heading.font=CafeTypography.Strong;heading.color=new Color(.91f,.75f,.47f);
   orderCount=Label(orderHud,new Vector2(0,1),Vector2.one,new Vector2(190,-45),new Vector2(-18,-12),13,TextAnchor.MiddleRight);
   for(int i=0;i<2;i++){float y=-53-i*144;var card=Panel(orderHud,new Vector2(0,1),new Vector2(1,1),new Vector2(10,y-136),new Vector2(-10,y),new Color(.10f,.12f,.13f,1));orderRows[i]=card.gameObject;int slot=i;var choose=card.gameObject.AddComponent<Button>();choose.targetGraphic=card.GetComponent<Image>();choose.onClick.AddListener(()=>{var c=cardCustomers[slot];if(c)game.GetComponent<CafeFocusedMode>().SelectOrder(c);});
    orderMeta[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-30),new Vector2(-15,-8),13,TextAnchor.MiddleLeft);orderMeta[i].color=new Color(.74f,.77f,.74f);
    orderNames[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-67),new Vector2(-15,-31),20,TextAnchor.MiddleLeft);orderNames[i].font=CafeTypography.Strong;orderNames[i].fontStyle=FontStyle.Normal;
    orderDetails[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-118),new Vector2(-15,-68),14,TextAnchor.MiddleLeft);orderDetails[i].color=new Color(.77f,.81f,.77f);
    var track=Panel(card,Vector2.zero,new Vector2(1,0),new Vector2(13,9),new Vector2(-13,12),new Color(.2f,.23f,.24f));var fill=Panel(track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.57f,.72f,.59f));patienceBars[i]=fill.GetComponent<Image>();patienceBars[i].type=Image.Type.Filled;patienceBars[i].fillMethod=Image.FillMethod.Horizontal;
   }orderHud.SetSiblingIndex(menu.GetSiblingIndex());
  }
  void UpdateOrderHud(){if(!orderHud)return;orderHud.gameObject.SetActive(!game.player.WorldInputBlocked&&game.player.transform.position.z>-11);var waiting=game.customers.Where(c=>c&&c.state==CafeCustomer.State.Waiting).OrderBy(c=>c.patience).ToArray();var lower=orderHud.offsetMin;lower.y=waiting.Length>=2?-376:-232;orderHud.offsetMin=lower;orderCount.text=waiting.Length+" bekleyen";for(int i=0;i<2;i++){bool present=i<waiting.Length;cardCustomers[i]=present?waiting[i]:null;orderRows[i].SetActive(present||i==0);if(!present){orderMeta[i].text="Servise hazır";orderNames[i].text="Yeni sipariş bekleniyor";orderDetails[i].text="Müşteri geldiğinde burada görünecek.";patienceBars[i].fillAmount=0;continue;}var c=waiting[i];orderMeta[i].text=(c.accepted?"Sipariş #"+c.orderNumber:"Yeni müşteri")+"   ·   "+Mathf.CeilToInt(c.waitSeconds)+" sn";orderNames[i].text=Economy.Recipes[c.recipe].name;var mode=game.GetComponent<CafeFocusedMode>();bool chosen=mode&&mode.selectedOrder==c;bool preparing=game.cup!=null&&game.cup.recipe==c.recipe;orderRows[i].GetComponent<Image>().color=chosen?new Color(.18f,.17f,.13f,1):new Color(.10f,.12f,.13f,1);orderDetails[i].text=CafeService.PreferenceLabel(c)+"\n"+(preparing?"Sıradaki: "+game.NextStep():chosen?(Economy.Recipes[c.recipe].cold?"Sıradaki: Soğuk bardak rafı":"Sıradaki: Sıcak bardak rafı"):(Application.isMobilePlatform?"Dokun: siparişi seç":game.player.PointerMode?"Tıkla: siparişi seç":"Q: imleç · siparişi seç"));patienceBars[i].fillAmount=Mathf.Clamp01(c.patience/100);patienceBars[i].color=c.patience<25?new Color(.88f,.40f,.29f):new Color(.57f,.72f,.59f);}}
 }
}


