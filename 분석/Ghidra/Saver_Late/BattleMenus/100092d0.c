
void __thiscall
FUN_100092d0(void *this,undefined4 param_1,int param_2,int param_3,undefined4 *param_4,int param_5,
            undefined4 *param_6)

{
  int iVar1;
  undefined4 *puVar2;
  int iVar3;
  undefined4 *puVar4;
  undefined4 *puVar5;
  
  iVar1 = param_2;
  *(int *)((int)this + 0x10) = param_2;
  *(undefined4 *)((int)this + 0x3c) = DAT_1001c47c;
  *(undefined4 *)((int)this + 0xc) = param_1;
  param_2 = 0;
  if (iVar1 < 1) {
    *(undefined4 *)((int)this + 4) = 0;
    *(undefined4 *)this = 1;
    return;
  }
  puVar4 = (undefined4 *)((int)this + 0x1a8);
  puVar5 = (undefined4 *)((int)this + 0xe0);
  iVar1 = param_3 - (int)param_4;
  iVar3 = param_5 - (int)param_6;
  do {
    puVar2 = (undefined4 *)(iVar1 + (int)param_4);
    puVar5[-0x28] = *puVar2;
    puVar5[-0x27] = puVar2[1];
    puVar5[-0x26] = puVar2[2];
    puVar5[-0x25] = puVar2[3];
    *puVar5 = *param_4;
    puVar5[1] = param_4[1];
    puVar5[2] = param_4[2];
    puVar5[3] = param_4[3];
    CString::operator=((CString *)(puVar4 + -10),(CString *)(iVar3 + (int)param_6));
    *puVar4 = *param_6;
    param_2 = param_2 + 1;
    param_4 = param_4 + 4;
    puVar5 = puVar5 + 4;
    param_6 = param_6 + 1;
    puVar4 = puVar4 + 1;
  } while (param_2 < *(int *)((int)this + 0x10));
  *(undefined4 *)((int)this + 4) = 0;
  *(undefined4 *)this = 1;
  return;
}

