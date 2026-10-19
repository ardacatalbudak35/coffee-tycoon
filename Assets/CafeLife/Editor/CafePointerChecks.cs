using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafePointerChecks {
  public static string Report="";
  static void Check(bool ok,string title){if(!ok)throw new Exception(title);Report+="PASS "+title+"\n";}
  public static void Validate(CafeGame g){Report="";g.Continue();g.player.Lock(true);g.player.TogglePointer();Check(g.player.PointerMode,"Pointer toggle releases desktop camera mode");g.player.TogglePointer();Check(!g.player.PointerMode,"Pointer toggle restores desktop camera mode");g.OpenOperations();g.player.TogglePointer();Check(g.player.PointerMode&&g.Paused,"Pointer toggle preserves open menu");g.Continue();g.cup=new CupWork{recipe=1,hasMilk=true,brewed=true,foamed=true};var pour=g.GetComponent<CafeLattePour>();pour.Begin(g);g.player.TogglePointer();Check(pour.Active&&g.player.PointerMode,"Pointer toggle preserves latte drawing session");pour.Cancel();g.Discard();g.player.Lock(true);
  }
  public static void ValidateRaycasts(CafeGame g){g.player.Lock(false);g.ui.SendMessage("Update");Canvas.ForceUpdateCanvases();var card=GameObject.Find("Cafe HUD").GetComponentsInChildren<Transform>(true).First(t=>t.name=="Order cards HUD").GetComponentsInChildren<Button>().First();var r=(RectTransform)card.transform;var screen=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));Check(g.player.OverInterface(screen),"Rendered order card receives immediate pointer raycast");Check(!g.player.OverInterface(new Vector2(4,Screen.height*.5f)),"Free scene area remains available for camera recapture");g.player.Lock(true);}
 }
}
