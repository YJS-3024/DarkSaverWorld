
/* void __cdecl DlgToggleButtonAction(int,int,int) */

void __cdecl DlgToggleButtonAction(int param_1,int param_2,int param_3)

{
  int iVar1;
  short sVar2;
  undefined4 uVar3;
  CString local_10 [4];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0x5080  24  ?DlgToggleButtonAction@@YAXHHH@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014f68;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_10);
  local_4 = 0;
  if (param_1 == 3) {
    if (-1 < DAT_1006a738) {
      iVar1 = DAT_1001c4b8 + -2 + param_2;
      if (DAT_1006c118 == 0) {
        if (iVar1 < DAT_1006bd10) {
          sVar2 = *(short *)(&DAT_1006bd12 + iVar1 * 0x2a);
          if (sVar2 == DAT_1006a738) {
            (*DAT_1001c44c)(3,param_2,1);
          }
          else if (param_3 == 0) {
            FUN_1000e9d0(sVar2);
          }
          else if (param_3 == 1) {
            FUN_1000e970(sVar2);
          }
        }
        else {
          (*DAT_1001c44c)(3,param_2,0);
        }
      }
      else if ((DAT_1006c0f0 == 0xf) && (iVar1 == 1)) {
        DAT_1006c104 = 1;
      }
    }
    goto LAB_100051d6;
  }
  if (param_1 != 0xc) goto LAB_100051d6;
  if (param_2 == 0) {
    DAT_1001a8f4 = 4;
    uVar3 = 1;
LAB_10005107:
    (*DAT_1001c44c)(0xc,uVar3,0);
    uVar3 = 2;
LAB_10005113:
    (*DAT_1001c44c)(0xc,uVar3,0);
  }
  else {
    if (param_2 == 1) {
      DAT_1001a8f4 = 2;
      uVar3 = 0;
      goto LAB_10005107;
    }
    if (param_2 == 2) {
      DAT_1001a8f4 = 8;
      (*DAT_1001c44c)(0xc,0,0);
      uVar3 = 1;
      goto LAB_10005113;
    }
  }
  if (param_3 == 0) {
    DAT_1001a8f4 = 1;
  }
LAB_100051d6:
  local_4 = 0xffffffff;
  CString::~CString(local_10);
  ExceptionList = pvStack_c;
  return;
}

