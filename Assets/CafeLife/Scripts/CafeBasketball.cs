using UnityEngine;
using UnityEngine.InputSystem;
namespace CafeLife {
 public sealed class CafeBasketball:MonoBehaviour {
  public CafeGame game;public Rigidbody ball;public Vector3 hoop;public bool Held {get;private set;}public int baskets {get;private set;}
  public bool AssistedLastShot {get;private set;}
  float restingDamping;
  SphereCollider physical;Collider interaction;Vector3 home,previous,pausedVelocity,pausedSpin;bool paused,thrown,scored;float flight;
  public void Setup(CafeGame owner,Rigidbody body,Vector3 center){game=owner;ball=body;restingDamping=body.linearDamping;hoop=center;home=body.position;physical=body.GetComponent<SphereCollider>();foreach(var collider in body.GetComponentsInChildren<Collider>())if(collider.isTrigger)interaction=collider;previous=body.position;}
  void Start(){if(game&&game.player&&physical)Physics.IgnoreCollision(physical,game.player.body,true);}
  public bool PickUp(){if(!game||!game.player||game.player.WorldInputBlocked||Held||Vector3.Distance(game.player.transform.position,ball.position)>3.5f)return false;if(game.cup!=null){game.Tell("Önce elindeki kahveyi bırak.");return false;}Held=true;thrown=false;AssistedLastShot=false;ball.linearDamping=restingDamping;ball.linearVelocity=Vector3.zero;ball.angularVelocity=Vector3.zero;ball.isKinematic=true;physical.enabled=false;if(interaction)interaction.enabled=false;ball.gameObject.layer=2;ball.transform.SetParent(game.player.view.transform,true);game.Tell("Top elinde · Çembere nişan al · E / ETKİLEŞ ile at");return true;}
  public void Throw()
  {
   if(!Held||game.player.WorldInputBlocked)return;
   if(!TryReleaseOrigin(out var origin)){game.Tell("Atmak için önünde biraz yer aç.");return;}
   AssistedLastShot=TryAssistedVelocity(origin,out var velocity);
   if(!AssistedLastShot)velocity=(game.player.view.transform.forward+Vector3.up*.4f).normalized*10.5f;
   Held=false;ball.transform.SetParent(game.world.transform,true);ball.position=origin;
   physical.enabled=true;if(interaction)interaction.enabled=true;ball.gameObject.layer=8;
   // Negligible air drag: a predictable arc, with ground friction still handled by physics.
   ball.linearDamping=0;ball.isKinematic=false;ball.linearVelocity=velocity;
   ball.angularVelocity=game.player.view.transform.right*-9;previous=origin;thrown=true;scored=false;flight=0;
  }

  float BallRadius=>physical.radius*Mathf.Max(ball.transform.lossyScale.x,ball.transform.lossyScale.y,ball.transform.lossyScale.z);

  bool IsObstacle(Collider collider)
  {
   return collider&&!collider.isTrigger&&collider!=physical
    &&!collider.transform.IsChildOf(ball.transform)&&!collider.transform.IsChildOf(game.player.transform);
  }

  bool SolidBetween(Vector3 start,Vector3 end,float radius,out float distance)
  {
   Vector3 delta=end-start;distance=delta.magnitude;
   if(distance<.001f)return false;
   bool blocked=false;
   foreach(var hit in Physics.SphereCastAll(start,radius,delta/distance,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
    if(IsObstacle(hit.collider)&&hit.distance<distance){distance=hit.distance;blocked=true;}
   return blocked;
  }

  bool TryReleaseOrigin(out Vector3 origin)
  {
   var camera=game.player.view.transform;
   Vector3 wanted=camera.TransformPoint(new Vector3(0,-.12f,.8f));
   Vector3 direction=(wanted-camera.position).normalized;
   origin=wanted;
   if(SolidBetween(camera.position,wanted,BallRadius+.01f,out float clearance))
   {
    if(clearance<.25f)return false;
    origin=camera.position+direction*Mathf.Max(0,clearance-.025f);
   }
   foreach(var collider in Physics.OverlapSphere(origin,BallRadius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
    if(IsObstacle(collider))return false;
   return true;
  }

  public bool TryAssistedVelocity(Vector3 origin,out Vector3 velocity)
  {
   velocity=Vector3.zero;
   if(!game||!game.player||game.player.WorldInputBlocked||!ball||Physics.gravity.y>=-.01f)return false;
   var camera=game.player.view.transform;
   Vector3 delta=hoop-camera.position;
   float range=new Vector2(delta.x,delta.z).magnitude;
   if(range<2||range>11||camera.position.z<hoop.z+.6f||Vector3.Angle(camera.forward,delta)>7)return false;

   float gravity=-Physics.gravity.y;
   float apex=Mathf.Max(origin.y,hoop.y)+1.25f+range*.04f;
   float time=Mathf.Sqrt(2*(apex-origin.y)/gravity)+Mathf.Sqrt(2*(apex-hoop.y)/gravity);
   float step=Time.fixedDeltaTime;
   time=Mathf.Ceil(time/step)*step;
   // PhysX applies gravity before advancing position on every fixed tick.
   velocity=(hoop-origin-.5f*Physics.gravity*time*(time+step))/time;
   Vector3 previousPoint=origin;
   int segments=Mathf.CeilToInt(time/.08f);
   for(int i=1;i<=segments;i++)
   {
    float t=time*i/segments;
    Vector3 point=origin+velocity*t+.5f*Physics.gravity*t*(t+step);
    if(SolidBetween(previousPoint,point,BallRadius+.01f,out _)){velocity=Vector3.zero;return false;}
    previousPoint=point;
   }
   return true;
  }
  public void Interact(){if(Held)Throw();else PickUp();}
  void Update(){if(!game||!game.player||!ball)return;bool blocked=game.player.WorldInputBlocked;if(blocked&&!paused&&!Held){pausedVelocity=ball.linearVelocity;pausedSpin=ball.angularVelocity;ball.isKinematic=true;paused=true;}else if(!blocked&&paused){ball.isKinematic=Held;paused=false;if(!Held){ball.linearVelocity=pausedVelocity;ball.angularVelocity=pausedSpin;}}if(Held){ball.transform.localPosition=new Vector3(.28f,-.28f,.62f);ball.transform.localRotation=Quaternion.Euler(12,0,0);if(!blocked&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&!game.player.PointerMode&&!game.player.OverInterface(Mouse.current.position.ReadValue()))Throw();}}
  public bool CheckBasket(Vector3 from,Vector3 to){if(!thrown||Held||scored||game.player.WorldInputBlocked||from.y<=hoop.y||to.y>hoop.y||to.y>=from.y)return false;float t=(from.y-hoop.y)/(from.y-to.y);Vector3 crossing=Vector3.Lerp(from,to,t)-hoop;if(new Vector2(crossing.x,crossing.z).magnitude>.145f)return false;scored=true;baskets++;game.data.hundred.basketBest=Mathf.Max(game.data.hundred.basketBest,baskets);game.data.money+=50;if(game.expansion)game.expansion.Data.dayIncome+=50;game.Save();game.Tell("Basket! +50 coin",true);return true;}
  void FixedUpdate(){if(!game||!game.player||!ball||Held||paused||game.player.WorldInputBlocked)return;CheckBasket(previous,ball.position);previous=ball.position;if(thrown)flight+=Time.fixedDeltaTime;if(ball.position.y<-.8f||ball.position.y>20||Mathf.Abs(ball.position.x)>35||ball.position.z< -43||ball.position.z> -20||flight>35)ResetBall();}
  public void ResetBall(){AssistedLastShot=false;ball.linearDamping=restingDamping;Held=false;paused=false;thrown=false;scored=false;flight=0;physical.enabled=true;if(interaction)interaction.enabled=true;ball.gameObject.layer=8;ball.transform.SetParent(game.world.transform,true);ball.isKinematic=false;ball.linearVelocity=Vector3.zero;ball.angularVelocity=Vector3.zero;ball.position=home;previous=home;}
 }
}
