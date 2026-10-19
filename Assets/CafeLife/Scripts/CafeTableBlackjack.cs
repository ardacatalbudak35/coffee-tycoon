using UnityEngine;
namespace CafeLife {
 public sealed class CafeTableBlackjack:MonoBehaviour {
  public bool Opened {get;private set;} public Vector3 Center {get;private set;} GameObject layout;CafeGame game;CafeCasino casino;Vector3 returnPosition;Quaternion returnRotation;float returnPitch,returnFov;
  public void Enter(CafeGame owner){game=owner;casino=owner.GetComponent<CafeCasino>();if(!casino)casino=owner.gameObject.AddComponent<CafeCasino>();if(!Opened)Center=new Vector3(Mathf.Abs(owner.player.transform.position.x-8)<Mathf.Abs(owner.player.transform.position.x-15)?8:15,5.23f,-33);if(!Opened){returnPosition=owner.player.transform.position;returnRotation=owner.player.transform.rotation;returnPitch=owner.player.pitch;returnFov=owner.player.view.fieldOfView;}Opened=true;owner.Continue();owner.player.body.enabled=false;owner.player.transform.position=new Vector3(Center.x,4.65f,Center.z+2.15f);owner.player.transform.rotation=Quaternion.Euler(0,180,0);owner.player.body.enabled=true;owner.player.pitch=32;owner.player.view.transform.localRotation=Quaternion.Euler(32,0,0);owner.player.view.fieldOfView=52;Refresh();owner.Tell("Masadaki fiş veya karta bak · E / ETKİLEŞ");}
  public bool CanUse=>Opened&&game&&!game.player.WorldInputBlocked&&Mathf.Abs(game.player.transform.position.y-4.2f)<1.2f&&Vector3.Distance(game.player.transform.position,Center)<5;
  public void Action(string action){if(!CanUse)return;if(action=="hit")casino.Hit();else if(action=="stand")casino.Stand();else if(action=="leave"){if(casino.Active)casino.Stand();Opened=false;game.player.body.enabled=false;game.player.transform.position=returnPosition;game.player.transform.rotation=returnRotation;game.player.body.enabled=true;game.player.pitch=returnPitch;game.player.view.transform.localRotation=Quaternion.Euler(returnPitch,0,0);game.player.view.fieldOfView=returnFov;if(layout)Destroy(layout);game.Tell("Masadan ayrıldın.");}else if(int.TryParse(action,out int bet))casino.StartHand(bet);}
  readonly System.Collections.Generic.List<Material> finishes=new System.Collections.Generic.List<Material>();
  readonly System.Collections.Generic.List<Mesh> meshes=new System.Collections.Generic.List<Mesh>();
  readonly Mesh[] suits=new Mesh[4];Mesh cardShape,feltShape;Texture2D feltGrain;
  Material paper,redInk,blackInk,backInk,actionInk,brassInk,felt;
  void PrepareVisuals(){if(paper)return;
   paper=Finish("İskambil mat kâğıt",new Color(.96f,.95f,.90f));
   redInk=Finish("İskambil kırmızı mürekkep",new Color(.57f,.075f,.085f),true);
   blackInk=Finish("İskambil siyah mürekkep",new Color(.055f,.07f,.065f),true);
   backInk=Finish("İskambil arka baskı",new Color(.14f,.20f,.23f));
   actionInk=Finish("Masa ince eylem plakası",new Color(.075f,.135f,.12f));
   brassInk=Finish("Masa saten pirinç çizgisi",new Color(.47f,.40f,.25f));
   felt=Finish("Dokulu casino keçesi",new Color(.055f,.19f,.145f));
   feltGrain=new Texture2D(128,128,TextureFormat.RGB24,true){name="İnce keçe lifleri",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
   var colors=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float shade=.92f+Mathf.PerlinNoise(x*.63f,y*.63f)*.08f;colors[y*128+x]=new Color(shade,shade,shade);}
   feltGrain.SetPixels(colors);feltGrain.Apply(true,true);felt.SetTexture("_BaseMap",feltGrain);felt.SetTextureScale("_BaseMap",new Vector2(8,5));
   cardShape=RoundedCard();feltShape=Ellipse();for(int i=0;i<4;i++)suits[i]=SuitShape(i);
  }
  Material Finish(string title,Color color,bool unlit=false){
   // A Resources material keeps the ink shader in standalone builds; Shader.Find alone can be stripped.
   var template=unlit?Resources.Load<Material>("CafeMaterials/CardInk"):null;
   var material=template&&template.shader&&template.shader.isSupported?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));
   material.name=title;material.SetColor("_BaseColor",color);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.14f);finishes.Add(material);return material;
  }
  GameObject Visual(string title,Mesh mesh,Material material,Vector3 position,Vector3 scale){var part=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(layout.transform,false);part.transform.localPosition=position;part.transform.localScale=scale;part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<Renderer>().sharedMaterial=material;return part;}
  public void Refresh(){if(!Opened||!game)return;PrepareVisuals();if(layout){layout.SetActive(false);Destroy(layout);}layout=new GameObject("Masada blackjack canlı kartlar");layout.transform.SetParent(game.world.transform,false);layout.transform.position=Center;
   foreach(var item in game.world.GetComponentsInChildren<Transform>(true))if((item.name=="Casino kapalı iskambil"||item.name=="Casino kart arka baskı"||item.name=="Casino ince fiş")&&Mathf.Abs(item.position.x-Center.x)<2.2f&&Mathf.Abs(item.position.z-Center.z)<2)item.gameObject.SetActive(false);
   Visual("Canlı masa dokulu keçe",feltShape,felt,new Vector3(0,-.001f,0),new Vector3(3.35f,1,2.08f));
   Print("KRUPİYE  ·  "+(casino.Active?"?":CafeCasino.Score(casino.dealer).ToString()),new Vector3(0,.006f,-.84f),new Vector2(1.4f,.065f),new Color(.76f,.73f,.59f));
   Cards(casino.dealer,-.42f,true);Cards(casino.player,.20f,false);
   Print(casino.Active?"ELİN  ·  "+CafeCasino.Score(casino.player):casino.Result,new Vector3(0,.006f,.54f),new Vector2(2.25f,.075f),new Color(.91f,.90f,.82f));
   if(casino.Active){Button("Kart çek","hit",-.65f);Button("Dur","stand",0);}else{Button("10 coin","10",-.95f);Button("50 coin","50",-.35f);Button("200 coin","200",.25f);}Button("Ayrıl","leave",.95f);
  }
  void Cards(System.Collections.Generic.List<int> cards,float z,bool dealer){
   for(int i=0;i<cards.Count;i++){
    bool hidden=dealer&&casino.Active&&i>0;
    float x=(i-(cards.Count-1)*.5f)*Mathf.Min(.38f,2.4f/Mathf.Max(1,cards.Count));
    Visual(hidden?"Kapalı krupiye kartı":"Açık blackjack kartı",cardShape,paper,new Vector3(x,.002f,z),Vector3.one);
    if(hidden){
     Visual("İskambil çift bordürlü arka baskı",cardShape,backInk,new Vector3(x,.004f,z),new Vector3(.9f,.12f,.93f));
     for(int row=-2;row<=2;row++)for(int col=-1;col<=1;col++)Pip(2,x+col*.075f,z+row*.065f,.026f,brassInk);
     continue;
    }
    int id=cards[i],rank=id%13+1,suit=id/13;Material ink=suit==1||suit==2?redInk:blackInk;
    Color color=suit==1||suit==2?new Color(.57f,.075f,.085f):new Color(.055f,.07f,.065f);
    string face=rank==1?"A":rank==11?"J":rank==12?"Q":rank==13?"K":rank.ToString();
    Print(face,new Vector3(x+.112f,.006f,z-.145f),new Vector2(.053f,.055f),color);
    Pip(suit,x+.112f,z-.092f,.039f,ink);
    Print(face,new Vector3(x-.112f,.006f,z+.145f),new Vector2(.053f,.055f),color,true);
    Pip(suit,x-.112f,z+.092f,.039f,ink,true);
    if(rank==1)Pip(suit,x,z,.13f,ink);
    else if(rank>10){
     Visual("Resimli kart ince madalyon",cardShape,backInk,new Vector3(x,.0043f,z),new Vector3(.48f,.12f,.60f));
     Visual("Resimli kart kâğıt iç alan",cardShape,paper,new Vector3(x,.0048f,z),new Vector3(.44f,.08f,.57f));
     Print(face,new Vector3(x,.006f,z-.018f),new Vector2(.09f,.105f),color);Pip(suit,x,z+.085f,.058f,ink);
    }else{
     // Traditional pip counts remain readable without giant suit-name text.
     if(rank<=3){for(int n=0;n<rank;n++)Pip(suit,x,z+(n-(rank-1)*.5f)*.12f,.06f,ink,n>=rank/2&&n>0);}
     else{
      int pairs=rank/2;float span=rank>=8?.27f:.24f;
      for(int n=0;n<pairs;n++){float dz=(n/(float)(pairs-1)-.5f)*span;Pip(suit,x-.058f,z+dz,.052f,ink,dz>0);Pip(suit,x+.058f,z+dz,.052f,ink,dz>0);}
      if(rank%2==1)Pip(suit,x,z,.052f,ink);
     }
    }
   }
  }
  void Pip(int suit,float x,float z,float size,Material ink,bool inverted=false){var pip=Visual("İskambil takım simgesi",suits[suit],ink,new Vector3(x,.0065f,z),new Vector3(size,1,size));if(inverted)pip.transform.localRotation=Quaternion.Euler(0,180,0);}
  void Button(string label,string action,float x){
   Visual("Masa eylemi · "+label,cardShape,brassInk,new Vector3(x,.003f,.88f),new Vector3(.46f/.32f,1,.18f/.44f));
   Visual("İnce eylem plakası yüzeyi",cardShape,actionInk,new Vector3(x,.005f,.88f),new Vector3(.45f/.32f,.3f,.17f/.44f));
   // Preserve the existing generous interaction volume, independent of the visual plate.
   var hit=new GameObject("Masa dokunma alanı");hit.transform.SetParent(layout.transform,false);hit.transform.localPosition=new Vector3(x,.08f,.88f);hit.layer=8;var box=hit.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(.54f,.18f,.3f);hit.AddComponent<CafeTarget>().kind="blackjack_"+action;
   Print(label,new Vector3(x,.007f,.88f),new Vector2(.37f,.065f),new Color(.91f,.88f,.74f));
  }
  void Print(string value,Vector3 at,Vector2 area,Color color,bool inverted=false){var text=new GameObject("Masaya basılı yazı").AddComponent<TextMesh>();text.transform.SetParent(layout.transform,false);text.transform.localPosition=at;text.transform.localRotation=Quaternion.Euler(90,inverted?0:180,0);text.text=value;text.fontSize=64;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;text.gameObject.AddComponent<CafeDepthLabel>().Setup(text,CafeTypography.Strong);text.gameObject.AddComponent<CafePrintedLabel>().area=area;}
  Mesh Ellipse(){const int count=96;var v=new Vector3[count+1];var uv=new Vector2[count+1];var triangles=new int[count*3];uv[0]=new Vector2(.5f,.5f);for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;v[i+1]=new Vector3(Mathf.Cos(a)*.5f,0,-Mathf.Sin(a)*.5f);uv[i+1]=new Vector2(v[i+1].x+.5f,v[i+1].z+.5f);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%count+1;}var mesh=new Mesh{name="İnce oval masa keçesi",vertices=v,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;}
  Mesh RoundedCard(){
   var edge=new System.Collections.Generic.List<Vector2>();const float radius=.012f;
   for(int corner=0;corner<4;corner++){float cx=corner==0||corner==3?.16f-radius:-.16f+radius,cy=corner<2?.22f-radius:-.22f+radius;for(int i=0;i<=6;i++){float a=(corner*90+i*15)*Mathf.Deg2Rad;edge.Add(new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius));}}
   var v=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();int count=edge.Count;
   for(int side=0;side<2;side++){float y=side==0?.0015f:-.0015f;int start=v.Count;v.Add(new Vector3(0,y,0));foreach(var point in edge)v.Add(new Vector3(point.x,y,-point.y));for(int i=0;i<count;i++){int a=start+i+1,b=start+(i+1)%count+1;triangles.AddRange(side==0?new[]{start,a,b}:new[]{start,b,a});}}
   for(int i=0;i<count;i++){var a=edge[i];var b=edge[(i+1)%count];int start=v.Count;v.Add(new Vector3(a.x,-.0015f,-a.y));v.Add(new Vector3(a.x,.0015f,-a.y));v.Add(new Vector3(b.x,.0015f,-b.y));v.Add(new Vector3(b.x,-.0015f,-b.y));triangles.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});}
   var mesh=new Mesh{name="İnce yuvarlak köşeli iskambil"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
  }
  Mesh SuitShape(int suit){
   var points=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
   if(suit==2)Polygon(new[]{new Vector2(0,.5f),new Vector2(-.36f,0),new Vector2(0,-.5f),new Vector2(.36f,0)},points,indices);
   else if(suit==3){Circle(new Vector2(0,.23f),.25f,points,indices);Circle(new Vector2(-.23f,-.08f),.25f,points,indices);Circle(new Vector2(.23f,-.08f),.25f,points,indices);Stem(points,indices);}
   else{var heart=new Vector2[48];for(int i=0;i<heart.Length;i++){float t=i*Mathf.PI*2/heart.Length;float x=16*Mathf.Pow(Mathf.Sin(t),3)/32,y=(13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t))/32;heart[i]=new Vector2(x,suit==0?-y*.78f+.08f:y);}
    Polygon(heart,points,indices);if(suit==0)Stem(points,indices);
   }
   var mesh=new Mesh{name="İskambil simgesi "+suit};mesh.SetVertices(points);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
  }
  void Stem(System.Collections.Generic.List<Vector3> points,System.Collections.Generic.List<int> indices){Polygon(new[]{new Vector2(-.05f,-.12f),new Vector2(-.15f,-.48f),new Vector2(.15f,-.48f),new Vector2(.05f,-.12f)},points,indices);}
  void Circle(Vector2 center,float radius,System.Collections.Generic.List<Vector3> points,System.Collections.Generic.List<int> indices){var ring=new Vector2[24];for(int i=0;i<ring.Length;i++){float t=i*Mathf.PI*2/ring.Length;ring[i]=center+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius;}Polygon(ring,points,indices);}
  void Polygon(Vector2[] ring,System.Collections.Generic.List<Vector3> points,System.Collections.Generic.List<int> indices){Vector2 center=Vector2.zero;float area=0;for(int i=0;i<ring.Length;i++){center+=ring[i];var next=ring[(i+1)%ring.Length];area+=ring[i].x*next.y-next.x*ring[i].y;}center/=ring.Length;int start=points.Count;points.Add(new Vector3(center.x,0,-center.y));foreach(var p in ring)points.Add(new Vector3(p.x,0,-p.y));for(int i=0;i<ring.Length;i++){int a=start+i+1,b=start+(i+1)%ring.Length+1;indices.AddRange(area>=0?new[]{start,a,b}:new[]{start,b,a});}}
  void OnDestroy(){if(layout)Destroy(layout);foreach(var mesh in meshes)if(mesh)Destroy(mesh);foreach(var material in finishes)if(material)Destroy(material);if(feltGrain)Destroy(feltGrain);}

 }
}
