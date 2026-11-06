
undefined4 __cdecl FUN_10003860(undefined2 *param_1,int param_2,int *param_3,int param_4)

{
  int iVar1;
  int *piVar2;
  int iVar3;
  int iVar4;
  int *piVar5;
  int *local_22c;
  int local_228;
  int local_224;
  undefined2 local_220 [2];
  undefined2 local_21c [2];
  int local_218 [134];
  
  *param_3 = -1;
  ClearTa(&DAT_100646a0);
  SetArea(g_RuntimeCharacters[param_2].tileX,g_RuntimeCharacters[param_2].tileY,param_4,
          &DAT_100646a0);
  iVar1 = DAT_100e4298;
  iVar4 = 0;
  local_224 = 0;
  (&DAT_100646a0)[g_RuntimeCharacters[param_2].tileY + g_RuntimeCharacters[param_2].tileX * 200] =
       0xffffffff;
  local_228 = 0;
  if (0 < iVar1) {
    local_22c = &DAT_100646a0;
    iVar1 = DAT_100e429c;
    do {
      iVar3 = 0;
      if (0 < iVar1) {
        piVar2 = local_218 + iVar4;
        piVar5 = local_22c;
        do {
          iVar1 = mainmapgetchar(local_228,iVar3);
          if (((*piVar5 < 1) || (iVar1 < 0)) || (iVar4 = ally3(iVar1,param_2), iVar4 != 0)) {
            *piVar5 = -1;
          }
          else if (((g_RuntimeCharacters[iVar1].active != 0) &&
                   (*(int *)&g_RuntimeCharacters[iVar1].field_0x4 != 0)) &&
                  ((g_RuntimeCharacters[iVar1].field_0x100 & 0x80) == 0)) {
            iVar4 = *(int *)&g_RuntimeCharacters[iVar1].field_0x20;
            local_224 = local_224 + 1;
            *piVar2 = iVar1;
            *piVar5 = iVar4;
            piVar2 = piVar2 + 1;
          }
          iVar3 = iVar3 + 1;
          piVar5 = piVar5 + 1;
          iVar1 = DAT_100e429c;
          iVar4 = local_224;
        } while (iVar3 < DAT_100e429c);
      }
      local_228 = local_228 + 1;
      local_22c = local_22c + 200;
    } while (local_228 < DAT_100e4298);
    if (iVar4 != 0) {
      iVar1 = rand();
      iVar1 = local_218[iVar1 % iVar4];
      *param_3 = iVar1;
      FUN_10002260(param_2,iVar1,(int *)local_220,(int *)local_21c);
      *param_1 = (short)param_2;
      param_1[1] = (short)*param_3;
      param_1[2] = local_220[0];
      param_1[3] = local_21c[0];
      return 1;
    }
  }
  return 0;
}


/* incoming refs
10003144 UNCONDITIONAL_CALL
10003ef6 UNCONDITIONAL_CALL
10004483 UNCONDITIONAL_CALL
1000455f UNCONDITIONAL_CALL
CALLER 100042e0 CommanderAIAction
  10014014 DATA
  Entry Point EXTERNAL
  100049ed UNCONDITIONAL_CALL
  10004920 UNCONDITIONAL_CALL
CALLER 100030b0 MoveAttack
  100140d4 DATA
  Entry Point EXTERNAL
  100035a5 UNCONDITIONAL_CALL
CALLER 10003dd0 FUN_10003dd0
  1000402c UNCONDITIONAL_CALL
  10004198 UNCONDITIONAL_CALL
  100041b2 UNCONDITIONAL_CALL
  100041cc UNCONDITIONAL_CALL
*/
