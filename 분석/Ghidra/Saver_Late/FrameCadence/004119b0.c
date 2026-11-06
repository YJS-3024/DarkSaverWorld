
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined4 __thiscall FUN_004119b0(void *this,int param_1)

{
  int iVar1;
  int *piVar2;
  clock_t cVar3;
  undefined4 uVar4;
  
  if (*(int *)((int)this + 0x1bc) != 0) {
    thunk_FUN_00411330(this);
    thunk_FUN_00412880(this);
    return 0;
  }
  if (*(int *)((int)this + 0x128) == 0) {
    if (*(int *)((int)this + 0xe0) == 0) {
      if (param_1 == 1000) {
        DAT_00495158 = DAT_00495158 + 1;
      }
      if ((DAT_004950f4 & 2) == 0) {
        DAT_004950f4 = DAT_004950f4 | 2;
        DAT_004950e8 = clock();
      }
      cVar3 = clock();
      DAT_004950e4 = cVar3 - DAT_004950e8;
      if (1000 < DAT_004950e4) {
        _DAT_0049515c = (DAT_00495158 * 1000) / DAT_004950e4;
      }
      if (0x17 < DAT_004950e4) {
        DAT_00495158 = 0;
        DAT_004950e8 = clock();
      }
      if (*(BBDX_MouseInformation **)((int)this + 0xe8) == (BBDX_MouseInformation *)0x0)
      goto LAB_00411e3c;
      CDirectInput::MouseInputCheck(*(BBDX_MouseInformation **)((int)this + 0xe8));
      iVar1 = *(int *)((int)this + 0xec);
      if (*(int *)(iVar1 + 0x14) == 0) {
        if (*(int *)(iVar1 + 4) == 0) {
          if (*(int *)(iVar1 + 0x14) != 0) goto LAB_00411d2f;
          goto LAB_00411d49;
        }
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_004125b0((int)this);
      }
      else {
LAB_00411d2f:
        if (*(int *)(iVar1 + 4) == 0) {
          thunk_FUN_00412700((int)this);
        }
        else {
LAB_00411d49:
          if (((*(int *)(iVar1 + 4) != 0) && (*(int *)(iVar1 + 4) == *(int *)(iVar1 + 0x14))) &&
             (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
            *(clock_t *)(iVar1 + 0x2c) = cVar3;
            *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
            thunk_FUN_004127e0(this,*(int *)((int)this + 0xec) + 0xc);
          }
        }
      }
      iVar1 = *(int *)((int)this + 0xec);
      if (*(int *)(iVar1 + 0x18) == 0) {
        if (*(int *)(iVar1 + 8) == 0) {
          if (*(int *)(iVar1 + 0x18) != 0) goto LAB_00411dbe;
          goto LAB_00411dd8;
        }
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_00412690((int)this);
      }
      else {
LAB_00411dbe:
        if (*(int *)(iVar1 + 8) == 0) {
          thunk_FUN_00412770((int)this);
        }
        else {
LAB_00411dd8:
          if (((*(int *)(iVar1 + 8) != 0) && (*(int *)(iVar1 + 8) == *(int *)(iVar1 + 0x18))) &&
             (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
            *(clock_t *)(iVar1 + 0x2c) = cVar3;
            *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
            thunk_FUN_00412830(this,*(int *)((int)this + 0xec) + 0xc);
          }
        }
      }
      iVar1 = *(int *)((int)this + 0xec);
      if ((*(int *)(iVar1 + 0xc) != *(int *)(iVar1 + 0x1c)) ||
         (*(int *)(iVar1 + 0x10) != *(int *)(iVar1 + 0x20))) {
        thunk_FUN_00412620((int)this);
      }
LAB_00411e3c:
      if ((*(int *)((int)this + 0x144) == 0) && (*(int *)((int)this + 0x148) == 0)) {
        if ((*(int *)((int)this + 0x10c) != 0) &&
           (DAT_00495160 = DAT_00495160 + 1, 1000 < DAT_00495160)) {
          DAT_00495160 = 0;
          thunk_FUN_00413320();
        }
        uVar4 = (**(code **)(**(int **)((int)this + 0x148) + 0x18))(param_1);
        return uVar4;
      }
      if (DAT_004950e4 < 0xe) {
        return 0;
      }
      if (*(int *)((int)this + 0x144) == 0) {
        if (*(short *)((int)this + 0xc0) == 2) {
          piVar2 = *(int **)((int)this + 0x1c0);
          if (*(int *)((int)this + 0x1c8) == 1) {
            uVar4 = (**(code **)(*piVar2 + 0x18))(param_1);
            return uVar4;
          }
          if (piVar2 != (int *)0x0) {
            uVar4 = (**(code **)(*piVar2 + 0x18))(param_1);
            return uVar4;
          }
        }
        uVar4 = (**(code **)(**(int **)((int)this + 0xf0) + 0x18))(param_1);
        return uVar4;
      }
      uVar4 = (**(code **)(**(int **)((int)this + 0x148) + 0x18))(param_1);
      return uVar4;
    }
    if (param_1 == 1000) {
      DAT_0049514c = DAT_0049514c + 1;
    }
    if ((DAT_004950f4 & 1) == 0) {
      DAT_004950f4 = DAT_004950f4 | 1;
      DAT_004950f0 = clock();
    }
    cVar3 = clock();
    DAT_004950ec = cVar3 - DAT_004950f0;
    if (1000 < DAT_004950ec) {
      _DAT_00495150 = (DAT_0049514c * 1000) / DAT_004950ec;
    }
    if (0x17 < DAT_004950ec) {
      DAT_0049514c = 0;
      DAT_004950f0 = clock();
    }
    if (*(BBDX_MouseInformation **)((int)this + 0xe8) != (BBDX_MouseInformation *)0x0) {
      CDirectInput::MouseInputCheck(*(BBDX_MouseInformation **)((int)this + 0xe8));
      iVar1 = *(int *)((int)this + 0xec);
      if (*(int *)(iVar1 + 0x14) == 0) {
        if (*(int *)(iVar1 + 4) == 0) {
          if (*(int *)(iVar1 + 0x14) != 0) goto LAB_00411ab4;
          goto LAB_00411acb;
        }
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_004125b0((int)this);
      }
      else {
LAB_00411ab4:
        if (*(int *)(iVar1 + 4) == 0) {
          thunk_FUN_00412700((int)this);
        }
        else {
LAB_00411acb:
          if (((*(int *)(iVar1 + 4) != 0) && (*(int *)(iVar1 + 4) == *(int *)(iVar1 + 0x14))) &&
             (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
            *(clock_t *)(iVar1 + 0x2c) = cVar3;
            *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
            thunk_FUN_004127e0(this,*(int *)((int)this + 0xec) + 0xc);
          }
        }
      }
      iVar1 = *(int *)((int)this + 0xec);
      if (*(int *)(iVar1 + 0x18) == 0) {
        if (*(int *)(iVar1 + 8) == 0) {
          if (*(int *)(iVar1 + 0x18) != 0) goto LAB_00411b35;
          goto LAB_00411b4c;
        }
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_00412690((int)this);
      }
      else {
LAB_00411b35:
        if (*(int *)(iVar1 + 8) == 0) {
          thunk_FUN_00412770((int)this);
        }
        else {
LAB_00411b4c:
          if (((*(int *)(iVar1 + 8) != 0) && (*(int *)(iVar1 + 8) == *(int *)(iVar1 + 0x18))) &&
             (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
            *(clock_t *)(iVar1 + 0x2c) = cVar3;
            *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
            thunk_FUN_00412830(this,*(int *)((int)this + 0xec) + 0xc);
          }
        }
      }
      iVar1 = *(int *)((int)this + 0xec);
      if ((*(int *)(iVar1 + 0xc) != *(int *)(iVar1 + 0x1c)) ||
         (*(int *)(iVar1 + 0x10) != *(int *)(iVar1 + 0x20))) {
        thunk_FUN_00412620((int)this);
      }
    }
    if ((*(int *)((int)this + 0x144) != 0) || (*(int *)((int)this + 0x148) != 0)) {
      if (DAT_004950ec < 0xe) {
        return 0;
      }
      if (*(int *)((int)this + 0x144) == 0) {
        if (*(short *)((int)this + 0xc0) == 2) {
          piVar2 = *(int **)((int)this + 0x1c0);
          if (*(int *)((int)this + 0x1c8) == 1) {
            uVar4 = (**(code **)(*piVar2 + 0x18))(param_1);
            return uVar4;
          }
          if (piVar2 != (int *)0x0) {
            uVar4 = (**(code **)(*piVar2 + 0x18))(param_1);
            return uVar4;
          }
        }
        uVar4 = (**(code **)(**(int **)((int)this + 0xf0) + 0x18))(param_1);
        return uVar4;
      }
      goto LAB_00412085;
    }
    if ((*(int *)((int)this + 0x10c) == 0) ||
       (DAT_00495154 = DAT_00495154 + 1, DAT_00495154 < 0x3e9)) goto LAB_00412085;
    DAT_00495154 = 0;
  }
  else {
    if (*(BBDX_MouseInformation **)((int)this + 0xe8) != (BBDX_MouseInformation *)0x0) {
      CDirectInput::MouseInputCheck(*(BBDX_MouseInformation **)((int)this + 0xe8));
      iVar1 = *(int *)((int)this + 0xec);
      if ((*(int *)(iVar1 + 0x14) == 0) && (*(int *)(iVar1 + 4) != 0)) {
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_004125b0((int)this);
      }
      else if ((*(int *)(iVar1 + 0x14) == 0) || (*(int *)(iVar1 + 4) != 0)) {
        if (((*(int *)(iVar1 + 4) != 0) && (*(int *)(iVar1 + 4) == *(int *)(iVar1 + 0x14))) &&
           (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
          *(clock_t *)(iVar1 + 0x2c) = cVar3;
          *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
          thunk_FUN_004127e0(this,*(int *)((int)this + 0xec) + 0xc);
        }
      }
      else {
        thunk_FUN_00412700((int)this);
      }
      iVar1 = *(int *)((int)this + 0xec);
      if (*(int *)(iVar1 + 0x18) == 0) {
        if (*(int *)(iVar1 + 8) == 0) {
          if (*(int *)(iVar1 + 0x18) != 0) goto LAB_00411fd6;
          goto LAB_00411fed;
        }
        cVar3 = clock();
        *(clock_t *)(iVar1 + 0x2c) = cVar3;
        *(undefined4 *)(iVar1 + 0x30) = 0;
        thunk_FUN_00412690((int)this);
      }
      else {
LAB_00411fd6:
        if (*(int *)(iVar1 + 8) == 0) {
          thunk_FUN_00412770((int)this);
        }
        else {
LAB_00411fed:
          if (((*(int *)(iVar1 + 8) != 0) && (*(int *)(iVar1 + 8) == *(int *)(iVar1 + 0x18))) &&
             (cVar3 = clock(), 0x1e < cVar3 - *(int *)(iVar1 + 0x2c))) {
            *(clock_t *)(iVar1 + 0x2c) = cVar3;
            *(int *)(iVar1 + 0x30) = *(int *)(iVar1 + 0x30) + 1;
            thunk_FUN_00412830(this,*(int *)((int)this + 0xec) + 0xc);
          }
        }
      }
      iVar1 = *(int *)((int)this + 0xec);
      if ((*(int *)(iVar1 + 0xc) != *(int *)(iVar1 + 0x1c)) ||
         (*(int *)(iVar1 + 0x10) != *(int *)(iVar1 + 0x20))) {
        thunk_FUN_00412620((int)this);
      }
    }
    if ((*(int *)((int)this + 0x144) == 0) || (*(int *)((int)this + 0x148) == 0)) {
      piVar2 = *(int **)((int)this + 0xf0);
      if ((piVar2 == (int *)0x0) || (piVar2[0x2f] == 0)) {
        return 0;
      }
      if (*(short *)((int)this + 0xc0) == 2) {
        if (*(int *)((int)this + 0x1c8) == 1) {
          uVar4 = (**(code **)(**(int **)((int)this + 0x1c0) + 0x18))(param_1);
          return uVar4;
        }
        if (*(int **)((int)this + 0x1c0) != (int *)0x0) {
          uVar4 = (**(code **)(**(int **)((int)this + 0x1c0) + 0x18))(param_1);
          return uVar4;
        }
      }
      uVar4 = (**(code **)(*piVar2 + 0x18))(param_1);
      return uVar4;
    }
    if ((*(int *)((int)this + 0x10c) == 0) ||
       (DAT_00495164 = DAT_00495164 + 1, DAT_00495164 < 0x3e9)) goto LAB_00412085;
    DAT_00495164 = 0;
  }
  thunk_FUN_00413320();
LAB_00412085:
  uVar4 = (**(code **)(**(int **)((int)this + 0x148) + 0x18))(param_1);
  return uVar4;
}


/* incoming refs
00402716 UNCONDITIONAL_JUMP
*/
