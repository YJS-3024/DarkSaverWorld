
CString * __cdecl FUN_1000f200(undefined4 param_1,CString *param_2)

{
  CString *pCVar1;
  int iVar2;
  CString *pCVar3;
  void *local_14;
  undefined4 local_10;
  void *local_c;
  undefined1 *local_8;
  CString *local_4;
  
  local_4 = (CString *)0xffffffff;
  local_8 = &LAB_1001559f;
  local_c = ExceptionList;
  local_10 = 0;
  ExceptionList = &local_c;
  CString::CString((CString *)&local_14);
  pCVar1 = param_2;
  local_4 = (CString *)0x1;
  iVar2 = CString::Find(param_2,' ');
  if (iVar2 == -1) {
    if (*(int *)(*(int *)pCVar1 + -8) != 0) {
      CString::operator=((CString *)&local_14,pCVar1);
      CString::Empty(pCVar1);
    }
  }
  else {
    pCVar3 = (CString *)CString::Left(pCVar1,(int)&param_2);
    local_8._0_1_ = 2;
    CString::operator=((CString *)&stack0xffffffe8,pCVar3);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
    CString::~CString((CString *)&param_1);
    CString::TrimLeft((CString *)&stack0xffffffe8);
    CString::TrimRight((CString *)&stack0xffffffe8);
    pCVar3 = (CString *)CString::Right(pCVar1,(int)&param_1);
    local_c._0_1_ = 3;
    CString::operator=(pCVar1,pCVar3);
    local_c = (void *)CONCAT31(local_c._1_3_,1);
    CString::~CString((CString *)&stack0x00000000);
    CString::TrimLeft(pCVar1);
  }
  pCVar1 = local_4;
  CString::CString(local_4,(CString *)&stack0xffffffe4);
  local_c = (void *)((uint)local_c & 0xffffff00);
  CString::~CString((CString *)&stack0xffffffe4);
  ExceptionList = local_14;
  return pCVar1;
}

