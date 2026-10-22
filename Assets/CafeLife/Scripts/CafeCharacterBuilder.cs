using UnityEngine;

namespace CafeLife
{
    // Sıfırdan, koddan kurulan insan figürü: Kenney modeli yok. Ten/saç/kıyafet/aksesuar CafeCharacters.json'dan gelir.
    public static class CafeCharacterBuilder
    {
        public sealed class Rig
        {
            public Transform body,hipL,hipR,kneeL,kneeR,shoulderL,shoulderR,elbowL,elbowR,head;
            public void Walk(float s,float amount)
            {
                float leg=s*30*amount,arm=s*24*amount;
                hipL.localRotation=Quaternion.Euler(leg,0,0); hipR.localRotation=Quaternion.Euler(-leg,0,0);
                kneeL.localRotation=Quaternion.Euler(Mathf.Clamp(-leg,0,45)*1.1f,0,0); kneeR.localRotation=Quaternion.Euler(Mathf.Clamp(leg,0,45)*1.1f,0,0);
                shoulderL.localRotation=Quaternion.Euler(-arm,0,4); shoulderR.localRotation=Quaternion.Euler(arm,0,-4);
                elbowL.localRotation=Quaternion.Euler(-10-Mathf.Abs(arm)*.4f,0,0); elbowR.localRotation=Quaternion.Euler(-10-Mathf.Abs(arm)*.4f,0,0);
                body.localPosition=new Vector3(0,-Mathf.Abs(s)*.012f*amount,0);
                if(head)head.localRotation=Quaternion.Euler(0,0,0);
            }
            public void Sit(bool sit)
            {
                if(!sit){body.localPosition=Vector3.zero;kneeL.localRotation=kneeR.localRotation=Quaternion.identity;return;}
                body.localPosition=new Vector3(0,-.34f,0);
                hipL.localRotation=hipR.localRotation=Quaternion.Euler(-88,0,0); kneeL.localRotation=kneeR.localRotation=Quaternion.Euler(88,0,0);
                shoulderL.localRotation=shoulderR.localRotation=Quaternion.Euler(-35,0,0); elbowL.localRotation=elbowR.localRotation=Quaternion.Euler(-60,0,0);
            }
        }

        static CafeGame game; static Transform parent;
        static Transform P(Transform to,string n,PrimitiveType t,Vector3 pos,Vector3 scale,Color c,Vector3? euler=null)
        {
            var o=GameObject.CreatePrimitive(t); o.name=n; o.transform.SetParent(to,false); o.transform.localPosition=pos; o.transform.localScale=scale;
            if(euler.HasValue)o.transform.localEulerAngles=euler.Value;
            var col=o.GetComponent<Collider>(); if(col)Object.Destroy(col);
            o.GetComponent<Renderer>().sharedMaterial=game.world.ColorMaterial(c); return o.transform;
        }
        static Transform Pivot(Transform to,string n,Vector3 pos){var g=new GameObject(n).transform;g.SetParent(to,false);g.localPosition=pos;return g;}
        static Color Hex(string s,Color fallback){return ColorUtility.TryParseHtmlString(s,out var c)?c:fallback;}
        static Color Skin(string t){switch(t){case "acik":return Hex("#F2C7A5",Color.white);case "esmer":return Hex("#A8714D",Color.white);case "koyu":return Hex("#6B4630",Color.white);default:return Hex("#D9A073",Color.white);}}
        static bool Has(string s,params string[] keys){s=s.ToLowerInvariant();foreach(var k in keys)if(s.Contains(k))return true;return false;}

        public static Rig Build(CafeGame g,Transform root,CafeResidents.Resident r)
        {
            game=g; var look=r.gorunum; bool female=r.cinsiyet=="kadin";
            Color skin=Skin(look.ten),hair=Hex(look.sac_rengi_hex,new Color(.2f,.12f,.08f)),top=Hex(look.ust_renk_hex,Color.gray),bottom=Hex(look.alt_renk_hex,new Color(.2f,.2f,.3f));
            string topN=look.ust_giysi??"",botN=look.alt_giysi??"",shoeN=look.ayakkabi??"",hairN=look.sac??"",accN=look.aksesuar??"";
            var rig=new Rig(); var body=Pivot(root,"Vücut",Vector3.zero); rig.body=body;
            bool shortSleeve=Has(topN,"tişört","polo"),coat=Has(topN,"önlük","mont","ceket"),skirt=Has(botN,"etek"),shorts=Has(botN,"şort");
            float torsoW=female?.33f:.38f;
            // gövde
            P(body,"Pelvis",PrimitiveType.Sphere,new Vector3(0,.93f,0),new Vector3(torsoW*.95f,.22f,.22f),skirt?bottom:bottom);
            P(body,"Gövde",PrimitiveType.Capsule,new Vector3(0,1.18f,0),new Vector3(torsoW,.3f,.22f),top);
            if(female)P(body,"Omuz",PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(torsoW+.06f,.14f,.2f),top);
            else P(body,"Omuz",PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(torsoW+.12f,.16f,.22f),top);
            if(coat){P(body,"Önlük/mont",PrimitiveType.Capsule,new Vector3(0,1.14f,0),new Vector3(torsoW+.03f,.34f,.25f),top); if(Has(topN,"önlük"))P(body,"Önlük etek",PrimitiveType.Cube,new Vector3(0,.78f,0),new Vector3(torsoW+.02f,.5f,.25f),top);
                if(Has(topN,"mont"))P(body,"Reflektör",PrimitiveType.Cube,new Vector3(0,1.12f,0),new Vector3(torsoW+.045f,.05f,.27f),new Color(.95f,.9f,.2f));}
            if(skirt)P(body,"Etek",PrimitiveType.Cylinder,new Vector3(0,.66f,0),new Vector3(.4f,.3f,.3f),bottom);
            P(body,"Kemer",PrimitiveType.Cube,new Vector3(0,.97f,0),new Vector3(torsoW+.01f,.04f,.235f),new Color(.12f,.08f,.06f));
            // boyun ve baş
            P(body,"Boyun",PrimitiveType.Cylinder,new Vector3(0,1.52f,0),new Vector3(.09f,.05f,.09f),skin);
            var head=Pivot(body,"Baş",new Vector3(0,1.64f,0)); rig.head=head;
            P(head,"Kafa",PrimitiveType.Sphere,Vector3.zero,new Vector3(.215f,.25f,.235f),skin);
            P(head,"Çene",PrimitiveType.Sphere,new Vector3(0,-.07f,.02f),new Vector3(.17f,.15f,.18f),skin);
            P(head,"Burun",PrimitiveType.Sphere,new Vector3(0,-.015f,.115f),new Vector3(.035f,.045f,.045f),skin*.93f);
            foreach(float sx in new[]{-1f,1f})
            {
                P(head,"Göz",PrimitiveType.Sphere,new Vector3(sx*.045f,.025f,.1f),new Vector3(.034f,.026f,.02f),Color.white);
                P(head,"Gözbebeği",PrimitiveType.Sphere,new Vector3(sx*.045f,.025f,.109f),new Vector3(.016f,.016f,.01f),new Color(.07f,.05f,.04f));
                P(head,"Kaş",PrimitiveType.Cube,new Vector3(sx*.045f,.062f,.1f),new Vector3(.05f,.009f,.012f),hair*.8f,new Vector3(0,0,sx*-6));
                P(head,"Kulak",PrimitiveType.Sphere,new Vector3(sx*.108f,-.005f,0),new Vector3(.03f,.055f,.04f),skin*.95f);
            }
            P(head,"Ağız",PrimitiveType.Cube,new Vector3(0,-.07f,.105f),new Vector3(.05f,.008f,.01f),new Color(.55f,.22f,.22f));
            if(r.yas>=50)P(head,"Yaşlılık kırışığı",PrimitiveType.Cube,new Vector3(0,.045f,.11f),new Vector3(.1f,.004f,.004f),skin*.85f);
            // saç
            if(Has(hairN,"saçsız")){foreach(float sx in new[]{-1f,1f})P(head,"Yan saç",PrimitiveType.Sphere,new Vector3(sx*.1f,-.03f,-.02f),new Vector3(.04f,.07f,.07f),new Color(.82f,.82f,.82f));}
            else
            {
                P(head,"Saç tepesi",PrimitiveType.Sphere,new Vector3(0,.035f,-.028f),new Vector3(.235f,.245f,.255f),hair);
                if(Has(hairN,"bob")){P(head,"Bob arka",PrimitiveType.Cube,new Vector3(0,-.04f,-.07f),new Vector3(.22f,.2f,.11f),hair);foreach(float sx in new[]{-1f,1f})P(head,"Bob yan",PrimitiveType.Cube,new Vector3(sx*.105f,-.035f,-.01f),new Vector3(.03f,.19f,.16f),hair);}
                else if(Has(hairN,"uzun","omuz","dalgalı")){P(head,"Uzun saç",PrimitiveType.Capsule,new Vector3(0,-.15f,-.07f),new Vector3(.22f,.2f,.1f),hair);foreach(float sx in new[]{-1f,1f})P(head,"Saç tutamı",PrimitiveType.Capsule,new Vector3(sx*.1f,-.12f,0),new Vector3(.05f,.15f,.07f),hair);}
                else if(Has(hairN,"toplu","topuz"))P(head,"Topuz",PrimitiveType.Sphere,new Vector3(0,.12f,-.1f),Vector3.one*.1f,hair);
                if(Has(hairN,"kaşlı","uzun kaşlı"))P(head,"Perçem",PrimitiveType.Cube,new Vector3(.02f,.095f,.085f),new Vector3(.17f,.04f,.06f),hair,new Vector3(-20,0,-8));
                if(look.boy_cm>0&&Has(r.id,"insaat","balikci"))P(head,"Sakal",PrimitiveType.Sphere,new Vector3(0,-.1f,.045f),new Vector3(.15f,.07f,.1f),hair*.9f);
            }
            // kollar
            foreach(float sx in new[]{-1f,1f})
            {
                var sh=Pivot(body,"Omuz pivot",new Vector3(sx*(torsoW/2+.09f),1.42f,0)); var el=Pivot(sh,"Dirsek",new Vector3(0,-.3f,0));
                P(sh,"Üst kol",PrimitiveType.Capsule,new Vector3(0,-.14f,0),new Vector3(.1f,.16f,.1f),top);
                P(el,"Alt kol",PrimitiveType.Capsule,new Vector3(0,-.14f,0),new Vector3(.085f,.15f,.085f),shortSleeve?skin:top);
                bool gloves=Has(accN,"eldiven"); P(el,"El",PrimitiveType.Sphere,new Vector3(0,-.31f,0),Vector3.one*.085f,gloves?new Color(.2f,.2f,.22f):skin);
                if(sx<0)rig.shoulderL=sh; else rig.shoulderR=sh; if(sx<0)rig.elbowL=el; else rig.elbowR=el;
                if(sx<0&&Has(accN,"saat"))P(el,"Saat",PrimitiveType.Cylinder,new Vector3(0,-.26f,0),new Vector3(.095f,.015f,.095f),new Color(.1f,.1f,.12f));
                if(sx>0&&Has(accN,"bilezik"))P(el,"Bilezik",PrimitiveType.Cylinder,new Vector3(0,-.26f,0),new Vector3(.095f,.01f,.095f),new Color(.85f,.75f,.65f));
            }
            // bacaklar
            Color shoe=Has(shoeN,"beyaz")?new Color(.92f,.92f,.92f):Has(shoeN,"deri","oxford")?new Color(.22f,.13f,.08f):Has(shoeN,"çelik")?new Color(.35f,.28f,.15f):Has(shoeN,"sneaker")?new Color(.7f,.7f,.72f):Has(shoeN,"klok")?new Color(.9f,.9f,.9f):new Color(.15f,.15f,.16f);
            bool boot=Has(shoeN,"bot","çizme");
            foreach(float sx in new[]{-1f,1f})
            {
                var hip=Pivot(body,"Kalça",new Vector3(sx*.1f,.92f,0)); var knee=Pivot(hip,"Diz",new Vector3(0,-.46f,0));
                P(hip,"Üst bacak",PrimitiveType.Capsule,new Vector3(0,-.23f,0),new Vector3(.15f,.24f,.15f),skirt?skin:bottom);
                P(knee,"Alt bacak",PrimitiveType.Capsule,new Vector3(0,-.21f,0),new Vector3(.12f,.23f,.12f),shorts||skirt?skin:bottom);
                if(boot)P(knee,"Konç",PrimitiveType.Cylinder,new Vector3(0,-.37f,0),new Vector3(.14f,.07f,.14f),shoe);
                P(knee,"Ayakkabı",PrimitiveType.Cube,new Vector3(0,-.43f,.05f),new Vector3(.115f,.07f,.27f),shoe);
                if(sx<0){rig.hipL=hip;rig.kneeL=knee;}else{rig.hipR=hip;rig.kneeR=knee;}
            }
            // aksesuarlar
            if(Has(accN,"kolye"))P(body,"Kolye",PrimitiveType.Sphere,new Vector3(0,1.38f,.1f),Vector3.one*.03f,new Color(.9f,.75f,.3f));
            if(Has(accN,"gözlük")){foreach(float sx in new[]{-1f,1f})P(head,"Gözlük camı",PrimitiveType.Cylinder,new Vector3(sx*.048f,.025f,.112f),new Vector3(.062f,.004f,.062f),new Color(.1f,.1f,.12f),new Vector3(90,0,0));P(head,"Gözlük köprü",PrimitiveType.Cube,new Vector3(0,.03f,.115f),new Vector3(.03f,.006f,.006f),new Color(.1f,.1f,.12f));}
            if(Has(accN,"şapka")){P(head,"Şapka",PrimitiveType.Cylinder,new Vector3(0,.115f,-.01f),new Vector3(.25f,.05f,.26f),new Color(.78f,.65f,.4f));P(head,"Şapka kenarı",PrimitiveType.Cylinder,new Vector3(0,.075f,-.01f),new Vector3(.36f,.008f,.37f),new Color(.78f,.65f,.4f));}
            if(Has(accN,"kulaklık")){foreach(float sx in new[]{-1f,1f})P(head,"Kulaklık",PrimitiveType.Sphere,new Vector3(sx*.118f,0,0),new Vector3(.05f,.08f,.08f),new Color(.12f,.12f,.14f));P(head,"Kulaklık bandı",PrimitiveType.Cube,new Vector3(0,.125f,0),new Vector3(.24f,.012f,.02f),new Color(.12f,.12f,.14f));}
            if(Has(accN,"çanta"))P(body,"Çanta",PrimitiveType.Cube,new Vector3(torsoW/2+.18f,.98f,0),new Vector3(.12f,.2f,.28f),new Color(.45f,.55f,.7f));
            if(Has(accN,"yüzük")||Has(accN,"kabuğu"))P(body,"Küçük süs",PrimitiveType.Sphere,new Vector3(-.3f,.9f,.02f),Vector3.one*.025f,new Color(.9f,.8f,.5f));
            return rig;
        }
    }
}
