using System;
using System.Linq;
using UnityEngine;
namespace DarkSaver.Original.Entry
{
    // Explicit asset references keep authored training layouts available in Player builds.
    public sealed class TrainingTestLayoutCatalog : ScriptableObject
    {
        public int[] maps=Array.Empty<int>();
        public TextAsset[] layouts=Array.Empty<TextAsset>();
        public TextAsset Find(int map)
        {int index=maps==null?-1:Array.IndexOf(maps,map);return index<0||layouts==null||index>=layouts.Length?null:layouts[index];}
        public void Set(int map,TextAsset layout)
        {
            if(map<101||map>107||layout==null)throw new ArgumentException("Invalid training layout");
            int mapCount=maps?.Length??0,layoutCount=layouts?.Length??0;
            if(mapCount!=layoutCount||mapCount>0&&maps.Distinct().Count()!=mapCount)
                throw new InvalidOperationException("수련장 배치 카탈로그의 맵/참조 개수 또는 중복 번호를 확인하세요. 맵 "+mapCount+" / 참조 "+layoutCount);
            int index=maps==null?-1:Array.IndexOf(maps,map);
            if(index<0){maps=(maps??Array.Empty<int>()).Concat(new[]{map}).ToArray();layouts=(layouts??Array.Empty<TextAsset>()).Concat(new[]{layout}).ToArray();}
            else {if(layouts==null||index>=layouts.Length)throw new InvalidOperationException("Training catalog arrays do not match");layouts[index]=layout;}
        }
    }
}
