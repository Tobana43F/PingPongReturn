using UnityEngine;

public static class Utility
{
    public static bool IsCrossing(
        Vector2 s1,
        Vector2 e1,
        Vector2 s2,
        Vector2 e2,
        out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var vec1 = e1 - s1;
        var vec2 = e2 - s2;
        var d = Vector3.Cross(vec1, vec2).z;

        if (d == 0.0f)
        {
            return false;
        }

        var vecS1ToS2 = s2 - s1;
        var u = Vector3.Cross(vecS1ToS2, vec2).z / d;
        var v = Vector3.Cross(vecS1ToS2, vec1).z / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection = s1 + vec1 * u;
        return true;
    }

    public static Vector3 GetHermiteCurve(
        Vector3 p1,
        Vector3 v1,
        Vector3 p2,
        Vector3 v2,
        float t)
    {
        Vector3 position = Vector3.zero;
        t = Mathf.Clamp01(t);

        float h0 = ((t - 1) * (t - 1)) * (2 * t + 1);
        float h1 = (t * t) * (3 - (2 * t));
        float h2 = ((1 - t) * (1 - t)) * t;
        float h3 = (t - 1) * (t * t);

        position =
            h0 * p1 +
            h1 * p2 +
            h2 * v1 +
            h3 * v2;

        return position;
    }

    public static Vector3 Get2DBezierCurve(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        float t)
    {
        t = Mathf.Clamp01(t);

        var p01 = Vector3.Lerp(p0, p1, t);
        var p12 = Vector3.Lerp(p1, p2, t);

        var p = Vector3.Lerp(p01, p12, t);

        return p;
    }
}
