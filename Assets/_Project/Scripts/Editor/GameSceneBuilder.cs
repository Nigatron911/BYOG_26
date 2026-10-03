using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Project.Audio;
using Project.CameraControl;
using Project.Core;
using Project.Environment;
using Project.Managers;
using Project.Player;
using Project.UI;

namespace Project.Editor
{
    public static class GameSceneBuilder
    {
        [MenuItem("Tools/Build Complete Game Scene", priority = 20)]
        public static void BuildScene()
        {
            // First ensure all sprites exist
            AssetGenerator.GenerateAllSprites();

            // Create new scene or clear current
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main_Scene";

            // Load sprite assets
            var paperSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/paper_form.png");
            var stoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/stone_form.png");
            var rubberSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/rubber_form.png");
            var platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/platform_tile.png");
            var fragileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/fragile_tile.png");
            var windParticleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/wind_particle.png");
            var ventGrateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/wind_vent_grate.png");
            var beaconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/checkpoint_beacon.png");
            var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/finish_trophy.png");
            var sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/particle_spark.png");

            var particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/ParticleUnlit.mat");
            var spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Sprite2DUnlit.mat");

            Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Setup Ground Layer
            CreateLayerIfMissing("Ground");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0; // Default fallback

            // 1. Root Headers for clean organization
            var coreRoot = new GameObject("Core====");
            var controllerRoot = new GameObject("Controller====");
            var envRoot = new GameObject("ENV====");
            var uiRoot = new GameObject("UI====");

            // 2. Camera Setup
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(coreRoot.transform);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(1.5f, 2.4f, -10f);

            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.18f, 1f); // Deep night sky
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;

            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var smoothCam = cameraGo.AddComponent<SmoothCamera2D>();
            var soCam = new SerializedObject(smoothCam);
            soCam.FindProperty("minBounds").vector2Value = new Vector2(-1f, 1.2f);
            soCam.FindProperty("maxBounds").vector2Value = new Vector2(40f, 10f);
            soCam.FindProperty("offset").vector3Value = new Vector3(2f, 1.2f, -10f);
            soCam.ApplyModifiedPropertiesWithoutUndo();

            // Global 2D Light for ambience
            var lightGo = new GameObject("Global Light 2D");
            lightGo.transform.SetParent(envRoot.transform);
            var light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 3. Audio & Managers
            var managersGo = new GameObject("GameManager");
            managersGo.transform.SetParent(controllerRoot.transform);
            var audioComp = managersGo.AddComponent<ProceduralAudio>();
            var respawnMgr = managersGo.AddComponent<RespawnManager>();
            var objMgr = managersGo.AddComponent<ObjectivesManager>();

            // 4. Background Backdrop
            CreateBackdrop(envRoot.transform, spriteMat);

            // 5. Build Platforms & Geometry (matching user sketch precisely)
            var platformsRoot = new GameObject("Platforms");
            platformsRoot.transform.SetParent(envRoot.transform);

            // Ground Floor (Continuous floor from X = -1.0 to X = 19.5, thickness 1.0, top at Y = 0.5)
            CreatePlatform(platformsRoot.transform, "GroundFloor", new Vector2(9.25f, 0f), new Vector2(20.5f, 1f), platformSprite, groundLayer);

            // Left boundary wall (X = -1.5, Y = 4.5, height 10)
            CreatePlatform(platformsRoot.transform, "LeftBoundaryWall", new Vector2(-1.5f, 4.5f), new Vector2(1f, 10f), platformSprite, groundLayer);

            // Floating Platform 1 (Elevated at Y = 3.2, top at Y = 3.6, reachable only via Wind Vent lift!)
            // Approach Ledge (X = 10.0 to 12.4, width 2.4)
            CreatePlatform(platformsRoot.transform, "UpperApproachLedge", new Vector2(11.2f, 3.2f), new Vector2(2.4f, 0.8f), platformSprite, groundLayer);

            // Fragile Floor attached to Platform 1 approach ledge (X = 12.4 to 17.4, width 5.0)
            var fragileFloorGo = CreateFragileFloor(envRoot.transform, new Vector2(14.9f, 3.2f), new Vector2(5.0f, 0.8f), fragileSprite, particleMat, sparkSprite, groundLayer);

            // Upper barrier above fragile floor right end (prevents jumping over without breaking through)
            CreatePlatform(platformsRoot.transform, "UpperBarrier", new Vector2(17.8f, 5.8f), new Vector2(0.8f, 4.5f), platformSprite, groundLayer);

            // Takeoff Cliff Rubber Guide Signpost before the gap
            CreateSignpost(envRoot.transform, new Vector2(17.5f, 0.5f), defaultFont);

            // Platform 2 across the 8m gap (X = 27.5 to 36.5, Y = 2.8, top at Y = 3.2)
            CreatePlatform(platformsRoot.transform, "Platform2Landing", new Vector2(32.0f, 2.8f), new Vector2(9.0f, 0.8f), platformSprite, groundLayer);

            // Right boundary wall (X = 36.8, Y = 5.5)
            CreatePlatform(platformsRoot.transform, "RightBoundaryWall", new Vector2(36.8f, 5.5f), new Vector2(0.8f, 6.5f), platformSprite, groundLayer);

            // KillZone below the chasm
            var killZoneGo = new GameObject("KillZone");
            killZoneGo.transform.SetParent(envRoot.transform);
            killZoneGo.transform.position = new Vector3(23.5f, -6f, 0f);
            var kzCol = killZoneGo.AddComponent<BoxCollider2D>();
            kzCol.size = new Vector2(50f, 4f);
            kzCol.isTrigger = true;
            killZoneGo.AddComponent<KillZone>();

            // 6. Build Wind Obstacle (Floor vent at X = 8.5, Y = 2.9, size 2.6 x 4.8)
            var windGo = CreateWindObstacle(envRoot.transform, new Vector2(8.5f, 2.9f), new Vector2(2.6f, 4.8f), particleMat, windParticleSprite, ventGrateSprite);

            // 7. Initial Spawn Point (Transform only, NO Checkpoint component! Eliminates green circle at spawn!)
            var spawnPointGo = new GameObject("InitialSpawnPoint");
            spawnPointGo.transform.SetParent(envRoot.transform);
            spawnPointGo.transform.position = new Vector3(1.5f, 1.2f, 0f);

            // Checkpoints in the level:
            // Checkpoint 1: On Platform 1 approach ledge (reached via Paper wind lift)
            var cp1 = CreateCheckpoint(envRoot.transform, 1, new Vector2(11.0f, 4.3f), beaconSprite, particleMat, sparkSprite);

            // Checkpoint 2: On Lower Ground below Fragile Floor (reached by Stone breaking floor)
            var cp2 = CreateCheckpoint(envRoot.transform, 2, new Vector2(15.0f, 1.3f), beaconSprite, particleMat, sparkSprite);

            // Checkpoint 3: On Platform 2 (reached by Rubber double jump across gap)
            var cp3 = CreateCheckpoint(envRoot.transform, 3, new Vector2(29.0f, 3.9f), beaconSprite, particleMat, sparkSprite);

            // 8. Build Objective Triggers
            CreateObjectiveTrigger(envRoot.transform, ObjectivesManager.ObjectiveStep.BreakFragileFloor, new Vector2(11.2f, 4.2f), new Vector2(2.5f, 3.0f));
            CreateObjectiveTrigger(envRoot.transform, ObjectivesManager.ObjectiveStep.UseRubberToCrossGap, new Vector2(15.0f, 1.3f), new Vector2(4.0f, 3.0f));
            CreateObjectiveTrigger(envRoot.transform, ObjectivesManager.ObjectiveStep.ReachTheFinish, new Vector2(29.0f, 3.9f), new Vector2(3.0f, 3.0f));

            // 9. Build Finish Shrine (At the end of Platform 2)
            var finishGo = CreateFinishShrine(envRoot.transform, new Vector2(34.5f, 4.1f), trophySprite, particleMat, sparkSprite);

            // 10. Build Player (Spawn at X = 1.5, Y = 1.2)
            var playerGo = CreatePlayer(coreRoot.transform, new Vector2(1.5f, 1.2f), paperSprite, stoneSprite, rubberSprite, particleMat, sparkSprite, groundLayer);
            var playerCtrl = playerGo.GetComponent<PlayerMaterialController>();
            smoothCam.SetTarget(playerGo.transform);

            // Bind player to RespawnManager via SerializedObject
            var soRespawn = new SerializedObject(respawnMgr);
            soRespawn.FindProperty("player").objectReferenceValue = playerCtrl;
            soRespawn.FindProperty("initialSpawnPoint").objectReferenceValue = spawnPointGo.transform;
            soRespawn.ApplyModifiedPropertiesWithoutUndo();

            // 11. Build UI Canvas
            BuildGameUI(uiRoot.transform, playerCtrl, respawnMgr, objMgr, cam);

            // Save scene to Assets/_Project/Scenes/game.unity and copy to Assets/Main_Scene.unity
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/game.unity");
            EditorSceneManager.SaveScene(scene, "Assets/Main_Scene.unity", true);

            // Update EditorBuildSettings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Main_Scene.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/game.unity", true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("[GameSceneBuilder] Complete game scene built and saved to Main_Scene.unity and game.unity!");
        }

        private static void CreateLayerIfMissing(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                if (element.stringValue == layerName) return; // Already exists
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }

        private static void CreateBackdrop(Transform parent, Material spriteMat)
        {
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parent);
            backdrop.transform.position = new Vector3(32f, 8f, 5f);

            var sr = backdrop.AddComponent<SpriteRenderer>();
            // 1x1 white texture tinted to dark sci-fi night sky gradient
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.06f, 0.09f, 0.16f, 1f));
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.material = spriteMat;
            backdrop.transform.localScale = new Vector3(140f, 40f, 1f);
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

        private static GameObject CreateFragileFloor(Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Material particleMat, Sprite sparkSprite, int layer)
        {
            var go = new GameObject("FragileFloor");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            // Particles for breaking
            var psGo = new GameObject("BreakParticles");
            psGo.transform.SetParent(go.transform);
            psGo.transform.localPosition = Vector3.zero;

            var ps = psGo.AddComponent<ParticleSystem>();
            var psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (particleMat != null) psRenderer.material = particleMat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = new Color(0.85f, 0.65f, 0.45f, 1f);
            main.gravityModifier = 1.8f;

            var emission = ps.emission;
            emission.enabled = false;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, 0.4f, 1f);

            var fragile = go.AddComponent<FragileFloor>();
            var so = new SerializedObject(fragile);
            so.FindProperty("breakParticles").objectReferenceValue = ps;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("floorCollider").objectReferenceValue = col;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateWindObstacle(Transform parent, Vector2 pos, Vector2 size, Material particleMat, Sprite windSprite, Sprite grateSprite)
        {
            var go = new GameObject("WindObstacle");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            // Wind Vent Floor Grate visual
            if (grateSprite != null)
            {
                var grateGo = new GameObject("VentGrateVisual");
                grateGo.transform.SetParent(go.transform);
                grateGo.transform.localPosition = new Vector3(0f, -size.y * 0.5f + 0.15f, 0f);
                var grateSr = grateGo.AddComponent<SpriteRenderer>();
                grateSr.sprite = grateSprite;
                grateSr.drawMode = SpriteDrawMode.Tiled;
                grateSr.size = new Vector2(size.x, 0.35f);
                grateSr.sortingOrder = 2;
            }

            // Wind Visual Column
            var bgGo = new GameObject("WindColumnVisual");
            bgGo.transform.SetParent(go.transform);
            bgGo.transform.localPosition = Vector3.zero;
            var sr = bgGo.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.2f, 0.75f, 1.0f, 0.18f)); // Soft translucent cyan wind shaft
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            bgGo.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.sortingOrder = -5;

            // Wind Particles
            var psGo = new GameObject("WindParticles");
            psGo.transform.SetParent(go.transform);
            psGo.transform.localPosition = new Vector3(0f, -size.y * 0.45f, 0f);

            var ps = psGo.AddComponent<ParticleSystem>();
            var psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (particleMat != null)
            {
                var windMat = new Material(particleMat);
                if (windSprite != null)
                {
                    windMat.mainTexture = windSprite.texture;
                    windMat.SetTexture("_BaseMap", windSprite.texture);
                }
                psRenderer.material = windMat;
            }

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = 1.0f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.8f);
            main.gravityModifier = -0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x * 0.85f, 0.2f, 1f);
            shape.rotation = new Vector3(-90f, 0f, 0f); // Point upwards

            var windComp = go.AddComponent<WindObstacle>();
            var so = new SerializedObject(windComp);
            so.FindProperty("windParticles").objectReferenceValue = ps;
            so.FindProperty("windBackground").objectReferenceValue = sr;
            so.FindProperty("windForce").vector2Value = new Vector2(1.5f, 26f);
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
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

            // Sparkle flare
            var psGo = new GameObject("ActiveParticles");
            psGo.transform.SetParent(go.transform);
            psGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var ps = psGo.AddComponent<ParticleSystem>();
            var psRenderer = psGo.GetComponent<ParticleSystemRenderer>();
            if (particleMat != null) psRenderer.material = particleMat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 3f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.1f, 1.0f, 0.6f, 1f);

            var emission = ps.emission;
            emission.enabled = false;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var cp = go.AddComponent<Checkpoint>();
            var so = new SerializedObject(cp);
            so.FindProperty("checkpointIndex").intValue = index;
            so.FindProperty("spawnPoint").objectReferenceValue = go.transform;
            so.FindProperty("beaconRenderer").objectReferenceValue = sr;
            so.FindProperty("activeParticles").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cp;
        }

        private static void CreateObjectiveTrigger(Transform parent, ObjectivesManager.ObjectiveStep step, Vector2 pos, Vector2 size)
        {
            var go = new GameObject($"Trigger_{step}");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var trig = go.AddComponent<ObjectiveTrigger>();
            var so = new SerializedObject(trig);
            so.FindProperty("stepToTrigger").enumValueIndex = (int)step;
            so.ApplyModifiedPropertiesWithoutUndo();
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

            // Confetti particles
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

        private static GameObject CreatePlayer(Transform parent, Vector2 pos, Sprite paperSprite, Sprite stoneSprite, Sprite rubberSprite, Material particleMat, Sprite sparkSprite, int groundLayer)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.tag = "Player";

            var rb = go.AddComponent<Rigidbody2D>();
            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 1.0f);

            var gfx = new GameObject("Visual");
            gfx.transform.SetParent(go.transform);
            gfx.transform.localPosition = Vector3.zero;
            var sr = gfx.AddComponent<SpriteRenderer>();
            sr.sprite = paperSprite;
            sr.sortingOrder = 10;

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(go.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.48f, 0f);

            // Transformation VFX
            var transformVFX = CreateParticleHelper(go.transform, "TransformVFX", particleMat, 18, 0.4f, 4f, 0.35f, Color.white);
            var bounceVFX = CreateParticleHelper(go.transform, "BounceVFX", particleMat, 12, 0.3f, 3f, 0.25f, new Color(0f, 1f, 0.8f));
            var doubleJumpVFX = CreateParticleHelper(go.transform, "DoubleJumpVFX", particleMat, 16, 0.35f, 5f, 0.3f, new Color(0.2f, 0.8f, 1f));

            var ctrl = go.AddComponent<PlayerMaterialController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("paperSprite").objectReferenceValue = paperSprite;
            so.FindProperty("stoneSprite").objectReferenceValue = stoneSprite;
            so.FindProperty("rubberSprite").objectReferenceValue = rubberSprite;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("rb").objectReferenceValue = rb;
            so.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
            so.FindProperty("groundLayer").intValue = 1 << groundLayer;
            so.FindProperty("transformParticles").objectReferenceValue = transformVFX;
            so.FindProperty("bounceParticles").objectReferenceValue = bounceVFX;
            so.FindProperty("doubleJumpParticles").objectReferenceValue = doubleJumpVFX;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
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

        private static void CreateSignpost(Transform parent, Vector2 pos, Font font)
        {
            var go = new GameObject("RubberGuideSign");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            // Wooden Posts
            for (int i = -1; i <= 1; i += 2)
            {
                var postGo = new GameObject($"Post_{i}");
                postGo.transform.SetParent(go.transform);
                postGo.transform.localPosition = new Vector3(i * 1.2f, 0.65f, 0f);
                var srPost = postGo.AddComponent<SpriteRenderer>();
                var texPost = new Texture2D(1, 1);
                texPost.SetPixel(0, 0, new Color(0.35f, 0.25f, 0.15f, 1f));
                texPost.Apply();
                srPost.sprite = Sprite.Create(texPost, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                postGo.transform.localScale = new Vector3(0.14f, 1.6f, 1f);
                srPost.sortingOrder = 1;
            }

            // World Space Canvas for clear text display
            var canvasGo = new GameObject("SignCanvas");
            canvasGo.transform.SetParent(go.transform);
            canvasGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            canvasGo.transform.localScale = new Vector3(0.012f, 0.012f, 1f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            var panelGo = CreateUIPanel(canvasGo.transform, "SignBoardPanel", Vector2.zero, new Vector2(380, 130), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));

            string text = "<size=20><color=#00E5FF><b>PRESS [3] TO BECOME RUBBER</b></color></size>\n\n" +
                          "<size=16><color=#FFFFFF><b>[SPACE]</b> — JUMP</color>\n" +
                          "<color=#FDE047><b>[RUBBER]</b> — SPECIAL DOUBLE JUMP</color></size>";

            CreateUIText(panelGo.transform, "SignText", text, 16, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(370, 120), font, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private static void BuildGameUI(Transform parent, PlayerMaterialController player, RespawnManager respawnMgr, ObjectivesManager objMgr, Camera cam)
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

            // EventSystem with InputSystemUIInputModule
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(parent);
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Fonts/arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- Top-Left HUD Card (Materials & Charges) ---
            var hudCard = CreateUIPanel(canvasGo.transform, "HUDCard", new Vector2(20, -20), new Vector2(340, 245), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Color(0.08f, 0.11f, 0.18f, 0.90f));

            var matText = CreateUIText(hudCard.transform, "MaterialText", "MATERIAL: PAPER", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18, -25), new Vector2(300, 32), defaultFont, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var transText = CreateUIText(hudCard.transform, "TransText", "TRANSFORMATIONS: 3/3", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18, -60), new Vector2(300, 28), defaultFont, new Color(0.22f, 0.74f, 0.97f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // Key list
            var key1 = CreateUIText(hudCard.transform, "Key1", "1 PAPER", 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(25, -96), new Vector2(290, 24), defaultFont, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var key2 = CreateUIText(hudCard.transform, "Key2", "2 STONE", 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(25, -126), new Vector2(290, 24), defaultFont, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            var key3 = CreateUIText(hudCard.transform, "Key3", "3 RUBBER", 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(25, -156), new Vector2(290, 24), defaultFont, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // Controls hint at bottom of HUD card
            CreateUIText(hudCard.transform, "Hint", "<color=#94A3B8>[1/2/3] Transform form</color>", 13, FontStyle.Italic, TextAnchor.MiddleLeft, new Vector2(18, -195), new Vector2(300, 20), defaultFont, Color.gray, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // --- Top-Center Objective Card ---
            var objCard = CreateUIPanel(canvasGo.transform, "ObjectiveCard", new Vector2(0, -20), new Vector2(640, 85), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.08f, 0.11f, 0.18f, 0.90f));
            var objText = CreateUIText(objCard.transform, "ObjectiveText", "OBJECTIVE:\nUSE WIND TO REACH THE UPPER PLATFORM", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620, 75), defaultFont, new Color(0.99f, 0.88f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // --- Center Notification Toast ---
            var notifCard = CreateUIPanel(canvasGo.transform, "NotificationCard", new Vector2(0, 100), new Vector2(500, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.95f));
            var notifText = CreateUIText(notifCard.transform, "NotificationText", "CHECKPOINT REACHED", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480, 50), defaultFont, new Color(0.2f, 1.0f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            notifCard.SetActive(false);

            // --- Victory Panel ---
            var victoryPanel = CreateUIPanel(canvasGo.transform, "VictoryPanel", Vector2.zero, new Vector2(550, 320), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.06f, 0.08f, 0.14f, 0.97f));
            var vicTitle = CreateUIText(victoryPanel.transform, "VictoryTitle", "★ LEVEL COMPLETE ★", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, 80), new Vector2(500, 50), defaultFont, new Color(0.99f, 0.88f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var vicStats = CreateUIText(victoryPanel.transform, "VictoryStats", "TRANSFORMATIONS REMAINING: 3\n\n<size=18><color=#94A3B8>Press [R] to Play Again</color></size>", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0, -20), new Vector2(500, 120), defaultFont, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            victoryPanel.SetActive(false);

            // --- Bottom Controls Bar ---
            var footerCard = CreateUIPanel(canvasGo.transform, "FooterCard", new Vector2(0, 20), new Vector2(740, 42), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Color(0.08f, 0.11f, 0.18f, 0.85f));
            CreateUIText(footerCard.transform, "FooterText", "<b>[A / D]</b> Move   •   <b>[SPACE]</b> Jump / Double Jump   •   <b>[1/2/3]</b> Materials   •   <b>[R]</b> Restart", 15, FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(720, 36), defaultFont, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Attach GameUIController
            var uiController = canvasGo.AddComponent<GameUIController>();
            var so = new SerializedObject(uiController);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("respawnManager").objectReferenceValue = respawnMgr;
            so.FindProperty("objectivesManager").objectReferenceValue = objMgr;
            so.FindProperty("materialText").objectReferenceValue = matText;
            so.FindProperty("transformationsText").objectReferenceValue = transText;
            so.FindProperty("keyPaperText").objectReferenceValue = key1;
            so.FindProperty("keyStoneText").objectReferenceValue = key2;
            so.FindProperty("keyRubberText").objectReferenceValue = key3;
            so.FindProperty("objectiveText").objectReferenceValue = objText;
            so.FindProperty("notificationText").objectReferenceValue = notifText;
            so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            so.FindProperty("victoryTitleText").objectReferenceValue = vicTitle;
            so.FindProperty("victoryStatsText").objectReferenceValue = vicStats;
            so.ApplyModifiedPropertiesWithoutUndo();
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

            // Subtle border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.45f, 0.65f, 0.5f);
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
    }
}
