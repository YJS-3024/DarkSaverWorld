
int __cdecl FUN_1000f4e0(char *param_1,int param_2)

{
  char cVar1;
  int iVar2;
  char *pcVar3;
  
  cVar1 = *param_1;
  pcVar3 = param_1;
  if (cVar1 != '\0') {
    while (('/' < cVar1 && (*pcVar3 < ':'))) {
      cVar1 = pcVar3[1];
      pcVar3 = pcVar3 + 1;
      if (cVar1 == '\0') {
        iVar2 = atoi(param_1);
        return iVar2;
      }
    }
  }
  return param_2;
}


/* incoming refs
10002d18 UNCONDITIONAL_CALL
10002712 UNCONDITIONAL_CALL
10002720 UNCONDITIONAL_CALL
1000272e UNCONDITIONAL_CALL
10002783 UNCONDITIONAL_CALL
10002abb UNCONDITIONAL_CALL
1000ebb5 UNCONDITIONAL_CALL
1000ec3a UNCONDITIONAL_CALL
CALLER 1000ea80 SpawnActorFromScriptArguments
  10014160 DATA
  Entry Point EXTERNAL
CALLER 100027d0 FUN_100027d0
  10002df9 UNCONDITIONAL_CALL
CALLER 10002670 FUN_10002670
  10002dee UNCONDITIONAL_CALL
CALLER 10002b40 LoadScenarioAiRules
  10014174 DATA
  Entry Point EXTERNAL
  1000135f UNCONDITIONAL_CALL
*/
