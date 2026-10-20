using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CafeLife
{
    // Kafe zaman zaman tozlanır: yerdeki tozlar paspasla, masadakiler bezle silinir. Aletler kafede sabit yerlerde durur.
    public sealed class CafeDirt : MonoBehaviour
    { public bool table; public int tableIndex=-1; public float progress; public Vector3 baseScale; public GameObject marker; void OnDestroy(){ if(marker)Destroy(marker); } }

    public sealed class CafeCleaning : MonoBehaviour
    {
        public enum Tool { None, Mop, Cloth }
        public static readonly Vector3 MopSpot=new Vector3(-9.3f,0,4.9f), ClothSpot=new Vector3(-9.75f,1.12f,4.3f); // merdiven altı: kova yerde, bez rafta
        public CafeGame game; public Tool held;
        public readonly List<CafeDirt> spots=new List<CafeDirt>();
        float timer=45,swing; GameObject mopProp,clothProp,mopHand,clothHand;

        public void Setup(CafeGame owner)
        {
            game=owner; var parent=game.world.transform;
            var stand=new GameObject("Paspas kovası"); stand.transform.SetParent(parent,false); stand.transform.position=MopSpot;
            Part("Kova",PrimitiveType.Cylinder,stand.transform,new Vector3(0,.24f,0),new Vector3(.42f,.24f,.42f),new Color(.32f,.42f,.5f));
            mopProp=new GameObject("Paspas"); mopProp.transform.SetParent(stand.transform,false); mopProp.transform.localRotation=Quaternion.Euler(0,0,7);
            Part("Paspas sapı",PrimitiveType.Cylinder,mopProp.transform,new Vector3(0,.85f,0),new Vector3(.04f,.6f,.04f),new Color(.55f,.38f,.22f));
            Part("Paspas başı",PrimitiveType.Sphere,mopProp.transform,new Vector3(0,.5f,0),new Vector3(.24f,.16f,.24f),new Color(.9f,.88f,.8f));
            Trigger(stand,"cc_mop",new Vector3(0,.7f,0),.9f);
            // Etkileşim alanı bezden ayrı durur: bez ele alınınca gizlenir ama geri bırakma noktası kalır.
            var clothSpot=new GameObject("Temizlik bezi yeri"); clothSpot.transform.SetParent(parent,false); clothSpot.transform.position=ClothSpot;
            clothProp=new GameObject("Temizlik bezi"); clothProp.transform.SetParent(clothSpot.transform,false);
            Part("Bez",PrimitiveType.Cube,clothProp.transform,Vector3.zero,new Vector3(.3f,.025f,.22f),new Color(.9f,.82f,.4f));
            Trigger(clothSpot,"cc_cloth",Vector3.up*.1f,.55f);
            var view=game.player.view.transform;
            mopHand=new GameObject("Elde paspas"); mopHand.transform.SetParent(view,false); mopHand.transform.localPosition=new Vector3(.36f,-.55f,.8f);
            Part("Sap",PrimitiveType.Cylinder,mopHand.transform,new Vector3(0,.4f,0),new Vector3(.035f,.45f,.035f),new Color(.55f,.38f,.22f));
            Part("Baş",PrimitiveType.Sphere,mopHand.transform,new Vector3(0,-.05f,0),new Vector3(.22f,.12f,.2f),new Color(.9f,.88f,.8f));
            clothHand=new GameObject("Elde bez"); clothHand.transform.SetParent(view,false); clothHand.transform.localPosition=new Vector3(.24f,-.2f,.55f);
            Part("Bez",PrimitiveType.Cube,clothHand.transform,Vector3.zero,new Vector3(.2f,.02f,.15f),new Color(.9f,.82f,.4f));
            foreach(var h in new[]{mopHand,clothHand}){ foreach(var r in h.GetComponentsInChildren<Renderer>())r.gameObject.layer=2; h.SetActive(false); }
        }

        Transform Part(string n,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Color color)
        {
            var o=GameObject.CreatePrimitive(type); o.name=n; o.transform.SetParent(parent,false); o.transform.localPosition=pos; o.transform.localScale=scale;
            Destroy(o.GetComponent<Collider>()); o.GetComponent<Renderer>().sharedMaterial=game.world.ColorMaterial(color); return o.transform;
        }
        CafeTarget Trigger(GameObject root,string kind,Vector3 center,float radius)
        {
            var marker=new GameObject(root.name+" etkileşim"); marker.transform.SetParent(root.transform,false); marker.transform.localPosition=center; marker.layer=8;
            var sphere=marker.AddComponent<SphereCollider>(); sphere.isTrigger=true; sphere.radius=radius;
            var target=root.GetComponent<CafeTarget>()??root.AddComponent<CafeTarget>(); target.kind=kind; return target;
        }

        // ---------- kirlenme ----------
        void Update()
        {
            if(!game||game.data==null||game.Paused||CafeStory.Busy)return;
            swing=Mathf.Max(0,swing-Time.deltaTime);
            float wobble=Mathf.Sin(swing*38)*swing*70;
            if(mopHand)mopHand.transform.localRotation=Quaternion.Euler(-16+wobble,-12,0);
            if(clothHand)clothHand.transform.localRotation=Quaternion.Euler(4,wobble*.6f,wobble);
            timer-=Time.deltaTime; if(timer>0)return;
            timer=Random.Range(40f,75f);
            spots.RemoveAll(s=>!s);
            if(spots.Count<7)Spawn(-1);
        }

        // Gece boyunca kafe tozlanır: uyandığında yeni tozlar çıkmış olur. Dönen değer: eklenen toz sayısı.
        public int Overnight()
        {
            spots.RemoveAll(s=>!s); int added=0, floors=Random.Range(4,7), tables=Random.Range(1,3);
            for(int i=0;i<floors&&spots.Count<14;i++)if(Spawn(0))added++;
            for(int i=0;i<tables&&spots.Count<14;i++)if(Spawn(1))added++;
            if(game.expansion)game.expansion.Data.cleanliness=Mathf.Max(0,game.expansion.Data.cleanliness-added*3f);
            return added;
        }

        // kind: 0 yer tozu, 1 masa tozu, -1 rastgele
        public CafeDirt Spawn(int kind)
        {
            spots.RemoveAll(s=>!s);
            int tables=Mathf.Max(1,game.data.tables);
            bool table=kind==1||(kind<0&&Random.value<.4f);
            Vector3 pos; float radius; int index=Random.Range(0,tables);
            if(table)
            {
                for(int i=0;i<tables&&spots.Any(s=>s.table&&s.tableIndex==index);i++)index=(index+1)%tables;
                if(spots.Any(s=>s.table&&s.tableIndex==index))return null;
                pos=game.world.TableCenter(index)+new Vector3(Random.Range(-.2f,.2f),.845f,Random.Range(-.2f,.2f)); radius=.2f; // masa tablasının üstü ~.80-.84
            }
            else
            {
                float a=Random.value*Mathf.PI*2,r=Random.Range(1.3f,2.3f);
                pos=game.world.TableCenter(index)+new Vector3(Mathf.Cos(a)*r,.055f,Mathf.Sin(a)*r); // parke yüzeyi y=.033, giriş paspası ~.058
                pos.x=Mathf.Clamp(pos.x,-8.8f,8.8f); pos.z=Mathf.Clamp(pos.z,-9.6f,7f); radius=Random.Range(.45f,.7f);
            }
            var go=new GameObject(table?"Masa tozu":"Yer tozu"); go.transform.SetParent(game.world.transform,false);
            go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0,Random.value*360,0);
            BuildDust(go.transform,radius,table?.45f:1f);
            var dirt=go.AddComponent<CafeDirt>(); dirt.table=table; dirt.tableIndex=table?index:-1; dirt.baseScale=Vector3.one;
            // İşaretçi tozun çocuğu olamaz: ölçek etkileşim alanını da bozardı.
            var marker=new GameObject("Toz etkileşim"); marker.transform.SetParent(game.world.transform,false); marker.layer=8;
            marker.transform.position=pos+Vector3.up*(table?.1f:.35f);
            var sphere=marker.AddComponent<SphereCollider>(); sphere.isTrigger=true; sphere.radius=table?.45f:.8f;
            marker.AddComponent<CafeTarget>().dirt=dirt; dirt.marker=marker; spots.Add(dirt); return dirt;
        }

        // ---------- toz görünümü ----------
        // Zemin yok: yalnızca yumuşak kenarlı, saydam, siyah toz parçacıkları. Her toz farklı sayıda, boyda, yoğunlukta ve dağılımda çıkar.
        static Texture2D atlas; static Material[] dustMaterials;

        // 2x2 desenli atlas: gürültüyle bozulmuş yumuşak lekeler (alfa kanalı). Dört farklı şekil.
        static Texture2D Atlas()
        {
            if(atlas)return atlas;
            const int tile=64,size=128; atlas=new Texture2D(size,size,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear,name="Toz atlası"};
            var px=new Color32[size*size];
            for(int ty=0;ty<2;ty++)for(int tx=0;tx<2;tx++)
            {
                float seed=(ty*2+tx)*13.7f+3.1f;
                for(int y=0;y<tile;y++)for(int x=0;x<tile;x++)
                {
                    float u=(x+.5f)/tile*2-1,v=(y+.5f)/tile*2-1,d=Mathf.Sqrt(u*u+v*v);
                    float n=Mathf.PerlinNoise(x*.11f+seed,y*.11f+seed)*.6f+Mathf.PerlinNoise(x*.35f+seed*2,y*.35f+seed*2)*.4f;
                    float a=Mathf.Clamp01((1-d)*1.5f+(n-.5f)*.95f); a=a*a*(3-2*a)*Mathf.Clamp01((1-d)*5f);
                    px[(ty*tile+y)*size+tx*tile+x]=new Color32(255,255,255,(byte)(a*255));
                }
            }
            atlas.SetPixels32(px); atlas.Apply(true); return atlas;
        }

        static Material[] DustMaterials()
        {
            if(dustMaterials!=null&&dustMaterials[0])return dustMaterials;
            var baseMat=Resources.Load<Material>("CafeMaterials/CafeDust");
            if(!baseMat)
            {   // Yedek: .mat bulunamazsa aynı ayarlarla koddan kur.
                baseMat=new Material(Shader.Find("Universal Render Pipeline/Lit")); baseMat.SetFloat("_Surface",1); baseMat.SetFloat("_Blend",0);
                baseMat.SetFloat("_SrcBlend",5); baseMat.SetFloat("_DstBlend",10); baseMat.SetFloat("_ZWrite",0); baseMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                baseMat.SetOverrideTag("RenderType","Transparent"); baseMat.renderQueue=3000; baseMat.SetFloat("_Smoothness",0); baseMat.SetFloat("_Metallic",0); baseMat.SetFloat("_SpecularHighlights",0);
            }
            float[] alpha={.62f,.3f,.13f}; dustMaterials=new Material[3];
            for(int i=0;i<3;i++)
            {
                var m=new Material(baseMat){name="Toz "+i}; var c=new Color(.025f,.024f,.022f,alpha[i]);
                m.SetTexture("_BaseMap",Atlas()); m.SetTexture("_MainTex",Atlas()); m.SetColor("_BaseColor",c); m.SetColor("_Color",c); dustMaterials[i]=m;
            }
            return dustMaterials;
        }

        static void Quad(List<Vector3> v,List<Vector2> uv,List<int> t,Vector2 c,float size,float stretch,float rot,float y)
        {
            int tile=Random.Range(0,4),b=v.Count; float cs=Mathf.Cos(rot),sn=Mathf.Sin(rot);
            for(int i=0;i<4;i++)
            {
                float lx=(i==0||i==3?-.5f:.5f)*size*stretch,lz=(i<2?-.5f:.5f)*size;
                v.Add(new Vector3(c.x+lx*cs-lz*sn,y,c.y+lx*sn+lz*cs));
                uv.Add(new Vector2((tile%2)*.5f+(i==0||i==3?0f:1f)*.5f,(tile/2)*.5f+(i<2?0f:1f)*.5f));
            }
            t.AddRange(new[]{b,b+2,b+1, b,b+3,b+2, b,b+1,b+2, b,b+2,b+3});
        }

        void Level(Transform root,int level,int count,float spread,float minSize,float maxSize,float angle,float elong,float y)
        {
            var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>();
            for(int i=0;i<count;i++)
            {
                var p=Random.insideUnitCircle*spread*Mathf.Lerp(.25f,1f,Random.value); p.x*=elong;
                float ca=Mathf.Cos(angle),sa=Mathf.Sin(angle); p=new Vector2(p.x*ca-p.y*sa,p.x*sa+p.y*ca);
                Quad(v,uv,t,p,Random.Range(minSize,maxSize),Random.Range(.7f,1.8f),Random.value*6.28f,y);
            }
            var normals=new Vector3[v.Count]; for(int i=0;i<normals.Length;i++)normals[i]=Vector3.up;
            var o=new GameObject("Toz "+level); o.transform.SetParent(root,false);
            o.AddComponent<MeshFilter>().sharedMesh=new Mesh{vertices=v.ToArray(),uv=uv.ToArray(),triangles=t.ToArray(),normals=normals};
            var r=o.AddComponent<MeshRenderer>(); r.sharedMaterial=DustMaterials()[level]; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
        }

        void BuildDust(Transform root,float radius,float scale)
        {
            float density=Random.Range(.55f,1.45f),elong=Random.Range(1f,2.1f),angle=Random.value*Mathf.PI*2;
            Level(root,0,Mathf.RoundToInt(Random.Range(24,48)*density*Mathf.Lerp(.5f,1f,scale)),radius*1.1f,.03f*scale,.1f*scale,angle,elong,0f);        // ince, koyu tanecikler
            Level(root,1,Mathf.RoundToInt(Random.Range(5,11)*density*Mathf.Lerp(.5f,1f,scale)),radius,.12f*scale,.3f*scale,angle,elong,.002f);            // orta, yumuşak kümeler
            Level(root,2,Random.Range(2,4),radius*.8f,.4f*scale,.75f*scale,angle,elong,.004f);                                                           // geniş, çok hafif toz buğusu
        }

        // ---------- etkileşim ----------
        static Tool Needs(CafeDirt d)=>d.table?Tool.Cloth:Tool.Mop;
        public string DirtLabel(CafeDirt d)
        {
            if(held!=Needs(d))return d.table?"Masada toz var · bez gerekli (merdiven altındaki rafta)":"Yerde toz var · paspas gerekli (merdiven altındaki kovada)";
            return (d.table?"Masayı bezle sil":"Tozu paspasla sil")+" · %"+Mathf.RoundToInt(d.progress*100)+" [E]";
        }
        public void UseDirt(CafeDirt d)
        {
            if(held!=Needs(d)){game.Tell(d.table?"Masayı silmek için merdiven altındaki raftan bezi al.":"Yeri silmek için merdiven altındaki kovadan paspası al.");return;}
            d.progress+=.34f; swing=.4f;
            if(d.progress<.99f){d.transform.localScale=d.baseScale*(1-d.progress*.7f);return;}
            spots.Remove(d); var x=game.expansion;
            if(x){x.Data.cleanliness=Mathf.Min(100,x.Data.cleanliness+(d.table?7:9));x.Data.cleaned++;x.GetComponent<CafeHundred>()?.Clean();}
            Destroy(d.gameObject); game.Tell(d.table?"Masa tertemiz.":"Toz temizlendi.",true); game.Save();
        }
        public string ToolLabel(string kind)
        {
            var tool=kind=="cc_mop"?Tool.Mop:Tool.Cloth;
            return held==tool?(tool==Tool.Mop?"Paspası yerine bırak [E]":"Bezi yerine bırak [E]"):(tool==Tool.Mop?"Paspası al · yer tozu için [E]":"Bezi al · masa tozu için [E]");
        }
        public void UseTool(string kind)
        {
            var tool=kind=="cc_mop"?Tool.Mop:Tool.Cloth;
            held=held==tool?Tool.None:tool;
            mopHand.SetActive(held==Tool.Mop); clothHand.SetActive(held==Tool.Cloth);
            mopProp.SetActive(held!=Tool.Mop); clothProp.SetActive(held!=Tool.Cloth);
            game.Tell(held==Tool.None?"Aleti yerine bıraktın.":held==Tool.Mop?"Paspas elinde. Yerdeki tozlara bakıp E'ye bas.":"Bez elinde. Tozlu masalara bakıp E'ye bas.",true);
        }
    }
}
