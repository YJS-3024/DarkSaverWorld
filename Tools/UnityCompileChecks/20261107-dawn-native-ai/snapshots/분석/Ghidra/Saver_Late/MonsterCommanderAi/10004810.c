
/* int __cdecl CommanderAIMove(int,int) */

int __cdecl CommanderAIMove(int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  int *piVar3;
  int iVar4;
  undefined1 local_20 [8];
  _PGSMOVECHARPATH local_18 [24];
  
                    /* 0x4810  13  ?CommanderAIMove@@YAHHH@Z */
  iVar1 = GetCmderAINum(param_1);
  if (-1 < iVar1) {
    iVar1 = iVar1 * 0x110;
    if (*(int *)(&DAT_100d99a8 + iVar1) == 0) {
      iVar2 = *(int *)(&DAT_100d99ac + iVar1);
      iVar4 = *(int *)(&DAT_100d99b0 + iVar1);
      iVar1 = *(int *)(&DAT_100d99b4 + iVar1);
      if (((-1 < iVar2) && (-1 < iVar4)) && (-1 < iVar1)) goto LAB_100048a9;
    }
    else if (((*(int *)(&DAT_100d99a8 + iVar1) == 1) &&
             (iVar1 = *(int *)(&DAT_100d99b8 + iVar1), g_RuntimeCharacters[iVar1].active != 0)) &&
            (*(int *)&g_RuntimeCharacters[iVar1].field_0x4 != 0)) {
      iVar2 = g_RuntimeCharacters[iVar1].tileX;
      iVar4 = g_RuntimeCharacters[iVar1].tileY;
      iVar1 = *(int *)&g_RuntimeCharacters[iVar1].field_0x4c;
      if (iVar1 < 8) {
        iVar1 = 7;
      }
      goto LAB_100048a9;
    }
  }
  iVar2 = g_RuntimeCharacters[param_1].tileX;
  iVar4 = g_RuntimeCharacters[param_1].tileY;
  iVar1 = 0x14;
LAB_100048a9:
  piVar3 = (int *)GetMovePos((int)local_20,(int *)param_1,(int)&param_2,iVar2,iVar4,iVar1);
  iVar4 = param_2;
  iVar1 = *piVar3;
  iVar2 = piVar3[1];
  if (((-1 < iVar1) && (-1 < iVar2)) &&
     ((g_RuntimeCharacters[param_1].tileX != iVar1 || (g_RuntimeCharacters[param_1].tileY != iVar2))
     )) {
    iVar1 = MoveCharPath(local_18,param_1,iVar1,iVar2);
    if (iVar1 != 0) {
      FUN_10004ff0((undefined2 *)local_18);
    }
    *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
         *(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0xffffffcf;
  }
  CommanderAIAction(param_1,iVar4);
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
