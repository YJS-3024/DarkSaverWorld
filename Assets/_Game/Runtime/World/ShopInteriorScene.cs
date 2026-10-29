using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Maps;
using DarkSaver.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.World
{
    public static class ShopInteriorContext
    {
        public static ShopInteriorDefinition Current
        {
            get
            {
                ShopInteriorDefinition result=null;
                foreach(var row in ShopInteriorCatalog.ShopsWithFacilities)if(SceneManager.GetSceneByName(row.Region.RuntimeScene).isLoaded)
                {if(result!=null)return null;result=row;}
                return result;
            }
        }
    }
    // Native2000/2100 artwork/collision; runtime reads converted resources only.
    public sealed class ShopInteriorScene : MonoBehaviour
    {
        [SerializeField] private string shopKey;
        [SerializeField] private bool runtimeOnly;
        [SerializeField] private bool mapTilesBaked;
        private bool built;
        private void Awake()=>Build();
        public bool Build()
        {
            if(built)return true;
            if(!ShopInteriorCatalog.TryGet(shopKey,out _))return false;
            var native=gameObject.GetComponent<NativeTownScene>() ?? gameObject.AddComponent<NativeTownScene>();
            if(!native.Configure(shopKey,runtimeOnly,mapTilesBaked))return false;
            built=true;return true;
        }
    }
}
