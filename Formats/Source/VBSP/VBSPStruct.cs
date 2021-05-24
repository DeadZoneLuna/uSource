using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace uSource.Formats.Source.VBSP
{
    public class VBSPStruct : VBSPLump
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dheader_t
        {
            public Int32 Ident;
            // BSP file identifier
            public Int32 Version;
            // BSP file version

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public lump_t[] Lumps;
            // lump directory array

            public Int32 MapRevision;
            // the map's revision (iteration, version) number

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct lump_t
            {
                public Int32 FileOfs;
                // offset into file (bytes)
                public Int32 FileLen;
                // length of lump (bytes)
                public Int32 Version;
                // lump format version
                public Int32 FourCC;
                // lump ident code
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dedge_t
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UInt16[] V;
            // vertex indices
        }

        /// <summary>
        /// sizeof = 56
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dface_t
        {
            public UInt16 Planenum;
            // the plane number

            public Byte Side;
            // faces opposite to the node's plane direction
            public Byte OnNode;
            // 1 of on node, 0 if in leaf

            public Int32 FirstEdge;
            // index into surfedges
            public Int16 NumEdges;
            // number of surfedges
            public Int16 TexInfo;
            // texture info
            public Int16 DispInfo;
            // displacement info

            public Int16 SurfaceFogVolumeID;
            // ?

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] Styles;
            // switchable lighting info

            public Int32 LightOfs;
            // offset into lightmap lump
            public Single Area;
            // face area in units^2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int32[] LightmapTextureMinsInLuxels;
            // texture lighting info

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int32[] LightmapTextureSizeInLuxels;
            // texture lighting info

            public Int32 OrigFace;
            // original face this was split from

            public UInt16 NumPrims;
            // primitives
            public UInt16 FirstPrimID;
            public uint SmoothingGroups;
            // lightmap smoothing group
        }

        /// <summary>
        /// sizeof = 72
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct texinfo_t
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector4[] TextureVecs;
            // [s/t][xyz offset]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector4[] LightmapVecs;
            // [s/t][xyz offset] - length is in units of texels/area

            public SurfFlags Flags;
            // miptex flags overrides
            public Int32 TexData;
            // Pointer to texture name, size, etc.

            public enum SurfFlags
            {
                SURF_LIGHT = 0x0001,
                // value will hold the light strength
                SURF_SLICK = 0x0002,
                // effects game physics
                SURF_SKY = 0x0004,
                // don't draw, but add to skybox
                SURF_WARP = 0x0008,
                // turbulent water warp
                SURF_TRANS = 0x0010,
                // surface is transparent
                SURF_WET = 0x0020,
                // the surface is wet
                SURF_FLOWING = 0x0040,
                // scroll towards angle
                SURF_NODRAW = 0x0080,
                // don't bother referencing the texture
                SURF_HINT = 0x0100,
                // make a primary bsp splitter
                SURF_SKIP = 0x0200,
                // completely ignore, allowing non-closed brushes
                SURF_NOLIGHT = 0x0400,
                // Don't calculate light on this surface
                SURF_BUMPLIGHT = 0x0800,
                // calculate three lightmaps for the surface for bumpmapping
                SURF_NOSHADOWS = 0x1000,
                // Don't receive shadows
                SURF_NODECALS = 0x2000,
                // Don't receive decals
                SURF_NOCHOP = 0x4000,
                // Don't subdivide patches on this surface
                SURF_HITBOX = 0x8000
                // surface is part of a hitbox
            }
        }

        /// <summary>
        /// sizeof = 32
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dtexdata_t
        {
            public Vector3 Reflectivity;
            // RGB reflectivity
            public Int32 NameStringTableID;
            // index into TexdataStringTable
            public Int32 Width, Height;
            // source image
            public Int32 View_Width, View_Height;
        }

        /// <summary>
        /// sizeof = 48
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dmodel_t
        {
            public Vector3 Mins, Maxs;
            // bounding box
            public Vector3 Origin;
            // for sounds or lights
            public Int32 HeadNode;
            // index into node array
            public Int32 FirstFace, NumFaces;
            // index into face array
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dphysmodel_t
        {
            Int32 modelIndex;  // Perhaps the index of the model to which this physics model applies?
            Int32 dataSize;    // Total size of the collision data sections
            Int32 keydataSize; // Size of the text section
            Int32 solidCount;  // Number of collision data sections
        }

        /// <summary>
        /// sizeof = 352
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct doverlay_t
        {
			//Special ID
            public Int32 Id;
			//Texture Info
            public Int16 TexInfo;
            public UInt16 FaceCountAndRenderOrder;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public Int32[] Ofaces;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector2 U;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Vector2 V;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Vector3[] UVPoints;

            public Vector3 Origin;
            public Vector3 BasisNormal;
        }

        /// <summary>
        /// sizeof = 16
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dgamelump_t
        {
            public Int32 Id;
            // gamelump ID
            public UInt16 Flags;
            // flags
            public UInt16 Version;
            // gamelump version
            public Int32 FileOfs;
            // offset to this gamelump
            public Int32 FileLen;
            // length
        }

        //Displacments

        /// <summary>
        /// sizeof = 176
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ddispinfo_t
        {
            public Vector3 StartPosition;
            // start position used for orientation
            public Int32 DispVertStart;
            // Index into LUMP_DISP_VERTS.
            public Int32 DispTriStart;
            // Index into LUMP_DISP_TRIS.
            public Int32 Power;
            // power - indicates size of surface (2^power 1)
            public Int32 MinTess;
            // minimum tesselation allowed
            public Single SmoothingAngle;
            // lighting smoothing angle
            public Int32 Contents;
            // surface contents
            public UInt16 MapFace;
            // Which map face this displacement comes from.
            public Int32 LightmapAlphaStart;
            // Index into ddisplightmapalpha.
            public Int32 LightmapSamplePositionStart;
            // Index into LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 130)]
            public Byte[] Unknown;
        }

        /// <summary>
        /// sizeof = 20
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dDispVert
        {
            public Vector3 Vec;
            // Vector field defining displacement volume.
            public Single Dist;
            // Displacement distances.
            public Single Alpha;
            // "per vertex" alpha values.
        }

        //Displacments

        //LIGHTS

        // lights that were used to illuminate the world
        [Flags]
        public enum emittype_t
        {
            emit_surface,       // 90 degree spotlight
            emit_point,         // simple point light source
            emit_spotlight,     // spotlight with penumbra
            emit_skylight,      // directional light with no falloff (surface must trace to SKY texture)
            emit_quakelight,    // linear falloff, non-lambertian
            emit_skyambient,    // spherical light source with no falloff (surface must trace to SKY texture)
        };

        /// <summary>
        /// LUMP - 15 (LDR) or 54 (HDR)
        /// sizeof = 88
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct dworldlight_t
        {
            public Vector3 origin;
            public Vector3 intensity;
            public Vector3 normal;          // for surfaces and spotlights
            public Int32 cluster;
            public emittype_t type;
            public Int32 style;
            public Single stopdot;      // start of penumbra for emit_spotlight
            public Single stopdot2;     // end of penumbra for emit_spotlight
            public Single exponent;     // 
            public Single radius;       // cutoff distance
            // falloff for emit_spotlight + emit_point: 
            // 1 / (constant_attn + linear_attn * dist + quadratic_attn * dist^2)
            public Single constant_attn;
            public Single linear_attn;
            public Single quadratic_attn;
            public Int32 flags;          // Uses a combination of the DWL_FLAGS_ defines.
            public Int32 texinfo;        // 
            public Int32 owner;          // entity that this light it relative to
        };

        //LIGHTS

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct dleafambientlighting_t
        {
            public CompressedLightCube cube;
            public byte x;     // fixed point fraction of leaf bounds
            public byte y;     // fixed point fraction of leaf bounds
            public byte z;     // fixed point fraction of leaf bounds
            public byte pad;   // unused
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CompressedLightCube
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ColorRGBExp32[] m_Color;
        };

        /// <summary>
        /// sizeof = 4
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ColorRGBExp32
        {
            public Byte r, g, b;
            public SByte exponent;
        }

        public struct Face
        {
            public texinfo_t TexInfo;
            public dtexdata_t TexData;
            public Vector3[] Vertices;
            public Vector2[] UV, UV2;
            public Int32[] Triangles;
            public Color32[] Colors;
            public Int32 LightOfs;
            public Int32 LightMapW;
            public Int32 LightMapH;
        }
    }
}
