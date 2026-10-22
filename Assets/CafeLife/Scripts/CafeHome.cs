using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    // Hasan Dede'nin evi: dükkânın sol yarısının üstünde. Sol duvardaki merdivenle yürüyerek çıkılır.
    // Kat planı: oturma + mutfak (açık), Hasan Dede'nin odası, bizim oda, banyo. Kalan çatı düz teras kalır.
    public sealed class CafeHome : MonoBehaviour
    {
        public const float Fy=3.7f, H=2.9f;                  // üst kat zemin yüksekliği, duvar boyu
        const float X0=-10f, X1=1.5f, Z0=-1f, Z1=9f;          // dairenin dış sınırları
        const float StairX=-9.25f, StairW=1.3f, StairZ0=.3f, StairRun=.3f; const int StairSteps=19; // merdiven: z=.3'ten 6.0'a
        const float HoleX1=-8.5f, HoleZ0=2.0f, HoleZ1=6f;     // tavan/zemin boşluğu: x -10..-8.5, z 2..6 (kafa tavana çarpmasın diye erken başlar)
        public CafeGame game; Transform room; Font font;
        public static readonly Vector3 NoteSpot=new Vector3(-.1f,Fy+.74f,2.0f); // battaniyenin üstü

        static readonly Dictionary<string,float> Sizes=new Dictionary<string,float>{
            {"1P Bed",1.30f},{"2P Bed",2.33f},{"Bed Table",.71f},{"Sofa",1.85f},{"Sofa 2",1.84f},{"Coffee Table",1.47f},{"TV 1",.91f},{"Wardrobe",1.19f},{"Dining Table",2.72f},
            {"Chair",.58f},{"Chair 2",.58f},{"Stool",.59f},{"Table (Modern)",1.56f},{"Drawer 1",1f},{"Drawer 2",1f},{"Modern Shelves",1.24f},{"Wall Shelf",1f},{"Wall Shelf 2",2f},
            {"Fridge",.93f},{"Counter (Connected)",1f},{"Counter (Connected - Sink)",1f},{"Oven",.86f},{"Microwave",.72f},{"Coffee Machine",.29f},{"Toaster",.26f},
            {"Toilet",.48f},{"Bathtub",.94f},{"Mirror",.65f},{"Towel (Wall)",.56f},{"Washing Machine",.86f},{"Trash Bin",.29f},
            {"Lamp 1",.35f},{"Lamp 2",.35f},{"Lamp 3",.15f},{"Small Lamp 1",.21f},{"Small Lamp 2",.21f},{"Ceiling Lamp 3",.34f},
            {"Painting 1",.88f},{"Painting 2",.92f},{"Painting 3",.92f},{"Painting Small 1",.25f},{"Painting Small 2",.22f},{"Books I",.27f},{"Books II",.31f},{"Vase 1",.26f},{"Small Green Deco",.16f},
            {"Laptop",.48f},{"Cup (Filled)",.17f},{"Simple Coffee",.15f},{"Box",.65f},{"Toolbox",.62f},{"Simple Pillow (Sofa)",.5f}
        };

        public void Setup(CafeGame owner)
        {
            game=owner; font=CafeTypography.Regular?CafeTypography.Regular:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            room=new GameObject("Üst kat evi").transform; room.SetParent(transform,false);
            CutCeiling(); BuildStairs(); BuildShell(); BuildLiving(); BuildHasan(); BuildOurRoom(); BuildBath(); BuildLights();
        }

        // ---------- yapı taşları ----------
        Material Mat(Color c)=>game.world.ColorMaterial(c);
        GameObject Box(string n,Vector3 pos,Vector3 size,Material m,bool collider=true)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube); o.name=n; o.transform.SetParent(room,false); o.transform.position=pos; o.transform.localScale=size;
            o.GetComponent<Renderer>().sharedMaterial=m; if(!collider)Destroy(o.GetComponent<Collider>()); return o;
        }
        void Light(Vector3 pos,Color c,float intensity,float range)
        { var l=new GameObject("Oda ışığı").AddComponent<Light>(); l.transform.SetParent(room,false); l.transform.position=pos; l.type=LightType.Point; l.color=c; l.intensity=intensity; l.range=range; }
        GameObject Put(string name,float x,float z,float yaw,float y=0,bool solid=true)
        {
            try
            {
                float width=Sizes.TryGetValue(name,out var s)?s:1f; var o=game.world.Model("VNB "+name,new Vector3(x,Fy+y,z),width,yaw,solid);
                o.transform.SetParent(room,true); return o;
            }
            catch(System.Exception ex){ Debug.LogWarning("Üst kat eşyası konamadı: "+name+" · "+ex.Message); return null; }
        }
        static float[] Op(float t0,float t1,float sill,float head,bool window)=>new[]{t0,t1,sill,head,window?1f:0f};

        // a'dan b'ye düz bir duvar; açıklıklar (pencere/kapı) uzunluk boyunca t0..t1 aralığında
        void Wall(string name,Vector2 a,Vector2 b,float thick,Color color,params float[][] ops)
        {
            bool alongX=Mathf.Approximately(a.y,b.y); Vector2 dir=(b-a).normalized; float len=(b-a).magnitude; var mat=Mat(color);
            System.Array.Sort(ops,(p,q)=>p[0].CompareTo(q[0]));
            void Seg(float t0,float t1,float y0,float y1)
            {
                if(t1-t0<.01f||y1-y0<.01f)return; var c=a+dir*((t0+t1)/2);
                Box(name,new Vector3(c.x,Fy+(y0+y1)/2,c.y),alongX?new Vector3(t1-t0,y1-y0,thick):new Vector3(thick,y1-y0,t1-t0),mat);
            }
            float pos=-thick/2;
            foreach(var o in ops)
            {
                Seg(pos,o[0],0,H); if(o[2]>0)Seg(o[0],o[1],0,o[2]); if(o[3]<H)Seg(o[0],o[1],o[3],H);
                var mid=a+dir*((o[0]+o[1])/2); float width=o[1]-o[0],height=o[3]-o[2],cy=Fy+(o[2]+o[3])/2; var wood=Mat(new Color(.25f,.16f,.1f));
                Vector3 Along(float w,float h,float d)=>alongX?new Vector3(w,h,d):new Vector3(d,h,w);
                if(o[4]>.5f)
                {
                    Box("Cam",new Vector3(mid.x,cy,mid.y),Along(width,height,.03f),Mat(new Color(.62f,.8f,.85f,.16f)),false);
                    Box("Pencere çerçevesi",new Vector3(mid.x,Fy+o[2],mid.y),Along(width+.08f,.06f,thick+.04f),wood,false); Box("Pencere çerçevesi",new Vector3(mid.x,Fy+o[3],mid.y),Along(width+.08f,.06f,thick+.04f),wood,false);
                    foreach(float side in new[]{-1f,1f}){ var p=mid+dir*(side*width/2); Box("Pencere çerçevesi",new Vector3(p.x,cy,p.y),Along(.06f,height,thick+.04f),wood,false); }
                    Box("Pencere ortası",new Vector3(mid.x,cy,mid.y),Along(.04f,height,.05f),wood,false);
                }
                else
                {
                    foreach(float side in new[]{-1f,1f}){ var p=mid+dir*(side*width/2); Box("Kapı çerçevesi",new Vector3(p.x,Fy+o[3]/2,p.y),Along(.07f,o[3],thick+.04f),wood,false); }
                    Box("Kapı çerçevesi",new Vector3(mid.x,Fy+o[3],mid.y),Along(width+.14f,.07f,thick+.04f),wood,false);
                    // aralık duran kapı kanadı: menteşe kenarında, duvara yaklaşık 75° açık; çarpışması yok
                    var hinge=mid-dir*(width/2-.03f); var perp=new Vector2(-dir.y,dir.x); var d2=(dir*Mathf.Cos(1.3f)+perp*Mathf.Sin(1.3f)).normalized; var leafMid=hinge+d2*(width*.46f);
                    var leaf=Box("Kapı kanadı",new Vector3(leafMid.x,Fy+(o[3]-.04f)/2,leafMid.y),new Vector3(width*.92f,o[3]-.06f,.045f),Mat(new Color(.62f,.46f,.3f)),false); leaf.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(-d2.y,d2.x)*Mathf.Rad2Deg,0);
                    var knob=Box("Kapı kolu",new Vector3(hinge.x+d2.x*width*.85f,Fy+1f,hinge.y+d2.y*width*.85f),Vector3.one*.06f,Mat(new Color(.85f,.7f,.3f)),false);
                }
                pos=o[1];
            }
            Seg(pos,len+thick/2,0,H);
        }

        // ---------- tavan boşluğu ve merdiven ----------
        void CutCeiling()
        {
            var tavan=game.world.transform.Find("Tavan"); if(!tavan)return;
            var mat=tavan.GetComponent<Renderer>().sharedMaterial; tavan.gameObject.SetActive(false);
            void Piece(float x0,float x1,float z0,float z1)
            {
                var o=GameObject.CreatePrimitive(PrimitiveType.Cube); o.name="Tavan (merdiven boşluklu)"; o.transform.SetParent(game.world.transform,false);
                o.transform.position=new Vector3((x0+x1)/2,3.55f,(z0+z1)/2); o.transform.localScale=new Vector3(x1-x0,.15f,z1-z0); o.GetComponent<Renderer>().sharedMaterial=mat;
            }
            Piece(-10,HoleX1,-11,HoleZ0); Piece(-10,HoleX1,HoleZ1,9); Piece(HoleX1,10,-11,9);
            // Boşluğa denk gelen küçük süsleri (avize vb.) kaldır; uzun kirişlere ve duvarlara dokunma.
            var hole=new Bounds(new Vector3((-10+HoleX1)/2,2.8f,(HoleZ0+HoleZ1)/2),new Vector3(HoleX1+10,2f,HoleZ1-HoleZ0));
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if(!r||r.transform.IsChildOf(room)||r.name.StartsWith("Tavan")||r.name.Contains("uvar")||!r.bounds.Intersects(hole))continue;
                var s=r.bounds.size; if(s.x<2.5f&&s.z<2.5f&&r.bounds.min.y>1.8f)r.gameObject.SetActive(false);
            }
        }

        void BuildStairs()
        {
            var wood=Mat(new Color(.45f,.3f,.18f)); var dark=Mat(new Color(.25f,.17f,.11f)); float rise=Fy/StairSteps;
            for(int i=0;i<StairSteps;i++)
            {
                float top=(i+1)*rise,zc=StairZ0+i*StairRun+StairRun/2;
                Box("Basamak",new Vector3(StairX,top-.035f,zc),new Vector3(StairW,.07f,StairRun+.02f),wood);
                Box("Basamak yüzü",new Vector3(StairX,top-rise/2,zc-StairRun/2+.015f),new Vector3(StairW,rise,.03f),dark,false);
            }
            var slope=new Vector3(0,Fy,StairSteps*StairRun); var rot=Quaternion.LookRotation(slope.normalized); var mid=new Vector3(StairX+StairW/2-.03f,Fy/2,StairZ0+StairSteps*StairRun/2);
            var beam=Box("Merdiven kirişi",mid+Vector3.down*.18f,new Vector3(.06f,.32f,slope.magnitude),dark,false); beam.transform.rotation=rot;
            var rail=Box("Tırabzan",mid+Vector3.up*.95f,new Vector3(.05f,.05f,slope.magnitude),dark,false); rail.transform.rotation=rot;
            for(int i=0;i<StairSteps;i+=3){ float zc=StairZ0+i*StairRun+StairRun/2; Box("Tırabzan direği",new Vector3(StairX+StairW/2-.03f,(i+1)*rise+.45f,zc),new Vector3(.04f,.9f,.04f),dark,false); }
            // üst katta boşluğun kenarları: düşmemek için korkuluk
            Railing(new Vector3(HoleX1+.02f,Fy,HoleZ0),new Vector3(HoleX1+.02f,Fy,HoleZ1-.8f));
            Railing(new Vector3(-10f,Fy,HoleZ0-.02f),new Vector3(HoleX1,Fy,HoleZ0-.02f));
            // merdiven başındaki tabela (dükkân içinden okunur)
            var sign=new GameObject("Üst kat tabelası"); sign.transform.SetParent(room,false); sign.transform.position=new Vector3(-9.9f,2.35f,StairZ0+1.2f); sign.transform.rotation=Quaternion.Euler(0,270,0);
            var t=sign.AddComponent<TextMesh>(); t.text="ÜST KAT"; t.characterSize=.07f; t.fontSize=64; t.anchor=TextAnchor.MiddleCenter; t.color=new Color(.95f,.8f,.45f);
            sign.AddComponent<CafeDepthLabel>().Setup(t,font); // duvarın/zeminin arkasından görünmesin
        }

        // Üst katta merdiven boşluğunun kenarı: ince direkler, iki tutamak ve görünmez çarpışma duvarı
        void Railing(Vector3 a,Vector3 b)
        {
            var wood=Mat(new Color(.25f,.17f,.11f)); var d=b-a; float len=d.magnitude; var dir=d/len; int posts=Mathf.Max(1,Mathf.CeilToInt(len/.8f)); bool alongX=Mathf.Abs(dir.x)>.5f;
            for(int i=0;i<=posts;i++)Box("Korkuluk direği",a+dir*(len*i/posts)+Vector3.up*.5f,new Vector3(.06f,1f,.06f),wood,false);
            var mid=(a+b)/2; var size=alongX?new Vector3(len,.05f,.05f):new Vector3(.05f,.05f,len);
            Box("Korkuluk üst",mid+Vector3.up*1f,size,wood,false); Box("Korkuluk orta",mid+Vector3.up*.5f,size,wood,false);
            var wall=Box("Korkuluk çarpışma",mid+Vector3.up*.5f,alongX?new Vector3(len,1f,.06f):new Vector3(.06f,1f,len),wood); wall.GetComponent<Renderer>().enabled=false;
        }

        // ---------- kabuk: zemin, duvarlar, tavan, çatı ----------
        void BuildShell()
        {
            var wood=Mat(new Color(.5f,.35f,.22f)); var tex=Resources.Load<Texture2D>("CafeMaterials/WoodFloorColor");
            if(tex){ wood=new Material(wood); wood.SetTexture("_BaseMap",tex); wood.mainTexture=tex; wood.SetTextureScale("_BaseMap",new Vector2(6,6)); wood.color=Color.white; }
            void Slab(float x0,float x1,float z0,float z1,Material m)=>Box("Üst kat zemini",new Vector3((x0+x1)/2,Fy-.1f,(z0+z1)/2),new Vector3(x1-x0,.2f,z1-z0),m);
            Slab(X0,HoleX1,Z0,HoleZ0,wood); Slab(X0,HoleX1,HoleZ1,Z1,wood); Slab(HoleX1,X1,Z0,Z1,wood);
            Box("Banyo fayansı",new Vector3(-1f,Fy+.006f,4f),new Vector3(5f,.012f,2.4f),Mat(new Color(.82f,.86f,.86f)),false);
            Box("Üst kat tavanı",new Vector3((X0+X1)/2,Fy+H+.1f,(Z0+Z1)/2),new Vector3(X1-X0+.4f,.2f,Z1-Z0+.4f),Mat(new Color(.9f,.87f,.8f)));
            var outer=new Color(.84f,.8f,.68f); var inner=new Color(.88f,.85f,.78f); const float T=.2f,P=.15f;
            // dış duvarlar
            Wall("Batı duvarı",new Vector2(X0,Z0),new Vector2(X0,Z1),T,outer,Op(1.3f,2.8f,.9f,2.1f,true)); // t = duvarın başından uzaklık (z+1)
            Wall("Kuzey duvarı",new Vector2(X0,Z1),new Vector2(X1,Z1),T,outer,Op(2.4f,3.6f,1.25f,2.2f,true));
            Wall("Doğu duvarı",new Vector2(X1,Z0),new Vector2(X1,Z1),T,outer,Op(.6f,2.4f,.9f,2.1f,true),Op(7.2f,9f,.9f,2.1f,true));
            Wall("Güney duvarı",new Vector2(X0,Z0),new Vector2(X1,Z0),T,outer);
            // iç duvarlar (bölme)
            Wall("Bölme duvarı",new Vector2(-3.5f,Z0),new Vector2(-3.5f,Z1),P,inner,Op(1.4f,2.4f,0,2.1f,false),Op(4.4f,5.4f,0,2.1f,false),Op(7.6f,8.6f,0,2.1f,false));
            Wall("Banyo kuzey duvarı",new Vector2(-3.5f,5.2f),new Vector2(X1,5.2f),P,inner);
            Wall("Banyo güney duvarı",new Vector2(-3.5f,2.8f),new Vector2(X1,2.8f),P,inner);
        }

        void BuildLights()
        {
            var warm=new Color(1f,.86f,.66f);
            Light(new Vector3(-6.2f,Fy+2.5f,1.5f),warm,1.5f,8); Light(new Vector3(-6.4f,Fy+2.5f,7.2f),warm,1.5f,8);
            Light(new Vector3(-1f,Fy+2.5f,7f),warm,1.3f,7); Light(new Vector3(-1f,Fy+2.5f,1f),warm,1.3f,7); Light(new Vector3(-1f,Fy+2.5f,4f),new Color(.95f,.97f,1f),1.2f,5);
        }

        // ---------- odalar ----------
        void BuildLiving()
        {
            // TV köşesi (güney duvarı) ve kanepe
            Put("Table (Modern)",-5.9f,-.62f,0); Put("TV 1",-5.9f,-.62f,0,.51f,false); Put("Sofa",-5.9f,2.1f,180); Put("Coffee Table",-5.9f,.85f,0);
            Put("Lamp 1",-8.1f,-.55f,0,0,false); Put("Modern Shelves",-3.75f,2.45f,90); Put("Books I",-3.75f,2.2f,90,.9f,false);
            // mutfak (kuzey duvarı) ve yemek masası
            Put("Fridge",-9.45f,8.4f,90);
            Put("Counter (Connected)",-8.05f,8.55f,180); Put("Counter (Connected - Sink)",-7f,8.55f,180); Put("Counter (Connected)",-6f,8.55f,180);
            Put("Oven",-5.07f,8.55f,180); Put("Counter (Connected)",-4.2f,8.55f,180);
            Put("Coffee Machine",-8.05f,8.55f,180,.95f,false); Put("Microwave",-6f,8.55f,180,.95f,false); Put("Toaster",-4.2f,8.55f,180,.95f,false);
        }

        void BuildHasan()
        {
            Put("2P Bed",-.9f,7.65f,0); Put("Bed Table",-2.65f,8.5f,0); Put("Bed Table",.95f,8.5f,0);
            Put("Small Lamp 1",-2.65f,8.5f,0,.85f,false); Put("Books II",.95f,8.5f,0,.85f,false);
            Put("Wardrobe",-1f,5.55f,0); Put("Drawer 2",.95f,5.6f,0); Put("Chair 2",.9f,6.7f,270);
            Put("Lamp 3",-3.0f,5.5f,0,0,false);
            Trigger("Defter etkileşim",new Vector3(.95f,Fy+.75f,5.3f),new Vector3(1f,.7f,.9f),"cc_book");
            Trigger("Borç defteri etkileşim",new Vector3(.95f,Fy+.25f,5.3f),new Vector3(1f,.45f,.9f),"cc_ledger");
        }

        void BuildOurRoom()
        {
            // Hasan Dede'nin bize hazırladığı oda: yatak, çalışma köşesi, TV; yatağın üstünde dedenin notu
            Put("1P Bed",-.5f,2.0f,90); Put("Bed Table",1.15f,2.2f,0); Put("Small Lamp 2",1.15f,2.2f,0,.85f,false);
            Put("Wardrobe",-2.6f,-.55f,0); Put("Table (Modern)",1.1f,.2f,90); Put("Laptop",1.1f,.2f,90,.51f,false); Put("Chair 2",.4f,.25f,270);
            Put("Table (Modern)",-.3f,-.65f,0); Put("TV 1",-.3f,-.65f,0,.51f,false); Put("Simple Pillow (Sofa)",.5f,2.0f,0,.55f,false);
            var paper=Box("Hasan Dede'nin notu",NoteSpot,new Vector3(.38f,.012f,.27f),Mat(new Color(.98f,.95f,.85f)),false); paper.transform.rotation=Quaternion.Euler(0,20,0);
            // İki alan çakışmasın: nota bakınca not, yatağın baş tarafına bakınca uyku çıksın
            // Nermin Nine'nin izleri: tarak (yatakta), fotoğraf (komodinde), kuru papatyalar (masada)
            var comb=Box("Eski tarak",new Vector3(.45f,Fy+.585f,2.5f),new Vector3(.04f,.012f,.16f),Mat(new Color(.55f,.5f,.42f)),false); comb.transform.rotation=Quaternion.Euler(0,25,0);
            var frame=Box("Eski fotoğraf çerçevesi",new Vector3(1.15f,Fy+.96f,2.2f),new Vector3(.02f,.2f,.15f),Mat(new Color(.32f,.22f,.14f)),false); frame.transform.rotation=Quaternion.Euler(0,-10,6); Box("Eski fotoğraf",new Vector3(1.145f,Fy+.96f,2.2f),new Vector3(.006f,.15f,.11f),Mat(new Color(.78f,.72f,.6f)),false).transform.rotation=Quaternion.Euler(0,-10,6);
            Box("Papatya vazosu",new Vector3(1.1f,Fy+.58f,.8f),new Vector3(.08f,.12f,.08f),Mat(new Color(.7f,.75f,.72f)),false); Box("Kuru papatyalar",new Vector3(1.1f,Fy+.7f,.8f),new Vector3(.12f,.12f,.12f),Mat(new Color(.78f,.7f,.52f)),false);
            Trigger("Tarak etkileşim",new Vector3(.45f,Fy+.6f,2.5f),new Vector3(.5f,.4f,.5f),"cc_nermin0");
            Trigger("Fotoğraf etkileşim",new Vector3(1.15f,Fy+.95f,2.2f),new Vector3(.5f,.5f,.5f),"cc_nermin1");
            Trigger("Papatya etkileşim",new Vector3(1.1f,Fy+.65f,.8f),new Vector3(.5f,.5f,.5f),"cc_nermin2");
            Trigger("Not etkileşim",NoteSpot,new Vector3(.8f,.5f,.7f),"cc_note");
            Trigger("Yatak etkileşim",new Vector3(-1.3f,Fy+.6f,2.0f),new Vector3(1.2f,1.2f,1.5f),"cc_bed");
            Trigger("Televizyon etkileşim",new Vector3(-.3f,Fy+1f,-.2f),new Vector3(1.6f,1.6f,1.2f),"cc_tv");
        }

        void BuildBath()
        {
            Put("Bathtub",.4f,3.35f,90); Put("Toilet",-2.6f,4.8f,180); Put("Sink",-1.3f,4.8f,180); Put("Mirror",-1.3f,5.12f,180,1.4f,false);
            Put("Washing Machine",.05f,4.8f,180); Put("Towel (Wall)",1.2f,5.12f,180,1.2f,false); Put("Trash Bin",-2.0f,4.85f,0,0,false);
        }

        void Trigger(string n,Vector3 pos,Vector3 size,string kind)
        {
            var o=new GameObject(n); o.transform.SetParent(room,false); o.transform.position=pos; o.layer=8;
            var c=o.AddComponent<BoxCollider>(); c.isTrigger=true; c.size=size; o.AddComponent<CafeTarget>().kind=kind;
        }

        // ---------- etkileşim ----------
        public string Label(string kind)
        {
            switch(kind)
            {
                case "cc_tv": return "Televizyon · uyuyunca akşam haberleri açılıyor";
                case "cc_bed": return game.story.BedLabel();
                case "cc_nermin0": return game.story.NerminLabel(0);
                case "cc_nermin1": return game.story.NerminLabel(1);
                case "cc_nermin2": return game.story.NerminLabel(2);
                case "cc_ledger": return game.data.day>=6?(game.data.ledgerRead?"Borç Defteri · fincan sayaçları [E]":"Çekmecenin altında ikinci bir bölme · aç [E]"):"Alt çekmece kilitli";
                case "cc_book": return game.data.bookRead?"Hasan'ın defteri · tekrar oku [E]":"Altıncı çekmece · aç [E]";
                case "cc_note": return game.data.noteRead?"Hasan Dede'nin notu · tekrar oku [E]":"Yatağın üstünde bir not var · oku [E]";
            }
            return "";
        }
        public void Use(string kind)
        {
            switch(kind)
            {
                case "cc_tv": game.Tell("Ekranda karıncalar var. Uyuyunca haberler kendiliğinden açılıyor."); break;
                case "cc_bed": game.story.UseBed(); break;
                case "cc_note": game.story.ReadNote(); break;
                case "cc_book": game.story.ReadBook(); break;
                case "cc_nermin0": game.story.ReadNermin(0); break;
                case "cc_nermin1": game.story.ReadNermin(1); break;
                case "cc_nermin2": game.story.ReadNermin(2); break;
                case "cc_ledger": if(game.data.day>=6)game.story.ReadLedger(); else game.Tell("Alt çekmece kilitli."); break;
            }
        }
    }
}
