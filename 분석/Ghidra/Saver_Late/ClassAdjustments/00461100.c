
undefined4 * __thiscall
FUN_00461100(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  undefined4 uVar1;
  undefined4 *puVar2;
  CDDSurface *pCVar3;
  HBRUSH pHVar4;
  int iVar5;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047fecd;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,0,param_4,param_5);
  local_4 = 0;
  CString::CString((CString *)((int)this + 0x128));
  local_4._0_1_ = 1;
  CString::CString((CString *)((int)this + 300));
  local_4._0_1_ = 2;
  CString::CString((CString *)((int)this + 0x130));
  local_4._0_1_ = 3;
  CString::CString((CString *)((int)this + 0x134));
  local_4._0_1_ = 4;
  CString::CString((CString *)((int)this + 0x138));
  local_4._0_1_ = 5;
  CPen::CPen((CPen *)((int)this + 0x148),0,1,0x40ffff);
  local_4._0_1_ = 6;
  CBrush::CBrush((CBrush *)((int)this + 0x150),0xc83232);
  local_4._0_1_ = 7;
  CBrush::CBrush((CBrush *)((int)this + 0x158),0xffffff);
  local_4._0_1_ = 8;
  FUN_00479d72((int)this + 0x160,8,6,&LAB_00403788);
  *(undefined4 *)((int)this + 0x194) = 0;
  *(undefined ***)((int)this + 400) = &PTR_LAB_004841fc;
  *(undefined ***)this = &PTR_LAB_00485d24;
  local_4._0_1_ = 10;
  uVar1 = *(undefined4 *)(param_3 + 0x28);
  *(undefined4 *)((int)this + 0x198) = 1000;
  *(undefined4 *)((int)this + 0xac) = uVar1;
  *(int *)((int)this + 0x144) = param_3;
  puVar2 = operator_new(0x2dc);
  local_4._0_1_ = 0xb;
  if (puVar2 == (undefined4 *)0x0) {
    puVar2 = (undefined4 *)0x0;
  }
  else {
    puVar2 = thunk_FUN_00427ce0(puVar2);
  }
  local_4._0_1_ = 10;
  *(undefined4 **)((int)this + 0x140) = puVar2;
  *(undefined4 *)((int)this + 0x13c) = 0;
  thunk_FUN_00461a60((int)this);
  pCVar3 = operator_new(0x8c);
  local_4._0_1_ = 0xc;
  if (pCVar3 == (CDDSurface *)0x0) {
    pCVar3 = (CDDSurface *)0x0;
  }
  else {
    CDDSurface::CDDSurface(pCVar3);
    *(undefined ***)pCVar3 = &PTR_LAB_00483108;
  }
  local_4._0_1_ = 10;
  *(CDDSurface **)((int)this + 0xb4) = pCVar3;
  CDDSurface::Create(pCVar3,*(CDirectDraw **)((int)this + 4),0x40,0x40,0);
  *(undefined4 *)((int)this + 0xf8) = 0;
  *(undefined4 *)((int)this + 0xf4) = 0xffffffff;
  thunk_FUN_00462b90(this,0);
  *(undefined4 *)((int)this + 0xfc) = 0;
  *(undefined4 *)((int)this + 0xc4) = 0;
  *(undefined4 *)((int)this + 200) = 0;
  *(undefined4 *)((int)this + 0xd8) = 0;
  *(undefined4 *)((int)this + 0xcc) = 0;
  *(undefined4 *)((int)this + 0xdc) = 0;
  pCVar3 = operator_new(0x8c);
  local_4._0_1_ = 0xd;
  if (pCVar3 == (CDDSurface *)0x0) {
    pCVar3 = (CDDSurface *)0x0;
  }
  else {
    CDDSurface::CDDSurface(pCVar3);
    *(undefined ***)pCVar3 = &PTR_LAB_00483108;
  }
  local_4 = CONCAT31(local_4._1_3_,10);
  *(CDDSurface **)((int)this + 0xc0) = pCVar3;
  CDDSurface::CreateFromImage(pCVar3,*(CDirectDraw **)((int)this + 4),(char *)0x9a,0);
  CDDSurface::SetColorKey(*(CDDSurface **)((int)this + 0xc0),0);
  *(undefined4 *)((int)this + 0xb8) = 0xffffffff;
  *(undefined4 *)((int)this + 0xbc) = 0;
  puVar2 = (undefined4 *)((int)this + 0x19c);
  for (iVar5 = 6; iVar5 != 0; iVar5 = iVar5 + -1) {
    *puVar2 = 1000;
    puVar2 = puVar2 + 1;
  }
  thunk_FUN_00462b10((int)this);
  puVar2 = (undefined4 *)((int)this + 0x100);
  for (iVar5 = 10; iVar5 != 0; iVar5 = iVar5 + -1) {
    *puVar2 = 0xffffffff;
    puVar2 = puVar2 + 1;
  }
  CFont::CreatePointFont((CFont *)((int)this + 400),0x50,&DAT_0048ea80,(CDC *)0x0);
  pHVar4 = CreateSolidBrush(0xff0000);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x160),pHVar4);
  pHVar4 = CreateSolidBrush(0xff);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x168),pHVar4);
  pHVar4 = CreateSolidBrush(0x4080ff);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x170),pHVar4);
  pHVar4 = CreateSolidBrush(0xffff00);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x178),pHVar4);
  pHVar4 = CreateSolidBrush(0xff00);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x180),pHVar4);
  pHVar4 = CreateSolidBrush(0xffff);
  CGdiObject::Attach((CGdiObject *)((int)this + 0x188),pHVar4);
  ExceptionList = local_c;
  return this;
}

