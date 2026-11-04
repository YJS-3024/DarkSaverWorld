"""Opt-in path compatibility for standalone checks and their Python children.
No assertion or production source is removed. Activated only by the runner env.
"""
import json
import os
import pathlib
import subprocess
import sys

if os.environ.get('DARKSAVER_CHECK_PATHS'):
    _rules = sorted(json.loads(os.environ['DARKSAVER_CHECK_PATHS']), key=lambda p: -len(p[0]))
    def translate(value):
        if not isinstance(value, (str, os.PathLike)):
            return value
        raw = os.fspath(value).replace('\\', '/')
        for old, new in _rules:
            if new != old and (raw == new or raw.startswith(new + '/')):
                return os.fspath(value)
        for old, new in _rules:
            if raw == old or raw.startswith(old + '/'):
                return new + raw[len(old):]
        return os.fspath(value)

    _new = pathlib.Path.__new__
    def mapped_new(cls, *args, **kwargs):
        if args:
            args = (translate(args[0]),) + args[1:]
        return _new(cls, *args, **kwargs)
    pathlib.Path.__new__ = mapped_new
    if sys.version_info >= (3, 12):
        _init = pathlib.Path.__init__
        def mapped_init(self, *args, **kwargs):
            if args:
                args = (translate(args[0]),) + args[1:]
            _init(self, *args, **kwargs)
        pathlib.Path.__init__ = mapped_init

    _divide = pathlib.Path.__truediv__
    _join = pathlib.Path.joinpath
    def mapped_divide(self, key):
        result = _divide(self, key)
        return type(result)(translate(result))
    def mapped_join(self, *keys):
        result = _join(self, *keys)
        return type(result)(translate(result))
    pathlib.Path.__truediv__ = mapped_divide
    pathlib.Path.joinpath = mapped_join

    _popen = subprocess.Popen
    class CheckPopen(_popen):
        def __init__(self, args, *positional, **kwargs):
            if isinstance(args, (list, tuple)):
                args = [translate(a) if isinstance(a, os.PathLike) else a for a in args]
                # All subprocess calls remain argument arrays; never invoke a shell.
                if args and os.name == 'nt' and isinstance(args[0], str):
                    if not os.path.isfile(args[0]) and os.path.isfile(args[0] + '.exe'):
                        args[0] += '.exe'
                # Old check writers emitted unquoted response-file rows. Quote each
                # complete compiler argument, preserving option prefixes and spaces.
                if any('csc.dll' in str(a) for a in args):
                    for a in args:
                        if isinstance(a, str) and a.startswith('@'):
                            path = pathlib.Path(a[1:])
                            lines = path.read_text().splitlines()
                            fixed = ['"' + line + '"' if line and not line.startswith('"') else line for line in lines]
                            path.write_text('\n'.join(fixed) + '\n')
            super().__init__(args, *positional, **kwargs)
    subprocess.Popen = CheckPopen
