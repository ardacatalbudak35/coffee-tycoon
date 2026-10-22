using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CafeLife
{
    // Mixamo FBX'lerini (MixamoIndirilen klasörü) Resources/CafeMixamo altına alır, Humanoid olarak içe aktarır ve animator denetleyicisini kurar.
    public class CafeMixamoSetup : AssetPostprocessor
    {
        const string Folder="Assets/CafeLife/Resources/CafeMixamo";
        static readonly string[] AnimNames={"Idle","Walking","SittingIdle","Talking"};
        static bool IsAnim(string path)=>AnimNames.Contains(Path.GetFileNameWithoutExtension(path));
        static bool Mine(string path)=>path.Replace('\\','/').StartsWith(Folder+"/");

        void OnPreprocessTexture(){ if(!Mine(assetPath))return; var t=(TextureImporter)assetImporter; t.maxTextureSize=1024; t.textureCompression=TextureImporterCompression.Compressed; t.mipmapEnabled=true; }

        void OnPreprocessModel()
        {
            if(!Mine(assetPath))return; var m=(ModelImporter)assetImporter;
            m.animationType=ModelImporterAnimationType.Human; m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; m.isReadable=false; m.meshCompression=ModelImporterMeshCompression.Off;
            if(IsAnim(assetPath))
            {
                m.importAnimation=true; m.materialImportMode=ModelImporterMaterialImportMode.None;
                string clipName=Path.GetFileNameWithoutExtension(assetPath)=="Walking"?"Walk":Path.GetFileNameWithoutExtension(assetPath);
                var clips=m.defaultClipAnimations; foreach(var c in clips){c.name=clipName;c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}
                m.clipAnimations=clips;
            }
            else { m.importAnimation=false; }
        }

        [InitializeOnLoadMethod]
        static void Watch(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            var flag="Logs/RunCafeMixamoSetup.flag"; if(File.Exists(flag)){File.Delete(flag);Run();return;}
            flag="Logs/RunCafeMixamoTextures.flag"; if(File.Exists(flag)){File.Delete(flag);ExtractTextures();return;}
            flag="Logs/RunCafeMixamoMaterials.flag"; if(File.Exists(flag)){File.Delete(flag);ExtractMaterials();}
        }

        // Her modelin içindeki materyalleri .mat olarak çıkarır ve çıkarılmış Diffuse dokusunu _BaseMap'e bağlar.
        [MenuItem("Cafe Life/Mixamo materyallerini çıkar")]
        public static void ExtractMaterials()
        {
            var log=""; int fixedCount=0;
            foreach(var path in Directory.GetFiles(Folder,"*.fbx"))
            {
                string p=path.Replace('\\','/'); string n=Path.GetFileNameWithoutExtension(p); if(IsAnim(p))continue;
                string matDir=Folder+"/Mat_"+n,texDir=Folder+"/Tex_"+n; Directory.CreateDirectory(matDir);
                var mats=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>().ToArray();
                foreach(var m in mats){ string err=AssetDatabase.ExtractAsset(m,matDir+"/"+m.name+".mat"); if(!string.IsNullOrEmpty(err))log+=n+": "+m.name+" çıkarılamadı: "+err+"\n"; }
                AssetDatabase.WriteImportSettingsIfDirty(p); AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);
                var diffuses=Directory.Exists(texDir)?Directory.GetFiles(texDir).Where(f=>(f.EndsWith(".png")||f.EndsWith(".jpg"))&&Path.GetFileName(f).ToLowerInvariant().Contains("diffuse")).OrderBy(f=>Path.GetFileName(f),System.StringComparer.OrdinalIgnoreCase).ToList():new System.Collections.Generic.List<string>();
                var matFiles=Directory.GetFiles(matDir,"*.mat").OrderBy(f=>Path.GetFileNameWithoutExtension(f),System.StringComparer.OrdinalIgnoreCase).ToList();
                for(int mi=0;mi<matFiles.Count;mi++)
                {
                    var mp=matFiles[mi]; var mat=AssetDatabase.LoadAssetAtPath<Material>(mp.Replace('\\','/')); if(!mat)continue;
                    string diffuse=null;
                    if(n=="Remy"){ string token=mat.name.ToLowerInvariant().Replace("mat",""); diffuse=diffuses.FirstOrDefault(f=>Path.GetFileName(f).ToLowerInvariant().Contains(token)); }
                    else if(mi<diffuses.Count)diffuse=diffuses[mi];
                    mat.DisableKeyword("_EMISSION"); if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",Color.black); mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(mat);
                    if(diffuse!=null){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse.Replace('\\','/')); mat.SetTexture("_BaseMap",tex); mat.SetColor("_BaseColor",Color.white); mat.SetFloat("_Smoothness",.25f); mat.SetFloat("_Metallic",0f); EditorUtility.SetDirty(mat); fixedCount++; log+=n+": "+mat.name+" ← "+Path.GetFileName(diffuse)+"\n";}
                    else log+=n+": "+mat.name+" diffuse bulunamadı\n";
                }
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllText("Logs/CafeMixamoMaterials.txt",log+"done "+fixedCount);
        }

        // FBX içine gömülü dokuları dosyaya çıkarır; materyaller dokuları otomatik bulur.
        [MenuItem("Cafe Life/Mixamo dokularını çıkar")]
        public static void ExtractTextures()
        {
            int ok=0; var log="";
            foreach(var path in Directory.GetFiles(Folder,"*.fbx"))
            {
                string p=path.Replace('\\','/'); string n=Path.GetFileNameWithoutExtension(p); if(IsAnim(p))continue;
                var imp=AssetImporter.GetAtPath(p) as ModelImporter; if(imp==null){log+=n+": importer yok\n";continue;}
                string dir=Folder+"/Tex_"+n; Directory.CreateDirectory(dir);
                try{ bool r=imp.ExtractTextures(dir); AssetDatabase.WriteImportSettingsIfDirty(p); AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate); log+=n+": "+(r?"çıkarıldı":"dosya yok")+" · "+Directory.GetFiles(dir).Count(f=>!f.EndsWith(".meta"))+" doku\n"; ok++; }
                catch(System.Exception ex){log+=n+": HATA "+ex.Message+"\n";}
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllText("Logs/CafeMixamoTextures.txt",log+"done "+ok);
        }

        [MenuItem("Cafe Life/Mixamo karakterlerini kur")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            foreach(var src in Directory.GetFiles("MixamoIndirilen","*.fbx"))
            {
                string name=Path.GetFileName(src); string n=Path.GetFileNameWithoutExtension(src);
                // dosya adlarını sadeleştir: "Lola B Styperek" → Lola, "Skeletonzombie T Avelange" → Skeleton, ...
                string clean=n.StartsWith("Lola")?"Lola":n.StartsWith("Skeletonzombie")?"Skeleton":n.StartsWith("Vampire")?"Vampire":n=="kaya"?"Kaya":n.Replace(" ","");
                string dst=Folder+"/"+clean+".fbx"; if(!File.Exists(dst))File.Copy(src,dst);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildController();
            File.WriteAllText("Logs/CafeMixamoSetup.txt","OK "+Directory.GetFiles(Folder,"*.fbx").Length);
        }

        static AnimationClip Clip(string file,string clipName)
        {
            foreach(var o in AssetDatabase.LoadAllAssetsAtPath(Folder+"/"+file+".fbx"))if(o is AnimationClip c&&!c.name.StartsWith("__preview__")&&c.name==clipName)return c;
            foreach(var o in AssetDatabase.LoadAllAssetsAtPath(Folder+"/"+file+".fbx"))if(o is AnimationClip c&&!c.name.StartsWith("__preview__"))return c;
            return null;
        }
        static void BuildController()
        {
            string path=Folder+"/CafeMixamo.controller"; AssetDatabase.DeleteAsset(path);
            var ctrl=AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Moving",AnimatorControllerParameterType.Bool); ctrl.AddParameter("Sitting",AnimatorControllerParameterType.Bool); ctrl.AddParameter("Talking",AnimatorControllerParameterType.Bool);
            var sm=ctrl.layers[0].stateMachine;
            var idle=sm.AddState("Idle"); idle.motion=Clip("Idle","Idle");
            var walk=sm.AddState("Walk"); walk.motion=Clip("Walking","Walk");
            var sit=sm.AddState("Sitting"); sit.motion=Clip("SittingIdle","SittingIdle");
            var talk=sm.AddState("Talking"); talk.motion=File.Exists(Folder+"/Talking.fbx")?Clip("Talking","Talking"):idle.motion;
            sm.defaultState=idle;
            AnimatorStateTransition T(AnimatorState from,AnimatorState to,string param,bool value){var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.15f;t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,param);return t;}
            T(idle,walk,"Moving",true); T(walk,idle,"Moving",false);
            foreach(var s in new[]{idle,walk,talk}){var t=sm.AddAnyStateTransition(sit);t.hasExitTime=false;t.duration=.2f;t.AddCondition(AnimatorConditionMode.If,0,"Sitting");t.canTransitionToSelf=false;break;}
            T(sit,idle,"Sitting",false); T(idle,talk,"Talking",true); T(talk,idle,"Talking",false);
            AssetDatabase.SaveAssets();
        }
    }
}
