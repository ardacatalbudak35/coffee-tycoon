using System;
using System.IO;
using UnityEngine;

namespace CafeLife
{
    [Serializable] public class CafeBranchData
    {
        public string name="Boş kafe";
        public bool owned;
        public int baristaLevel,espressoLevel=1,paintLevel,capacity=20;
    }
    [Serializable] public class SaveData
    {
        public int interiorStyle;
        public int version=3, money=160, xp, machine=1, tables=6, decor,baristaLevel,espressoLevel=1,paintLevel;
        public CafeBranchData[] branches={new CafeBranchData{name="Arda's Coffee",owned=true,capacity=12},new CafeBranchData{name="Küçük Meydan Kafesi",capacity=24},new CafeBranchData{name="Sahil Kahvecisi",capacity=38},new CafeBranchData{name="Merkez Roastery",capacity=56},new CafeBranchData{name="Grand Avenue Café",capacity=80}};
        public int beans=20, milk=12, ice=12, cupsHot=12, cupsCold=12, lidsHot=12, lidsCold=12;
        public int caramel=10, vanilla=10, depotBeans=30, depotMilk=20, depotIce=20;
        public int depotCupsHot=20, depotCupsCold=20, depotLidsHot=20, depotLidsCold=20, depotCaramel=15, depotVanilla=15;
        public int unlocked=32767, served, missed, day=1, earned, tutorial, quest, staff, outfit;
        public bool noteRead,shadowSeen,jumpSeen,bookRead,finale,ledgerRead; public int nermin,choice1,choice2,choice3,dayStartServed,dayStartMoney,dayStartMet; public System.Collections.Generic.List<JournalEntry> journal=new System.Collections.Generic.List<JournalEntry>(); public System.Collections.Generic.List<ResState> residents=new System.Collections.Generic.List<ResState>();
        public int storyStage, menuMask=1537; // storyStage -1: eski kayıt, hikaye kapalı; menuMask: eski alan (menü artık güne göre CafeStory.MaskForDay ile belirlenir)
        public float reputation=80, dayTime, volume=.55f, sensitivity=.10f;
        public CafeHundredData hundred=new CafeHundredData();
        public CafeExpansionData expansion=new CafeExpansionData(); public int quality=1; public int districtContract=-1,contractStart; public int[] venueVisitDay=new int[64];
    }
    public sealed class Recipe
    {
        public readonly string name;
        public readonly int price, level, unlock;
        public readonly bool cold, milk, water, filter;
        public readonly string syrup;
        public readonly int shots;   // espresso shot sayısı (filtre kahvede 0)
        public Recipe(string name,int price,bool cold,bool milk,bool water,string syrup="",int shots=1,bool filter=false)
        { this.name=name; this.price=price; this.cold=cold; this.milk=milk; this.water=water; this.syrup=syrup; this.shots=shots; this.filter=filter; level=1; unlock=0; }
        public int BeansNeeded=>Mathf.Max(1,shots+(filter?1:0));
    }
    public static class Economy
    {
        public static readonly Recipe[] Recipes={
            new Recipe("Espresso",18,false,false,false),
            new Recipe("Latte",30,false,true,false),
            new Recipe("Karamel Latte",38,false,true,false,"caramel"),
            new Recipe("Vanilyalı Latte",38,false,true,false,"vanilla"),
            new Recipe("Americano",26,false,false,true),
            new Recipe("Buzlu Latte",35,true,true,false),
            new Recipe("Buzlu Karamel Latte",43,true,true,false,"caramel"),
            new Recipe("Buzlu Vanilyalı Latte",43,true,true,false,"vanilla"),
            new Recipe("Buzlu Americano",31,true,false,true),
            new Recipe("Buzlu Espresso",25,true,false,false),
            new Recipe("Filtre Kahve",16,false,false,false,"",0,true),       // 10 · yalnızca filtre makinesi
            new Recipe("Flat White",38,false,true,false,"",2),               // 11 · 2 shot + süt
            new Recipe("Red Eye",36,false,false,false,"",1,true),            // 12 · filtre kahve + 1 shot
            new Recipe("Black Eye",44,false,false,false,"",2,true),          // 13 · filtre kahve + 2 shot
            new Recipe("Dead Eye",52,false,false,false,"",3,true)            // 14 · filtre kahve + 3 shot
        };
        public static int Level(SaveData d) => 1+d.xp/100;
        public static int BranchIncome(CafeBranchData b)
        { return b==null||!b.owned||b.baristaLevel<3?0:Mathf.RoundToInt((4+b.capacity*.22f+b.espressoLevel*3f)*b.baristaLevel*.55f); }
        public static bool Spend(SaveData d,int amount) { if(amount<0||d.money<amount) return false; d.money-=amount; return true; }
        public static bool HasStock(SaveData d,int recipe)
        {
            if(recipe<0||recipe>=Recipes.Length) return false;
            var r=Recipes[recipe]; return d.beans>=r.BeansNeeded && (!r.milk||d.milk>0) && (!r.cold||d.ice>0) &&
                (r.cold?d.cupsCold>0&&d.lidsCold>0:d.cupsHot>0&&d.lidsHot>0) &&
                (r.syrup!="caramel"||d.caramel>0) && (r.syrup!="vanilla"||d.vanilla>0);
        }
        public static bool StartRecipe(SaveData d,int recipe)
        {
            if(!HasStock(d,recipe)) return false;
            var r=Recipes[recipe]; d.beans-=r.BeansNeeded; if(r.milk)d.milk--; if(r.cold)d.ice--; return true;
        }
        public static int Sale(SaveData d,int recipe,bool good=true)
        {
            int basePrice=Recipes[recipe].price+(Mathf.Max(1,d.espressoLevel)-1)*2;
            int payment=good?basePrice:Mathf.Max(1,basePrice/2);
            d.money+=payment; d.earned+=payment; d.served++; d.xp+=good?25:10;
            d.reputation=Mathf.Clamp(d.reputation+(good?2:-5),0,100);
            return payment;
        }
        public static void Save(SaveData d,string path)
        {
            string temp=path+".tmp"; Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(temp,JsonUtility.ToJson(d,true));
            if(File.Exists(path)) File.Copy(path,path+".bak",true);
            File.Copy(temp,path,true); File.Delete(temp);
        }
        public static SaveData Load(string path)
        {
            foreach(string file in new[]{path,path+".bak"}) try
            {
                if(!File.Exists(file)) continue;
                string json=File.ReadAllText(file); var d=JsonUtility.FromJson<SaveData>(json);
                if(d==null || (d.version!=1&&d.version!=2&&d.version!=3)) continue;
                if(d.version==1) { var fresh=new SaveData(); fresh.money=d.money; fresh.xp=d.xp; fresh.machine=d.machine;
                    fresh.tables=d.tables; fresh.decor=d.decor; fresh.beans=d.beans; fresh.milk=d.milk; fresh.ice=d.ice;
                    fresh.served=d.served; fresh.missed=d.missed; fresh.day=d.day; fresh.earned=d.earned;
                    fresh.quest=d.quest; fresh.staff=d.staff; fresh.outfit=d.outfit; fresh.reputation=d.reputation;
                    fresh.dayTime=d.dayTime; fresh.volume=d.volume; fresh.sensitivity=d.sensitivity; fresh.quality=d.quality; d=fresh; }
                if(!json.Contains("\"districtContract\"")) { d.districtContract=-1; d.contractStart=0; }
                if(!json.Contains("\"menuMask\"")) { d.storyStage=-1; d.menuMask=32767; }
                if(d.hundred==null)d.hundred=new CafeHundredData();CafeHundred.Normalize(d.hundred);
                if(d.expansion==null)d.expansion=new CafeExpansionData(); CafeExpansion.Normalize(d.expansion);
                if(d.venueVisitDay==null || d.venueVisitDay.Length!=64) d.venueVisitDay=new int[64];
                d.interiorStyle=Mathf.Clamp(d.interiorStyle,0,3);
                d.money=Mathf.Max(0,d.money); d.machine=Mathf.Clamp(d.machine,1,5); d.tables=Mathf.Clamp(d.tables,6,12);
                d.espressoLevel=Mathf.Clamp(d.espressoLevel,1,10); d.baristaLevel=Mathf.Clamp(d.baristaLevel,0,5); d.paintLevel=Mathf.Clamp(d.paintLevel,0,8);
                if(d.branches==null || d.branches.Length!=5) d.branches=new SaveData().branches;
                var defaults=new SaveData().branches;
                for(int i=0;i<d.branches.Length;i++)
                {
                    if(d.branches[i]==null) d.branches[i]=defaults[i];
                    d.branches[i].capacity=defaults[i].capacity;
                    if(string.IsNullOrWhiteSpace(d.branches[i].name)) d.branches[i].name=defaults[i].name;
                    d.branches[i].baristaLevel=Mathf.Clamp(d.branches[i].baristaLevel,0,5);
                    d.branches[i].espressoLevel=Mathf.Clamp(d.branches[i].espressoLevel,1,10);
                    d.branches[i].paintLevel=Mathf.Clamp(d.branches[i].paintLevel,0,8);
                }
                d.branches[0].owned=true; d.branches[0].baristaLevel=d.baristaLevel; d.branches[0].espressoLevel=d.espressoLevel; d.branches[0].paintLevel=d.paintLevel;
                d.reputation=Mathf.Clamp(d.reputation,0,100); d.unlocked=32767; d.version=3; return d;
            } catch(Exception) { }
            return new SaveData();
        }
    }
}



