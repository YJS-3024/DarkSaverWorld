
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl aiexcute(void) */

void __cdecl aiexcute(void)

{
                    /* 0x23c0  88  ?aiexcute@@YAXXZ */
  if (DAT_100e03e4 != DAT_100e03e8) {
    _DAT_1003d3a8 = *(int *)(&DAT_1003d3b0 + DAT_100e03e4 * 4);
    *(undefined4 *)(&DAT_1003d3b0 + DAT_100e03e4 * 4) = 0xffffffff;
    DAT_100e03e4 = (DAT_100e03e4 + 1) % 100;
    _DAT_100e03e0 = 1;
    _DAT_100e03ec = 0;
    DispatchUnitAiByRole(_DAT_1003d3a8);
  }
  return;
}


/* incoming refs
10014144 DATA
Entry Point EXTERNAL
*/
