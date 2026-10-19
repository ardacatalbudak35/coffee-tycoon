using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CafeLife {
 public sealed partial class CafePolish {
  // Physical joinery and restrained material variation. No new movement blockers.
  void CraftedInterior(){
   oak.SetFloat("_ReceiveShadows",1);oak.DisableKeyword("_RECEIVE_SHADOWS_OFF");
   fabric.SetTexture("_BaseMap",Weave());fabric.SetFloat("_Smoothness",.16f);
   ceramic.SetFloat("_Smoothness",.32f);brass.SetFloat("_Smoothness",.36f);
   var meshes=new Dictionary<string,Mesh>();
   foreach(var r in game.world.GetComponentsInChildren<MeshRenderer>(true)){
    string n=r.name;
    if(n=="Meşe döşeme"||n=="Casino kapalı iskambil"||n=="Casino kart arka baskı"||n=="Casino ince fiş")r.enabled=false;
    if(n=="Merkez geçiş pirinç şeridi"||n=="Duvar ince çıta detayı")r.enabled=false;
    if(n=="Sedir dokulu minder"||n=="Sedir yüksek sırtlık"||n=="Döşemeli sırtlık"||n=="NOIR lounge minder"||n=="NOIR lounge sırt"){
     var size=r.transform.localScale;string key=size.ToString("F3");
     if(!meshes.TryGetValue(key,out var mesh)){mesh=SoftBox(size,Mathf.Min(.055f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.3f));meshes[key]=mesh;combinedMeshes.Add(mesh);}
     r.GetComponent<MeshFilter>().sharedMesh=mesh;r.transform.localScale=Vector3.one;
    }
    if(n=="Masa keten runner"){r.transform.localScale=new Vector3(.28f,.006f,.72f);r.transform.localRotation=Quaternion.Euler(0,8,0);}
   }
   CraftedFloor();
   int table=0;foreach(var t in game.world.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Butik masa seti")){
    // Small arranged differences preserve every table/chair interaction anchor.
    foreach(Transform child in t)if(child.name=="Seramik vazo"||child.name=="Vazo çiçeği"||child.name=="Küçük yaprak"){var p=child.localPosition;p.z+=(table%3-1)*.12f;child.localPosition=p;}
    foreach(Transform child in t)if(child.name=="Masa kahve altlığı")child.localRotation=Quaternion.Euler(0,table*23,0);
    if(table%3==1){var runner=t.Find("Masa keten runner");if(runner)runner.gameObject.SetActive(false);}
    table++;
   }
   // A recessed warm strip gives the rear working area depth without text clutter.
   var strip=Surface("Raf altı sıcak difüzör",new Color(.9f,.77f,.55f),.2f);
   strip.EnableKeyword("_EMISSION");strip.SetColor("_EmissionColor",new Color(1,.77f,.48f)*.65f);
   foreach(float x in new[]{-5.7f,0f,5.7f}){
    Shape("Fotoğraf üstü ceviz ışık rayı",PrimitiveType.Cube,scenery,new Vector3(x,3.22f,8.63f),new Vector3(1.5f,.055f,.20f),walnut);
    Shape("Ray altı difüzör",PrimitiveType.Cube,scenery,new Vector3(x,3.181f,8.49f),new Vector3(1.3f,.014f,.018f),strip);
   }
  }
  Texture2D Weave(){var tex=new Texture2D(128,128,TextureFormat.RGB24,true){name="İnce dokuma",wrapMode=TextureWrapMode.Repeat,anisoLevel=4};var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float v=.88f+(((x/2+y/2)%2)==0?.055f:0)+Mathf.PerlinNoise(x*.7f,y*.7f)*.065f;pixels[y*128+x]=new Color(v,v,v);}tex.SetPixels(pixels);tex.Apply();textures.Add(tex);return tex;}
  void CraftedFloor(){
   // One continuous surface: subpixel physical gaps produced dotted black seams.
   // Real scanned grain and mipmapped seams remain stable at oblique angles.
   var color=Resources.Load<Texture2D>("CafeMaterials/WoodFloorColor");
   var normal=Resources.Load<Texture2D>("CafeMaterials/WoodFloorNormal");
   var material=Surface("Doğal meşe parke",Color.white,.24f,0,color);
   if(normal){material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",.3f);material.EnableKeyword("_NORMALMAP");}
   var mesh=new Mesh{name="Kesintisiz doğal parke"};
   mesh.vertices=new[]{new Vector3(-10,.033f,-11),new Vector3(-10,.033f,9),new Vector3(10,.033f,9),new Vector3(10,.033f,-11)};
   float tiles=20f/1.7f;
   mesh.uv=new[]{Vector2.zero,new Vector2(0,tiles),new Vector2(tiles,tiles),new Vector2(tiles,0)};
   mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();combinedMeshes.Add(mesh);
   var floor=new GameObject("El işçiliği parke",typeof(MeshFilter),typeof(MeshRenderer));floor.transform.SetParent(scenery,false);
   floor.GetComponent<MeshFilter>().sharedMesh=mesh;floor.GetComponent<MeshRenderer>().sharedMaterial=material;
   Shape("Ön tavan birleşim silmesi",PrimitiveType.Cube,scenery,new Vector3(0,3.41f,-10.94f),new Vector3(20,.22f,.14f),walnut);
   // Close the 7.5 cm sky gap between the existing walls and ceiling.
   foreach(float x in new[]{-9.94f,9.94f})Shape("Tavan duvar birleşim silmesi",PrimitiveType.Cube,scenery,new Vector3(x,3.43f,-1),new Vector3(.14f,.17f,20),wall);
   Shape("Arka tavan birleşim silmesi",PrimitiveType.Cube,scenery,new Vector3(0,3.43f,8.94f),new Vector3(20,.17f,.14f),wall);
  }
  Mesh SoftBox(Vector3 size,float radius){
   var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();Vector3 half=size*.5f,inner=half-Vector3.one*radius;
   Vector3[] faces={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
   foreach(var normal in faces){Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,v=Vector3.Cross(normal,u);int first=verts.Count;const int segments=4;
    for(int y=0;y<=segments;y++)for(int x=0;x<=segments;x++){
     float a=x/(float)segments,b=y/(float)segments;Vector3 p=Vector3.Scale(normal+u*(a*2-1)+v*(b*2-1),half),c=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));Vector3 n=(p-c).normalized;verts.Add(c+n*radius);normals.Add(n);uv.Add(new Vector2(a,b));
    }
    for(int y=0;y<segments;y++)for(int x=0;x<segments;x++){int k=first+y*(segments+1)+x;tris.AddRange(new[]{k,k+1,k+segments+1,k+1,k+segments+2,k+segments+1});}
   }
   var mesh=new Mesh{name="Yuvarlatılmış döşeme"};mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
  }
 }
}
