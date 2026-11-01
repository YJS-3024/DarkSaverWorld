/* Ghidra decompilation; not recovered original source. Evidence: ev_57096d339aed562b124046441bf08c0a9c2539a3d50970285c08afabb79b8fe8 */

/* void __cdecl dmSetChar2(int,int,int,int,int,int) */

void __cdecl dmSetChar2(int param_1,int param_2,int param_3,int param_4,int param_5,int param_6)

{
  undefined4 uVar1;
  int iVar2;
  undefined2 local_8;
  short local_6;
  undefined2 local_4;
  
                    /* 0xef10  94  ?dmSetChar2@@YAXHHHHHH@Z */
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

