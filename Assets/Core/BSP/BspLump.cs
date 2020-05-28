using UnityEngine;
using System.Runtime.InteropServices;

public class BspLump
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV4_t
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
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV5_t
    {
        public float m_flForcedFadeScale;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV6_t
    {
        public float m_flForcedFadeScale;
        public ushort m_nMinDXLevel;
        public ushort m_nMaxDXLevel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV7_t
    {
        public float m_flForcedFadeScale;
        public ushort m_nMinDXLevel;
        public ushort m_nMaxDXLevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_DiffuseModulation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV8_t
    {
        public float m_flForcedFadeScale;
        public byte m_nMinCPULevel;
        public byte m_nMaxCPULevel;
        public byte m_nMinGPULevel;
        public byte m_nMaxGPULevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_DiffuseModulation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV9_t
    {
        public float m_flForcedFadeScale;
        public byte m_nMinCPULevel;
        public byte m_nMaxCPULevel;
        public byte m_nMinGPULevel;
        public byte m_nMaxGPULevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_DiffuseModulation;

        public bool DisableX360;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] unknown;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticPropLumpV10_t
    {
        public float m_flForcedFadeScale;
        public byte m_nMinCPULevel;
        public byte m_nMaxCPULevel;
        public byte m_nMinGPULevel;
        public byte m_nMaxGPULevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_DiffuseModulation;

        public float unknown;

        public bool DisableX360;
    }
}