Shader "USource/DecalModulate" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType" = "Transparent" }
		LOD 200

		//ZTest Greater
		Blend DstColor SrcColor

		CGPROGRAM
		#pragma surface surf Lambert alpha

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = baseColor.rgb;
			o.Alpha = baseColor.a;
		}
		ENDCG
	}

	Fallback "Legacy Shaders/VertexLit"
}