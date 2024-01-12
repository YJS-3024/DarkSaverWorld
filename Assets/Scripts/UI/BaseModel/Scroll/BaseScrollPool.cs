using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseScrollPool : MonoBehaviour
{
    public BaseScrollItem ItemPrefab => itemPrefab;
    [SerializeField] private BaseScrollItem itemPrefab;

    private Stack<BaseScrollItem> itemPool = new Stack<BaseScrollItem>();

    private void Awake()
    {
        itemPrefab.gameObject.SetActive(false);
    }

    public void PushItem(BaseScrollItem item)
    {
        if (item == null)
            return;

        item.transform.SetParent(gameObject.transform);
        item.gameObject.SetActive(false);

        itemPool.Push(item);
    }

    public BaseScrollItem PopItem(Transform parentTf)
    {
        var item = itemPool.Count > 0 ? itemPool.Pop() : null;
        if (item != null)
        {
            item.transform.SetParent(parentTf);
            return item;
        }
        else
        {
            return Instantiate<BaseScrollItem>(itemPrefab, parentTf);
        }
    }

    public bool IsSavedItem()
    {
        return itemPool.Count > 0;
    }
}
