
void __fastcall FUN_004626f0(void *param_1)

{
  int iVar1;
  int iVar2;
  
  iVar1 = *(int *)((int)param_1 + 0xb8);
  iVar2 = -1;
  if (iVar1 != -1) {
    if (*(int *)((int)param_1 + 0xbc) != 0) {
      thunk_FUN_00462740(param_1,(int)*(short *)(*(int *)((int)param_1 + 0x13c) + 0x70 + iVar1 * 2))
      ;
      return;
    }
    iVar2 = (int)*(short *)(*(int *)((int)param_1 + 0x13c) + 0x46 + iVar1 * 2);
  }
  thunk_FUN_00462740(param_1,iVar2);
  return;
}

