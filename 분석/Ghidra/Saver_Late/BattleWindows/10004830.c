
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl DlgLButtonDown(int,int,int) */

void __cdecl DlgLButtonDown(int param_1,int param_2,int param_3)

{
  clock_t cVar1;
  int iVar2;
  CString local_bc [4];
  int local_b8;
  undefined1 auStack_b4 [4];
  char acStack_b0 [164];
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
                    /* 0x4830  23  ?DlgLButtonDown@@YAXHHH@Z */
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10014f4b;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_bc);
  local_4 = 0;
  if (DAT_1006c118 == 0) {
    switch(param_1) {
    case 0:
      if (0x14 < param_3) {
        DAT_1001c460 = ((param_2 + -0xb) * DAT_1001c45c) / 100 - DAT_1001a0b8 / 2;
        iVar2 = ((param_3 + -0x19) * DAT_1001c458) / 100 - DAT_1001a0bc / 2;
        if (DAT_1001c460 < 0) {
          DAT_1001c460 = 0;
        }
        if (iVar2 < 0) {
          iVar2 = 0;
        }
        if (DAT_1001c45c - DAT_1001a0b8 <= DAT_1001c460) {
          DAT_1001c460 = DAT_1001c45c - DAT_1001a0b8;
        }
        DAT_1001c464 = DAT_1001c458 - DAT_1001a0bc;
        if (iVar2 < DAT_1001c464) {
          DAT_1001c464 = iVar2;
        }
      }
      break;
    case 8:
      if (*DAT_1006a708 != 0) {
        FUN_10008ca0(DAT_1006a708);
      }
      break;
    case 9:
      cVar1 = clock();
      if (cVar1 - _DAT_1001a0ec < 400) {
        FUN_100029d0(0x4cb,9,0);
        FUN_100029d0(0x4ca,1,0);
      }
      _DAT_1001a0ec = clock();
      break;
    case 0x13:
      iVar2 = (param_3 + -0x39) / 0x13;
      if ((-1 < iVar2) && (iVar2 < 0xc)) {
        iVar2 = iVar2 + DAT_1001c4bc;
        (*DAT_1001c444)(0xffffffff,&local_b8);
        if ((iVar2 < local_b8) && ((*DAT_1001c444)(iVar2,auStack_b4), acStack_b0[0] != '\0')) {
          CString::operator=((CString *)&DAT_1001a8f0,acStack_b0);
        }
      }
    }
  }
  else if ((DAT_1006c104 == 0) && (DAT_1006c0f0 == 0x10)) {
    DAT_1006c104 = 1;
  }
  local_4 = 0xffffffff;
  CString::~CString(local_bc);
  ExceptionList = local_c;
  return;
}

