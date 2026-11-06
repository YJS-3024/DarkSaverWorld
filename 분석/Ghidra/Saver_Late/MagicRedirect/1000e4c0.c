
void __cdecl FUN_1000e4c0(short *param_1,int param_2)

{
  uint uVar1;
  void *pvVar2;
  magicrecord *pmVar3;
  int iVar4;
  undefined1 uVar5;
  int local_4;
  
  if ((DAT_1001a8ec == 0x400) || (DAT_1001a8ec == 0x200)) {
    iVar4 = 0;
    local_4 = 0;
    uVar1 = (uint)*(byte *)(param_1 + 6) * 2 + 0x10;
    pvVar2 = operator_new(uVar1);
    pmVar3 = GetMagicTbl((int)*param_1);
    *(int *)(param_1 + 4) =
         (*(int *)(pmVar3 + 0x170) + *(int *)(pmVar3 + 0x14) + *(int *)(pmVar3 + 0x10) +
          *(int *)(pmVar3 + 0xc) + *(int *)(pmVar3 + 8)) * *(int *)pmVar3;
    FUN_1000c600((int)pvVar2,&local_4,0x5c);
    if (*param_1 < 10000) {
      uVar5 = 0x89;
    }
    else {
      uVar5 = 0x92;
    }
    FUN_1000c600((int)pvVar2,&local_4,uVar5);
    FUN_1000c600((int)pvVar2,&local_4,(param_2 != 0) + '\x01');
    FUN_1000c5e0((int)pvVar2,&local_4,*param_1);
    FUN_1000c5e0((int)pvVar2,&local_4,param_1[1]);
    FUN_1000c5e0((int)pvVar2,&local_4,param_1[2]);
    FUN_1000c5e0((int)pvVar2,&local_4,param_1[3]);
    FUN_1000c660((int)pvVar2,&local_4,*(undefined4 *)(param_1 + 4));
    FUN_1000c600((int)pvVar2,&local_4,(char)param_1[6]);
    if ((char)param_1[6] != '\0') {
      do {
        FUN_1000c5e0((int)pvVar2,&local_4,*(undefined2 *)(*(int *)(param_1 + 8) + iVar4 * 2));
        iVar4 = iVar4 + 1;
      } while (iVar4 < (int)(uint)*(byte *)(param_1 + 6));
    }
    FUN_1000e2c0((LPARAM)pvVar2,uVar1);
    operator_delete(pvVar2);
  }
  return;
}


/* incoming refs
10013869 UNCONDITIONAL_CALL
1000df79 UNCONDITIONAL_CALL
1000e018 UNCONDITIONAL_CALL
1000a3fc UNCONDITIONAL_CALL
1000a5e1 UNCONDITIONAL_CALL
CALLER 10010f20 FUN_10010f20
  10003883 UNCONDITIONAL_CALL
  10003896 UNCONDITIONAL_CALL
  1000dd87 UNCONDITIONAL_CALL
  10003bcc UNCONDITIONAL_CALL
  10003bdb UNCONDITIONAL_CALL
CALLER 10009890 FUN_10009890
  10002e84 UNCONDITIONAL_CALL
  10002ea4 UNCONDITIONAL_CALL
CALLER 1000df10 FUN_1000df10
  1000ecbd UNCONDITIONAL_CALL
  1000ecca UNCONDITIONAL_CALL
CALLER 1000dfb0 FUN_1000dfb0
  1000ece4 UNCONDITIONAL_CALL
*/
