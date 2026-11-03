
undefined4 * __thiscall
FUN_00472440(void *this,CDirectDraw *param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  CDDSurface *this_00;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_00480b40;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  local_4 = 0;
  CString::CString((CString *)((int)this + 0xb4));
  local_4._0_1_ = 1;
  *(undefined ***)this = &PTR_LAB_0048652c;
  *(undefined4 *)((int)this + 0xb8) = 0;
  this_00 = operator_new(0x8c);
  local_4._0_1_ = 2;
  if (this_00 == (CDDSurface *)0x0) {
    this_00 = (CDDSurface *)0x0;
  }
  else {
    CDDSurface::CDDSurface(this_00);
    *(undefined ***)this_00 = &PTR_LAB_00483108;
  }
  local_4 = CONCAT31(local_4._1_3_,1);
  *(CDDSurface **)((int)this + 0xbc) = this_00;
  CDDSurface::Create(this_00,param_1,0x40,0x40,0);
  GetFace(*(CDDSurface **)((int)this + 0xbc),0x7d,0,0);
  ExceptionList = local_c;
  return this;
}

