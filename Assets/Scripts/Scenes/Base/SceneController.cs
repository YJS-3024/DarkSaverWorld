using UnityEngine;

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
    public abstract Scene.SceneType GetSceneType();

    protected virtual void Awake()
    {
        Init();
    }

    public void Init()
    {
        var gameSystem = FindObjectOfType<GameSystem>();
        if (gameSystem != null)
            return;

        GameSystem.I.Initialize();
    }
}
