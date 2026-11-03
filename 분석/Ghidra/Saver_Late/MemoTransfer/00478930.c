
byte __cdecl FUN_00478930(undefined4 param_1,undefined4 param_2,int param_3,undefined4 param_4)

{
  undefined4 *puVar1;
  uint uVar2;
  
  puVar1 = (undefined4 *)(param_3 + 0x1c);
  *(undefined4 *)(param_3 + 0x28) = param_1;
  *(undefined4 *)(param_3 + 0x2c) = param_2;
  *(undefined4 *)(param_3 + 0x24) = param_4;
  *puVar1 = 0x800;
  uVar2 = (**(code **)(param_3 + 0x28))
                    ((byte *)(param_3 + 0x2234),puVar1,*(undefined4 *)(param_3 + 0x24));
  *(uint *)(param_3 + 0x20) = uVar2;
  if (uVar2 < 5) {
    return 3;
  }
  *(uint *)(param_3 + 4) = (uint)*(byte *)(param_3 + 0x2234);
  *(uint *)(param_3 + 0xc) = (uint)*(byte *)(param_3 + 0x2235);
  *(uint *)(param_3 + 0x14) = (uint)*(byte *)(param_3 + 0x2236);
  *(undefined4 *)(param_3 + 0x18) = 0;
  *puVar1 = 3;
  uVar2 = *(uint *)(param_3 + 0xc);
  if ((3 < uVar2) && (uVar2 < 7)) {
    *(int *)(param_3 + 0x10) = 0xffff >> (0x10U - (char)uVar2 & 0x1f);
    if (*(int *)(param_3 + 4) != 0) {
      if (*(int *)(param_3 + 4) != 1) {
        return 2;
      }
      FUN_00478fb0((undefined4 *)(param_3 + 0x2fb4),(undefined4 *)&DAT_00491cb8,0x100);
      FUN_00478eb0(param_3);
    }
    FUN_00478fb0((undefined4 *)(param_3 + 0x30f4),(undefined4 *)&DAT_00491c98,0x10);
    FUN_00478e70(0x10,param_3 + 0x30f4,0x491ca8,param_3 + 0x2b34);
    FUN_00478fb0((undefined4 *)(param_3 + 0x3104),(undefined4 *)&DAT_00491c68,0x10);
    FUN_00478fb0((undefined4 *)(param_3 + 0x3114),(undefined4 *)&DAT_00491c78,0x20);
    FUN_00478fb0((undefined4 *)(param_3 + 0x30b4),(undefined4 *)&DAT_00491be8,0x40);
    FUN_00478e70(0x40,param_3 + 0x30b4,0x491c28,param_3 + 0x2a34);
    uVar2 = FUN_00478ab0(param_3);
    return -(uVar2 == 0x306) & 4;
  }
  return 1;
}

