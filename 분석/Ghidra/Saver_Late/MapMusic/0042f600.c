
void FUN_0042f600(void)

{
  short sVar1;
  AFX_MODULE_STATE *pAVar2;
  char local_64 [100];
  
  if (DAT_004953b8 != (void *)0x0) {
    pAVar2 = AfxGetModuleState();
    sVar1 = *(short *)(*(int *)(pAVar2 + 4) + 0xd2);
    sprintf(local_64,s_RoomNumber____d_0048ff98,(int)sVar1);
    thunk_FUN_0041f310(DAT_004953b8,(int)sVar1);
  }
  return;
}

