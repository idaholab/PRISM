// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/VolumeVisualization"
{
	Properties
	{
		_STEPS ("Steps", int) = 64
		_STEP_SIZE ("Step Size", float) = 0.01
		_Color ("Color", Color) = (1, 1, 1, 1)
		_SpecularPower ("Specular Power", int) = 32
		_Gloss ("Gloss", int) = 1
		_BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
		_VolumeDataTexture("Texture", 3D) = "" { }
	}
		
	SubShader
	{ 
		Tags
		{ 
			"LightMode" = "ForwardBase" /* Use the main directional light. */
			"Queue" = "Transparent"	   /* Allow transparent surfaces to render. */
		}

		Blend SrcAlpha OneMinusSrcAlpha	// Needed for rendering transparent surfaces.

		Pass
		{
			CGPROGRAM
			#pragma vertex vert 
			#pragma fragment frag 

			#include "UnityCG.cginc"

			#define MIN_DISTANCE 0.01
			#define EPSILON 0.01

			/********************** DATA **********************/
			int _STEPS;
			float _STEP_SIZE;
			fixed4 _LightColor0;
			fixed3 _Color;
			float _SpecularPower;
			float _Gloss;
			fixed4 _BackgroundColor;
			sampler3D _VolumeDataTexture;

			/********************** STRUCTS **********************/
			struct Sphere
			{
				float3 position;
				float radius;
			};

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;	// clip space
				float3 wPos : TEXCOORD1;	// world position
			};

			/****************** DISTANCE FUNCTIONS ******************/
			// Distance function for a sphere
			float sphereDistance(float3 p, Sphere sphere)
			{
				return distance(p, sphere.position) - sphere.radius;
			}

			// Distance function for a torus
			// p = position, t.x = radius of the torus, t.y = thickness of the torus
			float torusDistance(float3 p, float2 t)	
			{
				float2 q = float2(length(p.xz) - t.x, p.y);
				return length(q) - t.y;
			}

			// Provides displacement for the given position
			float displacement(float3 p)
			{
				return sin(20 * p.x) * sin(20 * p.y) * sin(20 * p.z);
			}

			// Distance function for a displaced torus
			float displacedTorus(float3 position, float2 torus)
			{
				float d1 = torusDistance(position, torus);
				float d2 = displacement(position);
				return d1 + d2;
			}

			// Distance function for a rectangular prism
			// p = position, b = x, y, z dimensions of the box
			float boxDistance(float3 p, float3 b)
			{
				float3 d = abs(p) - b;
				return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
			}

			// Wrapper for the other distance functions.
			// Comment out the distances you don't want to get
			float distanceFunction(float3 position)
			{
				// Sphere
				Sphere testSphere;
				testSphere.position = float3(0.0, 0.0, 0.0);
				testSphere.radius = 1.0;
				float d1 = sphereDistance(position, testSphere);
				//return sphereDistance(position, testSphere);

				// Torus
				float2 torus = float2(0.8, 0.2);
				float d2 = torusDistance(position, torus);
				//return torusDistance(position, torus);

				// Displaced Torus
				//return displacedTorus(position, torus);

				// Box
				float3 box = float3(1, 0.5, 1);
				d2 = boxDistance(position, box);

				
				return max(-d1, d2);		// Intersection
				//return min(d1, d2);		// Union
			}

			/****************** SHADING FUNCTIONS ******************/
			// Estimates the normal at a position by using gradient estimation
			float3 normal(float3 position)
			{
				float2 eps = float2(0.0, EPSILON);
				return normalize(float3(
					distanceFunction(position + eps.yxx) - distanceFunction(position - eps.yxx),
					distanceFunction(position + eps.xyx) - distanceFunction(position - eps.xyx),
					distanceFunction(position + eps.xxy) - distanceFunction(position - eps.xxy)));
			}

			// Provides a Lambertian shading
			fixed4 simpleLambert(float3 normal, float3 viewDirection)
			{
				fixed3 lightDir = _WorldSpaceLightPos0.xyz;		// provided by Unity
				fixed3 lightCol =  _LightColor0.rgb;			// provided by Unity

				// Specular Lighting
				fixed NdotL = max(dot(normal, lightDir), 0);
				fixed3 h = (lightDir - viewDirection) / 2;
				fixed s = pow(dot(normal, h), _SpecularPower) * _Gloss;

				fixed4 c;
				c.rgb = _Color * lightCol * NdotL + s;
				c.a = 1;

				return c;
			}

			// Renders the volume at a given position
			fixed4 renderSurface(float3 position, float3 viewDirection)
			{
				float3 n = normal(position);
				return simpleLambert(n, viewDirection);
			}

			// Distance-aided raymarching for rendering the volume
			fixed4 raymarch(float3 position, float3 direction)
			{
				for (int i = 0; i < _STEPS; i++)
				{
					float distance = distanceFunction(position);
					
					if (distance < MIN_DISTANCE)
					{
						return renderSurface(position, direction);
					}
					position += distance * direction;
				}
				return _BackgroundColor;
			}
			
			/****************** SHADERS ******************/
			// Vertex Shader
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			// Fragment Shader
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPosition = i.wPos; 
				float3 viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);	// _WorldSpaceCameraPos is the position of the camera, provided by Unity
				return tex3D(_VolumeDataTexture, i.wPos);
				//return raymarch(worldPosition, viewDirection);
			}
			ENDCG
		}
	}
}
