using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.Editor
{
    /// <summary>
    /// Phase 4 editor utility. Creates the Rain Particle System and World State
    /// HUD label in the active scene, then auto-wires both into WorldStateController.
    /// Menu: AI Business Tycoon / Setup World State (Phase 4)
    /// </summary>
    public class WorldStateSetupEditor : EditorWindow
    {
        private readonly Color _sky  = new Color(0.12f, 0.53f, 0.90f);
        private readonly Color _dark = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        private readonly Color _grn  = new Color(0.20f, 0.80f, 0.30f);

        [MenuItem("AI Business Tycoon/Setup World State (Phase 4)")]
        public static void ShowWindow()
        {
            var win = GetWindow<WorldStateSetupEditor>("World State Setup");
            win.minSize = new Vector2(420, 270);
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Business Tycoon — World State Setup (Phase 4)", EditorStyles.boldLabel);
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Creates the Rain Particle System and HUD label in the active scene\n" +
                "and auto-wires them into WorldStateController.\n\n" +
                "Re-running is safe — existing objects are reused.",
                MessageType.Info);
            GUILayout.Space(10);

            GUI.backgroundColor = _sky;
            if (GUILayout.Button("  🌧  Create Rain Particle System", GUILayout.Height(44)))
                CreateRainParticleSystem();

            GUILayout.Space(8);
            GUI.backgroundColor = _grn;
            if (GUILayout.Button("  🖥  Create World State HUD Label", GUILayout.Height(44)))
                CreateWorldStateHUD();

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.90f, 0.55f, 0.10f);
            if (GUILayout.Button("  ⚡  Create Both + Auto-Wire", GUILayout.Height(44)))
            {
                CreateRainParticleSystem();
                CreateWorldStateHUD();
            }

            GUI.backgroundColor = Color.white;
        }

        // =====================================================================
        // Rain Particle System
        // =====================================================================

        private static void CreateRainParticleSystem()
        {
            const string NAME = "Rain Particle System";
            GameObject go = GameObject.Find(NAME);
            ParticleSystem ps = go != null ? go.GetComponent<ParticleSystem>() : null;

            if (ps == null)
            {
                go = new GameObject(NAME);
                go.transform.position = new Vector3(0f, 10f, 0f);
                ps = go.AddComponent<ParticleSystem>();
                Undo.RegisterCreatedObjectUndo(go, "Create Rain Particle System");
            }

            ConfigureRainParticles(ps);
            Wire(rain: ps);

            EditorUtility.SetDirty(ps.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[WorldStateSetup] Rain Particle System created/updated.");
            Selection.activeGameObject = ps.gameObject;
        }

        private static void ConfigureRainParticles(ParticleSystem ps)
        {
            // Main
            var m = ps.main;
            m.playOnAwake   = false;
            m.loop          = true;
            m.duration      = 1f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
            m.startSpeed    = new ParticleSystem.MinMaxCurve(14f, 18f);
            m.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.07f);
            m.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0.72f, 0.83f, 1f, 0.45f),
                new Color(0.80f, 0.90f, 1f, 0.70f));
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 2000;
            m.gravityModifier = 0.12f;

            // Emission
            var em = ps.emission;
            em.enabled      = true;
            em.rateOverTime = 160f;

            // Shape
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale     = new Vector3(20f, 0.1f, 20f);

            // Velocity over lifetime — slight constant sideways wind drift.
            // The single-value constructor configures every axis as Constant,
            // which Unity requires for all three velocity axes.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = new ParticleSystem.MinMaxCurve(-0.6f);
            vel.y       = new ParticleSystem.MinMaxCurve(0f);
            vel.z       = new ParticleSystem.MinMaxCurve(0f);

            // Renderer
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode         = ParticleSystemRenderMode.Stretch;
            r.velocityScale      = 0.07f;
            r.cameraVelocityScale = 0f;
            r.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows     = false;

            // Material
            if (r.sharedMaterial == null || r.sharedMaterial.name.Contains("Default"))
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader != null)
                {
                    System.IO.Directory.CreateDirectory("Assets/Materials");
                    string mp  = "Assets/Materials/Rain Particle Material.mat";
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (mat == null)
                    {
                        mat = new Material(shader) { name = "Rain Particle Material" };
                        AssetDatabase.CreateAsset(mat, mp);
                    }
                    r.sharedMaterial = mat;
                }
            }
        }

        // =====================================================================
        // World State HUD Label
        // =====================================================================

        private void CreateWorldStateHUD()
        {
            const string CANVAS = "World State Canvas";
            const string PANEL  = "World State Panel";
            const string LABEL  = "World State Label";

            // Canvas
            GameObject canvasGO = GameObject.Find(CANVAS);
            if (canvasGO == null)
            {
                canvasGO = new GameObject(CANVAS);
                var cv = canvasGO.AddComponent<Canvas>();
                cv.renderMode   = RenderMode.ScreenSpaceOverlay;
                cv.sortingOrder = 10;
                var sc = canvasGO.AddComponent<CanvasScaler>();
                sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution  = new Vector2(1920f, 1080f);
                sc.matchWidthOrHeight   = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create World State Canvas");
            }

            // EventSystem guard
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // Background panel (top-right anchored)
            GameObject panelGO = canvasGO.transform.Find(PANEL)?.gameObject;
            if (panelGO == null)
            {
                panelGO = new GameObject(PANEL, typeof(RectTransform), typeof(Image));
                panelGO.transform.SetParent(canvasGO.transform, false);
                Undo.RegisterCreatedObjectUndo(panelGO, "Create World State Panel");
            }

            var pr = panelGO.GetComponent<RectTransform>();
            pr.anchorMin        = Vector2.one;
            pr.anchorMax        = Vector2.one;
            pr.pivot            = Vector2.one;
            pr.anchoredPosition = new Vector2(-24f, -24f);
            pr.sizeDelta        = new Vector2(220f, 48f);

            var pi = panelGO.GetComponent<Image>();
            pi.color         = _dark;
            pi.raycastTarget = false;
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (rounded != null) { pi.sprite = rounded; pi.type = Image.Type.Sliced; }

            // TMP Label
            GameObject labelGO = panelGO.transform.Find(LABEL)?.gameObject;
            if (labelGO == null)
            {
                labelGO = new GameObject(LABEL, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGO.transform.SetParent(panelGO.transform, false);
                Undo.RegisterCreatedObjectUndo(labelGO, "Create World State Label");
            }

            var lr = labelGO.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(12f, 4f);
            lr.offsetMax = new Vector2(-12f, -4f);

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text          = "DAY | CLEAR";
            tmp.fontSize      = 18f;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            Wire(label: tmp);

            EditorUtility.SetDirty(canvasGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[WorldStateSetup] World State HUD created/updated.");
            Selection.activeGameObject = labelGO;
        }

        // =====================================================================
        // Auto-Wire into WorldStateController
        // =====================================================================

        private static void Wire(
            ParticleSystem  rain  = null,
            TextMeshProUGUI label = null)
        {
            WorldStateController ctrl =
                Object.FindFirstObjectByType<WorldStateController>();

            if (ctrl == null)
            {
                CustomerSpawner sp = Object.FindFirstObjectByType<CustomerSpawner>();
                if (sp != null)
                {
                    ctrl = Undo.AddComponent<WorldStateController>(sp.gameObject);
                    Debug.Log("[WorldStateSetup] WorldStateController added to CustomerSpawner.");
                }
                else
                {
                    Debug.LogWarning(
                        "[WorldStateSetup] No WorldStateController or CustomerSpawner found. " +
                        "Add WorldStateController to a scene object manually, then re-run.");
                    return;
                }
            }

            var so = new SerializedObject(ctrl);
            if (rain  != null)
            {
                var p = so.FindProperty("rainParticles");
                if (p != null) p.objectReferenceValue = rain;
            }
            if (label != null)
            {
                var p = so.FindProperty("worldStatusText");
                if (p != null) p.objectReferenceValue = label;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
            Debug.Log("[WorldStateSetup] References wired into WorldStateController.");
        }
    }
}
