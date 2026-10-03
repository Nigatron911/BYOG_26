using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    public static class AssetGenerator
    {
        [MenuItem("Tools/Generate Game Sprites", priority = 10)]
        public static void GenerateAllSprites()
        {
            string folder = "Assets/_Project/Sprites";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            CreatePaperSprite(folder + "/paper_form.png");
            CreateStoneSprite(folder + "/stone_form.png");
            CreateRubberSprite(folder + "/rubber_form.png");
            CreatePlatformTile(folder + "/platform_tile.png");
            CreateFragileTile(folder + "/fragile_tile.png");
            CreateWindParticle(folder + "/wind_particle.png");
            CreateWindVentGrateSprite(folder + "/wind_vent_grate.png");
            CreateCheckpointSprite(folder + "/checkpoint_beacon.png");
            CreateFinishGateSprite(folder + "/finish_trophy.png");
            CreateSparkleParticle(folder + "/particle_spark.png");

            // Level 2 Sprites
            CreateSpikeSprite(folder + "/spike_tile.png");
            CreateSpringPadSprite(folder + "/spring_pad.png");
            CreateTechWallSprite(folder + "/tech_wall.png");
            CreateTechFloorSprite(folder + "/tech_floor.png");
            CreateReticleCrossSprite(folder + "/reticle_cross.png");
            CreateReticleBracketSprite(folder + "/reticle_bracket.png");
            CreateRealityPlayerSprite(folder + "/reality_player.png");

            // Level 3 Sprites
            CreatePaperScrapSprite(folder + "/paper_scrap.png");
            CreateWoodBlockSprite(folder + "/wood_block.png");
            CreateRubberPadSprite(folder + "/rubber_pad.png");
            CreateAnvilBlockSprite(folder + "/anvil_block.png");
            CreatePressurePlateSprite(folder + "/pressure_plate.png");
            CreateHeavyGateSprite(folder + "/heavy_gate.png");
            CreateCanvasFrameSprite(folder + "/canvas_frame.png");

            AssetDatabase.Refresh();

            ConfigureSprite(folder + "/paper_form.png", 64);
            ConfigureSprite(folder + "/stone_form.png", 64);
            ConfigureSprite(folder + "/rubber_form.png", 64);
            ConfigureSprite(folder + "/platform_tile.png", 64);
            ConfigureSprite(folder + "/fragile_tile.png", 64);
            ConfigureSprite(folder + "/wind_particle.png", 64);
            ConfigureSprite(folder + "/wind_vent_grate.png", 64);
            ConfigureSprite(folder + "/checkpoint_beacon.png", 64);
            ConfigureSprite(folder + "/finish_trophy.png", 64);
            ConfigureSprite(folder + "/particle_spark.png", 64);

            ConfigureSprite(folder + "/spike_tile.png", 64);
            ConfigureSprite(folder + "/spring_pad.png", 64);
            ConfigureSprite(folder + "/tech_wall.png", 64);
            ConfigureSprite(folder + "/tech_floor.png", 64);
            ConfigureSprite(folder + "/reticle_cross.png", 64);
            ConfigureSprite(folder + "/reticle_bracket.png", 64);
            ConfigureSprite(folder + "/reality_player.png", 64);

            ConfigureSprite(folder + "/paper_scrap.png", 64);
            ConfigureSprite(folder + "/wood_block.png", 64);
            ConfigureSprite(folder + "/rubber_pad.png", 64);
            ConfigureSprite(folder + "/anvil_block.png", 64);
            ConfigureSprite(folder + "/pressure_plate.png", 64);
            ConfigureSprite(folder + "/heavy_gate.png", 64);
            ConfigureSprite(folder + "/canvas_frame.png", 64);

            AssetDatabase.SaveAssets();
            Debug.Log("[AssetGenerator] All sprites generated and configured successfully!");
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Repeat;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.wrapMode = TextureWrapMode.Repeat;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        private static void CreatePaperSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Diamond/folded paper shape
                    float dx = Mathf.Abs(x - 31.5f) / 28f;
                    float dy = Mathf.Abs(y - 31.5f) / 28f;

                    if (dx + dy <= 1.0f)
                    {
                        // Paper white/cream with subtle origami folds
                        float fold = (x > y) ? 1.0f : 0.88f;
                        if (Mathf.Abs(x - y) <= 1) fold = 0.72f; // Crease line
                        if (x == 31 || y == 31) fold *= 0.93f;

                        Color paperCol = new Color(0.96f * fold, 0.98f * fold, 1.0f * fold, 1.0f);
                        if (dx + dy >= 0.92f) paperCol = new Color(0.6f, 0.7f, 0.85f, 1.0f); // Edge border
                        tex.SetPixel(x, y, paperCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStoneSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Sturdy rounded rock / stone block with chamfered corners
                    float nx = Mathf.Clamp01(Mathf.Abs(x - 31.5f) / 26f);
                    float ny = Mathf.Clamp01(Mathf.Abs(y - 31.5f) / 26f);
                    float corner = Mathf.Max(nx, ny);
                    if (nx > 0.7f && ny > 0.7f)
                    {
                        corner = Mathf.Sqrt(Mathf.Pow(nx - 0.7f, 2) + Mathf.Pow(ny - 0.7f, 2)) / 0.42f + 0.7f;
                    }

                    if (corner <= 1.0f)
                    {
                        // Rock shading
                        float highlight = (y > 35) ? 0.2f : 0f;
                        float shadow = (y < 20) ? -0.2f : 0f;
                        float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.15f;

                        // Crack details
                        bool isCrack = (x > 20 && x < 45 && Mathf.Abs(y - (x * 0.6f + 14)) < 1.2f);
                        if (isCrack)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.14f, 0.18f, 1.0f));
                        }
                        else
                        {
                            float b = 0.42f + highlight + shadow + noise;
                            Color rock = new Color(b * 0.85f, b * 0.9f, b * 1.0f, 1.0f);
                            if (corner >= 0.88f) rock = new Color(0.2f, 0.22f, 0.28f, 1.0f); // Dark stone border
                            tex.SetPixel(x, y, rock);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRubberSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Smooth circle/capsule
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 27f;

                    if (dist <= 1.0f)
                    {
                        // Vibrant bouncy cyan/lime
                        Color baseCol = new Color(0.0f, 0.9f, 0.75f, 1.0f);
                        // Specular shine at top-left
                        float shine = Vector2.Distance(new Vector2(x, y), new Vector2(23f, 40f)) / 10f;
                        if (shine < 1.0f)
                        {
                            baseCol = Color.Lerp(Color.white, baseCol, shine);
                        }
                        if (dist > 0.85f)
                        {
                            baseCol = Color.Lerp(baseCol, new Color(0.0f, 0.5f, 0.4f), 0.7f); // Deep rim
                        }
                        tex.SetPixel(x, y, baseCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePlatformTile(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;
                    if (y >= 58)
                    {
                        c = new Color(0.38f, 0.65f, 0.98f); // Bright blue top highlight
                    }
                    else if (y >= 54)
                    {
                        c = new Color(0.2f, 0.45f, 0.8f); // Upper ridge
                    }
                    else if (x < 3 || x >= 61 || y < 3)
                    {
                        c = new Color(0.1f, 0.14f, 0.22f); // Border
                    }
                    else
                    {
                        // Slate body with subtle tech grid
                        float grid = ((x % 16 == 0) || (y % 16 == 0)) ? 0.88f : 1.0f;
                        c = new Color(0.16f * grid, 0.22f * grid, 0.32f * grid);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateFragileTile(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Cracked crumbling platform with hazard reddish/amber cracks
                    bool crack1 = Mathf.Abs(y - (x * 0.7f + 12)) < 1.8f && x > 10 && x < 54;
                    bool crack2 = Mathf.Abs(y - (-x * 0.8f + 56)) < 1.8f && x > 15 && x < 48;
                    bool crack3 = Mathf.Abs(x - 32) < 1.5f && y < 35 && y > 8;

                    if (crack1 || crack2 || crack3)
                    {
                        tex.SetPixel(x, y, new Color(0.95f, 0.3f, 0.15f, 1.0f)); // Bright glowing crack
                    }
                    else if (y >= 58)
                    {
                        tex.SetPixel(x, y, new Color(0.85f, 0.65f, 0.4f, 1.0f)); // Weathered sandstone top
                    }
                    else if (x < 3 || x >= 61 || y < 3)
                    {
                        tex.SetPixel(x, y, new Color(0.2f, 0.15f, 0.1f, 1.0f));
                    }
                    else
                    {
                        float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.15f;
                        float b = 0.45f + noise;
                        tex.SetPixel(x, y, new Color(b * 0.9f, b * 0.65f, b * 0.45f, 1.0f));
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateWindParticle(string path)
        {
            int w = 32, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                // Curving wavy ribbon x-center
                float normalizedY = (float)y / h;
                float waveOffset = Mathf.Sin(normalizedY * Mathf.PI * 2.5f) * 6f;
                float centerX = 15.5f + waveOffset;

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - centerX) / 4.5f;
                    float dy = Mathf.Sin(normalizedY * Mathf.PI); // fade at top and bottom

                    if (dx <= 1.0f)
                    {
                        float alpha = (1.0f - dx) * dy * 0.9f;
                        tex.SetPixel(x, y, new Color(0.75f, 0.92f, 1.0f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateWindVentGrateSprite(string path)
        {
            int w = 128, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Outer beveled frame
                    if (x < 4 || x >= w - 4 || y < 3 || y >= h - 3)
                    {
                        tex.SetPixel(x, y, new Color(0.22f, 0.28f, 0.38f, 1.0f));
                    }
                    else if (x < 7 || x >= w - 7 || y < 5 || y >= h - 5)
                    {
                        tex.SetPixel(x, y, new Color(0.12f, 0.16f, 0.24f, 1.0f));
                    }
                    else
                    {
                        // Internal ventilation louvers / grill slats
                        int slot = (x - 7) % 10;
                        if (slot < 3)
                        {
                            tex.SetPixel(x, y, new Color(0.35f, 0.42f, 0.52f, 1.0f)); // Metal slat
                        }
                        else
                        {
                            // Glowing updraft aperture
                            float intensity = (float)(y - 5) / (h - 10);
                            tex.SetPixel(x, y, new Color(0.1f, 0.7f + intensity * 0.3f, 0.9f + intensity * 0.1f, 1.0f));
                        }
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateCheckpointSprite(string path)
        {
            // Sleek futuristic checkpoint spire / beacon pylon (32 x 80)
            int w = 48, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 1. Heavy metallic base pedestal (y: 0 to 18)
                    if (y <= 18)
                    {
                        float baseWidth = 36f - y * 0.5f;
                        if (Mathf.Abs(x - 23.5f) <= baseWidth * 0.5f)
                        {
                            bool border = Mathf.Abs(x - 23.5f) >= baseWidth * 0.5f - 2f || y <= 2;
                            Color c = border ? new Color(0.5f, 0.6f, 0.75f, 1f) : new Color(0.2f, 0.25f, 0.35f, 1f);
                            tex.SetPixel(x, y, c);
                            continue;
                        }
                    }

                    // 2. Twin vertical antenna struts / energy pylons (y: 18 to 68)
                    if (y > 18 && y <= 68)
                    {
                        float leftPylon = Mathf.Abs(x - 13.5f);
                        float rightPylon = Mathf.Abs(x - 33.5f);
                        if (leftPylon <= 2.5f || rightPylon <= 2.5f)
                        {
                            tex.SetPixel(x, y, new Color(0.3f, 0.38f, 0.48f, 1.0f));
                            continue;
                        }

                        // Central energy stream between pylons
                        if (Mathf.Abs(x - 23.5f) <= 2.0f)
                        {
                            tex.SetPixel(x, y, new Color(0.2f, 0.8f, 0.95f, 0.75f));
                            continue;
                        }
                    }

                    // 3. Floating Energy Diamond / Apex Crystal (y: 65 to 92)
                    if (y >= 65 && y <= 92)
                    {
                        float dx = Mathf.Abs(x - 23.5f) / 10f;
                        float dy = Mathf.Abs(y - 78.5f) / 13f;
                        if (dx + dy <= 1.0f)
                        {
                            float core = 1.0f - (dx + dy);
                            // Glowing energy core
                            Color crystalCol = new Color(0.2f + core * 0.7f, 0.95f, 0.65f + core * 0.35f, 1.0f);
                            if (dx + dy >= 0.85f) crystalCol = new Color(0.1f, 0.6f, 0.4f, 1.0f); // Diamond edge
                            tex.SetPixel(x, y, crystalCol);
                            continue;
                        }
                    }

                    tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateFinishGateSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Golden trophy cup / victory arch
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 26f;
                    if (dist <= 1.0f)
                    {
                        // Golden gradient
                        float gold = (y / 64f) * 0.5f + 0.5f;
                        Color c = new Color(1.0f * gold, 0.85f * gold, 0.2f, 1.0f);
                        if (dist >= 0.82f) c = new Color(0.8f, 0.55f, 0.1f, 1.0f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSparkleParticle(string path)
        {
            int w = 32, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 14f;
                    if (dist <= 1.0f)
                    {
                        float alpha = Mathf.Pow(1.0f - dist, 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSpikeSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Base metal plate at bottom
                    if (y < 12)
                    {
                        Color baseCol = (y < 4 || x % 16 == 0) ? new Color(0.18f, 0.2f, 0.28f, 1f) : new Color(0.3f, 0.35f, 0.45f, 1f);
                        tex.SetPixel(x, y, baseCol);
                        continue;
                    }

                    // Two teeth: center 1 at x = 16, center 2 at x = 48
                    int localX = (x < 32) ? x : (x - 32);
                    float centerX = 16f;
                    float toothHeight = 48f;
                    float slope = (toothHeight - (y - 12)) / toothHeight;
                    float halfWidth = 14f * slope;

                    if (Mathf.Abs(localX - centerX) <= halfWidth && y >= 12 && y <= 60)
                    {
                        float t = (y - 12f) / 48f;
                        Color spikeCol = Color.Lerp(new Color(0.85f, 0.15f, 0.1f, 1f), new Color(1.0f, 0.6f, 0.15f, 1f), t);
                        if (localX < centerX && Mathf.Abs(localX - (centerX - halfWidth)) < 2.5f)
                        {
                            spikeCol = new Color(1.0f, 0.85f, 0.5f, 1f);
                        }
                        if (Mathf.Abs(localX - centerX) < 1.5f)
                        {
                            spikeCol = Color.Lerp(spikeCol, Color.white, 0.35f);
                        }
                        tex.SetPixel(x, y, spikeCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSpringPadSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y < 12)
                    {
                        Color baseCol = (y < 4) ? new Color(0.12f, 0.15f, 0.22f, 1f) : new Color(0.25f, 0.35f, 0.5f, 1f);
                        tex.SetPixel(x, y, baseCol);
                        continue;
                    }

                    if (y >= 46 && y <= 58 && x >= 4 && x <= 60)
                    {
                        float border = (x <= 6 || x >= 58 || y <= 47 || y >= 57) ? 0.3f : 1.0f;
                        Color padCol = new Color(0.0f, 0.95f * border, 1.0f * border, 1.0f);
                        if (y >= 49 && y <= 55 && Mathf.Abs(x - 32) <= (55 - y))
                        {
                            padCol = Color.white;
                        }
                        tex.SetPixel(x, y, padCol);
                        continue;
                    }

                    if (y >= 12 && y < 46 && x >= 14 && x <= 50)
                    {
                        float cycle = Mathf.Sin((y - 12f) * 0.35f);
                        float coilX = 32f + cycle * 12f;
                        if (Mathf.Abs(x - coilX) <= 4.0f)
                        {
                            Color coilCol = new Color(1.0f, 0.85f, 0.2f, 1f);
                            if (Mathf.Abs(x - coilX) <= 1.5f) coilCol = Color.white;
                            tex.SetPixel(x, y, coilCol);
                            continue;
                        }
                    }

                    tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateTechWallSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x < 4 || x >= 60 || y < 4 || y >= 60;
                    bool isInnerBevel = x == 4 || x == 59 || y == 4 || y == 59;
                    bool isDiagonalTech = ((x + y) % 16 == 0) && (x > 8 && x < 56 && y > 8 && y < 56);

                    Color col = new Color(0.12f, 0.16f, 0.26f, 1f);
                    if (isBorder) col = new Color(0.25f, 0.38f, 0.58f, 1f);
                    else if (isInnerBevel) col = new Color(0.0f, 0.85f, 1.0f, 0.9f);
                    else if (isDiagonalTech) col = new Color(0.18f, 0.26f, 0.42f, 1f);

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateTechFloorSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x < 3 || x >= 61 || y < 3 || y >= 61;
                    bool isCenterSeam = Mathf.Abs(x - 32) < 2 || Mathf.Abs(y - 32) < 2;
                    bool isGrip = ((x / 8 + y / 8) % 2 == 0);

                    Color col = new Color(0.14f, 0.18f, 0.28f, 1f);
                    if (isBorder) col = new Color(0.0f, 0.75f, 0.95f, 1f);
                    else if (isCenterSeam) col = new Color(0.08f, 0.11f, 0.18f, 1f);
                    else if (isGrip) col = new Color(0.18f, 0.24f, 0.36f, 1f);

                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateReticleCrossSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));

                    if (dist <= 3.0f)
                    {
                        tex.SetPixel(x, y, Color.white);
                        continue;
                    }

                    if (dist >= 11f && dist <= 13.5f)
                    {
                        tex.SetPixel(x, y, Color.white);
                        continue;
                    }

                    bool onAxis = (Mathf.Abs(x - 31.5f) <= 1.5f && (dist >= 17f && dist <= 28f)) ||
                                  (Mathf.Abs(y - 31.5f) <= 1.5f && (dist >= 17f && dist <= 28f));

                    if (onAxis)
                    {
                        tex.SetPixel(x, y, Color.white);
                        continue;
                    }

                    tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateReticleBracketSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool verticalBar = (x >= 14 && x <= 20) && (y >= 12 && y <= 52);
                    bool topBar = (x >= 14 && x <= 46) && (y >= 46 && y <= 52);
                    bool bottomBar = (x >= 14 && x <= 46) && (y >= 12 && y <= 18);

                    if (verticalBar || topBar || bottomBar)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRealityPlayerSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 27f;
                    if (dist <= 1.0f)
                    {
                        Color chassis = new Color(0.12f, 0.16f, 0.28f, 1f);
                        if (dist >= 0.85f) chassis = new Color(0.0f, 0.95f, 1.0f, 1f);
                        else if (dist <= 0.45f) chassis = new Color(0.0f, 1.0f, 0.85f, 1f);

                        if (Mathf.Abs(y - 31.5f) <= 4.0f && Mathf.Abs(x - 31.5f) <= 18f)
                        {
                            chassis = Color.white;
                        }

                        tex.SetPixel(x, y, chassis);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePaperScrapSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Folded paper scrap shape (tilted rectangle)
                    bool inPaper = (x >= 14 && x <= 50) && (y >= 10 && y <= 54);
                    // Folded corner
                    bool inFold = (x > 38) && (y > 42) && ((x - 38) + (y - 42) > 14);

                    if (inPaper && !inFold)
                    {
                        // Glowing paper color with ruled lines
                        Color paperCol = new Color(0.96f, 0.94f, 0.85f, 1f);
                        if (x == 14 || x == 50 || y == 10 || y == 54) paperCol = new Color(0.2f, 0.9f, 1f, 1f); // Glowing cyan border
                        else if (y % 8 == 0) paperCol = new Color(0.85f, 0.82f, 0.72f, 1f); // Ruled line
                        tex.SetPixel(x, y, paperCol);
                    }
                    else if (inPaper && inFold)
                    {
                        // Fold shadow / flap
                        tex.SetPixel(x, y, new Color(0.80f, 0.75f, 0.65f, 0.9f));
                    }
                    else
                    {
                        // Outer soft aura
                        float distToBorder = Mathf.Min(Mathf.Abs(x - 32), Mathf.Abs(y - 32));
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateWoodBlockSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Dark sturdy wood planks
                    Color wood = new Color(0.18f, 0.14f, 0.10f, 1f);
                    if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2) wood = new Color(0.32f, 0.24f, 0.16f, 1f);
                    else if (y % 16 == 0) wood = new Color(0.10f, 0.08f, 0.05f, 1f); // Plank seam
                    else if ((x + y * 2) % 17 == 0) wood = new Color(0.24f, 0.18f, 0.12f, 1f); // Wood grain
                    tex.SetPixel(x, y, wood);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateRubberPadSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Electric blue bouncy rubber surface
                    Color rubber = new Color(0.0f, 0.55f, 0.95f, 1f);
                    if (y >= 54) rubber = new Color(0.3f, 0.85f, 1f, 1f); // High-bounce top sheen
                    else if (x < 2 || x >= w - 2 || y < 2) rubber = new Color(0.0f, 0.35f, 0.70f, 1f);
                    else if ((x / 8 + y / 8) % 2 == 0) rubber = new Color(0.05f, 0.60f, 1.0f, 1f); // Diamond grip texture
                    tex.SetPixel(x, y, rubber);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateAnvilBlockSprite(string path)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Heavy iron anvil / steel block
                    Color steel = new Color(0.35f, 0.38f, 0.44f, 1f);
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3) steel = new Color(0.18f, 0.20f, 0.24f, 1f);
                    else if (y >= 56) steel = new Color(0.55f, 0.60f, 0.68f, 1f); // Polished strike surface
                    // Corner rivets
                    bool isRivet = (x >= 6 && x <= 10 && y >= 6 && y <= 10) ||
                                  (x >= 54 && x <= 58 && y >= 6 && y <= 10) ||
                                  (x >= 6 && x <= 10 && y >= 50 && y <= 54) ||
                                  (x >= 54 && x <= 58 && y >= 50 && y <= 54);
                    if (isRivet) steel = new Color(0.75f, 0.80f, 0.88f, 1f);
                    tex.SetPixel(x, y, steel);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePressurePlateSprite(string path)
        {
            int w = 64, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Mechanical base with yellow hazard stripes
                    Color col = new Color(0.2f, 0.22f, 0.26f, 1f);
                    if (y >= 16)
                    {
                        // Yellow / black warning stripes
                        bool stripe = ((x + y) / 6) % 2 == 0;
                        col = stripe ? new Color(0.95f, 0.80f, 0.1f, 1f) : new Color(0.15f, 0.15f, 0.18f, 1f);
                        if (y >= 28 || x < 2 || x >= w - 2) col = new Color(0.4f, 0.42f, 0.48f, 1f); // Bevel
                    }
                    else
                    {
                        if (x < 4 || x >= w - 4 || y < 2) col = new Color(0.12f, 0.14f, 0.16f, 1f);
                    }
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateHeavyGateSprite(string path)
        {
            int w = 64, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color gate = new Color(0.15f, 0.18f, 0.24f, 1f);
                    // Vertical steel bars
                    bool bar = (x >= 6 && x <= 14) || (x >= 26 && x <= 34) || (x >= 46 && x <= 54);
                    // Horizontal reinforcement
                    bool hBar = (y >= 6 && y <= 16) || (y >= 58 && y <= 68) || (y >= 112 && y <= 122);
                    // Cross bracing
                    bool cross = Mathf.Abs(x - (y % 64)) <= 3 || Mathf.Abs((w - x) - (y % 64)) <= 3;

                    if (bar || hBar) gate = new Color(0.45f, 0.50f, 0.58f, 1f);
                    else if (cross) gate = new Color(0.30f, 0.35f, 0.42f, 1f);

                    if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2) gate = new Color(0.08f, 0.10f, 0.14f, 1f);
                    tex.SetPixel(x, y, gate);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateCanvasFrameSprite(string path)
        {
            int w = 256, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color bg = new Color(0.07f, 0.10f, 0.16f, 0.95f);
                    // Cyan glow border
                    bool border = (x < 4 || x >= w - 4 || y < 4 || y >= h - 4);
                    bool innerBorder = (x < 8 || x >= w - 8 || y < 8 || y >= h - 8);
                    // Grid background
                    bool grid = (x % 32 == 0) || (y % 32 == 0);

                    if (border) bg = new Color(0.0f, 0.9f, 1.0f, 1f);
                    else if (innerBorder) bg = new Color(0.0f, 0.45f, 0.65f, 0.9f);
                    else if (grid) bg = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                    tex.SetPixel(x, y, bg);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
