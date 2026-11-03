
void __cdecl FUN_100063e0(int param_1,int param_2)

{
  undefined1 *puVar1;
  character *pcVar2;
  int iVar3;
  CDDSurface *pCVar4;
  undefined1 *puVar5;
  int extraout_ECX;
  int extraout_ECX_00;
  character *unaff_EBX;
  int iVar6;
  int iVar7;
  undefined4 *puVar8;
  code *pcVar9;
  int iVar10;
  int iVar11;
  int local_a0;
  character *pcStack_9c;
  undefined1 *puStack_98;
  int iStack_94;
  tagRECT tStack_90;
  tagRECT tStack_80;
  undefined4 auStack_70 [25];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014fcb;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_a0);
  local_4 = 0;
  DAT_1001c4ec = (DAT_1001c4ec + 1) % 3;
  if (DAT_1001c450 != (code *)0x0) {
    CString::operator=((CString *)&local_a0,(CString *)&DAT_1006a73c);
    (*DAT_1001c450)(DAT_1001c470,param_1 + 9,param_2 + 9);
    iStack_94 = 0;
    iVar6 = param_2 + -7;
    do {
      pcVar2 = GetUnitInfoP((iStack_94 + DAT_1006a738 * 5) * 10);
      iVar7 = iVar6;
      pcStack_9c = pcVar2;
      if ((*(int *)pcVar2 != 0) && (*(int *)(pcVar2 + 4) != 0)) {
        iVar7 = iVar6 + 0x29;
        iVar3 = getsnum(*(int *)(pcVar2 + 0x10));
        pCVar4 = GetCharSurface(*(int *)(pcVar2 + 0x68),iVar3);
        pcVar9 = SetRect_exref;
        if (pCVar4 != (CDDSurface *)0x0) {
          if (*(int *)(pcVar2 + 0x68) / 100 == 7) {
            puVar8 = auStack_70;
            for (iVar3 = 0x19; iVar3 != 0; iVar3 = iVar3 + -1) {
              *puVar8 = 0;
              puVar8 = puVar8 + 1;
            }
            auStack_70[0] = 100;
            SetRect(&tStack_80,param_1 + 8,iVar6 + 0x2b,param_1 + 0x28,iVar6 + 0x4b);
            SetRect(&tStack_90,0x40,0x40,0x80,0x80);
            CDDSurface::Blt(DAT_1001c470,&tStack_80,pCVar4,&tStack_90,0x8000,(_DDBLTFX *)auStack_70)
            ;
          }
          else {
            FUN_10005300(DAT_1001c470,param_1 + 8,iVar6 + 0x2b,pCVar4,0x20,0x20,0x40,0x40,0);
            pcVar9 = SetRect_exref;
          }
        }
        if ((*(int *)(pcStack_9c + 0x20) == 0) || (*(int *)pcStack_9c == 0)) {
          FUN_10005300(DAT_1001c470,param_1 + 8,iVar6 + 0x2b,DAT_1001c480,0xe0,0,0x100,0x20,0);
        }
        else {
          puVar1 = *(undefined1 **)(pcStack_9c + 0x5c);
          iVar3 = 0x30;
          pcVar2 = pcStack_9c;
          if (puVar1 != (undefined1 *)0x0) {
            puStack_98 = *(undefined1 **)(pcStack_9c + 0x60);
            puVar5 = puStack_98;
            if ((int)puVar1 < (int)puStack_98) {
              puVar5 = puVar1;
            }
            iVar3 = ((int)puVar5 * 0x30) / (int)puVar1;
            if ((int)puStack_98 < (int)puVar1) {
              (*pcVar9)(&tStack_90,2);
              iVar11 = iVar7;
            }
            else {
              (*pcVar9)(&tStack_90,2);
              iVar11 = iVar6 + 0x2a;
            }
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,iVar11,DAT_1006a704,&puStack_98);
            pcVar2 = unaff_EBX;
          }
          iVar11 = *(int *)(pcVar2 + 0x24);
          if (iVar11 != 0) {
            iVar3 = *(int *)(pcVar2 + 0x20);
            if (iVar11 < *(int *)(pcVar2 + 0x20)) {
              iVar3 = iVar11;
            }
            iVar3 = (iVar3 * 0x30) / iVar11;
            (*pcVar9)(&puStack_98,2,0x17e,iVar3 + 2);
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,iVar6 + 0x37,DAT_1006a704,&puStack_98);
            pcVar2 = unaff_EBX;
          }
          CString::Format(*(CString **)(pcVar2 + 0x24),&stack0xffffff58);
          iVar10 = 2;
          pcStack_9c = (character *)&stack0xffffff44;
          iVar11 = extraout_ECX;
          CString::CString((CString *)&stack0xffffff44,(CString *)&stack0xffffff5c);
          FUN_100053f0(param_1 + 0x45,iVar6 + 0x38,iVar11,iVar10);
          iVar11 = *(int *)(local_a0 + 0x2c);
          if (iVar11 != 0) {
            iVar3 = *(int *)(local_a0 + 0x28);
            if (iVar11 < *(int *)(local_a0 + 0x28)) {
              iVar3 = iVar11;
            }
            iVar3 = (iVar3 * 0x30) / iVar11;
            (*pcVar9)(&iStack_94,2,0x18a);
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,iVar6 + 0x44,DAT_1006a704);
          }
          CString::Format(*(CString **)(local_a0 + 0x2c),&stack0xffffff5c);
          iVar11 = 2;
          puStack_98 = &stack0xffffff48;
          if (iVar3 < 0x19) {
            iVar3 = extraout_ECX_00;
            puStack_98 = &stack0xffffff48;
            CString::CString((CString *)&stack0xffffff48,(CString *)&local_a0);
            FUN_100053f0(param_1 + 0x45,iVar6 + 0x45,iVar3,iVar11);
          }
          else {
            iVar3 = extraout_ECX_00;
            CString::CString((CString *)&stack0xffffff48,(CString *)&local_a0);
            FUN_100053f0(param_1 + 0x45,iVar6 + 0x45,iVar3,iVar11);
          }
        }
      }
      iStack_94 = iStack_94 + 1;
      iVar6 = iVar7;
    } while (iStack_94 < 4);
  }
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_a0);
  ExceptionList = pvStack_c;
  return;
}

