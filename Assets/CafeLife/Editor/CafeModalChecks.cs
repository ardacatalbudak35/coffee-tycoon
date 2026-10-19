using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace CafeLife {
 public static class CafeModalChecks {
  public static string Report="";static void Check(bool ok,string text){if(!ok)throw new Exception(text);Report+="PASS "+text+"\n";}
  public static void Validate(CafeGame g){Report="";g.Continue();g.Discard();bool preview=g.ui.EditorTouchPreview;var target=g.player.target;g.ui.EditorTouchPreview=true;g.ui.SendMessage("Update");Check(g.player.movePad.gameObject.activeInHierarchy&&g.player.lookPad.gameObject.activeInHierarchy&&g.player.sprintPad.gameObject.activeInHierarchy,"Normal mobile preview exposes world controls");g.cup=new CupWork{recipe=1,hasMilk=true,foamed=true,brewed=true};var cup=g.cup;var art=g.GetComponent<CafeLattePour>();art.Begin(g);g.ui.SendMessage("Update");Check(!g.player.movePad.gameObject.activeInHierarchy&&!g.player.lookPad.gameObject.activeInHierarchy&&!g.player.sprintPad.gameObject.activeInHierarchy,"Latte overlay disables underlying mobile controls");g.player.target=g.world.GetComponentsInChildren<CafeTarget>().First(t=>t.kind=="trash");g.player.Use();Check(g.cup==cup&&art.Active,"Underlying interaction cannot discard cup while drawing");g.player.Jump();var queued=(bool)typeof(CafePlayer).GetField("jumpRequested",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g.player);Check(!queued,"Modal jump cannot leak into resumed gameplay");art.Cancel();g.ui.SendMessage("Update");Check(g.player.movePad.gameObject.activeInHierarchy&&g.player.sprintPad.gameObject.activeInHierarchy,"Closing latte overlay restores world controls");g.OpenOperations();g.player.Use();Check(g.cup==cup&&g.Paused,"Paused interaction cannot modify held drink");g.Continue();g.Discard();g.player.target=target;g.ui.EditorTouchPreview=preview;g.ui.SendMessage("Update");
  }
 }
}
