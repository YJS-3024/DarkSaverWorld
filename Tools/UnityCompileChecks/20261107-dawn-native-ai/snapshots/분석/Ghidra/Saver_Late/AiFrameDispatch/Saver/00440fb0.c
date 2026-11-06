
int __thiscall FUN_00440fb0(void *this,int *param_1)

{
  int iVar1;
  undefined1 local_20 [12];
  LONG local_14;
  tagRECT local_10;
  
  local_10.left = 0;
  local_20._0_4_ = 0;
  local_10.top = 0;
  local_10.right = 0x280;
  local_10.bottom = 0x1e0;
  local_20._4_4_ = 0;
  local_20._8_4_ = 0x280;
  local_14 = 0x1e0;
  switch(*param_1) {
  case 0:
  case 1:
  case 2:
  case 3:
  case 4:
  case 5:
  case 6:
  case 7:
  case 8:
  case 9:
    iVar1 = Update(*param_1);
    return iVar1;
  case 0xe:
    iVar1 = (**(code **)(*(int *)this + 0x7c))();
    return iVar1;
  case 0xf:
    iVar1 = thunk_FUN_00429770((int)this);
    return iVar1;
  case 0x10:
    iVar1 = (**(code **)(*(int *)this + 0x80))();
    return iVar1;
  case 0x11:
    iVar1 = thunk_FUN_004297e0((int)this);
    return iVar1;
  case 0x16:
    iVar1 = (**(code **)(*(int *)this + 0x84))(DAT_00495390);
    return iVar1;
  case 0x17:
    iVar1 = Update(200);
    return iVar1;
  case 0x18:
    ClientToScreen(*(HWND *)((int)this + 0x104),(LPPOINT)local_20);
    ClientToScreen(*(HWND *)((int)this + 0x104),(LPPOINT)(local_20 + 8));
    iVar1 = CDDSurface::Blt(*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0xc),(tagRECT *)local_20,
                            DAT_00495390,&local_10,0x1000000,(_DDBLTFX *)0x0);
    if (iVar1 == 0) {
      iVar1 = CDirectDraw::ErrorHandle(*(long *)(*(int *)(*(int *)((int)this + 0xb0) + 0xc) + 4));
      return iVar1;
    }
  }
  return 1;
}

