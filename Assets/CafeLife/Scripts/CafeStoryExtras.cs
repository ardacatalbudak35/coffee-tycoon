using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CafeLife
{
    [System.Serializable] public class JournalEntry { public string id,title,text; public int day; }

    // Hikâyeyi oyuncuya hissettirenler: günlük (J), açılış/bölüm kartları, gün özeti, karar anları, Nermin'in odası ipuçları.
    public sealed partial class CafeStory
    {
        // ---------- günlük verisi ----------
        public void AddJournal(string id,string title,string text)
        {
            var d=game.data; if(d.journal==null)d.journal=new List<JournalEntry>(); foreach(var e in d.journal)if(e.id==id)return;
            d.journal.Add(new JournalEntry{id=id,title=CafeNames.Fix(title),text=CafeNames.Fix(text),day=d.day}); if(!id.StartsWith("haber"))game.Tell("Günlüğe eklendi: "+title+"  [J]");
        }
        int MetCount(){ int n=0; foreach(var r in CafeResidents.All)if(CafeResidents.State(game,r).met)n++; return n; }
        void MarkDayStart(){ var d=game.data; d.dayStartServed=d.served; d.dayStartMoney=d.money; d.dayStartMet=MetCount(); }

        // ---------- gün özeti ----------
        IEnumerator DaySummary()
        {
            var d=game.data; int served=d.served-d.dayStartServed,met=MetCount()-d.dayStartMet; string text="Gün özeti: "+Mathf.Max(0,served)+" müşteri servis ettin";
            if(!(d.dayStartMoney==0&&d.day>1)){ int delta=d.money-d.dayStartMoney; text+=", kasa "+(delta>=0?"+":"")+delta+" coin"; }
            text+="."; if(met>0)text+=" "+met+" yeni sakinle tanıştın."; var gone=CafeResidents.GoneToday(game); if(gone!=null)text+=" "+gone.ad+" bugün gelmedi.";
            yield return Say(new[]{L("Arda",text)});
        }

        // ---------- bölüm / açılış kartı ----------
        GameObject card; CanvasGroup cardGroup; Text cardTag,cardTitle,cardSub;
        void BuildCard()
        {
            card=new GameObject("Bölüm kartı",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)); card.transform.SetParent(transform,false);
            var cv=card.GetComponent<Canvas>(); cv.renderMode=RenderMode.ScreenSpaceOverlay; cv.sortingOrder=60;
            var sc=card.GetComponent<CanvasScaler>(); sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution=new Vector2(1280,720); sc.matchWidthOrHeight=.5f;
            var bg=Box("Siyah",card.transform,Vector2.zero,Vector2.one,Color.black); cardGroup=bg.gameObject.AddComponent<CanvasGroup>(); cardGroup.alpha=0; cardGroup.blocksRaycasts=false;
            var gold=new Color(.95f,.78f,.42f); var paper=new Color(.96f,.93f,.85f);
            cardTag=Label(bg,new Vector2(.1f,.60f),new Vector2(.9f,.68f),22,gold,TextAnchor.MiddleCenter); cardTitle=Label(bg,new Vector2(.1f,.46f),new Vector2(.9f,.60f),58,paper,TextAnchor.MiddleCenter,true); cardSub=Label(bg,new Vector2(.1f,.36f),new Vector2(.9f,.46f),30,gold,TextAnchor.MiddleCenter);
        }
        IEnumerator ChapterCard(string title,string sub,string tag="")
        {
            if(!card)BuildCard(); cardTag.text=tag; cardTitle.text=title; cardSub.text=sub;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.7f){ cardGroup.alpha=t; yield return null; } cardGroup.alpha=1;
            pressed=false; float hold=0; while(hold<2.8f&&!(pressed&&hold>.8f)){ hold+=Time.unscaledDeltaTime; yield return null; } pressed=false;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.9f){ cardGroup.alpha=1-t; yield return null; } cardGroup.alpha=0;
        }
        IEnumerator OpeningThenIntro()
        {
            Busy=true; game.StoryPause(true);
            yield return ChapterCard("BÖLÜM 1","Miras","Hasan Dede'nin cenazesinden üç gün sonra…");
            yield return Beat(Intro,1,true);
        }

        // ---------- günlük arayüzü (J) ----------
        GameObject journalGo; Text jTabs,jBody,jFoot; int jTab,jPage;
        void OpenJournal(){ if(Busy||!game||game.Paused||!Active)return; StartCoroutine(JournalRoutine()); }
        void BuildJournal()
        {
            var gold=new Color(.95f,.78f,.42f); var paper=new Color(.96f,.93f,.85f);
            journalGo=Box("Günlük",root.transform,new Vector2(.08f,.06f),new Vector2(.92f,.94f),new Color(.07f,.05f,.04f,.97f)).gameObject;
            jTabs=Label(journalGo.transform,new Vector2(.03f,.88f),new Vector2(.97f,.97f),24,gold,TextAnchor.MiddleLeft,true);
            jBody=Label(journalGo.transform,new Vector2(.04f,.10f),new Vector2(.96f,.86f),22,paper,TextAnchor.UpperLeft);
            jFoot=Label(journalGo.transform,new Vector2(.03f,.02f),new Vector2(.97f,.09f),16,new Color(.8f,.75f,.65f),TextAnchor.MiddleCenter);
            journalGo.SetActive(false);
        }
        static string Stars(int n){ n=Mathf.Clamp(n,0,8); return new string('★',n)+new string('☆',8-n); }
        void RenderJournal()
        {
            var d=game.data; string[] names={"Notlar ve anılar","Sakinler","Haberler"}; string tabs=""; for(int i=0;i<3;i++)tabs+=(i==jTab?"<color=#F2C76B>[ "+names[i]+" ]</color>":"<color=#8c8270>  "+names[i]+"  </color>")+"   ";
            jTabs.text=tabs; var lines=new List<string>(); int per=4;
            if(jTab==1)
            {
                per=3; foreach(var r in CafeResidents.All){ var s=CafeResidents.State(game,r);
                    if(!s.met){ lines.Add("<b>???</b>  ·  henüz tanışmadın"); continue; }
                    string status=s.absentUntil>=d.day&&s.absentUntil>0?"  <color=#8fb4ff>· gölge olarak dolaşıyor</color>":""; lines.Add("<b>"+r.ad+"</b>  ·  "+r.meslek+", "+r.yas+status+"\nBağ  "+Stars(s.bond)+"   Fincan  "+Mathf.Min(20,s.cups)+"/20   Sipariş: "+r.siparis+"\n<i>"+r.huy+"</i>"+(s.memory1?"\n<color=#9EE0AE>Anı: bir sırrını paylaştı</color>":"")); }
            }
            else
            {
                var list=new List<JournalEntry>(); if(d.journal!=null)foreach(var e in d.journal){ bool news=e.id.StartsWith("haber"); if((jTab==2)==news)list.Add(e); } list.Reverse();
                foreach(var e in list)lines.Add("<b>"+e.title+"</b>  <color=#8c8270>(gün "+e.day+")</color>\n"+e.text);
                if(lines.Count==0)lines.Add(jTab==2?"Henüz haber izlemedin. Üst kattaki yatakta uyuyunca akşam haberleri açılır.":"Henüz bir not yok. Hasan Dede'nin notunu bul, sakinlerle tanış.");
            }
            int pages=Mathf.Max(1,Mathf.CeilToInt(lines.Count/(float)per)); jPage=Mathf.Clamp(jPage,0,pages-1); string body=""; for(int i=jPage*per;i<Mathf.Min(lines.Count,(jPage+1)*per);i++)body+=lines[i]+"\n\n";
            jBody.text=body; jFoot.text="◀ ▶ sekme    ▲ ▼ sayfa ("+(jPage+1)+"/"+pages+")    [J] kapat";
        }
        IEnumerator JournalRoutine()
        {
            Busy=true; game.StoryPause(true); if(!journalGo)BuildJournal(); journalGo.SetActive(true); jTab=0; jPage=0; RenderJournal(); yield return null;
            while(true)
            {
                var kb=Keyboard.current; if(kb==null)break;
                if(kb.jKey.wasPressedThisFrame||kb.eKey.wasPressedThisFrame||kb.backspaceKey.wasPressedThisFrame)break;
                if(kb.rightArrowKey.wasPressedThisFrame||kb.dKey.wasPressedThisFrame){ jTab=(jTab+1)%3; jPage=0; RenderJournal(); }
                else if(kb.leftArrowKey.wasPressedThisFrame||kb.aKey.wasPressedThisFrame){ jTab=(jTab+2)%3; jPage=0; RenderJournal(); }
                else if(kb.downArrowKey.wasPressedThisFrame||kb.sKey.wasPressedThisFrame){ jPage++; RenderJournal(); }
                else if(kb.upArrowKey.wasPressedThisFrame||kb.wKey.wasPressedThisFrame){ jPage=Mathf.Max(0,jPage-1); RenderJournal(); }
                yield return null;
            }
            journalGo.SetActive(false); pressed=false; game.StoryPause(false); Busy=false;
        }

        // ---------- karar anları ----------
        IEnumerator Choose(string speaker,string prompt,string[] options,System.Action<int> pick)
        {
            dialog.SetActive(true); who.text=speaker; var sb=new System.Text.StringBuilder(prompt+"\n"); for(int i=0;i<options.Length;i++)sb.Append("\n<color=#F2C76B>["+(i+1)+"]</color> "+options[i]); body.text=sb.ToString(); pressed=false;
            int choice=-1; while(choice<0){ var kb=Keyboard.current; if(kb!=null){ if(kb.digit1Key.wasPressedThisFrame)choice=0; else if(kb.digit2Key.wasPressedThisFrame&&options.Length>1)choice=1; else if(kb.digit3Key.wasPressedThisFrame&&options.Length>2)choice=2; } yield return null; }
            dialog.SetActive(false); pressed=false; pick(choice);
        }
        IEnumerator Decisions()
        {
            var d=game.data;
            if(d.day==8&&d.choice1==0)
            {
                yield return Say(new[]{L("Arda","Gece rüyamda uzun paltolu biri vardı, Nuri. Sesi hâlâ kulağımda.")});
                yield return Choose("Nuri","Anlaşmayı bozdurmak için bana yardım et, evladım. Sayma bitsin. Ne dersin?",new[]{"Güven. Ona yardım et.","Güvenme. Kendi yolumu bulacağım."},i=>{
                    d.choice1=i+1;
                    if(i==0){ d.money+=100; d.earned+=100; AddJournal("karar1","Karar: Nuri'ye güvendim","Nuri'ye yardım edeceğimi söyledim. Karşılığında 100 coin ve makinenin ışığının etkisi genişledi: güvenli alan büyüdü."); game.Tell("Güvenli alan büyüdü (+1,2 m). +100 coin.",true); }
                    else { AddJournal("karar1","Karar: Nuri'ye güvenmedim","Kendi yolumu seçtim. Sayan'ın saymak için daha çok zamanı olacak, bana karşı gardım yüksek."); game.Tell("Sayan daha yavaş sayıyor.",true); }
                });
            }
            else if(d.day==9&&d.choice2==0)
            {
                yield return Say(new[]{L("Arda","Sabah kapıda iyi giyimli bir adam bekliyordu: Kemal Aksu, belediyeden.")});
                yield return Choose("Kemal Aksu","Dükkânınıza 3000 coin veriyorum. Altındaki odayı da isterim. Ne dersiniz?",new[]{"Reddet. Dükkân satılık değil.","Pazarlık et: bilgi karşılığı 300 coin al.","Zaman kazan: düşüneceğimi söyle."},i=>{
                    d.choice2=i+1;
                    if(i==0){ d.reputation=Mathf.Min(100,d.reputation+2); AddJournal("karar2","Karar: Kemal'i reddettim","Dükkânı satmadığımı söyledim. Mahalle bunu duydu, itibarım arttı."); game.Tell("İtibar +2.",true); }
                    else if(i==1){ d.money+=300; d.earned+=300; AddJournal("karar2","Karar: Kemal'le pazarlık","Ona kayıtlar hakkında bir şeyler söyledim, 300 coin aldım. Ama artık beni izliyor: hırsız daha sık gelecek."); game.Tell("+300 coin. Hırsız artık daha sık gelecek.",true); }
                    else { AddJournal("karar2","Karar: Kemal'e zaman istedim","Düşüneceğimi söyledim. Bu, ona iki kapıyı da açık bıraktı."); game.Tell("Karar ertelendi.",true); }
                });
            }
            else if(d.day==10&&d.choice3==0)
            {
                yield return Say(new[]{L("Arda","Gece Nermin'in odasından bir ses geldi. Bu sefer korkutmak için değil, konuşmak için.")});
                yield return Choose("Nermin (ses)","Arda… oğlum… Seni duyabiliyor muyum?",new[]{"Cevap ver: yanındayım.","Sustur: kapıyı kapat, odayı kilitle."},i=>{
                    d.choice3=i+1;
                    if(i==0){ AddJournal("karar3","Karar: Nermin'e cevap verdim","Nine'ye cevap verdim. Odası artık yalnız değil. Gölgeler sakinleşiyor: olaylar daha seyrek."); game.Tell("Paranormal olaylar daha seyrek.",true); }
                    else { d.money+=100; d.earned+=100; AddJournal("karar3","Karar: Nermin'in odasını kilitledim","Kapıyı kapattım. Sessizlik uzun sürmeyecek; gölgeler daha sık gelecek. Dolapta 100 coin buldum."); game.Tell("Olaylar sıklaşacak. +100 coin.",true); }
                });
            }
            else if(d.day==11&&!d.epilogue)
            {
                // Bölüm sonu: üç kararın (Nuri, Kemal, Nermin) sonucuna göre değişen kapanış; skor yüksekse "huzur", düşükse "uzun gece" sonu
                d.epilogue=true; int score=(d.choice1==1?1:0)+(d.choice2==1?1:0)+(d.choice3==1?1:0);
                string nuri=d.choice1==1?"Nuri'ye güvenmiştim. Makinenin ışığı artık yalnızca yakındakileri değil, sokağın ucunu bile ısıtıyor.":"Nuri'ye güvenmedim. Sayan yavaş sayıyor ama bu savaşı yalnız veriyorum.";
                string kemal=d.choice2==1?"Kemal'e dükkânın satılık olmadığını söyledim. Mahalle bu cümleyi ezberledi.":d.choice2==2?"Kemal'le pazarlık ettim. Verdiğim bilgi yüzünden hâlâ arkamı kolluyorum.":"Kemal'e zaman istedim. İki kapı da hâlâ açık.";
                string nermin=d.choice3==1?"Nermin Nine'ye cevap verdim. Odası artık yalnız değil.":"Nermin'in odasını kilitledim. Sessizlik bir borç gibi duruyor.";
                yield return Say(new[]{L("Arda","On gün geçti. Bu şehirde bir gece, fincanlar sayılmadan ilk kez huzur içinde sabaha çıktı."),L("Arda",nuri),L("Arda",kemal),L("Arda",nermin)});
                if(score>=2)
                {
                    yield return Say(new[]{L("Hasan Dede (not)","Evladım, dükkânı bir miras sanmıştım. Meğer bir mahalleymiş. Aferin sana."),L("Arda","Dedemin anlaşması hâlâ ıslak olabilir ama kimse bu sabah kapıyı yalnız çalmadı. Bölüm 1 burada biter.")});
                    d.money+=500; d.earned+=500; d.reputation=Mathf.Min(100,d.reputation+5); AddJournal("bolum1","Bölüm 1 sonu: Huzur","Üç kararın çoğunda halkın ve gölgelerin yanında durdum. Kafe artık bir miras değil, bir mahalle. (+500 coin, itibar +5)"); game.Tell("Bölüm 1 sonu: Huzur · +500 coin, itibar +5",true);
                }
                else
                {
                    yield return Say(new[]{L("Hasan Dede (not)","Her şeyi yalnız taşımak zorunda değilsin evladım. Ama taşıyorsan da düşürme."),L("Arda","Dükkân ayakta ama gece uzun sürüyor ve sayma bitmedi. Yarın yine bir fincanla başlıyoruz. Bölüm 1 burada biter.")});
                    d.money+=200; d.earned+=200; AddJournal("bolum1","Bölüm 1 sonu: Uzun gece","Kararlarımın çoğunda kendi yolumu seçtim. Kafe ayakta ama gece hâlâ uzun. (+200 coin)"); game.Tell("Bölüm 1 sonu: Uzun gece · +200 coin",true);
                }
            }
        }

        // ---------- Nermin'in odası ----------
        static readonly string[][] NerminLines={
            new[]{"Arda","Yatağın başucunda yaşlı bir kadının tarağı. Üstünde hâlâ gümüş saç telleri var.","Arda","Dedem yalnız yaşıyordu, diye bilirdim. Bu oda kimin?"},
            new[]{"Arda","Çerçeveli eski bir fotoğraf: genç Hasan ve yanında gülen bir kadın. Arkasında kurşun kalemle yazılmış: \"Nermin ile, 1971. On dokuzuncu fincan.\"","Arda","Nermin… Nine? Hiç anlatılmamıştı."},
            new[]{"Arda","Masanın üstünde kurumuş papatyalar, kâğıda sarılı. Kâğıtta dedemin yazısı: \"Her gün o değiştirirdi. Şimdi ben.\"","Arda","Vazodaki su… hâlâ taze. Kim değiştirdi?"}
        };
        static readonly string[] NerminJournal={"Gümüş saçlı bir tarak, Hasan Dede'nin odasında değil, benim odamda. Bu oda kimin?","Eski fotoğraf: Hasan ve Nermin, 1971. Arkasında \"on dokuzuncu fincan\" yazıyor.","Kuru papatyalar ve taze su. Dedem her gün çiçeği değiştirirmiş, şimdi biri hâlâ değiştiriyor."};
        static readonly string[] NerminTitle={"Gümüş saçlı tarak","Hasan ve Nermin, 1971","Kuru papatyalar"};
        public void ReadNermin(int i)
        {
            if(Busy||!game||game.Paused||i<0||i>2)return;
            Talk(()=>{ game.data.nermin|=1<<i; AddJournal("nermin"+i,NerminTitle[i],NerminJournal[i]); if(game.data.nermin==7)game.Tell("Üç ipucu birleşti: bu odanın sahibi Nermin Nine. Gece sesleri ondan geliyor olabilir.",true); },NerminLines[i]);
        }
        public string NerminLabel(int i)
        {
            string[] names={"Gümüş saçlı bir tarak","Çerçeveli eski bir fotoğraf","Kuru papatyalar ve taze su"}; bool seen=(game.data.nermin&(1<<i))!=0; return names[i]+(seen?" · tekrar bak [E]":" · bak [E]");
        }
    }
}
