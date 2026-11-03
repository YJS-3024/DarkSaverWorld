
void __thiscall FUN_0044ac40(void *this,CDDSurface *param_1)

{
  bool bVar1;
  CDDSurface *pCVar2;
  itemrecord *piVar3;
  int iVar4;
  BOOL BVar5;
  int iVar6;
  int iVar7;
  int yTop;
  int iVar8;
  tagRECT local_30;
  tagRECT local_20;
  tagRECT local_10;
  
  SetRect(&local_20,*(int *)((int)this + 0x284),*(int *)((int)this + 0x288),
          *(int *)((int)this + 0x33c),*(int *)((int)this + 0x340));
  SetRect(&local_10,*(int *)((int)this + 0xc4),*(int *)((int)this + 200),*(int *)((int)this + 0x27c)
          ,*(int *)((int)this + 0x280));
  pCVar2 = GetItemSurface();
  if (pCVar2 != (CDDSurface *)0x0) {
    if (*(int *)((int)this + 0x364) == 0) {
      if (*(int *)((int)this + 0x35c) != 0) {
        iVar6 = *(int *)(*(int *)((int)this + 0xbc) + *(int *)((int)this + 0x360) * 4);
        piVar3 = GetItemTable(iVar6);
        iVar4 = *(int *)((int)this + 0x10) + *(int *)((int)this + 0x40) + -0x10;
        iVar8 = *(int *)((int)this + 0x14) + *(int *)((int)this + 0x44) + -0x10;
        if (*(int *)piVar3 == iVar6) {
          iVar7 = (*(int *)(piVar3 + 4) / 100) * 0x20;
          iVar6 = (*(int *)(piVar3 + 4) % 100) * 0x20;
          SetRect(&local_30,iVar6,iVar7,iVar6 + 0x20,iVar7 + 0x20);
          CDDSurface::BltFast(param_1,iVar4,iVar8,pCVar2,&local_30,1);
        }
        BVar5 = PtInRect(&local_10,*(POINT *)((int)this + 0x10));
        if (BVar5 == 0) {
          CDDSurface::BltFast(param_1,iVar4,iVar8,*(undefined4 *)((int)this + 0x358),1);
        }
      }
    }
    else {
      iVar4 = *(int *)((int)this + 0xb0);
      if (((*(int *)(iVar4 + 0x44) == 0) || (*(int *)(iVar4 + 0x48) == 0)) ||
         (*(int *)(iVar4 + 0x40) <= *(int *)((int)this + 0x368))) {
        iVar4 = -1;
      }
      else {
        iVar4 = *(int *)(*(int *)(iVar4 + 0x44) + *(int *)((int)this + 0x368) * 4);
      }
      piVar3 = GetItemTable(iVar4);
      iVar6 = *(int *)((int)this + 0x10) + *(int *)((int)this + 0x40) + -0x10;
      iVar8 = *(int *)((int)this + 0x14) + *(int *)((int)this + 0x44) + -0x10;
      bVar1 = false;
      if (*(int *)piVar3 == iVar4) {
        yTop = (*(int *)(piVar3 + 4) / 100) * 0x20;
        iVar7 = (*(int *)(piVar3 + 4) % 100) * 0x20;
        SetRect(&local_30,iVar7,yTop,iVar7 + 0x20,yTop + 0x20);
        CDDSurface::BltFast(param_1,iVar6,iVar8,pCVar2,&local_30,1);
        iVar4 = (**(code **)(*(int *)this + 0x60))(iVar4);
        if (iVar4 == 0) {
          bVar1 = true;
        }
      }
      BVar5 = PtInRect(&local_20,*(POINT *)((int)this + 0x10));
      if ((BVar5 == 0) || (bVar1)) {
        CDDSurface::BltFast(param_1,iVar6,iVar8,*(undefined4 *)((int)this + 0x358),1);
        return;
      }
    }
  }
  return;
}

