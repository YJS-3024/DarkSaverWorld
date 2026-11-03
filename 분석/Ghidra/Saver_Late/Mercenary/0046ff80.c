
void __thiscall FUN_0046ff80(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  BOOL BVar2;
  RECT *lprc;
  int iVar3;
  
  if (((*(int *)((int)this + 0x78) != 0) &&
      (thunk_FUN_00433be0(this,param_1,param_2), *(int *)((int)this + 0x80) == 0)) &&
     (*(short *)(*(int *)((int)this + 0x184) + 0x9a) == -1)) {
    iVar3 = 0;
    lprc = (RECT *)((int)this + 0xe4);
    do {
      BVar2 = PtInRect(lprc,*(POINT *)((int)this + 0x10));
      if ((BVar2 != 0) &&
         (iVar1 = *(int *)((int)this + 0x180) + iVar3, iVar1 < *(int *)((int)this + 0x174))) {
        *(int *)((int)this + 0x188) = iVar1;
      }
      iVar3 = iVar3 + 1;
      lprc = lprc + 1;
    } while (iVar3 < 9);
  }
  return;
}

