using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CafeLife
{
    // Hikaye: miras kalan kafe, gün içi görevler, geceleri üst katta uyumadan sabah olmaz; uyuyunca TV'de o günün haberleri çıkar.
    // storyStage: -1 kapalı (eski kayıt) · 0 giriş · 1 temizlik · 2 servis · 3 uyumaya hazır · 4 serbest oyun
    public sealed partial class CafeStory : MonoBehaviour
    {
        public static bool Active,Busy,Free;
        public static int Mask=32767;
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
            L("Arda","Üç müşteri, hepsi sağ salim. Bu şehirde bu bile bir başarı."),
            L("Arda","Bugünlük yeter. Merdivenden çıkıp odamda yatayım, yoksa sabah olmayacak. Haberleri de kaçırmayayım.")
        };
        // Menü güne göre açılır: 1. gün Espresso/Buzlu Espresso/Filtre · 2. Americano · 3. Latte ailesi · 4. Flat White, Red Eye · 5. Black Eye, Dead Eye
        public static int MaskForDay(int day)
        {
            int m=(1<<0)|(1<<9)|(1<<10);
            if(day>=2)m|=(1<<4)|(1<<8); if(day>=3)m|=(1<<1)|(1<<2)|(1<<3)|(1<<5)|(1<<6)|(1<<7); if(day>=4)m|=(1<<11)|(1<<12); if(day>=5)m|=(1<<13)|(1<<14);
            return m;
        }
        public static int UnlockDay(int recipe){ for(int d=1;d<=5;d++)if((MaskForDay(d)&(1<<recipe))!=0)return d; return 5; }

        // Her gecenin haberleri: (gün N akşamı) → ertesi sabah yeni içecekler menüde
        static readonly Page[][] NightNews={
            new[]{
                new Page("SON DAKİKA: Kahve tüketimi rekor kırdı","Şehirde kişi başı günlük kahve tüketimi 19 fincana ulaştı. Uzmanlar, 20. fincanda insanın zamanı geriye saymaya başladığını söylüyor. Belediye \"Yarın pazartesi olmasın da ne olursa olsun\" açıklaması yaptı."),
                new Page("Espressoyu suyla sulandıran cesur adam: Americano doğdu","Şehre gelen yabancı bir gezgin, espressonun üstüne sıcak su ekleyince kalabalık önce sustu, sonra alkışladı. Belediye içeceği \"uzun içimli ama tehlikesiz\" ilan etti. Americano yarın menülerde.")
            },
            new[]{
                new Page("Gece dükkânlarda gölge görenler artıyor","Şehirde 20. fincanı içenlerin geceleri gölge olarak geri döndüğü iddia ediliyor. Uzmanlar \"Gölgeye uzun uzun bakmayın, bakınca kayboluyorlar\" diyor. Bir muhtar, gölgelerin yalnızca bir fincan istediğini söyledi."),
                new Page("Sütle barışan espresso: Latte geliyor","Kahve bilim insanları espressoyu sütle buluşturdu, şehirde sakinlik dalgası başladı. Vanilya ve karamel şurubu depoları da açıldı. Latte, Vanilyalı Latte ve Karamel Latte yarın menülerde.")
            },
            new[]{
                new Page("Televizyonlar kendiliğinden açılıyor","Dün gece binlerce televizyon kimse dokunmadan açıldı. Ekranlarda yalnızca \"20. fincan\" yazıyordu. Yetkililer ekranı kapatmak yerine kahve içmeyi öneriyor."),
                new Page("İki shot, sıfır uyku: Flat White ve Red Eye","Bir grup barista latteye bir shot daha ekleyince Flat White doğdu. Filtre kahveye espresso eklenince ortaya çıkan Red Eye ise gözleri kıpkırmızı yapıyor. Yetkililer \"Uyumak ne zaman moda oldu?\" diye sordu. İkisi de yarın menülerde.")
            },
            new[]{
                new Page("Gölgeler artık fincan istiyor","Geceleri görülen gölgelerin \"yalnızca bir fincan\" istediği iddiası doğrulandı. Muhtarlar kapı önlerine birer fincan bırakmayı öneriyor. Fincanın boş dönmediği bildirildi."),
                new Page("Gözlerin önce kızarıyor, sonra kararıyor: Black Eye ve Dead Eye","Filtre kahveye iki shot eklenince Black Eye, üç shot eklenince Dead Eye şehre yayıldı. Sağlık Bakanlığı Dead Eye için \"yasal ama tavsiye edilmez\" dedi. İçenlerin gözleri parlıyor, hikâyelerin sonu karanlık. İkisi de yarın menülerde.")
            },
            new[]{
                new Page("Şehir sessiz: kimse 20. fincanı içmedi","İlk kez bir gece boyunca gölge görülmedi. Uzmanlar bunu \"mahallenin bir dükkândan kahve içmesine\" bağladı. Haberi okuyan sunucu, sesini alçaltıp \"Hasan Dede'yi hatırlayanlar kapıyı çalsın\" dedi."),
                new Page("Eski bir tarif defteri aranıyor","Şehir arşivi, kayıp bir tarif defterinin izini sürdüğünü duyurdu. Sahibinin dükkânı bir torununa bıraktığı söyleniyor. Defterin altıncı çekmecede olduğu iddia ediliyor, ama kimse çekmeceyi bulamamış.")
            },
            new[]{
                new Page("Fincan sayaçları kapı kapı dolaşıyor","Mahalle sakinleri yarım ağızla konuşuyor: bazıları dün gece kaç fincan içtiğini hatırlamıyor. Uzmanlar \"19'dan sonra sayı değil, hatıra sayılır\" diyor."),
                new Page("Işıklar mavi yanıyor","Şehirde bazı dükkânların lambaları geceleri soğuk maviye dönüyor. Elektrik idaresi \"arıza yok\" açıklaması yaptı. Mavi ışığı gören, fincanı bırakıyor.")
            },
            new[]{
                new Page("Boş masalar dolu çıkıyor","Esnaflar, kapandıktan sonra boş masaların başında birinin oturduğunu söylüyor. Kimse ona hesap getirmiyor, o da bir şey istemiyor. Yalnızca bekliyor."),
                new Page("Sayma sesi: on yedi, on sekiz…","Gece yarısı sayma sesi duyanlar kapılarını kilitliyor. \"On dokuzda kesmek lazım\" diyor yaşlılar. Yirmiye gelen sayının ne olacağını kimse bilmiyor.")
            },
            new[]{
                new Page("Yankılar yürüyor","Bazı vatandaşlar sokakta kendi adımlarını bir an gecikmeyle duyduğunu söylüyor. Bilim insanları \"durursanız o da durur\" diyor, ama ilk gece kimse durmaya cesaret edemiyor."),
                new Page("Yüzsüzler mahallede","Gölgelerin bazıları artık tanıdık yüzlerle dolaşıyor, yalnızca gözleri yok. Muhtar \"onların siparişini bilen yatıştırabilir\" açıklaması yaptı.")
            },
            new[]{
                new Page("Nöbetçi göründü","Işığın altında durmayan uzun bir figür gece dükkânları dolaşıyor. Tanık sayısı arttı: \"Işığa yaklaşırsan dokunamıyor\" diyorlar."),
                new Page("Hasan'ın anlaşması konuşuluyor","Belediye arşivinde 1962'de imzalanmış bir kâğıdın varlığı iddia ediliyor. Kâğıtta tek bir cümle yazıyormuş: \"Mahalle uyumasın.\" Kâğıt hâlâ ıslakmış.")
            }
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
            Active=d!=null&&d.storyStage>=0&&(!game.testing||allowInTest); Free=false; Mask=Active?MaskForDay(d.day):(1<<Economy.Recipes.Length)-1;
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
            if(kb!=null&&kb.jKey.wasPressedThisFrame&&d.storyStage>=1){ OpenJournal(); return; }
            if(d.storyStage==0)StartCoroutine(OpeningThenIntro());
            else if(d.storyStage==1&&game.expansion.Data.cleaned>=5)StartCoroutine(Beat(Cleaned,2,false));
            else if(d.storyStage==2&&d.served>=3)StartCoroutine(Beat(Served,3,false));
        }

        // 2. ve 3. günün görevleri: o gün açılan içecekleri servis et, kafeyi temiz tut (toplamlar kayıttan okunur)
        // Gün görevleri: (içecek tarif numaraları, gereken servis, gereken temizlik, görev adı, ipucu)
        static readonly Dictionary<int,(int[] recipes,int need,int clean,string title,string tip)> Tasks=new Dictionary<int,(int[],int,int,string,string)>{
            {2,(new[]{4,8},3,7,"Americano servis et","Americano: espresso, sonra arıtma çeşmesinden su")},
            {3,(new[]{1,2,3,5,6,7},4,10,"Latte çeşitlerinden servis et","Latte: espresso, sonra süt dolabından süt")},
            {4,(new[]{11,12},3,12,"Flat White veya Red Eye servis et","Flat White: 2 shot + süt · Red Eye: filtre kahve + 1 shot")},
            {5,(new[]{13,14},3,14,"Black Eye veya Dead Eye servis et","Black Eye: filtre + 2 shot · Dead Eye: filtre + 3 shot")},
            // Bölüm 2 · Borç Defteri: sakinlerin fincan sayacı (19'da reddet [X], 20. fincan gölge yapar)
            {6,(new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14},6,14,"Müşterilere servis et","Borç defteri: sakin 19. fincana gelince Reddet [X] seçeneği çıkar")},
            {7,(new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14},8,14,"Müşterilere servis et","Işık maviye dönerse bir gölge yakın: makinenin yanında güvendesin")},
            {8,(new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14},10,16,"Müşterilere servis et","Gölgeye kapaklı fincan ikram edersen huzur bulur")},
            {9,(new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14},12,16,"Müşterilere servis et","Sayan'ı 19'da kes, Yankı'ya yaklaşma, Nöbetçi'den ışığa kaç")},
            {10,(new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14},14,18,"Müşterilere servis et","Yüzsüz gölgeye sakinin siparişini hazırla")}
        };
        int TaskSales(SaveData d,int[] recipes){ var sales=d.hundred!=null&&d.hundred.sales!=null?d.hundred.sales:new int[15]; int n=0; foreach(int r in recipes)if(r<sales.Length)n+=sales[r]; return n; }
        bool TasksDone(SaveData d){ if(!Tasks.TryGetValue(d.day,out var t))return true; return TaskSales(d,t.recipes)>=t.need&&game.expansion.Data.cleaned>=t.clean&&(d.day<5||d.finale)&&(d.day!=6||d.ledgerRead); }
        string DayTasks(SaveData d)
        {
            if(!Tasks.TryGetValue(d.day,out var t))return null; int n=TaskSales(d,t.recipes),cleaned=game.expansion.Data.cleaned;
            string Mark(bool ok)=>ok?"<color=#9EE0AE>[x]</color> ":"[  ] "; bool doneA=n>=t.need,doneB=cleaned>=t.clean;
            return Mark(doneA)+t.title+" ("+Mathf.Min(t.need,n)+"/"+t.need+")\n"+Mark(doneB)+"Kafeyi temiz tut: toz temizle ("+Mathf.Min(t.clean,cleaned)+"/"+t.clean+")"+(d.day==5?"\n"+Mark(d.finale)+"Kapıdaki uzun paltolu yabancıyla konuş":"")+(d.day==6?"\n"+Mark(d.ledgerRead)+"Altıncı çekmecenin altındaki Borç Defteri'ni oku":"")+"\n<size=13>"+t.tip+"</size>"+(doneA&&doneB&&(d.day!=5||d.finale)&&(d.day!=6||d.ledgerRead)?"\n<color=#F2C76B>Görevler tamam. Hazırsan merdivenle üst kata çık.</color>":"");
        }

        string TrackerText(SaveData d)
        {
            string head="<color=#F2C76B><b>GÜN "+d.day+(d.storyStage>=4?"":" · MİRAS")+"</b></color>\n";
            if(d.dayTime>=300)return head+"Kafe kapandı. Merdivenle üst kata çık, odandaki yatağa yat.";
            if(d.storyStage>=4)return head+(DayTasks(d)??"Gün sonunda merdivenle üst kata çık, odandaki yatakta uyu.");
            int c=Mathf.Min(5,game.expansion.Data.cleaned);
            string Mark(bool ok)=>ok?"<color=#9EE0AE>[x]</color> ":"[  ] ";
            return head+Mark(c>=5)+"Tozları temizle ("+c+"/5)\n<size=13>yer: paspas · masa: bez (ikisi de merdiven altında)</size>\n"+Mark(d.served>=1)+"İlk müşteriyi servis et\n"+Mark(d.served>=3)+"3 müşteriye servis yap ("+Mathf.Min(3,d.served)+"/3)"+(d.storyStage>=3?"\n<color=#F2C76B>Hazırsan yan kapıdan üst kata çık.</color>":"");
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
        // Çok satırlı kısa konuşma (sakinlerin tanışma/anı/dönüş diyalogları); bitince done çağrılır
        public void Talk(System.Action done,params string[] whoText){ if(Busy||!game||game.Paused)return; StartCoroutine(TalkRoutine(whoText,done)); }
        IEnumerator TalkRoutine(string[] whoText,System.Action done)
        { Busy=true; game.StoryPause(true); var lines=new List<Line>(); for(int i=0;i+1<whoText.Length;i+=2)lines.Add(L(whoText[i],whoText[i+1])); yield return Say(lines.ToArray()); done?.Invoke(); game.Save(); game.StoryPause(false); Busy=false; }
        public void Remark(string who,string text){ if(!Busy&&game&&!game.Paused)StartCoroutine(RemarkRoutine(who,text)); }
        IEnumerator RemarkRoutine(string who,string text)
        { Busy=true; game.StoryPause(true); yield return Say(new[]{L(who,text)}); game.StoryPause(false); Busy=false; }
        static readonly Line[] Book={
            L("Arda","Altıncı çekmece… Bir tarif defteri. Kapağında dedenin el yazısı var: \"Hasan'ın Defteri\"."),
            L("Hasan Dede (defter)","İlk sayfa: İyi kahve acele istemez. Espressoyu sıkıştır, sütü sabırla köpürt, müşteriye adıyla seslen."),
            L("Hasan Dede (defter)","İkinci sayfa: Filtre kahveyi makineye bırak, işine bak. Dükkânın en iyi işçisi bekleyendir."),
            L("Hasan Dede (defter)","Son sayfa: 20. fincanı kimseye ikram etme. İçeni ben geri götüremem. Arda, sen yap bunu."),
            L("Hasan Dede (defter)","Gizli tarif: her fincana bir tutam tarçın, bir tutam sabır. Müşteri farkını anlamaz ama içer, geri gelir."),
            L("Arda","Dedem bana bir kafe değil, bir görev bırakmış. Bu tarifi bundan sonra her fincana ekleyeceğim. (Tüm satışlarda +%10)")
        };
        public void ReadLedger()
        {
            if(Busy||!game||game.Paused)return; var pages=CafeResidents.LedgerPages(game); var args=new List<string>();
            if(!game.data.ledgerRead){ args.AddRange(new[]{"Arda","Çekmecenin altında ikinci bir bölme… içinde kalın bir defter. Kapağında dedemin yazısı: \"Borç\".","Hasan Dede (defter)","Her sakinin adı ve yanında bir sayı yazıyor. Fincanları saymışım. 20'ye gelen bu defterden silinir, gölge olarak dükkâna döner.","Hasan Dede (defter)","19'a gelene fincan verme. Reddetmek onu kızdırır ama korur. Onları ben korudum, şimdi sıra sende."}); }
            foreach(var p in pages){ args.Add("Borç Defteri"); args.Add(p); }
            Talk(()=>{ AddJournal("borc","Borç Defteri","Her sakinin fincanı sayılıyor. 19'da fincan verme (Reddet [X]); 20. fincan gölgeye çevirir."); if(!game.data.ledgerRead){ game.data.ledgerRead=true; game.Tell("Borç Defteri açıldı: sakinlerin fincan sayacını artık görebilirsin.",true); } },args.ToArray());
        }
        public void ReadBook(){ if(!Busy&&game&&!game.Paused)StartCoroutine(BookRoutine()); }
        IEnumerator BookRoutine()
        {
            Busy=true; game.StoryPause(true); yield return Say(Book);
            AddJournal("defter","Hasan'ın tarif defteri","20. fincanı kimseye ikram etme. Gizli tarif: her fincana bir tutam tarçın, bir tutam sabır (tüm satışlarda +%10).");
            if(!game.data.bookRead){game.data.bookRead=true;game.data.money+=150;game.data.earned+=150;game.Tell("Defterin arasından 150 coin çıktı. Gizli tarif: tüm satışlarda +%10.",true);}
            game.Save(); game.StoryPause(false); Busy=false;
        }
        public void ReadNote(){ if(!Busy&&game&&!game.Paused)StartCoroutine(NoteRoutine()); }
        IEnumerator NoteRoutine()
        {
            Busy=true; game.StoryPause(true); yield return Say(Note);
            game.data.noteRead=true; AddJournal("not","Hasan Dede'nin notu","Bu odayı sen gelmeden hazırladım. Dükkânı sakın satma, kendini de kahveye satma. Gece uyumadan sabah olmaz. Altıncı çekmecede tarif defterim var."); game.Save(); game.StoryPause(false); Busy=false;
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

        public const bool SleepAnytime=false; // TEST: true iken yatağa her an yatılır. false: gün süresi dolunca veya o günün görevleri bitince uyunur.
        bool CanSleep=>Active&&game.data.storyStage>=1&&(SleepAnytime||game.data.dayTime>=300||game.data.storyStage==3||(game.data.storyStage>=4&&TasksDone(game.data)));
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
            yield return DaySummary();
            yield return Fade(1);
            var pages=d.day<=NightNews.Length?NightNews[d.day-1]:new[]{Generic[(d.day-2)%Generic.Length],Generic[(d.day-1)%Generic.Length]};
            int oldMask=MaskForDay(d.day);
            tv.SetActive(true); tickerOn=true; ticker.anchoredPosition=Vector2.zero; tvDay.text="GÜN "+d.day+" · AKŞAM BÜLTENİ";
            yield return Fade(0);
            for(int i=0;i<pages.Length;i++)
            {
                var p=pages[i]; AddJournal("haber"+d.day+"_"+i,"Haber · "+p.title,p.body); tvTitle.text=p.title; tvBody.text=""; tvHint.text=""; pressed=false; float t=0; int shownBefore=0;
                while(t<p.body.Length)
                {
                    t+=Time.unscaledDeltaTime*45; int shown=Mathf.Min(p.body.Length,(int)t); tvBody.text=p.body.Substring(0,shown);
                    if(shown>shownBefore){ TypeSound(shown,p.body[shown-1]); shownBefore=shown; }
                    if(pressed)break; yield return null;
                }
                tvBody.text=p.body; pressed=false; tvHint.text=i==pages.Length-1?"Uyu  ▸  [E]":"Sonraki haber  ▸  [E]";
                while(!pressed)yield return null;
                pressed=false;
            }
            yield return Fade(1);
            tv.SetActive(false); tickerOn=false;
            if(d.storyStage<4)d.storyStage=4;
            game.AdvanceDay();
            int fresh=MaskForDay(d.day)&~oldMask; var names=new List<string>(); for(int i=0;i<Economy.Recipes.Length;i++)if((fresh&(1<<i))!=0)names.Add(Economy.Recipes[i].name);
            d.menuMask=MaskForDay(d.day); MarkDayStart();
            if(d.day==6)yield return ChapterCard("BÖLÜM 2","Borç Defteri");
            yield return Fade(0);
            if(names.Count>0)yield return Say(new[]{L("Arda","Sabah oldu. Haberlerde duyurulan içecekler artık menüde: "+string.Join(", ",names)+".")});
            else yield return Say(new[]{L("Arda","Yeni bir gün. Kahve yine bizi bekliyor.")});
            if(d.day==6&&!d.ledgerRead)yield return Say(new[]{L("Arda","Gece rüyamda sayılar vardı: on yedi, on sekiz… Dedemin odasındaki çekmecenin altında bir şey var gibi hissediyorum."),L("Arda","Sabah ilk işim oraya bakmak.")});
            var lost=CafeResidents.LoseOne(game);
            if(lost!=null)yield return Say(new[]{L("Arda","Bu sabah "+lost.ad+" uğramadı. Gece mahallede bir televizyonun kendiliğinden açıldığını söylediler."),L("Arda","Gölge görürsem kim olduğunu anlayacağım. Bakınca kayboluyorlarmış.")});
            yield return Decisions();
            game.Save(); game.StoryPause(false); Busy=false;
        }
    }
}
