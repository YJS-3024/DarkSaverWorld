
void __cdecl FUN_1000db20(int param_1)

{
  byte bVar1;
  short *psVar2;
  undefined2 *puVar3;
  byte *pbVar4;
  undefined2 *puVar5;
  undefined2 *puVar6;
  int iVar7;
  uint uVar8;
  undefined4 *puVar9;
  int local_1bc;
  CString local_1b8 [4];
  short local_1b4;
  undefined2 local_1b2;
  undefined2 local_1b0;
  undefined2 local_1ae;
  byte local_1a8;
  undefined3 uStack_1a7;
  undefined2 *local_1a4;
  undefined1 local_1a0 [404];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_100154c1;
  local_c = ExceptionList;
  local_1bc = 0;
  ExceptionList = &local_c;
  CString::CString(local_1b8);
  local_4 = 0;
  FUN_100149de(local_1a0,4,0x65,CString::CString);
  local_4._0_1_ = 1;
  psVar2 = (short *)FUN_1000c540(param_1,&local_1bc);
  local_1b4 = *psVar2;
  puVar3 = (undefined2 *)FUN_1000c540(param_1,&local_1bc);
  local_1b2 = *puVar3;
  puVar3 = (undefined2 *)FUN_1000c540(param_1,&local_1bc);
  local_1b0 = *puVar3;
  puVar3 = (undefined2 *)FUN_1000c540(param_1,&local_1bc);
  local_1ae = *puVar3;
  pbVar4 = (byte *)FUN_1000c560(param_1,&local_1bc);
  bVar1 = *pbVar4;
  _local_1a8 = CONCAT31(uStack_1a7,bVar1);
  if (bVar1 != 0) {
    uVar8 = (uint)bVar1;
    puVar5 = operator_new(uVar8 * 2);
    local_1a4 = puVar5;
    puVar3 = puVar5;
    if (bVar1 != 0) {
      do {
        puVar6 = (undefined2 *)FUN_1000c540(param_1,&local_1bc);
        *puVar3 = *puVar6;
        uVar8 = uVar8 - 1;
        puVar3 = puVar3 + 1;
      } while (uVar8 != 0);
    }
    if (-1 < local_1b4) {
      psVar2 = &local_1b4;
      puVar9 = (undefined4 *)&stack0xfffffe20;
      for (iVar7 = 5; iVar7 != 0; iVar7 = iVar7 + -1) {
        *puVar9 = *(undefined4 *)psVar2;
        psVar2 = psVar2 + 2;
        puVar9 = puVar9 + 1;
      }
      Magic();
    }
    operator_delete(puVar5);
  }
  local_4 = (uint)local_4._1_3_ << 8;
  FUN_1001489a(local_1a0,4,0x65,CString::~CString);
  local_4 = 0xffffffff;
  CString::~CString(local_1b8);
  ExceptionList = local_c;
  return;
}


/* incoming refs
1000ec6f UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
