
void __thiscall FUN_00464380(void *this,CDDSurface *param_1)

{
  CDDSurface *pCVar1;
  int iVar2;
  newmagicrecord *pnVar3;
  int iVar4;
  undefined4 unaff_EBX;
  undefined4 unaff_EBP;
  tagRECT local_20;
  tagRECT local_10;
  
  if ((*(int *)((int)this + 200) == 0) && (*(int *)((int)this + 0xd8) == 0)) {
    return;
  }
  pCVar1 = GetIconSurface();
  if (pCVar1 == (CDDSurface *)0x0) {
    return;
  }
  iVar4 = *(int *)((int)this + 0x13c);
  if (*(int *)((int)this + 200) == 0) {
    if (*(int *)((int)this + 0xd8) == 0) {
      return;
    }
    iVar2 = *(int *)((int)this + 0xdc) * 0x10;
    local_20.left = *(LONG *)(iVar2 + 0x2f4 + (int)this);
    local_20.top = *(LONG *)((int)this + iVar2 + 0x2f8);
    local_20.right = *(LONG *)((int)this + iVar2 + 0x2fc);
    local_20.bottom = *(LONG *)((int)this + iVar2 + 0x300);
    OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    OffsetRect(&local_20,*(int *)((int)this + 0x10) - *(int *)((int)this + 0xe0),
               *(int *)((int)this + 0x14) - *(int *)((int)this + 0xe4));
    pnVar3 = GetMagicTbl2((int)*(short *)(iVar4 + 0x70 + *(int *)((int)this + 0xdc) * 2));
    *(newmagicrecord **)((int)this + 0xc4) = pnVar3;
    if (pnVar3 == (newmagicrecord *)0x0) {
      return;
    }
    if (*(int *)pnVar3 != (int)*(short *)(iVar4 + 0x70 + *(int *)((int)this + 0xdc) * 2)) {
      return;
    }
    SetRect(&local_10,*(int *)(pnVar3 + 0x138),*(int *)(pnVar3 + 0x13c),
            *(int *)(pnVar3 + 0x138) + 0x20,*(int *)(pnVar3 + 0x13c) + 0x20);
    CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar1,&local_10,1);
    if (*(short *)(iVar4 + 0x44) == 0x10) goto LAB_004645cf;
    iVar4 = thunk_FUN_00464330(this,(POINT *)((int)this + 0x10));
  }
  else {
    iVar2 = *(int *)((int)this + 0xcc) * 0x10;
    local_20.left = *(LONG *)(iVar2 + 500 + (int)this);
    local_20.top = *(LONG *)((int)this + iVar2 + 0x1f8);
    local_20.right = *(LONG *)((int)this + iVar2 + 0x1fc);
    local_20.bottom = *(LONG *)((int)this + iVar2 + 0x200);
    OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    OffsetRect(&local_20,*(int *)((int)this + 0x10) - *(int *)((int)this + 0xd0),
               *(int *)((int)this + 0x14) - *(int *)((int)this + 0xd4));
    pnVar3 = GetMagicTbl2((int)*(short *)(iVar4 + 0x46 + *(int *)((int)this + 0xcc) * 2));
    *(newmagicrecord **)((int)this + 0xc4) = pnVar3;
    if (pnVar3 == (newmagicrecord *)0x0) {
      return;
    }
    if (*(int *)pnVar3 != (int)*(short *)(iVar4 + 0x46 + *(int *)((int)this + 0xcc) * 2)) {
      return;
    }
    SetRect(&local_10,*(int *)(pnVar3 + 0x138),*(int *)(pnVar3 + 0x13c),
            *(int *)(pnVar3 + 0x138) + 0x20,*(int *)(pnVar3 + 0x13c) + 0x20);
    CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar1,&local_10,1);
    if (*(short *)(iVar4 + 0x6e) == 8) goto LAB_004645cf;
    iVar4 = thunk_FUN_004642e0(this,(POINT *)((int)this + 0x10));
  }
  if (iVar4 != 0) {
    return;
  }
LAB_004645cf:
  CDDSurface::BltFast(param_1,unaff_EBP,unaff_EBX,*(undefined4 *)((int)this + 0xc0),1);
  return;
}

