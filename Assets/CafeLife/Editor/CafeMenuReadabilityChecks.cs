using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CafeLife {
 public static class CafeMenuReadabilityChecks {
  public static void Validate(string section){
   Canvas.ForceUpdateCanvases();
   var frame=GameObject.Find("Cafe HUD").GetComponentsInChildren<RectTransform>().First(t=>t.name=="Studio reference frame");
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

