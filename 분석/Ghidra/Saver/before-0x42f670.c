/* Ghidra decompilation; not recovered original source. Evidence: ev_542e35c0a25555fa2f2abc4bb4c94be286f2a11e5c43c44fcbbdbdb4db5c3852 */

void __thiscall FUN_0042f670(void *this,undefined4 param_1,char *param_2,int param_3)

{
  char *pcVar1;
  int iVar2;
  int iVar3;
  void *pvStack_c;
  undefined *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &DAT_0047d2f8;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  iVar3 = thunk_FUN_0042f5c0(this);
  iVar2 = param_3;
  pcVar1 = param_2;
  thunk_FUN_004298b0(this,(short)param_1,(int)param_2,param_3);
  if (iVar3 != 0) {
    *(short *)(iVar3 + 0xc) = (short)pcVar1;
    *(undefined2 *)(iVar3 + 6) = (undefined2)param_1;
    *(short *)(iVar3 + 0xe) = (short)iVar2;
    CString::CString((CString *)&param_1);
    local_4 = 0;
    CString::Format((CString *)(int)*(short *)(iVar3 + 8),(char *)&param_1);
    dmSetChar(param_2);
    dmSetChar2((int)*(short *)(iVar3 + 6),(int)*(short *)(iVar3 + 0x36),(int)*(short *)(iVar3 + 0xc)
               ,(int)*(short *)(iVar3 + 0xe),(int)*(short *)(iVar3 + 0x10),
               (int)*(short *)(iVar3 + 0x38));
    *(int *)((int)this + 0x124) = *(short *)(iVar3 + 0xc) + -10;
    *(int *)((int)this + 0x128) = *(short *)(iVar3 + 0xe) + -7;
    CString::~CString((CString *)&param_2);
  }
  ExceptionList = local_8;
  return;
}

