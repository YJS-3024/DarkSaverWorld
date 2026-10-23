using UnityEngine;

namespace DarkSaver.Maps
{
    public sealed class MapSceneDefinition : MonoBehaviour
    {
        [SerializeField] private string mapKey;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string summary;

        public string MapKey => mapKey;
        public string DisplayName => displayName;
        public string Summary => summary;
        public void Configure(string key, string title, string description)
        {
            mapKey = key;
            displayName = title;
            summary = description;
        }
    }
}
