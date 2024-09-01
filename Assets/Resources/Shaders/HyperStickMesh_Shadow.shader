
Shader "UMol/Sticks HyperBalls Shadow Merged"
{   
    Properties 
    {
        _MainTex        ("Parameters texture", 2D) = "white"{}
        _Shrink         ("Shrink Factor", float) = 0.1
        _Scale          ("Link Scale", float) = 1.0
        _EllipseFactor  ("Ellipse Factor", float) = 1.0
        _Brightness     ("Brightness", float) = 0.75
        _NBParam        ("Texture size in x", Float) = 14.0
        _NBSticks       ("Texture size in y", Float) = 100.0
        _Shininess      ("Shininess", float) = 0.0
        _SpecularColor  ("Specular color", Color) = (1, 1, 1, 1)
        // _SelectedColor ("Color when selected", Color) = (1, 0.68, 0, 1)
        _MatCap        ("MatCap  (RGB)", 2D) = "white" {}
        [Toggle] _UseFog ("Enable fog", Float) = 0.0
        _FogStart ("Fog start", Float) = 0.0
        _FogDensity ("Fog density", Float) = 0.5

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
    uniform float _Shrink;
    uniform float _Scale;
    uniform float _EllipseFactor;
    uniform float _NBParam;
    uniform float _NBSticks;
    uniform sampler2D _MainTex;

    float _UseFog;
    uniform float _FogStart;
    uniform float _FogDensity;

    uniform float4 _PointLightPosition0;
    uniform float4 _PointLightColor0;
    uniform float _PointLightRadius0;

    uniform float4 _PointLightPosition1;
    uniform float4 _PointLightColor1;
    uniform float _PointLightRadius1;

    ENDCG

    SubShader {

        Tags { "DisableBatching" = "True" "RenderType" = "Opaque"}

        Pass {
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fog
            #pragma multi_compile_fwdbase



            uniform float _Brightness;
            uniform float _Shininess;
            uniform float4 _SpecularColor;
            // uniform float4 _SelectedColor;
            // uniform float4 _LightColor0;
            // uniform sampler2D _ShadowMapTexture;

            uniform sampler2D _MatCap;

            // vertex input: position
            struct appdata
            {
                float4 vertex      : POSITION;
                float2 uv_vetexids : TEXCOORD0;
            };

            // Variables passees du vertex au pixel shader
            struct vertexOutput
            {
                float4 pos              : SV_POSITION;
                float4 near    : TEXCOORD0;
                float4 far     : TEXCOORD1;
                float4 focus   : TEXCOORD2;
                float4 cutoff1 : TEXCOORD3;
                float4 cutoff2 : TEXCOORD4;
                float4 e1      : TEXCOORD5;
                float4 e2      : TEXCOORD6;
                float4 e3      : TEXCOORD7;
                // bool2 selected  : TEXCOORD8;
                float4 Color1  : COLOR0;

                LIGHTING_COORDS(9, 10)
                // UNITY_FOG_COORDS(11)
            };

            struct fragmentOutput
            {
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

            struct shadowInput 
            {
                SHADOW_COORDS(0)
            };


            // VERTEX SHADER IMPLEMENTATION =============================

            vertexOutput vert (appdata v) 
            {
                // OpenGL matrices
                float4x4 ModelViewProjI = mat_inverse(UNITY_MATRIX_MVP);

                vertexOutput o; // Shader output

                float4 vertexPosition;
                float NBParamm1 = _NBParam - 1;
                float vertexid = v.uv_vetexids.x;
                float x_texfetch = v.uv_vetexids.y;//vertexid / (_NBSticks - 1);

                //Calculate all the stuffs to create parallepipeds that defines the enveloppe for ray-casting
                half visibility = tex2Dlod(_MainTex, float4(x_texfetch, 10 / NBParamm1, 0, 0)).x;

                half2 scaleAtoms = tex2Dlod(_MainTex, float4(x_texfetch, 11 / NBParamm1, 0, 0)).xy;


                float rad1 = tex2Dlod(_MainTex, float4(x_texfetch, 0, 0, 0));
                float rad2 = tex2Dlod(_MainTex, float4(x_texfetch, 1 / NBParamm1, 0, 0));
                float radius1 = scaleAtoms.x * visibility * rad1 * _Scale;
                float radius2 = scaleAtoms.y * visibility * rad2 * _Scale;

                float4 Color1 = tex2Dlod(_MainTex, float4(x_texfetch, 2 / NBParamm1, 0, 0));
                float4 Color2 = tex2Dlod(_MainTex, float4(x_texfetch, 3 / NBParamm1, 0, 0));
                o.Color1 = Color1;

                float4 texpos1 = tex2Dlod(_MainTex, float4(x_texfetch, 4 / NBParamm1, 0, 0));
                float4 texpos2 = tex2Dlod(_MainTex, float4(x_texfetch, 5 / NBParamm1, 0, 0));

                float4 basepos1 = tex2Dlod(_MainTex, float4(x_texfetch, 6 / NBParamm1, 0, 0));
                float4 basepos2 = tex2Dlod(_MainTex, float4(x_texfetch, 7 / NBParamm1, 0, 0));

                float sel1 = tex2Dlod(_MainTex, float4(x_texfetch, 12 / NBParamm1, 0, 0)).x;
                float sel2 = tex2Dlod(_MainTex, float4(x_texfetch, 13 / NBParamm1, 0, 0)).x;

                // o.selected.x = (sel1 >= 0.9);
                // o.selected.y = (sel2 >= 0.9);

                // Calculate distance between particles.
                float4 posAtom1 = texpos1;
                float4 posAtom2 = texpos2;
                float atomDistance = distance(posAtom1, posAtom2);

                // Calculate space position
                float4 spacePosition;
                //Center to 0,0,0
                spacePosition.xy = (v.vertex.xy - basepos1.xy) * 2.0 * (radius1 > radius2 ? radius1 : radius2);
                spacePosition.z = (v.vertex.z - basepos1.z) * atomDistance;
                spacePosition.w = 1.0;

                float4 e3;
                e3.xyz = normalize(posAtom1.xyz - posAtom2.xyz);
                if (e3.z == 0.0) { e3.z = 1e-7;}
                if ( (posAtom1.x - posAtom2.x) == 0.0) { posAtom1.x += 0.001;}
                if ( (posAtom1.y - posAtom2.y) == 0.0) { posAtom1.y += 0.001;}
                if ( (posAtom1.z - posAtom2.z) == 0.0) { posAtom1.z += 0.001;}

                // Calculate focus.
                float4 focus = calculate_focus(posAtom1, posAtom2,
                radius1, radius2,
                e3, _Shrink);

                float3 e1;
                e1.x = 1.0;
                e1.y = 1.0;
                e1.z = ( sum(e3.xyz  *  focus.xyz) - e1.x  *  e3.x - e1.y  *  e3.y) / e3.z;
                e1 = normalize(e1 - focus.xyz);

                float3 e2 = normalize(cross(e1, e3.xyz));


                // Calculate rotation
                float3x3 R = float3x3(float3(e1.x, e2.x, e3.x),
                float3(e1.y, e2.y, e3.y),
                float3(e1.z, e2.z, e3.z));

                vertexPosition.xyz = mul(R, spacePosition.xyz);
                vertexPosition.w = 1.0;

                // Calculate translation
                vertexPosition.xyz += (posAtom1.xyz + posAtom2.xyz) / 2;

                o.pos = UnityObjectToClipPos(vertexPosition);


                // Calculate origin and direction of ray that we pass to the fragment ----
                float4 near = o.pos;
                near.z = 0.0 ;
                near = mul(ModelViewProjI, near) ;

                float4 far = o.pos ;
                far.z = far.w ;
                far = mul(ModelViewProjI, far);

                o.near = near;
                o.far = far;

                #if UNITY_VERSION >= 550 && !SHADER_API_GLCORE && !SHADER_API_GLES && !SHADER_API_GLES3//Since Unity 5.5 near and far plane are inverted 
                    o.near = far;
                    o.far = near;
                #endif

                // UNITY_TRANSFER_FOG(o, o.pos);

                float4 prime1, prime2;
                prime1.xyz = posAtom1.xyz - (posAtom1.xyz - focus.xyz)  *  _Shrink;
                prime2.xyz = posAtom2.xyz - (posAtom2.xyz - focus.xyz)  *  _Shrink;
                prime1.w = Color2.y;
                prime2.w = Color2.z;

                o.cutoff1 = prime1;
                o.cutoff2 = prime2;

                float4 a2fsq = (posAtom1 - focus)  *  (posAtom1 - focus);
                float Rcarre = (radius1 * radius1 / _Shrink) - sum(a2fsq.xyz);
                focus.w = Rcarre;

                e3.w = Color2.x;

                o.focus = focus;

                o.e3 = e3;
                o.e1.xyz = e1;
                o.e2.xyz = e2;

                TRANSFER_VERTEX_TO_FRAGMENT(o);

                return o;
            }

            // PIXEL SHADER IMPLEMENTATION ===============================

            fragmentOutput frag (vertexOutput i)
            {
                if (_Shrink < 0.000001 || _Shrink > 0.9999)
                discard;

                float4x4 ModelViewIT = UNITY_MATRIX_IT_MV;

                fragmentOutput o;

                float4 i_near = i.near;
                float4 i_far  = i.far;
                float4 focus = i.focus;

                float3 e1 = i.e1.xyz;
                float3 e2 = i.e2.xyz;
                float3 e3 = i.e3.xyz;

                float3 color_atom2 = float3(i.e3.w, i.cutoff1.w, i.cutoff2.w);

                float3 cutoff1 = i.cutoff1.xyz;
                float3 cutoff2 = i.cutoff2.xyz;

                float t1 = -1 / (1 - _Shrink);
                float t2 =  1 / _Shrink;
                float3 equation1 = float3(t2,   t2  *  _EllipseFactor,    t1);

                float A1 = sum(-e1  *  focus.xyz);
                float A2 = sum(-e2  *  focus.xyz);
                float A3 = sum(-e3  *  focus.xyz);

                float3 As = float3(A1, A2, A3);

                float3 eqex = equation1  *  float3(e1.x, e2.x, e3.x);
                float3 eqey = equation1  *  float3(e1.y, e2.y, e3.y);
                float3 eqez = equation1  *  float3(e1.z, e2.z, e3.z);
                float3 eqAs = equation1  *  As  *  As;
                float4 e1ext = float4(e1, As.x);
                float4 e2ext = float4(e2, As.y);
                float4 e3ext = float4(e3, As.z);

                float4  An1 = eqex.x  *  e1ext     + eqex.y  *  e2ext     + eqex.z  *  e3ext;     // Contains A11, A21, A31, A41
                float3  An2 = eqey.x  *  e1ext.yzw + eqey.y  *  e2ext.yzw + eqey.z  *  e3ext.yzw; // Contains A22, A32, A42
                float2  An3 = eqez.x  *  e1ext.zw  + eqez.y  *  e2ext.zw  + eqez.z  *  e3ext.zw;  // Contains A33, A43
                float   A44 = eqAs.x             + eqAs.y             + eqAs.z - focus.w;   // Just A44

                float4x4 mat = float4x4(An1,
                float4(An1.y, An2.xyz),
                float4(An1.z, An2.y, An3.xy),
                float4(An1.w, An2.z, An3.y, A44));

                Ray ray = primary_ray(i_near, i_far);
                float3 M = isect_surf(ray, mat);
                float4 M1 = float4(M, 1.0);
                float4 M2 = mul(mat,M1);

                float4 clipHit = UnityObjectToClipPos(M1);
                o.depth = update_z_buffer(clipHit);

                if (cutoff_plane(M, cutoff1, -e3) || cutoff_plane(M, cutoff2, e3))
                discard;

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

                UNITY_LIGHT_ATTENUATION(attenDir, shadowIN, worldPos);
                half3 directionalLighting = _LightColor0 * ndotl * attenDir; 
                lighting += directionalLighting; 

                // point light computations
                PointLightResult pointLightResult1 = CalculatePointLight(worldPos, worldNormal, _PointLightPosition0.xyz, _PointLightColor0, _PointLightRadius0);
                PointLightResult pointLightResult2 = CalculatePointLight(worldPos, worldNormal, _PointLightPosition1.xyz, _PointLightColor1, _PointLightRadius1);

                lighting += pointLightResult1.diffuseLighting;
                lighting += pointLightResult2.diffuseLighting; 


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


                //------------ blinn phong light try ------------------------

                float3 normal = normalize(mul(ModelViewIT, M2).xyz);
                float a = sum((M.xyz - cutoff2)  *  e3) / distance(cutoff2, cutoff1);
                float4 color_atom1 = float4(i.Color1.xyz, 1);

                // color_atom1 = lerp(color_atom1, _SelectedColor , i.selected.x*(_Time.y % 1.0));
                // color_atom2 = lerp(color_atom2, _SelectedColor , i.selected.y*(_Time.y % 1.0));

                float4 pcolor = float4(lerp(color_atom2, color_atom1, a), i.Color1.w);

                // MatCap
                half2 vn = normal.xy;

                float4 matcapLookup1 = tex2D(_MatCap, vn * 0.5 + 0.5);
                float4 matcapLookup2 = tex2D(_MatCap, vn * 0.5 + 0.5);
                float4 matcapLookup = lerp(matcapLookup2, matcapLookup1, a);

                o.color = float4(lighting, 1) * pcolor * matcapLookup; 

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

                    o.color += (specularDir + specularPoint1 + specularPoint2) * _SpecularColor;
                }

                o.color *=   1.25 * _Brightness; //1.25

                // UNITY_APPLY_FOG(i.fogCoord, o.color);
                if(_UseFog)
                {
                    // float fogFactor = smoothstep(_FogEnd, _FogStart, mul(UNITY_MATRIX_M, M1).z);        
                    float fogFactor = exp(_FogStart - mul(UNITY_MATRIX_M, M1).z  / max(0.0001, _FogDensity));
                    o.color.rgb = lerp(unity_FogColor, o.color.rgb, saturate(fogFactor));
                }

                return o;
            }

            ENDCG
        }


        Pass {

            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            // Setup

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster


            // vertex input: position
            struct appdata
            {
                float4 vertex      : POSITION;
                float2 uv_vetexids : TEXCOORD0;
            };

            // Variables passees du vertex au pixel shader
            struct vertexOutput
            {
                float4 pos     : POSITION;
                float4 near    : TEXCOORD1;
                float4 far     : TEXCOORD2;
                float4 focus   : TEXCOORD3;
                float4 cutoff1 : TEXCOORD4;
                float4 cutoff2 : TEXCOORD5;
                float4 e1      : TEXCOORD6;
                float3 e2      : TEXCOORD7;
                float4 e3      : TEXCOORD8;
            };



            // VERTEX SHADER IMPLEMENTATION =============================

            vertexOutput vert (appdata v) 
            {
                // OpenGL matrices
                float4x4 ModelViewProjI = mat_inverse(UNITY_MATRIX_MVP);

                vertexOutput o; // Shader output

                float4 vertexPosition;
                float NBParamm1 = _NBParam - 1;
                float vertexid = v.uv_vetexids.x;
                float x_texfetch = v.uv_vetexids.y;//vertexid / (_NBSticks - 1);


                //Calculate all the stuffs to create parallepipeds that defines the enveloppe for ray-casting


                half visibility = tex2Dlod(_MainTex, float4(x_texfetch, 10 / NBParamm1, 0, 0)).x;

                half2 scaleAtoms = tex2Dlod(_MainTex, float4(x_texfetch, 11 / NBParamm1, 0, 0)).xy;


                float radius1 = scaleAtoms.x * visibility * tex2Dlod(_MainTex, float4(x_texfetch, 0, 0, 0)) * _Scale;
                float radius2 = scaleAtoms.y * visibility * tex2Dlod(_MainTex, float4(x_texfetch, 1 / NBParamm1, 0, 0)) * _Scale;


                float4 texpos1 = tex2Dlod(_MainTex, float4(x_texfetch, 4 / NBParamm1, 0, 0));
                float4 texpos2 = tex2Dlod(_MainTex, float4(x_texfetch, 5 / NBParamm1, 0, 0));

                float4 basepos1 = tex2Dlod(_MainTex, float4(x_texfetch, 6 / NBParamm1, 0, 0));
                float4 basepos2 = tex2Dlod(_MainTex, float4(x_texfetch, 7 / NBParamm1, 0, 0));


                float atomType1 = tex2Dlod(_MainTex, float4(x_texfetch, 8 / NBParamm1, 0, 0)).x;
                float atomType2 = tex2Dlod(_MainTex, float4(x_texfetch, 9 / NBParamm1, 0, 0)).x;


                // Calculate distance between particles.
                float4 posAtom1 = texpos1;
                float4 posAtom2 = texpos2;
                float atomDistance = distance(posAtom1, posAtom2);

                // Calculate space position
                float4 spacePosition;
                //Center to 0,0,0
                spacePosition.xy = (v.vertex.xy - basepos1.xy) * 2.0 * (radius1 > radius2 ? radius1 : radius2);
                spacePosition.z = (v.vertex.z - basepos1.z) * atomDistance;
                spacePosition.w = 1.0;

                float4 e3;

                e3.w = 1;
                e3.xyz = normalize(posAtom1.xyz - posAtom2.xyz);
                if (e3.z == 0.0) { e3.z = 0.0000000000001;}
                if ( (posAtom1.x - posAtom2.x) == 0.0) { posAtom1.x += 0.001;}
                if ( (posAtom1.y - posAtom2.y) == 0.0) { posAtom1.y += 0.001;}
                if ( (posAtom1.z - posAtom2.z) == 0.0) { posAtom1.z += 0.001;}

                // Calculate focus.
                float4 focus = calculate_focus(posAtom1, posAtom2,
                radius1, radius2,
                e3, _Shrink);

                float3 e1;
                e1.x = 1.0;
                e1.y = 1.0;
                e1.z = ( sum(e3.xyz  *  focus.xyz) - e1.x  *  e3.x - e1.y  *  e3.y) / e3.z;
                e1 = normalize(e1 - focus.xyz);

                float3 e2 = normalize(cross(e1, e3.xyz));


                // Calculate rotation
                float3x3 R = float3x3(float3(e1.x, e2.x, e3.x),
                float3(e1.y, e2.y, e3.y),
                float3(e1.z, e2.z, e3.z));

                vertexPosition.xyz = mul(R, spacePosition.xyz);
                vertexPosition.w = 1.0;

                // Calculate translation
                vertexPosition.xyz += (posAtom2.xyz + posAtom1.xyz) / 2;

                o.pos = UnityObjectToClipPos(vertexPosition);


                // Calculate origin and direction of ray that we pass to the fragment ----
                float4 near = o.pos;
                near.z = 0.0 ;
                near = mul(ModelViewProjI, near) ;

                float4 far = o.pos ;
                far.z = far.w ;
                far = mul(ModelViewProjI, far);

                o.near = near;
                o.far = far;
                
                #if UNITY_VERSION >= 550  && !SHADER_API_GLCORE && !SHADER_API_GLES && !SHADER_API_GLES3//Since Unity 5.5 near and far plane are inverted 
                    o.near = far;
                    o.far = near;
                #endif

                float4 prime1, prime2;
                prime1.xyz = posAtom1.xyz - (posAtom1.xyz - focus.xyz)  *  _Shrink;
                prime2.xyz = posAtom2.xyz - (posAtom2.xyz - focus.xyz)  *  _Shrink;

                o.cutoff1 = prime1;
                o.cutoff2 = prime2;

                o.cutoff1.w = 1;
                o.cutoff2.w = 1;


                float4 a2fsq = (posAtom1 - focus)  *  (posAtom1 - focus);
                float Rcarre = (radius1 * radius1 / _Shrink) - sum(a2fsq.xyz);
                focus.w = Rcarre;


                o.focus = focus;

                o.e3 = e3;
                o.e1.xyz = e1;
                o.e1.w = (int)visibility;
                o.e2 = e2;

                return o;
            }
            

            float frag (vertexOutput i): SV_Depth
            {

                if (_Shrink < 0.0)
                discard;

                if (i.e1.w == 0)
                discard;
                float4x4 ModelViewIT = UNITY_MATRIX_IT_MV;


                float4 i_near = i.near;
                float4 i_far  = i.far;
                float4 focus = i.focus;

                float3 e1 = i.e1;
                float3 e2 = i.e2;
                float3 e3 = i.e3.xyz;

                float3 cutoff1 = i.cutoff1.xyz;
                float3 cutoff2 = i.cutoff2.xyz;

                float t1 = -1 / (1 - _Shrink);
                float t2 =  1 / _Shrink;
                float3 equation1 = float3(t2,   t2  *  _EllipseFactor,    t1);

                float A1 = sum(-e1  *  focus.xyz);
                float A2 = sum(-e2  *  focus.xyz);
                float A3 = sum(-e3  *  focus.xyz);

                float3 As = float3(A1, A2, A3);

                float3 eqex = equation1  *  float3(e1.x, e2.x, e3.x);
                float3 eqey = equation1  *  float3(e1.y, e2.y, e3.y);
                float3 eqez = equation1  *  float3(e1.z, e2.z, e3.z);
                float3 eqAs = equation1  *  As  *  As;
                float4 e1ext = float4(e1, As.x);
                float4 e2ext = float4(e2, As.y);
                float4 e3ext = float4(e3, As.z);

                float4  An1 = eqex.x  *  e1ext     + eqex.y  *  e2ext     + eqex.z  *  e3ext;     // Contains A11, A21, A31, A41
                float3  An2 = eqey.x  *  e1ext.yzw + eqey.y  *  e2ext.yzw + eqey.z  *  e3ext.yzw; // Contains A22, A32, A42
                float2  An3 = eqez.x  *  e1ext.zw  + eqez.y  *  e2ext.zw  + eqez.z  *  e3ext.zw;  // Contains A33, A43
                float   A44 = eqAs.x             + eqAs.y             + eqAs.z - focus.w;   // Just A44

                float4x4 mat = float4x4(An1,
                float4(An1.y, An2.xyz),
                float4(An1.z, An2.y, An3.xy),
                float4(An1.w, An2.z, An3.y, A44));

                Ray ray = primary_ray(i_near, i_far);
                float3 M = isect_surf(ray, mat);
                float4 M1 = float4(M, 1.0);

                float4 clipHit = UnityObjectToClipPos(M1);
                float depth = update_z_buffer(clipHit);

                if (cutoff_plane(M, cutoff1, -e3) || cutoff_plane(M, cutoff2, e3))
                discard;

                #if defined(UNITY_REVERSED_Z)
                    depth += max(-1, min(unity_LightShadowBias.x / i.pos.w, 0));
                    float clamped = min(depth, i.pos.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    depth += saturate(unity_LightShadowBias.x / i.pos.w);
                    float clamped = max(depth, i.pos.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                depth = lerp(depth, clamped, unity_LightShadowBias.y);

                return depth;
            }

            ENDCG
        }

    }
}