using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uSource.Decals
{
    static class DecalBuilder
    {
        private static readonly MeshBuilder Builder = new MeshBuilder();

        public static void Build(Decal decal)
        {
            Build(Builder, decal);
        }

        private static void Build(MeshBuilder builder, Decal decal)
        {
            var filter = decal.MeshFilter;
            var renderer = decal.MeshRenderer;

            if (decal.Material && decal.Sprite)
            {
                builder.Clear();
                Build_(builder, decal);
                filter.sharedMesh = builder.ToMesh(filter.sharedMesh, GetUVRect(decal.Sprite), decal.Offset);
                renderer.sharedMaterial = decal.Material;
            }
            else
            {
                Object.DestroyImmediate(filter.sharedMesh);
                filter.sharedMesh = null;
                renderer.sharedMaterial = null;
            }
        }

        private static void Build_(MeshBuilder builder, Decal decal)
        {
            var objects = DecalUtils.GetAffectedObjects(decal);
            var terrains = DecalUtils.GetAffectedTerrains(decal);
            var bounds = DecalUtils.GetBounds(decal);
            var worldToDecalMatrix = decal.transform.worldToLocalMatrix;

            var triangles1 = MeshUtils.GetTriangles(objects, worldToDecalMatrix).Where(i => Filter(i, decal));
            var triangles2 = TerrainUtils.GetTriangles(terrains, bounds, worldToDecalMatrix).Where(i => Filter(i, decal));

            AddTriangles(builder, triangles1);
            AddTriangles(builder, triangles2);
        }

        // Add
        private static void AddTriangles(MeshBuilder builder, IEnumerable<Triangle> triangles)
        {
            foreach (var triangle in triangles)
            {
                AddTriangle(builder, triangle);
            }
        }

        private static void AddTriangle(MeshBuilder builder, Triangle triangle)
        {
            var poly = PolygonUtils.Clip(triangle.V1, triangle.V2, triangle.V3);
            if (poly.Length > 0) builder.AddPolygon(poly);
        }

        // Helpers
        private static bool Filter(Triangle triangle, Decal decal)
        {
            var normal = GetNormal(triangle);
            return Vector3.Angle(Vector3.back, normal) <= decal.MaxAngle;
        }

        private static Vector3 GetNormal(Triangle triangle)
        {
            return Vector3.Cross(triangle.V2 - triangle.V1, triangle.V3 - triangle.V1).normalized;
        }

        private static Rect GetUVRect(Sprite sprite)
        {
            return ToRect01(sprite.rect, sprite.texture);
        }

        private static Rect ToRect01(Rect rect, Texture2D texture)
        {
            //rect.x /= texture.width;
            //rect.y /= texture.height;
            rect.width /= texture.width;
            rect.height /= texture.height;
            return rect;
        }
    }
}