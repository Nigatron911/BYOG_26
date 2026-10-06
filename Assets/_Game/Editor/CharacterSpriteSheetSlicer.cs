#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CharacterSpriteSheetSlicer
    {
        public const float TargetPPU = 100f; // Environmental golden scale (~2.76m visual height, perfectly fits 3.12m doors and 2.5m ladders)
        private const int FrameWidth = 224;
        private const int FrameHeight = 384;
        private const int TotalFrames = 5;
        private const float FeetY = 38f;
        private const float PivotY = FeetY / FrameHeight; // ~0.098958f
        private const float PivotX = 0.5f;

        private static readonly string[] SheetPaths = new string[]
        {
            "Assets/assets/SpriteSheet/character_idle_breath_strip.png",
            "Assets/assets/SpriteSheet/character_idle_front_noblink_strip.png",
            "Assets/assets/SpriteSheet/character_jump_strip.png",
            "Assets/assets/SpriteSheet/character_run_strip.png",
            "Assets/assets/SpriteSheet/character_zerog_strip.png"
        };

        [MenuItem("Game/Slice Character Sprite Sheets", priority = 50)]
        public static void SliceAllSheets()
        {
            foreach (var path in SheetPaths)
            {
                SliceSheet(path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterSpriteSheetSlicer] All 5 character sprite sheets sliced uniformly with ground pivots.");
        }

        public static void SliceSheet(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[CharacterSpriteSheetSlicer] Could not load importer for {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TargetPPU;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var metas = new SpriteMetaData[TotalFrames];

            for (int i = 0; i < TotalFrames; i++)
            {
                var meta = new SpriteMetaData
                {
                    name = $"{baseName}_{i}",
                    rect = new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(PivotX, PivotY)
                };
                metas[i] = meta;
            }

            importer.spritesheet = metas;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
#endif
