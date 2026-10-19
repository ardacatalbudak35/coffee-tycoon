using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace CafeLife {
 public sealed partial class CafeUI {
  readonly Color studioBg=new Color(.065f,.061f,.052f), studioCard=new Color(.095f,.088f,.073f);
  readonly Color studioMuted=new Color(.65f,.64f,.59f), studioGold=new Color(.76f,.65f,.44f);
  readonly Color studioRule=new Color(.22f,.21f,.18f), studioGood=new Color(.58f,.69f,.53f);
  Text studioMessage;
  RectTransform aimMarker,studioFrame;
  string studioPage;
  int studioRecipe=-1;
  RectTransform StudioRoot=>studioFrame?studioFrame:menu;

  void FitStudioFrame(){
   if(!studioFrame||!menu)return;
   float factor=Mathf.Min(menu.rect.width/1280f,menu.rect.height/720f);
   studioFrame.localScale=Vector3.one*Mathf.Max(.1f,factor);
  }
  public bool IsPage(string name)=>page==name;
  public void SetStudioSection(string section){
   if(section=="craftreport"||section=="reports")section="report";
   else if(section=="barista")section="upgrade";
   else if(section=="travel")section="ranking";
   else if(section=="help")section="controls";
   focusSection=new[]{"business","supply","upgrade","report","recipes","ranking","settings","delivery","decor","achievements","controls"}.Contains(section)?section:"business";
  }

  RectTransform SBox(float x,float y,float w,float h,Color color,RectTransform parent=null){
   var root=Panel(parent?parent:StudioRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-h),new Vector2(x+w,-y),color);
   var image=root.GetComponent<Image>();image.sprite=null;image.type=Image.Type.Simple;image.raycastTarget=false;
   return root;
  }
  void SRule(float x,float y,float width)=>SBox(x,y,width,1,studioRule);
  Text SText(string content,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft,RectTransform parent=null){
   var root=Rect("Menu label",parent?parent:StudioRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y-h),new Vector2(x+w,-y));
   var text=Text(root,content,size,align);text.font=size>=20?CafeTypography.Strong:CafeTypography.Regular;
   text.lineSpacing=1f;text.color=color;var metric=text.GetComponent<CafeFontSize>();metric.color=color;
   return text;
  }
  Button SButton(string label,float x,float y,float w,float h,UnityAction action,bool primary=false,RectTransform parent=null){
    if(h>=40)h+=12; // Preserve breathing room when the accessibility text scale is enabled.
   var root=SBox(x,y,w,h,primary?studioGold:new Color(.16f,.15f,.125f),parent);
   root.name="Menu action / "+label;root.GetComponent<Image>().raycastTarget=true;
   var button=root.gameObject.AddComponent<Button>();button.targetGraphic=root.GetComponent<Image>();button.onClick.AddListener(action);
   var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.12f);
   colors.pressedColor=new Color(.76f,.76f,.72f);colors.disabledColor=new Color(.64f,.64f,.60f,.65f);colors.fadeDuration=.12f;button.colors=colors;
   var text=Text(root,label,h<38?14:16,TextAnchor.MiddleCenter);text.font=CafeTypography.Strong;text.fontStyle=FontStyle.Normal;text.lineSpacing=1f;
   text.rectTransform.offsetMin=new Vector2(12,4);text.rectTransform.offsetMax=new Vector2(-12,-4);
   text.color=primary?studioBg:ink;text.GetComponent<CafeFontSize>().color=text.color;
   return button;
  }
  Button SQuietButton(string label,float x,float y,float w,float h,UnityAction action,bool selected=false){
   var button=SButton(label,x,y,w,h,action);
   button.targetGraphic.color=selected?new Color(.16f,.15f,.12f):studioBg;
   var text=button.GetComponentInChildren<Text>();text.color=selected?studioGold:studioMuted;text.GetComponent<CafeFontSize>().color=text.color;
   return button;
  }
  void RefreshStudio(UnityAction action){action();if(game.Paused)ShowMenu("operations");}
  void StudioSelect(string section){SetStudioSection(section);ShowMenu("operations");}
  void SProgress(float x,float y,float width,float value,Color color){
   SBox(x,y,width,4,studioRule);if(value>0)SBox(x,y,width*Mathf.Clamp01(value),4,color);
  }
  void StudioSheet(){
   menu.GetComponent<Image>().color=new Color(.015f,.014f,.012f,.86f);
   SBox(43,31,1194,664,new Color(0,0,0,.20f));
   SBox(48,26,1184,664,studioBg);
   SBox(48,26,1184,1,studioRule);SBox(48,26,1,664,studioRule);SBox(1231,26,1,664,studioRule);
   SText("ARDA'S COFFEE",80,43,470,38,23,ink);
   SText("GÜN "+game.data.day+"   ·   SEVİYE "+Economy.Level(game.data),690,42,246,35,14,studioMuted,TextAnchor.MiddleRight);
   SText(game.data.money.ToString("N0")+" coin",948,42,252,35,20,studioGold,TextAnchor.MiddleRight);
   SRule(80,94,1120);
  }
  void StudioFooter(){
   SRule(80,640,1120);
   studioMessage=SText(StudioExcerpt(game.notice,105),80,649,795,31,13,studioMuted);
   SButton("OYUNA DÖN",1014,650,186,30,()=>game.Continue());
  }
  string MainStudioSection(){
   if(focusSection=="report"||focusSection=="achievements")return "business";
   if(focusSection=="delivery")return "supply";
   if(focusSection=="decor")return "upgrade";
   if(focusSection=="controls")return "settings";
   return focusSection;
  }
  void StudioNav(){
   string[] ids={"business","supply","upgrade","recipes","ranking","settings"};
   string[] labels={"İŞLETME","STOK","GELİŞİM","REÇETELER","KAFE LİGİ","AYARLAR"};
   string active=MainStudioSection();
   for(int i=0;i<ids.Length;i++){
    string id=ids[i];float x=80+i*190;bool selected=active==id;
    var button=SQuietButton(labels[i],x,106,170,42,()=>StudioSelect(id),selected);
    if(selected)SBox(x,148,170,2,studioGold);
   }
   SRule(80,151,1120);
  }
  void StudioTitle(string title,string[] ids=null,string[] labels=null){
   SText(title,80,168,470,38,25,ink);
   if(ids==null)return;
   float width=ids.Length==3?148:172,start=1200-ids.Length*width;
   for(int i=0;i<ids.Length;i++){
    string id=ids[i];bool active=focusSection==id;
    var b=SQuietButton(labels[i],start+i*width,170,width-6,34,()=>StudioSelect(id),active);
    var t=b.GetComponentInChildren<Text>();t.fontSize=14;t.GetComponent<CafeFontSize>().size=14;
   }
  }
  bool BuildStudioPage(string name){
   studioFrame=null;
   if(!new[]{"operations","home","pause","confirm","casino","ranking","arcade"}.Contains(name))return false;
   studioFrame=Rect("Studio reference frame",menu,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-640,-360),new Vector2(640,360));
   FitStudioFrame();studioPage=name;aimMarker=null;studioMessage=null;
   if(name=="operations")BuildStudioManager();
   else if(name=="home"||name=="pause")BuildStudioHome();
   else if(name=="confirm")BuildStudioConfirm();
   else if(name=="ranking"){StudioSheet();StudioTitle("Kafe ligi");BuildRanking(80,225,1120);StudioFooter();}
   else if(name=="casino")BuildStudioCasino();
   else BuildStudioArcade();
   game.GetComponent<CafeHundred>()?.Readability();
   return true;
  }
  void BuildStudioManager(){
   StudioSheet();StudioNav();
   string[] daily={"business","report","achievements"}, dailyNames={"Günlük","Rapor","Hedefler"};
   if(MainStudioSection()=="business")StudioTitle(focusSection=="report"?"Servis raporu":focusSection=="achievements"?"Hedefler":"Günlük işletme",daily,dailyNames);
   else if(MainStudioSection()=="supply")StudioTitle(focusSection=="delivery"?"Teslim tepsisi":"Stok defteri",new[]{"supply","delivery"},new[]{"Malzemeler","Teslim tepsisi"});
   else if(MainStudioSection()=="upgrade")StudioTitle(focusSection=="decor"?"İç mekân":"Kafeyi geliştir",new[]{"upgrade","decor"},new[]{"Ekipman","Dekorasyon"});
   else if(MainStudioSection()=="settings")StudioTitle(focusSection=="controls"?"Kontroller":"Ayarlar",new[]{"settings","controls"},new[]{"Oyun ayarları","Kontroller"});
   else StudioTitle(focusSection=="recipes"?"Reçete defteri":"Kafe ligi");
   switch(focusSection){
    case "business":BuildStudioBusiness();break;
    case "supply":BuildStudioSupply();break;
    case "delivery":BuildDeliveryManager();break;
    case "upgrade":BuildStudioUpgrades();break;
    case "decor":BuildDecorManager();break;
    case "report":BuildStudioReport(game.GetComponent<CafeHundred>());break;
    case "achievements":BuildStudioGoals();break;
    case "recipes":BuildStudioRecipes();break;
    case "ranking":BuildRanking(80,225,1120);break;
    case "controls":BuildStudioControls();break;
    default:BuildStudioSettings();break;
   }
   StudioFooter();
  }

  void StudioMetric(string label,string value,float x,float width){
   SText(label,x,224,width,24,13,studioMuted);
   SText(value,x,252,width,43,28,ink);
  }
  void StudioBusinessRow(string title,string detail,float y,string label,UnityAction action,bool enabled=true){
   SRule(80,y-9,720);SText(title,80,y,335,29,18,ink);SText(detail,80,y+30,490,27,14,studioMuted);
   SButton(label,608,y+7,192,40,()=>RefreshStudio(action)).interactable=enabled;
  }
  void BuildStudioBusiness(){
   var d=game.data;var x=game.expansion;var e=x.Data;
   StudioMetric("BUGÜNKÜ SERVİS",e.dayServed.ToString(),80,210);
   StudioMetric("İTİBAR","%"+Mathf.RoundToInt(d.reputation),328,210);
   StudioMetric("BUGÜNKÜ NET",(e.dayIncome-e.dayExpenses).ToString("N0"),576,224);
   SBox(304,230,1,58,studioRule);SBox(552,230,1,58,studioRule);
   StudioBusinessRow("Servis",e.open?"Kafe açık · yeni siparişler alınıyor":"Kafe kapalı · mevcut siparişler devam eder",320,e.open?"KAFEYİ KAPAT":"KAFEYİ AÇ",()=>{e.open=!e.open;game.Save();});
   StudioBusinessRow("Barista",d.staff>0?"Seviye "+d.baristaLevel+" · "+(e.staffOn?"Mesaide":"Dinleniyor"):"Henüz çalışan yok",398,d.staff>0?(e.staffOn?"MOLA VER":"MESAİYİ AÇ"):"PERSONEL",()=>{if(d.staff==0){focusSection="upgrade";return;}e.staffOn=!e.staffOn;game.Save();});
   SRule(80,467,720);SText("Bakım",80,478,210,28,18,ink);
   SText("Temizlik %"+Mathf.RoundToInt(e.cleanliness)+" · Makine kiri %"+Mathf.RoundToInt(e.machineDirt),80,508,440,28,14,studioMuted);
   SButton("KAFEYİ TEMİZLE",450,485,164,40,()=>RefreshStudio(()=>x.Action("clean_cafe")));
   SButton("MAKİNE · 5 coin",626,485,174,40,()=>RefreshStudio(()=>x.Action("clean_machine")));
   SRule(80,546,720);SText("Satış fiyatı",80,556,250,30,18,ink);SText("Standart fiyatın %"+e.price+" kadarı",80,587,440,26,14,studioMuted);
   SButton("− %10",608,565,90,40,()=>RefreshStudio(()=>x.SetPrice(-10)));
   SButton("+ %10",710,565,90,40,()=>RefreshStudio(()=>x.SetPrice(10)));
   SBox(832,224,1,392,studioRule);
   SText("GÜNÜN HEDEFİ",864,225,336,26,13,studioGold);
   SText(Mathf.Min(e.dayServed,8)+" / 8",864,264,336,58,40,ink);
   SText("Tamamlanan sipariş",864,327,336,28,15,studioMuted);
   SProgress(864,370,336,e.dayServed/8f,studioGold);
   SText("Ödül  +100 coin",864,386,336,30,18,ink);
   bool claimed=e.dailyRewardDay==d.day,ready=e.dayServed>=8;
   SButton(claimed?"ÖDÜL ALINDI":ready?"ÖDÜLÜ AL":"SERVİSLERE DEVAM ET",864,432,336,44,()=>RefreshStudio(()=>x.DailyReward()),ready&&!claimed).interactable=ready&&!claimed;
   SRule(864,506,336);SText("KAFE SEVİYESİ "+Economy.Level(d),864,525,336,27,14,studioMuted);
   SText((d.xp%100)+" / 100 TP",864,560,336,30,21,ink);SProgress(864,605,336,(d.xp%100)/100f,studioMuted);
  }

  void BuildStudioSupply(){
   var d=game.data;
   SText("MALZEME",96,220,270,28,13,studioMuted);
   SText("BAR",454,220,90,28,13,studioMuted,TextAnchor.MiddleRight);
   SText("DEPO",574,220,90,28,13,studioMuted,TextAnchor.MiddleRight);
   SRule(80,253,604);
   string[] names={"Çekirdek","Süt","Buz","Sıcak bardak","Soğuk bardak","Sıcak kapak","Soğuk kapak","Karamel şurubu","Vanilya şurubu"};
   int[] bar={d.beans,d.milk,d.ice,d.cupsHot,d.cupsCold,d.lidsHot,d.lidsCold,d.caramel,d.vanilla};
   int[] depot={d.depotBeans,d.depotMilk,d.depotIce,d.depotCupsHot,d.depotCupsCold,d.depotLidsHot,d.depotLidsCold,d.depotCaramel,d.depotVanilla};
   for(int i=0;i<names.Length;i++){
    float y=258+i*35;
    if(i==3||i==7)SRule(80,y-2,604);
    SText(names[i],96,y,326,31,16,ink);
    SText(bar[i].ToString(),454,y,90,31,16,bar[i]==0?new Color(.82f,.54f,.39f):ink,TextAnchor.MiddleRight);
    SText(depot[i].ToString(),574,y,90,31,16,studioMuted,TextAnchor.MiddleRight);
   }
   SButton("DEPODAN BARA DOLDUR",80,593,604,40,()=>RefreshStudio(()=>game.Restock()));
   SBox(716,224,1,408,studioRule);SText("TEDARİK SİPARİŞİ",750,220,450,28,13,studioGold);
   StockOrder("Çekirdek","35 adet",25,258,()=>game.GetComponent<CafeFocusedMode>().Supply("beans"));
   StockOrder("Süt ve buz","25 süt · 15 buz",20,335,()=>game.GetComponent<CafeFocusedMode>().Supply("milk"));
   StockOrder("Bardak ve kapak","Her türden 25 adet",18,412,()=>game.GetComponent<CafeFocusedMode>().Supply("packaging"));
   StockOrder("Şuruplar","10 karamel · 10 vanilya",22,489,()=>game.GetComponent<CafeFocusedMode>().Supply("syrup"));
   SQuietButton("ACİL STOK DESTEĞİ",750,590,450,40,()=>RefreshStudio(()=>game.RescueStock()));
  }
  void StockOrder(string title,string detail,int price,float y,UnityAction action){
   SText(title,750,y,270,28,18,ink);SText(detail,750,y+29,270,27,14,studioMuted);
   SButton(price+" coin  +",1040,y+5,160,42,()=>RefreshStudio(action));SRule(750,y+67,450);
  }
  void BuildDeliveryManager(){
   SText(game.shelf.Count+" / 6 alan dolu",80,219,540,29,16,studioMuted);
   SText("Müşteriye ayrılan içecekler korunur.",650,219,550,29,14,studioMuted,TextAnchor.MiddleRight);
   SRule(80,257,1120);
   for(int i=0;i<6;i++){
    float y=269+i*56;SText((i+1).ToString("00"),80,y,50,34,14,studioMuted);
    if(i>=game.shelf.Count){SText("Boş",149,y,790,34,16,studioMuted);SRule(80,y+47,1120);continue;}
    var drink=game.shelf[i];string status=drink.owner?"Sipariş #"+drink.owner.orderNumber:drink.age>90?"Bayat":drink.good?"Hazır":"Hatalı tarif";
    SText(Economy.Recipes[drink.cup.recipe].name,149,y,520,34,18,ink);
    SText(status,690,y,300,34,15,drink.owner?studioGold:studioMuted,TextAnchor.MiddleRight);
    SButton(drink.owner?"AYRILDI":"İÇECEĞİ AT",1020,y,180,36,()=>RefreshStudio(()=>game.DiscardPrepared(drink))).interactable=drink.owner==null;
    SRule(80,y+47,1120);
   }
  }

  void UpgradeRow(string title,string detail,int level,int maximum,int price,float y,string action,UnityAction purchase){
   SText(title,80,y,620,34,23,ink);SText(detail,80,y+38,680,29,15,studioMuted);
   for(int i=0;i<maximum;i++)SBox(80+i*19,y+85,13,4,i<level?studioGold:studioRule);
   SText((title=="Masa kapasitesi"?"Masa ":"Seviye ")+level+" / "+maximum,344,y+72,380,31,14,studioMuted);
   bool complete=level>=maximum;
   SText(complete?"Tamamlandı":price.ToString("N0")+" coin",880,y+5,320,30,18,complete?studioMuted:studioGold,TextAnchor.MiddleRight);
   SButton(complete?"EN YÜKSEK SEVİYE":action,880,y+48,320,42,()=>RefreshStudio(purchase),!complete).interactable=!complete;
   SRule(80,y+111,1120);
  }
  void BuildStudioUpgrades(){
   var d=game.data;
   UpgradeRow("Espresso makinesi","Hazırlama süresini kısaltır.",d.machine,5,d.machine*90,232,"MAKİNEYİ YÜKSELT",()=>game.Buy("machine"));
   int staffLevel=d.staff>0?d.baristaLevel:0;
   UpgradeRow("Barista",d.staff>0?"Eğitim, çalışanın servis hızını ve kazancını geliştirir.":"Siparişleri hazırlayacak bir çalışan işe al.",staffLevel,5,d.staff>0?650+staffLevel*1250:350,361,d.staff>0?"EĞİTİMİ YÜKSELT":"İŞE AL",()=>game.UpgradeMainBarista());
   UpgradeRow("Masa kapasitesi","Kafede kullanılabilir masa sayısı.",d.tables,12,140+d.tables*30,490,"MASA EKLE",()=>game.Buy("table"));
  }

  static string StudioExcerpt(string content,int maximum){
   if(string.IsNullOrWhiteSpace(content))return "";
   content=content.Replace("\r"," ").Replace("\n"," ").Trim();if(content.Length<=maximum)return content;
   int end=content.LastIndexOf(' ',maximum);if(end<maximum/2)end=maximum;return content.Substring(0,end).TrimEnd()+"…";
  }
  void StudioReportPair(string label,string value,float y){SText(label,80,y,320,30,16,studioMuted);SText(value,420,y,230,30,18,ink,TextAnchor.MiddleRight);}
  void BuildStudioReport(CafeHundred craft){
   var e=game.expansion.Data;var data=craft.Data;
   SText("GÜN "+game.data.day,80,223,560,28,13,studioGold);
   StudioReportPair("Gelir",e.dayIncome.ToString("N0")+" coin",266);
   StudioReportPair("Gider",e.dayExpenses.ToString("N0")+" coin",308);
   SRule(80,349,570);StudioReportPair("Net",(e.dayIncome-e.dayExpenses).ToString("N0")+" coin",360);
   SText("ÖNCEKİ GÜNLER",80,425,570,28,13,studioMuted);SRule(80,461,570);
   if(e.history.Count==0)SText("İlk günün sonunda burada görünecek.",80,480,570,34,16,studioMuted);
   for(int i=0;i<Mathf.Min(4,e.history.Count);i++){
    var day=e.history[i];float y=472+i*37;
    SText("Gün "+day.day,80,y,170,30,15,ink);SText(day.served+" servis",260,y,160,30,15,studioMuted);
    SText((day.income-day.expenses).ToString("N0")+" coin",445,y,205,30,16,ink,TextAnchor.MiddleRight);
   }
   SBox(692,224,1,395,studioRule);SText("SERVİS KALİTESİ",734,223,466,28,13,studioGold);
   int average=data.qualityCount>0?Mathf.RoundToInt(data.qualitySum/(float)data.qualityCount):0;
   SText(data.qualityCount>0?average+" / 100":"—",734,265,466,55,36,ink);SProgress(734,334,466,average/100f,studioGold);
   SText("Seri "+data.combo+"  ·  Rekor "+data.bestCombo+"  ·  Tercih "+data.preferencesMet,734,351,466,30,15,studioMuted);
   SText("İsraf "+data.wasted+" bardak  ·  Malzeme ~"+data.materialCost+" coin",734,389,466,30,15,studioMuted);
   SRule(734,437,466);SText("SON YORUM",734,455,466,26,13,studioMuted);
   string review=StudioExcerpt(e.reviews.FirstOrDefault(),140);
   SText(string.IsNullOrEmpty(review)?"Henüz müşteri yorumu yok.":review,734,494,466,96,17,ink,TextAnchor.UpperLeft);
  }
  void BuildStudioGoals(){
   var x=game.expansion;var e=x.Data;var c=game.GetComponent<CafeHundred>();
   SText("KAFE HEDEFLERİ",80,223,650,28,13,studioGold);
   string[] names={"10 servis tamamla","5 kez temizlik yap","25 müşteri ağırla","1.000 coin biriktir","Bir müdavime 5 servis yap"};
   for(int i=0;i<names.Length;i++){
    int index=i;bool claimed=(e.achievements&(1<<i))!=0,ready=x.AchievementReady(i);float y=267+i*66;
    SText(names[i],80,y,440,34,17,claimed?studioMuted:ink);
    SButton(claimed?"ALINDI":ready?"+75 coin":"DEVAM EDİYOR",530,y,202,36,()=>RefreshStudio(()=>x.ClaimAchievement(index)),ready&&!claimed).interactable=ready&&!claimed;
    SRule(80,y+51,652);
   }
   SBox(774,224,1,393,studioRule);SText("GÜNLÜK USTALIK",814,223,386,28,13,studioGold);
   SText("120 coin",814,266,386,50,34,ink);
   string[] tasks={c.Data.challengeSales+" / 10 servis",c.Data.challengeArt+" / 2 latte art",c.Data.challengeClean+" / 3 temizlik"};
   for(int i=0;i<3;i++)SText(tasks[i],814,344+i*50,386,32,18,studioMuted);
   bool done=c.Data.challengeRewardDay==game.data.day;
   SButton(done?"ÖDÜL ALINDI":c.ChallengeReady?"ÖDÜLÜ AL":"HEDEF DEVAM EDİYOR",814,535,386,44,()=>RefreshStudio(()=>c.Claim()),c.ChallengeReady&&!done).interactable=c.ChallengeReady&&!done;
  }

  static int StudioRecipeUnlock(int index)=>index==0||index==1?0:index==4?5:index==2||index==3?10:index==5||index==8?20:35;
  void BuildStudioRecipes(){
   var available=CafeFocusedMode.AvailableRecipes(game.data.served);
   if(studioRecipe<0||studioRecipe>=Economy.Recipes.Length)studioRecipe=Mathf.Clamp(game.selected,0,9);
   for(int i=0;i<10;i++){
    int selected=i;bool active=studioRecipe==i,unlocked=available.Contains(i);float y=223+i*39;
    var button=SQuietButton(Economy.Recipes[i].name,80,y,420,37,()=>{studioRecipe=selected;ShowMenu("operations");},active);
    var label=button.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleLeft;label.rectTransform.offsetMin=new Vector2(14,4);label.rectTransform.offsetMax=new Vector2(-72,-4);
    label.fontSize=15;label.GetComponent<CafeFontSize>().size=15;
    if(!unlocked)SText(StudioRecipeUnlock(i)+" servis",425,y+4,65,29,11,studioMuted,TextAnchor.MiddleRight);
    if(active)SBox(80,y+7,2,23,studioGold);
   }
   SBox(534,224,1,394,studioRule);
   var r=Economy.Recipes[studioRecipe];bool ready=available.Contains(studioRecipe);
   SText(r.cold?"SOĞUK KAHVE":"SICAK KAHVE",576,221,624,26,13,studioGold);
   SText(r.name,576,260,624,47,29,ink);
   int price=Mathf.RoundToInt((r.price+(Mathf.Max(1,game.data.espressoLevel)-1)*2)*game.expansion.Data.price/100f);
   SText(price+" coin  ·  "+(r.cold?"Soğuk bardak":"Sıcak bardak"),576,315,624,30,16,studioMuted);
   SRule(576,360,624);SText("HAZIRLAMA SIRASI",576,377,624,26,13,studioMuted);
   var steps=CafeGame.IdealSteps(r).ToArray();
   for(int i=0;i<steps.Length;i++){
    float y=415+i*35;SText((i+1).ToString("00"),576,y,46,29,13,studioGold);SText(StepName(steps[i]),630,y,570,29,18,ink);
   }
   SText(ready?(r.milk&&!r.cold?"Sütü köpürt · İstersen latte art ile bitir.":"Hazır kahveye kapağı tak ve servis et."):"Toplam "+StudioRecipeUnlock(studioRecipe)+" serviste açılır.",576,600,624,28,14,ready?studioMuted:studioGold);
  }

  void StudioSetting(string label,string value,float x,float y,UnityAction change){
   SText(label,x,y,310,31,17,ink);SButton(value,x+320,y,210,38,()=>RefreshStudio(change));SRule(x,y+58,530);
  }
  void BuildStudioSettings(){
   var d=game.data;var e=game.expansion.Data;var h=game.GetComponent<CafeHundred>();
   SText("SES",80,223,530,29,13,studioGold);SText("GÖRÜNTÜ VE ERİŞİLEBİLİRLİK",670,223,530,29,13,studioGold);
   StudioSetting("Ana ses","%"+Mathf.RoundToInt(d.volume*100),80,272,()=>{d.volume=d.volume>=.99f?0:Mathf.Min(1,d.volume+.25f);AudioListener.volume=d.volume;game.Save();});
   StudioSetting("Müzik","%"+Mathf.RoundToInt(e.music*100),80,350,()=>{e.music=e.music>=.99f?0:Mathf.Min(1,e.music+.25f);game.ApplyAudio();game.Save();});
   StudioSetting("Efektler","%"+Mathf.RoundToInt(e.effects*100),80,428,()=>{e.effects=e.effects>=.99f?0:Mathf.Min(1,e.effects+.25f);game.ApplyAudio();game.Save();});
   StudioSetting("Bakış hassasiyeti",d.sensitivity.ToString("0.00"),80,506,()=>{d.sensitivity=d.sensitivity>=.199f?.05f:Mathf.Min(.2f,d.sensitivity+.05f);game.Save();});
   StudioSetting("Yazı boyutu",d.hundred.largeText?"BÜYÜK":"STANDART",670,272,()=>{d.hundred.largeText=!d.hundred.largeText;game.Save();});
   StudioSetting("Kamera hareketi",d.hundred.reduceMotion?"AZALTILMIŞ":"NORMAL",670,350,()=>{d.hundred.reduceMotion=!d.hundred.reduceMotion;game.Save();});
   StudioSetting("Görüş açısı",e.fov.ToString("0")+"°",670,428,()=>{e.fov=e.fov>=95?60:Mathf.Min(95,e.fov+5);game.player.view.fieldOfView=e.fov;game.Save();});
   string[] names=QualitySettings.names;int level=Mathf.Clamp(QualitySettings.GetQualityLevel(),0,names.Length-1);
   StudioSetting("Grafik kalitesi",names[level],670,506,()=>{d.quality=(level+1)%names.Length;QualitySettings.SetQualityLevel(d.quality);h.ApplySettings();game.Save();});
   SText("Değerleri değiştirmek için seç. Ayarlar otomatik kaydedilir.",80,599,1120,30,14,studioMuted);
  }
  void BuildStudioControls(){
   SText("KLAVYE VE FARE",80,223,530,29,13,studioGold);
   string[] keys={"W A S D","FARE","E","TAB","R","Q","ESC"};
   string[] meanings={"Yürü","Etrafa bak","Etkileş / al / bırak","İşletme defteri","Reçete defteri","İmleç / kamera","Duraklat / geri dön"};
   for(int i=0;i<keys.Length;i++){float y=267+i*47;SBox(80,y,130,33,studioCard);SText(keys[i],90,y,110,33,14,studioGold,TextAnchor.MiddleCenter);SText(meanings[i],232,y,378,33,17,ink);}
   SBox(642,224,1,392,studioRule);SText("DOKUNMATİK",684,223,516,29,13,studioGold);
   SText("Sol çubuk",684,277,516,31,20,ink);SText("Yürümek için sürükle.",684,314,516,38,16,studioMuted);
   SText("Ekranın sağ tarafı",684,381,516,31,20,ink);SText("Bakmak için boş alanda sürükle.",684,418,516,38,16,studioMuted);
   SText("Etkileş düğmesi",684,485,516,31,20,ink);SText("Baktığın eşyayı kullan veya eline al.",684,522,516,38,16,studioMuted);
  }

  void BuildRanking(float x,float y,float width){
   var rows=new List<KeyValuePair<string,int>>{
    new KeyValuePair<string,int>("Velvet Roastery",4200),new KeyValuePair<string,int>("Moka Atelier",3100),new KeyValuePair<string,int>("Corner & Co.",2200),
    new KeyValuePair<string,int>("Juniper Café",1550),new KeyValuePair<string,int>("Daily Grind",950),new KeyValuePair<string,int>("Arda's Coffee",CafeStreetActivities.CafeScore(game))};
   rows=rows.OrderByDescending(r=>r.Value).ToList();
   SText("SIRA",x,y,width*.1f,28,13,studioMuted);SText("KAFE",x+110,y,width-410,28,13,studioMuted);SText("PUAN",x+width-230,y,230,28,13,studioMuted,TextAnchor.MiddleRight);SRule(x,y+35,width);
   for(int i=0;i<rows.Count;i++){
    bool own=rows[i].Key=="Arda's Coffee";float py=y+47+i*49;
    if(own){SBox(x-12,py-2,width+24,44,studioCard);SBox(x-12,py-2,2,44,studioGold);}
    SText((i+1).ToString("00"),x,py,80,36,19,own?studioGold:studioMuted);
    SText(rows[i].Key,x+110,py,width-410,36,20,own?studioGold:ink);
    SText(rows[i].Value.ToString("N0"),x+width-230,py,230,36,20,ink,TextAnchor.MiddleRight);
   }
   SText("Yerel kafe ligi · İyi servis, itibar ve yatırımlar puanını artırır.",x,y+365,width,38,14,studioMuted);
  }
  void BuildStudioHome(){
   StudioSheet();
   SText(studioPage=="pause"?"Oyun duraklatıldı":"Coffee Tycoon",80,144,650,66,44,ink);
   SText("Gün "+game.data.day+"   ·   "+game.data.served+" servis   ·   İtibar %"+Mathf.RoundToInt(game.data.reputation),80,226,650,35,18,studioMuted);
   SButton("DEVAM ET",80,325,420,58,()=>game.Continue(),true);
   SButton("İŞLETME DEFTERİ",80,400,420,48,()=>game.OpenOperations());
   SQuietButton("AYARLAR",80,465,198,43,()=>game.OpenOperations("settings"));
   SQuietButton("YENİ OYUN",298,465,202,43,()=>ShowMenu("confirm"));
   SRule(80,553,420);SText("WASD  Yürü    E  Etkileş    TAB  İşletme",80,573,600,34,14,studioMuted);
   DrawStudioCup(815,252,210,game.data.interiorStyle);
   SText("ARDA'S",730,508,390,45,31,ink,TextAnchor.MiddleCenter);SText("C O F F E E",730,555,390,30,15,studioGold,TextAnchor.MiddleCenter);
  }
  void DrawStudioCup(float x,float y,float size,int style){
   Color ceramic=new Color(.77f,.74f,.66f),coffee=new Color(.19f,.12f,.07f),seat=CafeDecorProgression.Swatch(style,3);
   SBox(x+size*.04f,y+size*.81f,size*.95f,size*.035f,studioRule);
   SBox(x+size*.10f,y+size*.72f,size*.82f,size*.025f,studioMuted);
   SBox(x+size*.15f,y+size*.20f,size*.60f,size*.49f,ceramic);
   SBox(x+size*.19f,y+size*.66f,size*.52f,size*.04f,ceramic);
   SBox(x+size*.19f,y+size*.23f,size*.52f,size*.045f,coffee);
   SBox(x+size*.75f,y+size*.29f,size*.15f,size*.05f,ceramic);
   SBox(x+size*.85f,y+size*.32f,size*.05f,size*.23f,ceramic);
   SBox(x+size*.75f,y+size*.53f,size*.15f,size*.05f,ceramic);
   SBox(x+size*.39f,y+size*.39f,size*.12f,size*.12f,seat);
  }
  void BuildStudioConfirm(){
   StudioSheet();SText("Yeni oyun",80,151,1050,55,34,ink);
   SText("Kafeye ilk günden başla.",80,236,1050,39,22,ink);
   SText("Mevcut kayıt yeni oyuna başlamadan önce yedeklenir.",80,291,1050,44,17,studioMuted);
   SButton("YENİ OYUNU BAŞLAT",80,418,430,54,()=>game.NewGame(),true);
   SButton("VAZGEÇ",534,418,300,54,()=>ShowMenu("home"));
  }

  // Compatibility screens remain available for older callers; the world activities use their own controls.
  void StudioChrome(string eyebrow,string title,string subtitle){
   StudioSheet();SText(eyebrow,80,112,1120,27,13,studioGold);SText(title,80,147,1120,47,30,ink);
   SText(subtitle,80,198,1120,38,16,studioMuted);StudioFooter();
  }
  void SCard(string label,string value,string detail,float x,float y,float width=300){
   SText(label,x,y,width,26,14,studioMuted);SText(value,x,y+32,width,46,30,ink);SText(detail,x,y+84,width,29,15,studioMuted);
  }
  void BuildStudioCasino(){
   var casino=game.GetComponent<CafeCasino>();if(!casino)casino=game.gameObject.AddComponent<CafeCasino>();
   StudioChrome("NOIR · ÜST KAT","Blackjack","As 1 veya 11 · Krupiye 17'de durur · Doğal 21, 3:2 öder");
   SText("KRUPİYE",80,246,1120,27,13,studioMuted);CardHand(casino.dealer,80,281,casino.Active);
   SText("SEN · "+(casino.player.Count>0?CafeCasino.Score(casino.player).ToString():"—"),80,399,1120,27,13,studioGold);CardHand(casino.player,80,434,false);
   SText(casino.Result,80,551,1120,35,19,ink);
   if(casino.Active){SButton("KART ÇEK",80,594,250,36,()=>game.BlackjackHit(),true);SButton("DUR",350,594,250,36,()=>game.BlackjackStand());}
   else {int[] bets={10,50,200};for(int i=0;i<3;i++){int bet=bets[i];SButton(bet+" coin",80+i*270,594,250,36,()=>game.StartBlackjack(bet),i==0);}}
  }
  void CardHand(List<int> cards,float x,float y,bool hidden){
   if(cards.Count==0){SText("Bahis seçerek yeni ele başla.",x,y,1000,70,18,studioMuted);return;}
   for(int i=0;i<cards.Count;i++){
    bool back=hidden&&i>0;float left=x+i*Mathf.Min(91,1100f/Mathf.Max(1,cards.Count));
    SBox(left,y,78,102,back?new Color(.19f,.23f,.19f):new Color(.92f,.89f,.82f));
    SText(back?"?":CafeCasino.Label(cards[i]),left+8,y+8,62,86,26,back?studioGold:cards[i]/13==1||cards[i]/13==2?new Color(.7f,.15f,.2f):studioBg,TextAnchor.MiddleCenter);
   }
  }
  void BuildStudioArcade(){
   var a=game.GetComponent<CafeStreetActivities>();StudioChrome("CADDE","Basket","5 atış · Yeşil alanda at");
   SCard("ATIŞ",a.shots+" / 5","",80,260,330);SCard("İSABET",a.hits.ToString(),"",470,260,330);SCard("REKOR",game.data.hundred.basketBest+" / 5","",870,260,330);
   SBox(80,424,1120,54,studioCard);SBox(80+1120*.39f,424,1120*.22f,54,new Color(.22f,.35f,.25f));aimMarker=SBox(80,419,6,64,studioGold);
   SText(a.message,80,500,1120,50,22,ink);SButton(a.playing?"ATIŞ YAP":"YENİ TUR",80,574,450,48,()=>{if(a.playing)a.Shoot();else a.Begin();},true);
  }
  void UpdateStudio(){FitStudioFrame();if(menu&&menu.gameObject.activeSelf&&studioPage=="arcade"&&aimMarker){var a=game.GetComponent<CafeStreetActivities>();aimMarker.anchoredPosition=new Vector2(80+1114*a.Aim,-451);}}
 }
}
