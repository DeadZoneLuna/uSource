Shader "Legacy Shaders/Lightmapped/Alpha" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _LightMap ("Lightmap (RGB)", 2D) = "black" {}
}
 
SubShader {
    LOD 200
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
CGPROGRAM
#pragma surface surf Lambert alpha
struct Input {
  float2 uv_MainTex;
  float2 uv2_LightMap;
};
sampler2D _MainTex;
sampler2D _LightMap;
fixed4 _Color;
void surf (Input IN, inout SurfaceOutput o)
{
  fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
  o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color;
  half4 lm = tex2D (_LightMap, IN.uv2_LightMap);
  o.Emission = lm.rgb*o.Albedo.rgb;
  o.Alpha = c.a;
}
ENDCG
}
FallBack "Legacy Shaders/Lightmapped/Diffuse"
}