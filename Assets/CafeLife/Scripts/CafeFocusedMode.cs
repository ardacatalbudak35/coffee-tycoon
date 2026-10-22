using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CafeLife {
 public sealed class CafeCounterPress:MonoBehaviour,IPointerDownHandler,IPointerUpHandler {
  public CafeFocusedMode mode;public void OnPointerDown(PointerEventData e){mode.PressDown();}public void OnPointerUp(PointerEventData e){mode.PressUp();}
 }
 public sealed class CafeFocusedMode:MonoBehaviour {
  public static bool Enabled=>true; public static bool CounterControls=>false;public CafeGame game;public CafeCustomer selectedOrder;public float holdProgress;public string heldAction="";bool consumeClick;GameObject jug,stream;
  static int[] ServedRecipes(int served)=>served<5?new[]{0,1}:served<10?new[]{0,1,4}:served<20?new[]{0,1,2,3,4}:served<35?new[]{0,1,2,3,4,5,8,10}:Enumerable.Range(0,Economy.Recipes.Length).ToArray();
  // Hikaye açıkken menüyü haberler belirler; ilk gecenin ardından eski servis sayısı kuralı da devreye girer.
  public static int[] AvailableRecipes(int served){var old=ServedRecipes(served);if(!CafeStory.Active)return old;var list=Enumerable.Range(0,Economy.Recipes.Length).Where(i=>(CafeStory.Mask&(1<<i))!=0).ToArray();return list.Length>0?list:new[]{0};}
  public void Setup(CafeGame owner){game=owner;if(!CounterControls){var p=game.player;p.body.enabled=false;p.transform.position=new Vector3(0,.08f,-9.7f);p.transform.rotation=Quaternion.identity;p.view.transform.localPosition=Vector3.up*1.65f;p.view.transform.localRotation=Quaternion.identity;p.pitch=0;p.body.enabled=true;return;}Pose();
   var light=new GameObject("Tezgâh çalışma ışığı").AddComponent<Light>();light.transform.SetParent(game.world.transform);light.transform.position=new Vector3(0,3,-6);light.type=LightType.Point;light.range=10;light.intensity=1.6f;light.color=new Color(1,.9f,.75f);
   jug=GameObject.CreatePrimitive(PrimitiveType.Cylinder);jug.name="Süt döküm sürahisi";jug.transform.SetParent(game.player.view.transform,false);jug.transform.localPosition=new Vector3(.2f,.06f,.78f);jug.transform.localScale=new Vector3(.1f,.09f,.1f);jug.transform.localRotation=Quaternion.Euler(0,0,35);jug.layer=2;Destroy(jug.GetComponent<Collider>());jug.GetComponent<Renderer>().sharedMaterial=game.world.ColorMaterial(new Color(.65f,.7f,.72f));
   stream=new GameObject("Bardağa süt akışı");stream.transform.SetParent(game.player.view.transform,false);stream.layer=2;var line=stream.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;line.SetPositions(new[]{new Vector3(.13f,.12f,.77f),new Vector3(.08f,.1f,.76f),new Vector3(.03f,.075f,.75f)});line.startWidth=line.endWidth=.005f;line.sharedMaterial=game.world.ColorMaterial(new Color(1,.96f,.85f));jug.SetActive(false);stream.SetActive(false);
  }
  void LateUpdate(){if(game&&CounterControls)Pose();}
  void Pose(){if(!game||!game.player)return;var p=game.player;p.body.enabled=false;p.transform.position=new Vector3(0,.08f,-9.7f);p.transform.rotation=Quaternion.identity;p.view.transform.localPosition=new Vector3(0,2.65f,0);p.view.transform.localRotation=Quaternion.Euler(22,0,0);p.view.fieldOfView=70;p.Lock(false);}
  void Update(){if(!game||game.Paused){CancelHold();return;}if(heldAction!="")TickHold(Time.deltaTime);bool pouring=heldAction=="milk"||heldAction=="water";if(jug)jug.SetActive(pouring);if(stream)stream.SetActive(pouring);}
  public CafeCustomer[] Orders=>game.customers.Where(c=>c&&(c.state==CafeCustomer.State.Waiting||c.state==CafeCustomer.State.Entering)).OrderBy(c=>c.patience).ThenBy(c=>c.slot).Take(2).ToArray();
  public void SelectOrder(CafeCustomer customer){if(!customer||customer.state!=CafeCustomer.State.Waiting){game.Tell("Müşteri tezgâha geliyor...");return;}if(game.cup!=null){game.Tell("Önce elindeki içeceği bitir veya bardağı at.");return;}selectedOrder=customer;if(!customer.accepted){customer.accepted=true;customer.orderNumber=++game.orderCounter;}game.selected=customer.recipe;game.Tell(Economy.Recipes[customer.recipe].name+" · "+CafeService.PreferenceLabel(customer),true);}
  public string ActionKey {get{if(game.cup==null)return game.filterCup!=null&&game.filterTime<=0?"filter":"cup";if(game.CupOnMachine)return "machine";var c=game.cup;int seen=0;foreach(var step in CafeGame.IdealSteps(Economy.Recipes[c.recipe])){if(step=="espresso"){seen++;if(c.steps.Count(s=>s=="espresso")>=seen)continue;return !c.ground?"grinder":"machine";}if(c.steps.Contains(step))continue;if(step=="lid")return c.coldCup?"lid_cold":"lid_hot";return step;}return "pickup";}}
  public static bool HoldAction(string key)=>key=="grinder"||key=="milk"||key=="water"||key=="caramel"||key=="vanilla";
  public string ActionLabel {get{switch(ActionKey){case "cup":return "SİPARİŞİ SEÇ · BARDAĞI AL";case "grinder":return "BASILI TUT · KAHVEYİ ÖĞÜT";case "machine":return game.brewTime>0?"AKIŞI KES · "+game.cup.yield.ToString("0")+" g":game.CupOnMachine?"BARDAĞI MAKİNEDEN AL":"ESPRESSOYU BAŞLAT";case "milk":return "BASILI TUT · SÜTÜ DÖK";case "water":return "BASILI TUT · SU EKLE";case "caramel":return "BASILI TUT · KARAMEL EKLE";case "vanilla":return "BASILI TUT · VANİLYA EKLE";case "ice":return "BUZ EKLE";case "pickup":return "MÜŞTERİYE SERVİS ET";default:return "KAPAĞI TAK";}}}
  public void PressDown(){consumeClick=false;if(game.Paused)return;string key=ActionKey;if(HoldAction(key)){heldAction=key;holdProgress=0;consumeClick=true;}}
  public void TickHold(float dt){if(heldAction=="")return;holdProgress+=Mathf.Max(0,dt)/(heldAction=="grinder"?1.2f:.85f);if(holdProgress>=1){string action=heldAction;heldAction="";holdProgress=1;game.Act(action);}}
  public void PressUp(){if(heldAction!=""){heldAction="";holdProgress=0;game.Tell("İşlem tamamlanana kadar basılı tut.");}}
  public void ClickPrimary(){if(consumeClick){consumeClick=false;return;}if(game.Paused)return;string action=ActionKey;if(HoldAction(action)){game.Tell("Bu işlem için düğmeyi basılı tut.");return;}
   if(action=="cup"){if(!selectedOrder||selectedOrder.state!=CafeCustomer.State.Waiting)selectedOrder=game.customers.Where(c=>c&&c.state==CafeCustomer.State.Waiting).OrderBy(c=>c.patience).FirstOrDefault();if(!selectedOrder){game.Tell("İlk müşteri birazdan geliyor.");return;}SelectOrder(selectedOrder);game.Act(Economy.Recipes[game.selected].cold?"cup_cold":"cup_hot");}
   else game.Act(action);
  }
  public void CancelHold(){heldAction="";holdProgress=0;if(jug)jug.SetActive(false);if(stream)stream.SetActive(false);}
  public void Restock(){game.Restock();}
  public void Supply(string kind){if(game.expansion.Supply(kind))game.Restock();}
 }
}

