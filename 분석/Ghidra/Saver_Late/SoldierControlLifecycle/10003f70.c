
undefined4 __cdecl FUN_10003f70(int param_1)

{
  uint uVar1;
  int iVar2;
  undefined2 local_8 [4];
  
  iVar2 = param_1 / 10;
  if (((g_RuntimeCharacters[param_1].active == 0) ||
      ((*(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0x10) == 0)) ||
     ((*(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 1) != 0)) {
    return 0xffffffff;
  }
  if (((g_RuntimeCharacters[iVar2 * 10].active == 0) ||
      (*(int *)&g_RuntimeCharacters[iVar2 * 10].field_0x4 == 0)) ||
     (uVar1 = g_RuntimeCharacters[iVar2 * 10].aiState, (uVar1 & 4) != 0)) {
    iVar2 = 2;
  }
  else if (((uVar1 & 2) == 0) && ((param_1 / 100 < 4 || (7 < param_1 / 100)))) {
    if ((uVar1 & 8) == 0) {
      return 0xfffffffd;
    }
    iVar2 = 3;
  }
  else {
    iVar2 = 1;
  }
  iVar2 = FUN_10003dd0(local_8,param_1,iVar2);
  if (iVar2 == 0) {
    return 0xfffffffd;
  }
  if (iVar2 == 1) {
    FUN_10005240(local_8);
  }
  else {
    FUN_100051e0((short)param_1);
  }
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x60 = 0;
  *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
       *(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0xffffffef;
  return 0xfffffffe;
}


/* incoming refs
1000423d UNCONDITIONAL_CALL
1000429a UNCONDITIONAL_CALL
100036be UNCONDITIONAL_CALL
1000384a UNCONDITIONAL_CALL
CALLER 100041f0 FUN_100041f0
  10002465 UNCONDITIONAL_CALL
CALLER 100035e0 FUN_100035e0
  10004282 UNCONDITIONAL_CALL
*/
