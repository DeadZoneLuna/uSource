Shader "USource/Lightmapped/WorldVertexTransition" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_SecondTex("Second Base (RGB)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _SecondTex;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_SecondTex;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 secondColor = tex2D(_SecondTex, IN.uv_SecondTex);
			o.Albedo = lerp(baseColor.rgb, secondColor.rgb, IN.color.a) * _Color;
			//o.Alpha = baseColor.a;
		}
		ENDCG
	}

	Fallback "Legacy Shaders/VertexLit"
}
