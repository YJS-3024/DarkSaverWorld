using System;
using GlobalEnum;
using UnityEngine;

public class TriggerObject : MonoBehaviour
{
    [SerializeField] private TriggerType triggerType;
    [SerializeField] private long triggerId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"충돌 했다 {other.name}");

        switch (triggerType)
        {
            case TriggerType.Village:
            {
                SceneController.I.ChangeScene(SceneType.Scene_Village);
                break;
            }
            case TriggerType.Shop:
            {
                SceneController.I.ChangeScene(SceneType.Scene_Shop);
                break;
            }
            case TriggerType.Field:
            {
                break;
            }
            case TriggerType.Dungeon:
            {
                SceneController.I.ChangeScene(SceneType.Scene_Battle);
                break;
            }
            case TriggerType.None:
            default:
            {
                break;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log($"충돌 했다 {other.collider.name}");
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.aquamarine;
        Gizmos.DrawSphere(this.transform.position, 0.5f);
    }
}