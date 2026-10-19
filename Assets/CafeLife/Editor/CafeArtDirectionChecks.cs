using System;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeArtDirectionChecks {
  public static string Report="";
  static void Check(bool value,string message){if(!value)throw new Exception(message);Report+="PASS "+message+"\n";}
  public static void Validate(CafeGame g){Report="";var polish=g.GetComponent<CafePolish>();var all=polish.scenery.GetComponentsInChildren<Transform>(true);
   var floor=all.First(t=>t.name=="El işçiliği parke").GetComponent<MeshFilter>().sharedMesh;
   Check(floor.vertexCount==4&&floor.triangles.Length==6,"Continuous parquet has no physical cracks or overlapping planks");
   Check(floor.normals.All(n=>n.y>.99f)&&floor.bounds.size.x<=20.01f&&floor.bounds.size.z<=20.01f,"Parquet faces upward and remains within cafe walls");
   Check(polish.oak.GetFloat("_ReceiveShadows")>.5f&&!polish.oak.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"),"Oak accepts real scene shadows");
   var seat=g.world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Döşemeli sırtlık");var mesh=seat.GetComponent<MeshFilter>().sharedMesh;
   Check(mesh.vertexCount>24&&mesh.bounds.size.x<.45f&&mesh.bounds.size.y<.25f&&mesh.bounds.size.z<.08f,"Rounded upholstery retains physical chair dimensions");
   var v=mesh.vertices;var n=mesh.normals;var tris=mesh.triangles;bool outward=true;for(int i=0;i<tris.Length;i+=3){int a=tris[i],b=tris[i+1],c=tris[i+2];outward&=Vector3.Dot(Vector3.Cross(v[b]-v[a],v[c]-v[a]),n[a]+n[b]+n[c])>=0;}Check(outward,"All upholstery faces render outward");
   Check(all.Where(t=>t.name=="Fotoğraf üstü ceviz ışık rayı").All(t=>t.GetComponent<Renderer>().bounds.min.y>3.1f),"Picture lights remain clear of framed artwork");
   var floorTexture=Resources.Load<Texture2D>("CafeMaterials/WoodFloorColor");
   Check(floorTexture&&floorTexture.width>=2048&&floorTexture.mipmapCount>1&&floorTexture.anisoLevel>=8,"Scanned parquet has high-resolution mipmaps and oblique filtering");
   var ceiling=all.First(t=>t.name=="Arka tavan birleşim silmesi").GetComponent<Renderer>().bounds;
   Check(ceiling.min.y<3.4f&&ceiling.max.y>3.475f,"Cornice closes the visible wall-ceiling sky gap");
   var worktop=g.world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Taş çalışma tezgâhı");
   Check(worktop.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("İnce pah")&&worktop.GetComponent<Collider>()!=null,"Rounded worktop retains its physical collision surface");
   var front=all.First(t=>t.name=="Ön tavan birleşim silmesi").GetComponent<Renderer>().bounds;
   Check(front.min.y<=3.35f&&front.max.y>=3.475f,"Front ceiling joint closes above entry lintel");
   Check(polish.fabric.GetTexture("_BaseMap")!=null,"Upholstery uses a woven surface texture");
  }
 }
}
