
void __cdecl FUN_10004ff0(undefined2 *param_1)

{
  void *pvVar1;
  int iVar2;
  int local_4;
  
  if (DAT_1010b3d8 != 0) {
    local_4 = 0;
    pvVar1 = operator_new(0x18);
    FUN_10004fb0((int)pvVar1,&local_4,0x5c);
    FUN_10004fb0((int)pvVar1,&local_4,0x43);
    FUN_10004f90((int)pvVar1,&local_4,*param_1);
    iVar2 = 5;
    do {
      FUN_10004f90((int)pvVar1,&local_4,param_1[1]);
      FUN_10004f90((int)pvVar1,&local_4,param_1[2]);
      iVar2 = iVar2 + -1;
      param_1 = param_1 + 2;
    } while (iVar2 != 0);
    FUN_10004f70((LPARAM)pvVar1,0x18);
    operator_delete(pvVar1);
  }
  return;
}


/* incoming refs
10003833 UNCONDITIONAL_CALL
10004909 UNCONDITIONAL_CALL
CALLER 100035e0 FUN_100035e0
  10004282 UNCONDITIONAL_CALL
CALLER 10004810 CommanderAIMove
  10014018 DATA
  Entry Point EXTERNAL
  100049d5 UNCONDITIONAL_CALL
*/
