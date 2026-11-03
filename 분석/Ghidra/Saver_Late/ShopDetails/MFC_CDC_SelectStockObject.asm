
MFC42D.DLL [SHA256 3a3fa253ce80cf55027abc7918f8984057a94d1832302fb9893402aba9d35dff]:     file format pei-i386


Disassembly of section .text:

5f43deee <.text+0x3ceee>:
5f43deee:	55                   	push   ebp
5f43deef:	8b ec                	mov    ebp,esp
5f43def1:	83 ec 0c             	sub    esp,0xc
5f43def4:	53                   	push   ebx
5f43def5:	56                   	push   esi
5f43def6:	57                   	push   edi
5f43def7:	89 4d f4             	mov    DWORD PTR [ebp-0xc],ecx
5f43defa:	8b 45 f4             	mov    eax,DWORD PTR [ebp-0xc]
5f43defd:	83 78 04 00          	cmp    DWORD PTR [eax+0x4],0x0
5f43df01:	75 14                	jne    0x5f43df17
5f43df03:	68 e8 00 00 00       	push   0xe8
5f43df08:	68 bc d5 4c 5f       	push   0x5f4cd5bc
5f43df0d:	e8 9e 8c 03 00       	call   0x5f476bb0
5f43df12:	85 c0                	test   eax,eax
5f43df14:	74 01                	je     0x5f43df17
5f43df16:	cc                   	int3
5f43df17:	33 c9                	xor    ecx,ecx
5f43df19:	85 c9                	test   ecx,ecx
5f43df1b:	75 dd                	jne    0x5f43defa
5f43df1d:	8b 55 08             	mov    edx,DWORD PTR [ebp+0x8]
5f43df20:	52                   	push   edx
5f43df21:	ff 15 ec b2 4a 5f    	call   DWORD PTR ds:0x5f4ab2ec
5f43df27:	89 45 fc             	mov    DWORD PTR [ebp-0x4],eax
5f43df2a:	c7 45 f8 00 00 00 00 	mov    DWORD PTR [ebp-0x8],0x0
5f43df31:	83 7d fc 00          	cmp    DWORD PTR [ebp-0x4],0x0
5f43df35:	75 14                	jne    0x5f43df4b
5f43df37:	68 ed 00 00 00       	push   0xed
5f43df3c:	68 bc d5 4c 5f       	push   0x5f4cd5bc
5f43df41:	e8 6a 8c 03 00       	call   0x5f476bb0
5f43df46:	85 c0                	test   eax,eax
5f43df48:	74 01                	je     0x5f43df4b
5f43df4a:	cc                   	int3
5f43df4b:	33 c0                	xor    eax,eax
5f43df4d:	85 c0                	test   eax,eax
5f43df4f:	75 e0                	jne    0x5f43df31
5f43df51:	8b 4d f4             	mov    ecx,DWORD PTR [ebp-0xc]
5f43df54:	8b 55 f4             	mov    edx,DWORD PTR [ebp-0xc]
5f43df57:	8b 41 04             	mov    eax,DWORD PTR [ecx+0x4]
5f43df5a:	3b 42 08             	cmp    eax,DWORD PTR [edx+0x8]
5f43df5d:	74 14                	je     0x5f43df73
5f43df5f:	8b 4d fc             	mov    ecx,DWORD PTR [ebp-0x4]
5f43df62:	51                   	push   ecx
5f43df63:	8b 55 f4             	mov    edx,DWORD PTR [ebp-0xc]
5f43df66:	8b 42 04             	mov    eax,DWORD PTR [edx+0x4]
5f43df69:	50                   	push   eax
5f43df6a:	ff 15 10 b2 4a 5f    	call   DWORD PTR ds:0x5f4ab210
5f43df70:	89 45 f8             	mov    DWORD PTR [ebp-0x8],eax
5f43df73:	8b 4d f4             	mov    ecx,DWORD PTR [ebp-0xc]
5f43df76:	83 79 08 00          	cmp    DWORD PTR [ecx+0x8],0x0
5f43df7a:	74 14                	je     0x5f43df90
5f43df7c:	8b 55 fc             	mov    edx,DWORD PTR [ebp-0x4]
5f43df7f:	52                   	push   edx
5f43df80:	8b 45 f4             	mov    eax,DWORD PTR [ebp-0xc]
5f43df83:	8b 48 08             	mov    ecx,DWORD PTR [eax+0x8]
5f43df86:	51                   	push   ecx
5f43df87:	ff 15 10 b2 4a 5f    	call   DWORD PTR ds:0x5f4ab210
5f43df8d:	89 45 f8             	mov    DWORD PTR [ebp-0x8],eax
5f43df90:	8b 55 f8             	mov    edx,DWORD PTR [ebp-0x8]
5f43df93:	52                   	push   edx
5f43df94:	e8 38 29 00 00       	call   0x5f4408d1
5f43df99:	5f                   	pop    edi
5f43df9a:	5e                   	pop    esi
5f43df9b:	5b                   	pop    ebx
5f43df9c:	8b e5                	mov    esp,ebp
5f43df9e:	5d                   	pop    ebp
5f43df9f:	c2 04 00             	ret    0x4
