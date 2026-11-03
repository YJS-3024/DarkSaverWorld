
/* void __cdecl DlgButtonAction(int,int) */

void __cdecl DlgButtonAction(int param_1,int param_2)

{
  character *pcVar1;
  int iVar2;
  int iVar3;
  CString *this;
  
                    /* 0x4a80  20  ?DlgButtonAction@@YAXHH@Z */
  DAT_1001c4c0 = (uint)(DAT_1006c118 != 0);
  switch(param_1) {
  case 0:
    if (param_2 == 0) {
      FUN_100029d0(0x4cf,0,0);
      return;
    }
    if (param_2 == 1) {
      FUN_100029d0(0x4cb,0,0);
      return;
    }
    break;
  case 1:
    if (param_2 == 0) {
      FUN_100029d0(0x4ca,9,0);
      FUN_100029d0(0x4cb,1,0);
      return;
    }
    if (param_2 == 1) {
      FUN_100029d0(0x4cb,1,0);
      return;
    }
    break;
  case 3:
    if (param_2 == 0) {
      FUN_100029d0(0x4cf,3,0);
      return;
    }
    if (param_2 == 1) {
      FUN_100029d0(0x4cb,3,0);
      return;
    }
    break;
  case 4:
    switch(param_2) {
    case 0:
      iVar2 = FUN_100029d0(0x4d1,0x1c,0);
      if (iVar2 == 0) {
        FUN_100029d0(0x4ca,0x1c,0);
        FUN_100029d0(0x4d0,0x1c,0xba00e4);
        return;
      }
      break;
    case 1:
      FUN_100029d0(0x4ca,7,0);
      FUN_100029d0(0x4d0,7,0xa000a);
      return;
    case 2:
      FUN_100029d0(0x4ca,6,0);
      FUN_100029d0(0x4d0,2,0x880200);
      return;
    case 3:
      iVar2 = -1;
      iVar3 = 0;
      do {
        pcVar1 = GetUnitInfoP((iVar3 + DAT_1006a738 * 5) * 10);
        if ((*(int *)pcVar1 != 0) && (*(int *)(pcVar1 + 4) != 0)) {
          iVar2 = iVar2 + 1;
        }
        iVar3 = iVar3 + 1;
      } while (iVar3 < 4);
      if (iVar2 < 0) {
        iVar2 = 0;
      }
      iVar2 = iVar2 + 0x18;
      iVar3 = FUN_100029d0(0x4d1,iVar2,0);
      if (iVar3 == 0) {
        FUN_100029d0(0x4ca,iVar2,0);
        FUN_100029d0(0x4d0,iVar2,0xe70005);
        return;
      }
      FUN_100029d0(0x4cb,0x18,0);
      FUN_100029d0(0x4cb,0x19,0);
      FUN_100029d0(0x4cb,0x1a,0);
      FUN_100029d0(0x4cb,0x1b,0);
      return;
    case 4:
      FUN_100029d0(0x4ca,0,0);
      FUN_100029d0(0x4d0,0,0x50203);
      return;
    case 5:
      FUN_100029d0(0x4ca,5,0);
      FUN_100029d0(0x4d0,5,0xba00e4);
      return;
    case 6:
      FUN_100029d0(0x4ca,3,0);
      FUN_100029d0(0x4d0,3,0x12e01c2);
      return;
    case 7:
      FUN_100029d0(0x4ca,1,0);
      FUN_100029d0(0x4cb,9,0);
      FUN_100029d0(0x4d0,1,0x50005);
      return;
    case 8:
      FUN_100029d0(0x4cb,4,0);
      return;
    }
    break;
  case 5:
    if (param_2 == 0) {
      FUN_100029d0(0x4cb,5,0);
      return;
    }
    break;
  case 6:
    if (param_2 == 0) {
      FUN_100029d0(0x4cb,6,0);
      return;
    }
    break;
  case 7:
    if (param_2 == 0) {
      DAT_1001c4b4 = ((int)(DAT_1001c4b4 - 1) < 0) - 1 & DAT_1001c4b4 - 1;
    }
    else {
      if (param_2 != 1) {
        if (param_2 != 2) {
          return;
        }
        FUN_100029d0(0x4cb,7,0);
        return;
      }
      DAT_1001c4b4 = DAT_1001c4b4 + 1;
      if ((int)(DAT_1001c4d0 - 0x12U) < (int)DAT_1001c4b4) {
        DAT_1001c4b4 = DAT_1001c4d0 - 0x12U;
        return;
      }
    }
    return;
  case 8:
    if (((param_2 == 0) && (DAT_1006c118 == 0)) && (DAT_1001c434 != 0)) {
      if (DAT_1006a740 != 1) {
        DAT_1001a8ec = 0;
        PostMessageA(DAT_1001c468,0x590,0,0);
        PostMessageA(DAT_1001c468,0x4cd,0,0);
        FUN_1000e750();
        return;
      }
      CString::CString((CString *)&stack0xfffffff0,&DAT_1001a274);
      FUN_1000fab0(DAT_10093864);
      iVar2 = 0;
      do {
        FUN_100029d0(0x4cb,iVar2,0);
        iVar2 = iVar2 + 1;
      } while (iVar2 < 10);
      FUN_10008d20();
      return;
    }
    break;
  case 0xc:
    if (param_2 == 3) {
      iVar2 = FUN_100029d0(0x4d1,4,0);
      if (iVar2 == 0) {
        FUN_100029d0(0x4ca,4,0);
        return;
      }
      FUN_100029d0(0x4cb,4,0);
      return;
    }
    if (param_2 == 4) {
      this = (CString *)&DAT_1001bde4;
      do {
        CString::Empty(this);
        this = this + 8;
      } while ((int)this < 0x1001c10c);
      return;
    }
    break;
  case 0x13:
    if (param_2 == 0) {
      FUN_100029d0(0x4cb,0x13,0);
      return;
    }
    break;
  case 0x18:
  case 0x19:
  case 0x1a:
  case 0x1b:
    if (param_2 == 0) {
      FUN_100029d0(0x4cf,param_1,0);
      return;
    }
    if (param_2 == 1) {
      FUN_100029d0(0x4cb,param_1,0);
      return;
    }
    break;
  case 0x1c:
    if (param_2 == 0) {
      FUN_1000e780();
      FUN_100029d0(0x4cb,0x1c,0);
      iVar2 = FUN_1000f040(DAT_1006c0e8,-1);
      if ((iVar2 < 999) && (DAT_1006c084 == 1)) {
        FUN_100029d0(0x4ca,8,0);
        FUN_100029d0(0x4d0,8,0xa006e);
      }
    }
    else if (param_2 == 1) {
      FUN_100029d0(0x4cb,0x1c,0);
      return;
    }
  }
  return;
}

