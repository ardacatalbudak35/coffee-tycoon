using System;
using System.Linq;
using UnityEngine;
namespace CafeLife {
 public static class CafeNaturalDetailChecks {
  public static string Report="";
  public static void Validate(CafeGame g){var art=Resources.Load<Texture2D>("CafeArt/CoffeePhotography");if(!art||art.width<512||art.height<512)throw new Exception("Photo art resource missing");var polish=g.GetComponent<CafePolish>();if(polish.scenery.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Fotoğraf çerçevesi "))!=3)throw new Exception("Three physical photo frames missing");foreach(var t in polish.scenery.GetComponentsInChildren<TextMesh>())if(t.text.Contains("DUAL GROUP")||t.text=="18.0 g"||t.text.Contains("ORIGIN /")||t.text.Contains("GOOD COMPANY")||t.text.Contains("SLOW MOMENTS"))throw new Exception("Artificial decorative lettering remains: "+t.text);Report="PASS Machine helper lettering and slogan art removed; three framed photo prints loaded\n";}
 }
}
