
/* public: int __thiscall CAlphaMask::LoadMask(class CString &) */

int __thiscall CAlphaMask::LoadMask(CAlphaMask *this,CString *param_1)

{
  int iVar1;
  ulong uVar2;
  void *pvVar3;
  uint uVar4;
  uint uVar5;
  uint uVar6;
  uint uVar7;
  uint uVar8;
  undefined4 unaff_EBX;
  uint uVar9;
  undefined1 local_20 [4];
  int local_1c [2];
  void *pvStack_14;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0x11b0  159  ?LoadMask@CAlphaMask@@QAEHAAVCString@@@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_1000c508;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  if (*(void **)this != (void *)0x0) {
    ExceptionList = &local_c;
    operator_delete(*(void **)this);
    *(undefined4 *)this = 0;
  }
  CFile::CFile((CFile *)local_1c);
  local_4 = 0;
  iVar1 = CFile::Open((CFile *)local_1c,*(char **)param_1,0x8000,(CFileException *)0x0);
  if (iVar1 == 0) {
    local_4 = 0xffffffff;
    CFile::~CFile((CFile *)local_1c);
    ExceptionList = local_c;
    return 0;
  }
  uVar2 = CFile::GetLength((CFile *)local_1c);
  uVar9 = uVar2 - 0x10;
  pvVar3 = operator_new(uVar9);
  *(void **)this = pvVar3;
  uVar4 = CFile::Read((CFile *)local_1c,local_20,4);
  uVar5 = CFile::Read((CFile *)local_1c,this + 4,4);
  uVar6 = CFile::Read((CFile *)local_1c,this + 8,4);
  uVar7 = CFile::Read((CFile *)local_1c,this + 0xc,4);
  uVar8 = (**(code **)(local_1c[0] + 0x3c))(*(undefined4 *)this,uVar9);
  *(undefined4 *)(this + 0x10) = unaff_EBX;
  uVar4 = (uint)((((uVar4 == 4 && uVar5 == 4) && uVar6 == 4) && uVar7 == 4) && uVar8 == uVar9);
  if (uVar4 == 0) {
    operator_delete(*(void **)this);
    *(undefined4 *)this = 0;
  }
  CFile::Close((CFile *)&stack0xffffffdc);
  local_c = (void *)0xffffffff;
  CFile::~CFile((CFile *)&stack0xffffffdc);
  ExceptionList = pvStack_14;
  return uVar4;
}

