using GlobalEnum;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar PlayerChar { get; private set; }
    
    public override bool Initialize()
    {
        if (PlayerChar is null)
        {
            var prefab = ResourceManager.I.Road<GameObject>(eResourceType.Prefabs, "Character/PlayerChar");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
                PlayerChar = go.GetComponent<PlayerChar>();
            }
        }

        return true;
    }

    protected override void Destroy()
    {

    }
}
