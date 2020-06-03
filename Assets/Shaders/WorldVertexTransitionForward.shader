Shader "Custom/WorldVertexTransitionForward" 
{
    Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlendTex ("Detail (RGB)", 2D) = "white" {}
    }
	
	SubShader 
	{
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert
		sampler2D _MainTex;
		sampler2D _BlendTex;
		
		struct Input 
		{
			float4 color : COLOR;
			float2 uv;
			float2 uv_MainTex;
			float2 uv_BlendTex;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			float2 uv1 = IN.uv_MainTex;
			float2 uv2 = IN.uv_BlendTex;
			
			half4 color1 = tex2D( _MainTex, uv1 );
			half4 color2 = tex2D( _BlendTex, uv2 );
			o.Albedo = lerp(color1.rgb, color2.rgb, IN.color.a);
		}
		
		ENDCG
    }
	
	Fallback "Diffuse"
}