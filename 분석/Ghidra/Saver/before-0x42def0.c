/* Ghidra decompilation; not recovered original source. Evidence: ev_0ed8b34329e2c472643016719ea0a4c628931ef632c1c9d89a1330af28cb80f6 */

undefined4 __thiscall FUN_0042def0(void *this,int param_1)

{
  int *piVar1;
  char cVar2;
  byte bVar3;
  short sVar4;
  short sVar5;
  int iVar6;
  undefined4 *puVar7;
  short *psVar8;
  undefined2 *puVar9;
  undefined4 *puVar10;
  char *pcVar11;
  byte *pbVar12;
  int iVar13;
  int iVar14;
  void *this_00;
  int local_18;
  undefined4 uStack_14;
  char *local_10;
  void *pvStack_c;
  undefined *puStack_8;
  undefined4 local_4;
  
  iVar13 = param_1;
  local_4 = 0xffffffff;
  puStack_8 = &DAT_0047d2ce;
  pvStack_c = ExceptionList;
  if (*(int *)((int)this + 300) == 0) {
    ExceptionList = &pvStack_c;
    local_10 = operator_new(0x54);
    local_4 = 1;
    if (local_10 != (char *)0x0) {
      puVar7 = thunk_FUN_004388f0((undefined4 *)local_10);
      goto LAB_0042df69;
    }
  }
  else {
    ExceptionList = &pvStack_c;
    local_10 = operator_new(0x54);
    local_4 = 0;
    if (local_10 != (char *)0x0) {
      iVar14 = *(int *)((int)this + 300);
      puVar7 = thunk_FUN_004389a0(local_10,iVar14,*(undefined4 *)(iVar14 + 4),
                                  *(undefined4 *)(iVar14 + 8));
      goto LAB_0042df69;
    }
  }
  puVar7 = (undefined4 *)0x0;
LAB_0042df69:
  local_18 = 0;
  local_4 = 0xffffffff;
  psVar8 = (short *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(short *)(puVar7 + 1) = *psVar8 + -1;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)((int)puVar7 + 6) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)(puVar7 + 2) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)((int)puVar7 + 10) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)(puVar7 + 3) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)((int)puVar7 + 0xe) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(param_1,&local_18);
  *(undefined2 *)(puVar7 + 4) = *puVar9;
  puVar10 = operator_new(0xff);
  pcVar11 = (char *)thunk_FUN_0043c420(param_1,&local_18);
  cVar2 = *pcVar11;
  *(char *)((int)puVar7 + 0x12) = cVar2;
  if (cVar2 != '\0') {
    thunk_FUN_0043c450(param_1,&local_18,puVar10,(uint)*(byte *)((int)puVar7 + 0x12));
    thunk_FUN_00438bf0(puVar7,puVar10);
  }
  pbVar12 = (byte *)thunk_FUN_0043c420(param_1,&local_18);
  bVar3 = *pbVar12;
  *(byte *)(puVar7 + 6) = bVar3;
  if (bVar3 != 0) {
    thunk_FUN_0043c450(param_1,&local_18,puVar10,(uint)bVar3);
    thunk_FUN_00438c60(puVar7,puVar10);
  }
  pbVar12 = (byte *)thunk_FUN_0043c420(param_1,&local_18);
  bVar3 = *pbVar12;
  *(byte *)(puVar7 + 8) = bVar3;
  if (bVar3 != 0) {
    thunk_FUN_0043c450(param_1,&local_18,puVar10,(uint)bVar3);
    thunk_FUN_00438cd0(puVar7,puVar10);
  }
  operator_delete(puVar10);
  puVar10 = puVar7 + 10;
  param_1 = 7;
  do {
    puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
    *(undefined2 *)puVar10 = *puVar9;
    puVar10 = (undefined4 *)((int)puVar10 + 2);
    param_1 = param_1 + -1;
  } while (param_1 != 0);
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)((int)puVar7 + 0x36) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)(puVar7 + 0xe) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)((int)puVar7 + 0x3a) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)(puVar7 + 0xf) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)((int)puVar7 + 0x3e) = *puVar9;
  puVar9 = (undefined2 *)thunk_FUN_0043c3f0(iVar13,&local_18);
  *(undefined2 *)(puVar7 + 0x10) = *puVar9;
  pbVar12 = (byte *)thunk_FUN_0043c420(iVar13,&local_18);
  bVar3 = *pbVar12;
  if (bVar3 == 0) {
    puVar7[0x11] = 0;
    CString::operator=((CString *)(puVar7 + 0x12),&DAT_0048ff24);
  }
  else {
    pcVar11 = operator_new(0xff);
    thunk_FUN_0043c450(iVar13,&local_18,(undefined4 *)pcVar11,(uint)bVar3);
    CString::operator=((CString *)(puVar7 + 0x12),pcVar11);
    operator_delete(pcVar11);
    puVar7[0x11] = 1;
  }
  iVar13 = thunk_FUN_00429db0(this,*(short *)((int)puVar7 + 6));
  if (iVar13 == -1) {
    CPtrArray::SetAtGrow((CPtrArray *)((int)this + 0xdc),*(int *)((int)this + 0xe4),puVar7);
  }
  else {
    piVar1 = (int *)(*(int *)((int)this + 0xe0) + iVar13 * 4);
    puVar10 = (undefined4 *)*piVar1;
    *piVar1 = (int)puVar7;
    if (puVar10 != (undefined4 *)0x0) {
      (**(code **)*puVar10)(1);
    }
  }
  iVar14 = thunk_FUN_00429db0(this,*(short *)((int)puVar7 + 6));
  iVar13 = *(int *)(*(int *)((int)this + 0xe0) + iVar14 * 4);
  CString::CString((CString *)&uStack_14);
  local_4 = 2;
  CString::Format((CString *)(int)*(short *)(iVar13 + 8),(char *)&uStack_14);
  dmKillChar((int)*(short *)(iVar13 + 6));
  dmSetChar(local_10);
  dmSetChar2((int)*(short *)(iVar13 + 6),(int)*(short *)(iVar13 + 0x36),
             (int)*(short *)(iVar13 + 0xc),(int)*(short *)(iVar13 + 0xe),
             (int)*(short *)(iVar13 + 0x10),(int)*(short *)(iVar13 + 0x38));
  if (*(short *)(iVar13 + 6) == *(short *)((int)this + 0xf0)) {
    this_00 = (void *)thunk_FUN_00475f50(*(void **)((int)this + 0x150),0x15);
    if (this_00 != (void *)0x0) {
      thunk_FUN_004586b0(this_00,0);
    }
    if (iVar14 != 0) {
      iVar6 = **(int **)((int)this + 0xe0);
      **(int **)((int)this + 0xe0) = iVar13;
      *(int *)(*(int *)((int)this + 0xe0) + iVar14 * 4) = iVar6;
    }
    sVar4 = *(short *)(iVar13 + 0xc);
    *(int *)((int)this + 0xf4) = (int)sVar4;
    sVar5 = *(short *)(iVar13 + 0xe);
    *(int *)((int)this + 0xf8) = (int)sVar5;
    *(int *)((int)this + 0x124) = sVar4 + -10;
    *(int *)((int)this + 0x128) = sVar5 + -7;
    thunk_FUN_0042f600();
    for (puVar7 = *(undefined4 **)((int)this + 0x1a8); puVar7 != (undefined4 *)0x0;
        puVar7 = (undefined4 *)*puVar7) {
    }
    *(undefined4 *)((int)this + 0x1b0) = 0;
    *(undefined4 *)((int)this + 0x1b4) = 0;
    *(undefined4 *)((int)this + 0x1ac) = 0;
    *(undefined4 *)((int)this + 0x1a8) = 0;
    CPlex::FreeDataChain(*(CPlex **)((int)this + 0x1b8));
    *(undefined4 *)((int)this + 0x1b8) = 0;
  }
  CString::~CString((CString *)&local_10);
  ExceptionList = puStack_8;
  return uStack_14;
}

