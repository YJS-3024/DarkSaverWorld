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

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.aquamarine;
        Gizmos.DrawSphere(this.transform.position, 0.5f);
    }
}