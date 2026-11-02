
bool __thiscall FUN_100025c0(void *this,undefined4 param_1)

{
  clock_t cVar1;
  
  CAlphaEffectorForChar::DrawAlphaEffect
            (*(CAlphaEffectorForChar **)((int)this + 0x18),param_1,*(int *)((int)this + 0x20) + 0x10
             ,*(int *)((int)this + 0x24) + 0x10);
  cVar1 = clock();
  return 2999 < cVar1 - *(int *)((int)this + 0xc);
}

