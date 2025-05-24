 // A simple shader to map a texture onto a mesh.
 // Will render eithr a Color or a texture
 
 Shader "GridMapMaker/DefaultShader" 
  { 
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)    
    }
    
    SubShader 
    {
       Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
                // Key settings for transparency
        Blend SrcAlpha OneMinusSrcAlpha
       // ZWrite Off

        Pass
        {

            HLSLPROGRAM   
            
            #include "UnityCG.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
       
            float4 _Color;

            sampler2D _MainTex;
            
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
                o.color = v.color;
                
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {              
                fixed4 col =  tex2D(_MainTex, i.uv);
                     
                return col * _Color;
            }   
            
            ENDHLSL
        }  
    }
}