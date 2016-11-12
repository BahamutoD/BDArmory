Shader "BDArmory/Particles/Bullet"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Lum ("Luminance", float) = 1
	}

	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		//Blend SrcAlpha OneMinusSrcAlpha
		//Blend SrcAlpha One
		Blend SrcAlpha OneMinusSrcColor
		AlphaTest Greater .01
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
		BindChannels {
			Bind "Color", color
			Bind "Vertex", vertex
			Bind "TexCoord", texcoord
	}
	
		// ---- Fragment program cards
		SubShader
		{
			Pass
			{
		
				Program "vp" {
// Vertex combos: 2
//   opengl - ALU: 6 to 14
//   d3d9 - ALU: 6 to 14
//   d3d11 - ALU: 1 to 6, TEX: 0 to 0, FLOW: 1 to 1
//   d3d11_9x - ALU: 1 to 6, TEX: 0 to 0, FLOW: 1 to 1
SubProgram "opengl " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 5 [_MainTex_ST]
"!!ARBvp1.0
# 6 ALU
PARAM c[6] = { program.local[0],
		state.matrix.mvp,
		program.local[5] };
MOV result.color, vertex.color;
MAD result.texcoord[0].xy, vertex.texcoord[0], c[5], c[5].zwzw;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
END
# 6 instructions, 0 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_mvp]
Vector 4 [_MainTex_ST]
"vs_2_0
; 6 ALU
dcl_position0 v0
dcl_color0 v1
dcl_texcoord0 v2
mov oD0, v1
mad oT0.xy, v2, c4, c4.zwzw
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
"
}

SubProgram "d3d11 " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
ConstBuffer "$Globals" 64 // 48 used size, 5 vars
Vector 32 [_MainTex_ST] 4
ConstBuffer "UnityPerDraw" 336 // 64 used size, 6 vars
Matrix 0 [glstate_matrix_mvp] 4
BindCB "$Globals" 0
BindCB "UnityPerDraw" 1
// 7 instructions, 1 temp regs, 0 temp arrays:
// ALU 1 float, 0 int, 0 uint
// TEX 0 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"vs_4_0
eefieceddoellgnlfilkjcnhpflcklipjanghigmabaaaaaahaacaaaaadaaaaaa
cmaaaaaajmaaaaaabaabaaaaejfdeheogiaaaaaaadaaaaaaaiaaaaaafaaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapapaaaafjaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapapaaaafpaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
adadaaaafaepfdejfeejepeoaaedepemepfcaafeeffiedepepfceeaaepfdeheo
gmaaaaaaadaaaaaaaiaaaaaafaaaaaaaaaaaaaaaabaaaaaaadaaaaaaaaaaaaaa
apaaaaaafmaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaaapaaaaaagcaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaacaaaaaaadamaaaafdfgfpfagphdgjhegjgpgoaa
edepemepfcaafeeffiedepepfceeaaklfdeieefcfiabaaaaeaaaabaafgaaaaaa
fjaaaaaeegiocaaaaaaaaaaaadaaaaaafjaaaaaeegiocaaaabaaaaaaaeaaaaaa
fpaaaaadpcbabaaaaaaaaaaafpaaaaadpcbabaaaabaaaaaafpaaaaaddcbabaaa
acaaaaaaghaaaaaepccabaaaaaaaaaaaabaaaaaagfaaaaadpccabaaaabaaaaaa
gfaaaaaddccabaaaacaaaaaagiaaaaacabaaaaaadiaaaaaipcaabaaaaaaaaaaa
fgbfbaaaaaaaaaaaegiocaaaabaaaaaaabaaaaaadcaaaaakpcaabaaaaaaaaaaa
egiocaaaabaaaaaaaaaaaaaaagbabaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaak
pcaabaaaaaaaaaaaegiocaaaabaaaaaaacaaaaaakgbkbaaaaaaaaaaaegaobaaa
aaaaaaaadcaaaaakpccabaaaaaaaaaaaegiocaaaabaaaaaaadaaaaaapgbpbaaa
aaaaaaaaegaobaaaaaaaaaaadgaaaaafpccabaaaabaaaaaaegbobaaaabaaaaaa
dcaaaaaldccabaaaacaaaaaaegbabaaaacaaaaaaegiacaaaaaaaaaaaacaaaaaa
ogikcaaaaaaaaaaaacaaaaaadoaaaaab"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES


#ifdef VERTEX

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp vec4 _MainTex_ST;
uniform highp mat4 glstate_matrix_mvp;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  gl_Position = (glstate_matrix_mvp * _glesVertex);
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp float _Lum;
uniform lowp vec4 _TintColor;
uniform sampler2D _MainTex;
void main ()
{
  lowp vec4 tint_1;
  lowp vec4 tmpvar_2;
  tmpvar_2 = texture2D (_MainTex, xlv_TEXCOORD0);
  lowp float tmpvar_3;
  tmpvar_3 = dot (tmpvar_2.xyz, vec3(0.22, 0.707, 0.071));
  tint_1.w = _TintColor.w;
  highp vec3 tmpvar_4;
  tmpvar_4 = (_TintColor.xyz + clamp (((tmpvar_3 * _Lum) - 1.0), 0.0, 1.0));
  tint_1.xyz = tmpvar_4;
  highp vec4 tmpvar_5;
  tmpvar_5 = (tint_1 * _Lum);
  tint_1 = tmpvar_5;
  gl_FragData[0] = (((2.0 * xlv_COLOR) * tmpvar_2) * tint_1);
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES


#ifdef VERTEX

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp vec4 _MainTex_ST;
uniform highp mat4 glstate_matrix_mvp;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  gl_Position = (glstate_matrix_mvp * _glesVertex);
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp float _Lum;
uniform lowp vec4 _TintColor;
uniform sampler2D _MainTex;
void main ()
{
  lowp vec4 tint_1;
  lowp vec4 tmpvar_2;
  tmpvar_2 = texture2D (_MainTex, xlv_TEXCOORD0);
  lowp float tmpvar_3;
  tmpvar_3 = dot (tmpvar_2.xyz, vec3(0.22, 0.707, 0.071));
  tint_1.w = _TintColor.w;
  highp vec3 tmpvar_4;
  tmpvar_4 = (_TintColor.xyz + clamp (((tmpvar_3 * _Lum) - 1.0), 0.0, 1.0));
  tint_1.xyz = tmpvar_4;
  highp vec4 tmpvar_5;
  tmpvar_5 = (tint_1 * _Lum);
  tint_1 = tmpvar_5;
  gl_FragData[0] = (((2.0 * xlv_COLOR) * tmpvar_2) * tint_1);
}



#endif"
}

SubProgram "flash " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_mvp]
Vector 4 [_MainTex_ST]
"agal_vs
[bc]
aaaaaaaaahaaapaeacaaaaoeaaaaaaaaaaaaaaaaaaaaaaaa mov v7, a2
adaaaaaaaaaaadacadaaaaoeaaaaaaaaaeaaaaoeabaaaaaa mul r0.xy, a3, c4
abaaaaaaaaaaadaeaaaaaafeacaaaaaaaeaaaaooabaaaaaa add v0.xy, r0.xyyy, c4.zwzw
bdaaaaaaaaaaaiadaaaaaaoeaaaaaaaaadaaaaoeabaaaaaa dp4 o0.w, a0, c3
bdaaaaaaaaaaaeadaaaaaaoeaaaaaaaaacaaaaoeabaaaaaa dp4 o0.z, a0, c2
bdaaaaaaaaaaacadaaaaaaoeaaaaaaaaabaaaaoeabaaaaaa dp4 o0.y, a0, c1
bdaaaaaaaaaaabadaaaaaaoeaaaaaaaaaaaaaaoeabaaaaaa dp4 o0.x, a0, c0
aaaaaaaaaaaaamaeaaaaaaoeabaaaaaaaaaaaaaaaaaaaaaa mov v0.zw, c0
"
}

SubProgram "d3d11_9x " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
ConstBuffer "$Globals" 64 // 48 used size, 5 vars
Vector 32 [_MainTex_ST] 4
ConstBuffer "UnityPerDraw" 336 // 64 used size, 6 vars
Matrix 0 [glstate_matrix_mvp] 4
BindCB "$Globals" 0
BindCB "UnityPerDraw" 1
// 7 instructions, 1 temp regs, 0 temp arrays:
// ALU 1 float, 0 int, 0 uint
// TEX 0 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"vs_4_0_level_9_1
eefiecedaohmkcmlcibmkipbenhacfnehadnaapgabaaaaaaheadaaaaaeaaaaaa
daaaaaaadaabaaaajaacaaaaaaadaaaaebgpgodjpiaaaaaapiaaaaaaaaacpopp
liaaaaaaeaaaaaaaacaaceaaaaaadmaaaaaadmaaaaaaceaaabaadmaaaaaaacaa
abaaabaaaaaaaaaaabaaaaaaaeaaacaaaaaaaaaaaaaaaaaaaaacpoppbpaaaaac
afaaaaiaaaaaapjabpaaaaacafaaabiaabaaapjabpaaaaacafaaaciaacaaapja
aeaaaaaeabaaadoaacaaoejaabaaoekaabaaookaafaaaaadaaaaapiaaaaaffja
adaaoekaaeaaaaaeaaaaapiaacaaoekaaaaaaajaaaaaoeiaaeaaaaaeaaaaapia
aeaaoekaaaaakkjaaaaaoeiaaeaaaaaeaaaaapiaafaaoekaaaaappjaaaaaoeia
aeaaaaaeaaaaadmaaaaappiaaaaaoekaaaaaoeiaabaaaaacaaaaammaaaaaoeia
abaaaaacaaaaapoaabaaoejappppaaaafdeieefcfiabaaaaeaaaabaafgaaaaaa
fjaaaaaeegiocaaaaaaaaaaaadaaaaaafjaaaaaeegiocaaaabaaaaaaaeaaaaaa
fpaaaaadpcbabaaaaaaaaaaafpaaaaadpcbabaaaabaaaaaafpaaaaaddcbabaaa
acaaaaaaghaaaaaepccabaaaaaaaaaaaabaaaaaagfaaaaadpccabaaaabaaaaaa
gfaaaaaddccabaaaacaaaaaagiaaaaacabaaaaaadiaaaaaipcaabaaaaaaaaaaa
fgbfbaaaaaaaaaaaegiocaaaabaaaaaaabaaaaaadcaaaaakpcaabaaaaaaaaaaa
egiocaaaabaaaaaaaaaaaaaaagbabaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaak
pcaabaaaaaaaaaaaegiocaaaabaaaaaaacaaaaaakgbkbaaaaaaaaaaaegaobaaa
aaaaaaaadcaaaaakpccabaaaaaaaaaaaegiocaaaabaaaaaaadaaaaaapgbpbaaa
aaaaaaaaegaobaaaaaaaaaaadgaaaaafpccabaaaabaaaaaaegbobaaaabaaaaaa
dcaaaaaldccabaaaacaaaaaaegbabaaaacaaaaaaegiacaaaaaaaaaaaacaaaaaa
ogikcaaaaaaaaaaaacaaaaaadoaaaaabejfdeheogiaaaaaaadaaaaaaaiaaaaaa
faaaaaaaaaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapapaaaafjaaaaaaaaaaaaaa
aaaaaaaaadaaaaaaabaaaaaaapapaaaafpaaaaaaaaaaaaaaaaaaaaaaadaaaaaa
acaaaaaaadadaaaafaepfdejfeejepeoaaedepemepfcaafeeffiedepepfceeaa
epfdeheogmaaaaaaadaaaaaaaiaaaaaafaaaaaaaaaaaaaaaabaaaaaaadaaaaaa
aaaaaaaaapaaaaaafmaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaaapaaaaaa
gcaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaaadamaaaafdfgfpfagphdgjhe
gjgpgoaaedepemepfcaafeeffiedepepfceeaakl"
}

SubProgram "gles3 " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES3#version 300 es


#ifdef VERTEX

#define gl_Vertex _glesVertex
in vec4 _glesVertex;
#define gl_Color _glesColor
in vec4 _glesColor;
#define gl_MultiTexCoord0 _glesMultiTexCoord0
in vec4 _glesMultiTexCoord0;

#line 150
struct v2f_vertex_lit {
    highp vec2 uv;
    lowp vec4 diff;
    lowp vec4 spec;
};
#line 186
struct v2f_img {
    highp vec4 pos;
    mediump vec2 uv;
};
#line 180
struct appdata_img {
    highp vec4 vertex;
    mediump vec2 texcoord;
};
#line 315
struct v2f {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
#line 308
struct appdata_t {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
uniform highp vec4 _Time;
uniform highp vec4 _SinTime;
#line 3
uniform highp vec4 _CosTime;
uniform highp vec4 unity_DeltaTime;
uniform highp vec3 _WorldSpaceCameraPos;
uniform highp vec4 _ProjectionParams;
#line 7
uniform highp vec4 _ScreenParams;
uniform highp vec4 _ZBufferParams;
uniform highp vec4 unity_CameraWorldClipPlanes[6];
uniform highp vec4 _WorldSpaceLightPos0;
#line 11
uniform highp vec4 _LightPositionRange;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosZ0;
#line 15
uniform highp vec4 unity_4LightAtten0;
uniform highp vec4 unity_LightColor[4];
uniform highp vec4 unity_LightPosition[4];
uniform highp vec4 unity_LightAtten[4];
#line 19
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform highp vec4 unity_SHBr;
#line 23
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHC;
uniform highp vec3 unity_LightColor0;
uniform highp vec3 unity_LightColor1;
uniform highp vec3 unity_LightColor2;
uniform highp vec3 unity_LightColor3;
#line 27
uniform highp vec4 unity_ShadowSplitSpheres[4];
uniform highp vec4 unity_ShadowSplitSqRadii;
uniform highp vec4 unity_LightShadowBias;
uniform highp vec4 _LightSplitsNear;
#line 31
uniform highp vec4 _LightSplitsFar;
uniform highp mat4 unity_World2Shadow[4];
uniform highp vec4 _LightShadowData;
uniform highp vec4 unity_ShadowFadeCenterAndType;
#line 35
uniform highp mat4 glstate_matrix_mvp;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_invtrans_modelview0;
uniform highp mat4 _Object2World;
#line 39
uniform highp mat4 _World2Object;
uniform highp vec4 unity_Scale;
uniform highp mat4 glstate_matrix_transpose_modelview0;
uniform highp mat4 glstate_matrix_texture0;
#line 43
uniform highp mat4 glstate_matrix_texture1;
uniform highp mat4 glstate_matrix_texture2;
uniform highp mat4 glstate_matrix_texture3;
uniform highp mat4 glstate_matrix_projection;
#line 47
uniform highp vec4 glstate_lightmodel_ambient;
uniform highp mat4 unity_MatrixV;
uniform highp mat4 unity_MatrixVP;
uniform lowp vec4 unity_ColorSpaceGrey;
#line 76
#line 81
#line 86
#line 90
#line 95
#line 119
#line 136
#line 157
#line 165
#line 192
#line 205
#line 214
#line 219
#line 228
#line 233
#line 242
#line 259
#line 264
#line 290
#line 298
#line 302
#line 306
uniform sampler2D _MainTex;
uniform lowp vec4 _TintColor;
#line 322
uniform highp vec4 _MainTex_ST;
#line 331
uniform sampler2D _CameraDepthTexture;
uniform highp float _InvFade;
uniform highp float _Lum;
#line 323
v2f vert( in appdata_t v ) {
    v2f o;
    #line 326
    o.vertex = (glstate_matrix_mvp * v.vertex);
    o.color = v.color;
    o.texcoord = ((v.texcoord.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
    return o;
}
out lowp vec4 xlv_COLOR;
out highp vec2 xlv_TEXCOORD0;
void main() {
    v2f xl_retval;
    appdata_t xlt_v;
    xlt_v.vertex = vec4(gl_Vertex);
    xlt_v.color = vec4(gl_Color);
    xlt_v.texcoord = vec2(gl_MultiTexCoord0);
    xl_retval = vert( xlt_v);
    gl_Position = vec4(xl_retval.vertex);
    xlv_COLOR = vec4(xl_retval.color);
    xlv_TEXCOORD0 = vec2(xl_retval.texcoord);
}


#endif
#ifdef FRAGMENT

#define gl_FragData _glesFragData
layout(location = 0) out mediump vec4 _glesFragData[4];

#line 150
struct v2f_vertex_lit {
    highp vec2 uv;
    lowp vec4 diff;
    lowp vec4 spec;
};
#line 186
struct v2f_img {
    highp vec4 pos;
    mediump vec2 uv;
};
#line 180
struct appdata_img {
    highp vec4 vertex;
    mediump vec2 texcoord;
};
#line 315
struct v2f {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
#line 308
struct appdata_t {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
uniform highp vec4 _Time;
uniform highp vec4 _SinTime;
#line 3
uniform highp vec4 _CosTime;
uniform highp vec4 unity_DeltaTime;
uniform highp vec3 _WorldSpaceCameraPos;
uniform highp vec4 _ProjectionParams;
#line 7
uniform highp vec4 _ScreenParams;
uniform highp vec4 _ZBufferParams;
uniform highp vec4 unity_CameraWorldClipPlanes[6];
uniform highp vec4 _WorldSpaceLightPos0;
#line 11
uniform highp vec4 _LightPositionRange;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosZ0;
#line 15
uniform highp vec4 unity_4LightAtten0;
uniform highp vec4 unity_LightColor[4];
uniform highp vec4 unity_LightPosition[4];
uniform highp vec4 unity_LightAtten[4];
#line 19
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform highp vec4 unity_SHBr;
#line 23
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHC;
uniform highp vec3 unity_LightColor0;
uniform highp vec3 unity_LightColor1;
uniform highp vec3 unity_LightColor2;
uniform highp vec3 unity_LightColor3;
#line 27
uniform highp vec4 unity_ShadowSplitSpheres[4];
uniform highp vec4 unity_ShadowSplitSqRadii;
uniform highp vec4 unity_LightShadowBias;
uniform highp vec4 _LightSplitsNear;
#line 31
uniform highp vec4 _LightSplitsFar;
uniform highp mat4 unity_World2Shadow[4];
uniform highp vec4 _LightShadowData;
uniform highp vec4 unity_ShadowFadeCenterAndType;
#line 35
uniform highp mat4 glstate_matrix_mvp;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_invtrans_modelview0;
uniform highp mat4 _Object2World;
#line 39
uniform highp mat4 _World2Object;
uniform highp vec4 unity_Scale;
uniform highp mat4 glstate_matrix_transpose_modelview0;
uniform highp mat4 glstate_matrix_texture0;
#line 43
uniform highp mat4 glstate_matrix_texture1;
uniform highp mat4 glstate_matrix_texture2;
uniform highp mat4 glstate_matrix_texture3;
uniform highp mat4 glstate_matrix_projection;
#line 47
uniform highp vec4 glstate_lightmodel_ambient;
uniform highp mat4 unity_MatrixV;
uniform highp mat4 unity_MatrixVP;
uniform lowp vec4 unity_ColorSpaceGrey;
#line 76
#line 81
#line 86
#line 90
#line 95
#line 119
#line 136
#line 157
#line 165
#line 192
#line 205
#line 214
#line 219
#line 228
#line 233
#line 242
#line 259
#line 264
#line 290
#line 298
#line 302
#line 306
uniform sampler2D _MainTex;
uniform lowp vec4 _TintColor;
#line 322
uniform highp vec4 _MainTex_ST;
#line 331
uniform sampler2D _CameraDepthTexture;
uniform highp float _InvFade;
uniform highp float _Lum;
#line 172
lowp float Luminance( in lowp vec3 c ) {
    #line 174
    return dot( c, vec3( 0.22, 0.707, 0.071));
}
#line 334
lowp vec4 frag( in v2f i ) {
    #line 336
    lowp vec4 texCol = texture( _MainTex, i.texcoord);
    highp float over = clamp( ((Luminance( vec3( texCol)) * _Lum) - 1.0), 0.0, 1.0);
    lowp vec4 tint = _TintColor;
    tint.xyz += (1.0 * over);
    #line 340
    tint *= _Lum;
    return (((2.0 * i.color) * texCol) * tint);
}
in lowp vec4 xlv_COLOR;
in highp vec2 xlv_TEXCOORD0;
void main() {
    lowp vec4 xl_retval;
    v2f xlt_i;
    xlt_i.vertex = vec4(0.0);
    xlt_i.color = vec4(xlv_COLOR);
    xlt_i.texcoord = vec2(xlv_TEXCOORD0);
    xl_retval = frag( xlt_i);
    gl_FragData[0] = vec4(xl_retval);
}


#endif"
}

SubProgram "opengl " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 9 [_ProjectionParams]
Vector 10 [_MainTex_ST]
"!!ARBvp1.0
# 14 ALU
PARAM c[11] = { { 0.5 },
		state.matrix.modelview[0],
		state.matrix.mvp,
		program.local[9..10] };
TEMP R0;
TEMP R1;
DP4 R1.w, vertex.position, c[8];
DP4 R0.x, vertex.position, c[5];
MOV R0.w, R1;
DP4 R0.y, vertex.position, c[6];
MUL R1.xyz, R0.xyww, c[0].x;
MUL R1.y, R1, c[9].x;
DP4 R0.z, vertex.position, c[7];
MOV result.position, R0;
DP4 R0.x, vertex.position, c[3];
ADD result.texcoord[1].xy, R1, R1.z;
MOV result.color, vertex.color;
MAD result.texcoord[0].xy, vertex.texcoord[0], c[10], c[10].zwzw;
MOV result.texcoord[1].z, -R0.x;
MOV result.texcoord[1].w, R1;
END
# 14 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_modelview0]
Matrix 4 [glstate_matrix_mvp]
Vector 8 [_ProjectionParams]
Vector 9 [_ScreenParams]
Vector 10 [_MainTex_ST]
"vs_2_0
; 14 ALU
def c11, 0.50000000, 0, 0, 0
dcl_position0 v0
dcl_color0 v1
dcl_texcoord0 v2
dp4 r1.w, v0, c7
dp4 r0.x, v0, c4
mov r0.w, r1
dp4 r0.y, v0, c5
mul r1.xyz, r0.xyww, c11.x
mul r1.y, r1, c8.x
dp4 r0.z, v0, c6
mov oPos, r0
dp4 r0.x, v0, c2
mad oT1.xy, r1.z, c9.zwzw, r1
mov oD0, v1
mad oT0.xy, v2, c10, c10.zwzw
mov oT1.z, -r0.x
mov oT1.w, r1
"
}

SubProgram "d3d11 " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
ConstBuffer "$Globals" 64 // 48 used size, 5 vars
Vector 32 [_MainTex_ST] 4
ConstBuffer "UnityPerCamera" 128 // 96 used size, 8 vars
Vector 80 [_ProjectionParams] 4
ConstBuffer "UnityPerDraw" 336 // 128 used size, 6 vars
Matrix 0 [glstate_matrix_mvp] 4
Matrix 64 [glstate_matrix_modelview0] 4
BindCB "$Globals" 0
BindCB "UnityPerCamera" 1
BindCB "UnityPerDraw" 2
// 17 instructions, 2 temp regs, 0 temp arrays:
// ALU 6 float, 0 int, 0 uint
// TEX 0 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"vs_4_0
eefiecednmpbjacibpimicngiphdfdihadeeendaabaaaaaaoaadaaaaadaaaaaa
cmaaaaaajmaaaaaaciabaaaaejfdeheogiaaaaaaadaaaaaaaiaaaaaafaaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapapaaaafjaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapapaaaafpaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
adadaaaafaepfdejfeejepeoaaedepemepfcaafeeffiedepepfceeaaepfdeheo
ieaaaaaaaeaaaaaaaiaaaaaagiaaaaaaaaaaaaaaabaaaaaaadaaaaaaaaaaaaaa
apaaaaaaheaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaaapaaaaaahkaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaacaaaaaaadamaaaahkaaaaaaabaaaaaaaaaaaaaa
adaaaaaaadaaaaaaapaaaaaafdfgfpfagphdgjhegjgpgoaaedepemepfcaafeef
fiedepepfceeaaklfdeieefclaacaaaaeaaaabaakmaaaaaafjaaaaaeegiocaaa
aaaaaaaaadaaaaaafjaaaaaeegiocaaaabaaaaaaagaaaaaafjaaaaaeegiocaaa
acaaaaaaaiaaaaaafpaaaaadpcbabaaaaaaaaaaafpaaaaadpcbabaaaabaaaaaa
fpaaaaaddcbabaaaacaaaaaaghaaaaaepccabaaaaaaaaaaaabaaaaaagfaaaaad
pccabaaaabaaaaaagfaaaaaddccabaaaacaaaaaagfaaaaadpccabaaaadaaaaaa
giaaaaacacaaaaaadiaaaaaipcaabaaaaaaaaaaafgbfbaaaaaaaaaaaegiocaaa
acaaaaaaabaaaaaadcaaaaakpcaabaaaaaaaaaaaegiocaaaacaaaaaaaaaaaaaa
agbabaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaakpcaabaaaaaaaaaaaegiocaaa
acaaaaaaacaaaaaakgbkbaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaakpcaabaaa
aaaaaaaaegiocaaaacaaaaaaadaaaaaapgbpbaaaaaaaaaaaegaobaaaaaaaaaaa
dgaaaaafpccabaaaaaaaaaaaegaobaaaaaaaaaaadgaaaaafpccabaaaabaaaaaa
egbobaaaabaaaaaadcaaaaaldccabaaaacaaaaaaegbabaaaacaaaaaaegiacaaa
aaaaaaaaacaaaaaaogikcaaaaaaaaaaaacaaaaaadiaaaaaiccaabaaaaaaaaaaa
bkaabaaaaaaaaaaaakiacaaaabaaaaaaafaaaaaadiaaaaakncaabaaaabaaaaaa
agahbaaaaaaaaaaaaceaaaaaaaaaaadpaaaaaaaaaaaaaadpaaaaaadpdgaaaaaf
iccabaaaadaaaaaadkaabaaaaaaaaaaaaaaaaaahdccabaaaadaaaaaakgakbaaa
abaaaaaamgaabaaaabaaaaaadiaaaaaibcaabaaaaaaaaaaabkbabaaaaaaaaaaa
ckiacaaaacaaaaaaafaaaaaadcaaaaakbcaabaaaaaaaaaaackiacaaaacaaaaaa
aeaaaaaaakbabaaaaaaaaaaaakaabaaaaaaaaaaadcaaaaakbcaabaaaaaaaaaaa
ckiacaaaacaaaaaaagaaaaaackbabaaaaaaaaaaaakaabaaaaaaaaaaadcaaaaak
bcaabaaaaaaaaaaackiacaaaacaaaaaaahaaaaaadkbabaaaaaaaaaaaakaabaaa
aaaaaaaadgaaaaageccabaaaadaaaaaaakaabaiaebaaaaaaaaaaaaaadoaaaaab
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES


#ifdef VERTEX

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp vec4 _MainTex_ST;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_mvp;
uniform highp vec4 _ProjectionParams;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  highp vec4 tmpvar_1;
  highp vec4 tmpvar_2;
  tmpvar_2 = (glstate_matrix_mvp * _glesVertex);
  highp vec4 o_3;
  highp vec4 tmpvar_4;
  tmpvar_4 = (tmpvar_2 * 0.5);
  highp vec2 tmpvar_5;
  tmpvar_5.x = tmpvar_4.x;
  tmpvar_5.y = (tmpvar_4.y * _ProjectionParams.x);
  o_3.xy = (tmpvar_5 + tmpvar_4.w);
  o_3.zw = tmpvar_2.zw;
  tmpvar_1.xyw = o_3.xyw;
  tmpvar_1.z = -((glstate_matrix_modelview0 * _glesVertex).z);
  gl_Position = tmpvar_2;
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_TEXCOORD1 = tmpvar_1;
}



#endif
#ifdef FRAGMENT

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp float _Lum;
uniform highp float _InvFade;
uniform sampler2D _CameraDepthTexture;
uniform lowp vec4 _TintColor;
uniform sampler2D _MainTex;
uniform highp vec4 _ZBufferParams;
void main ()
{
  lowp vec4 tmpvar_1;
  tmpvar_1.xyz = xlv_COLOR.xyz;
  lowp vec4 tint_2;
  lowp vec4 tmpvar_3;
  tmpvar_3 = texture2DProj (_CameraDepthTexture, xlv_TEXCOORD1);
  highp float z_4;
  z_4 = tmpvar_3.x;
  highp float tmpvar_5;
  tmpvar_5 = (xlv_COLOR.w * clamp ((_InvFade * ((1.0/(((_ZBufferParams.z * z_4) + _ZBufferParams.w))) - xlv_TEXCOORD1.z)), 0.0, 1.0));
  tmpvar_1.w = tmpvar_5;
  lowp vec4 tmpvar_6;
  tmpvar_6 = texture2D (_MainTex, xlv_TEXCOORD0);
  lowp float tmpvar_7;
  tmpvar_7 = dot (tmpvar_6.xyz, vec3(0.22, 0.707, 0.071));
  tint_2.w = _TintColor.w;
  highp vec3 tmpvar_8;
  tmpvar_8 = (_TintColor.xyz + clamp (((tmpvar_7 * _Lum) - 1.0), 0.0, 1.0));
  tint_2.xyz = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9 = (tint_2 * _Lum);
  tint_2 = tmpvar_9;
  gl_FragData[0] = (((2.0 * tmpvar_1) * tmpvar_6) * tint_2);
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES


#ifdef VERTEX

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp vec4 _MainTex_ST;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_mvp;
uniform highp vec4 _ProjectionParams;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  highp vec4 tmpvar_1;
  highp vec4 tmpvar_2;
  tmpvar_2 = (glstate_matrix_mvp * _glesVertex);
  highp vec4 o_3;
  highp vec4 tmpvar_4;
  tmpvar_4 = (tmpvar_2 * 0.5);
  highp vec2 tmpvar_5;
  tmpvar_5.x = tmpvar_4.x;
  tmpvar_5.y = (tmpvar_4.y * _ProjectionParams.x);
  o_3.xy = (tmpvar_5 + tmpvar_4.w);
  o_3.zw = tmpvar_2.zw;
  tmpvar_1.xyw = o_3.xyw;
  tmpvar_1.z = -((glstate_matrix_modelview0 * _glesVertex).z);
  gl_Position = tmpvar_2;
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_TEXCOORD1 = tmpvar_1;
}



#endif
#ifdef FRAGMENT

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp float _Lum;
uniform highp float _InvFade;
uniform sampler2D _CameraDepthTexture;
uniform lowp vec4 _TintColor;
uniform sampler2D _MainTex;
uniform highp vec4 _ZBufferParams;
void main ()
{
  lowp vec4 tmpvar_1;
  tmpvar_1.xyz = xlv_COLOR.xyz;
  lowp vec4 tint_2;
  lowp vec4 tmpvar_3;
  tmpvar_3 = texture2DProj (_CameraDepthTexture, xlv_TEXCOORD1);
  highp float z_4;
  z_4 = tmpvar_3.x;
  highp float tmpvar_5;
  tmpvar_5 = (xlv_COLOR.w * clamp ((_InvFade * ((1.0/(((_ZBufferParams.z * z_4) + _ZBufferParams.w))) - xlv_TEXCOORD1.z)), 0.0, 1.0));
  tmpvar_1.w = tmpvar_5;
  lowp vec4 tmpvar_6;
  tmpvar_6 = texture2D (_MainTex, xlv_TEXCOORD0);
  lowp float tmpvar_7;
  tmpvar_7 = dot (tmpvar_6.xyz, vec3(0.22, 0.707, 0.071));
  tint_2.w = _TintColor.w;
  highp vec3 tmpvar_8;
  tmpvar_8 = (_TintColor.xyz + clamp (((tmpvar_7 * _Lum) - 1.0), 0.0, 1.0));
  tint_2.xyz = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9 = (tint_2 * _Lum);
  tint_2 = tmpvar_9;
  gl_FragData[0] = (((2.0 * tmpvar_1) * tmpvar_6) * tint_2);
}



#endif"
}

SubProgram "flash " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_modelview0]
Matrix 4 [glstate_matrix_mvp]
Vector 8 [_ProjectionParams]
Vector 9 [unity_NPOTScale]
Vector 10 [_MainTex_ST]
"agal_vs
c11 0.5 0.0 0.0 0.0
[bc]
bdaaaaaaabaaaiacaaaaaaoeaaaaaaaaahaaaaoeabaaaaaa dp4 r1.w, a0, c7
bdaaaaaaaaaaabacaaaaaaoeaaaaaaaaaeaaaaoeabaaaaaa dp4 r0.x, a0, c4
aaaaaaaaaaaaaiacabaaaappacaaaaaaaaaaaaaaaaaaaaaa mov r0.w, r1.w
bdaaaaaaaaaaacacaaaaaaoeaaaaaaaaafaaaaoeabaaaaaa dp4 r0.y, a0, c5
adaaaaaaabaaahacaaaaaapeacaaaaaaalaaaaaaabaaaaaa mul r1.xyz, r0.xyww, c11.x
adaaaaaaabaaacacabaaaaffacaaaaaaaiaaaaaaabaaaaaa mul r1.y, r1.y, c8.x
abaaaaaaabaaadacabaaaafeacaaaaaaabaaaakkacaaaaaa add r1.xy, r1.xyyy, r1.z
bdaaaaaaaaaaaeacaaaaaaoeaaaaaaaaagaaaaoeabaaaaaa dp4 r0.z, a0, c6
aaaaaaaaaaaaapadaaaaaaoeacaaaaaaaaaaaaaaaaaaaaaa mov o0, r0
bdaaaaaaaaaaabacaaaaaaoeaaaaaaaaacaaaaoeabaaaaaa dp4 r0.x, a0, c2
adaaaaaaabaaadaeabaaaafeacaaaaaaajaaaaoeabaaaaaa mul v1.xy, r1.xyyy, c9
aaaaaaaaahaaapaeacaaaaoeaaaaaaaaaaaaaaaaaaaaaaaa mov v7, a2
adaaaaaaacaaadacadaaaaoeaaaaaaaaakaaaaoeabaaaaaa mul r2.xy, a3, c10
abaaaaaaaaaaadaeacaaaafeacaaaaaaakaaaaooabaaaaaa add v0.xy, r2.xyyy, c10.zwzw
bfaaaaaaabaaaeaeaaaaaaaaacaaaaaaaaaaaaaaaaaaaaaa neg v1.z, r0.x
aaaaaaaaabaaaiaeabaaaappacaaaaaaaaaaaaaaaaaaaaaa mov v1.w, r1.w
aaaaaaaaaaaaamaeaaaaaaoeabaaaaaaaaaaaaaaaaaaaaaa mov v0.zw, c0
"
}

SubProgram "d3d11_9x " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
ConstBuffer "$Globals" 64 // 48 used size, 5 vars
Vector 32 [_MainTex_ST] 4
ConstBuffer "UnityPerCamera" 128 // 96 used size, 8 vars
Vector 80 [_ProjectionParams] 4
ConstBuffer "UnityPerDraw" 336 // 128 used size, 6 vars
Matrix 0 [glstate_matrix_mvp] 4
Matrix 64 [glstate_matrix_modelview0] 4
BindCB "$Globals" 0
BindCB "UnityPerCamera" 1
BindCB "UnityPerDraw" 2
// 17 instructions, 2 temp regs, 0 temp arrays:
// ALU 6 float, 0 int, 0 uint
// TEX 0 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"vs_4_0_level_9_1
eefiecedebcippahhcnnkilpdplfokklbbpcocajabaaaaaakmafaaaaaeaaaaaa
daaaaaaapiabaaaalaaeaaaacaafaaaaebgpgodjmaabaaaamaabaaaaaaacpopp
heabaaaaemaaaaaaadaaceaaaaaaeiaaaaaaeiaaaaaaceaaabaaeiaaaaaaacaa
abaaabaaaaaaaaaaabaaafaaabaaacaaaaaaaaaaacaaaaaaaiaaadaaaaaaaaaa
aaaaaaaaaaacpoppfbaaaaafalaaapkaaaaaaadpaaaaaaaaaaaaaaaaaaaaaaaa
bpaaaaacafaaaaiaaaaaapjabpaaaaacafaaabiaabaaapjabpaaaaacafaaacia
acaaapjaafaaaaadaaaaapiaaaaaffjaaeaaoekaaeaaaaaeaaaaapiaadaaoeka
aaaaaajaaaaaoeiaaeaaaaaeaaaaapiaafaaoekaaaaakkjaaaaaoeiaaeaaaaae
aaaaapiaagaaoekaaaaappjaaaaaoeiaafaaaaadabaaabiaaaaaffiaacaaaaka
afaaaaadabaaaiiaabaaaaiaalaaaakaafaaaaadabaaafiaaaaapeiaalaaaaka
acaaaaadacaaadoaabaakkiaabaaomiaafaaaaadabaaabiaaaaaffjaaiaakkka
aeaaaaaeabaaabiaahaakkkaaaaaaajaabaaaaiaaeaaaaaeabaaabiaajaakkka
aaaakkjaabaaaaiaaeaaaaaeabaaabiaakaakkkaaaaappjaabaaaaiaabaaaaac
acaaaeoaabaaaaibaeaaaaaeabaaadoaacaaoejaabaaoekaabaaookaaeaaaaae
aaaaadmaaaaappiaaaaaoekaaaaaoeiaabaaaaacaaaaammaaaaaoeiaabaaaaac
acaaaioaaaaappiaabaaaaacaaaaapoaabaaoejappppaaaafdeieefclaacaaaa
eaaaabaakmaaaaaafjaaaaaeegiocaaaaaaaaaaaadaaaaaafjaaaaaeegiocaaa
abaaaaaaagaaaaaafjaaaaaeegiocaaaacaaaaaaaiaaaaaafpaaaaadpcbabaaa
aaaaaaaafpaaaaadpcbabaaaabaaaaaafpaaaaaddcbabaaaacaaaaaaghaaaaae
pccabaaaaaaaaaaaabaaaaaagfaaaaadpccabaaaabaaaaaagfaaaaaddccabaaa
acaaaaaagfaaaaadpccabaaaadaaaaaagiaaaaacacaaaaaadiaaaaaipcaabaaa
aaaaaaaafgbfbaaaaaaaaaaaegiocaaaacaaaaaaabaaaaaadcaaaaakpcaabaaa
aaaaaaaaegiocaaaacaaaaaaaaaaaaaaagbabaaaaaaaaaaaegaobaaaaaaaaaaa
dcaaaaakpcaabaaaaaaaaaaaegiocaaaacaaaaaaacaaaaaakgbkbaaaaaaaaaaa
egaobaaaaaaaaaaadcaaaaakpcaabaaaaaaaaaaaegiocaaaacaaaaaaadaaaaaa
pgbpbaaaaaaaaaaaegaobaaaaaaaaaaadgaaaaafpccabaaaaaaaaaaaegaobaaa
aaaaaaaadgaaaaafpccabaaaabaaaaaaegbobaaaabaaaaaadcaaaaaldccabaaa
acaaaaaaegbabaaaacaaaaaaegiacaaaaaaaaaaaacaaaaaaogikcaaaaaaaaaaa
acaaaaaadiaaaaaiccaabaaaaaaaaaaabkaabaaaaaaaaaaaakiacaaaabaaaaaa
afaaaaaadiaaaaakncaabaaaabaaaaaaagahbaaaaaaaaaaaaceaaaaaaaaaaadp
aaaaaaaaaaaaaadpaaaaaadpdgaaaaaficcabaaaadaaaaaadkaabaaaaaaaaaaa
aaaaaaahdccabaaaadaaaaaakgakbaaaabaaaaaamgaabaaaabaaaaaadiaaaaai
bcaabaaaaaaaaaaabkbabaaaaaaaaaaackiacaaaacaaaaaaafaaaaaadcaaaaak
bcaabaaaaaaaaaaackiacaaaacaaaaaaaeaaaaaaakbabaaaaaaaaaaaakaabaaa
aaaaaaaadcaaaaakbcaabaaaaaaaaaaackiacaaaacaaaaaaagaaaaaackbabaaa
aaaaaaaaakaabaaaaaaaaaaadcaaaaakbcaabaaaaaaaaaaackiacaaaacaaaaaa
ahaaaaaadkbabaaaaaaaaaaaakaabaaaaaaaaaaadgaaaaageccabaaaadaaaaaa
akaabaiaebaaaaaaaaaaaaaadoaaaaabejfdeheogiaaaaaaadaaaaaaaiaaaaaa
faaaaaaaaaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapapaaaafjaaaaaaaaaaaaaa
aaaaaaaaadaaaaaaabaaaaaaapapaaaafpaaaaaaaaaaaaaaaaaaaaaaadaaaaaa
acaaaaaaadadaaaafaepfdejfeejepeoaaedepemepfcaafeeffiedepepfceeaa
epfdeheoieaaaaaaaeaaaaaaaiaaaaaagiaaaaaaaaaaaaaaabaaaaaaadaaaaaa
aaaaaaaaapaaaaaaheaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaaapaaaaaa
hkaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaaadamaaaahkaaaaaaabaaaaaa
aaaaaaaaadaaaaaaadaaaaaaapaaaaaafdfgfpfagphdgjhegjgpgoaaedepemep
fcaafeeffiedepepfceeaakl"
}

SubProgram "gles3 " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES3#version 300 es


#ifdef VERTEX

#define gl_Vertex _glesVertex
in vec4 _glesVertex;
#define gl_Color _glesColor
in vec4 _glesColor;
#define gl_MultiTexCoord0 _glesMultiTexCoord0
in vec4 _glesMultiTexCoord0;

#line 150
struct v2f_vertex_lit {
    highp vec2 uv;
    lowp vec4 diff;
    lowp vec4 spec;
};
#line 186
struct v2f_img {
    highp vec4 pos;
    mediump vec2 uv;
};
#line 180
struct appdata_img {
    highp vec4 vertex;
    mediump vec2 texcoord;
};
#line 315
struct v2f {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
    highp vec4 projPos;
};
#line 308
struct appdata_t {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
uniform highp vec4 _Time;
uniform highp vec4 _SinTime;
#line 3
uniform highp vec4 _CosTime;
uniform highp vec4 unity_DeltaTime;
uniform highp vec3 _WorldSpaceCameraPos;
uniform highp vec4 _ProjectionParams;
#line 7
uniform highp vec4 _ScreenParams;
uniform highp vec4 _ZBufferParams;
uniform highp vec4 unity_CameraWorldClipPlanes[6];
uniform highp vec4 _WorldSpaceLightPos0;
#line 11
uniform highp vec4 _LightPositionRange;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosZ0;
#line 15
uniform highp vec4 unity_4LightAtten0;
uniform highp vec4 unity_LightColor[4];
uniform highp vec4 unity_LightPosition[4];
uniform highp vec4 unity_LightAtten[4];
#line 19
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform highp vec4 unity_SHBr;
#line 23
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHC;
uniform highp vec3 unity_LightColor0;
uniform highp vec3 unity_LightColor1;
uniform highp vec3 unity_LightColor2;
uniform highp vec3 unity_LightColor3;
#line 27
uniform highp vec4 unity_ShadowSplitSpheres[4];
uniform highp vec4 unity_ShadowSplitSqRadii;
uniform highp vec4 unity_LightShadowBias;
uniform highp vec4 _LightSplitsNear;
#line 31
uniform highp vec4 _LightSplitsFar;
uniform highp mat4 unity_World2Shadow[4];
uniform highp vec4 _LightShadowData;
uniform highp vec4 unity_ShadowFadeCenterAndType;
#line 35
uniform highp mat4 glstate_matrix_mvp;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_invtrans_modelview0;
uniform highp mat4 _Object2World;
#line 39
uniform highp mat4 _World2Object;
uniform highp vec4 unity_Scale;
uniform highp mat4 glstate_matrix_transpose_modelview0;
uniform highp mat4 glstate_matrix_texture0;
#line 43
uniform highp mat4 glstate_matrix_texture1;
uniform highp mat4 glstate_matrix_texture2;
uniform highp mat4 glstate_matrix_texture3;
uniform highp mat4 glstate_matrix_projection;
#line 47
uniform highp vec4 glstate_lightmodel_ambient;
uniform highp mat4 unity_MatrixV;
uniform highp mat4 unity_MatrixVP;
uniform lowp vec4 unity_ColorSpaceGrey;
#line 76
#line 81
#line 86
#line 90
#line 95
#line 119
#line 136
#line 157
#line 165
#line 192
#line 205
#line 214
#line 219
#line 228
#line 233
#line 242
#line 259
#line 264
#line 290
#line 298
#line 302
#line 306
uniform sampler2D _MainTex;
uniform lowp vec4 _TintColor;
#line 323
uniform highp vec4 _MainTex_ST;
uniform sampler2D _CameraDepthTexture;
#line 335
uniform highp float _InvFade;
uniform highp float _Lum;
#line 283
highp vec4 ComputeScreenPos( in highp vec4 pos ) {
    #line 285
    highp vec4 o = (pos * 0.5);
    o.xy = (vec2( o.x, (o.y * _ProjectionParams.x)) + o.w);
    o.zw = pos.zw;
    return o;
}
#line 324
v2f vert( in appdata_t v ) {
    v2f o;
    #line 327
    o.vertex = (glstate_matrix_mvp * v.vertex);
    o.projPos = ComputeScreenPos( o.vertex);
    o.projPos.z = (-(glstate_matrix_modelview0 * v.vertex).z);
    o.color = v.color;
    #line 331
    o.texcoord = ((v.texcoord.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
    return o;
}
out lowp vec4 xlv_COLOR;
out highp vec2 xlv_TEXCOORD0;
out highp vec4 xlv_TEXCOORD1;
void main() {
    v2f xl_retval;
    appdata_t xlt_v;
    xlt_v.vertex = vec4(gl_Vertex);
    xlt_v.color = vec4(gl_Color);
    xlt_v.texcoord = vec2(gl_MultiTexCoord0);
    xl_retval = vert( xlt_v);
    gl_Position = vec4(xl_retval.vertex);
    xlv_COLOR = vec4(xl_retval.color);
    xlv_TEXCOORD0 = vec2(xl_retval.texcoord);
    xlv_TEXCOORD1 = vec4(xl_retval.projPos);
}


#endif
#ifdef FRAGMENT

#define gl_FragData _glesFragData
layout(location = 0) out mediump vec4 _glesFragData[4];
float xll_saturate_f( float x) {
  return clamp( x, 0.0, 1.0);
}
vec2 xll_saturate_vf2( vec2 x) {
  return clamp( x, 0.0, 1.0);
}
vec3 xll_saturate_vf3( vec3 x) {
  return clamp( x, 0.0, 1.0);
}
vec4 xll_saturate_vf4( vec4 x) {
  return clamp( x, 0.0, 1.0);
}
mat2 xll_saturate_mf2x2(mat2 m) {
  return mat2( clamp(m[0], 0.0, 1.0), clamp(m[1], 0.0, 1.0));
}
mat3 xll_saturate_mf3x3(mat3 m) {
  return mat3( clamp(m[0], 0.0, 1.0), clamp(m[1], 0.0, 1.0), clamp(m[2], 0.0, 1.0));
}
mat4 xll_saturate_mf4x4(mat4 m) {
  return mat4( clamp(m[0], 0.0, 1.0), clamp(m[1], 0.0, 1.0), clamp(m[2], 0.0, 1.0), clamp(m[3], 0.0, 1.0));
}
#line 150
struct v2f_vertex_lit {
    highp vec2 uv;
    lowp vec4 diff;
    lowp vec4 spec;
};
#line 186
struct v2f_img {
    highp vec4 pos;
    mediump vec2 uv;
};
#line 180
struct appdata_img {
    highp vec4 vertex;
    mediump vec2 texcoord;
};
#line 315
struct v2f {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
    highp vec4 projPos;
};
#line 308
struct appdata_t {
    highp vec4 vertex;
    lowp vec4 color;
    highp vec2 texcoord;
};
uniform highp vec4 _Time;
uniform highp vec4 _SinTime;
#line 3
uniform highp vec4 _CosTime;
uniform highp vec4 unity_DeltaTime;
uniform highp vec3 _WorldSpaceCameraPos;
uniform highp vec4 _ProjectionParams;
#line 7
uniform highp vec4 _ScreenParams;
uniform highp vec4 _ZBufferParams;
uniform highp vec4 unity_CameraWorldClipPlanes[6];
uniform highp vec4 _WorldSpaceLightPos0;
#line 11
uniform highp vec4 _LightPositionRange;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosZ0;
#line 15
uniform highp vec4 unity_4LightAtten0;
uniform highp vec4 unity_LightColor[4];
uniform highp vec4 unity_LightPosition[4];
uniform highp vec4 unity_LightAtten[4];
#line 19
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform highp vec4 unity_SHBr;
#line 23
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHC;
uniform highp vec3 unity_LightColor0;
uniform highp vec3 unity_LightColor1;
uniform highp vec3 unity_LightColor2;
uniform highp vec3 unity_LightColor3;
#line 27
uniform highp vec4 unity_ShadowSplitSpheres[4];
uniform highp vec4 unity_ShadowSplitSqRadii;
uniform highp vec4 unity_LightShadowBias;
uniform highp vec4 _LightSplitsNear;
#line 31
uniform highp vec4 _LightSplitsFar;
uniform highp mat4 unity_World2Shadow[4];
uniform highp vec4 _LightShadowData;
uniform highp vec4 unity_ShadowFadeCenterAndType;
#line 35
uniform highp mat4 glstate_matrix_mvp;
uniform highp mat4 glstate_matrix_modelview0;
uniform highp mat4 glstate_matrix_invtrans_modelview0;
uniform highp mat4 _Object2World;
#line 39
uniform highp mat4 _World2Object;
uniform highp vec4 unity_Scale;
uniform highp mat4 glstate_matrix_transpose_modelview0;
uniform highp mat4 glstate_matrix_texture0;
#line 43
uniform highp mat4 glstate_matrix_texture1;
uniform highp mat4 glstate_matrix_texture2;
uniform highp mat4 glstate_matrix_texture3;
uniform highp mat4 glstate_matrix_projection;
#line 47
uniform highp vec4 glstate_lightmodel_ambient;
uniform highp mat4 unity_MatrixV;
uniform highp mat4 unity_MatrixVP;
uniform lowp vec4 unity_ColorSpaceGrey;
#line 76
#line 81
#line 86
#line 90
#line 95
#line 119
#line 136
#line 157
#line 165
#line 192
#line 205
#line 214
#line 219
#line 228
#line 233
#line 242
#line 259
#line 264
#line 290
#line 298
#line 302
#line 306
uniform sampler2D _MainTex;
uniform lowp vec4 _TintColor;
#line 323
uniform highp vec4 _MainTex_ST;
uniform sampler2D _CameraDepthTexture;
#line 335
uniform highp float _InvFade;
uniform highp float _Lum;
#line 279
highp float LinearEyeDepth( in highp float z ) {
    #line 281
    return (1.0 / ((_ZBufferParams.z * z) + _ZBufferParams.w));
}
#line 172
lowp float Luminance( in lowp vec3 c ) {
    #line 174
    return dot( c, vec3( 0.22, 0.707, 0.071));
}
#line 337
lowp vec4 frag( in v2f i ) {
    #line 339
    highp float sceneZ = LinearEyeDepth( textureProj( _CameraDepthTexture, i.projPos).x);
    highp float partZ = i.projPos.z;
    highp float fade = xll_saturate_f((_InvFade * (sceneZ - partZ)));
    i.color.w *= fade;
    #line 343
    lowp vec4 texCol = texture( _MainTex, i.texcoord);
    highp float over = clamp( ((Luminance( vec3( texCol)) * _Lum) - 1.0), 0.0, 1.0);
    lowp vec4 tint = _TintColor;
    tint.xyz += (1.0 * over);
    #line 347
    tint *= _Lum;
    return (((2.0 * i.color) * texCol) * tint);
}
in lowp vec4 xlv_COLOR;
in highp vec2 xlv_TEXCOORD0;
in highp vec4 xlv_TEXCOORD1;
void main() {
    lowp vec4 xl_retval;
    v2f xlt_i;
    xlt_i.vertex = vec4(0.0);
    xlt_i.color = vec4(xlv_COLOR);
    xlt_i.texcoord = vec2(xlv_TEXCOORD0);
    xlt_i.projPos = vec4(xlv_TEXCOORD1);
    xl_retval = frag( xlt_i);
    gl_FragData[0] = vec4(xl_retval);
}


#endif"
}

}
Program "fp" {
// Fragment combos: 2
//   opengl - ALU: 10 to 17, TEX: 1 to 2
//   d3d9 - ALU: 10 to 16, TEX: 1 to 2
//   d3d11 - ALU: 7 to 12, TEX: 1 to 2, FLOW: 1 to 1
//   d3d11_9x - ALU: 7 to 12, TEX: 1 to 2, FLOW: 1 to 1
SubProgram "opengl " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_TintColor]
Float 1 [_Lum]
SetTexture 0 [_MainTex] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 10 ALU, 1 TEX
PARAM c[4] = { program.local[0..1],
		{ 0.2199707, 0.70703125, 0.070983887, 1 },
		{ 2 } };
TEMP R0;
TEMP R1;
TEX R0, fragment.texcoord[0], texture[0], 2D;
DP3 R1.x, R0, c[2];
MUL R1.x, R1, c[1];
ADD_SAT R1.x, R1, -c[2].w;
MOV R1.w, c[0];
ADD R1.xyz, R1.x, c[0];
MUL R1, R1, c[1].x;
MUL R0, fragment.color.primary, R0;
MUL R0, R0, R1;
MUL result.color, R0, c[3].x;
END
# 10 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_TintColor]
Float 1 [_Lum]
SetTexture 0 [_MainTex] 2D
"ps_2_0
; 10 ALU, 1 TEX
dcl_2d s0
def c2, 0.21997070, 0.70703125, 0.07098389, -1.00000000
def c3, 2.00000000, 0, 0, 0
dcl v0
dcl t0.xy
texld r1, t0, s0
dp3_pp r0.x, r1, c2
mul r0.x, r0, c1
add_sat r0.x, r0, c2.w
mov_pp r0.w, c0
add_pp r0.xyz, r0.x, c0
mul_pp r0, r0, c1.x
mul r1, v0, r1
mul r0, r1, r0
mul r0, r0, c3.x
mov_pp oC0, r0
"
}

SubProgram "d3d11 " {
Keywords { "SOFTPARTICLES_OFF" }
ConstBuffer "$Globals" 64 // 56 used size, 5 vars
Vector 16 [_TintColor] 4
Float 52 [_Lum]
BindCB "$Globals" 0
SetTexture 0 [_MainTex] 2D 0
// 10 instructions, 3 temp regs, 0 temp arrays:
// ALU 7 float, 0 int, 0 uint
// TEX 1 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"ps_4_0
eefiecedmdakghemilkegljihilbadmgppimpmlaabaaaaaagmacaaaaadaaaaaa
cmaaaaaakaaaaaaaneaaaaaaejfdeheogmaaaaaaadaaaaaaaiaaaaaafaaaaaaa
aaaaaaaaabaaaaaaadaaaaaaaaaaaaaaapaaaaaafmaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapapaaaagcaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
adadaaaafdfgfpfagphdgjhegjgpgoaaedepemepfcaafeeffiedepepfceeaakl
epfdeheocmaaaaaaabaaaaaaaiaaaaaacaaaaaaaaaaaaaaaaaaaaaaaadaaaaaa
aaaaaaaaapaaaaaafdfgfpfegbhcghgfheaaklklfdeieefcjaabaaaaeaaaaaaa
geaaaaaafjaaaaaeegiocaaaaaaaaaaaaeaaaaaafkaaaaadaagabaaaaaaaaaaa
fibiaaaeaahabaaaaaaaaaaaffffaaaagcbaaaadpcbabaaaabaaaaaagcbaaaad
dcbabaaaacaaaaaagfaaaaadpccabaaaaaaaaaaagiaaaaacadaaaaaaefaaaaaj
pcaabaaaaaaaaaaaegbabaaaacaaaaaaeghobaaaaaaaaaaaaagabaaaaaaaaaaa
baaaaaakbcaabaaaabaaaaaaegacbaaaaaaaaaaaaceaaaaakoehgbdopepndedp
hdgijbdnaaaaaaaadccaaaakbcaabaaaabaaaaaaakaabaaaabaaaaaabkiacaaa
aaaaaaaaadaaaaaaabeaaaaaaaaaialpaaaaaaaihcaabaaaabaaaaaaagaabaaa
abaaaaaaegiccaaaaaaaaaaaabaaaaaadiaaaaaihcaabaaaabaaaaaaegacbaaa
abaaaaaafgifcaaaaaaaaaaaadaaaaaadiaaaaajicaabaaaabaaaaaadkiacaaa
aaaaaaaaabaaaaaabkiacaaaaaaaaaaaadaaaaaaaaaaaaahpcaabaaaacaaaaaa
egbobaaaabaaaaaaegbobaaaabaaaaaadiaaaaahpcaabaaaaaaaaaaaegaobaaa
aaaaaaaaegaobaaaacaaaaaadiaaaaahpccabaaaaaaaaaaaegaobaaaabaaaaaa
egaobaaaaaaaaaaadoaaaaab"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES"
}

SubProgram "flash " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_TintColor]
Float 1 [_Lum]
SetTexture 0 [_MainTex] 2D
"agal_ps
c2 0.219971 0.707031 0.070984 -1.0
c3 2.0 0.0 0.0 0.0
[bc]
ciaaaaaaabaaapacaaaaaaoeaeaaaaaaaaaaaaaaafaababb tex r1, v0, s0 <2d wrap linear point>
bcaaaaaaaaaaabacabaaaakeacaaaaaaacaaaaoeabaaaaaa dp3 r0.x, r1.xyzz, c2
adaaaaaaaaaaabacaaaaaaaaacaaaaaaabaaaaoeabaaaaaa mul r0.x, r0.x, c1
abaaaaaaaaaaabacaaaaaaaaacaaaaaaacaaaappabaaaaaa add r0.x, r0.x, c2.w
bgaaaaaaaaaaabacaaaaaaaaacaaaaaaaaaaaaaaaaaaaaaa sat r0.x, r0.x
aaaaaaaaaaaaaiacaaaaaaoeabaaaaaaaaaaaaaaaaaaaaaa mov r0.w, c0
abaaaaaaaaaaahacaaaaaaaaacaaaaaaaaaaaaoeabaaaaaa add r0.xyz, r0.x, c0
adaaaaaaaaaaapacaaaaaaoeacaaaaaaabaaaaaaabaaaaaa mul r0, r0, c1.x
adaaaaaaabaaapacahaaaaoeaeaaaaaaabaaaaoeacaaaaaa mul r1, v7, r1
adaaaaaaaaaaapacabaaaaoeacaaaaaaaaaaaaoeacaaaaaa mul r0, r1, r0
adaaaaaaaaaaapacaaaaaaoeacaaaaaaadaaaaaaabaaaaaa mul r0, r0, c3.x
aaaaaaaaaaaaapadaaaaaaoeacaaaaaaaaaaaaaaaaaaaaaa mov o0, r0
"
}

SubProgram "d3d11_9x " {
Keywords { "SOFTPARTICLES_OFF" }
ConstBuffer "$Globals" 64 // 56 used size, 5 vars
Vector 16 [_TintColor] 4
Float 52 [_Lum]
BindCB "$Globals" 0
SetTexture 0 [_MainTex] 2D 0
// 10 instructions, 3 temp regs, 0 temp arrays:
// ALU 7 float, 0 int, 0 uint
// TEX 1 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"ps_4_0_level_9_1
eefiecedgcnnngnjnpafcdpmblihpmkiclmpgbepabaaaaaakiadaaaaaeaaaaaa
daaaaaaagiabaaaaaaadaaaaheadaaaaebgpgodjdaabaaaadaabaaaaaaacpppp
paaaaaaaeaaaaaaaacaaciaaaaaaeaaaaaaaeaaaabaaceaaaaaaeaaaaaaaaaaa
aaaaabaaabaaaaaaaaaaaaaaaaaaadaaabaaabaaaaaaaaaaaaacppppfbaaaaaf
acaaapkakoehgbdopepndedphdgijbdnaaaaialpbpaaaaacaaaaaaiaaaaaapla
bpaaaaacaaaaaaiaabaaadlabpaaaaacaaaaaajaaaaiapkaecaaaaadaaaacpia
abaaoelaaaaioekaaiaaaaadabaaciiaaaaaoeiaacaaoekaabaaaaacabaaacia
abaaffkaaeaaaaaeabaabbiaabaappiaabaaffiaacaappkaacaaaaadacaachia
abaaaaiaaaaaoekaafaaaaadacaachiaacaaoeiaabaaffkaafaaaaadacaaciia
abaaffiaaaaappkaacaaaaadabaaapiaaaaaoelaaaaaoelaafaaaaadaaaaapia
aaaaoeiaabaaoeiaafaaaaadaaaacpiaacaaoeiaaaaaoeiaabaaaaacaaaicpia
aaaaoeiappppaaaafdeieefcjaabaaaaeaaaaaaageaaaaaafjaaaaaeegiocaaa
aaaaaaaaaeaaaaaafkaaaaadaagabaaaaaaaaaaafibiaaaeaahabaaaaaaaaaaa
ffffaaaagcbaaaadpcbabaaaabaaaaaagcbaaaaddcbabaaaacaaaaaagfaaaaad
pccabaaaaaaaaaaagiaaaaacadaaaaaaefaaaaajpcaabaaaaaaaaaaaegbabaaa
acaaaaaaeghobaaaaaaaaaaaaagabaaaaaaaaaaabaaaaaakbcaabaaaabaaaaaa
egacbaaaaaaaaaaaaceaaaaakoehgbdopepndedphdgijbdnaaaaaaaadccaaaak
bcaabaaaabaaaaaaakaabaaaabaaaaaabkiacaaaaaaaaaaaadaaaaaaabeaaaaa
aaaaialpaaaaaaaihcaabaaaabaaaaaaagaabaaaabaaaaaaegiccaaaaaaaaaaa
abaaaaaadiaaaaaihcaabaaaabaaaaaaegacbaaaabaaaaaafgifcaaaaaaaaaaa
adaaaaaadiaaaaajicaabaaaabaaaaaadkiacaaaaaaaaaaaabaaaaaabkiacaaa
aaaaaaaaadaaaaaaaaaaaaahpcaabaaaacaaaaaaegbobaaaabaaaaaaegbobaaa
abaaaaaadiaaaaahpcaabaaaaaaaaaaaegaobaaaaaaaaaaaegaobaaaacaaaaaa
diaaaaahpccabaaaaaaaaaaaegaobaaaabaaaaaaegaobaaaaaaaaaaadoaaaaab
ejfdeheogmaaaaaaadaaaaaaaiaaaaaafaaaaaaaaaaaaaaaabaaaaaaadaaaaaa
aaaaaaaaapaaaaaafmaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaaapapaaaa
gcaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaaadadaaaafdfgfpfagphdgjhe
gjgpgoaaedepemepfcaafeeffiedepepfceeaaklepfdeheocmaaaaaaabaaaaaa
aiaaaaaacaaaaaaaaaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapaaaaaafdfgfpfe
gbhcghgfheaaklkl"
}

SubProgram "gles3 " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES3"
}

SubProgram "opengl " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_TintColor]
Float 2 [_InvFade]
Float 3 [_Lum]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 17 ALU, 2 TEX
PARAM c[6] = { program.local[0..3],
		{ 0.2199707, 0.70703125, 0.070983887, 1 },
		{ 2 } };
TEMP R0;
TEMP R1;
TEMP R2;
TEX R0, fragment.texcoord[0], texture[1], 2D;
TXP R1.x, fragment.texcoord[1], texture[0], 2D;
MAD R1.w, R1.x, c[0].z, c[0];
RCP R2.x, R1.w;
DP3 R1.y, R0, c[4];
ADD R2.x, R2, -fragment.texcoord[1].z;
MUL_SAT R2.w, R2.x, c[2].x;
MUL R1.y, R1, c[3].x;
ADD_SAT R1.y, R1, -c[4].w;
ADD R1.xyz, R1.y, c[1];
MOV R1.w, c[1];
MUL R1, R1, c[3].x;
MOV R2.xyz, fragment.color.primary;
MUL R2.w, fragment.color.primary, R2;
MUL R0, R2, R0;
MUL R0, R0, R1;
MUL result.color, R0, c[5].x;
END
# 17 instructions, 3 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_TintColor]
Float 2 [_InvFade]
Float 3 [_Lum]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"ps_2_0
; 16 ALU, 2 TEX
dcl_2d s0
dcl_2d s1
def c4, 0.21997070, 0.70703125, 0.07098389, -1.00000000
def c5, 2.00000000, 0, 0, 0
dcl v0
dcl t0.xy
dcl t1
texldp r0, t1, s0
texld r1, t0, s1
mad r0.x, r0, c0.z, c0.w
dp3_pp r2.x, r1, c4
rcp r0.x, r0.x
mul r2.x, r2, c3
add r0.x, r0, -t1.z
mul_sat r0.x, r0, c2
add_sat r2.x, r2, c4.w
mov_pp r2.w, c1
add_pp r2.xyz, r2.x, c1
mul_pp r2, r2, c3.x
mov_pp r3.xyz, v0
mul_pp r3.w, v0, r0.x
mul r0, r3, r1
mul r0, r0, r2
mul r0, r0, c5.x
mov_pp oC0, r0
"
}

SubProgram "d3d11 " {
Keywords { "SOFTPARTICLES_ON" }
ConstBuffer "$Globals" 64 // 56 used size, 5 vars
Vector 16 [_TintColor] 4
Float 48 [_InvFade]
Float 52 [_Lum]
ConstBuffer "UnityPerCamera" 128 // 128 used size, 8 vars
Vector 112 [_ZBufferParams] 4
BindCB "$Globals" 0
BindCB "UnityPerCamera" 1
SetTexture 0 [_CameraDepthTexture] 2D 1
SetTexture 1 [_MainTex] 2D 0
// 18 instructions, 2 temp regs, 0 temp arrays:
// ALU 12 float, 0 int, 0 uint
// TEX 2 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"ps_4_0
eefiecedpgjphnhopoaiafjcapaeikofldhlojbnabaaaaaamaadaaaaadaaaaaa
cmaaaaaaliaaaaaaomaaaaaaejfdeheoieaaaaaaaeaaaaaaaiaaaaaagiaaaaaa
aaaaaaaaabaaaaaaadaaaaaaaaaaaaaaapaaaaaaheaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapapaaaahkaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
adadaaaahkaaaaaaabaaaaaaaaaaaaaaadaaaaaaadaaaaaaapapaaaafdfgfpfa
gphdgjhegjgpgoaaedepemepfcaafeeffiedepepfceeaaklepfdeheocmaaaaaa
abaaaaaaaiaaaaaacaaaaaaaaaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapaaaaaa
fdfgfpfegbhcghgfheaaklklfdeieefcmmacaaaaeaaaaaaaldaaaaaafjaaaaae
egiocaaaaaaaaaaaaeaaaaaafjaaaaaeegiocaaaabaaaaaaaiaaaaaafkaaaaad
aagabaaaaaaaaaaafkaaaaadaagabaaaabaaaaaafibiaaaeaahabaaaaaaaaaaa
ffffaaaafibiaaaeaahabaaaabaaaaaaffffaaaagcbaaaadpcbabaaaabaaaaaa
gcbaaaaddcbabaaaacaaaaaagcbaaaadpcbabaaaadaaaaaagfaaaaadpccabaaa
aaaaaaaagiaaaaacacaaaaaaaoaaaaahdcaabaaaaaaaaaaaegbabaaaadaaaaaa
pgbpbaaaadaaaaaaefaaaaajpcaabaaaaaaaaaaaegaabaaaaaaaaaaaeghobaaa
aaaaaaaaaagabaaaabaaaaaadcaaaaalbcaabaaaaaaaaaaackiacaaaabaaaaaa
ahaaaaaaakaabaaaaaaaaaaadkiacaaaabaaaaaaahaaaaaaaoaaaaakbcaabaaa
aaaaaaaaaceaaaaaaaaaiadpaaaaiadpaaaaiadpaaaaiadpakaabaaaaaaaaaaa
aaaaaaaibcaabaaaaaaaaaaaakaabaaaaaaaaaaackbabaiaebaaaaaaadaaaaaa
dicaaaaibcaabaaaaaaaaaaaakaabaaaaaaaaaaaakiacaaaaaaaaaaaadaaaaaa
diaaaaahicaabaaaaaaaaaaaakaabaaaaaaaaaaadkbabaaaabaaaaaadgaaaaaf
hcaabaaaaaaaaaaaegbcbaaaabaaaaaaaaaaaaahpcaabaaaaaaaaaaaegaobaaa
aaaaaaaaegaobaaaaaaaaaaaefaaaaajpcaabaaaabaaaaaaegbabaaaacaaaaaa
eghobaaaabaaaaaaaagabaaaaaaaaaaadiaaaaahpcaabaaaaaaaaaaaegaobaaa
aaaaaaaaegaobaaaabaaaaaabaaaaaakbcaabaaaabaaaaaaegacbaaaabaaaaaa
aceaaaaakoehgbdopepndedphdgijbdnaaaaaaaadccaaaakbcaabaaaabaaaaaa
akaabaaaabaaaaaabkiacaaaaaaaaaaaadaaaaaaabeaaaaaaaaaialpaaaaaaai
hcaabaaaabaaaaaaagaabaaaabaaaaaaegiccaaaaaaaaaaaabaaaaaadiaaaaai
hcaabaaaabaaaaaaegacbaaaabaaaaaafgifcaaaaaaaaaaaadaaaaaadiaaaaaj
icaabaaaabaaaaaadkiacaaaaaaaaaaaabaaaaaabkiacaaaaaaaaaaaadaaaaaa
diaaaaahpccabaaaaaaaaaaaegaobaaaaaaaaaaaegaobaaaabaaaaaadoaaaaab
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES"
}

SubProgram "flash " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_TintColor]
Float 2 [_InvFade]
Float 3 [_Lum]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"agal_ps
c4 1.0 0.003922 0.000015 0.0
c5 0.219971 0.707031 0.070984 -1.0
c6 2.0 0.0 0.0 0.0
[bc]
aeaaaaaaaaaaapacabaaaaoeaeaaaaaaabaaaappaeaaaaaa div r0, v1, v1.w
ciaaaaaaaaaaapacaaaaaafeacaaaaaaaaaaaaaaafaababb tex r0, r0.xyyy, s0 <2d wrap linear point>
ciaaaaaaabaaapacaaaaaaoeaeaaaaaaabaaaaaaafaababb tex r1, v0, s1 <2d wrap linear point>
bdaaaaaaaaaaabacaaaaaaoeacaaaaaaaeaaaaoeabaaaaaa dp4 r0.x, r0, c4
adaaaaaaaaaaabacaaaaaaaaacaaaaaaaaaaaakkabaaaaaa mul r0.x, r0.x, c0.z
abaaaaaaaaaaabacaaaaaaaaacaaaaaaaaaaaappabaaaaaa add r0.x, r0.x, c0.w
bcaaaaaaacaaabacabaaaakeacaaaaaaafaaaaoeabaaaaaa dp3 r2.x, r1.xyzz, c5
afaaaaaaaaaaabacaaaaaaaaacaaaaaaaaaaaaaaaaaaaaaa rcp r0.x, r0.x
adaaaaaaacaaabacacaaaaaaacaaaaaaadaaaaoeabaaaaaa mul r2.x, r2.x, c3
acaaaaaaaaaaabacaaaaaaaaacaaaaaaabaaaakkaeaaaaaa sub r0.x, r0.x, v1.z
adaaaaaaaaaaabacaaaaaaaaacaaaaaaacaaaaoeabaaaaaa mul r0.x, r0.x, c2
bgaaaaaaaaaaabacaaaaaaaaacaaaaaaaaaaaaaaaaaaaaaa sat r0.x, r0.x
abaaaaaaacaaabacacaaaaaaacaaaaaaafaaaappabaaaaaa add r2.x, r2.x, c5.w
bgaaaaaaacaaabacacaaaaaaacaaaaaaaaaaaaaaaaaaaaaa sat r2.x, r2.x
aaaaaaaaacaaaiacabaaaaoeabaaaaaaaaaaaaaaaaaaaaaa mov r2.w, c1
abaaaaaaacaaahacacaaaaaaacaaaaaaabaaaaoeabaaaaaa add r2.xyz, r2.x, c1
adaaaaaaacaaapacacaaaaoeacaaaaaaadaaaaaaabaaaaaa mul r2, r2, c3.x
aaaaaaaaadaaahacahaaaaoeaeaaaaaaaaaaaaaaaaaaaaaa mov r3.xyz, v7
adaaaaaaadaaaiacahaaaaoeaeaaaaaaaaaaaaaaacaaaaaa mul r3.w, v7, r0.x
adaaaaaaaaaaapacadaaaaoeacaaaaaaabaaaaoeacaaaaaa mul r0, r3, r1
adaaaaaaaaaaapacaaaaaaoeacaaaaaaacaaaaoeacaaaaaa mul r0, r0, r2
adaaaaaaaaaaapacaaaaaaoeacaaaaaaagaaaaaaabaaaaaa mul r0, r0, c6.x
aaaaaaaaaaaaapadaaaaaaoeacaaaaaaaaaaaaaaaaaaaaaa mov o0, r0
"
}

SubProgram "d3d11_9x " {
Keywords { "SOFTPARTICLES_ON" }
ConstBuffer "$Globals" 64 // 56 used size, 5 vars
Vector 16 [_TintColor] 4
Float 48 [_InvFade]
Float 52 [_Lum]
ConstBuffer "UnityPerCamera" 128 // 128 used size, 8 vars
Vector 112 [_ZBufferParams] 4
BindCB "$Globals" 0
BindCB "UnityPerCamera" 1
SetTexture 0 [_CameraDepthTexture] 2D 1
SetTexture 1 [_MainTex] 2D 0
// 18 instructions, 2 temp regs, 0 temp arrays:
// ALU 12 float, 0 int, 0 uint
// TEX 2 (0 load, 0 comp, 0 bias, 0 grad)
// FLOW 1 static, 0 dynamic
"ps_4_0_level_9_1
eefiecedfjlokpmfpfipmpcighlfbbnfohnehdeiabaaaaaakmafaaaaaeaaaaaa
daaaaaaabiacaaaaomaeaaaahiafaaaaebgpgodjoaabaaaaoaabaaaaaaacpppp
jaabaaaafaaaaaaaadaacmaaaaaafaaaaaaafaaaacaaceaaaaaafaaaabaaaaaa
aaababaaaaaaabaaabaaaaaaaaaaaaaaaaaaadaaabaaabaaaaaaaaaaabaaahaa
abaaacaaaaaaaaaaaaacppppfbaaaaafadaaapkakoehgbdopepndedphdgijbdn
aaaaialpbpaaaaacaaaaaaiaaaaaaplabpaaaaacaaaaaaiaabaaadlabpaaaaac
aaaaaaiaacaaaplabpaaaaacaaaaaajaaaaiapkabpaaaaacaaaaaajaabaiapka
agaaaaacaaaaaiiaacaapplaafaaaaadaaaaadiaaaaappiaacaaoelaecaaaaad
aaaaapiaaaaaoeiaabaioekaecaaaaadabaacpiaabaaoelaaaaioekaaeaaaaae
aaaaabiaacaakkkaaaaaaaiaacaappkaagaaaaacaaaaabiaaaaaaaiaacaaaaad
aaaaabiaaaaaaaiaacaakklbafaaaaadaaaabbiaaaaaaaiaabaaaakaafaaaaad
aaaaciiaaaaaaaiaaaaapplaabaaaaacaaaaahiaaaaaoelaacaaaaadaaaaapia
aaaaoeiaaaaaoeiaafaaaaadaaaaapiaabaaoeiaaaaaoeiaaiaaaaadabaacbia
abaaoeiaadaaoekaabaaaaacabaaaciaabaaffkaaeaaaaaeabaabbiaabaaaaia
abaaffiaadaappkaacaaaaadacaachiaabaaaaiaaaaaoekaafaaaaadacaachia
acaaoeiaabaaffkaafaaaaadacaaciiaabaaffiaaaaappkaafaaaaadaaaacpia
aaaaoeiaacaaoeiaabaaaaacaaaicpiaaaaaoeiappppaaaafdeieefcmmacaaaa
eaaaaaaaldaaaaaafjaaaaaeegiocaaaaaaaaaaaaeaaaaaafjaaaaaeegiocaaa
abaaaaaaaiaaaaaafkaaaaadaagabaaaaaaaaaaafkaaaaadaagabaaaabaaaaaa
fibiaaaeaahabaaaaaaaaaaaffffaaaafibiaaaeaahabaaaabaaaaaaffffaaaa
gcbaaaadpcbabaaaabaaaaaagcbaaaaddcbabaaaacaaaaaagcbaaaadpcbabaaa
adaaaaaagfaaaaadpccabaaaaaaaaaaagiaaaaacacaaaaaaaoaaaaahdcaabaaa
aaaaaaaaegbabaaaadaaaaaapgbpbaaaadaaaaaaefaaaaajpcaabaaaaaaaaaaa
egaabaaaaaaaaaaaeghobaaaaaaaaaaaaagabaaaabaaaaaadcaaaaalbcaabaaa
aaaaaaaackiacaaaabaaaaaaahaaaaaaakaabaaaaaaaaaaadkiacaaaabaaaaaa
ahaaaaaaaoaaaaakbcaabaaaaaaaaaaaaceaaaaaaaaaiadpaaaaiadpaaaaiadp
aaaaiadpakaabaaaaaaaaaaaaaaaaaaibcaabaaaaaaaaaaaakaabaaaaaaaaaaa
ckbabaiaebaaaaaaadaaaaaadicaaaaibcaabaaaaaaaaaaaakaabaaaaaaaaaaa
akiacaaaaaaaaaaaadaaaaaadiaaaaahicaabaaaaaaaaaaaakaabaaaaaaaaaaa
dkbabaaaabaaaaaadgaaaaafhcaabaaaaaaaaaaaegbcbaaaabaaaaaaaaaaaaah
pcaabaaaaaaaaaaaegaobaaaaaaaaaaaegaobaaaaaaaaaaaefaaaaajpcaabaaa
abaaaaaaegbabaaaacaaaaaaeghobaaaabaaaaaaaagabaaaaaaaaaaadiaaaaah
pcaabaaaaaaaaaaaegaobaaaaaaaaaaaegaobaaaabaaaaaabaaaaaakbcaabaaa
abaaaaaaegacbaaaabaaaaaaaceaaaaakoehgbdopepndedphdgijbdnaaaaaaaa
dccaaaakbcaabaaaabaaaaaaakaabaaaabaaaaaabkiacaaaaaaaaaaaadaaaaaa
abeaaaaaaaaaialpaaaaaaaihcaabaaaabaaaaaaagaabaaaabaaaaaaegiccaaa
aaaaaaaaabaaaaaadiaaaaaihcaabaaaabaaaaaaegacbaaaabaaaaaafgifcaaa
aaaaaaaaadaaaaaadiaaaaajicaabaaaabaaaaaadkiacaaaaaaaaaaaabaaaaaa
bkiacaaaaaaaaaaaadaaaaaadiaaaaahpccabaaaaaaaaaaaegaobaaaaaaaaaaa
egaobaaaabaaaaaadoaaaaabejfdeheoieaaaaaaaeaaaaaaaiaaaaaagiaaaaaa
aaaaaaaaabaaaaaaadaaaaaaaaaaaaaaapaaaaaaheaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapapaaaahkaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
adadaaaahkaaaaaaabaaaaaaaaaaaaaaadaaaaaaadaaaaaaapapaaaafdfgfpfa
gphdgjhegjgpgoaaedepemepfcaafeeffiedepepfceeaaklepfdeheocmaaaaaa
abaaaaaaaiaaaaaacaaaaaaaaaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapaaaaaa
fdfgfpfegbhcghgfheaaklkl"
}

SubProgram "gles3 " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES3"
}

}

#LINE 99
 
			}
		} 	
	
		// ---- Dual texture cards
		SubShader {
			Pass {
				SetTexture [_MainTex] {
					constantColor [_TintColor]
					combine constant * primary
				}
				SetTexture [_MainTex] {
					combine texture * previous DOUBLE
				}
			}
		}
	
		// ---- Single texture cards (does not do color tint)
		SubShader
		{
			Pass
			{
				SetTexture [_MainTex] { 
					combine texture * primary 
				}
			}
		}
	}
}
