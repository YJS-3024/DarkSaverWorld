using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
    private Camera mainCamera;

    [SerializeField] private Vector2 center;
    [SerializeField] private Vector2 mapMaxSize;
    [SerializeField] private Vector2 mapMinSize;

    [SerializeField] private float camMoveSpeed = 4;

    private float _width = 0;
    private float _height = 0;
    
    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        mainCamera = Camera.main;

        _height = mainCamera.orthographicSize;
        _width = _height * Screen.width / Screen.height;

        center = TilemapManager.I.Center;
        mapMaxSize = TilemapManager.I.MaxSize;
        mapMinSize = TilemapManager.I.MinSize;
        
        return true;
    }

    private void FixedUpdate()
    {
        SetLimitCameraArea();
    }

    private void SetLimitCameraArea()
    {
        var mainChar = PlayerManager.I.MainPlayer;
        mainCamera.transform.position = mainChar.CameraFollowPos.position;

        float lx = mapMaxSize.x - _width;
        float clampX = lx >= 0 
            ? Mathf.Clamp(mainCamera.transform.position.x, -lx + center.x, lx + center.x)
            : 0f;

        float ly = mapMaxSize.y - _height;
        float clampY = ly >= 0
            ? Mathf.Clamp(mainCamera.transform.position.y, -ly + center.y, ly + center.y)
            : 0f;
        
        mainCamera.transform.position = new Vector3(clampX, clampY, -10);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, mapMaxSize * 2);
    }
    
}
