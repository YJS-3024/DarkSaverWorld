
void __cdecl FUN_0043b390(undefined4 *param_1,uint *param_2,int param_3)

{
  int iVar1;
  undefined4 uVar2;
  uint uVar3;
  uint uVar4;
  undefined4 *puVar5;
  undefined4 *puVar6;
  
  if (param_3 != 0) {
    iVar1 = *(int *)(param_3 + 0x30);
    if (iVar1 < *(int *)(param_3 + 0x28)) {
      uVar4 = *param_2;
      if (*(int *)(param_3 + 0x28) - iVar1 < (int)uVar4) {
        *(undefined4 *)(param_3 + 0x38) = 1;
        return;
      }
      puVar5 = param_1;
      puVar6 = (undefined4 *)(*(int *)(param_3 + 0x20) + iVar1);
      for (uVar3 = uVar4 >> 2; uVar3 != 0; uVar3 = uVar3 - 1) {
        *puVar6 = *puVar5;
        puVar5 = puVar5 + 1;
        puVar6 = puVar6 + 1;
      }
      for (uVar4 = uVar4 & 3; uVar4 != 0; uVar4 = uVar4 - 1) {
        *(undefined1 *)puVar6 = *(undefined1 *)puVar5;
        puVar5 = (undefined4 *)((int)puVar5 + 1);
        puVar6 = (undefined4 *)((int)puVar6 + 1);
      }
      *(uint *)(param_3 + 0x30) = *(int *)(param_3 + 0x30) + *param_2;
      if (*(int *)(param_3 + 0x34) == 2) {
        uVar2 = FUN_00477e10(param_1,(int *)param_2);
        *(undefined4 *)(param_3 + 0x14) = uVar2;
        return;
      }
    }
    else {
      *(undefined4 *)(param_3 + 0x38) = 2;
    }
  }
  return;
}

