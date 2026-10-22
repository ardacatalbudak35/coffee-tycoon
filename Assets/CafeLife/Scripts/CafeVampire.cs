using UnityEngine;

namespace CafeLife
{
    // Final: 5. günde kapının önünde uzun paltolu yabancı (Mixamo "Vampire") bekler; dükkânın ilk müşterisidir. Konuşunca hikâyenin sırrı açılır.
    public sealed class CafeVampire : MonoBehaviour
    {
        CafeGame game; GameObject body; static readonly Vector3 Spot=new Vector3(1.3f,0,-11.3f);
        public void Setup(CafeGame owner){game=owner;}
        public string Label()=>"Uzun paltolu yabancı · konuş [E]";

        void Update()
        {
            if(!game||game.Paused||CafeStory.Busy||game.expansion==null||!CafeStory.Active)return;
            bool want=game.data.day>=5&&!game.data.finale&&game.data.storyStage>=4&&game.expansion.Data.open;
            if(want&&!body)Spawn(); else if(!want&&body)Remove();
        }
        void Spawn()
        {
            var prefab=Resources.Load<GameObject>("CafeMixamo/Vampire"); if(!prefab)return;
            body=new GameObject("Uzun paltolu yabancı"); body.transform.SetParent(game.world.transform,false); body.transform.position=Spot; body.transform.rotation=Quaternion.LookRotation(new Vector3(-.2f,0,1f));
            var avatar=Instantiate(prefab,body.transform); game.world.PrepareAvatar(avatar); foreach(var c in avatar.GetComponentsInChildren<Collider>())Destroy(c);
            var anim=avatar.GetComponentInChildren<Animator>(); if(anim){anim.applyRootMotion=false; var ctrl=CafeResidents.MixamoController(); if(ctrl)anim.runtimeAnimatorController=ctrl; anim.SetBool("Moving",false); anim.speed=.6f;}
            var hit=new GameObject("Yabancı etkileşim alanı"); hit.layer=8; hit.transform.SetParent(body.transform,false); hit.transform.localPosition=Vector3.up*1f;
            var col=hit.AddComponent<CapsuleCollider>(); col.isTrigger=true; col.height=2f; col.radius=.7f; hit.AddComponent<CafeTarget>().kind="cc_vampire";
            game.Tell("Kapının önünde uzun paltolu biri bekliyor…");
        }
        void Remove(){ if(body)Destroy(body); body=null; }

        public void Use()
        {
            if(!body||game.data.finale||game.story==null)return;
            game.story.Talk(()=>{ game.story.AddJournal("finale","Nuri'nin anlattıkları","1962'de Hasan ve Nuri bir anlaşma yaptı. 20. fincanı içen gölge olur; gölgeler fincanı yarım bırakılmış hatıralar. Kimseye 20. fincanı verme. Dükkân artık benim, nöbet bende."); game.data.finale=true; game.data.money+=200; game.data.earned+=200; game.Tell("Hasan Dede'nin sırrı çözüldü · +200 coin",true); Remove(); },
                "Yabancı","Hasan'ın torunu. Seni uzun zamandır bekliyordum.",
                "Yabancı","Ben bu dükkânın ilk müşterisiyim. 1962'de ilk fincanı bana verdi. Yirmincisini de ben içtim.",
                "Yabancı","Yirmincide insan zamanı geriye sayar, evladım. Ben hâlâ sayıyorum. Hasan benim yerime nöbet tuttu.",
                "Yabancı","Gölgeler, fincanı yarım bırakılmış insanlardır. Bu mahalle onları unutmasın diye sen kahve yapıyorsun. Dede bunu bilirdi.",
                "Arda","Peki ben ne yapacağım?",
                "Yabancı","Kimseye 20. fincanı verme. Gerisini zaman çözer. Dükkân artık senin, Arda. Hasan bu anı görseydi gülümserdi.",
                "Arda","Dedem bana bir kafe değil, bir nöbet bırakmış. Ben de nöbeti sürdüreceğim.");
        }
    }
}
