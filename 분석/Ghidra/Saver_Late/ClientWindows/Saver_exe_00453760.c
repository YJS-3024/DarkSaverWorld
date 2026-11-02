
void __thiscall FUN_00453760(void *this,CString *param_1)

{
  int iVar1;
  
  iVar1 = thunk_FUN_004220d0(*(void **)((int)this + 0xbc),param_1,9);
  if (0 < iVar1) {
    do {
      thunk_FUN_00437ba0(*(int *)((int)this + 0xbc));
      iVar1 = iVar1 + -1;
    } while (iVar1 != 0);
  }
  return;
}

