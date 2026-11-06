
void __cdecl FUN_1000d8f0(int param_1)

{
  byte bVar1;
  byte *pbVar2;
  byte *pbVar3;
  int iVar4;
  byte *pbVar5;
  bool bVar6;
  int local_14;
  undefined1 *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  iVar4 = param_1;
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015488;
  local_c = ExceptionList;
  local_14 = 0;
  ExceptionList = &local_c;
  FUN_1000c560(param_1,&local_14);
  pbVar2 = (byte *)FUN_1000c560(iVar4,&local_14);
  bVar1 = *pbVar2;
  local_10 = (undefined1 *)CONCAT31(local_10._1_3_,bVar1);
  pbVar3 = operator_new(bVar1 + 1);
  FUN_1000c580(iVar4,&local_14,(undefined4 *)pbVar3,(uint)bVar1);
  pbVar2 = DAT_1006a73c;
  pbVar5 = pbVar3;
  do {
    bVar1 = *pbVar2;
    bVar6 = bVar1 < *pbVar5;
    if (bVar1 != *pbVar5) {
LAB_1000d983:
      iVar4 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
      goto LAB_1000d988;
    }
    if (bVar1 == 0) break;
    bVar1 = pbVar2[1];
    bVar6 = bVar1 < pbVar5[1];
    if (bVar1 != pbVar5[1]) goto LAB_1000d983;
    pbVar2 = pbVar2 + 2;
    pbVar5 = pbVar5 + 2;
  } while (bVar1 != 0);
  iVar4 = 0;
LAB_1000d988:
  if (iVar4 != 0) {
    CString::CString((CString *)&param_1);
    local_4 = 0;
    CString::operator=((CString *)&param_1,(char *)pbVar3);
    CString::operator+=((CString *)&param_1,&DAT_1001a4bc);
    local_10 = &stack0xffffffdc;
    CString::CString((CString *)&stack0xffffffdc,(CString *)&param_1);
    FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  operator_delete(pbVar3);
  ExceptionList = local_c;
  return;
}


/* incoming refs
1000ec55 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
