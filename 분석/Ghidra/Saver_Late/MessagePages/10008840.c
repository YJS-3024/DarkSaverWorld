
int __thiscall FUN_10008840(void *this,int param_1)

{
  int iVar1;
  CString *pCVar2;
  int iVar3;
  CString *pCVar4;
  int iVar5;
  int local_20;
  CString local_1c [4];
  undefined1 local_18 [8];
  void *local_10;
  void *local_c;
  undefined1 *local_8;
  int local_4;
  
  local_8 = &LAB_100151d8;
  local_c = ExceptionList;
  iVar3 = 0;
  local_4 = 0;
  ExceptionList = &local_c;
  local_10 = this;
  CString::CString(local_1c);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_20);
  local_4 = CONCAT31(local_4._1_3_,2);
  iVar5 = 100;
  pCVar4 = (CString *)((int)this + 0x1c);
  do {
    CString::Empty(pCVar4);
    pCVar4 = pCVar4 + 4;
    iVar5 = iVar5 + -1;
  } while (iVar5 != 0);
  CString::Empty(local_1c);
  iVar5 = *(int *)(param_1 + -8);
  CString::operator=((CString *)&local_20,(CString *)&param_1);
  iVar1 = CString::Find((CString *)&local_20,'|');
  pCVar4 = (CString *)((int)this + 0x1c);
  do {
    if (iVar1 == -1) {
LAB_10008975:
      if (*(int *)(local_20 + -8) != 0) {
        CString::TrimLeft((CString *)&local_20);
        CString::TrimRight((CString *)&local_20);
        iVar5 = iVar3 * 4;
        iVar3 = iVar3 + 1;
        CString::operator=((CString *)((int)local_10 + iVar5 + 0x1c),(CString *)&local_20);
      }
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_20);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString(local_1c);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&param_1);
      ExceptionList = local_c;
      return iVar3;
    }
    pCVar2 = (CString *)CString::Left((CString *)&local_20,(int)local_18);
    local_8._0_1_ = 3;
    CString::operator=((CString *)&local_20,pCVar2);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,2);
    CString::~CString(local_1c);
    CString::TrimLeft((CString *)&local_20);
    CString::TrimRight((CString *)&local_20);
    iVar3 = iVar3 + 1;
    CString::operator=(pCVar4,(CString *)&local_20);
    iVar5 = iVar5 + (-1 - iVar1);
    if (iVar5 < 1) {
      CString::Empty((CString *)&stack0xffffffdc);
      goto LAB_10008975;
    }
    pCVar2 = (CString *)CString::Right((CString *)&stack0xffffffdc,(int)local_18);
    local_c._0_1_ = 4;
    CString::operator=((CString *)&stack0xffffffd8,pCVar2);
    local_c = (void *)CONCAT31(local_c._1_3_,2);
    CString::~CString(local_1c);
    iVar1 = CString::Find((CString *)&stack0xffffffd8,'|');
    pCVar4 = pCVar4 + 4;
  } while( true );
}


/* incoming refs
10008a51 UNCONDITIONAL_CALL
CALLER 100089f0 FUN_100089f0
  10010e88 UNCONDITIONAL_CALL
*/
