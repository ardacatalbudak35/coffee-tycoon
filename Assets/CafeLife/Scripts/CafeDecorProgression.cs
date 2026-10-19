using System.Collections.Generic;
using UnityEngine;
namespace CafeLife {
 public sealed class CafeDecorProgression:MonoBehaviour {
  public static readonly string[] Names={"Mahalle","Botanik","Atölye","İmza"};
  public static readonly int[] Levels={1,3,6,10};
  public static readonly string[] Details={"Mevcut sıcak ahşap ve zeytin döşeme.","Adaçayı duvarlar, açık meşe ve canlı bitkiler.","Kiremit döşeme, koyu ahşap ve duvar lamelleri.","Gece mavisi, ceviz, pirinç ve duvar aydınlatmaları."};
  static readonly Color[][] palettes={
   new[]{C("C6B698"),C("7A522E"),C("381F12"),C("335242"),C("2E3330"),C("A67030")},
   new[]{C("9CAFA1"),C("B89667"),C("67513A"),C("456751"),C("D1C8B4"),C("94754D")},
   new[]{C("C4B7A1"),C("826647"),C("483328"),C("A55E43"),C("4A4B46"),C("75664F")},
   new[]{C("B9B1A0"),C("69503C"),C("392A23"),C("253E52"),C("303C41"),C("B39356")}
  };
  sealed class Finish {public Material material;public Color original;public int channel;}
  readonly List<Finish> finishes=new List<Finish>();readonly List<Material> owned=new List<Material>();readonly List<Light> pendantLights=new List<Light>();
  CafeGame game;GameObject plants,panels,sconces;int knownLevel;
  public int Selected=>game?Mathf.Clamp(game.data.interiorStyle,0,3):0;
  public int SurfaceCount=>finishes.Count;
  public Color CurrentSeatColor {get{foreach(var f in finishes)if(f.channel==3)return f.material.color;return Color.clear;}}
  static Color FloorTint(int style)=>style==1?new Color(1,1,.96f):style==2?new Color(.84f,.78f,.7f):new Color(.79f,.78f,.75f);
  static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
  public static Color Swatch(int style,int channel)=>palettes[Mathf.Clamp(style,0,3)][Mathf.Clamp(channel,0,5)];
  public static bool Unlocked(SaveData data,int style)=>data!=null&&style>=0&&style<Levels.Length&&Economy.Level(data)>=Levels[style];
  public void Setup(CafeGame owner,CafePolish polish){game=owner;knownLevel=Economy.Level(game.data);var cache=new Dictionary<string,Material>();
   foreach(var r in game.world.GetComponentsInChildren<MeshRenderer>(true)){
    Vector3 pos=r.transform.position;if(pos.z< -10.1f||pos.z>9||Mathf.Abs(pos.x)>10.2f||r.GetComponent<TextMesh>()||r.sharedMaterials.Length!=1)continue;
    var source=r.sharedMaterial;if(!source)continue;int channel=-1;string n=r.name;
    if(n=="Arka duvar"||n=="Sol duvar"||n=="Sağ duvar"||source==polish.wall)channel=0;
    else if(source==polish.oak)channel=1;else if(source==polish.walnut||n=="Kahve barı - ahşap gövde"||n=="Ahşap ön panel")channel=2;
    else if(n=="El işçiliği parke")channel=6;
    else if(source==polish.fabric)channel=3;else if(source==polish.stone)channel=4;else if(source==polish.brass)channel=5;
    if(channel<0)continue;string key=channel+"/"+source.GetEntityId();if(!cache.TryGetValue(key,out var material)){material=new Material(source){name="Kafe tarzı / "+channel+" / "+source.name};cache[key]=material;owned.Add(material);finishes.Add(new Finish{material=material,original=source.color,channel=channel});}r.sharedMaterial=material;
   }
   foreach(var light in polish.scenery.GetComponentsInChildren<Light>(true))if(light.name=="Kafe lokal masa ışığı")pendantLights.Add(light);
   BuildDetails(polish);if(!Unlocked(game.data,Selected))game.data.interiorStyle=0;Apply();
  }
  public bool Select(int style){if(!Unlocked(game.data,style)){game.Tell(style>=0&&style<4?Names[style]+" için kafe seviyesi "+Levels[style]+" gerekli.":"Geçersiz dekor seçimi.");return false;}game.data.interiorStyle=style;Apply();game.Save();game.Tell(Names[style]+" tarzı uygulandı. Tarz değiştirmek ücretsiz.",true);return true;}
  public void Apply(){if(!game||finishes.Count==0)return;int style=Selected;foreach(var f in finishes)f.material.color=style==0?f.original:f.channel==6?FloorTint(style):palettes[style][f.channel];
   foreach(var light in pendantLights)if(light){light.color=style==2?new Color(1,.9f,.77f):style==3?new Color(1,.79f,.53f):new Color(1,.83f,.61f);light.intensity=style==3?.9f:1.1f;}
   plants.SetActive(style==1||style==3);panels.SetActive(style==2);sconces.SetActive(style==3);
  }
  void Update(){if(!game||!game.ui)return;int level=Economy.Level(game.data);if(level==knownLevel)return;int previous=knownLevel;knownLevel=level;for(int i=1;i<Levels.Length;i++)if(previous<Levels[i]&&level>=Levels[i])game.Tell("Yeni dekor açıldı: "+Names[i]+" · Menü > Dekorasyon",true);}
  GameObject Group(string name){var o=new GameObject(name);o.transform.SetParent(game.world.transform,false);return o;}
  Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.3f);owned.Add(m);return m;}
  GameObject Part(GameObject root,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material m){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(root.transform,false);o.transform.localPosition=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=m;var hit=o.GetComponent<Collider>();hit.enabled=false;Destroy(hit);return o;}
  void BuildDetails(CafePolish polish){plants=Group("Dekor tarzı / bitkiler");panels=Group("Dekor tarzı / lameller");sconces=Group("Dekor tarzı / aplikler");var ceramic=Mat("Dekor saksısı",C("B7A184"));var foliage=Mat("Dekor yaprağı",C("345D3D"));var timber=Mat("Dekor lameli",C("68503B"));var metal=Mat("Dekor aplik",C("B39356"));
   foreach(float side in new[]{-1f,1f}){var basePos=new Vector3(side*8.5f,0,-9.05f);Part(plants,"Dekor / seramik saksı",PrimitiveType.Cylinder,basePos+Vector3.up*.16f,new Vector3(.32f,.16f,.32f),ceramic);Part(plants,"Dekor / bitki gövdesi",PrimitiveType.Cylinder,basePos+Vector3.up*.5f,new Vector3(.022f,.32f,.022f),foliage);for(int i=0;i<6;i++){float a=i*2.4f;var leaf=Part(plants,"Dekor / bitki yaprağı",PrimitiveType.Sphere,basePos+new Vector3(Mathf.Cos(a)*.1f,.4f+i*.075f,Mathf.Sin(a)*.1f),new Vector3(.28f,.04f,.11f),foliage);leaf.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,20);}
    for(int i=0;i<10;i++)Part(panels,"Dekor / ahşap duvar lameli",PrimitiveType.Cube,new Vector3(side*9.68f,2.05f,-4.6f+i*.25f),new Vector3(.055f,.8f,.09f),timber);
    foreach(float z in new[]{-6.5f,-.7f}){var lamp=Part(sconces,"Dekor / pirinç duvar apliği",PrimitiveType.Cube,new Vector3(side*9.6f,2.15f,z),new Vector3(.12f,.26f,.18f),metal);var light=new GameObject("Dekor / aplik ışığı").AddComponent<Light>();light.transform.SetParent(lamp.transform,false);light.transform.position=new Vector3(side*9.35f,2.2f,z);light.type=LightType.Point;light.range=1.8f;light.intensity=.25f;light.color=new Color(1,.76f,.45f);light.shadows=LightShadows.None;}
   }
  }
  void OnDestroy(){foreach(var material in owned)if(material)Destroy(material);}
 }
}

