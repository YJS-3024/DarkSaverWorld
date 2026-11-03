
void FUN_00415ba0(undefined **param_1,undefined4 param_2,undefined4 param_3,undefined4 param_4,
                 undefined4 param_5,undefined4 param_6)

{
  int iVar1;
  bool bVar2;
  char *pcVar3;
  undefined2 *puVar4;
  int *piVar5;
  uint uVar6;
  undefined3 extraout_var;
  undefined3 extraout_var_00;
  undefined2 extraout_var_01;
  uint uVar7;
  CFile *this;
  undefined2 extraout_var_02;
  undefined4 *puVar8;
  undefined4 *puVar9;
  int unaff_retaddr;
  undefined **in_stack_0000002c;
  undefined4 in_stack_00000034;
  undefined4 in_stack_00000038;
  void *in_stack_00002054;
  void *in_stack_00002064;
  void *local_c;
  undefined1 *local_8;
  int local_4;
  
  local_4 = -1;
  local_8 = &LAB_0047be8a;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  FUN_00479c00();
  local_4 = 1;
  pcVar3 = (char *)thunk_FUN_0043c420((int)in_stack_00002064,&local_4);
  if (*pcVar3 == '\x13') {
    thunk_FUN_0043b5b0(0x4950f8);
    puVar4 = (undefined2 *)thunk_FUN_0043c3f0((int)in_stack_00002064,&local_4);
    local_c = (void *)CONCAT22(extraout_var_02,*puVar4);
    puVar4 = (undefined2 *)thunk_FUN_0043c3f0((int)in_stack_00002064,&local_4);
    local_8 = (undefined1 *)CONCAT22(extraout_var_01,*puVar4);
    piVar5 = (int *)thunk_FUN_0043c4f0((int)in_stack_00002064,&local_4);
    iVar1 = *piVar5;
    uVar6 = (uint)local_c & 0xffff;
    puVar8 = (undefined4 *)(local_4 + (int)in_stack_00002064);
    puVar9 = (undefined4 *)&stack0x00000050;
    for (uVar7 = uVar6 >> 2; uVar7 != 0; uVar7 = uVar7 - 1) {
      *puVar9 = *puVar8;
      puVar8 = puVar8 + 1;
      puVar9 = puVar9 + 1;
    }
    local_4 = local_4 + uVar6;
    for (uVar7 = (uint)local_c & 3; uVar7 != 0; uVar7 = uVar7 - 1) {
      *(undefined1 *)puVar9 = *(undefined1 *)puVar8;
      puVar8 = (undefined4 *)((int)puVar8 + 1);
      puVar9 = (undefined4 *)((int)puVar9 + 1);
    }
    thunk_FUN_0043b6d0(&DAT_004950f8,uVar6,(uint)local_8 & 0xffff);
    puVar8 = (undefined4 *)&stack0x00000050;
    puVar9 = DAT_00495114;
    for (uVar6 = ((uint)local_c & 0xffff) >> 2; uVar6 != 0; uVar6 = uVar6 - 1) {
      *puVar9 = *puVar8;
      puVar8 = puVar8 + 1;
      puVar9 = puVar9 + 1;
    }
    for (uVar6 = (uint)local_c & 3; uVar6 != 0; uVar6 = uVar6 - 1) {
      *(undefined1 *)puVar9 = *(undefined1 *)puVar8;
      puVar8 = (undefined4 *)((int)puVar8 + 1);
      puVar9 = (undefined4 *)((int)puVar9 + 1);
    }
    bVar2 = thunk_FUN_0043b7b0(0x4950f8);
    pcVar3 = DAT_00495118;
    if ((((CONCAT31(extraout_var,bVar2) == 0) || (DAT_00495130 != 0)) || (DAT_0049510c != iVar1)) ||
       (((uint)local_8 & 0xffff) != DAT_00495128)) {
      thunk_FUN_0043b7f0(0x4950f8);
      operator_delete(in_stack_00002064);
      ExceptionList = in_stack_00002054;
      return;
    }
    DAT_00495118[(uint)local_8 & 0xffff] = '\0';
    thunk_FUN_004161e0(pcVar3);
    CFile::CFile((CFile *)&stack0x00000040);
    CException::CException((CException *)&stack0x0000002c);
    CString::CString((CString *)&stack0x0000003c);
    in_stack_0000002c = &PTR_LAB_0048377c;
    in_stack_00000034 = 0;
    in_stack_00000038 = 0xffffffff;
    CString::operator=((CString *)&stack0x0000003c,(char *)0x0);
    CFile::Open((CFile *)&stack0x00000040,s_Candid_Dat_0048f178,0x9001,
                (CFileException *)&stack0x0000002c);
    CFile::Write((CFile *)&stack0x00000040,&local_c,2);
    CFile::Write((CFile *)&stack0x00000040,&local_8,2);
    CFile::Write((CFile *)&stack0x00000040,&stack0x00000000,4);
    CFile::Write((CFile *)&stack0x00000040,&stack0x00000050,(uint)local_c & 0xffff);
    CFile::Close((CFile *)&stack0x00000040);
    in_stack_0000002c = &PTR_LAB_0048377c;
    CString::~CString((CString *)&stack0x0000003c);
    in_stack_0000002c = &PTR_LAB_004830f0;
    this = (CFile *)&stack0x00000040;
  }
  else {
    if (*pcVar3 != '\x12') {
      ExceptionList = in_stack_00002054;
      return;
    }
    CFile::CFile((CFile *)&stack0x0000001c);
    CException::CException((CException *)&param_1);
    CString::CString((CString *)&param_5);
    param_1 = &PTR_LAB_0048377c;
    param_3 = 0;
    param_4 = 0xffffffff;
    CString::operator=((CString *)&param_5,(char *)0x0);
    CFile::Open((CFile *)&stack0x0000001c,s_Candid_Dat_0048f178,0x8000,(CFileException *)&param_1);
    thunk_FUN_0043b5b0(0x4950f8);
    CFile::Read((CFile *)&stack0x0000001c,&local_c,2);
    CFile::Read((CFile *)&stack0x0000001c,&local_8,2);
    CFile::Read((CFile *)&stack0x0000001c,&stack0x00000000,4);
    CFile::Read((CFile *)&stack0x0000001c,&stack0x00000050,(uint)local_c & 0xffff);
    CFile::Close((CFile *)&stack0x0000001c);
    thunk_FUN_0043b6d0(&DAT_004950f8,(uint)local_c & 0xffff,(uint)local_8 & 0xffff);
    puVar8 = (undefined4 *)&stack0x00000050;
    puVar9 = DAT_00495114;
    for (uVar6 = ((uint)local_c & 0xffff) >> 2; uVar6 != 0; uVar6 = uVar6 - 1) {
      *puVar9 = *puVar8;
      puVar8 = puVar8 + 1;
      puVar9 = puVar9 + 1;
    }
    for (uVar6 = (uint)local_c & 3; uVar6 != 0; uVar6 = uVar6 - 1) {
      *(undefined1 *)puVar9 = *(undefined1 *)puVar8;
      puVar8 = (undefined4 *)((int)puVar8 + 1);
      puVar9 = (undefined4 *)((int)puVar9 + 1);
    }
    bVar2 = thunk_FUN_0043b7b0(0x4950f8);
    pcVar3 = DAT_00495118;
    if (CONCAT31(extraout_var_00,bVar2) == 0) {
      thunk_FUN_0043b7f0(0x4950f8);
    }
    else if (DAT_00495130 == 0) {
      if (DAT_0049510c == unaff_retaddr) {
        if (((uint)local_8 & 0xffff) == DAT_00495128) {
          DAT_00495118[(uint)local_8 & 0xffff] = '\0';
          thunk_FUN_004161e0(pcVar3);
        }
        else {
          thunk_FUN_0043b7f0(0x4950f8);
        }
      }
      else {
        thunk_FUN_0043b7f0(0x4950f8);
      }
    }
    else {
      thunk_FUN_0043b7f0(0x4950f8);
    }
    param_1 = &PTR_LAB_0048377c;
    CString::~CString((CString *)&param_5);
    param_1 = &PTR_LAB_004830f0;
    this = (CFile *)&stack0x0000001c;
  }
  CFile::~CFile(this);
  ExceptionList = in_stack_00002054;
  return;
}

