Shader "USource/TranslucentGeneric" 
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cull("Cull State", Int) = 2
		_Detail("Base Detail (RGB)", 2D) = "white" {}
		_DetailFactor("Factor of detail", Range(0,1)) = 0
		_DetailBlendMode("Blend mode of detail", Int) = 0
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		Cull[_Cull]

		CGPROGRAM
		#pragma surface surf Lambert alpha:fade
		#include "Lightmapped/SourceCG.cginc"

		fixed4 _Color;
		sampler2D _MainTex;
		sampler2D _Detail;
		half _DetailFactor;
		float _DetailBlendMode;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_Detail;
		};

		void surf(Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			fixed4 detailColor = tex2D(_Detail, IN.uv_Detail);
			o.Albedo = TextureCombine(baseColor, detailColor, _DetailBlendMode, _DetailFactor);
			o.Alpha = baseColor.a;
		}
		ENDCG
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}
