using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    // NOIR kulübü: gerçek dans animasyonlu kalabalık, bar sohbeti, ritme göre yanıp sönen zemin, disko topu ve dönen ışıklar.
    public sealed class CafeNightlife:MonoBehaviour
    {
        CafeGame game; AudioSource music; Transform ball,sweep; Renderer[] tiles; Material[] tileMats; Vector2Int[] tileCell; List<Transform> eq=new List<Transform>();
        static readonly Color[] Neon={new Color(1f,.1f,.75f),new Color(.1f,.85f,1f),new Color(.55f,.2f,1f),new Color(.25f,.45f,1f)};
        static readonly Vector3 Booth=new Vector3(14,.36f,-39.6f);

        public void Setup(CafeGame owner)
        {
            game=owner; SpawnPeople(); BuildLights(); BuildMusic(); CollectTiles();
            foreach(var t in game.world.GetComponentsInChildren<Transform>())if(t.name=="DJ ekran ses çubuğu")eq.Add(t);
        }

        static bool HasParam(Animator a,string n){ foreach(var p in a.parameters)if(p.name==n)return true; return false; }

        void Person(string name,Vector3 pos,float yaw,string mood,float speed)
        {
            var variants=game.customerPrefabs; if(variants==null||variants.Length==0)return;
            var root=new GameObject(name); root.transform.SetParent(game.world.transform,false); root.transform.position=pos; root.transform.rotation=Quaternion.Euler(0,yaw,0);
            var avatar=Instantiate(variants[Random.Range(0,variants.Length)],root.transform); game.world.PrepareAvatar(avatar);
            foreach(Transform t in avatar.GetComponentsInChildren<Transform>(true))
            { string n=t.name.ToLowerInvariant(); if(n.Contains("baseball")||n.Contains("knife")||n.Contains("handgun")||n.Contains("weapon")||n.Contains("sword")||n.Contains("axe"))t.gameObject.SetActive(false); }
            foreach(var c in avatar.GetComponentsInChildren<Collider>())Destroy(c);
            var anim=avatar.GetComponentInChildren<Animator>(); if(!anim)return;
            anim.applyRootMotion=false; if(game.customerAnimator)anim.runtimeAnimatorController=game.customerAnimator;
            anim.SetBool("Moving",false);
            if(mood!=null&&HasParam(anim,mood))
            {
                anim.SetBool(mood,true); anim.speed=speed;
                int hash=Animator.StringToHash(mood=="Dancing"?"Dance":"Talking"); if(anim.HasState(0,hash))anim.Play(hash,0,Random.value); // herkes aynı anda aynı kareyi oynamasın
            }
        }

        void SpawnPeople()
        {
            var rng=new System.Random(11); float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
            var spots=new List<Vector2>();
            for(int tries=0;tries<400&&spots.Count<9;tries++)
            {
                var p=new Vector2(R(10.8f,15.6f),R(-32.6f,-28f)); bool ok=true; foreach(var s in spots)if((s-p).sqrMagnitude<1.35f*1.35f){ok=false;break;}
                if(ok)spots.Add(p);
            }
            int i=0; foreach(var p in spots)
            {
                float yaw=Mathf.Atan2(14-p.x,-39.6f-p.y)*Mathf.Rad2Deg+R(-35f,35f); Person("NOIR dansçı "+i++,new Vector3(p.x,0,p.y),yaw,"Dancing",R(.88f,1.18f));
            }
            Person("NOIR DJ",Booth,0,"Dancing",.55f);                                   // DJ kafa sallar
            Person("NOIR barmen",new Vector3(7,0,-39.3f),0,null,1f);                      // barın arkasında
            Person("NOIR bar sohbeti 0",new Vector3(5.6f,0,-36.5f),170,"Talking",1f); Person("NOIR bar sohbeti 1",new Vector3(6.5f,0,-36.2f),-10,"Talking",1.05f);
            Person("NOIR bar sohbeti 2",new Vector3(8.7f,0,-36.5f),185,"Talking",.95f);
            Person("NOIR kapı görevlisi",new Vector3(17f,0,-24.2f),20,null,1f);
        }

        void BuildLights()
        {
            var at=new Vector3(14,3.3f,-30.2f);
            var b=GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name="NOIR disko topu"; b.transform.SetParent(game.world.transform,false); b.transform.position=at; b.transform.localScale=Vector3.one*.85f; Destroy(b.GetComponent<Collider>());
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",new Color(.75f,.78f,.85f)); m.SetFloat("_Metallic",1f); m.SetFloat("_Smoothness",.95f); b.GetComponent<Renderer>().sharedMaterial=m; ball=b.transform;
            var rod=GameObject.CreatePrimitive(PrimitiveType.Cylinder); rod.name="NOIR disko topu askısı"; rod.transform.SetParent(game.world.transform,false); rod.transform.position=at+Vector3.up*.55f; rod.transform.localScale=new Vector3(.03f,.28f,.03f); Destroy(rod.GetComponent<Collider>());
            // karanlık köşeler: lounge, bar, pist kenarları ve casino için renkli noktasal ışıklar
            void Point(Vector3 p,Color c,float i,float r){ var l=new GameObject("NOIR köşe ışığı").AddComponent<Light>(); l.transform.SetParent(game.world.transform,false); l.transform.position=p; l.type=LightType.Point; l.color=c; l.intensity=i; l.range=r; }
            Point(new Vector3(6.5f,2.6f,-29.5f),Neon[2],9,7); Point(new Vector3(7,2.8f,-36.8f),new Color(1f,.7f,.4f),8,6); Point(new Vector3(10.5f,1.8f,-30.2f),Neon[0],7,6); Point(new Vector3(17.5f,1.8f,-30.2f),Neon[1],7,6);
            Point(new Vector3(7,6.6f,-29.5f),new Color(1f,.75f,.4f),9,8); Point(new Vector3(13,6.6f,-26f),new Color(1f,.75f,.4f),9,8);
            sweep=new GameObject("NOIR dönen ışıklar").transform; sweep.SetParent(game.world.transform,false); sweep.position=at;
            for(int i=0;i<4;i++)
            {
                var l=new GameObject("Tarama ışığı").AddComponent<Light>(); l.transform.SetParent(sweep,false); l.type=LightType.Spot; l.spotAngle=34; l.range=9; l.intensity=45; l.color=Neon[i];
                l.transform.localRotation=Quaternion.Euler(52,i*90,0);
            }
        }

        void BuildMusic()
        {
            var audio=new GameObject("NOIR müzik kaynağı"); audio.transform.SetParent(game.world.transform); audio.transform.position=new Vector3(14,1,-34); music=audio.AddComponent<AudioSource>();
            int rate=22050; var clip=AudioClip.Create("NOIR özgün ritim",rate*4,1,rate,false); var samples=new float[rate*4];
            float[] arp={220f,261.6f,329.6f,392f,329.6f,261.6f,440f,392f};
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,b=t%.5f;
                float kick=Mathf.Sin(2*Mathf.PI*(50*b+8*(1-Mathf.Exp(-30*b))))*Mathf.Exp(-16*b);
                float bass=Mathf.Sin(t*2*Mathf.PI*(t%2<1?110:82.4f))*.18f;
                float hat=((i*7919)%1000/500f-1f)*Mathf.Exp(-60*((t+.25f)%.5f))*.12f;                 // geri vuruşta hi-hat
                float note=arp[Mathf.FloorToInt(t/.125f)%arp.Length],step=t%.125f; float lead=(Mathf.Repeat(t*note,1f)*2-1)*Mathf.Exp(-12*step)*.05f; // kısa testere dişi arpej
                samples[i]=(kick*.55f+bass+hat+lead)*.35f;
            }
            clip.SetData(samples,0); music.clip=clip; music.loop=true; music.spatialBlend=1; music.minDistance=3; music.maxDistance=24; music.Play();
        }

        void CollectTiles()
        {
            var list=new List<Renderer>(); foreach(var r in game.world.GetComponentsInChildren<Renderer>())if(r.name=="NOIR dans pisti")list.Add(r);
            tiles=list.ToArray(); tileMats=new Material[tiles.Length]; tileCell=new Vector2Int[tiles.Length];
            for(int i=0;i<tiles.Length;i++)
            {
                var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.EnableKeyword("_EMISSION"); m.SetColor("_BaseColor",new Color(.05f,.04f,.08f)); m.SetFloat("_Smoothness",.85f);
                tiles[i].sharedMaterial=m; tileMats[i]=m; var p=tiles[i].transform.position; tileCell[i]=new Vector2Int(Mathf.RoundToInt((p.x-11)/1.1f),Mathf.RoundToInt((-28-p.z)/1.1f));
            }
        }

        void Update()
        {
            if(!game||!music)return;
            bool active=!game.Paused; music.volume=game.expansion.Data.music*.65f;
            if(active&&!music.isPlaying)music.UnPause(); else if(!active&&music.isPlaying)music.Pause();
            if(!active)return;
            float t=Time.time,mt=music.time,pulse=Mathf.Exp(-(mt%.5f)/.5f*6f); int beat=Mathf.FloorToInt(mt/.5f);
            for(int i=0;i<tileMats.Length;i++)
            {
                var cell=tileCell[i]; int shift=Mathf.FloorToInt(t*.4f);
                var c=Color.Lerp(Neon[(cell.x+cell.y+shift)%Neon.Length],Neon[(cell.x*2+cell.y+1+shift)%Neon.Length],.5f+.5f*Mathf.Sin(t*.8f+i*.7f));
                bool lit=((cell.x+cell.y+beat)&1)==0; float level=lit?.7f+2.6f*pulse:.1f;
                tileMats[i].SetColor("_EmissionColor",c*level); tileMats[i].SetColor("_BaseColor",Color.Lerp(new Color(.04f,.035f,.06f),c*.22f,lit?.5f:0f));
            }
            if(ball)ball.Rotate(0,38*Time.deltaTime,0);
            if(sweep)sweep.Rotate(0,-31*Time.deltaTime,0);
            for(int i=0;i<eq.Count;i++){ var s=eq[i].localScale; s.y=.12f+(.9f+.8f*pulse)*Mathf.Abs(Mathf.Sin(t*2.6f+i*.9f))*Mathf.Abs(Mathf.Sin(t*1.3f+i*.37f)); eq[i].localScale=s; var p=eq[i].position; p.y=1.5f+s.y/2f; eq[i].position=p; }
        }
    }
}
