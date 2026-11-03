
undefined4 __cdecl FUN_100029d0(int param_1,int param_2,uint param_3)

{
  undefined4 uVar1;
  
  if (((DAT_1006c118 == 0) || (DAT_1001c4c0 == 0)) || ((param_1 != 0x4cf && (param_1 != 0x4cb)))) {
    DAT_1001c4c0 = 0;
    if ((((param_1 == 0x4d0) && (param_2 != 0xc)) && (param_2 != 8)) &&
       ((param_2 != 0x1c && (DAT_1006c118 == 0)))) {
      return 0xffffffff;
    }
    if (DAT_1001c448 != (code *)0x0) {
      uVar1 = (*DAT_1001c448)(param_1,param_2,param_3 & 0xffff,(int)param_3 >> 0x10);
      return uVar1;
    }
  }
  DAT_1001c4c0 = 0;
  return 0;
}

