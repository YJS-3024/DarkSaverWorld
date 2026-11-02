using System;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.UI
{
    [AddComponentMenu("UI/DarkSaver Native Bitmap Text")]
    [RequireComponent(typeof(CanvasRenderer),typeof(RectTransform))]
    public sealed class NativeBitmapText : Text
    {
        public bool NativeParagraphWrap;
        private NativeBitmapFont original;
        private NativeTextSurface surface;private bool clipKnown;private Matrix4x4 clipMatrix;private Rect clipBounds;
        private static int variant;
        private static event Action Changed;
        public static int Variant=>variant;
        public static bool SetVariant(int value)
        {
            var font=Resources.Load<NativeBitmapFont>("OriginalBitmapFont");
            if(value<0||value>1||font?.Ready!=true)return false;
            variant=value;Changed?.Invoke();return true;
        }
        private NativeBitmapFont Original=>original!=null?original:original=Resources.Load<NativeBitmapFont>("OriginalBitmapFont");
        public override Texture mainTexture=>Original?.Ready==true?Original.Texture(variant):base.mainTexture;
        public override Material defaultMaterial=>Original?.Ready==true?defaultGraphicMaterial:base.defaultMaterial;
        protected override void OnEnable(){base.OnEnable();surface=GetComponentInParent<NativeTextSurface>();clipKnown=false;Changed+=RefreshOriginal;}
        protected override void OnDisable(){Changed-=RefreshOriginal;base.OnDisable();}
        protected override void OnTransformParentChanged(){surface=GetComponentInParent<NativeTextSurface>();clipKnown=false;base.OnTransformParentChanged();}
        private void LateUpdate()
        {
            if(surface==null)return;var target=surface.Surface;var matrix=target.worldToLocalMatrix*rectTransform.localToWorldMatrix;var bounds=target.rect;
            if(!clipKnown||matrix!=clipMatrix||bounds!=clipBounds){clipKnown=true;clipMatrix=matrix;clipBounds=bounds;SetVerticesDirty();}
        }
        private void RefreshOriginal(){SetVerticesDirty();SetMaterialDirty();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            var font=Original;if(font?.Ready!=true){base.OnPopulateMesh(mesh);return;}
            mesh.Clear();var rect=rectTransform.rect;var atlas=font.Texture(variant);
            var target=surface==null?null:surface.Surface;var matrix=target==null?Matrix4x4.identity:target.worldToLocalMatrix*rectTransform.localToWorldMatrix;
            var cells=NativeTextMetrics.Layout(text,font.Mapping,Mathf.FloorToInt(rect.width),Mathf.FloorToInt(rect.height),horizontalOverflow==HorizontalWrapMode.Wrap,
                (int)alignment%3,(int)alignment/3,verticalOverflow==VerticalWrapMode.Truncate,NativeParagraphWrap);
            foreach(var cell in cells)
            {
                float x=rect.xMin+cell.X,y=rect.yMax-cell.Y;int index=mesh.currentVertCount;
                if(target!=null)
                {
                    var origin=matrix.MultiplyPoint3x4(new Vector3(x,y));var bounds=target.rect;
                    if(!NativeTextMetrics.GlyphVisible(origin.x-bounds.xMin,bounds.yMax-origin.y,bounds.width,bounds.height))continue;
                }
                float left=(float)(cell.Glyph%NativeTextMetrics.Columns*14)/atlas.width,right=left+14f/atlas.width;
                float top=1f-(float)(cell.Glyph/NativeTextMetrics.Columns*14)/atlas.height,bottom=top-14f/atlas.height;
                mesh.AddVert(new Vector3(x,y),color,new Vector2(left,top));mesh.AddVert(new Vector3(x+14,y),color,new Vector2(right,top));
                mesh.AddVert(new Vector3(x+14,y-14),color,new Vector2(right,bottom));mesh.AddVert(new Vector3(x,y-14),color,new Vector2(left,bottom));
                mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index+2,index+3,index);
            }
        }
    }
}
