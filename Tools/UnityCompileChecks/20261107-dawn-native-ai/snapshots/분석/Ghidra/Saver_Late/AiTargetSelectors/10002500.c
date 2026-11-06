
/* int __cdecl ally3(int,int) */

int __cdecl ally3(int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  
                    /* 0x2500  89  ?ally3@@YAHHH@Z */
  if (param_1 < 0) {
    return 0;
  }
  if (param_2 < 0) {
    return 0;
  }
  iVar1 = GetTeamID(param_1);
  iVar2 = GetTeamID(param_2);
  if (iVar1 == iVar2) {
    return 1;
  }
  if ((iVar1 < 0x10) && (iVar2 < 0x10)) {
    if ((-1 < iVar1) && (((iVar1 < 8 && (-1 < iVar2)) && (iVar2 < 8)))) {
      if (iVar1 == DAT_1003d540) {
        if (((&DAT_1003d54c)[iVar2] != 2) && ((&DAT_1003d54c)[iVar2] != 1)) {
          return 0;
        }
        return 1;
      }
      if (iVar2 == DAT_1003d540) {
        if (((&DAT_1003d54c)[iVar1] != 2) && ((&DAT_1003d54c)[iVar1] != 1)) {
          return 0;
        }
        return 1;
      }
    }
    if ((((iVar1 < 8) || (0xf < iVar1)) || (iVar2 < 8)) || (0xf < iVar2)) {
      return 0;
    }
  }
  return 1;
}


/* incoming refs
10014148 DATA
Entry Point EXTERNAL
10002058 UNCONDITIONAL_CALL
100031f8 UNCONDITIONAL_CALL
10003932 UNCONDITIONAL_CALL
10003cd7 UNCONDITIONAL_CALL
1000ad85 UNCONDITIONAL_CALL
CALLER 1000ad00 getsnum
  10014164 DATA
  Entry Point EXTERNAL
  1000af2d UNCONDITIONAL_CALL
  1000b69a UNCONDITIONAL_CALL
  1000b980 UNCONDITIONAL_CALL
  1000fd76 UNCONDITIONAL_CALL
  1000bdd3 UNCONDITIONAL_CALL
CALLER 10003c00 FUN_10003c00
  10003156 UNCONDITIONAL_CALL
  10003efd UNCONDITIONAL_CALL
  10004571 UNCONDITIONAL_CALL
CALLER 10001d60 CheckMagicTarget
  1001400c DATA
  Entry Point EXTERNAL
  10004619 UNCONDITIONAL_CALL
CALLER 100030b0 MoveAttack
  100140d4 DATA
  Entry Point EXTERNAL
  100035a5 UNCONDITIONAL_CALL
CALLER 10003860 FUN_10003860
  10003144 UNCONDITIONAL_CALL
  10003ef6 UNCONDITIONAL_CALL
  10004483 UNCONDITIONAL_CALL
  1000455f UNCONDITIONAL_CALL
*/
