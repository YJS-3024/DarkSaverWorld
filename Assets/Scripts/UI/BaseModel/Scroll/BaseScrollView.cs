using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다음 할일들
///
/// TODO :: Bottom To Top 할경우 업데이트 아이템 생성이 꼬였음
/// 
/// </summary>

[RequireComponent(typeof(ScrollRect))]
public class BaseScrollView : MonoBehaviour
{
    public enum eScrollType
    {
        Left_To_Right,
        Right_To_Left,

        Top_To_Bottom,
        Bottom_To_Top,
    }

    [SerializeField] private BaseScrollPool scrollPool;

    public eScrollType ScrollType = eScrollType.Top_To_Bottom;

    public int CreateCount = 5;


    public Vector3 ScrollPos = Vector3.zero;


    private int ViewItemCount = 0;
    private int LineRow = 1;
    private int LineColumn = 1;

    private List<BaseScrollItem> itemList = new List<BaseScrollItem>();
    private BaseScrollItem scrollItem => scrollPool.ItemPrefab;
    private ScrollRect scrollRect;
    private int savePageNo = 0;
    private int maxPageNo = 0;

    private void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null)
                return;
        }

        InitScroll();

        SetScrollComponent();
    }

    private void OnEnable()
    {
        scrollRect.onValueChanged.AddListener(OnValueChanged_Scroll);
    }

    private void OnDisable()
    {
        scrollRect.onValueChanged.RemoveListener(OnValueChanged_Scroll);
    }

    private void InitScroll()
    {
        var scrollWidth = scrollRect.viewport.rect.size.x;
        var scrollHeight = scrollRect.viewport.rect.size.y;

        switch (ScrollType)
        {
            case eScrollType.Top_To_Bottom:
                {
                    ViewItemCount = Mathf.RoundToInt(scrollHeight / scrollItem.ItemSize.y) + 1;
                    LineRow = Mathf.FloorToInt(scrollWidth / scrollPool.ItemPrefab.ItemSize.x);

                    var contentWidth = LineRow * scrollPool.ItemPrefab.ItemSize.x;
                    var contentHeight = LineRow <= 1
                        ? scrollItem.ItemSize.y * CreateCount
                        : scrollItem.ItemSize.y * ((CreateCount / LineRow) + ((CreateCount) % LineRow == 0 ? 0 : 1));

                    maxPageNo = Mathf.RoundToInt(contentHeight / scrollPool.ItemPrefab.ItemSize.y) - ViewItemCount;
                    //Debug.Log($"Line Count : {CreateCount / RowLine} /n Remain Count : {((CreateCount) % RowLine == 0 ? 0 : 1)}");

                    //  크기
                    var contentRect = scrollRect.content;
                    contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
                    scrollRect.content = contentRect;

                    //  위치
                    var moveCenterX = (scrollWidth - contentWidth) * 0.5f;
                    var pos = scrollRect.content.localPosition;
                    pos.x += moveCenterX;
                    scrollRect.content.localPosition = pos;

                    scrollRect.verticalNormalizedPosition = ScrollType == eScrollType.Top_To_Bottom ? 1 : 0;

                    //  아이템 생성
                    int index = 0;
                    for (int i = 0; i <= ViewItemCount; i++)
                    {
                        for (int row = 0; row < LineRow; row++)
                        {
                            if (index >= CreateCount)
                            {
                                break;
                            }

                            var item = scrollPool.PopItem(scrollRect.content);
                            RepositionItem(item, index++, ScrollType);
                            item.SetActive(true);

                            itemList.Add(item);
                        }
                    }

                    //  초기 셋팅시 스크롤 페이지 번호 저장
                    savePageNo = 0;

                    break;
                }

            case eScrollType.Bottom_To_Top:
                {
                    ViewItemCount = Mathf.RoundToInt(scrollHeight / scrollItem.ItemSize.y) + 1;
                    LineRow = Mathf.FloorToInt(scrollWidth / scrollPool.ItemPrefab.ItemSize.x);

                    var contentWidth = LineRow * scrollPool.ItemPrefab.ItemSize.x;
                    var contentHeight = LineRow <= 1
                        ? scrollItem.ItemSize.y * CreateCount
                        : scrollItem.ItemSize.y * ((CreateCount / LineRow) + ((CreateCount) % LineRow == 0 ? 0 : 1));

                    maxPageNo = Mathf.RoundToInt(contentHeight / scrollPool.ItemPrefab.ItemSize.y) - ViewItemCount;
                    //Debug.Log($"Line Count : {CreateCount / RowLine} /n Remain Count : {((CreateCount) % RowLine == 0 ? 0 : 1)}");

                    //  크기
                    var contentRect = scrollRect.content;
                    contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
                    scrollRect.content = contentRect;

                    //  위치
                    var moveCenterX = (scrollWidth - contentWidth) * 0.5f;
                    var pos = scrollRect.content.localPosition;
                    pos.x += moveCenterX;
                    scrollRect.content.localPosition = pos;

                    scrollRect.verticalNormalizedPosition = ScrollType == eScrollType.Top_To_Bottom ? 1 : 0;

                    //  아이템 생성
                    int index = 0;
                    for (int i = 0; i <= ViewItemCount; i++)
                    {
                        for (int row = 0; row < LineRow; row++)
                        {
                            if (index >= CreateCount)
                            {
                                break;
                            }

                            var item = scrollPool.PopItem(scrollRect.content);
                            RepositionItem(item, index++, ScrollType);
                            item.SetActive(true);

                            itemList.Add(item);
                        }
                    }

                    //  초기 셋팅시 스크롤 페이지 번호 저장
                    savePageNo = maxPageNo;

                    break;
                }

            case eScrollType.Left_To_Right:
                {
                    ViewItemCount = Mathf.RoundToInt(scrollWidth / scrollItem.ItemSize.x) + 1;
                    LineColumn = Mathf.FloorToInt(scrollHeight / scrollPool.ItemPrefab.ItemSize.y);

                    var contentWidth = LineColumn <= 1
                        ? scrollItem.ItemSize.x * CreateCount
                        : scrollItem.ItemSize.x * ((CreateCount / LineColumn) + ((CreateCount) % LineColumn == 0 ? 0 : 1));
                    var contentHeight = LineColumn * scrollPool.ItemPrefab.ItemSize.y;

                    maxPageNo = Mathf.RoundToInt(contentWidth / scrollPool.ItemPrefab.ItemSize.x) - ViewItemCount;

                    //  크기
                    var contentRect = scrollRect.content;
                    contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
                    scrollRect.content = contentRect;

                    //  위치
                    var moveCenterY = (scrollHeight - contentHeight) * 0.5f;
                    var pos = scrollRect.content.localPosition;
                    pos.y -= moveCenterY;
                    scrollRect.content.localPosition = pos;

                    scrollRect.horizontalNormalizedPosition = ScrollType == eScrollType.Right_To_Left ? 1 : 0;

                    //  아이템 생성
                    int index = 0;
                    for (int i = 0; i <= ViewItemCount; i++)
                    {
                        for (int column = 0; column < LineColumn; column++)
                        {
                            if (index >= CreateCount)
                            {
                                break;
                            }

                            var item = scrollPool.PopItem(scrollRect.content);
                            RepositionItem(item, index++, ScrollType);
                            item.SetActive(true);

                            itemList.Add(item);
                        }
                    }

                    //  초기 셋팅시 스크롤 페이지 번호 저장
                    savePageNo = ScrollType == eScrollType.Left_To_Right
                        ? 0
                        : maxPageNo;

                    break;
                }

            case eScrollType.Right_To_Left:
                {
                    break;
                }
        }


        //  초기 셋팅시 스크롤 Content 위치값 저장
        ScrollPos = scrollRect.content.localPosition;
    }

    /// <summary>
    /// 스크롤 설정 값에 맞게 스크롤 컴포넌트 셋팅 변경
    /// </summary>
    private void SetScrollComponent()
    {
        scrollRect.vertical = false;
        scrollRect.horizontal = false;

        if (ScrollType == eScrollType.Top_To_Bottom ||
            ScrollType == eScrollType.Bottom_To_Top)
        {
            scrollRect.vertical = true;
        }

        if (ScrollType == eScrollType.Left_To_Right ||
            ScrollType == eScrollType.Right_To_Left)
        {
            scrollRect.horizontal = true;
        }
    }

    /// <summary>
    /// 각 아이템 인덱스 별 위치조정
    /// </summary>
    /// <param name="item">아이템 오브젝트 </param>
    /// <param name="index">아이템 인덱스 </param>
    /// <param name="scrollType">현재 아이템 스크롤 방향 </param>
    /// <returns></returns>
    private BaseScrollItem RepositionItem(BaseScrollItem item, int index, eScrollType scrollType)
    {
        item.UpdateItem(index);

        if (scrollType == eScrollType.Top_To_Bottom)
        {
            var posX = index % LineRow * scrollItem.ItemSize.x;
            var posY = -(index / LineRow * scrollItem.ItemSize.y);
            item.Rect.anchoredPosition = new Vector2(posX, posY);
        }
        else if (scrollType == eScrollType.Bottom_To_Top)
        {
            var posX = (index % LineRow) * scrollItem.ItemSize.x;
            var posY = -scrollRect.content.sizeDelta.y + scrollItem.ItemSize.y + Mathf.Abs(index / LineRow * scrollItem.ItemSize.y);
            item.Rect.anchoredPosition = new Vector2(posX, posY);
        }
        
        if (scrollType == eScrollType.Right_To_Left)
        {
            var posX = scrollRect.content.sizeDelta.x - scrollItem.ItemSize.x - Mathf.Abs(index / LineColumn * scrollItem.ItemSize.x);;
            var posY = -(index % LineColumn * scrollItem.ItemSize.y);
            item.Rect.anchoredPosition = new Vector2(posX, posY);
        }
        else
        {
            var posX = index / LineColumn * scrollItem.ItemSize.x;
            var posY = -(index % LineColumn * scrollItem.ItemSize.y);
            item.Rect.anchoredPosition = new Vector2(posX, posY);
        }

        return item;
    }

    /// <summary>
    /// 스크롤 업데이트
    /// </summary>
    /// <param name="pos">스크롤 Content 위치 변</param>
    private void OnValueChanged_Scroll(Vector2 pos)
    {
        var contentPos = scrollRect.content.localPosition;

        if (ScrollType == eScrollType.Top_To_Bottom)
        {
            var pageNo = Mathf.RoundToInt((ScrollPos.y / scrollItem.ItemSize.y));
            var count = Mathf.Abs(pageNo - savePageNo);

            Debug.Log($"{pageNo} / {savePageNo}");

            if (pageNo > savePageNo && pageNo < maxPageNo)
            {
                var lastIndex = (int)itemList.LastOrDefault()?.ItemIndex;
                if (lastIndex < CreateCount)
                {
                    for (int i = 0; i < count; i++)
                    {
                        //  이전
                        for (int line = 0; line < LineRow; line++)
                        {
                            var prevItem = itemList.FirstOrDefault();
                            var CheckSize = (Vector2)prevItem.transform.position;
                            CheckSize.y -= prevItem.ItemSize.y;
                            if (RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, CheckSize) == false)
                            {
                                scrollPool.PushItem(prevItem);
                                itemList.RemoveAt(0);
                            }
                        }

                        //  다음
                        for (int line = 0; line < LineRow; line++)
                        {
                            var nextIndex = lastIndex + 1;
                            if (nextIndex >= CreateCount)
                            {
                                break;
                            }

                            var nextItem = scrollPool.PopItem(scrollRect.content.transform);
                            RepositionItem(nextItem, ++lastIndex, ScrollType);
                            itemList.Add(nextItem);

                            nextItem.gameObject.SetActive(true);
                        }
                    }

                    savePageNo = pageNo > maxPageNo
                        ? maxPageNo
                        : pageNo;
                }
            }

            if (pageNo < savePageNo && pageNo >= 0)
            {
                var firstIndex = (int)itemList.FirstOrDefault()?.ItemIndex;
                if (firstIndex > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        //  이전
                        for (int line = 0; line < LineRow; line++)
                        {
                            var prevItem = itemList.LastOrDefault();
                            var CheckSize = (Vector2)prevItem.transform.position;
                            CheckSize.y += prevItem.ItemSize.y;
                            if (RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, CheckSize) == false)
                            {
                                scrollPool.PushItem(prevItem);
                                itemList.RemoveAt(itemList.Count - 1);
                            }
                        }

                        //  다음
                        for (int line = 0; line < LineRow; line++)
                        {
                            if (firstIndex < 0)
                            {
                                break;
                            }

                            var nextItem = scrollPool.PopItem(scrollRect.content.transform);
                            RepositionItem(nextItem, --firstIndex, ScrollType);
                            itemList.Insert(0, nextItem);

                            nextItem.gameObject.SetActive(true);
                        }
                    }

                    savePageNo = pageNo < 0
                        ? 0
                        : pageNo;
                }
            }
        }

        //  TODO :: 밑에서 위로는 문제 있음
        // if (ScrollType == eScrollType.Bottom_To_Top)
        // {
        //     var pageNo = Mathf.RoundToInt((ScrollPos.y / scrollItem.ItemSize.y));
        //     var count = Mathf.Abs(pageNo - savePageNo);
        //
        //     Debug.Log($"{pageNo} / {savePageNo}");
        //
        //     if (pageNo > savePageNo && pageNo > 0)
        //     {
        //         var firstIndex = (int)itemList.FirstOrDefault()?.ItemIndex;
        //         if (firstIndex >= 0)
        //         {
        //             for (int i = 0; i < count; i++)
        //             {
        //                 //  이전
        //                 for (int line = 0; line < LineRow; line++)
        //                 {
        //                     var prevItem = itemList.FirstOrDefault();
        //                     // var CheckSize = Camera.main.WorldToScreenPoint(prevItem.transform.position);
        //                     var CheckSize = prevItem.transform.position;
        //                     CheckSize.y += prevItem.ItemSize.y;
        //                     if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)scrollRect.transform, CheckSize) == false)
        //                     {
        //                         scrollPool.PushItem(prevItem);
        //                         itemList.RemoveAt(0);
        //                     }
        //                 }
        //
        //                 //  다음
        //                 for (int line = 0; line < LineRow; line++)
        //                 {
        //                     if (firstIndex + ViewItemCount >= CreateCount)
        //                     {
        //                         break;
        //                     }
        //
        //                     var nextItem = scrollPool.PopItem(scrollRect.content.transform);
        //                     RepositionItem(nextItem, (++firstIndex) + ViewItemCount, ScrollType);
        //                     itemList.Add(nextItem);
        //
        //                     nextItem.gameObject.SetActive(true);
        //                 }
        //             }
        //
        //             savePageNo = pageNo > maxPageNo
        //                 ? maxPageNo
        //                 : pageNo;
        //         }
        //     }
        //
        //
        //     if (pageNo > savePageNo && pageNo <= maxPageNo)
        //     {
        //         var lastIndex = (int)itemList.LastOrDefault()?.ItemIndex;
        //         if (lastIndex >= 0)
        //         {
        //             for (int i = 0; i < count; i++)
        //             {
        //                 //  이전
        //                 for (int line = 0; line < LineRow; line++)
        //                 {
        //                     var prevItem = itemList.LastOrDefault();
        //                     var CheckSize = Camera.main.WorldToScreenPoint(prevItem.transform.position);
        //                     CheckSize.y -= prevItem.ItemSize.y;
        //                     if (RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, CheckSize) == false)
        //                     {
        //                         scrollPool.PushItem(prevItem);
        //                         itemList.RemoveAt(itemList.Count - 1);
        //                     }
        //                 }
        //
        //                 //  다음
        //                 for (int line = 0; line < LineRow; line++)
        //                 {
        //                     var nextIndex = lastIndex - ViewItemCount;
        //                     if (nextIndex < 0)
        //                     {
        //                         break;
        //                     }
        //                     
        //                     var nextItem = scrollPool.PopItem(scrollRect.content.transform);
        //                     RepositionItem(nextItem, --lastIndex - ViewItemCount, ScrollType);
        //                     itemList.Insert(0, nextItem);
        //
        //                     nextItem.gameObject.SetActive(true);
        //                 }
        //             }
        //
        //             savePageNo = pageNo < 0
        //                 ? 0
        //                 : pageNo;
        //         }
        //     }
        // }

        if (ScrollType == eScrollType.Left_To_Right ||
            ScrollType == eScrollType.Right_To_Left)
        {
            var pageNo = (int)Mathf.Abs((ScrollPos.x / scrollItem.ItemSize.x));
            var count = Mathf.Abs(pageNo - savePageNo);

            //Debug.Log($"{pageNo} / {savePageNo}");
            //Debug.Log($"{ScrollPos.x}");

            if (pageNo > savePageNo && pageNo < maxPageNo)
            {
                if (contentPos.x >= 0)
                {
                    return;
                }

                var lastIndex = (int)itemList.LastOrDefault()?.ItemIndex;
                if (lastIndex < CreateCount)
                {
                    for (int i = 0; i < count; i++)
                    {
                        //  이전
                        for (int line = 0; line < LineColumn; line++)
                        {
                            var prevItem = itemList.FirstOrDefault();
                            var CheckSize = (Vector2)prevItem.transform.position;
                            CheckSize.x += prevItem.ItemSize.x;
                            if (RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, CheckSize) == false)
                            {
                                scrollPool.PushItem(prevItem);
                                itemList.RemoveAt(0);
                            }
                        }

                        //  다음
                        for (int line = 0; line < LineColumn; line++)
                        {
                            var nextIndex = lastIndex + 1;
                            if (nextIndex >= CreateCount)
                            {
                                break;
                            }

                            var nextItem = scrollPool.PopItem(scrollRect.content.transform);
                            RepositionItem(nextItem, ++lastIndex, ScrollType);
                            itemList.Add(nextItem);

                            nextItem.gameObject.SetActive(true);
                        }
                    }

                    savePageNo = pageNo > maxPageNo
                        ? maxPageNo
                        : pageNo;
                }
            }

            if (pageNo < savePageNo && pageNo >= 0)
            {
                var firstIndex = (int)itemList.FirstOrDefault()?.ItemIndex;
                if (firstIndex > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        //  이전
                        for (int line = 0; line < LineColumn; line++)
                        {
                            var prevItem = itemList.LastOrDefault();
                            var CheckSize = (Vector2)prevItem.transform.position;
                            CheckSize.x += prevItem.ItemSize.x;
                            if (RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, CheckSize) == false)
                            {
                                scrollPool.PushItem(prevItem);
                                itemList.RemoveAt(itemList.Count - 1);
                            }
                        }

                        //  다음
                        for (int line = 0; line < LineColumn; line++)
                        {
                            if (firstIndex < 0)
                            {
                                break;
                            }

                            var nextItem = scrollPool.PopItem(scrollRect.content.transform);
                            RepositionItem(nextItem, --firstIndex, ScrollType);
                            itemList.Insert(0, nextItem);

                            nextItem.gameObject.SetActive(true);
                        }
                    }

                    savePageNo = pageNo < 0
                        ? 0
                        : pageNo;
                }
            }
        }

        ScrollPos = contentPos;
    }
}
