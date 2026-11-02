
void __cdecl FUN_1000e420(undefined4 param_1)

{
  void *pvVar1;
  int local_4;
  
  local_4 = 0;
  pvVar1 = operator_new(5);
  FUN_1000c600((int)pvVar1,&local_4,0x3a);
  FUN_1000c660((int)pvVar1,&local_4,param_1);
  FUN_1000e2c0((LPARAM)pvVar1,5);
  operator_delete(pvVar1);
  return;
}

