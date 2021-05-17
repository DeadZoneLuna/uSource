using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uSource.Decals
{
    class MeshBuilder
    {

        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> indices = new List<int>();

        public void AddPolygon(Vector3[] poly)
        {
            var ind1 = AddVertex_(poly[0]);

            for (var i = 1; i < poly.Length - 1; i++)
            {
                var ind2 = AddVertex_(poly[i]);
                var ind3 = AddVertex_(poly[i + 1]);

                indices.Add(ind1);
                indices.Add(ind2);
                indices.Add(ind3);
            }
        }

        public Mesh ToMesh(Mesh mesh, Rect uvRect, float offset)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "Decal";
            }

            ToMesh_(mesh, uvRect, offset);
            return mesh;
        }

        private void ToMesh_(Mesh mesh, Rect uvRect, float offset)
        {
            mesh.Clear(true);
            if (indices.Count == 0) return;

            var vertices_ = vertices.ToArray();
            var indices_ = indices.ToArray();
            var normals = GetNormals(vertices_, indices_);
            var uvs = GetUVs(vertices_, uvRect);
            Push(vertices_, normals, offset);

            mesh.vertices = vertices_;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices_;
        }

        public void Clear()
        {
            vertices.Clear();
            indices.Clear();
        }

        // Helpers
        private int AddVertex_(Vector3 vertex)
        {
            var index = FindVertex(vertex);
            if (index == -1)
            {
                vertices.Add(vertex);
                return vertices.Count - 1;
            }
            else
            {
                return index;
            }
        }

        private int FindVertex(Vector3 vertex)
        {
            const float Epsilon = 0.01f;
            return vertices.FindIndex(i => Vector3.Distance(i, vertex) < Epsilon);
        }

        private static Vector3[] GetNormals(Vector3[] vertices, int[] indices)
        {
            var normals = new Vector3[vertices.Length];

            for (var i = 0; i < indices.Length; i += 3)
            {
                var ind1 = indices[i];
                var ind2 = indices[i + 1];
                var ind3 = indices[i + 2];

                var v1 = vertices[ind1];
                var v2 = vertices[ind2];
                var v3 = vertices[ind3];

                var n = GetNormal(v1, v2, v3);

                normals[ind1] += n;
                normals[ind2] += n;
                normals[ind3] += n;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i].Normalize();
            }

            return normals;
        }

        private static Vector3 GetNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            return Vector3.Cross(v2 - v1, v3 - v1).normalized;
        }

        private static Vector2[] GetUVs(Vector3[] vertices, Rect uvRect)
        {
            return vertices.Select(i => GetUVs(i, uvRect)).ToArray();
        }

        private static Vector2 GetUVs(Vector3 vertex, Rect uvRect)
        {
            var u = Mathf.Lerp(uvRect.xMin, uvRect.xMax, vertex.x + 0.5f);
            var v = Mathf.Lerp(uvRect.yMin, uvRect.yMax, vertex.y + 0.5f);
            return new Vector2(u, v);
        }

        private static void Push(Vector3[] vertices, Vector3[] normals, float offset)
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] += normals[i] * offset;
            }
        }
    }
}