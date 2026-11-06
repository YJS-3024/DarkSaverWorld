
void __cdecl FUN_10008e10(uint param_1,uint param_2,uint param_3,uint param_4)

{
  int iVar1;
  int iVar2;
  uint uVar3;
  int iVar4;
  uint uVar5;
  int iVar6;
  int iVar7;
  int iVar8;
  int local_18;
  tagRECT local_10;
  
  uVar3 = param_1 & 0x80000001;
  if ((int)uVar3 < 0) {
    uVar3 = (uVar3 - 1 | 0xfffffffe) + 1;
  }
  iVar7 = param_1 - uVar3;
  uVar3 = param_2 & 0x80000001;
  if ((int)uVar3 < 0) {
    uVar3 = (uVar3 - 1 | 0xfffffffe) + 1;
  }
  iVar4 = param_2 - uVar3;
  uVar3 = param_3 & 0x80000001;
  if ((int)uVar3 < 0) {
    uVar3 = (uVar3 - 1 | 0xfffffffe) + 1;
  }
  uVar5 = param_4 & 0x80000001;
  if ((int)uVar5 < 0) {
    uVar5 = (uVar5 - 1 | 0xfffffffe) + 1;
  }
  iVar8 = param_4 + uVar5;
  SetRect(&local_10,0x14,0x140,0x28,0x154);
  iVar6 = param_3 + uVar3 + -0x14;
  iVar2 = iVar7;
  if (iVar7 < iVar6) {
    iVar1 = iVar4;
    local_18 = iVar7;
    do {
      for (; iVar1 < iVar8 + -0x14; iVar1 = iVar1 + 0x14) {
        CDDSurface::BltFast(DAT_1001c470,local_18,iVar1,DAT_1006a704,&local_10,1);
      }
      local_18 = local_18 + 0x14;
      iVar1 = iVar4;
    } while (local_18 < iVar6);
  }
  while (iVar2 = iVar2 + 0x14, iVar2 < iVar6) {
    SetRect(&local_10,0x14,300,0x28,0x140);
    CDDSurface::BltFast(DAT_1001c470,iVar2,iVar4,DAT_1006a704,&local_10,1);
    SetRect(&local_10,0x14,0x154,0x28,0x168);
    CDDSurface::BltFast(DAT_1001c470,iVar2,iVar8 + -0x14,DAT_1006a704,&local_10,1);
  }
  iVar8 = iVar8 + -0x14;
  iVar2 = iVar4;
  while (iVar2 = iVar2 + 0x14, iVar2 < iVar8) {
    SetRect(&local_10,0,0x140,0x14,0x154);
    CDDSurface::BltFast(DAT_1001c470,iVar7,iVar2,DAT_1006a704,&local_10,1);
    SetRect(&local_10,0x28,0x140,0x3c,0x154);
    CDDSurface::BltFast(DAT_1001c470,iVar6,iVar2,DAT_1006a704,&local_10,1);
  }
  SetRect(&local_10,0,300,0x14,0x140);
  CDDSurface::BltFast(DAT_1001c470,iVar7,iVar4,DAT_1006a704,&local_10,1);
  SetRect(&local_10,0,0x154,0x14,0x168);
  CDDSurface::BltFast(DAT_1001c470,iVar7,iVar8,DAT_1006a704,&local_10,1);
  SetRect(&local_10,0x28,300,0x3c,0x140);
  CDDSurface::BltFast(DAT_1001c470,iVar6,iVar4,DAT_1006a704,&local_10,1);
  SetRect(&local_10,0x28,0x154,0x3c,0x168);
  CDDSurface::BltFast(DAT_1001c470,iVar6,iVar8,DAT_1006a704,&local_10,1);
  return;
}


/* incoming refs
1000a7a7 UNCONDITIONAL_CALL
10008b29 UNCONDITIONAL_CALL
10008c11 UNCONDITIONAL_CALL
CALLER 10008ad0 FUN_10008ad0
  10003bba UNCONDITIONAL_CALL
CALLER 1000a740 FUN_1000a740
  1000abf5 UNCONDITIONAL_CALL
  1000aec8 UNCONDITIONAL_CALL
  1000b051 UNCONDITIONAL_CALL
  1000b1c1 UNCONDITIONAL_CALL
*/
