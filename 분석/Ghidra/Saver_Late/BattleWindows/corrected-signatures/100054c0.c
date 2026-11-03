
void __cdecl FUN_100054c0(void *param_1,int param_2)

{
  LONG LVar1;
  character *pcVar2;
  int iVar3;
  int iVar4;
  character *pcVar5;
  short *psVar6;
  dword extraout_ECX;
  dword extraout_ECX_00;
  dword extraout_ECX_01;
  dword extraout_ECX_02;
  dword extraout_ECX_03;
  dword extraout_ECX_04;
  dword extraout_ECX_05;
  dword extraout_ECX_06;
  dword extraout_ECX_07;
  uint uVar7;
  undefined4 *puVar8;
  code *pcVar9;
  bool bVar10;
  undefined4 uStack0000000c;
  dword dVar11;
  dword dVar12;
  CString local_a4 [4];
  character *local_a0;
  CDDSurface *local_9c;
  int local_98;
  tagRECT local_94;
  undefined1 local_84 [4];
  tagRECT local_80;
  undefined4 local_70 [25];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014fab;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_a4);
  local_4 = 0;
  DAT_1001c4e8 = (DAT_1001c4e8 + 1) % 3;
  if (DAT_1001a380 < 0) {
    if (DAT_1006a738 < 0) goto LAB_10006119;
    pcVar2 = GetUnitInfoP(DAT_1006a738 * 0x32);
    DAT_1001a380 = DAT_1006a738 * 0x32;
  }
  else {
    pcVar2 = GetUnitInfoP(DAT_1001a380);
  }
  local_a0 = pcVar2;
  iVar3 = getsnum(*(int *)(pcVar2 + 0x10));
  local_9c = GetCharSurface(*(int *)(pcVar2 + 0x68),iVar3);
  pcVar9 = SetRect_exref;
  if (local_9c != (CDDSurface *)0x0) {
    if (*(int *)(pcVar2 + 0x68) / 100 == 7) {
      puVar8 = local_70;
      for (iVar3 = 0x19; iVar3 != 0; iVar3 = iVar3 + -1) {
        *puVar8 = 0;
        puVar8 = puVar8 + 1;
      }
      local_70[0] = 100;
      SetRect(&local_80,(int)param_1 + 0x2c,param_2 + 0x4f,(int)param_1 + 0x4b,param_2 + 0x6f);
      SetRect(&local_94,0x40,0x40,0x80,0x80);
      CDDSurface::Blt(DAT_1001c470,&local_80,local_9c,&local_94,0x8000,(_DDBLTFX *)local_70);
    }
    else {
      FUN_10005300((dword)DAT_1001c470,(int)param_1 + 0x2c,param_2 + 0x4f,(dword)local_9c,0x20,0x20,
                   0x40,0x40,0);
      pcVar9 = SetRect_exref;
    }
  }
  iVar3 = *(int *)(pcVar2 + 0x5c);
  local_9c = (CDDSurface *)0x50;
  if (iVar3 != 0) {
    iVar4 = *(int *)(pcVar2 + 0x60);
    if (iVar3 < *(int *)(pcVar2 + 0x60)) {
      iVar4 = iVar3;
    }
    local_9c = (CDDSurface *)((iVar4 * 0x50) / iVar3);
    if (*(int *)(pcVar2 + 0x60) < iVar3) {
      iVar3 = 0x172;
    }
    else {
      iVar3 = DAT_1001c4e8 * 0xc + 0x172;
    }
    (*pcVar9)(&local_94,2,iVar3,local_9c + 2);
    CDDSurface::BltFast(DAT_1001c470,(int)param_1 + 0x1e,param_2 + 0x8f,DAT_1006a704,
                        (dword)&local_94,1);
  }
  iVar3 = *(int *)(pcVar2 + 0x24);
  if (iVar3 != 0) {
    iVar4 = *(int *)(pcVar2 + 0x20);
    if (iVar3 < *(int *)(pcVar2 + 0x20)) {
      iVar4 = iVar3;
    }
    local_9c = (CDDSurface *)((iVar4 * 0x50) / iVar3);
    (*pcVar9)(&local_94,2,0x17e,local_9c + 2);
    CDDSurface::BltFast(DAT_1001c470,(int)param_1 + 0x1e,param_2 + 0x9c,DAT_1006a704,
                        (dword)&local_94,1);
  }
  CString::Format((dword)local_a4,0x1001a290,*(undefined4 *)(pcVar2 + 0x20));
  dVar12 = 2;
  local_94.left = (LONG)&stack0xffffff48;
  dVar11 = extraout_ECX;
  CString::CString((CString *)&stack0xffffff48,(CString *)&local_a0);
  FUN_100053f0((int)param_1 + 0x22,param_2 + 0x9d,dVar11,dVar12);
  iVar3 = *(int *)(pcVar2 + 0x2c);
  if (iVar3 != 0) {
    iVar4 = *(int *)(pcVar2 + 0x28);
    if (iVar3 < *(int *)(pcVar2 + 0x28)) {
      iVar4 = iVar3;
    }
    local_98 = (iVar4 * 0x50) / iVar3;
    (*pcVar9)(&local_94.top,2,0x18a);
    CDDSurface::BltFast(DAT_1001c470,(int)param_1 + 0x1e,param_2 + 0xa9,DAT_1006a704,
                        (dword)&local_94.top,1);
  }
  CString::Format((dword)&local_a0,0x1001a290);
  dVar12 = 2;
  local_94.top = (LONG)&stack0xffffff4c;
  dVar11 = extraout_ECX_00;
  if (local_94.left < 0x29) {
    local_94.top = (LONG)&stack0xffffff4c;
    CString::CString((CString *)&stack0xffffff4c,(CString *)&local_9c);
  }
  else {
    CString::CString((CString *)&stack0xffffff4c,(CString *)&local_9c);
  }
  FUN_100053f0((int)param_1 + 0x22,param_2 + 0xaa,dVar11,dVar12);
  if (*(int *)(pcVar2 + 0xc) != 0) {
    (*pcVar9)(&local_94.right,2);
    CDDSurface::BltFast(DAT_1001c470,(int)param_1 + 0x1e,param_2 + 0xb6,DAT_1006a704,
                        (dword)&local_94.top,1);
    CString::Format((dword)&local_a0,0x1001a288);
    dVar12 = 2;
    local_94.top = (LONG)&stack0xffffff4c;
    dVar11 = extraout_ECX_01;
    CString::CString((CString *)&stack0xffffff4c,(CString *)&local_9c);
    FUN_100053f0((int)param_1 + 0x22,param_2 + 0xb7,dVar11,dVar12);
  }
  if (*(int *)(pcVar2 + 0x10) % 10 == 0) {
    CString::Format((dword)&local_9c,0x1001a284);
    dVar12 = 2;
    local_94.right = (LONG)&stack0xffffff50;
    dVar11 = extraout_ECX_06;
    CString::CString((CString *)&stack0xffffff50,(CString *)&local_98);
    FUN_100053f0((int)param_1 + 0x20,param_2 + 0xc5,dVar11,dVar12);
    CString::Format((dword)&local_98,0x1001a284,*(undefined4 *)(pcVar2 + 0x38));
    dVar11 = extraout_ECX_07;
LAB_10005a8c:
    local_94.bottom = (LONG)&stack0xffffff54;
    dVar12 = 2;
    CString::CString((CString *)&stack0xffffff54,(CString *)&local_94);
  }
  else {
    pcVar5 = GetUnitInfoP((*(int *)(pcVar2 + 0x10) / 10) * 10);
    local_94.left = *(LONG *)(pcVar2 + 0x38);
    local_80.top = *(LONG *)(pcVar2 + 0x34);
    if ((*(int *)pcVar5 == 0) || (*(int *)(pcVar5 + 4) == 0)) {
      CString::Format((dword)&local_9c,0x1001a284);
      dVar12 = 2;
      local_94.right = (LONG)&stack0xffffff50;
      dVar11 = extraout_ECX_04;
      CString::CString((CString *)&stack0xffffff50,(CString *)&local_98);
      FUN_100053f0((int)param_1 + 0x20,param_2 + 0xc5,dVar11,dVar12);
      CString::Format((dword)&local_98,0x1001a284,*(undefined4 *)(pcVar2 + 0x38));
      dVar11 = extraout_ECX_05;
      goto LAB_10005a8c;
    }
    uVar7 = *(int *)(pcVar2 + 0x74) - *(int *)(pcVar5 + 0x74) >> 0x1f;
    local_94.top = (*(int *)(pcVar2 + 0x74) - *(int *)(pcVar5 + 0x74) ^ uVar7) - uVar7;
    uVar7 = *(int *)(pcVar2 + 0x70) - *(int *)(pcVar5 + 0x70) >> 0x1f;
    if ((int)(local_94.top + ((*(int *)(pcVar2 + 0x70) - *(int *)(pcVar5 + 0x70) ^ uVar7) - uVar7))
        <= *(int *)(pcVar5 + 0x4c)) {
      local_94.left = local_94.left + *(int *)(pcVar5 + 0x44);
    }
    CString::Format((dword)&local_9c,0x1001a284);
    dVar12 = 2;
    local_94.right = (LONG)&stack0xffffff50;
    dVar11 = extraout_ECX_02;
    CString::CString((CString *)&stack0xffffff50,(CString *)&local_98);
    FUN_100053f0((int)param_1 + 0x20,param_2 + 0xc5,dVar11,dVar12);
    CString::Format((dword)&local_98,0x1001a284,local_94.top);
    dVar12 = 2;
    dVar11 = extraout_ECX_03;
    local_94.bottom = (LONG)&stack0xffffff54;
    CString::CString((CString *)&stack0xffffff54,(CString *)&local_94);
  }
  FUN_100053f0((int)param_1 + 0x57,param_2 + 0xc5,dVar11,dVar12);
  if (DAT_1006c118 == 0) {
    bVar10 = (*(byte *)(local_94.top + 0x104) & 1) != 0;
    if (bVar10) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,DAT_1001a0f0 + (int)param_1,DAT_1001a0f4 + param_2,
                          DAT_1006a704,(dword)local_84,1);
    }
    uVar7 = (uint)bVar10;
    if ((*(uint *)(local_94.top + 0x104) & 0x100) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 0x80) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 8) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 0x40) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 0x20) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 4) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 0x10) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x400) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x200) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x800) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x4000) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x1000) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x2000) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(byte *)(local_94.top + 0x104) & 2) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
      uVar7 = uVar7 + 1;
    }
    if ((*(uint *)(local_94.top + 0x104) & 0x8000) != 0) {
      (*pcVar9)();
      CDDSurface::BltFast(DAT_1001c470,(&DAT_1001a0f0)[uVar7 * 2] + (int)param_1,
                          (&DAT_1001a0f4)[uVar7 * 2] + param_2,DAT_1006a704,(dword)local_84,1);
    }
  }
  if (DAT_1001c450 != (code *)0x0) {
    CString::operator=((CString *)&local_94,(CString *)(local_94.top + 0x114));
    LVar1 = local_94.top;
    iVar3 = 0;
    psVar6 = (short *)&DAT_1006bd12;
    do {
      if (*(int *)(local_94.top + 100) == (int)*psVar6) {
        CString::operator=((CString *)&local_94,(char *)((int)&DAT_1006bd14 + iVar3 * 0x2a));
        break;
      }
      psVar6 = psVar6 + 0x15;
      iVar3 = iVar3 + 1;
    } while ((int)psVar6 < 0x1006be8c);
    (*DAT_1001c450)(DAT_1001c470);
    CString::operator=((CString *)&local_94,(CString *)(LVar1 + 0x118));
    (*DAT_1001c450)(DAT_1001c470);
    if (*(int *)(LVar1 + 0x14) != 0) {
      CString::Format((dword)&local_94,0x1001a278);
      (*DAT_1001c450)(DAT_1001c470,(int)param_1 + 0xc,param_2 + 0x29);
    }
  }
LAB_10006119:
  uStack0000000c = 0xffffffff;
  CString::~CString((CString *)&local_94);
  ExceptionList = param_1;
  return;
}

