
void __fastcall FUN_00476870(int *param_1)

{
  void *this;
  CString *unaff_ESI;
  undefined4 unaff_retaddr;
  
  (**(code **)(*param_1 + 4))(0x2c,1);
  this = (void *)thunk_FUN_00475f50(param_1,0x2c);
  if (this != (void *)0x0) {
    thunk_FUN_00467f80(this,unaff_ESI,unaff_retaddr);
  }
  return;
}

