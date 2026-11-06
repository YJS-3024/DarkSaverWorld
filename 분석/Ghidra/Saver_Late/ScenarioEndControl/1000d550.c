
void __cdecl FUN_1000d550(int param_1)

{
  short *psVar1;
  undefined2 *puVar2;
  byte *pbVar3;
  int iVar4;
  char *pcVar5;
  undefined4 *puVar6;
  int local_37c;
  byte local_378;
  undefined3 uStack_377;
  short local_374 [2];
  char local_370 [880];
  
  iVar4 = 0;
  local_37c = 0;
  psVar1 = (short *)FUN_1000c540(param_1,&local_37c);
  local_374[0] = *psVar1;
  if (0 < *psVar1) {
    pcVar5 = local_370;
    do {
      puVar2 = (undefined2 *)FUN_1000c540(param_1,&local_37c);
      *(undefined2 *)(pcVar5 + -2) = *puVar2;
      pbVar3 = (byte *)FUN_1000c560(param_1,&local_37c);
      _local_378 = CONCAT31(uStack_377,*pbVar3);
      FUN_1000c580(param_1,&local_37c,(undefined4 *)pcVar5,(uint)*pbVar3);
      if (*(short *)(pcVar5 + -2) == DAT_1006a738) {
        CString::operator=((CString *)&DAT_1006a73c,pcVar5);
      }
      iVar4 = iVar4 + 1;
      pcVar5 = pcVar5 + 0x2a;
    } while (iVar4 < local_374[0]);
  }
  psVar1 = local_374;
  puVar6 = (undefined4 *)&DAT_1006bd10;
  for (iVar4 = 0xdd; iVar4 != 0; iVar4 = iVar4 + -1) {
    *puVar6 = *(undefined4 *)psVar1;
    psVar1 = psVar1 + 2;
    puVar6 = puVar6 + 1;
  }
  return;
}


/* incoming refs
1000ebb9 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
