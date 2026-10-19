using UnityEngine;
namespace CafeLife {
 public sealed class CafeDepthLabel:MonoBehaviour {
  Material material;Font font;
  public void Setup(TextMesh mesh,Font printFont=null){font=printFont?printFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");mesh.font=font;font.RequestCharactersInTexture(mesh.text,mesh.fontSize);material=new Material(font.material);material.shader=Resources.Load<Shader>("CafeWorldText");material.mainTexture=font.material.mainTexture;mesh.GetComponent<Renderer>().sharedMaterial=material;Font.textureRebuilt+=Rebuilt;}
  void Rebuilt(Font rebuilt){if(rebuilt==font&&material)material.mainTexture=font.material.mainTexture;}
  void OnDestroy(){Font.textureRebuilt-=Rebuilt;if(material)Destroy(material);}
 }
}

