
void __thiscall FUN_00412c80(void *this,int param_1)

{
  int *piVar1;
  
  if (*(int *)((int)this + 0x150) == 1) {
    *(undefined4 *)((int)this + 0x14c) = 1;
  }
  if (*(int *)((int)this + 0xc4) < 0) {
    PostMessageA(*(HWND *)((int)this + 0x20),0x112,0xf060,0);
  }
  if (((*(int *)((int)this + 0x144) == 0) ||
      (piVar1 = *(int **)((int)this + 0x148), piVar1 == (int *)0x0)) &&
     (piVar1 = *(int **)((int)this + 0xf0), piVar1 == (int *)0x0)) {
    if (((param_1 != 9) && (param_1 != 0xd)) && (param_1 != 0x1b)) {
      CWnd::Default(this);
    }
    return;
  }
  (**(code **)(*piVar1 + 0x50))(&param_1,&stack0x00000008,&stack0x0000000c);
  return;
}


/* incoming refs
*/
