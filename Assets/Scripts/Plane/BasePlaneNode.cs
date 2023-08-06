using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


[System.Serializable]
public class PlanePathNode
{
    //노드의 위치값
    public Vector2 centerPos;   //노드의 센터위치
    // public Vector2 minPos;   //노드의 최소위치
    // public Vector2 maxPos;   //노드의 최대위치

    //노드의 인덱스
    public int indexX;
    public int indexY;

    //임직일수 잇는 노드 확인
    public bool isMoveAble = true;

    //astar 패스 계산시 정보
    public PlanePathNode pParent;   //너는 누구로 부터 왓니
    public int costToGoal;          //목적지까지의 비용
    public int costFromStart = 0;   //시작위치로부터의 간략화된 비용
    public int costTotal = 0;       //코스트 토탈

    //노드 생성자
    public PlanePathNode(int x, int y)
    {
        indexX = x;
        indexY = y;
        Reset();
    }

    //노드 초기화
    //경로 검색전 반드시 초기화 되어 있어야 한다.
    public void Reset()
    {
        pParent = null;
        costToGoal = 0;
        costFromStart = 0;
        costTotal = 0;
    }
}