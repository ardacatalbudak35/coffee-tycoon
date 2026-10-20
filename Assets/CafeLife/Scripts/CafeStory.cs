using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CafeLife
{
    // Hikaye: miras kalan kafe, gün içi görevler, geceleri üst katta uyumadan sabah olmaz; uyuyunca TV'de o günün haberleri çıkar.
    // storyStage: -1 kapalı (eski kayıt) · 0 giriş · 1 temizlik · 2 servis · 3 uyumaya hazır · 4 serbest oyun
    public sealed class CafeStory : MonoBehaviour
    {
        public static bool Active,Busy,Free;
        public static int Mask=1023;
        public CafeGame game; public bool allowInTest;

        struct Line { public string who,text; }
        static Line L(string who,string text)=>new Line{who=who,text=text};
        sealed class Page { public string title,body; public int unlock=-1; public Page(string t,string b,int u=-1){title=t;body=b;unlock=u;} }

        static readonly Line[] Intro={
            L("Noter","Arda Bey, Hasan Dede'nin vasiyeti açıldı. Arda's Coffee artık sizin. Üst kattaki daire de dahil."),
            L("Noter","Şehirde kahve kıtlığı yüzünden üç belediye başkanı istifa etti. Dükkânınıza göz diken yedi şirket sıraya girmiş durumda."),
            L("Arda","Satmayacağım. Dedem bu tezgâhta elli yıl espresso çekti."),
            L("Noter","O zaman anahtarlar sizde. Bir şey daha: bu şehirde gece uyumadan sabah olmuyor. Lafın gelişi değil, gerçekten olmuyor."),
            L("Hasan Dede (not)","Evladım, tezgâhın altına not bıraktım. Dükkân epey kirlenmiş. Yerdeki tozları merdivenin altındaki kovadan paspasla sil, masaları da oradaki raftan bezle."),
            L("Hasan Dede (not)","Sonra espressoyu çek. Sol duvardaki merdivenden üst kata çıkarsın, orası benim evim, artık senin de. Sana bir oda hazırladım. Gerisini akşam haberleri söyler.")
        };
        static readonly Line[] Cleaned={
            L("Hasan Dede (not)","Tertemiz olmuş, aferin. Şimdi makineyi çalıştır. Kapıda kuyruk var, hepsinin gözü kafeinden bulanık."),
            L("Arda","Hadi bakalım. İlk gün, ilk fincan. Dükkân zamanla yine kirlenir, bezi ve paspası yerinde tutayım.")
        };
        static readonly Line[] Served={
            L("Arda","Beş müşteri, hepsi sağ salim. Bu şehirde bu bile bir başarı."),
            L("Arda","Bugünlük yeter. Merdivenden çıkıp odamda yatayım, yoksa sabah olmayacak. Haberleri de kaçırmayayım.")
        };
        static readonly Page[] Day1={
            new Page("SON DAKİKA: Kahve tüketimi rekor kırdı","Şehirde kişi başı günlük kahve tüketimi 19 fincana ulaştı. Uzmanlar, 20. fincanda insanın zamanı geriye saymaya başladığını söylüyor. Belediye \"Yarın pazartesi olmasın da ne olursa olsun\" açıklaması yaptı."),
            new Page("Kuzey Dağları'nda karamel kokan çekirdek bulundu","Yabani karamel çekirdeği bir grup dağcı tarafından keşfedildi. Üç kişi kokuyu aldıktan sonra kahve makinesiyle evlendiğini duyurdu. Yetkililer çekirdeğin \"şifa değil ama tatlı\" olduğunu belirtti. Karamel Latte yarın menülerde.",2)
        };
        static readonly Page[] Generic={
            new Page("Gece dükkânlarda gölge görenler artıyor","Şehirde 20. fincanı içenlerin geceleri gölge olarak geri döndüğü iddia ediliyor. Uzmanlar \"Gölgeye uzun uzun bakmayın, bakınca kayboluyorlar\" diyor. Bir muhtar, gölgelerin yalnızca bir fincan istediğini söyledi."),
            new Page("Televizyonlar kendiliğinden açılıyor","Dün gece binlerce televizyon kimse dokunmadan açıldı. Ekranlarda yalnızca \"20. fincan\" yazıyordu. Yetkililer ekranı kapatmak yerine kahve içmeyi öneriyor."),
            new Page("Arabica borsası çöktü, kuyruklar uzadı","Borsa uzmanları bu sabahki çöküşü \"fincan dibi fiyatları\" olarak adlandırdı. Muhtar, kahve içmeden karar vermeyeceğini duyurdu."),
            new Page("Kahve içmeyen adam bulundu, müzeye kaldırıldı","Şehrin son kahve içmeyen vatandaşı, kendi isteğiyle bir cam fanusa yerleştirildi. Ziyaretçiler fanusun önünde kahve içmeyi tercih ediyor."),
            new Page("Hava durumu: Yarın yağmur yerine neskafe yağabilir","Meteoroloji, sabaha karşı bulutlardan toz kahve sarkacağını öngörüyor. Şemsiye yerine fincan almanız önerildi."),
            new Page("Çekirdek hırsızları yakalandı","Üç kişi, geçen hafta kayıp çıkan on iki kilo çekirdeği \"kendi tüketimi için\" aldığını itiraf etti. Mahkeme, cezayı \"kahve içmeden tebliğ\" şeklinde verdi.")
        };
        const string Ticker="HAVA: yarın sabah 5'te kafein yoğunluğu yüksek  ·  BORSA: arabica %12 yükseldi, robusta bayram yaptı  ·  TRAFİK: kahve sırası yüzünden 4 km kuyruk  ·  SPOR: maraton, ilk mola kahvesinde bitti  ·  ";

        Font font;
        GameObject root,tracker,dialog,tv,fadeGo; bool nightAnnounced;
        Text trackerText,who,body,tvTitle,tvBody,tvHint,tvDay; RectTransform ticker;
        CanvasGroup fade; bool pressed,tickerOn;

        void Start()
        {
            Busy=false; // editörde oynatma oturumları arasında eski değer kalmasın
            font=CafeTypography.Regular?CafeTypography.Regular:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); BuildUi(); Sync();
            sfx=gameObject.AddComponent<AudioSource>(); sfx.playOnAwake=false; sfx.spatialBlend=0;
            keyClick=KeyClick(); foreach(var f in new[]{190f,230f,270f,310f,350f}) blips.Add(Blip(f));
        }

        // ---------- TV sesleri: yazı belirirken hafif klavye tıkırtısı ve rahatsız etmeyen, söze benzeyen mırıltı ----------
        AudioSource sfx; AudioClip keyClick; readonly List<AudioClip> blips=new List<AudioClip>();
        static AudioClip KeyClick()
        {
            const int rate=22050; int n=rate/28; var d=new float[n];
            for(int i=0;i<n;i++){ float t=(float)i/rate; d[i]=((Random.value*2-1)*.55f+Mathf.Sin(2*Mathf.PI*1700*t)*.35f)*Mathf.Exp(-t*480f); }
            var clip=AudioClip.Create("Klavye",n,1,rate,false); clip.SetData(d,0); return clip;
        }
        static AudioClip Blip(float freq)
        {
            const int rate=22050; int n=(int)(rate*.075f); var d=new float[n];
            for(int i=0;i<n;i++)
            {
                float t=(float)i/rate,p=(float)i/n,env=Mathf.Sin(Mathf.PI*p); env*=env; float f=freq*(1+.12f*(p-.5f));
                d[i]=(Mathf.Sin(2*Mathf.PI*f*t)+.4f*Mathf.Sin(4*Mathf.PI*f*t)+.15f*Mathf.Sin(6*Mathf.PI*f*t))*env*.35f;
            }
            var clip=AudioClip.Create("Mırıltı",n,1,rate,false); clip.SetData(d,0); return clip;
        }
        // Yeni harf göründükçe çağrılır: her harfte çok hafif tık, her birkaç harfte kısık bir mırıltı
        void TypeSound(int shown,char c)
        {
            if(!sfx||c==' '||c=='\n')return;
            sfx.pitch=Random.Range(.94f,1.08f); sfx.PlayOneShot(keyClick,.16f);
            if(shown%3==0&&blips.Count>0)sfx.PlayOneShot(blips[Random.Range(0,blips.Count)],.07f);
        }

        void Sync()
        {
            var d=game?game.data:null;
            Active=d!=null&&d.storyStage>=0&&(!game.testing||allowInTest); Free=Active&&d.storyStage>=4; Mask=Active?d.menuMask:1023;
        }

        // ---------- arayüz ----------
        RectTransform Box(string n,Transform parent,Vector2 a,Vector2 b,Color c)
        {
            var go=new GameObject(n,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=a; r.anchorMax=b; r.offsetMin=r.offsetMax=Vector2.zero;
            go.GetComponent<Image>().color=c; return r;
        }
        Text Label(Transform parent,Vector2 a,Vector2 b,int size,Color c,TextAnchor anchor,bool strong=false)
        {
            var go=new GameObject("Yazı",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=a; r.anchorMax=b; r.offsetMin=r.offsetMax=Vector2.zero;
            var t=go.GetComponent<Text>(); t.font=strong&&CafeTypography.Strong?CafeTypography.Strong:font; t.fontSize=size; t.color=c; t.alignment=anchor;
            t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; t.supportRichText=true; t.lineSpacing=1.1f; return t;
        }
        Button Click(GameObject go,UnityEngine.Events.UnityAction action)
        { var b=go.AddComponent<Button>(); b.targetGraphic=go.GetComponent<Image>(); b.transition=Selectable.Transition.None; b.onClick.AddListener(action); return b; }

        void BuildUi()
        {
            root=new GameObject("Hikaye arayüzü",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=50;
            var scale=root.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1280,720); scale.matchWidthOrHeight=.5f;
            var gold=new Color(.95f,.78f,.42f); var paper=new Color(.96f,.93f,.85f);

            var t=Box("Görev listesi",root.transform,new Vector2(.012f,.60f),new Vector2(.30f,.80f),new Color(.07f,.05f,.04f,.72f)); tracker=t.gameObject;
            trackerText=Label(t,new Vector2(.04f,.04f),new Vector2(.96f,.96f),16,paper,TextAnchor.UpperLeft);
            var dlg=Box("Diyalog",root.transform,new Vector2(.12f,.04f),new Vector2(.88f,.27f),new Color(.07f,.05f,.04f,.94f)); dialog=dlg.gameObject;
            who=Label(dlg,new Vector2(.025f,.70f),new Vector2(.6f,.96f),22,gold,TextAnchor.MiddleLeft,true);
            body=Label(dlg,new Vector2(.025f,.16f),new Vector2(.975f,.70f),24,paper,TextAnchor.UpperLeft);
            Label(dlg,new Vector2(.70f,.02f),new Vector2(.975f,.18f),15,new Color(.8f,.75f,.65f),TextAnchor.MiddleRight).text="Devam  ▸  [E]";
            Click(dialog,()=>pressed=true); dialog.SetActive(false);

            var tvRoot=Box("TV",root.transform,Vector2.zero,Vector2.one,new Color(.015f,.015f,.02f)); tv=tvRoot.gameObject;
            var frame=Box("TV çerçevesi",tvRoot,new Vector2(.10f,.08f),new Vector2(.90f,.92f),new Color(.16f,.14f,.12f));
            var screen=Box("TV ekranı",frame,new Vector2(.025f,.04f),new Vector2(.975f,.96f),new Color(.05f,.09f,.12f));
            var head=Box("Başlık bandı",screen,new Vector2(0,.88f),Vector2.one,new Color(.62f,.10f,.10f));
            Label(head,new Vector2(.03f,0),new Vector2(.7f,1),22,Color.white,TextAnchor.MiddleLeft,true).text="●  CANLI   KAHVE HABER 24";
            tvDay=Label(head,new Vector2(.7f,0),new Vector2(.97f,1),20,Color.white,TextAnchor.MiddleRight,true);
            tvTitle=Label(screen,new Vector2(.05f,.58f),new Vector2(.95f,.84f),34,Color.white,TextAnchor.MiddleLeft,true);
            tvBody=Label(screen,new Vector2(.05f,.20f),new Vector2(.95f,.58f),24,new Color(.84f,.9f,.92f),TextAnchor.UpperLeft);
            tvHint=Label(screen,new Vector2(.55f,.11f),new Vector2(.97f,.19f),16,gold,TextAnchor.MiddleRight,true);
            var band=Box("Alt bant",screen,Vector2.zero,new Vector2(1,.10f),new Color(.12f,.20f,.28f)); band.gameObject.AddComponent<RectMask2D>();
            var tk=Label(band,Vector2.zero,Vector2.one,20,paper,TextAnchor.MiddleLeft); tk.horizontalOverflow=HorizontalWrapMode.Overflow; tk.text=Ticker+Ticker+Ticker; ticker=tk.rectTransform;
            Click(tv,()=>pressed=true); tv.SetActive(false);

            fadeGo=Box("Kararma",root.transform,Vector2.zero,Vector2.one,Color.black).gameObject; fade=fadeGo.AddComponent<CanvasGroup>(); fade.alpha=0; fade.blocksRaycasts=false;
        }

        // ---------- akış ----------
        void Update()
        {
            if(!game||game.data==null||font==null)return;
            Sync();
            var kb=Keyboard.current; if(kb!=null&&(kb.eKey.wasPressedThisFrame||kb.spaceKey.wasPressedThisFrame||kb.enterKey.wasPressedThisFrame))pressed=true;
            if(tickerOn&&ticker){ var p=ticker.anchoredPosition; p.x-=Time.unscaledDeltaTime*90; if(p.x<-ticker.rect.width/3f)p.x=0; ticker.anchoredPosition=p; }
            var d=game.data; bool show=Active&&!Busy&&!game.Paused&&game.started;
            tracker.SetActive(show);
            if(!show)return;
            trackerText.text=TrackerText(d);
            if(d.storyStage==0)StartCoroutine(Beat(Intro,1,true));
            else if(d.storyStage==1&&game.expansion.Data.cleaned>=5)StartCoroutine(Beat(Cleaned,2,false));
            else if(d.storyStage==2&&d.served>=5)StartCoroutine(Beat(Served,3,false));
        }

        string TrackerText(SaveData d)
        {
            string head="<color=#F2C76B><b>GÜN "+d.day+(d.storyStage>=4?"":" · MİRAS")+"</b></color>\n";
            if(d.dayTime>=300)return head+"Kafe kapandı. Merdivenle üst kata çık, odandaki yatağa yat.";
            if(d.storyStage>=4)return head+"Gün sonunda merdivenle üst kata çık, odandaki yatakta uyu.";
            int c=Mathf.Min(5,game.expansion.Data.cleaned);
            string Mark(bool ok)=>ok?"<color=#9EE0AE>[x]</color> ":"[  ] ";
            return head+Mark(c>=5)+"Tozları temizle ("+c+"/5)\n<size=13>yer: paspas · masa: bez (ikisi de merdiven altında)</size>\n"+Mark(d.served>=1)+"İlk müşteriyi servis et\n"+Mark(d.served>=5)+"5 müşteriye servis yap ("+Mathf.Min(5,d.served)+"/5)"+(d.storyStage>=3?"\n<color=#F2C76B>Hazırsan yan kapıdan üst kata çık.</color>":"");
        }

        IEnumerator Say(Line[] lines)
        {
            dialog.SetActive(true);
            foreach(var l in lines)
            {
                who.text=l.who; body.text=""; pressed=false; float t=0;
                while(t<l.text.Length){ t+=Time.unscaledDeltaTime*55; body.text=l.text.Substring(0,Mathf.Min(l.text.Length,(int)t)); if(pressed)break; yield return null; }
                body.text=l.text; pressed=false;
                while(!pressed)yield return null;
                pressed=false;
            }
            dialog.SetActive(false);
        }

        IEnumerator Beat(Line[] lines,int next,bool firstDay)
        {
            Busy=true; game.StoryPause(true);
            if(firstDay)PrepareFirstDay();
            yield return Say(lines);
            game.data.storyStage=next; game.Save(); game.StoryPause(false); Busy=false;
            if(next==1)game.Tell("Görev: yerdeki tozları paspasla, masaları bezle temizle (ikisi de merdiven altında).");
            if(next==3)game.Tell("Bugünlük işler tamam. İstersen merdivenle üst kata çıkıp uyuyabilirsin.",true);
        }

        static readonly Line[] Note={
            L("Hasan Dede (not)","Arda'm, bu odayı sen gelmeden hazırladım. Çarşafları kendim serdim. Dizlerim el vermedi ama olsun."),
            L("Hasan Dede (not)","Bu şehirde kahve artık bir içecek değil, bir yemin. Dükkânı sakın satma. Ama kendini de kahveye satma."),
            L("Hasan Dede (not)","Gece uyumadan sabah olmaz. Uyu, haberleri izle: şehir her gün yeni bir kahveyle uyanıyor. Sen de sabaha onu menüye koy."),
            L("Hasan Dede (not)","Altıncı çekmecede tarif defterim var. Seni çok seviyorum evladım. — Dedem Hasan")
        };
        // Tek satırlık kısa konuşma (paranormal olaylardan sonra Arda'nın yorumu vb.)
        public void Remark(string who,string text){ if(!Busy&&game&&!game.Paused)StartCoroutine(RemarkRoutine(who,text)); }
        IEnumerator RemarkRoutine(string who,string text)
        { Busy=true; game.StoryPause(true); yield return Say(new[]{L(who,text)}); game.StoryPause(false); Busy=false; }
        public void ReadNote(){ if(!Busy&&game&&!game.Paused)StartCoroutine(NoteRoutine()); }
        IEnumerator NoteRoutine()
        {
            Busy=true; game.StoryPause(true); yield return Say(Note);
            game.data.noteRead=true; game.Save(); game.StoryPause(false); Busy=false;
        }

        void PrepareFirstDay()
        {
            var x=game.expansion; if(!x)return;
            x.Data.cleanliness=Mathf.Min(x.Data.cleanliness,35f);
            for(int i=0;i<3;i++)game.cleaning.Spawn(0);
            for(int i=0;i<2;i++)game.cleaning.Spawn(1);
        }

        // Gün süresi dolunca kafe kapanır, saat durur; yatakta uyumadan sabah olmaz.
        public bool NightHold()
        {
            if(!Active||game.data.storyStage<1)return false;
            if(!nightAnnounced&&!Busy){nightAnnounced=true;game.Tell("Kafe kapandı. Merdivenle üst kata çıkıp uyumadan sabah olmaz.",true);}
            return true;
        }

        public const bool SleepAnytime=true; // TEST: true iken yatağa her an yatılır. Normal oyun için false yap (gün bitmeden veya görevler bitmeden uyunmaz).
        bool CanSleep=>Active&&game.data.storyStage>=1&&(SleepAnytime||game.data.dayTime>=300||game.data.storyStage>=3);
        public string BedLabel()
        {
            if(!Active)return "Yatak · dinlenebilirsin";
            return CanSleep?"Yatağa yat · günü bitir [E]":"Henüz uykun yok · önce işleri bitir";
        }
        public void UseBed()
        {
            if(!Active){game.Tell("Biraz dinlendin.");return;}
            if(!CanSleep){game.Tell("Henüz uykun yok. Önce gün görevlerini bitir.");return;}
            if(!Busy)StartCoroutine(Sleep());
        }

        public void Travel(Vector3 pos,float yaw){ if(!Busy&&fade)StartCoroutine(TravelRoutine(pos,yaw)); }
        IEnumerator TravelRoutine(Vector3 pos,float yaw)
        {
            Busy=true; game.StoryPause(true); yield return Fade(1);
            var p=game.player; p.body.enabled=false; p.transform.position=pos; p.transform.rotation=Quaternion.Euler(0,yaw,0); p.pitch=0; p.view.transform.localRotation=Quaternion.identity; p.body.enabled=true;
            yield return Fade(0); game.StoryPause(false); Busy=false;
        }

        IEnumerator Fade(float to)
        {
            fade.blocksRaycasts=true; float from=fade.alpha;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.7f){ fade.alpha=Mathf.Lerp(from,to,t); yield return null; }
            fade.alpha=to; fade.blocksRaycasts=to>.5f;
        }

        IEnumerator Sleep()
        {
            Busy=true; var d=game.data; game.StoryPause(true); nightAnnounced=false;
            yield return Fade(1);
            var pages=d.day==1?Day1:new[]{Generic[(d.day-2)%Generic.Length],Generic[(d.day-1)%Generic.Length]};
            tv.SetActive(true); tickerOn=true; ticker.anchoredPosition=Vector2.zero; tvDay.text="GÜN "+d.day+" · AKŞAM BÜLTENİ";
            yield return Fade(0);
            int unlock=-1;
            for(int i=0;i<pages.Length;i++)
            {
                var p=pages[i]; tvTitle.text=p.title; tvBody.text=""; tvHint.text=""; pressed=false; float t=0; int shownBefore=0;
                while(t<p.body.Length)
                {
                    t+=Time.unscaledDeltaTime*45; int shown=Mathf.Min(p.body.Length,(int)t); tvBody.text=p.body.Substring(0,shown);
                    if(shown>shownBefore){ TypeSound(shown,p.body[shown-1]); shownBefore=shown; }
                    if(pressed)break; yield return null;
                }
                tvBody.text=p.body; pressed=false; tvHint.text=i==pages.Length-1?"Uyu  ▸  [E]":"Sonraki haber  ▸  [E]";
                if(p.unlock>=0)unlock=p.unlock;
                while(!pressed)yield return null;
                pressed=false;
            }
            yield return Fade(1);
            tv.SetActive(false); tickerOn=false;
            if(unlock>=0)d.menuMask|=1<<unlock;
            if(d.storyStage<4)d.storyStage=4;
            game.AdvanceDay();
            yield return Fade(0);
            if(unlock>=0)yield return Say(new[]{L("Arda","Sabah oldu. Haberlerdeki çekirdeği tezgâha koydum: "+Economy.Recipes[unlock].name+" artık menüde.")});
            else yield return Say(new[]{L("Arda","Yeni bir gün. Kahve yine bizi bekliyor.")});
            game.Save(); game.StoryPause(false); Busy=false;
        }
    }
}
