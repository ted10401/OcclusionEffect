Shader "FS/Occlusion/Dissolve"
{
	Properties
	{
		_DissolveThreshold("DissolveThreshold", Range(0,2)) = 2
		_DissolveDistance("DissolveDistance", Range(0, 20)) = 14
		_DissolveDistanceFactor("DissolveDistanceFactor", Range(0,3)) = 3
		_DissolveMap("DissolveMap", 2D) = "white"{}
		_ColorFactorA("ColorFactorA", Range(0,1)) = 0.7
		_ColorFactorB("ColorFactorB", Range(0,1)) = 0.8
		_DissolveColorA("Dissolve Color A", Color) = (0,1,1,0)
		_DissolveColorB("Dissolve Color B", Color) = (0.3,0.3,0.3,1)
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Pass
		{
			CGPROGRAM
			#include "Lighting.cginc"

			#pragma vertex vert
			#pragma fragment frag

			fixed _DissolveThreshold;
			fixed _DissolveDistance;
			fixed _DissolveDistanceFactor;
			sampler2D _DissolveMap;
			fixed _ColorFactorA;
			fixed _ColorFactorB;
			fixed4 _DissolveColorA;
			fixed4 _DissolveColorB;
			
			struct a2f
			{
				fixed4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
				fixed3 normal : NORMAL;
			};
	
			struct v2f
			{
				fixed4 vertex : SV_POSITION;
				fixed2 uv : TEXCOORD0;
				fixed3 worldNormal : TEXCOORD1;
				fixed4 screenPos : TEXCOORD2;
				fixed3 viewDir : TEXCOORD3;
			};
	
			v2f vert(a2f v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);
				o.screenPos = ComputeGrabScreenPos(o.vertex);
				o.viewDir = ObjSpaceViewDir(v.vertex);
				return o;
			}
	
			fixed4 frag(v2f i) : SV_Target
			{
				fixed aspect = _ScreenParams.y / _ScreenParams.x;

				fixed2 screenPos = i.screenPos.xy / i.screenPos.w;
				fixed2 dir = fixed2(0.5, 0.5) - screenPos;
				dir.y *= aspect;
				fixed screenSpaceDistance = 0.5 - sqrt(dir.x * dir.x + dir.y * dir.y);
				fixed viewDistance =  max(0,(_DissolveDistance - length(i.viewDir)) / _DissolveDistance) * _DissolveDistanceFactor;
				fixed disolveFactor = viewDistance * screenSpaceDistance * _DissolveThreshold;
				fixed4 dissolveValue = tex2D(_DissolveMap, i.uv);

				if (dissolveValue.r < disolveFactor)
				{
					discard;
				}

				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
				fixed3 lambert = saturate(dot(worldNormal, worldLightDir));
				fixed3 albedo = lambert * _LightColor0.xyz + UNITY_LIGHTMODEL_AMBIENT.xyz;
				fixed3 color = albedo;

				fixed lerpValue = disolveFactor / dissolveValue.r;
				if (lerpValue > _ColorFactorA)
				{
					if (lerpValue > _ColorFactorB)
					{
						return _DissolveColorB;
					}
						
					return _DissolveColorA;
				}

				return fixed4(color, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}