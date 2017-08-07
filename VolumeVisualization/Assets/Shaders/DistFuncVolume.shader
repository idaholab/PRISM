/* Distance Function Volume Shader | Marko Sterbentz
 *﻿ This shader is for barebones volume visualization using distance functions.
 * NOTE: This shader is no longer recommended for use.
 */
Shader "Custom/DistFuncVolume"
{
	Properties
	{
		_STEPS ("Steps", int) = 500
		_STEP_SIZE ("Step Size", float) = 0.01
		_Color ("Color", Color) = (1, 1, 1, 1)
		_SpecularPower ("Specular Power", int) = 32
		_Gloss ("Gloss", int) = 1
		_BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
		_VolumeDataTexture("Texture", 3D) = "" { }
		_AlphaCorrection("Alpha Correction", float) = 0.5
	}

	SubShader
	{
		Tags 
		{ 
			//"LightMode" = "ForwardBase" /* Use the main directional light. */
			"Queue" = "Transparent"	   /* Allow transparent surfaces to render. */
		}

		Blend SrcAlpha OneMinusSrcAlpha	// Needed for rendering transparent surfaces.

		// Original Pass
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
			float _AlphaCorrection;

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
				float4 pos : SV_POSITION;		// clip space
				float3 wPos : TEXCOORD1;		// world position
				float4 pCoords : TEXCOORD2;		// projected coordinates
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
			
			fixed4 sampleVolume(float4 pCoords, float3 worldSpaceCoords)
			{
				// Transform the coordinates from [-1:1] to [0:1]
				//float2 texc = float2(((pCoords.x / pCoords.w) + 1.0) / 2.0, ((pCoords.y / pCoords.w) + 1.0) / 2.0);

				float3 texc = float3(((pCoords.x / pCoords.w) + 1.0) / 2.0, 
									 ((pCoords.y / pCoords.w) + 1.0) / 2.0,
									 ((pCoords.z / pCoords.w) + 1.0) / 2.0);

				// The back position is the world space position stored in the texture
				float3 backPos = tex3D(_VolumeDataTexture, texc).xyz;											// THIS IS DIFFERENT THAN THEIR CODE

				// The front position is the world space position
				float3 frontPos = worldSpaceCoords;

				// The direction from the front position to the back
				float3 viewDir = backPos - frontPos;

				float rayLength = length(viewDir);

				// Perform ray-marched sampling
				// Calculate how long to increment in each step
				float delta = 1.0 / _STEPS;

				// The increment in ecah direction for each step
				float3 deltaDirection = normalize(viewDir) * delta;
				float deltaDirectionLength = length(deltaDirection);

				// Start the ray casting from the front position
				float3 currentPosition = frontPos;

				// The color accumulator
				fixed4 accumulatedColor = fixed4(0, 0, 0, 0);

				// The alpha value accumulated so far
				float accumulatedAlpha = 0.0;

				// How long has the ray travelled so far
				float accumulatedLength = 0.0;

				float4 colorSample;
				float alphaSample;
				float intensitySample;

				// Ray-marching loop
				[loop]
				for (int i = 0; i < _STEPS; i++)
				{
					// Get the voxel intensity value from the 3D texture
					intensitySample = tex3D(_VolumeDataTexture, currentPosition).r;					// Intensity data is currently stored in the r values of the Texture3D

					// Allow the alpha correction customization

					alphaSample = intensitySample * _AlphaCorrection; 

					// Perform the composition
					accumulatedColor += float4(intensitySample, intensitySample, intensitySample, alphaSample);

					// Advance the ray
					currentPosition += deltaDirection;
					accumulatedLength += deltaDirectionLength;

					// If the length traversed is more than the ray length, or if the alpha accumulated reaches 1.0 then exit
					if (accumulatedLength >= rayLength || accumulatedAlpha >= 1.0)
						break;

				}
				return accumulatedColor;

				// Test code
				//intensitySample = tex3D(_VolumeDataTexture, currentPosition).r;
				//return fixed4(intensitySample, intensitySample, intensitySample, 1);
			}

			/****************** SHADERS ******************/
			// Vertex Shader
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.pCoords = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}
			
			// Fragment Shader
			fixed4 frag (v2f i) : SV_Target
			{
				// Distance Function, Raymarching Volume Rendering
				//float3 worldPosition = i.wPos; 
				//float3 viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);	// _WorldSpaceCameraPos is the position of the camera, provided by Unity
				//return raymarch(worldPosition, viewDirection);

				// Texture sampling raymarched volume rendering
				//return sampleVolume(i.pCoords, i.wPos);

				// Testing texture sampling
				//return tex3D(_VolumeDataTexture, i.wPos);
				
				// Rainbow cube, if cube is in the correct position (for testing)
				//return fixed4(worldPosition.x, worldPosition.y, worldPosition.z, 1); // rainbow cube, if cube is in the correct position
				return fixed4(0,0,0,0);
			}
			ENDCG
		}
	}
}
