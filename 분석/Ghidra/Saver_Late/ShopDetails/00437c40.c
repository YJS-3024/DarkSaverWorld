
void __thiscall FUN_00437c40(void *this,CDDSurface *param_1,int param_2,int param_3,uint param_4)

{
  tagRECT local_10;
  
  local_10.left = *(int *)((int)this + 0x38);
  local_10.top = *(int *)((int)this + 0x3c);
  local_10.right = *(int *)((int)this + 0x40);
  local_10.bottom = *(LONG *)((int)this + 0x44);
  OffsetRect(&local_10,param_2,param_3);
  thunk_FUN_004376e0(this,param_1,local_10.left,local_10.top,local_10.right,local_10.bottom,
                     *(int *)((int)this + 0x4c),param_4,*(int *)((int)this + 0x48));
  return;
}

