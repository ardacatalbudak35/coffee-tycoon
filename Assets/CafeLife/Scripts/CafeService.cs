using UnityEngine;
namespace CafeLife {
 public static class CafeService {
  public static int Preference(CafeCustomer customer){int kind=customer.identity%4+1;var recipe=Economy.Recipes[customer.recipe];return kind==1&&(!recipe.milk||recipe.cold)?4:kind;}
  public static string PreferenceLabel(CafeCustomer customer){switch(Preference(customer)){case 1:return "Latte art ister";case 2:return "Güçlü kahve · en az 20 g";case 3:return "Dengeli espresso ister";default:return "Taze servis · 30 sn altında";}}
  public static bool Meets(CafeCustomer customer,PreparedDrink drink){if(!drink.good)return false;switch(Preference(customer)){case 1:return drink.cup.art;case 2:return drink.cup.dose>=20;case 3:return CafeHundred.Score(drink.cup)>=.85f;default:return drink.age<30;}}
  public static int Quality(PreparedDrink drink){var cup=drink.cup;float fresh=Mathf.Clamp01(1-drink.age/120);float score=CafeHundred.Score(cup)*75+fresh*(Economy.Recipes[cup.recipe].milk&&!cup.coldCup?15:25);if(cup.foamed)score+=5;if(cup.art)score+=5;return Mathf.RoundToInt(Mathf.Clamp(score,0,drink.good?100:40));}
  public static string Feedback(PreparedDrink drink,CafeCustomer customer){if(!drink.good){if(drink.age>60)return "Kahve beklemişti; daha taze servis et.";if(CafeHundred.Score(drink.cup)<.55f||drink.cup.yield/drink.cup.dose<1.55f||drink.cup.yield/drink.cup.dose>2.55f)return CafeHundred.Taste(drink.cup)+"; demlemeyi ayarla.";return "Malzeme veya hazırlık sırası yanlıştı.";}if(Meets(customer,drink))return "Tam istediğim gibi! "+PreferenceLabel(customer)+" · tercih bonusu +3";return "Tarif güzeldi. Bir dahakine: "+PreferenceLabel(customer)+".";}
  public static void Assess(CafeCustomer customer,PreparedDrink drink){customer.serviceQuality=Quality(drink);customer.preferenceMet=Meets(customer,drink);customer.feedback=Feedback(drink,customer);customer.qualityBonus=(drink.cup.foamed?3:0)+(drink.cup.art?5:0)+(drink.cup.perfectBrew?4:0)+(customer.preferenceMet?3:0)+(customer.serviceQuality>=90?2:0);}
 }
}
