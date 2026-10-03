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
using Project.Level2;
using Project.Player;

namespace Project.Editor
{
    public static class Level2SceneBuilder
    {
        [MenuItem("Tools/Build Level 2 (Reality Rewrite)", priority = 25)]
        public static void BuildScene()
        {
            // 1. Ensure all Level 2 sprites exist
            AssetGenerator.GenerateAllSprites();

            // 2. Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Level_02_RealityRewrite";

            // Load Sprites & Materials
            var platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/platform_tile.png");
            var spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/spike_tile.png");
            var springSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/spring_pad.png");
            var techWallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/tech_wall.png");
            var techFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/tech_floor.png");
            var reticleCrossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/reticle_cross.png");
            var reticleBracketSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/reticle_bracket.png");
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/reality_player.png");
            var beaconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/checkpoint_beacon.png");
            var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/finish_trophy.png");
            var sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/particle_spark.png");

            var particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/ParticleUnlit.mat");
            var spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Sprite2DUnlit.mat");

            Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Setup Ground Layer
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0;

            // --- Root Hierarchies ---
            var envRoot = new GameObject("Environment====");
            var targetsRoot = new GameObject("RealityRewriteTargets====");
            var checkpointsRoot = new GameObject("Checkpoints====");
            var coreRoot = new GameObject("Core====");
            var controllerRoot = new GameObject("Controller====");
            var uiRoot = new GameObject("UI====");

            // --- Lighting ---
            var lightGo = new GameObject("Global Light 2D");
            lightGo.transform.SetParent(envRoot.transform);
            var light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.92f, 0.95f, 1.0f);
            light2D.intensity = 1.0f;

            // --- Managers ---
            var managersGo = new GameObject("GameManager");
            managersGo.transform.SetParent(controllerRoot.transform);
            var audioComp = managersGo.AddComponent<ProceduralAudio>();
            var respawnMgr = managersGo.AddComponent<RespawnManager>();

            // --- Camera ---
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(coreRoot.transform);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(2.0f, 2.0f, -10f);

            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.14f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var smoothCam = cameraGo.AddComponent<SmoothCamera2D>();
            var soCam = new SerializedObject(smoothCam);
            soCam.FindProperty("minBounds").vector2Value = new Vector2(-2f, -1f);
            soCam.FindProperty("maxBounds").vector2Value = new Vector2(130f, 20f);
            soCam.FindProperty("offset").vector3Value = new Vector3(2.5f, 1.5f, -10f);
            soCam.ApplyModifiedPropertiesWithoutUndo();

            // --- Backdrop ---
            CreateBackdrop(envRoot.transform, spriteMat);

            // --- Environment Platforms ---
            var platformsRoot = new GameObject("Platforms");
            platformsRoot.transform.SetParent(envRoot.transform);

            // Area 1: Tutorial Room
            CreatePlatform(platformsRoot.transform, "TutorialFloor", new Vector2(7f, 0f), new Vector2(18f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "LeftBoundary", new Vector2(-2.5f, 5.5f), new Vector2(1f, 12f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "TutorialCeiling", new Vector2(7f, 7.5f), new Vector2(18f, 1f), platformSprite, groundLayer);

            // Area 2: Runway & Pit
            CreatePlatform(platformsRoot.transform, "RunwayFloor", new Vector2(20f, 0f), new Vector2(8f, 1f), platformSprite, groundLayer);
            // Pit floor (under spikes)
            CreatePlatform(platformsRoot.transform, "SpikePitBase", new Vector2(28f, -0.6f), new Vector2(8f, 0.4f), platformSprite, groundLayer);

            // Area 3: Raised Platform (landing from spike bounce)
            CreatePlatform(platformsRoot.transform, "RaisedPlatform", new Vector2(44f, 3.5f), new Vector2(24f, 1f), platformSprite, groundLayer);

            // Area 4: Intangible Floor Drop Platform & Safe Lower Route
            CreatePlatform(platformsRoot.transform, "UpperDropApproach", new Vector2(55f, 3.5f), new Vector2(6f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "UpperDropEndWall", new Vector2(63.5f, 7.5f), new Vector2(1f, 9f), platformSprite, groundLayer);
            // Safe Lower Route
            CreatePlatform(platformsRoot.transform, "LowerRouteFloor", new Vector2(65f, -1.0f), new Vector2(22f, 1f), platformSprite, groundLayer);

            // Area 5: Gauntlet
            CreatePlatform(platformsRoot.transform, "GauntletSpikePitBase", new Vector2(80f, -1.6f), new Vector2(8f, 0.4f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "GauntletHighLedge", new Vector2(90f, 3.5f), new Vector2(12f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "GauntletChuteWall", new Vector2(101.5f, 7.5f), new Vector2(1f, 9f), platformSprite, groundLayer);

            // Area 6: Finish Courtyard
            CreatePlatform(platformsRoot.transform, "FinishCourtyard", new Vector2(112f, 0f), new Vector2(22f, 1f), platformSprite, groundLayer);
            CreatePlatform(platformsRoot.transform, "RightBoundary", new Vector2(123.5f, 5.5f), new Vector2(1f, 12f), platformSprite, groundLayer);

            // KillZone below everything
            var kzGo = new GameObject("DeathZone");
            kzGo.transform.SetParent(envRoot.transform);
            kzGo.transform.position = new Vector3(60f, -8f, 0f);
            var kzCol = kzGo.AddComponent<BoxCollider2D>();
            kzCol.size = new Vector2(160f, 4f);
            kzCol.isTrigger = true;
            kzGo.AddComponent<KillZone>();

            // --- Reality Rewrite Targets ---
            // 1. Tutorial Target: Wall blocking tutorial exit (X = 13.5, Y = 3.5, size 1.5 x 6)
            var tutorialWall = CreateWallTarget(targetsRoot.transform, "TutorialWallTarget", new Vector2(13.5f, 3.5f), new Vector2(1.5f, 6.0f), techWallSprite, particleMat, sparkSprite);

            // 2. Area 2: Spikes to Bouncy Spring (X = 28.0, Y = 0.35, size 7.6 x 0.9)
            var spikeTarget1 = CreateSpikeTarget(targetsRoot.transform, "Area2_SpikesToSpring", new Vector2(28.0f, 0.35f), new Vector2(7.6f, 0.9f), spikeSprite, springSprite, particleMat, sparkSprite);

            // 3. Area 3: Solid Wall to Intangible Wall (X = 46.0, Y = 6.5f, size 1.6 x 5.0)
            var wallTarget1 = CreateWallTarget(targetsRoot.transform, "Area3_SolidWallTarget", new Vector2(46.0f, 6.5f), new Vector2(1.6f, 5.0f), techWallSprite, particleMat, sparkSprite);

            // 4. Area 4: Floor to Intangible Floor Drop (X = 59.0f, Y = 3.5f, size 4.0 x 1.0)
            var floorTarget1 = CreateFloorTarget(targetsRoot.transform, "Area4_FloorDropTarget", new Vector2(59.0f, 3.5f), new Vector2(4.0f, 1.0f), techFloorSprite, particleMat, sparkSprite);

            // 5. Area 5 Gauntlet Target A: Spikes (X = 80.0, Y = -0.65, size 7.6 x 0.9)
            var spikeTarget2 = CreateSpikeTarget(targetsRoot.transform, "Area5_GauntletSpikes", new Vector2(80.0f, -0.65f), new Vector2(7.6f, 0.9f), spikeSprite, springSprite, particleMat, sparkSprite);

            // 6. Area 5 Gauntlet Target B: Wall (X = 92.0, Y = 6.5f, size 1.6 x 5.0)
            var wallTarget2 = CreateWallTarget(targetsRoot.transform, "Area5_GauntletWall", new Vector2(92.0f, 6.5f), new Vector2(1.6f, 5.0f), techWallSprite, particleMat, sparkSprite);

            // 7. Area 5 Gauntlet Target C: Floor Chute (X = 98.0, Y = 3.5f, size 4.0 x 1.0)
            var floorTarget2 = CreateFloorTarget(targetsRoot.transform, "Area5_GauntletFloor", new Vector2(98.0f, 3.5f), new Vector2(4.0f, 1.0f), techFloorSprite, particleMat, sparkSprite);

            // --- Checkpoints ---
            var cp0 = CreateCheckpoint(checkpointsRoot.transform, 0, new Vector2(2.0f, 1.3f), beaconSprite, particleMat, sparkSprite);
            var cp1 = CreateCheckpoint(checkpointsRoot.transform, 1, new Vector2(35.0f, 4.8f), beaconSprite, particleMat, sparkSprite);
            var cp2 = CreateCheckpoint(checkpointsRoot.transform, 2, new Vector2(50.0f, 4.8f), beaconSprite, particleMat, sparkSprite);
            var cp3 = CreateCheckpoint(checkpointsRoot.transform, 3, new Vector2(66.0f, 0.3f), beaconSprite, particleMat, sparkSprite);
            var cp4 = CreateCheckpoint(checkpointsRoot.transform, 4, new Vector2(86.0f, 4.8f), beaconSprite, particleMat, sparkSprite);

            // --- Finish Trophy Shrine ---
            var finishGo = CreateFinishShrine(envRoot.transform, new Vector2(115.0f, 1.3f), trophySprite, particleMat, sparkSprite);

            // --- In-World Guidance Signboards ---
            CreateSignboard(envRoot.transform, new Vector2(4.5f, 0.5f), defaultFont,
                "<color=#00E5FF><b>LEVEL 2 — REALITY REWRITE</b></color>\n" +
                "<color=#FFFFFF>[WASD] Move   •   [SHIFT] Sprint</color>\n" +
                "<color=#FDE047>[MOUSE] Aim Reticle   •   [LMB] Rewrite Target (3s)</color>");

            CreateSignboard(envRoot.transform, new Vector2(21.0f, 0.5f), defaultFont,
                "<color=#F87171><b>DEADLY SPIKES AHEAD</b></color>\n" +
                "<color=#FFFFFF>Sprint + Jump into air</color>\n" +
                "<color=#00FFCC>Aim & [LMB] Rewrite Spikes into BOUNCY SPRING!</color>");

            CreateSignboard(envRoot.transform, new Vector2(41.0f, 4.0f), defaultFont,
                "<color=#FDE047><b>SOLID BARRIER</b></color>\n" +
                "<color=#FFFFFF>Aim at wall & press [LMB]</color>\n" +
                "<color=#00FFCC>Pass through during the 3-second intangible window!</color>");

            CreateSignboard(envRoot.transform, new Vector2(54.5f, 4.0f), defaultFont,
                "<color=#FDE047><b>DEAD END AHEAD</b></color>\n" +
                "<color=#FFFFFF>Stand on the floor grating ahead</color>\n" +
                "<color=#00FFCC>Aim & [LMB] Rewrite Floor to drop through safely!</color>");

            CreateSignboard(envRoot.transform, new Vector2(72.0f, -0.5f), defaultFont,
                "<color=#F43F5E><b>★ FINAL GAUNTLET ★</b></color>\n" +
                "<color=#FFFFFF>Bounce on Spikes  •  Phase through Wall  •  Drop Floor</color>\n" +
                "<color=#FDE047>Manage the 5-second cooldown!</color>");

            // --- Player Setup ---
            var playerGo = CreatePlayer(coreRoot.transform, new Vector2(2.0f, 1.2f), playerSprite, particleMat, sparkSprite, groundLayer, cam);
            var playerCtrl = playerGo.GetComponent<PlayerMaterialController>();
            var rewriteCtrl = playerGo.GetComponent<RealityRewriteController>();
            smoothCam.SetTarget(playerGo.transform);

            // Bind player to RespawnManager
            var soRespawn = new SerializedObject(respawnMgr);
            soRespawn.FindProperty("player").objectReferenceValue = playerCtrl;
            soRespawn.FindProperty("initialSpawnPoint").objectReferenceValue = cp0.transform;
            soRespawn.ApplyModifiedPropertiesWithoutUndo();

            // --- UI Canvas & HUD ---
            var uiController = BuildLevel2UI(uiRoot.transform, rewriteCtrl, respawnMgr, cam, defaultFont, reticleCrossSprite, reticleBracketSprite);

            // --- Objective Triggers ---
            CreateObjectiveTrigger(envRoot.transform, "LEARN TO REWRITE REALITY", new Vector2(2f, 1.5f), new Vector2(4f, 4f), uiController);
            CreateObjectiveTrigger(envRoot.transform, "REWRITE THE SPIKES", new Vector2(17f, 1.5f), new Vector2(4f, 4f), uiController);
            CreateObjectiveTrigger(envRoot.transform, "PASS THROUGH THE WALL", new Vector2(35f, 4.5f), new Vector2(4f, 4f), uiController);
            CreateObjectiveTrigger(envRoot.transform, "DROP THROUGH", new Vector2(50f, 4.5f), new Vector2(4f, 4f), uiController);
            CreateObjectiveTrigger(envRoot.transform, "SURVIVE THE FINAL GAUNTLET", new Vector2(70f, 0.5f), new Vector2(4f, 4f), uiController);
            CreateObjectiveTrigger(envRoot.transform, "REACH THE FINISH", new Vector2(104f, 1.5f), new Vector2(4f, 4f), uiController);

            // Save Scene
            string scenePath1 = "Assets/_Project/Scenes/Level_02_RealityRewrite.unity";
            string scenePath2 = "Assets/Level_02_RealityRewrite.unity";
            EditorSceneManager.SaveScene(scene, scenePath1);
            EditorSceneManager.SaveScene(scene, scenePath2, true);

            // Update Build Settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Main_Scene.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/game.unity", true),
                new EditorBuildSettingsScene(scenePath2, true),
                new EditorBuildSettingsScene(scenePath1, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("[Level2SceneBuilder] Level 2 (Reality Rewrite) successfully built and saved!");
        }

        private static void CreateBackdrop(Transform parent, Material spriteMat)
        {
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parent);
            backdrop.transform.position = new Vector3(60f, 10f, 5f);

            var sr = backdrop.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.12f, 1f)); // Deep dark sci-fi night
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.material = spriteMat;
            backdrop.transform.localScale = new Vector3(200f, 50f, 1f);
            sr.sortingOrder = -50;
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
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            return go;
        }

        private static GameObject CreateSpikeTarget(Transform parent, string name, Vector2 pos, Vector2 size, Sprite spikeSprite, Sprite springSprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            // Deadly collider (trigger)
            var deadlyCol = go.AddComponent<BoxCollider2D>();
            deadlyCol.size = size;
            deadlyCol.isTrigger = true;
            var killZone = go.AddComponent<KillZone>();

            // Safe spring collider (trigger)
            var springCol = go.AddComponent<BoxCollider2D>();
            springCol.size = new Vector2(size.x, size.y + 0.3f);
            springCol.offset = new Vector2(0f, 0.15f);
            springCol.isTrigger = true;
            springCol.enabled = false;

            // Spike Visual
            var spikeGfx = new GameObject("SpikeVisual");
            spikeGfx.transform.SetParent(go.transform);
            spikeGfx.transform.localPosition = Vector3.zero;
            var srSpike = spikeGfx.AddComponent<SpriteRenderer>();
            srSpike.sprite = spikeSprite;
            srSpike.drawMode = SpriteDrawMode.Tiled;
            srSpike.size = size;
            srSpike.sortingOrder = 1;

            // Spring Visual
            var springGfx = new GameObject("SpringVisual");
            springGfx.transform.SetParent(go.transform);
            springGfx.transform.localPosition = Vector3.zero;
            var srSpring = springGfx.AddComponent<SpriteRenderer>();
            srSpring.sprite = springSprite;
            srSpring.drawMode = SpriteDrawMode.Tiled;
            srSpring.size = size;
            srSpring.sortingOrder = 2;
            springGfx.SetActive(false);

            // Rewrite burst particles
            var rewritePS = CreateParticleHelper(go.transform, "RewriteVFX", particleMat, 20, 0.4f, 4.5f, 0.35f, new Color(0f, 1f, 0.85f));
            var bouncePS = CreateParticleHelper(go.transform, "BounceVFX", particleMat, 15, 0.35f, 5f, 0.3f, new Color(1f, 0.85f, 0.2f));

            // Outline highlight
            var outline = CreateOutlineHelper(go.transform, size);

            var target = go.AddComponent<SpikeRewriteTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("deadlyCollider").objectReferenceValue = deadlyCol;
            so.FindProperty("springCollider").objectReferenceValue = springCol;
            so.FindProperty("killZoneComponent").objectReferenceValue = killZone;
            so.FindProperty("spikeVisual").objectReferenceValue = spikeGfx;
            so.FindProperty("springVisual").objectReferenceValue = springGfx;
            so.FindProperty("rewriteParticles").objectReferenceValue = rewritePS;
            so.FindProperty("bounceParticles").objectReferenceValue = bouncePS;
            so.FindProperty("highlightOutline").objectReferenceValue = outline;
            so.FindProperty("mainRenderer").objectReferenceValue = srSpike;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateWallTarget(Transform parent, string name, Vector2 pos, Vector2 size, Sprite wallSprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = wallSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 3;

            var dissolvePS = CreateParticleHelper(go.transform, "DissolveVFX", particleMat, 16, 0.35f, 3.5f, 0.25f, new Color(0.2f, 0.85f, 1f));
            var outline = CreateOutlineHelper(go.transform, size);

            var target = go.AddComponent<WallRewriteTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("wallCollider").objectReferenceValue = col;
            so.FindProperty("dissolveParticles").objectReferenceValue = dissolvePS;
            so.FindProperty("mainRenderer").objectReferenceValue = sr;
            so.FindProperty("highlightOutline").objectReferenceValue = outline;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateFloorTarget(Transform parent, string name, Vector2 pos, Vector2 size, Sprite floorSprite, Material particleMat, Sprite sparkSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = floorSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 2;

            var dissolvePS = CreateParticleHelper(go.transform, "DissolveVFX", particleMat, 16, 0.35f, 3.5f, 0.25f, new Color(0.2f, 0.85f, 1f));
            var outline = CreateOutlineHelper(go.transform, size);

            var target = go.AddComponent<FloorRewriteTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("floorCollider").objectReferenceValue = col;
            so.FindProperty("dissolveParticles").objectReferenceValue = dissolvePS;
            so.FindProperty("mainRenderer").objectReferenceValue = sr;
            so.FindProperty("highlightOutline").objectReferenceValue = outline;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateOutlineHelper(Transform parent, Vector2 size)
        {
            var outlineGo = new GameObject("HighlightOutline");
            outlineGo.transform.SetParent(parent);
            outlineGo.transform.localPosition = Vector3.zero;

            var lr = outlineGo.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = 4;
            lr.useWorldSpace = false;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.0f, 1.0f, 0.85f, 0.95f);
            lr.endColor = new Color(0.0f, 1.0f, 0.85f, 0.95f);
            lr.sortingOrder = 15;

            float hw = size.x * 0.5f + 0.05f;
            float hh = size.y * 0.5f + 0.05f;
            lr.SetPosition(0, new Vector3(-hw, -hh, 0f));
            lr.SetPosition(1, new Vector3(hw, -hh, 0f));
            lr.SetPosition(2, new Vector3(hw, hh, 0f));
            lr.SetPosition(3, new Vector3(-hw, hh, 0f));

            outlineGo.SetActive(false);
            return outlineGo;
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
            main.gravityModifier = 0.8f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.cyan, 0.5f), new GradientColorKey(Color.magenta, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var finish = go.AddComponent<FinishTrigger>();
            var so = new SerializedObject(finish);
            so.FindProperty("confettiParticles").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreatePlayer(Transform parent, Vector2 pos, Sprite playerSprite, Material particleMat, Sprite sparkSprite, int groundLayer, Camera cam)
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

            var transformVFX = CreateParticleHelper(go.transform, "TransformVFX", particleMat, 18, 0.4f, 4f, 0.35f, new Color(0f, 1f, 0.85f));
            var shockwaveVFX = CreateParticleHelper(go.transform, "ShockwaveVFX", particleMat, 25, 0.45f, 6f, 0.4f, new Color(0f, 0.85f, 1f));

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

            var rewriteCtrl = go.AddComponent<RealityRewriteController>();
            var soRewrite = new SerializedObject(rewriteCtrl);
            soRewrite.FindProperty("mainCam").objectReferenceValue = cam;
            soRewrite.FindProperty("fireShockwaveParticles").objectReferenceValue = shockwaveVFX;
            soRewrite.FindProperty("rewriteDuration").floatValue = 3.0f;
            soRewrite.FindProperty("cooldownDuration").floatValue = 5.0f;
            soRewrite.FindProperty("aimRadius").floatValue = 4.2f;
            soRewrite.ApplyModifiedPropertiesWithoutUndo();

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

            var panelGo = CreateUIPanel(canvasGo.transform, "SignPanel", Vector2.zero, new Vector2(420, 120), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));
            CreateUIText(panelGo.transform, "SignText", text, 15, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(400, 110), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private static void CreateObjectiveTrigger(Transform parent, string text, Vector2 pos, Vector2 size, Level2UIController ui)
        {
            var go = new GameObject($"ObjectiveTrigger_{text.Substring(0, Mathf.Min(8, text.Length))}");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var trig = go.AddComponent<Level2ObjectiveTrigger>();
            var so = new SerializedObject(trig);
            so.FindProperty("objectiveText").stringValue = text;
            so.FindProperty("uiController").objectReferenceValue = ui;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Level2UIController BuildLevel2UI(Transform parent, RealityRewriteController rewriteCtrl, RespawnManager respawnMgr, Camera cam, Font font, Sprite crossSprite, Sprite bracketSprite)
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

            // --- Top-Left Header Card (Level 2 info & Cooldown / Rewrite status) ---
            var hudCard = CreateUIPanel(canvasGo.transform, "HUDCard", new Vector2(25, -25), new Vector2(360, 200), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Color(0.06f, 0.09f, 0.16f, 0.92f));

            var titleText = CreateUIText(hudCard.transform, "LevelTitle", "LEVEL 2 — REALITY REWRITE", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20, -25), new Vector2(320, 30), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var cdText = CreateUIText(hudCard.transform, "CooldownText", "REWRITE: <color=#4ADE80>READY [LMB]</color>", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(20, -65), new Vector2(320, 28), font, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var activeText = CreateUIText(hudCard.transform, "ActiveText", "REALITY REWRITE: <color=#94A3B8>IDLE</color>", 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(20, -100), new Vector2(320, 28), font, new Color(0.9f, 0.95f, 1f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            CreateUIText(hudCard.transform, "Hint", "<color=#94A3B8>Aim at obstacles & click [LMB] to rewrite for 3s</color>", 13, FontStyle.Italic, TextAnchor.MiddleLeft, new Vector2(20, -150), new Vector2(320, 35), font, Color.gray, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // --- Top-Center Objective Card ---
            var objCard = CreateUIPanel(canvasGo.transform, "ObjectiveCard", new Vector2(0, -25), new Vector2(660, 85), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.06f, 0.09f, 0.16f, 0.92f));
            var objText = CreateUIText(objCard.transform, "ObjectiveText", "OBJECTIVE:\n<color=#FDE047>LEARN TO REWRITE REALITY</color>", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(640, 75), font, new Color(0.99f, 0.88f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // --- Center Reticle UI ---
            var reticleContainerGo = new GameObject("ReticleContainer");
            reticleContainerGo.transform.SetParent(canvasGo.transform, false);
            var rtReticle = reticleContainerGo.AddComponent<RectTransform>();
            rtReticle.anchorMin = new Vector2(0.5f, 0.5f);
            rtReticle.anchorMax = new Vector2(0.5f, 0.5f);
            rtReticle.pivot = new Vector2(0.5f, 0.5f);
            rtReticle.anchoredPosition = Vector2.zero;
            rtReticle.sizeDelta = new Vector2(100, 100);

            // Crosshair
            var crossGo = new GameObject("Crosshair");
            crossGo.transform.SetParent(reticleContainerGo.transform, false);
            var rtCross = crossGo.AddComponent<RectTransform>();
            rtCross.sizeDelta = new Vector2(44, 44);
            var imgCross = crossGo.AddComponent<Image>();
            imgCross.sprite = crossSprite;
            imgCross.color = new Color(0.85f, 0.35f, 0.35f, 0.7f);

            // Left bracket
            var bLeftGo = new GameObject("BracketLeft");
            bLeftGo.transform.SetParent(reticleContainerGo.transform, false);
            var rtBLeft = bLeftGo.AddComponent<RectTransform>();
            rtBLeft.anchoredPosition = new Vector2(-28, 0);
            rtBLeft.sizeDelta = new Vector2(24, 48);
            var imgBLeft = bLeftGo.AddComponent<Image>();
            imgBLeft.sprite = bracketSprite;
            imgBLeft.color = new Color(0.85f, 0.35f, 0.35f, 0.7f);

            // Right bracket (flipped)
            var bRightGo = new GameObject("BracketRight");
            bRightGo.transform.SetParent(reticleContainerGo.transform, false);
            var rtBRight = bRightGo.AddComponent<RectTransform>();
            rtBRight.anchoredPosition = new Vector2(28, 0);
            rtBRight.sizeDelta = new Vector2(24, 48);
            rtBRight.localScale = new Vector3(-1f, 1f, 1f);
            var imgBRight = bRightGo.AddComponent<Image>();
            imgBRight.sprite = bracketSprite;
            imgBRight.color = new Color(0.85f, 0.35f, 0.35f, 0.7f);

            // Prompt text below reticle
            var promptText = CreateUIText(reticleContainerGo.transform, "PromptText", "<color=#94A3B8>TARGET INVALID</color>", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, -45), new Vector2(260, 40), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // --- Center Notification Toast ---
            var notifCard = CreateUIPanel(canvasGo.transform, "NotificationCard", new Vector2(0, 120), new Vector2(520, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));
            var notifText = CreateUIText(notifCard.transform, "NotificationText", "CHECKPOINT REACHED", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500, 50), font, new Color(0.2f, 1.0f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            notifCard.SetActive(false);

            // --- Victory Panel ---
            var victoryPanel = CreateUIPanel(canvasGo.transform, "VictoryPanel", Vector2.zero, new Vector2(600, 320), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.98f));
            var vicTitle = CreateUIText(victoryPanel.transform, "VictoryTitle", "★ REALITY REWRITE COMPLETE ★", 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 80), new Vector2(560, 50), font, new Color(0.0f, 0.95f, 1.0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var vicSub = CreateUIText(victoryPanel.transform, "VictorySubtitle", "LEVEL 2 COMPLETE\n\n<size=18><color=#94A3B8>Press [ENTER] to Continue  •  [R] to Restart</color></size>", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, -20), new Vector2(540, 120), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            victoryPanel.SetActive(false);

            // --- Bottom Controls Bar ---
            var footerCard = CreateUIPanel(canvasGo.transform, "FooterCard", new Vector2(0, 20), new Vector2(820, 42), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Color(0.06f, 0.09f, 0.16f, 0.85f));
            CreateUIText(footerCard.transform, "FooterText", "<b>[WASD]</b> Move   •   <b>[SHIFT]</b> Sprint   •   <b>[SPACE]</b> Jump   •   <b>[MOUSE]</b> Aim   •   <b>[LMB]</b> Reality Rewrite   •   <b>[R]</b> Restart", 15, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(800, 36), font, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Attach Level2UIController
            var uiCtrl = canvasGo.AddComponent<Level2UIController>();
            var soUI = new SerializedObject(uiCtrl);
            soUI.FindProperty("rewriteController").objectReferenceValue = rewriteCtrl;
            soUI.FindProperty("respawnManager").objectReferenceValue = respawnMgr;
            soUI.FindProperty("levelTitleText").objectReferenceValue = titleText;
            soUI.FindProperty("objectiveText").objectReferenceValue = objText;
            soUI.FindProperty("cooldownText").objectReferenceValue = cdText;
            soUI.FindProperty("rewriteActiveText").objectReferenceValue = activeText;
            soUI.FindProperty("reticleContainer").objectReferenceValue = rtReticle;
            soUI.FindProperty("reticleCrosshair").objectReferenceValue = imgCross;
            soUI.FindProperty("reticleBracketLeft").objectReferenceValue = imgBLeft;
            soUI.FindProperty("reticleBracketRight").objectReferenceValue = imgBRight;
            soUI.FindProperty("targetPromptText").objectReferenceValue = promptText;
            soUI.FindProperty("notificationCard").objectReferenceValue = notifCard;
            soUI.FindProperty("notificationText").objectReferenceValue = notifText;
            soUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            soUI.FindProperty("victoryTitleText").objectReferenceValue = vicTitle;
            soUI.FindProperty("victorySubtitleText").objectReferenceValue = vicSub;
            soUI.ApplyModifiedPropertiesWithoutUndo();

            return uiCtrl;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = col;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return go;
        }

        private static Text CreateUIText(Transform parent, string name, string content, int fontSize, FontStyle style, TextAnchor align, Vector2 anchoredPos, Vector2 size, Font font, Color col, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            if (font != null) txt.material = font.material;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = col;
            txt.supportRichText = true;

            return txt;
        }

        private static ParticleSystem CreateParticleHelper(Transform parent, string name, Material mat, int burstCount, float lifetime, float speed, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var psRenderer = go.GetComponent<ParticleSystemRenderer>();
            if (mat != null) psRenderer.material = mat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;

            var emission = ps.emission;
            emission.enabled = false;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            return ps;
        }
    }
}
