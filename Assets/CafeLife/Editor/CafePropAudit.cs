using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace CafeLife {
 public static class CafePropAudit {
  public static string Report="";
  static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
  static string Q(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
  public static void Validate(CafeGame g){var csv=new StringBuilder("type,name,x,y,z,width,height,depth\n");int meshes=0,colliders=0,targets=0;foreach(var r in g.world.GetComponentsInChildren<MeshRenderer>()){if(!r.enabled||r.GetComponent<TextMesh>())continue;var b=r.bounds;if(!Finite(b.center)||!Finite(b.size)||b.size.magnitude>150)throw new Exception("Invalid or giant renderer "+r.name+" "+b);if(!r.sharedMaterial||!r.sharedMaterial.shader||!r.sharedMaterial.shader.isSupported)throw new Exception("Unsupported prop material "+r.name);var f=r.GetComponent<MeshFilter>();if(f&&!f.sharedMesh)throw new Exception("Missing prop mesh "+r.name);meshes++;csv.AppendLine("mesh,"+Q(r.name)+","+F(b.center)+","+F(b.size));}
   foreach(var c in g.world.GetComponentsInChildren<Collider>()){if(!c.enabled||c.isTrigger)continue;var b=c.bounds;if(!Finite(b.center)||!Finite(b.size)||b.size.magnitude>150)throw new Exception("Invalid collider "+c.name);colliders++;csv.AppendLine("collider,"+Q(c.name)+","+F(b.center)+","+F(b.size));}
   foreach(var t in g.world.GetComponentsInChildren<CafeTarget>()){var volumes=t.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&c.isTrigger).ToArray();if(volumes.Length==0&&t.customer==null)throw new Exception("Interaction missing active trigger "+t.name);foreach(var c in volumes)if(c.bounds.size.magnitude>10)throw new Exception("Oversized interaction "+t.name);targets++;}
   File.WriteAllText("Logs/CafePropAudit.csv",csv.ToString(),Encoding.UTF8);Report="PASS Prop audit: "+meshes+" visible mesh renderers, "+colliders+" solid colliders, "+targets+" interaction roots checked\n";
  }
  static string F(Vector3 v)=>v.x.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+","+v.y.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+","+v.z.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
 }
}
