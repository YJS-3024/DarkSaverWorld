
/* void __cdecl SetTeamId(int) */

void __cdecl SetTeamId(int param_1)

{
                    /* 0x8370  79  ?SetTeamId@@YAXH@Z */
  DAT_1006a738 = -99;
  if (param_1 < 0x14) {
    DAT_1006a738 = param_1;
  }
  CString::Empty((CString *)&DAT_1006bd0c);
  FUN_100029d0(0x4ca,0xc,0);
  FUN_100029d0(0x4d0,0xc,0x1c0001e);
  FUN_10004460();
  return;
}

