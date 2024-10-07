Shader "Worldmap/AnimatedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _tileOffset("Offset", vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
       
            sampler2D _MainTex;
            vector _tileOffset;

            
            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float y : float;
            };


            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv.x += _tileOffset.x;
                o.color = v.color;
                
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                
                return o;
            }

               

            fixed4 frag(v2f i) : SV_Target 
            {                   
                 fixed4 col =  tex2D(_MainTex, i.uv);
                 return col;
            }  

            ENDHLSL
        }
    }
}
