using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Extension
{
    [RequireComponent(typeof(Button))]
    public class ButtonEx : MonoBehaviour
    {
        private UnityAction _onClick;

        private Text _btnText = null;

        public Text Text => _btnText != null
            ? _btnText
            : _btnText = GetComponentInChildren<Text>();

        public string ButtonString
        {
            get => Text.text;
            set => Text.text = value;
        }

        private Button _btn;

        public Button Button
        {
            get
            {
                if (_btn is null)
                {
                    _btn = GetComponent<Button>();
                    if (_btn is null)
                        _btn = gameObject.AddComponent<Button>();
                }

                return _btn;
            }
        }

        private void Awake()
        {
            Button.onClick.AddListener(_onClick);
        }

        public void AddListener(Action action)
        {
            _onClick = () => action?.Invoke();
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}
