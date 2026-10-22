using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CafeLife
{
    // Geceleri kafede ve üst kat evde paranormal olaylar. Hikâyeye bağlı: gün arttıkça sıklaşır ("20. fincanı içenlerin gölgeleri").
    // Katmanlar: sessiz huzursuzluk (titreme, ayak sesi, kapı vurması, fısıltı, eşya kıpırtısı) → gölge figürü → nadir jumpscare.
    // Seviye: gün 1 tek ipucu · gün 2 huzursuzluk + gölge · gün 3 TV ve nadir jumpscare · gün 4+ daha sık.
    public sealed class CafeParanormal:MonoBehaviour
    {
        public static bool JumpscaresEnabled=true;   // kapatılırsa yalnızca huzursuzluk ve gölgeler kalır
        public CafeGame game;
        float nextEvent=20,tension,lastBig=-999; bool hinted;
        // ışık titremesi / kısılması
        readonly List<Light> lights=new List<Light>(); readonly List<float> baseIntensity=new List<float>(); float flickerTime,flickerStep,dimTime,dimTotal;
        // gölge
        GameObject shadow; float shadowLook,shadowLife,shadowY,scareT,behindT; bool behind; string lastInfo="";
        public string Info()=>shadow?lastInfo:"gölge yok";
        public Transform ShadowTransform=>shadow?shadow.transform:null;
        // eşya kıpırtısı
        readonly Dictionary<Transform,Pose> chairHome=new Dictionary<Transform,Pose>();
        // TV
        GameObject tvQuad,tvEyes; TextMesh tvText; Texture2D tvStatic; float tvTime,tvStep; Color32[] tvPixels; bool tvScare,tvScared;
        // ses ve ekran efektleri
        AudioSource sfx,heart; AudioClip whisper,creak,thump,staticBurst,stinger,step,knock,breath,beat; Image flash; float flashA,kickT,kickDur=.3f;

        static readonly string[] TvMessages={"20. FİNCANI\nKİM İÇTİ?","UYUMA\nSABAH OLMAYACAK","SANA BİR FİNCAN\nKALDI","ARKANA\nBAKMA","KAHVE\nBİTTİ","HASAN DEDE\nSELAM SÖYLÜYOR"};
        static readonly Vector3 TvScreen=new Vector3(-.3f,CafeHome.Fy+.82f,-.575f);

        int Level=>Mathf.Clamp(game.data.day-1,0,3);
        bool Upstairs=>game.player.transform.position.y>3.4f;
        bool InBuilding{ get{ var p=game.player.transform.position; return Upstairs||(Mathf.Abs(p.x)<10&&p.z>-11&&p.z<9); } }
        float FloorY=>Upstairs?CafeHome.Fy:0;

        void Start()
        {
            sfx=gameObject.AddComponent<AudioSource>(); sfx.playOnAwake=false; sfx.spatialBlend=0;
            heart=gameObject.AddComponent<AudioSource>(); heart.playOnAwake=false; heart.loop=true; heart.spatialBlend=0; heart.volume=0;
            whisper=Whisper(); creak=Creak(); thump=Thump(); staticBurst=StaticBurst(); stinger=Stinger(); step=Step(); knock=Knock(); breath=Breath(); beat=Beat(); heart.clip=beat;
            var go=new GameObject("Paranormal ekran efekti",typeof(RectTransform),typeof(Canvas)); go.transform.SetParent(transform,false); go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; go.GetComponent<Canvas>().sortingOrder=95;
            var img=new GameObject("Flaş",typeof(RectTransform),typeof(Image)); img.transform.SetParent(go.transform,false); var rt=img.GetComponent<RectTransform>(); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
            flash=img.GetComponent<Image>(); flash.raycastTarget=false; flash.color=new Color(1f,.97f,.95f,0f);
        }

        // ---------- ses (hepsi kodla üretilir) ----------
        static AudioClip Make(string name,float seconds,System.Func<float,float,float> sample)
        {
            const int rate=22050; int n=(int)(rate*seconds); var d=new float[n]; for(int i=0;i<n;i++)d[i]=sample(i/(float)rate,i/(float)n);
            var clip=AudioClip.Create(name,n,1,rate,false); clip.SetData(d,0); return clip;
        }
        static AudioClip Whisper(){ float lp=0,lp2=0; return Make("Fısıltı",2.4f,(t,p)=>{ float n=Random.value*2-1; lp+=(n-lp)*.12f; lp2+=(lp-lp2)*.25f; float env=Mathf.Sin(Mathf.PI*p); env*=env; return lp2*3.2f*env*(.5f+.5f*Mathf.Sin(t*17f))*.5f; }); }
        static AudioClip Creak(){ return Make("Gıcırtı",.8f,(t,p)=>{ float f=70+30*Mathf.Sin(t*9f)+40*(1-p); float saw=Mathf.Repeat(t*f,1f)*2-1; return saw*Mathf.Sin(Mathf.PI*p)*.22f+(Random.value*2-1)*.03f; }); }
        static AudioClip Thump(){ return Make("Güm",.3f,(t,p)=>Mathf.Sin(2*Mathf.PI*(55-30*p)*t)*Mathf.Exp(-t*14f)*.8f); }
        static AudioClip StaticBurst(){ return Make("Cızırtı",.6f,(t,p)=>(Random.value*2-1)*.25f*(1-p)); }
        static AudioClip Step(){ float lp=0; return Make("Ayak sesi",.16f,(t,p)=>{ float n=Random.value*2-1; lp+=(n-lp)*.15f; return (lp*1.6f*Mathf.Exp(-t*40f)+Mathf.Sin(2*Mathf.PI*70*t)*Mathf.Exp(-t*28f))*.8f; }); }
        static AudioClip Knock(){ return Make("Vurma",.14f,(t,p)=>Mathf.Sin(2*Mathf.PI*170*t)*Mathf.Exp(-t*38f)*.9f+(Random.value*2-1)*Mathf.Exp(-t*90f)*.35f); }
        static AudioClip Breath(){ float lp=0; return Make("Nefes",1.1f,(t,p)=>{ float n=Random.value*2-1; lp+=(n-lp)*.2f; float env=Mathf.Sin(Mathf.PI*p); return lp*env*env*1.4f*.45f; }); }
        static AudioClip Beat(){ return Make("Kalp atışı",.95f,(t,p)=>(Mathf.Sin(2*Mathf.PI*52*t)*Mathf.Exp(-t*22f)+.7f*Mathf.Sin(2*Mathf.PI*48*Mathf.Max(0,t-.26f))*Mathf.Exp(-Mathf.Max(0,t-.26f)*22f)*(t>.26f?1:0))*.9f); }
        // Keskin korku vuruşu: sarsılan testere dişleri, gürültü patlaması ve alçak gümleme (tek seferlik, hızlı sönümlü)
        static AudioClip Stinger()
        {
            return Make("Korku vuruşu",1.2f,(t,p)=>{
                float env=Mathf.Exp(-t*3.2f)*(1-Mathf.Exp(-t*120f)),f=900-420*p,s=0;
                for(int k=0;k<4;k++){ float ff=f*(1+k*.0173f*(k%2==0?1:-1)); s+=Mathf.Repeat(t*ff,1f)*2-1; }
                float noise=(Random.value*2-1)*Mathf.Exp(-t*9f),boom=Mathf.Sin(2*Mathf.PI*48*t)*Mathf.Exp(-t*7f);
                return Mathf.Clamp((s*.12f+noise*.5f+boom*.8f)*env*1.3f,-.9f,.9f);
            });
        }
        void Play(AudioClip c,float volume,Vector3? at=null){ if(!sfx||!c)return; if(at.HasValue)AudioSource.PlayClipAtPoint(c,at.Value,volume); else sfx.PlayOneShot(c,volume); }

        // ---------- zamanlayıcı ----------
        void Update()
        {
            if(!game||game.data==null)return;
            flashA=Mathf.MoveTowards(flashA,0,Time.deltaTime*3.2f); if(flash)flash.color=new Color(1f,.97f,.95f,flashA);
            if(!CafeStory.Active||game.Paused||CafeStory.Busy){ if(heart)heart.volume=0; return; }
            float dt=Time.deltaTime; TickFlicker(dt); TickShadow(dt); TickTv(dt); TickHeart();
            nextEvent-=dt; if(nextEvent>0)return;
            if(!InBuilding){ nextEvent=8; return; }
            bool up=Upstairs; bool night=game.data.dayTime>=170f||(up&&game.data.dayTime>=120f);
            if(!night){ nextEvent=8; return; }
            int level=Level;
            if(level==0){ if(!hinted&&!up){ hinted=true; Flicker(1.4f); } nextEvent=30; return; }   // ilk gün: yalnızca bir ipucu
            nextEvent=Random.Range(20f,42f)/(1f+level*.4f)*(game.data.choice3==1?1.3f:game.data.choice3==2?.75f:1f);   // Nermin'e cevap verdiysen olaylar seyrek, odayı kilitlediysen sık tension+=.22f+.07f*level;
            // Büyük korku: seviye 2+ ve gerginlik dolunca, uzun bekleme süresiyle
            if(JumpscaresEnabled&&level>=2&&tension>=1f&&Time.time-lastBig>150f&&!shadow&&tvTime<=0){ tension=0; lastBig=Time.time; if(up&&Random.value<.5f)TvOn(null,true); else BehindScare(); return; }
            var pool=new List<int>{0,1,5};
            if(level>=1){ pool.Add(2); pool.Add(6); }
            if(!shadow&&level>=1){ pool.Add(3); pool.Add(3); }
            if(!up||level>=2)pool.Add(4);
            if(up&&level>=2&&tvTime<=0)pool.Add(7);
            if(level>=2)pool.Add(8);
            if(level>=1&&!shadow&&!up&&game.ghosts!=null&&!game.ghosts.Active){ pool.Add(9); if(level>=2)pool.Add(9); }
            switch(pool[Random.Range(0,pool.Count)])
            {
                case 0:Flicker(Random.Range(.8f,1.8f));break; case 1:Dim(Random.Range(3f,5f));break; case 2:StartCoroutine(StepsRoutine());break;
                case 3:SpawnShadow(PickShadowSpot());break; case 4:MoveChair();break; case 5:Play(whisper,.14f,game.player.transform.position+Random.onUnitSphere*2f);break;
                case 6:StartCoroutine(KnockRoutine());break; case 7:TvOn(null,false);break; case 8:Play(breath,.22f,game.player.transform.position-FlatForward()*.7f+Vector3.up*1.5f);break;
                case 9:game.ghosts.Spawn(level);break;
            }
        }
        Vector3 FlatForward(){ var f=game.player.view.transform.forward; f.y=0; return f.sqrMagnitude<.01f?Vector3.forward:f.normalized; }

        // ---------- ışıklar ----------
        void CollectLights()
        {
            lights.Clear(); baseIntensity.Clear();
            foreach(var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if(!l||l.type==LightType.Directional||l.intensity<.1f)continue; var p=l.transform.position;
                bool cafe=p.y<3.6f&&Mathf.Abs(p.x)<10.5f&&p.z>-11.5f&&p.z<9.5f, home=p.y>3.6f&&p.y<7.2f&&p.x>-10.5f&&p.x<2f&&p.z>-1.5f&&p.z<9.5f;
                if(cafe||home){ lights.Add(l); baseIntensity.Add(l.intensity); }
            }
        }
        public void Scare(){ Kick(.3f); flashA=.7f; Play(stinger,.55f); Flicker(.4f); }
        public void Whisper(Vector3 at){ Play(whisper,.16f,at); }
        public void Flicker(float duration){ if(flickerTime<=0&&dimTime<=0)CollectLights(); flickerTime=Mathf.Max(flickerTime,duration); Play(thump,.35f); }
        public void Dim(float duration){ if(flickerTime<=0&&dimTime<=0)CollectLights(); dimTime=dimTotal=duration; }
        void TickFlicker(float dt)
        {
            if(flickerTime>0)
            {
                flickerTime-=dt; flickerStep-=dt;
                if(flickerStep<=0){ flickerStep=Random.Range(.03f,.12f); for(int i=0;i<lights.Count;i++)if(lights[i])lights[i].intensity=baseIntensity[i]*(Random.value<.45f?Random.Range(0f,.08f):1f); }
                if(flickerTime<=0)RestoreLights();
            }
            else if(dimTime>0)
            {   // yavaşça kararır, sonra geri gelir
                dimTime-=dt; float k=Mathf.Sin(Mathf.PI*Mathf.Clamp01(1-dimTime/dimTotal)); for(int i=0;i<lights.Count;i++)if(lights[i])lights[i].intensity=baseIntensity[i]*Mathf.Lerp(1f,.12f,k);
                if(dimTime<=0)RestoreLights();
            }
        }
        void RestoreLights(){ for(int i=0;i<lights.Count;i++)if(lights[i])lights[i].intensity=baseIntensity[i]; }

        // ---------- sessiz huzursuzluk ----------
        IEnumerator StepsRoutine()
        {
            var p=game.player.transform.position; var back=-FlatForward(); float dist=5f;
            for(int i=0;i<6;i++){ if(!InBuilding)yield break; Play(step,Mathf.Lerp(.22f,.5f,i/5f),game.player.transform.position+back*dist+Vector3.up*.05f); dist-=.7f; yield return new WaitForSeconds(Random.Range(.5f,.65f)); }
        }
        IEnumerator KnockRoutine()
        {
            var dir=Quaternion.Euler(0,Random.Range(60f,300f),0)*FlatForward(); var at=game.player.transform.position+dir*3f+Vector3.up*1.3f;
            for(int i=0;i<3;i++){ Play(knock,.7f,at); yield return new WaitForSeconds(.4f); }
        }

        // ---------- gölge figürü ----------
        Vector3 PickShadowSpot()
        {
            var p=game.player.transform.position; var cam=game.player.view.transform; bool up=Upstairs; float y=FloorY;
            Vector3[] spots=up?new[]{new Vector3(-6.5f,y,.4f),new Vector3(-6f,y,7.6f),new Vector3(-1.5f,y,7.3f),new Vector3(-2.6f,y,4f),new Vector3(-1f,y,1f)}
                              :new[]{new Vector3(-8.6f,y,-8f),new Vector3(8.7f,y,-8f),new Vector3(-8.6f,y,6.5f),new Vector3(8.2f,y,6f),new Vector3(0,y,-2f),new Vector3(-9f,y,-2f),new Vector3(9f,y,-3f)};
            var good=new List<Vector3>(); foreach(var s in spots){ var d=s-p; d.y=0; float dist=d.magnitude; if(dist<5f||dist>14f)continue; float ang=Vector3.Angle(new Vector3(cam.forward.x,0,cam.forward.z),d); if(ang>30f&&ang<120f)good.Add(s); }
            if(good.Count==0)foreach(var s in spots)if((s-p).magnitude>5f)good.Add(s);
            return good.Count>0?good[Random.Range(0,good.Count)]:spots[0];
        }

        public void SpawnShadow(Vector3 pos,bool scary=false)
        {
            if(shadow)Destroy(shadow); var variants=game.customerPrefabs; if(variants==null||variants.Length==0)return;
            var goneRes=scary?null:CafeResidents.GoneToday(game); var goneModel=goneRes!=null?CafeResidents.Mixamo(goneRes):null;   // 20. fincanı içen sakin: gölge onun siluetinde
            var mix=goneModel?goneModel:Resources.Load<GameObject>(scary?"CafeMixamo/Skeleton":"CafeMixamo/Olivia"); var mixCtrl=mix?CafeResidents.MixamoController():null;
            shadow=new GameObject("Gölge"); shadow.transform.SetParent(game.world.transform,false); shadow.transform.position=pos; shadowY=pos.y;
            var toPlayer=game.player.transform.position-pos; toPlayer.y=0; if(toPlayer.sqrMagnitude>.01f)shadow.transform.rotation=Quaternion.LookRotation(toPlayer);
            var avatar=Instantiate(mix?mix:variants[0],shadow.transform); game.world.PrepareAvatar(avatar);
            shadow.transform.localScale=new Vector3(.88f,1.2f,.88f);       // uzamış, ürkütücü orantı
            var black=game.world.ColorMaterial(new Color(.008f,.008f,.012f));
            foreach(var r in avatar.GetComponentsInChildren<Renderer>()){ if(scary&&mix){ r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; continue; } var m=new Material[r.sharedMaterials.Length]; for(int i=0;i<m.Length;i++)m[i]=black; r.sharedMaterials=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; }
            foreach(Transform t in avatar.GetComponentsInChildren<Transform>(true)){ string n=t.name.ToLowerInvariant(); if(n.Contains("baseball")||n.Contains("knife")||n.Contains("handgun")||n.Contains("weapon")||n.Contains("sword")||n.Contains("axe"))t.gameObject.SetActive(false); }
            foreach(var c in avatar.GetComponentsInChildren<Collider>())Destroy(c);
            var anim=avatar.GetComponentInChildren<Animator>();
            if(anim){ anim.applyRootMotion=false; if(mixCtrl)anim.runtimeAnimatorController=mixCtrl; else if(game.customerAnimator)anim.runtimeAnimatorController=game.customerAnimator; anim.SetBool("Moving",false); anim.speed=.45f; if(anim.isHuman&&!scary)AddEyes(anim.GetBoneTransform(HumanBodyBones.Head),mix!=null); }
            shadowLook=0; shadowLife=42f; scareT=0; Play(whisper,.12f,pos);
        }
        // Parlayan iki göz: kafa kemiğine bağlı, yüzün önünde
        void AddEyes(Transform head,bool realistic=false)
        {
            if(!head)return; float fwd=realistic?.1f:.25f,side0=realistic?.032f:.075f,size=realistic?.022f:.05f; var root=shadow.transform; var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",Color.black); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",new Color(.9f,.95f,1f)*6f);
            foreach(float side in new[]{-1f,1f})
            {
                var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name="Gölge gözü"; Destroy(eye.GetComponent<Collider>()); eye.GetComponent<Renderer>().sharedMaterial=m; eye.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var world=head.position+root.forward*fwd+Vector3.up*(realistic?.06f:.03f)+root.right*side*side0; eye.transform.SetParent(head,false); eye.transform.position=world;   // kartun kafası büyük: göz yüzün dışında durmalı
                float s=size/Mathf.Max(.001f,head.lossyScale.x); eye.transform.localScale=new Vector3(s,s*.65f,s);
            }
        }
        void TickShadow(float dt)
        {
            if(!shadow)return; var cam=game.player.view.transform; var chest=shadow.transform.position+Vector3.up*1.3f; var to=chest-cam.position; float dist=to.magnitude;
            float angle=Vector3.Angle(cam.forward,to); bool blocked=Physics.Linecast(cam.position,chest,out var hit,~0,QueryTriggerInteraction.Ignore);
            bool seen=angle<24f&&dist<24f&&!blocked; lastInfo="angle="+angle.ToString("F1")+" dist="+dist.ToString("F1")+" blocked="+(blocked?hit.collider.name:"-")+" look="+shadowLook.ToString("F2");
            var flat=game.player.transform.position-shadow.transform.position; flat.y=0;
            if(flat.sqrMagnitude>.01f)shadow.transform.rotation=Quaternion.Slerp(shadow.transform.rotation,Quaternion.LookRotation(flat),dt*3f);
            if(scareT>0){ scareT-=dt; var front=cam.position+FlatForward()*1.15f; front.y=shadowY; shadow.transform.position=front; if(scareT<=0)Vanish(false,true); return; }   // jumpscare: tam önünde
            if(behind)
            {
                behindT+=dt; if(dist>1.5f){ var q=shadow.transform.position+flat.normalized*.17f*dt; q.y=shadowY; shadow.transform.position=q; }   // sırtına doğru sinsice yaklaşır
                if(angle<55f&&!blocked)StartJumpscare(); else if(behindT>18f)Vanish(false,true);
                return;
            }
            shadowLook=seen?shadowLook+dt:Mathf.Max(0,shadowLook-dt*.5f); shadowLife-=dt;
            if(!seen&&flat.magnitude>3.6f){ var p=shadow.transform.position+flat.normalized*.3f*dt; p.y=shadowY; shadow.transform.position=p; }   // göz kırpmadığında yavaşça yaklaşır
            if(shadowLook>=1.1f)Vanish(true,false); else if(shadowLife<=0)Vanish(false,false); else if(flat.magnitude<2.6f){ Play(thump,.6f); Vanish(false,false); }
        }
        void TickHeart()
        {
            if(!heart)return; float target=0;
            if(shadow){ float d=(shadow.transform.position-game.player.transform.position).magnitude; target=Mathf.Clamp01((11f-d)/9f)*(behind?.6f:.4f); }
            heart.volume=Mathf.MoveTowards(heart.volume,target,Time.deltaTime*.5f);
            if(heart.volume>.01f&&!heart.isPlaying)heart.Play(); else if(heart.volume<=.01f&&heart.isPlaying)heart.Stop();
        }
        void Vanish(bool looked,bool silent)
        {
            if(shadow)Destroy(shadow); shadow=null; behind=false; scareT=0;
            if(!silent){ Play(whisper,.2f); Flicker(.5f); }
            if(looked&&!game.data.shadowSeen&&game.story)
            { game.data.shadowSeen=true; game.Save(); game.story.Remark("Arda","…Orada biri vardı. Haberdeki 20. fincan lafı geldi aklıma. Hasan Dede bunu mu kastetmişti?"); }
        }

        // ---------- jumpscare ----------
        // Arkandan: gölge sırtında belirir ve sinsice yaklaşır; döndüğünde tam karşında çıkar
        public bool BehindScare()
        {
            var p=game.player.transform.position; var back=-FlatForward(); float dist=2.7f; Vector3 pos=Vector3.zero; bool ok=false;
            for(int tries=0;tries<4&&!ok;tries++){ pos=p+Quaternion.Euler(0,Random.Range(-20f,20f),0)*back*dist; pos.y=FloorY; ok=!Physics.Linecast(p+Vector3.up,pos+Vector3.up,~0,QueryTriggerInteraction.Ignore); dist-=.45f; }
            if(!ok)return false;
            SpawnShadow(pos,true); behind=true; behindT=0; Play(breath,.25f,pos+Vector3.up*1.5f); return true;
        }
        void StartJumpscare()
        {
            scareT=.9f; behind=false; Kick(.3f); flashA=.85f; Play(stinger,.65f); Flicker(.4f);
            if(!game.data.jumpSeen&&game.story){ game.data.jumpSeen=true; game.Save(); StartCoroutine(AfterScare("Arda","…Kalbim yerinden çıkacaktı. Bu ev yalnızca dedemin anılarıyla dolu değil.")); }
        }
        IEnumerator AfterScare(string who,string text){ yield return new WaitForSeconds(1.3f); if(game.story)game.story.Remark(who,text); }
        void Kick(float duration){ kickT=kickDur=duration; }
        void LateUpdate()
        {
            if(kickT<=0||!game||game.expansion==null)return; kickT-=Time.deltaTime; var cam=game.player.view;
            float k=Mathf.Sin(Mathf.PI*Mathf.Clamp01(1-kickT/kickDur)); cam.fieldOfView=game.expansion.Data.fov-16f*k; if(kickT<=0)cam.fieldOfView=game.expansion.Data.fov;
        }

        // ---------- eşya kıpırtısı ----------
        public void MoveChair()
        {
            var cam=game.player.view.transform; var p=game.player.transform.position; bool up=Upstairs; var found=new List<Transform>();
            foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if(t.name!="Döşemeli butik sandalye"&&t.name!="VNB Chair 2")continue; bool atHome=t.position.y>3.4f; if(atHome!=up)continue;
                var d=t.position-p; if(d.magnitude<3f)continue; if(Vector3.Angle(cam.forward,d)<55f&&d.magnitude<14f)continue;   // oyuncunun gözü önünde oynatma
                found.Add(t);
            }
            if(found.Count==0)return; var chair=found[Random.Range(0,found.Count)];
            if(!chairHome.ContainsKey(chair))chairHome[chair]=new Pose(chair.position,chair.rotation); var home=chairHome[chair];
            var off=Quaternion.Euler(0,Random.Range(0f,360f),0)*Vector3.forward*Random.Range(.25f,.55f); chair.position=home.position+off; chair.rotation=home.rotation*Quaternion.Euler(0,Random.Range(-30f,30f),0);
            Play(creak,.5f,chair.position);
        }

        // ---------- TV kendiliğinden açılır ----------
        public void TvOn(string message,bool scare=false)
        {
            if(!tvQuad)
            {
                tvQuad=GameObject.CreatePrimitive(PrimitiveType.Quad); tvQuad.name="Hayalet TV"; Destroy(tvQuad.GetComponent<Collider>()); tvQuad.transform.SetParent(game.world.transform,false);
                tvQuad.transform.position=TvScreen; tvQuad.transform.rotation=Quaternion.Euler(0,180,0); tvQuad.transform.localScale=new Vector3(.8f,.46f,1);
                tvStatic=new Texture2D(48,27,TextureFormat.RGBA32,false){filterMode=FilterMode.Point}; tvPixels=new Color32[48*27];
                var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetTexture("_BaseMap",tvStatic); m.mainTexture=tvStatic; tvQuad.GetComponent<Renderer>().sharedMaterial=m;
                var tg=new GameObject("Hayalet TV yazısı"); tg.transform.SetParent(tvQuad.transform.parent,false); tg.transform.position=TvScreen+new Vector3(0,0,.012f); tg.transform.rotation=Quaternion.Euler(0,180,0);
                tvText=tg.AddComponent<TextMesh>(); tvText.fontSize=64; tvText.characterSize=.028f; tvText.anchor=TextAnchor.MiddleCenter; tvText.alignment=TextAlignment.Center; tvText.color=new Color(.85f,1f,.9f);
                tg.AddComponent<CafeDepthLabel>().Setup(tvText,CafeTypography.Strong);
                tvEyes=new GameObject("Hayalet TV gözleri"); tvEyes.transform.SetParent(tvQuad.transform.parent,false);
                var em=new Material(Shader.Find("Universal Render Pipeline/Unlit")); em.SetColor("_BaseColor",new Color(.95f,.98f,1f));
                foreach(float side in new[]{-1f,1f}){ var e=GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(e.GetComponent<Collider>()); e.transform.SetParent(tvEyes.transform,false); e.transform.position=TvScreen+new Vector3(side*.11f,.02f,.016f); e.transform.rotation=Quaternion.Euler(0,180,0); e.transform.localScale=new Vector3(.07f,.045f,1); e.GetComponent<Renderer>().sharedMaterial=em; }
            }
            tvText.text=message??TvMessages[Random.Range(0,TvMessages.Length)]; tvTime=scare?8f:7f; tvScare=scare; tvScared=false; tvStep=0; tvEyes.SetActive(false);
            tvQuad.SetActive(true); tvText.gameObject.SetActive(true); Play(staticBurst,.5f,TvScreen); Flicker(.6f);
        }
        void TickTv(float dt)
        {
            if(tvTime<=0||!tvQuad)return; tvTime-=dt; tvStep-=dt;
            if(tvScared)
            {   // jumpscare anı: ekran kararır, iki parlak göz
                if(tvTime<=0){ tvQuad.SetActive(false); tvText.gameObject.SetActive(false); tvEyes.SetActive(false); tvScared=false; }
                return;
            }
            if(tvStep<=0)
            {
                tvStep=.08f; int band=Random.Range(0,27);
                for(int i=0;i<tvPixels.Length;i++){ byte g=(byte)Random.Range(30,190); if(i/48==band||i/48==band+1)g=(byte)Mathf.Min(255,g+70); tvPixels[i]=new Color32(g,g,(byte)Mathf.Min(255,g+10),255); }
                tvStatic.SetPixels32(tvPixels); tvStatic.Apply(false);
            }
            if(tvText)tvText.gameObject.SetActive(tvTime%2f<1.5f&&tvTime<6.2f);
            if(tvScare&&tvTime<5f)
            {   // oyuncu ekrana bakıyorsa tetikle
                var cam=game.player.view.transform; var to=TvScreen-cam.position;
                if(to.magnitude<6f&&Vector3.Angle(cam.forward,to)<48f){ tvScared=true; tvTime=.9f; for(int i=0;i<tvPixels.Length;i++)tvPixels[i]=new Color32(0,0,0,255); tvStatic.SetPixels32(tvPixels); tvStatic.Apply(false); tvText.gameObject.SetActive(false); tvEyes.SetActive(true); Play(stinger,.65f); Kick(.3f); flashA=.7f; if(!game.data.jumpSeen&&game.story){ game.data.jumpSeen=true; game.Save(); StartCoroutine(AfterScare("Arda","…O televizyon kendi kendine açılmıyor. Dedem bunu biliyor muydu?")); } }
            }
            if(tvTime<=0&&!tvScared){ tvQuad.SetActive(false); if(tvText)tvText.gameObject.SetActive(false); }
        }
    }
}
