
void __thiscall FUN_00462b90(void *this,int param_1)

{
  undefined4 *puVar1;
  undefined4 *puVar2;
  int iVar3;
  undefined4 *puVar4;
  undefined4 *puVar5;
  
  iVar3 = thunk_FUN_00427b50(*(void **)((int)this + 0xb0),param_1);
  *(int *)((int)this + 0x13c) = iVar3;
  if (iVar3 == 0) {
    *(undefined4 *)((int)this + 0x13c) = *(undefined4 *)(*(int *)((int)this + 0xb0) + 0x3c);
  }
  puVar1 = *(undefined4 **)((int)this + 0x13c);
  puVar2 = *(undefined4 **)((int)this + 0x140);
  iVar3 = 0xb5;
  puVar4 = puVar1;
  puVar5 = puVar2;
  while( true ) {
    puVar5 = puVar5 + 1;
    puVar4 = puVar4 + 1;
    if (iVar3 == 0) break;
    iVar3 = iVar3 + -1;
    *puVar5 = *puVar4;
  }
  puVar2[0xb6] = puVar1[0xb6];
  iVar3 = *(int *)((int)this + 0x13c);
  if (*(short *)(iVar3 + 0x3e) != 0) {
    thunk_FUN_004623a0((int)this);
  }
  GetFace(*(CDDSurface **)((int)this + 0xb4),(int)*(short *)(iVar3 + 10),0,0);
  return;
}

