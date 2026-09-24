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
        static Color creamText = new Color(0.93f, 0.89f, 0.83f);
        static Color goldText = new Color(0.65f, 0.51f, 0.25f);

        static Material matPlaster, matWood, matWoodDark, matTatami, matTatamiEdge;
        static Material matShoji, matMirror, matGold, matPaper, matStone, matBulb;

        [MenuItem("Ushimitsu/Build Scene")]
        public static void BuildScene()
        {
            // The built-in LegacyRuntime font falls back to the OS's own fonts for
            // glyphs it doesn't ship, which is how Japanese renders fine in the Editor
            // and standalone builds. WebGL has no OS font access at all, so without an
            // embedded Japanese-capable font every non-ASCII character is simply blank.
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/MPLUS1p-Regular.ttf");
            if (uiFont == null)
            {
                UnityEngine.Debug.LogError("[SceneBuilder] Assets/Fonts/MPLUS1p-Regular.ttf not found; " +
                    "Japanese text will render blank in WebGL builds.");
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureAntiAliasing();
            CreateMaterials();
            SetupLightingAndFog();

            BuildEventSystem();

            GameObject player = BuildPlayer();
            HUDController hud = BuildUI();
            JumpscareController jumpscare = BuildJumpscareAudioRig(hud);

            hud.mobileControls = BuildMobileControls(hud.transform, player.GetComponent<FirstPersonController>(),
                player.GetComponentInChildren<InteractionController>());

            BuildOrientationGuard(hud.transform);

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

        // WebGL's default quality level ("High") ships with anti-aliasing off, which
        // on all the thin parallel beams/lattice slats in this scene reads as a fuzzy
        // dithered noise rather than clean edges. Force 4x MSAA on every level.
        static void EnsureAntiAliasing()
        {
            int originalLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, true);
                QualitySettings.antiAliasing = 4;
            }
            QualitySettings.SetQualityLevel(originalLevel, true);
        }

        static void BuildEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        static void SetupLightingAndFog()
        {
            // Cold, dim ambient so the warm bulbs and the moonlit shoji still carry the
            // scene, just with a bit more of a floor under them than before.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.075f, 0.09f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.023f, 0.028f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.04f;
        }

        static GameObject BuildPlayer()
        {
            GameObject player = new GameObject("Player");
            // Starts in the middle of the washitsu, facing the tokonoma alcove.
            player.transform.position = new Vector3(0f, 0.6f, 1.1f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.55f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.775f, 0f);
            cc.stepOffset = 0.25f;

            FirstPersonController fpc = player.AddComponent<FirstPersonController>();

            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
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

        // ---------------- Layout constants ----------------

        const float RoomHalf = 2.7f;      // washitsu is a 5.4m square
        const float WallH = 2.4f;
        const float WallT = 0.3f;
        const float DoorHalf = 1f;        // doorway / corridor half width
        const float CorridorZ0 = 2.7f;
        const float CorridorZ1 = 8.7f;
        const float GenkanZ0 = 8.7f;
        const float GenkanZ1 = 13.7f;
        const float GenkanHalfX = 2.5f;

        static float WallInner(float outer)
        {
            return outer - WallT * 0.5f;
        }

        // ---------------- Rooms ----------------

        static void BuildWashitsu()
        {
            GameObject room = new GameObject("Room_Washitsu");
            Transform t = room.transform;

            Box(t, "FloorSlab", new Vector3(0f, -0.1f, 0f),
                new Vector3(RoomHalf * 2f, 0.2f, RoomHalf * 2f), matWoodDark, true);
            BuildTatamiField(t);

            float wallY = WallH * 0.5f;
            Box(t, "Wall_South", new Vector3(0f, wallY, -RoomHalf),
                new Vector3(RoomHalf * 2f + WallT, WallH, WallT), matPlaster, true);
            Box(t, "Wall_East", new Vector3(RoomHalf, wallY, 0f),
                new Vector3(WallT, WallH, RoomHalf * 2f + WallT), matPlaster, true);
            Box(t, "Wall_West", new Vector3(-RoomHalf, wallY, 0f),
                new Vector3(WallT, WallH, RoomHalf * 2f + WallT), matPlaster, true);

            float segW = RoomHalf - DoorHalf;
            float segCx = DoorHalf + segW * 0.5f;
            Box(t, "Wall_North_Left", new Vector3(-segCx, wallY, RoomHalf),
                new Vector3(segW, WallH, WallT), matPlaster, true);
            Box(t, "Wall_North_Right", new Vector3(segCx, wallY, RoomHalf),
                new Vector3(segW, WallH, WallT), matPlaster, true);

            BuildCeiling(t, new Vector3(0f, WallH, 0f), new Vector2(RoomHalf * 2f, RoomHalf * 2f), 4);

            BuildTokonoma(t);
            BuildCloset(t);
            BuildAltar(t);
            BuildMirrorStand(t);

            // Moon-lit shoji on the east wall: the paper glows, the lattice reads black.
            BuildShoji(t, new Vector3(WallInner(RoomHalf) - 0.03f, 1.2f, 1.35f), new Vector2(2.2f, 1.9f), true);

            BuildHangingBulb(t, new Vector3(0f, 2.02f, -0.2f), 1.9f, 10f, 0.08f);
        }

        static void BuildCorridor()
        {
            GameObject corridor = new GameObject("Corridor");
            Transform t = corridor.transform;

            float zc = (CorridorZ0 + CorridorZ1) * 0.5f;
            float len = CorridorZ1 - CorridorZ0 + WallT;

            Box(t, "FloorSlab", new Vector3(0f, -0.1f, zc),
                new Vector3(DoorHalf * 2f, 0.2f, len), matWood, true);

            for (int i = 0; i < 14; i++)
            {
                float z = CorridorZ0 + 0.3f + i * 0.46f;
                if (z > CorridorZ1) break;
                Box(t, "Plank" + i, new Vector3(0f, 0.012f, z),
                    new Vector3(DoorHalf * 2f - 0.04f, 0.024f, 0.42f), matWoodDark, false);
            }

            float wallY = WallH * 0.5f;
            Box(t, "Wall_East", new Vector3(DoorHalf, wallY, zc),
                new Vector3(WallT, WallH, len), matPlaster, true);
            Box(t, "Wall_West", new Vector3(-DoorHalf, wallY, zc),
                new Vector3(WallT, WallH, len), matPlaster, true);

            BuildCeiling(t, new Vector3(0f, WallH, zc), new Vector2(DoorHalf * 2f, len), 4);
            BuildShoji(t, new Vector3(WallInner(DoorHalf) - 0.03f, 1.2f, zc - 0.9f), new Vector2(1.8f, 1.9f), true);
            BuildHangingBulb(t, new Vector3(0f, 2.02f, zc + 1.3f), 1.05f, 8f, 0.3f);
        }

        static DoorLock BuildGenkan()
        {
            GameObject room = new GameObject("Room_Genkan");
            Transform t = room.transform;

            float zc = (GenkanZ0 + GenkanZ1) * 0.5f;
            float depth = GenkanZ1 - GenkanZ0;

            Box(t, "FloorSlab", new Vector3(0f, -0.1f, zc),
                new Vector3(GenkanHalfX * 2f, 0.2f, depth), matWood, true);
            Box(t, "Doma", new Vector3(0f, 0.008f, GenkanZ1 - 1.1f),
                new Vector3(GenkanHalfX * 2f - 0.3f, 0.03f, 2f), matStone, false);
            Box(t, "Kamachi", new Vector3(0f, 0.06f, GenkanZ1 - 2.1f),
                new Vector3(GenkanHalfX * 2f - 0.3f, 0.11f, 0.12f), matWoodDark, false);

            float wallY = WallH * 0.5f;
            Box(t, "Wall_North", new Vector3(0f, wallY, GenkanZ1),
                new Vector3(GenkanHalfX * 2f + WallT, WallH, WallT), matPlaster, true);
            Box(t, "Wall_East", new Vector3(GenkanHalfX, wallY, zc),
                new Vector3(WallT, WallH, depth + WallT), matPlaster, true);
            Box(t, "Wall_West", new Vector3(-GenkanHalfX, wallY, zc),
                new Vector3(WallT, WallH, depth + WallT), matPlaster, true);

            float stubW = GenkanHalfX - DoorHalf;
            float stubCx = DoorHalf + stubW * 0.5f;
            Box(t, "Wall_South_Left", new Vector3(-stubCx, wallY, GenkanZ0),
                new Vector3(stubW, WallH, WallT), matPlaster, true);
            Box(t, "Wall_South_Right", new Vector3(stubCx, wallY, GenkanZ0),
                new Vector3(stubW, WallH, WallT), matPlaster, true);

            BuildCeiling(t, new Vector3(0f, WallH, zc), new Vector2(GenkanHalfX * 2f, depth), 3);
            BuildHangingBulb(t, new Vector3(0f, 2.02f, zc - 0.6f), 1.15f, 9f, 0.15f);

            GameObject door = new GameObject("出口の扉");
            door.transform.SetParent(t, false);
            float dz = WallInner(GenkanZ1) - 0.06f;

            Box(door.transform, "Panel_L", new Vector3(-0.44f, 1f, dz),
                new Vector3(0.86f, 1.94f, 0.05f), matShoji, true);
            Box(door.transform, "Panel_R", new Vector3(0.44f, 1f, dz - 0.06f),
                new Vector3(0.86f, 1.94f, 0.05f), matShoji, true);
            Box(door.transform, "Frame_Top", new Vector3(0f, 2.04f, dz - 0.03f),
                new Vector3(1.94f, 0.12f, 0.16f), matWood, false);
            Box(door.transform, "Frame_Bottom", new Vector3(0f, 0.03f, dz - 0.03f),
                new Vector3(1.94f, 0.1f, 0.16f), matWood, false);
            Box(door.transform, "Frame_L", new Vector3(-0.94f, 1f, dz - 0.03f),
                new Vector3(0.1f, 2.06f, 0.16f), matWood, false);
            Box(door.transform, "Frame_R", new Vector3(0.94f, 1f, dz - 0.03f),
                new Vector3(0.1f, 2.06f, 0.16f), matWood, false);

            for (int i = 1; i < 5; i++)
            {
                float y = 0.1f + 1.84f * i / 5f;
                Box(door.transform, "Slat" + i, new Vector3(0f, y, dz - 0.05f),
                    new Vector3(1.8f, 0.035f, 0.04f), matWoodDark, false);
            }

            Box(door.transform, "LockBox", new Vector3(1.2f, 1.05f, dz - 0.04f),
                new Vector3(0.24f, 0.32f, 0.12f), matWoodDark, true);
            Cyl(door.transform, "Dial", new Vector3(1.2f, 1.05f, dz - 0.13f),
                0.07f, 0.03f, matGold, false, new Vector3(90f, 0f, 0f));

            DoorLock doorLock = door.AddComponent<DoorLock>();

            GameObject moon = new GameObject("MoonBleed");
            moon.transform.SetParent(t, false);
            moon.transform.position = new Vector3(0f, 1.7f, GenkanZ1 - 0.5f);
            Light ml = moon.AddComponent<Light>();
            ml.type = LightType.Point;
            ml.color = new Color(0.52f, 0.66f, 0.95f);
            ml.intensity = 0.9f;
            ml.range = 5.5f;

            return doorLock;
        }

        // ---------------- Set dressing ----------------

        static void BuildTatamiField(Transform parent)
        {
            GameObject group = new GameObject("Tatami");
            group.transform.SetParent(parent, false);

            float[] xs = { -1.8f, 0f, 1.8f };
            float[] zs = { -2.25f, -1.35f, -0.45f, 0.45f, 1.35f, 2.25f };

            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    if (Mathf.Approximately(x, 0f) && Mathf.Approximately(z, 0.45f)) continue;
                    CreateTatamiMat(group.transform, new Vector3(x, 0.03f, z), false);
                }
            }

            // The one loose mat the player can lift.
            GameObject loose = new GameObject("畳");
            loose.transform.SetParent(parent, false);
            CreateTatamiMat(loose.transform, new Vector3(0f, 0.07f, 0.45f), true);
            loose.AddComponent<TatamiFloor>();

            // The visible mat is only 6cm tall, which is hard to land a look-ray on
            // once the camera sits at a real eye height. This invisible box gives the
            // interaction sweep a much taller target without changing how it looks.
            GameObject hitbox = new GameObject("InteractHitbox");
            hitbox.transform.SetParent(loose.transform, false);
            hitbox.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            BoxCollider bc = hitbox.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.74f, 0.4f, 0.84f);
        }

        static void CreateTatamiMat(Transform parent, Vector3 center, bool collider)
        {
            Box(parent, "Mat", center, new Vector3(1.74f, 0.06f, 0.84f), matTatami, collider);
            Box(parent, "Edge_N", center + new Vector3(0f, 0.002f, 0.42f),
                new Vector3(1.74f, 0.064f, 0.05f), matTatamiEdge, false);
            Box(parent, "Edge_S", center + new Vector3(0f, 0.002f, -0.42f),
                new Vector3(1.74f, 0.064f, 0.05f), matTatamiEdge, false);
        }

        static void BuildCeiling(Transform parent, Vector3 center, Vector2 size, int beamCount)
        {
            GameObject group = new GameObject("Ceiling");
            group.transform.SetParent(parent, false);

            // `center.y` is exactly the wall top. The panel's bottom face must sit
            // flush there (any higher and the sky shows through the gap above the
            // walls); the beams hang further down, with clear air between them and
            // the panel so the two don't z-fight on WebGL's shallower depth buffer.
            Box(group.transform, "Panel", new Vector3(center.x, center.y + 0.04f, center.z),
                new Vector3(size.x, 0.08f, size.y), matWoodDark, false);

            for (int i = 0; i < beamCount; i++)
            {
                float k = (i + 1f) / (beamCount + 1f);
                float z = center.z - size.y * 0.5f + size.y * k;
                Box(group.transform, "Beam" + i, new Vector3(center.x, center.y - 0.09f, z),
                    new Vector3(size.x, 0.09f, 0.1f), matWood, false);
            }
        }

        static void BuildTokonoma(Transform parent)
        {
            GameObject group = new GameObject("床の間");
            group.transform.SetParent(parent, false);

            float back = -WallInner(RoomHalf) + 0.02f;

            Box(group.transform, "BackPanel", new Vector3(-0.85f, 1.2f, back),
                new Vector3(1.9f, 2.2f, 0.04f), matPaper, false);
            Box(group.transform, "Platform", new Vector3(-0.85f, 0.09f, back + 0.24f),
                new Vector3(1.9f, 0.18f, 0.5f), matWood, true);
            Box(group.transform, "Lintel", new Vector3(-0.85f, 2.08f, back + 0.24f),
                new Vector3(1.9f, 0.16f, 0.5f), matWood, false);
            Cyl(group.transform, "Tokobashira", new Vector3(0.14f, 1.2f, back + 0.3f),
                0.06f, 2.4f, matWood, false);

            GameObject scroll = new GameObject("掛け軸");
            scroll.transform.SetParent(group.transform, false);
            Box(scroll.transform, "Paper", new Vector3(-0.85f, 1.35f, back + 0.04f),
                new Vector3(0.52f, 1.2f, 0.02f), matPaper, true);
            Rod(scroll.transform, "Rod_Top", new Vector3(-0.85f, 1.97f, back + 0.04f), 0.62f, 0.022f);
            Rod(scroll.transform, "Rod_Bottom", new Vector3(-0.85f, 0.73f, back + 0.04f), 0.62f, 0.022f);
            scroll.AddComponent<ScrollClue>();
        }

        static void BuildCloset(Transform parent)
        {
            GameObject closet = new GameObject("押入れ");
            closet.transform.SetParent(parent, false);

            float x = -WallInner(RoomHalf) + 0.04f;

            Box(closet.transform, "Frame", new Vector3(x - 0.03f, 1f, 0.35f),
                new Vector3(0.1f, 2.06f, 1.98f), matWood, false);
            Box(closet.transform, "Door_L", new Vector3(x + 0.02f, 1f, -0.11f),
                new Vector3(0.05f, 1.86f, 0.9f), matPaper, true);
            Box(closet.transform, "Door_R", new Vector3(x + 0.06f, 1f, 0.81f),
                new Vector3(0.05f, 1.86f, 0.9f), matPaper, true);
            Box(closet.transform, "Rail", new Vector3(x + 0.04f, 0.05f, 0.35f),
                new Vector3(0.14f, 0.1f, 1.9f), matWood, false);

            closet.AddComponent<ClosetDoll>();
        }

        static void BuildAltar(Transform parent)
        {
            GameObject altar = new GameObject("仏壇");
            altar.transform.SetParent(parent, false);

            float x = WallInner(RoomHalf) - 0.3f;

            Box(altar.transform, "Body", new Vector3(x, 0.72f, -1f),
                new Vector3(0.6f, 1.44f, 1f), matWoodDark, true);
            Box(altar.transform, "Interior", new Vector3(x - 0.24f, 0.95f, -1f),
                new Vector3(0.12f, 0.8f, 0.76f), matGold, false);
            Box(altar.transform, "Drawer", new Vector3(x - 0.31f, 0.26f, -1f),
                new Vector3(0.06f, 0.26f, 0.84f), matWood, true);
            Box(altar.transform, "Knob", new Vector3(x - 0.35f, 0.26f, -1f),
                new Vector3(0.05f, 0.05f, 0.05f), matGold, false);
            Cyl(altar.transform, "Candle", new Vector3(x - 0.14f, 1.45f, -1.22f),
                0.025f, 0.16f, matPaper, false);

            GameObject candle = new GameObject("CandleLight");
            candle.transform.SetParent(altar.transform, false);
            candle.transform.position = new Vector3(x - 0.16f, 1.52f, -1f);
            Light cl = candle.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(1f, 0.6f, 0.26f);
            cl.intensity = 0.7f;
            cl.range = 2.8f;

            LightFlicker cf = candle.AddComponent<LightFlicker>();
            cf.baseIntensity = 0.7f;
            cf.flickerAmount = 0.4f;
            cf.speed = 9f;
            cf.blackoutChance = 0f;

            altar.AddComponent<AltarDrawer>();
        }

        static void BuildMirrorStand(Transform parent)
        {
            GameObject mirror = new GameObject("鏡台");
            mirror.transform.SetParent(parent, false);

            float x = -WallInner(RoomHalf) + 0.24f;

            Box(mirror.transform, "Stand", new Vector3(x, 0.3f, 2f),
                new Vector3(0.44f, 0.6f, 0.82f), matWood, true);
            Box(mirror.transform, "Post_L", new Vector3(x, 0.74f, 1.68f),
                new Vector3(0.06f, 0.34f, 0.06f), matWoodDark, false);
            Box(mirror.transform, "Post_R", new Vector3(x, 0.74f, 2.32f),
                new Vector3(0.06f, 0.34f, 0.06f), matWoodDark, false);
            Box(mirror.transform, "Frame", new Vector3(x, 1.18f, 2f),
                new Vector3(0.08f, 0.94f, 0.74f), matWoodDark, true);
            Box(mirror.transform, "Glass", new Vector3(x + 0.05f, 1.18f, 2f),
                new Vector3(0.02f, 0.82f, 0.62f), matMirror, false);

            mirror.AddComponent<MirrorStand>();
        }

        // Paper panel plus a dark lattice in front of it. The paper is emissive so it
        // reads as moonlight from outside without needing light to pass through walls.
        static void BuildShoji(Transform parent, Vector3 center, Vector2 size, bool facingX)
        {
            GameObject group = new GameObject("障子");
            group.transform.SetParent(parent, false);

            float w = size.x;
            float h = size.y;
            // The whole wooden frame (rails, stiles, slats) sits one consistent step
            // in front of the paper. Without this every piece shared the paper's exact
            // depth and z-fought with it as a flickering moire over the whole screen.
            float inward = -0.06f;
            Vector3 offset = facingX ? new Vector3(inward, 0f, 0f) : new Vector3(0f, 0f, inward);
            // The vertical slats sit a hair further in than the horizontal ones so the
            // two sets don't share a depth at every crossing point in the lattice.
            Vector3 offsetV = facingX ? new Vector3(inward - 0.015f, 0f, 0f) : new Vector3(0f, 0f, inward - 0.015f);

            Box(group.transform, "Paper", center,
                facingX ? new Vector3(0.04f, h, w) : new Vector3(w, h, 0.04f), matShoji, false);

            Vector3 railSize = facingX ? new Vector3(0.07f, 0.1f, w + 0.1f) : new Vector3(w + 0.1f, 0.1f, 0.07f);
            Box(group.transform, "Rail_Top", center + new Vector3(0f, h * 0.5f, 0f) + offset, railSize, matWood, false);
            Box(group.transform, "Rail_Bottom", center - new Vector3(0f, h * 0.5f, 0f) + offset, railSize, matWood, false);

            Vector3 stileSize = facingX ? new Vector3(0.07f, h, 0.1f) : new Vector3(0.1f, h, 0.07f);
            Vector3 edge = facingX ? new Vector3(0f, 0f, w * 0.5f) : new Vector3(w * 0.5f, 0f, 0f);
            Box(group.transform, "Stile_A", center + edge + offset, stileSize, matWood, false);
            Box(group.transform, "Stile_B", center - edge + offset, stileSize, matWood, false);

            int rows = Mathf.Max(2, Mathf.RoundToInt(h / 0.3f));
            for (int i = 1; i < rows; i++)
            {
                float y = center.y - h * 0.5f + h * i / rows;
                Vector3 s = facingX ? new Vector3(0.04f, 0.035f, w) : new Vector3(w, 0.035f, 0.04f);
                Box(group.transform, "Slat_H" + i, new Vector3(center.x, y, center.z) + offset, s, matWoodDark, false);
            }

            int cols = Mathf.Max(2, Mathf.RoundToInt(w / 0.34f));
            for (int i = 1; i < cols; i++)
            {
                float o = -w * 0.5f + w * i / cols;
                Vector3 pos = facingX
                    ? new Vector3(center.x, center.y, center.z + o)
                    : new Vector3(center.x + o, center.y, center.z);
                Vector3 s = facingX ? new Vector3(0.04f, h, 0.035f) : new Vector3(0.035f, h, 0.04f);
                Box(group.transform, "Slat_V" + i, pos + offsetV, s, matWoodDark, false);
            }
        }

        static void BuildHangingBulb(Transform parent, Vector3 pos, float intensity, float range, float blackoutChance)
        {
            GameObject group = new GameObject("裸電球");
            group.transform.SetParent(parent, false);

            float cordLen = Mathf.Max(0.1f, WallH - pos.y);
            Cyl(group.transform, "Cord", new Vector3(pos.x, pos.y + cordLen * 0.5f, pos.z),
                0.008f, cordLen, matWoodDark, false);
            Box(group.transform, "Bulb", pos, new Vector3(0.11f, 0.14f, 0.11f), matBulb, false);

            GameObject lightObj = new GameObject("Light");
            lightObj.transform.SetParent(group.transform, false);
            lightObj.transform.position = pos - new Vector3(0f, 0.06f, 0f);

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.81f, 0.6f);
            l.intensity = intensity;
            l.range = range;
            // Point light shadow maps are low-resolution cubemaps by default and read
            // as a blocky, pixelated black smear on nearby surfaces. This scene doesn't
            // need dynamic shadows to sell the mood, so just turn them off outright.
            l.shadows = LightShadows.None;

            LightFlicker flicker = lightObj.AddComponent<LightFlicker>();
            flicker.baseIntensity = intensity;
            flicker.flickerAmount = 0.22f;
            flicker.speed = 5f;
            flicker.blackoutChance = blackoutChance;
        }

        // ---------------- Primitive helpers ----------------

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        static GameObject Cyl(Transform parent, string name, Vector3 pos, float radius, float height,
            Material mat, bool collider, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        static GameObject Rod(Transform parent, string name, Vector3 pos, float length, float radius)
        {
            return Cyl(parent, name, pos, radius, length, matWoodDark, false, new Vector3(0f, 0f, 90f));
        }

        // ---------------- Materials ----------------

        static void CreateMaterials()
        {
            matPlaster = MakeMat("M_Plaster", new Color(0.15f, 0.125f, 0.105f), 0.04f, 0f);
            matWood = MakeMat("M_Wood", new Color(0.105f, 0.075f, 0.055f), 0.2f, 0f);
            matWoodDark = MakeMat("M_WoodDark", new Color(0.055f, 0.04f, 0.03f), 0.15f, 0f);
            matTatami = MakeMat("M_Tatami", new Color(0.215f, 0.205f, 0.125f), 0.05f, 0f);
            matTatamiEdge = MakeMat("M_TatamiEdge", new Color(0.085f, 0.07f, 0.045f), 0.1f, 0f);
            matShoji = MakeMat("M_Shoji", new Color(0.55f, 0.6f, 0.68f), 0.05f, 0f,
                new Color(0.22f, 0.29f, 0.4f));
            matMirror = MakeMat("M_Mirror", new Color(0.05f, 0.06f, 0.07f), 0.95f, 0.85f);
            matGold = MakeMat("M_Gold", new Color(0.26f, 0.19f, 0.07f), 0.55f, 0.8f,
                new Color(0.05f, 0.035f, 0.01f));
            matPaper = MakeMat("M_Paper", new Color(0.38f, 0.36f, 0.32f), 0.05f, 0f);
            matStone = MakeMat("M_Stone", new Color(0.065f, 0.06f, 0.055f), 0.05f, 0f);
            matBulb = MakeMat("M_Bulb", new Color(0.9f, 0.82f, 0.68f), 0.9f, 0f,
                new Color(1f, 0.78f, 0.52f));
        }

        static Shader cachedStandardShader;

        static Shader FindStandardShader()
        {
            if (cachedStandardShader != null) return cachedStandardShader;

            // Try the usual name first, then fall back to shaders that are always
            // present regardless of render pipeline / stripping settings, so we get
            // a visibly-wrong-but-not-pink result instead of the error shader.
            string[] candidates = { "Standard", "Legacy Shaders/Diffuse", "Unlit/Color", "Sprites/Default" };
            foreach (string candidate in candidates)
            {
                Shader s = Shader.Find(candidate);
                if (s != null)
                {
                    if (candidate != "Standard")
                    {
                        Debug.LogWarning("[SceneBuilder] Shader 'Standard' not found; falling back to '" + candidate + "'.");
                    }
                    cachedStandardShader = s;
                    return s;
                }
            }

            Debug.LogError("[SceneBuilder] No usable shader found at all (checked: " + string.Join(", ", candidates) + "). Materials will render pink.");
            return null;
        }

        static Material MakeMat(string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            Material m = new Material(FindStandardShader());
            m.name = name;
            m.color = color;
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        // ---------------- UI ----------------

        // On-screen joystick + drag-to-look + interact button, shown only when
        // Input.touchSupported is true at runtime (see MobileControlsUI). Desktop
        // players never see this layer; PC controls are completely unchanged.
        // The left/right touch split only makes sense in landscape. This sits above
        // every other layer and blocks play with a clear prompt until the phone is
        // actually turned sideways.
        static void BuildOrientationGuard(Transform canvasParent)
        {
            GameObject panel = CreatePanel(canvasParent, "OrientationGuard", new Color(0.02f, 0.015f, 0.012f, 0.97f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            CreateText(panel.transform, "Message", 26, creamText, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 160f))
                .text = "スマホを横向きにしてください";

            OrientationGuard guard = canvasParent.gameObject.AddComponent<OrientationGuard>();
            guard.prompt = panel;
        }

        static MobileControlsUI BuildMobileControls(Transform canvasParent, FirstPersonController player, InteractionController interaction)
        {
            GameObject root = new GameObject("MobileControls");
            root.transform.SetParent(canvasParent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            StretchFull(rootRect);

            MobileControlsUI controls = root.AddComponent<MobileControlsUI>();
            controls.root = root;
            controls.player = player;
            controls.interaction = interaction;

            // Input itself is read straight from Input.touches in MobileControlsUI
            // (see its header comment for why), so nothing here needs to be a UI
            // raycast target for movement/look. The ring/knob are visuals only;
            // MobileControlsUI resizes them to the real stick radius and moves the
            // ring to wherever the thumb lands.
            GameObject ringObj = new GameObject("JoystickRing");
            ringObj.transform.SetParent(root.transform, false);
            RectTransform ringRect = ringObj.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0f, 0f);
            ringRect.anchorMax = new Vector2(0f, 0f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = new Vector2(150f, 150f);
            ringRect.sizeDelta = new Vector2(190f, 190f);
            Image ringImg = ringObj.AddComponent<Image>();
            ringImg.color = new Color(0.9f, 0.87f, 0.8f, 0.18f);
            ringImg.raycastTarget = false;

            GameObject knobObj = new GameObject("JoystickKnob");
            knobObj.transform.SetParent(ringObj.transform, false);
            RectTransform knobRect = knobObj.AddComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(80f, 80f);
            Image knobImg = knobObj.AddComponent<Image>();
            knobImg.color = new Color(0.9f, 0.87f, 0.8f, 0.4f);
            knobImg.raycastTarget = false;
            controls.joystickRing = ringRect;
            controls.joystickKnob = knobRect;

            // Interact button: bottom-right. This one still goes through the normal
            // UI Button/EventSystem click path (a single tap has no multi-touch
            // ambiguity to worry about) - Input.touches only needs to know its
            // screen-space rect so a look-drag starting on top of it is ignored.
            GameObject interactBtn = CreateButton(root.transform, "InteractButton", "調べる", 20,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-110f, 110f), new Vector2(150f, 150f));
            controls.interactButton = interactBtn.GetComponent<Button>();
            controls.interactButtonRect = interactBtn.GetComponent<RectTransform>();

            return controls;
        }

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
