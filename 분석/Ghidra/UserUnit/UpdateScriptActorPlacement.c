/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x1000ef10
 * Evidence: ev_9ef6510b45e929699853f7a6a4b54da621d03be10e92208e16ba8abae9c4d433
 */

/* void __cdecl dmSetChar2(int,int,int,int,int,int) */

void __cdecl
UpdateScriptActorPlacement(int param_1,int param_2,int param_3,int param_4,int param_5,int param_6)

{
  undefined4 uVar1;
  int iVar2;
  undefined2 local_8;
  short local_6;
  undefined2 local_4;
  
                    /* Original export dmSetChar2. Update an existing unit placement/related state,
                       clear previous occupancy, and register the new native tile. Do not count this
                       as a separate initial monster roster loader. */
  iVar2 = param_1 * 0x2e0;
  *(int *)(iVar2 + 0x68 + DAT_1010b3a0) = param_2;
  if (param_2 == 0x316) {
LAB_1000ef4e:
    uVar1 = 1;
  }
  else {
    if (param_2 != 899) {
      if (param_2 != 0x382) goto LAB_1000ef5d;
      goto LAB_1000ef4e;
    }
    uVar1 = 3;
  }
  *(undefined4 *)(iVar2 + 0x1c + DAT_1010b3a0) = uVar1;
LAB_1000ef5d:
  *(int *)(iVar2 + 0x2b0 + DAT_1010b3a0) = param_5;
  mainmapsetchar(*(int *)(iVar2 + 0x70 + DAT_1010b3a0),*(int *)(iVar2 + 0x74 + DAT_1010b3a0),-1);
  *(int *)(iVar2 + 0x70 + DAT_1010b3a0) = param_3;
  *(int *)(iVar2 + 0x74 + DAT_1010b3a0) = param_4;
  mainmapsetchar(param_3,param_4,param_1);
  local_8 = (undefined2)param_1;
  local_6 = (short)param_6;
  local_4 = 0;
  if ((param_5 == 2) && (-1 < local_6)) {
    AttackChar((_PGSATTACKFX *)&local_8);
    return;
  }
  *(uint *)(iVar2 + 0x100 + DAT_1010b3a0) = *(uint *)(iVar2 + 0x100 + DAT_1010b3a0) & 0xfffffffb;
  return;
}

