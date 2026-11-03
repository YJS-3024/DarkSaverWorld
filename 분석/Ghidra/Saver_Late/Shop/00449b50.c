
undefined4 * __thiscall
FUN_00449b50(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  CDDSurface *this_00;
  void *this_01;
  undefined4 *puVar1;
  LPRECT ptVar2;
  int iVar3;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e76b;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  iVar3 = 0;
  local_4 = 0;
  CPen::CPen((CPen *)((int)this + 0x344),0,1,0x40ffff);
  local_4 = CONCAT31(local_4._1_3_,1);
  *(undefined ***)this = &PTR_LAB_00484d98;
  *(undefined4 *)((int)this + 0x90) = 0;
  ptVar2 = (LPRECT)((int)this + 0xc4);
  do {
    SetRect(ptVar2,(iVar3 % 7) * 0x23 + 0x14,(iVar3 / 7) * 0x23 + 0x28,0,0);
    thunk_FUN_0046bab0(&ptVar2->left,0x20,0x20);
    iVar3 = iVar3 + 1;
    ptVar2 = ptVar2 + 1;
  } while (iVar3 < 0x1c);
  *(LONG *)((int)this + 0x284) = 0x116;
  *(undefined4 *)((int)this + 0x288) = 0x28;
  *(undefined4 *)((int)this + 0x28c) = 0x136;
  *(undefined4 *)((int)this + 0x290) = 0x48;
  iVar3 = 1;
  ptVar2 = (LPRECT)((int)this + 0x294);
  do {
    ptVar2->left = *(LONG *)((int)this + 0x284);
    ptVar2->top = *(LONG *)((int)this + 0x288);
    ptVar2->right = *(LONG *)((int)this + 0x28c);
    ptVar2->bottom = *(LONG *)((int)this + 0x290);
    OffsetRect(ptVar2,(iVar3 % 3) * 0x23,(iVar3 / 3) * 0x23);
    iVar3 = iVar3 + 1;
    ptVar2 = ptVar2 + 1;
  } while (iVar3 < 0xc);
  *(undefined4 *)((int)this + 0xc0) = 0;
  *(undefined4 *)((int)this + 0xb8) = 0;
  *(undefined4 *)((int)this + 0xbc) = 0;
  *(undefined4 *)((int)this + 0x350) = 1;
  *(undefined4 *)((int)this + 0x34c) = 0xffffffff;
  this_00 = operator_new(0x8c);
  local_4._0_1_ = 2;
  if (this_00 == (CDDSurface *)0x0) {
    this_00 = (CDDSurface *)0x0;
  }
  else {
    CDDSurface::CDDSurface(this_00);
    *(undefined ***)this_00 = &PTR_LAB_00483108;
  }
  local_4._0_1_ = 1;
  *(CDDSurface **)((int)this + 0x358) = this_00;
  CDDSurface::CreateFromImage(this_00,*(CDirectDraw **)((int)this + 4),(char *)0x9a,0);
  CDDSurface::SetColorKey(*(CDDSurface **)((int)this + 0x358),0);
  *(undefined4 *)((int)this + 0x35c) = 0;
  *(undefined4 *)((int)this + 0x360) = 0xffffffff;
  *(undefined4 *)((int)this + 0x364) = 0;
  *(undefined4 *)((int)this + 0x368) = 0xffffffff;
  this_01 = operator_new(0x54);
  local_4 = CONCAT31(local_4._1_3_,3);
  if (this_01 == (void *)0x0) {
    puVar1 = (undefined4 *)0x0;
  }
  else {
    iVar3 = *(int *)((int)this + 0xac);
    puVar1 = thunk_FUN_00437aa0(this_01,iVar3,*(undefined4 *)(iVar3 + 4),*(undefined4 *)(iVar3 + 8),
                                0x10,0xbb,0x194,0xfa,100,0);
  }
  *(undefined4 **)((int)this + 0x354) = puVar1;
  puVar1[5] = 0x184;
  ExceptionList = local_c;
  return this;
}

