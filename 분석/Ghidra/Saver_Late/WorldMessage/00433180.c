
undefined4 * __thiscall FUN_00433180(void *this,undefined4 param_1,CString *param_2)

{
  CString *this_00;
  CPtrArray *this_01;
  undefined4 *puVar1;
  void *this_02;
  CDDSurface *pCVar2;
  int iVar3;
  CString local_14 [4];
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047d6c4;
  local_c = ExceptionList;
  this_00 = (CString *)((int)this + 0xc);
  ExceptionList = &local_c;
  local_10 = this;
  CString::CString(this_00);
  this_01 = (CPtrArray *)((int)this + 0x50);
  local_4 = 0;
  CPtrArray::CPtrArray(this_01);
  *(undefined ***)this_01 = &PTR_LAB_00484358;
  local_4._0_1_ = 1;
  CPtrArray::CPtrArray((CPtrArray *)((int)this + 100));
  *(undefined ***)((int)this + 100) = &PTR_LAB_00484370;
  local_4._0_1_ = 2;
  *(undefined ***)this = &PTR_LAB_0048452c;
  *(undefined4 *)((int)this + 0x90) = 0;
  *(undefined4 *)((int)this + 4) = param_1;
  CString::CString(local_14);
  local_4._0_1_ = 3;
  thunk_FUN_00412af0(local_14,1);
  CString::operator=(this_00,local_14);
  CString::operator+=(this_00,s_WLIB__0049006c);
  CString::operator+=(this_00,param_2);
  *(undefined4 *)((int)this + 0x10) = 0;
  *(undefined4 *)((int)this + 0x14) = 0;
  *(undefined4 *)((int)this + 0x18) = 0;
  *(undefined4 *)((int)this + 0x1c) = 0;
  *(undefined4 *)((int)this + 0x20) = 0;
  *(undefined4 *)((int)this + 0x24) = 0;
  if (DAT_004953e0 == (undefined4 *)0x0) {
    this_02 = operator_new(0xa8);
    local_4._0_1_ = 4;
    if (this_02 == (void *)0x0) {
      DAT_004953e0 = (undefined4 *)0x0;
    }
    else {
      DAT_004953e0 = thunk_FUN_00431ac0(this_02,*(undefined4 *)((int)this + 4),this_00,0);
    }
    local_4._0_1_ = 3;
  }
  thunk_FUN_00432f20(DAT_004953e0,this_00);
  if (DAT_004953e0[6] == 0) {
    *(undefined4 *)((int)this + 8) = 0;
    *(undefined4 *)((int)this + 0x88) = 0;
  }
  else {
    *(undefined4 *)((int)this + 0x40) = 0;
    *(undefined4 *)((int)this + 0x88) = 1;
    *(undefined4 *)((int)this + 0x44) = 0;
    puVar1 = DAT_004953e0;
    *(undefined4 *)((int)this + 0x48) = DAT_004953e0[0xe];
    *(undefined4 *)((int)this + 0x4c) = puVar1[0xf];
    pCVar2 = thunk_FUN_00432690(DAT_004953e0,0);
    *(CDDSurface **)((int)this + 8) = pCVar2;
    puVar1 = DAT_004953e0;
    *(undefined4 *)((int)this + 0x30) = DAT_004953e0[0x10];
    *(undefined4 *)((int)this + 0x34) = puVar1[0x11];
    *(undefined4 *)((int)this + 0x38) = puVar1[0x12];
    *(undefined4 *)((int)this + 0x3c) = puVar1[0x13];
    thunk_FUN_00432ce0(DAT_004953e0,this_01);
    thunk_FUN_00432e00(DAT_004953e0,(CPtrArray *)((int)this + 100));
    iVar3 = *(int *)((int)this + 0x4c) * *(int *)((int)this + 0x48);
    *(undefined4 *)((int)this + 0x78) = 1;
    *(undefined4 *)((int)this + 0x7c) = 0;
    *(undefined4 *)((int)this + 0x80) = 0;
    *(undefined4 *)((int)this + 0x84) = 0;
    *(uint *)((int)this + 0x8c) = (uint)((int)(iVar3 + (iVar3 >> 0x1f & 3U)) >> 2 < 0x12c00);
    *(undefined4 *)((int)this + 0x94) = 0;
    *(undefined4 *)((int)this + 0x98) = 0;
    *(undefined4 *)((int)this + 0x9c) = 0;
  }
  local_4 = CONCAT31(local_4._1_3_,2);
  CString::~CString(local_14);
  ExceptionList = local_c;
  return this;
}

