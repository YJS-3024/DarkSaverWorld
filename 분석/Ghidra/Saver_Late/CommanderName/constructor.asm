
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

00456c00 <.text+0x55c00>:
  456c00:	83 c4 04             	add    esp,0x4
  456c03:	89 6b 04             	mov    DWORD PTR [ebx+0x4],ebp
  456c06:	33 c0                	xor    eax,eax
  456c08:	89 43 0c             	mov    DWORD PTR [ebx+0xc],eax
  456c0b:	89 43 08             	mov    DWORD PTR [ebx+0x8],eax
  456c0e:	e9 ec 00 00 00       	jmp    0x456cff
  456c13:	8b 4b 04             	mov    ecx,DWORD PTR [ebx+0x4]
  456c16:	85 c9                	test   ecx,ecx
  456c18:	75 25                	jne    0x456c3f
  456c1a:	8d 14 ed 00 00 00 00 	lea    edx,[ebp*8+0x0]
  456c21:	52                   	push   edx
  456c22:	e8 65 29 02 00       	call   0x47958c
  456c27:	83 c4 04             	add    esp,0x4
  456c2a:	89 43 04             	mov    DWORD PTR [ebx+0x4],eax
  456c2d:	55                   	push   ebp
  456c2e:	50                   	push   eax
  456c2f:	e8 c6 c2 fa ff       	call   0x402efa
  456c34:	89 6b 0c             	mov    DWORD PTR [ebx+0xc],ebp
  456c37:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456c3a:	e9 c0 00 00 00       	jmp    0x456cff
  456c3f:	8b 7b 0c             	mov    edi,DWORD PTR [ebx+0xc]
  456c42:	3b ef                	cmp    ebp,edi
  456c44:	7f 1d                	jg     0x456c63
  456c46:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  456c49:	3b e8                	cmp    ebp,eax
  456c4b:	7e 0e                	jle    0x456c5b
  456c4d:	8b d5                	mov    edx,ebp
  456c4f:	2b d0                	sub    edx,eax
  456c51:	8d 04 c1             	lea    eax,[ecx+eax*8]
  456c54:	52                   	push   edx
  456c55:	50                   	push   eax
  456c56:	e8 9f c2 fa ff       	call   0x402efa
  456c5b:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456c5e:	e9 9c 00 00 00       	jmp    0x456cff
  456c63:	8b 43 10             	mov    eax,DWORD PTR [ebx+0x10]
  456c66:	85 c0                	test   eax,eax
  456c68:	75 24                	jne    0x456c8e
  456c6a:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  456c6d:	99                   	cdq
  456c6e:	83 e2 07             	and    edx,0x7
  456c71:	03 c2                	add    eax,edx
  456c73:	c1 f8 03             	sar    eax,0x3
  456c76:	83 f8 04             	cmp    eax,0x4
  456c79:	7d 07                	jge    0x456c82
  456c7b:	b8 04 00 00 00       	mov    eax,0x4
  456c80:	eb 0c                	jmp    0x456c8e
  456c82:	3d 00 04 00 00       	cmp    eax,0x400
  456c87:	7e 05                	jle    0x456c8e
  456c89:	b8 00 04 00 00       	mov    eax,0x400
  456c8e:	03 c7                	add    eax,edi
  456c90:	3b e8                	cmp    ebp,eax
  456c92:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  456c96:	7c 04                	jl     0x456c9c
  456c98:	89 6c 24 10          	mov    DWORD PTR [esp+0x10],ebp
  456c9c:	8b 4c 24 10          	mov    ecx,DWORD PTR [esp+0x10]
  456ca0:	8d 14 cd 00 00 00 00 	lea    edx,[ecx*8+0x0]
  456ca7:	52                   	push   edx
  456ca8:	e8 df 28 02 00       	call   0x47958c
  456cad:	8b 4b 08             	mov    ecx,DWORD PTR [ebx+0x8]
  456cb0:	8b 73 04             	mov    esi,DWORD PTR [ebx+0x4]
  456cb3:	c1 e1 03             	shl    ecx,0x3
  456cb6:	8b d1                	mov    edx,ecx
  456cb8:	8b f8                	mov    edi,eax
  456cba:	c1 e9 02             	shr    ecx,0x2
  456cbd:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  456cbf:	8b ca                	mov    ecx,edx
  456cc1:	8b d5                	mov    edx,ebp
  456cc3:	83 e1 03             	and    ecx,0x3
  456cc6:	83 c4 04             	add    esp,0x4
  456cc9:	f3 a4                	rep movs BYTE PTR es:[edi],BYTE PTR ds:[esi]
  456ccb:	8b 4b 08             	mov    ecx,DWORD PTR [ebx+0x8]
  456cce:	89 44 24 14          	mov    DWORD PTR [esp+0x14],eax
  456cd2:	2b d1                	sub    edx,ecx
  456cd4:	8d 04 c8             	lea    eax,[eax+ecx*8]
  456cd7:	52                   	push   edx
  456cd8:	50                   	push   eax
  456cd9:	e8 1c c2 fa ff       	call   0x402efa
  456cde:	8b 4b 04             	mov    ecx,DWORD PTR [ebx+0x4]
  456ce1:	51                   	push   ecx
  456ce2:	e8 99 28 02 00       	call   0x479580
  456ce7:	8b 54 24 18          	mov    edx,DWORD PTR [esp+0x18]
  456ceb:	8b 44 24 14          	mov    eax,DWORD PTR [esp+0x14]
  456cef:	8b 74 24 20          	mov    esi,DWORD PTR [esp+0x20]
  456cf3:	83 c4 04             	add    esp,0x4
  456cf6:	89 53 04             	mov    DWORD PTR [ebx+0x4],edx
  456cf9:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456cfc:	89 43 0c             	mov    DWORD PTR [ebx+0xc],eax
  456cff:	8b 4e 14             	mov    ecx,DWORD PTR [esi+0x14]
  456d02:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  456d05:	8b 5b 04             	mov    ebx,DWORD PTR [ebx+0x4]
  456d08:	f7 d1                	not    ecx
  456d0a:	f6 c1 01             	test   cl,0x1
  456d0d:	74 1a                	je     0x456d29
  456d0f:	8d 14 c5 00 00 00 00 	lea    edx,[eax*8+0x0]
  456d16:	8b ce                	mov    ecx,esi
  456d18:	52                   	push   edx
  456d19:	53                   	push   ebx
  456d1a:	e8 17 2d 02 00       	call   0x479a36
  456d1f:	5f                   	pop    edi
  456d20:	5e                   	pop    esi
  456d21:	5d                   	pop    ebp
  456d22:	5b                   	pop    ebx
  456d23:	83 c4 08             	add    esp,0x8
  456d26:	c2 04 00             	ret    0x4
  456d29:	c1 e0 03             	shl    eax,0x3
  456d2c:	50                   	push   eax
  456d2d:	53                   	push   ebx
  456d2e:	8b ce                	mov    ecx,esi
  456d30:	e8 fb 2c 02 00       	call   0x479a30
  456d35:	5f                   	pop    edi
  456d36:	5e                   	pop    esi
  456d37:	5d                   	pop    ebp
  456d38:	5b                   	pop    ebx
  456d39:	83 c4 08             	add    esp,0x8
  456d3c:	c2 04 00             	ret    0x4
  456d3f:	90                   	nop
  456d40:	cc                   	int3
  456d41:	cc                   	int3
  456d42:	cc                   	int3
  456d43:	cc                   	int3
  456d44:	cc                   	int3
  456d45:	cc                   	int3
  456d46:	cc                   	int3
  456d47:	cc                   	int3
  456d48:	cc                   	int3
  456d49:	cc                   	int3
  456d4a:	cc                   	int3
  456d4b:	cc                   	int3
  456d4c:	cc                   	int3
  456d4d:	cc                   	int3
  456d4e:	cc                   	int3
  456d4f:	cc                   	int3
  456d50:	cc                   	int3
  456d51:	cc                   	int3
  456d52:	cc                   	int3
  456d53:	cc                   	int3
  456d54:	cc                   	int3
  456d55:	cc                   	int3
  456d56:	cc                   	int3
  456d57:	cc                   	int3
  456d58:	cc                   	int3
  456d59:	cc                   	int3
  456d5a:	cc                   	int3
  456d5b:	cc                   	int3
  456d5c:	cc                   	int3
  456d5d:	cc                   	int3
  456d5e:	cc                   	int3
  456d5f:	cc                   	int3
  456d60:	cc                   	int3
  456d61:	cc                   	int3
  456d62:	cc                   	int3
  456d63:	cc                   	int3
  456d64:	cc                   	int3
  456d65:	cc                   	int3
  456d66:	cc                   	int3
  456d67:	cc                   	int3
  456d68:	cc                   	int3
  456d69:	cc                   	int3
  456d6a:	cc                   	int3
  456d6b:	cc                   	int3
  456d6c:	cc                   	int3
  456d6d:	cc                   	int3
  456d6e:	cc                   	int3
  456d6f:	cc                   	int3
  456d70:	cc                   	int3
  456d71:	cc                   	int3
  456d72:	cc                   	int3
  456d73:	cc                   	int3
  456d74:	cc                   	int3
  456d75:	cc                   	int3
  456d76:	cc                   	int3
  456d77:	cc                   	int3
  456d78:	cc                   	int3
  456d79:	cc                   	int3
  456d7a:	cc                   	int3
  456d7b:	cc                   	int3
  456d7c:	cc                   	int3
  456d7d:	cc                   	int3
  456d7e:	cc                   	int3
  456d7f:	cc                   	int3
  456d80:	cc                   	int3
  456d81:	cc                   	int3
  456d82:	cc                   	int3
  456d83:	cc                   	int3
  456d84:	cc                   	int3
  456d85:	cc                   	int3
  456d86:	cc                   	int3
  456d87:	cc                   	int3
  456d88:	cc                   	int3
  456d89:	cc                   	int3
  456d8a:	cc                   	int3
  456d8b:	cc                   	int3
  456d8c:	cc                   	int3
  456d8d:	cc                   	int3
  456d8e:	cc                   	int3
  456d8f:	cc                   	int3
  456d90:	cc                   	int3
  456d91:	cc                   	int3
  456d92:	cc                   	int3
  456d93:	cc                   	int3
  456d94:	cc                   	int3
  456d95:	cc                   	int3
  456d96:	cc                   	int3
  456d97:	cc                   	int3
  456d98:	cc                   	int3
  456d99:	cc                   	int3
  456d9a:	cc                   	int3
  456d9b:	cc                   	int3
  456d9c:	cc                   	int3
  456d9d:	cc                   	int3
  456d9e:	cc                   	int3
  456d9f:	cc                   	int3
  456da0:	56                   	push   esi
  456da1:	8b f1                	mov    esi,ecx
  456da3:	e8 cc b0 fa ff       	call   0x401e74
  456da8:	f6 44 24 08 01       	test   BYTE PTR [esp+0x8],0x1
  456dad:	74 09                	je     0x456db8
  456daf:	56                   	push   esi
  456db0:	e8 cb 27 02 00       	call   0x479580
  456db5:	83 c4 04             	add    esp,0x4
  456db8:	8b c6                	mov    eax,esi
  456dba:	5e                   	pop    esi
  456dbb:	c2 04 00             	ret    0x4
  456dbe:	90                   	nop
  456dbf:	90                   	nop
  456dc0:	cc                   	int3
  456dc1:	cc                   	int3
  456dc2:	cc                   	int3
  456dc3:	cc                   	int3
  456dc4:	cc                   	int3
  456dc5:	cc                   	int3
  456dc6:	cc                   	int3
  456dc7:	cc                   	int3
  456dc8:	cc                   	int3
  456dc9:	cc                   	int3
  456dca:	cc                   	int3
  456dcb:	cc                   	int3
  456dcc:	cc                   	int3
  456dcd:	cc                   	int3
  456dce:	cc                   	int3
  456dcf:	cc                   	int3
  456dd0:	8b 44 24 08          	mov    eax,DWORD PTR [esp+0x8]
  456dd4:	53                   	push   ebx
  456dd5:	55                   	push   ebp
  456dd6:	56                   	push   esi
  456dd7:	83 f8 ff             	cmp    eax,0xffffffff
  456dda:	57                   	push   edi
  456ddb:	8b d9                	mov    ebx,ecx
  456ddd:	74 03                	je     0x456de2
  456ddf:	89 43 10             	mov    DWORD PTR [ebx+0x10],eax
  456de2:	8b 6c 24 14          	mov    ebp,DWORD PTR [esp+0x14]
  456de6:	33 ff                	xor    edi,edi
  456de8:	3b ef                	cmp    ebp,edi
  456dea:	75 20                	jne    0x456e0c
  456dec:	8b 43 04             	mov    eax,DWORD PTR [ebx+0x4]
  456def:	3b c7                	cmp    eax,edi
  456df1:	74 0c                	je     0x456dff
  456df3:	50                   	push   eax
  456df4:	e8 87 27 02 00       	call   0x479580
  456df9:	83 c4 04             	add    esp,0x4
  456dfc:	89 7b 04             	mov    DWORD PTR [ebx+0x4],edi
  456dff:	89 7b 0c             	mov    DWORD PTR [ebx+0xc],edi
  456e02:	89 7b 08             	mov    DWORD PTR [ebx+0x8],edi
  456e05:	5f                   	pop    edi
  456e06:	5e                   	pop    esi
  456e07:	5d                   	pop    ebp
  456e08:	5b                   	pop    ebx
  456e09:	c2 08 00             	ret    0x8
  456e0c:	8b 4b 04             	mov    ecx,DWORD PTR [ebx+0x4]
  456e0f:	3b cf                	cmp    ecx,edi
  456e11:	75 27                	jne    0x456e3a
  456e13:	8d 04 ed 00 00 00 00 	lea    eax,[ebp*8+0x0]
  456e1a:	50                   	push   eax
  456e1b:	e8 6c 27 02 00       	call   0x47958c
  456e20:	83 c4 04             	add    esp,0x4
  456e23:	89 43 04             	mov    DWORD PTR [ebx+0x4],eax
  456e26:	55                   	push   ebp
  456e27:	50                   	push   eax
  456e28:	e8 cd c0 fa ff       	call   0x402efa
  456e2d:	5f                   	pop    edi
  456e2e:	89 6b 0c             	mov    DWORD PTR [ebx+0xc],ebp
  456e31:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456e34:	5e                   	pop    esi
  456e35:	5d                   	pop    ebp
  456e36:	5b                   	pop    ebx
  456e37:	c2 08 00             	ret    0x8
  456e3a:	8b 73 0c             	mov    esi,DWORD PTR [ebx+0xc]
  456e3d:	3b ee                	cmp    ebp,esi
  456e3f:	7f 1f                	jg     0x456e60
  456e41:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  456e44:	3b e8                	cmp    ebp,eax
  456e46:	7e 0e                	jle    0x456e56
  456e48:	8b d5                	mov    edx,ebp
  456e4a:	2b d0                	sub    edx,eax
  456e4c:	8d 04 c1             	lea    eax,[ecx+eax*8]
  456e4f:	52                   	push   edx
  456e50:	50                   	push   eax
  456e51:	e8 a4 c0 fa ff       	call   0x402efa
  456e56:	5f                   	pop    edi
  456e57:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456e5a:	5e                   	pop    esi
  456e5b:	5d                   	pop    ebp
  456e5c:	5b                   	pop    ebx
  456e5d:	c2 08 00             	ret    0x8
  456e60:	8b 43 10             	mov    eax,DWORD PTR [ebx+0x10]
  456e63:	3b c7                	cmp    eax,edi
  456e65:	75 24                	jne    0x456e8b
  456e67:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  456e6a:	99                   	cdq
  456e6b:	83 e2 07             	and    edx,0x7
  456e6e:	03 c2                	add    eax,edx
  456e70:	c1 f8 03             	sar    eax,0x3
  456e73:	83 f8 04             	cmp    eax,0x4
  456e76:	7d 07                	jge    0x456e7f
  456e78:	b8 04 00 00 00       	mov    eax,0x4
  456e7d:	eb 0c                	jmp    0x456e8b
  456e7f:	3d 00 04 00 00       	cmp    eax,0x400
  456e84:	7e 05                	jle    0x456e8b
  456e86:	b8 00 04 00 00       	mov    eax,0x400
  456e8b:	03 c6                	add    eax,esi
  456e8d:	3b e8                	cmp    ebp,eax
  456e8f:	89 44 24 18          	mov    DWORD PTR [esp+0x18],eax
  456e93:	7c 04                	jl     0x456e99
  456e95:	89 6c 24 18          	mov    DWORD PTR [esp+0x18],ebp
  456e99:	8b 4c 24 18          	mov    ecx,DWORD PTR [esp+0x18]
  456e9d:	8d 14 cd 00 00 00 00 	lea    edx,[ecx*8+0x0]
  456ea4:	52                   	push   edx
  456ea5:	e8 e2 26 02 00       	call   0x47958c
  456eaa:	8b 4b 08             	mov    ecx,DWORD PTR [ebx+0x8]
  456ead:	8b 73 04             	mov    esi,DWORD PTR [ebx+0x4]
  456eb0:	c1 e1 03             	shl    ecx,0x3
  456eb3:	8b d1                	mov    edx,ecx
  456eb5:	8b f8                	mov    edi,eax
  456eb7:	c1 e9 02             	shr    ecx,0x2
  456eba:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  456ebc:	8b ca                	mov    ecx,edx
  456ebe:	8b d5                	mov    edx,ebp
  456ec0:	83 e1 03             	and    ecx,0x3
  456ec3:	83 c4 04             	add    esp,0x4
  456ec6:	f3 a4                	rep movs BYTE PTR es:[edi],BYTE PTR ds:[esi]
  456ec8:	8b 4b 08             	mov    ecx,DWORD PTR [ebx+0x8]
  456ecb:	89 44 24 14          	mov    DWORD PTR [esp+0x14],eax
  456ecf:	2b d1                	sub    edx,ecx
  456ed1:	8d 04 c8             	lea    eax,[eax+ecx*8]
  456ed4:	52                   	push   edx
  456ed5:	50                   	push   eax
  456ed6:	e8 1f c0 fa ff       	call   0x402efa
  456edb:	8b 4b 04             	mov    ecx,DWORD PTR [ebx+0x4]
  456ede:	51                   	push   ecx
  456edf:	e8 9c 26 02 00       	call   0x479580
  456ee4:	8b 54 24 18          	mov    edx,DWORD PTR [esp+0x18]
  456ee8:	8b 44 24 1c          	mov    eax,DWORD PTR [esp+0x1c]
  456eec:	83 c4 04             	add    esp,0x4
  456eef:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  456ef2:	89 53 04             	mov    DWORD PTR [ebx+0x4],edx
  456ef5:	89 43 0c             	mov    DWORD PTR [ebx+0xc],eax
  456ef8:	5f                   	pop    edi
  456ef9:	5e                   	pop    esi
  456efa:	5d                   	pop    ebp
  456efb:	5b                   	pop    ebx
  456efc:	c2 08 00             	ret    0x8
  456eff:	90                   	nop
  456f00:	cc                   	int3
  456f01:	cc                   	int3
  456f02:	cc                   	int3
  456f03:	cc                   	int3
  456f04:	cc                   	int3
  456f05:	cc                   	int3
  456f06:	cc                   	int3
  456f07:	cc                   	int3
  456f08:	cc                   	int3
  456f09:	cc                   	int3
  456f0a:	cc                   	int3
  456f0b:	cc                   	int3
  456f0c:	cc                   	int3
  456f0d:	cc                   	int3
  456f0e:	cc                   	int3
  456f0f:	cc                   	int3
  456f10:	cc                   	int3
  456f11:	cc                   	int3
  456f12:	cc                   	int3
  456f13:	cc                   	int3
  456f14:	cc                   	int3
  456f15:	cc                   	int3
  456f16:	cc                   	int3
  456f17:	cc                   	int3
  456f18:	cc                   	int3
  456f19:	cc                   	int3
  456f1a:	cc                   	int3
  456f1b:	cc                   	int3
  456f1c:	cc                   	int3
  456f1d:	cc                   	int3
  456f1e:	cc                   	int3
  456f1f:	cc                   	int3
  456f20:	cc                   	int3
  456f21:	cc                   	int3
  456f22:	cc                   	int3
  456f23:	cc                   	int3
  456f24:	cc                   	int3
  456f25:	cc                   	int3
  456f26:	cc                   	int3
  456f27:	cc                   	int3
  456f28:	cc                   	int3
  456f29:	cc                   	int3
  456f2a:	cc                   	int3
  456f2b:	cc                   	int3
  456f2c:	cc                   	int3
  456f2d:	cc                   	int3
  456f2e:	cc                   	int3
  456f2f:	cc                   	int3
  456f30:	cc                   	int3
  456f31:	cc                   	int3
  456f32:	cc                   	int3
  456f33:	cc                   	int3
  456f34:	cc                   	int3
  456f35:	cc                   	int3
  456f36:	cc                   	int3
  456f37:	cc                   	int3
  456f38:	cc                   	int3
  456f39:	cc                   	int3
  456f3a:	cc                   	int3
  456f3b:	cc                   	int3
  456f3c:	cc                   	int3
  456f3d:	cc                   	int3
  456f3e:	cc                   	int3
  456f3f:	cc                   	int3
  456f40:	cc                   	int3
  456f41:	cc                   	int3
  456f42:	cc                   	int3
  456f43:	cc                   	int3
  456f44:	cc                   	int3
  456f45:	cc                   	int3
  456f46:	cc                   	int3
  456f47:	cc                   	int3
  456f48:	cc                   	int3
  456f49:	cc                   	int3
  456f4a:	cc                   	int3
  456f4b:	cc                   	int3
  456f4c:	cc                   	int3
  456f4d:	cc                   	int3
  456f4e:	cc                   	int3
  456f4f:	cc                   	int3
  456f50:	e8 1b 00 00 00       	call   0x456f70
  456f55:	e9 36 00 00 00       	jmp    0x456f90
  456f5a:	90                   	nop
  456f5b:	90                   	nop
  456f5c:	90                   	nop
  456f5d:	90                   	nop
  456f5e:	90                   	nop
  456f5f:	90                   	nop
  456f60:	cc                   	int3
  456f61:	cc                   	int3
  456f62:	cc                   	int3
  456f63:	cc                   	int3
  456f64:	cc                   	int3
  456f65:	cc                   	int3
  456f66:	cc                   	int3
  456f67:	cc                   	int3
  456f68:	cc                   	int3
  456f69:	cc                   	int3
  456f6a:	cc                   	int3
  456f6b:	cc                   	int3
  456f6c:	cc                   	int3
  456f6d:	cc                   	int3
  456f6e:	cc                   	int3
  456f6f:	cc                   	int3
  456f70:	b9 81 7b 49 00       	mov    ecx,0x497b81
  456f75:	ff 25 30 97 49 00    	jmp    DWORD PTR ds:0x499730
  456f7b:	90                   	nop
  456f7c:	90                   	nop
  456f7d:	90                   	nop
  456f7e:	90                   	nop
  456f7f:	90                   	nop
  456f80:	cc                   	int3
  456f81:	cc                   	int3
  456f82:	cc                   	int3
  456f83:	cc                   	int3
  456f84:	cc                   	int3
  456f85:	cc                   	int3
  456f86:	cc                   	int3
  456f87:	cc                   	int3
  456f88:	cc                   	int3
  456f89:	cc                   	int3
  456f8a:	cc                   	int3
  456f8b:	cc                   	int3
  456f8c:	cc                   	int3
  456f8d:	cc                   	int3
  456f8e:	cc                   	int3
  456f8f:	cc                   	int3
  456f90:	68 b0 6f 45 00       	push   0x456fb0
  456f95:	e8 24 2c 02 00       	call   0x479bbe
  456f9a:	59                   	pop    ecx
  456f9b:	c3                   	ret
  456f9c:	90                   	nop
  456f9d:	90                   	nop
  456f9e:	90                   	nop
  456f9f:	90                   	nop
  456fa0:	cc                   	int3
  456fa1:	cc                   	int3
  456fa2:	cc                   	int3
  456fa3:	cc                   	int3
  456fa4:	cc                   	int3
  456fa5:	cc                   	int3
  456fa6:	cc                   	int3
  456fa7:	cc                   	int3
  456fa8:	cc                   	int3
  456fa9:	cc                   	int3
  456faa:	cc                   	int3
  456fab:	cc                   	int3
  456fac:	cc                   	int3
  456fad:	cc                   	int3
  456fae:	cc                   	int3
  456faf:	cc                   	int3
  456fb0:	b9 81 7b 49 00       	mov    ecx,0x497b81
  456fb5:	ff 25 28 97 49 00    	jmp    DWORD PTR ds:0x499728
  456fbb:	90                   	nop
  456fbc:	90                   	nop
  456fbd:	90                   	nop
  456fbe:	90                   	nop
  456fbf:	90                   	nop
  456fc0:	cc                   	int3
  456fc1:	cc                   	int3
  456fc2:	cc                   	int3
  456fc3:	cc                   	int3
  456fc4:	cc                   	int3
  456fc5:	cc                   	int3
  456fc6:	cc                   	int3
  456fc7:	cc                   	int3
  456fc8:	cc                   	int3
  456fc9:	cc                   	int3
  456fca:	cc                   	int3
  456fcb:	cc                   	int3
  456fcc:	cc                   	int3
  456fcd:	cc                   	int3
  456fce:	cc                   	int3
  456fcf:	cc                   	int3
  456fd0:	e8 1b 00 00 00       	call   0x456ff0
  456fd5:	e9 36 00 00 00       	jmp    0x457010
  456fda:	90                   	nop
  456fdb:	90                   	nop
  456fdc:	90                   	nop
  456fdd:	90                   	nop
  456fde:	90                   	nop
  456fdf:	90                   	nop
  456fe0:	cc                   	int3
  456fe1:	cc                   	int3
  456fe2:	cc                   	int3
  456fe3:	cc                   	int3
  456fe4:	cc                   	int3
  456fe5:	cc                   	int3
  456fe6:	cc                   	int3
  456fe7:	cc                   	int3
  456fe8:	cc                   	int3
  456fe9:	cc                   	int3
  456fea:	cc                   	int3
  456feb:	cc                   	int3
  456fec:	cc                   	int3
  456fed:	cc                   	int3
  456fee:	cc                   	int3
  456fef:	cc                   	int3
  456ff0:	b9 80 7b 49 00       	mov    ecx,0x497b80
  456ff5:	ff 25 34 97 49 00    	jmp    DWORD PTR ds:0x499734
  456ffb:	90                   	nop
  456ffc:	90                   	nop
  456ffd:	90                   	nop
  456ffe:	90                   	nop
  456fff:	90                   	nop
  457000:	cc                   	int3
  457001:	cc                   	int3
  457002:	cc                   	int3
  457003:	cc                   	int3
  457004:	cc                   	int3
  457005:	cc                   	int3
  457006:	cc                   	int3
  457007:	cc                   	int3
  457008:	cc                   	int3
  457009:	cc                   	int3
  45700a:	cc                   	int3
  45700b:	cc                   	int3
  45700c:	cc                   	int3
  45700d:	cc                   	int3
  45700e:	cc                   	int3
  45700f:	cc                   	int3
  457010:	68 30 70 45 00       	push   0x457030
  457015:	e8 a4 2b 02 00       	call   0x479bbe
  45701a:	59                   	pop    ecx
  45701b:	c3                   	ret
  45701c:	90                   	nop
  45701d:	90                   	nop
  45701e:	90                   	nop
  45701f:	90                   	nop
  457020:	cc                   	int3
  457021:	cc                   	int3
  457022:	cc                   	int3
  457023:	cc                   	int3
  457024:	cc                   	int3
  457025:	cc                   	int3
  457026:	cc                   	int3
  457027:	cc                   	int3
  457028:	cc                   	int3
  457029:	cc                   	int3
  45702a:	cc                   	int3
  45702b:	cc                   	int3
  45702c:	cc                   	int3
  45702d:	cc                   	int3
  45702e:	cc                   	int3
  45702f:	cc                   	int3
  457030:	b9 80 7b 49 00       	mov    ecx,0x497b80
  457035:	ff 25 2c 97 49 00    	jmp    DWORD PTR ds:0x49972c
  45703b:	90                   	nop
  45703c:	90                   	nop
  45703d:	90                   	nop
  45703e:	90                   	nop
  45703f:	90                   	nop
  457040:	cc                   	int3
  457041:	cc                   	int3
  457042:	cc                   	int3
  457043:	cc                   	int3
  457044:	cc                   	int3
  457045:	cc                   	int3
  457046:	cc                   	int3
  457047:	cc                   	int3
  457048:	cc                   	int3
  457049:	cc                   	int3
  45704a:	cc                   	int3
  45704b:	cc                   	int3
  45704c:	cc                   	int3
  45704d:	cc                   	int3
  45704e:	cc                   	int3
  45704f:	cc                   	int3
  457050:	6a ff                	push   0xffffffff
  457052:	68 b8 f2 47 00       	push   0x47f2b8
  457057:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  45705d:	50                   	push   eax
  45705e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  457065:	51                   	push   ecx
  457066:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  45706a:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  45706e:	55                   	push   ebp
  45706f:	56                   	push   esi
  457070:	8b f1                	mov    esi,ecx
  457072:	57                   	push   edi
  457073:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
  457077:	50                   	push   eax
  457078:	8b 44 24 28          	mov    eax,DWORD PTR [esp+0x28]
  45707c:	51                   	push   ecx
  45707d:	8b 4c 24 28          	mov    ecx,DWORD PTR [esp+0x28]
  457081:	52                   	push   edx
  457082:	50                   	push   eax
  457083:	51                   	push   ecx
  457084:	8b ce                	mov    ecx,esi
  457086:	89 74 24 20          	mov    DWORD PTR [esp+0x20],esi
  45708a:	e8 51 bf fa ff       	call   0x402fe0
  45708f:	8d 8e b8 00 00 00    	lea    ecx,[esi+0xb8]
  457095:	c7 44 24 18 00 00 00 	mov    DWORD PTR [esp+0x18],0x0
  45709c:	00 
  45709d:	e8 f6 24 02 00       	call   0x479598
  4570a2:	8d be c0 00 00 00    	lea    edi,[esi+0xc0]
  4570a8:	c7 47 04 00 00 00 00 	mov    DWORD PTR [edi+0x4],0x0
  4570af:	c7 07 fc 41 48 00    	mov    DWORD PTR [edi],0x4841fc
  4570b5:	c7 86 d0 00 00 00 90 	mov    DWORD PTR [esi+0xd0],0x90
  4570bc:	00 00 00 
  4570bf:	c7 86 d8 00 00 00 e8 	mov    DWORD PTR [esi+0xd8],0xe8
  4570c6:	00 00 00 
  4570c9:	c7 86 d4 00 00 00 27 	mov    DWORD PTR [esi+0xd4],0x27
  4570d0:	00 00 00 
  4570d3:	c7 86 dc 00 00 00 37 	mov    DWORD PTR [esi+0xdc],0x37
  4570da:	00 00 00 
  4570dd:	68 8c 00 00 00       	push   0x8c
  4570e2:	c6 44 24 1c 02       	mov    BYTE PTR [esp+0x1c],0x2
  4570e7:	c7 06 c4 55 48 00    	mov    DWORD PTR [esi],0x4855c4
  4570ed:	c7 86 b4 00 00 00 00 	mov    DWORD PTR [esi+0xb4],0x0
  4570f4:	00 00 00 
  4570f7:	e8 90 24 02 00       	call   0x47958c
  4570fc:	8b e8                	mov    ebp,eax
  4570fe:	83 c4 04             	add    esp,0x4
  457101:	89 6c 24 30          	mov    DWORD PTR [esp+0x30],ebp
  457105:	85 ed                	test   ebp,ebp
  457107:	c6 44 24 18 03       	mov    BYTE PTR [esp+0x18],0x3
  45710c:	74 13                	je     0x457121
  45710e:	8b cd                	mov    ecx,ebp
  457110:	ff 15 80 8e 49 00    	call   DWORD PTR ds:0x498e80
  457116:	c7 45 00 08 31 48 00 	mov    DWORD PTR [ebp+0x0],0x483108
  45711d:	8b cd                	mov    ecx,ebp
  45711f:	eb 02                	jmp    0x457123
  457121:	33 c9                	xor    ecx,ecx
  457123:	8b 56 04             	mov    edx,DWORD PTR [esi+0x4]
  457126:	6a 00                	push   0x0
  457128:	6a 40                	push   0x40
  45712a:	6a 40                	push   0x40
  45712c:	52                   	push   edx
  45712d:	c6 44 24 28 02       	mov    BYTE PTR [esp+0x28],0x2
  457132:	89 8e bc 00 00 00    	mov    DWORD PTR [esi+0xbc],ecx
  457138:	ff 15 78 8e 49 00    	call   DWORD PTR ds:0x498e78
  45713e:	6a 00                	push   0x0
  457140:	8b ce                	mov    ecx,esi
  457142:	e8 53 be fa ff       	call   0x402f9a
  457147:	6a 00                	push   0x0
  457149:	68 80 ea 48 00       	push   0x48ea80
  45714e:	6a 64                	push   0x64
  457150:	8b cf                	mov    ecx,edi
  457152:	e8 11 2a 02 00       	call   0x479b68
  457157:	ff 15 98 98 49 00    	call   DWORD PTR ds:0x499898
  45715d:	68 80 00 00 00       	push   0x80
  457162:	89 86 c8 00 00 00    	mov    DWORD PTR [esi+0xc8],eax
  457168:	e8 1f 24 02 00       	call   0x47958c
  45716d:	8b f8                	mov    edi,eax
  45716f:	83 c4 04             	add    esp,0x4
  457172:	89 7c 24 30          	mov    DWORD PTR [esp+0x30],edi
  457176:	85 ff                	test   edi,edi
  457178:	c6 44 24 18 04       	mov    BYTE PTR [esp+0x18],0x4
  45717d:	74 50                	je     0x4571cf
  45717f:	6a 00                	push   0x0
  457181:	6a 0e                	push   0xe
  457183:	6a 0e                	push   0xe
  457185:	51                   	push   ecx
  457186:	8b cc                	mov    ecx,esp
  457188:	89 64 24 3c          	mov    DWORD PTR [esp+0x3c],esp
  45718c:	68 d4 f0 48 00       	push   0x48f0d4
  457191:	e8 0e 24 02 00       	call   0x4795a4
  457196:	8b 8e d0 00 00 00    	mov    ecx,DWORD PTR [esi+0xd0]
  45719c:	8b 96 d4 00 00 00    	mov    edx,DWORD PTR [esi+0xd4]
  4571a2:	83 ec 10             	sub    esp,0x10
  4571a5:	8b c4                	mov    eax,esp
  4571a7:	6a 00                	push   0x0
  4571a9:	89 08                	mov    DWORD PTR [eax],ecx
  4571ab:	8b 8e d8 00 00 00    	mov    ecx,DWORD PTR [esi+0xd8]
  4571b1:	89 50 04             	mov    DWORD PTR [eax+0x4],edx
  4571b4:	8b 96 dc 00 00 00    	mov    edx,DWORD PTR [esi+0xdc]
  4571ba:	89 48 08             	mov    DWORD PTR [eax+0x8],ecx
  4571bd:	8b cf                	mov    ecx,edi
  4571bf:	89 50 0c             	mov    DWORD PTR [eax+0xc],edx
  4571c2:	a1 a0 52 49 00       	mov    eax,ds:0x4952a0
  4571c7:	50                   	push   eax
  4571c8:	e8 18 c3 fa ff       	call   0x4034e5
  4571cd:	eb 02                	jmp    0x4571d1
  4571cf:	33 c0                	xor    eax,eax
  4571d1:	89 86 cc 00 00 00    	mov    DWORD PTR [esi+0xcc],eax
  4571d7:	c7 40 7c fa 1f 40 00 	mov    DWORD PTR [eax+0x7c],0x401ffa
  4571de:	8b 8e c8 00 00 00    	mov    ecx,DWORD PTR [esi+0xc8]
  4571e4:	51                   	push   ecx
  4571e5:	ff 15 e8 98 49 00    	call   DWORD PTR ds:0x4998e8
  4571eb:	8b 4c 24 10          	mov    ecx,DWORD PTR [esp+0x10]
  4571ef:	89 35 84 7b 49 00    	mov    DWORD PTR ds:0x497b84,esi
  4571f5:	8b c6                	mov    eax,esi
  4571f7:	5f                   	pop    edi
  4571f8:	5e                   	pop    esi
  4571f9:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  457200:	5d                   	pop    ebp
  457201:	83 c4 10             	add    esp,0x10
  457204:	c2 14 00             	ret    0x14
  457207:	90                   	nop
  457208:	90                   	nop
  457209:	90                   	nop
  45720a:	90                   	nop
  45720b:	90                   	nop
  45720c:	90                   	nop
  45720d:	90                   	nop
  45720e:	90                   	nop
  45720f:	90                   	nop
  457210:	cc                   	int3
  457211:	cc                   	int3
  457212:	cc                   	int3
  457213:	cc                   	int3
  457214:	cc                   	int3
  457215:	cc                   	int3
  457216:	cc                   	int3
  457217:	cc                   	int3
  457218:	cc                   	int3
  457219:	cc                   	int3
  45721a:	cc                   	int3
  45721b:	cc                   	int3
  45721c:	cc                   	int3
  45721d:	cc                   	int3
  45721e:	cc                   	int3
  45721f:	cc                   	int3
  457220:	cc                   	int3
  457221:	cc                   	int3
  457222:	cc                   	int3
  457223:	cc                   	int3
  457224:	cc                   	int3
  457225:	cc                   	int3
  457226:	cc                   	int3
  457227:	cc                   	int3
  457228:	cc                   	int3
  457229:	cc                   	int3
  45722a:	cc                   	int3
  45722b:	cc                   	int3
  45722c:	cc                   	int3
  45722d:	cc                   	int3
  45722e:	cc                   	int3
  45722f:	cc                   	int3
  457230:	cc                   	int3
  457231:	cc                   	int3
  457232:	cc                   	int3
  457233:	cc                   	int3
  457234:	cc                   	int3
  457235:	cc                   	int3
  457236:	cc                   	int3
  457237:	cc                   	int3
  457238:	cc                   	int3
  457239:	cc                   	int3
  45723a:	cc                   	int3
  45723b:	cc                   	int3
  45723c:	cc                   	int3
  45723d:	cc                   	int3
  45723e:	cc                   	int3
  45723f:	cc                   	int3
  457240:	cc                   	int3
  457241:	cc                   	int3
  457242:	cc                   	int3
  457243:	cc                   	int3
  457244:	cc                   	int3
  457245:	cc                   	int3
  457246:	cc                   	int3
  457247:	cc                   	int3
  457248:	cc                   	int3
  457249:	cc                   	int3
  45724a:	cc                   	int3
  45724b:	cc                   	int3
  45724c:	cc                   	int3
  45724d:	cc                   	int3
  45724e:	cc                   	int3
  45724f:	cc                   	int3
  457250:	cc                   	int3
  457251:	cc                   	int3
  457252:	cc                   	int3
  457253:	cc                   	int3
  457254:	cc                   	int3
  457255:	cc                   	int3
  457256:	cc                   	int3
  457257:	cc                   	int3
  457258:	cc                   	int3
  457259:	cc                   	int3
  45725a:	cc                   	int3
  45725b:	cc                   	int3
  45725c:	cc                   	int3
  45725d:	cc                   	int3
  45725e:	cc                   	int3
  45725f:	cc                   	int3
  457260:	cc                   	int3
  457261:	cc                   	int3
  457262:	cc                   	int3
  457263:	cc                   	int3
  457264:	cc                   	int3
  457265:	cc                   	int3
  457266:	cc                   	int3
  457267:	cc                   	int3
  457268:	cc                   	int3
  457269:	cc                   	int3
  45726a:	cc                   	int3
  45726b:	cc                   	int3
  45726c:	cc                   	int3
  45726d:	cc                   	int3
  45726e:	cc                   	int3
  45726f:	cc                   	int3
  457270:	cc                   	int3
  457271:	cc                   	int3
  457272:	cc                   	int3
  457273:	cc                   	int3
  457274:	cc                   	int3
  457275:	cc                   	int3
  457276:	cc                   	int3
  457277:	cc                   	int3
  457278:	cc                   	int3
  457279:	cc                   	int3
  45727a:	cc                   	int3
  45727b:	cc                   	int3
  45727c:	cc                   	int3
  45727d:	cc                   	int3
  45727e:	cc                   	int3
  45727f:	cc                   	int3
  457280:	b8 15 00 00 00       	mov    eax,0x15
  457285:	c3                   	ret
  457286:	90                   	nop
  457287:	90                   	nop
  457288:	90                   	nop
  457289:	90                   	nop
  45728a:	90                   	nop
  45728b:	90                   	nop
  45728c:	90                   	nop
  45728d:	90                   	nop
  45728e:	90                   	nop
  45728f:	90                   	nop
  457290:	cc                   	int3
  457291:	cc                   	int3
  457292:	cc                   	int3
  457293:	cc                   	int3
  457294:	cc                   	int3
  457295:	cc                   	int3
  457296:	cc                   	int3
  457297:	cc                   	int3
  457298:	cc                   	int3
  457299:	cc                   	int3
  45729a:	cc                   	int3
  45729b:	cc                   	int3
  45729c:	cc                   	int3
  45729d:	cc                   	int3
  45729e:	cc                   	int3
  45729f:	cc                   	int3
  4572a0:	56                   	push   esi
  4572a1:	8b f1                	mov    esi,ecx
  4572a3:	e8 99 bb fa ff       	call   0x402e41
  4572a8:	f6 44 24 08 01       	test   BYTE PTR [esp+0x8],0x1
  4572ad:	74 09                	je     0x4572b8
  4572af:	56                   	push   esi
  4572b0:	e8 cb 22 02 00       	call   0x479580
  4572b5:	83 c4 04             	add    esp,0x4
  4572b8:	8b c6                	mov    eax,esi
  4572ba:	5e                   	pop    esi
  4572bb:	c2 04 00             	ret    0x4
  4572be:	90                   	nop
  4572bf:	90                   	nop
  4572c0:	cc                   	int3
  4572c1:	cc                   	int3
  4572c2:	cc                   	int3
  4572c3:	cc                   	int3
  4572c4:	cc                   	int3
  4572c5:	cc                   	int3
  4572c6:	cc                   	int3
  4572c7:	cc                   	int3
  4572c8:	cc                   	int3
  4572c9:	cc                   	int3
  4572ca:	cc                   	int3
  4572cb:	cc                   	int3
  4572cc:	cc                   	int3
  4572cd:	cc                   	int3
  4572ce:	cc                   	int3
  4572cf:	cc                   	int3
  4572d0:	6a ff                	push   0xffffffff
  4572d2:	68 0c f3 47 00       	push   0x47f30c
  4572d7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4572dd:	50                   	push   eax
  4572de:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  4572e5:	83 ec 08             	sub    esp,0x8
  4572e8:	56                   	push   esi
  4572e9:	8b f1                	mov    esi,ecx
  4572eb:	57                   	push   edi
  4572ec:	89 74 24 08          	mov    DWORD PTR [esp+0x8],esi
  4572f0:	c7 06 c4 55 48 00    	mov    DWORD PTR [esi],0x4855c4
  4572f6:	8b 8e bc 00 00 00    	mov    ecx,DWORD PTR [esi+0xbc]
  4572fc:	c7 44 24 18 02 00 00 	mov    DWORD PTR [esp+0x18],0x2
  457303:	00 
  457304:	85 c9                	test   ecx,ecx
  457306:	74 07                	je     0x45730f
  457308:	8b 01                	mov    eax,DWORD PTR [ecx]
  45730a:	6a 01                	push   0x1
  45730c:	ff 50 04             	call   DWORD PTR [eax+0x4]
  45730f:	8d be c0 00 00 00    	lea    edi,[esi+0xc0]
  457315:	8b cf                	mov    ecx,edi
  457317:	e8 80 27 02 00       	call   0x479a9c
  45731c:	8b 86 cc 00 00 00    	mov    eax,DWORD PTR [esi+0xcc]
  457322:	85 c0                	test   eax,eax
  457324:	74 22                	je     0x457348
  457326:	55                   	push   ebp
  457327:	75 04                	jne    0x45732d
  457329:	33 ed                	xor    ebp,ebp
  45732b:	eb 03                	jmp    0x457330
  45732d:	8b 68 20             	mov    ebp,DWORD PTR [eax+0x20]
  457330:	ff 15 98 98 49 00    	call   DWORD PTR ds:0x499898
  457336:	3b e8                	cmp    ebp,eax
  457338:	5d                   	pop    ebp
  457339:	75 0d                	jne    0x457348
  45733b:	8b 8e c8 00 00 00    	mov    ecx,DWORD PTR [esi+0xc8]
  457341:	51                   	push   ecx
  457342:	ff 15 e8 98 49 00    	call   DWORD PTR ds:0x4998e8
  457348:	8b 8e cc 00 00 00    	mov    ecx,DWORD PTR [esi+0xcc]
  45734e:	85 c9                	test   ecx,ecx
  457350:	74 11                	je     0x457363
  457352:	8b 11                	mov    edx,DWORD PTR [ecx]
  457354:	6a 01                	push   0x1
  457356:	ff 52 04             	call   DWORD PTR [edx+0x4]
  457359:	c7 86 cc 00 00 00 00 	mov    DWORD PTR [esi+0xcc],0x0
  457360:	00 00 00 
  457363:	c7 05 84 7b 49 00 00 	mov    DWORD PTR ds:0x497b84,0x0
  45736a:	00 00 00 
  45736d:	89 7c 24 0c          	mov    DWORD PTR [esp+0xc],edi
  457371:	c7 07 14 42 48 00    	mov    DWORD PTR [edi],0x484214
  457377:	8b cf                	mov    ecx,edi
  457379:	c6 44 24 18 03       	mov    BYTE PTR [esp+0x18],0x3
  45737e:	e8 19 27 02 00       	call   0x479a9c
  457383:	8d 8e b8 00 00 00    	lea    ecx,[esi+0xb8]
  457389:	c7 07 f0 30 48 00    	mov    DWORD PTR [edi],0x4830f0
  45738f:	c6 44 24 18 00       	mov    BYTE PTR [esp+0x18],0x0
  457394:	e8 ed 21 02 00       	call   0x479586
  457399:	8b ce                	mov    ecx,esi
  45739b:	c7 44 24 18 ff ff ff 	mov    DWORD PTR [esp+0x18],0xffffffff
  4573a2:	ff 
  4573a3:	e8 02 ac fa ff       	call   0x401faa
  4573a8:	8b 4c 24 10          	mov    ecx,DWORD PTR [esp+0x10]
  4573ac:	5f                   	pop    edi
  4573ad:	5e                   	pop    esi
  4573ae:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  4573b5:	83 c4 14             	add    esp,0x14
  4573b8:	c3                   	ret
  4573b9:	90                   	nop
  4573ba:	90                   	nop
  4573bb:	90                   	nop
  4573bc:	90                   	nop
  4573bd:	90                   	nop
  4573be:	90                   	nop
  4573bf:	90                   	nop
  4573c0:	cc                   	int3
  4573c1:	cc                   	int3
  4573c2:	cc                   	int3
  4573c3:	cc                   	int3
  4573c4:	cc                   	int3
  4573c5:	cc                   	int3
  4573c6:	cc                   	int3
  4573c7:	cc                   	int3
  4573c8:	cc                   	int3
  4573c9:	cc                   	int3
  4573ca:	cc                   	int3
  4573cb:	cc                   	int3
  4573cc:	cc                   	int3
  4573cd:	cc                   	int3
  4573ce:	cc                   	int3
  4573cf:	cc                   	int3
  4573d0:	cc                   	int3
  4573d1:	cc                   	int3
  4573d2:	cc                   	int3
  4573d3:	cc                   	int3
  4573d4:	cc                   	int3
  4573d5:	cc                   	int3
  4573d6:	cc                   	int3
  4573d7:	cc                   	int3
  4573d8:	cc                   	int3
  4573d9:	cc                   	int3
  4573da:	cc                   	int3
  4573db:	cc                   	int3
  4573dc:	cc                   	int3
  4573dd:	cc                   	int3
  4573de:	cc                   	int3
  4573df:	cc                   	int3
  4573e0:	cc                   	int3
  4573e1:	cc                   	int3
  4573e2:	cc                   	int3
  4573e3:	cc                   	int3
  4573e4:	cc                   	int3
  4573e5:	cc                   	int3
  4573e6:	cc                   	int3
  4573e7:	cc                   	int3
  4573e8:	cc                   	int3
  4573e9:	cc                   	int3
  4573ea:	cc                   	int3
  4573eb:	cc                   	int3
  4573ec:	cc                   	int3
  4573ed:	cc                   	int3
  4573ee:	cc                   	int3
  4573ef:	cc                   	int3
  4573f0:	cc                   	int3
  4573f1:	cc                   	int3
  4573f2:	cc                   	int3
  4573f3:	cc                   	int3
  4573f4:	cc                   	int3
  4573f5:	cc                   	int3
  4573f6:	cc                   	int3
  4573f7:	cc                   	int3
  4573f8:	cc                   	int3
  4573f9:	cc                   	int3
  4573fa:	cc                   	int3
  4573fb:	cc                   	int3
  4573fc:	cc                   	int3
  4573fd:	cc                   	int3
  4573fe:	cc                   	int3
  4573ff:	cc                   	int3
  457400:	c2 04 00             	ret    0x4
  457403:	90                   	nop
  457404:	90                   	nop
  457405:	90                   	nop
  457406:	90                   	nop
  457407:	90                   	nop
  457408:	90                   	nop
  457409:	90                   	nop
  45740a:	90                   	nop
  45740b:	90                   	nop
  45740c:	90                   	nop
  45740d:	90                   	nop
  45740e:	90                   	nop
  45740f:	90                   	nop
  457410:	cc                   	int3
  457411:	cc                   	int3
  457412:	cc                   	int3
  457413:	cc                   	int3
  457414:	cc                   	int3
  457415:	cc                   	int3
  457416:	cc                   	int3
  457417:	cc                   	int3
  457418:	cc                   	int3
  457419:	cc                   	int3
  45741a:	cc                   	int3
  45741b:	cc                   	int3
  45741c:	cc                   	int3
  45741d:	cc                   	int3
  45741e:	cc                   	int3
  45741f:	cc                   	int3
  457420:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  457424:	56                   	push   esi
  457425:	83 e8 00             	sub    eax,0x0
  457428:	8b f1                	mov    esi,ecx
  45742a:	74 53                	je     0x45747f
  45742c:	48                   	dec    eax
  45742d:	74 39                	je     0x457468
  45742f:	48                   	dec    eax
  457430:	75 54                	jne    0x457486
  457432:	8b 8e a0 00 00 00    	mov    ecx,DWORD PTR [esi+0xa0]
  457438:	6a 01                	push   0x1
  45743a:	6a 28                	push   0x28
  45743c:	8b 01                	mov    eax,DWORD PTR [ecx]
  45743e:	ff 50 04             	call   DWORD PTR [eax+0x4]
  457441:	8b 8e a0 00 00 00    	mov    ecx,DWORD PTR [esi+0xa0]
  457447:	6a 28                	push   0x28
  457449:	e8 4a c2 fa ff       	call   0x403698
  45744e:	85 c0                	test   eax,eax
  457450:	74 34                	je     0x457486
  457452:	8b 8e b4 00 00 00    	mov    ecx,DWORD PTR [esi+0xb4]
  457458:	0f bf 51 04          	movsx  edx,WORD PTR [ecx+0x4]
  45745c:	52                   	push   edx
  45745d:	8b c8                	mov    ecx,eax
  45745f:	e8 2c b1 fa ff       	call   0x402590
  457464:	5e                   	pop    esi
  457465:	c2 04 00             	ret    0x4
  457468:	8b 06                	mov    eax,DWORD PTR [esi]
  45746a:	8b ce                	mov    ecx,esi
  45746c:	ff 50 04             	call   DWORD PTR [eax+0x4]
  45746f:	8b 8e a0 00 00 00    	mov    ecx,DWORD PTR [esi+0xa0]
  457475:	50                   	push   eax
  457476:	e8 fb 9d fa ff       	call   0x401276
  45747b:	5e                   	pop    esi
  45747c:	c2 04 00             	ret    0x4
  45747f:	8b 16                	mov    edx,DWORD PTR [esi]
  457481:	8b ce                	mov    ecx,esi
  457483:	ff 52 44             	call   DWORD PTR [edx+0x44]
  457486:	5e                   	pop    esi
  457487:	c2 04 00             	ret    0x4
  45748a:	90                   	nop
  45748b:	90                   	nop
  45748c:	90                   	nop
  45748d:	90                   	nop
  45748e:	90                   	nop
  45748f:	90                   	nop
  457490:	cc                   	int3
  457491:	cc                   	int3
  457492:	cc                   	int3
  457493:	cc                   	int3
  457494:	cc                   	int3
  457495:	cc                   	int3
  457496:	cc                   	int3
  457497:	cc                   	int3
  457498:	cc                   	int3
  457499:	cc                   	int3
  45749a:	cc                   	int3
  45749b:	cc                   	int3
  45749c:	cc                   	int3
  45749d:	cc                   	int3
  45749e:	cc                   	int3
  45749f:	cc                   	int3
  4574a0:	cc                   	int3
  4574a1:	cc                   	int3
  4574a2:	cc                   	int3
  4574a3:	cc                   	int3
  4574a4:	cc                   	int3
  4574a5:	cc                   	int3
  4574a6:	cc                   	int3
  4574a7:	cc                   	int3
  4574a8:	cc                   	int3
  4574a9:	cc                   	int3
  4574aa:	cc                   	int3
  4574ab:	cc                   	int3
  4574ac:	cc                   	int3
  4574ad:	cc                   	int3
  4574ae:	cc                   	int3
  4574af:	cc                   	int3
  4574b0:	8b 44 24 08          	mov    eax,DWORD PTR [esp+0x8]
  4574b4:	8b 54 24 04          	mov    edx,DWORD PTR [esp+0x4]
  4574b8:	50                   	push   eax
  4574b9:	52                   	push   edx
  4574ba:	e8 d9 a8 fa ff       	call   0x401d98
  4574bf:	c2 08 00             	ret    0x8
  4574c2:	90                   	nop
  4574c3:	90                   	nop
  4574c4:	90                   	nop
  4574c5:	90                   	nop
  4574c6:	90                   	nop
  4574c7:	90                   	nop
  4574c8:	90                   	nop
  4574c9:	90                   	nop
  4574ca:	90                   	nop
  4574cb:	90                   	nop
  4574cc:	90                   	nop
  4574cd:	90                   	nop
  4574ce:	90                   	nop
  4574cf:	90                   	nop
  4574d0:	cc                   	int3
  4574d1:	cc                   	int3
  4574d2:	cc                   	int3
  4574d3:	cc                   	int3
  4574d4:	cc                   	int3
  4574d5:	cc                   	int3
  4574d6:	cc                   	int3
  4574d7:	cc                   	int3
  4574d8:	cc                   	int3
  4574d9:	cc                   	int3
  4574da:	cc                   	int3
  4574db:	cc                   	int3
  4574dc:	cc                   	int3
  4574dd:	cc                   	int3
  4574de:	cc                   	int3
  4574df:	cc                   	int3
  4574e0:	8b 44 24 08          	mov    eax,DWORD PTR [esp+0x8]
  4574e4:	8b 54 24 04          	mov    edx,DWORD PTR [esp+0x4]
  4574e8:	50                   	push   eax
  4574e9:	52                   	push   edx
  4574ea:	e8 6d c1 fa ff       	call   0x40365c
  4574ef:	c2 08 00             	ret    0x8
  4574f2:	90                   	nop
  4574f3:	90                   	nop
  4574f4:	90                   	nop
  4574f5:	90                   	nop
  4574f6:	90                   	nop
  4574f7:	90                   	nop
  4574f8:	90                   	nop
  4574f9:	90                   	nop
  4574fa:	90                   	nop
  4574fb:	90                   	nop
  4574fc:	90                   	nop
  4574fd:	90                   	nop
  4574fe:	90                   	nop
  4574ff:	90                   	nop
  457500:	cc                   	int3
  457501:	cc                   	int3
  457502:	cc                   	int3
  457503:	cc                   	int3
  457504:	cc                   	int3
  457505:	cc                   	int3
  457506:	cc                   	int3
  457507:	cc                   	int3
  457508:	cc                   	int3
  457509:	cc                   	int3
  45750a:	cc                   	int3
  45750b:	cc                   	int3
  45750c:	cc                   	int3
  45750d:	cc                   	int3
  45750e:	cc                   	int3
  45750f:	cc                   	int3
  457510:	83 ec 10             	sub    esp,0x10
  457513:	53                   	push   ebx
  457514:	55                   	push   ebp
  457515:	56                   	push   esi
  457516:	8b f1                	mov    esi,ecx
  457518:	57                   	push   edi
  457519:	8b 7c 24 24          	mov    edi,DWORD PTR [esp+0x24]
  45751d:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
