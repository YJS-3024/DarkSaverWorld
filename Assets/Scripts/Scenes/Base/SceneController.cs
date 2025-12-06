using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BaseScene : MonoBehaviour
{
    [SerializeField] protected TileMap _map;

    public TileMap Map
    {
        get
        {
            if (_map == null)
                _map = FindAnyObjectByType<TileMap>();
            
            return _map;
        }
    }

    private PathUtility _pathUtility;

    public PathUtility Path
    {
        get
        {
            if (_pathUtility is null)
            {
                _pathUtility = GetComponent<PathUtility>();
                if (_pathUtility is null)
                {
                    _pathUtility = gameObject.AddComponent<PathUtility>();
                }
            }

            return _pathUtility;
        }
    }

    public abstract SceneType SceneType();

    public virtual void SetTileMap()
    {
        
    }

    public virtual void ClickEvent(Vector2 screenPosition)
    {
        
    }
}

public class SceneController : MonoSingleton<SceneController>
{
    private Dictionary<SceneType, string> dicScenes = new Dictionary<SceneType, string>();

    public BaseScene CurScene { get; private set; }
    public SceneType CurSceneType => CurScene.SceneType();
    public TileMap CurTileMap => CurScene?.Map ?? null;


    private Coroutine _continueChange;
    public bool IsLoadComplete { get; private set; } = false;

    protected override void Destroy() { }

    public override bool Initialize()
    {
        dicScenes.Add(SceneType.Scene_Title, "TitleScene");
        dicScenes.Add(SceneType.Scene_Village, "Scene_Viliiage");
        dicScenes.Add(SceneType.Scene_Battle, "2D_Scene");

        CompleteSceneLoad(SceneType.Scene_Title);
        
        return true;
    }

    /// <summary>
    /// 변경 씬 로드
    /// </summary>
    /// <param name="type"></param>
    public void ChangeScene(SceneType type)
    {
        IsLoadComplete = false;
        if(_continueChange != null)
            return;

        _continueChange = StartCoroutine(CoLoading(type));
    }
    
    /// <summary>
    /// 추가씬 로드
    /// </summary>
    /// <param name="sceneName"></param>
    public void AdditiveScene(string sceneName)
    {
        IsLoadComplete = false;
        if(_continueChange != null)
            return;

        _continueChange = StartCoroutine(CoLoading(sceneName));
    }

    /// <summary>
    /// 변경 씬 로드 코루틴
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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
                break;

            yield return new WaitUntil(()=> progress != async.progress);
        }

        yield return new WaitForSeconds(1f);

        CompleteSceneLoad(type);
    }
    
    /// <summary>
    /// 추가씬 로드 코루틴
    /// </summary>
    /// <param name="additiveSceneName"></param>
    /// <returns></returns>
    public IEnumerator CoLoading(string additiveSceneName)
    {
        var async = SceneManager.LoadSceneAsync(additiveSceneName, LoadSceneMode.Additive);
        var progress = 0f;

        while (true)
        {
            progress = async.progress;
            if (async.isDone)
                break;

            yield return new WaitUntil(()=> progress != async.progress);
        }

        yield return new WaitForSeconds(1f);

        IsLoadComplete = true;
    }
    
    /// <summary>
    /// 씬 로드 완료후 현재 씬과 씬타입을 셋팅
    /// </summary>
    /// <param name="type"></param>
    public void CompleteSceneLoad(SceneType type)
    {
        _continueChange = null;

        CurScene = FindAnyObjectByType<BaseScene>();
        CameraManager.I.Initialize();
        
        IsLoadComplete = true;

        UIManager.I.LoadingUI.SetActive(false);
    }
}
