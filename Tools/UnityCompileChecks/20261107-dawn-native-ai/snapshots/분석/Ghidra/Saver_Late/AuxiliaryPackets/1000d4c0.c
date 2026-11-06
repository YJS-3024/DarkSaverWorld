
void __cdecl FUN_1000d4c0(int param_1)

{
  short *psVar1;
  short *psVar2;
  int iVar3;
  short asStackY_c4 [36];
  undefined4 uStackY_7c;
  int local_5c;
  short local_58 [44];
  
  iVar3 = 0;
  local_5c = 0;
  psVar1 = (short *)FUN_1000c540(param_1,&local_5c);
  local_58[0] = *psVar1;
  if (0 < *psVar1) {
    psVar1 = local_58;
    do {
      psVar1 = psVar1 + 2;
      psVar2 = (short *)FUN_1000c540(param_1,&local_5c);
      psVar1[-1] = *psVar2;
      uStackY_7c = 0x1000d50c;
      psVar2 = (short *)FUN_1000c540(param_1,&local_5c);
      *psVar1 = *psVar2;
      iVar3 = iVar3 + 1;
    } while (iVar3 < local_58[0]);
  }
  psVar1 = local_58;
  psVar2 = asStackY_c4;
  for (iVar3 = 0x15; iVar3 != 0; iVar3 = iVar3 + -1) {
    *(undefined4 *)psVar2 = *(undefined4 *)psVar1;
    psVar1 = psVar1 + 2;
    psVar2 = psVar2 + 2;
  }
  *psVar2 = *psVar1;
  SetAllyInfo();
  return;
}


/* incoming refs
1000ebac UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
