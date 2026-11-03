
undefined4 __thiscall FUN_0044a910(void *this,POINT *param_1)

{
  bool bVar1;
  BOOL BVar2;
  RECT *pRVar3;
  int iVar4;
  int iVar5;
  int local_8;
  
  bVar1 = false;
  local_8 = -1;
  iVar4 = 0;
  pRVar3 = (RECT *)((int)this + 0xc4);
  do {
    BVar2 = PtInRect(pRVar3,*param_1);
    if (BVar2 != 0) {
      bVar1 = true;
      local_8 = iVar4;
    }
    iVar5 = 1;
    iVar4 = iVar4 + 1;
    pRVar3 = pRVar3 + 1;
  } while (iVar4 < 0x1c);
  if (!bVar1) {
    iVar4 = 0;
    pRVar3 = (RECT *)((int)this + 0x284);
    do {
      BVar2 = PtInRect(pRVar3,*param_1);
      if (BVar2 != 0) {
        iVar5 = 0;
        bVar1 = true;
        local_8 = iVar4;
      }
      iVar4 = iVar4 + 1;
      pRVar3 = pRVar3 + 1;
    } while (iVar4 < 0xc);
    if (!bVar1) {
      return 0;
    }
  }
  iVar4 = thunk_FUN_0044aa20(this,iVar5,local_8);
  if (iVar4 == -1) {
    return 0;
  }
  *(int *)((int)this + 0x34c) = iVar4;
  *(int *)((int)this + 0x350) = iVar5;
  thunk_FUN_0044b1c0((int)this);
  return 1;
}

