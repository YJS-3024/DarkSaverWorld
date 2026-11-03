
void __cdecl FUN_10006ba0(int param_1,int param_2)

{
  int iVar1;
  CString *this;
  int iVar2;
  character *pcVar3;
  int iVar4;
  tileattrrec *ptVar5;
  CString *this_00;
  uint uVar6;
  CString local_20 [4];
  undefined1 local_1c [4];
  LONG LStack_18;
  LONG LStack_14;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015008;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_20);
  iVar4 = DAT_1001c460 + DAT_1001a8e8;
  local_4 = 0;
  iVar1 = DAT_1001c464 + DAT_1001a8e4;
  SetRect((LPRECT)local_1c,0x8a,0x69,0xaa,0x89);
  CDDSurface::BltFast(DAT_1001c470,param_1 + 10,param_2 + 0x24,DAT_1001c484,local_1c,0);
  iVar2 = DAT_1001a380;
  if (DAT_1001a380 < 0) {
    if (DAT_1006a738 < 0) goto LAB_10006d7b;
    iVar2 = DAT_1006a738 * 0x32;
  }
  pcVar3 = GetUnitInfoP(iVar2);
  iVar4 = GetAttrTable3(iVar4,iVar1);
  ptVar5 = GetAttrTable2(*(int *)(pcVar3 + 0x1c),iVar4);
  if (DAT_1001c450 != (code *)0x0) {
    this = *(CString **)ptVar5;
    if (((uint)this ^ (int)this >> 0x1f) - ((int)this >> 0x1f) == 99) {
      CString::operator=(local_20,&DAT_1001a298);
    }
    else {
      CString::Format(this,(char *)local_20);
    }
    iVar4 = param_2 + 0x51;
    (*DAT_1001c450)(DAT_1001c470,iVar4,param_2 + 0x21,local_1c,0x80808,1);
    uVar6 = (int)*(uint *)(ptVar5 + 4) >> 0x1f;
    if ((*(uint *)(ptVar5 + 4) ^ uVar6) - uVar6 == 99) {
      CString::operator=((CString *)local_1c,&DAT_1001a298);
    }
    else {
      CString::Format((CString *)local_1c,(char *)local_1c);
    }
    (*DAT_1001c450)(DAT_1001c470,iVar4,param_2 + 0x32,LStack_18,0x80808,1);
    uVar6 = (int)*(uint *)(ptVar5 + 8) >> 0x1f;
    if ((*(uint *)(ptVar5 + 8) ^ uVar6) - uVar6 == 99) {
      CString::operator=((CString *)&LStack_18,&DAT_1001a298);
    }
    else {
      CString::Format(this_00,(char *)&LStack_18);
    }
    (*DAT_1001c450)(DAT_1001c470,iVar4,param_2 + 0x43,LStack_14,0x80808,1);
  }
LAB_10006d7b:
  local_4 = 0xffffffff;
  CString::~CString(local_20);
  ExceptionList = pvStack_c;
  return;
}

