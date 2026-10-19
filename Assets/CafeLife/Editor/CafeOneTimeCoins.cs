using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace CafeLife {
 [InitializeOnLoad] public static class CafeOneTimeCoins {
  static CafeOneTimeCoins(){EditorApplication.update+=Tick;}
  static void Tick(){const string request="Logs/AddTestCoins.flag";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!EditorApplication.isPlaying||!File.Exists(request))return;var game=UnityEngine.Object.FindAnyObjectByType<CafeGame>();if(!game||game.testing||game.data==null)return;int amount;if(!int.TryParse(File.ReadAllText(request).Trim(),out amount)||amount!=50000)return;File.Delete(request);try{int before=game.data.money;game.data.money+=amount;game.Save();game.Tell("Deneme bakiyesi +50.000 coin",true);File.WriteAllText("Logs/AddTestCoins-result.txt","Before: "+before+"\nAfter: "+game.data.money+"\nSave: "+game.SavePath);}catch(Exception e){File.WriteAllText("Logs/AddTestCoins-result.txt",e.ToString());}}
 }
}

