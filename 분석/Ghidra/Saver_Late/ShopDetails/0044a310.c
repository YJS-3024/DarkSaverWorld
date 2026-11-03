
void __thiscall FUN_0044a310(void *this,CDDSurface *param_1)

{
  CDDSurface *pCVar1;
  itemrecord *piVar2;
  int iVar3;
  int yTop;
  LONG *pLVar4;
  int local_28;
  tagRECT local_20;
  tagRECT local_10;
  
  pCVar1 = GetItemSurface();
  if (pCVar1 != (CDDSurface *)0x0) {
    local_28 = 0;
    pLVar4 = (LONG *)((int)this + 0xc4);
    do {
      if ((*(int *)((int)this + 0x364) == 0) || (local_28 != *(int *)((int)this + 0x368))) {
        iVar3 = *(int *)((int)this + 0xb0);
        if (*(int *)(iVar3 + 0x44) == 0) {
          return;
        }
        if (*(int *)(iVar3 + 0x48) == 0) {
          return;
        }
        if (*(int *)(iVar3 + 0x40) <= local_28) {
          return;
        }
        iVar3 = *(int *)(*(int *)(iVar3 + 0x44) + local_28 * 4);
        if (iVar3 == -1) {
          return;
        }
        piVar2 = GetItemTable(iVar3);
        if (*(int *)piVar2 == iVar3) {
          yTop = (*(int *)(piVar2 + 4) / 100) * 0x20;
          iVar3 = (*(int *)(piVar2 + 4) % 100) * 0x20;
          SetRect(&local_10,iVar3,yTop,iVar3 + 0x20,yTop + 0x20);
          local_20.left = *pLVar4;
          local_20.top = pLVar4[1];
          local_20.right = pLVar4[2];
          local_20.bottom = pLVar4[3];
          OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
          CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar1,&local_10,1);
        }
      }
      local_28 = local_28 + 1;
      pLVar4 = pLVar4 + 4;
    } while (local_28 < 0x1c);
  }
  return;
}

