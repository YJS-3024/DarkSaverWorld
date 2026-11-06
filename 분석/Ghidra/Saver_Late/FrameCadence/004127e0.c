
void __thiscall FUN_004127e0(void *this,undefined4 param_1)

{
  if ((*(int *)((int)this + 0x144) != 0) && (*(int **)((int)this + 0x148) != (int *)0x0)) {
    (**(code **)(**(int **)((int)this + 0x148) + 0x28))(param_1);
    return;
  }
  if (*(int **)((int)this + 0xf0) != (int *)0x0) {
    (**(code **)(**(int **)((int)this + 0xf0) + 0x28))(param_1);
  }
  return;
}


/* incoming refs
00402ca7 UNCONDITIONAL_JUMP
*/
