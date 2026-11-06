
void __thiscall
FUN_10008790(void *this,undefined4 param_1,undefined4 param_2,undefined4 param_3,undefined4 param_4)

{
  clock_t cVar1;
  int iVar2;
  CString *this_00;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_10015198;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  *(undefined4 *)((int)this + 8) = param_1;
  local_4 = 0;
  *(undefined4 *)((int)this + 0xc) = param_2;
  *(undefined4 *)((int)this + 0x10) = param_3;
  *(undefined4 *)((int)this + 0x14) = param_4;
  iVar2 = 0xc;
  this_00 = (CString *)((int)this + 0x1c);
  do {
    CString::Empty(this_00);
    this_00 = this_00 + 4;
    iVar2 = iVar2 + -1;
  } while (iVar2 != 0);
  CString::operator=((CString *)((int)this + 0x1c),(CString *)&stack0x00000014);
  *(undefined4 *)this = 1;
  *(undefined4 *)((int)this + 4) = 1;
  *(undefined4 *)((int)this + 0x1ac) = 0;
  cVar1 = clock();
  *(clock_t *)((int)this + 0x1bc) = cVar1;
  local_4 = 0xffffffff;
  CString::~CString((CString *)&stack0x00000014);
  ExceptionList = local_c;
  return;
}


/* incoming refs
10010d53 UNCONDITIONAL_CALL
1000d204 UNCONDITIONAL_CALL
1000d254 UNCONDITIONAL_CALL
1000d9da UNCONDITIONAL_CALL
10009e87 UNCONDITIONAL_CALL
100095ce UNCONDITIONAL_CALL
100094d0 UNCONDITIONAL_CALL
10009861 UNCONDITIONAL_CALL
CALLER 10009520 FUN_10009520
  1000bf22 UNCONDITIONAL_CALL
CALLER 100093c0 FUN_100093c0
  1000c085 UNCONDITIONAL_CALL
CALLER 100096c0 FUN_100096c0
  1000c375 UNCONDITIONAL_CALL
CALLER 100109e0 FUN_100109e0
  10012e91 UNCONDITIONAL_CALL
CALLER 10009890 FUN_10009890
  10002e84 UNCONDITIONAL_CALL
  10002ea4 UNCONDITIONAL_CALL
CALLER 1000d8f0 FUN_1000d8f0
  1000ec55 UNCONDITIONAL_CALL
CALLER 1000d150 FUN_1000d150
  1000eb78 UNCONDITIONAL_CALL
*/
