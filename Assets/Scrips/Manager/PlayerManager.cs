using GlobalEnum;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    public MainPlayer MainPlayer { get; private set; }
    
    public override bool Initialize()
    {
        if (MainPlayer is null)
        {
            var prefab = ResourceManager.I.Road<GameObject>(eResourceType.Prefabs, "Character/Player");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
                MainPlayer = go.GetComponent<MainPlayer>();
            }
        }

        return true;
    }

    protected override void Destroy()
    {

    }
}
