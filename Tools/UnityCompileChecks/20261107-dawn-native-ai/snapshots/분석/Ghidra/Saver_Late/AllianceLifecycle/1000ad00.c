
/* int __cdecl getsnum(int) */

int __cdecl getsnum(int param_1)

{
  int iVar1;
  
                    /* 0xad00  96  ?getsnum@@YAHH@Z */
  if (DAT_1010b3d8 == 0) {
    if (*(int *)&g_RuntimeCharacters[param_1].field_0x2dc == -3) {
      return DAT_10016114;
    }
    if (*(int *)&g_RuntimeCharacters[param_1].field_0x2dc != -2) {
      return DAT_10016110;
    }
  }
  else {
    iVar1 = GetTeamID(param_1);
    if (7 < iVar1) {
      return 0;
    }
    iVar1 = FUN_100024a0(DAT_1003d540 * 0x32,param_1);
    if (iVar1 != 0) {
      return DAT_10016110;
    }
    iVar1 = ally3(DAT_1003d540 * 0x32,param_1);
    if (iVar1 != 0) {
      return DAT_10016114;
    }
  }
  return DAT_10016118;
}

