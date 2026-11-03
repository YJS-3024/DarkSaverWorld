
void FUN_004161e0(char *param_1)

{
  char cVar1;
  byte bVar2;
  int iVar3;
  CString *pCVar4;
  byte *pbVar5;
  uint uVar6;
  int unaff_EBX;
  char *pcVar7;
  int iVar8;
  byte *pbVar9;
  char *pcVar10;
  bool bVar11;
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
  CString::CString((CString *)&local_11c,param_1);
  local_4 = 0;
  CString::CString(local_120);
  local_4 = CONCAT31(local_4._1_3_,1);
  iVar8 = 7;
  do {
    iVar3 = CString::Find((CString *)&local_11c,',',iVar8);
    pCVar4 = (CString *)CString::Mid((CString *)&local_11c,(int)local_110,iVar8);
    local_8._0_1_ = 2;
    CString::operator=((CString *)&local_124,pCVar4);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
    CString::~CString(local_114);
    pbVar9 = &DAT_0048f190;
    pbVar5 = local_124;
    do {
      bVar2 = *pbVar5;
      bVar11 = bVar2 < *pbVar9;
      if (bVar2 != *pbVar9) {
LAB_004162a7:
        iVar8 = (1 - (uint)bVar11) - (uint)(bVar11 != 0);
        goto LAB_004162ac;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar5[1];
      bVar11 = bVar2 < pbVar9[1];
      if (bVar2 != pbVar9[1]) goto LAB_004162a7;
      pbVar5 = pbVar5 + 2;
      pbVar9 = pbVar9 + 2;
    } while (bVar2 != 0);
    iVar8 = 0;
LAB_004162ac:
    if (iVar8 == 0) break;
    local_118 = &stack0xfffffec4;
    CString::CString((CString *)&stack0xfffffec4,(CString *)&local_124);
    thunk_FUN_00416ab0((void *)(unaff_EBX + 0x174));
    iVar8 = iVar3 + 1;
  } while( true );
  pcVar7 = param_1 + iVar3 + 2;
  sscanf(pcVar7,&DAT_0048ec0c);
  sscanf(pcVar7,&DAT_0048f18c,&local_11c);
  uVar6 = 0xffffffff;
  pcVar10 = local_110;
  do {
    if (uVar6 == 0) break;
    uVar6 = uVar6 - 1;
    cVar1 = *pcVar10;
    pcVar10 = pcVar10 + 1;
  } while (cVar1 != '\0');
  pcVar7 = pcVar7 + ~uVar6 + 1;
  iVar8 = 0;
  if (0 < local_11c) {
    do {
      sscanf(pcVar7,&DAT_0048f188);
      uVar6 = 0xffffffff;
      local_118 = &stack0xfffffec4;
      pcVar10 = local_110;
      do {
        if (uVar6 == 0) break;
        uVar6 = uVar6 - 1;
        cVar1 = *pcVar10;
        pcVar10 = pcVar10 + 1;
      } while (cVar1 != '\0');
      pcVar7 = pcVar7 + ~uVar6 + 1;
      CString::CString((CString *)&stack0xfffffec4,local_110);
      thunk_FUN_00416ab0((void *)(unaff_EBX + 0x188));
      iVar8 = iVar8 + 1;
    } while (iVar8 < local_11c);
  }
  local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
  CString::~CString((CString *)&local_124);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_120);
  ExceptionList = local_10;
  return;
}

