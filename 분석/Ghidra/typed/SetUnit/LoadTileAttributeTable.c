/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x100011a0; source SHA256 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca
 */

/* Original export ReadTileAttr(char*). Read tile attribute data from common resources; retain
   original table selection and error propagation.
   Source SHA256: 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca */

int __cdecl LoadTileAttributeTable(char *param_1)

{
  CString *pCVar1;
  int iVar2;
  undefined *puVar3;
  char *local_38;
  char *local_34;
  CString local_30 [4];
  CString local_2c [4];
  undefined1 local_28 [8];
  CStdioFile local_20 [20];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
                    /* 0x11a0  16  ?ReadTileAttr@@YAHPAD@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_100032f0;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_30,s_GameRes_set_arc_10005088);
  local_4 = 0;
  CString::CString((CString *)&local_38,param_1);
  local_4._0_1_ = 1;
  pCVar1 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&param_1,local_30);
  local_4._0_1_ = 2;
  CString::operator=((CString *)&local_38,pCVar1);
  local_4._0_1_ = 1;
  CString::~CString((CString *)&param_1);
  CString::CString((CString *)&local_34);
  local_4._0_1_ = 3;
  CString::CString(local_2c);
  local_4._0_1_ = 4;
  CStdioFile::CStdioFile(local_20);
  local_4._0_1_ = 5;
  iVar2 = CStdioFile::Open(local_20,local_38,0x4000,(CFileException *)0x0);
  if (iVar2 == 0) {
    local_4._0_1_ = 4;
    CStdioFile::~CStdioFile(local_20);
    local_4._0_1_ = 3;
    CString::~CString(local_2c);
    local_4._0_1_ = 1;
    CString::~CString((CString *)&local_34);
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString((CString *)&local_38);
    local_4 = 0xffffffff;
    CString::~CString(local_30);
    ExceptionList = local_c;
    return 0;
  }
  CStdioFile::ReadString(local_20,(CString *)&local_34);
  puVar3 = &DAT_10265270;
  do {
    iVar2 = CStdioFile::ReadString(local_20,(CString *)&local_34);
    if (iVar2 == 0) break;
    if (*(int *)(local_34 + -8) != 0) {
      sscanf(local_34,s__s__d__d__d__d__d__d__d__d__d__d_10005024,local_28,puVar3 + -0x70,
             puVar3 + -0x6c,puVar3 + -0x68,puVar3 + -100,puVar3 + -0x60,puVar3 + -0x5c,
             puVar3 + -0x58,puVar3 + -0x54,puVar3 + -0x50,puVar3 + -0x4c,puVar3 + -0x48,
             puVar3 + -0x44,puVar3 + -0x40,puVar3 + -0x3c,puVar3 + -0x38,puVar3 + -0x34,
             puVar3 + -0x30,puVar3 + -0x2c,puVar3 + -0x28,puVar3 + -0x24,puVar3 + -0x20,
             puVar3 + -0x1c,puVar3 + -0x18,puVar3 + -0x14,puVar3 + -0x10,puVar3 + -0xc,puVar3 + -8,
             puVar3 + -4,puVar3,puVar3 + 4);
    }
    puVar3 = puVar3 + 0xb4;
  } while ((int)puVar3 < 0x102698c0);
  CStdioFile::Close(local_20);
  BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&local_38);
  local_4._0_1_ = 4;
  CStdioFile::~CStdioFile(local_20);
  local_4._0_1_ = 3;
  CString::~CString(local_2c);
  local_4._0_1_ = 1;
  CString::~CString((CString *)&local_34);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString((CString *)&local_38);
  local_4 = 0xffffffff;
  CString::~CString(local_30);
  ExceptionList = local_c;
  return 1;
}

