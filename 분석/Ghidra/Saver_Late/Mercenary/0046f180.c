
undefined4 * __thiscall
FUN_0046f180(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  LPRECT lprc;
  int iVar1;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_00480976;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  local_4 = 0;
  CPen::CPen((CPen *)((int)this + 0x18c),0,1,0x40ffff);
  *(LONG *)((int)this + 0xe4) = 0x16;
  *(undefined4 *)((int)this + 0x90) = 0;
  local_4 = CONCAT31(local_4._1_3_,1);
  *(undefined4 *)((int)this + 0xe8) = 0x2a;
  *(undefined ***)this = &PTR_LAB_00486358;
  *(undefined4 *)((int)this + 0xec) = 0x36;
  *(undefined4 *)((int)this + 0xf0) = 0x4a;
  iVar1 = 1;
  lprc = (LPRECT)((int)this + 0xf4);
  do {
    lprc->left = *(LONG *)((int)this + 0xe4);
    lprc->top = *(LONG *)((int)this + 0xe8);
    lprc->right = *(LONG *)((int)this + 0xec);
    lprc->bottom = *(LONG *)((int)this + 0xf0);
    OffsetRect(lprc,(iVar1 % 3) * 0x23,(iVar1 / 3) * 0x23);
    iVar1 = iVar1 + 1;
    lprc = lprc + 1;
  } while (iVar1 < 9);
  *(undefined4 *)((int)this + 0xb4) = 0x9b;
  *(undefined4 *)((int)this + 0xb8) = 0x27;
  *(undefined4 *)((int)this + 0xbc) = 0xbb;
  *(undefined4 *)((int)this + 0xc0) = 0x47;
  *(undefined4 *)((int)this + 0xc4) = 0xbd;
  *(undefined4 *)((int)this + 200) = 0x30;
  *(undefined4 *)((int)this + 0xcc) = 0xd3;
  *(undefined4 *)((int)this + 0xd0) = 0x40;
  *(undefined4 *)((int)this + 0xd4) = 0x13;
  *(undefined4 *)((int)this + 0x178) = 0;
  *(undefined4 *)((int)this + 0x17c) = 0;
  *(undefined4 *)((int)this + 0xd8) = 0x94;
  *(undefined4 *)((int)this + 0x180) = 0;
  *(undefined4 *)((int)this + 0xdc) = 0x94;
  *(undefined4 *)((int)this + 0xe0) = 0xea;
  thunk_FUN_0046fdc0(this);
  ExceptionList = local_c;
  return this;
}

