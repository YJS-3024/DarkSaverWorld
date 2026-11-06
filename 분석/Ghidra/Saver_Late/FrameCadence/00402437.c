
void __thiscall thunk_FUN_004156b0(void *this,byte *param_1)

{
  byte bVar1;
  BOOL BVar2;
  AFX_MODULE_STATE *pAVar3;
  byte *pbVar4;
  int iVar5;
  CString *this_00;
  char *pcVar6;
  bool bVar7;
  void *pvStack_10;
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &LAB_0047bde0;
  pvStack_c = ExceptionList;
  pvStack_10 = this;
  if (param_1 == (byte *)0x0) {
    ExceptionList = &pvStack_c;
    BVar2 = IsWindow(*(HWND *)((int)this + 0x158));
    if (BVar2 == 0) {
      PostMessageA(*(HWND *)((int)this + 0x20),0x466,1,0);
      ExceptionList = pvStack_c;
      return;
    }
  }
  else if (param_1 == (byte *)0x1) {
    ExceptionList = &pvStack_c;
    CString::CString((CString *)&param_1);
    uStack_4 = 0;
    pAVar3 = AfxGetModuleState();
    CString::operator=((CString *)&param_1,*(char **)(*(int *)(pAVar3 + 4) + 0x104));
    pcVar6 = s_KEYPRESS_0048f16c;
    pbVar4 = param_1;
    do {
      bVar1 = *pbVar4;
      bVar7 = bVar1 < (byte)*pcVar6;
      if (bVar1 != *pcVar6) {
LAB_00415770:
        iVar5 = (1 - (uint)bVar7) - (uint)(bVar7 != 0);
        goto LAB_00415775;
      }
      if (bVar1 == 0) break;
      bVar1 = pbVar4[1];
      bVar7 = bVar1 < (byte)pcVar6[1];
      if (bVar1 != pcVar6[1]) goto LAB_00415770;
      pbVar4 = pbVar4 + 2;
      pcVar6 = pcVar6 + 2;
    } while (bVar1 != 0);
    iVar5 = 0;
LAB_00415775:
    if (iVar5 == 0) {
      PostMessageA(*(HWND *)((int)this + 0x20),0x466,1,0);
    }
    uStack_4 = 0xffffffff;
    CString::~CString((CString *)&param_1);
  }
  else if (param_1 == (byte *)0x2) {
    if (*(int *)((int)this + 0xc4) == 3) {
      ExceptionList = &pvStack_c;
      iVar5 = clock();
    }
    else {
      ExceptionList = &pvStack_c;
      iVar5 = clock();
      if (iVar5 - DAT_004950e0 < 900) goto LAB_004158a2;
    }
    DAT_004950e0 = iVar5;
    if (*(int *)((int)this + 0x144) == 0) {
      if (*(void **)((int)this + 0x1c0) != (void *)0x0) {
        thunk_FUN_0041c7e0(*(void **)((int)this + 0x1c0),*(int *)((int)this + 0xc4),
                           *(undefined4 *)((int)this + 0x14c),(int)this);
      }
    }
    else {
      CString::CString((CString *)&pvStack_10);
      uStack_4 = 1;
      CString::Format(this_00,(char *)&pvStack_10);
      if (*(void **)((int)this + 0x1c0) != (void *)0x0) {
        thunk_FUN_0041c7e0(*(void **)((int)this + 0x1c0),*(int *)((int)this + 0xc4),
                           *(undefined4 *)((int)this + 0x14c),(int)this);
      }
      CString::~CString((CString *)&pvStack_c);
    }
    iVar5 = *(int *)((int)this + 0xc4) + -1;
    *(int *)((int)this + 0xc4) = iVar5;
    if ((iVar5 < 0) && (*(int *)((int)this + 0x14c) == 1)) {
      PostMessageA(*(HWND *)((int)this + 0x20),0x112,0xf060,0);
    }
  }
  else {
    ExceptionList = &pvStack_c;
    if (param_1 == (byte *)0x2748) {
      ExceptionList = &pvStack_c;
      thunk_FUN_00409d30();
    }
  }
LAB_004158a2:
  CWnd::Default(this);
  ExceptionList = puStack_8;
  return;
}


/* incoming refs
*/
