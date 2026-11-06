
int __thiscall FUN_00440ce0(void *this,byte *param_1)

{
  int iVar1;
  byte *pbVar2;
  
  if (*(int *)((int)this + 0x94) != 0) {
    (**(code **)(*(int *)this + 0x8c))();
    DAT_0048f198 = 0;
    param_1 = (byte *)0x0;
  }
  if (*(int *)(*(int *)((int)this + 0x108) + -8) != 0) {
    pbVar2 = param_1;
    CString::CString((CString *)&stack0xfffffff0,(CString *)((int)this + 0x108));
    thunk_FUN_00441ca0(this,pbVar2);
  }
  if (*(int *)((int)this + 0xb4) != 0) {
    iVar1 = (**(code **)(*(int *)this + 0x1c))();
    return iVar1;
  }
  switch(param_1) {
  case (byte *)0x0:
  case (byte *)0x1:
  case (byte *)0x2:
  case (byte *)0x3:
  case (byte *)0x4:
  case (byte *)0x5:
  case (byte *)0x6:
  case (byte *)0x7:
  case (byte *)0x8:
  case (byte *)0x9:
    iVar1 = Update((int)param_1);
    return iVar1;
  case (byte *)0xd:
    iVar1 = (**(code **)(*(int *)this + 0x7c))();
    return iVar1;
  case (byte *)0xe:
    iVar1 = thunk_FUN_00429770((int)this);
    return iVar1;
  case (byte *)0xf:
    iVar1 = (**(code **)(*(int *)this + 0x80))();
    return iVar1;
  case (byte *)0x10:
    iVar1 = thunk_FUN_004297e0((int)this);
    return iVar1;
  case (byte *)0x11:
    iVar1 = (**(code **)(*(int *)this + 0x84))();
    return iVar1;
  case (byte *)0x12:
    iVar1 = Update(200);
    return iVar1;
  case (byte *)0x13:
    iVar1 = thunk_FUN_00429380(0,*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0x10),DAT_00495390,0)
    ;
    return iVar1;
  case (byte *)0x14:
    iVar1 = thunk_FUN_00429380(1,*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0x10),DAT_00495390,0)
    ;
    return iVar1;
  case (byte *)0x15:
    iVar1 = thunk_FUN_00429380(2,*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0x10),DAT_00495390,0)
    ;
    return iVar1;
  case (byte *)0x16:
    iVar1 = thunk_FUN_00429380(3,*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0x10),DAT_00495390,0)
    ;
    return iVar1;
  case (byte *)0x17:
    iVar1 = thunk_FUN_00429310(this,1);
    return iVar1;
  case (byte *)0x18:
    iVar1 = thunk_FUN_00429310(this,0);
    if (iVar1 == 0) {
      return 0;
    }
    iVar1 = CDDSurface::Flip(*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0xc),0);
    if (iVar1 == 0) {
      iVar1 = CDirectDraw::ErrorHandle(*(long *)(*(int *)(*(int *)((int)this + 0xb0) + 0xc) + 4));
      return iVar1;
    }
  }
  return 1;
}


/* incoming refs
004014e2 UNCONDITIONAL_CALL
*/
