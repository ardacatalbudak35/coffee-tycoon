using UnityEngine;

namespace CafeLife
{
    // Günlük rastgele olaylar ve hava durumu: gün numarasından sabit üretilir (kayıtta alan tutulmaz, yeniden yüklemede aynı gün aynı sonuç çıkar).
    // Hikaye günlerinde (1-10) görevleri aksatmamak için çıkmaz; yalnızca 11. günden sonra ya da hikayesiz eski kayıtta.
    public static class CafeEvents
    {
        struct Ev { public string title; public float spawn; public Ev(string t,float s){title=t;spawn=s;} }
        static readonly Ev[] All={
            new Ev(null,1f),                                                        // 0 · sıradan gün
            new Ev(null,1f),
            new Ev("Kalabalık gün: sokakta festival var, müşteri akını bekleniyor",.7f),
            new Ev("Sakin gün: sokaklar boş, müşteri az",1.4f),
            new Ev("Gazeteci ziyareti: bugün dükkâna daha çok merak eden uğruyor",.8f),
            new Ev("Bayat gün: mahalle erken uykuya yattı, müşteri az",1.25f)
        };
        // Hava: 0-2 açık, 3-4 yağmurlu, 5 sisli
        static readonly string[] WeatherName={"açık","açık","açık","yağmurlu","yağmurlu","sisli"};
        static readonly float[] WeatherSpawn={1f,1f,1f,1.15f,1.15f,1.1f};

        static int Hash(int day,int salt){ unchecked{ int h=day*73856093^salt; h^=h>>13; h*=0x27d4eb2d; h^=h>>15; return h&0x7fffffff; } }
        static int Pick(int day)=>Hash(day,unchecked((int)0x5bd1e995))%All.Length;
        // Yağmur, CafeExpansion.Rainy ile aynı kural (her 3. gün): parçacıklar ve sıcak içecek eğilimi zaten ona bağlı. Yağmursuz günlerin dörtte biri sisli.
        static int Weather(int day)=>day%3==0?3:Hash(day,0x2545f491)%4==0?5:0;
        static bool On(SaveData d)=>d!=null&&(d.storyStage<0||d.day>10);

        // Yeni gün mesajına eklenecek metin (olay ve hava); yoksa null
        public static string Title(SaveData d)
        {
            if(!On(d))return null; string ev=All[Pick(d.day)].title,w=WeatherName[Weather(d.day)];
            string hava=w=="açık"?null:"Hava "+w;
            return ev!=null&&hava!=null?ev+" · "+hava:ev??hava;
        }
        // Müşteri gelme aralığı çarpanı: küçük değer = daha sık müşteri
        public static float SpawnMultiplier(SaveData d)=>On(d)?All[Pick(d.day)].spawn*WeatherSpawn[Weather(d.day)]:1f;

        static bool fogSaved,origFog;
        // Sisli günde gerçek sis açar; sisli olmayan günde sahnenin özgün sis ayarına döner
        public static void ApplyWeather(SaveData d)
        {
            if(!fogSaved){ origFog=RenderSettings.fog; fogSaved=true; }
            bool foggy=On(d)&&WeatherName[Weather(d.day)]=="sisli";
            if(foggy){ RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Exponential; RenderSettings.fogDensity=.018f; RenderSettings.fogColor=new Color(.72f,.75f,.78f); }
            else RenderSettings.fog=origFog;
        }
    }
}
