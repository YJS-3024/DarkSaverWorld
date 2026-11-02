
int __cdecl FUN_1000f040(char *param_1,int param_2)

{
  char cVar1;
  int iVar2;
  char *pcVar3;
  
  cVar1 = *param_1;
  pcVar3 = param_1;
  if (cVar1 != '\0') {
    while (('/' < cVar1 && (*pcVar3 < ':'))) {
      cVar1 = pcVar3[1];
      pcVar3 = pcVar3 + 1;
      if (cVar1 == '\0') {
        iVar2 = atoi(param_1);
        return iVar2;
      }
    }
  }
  return param_2;
}

