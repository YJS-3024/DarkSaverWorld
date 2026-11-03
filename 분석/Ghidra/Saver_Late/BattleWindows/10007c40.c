
/* int __cdecl DlgDraw(int,class CDDSurface const *,int,int,int,int) */

int __cdecl DlgDraw(int param_1,CDDSurface *param_2,int param_3,int param_4,int param_5,int param_6)

{
  int unaff_ESI;
  int unaff_EDI;
  LPRECT lprc;
  int iVar1;
  int iVar2;
  int iVar3;
  undefined4 uVar4;
  tagRECT local_10;
  
                    /* 0x7c40  22  ?DlgDraw@@YAHHPBVCDDSurface@@HHHH@Z */
  switch(param_1) {
  case 0:
    iVar3 = 0xee;
    iVar2 = 0;
    lprc = &local_10;
    iVar1 = 0x8a;
    SetRect(lprc,0x8a,0,0xee,100);
    uVar4 = 0;
    FUN_10007ef0(&stack0xffffffd4,&local_10);
    FUN_10005300(param_2,param_3 + 0xb,param_4 + 0x19,DAT_1001c484,(int)lprc,iVar1,iVar2,iVar3,uVar4
                );
    FUN_10006150(param_3,param_4);
    return 1;
  case 1:
    FUN_100054c0((void *)param_3,param_4);
    return 1;
  case 3:
    FUN_10006f70(param_3,param_4);
    return 1;
  case 5:
    (*DAT_1001c450)(DAT_1001c470);
    (*DAT_1001c450)(DAT_1001c470,param_3 + 10,param_4 + 0x33,DAT_1001bddc,0x80808,1);
    return 1;
  case 6:
    FUN_10006ba0(param_3,param_4);
    return 1;
  case 7:
    FUN_100070c0(param_3);
    return 1;
  case 8:
    FUN_10007350(param_3,param_4,unaff_EDI,unaff_ESI);
    return 1;
  case 9:
    FUN_10006830(param_3,param_4);
    return 1;
  case 0xc:
    if (DAT_1001a8f4 == 8) {
      (*DAT_1001c450)(DAT_1001c470);
      return 1;
    }
    break;
  case 0x13:
    FUN_10007a90(param_3,param_4,unaff_EDI);
    break;
  case 0x18:
  case 0x19:
  case 0x1a:
  case 0x1b:
    if (DAT_1006a738 == -99) {
      FUN_100029d0(0x4cb,0x18,0);
      FUN_100029d0(0x4cb,0x19,0);
      FUN_100029d0(0x4cb,0x1a,0);
      FUN_100029d0(0x4cb,0x1b,0);
      return 1;
    }
    FUN_100063e0(param_3,param_4);
    return 1;
  }
  return 1;
}

