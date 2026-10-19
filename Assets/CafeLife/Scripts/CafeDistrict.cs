using System;
using UnityEngine;

namespace CafeLife
{
    public sealed class CafeVenue : MonoBehaviour
    {
        public int id;
        public string category, title;
        public CafeGame game;
        public string Description => category=="bakery"?"Fırınla ortak servis: 12 servis tamamla, 140 coin kazan.":
            category=="market"?"Toptan tedarik: 30 çekirdek, 20 süt ve 20 buz depoya gelir. 45 coin.":
            category=="pizzeria"?"Komşu işletmeyle çapraz tanıtım: 40 coin karşılığı +8 memnuniyet.":
            category=="cinema"?"Sinema fuayesinde reklam: 80 coin karşılığı +20 memnuniyet.":
            category=="hotel"?"Otel kahvaltı sözleşmesi: 20 servis tamamla, 260 coin kazan.":
            category=="blue_house"?"Mahalle teslimatı: 5 servis tamamla, 65 coin kazan.":
            category=="yellow_house"?"Barista atölyesi: 60 coin karşılığı +100 deneyim. Günde bir kez.":
            "Mahalle buluşması: 25 coin karşılığı +6 memnuniyet.";
        public bool Contract => category=="bakery" || category=="hotel" || category=="blue_house";
        public int Goal => category=="hotel"?20:category=="bakery"?12:5;
        public int Reward => category=="hotel"?260:category=="bakery"?140:65;
        public int Cost => category=="market"?45:category=="cinema"?80:category=="yellow_house"?60:category=="pizzeria"?40:25;
        public string Progress
        {
            get
            {
                if(Contract && game.data.districtContract==id)
                    return "İlerleme: "+Mathf.Min(Goal,Mathf.Max(0,game.data.served-game.data.contractStart))+" / "+Goal+" servis";
                if(!Contract && game.data.venueVisitDay[id]==game.data.day) return "Bugünkü işlem tamamlandı. Yarın tekrar gel.";
                return Contract?"Bir seferde bir mahalle sözleşmesi alabilirsin.":"Günde bir işlem • Bakiye: "+game.data.money+" coin";
            }
        }
        public string ActionLabel => Contract?(game.data.districtContract==id?"ÖDÜLÜ AL · +"+Reward+" COIN":"SÖZLEŞMEYİ AL · "+Goal+" SERVİS"):"ONAYLA · "+Cost+" COIN";
        public void Execute()
        {
            var d=game.data;
            if(Contract)
            {
                if(d.districtContract==-1) { d.districtContract=id; d.contractStart=d.served; game.Tell("Sözleşme alındı. Kafende servis yapıp buraya dön."); }
                else if(d.districtContract!=id) game.Tell("Önce aldığın diğer mahalle sözleşmesini tamamla.");
                else if(d.served-d.contractStart<Goal) game.Tell("Henüz yeterli servis yapmadın.");
                else { d.money+=Reward;if(game.expansion)game.expansion.Data.dayIncome+=Reward; d.xp+=Goal*5; d.districtContract=-1; game.Tell("Sözleşme tamamlandı! +"+Reward+" coin",true); }
            }
            else
            {
                if(d.venueVisitDay[id]==d.day) { game.Tell("Bugünkü işlemi yaptın. Yarın tekrar gel."); return; }
                if(!game.SpendExpense(Cost)) { game.Tell("Yetersiz para. Gereken: "+Cost+" coin."); return; }
                d.venueVisitDay[id]=d.day;
                if(category=="market") { d.depotBeans+=30; d.depotMilk+=20; d.depotIce+=20; }
                else if(category=="yellow_house") d.xp+=100;
                else d.reputation=Mathf.Min(100,d.reputation+(category=="cinema"?20:category=="pizzeria"?8:6));
                game.Tell(category=="market"?"Tedarik depoya geldi; kafendeki depodan bara taşı.":category=="yellow_house"?"Atölye tamamlandı. +100 deneyim!":"Tanıtım tamamlandı. Kafenin memnuniyeti arttı.",true);
            }
            game.Save();
        }
    }
}

