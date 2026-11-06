
void __thiscall thunk_FUN_004122f0(void *this,int param_1)

{
  HWND pHVar1;
  
  if (*(int *)((int)this + 0x128) == 1) {
    if (param_1 == 0) {
      pHVar1 = SetActiveWindow(*(HWND *)((int)this + 0x20));
      CWnd::FromHandle(pHVar1);
      return;
    }
    if (*(int *)((int)this + 0x120) != 0) {
      thunk_FUN_004123c0((int)this);
    }
  }
  CWnd::Default(this);
  if (*(int *)((int)this + 0x128) != param_1) {
    *(int *)((int)this + 0x128) = param_1;
    if ((param_1 != 0) && (*(int *)((int)this + 0x120) != 0)) {
      thunk_FUN_004123c0((int)this);
    }
    if (*(CDirectDraw **)((int)this + 0x120) != (CDirectDraw *)0x0) {
      CDirectDraw::OnActivate(*(CDirectDraw **)((int)this + 0x120),param_1);
    }
    if (*(CDirectInput **)((int)this + 0xe8) != (CDirectInput *)0x0) {
      CDirectInput::OnActivate(*(CDirectInput **)((int)this + 0xe8),param_1);
    }
  }
  return;
}


/* incoming refs
*/
