Shader "Unlit/RayMarching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 camPos : TEXCOORD1;
                float3 hitPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            #define MAX_STEPS 100
            #define MIN_HIT_DIST 0.001f
            #define MAX_TRACE_DIST 100

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.camPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
                o.hitPos = v.vertex;
                return o;
            }

            float GetDistFromCube(float3 pos, float3 size)
            {
                float3 o = abs(pos) - size;
                float ud = length(max(o, 0));
                float n = max(max(min(o.x, 0), min(o.y, 0)), min(o.z, 0));
                return ud + n;
            }

            float GetDistFromSphere(float3 pos, float radius)
            {
                return length(pos) - radius;
            }

            float GetDistFromCylinderY(float3 pos, float2 h)
            {
                float2 d = abs(float2(length((pos).xz), pos.y)) - h;
                return length(max(d, 0)) + max(min(d.x, 0), min(d.y, 0));
            }

            float GetDistFromCylinderX(float3 pos, float2 h)
            {
                float2 d = abs(float2(length((pos).yz), pos.x)) - h;
                return length(max(d, 0)) + max(min(d.x, 0), min(d.y, 0));
            }

            float GetDistFromCylinderZ(float3 pos, float2 h)
            {
                float2 d = abs(float2(length((pos).xy), pos.z)) - h;
                return length(max(d, 0)) + max(min(d.x, 0), min(d.y, 0));
            }

            float intersectSDF(float distA, float distB) {
                return max(distA, distB);
            }

            float unionSDF(float distA, float distB) {
                return min(distA, distB);
            }

            float differenceSDF(float distA, float distB) {
                return max(distA, -distB);
            }

            float SceneSDF(float3 pos)
            {
                float dC = GetDistFromCube(pos, float3(0.4, 0.4, 0.4));
                float dS = GetDistFromSphere(pos, 0.5);
                float dCyX = GetDistFromCylinderX(pos, float2(0.3, 0.5));
                float dCyY = GetDistFromCylinderY(pos, float2(0.3, 0.5));
                float dCyZ = GetDistFromCylinderZ(pos, float2(0.3, 0.5));

                float sdCyX = GetDistFromCylinderX(pos, float2(0.1, 0.4));
                float sdCyY = GetDistFromCylinderY(pos, float2(0.1, 0.4));
                float sdCyZ = GetDistFromCylinderZ(pos, float2(0.1, 0.4));

                float ssdCyX = GetDistFromCylinderX(pos, float2(0.08, 0.5));
                float ssdCyY = GetDistFromCylinderY(pos, float2(0.08, 0.5));
                float ssdCyZ = GetDistFromCylinderZ(pos, float2(0.08, 0.5));

                float sdS = GetDistFromSphere(pos, 0.2);
                float ssdS = GetDistFromSphere(pos, 0.1);

                //float sDs = GetDistFromSphere(pos, 0.15);

                float s1 = intersectSDF(dS, dC);
                float s2 = unionSDF(unionSDF(dCyY, dCyX), dCyZ);
                float s4 = unionSDF(unionSDF(ssdCyY, ssdCyX), ssdCyZ);
                float s3 = unionSDF(unionSDF(sdCyY, sdCyX), sdCyZ);

                float s5 = differenceSDF(s1, s2);

                //return differenceSDF(intersectSDF(dS, dC), dCy);

                return unionSDF(unionSDF(s5, differenceSDF(differenceSDF(s3, s4), sdS)), ssdS);

                //return differenceSDF(differenceSDF(s3, s4), sdS);
                //return unionSDF(unionSDF(dCyY, dCyX), dCyZ);

                //return GetDistFromCylinderZ(pos, float2(0.1, 0.3));
            }

            float Raymarch(float3 ray_origin, float3 ray_direction)
            {
                float dist_from_origin = 0.0f;
                float dist_to_surface;

                for (int i = 0; i < MAX_STEPS; i++)
                {
                    float3 current_pos = ray_origin + dist_from_origin * ray_direction;

                    dist_to_surface = SceneSDF(current_pos);

                    dist_from_origin += dist_to_surface;

                    if (dist_to_surface < MIN_HIT_DIST || dist_from_origin > MAX_TRACE_DIST)
                        break;
                }

                return dist_from_origin;
            }

            float3 GetNormal(float3 pos)
            {
                float2 eplison = float2(0.01, 0);
                float3 normal = SceneSDF(pos) - float3(
                    SceneSDF(pos - eplison.xyy),
                    SceneSDF(pos - eplison.yxy),
                    SceneSDF(pos - eplison.yyx)
                    );

                return normalize(normal);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = 0;
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Change uv to center
                float2 uv = i.uv-.5;

                float3 ray_origin = i.camPos;
                float3 ray_direction = normalize(i.hitPos - i.camPos);

                float distance = Raymarch(ray_origin, ray_direction);

                float m = dot(uv, uv);

                if (distance < MAX_TRACE_DIST)
                    col.rgb = GetNormal(ray_origin + ray_direction * distance);
                else
                    discard;

                //col = lerp(col, tex, smoothstep(0.1f, 0.2f, m));

                return col;
            }
            ENDCG
        }
    }
}
