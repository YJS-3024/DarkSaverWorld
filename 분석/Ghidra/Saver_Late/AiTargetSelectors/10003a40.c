
undefined4 __cdecl FUN_10003a40(undefined2 *param_1,int param_2,int *param_3,int param_4)

{
  int iVar1;
  int iVar2;
  int iVar3;
  int iVar4;
  int local_22c;
  int local_228;
  int *local_224;
  undefined2 local_220 [2];
  undefined2 local_21c [2];
  int local_218 [134];
  
  *param_3 = -1;
  ClearTa(&DAT_100646a0);
  SetArea(g_RuntimeCharacters[param_2].tileX,g_RuntimeCharacters[param_2].tileY,param_4,
          &DAT_100646a0);
  iVar1 = DAT_100e4298;
  iVar2 = 0;
  iVar3 = 0;
  local_22c = 0;
  (&DAT_100646a0)[g_RuntimeCharacters[param_2].tileY + g_RuntimeCharacters[param_2].tileX * 200] =
       0xffffffff;
  if (0 < iVar1) {
    local_228 = 0;
    iVar1 = DAT_100e429c;
    do {
      iVar4 = 0;
      if (0 < iVar1) {
        local_224 = local_218 + iVar3;
        do {
          iVar1 = mainmapgetchar(iVar2,iVar4);
          if ((((-1 < iVar1) && (iVar3 = FUN_100024a0(iVar1,param_2), iVar3 != 0)) &&
              (0 < (int)(&DAT_100646a0)[local_228 + iVar4])) &&
             (((g_RuntimeCharacters[iVar1].active != 0 &&
               (*(int *)&g_RuntimeCharacters[iVar1].field_0x4 != 0)) &&
              ((g_RuntimeCharacters[iVar1].field_0x100 & 0x80) == 0)))) {
            local_22c = local_22c + 1;
            *local_224 = iVar1;
            local_224 = local_224 + 1;
          }
          iVar4 = iVar4 + 1;
          iVar1 = DAT_100e429c;
          iVar3 = local_22c;
        } while (iVar4 < DAT_100e429c);
      }
      iVar2 = iVar2 + 1;
      local_228 = local_228 + 200;
    } while (iVar2 < DAT_100e4298);
    if (iVar3 != 0) {
      iVar1 = rand();
      iVar1 = local_218[iVar1 % iVar3];
      *param_3 = iVar1;
      FUN_10002260(param_2,iVar1,(int *)local_21c,(int *)local_220);
      *param_1 = (short)param_2;
      param_1[1] = (short)*param_3;
      param_1[2] = local_21c[0];
      param_1[3] = local_220[0];
      return 1;
    }
  }
  return 0;
}


/* incoming refs
100044a4 UNCONDITIONAL_CALL
CALLER 100042e0 CommanderAIAction
  10014014 DATA
  Entry Point EXTERNAL
  100049ed UNCONDITIONAL_CALL
  10004920 UNCONDITIONAL_CALL
*/
