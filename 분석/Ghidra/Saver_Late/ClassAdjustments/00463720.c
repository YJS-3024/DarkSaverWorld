
void __thiscall FUN_00463720(void *this,CDDSurface *param_1)

{
  int iVar1;
  CDDSurface *pCVar2;
  newmagicrecord *pnVar3;
  LONG *pLVar4;
  int iVar5;
  short *psVar6;
  tagRECT local_20;
  tagRECT local_10;
  
  iVar1 = *(int *)((int)this + 0x13c);
  pCVar2 = GetIconSurface();
  if (pCVar2 != (CDDSurface *)0x0) {
    iVar5 = 0;
    psVar6 = (short *)(iVar1 + 0x70);
    pLVar4 = (LONG *)((int)this + 0x2f4);
    do {
      if (*(short *)(iVar1 + 0x6e) <= iVar5) {
        return;
      }
      if (((*(int *)((int)this + 0xc4) == 0) || (*(int *)((int)this + 0xd8) == 0)) ||
         (*(int *)((int)this + 0xdc) != iVar5)) {
        local_20.left = *pLVar4;
        local_20.top = pLVar4[1];
        local_20.right = pLVar4[2];
        local_20.bottom = pLVar4[3];
        OffsetRect(&local_20,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
        pnVar3 = GetMagicTbl2((int)*psVar6);
        if ((pnVar3 != (newmagicrecord *)0x0) && (*(int *)pnVar3 == (int)*psVar6)) {
          SetRect(&local_10,*(int *)(pnVar3 + 0x138),*(int *)(pnVar3 + 0x13c),
                  *(int *)(pnVar3 + 0x138) + 0x20,*(int *)(pnVar3 + 0x13c) + 0x20);
          CDDSurface::BltFast(param_1,local_20.left,local_20.top,pCVar2,&local_10,1);
        }
      }
      iVar5 = iVar5 + 1;
      psVar6 = psVar6 + 1;
      pLVar4 = pLVar4 + 4;
    } while (iVar5 < 8);
  }
  return;
}

