
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

