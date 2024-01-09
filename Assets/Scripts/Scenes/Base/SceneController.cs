using System.Collections.Generic;
using Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene
{
    public enum SceneType
    {
        Scene_Title,
        Scene_Village,
        Scene_Battle,
    }
}

public abstract class SceneController : MonoBehaviour
{
    private Dictionary<SceneType, string> dicScenes = new Dictionary<SceneType, string>();
    
    public abstract Scene.SceneType GetSceneType();

    protected virtual void Awake()
    {
        dicScenes.Add(SceneType.Scene_Title, "TitleScene");
        dicScenes.Add(SceneType.Scene_Battle, "2D_Scene");

        Init();
    }

    public void Init()
    {
        var gameSystem = FindObjectOfType<GameSystem>();
        if (gameSystem != null)
            return;

        GameSystem.I.Initialize();
    }

    public void ChangeScene(SceneType type)
    {
        if (dicScenes.TryGetValue(type, out var sceneName))
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
