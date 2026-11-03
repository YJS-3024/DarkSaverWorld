
void __thiscall FUN_004161e0(void *this,dword arg0)

{
  char cVar1;
  byte bVar2;
  dword dVar3;
  byte *pbVar4;
  int iVar5;
  dword extraout_ECX;
  undefined3 uVar7;
  uint uVar6;
  int unaff_EBX;
  dword dVar8;
  byte *pbVar9;
  char *pcVar10;
  char *pcVar11;
  bool bVar12;
  byte *local_124;
  CString local_120 [4];
  int local_11c;
  undefined1 *local_118;
  CString local_114 [4];
  char local_110 [256];
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047bf01;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  local_124 = this;
  CString::CString((CString *)&local_11c,arg0);
  local_4 = 0;
  CString::CString(local_120);
  local_4 = CONCAT31(local_4._1_3_,1);
  dVar8 = 7;
  do {
    dVar3 = CString::Find((CString *)&local_11c,0x2c,dVar8);
    dVar8 = CString::Mid((CString *)&local_11c,(dword)local_110,dVar8,dVar3 - dVar8);
    local_8._0_1_ = 2;
    CString::operator=((CString *)&local_124,dVar8);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
    CString::~CString(local_114);
    pbVar9 = &DAT_0048f190;
    pbVar4 = local_124;
    dVar8 = extraout_ECX;
    do {
      bVar2 = *pbVar4;
      uVar7 = (undefined3)(dVar8 >> 8);
      dVar8 = CONCAT31(uVar7,bVar2);
      bVar12 = bVar2 < *pbVar9;
      if (bVar2 != *pbVar9) {
LAB_004162a7:
        iVar5 = (1 - (uint)bVar12) - (uint)(bVar12 != 0);
        goto LAB_004162ac;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar4[1];
      dVar8 = CONCAT31(uVar7,bVar2);
      bVar12 = bVar2 < pbVar9[1];
      if (bVar2 != pbVar9[1]) goto LAB_004162a7;
      pbVar4 = pbVar4 + 2;
      pbVar9 = pbVar9 + 2;
    } while (bVar2 != 0);
    iVar5 = 0;
LAB_004162ac:
    if (iVar5 == 0) break;
    local_118 = &stack0xfffffec4;
    CString::CString((CString *)&stack0xfffffec4,(dword)&local_124);
    thunk_FUN_00416ab0((void *)(unaff_EBX + 0x174),dVar8);
    dVar8 = dVar3 + 1;
  } while( true );
  dVar8 = arg0 + dVar3 + 2;
  sscanf(dVar8,0x48ec0c);
  sscanf(dVar8,0x48f18c,&local_11c);
  uVar6 = 0xffffffff;
  pcVar10 = local_110;
  do {
    if (uVar6 == 0) break;
    uVar6 = uVar6 - 1;
    cVar1 = *pcVar10;
    pcVar10 = pcVar10 + 1;
  } while (cVar1 != '\0');
  dVar8 = ~uVar6 + 1 + dVar8;
  iVar5 = 0;
  if (0 < local_11c) {
    do {
      pcVar10 = local_110;
      sscanf(dVar8,0x48f188);
      uVar6 = 0xffffffff;
      local_118 = &stack0xfffffec4;
      pcVar11 = local_110;
      do {
        if (uVar6 == 0) break;
        uVar6 = uVar6 - 1;
        cVar1 = *pcVar11;
        pcVar11 = pcVar11 + 1;
      } while (cVar1 != '\0');
      dVar8 = dVar8 + ~uVar6 + 1;
      CString::CString((CString *)&stack0xfffffec4,(dword)local_110);
      thunk_FUN_00416ab0((void *)(unaff_EBX + 0x188),(dword)pcVar10);
      iVar5 = iVar5 + 1;
    } while (iVar5 < local_11c);
  }
  local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
  CString::~CString((CString *)&local_124);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_120);
  ExceptionList = local_10;
  return;
}

