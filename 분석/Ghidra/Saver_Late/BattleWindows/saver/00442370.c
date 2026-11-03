
void __thiscall FUN_00442370(void *this,int param_1)

{
  int iVar1;
  int *piVar2;
  clock_t cVar3;
  CString *this_00;
  void *pvStack00000008;
  void *local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047dfe3;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  local_10 = this;
  CString::CString((CString *)&local_10);
  local_4 = 0;
  iVar1 = thunk_FUN_004429b0(this,param_1);
  if (iVar1 != 0) {
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_10);
    ExceptionList = local_c;
    return;
  }
  CString::Format(this_00,(char *)&local_10);
  pvStack00000008 = operator_new(0xa8);
  if (pvStack00000008 == (void *)0x0) {
    piVar2 = (int *)0x0;
  }
  else {
    piVar2 = thunk_FUN_00441d30(pvStack00000008,this,*(undefined4 *)((int)this + 0x20),
                                (CString *)&local_c,param_1);
  }
  iVar1 = thunk_FUN_00434ea0(piVar2);
  if (iVar1 == 0) {
    if (piVar2 != (int *)0x0) {
      (**(code **)*piVar2)(1);
    }
  }
  else {
    CPtrArray::SetAtGrow((CPtrArray *)((int)this + 4),*(int *)((int)this + 0xc),piVar2);
    *(int *)((int)this + 0x18) = *(int *)((int)this + 0xc);
    *(int *)((int)this + 0x1c) = *(int *)((int)this + 0xc) + -1;
    if (param_1 == 8) {
      cVar3 = clock();
      *(clock_t *)((int)this + 0x2c) = cVar3;
    }
  }
  CString::~CString((CString *)&local_c);
  ExceptionList = local_8;
  return;
}

