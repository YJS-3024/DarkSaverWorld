
void __fastcall FUN_00418db0(CWinApp *param_1)

{
  if (*(int *)(param_1 + 200) != 0) {
    CWinApp::WriteProfileInt(param_1,"Environment","GlobalChat",DAT_00490ae0);
    CWinApp::WriteProfileInt(param_1,"Environment","View Menu",DAT_00490ae8);
    CWinApp::WriteProfileInt(param_1,"Environment","Full Screen",DAT_00490aec);
    CWinApp::WriteProfileInt(param_1,"Environment","Font Number",DAT_00497b9c);
  }
  return;
}

