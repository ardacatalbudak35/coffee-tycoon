using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CafeLife
{
    // Üst kat evi için "VNB - Home Set" modellerini sahnedeki CafeGame.homePrefabs listesine bağlar ve boyutlarını Logs/CafeHomeAssets.txt'ye yazar.
    [InitializeOnLoad]
    public static class CafeHomeAssets
    {
        const string Folder="Assets/VNB - Home Set/Prefabs";
        static readonly string[] Names={
            "1P Bed","2P Bed","Bed Table","Sofa","Sofa 2","Coffee Table","TV 1","Wardrobe","Dining Table","Chair","Chair 2","Stool","Table (Modern)",
            "Drawer 1","Drawer 2","Drawer-Cabinet Complete 1","Drawer-Cabinet Complete 2","Modern Shelves","Wall Shelf","Wall Shelf 2","Simple Shelf",
            "Fridge","Counter (Connected)","Counter (Connected - Sink)","Counter (Separated)","Oven","Microwave","Coffee Machine","Toaster","Sink",
            "Toilet","Bathtub-Shower","Bathtub","Mirror","Mirror 2","Towel (Wall)","Washing Machine","Trash Bin",
            "Lamp 1","Lamp 2","Lamp 3","Small Lamp 1","Small Lamp 2","Ceiling Lamp 1","Ceiling Lamp 2","Ceiling Lamp 3",
            "Painting 1","Painting 2","Painting 3","Painting Small 1","Painting Small 2","Books I","Books II","Book","Vase 1","Vase 2","Small Green Deco",
            "Curtains","Blinds (Open)","Window 1","Window 3","Door 1","Door 2","Door Frame 1","Simple Pillow (Sofa)","Laptop","Cup (Filled)","Simple Coffee","Box","Small Box","Toolbox","Fan"
        };
        static CafeHomeAssets() { EditorApplication.delayCall+=Install; }

        [MenuItem("Cafe Life/Üst kat modellerini bağla")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
            try
            {
                var game=Object.FindAnyObjectByType<CafeGame>(); if(!game||!Directory.Exists(Folder))return;
                var map=AssetDatabase.FindAssets("t:Prefab",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).GroupBy(p=>Path.GetFileNameWithoutExtension(p)).ToDictionary(g=>g.Key,g=>g.First());
                var list=Names.Where(map.ContainsKey).Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(map[n])).Where(p=>p).ToArray();
                var report=new StringBuilder("Eksik: "+string.Join(", ",Names.Where(n=>!map.ContainsKey(n)))+"\n");
                foreach(var prefab in list)
                {
                    var temp=(GameObject)PrefabUtility.InstantiatePrefab(prefab); var renderers=temp.GetComponentsInChildren<Renderer>();
                    if(renderers.Length>0){ var b=renderers[0].bounds; foreach(var r in renderers)b.Encapsulate(r.bounds); report.AppendLine(prefab.name+" size="+b.size.ToString("F2")+" min="+(b.min-temp.transform.position).ToString("F2")); }
                    Object.DestroyImmediate(temp);
                }
                File.WriteAllText("Logs/CafeHomeAssets.txt",report.ToString());
                if(game.homePrefabs==null||!game.homePrefabs.SequenceEqual(list))
                {
                    game.homePrefabs=list; EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(game.gameObject.scene); EditorSceneManager.SaveScene(game.gameObject.scene);
                }
            }
            catch(System.Exception ex){ File.WriteAllText("Logs/CafeHomeAssets.txt",ex.ToString()); }
        }
    }
}
