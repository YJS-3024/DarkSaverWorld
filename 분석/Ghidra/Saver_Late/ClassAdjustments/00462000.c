
void __thiscall FUN_00462000(void *this,undefined4 param_1)

{
  undefined2 uVar1;
  int iVar2;
  char *pcVar3;
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_00480018;
  local_c = ExceptionList;
  iVar2 = *(int *)((int)this + 0x13c);
  local_10 = this;
  if (*(int *)((int)this + 0xb8) == -1) {
    if (*(int *)((int)this + 0x198) == 0x28) {
      ExceptionList = &local_c;
      CString::CString((CString *)&param_1,&DAT_00490d30);
      local_4 = 0;
      thunk_FUN_00476870(*(int **)((int)this + 0xa0));
      local_4 = 0xffffffff;
      CString::~CString((CString *)&param_1);
      ExceptionList = local_c;
      return;
    }
  }
  else if (*(short *)(iVar2 + 0x42) != 0) {
    ExceptionList = &local_c;
    pcVar3 = operator_new(0x14);
    local_10 = (void *)0x0;
    thunk_FUN_0043b9a0((int)pcVar3,(int *)&local_10,0x84);
    thunk_FUN_0043b970((int)pcVar3,(int *)&local_10,(short)param_1);
    thunk_FUN_0043b970((int)pcVar3,(int *)&local_10,*(undefined2 *)(iVar2 + 4));
    if (*(int *)((int)this + 0xbc) == 0) {
      uVar1 = *(undefined2 *)(iVar2 + 0x46 + *(int *)((int)this + 0xb8) * 2);
    }
    else {
      uVar1 = *(undefined2 *)(iVar2 + 0x70 + *(int *)((int)this + 0xb8) * 2);
    }
    thunk_FUN_0043b970((int)pcVar3,(int *)&local_10,uVar1);
    *(undefined2 *)(iVar2 + 0x42) = 0;
    thunk_FUN_004132d0(DAT_004952a0,(int)local_10,pcVar3);
    operator_delete(pcVar3);
  }
  ExceptionList = local_c;
  return;
}

