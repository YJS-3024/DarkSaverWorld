
void __thiscall FUN_00463f00(void *this,undefined4 param_1,int *param_2,int param_3)

{
  int *piVar1;
  int *piVar2;
  int iVar3;
  int iVar4;
  HBRUSH pHVar5;
  undefined4 local_3c;
  undefined4 local_38;
  undefined4 local_34;
  int local_30;
  int local_2c;
  undefined1 local_28 [16];
  void *local_18;
  LONG local_14;
  LONG local_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  piVar1 = param_2;
  local_4 = 0xffffffff;
  puStack_8 = &LAB_00480098;
  pvStack_c = ExceptionList;
  iVar3 = (int)param_2 * 0x10;
  local_28._12_4_ = *(int *)(iVar3 + 0x3dc + (int)this);
  local_18 = *(void **)((int)this + iVar3 + 0x3e0);
  local_14 = *(LONG *)((int)this + iVar3 + 0x3e4);
  local_10 = *(LONG *)((int)this + iVar3 + 1000);
  ExceptionList = &pvStack_c;
  OffsetRect((LPRECT)(local_28 + 0xc),*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  local_2c = local_28._12_4_;
  local_28._0_4_ = local_18;
  local_28._4_4_ = local_14;
  local_28._8_4_ = local_10;
  iVar4 = thunk_FUN_00464150(this,(int)piVar1,&local_34,&local_38,&local_3c,&local_30);
  CString::CString((CString *)&param_2);
  local_4 = 0;
  CString::Format((CString *)&param_2,(char *)&param_2);
  piVar2 = param_2;
  iVar3 = (int)this + (int)piVar1 * 8 + 0x160;
  local_28._8_4_ = local_28._0_4_ + iVar4;
  pHVar5 = (HBRUSH)0x0;
  if (iVar3 != 0) {
    pHVar5 = *(HBRUSH *)(iVar3 + 4);
  }
  FillRect((HDC)param_2[1],(RECT *)local_28,pHVar5);
  local_28._0_4_ = local_28._0_4_ + local_2c;
  pHVar5 = (HBRUSH)0x0;
  if (this != (void *)0xfffffea8) {
    pHVar5 = *(HBRUSH *)((int)this + 0x15c);
  }
  FillRect((HDC)piVar2[1],(RECT *)local_28,pHVar5);
  (**(code **)(*piVar2 + 0x70))(param_3,*(undefined4 *)(param_3 + -8),&local_18,0x21);
  local_10 = 0xffffffff;
  CString::~CString((CString *)&local_4);
  ExceptionList = local_18;
  return;
}

