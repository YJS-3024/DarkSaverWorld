
void __thiscall FUN_0046aa20(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  undefined4 *puVar2;
  POINT pt;
  POINT pt_00;
  POINT pt_01;
  int iVar3;
  BOOL BVar4;
  CDDSurface *pCVar5;
  itemrecord *piVar6;
  RECT *pRVar7;
  int iVar8;
  char cVar9;
  short sVar10;
  int local_14;
  tagRECT local_10;
  
  iVar8 = 0;
  if (*(int *)((int)this + 0x78) != 0) {
    iVar1 = *(int *)((int)this + 0x31c);
    if (*(int *)((int)this + 0x2fc) != 0) {
      if ((*(int *)((int)this + 0x2f4) == 0) ||
         (iVar3 = thunk_FUN_0046b610(this,*(LONG *)((int)this + 0x10),*(LONG *)((int)this + 0x14)),
         iVar3 == 0)) {
        thunk_FUN_0046bc00(this,*(int *)((int)this + 0x10),*(int *)((int)this + 0x14),
                           **(undefined4 **)((int)this + 0x2f4),*(undefined4 *)((int)this + 0x2f8));
      }
      else {
        local_14 = 0;
        pRVar7 = (RECT *)((int)this + 0x284);
        do {
          pt.y._2_2_ = (short)((uint)*(undefined4 *)((int)this + 0x14) >> 0x10);
          pt._0_6_ = *(undefined6 *)((int)this + 0x10);
          BVar4 = PtInRect(pRVar7,pt);
          if (BVar4 != 0) {
            local_14 = iVar8;
            if (iVar8 != 0) {
              if ((iVar8 != 2) && (iVar8 != 3)) goto LAB_0046aad5;
              if (*(int *)(*(int *)((int)this + 0x2f4) + 0xc) == 1) {
                if (iVar8 == 2) {
                  sVar10 = *(short *)(iVar1 + 0xa4);
                  if (sVar10 != -1) {
                    iVar3 = 3;
LAB_0046ab49:
                    thunk_FUN_0046bae0(this,iVar3,0xffff,0,sVar10);
                  }
                }
                else if ((iVar8 == 3) && (sVar10 = *(short *)(iVar1 + 0xa2), sVar10 != -1)) {
                  iVar3 = 2;
                  goto LAB_0046ab49;
                }
                thunk_FUN_0046bae0(this,iVar8,(short)**(undefined4 **)((int)this + 0x2f4),
                                   (short)*(undefined4 *)((int)this + 0x2f8),
                                   *(undefined2 *)(iVar1 + 0x9e + iVar8 * 2));
                pCVar5 = GetItemSurface();
                if (pCVar5 != (CDDSurface *)0x0) {
                  iVar8 = *(int *)(*(int *)((int)this + 0x2f4) + 4);
                  iVar3 = (iVar8 / 100) * 0x20;
                  iVar8 = (iVar8 % 100) * 0x20;
                  SetRect(&local_10,iVar8,iVar3,iVar8 + 0x20,iVar3 + 0x20);
                  CDDSurface::BltFast(*(CDDSurface **)((int)this + 0x328),0,0,pCVar5,
                                      (short)&local_10,0);
                  SetRect(&local_10,0,0,0x20,0x20);
                  CDDSurface::DrawFogLayer
                            (*(CDDSurface **)((int)this + 0x328),(CRect *)&local_10,0x202020,1);
                }
              }
              else {
                iVar3 = -1;
                cVar9 = *(short *)(iVar1 + 0xa4) != -1;
                if ((bool)cVar9) {
                  iVar3 = (int)*(short *)(iVar1 + 0xa4);
                }
                if (*(short *)(iVar1 + 0xa2) != -1) {
                  cVar9 = cVar9 + '\x01';
                  iVar3 = (int)*(short *)(iVar1 + 0xa2);
                }
                if (((cVar9 == '\x01') &&
                    (piVar6 = GetItemTable(iVar3), piVar6 != (itemrecord *)0x0)) &&
                   (*(int *)(piVar6 + 0xc) == 1)) {
                  thunk_FUN_0046bae0(this,3,0xffff,0,*(undefined2 *)(iVar1 + 0xa4));
                  thunk_FUN_0046bae0(this,2,0xffff,0,*(undefined2 *)(iVar1 + 0xa2));
                }
                thunk_FUN_0046bae0(this,iVar8,(short)**(undefined4 **)((int)this + 0x2f4),
                                   (short)*(undefined4 *)((int)this + 0x2f8),
                                   *(undefined2 *)(iVar1 + 0x9e + iVar8 * 2));
              }
              goto LAB_0046acb4;
            }
            break;
          }
          iVar8 = iVar8 + 1;
          pRVar7 = pRVar7 + 1;
        } while (iVar8 < 7);
        iVar8 = local_14;
        if ((*(undefined4 **)((int)this + 0x2f4))[3] == 10) {
          thunk_FUN_0046bd30(this,(short)**(undefined4 **)((int)this + 0x2f4),
                             (short)*(undefined4 *)((int)this + 0x2f8));
        }
        else {
LAB_0046aad5:
          thunk_FUN_0046bae0(this,iVar8,(short)**(undefined4 **)((int)this + 0x2f4),
                             (short)*(undefined4 *)((int)this + 0x2f8),
                             *(undefined2 *)(iVar1 + 0x9e + iVar8 * 2));
        }
      }
    }
LAB_0046acb4:
    if (*(int *)((int)this + 0x30c) != 0) {
      if ((*(int *)((int)this + 0x2f4) == 0) ||
         (iVar8 = thunk_FUN_0046b610(this,*(LONG *)((int)this + 0x10),*(LONG *)((int)this + 0x14)),
         iVar8 == 0)) {
        local_10.left = *(LONG *)((int)this + 0xb4);
        local_10.top = *(LONG *)((int)this + 0xb8);
        local_10.right = *(LONG *)((int)this + 0xbc);
        local_10.bottom = *(LONG *)((int)this + 0xc0);
        pt_01.y._2_2_ = (short)((uint)*(undefined4 *)((int)this + 0x14) >> 0x10);
        pt_01._0_6_ = *(undefined6 *)((int)this + 0x10);
        BVar4 = PtInRect(&local_10,pt_01);
        if (BVar4 == 0) {
          thunk_FUN_0046bcc0(this,*(int *)((int)this + 0x310),*(int *)((int)this + 0x10),
                             *(int *)((int)this + 0x14),**(undefined4 **)((int)this + 0x2f4),
                             *(undefined4 *)((int)this + 0x2f8));
        }
        else {
          thunk_FUN_0046bae0(this,*(int *)((int)this + 0x310),0xffff,0,
                             (short)**(undefined4 **)((int)this + 0x2f4));
        }
      }
      else {
        iVar8 = 0;
        pRVar7 = (RECT *)((int)this + 0x284);
        do {
          pt_00.y._2_2_ = (short)((uint)*(undefined4 *)((int)this + 0x14) >> 0x10);
          pt_00._0_6_ = *(undefined6 *)((int)this + 0x10);
          BVar4 = PtInRect(pRVar7,pt_00);
          iVar3 = iVar8;
          if (BVar4 != 0) break;
          iVar8 = iVar8 + 1;
          pRVar7 = pRVar7 + 1;
          iVar3 = 0;
        } while (iVar8 < 7);
        if (iVar3 != *(int *)((int)this + 0x310)) {
          puVar2 = *(undefined4 **)((int)this + 0x2f4);
          if (puVar2[3] == 1) {
            thunk_FUN_0046bae0(this,*(int *)((int)this + 0x310),0xffff,0,(short)*puVar2);
            thunk_FUN_0046bae0(this,iVar3,(short)**(undefined4 **)((int)this + 0x2f4),
                               (short)*(undefined4 *)((int)this + 0x2f8),
                               *(undefined2 *)(iVar1 + 0x9e + iVar3 * 2));
          }
          else {
            thunk_FUN_0046bae0(this,iVar3,(short)*puVar2,(short)*(undefined4 *)((int)this + 0x2f8),
                               *(undefined2 *)(iVar1 + 0x9e + iVar3 * 2));
            thunk_FUN_0046bae0(this,*(int *)((int)this + 0x310),0xffff,0,
                               (short)**(undefined4 **)((int)this + 0x2f4));
          }
        }
      }
    }
    *(undefined4 *)((int)this + 0x2fc) = 0;
    *(undefined4 *)((int)this + 0x30c) = 0;
    thunk_FUN_0046bf20(this,0);
    thunk_FUN_00433d00(this,param_1,param_2);
  }
  return;
}

