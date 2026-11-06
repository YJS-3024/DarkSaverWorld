
/* int __cdecl ExecuteCommanderAI(int,int) */

int __cdecl ExecuteCommanderAI(int param_1,int param_2)

{
  uint uVar1;
  int iVar2;
  
                    /* 0x4990  15  ?ExecuteCommanderAI@@YAHHH@Z */
  if (param_2 < 0) {
    param_2 = 0;
  }
  if ((g_RuntimeCharacters[param_1].active != 0) &&
     (*(int *)&g_RuntimeCharacters[param_1].field_0x4 != 0)) {
    uVar1 = *(uint *)&g_RuntimeCharacters[param_1].field_0x100;
    if ((uVar1 & 0x20) != 0) {
      iVar2 = CommanderAIMove(param_1,param_2);
      return iVar2;
    }
    if (((uVar1 & 0x10) != 0) && ((uVar1 & 1) == 0)) {
      CommanderAIAction(param_1,param_2);
      return -1;
    }
    if (*(int *)&g_RuntimeCharacters[param_1].field_0x60 ==
        *(int *)&g_RuntimeCharacters[param_1].field_0x5c) {
      *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x100 = 0x10;
      if ((*(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 1) == 0) {
        *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
             *(uint *)&g_RuntimeCharacters[param_1].field_0x100 | 0x20;
      }
    }
  }
  return -1;
}


/* incoming refs
10014020 DATA
Entry Point EXTERNAL
10002457 UNCONDITIONAL_CALL
CALLER 10002420 DispatchUnitAiByRole
  1001413c DATA
  Entry Point EXTERNAL
  1000240c UNCONDITIONAL_CALL
*/
