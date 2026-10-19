using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CafeLife {
 public static class CafeTouchChecks {
  public static string Report="";
  static void Check(bool ok,string title){if(!ok)throw new Exception(title);Report+="PASS "+title+"\n";}
  static PointerEventData Finger(int id,Vector2 position,Vector2 delta=default)=>new PointerEventData(EventSystem.current){pointerId=id,position=position,delta=delta};
  public static void Validate(CafeGame g){Report="";g.Continue();Canvas.ForceUpdateCanvases();var move=g.player.movePad;var look=g.player.lookPad;var sprint=g.player.sprintPad;var mobileActions=sprint.transform.parent.gameObject;bool actionsActive=mobileActions.activeSelf;bool sprintActive=sprint.gameObject.activeSelf;mobileActions.SetActive(true);move.gameObject.SetActive(true);look.gameObject.SetActive(true);sprint.gameObject.SetActive(true);move.ResetPad();look.ResetPad();sprint.ResetPad();
   var point=RectTransformUtility.WorldToScreenPoint(null,move.transform.TransformPoint(new Vector3(65,0,0)));var first=Finger(11,point);var other=Finger(22,point-Vector2.right*130);move.OnPointerDown(first);Check(move.value.x>.99f&&move.value.magnitude<=1.001f,"Touch joystick reaches full speed and clamps magnitude");move.OnPointerDown(other);move.OnDrag(other);move.OnPointerUp(other);Check(move.value.x>.99f,"Second finger cannot hijack or release movement joystick");
   look.OnPointerDown(Finger(22,Vector2.zero,new Vector2(8,-4)));look.OnDrag(Finger(11,Vector2.zero,new Vector2(99,99)));Check(look.value==new Vector2(8,-4)&&move.value.x>.99f,"Look and movement own independent fingers");look.OnPointerUp(Finger(22,Vector2.zero));Check(look.value==Vector2.zero,"Look release clears camera delta");move.OnPointerUp(first);Check(move.value==Vector2.zero&&move.knob.anchoredPosition==Vector2.zero,"Movement release recentres joystick");
   sprint.OnPointerDown(first);sprint.OnPointerDown(other);sprint.OnPointerUp(first);Check(!sprint.held,"Second finger cannot steal sprint ownership and leave it held");sprint.OnPointerDown(first);sprint.OnPointerExit(other);Check(sprint.held,"Unrelated pointer exit does not cancel sprint");sprint.OnPointerExit(first);Check(!sprint.held,"Sprint owner leaving button cancels hold");
   move.OnPointerDown(first);look.OnPointerDown(Finger(22,Vector2.zero,new Vector2(4,2)));sprint.OnPointerDown(first);g.player.Lock(false);Check(move.value==Vector2.zero&&look.value==Vector2.zero&&!sprint.held,"Menu or focus transition clears all touch holds");
   move.OnPointerDown(first);sprint.OnPointerDown(first);move.gameObject.SetActive(false);look.gameObject.SetActive(false);sprint.gameObject.SetActive(false);Check(move.value==Vector2.zero&&!sprint.held,"Disabled mobile controls cannot keep moving or sprinting");sprint.gameObject.SetActive(sprintActive);mobileActions.SetActive(actionsActive);
  }
 }
}

