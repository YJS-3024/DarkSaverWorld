
void __thiscall FUN_100089f0(void *this,int param_1,int param_2)

{
  int iVar1;
  void *pvVar2;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_100151f8;
  local_c = ExceptionList;
  local_4 = 0;
  ExceptionList = &local_c;
  *(undefined4 *)((int)this + 8) = 0x28;
  if (param_1 == 1) {
    *(undefined4 *)((int)this + 0xc) = 0x32;
  }
  else {
    *(undefined4 *)((int)this + 0xc) = 300;
  }
  *(undefined4 *)((int)this + 0x10) = 0x231;
  *(undefined4 *)((int)this + 0x14) = 0x68;
  pvVar2 = this;
  CString::CString((CString *)&stack0xffffffec,(CString *)&stack0x0000000c);
  iVar1 = FUN_10008840(this,(int)pvVar2);
  *(int *)((int)this + 0x1b4) = iVar1;
  *(undefined4 *)((int)this + 0x1b0) = 0;
  GetFace(DAT_1001c484,param_2,0,0);
  *(undefined4 *)this = 1;
  *(undefined4 *)((int)this + 4) = 1;
  *(undefined4 *)((int)this + 0x1ac) = 1;
  if (DAT_1006c118 != 0) {
    FUN_10008ca0(this);
  }
  local_4 = 0xffffffff;
  CString::~CString((CString *)&stack0x0000000c);
  ExceptionList = local_c;
  return;
}


/* incoming refs
10010e88 UNCONDITIONAL_CALL
CALLER 100109e0 FUN_100109e0
  10012e91 UNCONDITIONAL_CALL
*/
