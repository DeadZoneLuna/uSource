using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace uSource.Decals
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class Decal : MonoBehaviour
    {

        [FormerlySerializedAs("material")] public Material Material;
        [FormerlySerializedAs("sprite")] public Sprite Sprite;

        [FormerlySerializedAs("affectedLayers"), FormerlySerializedAs("AffectedLayers")] public LayerMask LayerMask = -1;
        [FormerlySerializedAs("maxAngle")] public float MaxAngle = 90.0f;
        [FormerlySerializedAs("pushDistance"), FormerlySerializedAs("PushDistance")] public float Offset = 0.009f;

        public MeshFilter MeshFilter
        {
            get
            {
                return gameObject.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            }
        }
        public MeshRenderer MeshRenderer
        {
            get
            {
                return gameObject.GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
            }
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Decal")]
        internal static void Create()
        {
            new GameObject("Decal", typeof(Decal), typeof(MeshFilter), typeof(MeshRenderer)).isStatic = true;
        }
#endif

        public void SetDirection()
        {
            float dist = (16 * uLoader.UnitScale) + 0.01f;
            List<Vector3> hits = new List<Vector3>();

            if (Physics.Raycast(transform.position - new Vector3(0, 0, 0.01f), Vector3.forward, dist))
                hits.Add(Vector3.forward);

            if (Physics.Raycast(transform.position - new Vector3(0, 0, -0.01f), Vector3.back, dist))
                hits.Add(Vector3.back);

            if (Physics.Raycast(transform.position - new Vector3(0, 0.01f, 0), Vector3.up, dist))
                hits.Add(Vector3.up);

            if (Physics.Raycast(transform.position - new Vector3(0, -0.01f, 0), Vector3.down, dist))
                hits.Add(Vector3.down);

            if (Physics.Raycast(transform.position - new Vector3(0.01f, 0, 0), Vector3.right, dist))
                hits.Add(Vector3.right);

            if (Physics.Raycast(transform.position - new Vector3(-0.01f, 0, 0), Vector3.left, dist))
                hits.Add(Vector3.left);

            //Find median point
            if (hits.Count > 0)
            {
                Vector3 minPoint = hits[0];
                Vector3 maxPoint = hits[0];

                for (int i = 1; i < hits.Count; i++)
                {
                    Vector3 pos = hits[i];
                    if (pos.x < minPoint.x)
                        minPoint.x = pos.x;
                    if (pos.x > maxPoint.x)
                        maxPoint.x = pos.x;
                    if (pos.y < minPoint.y)
                        minPoint.y = pos.y;
                    if (pos.y > maxPoint.y)
                        maxPoint.y = pos.y;
                    if (pos.z < minPoint.z)
                        minPoint.z = pos.z;
                    if (pos.z > maxPoint.z)
                        maxPoint.z = pos.z;
                }

                transform.forward = minPoint + 0.5f * (maxPoint - minPoint);
            }

            float yDefault = 180;
            if (transform.eulerAngles.x < 0)
                yDefault = -270;
            if (transform.eulerAngles.x > 0)
                yDefault = 270;

            transform.rotation *= Quaternion.Euler(0, 0, yDefault);
        }

        public void OnValidate()
        {
            if (!Material) Sprite = null;
            if (Sprite && Material.mainTexture != Sprite.texture) Sprite = null;

            MaxAngle = Mathf.Clamp(MaxAngle, 1, 180);
            Offset = Mathf.Clamp(Offset, 0.005f, 0.05f);
        }

        void Awake()
        {
            var mesh = MeshFilter.sharedMesh;
            var meshes = GameObject.FindObjectsOfType<Decal>().Select(i => i.MeshFilter.sharedMesh);
            if (meshes.Contains(mesh)) MeshFilter.sharedMesh = null; // if mesh was copied
        }

        void OnEnable()
        {
            //if (Application.isPlaying) 
            //    enabled = false;
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                BuildAndSetDirty();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            var bounds = DecalUtils.GetBounds(this);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * 0.01f);
        }


        public void BuildAndSetDirty()
        {
            DecalBuilder.Build(this);
            DecalUtils.SetDirty(this);
        }
    }
}