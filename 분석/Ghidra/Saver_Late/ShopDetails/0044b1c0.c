
void __fastcall FUN_0044b1c0(int param_1)

{
  itemrecord iVar1;
  char cVar2;
  int iVar3;
  int iVar4;
  itemrecord *piVar5;
  itemrecord *piVar6;
  uint uVar7;
  uint uVar8;
  CString *this;
  CString *extraout_ECX;
  CString *extraout_ECX_00;
  CString *extraout_ECX_01;
  CString *extraout_ECX_02;
  CString *extraout_ECX_03;
  CString *extraout_ECX_04;
  CString *extraout_ECX_05;
  CString *extraout_ECX_06;
  CString *extraout_ECX_07;
  CString *extraout_ECX_08;
  CString *extraout_ECX_09;
  CString *extraout_ECX_10;
  CString *extraout_ECX_11;
  CString *extraout_ECX_12;
  CString *extraout_ECX_13;
  CString *extraout_ECX_14;
  CString *extraout_ECX_15;
  CString *extraout_ECX_16;
  CString *this_00;
  itemrecord *piVar9;
  itemrecord *piVar10;
  char *pcVar11;
  char *pcVar12;
  itemrecord *piVar13;
  void *unaff_retaddr;
  CString local_20 [4];
  CString local_1c [4];
  int local_18;
  CString local_14 [4];
  int local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047e870;
  local_c = ExceptionList;
  iVar4 = *(int *)(param_1 + 0x34c);
  local_18 = param_1;
  if (iVar4 == -1) {
    iVar4 = *(int *)(param_1 + 0x354);
    ExceptionList = &local_c;
    CStringList::RemoveAll((CStringList *)(iVar4 + 0x1c));
    *(undefined4 *)(iVar4 + 0x4c) = 0;
  }
  else {
    if (*(int *)(param_1 + 0x350) == 0) {
      iVar4 = *(int *)(*(int *)(param_1 + 0xbc) + iVar4 * 4);
    }
    else {
      iVar3 = *(int *)(param_1 + 0xb0);
      if (((*(int *)(iVar3 + 0x44) == 0) || (*(int *)(iVar3 + 0x48) == 0)) ||
         (*(int *)(iVar3 + 0x40) <= iVar4)) {
        iVar4 = -1;
      }
      else {
        iVar4 = *(int *)(*(int *)(iVar3 + 0x44) + iVar4 * 4);
      }
    }
    ExceptionList = &local_c;
    piVar5 = GetItemTable(iVar4);
    if (piVar5 != (itemrecord *)0x0) {
      iVar4 = *(int *)(param_1 + 0x354);
      CStringList::RemoveAll((CStringList *)(iVar4 + 0x1c));
      *(undefined4 *)(iVar4 + 0x4c) = 0;
      piVar6 = operator_new(0xff);
      piVar13 = piVar6;
      if (piVar5[0x94] == (itemrecord)0x0) {
        uVar7 = 0xffffffff;
        pcVar11 = &DAT_0049076c;
        do {
          pcVar12 = pcVar11;
          if (uVar7 == 0) break;
          uVar7 = uVar7 - 1;
          pcVar12 = pcVar11 + 1;
          cVar2 = *pcVar11;
          pcVar11 = pcVar12;
        } while (cVar2 != '\0');
        uVar7 = ~uVar7;
        piVar9 = (itemrecord *)(pcVar12 + -uVar7);
        for (uVar8 = uVar7 >> 2; uVar8 != 0; uVar8 = uVar8 - 1) {
          *(undefined4 *)piVar13 = *(undefined4 *)piVar9;
          piVar9 = piVar9 + 4;
          piVar13 = piVar13 + 4;
        }
      }
      else {
        uVar7 = 0xffffffff;
        piVar9 = piVar5 + 0x94;
        do {
          piVar10 = piVar9;
          if (uVar7 == 0) break;
          uVar7 = uVar7 - 1;
          piVar10 = piVar9 + 1;
          iVar1 = *piVar9;
          piVar9 = piVar10;
        } while (iVar1 != (itemrecord)0x0);
        uVar7 = ~uVar7;
        piVar9 = piVar10 + -uVar7;
        for (uVar8 = uVar7 >> 2; uVar8 != 0; uVar8 = uVar8 - 1) {
          *(undefined4 *)piVar13 = *(undefined4 *)piVar9;
          piVar9 = piVar9 + 4;
          piVar13 = piVar13 + 4;
        }
      }
      for (uVar7 = uVar7 & 3; uVar7 != 0; uVar7 = uVar7 - 1) {
        *piVar13 = *piVar9;
        piVar9 = piVar9 + 1;
        piVar13 = piVar13 + 1;
      }
      CString::CString(local_20);
      local_4 = 0;
      CString::CString(local_1c);
      local_4._0_1_ = (CString)0x1;
      CString::CString((CString *)&local_10,&DAT_00490764);
      local_4._0_1_ = (CString)0x2;
      CString::CString(local_14,&DAT_0049075c);
      iVar3 = local_18;
      local_4 = CONCAT31(local_4._1_3_,3);
      iVar4 = *(int *)(*(int *)(local_18 + 0xb0) + 0x3c);
      if (*(int *)(piVar5 + 0x10) <= (int)*(short *)(iVar4 + 0xc)) {
        GetCharAbility((int)*(short *)(iVar4 + 10));
      }
      CString::Format(*(CString **)(piVar5 + 0x2c),(char *)local_20);
      thunk_FUN_00436b00(*(void **)(iVar3 + 0x354),local_1c);
      piVar13 = piVar6;
      if (piVar5[0x194] == (itemrecord)0x0) {
        uVar7 = 0xffffffff;
        pcVar11 = &DAT_00490724;
        do {
          pcVar12 = pcVar11;
          if (uVar7 == 0) break;
          uVar7 = uVar7 - 1;
          pcVar12 = pcVar11 + 1;
          cVar2 = *pcVar11;
          pcVar11 = pcVar12;
        } while (cVar2 != '\0');
        uVar7 = ~uVar7;
        piVar9 = (itemrecord *)(pcVar12 + -uVar7);
        for (uVar8 = uVar7 >> 2; uVar8 != 0; uVar8 = uVar8 - 1) {
          *(undefined4 *)piVar13 = *(undefined4 *)piVar9;
          piVar9 = piVar9 + 4;
          piVar13 = piVar13 + 4;
        }
      }
      else {
        uVar7 = 0xffffffff;
        piVar9 = piVar5 + 0x194;
        do {
          piVar10 = piVar9;
          if (uVar7 == 0) break;
          uVar7 = uVar7 - 1;
          piVar10 = piVar9 + 1;
          iVar1 = *piVar9;
          piVar9 = piVar10;
        } while (iVar1 != (itemrecord)0x0);
        uVar7 = ~uVar7;
        piVar9 = piVar10 + -uVar7;
        for (uVar8 = uVar7 >> 2; uVar8 != 0; uVar8 = uVar8 - 1) {
          *(undefined4 *)piVar13 = *(undefined4 *)piVar9;
          piVar9 = piVar9 + 4;
          piVar13 = piVar13 + 4;
        }
      }
      for (uVar7 = uVar7 & 3; uVar7 != 0; uVar7 = uVar7 - 1) {
        *piVar13 = *piVar9;
        piVar9 = piVar9 + 1;
        piVar13 = piVar13 + 1;
      }
      CString::Format(local_1c,(char *)local_1c);
      thunk_FUN_00436b00(*(void **)(local_10 + 0x354),(CString *)&local_18);
      this_00 = this;
      if (*(int *)(piVar5 + 0x34) != 0) {
        CString::Format(this,(char *)&local_18);
        CString::operator+=((CString *)&local_10,local_14);
        this_00 = extraout_ECX;
      }
      if (*(int *)(piVar5 + 0x38) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_00;
      }
      if (*(int *)(piVar5 + 0x3c) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_01;
      }
      if (*(int *)(piVar5 + 0x40) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_02;
      }
      if (*(int *)(piVar5 + 0x44) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_03;
      }
      if (*(int *)(piVar5 + 0x48) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_04;
      }
      if (*(int *)(piVar5 + 0x4c) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_05;
      }
      if (*(int *)(piVar5 + 0x50) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_06;
      }
      if (*(int *)(piVar5 + 0x54) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_07;
      }
      if (*(int *)(piVar5 + 0x58) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_08;
      }
      if (*(int *)(piVar5 + 0x5c) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_09;
      }
      if (*(int *)(piVar5 + 0x60) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_10;
      }
      if (*(int *)(piVar5 + 100) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_11;
      }
      if (*(int *)(piVar5 + 0x68) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_12;
      }
      if (*(int *)(piVar5 + 0x6c) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_13;
      }
      if (*(int *)(piVar5 + 0x74) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_14;
      }
      if (*(int *)(piVar5 + 0x78) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_15;
      }
      if (*(int *)(piVar5 + 0x7c) != 0) {
        CString::Format(local_14,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
        this_00 = extraout_ECX_16;
      }
      if (*(int *)(piVar5 + 0x80) != 0) {
        CString::Format(this_00,(char *)local_14);
        CString::operator+=((CString *)&local_c,(CString *)&local_10);
      }
      thunk_FUN_00436b00(*(void **)(local_10 + 0x354),(CString *)&local_10);
      operator_delete(piVar6);
      CString::~CString((CString *)&local_8);
      CString::~CString((CString *)&local_4);
      CString::~CString((CString *)&local_10);
      CString::~CString(local_14);
      ExceptionList = unaff_retaddr;
      return;
    }
  }
  ExceptionList = local_c;
  return;
}

