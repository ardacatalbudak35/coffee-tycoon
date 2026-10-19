using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafeOrderTextChecks {
  static readonly BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Fits(Text text,string content){var settings=text.GetGenerationSettings(new Vector2(text.rectTransform.rect.width,0));settings.verticalOverflow=VerticalWrapMode.Overflow;var generated=new TextGenerator();float height=generated.GetPreferredHeight(content,settings)/text.pixelsPerUnit;if(height>text.rectTransform.rect.height+.5f)throw new Exception("Order text clips: "+content+" height "+height+" available "+text.rectTransform.rect.height);}
  public static void Validate(CafeGame game){Canvas.ForceUpdateCanvases();var names=(Text[])typeof(CafeUI).GetField("orderNames",fields).GetValue(game.ui);var details=(Text[])typeof(CafeUI).GetField("orderDetails",fields).GetValue(game.ui);foreach(var recipe in Economy.Recipes)Fits(names[0],recipe.name);Fits(names[0],"Yeni sipariş bekleniyor");var preferences=new[]{"Latte art ister","Güçlü kahve · en az 20 g","Dengeli espresso ister","Taze servis · 30 sn altında"};var steps=new[]{"Bardak rafı","Teslim tezgâhı","Buz kovası","Karamel şurubu","Vanilya şurubu","Süt dolabı","Arıtma çeşmesi","Değirmen","Espresso demleniyor","Espresso makinesi","Kapak istasyonu","Soğuk bardak rafı","Sıcak bardak rafı","Süt köpürtülüyor","Süt köpürtme","Latte art istasyonu"};foreach(var preference in preferences)foreach(var step in steps)Fits(details[0],preference+"\nSıradaki: "+step);}
 }
}
