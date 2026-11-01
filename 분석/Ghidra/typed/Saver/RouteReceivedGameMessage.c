/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x4419c0; source SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689
 */

/* Native SEH prologue verified in original bytes. Handle selected outer message cases and forward
   the game payload into GameCtl.OnPacketReceive at0x441b0a. REA automatic containing-function
   lookup did not identify this body; boundary is recovered in the separate Ghidra project, not
   claimed as an MCP annotation.
   Source SHA256: dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689 */

undefined4 __thiscall RouteReceivedGameMessage(int param_1,char *param_2,int param_3)

{
  char cVar1;
  short sVar2;
  int *piVar3;
  void *pvVar4;
  undefined4 *puVar5;
  short *psVar6;
  char *pcVar7;
  undefined4 uVar8;
  int iVar9;
  undefined4 *puVar10;
  int iStack_21c;
  CString aCStack_218 [4];
  undefined4 uStack_214;
  undefined1 auStack_110 [4];
  char acStack_10c [256];
  void *pvStack_c;
  undefined *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &DAT_0047df4b;
  pvStack_c = ExceptionList;
  cVar1 = *param_2;
  if (((cVar1 == ',') && (param_2[1] == '$')) && (*(short *)(param_2 + 2) == 0x2d)) {
    ExceptionList = &pvStack_c;
    thunk_FUN_004424d0(*(void **)(param_1 + 0x114),1000);
    pvVar4 = (void *)thunk_FUN_00442a00(*(void **)(param_1 + 0x114),1000);
    if (pvVar4 != (void *)0x0) {
      thunk_FUN_00462b90(pvVar4,(int)*(short *)(param_2 + 4));
    }
  }
  else {
    if ((cVar1 == -0x79) || (cVar1 == -0x75)) {
      ExceptionList = &pvStack_c;
      pvVar4 = (void *)thunk_FUN_00442a00(*(void **)(param_1 + 0x114),1000);
      if (pvVar4 != (void *)0x0) {
        uVar8 = thunk_FUN_00462440(pvVar4,param_2,param_3);
        ExceptionList = pvStack_c;
        return uVar8;
      }
    }
    else if (cVar1 == 'z') {
      ExceptionList = &pvStack_c;
      puVar5 = (undefined4 *)thunk_FUN_0043c5d0(&uStack_214,(int)(param_2 + 1));
      puVar10 = &uStack_214;
      for (iVar9 = 0x41; iVar9 != 0; iVar9 = iVar9 + -1) {
        *puVar10 = *puVar5;
        puVar5 = puVar5 + 1;
        puVar10 = puVar10 + 1;
      }
      puVar5 = &uStack_214;
      puVar10 = (undefined4 *)auStack_110;
      for (iVar9 = 0x41; iVar9 != 0; iVar9 = iVar9 + -1) {
        *puVar10 = *puVar5;
        puVar5 = puVar5 + 1;
        puVar10 = puVar10 + 1;
      }
      if ((uStack_214._2_1_ == '\x03') && (DAT_00490ae0 == 0)) {
        ExceptionList = pvStack_c;
        return 1;
      }
      piVar3 = *(int **)(DAT_004952a0 + 0xf0);
      if (((piVar3 != (int *)0x0) && (iVar9 = (**(code **)(*piVar3 + 4))(), iVar9 == 1)) &&
         (pvVar4 = (void *)piVar3[0x54], pvVar4 != (void *)0x0)) {
        CString::CString(aCStack_218,acStack_10c);
        uStack_4 = 0;
        thunk_FUN_00476230(pvVar4,stack0xfffffef2 & 0xff,aCStack_218);
        uStack_4 = 0xffffffff;
        CString::~CString(aCStack_218);
      }
    }
    else {
      ExceptionList = &pvStack_c;
      if (cVar1 == 'p') {
        ExceptionList = &pvStack_c;
        thunk_FUN_004424d0(*(void **)(param_1 + 0x114),0x3e9);
        pvVar4 = (void *)thunk_FUN_00442a00(*(void **)(param_1 + 0x114),0x3e9);
        if (pvVar4 != (void *)0x0) {
          iStack_21c = 1;
          psVar6 = (short *)thunk_FUN_0043c3f0((int)param_2,&iStack_21c);
          sVar2 = *psVar6;
          pcVar7 = operator_new((int)sVar2 + 5);
          thunk_FUN_0043c450((int)param_2,&iStack_21c,(undefined4 *)pcVar7,(int)sVar2);
          thunk_FUN_00468680(pvVar4,pcVar7);
          operator_delete(pcVar7);
        }
        ExceptionList = pvStack_c;
        return 1;
      }
    }
    OnPacketReceive(param_2,param_3);
  }
  ExceptionList = pvStack_c;
  return 1;
}

