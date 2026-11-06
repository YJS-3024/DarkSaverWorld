
void __cdecl FUN_1000dcb0(int param_1)

{
  short sVar1;
  short *psVar2;
  ulong *puVar3;
  int local_4;
  
  local_4 = 0;
  psVar2 = (short *)FUN_1000c540(param_1,&local_4);
  sVar1 = *psVar2;
  puVar3 = (ulong *)FUN_1000c5c0(param_1,&local_4);
  MagicFlag((int)sVar1,*puVar3);
  return;
}


/* incoming refs
1000ec7c UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
