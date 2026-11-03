
void __thiscall FUN_004646a0(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  BOOL BVar2;
  RECT *pRVar3;
  int iVar4;
  
  iVar4 = 0;
  if (*(int *)((int)this + 0x78) != 0) {
    thunk_FUN_00433be0(this,param_1,param_2);
    *(undefined4 *)((int)this + 0xc4) = 0;
    *(undefined4 *)((int)this + 200) = 0;
    *(undefined4 *)((int)this + 0xd8) = 0;
    if (*(int *)((int)this + 0x80) == 0) {
      iVar1 = *(int *)((int)this + 0x13c);
      pRVar3 = (RECT *)((int)this + 500);
      do {
        if (*(short *)(iVar1 + 0x44) <= iVar4) break;
        BVar2 = PtInRect(pRVar3,*(POINT *)((int)this + 0x10));
        if (BVar2 != 0) {
          *(int *)((int)this + 0xb8) = iVar4;
          *(int *)((int)this + 0xcc) = iVar4;
          *(undefined4 *)((int)this + 0xbc) = 0;
          *(undefined4 *)((int)this + 200) = 1;
          *(undefined4 *)((int)this + 0xd0) = *(undefined4 *)(iVar4 * 0x10 + 500 + (int)this);
          *(undefined4 *)((int)this + 0xd4) = *(undefined4 *)(iVar4 * 0x10 + 0x1f8 + (int)this);
          *(int *)((int)this + 0xd0) = *(int *)((int)this + 0xd0) + 0x10;
          *(int *)((int)this + 0xd4) = *(int *)((int)this + 0xd4) + 0x10;
          return;
        }
        iVar4 = iVar4 + 1;
        pRVar3 = pRVar3 + 1;
      } while (iVar4 < 0x10);
      iVar4 = 0;
      pRVar3 = (RECT *)((int)this + 0x2f4);
      do {
        if (*(short *)(iVar1 + 0x6e) <= iVar4) break;
        BVar2 = PtInRect(pRVar3,*(POINT *)((int)this + 0x10));
        if (BVar2 != 0) {
          *(int *)((int)this + 0xb8) = iVar4;
          *(int *)((int)this + 0xdc) = iVar4;
          *(undefined4 *)((int)this + 0xbc) = 1;
          *(undefined4 *)((int)this + 0xd8) = 1;
          *(undefined4 *)((int)this + 0xe0) = *(undefined4 *)(iVar4 * 0x10 + 0x2f4 + (int)this);
          *(undefined4 *)((int)this + 0xe4) = *(undefined4 *)(iVar4 * 0x10 + 0x2f8 + (int)this);
          *(int *)((int)this + 0xe0) = *(int *)((int)this + 0xe0) + 0x10;
          *(int *)((int)this + 0xe4) = *(int *)((int)this + 0xe4) + 0x10;
          return;
        }
        iVar4 = iVar4 + 1;
        pRVar3 = pRVar3 + 1;
      } while (iVar4 < 8);
      if ((*(short *)(iVar1 + 0x3e) != 0) && (*(int *)((int)this + 0xf8) != 0)) {
        BVar2 = PtInRect((RECT *)((int)this + 0x38c),*(POINT *)((int)this + 0x10));
        if (BVar2 == 0) {
          BVar2 = PtInRect((RECT *)((int)this + 0x3ac),*(POINT *)((int)this + 0x10));
          if (BVar2 == 0) {
            BVar2 = PtInRect((RECT *)((int)this + 0x3bc),*(POINT *)((int)this + 0x10));
            if ((BVar2 != 0) && (0 < *(int *)((int)this + 0xf0))) {
              *(undefined4 *)((int)this + 0xf4) = 2;
              thunk_FUN_00462630((int)this);
            }
          }
          else if (*(int *)((int)this + 0xe8) != -1) {
            *(undefined4 *)((int)this + 0xf4) = 1;
            thunk_FUN_00462630((int)this);
            return;
          }
        }
        else if (*(int *)((int)this + 0xec) != -1) {
          *(undefined4 *)((int)this + 0xf4) = 0;
          thunk_FUN_00462630((int)this);
          return;
        }
      }
    }
  }
  return;
}

