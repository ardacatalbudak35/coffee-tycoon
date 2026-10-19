using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public sealed partial class CafeUI {
  static Sprite touchDisk,touchRing;
  static Sprite TouchCircle(bool ring){if(ring&&touchRing)return touchRing;if(!ring&&touchDisk)return touchDisk;const int n=96;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};for(int y=0;y<n;y++)for(int x=0;x<n;x++){float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*n*.5f);float alpha=Mathf.Clamp01(n*.5f-1-distance);if(ring)alpha*=Mathf.Clamp01(distance-(n*.5f-3));texture.SetPixel(x,y,new Color(1,1,1,alpha));}texture.Apply();var sprite=Sprite.Create(texture,new UnityEngine.Rect(0,0,n,n),Vector2.one*.5f,100);if(ring)touchRing=sprite;else touchDisk=sprite;return sprite;}
  void StyleTouchControls(){var background=joystick.GetComponent<Image>();background.sprite=TouchCircle(false);background.type=Image.Type.Simple;background.color=new Color(.035f,.05f,.045f,.65f);var rim=Panel((RectTransform)joystick.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.86f,.74f,.53f,.55f));rim.name="Joystick circular rim";rim.GetComponent<Image>().sprite=TouchCircle(true);rim.GetComponent<Image>().type=Image.Type.Simple;rim.GetComponent<Image>().raycastTarget=false;rim.SetAsFirstSibling();var thumb=game.player.movePad.knob.GetComponent<Image>();thumb.sprite=TouchCircle(false);thumb.type=Image.Type.Simple;thumb.color=new Color(.90f,.70f,.40f,.95f);
   foreach(var button in action.GetComponentsInChildren<Button>(true)){var label=button.GetComponentInChildren<Text>();if(label.text!="ETKİLEŞ")continue;button.targetGraphic.color=new Color(.90f,.70f,.40f);label.color=new Color(.055f,.065f,.07f);label.GetComponent<CafeFontSize>().color=label.color;label.fontStyle=FontStyle.Bold;}
  }
 }
}

