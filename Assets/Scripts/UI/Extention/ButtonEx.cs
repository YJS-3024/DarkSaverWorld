using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Extension
{
    [RequireComponent(typeof(Button))]
    public class ButtonEx : MonoBehaviour
    {
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

        public void AddListener(UnityAction action)
        {
            Button.onClick.AddListener(action);
        }
    }
}
