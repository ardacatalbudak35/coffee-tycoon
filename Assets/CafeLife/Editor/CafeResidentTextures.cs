using System.IO;
using UnityEditor;
using UnityEngine;

namespace CafeLife
{
    // 12 sakin için Kenney dokusunu (saç/ten/üst/alt) CafeCharacters.json renklerine göre boyayıp Resources/CafeResidents altına yazar.
    [InitializeOnLoad]
    public static class CafeResidentTextures
    {
        static CafeResidentTextures(){EditorApplication.update+=Poll;}
        static void Poll(){var flag="Logs/RunCafeResidentTextures.flag";if(!File.Exists(flag))return;File.Delete(flag);Generate();}
        [MenuItem("Cafe Life/Sakin dokularını üret")]
        public static void Generate()
        {
            var json=File.ReadAllText("Assets/CafeLife/Resources/CafeCharacters.json");
            var list=JsonUtility.FromJson<Wrap>("{\"items\":"+json+"}");
            Directory.CreateDirectory("Assets/CafeLife/Resources/CafeResidents");
            foreach(var r in list.items)
            {
                bool female=r.cinsiyet=="kadin";
                var src=Texture(female?"skaterFemaleA":"skaterMaleA");
                var px=src.GetPixels32(); int w=src.width,h=src.height;
                var hair=Hex(r.gorunum.sac_rengi_hex);var shirt=Hex(r.gorunum.ust_renk_hex);var pants=Hex(r.gorunum.alt_renk_hex);var skin=Skin(r.gorunum.ten);
                Color baseSkin=new Color32(245,140,106,255),baseHair=px[(h-1-30)*w+50];
                var outp=new Color32[px.Length];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    int ty=h-1-y; var c=(Color)px[y*w+x]; float l=Lum(c); Color o=c;
                    bool head=x<640&&ty<490,shirtReg=x<640&&ty>=490,pantsReg=x>=611&&ty>=764;
                    if(Near(c,baseSkin,.16f))o=Tint(c,skin,Lum(baseSkin));
                    else if(head&&Near(c,baseHair,.2f))o=Tint(c,hair,Lum(baseHair));
                    else if(shirtReg&&l>.18f&&!Near(c,baseSkin,.07f))o=Tint(c,shirt,.5f);
                    else if(pantsReg&&l>.1f)o=Tint(c,pants,.2f);
                    outp[y*w+x]=o;
                }
                var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);tex.SetPixels32(outp);
                File.WriteAllBytes("Assets/CafeLife/Resources/CafeResidents/"+r.id+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
            }
            AssetDatabase.Refresh();
            File.WriteAllText("Logs/CafeResidentTextures.txt","OK "+list.items.Length);
        }
        static Texture2D Texture(string n){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes("Assets/CafeLife/Art/Kenney/Skins/"+n+".png"));return t;}
        static float Lum(Color c)=>c.r*.3f+c.g*.59f+c.b*.11f;
        static bool Near(Color a,Color b,float d)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<d*3;
        static Color Tint(Color c,Color target,float baseLum){float k=Mathf.Clamp(Lum(c)/Mathf.Max(.05f,baseLum),.25f,1.4f);return new Color(Mathf.Clamp01(target.r*k),Mathf.Clamp01(target.g*k),Mathf.Clamp01(target.b*k),1);}
        static Color Hex(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
        static Color Skin(string t){switch(t){case "acik":return Hex("#F2C7A5");case "esmer":return Hex("#A8714D");case "koyu":return Hex("#6B4630");default:return Hex("#D9A073");}}
        [System.Serializable]public class Wrap{public R[] items;}
        [System.Serializable]public class G{public int boy_cm;public string yapi,ten,sac_rengi_hex,ust_renk_hex,alt_renk_hex;}
        [System.Serializable]public class R{public string id,ad,cinsiyet,siparis;public int geldigi_saat;public string[] replikler;public G gorunum;}
    }
}
