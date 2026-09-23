using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ushimitsu.Player;
using Ushimitsu.Interaction;
using Ushimitsu.Game;
using Ushimitsu.UI;
using Ushimitsu.Audio;

namespace Ushimitsu.EditorTools
{
    public static class SceneBuilder
    {
        static Font uiFont;
        static Color wallColor = new Color(0.11f, 0.086f, 0.075f);
        static Color floorColor = new Color(0.08f, 0.06f, 0.05f);
        static Color accentColor = new Color(0.42f, 0.19f, 0.16f);
        static Color propColor = new Color(0.18f, 0.14f, 0.12f);
        static Color creamText = new Color(0.93f, 0.89f, 0.83f);
        static Color goldText = new Color(0.65f, 0.51f, 0.25f);

        [MenuItem("Ushimitsu/Build Scene")]
        public static void BuildScene()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLightingAndFog();

            BuildEventSystem();

            GameObject player = BuildPlayer();
            HUDController hud = BuildUI();
            JumpscareController jumpscare = BuildJumpscareAudioRig(hud);

            BuildWashitsu();
            BuildCorridor();
            BuildGenkan();

            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.playerController = player.GetComponent<FirstPersonController>();
            gm.interactionController = player.GetComponentInChildren<InteractionController>();
            gm.hud = hud;
            gm.jumpscare = jumpscare;

            GameObject clockObj = new GameObject("UshimitsuClock");
            UshimitsuClock clock = clockObj.AddComponent<UshimitsuClock>();
            clock.hud = hud;
            gm.clock = clock;

            gm.interactionController.hud = hud;

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            string scenePath = scenesDir + "/Main.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            Debug.Log("[SceneBuilder] Scene built and saved at " + scenePath);
        }

        static void BuildEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        static void SetupLightingAndFog()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.045f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.015f, 0.012f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.045f;
        }

        static GameObject BuildPlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, -2.5f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            FirstPersonController fpc = player.AddComponent<FirstPersonController>();

            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            camObj.transform.localRotation = Quaternion.identity;
            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            fpc.cameraPivot = camObj.transform;

            InteractionController ic = camObj.AddComponent<InteractionController>();
            ic.range = 2.6f;

            return player;
        }

        static JumpscareController BuildJumpscareAudioRig(HUDController hud)
        {
            GameObject audioObj = new GameObject("AudioRig");

            GameObject bgmObj = new GameObject("BGMDrone");
            bgmObj.transform.SetParent(audioObj.transform);
            AudioSource bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.spatialBlend = 0f;
            bgmObj.AddComponent<BGMDrone>();

            GameObject stingObj = new GameObject("StingPlayer");
            stingObj.transform.SetParent(audioObj.transform);
            AudioSource stingSource = stingObj.AddComponent<AudioSource>();
            stingSource.spatialBlend = 0f;
            StingPlayer sting = stingObj.AddComponent<StingPlayer>();

            JumpscareController jumpscare = hud.GetComponent<JumpscareController>();
            jumpscare.sting = sting;
            return jumpscare;
        }

        // ---------------- Rooms ----------------

        static void BuildWashitsu()
        {
            GameObject room = new GameObject("Room_Washitsu");

            CreateFloor(room.transform, new Vector3(0f, 0f, 0f), new Vector3(8f, 0.2f, 8f));
            CreateWall(room.transform, "Wall_South", new Vector3(0f, 1.4f, -4f), new Vector3(8.3f, 2.8f, 0.3f));
            CreateWall(room.transform, "Wall_East", new Vector3(4f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 8.3f));
            CreateWall(room.transform, "Wall_West", new Vector3(-4f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 8.3f));
            // Doorway gap is exactly x:[-1,1] to line up flush with the corridor width below.
            CreateWall(room.transform, "Wall_North_Left", new Vector3(-2.5f, 1.4f, 4f), new Vector3(3f, 2.8f, 0.3f));
            CreateWall(room.transform, "Wall_North_Right", new Vector3(2.5f, 1.4f, 4f), new Vector3(3f, 2.8f, 0.3f));

            CreatePointLight(room.transform, new Vector3(0f, 2.4f, 0f), new Color(0.9f, 0.72f, 0.45f), 1.1f, 6f);

            GameObject scroll = CreateProp(room.transform, "床の間の掛け軸", new Vector3(0f, 1.7f, -3.85f), new Vector3(1.2f, 1.6f, 0.08f), accentColor);
            scroll.AddComponent<ScrollClue>();

            GameObject closet = CreateProp(room.transform, "押入れ", new Vector3(-3.8f, 1.2f, -1.5f), new Vector3(0.35f, 2.2f, 1.6f), propColor);
            closet.AddComponent<ClosetDoll>();

            GameObject altar = CreateProp(room.transform, "仏壇", new Vector3(3.8f, 1.0f, -1.5f), new Vector3(0.5f, 1.8f, 1.0f), propColor);
            altar.AddComponent<AltarDrawer>();

            GameObject tatami = CreateProp(room.transform, "畳", new Vector3(1.4f, 0.06f, 1.6f), new Vector3(1.6f, 0.08f, 1.6f), new Color(0.3f, 0.26f, 0.16f));
            tatami.AddComponent<TatamiFloor>();

            GameObject mirror = CreateProp(room.transform, "鏡台", new Vector3(-3.6f, 0.9f, 1.8f), new Vector3(0.6f, 1.2f, 0.5f), propColor);
            mirror.AddComponent<MirrorStand>();
        }

        static void BuildCorridor()
        {
            GameObject corridor = new GameObject("Corridor");

            // Walls/floor extend slightly past z:[4,10] into the neighbouring rooms so the
            // seams fully overlap the room walls instead of leaving a hairline gap.
            CreateFloor(corridor.transform, new Vector3(0f, 0f, 7f), new Vector3(2f, 0.2f, 6.3f));
            CreateWall(corridor.transform, "Wall_East", new Vector3(1f, 1.4f, 7f), new Vector3(0.3f, 2.8f, 6.3f));
            CreateWall(corridor.transform, "Wall_West", new Vector3(-1f, 1.4f, 7f), new Vector3(0.3f, 2.8f, 6.3f));

            CreatePointLight(corridor.transform, new Vector3(0f, 2.4f, 7f), new Color(0.55f, 0.42f, 0.3f), 0.6f, 5f);
        }

        static DoorLock BuildGenkan()
        {
            GameObject room = new GameObject("Room_Genkan");

            CreateFloor(room.transform, new Vector3(0f, 0f, 12.5f), new Vector3(5f, 0.2f, 5f));
            CreateWall(room.transform, "Wall_North", new Vector3(0f, 1.4f, 14.9f), new Vector3(5.3f, 2.8f, 0.3f));
            CreateWall(room.transform, "Wall_East", new Vector3(2.4f, 1.4f, 12.5f), new Vector3(0.3f, 2.8f, 5.3f));
            CreateWall(room.transform, "Wall_West", new Vector3(-2.4f, 1.4f, 12.5f), new Vector3(0.3f, 2.8f, 5.3f));
            // South wall of the entrance room is open only where the corridor connects (x:[-1,1]).
            CreateWall(room.transform, "Wall_South_Left", new Vector3(-1.75f, 1.4f, 10f), new Vector3(1.5f, 2.8f, 0.3f));
            CreateWall(room.transform, "Wall_South_Right", new Vector3(1.75f, 1.4f, 10f), new Vector3(1.5f, 2.8f, 0.3f));

            CreatePointLight(room.transform, new Vector3(0f, 2.4f, 12.5f), new Color(0.5f, 0.4f, 0.32f), 0.7f, 6f);

            GameObject door = CreateProp(room.transform, "出口の扉", new Vector3(0f, 1.3f, 14.7f), new Vector3(1.4f, 2.4f, 0.15f), accentColor);
            DoorLock doorLock = door.AddComponent<DoorLock>();
            return doorLock;
        }

        // ---------------- Primitive helpers ----------------

        static GameObject CreateFloor(Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Floor";
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.down * (scale.y * 0.5f);
            go.transform.localScale = scale;
            ApplyColor(go, floorColor);
            return go;
        }

        static GameObject CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            ApplyColor(go, wallColor);
            return go;
        }

        static GameObject CreateProp(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            ApplyColor(go, color);
            return go;
        }

        static void ApplyColor(GameObject go, Color color)
        {
            Renderer r = go.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            r.sharedMaterial = mat;
        }

        static void CreatePointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject("Lamp");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        // ---------------- UI ----------------

        static HUDController BuildUI()
        {
            GameObject canvasObj = new GameObject("HUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObj.AddComponent<GraphicRaycaster>();

            HUDController hud = canvasObj.AddComponent<HUDController>();

            hud.promptText = CreateText(canvasObj.transform, "PromptText", 26, creamText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(700f, 50f));

            hud.messagePanel = BuildMessagePanel(canvasObj.transform, out Text messageText);
            hud.messageText = messageText;

            hud.clockText = CreateText(canvasObj.transform, "ClockText", 22, creamText, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(380f, 50f));
            // Pivot must match the anchored corner, otherwise half the box hangs off screen.
            hud.clockText.rectTransform.pivot = new Vector2(1f, 1f);

            hud.inventoryText = CreateText(canvasObj.transform, "InventoryText", 18, creamText, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(500f, 40f));
            hud.inventoryText.rectTransform.pivot = new Vector2(0f, 0f);

            hud.dialPanel = BuildDialPanel(canvasObj.transform);
            BuildEndingPanel(canvasObj.transform, hud);
            BuildTitleScreen(canvasObj, hud);

            GameObject jumpscareObj = new GameObject("JumpscareLayer");
            jumpscareObj.transform.SetParent(canvasObj.transform, false);
            RectTransform jsRect = jumpscareObj.AddComponent<RectTransform>();
            StretchFull(jsRect);
            CanvasGroup group = jumpscareObj.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Image ghostImg = jumpscareObj.AddComponent<Image>();
            ghostImg.color = Color.white;
            ghostImg.preserveAspect = true;

            JumpscareController jumpscare = canvasObj.AddComponent<JumpscareController>();
            jumpscare.group = group;
            jumpscare.ghostImage = ghostImg;

            return hud;
        }

        // Center-screen message shown only while examining something. Sized by a
        // VerticalLayoutGroup + ContentSizeFitter so it always grows to fit the
        // full text instead of clipping it.
        static GameObject BuildMessagePanel(Transform canvasParent, out Text messageText)
        {
            GameObject panel = new GameObject("MessagePanel");
            panel.transform.SetParent(canvasParent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            rt.sizeDelta = new Vector2(760f, 0f);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.025f, 0.88f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 26, 26);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            // Without these the group only nudges the child's position and never
            // actually resizes it, so the Text rect keeps its default (near-zero) size.
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObj = new GameObject("MessageText");
            textObj.transform.SetParent(panel.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = 24;
            text.color = creamText;
            text.alignment = TextAnchor.MiddleCenter;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.25f;
            text.text = "";

            messageText = text;
            return panel;
        }

        static DialPanelController BuildDialPanel(Transform canvasParent)
        {
            // Everything inside is centre-anchored so the offsets below are simply
            // distances from the panel's middle (panel is 400 tall => +-200 usable).
            GameObject panel = CreatePanel(canvasParent, "DialPanel", new Color(0.08f, 0.06f, 0.05f, 0.94f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 400f));

            DialPanelController dial = panel.AddComponent<DialPanelController>();
            dial.panelRoot = panel;
            dial.shakeTarget = panel.GetComponent<RectTransform>();

            Text title = CreateText(panel.transform, "Title", 22, goldText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(480f, 50f));
            title.text = "ダイヤル錠";

            float[] xs = { -140f, 0f, 140f };
            for (int i = 0; i < 3; i++)
            {
                GameObject up = CreateButton(panel.transform, "Up" + i, "▲", 22,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 85f), new Vector2(64f, 44f));
                dial.upButtons[i] = up.GetComponent<Button>();

                Text digitText = CreateText(panel.transform, "Digit" + i, 34, creamText, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], 25f), new Vector2(64f, 50f));
                digitText.text = "0";
                dial.digitTexts[i] = digitText;

                GameObject down = CreateButton(panel.transform, "Down" + i, "▼", 22,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -35f), new Vector2(64f, 44f));
                dial.downButtons[i] = down.GetComponent<Button>();
            }

            dial.hintText = CreateText(panel.transform, "HintText", 18, goldText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(480f, 60f));

            GameObject tryBtn = CreateButton(panel.transform, "TryButton", "試す", 22,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f, -155f), new Vector2(150f, 50f));
            dial.tryButton = tryBtn.GetComponent<Button>();

            GameObject closeBtn = CreateButton(panel.transform, "CloseButton", "戻る", 22,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, -155f), new Vector2(150f, 50f));
            dial.closeButton = closeBtn.GetComponent<Button>();

            return dial;
        }

        static void BuildTitleScreen(GameObject canvasObj, HUDController hud)
        {
            GameObject panel = CreatePanel(canvasObj.transform, "TitlePanel", new Color(0.02f, 0.015f, 0.012f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            Text eyebrow = CreateText(panel.transform, "Eyebrow", 18, new Color(0.42f, 0.37f, 0.32f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(400f, 40f));
            eyebrow.text = "怪 異 譚";

            Text title = CreateText(panel.transform, "Title", 64, creamText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(900f, 100f));
            title.text = "丑 三 つ の 間";

            Text subtitle = CreateText(panel.transform, "Subtitle", 22, new Color(0.72f, 0.67f, 0.60f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 90f));
            subtitle.text = "目を開けると、そこは知らない和室だった。\n時計の針は、丑三つ時に近づいている。";

            Text controls = CreateText(panel.transform, "Controls", 18, new Color(0.55f, 0.50f, 0.44f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(760f, 70f));
            controls.text = "WASD：移動　　マウス：視点　　E：調べる / メッセージを閉じる";

            GameObject startBtn = CreateButton(panel.transform, "StartButton", "目を、開ける", 24,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(280f, 62f));

            Text hint = CreateText(panel.transform, "StartHint", 16, new Color(0.42f, 0.37f, 0.32f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(600f, 40f));
            hint.text = "クリック、または Enter キーで開始";

            TitleScreenController titleController = canvasObj.AddComponent<TitleScreenController>();
            titleController.titlePanel = panel;
            titleController.hud = hud;
            titleController.startButton = startBtn.GetComponent<Button>();
        }

        static void BuildEndingPanel(Transform canvasParent, HUDController hud)
        {
            GameObject panel = CreatePanel(canvasParent, "EndingPanel", new Color(0.02f, 0.015f, 0.012f, 0.96f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            hud.endingPanel = panel;

            hud.endingTitleText = CreateText(panel.transform, "EndingTitle", 44, new Color(0.76f, 0.27f, 0.23f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(800f, 80f));

            hud.endingBodyText = CreateText(panel.transform, "EndingBody", 22, creamText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(700f, 200f));

            GameObject restartBtn = CreateButton(panel.transform, "RestartButton", "もう一度、目を閉じる", 20,
                new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(320f, 56f));
            hud.restartButton = restartBtn.GetComponent<Button>();
        }

        static GameObject CreatePanel(Transform parent, string name, Color bg, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = bg;
            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text CreateText(Transform parent, string name, int fontSize, Color color, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, float lineSpacing = 1f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text text = go.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = lineSpacing;
            text.text = "";
            return text;
        }

        static GameObject CreateButton(Transform parent, string name, string label, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.12f, 0.1f, 1f);

            Button btn = go.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.16f, 0.12f, 0.1f);
            colors.highlightedColor = new Color(0.26f, 0.19f, 0.15f);
            colors.pressedColor = new Color(0.10f, 0.08f, 0.07f);
            btn.colors = colors;

            Text text = CreateText(go.transform, "Label", fontSize, goldText, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;

            return go;
        }
    }
}
