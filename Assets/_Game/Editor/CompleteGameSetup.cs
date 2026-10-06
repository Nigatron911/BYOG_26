using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Bootstrap;
using Game.Data.Audio;
using Game.Gameplay.Combat;
using Game.Gameplay.Interaction;
using Game.Gameplay.Transmutation;
using Game.Presentation.Audio;
using Game.Presentation.Environment;
using Game.Presentation.UI;
using Project.Environment;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click, idempotent setup that turns the prototype scene into the finished game:
    /// imports the hand-drawn art and audio, builds the paper backdrop, reskins terrain / hazards /
    /// enemies / tools, wires audio + menus into the composition root and removes test leftovers.
    /// Menu: BYOG > Setup Complete Game.
    /// </summary>
    public static class CompleteGameSetup
    {
        private const string ArtDir = "Assets/_Game/Art/Paper/";
        private const string SfxDir = "Assets/SFX/SFX/";
        private const string AudioLibraryPath = "Assets/_Game/Data/Audio/GameAudioLibrary.asset";
        private const string PrefabDir = "Assets/_Game/Data/Prefabs/";
        private const string DoorStripPath = "Assets/assets/SpriteSheet/door_opening_strip.png";
        private const string DoorAnimDir = "Assets/_Game/Presentation/Animation/Door";
        /// <summary>Door frames are 315 px tall; 60 PPU makes the door ~5.2 units, a head taller than the player.</summary>
        private const float DoorPixelsPerUnit = 60f;

        private static readonly string[] TestLeftoverNames =
        {
            "TestLPM", "TestCam", "TestPS", "TestContainer", "TestSwitch", "TestSpike",
            "TestPlatform", "TestDoor", "TestCoord", "VisualAnimTest"
        };

        private struct SpriteSpec
        {
            public float ppu;
            public Vector4 border; // left, bottom, right, top (pixels)
            public bool fullRect;
            public SpriteSpec(float ppu, Vector4 border, bool fullRect) { this.ppu = ppu; this.border = border; this.fullRect = fullRect; }
        }

        private static readonly Dictionary<string, SpriteSpec> SpriteSpecs = new Dictionary<string, SpriteSpec>
        {
            { "Terrain_Platform", new SpriteSpec(250f, new Vector4(60, 88, 60, 232), true) },
            { "Tool_Platform", new SpriteSpec(650f, new Vector4(60, 80, 60, 230), true) },
            { "Tool_Board", new SpriteSpec(400f, new Vector4(45, 45, 45, 45), true) },
            { "Tool_Ladder", new SpriteSpec(400f, new Vector4(0, 45, 0, 45), true) },
            { "Hazard_Spikes", new SpriteSpec(352.5f, Vector4.zero, true) },
            { "Hazard_BouncySpikes", new SpriteSpec(285f, Vector4.zero, true) },
            { "Enemy_Ghost", new SpriteSpec(100f, Vector4.zero, false) },
            { "Tool_ChainLink", new SpriteSpec(500f, Vector4.zero, false) },
            { "BG_Paper", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Mountains_Back", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Mountains_Middle", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Mountains_Front", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Cloud_1", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Cloud_2", new SpriteSpec(100f, Vector4.zero, false) },
            { "BG_Cloud_3", new SpriteSpec(100f, Vector4.zero, false) },
            { "FX_Vignette", new SpriteSpec(100f, Vector4.zero, true) },
            { "UI_Paper", new SpriteSpec(100f, Vector4.zero, true) },
            { "UI_TornPaper", new SpriteSpec(100f, Vector4.zero, true) },
            { "UI_Character", new SpriteSpec(100f, Vector4.zero, false) },
        };

        public static readonly Color PaperColor = new Color(0.30f, 0.29f, 0.27f, 1f);
        /// <summary>Player transform scale chosen so the character reads at the same scale as terrain steps and tools.</summary>
        public const float PlayerScale = 1.5f;
        public static readonly Color SpikeTint = new Color(0.93f, 0.62f, 0.56f, 1f);

        [MenuItem("BYOG/Setup Complete Game")]
        public static void RunFromMenu()
        {
            var report = Run();
            Debug.Log(report);
            EditorUtility.DisplayDialog("BYOG Setup", "Setup finished. See the Console for the full report.", "OK");
        }

        public static string Run()
        {
            var log = new StringBuilder("[CompleteGameSetup]\n");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return log.Append("Exit Play Mode first.").ToString();
            }

            ConfigureSpriteImports(log);
            BuildHandwritingFont(log);
            ConfigureCoverAndCharacterArt(log);
            var doorController = BuildDoorAnimation(log);
            ConfigureAudioImports(log);
            var library = CreateOrUpdateAudioLibrary(log);
            ReskinPrefabs(log);

            var scene = SceneManager.GetActiveScene();
            RemoveTestLeftovers(scene, log);
            BuildBackdrop(log);
            ReskinScene(log);
            WidenEnemyWallGaps(log);
            AddGoalDoors(doorController, log);
            ConfigureLevelFraming(log);
            ConfigureLevelBriefings(log);
            ScalePlayer(log);
            WireBootstrap(library, log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            log.AppendLine($"Saved scene '{scene.path}'.");
            return log.ToString();
        }

        // ------------------------------------------------------------------ Imports

        private static void ConfigureSpriteImports(StringBuilder log)
        {
            int changed = 0;
            foreach (var kv in SpriteSpecs)
            {
                string path = ArtDir + kv.Key + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { log.AppendLine($"  ! Missing art: {path}"); continue; }

                var spec = kv.Value;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = spec.ppu;
                importer.spriteBorder = spec.border;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = spec.fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
                changed++;
            }
            log.AppendLine($"Sprite imports configured: {changed}");
        }

        private static void ConfigureAudioImports(StringBuilder log)
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxDir.TrimEnd('/') });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                bool isMusic = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == "bg";
                var s = importer.defaultSampleSettings;
                s.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = isMusic ? 0.6f : 0.8f;
                importer.defaultSampleSettings = s;
                importer.forceToMono = !isMusic;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
            }
            log.AppendLine($"Audio imports configured: {guids.Length}");
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
        private static AudioClip LoadClip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + relative);

        private static GameAudioLibrary CreateOrUpdateAudioLibrary(StringBuilder log)
        {
            var lib = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(AudioLibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<GameAudioLibrary>();
                AssetDatabase.CreateAsset(lib, AudioLibraryPath);
            }

            var so = new SerializedObject(lib);
            so.FindProperty("menuMusic").objectReferenceValue = LoadClip("1/bg.mp3");

            var tracks = so.FindProperty("levelMusic");
            var trackDefs = new (int from, int to, string clip, float vol)[]
            {
                (1, 2, "1/bg.mp3", 0.55f),   // Construction levels
                (3, 4, "2/BG.mp3", 0.55f),   // Gravity levels
                (5, 6, "3/BG.mp3", 0.55f),   // Material levels
                (7, 8, "4/BG.mp3", 0.55f),   // Transmutation levels
            };
            tracks.arraySize = trackDefs.Length;
            for (int i = 0; i < trackDefs.Length; i++)
            {
                var t = tracks.GetArrayElementAtIndex(i);
                t.FindPropertyRelative("fromLevel").intValue = trackDefs[i].from;
                t.FindPropertyRelative("toLevel").intValue = trackDefs[i].to;
                t.FindPropertyRelative("clip").objectReferenceValue = LoadClip(trackDefs[i].clip);
                t.FindPropertyRelative("volume").floatValue = trackDefs[i].vol;
            }

            so.FindProperty("jump").objectReferenceValue = LoadClip("jump.mp3");
            so.FindProperty("land").objectReferenceValue = LoadClip("Jump_landing.mp3");
            so.FindProperty("runningLoop").objectReferenceValue = LoadClip("Running.mp3");
            so.FindProperty("hurt").objectReferenceValue = LoadClip("Hurt.mp3");
            so.FindProperty("door").objectReferenceValue = LoadClip("Door sound.mp3");
            so.FindProperty("gravityShift").objectReferenceValue = LoadClip("2/gravity_shift.mp3");
            so.FindProperty("gravityStabilize").objectReferenceValue = LoadClip("2/gravity_stabilize.mp3");
            so.FindProperty("bounce").objectReferenceValue = LoadClip("3/bounce.mp3");
            so.FindProperty("stoneImpact").objectReferenceValue = LoadClip("3/stone fall.mp3");
            so.FindProperty("rewriteShot").objectReferenceValue = LoadClip("4/reality_rewrite_shot.mp3");
            so.FindProperty("rewriteExpire").objectReferenceValue = LoadClip("4/reality_rewrite_expire.mp3");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lib);

            int missing = 0;
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && it.name != "m_Script") missing++;
            }
            log.AppendLine($"Audio library ready at {AudioLibraryPath} (missing clips: {missing})");
            return lib;
        }

        // ------------------------------------------------------------------ Prefabs

        private static void ReskinPrefabs(StringBuilder log)
        {
            ReskinToolPrefab("PlankPrefab", LoadSprite("Tool_Board"), new Vector2(2.5f, 0.5f), log);
            ReskinToolPrefab("LadderPrefab", LoadSprite("Tool_Ladder"), new Vector2(0.75f, 2.5f), log);
            ReskinToolPrefab("PlatformPrefab", LoadSprite("Tool_Platform"), new Vector2(2f, 1f), log);

            string chainPath = PrefabDir + "ChainPrefab.prefab";
            var chainRoot = PrefabUtility.LoadPrefabContents(chainPath);
            if (chainRoot != null)
            {
                var chain = chainRoot.GetComponent<ChainTool>();
                if (chain != null)
                {
                    var so = new SerializedObject(chain);
                    so.FindProperty("linkSprite").objectReferenceValue = LoadSprite("Tool_ChainLink");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(chainRoot, chainPath);
                PrefabUtility.UnloadPrefabContents(chainRoot);
                log.AppendLine("  Chain prefab: link sprite set");
            }
        }

        private static void ReskinToolPrefab(string prefabName, Sprite sprite, Vector2 localSize, StringBuilder log)
        {
            string path = PrefabDir + prefabName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null || sprite == null) { log.AppendLine($"  ! Skipped {prefabName}"); return; }
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Vector3 keepScale = root.transform.localScale;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.sprite = sprite;
                root.transform.localScale = keepScale;
                sr.size = localSize;
                sr.color = Color.white;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            log.AppendLine($"  {prefabName}: reskinned with {sprite.name}");
        }

        // ------------------------------------------------------------------ Scene

        private static void RemoveTestLeftovers(Scene scene, StringBuilder log)
        {
            int removed = 0;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (TestLeftoverNames.Contains(go.name))
                {
                    Object.DestroyImmediate(go);
                    removed++;
                }
            }
            log.AppendLine($"Removed test leftovers: {removed}");
        }

        private static void BuildBackdrop(StringBuilder log)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = PaperColor;
            }

            var waterfalls = GameObject.Find("Mid_Waterfalls");
            if (waterfalls != null) waterfalls.SetActive(false);

            var env = GameObject.Find("ENV====");
            var existing = Object.FindFirstObjectByType<PaperBackdrop>(FindObjectsInactive.Include);
            GameObject go = existing != null ? existing.gameObject : new GameObject("Paper_Backdrop");
            if (env != null) go.transform.SetParent(env.transform, false);
            var backdrop = existing != null ? existing : go.AddComponent<PaperBackdrop>();

            var so = new SerializedObject(backdrop);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("paperSprite").objectReferenceValue = LoadSprite("BG_Paper");
            // Semi-transparent over the solid paper-coloured camera background: keeps the texture but lowers contrast.
            // Dimmed charcoal paper (matches the Figma direction and keeps the light-sketched player readable).
            so.FindProperty("paperTint").colorValue = new Color(0.6f, 0.58f, 0.55f, 1f);
            so.FindProperty("vignetteSprite").objectReferenceValue = LoadSprite("FX_Vignette");
            so.FindProperty("vignetteTint").colorValue = new Color(1f, 1f, 1f, 0.8f);
            so.FindProperty("focusInfluence").floatValue = 1f;

            var strips = so.FindProperty("strips");
            // Mountains stand on the shared world ground line (top of the main floor slab).
            float groundY = FindGroundLineY();
            var defs = new (string name, string sprite, float parallax, float height, float bottom, Color tint, int order)[]
            {
                ("Mountains_Back", "BG_Mountains_Back", 0.10f, 0.36f, groundY - 1.0f, new Color(0.8f, 0.8f, 0.8f, 0.55f), -90),
                ("Mountains_Middle", "BG_Mountains_Middle", 0.25f, 0.24f, groundY - 1.0f, new Color(0.85f, 0.85f, 0.85f, 0.75f), -85),
                ("Mountains_Front", "BG_Mountains_Front", 0.45f, 0.10f, groundY - 1.0f, new Color(0.9f, 0.9f, 0.9f, 0.95f), -80),
            };
            strips.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var e = strips.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = defs[i].name;
                e.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(defs[i].sprite);
                e.FindPropertyRelative("parallax").floatValue = defs[i].parallax;
                e.FindPropertyRelative("heightFraction").floatValue = defs[i].height;
                e.FindPropertyRelative("bottomFraction").floatValue = 0f;
                e.FindPropertyRelative("anchorToWorldY").boolValue = true;
                e.FindPropertyRelative("worldBottomY").floatValue = defs[i].bottom;
                e.FindPropertyRelative("tint").colorValue = defs[i].tint;
                e.FindPropertyRelative("sortingOrder").intValue = defs[i].order;
            }

            var clouds = so.FindProperty("clouds");
            var cloudSprites = clouds.FindPropertyRelative("sprites");
            cloudSprites.arraySize = 3;
            for (int i = 0; i < 3; i++) cloudSprites.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite($"BG_Cloud_{i + 1}");
            clouds.FindPropertyRelative("count").intValue = 6;
            clouds.FindPropertyRelative("heightFraction").floatValue = 0.07f;
            clouds.FindPropertyRelative("verticalBand").vector2Value = new Vector2(0.6f, 0.9f);
            clouds.FindPropertyRelative("driftScreensPerMinute").floatValue = 1.2f;
            clouds.FindPropertyRelative("parallax").floatValue = 0.05f;
            clouds.FindPropertyRelative("tint").colorValue = new Color(1f, 1f, 1f, 0.55f);
            clouds.FindPropertyRelative("sortingOrder").intValue = -95;
            so.ApplyModifiedPropertiesWithoutUndo();

            log.AppendLine($"Paper backdrop configured (paper sheet, 3 parallax mountain layers on ground y={groundY:F1}, clouds).");
        }

        private static float FindGroundLineY()
        {
            var floor = GameObject.Find("Start_Floor");
            float best = float.NegativeInfinity;
            if (floor != null)
            {
                foreach (var r in floor.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var b = r.bounds;
                    if (b.center.y < 0f && b.max.y > best) best = b.max.y;   // the floor slab, not the ceiling
                }
            }
            return float.IsNegativeInfinity(best) ? -4.4f : best;
        }

        private static void ReskinScene(StringBuilder log)
        {
            var terrainSprite = LoadSprite("Terrain_Platform");
            var spikeSprite = LoadSprite("Hazard_Spikes");
            var bouncySprite = LoadSprite("Hazard_BouncySpikes");
            var boardSprite = LoadSprite("Tool_Board");
            var ghostSprite = LoadSprite("Enemy_Ghost");

            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.gameObject.scene.IsValid())
                .ToList();

            bool IsPrototype(SpriteRenderer r) => r.sprite != null && r.sprite.name == "Prototype_Square";
            bool IsEnvironment(SpriteRenderer r) =>
                r.GetComponent<Game.Gameplay.Environment.Level4Switch>() == null &&
                r.GetComponent<Game.Gameplay.Environment.Level4TimedDoor>() == null &&
                r.GetComponent<Game.Gameplay.Environment.Level4MovablePlatform>() == null &&
                r.GetComponent<BoxCollider2D>() != null &&
                r.transform.childCount == 0;

            // Already-converted objects are recognised by their new sprites, so the setup is re-runnable.
            var spikes = renderers.Where(r => r.GetComponent<Spike>() != null && IsEnvironment(r) && (IsPrototype(r) || r.sprite == spikeSprite)).ToList();
            var fragile = renderers.Where(r => r.GetComponent<FragileFloor>() != null && IsEnvironment(r) && (IsPrototype(r) || r.sprite == boardSprite)).ToList();
            var terrain = renderers.Where(r => (IsPrototype(r) || r.sprite == terrainSprite) && IsEnvironment(r)
                                               && r.GetComponent<Spike>() == null && r.GetComponent<FragileFloor>() == null).ToList();

            int t = 0, s = 0, f = 0, e = 0;
            foreach (var r in terrain) { if (Normalize(r, terrainSprite, SpriteDrawMode.Tiled)) t++; }
            foreach (var r in fragile) { if (Normalize(r, boardSprite, SpriteDrawMode.Sliced)) f++; }

            var solidBounds = terrain.Select(r => r.bounds).ToList();
            foreach (var r in spikes)
            {
                if (!Normalize(r, spikeSprite, SpriteDrawMode.Tiled)) continue;
                s++;
                // Spikes hanging from a ceiling point downwards.
                var b = r.bounds;
                var above = new Vector2(b.center.x, b.max.y + 0.15f);
                var below = new Vector2(b.center.x, b.min.y - 0.15f);
                bool ceilingAbove = solidBounds.Any(sb => sb.Contains(new Vector3(above.x, above.y, sb.center.z)));
                bool floorBelow = solidBounds.Any(sb => sb.Contains(new Vector3(below.x, below.y, sb.center.z)));
                r.flipY = ceilingAbove && !floorBelow;
                // A warm red wash makes hazards read clearly against the paper.
                if (r.color == Color.white) r.color = SpikeTint;

                var ts = r.GetComponent<TransmutableSpike>();
                if (ts != null && bouncySprite != null)
                {
                    var so = new SerializedObject(ts);
                    so.FindProperty("trampolineSprite").objectReferenceValue = bouncySprite;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (var enemy in Object.FindObjectsByType<Level7PatrolEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (AddEnemyArt(enemy, ghostSprite)) e++;
            }

            log.AppendLine($"Reskinned: terrain {t}, spikes {s}, fragile floors {f}, enemies {e}.");
        }

        /// <summary>
        /// Converts a scaled/rotated prototype block into an unscaled, unrotated tiled/sliced sprite with
        /// an identical world footprint and an identical collider.
        /// </summary>
        private static bool Normalize(SpriteRenderer r, Sprite sprite, SpriteDrawMode mode)
        {
            if (sprite == null) return false;
            var tr = r.transform;
            var box = r.GetComponent<BoxCollider2D>();

            float z = Mathf.Repeat(tr.localEulerAngles.z, 360f);
            int quarter = Mathf.RoundToInt(z / 90f) % 4;
            if (Mathf.Abs(z - quarter * 90f) > 0.5f && Mathf.Abs(z - 360f) > 0.5f) return false; // non-axis-aligned: leave alone

            Vector3 ls = tr.localScale;
            Vector2 scale = new Vector2(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
            bool alreadyNormalized = r.drawMode != SpriteDrawMode.Simple && Mathf.Approximately(scale.x, 1f) && Mathf.Approximately(scale.y, 1f) && quarter == 0;

            Vector2 visualSize;
            if (alreadyNormalized)
            {
                visualSize = r.size;
            }
            else
            {
                Vector2 baseSize = r.drawMode == SpriteDrawMode.Simple
                    ? (r.sprite != null ? (Vector2)r.sprite.bounds.size : Vector2.one)
                    : r.size;
                visualSize = Vector2.Scale(baseSize, scale);
                Vector2 colSize = box != null ? Vector2.Scale(box.size, scale) : visualSize;
                Vector2 colOffset = box != null ? Vector2.Scale(box.offset, new Vector2(ls.x, ls.y)) : Vector2.zero;

                if (quarter % 2 == 1)
                {
                    visualSize = new Vector2(visualSize.y, visualSize.x);
                    colSize = new Vector2(colSize.y, colSize.x);
                }
                colOffset = Quaternion.Euler(0f, 0f, quarter * 90f) * colOffset;

                Undo.RecordObject(tr, "Normalize block");
                tr.localRotation = Quaternion.identity;
                tr.localScale = new Vector3(1f, 1f, 1f);
                if (box != null)
                {
                    Undo.RecordObject(box, "Normalize block");
                    box.autoTiling = false;
                    box.size = colSize;
                    box.offset = colOffset;
                }
            }

            // Note: switching a renderer from Simple to Sliced/Tiled makes Unity rescale the transform to
            // preserve the old look, so the scale is reset *after* the draw mode/sprite change.
            Undo.RecordObject(r, "Reskin block");
            r.drawMode = mode;
            r.sprite = sprite;
            r.tileMode = SpriteTileMode.Continuous;
            tr.localScale = Vector3.one;
            r.size = visualSize;
            r.flipX = false;
            return true;
        }

        private static bool AddEnemyArt(Level7PatrolEnemy enemy, Sprite ghost)
        {
            if (ghost == null) return false;
            var src = enemy.GetComponent<SpriteRenderer>();
            if (src == null) return false;

            var art = enemy.transform.Find("Art");
            if (art == null)
            {
                art = new GameObject("Art").transform;
                art.SetParent(enemy.transform, false);
            }

            var sr = GetOrAdd<SpriteRenderer>(art.gameObject);
            sr.sprite = ghost;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = 6;   // in front of terrain (3) so a creature on a block is fully visible

            var mirror = GetOrAdd<SpriteTintMirror>(art.gameObject);
            mirror.Configure(src);

            // Fit the ghost inside the gameplay footprint while keeping its proportions.
            Vector3 parentScale = enemy.transform.lossyScale;
            Vector2 footprint = new Vector2(Mathf.Abs(parentScale.x) * src.sprite.bounds.size.x, Mathf.Abs(parentScale.y) * src.sprite.bounds.size.y);
            Vector2 ghostSize = ghost.bounds.size;
            float worldH = footprint.y * 1.35f;
            float worldW = worldH * ghostSize.x / ghostSize.y;
            if (worldW > footprint.x * 1.1f) { worldW = footprint.x * 1.1f; worldH = worldW * ghostSize.y / ghostSize.x; }
            art.localPosition = new Vector3(0f, (worldH - footprint.y) * 0.5f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)) * 0.6f, 0f);
            art.localScale = new Vector3(worldW / (ghostSize.x * Mathf.Abs(parentScale.x)), worldH / (ghostSize.y * Mathf.Abs(parentScale.y)), 1f);

            src.enabled = false;
            EditorUtility.SetDirty(src);
            EditorUtility.SetDirty(art.gameObject);
            return true;
        }

        private static Sprite[] LoadDoorFrames()
        {
            return AssetDatabase.LoadAllAssetsAtPath(DoorStripPath).OfType<Sprite>().OrderBy(sp => sp.rect.x).ToArray();
        }

        /// <summary>Imports the door strip (bottom-centre pivot) and builds its clips + Animator controller.</summary>
        private static AnimatorController BuildDoorAnimation(StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(DoorStripPath) as TextureImporter;
            if (importer == null) { log.AppendLine("  ! Door strip missing: " + DoorStripPath); return null; }

            importer.spritePixelsPerUnit = DoorPixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            foreach (var r in rects)
            {
                r.alignment = SpriteAlignment.BottomCenter;
                r.pivot = new Vector2(0.5f, 0f);
            }
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();

            var frames = LoadDoorFrames();
            if (frames.Length < 5) { log.AppendLine($"  ! Door strip has {frames.Length} frames, expected 5"); return null; }

            if (!AssetDatabase.IsValidFolder(DoorAnimDir))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Presentation/Animation", "Door");
            }

            // Closed: hold frame 0. Ajar: creak to frame 1 and hold. Opening: swing 1 -> 4. Opened: hold frame 4.
            var closed = SaveSpriteClip("Door_Closed", new[] { (0f, frames[0]) }, true);
            var ajar = SaveSpriteClip("Door_Ajar", new[] { (0f, frames[0]), (0.12f, frames[1]) }, false);
            var opening = SaveSpriteClip("Door_Opening", new[] { (0f, frames[1]), (0.08f, frames[2]), (0.17f, frames[3]), (0.28f, frames[4]), (0.36f, frames[4]) }, false);
            var opened = SaveSpriteClip("Door_Opened", new[] { (0f, frames[4]) }, true);

            string controllerPath = DoorAnimDir + "/Door.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Near", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var sClosed = sm.AddState(GoalDoorAnimator.StateClosed); sClosed.motion = closed;
            var sAjar = sm.AddState("Ajar"); sAjar.motion = ajar;
            var sOpening = sm.AddState(GoalDoorAnimator.StateOpening); sOpening.motion = opening;
            var sOpened = sm.AddState("Opened"); sOpened.motion = opened;
            sm.defaultState = sClosed;

            var toAjar = sClosed.AddTransition(sAjar);
            toAjar.hasExitTime = false; toAjar.duration = 0f; toAjar.AddCondition(AnimatorConditionMode.If, 0f, "Near");
            var toClosed = sAjar.AddTransition(sClosed);
            toClosed.hasExitTime = false; toClosed.duration = 0f; toClosed.AddCondition(AnimatorConditionMode.IfNot, 0f, "Near");
            var toOpened = sOpening.AddTransition(sOpened);
            toOpened.hasExitTime = true; toOpened.exitTime = 1f; toOpened.duration = 0f;

            AssetDatabase.SaveAssets();
            log.AppendLine($"Door animation built: 4 clips + controller ({frames.Length} frames, {DoorPixelsPerUnit} PPU).");
            return controller;
        }

        private static AnimationClip SaveSpriteClip(string clipName, (float time, Sprite sprite)[] keys, bool loop)
        {
            string path = DoorAnimDir + "/" + clipName + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName, frameRate = 30f };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keyframes = keys.Select(k => new ObjectReferenceKeyframe { time = k.time, value = k.sprite }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddGoalDoors(AnimatorController controller, StringBuilder log)
        {
            var frames = LoadDoorFrames();
            if (frames.Length == 0) { log.AppendLine("  ! Door frames missing"); return; }

            int count = 0;
            foreach (var goal in Object.FindObjectsByType<LevelGoal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var shrine = goal.GetComponent<SpriteRenderer>();
                if (shrine == null) continue;

                var art = goal.transform.Find("DoorArt");
                if (art == null)
                {
                    art = new GameObject("DoorArt").transform;
                    art.SetParent(goal.transform, false);
                }

                // The shrine sprite is replaced by the door, but its trigger collider stays exactly as it was.
                Bounds footprint = shrine.sprite != null
                    ? new Bounds(shrine.transform.TransformPoint(shrine.sprite.bounds.center), Vector3.Scale(shrine.sprite.bounds.size, shrine.transform.lossyScale))
                    : new Bounds(goal.transform.position, Vector3.one * 2f);
                shrine.enabled = false;

                Vector3 ps = goal.transform.lossyScale;
                art.localScale = new Vector3(1f / Mathf.Abs(ps.x), 1f / Mathf.Abs(ps.y), 1f);
                art.rotation = Quaternion.identity;
                // Pivot is at the door's base: stand it on the ground below the goal.
                art.position = new Vector3(footprint.center.x, FindGroundBelow(footprint.center, footprint.min.y), goal.transform.position.z + 0.01f);

                var sr = GetOrAdd<SpriteRenderer>(art.gameObject);
                sr.sprite = frames[0];
                sr.color = Color.white;
                sr.sortingLayerID = shrine.sortingLayerID;
                sr.sortingOrder = 2; // behind the player (8)

                var animator = GetOrAdd<Animator>(art.gameObject);
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;

                var anim = GetOrAdd<GoalDoorAnimator>(art.gameObject);
                anim.Configure(goal, animator, frames);
                EditorUtility.SetDirty(anim);
                count++;
            }
            log.AppendLine($"Goal doors added: {count}");
        }

        /// <summary>Top of the first solid, non-trigger collider below a point (falls back to the given y).</summary>
        private static float FindGroundBelow(Vector3 from, float fallbackY)
        {
            Physics2D.SyncTransforms();
            foreach (var hit in Physics2D.RaycastAll(from, Vector2.down, 20f))
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                return hit.point.y;
            }
            return fallbackY;
        }

        /// <summary>
        /// Gives every level its camera bounds (spawn -> exit, ground -> ceiling) so the camera never shows
        /// neighbouring levels, and marks which levels follow the player (all but the two planning levels).
        /// </summary>
        private static void ConfigureLevelFraming(StringBuilder log)
        {
            var lpm = Object.FindObjectsByType<Game.Gameplay.LevelProgressionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(l => l.GetComponent<GameBootstrap>() != null);
            if (lpm == null) lpm = Object.FindFirstObjectByType<Game.Gameplay.LevelProgressionManager>(FindObjectsInactive.Include);
            if (lpm == null) { log.AppendLine("  ! LevelProgressionManager not found"); return; }
            lpm.ReinitializeLevels();

            float groundY = FindGroundLineY();
            float ceilingY = FindCeilingLineY();
            var door4 = Object.FindFirstObjectByType<Game.Gameplay.Environment.Level4TimedDoor>(FindObjectsInactive.Include);

            var so = new SerializedObject(lpm);
            var levels = so.FindProperty("levels");
            var sb = new StringBuilder();
            float ExitX(Game.Gameplay.LevelProgressionManager.LevelConfig c) =>
                c.goalShrine != null ? c.goalShrine.transform.position.x
                    : (door4 != null ? door4.transform.position.x : c.spawnPosition.x + 80f);

            for (int i = 0; i < levels.arraySize; i++)
            {
                var cfg = lpm.Levels[i];
                float exitX = ExitX(cfg);
                float xMin = cfg.spawnPosition.x - 5f;
                float xMax = exitX + 5f;
                // Keep the previous level's exit door and the next level's start out of view.
                if (i > 0) xMin = Mathf.Max(xMin, ExitX(lpm.Levels[i - 1]) + 3f);
                if (i + 1 < levels.arraySize) xMax = Mathf.Min(xMax, lpm.Levels[i + 1].spawnPosition.x - 3f);
                // Planning levels show extra floor so the build toolbar never covers the ground line.
                float yMin = groundY - (i >= 2 ? 3.5f : 6.5f);
                float yMax = ceilingY + 2f;
                var rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

                var e = levels.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("cameraBounds").rectValue = rect;
                e.FindPropertyRelative("followCamera").boolValue = i >= 2;
                sb.Append($" L{i + 1}[{xMin:F0}..{xMax:F0}]{(i >= 2 ? "follow" : "static")}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"Level framing (y {groundY - 3.5f:F1}..{ceilingY + 2f:F1}):" + sb);
        }

        private static float FindCeilingLineY()
        {
            var floor = GameObject.Find("Start_Floor");
            float best = float.PositiveInfinity;
            if (floor != null)
            {
                foreach (var r in floor.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var b = r.bounds;
                    if (b.center.y > 0f && b.min.y < best) best = b.min.y;   // the ceiling slab
                }
            }
            return float.IsPositiveInfinity(best) ? 23.6f : best;
        }

        // ------------------------------------------------------------------ Fonts, cover, character art

        private const string FontPath = "Assets/_Game/Art/Fonts/PatrickHand-Regular.ttf";
        private const string FontAssetPath = "Assets/_Game/Art/Fonts/PatrickHand SDF.asset";
        private const string CoverPath = "Assets/assets/Background/cover.png";

        /// <summary>Builds the UI Toolkit font asset for the handwriting font (once).</summary>
        private static void BuildHandwritingFont(StringBuilder log)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(FontAssetPath) != null)
            {
                log.AppendLine("Handwriting font asset present.");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) { log.AppendLine("  ! Font missing: " + FontPath); return; }

            var fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                UnityEngine.TextCore.Text.AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
            {
                fa.atlasTextures[0].name = "PatrickHand Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }
            if (fa.material != null)
            {
                fa.material.name = "PatrickHand Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);
            log.AppendLine("Handwriting font asset created: " + FontAssetPath);
        }

        private static void ConfigureCoverAndCharacterArt(StringBuilder log)
        {
            if (AssetImporter.GetAtPath(CoverPath) is TextureImporter cover)
            {
                cover.textureType = TextureImporterType.Sprite;
                cover.spriteImportMode = SpriteImportMode.Single;
                cover.mipmapEnabled = false;
                cover.maxTextureSize = 2048;
                cover.textureCompression = TextureImporterCompression.CompressedHQ;
                cover.SaveAndReimport();
            }

            // The run and jump strips are drawn smaller than the idle strip (character ~210 px / ~253 px tall
            // vs 274 px). Matching pixels-per-unit keeps the character the same size in every animation;
            // the feet pivot stays put, so nothing jumps when the animation changes.
            SetStripPpu("Assets/assets/SpriteSheet/character_run_strip.png", 100f * 210f / 274f);
            SetStripPpu("Assets/assets/SpriteSheet/character_jump_strip.png", 100f * 253f / 274f);
            log.AppendLine("Cover art and character strip scales configured.");
        }

        private static void SetStripPpu(string path, float ppu)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) < 0.01f) return;
            importer.spritePixelsPerUnit = ppu;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ Level content

        private struct BriefingText
        {
            public string title, goal, how, catchText;
            public float formSeconds;
        }

        private static readonly BriefingText[] Briefings =
        {
            new BriefingText {
                title = "FADING INK",
                goal = "Help your character reach the exit door.",
                how = "The game starts paused. Drag planks, ladders and platforms from the toolbar into the level to build a safe path over the spikes and gaps. R rotates, right-click cancels.",
                catchText = "When you press Simulate (or Space) your character walks on their own - you can't stop them. Every tool you placed vanishes 6.5 seconds after you hit Simulate, so build your path carefully."
            },
            new BriefingText {
                title = "LOOSE THREADS",
                goal = "Cross the Suspension Chasm and reach the exit door.",
                how = "Same rules, more tools - and a chain. Pick the chain, then click where it starts and where it ends to hang it across a gap.",
                catchText = "The chasm is wider and the spikes run longer. Your tools last 8 seconds once the simulation starts - plan every second."
            },
            new BriefingText {
                title = "UPSIDE DOWN",
                goal = "Make it across the chasm to the exit door.",
                how = "You're in control now. A / D or the arrow keys to move, W or Space to jump.",
                catchText = "Gravity won't sit still. Every few seconds it swaps between Earth and Moon - on the Moon you float, so a jump that's safe now can carry you into the spikes a moment later. Time it."
            },
            new BriefingText {
                title = "THE CHAMBER",
                goal = "Open the chamber door and get through before it shuts.",
                how = "Step on the switch to raise the spikes, step off to bring out the platform, then touch the platform to open the door.",
                catchText = "Gravity now changes at random - sometimes it flips you onto the ceiling with your controls reversed. The door only stays open for 4 seconds."
            },
            new BriefingText {
                title = "PAPER, STONE, RUBBER",
                goal = "Reach the exit door.",
                how = "Change what you're made of: 1 Paper, 2 Stone, 3 Rubber (Q / E to cycle). Paper is light and rides the wind. Stone is heavy, ignores wind and cracks glass floors. Rubber bounces higher with every landing.",
                catchText = "Only 5 changes per attempt, and Stone and Rubber fade back to Paper after 6 seconds - watch the bar over your head.",
                formSeconds = 6f
            },
            new BriefingText {
                title = "AERIAL TRAVERSE",
                goal = "Cross the open sky to the exit door.",
                how = "1 Paper, 2 Stone, 3 Rubber (Q / E to cycle). Ride the wind as paper, drop through it as stone, bounce your way up as rubber.",
                catchText = "Longer gaps, stronger winds, still only 5 changes - and every form now lasts just 5 seconds.",
                formSeconds = 5f
            },
            new BriefingText {
                title = "REWRITE",
                goal = "Reach the exit door.",
                how = "Click an object to transmute it: walls become passable, spikes turn into trampolines and creatures become platforms you can stand on. Every click makes all changeable objects flash.",
                catchText = "Each change plunges the world into darkness for a moment, and only lasts 4 seconds before it snaps back."
            },
            new BriefingText {
                title = "THE LAST PAGE",
                goal = "Survive the final trial and walk through the last door.",
                how = "Everything you've learned: transmute walls, spikes and creatures with a click and chain them together to climb.",
                catchText = "More creatures, tighter gaps, and every change still lasts only 4 seconds. One wrong click and the path falls apart."
            },
        };

        private static void ConfigureLevelBriefings(StringBuilder log)
        {
            var lpm = Object.FindObjectsByType<Game.Gameplay.LevelProgressionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(l => l.GetComponent<GameBootstrap>() != null);
            if (lpm == null) { log.AppendLine("  ! LevelProgressionManager not found"); return; }

            var so = new SerializedObject(lpm);
            var levels = so.FindProperty("levels");
            for (int i = 0; i < levels.arraySize && i < Briefings.Length; i++)
            {
                var e = levels.GetArrayElementAtIndex(i);
                var b = Briefings[i];
                // Only fill empty cards, so text edited in the inspector survives re-running the setup.
                if (!string.IsNullOrEmpty(e.FindPropertyRelative("briefingTitle").stringValue)) continue;
                e.FindPropertyRelative("briefingTitle").stringValue = b.title;
                e.FindPropertyRelative("briefingGoal").stringValue = b.goal;
                e.FindPropertyRelative("briefingHowToPlay").stringValue = b.how;
                e.FindPropertyRelative("briefingCatch").stringValue = b.catchText;
                e.FindPropertyRelative("materialFormSeconds").floatValue = b.formSeconds;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"Level briefings written for {Mathf.Min(levels.arraySize, Briefings.Length)} levels (material forms: 3.A 6s, 3.B 5s).");
        }

        /// <summary>
        /// Keeps a clear gap between each patrolling creature and the walls at its patrol ends, so the
        /// player can always fit between them. Only patrol end points move; walls and props stay where
        /// they were designed. Re-runnable: ends that already have the gap are untouched.
        /// </summary>
        private static void WidenEnemyWallGaps(StringBuilder log)
        {
            const float requiredGap = 3.2f;
            const float minPatrol = 2.5f;
            var sb = new StringBuilder();
            Physics2D.SyncTransforms();

            foreach (var enemy in Object.FindObjectsByType<Level7PatrolEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(enemy);
                var p1 = so.FindProperty("path1")?.objectReferenceValue as Transform;
                var p2 = so.FindProperty("path2")?.objectReferenceValue as Transform;
                if (p1 == null || p2 == null) continue;

                var art = enemy.transform.Find("Art");
                var artRenderer = art != null ? art.GetComponent<SpriteRenderer>() : null;
                float halfWidth = artRenderer != null ? artRenderer.bounds.extents.x : 1.5f;
                float y = enemy.transform.position.y;

                bool changed = false;
                foreach (var (end, other) in new[] { (p1, p2), (p2, p1) })
                {
                    float dir = Mathf.Sign(end.position.x - other.position.x);
                    if (dir == 0f) continue;
                    float mid = (end.position.x + other.position.x) * 0.5f;
                    float? wallX = null;
                    foreach (var hit in Physics2D.RaycastAll(new Vector2(mid, y), new Vector2(dir, 0f), 40f).OrderBy(h => h.distance))
                    {
                        var c = hit.collider;
                        if (c == null || c.isTrigger || c.transform.IsChildOf(enemy.transform)) continue;
                        if (c.GetComponentInParent<Game.Gameplay.Player.AutonomousPlayerController>() != null) continue;
                        wallX = hit.point.x;
                        break;
                    }
                    if (wallX == null) continue;

                    float gap = Mathf.Abs(wallX.Value - end.position.x) - halfWidth;
                    if (gap >= requiredGap - 0.05f) continue;   // tolerance keeps re-runs stable

                    float newX = wallX.Value - dir * (halfWidth + requiredGap);
                    if (!HasGroundBelow(new Vector2(newX, y))) { sb.Append($" {enemy.name}: no ground for new end;"); continue; }

                    Undo.RecordObject(end, "Widen enemy gap");
                    end.position = new Vector3(newX, end.position.y, end.position.z);
                    if (Mathf.Abs(end.position.x - other.position.x) < minPatrol)
                    {
                        float otherX = newX - dir * minPatrol;
                        if (HasGroundBelow(new Vector2(otherX, y)))
                        {
                            Undo.RecordObject(other, "Widen enemy gap");
                            other.position = new Vector3(otherX, other.position.y, other.position.z);
                        }
                    }
                    if (Mathf.Sign(enemy.transform.position.x - newX) == dir)
                    {
                        // Start the creature inside its new patrol range.
                        Undo.RecordObject(enemy.transform, "Widen enemy gap");
                        var children = enemy.transform.Cast<Transform>().Select(t => (t, t.position)).ToList();
                        enemy.transform.position = new Vector3((end.position.x + other.position.x) * 0.5f, enemy.transform.position.y, enemy.transform.position.z);
                        foreach (var (t, pos) in children) t.position = pos;   // keep path points in place
                    }
                    sb.Append($" {enemy.name}: gap {gap:F1}->{requiredGap:F1};");
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(enemy);
            }
            log.AppendLine("Enemy/wall gaps:" + (sb.Length > 0 ? sb.ToString() : " all clear"));
        }

        private static bool HasGroundBelow(Vector2 point)
        {
            foreach (var hit in Physics2D.RaycastAll(point, Vector2.down, 12f))
            {
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.GetComponent<Level7PatrolEnemy>() == null) return true;
            }
            return false;
        }

        private static void ScalePlayer(StringBuilder log)
        {
            var mainCam = Camera.main;
            if (mainCam != null) GetOrAdd<Game.Presentation.CameraSystems.GameplayCameraFollow>(mainCam.gameObject);

            var player = Object.FindFirstObjectByType<Game.Gameplay.Player.AutonomousPlayerController>(FindObjectsInactive.Include);
            if (player == null) { log.AppendLine("  ! Player not found"); return; }
            var scale = new Vector3(PlayerScale, PlayerScale, 1f);
            player.transform.localScale = scale;
            var so = new SerializedObject(player);
            var prop = so.FindProperty("baseTransformScale");
            if (prop != null) prop.vector3Value = scale;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"Player scale set to {PlayerScale}.");
        }

        private static void WireBootstrap(GameAudioLibrary library, StringBuilder log)
        {
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null) { log.AppendLine("  ! GameBootstrap not found"); return; }

            var uiDoc = Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Include);
            GameObject uiHost = uiDoc != null ? uiDoc.gameObject : bootstrap.gameObject;

            var so = new SerializedObject(bootstrap);
            so.FindProperty("audioLibrary").objectReferenceValue = library;
            so.FindProperty("mainMenuUI").objectReferenceValue = GetOrAdd<MainMenuUI>(uiHost);
            so.FindProperty("pauseMenuUI").objectReferenceValue = GetOrAdd<PauseMenuUI>(uiHost);
            so.FindProperty("victoryUI").objectReferenceValue = GetOrAdd<VictoryUI>(uiHost);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(uiHost);   // e.g. the retired level banner
            so.FindProperty("briefingUI").objectReferenceValue = GetOrAdd<LevelBriefingUI>(uiHost);
            so.FindProperty("creditsUI").objectReferenceValue = GetOrAdd<CreditsUI>(uiHost);
            so.FindProperty("audioController").objectReferenceValue = GetOrAdd<GameAudioController>(bootstrap.gameObject);
            so.FindProperty("backdrop").objectReferenceValue = Object.FindFirstObjectByType<PaperBackdrop>(FindObjectsInactive.Include);
            so.FindProperty("showMainMenuOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("Bootstrap wired: audio library, audio controller, menus, banner, backdrop.");
        }

        private static T GetOrAdd<T>(GameObject host) where T : Component
        {
            var c = host.GetComponent<T>();
            return c != null ? c : host.AddComponent<T>();
        }
    }
}
