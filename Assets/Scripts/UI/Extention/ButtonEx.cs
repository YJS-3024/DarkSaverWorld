using UnityEngine.UI;

namespace UI.Extension
{
    public class ButtonEx : Button
    {
        private Text _btnText = null;

        public Text ButtonText => _btnText != null
            ? _btnText
            : _btnText = GetComponentInChildren<Text>();

        public string ButtonString
        {
            get => ButtonText.text;
            set => ButtonText.text = value;
        }
    }
}
