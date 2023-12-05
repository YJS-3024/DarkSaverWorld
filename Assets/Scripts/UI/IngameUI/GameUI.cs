using GlobalEnum;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private RectTransform commanderRect;

    [SerializeField] private Button btnMove;
    [SerializeField] private Button btnAttack;
    [SerializeField] private Button btnRecess;
    [SerializeField] private Button btnOption;

    public void Awake()
    {
        commanderRect.gameObject.SetActive(false);

        btnMove.onClick.AddListener(OnClick_Move);
    }

    public void SetCommander(Vector3 worldPos)
    {
        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screenPoint, Camera.main, out var localPoint);
        commanderRect.anchoredPosition = new Vector2(localPoint.x, localPoint.y);

        var referenceResolution = UIManager.I.CanvasScaler.referenceResolution;
        var radioFactor = UIManager.I.CanvasScale.y / referenceResolution.y;
        commanderRect.localScale = new Vector2(radioFactor,radioFactor);

        commanderRect.gameObject.SetActive(!commanderRect.gameObject.activeSelf);
    }

    private void OnClick_Move()
    {
        PlayerManager.I.PlayerChar.CharAction = eCharAction.Move;
        var pos = PlayerManager.I.PlayerChar.transform.position;
        PlayerManager.I.CreateMovePlates(pos);

        commanderRect.gameObject.SetActive(false);
    }
}
