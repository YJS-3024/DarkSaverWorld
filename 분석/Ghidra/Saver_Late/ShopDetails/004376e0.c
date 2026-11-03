
void __thiscall
FUN_004376e0(void *this,CDDSurface *param_1,int param_2,int param_3,int param_4,int param_5,
            int param_6,uint param_7,int param_8)

{
  int *this_00;
  byte *pbVar1;
  int iVar2;
  __POSITION *p_Var3;
  BOOL BVar4;
  int iVar5;
  _DDSURFACEDESC2 *p_Var6;
  int iVar7;
  undefined4 *puVar8;
  undefined4 auStack_148 [30];
  undefined4 uStack_d0;
  tagRECT local_a8;
  int local_98;
  int local_94;
  __POSITION *local_90;
  RECT local_8c;
  _DDSURFACEDESC2 local_7c [36];
  void *local_58;
  
  if (*(int *)((int)this + 4) != 0) {
    CRect::NormalizeRect((CRect *)&param_2);
    iVar7 = param_6;
    iVar5 = *(int *)((int)this + 0x28);
    local_94 = iVar5;
    p_Var3 = CStringList::FindIndex((CStringList *)((int)this + 0x1c),param_6);
    CDDSurface::GetDescription(param_1,local_7c);
    local_58 = CDDSurface::Lock(param_1);
    iVar2 = param_8;
    local_98 = iVar7;
    if (iVar7 < iVar5) {
      do {
        if (p_Var3 == (__POSITION *)0x0) break;
        uStack_d0 = 0x43779c;
        SetRect(&local_a8,param_2,
                (*(int *)((int)this + 0x10) + iVar2) * (local_98 - iVar7) + param_3,param_4,0);
        local_a8.bottom = *(int *)((int)this + 0x10) + local_a8.top + iVar2;
        CRect::NormalizeRect((CRect *)&local_a8);
        local_8c.left = local_a8.left;
        local_8c.top = local_a8.top;
        local_8c.right = local_a8.right;
        local_8c.bottom = local_a8.bottom;
        IntersectRect(&local_8c,&local_8c,(RECT *)&param_2);
        BVar4 = EqualRect(&local_8c,&local_a8);
        if (BVar4 == 0) break;
        local_90 = *(__POSITION **)p_Var3;
        this_00 = *(int **)((int)this + 4);
        pbVar1 = *(byte **)(p_Var3 + 8);
        if (*this_00 == 0) {
          p_Var6 = local_7c;
          puVar8 = auStack_148;
          for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
            *puVar8 = *(undefined4 *)p_Var6;
            p_Var6 = p_Var6 + 4;
            puVar8 = puVar8 + 1;
          }
          thunk_FUN_0043ea00(this_00,local_a8.left,local_a8.top,local_a8.right,local_a8.bottom,
                             pbVar1,param_7);
        }
        else {
          p_Var6 = local_7c;
          puVar8 = auStack_148;
          for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
            *puVar8 = *(undefined4 *)p_Var6;
            p_Var6 = p_Var6 + 4;
            puVar8 = puVar8 + 1;
          }
          thunk_FUN_0043e390(this_00,local_a8.left,local_a8.top,local_a8.right,local_a8.bottom,
                             pbVar1,param_7);
        }
        local_98 = local_98 + 1;
        p_Var3 = local_90;
        iVar7 = param_6;
      } while (local_98 < local_94);
    }
    CDDSurface::Unlock(param_1);
  }
  return;
}

