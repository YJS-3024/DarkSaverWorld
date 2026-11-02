
void __fastcall FUN_004537a0(int param_1)

{
  char cVar1;
  char *pcVar2;
  uint uVar3;
  int local_78;
  int local_74;
  char local_70 [100];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047eec0;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString((CString *)&local_74);
  local_4 = 0;
  CString::CString((CString *)&local_78);
  local_4._0_1_ = 1;
  if (DAT_00490ae4 == 0) {
    thunk_FUN_00453930();
  }
  pcVar2 = (char *)thunk_FUN_00425680(*(void **)(param_1 + 0xd0),2);
  CString::operator=((CString *)&local_78,pcVar2);
  if (*(int *)(local_78 + -8) == 0) {
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString((CString *)&local_78);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_74);
    ExceptionList = local_c;
    return;
  }
  CWnd::SetWindowTextA(*(CWnd **)(param_1 + 0xd0),&DAT_00492a60);
  CString::operator=((CString *)&local_74,(CString *)(param_1 + 0xb8));
  CString::TrimRight((CString *)&local_74,&DAT_004909d8);
  sprintf(local_70,s__c_c_c_s_s_004909c8,0x7a,9,*(undefined4 *)(local_74 + -8),local_74,local_78);
  uVar3 = 0xffffffff;
  pcVar2 = local_70;
  do {
    if (uVar3 == 0) break;
    uVar3 = uVar3 - 1;
    cVar1 = *pcVar2;
    pcVar2 = pcVar2 + 1;
  } while (cVar1 != '\0');
  thunk_FUN_004132d0(DAT_004952a0,~uVar3 - 1,local_70);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString((CString *)&local_78);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_74);
  ExceptionList = local_c;
  return;
}

