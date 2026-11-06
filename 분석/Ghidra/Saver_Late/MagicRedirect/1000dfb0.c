
void __cdecl FUN_1000dfb0(int param_1)

{
  short sVar1;
  short sVar2;
  short sVar3;
  short *psVar4;
  int iVar5;
  _PGSMAGIC *p_Var6;
  undefined4 *puVar7;
  int local_18;
  _PGSMAGIC local_14 [12];
  char local_8;
  void *local_4;
  
  local_18 = 0;
  psVar4 = (short *)FUN_1000c540(param_1,&local_18);
  sVar1 = *psVar4;
  psVar4 = (short *)FUN_1000c540(param_1,&local_18);
  sVar2 = *psVar4;
  psVar4 = (short *)FUN_1000c540(param_1,&local_18);
  sVar3 = *psVar4;
  psVar4 = (short *)FUN_1000c540(param_1,&local_18);
  iVar5 = CheckMagicTarget(local_14,(int)sVar1,(int)sVar2,(int)sVar3,(int)*psVar4);
  if (iVar5 != 0) {
    FUN_1000e4c0((short *)local_14,1);
    p_Var6 = local_14;
    puVar7 = (undefined4 *)&stack0xffffffc4;
    for (iVar5 = 5; iVar5 != 0; iVar5 = iVar5 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    Magic();
    if (local_8 != '\0') {
      operator_delete(local_4);
    }
  }
  return;
}


/* incoming refs
1000ece4 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
