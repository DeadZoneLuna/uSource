using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace Engine.Source
{
    //TODO
    public class MdlSpec
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct studiohdr_t
        {
            public Int32 id;
            public Int32 version;

            public Int32 checksum;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public Char[] name;

            public Int32 dataLength;

            public Vector3 eyeposition;
            public Vector3 illumposition;
            public Vector3 hull_min;
            public Vector3 hull_max;
            public Vector3 view_bbmin;
            public Vector3 view_bbmax;

            public Int32 flags;

            // mstudiobone_t
            public Int32 bone_count;
            public Int32 bone_offset;

            // mstudiobonecontroller_t
            public Int32 bonecontroller_count;
            public Int32 bonecontroller_offset;

            // mstudiohitboxset_t
            public Int32 hitbox_count;
            public Int32 hitbox_offset;

            // mstudioanimdesc_t
            public Int32 localanim_count;
            public Int32 localanim_offset;

            // mstudioseqdesc_t
            public Int32 localseq_count;
            public Int32 localseq_offset;

            public Int32 activitylistversion;
            public Int32 eventsindexed;

            // mstudiotexture_t
            public Int32 texture_count;
            public Int32 texture_offset;

            public Int32 texturedir_count;
            public Int32 texturedir_offset;

            public Int32 skinreference_count;
            public Int32 skinrfamily_count;
            public Int32 skinreference_index;

            // mstudiobodyparts_t
            public Int32 bodypart_count;
            public Int32 bodypart_offset;

            // mstudioattachment_t
            public Int32 attachment_count;
            public Int32 attachment_offset;

            public Int32 localnode_count;
            public Int32 localnode_index;
            public Int32 localnode_name_index;

            // mstudioflexdesc_t
            public Int32 flexdesc_count;
            public Int32 flexdesc_index;

            // mstudioflexcontroller_t
            public Int32 flexcontroller_count;
            public Int32 flexcontroller_index;

            // mstudioflexrule_t
            public Int32 flexrules_count;
            public Int32 flexrules_index;

            // mstudioikchain_t
            public Int32 ikchain_count;
            public Int32 ikchain_index;

            // mstudiomouth_t
            public Int32 mouths_count;
            public Int32 mouths_index;

            // mstudioposeparamdesc_t
            public Int32 localposeparam_count;
            public Int32 localposeparam_index;

            public Int32 surfaceprop_index;

            public Int32 keyvalue_index;
            public Int32 keyvalue_count;

            // mstudioiklock_t
            public Int32 iklock_count;
            public Int32 iklock_index;

            public Single mass;
            public Int32 contents;

            // mstudiomodelgroup_t
            public Int32 includemodel_count;
            public Int32 includemodel_index;

            public Int32 virtualModel;
            // Placeholder for mutable-void*

            // mstudioanimblock_t
            public Int32 animblocks_name_index;
            public Int32 animblocks_count;
            public Int32 animblocks_index;

            public Int32 animblockModel;
            // Placeholder for mutable-void*

            public Int32 bonetablename_index;

            public Int32 vertex_base;
            public Int32 offset_base;

            // Used with $constantdirectionallight from the QC
            // Model should have flag #13 set if enabled
            public Byte directionaldotproduct;

            public Byte rootLod;
            // Preferred rather than clamped

            // 0 means any allowed, N means Lod 0 -> (N-1)
            public Byte numAllowedRootLods;

            public Byte unused;
            public Int32 unused2;

            // mstudioflexcontrollerui_t
            public Int32 flexcontrollerui_count;
            public Int32 flexcontrollerui_index;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiobone_t
        {
            public Int32 sznameindex;
            public Int32 parent;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6)]
            public Int32[] bonecontroller;

            public Vector3 pos;
            public Quaternion quat;
            public Vector3 rot;

            public Vector3 posscale;
            public Vector3 rotscale;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
            public Single[] poseToBone;

            public Quaternion qAlignment;
            public Int32 flags;
            public Int32 proctype;
            public Int32 procindex;
            public Int32 physicsbone;
            public Int32 surfacepropidx;
            public Int32 contents;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public Int32[] unused;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiotexture_t
        {
            public Int32 sznameindex;
            public Int32 flags;
            public Int32 used;
            public Int32 unused1;
            public Int32 material;
            public Int32 clientmaterial;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 10)]
            public Int32[] unused;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiobodyparts_t
        {
            public Int32 sznameindex;
            public Int32 nummodels;
            public Int32 _base;
            public Int32 modelindex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiomodel_t
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public Char[] name;

            public Int32 type;

            public Single boundingradius;

            public Int32 nummeshes;
            public Int32 meshindex;

            public Int32 numvertices;
            public Int32 vertexindex;
            public Int32 tangentsindex;

            public Int32 numattachments;
            public Int32 attachmentindex;

            public Int32 numeyeballs;
            public Int32 eyeballindex;

            public mstudio_modelvertexdata_t vertexdata;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public Int32[] unused;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiomesh_t
        {
            public Int32 material;
            public Int32 modelindex;

            public Int32 numvertices;
            public Int32 vertexoffset;

            public Int32 numflexes;
            public Int32 flexoffset;

            public Int32 materialtype;
            public Int32 materialparam;

            public Int32 meshid;

            public Vector3 center;
            public mstudio_meshvertexdata_t vertexdata;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public Int32[] unused;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudio_modelvertexdata_t
        {
            public Int32 vertexdata;
            public Int32 tangentdata;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudio_meshvertexdata_t
        {
            public Int32 modelvertexdata;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public Int32[] numlodvertices;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vertexFileHeader_t
        {
            public Int32 id;
            public Int32 version;

            public Int32 checksum;

            public Int32 numLODs;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public Int32[] numLODVertexes;

            public Int32 numFixups;

            public Int32 fixupTableStart;
            public Int32 vertexDataStart;
            public Int32 tangentDataStart;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vertexFileFixup_t
        {
            public Int32 lod;
            public Int32 sourceVertexID;
            public Int32 numVertexes;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudiovertex_t
        {
            public mstudioboneweight_t m_BoneWeights;
            public Vector3 m_vecPosition;
            public Vector3 m_vecNormal;
            public Vector2 m_vecTexCoord;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mstudioboneweight_t
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public Single[] weight;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public Byte[] bone;

            public Byte numbones;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FileHeader_t
        {
            public Int32 version;

            public Int32 vertCacheSize;
            public UInt16 maxBonesPerStrip;
            public UInt16 maxBonesPerFace;
            public Int32 maxBonesPerVert;

            public Int32 checkSum;

            public Int32 numLODs;

            public Int32 materialReplacementListOffset;

            public Int32 numBodyParts;
            public Int32 bodyPartOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BodyPartHeader_t
        {
            public Int32 numModels;
            public Int32 modelOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ModelHeader_t
        {
            public Int32 numLODs;
            public Int32 lodOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ModelLODHeader_t
        {
            public Int32 numMeshes;
            public Int32 meshOffset;
            public Single switchPoint;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MeshHeader_t
        {
            public Int32 numStripGroups;
            public Int32 stripGroupHeaderOffset;
            public Byte flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct StripGroupHeader_t
        {
            public Int32 numVerts;
            public Int32 vertOffset;

            public Int32 numIndices;
            public Int32 indexOffset;

            public Int32 numStrips;
            public Int32 stripOffset;

            public Byte flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vertex_t
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public Byte[] boneWeightIndex;

            public Byte numBones;

            public UInt16 origMeshVertID;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public Byte[] boneID;
        }
    }
}
