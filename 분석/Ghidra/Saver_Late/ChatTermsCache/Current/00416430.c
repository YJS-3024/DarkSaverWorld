
undefined4 FUN_00416430(HWND param_1)

{
  byte bVar1;
  int iVar2;
  CString *pCVar3;
  byte *pbVar4;
  int iVar5;
  byte *pbVar6;
  int iVar7;
  bool bVar8;
  byte *local_114;
  CString local_110 [4];
  byte local_10c [256];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047bf36;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString((CString *)&local_114);
  local_4 = 0;
  GetClassNameA(param_1,(LPSTR)local_10c,0xff);
  iVar2 = *(int *)(DAT_004952a0 + 400);
  if (iVar2 == 0) {
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_114);
  }
  else {
    iVar7 = 0;
    if (0 < iVar2) {
      do {
        CString::operator=((CString *)&local_114,&DAT_00492a60);
        pCVar3 = thunk_FUN_00416a70((void *)(DAT_004952a0 + 0x188),local_110,iVar7);
        local_4._0_1_ = 1;
        CString::operator=((CString *)&local_114,pCVar3);
        local_4 = (uint)local_4._1_3_ << 8;
        CString::~CString(local_110);
        pbVar4 = local_10c;
        pbVar6 = local_114;
        do {
          bVar1 = *pbVar4;
          bVar8 = bVar1 < *pbVar6;
          if (bVar1 != *pbVar6) {
LAB_00416526:
            iVar5 = (1 - (uint)bVar8) - (uint)(bVar8 != 0);
            goto LAB_0041652b;
          }
          if (bVar1 == 0) break;
          bVar1 = pbVar4[1];
          bVar8 = bVar1 < pbVar6[1];
          if (bVar1 != pbVar6[1]) goto LAB_00416526;
          pbVar4 = pbVar4 + 2;
          pbVar6 = pbVar6 + 2;
        } while (bVar1 != 0);
        iVar5 = 0;
LAB_0041652b:
        if (iVar5 == 0) {
          PostMessageA(*(HWND *)(DAT_004952a0 + 0x20),0x466,1,0);
        }
        iVar7 = iVar7 + 1;
      } while (iVar7 < iVar2);
    }
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_114);
  }
  ExceptionList = local_c;
  return 1;
}

