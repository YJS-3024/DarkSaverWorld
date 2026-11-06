
/* int __cdecl MoveCharPath(struct _PGSMOVECHARPATH &,int,int,int) */

int __cdecl MoveCharPath(_PGSMOVECHARPATH *param_1,int param_2,int param_3,int param_4)

{
  uint uVar1;
  NativeCharacter32 *pNVar2;
  int iVar3;
  int local_4;
  
                    /* 0xe310  62  ?MoveCharPath@@YAHAAU_PGSMOVECHARPATH@@HHH@Z */
  ClearTa((int *)&DAT_1010b3ec);
  if ((((0 < param_3) && (0 < param_4)) && (param_3 < 200)) && (param_4 < 200)) {
    pNVar2 = g_RuntimeCharacters + param_2;
    if (pNVar2->active != 0) {
      iVar3 = pNVar2->tileX;
      uVar1 = pNVar2->tileY;
      SetArea2(iVar3,uVar1,*(int *)&pNVar2->field_0x3c + 7,param_2,*(int *)&pNVar2->field_0x1c,
               (int *)&DAT_1010b3ec);
      mainmapsetchar(iVar3,uVar1,param_2);
      local_4 = 0;
      if (-1 < *(int *)(&DAT_1010b3ec + (param_4 + param_3 * 200) * 4)) {
        iVar3 = FUN_1000dd30((undefined2 *)param_1,param_3,param_4,param_2,iVar3,uVar1,0x1010b3ec,
                             &local_4);
        if (iVar3 != 0) {
          return 1;
        }
      }
    }
  }
  return 0;
}


/* incoming refs
100140dc DATA
Entry Point EXTERNAL
10003822 UNCONDITIONAL_CALL
100048f8 UNCONDITIONAL_CALL
CALLER 100035e0 FUN_100035e0
  10004282 UNCONDITIONAL_CALL
CALLER 10004810 CommanderAIMove
  10014018 DATA
  Entry Point EXTERNAL
  100049d5 UNCONDITIONAL_CALL
*/
