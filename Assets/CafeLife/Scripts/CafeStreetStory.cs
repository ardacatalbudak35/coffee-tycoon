using System.Collections.Generic;
using UnityEngine;

namespace CafeLife
{
    // Sokak hikâyesi (SOKAK-PLANI.md, faz 1-3): eski kahvehane, saat kulesi, fincan çeşmesi ve 6. günden sonra gece mavi lambalar.
    // Mekânlar mevcut haber ve sakin hikâyelerine bağlı. Her mekânın ödülü bir kez alınır (SaveData.streetSeen bit maskesi).
    public sealed partial class CafeWorld
    {
        void BuildStreetStory()
        {
            Color wood=new Color(.22f,.15f,.11f),stone=new Color(.34f,.35f,.38f),gold=new Color(.85f,.62f,.26f),cream=new Color(.92f,.88f,.78f),dark=new Color(.055f,.045f,.09f);
            var street=game.GetComponent<CafeStreetStory>(); if(!street)street=game.gameObject.AddComponent<CafeStreetStory>(); street.Setup(game);

            // Eski kahvehane: market ile NOIR kulübünün arasındaki boşluk. Kapı aralığı ortada, ön cephe +z yönünde (sokağa bakar).
            Shell("Eski kahvehane",new Vector3(-2.5f,0,-28.5f),new Color(.2f,.16f,.14f),false,9,9);
            Sign("KAHVEHANE · 1962",new Vector3(-2.5f,3.3f,-23.75f),.06f,cream,180);
            var table=Block("Kahvehane masası",new Vector3(-2.5f,.4f,-29.5f),new Vector3(1.4f,.8f,1.4f),wood);
            Round("Kahvehane çaydanlığı",new Vector3(-2.5f,.95f,-29.5f),new Vector3(.3f,.15f,.3f),gold);
            for(int i=-1;i<=1;i+=2)Round("Kahvehane tabure",new Vector3(-2.5f+i*1.1f,.25f,-29.5f),new Vector3(.5f,.25f,.5f),wood);
            Wide(table,"street_kahvehane");

            // Saat kulesi: NOIR'un sağında, 19'da takılı kalmış saat.
            Block("Saat kulesi gövdesi",new Vector3(28,4.5f,-26),new Vector3(1.6f,9,1.6f),stone);
            Block("Saat kulesi kadranı",new Vector3(28,8,-25.1f),new Vector3(2.2f,2.2f,.2f),cream);
            Sign("XIX",new Vector3(28,8,-24.95f),.14f,dark,180);
            var plaque=Block("Saat kulesi plaketi",new Vector3(28,1.2f,-25.1f),new Vector3(.9f,.5f,.1f),gold);
            Wide(plaque,"street_saat");

            // Fincan çeşmesi: kahvehane ile kulüp arasındaki kaldırımda. 12 fincan = 12 mahalle sakini; renkleri CafeStreetStory günceller.
            var basin=Round("Fincan çeşmesi",new Vector3(2.2f,.2f,-22),new Vector3(2.6f,.2f,2.6f),stone);
            Round("Fincan çeşmesi sütunu",new Vector3(2.2f,.9f,-22),new Vector3(.35f,.5f,.35f),stone);
            int n=Mathf.Min(12,CafeResidents.All.Length);
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/Mathf.Max(1,n);
                var cup=Round("Çeşme fincanı · "+CafeResidents.All[i].id,new Vector3(2.2f+Mathf.Cos(a)*.95f,.47f,-22+Mathf.Sin(a)*.95f),new Vector3(.16f,.07f,.16f),cream);
                street.AddCup(cup.GetComponent<Renderer>(),CafeResidents.All[i]);
            }
            Wide(basin,"street_cesme");

            // Gece mavi lambalar: 6. gün haberi ("Işıklar mavi yanıyor") sonrası yanar; öncesinde şiddet 0.
            foreach(float x in new[]{-30f,-15f,0f,15f,30f})
            {
                var lamp=new GameObject("Mavi sokak lambası"); lamp.transform.SetParent(transform,false); lamp.transform.position=new Vector3(x,3.4f,-21.5f);
                var l=lamp.AddComponent<Light>(); l.type=LightType.Point; l.range=9; l.intensity=0; l.color=new Color(.35f,.55f,1f);
                street.AddLamp(l);
            }
        }
        // Etkileşim alanını genişletir (küçük nesnelere yaklaşınca E çıksın)
        void Wide(GameObject go,string kind)
        {
            Target(go,kind); var t=go.GetComponent<CafeTarget>();
            var near=go.AddComponent<CafeActivityArea>(); near.target=t; near.floorY=0; near.radius=2.3f;
        }
    }

    public sealed class CafeStreetStory : MonoBehaviour
    {
        CafeGame game; readonly List<Light> lamps=new List<Light>(); readonly List<Renderer> cupRenders=new List<Renderer>(); readonly List<CafeResidents.Resident> cupOwners=new List<CafeResidents.Resident>(); float timer;
        public void Setup(CafeGame g){ game=g; }
        public void AddLamp(Light l){ lamps.Add(l); }
        public void AddCup(Renderer r,CafeResidents.Resident owner){ cupRenders.Add(r); cupOwners.Add(owner); }

        public static string Prompt(string kind)
        {
            if(kind=="street_kahvehane")return "Eski kahvehane masası · bak [E]";
            if(kind=="street_saat")return "Saat kulesi · bak [E]";
            if(kind=="street_cesme")return "Fincan çeşmesi · bak [E]";
            return "Bak [E]";
        }

        static void Tint(Renderer r,Color c){ if(!r)return; var m=r.material; if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c); else if(m.HasProperty("_Color"))m.SetColor("_Color",c); }

        void Update()
        {
            if(!game||game.data==null)return;
            float want=game.data.day>=6?3.5f:0f; foreach(var l in lamps)if(l)l.intensity=want;
            timer-=Time.deltaTime; if(timer>0)return; timer=.5f;
            for(int i=0;i<cupRenders.Count;i++)
            {
                var s=CafeResidents.State(game,cupOwners[i]); bool gone=s.absentUntil>0&&s.absentUntil>=game.data.day;
                Tint(cupRenders[i],gone?new Color(.12f,.1f,.14f):s.cups>=19?new Color(.95f,.15f,.12f):s.met?new Color(.92f,.88f,.78f):new Color(.5f,.5f,.52f));
            }
        }

        void Reward(int bit,int coin,string id,string title,string text)
        {
            var d=game.data; if((d.streetSeen&bit)!=0)return; d.streetSeen|=bit; d.money+=coin; d.earned+=coin;
            game.story.AddJournal(id,title,text); game.Tell(title+" · +"+coin+" coin",true);
        }

        public void Use(string kind)
        {
            var st=game?game.story:null; if(st==null)return; var d=game.data;
            if(kind=="street_kahvehane")
            {
                if(d.finale) st.Talk(()=>Reward(2,50,"sokak_masa","Nuri'nin masası","1962'de Hasan ve Nuri bu masaya oturup anlaşmayı imzaladı. Masada iki fincan izi var, biri hiç kurumamış."),
                    "Arda","Nuri'nin anlattığı masa bu. İki fincan izi var, biri hâlâ ıslak.","Arda","Anlaşmayı bozmak için önce yazıldığı yeri görmek gerekiyordu. Burası.");
                else st.Talk(()=>Reward(1,25,"sokak_kahvehane","Eski kahvehane","Otuz yıldır kapalı denen kahvehanenin kapısı aralık, çaydanlığı sıcak. Masanın altında tebeşirle 19 yazıyor."),
                    "Arda","Otuz yıldır kepenkleri inik derlerdi. Kapı aralık, içeride tek masa, çaydanlık hâlâ sıcak.","Arda","Masanın altına tebeşirle bir sayı yazılmış: 19. Kimse yok. Belki de hepsi buradaydı.");
            }
            else if(kind=="street_saat")
            {
                st.Talk(()=>Reward(4,15,"sokak_saat","Saat kulesi","Saat on dokuzda takılı. Akrebin altında minik bir fincan oyması var."),
                    "Arda","Saat kulesi on dokuzda takılı kalmış. Akrebin altında minik bir fincan oyması var.","Arda","Şehir saati geri kaldı demişlerdi. Belki saat geri kalmıyor, biz hızlanıyoruz.");
            }
            else if(kind=="street_cesme")
            {
                int risk=0,gone=0; foreach(var r in cupOwners){ var s=CafeResidents.State(game,r); if(s.absentUntil>0&&s.absentUntil>=d.day)gone++; else if(s.cups>=19)risk++; }
                string line=(risk>0?risk+" kişi 19. fincanda, dikkat.":"Kimse sınırda değil.")+(gone>0?" "+gone+" kişi bu gece gölge.":"");
                st.Talk(()=>Reward(8,10,"sokak_cesme","Fincan çeşmesi","Çeşmedeki her fincan bir mahalle sakini. Kırmızı olan 19'da, koyu olan gölge."),
                    "Arda","Fincan çeşmesi: her fincan bir mahalle sakini. Kırmızı olan 19. fincanda, koyu olan bu gece gölge.","Arda",line);
            }
        }
    }
}
