
void __thiscall FUN_0046f620(void *this,CDDSurface *param_1)

{
  int iVar1;
  CDDSurface *pCVar2;
  LONG *pLVar3;
  int iVar4;
  tagRECT local_20;
  undefined4 local_10;
  undefined4 local_c;
  undefined4 local_8;
  undefined4 local_4;
  
  local_10 = 0x20;
  local_c = 0x20;
  local_8 = 0x40;
  local_4 = 0x40;
  iVar4 = 0;
  pLVar3 = (LONG *)((int)this + 0xe4);
  do {
    if (*(int *)((int)this + 0x174) <= *(int *)((int)this + 0x180) + iVar4) break;
    local_20.left = *pLVar3;
    local_20.top = pLVar3[1];
    local_20.right = pLVar3[2];
    local_20.bottom = pLVar3[3];
    OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    iVar1 = thunk_FUN_00477a90(*(int *)(*(int *)((int)this + 0x178) +
                                       (*(int *)((int)this + 0x180) + iVar4) * 4));
    pCVar2 = GetCharSurface(iVar1,-1);
    if (pCVar2 != (CDDSurface *)0x0) {
      CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar2,&local_10,1);
    }
    if (*(int *)((int)this + 0x174) <= *(int *)((int)this + 0x180) + iVar4) break;
    iVar4 = iVar4 + 1;
    pLVar3 = pLVar3 + 4;
  } while (iVar4 < 9);
  iVar4 = *(int *)((int)this + 0x184);
  if (*(short *)(iVar4 + 0x9a) != -1) {
    local_20.left = *(LONG *)((int)this + 0xb4);
    local_20.top = *(LONG *)((int)this + 0xb8);
    local_20.right = *(LONG *)((int)this + 0xbc);
    local_20.bottom = *(LONG *)((int)this + 0xc0);
    OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    iVar4 = thunk_FUN_00477a90((int)*(short *)(iVar4 + 0x9a));
    pCVar2 = GetCharSurface(iVar4,-1);
    if (pCVar2 != (CDDSurface *)0x0) {
      CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar2,&local_10,1);
    }
  }
  return;
}

