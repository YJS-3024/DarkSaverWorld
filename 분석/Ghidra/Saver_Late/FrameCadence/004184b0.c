
undefined4 __fastcall FUN_004184b0(int param_1)

{
  int iVar1;
  HWND hWnd;
  
  DAT_0048f198 = DAT_0048f198 + 1;
  if (DAT_0048f198 == 0x19) {
    DAT_0048f198 = 0;
  }
  iVar1 = thunk_FUN_004119b0(DAT_004952a0,DAT_0048f198);
  if ((iVar1 == 0) && (DAT_0048f198 = DAT_0048f198 + -1, *(int *)(DAT_004952a0 + 0x128) == 0)) {
    hWnd = (HWND)0x0;
    if (*(int *)(param_1 + 0x20) != 0) {
      hWnd = *(HWND *)(*(int *)(param_1 + 0x20) + 0x20);
    }
    SetFocus(hWnd);
    *(undefined4 *)(DAT_004952a0 + 0x128) = 1;
    CWnd::ShowWindow(DAT_004952a0,3);
    UpdateWindow(*(HWND *)(DAT_004952a0 + 0x20));
    PostMessageA(*(HWND *)(DAT_004952a0 + 0x20),0x466,1,0);
  }
  return 1;
}


/* incoming refs
00403553 UNCONDITIONAL_JUMP
*/
