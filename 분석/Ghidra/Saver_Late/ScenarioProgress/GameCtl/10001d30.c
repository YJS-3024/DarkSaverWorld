
/* public: void __thiscall CAlphaEffectorForChar::DrawAlphaEffect(class CDDSurface *,class CPoint)
    */

void __thiscall
CAlphaEffectorForChar::DrawAlphaEffect
          (CAlphaEffectorForChar *this,undefined4 param_1,undefined4 param_3,undefined4 param_4)

{
  clock_t cVar1;
  int iVar2;
  
                    /* 0x1d30  26
                       ?DrawAlphaEffect@CAlphaEffectorForChar@@QAEXPAVCDDSurface@@VCPoint@@@Z */
  cVar1 = clock();
  if (*(int *)(this + 0x18) < cVar1 - *(int *)(this + 0x14)) {
    *(int *)(this + 0x1c) = (*(int *)(this + 0x1c) + 1) % *(int *)(this + 4);
    if (*(int *)(this + 0x28) == 0) {
      iVar2 = (*(int *)(this + 0x20) + 1) % *(int *)(this + 8);
    }
    else {
      iVar2 = (*(int *)(this + 0x20) + 1) % *(int *)(this + 0x28);
    }
    *(int *)(this + 0x20) = iVar2;
    cVar1 = clock();
    *(clock_t *)(this + 0x14) = cVar1;
  }
  if (*(int *)(this + 0x28) != 0) {
    CAlphaProcess::AlphaBlit
              (param_1,param_3,param_4,
               *(undefined4 *)(*(int *)(this + 0x24) + *(int *)(this + 0x20) * 4));
    return;
  }
  CAlphaProcess::AlphaBlit
            (param_1,param_3,param_4,
             *(undefined4 *)(*(int *)(this + 0xc) + *(int *)(this + 0x20) * 4),
             *(undefined4 *)(*(int *)(this + 0x10) + *(int *)(this + 0x1c) * 4));
  return;
}

