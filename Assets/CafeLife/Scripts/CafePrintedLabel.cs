using UnityEngine;
namespace CafeLife {
 // Physical print dimensions, independent of the containing prop's world scale.
 public sealed class CafePrintedLabel:MonoBehaviour {
  public Vector2 area;
  public bool fitted {get;private set;}
  string lastText;
  void LateUpdate(){var text=GetComponent<TextMesh>();if(text&&text.text!=lastText)Fit();}
  void Start(){Fit();}
  public void Fit(){var text=GetComponent<TextMesh>();if(!text)return;var renderer=GetComponent<Renderer>();var size=renderer.localBounds.size;if(size.x<=0||size.y<=0)return;float ratio=Mathf.Min(area.x/size.x,area.y/size.y);text.characterSize*=ratio;lastText=text.text;fitted=true;}
 }
}
