
void __thiscall FUN_0044a490(void *this,CDDSurface *param_1)

{
  CDDSurface *pCVar1;
  int iVar2;
  itemrecord *piVar3;
  int yTop;
  int iVar4;
  LONG *pLVar5;
  tagRECT local_20;
  tagRECT local_10;
  
  pCVar1 = GetItemSurface();
  if (pCVar1 != (CDDSurface *)0x0) {
    iVar4 = 0;
    pLVar5 = (LONG *)((int)this + 0x284);
    do {
      if (*(int *)((int)this + 0xb8) <= iVar4) {
        return;
      }
      iVar2 = thunk_FUN_0044a6b0(this,*(int *)((int)this + 0xc0) + iVar4);
      piVar3 = GetItemTable(iVar2);
      if (*(int *)piVar3 != iVar2) {
        return;
      }
      yTop = (*(int *)(piVar3 + 4) / 100) * 0x20;
      iVar2 = (*(int *)(piVar3 + 4) % 100) * 0x20;
      SetRect(&local_10,iVar2,yTop,iVar2 + 0x20,yTop + 0x20);
      local_20.left = *pLVar5;
      local_20.top = pLVar5[1];
      local_20.right = pLVar5[2];
      local_20.bottom = pLVar5[3];
      OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
      CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar1,&local_10,1);
      iVar4 = iVar4 + 1;
      pLVar5 = pLVar5 + 4;
    } while (iVar4 < 0xc);
  }
  return;
}

