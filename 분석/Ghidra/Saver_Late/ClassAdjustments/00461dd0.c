
void __thiscall FUN_00461dd0(void *this,int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  int iVar3;
  short sVar4;
  char *pcVar5;
  short sVar6;
  
  iVar3 = param_2;
  iVar1 = *(int *)((int)this + 0x140);
  iVar2 = *(int *)((int)this + 0x13c);
  if (param_2 == 2) {
    switch(param_1) {
    case 0:
      sVar6 = *(short *)(iVar2 + 0x1a);
      sVar4 = *(short *)(iVar1 + 0x1a);
      break;
    case 1:
      sVar6 = *(short *)(iVar2 + 0x20);
      sVar4 = *(short *)(iVar1 + 0x20);
      break;
    case 2:
      sVar6 = *(short *)(iVar2 + 0x26);
      sVar4 = *(short *)(iVar1 + 0x26);
      break;
    case 3:
      sVar6 = *(short *)(iVar2 + 0x2c);
      sVar4 = *(short *)(iVar1 + 0x2c);
      break;
    case 4:
      sVar6 = *(short *)(iVar2 + 0x32);
      sVar4 = *(short *)(iVar1 + 0x32);
      break;
    case 5:
      sVar6 = *(short *)(iVar2 + 0x38);
      sVar4 = *(short *)(iVar1 + 0x38);
      break;
    default:
      return;
    }
    if (sVar6 <= sVar4) {
      return;
    }
  }
  else if ((int)*(short *)(iVar2 + 0x3c) < *(int *)((int)this + param_1 * 4 + 0x19c)) {
    return;
  }
  pcVar5 = operator_new(0x14);
  param_2 = 0;
  thunk_FUN_0043b9a0((int)pcVar5,&param_2,0x82);
  thunk_FUN_0043b970((int)pcVar5,&param_2,*(undefined2 *)(iVar2 + 4));
  thunk_FUN_0043b970((int)pcVar5,&param_2,(short)param_1);
  thunk_FUN_0043b970((int)pcVar5,&param_2,(short)iVar3);
  if (iVar3 == 1) {
    *(short *)(iVar2 + 0x3c) =
         *(short *)(iVar2 + 0x3c) - *(short *)((int)this + param_1 * 4 + 0x19c);
  }
  else {
    *(short *)(iVar2 + 0x3c) =
         *(short *)(iVar2 + 0x3c) + *(short *)((int)this + param_1 * 4 + 0x19c);
  }
  switch(param_1) {
  case 0:
    *(short *)(iVar2 + 0x1a) = *(short *)(iVar2 + 0x1a) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
    break;
  case 1:
    *(short *)(iVar2 + 0x20) = *(short *)(iVar2 + 0x20) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
    break;
  case 2:
    *(short *)(iVar2 + 0x26) = *(short *)(iVar2 + 0x26) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
    break;
  case 3:
    *(short *)(iVar2 + 0x2c) = *(short *)(iVar2 + 0x2c) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
    break;
  case 4:
    *(short *)(iVar2 + 0x32) = *(short *)(iVar2 + 0x32) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
    break;
  case 5:
    *(short *)(iVar2 + 0x38) = *(short *)(iVar2 + 0x38) + (-(ushort)(iVar3 != 1) & 0xfffe) + 1;
  }
  thunk_FUN_004132d0(DAT_004952a0,param_2,pcVar5);
  operator_delete(pcVar5);
  return;
}

