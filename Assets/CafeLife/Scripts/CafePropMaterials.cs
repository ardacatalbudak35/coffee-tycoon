using System.Collections.Generic;
using UnityEngine;

namespace CafeLife {
 public sealed partial class CafePolish {
  // Run before decoration materials are cloned and static scenery is combined.
  void RefinePropMaterials(){
   RefineAppliancePalettes();
   var machineSteel=Surface("Makine saten paslanmaz çelik",new Color(.48f,.50f,.49f),.35f,.4f);
   var rounded=new Dictionary<string,Mesh>();
   foreach(var renderer in game.world.GetComponentsInChildren<MeshRenderer>()){
    float radius;
    switch(renderer.name){
     case "Makine · kontrol yüzeyi":
     case "Makine · üst fincan ısıtıcı":
     case "Makine · alt kasa":renderer.sharedMaterial=machineSteel;radius=.006f;break;
     case "Taş çalışma tezgâhı":
     case "Bar sol dönüş taş tablası":
     case "Arka tezgâh taş yüzey":
     case "Pastane taş tabla":radius=.012f;break;
     case "Makine · arka kazan":
     case "Buz ünitesi gövdesi":radius=.025f;break;
     case "Peçetelik":radius=.008f;break;
     default:continue;
    }
    var filter=renderer.GetComponent<MeshFilter>();
    if(!filter||!filter.sharedMesh||filter.sharedMesh.name!="Cube")continue;
    // Only primitive visual meshes change; original transforms and collision boxes stay intact.
    // Store the rounded shape in unit coordinates to retain non-unit collider dimensions.
    Vector3 size=renderer.transform.localScale;
    if(size.x<=0||size.y<=0||size.z<=0)continue;
    string key=size.ToString("F4")+"/"+radius;
    if(!rounded.TryGetValue(key,out var mesh)){
     mesh=SoftBox(size,Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.2f));
     var points=mesh.vertices;var normals=mesh.normals;
     for(int i=0;i<points.Length;i++){
      points[i]=new Vector3(points[i].x/size.x,points[i].y/size.y,points[i].z/size.z);
      normals[i]=Vector3.Scale(normals[i],size).normalized;
     }
     mesh.vertices=points;mesh.normals=normals;mesh.RecalculateBounds();
     mesh.name="İnce pah · "+renderer.name;rounded[key]=mesh;combinedMeshes.Add(mesh);
    }
    filter.sharedMesh=mesh;
   }
  }

  void RefineAppliancePalettes(){
   var replacements=new Dictionary<Material,Material>();
   var palettes=new Dictionary<Texture,Texture2D>();
   foreach(string appliance in new[]{"refrigerator_1","waterCooler"}){
    var root=game.world.transform.Find(appliance);if(!root)continue;
    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
    // The dispenser body is a separate inspected mesh. Preserve its reservoir,
    // water, and every hot/cold tap renderer with their original identifying colors.
    if(appliance=="waterCooler"&&renderer.name!="waterCooler")continue;
    var slots=renderer.sharedMaterials;bool changed=false;
    for(int i=0;i<slots.Length;i++){
     var source=slots[i];
     // Main is the inspected imported color atlas. Glass and any additional materials remain intact.
     if(!source||(source.name!="Main"&&source.name!="Main (URP)")||!source.HasProperty("_BaseMap"))continue;
     var atlas=source.GetTexture("_BaseMap");if(!atlas)continue;
     if(!replacements.TryGetValue(source,out var finish)){
      if(!palettes.TryGetValue(atlas,out var palette)){palette=FridgePalette(atlas);palettes[atlas]=palette;}
      finish=new Material(source){name="Saten gri ekipman emayesi"};
      finish.SetTexture("_BaseMap",palette);finish.SetFloat("_Smoothness",.32f);finish.SetFloat("_Metallic",.12f);
      materials.Add(finish);replacements[source]=finish;
     }
     slots[i]=finish;changed=true;
    }
    if(changed)renderer.sharedMaterials=slots;
    }
   }
  }

  Texture2D FridgePalette(Texture source){
   // The imported atlas is not CPU-readable. Copy once, without changing the shared source asset.
   int width=Mathf.Min(1024,source.width),height=Mathf.Min(1024,source.height);
   var copy=new Texture2D(width,height,TextureFormat.RGBA32,true){name="Ekipman özel sakin palet",wrapMode=source.wrapMode,filterMode=FilterMode.Trilinear,anisoLevel=4};textures.Add(copy);
   var previous=RenderTexture.active;
   bool previousSrgb=GL.sRGBWrite;
   var buffer=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
   try{GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;Graphics.Blit(source,buffer);RenderTexture.active=buffer;copy.ReadPixels(new Rect(0,0,width,height),0,0,false);}
   finally{GL.sRGBWrite=previousSrgb;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(buffer);}
   var pixels=copy.GetPixels32();
   for(int i=0;i<pixels.Length;i++){
    Color color=pixels[i];Color.RGBToHSV(color,out float hue,out float saturation,out _);
    if(hue<.48f||hue>.76f||saturation<.23f)continue;
    float luminance=color.r*.2126f+color.g*.7152f+color.b*.0722f;
    float shade=Mathf.Clamp01(luminance*1.45f+.08f);
    Color neutral=Color.Lerp(new Color(.075f,.085f,.08f),new Color(.84f,.82f,.75f),shade);
    neutral.a=color.a;pixels[i]=neutral;
   }
   copy.SetPixels32(pixels);copy.Apply(true,true);return copy;
  }
 }
}
