/* Ghidra decompilation; not recovered original source. Evidence: ev_92a72b3abb01acb30f40032dd504217be29d94e2ebcba07a5821f9a050ccffaf */

void __cdecl FUN_1000eb10(undefined1 *param_1)

{
  switch(*param_1) {
  case 0x5a:
    InitValue();
    return;
  case 0x5b:
    FUN_1000e210((int)(param_1 + 1));
    break;
  case 0x5c:
    switch(param_1[1]) {
    case 1:
      FUN_1000d640((int)(param_1 + 2));
      return;
    case 3:
      FUN_1000d800((int)(param_1 + 2));
      return;
    case 4:
                    /* WARNING: Could not recover jumptable at 0x1000eb54. Too many branches */
                    /* WARNING: Treating indirect jump as call */
      IncTick();
      return;
    case 5:
      FUN_1000d760((int)(param_1 + 2));
      return;
    case 6:
      FUN_1000d7b0((int)(param_1 + 2));
      return;
    case 7:
      FUN_1000d830((int)(param_1 + 2));
      return;
    case 0xc:
      FUN_1000d870((int)(param_1 + 2));
      return;
    case 0xd:
      FUN_1000d8a0((int)(param_1 + 2));
      return;
    case 0xe:
      FUN_1000d6a0((int)(param_1 + 2));
      return;
    case 0xf:
      FUN_1000e290((int)(param_1 + 2));
      return;
    case 0x13:
      FUN_1000dcb0((int)(param_1 + 2));
      return;
    case 0x15:
      FUN_1000dcf0((int)(param_1 + 2));
      return;
    case 0x20:
      FUN_1000d8f0((int)(param_1 + 2));
      return;
    case 0x30:
      FUN_1000ddc0((int)(param_1 + 2));
      return;
    case 0x31:
      FUN_1000dec0((int)(param_1 + 2));
      return;
    case 0x40:
      FUN_1000d610((int)(param_1 + 2));
      return;
    case 0x50:
      FUN_1000c7d0((int)(param_1 + 2));
      return;
    case 0x51:
      FUN_1000cb90((int)(param_1 + 2));
      return;
    case 0x52:
      FUN_1000d400((int)(param_1 + 2));
      return;
    case 0x53:
      FUN_1000d470((int)(param_1 + 2));
      return;
    case 0x54:
      FUN_1000d280((int)(param_1 + 2));
      return;
    case 0x5c:
      FUN_1000dfa0(param_1 + 2);
      return;
    case 0x60:
      FUN_1000d550((int)(param_1 + 2));
      return;
    case 0x61:
      FUN_1000d150(param_1[2]);
      return;
    case 100:
      FUN_1000df10((int)(param_1 + 2));
      return;
    case 0x65:
      FUN_1000df10((int)(param_1 + 2));
      return;
    case 0x66:
      FUN_1000e060((int)(param_1 + 2));
      return;
    case 0x67:
      FUN_1000db20((int)(param_1 + 2));
      return;
    case 0x68:
      FUN_1000dfb0((int)(param_1 + 2));
      return;
    case 0x82:
      FUN_1000d700((int)(param_1 + 2));
      return;
    case 0x89:
      FUN_1000da10((int)(param_1 + 2));
      return;
    case 0xb2:
      FUN_1000d4c0((int)(param_1 + 2));
      return;
    }
    break;
  case 0x7a:
    FUN_1000e110((int)(param_1 + 1));
    return;
  case 0x7b:
    FUN_1000e0a0((int)(param_1 + 1));
    return;
  }
  return;
}

