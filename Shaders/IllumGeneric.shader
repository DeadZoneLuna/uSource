Shader "USource/IllumGeneric" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AlphaMask("Use Mask", Int) = 0
		_MaskTex("Mask (A)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
	
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float _AlphaMask;
		sampler2D _MaskTex;
		fixed4 _Color;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_MaskTex;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 maskColor = tex2D(_MaskTex, IN.uv_MaskTex);
			fixed4 c = baseColor * _Color;
			o.Albedo = c.rgb;

			if(_AlphaMask == 1)
				o.Emission = c.rgb * (c.a * maskColor.rgb);
			else
				o.Emission = c.rgb * c.a;
			//o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Legacy Shaders/VertexLit"
}