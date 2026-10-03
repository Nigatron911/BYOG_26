using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project.Audio;
using Project.CameraControl;
using Project.Core;
using Project.Level3;
using Project.Player;

namespace Project.Editor
{
    public static class Level3SceneBuilder
    {
        [MenuItem("Tools/Build Level 3 (Blank Canvas)", priority = 30)]
        public static void BuildScene()
        {
            // 1. Ensure all sprites exist
            AssetGenerator.GenerateAllSprites();

            // 2. Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Level_03_BlankCanvas";

            // Load Sprites & Materials
            var platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/platform_tile.png");
            var spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/spike_tile.png");
            var beaconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/checkpoint_beacon.png");
            var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/finish_trophy.png");
            var sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/particle_spark.png");
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/reality_player.png");

            // Level 3 specific sprites
            var paperScrapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/paper_scrap.png");
            var woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/wood_block.png");
            var rubberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/rubber_pad.png");
            var anvilSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/anvil_block.png");
            var plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/pressure_plate.png");
            var gateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/heavy_gate.png");
            var canvasFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/canvas_frame.png");

            var particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/ParticleUnlit.mat");
            var spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Sprite2DUnlit.mat");

            Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0;

            // --- Root Hierarchies ---
            var envRoot = new GameObject("Environment====");
            var scrapsRoot = new GameObject("PaperScraps====");
            var interactiveRoot = new GameObject("Interactive====");
            var checkpointsRoot = new GameObject("Checkpoints====");
            var coreRoot = new GameObject("Core====");
            var drawingSystemRoot = new GameObject("DrawingSystem====");
            var uiRoot = new GameObject("UI====");

            // --- Lighting ---
            var lightGo = new GameObject("Global Light 2D");
            lightGo.transform.SetParent(envRoot.transform);
            var light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.90f, 0.95f, 1.0f);
            light2D.intensity = 1.0f;

            // --- Camera ---
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(coreRoot.transform);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(2.0f, 2.0f, -10f);

            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var smoothCam = cameraGo.AddComponent<SmoothCamera2D>();
            var soCam = new SerializedObject(smoothCam);
            soCam.FindProperty("minBounds").vector2Value = new Vector2(-2f, -1f);
            soCam.FindProperty("maxBounds").vector2Value = new Vector2(140f, 20f);
            soCam.FindProperty("offset").vector3Value = new Vector3(2.5f, 1.5f, -10f);
            soCam.ApplyModifiedPropertiesWithoutUndo();

            // --- Audio & Respawn Managers ---
            var managersGo = new GameObject("GameManager");
            managersGo.transform.SetParent(coreRoot.transform);
            managersGo.AddComponent<ProceduralAudio>();
            var respawnMgr = managersGo.AddComponent<RespawnManager>();

            // --- Backdrop ---
            CreateBackdrop(envRoot.transform, spriteMat);

            // --- Platforms ---
            var platformsRoot = new GameObject("Platforms");
            platformsRoot.transform.SetParent(envRoot.transform);

            // Area 1: Tutorial
            CreatePlatform(platformsRoot.transform, "TutorialFloor", new Vector2(5f, 0f), new Vector2(16f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "LeftBoundary", new Vector2(-3.5f, 5.5f), new Vector2(1f, 12f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "TutorialCeiling", new Vector2(5f, 7.5f), new Vector2(16f, 1f), platformSprite, groundLayer);

            // Test Gap 1 (X = 13.0 to 17.0, width 4.0)

            // Area 2: Chasm approach & Chasm
            CreatePlatform(platformsRoot.transform, "Area2_Approach", new Vector2(20f, 0f), new Vector2(6f, 1f), platformSprite, groundLayer);
            // Large Chasm 2 (X = 23.0 to 28.5, width 5.5)
            CreatePlatform(platformsRoot.transform, "Area2_Landing", new Vector2(33f, 0f), new Vector2(9f, 1f), platformSprite, groundLayer);

            // Area 3: High Cliff Approach & Raised Platform
            CreatePlatform(platformsRoot.transform, "Area3_Floor", new Vector2(36f, 0f), new Vector2(5f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "Area3_CliffWall", new Vector2(38.5f, 3.0f), new Vector2(1f, 6f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "Area3_RaisedPlatform", new Vector2(44f, 6.0f), new Vector2(10f, 1f), platformSprite, groundLayer);

            // Area 4: Heavy Gate Section (Y = 6.0)
            CreatePlatform(platformsRoot.transform, "Area4_GateFloor", new Vector2(56f, 6.0f), new Vector2(14f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "Area4_PostGateFloor", new Vector2(65f, 6.0f), new Vector2(6f, 1f), platformSprite, groundLayer);

            // Area 5: Danger / Scavenge (Ramp down to Y = 0.0)
            CreatePlatform(platformsRoot.transform, "Area5_StartFloor", new Vector2(70f, 0f), new Vector2(6f, 1f), platformSprite, groundLayer);
            // Spike pit A base
            CreatePlatform(platformsRoot.transform, "SpikePitA_Base", new Vector2(74.5f, -0.6f), new Vector2(3f, 0.4f), platformSprite, groundLayer);
            CreateSpikeHazard(envRoot.transform, "SpikeHazardA", new Vector2(74.5f, 0.25f), new Vector2(3f, 0.8f), spikeSprite);
            // Center island
            CreatePlatform(platformsRoot.transform, "Area5_Island", new Vector2(78.5f, 0f), new Vector2(4f, 1f), platformSprite, groundLayer);
            // Spike pit B base
            CreatePlatform(platformsRoot.transform, "SpikePitB_Base", new Vector2(82.5f, -0.6f), new Vector2(3f, 0.4f), platformSprite, groundLayer);
            CreateSpikeHazard(envRoot.transform, "SpikeHazardB", new Vector2(82.5f, 0.25f), new Vector2(3f, 0.8f), spikeSprite);
            // Landing
            CreatePlatform(platformsRoot.transform, "Area5_EndFloor", new Vector2(88f, 0f), new Vector2(7f, 1f), platformSprite, groundLayer);

            // Area 6: Final Challenge
            // Stage A: Gap (X = 91.5 to 96.5, width 5.0)
            CreatePlatform(platformsRoot.transform, "Area6_StageB_Floor", new Vector2(99f, 0f), new Vector2(5f, 1f), platformSprite, groundLayer);
            // Stage B: Cliff Wall at X = 101.5, Height 5.5
            CreatePlatform(platformsRoot.transform, "Area6_CliffWall", new Vector2(101.5f, 2.75f), new Vector2(1f, 5.5f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "Area6_HighLedge", new Vector2(107f, 5.5f), new Vector2(10f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "Area6_PostGateLedge", new Vector2(113.5f, 5.5f), new Vector2(5f, 1f), platformSprite, groundLayer);

            // Area 7: Finish Courtyard (Drop down to Y = 0.0)
            CreatePlatform(platformsRoot.transform, "FinishCourtyard", new Vector2(124f, 0f), new Vector2(20f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "RightBoundary", new Vector2(134.5f, 5.5f), new Vector2(1f, 12f), platformSprite, groundLayer);

            // DeathZone below all chasms
            var kzGo = new GameObject("DeathZone");
            kzGo.transform.SetParent(envRoot.transform);
            kzGo.transform.position = new Vector3(70f, -8f, 0f);
            var kzCol = kzGo.AddComponent<BoxCollider2D>();
            kzCol.size = new Vector2(180f, 4f);
            kzCol.isTrigger = true;
            kzGo.AddComponent<KillZone>();

            // --- Heavy Gates & Pressure Plates ---
            // Area 4 Gate & Plate
            var gate1 = CreateGate(interactiveRoot.transform, "Area4_SecurityGate", new Vector2(59.0f, 9.0f), new Vector2(1.5f, 5.0f), gateSprite);
            var plate1 = CreatePressurePlate(interactiveRoot.transform, "Area4_PressurePlate", new Vector2(55.0f, 6.7f), new Vector2(2.0f, 0.4f), plateSprite, gate1);

            // Area 6 Final Gate & Plate
            var gate2 = CreateGate(interactiveRoot.transform, "Area6_FinalGate", new Vector2(110.0f, 8.5f), new Vector2(1.5f, 5.0f), gateSprite);
            var plate2 = CreatePressurePlate(interactiveRoot.transform, "Area6_FinalPlate", new Vector2(106.0f, 6.2f), new Vector2(2.0f, 0.4f), plateSprite, gate2);

            // --- Collectible Paper Scraps (Total 7) ---
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_1_Tutorial", new Vector2(6.0f, 1.5f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_2_Chasm", new Vector2(19.5f, 1.5f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_3_Cliff", new Vector2(34.0f, 1.5f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_4_Gate", new Vector2(51.0f, 7.5f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_5_Island", new Vector2(78.5f, 2.0f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_6_Danger", new Vector2(87.0f, 1.5f), paperScrapSprite, particleMat, sparkSprite);
            CreatePaperScrap(scrapsRoot.transform, "PaperScrap_7_Gauntlet", new Vector2(99.0f, 1.5f), paperScrapSprite, particleMat, sparkSprite);

            // --- Checkpoints ---
            CreateCheckpoint(checkpointsRoot.transform, 0, new Vector2(2.0f, 1.3f), beaconSprite, particleMat, sparkSprite);
            CreateCheckpoint(checkpointsRoot.transform, 1, new Vector2(31.0f, 1.3f), beaconSprite, particleMat, sparkSprite);
            CreateCheckpoint(checkpointsRoot.transform, 2, new Vector2(42.0f, 7.3f), beaconSprite, particleMat, sparkSprite);
            CreateCheckpoint(checkpointsRoot.transform, 3, new Vector2(63.0f, 7.3f), beaconSprite, particleMat, sparkSprite);
            CreateCheckpoint(checkpointsRoot.transform, 4, new Vector2(88.0f, 1.3f), beaconSprite, particleMat, sparkSprite);
            CreateCheckpoint(checkpointsRoot.transform, 5, new Vector2(113.0f, 6.8f), beaconSprite, particleMat, sparkSprite);

            // --- Finish Trophy Shrine ---
            CreateFinishShrine(envRoot.transform, new Vector2(122.0f, 1.3f), trophySprite, particleMat, sparkSprite);

            // --- In-World Guidance Signboards ---
            CreateSignboard(envRoot.transform, new Vector2(4.0f, 0.5f), defaultFont,
                "<color=#00E5FF><b>LEVEL 3 — BLANK CANVAS</b></color>\n" +
                "<color=#FFFFFF>[WASD] Move   •   [SHIFT] Sprint</color>\n" +
                "<color=#FDE047>Walk over Paper Scraps to Collect Drawing Resource!</color>");

            CreateSignboard(envRoot.transform, new Vector2(11.0f, 0.5f), defaultFont,
                "<color=#4ADE80><b>DRAWING TUTORIAL</b></color>\n" +
                "Press <color=#FBBF24><b>[TAB]</b></color> or <color=#FBBF24><b>[C]</b></color> to Open Drawing Canvas\n" +
                "Select <color=#D4A373><b>[1] WOOD</b></color> • Draw a line • Press <color=#00FFCC><b>[ENTER]</b></color> to Create Bridge!");

            CreateSignboard(envRoot.transform, new Vector2(18.5f, 0.5f), defaultFont,
                "<color=#F87171><b>LARGE CHASM AHEAD</b></color>\n" +
                "Collect Paper Scrap • Draw a Wood Bridge\n" +
                "<color=#FBBF24><b>EVADE:</b> Creations fade after 8s — Cross quickly!</color>");

            CreateSignboard(envRoot.transform, new Vector2(33.0f, 0.5f), defaultFont,
                "<color=#00D2FF><b>TALL CLIFF AHEAD</b></color>\n" +
                "Select <color=#00D2FF><b>[2] RUBBER</b></color> • Draw a bouncy pad\n" +
                "Jump on it to bounce high up to the platform!");

            CreateSignboard(envRoot.transform, new Vector2(49.0f, 6.5f), defaultFont,
                "<color=#94A3B8><b>HEAVY SECURITY GATE</b></color>\n" +
                "Switch requires massive weight (Player is too light!)\n" +
                "Select <color=#94A3B8><b>[3] ANVIL</b></color> • Draw above switch to open gate!");

            CreateSignboard(envRoot.transform, new Vector2(71.0f, 0.5f), defaultFont,
                "<color=#F87171><b>DANGER: SCAVENGE UNDER PRESSURE</b></color>\n" +
                "Leap across spike pits to collect remaining Paper Scraps!");

            CreateSignboard(envRoot.transform, new Vector2(90.0f, 0.5f), defaultFont,
                "<color=#FBBF24><b>★ FINAL CREATION GAUNTLET</b></color>\n" +
                "Combine Wood Bridge $\\to$ Rubber Bounce $\\to$ Anvil Switch!");

            // --- Objective Triggers ---
            CreateObjectiveTrigger(envRoot.transform, "DRAW A WOOD BRIDGE", new Vector2(11.0f, 1.5f), new Vector2(2f, 4f));
            CreateObjectiveTrigger(envRoot.transform, "DRAW A BOUNCY PLATFORM", new Vector2(33.0f, 1.5f), new Vector2(2f, 4f));
            CreateObjectiveTrigger(envRoot.transform, "DRAW SOMETHING HEAVY", new Vector2(49.0f, 7.5f), new Vector2(2f, 4f));
            CreateObjectiveTrigger(envRoot.transform, "SCAVENGE PAPER SCRAPS", new Vector2(71.0f, 1.5f), new Vector2(2f, 4f));
            CreateObjectiveTrigger(envRoot.transform, "CONQUER THE FINAL GAUNTLET", new Vector2(90.0f, 1.5f), new Vector2(2f, 4f));
            CreateObjectiveTrigger(envRoot.transform, "REACH THE FINISH SHRINE", new Vector2(115.0f, 1.5f), new Vector2(2f, 4f));

            // --- Drawing System Controllers ---
            var resourceCtrl = drawingSystemRoot.AddComponent<PaperResourceController>();
            var soRes = new SerializedObject(resourceCtrl);
            soRes.FindProperty("startingScraps").intValue = 0;
            soRes.FindProperty("totalScrapsInLevel").intValue = 6;
            soRes.ApplyModifiedPropertiesWithoutUndo();

            var spawner = drawingSystemRoot.AddComponent<DrawingObjectSpawner>();
            var soSp = new SerializedObject(spawner);
            soSp.FindProperty("woodSprite").objectReferenceValue = woodSprite;
            soSp.FindProperty("rubberSprite").objectReferenceValue = rubberSprite;
            soSp.FindProperty("anvilSprite").objectReferenceValue = anvilSprite;
            soSp.FindProperty("particleMaterial").objectReferenceValue = particleMat;
            soSp.FindProperty("sparkSprite").objectReferenceValue = sparkSprite;
            soSp.FindProperty("spawnForwardOffset").floatValue = 2.6f;
            soSp.FindProperty("spawnHeightOffset").floatValue = 0.8f;
            soSp.ApplyModifiedPropertiesWithoutUndo();

            // --- Player ---
            var playerGo = CreatePlayer(coreRoot.transform, new Vector2(2.0f, 1.3f), playerSprite, particleMat, groundLayer);
            smoothCam.SetTarget(playerGo.transform);

            // --- UI Canvas ---
            CreateUI(uiRoot.transform, cam, defaultFont, canvasFrameSprite, resourceCtrl, respawnMgr);

            // Save Scene
            string scenePath = "Assets/Level_03_BlankCanvas.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            string folder = "Assets/_Project/Scenes";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            AssetDatabase.CopyAsset(scenePath, folder + "/Level_03_BlankCanvas.unity");

            // Update Build Settings with all 3 scenes
            var scenes = new EditorBuildSettingsScene[] {
                new EditorBuildSettingsScene("Assets/Main_Scene.unity", true),
                new EditorBuildSettingsScene("Assets/Level_02_RealityRewrite.unity", true),
                new EditorBuildSettingsScene("Assets/Level_03_BlankCanvas.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();

            Debug.Log("[Level3SceneBuilder] Scene Level_03_BlankCanvas created and registered in build settings!");
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, int layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.color = new Color(0.18f, 0.22f, 0.32f, 1f);
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            return go;
        }

        private static void CreateSpikeHazard(Transform parent, string name, Vector2 pos, Vector2 size, Sprite spikeSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spikeSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            go.AddComponent<KillZone>();
        }

        private static HeavyGate CreateGate(Transform parent, string name, Vector2 pos, Vector2 size, Sprite gateSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gateSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var gate = go.AddComponent<HeavyGate>();
            return gate;
        }

        private static PressurePlate CreatePressurePlate(Transform parent, string name, Vector2 pos, Vector2 size, Sprite plateSprite, HeavyGate gate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var plateTop = new GameObject("PlateTop");
            plateTop.transform.SetParent(go.transform);
            plateTop.transform.localPosition = Vector3.zero;

            var sr = plateTop.AddComponent<SpriteRenderer>();
            sr.sprite = plateSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 1;

            // Trigger on plate top
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x, size.y + 0.4f);
            col.offset = new Vector2(0f, 0.2f);
            col.isTrigger = true;

            var plate = go.AddComponent<PressurePlate>();
            var so = new SerializedObject(plate);
            so.FindProperty("plateVisual").objectReferenceValue = plateTop.transform;
            so.FindProperty("plateRenderer").objectReferenceValue = sr;
            so.FindProperty("targetGate").objectReferenceValue = gate;
            so.ApplyModifiedPropertiesWithoutUndo();

            return plate;
        }

        private static PaperScrap CreatePaperScrap(Transform parent, string name, Vector2 pos, Sprite scrapSprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = scrapSprite;
            sr.sortingOrder = 4;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var ps = CreateParticleHelper(go.transform, "CollectParticles", particleMat, 15, 0.4f, 3.5f, 0.25f, new Color(0.2f, 0.9f, 1f));

            var scrap = go.AddComponent<PaperScrap>();
            var so = new SerializedObject(scrap);
            so.FindProperty("collectParticles").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            return scrap;
        }

        private static Checkpoint CreateCheckpoint(Transform parent, int index, Vector2 pos, Sprite beaconSprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject($"Checkpoint_{index}");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 2.0f);
            col.isTrigger = true;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = beaconSprite;
            sr.sortingOrder = 2;

            var ps = CreateParticleHelper(go.transform, "ActiveParticles", particleMat, 15, 0.6f, 3f, 0.3f, new Color(0.1f, 1.0f, 0.6f, 1f));

            var cp = go.AddComponent<Checkpoint>();
            var so = new SerializedObject(cp);
            so.FindProperty("checkpointIndex").intValue = index;
            so.FindProperty("spawnPoint").objectReferenceValue = go.transform;
            so.FindProperty("beaconRenderer").objectReferenceValue = sr;
            so.FindProperty("activeParticles").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cp;
        }

        private static GameObject CreateFinishShrine(Transform parent, Vector2 pos, Sprite trophySprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject("FinishShrine");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = trophySprite;
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2.5f);
            col.isTrigger = true;

            var psGo = new GameObject("VictoryConfetti");
            psGo.transform.SetParent(go.transform);
            psGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var ps = psGo.AddComponent<ParticleSystem>();
            var psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (particleMat != null) psRenderer.material = particleMat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 3.0f;
            main.startLifetime = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSize = 0.25f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.0f, 1.0f, 0.85f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0.5f), new GradientColorKey(new Color(0.9f, 0.2f, 0.9f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var emission = ps.emission;
            emission.rateOverTime = 35;

            var finish = go.AddComponent<FinishTrigger>();
            var so = new SerializedObject(finish);
            so.FindProperty("confettiParticles").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreatePlayer(Transform parent, Vector2 pos, Sprite playerSprite, Material particleMat, int groundLayer)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.tag = "Player";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.gravityScale = 2.2f;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 1.0f);

            var gfx = new GameObject("Visual");
            gfx.transform.SetParent(go.transform);
            gfx.transform.localPosition = Vector3.zero;
            var sr = gfx.AddComponent<SpriteRenderer>();
            sr.sprite = playerSprite;
            sr.sortingOrder = 10;

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(go.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.48f, 0f);

            var ctrl = go.AddComponent<PlayerMaterialController>();
            var soCtrl = new SerializedObject(ctrl);
            soCtrl.FindProperty("paperSprite").objectReferenceValue = playerSprite;
            soCtrl.FindProperty("stoneSprite").objectReferenceValue = playerSprite;
            soCtrl.FindProperty("rubberSprite").objectReferenceValue = playerSprite;
            soCtrl.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soCtrl.FindProperty("rb").objectReferenceValue = rb;
            soCtrl.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
            soCtrl.FindProperty("groundLayer").intValue = 1 << groundLayer;
            soCtrl.FindProperty("moveSpeed").floatValue = 8.0f;
            soCtrl.FindProperty("firstJumpVelocity").floatValue = 10.5f;
            soCtrl.FindProperty("enableMaterialTransform").boolValue = false;
            soCtrl.FindProperty("enableSprint").boolValue = true;
            soCtrl.FindProperty("sprintMultiplier").floatValue = 1.45f;
            soCtrl.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static void CreateSignboard(Transform parent, Vector2 pos, Font font, string text)
        {
            var go = new GameObject("Signboard");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            for (int i = -1; i <= 1; i += 2)
            {
                var postGo = new GameObject($"Post_{i}");
                postGo.transform.SetParent(go.transform);
                postGo.transform.localPosition = new Vector3(i * 1.4f, 0.7f, 0f);
                var srPost = postGo.AddComponent<SpriteRenderer>();
                var texPost = new Texture2D(1, 1);
                texPost.SetPixel(0, 0, new Color(0.28f, 0.2f, 0.12f, 1f));
                texPost.Apply();
                srPost.sprite = Sprite.Create(texPost, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                postGo.transform.localScale = new Vector3(0.14f, 1.8f, 1f);
                srPost.sortingOrder = 1;
            }

            var canvasGo = new GameObject("SignCanvas");
            canvasGo.transform.SetParent(go.transform);
            canvasGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            canvasGo.transform.localScale = new Vector3(0.012f, 0.012f, 1f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            var panelGo = CreateUIPanel(canvasGo.transform, "SignPanel", Vector2.zero, new Vector2(440, 120), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));
            CreateUIText(panelGo.transform, "SignText", text, 15, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(420, 110), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private static void CreateObjectiveTrigger(Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var go = new GameObject($"ObjectiveTrigger_{text.Replace(" ", "_")}");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var trig = go.AddComponent<Level3ObjectiveTrigger>();
            var so = new SerializedObject(trig);
            so.FindProperty("objectiveMessage").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateBackdrop(Transform parent, Material spriteMat)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(70f, 6f, 5f);

            var tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool grid = (x == 0 || y == 0);
                    Color col = grid ? new Color(0.09f, 0.14f, 0.22f, 0.6f) : new Color(0.04f, 0.07f, 0.12f, 0.95f);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(220f, 40f);
            sr.sortingOrder = -10;
        }

        private static ParticleSystem CreateParticleHelper(Transform parent, string name, Material mat, int rate, float life, float speed, float size, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (mat != null) psr.material = mat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = col;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)rate) });

            return ps;
        }

        private static void CreateUI(Transform parent, Camera cam, Font font, Sprite frameSprite, PaperResourceController resourceCtrl, RespawnManager respawnMgr)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(parent);
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // --- Top-Left HUD Card ---
            var hudCard = CreateUIPanel(canvasGo.transform, "HUDCard", new Vector2(25, -25), new Vector2(380, 210), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Color(0.06f, 0.09f, 0.16f, 0.92f));

            var titleText = CreateUIText(hudCard.transform, "LevelTitle", "LEVEL 3 — BLANK CANVAS", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20, -25), new Vector2(340, 30), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var paperText = CreateUIText(hudCard.transform, "PaperText", "PAPER SCRAPS: <color=#00E5FF><b>0 / 6</b></color>", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20, -65), new Vector2(340, 28), font, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var matText = CreateUIText(hudCard.transform, "MaterialText", "MATERIAL: <color=#D4A373><b>WOOD</b></color>", 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(20, -100), new Vector2(340, 28), font, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var drawStatusText = CreateUIText(hudCard.transform, "DrawStatusText", "DRAWING: <color=#4ADE80>READY [TAB / C]</color>", 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(20, -135), new Vector2(340, 28), font, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var stabilityText = CreateUIText(hudCard.transform, "StabilityText", "", 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20, -170), new Vector2(340, 28), font, new Color(0.0f, 0.9f, 1f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // --- Top-Center Objective Card ---
            var objCard = CreateUIPanel(canvasGo.transform, "ObjectiveCard", new Vector2(0, -25), new Vector2(660, 85), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.06f, 0.09f, 0.16f, 0.92f));
            var objText = CreateUIText(objCard.transform, "ObjectiveText", "OBJECTIVE:\n<color=#FDE047>COLLECT PAPER SCRAPS</color>", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(640, 75), font, new Color(0.99f, 0.88f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // --- Center Notification Toast ---
            var notifCard = CreateUIPanel(canvasGo.transform, "NotificationCard", new Vector2(0, 140), new Vector2(560, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));
            var notifText = CreateUIText(notifCard.transform, "NotificationText", "CHECKPOINT REACHED", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(540, 50), font, new Color(0.2f, 1.0f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            notifCard.SetActive(false);

            // --- Victory Panel ---
            var victoryPanel = CreateUIPanel(canvasGo.transform, "VictoryPanel", Vector2.zero, new Vector2(640, 340), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.98f));
            var vicTitle = CreateUIText(victoryPanel.transform, "VictoryTitle", "★ BLANK CANVAS COMPLETE ★", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 85), new Vector2(600, 50), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var vicSub = CreateUIText(victoryPanel.transform, "VictorySubtitle", "YOUR CREATION SURVIVED\n\n<size=20><color=#4ADE80>LEVEL 3 COMPLETE</color></size>\n\n<size=17><color=#94A3B8>Press [ENTER] to Continue  •  [R] to Restart</color></size>", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, -25), new Vector2(580, 140), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            victoryPanel.SetActive(false);

            // --- Bottom Controls Bar ---
            var footerCard = CreateUIPanel(canvasGo.transform, "FooterCard", new Vector2(0, 20), new Vector2(920, 42), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Color(0.06f, 0.09f, 0.16f, 0.85f));
            CreateUIText(footerCard.transform, "FooterText", "<b>[WASD]</b> Move   •   <b>[SHIFT]</b> Sprint   •   <b>[SPACE]</b> Jump   •   <b>[TAB / C]</b> Open Canvas   •   <b>[1/2/3]</b> Material   •   <b>[R]</b> Restart", 15, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900, 36), font, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // ==========================================
            // --- FULLSCREEN DRAWING CANVAS MODAL UI ---
            // ==========================================
            var modalRoot = CreateUIPanel(canvasGo.transform, "DrawingCanvasModal", Vector2.zero, new Vector2(1920, 1080), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Color(0.02f, 0.04f, 0.08f, 0.85f));

            var boardCard = CreateUIPanel(modalRoot.transform, "BoardCard", Vector2.zero, new Vector2(760, 560), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.08f, 0.12f, 0.20f, 0.98f));

            // Board Header
            CreateUIText(boardCard.transform, "HeaderTitle", "╔══════════════════════════════════════╗\n║          <b>BLANK CANVAS — DRAW SOLUTION</b>          ║\n╚══════════════════════════════════════╝", 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 240), new Vector2(700, 50), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // RawImage Drawing Surface
            var drawSurfaceGo = new GameObject("DrawingSurface");
            drawSurfaceGo.transform.SetParent(boardCard.transform, false);
            var rtDraw = drawSurfaceGo.AddComponent<RectTransform>();
            rtDraw.anchoredPosition = new Vector2(0, 45);
            rtDraw.sizeDelta = new Vector2(512, 300);
            var rawImg = drawSurfaceGo.AddComponent<RawImage>();
            rawImg.color = Color.white;

            // Border around drawing surface
            var borderGo = new GameObject("SurfaceBorder");
            borderGo.transform.SetParent(boardCard.transform, false);
            var rtBorder = borderGo.AddComponent<RectTransform>();
            rtBorder.anchoredPosition = new Vector2(0, 45);
            rtBorder.sizeDelta = new Vector2(518, 306);
            var imgBorder = borderGo.AddComponent<Image>();
            imgBorder.color = new Color(0.0f, 0.8f, 1.0f, 0.6f);
            borderGo.transform.SetAsFirstSibling();

            // Material Info & Resource Count Text
            var matInfoText = CreateUIText(boardCard.transform, "MaterialInfoText", "MATERIAL: <color=#D4A373><b>WOOD (Solid Platform / Bridge)</b></color>", 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(-250, -125), new Vector2(400, 30), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var paperCountText = CreateUIText(boardCard.transform, "PaperScrapCountText", "PAPER SCRAPS: <b>0 / 6</b>", 16, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(250, -125), new Vector2(200, 30), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Material Selection Buttons Row
            var btnWoodGo = CreateButton(boardCard.transform, "BtnWood", "[1] BROWN — WOOD", new Vector2(-180, -165), new Vector2(170, 38), font, new Color(0.38f, 0.25f, 0.15f, 1f));
            var btnRubberGo = CreateButton(boardCard.transform, "BtnRubber", "[2] BLUE — RUBBER", new Vector2(0, -165), new Vector2(170, 38), font, new Color(0.0f, 0.45f, 0.85f, 1f));
            var btnAnvilGo = CreateButton(boardCard.transform, "BtnAnvil", "[3] GREY — ANVIL", new Vector2(180, -165), new Vector2(170, 38), font, new Color(0.35f, 0.38f, 0.45f, 1f));

            // Action Buttons Row: Create, Clear, Cancel
            var btnCreateGo = CreateButton(boardCard.transform, "BtnCreate", "★ [ENTER] CREATE OBJECT", new Vector2(-150, -215), new Vector2(220, 44), font, new Color(0.0f, 0.65f, 0.45f, 1f));
            var btnClearGo = CreateButton(boardCard.transform, "BtnClear", "CLEAR", new Vector2(20, -215), new Vector2(100, 44), font, new Color(0.25f, 0.28f, 0.35f, 1f));
            var btnCancelGo = CreateButton(boardCard.transform, "BtnCancel", "[ESC] CANCEL", new Vector2(150, -215), new Vector2(140, 44), font, new Color(0.65f, 0.22f, 0.22f, 1f));

            var validationNoticeText = CreateUIText(boardCard.transform, "ValidationNotice", "[HOLD LMB TO DRAW]  •  [ENTER] CREATE  •  [ESC] CANCEL", 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0, -255), new Vector2(680, 26), font, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Attach DrawingCanvasController
            var drawCtrl = canvasGo.AddComponent<DrawingCanvasController>();
            var soDraw = new SerializedObject(drawCtrl);
            soDraw.FindProperty("canvasRootPanel").objectReferenceValue = modalRoot;
            soDraw.FindProperty("drawingSurface").objectReferenceValue = rawImg;
            soDraw.FindProperty("drawingRectTransform").objectReferenceValue = rtDraw;
            soDraw.FindProperty("materialInfoText").objectReferenceValue = matInfoText;
            soDraw.FindProperty("paperScrapCountText").objectReferenceValue = paperCountText;
            soDraw.FindProperty("validationNoticeText").objectReferenceValue = validationNoticeText;
            soDraw.FindProperty("btnWood").objectReferenceValue = btnWoodGo.GetComponent<Button>();
            soDraw.FindProperty("btnRubber").objectReferenceValue = btnRubberGo.GetComponent<Button>();
            soDraw.FindProperty("btnAnvil").objectReferenceValue = btnAnvilGo.GetComponent<Button>();
            soDraw.FindProperty("btnCreate").objectReferenceValue = btnCreateGo.GetComponent<Button>();
            soDraw.FindProperty("btnClear").objectReferenceValue = btnClearGo.GetComponent<Button>();
            soDraw.FindProperty("btnCancel").objectReferenceValue = btnCancelGo.GetComponent<Button>();
            soDraw.ApplyModifiedPropertiesWithoutUndo();

            modalRoot.SetActive(false);

            // Attach Level3UIController
            var uiCtrl = canvasGo.AddComponent<Level3UIController>();
            var soUI = new SerializedObject(uiCtrl);
            soUI.FindProperty("resourceController").objectReferenceValue = resourceCtrl;
            soUI.FindProperty("canvasController").objectReferenceValue = drawCtrl;
            soUI.FindProperty("respawnManager").objectReferenceValue = respawnMgr;
            soUI.FindProperty("levelTitleText").objectReferenceValue = titleText;
            soUI.FindProperty("objectiveText").objectReferenceValue = objText;
            soUI.FindProperty("paperScrapsText").objectReferenceValue = paperText;
            soUI.FindProperty("materialText").objectReferenceValue = matText;
            soUI.FindProperty("drawingStatusText").objectReferenceValue = drawStatusText;
            soUI.FindProperty("creationStabilityText").objectReferenceValue = stabilityText;
            soUI.FindProperty("notificationCard").objectReferenceValue = notifCard;
            soUI.FindProperty("notificationText").objectReferenceValue = notifText;
            soUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            soUI.FindProperty("victoryTitleText").objectReferenceValue = vicTitle;
            soUI.FindProperty("victorySubtitleText").objectReferenceValue = vicSub;
            soUI.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = col;

            return go;
        }

        private static Text CreateUIText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor align, Vector2 pos, Vector2 size, Font font, Color col, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.color = col;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Font font, Color btnColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = btnColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = btnColor * 1.25f;
            colors.pressedColor = btnColor * 0.8f;
            btn.colors = colors;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var rtTxt = txtGo.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            var t = txtGo.AddComponent<Text>();
            t.text = label;
            t.font = font;
            t.fontSize = 14;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;

            return go;
        }
    }
}
