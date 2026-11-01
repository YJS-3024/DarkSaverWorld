/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x1000ef10; source SHA256 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b
 */

/* Original export dmSetChar2. Update an existing unit placement/related state, clear previous
   occupancy, and register the new native tile. Do not count this as a separate initial monster
   roster loader.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl
UpdateScriptActorPlacement(int param_1,int param_2,int param_3,int param_4,int param_5,int param_6)

{
  undefined4 uVar1;
  undefined2 local_8;
  short local_6;
  undefined2 local_4;
  
                    /* 0xef10  94  ?dmSetChar2@@YAXHHHHHH@Z */
  *(int *)&g_RuntimeCharacters[param_1].field_0x68 = param_2;
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
  *(undefined4 *)&g_RuntimeCharacters[param_1].field_0x1c = uVar1;
LAB_1000ef5d:
  *(int *)&g_RuntimeCharacters[param_1].field_0x2b0 = param_5;
  mainmapsetchar(g_RuntimeCharacters[param_1].tileX,g_RuntimeCharacters[param_1].tileY,-1);
  g_RuntimeCharacters[param_1].tileX = param_3;
  g_RuntimeCharacters[param_1].tileY = param_4;
  mainmapsetchar(param_3,param_4,param_1);
  local_8 = (undefined2)param_1;
  local_6 = (short)param_6;
  local_4 = 0;
  if ((param_5 == 2) && (-1 < local_6)) {
    AttackChar((_PGSATTACKFX *)&local_8);
    return;
  }
  *(uint *)&g_RuntimeCharacters[param_1].field_0x100 =
       *(uint *)&g_RuntimeCharacters[param_1].field_0x100 & 0xfffffffb;
  return;
}

