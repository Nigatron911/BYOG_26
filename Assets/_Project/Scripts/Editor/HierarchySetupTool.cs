#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor
{
    public static class HierarchySetupTool
    {
        private const string CoreHeaderName = "Core====";
        private const string ControllerHeaderName = "Controller====";
        private const string UIHeaderName = "UI====";
        private const string ENVHeaderName = "ENV====";

        // vHierarchy Palette Color Indices
        private const int ColorRed = 2;
        private const int ColorYellow = 3;
        private const int ColorGreen = 4;
        private const int ColorTeal = 6;

        [MenuItem("Tools/Setup Scene Hierarchy", priority = 100)]
        [MenuItem("GameObject/Setup Hierarchy Headers", priority = 20)]
        public static void SetupHierarchy()
        {
            var scene = SceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Setup Scene Hierarchy Headers");
            int undoGroup = Undo.GetCurrentGroup();

            var core = GetOrCreateHeader(CoreHeaderName, 0);
            var controller = GetOrCreateHeader(ControllerHeaderName, 1);
            var ui = GetOrCreateHeader(UIHeaderName, 2);
            var env = GetOrCreateHeader(ENVHeaderName, 3);

            // Reorganize existing objects
            OrganizeExistingObjects(core, controller, ui, env);

            // Ensure foldout arrows exist by creating placeholders if empty
            EnsureChildPlaceholder(controller, "GameController");
            EnsureChildPlaceholder(ui, "UI Root");

            // Clean up empty stray GameObjects at the root
            CleanupStrayRootObjects();

            // Apply vHierarchy colors
            ApplyVHierarchyColors(core, controller, ui, env);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorApplication.RepaintHierarchyWindow();

            Debug.Log("[HierarchySetupTool] Hierarchy successfully arranged with vHierarchy header styling.");
        }

        private static GameObject GetOrCreateHeader(string name, int siblingIndex)
        {
            var scene = SceneManager.GetActiveScene();
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (existing == null)
            {
                existing = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(existing, $"Create {name}");
            }

            existing.transform.SetSiblingIndex(siblingIndex);
            return existing;
        }

        private static void OrganizeExistingObjects(GameObject core, GameObject controller, GameObject ui, GameObject env)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root == core || root == controller || root == ui || root == env)
                    continue;

                // Cameras or audio listeners -> Core
                if (root.GetComponentInChildren<Camera>() != null || root.GetComponentInChildren<AudioListener>() != null)
                {
                    Undo.SetTransformParent(root.transform, core.transform, "Move to Core");
                }
                // Canvases -> UI
                else if (root.GetComponentInChildren<Canvas>() != null)
                {
                    Undo.SetTransformParent(root.transform, ui.transform, "Move to UI");
                }
                // Lights, Tilemaps, Grids -> ENV
                else if (root.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>() != null ||
                         root.GetComponentInChildren<Light>() != null ||
                         root.GetComponentInChildren<Grid>() != null)
                {
                    Undo.SetTransformParent(root.transform, env.transform, "Move to ENV");
                }
            }
        }

        private static void EnsureChildPlaceholder(GameObject parent, string placeholderName)
        {
            if (parent.transform.childCount == 0)
            {
                var placeholder = new GameObject(placeholderName);
                placeholder.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(placeholder, $"Create {placeholderName}");
            }
        }

        private static void CleanupStrayRootObjects()
        {
            var scene = SceneManager.GetActiveScene();
            var stray = scene.GetRootGameObjects()
                .FirstOrDefault(g => g.name == "GameObject" && g.transform.childCount == 0 && g.GetComponents<Component>().Length <= 1);
            if (stray != null)
            {
                Undo.DestroyObjectImmediate(stray);
            }
        }

        private static void ApplyVHierarchyColors(GameObject core, GameObject controller, GameObject ui, GameObject env)
        {
            VHierarchy.VHierarchy.SetColor(core, ColorRed, false);
            VHierarchy.VHierarchy.SetColor(controller, ColorYellow, false);
            VHierarchy.VHierarchy.SetColor(ui, ColorGreen, false);
            VHierarchy.VHierarchy.SetColor(env, ColorTeal, false);

            if (VHierarchy.VHierarchy.data != null)
            {
                EditorUtility.SetDirty(VHierarchy.VHierarchy.data);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
