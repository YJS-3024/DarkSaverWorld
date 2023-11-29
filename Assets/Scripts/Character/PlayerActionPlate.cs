using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionPlate : MonoBehaviour
{
    [SerializeField] private GameObject goPlate;

    private Vector2 _centerPos;

    public void SetActionPlate(Vector2 centerPos)
    {
        _centerPos = centerPos;
    }
}
