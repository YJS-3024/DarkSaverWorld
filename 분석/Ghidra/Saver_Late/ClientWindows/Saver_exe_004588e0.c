
void __thiscall FUN_004588e0(void *this,char *param_1,LONG param_2)

{
  int iVar1;
  POINT pt;
  HWND pHVar2;
  BOOL BVar3;
  HWND pHVar4;
  void *local_c;
  undefined *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &DAT_0047f378;
  local_c = ExceptionList;
  if (*(int *)((int)this + 0xcc) == 0) {
    pHVar4 = (HWND)0x0;
  }
  else {
    pHVar4 = *(HWND *)(*(int *)((int)this + 0xcc) + 0x20);
  }
  ExceptionList = &local_c;
  pHVar2 = GetFocus();
  if (pHVar4 != pHVar2) {
    pt.y = param_2;
    pt.x = (LONG)param_1;
    BVar3 = PtInRect((RECT *)((int)this + 0xd0),pt);
    if (BVar3 != 0) {
      iVar1 = *(int *)((int)this + 0xb4);
      pHVar4 = GetFocus();
      *(HWND *)((int)this + 200) = pHVar4;
      CWnd::SetFocus(*(CWnd **)((int)this + 0xcc));
      CString::CString((CString *)&param_1,(char *)(iVar1 + 0xbb));
      local_4 = 0;
      CWnd::SetWindowTextA(*(CWnd **)((int)this + 0xcc),param_1);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&param_1);
    }
  }
  ExceptionList = local_c;
  return;
}

