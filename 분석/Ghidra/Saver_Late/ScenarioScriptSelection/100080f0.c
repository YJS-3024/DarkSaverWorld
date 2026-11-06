
/* void __cdecl SetCurrentScenario(char *) */

void __cdecl SetCurrentScenario(char *param_1)

{
                    /* 0x80f0  70  ?SetCurrentScenario@@YAXPAD@Z */
  DAT_1006c084 = 1;
  CString::operator=((CString *)&DAT_1006c0e8,param_1);
  FUN_10007f10();
  FUN_1000bdb0();
  return;
}

