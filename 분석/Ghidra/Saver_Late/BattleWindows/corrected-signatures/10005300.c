
undefined4 __cdecl
FUN_10005300(dword arg0,dword arg1,dword arg2,dword arg3,dword arg4,dword arg5,dword arg6,dword arg7
            ,dword arg8)

{
  dword dVar1;
  int yTop;
  dword dVar2;
  int xLeft;
  RECT local_30;
  tagRECT local_20;
  tagRECT local_10;
  
  dVar1 = arg5;
  dVar2 = arg4;
  arg7 = (arg7 - arg5) + arg2;
  arg6 = (arg6 - arg4) + arg1;
  arg4 = arg1;
  arg5 = arg2;
  CRect::NormalizeRect((CRect *)&arg4);
  local_30.left = 0;
  local_30.right = 0x280;
  local_30.top = 0;
  local_30.bottom = 0x1e0;
  IntersectRect(&local_20,(RECT *)&arg4,&local_30);
  xLeft = (dVar2 - arg4) + local_20.left;
  yTop = (local_20.top - arg5) + dVar1;
  SetRect(&local_10,xLeft,yTop,(local_20.right - local_20.left) + xLeft,
          (local_20.bottom - local_20.top) + yTop);
  dVar2 = local_30.top;
  if (local_30.top < (int)arg2) {
    dVar2 = arg2;
  }
  if ((int)arg1 <= local_30.left) {
    arg1 = local_30.left;
  }
  CDDSurface::BltFast(DAT_1001c470,arg1,dVar2,arg3,(dword)&local_10,arg8);
  return 1;
}

