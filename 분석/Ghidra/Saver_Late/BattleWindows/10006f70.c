
void __cdecl FUN_10006f70(int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  int iVar3;
  undefined4 local_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015068;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_10);
  iVar3 = 0;
  local_4 = 0;
  if (DAT_1001c4f4 == 0) {
    FUN_10006da0();
    DAT_1001c4f4 = 1;
  }
  iVar1 = param_2 + 0x37;
  do {
    if ((int)DAT_1006bd10 <= DAT_1001c4b8 + iVar3) break;
    iVar2 = (DAT_1001c4b8 + iVar3) * 0x2a;
    CString::operator=((CString *)&local_10,(char *)((int)&DAT_1006bd14 + iVar2));
    iVar2 = getsnum(*(short *)(&DAT_1006bd12 + iVar2) * 0x32);
    if ((iVar2 == DAT_1001a1cc) || (iVar2 == DAT_1001a1d0)) {
      (*DAT_1001c44c)(3,iVar3 + 2,1);
      (*DAT_1001c450)(DAT_1001c470,param_1 + 0x14,iVar1,local_10,0xffffff,0);
    }
    if (iVar2 == DAT_1001a1d4) {
      (*DAT_1001c44c)(3,iVar3 + 2,0);
      (*DAT_1001c450)(DAT_1001c470,param_1 + 0x14,iVar1,local_10,0x80808,0);
    }
    iVar3 = iVar3 + 1;
    iVar1 = iVar1 + 0x13;
  } while (iVar3 < 4);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_10);
  ExceptionList = pvStack_c;
  return;
}

