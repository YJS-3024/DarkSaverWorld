
void __thiscall FUN_00433be0(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  BOOL BVar2;
  int iVar3;
  
  if ((*(int *)((int)this + 0x78) != 0) && (*(int *)((int)this + 0x1c) == 0)) {
    iVar1 = param_2[1];
    *(int *)((int)this + 0x10) = *param_2 - *(int *)((int)this + 0x40);
    *(int *)((int)this + 0x14) = iVar1 - *(int *)((int)this + 0x44);
    iVar1 = *(int *)((int)this + 0x58);
    iVar3 = 0;
    if (0 < iVar1) {
      do {
        BVar2 = PtInRect((RECT *)(*(int *)(*(int *)((int)this + 0x54) + iVar3 * 4) + 0x20),
                         *(POINT *)((int)this + 0x10));
        if ((BVar2 != 0) && (*(int *)(*(int *)(*(int *)((int)this + 0x54) + iVar3 * 4) + 0xc) != 4))
        {
          iVar1 = *(int *)(*(int *)((int)this + 0x54) + iVar3 * 4);
          if (*(int *)(iVar1 + 0x3c) == 0) {
            *(undefined4 *)((int)this + 0x18) = 1;
            *(int *)((int)this + 0x20) = iVar3;
            *(undefined4 *)(*(int *)(*(int *)((int)this + 0x54) + iVar3 * 4) + 0xc) = 3;
            return;
          }
          *(uint *)(iVar1 + 0xc) = 2 - (uint)(*(int *)(iVar1 + 0xc) != 1);
          (**(code **)(*(int *)this + 0xc))(iVar3);
          return;
        }
        iVar3 = iVar3 + 1;
      } while (iVar3 < iVar1);
    }
    BVar2 = PtInRect((RECT *)((int)this + 0x30),*(POINT *)((int)this + 0x10));
    if (BVar2 != 0) {
      *(undefined4 *)((int)this + 0x28) = *(undefined4 *)((int)this + 0x10);
      *(undefined4 *)((int)this + 0x2c) = *(undefined4 *)((int)this + 0x14);
      *(undefined4 *)((int)this + 0x24) = 1;
    }
  }
  return;
}

