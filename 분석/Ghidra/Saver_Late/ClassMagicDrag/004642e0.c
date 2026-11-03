
undefined4 __thiscall FUN_004642e0(void *this,POINT *param_1)

{
  BOOL BVar1;
  RECT *lprc;
  int iVar2;
  
  iVar2 = 0;
  lprc = (RECT *)((int)this + 0x2f4);
  do {
    BVar1 = PtInRect(lprc,*param_1);
    if (BVar1 != 0) {
      return 1;
    }
    iVar2 = iVar2 + 1;
    lprc = lprc + 1;
  } while (iVar2 < 8);
  return 0;
}

