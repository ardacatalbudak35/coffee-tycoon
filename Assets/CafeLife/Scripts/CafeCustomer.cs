using UnityEngine;

namespace CafeLife
{
    public sealed class CafeCustomer : MonoBehaviour
    {
        public enum State { Entering, Waiting, ToPickup, ToSeat, Drinking, Leaving }
        public State state;
        public CafeGame game;
        public int identity,persona,qualityBonus; public bool vip; public int slot,recipe,seat=-1,orderNumber,queueSlot;
        public Vector3 QueuePosition=>new Vector3(-3.4f+queueSlot*1.7f,0,3.4f);
        public bool accepted;
        public bool goodDrink=true;
        public int serviceQuality;public bool preferenceMet;public string feedback="";
        public float patience=100,waitSeconds;
        private float drink=7;
        private Vector3 waypoint;
        private TextMesh label;
        private Transform leftLeg,rightLeg,leftArm,rightArm;
        private Animator animator;
        private int route;
        private float speed,stride;
        public const float WalkSpeed=1.4f;
        static float walkClipSpeed=-1;
        // Moving/idle with hysteresis and walk clip playback matched to real speed, so feet don't slide or moonwalk.
        public static void Gait(Animator a,float velocity)
        {
            if(!a)return;
            bool moving=a.GetBool("Moving")?velocity>.05f:velocity>.15f; a.SetBool("Moving",moving);
            if(walkClipSpeed<0){walkClipSpeed=0;if(a.runtimeAnimatorController)foreach(var clip in a.runtimeAnimatorController.animationClips)if(clip&&clip.name.Contains("Walk")){walkClipSpeed=clip.averageSpeed.magnitude*(a.isHuman?a.humanScale:1);break;}if(walkClipSpeed<.3f||walkClipSpeed>3)walkClipSpeed=WalkSpeed;}
            a.speed=moving?Mathf.Clamp(velocity/walkClipSpeed,.6f,1.4f):1;
            // Safety net: a non-looping walk clip would freeze on its last frame.
            if(moving&&!a.IsInTransition(0)){var info=a.GetCurrentAnimatorStateInfo(0);if(info.IsName("Walk")&&info.normalizedTime>=1)a.Play(info.fullPathHash,0,0);}
        }
        bool Passing=>(state==State.Entering&&route<2)||(state==State.ToSeat&&path!=null&&route<path.Length-1)||(state==State.Leaving&&path!=null&&route<path.Length-1);
        private Vector3[] path;
        public static Vector3[] SeatRoute(CafeWorld world,int seat)
        {
            Vector3 table=world.TableCenter(seat/2);if(world.game.expansion&&world.game.expansion.Data.layout==1)table.z+=(seat/2)%2==0?.3f:-.3f;
            float aisle=table.x+(seat%2==0?-1.65f:1.65f);Vector3 destination=world.Seat(seat);destination.y=0;
            return new[]{new Vector3(6.4f,0,3.6f),new Vector3(6.4f,0,1.6f),new Vector3(aisle,0,1.6f),new Vector3(aisle,0,table.z),destination};
        }
        private Transform[] sitBones;
        private Quaternion[] sitRest;
        public void Setup()
        {
            transform.position=new Vector3(-1.5f+(slot%4),0,-13.5f-(slot/4)*.6f);
            for(int q=0;q<3;q++){bool occupied=false;foreach(var other in game.customers)if(other&&other!=this&&(other.state==State.Entering||other.state==State.Waiting)&&other.queueSlot==q)occupied=true;if(!occupied){queueSlot=q;break;}}
            var choices=new System.Collections.Generic.List<int>();
            for(int i=0;i<Economy.Recipes.Length;i++) if((game.data.unlocked&(1<<i))!=0) choices.Add(i);
            recipe=choices[Random.Range(0,choices.Count)];
            if(game.expansion)game.expansion.Identify(this); gameObject.AddComponent<CafeSip>().Setup(this);
            var variants=game.customerPrefabs;
            if(variants!=null && variants.Length>0)
            {
                var prefab=variants[Random.Range(0,variants.Length)];
                if(prefab)
                {
                    var avatar=Instantiate(prefab,transform);
                    avatar.name="Kenney müşteri"; avatar.transform.localPosition=Vector3.zero;
                    avatar.transform.localRotation=Quaternion.identity; avatar.transform.localScale=Vector3.one;
                    foreach(Transform child in avatar.GetComponentsInChildren<Transform>(true))
                    { string prop=child.name.ToLowerInvariant(); if(prop.Contains("baseball")||prop.Contains("knife")||prop.Contains("handgun")||prop.Contains("weapon")||prop.Contains("sword")||prop.Contains("axe"))child.gameObject.SetActive(false); }
                    foreach(var collider in avatar.GetComponentsInChildren<Collider>()) Destroy(collider);
                    game.world.PrepareAvatar(avatar);
                    animator=avatar.GetComponentInChildren<Animator>();
                    if(animator)
                    {
                        animator.applyRootMotion=false;
                        if(game.customerAnimator) animator.runtimeAnimatorController=game.customerAnimator;
                        animator.SetBool("Moving",false);
                    }
                }
            }
            if(!animator)
            {
                Color shirt=Color.HSVToRGB(Random.value,.45f,.7f);
                Part("Torso",PrimitiveType.Capsule,new Vector3(0,1.06f,0),new Vector3(.44f,.38f,.3f),shirt);
                Part("Baş",PrimitiveType.Sphere,new Vector3(0,1.62f,0),Vector3.one*.34f,new Color(.77f,.52f,.36f));
                Part("Saç",PrimitiveType.Sphere,new Vector3(0,1.73f,-.025f),new Vector3(.35f,.18f,.33f),new Color(.14f,.08f,.05f));
                leftLeg=Limb("Sol kalça",Part("Sol bacak",PrimitiveType.Capsule,new Vector3(-.12f,.4f,0),new Vector3(.16f,.36f,.18f),new Color(.13f,.17f,.24f)),.36f);
                rightLeg=Limb("Sağ kalça",Part("Sağ bacak",PrimitiveType.Capsule,new Vector3(.12f,.4f,0),new Vector3(.16f,.36f,.18f),new Color(.13f,.17f,.24f)),.36f);
                leftArm=Limb("Sol omuz",Part("Sol kol",PrimitiveType.Capsule,new Vector3(-.29f,1.02f,0),new Vector3(.13f,.3f,.13f),shirt),.26f);
                rightArm=Limb("Sağ omuz",Part("Sağ kol",PrimitiveType.Capsule,new Vector3(.29f,1.02f,0),new Vector3(.13f,.3f,.13f),shirt),.26f);
            }
            var col=gameObject.AddComponent<CapsuleCollider>(); col.center=Vector3.up*.9f; col.height=1.8f; col.radius=.28f; col.isTrigger=true;
            var target=gameObject.AddComponent<CafeTarget>(); target.customer=this;
            var aim=new GameObject("Müşteri etkileşim alanı"); aim.layer=8; aim.transform.SetParent(transform,false); aim.transform.localPosition=Vector3.up*.95f;
            var aimCollider=aim.AddComponent<SphereCollider>(); aimCollider.isTrigger=true; aimCollider.radius=.78f;
            var text=new GameObject("Sipariş balonu"); text.transform.SetParent(transform,false); text.transform.localPosition=Vector3.up*2.05f;
            label=text.AddComponent<TextMesh>(); label.fontSize=48; label.characterSize=.014f; label.anchor=TextAnchor.MiddleCenter; label.color=Color.white;label.gameObject.AddComponent<CafeDepthLabel>().Setup(label);
            waypoint=new Vector3(0,0,-10.2f); state=State.Entering;
        }
        Transform Part(string n,PrimitiveType type,Vector3 pos,Vector3 scale,Color color)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=n; obj.transform.SetParent(transform,false); obj.transform.localPosition=pos; obj.transform.localScale=scale;
            Destroy(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial=game.world.ColorMaterial(color); return obj.transform;
        }
        // Pivot at the top of the capsule (hip/shoulder) so limbs swing from the joint instead of their middle.
        Transform Limb(string n,Transform part,float up)
        { var pivot=new GameObject(n).transform; pivot.SetParent(transform,false); pivot.localPosition=part.localPosition+Vector3.up*up; part.SetParent(pivot,true); return pivot; }
        void SwingLimbs(float velocity)
        {
            if(!leftLeg||!rightLeg)return;
            stride+=velocity*Time.deltaTime*5.2f; float amount=Mathf.Clamp01(velocity/WalkSpeed),s=Mathf.Sin(stride),leg=s*28*amount,arm=s*22*amount;
            leftLeg.localRotation=Quaternion.Euler(leg,0,0); rightLeg.localRotation=Quaternion.Euler(-leg,0,0);
            if(leftArm&&rightArm){leftArm.localRotation=Quaternion.Euler(-arm,0,0);rightArm.localRotation=Quaternion.Euler(arm,0,0);}
        }
        public string Label(CafeGame g) { return state==State.Waiting?(accepted?"Sipariş #"+orderNumber+" · ":"Sipariş al · ")+Economy.Recipes[recipe].name+" [E]":"Müşteri"; }
        public void Interact(CafeGame g)
        {
            if(state!=State.Waiting) return;
            if(!accepted) { accepted=true; orderNumber=++g.orderCounter; }
            if(g.cup==null){g.selected=recipe;var mode=g.GetComponent<CafeFocusedMode>();if(mode)mode.selectedOrder=this;}
            var r=Economy.Recipes[recipe]; var steps=new System.Collections.Generic.List<string>();
            steps.Add(r.cold?"Soğuk bardak":"Sıcak bardak"); if(r.cold)steps.Add("buz"); if(r.syrup!="")steps.Add(r.syrup=="vanilla"?"vanilya şurubu":"karamel şurubu");
            steps.Clear();steps.Add(r.cold?"Soğuk bardak":"Sıcak bardak");foreach(var step in CafeGame.IdealSteps(r))steps.Add(CafeUI.StepName(step));
            string order="Sipariş: "+r.name;
            if(g.noticePinned&&g.notice.StartsWith(order)) { g.Tell(""); return; }
            g.TellRecipe(order+"\n"+CafeService.PreferenceLabel(this)+"\nTarif: "+string.Join(" → ",steps.ToArray())+"\n(E’ye tekrar basınca kapatılır)");
        }
        public void Deliver(bool staff,bool good=true)
        {
            if(state!=State.Waiting) return;
            goodDrink=good;
            state=State.ToPickup; route=0; waypoint=CafeWorld.PickupPoint;
        }
        void Collect()
        {
            game.TakeReservedDrink(this);
            int payment=game.expansion?game.expansion.CompleteSale(this):Economy.Sale(game.data,recipe,goodDrink);
            game.Save(); game.Tell(CafeExpansion.Names[identity]+" · Kalite "+serviceQuality+" / 100 · +"+payment+" coin\n"+feedback,goodDrink);
            for(int i=0;i<game.data.tables*2;i++) { bool occupied=false; foreach(var c in game.customers) if(c!=this && c.seat==i) occupied=true; if(!occupied) { seat=i; break; } }
            if(seat>=0) { state=State.ToSeat; route=0;path=SeatRoute(game.world,seat);waypoint=path[0]; } else Leave();
        }
        bool HasSitting() { if(!animator)return false; foreach(var p in animator.parameters)if(p.name=="Sitting")return true; return false; }
        void Leave()
        {
            if(HasSitting())animator.SetBool("Sitting",false);route=0;
            if(seat>=0){Vector3[] inward=SeatRoute(game.world,seat);path=new[]{inward[3],new Vector3(inward[3].x,0,-8.9f),new Vector3(0,0,-8.9f),new Vector3(0,0,-10.5f),new Vector3(0,0,-14)};}
            else path=new[]{new Vector3(transform.position.x,0,-14)};
            state=State.Leaving;waypoint=path[0];seat=-1;
        }
        void Update()
        {
            if(!this || !game || !game.world) return;
            if(game.Paused){if(animator)animator.speed=0;return;}
            if(animator&&(state==State.Waiting||state==State.Drinking))animator.speed=1;
            if(state==State.Leaving&&path==null)Leave();
            if(label){bool visible=state==State.Waiting&&!game.Paused&&(game.player.transform.position-transform.position).sqrMagnitude<30.25f;label.gameObject.SetActive(visible);if(visible){label.transform.rotation=game.player.view.transform.rotation;label.text=(accepted?"#"+orderNumber:"Sipariş")+(vip?" ★":"")+" · "+CafeExpansion.Names[identity]+"\n"+Economy.Recipes[recipe].name;label.color=patience<25?new Color(1,.65f,.4f):new Color(.96f,.93f,.85f);}}
            if(state==State.Waiting)
            {
                if(game.TryMatchShelf(this)) return;
                waitSeconds+=Time.deltaTime;patience-=Time.deltaTime*Mathf.Max(.35f,1.1f-game.data.decor*.06f)*(vip?.85f:1)*(game.expansion?game.expansion.PatienceMultiplier(persona):1);
                if(patience<=0) { game.data.missed++; game.data.reputation=Mathf.Max(0,game.data.reputation-5); game.Save(); game.Tell("Bir müşteri beklemekten vazgeçti."); Leave(); }
                return;
            }
            if(state==State.Drinking) { drink-=Time.deltaTime; if(drink<=0) { if(Random.value<(goodDrink?.35f:.05f)) { int tip=Random.Range(2,9); game.data.money+=tip; game.data.earned+=tip;if(game.expansion)game.expansion.Data.dayIncome+=tip; game.Tell("Masada "+tip+" coin bahşiş bırakıldı!",true); game.Save(); } transform.position=new Vector3(transform.position.x,0,transform.position.z); Leave(); } return; }
            Vector3 delta=waypoint-transform.position; delta.y=0;
            // Corner waypoints are passed with a wider radius so the walk rounds turns instead of stopping at each one.
            bool passing=Passing; float distance=delta.magnitude;
            if(distance>(passing?.3f:.06f))
            {
                bool yield=false;foreach(var other in game.customers)if(other&&other!=this&&other.slot<slot&&other.state!=State.Drinking&&other.state!=State.Waiting&&Vector3.Distance(other.transform.position,transform.position)<.55f){yield=true;break;}
                Vector3 heading=delta/distance;
                float target=yield?0:WalkSpeed*Mathf.Lerp(.4f,1,Mathf.Clamp01(Vector3.Dot(transform.forward,heading)*1.4f));
                if(!passing)target*=Mathf.Lerp(.35f,1,Mathf.Clamp01(distance/.7f));
                speed=Mathf.MoveTowards(speed,target,Time.deltaTime*(target>speed?2.8f:5f));
                transform.position=Vector3.MoveTowards(transform.position,waypoint,Time.deltaTime*speed);
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),Time.deltaTime*Mathf.Lerp(220,420,speed/WalkSpeed));
                Gait(animator,speed); SwingLimbs(speed);
                return;
            }
            if(!passing){speed=0;Gait(animator,0);SwingLimbs(0);}
            if(state==State.Entering) { if(route==0){route++;waypoint=new Vector3(0,0,1.6f);}else if(route==1){route++;waypoint=QueuePosition;}else {state=State.Waiting;game.TryMatchShelf(this);} }
            else if(state==State.ToPickup) Collect();
            else if(state==State.ToSeat) { if(++route<path.Length)waypoint=path[route];else { state=State.Drinking; drink=Random.Range(14f,48f); transform.position=game.world.Seat(seat); Vector3 tableDirection=game.world.TableCenter(seat/2)-transform.position; tableDirection.y=0; if(tableDirection.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(tableDirection); if(animator) { if(HasSitting())animator.SetBool("Sitting",true); else SetupSittingPose(); } } }
            else if(state==State.Leaving) { if(++route<path.Length)waypoint=path[route];else {game.customers.Remove(this);Destroy(gameObject);} }
        }
        void SetupSittingPose()
        {
            if(!animator.isHuman)return;
            var bones=new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
            sitBones=new Transform[bones.Length]; sitRest=new Quaternion[bones.Length];
            for(int i=0;i<bones.Length;i++) { sitBones[i]=animator.GetBoneTransform(bones[i]); if(sitBones[i])sitRest[i]=sitBones[i].localRotation; }
        }
        void LateUpdate()
        {
            if(state!=State.Drinking)return;
            if(animator&&animator.isHuman&&game.GetComponent<CafePolish>()){var hips=animator.GetBoneTransform(HumanBodyBones.Hips);if(hips)transform.position+=Vector3.up*(.59f-hips.position.y);}
            if(sitBones==null) { if(leftLeg)leftLeg.localRotation=Quaternion.Euler(-68,0,0); if(rightLeg)rightLeg.localRotation=Quaternion.Euler(-68,0,0); return; }
            // The demo pack has no sitting clip; bend hips, knees and feet after the idle pose is evaluated.
            var pose=new[]{Quaternion.Euler(-68,0,0),Quaternion.Euler(-68,0,0),Quaternion.Euler(112,0,0),Quaternion.Euler(112,0,0),Quaternion.Euler(-38,0,0),Quaternion.Euler(-38,0,0)};
            for(int i=0;i<sitBones.Length;i++) if(sitBones[i])sitBones[i].localRotation=sitRest[i]*pose[i];
        }
    }
}





