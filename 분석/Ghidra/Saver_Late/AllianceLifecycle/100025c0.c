
/* int __cdecl ally4(int,int) */

int __cdecl ally4(int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  
                    /* 0x25c0  90  ?ally4@@YAHHH@Z */
  if ((-1 < param_1) && (-1 < param_2)) {
    iVar1 = GetTeamID(param_1);
    iVar2 = GetTeamID(param_2);
    if (((iVar1 != iVar2) && ((iVar1 < 0x10 && (iVar2 < 0x10)))) &&
       ((iVar1 < 8 || (((0xf < iVar1 || (iVar2 < 8)) || (0xf < iVar2)))))) {
      if (((iVar1 < 0) || (7 < iVar1)) || ((iVar2 < 0 || (7 < iVar2)))) {
        return 1;
      }
      if (iVar1 == DAT_1003d540) {
        if (((&DAT_1003d54c)[iVar2] == 4) || ((&DAT_1003d54c)[iVar2] == 1)) {
          return 1;
        }
      }
      else {
        if (iVar2 != DAT_1003d540) {
          return 1;
        }
        if ((&DAT_1003d54c)[iVar1] == 4) {
          return 1;
        }
        if ((&DAT_1003d54c)[iVar1] == 1) {
          return 1;
        }
      }
    }
  }
  return 0;
}

