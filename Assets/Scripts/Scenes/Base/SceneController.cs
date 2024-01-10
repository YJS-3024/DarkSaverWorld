using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoSingleton<SceneController>
{
    private Dictionary<SceneType, string> dicScenes = new Dictionary<SceneType, string>();

    public SceneType CurSceneType { get; private set; }


    private Coroutine _continueChange;

    protected override void Destroy() { }

    public override bool Initialize()
    {
        dicScenes.Add(SceneType.Scene_Title, "TitleScene");
        dicScenes.Add(SceneType.Scene_Battle, "2D_Scene");

        return true;
    }

    public void ChangeScene(SceneType type)
    {
        if(_continueChange != null)
            return;

        _continueChange = StartCoroutine(CoLoading(type));
    }

    private IEnumerator CoLoading(SceneType type)
    {
        if(dicScenes.TryGetValue(type, out var sceneName) == false)
            yield break;

        var async = SceneManager.LoadSceneAsync(sceneName);
        var progress = 0f;

        while (true)
        {
            progress = async.progress;
            UIManager.I.LoadingUI.SetProgress(progress);

            if (async.isDone)
            {
                break;
            }

            yield return new WaitUntil(()=> progress != async.progress);
        }

        yield return new WaitForSeconds(1f);

        CompleteSceneLoad();
    }

    public void CompleteSceneLoad()
    {
        _continueChange = null;

        UIManager.I.LoadingUI.SetActive(false);
    }
}
