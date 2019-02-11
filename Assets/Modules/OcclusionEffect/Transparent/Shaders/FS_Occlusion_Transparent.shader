Shader "FS/Occlusion/Transparent"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Main Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent-1" }

		Pass
		{
			ZWrite On
			ColorMask 0
		}

		ZWrite Off

		CGPROGRAM
		#pragma surface surf Lambert alpha

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input
		{
			fixed2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}

		ENDCG
    }

	FallBack "VertexLit"
}
