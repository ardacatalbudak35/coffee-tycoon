using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    // Resources/CafeCharacters.json'daki 12 mahalle sakini: geldigi_saat'te kafeye gerçek müşteri olarak girer.
    [System.Serializable] public class ResState{ public string id; public int visits,bond,goneDay,cups,absentUntil,refused; public bool met,memory1,memory2,returned; }
    public static class CafeResidents
    {
        [System.Serializable] public class Wrap{public Resident[] items;}
        [System.Serializable] public class Look{public int boy_cm;public string yapi,ten,sac,sac_rengi_hex,ust_giysi,ust_renk_hex,alt_giysi,alt_renk_hex,ayakkabi,aksesuar;}
        [System.Serializable] public class Resident{public int yas,kahve_bagimliligi;public string huy;public string id,ad,cinsiyet,meslek,siparis,haber_tepkisi,gece_gizemi;public int geldigi_saat;public string[] replikler;public Look gorunum;}
        static Resident[] all; static int visitDay=-1; static readonly HashSet<string> visited=new HashSet<string>();
        public static Resident[] All{get{if(all==null){var t=Resources.Load<TextAsset>("CafeCharacters");all=t?JsonUtility.FromJson<Wrap>("{\"items\":"+t.text+"}").items:new Resident[0];}return all;}}
        // ---------- hikâye bağı: tanışma, bağ puanı, anılar, 20. fincan kaybı ----------
        static readonly Dictionary<string,string[]> Memories=new Dictionary<string,string[]>{
            {"ayse_ogretmen",new[]{"Hasan Dede bana her sabah gazete ayırırdı. Bir gün manşette kendi adını görünce 'bunu saklama' dedi. Hâlâ saklıyorum.","Duvardaki üç resmi dede kendi çizdi, kimseye söylemedi. Sol alttaki imza H değil, A. Benim adım. Ona resim dersi vermiştim."}},
            {"can_tasarimci",new[]{"Dedenin kahve logosunu ben çizdim. Karşılığında ömür boyu bedava filtre istedi. Hâlâ ödüyor muyum bilmiyorum.","Logodaki fincanın tam ortasında küçük bir 20 var. Dedenin isteğiydi. Anlamını sormadım, sormamalıydım."}},
            {"elif_anne",new[]{"Bebeğim doğduğunda Hasan Dede ilk fincanı bedavaya yaptı. 'Süt kokusu kahveyle iyi gider' dedi.","Bebek geceleri hiç uyumuyor. Dedenin sütle ilgili bir tarifi vardı, belki defterindedir."}},
            {"mehmet_insaat",new[]{"Bu dükkânın tezgâhını ben yaptım. Dede parayı kahveyle ödedi. 19 fincan, gün gün.","Çekmecenin altı boş. Sakın içine el sokma dediğimi duymadın, değil mi?"}},
            {"elif",new[]{"Sınav haftalarında dedeniz bana demlik bırakırdı, parasız. Kimseye söylemeyin.","Geceleri çalışırken televizyon kendi kendine açılıyor. Ekranda hep aynı yazı: 20."}},
            {"ahmet",new[]{"Sabah 8'de buradaydım, 40 yıldır. Hasan Dede kapıyı benim için erken açardı.","Dede bir gece 20. fincanı içti, sabaha kadar dükkânda oturdu. Ertesi gün hiçbir şey olmamış gibi davrandı."}},
            {"zeynep",new[]{"Gece vardiyasından çıkıp buraya gelirdim. Dede 'uyuma, uyumak için kahve içme' derdi.","Hastanede bir hasta 20. fincanı içmişti, gece gölge gibi gitti. Kimse konuşmuyor."}},
            {"mehmet",new[]{"Taksiyle gelirdim, Dede arka bahçeye çıkıp bir şey gömerdi. Ne olduğunu hâlâ bilmiyorum.","Gece son müşteriyi bıraktığımda dükkânın ışığı hep açıktı. Dede yoktu ama içeride biri vardı."}},
            {"elena",new[]{"Hasan Dede bana Türkçe öğretti: 'kahve' ve 'selam'. Sonrası zor.","Gölge dedikleri şey bizim dilimizde hatıra demek. Ürkütücü ama kötü değil."}},
            {"memur_ahmet",new[]{"Belediyede kayıtlara baktım. Dükkân 1962'de açılmış. İlk sahibi Hasan değilmiş.","Kayıtta ilk sahibin adı silinmiş. Mürekkep hâlâ taze."}},
            {"balikci_mehmet",new[]{"Dede balığa çıkmadan önce termosumu doldururdu. Bir keresinde içinden kahve yerine harita çıktı.","Harita denizi değil, bu mahalleyi gösteriyordu. Dükkânın altında bir şey var."}},
            {"komsu_zeynep",new[]{"Ben kahve içmem. Ama Hasan hâlâ camdan el sallar. Öldüğünü biliyorum, yine de sallar.","Her gece camın önünde uzun paltolu bir adam duruyor. Dedeyi bekliyor gibi."}}
        };
        public static ResState State(CafeGame g,Resident r){ if(g.data.residents==null)g.data.residents=new List<ResState>(); foreach(var s in g.data.residents)if(s.id==r.id)return s; var n=new ResState{id=r.id}; g.data.residents.Add(n); return n; }
        // Sipariş alınınca çağrılır: tanışma, anı veya dönüş konuşması
        public static void OnMeet(CafeGame g,Resident r)
        {
            if(g.story==null||!CafeStory.Active)return; var s=State(g,r); var mem=Memories.TryGetValue(r.id,out var m)?m:null;
            if(!s.met){ s.met=true; g.story.Talk(null,r.ad,r.replikler!=null&&r.replikler.Length>0?r.replikler[0]:"Merhaba.","Arda","Tanışmış olduk: "+r.ad+", "+r.meslek+". "+r.huy); return; }
            if(s.absentUntil>0&&s.absentUntil<g.data.day&&!s.returned){ s.returned=true; s.cups=0; g.data.money+=30; g.data.earned+=30; g.story.Talk(null,r.ad,"Dün gece 20. fincanı içmişim… "+(string.IsNullOrEmpty(r.gece_gizemi)||r.gece_gizemi.ToLowerInvariant()=="yok"?"Gölge gibi biri beni izledi.":r.gece_gizemi),"Arda","İyi ki döndün. Bu sefer kahve benden, hatta bu 30 coin sana teşekkür."); return; }
            if(mem!=null&&s.bond>=3&&!s.memory1){ s.memory1=true; g.data.money+=25; g.data.earned+=25; g.story.Talk(null,r.ad,mem[0],"Arda","Bunu bilmiyordum. Anlattığın için sağ ol."); g.story.AddJournal("anı_"+r.id+"1",r.ad+" · anı",mem[0]); g.Tell(r.ad+" bir anı paylaştı · +25 coin",true); return; }
            if(mem!=null&&mem.Length>1&&s.bond>=6&&!s.memory2){ s.memory2=true; g.data.money+=50; g.data.earned+=50; g.story.Talk(null,r.ad,mem[1],"Arda","Dedem herkese bir şey bırakmış. Bana da en çok sizi bırakmış."); g.story.AddJournal("anı_"+r.id+"2",r.ad+" · sır",mem[1]); g.Tell(r.ad+" derin bir sırrı paylaştı · +50 coin",true); }
        }
        public static void Bond(CafeGame g,Resident r,bool good){ var s=State(g,r); s.visits++; s.cups++; if(good)s.bond++; }
        // Borç defteri: 19 fincanda uyarı, 20. fincan gölgeye çevirir; reddetmek sayacı düşürür ama bağı zedeler
        public static bool AtLimit(CafeGame g,Resident r){ return CafeStory.Active&&g.data.day>=6&&State(g,r).cups>=19; }
        public static void Refuse(CafeGame g,Resident r){ var s=State(g,r); s.refused++; s.cups=Mathf.Max(0,s.cups-3); s.bond=Mathf.Max(0,s.bond-1); g.data.reputation=Mathf.Max(0,g.data.reputation-1); g.Tell(r.ad+" fincanı alamadı, gücendi ama güvende. Fincan sayacı düştü. (Bağ -1, itibar -1)",true); }
        public static void Overdrawn(CafeGame g,Resident r){ var s=State(g,r); s.goneDay=g.data.day; s.absentUntil=g.data.day+2; s.returned=false; s.cups=0; g.Tell(r.ad+" 20. fincanı içti. Bu gece gölge olarak dönecek. 3 gün gelmeyecek.",true); }
        public static string[] LedgerPages(CafeGame g)
        {
            var list=new List<string>(); string page=""; int n=0;
            foreach(var r in All){ var s=State(g,r); if(!s.met)continue; page+=(n>0?"\n":"")+r.ad+" · "+Mathf.Min(20,s.cups)+"/20 fincan"+(s.absentUntil>=g.data.day?" · gölge":""); if(++n%4==0){list.Add(page);page="";} }
            if(page!="")list.Add(page); if(list.Count==0)list.Add("Henüz kimseyle tanışmadın. Defter boş."); return list.ToArray();
        }
        // 3. günden itibaren her sabah biri 20. fincanı içer, ertesi gün gelmez; gölge o kişinin siluetine bürünür
        public static Resident LoseOne(CafeGame g)
        {
            if(g.data.day<3)return null; Resident best=null; foreach(var r in All){ var s=State(g,r); if(s.goneDay>0)continue; if(!s.met)continue; if(best==null||r.kahve_bagimliligi>best.kahve_bagimliligi)best=r; }
            if(best!=null){ var s=State(g,best); s.goneDay=g.data.day; s.absentUntil=g.data.day; s.returned=false; } return best;
        }
        public static Resident GoneToday(CafeGame g){ foreach(var s in g.data.residents)if(s.goneDay==g.data.day){ foreach(var r in All)if(r.id==s.id)return r; } return null; }
        public static Resident Pick(CafeGame g,List<int> choices)
        {
            if(g.data.day!=visitDay){visitDay=g.data.day;visited.Clear();}
            int hour=g.expansion?g.expansion.Hour:8;
            foreach(var r in All){ if(visited.Contains(r.id)||r.geldigi_saat>hour||State(g,r).absentUntil>=g.data.day&&State(g,r).absentUntil>0)continue; int idx=RecipeIndex(r.siparis); if(idx<0||!choices.Contains(idx))continue; visited.Add(r.id); return r; }
            return null;
        }
        // Replik havuzu: normal replikler + (2. günden sonra) haber tepkisi + (3. günden sonra, gece ipucu varsa) gece gizemi. Sipariş numarasıyla döner.
        public static string Line(CafeGame g,Resident r,int n)
        {
            var pool=new List<string>(); if(r.replikler!=null)pool.AddRange(r.replikler);
            if(g.data.day>=2&&!string.IsNullOrEmpty(r.haber_tepkisi))pool.Add(r.haber_tepkisi);
            if(g.data.day>=3&&!string.IsNullOrEmpty(r.gece_gizemi)&&r.gece_gizemi.ToLowerInvariant()!="yok")pool.Add(r.gece_gizemi);
            if(pool.Count==0)return "...";
            // gün ilerledikçe yeni satırlar daha sık çıksın: sipariş numarası + gün ile karıştır
            return pool[Mathf.Abs(n+g.data.day*3+r.id.Length)%pool.Count];
        }
        public static int RecipeIndex(string name){for(int i=0;i<Economy.Recipes.Length;i++)if(Economy.Recipes[i].name==name)return i;return -1;}
        // Mixamo gövdeleri (Resources/CafeMixamo): sakin → model. Aynı model iki kişide kullanılırsa kıyafet rengiyle hafifçe ayrılır.
        static readonly Dictionary<string,string> Model=new Dictionary<string,string>{
            {"ayse_ogretmen","Louise"},{"can_tasarimci","Remy"},{"elif_anne","Kate"},{"mehmet_insaat","James"},{"elif","Megan"},{"ahmet","Leonard"},
            {"zeynep","Amy"},{"mehmet","Adam"},{"elena","Lola"},{"memur_ahmet","Leonard"},{"balikci_mehmet","PeasantMan"},{"komsu_zeynep","PeasantGirl"}
        };
        public static string ModelName(Resident r){ return Model.TryGetValue(r.id,out var n)?n:"Olivia"; }
        public static GameObject Mixamo(Resident r){ return Model.TryGetValue(r.id,out var n)?Resources.Load<GameObject>("CafeMixamo/"+n):null; }
        public static RuntimeAnimatorController MixamoController(){ return Resources.Load<RuntimeAnimatorController>("CafeMixamo/CafeMixamo"); }
        public static void DressMixamo(GameObject avatar,Transform root,Resident r)
        {
            ColorUtility.TryParseHtmlString(r.gorunum.ust_renk_hex,out var shirt); var tint=Color.Lerp(Color.white,shirt,.3f);
            foreach(var rend in avatar.GetComponentsInChildren<Renderer>()){var mats=rend.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]){mats[i]=new Material(mats[i]);if(mats[i].HasProperty("_BaseColor"))mats[i].SetColor("_BaseColor",tint);}rend.sharedMaterials=mats;}
            float height=Mathf.Clamp(r.gorunum.boy_cm,140,200)/175f,girth=r.gorunum.yapi=="iri"?1.1f:r.gorunum.yapi=="ince"?.94f:1f;
            root.localScale=new Vector3(girth*height,height,girth*height);
        }
        public static void Dress(CafeGame g,GameObject avatar,Transform root,Resident r)
        {
            var tex=Resources.Load<Texture2D>("CafeResidents/"+r.id);
            if(tex)foreach(var rend in avatar.GetComponentsInChildren<Renderer>()){var mats=rend.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]){mats[i]=new Material(mats[i]);mats[i].SetTexture("_BaseMap",tex);mats[i].mainTexture=tex;}rend.sharedMaterials=mats;}
            float height=Mathf.Clamp(r.gorunum.boy_cm,140,200)/175f,girth=r.gorunum.yapi=="iri"?1.14f:r.gorunum.yapi=="ince"?.92f:1f;
            root.localScale=new Vector3(girth*height,height,girth*height);
        }
        public static GameObject Prefab(CafeGame g,string gender)
        {
            foreach(var p in g.customerPrefabs)if(p&&p.name.ToLowerInvariant().Contains("female")==(gender=="kadin"))return p;
            return g.customerPrefabs[0];
        }
    }
}
