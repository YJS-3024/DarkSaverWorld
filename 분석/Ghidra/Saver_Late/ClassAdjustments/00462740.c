
void __thiscall FUN_00462740(void *this,int param_1)

{
  CString *this_00;
  newmagicrecord *pnVar1;
  CString *this_01;
  CString *extraout_ECX;
  CString *extraout_ECX_00;
  CString *pCVar2;
  char *pcVar3;
  void *local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_00480038;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  local_10 = this;
  CString::Empty((CString *)((int)this + 0x130));
  pCVar2 = (CString *)((int)this + 0x134);
  CString::Empty(pCVar2);
  this_00 = (CString *)((int)this + 0x138);
  CString::Empty(this_00);
  if (param_1 != -1) {
    pnVar1 = GetMagicTbl2(param_1);
    if ((pnVar1 != (newmagicrecord *)0x0) && (*(int *)pnVar1 == param_1)) {
      CString::operator=((CString *)((int)this + 0x130),(char *)(pnVar1 + 4));
      CString::operator=(pCVar2,&DAT_00490d8c);
      if (*(int *)(pnVar1 + 0x118) == 0x22) {
        pcVar3 = &DAT_00490d80;
      }
      else {
        pcVar3 = &DAT_00490d74;
      }
      CString::operator+=(pCVar2,pcVar3);
      CString::Empty(this_00);
      CString::CString((CString *)&local_10);
      local_4 = 0;
      pCVar2 = this_01;
      if (0 < *(int *)(pnVar1 + 0x110)) {
        CString::Format(this_01,(char *)&local_10);
        CString::operator+=(this_00,(CString *)&local_c);
        pCVar2 = extraout_ECX;
      }
      if (0 < *(int *)(pnVar1 + 0x108)) {
        CString::Format((CString *)&local_c,(char *)&local_c);
        CString::operator+=(this_00,(CString *)&local_8);
        pCVar2 = extraout_ECX_00;
      }
      if (0 < *(int *)(pnVar1 + 0x10c)) {
        CString::Format(pCVar2,(char *)&local_c);
        CString::operator+=(this_00,(CString *)&local_8);
      }
      CString::~CString((CString *)&local_c);
    }
  }
  ExceptionList = local_8;
  return;
}

