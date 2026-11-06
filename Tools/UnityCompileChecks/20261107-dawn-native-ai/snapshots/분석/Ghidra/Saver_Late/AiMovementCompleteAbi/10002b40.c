
/* Original export readai(). Opens Map\<scenario>set.arc and reads <scenario>.ai. Interpret AI
   commands separately from a recovered SET/POS battle roster.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl LoadScenarioAiRules(void)

{
  byte bVar1;
  undefined4 *puVar2;
  CString *pCVar3;
  int iVar4;
  byte *pbVar5;
  int *piVar6;
  char *pcVar7;
  bool bVar8;
  CString local_5c [4];
  CString local_58 [4];
  char *local_54;
  CString local_50 [4];
  CString local_4c [4];
  CString local_48 [4];
  undefined1 *local_44;
  CStdioFile local_40 [12];
  undefined1 local_34 [8];
  char *local_2c;
  byte *local_28;
  CString local_24 [4];
  CString local_20 [20];
  void *pvStack_c;
  undefined1 *puStack_8;
  void *local_4;
  
                    /* 0x2b40  100  ?readai@@YAXXZ */
  local_4 = (void *)0xffffffff;
  puStack_8 = &LAB_10011d2b;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_58);
  local_4 = (void *)0x0;
  FUN_10011a3e(local_34,4,10,CString::CString);
  local_4._0_1_ = 1;
  puVar2 = &DAT_100d99a0;
  do {
    *puVar2 = 999;
    puVar2 = puVar2 + 0x44;
  } while ((int)puVar2 < 0x100e03e0);
  CString::CString((CString *)&local_54);
  local_4._0_1_ = 2;
  CString::CString(local_5c);
  local_4 = (void *)CONCAT31(local_4._1_3_,3);
  CString::Format(DAT_100e0408,(char *)&local_54);
  CString::Format(local_58,(char *)local_58);
  pCVar3 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_1010b3e0,local_48,local_4c);
  CString::operator=((CString *)&local_54,pCVar3);
  CString::~CString(local_48);
  if (*(int *)(local_54 + -8) == 0) {
    CString::~CString((CString *)&local_54);
    CString::~CString(local_4c);
  }
  else {
    CStdioFile::CStdioFile(local_40);
    iVar4 = CStdioFile::Open(local_40,local_54,0x4000,(CFileException *)0x0);
    if (iVar4 == 0) {
      CStdioFile::~CStdioFile(local_40);
      CString::~CString((CString *)&local_54);
      CString::~CString(local_4c);
    }
    else {
      iVar4 = CStdioFile::ReadString(local_40,local_50);
      if (iVar4 != 0) {
        piVar6 = &DAT_100d99a0;
        do {
          CString::TrimLeft(local_50);
          CString::TrimRight(local_50);
          local_44 = &stack0xffffff90;
          CString::CString((CString *)&stack0xffffff90,local_50);
          GetStrTail();
          iVar4 = FUN_1000f4e0(local_2c,999);
          *piVar6 = iVar4;
          pcVar7 = s_ODNORMAL_1001606c;
          pbVar5 = local_28;
          do {
            bVar1 = *pbVar5;
            bVar8 = bVar1 < (byte)*pcVar7;
            if (bVar1 != *pcVar7) {
LAB_10002d52:
              iVar4 = (1 - (uint)bVar8) - (uint)(bVar8 != 0);
              goto LAB_10002d57;
            }
            if (bVar1 == 0) break;
            bVar1 = pbVar5[1];
            bVar8 = bVar1 < (byte)pcVar7[1];
            if (bVar1 != pcVar7[1]) goto LAB_10002d52;
            pbVar5 = pbVar5 + 2;
            pcVar7 = pcVar7 + 2;
          } while (bVar1 != 0);
          iVar4 = 0;
LAB_10002d57:
          if (iVar4 == 0) {
            piVar6[1] = 2;
          }
          else {
            pcVar7 = s_ODATTACK_10016060;
            pbVar5 = local_28;
            do {
              bVar1 = *pbVar5;
              bVar8 = bVar1 < (byte)*pcVar7;
              if (bVar1 != *pcVar7) {
LAB_10002d92:
                iVar4 = (1 - (uint)bVar8) - (uint)(bVar8 != 0);
                goto LAB_10002d97;
              }
              if (bVar1 == 0) break;
              bVar1 = pbVar5[1];
              bVar8 = bVar1 < (byte)pcVar7[1];
              if (bVar1 != pcVar7[1]) goto LAB_10002d92;
              pbVar5 = pbVar5 + 2;
              pcVar7 = pcVar7 + 2;
            } while (bVar1 != 0);
            iVar4 = 0;
LAB_10002d97:
            if (iVar4 == 0) {
              piVar6[1] = 4;
            }
            else {
              pcVar7 = s_ODDEFENCE_10016054;
              pbVar5 = local_28;
              do {
                bVar1 = *pbVar5;
                bVar8 = bVar1 < (byte)*pcVar7;
                if (bVar1 != *pcVar7) {
LAB_10002dcf:
                  iVar4 = (1 - (uint)bVar8) - (uint)(bVar8 != 0);
                  goto LAB_10002dd4;
                }
                if (bVar1 == 0) break;
                bVar1 = pbVar5[1];
                bVar8 = bVar1 < (byte)pcVar7[1];
                if (bVar1 != pcVar7[1]) goto LAB_10002dcf;
                pbVar5 = pbVar5 + 2;
                pcVar7 = pcVar7 + 2;
              } while (bVar1 != 0);
              iVar4 = 0;
LAB_10002dd4:
              if (iVar4 == 0) {
                piVar6[1] = 8;
              }
              else {
                *piVar6 = -1;
              }
            }
          }
          FUN_10002670(local_24,(int)piVar6);
          FUN_100027d0(local_20,(int)piVar6);
          if (*piVar6 == 999) break;
          piVar6 = piVar6 + 0x44;
          iVar4 = CStdioFile::ReadString(local_40,local_50);
        } while (iVar4 != 0);
      }
      CStdioFile::Close(local_40);
      BB_CompressDllMng::DeleteTmpFile(DAT_1010b3e0,(CString *)&local_54);
      CStdioFile::~CStdioFile(local_40);
      CString::~CString((CString *)&local_54);
      CString::~CString(local_4c);
    }
  }
  FUN_1001194a(&local_2c,4,10,CString::~CString);
  CString::~CString(local_50);
  ExceptionList = local_4;
  return;
}


/* incoming refs
10014174 DATA
Entry Point EXTERNAL
1000135f UNCONDITIONAL_CALL
CALLER 10001350 setusercurrentscenario
  100141a4 DATA
  Entry Point EXTERNAL
*/
