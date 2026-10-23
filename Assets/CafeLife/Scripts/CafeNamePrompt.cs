using UnityEngine;
using UnityEngine.UI;

namespace CafeLife
{
    // Oyuncu ve kafe adı. Hikâye metinlerinde sabit yazılı "Arda" ve "Arda's Coffee" gösterim anında bu adlarla değiştirilir (Fix),
    // çünkü hikâye satırları static readonly dizilerde tutuluyor ve oyun açılırken oluşuyor.
    public static class CafeNames
    {
        public const string DefaultPlayer="Arda", DefaultCafe="Arda's Coffee";
        public static string Player=DefaultPlayer, Cafe=DefaultCafe;
        static readonly System.Globalization.CultureInfo tr=new System.Globalization.CultureInfo("tr-TR");
        static string lastBrand="ARDA'S COFFEE";
        public static string CafeUpper=>Cafe.ToUpper(tr);
        public static string PlayerUpper=>Player.ToUpper(tr);

        // Boşluk ve <,> temizler (arayüz zengin metin kullanıyor), uzunluğu kısıtlar; boşsa yedek değeri verir.
        public static string Clean(string s,string fallback,int max)
        {
            if(s==null)return fallback; s=s.Replace("<","").Replace(">","").Trim();
            if(s.Length>max)s=s.Substring(0,max).Trim(); return s.Length==0?fallback:s;
        }
        public static void Apply(SaveData d)
        {
            if(d==null)return;
            Player=Clean(d.playerName,DefaultPlayer,16); Cafe=Clean(d.cafeName,DefaultCafe,22); d.playerName=Player; d.cafeName=Cafe;
            if(d.branches!=null&&d.branches.Length>0&&d.branches[0]!=null)d.branches[0].name=Cafe;
        }
        // Sahnedeki marka tabelalarını (TextMesh) yeni kafe adına çevirir
        public static void RelabelWorld(CafeGame g)
        {
            string now=CafeUpper;
            if(g&&g.world)foreach(var t in g.world.GetComponentsInChildren<TextMesh>(true))if(t&&t.text==lastBrand)t.text=now;
            lastBrand=now;
        }
        // Metindeki "Arda" ve "Arda's Coffee"yi oyuncu adıyla değiştirir. Ek alan tek yerde var: "Arda'm" (Hasan Dede notu) → "Evladım".
        public static string Fix(string s)
        {
            if(string.IsNullOrEmpty(s)||(Player==DefaultPlayer&&Cafe==DefaultCafe))return s;
            s=s.Replace("Arda's Coffee","\u0001C").Replace("Arda'm",Player==DefaultPlayer?"Arda'm":"Evladım").Replace("ARDA","\u0001U").Replace("Arda","\u0001P");
            return s.Replace("\u0001C",Cafe).Replace("\u0001P",Player).Replace("\u0001U",PlayerUpper);
        }
    }

    // Yeni oyun açılırken oyuncu ve kafe adını soran bağımsız ekran (üç farklı "yeni oyun" arayüzünden de aynı yolla açılır).
    public sealed class CafeNamePrompt : MonoBehaviour
    {
        CafeGame game; GameObject root; InputField playerField,cafeField; Text error; Font font;

        public void Open(CafeGame g)
        {
            game=g; if(!root)Build();
            playerField.text=""; cafeField.text=""; error.text=""; root.SetActive(true);
            playerField.ActivateInputField();
        }

        RectTransform Box(Transform parent,string n,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax,Color c)
        {
            var go=new GameObject(n,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=amin; r.anchorMax=amax; r.offsetMin=omin; r.offsetMax=omax; go.GetComponent<Image>().color=c; return r;
        }
        Text Label(Transform parent,string text,int size,TextAnchor anchor,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax,Color c)
        {
            var go=new GameObject("Yazı",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=amin; r.anchorMax=amax; r.offsetMin=omin; r.offsetMax=omax;
            var t=go.GetComponent<Text>(); t.font=font; t.fontSize=size; t.color=c; t.alignment=anchor; t.text=text; t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        InputField Field(Transform parent,string placeholder,float top,int limit)
        {
            var r=Box(parent,"Alan",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-340,top-54),new Vector2(340,top),new Color(.14f,.18f,.16f));
            var text=Label(r,"",24,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(16,4),new Vector2(-16,-4),Color.white); text.supportRichText=false;
            var ph=Label(r,placeholder,22,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(16,4),new Vector2(-16,-4),new Color(1,1,1,.35f));
            var input=r.gameObject.AddComponent<InputField>(); input.targetGraphic=r.GetComponent<Image>(); input.textComponent=text; input.placeholder=ph; input.characterLimit=limit;
            input.onValidateInput=(t,i,c)=>c=='<'||c=='>'||char.IsControl(c)?'\0':c; return input;
        }
        void Button(Transform parent,string label,Vector2 omin,Vector2 omax,Vector2 anchor,UnityEngine.Events.UnityAction action)
        {
            var r=Box(parent,label,anchor,anchor,omin,omax,new Color(.14f,.22f,.17f)); var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=r.GetComponent<Image>(); b.onClick.AddListener(action);
            var colors=b.colors; colors.highlightedColor=new Color(1.25f,1.4f,1.3f); colors.pressedColor=new Color(.65f,.8f,.7f); b.colors=colors;
            Label(r,label,21,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.94f,.92f,.87f));
        }

        void Build()
        {
            font=CafeTypography.Regular?CafeTypography.Regular:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root=new GameObject("İsim giriş ekranı",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200;
            var scale=root.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1280,720); scale.matchWidthOrHeight=.5f;
            var dim=Box(root.transform,"Karartma",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.88f));
            var card=Box(dim,"Kart",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-400,-235),new Vector2(400,235),new Color(.07f,.09f,.08f));
            var cream=new Color(.94f,.92f,.87f); var gold=new Color(.95f,.78f,.42f);
            Label(card,"YENİ OYUN",34,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(40,-80),new Vector2(-40,-20),gold);
            Label(card,"Önce kendini ve kafeni tanıt. Eski kayıt yedeklenir.",19,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(40,-125),new Vector2(-40,-85),cream);
            Label(card,"Senin adın",18,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(60,-170),new Vector2(-60,-145),gold);
            playerField=Field(card,"Örn. Ahmet",-175,16);
            Label(card,"Kafenin adı (boş bırakırsan adından üretilir)",18,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(60,-255),new Vector2(-60,-230),gold);
            cafeField=Field(card,"Örn. Ahmet's Coffee",-260,22);
            error=Label(card,"",18,TextAnchor.MiddleCenter,new Vector2(0,1),new Vector2(1,1),new Vector2(40,-345),new Vector2(-40,-322),new Color(1,.55f,.45f));
            Button(card,"BAŞLA",new Vector2(-340,30),new Vector2(-10,90),new Vector2(.5f,0),Begin);
            Button(card,"VAZGEÇ",new Vector2(10,30),new Vector2(340,90),new Vector2(.5f,0),()=>root.SetActive(false));
        }

        void Begin()
        {
            string p=CafeNames.Clean(playerField.text,"",16);
            if(p.Length==0){ error.text="Önce adını yaz."; return; }
            CafeGame.PendingPlayer=p; CafeGame.PendingCafe=CafeNames.Clean(cafeField.text,p+"'s Coffee",22);
            root.SetActive(false); game.NewGame();
        }
    }
}
