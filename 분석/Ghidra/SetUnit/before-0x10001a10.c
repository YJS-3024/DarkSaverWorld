/* Ghidra decompilation; not recovered original source. Evidence: ev_2b107590009c36fc91d9898b922e3a734f5fe5de164a85728cc683c3bfc8ef11 */

undefined4 FUN_10001a10(undefined4 param_1)

{
  char cVar1;
  undefined4 *puVar2;
  char *pcVar3;
  char *pcVar4;
  CString *pCVar5;
  undefined4 uVar6;
  int iVar7;
  uint uVar8;
  uint uVar9;
  char *unaff_EBX;
  int *piVar10;
  undefined4 *puVar11;
  int *piVar12;
  char *pcVar13;
  char *pcVar14;
  char *local_2d0;
  CString local_2cc [4];
  int local_2c8;
  undefined4 *local_2c4;
  undefined4 *local_2c0;
  CString local_2bc [20];
  int local_2a8;
  undefined1 local_2a4 [4];
  undefined1 local_2a0 [4];
  undefined1 local_29c [4];
  undefined1 local_298 [4];
  undefined1 local_294 [4];
  undefined1 local_290;
  undefined1 local_28f;
  undefined1 local_28e;
  undefined1 local_28d;
  undefined1 local_28c;
  undefined1 local_28b;
  undefined1 local_28a;
  undefined1 local_289;
  undefined1 local_288;
  undefined1 local_287 [7];
  undefined1 local_280 [8];
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
  undefined1 local_230 [4];
  undefined1 local_22c [4];
  undefined1 local_228 [4];
  undefined1 local_224 [4];
  undefined1 local_220 [4];
  undefined1 local_21c [4];
  undefined4 local_218;
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_100033a7;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_2cc);
  local_4 = 0;
  puVar2 = operator_new(0x200);
  local_2c0 = puVar2;
  pcVar3 = operator_new(0x200);
  pcVar4 = operator_new(0x200);
  puVar11 = puVar2;
  for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
    *puVar11 = 0;
    puVar11 = puVar11 + 1;
  }
  pcVar14 = pcVar3;
  for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
    pcVar14[0] = '\0';
    pcVar14[1] = '\0';
    pcVar14[2] = '\0';
    pcVar14[3] = '\0';
    pcVar14 = pcVar14 + 4;
  }
  pcVar14 = pcVar4;
  for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
    pcVar14[0] = '\0';
    pcVar14[1] = '\0';
    pcVar14[2] = '\0';
    pcVar14[3] = '\0';
    pcVar14 = pcVar14 + 4;
  }
  CString::CString((CString *)&local_2c8,s_GameRes_set_arc_10005088);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_2d0);
  local_4 = CONCAT31(local_4._1_3_,2);
  switch(param_1) {
  default:
    pcVar14 = s_itemtbl_atr_100051c0;
    break;
  case 1:
    pcVar14 = s_itemtbl1_atr_100051cc;
  }
  CString::operator=((CString *)&local_2d0,pcVar14);
  pCVar5 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,local_2bc,(CString *)&local_2c8);
  local_8._0_1_ = 3;
  CString::operator=((CString *)&stack0xfffffd2c,pCVar5);
  local_8._0_1_ = 2;
  CString::~CString((CString *)&local_2c0);
  CStdioFile::CStdioFile((CStdioFile *)local_2bc);
  local_8._0_1_ = 4;
  iVar7 = CStdioFile::Open((CStdioFile *)local_2bc,unaff_EBX,0x4000,(CFileException *)0x0);
  if (iVar7 == 0) {
    operator_delete(puVar2);
    operator_delete(pcVar3);
    operator_delete(pcVar4);
    local_8._0_1_ = 2;
    CStdioFile::~CStdioFile((CStdioFile *)local_2bc);
    local_8._0_1_ = 1;
    CString::~CString((CString *)&stack0xfffffd2c);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString(local_2cc);
    local_8 = (undefined1 *)0xffffffff;
    CString::~CString((CString *)&local_2d0);
    uVar6 = 0;
  }
  else {
    CStdioFile::ReadString((CStdioFile *)local_2bc,(CString *)&local_2d0);
    local_2c8 = 0;
    do {
      iVar7 = CStdioFile::ReadString((CStdioFile *)local_2bc,(CString *)&local_2d0);
      if (iVar7 == 0) break;
      if (*(int *)(local_2d0 + -8) != 0) {
        if (*local_2d0 == ';') break;
        puVar11 = puVar2;
        for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
          *puVar11 = 0;
          puVar11 = puVar11 + 1;
        }
        pcVar14 = pcVar3;
        for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
          pcVar14[0] = '\0';
          pcVar14[1] = '\0';
          pcVar14[2] = '\0';
          pcVar14[3] = '\0';
          pcVar14 = pcVar14 + 4;
        }
        pcVar14 = pcVar4;
        for (iVar7 = 0x80; iVar7 != 0; iVar7 = iVar7 + -1) {
          pcVar14[0] = '\0';
          pcVar14[1] = '\0';
          pcVar14[2] = '\0';
          pcVar14[3] = '\0';
          pcVar14 = pcVar14 + 4;
        }
        sscanf(local_2d0,s__d__d__d__d__d__d__d__d__d__d__d_10005138,&local_2a8,local_2a4,local_2a0,
               local_29c,local_298,local_294,&local_290,&local_28f,&local_28e,&local_28d,&local_28c,
               &local_28b,&local_28a,&local_289,&local_288,local_287,local_280,local_278,local_274,
               local_270,local_26c,local_268,local_264,local_260,local_25c,local_258,local_254,
               local_250,local_24c,local_248,local_244,local_240,local_23c,local_238,local_234,
               local_230,local_22c,local_228,local_224,local_220,local_21c,puVar2,pcVar3,pcVar4);
        local_218 = 0;
        piVar10 = &local_2a8;
        piVar12 = (int *)(&DAT_1007bde0 + local_2a8 * 0x298);
        for (iVar7 = 0x26; iVar7 != 0; iVar7 = iVar7 + -1) {
          *piVar12 = *piVar10;
          piVar10 = piVar10 + 1;
          piVar12 = piVar12 + 1;
        }
        uVar8 = 0xffffffff;
        pcVar14 = pcVar3;
        do {
          pcVar13 = pcVar14;
          if (uVar8 == 0) break;
          uVar8 = uVar8 - 1;
          pcVar13 = pcVar14 + 1;
          cVar1 = *pcVar14;
          pcVar14 = pcVar13;
        } while (cVar1 != '\0');
        uVar8 = ~uVar8;
        pcVar14 = pcVar13 + -uVar8;
        pcVar13 = &DAT_1007be74 + local_2a8 * 0x298;
        for (uVar9 = uVar8 >> 2; uVar9 != 0; uVar9 = uVar9 - 1) {
          *(undefined4 *)pcVar13 = *(undefined4 *)pcVar14;
          pcVar14 = pcVar14 + 4;
          pcVar13 = pcVar13 + 4;
        }
        for (uVar8 = uVar8 & 3; uVar8 != 0; uVar8 = uVar8 - 1) {
          *pcVar13 = *pcVar14;
          pcVar14 = pcVar14 + 1;
          pcVar13 = pcVar13 + 1;
        }
        FUN_10001160(&DAT_1007be74 + local_2a8 * 0x298);
        uVar8 = 0xffffffff;
        pcVar14 = pcVar4;
        do {
          pcVar13 = pcVar14;
          if (uVar8 == 0) break;
          uVar8 = uVar8 - 1;
          pcVar13 = pcVar14 + 1;
          cVar1 = *pcVar14;
          pcVar14 = pcVar13;
        } while (cVar1 != '\0');
        uVar8 = ~uVar8;
        pcVar14 = pcVar13 + -uVar8;
        pcVar13 = &DAT_1007bf74 + local_2a8 * 0x298;
        for (uVar9 = uVar8 >> 2; uVar9 != 0; uVar9 = uVar9 - 1) {
          *(undefined4 *)pcVar13 = *(undefined4 *)pcVar14;
          pcVar14 = pcVar14 + 4;
          pcVar13 = pcVar13 + 4;
        }
        for (uVar8 = uVar8 & 3; uVar8 != 0; uVar8 = uVar8 - 1) {
          *pcVar13 = *pcVar14;
          pcVar14 = pcVar14 + 1;
          pcVar13 = pcVar13 + 1;
        }
        FUN_10001160(&DAT_1007bf74 + local_2a8 * 0x298);
        puVar2 = local_2c4;
      }
      local_2c8 = local_2c8 + 1;
    } while (local_2c8 < 2000);
    CStdioFile::Close((CStdioFile *)local_2bc);
    BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&stack0xfffffd2c);
    operator_delete(puVar2);
    operator_delete(pcVar3);
    operator_delete(pcVar4);
    local_8._0_1_ = 2;
    CStdioFile::~CStdioFile((CStdioFile *)local_2bc);
    local_8._0_1_ = 1;
    CString::~CString((CString *)&stack0xfffffd2c);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString(local_2cc);
    local_8 = (undefined1 *)0xffffffff;
    CString::~CString((CString *)&local_2d0);
    uVar6 = 1;
  }
  ExceptionList = local_10;
  return uVar6;
}

