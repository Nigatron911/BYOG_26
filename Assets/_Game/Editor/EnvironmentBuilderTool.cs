#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Gameplay.Interaction;
using Game.Gameplay.Environment;
using Game.Gameplay.Transmutation;
using Game.Presentation.CameraSystems;

namespace Project.Editor
{
    /// <summary>
    /// Builds and assembles the complete game environment across all 8 levels
    /// using ONLY the imported project assets in Assets/assets/.
    /// Strictly adheres to GEMINI.md and user requirements:
    /// - Clean hierarchy: Background, Ground, Platforms, Walls, Ladders, Structures, Props, Decorations, GoalArea.
    /// - Sliced/Tiled non-stretching draw modes with exact collider alignment.
    /// - Full parallax backdrops and atmospheric clouds.
    /// - Global 2D lighting.
    /// </summary>
    public static class EnvironmentBuilderTool
    {
        private const string PlatformAssetPath = "Assets/assets/Props/Platform.png";
        private const string BoardAssetPath = "Assets/assets/Props/Board.png";
        private const string LadderAssetPath = "Assets/assets/Props/Ladder.png";
        private const string ChainAssetPath = "Assets/assets/Props/Chain.png";
        private const string SpikesAssetPath = "Assets/assets/Props/Spikes.png";
        private const string BouncySpikesAssetPath = "Assets/assets/Props/Bouncy Spikes.png";
        private const string EnemyAssetPath = "Assets/assets/Props/Enemy.png";
        private const string DoorAssetPath = "Assets/assets/Props/door_opening_hires_strip.png";

        private const string ParallaxBackPath = "Assets/assets/Background/Parallax/Back.png";
        private const string ParallaxMiddlePath = "Assets/assets/Background/Parallax/Middle.png";
        private const string ParallaxFrontPath = "Assets/assets/Background/Parallax/Front.png";
        private const string Cloud1Path = "Assets/assets/Background/cloud_1.png";
        private const string Cloud2Path = "Assets/assets/Background/cloud_2.png";
        private const string Cloud3Path = "Assets/assets/Background/cloud_3.png";
        private const string PaperTexturePath = "Assets/assets/Paper_Texture.png";

        [MenuItem("Tools/Build Complete Game Environment", priority = 50)]
        public static void BuildCompleteEnvironment()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Build Complete Game Environment");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Load Sprite Assets
            var platformSprite = LoadFirstSprite(PlatformAssetPath);
            var boardSprite = LoadFirstSprite(BoardAssetPath);
            var ladderSprite = LoadFirstSprite(LadderAssetPath);
            var chainSprite = LoadFirstSprite(ChainAssetPath);
            var spikesSprite = LoadFirstSprite(SpikesAssetPath);
            var doorSprite = LoadFirstSprite(DoorAssetPath);
            var backSprite = LoadFirstSprite(ParallaxBackPath);
            var midSprite = LoadFirstSprite(ParallaxMiddlePath);
            var frontSprite = LoadFirstSprite(ParallaxFrontPath);
            var cloud1Sprite = LoadFirstSprite(Cloud1Path);
            var cloud2Sprite = LoadFirstSprite(Cloud2Path);
            var cloud3Sprite = LoadFirstSprite(Cloud3Path);
            var paperSprite = LoadFirstSprite(PaperTexturePath);

            if (platformSprite == null || backSprite == null)
            {
                Debug.LogError("[EnvironmentBuilderTool] Required environment assets not found in Assets/assets/!");
                return;
            }

            // 2. Setup Clean Root Hierarchy
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                var oldEnv = GameObject.Find("ENV====");
                if (oldEnv != null)
                {
                    oldEnv.name = "Environment";
                    envRoot = oldEnv;
                }
                else
                {
                    envRoot = new GameObject("Environment");
                    Undo.RegisterCreatedObjectUndo(envRoot, "Create Environment");
                }
            }

            envRoot.transform.position = Vector3.zero;
            envRoot.transform.rotation = Quaternion.identity;
            envRoot.transform.localScale = Vector3.one;

            var bgContainer = GetOrCreateCategory(envRoot, "Background");
            var groundContainer = GetOrCreateCategory(envRoot, "Ground");
            var platformsContainer = GetOrCreateCategory(envRoot, "Platforms");
            var wallsContainer = GetOrCreateCategory(envRoot, "Walls");
            var laddersContainer = GetOrCreateCategory(envRoot, "Ladders");
            var structuresContainer = GetOrCreateCategory(envRoot, "Structures");
            var propsContainer = GetOrCreateCategory(envRoot, "Props");
            var decorationsContainer = GetOrCreateCategory(envRoot, "Decorations");
            var goalAreaContainer = GetOrCreateCategory(envRoot, "GoalArea");

            // Ensure Placed_Tools exists under Environment
            var placedTools = GameObject.Find("Placed_Tools");
            if (placedTools != null)
            {
                placedTools.transform.SetParent(propsContainer.transform, true);
            }
            else
            {
                placedTools = new GameObject("Placed_Tools");
                placedTools.transform.SetParent(propsContainer.transform, false);
                Undo.RegisterCreatedObjectUndo(placedTools, "Create Placed_Tools");
            }

            // 3. Assemble Background & Parallax
            SetupBackgroundLayers(bgContainer, paperSprite, backSprite, midSprite, frontSprite, cloud1Sprite, cloud2Sprite, cloud3Sprite);

            // 4. Convert and Organize Existing Scene Geometry
            OrganizeExistingSceneGeometry(groundContainer, platformsContainer, wallsContainer, propsContainer, goalAreaContainer, platformSprite, boardSprite);

            // 5. Upgrade Transmutable Walls in Levels 7 & 8
            UpgradeTransmutableWalls(platformsContainer, wallsContainer, platformSprite);

            // 6. Build Climbable Traversal Ladders
            BuildLadders(laddersContainer, ladderSprite);

            // 7. Add Structural Decorations (Hanging Chains and Planks)
            BuildDecorations(decorationsContainer, chainSprite, boardSprite);

            // 8. Setup Global 2D Lighting
            SetupLighting(envRoot);

            // 9. Move Puzzle Coordinator & Spawn Groups cleanly
            OrganizePuzzleAndSpawns(structuresContainer, platformsContainer, propsContainer);

            // 10. Clean up empty wrapper objects
            CleanupEmptyWrappers();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorApplication.RepaintHierarchyWindow();

            Debug.Log("[EnvironmentBuilderTool] Complete game environment successfully assembled across all levels!");
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject GetOrCreateCategory(GameObject parent, string name)
        {
            var child = parent.transform.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(go, $"Create Category {name}");
            return go;
        }

        private static void SetupBackgroundLayers(
            GameObject bgRoot,
            Sprite paperSprite,
            Sprite backSprite,
            Sprite midSprite,
            Sprite frontSprite,
            Sprite cloud1,
            Sprite cloud2,
            Sprite cloud3)
        {
            // Remove old placeholder Parallax_Layers if present
            var oldParallax = bgRoot.transform.parent.Find("Parallax_Layers");
            if (oldParallax != null)
            {
                Undo.DestroyObjectImmediate(oldParallax.gameObject);
            }

            // Clear previous background children to rebuild cleanly
            var existingChildren = new List<GameObject>();
            for (int i = 0; i < bgRoot.transform.childCount; i++)
            {
                existingChildren.Add(bgRoot.transform.GetChild(i).gameObject);
            }
            foreach (var go in existingChildren)
            {
                Undo.DestroyObjectImmediate(go);
            }

            const float worldSpanWidth = 1100f; // Covers X = -100 to X = 900
            const float centerX = 360f;

            // 1. Paper Backdrop Texture (1:1 uniform tiling, no vertical stretching)
            if (paperSprite != null)
            {
                var paperGO = new GameObject("Backdrop_Paper");
                paperGO.transform.SetParent(bgRoot.transform, false);
                paperGO.transform.position = new Vector3(centerX, 20f, 0f);

                var sr = paperGO.AddComponent<SpriteRenderer>();
                sr.sprite = paperSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(worldSpanWidth, 100f);
                paperGO.transform.localScale = Vector3.one;
                sr.sortingOrder = -100;
                sr.color = new Color(0.94f, 0.93f, 0.92f, 1.0f);
            }

            // 2. Parallax Back (Distant peaks - uniform scale, natural aspect ratio)
            if (backSprite != null)
            {
                var backGO = new GameObject("Parallax_Back");
                backGO.transform.SetParent(bgRoot.transform, false);
                backGO.transform.position = new Vector3(centerX, 2.5f, 0f);

                const float backScale = 2.4f;
                var sr = backGO.AddComponent<SpriteRenderer>();
                sr.sprite = backSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(worldSpanWidth / backScale, backSprite.rect.height / backSprite.pixelsPerUnit);
                backGO.transform.localScale = new Vector3(backScale, backScale, 1f);
                sr.sortingOrder = -80;
                sr.color = Color.white;

                var pl = backGO.AddComponent<ParallaxLayer>();
                SetParallaxField(pl, "parallaxEffectX", 0.04f);
                SetParallaxField(pl, "parallaxEffectY", 0.02f);
            }

            // 3. Parallax Middle (Midground hills - uniform scale)
            if (midSprite != null)
            {
                var midGO = new GameObject("Parallax_Middle");
                midGO.transform.SetParent(bgRoot.transform, false);
                midGO.transform.position = new Vector3(centerX, 0.0f, 0f);

                const float midScale = 2.0f;
                var sr = midGO.AddComponent<SpriteRenderer>();
                sr.sprite = midSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(worldSpanWidth / midScale, midSprite.rect.height / midSprite.pixelsPerUnit);
                midGO.transform.localScale = new Vector3(midScale, midScale, 1f);
                sr.sortingOrder = -60;
                sr.color = Color.white;

                var pl = midGO.AddComponent<ParallaxLayer>();
                SetParallaxField(pl, "parallaxEffectX", 0.10f);
                SetParallaxField(pl, "parallaxEffectY", 0.04f);
            }

            // 4. Parallax Front (Foreground contours - uniform scale)
            if (frontSprite != null)
            {
                var frontGO = new GameObject("Parallax_Front");
                frontGO.transform.SetParent(bgRoot.transform, false);
                frontGO.transform.position = new Vector3(centerX, -2.5f, 0f);

                const float frontScale = 1.8f;
                var sr = frontGO.AddComponent<SpriteRenderer>();
                sr.sprite = frontSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(worldSpanWidth / frontScale, frontSprite.rect.height / frontSprite.pixelsPerUnit);
                frontGO.transform.localScale = new Vector3(frontScale, frontScale, 1f);
                sr.sortingOrder = -40;
                sr.color = Color.white;

                var pl = frontGO.AddComponent<ParallaxLayer>();
                SetParallaxField(pl, "parallaxEffectX", 0.18f);
                SetParallaxField(pl, "parallaxEffectY", 0.06f);
            }

            // 5. Atmospheric Floating Clouds
            var cloudsRoot = new GameObject("Clouds");
            cloudsRoot.transform.SetParent(bgRoot.transform, false);
            var cloudSprites = new Sprite[] { cloud1, cloud2, cloud3 }.Where(s => s != null).ToArray();

            if (cloudSprites.Length > 0)
            {
                float[] cloudX = { -35f, 15f, 65f, 120f, 175f, 235f, 295f, 355f, 425f, 490f, 560f, 630f, 700f, 755f };
                float[] cloudY = { 22f, 26f, 20f, 28f, 23f, 27f, 21f, 29f, 24f, 27f, 22f, 28f, 23f, 26f };
                float[] cloudScale = { 1.2f, 0.9f, 1.4f, 1.1f, 0.85f, 1.3f, 1.0f, 1.2f, 0.95f, 1.35f, 1.1f, 0.9f, 1.25f, 1.0f };

                for (int i = 0; i < cloudX.Length; i++)
                {
                    var cGO = new GameObject($"Cloud_{i + 1}");
                    cGO.transform.SetParent(cloudsRoot.transform, false);
                    cGO.transform.position = new Vector3(cloudX[i], cloudY[i % cloudY.Length], 0f);
                    float s = cloudScale[i % cloudScale.Length];
                    cGO.transform.localScale = new Vector3(s, s, 1f);

                    var sr = cGO.AddComponent<SpriteRenderer>();
                    sr.sprite = cloudSprites[i % cloudSprites.Length];
                    sr.sortingOrder = -30;
                    sr.color = new Color(1f, 1f, 1f, 0.88f);
                }
            }
        }

        private static void OrganizeExistingSceneGeometry(
            GameObject groundContainer,
            GameObject platformsContainer,
            GameObject wallsContainer,
            GameObject propsContainer,
            GameObject goalAreaContainer,
            Sprite platformSprite,
            Sprite boardSprite)
        {
            var geoRoot = GameObject.Find("Level_03_Geometry");
            if (geoRoot == null) return;

            // 1. Process Start_Floor
            var startFloor = geoRoot.transform.Find("Start_Floor");
            if (startFloor != null)
            {
                // Body (bottom boundary floor)
                var bodyBottom = startFloor.Find("Body");
                if (bodyBottom != null)
                {
                    MigrateAndSlice(bodyBottom.gameObject, groundContainer, platformSprite, 0, "Ground_Base_Bedrock");
                }

                // Body (1) (top boundary ceiling)
                var bodyCeiling = startFloor.Find("Body (1)");
                if (bodyCeiling != null)
                {
                    MigrateAndSlice(bodyCeiling.gameObject, wallsContainer, platformSprite, 0, "Ceiling_Base_Bedrock");
                }
            }

            // 2. Process Mid_Floor (All 50 level body platforms, floors, and walls)
            var midFloor = geoRoot.transform.Find("Mid_Floor");
            if (midFloor != null)
            {
                var bodies = new List<GameObject>();
                for (int i = 0; i < midFloor.childCount; i++)
                {
                    bodies.Add(midFloor.GetChild(i).gameObject);
                }

                foreach (var go in bodies)
                {
                    var col = go.GetComponent<BoxCollider2D>();
                    var bounds = col != null ? col.bounds : new Bounds(go.transform.position, go.transform.localScale * 2f);

                    bool isWall = bounds.size.y > bounds.size.x * 1.5f;
                    bool isGround = bounds.center.y < -0.2f;

                    GameObject targetContainer = isWall ? wallsContainer : (isGround ? groundContainer : platformsContainer);
                    Sprite targetSprite = (bounds.size.y <= 1.0f && bounds.size.x <= 4.0f) ? boardSprite : platformSprite;
                    string namePrefix = isWall ? "Wall" : (isGround ? "Ground" : "Platform");

                    MigrateAndSlice(go, targetContainer, targetSprite, 0, $"{namePrefix}_{go.name}");
                }
            }

            // 3. Process End_Floor
            var endFloor = geoRoot.transform.Find("End_Floor");
            if (endFloor != null)
            {
                var endChildren = new List<GameObject>();
                for (int i = 0; i < endFloor.childCount; i++)
                {
                    endChildren.Add(endFloor.GetChild(i).gameObject);
                }

                foreach (var child in endChildren)
                {
                    if (child.name.ToLower().Contains("spike"))
                    {
                        // Spike hazard -> Props
                        child.transform.SetParent(propsContainer.transform, true);
                    }
                    else
                    {
                        // Body ground block -> Ground
                        MigrateAndSlice(child, groundContainer, platformSprite, 0, $"Ground_{child.name}");
                    }
                }
            }

            // 4. Process Goal Shrines
            var shrines = new List<GameObject>();
            for (int i = 0; i < geoRoot.transform.childCount; i++)
            {
                var child = geoRoot.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("Goal_Shrine"))
                {
                    shrines.Add(child);
                }
            }

            foreach (var shrine in shrines)
            {
                shrine.transform.SetParent(goalAreaContainer.transform, true);
                var sr = shrine.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 3;
                }
            }
        }

        private static void MigrateAndSlice(GameObject go, GameObject newParent, Sprite sprite, int sortingOrder, string newName)
        {
            var col = go.GetComponent<BoxCollider2D>();
            Bounds bounds = col != null ? col.bounds : new Bounds(go.transform.position, go.transform.localScale * 2f);

            // Set parent preserving world transform
            go.transform.SetParent(newParent.transform, true);
            go.name = newName;

            // Reset local rotation and scale to 1, exact world position to bounds center
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.transform.position = bounds.center;

            // Configure SpriteRenderer in Sliced DrawMode (no distortion, 1:1 crisp corners)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(bounds.size.x, bounds.size.y);
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white;
            if (newName == "Ceiling_Base_Bedrock")
            {
                sr.flipY = true;
            }

            // Re-align BoxCollider2D to 100% match bounds and sprite size
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = sr.size;
            }
        }

        private static void UpgradeTransmutableWalls(GameObject platformsContainer, GameObject wallsContainer, Sprite platformSprite)
        {
            var transmutables = Object.FindObjectsByType<TransmutableWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tw in transmutables)
            {
                var col = tw.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    Bounds bounds = col.bounds;
                    bool isWall = bounds.size.y > bounds.size.x * 1.5f;
                    GameObject targetContainer = isWall ? wallsContainer : platformsContainer;

                    tw.transform.SetParent(targetContainer.transform, true);
                    tw.transform.rotation = Quaternion.identity;
                    tw.transform.localScale = Vector3.one;
                    tw.transform.position = bounds.center;

                    var sr = tw.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = platformSprite;
                        sr.drawMode = SpriteDrawMode.Sliced;
                        sr.size = new Vector2(bounds.size.x, bounds.size.y);
                        sr.color = Color.white;
                    }

                    col.offset = Vector2.zero;
                    col.size = sr != null ? sr.size : (Vector2)bounds.size;
                }
            }
        }

        private static void BuildLadders(GameObject laddersContainer, Sprite ladderSprite)
        {
            // Clear existing ladders in container
            var existing = new List<GameObject>();
            for (int i = 0; i < laddersContainer.transform.childCount; i++)
            {
                existing.Add(laddersContainer.transform.GetChild(i).gameObject);
            }
            foreach (var go in existing) Undo.DestroyObjectImmediate(go);

            // Strategic ladder placements connecting tiers in exploration/platforming areas
            CreateLadder(laddersContainer, "Level1_Ascent_Ladder", new Vector2(-30.0f, -0.5f), 7.0f, ladderSprite);
            CreateLadder(laddersContainer, "Level2_Chasm_Ladder", new Vector2(68.0f, 0.4f), 3.4f, ladderSprite);
            CreateLadder(laddersContainer, "Level3_Inversion_Ladder", new Vector2(110.0f, 3.8f), 6.5f, ladderSprite);
        }

        private static void CreateLadder(GameObject parent, string name, Vector2 center, float height, Sprite ladderSprite)
        {
            var ladderGO = new GameObject(name);
            ladderGO.transform.SetParent(parent.transform, false);
            ladderGO.transform.position = new Vector3(center.x, center.y, 0f);

            var sr = ladderGO.AddComponent<SpriteRenderer>();
            sr.sprite = ladderSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(1.0f, height);
            sr.sortingOrder = 2; // In front of background and platforms, behind player
            sr.color = Color.white;

            var col = ladderGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.85f, height);
            col.offset = Vector2.zero;

            var climbZone = ladderGO.AddComponent<LadderClimbZone>();
            Undo.RegisterCreatedObjectUndo(ladderGO, $"Create Ladder {name}");
        }

        private static void BuildDecorations(GameObject decorContainer, Sprite chainSprite, Sprite boardSprite)
        {
            // Clear existing decorations in container
            var existing = new List<GameObject>();
            for (int i = 0; i < decorContainer.transform.childCount; i++)
            {
                existing.Add(decorContainer.transform.GetChild(i).gameObject);
            }
            foreach (var go in existing) Undo.DestroyObjectImmediate(go);

            // Hanging Chains dangling beneath floating platforms
            if (chainSprite != null)
            {
                float[] chainX = { -22f, -14f, 25f, 55f, 114f, 178f, 215f, 327f, 435f, 462f, 515f, 696f, 736f };
                float[] chainY = { -1.5f, -1.5f, -2.5f, -2.5f, 6.0f, 6.0f, 6.0f, 6.5f, 10.5f, 9.8f, 1.2f, 5.0f, 5.5f };
                float[] chainLen = { 3.5f, 3.5f, 3.0f, 3.0f, 4.0f, 4.0f, 4.0f, 3.5f, 3.5f, 3.5f, 3.0f, 4.0f, 3.5f };

                for (int i = 0; i < chainX.Length; i++)
                {
                    var chainGO = new GameObject($"HangingChain_{i + 1}");
                    chainGO.transform.SetParent(decorContainer.transform, false);
                    chainGO.transform.position = new Vector3(chainX[i], chainY[i], 0f);

                    var sr = chainGO.AddComponent<SpriteRenderer>();
                    sr.sprite = chainSprite;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(chainLen[i], 0.45f);
                    chainGO.transform.localRotation = Quaternion.Euler(0f, 0f, -90f); // Vertical hang
                    sr.sortingOrder = -5; // Behind platforms
                    sr.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                }
            }

            // Support Planks (under platform overhangs)
            if (boardSprite != null)
            {
                float[] plankX = { 3.3f, 43.7f, 314.1f, 449.0f, 721.2f };
                float[] plankY = { -4.5f, -0.6f, 7.3f, 7.2f, 0.2f };

                for (int i = 0; i < plankX.Length; i++)
                {
                    var plankGO = new GameObject($"SupportPlank_{i + 1}");
                    plankGO.transform.SetParent(decorContainer.transform, false);
                    plankGO.transform.position = new Vector3(plankX[i], plankY[i], 0f);

                    var sr = plankGO.AddComponent<SpriteRenderer>();
                    sr.sprite = boardSprite;
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(3.5f, 0.45f);
                    sr.sortingOrder = -2;
                    sr.color = new Color(0.9f, 0.85f, 0.8f, 1f);
                }
            }
        }

        private static void SetupLighting(GameObject envRoot)
        {
            var lightGO = GameObject.Find("Global Light 2D");
            if (lightGO == null)
            {
                lightGO = new GameObject("Global Light 2D");
                lightGO.transform.SetParent(envRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(lightGO, "Create Global Light 2D");
            }

            var light2D = lightGO.GetComponent<Light2D>();
            if (light2D == null) light2D = lightGO.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;
        }

        private static void OrganizePuzzleAndSpawns(GameObject structuresContainer, GameObject platformsContainer, GameObject propsContainer)
        {
            // Move Level 4 puzzle components
            var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject puzzleRoot = null;
            foreach (var go in allGos)
            {
                if (go.name == "Level_04_Puzzle" && go.scene.isLoaded) { puzzleRoot = go; break; }
            }

            if (puzzleRoot != null)
            {
                puzzleRoot.transform.SetParent(structuresContainer.transform, true);

                var platformSprite = LoadFirstSprite(PlatformAssetPath);
                var boardSprite = LoadFirstSprite(BoardAssetPath);

                var sw = puzzleRoot.transform.Find("Level4_Switch");
                if (sw != null)
                {
                    var sr = sw.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = boardSprite;
                        sr.drawMode = SpriteDrawMode.Sliced;
                        sr.color = new Color(1f, 0.8f, 0.4f, 1f);
                    }
                }

                var door = puzzleRoot.transform.Find("Level4_TimedDoor");
                if (door != null)
                {
                    var sr = door.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = platformSprite;
                        sr.drawMode = SpriteDrawMode.Sliced;
                    }
                }

                var plat = puzzleRoot.transform.Find("Level4_MovablePlatform");
                if (plat != null)
                {
                    var sr = plat.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = platformSprite;
                        sr.drawMode = SpriteDrawMode.Sliced;
                    }
                }
            }

            // Move Spikes from Level 7 spawn
            var lvl7Spawn = GameObject.Find("Level 7 spawn ") ?? GameObject.Find("Level 7 spawn");
            if (lvl7Spawn != null)
            {
                var children = new List<Transform>();
                for (int i = 0; i < lvl7Spawn.transform.childCount; i++) children.Add(lvl7Spawn.transform.GetChild(i));
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("spike") || child.name.ToLower().Contains("enemy") || child.name.ToLower().Contains("path"))
                    {
                        child.SetParent(propsContainer.transform, true);
                    }
                }
            }

            // Move Spikes and enemies from Level 8 spawn
            var lvl8Spawn = GameObject.Find("level 8 spawn ") ?? GameObject.Find("level 8 spawn");
            if (lvl8Spawn != null)
            {
                var children = new List<Transform>();
                for (int i = 0; i < lvl8Spawn.transform.childCount; i++) children.Add(lvl8Spawn.transform.GetChild(i));
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("spike") || child.name.ToLower().Contains("enemy") || child.name.ToLower().Contains("path"))
                    {
                        child.SetParent(propsContainer.transform, true);
                    }
                }
            }

            // Move Wind Spawners and Glass materials from Level 5 & 6
            var lvl5Spawn = GameObject.Find("Level 5 spawn");
            if (lvl5Spawn != null)
            {
                var children = new List<Transform>();
                for (int i = 0; i < lvl5Spawn.transform.childCount; i++) children.Add(lvl5Spawn.transform.GetChild(i));
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("wind")) child.SetParent(propsContainer.transform, true);
                    else if (child.name.ToLower().Contains("glass")) child.SetParent(platformsContainer.transform, true);
                }
            }

            var lvl6Spawn = GameObject.Find("Level 6 spawn");
            if (lvl6Spawn != null)
            {
                var children = new List<Transform>();
                for (int i = 0; i < lvl6Spawn.transform.childCount; i++) children.Add(lvl6Spawn.transform.GetChild(i));
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("wind")) child.SetParent(propsContainer.transform, true);
                    else if (child.name.ToLower().Contains("glass")) child.SetParent(platformsContainer.transform, true);
                }
            }
        }

        private static void CleanupEmptyWrappers()
        {
            var geo = GameObject.Find("Level_03_Geometry");
            if (geo != null)
            {
                var sf = geo.transform.Find("Start_Floor");
                if (sf != null && sf.childCount == 0) Undo.DestroyObjectImmediate(sf.gameObject);
                var mf = geo.transform.Find("Mid_Floor");
                if (mf != null && mf.childCount == 0) Undo.DestroyObjectImmediate(mf.gameObject);
                var ef = geo.transform.Find("End_Floor");
                if (ef != null && ef.childCount == 0) Undo.DestroyObjectImmediate(ef.gameObject);

                if (geo.transform.childCount == 0) Undo.DestroyObjectImmediate(geo);
            }

            var pz = GameObject.Find("Level_04_Puzzle");
            if (pz != null && pz.transform.childCount == 0) Undo.DestroyObjectImmediate(pz);

            var oldEnv = GameObject.Find("ENV====");
            if (oldEnv != null && oldEnv.transform.childCount == 0) Undo.DestroyObjectImmediate(oldEnv);
        }

        private static void SetParallaxField(ParallaxLayer layer, string fieldName, float value)
        {
            var field = typeof(ParallaxLayer).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(layer, value);
        }
    }
}
#endif
