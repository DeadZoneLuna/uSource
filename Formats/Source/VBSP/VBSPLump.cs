using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace uSource.Formats.Source.VBSP
{
	public class VBSPLump
	{
		[Flags]
		public enum STATICPROP_FLAGS
		{
			/// <summary>automatically computed</summary>
			STATIC_PROP_FLAG_FADES = 0x1,
			/// <summary>automatically computed</summary>
			STATIC_PROP_USE_LIGHTING_ORIGIN = 0x2,
			/// <summary>automatically computed; computed at run time based on dx level</summary>
			STATIC_PROP_NO_DRAW = 0x4,

			/// <summary>set in WC</summary>
			STATIC_PROP_IGNORE_NORMALS = 0x8,
			/// <summary>set in WC</summary>
			STATIC_PROP_NO_SHADOW = 0x10,
			/// <summary>set in WC</summary>
			STATIC_PROP_UNUSED = 0x20,

			/// <summary>in vrad, compute lighting at lighting origin, not for each vertex</summary>
			STATIC_PROP_NO_PER_VERTEX_LIGHTING = 0x40,

			/// <summary>disable self shadowing in vrad</summary>
			STATIC_PROP_NO_SELF_SHADOWING = 0x80
		}

		/// <summary>Game lump: Static prop. V4. Size: 56 bytes</summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct StaticPropLumpV4_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
		}

		/// <summary>Game lump: Static prop. V5. Size: 60 bytes</summary>
		public struct StaticPropLumpV5_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
		}

		/// <summary>Game lump: Static prop. V6. Size: 64 bytes</summary>
		public struct StaticPropLumpV6_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			public ushort m_nMinDXLevel;
			public ushort m_nMaxDXLevel;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
		}

		/// <summary>Game lump: Static prop. V7. Size: 68 bytes</summary>
		public struct StaticPropLumpV7_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			public ushort m_nMinDXLevel;
			public ushort m_nMaxDXLevel;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
			public Color32 m_DiffuseModulation;    // per instance color and alpha modulation
		}

		/// <summary>Game lump: Static prop. V8. Size: 68 bytes</summary>
		public struct StaticPropLumpV8_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			public byte m_nMinCPULevel;
			public byte m_nMaxCPULevel;
			public byte m_nMinGPULevel;
			public byte m_nMaxGPULevel;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
			public Color32 m_DiffuseModulation;    // per instance color and alpha modulation
		}

		/// <summary>Game lump: Static prop. V9. Size: 72 bytes</summary>
		public struct StaticPropLumpV9_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			public byte m_nMinCPULevel;
			public byte m_nMaxCPULevel;
			public byte m_nMinGPULevel;
			public byte m_nMaxGPULevel;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
			public Color32 m_DiffuseModulation;    // per instance color and alpha modulation
			public bool m_bDisableX360; // if true, don't show on XBox 360 (4-bytes long)
		}

		/// <summary>Game lump: Static prop. V10. Size: 76 bytes</summary>
		public struct StaticPropLumpV10_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public STATICPROP_FLAGS m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_flForcedFadeScale;
			public byte m_nMinCPULevel;
			public byte m_nMaxCPULevel;
			public byte m_nMinGPULevel;
			public byte m_nMaxGPULevel;
			//	int				m_Lighting;			// index into the GAMELUMP_STATIC_PROP_LIGHTING lump
			public Color32 m_DiffuseModulation;    // per instance color and alpha modulation
			public bool m_bDisableX360; // if true, don't show on XBox 360 (4-bytes long)
			public uint m_FlagsEx; // Further bitflags.
		}

		/// <summary>Game lump: Static prop. V11. Size: 80 bytes</summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct StaticPropLumpV11_t
		{
			public Vector3 m_Origin;
			public Vector3 m_Angles;
			public ushort m_PropType;
			public ushort m_FirstLeaf;
			public ushort m_LeafCount;
			public byte m_Solid;
			public byte m_Flags;
			public int m_Skin;
			public float m_FadeMinDist;
			public float m_FadeMaxDist;
			public Vector3 m_LightingOrigin;
			public float m_ForcedFadeScale;
			public byte m_MinCPULevel;
			public byte m_MaxCPULevel;
			public byte m_MinGPULevel;
			public byte m_MaxGPULevel;
			public Color32 m_DiffuseModulation; // per instance color and alpha modulation
			public bool m_DisableX360; // if true, don't show on XBox 360 (4-bytes long)
			public uint m_FlagsEx; // Further bitflags.
			public float m_UniformScale; // Prop scale
		}
	}
}