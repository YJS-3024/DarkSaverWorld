
void __cdecl FUN_10006830(int param_1,int param_2)

{
  character *pcVar1;
  int iVar2;
  CDDSurface *arg3;
  int iVar3;
  dword extraout_ECX;
  dword extraout_ECX_00;
  undefined4 *puVar4;
  code *pcVar5;
  int iVar6;
  dword dVar7;
  dword dVar8;
  int local_98;
  undefined1 *local_94;
  undefined1 local_90 [16];
  tagRECT local_80;
  undefined4 local_70 [25];
  void *pvStack_c;
  undefined1 *puStack_8;
  void *local_4;
  
  local_4 = (void *)0xffffffff;
  puStack_8 = &LAB_10014feb;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_94);
  local_4 = (void *)0x0;
  DAT_1001c4f0 = (DAT_1001c4f0 + 1) % 3;
  if (DAT_1001a380 < 0) {
    pcVar1 = GetUnitInfoP(DAT_1006a738 * 0x32);
  }
  else {
    pcVar1 = GetUnitInfoP(DAT_1001a380);
  }
  iVar2 = getsnum(*(int *)(pcVar1 + 0x10));
  arg3 = GetCharSurface(*(int *)(pcVar1 + 0x68),iVar2);
  pcVar5 = SetRect_exref;
  if (arg3 != (CDDSurface *)0x0) {
    if (*(int *)(pcVar1 + 0x68) / 100 == 7) {
      puVar4 = local_70;
      for (iVar2 = 0x19; iVar2 != 0; iVar2 = iVar2 + -1) {
        *puVar4 = 0;
        puVar4 = puVar4 + 1;
      }
      local_70[0] = 100;
      SetRect((LPRECT)local_90,0x40,0x40,0x80,0x80);
      SetRect(&local_80,param_1 + 7,param_2 + 9,param_1 + 0x27,param_2 + 0x29);
      CDDSurface::Blt(DAT_1001c470,&local_80,arg3,(tagRECT *)local_90,0x8000,(_DDBLTFX *)local_70);
    }
    else {
      FUN_10005300((dword)DAT_1001c470,param_1 + 7,param_2 + 9,(dword)arg3,0x20,0x20,0x40,0x40,0);
      pcVar5 = SetRect_exref;
    }
  }
  iVar2 = *(int *)(pcVar1 + 0x5c);
  if (iVar2 != 0) {
    local_98 = *(int *)(pcVar1 + 0x60);
    iVar3 = local_98;
    if (iVar2 < local_98) {
      iVar3 = iVar2;
    }
    if (local_98 < iVar2) {
      iVar6 = 0x172;
    }
    else {
      iVar6 = DAT_1001c4f0 * 0xc + 0x172;
    }
    (*pcVar5)(local_90,2,iVar6,(iVar3 * 0x30) / iVar2 + 2);
    CDDSurface::BltFast(DAT_1001c470,param_1 + 0x43,param_2 + 8,DAT_1006a704,(dword)local_90,1);
  }
  iVar2 = *(int *)(pcVar1 + 0x24);
  if (iVar2 != 0) {
    iVar3 = *(int *)(pcVar1 + 0x20);
    if (iVar2 < *(int *)(pcVar1 + 0x20)) {
      iVar3 = iVar2;
    }
    (*pcVar5)(local_90,2,0x17e,(iVar3 * 0x30) / iVar2 + 2);
    CDDSurface::BltFast(DAT_1001c470,param_1 + 0x43,param_2 + 0x15,DAT_1006a704,(dword)local_90,1);
  }
  CString::Format((dword)&local_94,0x1001a290,*(undefined4 *)(pcVar1 + 0x20));
  dVar8 = 2;
  local_94 = &stack0xffffff50;
  dVar7 = extraout_ECX;
  CString::CString((CString *)&stack0xffffff50,(CString *)local_90);
  local_94 = (undefined1 *)(param_1 + 0x47);
  FUN_100053f0((dword)local_94,param_2 + 0x15,dVar7,dVar8);
  if (*(int *)(local_98 + 0x2c) != 0) {
    (*pcVar5)(local_90 + 4,2,0x18a);
    CDDSurface::BltFast(DAT_1001c470,param_1 + 0x43,param_2 + 0x22,DAT_1006a704,
                        (dword)(local_90 + 4),1);
  }
  CString::Format((dword)local_90,0x1001a290);
  dVar8 = 2;
  local_94 = &stack0xffffff54;
  dVar7 = extraout_ECX_00;
  CString::CString((CString *)&stack0xffffff54,(CString *)(local_90 + 4));
  FUN_100053f0(local_90._0_4_,param_2 + 0x22,dVar7,dVar8);
  CString::~CString((CString *)(local_90 + 4));
  ExceptionList = local_4;
  return;
}

