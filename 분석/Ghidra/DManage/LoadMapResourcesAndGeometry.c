/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x10007f00
 * Evidence: ev_c820287d716d7e4f2d4fa8d60d4e7ec65b6f9dc8bb9f5cdb30de2e3e08b97f06
 */

/* public: int __thiscall BBDX_MapManager::Load(class CString) */

int __thiscall BBDX_MapManager::LoadMapResourcesAndGeometry(BBDX_MapManager *this,byte *param_2)

{
  byte bVar1;
  byte *pbVar2;
  int iVar3;
  int iVar4;
  byte *pbVar5;
  bool bVar6;
  CFile local_1c [16];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
                    /* Original BBDX_MapManager::Load(CString). Open-check the path; read required
                       GRS indices, reuse/load graphics and surfaces, call LoadMap, and reset
                       visible sprite cache. No UserUnit creation call is present in this function.
                        */
  puStack_8 = &LAB_1000c9f0;
  local_c = ExceptionList;
  local_4 = 0;
  if (*(int *)(this + 0x34) != 0) {
    pbVar2 = *(byte **)(this + 0x30);
    pbVar5 = param_2;
    do {
      bVar1 = *pbVar2;
      bVar6 = bVar1 < *pbVar5;
      if (bVar1 != *pbVar5) {
LAB_10007f59:
        iVar3 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
        goto LAB_10007f5d;
      }
      if (bVar1 == 0) break;
      bVar1 = pbVar2[1];
      bVar6 = bVar1 < pbVar5[1];
      if (bVar1 != pbVar5[1]) goto LAB_10007f59;
      pbVar2 = pbVar2 + 2;
      pbVar5 = pbVar5 + 2;
    } while (bVar1 != 0);
    iVar3 = 0;
LAB_10007f5d:
    ExceptionList = &local_c;
    if (iVar3 == 0) goto LAB_100080b2;
  }
  DAT_100121c4 = -(uint)(*(int *)(this + 100) != 0) & (uint)this;
  ExceptionList = &local_c;
  CFile::CFile(local_1c);
  local_4._0_1_ = 1;
  iVar3 = CFile::Open(local_1c,(char *)param_2,0,(CFileException *)0x0);
  if (iVar3 == 0) {
    *(undefined4 *)(this + 0x34) = 0;
    local_4 = (uint)local_4._1_3_ << 8;
    CFile::~CFile(local_1c);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_2);
    ExceptionList = local_c;
    return 0;
  }
  CFile::Close(local_1c);
  *(undefined4 *)(this + 0x34) = 0;
  SetUsedGrsIndexes(this,(CString *)&param_2);
  DestroySurfacePack(this,*(int *)(this + 0x5c),*(int **)(this + 0x60));
  CString::CString((CString *)&stack0xffffffc4,(CString *)&param_2);
  SetTotalSize(this);
  iVar3 = 0;
  if (0 < *(int *)(this + 0x5c)) {
    do {
      iVar4 = IsExistGrs(this,*(int *)(*(int *)(this + 0x60) + iVar3 * 4));
      if ((iVar4 == 0) &&
         (iVar4 = LoadGrs(this,*(int *)(*(int *)(this + 0x60) + iVar3 * 4)), iVar4 != 0)) {
        MakeSurfaces(this,*(int *)(*(int *)(this + 0x60) + iVar3 * 4));
      }
      iVar3 = iVar3 + 1;
    } while (iVar3 < *(int *)(this + 0x5c));
  }
  iVar3 = ReplaceAndLoadMapGeometry(this,(CString *)&param_2);
  if (iVar3 == 0) {
    *(undefined4 *)(this + 0x34) = 0;
  }
  else {
    StartPosAndValidSpriteInit(this);
    *(undefined4 *)(this + 0x34) = 1;
  }
  if (*(int *)(this + 0x34) == 0) {
    DAT_100121c4 = 0;
    local_4 = (uint)local_4._1_3_ << 8;
    CFile::~CFile(local_1c);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&param_2);
    ExceptionList = local_c;
    return 0;
  }
  CString::operator=((CString *)(this + 0x30),(CString *)&param_2);
  *(undefined4 *)(this + 100) = 0;
  DAT_100121c4 = 0;
  local_4 = (uint)local_4._1_3_ << 8;
  CFile::~CFile(local_1c);
LAB_100080b2:
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_2);
  ExceptionList = local_c;
  return 1;
}

