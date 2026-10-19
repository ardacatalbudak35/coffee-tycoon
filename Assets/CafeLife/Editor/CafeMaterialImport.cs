using UnityEditor;
using UnityEngine;
namespace CafeLife {
 public sealed class CafeMaterialImport:AssetPostprocessor {
  void OnPreprocessTexture(){
   if(!assetPath.Contains("/Resources/CafeMaterials/WoodFloor"))return;
   var t=(TextureImporter)assetImporter;
   t.textureType=assetPath.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
   t.sRGBTexture=!assetPath.Contains("Normal");t.maxTextureSize=2048;
   t.mipmapEnabled=true;t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;
   t.wrapMode=TextureWrapMode.Repeat;t.textureCompression=TextureImporterCompression.CompressedHQ;
   t.isReadable=false;
  }
 }
}
