using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    // 6 gölge türünün 5'i (6.: Bekleyen = CafeParanormal.SpawnShadow). Her tür bir kural taşır; ışık dili ve güvenli alan burada.
    // Oturan: boş masaya oturur, gözünü ayırınca yaklaşır · Sayan: sayar, 19'da kes · Yankı: yürüyüşünü taklit eder · Eksik: bir sakinin yüzsüz hâli · Nöbetçi: ışığın altında durmaz
    public sealed class CafeGhosts : MonoBehaviour
    {
        public enum Kind { Oturan, Sayan, Yanki, Eksik, Nobetci }
        sealed class Ghost { public Kind kind; public GameObject go; public Animator anim; public float life,t,unseen,countT,wait; public int count; public CafeResidents.Resident who; public TextMesh text; public bool calm; public Vector3 patrolTo; }
        CafeGame game; Ghost ghost; int lastDay=-1;
        Vector3 machinePos=new Vector3(2.6f,0,4.6f); bool machineFound;
        float SafeRadius=>3.4f+(game!=null&&game.data!=null&&game.data.choice1==1?1.2f:0f);   // Nuri'ye güvendiysen makinenin etkisi genişler
        AudioSource drone; float silenceT,duckMul=1;
        static AudioClip MakeDrone(){ const int rate=22050; int n=rate*4; var d=new float[n]; for(int i=0;i<n;i++){ float t=i/(float)rate; d[i]=(Mathf.Sin(2*Mathf.PI*55f*t)+.8f*Mathf.Sin(2*Mathf.PI*57.5f*t)+.35f*Mathf.Sin(2*Mathf.PI*110.7f*t))*.22f*(.75f+.25f*Mathf.Sin(2*Mathf.PI*.5f*t)); } var c=AudioClip.Create("Gölge drone",n,1,rate,false); c.SetData(d,0); return c; }
        // oyuncu izi (Yankı için)
        readonly List<Vector4> trail=new List<Vector4>();
        // ışık dili
        readonly List<Light> lights=new List<Light>(); readonly List<Color> baseColor=new List<Color>(); float lightRefresh; float tint;
        // köpük yazısı
        TextMesh foam; float foamT;
        static readonly string[] FoamWords={"SAY","KALMA","ONA VER","ARDA","UYUMA","20"};

        public void Setup(CafeGame owner){ game=owner; drone=gameObject.AddComponent<AudioSource>(); drone.clip=MakeDrone(); drone.loop=true; drone.spatialBlend=0; drone.volume=0; drone.playOnAwake=false; }
        // Işık dili sese de yansır: gölge yaklaşırken müzik kısılır, alçak bir drone yükselir. Sayan 19'a gelince tam sessizlik.
        void TickAudio(float dt)
        {
            silenceT=Mathf.Max(0,silenceT-dt); float target=silenceT>0?0:Mathf.Lerp(1f,.4f,tint); duckMul=Mathf.MoveTowards(duckMul,target,dt*1.5f); game.musicMul=duckMul; game.ApplyAudio();
            if(drone){ float v=silenceT>0?0:tint*.2f; drone.volume=Mathf.MoveTowards(drone.volume,v,dt*.5f); if(drone.volume>.005f&&!drone.isPlaying)drone.Play(); else if(drone.volume<=.005f&&drone.isPlaying)drone.Stop(); }
        }
        public bool Active=>ghost!=null;
        public bool PlayerSafe=>Vector3.Distance(Flat(game.player.transform.position),machinePos)<SafeRadius;
        static Vector3 Flat(Vector3 v){ v.y=0; return v; }

        void FindMachine()
        {
            machineFound=true;
            foreach(var t in FindObjectsByType<CafeTarget>(FindObjectsInactive.Exclude)){ if(t.kind=="machine"){ machinePos=Flat(t.transform.position); return; } }
        }

        // ---------- üretim ----------
        public Color SampleLight=>lights.Count>0&&lights[0]?lights[0].color:Color.clear; public Color SampleBase=>baseColor.Count>0?baseColor[0]:Color.clear;
        public Vector3 MachinePos{ get{ if(!machineFound)FindMachine(); return machinePos; } } public float Tint=>tint; public void DebugSetCount(int n){ if(ghost!=null){ ghost.count=n; if(ghost.text)ghost.text.text=n.ToString(); } } public Vector3 GhostPos=>ghost!=null&&ghost.go?ghost.go.transform.position:Vector3.zero;
        public void ClearAll(){ if(ghost!=null)Vanish(false); }
        public string Describe()=>ghost==null?"yok":ghost.kind+" konum="+(ghost.go?ghost.go.transform.position.ToString("F1"):"?")+" sayaç="+ghost.count+" etiket="+Label();
        public bool Spawn(int level,Kind? force=null)
        {
            if(ghost!=null||!game||(!CafeStory.Active&&!force.HasValue))return false; if(!machineFound)FindMachine();
            var pool=new List<Kind>{Kind.Oturan,Kind.Sayan}; if(level>=2){pool.Add(Kind.Yanki);pool.Add(Kind.Eksik);} if(level>=3)pool.Add(Kind.Nobetci);
            var kind=force.HasValue?force.Value:pool[Random.Range(0,pool.Count)]; var g=new Ghost{kind=kind,life=60};
            Vector3 pos=Vector3.zero; string model="CafeMixamo/Olivia"; bool eyes=true;
            switch(kind)
            {
                case Kind.Oturan:
                {
                    int seat=PickSeat(); if(seat<0)return false; pos=game.world.Seat(seat); pos.y=0; g.life=95; g.t=seat; break;
                }
                case Kind.Sayan: pos=PickFarSpot(); g.count=12; g.life=70; break;
                case Kind.Yanki: { float px=game.player.transform.position.x; g.countT=px>=0?-1:1; g.life=45; pos=YankiPos(g,new Vector2(px,game.player.transform.position.z)); break; }   // ayna tarafı: oyuncunun öbür yanı
                case Kind.Eksik:
                {
                    g.who=PickMetResident(); if(g.who==null)return false; pos=new Vector3(-1.2f,0,3.2f); g.life=80; eyes=false; var m=CafeResidents.Mixamo(g.who); if(m)model=null; break;
                }
                default: pos=PickFarSpot(); g.life=60; g.patrolTo=PickFarSpot(); break;
            }
            string path=g.who!=null?"CafeMixamo/"+ModelOf(g.who):model; var prefab=path!=null?Resources.Load<GameObject>(path):null; if(!prefab)prefab=Resources.Load<GameObject>("CafeMixamo/Olivia"); if(!prefab)return false;
            g.go=new GameObject("Gölge · "+kind); g.go.transform.SetParent(game.world.transform,false); g.go.transform.position=pos;
            var toPlayer=Flat(game.player.transform.position)-pos; if(toPlayer.sqrMagnitude>.01f)g.go.transform.rotation=Quaternion.LookRotation(toPlayer);
            var avatar=Instantiate(prefab,g.go.transform); game.world.PrepareAvatar(avatar); g.go.transform.localScale=kind==Kind.Nobetci?new Vector3(.95f,1.28f,.95f):new Vector3(.9f,1.12f,.9f);
            var black=game.world.ColorMaterial(new Color(.008f,.008f,.012f));
            foreach(var r in avatar.GetComponentsInChildren<Renderer>()){ var m=new Material[r.sharedMaterials.Length]; for(int i=0;i<m.Length;i++)m[i]=black; r.sharedMaterials=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; }
            foreach(var c in avatar.GetComponentsInChildren<Collider>())Destroy(c);
            g.anim=avatar.GetComponentInChildren<Animator>(); if(g.anim){ g.anim.applyRootMotion=false; var ctrl=CafeResidents.MixamoController(); if(ctrl)g.anim.runtimeAnimatorController=ctrl; g.anim.speed=.6f; if(kind==Kind.Oturan)g.anim.SetBool("Sitting",true); if(eyes&&g.anim.isHuman)AddEyes(g.anim.GetBoneTransform(HumanBodyBones.Head),g.go.transform); }
            var hit=new GameObject("Gölge etkileşim alanı"); hit.layer=8; hit.transform.SetParent(g.go.transform,false); hit.transform.localPosition=Vector3.up*(kind==Kind.Oturan?.7f:1f);
            var col=hit.AddComponent<CapsuleCollider>(); col.isTrigger=true; col.height=2f; col.radius=.65f; hit.AddComponent<CafeTarget>().kind="cc_ghost";
            if(kind==Kind.Sayan){ var tg=new GameObject("Sayaç"); tg.transform.SetParent(g.go.transform,false); tg.transform.localPosition=Vector3.up*2.35f; g.text=tg.AddComponent<TextMesh>(); g.text.fontSize=64; g.text.characterSize=.04f; g.text.anchor=TextAnchor.MiddleCenter; g.text.color=new Color(.7f,.85f,1f); tg.AddComponent<CafeDepthLabel>().Setup(g.text,CafeTypography.Strong); }
            ghost=g; Foam(FoamWords[Random.Range(0,FoamWords.Length)]); game.Tell(Intro(kind,g),false); return true;
        }
        // Yankı: oyuncunun 1.3 sn önceki konumunun ayna görüntüsü (x → -x), ortada duruyorsan yan tarafa kaydırılır
        static Vector3 YankiPos(Ghost g,Vector2 old){ float x=-old.x+g.countT*3.2f; return new Vector3(Mathf.Clamp(x,-9.3f,9.3f),0,old.y); }
        static string ModelOf(CafeResidents.Resident r){ return CafeResidents.ModelName(r); }
        string Intro(Kind k,Ghost g)
        {
            switch(k){ case Kind.Oturan:return "Boş bir masada biri oturuyor…"; case Kind.Sayan:return "Birinin sayma sesi geliyor…"; case Kind.Yanki:return "Aynı yürüyüşle yürüyen biri var."; case Kind.Eksik:return "Yüzsüz biri bekliyor. "+g.who.ad+"'e benziyor."; default:return "Işığın altında durmayan biri dolaşıyor."; }
        }
        int PickSeat()
        {
            var free=new List<int>(); var p=Flat(game.player.transform.position);
            for(int i=0;i<game.data.tables*2;i++){ bool occupied=false; foreach(var c in game.customers)if(c&&c.seat==i)occupied=true; if(occupied)continue; var s=game.world.Seat(i); s.y=0; if((s-p).magnitude>=3f)free.Add(i); }
            return free.Count>0?free[Random.Range(0,free.Count)]:-1;
        }
        Vector3 PickFarSpot()
        {
            Vector3[] spots={new Vector3(-8.6f,0,-8f),new Vector3(8.7f,0,-8f),new Vector3(-8.6f,0,6.5f),new Vector3(8.2f,0,6f),new Vector3(0,0,-2f),new Vector3(-9f,0,-2f),new Vector3(9f,0,-3f)};
            var p=Flat(game.player.transform.position); var far=new List<Vector3>(); foreach(var s in spots)if((s-p).magnitude>6f&&(s-machinePos).magnitude>SafeRadius+1)far.Add(s);
            return far.Count>0?far[Random.Range(0,far.Count)]:spots[0];
        }
        CafeResidents.Resident PickMetResident()
        {
            var met=new List<CafeResidents.Resident>(); foreach(var r in CafeResidents.All)if(CafeResidents.State(game,r).met&&CafeResidents.Mixamo(r))met.Add(r); return met.Count>0?met[Random.Range(0,met.Count)]:null;
        }
        void AddEyes(Transform head,Transform root)
        {
            if(!head)return; var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",Color.black); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",new Color(.75f,.9f,1f)*5f);
            foreach(float side in new[]{-1f,1f})
            {
                var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name="Gölge gözü"; Destroy(eye.GetComponent<Collider>()); eye.GetComponent<Renderer>().sharedMaterial=m; eye.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                eye.transform.SetParent(head,false); eye.transform.position=head.position+root.forward*.1f+Vector3.up*.06f+root.right*side*.032f; float s=.02f/Mathf.Max(.001f,head.lossyScale.x); eye.transform.localScale=new Vector3(s,s*.7f,s);
            }
        }

        // ---------- etkileşim ----------
        public string Label()
        {
            if(ghost==null)return "";
            bool cup=game.cup!=null&&game.cup.lidded;
            switch(ghost.kind)
            {
                case Kind.Oturan: return cup?"Boş masadaki gölgeye fincan ikram et [E]":"Boş masada biri oturuyor · kapaklı bir fincan lazım";
                case Kind.Sayan: return ghost.count>=19?"19! Saymayı kes [E]":"Sayıyor: "+ghost.count+" · 19'a gelince kes";
                case Kind.Yanki: return "Yankı · yaklaş ve dur [E]";
                case Kind.Eksik: return "Yüzsüz · "+ghost.who.ad+"'in siparişi: "+ghost.who.siparis+(cup?" [E]":"");
                default: return "Nöbetçi · ışığa yaklaş";
            }
        }
        public void Use()
        {
            if(ghost==null)return; bool cup=game.cup!=null&&game.cup.lidded;
            switch(ghost.kind)
            {
                case Kind.Oturan: if(!cup){game.Tell("Önce kapaklı bir fincan hazırla.");return;} Consume(); Calm("Gölge fincanı içti ve huzur buldu. +20 coin",20); break;
                case Kind.Sayan: if(ghost.count>=19&&ghost.count<20)Calm("Saymayı 19'da kestin. Ses sustu. +15 coin",15); else game.Tell(ghost.count<19?"Henüz erken, saymaya devam ediyor.":"Çok geç…"); break;
                case Kind.Yanki: Calm("Yankı durdu ve soldu.",0); break;
                case Kind.Eksik:
                    if(!cup){game.Tell("Siparişini hazırla: "+ghost.who.siparis);return;}
                    bool match=game.cup.productName==ghost.who.siparis||(game.cup.recipe>=0&&game.cup.recipe<Economy.Recipes.Length&&Economy.Recipes[game.cup.recipe].name==ghost.who.siparis);
                    if(!match){game.Tell("Başını iki yana salladı. Bu onun siparişi değil.");return;}
                    Consume(); CafeResidents.State(game,ghost.who).bond++; Calm(ghost.who.ad+"'in hatırası huzur buldu. +30 coin",30); break;
                default: game.Tell("Işığın yakınındaysan sana yaklaşamaz."); break;
            }
        }
        void Consume(){ game.cup=null; game.UpdateHand(); }
        void Calm(string message,int reward)
        {
            if(reward>0){ game.data.money+=reward; game.data.earned+=reward; }
            game.Tell(message,true); Foam(ghost.kind==Kind.Eksik?"TEŞEKKÜR":"HUZUR"); Vanish(true);
        }
        void Vanish(bool calm){ if(ghost!=null&&ghost.go)Destroy(ghost.go); ghost=null; if(!calm&&game.GetComponent<CafeParanormal>()){} }

        // ---------- köpük yazısı ----------
        public void Foam(string message)
        {
            if(!foam)
            {
                var go=new GameObject("Köpük yazısı"); go.transform.SetParent(game.world.transform,false); foam=go.AddComponent<TextMesh>(); foam.fontSize=72; foam.characterSize=.03f; foam.anchor=TextAnchor.MiddleCenter; foam.alignment=TextAlignment.Center; foam.color=new Color(.95f,.92f,.85f,0f); go.AddComponent<CafeDepthLabel>().Setup(foam,CafeTypography.Strong);
            }
            foam.text=message; foam.transform.position=CafeWorld.PickupPoint+new Vector3(0,1.32f,0); foamT=6f;
        }
        void TickFoam(float dt)
        {
            if(!foam)return; if(foamT>0){ foamT-=dt; var cam=game.player.view.transform; foam.transform.rotation=Quaternion.LookRotation(foam.transform.position-cam.position); float a=Mathf.Clamp01(Mathf.Min(foamT,6f-foamT)*1.5f)*.85f; foam.color=new Color(.95f,.92f,.85f,a); } else foam.color=new Color(.95f,.92f,.85f,0f);
        }

        // ---------- ışık dili: sıcak sarı = güvenli, soğuk mavi = yakın ----------
        void RefreshLights()
        {
            lights.Clear(); baseColor.Clear();
            foreach(var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if(!l||l.type==LightType.Directional||l.intensity<.1f)continue; var p=l.transform.position;
                bool cafe=p.y<3.6f&&Mathf.Abs(p.x)<10.5f&&p.z>-11.5f&&p.z<9.5f; if(cafe){ lights.Add(l); baseColor.Add(l.color); }
            }
        }
        void TickLightLanguage(float dt)
        {
            float target=0; var par=game.GetComponent<CafeParanormal>(); var sh=par?par.ShadowTransform:null; Transform near=ghost!=null&&ghost.go?ghost.go.transform:sh;
            if(sh&&near&&Vector3.Distance(game.player.transform.position,sh.position)<Vector3.Distance(game.player.transform.position,near.position))near=sh;
            if(near){ float d=Vector3.Distance(Flat(game.player.transform.position),Flat(near.position)); target=Mathf.Clamp01((13f-d)/9f); if(PlayerSafe)target*=.35f; }
            tint=Mathf.MoveTowards(tint,target,dt*.6f);
            lightRefresh-=dt; if((tint>.01f||target>0)&&lightRefresh<=0){ lightRefresh=6f; RefreshLights(); }
            if(lights.Count==0)return;
            var cold=new Color(.55f,.72f,1f);
            for(int i=0;i<lights.Count;i++){ if(!lights[i])continue; lights[i].color=Color.Lerp(baseColor[i],cold,tint*.85f); }
            if(tint<=.005f&&target<=0){ for(int i=0;i<lights.Count;i++)if(lights[i])lights[i].color=baseColor[i]; lights.Clear(); baseColor.Clear(); }
        }

        // ---------- davranışlar ----------
        void Update()
        {
            if(!game||game.data==null||game.player==null)return;
            if(lastDay!=game.data.day){ lastDay=game.data.day; if(ghost!=null)Vanish(false); }
            if(game.Paused||CafeStory.Busy){ return; }
            float dt=Time.deltaTime; var pp=game.player.transform.position; trail.Add(new Vector4(pp.x,pp.z,Time.time,0)); while(trail.Count>0&&Time.time-trail[0].z>3f)trail.RemoveAt(0);
            TickFoam(dt); TickLightLanguage(dt); TickAudio(dt);
            if(ghost==null)return; var g=ghost; if(!g.go){ghost=null;return;}
            g.life-=dt; g.t+=dt; if(g.life<=0){ Vanish(false); return; }
            var cam=game.player.view.transform; var to=g.go.transform.position+Vector3.up*1.2f-cam.position; float angle=Vector3.Angle(cam.forward,to),dist=to.magnitude; bool seen=angle<45f&&dist<26f;
            var flat=Flat(game.player.transform.position)-Flat(g.go.transform.position);
            switch(g.kind)
            {
                case Kind.Oturan:
                    g.unseen=seen?0:g.unseen+dt;
                    if(g.unseen>4f){ g.unseen=0; int next=PickSeatNear(); if(next>=0){ var s=game.world.Seat(next); s.y=0; g.go.transform.position=s; g.t=next; } }
                    break;
                case Kind.Sayan:
                    if(!PlayerSafe){ g.countT+=dt; if(g.countT>=(game.data.choice1==2?2.7f:2.3f)){ g.countT=0; g.count++; if(g.count==19)silenceT=3.2f; var par=game.GetComponent<CafeParanormal>(); if(par)par.Whisper(g.go.transform.position); } }
                    if(g.text){ var cp=game.player.view.transform; g.text.transform.rotation=Quaternion.LookRotation(g.text.transform.position-cp.position); g.text.text=PlayerSafe?"…":g.count.ToString(); }
                    if(flat.sqrMagnitude>.01f)g.go.transform.rotation=Quaternion.Slerp(g.go.transform.rotation,Quaternion.LookRotation(-flat),dt*2f);
                    if(g.count>=20){ var p=game.GetComponent<CafeParanormal>(); if(p){p.Flicker(1.2f);p.Dim(3f);} game.data.reputation=Mathf.Max(0,game.data.reputation-3); game.Tell("Yirmiye geldi… kapı zili çaldı. Kimse yok. (İtibar -3)",true); Foam("20"); Vanish(false); }
                    break;
                case Kind.Yanki:
                {
                    Vector4 old=trail.Count>0?trail[0]:new Vector4(pp.x,pp.z,0,0); foreach(var t in trail)if(Time.time-t.z<=1.3f){old=t;break;}
                    var target=YankiPos(g,new Vector2(old.x,old.y)); var delta=target-g.go.transform.position; float step=Mathf.Min(delta.magnitude,3.2f*dt); if(step>.001f)g.go.transform.position+=delta.normalized*step;
                    bool moving=delta.magnitude>.15f; if(moving)g.go.transform.rotation=Quaternion.Slerp(g.go.transform.rotation,Quaternion.LookRotation(delta.normalized),dt*8f);
                    if(g.anim)g.anim.SetBool("Moving",moving);
                    if(dist<1.7f){ game.Tell("Yankı sana dokunacaktı… dağıldı."); Vanish(false); }
                    break;
                }
                case Kind.Eksik:
                    if(flat.sqrMagnitude>.01f)g.go.transform.rotation=Quaternion.Slerp(g.go.transform.rotation,Quaternion.LookRotation(flat),dt*1.5f);
                    break;
                default:
                {
                    bool safe=PlayerSafe; var target=safe?g.patrolTo:Flat(game.player.transform.position); var delta=target-g.go.transform.position; delta.y=0;
                    if(!safe&&delta.magnitude>12f)target=g.patrolTo;
                    if(safe&&delta.magnitude<1f)g.patrolTo=PickFarSpot();
                    float speed=safe?1.4f:(dist<7?1.9f:1.1f); if(delta.magnitude>.05f){ g.go.transform.position+=delta.normalized*Mathf.Min(delta.magnitude,speed*dt); g.go.transform.rotation=Quaternion.Slerp(g.go.transform.rotation,Quaternion.LookRotation(delta.normalized),dt*5f); }
                    if(g.anim)g.anim.SetBool("Moving",true);
                    if(!safe&&dist<1.5f){ var par=game.GetComponent<CafeParanormal>(); if(par)par.Scare(); int loss=Mathf.Min(game.data.money,15); game.data.money-=loss; game.Tell("Nöbetçi seni yakaladı. Işıktan uzaklaşma! (-"+loss+" coin)",true); Vanish(false); }
                    break;
                }
            }
        }
        int PickSeatNear()
        {
            var p=Flat(game.player.transform.position); int best=-1; float bd=999; int current=(int)ghost.t;
            for(int i=0;i<game.data.tables*2;i++){ if(i==current)continue; bool occupied=false; foreach(var c in game.customers)if(c&&c.seat==i)occupied=true; if(occupied)continue; var s=game.world.Seat(i); s.y=0; float d=(s-p).magnitude; if(d<2.2f)continue; if(d<bd){bd=d;best=i;} }
            return best;
        }
        void LateUpdate()
        {
            if(ghost==null||ghost.kind!=Kind.Oturan||ghost.anim==null||!ghost.anim.isHuman)return;
            if(!ghost.anim.GetCurrentAnimatorStateInfo(0).IsName("Sitting"))return;   // oturuş animasyonu başlamadan kalça düzeltmesi yapma (zemine gömülür)
            var hips=ghost.anim.GetBoneTransform(HumanBodyBones.Hips); if(!hips)return; var gp=ghost.go.transform.position; float off=hips.position.y-gp.y; gp.y=Mathf.Clamp(.59f-off,-.2f,.1f); ghost.go.transform.position=gp;
        }
    }
}
