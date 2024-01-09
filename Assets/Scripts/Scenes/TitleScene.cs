using System.Collections;
using System.Collections.Generic;
using Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : SceneController
{
    [SerializeField] private Text txtTitleScene;
    [SerializeField] private Button btnNextScene;

    private float curTime = 0f;
    private bool isForward = true;

    private const float ReturnTime = 1f;

    public override SceneType GetSceneType() => SceneType.Scene_Title;

    protected override void Awake()
    {
        base.Awake();

        btnNextScene.onClick.AddListener(OnClick_NextScene);
    }

    // Update is called once per frame
    void Update()
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

    private void OnClick_NextScene()
    {
        SceneManager.LoadScene("2D_Scene");
    }
}
