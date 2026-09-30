using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using AIBusinessTycoon.UI;
using UnityEngine.EventSystems;

namespace AIBusinessTycoon.Editor
{
    /// <summary>
    /// Phase 5 editor utility.
    /// Builds the full professional leaderboard UI hierarchy in the active scene
    /// and auto-wires all references into LeaderboardPanel.
    /// Menu: AI Business Tycoon / Setup Leaderboard UI (Phase 5)
    /// </summary>
    public class LeaderboardSetupEditor : EditorWindow
    {
        // ── Colour palette ────────────────────────────────────────────────────
        private static readonly Color ColBackground  = new Color(0.06f, 0.07f, 0.11f, 0.96f);
        private static readonly Color ColCard        = new Color(0.10f, 0.12f, 0.18f, 1.00f);
        private static readonly Color ColAccent      = new Color(0.27f, 0.64f, 1.00f, 1.00f);
        private static readonly Color ColTabBar      = new Color(0.08f, 0.09f, 0.14f, 1.00f);
        private static readonly Color ColHeader      = new Color(0.14f, 0.16f, 0.24f, 1.00f);
        private static readonly Color ColClose       = new Color(0.90f, 0.25f, 0.25f, 1.00f);
        private static readonly Color ColOpenButton  = new Color(0.18f, 0.52f, 0.94f, 1.00f);
        private static readonly Color ColScrollTrack = new Color(0.10f, 0.12f, 0.18f, 0.00f);
        private static readonly Color ColScrollThumb = new Color(0.27f, 0.64f, 1.00f, 0.40f);

        // ── Font sizes ────────────────────────────────────────────────────────
        private const float FsTitleMain  = 22f;
        private const float FsTitleSub   = 11f;
        private const float FsTab        = 13f;
        private const float FsColHeader  = 11f;
        private const float FsRowRank    = 14f;
        private const float FsRowName    = 13f;
        private const float FsRowValue   = 13f;
        private const float FsEmpty      = 13f;
        private const float FsOpenBtn    = 13f;

        // ── References collected during build ─────────────────────────────────
        private static GameObject      _panelRoot;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _connectionStatus;
        private static TextMeshProUGUI _lastUpdated;
        private static Button          _closeBtn;
        private static Button          _tabNetWorth;
        private static Button          _tabRevenue;
        private static Button          _tabCustomers;
        private static Image           _underlineNetWorth;
        private static Image           _underlineRevenue;
        private static Image           _underlineCustomers;
        private static Transform       _rowContainer;
        private static GameObject      _rowPrefab;
        private static TextMeshProUGUI _emptyLabel;
        private static Button          _openBtn;

        // ── Menu entry ────────────────────────────────────────────────────────

        [MenuItem("AI Business Tycoon/Setup Leaderboard UI (Phase 5)")]
        public static void ShowWindow()
        {
            var win = GetWindow<LeaderboardSetupEditor>("Leaderboard UI Setup");
            win.minSize = new Vector2(440, 220);
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon — Leaderboard UI Setup (Phase 5)",
                EditorStyles.boldLabel);
            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Builds the full professional leaderboard UI in the active scene\n" +
                "and auto-wires all references into LeaderboardPanel.\n\n" +
                "Re-running is safe — existing objects are reused.",
                MessageType.Info);

            GUILayout.Space(12);

            GUI.backgroundColor = new Color(0.18f, 0.52f, 0.94f);
            if (GUILayout.Button("  🏆  Build Leaderboard UI + Auto-Wire", GUILayout.Height(48)))
                BuildAll();

            GUILayout.Space(8);

            GUI.backgroundColor = new Color(0.20f, 0.70f, 0.35f);
            if (GUILayout.Button("  🔗  Auto-Wire References Only", GUILayout.Height(36)))
                AutoWireOnly();

            GUI.backgroundColor = Color.white;
        }

        // ── Entry points ──────────────────────────────────────────────────────

        private static void BuildAll()
        {
            Canvas canvas   = GetOrCreateCanvas();
            EnsureEventSystem();
            BuildOpenButton(canvas);
            BuildPanel(canvas);
            AutoWireOnly();
            MarkSceneDirty();
            Debug.Log("[LeaderboardSetup] Leaderboard UI built and wired.");
        }

        private static void AutoWireOnly()
        {
            LeaderboardPanel panel = FindOrAddPanel();
            if (panel == null) { Debug.LogWarning("[LeaderboardSetup] LeaderboardPanel not found."); return; }

            var so = new SerializedObject(panel);

            SetRef(so, "panelRoot",              _panelRoot);
            SetRef(so, "titleLabel",             _titleLabel);
            SetRef(so, "connectionStatusLabel",  _connectionStatus);
            SetRef(so, "lastUpdatedLabel",       _lastUpdated);
            SetRef(so, "closeButton",            _closeBtn);
            SetRef(so, "tabNetWorth",            _tabNetWorth);
            SetRef(so, "tabRevenue",             _tabRevenue);
            SetRef(so, "tabCustomers",           _tabCustomers);
            SetRef(so, "tabNetWorthUnderline",   _underlineNetWorth);
            SetRef(so, "tabRevenueUnderline",    _underlineRevenue);
            SetRef(so, "tabCustomersUnderline",  _underlineCustomers);
            SetRef(so, "rowContainer",           _rowContainer);
            SetRef(so, "rowPrefab",              _rowPrefab);
            SetRef(so, "emptyLabel",             _emptyLabel);
            SetRef(so, "openButton",             _openBtn);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
            Debug.Log("[LeaderboardSetup] References wired into LeaderboardPanel.");
        }

        // ── Canvas / EventSystem ──────────────────────────────────────────────

        private static Canvas GetOrCreateCanvas()
        {
            const string NAME = "Leaderboard Canvas";
            GameObject go = GameObject.Find(NAME);
            if (go == null)
            {
                go = new GameObject(NAME);
                Undo.RegisterCreatedObjectUndo(go, "Create Leaderboard Canvas");
            }

            Canvas cv = go.GetComponent<Canvas>();
            if (cv == null) cv = go.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 20;

            CanvasScaler sc = go.GetComponent<CanvasScaler>();
            if (sc == null) sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution  = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight   = 0.5f;

            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();

            return cv;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // ── HUD Open button ───────────────────────────────────────────────────

        private static void BuildOpenButton(Canvas canvas)
        {
            const string NAME = "Leaderboard Open Button";
            GameObject existing = canvas.transform.Find(NAME)?.gameObject;
            _openBtn = existing != null
                ? existing.GetComponent<Button>()
                : null;

            if (_openBtn == null)
            {
                GameObject go = new GameObject(NAME, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Leaderboard Open Button");

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(1f, 0f);
                rt.anchorMax        = new Vector2(1f, 0f);
                rt.pivot            = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-24f, 24f);
                rt.sizeDelta        = new Vector2(172f, 44f);

                Image img = go.GetComponent<Image>();
                img.color = ColOpenButton;
                ApplyRoundedSprite(img);

                _openBtn = go.GetComponent<Button>();

                TextMeshProUGUI label = CreateTMP(go.transform, "Label", "🏆  LEADERBOARD", FsOpenBtn);
                label.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                label.GetComponent<RectTransform>().anchorMax = Vector2.one;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color     = Color.white;
            }
        }

        // ── Main panel ────────────────────────────────────────────────────────

        private static void BuildPanel(Canvas canvas)
        {
            const string NAME = "Leaderboard Panel Root";
            GameObject existing = canvas.transform.Find(NAME)?.gameObject;
            _panelRoot = existing ?? CreatePanelRoot(canvas, NAME);

            // Background card
            Image bg = _panelRoot.GetComponent<Image>();
            if (bg == null) bg = _panelRoot.AddComponent<Image>();
            bg.color = ColBackground;
            ApplyRoundedSprite(bg);

            // Position: centre-right
            RectTransform rt = _panelRoot.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-24f, 0f);
            rt.sizeDelta        = new Vector2(420f, 600f);

            BuildHeader(_panelRoot.transform);
            BuildTabBar(_panelRoot.transform);
            BuildColumnHeaders(_panelRoot.transform);
            BuildScrollArea(_panelRoot.transform);
        }

        private static GameObject CreatePanelRoot(Canvas canvas, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Leaderboard Panel Root");
            return go;
        }

        // ── Header ────────────────────────────────────────────────────────────

        private static void BuildHeader(Transform parent)
        {
            const string NAME = "Header";
            Transform existing = parent.Find(NAME);
            GameObject hdr = existing?.gameObject ?? CreateStrip(parent, NAME, 64f, 0f);

            Image hdrImg = hdr.GetComponent<Image>();
            if (hdrImg == null) hdrImg = hdr.AddComponent<Image>();
            hdrImg.color = ColHeader;

            RectTransform rt = hdr.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -64f);
            rt.offsetMax = Vector2.zero;

            // Trophy icon + Title
            Transform titleGO = hdr.transform.Find("Title") ??
                CreateTMP(hdr.transform, "Title", "🏆  LEADERBOARD", FsTitleMain).transform;
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color     = Color.white;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            _titleLabel = titleTMP;
            RectTransform titleRT = titleTMP.GetComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 0f);
            titleRT.anchorMax        = new Vector2(0.7f, 1f);
            titleRT.offsetMin        = new Vector2(16f, 0f);
            titleRT.offsetMax        = new Vector2(0f, 0f);

            // Connection status badge
            Transform statusGO = hdr.transform.Find("ConnectionStatus") ??
                CreateTMP(hdr.transform, "ConnectionStatus", "● CONNECTING", FsTitleSub).transform;
            _connectionStatus = statusGO.GetComponent<TextMeshProUGUI>();
            _connectionStatus.color     = new Color(1f, 0.65f, 0.1f);
            _connectionStatus.alignment = TextAlignmentOptions.MidlineLeft;
            _connectionStatus.fontStyle = FontStyles.Bold;
            RectTransform statusRT = _connectionStatus.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0f, 0f);
            statusRT.anchorMax = new Vector2(0.5f, 0.38f);
            statusRT.offsetMin = new Vector2(18f, 0f);
            statusRT.offsetMax = new Vector2(0f, 0f);

            // Last updated timestamp (right side)
            Transform updatedGO = hdr.transform.Find("LastUpdated") ??
                CreateTMP(hdr.transform, "LastUpdated", "", FsTitleSub).transform;
            _lastUpdated = updatedGO.GetComponent<TextMeshProUGUI>();
            _lastUpdated.color     = new Color(0.55f, 0.55f, 0.55f);
            _lastUpdated.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform updatedRT = _lastUpdated.GetComponent<RectTransform>();
            updatedRT.anchorMin = new Vector2(0.5f, 0f);
            updatedRT.anchorMax = new Vector2(1f,   0.5f);
            updatedRT.offsetMin = new Vector2(0f, 0f);
            updatedRT.offsetMax = new Vector2(-52f, 0f);

            // Close button (X)
            Transform closeGO = hdr.transform.Find("CloseButton");
            if (closeGO == null)
            {
                var cgo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                cgo.transform.SetParent(hdr.transform, false);
                Undo.RegisterCreatedObjectUndo(cgo, "Create Close Button");
                closeGO = cgo.transform;
            }
            Image closeImg = closeGO.GetComponent<Image>();
            closeImg.color = ColClose;
            ApplyRoundedSprite(closeImg);
            _closeBtn = closeGO.GetComponent<Button>();
            RectTransform closeRT = closeGO.GetComponent<RectTransform>();
            closeRT.anchorMin        = new Vector2(1f, 0.5f);
            closeRT.anchorMax        = new Vector2(1f, 0.5f);
            closeRT.pivot            = new Vector2(1f, 0.5f);
            closeRT.anchoredPosition = new Vector2(-10f, 0f);
            closeRT.sizeDelta        = new Vector2(32f, 32f);

            Transform closeLabelT = closeGO.transform.Find("X") ??
                CreateTMP(closeGO, "X", "✕", 13f).transform;
            var xl = closeLabelT.GetComponent<TextMeshProUGUI>();
            xl.alignment = TextAlignmentOptions.Center;
            xl.color     = Color.white;
            xl.fontStyle = FontStyles.Bold;
            var xlRT = xl.GetComponent<RectTransform>();
            xlRT.anchorMin = Vector2.zero;
            xlRT.anchorMax = Vector2.one;
            xlRT.offsetMin = Vector2.zero;
            xlRT.offsetMax = Vector2.zero;
        }

        // ── Tab bar ───────────────────────────────────────────────────────────

        private static void BuildTabBar(Transform parent)
        {
            const string NAME = "TabBar";
            Transform existing = parent.Find(NAME);
            GameObject bar = existing?.gameObject ?? CreateStrip(parent, NAME, 44f, -64f);

            Image barImg = bar.GetComponent<Image>();
            if (barImg == null) barImg = bar.AddComponent<Image>();
            barImg.color = ColTabBar;

            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -108f);
            rt.offsetMax = new Vector2(0f, -64f);

            float w = 1f / 3f;
            BuildTab(bar.transform, "Tab_NetWorth",  "Net Worth",  0f,       w,  out _tabNetWorth,  out _underlineNetWorth);
            BuildTab(bar.transform, "Tab_Revenue",   "Revenue",    w,        2*w, out _tabRevenue,   out _underlineRevenue);
            BuildTab(bar.transform, "Tab_Customers", "Customers",  2*w,      1f,  out _tabCustomers, out _underlineCustomers);
        }

        private static void BuildTab(
            Transform parent, string name, string label,
            float anchorXMin, float anchorXMax,
            out Button btn, out Image underline)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Tab " + name);
            }

            go.GetComponent<Image>().color = Color.clear;
            btn = go.GetComponent<Button>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorXMin, 0f);
            rt.anchorMax = new Vector2(anchorXMax, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Label
            Transform lblT = go.transform.Find("Label") ??
                CreateTMP(go.transform, "Label", label, FsTab).transform;
            var lbl = lblT.GetComponent<TextMeshProUGUI>();
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;
            lbl.fontStyle = FontStyles.Bold;
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0.2f);
            lblRT.anchorMax = new Vector2(1f, 1.0f);
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;

            // Underline
            Transform ulT = go.transform.Find("Underline");
            if (ulT == null)
            {
                var ul = new GameObject("Underline", typeof(RectTransform), typeof(Image));
                ul.transform.SetParent(go.transform, false);
                Undo.RegisterCreatedObjectUndo(ul, "Create Tab Underline");
                ulT = ul.transform;
            }
            underline = ulT.GetComponent<Image>();
            underline.color = Color.clear;
            RectTransform ulRT = ulT.GetComponent<RectTransform>();
            ulRT.anchorMin = new Vector2(0.05f, 0f);
            ulRT.anchorMax = new Vector2(0.95f, 0f);
            ulRT.pivot     = new Vector2(0.5f,  0f);
            ulRT.sizeDelta = new Vector2(0f, 3f);
            ulRT.anchoredPosition = Vector2.zero;
        }

        // ── Column headers ────────────────────────────────────────────────────

        private static void BuildColumnHeaders(Transform parent)
        {
            const string NAME = "ColumnHeaders";
            Transform existing = parent.Find(NAME);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(NAME, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Column Headers");
            }

            Image img = go.GetComponent<Image>();
            img.color = ColHeader;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -138f);
            rt.offsetMax = new Vector2(0f, -108f);

            BuildColHeader(go.transform, "ColRank",  "#",      0.00f, 0.14f);
            BuildColHeader(go.transform, "ColName",  "PLAYER", 0.14f, 0.65f);
            BuildColHeader(go.transform, "ColValue", "SCORE",  0.65f, 1.00f);
        }

        private static void BuildColHeader(
            Transform parent, string name, string text,
            float xMin, float xMax)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Col Header " + name);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Transform lblT = go.transform.Find("Label") ??
                CreateTMP(go.transform, "Label", text, FsColHeader).transform;
            var lbl = lblT.GetComponent<TextMeshProUGUI>();
            lbl.color     = new Color(0.55f, 0.55f, 0.55f);
            lbl.alignment = xMin < 0.1f
                ? TextAlignmentOptions.Center
                : xMin < 0.5f
                    ? TextAlignmentOptions.MidlineLeft
                    : TextAlignmentOptions.MidlineRight;
            lbl.fontStyle = FontStyles.Bold;
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = new Vector2(-8f, 0f);
        }

        // ── Scroll view + rows ────────────────────────────────────────────────

        private static void BuildScrollArea(Transform parent)
        {
            const string SCROLL_NAME    = "ScrollView";
            const string VIEWPORT_NAME  = "Viewport";
            const string CONTENT_NAME   = "Content";
            const string EMPTY_NAME     = "EmptyLabel";
            const string ROW_PREFAB_NAME = "RowPrefab";

            // ScrollView
            Transform existing = parent.Find(SCROLL_NAME);
            GameObject scrollGO;
            if (existing != null)
            {
                scrollGO = existing.gameObject;
            }
            else
            {
                scrollGO = new GameObject(SCROLL_NAME, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollGO.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(scrollGO, "Create ScrollView");
            }

            Image scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = Color.clear;

            RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(0f,  0f);
            scrollRT.offsetMax = new Vector2(0f, -138f);

            // Viewport
            Transform vpT = scrollGO.transform.Find(VIEWPORT_NAME);
            GameObject vp;
            if (vpT != null)
            {
                vp = vpT.gameObject;
            }
            else
            {
                vp = new GameObject(VIEWPORT_NAME, typeof(RectTransform), typeof(Image), typeof(Mask));
                vp.transform.SetParent(scrollGO.transform, false);
                Undo.RegisterCreatedObjectUndo(vp, "Create Viewport");
            }

            vp.GetComponent<Image>().color = Color.clear;
            vp.GetComponent<Mask>().showMaskGraphic = false;
            RectTransform vpRT = vp.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;

            // Content
            Transform contentT = vp.transform.Find(CONTENT_NAME);
            GameObject content;
            if (contentT != null)
            {
                content = contentT.gameObject;
            }
            else
            {
                content = new GameObject(CONTENT_NAME, typeof(RectTransform));
                content.transform.SetParent(vp.transform, false);
                Undo.RegisterCreatedObjectUndo(content, "Create Content");
            }

            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f;
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _rowContainer = content.transform;

            // Wire ScrollRect
            ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
            sr.content    = contentRT;
            sr.viewport   = vpRT;
            sr.horizontal = false;
            sr.vertical   = true;
            sr.scrollSensitivity = 30f;
            sr.movementType      = ScrollRect.MovementType.Clamped;

            // Scrollbar
            BuildScrollbar(scrollGO, sr);

            // Empty label
            Transform emptyT = scrollGO.transform.Find(EMPTY_NAME);
            GameObject emptyGO;
            if (emptyT != null)
            {
                emptyGO = emptyT.gameObject;
            }
            else
            {
                emptyGO = new GameObject(EMPTY_NAME, typeof(RectTransform), typeof(TextMeshProUGUI));
                emptyGO.transform.SetParent(scrollGO.transform, false);
                Undo.RegisterCreatedObjectUndo(emptyGO, "Create Empty Label");
            }

            _emptyLabel = emptyGO.GetComponent<TextMeshProUGUI>();
            _emptyLabel.text      = "Connecting to leaderboard server...";
            _emptyLabel.fontSize  = FsEmpty;
            _emptyLabel.color     = new Color(0.55f, 0.55f, 0.55f);
            _emptyLabel.alignment = TextAlignmentOptions.Center;
            RectTransform emptyRT = emptyGO.GetComponent<RectTransform>();
            emptyRT.anchorMin = Vector2.zero;
            emptyRT.anchorMax = Vector2.one;
            emptyRT.offsetMin = Vector2.zero;
            emptyRT.offsetMax = Vector2.zero;

            // Row prefab (hidden template)
            Transform prefabT = scrollGO.transform.Find(ROW_PREFAB_NAME);
            GameObject prefab;
            if (prefabT != null)
            {
                prefab = prefabT.gameObject;
            }
            else
            {
                prefab = BuildRowPrefab(scrollGO.transform);
            }
            prefab.SetActive(false);
            _rowPrefab = prefab;
        }

        private static void BuildScrollbar(GameObject parent, ScrollRect sr)
        {
            const string NAME = "Scrollbar";
            Transform existing = parent.transform.Find(NAME);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(NAME, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
                go.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(go, "Create Scrollbar");
            }

            go.GetComponent<Image>().color = ColScrollTrack;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(6f, 0f);

            // Sliding area
            Transform slidingT = go.transform.Find("SlidingArea");
            GameObject sliding;
            if (slidingT != null)
            {
                sliding = slidingT.gameObject;
            }
            else
            {
                sliding = new GameObject("SlidingArea", typeof(RectTransform));
                sliding.transform.SetParent(go.transform, false);
                Undo.RegisterCreatedObjectUndo(sliding, "Create Sliding Area");
            }
            RectTransform slidingRT = sliding.GetComponent<RectTransform>();
            slidingRT.anchorMin = Vector2.zero;
            slidingRT.anchorMax = Vector2.one;
            slidingRT.offsetMin = Vector2.zero;
            slidingRT.offsetMax = Vector2.zero;

            // Handle
            Transform handleT = sliding.transform.Find("Handle");
            GameObject handle;
            if (handleT != null)
            {
                handle = handleT.gameObject;
            }
            else
            {
                handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handle.transform.SetParent(sliding.transform, false);
                Undo.RegisterCreatedObjectUndo(handle, "Create Handle");
            }
            handle.GetComponent<Image>().color = ColScrollThumb;
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = Vector2.zero;

            Scrollbar sb = go.GetComponent<Scrollbar>();
            sb.direction     = Scrollbar.Direction.BottomToTop;
            sb.handleRect    = handleRT;
            sb.targetGraphic = handle.GetComponent<Image>();

            sr.verticalScrollbar            = sb;
            sr.verticalScrollbarVisibility  = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            sr.verticalScrollbarSpacing     = -3f;
        }

        private static GameObject BuildRowPrefab(Transform parent)
        {
            var go = new GameObject("RowPrefab", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Row Prefab");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 48f);

            go.GetComponent<Image>().color = Color.clear;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.childControlWidth     = false;
            hlg.childControlHeight    = true;
            hlg.childForceExpandWidth = false;
            hlg.padding               = new RectOffset(8, 8, 0, 0);
            hlg.spacing               = 0f;

            BuildRowCell(go.transform, "Rank",  14f, 52f,  TextAlignmentOptions.Center,         FontStyles.Bold,   new Color(0.8f, 0.8f, 0.8f));
            BuildRowCell(go.transform, "Name",  13f, 220f, TextAlignmentOptions.MidlineLeft,     FontStyles.Normal, Color.white);
            BuildRowCell(go.transform, "Value", 13f, 120f, TextAlignmentOptions.MidlineRight,    FontStyles.Bold,   ColAccent);

            return go;
        }

        private static void BuildRowCell(
            Transform parent, string name, float fontSize, float width,
            TextAlignmentOptions align, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Row Cell " + name);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 0f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text      = name;
            tmp.fontSize  = fontSize;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.color     = color;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject CreateStrip(Transform parent, string name, float height, float yOffset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static TextMeshProUGUI CreateTMP(
            Transform parent, string name, string text, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create TMP " + name);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = size;
            tmp.color         = Color.white;
            tmp.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return tmp;
        }

        private static Transform CreateTMP(
            GameObject parent, string name, string text, float size)
            => CreateTMP(parent.transform, name, text, size).transform;

        private static void ApplyRoundedSprite(Image img)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (rounded != null)
            {
                img.sprite = rounded;
                img.type   = Image.Type.Sliced;
            }
        }

        private static LeaderboardPanel FindOrAddPanel()
        {
            LeaderboardPanel existing = Object.FindFirstObjectByType<LeaderboardPanel>();
            if (existing != null) return existing;

            Canvas canvas = GetOrCreateCanvas();
            return Undo.AddComponent<LeaderboardPanel>(canvas.gameObject);
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            if (value == null) return;
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
