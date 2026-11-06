
undefined4 __cdecl FUN_100041f0(int *param_1,int param_2)

{
  uint uVar1;
  undefined4 uVar2;
  
  if (param_2 < 0) {
    param_2 = 0;
  }
  if ((g_RuntimeCharacters[(int)param_1].active != 0) &&
     (*(int *)&g_RuntimeCharacters[(int)param_1].field_0x4 != 0)) {
    if ((*(int *)&g_RuntimeCharacters[(int)param_1].field_0x1fc == 99) &&
       ((g_RuntimeCharacters[(int)param_1].field_0x100 & 0x10) != 0)) {
      FUN_10003f70((int)param_1);
      *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 =
           *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 & 0xffffffcf;
      return 0xffffffff;
    }
    uVar1 = *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100;
    if ((uVar1 & 0x20) != 0) {
      *(undefined4 *)&g_RuntimeCharacters[(int)param_1].field_0x298 = 0;
      uVar2 = FUN_100035e0(param_1,param_2);
      return uVar2;
    }
    if (((uVar1 & 0x10) != 0) && ((uVar1 & 1) == 0)) {
      FUN_10003f70((int)param_1);
      return 0xffffffff;
    }
    if (*(int *)&g_RuntimeCharacters[(int)param_1].field_0x60 ==
        *(int *)&g_RuntimeCharacters[(int)param_1].field_0x5c) {
      *(undefined4 *)&g_RuntimeCharacters[(int)param_1].field_0x100 = 0x10;
      if ((*(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 & 1) == 0) {
        *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 =
             *(uint *)&g_RuntimeCharacters[(int)param_1].field_0x100 | 0x20;
      }
    }
  }
  return 0xffffffff;
}


/* incoming refs
10002465 UNCONDITIONAL_CALL
CALLER 10002420 DispatchUnitAiByRole
  1001413c DATA
  Entry Point EXTERNAL
  1000240c UNCONDITIONAL_CALL
*/
