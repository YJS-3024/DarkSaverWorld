
void __thiscall FUN_0043fdf0(void *this,byte *param_1,byte *param_2,byte *param_3,int param_4)

{
  byte bVar1;
  int iVar2;
  uint uVar3;
  uint uVar4;
  uint uVar5;
  byte *pbVar6;
  byte *pbVar7;
  int iVar8;
  
  iVar8 = 0;
  pbVar6 = param_1;
  if (*param_1 == 0) {
    return;
  }
  do {
    bVar1 = *pbVar6;
    pbVar7 = pbVar6;
    if ((bVar1 == 10) || (bVar1 == 0xd)) {
      iVar8 = 0;
    }
    else {
      if (bVar1 < 0x80) {
        iVar2 = *(int *)((int)this + 4);
        if (bVar1 == 0x20) {
          iVar8 = iVar8 + ((int)(iVar2 * 3 + (iVar2 * 3 >> 0x1f & 7U)) >> 3);
          goto LAB_0043fe76;
        }
      }
      else if ((bVar1 == 0xa4) || ((0xaf < bVar1 && (bVar1 < 0xc9)))) {
        pbVar7 = pbVar6 + 1;
        iVar2 = *(int *)((int)this + 4);
        if (0x9f < pbVar6[1]) {
          iVar8 = iVar8 + iVar2;
          goto LAB_0043fe76;
        }
      }
      else {
        iVar2 = *(int *)((int)this + 4);
        pbVar7 = pbVar6 + 1;
      }
      iVar8 = iVar8 + ((int)(iVar2 * 4 + (iVar2 * 4 >> 0x1f & 7U)) >> 3);
    }
LAB_0043fe76:
    if (param_4 <= iVar8) {
      uVar3 = 0xffffffff;
      uVar5 = (int)pbVar6 - (int)param_1;
      pbVar7 = param_1;
      break;
    }
    pbVar6 = pbVar7 + 1;
    if (pbVar7[1] == 0) {
      return;
    }
  } while( true );
  while( true ) {
    uVar3 = uVar3 - 1;
    bVar1 = *pbVar7;
    pbVar7 = pbVar7 + 1;
    if (bVar1 == 0) break;
    if (uVar3 == 0) break;
  }
  if ((int)uVar5 < 1) {
    *param_2 = 0;
    *param_3 = 0;
    return;
  }
  pbVar7 = param_2;
  for (uVar4 = uVar5 >> 2; uVar4 != 0; uVar4 = uVar4 - 1) {
    *(undefined4 *)pbVar7 = *(undefined4 *)param_1;
    param_1 = param_1 + 4;
    pbVar7 = pbVar7 + 4;
  }
  uVar3 = (~uVar3 - 1) - uVar5;
  for (uVar4 = uVar5 & 3; uVar4 != 0; uVar4 = uVar4 - 1) {
    *pbVar7 = *param_1;
    param_1 = param_1 + 1;
    pbVar7 = pbVar7 + 1;
  }
  param_2[uVar5] = 0;
  if ((int)uVar3 < 1) {
    *param_3 = 0;
    return;
  }
  pbVar7 = param_3;
  for (uVar5 = uVar3 >> 2; uVar5 != 0; uVar5 = uVar5 - 1) {
    *(undefined4 *)pbVar7 = *(undefined4 *)pbVar6;
    pbVar6 = pbVar6 + 4;
    pbVar7 = pbVar7 + 4;
  }
  for (uVar5 = uVar3 & 3; uVar5 != 0; uVar5 = uVar5 - 1) {
    *pbVar7 = *pbVar6;
    pbVar6 = pbVar6 + 1;
    pbVar7 = pbVar7 + 1;
  }
  param_3[uVar3] = 0;
  return;
}

