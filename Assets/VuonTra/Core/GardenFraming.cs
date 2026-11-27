using System;

namespace VuonTra.Core
{
    public readonly struct GroundPoint
    {
        public readonly double X, Z;
        public GroundPoint(double x, double z) { X = x; Z = z; }
        public override string ToString() => "(" + X + ", " + Z + ")";
    }

    public readonly struct GroundRect
    {
        public readonly double MinX, MaxX, MinZ, MaxZ;
        public GroundRect(double minX, double maxX, double minZ, double maxZ) { MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; }
        public double Width => MaxX - MinX;
        public double Depth => MaxZ - MinZ;
        public GroundPoint Centre => new GroundPoint((MinX + MaxX) / 2d, (MinZ + MaxZ) / 2d);
        public bool Contains(GroundPoint p) => p.X >= MinX && p.X <= MaxX && p.Z >= MinZ && p.Z <= MaxZ;
    }

    public readonly struct CameraPose
    {
        public readonly double X, Y, Z, PitchDegrees;
        public CameraPose(double x, double y, double z, double pitchDegrees) { X = x; Y = y; Z = z; PitchDegrees = pitchDegrees; }
    }

    public readonly struct MapFrame
    {
        public readonly GroundPoint Centre;
        public readonly double HalfHeight;
        public MapFrame(GroundPoint centre, double halfHeight) { Centre = centre; HalfHeight = halfHeight; }
    }

    public enum LabelTier { Far, Default, Near }

    public sealed class CameraSpec
    {
        public const double ZoomFar = 1920d / 2970d, ZoomDefault = 0.83d, ZoomNear = 1.5d;
        public const double DetailZoom = (ZoomDefault + ZoomNear) / 2d;

        public double FovDegrees = 25d;
        public double TiltDegrees = 40d;
        public double TileWidth = 1d;
        public double RowDepth = GardenGrid.PitchY / GardenGrid.PitchX;
        public double DefaultDistance = 35d;
        public double EdgeGive = 0.6d;
        public double GlideFadeSeconds = 0.35d;
        public double GlideLeftAtFade = 0.05d;
        public double StopSpeed = 0.05d;
        public double FrontEdgeShare = 0.15d;

        public double NearDistance => DefaultDistance * ZoomDefault / ZoomNear;
        public double FarDistance => DefaultDistance * ZoomDefault / ZoomFar;
    }

    public static class GardenFraming
    {
        const double Degrees = Math.PI / 180d;

        public static GroundPoint Ground(Hex h, CameraSpec spec) => new GroundPoint(GardenGrid.Column(h) * spec.TileWidth / 2d, -h.R * spec.RowDepth);

        public static GroundRect Island(int segments, CameraSpec spec)
        {
            double halfX = spec.TileWidth / 2d, halfZ = spec.RowDepth / 2d;
            return new GroundRect(
                -GardenGrid.Reach(segments, GardenSide.Left) * spec.TileWidth / 2d - halfX,
                GardenGrid.Reach(segments, GardenSide.Right) * spec.TileWidth / 2d + halfX,
                -GardenGrid.Reach(segments, GardenSide.Down) * spec.RowDepth - halfZ,
                GardenGrid.Reach(segments, GardenSide.Up) * spec.RowDepth + halfZ);
        }

        public static Hex TileAt(GroundPoint p, CameraSpec spec)
        {
            int row = (int)Math.Round(-p.Z / spec.RowDepth);
            double column = p.X * 2d / spec.TileWidth;
            Hex best = default;
            double bestGap = double.MaxValue;
            for (int r = row - 1; r <= row + 1; r++)
            {
                int c = r + 2 * (int)Math.Round((column - r) / 2d);
                for (int cc = c - 2; cc <= c + 2; cc += 2)
                {
                    var h = new Hex((cc - r) / 2, r);
                    GroundPoint at = Ground(h, spec);
                    double dx = at.X - p.X, dz = at.Z - p.Z, gap = dx * dx + dz * dz;
                    if (gap < bestGap) { bestGap = gap; best = h; }
                }
            }
            return best;
        }

        public static CameraPose Pose(GroundPoint focus, double distance, CameraSpec spec)
        {
            double tilt = spec.TiltDegrees * Degrees;
            return new CameraPose(focus.X, distance * Math.Sin(tilt), focus.Z - distance * Math.Cos(tilt), spec.TiltDegrees);
        }

        public static double HalfWidthAt(double distance, double aspect, CameraSpec spec) => distance * Math.Tan(spec.FovDegrees * Degrees / 2d) * aspect;

        public static double DistanceOf(double zoom, CameraSpec spec) => spec.DefaultDistance * CameraSpec.ZoomDefault / zoom;
        public static double ZoomOf(double distance, CameraSpec spec) => spec.DefaultDistance * CameraSpec.ZoomDefault / distance;
        public static bool ShowsDetail(double distance, CameraSpec spec) => ZoomOf(distance, spec) > CameraSpec.DetailZoom;

        public static LabelTier TierOf(double distance, CameraSpec spec)
        {
            if (ShowsDetail(distance, spec)) return LabelTier.Near;
            return ZoomOf(distance, spec) < (CameraSpec.ZoomFar + CameraSpec.ZoomDefault) / 2d ? LabelTier.Far : LabelTier.Default;
        }

        public static double Pinch(double distance, double startSpan, double span, CameraSpec spec)
        {
            if (startSpan <= 0d || span <= 0d) return distance;
            return Math.Max(spec.NearDistance, Math.Min(spec.FarDistance, distance * startSpan / span));
        }

        public static GroundPoint ZoomAbout(GroundPoint focus, GroundPoint anchor, double fromDistance, double toDistance)
        {
            double k = toDistance / fromDistance;
            return new GroundPoint(anchor.X + (focus.X - anchor.X) * k, anchor.Z + (focus.Z - anchor.Z) * k);
        }

        public static GroundPoint Drag(GroundPoint wanted, GroundRect island, CameraSpec spec) =>
            new GroundPoint(Give(wanted.X, island.MinX, island.MaxX, spec.EdgeGive), Give(wanted.Z, island.MinZ, island.MaxZ, spec.EdgeGive));

        public static double FrontReach(double distance, CameraSpec spec)
        {
            double tilt = spec.TiltDegrees * Degrees;
            double below = Math.Atan((1d - 2d * spec.FrontEdgeShare) * Math.Tan(spec.FovDegrees * Degrees / 2d));
            return distance * Math.Cos(tilt) - distance * Math.Sin(tilt) / Math.Tan(tilt + below);
        }

        public static GroundRect FocusArea(GroundRect island, double front, double distance, CameraSpec spec)
        {
            double minZ = Math.Min(island.MaxZ, front + FrontReach(distance, spec));
            return new GroundRect(island.MinX, island.MaxX, minZ, island.MaxZ);
        }

        public static GroundPoint Settle(GroundPoint held, GroundRect island) =>
            new GroundPoint(Math.Max(island.MinX, Math.Min(island.MaxX, held.X)), Math.Max(island.MinZ, Math.Min(island.MaxZ, held.Z)));

        static double Give(double v, double min, double max, double give)
        {
            if (v > max) return max + Soft(v - max, give);
            if (v < min) return min - Soft(min - v, give);
            return v;
        }

        static double Soft(double over, double give) => give * over / (over + give);

        public static double Glide(double speed, double seconds, CameraSpec spec, bool reducedMotion)
        {
            if (reducedMotion) return 0d;
            double rate = -Math.Log(spec.GlideLeftAtFade) / spec.GlideFadeSeconds;
            double next = speed * Math.Exp(-rate * seconds);
            return Math.Abs(next) < spec.StopSpeed ? 0d : next;
        }

        public static double GlideReach(double speed, CameraSpec spec, bool reducedMotion)
        {
            if (reducedMotion || Math.Abs(speed) < spec.StopSpeed) return 0d;
            double rate = -Math.Log(spec.GlideLeftAtFade) / spec.GlideFadeSeconds;
            return Math.Sign(speed) * (Math.Abs(speed) - spec.StopSpeed) / rate;
        }

        public static double GlideSeconds(double speed, CameraSpec spec, bool reducedMotion)
        {
            if (reducedMotion || Math.Abs(speed) < spec.StopSpeed) return 0d;
            double rate = -Math.Log(spec.GlideLeftAtFade) / spec.GlideFadeSeconds;
            return Math.Log(Math.Abs(speed) / spec.StopSpeed) / rate;
        }

        public static MapFrame Map(GroundRect island, double aspect) =>
            new MapFrame(island.Centre, Math.Max(island.Depth / 2d, island.Width / 2d / aspect));

        public static GroundRect Pad(GroundRect area, double margin) =>
            new GroundRect(area.MinX - margin, area.MaxX + margin, area.MinZ - margin, area.MaxZ + margin);

        public static GroundPoint MapPoint(MapFrame frame, double aspect, double u, double v) =>
            new GroundPoint(frame.Centre.X + (u - 0.5d) * 2d * frame.HalfHeight * aspect, frame.Centre.Z + (v - 0.5d) * 2d * frame.HalfHeight);

        public const double TapSlopUnits = 12d;
        public const double DoubleTapSeconds = 0.35d, DoubleTapReachUnits = 48d;
        public const double CanvasWidth = 1080d, CanvasHeight = 1920d;

        public static double CanvasScale(double screenWidth, double screenHeight) =>
            Math.Sqrt(screenWidth / CanvasWidth * (screenHeight / CanvasHeight));

        public static double TapSlopPixels(double screenWidth, double screenHeight) => TapSlopUnits * CanvasScale(screenWidth, screenHeight);

        public static bool IsTap(double dx, double dy, double screenWidth, double screenHeight)
        {
            double slop = TapSlopPixels(screenWidth, screenHeight);
            return dx * dx + dy * dy <= slop * slop;
        }

        public static bool IsDoubleTap(double secondsApart, double dx, double dy, double screenWidth, double screenHeight)
        {
            double reach = DoubleTapReachUnits * CanvasScale(screenWidth, screenHeight);
            return secondsApart >= 0d && secondsApart <= DoubleTapSeconds && dx * dx + dy * dy <= reach * reach;
        }

        public static double DoubleTapDistance(double distance, CameraSpec spec) =>
            ShowsDetail(distance, spec) ? spec.DefaultDistance : spec.NearDistance;

        public static double NextZoomStep(double distance, CameraSpec spec)
        {
            double[] steps = { spec.FarDistance, spec.DefaultDistance, spec.NearDistance };
            int nearest = 0;
            for (int i = 1; i < steps.Length; i++)
                if (Math.Abs(Math.Log(distance / steps[i])) < Math.Abs(Math.Log(distance / steps[nearest]))) nearest = i;
            return steps[(nearest + 1) % steps.Length];
        }
    }
}
