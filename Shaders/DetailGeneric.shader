Shader "USource/DetailGeneric" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Detail("Detail (RGB)", 2D) = "white" {}
		_DetailFactor("Detail Blend Factor", Range(0,1)) = 0
		_DetailBlendMode("Detail Blend Mode", Int) = 0
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert
		#include "Lightmapped/SourceCG.cginc"

		sampler2D _MainTex;
		sampler2D _Detail;
		fixed4 _Color;
		half _DetailFactor;
		float _DetailBlendMode;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_Detail;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 secondColor = tex2D(_Detail, IN.uv_Detail);
			o.Albedo = TextureCombine(baseColor, secondColor, _DetailBlendMode, _DetailFactor) * _Color;
			//o.Alpha = baseColor.a;
		}
		ENDCG
	}

	Fallback "Legacy Shaders/VertexLit"
}