
undefined4 * __thiscall
FUN_00452d60(void *this,int param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  int *piVar1;
  HWND pHVar2;
  void *pvVar3;
  undefined4 *puVar4;
  undefined4 extraout_ECX;
  uint *puVar5;
  undefined1 *puVar6;
  int iVar7;
  undefined4 uVar8;
  undefined4 uVar9;
  undefined4 uVar10;
  CGdiObject *pCVar11;
  undefined4 uVar12;
  undefined1 *puVar13;
  undefined1 *puVar14;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_0047ee03;
  local_c = ExceptionList;
  local_4 = 0;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  local_4._0_1_ = 1;
  CString::CString((CString *)((int)this + 0xb8));
  local_4._0_1_ = 2;
  *(undefined ***)this = &PTR_LAB_00485450;
  CString::operator=((CString *)((int)this + 0xb8),(CString *)&stack0x00000018);
  pHVar2 = GetFocus();
  piVar1 = (int *)((int)this + 0xc0);
  *(HWND *)((int)this + 0xb4) = pHVar2;
  *piVar1 = 4;
  *(undefined4 *)((int)this + 0xc4) = 0x1b;
  *(undefined4 *)((int)this + 200) = 0xb5;
  *(undefined4 *)((int)this + 0xcc) = 0x73;
  pvVar3 = operator_new(0x80);
  local_4._0_1_ = 3;
  if (pvVar3 == (void *)0x0) {
    puVar4 = (undefined4 *)0x0;
  }
  else {
    puVar14 = (undefined1 *)0x0;
    uVar12 = 300;
    puVar13 = &stack0xffffffc8;
    iVar7 = *piVar1;
    puVar6 = (undefined1 *)0xe;
    puVar5 = (uint *)0xe;
    uVar8 = *(undefined4 *)((int)this + 0xc4);
    uVar9 = *(undefined4 *)((int)this + 200);
    uVar10 = *(undefined4 *)((int)this + 0xcc);
    CString::CString((CString *)&stack0xffffffbc,s_dark14_0048f0d4);
    puVar4 = thunk_FUN_00421cd0(pvVar3,param_1,puVar13,puVar5,puVar6,iVar7,uVar8,uVar9,uVar10,uVar12
                                ,puVar14);
  }
  *(undefined4 **)((int)this + 0xbc) = puVar4;
  puVar4[5] = *(int *)((int)this + 200) - *piVar1;
  *(LONG *)((int)this + 0xd4) = 4;
  *(undefined4 *)((int)this + 0xd8) = 0x78;
  local_4._0_1_ = 2;
  *(undefined4 *)((int)this + 0xdc) = 0xbd;
  *(undefined4 *)((int)this + 0xe0) = 0x87;
  pvVar3 = operator_new(0x80);
  local_4._0_1_ = 4;
  if (pvVar3 == (void *)0x0) {
    puVar4 = (undefined4 *)0x0;
  }
  else {
    puVar6 = (undefined1 *)0x80;
    puVar13 = (undefined1 *)0xe;
    pCVar11 = (CGdiObject *)0xe;
    uVar8 = extraout_ECX;
    CString::CString((CString *)&stack0xffffffd0,s_dark14_0048f0d4);
    puVar4 = thunk_FUN_00423390(pvVar3,DAT_004952a0,0,*(LONG *)((int)this + 0xd4),
                                *(LONG *)((int)this + 0xd8),*(LONG *)((int)this + 0xdc),
                                *(LONG *)((int)this + 0xe0),uVar8,pCVar11,puVar13,puVar6);
  }
  *(undefined4 **)((int)this + 0xd0) = puVar4;
  puVar4[0x1f] = &LAB_00401a3c;
  local_4 = CONCAT31(local_4._1_3_,2);
  thunk_FUN_00425cf0(*(void **)((int)this + 0xd0),0x3c,0x1a);
  local_4 = 0xffffffff;
  DAT_00497b70 = this;
  CString::~CString((CString *)&stack0x00000018);
  ExceptionList = local_c;
  return this;
}

