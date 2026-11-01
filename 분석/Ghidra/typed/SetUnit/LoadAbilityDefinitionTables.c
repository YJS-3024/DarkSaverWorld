/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10001420; source SHA256 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca
 */

/* Read ability.atr or ability1.atr from GameRes\set.arc according to the original selection flag.
   The file literals and original sscanf record stores establish common ability definitions, not
   per-scenario initial coordinates.
   Source SHA256: 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca */

undefined4 LoadAbilityDefinitionTables(undefined4 param_1)

{
  char cVar1;
  undefined1 uVar2;
  char *pcVar3;
  char *pcVar4;
  char *pcVar5;
  CString *pCVar6;
  undefined4 uVar7;
  int iVar8;
  int iVar9;
  int iVar10;
  uint uVar11;
  uint uVar12;
  int iVar13;
  int *piVar14;
  int *piVar15;
  char *pcVar16;
  char *local_2ec;
  char *local_2e8;
  CString local_2e4 [4];
  int local_2e0;
  CString local_2dc [4];
  char *local_2d8;
  char *local_2d4;
  char *local_2d0;
  CString local_2cc [4];
  CString local_2c8 [4];
  CStdioFile local_2c4 [20];
  int local_2b0;
  undefined1 local_2ac [4];
  undefined1 local_2a8 [4];
  undefined1 local_2a4 [4];
  undefined1 local_2a0 [4];
  undefined1 local_29c [4];
  undefined1 local_298 [4];
  undefined1 local_294 [4];
  undefined1 local_290 [4];
  undefined1 local_28c [4];
  undefined1 local_288 [4];
  undefined1 local_284 [4];
  undefined1 local_280 [4];
  undefined1 local_27c [4];
  undefined1 local_278 [4];
  undefined1 local_274 [4];
  undefined1 local_270 [4];
  undefined1 local_26c [4];
  undefined1 local_268 [4];
  undefined1 local_264 [4];
  undefined1 local_260 [4];
  undefined1 local_25c [4];
  undefined1 local_258 [4];
  undefined1 local_254 [4];
  undefined1 local_250 [4];
  undefined1 local_24c [4];
  undefined1 local_248 [4];
  undefined1 local_244 [4];
  undefined1 local_240 [4];
  undefined1 local_23c [4];
  undefined1 local_238 [4];
  undefined1 local_234 [4];
  undefined1 local_230 [544];
  undefined1 local_10 [4];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10003363;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString((CString *)&local_2e8);
  local_4 = 0;
  pcVar3 = operator_new(0x200);
  pcVar4 = operator_new(0x200);
  local_2d0 = pcVar4;
  pcVar5 = operator_new(0x200);
  pcVar16 = pcVar3;
  for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
    pcVar16[0] = '\0';
    pcVar16[1] = '\0';
    pcVar16[2] = '\0';
    pcVar16[3] = '\0';
    pcVar16 = pcVar16 + 4;
  }
  pcVar16 = pcVar4;
  for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
    pcVar16[0] = '\0';
    pcVar16[1] = '\0';
    pcVar16[2] = '\0';
    pcVar16[3] = '\0';
    pcVar16 = pcVar16 + 4;
  }
  pcVar16 = pcVar5;
  for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
    pcVar16[0] = '\0';
    pcVar16[1] = '\0';
    pcVar16[2] = '\0';
    pcVar16[3] = '\0';
    pcVar16 = pcVar16 + 4;
  }
  local_2d4 = pcVar5;
  CString::CString(local_2dc,s_GameRes_set_arc_10005088);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_2ec);
  local_4._0_1_ = 2;
  switch(param_1) {
  default:
    pcVar16 = s_ability_atr_1000511c;
    break;
  case 1:
    pcVar16 = s_ability1_atr_10005128;
  }
  CString::operator=((CString *)&local_2ec,pcVar16);
  pCVar6 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&local_2e0,local_2dc);
  local_4._0_1_ = 3;
  CString::operator=((CString *)&local_2ec,pCVar6);
  local_4._0_1_ = 2;
  CString::~CString((CString *)&local_2e0);
  CStdioFile::CStdioFile(local_2c4);
  local_4._0_1_ = 4;
  iVar9 = CStdioFile::Open(local_2c4,local_2ec,0x4000,(CFileException *)0x0);
  if (iVar9 == 0) {
    operator_delete(pcVar3);
    operator_delete(pcVar4);
    operator_delete(pcVar5);
    local_4._0_1_ = 2;
    CStdioFile::~CStdioFile(local_2c4);
    local_4._0_1_ = 1;
    CString::~CString((CString *)&local_2ec);
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString(local_2dc);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_2e8);
    uVar7 = 0;
  }
  else {
    CStdioFile::ReadString(local_2c4,(CString *)&local_2e8);
    local_2e0 = 0;
    do {
      iVar9 = CStdioFile::ReadString(local_2c4,(CString *)&local_2e8);
      if (iVar9 == 0) break;
      if (*(int *)(local_2e8 + -8) != 0) {
        CString::TrimLeft((CString *)&local_2e8);
        CString::TrimRight((CString *)&local_2e8);
        pcVar16 = pcVar3;
        for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
          pcVar16[0] = '\0';
          pcVar16[1] = '\0';
          pcVar16[2] = '\0';
          pcVar16[3] = '\0';
          pcVar16 = pcVar16 + 4;
        }
        pcVar16 = pcVar4;
        for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
          pcVar16[0] = '\0';
          pcVar16[1] = '\0';
          pcVar16[2] = '\0';
          pcVar16[3] = '\0';
          pcVar16 = pcVar16 + 4;
        }
        pcVar16 = pcVar5;
        for (iVar9 = 0x80; iVar9 != 0; iVar9 = iVar9 + -1) {
          pcVar16[0] = '\0';
          pcVar16[1] = '\0';
          pcVar16[2] = '\0';
          pcVar16[3] = '\0';
          pcVar16 = pcVar16 + 4;
        }
        sscanf(local_2e8,s__d__d__d__d__d__d__d__d__d__d__d_100050a8,&local_2b0,local_2ac,local_2a8,
               local_2a4,local_2a0,local_29c,local_298,local_294,local_290,local_28c,local_288,
               local_284,local_280,local_27c,local_278,local_274,local_270,local_26c,local_268,
               local_264,local_260,local_25c,local_258,local_254,local_250,local_24c,local_248,
               local_10,local_244,local_240,local_23c,local_238,local_234,local_230,pcVar3,pcVar4,
               pcVar5);
        if (local_2b0 == 999) break;
        iVar13 = 1;
        iVar9 = 0;
        piVar14 = &local_2b0;
        piVar15 = (int *)(&DAT_101c0160 + local_2b0 * 0x2a4);
        for (iVar10 = 0xa9; iVar10 != 0; iVar10 = iVar10 + -1) {
          *piVar15 = *piVar14;
          piVar14 = piVar14 + 1;
          piVar15 = piVar15 + 1;
        }
        uVar11 = 0xffffffff;
        pcVar5 = pcVar3;
        do {
          if (uVar11 == 0) break;
          uVar11 = uVar11 - 1;
          cVar1 = *pcVar5;
          pcVar5 = pcVar5 + 1;
        } while (cVar1 != '\0');
        if (0 < (int)(~uVar11 - 1)) {
          do {
            if (pcVar3[iVar9] == ',') {
              pcVar3[iVar9] = ' ';
              iVar13 = iVar13 + 1;
            }
            iVar9 = iVar9 + 1;
          } while (iVar9 < (int)(~uVar11 - 1));
        }
        piVar14 = (int *)(&DAT_101c01e4 + local_2b0 * 0x2a4);
        for (iVar9 = 7; iVar9 != 0; iVar9 = iVar9 + -1) {
          *piVar14 = 0;
          piVar14 = piVar14 + 1;
        }
        *(int *)(&DAT_101c01e4 + local_2b0 * 0x2a4) = iVar13;
        CString::CString(local_2e4,pcVar3);
        local_4._0_1_ = 5;
        CString::CString((CString *)&local_2d8);
        iVar9 = 1;
        local_4._0_1_ = 6;
        uVar2 = (undefined1)local_4;
        local_4._0_1_ = 6;
        if (0 < iVar13) {
          do {
            pCVar6 = (CString *)GetStrHead((char *)local_2c8);
            local_4._0_1_ = 7;
            CString::operator=((CString *)&local_2d8,pCVar6);
            local_4._0_1_ = 6;
            CString::~CString(local_2c8);
            pCVar6 = (CString *)GetStrTails((char *)local_2cc);
            local_4._0_1_ = 8;
            CString::operator=(local_2e4,pCVar6);
            local_4._0_1_ = 6;
            CString::~CString(local_2cc);
            iVar8 = atoi(local_2d8);
            iVar10 = iVar9 + local_2b0 * 0xa9;
            iVar9 = iVar9 + 1;
            *(int *)(&DAT_101c01e4 + iVar10 * 4) = iVar8;
            uVar2 = (undefined1)local_4;
          } while (iVar9 <= iVar13);
        }
        local_4._0_1_ = uVar2;
        uVar11 = 0xffffffff;
        pcVar5 = local_2d0;
        do {
          pcVar4 = pcVar5;
          if (uVar11 == 0) break;
          uVar11 = uVar11 - 1;
          pcVar4 = pcVar5 + 1;
          cVar1 = *pcVar5;
          pcVar5 = pcVar4;
        } while (cVar1 != '\0');
        uVar11 = ~uVar11;
        pcVar5 = pcVar4 + -uVar11;
        pcVar4 = &DAT_101c0200 + local_2b0 * 0x2a4;
        for (uVar12 = uVar11 >> 2; uVar12 != 0; uVar12 = uVar12 - 1) {
          *(undefined4 *)pcVar4 = *(undefined4 *)pcVar5;
          pcVar5 = pcVar5 + 4;
          pcVar4 = pcVar4 + 4;
        }
        for (uVar11 = uVar11 & 3; uVar11 != 0; uVar11 = uVar11 - 1) {
          *pcVar4 = *pcVar5;
          pcVar5 = pcVar5 + 1;
          pcVar4 = pcVar4 + 1;
        }
        FUN_10001160(&DAT_101c0200 + local_2b0 * 0x2a4);
        uVar11 = 0xffffffff;
        pcVar5 = local_2d4;
        do {
          pcVar4 = pcVar5;
          if (uVar11 == 0) break;
          uVar11 = uVar11 - 1;
          pcVar4 = pcVar5 + 1;
          cVar1 = *pcVar5;
          pcVar5 = pcVar4;
        } while (cVar1 != '\0');
        uVar11 = ~uVar11;
        pcVar5 = pcVar4 + -uVar11;
        pcVar4 = &DAT_101c0300 + local_2b0 * 0x2a4;
        for (uVar12 = uVar11 >> 2; uVar12 != 0; uVar12 = uVar12 - 1) {
          *(undefined4 *)pcVar4 = *(undefined4 *)pcVar5;
          pcVar5 = pcVar5 + 4;
          pcVar4 = pcVar4 + 4;
        }
        for (uVar11 = uVar11 & 3; uVar11 != 0; uVar11 = uVar11 - 1) {
          *pcVar4 = *pcVar5;
          pcVar5 = pcVar5 + 1;
          pcVar4 = pcVar4 + 1;
        }
        FUN_10001160(&DAT_101c0300 + local_2b0 * 0x2a4);
        local_4._0_1_ = 5;
        CString::~CString((CString *)&local_2d8);
        local_4._0_1_ = 4;
        CString::~CString(local_2e4);
        pcVar5 = local_2d4;
        pcVar4 = local_2d0;
      }
      local_2e0 = local_2e0 + 1;
    } while (local_2e0 < 1000);
    operator_delete(pcVar3);
    operator_delete(pcVar4);
    operator_delete(pcVar5);
    CStdioFile::Close(local_2c4);
    BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&local_2ec);
    local_4._0_1_ = 2;
    CStdioFile::~CStdioFile(local_2c4);
    local_4._0_1_ = 1;
    CString::~CString((CString *)&local_2ec);
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString(local_2dc);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_2e8);
    uVar7 = 1;
  }
  ExceptionList = local_c;
  return uVar7;
}

