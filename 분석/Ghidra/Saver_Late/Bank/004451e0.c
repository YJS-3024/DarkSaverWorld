
void __thiscall FUN_004451e0(void *this,int param_1)

{
  int iVar1;
  int iVar2;
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  iVar1 = param_1;
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e2b0;
  local_c = ExceptionList;
  if (-1 < param_1) {
    local_10 = this;
    if (param_1 < 2) {
      if (*(int *)((int)this + 0xc0) != 0) {
        if (param_1 == 1) {
          if (*(int *)((int)this + 0xb8) == 0 && *(int *)((int)this + 0xbc) == 0) {
            ExceptionList = &local_c;
            CString::CString((CString *)&param_1,&DAT_00490464);
            local_4 = 0;
            thunk_FUN_00476870(*(int **)((int)this + 0xa0));
            local_4 = 0xffffffff;
            CString::~CString((CString *)&param_1);
            ExceptionList = local_c;
            return;
          }
        }
        else if ((param_1 == 0) && (*(int *)(*(int *)((int)this + 0xb0) + 0x4c) == 0)) {
          ExceptionList = &local_c;
          CString::CString((CString *)&local_10,&DAT_00490440);
          local_4 = 1;
          thunk_FUN_00476870(*(int **)((int)this + 0xa0));
          local_4 = 0xffffffff;
          CString::~CString((CString *)&local_10);
          ExceptionList = local_c;
          return;
        }
        ExceptionList = &local_c;
        (**(code **)(**(int **)((int)this + 0xa0) + 4))(0x23,1);
        iVar2 = thunk_FUN_00475f50(*(void **)((int)this + 0xa0),0x23);
        if (iVar2 != 0) {
          *(uint *)(iVar2 + 0xcc) = (uint)(iVar1 == 0);
        }
      }
    }
    else if (param_1 == 2) {
      ExceptionList = &local_c;
      iVar1 = (**(code **)(*(int *)this + 4))();
      thunk_FUN_004758e0(*(void **)((int)this + 0xa0),iVar1);
      ExceptionList = local_c;
      return;
    }
  }
  ExceptionList = local_c;
  return;
}

