using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafeMenuReadabilityChecks {
  public static void Validate(string section){
   Canvas.ForceUpdateCanvases();
   var frame=GameObject.Find("Cafe HUD").GetComponentsInChildren<RectTransform>().First(t=>t.name=="Studio reference frame");
   var buttons=frame.GetComponentsInChildren<Button>();
   foreach(string tab in new[]{"İŞLETME","STOK","GELİŞİM","REÇETELER","KAFE LİGİ","AYARLAR"}){
    if(buttons.Count(b=>b.GetComponentInChildren<Text>()?.text==tab)!=1)throw new Exception("Manager navigation missing or duplicated ["+section+"]: "+tab);
   }
   var corners=new Vector3[4];
   var clickable=new System.Collections.Generic.List<UnityEngine.Rect>();
   foreach(var button in buttons){
    var rect=button.transform as RectTransform;rect.GetWorldCorners(corners);
    Vector3 lower=frame.InverseTransformPoint(corners[0]),upper=frame.InverseTransformPoint(corners[2]);
    var area=new UnityEngine.Rect(lower.x,lower.y,upper.x-lower.x,upper.y-lower.y);
    if(area.xMin<frame.rect.xMin-1||area.xMax>frame.rect.xMax+1||area.yMin<frame.rect.yMin-1||area.yMax>frame.rect.yMax+1)
     throw new Exception("Menu action leaves safe reference frame ["+section+"]: "+button.name);
    if(area.width<30||area.height<28)throw new Exception("Menu action has an unusable hit target ["+section+"]: "+button.name);
    if(button.interactable){
     if(!button.targetGraphic||!button.targetGraphic.raycastTarget)throw new Exception("Menu action cannot receive pointer input: "+button.name);
     foreach(var other in clickable){
      float width=Mathf.Min(area.xMax,other.xMax)-Mathf.Max(area.xMin,other.xMin);
      float height=Mathf.Min(area.yMax,other.yMax)-Mathf.Max(area.yMin,other.yMin);
      if(width>2&&height>2)throw new Exception("Menu actions overlap ["+section+"]: "+button.name);
     }
     clickable.Add(area);
    }
   }
   foreach(var text in frame.GetComponentsInChildren<Text>()){
    if(string.IsNullOrWhiteSpace(text.text))continue;
    var metric=text.GetComponent<CafeFontSize>(); if(section.Contains("large text")&&metric&&text.fontSize!=Mathf.RoundToInt(metric.size*1.12f))throw new Exception("Large text did not resize actual canvas: "+text.text);
    var settings=text.GetGenerationSettings(new Vector2(text.rectTransform.rect.width,0));
    settings.verticalOverflow=VerticalWrapMode.Overflow;
    using(var generator=new TextGenerator()){
     float required=generator.GetPreferredHeight(text.text,settings)/text.pixelsPerUnit;
     if(required>text.rectTransform.rect.height+1)throw new Exception("Menu text clips ["+section+"]: "+text.text+" / needs "+required+" available "+text.rectTransform.rect.height);
    }
   }
  }
 }
}

