using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Game.Core.Bootstrap;
using Game.Core.Events;
using Game.Data.Items;
using Game.Gameplay.Combat;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Presentation.CameraSystems;
using Game.Presentation.UI;

namespace Game.Editor
{
    public static class LevelBuilderTool
    {
        [MenuItem("Game/Build Complete Level 2", false, 10)]
        public static void BuildCompleteLevel()
        {
            Debug.Log("[LevelBuilderTool] Starting Level 2 build...");

            // 1. Build Prefabs and Data Assets (Plank, Ladder, Platform with maxCount = 2)
            BuildToolPrefabsAndData();

            // 2. Setup Camera & Parallax (Framing Level 2 at x: 5.0, y: 0.5)
            SetupCameraAndParallax();

            // 3. Setup Scene Geometry (Pillar, Wide Spikes, U-Pit Overhang, Lower Spikes Aerial Gap, Goal Platform)
            BuildSceneGeometry();

            // 4. Setup Player (under Controller====)
            var player = BuildPlayer();

            // 5. Setup UI (3 cards: Plank x2, Ladder x2, Platform x2)
            var ui = BuildUI(player);

            // 6. Setup Bootstrap on GameController
            SetupBootstrap(player, ui);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[LevelBuilderTool] Level 2 built successfully matching sketch with 4 obstacles, maxCount = 2 per tool, and UI Toolkit!");
        }

        public static void BuildToolPrefabsAndData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Items"))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Items");
            }

            var plankSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Plank.png");
            var ladderSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Ladder.png");
            var platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Platform.png");

            // Physics Material for tools to prevent excessive sliding
            var toolPhysMat = new PhysicsMaterial2D("ToolPhysicsMaterial")
            {
                friction = 0.85f,
                bounciness = 0.05f
            };
            string physMatPath = "Assets/_Game/Data/ToolPhysicsMat.physicsMaterial2D";
            AssetDatabase.CreateAsset(toolPhysMat, physMatPath);

            // --- 1. PLANK PREFAB (Horizontal Wooden Board / Bridge) ---
            var plankGO = new GameObject("PlankTool");
            var plankSR = plankGO.AddComponent<SpriteRenderer>();
            plankSR.sprite = plankSprite;
            plankSR.sortingOrder = 10;
            plankGO.transform.localScale = new Vector3(3.442985f, 2.330985f, 1f); // Enlarged bridge matching platform ratio

            var plankCol = plankGO.AddComponent<BoxCollider2D>();
            plankCol.size = new Vector2(2.6f, 0.4f);
            plankCol.sharedMaterial = toolPhysMat;

            var plankRB = plankGO.AddComponent<Rigidbody2D>();
            plankRB.bodyType = RigidbodyType2D.Dynamic;
            plankRB.mass = 3.0f;
            plankRB.gravityScale = 1.8f;
            plankRB.linearDamping = 1.0f;
            plankRB.angularDamping = 2.0f;
            plankRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var plankTool = plankGO.AddComponent<DraggableTool>();
            SetPrivateField(plankTool, "toolType", ToolType.Plank);

            string plankPrefabPath = "Assets/_Game/Data/Prefabs/PlankPrefab.prefab";
            var plankPrefab = PrefabUtility.SaveAsPrefabAsset(plankGO, plankPrefabPath);
            GameObject.DestroyImmediate(plankGO);

            // --- 2. LADDER PREFAB (Vertical Wooden Climbing Structure) ---
            var ladderGO = new GameObject("LadderTool");
            var ladderSR = ladderGO.AddComponent<SpriteRenderer>();
            ladderSR.sprite = ladderSprite;
            ladderSR.sortingOrder = 10;
            ladderGO.transform.localScale = new Vector3(2.787178f, 2.85f, 1f); // Enlarged climbing ladder

            // Solid physical frame collider (rests on ground)
            var ladderCol = ladderGO.AddComponent<BoxCollider2D>();
            ladderCol.size = new Vector2(0.75f, 2.5f);
            ladderCol.sharedMaterial = toolPhysMat;

            // Trigger child zone for climbing
            var climbGO = new GameObject("ClimbZone");
            climbGO.transform.SetParent(ladderGO.transform, false);
            var climbCol = climbGO.AddComponent<BoxCollider2D>();
            climbCol.size = new Vector2(1.1f, 2.6f);
            climbCol.isTrigger = true;
            climbGO.AddComponent<LadderClimbZone>();

            var ladderRB = ladderGO.AddComponent<Rigidbody2D>();
            ladderRB.bodyType = RigidbodyType2D.Dynamic;
            ladderRB.mass = 4.0f;
            ladderRB.gravityScale = 1.8f;
            ladderRB.linearDamping = 1.0f;
            ladderRB.angularDamping = 2.0f;
            ladderRB.freezeRotation = true; // Stays upright when falling!
            ladderRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var ladderTool = ladderGO.AddComponent<DraggableTool>();
            SetPrivateField(ladderTool, "toolType", ToolType.Ladder);

            string ladderPrefabPath = "Assets/_Game/Data/Prefabs/LadderPrefab.prefab";
            var ladderPrefab = PrefabUtility.SaveAsPrefabAsset(ladderGO, ladderPrefabPath);
            GameObject.DestroyImmediate(ladderGO);

            // --- 3. PLATFORM PREFAB (Chiseled Stone Platform Block) ---
            var platformGO = new GameObject("PlatformTool");
            var platformSR = platformGO.AddComponent<SpriteRenderer>();
            platformSR.sprite = platformSprite;
            platformSR.sortingOrder = 10;
            platformGO.transform.localScale = new Vector3(3.27903295f, 3.20f, 1f); // Enlarged platform scale

            var platformCol = platformGO.AddComponent<BoxCollider2D>();
            platformCol.size = new Vector2(2.0f, 0.95f);
            platformCol.sharedMaterial = toolPhysMat;

            var platformRB = platformGO.AddComponent<Rigidbody2D>();
            platformRB.bodyType = RigidbodyType2D.Dynamic;
            platformRB.mass = 6.0f;
            platformRB.gravityScale = 1.8f;
            platformRB.linearDamping = 1.0f;
            platformRB.angularDamping = 2.0f;
            platformRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var platformTool = platformGO.AddComponent<DraggableTool>();
            SetPrivateField(platformTool, "toolType", ToolType.Platform);

            string platformPrefabPath = "Assets/_Game/Data/Prefabs/PlatformPrefab.prefab";
            var platformPrefab = PrefabUtility.SaveAsPrefabAsset(platformGO, platformPrefabPath);
            GameObject.DestroyImmediate(platformGO);

            // --- CREATE SCRIPTABLE OBJECT DEFINITIONS (maxCount = 2 for Level 2!) ---
            CreateOrUpdateToolDef("Assets/_Game/Data/Items/PlankToolData.asset", ToolType.Plank, "Plank", plankSprite, plankPrefab, 2);
            CreateOrUpdateToolDef("Assets/_Game/Data/Items/LadderToolData.asset", ToolType.Ladder, "Ladder", ladderSprite, ladderPrefab, 2);
            CreateOrUpdateToolDef("Assets/_Game/Data/Items/PlatformToolData.asset", ToolType.Platform, "Platform", platformSprite, platformPrefab, 2);

            // Backward compatibility assets
            CreateOrUpdateToolDef("Assets/_Game/Data/Items/RampToolData.asset", ToolType.Plank, "Plank", plankSprite, plankPrefab, 2);
            CreateOrUpdateToolDef("Assets/_Game/Data/Items/BoxToolData.asset", ToolType.Platform, "Platform", platformSprite, platformPrefab, 2);

            AssetDatabase.SaveAssets();
        }

        private static void CreateOrUpdateToolDef(string path, ToolType type, string name, Sprite icon, GameObject prefab, int maxCount = 2)
        {
            var def = AssetDatabase.LoadAssetAtPath<ToolDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ToolDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            SetPrivateField(def, "toolType", type);
            SetPrivateField(def, "displayName", name);
            SetPrivateField(def, "icon", icon);
            SetPrivateField(def, "prefab", prefab);
            SetPrivateField(def, "maxCount", maxCount);
            EditorUtility.SetDirty(def);
        }

        public static void BuildSceneGeometry()
        {
            var bedrockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Bedrock.png");
            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Square.png");
            var ledgeMossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_LedgeMoss.png");
            var spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Spike.png");
            var goalShrineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_GoalShrine.png");

            var envRoot = GameObject.Find("ENV====");
            if (envRoot == null) envRoot = new GameObject("ENV====");

            // Remove existing Level_01_Geometry or Level_02_Geometry if present
            var existingGeom1 = envRoot.transform.Find("Level_01_Geometry");
            if (existingGeom1 != null) GameObject.DestroyImmediate(existingGeom1.gameObject);
            var existingGeom2 = envRoot.transform.Find("Level_02_Geometry");
            if (existingGeom2 != null) GameObject.DestroyImmediate(existingGeom2.gameObject);

            var levelGO = new GameObject("Level_02_Geometry");
            levelGO.transform.SetParent(envRoot.transform, false);

            var terrainMat = new PhysicsMaterial2D("TerrainPhysMat") { friction = 0.65f, bounciness = 0f };

            // =========================================================================
            // 0. DEEP SOLID BEDROCK FILL (Fills all underground solidly down to y = -7.0)
            // =========================================================================
            var bedrockBaseGO = CreateSimpleBlock(levelGO.transform, "Deep_Bedrock_Fill", bedrockSprite, Color.white, new Vector2(8.5f, -4.75f), new Vector2(34.0f, 4.5f), null, isTrigger: false);
            bedrockBaseGO.GetComponent<SpriteRenderer>().sortingOrder = 1;

            // =========================================================================
            // 1. START FLOOR (x: -7.2 to -3.8, surface at y = -1.5)
            // Low start platform where player stands, matching user sketch
            // =========================================================================
            CreatePlatformBlock(levelGO.transform, "Start_Floor", squareSprite, ledgeMossSprite, new Vector2(-5.5f, -4.25f), new Vector2(3.4f, 5.5f), terrainMat);

            // =========================================================================
            // 2. OBSTACLE 1: RAISED PILLAR BLOCK (x: -3.8 to -1.6, surface at y = 0.5)
            // Step up of 2.0m! Solved by climbing with Ladder 1.
            // =========================================================================
            CreatePlatformBlock(levelGO.transform, "Obstacle1_Pillar", squareSprite, ledgeMossSprite, new Vector2(-2.7f, -3.25f), new Vector2(2.2f, 7.5f), terrainMat);

            // =========================================================================
            // 3. OBSTACLE 2: WIDE SPIKE BED (x: -1.6 to +2.8, width = 4.4m)
            // Bedrock at y = -1.7 with 3 spike segments (tips reach y = -1.18).
            // Solved effortlessly by bridging with Plank 1 and Plank 2 (total 5.2m with overlap)!
            // =========================================================================
            CreatePlatformBlock(levelGO.transform, "Obstacle2_Bedrock", squareSprite, null, new Vector2(0.6f, -4.35f), new Vector2(4.4f, 5.3f), terrainMat);
            CreateSpikeBed(levelGO.transform, "Obstacle2_Spikes_1", spikeSprite, new Vector2(-0.87f, -1.425f), 1.46f);
            CreateSpikeBed(levelGO.transform, "Obstacle2_Spikes_2", spikeSprite, new Vector2(0.60f, -1.425f), 1.46f);
            CreateSpikeBed(levelGO.transform, "Obstacle2_Spikes_3", spikeSprite, new Vector2(2.07f, -1.425f), 1.46f);

            // =========================================================================
            // 4. OBSTACLE 3: MID STRUCTURE WITH U-PIT & OVERHANG (x: 2.8 to 9.8)
            // Surface at y = 0.8 with recessed U-Pit (depth 1.2m) and cantilevered overhang
            // =========================================================================
            // Left Plateau (x: 2.8 to 4.4, surface at y = 0.8)
            CreatePlatformBlock(levelGO.transform, "Obstacle3_Mid_Left", squareSprite, ledgeMossSprite, new Vector2(3.6f, -3.1f), new Vector2(1.6f, 7.8f), terrainMat);

            // Recessed U-Pit (x: 4.4 to 6.6, surface at y = -0.4)
            // Solved by dropping Platform 1 into the pit to create a level walking surface!
            CreatePlatformBlock(levelGO.transform, "Obstacle3_Pit_Bedrock", squareSprite, null, new Vector2(5.5f, -3.7f), new Vector2(2.2f, 6.6f), terrainMat);
            var pitHazardGO = new GameObject("Obstacle3_Pit_Hazard");
            pitHazardGO.transform.SetParent(levelGO.transform, false);
            pitHazardGO.transform.position = new Vector3(5.5f, -0.3f, 0f);
            var pitCol = pitHazardGO.AddComponent<BoxCollider2D>();
            pitCol.size = new Vector2(2.1f, 0.35f);
            pitCol.isTrigger = true;
            var pitHazard = pitHazardGO.AddComponent<Hazard2D>();
            SetPrivateField(pitHazard, "hazardName", "Pit Hazard");

            // Right Plateau (x: 6.6 to 7.8, surface at y = 0.8)
            CreatePlatformBlock(levelGO.transform, "Obstacle3_Mid_Right", squareSprite, ledgeMossSprite, new Vector2(7.2f, -3.1f), new Vector2(1.2f, 7.8f), terrainMat);

            // Cantilevered Overhang (x: 7.8 to 9.8, surface at y = 0.8, thickness = 0.4m)
            CreatePlatformBlock(levelGO.transform, "Obstacle3_Overhang", squareSprite, ledgeMossSprite, new Vector2(8.8f, 0.6f), new Vector2(2.0f, 0.4f), terrainMat);

            // =========================================================================
            // 5. OBSTACLE 4: LOWER SPIKES & AERIAL GAP (x: 7.8 to 11.8)
            // Bedrock at y = -1.7 under overhang and across the 2.0m aerial gap (x: 9.8 to 11.8).
            // Solved by bridging with Platform 2 or Plank 2!
            // =========================================================================
            CreatePlatformBlock(levelGO.transform, "Obstacle4_Bedrock", squareSprite, null, new Vector2(9.8f, -4.35f), new Vector2(4.0f, 5.3f), terrainMat);
            CreateSpikeBed(levelGO.transform, "Obstacle4_Spikes_1", spikeSprite, new Vector2(8.8f, -1.425f), 2.0f);
            CreateSpikeBed(levelGO.transform, "Obstacle4_Spikes_2", spikeSprite, new Vector2(10.8f, -1.425f), 2.0f);

            // =========================================================================
            // 6. GOAL PLATFORM & SHRINE (x: 11.8 to 23.0, surface at y = 0.8)
            // =========================================================================
            CreatePlatformBlock(levelGO.transform, "Goal_Platform", squareSprite, ledgeMossSprite, new Vector2(17.4f, -3.1f), new Vector2(11.2f, 7.8f), terrainMat);

            var goalGO = new GameObject("Goal_Shrine");
            goalGO.transform.SetParent(levelGO.transform, false);
            goalGO.transform.position = new Vector3(15.5f, 1.8f, 0f);
            goalGO.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var goalSR = goalGO.AddComponent<SpriteRenderer>();
            goalSR.sprite = goalShrineSprite;
            goalSR.sortingOrder = 7;

            var goalCol = goalGO.AddComponent<BoxCollider2D>();
            goalCol.size = new Vector2(1.6f, 2.4f);
            goalCol.isTrigger = true;
            goalGO.AddComponent<LevelGoal>();

            // Boundary Walls
            CreateSimpleBlock(levelGO.transform, "Left_Boundary", squareSprite, new Color(0.08f, 0.10f, 0.14f), new Vector2(-8.0f, 0.5f), new Vector2(1.2f, 16.0f), terrainMat);
            CreateSimpleBlock(levelGO.transform, "Right_Boundary", squareSprite, new Color(0.08f, 0.10f, 0.14f), new Vector2(23.6f, 0.5f), new Vector2(1.2f, 16.0f), terrainMat);
            CreateSimpleBlock(levelGO.transform, "Ceiling_Boundary", squareSprite, new Color(0.08f, 0.10f, 0.14f), new Vector2(7.8f, 8.5f), new Vector2(34.0f, 1.2f), terrainMat);

            // Tools Container
            var existingTools = envRoot.transform.Find("Placed_Tools");
            if (existingTools != null) GameObject.DestroyImmediate(existingTools.gameObject);
            var toolsContainer = new GameObject("Placed_Tools");
            toolsContainer.transform.SetParent(envRoot.transform, false);
        }

        private static GameObject CreateSpikeBed(
            Transform parent,
            string name,
            Sprite spikeSprite,
            Vector2 position,
            float width,
            float height = 0.55f)
        {
            var spikeGO = new GameObject(name);
            spikeGO.transform.SetParent(parent, false);
            spikeGO.transform.position = new Vector3(position.x, position.y, 0f);
            spikeGO.transform.localScale = new Vector3(width, height, 1f);

            var spikeSR = spikeGO.AddComponent<SpriteRenderer>();
            spikeSR.sprite = spikeSprite;
            spikeSR.sortingOrder = 4;

            var poly = spikeGO.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.pathCount = 2;
            poly.SetPath(0, new Vector2[] {
                new Vector2(-0.44f, -0.45f),
                new Vector2(-0.24f, 0.45f),
                new Vector2(-0.04f, -0.45f)
            });
            poly.SetPath(1, new Vector2[] {
                new Vector2(0.04f, -0.45f),
                new Vector2(0.24f, 0.45f),
                new Vector2(0.44f, -0.45f)
            });

            var hazard = spikeGO.AddComponent<Hazard2D>();
            SetPrivateField(hazard, "hazardName", "Spikes");

            return spikeGO;
        }

        private static GameObject CreatePlatformBlock(
            Transform parent,
            string name,
            Sprite bodySprite,
            Sprite capSprite,
            Vector2 position,
            Vector2 size,
            PhysicsMaterial2D mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            // Main architectural slate block
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(go.transform, false);
            var bodySR = bodyGO.AddComponent<SpriteRenderer>();
            bodySR.sprite = bodySprite;
            bodySR.color = Color.white;
            bodySR.sortingOrder = 3;
            bodyGO.transform.localScale = new Vector3(size.x / 2.0f, size.y / 2.0f, 1f);

            // Physical Solid Collider
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = mat;

            // Glowing cyan/green moss cap along the top surface
            if (capSprite != null)
            {
                var capGO = new GameObject("LedgeCap");
                capGO.transform.SetParent(go.transform, false);
                capGO.transform.localPosition = new Vector3(0f, (size.y / 2.0f) - 0.15f, 0f);
                capGO.transform.localScale = new Vector3(size.x / 2.0f, 0.6f, 1f);
                var capSR = capGO.AddComponent<SpriteRenderer>();
                capSR.sprite = capSprite;
                capSR.color = Color.white;
                capSR.sortingOrder = 5;
            }

            return go;
        }

        private static GameObject CreateSimpleBlock(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector2 position,
            Vector2 scale,
            PhysicsMaterial2D mat,
            bool isTrigger = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 4;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.isTrigger = isTrigger;
            if (mat != null) col.sharedMaterial = mat;

            return go;
        }

        public static AutonomousPlayerController BuildPlayer()
        {
            var controllerRoot = GameObject.Find("Controller====");
            if (controllerRoot == null) controllerRoot = new GameObject("Controller====");

            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Player.png");

            var existingPlayer = controllerRoot.transform.Find("Player");
            if (existingPlayer != null) GameObject.DestroyImmediate(existingPlayer.gameObject);

            var playerGO = new GameObject("Player");
            playerGO.transform.SetParent(controllerRoot.transform, false);
            playerGO.transform.position = new Vector3(-5.6f, -0.9f, 0f); // Resting on Start Floor (y = -1.5)
            playerGO.transform.localScale = new Vector3(0.82f, 0.82f, 1f); // Proportional scale matching Hollow Knight reference

            var sr = playerGO.AddComponent<SpriteRenderer>();
            sr.sprite = playerSprite;
            sr.sortingOrder = 8;

            var col = playerGO.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.65f, 1.25f);
            col.offset = new Vector2(0f, 0f);

            var playerMat = new PhysicsMaterial2D("PlayerPhysMat") { friction = 0f, bounciness = 0f };
            col.sharedMaterial = playerMat;

            var rb = playerGO.AddComponent<Rigidbody2D>();
            rb.mass = 1.0f;
            rb.linearDamping = 0.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var controller = playerGO.AddComponent<AutonomousPlayerController>();
            SetPrivateField(controller, "walkSpeed", 6.7f);
            SetPrivateField(controller, "climbSpeed", 6.5f);
            SetPrivateField(controller, "stuckTimeoutSeconds", 5.0f);
            SetPrivateField(controller, "maxStepHeight", 0.45f);
            SetPrivateField(controller, "stepSearchDistance", 0.50f);

            return controller;
        }

        public static (ScreenFaderUI fader, ToolSelectionBarUI toolBar, GameOverUI gameOver, PlayerHUDUI hud) BuildUI(AutonomousPlayerController player)
        {
            var uiRoot = GameObject.Find("UI====");
            if (uiRoot == null) uiRoot = new GameObject("UI====");

            // Clean up any legacy objects under UI====
            for (int i = uiRoot.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(uiRoot.transform.GetChild(i).gameObject);
            }

            // Create UI Toolkit UIDocument Root
            var uiDocGO = new GameObject("UIDocument");
            uiDocGO.transform.SetParent(uiRoot.transform, false);

            var uiDoc = uiDocGO.AddComponent<UIDocument>();
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Game/Presentation/UI/DefaultPanelSettings.asset");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/Presentation/UI/GameUI.uxml");

            if (panelSettings != null) uiDoc.panelSettings = panelSettings;
            if (visualTree != null) uiDoc.visualTreeAsset = visualTree;

            // Add UI Presentation Controllers
            var faderUI = uiDocGO.AddComponent<ScreenFaderUI>();
            var toolBarUI = uiDocGO.AddComponent<ToolSelectionBarUI>();
            var gameOverUI = uiDocGO.AddComponent<GameOverUI>();
            var hudUI = uiDocGO.AddComponent<PlayerHUDUI>();

            SetPrivateField(faderUI, "uiDocument", uiDoc);
            SetPrivateField(toolBarUI, "uiDocument", uiDoc);
            SetPrivateField(gameOverUI, "uiDocument", uiDoc);
            SetPrivateField(hudUI, "uiDocument", uiDoc);

            return (faderUI, toolBarUI, gameOverUI, hudUI);
        }

        public static void SetupCameraAndParallax()
        {
            var coreRoot = GameObject.Find("Core====");
            if (coreRoot == null) coreRoot = new GameObject("Core====");

            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(coreRoot.transform, false);
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            else
            {
                cam.transform.SetParent(coreRoot.transform, true);
            }

            cam.transform.position = new Vector3(4.0f, 0.5f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 7.0f; // Wide cinematic view matching Hollow Knight reference ratio
            cam.backgroundColor = new Color(0.07f, 0.10f, 0.18f); // Deep mystical blue twilight

            // Parallax Backdrop Layers
            var envRoot = GameObject.Find("ENV====");
            if (envRoot == null) envRoot = new GameObject("ENV====");

            var existingParallax = envRoot.transform.Find("Parallax_Layers");
            if (existingParallax != null) GameObject.DestroyImmediate(existingParallax.gameObject);

            var parallaxRoot = new GameObject("Parallax_Layers");
            parallaxRoot.transform.SetParent(envRoot.transform, false);

            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Square.png");

            // Distant moon & ruin backdrop
            var distantGO = CreateVisualBlock(parallaxRoot.transform, "Distant_Ruins", squareSprite, new Color(0.05f, 0.08f, 0.14f, 0.7f), new Vector2(8f, 4.0f), new Vector2(56f, 9f));
            distantGO.GetComponent<SpriteRenderer>().sortingOrder = -8;
            var distLayer = distantGO.AddComponent<ParallaxLayer>();
            SetPrivateField(distLayer, "parallaxEffectX", 0.04f);

            // Midground waterfall silhouettes
            var midGO = CreateVisualBlock(parallaxRoot.transform, "Mid_Waterfalls", squareSprite, new Color(0.10f, 0.15f, 0.25f, 0.85f), new Vector2(8f, 3.0f), new Vector2(54f, 7.5f));
            midGO.GetComponent<SpriteRenderer>().sortingOrder = -5;
            var midLayer = midGO.AddComponent<ParallaxLayer>();
            SetPrivateField(midLayer, "parallaxEffectX", 0.10f);
        }

        private static GameObject CreateVisualBlock(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector2 position,
            Vector2 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;

            return go;
        }

        public static void SetupBootstrap(
            AutonomousPlayerController player,
            (ScreenFaderUI fader, ToolSelectionBarUI toolBar, GameOverUI gameOver, PlayerHUDUI hud) ui)
        {
            var controllerRoot = GameObject.Find("Controller====");
            var gameCtrl = controllerRoot.transform.Find("GameController");
            if (gameCtrl == null)
            {
                var go = new GameObject("GameController");
                go.transform.SetParent(controllerRoot.transform, false);
                gameCtrl = go.transform;
            }

            // Add PlacementSystem
            var placementSystem = gameCtrl.GetComponent<PlacementSystem>();
            if (placementSystem == null) placementSystem = gameCtrl.gameObject.AddComponent<PlacementSystem>();

            var uiDoc = Object.FindFirstObjectByType<UIDocument>();
            var soPS = new SerializedObject(placementSystem);
            soPS.FindProperty("uiDocument").objectReferenceValue = uiDoc;
            soPS.ApplyModifiedProperties();
            SetPrivateField(placementSystem, "uiDocument", uiDoc);

            // Add GameBootstrap
            var bootstrap = gameCtrl.GetComponent<GameBootstrap>();
            if (bootstrap == null) bootstrap = gameCtrl.gameObject.AddComponent<GameBootstrap>();

            var envRoot = GameObject.Find("ENV====");
            var toolsContainer = envRoot.transform.Find("Placed_Tools");

            var plankDef = AssetDatabase.LoadAssetAtPath<ToolDefinition>("Assets/_Game/Data/Items/PlankToolData.asset");
            var ladderDef = AssetDatabase.LoadAssetAtPath<ToolDefinition>("Assets/_Game/Data/Items/LadderToolData.asset");
            var platformDef = AssetDatabase.LoadAssetAtPath<ToolDefinition>("Assets/_Game/Data/Items/PlatformToolData.asset");
            var toolDefs = new[] { plankDef, ladderDef, platformDef };

            var so = new SerializedObject(bootstrap);
            so.FindProperty("mainCamera").objectReferenceValue = Camera.main;
            so.FindProperty("playerController").objectReferenceValue = player;
            so.FindProperty("placementSystem").objectReferenceValue = placementSystem;
            so.FindProperty("toolSpawner").objectReferenceValue = null;
            so.FindProperty("toolsContainer").objectReferenceValue = toolsContainer;
            so.FindProperty("screenFader").objectReferenceValue = ui.fader;
            so.FindProperty("toolSelectionBar").objectReferenceValue = ui.toolBar;
            so.FindProperty("gameOverUI").objectReferenceValue = ui.gameOver;
            so.FindProperty("playerHUD").objectReferenceValue = ui.hud;

            var defsProp = so.FindProperty("toolDefinitions");
            if (defsProp != null)
            {
                defsProp.arraySize = toolDefs.Length;
                for (int i = 0; i < toolDefs.Length; i++)
                {
                    defsProp.GetArrayElementAtIndex(i).objectReferenceValue = toolDefs[i];
                }
            }
            so.ApplyModifiedProperties();

            SetPrivateField(bootstrap, "mainCamera", Camera.main);
            SetPrivateField(bootstrap, "playerController", player);
            SetPrivateField(bootstrap, "placementSystem", placementSystem);
            SetPrivateField(bootstrap, "toolSpawner", null); // Mouse placement is direct!
            SetPrivateField(bootstrap, "toolsContainer", toolsContainer);
            SetPrivateField(bootstrap, "screenFader", ui.fader);
            SetPrivateField(bootstrap, "toolSelectionBar", ui.toolBar);
            SetPrivateField(bootstrap, "gameOverUI", ui.gameOver);
            SetPrivateField(bootstrap, "playerHUD", ui.hud);
            SetPrivateField(bootstrap, "toolDefinitions", toolDefs);

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(gameCtrl.gameObject);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[LevelBuilderTool] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
