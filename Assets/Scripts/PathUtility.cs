using System.Collections.Generic;
using UnityEngine;

public partial class PathUtility : MonoBehaviour
{
    private readonly List<PlanePathNode> openNodeList = new List<PlanePathNode>(); //오픈노드 리스트
    private readonly List<PlanePathNode> closeNodeList = new List<PlanePathNode>(); //클로즈 노드 리스트

    private readonly List<PlanePathNode> result = new List<PlanePathNode>(); //결과 벡터
    private readonly List<PlanePathNode> resultStack = new List<PlanePathNode>(); //결과 스택(백트레킹 때문에 쓴다.)

    private PlanePathNode startNode; //시작 노드
    private PlanePathNode endNode; //목적지 노드
    private bool bFindGoal; //목적지 찾은값

    private TileMap tileMap => SceneController.I.CurTileMap;

    /// <summary>
    /// 알고리즘으로 경로 찾기
    /// </summary>
    /// <param name="startPos">월드 시작 위치</param>
    /// <param name="endPos">월드 종료 위치</param>
    /// <param name="bDiagonal">대각 갈수있는</param>
    /// <returns></returns>
    public List<PlanePathNode> FindPath(Vector3 startPos, Vector3 endPos, bool bDiagonal)
    {
        //위치에 따른 시작노드와 종료 노드를 얻는다.
        startNode = tileMap.GetNode_WorldPos(startPos);
        endNode = tileMap.GetNode_WorldPos(endPos);

        //유효하지 않는 경로다.
        if (startNode == null || endNode == null)
            return null;

        //경로 계산을 할 필요가 없다.
        if (startNode == endNode)
            return null;

        //갈수 없는 목적지를 선택햇다면 
        if (endNode.isMoveAble == false ||
            tileMap.IsMove((int)endNode.centerPos.x, (int)endNode.centerPos.y) == false)
            return null;

        //경로 검색전 리셋 작업
        //모든 리스트 초기화
        result.Clear();
        resultStack.Clear();
        openNodeList.Clear();
        closeNodeList.Clear();
        bFindGoal = false;

        //시작 노드의 부모는 반드시 널
        startNode.pParent = null;

        //
        //시작 노드를 클로즈리스트에 넣고 그놈을 현재 검색노드로 설정한다.
        //
        closeNodeList.Add(startNode);
        PlanePathNode curNode = startNode; //현재 검색노드
        //
        //핵심 패스파인딩 loop검색노드를 찾을수 없다면 검색실패
        //보통 이루프가 빠져나가는 경우는 목적지까지 길이 다 막혀 있을때 이다.
        //

        while (curNode != null)
        {
            //루프에서 첫번째
            //현재노드 주변으로 갈수 있는 노드를 오픈노드에 추가한다.
            CheckAround(curNode, bDiagonal);

            //위의 첫번째 과정에서 엔드노드를 찾았다면
            if (bFindGoal)
            {
                //백트래킹
                PlanePathNode node = endNode;

                //스택에 목적지부터 result 거꾸로 쌓는다.
                while (node != null)
                {
                    //스택에 추가
                    result.Add(node);
                    node = node.pParent;
                }

                //반대로 뒤집는다.
                result.Reverse();

                return result;
            }

            //루프 두번째
            //오픈노드리스트중에 토탈코스트가 가장 작은놈을 오픈노드리스트에서 빼고 클로즈노드리스트에 넣은후
            //그놈을 검색 노드로 지정
            curNode = GetMinimumNodeFromOpenNode();
        }

        return null;
    }


    public List<PlanePathNode> FindPath_IncludeFindEnemy(Vector3 startPos, Vector3 endPos, bool bDiagonal)
    {
        //위치에 따른 시작노드와 종료 노드를 얻는다.
        startNode = tileMap.GetNode_WorldPos(startPos);
        endNode = tileMap.GetNode_WorldPos(endPos);

        //유효하지 않는 경로다.
        if (startNode == null || endNode == null)
            return null;

        //경로 계산을 할 필요가 없다.
        if (startNode == endNode)
            return null;

        //갈수 없는 목적지를 선택햇다면
        if (endNode.isMoveAble == false)
            return null;

        //적을 선택했다.
        if (EnemyManager.I.GetIsEnemy((int)endNode.centerPos.x, (int)endNode.centerPos.y))
            return null;

        //경로 검색전 리셋 작업
        //모든 리스트 초기화
        result.Clear();
        resultStack.Clear();
        openNodeList.Clear();
        closeNodeList.Clear();
        bFindGoal = false;

        //시작 노드의 부모는 반드시 널
        startNode.pParent = null;

        //
        //시작 노드를 클로즈리스트에 넣고 그놈을 현재 검색노드로 설정한다.
        //
        closeNodeList.Add(startNode);
        PlanePathNode curNode = startNode; //현재 검색노드
        //
        //핵심 패스파인딩 loop검색노드를 찾을수 없다면 검색실패
        //보통 이루프가 빠져나가는 경우는 목적지까지 길이 다 막혀 있을때 이다.
        //

        while (curNode != null)
        {
            //루프에서 첫번째
            //현재노드 주변으로 갈수 있는 노드를 오픈노드에 추가한다.
            CheckAround(curNode, bDiagonal, true);

            //위의 첫번째 과정에서 엔드노드를 찾았다면
            if (bFindGoal)
            {
                //백트래킹
                PlanePathNode node = endNode;

                //스택에 목적지부터 result 거꾸로 쌓는다.
                while (node != null)
                {
                    //스택에 추가
                    result.Add(node);
                    node = node.pParent;
                }

                //반대로 뒤집는다.
                result.Reverse();

                return result;
            }

            //루프 두번째
            //오픈노드리스트중에 토탈코스트가 가장 작은놈을 오픈노드리스트에서 빼고 클로즈노드리스트에 넣은후
            //그놈을 검색 노드로 지정
            curNode = GetMinimumNodeFromOpenNode();
        }

        return null;
    }


    /// <summary>
    /// 현재 노드 중심으로 갈수있는 노드를 오픈리스트에 추가
    /// </summary>
    /// <param name="curNode">현재노드</param>
    /// <param name="bDiagonal">대각 검색</param>
    private void CheckAround(PlanePathNode curNode, bool bDiagonal, bool bCheckChar = false)
    {
        if (IsMoveAble(curNode.indexX, curNode.indexY + 1, bCheckChar))
            AddOpenList(curNode.indexX, curNode.indexY + 1, curNode);

        if (IsMoveAble(curNode.indexX, curNode.indexY - 1, bCheckChar))
            AddOpenList(curNode.indexX, curNode.indexY - 1, curNode);

        if (IsMoveAble(curNode.indexX + 1, curNode.indexY, bCheckChar))
            AddOpenList(curNode.indexX + 1, curNode.indexY, curNode);

        if (IsMoveAble(curNode.indexX - 1, curNode.indexY, bCheckChar))
            AddOpenList(curNode.indexX - 1, curNode.indexY, curNode);

        if (bDiagonal)
        {
            if (IsMoveAble(curNode.indexX + 1, curNode.indexY + 1, bCheckChar))
                AddOpenList(curNode.indexX + 1, curNode.indexY + 1, curNode);

            if (IsMoveAble(curNode.indexX + 1, curNode.indexY - 1, bCheckChar))
                AddOpenList(curNode.indexX + 1, curNode.indexY - 1, curNode);

            if (IsMoveAble(curNode.indexX - 1, curNode.indexY + 1, bCheckChar))
                AddOpenList(curNode.indexX - 1, curNode.indexY + 1, curNode);

            if (IsMoveAble(curNode.indexX - 1, curNode.indexY - 1, bCheckChar))
                AddOpenList(curNode.indexX - 1, curNode.indexY - 1, curNode);
        }
    }


    //해당 인덱스의 위치가 갈수 있는 노드인지 확인
    public bool IsMoveAble(int indexX, int indexY, bool ignoreCheckChar = false)
    {
        //  0부터 만들어진 필드플랜의 갯수 유효한 노드인지를 판단합니다.
        if (0 <= indexX && indexX < tileMap.CellMaxWidth &&
            0 <= indexY && indexY < tileMap.CellMaxHeight)
        {
            var tileNode = tileMap.GetNode(indexX, indexY);
            if (ignoreCheckChar)
            {
                //  적 배치
                var centerPos = tileNode.centerPos;
                if (tileNode != endNode &&
                    IsDontStandAble(indexX, indexY))
                {
                    return false;
                }
            }

            if (tileMap.IsMove((int)tileNode.centerPos.x, (int)tileNode.centerPos.y) == false)
                return false;

            return tileNode.isMoveAble;
        }

        return false;
    }


    //해당 인덱스의 위치가 갈수 있는 노드인지 확인
    public bool IsDontStandAble(int indexX, int indexY)
    {
        //  0부터 만들어진 필드플랜의 갯수 유효한 노드인지를 판단합니다.
        if (0 <= indexX && indexX < tileMap.CellMaxWidth &&
            0 <= indexY && indexY < tileMap.CellMaxHeight)
        {
            var tileNode = tileMap.GetNode(indexX, indexY);
            var centerPos = tileNode.centerPos;

            if (tileMap.IsStandChar(centerPos))
                return true;

            if (tileMap.IsStandEnemy(centerPos))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 해당 인덱스의 노드 오픈리스트 추가
    /// </summary>
    /// <param name="indexX">인덱스X</param>
    /// <param name="indexY">인덱스Y</param>
    /// <param name="parent">누구로 부터왓니?</param>
    private void AddOpenList(int indexX, int indexY, PlanePathNode parent)
    {
        PlanePathNode node = tileMap.GetNode(indexX, indexY);

        if (closeNodeList.Contains(node))
            return;

        if (openNodeList.Contains(node))
        {
            int nowStartCost = parent.costFromStart + 1;

            int prevStartCost = node.costFromStart;

            if (nowStartCost < prevStartCost)
            {
                node.costFromStart = nowStartCost;
                node.pParent = parent;
                node.costTotal = node.costFromStart + node.costToGoal;
            }
        }
        else
        {
            node.pParent = parent;
            node.costFromStart = parent.costFromStart + 1;
            node.costToGoal = GetCostToGoal(node);
            node.costTotal = node.costFromStart + node.costToGoal;


            openNodeList.Add(node);


            if (node == endNode)
                bFindGoal = true;
        }
    }

    private int GetCostToGoal(PlanePathNode node)
    {
        return Mathf.Abs(node.indexX - endNode.indexX) + Mathf.Abs(node.indexY - endNode.indexY);
    }

    private PlanePathNode GetMinimumNodeFromOpenNode()
    {
        if (openNodeList.Count == 0)
            return null;

        PlanePathNode minimumNode = openNodeList[0];
        for (int i = 1; i < openNodeList.Count; i++)
        {
            if (openNodeList[i].costTotal < minimumNode.costTotal)
                minimumNode = openNodeList[i];
        }

        openNodeList.Remove(minimumNode);
        closeNodeList.Add(minimumNode);

        return minimumNode;
    }

    /// <summary>
    /// 센터 위치 기준의 범위에 위치한 플레이어 유닛 아래의 노드를 반환
    /// </summary>
    /// <param name="center"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public List<PlanePathNode> FindUnitNodeList(Vector2 center, int range)
    {
        var list = new List<PlanePathNode>();
        var unitList = PlayerManager.I.GetPlayerList();

        var centerPos = SceneController.I.CurScene.Map.GetNode_WorldPos(center).centerPos;
        foreach (var unit in unitList)
        {
            if (unit == null)
                continue;

            var targetPos = SceneController.I.CurScene.Map.GetNode_WorldPos(unit.transform.position).centerPos;
            var pathList = FindPath_IncludeFindEnemy(centerPos, targetPos, false);

            foreach (var node in pathList)
            {
                if (node.centerPos == centerPos)
                    continue;

                list.Add(node);
            }
        }

        return list;
    }

    /// <summary>
    /// 노드 위에 있는 플레이어 유닛 리턴
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public PlayerUnit CheckNodeInPlayerUnitList(PlanePathNode node)
    {
        var unitList = PlayerManager.I.GetPlayerList();
        foreach (var unit in unitList)
        {
            var centerPos = SceneController.I.CurScene.Map.GetNode_WorldPos(unit.transform.position).centerPos;
            if (node.centerPos == centerPos)
                return unit;
        }

        return null;
    }
}