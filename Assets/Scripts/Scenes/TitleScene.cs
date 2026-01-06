using GlobalEnum;
using UnityEngine;
using UnityEngine.UI;

public partial class TitleScene : BaseScene
{
    [SerializeField] private Text txtTitleScene;
    [SerializeField] private Button btnNextScene;

    private float curTime = 0f;
    private bool isForward = true;

    private const float ReturnTime = 1f;

    public void Start()
    {
        var gameSystem = FindAnyObjectByType<GameSystem>();
        if (gameSystem != null)
            return;

        GameSystem.I.Initialize();
    }

    // Update is called once per frame
    private void Update()
    {
        if (curTime >= ReturnTime)
        {
            isForward = !isForward;
            curTime = 0;
        }

        txtTitleScene.color = isForward
            ? new Color(0, 0, 0, Mathf.Lerp(0, 1, curTime / ReturnTime))
            : new Color(0, 0, 0, Mathf.Lerp(1, 0, curTime / ReturnTime));

        curTime += Time.deltaTime;
    }
    


    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Title;
}

public partial class TitleScene
{
    public override void ClickEvent(Vector2 screenPosition)
    {
        base.ClickEvent(screenPosition);
        
        SceneController.I.ChangeScene(GlobalEnum.SceneType.Scene_Village);
    }
}
