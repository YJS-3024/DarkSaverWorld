
int __thiscall FUN_00429310(void *this,int param_1)

{
  int iVar1;
  
  if (DAT_00495140 == (void *)0x0) {
    return 1;
  }
  if (param_1 != 0) {
    iVar1 = thunk_FUN_0041af80(DAT_00495140,*(undefined4 *)(*(int *)((int)this + 0xb0) + 0x10));
    return iVar1;
  }
  iVar1 = thunk_FUN_0041aee0(DAT_00495140,*(CDDSurface **)(*(int *)((int)this + 0xb0) + 0x10),0);
  return iVar1;
}


/* incoming refs
00403684 UNCONDITIONAL_JUMP
*/
