
CString * FUN_10010810(undefined4 param_1,undefined4 param_2,int param_3)

{
  CString *pCVar1;
  int iVar2;
  undefined4 *puVar3;
  CString local_1c [4];
  undefined4 local_18;
  void *local_14;
  undefined4 local_10;
  void *pvStack_c;
  undefined1 *local_8;
  CString *local_4;
  
  local_8 = &LAB_100158ff;
  pvStack_c = ExceptionList;
  local_10 = 0;
  local_4 = (CString *)0x2;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_18);
  local_4._0_1_ = 3;
  CString::CString(local_1c);
  local_4 = (CString *)CONCAT31(local_4._1_3_,4);
  CString::operator=(local_1c,(CString *)&param_3);
  pCVar1 = (CString *)CString::Left((CString *)&param_3,(int)&local_14);
  local_8._0_1_ = 5;
  CString::operator=(local_1c,pCVar1);
  local_8 = (undefined1 *)CONCAT31(local_8._1_3_,4);
  CString::~CString((CString *)&local_18);
  iVar2 = CString::Find(local_1c,&DAT_1001a560);
  if (iVar2 == -1) {
    iVar2 = CString::Find(local_1c,&DAT_1001a568);
    if (iVar2 == -1) {
      iVar2 = CString::Find(local_1c,&DAT_1001a564);
      if (iVar2 == -1) goto LAB_1001097b;
    }
    CString::operator=((CString *)&stack0xffffffe0,(CString *)&DAT_1006a73c);
  }
  else if (param_3 == 0) {
    puVar3 = (undefined4 *)CString::Mid((CString *)&param_2,(int)&param_3,3);
    iVar2 = atoi((char *)*puVar3);
    CString::~CString((CString *)&param_2);
    CString::Format((CString *)(iVar2 * 0x67),&stack0xffffffdc);
  }
  else {
    puVar3 = (undefined4 *)CString::Mid((CString *)&param_2,(int)&param_3,3);
    iVar2 = atoi((char *)*puVar3);
    CString::~CString((CString *)&param_2);
    CString::operator=((CString *)&stack0xffffffdc,&DAT_1001ab24 + iVar2 * 0x19c);
  }
LAB_1001097b:
  pCVar1 = local_4;
  CString::CString(local_4,(CString *)&stack0xffffffdc);
  local_18 = 1;
  pvStack_c._0_1_ = 3;
  CString::~CString((CString *)&stack0xffffffdc);
  pvStack_c._0_1_ = 2;
  CString::~CString((CString *)&stack0xffffffe0);
  pvStack_c._0_1_ = 1;
  CString::~CString((CString *)&stack0x00000000);
  pvStack_c = (void *)((uint)pvStack_c._1_3_ << 8);
  CString::~CString((CString *)&param_1);
  ExceptionList = local_14;
  return pCVar1;
}


/* incoming refs
10010c5f UNCONDITIONAL_CALL
10010cb4 UNCONDITIONAL_CALL
10010e01 UNCONDITIONAL_CALL
CALLER 100109e0 FUN_100109e0
  10012e91 UNCONDITIONAL_CALL
*/
