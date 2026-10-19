using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace CafeLife {
 [InitializeOnLoad] public static class CafeReleaseBuild {
  static CafeReleaseBuild(){EditorApplication.update+=Tick;}
  static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/RunCafeBuild.flag"))return;
   File.Delete("Logs/RunCafeBuild.flag");try{
    string folder=@"C:\Users\ardac\Documents\Codex\2026-10-16\kanka-udem\outputs\CoffeeTycoon-Windows-v4";Directory.CreateDirectory(folder);
    PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/CafeLife/Scenes/CafeLife.unity"},locationPathName=Path.Combine(folder,"CoffeeTycoon.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
    File.WriteAllText("Logs/CafeReleaseBuild.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize+"\nTime: "+report.summary.totalTime);
   }catch(Exception e){File.WriteAllText("Logs/CafeReleaseBuild.txt",e.ToString());}
  }
 }
}
