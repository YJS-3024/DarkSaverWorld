using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseScrollItem : MonoBehaviour
{
    [SerializeField] private Text itemText;


    public long ItemIndex { get; private set; } = 0;

    public Vector2 ItemSize => Rect.sizeDelta;

    public RectTransform Rect => (RectTransform)gameObject.transform;

    private void Awake()
    {
        Rect.sizeDelta = ItemSize;
    }

    public void UpdateItem(long index)
    {
        ItemIndex = index;

        itemText.text = index.ToString();
    }

    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
