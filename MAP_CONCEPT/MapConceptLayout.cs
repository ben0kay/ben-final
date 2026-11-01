// Authored 2D blueprint data for a disposable island concept; no Godot scene or 3D generation dependency.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;

public enum ConceptRegionKind { PineForest, MixedForest, Highlands, Meadow, Wetlands }
public enum ConceptSiteKind { Village, Outpost, Ruin, Airstrip, Harbour }

public sealed record ConceptRegion(string Id, string Name, ConceptRegionKind Kind, Vector2[] Outline, int Density);
public sealed record ConceptSite(string Id, string Name, ConceptSiteKind Kind, Vector2 Position);
public sealed record ConceptRoute(string Id, bool IsRiver, Vector2[] Points);

public sealed class MapConceptLayout
{
    // Illustrator units are not terrain vertices; future translation may rescale them.
    public const float MapWidth = 1600f;
    public const float MapHeight = 1080f;
    public const float MetresPerUnit = 4f; // proposed 6.4 km x 4.32 km document, not actual world size.

    public readonly Vector2[] Coast;
    public readonly List<Vector2[]> Islets = new();
    public readonly List<ConceptRegion> Regions = new();
    public readonly List<ConceptSite> Sites = new();
    public readonly List<ConceptRoute> Routes = new();

    private MapConceptLayout(Vector2[] coast) => Coast = coast;

    // =========================================================
    // Layout locations are intentionally authored and seed-independent.
    // Changing the scatter seed only changes small decorative markings.
    public static MapConceptLayout CreateDefault()
    {
        var map = new MapConceptLayout(Points(
            "-672,-268 -612,-361 -526,-407 -427,-447 -315,-420 -243,-466 -151,-439 " +
            "-42,-471 66,-447 178,-424 271,-456 367,-422 454,-356 535,-329 " +
            "608,-259 620,-184 670,-142 641,-64 672,3 628,67 548,82 " +
            "497,104 392,120 334,167 380,221 481,213 557,242 579,315 " +
            "528,392 432,421 339,396 261,349 177,371 93,420 1,419 " +
            "-91,445 -189,415 -284,427 -370,383 -486,367 -569,319 " +
            "-632,242 -623,174 -673,117 -690,32 -649,-45 -676,-132 -641,-201"));

        map.Islets.Add(Points("696,203 745,186 772,213 756,257 720,266 693,245"));
        map.Islets.Add(Points("660,316 694,305 717,329 707,354 671,359 654,339"));
        map.Islets.Add(Points("-728,246 -692,247 -682,277 -707,299 -741,278"));
        map.Islets.Add(Points("714,-297 743,-328 770,-309 766,-267 736,-253 716,-276"));

        map.Regions.Add(new ConceptRegion("northern-pines", "NORTHWOOD", ConceptRegionKind.PineForest,
            Points("-315,-408 -176,-432 -29,-433 105,-414 165,-332 97,-263 -36,-271 -173,-292 -266,-307"), 280));
        map.Regions.Add(new ConceptRegion("westwood", "WESTWOOD", ConceptRegionKind.MixedForest,
            Points("-644,-118 -531,-211 -395,-221 -300,-172 -322,-72 -433,39 -576,63 -661,13"), 255));
        map.Regions.Add(new ConceptRegion("eastwood", "EASTERN WOOD", ConceptRegionKind.PineForest,
            Points("241,-242 379,-352 518,-314 599,-234 611,-95 519,-21 390,-54 304,-104"), 260));
        map.Regions.Add(new ConceptRegion("southwood", "DARK FERN WOOD", ConceptRegionKind.MixedForest,
            Points("45,197 182,192 268,247 309,345 176,373 89,407 -29,378 -74,278"), 230));
        map.Regions.Add(new ConceptRegion("western-highlands", "THE GREY SPINE", ConceptRegionKind.Highlands,
            Points("-590,-343 -460,-421 -352,-385 -248,-321 -230,-192 -304,-101 -416,-123 -517,-208"), 110));
        map.Regions.Add(new ConceptRegion("eastern-highlands", "WATCHER'S RIDGE", ConceptRegionKind.Highlands,
            Points("273,-348 414,-382 536,-315 553,-250 473,-172 334,-195 271,-265"), 72));
        map.Regions.Add(new ConceptRegion("southern-highlands", "SOUTHERN CRAGS", ConceptRegionKind.Highlands,
            Points("-540,210 -449,157 -336,182 -265,255 -291,365 -408,348 -508,296"), 76));
        map.Regions.Add(new ConceptRegion("heart-meadows", "THE LONG MEADOW", ConceptRegionKind.Meadow,
            Points("-244,-132 -96,-217 101,-199 272,-126 299,65 205,173 15,183 -167,127 -277,9"), 160));
        map.Regions.Add(new ConceptRegion("marsh", "REED FEN", ConceptRegionKind.Wetlands,
            Points("-205,234 -137,174 -46,191 10,244 -28,351 -132,376 -202,326"), 120));

        map.Routes.Add(new ConceptRoute("western-river", true, Points(
            "-388,-272 -367,-228 -336,-195 -317,-148 -289,-126 -254,-76 -221,-24 -186,31 -134,72 -102,117 -80,182 -48,244")));
        map.Routes.Add(new ConceptRoute("north-river", true, Points(
            "104,-352 78,-303 104,-271 76,-221 47,-172 55,-111 19,-58 -31,-17 -51,44 -91,101")));
        map.Routes.Add(new ConceptRoute("eastern-river", true, Points(
            "446,-243 409,-197 425,-147 398,-115 361,-70 334,-16 299,22 254,67 206,119 167,168 180,247")));
        map.Routes.Add(new ConceptRoute("west-road", false, Points(
            "-483,133 -391,83 -273,62 -144,22 -31,1 89,-6 201,-15 308,-44 375,-79")));
        map.Routes.Add(new ConceptRoute("southern-road", false, Points(
            "-391,83 -358,174 -242,245 -122,290 -3,285 134,316 266,312 380,295")));
        map.Routes.Add(new ConceptRoute("north-road", false, Points(
            "-31,1 9,-97 74,-163 183,-204 303,-213")));

        map.Sites.Add(new ConceptSite("greywatch", "GREYWATCH", ConceptSiteKind.Village, new Vector2(-279,75)));
        map.Sites.Add(new ConceptSite("stonehaven", "STONEHAVEN", ConceptSiteKind.Harbour, new Vector2(-492,133)));
        map.Sites.Add(new ConceptSite("relay", "OLD RELAY", ConceptSiteKind.Outpost, new Vector2(305,-218)));
        map.Sites.Add(new ConceptSite("mines", "ABANDONED MINE", ConceptSiteKind.Ruin, new Vector2(-363,193)));
        map.Sites.Add(new ConceptSite("airstrip", "BROKEN AIRSTRIP", ConceptSiteKind.Airstrip, new Vector2(370,292)));
        map.Sites.Add(new ConceptSite("chapel", "FALLEN CHAPEL", ConceptSiteKind.Ruin, new Vector2(-91,288)));
        return map;
    }

    // =========================================================
    // Small editable coordinate strings keep the authored outline and regions legible.
    public static Vector2[] Points(string pairs) => pairs.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair =>
        {
            string[] pieces = pair.Split(',');
            return new Vector2(float.Parse(pieces[0], CultureInfo.InvariantCulture),
                float.Parse(pieces[1], CultureInfo.InvariantCulture));
        }).ToArray();

    // =========================================================
    // Layout export: raw, editable coordinate polygons + stable IDs, not an inferred terrain heightmap.
    public string BlueprintJson(int scatterSeed)
    {
        static float[][] Coords(Vector2[] points) => points.Select(p => new[] { p.X, p.Y }).ToArray();
        var data = new
        {
            schema = 1, id = "greywake-concept-v1", scatter_seed = scatterSeed,
            map_units = new { width = MapWidth, height = MapHeight, metres_per_unit = MetresPerUnit },
            coordinate_convention = "X east, Y south (maps to world Z); map zero at island centre",
            coastline = Coords(Coast),
            islets = Islets.Select(Coords).ToArray(),
            regions = Regions.Select(z => new { id = z.Id, name = z.Name, kind = z.Kind.ToString(),
                density = z.Density, outline = Coords(z.Outline) }).ToArray(),
            routes = Routes.Select(r => new { id = r.Id, type = r.IsRiver ? "river" : "road",
                points = Coords(r.Points) }).ToArray(),
            sites = Sites.Select(s => new { id = s.Id, name = s.Name, kind = s.Kind.ToString(),
                x = s.Position.X, y = s.Position.Y }).ToArray()
        };
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    // =========================================================
    public bool IsOnLand(Vector2 point)
    {
        if (PointInPolygon(point, Coast)) return true;
        foreach (Vector2[] island in Islets) if (PointInPolygon(point, island)) return true;
        return false;
    }

    // =========================================================
    public static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }
}
