using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CafeLife
{
    public sealed partial class CafeUI : MonoBehaviour
    {
        public CafeGame game;
#if UNITY_EDITOR
        public bool EditorTouchPreview;
#endif
        private RectTransform canvas,safe,menu; private Text stats,hint,toast; private Font font;
        private int districtPage; private Text orders; private Image brewFill; private RectTransform brewCard; private static Sprite rounded; private Image hintCard; private string page=""; private GameObject joystick,look,action,worldMenuButton; private Vector2 lastSize;
        Color ink=new Color(.94f,.92f,.87f), panel=new Color(.045f,.065f,.055f,.97f), green=new Color(.14f,.18f,.15f);
        public void Build()
        {
            font=CafeTypography.Regular;
            var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var go=new GameObject("Cafe HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=go.GetComponent<RectTransform>(); go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().pixelPerfect=true;
            var scale=go.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1280,720); scale.matchWidthOrHeight=.5f;
            safe=Rect("Safe area",canvas,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var top=Panel(safe,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-88),Vector2.zero,panel);
            top.name="Hidden legacy status header";top.gameObject.SetActive(false); stats=Text(top,"",22,TextAnchor.MiddleLeft); stats.rectTransform.offsetMin=new Vector2(22,0);
            var hc=Panel(safe,new Vector2(.2f,0),new Vector2(.8f,0),new Vector2(0,86),new Vector2(0,150),new Color(.035f,.08f,.075f,.96f)); hintCard=hc.GetComponent<Image>(); hint=Text(hc,"",21,TextAnchor.MiddleCenter); hint.rectTransform.offsetMin=new Vector2(18,8); hint.rectTransform.offsetMax=new Vector2(-18,-8);
            toast=Label(safe,new Vector2(.2f,0),new Vector2(.8f,0),new Vector2(0,135),new Vector2(0,225),21,TextAnchor.MiddleCenter);
            var aim=Rect("Aim reticle",canvas,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-12,-12),new Vector2(12,12)); aim.gameObject.AddComponent<CafeReticle>().game=game;
            Button(safe,"MENÜ",new Vector2(1,1),new Vector2(-452,-73),new Vector2(100,45),()=>game.TogglePause());worldMenuButton=safe.GetChild(safe.childCount-1).gameObject;
            joystick=Panel(safe,Vector2.zero,Vector2.zero,new Vector2(25,25),new Vector2(185,185),new Color(.1f,.15f,.15f,.6f)).gameObject;
            var move=joystick.AddComponent<TouchPad>(); var knob=Panel(joystick.transform as RectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-25,-25),new Vector2(25,25),new Color(.85f,.8f,.65f,.8f)); move.knob=knob; game.player.movePad=move;
            look=Panel(safe,new Vector2(.55f,.25f),new Vector2(1,.85f),Vector2.zero,Vector2.zero,new Color(0,0,0,.001f)).gameObject; var lp=look.AddComponent<TouchPad>(); lp.look=true; game.player.lookPad=lp;
            action=new GameObject("Mobil düğmeler",typeof(RectTransform)); action.transform.SetParent(safe,false); var ar=action.GetComponent<RectTransform>(); ar.anchorMin=Vector2.zero; ar.anchorMax=Vector2.one; ar.offsetMin=ar.offsetMax=Vector2.zero;
            Button(ar,"ETKİLEŞ",new Vector2(1,0),new Vector2(-170,25),new Vector2(145,75),()=>game.player.Use());
            Button(ar,"ZIPLA",new Vector2(1,0),new Vector2(-320,25),new Vector2(120,65),()=>game.player.Jump());
            var sprintButton=Panel(ar,new Vector2(1,0),new Vector2(1,0),new Vector2(-320,100),new Vector2(-200,160),green);
            game.player.sprintPad=sprintButton.gameObject.AddComponent<TouchSprint>();
            Text(sprintButton,"HIZLI",18,TextAnchor.MiddleCenter);
            menu=Panel(safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.045f,.065f,.055f,1));
            var orderCard=Panel(safe,new Vector2(1,1),new Vector2(1,1),new Vector2(-302,-435),new Vector2(-18,-108),panel);
            orders=Text(orderCard,"",17,TextAnchor.UpperLeft); orders.rectTransform.offsetMin=new Vector2(18,14); orders.rectTransform.offsetMax=new Vector2(-18,-14);
            brewCard=Panel(safe,new Vector2(.32f,0),new Vector2(.68f,0),new Vector2(0,40),new Vector2(0,70),panel);
            var fill=Panel(brewCard,Vector2.zero,Vector2.one,new Vector2(4,4),new Vector2(-4,-4),new Color(.72f,.53f,.26f)); brewFill=fill.GetComponent<Image>(); brewFill.type=Image.Type.Filled; brewFill.fillMethod=Image.FillMethod.Horizontal;
            Text(brewCard,"ESPRESSO HAZIRLANIYOR",14,TextAnchor.MiddleCenter);
            BuildExpansionHud();BuildOrderHud();StyleTouchControls();if(CafeFocusedMode.CounterControls)BuildFocusedHud(); SafeArea();
        }
        public void ApplyReadability(bool largeText,bool contrast)
        {
            if(!canvas)return;
            foreach(var metric in canvas.GetComponentsInChildren<CafeFontSize>(true))
            {
                var text=metric.GetComponent<Text>();if(!text)continue;
                text.fontSize=largeText?Mathf.RoundToInt(metric.size*1.12f):metric.size;
                text.color=contrast?Color.white:metric.color;
            }
        }
        void SafeArea()
        {
            if(Screen.width<=0 || Screen.height<=0) return;
            var a=Screen.safeArea; safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height); safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height); lastSize=new Vector2(Screen.width,Screen.height);
        }
        void Update()
        {
            if(game.data==null || !stats) return;
            UpdateStudio();
            if(lastSize!=new Vector2(Screen.width,Screen.height)) SafeArea();
            if(CafeFocusedMode.CounterControls){UpdateFocusedHud();return;}
            UpdateExpansionHud(); var d=game.data;
            stats.transform.parent.gameObject.SetActive(false);toast.gameObject.SetActive(!game.Paused);
            stats.text="ARDA'S COFFEE    •    "+d.money+" coin    •    Lv."+Economy.Level(d)+"    •    Gün "+d.day+"\nMemnuniyet %"+Mathf.RoundToInt(d.reputation)+"    Çekirdek "+d.beans+" / Süt "+d.milk+" / Buz "+d.ice+"    •    Müşteri "+game.customers.Count+" / "+Mathf.Min(game.data.tables*2,24)+"    •    Teslimde "+game.shelf.Count;
            if(game.player.transform.position.z<-11)stats.text="ARDA'S COFFEE   /   "+d.money+" COIN   /   GÜN "+d.day+"\n"+(game.player.transform.position.z<-23?(game.player.transform.position.x>3?(game.player.transform.position.y>3?"ROYAL JACKPOT · ÜST KAT":"SMOKEBANK · GECE KULÜBÜ"):game.player.transform.position.x< -22?"BASKET NOKTASI":"GECE MARKETİ"):"CADDE · E ile etkileş");
            hint.text=game.player.Hint; hintCard.gameObject.SetActive(!game.Paused && !string.IsNullOrEmpty(hint.text)); toast.text=game.noticePinned||Time.unscaledTime<game.noticeUntil?game.notice:"";
            orders.transform.parent.gameObject.SetActive(false);UpdateOrderHud();
            string queue="<color=#E8C88F><b>SİPARİŞLER</b></color>\n"; int count=0;
            foreach(var customer in game.customers.Where(c=>c&&c.state==CafeCustomer.State.Waiting).OrderBy(c=>c.patience)) if(count++<2) queue+="\n"+(customer.accepted?"#"+customer.orderNumber:"Yeni")+" · "+Economy.Recipes[customer.recipe].name+"\n<color=#ABBDB5>Sabır %"+Mathf.CeilToInt(customer.patience)+" · "+CafeService.PreferenceLabel(customer)+"</color>\n";
            if(count==0)queue+="\nYeni müşteriler bekleniyor.";
            if(game.data.districtContract>=0)queue+="\n<color=#E8C88F>Mahalle sözleşmen var.</color>";
            if(game.trackedVenue)queue+="\n<color=#E8C88F>HEDEF</color>\n"+game.trackedVenue.title+"\n"+(game.expansion?game.expansion.Direction(game.trackedVenue)+" · ":"")+Mathf.RoundToInt(Vector3.Distance(game.trackedVenue.transform.position,game.player.transform.position))+" m";
            orders.text=queue; brewCard.gameObject.SetActive(!game.Paused && game.brewTime>0); if(game.brewTotal>0)brewFill.fillAmount=1-game.brewTime/game.brewTotal;
            bool mobile=Application.isMobilePlatform;
#if UNITY_EDITOR
            mobile|=EditorTouchPreview;
#endif
            bool worldInput=!game.player.WorldInputBlocked;joystick.SetActive(mobile&&worldInput);look.SetActive(mobile&&worldInput);action.SetActive(mobile&&worldInput);worldMenuButton.SetActive(!(game.GetComponent<CafeLattePour>()?.Active??false));
        }
        public void ShowMenu(string name)
        {
            page=name;
            foreach(Transform child in menu){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            menu.gameObject.SetActive(name!=""); if(name=="") return; menu.SetAsLastSibling();
            if(BuildStudioPage(name))return;
            var title=Label(menu,new Vector2(0,1),new Vector2(1,1),new Vector2(35,-80),new Vector2(-35,-20),34,TextAnchor.MiddleLeft);
            title.text=name=="operations"?"COFFEE TYCOON · YÖNETİM":name=="district"?"MAHALLE REHBERİ · M":name=="venue"?game.activeVenue.title:name=="shop"?"KAFE YÖNETİMİ":name=="settings"?"AYARLAR":name=="casino"?"ROYAL JACKPOT • BLACKJACK":name=="branch"?game.data.branches[game.activeBranchIndex].name+" • YÖNETİM":"ARDA'S COFFEE";
            if(CafeFocusedMode.Enabled){if(name=="home"||name=="pause"){BuildFocusedHome(name);return;}if(name=="operations"){BuildFocusedMenu();return;}if(name=="confirm"){BuildFocusedConfirm();return;}}
            var note=Label(menu,new Vector2(0,0),new Vector2(1,0),new Vector2(30,10),new Vector2(-30,60),18,TextAnchor.MiddleLeft);
            if(name=="operations")note.text=game.notice;
            else if(name=="venue") { note.text=game.data.money+" coin   •   "+game.notice; }
            else if(name=="casino") note.text=game.blackjackMessage+"\nElinde 21'i geçmeden dur veya krupiyeyi geç. Bakiye: "+game.data.money+" coin.";
            else if(name=="branch") { var b=game.data.branches[game.activeBranchIndex]; note.text=b.owned?"Kapasite "+b.capacity+" müşteri • Kahve geliri Lv."+b.espressoLevel+" • Barista Lv."+b.baristaLevel:"Boş ve kiralık kafe • Sıradaki açılış daha büyük, daha çok müşterili ve daha pahalı."; }
            else note.gameObject.AddComponent<MenuNotice>().game=game;
            if(name=="operations") BuildOperations(menu);
            else if(name=="district")
            {
                var venues=UnityEngine.Object.FindObjectsByType<CafeVenue>(FindObjectsSortMode.None).OrderBy(v=>Vector3.Distance(v.transform.position,game.player.transform.position)).ToArray();
                for(int i=0;i<Mathf.Min(10,venues.Length-districtPage*10);i++) { var v=venues[i+districtPage*10]; int row=i%5; string label=v.title+" · "+Mathf.RoundToInt(Vector3.Distance(v.transform.position,game.player.transform.position))+" m"; UnityEngine.Events.UnityAction choose=()=>{game.trackedVenue=v;game.Continue();}; if(i<5)Row(label,row,choose);else RightRow(label,row,choose); }
                Row("ÖNCEKİ İŞLETMELER",5,()=>{districtPage=Mathf.Max(0,districtPage-1);ShowMenu("district");});
                RightRow("SONRAKİ İŞLETMELER",5,()=>{districtPage=Mathf.Min((venues.Length-1)/10,districtPage+1);ShowMenu("district");});
                Row("OYUNA DÖN",6,()=>game.Continue());
                RightRow("İşaretlemeyi kaldır",6,()=>{game.trackedVenue=null;game.Continue();});
            }
            else if(name=="venue")
            {
                var venue=game.activeVenue;
                var card=Panel(menu,new Vector2(.04f,.35f),new Vector2(.96f,.83f),Vector2.zero,Vector2.zero,new Color(.09f,.16f,.15f,1));
                var copy=Text(card,venue.Description+"\n\n"+venue.Progress,26,TextAnchor.MiddleLeft); copy.rectTransform.offsetMin=new Vector2(30,20); copy.rectTransform.offsetMax=new Vector2(-30,-20);
                Button(menu,venue.ActionLabel,new Vector2(0,0),new Vector2(50,130),new Vector2(700,64),()=>{venue.Execute();ShowMenu("venue");});
                Button(menu,"ŞEHRE DÖN",new Vector2(1,0),new Vector2(-350,130),new Vector2(300,64),()=>game.Continue());
            }
            else if(name=="home" || name=="pause")
            {
                Row("OYUNA DEVAM ET",0,()=>game.Continue());
                Row("YENİ OYUN",1,()=>ShowMenu("confirm"));
                Row("AYARLAR",2,()=>game.Settings());
                Row("KAYDET",3,()=>{game.Save();game.Tell("İlerleme kaydedildi.");});
                Row("ÇIKIŞ",4,()=>{game.Save(); Application.Quit();});
                Row("MAHALLE REHBERİ · M",5,()=>game.OpenDistrict());
                Row("İŞLETME / TARİFLER / RAPOR · TAB",6,()=>game.OpenOperations());
                var help=Label(menu,new Vector2(.52f,.25f),new Vector2(.95f,.7f),Vector2.zero,Vector2.zero,24,TextAnchor.UpperLeft);
                help.text="WASD • Yürü\nMouse • Etrafa bak\nSPACE • Zıpla\nSHIFT • Hafif hızlı koş\nE • Etkileşim\nESC • Duraklat\n\nKafeyi yönlendirmek için içerideki yönetim noktasına yaklaş ve E'ye bas.";
            }
            else if(name=="confirm") { Row("YENİ OYUNU BAŞLAT (eski kayıt yedeklenir)",0,()=>game.NewGame()); Row("VAZGEÇ",1,()=>ShowMenu("home")); }
            else if(name=="settings")
            {
                Row("Ses: "+Mathf.RoundToInt(game.data.volume*100)+"%",0,()=>{game.data.volume=game.data.volume>=.99f?0:Mathf.Min(1,game.data.volume+.25f); AudioListener.volume=game.data.volume; game.Save(); ShowMenu(page);});
                Row("Bakış hassasiyeti: "+game.data.sensitivity.ToString("0.00"),1,()=>{game.data.sensitivity=game.data.sensitivity>=.2f?.05f:game.data.sensitivity+.05f; game.Save(); ShowMenu(page);});
                Row("Grafik: "+QualitySettings.names[QualitySettings.GetQualityLevel()],2,()=>{game.data.quality=(QualitySettings.GetQualityLevel()+1)%QualitySettings.names.Length;QualitySettings.SetQualityLevel(game.data.quality);game.Save();ShowMenu(page);});
                Row("GÖRÜŞ / MÜZİK / EFEKTLER",3,()=>game.OpenOperations("settings"));
                Row("Ani korkutmalar (jumpscare): "+(CafeParanormal.JumpscaresEnabled?"AÇIK":"KAPALI"),4,()=>{CafeParanormal.JumpscaresEnabled=!CafeParanormal.JumpscaresEnabled; game.data.noJumpscare=!CafeParanormal.JumpscaresEnabled; game.Save(); ShowMenu(page);});
                Row("GERİ",5,()=>ShowMenu("pause"));
            }
            else if(name=="shop")
            {
                Row("Makine Lv."+game.data.machine+" → "+game.data.machine*90+" coin",0,()=>Buy("machine"));
                Row("Yeni masa → "+(140+game.data.tables*30)+" coin",1,()=>Buy("table"));
                Row("Dekor → "+(50+game.data.decor*30)+" coin",2,()=>Buy("decor"));
                Row("Depoya stok siparişi → 35 coin",3,()=>Buy("stock"));
                Row(game.data.staff>0?"Barista Lv."+game.data.baristaLevel+" → yükselt":"Barista işe al → 350 coin",4,()=>game.UpgradeMainBarista());
                Row("Kıyafet rengini değiştir → 25 coin",5,()=>Buy("outfit"));
                Row("Görev: "+game.data.served+" / "+((game.data.quest+1)*5)+" servis • ödül al",6,()=>{game.Quest();ShowMenu(page);});
                RightRow("Espresso geliri Lv."+game.data.espressoLevel+" → yükselt",0,()=>Buy("espresso"));
                RightRow("Duvar boyası kalite Lv."+game.data.paintLevel+" → boya",1,()=>Buy("paint"));
                RightRow("Arda's Coffee yönetim paneli",2,()=>game.OpenBranch(0));
                RightRow("Stok depoya gelir; arka raftan taşı",3,()=>{});
                RightRow("Acil ücretsiz çekirdek yardımı",5,()=>{game.RescueStock();ShowMenu(page);});
                RightRow("OYUNA DÖN",6,()=>game.Continue());
            }
            else if(name=="branch")
            {
                int i=game.activeBranchIndex; var b=game.data.branches[i];
                if(!b.owned)
                {
                    int[] prices={0,2500,12000,50000,180000};
                    Row("KAFEYİ KİRALA → "+prices[i]+" coin",0,()=>game.BuyBranch(i));
                    RightRow("Potansiyel: "+b.capacity+" müşteri",0,()=>{});
                    RightRow("Bir önceki şube açık olmalı",1,()=>{});
                }
                else
                {
                    Row(i==0&&game.data.staff==0?"Barista işe al → 350 coin":"Barista seviyesi Lv."+(i==0?game.data.baristaLevel:b.baristaLevel)+" → yükselt",0,()=>{if(i==0)game.UpgradeMainBarista();else game.UpgradeBranch(i,"barista");});
                    Row("Espresso kalitesi Lv."+(i==0?game.data.espressoLevel:b.espressoLevel)+" → yükselt",1,()=>game.UpgradeBranch(i,"espresso"));
                    Row("Duvar rengini boya Lv."+(i==0?game.data.paintLevel:b.paintLevel)+" → kalite",2,()=>game.UpgradeBranch(i,"paint"));
                    if(i>0)RightRow("KAFENİN İÇİNE GİR",3,()=>game.EnterBranch(i));
                    RightRow("Barista Lv.3'te şube kendi kendine çalışır",0,()=>{});
                    RightRow("Her şubenin kazancı kasana eklenir",1,()=>{});
                }
                Row("BU KAFEDEN ÇIK",6,()=>game.Continue());
            }
            else if(name=="casino")
            {
                if(game.blackjackActive)
                {
                    Row("KART ÇEK (HIT)",0,()=>game.BlackjackHit());
                    Row("DUR (STAND)",1,()=>game.BlackjackStand());
                }
                else
                {
                    Row("Bahis 10 coin",0,()=>game.StartBlackjack(10));
                    Row("Bahis 50 coin",1,()=>game.StartBlackjack(50));
                    Row("Bahis 200 coin",2,()=>game.StartBlackjack(200));
                }
                Row("KAFEDEN ÇIK",6,()=>game.Continue());
            }
        }
        void Buy(string what) { game.Buy(what); ShowMenu(page); }
        void Row(string text,int row,UnityEngine.Events.UnityAction action) => Button(menu,text,new Vector2(0,1),new Vector2(35,-142-row*64),new Vector2(560,54),action);
        void RightRow(string text,int row,UnityEngine.Events.UnityAction action) => Button(menu,text,new Vector2(1,1),new Vector2(-580,-142-row*64),new Vector2(545,54),action);
        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper)
        { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=min; r.anchorMax=max; r.offsetMin=lower; r.offsetMax=upper; return r; }
        RectTransform Panel(RectTransform parent,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,Color color)
        { var r=Rect("Panel",parent,min,max,lower,upper); var image=r.gameObject.AddComponent<Image>(); image.color=color;
            if(!rounded) { int size=32; var tex=new Texture2D(size,size,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Bilinear; for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=Mathf.Max(8-x,x-23),dy=Mathf.Max(8-y,y-23);float alpha=1-Mathf.Clamp01(Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy))-7);tex.SetPixel(x,y,new Color(1,1,1,alpha));}tex.Apply();rounded=Sprite.Create(tex,new UnityEngine.Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(10,10,10,10)); }
            image.sprite=rounded;image.type=Image.Type.Sliced; return r; }
        Text Text(RectTransform parent,string content,int size,TextAnchor align)
        { var r=Rect("Text",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); var t=r.gameObject.AddComponent<Text>(); t.font=size>=24?CafeTypography.Strong:font; t.fontSize=size;t.lineSpacing=1.12f;t.color=ink;var metric=t.gameObject.AddComponent<CafeFontSize>();metric.size=size;metric.color=ink; t.alignment=align; t.text=content; t.raycastTarget=false; return t; }
        Text Label(RectTransform parent,Vector2 min,Vector2 max,Vector2 lower,Vector2 upper,int size,TextAnchor align)
        { var r=Rect("Label",parent,min,max,lower,upper); return Text(r,"",size,align); }
        void Button(RectTransform parent,string label,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        { var r=Panel(parent,anchor,anchor,pos,pos+size,green); var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=r.GetComponent<Image>(); b.onClick.AddListener(action); var palette=b.colors; palette.normalColor=Color.white; palette.highlightedColor=new Color(1.2f,1.35f,1.25f); palette.pressedColor=new Color(.65f,.8f,.7f); palette.fadeDuration=.12f; b.colors=palette; var text=Text(r,label,19,TextAnchor.MiddleCenter); text.font=CafeTypography.Strong;text.fontStyle=FontStyle.Normal; text.rectTransform.offsetMin=new Vector2(14,4); text.rectTransform.offsetMax=new Vector2(-14,-4); }
    }
    public sealed class MenuNotice : MonoBehaviour
    {
        public CafeGame game;
#if UNITY_EDITOR
        public bool EditorTouchPreview;
#endif
        void Update() { GetComponent<Text>().text=game.data.money+" coin   •   "+game.notice; }
    }
}














