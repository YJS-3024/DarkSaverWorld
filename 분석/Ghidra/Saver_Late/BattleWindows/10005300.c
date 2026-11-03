
undefined4 __cdecl
FUN_10005300(undefined4 param_1,LONG param_2,int param_3,undefined4 param_4,int param_5,int param_6,
            int param_7,int param_8,undefined4 param_9)

{
  int iVar1;
  int iVar2;
  RECT local_30;
  tagRECT local_20;
  tagRECT local_10;
  
  iVar1 = param_6;
  iVar2 = param_5;
  param_8 = (param_8 - param_6) + param_3;
  param_7 = (param_7 - param_5) + param_2;
  param_5 = param_2;
  param_6 = param_3;
  CRect::NormalizeRect((CRect *)&param_5);
  local_30.left = 0;
  local_30.right = 0x280;
  local_30.top = 0;
  local_30.bottom = 0x1e0;
  IntersectRect(&local_20,(RECT *)&param_5,&local_30);
  iVar2 = (iVar2 - param_5) + local_20.left;
  iVar1 = (local_20.top - param_6) + iVar1;
  SetRect(&local_10,iVar2,iVar1,(local_20.right - local_20.left) + iVar2,
          (local_20.bottom - local_20.top) + iVar1);
  iVar2 = local_30.top;
  if (local_30.top < param_3) {
    iVar2 = param_3;
  }
  if (param_2 <= local_30.left) {
    param_2 = local_30.left;
  }
  CDDSurface::BltFast(DAT_1001c470,param_2,iVar2,param_4,&local_10,param_9);
  return 1;
}

