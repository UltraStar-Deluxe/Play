/* MIT License

Copyright (c) 2022 David Tattersall 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;

[UxmlElement]
public partial class Shadow : VisualElement
{

    //private Vertex[] k_Vertices;
    private NativeArray<Vertex> k_Vertices;
    private Color clearColor;

    [UxmlAttribute("shadow-corner-radius")]
    public int shadowCornerRadius { get; set; } = 10;

    [UxmlAttribute("shadow-scale")]
    public float shadowScale { get; set; } = 1.1f;

    [UxmlAttribute("shadow-offset-x")]
    public int shadowOffsetX { get; set; } = 0;

    [UxmlAttribute("shadow-offset-y")]
    public int shadowOffsetY { get; set; } = 0;

    public int shadowCornerSubdivisions => 3;

    // Deprecated
    //[UnityEngine.Scripting.Preserve]
    //public new class UxmlFactory : UxmlFactory<Shadow, UxmlTraits> { }

    //public new class UxmlTraits : VisualElement.UxmlTraits
    //{

    //    public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
    //    {
    //        get { yield break; }
    //    }

    //    // Rounded corner radius. Increase to make the shadow "fluffier"
    //    UxmlIntAttributeDescription radiusAttr =
    //        new UxmlIntAttributeDescription { name = "shadow-corner-radius", defaultValue = 10 };

    //    // Scale. Increase to make the shadow extend farther away from the element.
    //    UxmlFloatAttributeDescription scaleAttr =
    //        new UxmlFloatAttributeDescription { name = "shadow-scale", defaultValue = 1.1f };

    //    // Offsets. Tweak to have e.g. a shadow below and to the right of an element.
    //    UxmlIntAttributeDescription offsetXAttr =
    //        new UxmlIntAttributeDescription { name = "shadow-offset-x", defaultValue = 0 };

    //    UxmlIntAttributeDescription offsetYAttr =
    //        new UxmlIntAttributeDescription { name = "shadow-offset-y", defaultValue = 0 };

    //    // Buggy right now - always set to 3.
    //    /*UxmlIntAttributeDescription subdivisionsAttr =
    //        new UxmlIntAttributeDescription { name="shadow-corner-subdivisions", defaultValue = 3};*/

    //    public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
    //    {
    //        base.Init(ve, bag, cc);
    //        var shadow = ve as Shadow;

    //        shadow.shadowCornerRadius = radiusAttr.GetValueFromBag(bag, cc);
    //        shadow.shadowScale = scaleAttr.GetValueFromBag(bag, cc);
    //        shadow.shadowOffsetX = offsetXAttr.GetValueFromBag(bag, cc);
    //        shadow.shadowOffsetY = offsetYAttr.GetValueFromBag(bag, cc);
    //        //shadow.shadowCornerSubdivisions = subdivisionsAttr.GetValueFromBag(bag, cc);
    //    }
    //}

    public Shadow()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        clearColor = new(resolvedStyle.color.r, resolvedStyle.color.g, resolvedStyle.color.b, 0);

        Rect r = contentRect;

        float left = 0;
        float right = r.width;
        float top = 0;
        float bottom = r.height;
        float halfSpread = (shadowCornerRadius / 2f);
        int curveSubdivisions = this.shadowCornerSubdivisions;
        int totalVertices = 12 + ((curveSubdivisions - 1) * 4);

        /*

        4/5/6/7 = inset rectangle (rect-shadowInsetAmount)
        0/1/2/3/8/9/10/11 = outset rectangle (rect+shadowSpread)

            1        2     12 => 12+(subdivisions-1)
           \|         /
       10 - 5========6 - 11
            |        |
            |        |
            |        |
            |        |
        9 - 4========7 - 8
           /          \
            0        3     (12+subdivisions-1)+1 => 12 + 2*(subdivisions-1) + 1

        */


        k_Vertices = new NativeArray<Vertex>(totalVertices, Allocator.Temp);

        var vertex = k_Vertices[0];
        vertex.position = new Vector3(left + halfSpread, bottom + halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[0] = vertex;

        vertex = k_Vertices[1];
        vertex.position = new Vector3(left + halfSpread, top - halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[1] = vertex;

        vertex = k_Vertices[2];
        vertex.position = new Vector3(right - halfSpread, top - halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[2] = vertex;

        vertex = k_Vertices[3];
        vertex.position = new Vector3(right - halfSpread, bottom + halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[3] = vertex;

        vertex = k_Vertices[8];
        vertex.position = new Vector3(right + halfSpread, bottom - halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[8] = vertex;

        vertex = k_Vertices[9];
        vertex.position = new Vector3(left - halfSpread, bottom - halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[9] = vertex;

        vertex = k_Vertices[10];
        vertex.position = new Vector3(left - halfSpread, top + halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[10] = vertex;

        vertex = k_Vertices[11];
        vertex.position = new Vector3(right + halfSpread, top + halfSpread, Vertex.nearZ);
        vertex.tint = clearColor;
        k_Vertices[11] = vertex;

        // Inside rectangle
        vertex = k_Vertices[4];
        vertex.position = new Vector3(0 + halfSpread, r.height - halfSpread, Vertex.nearZ);
        vertex.tint = resolvedStyle.color;
        k_Vertices[4] = vertex;

        vertex = k_Vertices[5];
        vertex.position = new Vector3(0 + halfSpread, 0 + halfSpread, Vertex.nearZ);
        vertex.tint = resolvedStyle.color;
        k_Vertices[5] = vertex;

        vertex = k_Vertices[6];
        vertex.position = new Vector3(r.width - halfSpread, 0 + halfSpread, Vertex.nearZ);
        vertex.tint = resolvedStyle.color;
        k_Vertices[6] = vertex;

        vertex = k_Vertices[7];
        vertex.position = new Vector3(r.width - halfSpread, r.height - halfSpread, Vertex.nearZ);
        vertex.tint = resolvedStyle.color;
        k_Vertices[7] = vertex;

        // Top right corner
        for (int i = 0; i < curveSubdivisions - 1; i++)
        {
            int vertexId = 12 + i;
            float angle = (Mathf.PI * 0.5f / curveSubdivisions) + (Mathf.PI * 0.5f / curveSubdivisions) * i;
            var vert = k_Vertices[vertexId];
            vert.position = new Vector3(r.width - halfSpread + Mathf.Sin(angle) * shadowCornerRadius, 0 + halfSpread + (-Mathf.Cos(angle) * shadowCornerRadius), Vertex.nearZ);
            vert.tint = clearColor;
            k_Vertices[vertexId] = vert;
        }

        // Bottom right corner
        for (int i = 0; i < curveSubdivisions - 1; i++)
        {
            int vertexId = 12 + i + (curveSubdivisions - 1);
            float angle = (Mathf.PI * 0.5f) + (Mathf.PI * 0.5f / curveSubdivisions) + (Mathf.PI * 0.5f / curveSubdivisions) * i;
            var vert = k_Vertices[vertexId];
            vert.position = new Vector3(r.width - halfSpread + Mathf.Sin(angle) * shadowCornerRadius, r.height - halfSpread + (-Mathf.Cos(angle) * shadowCornerRadius), Vertex.nearZ);
            vert.tint = clearColor;
            k_Vertices[vertexId] = vert;
        }

        // Bottom left corner
        for (int i = 0; i < curveSubdivisions - 1; i++)
        {
            int vertexId = 12 + i + (curveSubdivisions - 1) * 2;
            float angle = (Mathf.PI) + (Mathf.PI * 0.5f / curveSubdivisions) + (Mathf.PI * 0.5f / curveSubdivisions) * i;

            var vert = k_Vertices[vertexId];
            vert.position = new Vector3(0 + halfSpread + Mathf.Sin(angle) * shadowCornerRadius, r.height - halfSpread + (-Mathf.Cos(angle) * shadowCornerRadius), Vertex.nearZ);
            vert.tint = clearColor;
            k_Vertices[vertexId] = vert;
        }

        // Top left corner
        for (int i = 0; i < curveSubdivisions - 1; i++)
        {
            int vertexId = 12 + i + (curveSubdivisions - 1) * 3;
            float angle = (Mathf.PI * 1.5f) + (Mathf.PI * 0.5f / curveSubdivisions) + (Mathf.PI * 0.5f / curveSubdivisions) * i;

            var vert = k_Vertices[vertexId];
            vert.position = new Vector3(0 + halfSpread + Mathf.Sin(angle) * shadowCornerRadius, 0 + halfSpread + (-Mathf.Cos(angle) * shadowCornerRadius), Vertex.nearZ);
            vert.tint = clearColor;
            k_Vertices[vertexId] = vert;
        }

        Vector3 dimensions = new Vector3(r.width, r.height, Vertex.nearZ);

        for (int i = 0; i < k_Vertices.Length; i++)
        {
            // Do not scale the inner rectangle
            var vert = k_Vertices[i];
            vert.position = vert.position + new Vector3(shadowOffsetX, shadowOffsetY, 0);

            if (i >= 4 && i <= 7)
            {
                // Do nothing
            }
            else
            {
                vert.position = ((vert.position - (dimensions * 0.5f)) * shadowScale) + (dimensions * 0.5f);
            }
            // Scale verticles using scale factor
            k_Vertices[i] = vert;
        }

        List<ushort> tris = new List<ushort>();
        tris.AddRange(new ushort[]{
            1,6,5,
            2,6,1,
            6,11,8,
            6,8,7,
            4,7,3,
            4,3,0,
            10,5,4,
            10,4,9,
            5,6,4,
            6,7,4,
        });

        for (ushort i = 0; i < curveSubdivisions; i++)
        {
            if (i == 0)
            {
                tris.AddRange(new ushort[] { 2, 12, 6 });
            }
            else if (i == curveSubdivisions - 1)
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1), 11, 6 });
            }
            else
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1), (ushort) (12 + i), 6 });
            }
        }
        for (ushort i = 0; i < curveSubdivisions; i++)
        {
            if (i == 0)
            {
                tris.AddRange(new ushort[] { 7, 8, 14 });
            }
            else if (i == curveSubdivisions - 1)
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + (curveSubdivisions - 1)), 3, 7 });
            }
            else
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + (curveSubdivisions - 1)), (ushort) (12 + i + (curveSubdivisions - 1)), 7 });
            }
        }
        for (ushort i = 0; i < curveSubdivisions; i++)
        {
            if (i == 0)
            {
                tris.AddRange(new ushort[] { 4, 0, 16 });
            }
            else if (i == curveSubdivisions - 1)
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + 2 * (curveSubdivisions - 1)), 9, 4 });
            }
            else
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + 2 * (curveSubdivisions - 1)), (ushort) (12 + i + (2 * (curveSubdivisions - 1))), 4 });
            }
        }
        for (ushort i = 0; i < curveSubdivisions; i++)
        {
            if (i == 0)
            {
                tris.AddRange(new ushort[] { 5, 10, 18 });
            }
            else if (i == curveSubdivisions - 1)
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + 3 * (curveSubdivisions - 1)), 1, 5 });
            }
            else
            {
                tris.AddRange(new ushort[] { (ushort) (12 + i - 1 + 3 * (curveSubdivisions - 1)), (ushort) (12 + i + 3 * (curveSubdivisions - 1)), 5 });
            }
        }

        MeshWriteData mwd = ctx.Allocate(k_Vertices.Length, tris.Count);
        mwd.SetAllVertices(k_Vertices);
        mwd.SetAllIndices(tris.ToArray());

        k_Vertices.Dispose();
    }
}
