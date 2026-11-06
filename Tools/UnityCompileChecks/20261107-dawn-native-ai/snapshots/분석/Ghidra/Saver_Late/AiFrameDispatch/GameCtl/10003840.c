
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* int __cdecl Update(int) */

int __cdecl Update(int param_1)

{
  int iVar1;
  clock_t cVar2;
  CString local_20 [4];
  tagRECT local_1c;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0x3840  87  ?Update@@YAHH@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014e18;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_20);
  local_4 = 0;
  if (((DAT_1001c46c == 0) && (DAT_10093864 != (char *)0x0)) && (*(int *)(DAT_10093864 + 4) == 2)) {
    iVar1 = FUN_10010f20(DAT_10093864);
    while (iVar1 == 0) {
      iVar1 = FUN_10010f20(DAT_10093864);
    }
  }
  else {
    switch(param_1) {
    case 1:
      if (*(int *)(DAT_10093864 + 4) != 2) {
        aiexcute();
      }
      break;
    case 3:
      if (DAT_1001c46c != 0) {
        if (DAT_1001c4ac != 0) {
          DAT_1001c4ac = 0;
        }
        iVar1 = FUN_10008530(DAT_1001c470,DAT_1001c460,DAT_1001c464);
        local_4 = 0xffffffff;
        CString::~CString(local_20);
        ExceptionList = local_c;
        return iVar1;
      }
      break;
    case 4:
      if ((DAT_1001c46c != 0) && (SetMainView(DAT_1001c460,DAT_1001c464), DAT_1001c4d4 == 0)) {
        _UpdateCharacter(0);
      }
      break;
    case 5:
      if ((DAT_1001c46c != 0) && (DAT_1001c4d4 == 0)) {
        _UpdateCharacter(1);
      }
      break;
    case 6:
      if ((DAT_1001c46c != 0) && (FUN_100086f0(DAT_1001c470), DAT_1001c4d4 == 0)) {
        _UpdateCharacter(2);
      }
      break;
    case 7:
      FUN_100036a0();
      if (DAT_1001c4d4 != 0) {
        FUN_10002030();
      }
      break;
    case 8:
      iVar1 = mainmapgetchar(DAT_1001a8e8 + DAT_1001c460,DAT_1001a8e4 + DAT_1001c464);
      if (-1 < iVar1) {
        FUN_10002bf0(0);
      }
      break;
    case 9:
      FUN_1000a710(DAT_1006a70c);
      if (((DAT_1006c118 != 0) && (-1 < DAT_1001a4ec)) && (-1 < DAT_1001a4f0)) {
        cVar2 = clock();
        iVar1 = ((cVar2 / 100) % 3) * 0x2e;
        SetRect(&local_1c,iVar1 + 0x9f,0x137,iVar1 + 0xcb,0x163);
        CDDSurface::BltFast(DAT_1001c470,(DAT_1001a4ec - DAT_1001c460) * 0x20 + -6,
                            (DAT_1001a4f0 - DAT_1001c464) * 0x20 + -6,DAT_1006a704,&local_1c,1);
      }
      break;
    case 100:
      if ((DAT_10093864 == (char *)0x0) || (*(int *)(DAT_10093864 + 4) != 2)) {
        if ((DAT_1001a0e4 < 2) && (0 < DAT_1001c460)) {
          DAT_1001c460 = DAT_1001c460 + -1;
        }
        if ((0x27e < DAT_1001a0e4) && (DAT_1001c460 < *(int *)(DAT_1001c490 + 0x88) - DAT_1001a0b8))
        {
          DAT_1001c460 = DAT_1001c460 + 1;
        }
        if ((DAT_1001a0e8 < 2) && (0 < DAT_1001c464)) {
          DAT_1001c464 = DAT_1001c464 + -1;
        }
        if ((0x1de < DAT_1001a0e8) && (DAT_1001c464 < *(int *)(DAT_1001c490 + 0x8c) - DAT_1001a0bc))
        {
          DAT_1001c464 = DAT_1001c464 + 1;
        }
      }
      break;
    case 200:
      if (DAT_1001c440 != (code *)0x0) {
        (*DAT_1001c440)(0x32,0x1c7,400,0x1db,DAT_1001c470);
      }
      FUN_10002a60();
      if (*DAT_1006a708 != 0) {
        FUN_10008ad0(DAT_1006a708);
      }
      if (*(int *)(DAT_10093864 + 0xc) != 0) {
        iVar1 = FUN_10010f20(DAT_10093864);
        while (iVar1 == 0) {
          iVar1 = FUN_10010f20(DAT_10093864);
        }
      }
    }
    _DAT_1001c4e4 = _DAT_1001c4e4 + 1;
  }
  local_4 = 0xffffffff;
  CString::~CString(local_20);
  ExceptionList = local_c;
  return 1;
}

