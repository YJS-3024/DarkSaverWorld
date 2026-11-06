
CString * __cdecl FUN_1000f310(undefined4 param_1,undefined4 param_2)

{
  int iVar1;
  CString *pCVar2;
  CString *unaff_retaddr;
  CString local_18 [4];
  undefined4 local_14;
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_8 = &LAB_100155df;
  pvStack_c = ExceptionList;
  local_10 = (void *)0x0;
  local_4 = 1;
  ExceptionList = &pvStack_c;
  CString::CString(local_18);
  local_4 = CONCAT31(local_4._1_3_,2);
  iVar1 = CString::Find((CString *)&param_2,' ');
  if (iVar1 != -1) {
    pCVar2 = (CString *)CString::Right((CString *)&param_2,(int)&local_14);
    local_8._0_1_ = 3;
    CString::operator=((CString *)&stack0xffffffe4,pCVar2);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,2);
    CString::~CString(local_18);
    CString::TrimLeft((CString *)&stack0xffffffe4);
    CString::TrimRight((CString *)&stack0xffffffe4);
  }
  CString::CString(unaff_retaddr,(CString *)&stack0xffffffe4);
  local_14 = 1;
  local_8._0_1_ = 1;
  CString::~CString((CString *)&stack0xffffffe4);
  local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
  CString::~CString((CString *)&param_1);
  ExceptionList = local_10;
  return unaff_retaddr;
}


/* incoming refs
100112b9 UNCONDITIONAL_CALL
10011d89 UNCONDITIONAL_CALL
10012f8f UNCONDITIONAL_CALL
10010641 UNCONDITIONAL_CALL
10010749 UNCONDITIONAL_CALL
10010aab UNCONDITIONAL_CALL
10010b1f UNCONDITIONAL_CALL
10010b93 UNCONDITIONAL_CALL
10010c07 UNCONDITIONAL_CALL
10013f71 UNCONDITIONAL_CALL
1000f945 UNCONDITIONAL_CALL
CALLER 10013d60 LoadScenarioEventActions
  10004372 UNCONDITIONAL_CALL
CALLER 100109e0 FUN_100109e0
  10012e91 UNCONDITIONAL_CALL
CALLER 10010f20 FUN_10010f20
  10003883 UNCONDITIONAL_CALL
  10003896 UNCONDITIONAL_CALL
  1000dd87 UNCONDITIONAL_CALL
  10003bcc UNCONDITIONAL_CALL
  10003bdb UNCONDITIONAL_CALL
CALLER 100105a0 ExecuteCharPosCommand
  10013325 UNCONDITIONAL_CALL
CALLER 1000f7b0 FUN_1000f7b0
  1000fcfa UNCONDITIONAL_CALL
*/
