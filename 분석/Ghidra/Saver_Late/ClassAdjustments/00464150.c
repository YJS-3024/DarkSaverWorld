
void __thiscall
FUN_00464150(void *this,int param_1,undefined4 *param_2,undefined4 *param_3,undefined4 *param_4,
            int *param_5)

{
  int *piVar1;
  int iVar2;
  int iVar3;
  int *nNumber;
  int *nNumber_00;
  int *piVar4;
  int *nDenominator;
  
  iVar3 = *(int *)((int)this + 0x13c);
  iVar2 = *(int *)((int)this + 0x140);
  nNumber = param_5;
  nNumber_00 = param_5;
  piVar4 = param_5;
  nDenominator = param_5;
  switch(param_1) {
  case 0:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x18);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x18);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x1a);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x1c);
    break;
  case 1:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x1e);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x1e);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x20);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x22);
    break;
  case 2:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x24);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x24);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x26);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x28);
    break;
  case 3:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x2a);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x2a);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x2c);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x2e);
    break;
  case 4:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x30);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x30);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x32);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x34);
    break;
  case 5:
    nNumber = (int *)(int)*(short *)(iVar2 + 0x36);
    nNumber_00 = (int *)(int)*(short *)(iVar3 + 0x36);
    piVar4 = (int *)(int)*(short *)(iVar3 + 0x38);
    nDenominator = (int *)(int)*(short *)(iVar3 + 0x3a);
  }
  piVar1 = (int *)(param_1 * 0x10 + 0x3dc + (int)this);
  iVar3 = MulDiv((int)nNumber,*(int *)(param_1 * 0x10 + 0x3e4 + (int)this) - *piVar1,
                 (int)nDenominator);
  *param_5 = iVar3;
  iVar3 = MulDiv((int)nNumber_00,piVar1[2] - *piVar1,(int)nDenominator);
  if (piVar1[2] - *piVar1 < iVar3) {
    iVar3 = piVar1[2] - *piVar1;
  }
  if (piVar1[2] - *piVar1 < *param_5) {
    *param_5 = piVar1[2] - *piVar1;
  }
  if (iVar3 < *param_5) {
    *param_5 = iVar3;
  }
  *param_2 = nNumber_00;
  *param_3 = piVar4;
  *param_4 = nDenominator;
  return;
}

