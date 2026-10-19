using UnityEngine;
using UnityEngine.UI;

namespace CafeLife {
 public sealed partial class CafeUI {
  int studioDecor=-1;
  void BuildDecorManager(){
   var decor=game.GetComponent<CafeDecorProgression>();int level=Economy.Level(game.data);
   if(studioDecor<0||studioDecor>=4)studioDecor=decor?decor.Selected:0;
   int next=System.Array.FindIndex(CafeDecorProgression.Levels,value=>value>level);
   SText("SEVİYE "+level,80,221,220,29,14,studioGold);
   SText(next<0?"Bütün tarzlar açık":CafeDecorProgression.Names[next]+" tarzı: seviye "+CafeDecorProgression.Levels[next],350,221,850,29,14,studioMuted,TextAnchor.MiddleRight);
   SProgress(80,253,1120,(game.data.xp%100)/100f,studioGold);
   for(int i=0;i<4;i++){
    int style=i;float y=278+i*83;bool unlocked=CafeDecorProgression.Unlocked(game.data,i),selected=studioDecor==i,current=decor&&decor.Selected==i;
    var button=SQuietButton("",80,y,420,70,()=>{studioDecor=style;ShowMenu("operations");},selected);
    if(selected)SBox(80,y+12,2,46,studioGold);
    SText(CafeDecorProgression.Names[i],96,y+3,274,32,22,selected?ink:studioMuted);
    SText(current?"Kullanılıyor":unlocked?"Kullanılabilir":"Seviye "+CafeDecorProgression.Levels[i]+" gerekli",96,y+36,280,27,14,current?studioGold:studioMuted);
    SBox(431,y+20,39,29,CafeDecorProgression.Swatch(i,3));
    SRule(80,y+76,420);
   }
   SBox(534,279,1,347,studioRule);
   bool available=CafeDecorProgression.Unlocked(game.data,studioDecor),active=decor&&decor.Selected==studioDecor;
   SText(CafeDecorProgression.Names[studioDecor],576,278,624,44,29,ink);
   SText(CafeDecorProgression.Details[studioDecor],576,331,624,49,16,studioMuted,TextAnchor.UpperLeft);
   string[] materials={"Duvar","Meşe","Koyu ahşap","Döşeme"};
   for(int i=0;i<4;i++){
    float x=576+i*160;Color sample=CafeDecorProgression.Swatch(studioDecor,i);
    SBox(x+4,401,144,127,new Color(0,0,0,.25f));
    var swatch=SBox(x,397,144,127,sample);
     if(i==1||i==2){
      // Keep this as a stable UI Image; the applied world material carries the wood grain.
      SBox(x+10,407,124,2,new Color(1,1,1,.08f));
     }
    if(i==3){
     for(int stitch=0;stitch<12;stitch++)SBox(x+8+stitch*11,405,1,111,new Color(1,1,1,.055f));
     for(int stitch=0;stitch<9;stitch++)SBox(x+8,405+stitch*13,128,1,new Color(0,0,0,.07f));
    }
    SText(materials[i],x,535,144,29,14,studioMuted);
   }
   string label=active?"BU TARZ KULLANILIYOR":available?"TARZI UYGULA · ÜCRETSİZ":"SEVİYE "+CafeDecorProgression.Levels[studioDecor]+" GEREKLİ";
   SButton(label,576,585,624,43,()=>{if(decor)decor.Select(studioDecor);ShowMenu("operations");},available&&!active).interactable=available&&!active;
  }
 }
}
