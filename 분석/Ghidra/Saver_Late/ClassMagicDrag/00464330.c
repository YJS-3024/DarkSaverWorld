
undefined4 __thiscall FUN_00464330(void *this,POINT *param_1)

{
  BOOL BVar1;
  RECT *lprc;
  int iVar2;
  
  iVar2 = 0;
  lprc = (RECT *)((int)this + 500);
  do {
    BVar1 = PtInRect(lprc,*param_1);
    if (BVar1 != 0) {
      return 1;
    }
    iVar2 = iVar2 + 1;
    lprc = lprc + 1;
  } while (iVar2 < 0x10);
  return 0;
}

