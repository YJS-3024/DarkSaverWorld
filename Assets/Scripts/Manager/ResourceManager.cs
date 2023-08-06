using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GlobalEnum;
using UnityEngine;

public class ResourceManager : MonoSingleton<ResourceManager>
{
    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        return true;
    }

    public T Load<T>(GlobalEnum.eResourceType type, string path) where T : class
    {
        var resourcePath = string.Empty;
        switch (type)
        {
            case eResourceType.Prefabs:
                resourcePath = $"Prefabs/{path}";
                break;
            case eResourceType.Sprite:
                resourcePath = $"Sprite/{path}";
                break;
            case eResourceType.Texture:
                resourcePath = $"Texture/{path}";
                break;
            case eResourceType.TextAsset:
                resourcePath = $"Tables/{path}";
                break;
            case eResourceType.None:
            default:
                return null;
        }

        if (Resources.Load(resourcePath) is T temp)
        {
            return temp;
        }

        return null;
    }

    public Sprite[] RoadSpritesAll(string pathName)
    {
        var path =  $"Sprite/{pathName}";
        return Resources.LoadAll<Sprite>(path);   
    }
}
