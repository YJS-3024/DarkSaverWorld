
void __fastcall FUN_00461a60(int param_1)

{
  uint uVar1;
  int iVar2;
  uint uVar3;
  LPRECT ptVar4;
  int iVar5;
  
  SetRect((LPRECT)(param_1 + 0x1b4),0x14,0x2f,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x1b4),0x40,0x40);
  SetRect((LPRECT)(param_1 + 0x1c4),0x7f,0x4f,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x1c4),0x40,0x40);
  *(undefined4 *)(param_1 + 0x1d4) = 0x32;
  *(undefined4 *)(param_1 + 0x1dc) = 0x32;
  *(undefined4 *)(param_1 + 0x1d8) = 0x80;
  *(undefined4 *)(param_1 + 0x1e0) = 0x80;
  *(undefined4 *)(param_1 + 0x1e0) = 0x91;
  *(undefined4 *)(param_1 + 0x1e4) = *(undefined4 *)(param_1 + 0x1dc);
  *(int *)(param_1 + 0x1e8) = *(int *)(param_1 + 0x1e0);
  *(int *)(param_1 + 0x1e8) = *(int *)(param_1 + 0x1e0) + 0x11;
  *(undefined4 *)(param_1 + 0x1ec) = *(undefined4 *)(param_1 + 0x1e4);
  *(int *)(param_1 + 0x1f0) = *(int *)(param_1 + 0x1e8);
  uVar3 = 0;
  *(int *)(param_1 + 0x1f0) = *(int *)(param_1 + 0x1e8) + 0x11;
  ptVar4 = (LPRECT)(param_1 + 500);
  do {
    uVar1 = uVar3 & 0x80000007;
    if ((int)uVar1 < 0) {
      uVar1 = (uVar1 - 1 | 0xfffffff8) + 1;
    }
    SetRect(ptVar4,uVar1 * 0x21 + 0xd0,((int)(uVar3 + ((int)uVar3 >> 0x1f & 7U)) >> 3) * 0x21 + 0x2f
            ,0,0);
    thunk_FUN_0046bab0(&ptVar4->left,0x20,0x20);
    uVar3 = uVar3 + 1;
    ptVar4 = ptVar4 + 1;
  } while ((int)uVar3 < 0x12);
  uVar3 = 0;
  ptVar4 = (LPRECT)(param_1 + 0x2f4);
  do {
    uVar1 = uVar3 & 0x80000007;
    if ((int)uVar1 < 0) {
      uVar1 = (uVar1 - 1 | 0xfffffff8) + 1;
    }
    SetRect(ptVar4,uVar1 * 0x21 + 0xd0,0x79,0,0);
    thunk_FUN_0046bab0(&ptVar4->left,0x20,0x20);
    uVar3 = uVar3 + 1;
    ptVar4 = ptVar4 + 1;
  } while ((int)uVar3 < 8);
  *(undefined4 *)(param_1 + 0x374) = 500;
  *(undefined4 *)(param_1 + 0x378) = 0x5c;
  SetRect((LPRECT)(param_1 + 0x37c),0xb1,0xa0,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x37c),0x159,0x25);
  SetRect((LPRECT)(param_1 + 0x38c),0x20,0xe4,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x38c),0x20,0x20);
  SetRect((LPRECT)(param_1 + 0x39c),99,0xe4,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x39c),0x20,0x20);
  SetRect((LPRECT)(param_1 + 0x3ac),0xa7,0xe4,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x3ac),0x20,0x20);
  SetRect((LPRECT)(param_1 + 0x3bc),100,0x128,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x3bc),0x20,0x20);
  SetRect((LPRECT)(param_1 + 0x3cc),0x1e,0x15d,0,0);
  thunk_FUN_0046bab0((int *)(param_1 + 0x3cc),0xab,0x27);
  iVar5 = 0;
  do {
    if (iVar5 == 0) {
      SetRect((LPRECT)(param_1 + 0x3dc),0x11d,0xe2,0,0);
      thunk_FUN_0046bab0((int *)(param_1 + 0x3dc),0xb0,10);
    }
    else {
      iVar2 = iVar5 * 0x10 + param_1;
      ((LPRECT)(iVar2 + 0x3dc))->left = *(LONG *)(iVar2 + 0x3cc);
      *(undefined4 *)(iVar2 + 0x3e0) = *(undefined4 *)(iVar2 + 0x3d0);
      *(undefined4 *)(iVar2 + 0x3e4) = *(undefined4 *)(iVar2 + 0x3d4);
      *(undefined4 *)(iVar2 + 1000) = *(undefined4 *)(iVar2 + 0x3d8);
      OffsetRect((LPRECT)(iVar2 + 0x3dc),0,0x16);
    }
    iVar5 = iVar5 + 1;
  } while (iVar5 < 6);
  *(undefined4 *)(param_1 + 0x43c) = 0x1e4;
  *(undefined4 *)(param_1 + 0x440) = 0x16d;
  return;
}

