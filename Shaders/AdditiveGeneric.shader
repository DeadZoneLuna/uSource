Shader "USource/AdditiveGeneric" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Cull("Cull State", Int) = 2
	}

	SubShader 
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Blend SrcAlpha One
		Cull[_Cull]
		Lighting Off 
		ZWrite Off 
		Fog { Color(0,0,0,0) }

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = baseColor.rgb * 0.15;
			o.Alpha = baseColor.rgb;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/VertexLit"
}