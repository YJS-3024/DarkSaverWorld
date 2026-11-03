
void __fastcall FUN_004355f0(char *param_1)

{
  int iVar1;
  __POSITION *p_Var2;
  void *this;
  char *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047d958;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  local_10 = param_1;
  iVar1 = thunk_FUN_00435750(param_1);
  if ((iVar1 != 0) || (*(int *)(param_1 + 500) != 0)) {
    param_1[500] = '\0';
    param_1[0x1f5] = '\0';
    param_1[0x1f6] = '\0';
    param_1[0x1f7] = '\0';
    iVar1 = *(int *)(param_1 + 0x1c4) + 1;
    *(int *)(param_1 + 0x1c4) = iVar1;
    if (iVar1 < 0x24) {
      do {
        p_Var2 = CStringList::FindIndex((CStringList *)(param_1 + 0x1c8),iVar1);
        CString::CString((CString *)&local_10,(CString *)(p_Var2 + 8));
        local_4 = 0;
        if (*local_10 != '|') {
          this = (void *)thunk_FUN_00475f50(*(void **)(param_1 + 0x150),0x39);
          thunk_FUN_004728d0(this,(CString *)&local_10,1);
          local_4 = 0xffffffff;
          CString::~CString((CString *)&local_10);
          break;
        }
        local_4 = 0xffffffff;
        CString::~CString((CString *)&local_10);
        param_1[500] = '\0';
        param_1[0x1f5] = '\0';
        param_1[0x1f6] = '\0';
        param_1[0x1f7] = '\0';
        iVar1 = *(int *)(param_1 + 0x1c4) + 1;
        *(int *)(param_1 + 0x1c4) = iVar1;
      } while (iVar1 < 0x24);
    }
  }
  if (0x23 < *(int *)(param_1 + 0x1c4)) {
    *(undefined4 *)(DAT_004952a0 + 0x108) = 0;
    PostMessageA(*(HWND *)(DAT_004952a0 + 0x20),0x403,0,1);
  }
  ExceptionList = local_c;
  return;
}

