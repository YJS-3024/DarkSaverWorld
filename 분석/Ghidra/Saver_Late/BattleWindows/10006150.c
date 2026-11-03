
void __cdecl FUN_10006150(int param_1,int param_2)

{
  character *pcVar1;
  int iVar2;
  int iVar3;
  int iVar4;
  int yBottom;
  int iVar5;
  int iVar6;
  int local_1c;
  int local_18;
  int local_14;
  tagRECT local_10;
  
  DAT_1001c45c = *(int *)(DAT_1001c490 + 0x88);
  DAT_1001c458 = *(int *)(DAT_1001c490 + 0x8c);
  iVar5 = 0;
  local_1c = 0;
  do {
    local_14 = 10;
    iVar6 = iVar5;
    do {
      local_18 = 10;
      do {
        pcVar1 = GetUnitInfoP(iVar6);
        if ((*(int *)pcVar1 != 0) && (*(int *)(pcVar1 + 4) != 0)) {
          iVar2 = (*(int *)(pcVar1 + 0x70) * 100) / DAT_1001c45c;
          iVar3 = (*(int *)(pcVar1 + 0x74) * 100) / DAT_1001c458;
          iVar4 = local_1c;
          if (iVar5 < 400) {
            iVar4 = getsnum(iVar6);
          }
          SetRect(&local_10,iVar4 * 3 + 0xc0,0,iVar4 * 3 + 0xc3,3);
          CDDSurface::BltFast(DAT_1001c470,iVar2 + 0xb + param_1,iVar3 + 0x19 + param_2,DAT_1001c480
                              ,&local_10,1);
        }
        iVar6 = iVar6 + 1;
        local_18 = local_18 + -1;
      } while (local_18 != 0);
      local_14 = local_14 + -1;
    } while (local_14 != 0);
    iVar5 = iVar5 + 100;
    local_1c = local_1c + 1;
  } while (iVar5 < 0x385);
  iVar6 = (DAT_1001c460 * 100) / DAT_1001c45c + 0xb + param_1;
  iVar4 = ((DAT_1001a0b8 + -2 + DAT_1001c460) * 100) / DAT_1001c45c + 0xb + param_1;
  iVar2 = (DAT_1001c464 * 100) / DAT_1001c458 + 0x19 + param_2;
  iVar3 = ((DAT_1001a0bc + -2 + DAT_1001c464) * 100) / DAT_1001c458 + 0x19 + param_2;
  iVar5 = (iVar4 - iVar6) + 100;
  SetRect(&local_10,100,400,iVar5,0x191);
  CDDSurface::BltFast(DAT_1001c470,iVar6,iVar2,DAT_1006a704,&local_10,0);
  yBottom = (iVar3 - iVar2) + 400;
  SetRect(&local_10,100,400,0x65,yBottom);
  CDDSurface::BltFast(DAT_1001c470,iVar6,iVar2,DAT_1006a704,&local_10,0);
  SetRect(&local_10,100,400,iVar5,0x191);
  CDDSurface::BltFast(DAT_1001c470,iVar6,iVar3,DAT_1006a704,&local_10,0);
  SetRect(&local_10,100,400,0x65,yBottom);
  CDDSurface::BltFast(DAT_1001c470,iVar4,iVar2,DAT_1006a704,&local_10,0);
  return;
}

