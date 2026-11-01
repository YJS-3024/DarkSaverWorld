/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10007e50; source SHA256 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3
 */

/* Original BBDX_MapManager::LoadMap(CString&). Destroy/recreate BB_Map with the existing resource
   array, decode the selected MRC, report progress and apply TileAttrChange on success. The outer
   Load routine performs GRS acquisition.
   Source SHA256: 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3 */

int __thiscall BBDX_MapManager::ReplaceAndLoadMapGeometry(BBDX_MapManager *this,CString *param_1)

{
  BB_Map *pBVar1;
  int iVar2;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 uStack_4;
  
                    /* 0x7e50  157  ?LoadMap@BBDX_MapManager@@QAEHAAVCString@@@Z */
  uStack_4 = 0xffffffff;
  puStack_8 = &LAB_1000c9cb;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  if (*(undefined4 **)(this + 0x4c) != (undefined4 *)0x0) {
    ExceptionList = &pvStack_c;
    (**(code **)**(undefined4 **)(this + 0x4c))(1);
  }
  pBVar1 = operator_new(0x40);
  uStack_4 = 0;
  if (pBVar1 == (BB_Map *)0x0) {
    pBVar1 = (BB_Map *)0x0;
  }
  else {
    pBVar1 = (BB_Map *)BB_Map::BB_Map(pBVar1,(CTypedPtrArray<CPtrArray,CResource*> *)(this + 0x38));
  }
  *(BB_Map **)(this + 0x4c) = pBVar1;
  uStack_4 = 0xffffffff;
  iVar2 = BB_Map::DecodeMrcMapGeometry(pBVar1,*(char **)param_1,CurLoadStatus);
  if (*(int *)(this + 100) != 0) {
    DrawLoadStatus(this,1);
  }
  if (iVar2 != 0) {
    BB_Map::TileAttrChange(*(BB_Map **)(this + 0x4c));
  }
  ExceptionList = pvStack_c;
  return iVar2;
}

