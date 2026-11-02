
void __cdecl FUN_1000eab0(undefined1 param_1)

{
  void *pvVar1;
  int local_4;
  
  local_4 = 0;
  pvVar1 = operator_new(3);
  FUN_1000c600((int)pvVar1,&local_4,0x5c);
  FUN_1000c600((int)pvVar1,&local_4,0x20);
  FUN_1000c600((int)pvVar1,&local_4,param_1);
  FUN_1000e2c0((LPARAM)pvVar1,3);
  operator_delete(pvVar1);
  return;
}

