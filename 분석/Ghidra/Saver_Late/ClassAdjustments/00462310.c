
void __thiscall FUN_00462310(void *this,int param_1)

{
  int iVar1;
  int iVar2;
  undefined2 *puVar3;
  
  iVar1 = *(int *)((int)this + 0x13c);
  if (*(short *)(iVar1 + 0x6e) != 0) {
    iVar2 = *(short *)(iVar1 + 0x6e) + -1;
    if ((param_1 != iVar2) && (param_1 < iVar2)) {
      puVar3 = (undefined2 *)(iVar1 + 0x70 + param_1 * 2);
      do {
        param_1 = param_1 + 1;
        *puVar3 = puVar3[1];
        puVar3 = puVar3 + 1;
      } while (param_1 < *(short *)(iVar1 + 0x6e) + -1);
    }
    *(short *)(iVar1 + 0x6e) = *(short *)(iVar1 + 0x6e) + -1;
    if (*(short *)(iVar1 + 0x6e) == 0) {
      *(undefined4 *)((int)this + 0xb8) = 0xffffffff;
      return;
    }
    iVar1 = *(short *)(iVar1 + 0x6e) + -1;
    if (iVar1 < *(int *)((int)this + 0xb8)) {
      *(int *)((int)this + 0xb8) = iVar1;
    }
  }
  return;
}

