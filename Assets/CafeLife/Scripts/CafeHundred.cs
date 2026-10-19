using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CafeLife {
 public sealed class CafeFontSize:MonoBehaviour {public int size;public Color color;}
 [Serializable] public sealed class CafeHundredData {
  public float dose=18,grind=5,temperature=93,walkSpeed=3.4f,drawDistance=140;
  public int roast=1,combo,bestCombo,wasted,materialCost,challengeDay,challengeSales,challengeArt,challengeClean,challengeRewardDay;
  public int qualitySum,qualityCount,preferencesMet,basketBest,streetRewardDay;public int[] mastery=new int[10];
  public int[] sales=new int[10]; public bool minimap=true,autoSupply,reduceMotion,largeText,contrast; public float mapZoom=1;
 }
 public sealed class CafeHundred:MonoBehaviour {
  public CafeGame game; public CafeHundredData Data=>game.data.hundred;
  public readonly List<string> history=new List<string>();float supplyClock,refresh;CafeVenue[] venues=new CafeVenue[0];Texture2D pixel;GUIStyle small;Vector3 lastPosition;GameObject beacon;TextMesh beaconLabel;TextMesh[] signs;
  public static void Normalize(CafeHundredData d){d.dose=Mathf.Clamp(d.dose,14,22);d.grind=Mathf.Clamp(d.grind,1,9);d.temperature=Mathf.Clamp(d.temperature,88,98);d.walkSpeed=Mathf.Clamp(d.walkSpeed,2.5f,5);d.mapZoom=Mathf.Clamp(d.mapZoom,.7f,2);d.roast=Mathf.Clamp(d.roast,0,2);d.drawDistance=Mathf.Clamp(d.drawDistance,70,180);if(d.sales==null||d.sales.Length!=10)d.sales=new int[10];if(d.mastery==null||d.mastery.Length!=10)d.mastery=new int[10];for(int i=0;i<10;i++)d.mastery[i]=Mathf.Clamp(d.mastery[i],0,400);}
  public void Setup(CafeGame owner){game=owner;if(game.data.hundred==null)game.data.hundred=new CafeHundredData();Normalize(Data);venues=FindObjectsByType<CafeVenue>();Physics.SyncTransforms();ApplySettings();NewDay();
   foreach(var v in venues){Vector3 entrance=v.transform.position;if(Physics.CheckSphere(entrance+Vector3.up,.25f,~(1<<8),QueryTriggerInteraction.Ignore)){bool found=false;for(float radius=.5f;radius<=10&&!found;radius+=.5f)for(int direction=0;direction<8;direction++){float angle=direction*Mathf.PI/4;Vector3 candidate=entrance+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius;if(!Physics.CheckSphere(candidate+Vector3.up,.3f,~(1<<8),QueryTriggerInteraction.Ignore)){v.transform.position=candidate;found=true;break;}}}var tint=v.category=="market"?new Color(.3f,.8f,.5f):v.Contract?new Color(1,.7f,.25f):new Color(.4f,.65f,1);var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Hizmet kategorisi · "+v.category;marker.transform.SetParent(game.world.transform);marker.transform.position=v.transform.position+Vector3.up*.025f;marker.transform.localScale=new Vector3(1.5f,.025f,1.5f);Destroy(marker.GetComponent<Collider>());marker.GetComponent<Renderer>().sharedMaterial=game.world.ColorMaterial(tint);}
   if(CafeFocusedMode.Enabled){signs=game.world.GetComponentsInChildren<TextMesh>();return;}
   beacon=new GameObject("Yön bulma hedefi");beacon.transform.SetParent(game.world.transform);beaconLabel=beacon.AddComponent<TextMesh>();beaconLabel.anchor=TextAnchor.MiddleCenter;beaconLabel.characterSize=.08f;beaconLabel.fontSize=48;beaconLabel.color=new Color(1,.85f,.3f);signs=game.world.GetComponentsInChildren<TextMesh>();
  }
  public void Readability(){if(!game.ui)return;foreach(var metric in game.ui.GetComponentsInChildren<CafeFontSize>(true)){var text=metric.GetComponent<UnityEngine.UI.Text>();text.fontSize=Data.largeText?Mathf.RoundToInt(metric.size*1.12f):metric.size;text.color=Data.contrast?Color.white:metric.color;}}
  public void ApplySettings(){game.player.view.farClipPlane=Data.drawDistance;QualitySettings.shadowDistance=game.data.quality==0?20:game.data.quality==1?35:50;}
  void LateUpdate(){if(!game||signs==null)return;foreach(var text in signs)if(text&&text.gameObject.activeInHierarchy){var renderer=text.GetComponent<Renderer>();if(renderer)renderer.enabled=Vector3.Dot(game.player.view.transform.position-text.transform.position,text.transform.forward)<0;}}
  void NewDay(){if(Data.challengeDay==game.data.day)return;Data.materialCost=0;Data.challengeDay=game.data.day;Data.challengeSales=Data.challengeArt=Data.challengeClean=0;}
  void Update(){if(!game||game.data==null)return;NewDay();refresh-=Time.unscaledDeltaTime;if(refresh<=0){refresh=.5f;Readability();}if(game.Paused)return;
   if(beacon){Vector3 destination=game.trackedVenue?game.trackedVenue.transform.position:new Vector3(0,0,-12);beacon.transform.position=destination+Vector3.up*3.5f;beacon.transform.rotation=game.player.view.transform.rotation;beaconLabel.text=(game.trackedVenue?"▼ HEDEF":"▼ KAFE")+"\n"+Vector3.Distance(game.player.transform.position,destination).ToString("0")+" m";}
   if(!CafeFocusedMode.Enabled&&Keyboard.current!=null&&Keyboard.current.hKey.wasPressedThisFrame){Data.minimap=!Data.minimap;game.Save();}
   // Branch interiors are deliberately outside the outdoor district.
   var p=game.player.transform.position;if(!CafeFocusedMode.Enabled&&p.x<140&&(p.x< -71||p.x>71||p.z< -111)){game.player.body.enabled=false;p.x=Mathf.Clamp(p.x,-70,70);p.z=Mathf.Max(-110,p.z);game.player.transform.position=p;game.player.body.enabled=true;}
   supplyClock-=Time.deltaTime;if(Data.autoSupply&&supplyClock<=0){supplyClock=25;if(game.data.beans<5&&game.data.depotBeans<15&&game.data.money>=50){game.expansion.Supply("beans");Log("Otomatik tedarik: 35 çekirdek / 25 coin");}}
   foreach(var drink in game.shelf){if((drink.Temperature<45&&!drink.cup.coldCup)||(drink.cup.coldCup&&drink.Temperature>16))drink.good=false;}
  }
  public void Log(string message){history.Insert(0,message);if(history.Count>5)history.RemoveAt(5);}
  public static float UnitCost(int recipe){var r=Economy.Recipes[recipe];return 2.2f+(r.milk?2.5f:0)+(r.cold?.8f:0)+(r.syrup!=""?1:0);}
  public int Capacity(int recipe){var r=Economy.Recipes[recipe];int n=Mathf.Min(game.data.beans,r.cold?Mathf.Min(game.data.cupsCold,game.data.lidsCold):Mathf.Min(game.data.cupsHot,game.data.lidsHot));if(r.milk)n=Mathf.Min(n,game.data.milk);if(r.cold)n=Mathf.Min(n,game.data.ice);if(r.syrup=="caramel")n=Mathf.Min(n,game.data.caramel);if(r.syrup=="vanilla")n=Mathf.Min(n,game.data.vanilla);return Mathf.Max(0,n);}
  public void Ground(CupWork cup){cup.dose=Data.dose;cup.grind=Data.grind;cup.temperature=Data.temperature;cup.roast=Data.roast;cup.extraction=0;cup.yield=0;}
  public static float IdealTemperature(int roast)=>roast==0?95:roast==2?90:93;
  public static float Score(CupWork c){float ratio=c.yield/Mathf.Max(1,c.dose);return Mathf.Clamp01(1-Mathf.Abs(ratio-2)*.65f-Mathf.Abs(c.temperature-IdealTemperature(c.roast))*.035f-Mathf.Abs(c.extraction-28)*.012f);}
  public static string Taste(CupWork c){if(c.yield/c.dose<1.55f||c.extraction<20)return "Ekşi · az ekstraksiyon";if(c.yield/c.dose>2.55f||c.extraction>35)return "Acı · fazla ekstraksiyon";return Score(c)>.7f?"Dengeli · tatlı gövde":"Dengesiz · ayarları gözden geçir";}
  public float BrewClockRate=>7*(1+(game.data.machine-1)*.075f);
  public void StartBrew(){var c=game.cup;c.extraction=0;c.yield=0;game.brewTotal=5f*Mathf.Lerp(1.35f,.7f,(c.grind-1)/8f)*(1+game.expansion.Data.machineDirt/250f)/(1+(game.data.machine-1)*.075f);game.brewTime=game.brewTotal;}
  public void BrewTick(float dt){if(game.cup==null)return;var c=game.cup;c.extraction+=dt*BrewClockRate;c.yield=c.dose*2.8f*Mathf.Clamp01(1-game.brewTime/game.brewTotal);}
  public void FinishBrew(){if(game.cup==null)return;var c=game.cup;c.perfectBrew=Score(c)>.8f;Log(c.dose.ToString("0")+" g → "+c.yield.ToString("0")+" g / "+c.extraction.ToString("0")+" sn · "+Taste(c));}
  public int MasteryLevel(int recipe)=>1+Mathf.Min(4,Data.mastery[recipe]/100);
  public void ResetSession(){history.Clear();supplyClock=0;Normalize(Data);ApplySettings();}
  public int Sold(CafeCustomer c){Data.sales[c.recipe]++;Data.qualitySum+=c.serviceQuality;Data.qualityCount++;if(c.preferenceMet)Data.preferencesMet++;Data.materialCost+=Mathf.RoundToInt(UnitCost(c.recipe));Data.challengeSales++;int bonus=0;if(c.goodDrink){int before=MasteryLevel(c.recipe);Data.mastery[c.recipe]=Mathf.Min(400,Data.mastery[c.recipe]+10+(c.preferenceMet?5:0));Data.combo++;Data.bestCombo=Mathf.Max(Data.bestCombo,Data.combo);bonus=Mathf.Min(5,Data.combo/3)+(c.waitSeconds<35?2:0)+MasteryLevel(c.recipe)-1;game.data.money+=bonus;game.data.earned+=bonus;game.expansion.Data.dayIncome+=bonus;Log(Economy.Recipes[c.recipe].name+" · Kalite "+c.serviceQuality+" · Seri "+Data.combo+" · ek +"+bonus);if(MasteryLevel(c.recipe)>before)Log(Economy.Recipes[c.recipe].name+" ustalığı Lv."+MasteryLevel(c.recipe)+"! Servis bonusu arttı.");}else{Data.combo=0;Log("Yanlış servis · "+c.feedback);}return bonus;}
  public void Waste(){Data.wasted++;Log("İçecek atıldı · israf "+Data.wasted);}
  public void Clean(){Data.challengeClean++;}
  public void Art(){Data.challengeArt++;}
  public bool ChallengeReady=>Data.challengeSales>=10&&Data.challengeArt>=2&&Data.challengeClean>=3;
  public void Claim(){if(Data.challengeRewardDay==game.data.day||!ChallengeReady){game.Tell("Hedef: 10 servis · 2 latte art · 3 temizlik");return;}Data.challengeRewardDay=game.data.day;game.data.money+=120;game.expansion.Data.dayIncome+=120;game.Tell("Ustalık hedefleri tamamlandı! +120 coin",true);game.Save();}
  public string Summary(){int best=0;for(int i=1;i<10;i++)if(Data.sales[i]>Data.sales[best])best=i;return "USTALIK VE İŞLETME\n\nOrtalama kalite: "+(Data.qualityCount>0?Mathf.RoundToInt(Data.qualitySum/(float)Data.qualityCount):0)+" / 100 · Tercih "+Data.preferencesMet+"\nSeri: "+Data.combo+" · Rekor: "+Data.bestCombo+"\nEn çok satılan: "+Economy.Recipes[best].name+" ("+Data.sales[best]+")\nMalzeme maliyeti (tahmini): "+Data.materialCost+" coin\nİsraf: "+Data.wasted+" bardak ≈ "+(Data.wasted*4)+" coin\nSeçili tarife yetecek stok: "+Capacity(game.selected)+"\n\nGÜNLÜK HEDEFLER · +120 coin\nServis "+Data.challengeSales+" / 10 · Art "+Data.challengeArt+" / 2\nTemizlik "+Data.challengeClean+" / 3\n\nSON İŞLEMLER\n"+string.Join("\n",history);}
  void OnGUI(){if(CafeFocusedMode.Enabled||!game||game.Paused)return;var pour=GetComponent<CafeLattePour>();if(pour&&pour.Active)return;GUI.depth=-5;float scale=Screen.height/720f;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale;
   if(small==null)small=new GUIStyle(GUI.skin.label){fontSize=13};small.fontSize=Data.largeText?16:13;small.normal.textColor=Color.white;
   GUI.color=Data.contrast?Color.yellow:game.player.target?new Color(.4f,1,.7f):Color.white;GUI.DrawTexture(new Rect(width/2-2,358,4,4),Texture2D.whiteTexture);GUI.color=Color.white;
   if(!Data.minimap){GUI.matrix=Matrix4x4.identity;return;}
   var map=new Rect(width-214,410,200,200);GUI.color=new Color(.04f,.09f,.08f,.94f);GUI.DrawTexture(map,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(map.x+7,map.y+4,190,25),"MAHALLE · H aç/kapat",small);
   Vector3 origin=new Vector3(0,0,-55);float zoom=Data.mapZoom*1.35f;
   foreach(var v in venues){Vector2 q=Map(v.transform.position,origin,zoom,map);GUI.color=v==game.trackedVenue?Color.yellow:new Color(.4f,.85f,.65f);GUI.DrawTexture(new Rect(q.x-3,q.y-3,6,6),Texture2D.whiteTexture);}
   Vector2 cafe=Map(new Vector3(0,0,-12),origin,zoom,map);GUI.color=new Color(1,.7f,.3f);GUI.DrawTexture(new Rect(cafe.x-5,cafe.y-3,10,6),Texture2D.whiteTexture);GUI.color=Color.white;
   Vector2 player=Map(game.player.transform.position,origin,zoom,map);GUI.Label(new Rect(player.x-8,player.y-11,30,25),Arrow(game.player.transform.eulerAngles.y),small);
   var near=venues.OrderBy(v=>Vector3.Distance(game.player.transform.position,v.transform.position)).Take(2);int row=0;foreach(var v in near){float dist=Vector3.Distance(game.player.transform.position,v.transform.position);GUI.Label(new Rect(map.x,map.y+203+row++*19,215,22),v.title.Split('·')[0]+" · "+dist.ToString("0")+" m",small);}
   var target=game.trackedVenue?game.trackedVenue.transform.position:new Vector3(0,0,-12);float distance=Vector3.Distance(game.player.transform.position,target);GUI.Label(new Rect(map.x,map.y+244,215,42),(game.trackedVenue?"Hedef":"Kafe")+": "+distance.ToString("0")+" m · ~"+Mathf.CeilToInt(distance/Data.walkSpeed)+" sn",small);GUI.matrix=Matrix4x4.identity;
  }
  static Vector2 Map(Vector3 p,Vector3 origin,float zoom,Rect r)=>new Vector2(Mathf.Clamp(r.center.x+(p.x-origin.x)*zoom,r.x+8,r.xMax-8),Mathf.Clamp(r.center.y-(p.z-origin.z)*zoom,r.y+30,r.yMax-8));
  static string Arrow(float yaw){string[] arrows={"↑","↗","→","↘","↓","↙","←","↖"};return arrows[Mathf.RoundToInt(yaw/45)%8];}
 }
}
