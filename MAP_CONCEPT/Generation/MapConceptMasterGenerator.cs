// BW-MAP2.1B: rasterize the existing authored/generated geography to a 4096²
// 12-km master elevation and a cached relief image, without altering coast/POIs.
using System;
using System.Collections.Generic;
using Godot;

internal static class MapConceptMasterGenerator
{
    private const ulong GlobalDetailSalt = 0xA01857ECA9B3F647UL;

    #region Build

    // =========================================================
    public static void Populate(MapConceptLayout map, long worldSeed)
    {
        if (map.Elevation == null) return;
        var master = new MapConceptMasterElevation();
        int[] ridgeOwners = RidgeOwnerGrid(map);
        long globalDetailSeed = unchecked(worldSeed ^ (long)GlobalDetailSalt);
        var crossings = new List<float>(128);

        RasterizeLand(master, map.Coast, map.Elevation, map.MountainRidges,
            ridgeOwners, globalDetailSeed, crossings);
        foreach (Vector2[] island in map.Islets)
            RasterizeLand(master, island, map.Elevation, map.MountainRidges,
                ridgeOwners, globalDetailSeed, crossings);

        MapConceptWaterCarver.Apply(master, map);
        master.ReliefTexture = MakeRelief(master.Heights, master.WaterMask);
        map.MasterElevation = master;
    }

    // =========================================================
    // Polygon scan conversion tests only boundary edges per row, not every
    // coast vertex for every one of 16.8 million pixels.
    private static void RasterizeLand(MapConceptMasterElevation master, Vector2[] polygon,
        MapConceptElevationGrid preview, List<ConceptMountainRidge> ridges,
        int[] owners, long globalSeed, List<float> crossings)
    {
        Vector2[] coast = MapConceptDrawingGeometry.SmoothClosed(polygon, 2);
        float mapStart = MapConceptMasterElevation.Origin.X;
        float step = MapConceptMasterElevation.Step;
        for (int y = 0; y < MapConceptMasterElevation.Resolution; y++)
        {
            float mapY = MapConceptMasterElevation.Origin.Y + y * step;
            crossings.Clear();
            for (int i = 0; i < coast.Length; i++)
            {
                Vector2 a = coast[i], b = coast[(i + 1) % coast.Length];
                if ((a.Y > mapY) == (b.Y > mapY)) continue;
                float intersection = a.X + (mapY - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                crossings.Add(intersection);
            }
            if (crossings.Count < 2) continue;
            crossings.Sort();
            int row = y * MapConceptMasterElevation.Resolution;
            float coarseY = Mathf.Clamp(
                (mapY - MapConceptElevationGrid.Origin.Y) / MapConceptElevationGrid.Step.Y,
                0f, MapConceptElevationGrid.Rows - 1.0001f);
            int ownerY = Mathf.FloorToInt(coarseY);
            float ownerBlendY = coarseY - ownerY;

            for (int k = 0; k + 1 < crossings.Count; k += 2)
            {
                int start = Mathf.Max(0, Mathf.CeilToInt((crossings[k] - mapStart) / step));
                int end = Mathf.Min(MapConceptMasterElevation.Resolution - 1,
                    Mathf.FloorToInt((crossings[k + 1] - mapStart) / step));
                for (int x = start; x <= end; x++)
                {
                    float mapX = mapStart + x * step;
                    var point = new Vector2(mapX, mapY);
                    float baseHeight = Mathf.Max(0.012f, preview.Sample(point));
                    // Microdetail is evaluated in continuous map space, not
                    // by screen pixel. Blend local seeds between overview cells
                    // so regional borders do not form visible square seams.
                    float undulation = MapConceptDetailNoise.Sample(
                        mapX * 0.18f, mapY * 0.18f, globalSeed);
                    float detail = undulation * 0.015f;
                    float mountainWeight = Mathf.Clamp((baseHeight - 0.22f) * 2.3f, 0f, 1f);
                    if (ridges.Count > 0 && mountainWeight > 0f)
                    {
                        float coarseX = Mathf.Clamp(
                            (mapX - MapConceptElevationGrid.Origin.X) / MapConceptElevationGrid.Step.X,
                            0f, MapConceptElevationGrid.Columns - 1.0001f);
                        int ownerX = Mathf.FloorToInt(coarseX);
                        float blendX = coarseX - ownerX;
                        int a = ownerY * MapConceptElevationGrid.Columns + ownerX;
                        float p00 = LocalDetail(owners[a], ridges, mapX, mapY);
                        float p10 = LocalDetail(owners[a + 1], ridges, mapX, mapY);
                        float p01 = LocalDetail(owners[a + MapConceptElevationGrid.Columns],
                            ridges, mapX, mapY);
                        float p11 = LocalDetail(owners[a + MapConceptElevationGrid.Columns + 1],
                            ridges, mapX, mapY);
                        float regionDetail = Mathf.Lerp(
                            Mathf.Lerp(p00, p10, blendX),
                            Mathf.Lerp(p01, p11, blendX), ownerBlendY);
                        detail += regionDetail * mountainWeight;
                    }

                    // Avoid applying micro-noise over the beach/sea boundary.
                    float coastFade = Mathf.Clamp(baseHeight / 0.11f, 0f, 1f);
                    float height = Mathf.Clamp(baseHeight + detail * coastFade, 0.006f, 1f);
                    master.Heights[row + x] = (ushort)Mathf.RoundToInt(height * ushort.MaxValue);
                }
            }
        }
    }

    // =========================================================
    private static float LocalDetail(int owner, List<ConceptMountainRidge> ridges,
        float mapX, float mapY)
    {
        if (owner < 0) return 0f;
        long localSeed = ridges[owner].DetailSeed;
        return MapConceptDetailNoise.Sample(mapX * 0.31f, mapY * 0.31f, localSeed) * 0.056f +
            MapConceptDetailNoise.Sample(mapX * 0.66f, mapY * 0.66f,
                localSeed ^ 0x45679A28L) * 0.018f;
    }

    // =========================================================
    // Choose a nearest dominant ridge for each *small* overview cell, rather
    // than measuring ridge distance repeatedly in the 4096-square raster.
    private static int[] RidgeOwnerGrid(MapConceptLayout layout)
    {
        var owners = new int[MapConceptElevationGrid.Columns * MapConceptElevationGrid.Rows];
        Array.Fill(owners, -1);
        for (int y = 0; y < MapConceptElevationGrid.Rows; y++)
        for (int x = 0; x < MapConceptElevationGrid.Columns; x++)
        {
            Vector2 position = MapConceptElevationGrid.Origin + new Vector2(
                x * MapConceptElevationGrid.Step.X, y * MapConceptElevationGrid.Step.Y);
            float best = float.MaxValue;
            for (int i = 0; i < layout.MountainRidges.Count; i++)
            {
                ConceptMountainRidge ridge = layout.MountainRidges[i];
                float distance = DistanceToRidge(position, ridge.Centreline) /
                    Mathf.Max(1f, ridge.HalfWidth);
                if (distance >= best || distance > 2.8f) continue;
                best = distance;
                owners[y * MapConceptElevationGrid.Columns + x] = i;
            }
        }
        return owners;
    }

    // =========================================================
    private static float DistanceToRidge(Vector2 point, Vector2[] spine)
    {
        float best = float.MaxValue;
        for (int i = 0; i < spine.Length - 1; i++)
        {
            Vector2 a = spine[i], segment = spine[i + 1] - a;
            float length = segment.LengthSquared();
            float t = length < 0.0001f ? 0f :
                Mathf.Clamp((point - a).Dot(segment) / length, 0f, 1f);
            best = Mathf.Min(best, point.DistanceTo(a + segment * t));
        }
        return best;
    }
    #endregion

    #region Cached map shading

    // =========================================================
    // Single 4096² RGBA texture; ocean pixels remain fully transparent.
    // Height/edge lighting is deliberately subtle beneath cartographic icons.
    private static ImageTexture MakeRelief(ushort[] heights, byte[] water)
    {
        const int side = MapConceptMasterElevation.Resolution;
        var pixels = new byte[side * side * 4];
        for (int y = 1; y < side - 1; y++)
        for (int x = 1; x < side - 1; x++)
        {
            int index = y * side + x;
            ushort height = heights[index];
            if (height == 0) continue;
            float h = height / (float)ushort.MaxValue;
            int dx = heights[index + 1] - heights[index - 1];
            int dy = heights[index + side] - heights[index - side];
            float light = Mathf.Clamp(0.5f + (dx + dy) / 9500f, 0f, 1f);
            float slope = Mathf.Clamp((Mathf.Abs(dx) + Mathf.Abs(dy)) / 16000f, 0f, 1f);
            int rgba = index * 4;
            if (water[index] >= MapConceptWaterCarver.River)
            {
                pixels[rgba] = 64;
                pixels[rgba + 1] = 144;
                pixels[rgba + 2] = 168;
                pixels[rgba + 3] = 218;
                continue;
            }
            pixels[rgba] = (byte)Mathf.Clamp(Mathf.RoundToInt(82f + light * 60f), 0, 255);
            pixels[rgba + 1] = (byte)Mathf.Clamp(Mathf.RoundToInt(81f + light * 55f), 0, 255);
            pixels[rgba + 2] = (byte)Mathf.Clamp(Mathf.RoundToInt(61f + light * 46f), 0, 255);
            pixels[rgba + 3] = (byte)Mathf.Clamp(
                Mathf.RoundToInt(8f + h * 56f + slope * 52f), 0, 135);
        }
        using var image = Image.CreateFromData(side, side, false, Image.Format.Rgba8, pixels);
        return ImageTexture.CreateFromImage(image);
    }
    #endregion
}
