using UnityEngine;

namespace CafeLife
{
    // NOIR kulübü ve casino katı dekoru. Kulüp kabuğu: x 4..24, z -41..-23. Alt kat tavanı ~3.96, casino zemini y=4.2, tavan ~8.
    public sealed partial class CafeWorld
    {
        Material EmissiveMat(Color c,float intensity)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",c*.12f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",c*intensity); m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None; return m;
        }
        // Işık yayan, çarpışması olmayan süs parçası (neon şerit, ekran, lamba)
        GameObject Glow(string n,Vector3 pos,Vector3 size,Color c,float intensity=3f,PrimitiveType shape=PrimitiveType.Cube)
        {
            var o=GameObject.CreatePrimitive(shape); o.name=n; o.transform.SetParent(transform); o.transform.position=pos; o.transform.localScale=size;
            Destroy(o.GetComponent<Collider>()); o.GetComponent<Renderer>().sharedMaterial=EmissiveMat(c,intensity); return o;
        }
        // Çarpışması olmayan sade süs parçası
        GameObject Deco(string n,Vector3 pos,Vector3 size,Color c){ var o=Block(n,pos,size,c); Destroy(o.GetComponent<Collider>()); return o; }
        GameObject TryModel(string name,Vector3 at,float width,float yaw=0,bool solid=false){ try{ return Model(name,at,width,yaw,solid); }catch(System.Exception){ return null; } }

        void DecorateClub()
        {
            var pink=new Color(1f,.1f,.75f); var cyan=new Color(.1f,.85f,1f); var violet=new Color(.55f,.2f,1f); var gold=new Color(.85f,.62f,.26f); var warm=new Color(1f,.7f,.35f);
            var black=new Color(.03f,.025f,.05f); var wood=new Color(.16f,.09f,.06f);

            // neon şeritler: arka ve yan duvarlar, tavan altı ve zemin üstü
            Glow("NOIR neon · arka üst",new Vector3(14,3.65f,-40.78f),new Vector3(18.5f,.09f,.06f),pink,3.5f); Glow("NOIR neon · arka alt",new Vector3(14,.3f,-40.78f),new Vector3(18.5f,.07f,.06f),cyan,3f);
            Glow("NOIR neon · batı",new Vector3(4.2f,3.65f,-32),new Vector3(.06f,.09f,17.5f),cyan,3.5f); Glow("NOIR neon · doğu",new Vector3(23.8f,3.65f,-32),new Vector3(.06f,.09f,17.5f),pink,3.5f);
            for(int i=0;i<5;i++)Glow("NOIR dikey neon",new Vector3(6+i*4,1.9f,-40.78f),new Vector3(.06f,3f,.06f),i%2==0?violet:cyan,2.6f);

            // akustik panel: batı duvarında dikey çıtalar
            for(int i=0;i<12;i++)Deco("NOIR akustik çıta",new Vector3(4.2f,1.7f,-24.4f-i*1.35f),new Vector3(.12f,2.9f,.42f),i%2==0?new Color(.12f,.08f,.16f):new Color(.07f,.05f,.1f));

            // DJ kabini: LED ön yüz, ekran ve ses çubukları, tavan kirişi ve ışıkları
            Glow("DJ ön LED",new Vector3(14,.78f,-38.48f),new Vector3(3f,.28f,.03f),pink,3.5f);
            Deco("DJ arka ekran",new Vector3(14,2.4f,-40.72f),new Vector3(6.2f,2.2f,.1f),black);
            for(int i=0;i<24;i++){ var c=Color.Lerp(cyan,pink,i/23f); var bar=Glow("DJ ekran ses çubuğu",new Vector3(11.5f+i*.2f,1.7f,-40.62f),new Vector3(.13f,.5f,.04f),c,3.2f); }
            Deco("NOIR ışık kirişi",new Vector3(14,3.45f,-38.5f),new Vector3(8,.12f,.12f),new Color(.12f,.12f,.14f));
            for(int i=0;i<7;i++)Glow("NOIR kiriş lambası",new Vector3(10.5f+i*1,3.35f,-38.5f),Vector3.one*.14f,i%3==0?pink:i%3==1?cyan:violet,4f,PrimitiveType.Sphere);

            // bar: arka raf, rafları aydınlatan sıcak şerit, şişeler, neon yazı, taburelere ve tezgâh altına LED
            Deco("NOIR bar arka raf",new Vector3(7,1.45f,-40.6f),new Vector3(4.6f,2.9f,.35f),wood);
            foreach(float y in new[]{1.35f,1.85f,2.35f}){ Deco("NOIR bar rafı",new Vector3(7,y,-40.38f),new Vector3(4.4f,.05f,.3f),new Color(.3f,.18f,.1f)); Glow("NOIR raf ışığı",new Vector3(7,y+.03f,-40.32f),new Vector3(4.3f,.025f,.04f),warm,3f);
                for(int i=0;i<8;i++)TryModel("bottle",new Vector3(5.1f+i*.5f,y+.03f,-40.38f),.16f); }
            Sign("B A R",new Vector3(7,3.15f,-40.72f),.14f,pink,180);
            Glow("NOIR tezgâh LED",new Vector3(7,.14f,-37.42f),new Vector3(4f,.04f,.04f),cyan,3f);
            for(int i=0;i<4;i++){ Round("NOIR bar taburesi",new Vector3(5.5f+i*.95f,.3f,-36.8f),new Vector3(.4f,.3f,.4f),new Color(.14f,.1f,.2f)); Glow("NOIR tabure halkası",new Vector3(5.5f+i*.95f,.62f,-36.8f),new Vector3(.42f,.03f,.42f),violet,2.5f,PrimitiveType.Cylinder); }

            // sağ duvar boyunca lounge: kanepeler, sehpa, ışıklı çizgi
            // (kulüp merdiveni doğu tarafta olduğu için lounge batı duvarında)
            for(int i=0;i<2;i++){ TryModel("VNB Sofa",new Vector3(5.3f,0,-27.5f-i*4),1.85f,90,true); TryModel("VNB Coffee Table",new Vector3(7.1f,0,-27.5f-i*4),1.47f,90,true); }
            Glow("NOIR lounge LED",new Vector3(4.4f,.12f,-29.5f),new Vector3(.04f,.04f,6.5f),violet,2.5f);

            // tavan: yıldız gibi minik renkli ışıklar
            var rng=new System.Random(5); for(int i=0;i<36;i++){ float x=Mathf.Lerp(5,23,(float)rng.NextDouble()),z=Mathf.Lerp(-40,-24,(float)rng.NextDouble()); Glow("NOIR tavan yıldızı",new Vector3(x,3.88f,z),Vector3.one*.08f,Color.Lerp(pink,cyan,(float)rng.NextDouble()),4f,PrimitiveType.Sphere); }

            // dış cephe: SMOKEBANK tabelasının neon çerçevesi
            Deco("SMOKEBANK tabela zemini",new Vector3(14,3.1f,-22.8f),new Vector3(11.5f,1f,.07f),black); // duvar yüzü z=-22.875, yazı z=-22.7: ikisinin arasında
            Glow("SMOKEBANK çerçeve üst",new Vector3(14,3.62f,-22.7f),new Vector3(11.5f,.07f,.05f),pink,3.5f); Glow("SMOKEBANK çerçeve alt",new Vector3(14,2.58f,-22.7f),new Vector3(11.5f,.07f,.05f),pink,3.5f);
            Glow("SMOKEBANK çerçeve sol",new Vector3(8.25f,3.1f,-22.7f),new Vector3(.07f,1.05f,.05f),pink,3.5f); Glow("SMOKEBANK çerçeve sağ",new Vector3(19.75f,3.1f,-22.7f),new Vector3(.07f,1.05f,.05f),pink,3.5f);

            DecorateCasino(pink,gold,warm,black);
        }

        void DecorateCasino(Color pink,Color gold,Color warm,Color black)
        {
            var red=new Color(.55f,.05f,.1f);
            Deco("Casino halısı",new Vector3(12,4.22f,-32),new Vector3(14.4f,.03f,16.4f),red);
            Glow("Casino halı çizgisi · ön",new Vector3(12,4.245f,-24.2f),new Vector3(14.4f,.02f,.1f),gold,1.6f); Glow("Casino halı çizgisi · arka",new Vector3(12,4.245f,-39.8f),new Vector3(14.4f,.02f,.1f),gold,1.6f);
            Glow("Casino halı çizgisi · sol",new Vector3(4.8f,4.245f,-32),new Vector3(.1f,.02f,15.6f),gold,1.6f); Glow("Casino halı çizgisi · sağ",new Vector3(19.2f,4.245f,-32),new Vector3(.1f,.02f,15.6f),gold,1.6f);

            // avizeler: altın halka ve etrafında sıcak ışık küreleri
            foreach(var c in new[]{new Vector3(8,7.1f,-33),new Vector3(15,7.1f,-33)})
            {
                Glow("Casino avize halkası",c,new Vector3(1.5f,.06f,1.5f),gold,1.2f,PrimitiveType.Cylinder);
                for(int k=0;k<8;k++){ float a=k*Mathf.PI/4; Glow("Casino avize ışığı",c+new Vector3(Mathf.Cos(a)*.72f,-.08f,Mathf.Sin(a)*.72f),Vector3.one*.16f,warm,5f,PrimitiveType.Sphere); }
                Glow("Casino avize merkezi",c+Vector3.down*.25f,Vector3.one*.28f,warm,5f,PrimitiveType.Sphere);
            }

            // arka duvar aplikleri
            for(int i=0;i<5;i++){ float x=6+i*3.2f; Deco("Casino aplik zemini",new Vector3(x,6f,-40.8f),new Vector3(.4f,.7f,.08f),gold); Glow("Casino aplik",new Vector3(x,6f,-40.72f),new Vector3(.22f,.5f,.06f),warm,4f); }

            // batı duvarı: slot makineleri
            for(int i=0;i<4;i++)
            {
                float z=-26.5f-i*2.1f; Deco("Slot makinesi gövde",new Vector3(4.7f,5.1f,z),new Vector3(.85f,1.8f,.75f),new Color(.28f,.03f,.07f));
                Glow("Slot makinesi ekran",new Vector3(5.14f,5.45f,z),new Vector3(.04f,.55f,.55f),i%2==0?new Color(.9f,.8f,.2f):new Color(.2f,.9f,.4f),3.5f);
                Glow("Slot makinesi kol ışığı",new Vector3(5.14f,4.85f,z),new Vector3(.04f,.12f,.5f),pink,3.5f);
                Deco("Slot makinesi tabure",new Vector3(5.7f,4.55f,z),new Vector3(.4f,.7f,.4f),new Color(.1f,.06f,.08f));
            }
        }
    }
}
