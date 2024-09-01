// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "UMol/Ball HyperBalls Shadow Merged" 
{
	// Properties exposed to the interface
	Properties 
	{
		_MainTex       ("Parameters texture",2D) = "white"{}
		_Brightness    ("Brightness",Float) = 1.0
		_NBParam       ("Texture size in x",Float) = 12.0
		_NBAtoms       ("Texture size in y",Float) = 10.0
		_Shininess     ("Shininess",float) = 0.0
		_SpecularColor ("Specular color",Color) = (1,1,1,1)
		_SelectedColor ("Color when selected",Color) = (1,0.68,0,1)
		_MatCap        ("MatCap  (RGB)", 2D) = "white" {}

		[Toggle] _UseFog ("Enable fog", Float) = 0.0
		_FogStart ("Fog start", Float) = 0.0
		_FogDensity ("Fog density", Float) = 0.5

		_AOStrength ("Ambient occlusion strength",float) = 1.0
		_AOTex("Ambient occlusion texture",2D) = "white" {}
		_AOTexwidth ("AO width", float) = 1.0
		_AOTexheight ("AO height", float) = 1.0
		_AORes ("AO resolution",float) = 1.0
		_AOcoords ("AO coordinates in the atlas",Vector) = (0,0,0,0)		

		_PointLightPosition0 ("Point Light 1 Position", Vector) = (0, 0, 0) 
		_PointLightColor0 ("Point Light 1 Color", Color) = (1, 1, 1, 1) // Default to white 
		_PointLightRadius0("Point Light 1 Radius", Range(0, 100)) = 0.1

		_PointLightPosition1 ("Point Light 2 Position", Vector) = (0, 0, 0) 
		_PointLightColor1 ("Point Light 2 Color", Color) = (1, 1, 1, 1) // Default to white 
		_PointLightRadius1("Point Light 2 Radius", Range(0, 100)) = 0.1
	}


	CGINCLUDE

	#include "UnityCG.cginc"
	#include "shared_hyperball.cginc"
	#include "AutoLight.cginc"
	#include "Lighting.cginc"

	uniform sampler2D _MainTex;
	uniform	float _Brightness,_NBParam,_NBAtoms;

	uniform float4 _PointLightPosition0;
	uniform float4 _PointLightColor0;
	uniform float _PointLightRadius0;

	uniform float4 _PointLightPosition1;
	uniform float4 _PointLightColor1;
	uniform float _PointLightRadius1;

	ENDCG

	SubShader {
		Tags { "DisableBatching" = "True" "RenderType"="Opaque"}
		LOD 100

		Pass 
		{
			// Lighting On
			Tags {"LightMode" = "ForwardBase"}	

			CGPROGRAM

			// Setup
			#pragma target 3.0
			#pragma vertex ballimproved_v
			#pragma fragment ballimproved_p
			#pragma multi_compile_fwdbase
			// #pragma multi_compile_fog

			uniform sampler2D _MatCap;

			uniform float _Shininess;
			uniform float4 _SpecularColor;
			uniform float4 _SelectedColor;
			// uniform float4 _LightColor0;
			// uniform sampler2D _ShadowMapTexture;

			float _UseFog;
			float _FogStart;
			float _FogDensity;

			float _AOStrength;
			float _AOTexwidth;
			float _AOTexheight;
			float _AORes;
			sampler2D _AOTex;

			struct shadowInput {
				SHADOW_COORDS(0)
			};

			// vertex input: position
			struct appdata {
				float4 vertex      : POSITION;
				float2 uv_vetexids : TEXCOORD0;//Id of the sphere in the texture for each vertex
			};

			// From vertex shader to fragment shader
			struct v2p {
				float4 pos         		: SV_POSITION;
				float4 i_near	   		: TEXCOORD0;
				float4 i_far	   		: TEXCOORD1;
				float4 colonne1			: TEXCOORD2;
				float4 colonne2			: TEXCOORD3;
				float4 colonne3			: TEXCOORD4;
				float4 colonne4			: TEXCOORD5;
				float4 worldpos			: TEXCOORD6;
				float4 color			: COLOR0;
				// float4 _ShadowCoord     : TEXCOORD7;
				LIGHTING_COORDS(7,8)
				// UNITY_FOG_COORDS(9)
				// bool selected           : TEXCOORD10;
				float4 atlasinfo	    : TEXCOORD10;
				float3 spherePos        : TEXCOORD11;
			};

			struct fragment_out  {
				float4 color : SV_Target;
				float depth  : SV_Depth;
			};

			struct PointLightResult 
			{
				half3 diffuseLighting;
				half attenuation;
				half ndotlPoint;
			};
			

			PointLightResult CalculatePointLight(float3 worldPos, half3 worldNormal, float3 lightPosition, half3 lightColor, float lightRadius) 
			{
				PointLightResult result;
				float3 lightDir = lightPosition - worldPos;				
				float distanceFromLight = length(lightDir); // Calculate distance from light source

				if(distanceFromLight > lightRadius) {
					result.diffuseLighting = half3(0, 0, 0);
					result.attenuation = 0.0;
					result.ndotlPoint = 0.0;
					return result;
				}

				// Calculate attenuation based on light radius
				result.attenuation = clamp(1.0 - distanceFromLight / lightRadius, 0.0, 1.0);
				result.attenuation = result.attenuation * result.attenuation; // quadratic falloff

				// Normalize light direction
				lightDir = lightDir / distanceFromLight;

				result.ndotlPoint = saturate(dot(worldNormal, lightDir));

				// Lambertian lighting
				result.diffuseLighting = lightColor.rgb * 10.5 * result.ndotlPoint * result.attenuation;

				return result;
			}


			// VERTEX SHADER IMPLEMENTATION =============================

			v2p ballimproved_v (appdata v) {
				// OpenGL matrices
				float4x4 ModelViewProj = UNITY_MATRIX_MVP;	// Matrix for screen coordinates
				float4x4 ModelViewProjI = mat_inverse(ModelViewProj);
				float NBParamm1 = _NBParam - 1;
				v2p o; // Shader output

				float vertexid = v.uv_vetexids.x;
				float x_texfetch = v.uv_vetexids.y;//vertexid/(_NBAtoms-1);

				float4 sphereposition = tex2Dlod(_MainTex,float4(x_texfetch,0,0,0));

				half visibility = tex2Dlod(_MainTex,float4(x_texfetch,7/NBParamm1,0,0)).x;

				float4 baseposition = tex2Dlod(_MainTex,float4(x_texfetch,4/NBParamm1,0,0));

				float4 equation = tex2Dlod(_MainTex,float4(x_texfetch,6/NBParamm1,0,0));

				float scale = tex2Dlod(_MainTex,float4(x_texfetch,8/NBParamm1,0,0)).x;

				// float sel = tex2Dlod(_MainTex,float4(x_texfetch,9/NBParamm1,0,0)).x;
				// o.selected = (sel >= 0.9);


				//Fetch the encoded radius of the sphere
				float rayon = scale * visibility * tex2Dlod(_MainTex,float4(x_texfetch,1/NBParamm1,0,0)).x;

				o.color = tex2Dlod(_MainTex,float4(x_texfetch,2/NBParamm1,0,0));

				float2 atlasid = tex2Dlod(_MainTex,float4(x_texfetch,10/NBParamm1,0,0)).xy;

				o.atlasinfo = float4(atlasid.x, atlasid.y, rayon, vertexid);
				o.spherePos = baseposition;


				float4 spaceposition;
				
				//Center to 0,0,0 + make the bounding box larger + re-translate to position
				spaceposition.xyz = (v.vertex.xyz - baseposition.xyz)*(2*rayon) + sphereposition.xyz;
				spaceposition.w = 1.0;

				o.pos = mul(ModelViewProj, spaceposition);
				v.vertex = o.pos;
				
				o.worldpos = o.pos;
				
				float4 near = o.pos ; 
				near.z = 0.0f ;
				near = mul(ModelViewProjI, near) ;

				float4 far = o.pos ; 
				far.z = far.w ;
				o.i_far = mul(ModelViewProjI,far) ;
				o.i_near = near;

				#if UNITY_VERSION >= 550 && !SHADER_API_GLCORE && !SHADER_API_GLES && !SHADER_API_GLES3//Since Unity 5.5 near and far plane are inverted 
					o.i_near =  o.i_far;
					o.i_far = near;
				#endif

				// UNITY_TRANSFER_FOG(o,o.pos);

				float4 eq1TexPos,eq1TexSq;
				float4 equation1 = float4(equation.xyz,rayon);


				eq1TexPos = equation1 * sphereposition;
				eq1TexSq =  eq1TexPos * sphereposition;

				o.colonne1 = float4(equation1.x,	0.0f,			0.0f,			-eq1TexPos.x);
				o.colonne2 = float4(0.0f,			equation1.y,	0.0f,			-eq1TexPos.y);
				o.colonne3 = float4(0.0f,			0.0f,			equation1.z,	-eq1TexPos.z);
				o.colonne4 = float4(-eq1TexPos.x,	-eq1TexPos.y,	-eq1TexPos.z,	-equation1.w*equation1.w + eq1TexSq.x + eq1TexSq.y + eq1TexSq.z);

				// o._ShadowCoord = ComputeScreenPos(o.p);
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;  
			}



			// PIXEL SHADER IMPLEMENTATION ===============================

			fragment_out ballimproved_p (v2p i) 
			{
				fragment_out OUT;

				float4x4 ModelViewProj = UNITY_MATRIX_MVP;	// Matrix for screen coordinates
				float4x4 ModelViewIT = UNITY_MATRIX_IT_MV; 


				//create matrix for the quadric equation of the sphere 
				float4x4 mat = float4x4(i.colonne1,i.colonne2,i.colonne3,i.colonne4); 	
				
				Ray ray = primary_ray(i.i_near,i.i_far) ;

				float3 M = isect_surf_ball(ray, mat);
				float4 clipHit = UnityObjectToClipPos(float4(M,1));
				OUT.depth = update_z_buffer(clipHit);
				
				//Transform normal to model space to view-space
				float4 M1 = float4(M,1.0);
				float4 M2 = mul(mat,M1);

				float3 worldPos = mul(unity_ObjectToWorld, M1);
				float4 clipPos = UnityWorldToClipPos(float4(worldPos, 1.0));

				// stuff for directional shadow receiving
				#if defined (SHADOWS_SCREEN)
					// setup shadow struct for screen space shadows
					shadowInput shadowIN;
					#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
						// mobile directional shadow
						shadowIN._ShadowCoord = mul(unity_WorldToShadow[0], float4(worldPos, 1.0));
					#else
						// screen space directional shadow
						shadowIN._ShadowCoord = ComputeScreenPos(clipPos);
					#endif // UNITY_NO_SCREENSPACE_SHADOWS
				#else
					// no shadow, or no directional shadow
					float shadowIN = 0;
				#endif // SHADOWS_SCREEN			
				


				half3 lighting = 0;

				// directional lighting part
				half3 worldNormal = UnityObjectToWorldNormal(M2);
				half3 worldLightDir = UnityWorldSpaceLightDir(worldPos);
				half ndotl = saturate(dot(worldNormal, worldLightDir));

				UNITY_LIGHT_ATTENUATION(attenDir, shadowIN, worldPos); // get shadow, attenuation, and cookie				
				half3 directionalLighting = _LightColor0 * ndotl * attenDir; // per pixel lighting
				lighting += directionalLighting;

				// point light computations
				PointLightResult pointLightResult1 = CalculatePointLight(worldPos, worldNormal, _PointLightPosition0.xyz, _PointLightColor0, _PointLightRadius0);
				PointLightResult pointLightResult2 = CalculatePointLight(worldPos, worldNormal, _PointLightPosition1.xyz, _PointLightColor1, _PointLightRadius1);

				lighting += pointLightResult1.diffuseLighting;
				lighting += pointLightResult2.diffuseLighting;

				/*
				UNITY_LIGHT_ATTENUATION(attenPoint, shadowIN, worldPos); // get shadow, attenuation, and cookie
				pointLighting *= attenPoint; // per pixel lighting
				lighting += pointLighting;
				*/


				#if defined(UNITY_SHOULD_SAMPLE_SH)					
					// ambient lighting
					half3 ambient = ShadeSH9(float4(worldNormal, 1));
					lighting += ambient;

					#if defined(VERTEXLIGHT_ON)
						// "per vertex" non-important lights
						half3 vertexLighting = Shade4PointLights(
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, worldPos, worldNormal);

						lighting += vertexLighting;

					#endif // VERTEXLIGHT_ON
				#endif // UNITY_SHOULD_SAMPLE_SH


				float3 normal = normalize(mul(ModelViewIT, M2).xyz);//Eye space normal

				//LitSPhere / MatCap
				half2 vn = normal.xy;
				
				float4 matcapLookup = tex2D(_MatCap, vn*0.5 + 0.5);

				float4 inColor = float4(i.color.xyz,1);

				OUT.color = float4(lighting, 1) * inColor;				

				if (_Shininess) 
				{
					float specularDir = 0.0;
					float specularPoint1 = 0.0, specularPoint2 = 0.0;

					if (attenDir > 0.5) 
					{
						specularDir = pow(max(ndotl, 0.0), _Shininess);
					}

					if (pointLightResult1.attenuation > 0.5) 
					{
						specularPoint1 = pow(max(pointLightResult1.ndotlPoint, 0.0), _Shininess);
					}

					if (pointLightResult2.attenuation > 0.5) 
					{
						specularPoint2 = pow(max(pointLightResult2.ndotlPoint, 0.0), _Shininess);
					}

					OUT.color += (specularDir + specularPoint1 + specularPoint2) * _SpecularColor;
				}



				float aoterm = 1.0;

				if(_AOStrength != 0){

					float radius = i.atlasinfo.z;
					float3 posModelunit = (M - i.spherePos)/radius;

					float a = abs(posModelunit.x) + abs(posModelunit.y) + abs(posModelunit.z);
					float u = (posModelunit.z>0)? sign(posModelunit.x)*(1-abs(posModelunit.y)/a) : posModelunit.x/a;
					float v = (posModelunit.z>0)? sign(posModelunit.y)*(1-abs(posModelunit.x)/a) : posModelunit.y/a;
					float2 myuv = float2((u+1)*0.5,(v+1)*0.5);//between 0 and 1

					float2 posAOpath = (_AORes-1)*myuv;
					float2 sizePatch = float2(_AORes,_AORes);

					float2 weight = frac(posAOpath) ;

					float2 uvtexcale = posAOpath;
					float2 texelcenter = floor( uvtexcale )  + 0.5;


					float2 posinAO = round(i.atlasinfo.xy) + texelcenter;
					// float2 posinAO = round(i.atlasinfo.xy) + posAOpath;

					// float c4 = tex2D(_AOTex,posinAO / float2(_AOTexwidth,_AOTexheight));
					float c4 = tex2D(_AOTex,(posinAO + weight) / float2(_AOTexwidth,_AOTexheight));

					// aoterm *= _AOStrength;
					aoterm = _AOStrength * c4;
					// aoterm = pow(c4, _AOStrength);

					// aoterm = log(_AOStrength*aoterm);
					// aoterm =  1/(1+exp((-15)*aoterm+(7+_AOStrength*3)));

					// aoterm = clamp(aoterm,0,1);

				}

				OUT.color *= matcapLookup * 1.25 * _Brightness * aoterm; //1.25

				if(_UseFog)
				{
					// float fogFactor = smoothstep(_FogEnd, _FogStart, mul(UNITY_MATRIX_M, M1).z);		
					float fogFactor = exp(_FogStart - worldPos.z  / max(0.0001, _FogDensity));
					OUT.color.rgb = lerp(unity_FogColor, OUT.color.rgb, saturate(fogFactor));
				}

				return OUT;
			}

			ENDCG
		}


		Pass 
		{

			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster"}


			CGPROGRAM
			#pragma target 3.0
			#pragma vertex ballmerged_v
			#pragma fragment ballmerged_f
			#pragma multi_compile_shadowcaster
			// #pragma fragmentoption ARB_precision_hint_fastest


			struct v2f { 
				float4 pos              : POSITION;
				float4 i_near	   		: TEXCOORD1;
				float4 i_far	   		: TEXCOORD2;
				float4 colonne1			: TEXCOORD6;
				float4 colonne2			: TEXCOORD7;
				float4 colonne3			: COLOR0;
				float4 colonne4			: TEXCOORD3;
				float4 worldpos			: TEXCOORD4;
				int visibility  		: TEXCOORD5;
			};


			// vertex input: position
			struct appdata {
				float4 vertex : POSITION;
				float2 uv_vetexids : TEXCOORD0;//Id of the sphere in the texture for each vertex
			};



			v2f ballmerged_v (appdata v) {
				// OpenGL matrices
				float4x4 ModelViewProj = UNITY_MATRIX_MVP;	// Matrix for screen coordinates
				float4x4 ModelViewProjI = mat_inverse(ModelViewProj);
				float NBParamm1 = _NBParam - 1;
				v2f o; // Shader output

				float vertexid = v.uv_vetexids.x;
				float x_texfetch = v.uv_vetexids.y;//vertexid/(_NBAtoms-1);


				float4 sphereposition = tex2Dlod(_MainTex,float4(x_texfetch,0,0,0));

				half visibility = tex2Dlod(_MainTex,float4(x_texfetch,7/NBParamm1,0,0)).x;



				float4 baseposition = tex2Dlod(_MainTex,float4(x_texfetch,4/NBParamm1,0,0));

				float4 equation = tex2Dlod(_MainTex,float4(x_texfetch,6/NBParamm1,0,0));

				float scale = tex2Dlod(_MainTex,float4(x_texfetch,8/NBParamm1,0,0)).x;


				float rayon =  scale * tex2Dlod(_MainTex,float4(x_texfetch,1/NBParamm1,0,0)).x;


				o.visibility = (int)visibility;
				float4 spaceposition;
				
				spaceposition.xyz = (v.vertex.xyz - baseposition.xyz)*(2*rayon) + sphereposition.xyz;

				spaceposition.w = 1.0;

				o.pos = mul(ModelViewProj, spaceposition);
				v.vertex = o.pos;
				
				o.worldpos = o.pos;
				
				float4 near = o.pos ; 
				near.z = 0.0f ;
				near = mul(ModelViewProjI, near) ;

				float4 far = o.pos; 
				far.z = far.w ;
				o.i_far = mul(ModelViewProjI,far) ;
				o.i_near = near;


				#if UNITY_VERSION >= 550 && !SHADER_API_GLCORE && !SHADER_API_GLES && !SHADER_API_GLES3//Since Unity 5.5 near and far plane are inverted 
					o.i_near =  o.i_far;
					o.i_far = near;
				#endif



				float4 eq1TexPos,eq1TexSq;
				float4 equation1 = float4(equation.xyz,rayon);


				eq1TexPos = equation1 * sphereposition;
				eq1TexSq =  eq1TexPos * sphereposition;

				o.colonne1 = float4(equation1.x,	0.0f,			0.0f,			-eq1TexPos.x);
				o.colonne2 = float4(0.0f,			equation1.y,	0.0f,			-eq1TexPos.y);
				o.colonne3 = float4(0.0f,			0.0f,			equation1.z,	-eq1TexPos.z);
				o.colonne4 = float4(-eq1TexPos.x,	-eq1TexPos.y,	-eq1TexPos.z,	-equation1.w*equation1.w + eq1TexSq.x + eq1TexSq.y + eq1TexSq.z);


				return o;  
			}


			//TODO Fix the version when we output to a SHADOWS_CUBE
			// #if !defined(SHADOWS_CUBE) && !defined(UNITY_MIGHT_NOT_HAVE_DEPTH_TEXTURE)
			// #define OUTPUT_DEPTH
			// #endif
			
			// #ifdef OUTPUT_DEPTH
			// #define SHADOW_OUT_PS SV_Depth
			// #define SHADOW_OUT_TYPE float
			// #else
			// #define SHADOW_OUT_PS SV_Target0
			// #define SHADOW_OUT_TYPE float4
			// #endif

			float ballmerged_f (v2f i) : SV_Depth{


				float4x4 ModelViewProj = UNITY_MATRIX_MVP;	// Matrix for screen coordinates
				float4x4 ModelViewIT = UNITY_MATRIX_IT_MV; 

				if(i.visibility != 1)
				clip(-1);

				//create matrix for the quadric equation of the sphere 
				float4x4 mat = float4x4(i.colonne1,i.colonne2,i.colonne3,i.colonne4); 	
				
				Ray ray = primary_ray(i.i_near,i.i_far) ;

				float3 M = isect_surf_ball(ray, mat);
				float4 clipHit = UnityObjectToClipPos(float4(M,1));
				float depth = update_z_buffer(clipHit);

				#if defined(UNITY_REVERSED_Z)
					depth += max(-1,min(unity_LightShadowBias.x/i.pos.w,0));
					float clamped = min(depth, i.pos.w*UNITY_NEAR_CLIP_VALUE);
				#else
					depth += saturate(unity_LightShadowBias.x/i.pos.w);
					float clamped = max(depth, i.pos.w*UNITY_NEAR_CLIP_VALUE);
				#endif


				depth = lerp(depth, clamped, unity_LightShadowBias.y);
				

				return depth;

			}

			ENDCG
		}
	}
}