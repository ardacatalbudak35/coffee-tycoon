using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CafeLife
{
    // Bar arkası düzeni: arka tezgâhtaki boş proplar kalkar, filtre makinesi ve arıtma çeşmesi tezgâhın üstüne gelir,
    // arka tezgâh duvara kadar uzar, süt için espresso makinesinin altına camlı bar dolabı konur.
    // Her şey oluştuktan sonra (CafeGame.Start sonunda) çağrılır.
    public sealed partial class CafeWorld
    {
        const float BackTopY=.97f;       // arka hazırlık tezgâhı taş yüzeyi
        const float BackZ=7.74f;         // tezgâhın ortası
        const float BackFront=8.01f, BackWall=8.875f;   // tezgâhın eski arka kenarı / arka duvarın iç yüzü

        // Boş süs propları: kavanozlar, kapakları, fincanlar, altlıklar, bakım bezi. CafePolish.MergeScenery'den ÖNCE çağrılır.
        public void RemoveBackBarProps()
        {
            var remove=new HashSet<string>{"Kahve kavanozu","Kavanoz ahşap kapak","Sergi fincanı","Fincan altlığı","Barista bakım bezi"};
            foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(x=>x&&remove.Contains(x.name)).ToArray())if(t){ t.gameObject.SetActive(false); Destroy(t.gameObject); }
        }

        public void RearrangeBackBar()
        {
            // 1) Süt buzdolabı ve su sebili artık yok (yenileri aşağıda kurulur)
            foreach(var tgt in GetComponentsInChildren<CafeTarget>(true))if(tgt&&(tgt.kind=="milk"||tgt.kind=="water"))tgt.gameObject.SetActive(false);

            // 2) Arka tezgâh duvara kadar uzar
            ExtendBackCounter();

            // 3) Arka tezgâhın üstü: arıtma çeşmesi ve filtre kahve makinesi
            BuildWaterFaucet(new Vector3(-1.3f,BackTopY,BackZ-.02f));
            if(filterRoot)filterRoot.position=new Vector3(.6f,BackTopY,BackZ);

            // 4) Süt dolabı: espresso makinesinin altına, bar gövdesinin arka yüzüne camlı bar dolabı
            BuildMilkCabinet(new Vector3(-.55f,0,5.6f));
        }

        GameObject Prop(Transform root,Vector3 at,string n,Vector3 local,Vector3 size,Color c)
        { var o=Block(n,at+local,size,c); o.transform.SetParent(root); Destroy(o.GetComponent<Collider>()); return o; }
        static Material Steel(Color c,float metallic=.9f,float smooth=.72f)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor",c); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smooth); return m; }
        // iki nokta arasında silindir (çubuk): çeşme boynu, kulplar
        GameObject Rod(Transform root,string n,Vector3 a,Vector3 b,float radius,Material m)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder); o.name=n; o.transform.SetParent(root); Destroy(o.GetComponent<Collider>());
            o.transform.position=(a+b)/2; o.transform.rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized); o.transform.localScale=new Vector3(radius*2,(b-a).magnitude/2,radius*2);
            o.GetComponent<Renderer>().sharedMaterial=m; return o;
        }

        // Arka hazırlık tezgâhı: eski arka kenarından duvara kadar dolap gövdesi ve taş yüzey eklenir
        void ExtendBackCounter()
        {
            Material top=null,body=null;
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if(!r||!r.transform.parent||r.transform.parent.name!="Düzenli arka hazırlık tezgâhı")continue;
                if(r.name=="Arka tezgâh taş yüzey")top=r.sharedMaterial;
                else if(body==null&&r.bounds.size.x>3f&&r.bounds.size.y>.5f)body=r.sharedMaterial;
            }
            float depth=BackWall-BackFront, cz=(BackWall+BackFront)/2;
            var b=Block("Arka tezgâh uzantısı gövde",new Vector3(-.6f,.47f,cz),new Vector3(4.5f,.94f,depth),new Color(.06f,.05f,.06f)); if(body)b.GetComponent<Renderer>().sharedMaterial=body;
            var t=Block("Arka tezgâh uzantısı yüzey",new Vector3(-.6f,.94f,cz),new Vector3(4.5f,.06f,depth),new Color(.07f,.08f,.1f)); if(top)t.GetComponent<Renderer>().sharedMaterial=top;
        }

        // Arıtma çeşmesi: paslanmaz çelik, kıvrık boyunlu, yan kollu mutfak çeşmesi; altında küçük damlalık
        void BuildWaterFaucet(Vector3 at)
        {
            var root=new GameObject("Arıtma çeşmesi").transform; root.SetParent(transform); root.position=at;
            var steel=Steel(new Color(.76f,.78f,.8f)); var dark=new Color(.1f,.11f,.12f);
            Prop(root,at,"Çeşme damlalık tepsisi",new Vector3(.21f,.012f,0),new Vector3(.2f,.024f,.2f),new Color(.6f,.62f,.64f));    // uç, tepsinin üstüne gelir
            Prop(root,at,"Çeşme ızgarası",new Vector3(.21f,.027f,0),new Vector3(.16f,.008f,.16f),dark);
            var baseDisc=Round("Çeşme tabanı",at+new Vector3(0,.016f,0),new Vector3(.075f,.016f,.075f),Color.white); baseDisc.transform.SetParent(root); Destroy(baseDisc.GetComponent<Collider>()); baseDisc.GetComponent<Renderer>().sharedMaterial=steel;
            // gövde: tabandan yukarı dik boru, sonra öne (oyuncuya, -z) kavis çizip aşağı inen uç
            const float r0=.016f,stem=.34f,arc=.105f; var tip=new List<Vector3>{at+new Vector3(0,.03f,0),at+new Vector3(0,.03f+stem,0)};
            for(int i=1;i<=8;i++){ float a=Mathf.PI*i/8f; tip.Add(at+new Vector3(arc-Mathf.Cos(a)*arc,.03f+stem+Mathf.Sin(a)*arc,0)); }   // kavis: oyuncuya göre sağa, böylece yandan bakınca ark görünür
            tip.Add(tip[tip.Count-1]+Vector3.down*.1f);                                                                                          // aşağı bakan uç
            for(int i=0;i<tip.Count-1;i++)Rod(root,"Çeşme boyun parçası",tip[i],tip[i+1],r0,steel);
            var nozzle=Rod(root,"Çeşme ucu",tip[tip.Count-1],tip[tip.Count-1]+Vector3.down*.012f,.016f,Steel(new Color(.55f,.57f,.6f)));
            // yan kol: gövdeden yana doğru uzanıp yukarı kalkan küçük çubuk
            var lowStem=at+new Vector3(0,.12f,0); var armEnd=at+new Vector3(-.075f,.12f,0);
            Rod(root,"Çeşme kolu",lowStem,armEnd,.009f,steel); Rod(root,"Çeşme kolu ucu",armEnd,armEnd+new Vector3(-.01f,.075f,0),.008f,steel);
            Target(root.gameObject,"water");
        }

        // Süt dolabı: gerçek bir camlı bar dolabı. Siyah çerçeve, çift cam kapak, iç ışık, raflarda süt kutuları.
        void BuildMilkCabinet(Vector3 at)
        {
            var root=new GameObject("Süt dolabı").transform; root.SetParent(transform); root.position=at;
            const float w=1.0f,h=.86f,d=.36f,y0=.05f; float z0=at.z,zf=z0+d,cx=at.x;     // z0: bar gövdesinin arka yüzü, zf: kapak yüzü
            var black=new Color(.03f,.03f,.035f); var inner=new Color(.86f,.88f,.9f); var glass=new Color(.78f,.88f,.92f,.1f);
            void P(string n,Vector3 c,Vector3 s,Color col)=>Prop(root,Vector3.zero,n,c,s,col);
            // kabuk: üst, alt, yanlar, arka panel
            P("Dolap üstü",new Vector3(cx,y0+h-.02f,z0+d/2),new Vector3(w,.04f,d),black); P("Dolap altı",new Vector3(cx,y0+.02f,z0+d/2),new Vector3(w,.04f,d),black);
            P("Dolap sol yanı",new Vector3(cx-w/2+.02f,y0+h/2,z0+d/2),new Vector3(.04f,h,d),black); P("Dolap sağ yanı",new Vector3(cx+w/2-.02f,y0+h/2,z0+d/2),new Vector3(.04f,h,d),black);
            var backPanel=Prop(root,Vector3.zero,"Dolap iç arka paneli",new Vector3(cx,y0+h/2,z0+.012f),new Vector3(w-.08f,h-.08f,.02f),inner); backPanel.GetComponent<Renderer>().sharedMaterial=EmissiveMat(new Color(.8f,.86f,.9f),1.1f);   // aydınlık iç
            var cool=new GameObject("Dolap iç lambası").AddComponent<Light>(); cool.transform.SetParent(root,false); cool.transform.position=new Vector3(cx,y0+h*.62f,z0+d-.1f); cool.type=LightType.Point; cool.range=1.4f; cool.intensity=2.4f; cool.color=new Color(.9f,.96f,1f);
            var lightBar=Prop(root,Vector3.zero,"Dolap iç ışığı",new Vector3(cx,y0+h-.055f,z0+d-.07f),new Vector3(w-.14f,.012f,.03f),Color.white); lightBar.GetComponent<Renderer>().sharedMaterial=EmissiveMat(new Color(.95f,.97f,1f),2.2f);
            // raflar ve süt kutuları
            var carton=new Color(.95f,.95f,.97f); var cap=new Color(.2f,.45f,.85f); var shelf=new Color(.18f,.18f,.2f);
            float[] levels={y0+.04f,y0+.30f,y0+.56f};
            for(int s=1;s<levels.Length;s++)P("Dolap rafı",new Vector3(cx,levels[s],z0+d/2),new Vector3(w-.08f,.012f,d-.04f),shelf);
            foreach(float ly in levels)for(int row=0;row<2;row++)for(int i=0;i<6;i++)
            {
                float x=cx-w/2+.12f+i*.15f,z=z0+.1f+row*.12f,y=ly+.006f+.1f; P("Süt kutusu",new Vector3(x,y,z),new Vector3(.08f,.2f,.075f),carton); P("Süt kutusu kapağı",new Vector3(x,y+.105f,z),new Vector3(.06f,.012f,.055f),cap);
            }
            // çift cam kapak: ince siyah çerçeveli, orta ayırıcı ve dikey kulplar
            float dh=h-.1f,dw=w/2-.045f,zc=zf+.004f;
            foreach(float side in new[]{-1f,1f})
            {
                float x=cx+side*(w/4-.0f);
                P("Cam kapak",new Vector3(x,y0+h/2,zc),new Vector3(dw,dh,.01f),glass);
                P("Kapak çerçevesi üst",new Vector3(x,y0+h/2+dh/2,zc+.004f),new Vector3(dw+.02f,.03f,.022f),black); P("Kapak çerçevesi alt",new Vector3(x,y0+h/2-dh/2,zc+.004f),new Vector3(dw+.02f,.03f,.022f),black);
                P("Kapak çerçevesi dış",new Vector3(x+side*dw/2,y0+h/2,zc+.004f),new Vector3(.03f,dh,.022f),black);
                var h2=Prop(root,Vector3.zero,"Kapak kulpu",new Vector3(cx+side*.045f,y0+h/2,zf+.03f),new Vector3(.014f,.34f,.02f),black); h2.GetComponent<Renderer>().sharedMaterial=Steel(new Color(.12f,.12f,.13f),.4f,.6f);
            }
            P("Kapak orta ayırıcı",new Vector3(cx,y0+h/2,zc+.004f),new Vector3(.03f,dh,.024f),black);
            // alt havalandırma ızgarası ve küçük kırmızı gösterge
            P("Havalandırma ızgarası",new Vector3(cx+.05f,y0+.065f,zf+.004f),new Vector3(w-.28f,.05f,.012f),new Color(.07f,.07f,.08f));
            var led=Prop(root,Vector3.zero,"Dolap göstergesi",new Vector3(cx-.4f,y0+.065f,zf+.012f),new Vector3(.05f,.018f,.01f),new Color(1f,.15f,.1f)); led.GetComponent<Renderer>().sharedMaterial=EmissiveMat(new Color(1f,.15f,.1f),3f);
            var body=root.gameObject.AddComponent<BoxCollider>(); body.center=new Vector3(cx-root.position.x,y0+h/2,z0+d/2-root.position.z); body.size=new Vector3(w,h,d);   // içinden geçilmesin
            Target(root.gameObject,"milk");
        }
    }
}
