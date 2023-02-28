using UnityEngine;

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
    
    /**
     * Calculates the intersection point of two lines.
     * See https://stackoverflow.com/a/5222390/4412885
     */
    public static bool TryGetIntersection(
        Vector2 line1Start,
        Vector2 line1End,
        Vector2 line2Start,
        Vector2 line2End,
        out Vector2 intersectionPoint)
    {
        Vector2 b = line1End - line1Start;
        Vector2 d = line2End - line2Start;
        float bDotDPerp = b.x * d.y - b.y * d.x;

        // if b dot d == 0, it means the lines are parallel so have infinite intersection points
        if (bDotDPerp == 0)
        {
            intersectionPoint = Vector2.zero;
            return false;
        }

        Vector2 c = line2Start - line1Start;
        float t = (c.x * d.y - c.y * d.x) / bDotDPerp;
        if (t < 0 || t > 1)
        {
            intersectionPoint = Vector2.zero;
            return false;
        }

        float u = (c.x * b.y - c.y * b.x) / bDotDPerp;
        if (u is < 0 or > 1)
        {
            intersectionPoint = Vector2.zero;
            return false;
        }

        intersectionPoint = line1Start + t * b;
        return true;
    }
}
