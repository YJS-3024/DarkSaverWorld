
/* void __cdecl OnKeyPress(int) */

void __cdecl OnKeyPress(int param_1)

{
                    /* 0x2f50  57  ?OnKeyPress@@YAXH@Z */
  if (DAT_1006c118 == 0) {
    FUN_10002fb0(param_1);
    return;
  }
  if (DAT_1006c10c != 0) {
    if (DAT_1006c104 != 0) {
      FUN_10002fb0(param_1);
      return;
    }
    if ((DAT_1006c0f0 == 4) && (param_1 == DAT_1006c0f4)) {
      DAT_1006c104 = 1;
      FUN_10002fb0(param_1);
    }
  }
  return;
}

