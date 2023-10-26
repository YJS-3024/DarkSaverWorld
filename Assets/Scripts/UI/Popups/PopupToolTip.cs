using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PopupToolTip : MonoBehaviour
{
    private enum eToolTipType
    {
        Dir_Left_Bot,
        Dir_Center_Bot,
        Dir_Right_Bot,
    }

    [SerializeField] private GameObject goPopupToolTip;
    [SerializeField] private Text txtMessage;
    [SerializeField] private Button btnBg;

    [SerializeField] private Button btnTestActive;
    [SerializeField] private RectTransform rectCheckArea;

    private eToolTipType _toolTipType = eToolTipType.Dir_Center_Bot;

    private void Awake()
    {
        btnBg.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private string GetTestString()
    {
        StringBuilder sb = new StringBuilder();
        int rangeNum = Random.Range(0, 5);
        for (int i = 0; i < rangeNum; i++)
        {
            sb.AppendLine("1,2, 3, 4, 5, 6, 7, 8, 9, 10");
        }

        return sb.ToString();
    }

    public void SetActive(bool isActive)
    {
        goPopupToolTip.SetActive(isActive);
        if (isActive)
        {
            txtMessage.text = GetTestString();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)goPopupToolTip.transform);

            SetTestPosition();
        }
    }



    private void SetTestPosition()
    {
        var localPos = btnTestActive.transform.localPosition;
        var isCheck = RectTransformUtility.RectangleContainsScreenPoint(rectCheckArea, localPos);
        if (isCheck)
        {

        }
        // rectToolTip.
    }
}
