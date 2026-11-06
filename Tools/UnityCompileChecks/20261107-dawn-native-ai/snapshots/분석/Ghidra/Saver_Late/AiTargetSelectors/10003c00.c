
undefined4 __cdecl FUN_10003c00(undefined2 *param_1,int param_2,int *param_3,int param_4)

{
  int iVar1;
  NativeCharacter32 *pNVar2;
  int *piVar3;
  int iVar4;
  int iVar5;
  int *piVar6;
  int *local_22c;
  int local_228;
  int local_224;
  undefined2 local_220 [2];
  undefined2 local_21c [2];
  int local_218 [134];
  
  *param_3 = -1;
  ClearTa(&DAT_100646a0);
  SetAreaB(g_RuntimeCharacters[param_2].tileX,g_RuntimeCharacters[param_2].tileY,param_4,
           *(int *)&g_RuntimeCharacters[param_2].field_0x1c,&DAT_100646a0);
  iVar1 = DAT_100e4298;
  iVar5 = 0;
  local_224 = 0;
  local_228 = 0;
  (&DAT_100646a0)[g_RuntimeCharacters[param_2].tileY + g_RuntimeCharacters[param_2].tileX * 200] =
       0xffffffff;
  if (0 < iVar1) {
    local_22c = &DAT_100646a0;
    iVar1 = DAT_100e429c;
    do {
      iVar4 = 0;
      if (0 < iVar1) {
        piVar3 = local_218 + iVar5;
        piVar6 = local_22c;
        do {
          iVar1 = mainmapgetchar(local_228,iVar4);
          if (((*piVar6 < 1) || (iVar1 < 0)) || (iVar5 = ally3(iVar1,param_2), iVar5 != 0)) {
            *piVar6 = -1;
          }
          else {
            pNVar2 = g_RuntimeCharacters + iVar1;
            if (((pNVar2->active != 0) && (*(int *)&pNVar2->field_0x4 != 0)) &&
               ((pNVar2->field_0x100 & 0x80) == 0)) {
              iVar5 = *(int *)&pNVar2->field_0x20;
              *piVar3 = iVar1;
              *piVar6 = iVar5;
              local_224 = local_224 + 1;
              piVar3 = piVar3 + 1;
            }
          }
          iVar4 = iVar4 + 1;
          piVar6 = piVar6 + 1;
          iVar1 = DAT_100e429c;
          iVar5 = local_224;
        } while (iVar4 < DAT_100e429c);
      }
      local_228 = local_228 + 1;
      local_22c = local_22c + 200;
    } while (local_228 < DAT_100e4298);
    if (iVar5 != 0) {
      iVar1 = rand();
      iVar1 = local_218[iVar1 % iVar5];
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
10003156 UNCONDITIONAL_CALL
10003efd UNCONDITIONAL_CALL
10004571 UNCONDITIONAL_CALL
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
