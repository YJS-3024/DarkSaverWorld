
/* void __cdecl OnMouseMove(int,int) */

void __cdecl OnMouseMove(int param_1,int param_2)

{
  int iVar1;
  
                    /* 0x2d50  61  ?OnMouseMove@@YAXHH@Z */
  if ((DAT_1006c118 == 0) || (DAT_1006c110 != 0)) {
    DAT_1001a0e4 = param_1;
    DAT_1001a0e8 = param_2;
    if (DAT_1006a70c != (void *)0x0) {
      FUN_1000a6f0(DAT_1006a70c,param_1,param_2,0);
    }
    DAT_1001a8e4 = (int)(param_2 + (param_2 >> 0x1f & 0x1fU)) >> 5;
    DAT_1001a8e8 = (int)(param_1 + (param_1 >> 0x1f & 0x1fU)) >> 5;
    iVar1 = mainmapgetchar(DAT_1001c460 + DAT_1001a8e8,DAT_1001c464 + DAT_1001a8e4);
    if (-1 < iVar1) {
      DAT_1001a380 = iVar1;
    }
    if ((((DAT_1006c118 != 0) && (DAT_1006c104 == 0)) && (DAT_1006c0f0 == 5)) &&
       (DAT_1006c0f4 == iVar1)) {
      DAT_1006c104 = 1;
    }
  }
  return;
}

