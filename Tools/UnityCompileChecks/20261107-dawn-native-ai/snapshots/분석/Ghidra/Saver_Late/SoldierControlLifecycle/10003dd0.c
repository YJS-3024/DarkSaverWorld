
undefined4 __cdecl FUN_10003dd0(undefined2 *param_1,int param_2,int param_3)

{
  bool bVar1;
  int iVar2;
  int iVar3;
  
  iVar2 = param_2;
  bVar1 = false;
  if (param_3 == 2) {
    if ((*(int *)&g_RuntimeCharacters[param_2].field_0x24 * 0x28) / 100 <=
        *(int *)&g_RuntimeCharacters[param_2].field_0x20) goto LAB_10003e93;
  }
  else if (param_3 == 1) {
    if ((*(int *)&g_RuntimeCharacters[param_2].field_0x24 * 0x3c) / 100 <=
        *(int *)&g_RuntimeCharacters[param_2].field_0x20) goto LAB_10003e93;
  }
  else if ((param_3 != 3) ||
          ((*(int *)&g_RuntimeCharacters[param_2].field_0x24 * 0x5a) / 100 <=
           *(int *)&g_RuntimeCharacters[param_2].field_0x20)) goto LAB_10003e93;
  bVar1 = true;
LAB_10003e93:
  if (*(int *)&g_RuntimeCharacters[param_2].field_0x1fc == 99) {
    *(undefined4 *)&g_RuntimeCharacters[param_2].field_0x1fc = 0xffffffff;
  }
  else if (!bVar1) {
    if (*(int *)&g_RuntimeCharacters[param_2].field_0x1c == 5) {
      iVar3 = FUN_10003860(param_1,param_2,(int *)&param_1,
                           *(int *)&g_RuntimeCharacters[param_2].field_0x30);
    }
    else {
      iVar3 = FUN_10003c00(param_1,param_2,(int *)&param_1,
                           *(int *)&g_RuntimeCharacters[param_2].field_0x30);
    }
    if ((-1 < (int)param_1) && (iVar3 != 0)) {
      return 1;
    }
    if (*(int *)&g_RuntimeCharacters[iVar2].field_0x1fc == 99) {
      *(undefined4 *)&g_RuntimeCharacters[iVar2].field_0x1fc = 0xffffffff;
      bVar1 = true;
    }
    if ((*(int *)&g_RuntimeCharacters[iVar2].field_0x20 <
         *(int *)&g_RuntimeCharacters[iVar2].field_0x24) && (bVar1)) {
      return 2;
    }
    return 0;
  }
  if (*(int *)&g_RuntimeCharacters[param_2].field_0x24 <=
      *(int *)&g_RuntimeCharacters[param_2].field_0x20) {
    return 0;
  }
  return 2;
}


/* incoming refs
1000402c UNCONDITIONAL_CALL
10004198 UNCONDITIONAL_CALL
100041b2 UNCONDITIONAL_CALL
100041cc UNCONDITIONAL_CALL
CALLER 10003f70 FUN_10003f70
  1000423d UNCONDITIONAL_CALL
  1000429a UNCONDITIONAL_CALL
  100036be UNCONDITIONAL_CALL
  1000384a UNCONDITIONAL_CALL
CALLER 100040a0 FUN_100040a0
  10003682 UNCONDITIONAL_CALL
*/
