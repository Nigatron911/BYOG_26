using System.IO;
using UnityEngine;
using UnityEditor;
using Game.Core.Bootstrap;
using Game.Gameplay.Player;

namespace Game.Editor
{
    public static class Level3BuilderTool
    {
        [MenuItem("Game/Build Simple Level 3", false, 11)]
        public static void BuildSimpleLevel()
        {
            var envRoot = GameObject.Find("ENV====");
            if (envRoot == null) envRoot = new GameObject("ENV====");

            var existingGeom1 = envRoot.transform.Find("Level_01_Geometry");
            if (existingGeom1 != null) GameObject.DestroyImmediate(existingGeom1.gameObject);
            var existingGeom2 = envRoot.transform.Find("Level_02_Geometry");
            if (existingGeom2 != null) GameObject.DestroyImmediate(existingGeom2.gameObject);
            var existingGeom3 = envRoot.transform.Find("Level_03_Geometry");
            if (existingGeom3 != null) GameObject.DestroyImmediate(existingGeom3.gameObject);

            var levelGO = new GameObject("Level_03_Geometry");
            levelGO.transform.SetParent(envRoot.transform, false);

            var terrainMat = new PhysicsMaterial2D("TerrainPhysMat") { friction = 0.65f, bounciness = 0f };
            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_Square.png");
            var ledgeMossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_LedgeMoss.png");
            var goalShrineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Sprites/Prototype_GoalShrine.png");

            // Lowered terrain: top surface at y = -4 (was -1.5)
            // Left ground (Start to Small Hole) -> (-11 to -1) -> x = -6, width = 10, y = -6.5, height = 5
            CreatePlatformBlock(levelGO.transform, "Start_Floor", squareSprite, ledgeMossSprite, new Vector2(-6.0f, -6.5f), new Vector2(10.0f, 5.0f), terrainMat);

            // Small Hole: x = -1 to 1 (Width = 2)

            // Middle ground (Small Hole to Big Hole) -> (1 to 5) -> x = 3, width = 4
            CreatePlatformBlock(levelGO.transform, "Mid_Floor", squareSprite, ledgeMossSprite, new Vector2(3.0f, -6.5f), new Vector2(4.0f, 5.0f), terrainMat);

            // Big Hole: x = 5 to 11 (Width = 6)

            // Right ground (Big Hole to End) -> (11 to 21) -> x = 16, width = 10
            CreatePlatformBlock(levelGO.transform, "End_Floor", squareSprite, ledgeMossSprite, new Vector2(16.0f, -6.5f), new Vector2(10.0f, 5.0f), terrainMat);

            // Goal Shrine
            var goalGO = new GameObject("Goal_Shrine");
            goalGO.transform.SetParent(levelGO.transform, false);
            goalGO.transform.position = new Vector3(18.0f, -2.8f, 0f);
            goalGO.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var goalSR = goalGO.AddComponent<SpriteRenderer>();
            goalSR.sprite = goalShrineSprite;
            goalSR.sortingOrder = 7;
            var goalCol = goalGO.AddComponent<BoxCollider2D>();
            goalCol.size = new Vector2(1.6f, 2.4f);
            goalCol.isTrigger = true;
            goalGO.AddComponent<Game.Gameplay.Combat.LevelGoal>();

            // Pit Hazards
            CreatePitHazard(levelGO.transform, "Pit_Small", new Vector2(0f, -4.5f), 2.1f);
            CreatePitHazard(levelGO.transform, "Pit_Big", new Vector2(8f, -4.5f), 6.1f);

            // Player Pos: (-9, -3)
            var controllerRoot = GameObject.Find("Controller====");
            if (controllerRoot != null) {
                var playerGO = controllerRoot.transform.Find("Player");
                if (playerGO != null) playerGO.position = new Vector3(-9.0f, -3.3f, 0f);
            }

            // Camera bounds (simple adjustment)
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(4.0f, -1.5f, -10f);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static GameObject CreatePlatformBlock(Transform parent, string name, Sprite bodySprite, Sprite capSprite, Vector2 position, Vector2 size, PhysicsMaterial2D mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(go.transform, false);
            var bodySR = bodyGO.AddComponent<SpriteRenderer>();
            bodySR.sprite = bodySprite;
            bodySR.color = Color.white;
            bodySR.sortingOrder = 3;
            bodyGO.transform.localScale = new Vector3(size.x / 2.0f, size.y / 2.0f, 1f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = mat;
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

        private static void CreatePitHazard(Transform parent, string name, Vector2 position, float width)
        {
            var pitHazardGO = new GameObject(name);
            pitHazardGO.transform.SetParent(parent, false);
            pitHazardGO.transform.position = new Vector3(position.x, position.y, 0f);
            var pitCol = pitHazardGO.AddComponent<BoxCollider2D>();
            pitCol.size = new Vector2(width, 0.5f);
            pitCol.isTrigger = true;
            var pitHazard = pitHazardGO.AddComponent<Game.Gameplay.Combat.Hazard2D>();
            var field = pitHazard.GetType().GetField("hazardName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(pitHazard, "Pit Hazard");
        }
    }
}
