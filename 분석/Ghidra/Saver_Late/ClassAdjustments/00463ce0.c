
void __thiscall FUN_00463ce0(void *this,CDDSurface *param_1)

{
  int *piVar1;
  int iVar2;
  CDC *this_00;
  int iVar3;
  HBRUSH hbr;
  CPen *unaff_retaddr;
  HDC hdc;
  int left;
  int top;
  
  this_00 = CDDSurface::GetDC(param_1);
  CDC::SelectObject(this_00,(CPen *)((int)this + 0x148));
  (**(code **)(*(int *)this_00 + 0x2c))(5);
  iVar3 = *(int *)((int)this + 0xb8);
  if (iVar3 != -1) {
    if (*(int *)((int)this + 0xbc) == 0) {
      iVar3 = iVar3 * 0x10;
      left = *(int *)(iVar3 + 500 + (int)this);
      top = *(int *)((int)this + iVar3 + 0x1f8);
      iVar2 = *(int *)((int)this + iVar3 + 0x1fc);
      iVar3 = *(int *)((int)this + iVar3 + 0x200);
      OffsetRect((LPRECT)&stack0xffffffec,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
      hdc = *(HDC *)(this_00 + 4);
    }
    else {
      piVar1 = (int *)(iVar3 * 0x10 + 0x2f4 + (int)this);
      left = *piVar1;
      top = piVar1[1];
      iVar2 = piVar1[2];
      iVar3 = piVar1[3];
      OffsetRect((LPRECT)&stack0xffffffec,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
      hdc = *(HDC *)(this_00 + 4);
    }
    Rectangle(hdc,left,top,iVar2 + 1,iVar3 + 1);
  }
  if (*(int *)((int)this + 0xf4) != -1) {
    OffsetRect((LPRECT)&stack0xffffffec,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    hbr = (HBRUSH)0x0;
    if (this != (void *)0xfffffeb0) {
      hbr = *(HBRUSH *)((int)this + 0x154);
    }
    FillRect(*(HDC *)(this_00 + 4),(RECT *)&stack0xffffffec,hbr);
  }
  CDC::SelectObject(this_00,unaff_retaddr);
  CDDSurface::ReleaseDC(param_1,this_00);
  return;
}

