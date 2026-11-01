/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x1000c7d0; source SHA256 f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0
 */

/* Decode the5C50 message body (the caller skips the two opcode bytes), construct unitinfo, call
   UserUnit.resetcharacter. Whole-message placement offsets: unit2, job6, native tileX30,Y32. Other
   fields are preserved rather than assigned guessed meanings.
   Source SHA256: f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0 */

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
  undefined1 local_bc [168];
  CString local_14 [4];
  int local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
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
  CString::CString((CString *)(local_bc + 0xa4));
  local_4 = 0;
  CString::CString(local_14);
  local_bc._4_4_ = SEXT24(local_e4);
  local_bc._8_4_ = SEXT24(local_e2);
  local_bc._24_4_ = SEXT24(local_e0);
  iVar8 = (int)local_e8;
  local_bc._28_4_ = SEXT24(local_dc);
  local_bc._32_4_ = SEXT24(local_da);
  local_bc._44_4_ = SEXT24(local_d4);
  local_bc._48_4_ = SEXT24(local_d2);
  local_4 = 1;
  local_bc._12_4_ = 0;
  local_bc._16_4_ = 0;
  local_bc._20_4_ = 0;
  local_bc._36_4_ = 0;
  local_bc._40_4_ = 0;
  local_bc._52_4_ = SEXT24(local_d0);
  local_bc._76_4_ = SEXT24(local_ca);
  iVar7 = (int)local_cc;
  local_bc._84_4_ = local_c4;
  local_bc._88_4_ = SEXT24(local_d8);
  local_bc._96_4_ = SEXT24(local_e6);
  local_bc._80_4_ = local_c8;
  local_bc._56_4_ = 0;
  local_bc._92_4_ = SEXT24(local_d6);
  local_bc._60_4_ = 0;
  local_bc._68_4_ = 0;
  local_bc._64_4_ = 0x32;
  puVar3 = (undefined4 *)(local_bc + 100);
  for (iVar6 = 8; iVar6 != 0; iVar6 = iVar6 + -1) {
    *puVar3 = 0xffffffff;
    puVar3 = puVar3 + 1;
  }
  puVar3 = (undefined4 *)(local_bc + 0x84);
  for (iVar6 = 8; iVar6 != 0; iVar6 = iVar6 + -1) {
    *puVar3 = 0xffffffff;
    puVar3 = puVar3 + 1;
  }
  local_bc._0_4_ = iVar8;
  local_bc._72_4_ = iVar7;
  CString::Empty((CString *)(local_bc + 0xa4));
  CString::operator=(local_14,pcVar5);
  local_10 = (int)local_ce;
  resetcharacter((NativeUnitInfo32 *)local_bc);
  operator_delete(pcVar5);
  FUN_1000c680(iVar8,iVar7,(int)local_ca);
  local_4 = 2;
  CString::~CString(local_14);
  local_4 = 0xffffffff;
  CString::~CString((CString *)(local_bc + 0xa4));
  ExceptionList = local_c;
  return;
}

