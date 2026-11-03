
uint FUN_0043b2f0(undefined4 *param_1,uint *param_2,uint param_3)

{
  int iVar1;
  undefined4 uVar2;
  uint uVar3;
  uint uVar4;
  undefined4 *puVar5;
  undefined4 *puVar6;
  
  iVar1 = param_3;
  if (param_3 == 0) {
    return 0;
  }
  if (*(int *)(param_3 + 0x2c) < *(int *)(param_3 + 0x24)) {
    uVar3 = *(int *)(param_3 + 0x24) - *(int *)(param_3 + 0x2c);
    if (uVar3 < *param_2) {
      *param_2 = uVar3;
    }
    uVar3 = *param_2;
    puVar5 = (undefined4 *)(*(int *)(param_3 + 0x1c) + *(int *)(param_3 + 0x2c));
    puVar6 = param_1;
    for (uVar4 = uVar3 >> 2; uVar4 != 0; uVar4 = uVar4 - 1) {
      *puVar6 = *puVar5;
      puVar5 = puVar5 + 1;
      puVar6 = puVar6 + 1;
    }
    for (uVar3 = uVar3 & 3; uVar3 != 0; uVar3 = uVar3 - 1) {
      *(undefined1 *)puVar6 = *(undefined1 *)puVar5;
      puVar5 = (undefined4 *)((int)puVar5 + 1);
      puVar6 = (undefined4 *)((int)puVar6 + 1);
    }
    *(uint *)(param_3 + 0x2c) = *(int *)(param_3 + 0x2c) + *param_2;
    param_3 = *param_2;
  }
  else {
    param_3 = 0;
  }
  if (*(int *)(iVar1 + 0x34) == 1) {
    uVar2 = FUN_00477e10(param_1,(int *)&param_3);
    *(undefined4 *)(iVar1 + 0x14) = uVar2;
  }
  return param_3;
}

