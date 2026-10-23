namespace CafeLife
{
    // Günlük rastgele olaylar: gün numarasından sabit üretilir (kayıtta alan tutulmaz, yeniden yüklemede aynı gün aynı olay çıkar).
    // Hikaye günlerinde (1-10) görevleri aksatmamak için olay çıkmaz; yalnızca 11. günden sonra ya da hikayesiz eski kayıtta.
    public static class CafeEvents
    {
        struct Ev { public string title; public float spawn; public Ev(string t,float s){title=t;spawn=s;} }
        static readonly Ev[] All={
            new Ev(null,1f),                                                        // 0 · sıradan gün
            new Ev(null,1f),
            new Ev("Kalabalık gün: sokakta festival var, müşteri akını bekleniyor",.7f),
            new Ev("Sakin gün: hava yağmurlu, sokaklar boş",1.4f),
            new Ev("Gazeteci ziyareti: bugün dükkâna daha çok merak eden uğruyor",.8f),
            new Ev("Bayat gün: mahalle erken uykuya yattı, müşteri az",1.25f)
        };
        static int Pick(int day){ unchecked{ int h=day*73856093^unchecked((int)0x5bd1e995); h^=h>>13; h*=0x27d4eb2d; h^=h>>15; return (h&0x7fffffff)%All.Length; } }
        static bool On(SaveData d)=>d!=null&&(d.storyStage<0||d.day>10);
        public static string Title(SaveData d)=>On(d)?All[Pick(d.day)].title:null;
        // Müşteri gelme aralığı çarpanı: küçük değer = daha sık müşteri
        public static float SpawnMultiplier(SaveData d)=>On(d)?All[Pick(d.day)].spawn:1f;
    }
}
