
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl SetInitialValues(struct HWND__ *,class CDirectDraw *,class CDDSurface *,class
   CDDSurface *,class BBDX_MapManager *) */

void __cdecl
SetInitialValues(HWND__ *param_1,CDirectDraw *param_2,CDDSurface *param_3,CDDSurface *param_4,
                BBDX_MapManager *param_5)

{
  BBDX_MapManager *pBVar1;
  CString *pCVar2;
  CMemFile *pCVar3;
  long lVar4;
  HWND__ *unaff_retaddr;
  CString local_18 [4];
  CString local_14 [4];
  void *local_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  int local_4;
  
                    /* 0x4500  77
                       ?SetInitialValues@@YAXPAUHWND__@@PAVCDirectDraw@@PAVCDDSurface@@2PAVBBDX_MapManager@@@Z
                        */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014f34;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_10);
  local_4 = 0;
  CString::CString(local_14);
  local_4._0_1_ = 1;
  CString::CString(local_18);
  pBVar1 = param_5;
  local_4 = CONCAT31(local_4._1_3_,2);
  DAT_1001c48c = param_2;
  DAT_1001c490 = param_5;
  if (DAT_1001c484 != (CDDSurface *)0x0) {
    (**(code **)(*(int *)DAT_1001c484 + 4))(1);
    DAT_1001c484 = (CDDSurface *)0x0;
  }
  if (DAT_1001c488 != (CDDSurface *)0x0) {
    (**(code **)(*(int *)DAT_1001c488 + 4))(1);
    DAT_1001c488 = (CDDSurface *)0x0;
  }
  if (DAT_1001c480 != (CDDSurface *)0x0) {
    (**(code **)(*(int *)DAT_1001c480 + 4))(1);
    DAT_1001c480 = (CDDSurface *)0x0;
  }
  param_2 = operator_new(0x8c);
  local_4._0_1_ = 3;
  if (param_2 == (CDirectDraw *)0x0) {
    DAT_1001c484 = (CDDSurface *)0x0;
  }
  else {
    DAT_1001c484 = (CDDSurface *)CDDSurface::CDDSurface((CDDSurface *)param_2);
  }
  local_4._0_1_ = 2;
  CDDSurface::Create(DAT_1001c484,DAT_1001c48c,0xf8,0x8a,0);
  CDDSurface::SetColorKey(DAT_1001c484,0);
  param_2 = operator_new(0x8c);
  local_4._0_1_ = 4;
  if (param_2 == (CDirectDraw *)0x0) {
    DAT_1001c488 = (CDDSurface *)0x0;
  }
  else {
    DAT_1001c488 = (CDDSurface *)CDDSurface::CDDSurface((CDDSurface *)param_2);
  }
  local_4._0_1_ = 2;
  CDDSurface::Create(DAT_1001c488,DAT_1001c48c,0x1b8,0x15e,0);
  CDDSurface::SetColorKey(DAT_1001c488,0);
  if (DAT_1001a8b0 == (BB_CompressDllMng *)0x0) {
    local_4._0_1_ = 1;
    CString::~CString(local_18);
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString(local_14);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_10);
    ExceptionList = pvStack_c;
    return;
  }
  pCVar2 = (CString *)operator+((CString *)&param_2,&DAT_1001a8b4);
  puStack_8._0_1_ = 5;
  CString::operator=(local_18,pCVar2);
  puStack_8 = (undefined1 *)CONCAT31(puStack_8._1_3_,2);
  CString::~CString((CString *)&param_1);
  CString::operator=((CString *)&stack0xffffffe4,s_mcursor_bmp_1001a258);
  pCVar3 = BB_CompressDllMng::WriteMemFile(DAT_1001a8b0,local_18,(CString *)&stack0xffffffe4);
  if (pCVar3 != (CMemFile *)0x0) {
    lVar4 = (**(code **)(*(int *)pCVar3 + 0x38))();
    DAT_1001c480 = BB_Bitmap::CreateSurfaceFromLib(DAT_1001c48c,(CFile *)pCVar3,0,lVar4);
    if (DAT_1001c480 != (CDDSurface *)0x0) {
      CDDSurface::SetColorKey(DAT_1001c480,0);
    }
    BB_CompressDllMng::DeleteMemFile(DAT_1001a8b0,pCVar3);
  }
  _DAT_1001c474 = param_2;
  _DAT_1001c478 = param_3;
  DAT_1001c470 = param_3;
  DAT_1001c468 = unaff_retaddr;
  userclearstatusall();
  InitDirectScreen(unaff_retaddr,DAT_1001c48c,DAT_1001c470,pBVar1);
  DAT_1006a704 = GetToneSurface();
  DAT_1001c47c = GetIconSurface();
  puStack_8._0_1_ = 1;
  CString::~CString((CString *)&stack0xffffffe4);
  puStack_8 = (undefined1 *)((uint)puStack_8._1_3_ << 8);
  CString::~CString(local_18);
  puStack_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_14);
  ExceptionList = local_10;
  return;
}

