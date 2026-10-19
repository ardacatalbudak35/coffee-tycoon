using System;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafeTypographyChecks {
  public static string Report="";
  public static void Validate(CafeGame g){Report="";var regular=CafeTypography.Regular;var strong=CafeTypography.Strong;if(!regular||!strong||regular==strong)throw new Exception("Typography font weights missing");foreach(var font in new[]{regular,strong}){font.RequestCharactersInTexture("ÇçĞğİıÖöŞşÜü",24);foreach(char c in "ÇçĞğİıÖöŞşÜü")if(!font.HasCharacter(c))throw new Exception("Missing Turkish glyph "+c);}foreach(var t in GameObject.Find("Cafe HUD").GetComponentsInChildren<Text>(true)){if(t.font!=regular&&t.font!=strong)throw new Exception("Legacy UI font remains: "+t.text);if(t.GetComponent<Outline>())throw new Exception("Heavy text outline remains: "+t.text);}Report="PASS UI uses bundled regular/semibold fonts with Turkish glyphs and no heavy outlines\n";}
 }
}
