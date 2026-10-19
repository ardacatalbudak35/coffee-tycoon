using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    public sealed class CafeCityLife : MonoBehaviour
    {
        CafeExpansion expansion; CafeGame Game=>expansion.game;
        readonly List<Transform> traffic=new List<Transform>();
        readonly List<CafeWalker> walkers=new List<CafeWalker>();
        ParticleSystem rain; Light sun; Light[] lights;
        public void Setup(CafeExpansion owner)
        {
            expansion=owner;
            foreach(Transform child in Game.world.transform)if(child.name.StartsWith("Gerçek ölçekte"))traffic.Add(child);
            lights=Game.world.GetComponentsInChildren<Light>(); foreach(var light in lights)if(light.type==LightType.Directional)sun=light;
            var indoor=new GameObject("Kafe tavan aydınlatması");indoor.transform.SetParent(Game.world.transform);indoor.transform.position=new Vector3(0,3,-1);var point=indoor.AddComponent<Light>();point.type=LightType.Point;point.range=18;point.intensity=2.2f;point.color=new Color(1,.84f,.6f);
            var r=new GameObject("Yağmur");r.transform.SetParent(Game.world.transform);rain=r.AddComponent<ParticleSystem>();var main=rain.main;main.startLifetime=1.2f;main.startSpeed=16;main.startSize=.035f;main.startColor=new Color(.6f,.75f,.85f,.6f);main.maxParticles=600;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=rain.emission;emission.rateOverTime=200;var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(24,.1f,24);r.transform.rotation=Quaternion.Euler(90,0,0);rain.GetComponent<ParticleSystemRenderer>().sharedMaterial=Game.world.ColorMaterial(new Color(.6f,.75f,.85f,.65f));rain.Stop();
            if(CafeFocusedMode.Enabled)return;
            for(int i=0;i<6;i++)
            {
                var root=new GameObject("Mahalle yayası "+i);root.transform.SetParent(Game.world.transform);root.transform.position=new Vector3(-24+i*8,0,-29);
                var avatar=Instantiate(Game.customerPrefabs[i%Game.customerPrefabs.Length],root.transform);Game.world.PrepareAvatar(avatar);foreach(var col in avatar.GetComponentsInChildren<Collider>())Destroy(col);
                var walker=root.AddComponent<CafeWalker>();walker.expansion=expansion;walker.left=-24;walker.right=24;walker.direction=i%2==0?1:-1;walker.animator=avatar.GetComponentInChildren<Animator>();walkers.Add(walker);
            }
            int b=0;foreach(Transform child in Game.world.GetComponentsInChildren<Transform>())
            {
                if((child.name=="Bench01"||child.name=="bench") && child.parent==Game.world.transform && b++<8)
                {var marker=new GameObject("Dinlenme bankı");marker.transform.SetParent(Game.world.transform);marker.transform.position=child.position+Vector3.up;marker.layer=8;var collider=marker.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=1;marker.AddComponent<CafeTarget>().kind="bench";}
            }
            var taxi=new GameObject("Taksi durağı");taxi.transform.SetParent(Game.world.transform);taxi.transform.position=new Vector3(17,1,-14);taxi.layer=8;var taxiCol=taxi.AddComponent<SphereCollider>();taxiCol.isTrigger=true;taxiCol.radius=1.2f;taxi.AddComponent<CafeTarget>().kind="taxi";
            var sign=taxi.AddComponent<TextMesh>();sign.text="TAKSİ · 15 COIN\n[E]";sign.characterSize=.07f;sign.fontSize=48;sign.anchor=TextAnchor.MiddleCenter;sign.color=new Color(1,.82f,.3f);taxi.transform.rotation=Quaternion.Euler(0,180,0);
            for(int i=0;i<6;i++)CafeLitter.Create(expansion,new Vector3(-20+i*7,.05f,-25),false);
        }
        void Update()
        {
            if(!expansion || Game.data==null)return;
            float phase=Game.data.dayTime/300f;
            if(sun){sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(15,175,phase),-25,0);sun.intensity=phase>.82f?.12f:Mathf.Lerp(.7f,1.3f,Mathf.Sin(phase*Mathf.PI));sun.color=phase>.7f?new Color(1,.64f,.4f):new Color(1,.95f,.85f);}
            foreach(var light in lights)if(light&&light.type==LightType.Point)light.enabled=(light.name.StartsWith("NOIR")||light.name.Contains("vitrin"))||phase>.65f||light.transform.position.z>-12;
            RenderSettings.ambientLight=Color.Lerp(new Color(.22f,.27f,.4f),new Color(.7f,.72f,.75f),phase>.82f?.12f:.8f);
            bool outside=Game.player.transform.position.z<-11 && Game.player.transform.position.x<140;
            rain.transform.position=Game.player.transform.position+Vector3.up*14;
            if(expansion.Rainy&&outside&&!Game.Paused){if(!rain.isPlaying)rain.Play();}else if(rain.isPlaying)rain.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            if(Game.Paused)return;
            foreach(var car in traffic)
            {
                float direction=car.name.Contains("taxi")?-1:1;
                Vector3 ahead=car.position+Vector3.right*direction*4;
                bool blocked=Vector3.Distance(Game.player.transform.position,ahead)<4;
                foreach(var w in walkers)if(w&&Vector3.Distance(w.transform.position,ahead)<3)blocked=true;
                if(!blocked)car.position+=Vector3.right*direction*5*Time.deltaTime;
                if(car.position.x>66)car.position=new Vector3(-66,car.position.y,car.position.z);else if(car.position.x< -66)car.position=new Vector3(66,car.position.y,car.position.z);
            }
        }
    }
    public sealed class CafeWalker : MonoBehaviour
    {
        public CafeExpansion expansion;public float left,right,direction;public Animator animator;
        void Update(){if(!expansion)return;bool move=!expansion.game.Paused;if(animator)animator.SetBool("Moving",move);if(!move)return;transform.position+=Vector3.right*direction*1.2f*Time.deltaTime;if(transform.position.x>=right)direction=-1;if(transform.position.x<=left)direction=1;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Vector3.right*direction),Time.deltaTime*5);}
    }
    public sealed class CafeLitter : MonoBehaviour
    {
        public CafeExpansion expansion; public bool table;
        public static CafeLitter Create(CafeExpansion expansion,Vector3 position,bool table)
        {
            if(expansion.litter.Count>=30){var old=expansion.litter[0];expansion.litter.RemoveAt(0);if(old)Destroy(old.gameObject);}
            var go=expansion.game.world.Model(table?"Cup_With_Coffee":"Tray",position,table?.15f:.24f,0,false);go.name=table?"Masada bırakılan bardak":"Sokak çöpü";go.layer=8;
            var item=go.AddComponent<CafeLitter>();item.expansion=expansion;item.table=table;
            var trigger=go.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.45f;var target=go.AddComponent<CafeTarget>();target.kind="litter";target.litter=item;expansion.litter.Add(item);return item;
        }
        public void Clean()
        {if(!expansion)return;expansion.GetComponent<CafeHundred>()?.Clean();expansion.Data.cleanliness=Mathf.Min(100,expansion.Data.cleanliness+(table?5:2));expansion.Data.cleaned++;if(!table){expansion.game.data.money+=2;expansion.Data.dayIncome+=2;}expansion.litter.Remove(this);expansion.game.Tell(table?"Masa temizlendi.":"Sokak temizlendi. +2 coin",true);expansion.game.Save();gameObject.SetActive(false);Destroy(gameObject);}
    }
    public sealed class CafeSip : MonoBehaviour
    {
        CafeCustomer customer;GameObject cup;int previousSeat=-1;
        public void Setup(CafeCustomer owner){customer=owner;}
        void OnDestroy(){if(cup)Destroy(cup);}
        void Update()
        {
            if(!customer||!customer.game.expansion||customer.game.Paused)return;
            if(customer.state==CafeCustomer.State.Drinking)
            {
                previousSeat=customer.seat;
                if(!cup){var work=new CupWork{recipe=customer.recipe,coldCup=Economy.Recipes[customer.recipe].cold,brewed=true,lidded=true};cup=customer.game.world.CupModel(work,Economy.Recipes[customer.recipe],customer.game.world.transform,false);cup.transform.localScale=Vector3.one*.6f;}
                Vector3 table=customer.game.world.TableCenter(customer.seat/2);if(customer.game.expansion.Data.layout==1)table.z+=(customer.seat/2)%2==0?.3f:-.3f;
                cup.transform.position=table+new Vector3(customer.seat%2==0?-.28f:.28f,.89f,.12f);cup.transform.rotation=Quaternion.identity;
            }
            else if(previousSeat>=0)
            {if(cup)Destroy(cup);var p=customer.game.world.TableCenter(previousSeat/2)+Vector3.up*.9f;CafeLitter.Create(customer.game.expansion,p,true);customer.game.expansion.Data.cleanliness=Mathf.Max(0,customer.game.expansion.Data.cleanliness-2);previousSeat=-1;}
        }
    }
}

