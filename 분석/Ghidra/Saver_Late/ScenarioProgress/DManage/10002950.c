
/* public: static class CRect __cdecl CAlphaProcess::GetAdjustedRect(class CDDSurface *,class
   CPoint,class CRect &) */

void __cdecl
CAlphaProcess::GetAdjustedRect
          (int *param_1,CDDSurface *param_2,int param_3,int param_4,LPRECT param_5)

{
  int dy;
  int dx;
  LONG dx_00;
  LONG dy_00;
  tagRECT local_20;
  RECT local_10;
  
                    /* 0x2950  86
                       ?GetAdjustedRect@CAlphaProcess@@SA?AVCRect@@PAVCDDSurface@@VCPoint@@AAV2@@Z
                        */
  CDDSurface::GetRect(param_2,&local_20);
  local_10.right = param_5->right - param_5->left;
  local_10.bottom = param_5->bottom - param_5->top;
  local_10.left = param_3 - local_10.right / 2;
  local_10.top = param_4 - local_10.bottom / 2;
  local_10.right = local_10.left + local_10.right;
  local_10.bottom = local_10.top + local_10.bottom;
  IntersectRect(&local_20,&local_20,&local_10);
  dy_00 = local_20.top;
  dx_00 = local_20.left;
  if (local_10.left < 0) {
    param_5->left = -local_10.left;
  }
  if (local_10.top < 0) {
    param_5->top = -local_10.top;
  }
  dy = param_5->top;
  dx = param_5->left;
  OffsetRect(param_5,-dx,-dy);
  OffsetRect(&local_20,-dx_00,-dy_00);
  IntersectRect(&local_20,&local_20,param_5);
  OffsetRect(param_5,dx,dy);
  OffsetRect(&local_20,dx_00,dy_00);
  *param_1 = local_20.left;
  param_1[1] = local_20.top;
  param_1[2] = local_20.right;
  param_1[3] = local_20.bottom;
  return;
}

