
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void FUN_10004150(void)

{
  char *pcVar1;
  int iVar2;
  undefined4 *unaff_ESI;
  CString *pCVar3;
  undefined4 *puVar4;
  CString local_190 [4];
  CString aCStack_18c [4];
  char *pcStack_188;
  CString aCStack_184 [4];
  tagRECT tStack_180;
  tagRECT tStack_170;
  CDDSurface aCStack_160 [8];
  CDDSurface aCStack_158 [132];
  undefined4 local_d4 [25];
  undefined4 auStack_70 [20];
  undefined4 uStack_20;
  void *pvStack_14;
  void *pvStack_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014eed;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_190);
  puVar4 = local_d4;
  for (iVar2 = 0x19; iVar2 != 0; iVar2 = iVar2 + -1) {
    *puVar4 = 0;
    puVar4 = puVar4 + 1;
  }
  local_4 = 0;
  local_d4[0] = 100;
  pCVar3 = (CString *)&DAT_1001bde4;
  do {
    CString::Empty(pCVar3);
    pCVar3 = pCVar3 + 8;
  } while ((int)pCVar3 < 0x1001c10c);
  if (DAT_1001c49c != (code *)0x0) {
    iVar2 = (*DAT_1001c49c)(DAT_1006c0e8);
    if (iVar2 != 0) {
      CDDSurface::CDDSurface(aCStack_158);
      CString::CString((CString *)&tStack_180);
      unaff_ESI = &DAT_1006c0e8;
      DAT_1001c46c = 1;
      pcVar1 = (char *)operator+((char *)&tStack_180.top,(CString *)&DAT_1001a1f8);
      pCVar3 = (CString *)operator+((CString *)&pcStack_188,pcVar1);
      local_4._0_1_ = 4;
      CString::operator=((CString *)&pcStack_188,pCVar3);
      local_4._0_1_ = 3;
      CString::~CString(aCStack_18c);
      local_4 = CONCAT31(local_4._1_3_,2);
      CString::~CString(aCStack_184);
      iVar2 = CDDSurface::CreateFromImage(aCStack_160,DAT_1001c48c,pcStack_188,0);
      if (iVar2 == 0) {
        auStack_70[0] = 100;
        uStack_20 = 0;
        (**(code **)(**(int **)(DAT_1001c484 + 0x84) + 0x14))
                  (*(int **)(DAT_1001c484 + 0x84),&tStack_180,0,0,0x400,auStack_70);
      }
      else {
        CDDSurface::SetColorKey(aCStack_160,0);
        CDDSurface::GetRect(aCStack_160,&tStack_170);
        SetRect(&tStack_180,0x8a,0,0xee,100);
        CDDSurface::Blt(DAT_1001c484,&tStack_180,aCStack_160,&tStack_170,0x8000,(_DDBLTFX *)local_d4
                       );
      }
      FUN_10004050();
      FUN_10003d10(*(int *)(DAT_1001c490 + 0x88) / 2,*(int *)(DAT_1001c490 + 0x8c) / 2);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&pcStack_188);
      local_4 = (uint)local_4._1_3_ << 8;
      CDDSurface::~CDDSurface(aCStack_160);
      goto LAB_1000435a;
    }
  }
  DAT_1001c46c = 0;
LAB_1000435a:
  LoadScenarioBriefing();
  setspeedmove(0);
  setusercurrentscenario(DAT_1006c0e8);
  LoadScenarioEventActions();
  _DAT_1001c4cc = 0;
  DAT_1006ad4c = 0;
  DAT_1006a890 = 0;
  userclearstatusall();
  setgamestatus(1);
  FUN_1000ea80();
  _DAT_1001c4b0 = 0;
  DAT_1001a8e0 = 0;
  DAT_1001a8f4 = 1;
  DAT_1001c434 = 0;
  pcVar1 = (char *)operator+((char *)aCStack_18c,(CString *)&DAT_1001a244);
  puStack_8 = (undefined1 *)CONCAT31(puStack_8._1_3_,5);
  pCVar3 = (CString *)operator+((CString *)&pcStack_188,pcVar1);
  pvStack_c._0_1_ = 6;
  CString::operator=((CString *)&stack0xfffffe68,pCVar3);
  pvStack_c._0_1_ = 5;
  CString::~CString(aCStack_18c);
  pvStack_c = (void *)((uint)pvStack_c._1_3_ << 8);
  CString::~CString((CString *)&stack0xfffffe6c);
  SendMessageA(DAT_1001c468,0x591,1,(LPARAM)unaff_ESI);
  pvStack_c = (void *)0xffffffff;
  CString::~CString((CString *)&stack0xfffffe68);
  ExceptionList = pvStack_14;
  return;
}

