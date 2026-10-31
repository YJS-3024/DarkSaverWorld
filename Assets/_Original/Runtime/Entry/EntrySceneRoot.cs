using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkSaver.Original.Entry
{
    [DisallowMultipleComponent, RequireComponent(typeof(LocalServerManager))]
    public sealed class EntrySceneRoot : MonoBehaviour
    {
        [SerializeField] private NativeEntryArt art = null;
        [Tooltip("Enable the preserved login and server-selection flow. Disabled uses a separate local profile.")]
        [SerializeField] private bool useLoginAndServerSelection = false;
        private EntryClient client;
        private GameObject ownedCanvas, ownedEvents;
        private RectTransform screen, login, servers, creation, modal;
        private InputField idInput, passwordInput, nameInput;
        private GameObject drawer, selectedServer;
        private Button confirmCreate;
        private Text serverName;
        private readonly System.Collections.Generic.List<GameObject> selectionFrames = new System.Collections.Generic.List<GameObject>();
        private Font font;
        private bool ownsFont;
        private EntryCharacter pendingReady;
        private int selectedJob, selectedServerId;
        private int modalDismissedFrame = -1;
        private bool busy;
        private bool hasRenderedStage;
        private EntryStage renderedStage;
        public EntryStage Stage => client?.Stage ?? EntryStage.Login;
        public EntryCharacter Character => client?.Character;
        public bool UsesLoginAndServerSelection => useLoginAndServerSelection;
        public event Action<EntryCharacter> CharacterReady;

        private void Start()
        {
            if (art == null || !art.IsConfigured)
            { Debug.LogError("Original entry assets are missing.", this); enabled = false; return; }
            client = new EntryClient(GetComponent<LocalServerManager>());
            client.CharacterReady += value => pendingReady = value;
            BuildUi();
            if (useLoginAndServerSelection) ShowStage(); else OpenLocalProfile();
        }
        private void BuildUi()
        {
            ownedCanvas = new GameObject("Original Entry Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = ownedCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            var scaler = ownedCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 480); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var black = new GameObject("Letterbox", typeof(RectTransform), typeof(Image)); black.transform.SetParent(ownedCanvas.transform, false);
            var br = (RectTransform)black.transform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            black.GetComponent<Image>().color = Color.black; black.GetComponent<Image>().raycastTarget = false;
            screen = new GameObject("Native 640x480", typeof(RectTransform)).GetComponent<RectTransform>(); screen.SetParent(ownedCanvas.transform, false);
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(.5f, .5f); screen.sizeDelta = new Vector2(640, 480);
            if (EventSystem.current == null)
                ownedEvents = new GameObject("Original Entry Events", typeof(EventSystem), typeof(StandaloneInputModule));
            font = Font.CreateDynamicFontFromOSFont(new[] { "굴림", "Gulim", "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR" }, 12);
            ownsFont = font != null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Picture(screen, "Logo6", new Rect(0, 0, 640, 480), art.background);
            BuildLogin(); BuildServers(); BuildCreation();
        }
        private RectTransform Group(RectTransform parent, string title, Rect rect)
        {
            var transform = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>(); transform.SetParent(parent, false);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 1);
            transform.anchoredPosition = new Vector2(rect.x, -rect.y); transform.sizeDelta = rect.size; return transform;
        }
        private RawImage Picture(RectTransform parent, string name, Rect rect, Texture texture, Rect? source = null)
        {
            var rt = Group(parent, name, rect); var image = rt.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
            if (source.HasValue) image.uvRect = NativeEntryLayout.Uv(texture, source.Value);
            return image;
        }
        private Text Label(RectTransform parent, string name, Rect rect, string value, Color color, int size = 12)
        {
            var label = Group(parent, name, rect).gameObject.AddComponent<Text>(); label.font = font; label.fontSize = size;
            label.fontStyle = FontStyle.Bold; label.alignment = TextAnchor.UpperCenter; label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow; label.raycastTarget = false; label.text = value; return label;
        }
        private Button Hit(RectTransform parent, string name, Rect rect, Action callback)
        {
            var rt = Group(parent, name, rect); var graphic = rt.gameObject.AddComponent<Image>(); graphic.color = Color.clear;
            var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; button.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<NativeExclusiveHit>(); button.onClick.AddListener(() => { if (!busy) callback(); }); return button;
        }
        private Button AtlasButton(RectTransform parent, string name, Vector2 location, Texture atlas, Rect normal, Rect hover, Rect pressed, Action callback, Rect? hitBounds = null)
        {
            var bounds = hitBounds ?? new Rect(location, normal.size);
            var button = Hit(parent, name, bounds, callback);
            var image = Picture((RectTransform)button.transform, "Native pixels", new Rect(location - bounds.position, normal.size), atlas, normal);
            button.gameObject.AddComponent<NativeEntryButton>().Configure(image, atlas, normal, hover, pressed); return button;
        }
        private InputField Input(RectTransform parent, string name, Rect hit, Vector2 textOffset, bool password = false)
        {
            var rt = Group(parent, name, hit); var graphic = rt.gameObject.AddComponent<Image>(); graphic.color = Color.clear;
            var text = Label(rt, "Text", new Rect(textOffset, new Vector2(hit.width, 16)), "", Color.white);
            var input = rt.gameObject.AddComponent<InputField>(); input.textComponent = text; input.targetGraphic = graphic;
            input.transition = Selectable.Transition.None; input.lineType = InputField.LineType.SingleLine;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.asteriskChar = '*'; input.customCaretColor = true; input.caretColor = Color.white;
            rt.gameObject.AddComponent<NativeExclusiveHit>(); return input;
        }
        private void BuildLogin()
        {
            login = Group(screen, "Login", new Rect(0, 0, 640, 480));
            Picture(login, "Panel", NativeEntryLayout.LoginPanel, art.login, new Rect(9, 9, 197, 87));
            idInput = Input(login, "Account ID", NativeEntryLayout.IdHit, new Vector2(-1, -2));
            passwordInput = Input(login, "Password", NativeEntryLayout.PasswordHit, new Vector2(-1, -2), true);
            var idNav = idInput.navigation; idNav.mode = Navigation.Mode.Explicit; idNav.selectOnDown = passwordInput; idInput.navigation = idNav;
            var passNav = passwordInput.navigation; passNav.mode = Navigation.Mode.Explicit; passNav.selectOnUp = idInput; passwordInput.navigation = passNav;
            AtlasButton(login, "Connect", new Vector2(239, 339), art.login, new Rect(22, 69, 51, 17), new Rect(10, 379, 55, 19), new Rect(77, 379, 50, 16), () => Login());
            AtlasButton(login, "Web signup", new Vector2(294, 339), art.login, new Rect(77, 69, 35, 17), new Rect(209, 55, 38, 19), new Rect(209, 79, 38, 16), () => ShowMessage(false, true));
            AtlasButton(login, "Options", new Vector2(334, 339), art.login, new Rect(117, 69, 35, 17), new Rect(209, 10, 38, 19), new Rect(209, 34, 35, 16), () => drawer.SetActive(!drawer.activeSelf));
            AtlasButton(login, "Exit", new Vector2(374, 339), art.login, new Rect(157, 69, 36, 17), new Rect(252, 54, 38, 19), new Rect(252, 79, 35, 16), Quit);
            drawer = Group(login, "Options drawer", new Rect(226, 360, 197, 45)).gameObject;
            Picture((RectTransform)drawer.transform, "Original drawer", new Rect(0, 0, 197, 45), art.login, new Rect(9, 100, 197, 45));
            // Native billing/mode backend is unavailable; do not invent subscription rules.
            Hit((RectTransform)drawer.transform, "Retired service", new Rect(0, 0, 197, 45), () => ShowMessage(false, true)); drawer.SetActive(false);
        }
        private void BuildServers()
        {
            servers = Group(screen, "Server selection", new Rect(0, 0, 640, 480));
            Picture(servers, "Native two-row panel", NativeEntryLayout.ServerPanel, art.serverPanel);
            selectedServer = Picture(servers, "Selected row", new Rect(290, 307, 75, 19), art.serverSelected).gameObject;
            Hit(servers, "Local server row", NativeEntryLayout.ServerHit, () => { if (client.Servers.Length > 0) { selectedServerId = client.Servers[0].Id; selectedServer.SetActive(true); } });
            serverName = Label(servers, "Server name", new Rect(302, 309, 59, 16), "", art.serverNameColor);
            serverName.fontStyle = FontStyle.Normal; serverName.alignment = TextAnchor.MiddleLeft;
            // Both buttons are already painted in the native login2 panel. Do not cover them with login.bmp patches.
            Hit(servers, "Server connect", NativeEntryLayout.ServerConnectHit, SelectServer);
            Hit(servers, "Server exit", NativeEntryLayout.ServerExitHit, Logout);
        }
        private void BuildCreation()
        {
            creation = Group(screen, "First commander creation", NativeEntryLayout.CreationSize);
            creation.anchoredPosition = new Vector2(NativeEntryLayout.CreationOrigin.x, -NativeEntryLayout.CreationOrigin.y);
            Picture(creation, "Wlib scene24", NativeEntryLayout.CreationSize, art.creation);
            for (int i = 0; i < 3; i++)
            {
                int index = i; Picture(creation, "Face " + NativeEntryLayout.Jobs[i], NativeEntryLayout.Face(i), art.faces[i], new Rect(0, 0, 62, 62));
                Hit(creation, "Commander " + NativeEntryLayout.Jobs[i], NativeEntryLayout.Card(i), () => { selectedJob = index; RefreshSelection(); nameInput.ActivateInputField(); });
                var frame = Group(creation, "Selected commander " + NativeEntryLayout.Jobs[i], NativeEntryLayout.Card(i));
                // Explicit feedback requested in the direct test; exact native selected-card artwork is unconfirmed.
                var color = new Color32(255, 255, 64, 255);
                foreach (var edge in new[] { new Rect(0, 0, 90, 1), new Rect(0, 175, 90, 1), new Rect(0, 0, 1, 176), new Rect(89, 0, 1, 176) })
                { var line = Group(frame, "Selection edge", edge).gameObject.AddComponent<Image>(); line.color = color; line.raycastTarget = false; }
                selectionFrames.Add(frame.gameObject);
            }
            nameInput = Input(creation, "Commander name", NativeEntryLayout.NameHit, Vector2.zero); nameInput.textComponent.color = new Color32(255, 255, 64, 255); // Native COLORREF0x40FFFF at45BD49.
            nameInput.textComponent.fontSize = 14;
            // Source LTN75: native confirm states; name/selection are submitted only to the server.
            confirmCreate = AtlasButton(creation, "Create commander", new Vector2(300, 265), art.buttons,
                new Rect(276, 399, 46, 19), new Rect(322, 399, 46, 19), new Rect(368, 399, 46, 19), Create);
        }
        private void RefreshSelection()
        { for (int i = 0; i < selectionFrames.Count; i++) selectionFrames[i].SetActive(i == selectedJob); }
        public bool Login()
        {
            if (!useLoginAndServerSelection || busy || client == null || Stage != EntryStage.Login) return false;
            busy = true; bool ok;
            try { ok = client.Login(idInput.text, passwordInput.text); if (ok) passwordInput.text = ""; }
            finally { busy = false; }
            ShowStage(); if (!ok) ShowMessage(client.Failure == EntryFailure.InvalidCredentials); return ok;
        }
        private void SelectServer()
        { if (!useLoginAndServerSelection || selectedServerId <= 0 || busy) return; busy = true; bool ok; try { ok = client.SelectServer(selectedServerId); } finally { busy = false; } ShowStage(); if (!ok) ShowMessage(false); }
        private void OpenLocalProfile()
        {
            if (useLoginAndServerSelection || client == null || busy) return;
            busy = true; bool ok;
            try { ok = client.OpenLocalProfile(); } finally { busy = false; }
            ShowStage(); if (!ok) ShowMessage(false);
        }
        private void Create()
        {
            if (busy || string.IsNullOrWhiteSpace(nameInput.text)) { nameInput.ActivateInputField(); return; }
            busy = true; bool ok; try { ok = client.Create(NativeEntryLayout.Jobs[selectedJob], nameInput.text); } finally { busy = false; }
            ShowStage(); if (!ok) ShowMessage(false);
        }
        private void Logout() { client.Logout(); selectedServerId = 0; if (useLoginAndServerSelection) ShowStage(); else OpenLocalProfile(); }
        private void ShowStage()
        {
            bool entered = !hasRenderedStage || renderedStage != Stage;
            hasRenderedStage = true; renderedStage = Stage;
            login.gameObject.SetActive(useLoginAndServerSelection && Stage == EntryStage.Login);
            servers.gameObject.SetActive(useLoginAndServerSelection && Stage == EntryStage.ServerSelection);
            creation.gameObject.SetActive(Stage == EntryStage.FirstCharacterCreation);
            if (Stage == EntryStage.ServerSelection && entered) { selectedServerId = client.Servers.Length > 0 ? client.Servers[0].Id : 0; selectedServer.SetActive(selectedServerId > 0); serverName.text = client.Servers.Length > 0 ? client.Servers[0].Name : ""; }
            if (Stage == EntryStage.FirstCharacterCreation && entered) { selectedJob = 0; RefreshSelection(); nameInput.text = ""; nameInput.ActivateInputField(); }
            // CharacterReady is an integration boundary, not a fake town/tutorial transition.
            if (pendingReady != null)
            { var ready = pendingReady; pendingReady = null; CharacterReady?.Invoke(ready.Copy()); }
        }
        private void ShowMessage(bool credentials, bool unsupported = false)
        {
            if (modal != null) return;
            modal = Group(screen, "Native notification", new Rect(0, 0, 640, 480));
            var blocker = modal.gameObject.AddComponent<Image>(); blocker.color = Color.clear;
            Texture atlas = unsupported || !credentials ? art.secondary : art.login;
            Rect source = unsupported ? new Rect(9, 9, 208, 109) : credentials ? new Rect(214, 194, 216, 106) : new Rect(225, 9, 216, 109);
            Picture(modal, "Original message pixels", new Rect(212, 190, source.width, source.height), atlas, source);
            Rect confirm = credentials && !unsupported ? new Rect(265, 243, 38, 18)
                : unsupported ? new Rect(291, 264, 38, 18) : new Rect(293, 265, 38, 18);
            Hit(modal, "Message confirm", confirm, () =>
            {
                if (modal == null) return;
                var closing = modal; modal = null; modalDismissedFrame = Time.frameCount;
                closing.gameObject.SetActive(false); Destroy(closing.gameObject);
                if (!useLoginAndServerSelection && Stage == EntryStage.Login) OpenLocalProfile();
            }).Select();
        }
        private void Update()
        {
            if (client == null || modal != null || busy || modalDismissedFrame == Time.frameCount) return;
            if (useLoginAndServerSelection && UnityEngine.Input.GetKeyDown(KeyCode.Tab) && Stage == EntryStage.Login)
            { if (idInput.isFocused) passwordInput.ActivateInputField(); else idInput.ActivateInputField(); }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
            { if (Stage == EntryStage.Login) { if (useLoginAndServerSelection) Login(); else OpenLocalProfile(); } else if (Stage == EntryStage.ServerSelection) SelectServer(); else if (Stage == EntryStage.FirstCharacterCreation) Create(); }
        }
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        { if (ownedCanvas != null) Destroy(ownedCanvas); if (ownedEvents != null) Destroy(ownedEvents); if (font != null && ownsFont) Destroy(font); }
        private void OnDisable() { if (ownedCanvas != null) ownedCanvas.SetActive(false); }
        private void OnEnable()
        {
            if (ownedCanvas == null || client == null) return;
            client.Logout(); ownedCanvas.SetActive(true);
            if (useLoginAndServerSelection) ShowStage(); else OpenLocalProfile();
        }
    }
}
