using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerObject : MonoBehaviour
{
    [SerializeField] private BoxCollider2D collider2D;

    private void Awake()
    {
        collider2D = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"충돌 했다 {other.name}");
    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log($"충돌 했다 {other.collider.name}");
    }
}
