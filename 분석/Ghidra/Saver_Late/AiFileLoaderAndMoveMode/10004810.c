
/* int __cdecl CommanderAIMove(int,int) */

int __cdecl CommanderAIMove(int param_1,int param_2)

{
  int iVar1;
  int centerX;
  int *piVar2;
  int iVar3;
  int centerY;
  int iVar4;
  int local_20 [2];
  _PGSMOVECHARPATH local_18 [24];
  
                    /* 0x4810  13  ?CommanderAIMove@@YAHHH@Z */
  iVar1 = GetCmderAINum(param_1);
  iVar4 = 1;
  if (-1 < iVar1) {
    iVar3 = iVar1 * 0x110;
    iVar1 = (&DAT_100d99a4)[iVar1 * 0x44];
    if (iVar1 == 4) {
      iVar4 = 2;
    }
    if (iVar1 == 2) {
      iVar4 = 1;
    }
    if (iVar1 == 8) {
      iVar4 = 3;
    }
    if (*(int *)(&DAT_100d99a8 + iVar3) == 0) {
      centerX = *(int *)(&DAT_100d99ac + iVar3);
      centerY = *(int *)(&DAT_100d99b0 + iVar3);
      iVar1 = *(int *)(&DAT_100d99b4 + iVar3);
      if (((-1 < centerX) && (-1 < centerY)) && (-1 < iVar1)) goto LAB_100048a9;
    }
    else if (((*(int *)(&DAT_100d99a8 + iVar3) == 1) &&
             (iVar1 = *(int *)(&DAT_100d99b8 + iVar3), g_RuntimeCharacters[iVar1].active != 0)) &&
            (*(int *)&g_RuntimeCharacters[iVar1].field_0x4 != 0)) {
      centerX = g_RuntimeCharacters[iVar1].tileX;
      centerY = g_RuntimeCharacters[iVar1].tileY;
      iVar1 = *(int *)&g_RuntimeCharacters[iVar1].field_0x4c;
      if (iVar1 < 8) {
        iVar1 = 7;
      }
      goto LAB_100048a9;
    }
  }
  iVar4 = 2;
  centerX = g_RuntimeCharacters[param_1].tileX;
  centerY = g_RuntimeCharacters[param_1].tileY;
  iVar1 = 0x14;
LAB_100048a9:
  piVar2 = GetMovePos(local_20,param_1,&param_2,centerX,centerY,iVar1,iVar4);
  iVar3 = param_2;
  iVar1 = *piVar2;
  iVar4 = piVar2[1];
  if ((-1 < iVar1) && (-1 < iVar4)) {
    if ((g_RuntimeCharacters[param_1].tileX != iVar1) ||
       (g_RuntimeCharacters[param_1].tileY != iVar4)) {
      iVar1 = MoveCharPath(local_18,param_1,iVar1,iVar4);
      if (iVar1 != 0) {
        FUN_10004ff0((undefined2 *)local_18);
      }
      *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
           *(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0xffffffcf;
    }
  }
  CommanderAIAction(param_1,iVar3);
  return -1;
}


/* incoming refs
10014018 DATA
Entry Point EXTERNAL
100049d5 UNCONDITIONAL_CALL
CALLER 10004990 ExecuteCommanderAI
  10014020 DATA
  Entry Point EXTERNAL
  10002457 UNCONDITIONAL_CALL
*/
