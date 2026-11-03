
undefined4 * __thiscall
FUN_0046ea00(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  HWND pHVar1;
  void *this_00;
  undefined4 *puVar2;
  undefined4 extraout_ECX;
  undefined4 uVar3;
  CGdiObject *pCVar4;
  undefined1 *puVar5;
  undefined1 *puVar6;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_004808d2;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  *(undefined4 *)((int)this + 0xbc) = 0x39;
  *(undefined4 *)((int)this + 0xc4) = 0x74;
  *(undefined4 *)((int)this + 0xc0) = 0x2f;
  *(undefined4 *)((int)this + 200) = 0x3e;
  local_4 = 0;
  *(undefined ***)this = &PTR_LAB_004862ec;
  *(undefined4 *)((int)this + 0xcc) = 1;
  *(undefined4 *)((int)this + 0x90) = 0;
  pHVar1 = GetFocus();
  *(HWND *)((int)this + 0xb4) = pHVar1;
  this_00 = operator_new(0x80);
  local_4 = CONCAT31(local_4._1_3_,1);
  if (this_00 == (void *)0x0) {
    puVar2 = (undefined4 *)0x0;
  }
  else {
    puVar6 = (undefined1 *)0x0;
    puVar5 = (undefined1 *)0xe;
    pCVar4 = (CGdiObject *)0xe;
    uVar3 = extraout_ECX;
    CString::CString((CString *)&stack0xffffffd8,s_dark14_0048f0d4);
    puVar2 = thunk_FUN_00423390(this_00,DAT_004952a0,0,*(LONG *)((int)this + 0xbc),
                                *(LONG *)((int)this + 0xc0),*(LONG *)((int)this + 0xc4),
                                *(LONG *)((int)this + 200),uVar3,pCVar4,puVar5,puVar6);
  }
  *(undefined4 **)((int)this + 0xb8) = puVar2;
  puVar2[0x1f] = &LAB_00402bf3;
  DAT_00497c40 = this;
  ExceptionList = local_c;
  return this;
}

