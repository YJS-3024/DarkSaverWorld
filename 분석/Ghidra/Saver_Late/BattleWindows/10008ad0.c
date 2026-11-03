
void __fastcall FUN_10008ad0(int *param_1)

{
  clock_t cVar1;
  int iVar2;
  int iVar3;
  int local_20;
  tagRECT local_1c;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015218;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_20);
  local_4 = 0;
  if (param_1[0x6b] == 0) {
    FUN_10008e10(param_1[2],param_1[3],param_1[4] + param_1[2],param_1[5] + param_1[3]);
    CString::operator=((CString *)&local_20,(CString *)(param_1 + 7));
    if (*(int *)(local_20 + -8) != 0) {
      (*DAT_1001c450)(DAT_1001c470,param_1[4] / 2 + param_1[2],param_1[3] + 0x17,local_20,0xffffff,
                      100);
      cVar1 = clock();
      if (10000 < cVar1 - param_1[0x6f]) {
        FUN_10008ca0(param_1);
      }
    }
  }
  else if (param_1[0x6b] == 1) {
    FUN_10008e10(param_1[2],param_1[3],param_1[4] + param_1[2],param_1[5] + param_1[3]);
    iVar2 = param_1[2];
    iVar3 = param_1[3];
    SetRect(&local_1c,0x5a,300,0x9e,0x170);
    CDDSurface::BltFast(DAT_1001c470,iVar2 + 0x12,iVar3 + 0x12,DAT_1006a704,&local_1c,0);
    iVar2 = param_1[2];
    iVar3 = param_1[3];
    SetRect(&local_1c,0,0,0x40,0x40);
    CDDSurface::BltFast(DAT_1001c470,iVar2 + 0x14,iVar3 + 0x14,DAT_1001c484,&local_1c,0);
    iVar2 = 0;
    iVar3 = 0;
    do {
      CString::operator=((CString *)&local_20,(CString *)(param_1 + param_1[0x6c] + iVar2 + 7));
      (*DAT_1001c450)(DAT_1001c470,0x96,iVar3 + 0x17 + param_1[3],local_20,0xffffff,0);
      iVar3 = iVar3 + 0x14;
      iVar2 = iVar2 + 1;
    } while (iVar3 < 0x29);
  }
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_20);
  ExceptionList = pvStack_c;
  return;
}

