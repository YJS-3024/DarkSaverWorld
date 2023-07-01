
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SpriteFinderEditor : EditorWindow
{

    public class SpriteInfo
    {
        public int num;
        public GameObject gameObject;
        public Image image;
        public Sprite sprite;
        public Texture atlas;

        public SpriteInfo(int number, Image image)
        {
            this.num = number;
            this.gameObject = image.gameObject;
            this.sprite = image.sprite;
            this.image = image;
            this.atlas = this.image.mainTexture;
        }
    }

    private List<SpriteInfo> m_spriteList = new List<SpriteInfo>();
    private Vector2 m_scrollPos = Vector2.zero;

    [MenuItem("FGUI/Tools/SpriteFinder &v")]
    public static void OpenWindow()
    {
        SpriteFinderEditor window = GetWindow<SpriteFinderEditor>(true, "SpriteFinder");
        window.minSize = window.maxSize = new Vector2(400f, 600f);
        window.Show();
    }


    private void OnGUI()
    {
        Display();
    }

    private void Display()
    {
        EditorGUILayout.BeginVertical();
        {
            ReSearchSprite();

            //  검색된 UISprite들 표시
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            if (m_spriteList.Count != 0)
            {
                //  보여질 갯수
                int viewCount = 40;
                //  한 칼럼의 높이
                int culumnHeight = 20;
                //  첫 칼럼의 인덱스
                int firstIndex = (int)m_scrollPos.y / culumnHeight;
                firstIndex = Mathf.Clamp(firstIndex, 0, Mathf.Max(0, (m_spriteList.Count -1)));

                //  위쪽 공백
                GUILayout.Space(firstIndex * culumnHeight);

                //  보여지는 칼럼들
                int count = Mathf.Min(m_spriteList.Count, firstIndex + viewCount);
                for (int i = firstIndex; i < count; i++)
                {
                    HorizontalColumn(m_spriteList[i]);
                }

                //  아래쪽 공백
                GUILayout.Space(Mathf.Max(0, (m_spriteList.Count) - firstIndex - viewCount) * culumnHeight);
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    ///   검색 및 스프라이트 저장
    /// </summary>
    private void ReSearchSprite()
    {
        if(GUILayout.Button("SearchSprite"))
        {
            //  선택된 게임 오브젝트가 없는 경우
            if(Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("SelectedUIs Zero so Complate Find Sprites");
                return;
            }

            m_spriteList.Clear();

            //  선택된 게임 오브젝트들
            var child = Selection.gameObjects;
            if (child == null || child.Length == 0)
                return;

            //  스프라이트 추출
            List<Image> list = new List<Image>();
            int count = child.Length;
            for (int i = 0; i < count; i++)
            {
                list.AddRange(child[i].GetComponentsInChildren<Image>(true));
            }

            //  스프라이트 표시
            count = list.Count;
            for (int i = 0; i < count; i++)
            {
                SpriteInfo info = new SpriteInfo(i + 1, list[i]);
                m_spriteList.Add(info);
            }


            ////  선택된 갯수와 저장한 갯수와 다를 경우
            //if (Selection.gameObjects.Length != m_selectedUI)
            //{
            //    //  선택된 갯수와 저장한 갯수를 동일하게 해줍니다.
            //    m_selectedUI = Selection.gameObjects.Length;
            //}
        }
    }

    /// <summary>
    ///  가로 칼럼
    /// </summary>
    private void HorizontalColumn(SpriteInfo info)
    {
        EditorGUILayout.BeginHorizontal();
        {
            //  넘버
            GUIStyle style = new GUIStyle();
            style.wordWrap = true;
            style.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.IntField(info.num, style, new[] { GUILayout.Width(30), GUILayout.ExpandHeight(true) });

             //  스프라이트 없으면 게임 오브젝트
             EditorGUILayout.ObjectField(info.gameObject, typeof(GameObject), false);

            //  스프라이트 없으면 게임 오브젝트
            if (info.sprite != null)
            {
                EditorGUILayout.ObjectField(info.sprite, typeof(Image), false);
            }
            else
            {
                EditorGUILayout.ObjectField(info.gameObject, typeof(Image), false);
            }

            //  아틀라스 없으면 널
            if (info.atlas == null)
            {
                EditorGUILayout.ObjectField(null, typeof(Texture), false);
            }
            else
            {
                EditorGUILayout.ObjectField(info.atlas, typeof(Texture), false);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
