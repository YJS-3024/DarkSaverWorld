
undefined4 * __thiscall
FUN_00444720(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  HWND pHVar1;
  void *this_00;
  CWnd *pCVar2;
  undefined4 *puVar3;
  undefined4 extraout_ECX;
  int iVar4;
  LONG LVar5;
  LONG LVar6;
  LONG LVar7;
  LONG LVar8;
  undefined4 uVar9;
  CGdiObject *pCVar10;
  undefined1 *puVar11;
  undefined1 *puVar12;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e22a;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  *(undefined4 *)((int)this + 0xbc) = 0;
  *(undefined4 *)((int)this + 0xc4) = 0x32;
  *(undefined4 *)((int)this + 0xc0) = 0;
  *(undefined4 *)((int)this + 200) = 0x32;
  local_4 = 0;
  *(undefined ***)this = &PTR_LAB_004849b8;
  *(undefined4 *)((int)this + 0x90) = 0;
  pHVar1 = GetFocus();
  *(HWND *)((int)this + 0xb4) = pHVar1;
  this_00 = operator_new(0x80);
  local_4._0_1_ = 1;
  if (this_00 == (void *)0x0) {
    puVar3 = (undefined4 *)0x0;
  }
  else {
    puVar12 = (undefined1 *)0x0;
    puVar11 = (undefined1 *)0xe;
    pCVar10 = (CGdiObject *)0xe;
    uVar9 = extraout_ECX;
    CString::CString((CString *)&stack0xffffffd4,s_dark14_0048f0d4);
    LVar5 = *(LONG *)((int)this + 0xbc);
    LVar6 = *(LONG *)((int)this + 0xc0);
    local_4 = CONCAT31(local_4._1_3_,1);
    iVar4 = 0;
    LVar7 = *(LONG *)((int)this + 0xc4);
    LVar8 = *(LONG *)((int)this + 200);
    pCVar2 = (CWnd *)thunk_FUN_00444890();
    puVar3 = thunk_FUN_00423390(this_00,pCVar2,iVar4,LVar5,LVar6,LVar7,LVar8,uVar9,pCVar10,puVar11,
                                puVar12);
  }
  *(undefined4 **)((int)this + 0xb8) = puVar3;
  puVar3[0x1f] = &LAB_004035a3;
  DAT_00497b24 = this;
  ExceptionList = local_c;
  return this;
}

