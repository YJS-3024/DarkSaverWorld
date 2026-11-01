/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x100011a0
 * Evidence: ev_0bccafb1de232e08efdedbc88a2a30e96e56a441001d9de7871f3716e8e925db
 */

/* int __cdecl ReadTileAttr(char *) */

int __cdecl LoadTileAttributeTable(char *param_1)

{
  CString *pCVar1;
  int iVar2;
  char *unaff_EBX;
  undefined *puVar3;
  char *local_38;
  CString local_34 [4];
  CString local_30 [4];
  undefined1 local_2c [8];
  CStdioFile local_24 [20];
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
                    /* Original export ReadTileAttr(char*). Read tile attribute data from common
                       resources; retain original table selection and error propagation. */
  local_4 = 0xffffffff;
  local_8 = &LAB_100032f0;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_30,s_GameRes_set_arc_10005088);
  local_4 = 0;
  CString::CString((CString *)&local_38,param_1);
  local_4 = CONCAT31(local_4._1_3_,1);
  pCVar1 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&param_1,local_30);
  local_8._0_1_ = 2;
  CString::operator=((CString *)&stack0xffffffc4,pCVar1);
  local_8._0_1_ = 1;
  CString::~CString((CString *)&stack0x00000000);
  CString::CString((CString *)&local_38);
  local_8._0_1_ = 3;
  CString::CString(local_30);
  local_8._0_1_ = 4;
  CStdioFile::CStdioFile(local_24);
  local_8._0_1_ = 5;
  iVar2 = CStdioFile::Open(local_24,unaff_EBX,0x4000,(CFileException *)0x0);
  if (iVar2 == 0) {
    local_8._0_1_ = 4;
    CStdioFile::~CStdioFile(local_24);
    local_8._0_1_ = 3;
    CString::~CString(local_30);
    local_8._0_1_ = 1;
    CString::~CString((CString *)&local_38);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString((CString *)&stack0xffffffc4);
    local_8 = (undefined1 *)0xffffffff;
    CString::~CString(local_34);
    ExceptionList = local_10;
    return 0;
  }
  CStdioFile::ReadString(local_24,(CString *)&local_38);
  puVar3 = &DAT_10265270;
  do {
    iVar2 = CStdioFile::ReadString(local_24,(CString *)&local_38);
    if (iVar2 == 0) break;
    if (*(int *)(local_38 + -8) != 0) {
      sscanf(local_38,s__s__d__d__d__d__d__d__d__d__d__d_10005024,local_2c,puVar3 + -0x70,
             puVar3 + -0x6c,puVar3 + -0x68,puVar3 + -100,puVar3 + -0x60,puVar3 + -0x5c,
             puVar3 + -0x58,puVar3 + -0x54,puVar3 + -0x50,puVar3 + -0x4c,puVar3 + -0x48,
             puVar3 + -0x44,puVar3 + -0x40,puVar3 + -0x3c,puVar3 + -0x38,puVar3 + -0x34,
             puVar3 + -0x30,puVar3 + -0x2c,puVar3 + -0x28,puVar3 + -0x24,puVar3 + -0x20,
             puVar3 + -0x1c,puVar3 + -0x18,puVar3 + -0x14,puVar3 + -0x10,puVar3 + -0xc,puVar3 + -8,
             puVar3 + -4,puVar3,puVar3 + 4);
    }
    puVar3 = puVar3 + 0xb4;
  } while ((int)puVar3 < 0x102698c0);
  CStdioFile::Close(local_24);
  BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&stack0xffffffc4);
  local_8._0_1_ = 4;
  CStdioFile::~CStdioFile(local_24);
  local_8._0_1_ = 3;
  CString::~CString(local_30);
  local_8._0_1_ = 1;
  CString::~CString((CString *)&local_38);
  local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
  CString::~CString((CString *)&stack0xffffffc4);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_34);
  ExceptionList = local_10;
  return 1;
}

