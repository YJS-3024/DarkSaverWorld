
void __cdecl FUN_100063e0(int param_1,int param_2)

{
  undefined1 *puVar1;
  int iVar2;
  character *pcVar3;
  int iVar4;
  CDDSurface *arg3;
  undefined1 *puVar5;
  dword extraout_ECX;
  dword extraout_ECX_00;
  character *unaff_EBX;
  dword dVar6;
  dword dVar7;
  undefined4 *puVar8;
  code *pcVar9;
  dword dVar10;
  dword dVar11;
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
    dVar6 = param_2 - 7;
    do {
      pcVar3 = GetUnitInfoP((iStack_94 + DAT_1006a738 * 5) * 10);
      dVar7 = dVar6;
      pcStack_9c = pcVar3;
      if ((*(int *)pcVar3 != 0) && (*(int *)(pcVar3 + 4) != 0)) {
        dVar7 = dVar6 + 0x29;
        iVar4 = getsnum(*(int *)(pcVar3 + 0x10));
        arg3 = GetCharSurface(*(int *)(pcVar3 + 0x68),iVar4);
        pcVar9 = SetRect_exref;
        if (arg3 != (CDDSurface *)0x0) {
          if (*(int *)(pcVar3 + 0x68) / 100 == 7) {
            puVar8 = auStack_70;
            for (iVar4 = 0x19; iVar4 != 0; iVar4 = iVar4 + -1) {
              *puVar8 = 0;
              puVar8 = puVar8 + 1;
            }
            auStack_70[0] = 100;
            SetRect(&tStack_80,param_1 + 8,dVar6 + 0x2b,param_1 + 0x28,dVar6 + 0x4b);
            SetRect(&tStack_90,0x40,0x40,0x80,0x80);
            CDDSurface::Blt(DAT_1001c470,&tStack_80,arg3,&tStack_90,0x8000,(_DDBLTFX *)auStack_70);
          }
          else {
            FUN_10005300((dword)DAT_1001c470,param_1 + 8,dVar6 + 0x2b,(dword)arg3,0x20,0x20,0x40,
                         0x40,0);
            pcVar9 = SetRect_exref;
          }
        }
        if ((*(int *)(pcStack_9c + 0x20) == 0) || (*(int *)pcStack_9c == 0)) {
          FUN_10005300((dword)DAT_1001c470,param_1 + 8,dVar6 + 0x2b,DAT_1001c480,0xe0,0,0x100,0x20,0
                      );
        }
        else {
          puVar1 = *(undefined1 **)(pcStack_9c + 0x5c);
          iVar4 = 0x30;
          pcVar3 = pcStack_9c;
          if (puVar1 != (undefined1 *)0x0) {
            puStack_98 = *(undefined1 **)(pcStack_9c + 0x60);
            puVar5 = puStack_98;
            if ((int)puVar1 < (int)puStack_98) {
              puVar5 = puVar1;
            }
            iVar4 = ((int)puVar5 * 0x30) / (int)puVar1;
            if ((int)puStack_98 < (int)puVar1) {
              (*pcVar9)(&tStack_90,2);
              dVar11 = dVar7;
            }
            else {
              (*pcVar9)(&tStack_90,2);
              dVar11 = dVar6 + 0x2a;
            }
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,dVar11,DAT_1006a704,(dword)&puStack_98,1
                               );
            pcVar3 = unaff_EBX;
          }
          iVar2 = *(int *)(pcVar3 + 0x24);
          if (iVar2 != 0) {
            iVar4 = *(int *)(pcVar3 + 0x20);
            if (iVar2 < *(int *)(pcVar3 + 0x20)) {
              iVar4 = iVar2;
            }
            iVar4 = (iVar4 * 0x30) / iVar2;
            (*pcVar9)(&puStack_98,2,0x17e,iVar4 + 2);
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,dVar6 + 0x37,DAT_1006a704,
                                (dword)&puStack_98,1);
            pcVar3 = unaff_EBX;
          }
          CString::Format((dword)&stack0xffffff58,0x1001a290,*(int *)(pcVar3 + 0x20));
          dVar10 = 2;
          pcStack_9c = (character *)&stack0xffffff44;
          dVar11 = extraout_ECX;
          CString::CString((CString *)&stack0xffffff44,(CString *)&stack0xffffff5c);
          FUN_100053f0(param_1 + 0x45,dVar6 + 0x38,dVar11,dVar10);
          iVar2 = *(int *)(local_a0 + 0x2c);
          if (iVar2 != 0) {
            iVar4 = *(int *)(local_a0 + 0x28);
            if (iVar2 < *(int *)(local_a0 + 0x28)) {
              iVar4 = iVar2;
            }
            iVar4 = (iVar4 * 0x30) / iVar2;
            (*pcVar9)(&iStack_94,2,0x18a);
            CDDSurface::BltFast(DAT_1001c470,param_1 + 0x41,dVar6 + 0x44,DAT_1006a704,
                                (dword)&iStack_94,1);
          }
          CString::Format((dword)&stack0xffffff5c,0x1001a290);
          dVar11 = 2;
          puStack_98 = &stack0xffffff48;
          if (iVar4 < 0x19) {
            dVar10 = extraout_ECX_00;
            puStack_98 = &stack0xffffff48;
            CString::CString((CString *)&stack0xffffff48,(CString *)&local_a0);
            FUN_100053f0(param_1 + 0x45,dVar6 + 0x45,dVar10,dVar11);
          }
          else {
            dVar10 = extraout_ECX_00;
            CString::CString((CString *)&stack0xffffff48,(CString *)&local_a0);
            FUN_100053f0(param_1 + 0x45,dVar6 + 0x45,dVar10,dVar11);
          }
        }
      }
      iStack_94 = iStack_94 + 1;
      dVar6 = dVar7;
    } while (iStack_94 < 4);
  }
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_a0);
  ExceptionList = pvStack_c;
  return;
}

