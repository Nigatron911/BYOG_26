using System.IO;
using UnityEngine;
using UnityEditor;

namespace Game.Editor
{
    public static class SpriteAssetGenerator
    {
        private const string SpritesDir = "Assets/_Game/Data/Sprites";

        [MenuItem("Game/Generate High Quality Prototype Sprites", false, 1)]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(SpritesDir))
            {
                Directory.CreateDirectory(SpritesDir);
            }

            GeneratePlayerSprite();
            GenerateCrateSprite();
            GeneratePlankSprite();
            GenerateLadderSprite();
            GeneratePlatformSprite();
            GenerateSpikeSprite();
            GenerateGoalSprite();
            GenerateGoalShrineSprite();
            GeneratePlatformTileSprite();
            GenerateLedgeCapSprite();
            GenerateLedgeMossSprite();
            GenerateBedrockSprite();
            GenerateSpawnerSprite();

            AssetDatabase.Refresh();
            Debug.Log("[SpriteAssetGenerator] All high quality sprites generated successfully!");
        }

        private static void GeneratePlayerSprite()
        {
            int w = 64, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color suitColor = new Color(0.18f, 0.22f, 0.28f); // Dark tech graphite
            Color visorColor = new Color(0.0f, 0.9f, 1.0f);   // Bright cyan visor
            Color accentColor = new Color(0.2f, 0.8f, 0.7f);  // Teal accent
            Color beltColor = new Color(0.9f, 0.65f, 0.2f);   // Amber belt
            Color bootColor = new Color(0.12f, 0.14f, 0.18f); // Heavy boots

            // Boots (y: 4 to 22)
            DrawFilledRect(tex, 16, 4, 14, 16, bootColor);
            DrawFilledRect(tex, 34, 4, 14, 16, bootColor);

            // Legs (y: 20 to 42)
            DrawFilledRect(tex, 18, 20, 10, 22, suitColor);
            DrawFilledRect(tex, 36, 20, 10, 22, suitColor);

            // Torso / Suit (y: 42 to 68)
            DrawFilledRect(tex, 14, 42, 36, 26, suitColor);
            DrawFilledRect(tex, 14, 42, 36, 6, beltColor); // Belt
            DrawFilledRect(tex, 24, 52, 16, 12, accentColor); // Chestplate badge

            // Helmet / Head (y: 68 to 92, x: 18 to 46)
            DrawFilledCircle(tex, 32, 78, 14, Color.white);
            DrawFilledCircle(tex, 32, 78, 12, suitColor);

            // Visor (y: 74 to 82, x: 26 to 44)
            DrawFilledRect(tex, 26, 74, 18, 8, visorColor);
            // Visor shine highlight
            DrawFilledRect(tex, 28, 79, 6, 2, Color.white);

            // Backpack (y: 46 to 66, x: 8 to 14)
            DrawFilledRect(tex, 10, 46, 5, 20, new Color(0.12f, 0.15f, 0.2f));

            SaveTexture(tex, "Prototype_Player.png", filterMode: FilterMode.Point);
        }

        private static void GenerateCrateSprite()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color woodBase = new Color(0.82f, 0.52f, 0.24f);     // Warm crate wood
            Color woodDark = new Color(0.68f, 0.40f, 0.18f);     // Wood shadow
            Color metalPlate = new Color(0.35f, 0.38f, 0.44f);   // Steel brackets
            Color metalRivet = new Color(0.85f, 0.90f, 0.95f);   // Steel rivets
            Color borderDark = new Color(0.20f, 0.12f, 0.08f);   // Crisp dark border

            // Outer border
            DrawFilledRect(tex, 0, 0, size, size, borderDark);
            // Inner wood fill
            DrawFilledRect(tex, 4, 4, size - 8, size - 8, woodBase);

            // Wood panel horizontal seams
            for (int y = 32; y < size; y += 32)
            {
                DrawFilledRect(tex, 4, y - 2, size - 8, 3, woodDark);
            }

            // Diagonal cross bracing (X)
            int thick = 12;
            for (int i = 8; i < size - 8; i++)
            {
                for (int t = -thick / 2; t <= thick / 2; t++)
                {
                    int y1 = i + t;
                    if (y1 >= 8 && y1 < size - 8) tex.SetPixel(i, y1, woodDark);

                    int y2 = (size - 1 - i) + t;
                    if (y2 >= 8 && y2 < size - 8) tex.SetPixel(i, y2, woodDark);
                }
            }

            // Metal corner reinforcements (28x28 in each corner)
            int corner = 26;
            DrawCornerBracket(tex, 0, 0, corner, metalPlate, metalRivet, 0);
            DrawCornerBracket(tex, size - corner, 0, corner, metalPlate, metalRivet, 1);
            DrawCornerBracket(tex, 0, size - corner, corner, metalPlate, metalRivet, 2);
            DrawCornerBracket(tex, size - corner, size - corner, corner, metalPlate, metalRivet, 3);

            SaveTexture(tex, "Prototype_Box.png", filterMode: FilterMode.Bilinear);
        }

        private static void DrawCornerBracket(Texture2D tex, int ox, int oy, int s, Color plate, Color rivet, int cornerIdx)
        {
            DrawFilledRect(tex, ox, oy, s, s, plate);
            // Dark edge
            for (int i = 0; i < s; i++)
            {
                tex.SetPixel(ox + i, oy, Color.black);
                tex.SetPixel(ox + i, oy + s - 1, Color.black);
                tex.SetPixel(ox, oy + i, Color.black);
                tex.SetPixel(ox + s - 1, oy + i, Color.black);
            }
            // Rivet in center
            DrawFilledCircle(tex, ox + s / 2, oy + s / 2, 3, rivet);
        }

        private static void GeneratePlankSprite()
        {
            int w = 160, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color woodBase = new Color(0.72f, 0.48f, 0.26f);     // Warm oak
            Color woodDark = new Color(0.48f, 0.28f, 0.12f);     // Grain shadow & border
            Color woodGrain = new Color(0.60f, 0.38f, 0.18f);    // Subtle timber grain
            Color ironPlate = new Color(0.28f, 0.32f, 0.38f);    // Iron end brackets
            Color ironRivet = new Color(0.85f, 0.88f, 0.92f);    // Steel rivets

            // Main wood body
            DrawFilledRect(tex, 0, 0, w, h, woodDark);
            DrawFilledRect(tex, 2, 2, w - 4, h - 4, woodBase);

            // Longitudinal wood grain lines
            DrawFilledRect(tex, 10, 8, w - 20, 2, woodGrain);
            DrawFilledRect(tex, 14, 16, w - 28, 2, woodGrain);
            DrawFilledRect(tex, 8, 23, w - 16, 2, woodGrain);

            // Left iron end bracket (12px)
            DrawFilledRect(tex, 2, 2, 12, h - 4, ironPlate);
            DrawFilledRect(tex, 13, 2, 1, h - 4, woodDark);
            DrawFilledCircle(tex, 8, 8, 2, ironRivet);
            DrawFilledCircle(tex, 8, h - 9, 2, ironRivet);

            // Right iron end bracket (12px)
            DrawFilledRect(tex, w - 14, 2, 12, h - 4, ironPlate);
            DrawFilledRect(tex, w - 15, 2, 1, h - 4, woodDark);
            DrawFilledCircle(tex, w - 9, 8, 2, ironRivet);
            DrawFilledCircle(tex, w - 9, h - 9, 2, ironRivet);

            SaveTexture(tex, "Prototype_Plank.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateLadderSprite()
        {
            int w = 64, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color woodPole = new Color(0.70f, 0.46f, 0.24f);     // Rustic wooden pole
            Color woodDark = new Color(0.44f, 0.25f, 0.10f);     // Pole shadow
            Color woodRung = new Color(0.82f, 0.58f, 0.32f);     // Ladder rungs
            Color rungShad = new Color(0.36f, 0.20f, 0.08f);     // Rung shadow
            Color ironPeg  = new Color(0.24f, 0.28f, 0.34f);     // Iron peg pins

            // Left Side Pole (x: 8 to 20)
            DrawFilledRect(tex, 8, 0, 12, h, woodDark);
            DrawFilledRect(tex, 10, 0, 8, h, woodPole);

            // Right Side Pole (x: 44 to 56)
            DrawFilledRect(tex, 44, 0, 12, h, woodDark);
            DrawFilledRect(tex, 46, 0, 8, h, woodPole);

            // 6 Horizontal Rungs spaced along height
            for (int y = 16; y < h - 14; y += 24)
            {
                // Rung body spanning across poles
                DrawFilledRect(tex, 6, y, 52, 10, rungShad);
                DrawFilledRect(tex, 7, y + 2, 50, 7, woodRung);
                DrawFilledRect(tex, 7, y + 7, 50, 2, new Color(0.92f, 0.72f, 0.45f)); // Top rung highlight

                // Peg pins fastening rungs to uprights
                DrawFilledCircle(tex, 14, y + 5, 2, ironPeg);
                DrawFilledCircle(tex, 50, y + 5, 2, ironPeg);
            }

            SaveTexture(tex, "Prototype_Ladder.png", filterMode: FilterMode.Bilinear);
        }

        private static void GeneratePlatformSprite()
        {
            int w = 128, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color stoneBody = new Color(0.32f, 0.35f, 0.42f);   // Chiseled stone brick
            Color stoneLight = new Color(0.46f, 0.50f, 0.58f);  // Brick highlight
            Color mortarDark = new Color(0.16f, 0.18f, 0.24f);  // Deep mortar seam
            Color mossGreen  = new Color(0.20f, 0.75f, 0.60f);  // Cyan moss flecks

            // Base fill
            DrawFilledRect(tex, 0, 0, w, h, mortarDark);
            DrawFilledRect(tex, 2, 2, w - 4, h - 4, stoneBody);

            // Staggered stone brick layers
            // Row 1 (y: 34 to 60)
            DrawFilledRect(tex, 4, 34, 56, 26, stoneLight);
            DrawFilledRect(tex, 64, 34, 60, 26, stoneLight);

            // Row 2 (y: 4 to 30)
            DrawFilledRect(tex, 4, 4, 36, 26, stoneBody);
            DrawFilledRect(tex, 44, 4, 44, 26, stoneBody);
            DrawFilledRect(tex, 92, 4, 32, 26, stoneBody);

            // Moss highlights along top seam
            for (int x = 8; x < w - 8; x += 12)
            {
                DrawFilledCircle(tex, x, 58, 3, mossGreen);
            }

            SaveTexture(tex, "Prototype_Platform.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateSpikeSprite()
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color spikeSteel = new Color(0.40f, 0.42f, 0.48f);
            Color dangerTip = new Color(1.0f, 0.22f, 0.22f);
            Color baseRim = new Color(0.18f, 0.18f, 0.22f);

            // Base plate (y: 0 to 8)
            DrawFilledRect(tex, 0, 0, w, 8, baseRim);

            // 2 Triangle Spikes (x: 4 to 30, and 34 to 60)
            DrawTriangleSpike(tex, 4, 30, 8, 60, spikeSteel, dangerTip);
            DrawTriangleSpike(tex, 34, 60, 8, 60, spikeSteel, dangerTip);

            SaveTexture(tex, "Prototype_Spike.png", filterMode: FilterMode.Bilinear);
        }

        private static void DrawTriangleSpike(Texture2D tex, int x0, int x1, int y0, int y1, Color baseColor, Color tipColor)
        {
            int midX = (x0 + x1) / 2;
            int height = y1 - y0;

            for (int y = y0; y <= y1; y++)
            {
                float t = (float)(y - y0) / height;
                int span = (int)((1f - t) * (midX - x0));
                Color col = Color.Lerp(baseColor, tipColor, t);

                for (int x = midX - span; x <= midX + span; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
        }

        private static void GenerateGoalSprite()
        {
            int w = 64, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color mastColor = new Color(0.85f, 0.88f, 0.92f); // Chrome mast
            Color flagGreen = new Color(0.18f, 0.85f, 0.45f); // Victory emerald
            Color flagAccent = new Color(1.0f, 0.9f, 0.2f);   // Gold star/trim
            Color baseColor = new Color(0.2f, 0.22f, 0.28f);

            // Pedestal base (y: 0 to 10)
            DrawFilledRect(tex, 16, 0, 32, 10, baseColor);
            DrawFilledRect(tex, 20, 10, 24, 4, mastColor);

            // Pole (y: 10 to 92, x: 28 to 34)
            DrawFilledRect(tex, 29, 10, 6, 82, mastColor);
            DrawFilledCircle(tex, 32, 92, 5, flagAccent); // Gold finial top

            // Flag cloth (y: 56 to 88, extends right to x: 62)
            DrawFilledRect(tex, 35, 56, 27, 32, flagGreen);
            // Flag golden border
            DrawFilledRect(tex, 35, 56, 27, 3, flagAccent);
            DrawFilledRect(tex, 35, 85, 27, 3, flagAccent);
            DrawFilledRect(tex, 59, 56, 3, 32, flagAccent);

            // Star/Diamond in center
            DrawFilledCircle(tex, 48, 72, 5, flagAccent);

            SaveTexture(tex, "Prototype_Goal.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateGoalShrineSprite()
        {
            int w = 96, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color stoneFrame = new Color(0.25f, 0.28f, 0.35f);  // Ancient carved stone
            Color stoneDark  = new Color(0.14f, 0.16f, 0.22f);  // Stone shadows
            Color goldRim    = new Color(0.95f, 0.78f, 0.25f);  // Golden shrine roof & rim
            Color lanternCore= new Color(1.0f, 0.92f, 0.60f);   // Radiant lantern core
            Color lanternHalo= new Color(1.0f, 0.65f, 0.15f, 0.45f); // Warm amber glow
            Color roofTile   = new Color(0.35f, 0.40f, 0.50f);  // Slate pagoda roof

            // Stepped Pedestal Base (y: 0 to 18)
            DrawFilledRect(tex, 8, 0, w - 16, 8, stoneDark);
            DrawFilledRect(tex, 14, 8, w - 28, 6, stoneFrame);
            DrawFilledRect(tex, 18, 14, w - 36, 4, goldRim);

            // Side Pillars (Left: 18 to 28, Right: w-28 to w-18)
            DrawFilledRect(tex, 18, 18, 10, 68, stoneFrame);
            DrawFilledRect(tex, 18, 18, 2, 68, stoneDark);
            DrawFilledRect(tex, w - 28, 18, 10, 68, stoneFrame);
            DrawFilledRect(tex, w - 20, 18, 2, 68, stoneDark);

            // Pagoda Roof Arch (y: 86 to 118)
            DrawFilledRect(tex, 12, 86, w - 24, 8, goldRim);
            for (int y = 94; y < 118; y++)
            {
                int span = (int)((1f - (float)(y - 94) / 24f) * (w / 2 - 8));
                DrawFilledRect(tex, w / 2 - span, y, span * 2, 1, roofTile);
            }
            DrawFilledRect(tex, w / 2 - 6, 118, 12, 8, goldRim); // Roof spire

            // Center Lantern Orb Glow (cx: w/2, cy: 52)
            int cx = w / 2, cy = 52;
            DrawFilledCircle(tex, cx, cy, 22, lanternHalo);
            DrawFilledCircle(tex, cx, cy, 14, lanternCore);
            DrawFilledCircle(tex, cx, cy, 8, Color.white);

            SaveTexture(tex, "Prototype_GoalShrine.png", filterMode: FilterMode.Bilinear);
        }

        private static void GeneratePlatformTileSprite()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color slateBody = new Color(0.22f, 0.25f, 0.32f);   // Modern slate
            Color slateLight = new Color(0.28f, 0.32f, 0.40f);  // Top/left bevel highlight
            Color slateDark = new Color(0.14f, 0.16f, 0.22f);   // Bottom/right bevel shadow
            Color gridLine = new Color(0.18f, 0.20f, 0.26f);    // Subtle panel seams

            DrawFilledRect(tex, 0, 0, size, size, slateBody);

            // Bevel edges (4px)
            DrawFilledRect(tex, 0, size - 4, size, 4, slateLight); // Top
            DrawFilledRect(tex, 0, 0, 4, size, slateLight);        // Left
            DrawFilledRect(tex, 0, 0, size, 4, slateDark);         // Bottom
            DrawFilledRect(tex, size - 4, 0, 4, size, slateDark);  // Right

            // Interior panel line
            DrawFilledRect(tex, 0, size / 2 - 1, size, 2, gridLine);
            DrawFilledRect(tex, size / 2 - 1, 0, 2, size, gridLine);

            SaveTexture(tex, "Prototype_Square.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateLedgeCapSprite()
        {
            int w = 128, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color mintTop = new Color(0.28f, 0.85f, 0.72f);    // Vibrant mint surface
            Color cyanGlow = new Color(0.0f, 0.95f, 1.0f);     // Pure neon cyan highlight
            Color darkBase = new Color(0.15f, 0.18f, 0.24f);

            DrawFilledRect(tex, 0, 0, w, h, mintTop);
            DrawFilledRect(tex, 0, h - 4, w, 4, cyanGlow);     // Glowing rim
            DrawFilledRect(tex, 0, 0, w, 4, darkBase);         // Under-bevel

            SaveTexture(tex, "Prototype_LedgeCap.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateLedgeMossSprite()
        {
            int w = 128, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color mossCyan = new Color(0.15f, 0.85f, 0.75f);   // Glowing cyan-green moss
            Color mossGlow = new Color(0.0f, 0.98f, 1.0f);     // Neon cyan fringe
            Color darkGrass= new Color(0.08f, 0.35f, 0.32f);   // Under-shadow

            DrawFilledRect(tex, 0, 16, w, 12, mossCyan);
            DrawFilledRect(tex, 0, 26, w, 6, mossGlow);        // Glowing surface lip

            // Hanging moss blades along the bottom
            for (int x = 2; x < w - 2; x += 6)
            {
                int hang = ((x * 7) % 11) + 4;
                DrawFilledRect(tex, x, 16 - hang, 3, hang, darkGrass);
                DrawFilledRect(tex, x, 16 - hang + 2, 2, hang - 2, mossCyan);
            }

            SaveTexture(tex, "Prototype_LedgeMoss.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateBedrockSprite()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color bedrockDark = new Color(0.09f, 0.11f, 0.15f);   // Solid deep cavern rock
            Color bedrockMid  = new Color(0.13f, 0.15f, 0.20f);   // Rock strata
            Color crystalFleck= new Color(0.20f, 0.35f, 0.45f);   // Subtle crystalline veins

            DrawFilledRect(tex, 0, 0, size, size, bedrockDark);

            // Subtle stratified rock texture lines
            for (int y = 12; y < size; y += 28)
            {
                DrawFilledRect(tex, 0, y, size, 4, bedrockMid);
            }

            // Mineral flecks
            for (int x = 16; x < size; x += 32)
            {
                for (int y = 16; y < size; y += 32)
                {
                    DrawFilledCircle(tex, x, y, 2, crystalFleck);
                }
            }

            SaveTexture(tex, "Prototype_Bedrock.png", filterMode: FilterMode.Bilinear);
        }

        private static void GenerateSpawnerSprite()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ClearTexture(tex);

            Color nozzleColor = new Color(0.3f, 0.7f, 1.0f);
            Color arrowColor = new Color(1.0f, 0.85f, 0.2f);
            Color darkHousing = new Color(0.15f, 0.18f, 0.25f);

            // Top housing
            DrawFilledRect(tex, 12, 40, 40, 20, darkHousing);
            DrawFilledRect(tex, 16, 34, 32, 10, nozzleColor);

            // Downward Chevron Arrow (y: 8 to 28)
            int midX = 32;
            for (int y = 10; y <= 26; y++)
            {
                int span = (y - 10);
                for (int x = midX - span; x <= midX + span; x++)
                {
                    if (Mathf.Abs(x - midX) >= span - 3)
                    {
                        tex.SetPixel(x, y, arrowColor);
                    }
                }
            }

            SaveTexture(tex, "Prototype_Spawner.png", filterMode: FilterMode.Bilinear);
        }

        // --- Helper Drawing Primitives ---

        private static void ClearTexture(Texture2D tex)
        {
            var clear = new Color(0, 0, 0, 0);
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);
        }

        private static void DrawFilledRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            int maxX = Mathf.Min(x + w, tex.width);
            int maxY = Mathf.Min(y + h, tex.height);
            for (int px = Mathf.Max(0, x); px < maxX; px++)
            {
                for (int py = Mathf.Max(0, y); py < maxY; py++)
                {
                    tex.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawFilledCircle(Texture2D tex, int cx, int cy, int r, Color color)
        {
            int r2 = r * r;
            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        int dx = x - cx;
                        int dy = y - cy;
                        if (dx * dx + dy * dy <= r2)
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }

        private static void SaveTexture(Texture2D tex, string fileName, FilterMode filterMode)
        {
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(SpritesDir, fileName);
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64f;
                importer.filterMode = filterMode;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
