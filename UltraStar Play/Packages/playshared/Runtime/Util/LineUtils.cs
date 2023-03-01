using UnityEngine;

// See https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
// License: Public Domain
public static class LineUtils
{
    /**
     * Calculates the intersection of a line and a rectangle.
     */
    public static bool TryGetIntersection(
        Vector2 lineStart,
        Vector2 lineEnd,
        Rect rect,
        out Vector2 intersection)
    {
        Vector2 topLeftCorner = new Vector2(rect.xMin, rect.yMax);
        Vector2 topRightCorner = new Vector2(rect.xMax, rect.yMax);
        Vector2 bottomLeftCorner = new Vector2(rect.xMin, rect.yMin);
        Vector2 bottomRightCorner = new Vector2(rect.xMax, rect.yMin);

        // Check top
        if (TryGetIntersection(lineStart, lineEnd, topLeftCorner, topRightCorner, out intersection))
        {
            return true;
        }
        
        // Check bottom
        if (TryGetIntersection(lineStart, lineEnd, bottomLeftCorner, bottomRightCorner, out intersection))
        {
            return true;
        }
        
        // Check left
        if (TryGetIntersection(lineStart, lineEnd, bottomLeftCorner, topLeftCorner, out intersection))
        {
            return true;
        }
        
        // Check right
        if (TryGetIntersection(lineStart, lineEnd, bottomRightCorner, topRightCorner, out intersection))
        {
            return true;
        }

        return false;
    }
    
    public static bool Approximately(float a, float b, float tolerance = 1e-5f) {
        return Mathf.Abs(a - b) <= tolerance;
    }

    public static float CrossProduct2D(Vector2 a, Vector2 b) {
        return a.x * b.y - b.x * a.y;
    }

    /// <summary>
    /// Determine whether 2 lines intersect, and give the intersection point if so.
    /// </summary>
    /// <param name="p1start">Start point of the first line</param>
    /// <param name="p1end">End point of the first line</param>
    /// <param name="p2start">Start point of the second line</param>
    /// <param name="p2end">End point of the second line</param>
    /// <param name="intersection">If there is an intersection, this will be populated with the point</param>
    /// <returns>True if the lines intersect, false otherwise.</returns>
    public static bool TryGetIntersection(
        Vector2 p1start,
        Vector2 p1end,
        Vector2 p2start,
        Vector2 p2end,
        out Vector2 intersection) {
        // Consider:
        //   p1start = p
        //   p1end = p + r
        //   p2start = q
        //   p2end = q + s
        // We want to find the intersection point where :
        //  p + t*r == q + u*s
        // So we need to solve for t and u
        Vector2 p = p1start;
        Vector2 r = p1end - p1start;
        Vector2 q = p2start;
        Vector2 s = p2end - p2start;
        Vector2 qminusp = q - p;

        float cross_rs = CrossProduct2D(r, s);

        if (Approximately(cross_rs, 0f)) {
            // Parallel lines
            if (Approximately(CrossProduct2D(qminusp, r), 0f)) {
                // Co-linear lines, could overlap
                float rdotr = Vector2.Dot(r, r);
                float sdotr = Vector2.Dot(s, r);
                // this means lines are co-linear
                // they may or may not be overlapping
                float t0 = Vector2.Dot(qminusp, r / rdotr);
                float t1 = t0 + sdotr / rdotr;
                if (sdotr < 0) {
                    // lines were facing in different directions so t1 > t0, swap to simplify check
                    ObjectUtils.Swap(ref t0, ref t1);
                }

                if (t0 <= 1 && t1 >= 0) {
                    // Nice half-way point intersection
                    float t = Mathf.Lerp(Mathf.Max(0, t0), Mathf.Min(1, t1), 0.5f);
                    intersection = p + t * r;
                    return true;
                } else {
                    // Co-linear but disjoint
                    intersection = Vector2.zero;
                    return false;
                }
            } else {
                // Just parallel in different places, cannot intersect
                intersection = Vector2.zero;
                return false;
            }
        } else {
            // Not parallel, calculate t and u
            float t = CrossProduct2D(qminusp, s) / cross_rs;
            float u = CrossProduct2D(qminusp, r) / cross_rs;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) {
                intersection = p + t * r;
                return true;
            } else {
                // Lines only cross outside segment range
                intersection = Vector2.zero;
                return false;
            }
        }
    }
}
