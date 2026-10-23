using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CafeLife
{
    public sealed partial class CafeUI
    {
        public string operationSection="business";
        Text expansionStatus,checklist,timing;Image staminaFill;
        RectTransform expansionHud; int travelPage;
        void BuildExpansionHud()
        {
            expansionHud=Rect("İşletme göstergeleri",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var left=Panel(expansionHud,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-235),new Vector2(330,-102),panel);
            expansionStatus=Text(left,"",16,TextAnchor.UpperLeft);expansionStatus.rectTransform.offsetMin=new Vector2(12,10);expansionStatus.rectTransform.offsetMax=new Vector2(-12,-10);
            var preparation=Panel(expansionHud,new Vector2(0,0),new Vector2(0,0),new Vector2(18,100),new Vector2(330,330),panel);
            checklist=Text(preparation,"",17,TextAnchor.UpperLeft);checklist.rectTransform.offsetMin=new Vector2(12,12);checklist.rectTransform.offsetMax=new Vector2(-12,-12);
            var stamina=Panel(expansionHud,Vector2.zero,Vector2.zero,new Vector2(18,30),new Vector2(230,50),panel);
            var fill=Panel(stamina,Vector2.zero,Vector2.one,new Vector2(3,3),new Vector2(-3,-3),new Color(.4f,.7f,.55f));staminaFill=fill.GetComponent<Image>();staminaFill.type=Image.Type.Filled;staminaFill.fillMethod=Image.FillMethod.Horizontal;
            var shortkeys=Label(expansionHud,Vector2.zero,Vector2.zero,new Vector2(18,55),new Vector2(350,83),14,TextAnchor.MiddleLeft);shortkeys.text="TAB Yönetim   /   R Reçeteler   /   M Cadde ligi";
            timing=Label(expansionHud,new Vector2(.3f,.5f),new Vector2(.7f,.5f),new Vector2(0,-120),new Vector2(0,-20),22,TextAnchor.MiddleCenter);
            left.gameObject.SetActive(false);preparation.gameObject.SetActive(false);stamina.gameObject.SetActive(false);shortkeys.gameObject.SetActive(false);
            expansionHud.SetSiblingIndex(menu.GetSiblingIndex());
        }
        void UpdateExpansionHud()
        {
            if(!game.expansion || !expansionHud)return;
            var x=game.expansion;var d=x.Data;expansionHud.gameObject.SetActive(!game.Paused);staminaFill.fillAmount=d.stamina/100;
            expansionStatus.transform.parent.gameObject.SetActive(false);
            string warning=x.LowStock();
            expansionStatus.text=(d.open?"<color=#9EE0AE>AÇIK</color>":"<color=#EEAE88>KAPALI</color>")+" · "+x.Hour.ToString("00")+":"+Mathf.FloorToInt(game.data.dayTime/300f*14*60%60).ToString("00")+" · "+(x.Rainy?"Yağmurlu":"Açık hava")+(x.Rush?" · YOĞUN":"")+"\nTemizlik %"+Mathf.RoundToInt(d.cleanliness)+" · Makine kir %"+Mathf.RoundToInt(d.machineDirt)+"\nFiyat %"+d.price+" · "+x.saveStatus+(warning!=""?"\n<color=#F0BA75>Azalan stok: "+warning+"</color>":"");
            if((d.tutorial&31)!=31){string[] starter={"Bardak al · R: tarifler","Çekirdek öğüt","Süt veya su ekle","Kapak tak","Doğru içeceği teslim et"};for(int i=0;i<5;i++)if((d.tutorial&(1<<i))==0){expansionStatus.text+="\nBaşlangıç: "+starter[i];break;}}
            checklist.transform.parent.gameObject.SetActive(false);
            if(game.cup!=null)
            {
                var cup=game.cup;var recipe=Economy.Recipes[cup.recipe];string text="<b>"+recipe.name+"</b>\n";
                foreach(var step in CafeGame.IdealSteps(recipe)){bool done=cup.steps.Contains(step);text+="\n"+(done?"<color=#A3DDB0>✓ ":"<color=#F0D4A3>○ ")+StepName(step)+"</color>";}
                if(cup.foamed)text+="\n✓ Süt köpüğü +3";if(cup.art)text+="\n✓ Latte art +5";if(cup.perfectBrew)text+="\n✓ Usta espresso +4";checklist.text=text;
            }
            timing.text=x.artTime>=0?"LATTE ART · Ortada E\n"+Meter(x.artTime/4f):x.foamTime>0?"SÜT KÖPÜRTÜLÜYOR · "+x.foamTime.ToString("0.0")+" sn · "+(game.cup!=null?game.cup.milkTemperature:20).ToString("0")+"°C":game.brewTime>0?game.cup.dose.ToString("0")+" g doz · "+game.cup.temperature+"°C · 9 bar · "+game.cup.yield.ToString("0")+" g\nHedef: "+(game.cup.dose*2).ToString("0")+" g · "+game.cup.extraction.ToString("0")+" sn · E: akışı kes":"";
            brewFill.color=game.cup!=null&&game.cup.yield/game.cup.dose>=1.8f&&game.cup.yield/game.cup.dose<=2.2f?new Color(.3f,.8f,.45f):new Color(.72f,.53f,.26f);
        }
        static string Meter(float value){int marker=Mathf.Clamp(Mathf.RoundToInt(value*16),0,16);string bar="";for(int i=0;i<=16;i++)bar+=i==marker?"◆":"─";return bar;}
        public static string StepName(string s)=>s=="ice"?"Buz":s=="milk"?"Süt":s=="water"?"Su":s=="espresso"?"Espresso":s=="filter"?"Filtre kahve":s=="lid"?"Kapak":s=="vanilla"?"Vanilya şurubu":s=="caramel"?"Karamel şurubu":s;
        void Op(string label,int row,bool right,UnityEngine.Events.UnityAction action)
        {
            UnityEngine.Events.UnityAction wrapped=()=>{action();if(game.Paused)ShowMenu("operations");};
            Button(menu,label,new Vector2(right?1:0,1),new Vector2(right?-580:35,-190-row*58),new Vector2(right?545:560,48),wrapped);
        }
        void OperationInfo(string content)
        {
            var label=Label(menu,new Vector2(0,0),new Vector2(1,0),new Vector2(40,62),new Vector2(-40,123),16,TextAnchor.MiddleLeft);label.text=content;
        }
        void BodyText(string content,bool right)
        {
            var card=Panel(menu,new Vector2(right?.51f:.03f,.34f),new Vector2(right?.97f:.49f,.8f),Vector2.zero,Vector2.zero,new Color(.07f,.13f,.12f,1));
            var text=Text(card,content,17,TextAnchor.UpperLeft);text.rectTransform.offsetMin=new Vector2(18,15);text.rectTransform.offsetMax=new Vector2(-18,-15);text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=17;
        }
        public void BuildOperations(RectTransform root)
        {
            string[] sections={"recipes","business","supply","reports","achievements","decor","settings","help","travel","barista","craftreport"};
            string[] labels={"TARİFLER","İŞLETME","TEDARİK","RAPOR","BAŞARIM","DEKOR","AYARLAR","YARDIM","TAKSİ","BARİSTA","USTALIK"};
            for(int i=0;i<sections.Length;i++){string section=sections[i];Button(root,labels[i],new Vector2(0,1),new Vector2(35+i*110,-115),new Vector2(104,30),()=>{operationSection=section;ShowMenu("operations");});}
            var x=game.expansion;var d=x.Data;
            if(operationSection=="barista")BuildBarista();
            else if(operationSection=="craftreport")BuildHundredReport();
            else if(operationSection=="recipes")
            {
                var open=CafeFocusedMode.AvailableRecipes(game.data.served); if(System.Array.IndexOf(open,game.selected)<0&&open.Length>0)game.selected=open[0]; int shownCount=0;
                foreach(int i in open){int recipe=i;var r=Economy.Recipes[i];Op((game.selected==i?"✓ ":"")+r.name+" · "+Mathf.RoundToInt(r.price*d.price/100f)+" coin",shownCount%8,shownCount>=8,()=>game.selected=recipe);shownCount++;}
                var chosen=Economy.Recipes[game.selected];OperationInfo("Seçili: "+chosen.name+" · "+string.Join(" → ",CafeGame.IdealSteps(chosen).Select(StepName))+"\nBardak almadan önce tarifi seç. Doğru sırayla ekle; köpük ve latte art sıcak sütlü içeceklere ekstra kazandırır.");
            }
            else if(operationSection=="business")
            {
                Op("Kafe: "+(d.open?"AÇIK · kapat":"KAPALI · aç"),0,false,()=>{d.open=!d.open;game.Save();});
                Op("Fiyatı artır: %"+d.price+" → +%10",1,false,()=>x.SetPrice(10));Op("Fiyatı azalt: %"+d.price+" → -%10",2,false,()=>x.SetPrice(-10));
                Op("Barista mesaisi: "+(d.staffOn?"AÇIK":"KAPALI"),3,false,()=>{d.staffOn=!d.staffOn;game.Save();});
                Op("Günlük hedef "+d.dayServed+" / 8 · +100 coin",4,false,()=>x.DailyReward());
                Op("Borç öde: "+d.debt+" coin",5,false,()=>x.DebtPayment());
                Op("Makineyi temizle · 5 coin",0,true,()=>x.Action("clean_machine"));Op("Kafeyi temizle · ücretsiz",1,true,()=>x.Action("clean_cafe"));Op("Bayat ürünleri kaldır",2,true,()=>x.ClearStale());Op("Mevcut yükseltmeler ve personel",3,true,()=>game.OpenShop());Op("Şube yönetimi",4,true,()=>game.OpenBranch(0));
                OperationInfo("Kafe kapalıyken yeni müşteri gelmez; mevcut siparişler devam eder. Yüksek fiyatlar sabrı ve geliş hızını azaltır.\nBarista mesaisi kapatılınca yeni iş almaz; başladığı siparişi bitirir. Gün sonunda kira 20 coin + aktif personel maaşı alınır.");
            }
            else if(operationSection=="supply")
            {
                Op("Çekirdek paketi · 35 adet / 25 coin",0,false,()=>x.Supply("beans"));Op("Süt + buz · 25 + 15 / 20 coin",1,false,()=>x.Supply("milk"));Op("Ambalaj · her türden 25 / 18 coin",2,false,()=>x.Supply("packaging"));Op("ACİL TESLİMAT · bara / 60 coin",3,false,()=>x.Supply("emergency"));Op("PARASIZ KALDIM · acil destek",4,false,()=>game.RescueStock());
                BodyText("BAR STOKLARI\n\nÇekirdek: "+game.data.beans+"\nSüt: "+game.data.milk+"\nBuz: "+game.data.ice+"\nSıcak / soğuk bardak: "+game.data.cupsHot+" / "+game.data.cupsCold+"\n\nDEPODA\nÇekirdek: "+game.data.depotBeans+" · Süt: "+game.data.depotMilk+"\nBuz: "+game.data.depotIce,true);
                OperationInfo("Normal paketler depoya teslim edilir. Arka raftaki E etkileşimiyle bara taşı. Acil teslimat doğrudan barı doldurur.");
            }
            else if(operationSection=="reports")
            {
                string history="BUGÜN\nGelir: "+d.dayIncome+" · Gider: "+d.dayExpenses+"\nNet: "+(d.dayIncome-d.dayExpenses)+" coin\nServis: "+d.dayServed+" · Borç: "+d.debt+"\n\nGEÇMİŞ GÜNLER\n";
                foreach(var day in d.history.Take(5))history+="Gün "+day.day+" · "+day.served+" servis · Net "+day.Profit+"\n";
                BodyText(history,false);BodyText("MÜŞTERİ YORUMLARI\n\n"+string.Join("\n\n",d.reviews.Take(5)),true);OperationInfo("Gelir; servis, bahşiş, şube, görev ve ödülleri kapsar. Gider; alışveriş, kira, personel ve yolculukları kapsar. Kâr = gelir − gider.");
            }
            else if(operationSection=="achievements")
            {
                string[] titles={"10 servis tamamla","5 kez temizlik yap","25 müşteri ağırla","1000 coin biriktir","Bir müdavime 5 servis yap","100 servis tamamla","İlk şubeni aç","10. güne ulaş","Bölüm 1'i bitir"};
                for(int i=0;i<titles.Length;i++){int index=i;Op(titles[i]+" · "+((d.achievements&(1<<i))!=0?"ALINDI":x.AchievementReady(i)?"ÖDÜLÜ AL +75":"DEVAM EDİYOR"),i,false,()=>x.ClaimAchievement(index));}
                OperationInfo("Her başarım ödülü bir kez alınabilir ve kayıtla korunur. Müdavimler üçüncü ziyaretten sonra iyi kahveye +4 coin verir.");
            }
            else if(operationSection=="decor")
            {
                string[] themes={"Klasik ahşap","Yeşil butik","Açık doğal"};Op("Tema: "+themes[d.theme%3]+" · değiştir / 15",0,false,()=>x.SetDecor("theme"));Op("Duvar paleti: "+(d.wall+1)+" / 5 · 15 coin",1,false,()=>x.SetDecor("wall"));Op("Işık: "+new[]{"Sıcak","Dengeli","Soğuk"}[d.light%3]+" · 15 coin",2,false,()=>x.SetDecor("light"));Op("Masa düzeni: "+(d.layout+1)+" / 2 · 15 coin",3,false,()=>x.SetDecor("layout"));OperationInfo("Seçimler hemen kafeye uygulanır ve kaydedilir. Masa düzeni koltuk hedeflerini de günceller. Her değişiklik 15 coin.");
            }
            else if(operationSection=="settings")
            {
                Op("Görüş açısı: "+d.fov+" · +5",0,false,()=>{d.fov=d.fov>=95?60:Mathf.Min(95,d.fov+5);game.player.view.fieldOfView=d.fov;game.Save();});Op("Müzik: %"+Mathf.RoundToInt(d.music*100),1,false,()=>{d.music=d.music>=.99f?0:Mathf.Min(1,d.music+.25f);game.ApplyAudio();game.Save();});Op("Efektler: %"+Mathf.RoundToInt(d.effects*100),2,false,()=>{d.effects=d.effects>=.99f?0:Mathf.Min(1,d.effects+.25f);game.ApplyAudio();game.Save();});Op("Genel oyun ayarları",3,false,()=>game.Settings());OperationInfo("Görüş açısı, müzik ve efekt seviyeleri kayıtla korunur. Ana ses ayarı tüm sesleri birlikte etkiler.");
            }
            else if(operationSection=="help")
            {
                string[] tasks={"Bardak al","Çekirdek öğüt","Süt veya su ekle","Kapak tak","Doğru içeceği müşteriye sat"};string help="BAŞLANGIÇ GÖREVLERİ\n\n";for(int i=0;i<tasks.Length;i++)help+=((d.tutorial&(1<<i))!=0?"✓ ":"○ ")+tasks[i]+"\n\n";BodyText(help,false);
                BodyText("KONTROLLER\n\nWASD: yürü · Shift: koş\nE: kullan · Space: zıpla / banktan kalk\nR: tarif kitabı · Tab: yönetim\nM: mahalle · F1: yardım · Esc: duraklat\n\nUSTALIK\nSüt dolabının yanında köpürtme ve art istasyonu var. Espressoyu yaklaşık 1:2 oranında E ile kes. Art istasyonunda sol tuşla süt dök; tekerlekle yüksekliği ayarla, desen çiz ve bitir. H: küçük harita.",true);
            }
            else if(operationSection=="travel")
            {
                var venues=UnityEngine.Object.FindObjectsByType<CafeVenue>(FindObjectsSortMode.None).OrderBy(v=>Vector3.Distance(v.transform.position,game.player.transform.position)).ToArray();travelPage=Mathf.Clamp(travelPage,0,Mathf.Max(0,(venues.Length-1)/8));
                for(int i=0;i<Mathf.Min(8,venues.Length-travelPage*8);i++){var venue=venues[i+travelPage*8];Op(venue.title+" · 15 coin",i%4,i>=4,()=>x.Travel(venue));}
                Op("KAFEYE DÖN · 15 coin",5,true,()=>x.TravelHome());Op("Önceki duraklar",4,false,()=>travelPage=Mathf.Max(0,travelPage-1));Op("Sonraki duraklar",4,true,()=>travelPage=Mathf.Min((venues.Length-1)/8,travelPage+1));OperationInfo("Taksi ücreti 15 coin. Seçtiğin işletmenin önüne götürür; alışveriş veya sözleşme kendiliğinden yapılmaz.");
            }
            Op("OYUNA DÖN",6,true,()=>game.Continue());
        }
    }
    public sealed class CafeCupMotion : MonoBehaviour
    {
        public CafeExpansion expansion;Vector3 rest;Quaternion rotation;
        void Start(){rest=transform.localPosition;rotation=transform.localRotation;}
        void LateUpdate(){if(!expansion||expansion.game.Paused)return;if(expansion.game.data.hundred.reduceMotion){transform.localPosition=rest;transform.localRotation=rotation;return;}var velocity=expansion.game.player.body.velocity;float amount=Mathf.Clamp01(new Vector2(velocity.x,velocity.z).magnitude/3.4f);float pour=Mathf.Clamp01(expansion.pourTime/.6f);transform.localPosition=rest+new Vector3(Mathf.Sin(Time.time*7)*.007f,Mathf.Cos(Time.time*14)*.005f,0)*amount+Vector3.up*pour*.04f;transform.localRotation=rotation*Quaternion.Euler(pour*-12,0,Mathf.Sin(Time.time*7)*amount*.8f);}
    }
}



