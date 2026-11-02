
void __fastcall FUN_00477520(int param_1)

{
  int iVar1;
  AFX_MODULE_STATE *pAVar2;
  void *this;
  int *piVar3;
  int iVar4;
  undefined4 *puVar5;
  CString local_18 [4];
  void *local_14;
  undefined4 *local_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  uint local_4;
  
  puStack_8 = &LAB_00481225;
  pvStack_c = ExceptionList;
  local_4 = 1;
  ExceptionList = &pvStack_c;
  pAVar2 = AfxGetModuleState();
  if (*(short *)(*(int *)(pAVar2 + 4) + 0xd2) != 0x9c4) {
    CString::CString(local_18);
    local_4._0_1_ = 2;
    CString::operator=(local_18,s_scene47_lsc_00491298);
    this = operator_new(0xe4);
    local_4._0_1_ = 3;
    local_14 = this;
    if (this == (void *)0x0) {
      piVar3 = (int *)0x0;
    }
    else {
      local_10 = (undefined4 *)&stack0xffffffd8;
      CString::CString((CString *)&stack0xffffffd8,(CString *)&stack0x00000004);
      piVar3 = thunk_FUN_00452d60(this,*(int *)(param_1 + 0x44),local_18,param_1,
                                  *(undefined4 *)(param_1 + 0x4c),*(undefined4 *)(param_1 + 0x50));
    }
    local_4._0_1_ = 2;
    if (piVar3 != (int *)0x0) {
      iVar4 = thunk_FUN_00434ea0(piVar3);
      if (iVar4 != 0) {
        if (((*(int *)(param_1 + 0x40) != -1) &&
            (*(int *)(*(int *)(*(int *)(param_1 + 0x18) + *(int *)(param_1 + 0x40) * 4) + 0x90) != 0
            )) && (piVar3[0x24] == 0)) {
          (**(code **)*piVar3)();
        }
        puVar5 = operator_new(0xc);
        local_10 = puVar5;
        if (puVar5 == (undefined4 *)0x0) {
          puVar5 = (undefined4 *)0x0;
        }
        else {
          *puVar5 = &PTR_LAB_004830f0;
          local_4._0_1_ = 5;
          CString::CString((CString *)(puVar5 + 1));
          *puVar5 = &PTR_LAB_00486664;
        }
        local_4._0_1_ = 2;
        CString::operator=((CString *)(puVar5 + 1),(CString *)&stack0x00000004);
        puVar5[2] = piVar3;
        CPtrArray::SetAtGrow((CPtrArray *)(param_1 + 0x28),*(int *)(param_1 + 0x30),puVar5);
        CPtrArray::SetAtGrow((CPtrArray *)(param_1 + 0x14),*(int *)(param_1 + 0x1c),piVar3);
        *(int *)(param_1 + 0x3c) = *(int *)(param_1 + 0x1c);
        *(int *)(param_1 + 0x40) = *(int *)(param_1 + 0x1c) + -1;
        iVar4 = *(int *)(param_1 + 0x30);
        iVar1 = puVar5[2];
        *(undefined4 *)(iVar1 + 0x40) = 0x1ba;
        *(int *)(iVar1 + 0x44) = (iVar4 * 3 + -3) * 8;
        thunk_FUN_00453760((void *)puVar5[2],(CString *)&stack0x00000008);
      }
    }
    local_4 = CONCAT31(local_4._1_3_,1);
    CString::~CString(local_18);
  }
  local_4 = local_4 & 0xffffff00;
  CString::~CString((CString *)&stack0x00000004);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&stack0x00000008);
  ExceptionList = pvStack_c;
  return;
}

