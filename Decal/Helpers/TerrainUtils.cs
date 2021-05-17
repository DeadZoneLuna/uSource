using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uSource.Decals
{
    public static class TerrainUtils
    {
        public static IEnumerable<Triangle> GetTriangles(Terrain[] terrains, Bounds bounds, Matrix4x4 worldToDecalMatrix)
        {
            return terrains.SelectMany(i => GetTriangles(i, bounds, worldToDecalMatrix));
        }

        private static IEnumerable<Triangle> GetTriangles(Terrain terrain, Bounds bounds, Matrix4x4 worldToDecalMatrix)
        {
            var terrainToWorldMatrix = GetLocalToWorldMatrix(terrain);
            var terrainToDecalMatrix = worldToDecalMatrix * terrainToWorldMatrix;

            bounds = Transform(terrainToWorldMatrix.inverse, bounds); // world to terrain
            Vector3Int min, max;
            GetMinMax(bounds, terrain.terrainData, out min, out max);

            return GetTriangles(terrain.terrainData, min, max).Select(i => MeshUtils.Transform(terrainToDecalMatrix, i));
        }

        private static IEnumerable<Triangle> GetTriangles(TerrainData terrain, Vector3Int min, Vector3Int max)
        {
            for (var z = min.z; z <= max.z; z++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    // 1  2
                    // 3  4
                    var v1 = terrain.GetVertex(x + 0, z + 0);
                    var v2 = terrain.GetVertex(x + 1, z + 0);
                    var v3 = terrain.GetVertex(x + 0, z + 1);
                    var v4 = terrain.GetVertex(x + 1, z + 1);

                    yield return new Triangle(v1, v3, v4);
                    yield return new Triangle(v1, v4, v2);
                }
            }
        }

        // Helpers
        private static Vector3 GetVertex(this TerrainData terrain, int x, int z)
        {
            var y = terrain.GetHeight(x, z);
            return new Vector3(x, y, z);
        }

        private static Matrix4x4 GetLocalToWorldMatrix(Terrain terrain)
        {
            var width = terrain.terrainData.heightmapWidth - 1;
            var height = terrain.terrainData.heightmapHeight - 1;
            var scale = new Vector3(terrain.terrainData.size.x / width, 1, terrain.terrainData.size.z / height);
            return Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, scale);
        }

        private static Bounds Transform(Matrix4x4 matrix, Bounds bounds)
        {
            bounds.min = matrix.MultiplyPoint(bounds.min);
            bounds.max = matrix.MultiplyPoint(bounds.max);
            return bounds;
        }

        private static Triangle Transform(Matrix4x4 matrix, Triangle triangle)
        {
            return MeshUtils.Transform(matrix, triangle);
        }

        private static void GetMinMax(Bounds bounds, TerrainData terrain, out Vector3Int min, out Vector3Int max)
        {
            min = Vector3Int.FloorToInt(bounds.min);
            max = Vector3Int.CeilToInt(bounds.max);
            min.x = Mathf.Max(min.x, 0);
            min.z = Mathf.Max(min.z, 0);
            max.x = Mathf.Min(max.x, terrain.heightmapWidth - 1);
            max.z = Mathf.Min(max.z, terrain.heightmapHeight - 1);
        }
    }
}
