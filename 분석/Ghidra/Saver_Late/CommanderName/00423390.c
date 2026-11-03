
undefined4 * __thiscall
FUN_00423390(void *this,CWnd *param_1,int param_2,LONG param_3,LONG param_4,LONG param_5,
            LONG param_6,undefined4 param_7,CGdiObject *param_8,undefined1 *param_9,
            undefined1 *param_10)

{
  undefined1 *puVar1;
  CGdiObject *pCVar2;
  undefined1 *puVar3;
  undefined1 *puVar4;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_0047c9c8;
  local_c = ExceptionList;
  local_4 = 0;
  ExceptionList = &local_c;
  puVar4 = param_10;
  param_10 = &stack0xffffffe0;
  puVar1 = param_9;
  pCVar2 = param_8;
  puVar3 = param_9;
  CString::CString((CString *)&stack0xffffffe0,(CString *)&param_7);
  thunk_FUN_00424820(this,param_1,param_2,param_3,param_4,param_5,param_6,puVar1,pCVar2,puVar3,
                     (uint)puVar4);
  *(undefined ***)this = &PTR_LAB_00483db4;
  *(undefined4 *)((int)this + 0x7c) = 0;
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_7);
  ExceptionList = local_c;
  return this;
}

