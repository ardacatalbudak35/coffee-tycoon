using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

namespace CafeLife
{
    [InitializeOnLoad]
    public static class CafeVisualUpgrade
    {
        const string Root="Assets/CafeLife/Art/";
        public const string ControllerPath="Assets/CafeLife/Art/CafePeople.controller";
        static CafeVisualUpgrade() { EditorApplication.delayCall+=Install; }
        static void Human(string path)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(!importer)throw new Exception("Missing model "+path);
            if(importer.animationType!=ModelImporterAnimationType.Human || !importer.importAnimation)
            {
                importer.animationType=ModelImporterAnimationType.Human; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=true; importer.SaveAndReimport();
            }
        }
        public static GameObject[] People() => new[]{"skaterMaleA","skaterFemaleA","criminalMaleA"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Root+n+".prefab")).Where(p=>p).ToArray();
        public static RuntimeAnimatorController Controller()=>AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        [MenuItem("Cafe Life/Görsel yenilemeyi uygula")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
            if(!File.Exists(Root+"Kenney/Model/characterMedium.fbx"))return;
            try
            {
                if(People().Length!=3 || !Controller())
                {
                    Human(Root+"Kenney/Model/characterMedium.fbx"); Human(Root+"Quaternius/UAL1_Standard.fbx");
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Kenney/Model/characterMedium.fbx");
                    var avatar=AssetDatabase.LoadAllAssetsAtPath(Root+"Kenney/Model/characterMedium.fbx").OfType<Avatar>().FirstOrDefault();
                    var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"Quaternius/UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                    File.WriteAllText("Logs/CafeVisualAssets.txt","Avatar valid="+(avatar && avatar.isValid)+" human="+(avatar && avatar.isHuman)+"\n"+string.Join("\n",clips.Select(c=>c.name)));
                    if(!avatar || !avatar.isValid || !avatar.isHuman)throw new Exception("Kenney humanoid avatar could not be mapped; inspect import report.");
                    AnimationClip Clip(string suffix) => clips.First(c=>c.name==suffix || c.name.EndsWith("|"+suffix));
                    var idle=Clip("Idle_Loop"); var walk=Clip("Walk_Loop"); var sit=Clip("Sitting_Idle_Loop");
                    foreach(var clip in new[]{idle,walk,sit}) { var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings); }
                    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)??AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                    controller.AddParameter("Moving",AnimatorControllerParameterType.Bool); controller.AddParameter("Sitting",AnimatorControllerParameterType.Bool);
                    var sm=controller.layers[0].stateMachine;
                    var a=sm.AddState("Idle"); a.motion=idle; sm.defaultState=a;
                    var b=sm.AddState("Walk"); b.motion=walk; b.speed=1;
                    var c=sm.AddState("Sitting"); c.motion=sit;
                    Transition(a,b,"Moving",true); Transition(b,a,"Moving",false);
                    var seated=sm.AddAnyStateTransition(c); seated.hasExitTime=false; seated.duration=.25f; seated.canTransitionToSelf=false; seated.AddCondition(AnimatorConditionMode.If,0,"Sitting");
                    Transition(c,a,"Sitting",false);
                    foreach(string skin in new[]{"skaterMaleA","skaterFemaleA","criminalMaleA"})
                    {
                        string matPath=Root+skin+".mat";
                        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Kenney/Skins/"+skin+".png")); mat.SetFloat("_Smoothness",.15f); AssetDatabase.CreateAsset(mat,matPath); }
                        var obj=UnityEngine.Object.Instantiate(model); obj.name=skin;
                        foreach(var renderer in obj.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>mat).ToArray();
                        var animator=obj.GetComponent<Animator>()??obj.AddComponent<Animator>(); animator.avatar=avatar; animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
                        PrefabUtility.SaveAsPrefabAsset(obj,Root+skin+".prefab"); UnityEngine.Object.DestroyImmediate(obj);
                    }
                    AssetDatabase.SaveAssets();
                }
                var game=UnityEngine.Object.FindAnyObjectByType<CafeGame>();
                if(game) { game.customerPrefabs=People(); game.customerAnimator=Controller(); EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(game.gameObject.scene); EditorSceneManager.SaveScene(game.gameObject.scene); }
                File.WriteAllText("Logs/CafeVisualInstall.txt","OK: Kenney CC0 models + Quaternius humanoid idle/walk/sitting; "+DateTime.Now);
            }
            catch(Exception ex) { File.WriteAllText("Logs/CafeVisualInstall.txt",ex.ToString()); Debug.LogException(ex); }
        }
        static void Transition(AnimatorState a,AnimatorState b,string parameter,bool value)
        { var t=a.AddTransition(b); t.hasExitTime=false;t.duration=.18f;t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,parameter); }
    }
}
