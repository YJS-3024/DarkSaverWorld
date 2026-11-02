
void __cdecl
FUN_10002070(int param_1,undefined4 *param_2,undefined4 *param_3,undefined4 param_4,
            undefined4 param_5,undefined4 param_6,undefined4 param_7)

{
  void *this;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &LAB_10014d3b;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  userclearstatusall();
  if (DAT_1001a8d4 != (undefined4 *)0x0) {
    (**(code **)*DAT_1001a8d4)(1);
    DAT_1001a8d4 = (undefined4 *)0x0;
  }
  this = operator_new(0x38);
  uStack_4 = 0;
  if (this == (void *)0x0) {
    DAT_1001a8d4 = (undefined4 *)0x0;
  }
  else {
    DAT_1001a8d4 = FUN_10002150(this,DAT_1001c48c,param_1);
  }
  uStack_4 = 0xffffffff;
  FUN_10002680(DAT_1001a8d4,param_2);
  DAT_1001a8d4[0xc] = param_7;
  DAT_1001a8d4[8] = param_4;
  DAT_1001a8d4[9] = param_5;
  DAT_1001a8d4[10] = param_6;
  FUN_100026d0(DAT_1001a8d4,param_3);
  ExceptionList = pvStack_c;
  return;
}

