
void __thiscall FUN_00433d00(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  POINT pt;
  clock_t cVar2;
  BOOL BVar3;
  
  if ((*(int *)((int)this + 0x78) != 0) && (*(int *)((int)this + 0x1c) == 0)) {
    *(undefined4 *)((int)this + 0x24) = 0;
    iVar1 = param_2[1];
    *(int *)((int)this + 0x10) = *param_2 - *(int *)((int)this + 0x40);
    *(int *)((int)this + 0x14) = iVar1 - *(int *)((int)this + 0x44);
    if (*(int *)((int)this + 0x18) == 0) {
      cVar2 = clock();
      if (cVar2 - *(int *)((int)this + 0x84) < 0xfa) {
        if (*(int *)((int)this + 0x80) == 0) {
          *(clock_t *)((int)this + 0x84) = cVar2;
          (**(code **)(*(int *)this + 0x28))
                    (*(undefined4 *)((int)this + 0x10),*(undefined4 *)((int)this + 0x14));
          return;
        }
        (**(code **)(*(int *)this + 0x48))();
      }
      *(clock_t *)((int)this + 0x84) = cVar2;
      return;
    }
    iVar1 = *(int *)((int)this + 0x20);
    *(undefined4 *)(*(int *)(*(int *)((int)this + 0x54) + iVar1 * 4) + 0xc) = 1;
    *(undefined4 *)((int)this + 0x20) = 0;
    *(undefined4 *)((int)this + 0x18) = 0;
    pt.y = *(LONG *)((int)this + 0x14);
    pt.x = *(LONG *)((int)this + 0x10);
    BVar3 = PtInRect((RECT *)(*(int *)(*(int *)((int)this + 0x54) + iVar1 * 4) + 0x20),pt);
    if (BVar3 != 0) {
      (**(code **)(*(int *)this + 0x10))(iVar1);
    }
  }
  return;
}

