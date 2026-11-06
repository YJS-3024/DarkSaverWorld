
void __cdecl FUN_100027d0(CString *param_1,int param_2)

{
  byte bVar1;
  char *pcVar2;
  CString *pCVar3;
  int iVar4;
  byte *pbVar5;
  byte *pbVar6;
  int iVar7;
  undefined4 *puVar8;
  CString *this;
  byte *pbVar9;
  bool bVar10;
  char *local_70;
  CString local_6c [4];
  CString local_68 [4];
  CString local_64 [4];
  CString local_60 [4];
  CString local_5c [80];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10011cdb;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  FUN_10011a3e(local_5c,4,0x14,CString::CString);
  local_4 = 0;
  iVar4 = *(int *)(*(int *)param_1 + -8);
  pcVar2 = CString::GetBuffer(param_1,iVar4);
  iVar7 = 0;
  if (0 < iVar4) {
    do {
      if (pcVar2[iVar7] == ',') {
        pcVar2[iVar7] = ' ';
      }
      iVar7 = iVar7 + 1;
    } while (iVar7 < iVar4);
  }
  CString::ReleaseBuffer(param_1,-1);
  CString::CString((CString *)&stack0xffffff74,param_1);
  GetStrTail();
  CString::CString((CString *)&local_70);
  local_4._0_1_ = 1;
  param_1 = (CString *)0x0;
  this = local_5c;
  *(undefined4 *)(param_2 + 0x1c) = 0x14;
  puVar8 = (undefined4 *)(param_2 + 0x24);
  do {
    iVar4 = *(int *)(*(int *)this + -8);
    if (iVar4 == 0) {
      *(CString **)(param_2 + 0x1c) = param_1;
      break;
    }
    pcVar2 = CString::GetBuffer(this,iVar4);
    iVar7 = 0;
    if (0 < iVar4) {
      do {
        if ((pcVar2[iVar7] == '(') || (pcVar2[iVar7] == ')')) {
          pcVar2[iVar7] = ' ';
        }
        iVar7 = iVar7 + 1;
      } while (iVar7 < iVar4);
    }
    CString::ReleaseBuffer(this,-1);
    CString::TrimLeft(this);
    CString::TrimRight(this);
    pCVar3 = (CString *)GetStrTails((char *)local_6c);
    local_4._0_1_ = 2;
    CString::operator=((CString *)&local_70,pCVar3);
    local_4._0_1_ = 1;
    CString::~CString(local_6c);
    pCVar3 = (CString *)GetStrHead((char *)local_68);
    local_4._0_1_ = 3;
    CString::operator=(this,pCVar3);
    local_4._0_1_ = 1;
    CString::~CString(local_68);
    iVar4 = atoi(local_70);
    pbVar6 = *(byte **)this;
    puVar8[-1] = iVar4;
    pbVar9 = &DAT_1001604c;
    pbVar5 = pbVar6;
    do {
      bVar1 = *pbVar5;
      bVar10 = bVar1 < *pbVar9;
      if (bVar1 != *pbVar9) {
LAB_1000296c:
        iVar4 = (1 - (uint)bVar10) - (uint)(bVar10 != 0);
        goto LAB_10002971;
      }
      if (bVar1 == 0) break;
      bVar1 = pbVar5[1];
      bVar10 = bVar1 < pbVar9[1];
      if (bVar1 != pbVar9[1]) goto LAB_1000296c;
      pbVar5 = pbVar5 + 2;
      pbVar9 = pbVar9 + 2;
    } while (bVar1 != 0);
    iVar4 = 0;
LAB_10002971:
    if (iVar4 == 0) {
      *puVar8 = 0;
    }
    else {
      pcVar2 = s_ATTACK_10016044;
      pbVar5 = pbVar6;
      do {
        bVar1 = *pbVar5;
        bVar10 = bVar1 < (byte)*pcVar2;
        if (bVar1 != *pcVar2) {
LAB_100029a7:
          iVar4 = (1 - (uint)bVar10) - (uint)(bVar10 != 0);
          goto LAB_100029ac;
        }
        if (bVar1 == 0) break;
        bVar1 = pbVar5[1];
        bVar10 = bVar1 < (byte)pcVar2[1];
        if (bVar1 != pcVar2[1]) goto LAB_100029a7;
        pbVar5 = pbVar5 + 2;
        pcVar2 = pcVar2 + 2;
      } while (bVar1 != 0);
      iVar4 = 0;
LAB_100029ac:
      if (iVar4 == 0) {
        *puVar8 = 1;
      }
      else {
        iVar4 = *(int *)(pbVar6 + -8);
        pcVar2 = CString::GetBuffer(this,iVar4);
        iVar7 = 0;
        if (0 < iVar4) {
          do {
            if (pcVar2[iVar7] == '_') {
              pcVar2[iVar7] = ' ';
            }
            iVar7 = iVar7 + 1;
          } while (iVar7 < iVar4);
        }
        CString::ReleaseBuffer(this,-1);
        pCVar3 = (CString *)GetStrTails((char *)local_64);
        local_4._0_1_ = 4;
        CString::operator=((CString *)&local_70,pCVar3);
        local_4._0_1_ = 1;
        CString::~CString(local_64);
        pCVar3 = (CString *)GetStrHead((char *)local_60);
        local_4._0_1_ = 5;
        CString::operator=(this,pCVar3);
        local_4._0_1_ = 1;
        CString::~CString(local_60);
        pbVar6 = *(byte **)this;
        pcVar2 = s_MAGIC_1001603c;
        pbVar5 = pbVar6;
        do {
          bVar1 = *pbVar5;
          bVar10 = bVar1 < (byte)*pcVar2;
          if (bVar1 != *pcVar2) {
LAB_10002a71:
            iVar4 = (1 - (uint)bVar10) - (uint)(bVar10 != 0);
            goto LAB_10002a76;
          }
          if (bVar1 == 0) break;
          bVar1 = pbVar5[1];
          bVar10 = bVar1 < (byte)pcVar2[1];
          if (bVar1 != pcVar2[1]) goto LAB_10002a71;
          pbVar5 = pbVar5 + 2;
          pcVar2 = pcVar2 + 2;
        } while (bVar1 != 0);
        iVar4 = 0;
LAB_10002a76:
        if (iVar4 != 0) {
          pbVar5 = &DAT_10016034;
          do {
            bVar1 = *pbVar6;
            bVar10 = bVar1 < *pbVar5;
            if (bVar1 != *pbVar5) {
LAB_10002aa5:
              iVar4 = (1 - (uint)bVar10) - (uint)(bVar10 != 0);
              goto LAB_10002aaa;
            }
            if (bVar1 == 0) break;
            bVar1 = pbVar6[1];
            bVar10 = bVar1 < pbVar5[1];
            if (bVar1 != pbVar5[1]) goto LAB_10002aa5;
            pbVar6 = pbVar6 + 2;
            pbVar5 = pbVar5 + 2;
          } while (bVar1 != 0);
          iVar4 = 0;
LAB_10002aaa:
          if (iVar4 != 0) goto LAB_10002ac6;
        }
        *puVar8 = 2;
        iVar4 = FUN_1000f4e0(local_70,-1);
        puVar8[1] = iVar4;
      }
    }
LAB_10002ac6:
    this = this + 4;
    param_1 = param_1 + 1;
    puVar8 = puVar8 + 3;
  } while ((int)param_1 < 0x14);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString((CString *)&local_70);
  local_4 = 0xffffffff;
  FUN_1001194a(local_5c,4,0x14,CString::~CString);
  ExceptionList = local_c;
  return;
}


/* incoming refs
10002df9 UNCONDITIONAL_CALL
CALLER 10002b40 LoadScenarioAiRules
  10014174 DATA
  Entry Point EXTERNAL
  1000135f UNCONDITIONAL_CALL
*/
