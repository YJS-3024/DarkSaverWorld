
int __thiscall FUN_00442730(void *this,CDDSurface *param_1)

{
  int iVar1;
  clock_t cVar2;
  
  iVar1 = thunk_FUN_004429b0(this,8);
  if ((iVar1 != 0) && (cVar2 = clock(), 10000 < cVar2 - *(int *)((int)this + 0x2c))) {
    *(undefined4 *)((int)this + 0x2c) = 0;
    thunk_FUN_00441270(8,0);
  }
  if (*(int *)((int)this + 0x18) != 0) {
    if (DAT_00497ad0 < *(int *)((int)this + 0x18)) {
      iVar1 = thunk_FUN_00433830(*(void **)(*(int *)((int)this + 8) + DAT_00497ad0 * 4),param_1);
      if (iVar1 == 0) {
        return 0;
      }
      if (DAT_00497ad0 < *(int *)((int)this + 0x18) + -1) {
        DAT_00497ad0 = DAT_00497ad0 + 1;
        return 0;
      }
      DAT_00497ad0 = 0;
      return iVar1;
    }
    DAT_00497ad0 = 0;
  }
  return 1;
}


/* incoming refs
0040323d UNCONDITIONAL_JUMP
*/
