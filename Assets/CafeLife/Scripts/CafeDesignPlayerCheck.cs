using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace CafeLife {
 public sealed class CafeDesignPlayerCheck:MonoBehaviour {
  CafeGame game;string output;bool isolated;readonly StringBuilder report=new StringBuilder();

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Bootstrap(){
   var args=Environment.GetCommandLineArgs();
   if(Application.isEditor||Array.IndexOf(args,"-cafe-smoke")<0||Array.IndexOf(args,"-cafe-design-check")<0)return;
   var helper=new GameObject("Explicit standalone design verification");DontDestroyOnLoad(helper);helper.AddComponent<CafeDesignPlayerCheck>();
  }

  IEnumerator Start(){
   output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","DesignValidation"));
   var run=Verify();
   while(true){
    bool more;object next;
    try{more=run.MoveNext();next=more?run.Current:null;}
    catch(Exception error){Finish(error);yield break;}
    if(!more)break;
    yield return next;
   }
   Finish(null);
  }

  IEnumerator Verify(){
   Directory.CreateDirectory(output);
   float deadline=Time.realtimeSinceStartup+30;
   while(Time.realtimeSinceStartup<deadline){
    game=FindFirstObjectByType<CafeGame>();
    if(game&&game.ui&&game.world&&game.player&&game.data!=null)break;
    yield return null;
   }
   Require(game&&game.ui&&game.world&&game.player&&game.data!=null,"Game and UI initialized within 30 seconds");
   Require(game.testing&&game.suppressSaveOnExit,"Testing and save-on-exit suppression are enabled");
   string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
   Require(Path.GetFullPath(game.SavePath).StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase),"Game save path is isolated in the temporary directory");
   isolated=true;
   game.data.money=500;game.data.dayTime=120;
   game.Continue();At(new Vector3(0,.1f,-9),0);
   for(int i=0;i<24;i++)yield return null;
   yield return new WaitForEndOfFrame();Capture("Interior.png");

   var reticle=FindFirstObjectByType<CafeReticle>();
   Require(reticle&&reticle.canvasRenderer.GetAlpha()>.9f,"Reticle is visible in standalone gameplay");
   Vector2 centre=RectTransformUtility.WorldToScreenPoint(null,reticle.rectTransform.position);
   Require(Vector2.Distance(centre,game.player.view.pixelRect.center)<1&&!game.player.OverInterface(centre),"Reticle is camera-centred and does not block input");
   CafeTarget machine=null;
   foreach(var target in FindObjectsByType<CafeTarget>(FindObjectsSortMode.None))if(target.kind=="machine"){machine=target;break;}
   Require(machine,"Espresso interaction target exists");
   Vector3 aim=machine.transform.position; foreach(var volume in machine.GetComponentsInChildren<Collider>())if(volume.enabled&&volume.isTrigger&&volume.gameObject.layer==8){aim=volume.bounds.center;break;}
   At(new Vector3(aim.x,.1f,aim.z+1.8f),180);
   game.player.pitch=Mathf.Atan2(game.player.view.transform.position.y-aim.y,1.8f)*Mathf.Rad2Deg;
   game.player.view.transform.localRotation=Quaternion.Euler(game.player.pitch,0,0);
   for(int i=0;i<24;i++)yield return null;
   Require(game.player.target==machine,"Reticle highlights the actual espresso interaction target");
   yield return new WaitForEndOfFrame();Capture("Reticle-interaction.png");

   var ink=Resources.Load<Material>("CafeMaterials/CardInk");
   Require(ink&&ink.shader&&ink.shader.isSupported&&ink.shader.name=="Universal Render Pipeline/Unlit","Bundled CardInk material retains the supported URP Unlit shader");
   At(new Vector3(15,4.25f,-29),180);game.OpenCasino();game.StartBlackjack(50);
   for(int i=0;i<12;i++)yield return null;
   yield return new WaitForEndOfFrame();Capture("Blackjack.png");
   Transform live=null;
   foreach(var node in game.world.GetComponentsInChildren<Transform>())if(node.name=="Masada blackjack canlı kartlar"){live=node;break;}
   Require(live,"Live blackjack card layout exists");
   int renderers=0;
   foreach(var renderer in live.GetComponentsInChildren<Renderer>()){
    if(!renderer.enabled)continue;renderers++;
    foreach(var material in renderer.sharedMaterials){
     if(!material||!material.shader||!material.shader.isSupported||material.shader.name.IndexOf("InternalErrorShader",StringComparison.OrdinalIgnoreCase)>=0)
      throw new InvalidOperationException("Unsupported live card material on "+renderer.name);
    }
   }
   Require(renderers>=4,"Live card renderers have supported non-error shaders ("+renderers+")");
   Require(game.GetComponent<CafeCasino>().player.Count==2,"Blackjack dealt two player cards using isolated test coins");
   game.OpenOperations();game.ui.ShowMenu("home");
   Require(game.Paused,"Verification ends at the paused home menu");
   yield return null;
   yield return new WaitForEndOfFrame();
   Require(reticle.canvasRenderer.GetAlpha()==0,"Reticle hides on the standalone home menu");
  }

  void At(Vector3 position,float yaw){
   game.player.body.enabled=false;game.player.transform.position=position;game.player.transform.rotation=Quaternion.Euler(0,yaw,0);
   game.player.pitch=0;game.player.view.transform.localRotation=Quaternion.identity;game.player.body.enabled=true;Physics.SyncTransforms();
  }
  void Capture(string name){
   var image=ScreenCapture.CaptureScreenshotAsTexture();
   if(!image)throw new InvalidOperationException("Screenshot capture returned no texture: "+name);
   try{
    File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
    var pixels=image.GetPixels32();float minimum=1,maximum=0;int visible=0,samples=0;
    int stepX=Mathf.Max(1,image.width/80),stepY=Mathf.Max(1,image.height/45);
    for(int y=0;y<image.height;y+=stepY)for(int x=0;x<image.width;x+=stepX){
     var pixel=pixels[y*image.width+x];float luminance=(.2126f*pixel.r+.7152f*pixel.g+.0722f*pixel.b)/255f;
     minimum=Mathf.Min(minimum,luminance);maximum=Mathf.Max(maximum,luminance);if(luminance>.025f)visible++;samples++;
    }
    float visibleRatio=visible/(float)Mathf.Max(1,samples),variation=maximum-minimum;
    report.AppendLine("IMAGE "+name+" "+image.width+"x"+image.height+" variation="+variation.ToString("F3")+" visible="+visibleRatio.ToString("P1"));
    Require(variation>.05f&&visibleRatio>.05f,"Screenshot "+name+" contains a nonblank rendered frame");
   }
   finally{Destroy(image);}
  }
  void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);report.AppendLine("PASS "+message);}
  void Finish(Exception error){
   if(isolated&&game&&game.ui){try{game.OpenOperations();game.ui.ShowMenu("home");}catch(Exception pauseError){error=error??pauseError;}}
   report.AppendLine(error==null?"ALL STANDALONE DESIGN CHECKS PASSED":"FAIL "+error);
   try{Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Report.txt"),report.ToString());}
   catch(Exception writeError){Debug.LogError("Design verification report could not be written: "+writeError.Message);}
   if(error!=null)Debug.LogError(report.ToString());else Debug.Log(report.ToString());
   Destroy(gameObject);
  }
 }
}
