using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace CafeLife
{
    public sealed partial class CafeWorld : MonoBehaviour
    {
        public CafeGame game;
        public GameObject steam;
        private readonly Dictionary<string,GameObject> assets=new Dictionary<string,GameObject>();
        private readonly Dictionary<string,GameObject> cityAssets=new Dictionary<string,GameObject>();
        private readonly Dictionary<Color,Material> colors=new Dictionary<Color,Material>();
        private readonly Dictionary<Material,Material> converted=new Dictionary<Material,Material>();
        private readonly List<GameObject> tables=new List<GameObject>(),decor=new List<GameObject>();
        private readonly Dictionary<int,GameObject> branchBuildings=new Dictionary<int,GameObject>();
        private readonly Dictionary<int,TextMesh> branchSigns=new Dictionary<int,TextMesh>();
        private readonly Dictionary<int,List<GameObject>> branchPaintWalls=new Dictionary<int,List<GameObject>>();
        private readonly List<GameObject> mainPaintWalls=new List<GameObject>();
        private GameObject staff,apron,staffHeldCup; private Transform machine; private Animator staffAnimator; private TextMesh staffLabel; private int staffLevel=1;
        public Material ColorMaterial(Color color)
        {
            if(colors.TryGetValue(color,out var m)) return m;
            var glassAsset=color.a<.99f?Resources.Load<Material>("CafeMaterials/CafeGlass"):null;
            m=glassAsset?new Material(glassAsset):new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color;
            if(color.a<.99f && !glassAsset) { m.SetFloat("_Surface",1); m.SetFloat("_Blend",0); m.SetFloat("_SrcBlend",5);
                m.SetFloat("_DstBlend",10); m.SetFloat("_ZWrite",0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue=3000; }
            colors[color]=m; return m;
        }
        public GameObject Model(string name,Vector3 at,float width,float rotation=0,bool solid=true)
        {
            if(!assets.TryGetValue(name,out var prefab)) throw new System.InvalidOperationException("Eksik model: "+name);
            var root=new GameObject(name); root.transform.SetParent(transform,false);
            var model=Instantiate(prefab,root.transform); model.transform.localPosition=Vector3.zero; model.transform.localRotation=Quaternion.identity;
            foreach(var c in model.GetComponentsInChildren<Collider>()) { c.enabled=false; Destroy(c); }
            var renderers=model.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) throw new System.InvalidOperationException("Model görünümü yok: "+name);
            foreach(var renderer in renderers)
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++) mats[i]=ConvertMaterial(mats[i]);
                renderer.sharedMaterials=mats;
            }
            Bounds bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            float factor=width/Mathf.Max(.001f,bounds.size.x); model.transform.localScale*=factor;
            bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(solid) { var box=root.AddComponent<BoxCollider>(); box.center=new Vector3(0,bounds.size.y/2,0); box.size=bounds.size; }
            root.transform.position=at; root.transform.rotation=Quaternion.Euler(0,rotation,0); return root;
        }
        Material ConvertMaterial(Material source)
        {
            if(!source || source.shader && (source.shader.name.StartsWith("Universal Render Pipeline/") || source.shader.name.StartsWith("Shader Graphs/"))) return source;
            if(converted.TryGetValue(source,out var result)) return result;
            result=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            result.name=source.name+" (URP)";
            if(source.HasProperty("_Color")) result.SetColor("_BaseColor",source.GetColor("_Color"));
            else if(source.HasProperty("_BaseColor")) result.SetColor("_BaseColor",source.GetColor("_BaseColor"));
            string textureProperty=source.HasProperty("_MainTex")?"_MainTex":source.HasProperty("_BaseMap")?"_BaseMap":null;
            if(textureProperty!=null && source.GetTexture(textureProperty))
            {
                result.SetTexture("_BaseMap",source.GetTexture(textureProperty));
                result.SetTextureScale("_BaseMap",source.GetTextureScale(textureProperty));
                result.SetTextureOffset("_BaseMap",source.GetTextureOffset(textureProperty));
            }
            if(source.HasProperty("_Metallic")) result.SetFloat("_Metallic",source.GetFloat("_Metallic"));
            result.SetFloat("_Smoothness",source.HasProperty("_Glossiness")?source.GetFloat("_Glossiness"):.25f);
            if(source.name.ToLowerInvariant().Contains("transparent") || source.HasProperty("_Mode") && source.GetFloat("_Mode")>=2)
            {
                result.SetFloat("_Surface",1); result.SetFloat("_Blend",0); result.SetFloat("_SrcBlend",5);
                result.SetFloat("_DstBlend",10); result.SetFloat("_ZWrite",0);
                result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); result.renderQueue=3000;
            }
            converted[source]=result; return result;
        }
        public Vector3 TableCenter(int i) => new Vector3(new[]{-6.6f,-2.6f,2.6f,6.6f}[i%4],0,-7f+(i/4)*3.4f);
        public Vector3 Seat(int i) { Vector3 p=TableCenter(i/2); if(game.expansion && game.expansion.Data.layout==1)p+=Vector3.forward*((i/2)%2==0?.3f:-.3f); p.x+=(i%2==0?-.95f:.95f); p.y=.34f; return p; }
        GameObject City(string key,Vector3 pos,float scale=1,float yaw=0)
        {
            if(!cityAssets.TryGetValue(key,out var prefab) || !prefab)return null;
            var obj=Instantiate(prefab,pos,Quaternion.Euler(0,yaw,0),transform); obj.name=key; obj.transform.localScale=Vector3.one*scale;
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>()) { var mats=renderer.sharedMaterials; for(int i=0;i<mats.Length;i++)mats[i]=ConvertMaterial(mats[i]); renderer.sharedMaterials=mats; }
            return obj;
        }
        public static Vector3 CompactStreet(Vector3 p)=>new Vector3(p.x*.5f,p.y,-14+(p.z+14)*.5f);
        void BuildCityDistrict()
        {
            Color road=new Color(.12f,.15f,.18f),walk=new Color(.61f,.59f,.53f),stripe=new Color(.94f,.82f,.48f);
            for(int z=-22;z>=-190;z-=34)
            {
                Block("Bulvar asfaltı",new Vector3(0,-.15f,z),new Vector3(280,.22f,8),road);
                Block("Kaldırım şeridi",new Vector3(0,-.07f,z+5),new Vector3(280,.16f,1.4f),walk);
                Block("Kaldırım şeridi",new Vector3(0,-.07f,z-5),new Vector3(280,.16f,1.4f),walk);
                for(int x=-128;x<=128;x+=14) Block("Şerit çizgisi",new Vector3(x,-.028f,z),new Vector3(6,.025f,.12f),stripe);
                for(int crossingX=-120;crossingX<=120;crossingX+=48)
                    for(int mark=0;mark<6;mark++) Block("Yaya geçidi",new Vector3(crossingX-3+mark*1.2f,-.026f,z),new Vector3(.58f,.025f,6),new Color(.88f,.88f,.82f));
                Vehicle("small_car",new Vector3(-18,0,z+1.9f),90); Vehicle("taxi",new Vector3(18,0,z-1.9f),270);
            }
            for(int x=-120;x<=120;x+=48)
            {
                Block("Cadde asfaltı",new Vector3(x,-.14f,-105),new Vector3(8,.22f,210),road);
                Block("Cadde kaldırımı",new Vector3(x+5,-.07f,-105),new Vector3(1.4f,.16f,210),walk);
                Block("Cadde kaldırımı",new Vector3(x-5,-.07f,-105),new Vector3(1.4f,.16f,210),walk);
            }
            string[] buildings={"bakery","market","pizzeria","blue_house","yellow_house","cinema","green_house","hotel"};
            int n=0;
            for(int z=-38;z>=-178;z-=34) for(int x=-96;x<=96;x+=48)
            {
                bool reserved=(Mathf.Abs(x+96)<15&&Mathf.Abs(z+48)<15)||(Mathf.Abs(x+48)<15&&Mathf.Abs(z+82)<15)||
                    (Mathf.Abs(x-0)<15&&Mathf.Abs(z+116)<15)||(Mathf.Abs(x-48)<15&&Mathf.Abs(z+150)<15)||(Mathf.Abs(x-48)<15&&Mathf.Abs(z+82)<15);
                if(reserved)continue;
                string key=buildings[n%buildings.Length]; var building=City(key,new Vector3(x,0,z),1.15f,n%2==0?180:0);
                if(building) AddVenue(building,key,n); n++;
            }
            for(int roadRow=0;roadRow<5;roadRow++) for(int x=-112;x<=112;x+=28)
            {
                float z=-27-roadRow*34;
                City(roadRow%2==0?"lantern":"bench",new Vector3(x,0,z),1,0);
                if(x%56==0) City("tree_2",new Vector3(x+8,0,z-7),1.2f,0);
            }
            // The starter district has a generous public square before the deeper city grid.
            Block("Kafe meydanı - taş döşeme",new Vector3(0,-.25f,-25),new Vector3(52,.12f,12),new Color(.67f,.63f,.54f));
            for(int x=-21;x<=21;x+=14) City("Bench01",new Vector3(x,0,-27),1,90);
            City("Lamp01",new Vector3(-24,0,-25),1,0); City("Lamp01",new Vector3(24,0,-25),1,0);
            BuildBranchStores(); BuildCasino();
        }
        void Vehicle(string key,Vector3 position,float yaw)
        {
            var car=City(key,Vector3.zero,1,0); if(!car)return;
            var renderers=car.GetComponentsInChildren<Renderer>(); if(renderers.Length==0)return;
            Bounds b=renderers[0].bounds; foreach(var r in renderers)b.Encapsulate(r.bounds);
            car.transform.localScale*=4.3f/Mathf.Max(b.size.x,b.size.z);
            b=renderers[0].bounds; foreach(var r in renderers)b.Encapsulate(r.bounds);
            car.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
            var root=new GameObject("Gerçek ölçekte "+key); root.transform.SetParent(transform,false);
            car.transform.SetParent(root.transform,true); root.transform.position=position; root.transform.rotation=Quaternion.Euler(0,yaw,0);
            if(!car.GetComponentInChildren<Collider>()) { var box=car.AddComponent<BoxCollider>(); box.center=Vector3.up*(b.size.y/2/car.transform.lossyScale.x); box.size=b.size/car.transform.lossyScale.x; }
        }
        void AddVenue(GameObject building,string key,int id)
        {
            string title=key=="bakery"?"FIRIN · Servis ortaklığı":key=="market"?"MARKET · Toptan tedarik":key=="pizzeria"?"PİZZACI · Ortak tanıtım":key=="cinema"?"SİNEMA · Reklam":key=="hotel"?"OTEL · Kahvaltı sözleşmesi":key=="blue_house"?"KONUT · Mahalle teslimatı":key=="yellow_house"?"ATÖLYE · Barista eğitimi":"KOMŞULAR · Mahalle buluşması";
            var renderers=building.GetComponentsInChildren<Renderer>(); Bounds bounds=new Bounds(building.transform.position,Vector3.zero);
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Vector3 entry=new Vector3(bounds.center.x,0,bounds.max.z+1.3f);
            var marker=new GameObject(title); marker.transform.SetParent(transform,false); marker.transform.position=entry; marker.layer=8;
            var venue=marker.AddComponent<CafeVenue>(); venue.id=id; venue.category=key; venue.title=title; venue.game=game;
            var target=marker.AddComponent<CafeTarget>(); target.kind="venue"; target.venue=venue;
            var sphere=marker.AddComponent<SphereCollider>(); sphere.isTrigger=true; sphere.radius=1.5f; sphere.center=Vector3.up;
            Round("Hizmet giriş işareti",entry+Vector3.up*.02f,new Vector3(1.3f,.025f,1.3f),new Color(.15f,.65f,.48f));
            Sign(title,entry+Vector3.up*2.4f,.045f,new Color(1,.88f,.6f),180);
        }
        public void PrepareAvatar(GameObject avatar)
        {
            var renderers=avatar.GetComponentsInChildren<Renderer>(); if(renderers.Length==0)return;
            Bounds bounds=renderers[0].bounds; foreach(var r in renderers) { bounds.Encapsulate(r.bounds); var mats=r.sharedMaterials; for(int i=0;i<mats.Length;i++)mats[i]=ConvertMaterial(mats[i]); r.sharedMaterials=mats; }
            avatar.transform.localScale*=1.75f/Mathf.Max(.01f,bounds.size.y);
            bounds=renderers[0].bounds; foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            avatar.transform.position-=Vector3.up*(bounds.min.y-avatar.transform.parent.position.y);
        }
        void BuildBranchStores()
        {
            Vector3[] locations={new Vector3(-96,0,-48),new Vector3(-48,0,-82),new Vector3(0,0,-116),new Vector3(48,0,-150)};
            for(int i=1;i<=4;i++)
            {
                var b=City("coffe_shop",locations[i-1],1f+i*.16f,180);
                if(b)branchBuildings[i]=b;
                var sign=new GameObject("Şube tabelası"); sign.transform.SetParent(transform,false); sign.transform.position=locations[i-1]+new Vector3(0,7,7);
                var text=sign.AddComponent<TextMesh>(); text.fontSize=64; text.characterSize=.11f; text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; branchSigns[i]=text;
                var panel=new GameObject("Kafe yönetim noktası "+i); panel.transform.SetParent(transform,false); panel.transform.position=locations[i-1]+new Vector3(0,0,8);
                panel.layer=8; var trigger=panel.AddComponent<SphereCollider>(); trigger.isTrigger=true; trigger.radius=2.8f;
                var target=panel.AddComponent<CafeTarget>(); target.kind="branch"; target.branchIndex=i;
                BuildBranchInterior(i,new Vector3(174+(i-1)*29,0,-35));
            }
        }
        void BuildBranchInterior(int index,Vector3 origin)
        {
            Color[] wallColors={new Color(.85f,.79f,.67f),new Color(.7f,.84f,.78f),new Color(.9f,.74f,.61f),new Color(.75f,.77f,.9f),new Color(.9f,.82f,.61f)};
            Color wall=wallColors[index%wallColors.Length], dark=new Color(.22f,.12f,.08f), stone=new Color(.38f,.4f,.38f);
            var paintWalls=new List<GameObject>();
            Block("Şube "+index+" iç zemin",origin+new Vector3(0,-.15f,0),new Vector3(20,.3f,20),new Color(.72f,.68f,.58f));
            paintWalls.Add(Block("Şube arka duvar",origin+new Vector3(0,2.1f,10),new Vector3(20,4.2f,.25f),wall));
            paintWalls.Add(Block("Şube sol duvar",origin+new Vector3(-10,2.1f,0),new Vector3(.25f,4.2f,20),wall));
            paintWalls.Add(Block("Şube sağ duvar",origin+new Vector3(10,2.1f,0),new Vector3(.25f,4.2f,20),wall));
            paintWalls.Add(Block("Şube ön cephe sol",origin+new Vector3(-7,2.1f,-10),new Vector3(6,4.2f,.25f),dark));
            paintWalls.Add(Block("Şube ön cephe sağ",origin+new Vector3(7,2.1f,-10),new Vector3(6,4.2f,.25f),dark));
            branchPaintWalls[index]=paintWalls;
            Block("Şube tavan",origin+new Vector3(0,4.3f,0),new Vector3(20,.2f,20),new Color(.91f,.88f,.8f));
            Block("Şube bar gövdesi",origin+new Vector3(0,.55f,-5.2f),new Vector3(8,.95f,.85f),dark);
            Block("Şube bar tezgâhı",origin+new Vector3(0,1.08f,-5.2f),new Vector3(8.1f,.12f,1.05f),stone);
            Model("Coffee_Machine",origin+new Vector3(-1,1.16f,-5.2f),.9f,0,false);
            Model("Display_Counter",origin+new Vector3(7.1f,0,5.7f),1.25f,90,false);
            int tableCount=5+index*2;
            for(int t=0;t<tableCount;t++)
            {
                int col=t%4,row=t/4; Vector3 p=origin+new Vector3(-7.2f+col*4.8f,0,-.8f+row*3.2f);
                Model("Table_Circle",p,1.3f,0,false);
                Model("Chair",p+Vector3.right*.95f,.62f,270,false); Model("Chair",p+Vector3.left*.95f,.62f,90,false);
                if(t%2==0)Model("Chair",p+Vector3.forward*.95f,.62f,180,false);
            }
            Sign("ŞUBE "+index+"  •  MÜŞTERİ ALANI",origin+new Vector3(0,3.1f,9.75f),.045f,new Color(1,.8f,.45f));
            var exit=new GameObject("Şube çıkış noktası "+index); exit.transform.SetParent(transform,false); exit.transform.position=origin+new Vector3(0,1,-8.3f); exit.layer=8;
            exit.transform.position=origin+new Vector3(0,1,-9.2f);
            var trigger=exit.AddComponent<SphereCollider>(); trigger.isTrigger=true; trigger.radius=1.15f;
            var target=exit.AddComponent<CafeTarget>(); target.kind="branch_exit"; target.branchIndex=index;
            var manager=new GameObject("Şube yönetim paneli "+index); manager.transform.SetParent(transform,false); manager.transform.position=origin+new Vector3(7,1,-8.2f); manager.layer=8;
            var manage=manager.AddComponent<SphereCollider>(); manage.isTrigger=true; manage.radius=.8f;
            var manageTarget=manager.AddComponent<CafeTarget>(); manageTarget.kind="branch"; manageTarget.branchIndex=index;
        }
        public Vector3 BranchRoomCenter(int index) => new Vector3(174+(index-1)*29,0,-35);
        public Vector3 BranchStreetEntry(int index)
        {
            Vector3[] locations={new Vector3(-96,0,-48),new Vector3(-48,0,-82),new Vector3(0,0,-116),new Vector3(48,0,-150)};
            return CompactStreet(locations[Mathf.Clamp(index,1,4)-1]+new Vector3(0,0,11));
        }
        void BuildCasino()
        {
            Vector3 c=new Vector3(55,0,-86); Color burgundy=new Color(.23f,.035f,.075f),gold=new Color(.9f,.66f,.23f),dark=new Color(.08f,.07f,.09f);
            Block("Blackjack salonu - zemin",c+new Vector3(0,-.12f,0),new Vector3(22,.24f,16),new Color(.22f,.16f,.18f));
            Block("Casino arka duvar",c+new Vector3(0,4,-8),new Vector3(22,8,.5f),burgundy);
            Block("Casino sol duvar",c+new Vector3(-11,4,0),new Vector3(.5f,8,16),dark);
            Block("Casino sağ duvar",c+new Vector3(11,4,0),new Vector3(.5f,8,16),dark);
            Block("Casino ön cephe sol",c+new Vector3(-8,3.5f,8),new Vector3(6,7,.5f),burgundy);
            Block("Casino ön cephe sağ",c+new Vector3(8,3.5f,8),new Vector3(6,7,.5f),burgundy);
            Block("Casino üst saçak",c+new Vector3(0,7.2f,8),new Vector3(22,.7f,.9f),gold);
            Block("Casino kırmızı tabela",c+new Vector3(0,6.1f,8.4f),new Vector3(10,1.2f,.2f),dark);
            Sign("ROYAL JACKPOT • BLACKJACK",c+new Vector3(0,6.12f,8.55f),.075f,new Color(1,.75f,.24f));
            for(int i=-1;i<=1;i++)
            {
                var top=Round("Blackjack masası",c+new Vector3(i*5,.85f,1),new Vector3(2.9f,.16f,1.6f),new Color(.05f,.35f,.21f));
                Round("Masa ayağı",c+new Vector3(i*5,.4f,1),new Vector3(.32f,.9f,.32f),dark);
                for(int j=-1;j<=1;j++) Round("Fiş yığını",c+new Vector3(i*5+j*.28f,.99f,.85f),new Vector3(.16f,.16f,.16f),j==0?gold:new Color(.75f,.12f,.2f));
            }
            var entry=new GameObject("Blackjack giriş alanı"); entry.transform.SetParent(transform,false); entry.transform.position=c+new Vector3(0,1,7.4f); entry.layer=8;
            var hit=entry.AddComponent<SphereCollider>(); hit.isTrigger=true; hit.radius=2.5f; entry.AddComponent<CafeTarget>().kind="casino";
        }
        static float Top(GameObject go)
        { float top=float.MinValue; foreach(var r in go.GetComponentsInChildren<Renderer>()) top=Mathf.Max(top,r.bounds.max.y); return top; }
        GameObject Block(string n,Vector3 p,Vector3 size,Color c)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=n; go.transform.SetParent(transform); go.transform.position=p; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=ColorMaterial(c); return go; }
        GameObject Round(string n,Vector3 p,Vector3 size,Color c)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name=n; go.transform.SetParent(transform); go.transform.position=p; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=ColorMaterial(c); return go; }
        void Target(GameObject go,string kind)
        {
            var t=go.AddComponent<CafeTarget>(); t.kind=kind;
            var renderers=go.GetComponentsInChildren<Renderer>();
            var volume=new GameObject("Geniş etkileşim alanı"); volume.layer=8; volume.transform.SetParent(go.transform,false);
            var sphere=volume.AddComponent<SphereCollider>(); sphere.isTrigger=true;
            if(renderers.Length==0) { volume.transform.localPosition=Vector3.up*1.15f; sphere.radius=.34f; return; }
            Bounds bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            volume.transform.position=bounds.center; sphere.radius=Mathf.Clamp(bounds.extents.magnitude+.04f,.18f,.42f);
        }
        void Sign(string text,Vector3 pos,float size,Color color,float yaw=0,Vector2? printArea=null)
        {
            var go=new GameObject(text); go.transform.SetParent(transform); go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var tm=go.AddComponent<TextMesh>(); tm.text=text; tm.characterSize=size; tm.fontSize=64; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=color;go.AddComponent<CafeDepthLabel>().Setup(tm,printArea.HasValue?CafeTypography.Strong:null);if(printArea.HasValue)go.AddComponent<CafePrintedLabel>().area=printArea.Value;
        }
        public void Build()
        {
            foreach(var prefab in game.prefabs) if(prefab) assets[prefab.name]=prefab;
            if(game.cityPrefabs!=null) foreach(var prefab in game.cityPrefabs) if(prefab) cityAssets[prefab.name]=prefab;
            if(game.homePrefabs!=null) foreach(var prefab in game.homePrefabs) if(prefab) assets["VNB "+prefab.name]=prefab; // Üst kat evi: "VNB Sofa" gibi çağrılır
            Color cream=new Color(.8f,.73f,.6f), wood=new Color(.2f,.1f,.06f);
            Block("Zemin",new Vector3(0,-.15f,-1),new Vector3(20,.3f,20),cream);
            for(int x=-8;x<=8;x+=2) for(int z=-9;z<=7;z+=2) {
                var tile=Model("Floor",new Vector3(x,-.025f,z),1.99f,0,false);
                var r=tile.GetComponentsInChildren<Renderer>(); Bounds b=r[0].bounds; foreach(var a in r) b.Encapsulate(a.bounds);
                tile.transform.position+=Vector3.up*(-b.max.y+.015f);
            }
            mainPaintWalls.Add(Block("Arka duvar",new Vector3(0,1.7f,9),new Vector3(20,3.4f,.2f),cream));
            mainPaintWalls.Add(Block("Sol duvar",new Vector3(-10,1.7f,-1),new Vector3(.2f,3.4f,20),cream));
            mainPaintWalls.Add(Block("Sağ duvar",new Vector3(10,1.7f,-1),new Vector3(.2f,3.4f,20),cream));
            Block("Ön sol",new Vector3(-8,.5f,-11),new Vector3(4,1,.2f),wood);
            Block("Ön sağ",new Vector3(8,.5f,-11),new Vector3(4,1,.2f),wood);
            Block("Kapı üstü",new Vector3(0,3,-11),new Vector3(20,.7f,.25f),wood);
            Block("Tavan",new Vector3(0,3.55f,-1),new Vector3(20,.15f,20),new Color(.88f,.84f,.74f));
            Block("Arka süpürgelik",new Vector3(0,.12f,8.84f),new Vector3(20,.24f,.1f),wood);
            Block("Sol süpürgelik",new Vector3(-9.84f,.12f,-1),new Vector3(.1f,.24f,20),wood);
            Block("Sağ süpürgelik",new Vector3(9.84f,.12f,-1),new Vector3(.1f,.24f,20),wood);
            foreach(float x in new[]{-9f,-6f,-3f,3f,6f,9f}) Block("Pencere doğraması",new Vector3(x,1.85f,-11),new Vector3(.075f,1.7f,.15f),wood);
            for(int i=0;i<5;i++) {
                float x=(i-2)*4;
                var shade=GameObject.CreatePrimitive(PrimitiveType.Cylinder); shade.name="Sarkıt lamba"; shade.transform.SetParent(transform); shade.transform.position=new Vector3(x,3.15f,0); shade.transform.localScale=new Vector3(.6f,.15f,.6f); shade.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.2f,.28f,.25f));
                var light=new GameObject("Sıcak iç ışık"); light.transform.SetParent(transform); light.transform.position=new Vector3(x,2.9f,0); var source=light.AddComponent<Light>(); source.type=LightType.Point; source.color=new Color(1,.85f,.64f); source.intensity=1.5f; source.range=6;
            }
            Sign("ARDA'S COFFEE",new Vector3(0,3,-11.2f),.055f,new Color(1,.85f,.5f));
            Sign("COFFEE • FRESH BAKERY",new Vector3(0,2.7f,4.85f),.045f,wood);
            // The long customer counter is fixed beside the front entrance. Customers
            // queue on -Z; the barista and restock route stay behind it on +Z.
            int serviceStart=transform.childCount;
            Color walnut=new Color(.24f,.12f,.07f), stone=new Color(.34f,.35f,.32f), brass=new Color(.78f,.57f,.27f);
            Block("Kahve barı - ahşap gövde",new Vector3(-.2f,.52f,-5.8f),new Vector3(10.4f,1.04f,.78f),walnut);
            Block("Taş çalışma tezgâhı",new Vector3(-.2f,1.08f,-5.8f),new Vector3(10.5f,.13f,1.05f),stone);
            Block("Pirinç ön bordür",new Vector3(-.2f,.98f,-6.36f),new Vector3(10.4f,.045f,.05f),brass);
            for(int i=-5;i<=5;i++) Block("Ahşap ön panel",new Vector3(-.2f+i*.98f,.51f,-6.21f),new Vector3(.035f,.85f,.025f),brass);
            Block("Ayak dayama",new Vector3(-.2f,.22f,-6.36f),new Vector3(10.3f,.055f,.08f),brass);
            var hotCups=Model("fastfood_Cup_1",new Vector3(-4.75f,1.15f,-5.8f),.28f,180); Target(hotCups,"cup_hot");
            var coldCups=Model("fastfood_Cup_2",new Vector3(-3.85f,1.15f,-5.8f),.27f,180); Target(coldCups,"cup_cold");
            hotCups.GetComponentInChildren<SphereCollider>().radius=.20f; coldCups.GetComponentInChildren<SphereCollider>().radius=.20f;
            var iceMachine=new GameObject("Buz makinesi"); iceMachine.transform.SetParent(transform); iceMachine.transform.position=new Vector3(-3f,0,-5.8f); Target(iceMachine,"ice");
            var iceHousing=Block("Buz makinesi çelik gövde",new Vector3(-3f,1.46f,-5.63f),new Vector3(.55f,.58f,.38f),new Color(.66f,.73f,.76f)); iceHousing.transform.SetParent(iceMachine.transform);
            var icePanel=Block("Buz makinesi kontrol paneli",new Vector3(-3f,1.56f,-5.835f),new Vector3(.38f,.18f,.025f),new Color(.12f,.2f,.24f)); icePanel.transform.SetParent(iceMachine.transform);
            var iceSpout=Block("Buz çıkış ağzı",new Vector3(-3f,1.31f,-5.87f),new Vector3(.21f,.1f,.14f),new Color(.22f,.27f,.29f)); iceSpout.transform.SetParent(iceMachine.transform);
            var iceBucket=Model("fastfood_Bucket",new Vector3(-3f,1.15f,-6.03f),.34f,180); iceBucket.transform.SetParent(iceMachine.transform);
            var grinder=new GameObject("Espresso değirmeni"); grinder.transform.SetParent(transform); grinder.transform.position=new Vector3(-1.9f,0,-5.8f); Target(grinder,"grinder");
            var grindBody=Block("Değirmen metal gövde",new Vector3(-1.9f,1.38f,-5.8f),new Vector3(.44f,.46f,.42f),new Color(.16f,.2f,.2f)); grindBody.transform.SetParent(grinder.transform);
            var hopper=Model("Jar",new Vector3(-1.9f,1.63f,-5.8f),.36f,0,false); hopper.transform.SetParent(grinder.transform);
            var chute=Block("Öğütücü çıkışı",new Vector3(-1.9f,1.24f,-6.06f),new Vector3(.2f,.1f,.18f),new Color(.72f,.69f,.62f)); chute.transform.SetParent(grinder.transform);
            var dial=Round("Öğütüm ayarı",new Vector3(-1.7f,1.44f,-6.05f),new Vector3(.09f,.035f,.09f),brass); dial.transform.rotation=Quaternion.Euler(90,0,0); dial.transform.SetParent(grinder.transform);
            var coffee=Model("Coffee_Machine",new Vector3(-.55f,1.15f,-5.8f),.85f,0); Target(coffee,"machine"); machine=coffee.transform;
            var water=Model("waterCooler",new Vector3(.53f,0,4.33f),.65f,180); Target(water,"water");
            var caramel=Model("bottle",new Vector3(.75f,1.15f,-5.8f),.2f,180); Target(caramel,"caramel");
            var vanilla=Model("bottle",new Vector3(1.7f,1.15f,-5.8f),.2f,180); Target(vanilla,"vanilla");
            BuildFilterStation(new Vector3(3.6f,1.15f,-5.8f));
            var carLabel=Block("Karamel etiketi",new Vector3(.75f,1.37f,-6.04f),new Vector3(.16f,.1f,.018f),new Color(.6f,.3f,.1f)); carLabel.GetComponent<Collider>().enabled=false;
            var vanLabel=Block("Vanilya etiketi",new Vector3(1.7f,1.37f,-6.04f),new Vector3(.16f,.1f,.018f),new Color(.94f,.8f,.48f)); vanLabel.GetComponent<Collider>().enabled=false;
            var hotLids=new GameObject("Karton kapak rafı"); hotLids.transform.SetParent(transform); hotLids.transform.position=new Vector3(2.8f,0,-5.8f); Target(hotLids,"lid_hot");
            var coldLids=new GameObject("Şeffaf plastik kapak rafı"); coldLids.transform.SetParent(transform); coldLids.transform.position=new Vector3(3.75f,0,-5.8f); Target(coldLids,"lid_cold");
            for(int i=0;i<3;i++)
            {
                Round("Karton kapak",new Vector3(2.8f,1.17f+i*.02f,-5.8f),new Vector3(.16f,.015f,.16f),new Color(.24f,.23f,.2f));
                Round("Şeffaf plastik kapak",new Vector3(3.75f,1.17f+i*.02f,-5.8f),new Vector3(.16f,.015f,.16f),new Color(.68f,.82f,.85f,.55f));
            }
            var pickup=Model("Tray",new Vector3(4.7f,1.15f,-5.8f),.8f,180); Target(pickup,"pickup");
            Sign("TESLİM",new Vector3(4.7f,2.05f,-5.8f),.045f,brass,180);
            var fridge=Model("refrigerator_1",new Vector3(-4.23f,0,3.7f),.78f,90); Target(fridge,"milk");
            Model("milk",new Vector3(-3.74f,1.15f,4.32f),.22f,90,false);
            var depot=Model("Modern Shelves",new Vector3(4.2f,0,4.25f),.9f,180); Target(depot,"depot");
            Model("crate",new Vector3(4.1f,0,3.72f),.48f,0,false);
            for(int i=0;i<2;i++)
            {
                float y=1.45f+i*.55f;
                Block("Arka bar açık raf",new Vector3(.1f,y,4.67f),new Vector3(4.5f,.085f,.55f),walnut);
                for(int j=0;j<5;j++) Model("Jar",new Vector3(-1.75f+j*.85f,y+.05f,4.63f),.23f,0,false);
            }
            Model("Menu_Board",new Vector3(-2.9f,1.6f,4.77f),1.1f,180,false);
            Model("Menu_Board",new Vector3(3,1.6f,4.77f),1.1f,180,false);
            for(int service=serviceStart;service<transform.childCount;service++)serviceObjects.Add(transform.GetChild(service));
            var display=Model("Display_Counter",new Vector3(-8.9f,0,-7.7f),1.2f,90); Target(display,"shop"); float pastryTop=Top(display);
            Model("Cake_1",new Vector3(-8.9f,pastryTop,-7.7f),.3f,0,false);
            Model("Croissant",new Vector3(-9.3f,pastryTop,-7.7f),.22f,0,false);
            Model("Donut_1",new Vector3(-8.5f,pastryTop,-7.7f),.22f,0,false);
            var bin=Model("Trash_Can",new Vector3(-6f,0,-6f),.55f); Target(bin,"trash");
            Sign("BARDAK  •  BUZ  •  DEĞİRMEN  •  ESPRESSO  •  ŞURUP  •  KAPAK  •  TESLİM",new Vector3(0,2.85f,4.84f),.025f,wood);
            for(int i=0;i<12;i++)
            {
                var root=new GameObject("Masa alanı "+i); root.transform.SetParent(transform);
                Vector3 p=TableCenter(i);
                var table=Model("Table_Circle",p,1.3f); table.transform.SetParent(root.transform);
                Model("Chair",p+Vector3.right*.95f,.62f,270,false).transform.SetParent(root.transform);
                Model("Chair",p+Vector3.left*.95f,.62f,90,false).transform.SetParent(root.transform);
                Model("Cup_With_Coffee",p+Vector3.up*Top(table),.15f,0,false).transform.SetParent(root.transform);
                tables.Add(root);
            }
            for(int i=0;i<6;i++) decor.Add(Model("Flower_Pot",new Vector3(-8+i*3.2f,0,-9.6f),.42f,0,false));
            Block("Kaldırım",new Vector3(0,-.15f,-13),new Vector3(40,.3f,4),new Color(.55f,.53f,.49f));
            Block("Şehir asfaltı",new Vector3(0,-.2f,-18),new Vector3(72,.3f,6),new Color(.15f,.18f,.22f));
            Block("Büyük şehir zemini",new Vector3(0,-.25f,-22),new Vector3(72,.3f,46),new Color(.34f,.4f,.31f));
            for(int i=0;i<8;i++) {
                float x=-28+i*8; Block("Yol kesikli çizgi",new Vector3(x,-.035f,-18),new Vector3(3,.02f,.1f),new Color(.94f,.82f,.45f));
                var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.transform.SetParent(transform); post.transform.position=new Vector3(x,1.4f,-8.8f); post.transform.localScale=new Vector3(.08f,1.4f,.08f); post.GetComponent<Renderer>().sharedMaterial=ColorMaterial(wood);
                post.transform.position=new Vector3(x,1.4f,-15f);
                var lamp=new GameObject("Sokak ışığı"); lamp.transform.SetParent(post.transform); lamp.transform.position=new Vector3(x,2.9f,-15f); var l=lamp.AddComponent<Light>(); l.type=LightType.Point; l.color=new Color(1,.75f,.4f); l.range=5; l.intensity=1;
            }
            var furniture=Model("Welcome_Board",new Vector3(-12,0,-15.5f),.8f,180); Target(furniture,"shop"); Sign("MOBİLYA / DEKOR",new Vector3(-12,1.8f,-15.5f),.035f,Color.white);
            var clothes=Model("Welcome_Board",new Vector3(12,0,-15.5f),.8f,180); Target(clothes,"shop"); Sign("KIYAFET / YÖNETİM",new Vector3(12,1.8f,-15.5f),.035f,Color.white);
            for(int i=0;i<4;i++) {
                Vector3 p=i<2?new Vector3(i==0?-140:140,1.5f,-85):new Vector3(0,1.5f,i==2?35:-205);
                Vector3 boundarySize=i<2?new Vector3(.3f,3,240):new Vector3(280,3,.3f);
                if(CafeFocusedMode.Enabled){p=i<2?new Vector3(i==0?-36:36,1.5f,-17.5f):new Vector3(0,1.5f,i==2?10:-45);boundarySize=i<2?new Vector3(.3f,3,55):new Vector3(72,3,.3f);}
                var boundary=Block("Alan sınırı",p,boundarySize,cream); boundary.GetComponent<Renderer>().enabled=false;
            }
            BuildBoulevard();
            if(!CafeFocusedMode.Enabled){
            var districtStart=transform.childCount;
            BuildCityDistrict();
            // Keep actors and vehicles life size; shorten the street grid and footprints.
            for(int i=districtStart;i<transform.childCount;i++) {
                var t=transform.GetChild(i); if(t.position.x>140)continue;
                t.position=CompactStreet(t.position);
                if(!t.name.StartsWith("Gerçek ölçekte") && !t.GetComponent<CafeTarget>() && !t.GetComponent<TextMesh>())
                    t.localScale=Vector3.Scale(t.localScale,new Vector3(.5f,1,.5f));
            }
            }
            City("Bench01",new Vector3(-7,0,-13),1,90); City("Lamp01",new Vector3(7,0,-13),1,0); City("TrashCan03",new Vector3(5,0,-13),1,0);
            staff=new GameObject("Barista"); staff.transform.SetParent(transform); staff.transform.position=new Vector3(.9f,0,-4.9f);
            if(game.customerPrefabs!=null && game.customerPrefabs.Length>0 && game.customerPrefabs[0])
            {
                var avatar=Instantiate(game.customerPrefabs[0],staff.transform); avatar.transform.localPosition=Vector3.zero;
                avatar.transform.localRotation=Quaternion.identity; avatar.transform.localScale=Vector3.one;
                RemoveDemoProps(avatar); PrepareAvatar(avatar);
                foreach(var collider in avatar.GetComponentsInChildren<Collider>()) Destroy(collider);
                staffAnimator=avatar.GetComponentInChildren<Animator>();
                if(staffAnimator) { staffAnimator.applyRootMotion=false; if(game.customerAnimator) staffAnimator.runtimeAnimatorController=game.customerAnimator; }
            }
            else
            {
                var torso=GameObject.CreatePrimitive(PrimitiveType.Capsule); torso.transform.SetParent(staff.transform,false); torso.transform.localPosition=Vector3.up; torso.transform.localScale=new Vector3(.45f,.65f,.4f); torso.GetComponent<Renderer>().sharedMaterial=ColorMaterial(wood); Destroy(torso.GetComponent<Collider>());
                var head=GameObject.CreatePrimitive(PrimitiveType.Sphere); head.transform.SetParent(staff.transform,false); head.transform.localPosition=Vector3.up*1.7f; head.transform.localScale=Vector3.one*.35f; head.GetComponent<Renderer>().sharedMaterial=ColorMaterial(cream); Destroy(head.GetComponent<Collider>());
            }
            var staffTag=new GameObject("Barista durumu"); staffTag.transform.SetParent(staff.transform,false); staffTag.transform.localPosition=Vector3.up*2.25f;
            staffLabel=staffTag.AddComponent<TextMesh>(); staffLabel.text="Barista"; staffLabel.fontSize=44; staffLabel.characterSize=.04f; staffLabel.anchor=TextAnchor.MiddleCenter; staffLabel.alignment=TextAlignment.Center; staffLabel.color=Color.white;
            steam=new GameObject("Kahve buharı"); steam.transform.SetParent(transform); steam.transform.position=coffee.transform.position+Vector3.up*.5f;
            var ps=steam.AddComponent<ParticleSystem>(); var main=ps.main; main.startLifetime=.6f; main.startSpeed=.25f; main.startSize=.06f; main.startColor=new Color(1,1,1,.3f); main.maxParticles=20;
            var emission=ps.emission; emission.rateOverTime=10; var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.radius=.06f;
            steam.GetComponent<ParticleSystemRenderer>().sharedMaterial=ColorMaterial(new Color(.8f,.8f,.8f)); steam.SetActive(false);
            var sun=new GameObject("Güneş").AddComponent<Light>(); sun.transform.SetParent(transform); sun.type=LightType.Directional; sun.intensity=1.2f; sun.transform.rotation=Quaternion.Euler(45,-25,0);
            RenderSettings.ambientLight=new Color(.7f,.7f,.73f);
            var player=new GameObject("Oyuncu"); player.transform.position=new Vector3(7,.08f,-7.8f); player.transform.rotation=Quaternion.Euler(0,288,0);
            var body=player.AddComponent<CharacterController>(); body.height=1.8f; body.center=Vector3.up*.9f; body.radius=.28f; body.stepOffset=.25f; body.skinWidth=.025f;
            var cam=new GameObject("Göz hizası"); cam.tag="MainCamera"; cam.transform.SetParent(player.transform,false); cam.transform.localPosition=Vector3.up*1.65f;
            var camera=cam.AddComponent<Camera>(); camera.nearClipPlane=.05f; camera.farClipPlane=90; camera.fieldOfView=72; cam.AddComponent<AudioListener>();
            game.player=player.AddComponent<CafePlayer>(); game.player.game=game; game.player.body=body; game.player.view=camera;
            apron=GameObject.CreatePrimitive(PrimitiveType.Capsule); apron.name="Kol / kıyafet"; Destroy(apron.GetComponent<Collider>()); apron.layer=2; apron.transform.SetParent(cam.transform,false); apron.transform.localPosition=new Vector3(.32f,-.36f,.35f); apron.transform.localScale=new Vector3(.12f,.28f,.12f); apron.transform.localRotation=Quaternion.Euler(75,0,0);
            Refresh();
        }
        public void Refresh()
        {
            for(int i=0;i<tables.Count;i++) tables[i].SetActive(i<game.data.tables);
            for(int i=0;i<decor.Count;i++) decor[i].SetActive(i<game.data.decor);
            if(staff) staff.SetActive(game.data.staff>0);
            if(machine) machine.localScale=Vector3.one*(1+(game.data.machine-1)*.025f);
            if(apron) apron.GetComponent<Renderer>().sharedMaterial=ColorMaterial(Color.HSVToRGB(game.data.outfit*.24f,.5f,.5f));
            RefreshBranch(0); for(int i=1;i<game.data.branches.Length;i++) RefreshBranch(i);
        }
        public void RefreshBranch(int index)
        {
            Color[] palette={new Color(.79f,.72f,.59f),new Color(.66f,.81f,.77f),new Color(.86f,.71f,.58f),new Color(.72f,.72f,.86f),new Color(.88f,.79f,.55f),new Color(.65f,.8f,.87f)};
            if(index==0)
            {
                var paint=ColorMaterial(game.data.paintLevel==0?palette[0]:palette[(game.data.paintLevel-1)%palette.Length]);
                foreach(var wall in mainPaintWalls) if(wall)wall.GetComponent<Renderer>().sharedMaterial=paint;
            }
            else if(branchBuildings.TryGetValue(index,out var building) && building)
            {
                var branch=game.data.branches[index]; Color tint=branch.paintLevel==0?Color.white:palette[(branch.paintLevel-1)%palette.Length];
                foreach(var renderer in building.GetComponentsInChildren<Renderer>()) renderer.material.color=tint;
                if(branch.paintLevel>0&&branchPaintWalls.TryGetValue(index,out var walls)) foreach(var wall in walls) if(wall)wall.GetComponent<Renderer>().sharedMaterial=ColorMaterial(tint);
                if(branchSigns.TryGetValue(index,out var sign) && sign) { sign.text=(branch.owned?"AÇIK":"KİRALIK")+" • "+branch.name; sign.color=branch.owned?new Color(1,.82f,.35f):Color.white; }
            }
        }
        static void RemoveDemoProps(GameObject avatar)
        {
            foreach(Transform child in avatar.GetComponentsInChildren<Transform>(true))
            { string prop=child.name.ToLowerInvariant(); if(prop.Contains("baseball")||prop.Contains("knife")||prop.Contains("handgun")||prop.Contains("weapon")||prop.Contains("sword")||prop.Contains("axe"))child.gameObject.SetActive(false); }
        }
        public void SetStaffStatus(string status) { if(staffLabel) staffLabel.text=status; }
        public void SetStaffLevel(int level) { staffLevel=Mathf.Clamp(level,1,5); }
        public IEnumerator StaffMoveTo(Vector3 destination)
        {
            destination=ServicePoint(destination);destination.y=0;
            while(staff && Vector3.Distance(new Vector3(staff.transform.position.x,0,staff.transform.position.z),destination)>.08f)
            {
                if(game.Paused) { if(staffAnimator) staffAnimator.SetBool("Moving",false); yield return null; continue; }
                Vector3 delta=destination-staff.transform.position; delta.y=0;
                if(staffAnimator) staffAnimator.SetBool("Moving",true);
                float speed=Mathf.Lerp(.65f,1.5f,Mathf.Clamp01((staffLevel-1)/4f));
                staff.transform.position=Vector3.MoveTowards(staff.transform.position,destination,speed*Time.deltaTime);
                if(delta.sqrMagnitude>.01f) staff.transform.rotation=Quaternion.Slerp(staff.transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*8f);
                
                yield return null;
            }
            if(staffAnimator) staffAnimator.SetBool("Moving",false);
        }
        public void StaffHoldCup(CupWork work)
        {
            StaffClearCup(); if(!staffAnimator || !staff) return;
            Transform hand=staffAnimator.isHuman?staffAnimator.GetBoneTransform(HumanBodyBones.RightHand):null; if(!hand) hand=staff.transform;
            staffHeldCup=CupModel(work,Economy.Recipes[work.recipe],transform,false);
            staffHeldCup.AddComponent<CafeMetricAttachment>().Setup(hand,hand==staff.transform?new Vector3(.32f,1.05f,.2f):new Vector3(0,.045f,.09f),Quaternion.Euler(-8,0,0),.6f);
        }
        public GameObject PlaceStaffCup(CupWork work,int slot)
        { StaffClearCup(); var visual=CupModel(work,Economy.Recipes[work.recipe],transform,false); PositionPrepared(visual,slot); AddProductLabel(visual,string.IsNullOrEmpty(work.productName)?Economy.Recipes[work.recipe].name:work.productName); return visual; }
        public void AddProductLabel(GameObject product,string name)
        {
            if(!product)return;
            var existing=product.transform.Find("İçecek adı");
            if(existing){existing.GetComponent<TextMesh>().text=name.Replace(" · ","\n");return;}
            var paperTag=GameObject.CreatePrimitive(PrimitiveType.Quad);paperTag.name="Sipariş baskı etiketi";paperTag.transform.SetParent(product.transform,false);paperTag.transform.localPosition=new Vector3(0,.08f,-.083f);paperTag.transform.localScale=new Vector3(.112f,.058f,1);paperTag.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.94f,.92f,.85f));Destroy(paperTag.GetComponent<Collider>());paperTag.layer=2;
            var tag=new GameObject("İçecek adı");tag.transform.SetParent(product.transform,false);tag.transform.localPosition=new Vector3(0,.08f,-.084f);tag.layer=2;
            var text=tag.AddComponent<TextMesh>();text.text=name.Replace(" · ","\n");text.fontSize=58;text.characterSize=.006f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.12f,.11f,.09f);tag.AddComponent<CafeDepthLabel>().Setup(text,CafeTypography.Strong);tag.AddComponent<CafePrintedLabel>().area=new Vector2(.102f,.048f);
        }
        public void StaffClearCup() { if(staffHeldCup) Destroy(staffHeldCup); staffHeldCup=null; }
        public GameObject CupModel(CupWork cup,Recipe recipe,Transform parent,bool inHand=true)
        {
            bool cold=cup.coldCup||cup.iced||recipe.cold;
            var root=new GameObject(cold?"Eldeki şeffaf plastik bardak":"Eldeki karton bardak");
            root.transform.SetParent(parent,false); root.transform.localPosition=inHand?new Vector3(.29f,-.31f,.55f):Vector3.zero;
            root.transform.localRotation=inHand?Quaternion.Euler(-12,0,-12):Quaternion.identity;root.transform.localScale=Vector3.one*.6f;
            Color outside=cold?new Color(.67f,.84f,.88f,.52f):new Color(.78f,.59f,.37f);
            var body=GameObject.CreatePrimitive(PrimitiveType.Cylinder); body.name="Bardak"; body.transform.SetParent(root.transform,false);
            body.transform.localPosition=Vector3.zero; body.transform.localScale=new Vector3(.15f,.14f,.15f);
            body.GetComponent<Renderer>().sharedMaterial=ColorMaterial(outside); Destroy(body.GetComponent<Collider>()); body.layer=2;
            if(cup.steps.Count>0)
            {
                Color liquid=(cup.brewed||cup.filtered)?(cup.hasMilk?new Color(.67f,.43f,.25f):new Color(.22f,.11f,.06f)):
                    cup.hasMilk?new Color(.91f,.87f,.74f):cup.hasWater?new Color(.65f,.82f,.89f):
                    cup.syrup!=""?new Color(.7f,.42f,.19f):new Color(.83f,.93f,.97f);
                var fill=GameObject.CreatePrimitive(PrimitiveType.Cylinder); fill.name="Bardaktaki karışım"; fill.transform.SetParent(root.transform,false);
                fill.transform.localPosition=new Vector3(0,.045f,0); fill.transform.localScale=new Vector3(.142f,.07f,.142f);
                fill.GetComponent<Renderer>().sharedMaterial=ColorMaterial(liquid); Destroy(fill.GetComponent<Collider>()); fill.layer=2;
            }
            if(cup.iced)
            {
                var ice=GameObject.CreatePrimitive(PrimitiveType.Cube); ice.name="Buz parçaları"; ice.transform.SetParent(root.transform,false);
                ice.transform.localPosition=new Vector3(-.035f,.115f,0); ice.transform.localScale=new Vector3(.065f,.045f,.06f);
                ice.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.84f,.94f,.98f)); Destroy(ice.GetComponent<Collider>()); ice.layer=2;
            }
            if(cup.syrup!="")
            {
                var swirl=GameObject.CreatePrimitive(PrimitiveType.Cylinder); swirl.name=cup.syrup=="vanilla"?"Vanilya şurubu":"Karamel şurubu"; swirl.transform.SetParent(root.transform,false);
                swirl.transform.localPosition=new Vector3(.055f,.12f,0); swirl.transform.localScale=new Vector3(.034f,.015f,.034f);
                swirl.GetComponent<Renderer>().sharedMaterial=ColorMaterial(cup.syrup=="vanilla"?new Color(.92f,.73f,.37f):new Color(.46f,.23f,.08f)); Destroy(swirl.GetComponent<Collider>()); swirl.layer=2;
            }
            if(cup.foamed && !cup.lidded)
            {
                var foam=GameObject.CreatePrimitive(PrimitiveType.Cylinder);foam.name="Süt köpüğü";foam.transform.SetParent(root.transform,false);foam.transform.localPosition=new Vector3(0,.128f,0);foam.transform.localScale=new Vector3(.14f,.012f,.14f);foam.GetComponent<Renderer>().sharedMaterial=ColorMaterial(cup.artPoints.Count>0?new Color(.48f,.27f,.12f):new Color(.97f,.92f,.8f));Destroy(foam.GetComponent<Collider>());foam.layer=2;
                if(cup.art && cup.artPoints.Count==0)for(int i=0;i<5;i++){var stroke=GameObject.CreatePrimitive(PrimitiveType.Sphere);stroke.name="Latte art yaprağı";stroke.transform.SetParent(root.transform,false);stroke.transform.localPosition=new Vector3(Mathf.Sin(i*2.4f)*.025f,.144f,-.035f+i*.017f);stroke.transform.localScale=new Vector3(.045f,.004f,.02f);stroke.GetComponent<Renderer>().sharedMaterial=ColorMaterial(new Color(.48f,.29f,.13f));Destroy(stroke.GetComponent<Collider>());stroke.layer=2;}
            }
            if(cup.lidded)
            {
                var lid=GameObject.CreatePrimitive(PrimitiveType.Cylinder); lid.name=recipe.cold?"Plastik kapak":"Karton kapak"; lid.transform.SetParent(root.transform,false);
                lid.transform.localPosition=new Vector3(0,.15f,0); lid.transform.localScale=new Vector3(.17f,.018f,.17f);
                lid.GetComponent<Renderer>().sharedMaterial=ColorMaterial(cold?new Color(.75f,.85f,.88f):new Color(.17f,.17f,.17f)); Destroy(lid.GetComponent<Collider>()); lid.layer=2;
            }
            var polish=game.GetComponent<CafePolish>();if(polish)polish.DecorateCup(root,cup,inHand);
            CafeLattePour.Decorate(root,cup,ColorMaterial(new Color(1,.95f,.8f)));
            return root;
        }
        public void PositionPrepared(GameObject visual,int slot)
        {
            if(!visual)return;
            // Keep all six drinks on the delivery tray, rather than among the lid dispensers.
            var tray=transform.Find("Tray");
            Vector3 center=tray?tray.position:ServicePoint(new Vector3(4.7f,1.15f,-5.8f));
            float top=center.y;
            if(tray)foreach(var renderer in tray.GetComponentsInChildren<Renderer>())top=Mathf.Max(top,renderer.bounds.max.y);
            visual.transform.position=new Vector3(center.x+((slot%3)-1)*.24f,top+.095f,center.z+((slot/3)-.5f)*.24f);
            visual.transform.rotation=Quaternion.identity;
        }
        void LateUpdate() { if(staffLabel && game && game.player && game.player.view) staffLabel.transform.rotation=game.player.view.transform.rotation; }
        void OnDestroy() { foreach(var material in colors.Values) if(material) Destroy(material); foreach(var material in converted.Values) if(material) Destroy(material); }
    }
}








