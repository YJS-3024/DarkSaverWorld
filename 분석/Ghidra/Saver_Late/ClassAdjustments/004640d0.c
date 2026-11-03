
void __thiscall FUN_004640d0(void *this,CDDSurface *param_1)

{
  CDC *this_00;
  undefined4 uVar1;
  int *piVar2;
  int iVar3;
  
  this_00 = CDDSurface::GetDC(param_1);
  iVar3 = (int)this + 400;
  uVar1 = (**(code **)(*(int *)this_00 + 0x30))();
  CDC::SetBkMode(this_00,1);
  piVar2 = (int *)0x0;
  do {
    thunk_FUN_00463f00(this,this_00,piVar2,iVar3);
    piVar2 = (int *)((int)piVar2 + 1);
  } while ((int)piVar2 < 6);
  (**(code **)(*(int *)this_00 + 0x30))(uVar1);
  CDDSurface::ReleaseDC(param_1,this_00);
  return;
}

