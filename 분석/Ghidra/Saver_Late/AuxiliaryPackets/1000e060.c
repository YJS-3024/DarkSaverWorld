
void __cdecl FUN_1000e060(int param_1)

{
  char *pcVar1;
  int local_4;
  
  local_4 = 0;
  pcVar1 = (char *)FUN_1000c560(param_1,&local_4);
  if (*pcVar1 == '\0') {
    CString::Empty((CString *)&DAT_1006bd0c);
    return;
  }
  CString::operator=((CString *)&DAT_1006bd0c,*pcVar1 + '`');
  return;
}


/* incoming refs
1000ec96 UNCONDITIONAL_CALL
CALLER 1000eb10 DispatchGamePacket
  10002d3d UNCONDITIONAL_CALL
*/
