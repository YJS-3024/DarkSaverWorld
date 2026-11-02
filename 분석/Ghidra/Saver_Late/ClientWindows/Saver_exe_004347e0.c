
void __fastcall FUN_004347e0(int *param_1)

{
  char cVar1;
  CWinApp *this;
  AFX_MODULE_STATE *pAVar2;
  CString *pCVar3;
  char *_Str;
  char *pcVar4;
  BOOL BVar5;
  CString *this_00;
  uint uVar6;
  uint uVar7;
  int iVar8;
  char *pcVar9;
  int *piVar10;
  CString CStack_48;
  undefined1 uStack_47;
  char *local_44;
  char *local_40;
  int iStack_3c;
  CWinApp *local_38;
  int iStack_34;
  int iStack_30;
  RECT RStack_2c;
  RECT RStack_1c;
  void *pvStack_c;
  undefined1 *puStack_8;
  uint local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047d858;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  pAVar2 = AfxGetModuleState();
  this = *(CWinApp **)(pAVar2 + 4);
  local_38 = this;
  CString::CString((CString *)&local_40);
  local_4 = 0;
  CString::CString((CString *)&local_44);
  local_4._0_1_ = 1;
  if (param_1[0x25] == 0) {
    (**(code **)(*param_1 + 4))();
    CString::Format(this_00,(char *)&local_44);
    iVar8 = (**(code **)(*param_1 + 4))();
    if (iVar8 == 0x13) {
      pCVar3 = (CString *)
               CWinApp::GetProfileStringA(this,(char *)&local_44,"Dlg Information",local_40);
      local_4._0_1_ = 3;
      CString::operator=((CString *)&local_40,pCVar3);
      local_4 = CONCAT31(local_4._1_3_,1);
    }
    else {
      pCVar3 = (CString *)
               CWinApp::GetProfileStringA(this,(char *)&local_44,"Dlg Information",local_40);
      local_4._0_1_ = 4;
      CString::operator=((CString *)&local_40,pCVar3);
      local_4 = CONCAT31(local_4._1_3_,1);
    }
  }
  else {
    (**(code **)(*param_1 + 4))();
    CString::Format((CString *)&local_44,(char *)&local_44);
    pCVar3 = (CString *)
             CWinApp::GetProfileStringA(this,(char *)&local_44,"Dlg Information",local_40);
    local_4._0_1_ = 2;
    CString::operator=((CString *)&local_40,pCVar3);
    local_4 = CONCAT31(local_4._1_3_,1);
  }
  CString::~CString(&CStack_48);
  CStack_48 = (CString)0x20;
  uStack_47 = 0;
  _Str = operator_new(200);
  uVar6 = 0xffffffff;
  do {
    pcVar4 = local_40;
    if (uVar6 == 0) break;
    uVar6 = uVar6 - 1;
    pcVar4 = local_40 + 1;
    cVar1 = *local_40;
    local_40 = pcVar4;
  } while (cVar1 != '\0');
  uVar6 = ~uVar6;
  pcVar4 = pcVar4 + -uVar6;
  pcVar9 = _Str;
  for (uVar7 = uVar6 >> 2; uVar7 != 0; uVar7 = uVar7 - 1) {
    *(undefined4 *)pcVar9 = *(undefined4 *)pcVar4;
    pcVar4 = pcVar4 + 4;
    pcVar9 = pcVar9 + 4;
  }
  for (uVar6 = uVar6 & 3; uVar6 != 0; uVar6 = uVar6 - 1) {
    *pcVar9 = *pcVar4;
    pcVar4 = pcVar4 + 1;
    pcVar9 = pcVar9 + 1;
  }
  pcVar4 = strtok(_Str,(char *)&CStack_48);
  iVar8 = 0;
  do {
    if ((pcVar4 == (char *)0x0) || (2 < iVar8)) {
      operator_delete(_Str);
      if (iStack_3c != 0) {
        (**(code **)(*param_1 + 0x44))();
      }
      if ((param_1[0x25] == 0) && (iVar8 = (**(code **)(*param_1 + 4))(), iVar8 == 0x13)) {
        CString::operator=((CString *)&local_44,s_ChatWindowSize_0049008c);
        uVar6 = CWinApp::GetProfileIntA(local_38,"Dlg Information",local_44,1);
        if ((uVar6 == 2) && (iStack_3c == 0)) {
          thunk_FUN_004545a0(param_1);
        }
      }
      param_1[0x10] = iStack_34;
      param_1[0x11] = iStack_30;
      RStack_2c.left = param_1[0xc];
      RStack_2c.top = param_1[0xd];
      RStack_2c.right = param_1[0xe];
      RStack_2c.bottom = param_1[0xf];
      BVar5 = IsRectEmpty(&RStack_2c);
      if (BVar5 == 0) {
        OffsetRect(&RStack_2c,param_1[0x10],param_1[0x11]);
      }
      else {
        RStack_2c.left = param_1[0x10];
        RStack_2c.top = param_1[0x11];
        RStack_2c.right = param_1[0x12] + RStack_2c.left;
        RStack_2c.bottom = param_1[0x13] + RStack_2c.top;
      }
      RStack_1c.left = 0;
      RStack_1c.top = 0;
      RStack_1c.right = 0x280;
      RStack_1c.bottom = 0x1e0;
      BVar5 = IntersectRect(&RStack_2c,&RStack_2c,&RStack_1c);
      if (BVar5 == 0) {
        RStack_2c.left = param_1[0x10];
        RStack_2c.top = param_1[0x11];
        RStack_2c.right = param_1[0x12] + RStack_2c.left;
        RStack_2c.bottom = param_1[0x13] + RStack_2c.top;
        if (RStack_2c.left < 0) {
          param_1[0x10] = 0;
        }
        else {
          param_1[0x10] = (RStack_2c.left - RStack_2c.right) + 0x280;
        }
        if (param_1[0x11] < 0) {
          param_1[0x11] = 0;
        }
        else {
          param_1[0x11] = (RStack_2c.top - RStack_2c.bottom) + 0x1e0;
        }
      }
      (**(code **)(*param_1 + 0x50))();
      local_4 = local_4 & 0xffffff00;
      CString::~CString((CString *)&local_44);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_40);
      ExceptionList = pvStack_c;
      return;
    }
    if (iVar8 == 0) {
      piVar10 = &iStack_34;
LAB_00434993:
      sscanf(pcVar4,&DAT_0048f18c,piVar10);
    }
    else {
      if (iVar8 == 1) {
        piVar10 = &iStack_30;
        goto LAB_00434993;
      }
      if (iVar8 == 2) {
        piVar10 = &iStack_3c;
        goto LAB_00434993;
      }
    }
    pcVar4 = strtok((char *)0x0,(char *)&CStack_48);
    iVar8 = iVar8 + 1;
  } while( true );
}

