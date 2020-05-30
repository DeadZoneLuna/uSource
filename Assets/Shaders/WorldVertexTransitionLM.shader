Shader "Custom/WorldVertexTransitionLM" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlendTex ("Blend (RGB)", 2D) = "white" {}
		_LightMap ("Lightmap (RGB)", 2D) = "black" {}
	}

	SubShader 
	{
		Tags { "RenderType" = "Opaque" }
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _BlendTex;
		sampler2D _LightMap;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv2_LightMap;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			half4 lm = tex2D(_LightMap, IN.uv2_LightMap);

			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			half4 b = tex2D(_BlendTex, IN.uv_MainTex);

			float alpha = IN.color.a;

			o.Albedo = lerp(c.rgb, b.rgb, alpha) * _Color;
			o.Alpha = lerp(c.a, b.a, alpha) * _Color;
			o.Emission = lm.rgb * o.Albedo.rgb;
		}
		ENDCG
	} 
	FallBack "Legacy Shaders/Diffuse"
}
