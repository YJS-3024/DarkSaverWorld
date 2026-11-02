
/* void __cdecl GetFace(class CDDSurface *,int,int,int) */

void __cdecl GetFace(CDDSurface *param_1,int param_2,int param_3,int param_4)

{
  int iVar1;
  CMemFile *pCVar2;
  long lVar3;
  CDDSurface *pCVar4;
  CString *extraout_ECX;
  CString *extraout_ECX_00;
  CString *extraout_ECX_01;
  CString *this;
  int iVar5;
  CString local_28 [4];
  undefined4 local_24;
  undefined4 local_20;
  BB_Bitmap aBStack_1c [16];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0xf180  30  ?GetFace@@YAXPAVCDDSurface@@HHH@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10011f68;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_24);
  local_4 = 0;
  CString::CString(local_28);
  local_4._0_1_ = 1;
  BB_Bitmap::BB_Bitmap((BB_Bitmap *)&local_20);
  local_4 = CONCAT31(local_4._1_3_,2);
  CString::Format((CString *)&local_24,(char *)&local_24);
  iVar5 = 0;
  this = extraout_ECX;
  do {
    if (iVar5 == 0) {
      CString::operator=((CString *)&local_24,s_gameres_face_arc_10016244);
    }
    else {
      CString::Format(this,(char *)&local_24);
    }
    iVar1 = (**(code **)(DAT_1010b3e0 + 0x14))(local_24);
    this = extraout_ECX_00;
    if (iVar1 != 0) {
      iVar1 = (**(code **)(DAT_1010b3e0 + 0xc))(local_20);
      this = extraout_ECX_01;
      if (iVar1 != 0) goto LAB_1000f252;
    }
    iVar5 = iVar5 + 1;
  } while (iVar5 < 4);
  CString::operator=((CString *)&local_24,s_gameres_etc_arc_10016208);
  CString::operator=((CString *)&local_20,s_noface_bmp_10016224);
LAB_1000f252:
  pCVar2 = BB_CompressDllMng::WriteMemFile(DAT_1010b3e0,(CString *)&local_24,(CString *)&local_20);
  if (pCVar2 != (CMemFile *)0x0) {
    lVar3 = (**(code **)(*(int *)pCVar2 + 0x38))();
    pCVar4 = BB_Bitmap::CreateSurfaceFromLib(DAT_1010b3a4,(CFile *)pCVar2,0,lVar3);
    BB_CompressDllMng::DeleteMemFile(DAT_1010b3e0,pCVar2);
    if (pCVar4 != (CDDSurface *)0x0) {
      CDDSurface::BltFast((CDDSurface *)param_2,0,0,pCVar4,0);
      (**(code **)(*(int *)pCVar4 + 4))(1);
    }
  }
  (**(code **)(DAT_1010b3e0 + 4))();
  BB_Bitmap::~BB_Bitmap(aBStack_1c);
  CString::~CString((CString *)&local_24);
  CString::~CString((CString *)&local_20);
  ExceptionList = puStack_8;
  return;
}

