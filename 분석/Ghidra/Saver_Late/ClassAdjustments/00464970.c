
void __thiscall FUN_00464970(void *this,undefined4 param_1,int *param_2)

{
  BOOL BVar1;
  int iVar2;
  RECT *pRVar3;
  
  if (*(int *)((int)this + 0x78) != 0) {
    if (*(int *)((int)this + 0x80) == 0) {
      if ((*(int *)((int)this + 200) == 0) || (7 < *(short *)(*(int *)((int)this + 0x13c) + 0x6e)))
      {
        if ((*(int *)((int)this + 0xd8) != 0) &&
           (*(short *)(*(int *)((int)this + 0x13c) + 0x44) < 0x10)) {
          iVar2 = 0;
          pRVar3 = (RECT *)((int)this + 500);
LAB_004649f9:
          BVar1 = PtInRect(pRVar3,*(POINT *)((int)this + 0x10));
          if (BVar1 == 0) goto code_r0x00464a08;
          thunk_FUN_00462250(this);
LAB_00464a1a:
          *(undefined4 *)((int)this + 0xb8) = 0xffffffff;
        }
      }
      else {
        iVar2 = 0;
        pRVar3 = (RECT *)((int)this + 0x2f4);
        do {
          BVar1 = PtInRect(pRVar3,*(POINT *)((int)this + 0x10));
          if (BVar1 != 0) {
            thunk_FUN_00462190((int)this);
            goto LAB_00464a1a;
          }
          iVar2 = iVar2 + 1;
          pRVar3 = pRVar3 + 1;
        } while (iVar2 < 8);
      }
LAB_00464a24:
      *(undefined4 *)((int)this + 200) = 0;
      *(undefined4 *)((int)this + 0xd8) = 0;
      *(undefined4 *)((int)this + 0xc4) = 0;
    }
    thunk_FUN_00433d00(this,param_1,param_2);
  }
  return;
code_r0x00464a08:
  iVar2 = iVar2 + 1;
  pRVar3 = pRVar3 + 1;
  if (0xf < iVar2) goto LAB_00464a24;
  goto LAB_004649f9;
}

