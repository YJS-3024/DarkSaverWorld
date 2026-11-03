
undefined4 * __thiscall
FUN_00465e10(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  CString *this_00;
  int iVar1;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_004801a2;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  local_4 = 0;
  CBrush::CBrush((CBrush *)((int)this + 0xc4),0xc83232);
  local_4._0_1_ = 1;
  CBrush::CBrush((CBrush *)((int)this + 0xcc),0xc0c0c0);
  this_00 = (CString *)((int)this + 0x108);
  local_4._0_1_ = 2;
  CString::CString(this_00);
  local_4 = CONCAT31(local_4._1_3_,3);
  *(undefined ***)this = &PTR_LAB_00485e14;
  thunk_FUN_00466b90((int)this);
  thunk_FUN_00466c00(this);
  iVar1 = thunk_FUN_0042f5c0(*(void **)(*(int *)((int)this + 0xa0) + 0x48));
  if (iVar1 == 0) {
    CString::Empty(this_00);
  }
  else {
    CString::operator=(this_00,*(char **)(iVar1 + 0x24));
  }
  *(undefined4 *)((int)this + 0xb4) = 0;
  thunk_FUN_00466ee0(this,0);
  ExceptionList = local_c;
  return this;
}

