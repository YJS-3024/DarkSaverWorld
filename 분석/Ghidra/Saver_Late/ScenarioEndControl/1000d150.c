
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void __cdecl FUN_1000d150(char param_1)

{
  SendMessageA(DAT_1001c468,0x4dc,1,0);
  DAT_1001c430 = 0;
  DAT_1001c434 = 1;
  *(undefined4 *)((int)DAT_1006a708 + 0x18) = 0;
  if (param_1 == '\x01') {
    FUN_1000d0b0();
    setgamestatus(0);
    return;
  }
  if (DAT_1006a740 != 1) {
    if (DAT_1006a740 != 3) {
      FUN_100029d0(0x4ca,8,0);
      FUN_100029d0(0x4d0,8,0xa006e);
      setgamestatus(0);
      return;
    }
    CString::CString((CString *)&stack0xfffffff8,&DAT_1001a4d4);
    FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
    *(undefined4 *)((int)DAT_1006a708 + 0x18) = 1;
    _DAT_1001c4cc = 0;
    setgamestatus(0);
    return;
  }
  operator+((CString *)&stack0xfffffff8,(char *)&DAT_1006a73c);
  FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
  *(undefined4 *)((int)DAT_1006a708 + 0x18) = 1;
  _DAT_1001c4cc = 1;
  setgamestatus(0);
  return;
}


/* incoming refs
1000eb78 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
