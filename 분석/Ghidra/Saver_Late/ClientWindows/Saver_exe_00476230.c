
void __thiscall FUN_00476230(void *this,int param_1,CString *param_2)

{
  int *piVar1;
  byte bVar2;
  CString *pCVar3;
  CString *pCVar4;
  byte *pbVar5;
  int iVar6;
  void *this_00;
  int *piVar7;
  byte *unaff_EBP;
  byte *pbVar8;
  bool bVar9;
  int iVar10;
  undefined1 *local_18 [2];
  void *local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  iVar10 = param_1;
  local_4 = 0xffffffff;
  local_8 = &LAB_004810b8;
  local_c = ExceptionList;
  local_10 = this;
  if (param_1 == 9) {
    if (*(int *)(DAT_004952a0 + 0x144) == 0) {
      ExceptionList = &local_c;
      CString::CString((CString *)local_18);
      local_4 = 0;
      CString::CString((CString *)&param_1);
      pCVar4 = param_2;
      local_4 = CONCAT31(local_4._1_3_,1);
      pCVar3 = (CString *)CString::Left(param_2,(int)&param_2);
      local_8._0_1_ = 2;
      CString::operator=((CString *)&stack0xffffffe4,pCVar3);
      local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
      CString::~CString((CString *)&param_1);
      pCVar4 = (CString *)CString::Mid(pCVar4,(int)local_18);
      local_c._0_1_ = 3;
      CString::operator=((CString *)&local_4,pCVar4);
      local_c._0_1_ = 1;
      CString::~CString((CString *)&stack0xffffffe4);
      iVar10 = 0;
      piVar1 = (int *)((int)this + 0x30);
      if (0 < *piVar1) {
        piVar7 = *(int **)((int)this + 0x2c);
        do {
          pbVar5 = *(byte **)(*piVar7 + 4);
          pbVar8 = unaff_EBP;
          do {
            bVar2 = *pbVar5;
            bVar9 = bVar2 < *pbVar8;
            if (bVar2 != *pbVar8) {
LAB_00476321:
              iVar6 = (1 - (uint)bVar9) - (uint)(bVar9 != 0);
              goto LAB_00476326;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar5[1];
            bVar9 = bVar2 < pbVar8[1];
            if (bVar2 != pbVar8[1]) goto LAB_00476321;
            pbVar5 = pbVar5 + 2;
            pbVar8 = pbVar8 + 2;
          } while (bVar2 != 0);
          iVar6 = 0;
LAB_00476326:
          if (iVar6 == 0) {
            thunk_FUN_00453760(*(void **)(*piVar7 + 8),(CString *)&local_4);
            goto LAB_0047637b;
          }
          piVar7 = piVar7 + 1;
          iVar10 = iVar10 + 1;
          this = local_18[0];
        } while (iVar10 < *piVar1);
      }
      CString::CString((CString *)&stack0xffffffcc,(CString *)&local_4);
      local_18[0] = &stack0xffffffc8;
      local_c._0_1_ = 4;
      CString::CString((CString *)&stack0xffffffc8,(CString *)&stack0xffffffe0);
      local_c._0_1_ = 1;
      thunk_FUN_00477520((int)this);
      thunk_FUN_00476840(this,0x50);
LAB_0047637b:
      local_c = (void *)((uint)local_c._1_3_ << 8);
      CString::~CString((CString *)&local_4);
      local_c = (void *)0xffffffff;
      CString::~CString((CString *)&stack0xffffffe0);
    }
  }
  else {
    ExceptionList = &local_c;
    this_00 = (void *)thunk_FUN_00475f50(this,0x13);
    if ((this_00 != (void *)0x0) && ((iVar10 != 3 || (DAT_00490ae0 != 0)))) {
      thunk_FUN_00453ee0(this_00,iVar10,param_2);
    }
  }
  ExceptionList = local_c;
  return;
}

