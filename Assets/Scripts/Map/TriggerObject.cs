using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class TriggerObject : MonoBehaviour
{
    [SerializeField] private TriggerType triggerType;

    private BoxCollider2D collider2D;

    private void Awake()
    {
        collider2D = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"충돌 했다 {other.name}");

        switch (triggerType)
        {
            case TriggerType.Portal:
            {
                SceneController.I.ChangeScene(SceneType.Scene_Battle);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log($"충돌 했다 {other.collider.name}");
    }
}
