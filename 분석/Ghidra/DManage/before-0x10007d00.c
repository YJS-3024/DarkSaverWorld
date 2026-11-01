/* Ghidra decompilation. Evidence: ev_92b98a109868da7ae5980bd03982b16ea3b97819c5139269115f2fd19edd5964 */

/* public: void __thiscall BBDX_MapManager::StartPosAndValidSpriteInit(void) */

void __thiscall BBDX_MapManager::StartPosAndValidSpriteInit(BBDX_MapManager *this)

{
  void *pvVar1;
  
                    /* 0x7d00  195  ?StartPosAndValidSpriteInit@BBDX_MapManager@@QAEXXZ */
  *(undefined4 *)(this + 0x70) = 0xffffff9c;
  *(undefined4 *)(this + 0x78) = 0xffffff9c;
  *(undefined4 *)(this + 0x74) = 0xffffff9c;
  *(undefined4 *)(this + 0x7c) = 0xffffff9c;
  if (*(void **)(this + 0x68) != (void *)0x0) {
    operator_delete(*(void **)(this + 0x68));
    *(undefined4 *)(this + 0x68) = 0;
  }
  pvVar1 = operator_new(*(int *)(*(int *)(this + 0x4c) + 0x38) << 2);
  *(void **)(this + 0x68) = pvVar1;
  return;
}

