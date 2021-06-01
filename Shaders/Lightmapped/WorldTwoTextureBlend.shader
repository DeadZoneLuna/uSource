Shader "USource/Lightmapped/WorldTwoTextureBlend" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGBA)", 2D) = "white" {}
		_Detail("Detail (RGBA)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert

		fixed4 _Color;
		sampler2D _MainTex;
		sampler2D _Detail;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_Detail;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			fixed4 detailColor = tex2D(_Detail, IN.uv_Detail);

			o.Albedo = lerp(detailColor.rgb, baseColor.rgb * detailColor.rgb, detailColor.a);
			o.Alpha = baseColor.a;
		}
		ENDCG
	}

	Fallback "Legacy Shaders/VertexLit"
}