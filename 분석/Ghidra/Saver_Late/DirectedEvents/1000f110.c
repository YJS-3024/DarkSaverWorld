
CString * __cdecl FUN_1000f110(undefined4 param_1,int param_2)

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
  
  local_8 = &LAB_1001555f;
  pvStack_c = ExceptionList;
  local_10 = (void *)0x0;
  local_4 = 1;
  ExceptionList = &pvStack_c;
  CString::CString(local_18);
  local_4 = CONCAT31(local_4._1_3_,2);
  iVar1 = CString::Find((CString *)&param_2,' ');
  if (iVar1 == -1) {
    if (*(int *)(param_2 + -8) != 0) {
      CString::operator=(local_18,(CString *)&param_2);
    }
  }
  else {
    pCVar2 = (CString *)CString::Left((CString *)&param_2,(int)&local_14);
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
10011278 UNCONDITIONAL_CALL
10011403 UNCONDITIONAL_CALL
10011494 UNCONDITIONAL_CALL
10011588 UNCONDITIONAL_CALL
10011614 UNCONDITIONAL_CALL
1001169c UNCONDITIONAL_CALL
1001186d UNCONDITIONAL_CALL
10011998 UNCONDITIONAL_CALL
10011d4d UNCONDITIONAL_CALL
10011dcb UNCONDITIONAL_CALL
10012a68 UNCONDITIONAL_CALL
10012f4e UNCONDITIONAL_CALL
10012fd0 UNCONDITIONAL_CALL
10013243 UNCONDITIONAL_CALL
100133d7 UNCONDITIONAL_CALL
100136a2 UNCONDITIONAL_CALL
10013722 UNCONDITIONAL_CALL
10013c43 UNCONDITIONAL_CALL
10010607 UNCONDITIONAL_CALL
1001070f UNCONDITIONAL_CALL
10010a71 UNCONDITIONAL_CALL
10010ae5 UNCONDITIONAL_CALL
10010b59 UNCONDITIONAL_CALL
10010bcd UNCONDITIONAL_CALL
10013f37 UNCONDITIONAL_CALL
10013fea UNCONDITIONAL_CALL
1000f905 UNCONDITIONAL_CALL
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
