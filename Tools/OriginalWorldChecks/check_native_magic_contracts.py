"""Focused production magic contracts; run entry/API and gateway prerequisites first.
The full check_native_magic entrypoint additionally owns its broad dependency chain.
"""
from check_native_magic import run_contract
if __name__=='__main__':run_contract()
