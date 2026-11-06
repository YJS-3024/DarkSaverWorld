
undefined4 __cdecl FUN_100024a0(int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  
  if ((-1 < param_1) && (-1 < param_2)) {
    iVar1 = GetTeamID(param_1);
    iVar2 = GetTeamID(param_2);
    if (iVar1 == iVar2) {
      return 1;
    }
    if (((0xf < iVar1) || (0xf < iVar2)) ||
       ((7 < iVar1 && (((iVar1 < 0x10 && (7 < iVar2)) && (iVar2 < 0x10)))))) {
      return 1;
    }
  }
  return 0;
}


/* incoming refs
10003afc UNCONDITIONAL_CALL
1000ad63 UNCONDITIONAL_CALL
CALLER 1000ad00 getsnum
  10014164 DATA
  Entry Point EXTERNAL
  1000af2d UNCONDITIONAL_CALL
  1000b69a UNCONDITIONAL_CALL
  1000b980 UNCONDITIONAL_CALL
  1000fd76 UNCONDITIONAL_CALL
  1000bdd3 UNCONDITIONAL_CALL
CALLER 10003a40 FUN_10003a40
  100044a4 UNCONDITIONAL_CALL
*/
