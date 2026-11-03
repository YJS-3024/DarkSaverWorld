
void __fastcall FUN_004614f0(undefined4 *param_1)

{
  int iVar1;
  CGdiObject *pCVar2;
  void *pvStack_c;
  undefined1 *puStack_8;
  int local_4;
  
  puStack_8 = &LAB_0047ffce;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  *param_1 = &PTR_LAB_00485d24;
  local_4 = 10;
  if ((undefined4 *)param_1[0x50] != (undefined4 *)0x0) {
    (*(code *)**(undefined4 **)param_1[0x50])(1);
    param_1[0x50] = 0;
  }
  if ((int *)param_1[0x2d] != (int *)0x0) {
    (**(code **)(*(int *)param_1[0x2d] + 4))(1);
    param_1[0x2d] = 0;
  }
  if ((int *)param_1[0x30] != (int *)0x0) {
    (**(code **)(*(int *)param_1[0x30] + 4))(1);
    param_1[0x30] = 0;
  }
  iVar1 = 6;
  pCVar2 = (CGdiObject *)(param_1 + 0x58);
  do {
    CGdiObject::DeleteObject(pCVar2);
    pCVar2 = pCVar2 + 8;
    iVar1 = iVar1 + -1;
  } while (iVar1 != 0);
  pCVar2 = (CGdiObject *)(param_1 + 100);
  *(undefined ***)pCVar2 = &PTR_LAB_00484214;
  local_4._0_1_ = 0xb;
  CGdiObject::DeleteObject(pCVar2);
  *(undefined ***)pCVar2 = &PTR_LAB_004830f0;
  local_4._0_1_ = 8;
  FUN_00479c7e((CGdiObject *)(param_1 + 0x58),8,6,thunk_FUN_0044d530);
  pCVar2 = (CGdiObject *)(param_1 + 0x56);
  *(undefined ***)pCVar2 = &PTR_LAB_00484214;
  local_4._0_1_ = 0xc;
  CGdiObject::DeleteObject(pCVar2);
  *(undefined ***)pCVar2 = &PTR_LAB_004830f0;
  pCVar2 = (CGdiObject *)(param_1 + 0x54);
  *(undefined ***)pCVar2 = &PTR_LAB_00484214;
  local_4._0_1_ = 0xd;
  CGdiObject::DeleteObject(pCVar2);
  *(undefined ***)pCVar2 = &PTR_LAB_004830f0;
  pCVar2 = (CGdiObject *)(param_1 + 0x52);
  *(undefined ***)pCVar2 = &PTR_LAB_00484214;
  local_4._0_1_ = 0xe;
  CGdiObject::DeleteObject(pCVar2);
  *(undefined ***)pCVar2 = &PTR_LAB_004830f0;
  local_4._0_1_ = 4;
  CString::~CString((CString *)(param_1 + 0x4e));
  local_4._0_1_ = 3;
  CString::~CString((CString *)(param_1 + 0x4d));
  local_4._0_1_ = 2;
  CString::~CString((CString *)(param_1 + 0x4c));
  local_4._0_1_ = 1;
  CString::~CString((CString *)(param_1 + 0x4b));
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString((CString *)(param_1 + 0x4a));
  local_4 = 0xffffffff;
  thunk_FUN_00477a70(param_1);
  ExceptionList = pvStack_c;
  return;
}

