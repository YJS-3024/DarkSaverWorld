
/* public: class CDC * __thiscall CDDSurface::GetDC(void) */

CDC * __thiscall CDDSurface::GetDC(CDDSurface *this)

{
  int iVar1;
  CDC *pCVar2;
  HDC__ local_14 [2];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 uStack_4;
  
                    /* 0x63e0  97  ?GetDC@CDDSurface@@QAEPAVCDC@@XZ */
  uStack_4 = 0xffffffff;
  puStack_8 = &LAB_1000c83a;
  pvStack_c = ExceptionList;
  local_14[0].unused = 0;
  ExceptionList = &pvStack_c;
  iVar1 = (**(code **)(**(int **)(this + 0x84) + 0x44))(*(int **)(this + 0x84));
  *(int *)(this + 4) = iVar1;
  if (iVar1 != 0) {
    ExceptionList = (void *)local_14[0].unused;
    return (CDC *)0x0;
  }
  pCVar2 = operator_new(0x10);
  pvStack_c = (void *)0x0;
  if (pCVar2 == (CDC *)0x0) {
    pCVar2 = (CDC *)0x0;
  }
  else {
    pCVar2 = (CDC *)CDC::CDC(pCVar2);
  }
  pvStack_c = (void *)0xffffffff;
  CDC::Attach(pCVar2,local_14);
  ExceptionList = (void *)local_14[0].unused;
  return pCVar2;
}

