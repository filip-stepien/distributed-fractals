using DistributedFractals.Core.Core;

namespace DistributedFractals.Core.Zoom;

public class KeyframeZoomSequenceGenerator : IKeyframeZoomSequenceGenerator
{
    public IEnumerable<FrameBounds> Generate(
        IFractalGeneratorOptions options,
        IReadOnlyList<ZoomKeyframe> keyframes,
        int totalFrames,
        IZoomInterpolation interpolation)
    {
        List<ZoomKeyframe> sorted = keyframes.OrderBy(k => k.T).ToList();

        FrameBounds defaultBounds = options.DefaultBounds;
        double baseReRange = defaultBounds.MaxRe - defaultBounds.MinRe;
        double baseImRange = defaultBounds.MaxIm - defaultBounds.MinIm;

        for (int i = 0; i < totalFrames; i++)
        {
            double globalT = totalFrames == 1 ? 0.0 : (double)i / (totalFrames - 1);
            double easedT = interpolation.Interpolate(globalT);

            (ZoomKeyframe kf0, ZoomKeyframe kf1, double localT) = FindSegment(sorted, easedT);

            double centerRe = Lerp(kf0.CenterRe, kf1.CenterRe, localT);
            double centerIm = Lerp(kf0.CenterIm, kf1.CenterIm, localT);
            double scale = Math.Exp(Lerp(Math.Log(kf0.Scale), Math.Log(kf1.Scale), localT));

            double halfRe = baseReRange * scale / 2.0;
            double halfIm = baseImRange * scale / 2.0;

            yield return new FrameBounds(
                centerRe - halfRe,
                centerRe + halfRe,
                centerIm - halfIm,
                centerIm + halfIm);
        }
    }

    private static (ZoomKeyframe kf0, ZoomKeyframe kf1, double localT) FindSegment(
        List<ZoomKeyframe> keyframes, double t)
    {
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            if (t <= keyframes[i + 1].T)
            {
                ZoomKeyframe kf0 = keyframes[i];
                ZoomKeyframe kf1 = keyframes[i + 1];
                double segmentLength = kf1.T - kf0.T;
                double localT = segmentLength > 0 ? (t - kf0.T) / segmentLength : 0.0;
                return (kf0, kf1, localT);
            }
        }

        ZoomKeyframe last = keyframes[^1];
        return (last, last, 0.0);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
