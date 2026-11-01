/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x1000a850; source SHA256 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3
 */

/* Original BB_Map::LoadMap(path,callback). Decode map resource IDs/dimensions, background arrays,
   tile records and decorative sprites. No unknown trailing bytes are invented as a monster roster.
   Source SHA256: 08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3 */

int __thiscall BB_Map::DecodeMrcMapGeometry(BB_Map *this,char *param_1,_func_void_ulong *param_2)

{
  int iVar1;
  uint uVar2;
  void *pvVar3;
  int iVar4;
  uint uVar5;
  size_t sVar6;
  int iVar7;
  time_t tVar8;
  int local_20;
  CFile local_1c [4];
  CFile aCStack_18 [12];
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0xa850  158  ?LoadMap@BB_Map@@QAEHPBDP6AXK@Z@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_1000cc28;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CFile::CFile(local_1c);
  local_4 = 0;
  iVar1 = CFile::Open(local_1c,param_1,0,(CFileException *)0x0);
  if (iVar1 == 0) {
    local_4 = 0xffffffff;
    CFile::~CFile(local_1c);
    iVar1 = 0;
  }
  else {
    local_20 = 0;
    CFile::Read(local_1c,&local_20,4);
    CFile::Seek(local_1c,local_20 * 4,1);
    if (param_2 != (_func_void_ulong *)0x0) {
      (*param_2)(local_20 * 4 + 4);
    }
    uVar2 = CFile::Read(aCStack_18,this + 0x30,4);
    if (uVar2 == 0) {
      CFile::~CFile(aCStack_18);
      iVar1 = 0;
    }
    else {
      uVar2 = CFile::Read(aCStack_18,this + 0x34,4);
      if (uVar2 == 0) {
        CFile::~CFile(aCStack_18);
        iVar1 = 0;
      }
      else {
        uVar2 = CFile::Read(aCStack_18,this + 0x38,4);
        if (uVar2 == 0) {
          CFile::~CFile(aCStack_18);
          iVar1 = 0;
        }
        else {
          if (param_2 != (_func_void_ulong *)0x0) {
            (*param_2)(0xc);
          }
          uVar5 = *(int *)(this + 0x34) * *(int *)(this + 0x30);
          pvVar3 = operator_new(uVar5);
          *(void **)(this + 0x10) = pvVar3;
          uVar2 = CFile::Read(aCStack_18,pvVar3,uVar5);
          if (uVar2 == 0) {
            CFile::~CFile(aCStack_18);
            iVar1 = 0;
          }
          else {
            if (param_2 != (_func_void_ulong *)0x0) {
              (*param_2)(uVar5);
            }
            pvVar3 = operator_new(uVar5);
            *(void **)(this + 0xc) = pvVar3;
            uVar2 = CFile::Read(aCStack_18,pvVar3,uVar5);
            if (uVar2 == 0) {
              CFile::~CFile(aCStack_18);
              iVar1 = 0;
            }
            else {
              if (param_2 != (_func_void_ulong *)0x0) {
                (*param_2)(uVar5);
              }
              sVar6 = uVar5 * 5;
              pvVar3 = malloc(sVar6);
              *(void **)(this + 0x14) = pvVar3;
              if (pvVar3 == (void *)0x0) {
                CFile::~CFile(aCStack_18);
                iVar1 = 0;
              }
              else {
                uVar2 = CFile::Read(aCStack_18,pvVar3,sVar6);
                if (uVar2 == sVar6) {
                  if (param_2 != (_func_void_ulong *)0x0) {
                    (*param_2)(sVar6);
                  }
                  sVar6 = *(int *)(this + 0x38) << 4;
                  pvVar3 = malloc(sVar6);
                  *(void **)(this + 0x18) = pvVar3;
                  if (pvVar3 == (void *)0x0) {
                    CFile::~CFile(aCStack_18);
                    iVar1 = 0;
                  }
                  else {
                    uVar2 = CFile::Read(aCStack_18,pvVar3,sVar6);
                    if (uVar2 == sVar6) {
                      if (param_2 != (_func_void_ulong *)0x0) {
                        (*param_2)(sVar6);
                      }
                      CFile::Close(aCStack_18);
                      iVar1 = *(int *)(this + 0x24);
                      iVar7 = 0;
                      if (0 < iVar1) {
                        do {
                          operator_delete(*(void **)(*(int *)(this + 0x20) + iVar7 * 4));
                          iVar7 = iVar7 + 1;
                        } while (iVar7 < iVar1);
                      }
                      CPtrArray::SetSize((CPtrArray *)(this + 0x1c),*(int *)(this + 0x34),-1);
                      tVar8 = time((time_t *)0x0);
                      srand((uint)tVar8);
                      iVar1 = 0;
                      if (0 < *(int *)(this + 0x34)) {
                        do {
                          pvVar3 = operator_new(*(uint *)(this + 0x30));
                          iVar7 = 0;
                          if (0 < *(int *)(this + 0x30)) {
                            do {
                              iVar4 = rand();
                              *(char *)(iVar7 + (int)pvVar3) = (char)iVar4;
                              iVar7 = iVar7 + 1;
                            } while (iVar7 < *(int *)(this + 0x30));
                          }
                          iVar1 = iVar1 + 1;
                          *(void **)(*(int *)(this + 0x20) + -4 + iVar1 * 4) = pvVar3;
                        } while (iVar1 < *(int *)(this + 0x34));
                      }
                      CFile::~CFile(aCStack_18);
                      iVar1 = 1;
                    }
                    else {
                      CFile::~CFile(aCStack_18);
                      iVar1 = 0;
                    }
                  }
                }
                else {
                  CFile::~CFile(aCStack_18);
                  iVar1 = 0;
                }
              }
            }
          }
        }
      }
    }
  }
  ExceptionList = puStack_8;
  return iVar1;
}

