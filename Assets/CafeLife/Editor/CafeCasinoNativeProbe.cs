using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CafeLife {
 [InitializeOnLoad] public static class CafeCasinoNativeProbe {
  static int stage;static double began,settled;static CafeGame game;
  static CafeCasinoNativeProbe(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
  static void Mode(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("CasinoNative",false))return;stage=0;SessionState.SetBool("CasinoNative",false);var owner=Object.FindAnyObjectByType<CafeGame>();if(owner){owner.testing=false;owner.suppressSaveOnExit=false;owner.data=Economy.Load(owner.SavePath);EditorUtility.SetDirty(owner);EditorSceneManager.SaveScene(owner.gameObject.scene);}}
  static void Finish(string text){File.WriteAllText("Logs/CafeCasinoNativeCheck.txt",text);EditorApplication.isPlaying=false;}
  static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(stage==0){if(!File.Exists("Logs/RunCafeCasinoNative.flag"))return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}game=Object.FindAnyObjectByType<CafeGame>();if(!game)return;File.Delete("Logs/RunCafeCasinoNative.flag");game.testing=true;game.suppressSaveOnExit=true;Economy.Save(new SaveData(),game.SavePath);SessionState.SetBool("CasinoNative",true);stage=1;began=EditorApplication.timeSinceStartup;EditorApplication.isPlaying=true;return;}if(!EditorApplication.isPlaying)return;if(EditorApplication.timeSinceStartup-began>180){Finish("FAIL native casino input timeout");return;}game=Object.FindAnyObjectByType<CafeGame>();if(!game||!game.ui)return;if(stage==1){game.data.money=500;game.data.staff=0;game.world.Refresh();game.Continue();CafeBoulevardChecks.At(game,new Vector3(15,4.25f,-29),180);game.player.RefreshTarget();game.player.Use();if(!game.ui.IsPage("casino")){Finish("FAIL table interaction did not open casino");return;}stage=2;File.WriteAllText("Logs/CafeCasinoNativeCheck.txt","READY: isolated 500-coin save; table interaction opened casino; waiting for native UI click.");return;}var casino=game.GetComponent<CafeCasino>();if(stage==2&&casino&&casino.player.Count>=2){stage=3;File.AppendAllText("Logs/CafeCasinoNativeCheck.txt","\nPASS native bet click dealt "+casino.player.Count+" cards; balance "+game.data.money);}
   if(stage==3&&casino&&!casino.Active){stage=4;settled=EditorApplication.timeSinceStartup;File.AppendAllText("Logs/CafeCasinoNativeCheck.txt","\nPASS native hand settled; "+casino.Result+"; balance "+game.data.money+"\nALL NATIVE CASINO CHECKS PASSED");}if(stage==4&&EditorApplication.timeSinceStartup-settled>8)EditorApplication.isPlaying=false;
  }
 }
}
