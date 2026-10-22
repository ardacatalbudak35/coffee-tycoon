using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CafeLife
{
    [Serializable] public sealed class CafeDayRecord
    { public int day,income,expenses,served,debt; public int Profit=>income-expenses; }
    [Serializable] public sealed class CafeExpansionData
    {
        public int schema=1,price=100,dayIncome,dayExpenses,dayServed,debt,dailyRewardDay,achievements,visits,cleaned,tutorial;
        public float cleanliness=100,machineDirt,stamina=100,fov=72,music=.5f,effects=.6f;
        public bool open=true,staffOn=true;
        public int theme,wall,layout,light=1;
        public int[] loyalty=new int[12];
        public List<CafeDayRecord> history=new List<CafeDayRecord>();
        public List<string> reviews=new List<string>();
    }
    public sealed class CafeExpansion : MonoBehaviour
    {
        public CafeGame game;
        public CafeExpansionData Data=>game.data.expansion;
        public float clock,foamTime,pourTime,artTime=-1,brewMeter=-1;
        public int lastSavedFrame;
        CupWork foamCup;
        public string saveStatus="Kayıt hazır";
        public readonly List<CafeLitter> litter=new List<CafeLitter>();
        public bool Rainy=>game.data.day%3==0;
        public int Hour=>8+Mathf.FloorToInt(game.data.dayTime/300f*14);
        public bool Rush=>(Hour>=8&&Hour<10)||(Hour>=12&&Hour<14)||(Hour>=17&&Hour<19);
        public float DemandMultiplier=>(Rush?.65f:1f)*Mathf.Lerp(.85f,1.3f,(Data.price-80)/60f);
        public static readonly string[] Names={"Deniz","Ece","Arda","Mert","Selin","Can","Duru","Eren","Ada","Bora","İpek","Ali"};
        public void Setup()
        {
            if(game.data.expansion==null)game.data.expansion=new CafeExpansionData();
            Normalize(Data); clock=0; game.player.view.fieldOfView=Data.fov;
            gameObject.AddComponent<CafeCityLife>().Setup(this);
            MakeStation("Süt köpürtme","foam",new Vector3(-2.8f,1.2f,CafeFocusedMode.Enabled?-5.5f:3.5f),new Color(.65f,.72f,.7f));
            MakeStation("Latte art tezgâhı","art",new Vector3(-2.15f,1.2f,CafeFocusedMode.Enabled?-5.5f:3.5f),new Color(.7f,.48f,.25f));
            MakeStation("Makine temizliği","clean_machine",new Vector3(-.6f,1.3f,-5.25f),new Color(.2f,.55f,.45f),.16f);
            ApplyDecor();
        }
        public static void Normalize(CafeExpansionData d)
        {
            if(d.loyalty==null||d.loyalty.Length!=12)d.loyalty=new int[12];
            if(d.history==null)d.history=new List<CafeDayRecord>();if(d.reviews==null)d.reviews=new List<string>();
            d.price=Mathf.Clamp(d.price,80,140);d.cleanliness=Mathf.Clamp(d.cleanliness,0,100);d.machineDirt=Mathf.Clamp(d.machineDirt,0,100);d.fov=Mathf.Clamp(d.fov,60,95);
            d.music=Mathf.Clamp01(d.music);d.effects=Mathf.Clamp01(d.effects);d.stamina=Mathf.Clamp(d.stamina,0,100);d.debt=Mathf.Max(0,d.debt);
        }
        void MakeStation(string title,string action,Vector3 pos,Color color,float size=.3f)
        {
            string model=action=="foam"?"Jar":action=="art"?"Tray":action=="clean_machine"?"bottle":"crate";
            var obj=game.world.Model(model,pos,size,0,false);obj.name=title;
            var marker=new GameObject(title+" etkileşim");marker.transform.SetParent(obj.transform,false);marker.layer=8;
            var sphere=marker.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.4f;
            var target=obj.AddComponent<CafeTarget>();target.kind=action;
        }
        void Update()
        {
            if(!game || game.data==null || game.Paused)return;
            Tick(Time.deltaTime);
        }
        public void Tick(float dt)
        {
            clock+=dt;
            if(foamTime>0){if(foamCup!=null)foamCup.milkTemperature=Mathf.Lerp(20,65,1-foamTime/2.5f);foamTime=Mathf.Max(0,foamTime-dt);if(foamTime==0 && game.cup!=null && game.cup==foamCup){game.cup.foamed=true;game.cup.milkTemperature=65;game.Tell("Süt köpüğü hazır. Kalite bonusu +3 coin.",true);game.UpdateHand();}}
            if(pourTime>0)pourTime-=dt;
            if(artTime>=0){artTime+=dt;if(artTime>4){artTime=-1;game.Tell("Latte art süresi kaçtı. Yeniden deneyebilirsin.");}}
            if(brewMeter>=0)brewMeter+=dt;
            foreach(var drink in game.shelf.ToArray()){drink.age+=dt; if(drink.age>90)drink.good=false;}
            Data.cleanliness=Mathf.Max(0,Data.cleanliness-dt*.015f);
        }
        public float PatienceMultiplier(int persona)
        { return (persona==1?1.4f:persona==2?.65f:1f)*Mathf.Lerp(1.4f,.85f,Data.cleanliness/100f)*Mathf.Lerp(.9f,1.3f,(Data.price-80)/60f); }
        public int PickRecipe()
        {
            bool cold=UnityEngine.Random.value<(Rainy?.2f:.6f);
            var available=CafeFocusedMode.Enabled?CafeFocusedMode.AvailableRecipes(game.data.served):Enumerable.Range(0,Economy.Recipes.Length).ToArray();
            var options=available.Where(i=>Economy.Recipes[i].cold==cold).ToArray();if(options.Length==0)options=available;
            return options[UnityEngine.Random.Range(0,options.Length)];
        }
        public void Identify(CafeCustomer c)
        { c.identity=UnityEngine.Random.Range(0,Names.Length);c.persona=UnityEngine.Random.Range(0,3);c.vip=UnityEngine.Random.value<.12f;c.recipe=PickRecipe();Data.visits++; }
        public int CompleteSale(CafeCustomer c)
        {
            var craft=GetComponent<CafeHundred>();
            int money=game.data.money;int payment=Economy.Sale(game.data,c.recipe,c.goodDrink);
            int bonus=c.goodDrink?(c.qualityBonus+(c.vip?8:0)+(Data.loyalty[c.identity]>=3?4:0)):0;
            int actual=Mathf.Max(1,Mathf.RoundToInt(payment*Data.price/100f*(game.data.bookRead?1.1f:1f))+bonus);   // Hasan'ın defteri: gizli dokunuş +%10
            game.data.money=money+actual;game.data.earned+=actual-payment;Data.dayIncome+=actual;Data.dayServed++;Data.loyalty[c.identity]++;
            AddReview(Names[c.identity]+": "+c.feedback+(c.goodDrink?" ★★★★★":" ★★"));
            if(c.goodDrink)Data.tutorial|=16;
            Data.cleanliness=Mathf.Max(0,Data.cleanliness-1.5f);if(craft)actual+=craft.Sold(c);return actual;
        }
        public void AddReview(string text){Data.reviews.Insert(0,text);if(Data.reviews.Count>8)Data.reviews.RemoveAt(8);}
        public void Expense(int amount){Data.dayExpenses+=Mathf.Max(0,amount);}
        public bool Pay(int cost)
        { if(!Economy.Spend(game.data,cost)){game.Tell("Yetersiz para: "+cost+" coin gerekli.");return false;}Expense(cost);return true; }
        public void EndDay(int day)
        {
            int bill=20+(game.data.staff>0&&Data.staffOn?15+game.data.baristaLevel*5:0);
            int paid=Mathf.Min(game.data.money,bill);game.data.money-=paid;Data.debt+=bill-paid;Expense(bill);
            Data.history.Insert(0,new CafeDayRecord{day=day,income=Data.dayIncome,expenses=Data.dayExpenses,served=Data.dayServed,debt=Data.debt});if(Data.history.Count>14)Data.history.RemoveAt(14);
            Data.dayIncome=Data.dayExpenses=Data.dayServed=0;
            game.Tell("Gün sonu: kira ve maaş "+bill+" coin. Borç: "+Data.debt+". Rapor: Tab.");
        }
        public void DailyReward()
        {
            if(Data.dailyRewardDay==game.data.day){game.Tell("Bugünkü ödül alındı.");return;}
            if(Data.dayServed<8){game.Tell("Günlük hedef: "+Data.dayServed+" / 8 servis.");return;}
            Data.dailyRewardDay=game.data.day;game.data.money+=100;Data.dayIncome+=100;game.Tell("Günlük servis ödülü +100 coin!",true);game.Save();
        }
        public bool AchievementReady(int i)=>i==0?game.data.served>=10:i==1?Data.cleaned>=5:i==2?Data.visits>=25:i==3?game.data.money>=1000:Data.loyalty.Any(n=>n>=5);
        public void ClaimAchievement(int i)
        {if(i<0||i>=5)return;int bit=1<<i;if((Data.achievements&bit)!=0||!AchievementReady(i)){game.Tell("Başarım henüz hazır değil veya ödülü alındı.");return;}Data.achievements|=bit;game.data.money+=75;Data.dayIncome+=75;game.Tell("Başarım ödülü +75 coin!",true);game.Save();}
        public bool Action(string action)
        {
            switch(action)
            {
                case "foam":
                    if(game.cup==null||!game.cup.hasMilk||game.cup.coldCup||game.cup.lidded||game.cup.foamed){game.Tell("Köpük için sütlü, sıcak ve açık bir bardak gerekli.");return true;}
                    if(foamTime<=0){foamCup=game.cup;foamTime=2.5f;game.Tell("Süt köpürtülüyor...");}return true;
                case "art":
                    if(game.cup==null||!game.cup.foamed||!game.cup.brewed||game.cup.lidded||game.cup.art){game.Tell("Latte art için köpüklü, demlenmiş ve açık bardak gerekli.");return true;}
                    var pour=GetComponent<CafeLattePour>();if(!pour)pour=gameObject.AddComponent<CafeLattePour>();pour.Begin(game);return true;
                case "clean_machine":if(Pay(5)){Data.machineDirt=0;game.Tell("Makine temizlendi. Demleme hızı düzeldi.",true);game.Save();}return true;
                case "clean_cafe":game.Tell("Kafe tek tuşla temizlenmez: yerdeki tozları paspasla, masaları bezle sil (ikisi de merdiven altında).");return true;
                case "taxi":game.OpenOperations("travel");return true;
                case "bench":ToggleSit();return true;
                case "clear_stale":ClearStale();return true;
                default:return false;
            }
        }
        public static float TimingScore(float seconds)=>Mathf.Clamp01(1-Mathf.Abs(seconds-2)/2);
        public void CancelWork(){foamTime=0;foamCup=null;artTime=-1;brewMeter=-1;}
        public void ResetRun(){CancelWork();clock=0;seated=false;game.player.view.transform.localPosition=Vector3.up*1.65f;game.player.view.fieldOfView=Data.fov;foreach(var item in litter.ToArray())if(item)Destroy(item.gameObject);litter.Clear();ApplyDecor();game.ApplyAudio();}
        public void BrewStarted(){Data.machineDirt=Mathf.Min(100,Data.machineDirt+3);brewMeter=0;game.brewTime*=1+Data.machineDirt/150f;game.brewTotal=game.brewTime;}
        public bool StopBrew()
        {
            if(game.cup==null||game.brewTime<=0)return false;
            float progress=1-game.brewTime/game.brewTotal;
            game.cup.yield=progress*game.cup.dose*2.8f;game.cup.extraction=progress*game.brewTotal*GetComponent<CafeHundred>().BrewClockRate;
            game.cup.perfectBrew=CafeHundred.Score(game.cup)>.8f;GetComponent<CafeHundred>()?.FinishBrew();
            game.brewTime=0;game.EspressoShotDone(game.cup);brewMeter=-1;game.UpdateHand();
            game.Tell(game.cup.dose.ToString("0")+" g → "+game.cup.yield.ToString("0")+" g · "+CafeHundred.Taste(game.cup),game.cup.perfectBrew);return true;
        }
        public void Ingredient(string kind)
        {pourTime=.6f;Data.tutorial|=kind.StartsWith("cup")?1:kind=="grinder"?2:kind=="milk"||kind=="water"?4:kind.StartsWith("lid")?8:0;}
        public void ClearStale()
        {int n=0;foreach(var d in game.shelf.ToArray())if(d.owner==null&&d.age>90){game.shelf.Remove(d);if(d.visual)Destroy(d.visual);n++;}game.ReflowShelf();game.Tell(n+" bayat içecek temizlendi.");}
        public string LowStock()
        {var items=new List<string>();var d=game.data;if(d.beans<5)items.Add("çekirdek");if(d.milk<3)items.Add("süt");if(d.ice<3)items.Add("buz");if(d.cupsHot<3||d.cupsCold<3)items.Add("bardak");if(d.lidsHot<3||d.lidsCold<3)items.Add("kapak");return string.Join(", ",items);}
        public bool Supply(string kind)
        {
            if(kind!="emergency"&&kind!="beans"&&kind!="milk"&&kind!="packaging"&&kind!="syrup")return false;
            int cost=kind=="emergency"?60:kind=="beans"?25:kind=="milk"?20:kind=="syrup"?22:18;if(!Pay(cost))return false;
            var d=game.data;if(kind=="beans")d.depotBeans+=35;else if(kind=="milk"){d.depotMilk+=25;d.depotIce+=15;}else if(kind=="syrup"){d.depotCaramel+=10;d.depotVanilla+=10;}else if(kind=="packaging"){d.depotCupsHot+=25;d.depotCupsCold+=25;d.depotLidsHot+=25;d.depotLidsCold+=25;}else {d.beans+=10;d.milk+=8;d.ice+=8;d.cupsHot+=8;d.cupsCold+=8;d.lidsHot+=8;d.lidsCold+=8;}
            game.Tell(kind=="emergency"?"Acil stok doğrudan bara ulaştı.":"Paket depoya ulaştı; arka raftan bara taşı.",true);game.Save();return true;
        }
        public void DebtPayment(){int amount=Mathf.Min(game.data.money,Data.debt);game.data.money-=amount;Data.debt-=amount;game.Tell(amount+" coin borç ödendi.");game.Save();}
        public void SetPrice(int delta){Data.price=Mathf.Clamp(Data.price+delta,80,140);game.Save();}
        public string Direction(CafeVenue venue)
        {if(!venue)return "";Vector3 delta=venue.transform.position-game.player.transform.position;delta.y=0;float angle=Vector3.SignedAngle(game.player.transform.forward,delta,Vector3.up);return Mathf.Abs(angle)<25?"↑ İleri":Mathf.Abs(angle)>145?"↓ Arkanda":angle>0?"→ Sağında":"← Solunda";}
        public void SetDecor(string kind)
        {if(!Pay(15))return;if(kind=="theme")Data.theme=(Data.theme+1)%3;else if(kind=="wall")Data.wall=(Data.wall+1)%5;else if(kind=="light")Data.light=(Data.light+1)%3;else Data.layout=1-Data.layout;ApplyDecor();game.Save();}
        public void ApplyDecor()
        {
            var style=game.GetComponent<CafeDecorProgression>();if(style){style.Apply();return;}
            Color[] walls={new Color(.8f,.73f,.6f),new Color(.5f,.7f,.65f),new Color(.72f,.5f,.4f),new Color(.55f,.58f,.75f),new Color(.9f,.87f,.77f)};
            foreach(var r in game.world.GetComponentsInChildren<Renderer>())
            {
                if(r.name=="Arka duvar"||r.name=="Sol duvar"||r.name=="Sağ duvar")r.sharedMaterial=game.world.ColorMaterial(walls[Data.wall%5]);
                if(r.name=="Kahve barı - ahşap gövde"||r.name=="Ahşap ön panel")r.sharedMaterial=game.world.ColorMaterial(Data.theme==0?new Color(.24f,.12f,.07f):Data.theme==1?new Color(.15f,.27f,.23f):new Color(.45f,.35f,.25f));
            }
            foreach(var light in game.world.GetComponentsInChildren<Light>())if(light.type==LightType.Point && light.transform.position.z>-12)light.color=Data.light==0?new Color(1,.65f,.3f):Data.light==1?new Color(1,.84f,.6f):new Color(.8f,.9f,1);
            for(int i=0;i<12;i++){var root=game.world.transform.Find("Masa alanı "+i);if(root)root.position=Vector3.forward*(Data.layout==0?0:(i%2==0?.3f:-.3f));}
            var polish=game.GetComponent<CafePolish>();if(polish)polish.ApplyPalette();
        }
        Vector3 sitReturn; bool seated;
        public bool Seated=>seated;
        public void ToggleSit()
        {var p=game.player; p.body.enabled=false;if(seated)p.transform.position=sitReturn;else{sitReturn=p.transform.position;Vector3 seat=p.target?p.target.transform.position:p.transform.position;seat.y=.35f;p.transform.position=seat;}p.body.enabled=true;seated=!seated;p.view.transform.localPosition=Vector3.up*(seated?1.05f:1.65f);game.Tell(seated?"Bankta dinleniyorsun. Kalkmak için E.":"Ayağa kalktın.");}
        public void Travel(CafeVenue venue)
        {int cost=15;if(!venue||!Pay(cost))return;game.player.body.enabled=false;game.player.transform.position=venue.transform.position+Vector3.forward*2;game.player.body.enabled=true;game.trackedVenue=venue;game.Continue();game.Save();}
        public void TravelHome()
        {if(!Pay(15))return;game.player.body.enabled=false;game.player.transform.position=new Vector3(0,.08f,-12.5f);game.player.body.enabled=true;game.trackedVenue=null;game.Continue();game.Save();}
        public float Stamina(bool sprint,bool moving,float dt)
        {Data.stamina=Mathf.Clamp(Data.stamina+(sprint&&moving?-22f:16f)*dt,0,100);return Data.stamina;}
    }
}
