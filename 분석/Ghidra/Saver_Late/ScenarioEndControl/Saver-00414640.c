
undefined4 __fastcall FUN_00414640(void *param_1)

{
  int *this;
  AFX_MODULE_STATE *pAVar1;
  undefined2 extraout_var;
  int iVar2;
  int unaff_EDI;
  int local_10;
  char local_c [12];
  
  if (*(int *)((int)param_1 + 0x144) != 0) {
    *(undefined4 *)((int)param_1 + 0x144) = 0;
    if (*(int **)((int)param_1 + 0x148) != (int *)0x0) {
      (**(code **)(**(int **)((int)param_1 + 0x148) + 0x10))();
    }
    local_10 = 0;
    thunk_FUN_0043b9a0((int)local_c,&local_10,0x5b);
    thunk_FUN_004132d0(param_1,local_10,local_c);
    if (*(int *)((int)param_1 + 0x10c) != 0) {
      *(undefined4 *)((int)param_1 + 0x108) = 0;
      pAVar1 = AfxGetModuleState();
      *(undefined2 *)(*(int *)(pAVar1 + 4) + 0xd2) = 1000;
      thunk_FUN_00411850(param_1,0x14,0x14);
      *(undefined4 *)((int)param_1 + 0x10c) = 0;
      return 1;
    }
    thunk_FUN_00412460(param_1,0);
    if (*(int **)((int)param_1 + 0xf0) != (int *)0x0) {
      if (DAT_00495138 != -1) {
        (**(code **)(**(int **)((int)param_1 + 0xf0) + 0x98))
                  (CONCAT22(extraout_var,DAT_00495138),(int)DAT_00495136,(int)DAT_00495134);
        DAT_00495138 = -1;
      }
      iVar2 = (**(code **)(**(int **)((int)param_1 + 0xf0) + 4))();
      if (iVar2 == 1) {
        this = *(int **)((int)param_1 + 0xf0);
        (**(code **)(*this + 0xc))();
        pAVar1 = AfxGetModuleState();
        thunk_FUN_0042b940(this,(CString *)(int)*(short *)(*(int *)(pAVar1 + 4) + 0xd2),1,unaff_EDI)
        ;
        return 1;
      }
      (**(code **)(**(int **)((int)param_1 + 0xf0) + 0xc))();
    }
  }
  return 1;
}


/* incoming refs
00401451 UNCONDITIONAL_JUMP
*/
