using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public sealed partial class CafeUI {
  readonly CafeCustomer[] cardCustomers=new CafeCustomer[2];
  RectTransform orderHud;
  Text orderCount;
  readonly Text[] orderNames=new Text[2],orderMeta=new Text[2],orderDetails=new Text[2],orderStatus=new Text[2];
  readonly Image[] patienceBars=new Image[2],orderAccents=new Image[2];
  readonly GameObject[] orderRows=new GameObject[2];
  static readonly Color orderInk=new Color(.95f,.93f,.86f),orderMuted=new Color(.69f,.75f,.68f),orderBrass=new Color(.81f,.70f,.49f);

  void OrderTextColor(Text text,Color color){text.color=game&&game.data!=null&&game.data.hundred!=null&&game.data.hundred.contrast?Color.white:color;text.GetComponent<CafeFontSize>().color=color;}
  void BuildOrderHud(){
   orderHud=Panel(safe,new Vector2(1,1),new Vector2(1,1),new Vector2(-340,-376),new Vector2(-20,-28),new Color(.045f,.065f,.055f,.95f));
   orderHud.name="Order cards HUD";
   var heading=Label(orderHud,new Vector2(0,1),Vector2.one,new Vector2(18,-43),new Vector2(-120,-10),17,TextAnchor.MiddleLeft);
   heading.text="Siparişler";heading.font=CafeTypography.Strong;OrderTextColor(heading,orderInk);
   orderCount=Label(orderHud,new Vector2(0,1),Vector2.one,new Vector2(190,-43),new Vector2(-18,-10),12,TextAnchor.MiddleRight);OrderTextColor(orderCount,orderMuted);
   var divider=Panel(orderHud,new Vector2(0,1),Vector2.one,new Vector2(18,-47),new Vector2(-18,-46),new Color(.25f,.31f,.25f));divider.GetComponent<Image>().raycastTarget=false;
   for(int i=0;i<2;i++){
    float y=-53-i*144;
    var card=Panel(orderHud,new Vector2(0,1),new Vector2(1,1),new Vector2(10,y-136),new Vector2(-10,y),new Color(.10f,.135f,.11f));
    orderRows[i]=card.gameObject;int slot=i;
    var choose=card.gameObject.AddComponent<Button>();choose.targetGraphic=card.GetComponent<Image>();
    var colors=choose.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.08f);colors.pressedColor=new Color(.85f,.90f,.84f);colors.fadeDuration=.12f;choose.colors=colors;
    choose.onClick.AddListener(()=>{var c=cardCustomers[slot];if(c)game.GetComponent<CafeFocusedMode>().SelectOrder(c);});
    var accent=Panel(card,Vector2.zero,new Vector2(0,1),new Vector2(0,14),new Vector2(3,-14),orderBrass);orderAccents[i]=accent.GetComponent<Image>();orderAccents[i].raycastTarget=false;
    orderMeta[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-29),new Vector2(-118,-8),12,TextAnchor.MiddleLeft);OrderTextColor(orderMeta[i],orderMuted);
    orderStatus[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(158,-29),new Vector2(-15,-8),12,TextAnchor.MiddleRight);OrderTextColor(orderStatus[i],orderMuted);
    orderNames[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-67),new Vector2(-15,-31),20,TextAnchor.MiddleLeft);orderNames[i].font=CafeTypography.Strong;orderNames[i].fontStyle=FontStyle.Normal;OrderTextColor(orderNames[i],orderInk);
    orderDetails[i]=Label(card,new Vector2(0,1),Vector2.one,new Vector2(15,-118),new Vector2(-15,-68),14,TextAnchor.MiddleLeft);OrderTextColor(orderDetails[i],orderMuted);
    var track=Panel(card,Vector2.zero,new Vector2(1,0),new Vector2(15,9),new Vector2(-15,11),new Color(.22f,.28f,.22f));track.GetComponent<Image>().raycastTarget=false;
    var fill=Panel(track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.58f,.74f,.60f));patienceBars[i]=fill.GetComponent<Image>();patienceBars[i].type=Image.Type.Filled;patienceBars[i].fillMethod=Image.FillMethod.Horizontal;patienceBars[i].raycastTarget=false;
   }
   orderHud.SetSiblingIndex(menu.GetSiblingIndex());
  }
  void UpdateOrderHud(){
   if(!orderHud)return;
   orderHud.gameObject.SetActive(!game.player.WorldInputBlocked&&game.player.transform.position.z>-11);
   var waiting=game.customers.Where(c=>c&&c.state==CafeCustomer.State.Waiting).OrderBy(c=>c.patience).ToArray();
   var lower=orderHud.offsetMin;lower.y=waiting.Length>=2?-376:-232;orderHud.offsetMin=lower;
   orderCount.text=waiting.Length==0?"TEZGÂH HAZIR":waiting.Length+" bekleyen";
   var mode=game.GetComponent<CafeFocusedMode>();
   for(int i=0;i<2;i++){
    bool present=i<waiting.Length;cardCustomers[i]=present?waiting[i]:null;orderRows[i].SetActive(present||i==0);
    if(!present){
     orderMeta[i].text="SERVİSE HAZIR";orderStatus[i].text="";orderNames[i].text="Yeni sipariş bekleniyor";orderDetails[i].text="Misafirin siparişi burada görünecek.";
     orderRows[i].GetComponent<Image>().color=new Color(.085f,.115f,.095f);orderAccents[i].enabled=false;patienceBars[i].fillAmount=0;continue;
    }
    var c=waiting[i];bool chosen=mode&&mode.selectedOrder==c;bool preparing=game.cup!=null&&game.cup.recipe==c.recipe;
    orderMeta[i].text=c.accepted?"SİPARİŞ #"+c.orderNumber:"YENİ SİPARİŞ";
    orderStatus[i].text=(chosen?"SEÇİLİ · ":"")+Mathf.CeilToInt(c.waitSeconds)+" sn";
    OrderTextColor(orderStatus[i],chosen?orderBrass:orderMuted);
    orderNames[i].text=Economy.Recipes[c.recipe].name;
    orderRows[i].GetComponent<Image>().color=chosen?new Color(.16f,.205f,.155f):new Color(.10f,.135f,.11f);
    orderAccents[i].enabled=chosen;
    orderDetails[i].text=CafeService.PreferenceLabel(c)+"\n"+(preparing?"Sıradaki: "+game.NextStep():chosen?(Economy.Recipes[c.recipe].cold?"Sıradaki: Soğuk bardak rafı":"Sıradaki: Sıcak bardak rafı"):(Application.isMobilePlatform?"Siparişi seçmek için dokun":game.player.PointerMode?"Siparişi seçmek için tıkla":"Q · İmleci aç ve sipariş seç"));
    patienceBars[i].fillAmount=Mathf.Clamp01(c.patience/100);
    patienceBars[i].color=c.patience<25?new Color(.90f,.53f,.38f):new Color(.58f,.74f,.60f);
   }
  }
 }
}
