using System.Collections;
using System.IO;
using UnityEngine;

namespace CafeLife {
 public static class CafeBasketAimChecks {
  public static bool Passed {get;private set;}
  static readonly Vector3 ReleaseOffset=new Vector3(0,-.12f,.8f);
  static bool Check(bool value,string description)=>CafeFocusedCheck.VisualCheck(value,description);

  public static IEnumerator Run(CafeGame game){
   Passed=false;
   if(!Check(game&&game.testing&&game.suppressSaveOnExit,"Basket aiming checks run only with an isolated test save"))yield break;
   string temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
   if(!Check(Path.GetFullPath(game.SavePath).StartsWith(temp,System.StringComparison.OrdinalIgnoreCase),"Basket physics cannot write the real player save"))yield break;
   var basketball=game.GetComponent<CafeBasketball>();
   if(!Check(basketball&&basketball.ball&&game.player&&game.player.view,"Basket aiming fixture has ball and camera"))yield break;

   var player=game.player;var oldPosition=player.transform.position;var oldRotation=player.transform.rotation;
   float oldPitch=player.pitch,oldFov=player.view.fieldOfView;
   bool oldPointer=player.PointerMode,oldBody=player.body.enabled,oldPaused=game.Paused;
   GameObject obstacle=null;
   try{
    game.Continue();game.Discard();
    float[] distances={2.5f,5f,8.5f,10.5f,6f,6f};
    float[] sides={0,0,0,0,-25,25};
    for(int shot=0;shot<distances.Length;shot++){
     // Keep the economy clock away from day rollover while this long-running
     // physics matrix verifies the exact +50 reward.
     game.data.dayTime=120;
     if(!Check(PickUpAtHome(game,basketball),"Basket test picks up the physical ball for shot "+(shot+1)))yield break;
     StandAndAim(game,basketball.hoop,distances[shot],sides[shot]);
     // Let the character settle on the court, then use the current camera height.
     yield return new WaitForFixedUpdate();yield return null;
     Aim(game,basketball.hoop-player.view.transform.position);
     var camera=player.view.transform;var origin=camera.TransformPoint(ReleaseOffset);
     if(!Check(basketball.TryAssistedVelocity(origin,out var planned),"Assistance accepts a clear aimed shot at "+distances[shot]+"m / "+sides[shot]+" degrees"))yield break;
     int beforeBaskets=basketball.baskets,beforeMoney=game.data.money;
     basketball.Throw();
     if(!Check(!basketball.Held&&basketball.AssistedLastShot&&!basketball.ball.isKinematic&&basketball.ball.useGravity,"Assisted shot releases a real gravity-driven Rigidbody"))yield break;
     var local=camera.InverseTransformPoint(basketball.ball.position);
     if(!Check(Vector3.Distance(local,ReleaseOffset)<.001f,"Release originates from camera centre with no right-hand offset"))yield break;
     if(!Check(basketball.ball.linearDamping<.0001f&&Vector3.Distance(basketball.ball.linearVelocity,planned)<.001f,"Released shot uses its planned velocity without flight damping"))yield break;

     bool crossed=false,captured=false;float nearestCrossing=float.PositiveInfinity;
     var previous=basketball.ball.position;float began=Time.time;
     while(Time.time-began<3&&basketball.baskets==beforeBaskets){
      game.data.dayTime=120;
      yield return new WaitForFixedUpdate();
      var current=basketball.ball.position;
      if(previous.y>basketball.hoop.y&&current.y<=basketball.hoop.y&&current.y<previous.y){
       float t=(previous.y-basketball.hoop.y)/(previous.y-current.y);
       var crossing=Vector3.Lerp(previous,current,t)-basketball.hoop;
       float radius=new Vector2(crossing.x,crossing.z).magnitude;
       nearestCrossing=Mathf.Min(nearestCrossing,radius);crossed|=radius<=.1451f;
      }
      previous=current;
      if(shot==0&&!captured&&Time.time-began>.25f){
       captured=true;yield return new WaitForEndOfFrame();
       var picture=ScreenCapture.CaptureScreenshotAsTexture();
       if(!Check(picture,"Assisted basket screenshot contains a rendered frame"))yield break;
       try{File.WriteAllBytes("Logs/Basket-assisted.png",picture.EncodeToPNG());}
       finally{Object.Destroy(picture);}
      }
     }
     string detail=" at "+distances[shot]+"m / "+sides[shot]+" degrees (nearest crossing "+nearestCrossing.ToString("F3")+"m; ball "+basketball.ball.position+")";
     if(!Check(crossed&&basketball.baskets==beforeBaskets+1,"Real ball descends through the physical rim"+detail))yield break;
     if(!Check(game.data.money==beforeMoney+50,"Physical assisted basket awards exactly 50 coins"))yield break;
     for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();
     if(!Check(basketball.baskets==beforeBaskets+1&&game.data.money==beforeMoney+50,"One assisted throw cannot award coins twice"))yield break;
    }

    if(!Check(PickUpAtHome(game,basketball),"Ball can be retrieved after all assisted trajectories"))yield break;
    StandAndAim(game,basketball.hoop,5,0);
    var straight=player.view.transform.forward;var turnAxis=player.view.transform.up;
    Aim(game,Quaternion.AngleAxis(6,turnAxis)*straight);
    if(!Check(basketball.TryAssistedVelocity(Origin(game),out _),"Small six-degree aiming error receives assistance"))yield break;
    Aim(game,Quaternion.AngleAxis(8,turnAxis)*straight);
    if(!Check(!basketball.TryAssistedVelocity(Origin(game),out _),"Eight-degree aiming error remains an unaided shot"))yield break;
    foreach(float distance in new[]{1f,12f}){
     StandAndAim(game,basketball.hoop,distance,0);
     if(!Check(!basketball.TryAssistedVelocity(Origin(game),out _),"Assistance rejects distance "+distance+"m outside its range"))yield break;
    }
    StandAndAim(game,basketball.hoop,5,0);Aim(game,player.view.transform.position-basketball.hoop);
    if(!Check(!basketball.TryAssistedVelocity(Origin(game),out _),"Looking away from the hoop cannot snap a shot backwards"))yield break;

    StandAndAim(game,basketball.hoop,5,0);
    var launch=Origin(game);
    if(!Check(basketball.TryAssistedVelocity(launch,out var clearVelocity),"Clear five-metre trajectory is available before obstruction"))yield break;
    float horizontal=new Vector2(basketball.hoop.x-launch.x,basketball.hoop.z-launch.z).magnitude;
    float speed=new Vector2(clearVelocity.x,clearVelocity.z).magnitude;
    float halfway=horizontal/Mathf.Max(.01f,speed)*.5f;
    // A large enough test block avoids depending on an exact implementation of discrete integration.
    var middle=launch+clearVelocity*halfway+Physics.gravity*(halfway*(halfway+Time.fixedDeltaTime)*.5f);
    obstacle=Block("Basket test / solid arc obstacle",middle,Vector3.one*.7f);
    Physics.SyncTransforms();
    if(!Check(!basketball.TryAssistedVelocity(launch,out _),"A solid obstruction in the middle of the arc disables assistance"))yield break;
    obstacle.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();
    if(!Check(basketball.TryAssistedVelocity(launch,out _),"Non-solid trigger volumes do not obstruct assisted flight"))yield break;
    Remove(ref obstacle);

    var near=player.view.transform.TransformPoint(new Vector3(0,-.06f,.40f));
    obstacle=Block("Basket test / release obstruction",near,new Vector3(.6f,.6f,.12f));Physics.SyncTransforms();
    int moneyBeforeBlocked=game.data.money,basketsBeforeBlocked=basketball.baskets;
    basketball.Throw();
    if(!Check(basketball.Held&&basketball.ball.isKinematic&&basketball.ball.transform.parent==player.view.transform,"Blocked release keeps the ball in the player's hand"))yield break;
    if(!Check(game.data.money==moneyBeforeBlocked&&basketball.baskets==basketsBeforeBlocked,"Blocked release cannot award a basket"))yield break;
    Remove(ref obstacle);Passed=true;
   }
   finally{
    Remove(ref obstacle);
    if(basketball&&basketball.ball)basketball.ResetBall();
    if(game&&player){
     player.body.enabled=false;player.transform.position=oldPosition;player.transform.rotation=oldRotation;
     player.pitch=oldPitch;player.view.transform.localRotation=Quaternion.Euler(oldPitch,0,0);player.view.fieldOfView=oldFov;
     player.body.enabled=oldBody;player.Lock(!oldPointer);Physics.SyncTransforms();
     if(oldPaused&&!game.Paused)game.TogglePause();
    }
   }
  }

  static Vector3 Origin(CafeGame game)=>game.player.view.transform.TransformPoint(ReleaseOffset);
  static bool PickUpAtHome(CafeGame game,CafeBasketball basketball){
   basketball.ResetBall();var home=basketball.ball.position;
   Place(game,new Vector3(home.x,.1f,home.z+1));
   return basketball.PickUp();
  }
  static void StandAndAim(CafeGame game,Vector3 hoop,float distance,float degrees){
   float a=degrees*Mathf.Deg2Rad;
   Place(game,new Vector3(hoop.x+Mathf.Sin(a)*distance,.1f,hoop.z+Mathf.Cos(a)*distance));
   Aim(game,hoop-game.player.view.transform.position);
  }
  static void Place(CafeGame game,Vector3 at){
   game.player.body.enabled=false;game.player.transform.position=at;game.player.body.enabled=true;Physics.SyncTransforms();
  }
  static void Aim(CafeGame game,Vector3 direction){
   direction.Normalize();float horizontal=new Vector2(direction.x,direction.z).magnitude;
   game.player.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,0);
   game.player.pitch=-Mathf.Atan2(direction.y,horizontal)*Mathf.Rad2Deg;
   game.player.view.transform.localRotation=Quaternion.Euler(game.player.pitch,0,0);Physics.SyncTransforms();
  }
  static GameObject Block(string title,Vector3 position,Vector3 size){
   var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name=title;block.transform.position=position;block.transform.localScale=size;block.GetComponent<Renderer>().enabled=false;return block;
  }
  static void Remove(ref GameObject obstacle){if(!obstacle)return;obstacle.SetActive(false);Object.Destroy(obstacle);obstacle=null;Physics.SyncTransforms();}
 }
}
