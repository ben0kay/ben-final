// Continuous map-space land/water signed field and seeded shape composition.
using System;
using System.Collections.Generic;
using Godot;

internal sealed class MapConceptLandField : IDisposable
{
    private readonly record struct Ellipse(Vector2 Position, float RadiusX, float RadiusY, float Angle);
    private readonly record struct PathPoint(Vector2 Position, float Radius);
    private sealed class PathShape
    {
        public readonly PathPoint[] Samples;
        public PathShape(PathPoint[] samples) => Samples = samples;
    }

    // =========================================================

    private readonly FastNoiseLite _macro, _detail, _warpX, _warpY;
    private readonly List<Ellipse> _lands = new();
    private readonly List<PathShape> _arms = new(), _inlets = new();
    private readonly float _roughness, _complexity, _unionSmooth;
    private readonly Vector2 _centre;

    // =========================================================
    public MapConceptLandField(MapConceptParameters p, ulong seed)
    {
        var rng = new MapConceptSeedStream(seed);
        _roughness = p.CoastRoughness / 100f;
        _complexity = p.LandmassComplexity / 100f;
        float branching = p.BaysAndPeninsulas / 100f;
        float penetration = p.OceanPenetration / 100f;
        float scale = Mathf.Sqrt(p.LandCoverage / 55f);
        _centre = new Vector2(rng.Range(-52f, 52f), rng.Range(-30f, 30f));
        _macro = Noise(rng.NextInt(), 0.0036f, 3);
        _detail = Noise(rng.NextInt(), 0.019f, 2);
        _warpX = Noise(rng.NextInt(), 0.0035f, 2);
        _warpY = Noise(rng.NextInt(), 0.0035f, 2);
        _unionSmooth = 25f + 35f * _complexity;

        float aspect = rng.Range(0.74f, 1.28f);
        float rx = 285f * scale * Mathf.Sqrt(aspect);
        float ry = 215f * scale / Mathf.Sqrt(aspect);
        _lands.Add(new Ellipse(_centre, rx, ry, rng.Range(-0.55f, 0.55f)));

        // Separate overlapping cores break the elliptical silhouette.
        int bulges = 1 + rng.NextInt(1, 3 + (int)(_complexity * 3f));
        float firstDirection = rng.Range(0f, Mathf.Tau);
        for (int i = 0; i < bulges; i++)
        {
            float angle = firstDirection + Mathf.Tau * (i + rng.Range(-0.24f, 0.24f)) /
                Mathf.Max(1f, bulges);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float reach = rng.Range(125f, 285f) * scale;
            _lands.Add(new Ellipse(_centre + direction * reach,
                rng.Range(130f, 250f) * scale, rng.Range(90f, 190f) * scale,
                angle + rng.Range(-0.68f, 0.68f)));
        }

        // Curved tapered limbs allow coastlines with real branching and necks.
        int arms = 1 + rng.NextInt(1, 3 + (int)(_complexity * 4f + branching * 2f));
        float armPhase = rng.Range(0f, Mathf.Tau);
        for (int i = 0; i < arms; i++)
        {
            float angle = armPhase + Mathf.Tau * (i + rng.Range(-0.23f, 0.23f)) /
                Mathf.Max(1f, arms);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new(-direction.Y, direction.X);
            float length = rng.Range(290f, 510f) * scale * (0.72f + 0.45f * branching);
            float bend = rng.Range(-170f, 170f) * (0.45f + 0.55f * _complexity);
            float width = rng.Range(52f, 120f) * (1f + branching * 0.25f);
            var points = new PathPoint[10];
            for (int j = 0; j < points.Length; j++)
            {
                float t = j / (float)(points.Length - 1);
                Vector2 position = _centre + direction * Mathf.Lerp(70f, length, t) +
                    side * bend * Mathf.Sin(t * Mathf.Pi);
                float taper = Mathf.Lerp(1.24f, 0.43f, t);
                points[j] = new PathPoint(position, width * taper);
            }
            _arms.Add(new PathShape(points));
        }

        // Ocean-connected paths: a wide outer bay narrows into a winding
        // estuary-like inland channel. Future freshwater rivers are separate.
        // Always create at least one ocean-connected channel. This path starts
        // outside the map and reaches the solid central land core.
        int inlets = p.MajorOceanInlets;
        float inletPhase = rng.Range(0f, Mathf.Tau);
        for (int i = 0; i < inlets; i++)
        {
            float angle = inletPhase + Mathf.Tau * (i + rng.Range(-0.26f, 0.26f)) /
                Mathf.Max(1f, inlets);
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new(-direction.Y, direction.X);
            float endDistance = Mathf.Lerp(145f, 42f, penetration) +
                rng.Range(-16f, 27f);
            float bend = rng.Range(-200f, 200f) * (0.30f + 0.70f * penetration);
            float wiggle = rng.Range(-75f, 75f) * penetration;
            var points = new PathPoint[17];
            for (int j = 0; j < points.Length; j++)
            {
                float t = j / (float)(points.Length - 1);
                float reach = Mathf.Lerp(1000f, endDistance, t);
                Vector2 position = _centre + direction * reach +
                    side * (bend * Mathf.Sin(Mathf.Pi * t) +
                        wiggle * Mathf.Sin(3f * Mathf.Pi * t) * t);
                float mouth = Mathf.Lerp(75f, 11f, t);
                float basin = 85f * penetration *
                    Mathf.Exp(-Mathf.Pow((t - 0.57f) / 0.17f, 2f));
                points[j] = new PathPoint(position, (mouth + basin) *
                    Mathf.Lerp(0.55f, 1.20f, penetration));
            }
            _inlets.Add(new PathShape(points));
        }
    }

    // =========================================================
    // Preserve the *same* excavating paths as explicit map data. Do not
    // regenerate independent blue lines or re-roll geography in Water/.
    public List<ConceptWaterway> ExportOceanInlets()
    {
        var result = new List<ConceptWaterway>(_inlets.Count);
        for (int i = 0; i < _inlets.Count; i++)
        {
            PathPoint[] samples = _inlets[i].Samples;
            var positions = new Vector2[samples.Length];
            var widths = new float[samples.Length];
            var surfaces = new float[samples.Length];
            for (int j = 0; j < samples.Length; j++)
            {
                positions[j] = samples[j].Position;
                widths[j] = samples[j].Radius;
            }
            result.Add(new ConceptWaterway(ConceptWaterKind.OceanInlet, i + 1,
                positions, widths, surfaces));
        }
        return result;
    }

    // =========================================================
    public float Sample(Vector2 position)
    {
        float warp = 14f + _roughness * 44f + _complexity * 25f;
        Vector2 point = position + new Vector2(
            _warpX.GetNoise2D(position.X + 190f, position.Y) * warp,
            _warpY.GetNoise2D(position.X, position.Y - 370f) * warp);

        float land = float.NegativeInfinity;
        foreach (Ellipse ellipse in _lands)
            land = SmoothMax(land, EllipseDistance(point, ellipse), _unionSmooth);
        foreach (PathShape arm in _arms)
            land = SmoothMax(land, PathField(point, arm), _unionSmooth);

        land += _macro.GetNoise2D(point.X, point.Y) * (18f + 24f * _complexity);
        land += _detail.GetNoise2D(point.X, point.Y) * (4f + 22f * _roughness);

        foreach (PathShape inlet in _inlets)
            land = Mathf.Min(land, -PathField(point, inlet));

        // Sea border; gradual erosion near the frame, then guaranteed water
        // at the outer samples so marching-squares loops remain closed.
        float fx = Mathf.Max(0f, Mathf.Abs(position.X) - 632f);
        float fy = Mathf.Max(0f, Mathf.Abs(position.Y) - 386f);
        land -= fx * fx * 0.026f + fy * fy * 0.037f;
        land = Mathf.Min(land, Mathf.Min(
            757f - Mathf.Abs(position.X), 495f - Mathf.Abs(position.Y)));
        return land;
    }

    // =========================================================
    private static float EllipseDistance(Vector2 point, Ellipse e)
    {
        Vector2 delta = point - e.Position;
        float cos = Mathf.Cos(e.Angle), sin = Mathf.Sin(e.Angle);
        float x = (delta.X * cos + delta.Y * sin) / e.RadiusX;
        float y = (-delta.X * sin + delta.Y * cos) / e.RadiusY;
        return (1f - Mathf.Sqrt(x * x + y * y)) *
            Mathf.Min(e.RadiusX, e.RadiusY);
    }

    // =========================================================
    private static float PathField(Vector2 point, PathShape path)
    {
        float best = float.NegativeInfinity;
        for (int i = 0; i < path.Samples.Length - 1; i++)
        {
            PathPoint a = path.Samples[i], b = path.Samples[i + 1];
            Vector2 segment = b.Position - a.Position;
            float squared = segment.LengthSquared();
            float t = squared <= 0.00001f ? 0f :
                Mathf.Clamp((point - a.Position).Dot(segment) / squared, 0f, 1f);
            float width = Mathf.Lerp(a.Radius, b.Radius, t);
            best = Mathf.Max(best, width - point.DistanceTo(a.Position + t * segment));
        }
        return best;
    }

    // =========================================================
    private static float SmoothMax(float a, float b, float k)
    {
        if (float.IsNegativeInfinity(a)) return b;
        float h = Mathf.Clamp(0.5f + 0.5f * (a - b) / k, 0f, 1f);
        return Mathf.Lerp(b, a, h) + k * h * (1f - h);
    }

    // =========================================================
    private static FastNoiseLite Noise(int seed, float frequency, int octaves) =>
        new()
        {
            Seed = seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
            Frequency = frequency,
            FractalOctaves = octaves
        };

    // =========================================================
    public void Dispose()
    {
        _macro.Dispose();
        _detail.Dispose();
        _warpX.Dispose();
        _warpY.Dispose();
    }
}
