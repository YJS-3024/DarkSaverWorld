using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T I => Instance;
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                    _instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }

            return _instance;
        }
    }

    public void SetParent(Transform tf)
    {
        if(tf == null)
            return;

        transform.SetParent(tf);
    }

    protected abstract void Destroy();
    public abstract bool Initialize();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        Destroy();
    }
}
