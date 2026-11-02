
void __thiscall FUN_0046a7d0(void *this,undefined4 param_1,int *param_2)

{
  BOOL BVar1;
  int iVar2;
  itemrecord *piVar3;
  short *psVar4;
  RECT *pRVar5;
  int iVar6;
  int iVar7;
  undefined4 uVar8;
  
  if ((*(int *)((int)this + 0x78) != 0) &&
     (thunk_FUN_00433be0(this,param_1,param_2), *(int *)((int)this + 0x80) == 0)) {
    iVar7 = 0;
    iVar6 = *(int *)(*(int *)((int)this + 0xb0) + 0x40);
    if (0 < iVar6) {
      pRVar5 = (RECT *)((int)this + 0xc4);
      do {
        if (0x1b < iVar7) break;
        BVar1 = PtInRect(pRVar5,*(POINT *)((int)this + 0x10));
        if (BVar1 != 0) {
          *(undefined4 *)((int)this + 0x2fc) = 1;
          *(int *)((int)this + 0x300) = iVar7;
          *(undefined4 *)((int)this + 0x304) = *(undefined4 *)(iVar7 * 0x10 + 0xc4 + (int)this);
          *(undefined4 *)((int)this + 0x308) = *(undefined4 *)(iVar7 * 0x10 + 200 + (int)this);
          *(int *)((int)this + 0x304) = *(int *)((int)this + 0x304) + 0x10;
          *(int *)((int)this + 0x308) = *(int *)((int)this + 0x308) + 0x10;
          iVar6 = *(int *)((int)this + 0xb0);
          if (((*(int *)(iVar6 + 0x44) == 0) || (*(int *)(iVar6 + 0x48) == 0)) ||
             (*(int *)(iVar6 + 0x40) <= iVar7)) {
            iVar2 = -1;
            uVar8 = 0;
          }
          else {
            iVar2 = *(int *)(*(int *)(iVar6 + 0x44) + iVar7 * 4);
            uVar8 = *(undefined4 *)(*(int *)(iVar6 + 0x48) + iVar7 * 4);
          }
          piVar3 = GetItemTable(iVar2);
          *(itemrecord **)((int)this + 0x2f4) = piVar3;
          *(undefined4 *)((int)this + 0x2f8) = uVar8;
          thunk_FUN_0046bf20(this,(int)piVar3);
          break;
        }
        iVar7 = iVar7 + 1;
        pRVar5 = pRVar5 + 1;
      } while (iVar7 < iVar6);
    }
    if (*(int *)((int)this + 0x2fc) == 0) {
      pRVar5 = (RECT *)((int)this + 0x284);
      iVar6 = 0;
      psVar4 = (short *)(*(int *)((int)this + 0x31c) + 0x9e);
      do {
        if ((*psVar4 != -1) && (BVar1 = PtInRect(pRVar5,*(POINT *)((int)this + 0x10)), BVar1 != 0))
        {
          *(undefined4 *)((int)this + 0x30c) = 1;
          *(int *)((int)this + 0x310) = iVar6;
          *(LONG *)((int)this + 0x314) = pRVar5->left;
          *(LONG *)((int)this + 0x318) = pRVar5->top;
          *(int *)((int)this + 0x314) = *(int *)((int)this + 0x314) + 0x10;
          *(int *)((int)this + 0x318) = *(int *)((int)this + 0x318) + 0x10;
          piVar3 = GetItemTable((int)*psVar4);
          *(itemrecord **)((int)this + 0x2f4) = piVar3;
          *(int *)((int)this + 0x2f8) = (int)psVar4[7];
          thunk_FUN_0046bf20(this,(int)piVar3);
        }
        iVar6 = iVar6 + 1;
        psVar4 = psVar4 + 1;
        pRVar5 = pRVar5 + 1;
      } while (iVar6 < 7);
    }
  }
  return;
}

