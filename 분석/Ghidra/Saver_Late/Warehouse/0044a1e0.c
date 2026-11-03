
void __thiscall FUN_0044a1e0(void *this,undefined4 param_1,int *param_2)

{
  BOOL BVar1;
  tagRECT local_20;
  tagRECT local_10;
  
  if (*(int *)((int)this + 0x78) != 0) {
    SetRect(&local_10,*(int *)((int)this + 0x284),*(int *)((int)this + 0x288),
            *(int *)((int)this + 0x33c),*(int *)((int)this + 0x340));
    SetRect(&local_20,*(int *)((int)this + 0xc4),*(int *)((int)this + 200),
            *(int *)((int)this + 0x27c),*(int *)((int)this + 0x280));
    if ((*(int *)((int)this + 0x35c) == 0) ||
       (BVar1 = PtInRect(&local_20,*(POINT *)((int)this + 0x10)), BVar1 == 0)) {
      if ((*(int *)((int)this + 0x364) != 0) &&
         (BVar1 = PtInRect(&local_10,*(POINT *)((int)this + 0x10)), BVar1 != 0)) {
        (**(code **)(*(int *)this + 0x5c))();
      }
    }
    else {
      (**(code **)(*(int *)this + 0x58))();
    }
    *(undefined4 *)((int)this + 0x35c) = 0;
    *(undefined4 *)((int)this + 0x360) = 0xffffffff;
    *(undefined4 *)((int)this + 0x364) = 0;
    *(undefined4 *)((int)this + 0x368) = 0xffffffff;
    thunk_FUN_00433d00(this,param_1,param_2);
    return;
  }
  return;
}

