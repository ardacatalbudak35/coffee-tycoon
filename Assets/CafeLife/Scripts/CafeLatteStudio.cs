using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeLattePour {
  public static readonly Vector2 PourCenter=new Vector2(340,362);
  public const float PourRadius=205;
  public static readonly Rect[] StudioButtons={new Rect(932,30,148,52),new Rect(650,505,390,64),new Rect(650,582,190,54),new Rect(850,582,190,54)};
  public static Rect StudioLayout(Rect safe,float screenHeight){float scale=Mathf.Min(safe.width/1120f,safe.height/680f);return new Rect(safe.x+(safe.width-1120*scale)/2,screenHeight-safe.yMax+(safe.height-680*scale)/2,1120*scale,680*scale);}
  static readonly Color Gold=new Color(.82f,.66f,.39f),Ink=new Color(.075f,.085f,.10f),Cream=new Color(.94f,.91f,.84f),Muted=new Color(.61f,.64f,.65f);
  void Box(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
  void Words(Rect r,string text,int size,Color color,FontStyle weight=FontStyle.Normal){var style=new GUIStyle(GUI.skin.label){font=weight==FontStyle.Bold?CafeTypography.Strong:CafeTypography.Regular,fontSize=size,fontStyle=FontStyle.Normal,wordWrap=true,alignment=TextAnchor.MiddleLeft};style.normal.textColor=color;GUI.Label(r,text,style);}
  bool Action(Rect r,string text,bool primary=false){Box(r,primary?Gold:new Color(.14f,.16f,.18f));var style=new GUIStyle(GUI.skin.label){font=CafeTypography.Strong,fontSize=20,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter};style.normal.textColor=primary?Ink:Cream;return GUI.Button(r,text,style);}
  void Disc(Vector2 c,float r,Color color){GUI.color=color;GUI.DrawTexture(new Rect(c.x-r,c.y-r,r*2,r*2),disc);GUI.color=Color.white;}
  void DrawStudio(){GUI.depth=-100;var previousMatrix=GUI.matrix;var previousColor=GUI.color;Box(new Rect(0,0,Screen.width,Screen.height),new Color(.035f,.042f,.05f));var layout=StudioLayout(Screen.safeArea,Screen.height);GUI.matrix=Matrix4x4.TRS(new Vector3(layout.x,layout.y,0),Quaternion.identity,Vector3.one*(layout.width/1120f));
   if(!disc){disc=new Texture2D(512,512,TextureFormat.RGBA32,false);disc.filterMode=FilterMode.Bilinear;disc.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<512;y++)for(int x=0;x<512;x++)disc.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(255-Vector2.Distance(new Vector2(x,y),new Vector2(255.5f,255.5f)))));disc.Apply();}
   Box(new Rect(0,0,1120,680),Ink);Box(new Rect(38,111,1042,1),new Color(.28f,.27f,.23f));Words(new Rect(42,23,620,25),"BARİSTA ATÖLYESİ",16,Gold,FontStyle.Bold);Words(new Rect(40,48,700,53),"İmzanı kahveye bırak",34,Cream,FontStyle.Bold);
   Disc(PourCenter+new Vector2(0,8),PourRadius+20,new Color(.025f,.03f,.035f));Disc(PourCenter,PourRadius+12,Gold);Disc(PourCenter,PourRadius+9,Cream);Disc(PourCenter,PourRadius,new Color(.30f,.15f,.065f));Disc(PourCenter,PourRadius-8,new Color(.38f,.21f,.10f));
   for(int i=0;i<cup.artPoints.Count;i++){var p=cup.artPoints[i];float w=(i<cup.artWidths.Count?cup.artWidths[i]:.006f)*PourRadius/.065f;Disc(PourCenter+p*PourRadius,w/2,new Color(1,.95f,.80f));}
   Words(new Rect(120,604,440,40),"Basılı tut · yavaşça desen çiz",19,Muted);
   Words(new Rect(650,142,390,34),"SÜT DÖKÜMÜ",17,Gold,FontStyle.Bold);Words(new Rect(650,184,390,74),height<.55f?"Yakından dök,\nbeyaz desenini çiz.":"Yüksekten dök,\nsütü kahveyle karıştır.",25,Cream,FontStyle.Bold);
   Words(new Rect(650,276,390,30),"Süt  "+milk.ToString("0")+" / 160 ml",21,Cream);Box(new Rect(650,321,390,7),new Color(.19f,.21f,.22f));Box(new Rect(650,321,390*Mathf.Clamp01(milk/160),7),Gold);
   Words(new Rect(650,349,390,32),"Dökülen  "+spill.ToString("0")+" ml",19,spill>=25?new Color(.94f,.48f,.37f):Muted);Words(new Rect(650,395,390,70),milk<35?"En az 35 ml sütle desenini tamamla.":spill>=25?"Fazla süt taştı. Yeniden deneyebilirsin.":"Desenin hazır olduğunda servise geç.",19,Muted);
   bool cancel=Action(StudioButtons[0],"İptal / Esc"),complete=Action(StudioButtons[1],"Servise hazır",true),retry=Action(StudioButtons[2],"Yeniden dene"),toggle=Action(StudioButtons[3],height<.55f?"Yakın döküm":"Yüksek döküm");GUI.matrix=previousMatrix;GUI.color=previousColor;
   if(cancel)Cancel();else if(complete)Complete();else if(retry){milk=spill=0;cup.artPoints.Clear();cup.artWidths.Clear();}else if(toggle)height=height<.55f?.8f:.2f;
  }
 }
}
