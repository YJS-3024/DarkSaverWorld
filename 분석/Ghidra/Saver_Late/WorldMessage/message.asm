
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

00467b60 <.text+0x66b60>:
  467b60:	6a ff                	push   0xffffffff
  467b62:	68 98 02 48 00       	push   0x480298
  467b67:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  467b6d:	50                   	push   eax
  467b6e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  467b75:	51                   	push   ecx
  467b76:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  467b7a:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  467b7e:	56                   	push   esi
  467b7f:	8b f1                	mov    esi,ecx
  467b81:	8b 4c 24 24          	mov    ecx,DWORD PTR [esp+0x24]
  467b85:	50                   	push   eax
  467b86:	8b 44 24 20          	mov    eax,DWORD PTR [esp+0x20]
  467b8a:	51                   	push   ecx
  467b8b:	8b 4c 24 20          	mov    ecx,DWORD PTR [esp+0x20]
  467b8f:	52                   	push   edx
  467b90:	50                   	push   eax
  467b91:	51                   	push   ecx
  467b92:	8b ce                	mov    ecx,esi
  467b94:	89 74 24 18          	mov    DWORD PTR [esp+0x18],esi
  467b98:	e8 43 b4 f9 ff       	call   0x402fe0
  467b9d:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  467ba3:	c7 44 24 10 00 00 00 	mov    DWORD PTR [esp+0x10],0x0
  467baa:	00 
  467bab:	e8 e8 19 01 00       	call   0x479598
  467bb0:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  467bb4:	c7 06 ec 5e 48 00    	mov    DWORD PTR [esi],0x485eec
  467bba:	c7 86 90 00 00 00 00 	mov    DWORD PTR [esi+0x90],0x0
  467bc1:	00 00 00 
  467bc4:	89 35 04 7c 49 00    	mov    DWORD PTR ds:0x497c04,esi
  467bca:	c7 86 b8 00 00 00 00 	mov    DWORD PTR [esi+0xb8],0x0
  467bd1:	00 00 00 
  467bd4:	8b c6                	mov    eax,esi
  467bd6:	5e                   	pop    esi
  467bd7:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  467bde:	83 c4 10             	add    esp,0x10
  467be1:	c2 14 00             	ret    0x14
  467be4:	90                   	nop
  467be5:	90                   	nop
  467be6:	90                   	nop
  467be7:	90                   	nop
  467be8:	90                   	nop
  467be9:	90                   	nop
  467bea:	90                   	nop
  467beb:	90                   	nop
  467bec:	90                   	nop
  467bed:	90                   	nop
  467bee:	90                   	nop
  467bef:	90                   	nop
  467bf0:	cc                   	int3
  467bf1:	cc                   	int3
  467bf2:	cc                   	int3
  467bf3:	cc                   	int3
  467bf4:	cc                   	int3
  467bf5:	cc                   	int3
  467bf6:	cc                   	int3
  467bf7:	cc                   	int3
  467bf8:	cc                   	int3
  467bf9:	cc                   	int3
  467bfa:	cc                   	int3
  467bfb:	cc                   	int3
  467bfc:	cc                   	int3
  467bfd:	cc                   	int3
  467bfe:	cc                   	int3
  467bff:	cc                   	int3
  467c00:	cc                   	int3
  467c01:	cc                   	int3
  467c02:	cc                   	int3
  467c03:	cc                   	int3
  467c04:	cc                   	int3
  467c05:	cc                   	int3
  467c06:	cc                   	int3
  467c07:	cc                   	int3
  467c08:	cc                   	int3
  467c09:	cc                   	int3
  467c0a:	cc                   	int3
  467c0b:	cc                   	int3
  467c0c:	cc                   	int3
  467c0d:	cc                   	int3
  467c0e:	cc                   	int3
  467c0f:	cc                   	int3
  467c10:	cc                   	int3
  467c11:	cc                   	int3
  467c12:	cc                   	int3
  467c13:	cc                   	int3
  467c14:	cc                   	int3
  467c15:	cc                   	int3
  467c16:	cc                   	int3
  467c17:	cc                   	int3
  467c18:	cc                   	int3
  467c19:	cc                   	int3
  467c1a:	cc                   	int3
  467c1b:	cc                   	int3
  467c1c:	cc                   	int3
  467c1d:	cc                   	int3
  467c1e:	cc                   	int3
  467c1f:	cc                   	int3
  467c20:	b8 2c 00 00 00       	mov    eax,0x2c
  467c25:	c3                   	ret
  467c26:	90                   	nop
  467c27:	90                   	nop
  467c28:	90                   	nop
  467c29:	90                   	nop
  467c2a:	90                   	nop
  467c2b:	90                   	nop
  467c2c:	90                   	nop
  467c2d:	90                   	nop
  467c2e:	90                   	nop
  467c2f:	90                   	nop
  467c30:	cc                   	int3
  467c31:	cc                   	int3
  467c32:	cc                   	int3
  467c33:	cc                   	int3
  467c34:	cc                   	int3
  467c35:	cc                   	int3
  467c36:	cc                   	int3
  467c37:	cc                   	int3
  467c38:	cc                   	int3
  467c39:	cc                   	int3
  467c3a:	cc                   	int3
  467c3b:	cc                   	int3
  467c3c:	cc                   	int3
  467c3d:	cc                   	int3
  467c3e:	cc                   	int3
  467c3f:	cc                   	int3
  467c40:	56                   	push   esi
  467c41:	8b f1                	mov    esi,ecx
  467c43:	e8 4c af f9 ff       	call   0x402b94
  467c48:	f6 44 24 08 01       	test   BYTE PTR [esp+0x8],0x1
  467c4d:	74 09                	je     0x467c58
  467c4f:	56                   	push   esi
  467c50:	e8 2b 19 01 00       	call   0x479580
  467c55:	83 c4 04             	add    esp,0x4
  467c58:	8b c6                	mov    eax,esi
  467c5a:	5e                   	pop    esi
  467c5b:	c2 04 00             	ret    0x4
  467c5e:	90                   	nop
  467c5f:	90                   	nop
  467c60:	cc                   	int3
  467c61:	cc                   	int3
  467c62:	cc                   	int3
  467c63:	cc                   	int3
  467c64:	cc                   	int3
  467c65:	cc                   	int3
  467c66:	cc                   	int3
  467c67:	cc                   	int3
  467c68:	cc                   	int3
  467c69:	cc                   	int3
  467c6a:	cc                   	int3
  467c6b:	cc                   	int3
  467c6c:	cc                   	int3
  467c6d:	cc                   	int3
  467c6e:	cc                   	int3
  467c6f:	cc                   	int3
  467c70:	6a ff                	push   0xffffffff
  467c72:	68 b8 02 48 00       	push   0x4802b8
  467c77:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  467c7d:	50                   	push   eax
  467c7e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  467c85:	51                   	push   ecx
  467c86:	56                   	push   esi
  467c87:	8b f1                	mov    esi,ecx
  467c89:	89 74 24 04          	mov    DWORD PTR [esp+0x4],esi
  467c8d:	c7 06 ec 5e 48 00    	mov    DWORD PTR [esi],0x485eec
  467c93:	33 c0                	xor    eax,eax
  467c95:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  467c9b:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  467c9f:	a3 04 7c 49 00       	mov    ds:0x497c04,eax
  467ca4:	e8 dd 18 01 00       	call   0x479586
  467ca9:	8b ce                	mov    ecx,esi
  467cab:	c7 44 24 10 ff ff ff 	mov    DWORD PTR [esp+0x10],0xffffffff
  467cb2:	ff 
  467cb3:	e8 f2 a2 f9 ff       	call   0x401faa
  467cb8:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  467cbc:	5e                   	pop    esi
  467cbd:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  467cc4:	83 c4 10             	add    esp,0x10
  467cc7:	c3                   	ret
  467cc8:	90                   	nop
  467cc9:	90                   	nop
  467cca:	90                   	nop
  467ccb:	90                   	nop
  467ccc:	90                   	nop
  467ccd:	90                   	nop
  467cce:	90                   	nop
  467ccf:	90                   	nop
  467cd0:	cc                   	int3
  467cd1:	cc                   	int3
  467cd2:	cc                   	int3
  467cd3:	cc                   	int3
  467cd4:	cc                   	int3
  467cd5:	cc                   	int3
  467cd6:	cc                   	int3
  467cd7:	cc                   	int3
  467cd8:	cc                   	int3
  467cd9:	cc                   	int3
  467cda:	cc                   	int3
  467cdb:	cc                   	int3
  467cdc:	cc                   	int3
  467cdd:	cc                   	int3
  467cde:	cc                   	int3
  467cdf:	cc                   	int3
  467ce0:	cc                   	int3
  467ce1:	cc                   	int3
  467ce2:	cc                   	int3
  467ce3:	cc                   	int3
  467ce4:	cc                   	int3
  467ce5:	cc                   	int3
  467ce6:	cc                   	int3
  467ce7:	cc                   	int3
  467ce8:	cc                   	int3
  467ce9:	cc                   	int3
  467cea:	cc                   	int3
  467ceb:	cc                   	int3
  467cec:	cc                   	int3
  467ced:	cc                   	int3
  467cee:	cc                   	int3
  467cef:	cc                   	int3
  467cf0:	e9 cf ab f9 ff       	jmp    0x4028c4
  467cf5:	90                   	nop
  467cf6:	90                   	nop
  467cf7:	90                   	nop
  467cf8:	90                   	nop
  467cf9:	90                   	nop
  467cfa:	90                   	nop
  467cfb:	90                   	nop
  467cfc:	90                   	nop
  467cfd:	90                   	nop
  467cfe:	90                   	nop
  467cff:	90                   	nop
  467d00:	cc                   	int3
  467d01:	cc                   	int3
  467d02:	cc                   	int3
  467d03:	cc                   	int3
  467d04:	cc                   	int3
  467d05:	cc                   	int3
  467d06:	cc                   	int3
  467d07:	cc                   	int3
  467d08:	cc                   	int3
  467d09:	cc                   	int3
  467d0a:	cc                   	int3
  467d0b:	cc                   	int3
  467d0c:	cc                   	int3
  467d0d:	cc                   	int3
  467d0e:	cc                   	int3
  467d0f:	cc                   	int3
  467d10:	81 ec 8c 00 00 00    	sub    esp,0x8c
  467d16:	53                   	push   ebx
  467d17:	55                   	push   ebp
  467d18:	56                   	push   esi
  467d19:	8b f1                	mov    esi,ecx
  467d1b:	57                   	push   edi
  467d1c:	8b bc 24 a0 00 00 00 	mov    edi,DWORD PTR [esp+0xa0]
  467d23:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  467d26:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  467d29:	8d 98 20 01 00 00    	lea    ebx,[eax+0x120]
  467d2f:	8d 44 24 20          	lea    eax,[esp+0x20]
  467d33:	8d 69 23             	lea    ebp,[ecx+0x23]
  467d36:	50                   	push   eax
  467d37:	8b cf                	mov    ecx,edi
  467d39:	ff 15 38 8f 49 00    	call   DWORD PTR ds:0x498f38
  467d3f:	8b cf                	mov    ecx,edi
  467d41:	ff 15 04 8f 49 00    	call   DWORD PTR ds:0x498f04
  467d47:	89 44 24 44          	mov    DWORD PTR [esp+0x44],eax
  467d4b:	8b 86 b8 00 00 00    	mov    eax,DWORD PTR [esi+0xb8]
  467d51:	85 c0                	test   eax,eax
  467d53:	75 61                	jne    0x467db6
  467d55:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  467d5b:	8b 96 b4 00 00 00    	mov    edx,DWORD PTR [esi+0xb4]
  467d61:	6a 06                	push   0x6
  467d63:	83 38 00             	cmp    DWORD PTR [eax],0x0
  467d66:	74 27                	je     0x467d8f
  467d68:	83 ec 7c             	sub    esp,0x7c
  467d6b:	b9 1f 00 00 00       	mov    ecx,0x1f
  467d70:	8d b4 24 a0 00 00 00 	lea    esi,[esp+0xa0]
  467d77:	8b fc                	mov    edi,esp
  467d79:	68 ff 00 00 00       	push   0xff
  467d7e:	52                   	push   edx
  467d7f:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  467d81:	55                   	push   ebp
  467d82:	53                   	push   ebx
  467d83:	8b c8                	mov    ecx,eax
  467d85:	e8 9a b9 f9 ff       	call   0x403724
  467d8a:	e9 10 01 00 00       	jmp    0x467e9f
  467d8f:	83 ec 7c             	sub    esp,0x7c
  467d92:	b9 1f 00 00 00       	mov    ecx,0x1f
  467d97:	8d b4 24 a0 00 00 00 	lea    esi,[esp+0xa0]
  467d9e:	8b fc                	mov    edi,esp
  467da0:	68 ff ff 00 00       	push   0xffff
  467da5:	52                   	push   edx
  467da6:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  467da8:	55                   	push   ebp
  467da9:	53                   	push   ebx
  467daa:	8b c8                	mov    ecx,eax
  467dac:	e8 d9 b2 f9 ff       	call   0x40308a
  467db1:	e9 e9 00 00 00       	jmp    0x467e9f
  467db6:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  467db9:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  467dbc:	51                   	push   ecx
  467dbd:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  467dc1:	50                   	push   eax
  467dc2:	51                   	push   ecx
  467dc3:	c7 44 24 1c 14 00 00 	mov    DWORD PTR [esp+0x1c],0x14
  467dca:	00 
  467dcb:	c7 44 24 24 1e 02 00 	mov    DWORD PTR [esp+0x24],0x21e
  467dd2:	00 
  467dd3:	c7 44 24 20 0a 00 00 	mov    DWORD PTR [esp+0x20],0xa
  467dda:	00 
  467ddb:	c7 44 24 28 4a 00 00 	mov    DWORD PTR [esp+0x28],0x4a
  467de2:	00 
  467de3:	ff 15 fc 98 49 00    	call   DWORD PTR ds:0x4998fc
  467de9:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  467def:	8b 96 b4 00 00 00    	mov    edx,DWORD PTR [esi+0xb4]
  467df5:	6a 01                	push   0x1
  467df7:	6a 02                	push   0x2
  467df9:	8b 08                	mov    ecx,DWORD PTR [eax]
  467dfb:	6a ff                	push   0xffffffff
  467dfd:	85 c9                	test   ecx,ecx
  467dff:	6a 00                	push   0x0
  467e01:	6a 00                	push   0x0
  467e03:	74 4e                	je     0x467e53
  467e05:	83 ec 7c             	sub    esp,0x7c
  467e08:	b9 1f 00 00 00       	mov    ecx,0x1f
  467e0d:	8b fc                	mov    edi,esp
  467e0f:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  467e16:	68 ff 00 00 00       	push   0xff
  467e1b:	52                   	push   edx
  467e1c:	83 ec 10             	sub    esp,0x10
  467e1f:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  467e21:	8b 8c 24 b8 00 00 00 	mov    ecx,DWORD PTR [esp+0xb8]
  467e28:	8b d4                	mov    edx,esp
  467e2a:	89 0a                	mov    DWORD PTR [edx],ecx
  467e2c:	8b 8c 24 bc 00 00 00 	mov    ecx,DWORD PTR [esp+0xbc]
  467e33:	89 4a 04             	mov    DWORD PTR [edx+0x4],ecx
  467e36:	8b 8c 24 c0 00 00 00 	mov    ecx,DWORD PTR [esp+0xc0]
  467e3d:	89 4a 08             	mov    DWORD PTR [edx+0x8],ecx
  467e40:	8b 8c 24 c4 00 00 00 	mov    ecx,DWORD PTR [esp+0xc4]
  467e47:	89 4a 0c             	mov    DWORD PTR [edx+0xc],ecx
  467e4a:	8b c8                	mov    ecx,eax
  467e4c:	e8 9d b7 f9 ff       	call   0x4035ee
  467e51:	eb 4c                	jmp    0x467e9f
  467e53:	83 ec 7c             	sub    esp,0x7c
  467e56:	b9 1f 00 00 00       	mov    ecx,0x1f
  467e5b:	8b fc                	mov    edi,esp
  467e5d:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  467e64:	68 ff ff 00 00       	push   0xffff
  467e69:	52                   	push   edx
  467e6a:	83 ec 10             	sub    esp,0x10
  467e6d:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  467e6f:	8b 8c 24 b8 00 00 00 	mov    ecx,DWORD PTR [esp+0xb8]
  467e76:	8b d4                	mov    edx,esp
  467e78:	89 0a                	mov    DWORD PTR [edx],ecx
  467e7a:	8b 8c 24 bc 00 00 00 	mov    ecx,DWORD PTR [esp+0xbc]
  467e81:	89 4a 04             	mov    DWORD PTR [edx+0x4],ecx
  467e84:	8b 8c 24 c0 00 00 00 	mov    ecx,DWORD PTR [esp+0xc0]
  467e8b:	89 4a 08             	mov    DWORD PTR [edx+0x8],ecx
  467e8e:	8b 8c 24 c4 00 00 00 	mov    ecx,DWORD PTR [esp+0xc4]
  467e95:	89 4a 0c             	mov    DWORD PTR [edx+0xc],ecx
  467e98:	8b c8                	mov    ecx,eax
  467e9a:	e8 a9 99 f9 ff       	call   0x401848
  467e9f:	8b 8c 24 a0 00 00 00 	mov    ecx,DWORD PTR [esp+0xa0]
  467ea6:	ff 15 00 8f 49 00    	call   DWORD PTR ds:0x498f00
  467eac:	5f                   	pop    edi
  467ead:	5e                   	pop    esi
  467eae:	5d                   	pop    ebp
  467eaf:	b8 01 00 00 00       	mov    eax,0x1
  467eb4:	5b                   	pop    ebx
  467eb5:	81 c4 8c 00 00 00    	add    esp,0x8c
  467ebb:	c2 04 00             	ret    0x4
  467ebe:	90                   	nop
  467ebf:	90                   	nop
  467ec0:	cc                   	int3
  467ec1:	cc                   	int3
  467ec2:	cc                   	int3
  467ec3:	cc                   	int3
  467ec4:	cc                   	int3
  467ec5:	cc                   	int3
  467ec6:	cc                   	int3
  467ec7:	cc                   	int3
  467ec8:	cc                   	int3
  467ec9:	cc                   	int3
  467eca:	cc                   	int3
  467ecb:	cc                   	int3
  467ecc:	cc                   	int3
  467ecd:	cc                   	int3
  467ece:	cc                   	int3
  467ecf:	cc                   	int3
  467ed0:	cc                   	int3
  467ed1:	cc                   	int3
  467ed2:	cc                   	int3
  467ed3:	cc                   	int3
  467ed4:	cc                   	int3
  467ed5:	cc                   	int3
  467ed6:	cc                   	int3
  467ed7:	cc                   	int3
  467ed8:	cc                   	int3
  467ed9:	cc                   	int3
  467eda:	cc                   	int3
  467edb:	cc                   	int3
  467edc:	cc                   	int3
  467edd:	cc                   	int3
  467ede:	cc                   	int3
  467edf:	cc                   	int3
  467ee0:	cc                   	int3
  467ee1:	cc                   	int3
  467ee2:	cc                   	int3
  467ee3:	cc                   	int3
  467ee4:	cc                   	int3
  467ee5:	cc                   	int3
  467ee6:	cc                   	int3
  467ee7:	cc                   	int3
  467ee8:	cc                   	int3
  467ee9:	cc                   	int3
  467eea:	cc                   	int3
  467eeb:	cc                   	int3
  467eec:	cc                   	int3
  467eed:	cc                   	int3
  467eee:	cc                   	int3
  467eef:	cc                   	int3
  467ef0:	cc                   	int3
  467ef1:	cc                   	int3
  467ef2:	cc                   	int3
  467ef3:	cc                   	int3
  467ef4:	cc                   	int3
  467ef5:	cc                   	int3
  467ef6:	cc                   	int3
  467ef7:	cc                   	int3
  467ef8:	cc                   	int3
  467ef9:	cc                   	int3
  467efa:	cc                   	int3
  467efb:	cc                   	int3
  467efc:	cc                   	int3
  467efd:	cc                   	int3
  467efe:	cc                   	int3
  467eff:	cc                   	int3
  467f00:	cc                   	int3
  467f01:	cc                   	int3
  467f02:	cc                   	int3
  467f03:	cc                   	int3
  467f04:	cc                   	int3
  467f05:	cc                   	int3
  467f06:	cc                   	int3
  467f07:	cc                   	int3
  467f08:	cc                   	int3
  467f09:	cc                   	int3
  467f0a:	cc                   	int3
  467f0b:	cc                   	int3
  467f0c:	cc                   	int3
  467f0d:	cc                   	int3
  467f0e:	cc                   	int3
  467f0f:	cc                   	int3
  467f10:	cc                   	int3
  467f11:	cc                   	int3
  467f12:	cc                   	int3
  467f13:	cc                   	int3
  467f14:	cc                   	int3
  467f15:	cc                   	int3
  467f16:	cc                   	int3
  467f17:	cc                   	int3
  467f18:	cc                   	int3
  467f19:	cc                   	int3
  467f1a:	cc                   	int3
  467f1b:	cc                   	int3
  467f1c:	cc                   	int3
  467f1d:	cc                   	int3
  467f1e:	cc                   	int3
  467f1f:	cc                   	int3
  467f20:	cc                   	int3
  467f21:	cc                   	int3
  467f22:	cc                   	int3
  467f23:	cc                   	int3
  467f24:	cc                   	int3
  467f25:	cc                   	int3
  467f26:	cc                   	int3
  467f27:	cc                   	int3
  467f28:	cc                   	int3
  467f29:	cc                   	int3
  467f2a:	cc                   	int3
  467f2b:	cc                   	int3
  467f2c:	cc                   	int3
  467f2d:	cc                   	int3
  467f2e:	cc                   	int3
  467f2f:	cc                   	int3
  467f30:	8b 0d 04 7c 49 00    	mov    ecx,DWORD PTR ds:0x497c04
  467f36:	85 c9                	test   ecx,ecx
  467f38:	74 27                	je     0x467f61
  467f3a:	8b 01                	mov    eax,DWORD PTR [ecx]
  467f3c:	ff 50 04             	call   DWORD PTR [eax+0x4]
  467f3f:	8b 0d 04 7c 49 00    	mov    ecx,DWORD PTR ds:0x497c04
  467f45:	50                   	push   eax
  467f46:	8b 89 a0 00 00 00    	mov    ecx,DWORD PTR [ecx+0xa0]
  467f4c:	e8 25 93 f9 ff       	call   0x401276
  467f51:	c7 05 04 7c 49 00 00 	mov    DWORD PTR ds:0x497c04,0x0
  467f58:	00 00 00 
  467f5b:	b8 01 00 00 00       	mov    eax,0x1
  467f60:	c3                   	ret
  467f61:	33 c0                	xor    eax,eax
  467f63:	c3                   	ret
  467f64:	90                   	nop
  467f65:	90                   	nop
  467f66:	90                   	nop
  467f67:	90                   	nop
  467f68:	90                   	nop
  467f69:	90                   	nop
  467f6a:	90                   	nop
  467f6b:	90                   	nop
  467f6c:	90                   	nop
  467f6d:	90                   	nop
  467f6e:	90                   	nop
  467f6f:	90                   	nop
  467f70:	cc                   	int3
  467f71:	cc                   	int3
  467f72:	cc                   	int3
  467f73:	cc                   	int3
  467f74:	cc                   	int3
  467f75:	cc                   	int3
  467f76:	cc                   	int3
  467f77:	cc                   	int3
  467f78:	cc                   	int3
  467f79:	cc                   	int3
  467f7a:	cc                   	int3
  467f7b:	cc                   	int3
  467f7c:	cc                   	int3
  467f7d:	cc                   	int3
  467f7e:	cc                   	int3
  467f7f:	cc                   	int3
  467f80:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  467f84:	56                   	push   esi
  467f85:	8b f1                	mov    esi,ecx
  467f87:	50                   	push   eax
  467f88:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  467f8e:	e8 47 16 01 00       	call   0x4795da
  467f93:	8b 4c 24 0c          	mov    ecx,DWORD PTR [esp+0xc]
  467f97:	89 8e b8 00 00 00    	mov    DWORD PTR [esi+0xb8],ecx
  467f9d:	5e                   	pop    esi
  467f9e:	c2 08 00             	ret    0x8
  467fa1:	90                   	nop
  467fa2:	90                   	nop
  467fa3:	90                   	nop
  467fa4:	90                   	nop
  467fa5:	90                   	nop
  467fa6:	90                   	nop
  467fa7:	90                   	nop
  467fa8:	90                   	nop
  467fa9:	90                   	nop
  467faa:	90                   	nop
  467fab:	90                   	nop
  467fac:	90                   	nop
  467fad:	90                   	nop
  467fae:	90                   	nop
  467faf:	90                   	nop
  467fb0:	cc                   	int3
  467fb1:	cc                   	int3
  467fb2:	cc                   	int3
  467fb3:	cc                   	int3
  467fb4:	cc                   	int3
  467fb5:	cc                   	int3
  467fb6:	cc                   	int3
  467fb7:	cc                   	int3
  467fb8:	cc                   	int3
  467fb9:	cc                   	int3
  467fba:	cc                   	int3
  467fbb:	cc                   	int3
  467fbc:	cc                   	int3
  467fbd:	cc                   	int3
  467fbe:	cc                   	int3
  467fbf:	cc                   	int3
  467fc0:	e8 1b 00 00 00       	call   0x467fe0
  467fc5:	e9 36 00 00 00       	jmp    0x468000
  467fca:	90                   	nop
  467fcb:	90                   	nop
  467fcc:	90                   	nop
  467fcd:	90                   	nop
  467fce:	90                   	nop
  467fcf:	90                   	nop
  467fd0:	cc                   	int3
  467fd1:	cc                   	int3
  467fd2:	cc                   	int3
  467fd3:	cc                   	int3
  467fd4:	cc                   	int3
  467fd5:	cc                   	int3
  467fd6:	cc                   	int3
  467fd7:	cc                   	int3
  467fd8:	cc                   	int3
  467fd9:	cc                   	int3
  467fda:	cc                   	int3
  467fdb:	cc                   	int3
  467fdc:	cc                   	int3
  467fdd:	cc                   	int3
  467fde:	cc                   	int3
  467fdf:	cc                   	int3
  467fe0:	b9 0d 7c 49 00       	mov    ecx,0x497c0d
  467fe5:	ff 25 30 97 49 00    	jmp    DWORD PTR ds:0x499730
  467feb:	90                   	nop
  467fec:	90                   	nop
  467fed:	90                   	nop
  467fee:	90                   	nop
  467fef:	90                   	nop
  467ff0:	cc                   	int3
  467ff1:	cc                   	int3
  467ff2:	cc                   	int3
  467ff3:	cc                   	int3
  467ff4:	cc                   	int3
  467ff5:	cc                   	int3
  467ff6:	cc                   	int3
  467ff7:	cc                   	int3
  467ff8:	cc                   	int3
  467ff9:	cc                   	int3
  467ffa:	cc                   	int3
  467ffb:	cc                   	int3
  467ffc:	cc                   	int3
  467ffd:	cc                   	int3
  467ffe:	cc                   	int3
  467fff:	cc                   	int3
  468000:	68 20 80 46 00       	push   0x468020
  468005:	e8 b4 1b 01 00       	call   0x479bbe
  46800a:	59                   	pop    ecx
  46800b:	c3                   	ret
  46800c:	90                   	nop
  46800d:	90                   	nop
  46800e:	90                   	nop
  46800f:	90                   	nop
  468010:	cc                   	int3
  468011:	cc                   	int3
  468012:	cc                   	int3
  468013:	cc                   	int3
  468014:	cc                   	int3
  468015:	cc                   	int3
  468016:	cc                   	int3
  468017:	cc                   	int3
  468018:	cc                   	int3
  468019:	cc                   	int3
  46801a:	cc                   	int3
  46801b:	cc                   	int3
  46801c:	cc                   	int3
  46801d:	cc                   	int3
  46801e:	cc                   	int3
  46801f:	cc                   	int3
