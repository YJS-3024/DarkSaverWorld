
void __thiscall FUN_00424ad0(CWnd *param_1,WPARAM param_2,uint param_3,int param_4)

{
  if (param_2 == 0x1b) {
    thunk_FUN_00424b60(param_1);
  }
  CWnd::Default(param_1);
  SendMessageA(*(HWND *)(DAT_004952a0 + 0x20),0x100,param_2,param_4 << 0x10 | param_3 & 0xffff);
  return;
}


/* incoming refs
*/
