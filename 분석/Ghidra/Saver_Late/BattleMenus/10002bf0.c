
void __cdecl FUN_10002bf0(short param_1)

{
  clock_t cVar1;
  int xLeft;
  tagRECT local_10;
  
  if (DAT_1001c4d8 == 0) {
    DAT_1001c4d8 = clock();
  }
  cVar1 = clock();
  if (0xfa < cVar1 - DAT_1001c4d8) {
    DAT_1001c4dc = (DAT_1001c4dc + 1) % 3;
    DAT_1001c4e0 = DAT_1001c4e0 + 1 & 0x80000001;
    if ((int)DAT_1001c4e0 < 0) {
      DAT_1001c4e0 = (DAT_1001c4e0 - 1 | 0xfffffffe) + 1;
    }
    DAT_1001c4d8 = clock();
  }
  SetRect(&local_10,DAT_1001c4dc * 0x2e + 0x9e,0x136,DAT_1001c4dc * 0x2e + 0xcc,0x164);
  CDDSurface::BltFast(DAT_1001c470,DAT_1001a8e8 * 0x20 + -7,DAT_1001a8e4 * 0x20 + -7,DAT_1006a704,
                      &local_10,1);
  xLeft = (param_1 + 8) * 0x20;
  if (((param_1 == 1) || (param_1 == 2)) || (param_1 == 3)) {
    SetRect(&local_10,xLeft,DAT_1001c4e0 * 0xa0,xLeft + 0x20,DAT_1001c4e0 * 0xa0 + 0x20);
    CDDSurface::BltFast(DAT_1001c470,DAT_1001a8e8 << 5,DAT_1001a8e4 << 5,DAT_1001c47c,&local_10,1);
  }
  return;
}

