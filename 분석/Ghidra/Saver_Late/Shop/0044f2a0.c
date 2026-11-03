
void __thiscall FUN_0044f2a0(void *this,undefined1 *param_1)

{
  void *this_00;
  undefined4 uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047ebf8;
  local_c = ExceptionList;
  local_10 = this;
  if ((short)param_1 == 0) {
    ExceptionList = &local_c;
    *(undefined4 *)((int)this + 0xd8) = 1;
  }
  else if ((short)param_1 == 1) {
    ExceptionList = &local_c;
    CString::CString((CString *)&param_1,&DAT_004908c4);
    local_4 = 0;
    thunk_FUN_00476870(*(int **)((int)this + 0xa0));
    *(undefined4 *)((int)this + 0xd8) = 0;
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  else if ((short)param_1 == 2) {
    ExceptionList = &local_c;
    CString::CString((CString *)&param_1,&DAT_004908a4);
    local_4 = 1;
    thunk_FUN_00476870(*(int **)((int)this + 0xa0));
    *(undefined4 *)((int)this + 0xd8) = 0;
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  else if ((short)param_1 == 3) {
    ExceptionList = &local_c;
    CString::CString((CString *)&param_1,&DAT_00490880);
    local_4 = 2;
    thunk_FUN_00476870(*(int **)((int)this + 0xa0));
    *(undefined4 *)((int)this + 0xd8) = 0;
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  else if ((short)param_1 == 4) {
    ExceptionList = &local_c;
    CString::CString((CString *)&param_1,&DAT_0049084c);
    local_4 = 3;
    thunk_FUN_00476870(*(int **)((int)this + 0xa0));
    *(undefined4 *)((int)this + 0xd8) = 0;
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  else if ((short)param_1 == 5) {
    ExceptionList = &local_c;
    CString::CString((CString *)&local_10,&DAT_00490828);
    local_4 = 4;
    thunk_FUN_00476870(*(int **)((int)this + 0xa0));
    *(undefined4 *)((int)this + 0xd8) = 0;
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_10);
  }
  else {
    ExceptionList = &local_c;
    *(undefined4 *)((int)this + 0xd8) = 0;
  }
  if (*(int *)((int)this + 0xd8) != 0) {
    (**(code **)(**(int **)((int)this + 0xa0) + 4))();
    this_00 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0xa0),0x49);
    if (this_00 != (void *)0x0) {
      uVar3 = *(undefined4 *)((int)this + 0xd4);
      uVar1 = *(undefined4 *)((int)this + 0xd0);
      param_1 = &stack0xffffffd8;
      uVar2 = uVar1;
      CString::CString((CString *)&stack0xffffffd8,(CString *)((int)this + 0xb4));
      thunk_FUN_004499c0(this_00,uVar1,uVar2,uVar3);
    }
  }
  ExceptionList = local_c;
  return;
}

