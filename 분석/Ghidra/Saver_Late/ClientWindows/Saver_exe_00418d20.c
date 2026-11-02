
void __fastcall FUN_00418d20(CWinApp *param_1)

{
  DAT_00490ae0 = CWinApp::GetProfileIntA(param_1,"Environment","GlobalChat",1);
  DAT_00490ae8 = CWinApp::GetProfileIntA(param_1,"Environment","View Menu",1);
  DAT_00490aec = CWinApp::GetProfileIntA(param_1,"Environment","Full Screen",1);
  DAT_00497b9c = CWinApp::GetProfileIntA(param_1,"Environment","Font Number",0);
  *(undefined4 *)(param_1 + 200) = 1;
  return;
}

