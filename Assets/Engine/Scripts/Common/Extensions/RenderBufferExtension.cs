﻿using Engine.Scripts.Core.Pooling;
using Engine.Scripts.Rendering;
using UnityEngine;
using GeometryBuffer = Engine.Scripts.Rendering.GeometryBuffer;

namespace Engine.Scripts.Common.Extensions
{
    public static class RenderBufferExtension
    {
        public static void AddIndex(this GeometryBuffer target, int offset)
        {
            target.Triangles.Add(offset);
        }

        /// <summary>
        ///     Adds triangle indices for a quad
        /// </summary>
        public static void AddIndices(this GeometryBuffer target, int offset, bool backFace)
        {
            // 0--1
            // |\ |
            // | \|
            // 3--2

            if (backFace)
            {
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 0);
                target.Triangles.Add(offset + 1);

                target.Triangles.Add(offset + 3);
                target.Triangles.Add(offset + 0);
                target.Triangles.Add(offset + 2);
            }
            else
            {
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 1);
                target.Triangles.Add(offset + 0);

                target.Triangles.Add(offset + 3);
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 0);
            }
        }

        public static void AddVertex(this GeometryBuffer target, ref VertexDataFixed vertex)
        {
            target.Vertices.Add(vertex);
        }

        /// <summary>
        ///     Adds the vertices to the render buffer.
        /// </summary>
        public static void AddVertices(this GeometryBuffer target, VertexDataFixed[] vertices)
        {
            target.Vertices.AddRange(vertices);
        }

        public static void GenerateTangents(this GeometryBuffer buffer, LocalPools pools)
        {
            var vertices = buffer.Vertices;
            var triangles = buffer.Triangles;

            var tan1 = pools.PopVector3Array(vertices.Count);
            var tan2 = pools.PopVector3Array(vertices.Count);

            for (int t = 0; t < triangles.Count; t += 3)
            {
                int i1 = triangles[t + 0];
                int i2 = triangles[t + 1];
                int i3 = triangles[t + 2];

                VertexDataFixed vd1 = vertices[i1];
                VertexDataFixed vd2 = vertices[i2];
                VertexDataFixed vd3 = vertices[i3];

                Vector3 v1 = vd1.Vertex;
                Vector3 v2 = vd2.Vertex;
                Vector3 v3 = vd3.Vertex;

                Vector2 w1 = vd1.UV;
                Vector2 w2 = vd2.UV;
                Vector2 w3 = vd3.UV;

                float x1 = v2.x - v1.x;
                float y1 = v2.y - v1.y;
                float z1 = v2.z - v1.z;

                float x2 = v3.x - v1.x;
                float y2 = v3.y - v1.y;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;

                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                // Avoid division by zero
                float div = s1 * t2 - s2 * t1;
                float r = (Mathf.Abs(div) > Mathf.Epsilon) ? (1f / div) : 0f;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (int v = 0; v < vertices.Count; ++v)
            {
                VertexDataFixed vd = vertices[v];

                Vector3 n = vd.Normal;
                Vector3 t = tan1[v];

                //Vector3 tmp = (t - n*Vector3.Dot(n, t)).normalized;
                //tangents[v] = new Vector4(tmp.x, tmp.y, tmp.z);
                Vector3.OrthoNormalize(ref n, ref t);

                vd.Tangent = new Vector4(
                    t.x, t.y, t.z,
                    (Vector3.Dot(Vector3.Cross(n, t), tan2[v]) < 0.0f) ? -1.0f : 1.0f
                    );

                tan1[v] = Vector3.zero;
                tan2[v] = Vector3.zero;
            }

            pools.PushVector3Array(tan1);
            pools.PushVector3Array(tan2);
        }
    }
}
