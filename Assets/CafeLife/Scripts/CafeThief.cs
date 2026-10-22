using UnityEngine;

namespace CafeLife
{
    // Hırsız (Mixamo "Ninja"): kafe açıkken içeri süzülür, kasadan para kapar ve kaçar. Kaçmadan yakalarsan para geri gelir + ödül.
    public sealed class CafeThief : MonoBehaviour
    {
        enum State { Idle, Entering, Stealing, Fleeing }
        State state=State.Idle; CafeGame game; GameObject body; Animator anim; float cooldown=100f,wait; int stolen,route; Vector3[] path; float speed=2.6f;
        static readonly Vector3 Loot=new Vector3(3.9f,0,3.0f);
        public static readonly Vector3[] Inward={new Vector3(0,0,-14f),new Vector3(0,0,-10.2f),new Vector3(0,0,1.6f),Loot};
        public void Setup(CafeGame owner){game=owner;}
        public bool Active=>state!=State.Idle;

        public string Label()=>state==State.Fleeing||state==State.Stealing?"Hırsız! Yakala [E]":"Şüpheli biri · yakala [E]";
        public void Use()
        {
            if(state==State.Idle)return;
            game.data.money+=stolen; game.data.earned+=stolen; int bonus=40; game.data.money+=bonus; game.data.earned+=bonus;
            game.Tell("Hırsızı yakaladın! "+(stolen>0?stolen+" coin geri aldın, ":"")+bonus+" coin ödül kazandın.",true); game.Save(); Clear();
        }
        void Clear(){ if(body)Destroy(body); body=null; state=State.Idle; stolen=0; cooldown=Random.Range(150f,300f); }

        void Update()
        {
            if(!game||game.Paused||CafeStory.Busy||game.expansion==null)return;
            if(state==State.Idle)
            {
                if(!game.expansion.Data.open||game.data.day<2||game.data.dayTime>=290f)return;
                cooldown-=Time.deltaTime*(game.data.choice2==2?2f:1f); if(cooldown>0)return; Spawn(); return;   // Kemal'le pazarlık ettiysen hırsız iki kat sık gelir
            }
            if(state==State.Stealing){ wait-=Time.deltaTime; if(wait<=0)Steal(); return; }
            Move();
        }

        void Spawn()
        {
            var prefab=Resources.Load<GameObject>("CafeMixamo/Ninja"); if(!prefab){cooldown=600f;return;}
            body=new GameObject("Hırsız"); body.transform.SetParent(game.world.transform,false); body.transform.position=Inward[0];
            var avatar=Instantiate(prefab,body.transform); game.world.PrepareAvatar(avatar); foreach(var c in avatar.GetComponentsInChildren<Collider>())Destroy(c);
            anim=avatar.GetComponentInChildren<Animator>(); if(anim){anim.applyRootMotion=false; var ctrl=CafeResidents.MixamoController(); if(ctrl)anim.runtimeAnimatorController=ctrl;}
            var hit=new GameObject("Hırsız etkileşim alanı"); hit.layer=8; hit.transform.SetParent(body.transform,false); hit.transform.localPosition=Vector3.up*.95f;
            var col=hit.AddComponent<CapsuleCollider>(); col.isTrigger=true; col.height=1.9f; col.radius=.55f; hit.AddComponent<CafeTarget>().kind="cc_thief";
            path=Inward; route=1; state=State.Entering; speed=1.9f; game.Tell("Kapıdan şüpheli biri sızdı…");
        }
        void Steal()
        {
            int take=Mathf.Min(game.data.money,Random.Range(15,36)); game.data.money-=take; stolen=take;
            game.Tell(take>0?"Biri kasadan "+take+" coin kaptı! Yakala!":"Biri kasayı karıştırdı ama bir şey bulamadı. Yakala!",true);
            var back=new Vector3[Inward.Length]; for(int i=0;i<Inward.Length;i++)back[i]=Inward[Inward.Length-1-i]; path=back; route=1; state=State.Fleeing; speed=3.2f;
        }
        void Move()
        {
            if(!body)return; var target=path[Mathf.Min(route,path.Length-1)]; var d=target-body.transform.position; d.y=0;
            bool moving=d.magnitude>.08f;
            if(moving)
            {
                body.transform.position=Vector3.MoveTowards(body.transform.position,target,speed*Time.deltaTime);
                body.transform.rotation=Quaternion.RotateTowards(body.transform.rotation,Quaternion.LookRotation(d.normalized),420f*Time.deltaTime);
            }
            else if(++route>=path.Length)
            {
                if(state==State.Entering){ state=State.Stealing; wait=1.8f; moving=false; }
                else { if(stolen>0)game.Tell("Hırsız kaçtı."); Clear(); return; }
            }
            if(anim){anim.SetBool("Moving",moving&&state!=State.Stealing); anim.speed=state==State.Fleeing?1.4f:1f;}
        }
    }
}
