
void __fastcall FUN_00434c20(int *param_1)

{
  CWinApp *this;
  AFX_MODULE_STATE *pAVar1;
  BOOL BVar2;
  int iVar3;
  CString *extraout_ECX;
  CString *this_00;
  CString local_30 [4];
  undefined1 local_2c [4];
  char *local_28;
  char *local_24;
  char *local_20;
  RECT local_1c;
  void *pvStack_c;
  undefined1 *puStack_8;
  void *local_4;
  
  local_4 = (void *)0xffffffff;
  puStack_8 = &LAB_0047d880;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  pAVar1 = AfxGetModuleState();
  this = *(CWinApp **)(pAVar1 + 4);
  CString::CString(local_30);
  local_4 = (void *)0x0;
  local_2c = (undefined1  [4])param_1[0xc];
  local_28 = (char *)param_1[0xd];
  local_24 = (char *)param_1[0xe];
  local_20 = (char *)param_1[0xf];
  BVar2 = IsRectEmpty((RECT *)local_2c);
  if (BVar2 == 0) {
    OffsetRect((LPRECT)local_2c,param_1[0x10],param_1[0x11]);
  }
  else {
    local_2c = (undefined1  [4])param_1[0x10];
    local_28 = (char *)param_1[0x11];
    local_24 = (char *)((int)local_2c + param_1[0x12]);
    local_20 = local_28 + param_1[0x13];
  }
  local_1c.left = 0;
  local_1c.top = 0;
  local_1c.right = 0x280;
  local_1c.bottom = 0x1e0;
  BVar2 = IntersectRect((LPRECT)local_2c,(RECT *)local_2c,&local_1c);
  if (BVar2 == 0) {
    local_2c = (undefined1  [4])param_1[0x10];
    local_28 = (char *)param_1[0x11];
    local_24 = (char *)((int)local_2c + param_1[0x12]);
    local_20 = local_28 + param_1[0x13];
    if ((int)local_2c < 0) {
      param_1[0x10] = 0;
    }
    else {
      param_1[0x10] = (int)((int)local_2c + (0x280 - (int)local_24));
    }
    if (param_1[0x11] < 0) {
      param_1[0x11] = 0;
    }
    else {
      param_1[0x11] = (int)(local_28 + (0x1e0 - (int)local_20));
    }
  }
  CString::Format((CString *)param_1[0x10],(char *)local_30);
  CString::CString(local_30);
  if (param_1[0x25] == 0) {
    (**(code **)(*param_1 + 4))();
    this_00 = extraout_ECX;
  }
  else {
    (**(code **)(*param_1 + 4))();
    this_00 = local_30;
  }
  CString::Format(this_00,(char *)local_30);
  CWinApp::WriteProfileStringA(this,"Dlg Information",(char *)local_2c,local_28);
  iVar3 = (**(code **)(*param_1 + 4))();
  if (iVar3 == 0x13) {
    CString::operator=((CString *)local_2c,s_ChatWindowSize_0049008c);
    iVar3 = param_1[0x2d];
    if (param_1[0x20] != 0) {
      iVar3 = 1;
    }
    CWinApp::WriteProfileInt(this,"Dlg Information",(char *)local_2c,iVar3);
  }
  CString::~CString((CString *)local_2c);
  CString::~CString((CString *)&local_28);
  ExceptionList = local_4;
  return;
}

