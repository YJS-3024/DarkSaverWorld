
void __cdecl FUN_1000d8a0(int param_1)

{
  undefined2 uVar1;
  undefined2 *puVar2;
  int local_4;
  
  local_4 = 0;
  puVar2 = (undefined2 *)FUN_1000c540(param_1,&local_4);
  uVar1 = *puVar2;
  puVar2 = (undefined2 *)FUN_1000c540(param_1,&local_4);
  param_1 = CONCAT22(*puVar2,uVar1);
  FUN_1000f070(param_1);
  return;
}


/* incoming refs
1000ec48 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
