using System.Collections;
using System.Linq;
using GlobalEnum;
using UnityEngine;

public class ShopScene : ViliageScene
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {
        if (GameSystem.I is null)
        {
            yield return new WaitUntil(()=>GameSystem.Instance.Initialize());
        }

        yield return new WaitUntil(()=>CameraManager.I.Initialize());
        
        PlayerManager.I.CreatePlayer(true);
    }
    
    public override void ClickEvent(Vector2 screenPosition)
    {
        base.ClickEvent(screenPosition);
        
        var mousePos = Camera.main.ScreenPointToRay(screenPosition);
        var hit = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);

        if(hit.Length <= 0)
            return;

        SetClick_NPC(hit);
    }

    private void SetClick_NPC(RaycastHit2D[] hit)
    {
        var npcChar = hit
            .Where(x=>x.collider.gameObject.layer == (int)eLayer.NonPlayerChar)
            .Select(x=>x.collider.GetComponentInParent<NonPlayerChar>())
            .FirstOrDefault();

        if (npcChar != null)
        {
            UIManager.I.ShowPanel(UIType.TestPanel);
            Debug.Log($"엔피씨 클릭으로 인한 상점 열기 시도!");
        }
    }

    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Shop;
}
