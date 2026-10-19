using UnityEngine;
namespace CafeLife {
 public sealed partial class CafeUI {
  void BuildDecorManager(){var decor=game.GetComponent<CafeDecorProgression>();int level=Economy.Level(game.data);int next=System.Array.FindIndex(CafeDecorProgression.Levels,l=>l>level);string progress=next<0?"Tüm dekor tarzları açık.":"Sıradaki: "+CafeDecorProgression.Names[next]+" · Lv."+CafeDecorProgression.Levels[next]+" · "+Mathf.Max(0,(CafeDecorProgression.Levels[next]-1)*100-game.data.xp)+" TP kaldı";
   SText("KAFE LV."+level+"   /   "+(game.data.xp%100)+" / 100 TP",268,217,360,32,23,studioGold);SText(progress,635,217,615,32,17,studioMuted,TextAnchor.MiddleRight);
   SBox(268,256,982,5,new Color(.17f,.2f,.2f));SBox(268,256,Mathf.Max(2,982*(game.data.xp%100)/100f),5,studioGold);
   for(int i=0;i<4;i++){int style=i;float x=268+(i%2)*492,y=279+(i/2)*180;bool unlocked=CafeDecorProgression.Unlocked(game.data,i),active=decor&&decor.Selected==i;
    SBox(x,y,474,166,active?new Color(.16f,.19f,.17f):studioCard);SText(CafeDecorProgression.Names[i]+"  /  Lv."+CafeDecorProgression.Levels[i],x+16,y+10,442,34,23,active?studioGold:ink);
    SText(CafeDecorProgression.Details[i],x+16,y+44,442,42,15,studioMuted);
    for(int c=0;c<4;c++)SBox(x+17+c*57,y+88,47,21,CafeDecorProgression.Swatch(i,c));
    SText(active?"KULLANILIYOR":unlocked?"AÇIK · ÜCRETSİZ":"SEVİYEYLE AÇILIR",x+252,y+84,205,28,13,unlocked?studioGold:studioMuted,TextAnchor.MiddleRight);
    var button=SButton(active?"SEÇİLİ TARZ":unlocked?"TARZI UYGULA":"KİLİTLİ · KAFE LV."+CafeDecorProgression.Levels[i],x+16,y+121,442,33,()=>{decor.Select(style);ShowMenu("operations");},unlocked&&!active);button.interactable=unlocked&&!active;
   }
  }
 }
}

