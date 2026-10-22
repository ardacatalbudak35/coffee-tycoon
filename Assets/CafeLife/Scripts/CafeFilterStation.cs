using UnityEngine;

namespace CafeLife
{
    // Barın üstündeki filtre kahve makinesi: bardak makineye konur, kahve 20 saniyede demlenir, hazır olunca bardak geri alınır.
    // Filtre Kahve, Red Eye, Black Eye ve Dead Eye bu makineyi kullanır (espresso makinesini değil).
    public sealed partial class CafeWorld
    {
        public const float FilterSeconds=20f;
        Transform filterRoot; GameObject filterPot,filterCupModel; Renderer filterLamp; Material lampRed,lampGreen;

        void BuildFilterStation(Vector3 at)
        {
            var root=new GameObject("Filtre kahve makinesi"); root.transform.SetParent(transform); root.transform.position=at; filterRoot=root.transform;
            GameObject Part(string n,Vector3 local,Vector3 size,Color c)
            { var o=Block(n,at+local,size,c); o.transform.SetParent(root.transform); Destroy(o.GetComponent<Collider>()); return o; }
            var steel=new Color(.13f,.14f,.15f); var brass=new Color(.78f,.58f,.26f);
            Part("Filtre makinesi tabanı",new Vector3(0,.02f,0),new Vector3(.42f,.04f,.3f),steel);                    // ısıtıcı plaka
            Part("Filtre makinesi gövdesi",new Vector3(-.15f,.27f,-.08f),new Vector3(.12f,.5f,.12f),steel);          // arka sütun
            Part("Filtre makinesi başlığı",new Vector3(-.04f,.46f,-.02f),new Vector3(.34f,.12f,.22f),steel);         // su haznesi/filtre başlığı
            Part("Filtre makinesi pirinç şerit",new Vector3(-.04f,.46f,.095f),new Vector3(.32f,.02f,.02f),brass);
            Part("Filtre makinesi damla borusu",new Vector3(.04f,.38f,0),new Vector3(.03f,.07f,.03f),steel);
            filterPot=Round("Filtre kahve sürahisi",at+new Vector3(.04f,.15f,0),new Vector3(.17f,.11f,.17f),new Color(.30f,.15f,.07f,.62f)); filterPot.transform.SetParent(root.transform); Destroy(filterPot.GetComponent<Collider>());
            Part("Sürahi sapı",new Vector3(.15f,.16f,0),new Vector3(.03f,.1f,.05f),steel);
            var lamp=Part("Filtre makinesi ışığı",new Vector3(.12f,.3f,.115f),new Vector3(.03f,.03f,.01f),new Color(1f,.25f,.15f));
            lampRed=EmissiveMat(new Color(1f,.25f,.15f),3f); lampGreen=EmissiveMat(new Color(.2f,1f,.35f),3.5f); filterLamp=lamp.GetComponent<Renderer>(); filterLamp.sharedMaterial=lampRed;
            Target(root,"filter");
        }

        // Bardağı makinenin plakasına koyar (sürahi gizlenir), ışık kırmızı: demleniyor
        public void SetFilterCup(CupWork cup)
        {
            ClearFilterCup(); if(!filterRoot||cup==null)return;
            filterCupModel=CupModel(cup,Economy.Recipes[cup.recipe],transform,false);
            filterCupModel.transform.position=filterRoot.position+new Vector3(.04f,.125f,0); filterCupModel.transform.rotation=Quaternion.identity;
            if(filterPot)filterPot.SetActive(false); if(filterLamp)filterLamp.sharedMaterial=lampRed;
        }
        public void SetFilterReady(bool ready){ if(filterLamp)filterLamp.sharedMaterial=ready?lampGreen:lampRed; }
        public void ClearFilterCup()
        {
            if(filterCupModel){ filterCupModel.SetActive(false); Destroy(filterCupModel); filterCupModel=null; }
            if(filterPot)filterPot.SetActive(true); if(filterLamp)filterLamp.sharedMaterial=lampRed;
        }
    }
}
