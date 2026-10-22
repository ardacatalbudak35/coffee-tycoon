using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CafeLife
{
    public sealed partial class CafePolish : MonoBehaviour
    {
        public CafeGame game;
        public Transform scenery;
        public Material oak,walnut,stone,brass,steel,fabric,ceramic,leaf,wall;
        readonly List<Material> materials=new List<Material>();readonly List<Texture2D> textures=new List<Texture2D>();
        VolumeProfile profile;Mesh bevel,cupMesh;readonly List<Mesh> combinedMeshes=new List<Mesh>();
        public void Build()
        {
            scenery=new GameObject("Kafe · tasarım ve ayrıntılar").transform;scenery.SetParent(game.world.transform,false);
            oak=Surface("Meşe",new Color(.48f,.32f,.18f),.24f,0,Wood(3));walnut=Surface("Ceviz",new Color(.22f,.12f,.07f),.32f,0,Wood(9));
            stone=Surface("Koyu taş",new Color(.18f,.2f,.19f),.42f,0,Stone());brass=Surface("Fırçalanmış pirinç",new Color(.65f,.44f,.19f),.5f,.75f);steel=Surface("Antrasit metal",new Color(.055f,.07f,.065f),.4f,.65f);
            fabric=Surface("Zeytin kumaş",new Color(.2f,.32f,.26f),.08f);ceramic=Surface("Krem seramik",new Color(.87f,.82f,.7f),.6f);leaf=Surface("Bitki",new Color(.16f,.32f,.17f),.12f);wall=Surface("Sıcak sıva",new Color(.67f,.62f,.51f),.05f,0,Plaster());
            oak.SetFloat("_ReceiveShadows",0);oak.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            foreach(var text in game.world.GetComponentsInChildren<TextMesh>())if(text.text=="TESLİM"||text.text.StartsWith("BARDAK  •")||text.text=="COFFEE • FRESH BAKERY")text.gameObject.SetActive(false);
            foreach(var r in game.world.GetComponentsInChildren<MeshRenderer>()){string n=r.name;if(n=="NOIR gece kulübü zemin"||n.StartsWith("Casino üst kat")||n=="Merdiven üst sahanlık")r.sharedMaterial=walnut;else if(n.StartsWith("NOIR gece kulübü")||n.StartsWith("Cadde marketi"))r.sharedMaterial=wall;else if(n=="Karşı kaldırım"||n=="Kaldırım")r.sharedMaterial=stone;}
            bevel=Lathe();Floor();Furniture();Bar();Decor();InteriorRevamp();RearFloorPlan();FinishBarEquipment();FinishNightInteriors();NaturalCafeDetails();CraftedInterior();RefinePropMaterials();RefineStreetArtDirection();Lighting();game.expansion.ApplyDecor();game.gameObject.AddComponent<CafeDecorProgression>().Setup(game,this);MergeScenery();
            game.player.view.farClipPlane=140;game.player.view.nearClipPlane=.08f;
            var arm=game.world.transform.Find("Kol / kıyafet");if(arm)arm.gameObject.SetActive(false);
            foreach(var transform in game.player.view.GetComponentsInChildren<Transform>())if(transform.name=="Kol / kıyafet")transform.gameObject.SetActive(false);
        }
        Material Surface(string name,Color color,float smooth,float metal=0,Texture2D texture=null)
        {var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=name;mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);if(texture)mat.SetTexture("_BaseMap",texture);mat.enableInstancing=true;materials.Add(mat);return mat;}
        Texture2D Wood(float seed)
        {var tex=new Texture2D(256,256,TextureFormat.RGB24,true);tex.wrapMode=TextureWrapMode.Repeat;tex.filterMode=FilterMode.Trilinear;tex.anisoLevel=8;for(int y=0;y<256;y++)for(int x=0;x<256;x++){float grain=Mathf.PerlinNoise(x*.08f+seed,y*.008f);float fine=Mathf.Sin(x*.5f+Mathf.PerlinNoise(seed+x*.012f,y*.02f)*5)*.025f;float v=.88f+grain*.12f+fine*.3f;tex.SetPixel(x,y,new Color(v,v*.96f,v*.89f));}tex.Apply();textures.Add(tex);return tex;}
        Texture2D Stone()
        {var tex=new Texture2D(128,128,TextureFormat.RGB24,true);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=.78f+Mathf.PerlinNoise(x*.17f,y*.17f)*.3f;tex.SetPixel(x,y,new Color(n,n,n));}tex.Apply();textures.Add(tex);return tex;}
        Texture2D Plaster()
        {var tex=new Texture2D(128,128,TextureFormat.RGB24,true);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=.9f+Mathf.PerlinNoise(x*.5f,y*.5f)*.1f;tex.SetPixel(x,y,new Color(n,n,n));}tex.Apply();textures.Add(tex);return tex;}
        GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=false)
        {var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;if(!collider){var c=obj.GetComponent<Collider>();c.enabled=false;Destroy(c);}return obj;}
        void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
        {var obj=Shape(name,PrimitiveType.Cylinder,parent,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),material);obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        GameObject Disc(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<MeshFilter>().sharedMesh=bevel;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;}
        Mesh Lathe()
        {
            const int segments=32;Vector2[] ring={new Vector2(0,-.5f),new Vector2(.46f,-.5f),new Vector2(.5f,-.3f),new Vector2(.5f,.3f),new Vector2(.46f,.5f),new Vector2(0,.5f)};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int j=0;j<ring.Length;j++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(a)*ring[j].x,ring[j].y,Mathf.Sin(a)*ring[j].x));uv.Add(new Vector2((Mathf.Cos(a)*ring[j].x+.5f)*2,(Mathf.Sin(a)*ring[j].x+.5f)*2));}
            for(int j=0;j<ring.Length-1;j++)for(int i=0;i<segments;i++){int a=j*(segments+1)+i,b=a+segments+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh{name="Yumuşak kenarlı dairesel yüzey"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void Floor()
        {
            var children=new List<Transform>();foreach(Transform t in game.world.transform)children.Add(t);
            foreach(var t in children)
            {
                if(t.name=="Floor"){t.gameObject.SetActive(false);Destroy(t.gameObject);}
                if(t.name=="Arka duvar"||t.name=="Sol duvar"||t.name=="Sağ duvar")t.GetComponent<Renderer>().sharedMaterial=wall;
                if(t.name=="Tavan")t.GetComponent<Renderer>().sharedMaterial=ceramic;
            }
            for(int x=0;x<20;x++)for(int z=0;z<5;z++)
            {var plank=Shape("Meşe döşeme",PrimitiveType.Cube,scenery,new Vector3(-9.5f+x,.018f,-9+z*4),new Vector3(.986f,.028f,3.985f),oak);plank.GetComponent<Renderer>().sharedMaterial=oak;}
            Shape("Bar çalışma alanı · taş zemin",PrimitiveType.Cube,scenery,new Vector3(0,.036f,-4.15f),new Vector3(10,.018f,2.45f),stone);
            Shape("Giriş paspası",PrimitiveType.Cube,scenery,new Vector3(0,.045f,-9.65f),new Vector3(3,.025f,1.25f),fabric);
            foreach(float x in new[]{-9.82f,9.82f})Shape("Ahşap duvar eteği",PrimitiveType.Cube,scenery,new Vector3(x,.42f,-1),new Vector3(.09f,.84f,19.4f),walnut);
        }
        void Furniture()
        {
            for(int i=0;i<12;i++)
            {
                var root=game.world.transform.Find("Masa alanı "+i);if(!root)continue;
                foreach(Transform child in root){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                var group=new GameObject("Butik masa seti "+i).transform;group.SetParent(root,false);Vector3 p=game.world.TableCenter(i);group.localPosition=p;
                Disc("Meşe masa tablası",group,new Vector3(0,.765f,0),new Vector3(1.22f,.075f,1.22f),oak);
                Disc("Pirinç kenar",group,new Vector3(0,.729f,0),new Vector3(1.225f,.014f,1.225f),brass);
                Rod("Döküm masa ayağı",group,new Vector3(0,.06f,0),new Vector3(0,.72f,0),.048f,steel);Disc("Döküm taban",group,new Vector3(0,.045f,0),new Vector3(.54f,.07f,.54f),steel);
                var collision=group.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,.42f,0);collision.size=new Vector3(.92f,.84f,.92f);
                Chair(group,new Vector3(-.95f,0,0),90);Chair(group,new Vector3(.95f,0,0),270);
                Disc("Seramik vazo",group,new Vector3(0,.86f,0),new Vector3(.1f,.13f,.1f),ceramic);
                Rod("Vazo çiçeği",group,new Vector3(0,.9f,0),new Vector3(.015f,1.08f,0),.007f,leaf);Shape("Küçük yaprak",PrimitiveType.Sphere,group,new Vector3(.03f,1.02f,0),new Vector3(.075f,.025f,.04f),leaf);
                Shape("Peçetelik",PrimitiveType.Cube,group,new Vector3(.3f,.835f,.08f),new Vector3(.13f,.08f,.12f),walnut);Shape("Keten peçete",PrimitiveType.Cube,group,new Vector3(.3f,.88f,.08f),new Vector3(.115f,.02f,.11f),ceramic);
            }
        }
        void Chair(Transform parent,Vector3 at,float yaw)
        {
            var chair=new GameObject("Döşemeli butik sandalye").transform;chair.SetParent(parent,false);chair.localPosition=at;chair.localRotation=Quaternion.Euler(0,yaw,0);
            Disc("Kavisli ahşap oturak",chair,new Vector3(0,.43f,0),new Vector3(.49f,.055f,.48f),walnut);Disc("Kumaş minder",chair,new Vector3(0,.47f,0),new Vector3(.46f,.065f,.45f),fabric);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.15f,.15f})Rod("Konik sandalye ayağı",chair,new Vector3(x*1.25f,.035f,z*1.25f),new Vector3(x,.42f,z),.019f,steel);
            Rod("Sol sırt desteği",chair,new Vector3(-.18f,.41f,-.18f),new Vector3(-.2f,.9f,-.22f),.018f,walnut);Rod("Sağ sırt desteği",chair,new Vector3(.18f,.41f,-.18f),new Vector3(.2f,.9f,-.22f),.018f,walnut);
            var back=Shape("Döşemeli sırtlık",PrimitiveType.Cube,chair,new Vector3(0,.81f,-.215f),new Vector3(.43f,.23f,.065f),fabric);back.transform.localRotation=Quaternion.Euler(-6,0,0);
        }
        void Bar()
        {
            foreach(var r in game.world.GetComponentsInChildren<Renderer>())
            {
                string n=r.name;if(n=="Kahve barı - ahşap gövde"||n=="Ahşap ön panel")r.sharedMaterial=walnut;else if(n=="Taş çalışma tezgâhı")r.sharedMaterial=stone;else if(n.Contains("Pirinç")||n=="Ayak dayama")r.sharedMaterial=brass;
            }
            for(int i=0;i<82;i++)Shape("Bar · ahşap yiv",PrimitiveType.Cube,scenery,new Vector3(-5.2f+i*.123f,.53f,-6.21f),new Vector3(.035f,.82f,.035f),oak);
            Shape("Tezgâh kenar çıtası",PrimitiveType.Cube,scenery,new Vector3(-.2f,1.09f,-6.32f),new Vector3(10.55f,.045f,.045f),brass);
            for(int i=0;i<5;i++)
            {Disc("Fincan altlığı",scenery,new Vector3(-1+i*.28f,1.16f,-5.45f),new Vector3(.2f,.015f,.2f),ceramic);var cup=game.world.Model("Cup_With_Coffee",new Vector3(-1+i*.28f,1.17f,-5.45f),.13f,0,false);cup.name="Sergi fincanı";}
            StationLabel("SICAK / SOĞUK BARDAK",new Vector3(-4.3f,1.04f,-6.33f));StationLabel("ESPRESSO",new Vector3(-.55f,1.04f,-6.33f));StationLabel("ŞURUP",new Vector3(1.2f,1.04f,-6.33f));StationLabel("TESLİM",new Vector3(4.6f,1.04f,-6.33f));
            StationLabel("SÜT · KÖPÜK · LATTE ART",new Vector3(-3,1.75f,4.55f),0);
        }
        void StationLabel(string text,Vector3 position,float yaw=0)
        {var plaque=Shape("İstasyon etiketi",PrimitiveType.Cube,scenery,position,new Vector3(1.25f,.14f,.025f),steel);var label=new GameObject(text).AddComponent<TextMesh>();label.transform.SetParent(plaque.transform,false);label.transform.localScale=new Vector3(1/1.25f,1/.14f,1/.025f);label.transform.position=position+Vector3.back*.02f;label.transform.rotation=Quaternion.Euler(0,yaw,0);label.text=text;label.fontSize=64;label.characterSize=.015f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.9f,.78f,.5f);}
        void Decor()
        {
            for(int i=0;i<4;i++)
            {
                float x=-7.2f+i*4.8f;Rod("Sarkıt kablosu",scenery,new Vector3(x,3.45f,-1.8f),new Vector3(x,2.7f,-1.8f),.012f,steel);Disc("Pirinç sarkıt gövdesi",scenery,new Vector3(x,2.63f,-1.8f),new Vector3(.7f,.12f,.7f),brass);Disc("Sarkıt ışık yüzeyi",scenery,new Vector3(x,2.565f,-1.8f),new Vector3(.55f,.014f,.55f),ceramic);
                var light=new GameObject("Sarkıt ışığı "+i).AddComponent<Light>();light.transform.SetParent(scenery);light.transform.position=new Vector3(x,2.48f,-1.8f);light.type=LightType.Point;light.range=6;light.intensity=.9f;light.color=new Color(1,.78f,.49f);
            }
            foreach(float x in new[]{-8.8f,8.8f})
            {Disc("Büyük seramik saksı",scenery,new Vector3(x,.27f,7.5f),new Vector3(.5f,.5f,.5f),ceramic);Rod("İç mekân bitkisi gövde",scenery,new Vector3(x,.4f,7.5f),new Vector3(x,1.65f,7.5f),.025f,walnut);for(int i=0;i<8;i++){float a=i*2.4f;var leafObj=Shape("Uzun yeşil yaprak",PrimitiveType.Sphere,scenery,new Vector3(x+Mathf.Sin(a)*.2f,.8f+i*.1f,7.5f+Mathf.Cos(a)*.2f),new Vector3(.38f,.07f,.2f),leaf);leafObj.transform.localRotation=Quaternion.Euler(12,a*Mathf.Rad2Deg,25);}}
            for(int i=0;i<3;i++)
            {var frame=Shape("Duvar sanat çerçevesi",PrimitiveType.Cube,scenery,new Vector3(-4+i*4,2.1f,8.82f),new Vector3(2.4f,1.25f,.07f),walnut);Shape("Duvar sanat zemini",PrimitiveType.Cube,scenery,new Vector3(-4+i*4,2.1f,8.77f),new Vector3(2.22f,1.08f,.025f),fabric);var caption=new GameObject("Kafe sanat yazısı").AddComponent<TextMesh>();caption.transform.SetParent(scenery);caption.transform.position=new Vector3(-4+i*4,2.1f,8.74f);caption.text=new[]{"FRESHLY\nROASTED","ARDA'S\nCOFFEE","GOOD DAYS\nSTART HERE"}[i];caption.characterSize=.055f;caption.fontSize=64;caption.anchor=TextAnchor.MiddleCenter;caption.alignment=TextAlignment.Center;caption.color=new Color(.9f,.81f,.59f);}
            Shape("Marka tabelası opak arkalık",PrimitiveType.Cube,scenery,new Vector3(0,2.95f,-11.02f),new Vector3(6,.6f,.09f),steel);
            var brand=new GameObject("İç cephe marka tabelası").AddComponent<TextMesh>();brand.transform.SetParent(scenery);brand.transform.position=new Vector3(0,2.95f,-10.82f);brand.transform.rotation=Quaternion.Euler(0,180,0);brand.text="ARDA'S COFFEE";brand.fontSize=64;brand.characterSize=.055f;brand.anchor=TextAnchor.MiddleCenter;brand.color=new Color(.9f,.78f,.5f);
            var apron=game.player.view.transform.Find("Kol / kıyafet");if(apron)apron.gameObject.SetActive(false);
        }
        void Lighting()
        {
            QualitySettings.shadowDistance=45;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowResolution=UnityEngine.ShadowResolution.Medium;QualitySettings.antiAliasing=0;
            foreach(var light in game.world.GetComponentsInChildren<Light>())if(light.type==LightType.Directional){light.shadows=LightShadows.Soft;light.shadowStrength=.65f;light.shadowBias=.035f;light.shadowNormalBias=.25f;}
            var camera=game.player.view.GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;camera.antialiasing=Application.isMobilePlatform?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.SubpixelMorphologicalAntiAliasing;camera.antialiasingQuality=AntialiasingQuality.High;
            var volume=scenery.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=profile;
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var colors=profile.Add<ColorAdjustments>(true);colors.postExposure.Override(.08f);colors.contrast.Override(6);colors.saturation.Override(-3);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.075f);bloom.threshold.Override(1.3f);var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.065f);vignette.smoothness.Override(.4f);
        }
        public void ApplyPalette()
        {
            if(!oak)return;
            var d=game.expansion.Data;Color[] palette={new Color(.67f,.62f,.51f),new Color(.42f,.55f,.48f),new Color(.62f,.43f,.32f),new Color(.47f,.49f,.6f),new Color(.81f,.77f,.66f)};wall.SetColor("_BaseColor",palette[d.wall%5]);
            foreach(var renderer in game.world.GetComponentsInChildren<Renderer>())if(renderer.name=="Arka duvar"||renderer.name=="Sol duvar"||renderer.name=="Sağ duvar")renderer.sharedMaterial=wall;
            oak.SetColor("_BaseColor",d.theme==0?new Color(.48f,.39f,.28f):d.theme==1?new Color(.38f,.32f,.2f):new Color(.63f,.5f,.32f));fabric.SetColor("_BaseColor",d.theme==0?new Color(.2f,.32f,.26f):d.theme==1?new Color(.12f,.26f,.22f):new Color(.45f,.38f,.29f));
            foreach(var renderer in game.world.GetComponentsInChildren<Renderer>())if(renderer.name=="Kahve barı - ahşap gövde")renderer.sharedMaterial=walnut;
        }
        void MergeScenery()
        {
            game.world.RemoveBackBarProps();   // birleştirmeden ÖNCE: sonra tek mesh'in içinden çıkarılamaz
            var groups=new Dictionary<Material,List<CombineInstance>>();var originals=new List<Renderer>();
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(filter.GetComponent<TextMesh>()||!renderer||!renderer.enabled||!filter.sharedMesh||!filter.sharedMesh.isReadable||renderer.sharedMaterials.Length!=1)continue;
                var material=renderer.sharedMaterial;if(!groups.TryGetValue(material,out var items)){items=new List<CombineInstance>();groups[material]=items;}
                items.Add(new CombineInstance{mesh=filter.sharedMesh,transform=scenery.worldToLocalMatrix*filter.transform.localToWorldMatrix});originals.Add(renderer);
            }
            foreach(var pair in groups){var mesh=new Mesh{name="Birleşik kafe · "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);combinedMeshes.Add(mesh);var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(scenery,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;}
            foreach(var renderer in originals)renderer.enabled=false;
        }
        public void DecorateCup(GameObject root,CupWork work,bool held)
        {
            var body=root.transform.Find("Bardak");if(!body)return;
            if(!cupMesh)
            {
                Vector2[] rings={new Vector2(0,-.14f),new Vector2(.056f,-.14f),new Vector2(.06f,-.13f),new Vector2(.078f,.14f),new Vector2(.078f,.147f),new Vector2(.071f,.147f),new Vector2(.071f,.13f),new Vector2(.053f,-.12f),new Vector2(0,-.12f)};
                var v=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();const int n=32;
                for(int j=0;j<rings.Length;j++)for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*rings[j].x,rings[j].y,Mathf.Sin(a)*rings[j].x));uv.Add(new Vector2(i/(float)n,(rings[j].y+.14f)/.287f));}
                for(int j=0;j<rings.Length-1;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+n+1;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                cupMesh=new Mesh{name="İçi boş konik kahve bardağı"};cupMesh.SetVertices(v);cupMesh.SetUVs(0,uv);cupMesh.SetTriangles(indices,0);cupMesh.RecalculateNormals();cupMesh.RecalculateBounds();
            }
            body.GetComponent<MeshFilter>().sharedMesh=cupMesh;body.localScale=Vector3.one;
            if(!work.coldCup)
            {
                body.GetComponent<Renderer>().sharedMaterial=ceramic;
                var sleeve=Shape("Keten marka kılıfı",PrimitiveType.Cylinder,root.transform,Vector3.zero,new Vector3(.145f,.037f,.145f),fabric);sleeve.layer=2;
                var logo=new GameObject("Bardak markası").AddComponent<TextMesh>();logo.transform.SetParent(root.transform,false);logo.transform.localPosition=new Vector3(0,0,-.075f);logo.text="A";logo.characterSize=.035f;logo.fontSize=64;logo.anchor=TextAnchor.MiddleCenter;logo.color=new Color(.94f,.84f,.6f);logo.gameObject.layer=2;
            }
            else if(work.lidded){Rod("İçecek pipeti",root.transform,new Vector3(.015f,.13f,0),new Vector3(.04f,.29f,0),.004f,ceramic);}
            if(work.brewed&&!work.coldCup&&!work.lidded)
            {
                var steam=new GameObject("İçecek buharı");steam.transform.SetParent(root.transform,false);steam.transform.localPosition=Vector3.up*.15f;var particles=steam.AddComponent<ParticleSystem>();var main=particles.main;main.startSpeed=.07f;main.startLifetime=.7f;main.startSize=.016f;main.startColor=new Color(1,1,1,.2f);main.maxParticles=8;var emission=particles.emission;emission.rateOverTime=4;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.025f;steam.transform.localRotation=Quaternion.Euler(-90,0,0);particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=game.world.ColorMaterial(new Color(1,1,1,.2f));steam.layer=2;
            }
            RefineCup(root,work);
            if(held){root.transform.localPosition=new Vector3(.24f,-.23f,.6f);root.transform.localRotation=Quaternion.Euler(-5,0,-8);}
        }
        void OnDestroy(){foreach(var mat in materials)if(mat)Destroy(mat);foreach(var texture in textures)if(texture)Destroy(texture);foreach(var mesh in combinedMeshes)if(mesh)Destroy(mesh);if(profile)Destroy(profile);if(bevel)Destroy(bevel);if(cupMesh)Destroy(cupMesh);}
    }
}



