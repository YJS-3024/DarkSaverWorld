/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x1000c7d0
 * Evidence: ev_9b88e8f6beb0e79caf19f544ac8ba6391849d0556a1e3f6771280eebf9d43a7d
 */

void __cdecl DecodeUnitSnapshot_5C50(int param_1)

{
  byte bVar1;
  short *psVar2;
  undefined4 *puVar3;
  byte *pbVar4;
  char *pcVar5;
  int iVar6;
  int iVar7;
  int iVar8;
  int local_ec;
  short local_e8;
  short local_e6;
  short local_e4;
  short local_e2;
  short local_e0;
  short local_dc;
  short local_da;
  short local_d8;
  short local_d6;
  short local_d4;
  short local_d2;
  short local_d0;
  short local_ce;
  short local_cc;
  short local_ca;
  undefined4 local_c8;
  undefined4 local_c4;
  byte local_c0;
  undefined3 uStack_bf;
  int local_bc;
  int local_b8;
  int local_b4;
  undefined4 local_b0;
  undefined4 local_ac;
  undefined4 local_a8;
  int local_a4;
  int local_a0;
  int local_9c;
  undefined4 local_98;
  undefined4 local_94;
  int local_90;
  int local_8c;
  int local_88;
  undefined4 local_84;
  undefined4 local_80;
  undefined4 local_7c;
  undefined4 local_78;
  int local_74;
  int local_70;
  undefined4 local_6c;
  undefined4 local_68;
  int local_64;
  int local_60;
  int local_5c;
  undefined4 local_58 [8];
  undefined4 local_38 [8];
  CString local_18 [4];
  CString local_14 [4];
  int local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* Decode the5C50 message body (the caller skips the two opcode bytes),
                       construct unitinfo, call UserUnit.resetcharacter. Whole-message placement
                       offsets: unit2, job6, native tileX30,Y32. Other fields are preserved rather
                       than assigned guessed meanings. */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_1001541b;
  local_c = ExceptionList;
  local_ec = 0;
  ExceptionList = &local_c;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_e8 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_e6 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_e4 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_e2 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_e0 = *psVar2;
  FUN_1000c540(param_1,&local_ec);
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_dc = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_da = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_d8 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_d6 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_d4 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_d2 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_d0 = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_ce = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_cc = *psVar2;
  psVar2 = (short *)FUN_1000c540(param_1,&local_ec);
  local_ca = *psVar2;
  puVar3 = (undefined4 *)FUN_1000c5c0(param_1,&local_ec);
  local_c8 = *puVar3;
  puVar3 = (undefined4 *)FUN_1000c5c0(param_1,&local_ec);
  local_c4 = *puVar3;
  pbVar4 = (byte *)FUN_1000c560(param_1,&local_ec);
  bVar1 = *pbVar4;
  _local_c0 = CONCAT31(uStack_bf,bVar1);
  pcVar5 = operator_new(bVar1 + 1);
  FUN_1000c580(param_1,&local_ec,(undefined4 *)pcVar5,(uint)bVar1);
  CString::CString(local_18);
  local_4 = 0;
  CString::CString(local_14);
  local_b8 = (int)local_e4;
  local_b4 = (int)local_e2;
  local_a4 = (int)local_e0;
  iVar8 = (int)local_e8;
  local_a0 = (int)local_dc;
  local_9c = (int)local_da;
  local_90 = (int)local_d4;
  local_8c = (int)local_d2;
  local_4 = 1;
  local_b0 = 0;
  local_ac = 0;
  local_a8 = 0;
  local_98 = 0;
  local_94 = 0;
  local_88 = (int)local_d0;
  local_70 = (int)local_ca;
  iVar7 = (int)local_cc;
  local_68 = local_c4;
  local_64 = (int)local_d8;
  local_5c = (int)local_e6;
  local_6c = local_c8;
  local_84 = 0;
  local_60 = (int)local_d6;
  local_80 = 0;
  local_78 = 0;
  local_7c = 0x32;
  puVar3 = local_58;
  for (iVar6 = 8; iVar6 != 0; iVar6 = iVar6 + -1) {
    *puVar3 = 0xffffffff;
    puVar3 = puVar3 + 1;
  }
  puVar3 = local_38;
  for (iVar6 = 8; iVar6 != 0; iVar6 = iVar6 + -1) {
    *puVar3 = 0xffffffff;
    puVar3 = puVar3 + 1;
  }
  local_bc = iVar8;
  local_74 = iVar7;
  CString::Empty(local_18);
  CString::operator=(local_14,pcVar5);
  local_10 = (int)local_ce;
  resetcharacter((unitinfo *)&local_bc);
  operator_delete(pcVar5);
  FUN_1000c680(iVar8,iVar7,(int)local_ca);
  local_4 = 2;
  CString::~CString(local_14);
  local_4 = 0xffffffff;
  CString::~CString(local_18);
  ExceptionList = local_c;
  return;
}

