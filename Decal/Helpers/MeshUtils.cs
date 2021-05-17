using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uSource.Decals
{
    public struct Triangle
    {
        public readonly Vector3 V1, V2, V3;
        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }
    }

    public static class MeshUtils
    {

        public static IEnumerable<Triangle> GetTriangles(MeshFilter[] objects, Matrix4x4 worldToDecalMatrix)
        {
            return objects.SelectMany(i => GetTriangles(i, worldToDecalMatrix));
        }
        private static IEnumerable<Triangle> GetTriangles(MeshFilter obj, Matrix4x4 worldToDecalMatrix)
        {
            var objToDecalMatrix = worldToDecalMatrix * obj.transform.localToWorldMatrix;
            return GetTriangles(obj.sharedMesh).Select(i => Transform(objToDecalMatrix, i));
        }
        private static IEnumerable<Triangle> GetTriangles(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var i1 = triangles[i];
                var i2 = triangles[i + 1];
                var i3 = triangles[i + 2];

                var v1 = vertices[i1];
                var v2 = vertices[i2];
                var v3 = vertices[i3];

                yield return new Triangle(v1, v2, v3);
            }
        }

        // Helpers
        internal static Triangle Transform(Matrix4x4 matrix, Triangle triangle)
        {
            var v1 = matrix.MultiplyPoint(triangle.V1);
            var v2 = matrix.MultiplyPoint(triangle.V2);
            var v3 = matrix.MultiplyPoint(triangle.V3);
            return new Triangle(v1, v2, v3);
        }
    }
}