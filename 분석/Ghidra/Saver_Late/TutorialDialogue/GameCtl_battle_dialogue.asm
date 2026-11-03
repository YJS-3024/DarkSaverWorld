
GameCtl.dll [SHA256 f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0]:     file format pei-i386


Disassembly of section .text:

10008a00 <.text+0x7a00>:
10008a00:	25 00 00 00 00       	and    eax,0x0
10008a05:	56                   	push   esi
10008a06:	8b f1                	mov    esi,ecx
10008a08:	8b 44 24 14          	mov    eax,DWORD PTR [esp+0x14]
10008a0c:	c7 44 24 0c 00 00 00 	mov    DWORD PTR [esp+0xc],0x0
10008a13:	00 
10008a14:	83 f8 01             	cmp    eax,0x1
10008a17:	c7 46 08 28 00 00 00 	mov    DWORD PTR [esi+0x8],0x28
10008a1e:	75 09                	jne    0x10008a29
10008a20:	c7 46 0c 32 00 00 00 	mov    DWORD PTR [esi+0xc],0x32
10008a27:	eb 07                	jmp    0x10008a30
10008a29:	c7 46 0c 2c 01 00 00 	mov    DWORD PTR [esi+0xc],0x12c
10008a30:	51                   	push   ecx
10008a31:	8d 44 24 20          	lea    eax,[esp+0x20]
10008a35:	8b cc                	mov    ecx,esp
10008a37:	89 64 24 18          	mov    DWORD PTR [esp+0x18],esp
10008a3b:	50                   	push   eax
10008a3c:	c7 46 10 31 02 00 00 	mov    DWORD PTR [esi+0x10],0x231
10008a43:	c7 46 14 68 00 00 00 	mov    DWORD PTR [esi+0x14],0x68
10008a4a:	e8 13 bd 00 00       	call   0x10014762
10008a4f:	8b ce                	mov    ecx,esi
10008a51:	e8 ea fd ff ff       	call   0x10008840
10008a56:	8b 4c 24 18          	mov    ecx,DWORD PTR [esp+0x18]
10008a5a:	89 86 b4 01 00 00    	mov    DWORD PTR [esi+0x1b4],eax
10008a60:	c7 86 b0 01 00 00 00 	mov    DWORD PTR [esi+0x1b0],0x0
10008a67:	00 00 00 
10008a6a:	8b 15 84 c4 01 10    	mov    edx,DWORD PTR ds:0x1001c484
10008a70:	6a 00                	push   0x0
10008a72:	6a 00                	push   0x0
10008a74:	51                   	push   ecx
10008a75:	52                   	push   edx
10008a76:	ff 15 6c 62 01 10    	call   DWORD PTR ds:0x1001626c
10008a7c:	c7 06 01 00 00 00    	mov    DWORD PTR [esi],0x1
10008a82:	c7 46 04 01 00 00 00 	mov    DWORD PTR [esi+0x4],0x1
10008a89:	c7 86 ac 01 00 00 01 	mov    DWORD PTR [esi+0x1ac],0x1
10008a90:	00 00 00 
10008a93:	a1 18 c1 06 10       	mov    eax,ds:0x1006c118
10008a98:	83 c4 10             	add    esp,0x10
10008a9b:	85 c0                	test   eax,eax
10008a9d:	74 07                	je     0x10008aa6
10008a9f:	8b ce                	mov    ecx,esi
10008aa1:	e8 fa 01 00 00       	call   0x10008ca0
10008aa6:	8d 4c 24 1c          	lea    ecx,[esp+0x1c]
10008aaa:	c7 44 24 0c ff ff ff 	mov    DWORD PTR [esp+0xc],0xffffffff
10008ab1:	ff 
10008ab2:	e8 b7 bc 00 00       	call   0x1001476e
10008ab7:	8b 4c 24 04          	mov    ecx,DWORD PTR [esp+0x4]
10008abb:	5e                   	pop    esi
10008abc:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
10008ac3:	83 c4 0c             	add    esp,0xc
10008ac6:	c2 0c 00             	ret    0xc
10008ac9:	90                   	nop
10008aca:	90                   	nop
10008acb:	90                   	nop
10008acc:	90                   	nop
10008acd:	90                   	nop
10008ace:	90                   	nop
10008acf:	90                   	nop
10008ad0:	6a ff                	push   0xffffffff
10008ad2:	68 18 52 01 10       	push   0x10015218
10008ad7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
10008add:	50                   	push   eax
10008ade:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
10008ae5:	83 ec 14             	sub    esp,0x14
10008ae8:	56                   	push   esi
10008ae9:	8b f1                	mov    esi,ecx
10008aeb:	8d 4c 24 04          	lea    ecx,[esp+0x4]
10008aef:	e8 9e bc 00 00       	call   0x10014792
10008af4:	8b 86 ac 01 00 00    	mov    eax,DWORD PTR [esi+0x1ac]
10008afa:	c7 44 24 20 00 00 00 	mov    DWORD PTR [esp+0x20],0x0
10008b01:	00 
10008b02:	83 e8 00             	sub    eax,0x0
10008b05:	0f 84 f2 00 00 00    	je     0x10008bfd
10008b0b:	48                   	dec    eax
10008b0c:	0f 85 66 01 00 00    	jne    0x10008c78
10008b12:	8b 46 0c             	mov    eax,DWORD PTR [esi+0xc]
10008b15:	8b 56 14             	mov    edx,DWORD PTR [esi+0x14]
10008b18:	8b 4e 08             	mov    ecx,DWORD PTR [esi+0x8]
10008b1b:	53                   	push   ebx
10008b1c:	55                   	push   ebp
10008b1d:	03 d0                	add    edx,eax
10008b1f:	57                   	push   edi
10008b20:	52                   	push   edx
10008b21:	8b 56 10             	mov    edx,DWORD PTR [esi+0x10]
10008b24:	03 d1                	add    edx,ecx
10008b26:	52                   	push   edx
10008b27:	50                   	push   eax
10008b28:	51                   	push   ecx
10008b29:	e8 e2 02 00 00       	call   0x10008e10
10008b2e:	8b 7e 08             	mov    edi,DWORD PTR [esi+0x8]
10008b31:	8b 5e 0c             	mov    ebx,DWORD PTR [esi+0xc]
10008b34:	8b 2d d4 61 01 10    	mov    ebp,DWORD PTR ds:0x100161d4
10008b3a:	83 c4 10             	add    esp,0x10
10008b3d:	8d 44 24 14          	lea    eax,[esp+0x14]
10008b41:	83 c7 12             	add    edi,0x12
10008b44:	68 70 01 00 00       	push   0x170
10008b49:	68 9e 00 00 00       	push   0x9e
10008b4e:	68 2c 01 00 00       	push   0x12c
10008b53:	6a 5a                	push   0x5a
10008b55:	50                   	push   eax
10008b56:	83 c3 12             	add    ebx,0x12
10008b59:	ff d5                	call   ebp
10008b5b:	8b 15 04 a7 06 10    	mov    edx,DWORD PTR ds:0x1006a704
10008b61:	8d 4c 24 14          	lea    ecx,[esp+0x14]
10008b65:	6a 00                	push   0x0
10008b67:	51                   	push   ecx
10008b68:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008b6e:	52                   	push   edx
10008b6f:	53                   	push   ebx
10008b70:	57                   	push   edi
10008b71:	e8 9e bb 00 00       	call   0x10014714
10008b76:	8b 7e 08             	mov    edi,DWORD PTR [esi+0x8]
10008b79:	8b 5e 0c             	mov    ebx,DWORD PTR [esi+0xc]
10008b7c:	6a 40                	push   0x40
10008b7e:	6a 40                	push   0x40
10008b80:	6a 00                	push   0x0
10008b82:	8d 44 24 20          	lea    eax,[esp+0x20]
10008b86:	6a 00                	push   0x0
10008b88:	50                   	push   eax
10008b89:	83 c7 14             	add    edi,0x14
10008b8c:	83 c3 14             	add    ebx,0x14
10008b8f:	ff d5                	call   ebp
10008b91:	8b 15 84 c4 01 10    	mov    edx,DWORD PTR ds:0x1001c484
10008b97:	8d 4c 24 14          	lea    ecx,[esp+0x14]
10008b9b:	6a 00                	push   0x0
10008b9d:	51                   	push   ecx
10008b9e:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008ba4:	52                   	push   edx
10008ba5:	53                   	push   ebx
10008ba6:	57                   	push   edi
10008ba7:	e8 68 bb 00 00       	call   0x10014714
10008bac:	33 db                	xor    ebx,ebx
10008bae:	33 ff                	xor    edi,edi
10008bb0:	8b 86 b0 01 00 00    	mov    eax,DWORD PTR [esi+0x1b0]
10008bb6:	03 c3                	add    eax,ebx
10008bb8:	8d 4c 86 1c          	lea    ecx,[esi+eax*4+0x1c]
10008bbc:	51                   	push   ecx
10008bbd:	8d 4c 24 14          	lea    ecx,[esp+0x14]
10008bc1:	e8 a2 bb 00 00       	call   0x10014768
10008bc6:	8b 46 0c             	mov    eax,DWORD PTR [esi+0xc]
10008bc9:	8b 54 24 10          	mov    edx,DWORD PTR [esp+0x10]
10008bcd:	6a 00                	push   0x0
10008bcf:	68 ff ff ff 00       	push   0xffffff
10008bd4:	8d 4c 07 17          	lea    ecx,[edi+eax*1+0x17]
10008bd8:	52                   	push   edx
10008bd9:	8b 15 70 c4 01 10    	mov    edx,DWORD PTR ds:0x1001c470
10008bdf:	51                   	push   ecx
10008be0:	68 96 00 00 00       	push   0x96
10008be5:	52                   	push   edx
10008be6:	ff 15 50 c4 01 10    	call   DWORD PTR ds:0x1001c450
10008bec:	83 c4 18             	add    esp,0x18
10008bef:	83 c7 14             	add    edi,0x14
10008bf2:	43                   	inc    ebx
10008bf3:	83 ff 28             	cmp    edi,0x28
10008bf6:	7e b8                	jle    0x10008bb0
10008bf8:	5f                   	pop    edi
10008bf9:	5d                   	pop    ebp
10008bfa:	5b                   	pop    ebx
10008bfb:	eb 7b                	jmp    0x10008c78
10008bfd:	8b 46 0c             	mov    eax,DWORD PTR [esi+0xc]
10008c00:	8b 56 14             	mov    edx,DWORD PTR [esi+0x14]
10008c03:	8b 4e 08             	mov    ecx,DWORD PTR [esi+0x8]
10008c06:	03 d0                	add    edx,eax
10008c08:	52                   	push   edx
10008c09:	8b 56 10             	mov    edx,DWORD PTR [esi+0x10]
10008c0c:	03 d1                	add    edx,ecx
10008c0e:	52                   	push   edx
10008c0f:	50                   	push   eax
10008c10:	51                   	push   ecx
10008c11:	e8 fa 01 00 00       	call   0x10008e10
10008c16:	83 c4 10             	add    esp,0x10
10008c19:	8d 46 1c             	lea    eax,[esi+0x1c]
10008c1c:	8d 4c 24 04          	lea    ecx,[esp+0x4]
10008c20:	50                   	push   eax
10008c21:	e8 42 bb 00 00       	call   0x10014768
10008c26:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
10008c2a:	8b 48 f8             	mov    ecx,DWORD PTR [eax-0x8]
10008c2d:	85 c9                	test   ecx,ecx
10008c2f:	74 47                	je     0x10008c78
10008c31:	8b 4e 0c             	mov    ecx,DWORD PTR [esi+0xc]
10008c34:	6a 64                	push   0x64
10008c36:	68 ff ff ff 00       	push   0xffffff
10008c3b:	50                   	push   eax
10008c3c:	8b 46 10             	mov    eax,DWORD PTR [esi+0x10]
10008c3f:	83 c1 17             	add    ecx,0x17
10008c42:	99                   	cdq
10008c43:	2b c2                	sub    eax,edx
10008c45:	8b 15 70 c4 01 10    	mov    edx,DWORD PTR ds:0x1001c470
10008c4b:	51                   	push   ecx
10008c4c:	8b 4e 08             	mov    ecx,DWORD PTR [esi+0x8]
10008c4f:	d1 f8                	sar    eax,1
10008c51:	03 c1                	add    eax,ecx
10008c53:	50                   	push   eax
10008c54:	52                   	push   edx
10008c55:	ff 15 50 c4 01 10    	call   DWORD PTR ds:0x1001c450
10008c5b:	83 c4 18             	add    esp,0x18
10008c5e:	ff 15 8c 61 01 10    	call   DWORD PTR ds:0x1001618c
10008c64:	2b 86 bc 01 00 00    	sub    eax,DWORD PTR [esi+0x1bc]
10008c6a:	3d 10 27 00 00       	cmp    eax,0x2710
10008c6f:	7e 07                	jle    0x10008c78
10008c71:	8b ce                	mov    ecx,esi
10008c73:	e8 28 00 00 00       	call   0x10008ca0
10008c78:	8d 4c 24 04          	lea    ecx,[esp+0x4]
10008c7c:	c7 44 24 20 ff ff ff 	mov    DWORD PTR [esp+0x20],0xffffffff
10008c83:	ff 
10008c84:	e8 e5 ba 00 00       	call   0x1001476e
10008c89:	8b 4c 24 18          	mov    ecx,DWORD PTR [esp+0x18]
10008c8d:	5e                   	pop    esi
10008c8e:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
10008c95:	83 c4 20             	add    esp,0x20
10008c98:	c3                   	ret
10008c99:	90                   	nop
10008c9a:	90                   	nop
10008c9b:	90                   	nop
10008c9c:	90                   	nop
10008c9d:	90                   	nop
10008c9e:	90                   	nop
10008c9f:	90                   	nop
10008ca0:	8b 81 ac 01 00 00    	mov    eax,DWORD PTR [ecx+0x1ac]
10008ca6:	33 d2                	xor    edx,edx
10008ca8:	2b c2                	sub    eax,edx
10008caa:	56                   	push   esi
10008cab:	74 33                	je     0x10008ce0
10008cad:	48                   	dec    eax
10008cae:	75 3d                	jne    0x10008ced
10008cb0:	8b 81 b0 01 00 00    	mov    eax,DWORD PTR [ecx+0x1b0]
10008cb6:	8b b1 b4 01 00 00    	mov    esi,DWORD PTR [ecx+0x1b4]
10008cbc:	83 c0 03             	add    eax,0x3
10008cbf:	3b c6                	cmp    eax,esi
10008cc1:	89 81 b0 01 00 00    	mov    DWORD PTR [ecx+0x1b0],eax
10008cc7:	7c 24                	jl     0x10008ced
10008cc9:	39 15 18 c1 06 10    	cmp    DWORD PTR ds:0x1006c118,edx
10008ccf:	75 04                	jne    0x10008cd5
10008cd1:	89 11                	mov    DWORD PTR [ecx],edx
10008cd3:	eb 18                	jmp    0x10008ced
10008cd5:	83 c0 fd             	add    eax,0xfffffffd
10008cd8:	89 81 b0 01 00 00    	mov    DWORD PTR [ecx+0x1b0],eax
10008cde:	eb 0a                	jmp    0x10008cea
10008ce0:	39 15 18 c1 06 10    	cmp    DWORD PTR ds:0x1006c118,edx
10008ce6:	75 05                	jne    0x10008ced
10008ce8:	89 11                	mov    DWORD PTR [ecx],edx
10008cea:	89 51 04             	mov    DWORD PTR [ecx+0x4],edx
10008ced:	8b 01                	mov    eax,DWORD PTR [ecx]
10008cef:	5e                   	pop    esi
10008cf0:	3b c2                	cmp    eax,edx
10008cf2:	75 27                	jne    0x10008d1b
10008cf4:	83 79 18 01          	cmp    DWORD PTR [ecx+0x18],0x1
10008cf8:	75 21                	jne    0x10008d1b
10008cfa:	52                   	push   edx
10008cfb:	6a 08                	push   0x8
10008cfd:	68 ca 04 00 00       	push   0x4ca
10008d02:	e8 c9 9c ff ff       	call   0x100029d0
10008d07:	68 6e 00 0a 00       	push   0xa006e
10008d0c:	6a 08                	push   0x8
10008d0e:	68 d0 04 00 00       	push   0x4d0
10008d13:	e8 b8 9c ff ff       	call   0x100029d0
10008d18:	83 c4 18             	add    esp,0x18
10008d1b:	c3                   	ret
10008d1c:	90                   	nop
10008d1d:	90                   	nop
10008d1e:	90                   	nop
10008d1f:	90                   	nop
10008d20:	a1 0c a7 06 10       	mov    eax,ds:0x1006a70c
10008d25:	8b 48 08             	mov    ecx,DWORD PTR [eax+0x8]
10008d28:	85 c9                	test   ecx,ecx
10008d2a:	74 12                	je     0x10008d3e
10008d2c:	e8 af 04 00 00       	call   0x100091e0
10008d31:	8b 0d 0c a7 06 10    	mov    ecx,DWORD PTR ds:0x1006a70c
10008d37:	8b 41 08             	mov    eax,DWORD PTR [ecx+0x8]
10008d3a:	85 c0                	test   eax,eax
10008d3c:	75 ee                	jne    0x10008d2c
10008d3e:	c3                   	ret
10008d3f:	90                   	nop
10008d40:	a1 c4 c4 01 10       	mov    eax,ds:0x1001c4c4
10008d45:	83 ec 10             	sub    esp,0x10
10008d48:	85 c0                	test   eax,eax
10008d4a:	53                   	push   ebx
10008d4b:	55                   	push   ebp
10008d4c:	56                   	push   esi
10008d4d:	57                   	push   edi
10008d4e:	0f 84 ae 00 00 00    	je     0x10008e02
10008d54:	8b 6c 24 28          	mov    ebp,DWORD PTR [esp+0x28]
10008d58:	8b c5                	mov    eax,ebp
10008d5a:	25 01 00 00 80       	and    eax,0x80000001
10008d5f:	79 05                	jns    0x10008d66
10008d61:	48                   	dec    eax
10008d62:	83 c8 fe             	or     eax,0xfffffffe
10008d65:	40                   	inc    eax
10008d66:	8b 74 24 2c          	mov    esi,DWORD PTR [esp+0x2c]
10008d6a:	2b e8                	sub    ebp,eax
10008d6c:	8b ce                	mov    ecx,esi
10008d6e:	81 e1 01 00 00 80    	and    ecx,0x80000001
10008d74:	79 05                	jns    0x10008d7b
10008d76:	49                   	dec    ecx
10008d77:	83 c9 fe             	or     ecx,0xfffffffe
10008d7a:	41                   	inc    ecx
10008d7b:	8b 5c 24 30          	mov    ebx,DWORD PTR [esp+0x30]
10008d7f:	03 f1                	add    esi,ecx
10008d81:	8b d3                	mov    edx,ebx
10008d83:	81 e2 01 00 00 80    	and    edx,0x80000001
10008d89:	79 05                	jns    0x10008d90
10008d8b:	4a                   	dec    edx
10008d8c:	83 ca fe             	or     edx,0xfffffffe
10008d8f:	42                   	inc    edx
10008d90:	8b 7c 24 24          	mov    edi,DWORD PTR [esp+0x24]
10008d94:	03 da                	add    ebx,edx
10008d96:	8b c7                	mov    eax,edi
10008d98:	25 01 00 00 80       	and    eax,0x80000001
10008d9d:	79 05                	jns    0x10008da4
10008d9f:	48                   	dec    eax
10008da0:	83 c8 fe             	or     eax,0xfffffffe
10008da3:	40                   	inc    eax
10008da4:	68 8c 00 00 00       	push   0x8c
10008da9:	68 08 02 00 00       	push   0x208
10008dae:	6a 78                	push   0x78
10008db0:	8d 4c 24 1c          	lea    ecx,[esp+0x1c]
10008db4:	68 f4 01 00 00       	push   0x1f4
10008db9:	51                   	push   ecx
10008dba:	2b f8                	sub    edi,eax
10008dbc:	ff 15 d4 61 01 10    	call   DWORD PTR ds:0x100161d4
10008dc2:	8d 46 ec             	lea    eax,[esi-0x14]
10008dc5:	3b f8                	cmp    edi,eax
10008dc7:	89 44 24 28          	mov    DWORD PTR [esp+0x28],eax
10008dcb:	7f 35                	jg     0x10008e02
10008dcd:	83 c3 ec             	add    ebx,0xffffffec
10008dd0:	3b eb                	cmp    ebp,ebx
10008dd2:	8b f5                	mov    esi,ebp
10008dd4:	7f 25                	jg     0x10008dfb
10008dd6:	a1 04 a7 06 10       	mov    eax,ds:0x1006a704
10008ddb:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008de1:	8d 54 24 10          	lea    edx,[esp+0x10]
10008de5:	6a 01                	push   0x1
10008de7:	52                   	push   edx
10008de8:	50                   	push   eax
10008de9:	56                   	push   esi
10008dea:	57                   	push   edi
10008deb:	e8 24 b9 00 00       	call   0x10014714
10008df0:	83 c6 14             	add    esi,0x14
10008df3:	3b f3                	cmp    esi,ebx
10008df5:	7e df                	jle    0x10008dd6
10008df7:	8b 44 24 28          	mov    eax,DWORD PTR [esp+0x28]
10008dfb:	83 c7 14             	add    edi,0x14
10008dfe:	3b f8                	cmp    edi,eax
10008e00:	7e ce                	jle    0x10008dd0
10008e02:	5f                   	pop    edi
10008e03:	5e                   	pop    esi
10008e04:	5d                   	pop    ebp
10008e05:	5b                   	pop    ebx
10008e06:	83 c4 10             	add    esp,0x10
10008e09:	c3                   	ret
10008e0a:	90                   	nop
10008e0b:	90                   	nop
10008e0c:	90                   	nop
10008e0d:	90                   	nop
10008e0e:	90                   	nop
10008e0f:	90                   	nop
10008e10:	83 ec 18             	sub    esp,0x18
10008e13:	53                   	push   ebx
10008e14:	55                   	push   ebp
10008e15:	8b 6c 24 24          	mov    ebp,DWORD PTR [esp+0x24]
10008e19:	56                   	push   esi
10008e1a:	8b c5                	mov    eax,ebp
10008e1c:	57                   	push   edi
10008e1d:	25 01 00 00 80       	and    eax,0x80000001
10008e22:	79 05                	jns    0x10008e29
10008e24:	48                   	dec    eax
10008e25:	83 c8 fe             	or     eax,0xfffffffe
10008e28:	40                   	inc    eax
10008e29:	2b e8                	sub    ebp,eax
10008e2b:	8b 44 24 30          	mov    eax,DWORD PTR [esp+0x30]
10008e2f:	8b c8                	mov    ecx,eax
10008e31:	81 e1 01 00 00 80    	and    ecx,0x80000001
10008e37:	79 05                	jns    0x10008e3e
10008e39:	49                   	dec    ecx
10008e3a:	83 c9 fe             	or     ecx,0xfffffffe
10008e3d:	41                   	inc    ecx
10008e3e:	8b 5c 24 34          	mov    ebx,DWORD PTR [esp+0x34]
10008e42:	2b c1                	sub    eax,ecx
10008e44:	8b d3                	mov    edx,ebx
10008e46:	89 44 24 30          	mov    DWORD PTR [esp+0x30],eax
10008e4a:	81 e2 01 00 00 80    	and    edx,0x80000001
10008e50:	79 05                	jns    0x10008e57
10008e52:	4a                   	dec    edx
10008e53:	83 ca fe             	or     edx,0xfffffffe
10008e56:	42                   	inc    edx
10008e57:	8b 7c 24 38          	mov    edi,DWORD PTR [esp+0x38]
10008e5b:	03 da                	add    ebx,edx
10008e5d:	8b c7                	mov    eax,edi
10008e5f:	25 01 00 00 80       	and    eax,0x80000001
10008e64:	79 05                	jns    0x10008e6b
10008e66:	48                   	dec    eax
10008e67:	83 c8 fe             	or     eax,0xfffffffe
10008e6a:	40                   	inc    eax
10008e6b:	8b 35 d4 61 01 10    	mov    esi,DWORD PTR ds:0x100161d4
10008e71:	68 54 01 00 00       	push   0x154
10008e76:	6a 28                	push   0x28
10008e78:	68 40 01 00 00       	push   0x140
10008e7d:	8d 4c 24 24          	lea    ecx,[esp+0x24]
10008e81:	03 f8                	add    edi,eax
10008e83:	6a 14                	push   0x14
10008e85:	51                   	push   ecx
10008e86:	89 7c 24 4c          	mov    DWORD PTR [esp+0x4c],edi
10008e8a:	89 6c 24 24          	mov    DWORD PTR [esp+0x24],ebp
10008e8e:	ff d6                	call   esi
10008e90:	83 c3 ec             	add    ebx,0xffffffec
10008e93:	3b eb                	cmp    ebp,ebx
10008e95:	7d 4e                	jge    0x10008ee5
10008e97:	83 c7 ec             	add    edi,0xffffffec
10008e9a:	89 7c 24 2c          	mov    DWORD PTR [esp+0x2c],edi
10008e9e:	8b 7c 24 30          	mov    edi,DWORD PTR [esp+0x30]
10008ea2:	8b 54 24 2c          	mov    edx,DWORD PTR [esp+0x2c]
10008ea6:	8b c7                	mov    eax,edi
10008ea8:	3b c2                	cmp    eax,edx
10008eaa:	7d 2a                	jge    0x10008ed6
10008eac:	8b 0d 04 a7 06 10    	mov    ecx,DWORD PTR ds:0x1006a704
10008eb2:	8b 54 24 10          	mov    edx,DWORD PTR [esp+0x10]
10008eb6:	8d 44 24 18          	lea    eax,[esp+0x18]
10008eba:	6a 01                	push   0x1
10008ebc:	50                   	push   eax
10008ebd:	51                   	push   ecx
10008ebe:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008ec4:	57                   	push   edi
10008ec5:	52                   	push   edx
10008ec6:	e8 49 b8 00 00       	call   0x10014714
10008ecb:	8b 44 24 2c          	mov    eax,DWORD PTR [esp+0x2c]
10008ecf:	83 c7 14             	add    edi,0x14
10008ed2:	3b f8                	cmp    edi,eax
10008ed4:	7c d6                	jl     0x10008eac
10008ed6:	8b 44 24 10          	mov    eax,DWORD PTR [esp+0x10]
10008eda:	83 c0 14             	add    eax,0x14
10008edd:	3b c3                	cmp    eax,ebx
10008edf:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
10008ee3:	7c b9                	jl     0x10008e9e
10008ee5:	8d 7d 14             	lea    edi,[ebp+0x14]
10008ee8:	3b fb                	cmp    edi,ebx
10008eea:	7d 71                	jge    0x10008f5d
10008eec:	68 40 01 00 00       	push   0x140
10008ef1:	6a 28                	push   0x28
10008ef3:	68 2c 01 00 00       	push   0x12c
10008ef8:	8d 44 24 24          	lea    eax,[esp+0x24]
10008efc:	6a 14                	push   0x14
10008efe:	50                   	push   eax
10008eff:	ff d6                	call   esi
10008f01:	8b 15 04 a7 06 10    	mov    edx,DWORD PTR ds:0x1006a704
10008f07:	8b 44 24 30          	mov    eax,DWORD PTR [esp+0x30]
10008f0b:	8d 4c 24 18          	lea    ecx,[esp+0x18]
10008f0f:	6a 01                	push   0x1
10008f11:	51                   	push   ecx
10008f12:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008f18:	52                   	push   edx
10008f19:	50                   	push   eax
10008f1a:	57                   	push   edi
10008f1b:	e8 f4 b7 00 00       	call   0x10014714
10008f20:	68 68 01 00 00       	push   0x168
10008f25:	6a 28                	push   0x28
10008f27:	68 54 01 00 00       	push   0x154
10008f2c:	8d 4c 24 24          	lea    ecx,[esp+0x24]
10008f30:	6a 14                	push   0x14
10008f32:	51                   	push   ecx
10008f33:	ff d6                	call   esi
10008f35:	a1 04 a7 06 10       	mov    eax,ds:0x1006a704
10008f3a:	8b 4c 24 38          	mov    ecx,DWORD PTR [esp+0x38]
10008f3e:	8d 54 24 18          	lea    edx,[esp+0x18]
10008f42:	6a 01                	push   0x1
10008f44:	52                   	push   edx
10008f45:	50                   	push   eax
10008f46:	8d 41 ec             	lea    eax,[ecx-0x14]
10008f49:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008f4f:	50                   	push   eax
10008f50:	57                   	push   edi
10008f51:	e8 be b7 00 00       	call   0x10014714
10008f56:	83 c7 14             	add    edi,0x14
10008f59:	3b fb                	cmp    edi,ebx
10008f5b:	7c 8f                	jl     0x10008eec
10008f5d:	8b 54 24 30          	mov    edx,DWORD PTR [esp+0x30]
10008f61:	8b 44 24 38          	mov    eax,DWORD PTR [esp+0x38]
10008f65:	83 c0 ec             	add    eax,0xffffffec
10008f68:	8d 7a 14             	lea    edi,[edx+0x14]
10008f6b:	89 44 24 2c          	mov    DWORD PTR [esp+0x2c],eax
10008f6f:	3b f8                	cmp    edi,eax
10008f71:	7d 69                	jge    0x10008fdc
10008f73:	68 54 01 00 00       	push   0x154
10008f78:	6a 14                	push   0x14
10008f7a:	68 40 01 00 00       	push   0x140
10008f7f:	8d 4c 24 24          	lea    ecx,[esp+0x24]
10008f83:	6a 00                	push   0x0
10008f85:	51                   	push   ecx
10008f86:	ff d6                	call   esi
10008f88:	a1 04 a7 06 10       	mov    eax,ds:0x1006a704
10008f8d:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008f93:	8d 54 24 18          	lea    edx,[esp+0x18]
10008f97:	6a 01                	push   0x1
10008f99:	52                   	push   edx
10008f9a:	50                   	push   eax
10008f9b:	57                   	push   edi
10008f9c:	55                   	push   ebp
10008f9d:	e8 72 b7 00 00       	call   0x10014714
10008fa2:	68 54 01 00 00       	push   0x154
10008fa7:	6a 3c                	push   0x3c
10008fa9:	68 40 01 00 00       	push   0x140
10008fae:	8d 4c 24 24          	lea    ecx,[esp+0x24]
10008fb2:	6a 28                	push   0x28
10008fb4:	51                   	push   ecx
10008fb5:	ff d6                	call   esi
10008fb7:	a1 04 a7 06 10       	mov    eax,ds:0x1006a704
10008fbc:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10008fc2:	8d 54 24 18          	lea    edx,[esp+0x18]
10008fc6:	6a 01                	push   0x1
10008fc8:	52                   	push   edx
10008fc9:	50                   	push   eax
10008fca:	57                   	push   edi
10008fcb:	53                   	push   ebx
10008fcc:	e8 43 b7 00 00       	call   0x10014714
10008fd1:	8b 44 24 2c          	mov    eax,DWORD PTR [esp+0x2c]
10008fd5:	83 c7 14             	add    edi,0x14
10008fd8:	3b f8                	cmp    edi,eax
10008fda:	7c 97                	jl     0x10008f73
10008fdc:	68 40 01 00 00       	push   0x140
10008fe1:	6a 14                	push   0x14
10008fe3:	68 2c 01 00 00       	push   0x12c
10008fe8:	8d 4c 24 24          	lea    ecx,[esp+0x24]
10008fec:	6a 00                	push   0x0
10008fee:	51                   	push   ecx
10008fef:	ff d6                	call   esi
10008ff1:	a1 04 a7 06 10       	mov    eax,ds:0x1006a704
10008ff6:	8b 4c 24 30          	mov    ecx,DWORD PTR [esp+0x30]
10008ffa:	8d 54 24 18          	lea    edx,[esp+0x18]
10008ffe:	6a 01                	push   0x1
10009000:	52                   	push   edx
10009001:	50                   	push   eax
10009002:	51                   	push   ecx
10009003:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10009009:	55                   	push   ebp
1000900a:	e8 05 b7 00 00       	call   0x10014714
1000900f:	68 68 01 00 00       	push   0x168
10009014:	6a 14                	push   0x14
10009016:	68 54 01 00 00       	push   0x154
1000901b:	8d 54 24 24          	lea    edx,[esp+0x24]
1000901f:	6a 00                	push   0x0
10009021:	52                   	push   edx
10009022:	ff d6                	call   esi
10009024:	8b 0d 04 a7 06 10    	mov    ecx,DWORD PTR ds:0x1006a704
1000902a:	8b 7c 24 2c          	mov    edi,DWORD PTR [esp+0x2c]
1000902e:	8d 44 24 18          	lea    eax,[esp+0x18]
10009032:	6a 01                	push   0x1
10009034:	50                   	push   eax
10009035:	51                   	push   ecx
10009036:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
1000903c:	57                   	push   edi
1000903d:	55                   	push   ebp
1000903e:	e8 d1 b6 00 00       	call   0x10014714
10009043:	68 40 01 00 00       	push   0x140
10009048:	6a 3c                	push   0x3c
1000904a:	68 2c 01 00 00       	push   0x12c
1000904f:	8d 54 24 24          	lea    edx,[esp+0x24]
10009053:	6a 28                	push   0x28
10009055:	52                   	push   edx
10009056:	ff d6                	call   esi
10009058:	8b 0d 04 a7 06 10    	mov    ecx,DWORD PTR ds:0x1006a704
1000905e:	8b 54 24 30          	mov    edx,DWORD PTR [esp+0x30]
10009062:	8d 44 24 18          	lea    eax,[esp+0x18]
10009066:	6a 01                	push   0x1
10009068:	50                   	push   eax
10009069:	51                   	push   ecx
1000906a:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
10009070:	52                   	push   edx
10009071:	53                   	push   ebx
10009072:	e8 9d b6 00 00       	call   0x10014714
10009077:	68 68 01 00 00       	push   0x168
1000907c:	6a 3c                	push   0x3c
1000907e:	68 54 01 00 00       	push   0x154
10009083:	8d 44 24 24          	lea    eax,[esp+0x24]
10009087:	6a 28                	push   0x28
10009089:	50                   	push   eax
1000908a:	ff d6                	call   esi
1000908c:	8b 15 04 a7 06 10    	mov    edx,DWORD PTR ds:0x1006a704
10009092:	8d 4c 24 18          	lea    ecx,[esp+0x18]
10009096:	6a 01                	push   0x1
10009098:	51                   	push   ecx
10009099:	8b 0d 70 c4 01 10    	mov    ecx,DWORD PTR ds:0x1001c470
1000909f:	52                   	push   edx
100090a0:	57                   	push   edi
100090a1:	53                   	push   ebx
100090a2:	e8 6d b6 00 00       	call   0x10014714
100090a7:	5f                   	pop    edi
100090a8:	5e                   	pop    esi
100090a9:	5d                   	pop    ebp
100090aa:	5b                   	pop    ebx
100090ab:	83 c4 18             	add    esp,0x18
100090ae:	c3                   	ret
100090af:	90                   	nop
100090b0:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
100090b4:	83 ec 10             	sub    esp,0x10
100090b7:	83 f8 02             	cmp    eax,0x2
100090ba:	56                   	push   esi
100090bb:	57                   	push   edi
100090bc:	7f 50                	jg     0x1000910e
100090be:	8b 74 24 28          	mov    esi,DWORD PTR [esp+0x28]
100090c2:	8d 04 80             	lea    eax,[eax+eax*4]
100090c5:	8b 7c 24 30          	mov    edi,DWORD PTR [esp+0x30]
100090c9:	8b 54 24 2c          	mov    edx,DWORD PTR [esp+0x2c]
100090cd:	8d 04 80             	lea    eax,[eax+eax*4]
100090d0:	c1 e0 02             	shl    eax,0x2
100090d3:	8b c8                	mov    ecx,eax
100090d5:	2b ce                	sub    ecx,esi
100090d7:	03 cf                	add    ecx,edi
100090d9:	8b 7c 24 24          	mov    edi,DWORD PTR [esp+0x24]
100090dd:	2b d7                	sub    edx,edi
100090df:	51                   	push   ecx
100090e0:	52                   	push   edx
100090e1:	50                   	push   eax
100090e2:	8d 44 24 14          	lea    eax,[esp+0x14]
100090e6:	6a 00                	push   0x0
100090e8:	50                   	push   eax
100090e9:	ff 15 d4 61 01 10    	call   DWORD PTR ds:0x100161d4
100090ef:	8b 15 04 a7 06 10    	mov    edx,DWORD PTR ds:0x1006a704
100090f5:	8d 4c 24 08          	lea    ecx,[esp+0x8]
100090f9:	6a 01                	push   0x1
100090fb:	51                   	push   ecx
100090fc:	8b 4c 24 28          	mov    ecx,DWORD PTR [esp+0x28]
10009100:	52                   	push   edx
10009101:	56                   	push   esi
10009102:	57                   	push   edi
10009103:	e8 0c b6 00 00       	call   0x10014714
10009108:	5f                   	pop    edi
10009109:	5e                   	pop    esi
1000910a:	83 c4 10             	add    esp,0x10
1000910d:	c3                   	ret
1000910e:	83 f8 03             	cmp    eax,0x3
10009111:	75 44                	jne    0x10009157
10009113:	8b 44 24 30          	mov    eax,DWORD PTR [esp+0x30]
10009117:	8b 74 24 28          	mov    esi,DWORD PTR [esp+0x28]
1000911b:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
1000911f:	8b 7c 24 24          	mov    edi,DWORD PTR [esp+0x24]
10009123:	2b c6                	sub    eax,esi
10009125:	2b cf                	sub    ecx,edi
10009127:	50                   	push   eax
10009128:	51                   	push   ecx
10009129:	6a 00                	push   0x0
1000912b:	8d 54 24 14          	lea    edx,[esp+0x14]
1000912f:	6a 00                	push   0x0
10009131:	52                   	push   edx
10009132:	ff 15 d4 61 01 10    	call   DWORD PTR ds:0x100161d4
10009138:	8b 0d 04 a7 06 10    	mov    ecx,DWORD PTR ds:0x1006a704
1000913e:	8d 44 24 08          	lea    eax,[esp+0x8]
10009142:	6a 01                	push   0x1
10009144:	50                   	push   eax
10009145:	51                   	push   ecx
10009146:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
1000914a:	56                   	push   esi
1000914b:	57                   	push   edi
1000914c:	e8 c3 b5 00 00       	call   0x10014714
10009151:	5f                   	pop    edi
10009152:	5e                   	pop    esi
10009153:	83 c4 10             	add    esp,0x10
10009156:	c3                   	ret
10009157:	7e 76                	jle    0x100091cf
10009159:	8d 48 fc             	lea    ecx,[eax-0x4]
1000915c:	b8 56 55 55 55       	mov    eax,0x55555556
10009161:	f7 e9                	imul   ecx
10009163:	8b c2                	mov    eax,edx
10009165:	8b 7c 24 24          	mov    edi,DWORD PTR [esp+0x24]
10009169:	c1 e8 1f             	shr    eax,0x1f
1000916c:	03 d0                	add    edx,eax
1000916e:	8b c1                	mov    eax,ecx
10009170:	8b f2                	mov    esi,edx
10009172:	b9 03 00 00 00       	mov    ecx,0x3
10009177:	99                   	cdq
10009178:	f7 f9                	idiv   ecx
1000917a:	8d 04 b6             	lea    eax,[esi+esi*4]
1000917d:	8b 74 24 28          	mov    esi,DWORD PTR [esp+0x28]
10009181:	53                   	push   ebx
10009182:	8b 5c 24 34          	mov    ebx,DWORD PTR [esp+0x34]
10009186:	8d 04 80             	lea    eax,[eax+eax*4]
10009189:	c1 e0 02             	shl    eax,0x2
1000918c:	8d 4c 92 0a          	lea    ecx,[edx+edx*4+0xa]
10009190:	8b d0                	mov    edx,eax
10009192:	2b d6                	sub    edx,esi
10009194:	8d 0c 89             	lea    ecx,[ecx+ecx*4]
10009197:	03 d3                	add    edx,ebx
10009199:	8b 5c 24 30          	mov    ebx,DWORD PTR [esp+0x30]
1000919d:	52                   	push   edx
1000919e:	c1 e1 02             	shl    ecx,0x2
100091a1:	8b d1                	mov    edx,ecx
100091a3:	2b d7                	sub    edx,edi
100091a5:	03 d3                	add    edx,ebx
100091a7:	52                   	push   edx
100091a8:	50                   	push   eax
100091a9:	8d 44 24 18          	lea    eax,[esp+0x18]
100091ad:	51                   	push   ecx
100091ae:	50                   	push   eax
100091af:	ff 15 d4 61 01 10    	call   DWORD PTR ds:0x100161d4
100091b5:	8b 15 04 a7 06 10    	mov    edx,DWORD PTR ds:0x1006a704
100091bb:	8d 4c 24 0c          	lea    ecx,[esp+0xc]
100091bf:	6a 01                	push   0x1
100091c1:	51                   	push   ecx
100091c2:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
100091c6:	52                   	push   edx
100091c7:	56                   	push   esi
100091c8:	57                   	push   edi
100091c9:	e8 46 b5 00 00       	call   0x10014714
100091ce:	5b                   	pop    ebx
100091cf:	5f                   	pop    edi
100091d0:	5e                   	pop    esi
100091d1:	83 c4 10             	add    esp,0x10
100091d4:	c3                   	ret
100091d5:	90                   	nop
100091d6:	90                   	nop
100091d7:	90                   	nop
100091d8:	90                   	nop
100091d9:	90                   	nop
100091da:	90                   	nop
100091db:	90                   	nop
100091dc:	90                   	nop
100091dd:	90                   	nop
100091de:	90                   	nop
100091df:	90                   	nop
100091e0:	56                   	push   esi
100091e1:	8b 35 0c a7 06 10    	mov    esi,DWORD PTR ds:0x1006a70c
100091e7:	85 f6                	test   esi,esi
100091e9:	74 22                	je     0x1000920d
100091eb:	57                   	push   edi
100091ec:	8b 7e 08             	mov    edi,DWORD PTR [esi+0x8]
100091ef:	8b ce                	mov    ecx,esi
100091f1:	e8 ba 00 00 00       	call   0x100092b0
100091f6:	56                   	push   esi
100091f7:	e8 60 b5 00 00       	call   0x1001475c
100091fc:	83 c4 04             	add    esp,0x4
100091ff:	89                   	.byte 0x89
