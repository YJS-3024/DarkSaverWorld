
void __thiscall FUN_0044aa70(void *this,CDDSurface *param_1)

{
  int *piVar1;
  int iVar2;
  int iVar3;
  int iVar4;
  CDC *this_00;
  CPen *pCVar5;
  int unaff_ESI;
  int iVar6;
  tagRECT local_10;
  
  iVar4 = *(int *)((int)this + 0x34c);
  if (iVar4 != -1) {
    if (*(int *)((int)this + 0x350) == 0) {
      iVar4 = iVar4 - *(int *)((int)this + 0xc0);
      if (iVar4 < 0) {
        return;
      }
      if (0xb < iVar4) {
        return;
      }
      iVar4 = iVar4 * 0x10;
      local_10.left = *(int *)(iVar4 + 0x284 + (int)this);
      local_10.top = *(int *)((int)this + iVar4 + 0x288);
      iVar2 = *(int *)((int)this + iVar4 + 0x28c);
      iVar3 = *(int *)((int)this + iVar4 + 0x290);
      iVar4 = *(int *)((int)this + 0x40);
      iVar6 = *(int *)((int)this + 0x44);
    }
    else {
      if (iVar4 < 0) {
        return;
      }
      if (0x1b < iVar4) {
        return;
      }
      iVar6 = *(int *)((int)this + 0xb0);
      if ((((*(int *)(iVar6 + 0x44) == 0) || (*(int *)(iVar6 + 0x48) == 0)) ||
          (*(int *)(iVar6 + 0x40) <= iVar4)) || (*(int *)(*(int *)(iVar6 + 0x44) + iVar4 * 4) == -1)
         ) {
        *(undefined4 *)((int)this + 0x34c) = 0xffffffff;
        iVar4 = *(int *)((int)this + 0x354);
        CStringList::RemoveAll((CStringList *)(iVar4 + 0x1c));
        *(undefined4 *)(iVar4 + 0x4c) = 0;
        return;
      }
      piVar1 = (int *)(iVar4 * 0x10 + 0xc4 + (int)this);
      local_10.left = *piVar1;
      local_10.top = piVar1[1];
      iVar2 = piVar1[2];
      iVar3 = piVar1[3];
      iVar4 = *(int *)((int)this + 0x40);
      iVar6 = *(int *)((int)this + 0x44);
    }
    local_10.bottom = iVar3 + 1;
    local_10.right = iVar2 + 1;
    OffsetRect(&local_10,iVar4,iVar6);
    this_00 = CDDSurface::GetDC(param_1);
    pCVar5 = CDC::SelectObject(this_00,(CPen *)((int)this + 0x344));
    (**(code **)(*(int *)this_00 + 0x2c))(5);
    Rectangle(*(HDC *)(this_00 + 4),unaff_ESI,local_10.left,local_10.top,local_10.right);
    CDC::SelectObject(this_00,pCVar5);
    CDDSurface::ReleaseDC(param_1,this_00);
  }
  return;
}

