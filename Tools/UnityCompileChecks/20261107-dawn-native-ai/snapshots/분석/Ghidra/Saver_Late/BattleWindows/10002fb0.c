
void __cdecl FUN_10002fb0(undefined4 param_1)

{
  character *pcVar1;
  int iVar2;
  int iVar3;
  
  switch(param_1) {
  case 0x25:
    FUN_10003d10(DAT_1001c460 + 9,DAT_1001c464 + 7);
    return;
  case 0x26:
    FUN_10003d10(DAT_1001c460 + 10,DAT_1001c464 + 6);
    return;
  case 0x27:
    FUN_10003d10(DAT_1001c460 + 0xb,DAT_1001c464 + 7);
    return;
  case 0x28:
    FUN_10003d10(DAT_1001c460 + 10,DAT_1001c464 + 8);
  default:
    return;
  case 0x70:
    iVar2 = FUN_100029d0(0x4d1,7,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,7,0);
      return;
    }
    FUN_100029d0(0x4ca,7,0);
    FUN_100029d0(0x4d0,7,0xa000a);
    return;
  case 0x71:
    iVar2 = FUN_100029d0(0x4d1,1,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,1,0);
      return;
    }
    FUN_100029d0(0x4ca,1,0);
    FUN_100029d0(0x4cb,9,0);
    FUN_100029d0(0x4d0,1,0xb40005);
    return;
  case 0x72:
    iVar2 = FUN_100029d0(0x4d1,3,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,3,0);
      return;
    }
    FUN_100029d0(0x4ca,3,0);
    FUN_100029d0(0x4d0,3,0x12e01c2);
    return;
  case 0x73:
    iVar2 = FUN_100029d0(0x4d1,5,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,5,0);
      return;
    }
    FUN_100029d0(0x4ca,5,0);
    FUN_100029d0(0x4d0,5,0xba00e4);
    return;
  case 0x74:
    break;
  case 0x75:
    iVar2 = FUN_100029d0(0x4d1,0,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,0,0);
      return;
    }
    FUN_100029d0(0x4ca,0,0);
    FUN_100029d0(0x4d0,0,0x50203);
    return;
  case 0x77:
    iVar2 = FUN_100029d0(0x4d1,6,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,6,0);
      return;
    }
    FUN_100029d0(0x4ca,6,0);
    FUN_100029d0(0x4d0,6,0x880200);
    return;
  case 0x78:
    iVar2 = FUN_100029d0(0x4d1,0x1c,0);
    if (iVar2 != 0) {
      FUN_100029d0(0x4cb,0x1c,0);
      return;
    }
    FUN_100029d0(0x4ca,0x1c,0);
    FUN_100029d0(0x4d0,0x1c,0xba00e4);
    return;
  case 0x7b:
    FUN_100029d0(0x4ca,4,0);
    return;
  }
  if (-1 < DAT_1006a738) {
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
  }
  FUN_100029d0(0x4cb,0x18,0);
  FUN_100029d0(0x4cb,0x19,0);
  FUN_100029d0(0x4cb,0x1a,0);
  FUN_100029d0(0x4cb,0x1b,0);
  return;
}

