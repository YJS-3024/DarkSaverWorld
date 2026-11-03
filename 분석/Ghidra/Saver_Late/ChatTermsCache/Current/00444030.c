
void __cdecl FUN_00444030(uint param_1,undefined4 param_2,undefined4 param_3)

{
  undefined4 uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  bool bVar4;
  char *pcVar5;
  int iVar6;
  ulong uVar7;
  undefined3 extraout_var;
  int extraout_ECX;
  uint uVar8;
  undefined4 *puVar9;
  undefined4 *puVar10;
  undefined1 uVar11;
  undefined **ppuStack00000020;
  undefined4 uStack00000028;
  undefined4 uStack0000002c;
  int in_stack_00000034;
  void *in_stack_0000203c;
  char *pcVar12;
  void *local_c;
  undefined4 *local_8;
  uint local_4;
  
  local_4 = 0xffffffff;
  local_8 = (undefined4 *)&LAB_0047e1d2;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  FUN_00479c00();
  pcVar5 = operator_new(100);
  local_c = (void *)0x0;
  CFile::CFile((CFile *)&stack0x00000010);
  CException::CException((CException *)&stack0x00000020);
  CString::CString((CString *)&stack0x00000030);
  ppuStack00000020 = &PTR_LAB_0048377c;
  uStack00000028 = 0;
  uStack0000002c = 0xffffffff;
  CString::operator=((CString *)&stack0x00000030,(char *)0x0);
  CString::CString((CString *)&local_8);
  iVar6 = CFile::Open((CFile *)&stack0x00000010,s_Candid_Dat_0048f178,0x8000,
                      (CFileException *)&stack0x00000020);
  if (iVar6 == 0) {
    pcVar12 = s_V0_00_00490418;
LAB_00444276:
    CString::operator=((CString *)&local_8,pcVar12);
    uVar11 = 5;
  }
  else {
    thunk_FUN_0043b5b0(0x4950f8);
    uVar7 = CFile::GetLength((CFile *)&stack0x00000010);
    if (8 < (int)uVar7) {
      CFile::Read((CFile *)&stack0x00000010,&local_4,2);
      if (uVar7 - 8 == (local_4 & 0xffff)) {
        CFile::Read((CFile *)&stack0x00000010,&param_1,2);
        if ((ushort)param_1 < 0x2001) {
          CFile::Read((CFile *)&stack0x00000010,&stack0x00000034,4);
          CFile::Read((CFile *)&stack0x00000010,&stack0x00000038,local_4 & 0xffff);
          CFile::Close((CFile *)&stack0x00000010);
          thunk_FUN_0043b6d0(&DAT_004950f8,local_4 & 0xffff,param_1 & 0xffff);
          puVar9 = (undefined4 *)&stack0x00000038;
          puVar10 = DAT_00495114;
          for (uVar8 = (local_4 & 0xffff) >> 2; uVar8 != 0; uVar8 = uVar8 - 1) {
            *puVar10 = *puVar9;
            puVar9 = puVar9 + 1;
            puVar10 = puVar10 + 1;
          }
          for (uVar8 = local_4 & 3; uVar8 != 0; uVar8 = uVar8 - 1) {
            *(undefined1 *)puVar10 = *(undefined1 *)puVar9;
            puVar9 = (undefined4 *)((int)puVar9 + 1);
            puVar10 = (undefined4 *)((int)puVar10 + 1);
          }
          bVar4 = thunk_FUN_0043b7b0(0x4950f8);
          uVar2 = param_3;
          if (CONCAT31(extraout_var,bVar4) == 0) {
            thunk_FUN_0043b7f0(0x4950f8);
          }
          else if (DAT_00495130 == 0) {
            if (DAT_0049510c == in_stack_00000034) {
              if ((param_1 & 0xffff) == DAT_00495128) {
                uVar1 = *DAT_00495118;
                param_3 = CONCAT31(param_3._1_3_,*(undefined1 *)(DAT_00495118 + 1));
                uVar3 = param_3;
                param_2 = uVar1;
                if (((char)uVar1 == 'V') &&
                   (param_2._2_1_ = (char)((uint)uVar1 >> 0x10), bVar4 = param_2._2_1_ == '.', bVar4
                   )) {
                  pcVar12 = (char *)&param_2;
                  param_3._3_1_ = SUB41(uVar2,3);
                  param_3._0_3_ = (uint3)(ushort)uVar3;
                  goto LAB_00444276;
                }
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
        }
      }
    }
    CString::operator=((CString *)&local_8,s_V0_00_00490418);
    uVar11 = (undefined1)local_8[-2];
  }
  thunk_FUN_0043b9a0((int)pcVar5,(int *)&local_c,0x21);
  uVar8 = *(uint *)(**(int **)(extraout_ECX + 0x104) + -8);
  thunk_FUN_0043b9a0((int)pcVar5,(int *)&local_c,(char)uVar8);
  thunk_FUN_0043b9d0((int)pcVar5,(int *)&local_c,
                     (undefined4 *)**(undefined4 **)(extraout_ECX + 0x104),uVar8);
  uVar8 = *(uint *)(**(int **)(extraout_ECX + 0x108) + -8);
  thunk_FUN_0043b9a0((int)pcVar5,(int *)&local_c,(char)uVar8);
  thunk_FUN_0043b9d0((int)pcVar5,(int *)&local_c,
                     (undefined4 *)**(undefined4 **)(extraout_ECX + 0x108),uVar8);
  thunk_FUN_0043b970((int)pcVar5,(int *)&local_c,**(undefined2 **)(extraout_ECX + 0x114));
  thunk_FUN_0043b9a0((int)pcVar5,(int *)&local_c,uVar11);
  thunk_FUN_0043b9d0((int)pcVar5,(int *)&local_c,local_8,local_8[-2]);
  thunk_FUN_004132d0(DAT_004952a0,(int)local_c,pcVar5);
  if (pcVar5 != (char *)0x0) {
    operator_delete(pcVar5);
  }
  CString::~CString((CString *)&local_8);
  ppuStack00000020 = &PTR_LAB_0048377c;
  CString::~CString((CString *)&stack0x00000030);
  ppuStack00000020 = &PTR_LAB_004830f0;
  CFile::~CFile((CFile *)&stack0x00000010);
  ExceptionList = in_stack_0000203c;
  return;
}

