/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x42a9a0; source SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689
 */

/* Native SEH prologue verified. Iterate stored unit records, call UserUnit.dmSetChar and dmSetChar2
   using record identity/job/coordinates. This observation alone does not establish where the record
   array originated. REA did not identify the containing function; recover only in the separate
   project.
   Source SHA256: dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689 */

void __fastcall PopulateActorsFromUnitRecords_A(int param_1)

{
  int *piVar1;
  int iVar2;
  int iVar3;
  int iStack_24;
  int iStack_20;
  int iStack_1c;
  int iStack_18;
  CSize aCStack_14 [4];
  void *pvStack_10;
  void *pvStack_c;
  undefined *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &DAT_0047d148;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  thunk_FUN_00428be0(param_1);
  piVar1 = *(int **)(*(int *)(param_1 + 0x13c) + 0x84);
  iVar2 = (**(code **)(*piVar1 + 0x60))(piVar1);
  if (iVar2 != 0) {
    piVar1 = *(int **)(*(int *)(param_1 + 0x13c) + 0x84);
    iVar2 = (**(code **)(*piVar1 + 0x6c))(piVar1);
    if (iVar2 != 0) {
      CDirectDraw::ErrorHandle(*(long *)(*(int *)(param_1 + 0x13c) + 4));
    }
  }
  if (DAT_004953b0 != (void *)0x0) {
    thunk_FUN_0041ee40(DAT_004953b0,*(CDDSurface **)(param_1 + 0x13c));
    userclearstatusall();
    iVar2 = *(int *)(param_1 + 0xe4);
    iStack_1c = iVar2;
    CString::CString((CString *)&stack0xffffffd4);
    iVar3 = 0;
    puStack_8 = (undefined *)0x0;
    if (0 < iVar2) {
      do {
        iVar2 = *(int *)(*(int *)(param_1 + 0xe0) + iVar3 * 4);
        CString::Format((CString *)(int)*(short *)(iVar2 + 8),&stack0xffffffd4);
        dmSetChar((char *)0x0);
        dmSetChar2((int)*(short *)(iVar2 + 6),(int)*(short *)(iVar2 + 0x36),
                   (int)*(short *)(iVar2 + 0xc),(int)*(short *)(iVar2 + 0xe),
                   (int)*(short *)(iVar2 + 0x10),(int)*(short *)(iVar2 + 0x38));
        if ((*(short *)(iVar2 + 6) == *(short *)(param_1 + 0xf0)) && (DAT_004953b4 != 0)) {
          if (DAT_004953b0 == (void *)0x0) break;
          BBDX_MapManager::GetSizeOfMap(DAT_004953bc,aCStack_14);
          GetCharPos((int)*(short *)(param_1 + 0xf0),&iStack_20,&iStack_1c);
          *(int *)(param_1 + 0x124) = iStack_20 + -10;
          *(int *)(param_1 + 0x128) = iStack_1c + -7;
        }
        iVar3 = iStack_24 + 1;
        iStack_24 = iVar3;
      } while (iVar3 < iStack_18);
    }
    puStack_8 = (undefined *)0xffffffff;
    CString::~CString((CString *)&stack0xffffffd4);
  }
  ExceptionList = pvStack_10;
  return;
}

