using UnityEngine;
using UnityEngine.InputSystem;
namespace CafeLife {
 public sealed partial class CafeLattePour:MonoBehaviour {
  CafeGame game; CupWork cup; public bool Active{get;private set;} float milk,spill,height=.25f; Vector2 last; Texture2D disc;
  public void Begin(CafeGame owner){if(Active||!owner||owner.cup==null||owner.Paused||owner.cup.lidded)return;game=owner;cup=game.cup;milk=spill=0;cup.artPoints.Clear();cup.artWidths.Clear();Active=true;game.player.Lock(false);}
  void Update(){if(!Active)return;if(!game||game.cup!=cup||game.Paused||cup.lidded){Cancel();return;}if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Cancel();return;}
   Vector2 point=Vector2.zero,motion=Vector2.zero;bool pressed=false;var touch=Touchscreen.current;
   if(touch!=null&&touch.primaryTouch.press.isPressed){point=touch.primaryTouch.position.ReadValue();motion=touch.primaryTouch.delta.ReadValue();pressed=true;}
   else if(Mouse.current!=null){point=Mouse.current.position.ReadValue();motion=Mouse.current.delta.ReadValue();pressed=Mouse.current.leftButton.isPressed;height=Mathf.Clamp01(height+Mouse.current.scroll.ReadValue().y*.002f);}
   var layout=StudioLayout(Screen.safeArea,Screen.height);Vector2 local=(new Vector2(point.x,Screen.height-point.y)-layout.position)/(layout.width/1120f);Vector2 normalized=(local-PourCenter)/PourRadius;
   if(pressed&&normalized.magnitude<1.25f)PourAt(normalized,Time.deltaTime*22,motion.magnitude/(layout.width/1120f),height);
  }

  public void PourAt(Vector2 p,float amount,float motion,float pourHeight){if(!Active||amount<=0)return;if(!game||game.cup!=cup||game.Paused||cup.lidded){Cancel();return;}milk+=Mathf.Min(amount,10);
    if(p.magnitude>.94f)spill+=Mathf.Min(amount,10);else if(pourHeight<.55f&&cup.artPoints.Count<1500&&(cup.artPoints.Count==0||Vector2.Distance(last,p)>.015f)){cup.artPoints.Add(p);cup.artWidths.Add(Mathf.Lerp(.014f,.004f,Mathf.Clamp01(motion/18)));last=p;}
    if(milk>=160)Finish(true);
  }
  public void Complete()=>Finish(true);
  public void Cancel()=>Finish(false);
  void OnDisable(){Cancel();}

  void Finish(bool commit){if(!Active)return;Active=false;commit=commit&&game&&game.cup==cup&&!game.Paused&&!cup.lidded;cup.art=commit&&milk>=35&&spill<25&&cup.artPoints.Count>=12;
   if(!commit){cup.artPoints.Clear();cup.artWidths.Clear();}if(!game)return;if(cup.art)game.GetComponent<CafeHundred>()?.Art();game.UpdateHand();game.player.Lock(!game.Paused&&!CafeFocusedMode.CounterControls);game.Tell(cup.art?"Latte art hazır! "+milk.ToString("0")+" ml mikro köpük · +5 coin":"Desen için 35–160 ml sütü bardağın içinde yavaşça gezdir.",cup.art);}
  void OnGUI(){if(Active)DrawStudio();}
  public static void Decorate(GameObject root,CupWork cup,Material material){if(cup.lidded||cup.artPoints.Count==0)return;var g=new GameObject("El dökümü latte art");g.transform.SetParent(root.transform,false);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;line.startWidth=line.endWidth=.004f;line.positionCount=cup.artPoints.Count;var widths=new AnimationCurve();for(int i=0;i<cup.artPoints.Count;i++)widths.AddKey(i/(float)Mathf.Max(1,cup.artPoints.Count-1),i<cup.artWidths.Count?cup.artWidths[i]:.006f);line.widthCurve=widths;line.widthMultiplier=1;for(int i=0;i<cup.artPoints.Count;i++){var p=cup.artPoints[i];line.SetPosition(i,new Vector3(p.x*.065f,.145f,-p.y*.065f));}}
  void OnDestroy(){if(disc)Destroy(disc);}
 }
}

