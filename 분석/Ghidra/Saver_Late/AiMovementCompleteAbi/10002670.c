
void __cdecl FUN_10002670(CString *param_1,int param_2)

{
  byte bVar1;
  byte *pbVar2;
  int iVar3;
  byte *pbVar4;
  char *pcVar5;
  bool bVar6;
  CString *pCVar7;
  CString *pCVar8;
  byte *local_34;
  char *local_30;
  char *local_2c;
  char *local_28;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10011c93;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  FUN_10011a3e(&local_34,4,10,CString::CString);
  pCVar7 = (CString *)&local_34;
  local_4 = 0;
  pCVar8 = pCVar7;
  CString::CString((CString *)&stack0xffffffb0,param_1);
  FUN_10004690((undefined1 **)pCVar7,pCVar8);
  pbVar4 = &DAT_1001602c;
  pbVar2 = local_34;
  do {
    bVar1 = *pbVar2;
    bVar6 = bVar1 < *pbVar4;
    if (bVar1 != *pbVar4) {
LAB_100026fd:
      iVar3 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
      goto LAB_10002701;
    }
    if (bVar1 == 0) break;
    bVar1 = pbVar2[1];
    bVar6 = bVar1 < pbVar4[1];
    if (bVar1 != pbVar4[1]) goto LAB_100026fd;
    pbVar2 = pbVar2 + 2;
    pbVar4 = pbVar4 + 2;
  } while (bVar1 != 0);
  iVar3 = 0;
LAB_10002701:
  if (iVar3 == 0) {
    *(undefined4 *)(param_2 + 8) = 0;
    iVar3 = FUN_1000f4e0(local_30,-1);
    *(int *)(param_2 + 0xc) = iVar3;
    iVar3 = FUN_1000f4e0(local_2c,-1);
    *(int *)(param_2 + 0x10) = iVar3;
    iVar3 = FUN_1000f4e0(local_28,-1);
    *(int *)(param_2 + 0x14) = iVar3;
  }
  else {
    pcVar5 = s_SOILDER_10016024;
    do {
      bVar1 = *local_34;
      bVar6 = bVar1 < (byte)*pcVar5;
      if (bVar1 != *pcVar5) {
LAB_10002766:
        iVar3 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
        goto LAB_1000276a;
      }
      if (bVar1 == 0) break;
      bVar1 = local_34[1];
      bVar6 = bVar1 < (byte)pcVar5[1];
      if (bVar1 != pcVar5[1]) goto LAB_10002766;
      local_34 = local_34 + 2;
      pcVar5 = pcVar5 + 2;
    } while (bVar1 != 0);
    iVar3 = 0;
LAB_1000276a:
    if (iVar3 == 0) {
      *(undefined4 *)(param_2 + 8) = 1;
      iVar3 = FUN_1000f4e0(local_30,700);
      *(int *)(param_2 + 0x18) = iVar3;
    }
    else {
      *(undefined4 *)(param_2 + 8) = 0xffffffff;
      *(undefined4 *)(param_2 + 0xc) = 0xffffffff;
      *(undefined4 *)(param_2 + 0x10) = 0xffffffff;
      *(undefined4 *)(param_2 + 0x14) = 0xffffffff;
      *(undefined4 *)(param_2 + 0x18) = 700;
    }
  }
  local_4 = 0xffffffff;
  FUN_1001194a(&local_34,4,10,CString::~CString);
  ExceptionList = local_c;
  return;
}


/* incoming refs
10002dee UNCONDITIONAL_CALL
CALLER 10002b40 LoadScenarioAiRules
  10014174 DATA
  Entry Point EXTERNAL
  1000135f UNCONDITIONAL_CALL
*/
