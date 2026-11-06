
undefined4 __cdecl FUN_100040a0(undefined2 *param_1,int param_2)

{
  int iVar1;
  uint uVar2;
  uint uVar3;
  undefined4 uVar4;
  uint uVar5;
  uint uVar6;
  
  iVar1 = param_2 / 10;
  if (((g_RuntimeCharacters[iVar1 * 10].active == 0) ||
      (*(int *)&g_RuntimeCharacters[iVar1 * 10].field_0x4 == 0)) ||
     (uVar2 = g_RuntimeCharacters[param_2].tileY - g_RuntimeCharacters[iVar1 * 10].tileY,
     uVar5 = (int)uVar2 >> 0x1f,
     uVar3 = g_RuntimeCharacters[param_2].tileX - g_RuntimeCharacters[iVar1 * 10].tileX,
     uVar6 = (int)uVar3 >> 0x1f,
     (int)(((uVar2 ^ uVar5) - uVar5) + ((uVar3 ^ uVar6) - uVar6)) <=
     *(int *)&g_RuntimeCharacters[iVar1 * 10].field_0x4c)) {
    uVar4 = 0;
    if (((g_RuntimeCharacters[param_2].active != 0) &&
        (uVar2 = *(uint *)&g_RuntimeCharacters[param_2].field_0x100, (uVar2 & 0x10) != 0)) &&
       ((uVar2 & 1) == 0)) {
      if (((g_RuntimeCharacters[iVar1 * 10].active == 0) ||
          (*(int *)&g_RuntimeCharacters[iVar1 * 10].field_0x4 == 0)) ||
         (uVar2 = g_RuntimeCharacters[iVar1 * 10].aiState, (uVar2 & 4) != 0)) {
        uVar4 = FUN_10003dd0(param_1,param_2,2);
      }
      else {
        if (((uVar2 & 2) != 0) || ((3 < param_2 / 100 && (param_2 / 100 < 8)))) {
          uVar4 = FUN_10003dd0(param_1,param_2,1);
          return uVar4;
        }
        if ((uVar2 & 8) != 0) {
          uVar4 = FUN_10003dd0(param_1,param_2,3);
          return uVar4;
        }
      }
      return uVar4;
    }
  }
  return 0;
}


/* incoming refs
10003682 UNCONDITIONAL_CALL
CALLER 100035e0 FUN_100035e0
  10004282 UNCONDITIONAL_CALL
*/
