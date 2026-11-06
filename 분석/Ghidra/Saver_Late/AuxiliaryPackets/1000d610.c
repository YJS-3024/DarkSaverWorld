
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void __cdecl FUN_1000d610(int param_1)

{
  short *psVar1;
  int local_4;
  
  local_4 = 0;
  psVar1 = (short *)FUN_1000c540(param_1,&local_4);
  _DAT_1001a4b8 = (int)*psVar1;
  return;
}


/* incoming refs
1000ebc6 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
