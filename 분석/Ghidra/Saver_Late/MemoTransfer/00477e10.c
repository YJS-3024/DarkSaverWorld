
dword __cdecl FUN_00477e10(dword arg0,dword arg1,dword arg2)

{
  byte bVar1;
  uint uVar2;
  int iVar3;
  
  uVar2 = *(uint *)arg2;
  for (iVar3 = *(int *)arg1; iVar3 != 0; iVar3 = iVar3 + -1) {
    bVar1 = *(byte *)arg0;
    arg0 = arg0 + 1;
    uVar2 = *(uint *)(&DAT_00491370 + ((bVar1 ^ uVar2) & 0xff) * 4) ^ uVar2 >> 8;
  }
  return uVar2;
}

