
/* int __cdecl GetCmderAINum(int) */

int __cdecl GetCmderAINum(int param_1)

{
  int iVar1;
  int *piVar2;
  
                    /* 0x2eb0  24  ?GetCmderAINum@@YAHH@Z */
  iVar1 = 0;
  piVar2 = &DAT_100d99a0;
  do {
    if (*piVar2 == param_1) {
      return iVar1;
    }
    piVar2 = piVar2 + 0x44;
    iVar1 = iVar1 + 1;
  } while ((int)piVar2 < 0x100e03e0);
  return -1;
}


/* incoming refs
10014044 DATA
Entry Point EXTERNAL
10004323 UNCONDITIONAL_CALL
1000481b UNCONDITIONAL_CALL
CALLER 10004810 CommanderAIMove
  10014018 DATA
  Entry Point EXTERNAL
  100049d5 UNCONDITIONAL_CALL
CALLER 100042e0 CommanderAIAction
  10014014 DATA
  Entry Point EXTERNAL
  100049ed UNCONDITIONAL_CALL
  10004920 UNCONDITIONAL_CALL
*/
