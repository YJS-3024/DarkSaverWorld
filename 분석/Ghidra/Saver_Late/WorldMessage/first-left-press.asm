
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

00475c50 <.text+0x74c50>:
  475c50:	56                   	push   esi
  475c51:	57                   	push   edi
  475c52:	8b f1                	mov    esi,ecx
  475c54:	e8 5e d4 f8 ff       	call   0x4030b7
  475c59:	85 c0                	test   eax,eax
  475c5b:	8b 46 58             	mov    eax,DWORD PTR [esi+0x58]
  475c5e:	74 0e                	je     0x475c6e
  475c60:	85 c0                	test   eax,eax
  475c62:	75 0e                	jne    0x475c72
  475c64:	5f                   	pop    edi
  475c65:	b8 01 00 00 00       	mov    eax,0x1
  475c6a:	5e                   	pop    esi
  475c6b:	c2 08 00             	ret    0x8
  475c6e:	85 c0                	test   eax,eax
  475c70:	74 1c                	je     0x475c8e
  475c72:	8b 44 24 10          	mov    eax,DWORD PTR [esp+0x10]
  475c76:	8b 48 04             	mov    ecx,DWORD PTR [eax+0x4]
  475c79:	8b 10                	mov    edx,DWORD PTR [eax]
  475c7b:	51                   	push   ecx
  475c7c:	52                   	push   edx
  475c7d:	8b ce                	mov    ecx,esi
  475c7f:	e8 82 d0 f8 ff       	call   0x402d06
  475c84:	5f                   	pop    edi
  475c85:	b8 01 00 00 00       	mov    eax,0x1
  475c8a:	5e                   	pop    esi
  475c8b:	c2 08 00             	ret    0x8
  475c8e:	8b 7c 24 10          	mov    edi,DWORD PTR [esp+0x10]
  475c92:	8b 06                	mov    eax,DWORD PTR [esi]
  475c94:	57                   	push   edi
  475c95:	8b ce                	mov    ecx,esi
  475c97:	ff 50 08             	call   DWORD PTR [eax+0x8]
  475c9a:	83 f8 ff             	cmp    eax,0xffffffff
  475c9d:	89 46 40             	mov    DWORD PTR [esi+0x40],eax
  475ca0:	74 1d                	je     0x475cbf
  475ca2:	8b 4e 18             	mov    ecx,DWORD PTR [esi+0x18]
  475ca5:	57                   	push   edi
  475ca6:	8b 04 81             	mov    eax,DWORD PTR [ecx+eax*4]
  475ca9:	8b 4c 24 10          	mov    ecx,DWORD PTR [esp+0x10]
  475cad:	51                   	push   ecx
  475cae:	8b c8                	mov    ecx,eax
  475cb0:	8b 10                	mov    edx,DWORD PTR [eax]
  475cb2:	ff 52 24             	call   DWORD PTR [edx+0x24]
  475cb5:	5f                   	pop    edi
  475cb6:	b8 01 00 00 00       	mov    eax,0x1
  475cbb:	5e                   	pop    esi
  475cbc:	c2 08 00             	ret    0x8
  475cbf:	5f                   	pop    edi
