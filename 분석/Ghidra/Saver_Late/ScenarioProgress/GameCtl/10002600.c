
undefined4 __thiscall FUN_10002600(void *this,CDDSurface *param_1)

{
  int iVar1;
  
  iVar1 = *(int *)((int)this + 4);
  *(int *)((int)this + 4) = iVar1 + 1;
  CAlphaEffectorForChar::DrawAlphaEffect
            (*(CAlphaEffectorForChar **)((int)this + 0x18),param_1,*(int *)((int)this + 0x20) + 0x10
             ,*(int *)((int)this + 0x24) + iVar1 * 6 + 0x10);
  CAlphaProcess::ToTheColor(param_1,(CRect *)0x0,0xffffff,*(uchar *)((int)this + 8));
  if (0xff < *(byte *)((int)this + 8) + 8) {
    return 1;
  }
  *(byte *)((int)this + 8) = *(byte *)((int)this + 8) + 8;
  return 0;
}

