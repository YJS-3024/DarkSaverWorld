
void FUN_1000d0b0(void)

{
  int iVar1;
  
  if (DAT_1006c118 != 0) {
    setgamestatus(0);
    return;
  }
  if (DAT_1006a740 == 1) {
    CString::CString((CString *)&stack0xfffffff4,&DAT_1001a274);
    FUN_1000fab0(DAT_10093864);
    iVar1 = 0;
    do {
      FUN_100029d0(0x4cb,iVar1,0);
      iVar1 = iVar1 + 1;
    } while (iVar1 < 10);
    FUN_10008d20();
    return;
  }
  DAT_1001a8ec = 0;
  PostMessageA(DAT_1001c468,0x590,0,0);
  PostMessageA(DAT_1001c468,0x4cd,0,0);
  FUN_1000e750();
  return;
}


/* incoming refs
1000d18f UNCONDITIONAL_CALL
CALLER 1000d150 FUN_1000d150
  1000eb78 UNCONDITIONAL_CALL
*/
