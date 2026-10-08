using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using AIBusinessTycoon.Authentication;
using AIBusinessTycoon.UI;

namespace AIBusinessTycoon.Editor
{
    public class LoginSceneSetupEditor : EditorWindow
    {
        // ── Colour Palette ────────────────────────────────────────────────────
        private static readonly Color BgTop      = new Color(0.07f, 0.07f, 0.14f, 1.00f);
        private static readonly Color CardBg     = new Color(0.10f, 0.10f, 0.18f, 0.98f);
        private static readonly Color PrimaryBlue= new Color(0.20f, 0.52f, 0.98f, 1.00f);
        private static readonly Color GreenBtn   = new Color(0.12f, 0.72f, 0.44f, 1.00f);
        private static readonly Color GuestBtn   = new Color(0.55f, 0.35f, 0.90f, 1.00f);
        private static readonly Color InputBg    = new Color(0.15f, 0.15f, 0.24f, 1.00f);
        private static readonly Color WhiteText  = new Color(1.00f, 1.00f, 1.00f, 0.92f);
        private static readonly Color SubText    = new Color(1.00f, 1.00f, 1.00f, 0.50f);
        private static readonly Color ErrorRed   = new Color(0.95f, 0.30f, 0.30f, 1.00f);
        private static readonly Color LoadingBg  = new Color(0.05f, 0.05f, 0.10f, 0.97f);

        [MenuItem("AI Business Tycoon/Setup Login Scene")]
        public static void ShowWindow()
        {
            var w = GetWindow<LoginSceneSetupEditor>("Login Scene Setup");
            w.minSize = new Vector2(420, 260);
            w.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon — Login Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Creates a complete Login scene with Login / Register / Guest panels,\n" +
                "input fields, loading overlay, error toast, and all scripts wired up.\n\n" +
                "Run this inside an EMPTY new scene named 'Login'.",
                MessageType.Info);
            GUILayout.Space(12);
            GUI.backgroundColor = PrimaryBlue;
            if (GUILayout.Button("🔐  Create Login Scene  (Full Build)", GUILayout.Height(50)))
                CreateLoginScene();
            GUI.backgroundColor = Color.white;
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "After running:\n" +
                "1. Assign BackendConfig to TycoonAuthService on the Managers object.\n" +
                "2. Add Login (Scene 0) and Game (Scene 1) to File > Build Settings.",
                MessageType.Warning);
        }

        // ═══════════════════════════════════════════════════════════════════════
        private static void CreateLoginScene()
        {
            // ── Canvas ───────────────────────────────────────────────────────
            var canvasGO = new GameObject("LoginCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── EventSystem ──────────────────────────────────────────────────
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            // ── Managers ─────────────────────────────────────────────────────
            var managersGO = new GameObject("Managers");
            managersGO.AddComponent<TycoonAuthService>();

            // ── Background + Card ────────────────────────────────────────────
            var bgGO = CreatePanel("Background", canvasGO.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bgGO.GetComponent<Image>().color = BgTop;

            var cardGO = CreatePanel("LoginCard", bgGO.transform,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cardGO.GetComponent<Image>().color = CardBg;

            BuildLoginCardContents(cardGO, canvasGO, managersGO);
        }

        private static void BuildLoginCardContents(GameObject cardGO, GameObject canvasGO, GameObject managersGO)
        {
            CreateText("TitleText", cardGO.transform, "AI Business Tycoon",
                72, FontStyles.Bold, WhiteText,
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.95f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var subtitleGO = CreateText("SubtitleText", cardGO.transform, "Welcome Back!",
                44, FontStyles.Normal, SubText,
                new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.84f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var nameFieldGO = CreateInputField("NameInput", cardGO.transform, "Your Name",
                new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.74f));
            nameFieldGO.SetActive(false);

            var emailFieldGO = CreateInputField("EmailInput", cardGO.transform, "Email Address",
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.65f));

            var passFieldGO = CreateInputField("PasswordInput", cardGO.transform, "Password",
                new Vector2(0.05f, 0.49f), new Vector2(0.95f, 0.56f));
            passFieldGO.GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.Password;

            var statusGO = CreateText("StatusText", cardGO.transform, "",
                28, FontStyles.Normal, ErrorRed,
                new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.49f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var loginBtnGO    = CreateButton("LoginButton",    cardGO.transform, "LOGIN",           PrimaryBlue, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.43f));
            var registerBtnGO = CreateButton("RegisterButton", cardGO.transform, "CREATE ACCOUNT",  GreenBtn,    new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.43f));
            registerBtnGO.SetActive(false);

            var toggleBtnGO = CreateButton("ToggleModeButton", cardGO.transform, "", new Color(0,0,0,0), new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.34f));
            toggleBtnGO.GetComponent<Image>().color = new Color(0,0,0,0);

            var toggleModeLabelGO = CreateText("ToggleModeText", cardGO.transform, "New here? Register",
                30, FontStyles.Normal, PrimaryBlue,
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.34f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var guestBtnGO = CreateButton("GuestButton", cardGO.transform, "Play as Guest", GuestBtn, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.26f));

            var toastGO = CreatePanel("ErrorToast", cardGO.transform,
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.14f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            toastGO.GetComponent<Image>().color = ErrorRed;
            var toastTextGO = CreateText("ToastText", toastGO.transform, "Error",
                28, FontStyles.Normal, Color.white, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            toastGO.SetActive(false);

            WireSceneAndFinish(canvasGO, managersGO, cardGO,
                subtitleGO, nameFieldGO, emailFieldGO, passFieldGO, statusGO,
                loginBtnGO, registerBtnGO, toggleBtnGO, toggleModeLabelGO,
                guestBtnGO, toastGO, toastTextGO);
        }

        private static void WireSceneAndFinish(
            GameObject canvasGO, GameObject managersGO, GameObject cardGO,
            GameObject subtitleGO, GameObject nameFieldGO, GameObject emailFieldGO,
            GameObject passFieldGO, GameObject statusGO,
            GameObject loginBtnGO, GameObject registerBtnGO,
            GameObject toggleBtnGO, GameObject toggleModeLabelGO,
            GameObject guestBtnGO, GameObject toastGO, GameObject toastTextGO)
        {
            var loadingGO = CreatePanel("LoadingPanel", canvasGO.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            loadingGO.GetComponent<Image>().color = LoadingBg;
            var loadingTextGO = CreateText("LoadingText", loadingGO.transform, "Loading...",
                52, FontStyles.Bold, WhiteText,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            loadingGO.SetActive(false);

            var uiManager = canvasGO.AddComponent<LoginUIManager>();
            SetFieldValue(uiManager, "loginPanel",       cardGO);
            SetFieldValue(uiManager, "loadingPanel",     loadingGO);
            SetFieldValue(uiManager, "nameInput",        nameFieldGO.GetComponent<TMP_InputField>());
            SetFieldValue(uiManager, "emailInput",       emailFieldGO.GetComponent<TMP_InputField>());
            SetFieldValue(uiManager, "passwordInput",    passFieldGO.GetComponent<TMP_InputField>());
            SetFieldValue(uiManager, "loginButton",      loginBtnGO.GetComponent<Button>());
            SetFieldValue(uiManager, "registerButton",   registerBtnGO.GetComponent<Button>());
            SetFieldValue(uiManager, "guestButton",      guestBtnGO.GetComponent<Button>());
            SetFieldValue(uiManager, "toggleModeButton", toggleBtnGO.GetComponent<Button>());
            SetFieldValue(uiManager, "titleText",        subtitleGO.GetComponent<TextMeshProUGUI>());
            SetFieldValue(uiManager, "statusText",       statusGO.GetComponent<TextMeshProUGUI>());
            SetFieldValue(uiManager, "toggleModeText",   toggleModeLabelGO.GetComponent<TextMeshProUGUI>());
            SetFieldValue(uiManager, "loadingText",      loadingTextGO.GetComponent<TextMeshProUGUI>());
            SetFieldValue(uiManager, "errorToast",       toastGO);
            SetFieldValue(uiManager, "errorToastText",   toastTextGO.GetComponent<TextMeshProUGUI>());

            Undo.RegisterCreatedObjectUndo(canvasGO,   "Create Login Scene");
            Undo.RegisterCreatedObjectUndo(managersGO, "Create Login Scene");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[LoginSceneSetupEditor] Login scene built!");
            EditorUtility.DisplayDialog("Login Scene Created!",
                "Login scene built!\n\n" +
                "1. Assign BackendConfig to TycoonAuthService (Managers).\n" +
                "2. Save as 'Login.unity'.\n" +
                "3. Add Login (Scene 0) + Game (Scene 1) to Build Settings.",
                "Got it!");
        }
        #region UI Helpers

        private static GameObject CreatePanel(string name, Transform parent,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.AddComponent<Image>();
            return go;
        }

        private static GameObject CreateText(string name, Transform parent,
            string text, int size, FontStyles style, Color color,
            Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 delta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = delta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size;
            tmp.fontStyle = style; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent,
            string label, Color bgColor, Vector2 min, Vector2 max)
        {
            var go = CreatePanel(name, parent, min, max,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = bgColor;
            go.AddComponent<Button>();
            var txtGO = CreateText("Label", go.transform, label, 38, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            txtGO.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            return go;
        }

        private static GameObject CreateInputField(string name, Transform parent,
            string placeholder, Vector2 min, Vector2 max)
        {
            var go = CreatePanel(name, parent, min, max,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            go.GetComponent<Image>().color = InputBg;
            var inputField = go.AddComponent<TMP_InputField>();

            var phGO = CreateText("Placeholder", go.transform, placeholder,
                32, FontStyles.Italic, new Color(1, 1, 1, 0.4f),
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            phGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var txtGO = CreateText("Text", go.transform, "",
                32, FontStyles.Normal, Color.white,
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            txtGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            inputField.textComponent = txtGO.GetComponent<TextMeshProUGUI>();
            inputField.placeholder   = phGO.GetComponent<TextMeshProUGUI>();
            inputField.targetGraphic = go.GetComponent<Image>();
            return go;
        }

        private static void SetFieldValue(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public    |
                System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
            else Debug.LogWarning($"[LoginSceneSetupEditor] Field '{fieldName}' not found.");
        }

        #endregion
    }
}





