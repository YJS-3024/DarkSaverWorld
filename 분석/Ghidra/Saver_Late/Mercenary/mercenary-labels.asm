
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

0046f7f0 <.text+0x6e7f0>:
  46f7f0:	6a ff                	push   0xffffffff
  46f7f2:	68 c6 09 48 00       	push   0x4809c6
  46f7f7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  46f7fd:	50                   	push   eax
  46f7fe:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  46f805:	81 ec 9c 00 00 00    	sub    esp,0x9c
  46f80b:	53                   	push   ebx
  46f80c:	55                   	push   ebp
  46f80d:	56                   	push   esi
  46f80e:	8b b4 24 b8 00 00 00 	mov    esi,DWORD PTR [esp+0xb8]
  46f815:	8b e9                	mov    ebp,ecx
  46f817:	8d 44 24 2c          	lea    eax,[esp+0x2c]
  46f81b:	57                   	push   edi
  46f81c:	50                   	push   eax
  46f81d:	8b ce                	mov    ecx,esi
  46f81f:	ff 15 38 8f 49 00    	call   DWORD PTR ds:0x498f38
  46f825:	8b ce                	mov    ecx,esi
  46f827:	ff 15 04 8f 49 00    	call   DWORD PTR ds:0x498f04
  46f82d:	8b 9d 84 01 00 00    	mov    ebx,DWORD PTR [ebp+0x184]
  46f833:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46f837:	89 44 24 54          	mov    DWORD PTR [esp+0x54],eax
  46f83b:	83 c3 04             	add    ebx,0x4
  46f83e:	e8 55 9d 00 00       	call   0x479598
  46f843:	8d 8d c4 00 00 00    	lea    ecx,[ebp+0xc4]
  46f849:	c7 84 24 b4 00 00 00 	mov    DWORD PTR [esp+0xb4],0x0
  46f850:	00 00 00 00 
  46f854:	8b 11                	mov    edx,DWORD PTR [ecx]
  46f856:	89 54 24 18          	mov    DWORD PTR [esp+0x18],edx
  46f85a:	8b 41 04             	mov    eax,DWORD PTR [ecx+0x4]
  46f85d:	89 44 24 1c          	mov    DWORD PTR [esp+0x1c],eax
  46f861:	8b 51 08             	mov    edx,DWORD PTR [ecx+0x8]
  46f864:	89 54 24 20          	mov    DWORD PTR [esp+0x20],edx
  46f868:	8b 41 0c             	mov    eax,DWORD PTR [ecx+0xc]
  46f86b:	8b 4d 44             	mov    ecx,DWORD PTR [ebp+0x44]
  46f86e:	89 44 24 24          	mov    DWORD PTR [esp+0x24],eax
  46f872:	8b 45 40             	mov    eax,DWORD PTR [ebp+0x40]
  46f875:	51                   	push   ecx
  46f876:	8d 4c 24 1c          	lea    ecx,[esp+0x1c]
  46f87a:	50                   	push   eax
  46f87b:	51                   	push   ecx
  46f87c:	ff 15 fc 98 49 00    	call   DWORD PTR ds:0x4998fc
  46f882:	0f bf 93 98 00 00 00 	movsx  edx,WORD PTR [ebx+0x98]
  46f889:	52                   	push   edx
  46f88a:	8d 44 24 14          	lea    eax,[esp+0x14]
  46f88e:	68 e8 0e 49 00       	push   0x490ee8
  46f893:	50                   	push   eax
  46f894:	e8 35 9d 00 00       	call   0x4795ce
  46f899:	8b 85 ac 00 00 00    	mov    eax,DWORD PTR [ebp+0xac]
  46f89f:	83 c4 0c             	add    esp,0xc
  46f8a2:	8b 08                	mov    ecx,DWORD PTR [eax]
  46f8a4:	6a 00                	push   0x0
  46f8a6:	6a 00                	push   0x0
  46f8a8:	6a ff                	push   0xffffffff
  46f8aa:	85 c9                	test   ecx,ecx
  46f8ac:	6a 00                	push   0x0
  46f8ae:	6a 00                	push   0x0
  46f8b0:	74 52                	je     0x46f904
  46f8b2:	83 ec 7c             	sub    esp,0x7c
  46f8b5:	b9 1f 00 00 00       	mov    ecx,0x1f
  46f8ba:	8d b4 24 c0 00 00 00 	lea    esi,[esp+0xc0]
  46f8c1:	8b fc                	mov    edi,esp
  46f8c3:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46f8c5:	8b 8c 24 a0 00 00 00 	mov    ecx,DWORD PTR [esp+0xa0]
  46f8cc:	6a 00                	push   0x0
  46f8ce:	51                   	push   ecx
  46f8cf:	8b 8c 24 b0 00 00 00 	mov    ecx,DWORD PTR [esp+0xb0]
  46f8d6:	83 ec 10             	sub    esp,0x10
  46f8d9:	8b d4                	mov    edx,esp
  46f8db:	89 0a                	mov    DWORD PTR [edx],ecx
  46f8dd:	8b 8c 24 c4 00 00 00 	mov    ecx,DWORD PTR [esp+0xc4]
  46f8e4:	89 4a 04             	mov    DWORD PTR [edx+0x4],ecx
  46f8e7:	8b 8c 24 c8 00 00 00 	mov    ecx,DWORD PTR [esp+0xc8]
  46f8ee:	89 4a 08             	mov    DWORD PTR [edx+0x8],ecx
  46f8f1:	8b 8c 24 cc 00 00 00 	mov    ecx,DWORD PTR [esp+0xcc]
  46f8f8:	89 4a 0c             	mov    DWORD PTR [edx+0xc],ecx
  46f8fb:	8b c8                	mov    ecx,eax
  46f8fd:	e8 ec 3c f9 ff       	call   0x4035ee
  46f902:	eb 4d                	jmp    0x46f951
  46f904:	8b 54 24 24          	mov    edx,DWORD PTR [esp+0x24]
  46f908:	83 ec 7c             	sub    esp,0x7c
  46f90b:	8b fc                	mov    edi,esp
  46f90d:	b9 1f 00 00 00       	mov    ecx,0x1f
  46f912:	6a 00                	push   0x0
  46f914:	8d b4 24 c4 00 00 00 	lea    esi,[esp+0xc4]
  46f91b:	52                   	push   edx
  46f91c:	8b 94 24 b0 00 00 00 	mov    edx,DWORD PTR [esp+0xb0]
  46f923:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46f925:	83 ec 10             	sub    esp,0x10
  46f928:	8b cc                	mov    ecx,esp
  46f92a:	89 11                	mov    DWORD PTR [ecx],edx
  46f92c:	8b 94 24 c4 00 00 00 	mov    edx,DWORD PTR [esp+0xc4]
  46f933:	89 51 04             	mov    DWORD PTR [ecx+0x4],edx
  46f936:	8b 94 24 c8 00 00 00 	mov    edx,DWORD PTR [esp+0xc8]
  46f93d:	89 51 08             	mov    DWORD PTR [ecx+0x8],edx
  46f940:	8b 94 24 cc 00 00 00 	mov    edx,DWORD PTR [esp+0xcc]
  46f947:	89 51 0c             	mov    DWORD PTR [ecx+0xc],edx
  46f94a:	8b c8                	mov    ecx,eax
  46f94c:	e8 f7 1e f9 ff       	call   0x401848
  46f951:	0f bf b3 96 00 00 00 	movsx  esi,WORD PTR [ebx+0x96]
  46f958:	83 fe ff             	cmp    esi,0xffffffff
  46f95b:	89 74 24 28          	mov    DWORD PTR [esp+0x28],esi
  46f95f:	75 25                	jne    0x46f986
  46f961:	8b 85 88 01 00 00    	mov    eax,DWORD PTR [ebp+0x188]
  46f967:	3b c6                	cmp    eax,esi
  46f969:	0f 84 4f 02 00 00    	je     0x46fbbe
  46f96f:	8b 8d 78 01 00 00    	mov    ecx,DWORD PTR [ebp+0x178]
  46f975:	8b 04 81             	mov    eax,DWORD PTR [ecx+eax*4]
  46f978:	3b c6                	cmp    eax,esi
  46f97a:	89 44 24 28          	mov    DWORD PTR [esp+0x28],eax
  46f97e:	0f 84 3a 02 00 00    	je     0x46fbbe
  46f984:	8b f0                	mov    esi,eax
  46f986:	56                   	push   esi
  46f987:	ff 15 54 9a 49 00    	call   DWORD PTR ds:0x499a54
  46f98d:	83 c4 04             	add    esp,0x4
  46f990:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46f994:	8b d8                	mov    ebx,eax
  46f996:	e8 fd 9b 00 00       	call   0x479598
  46f99b:	8d 95 d4 00 00 00    	lea    edx,[ebp+0xd4]
  46f9a1:	c6 84 24 b4 00 00 00 	mov    BYTE PTR [esp+0xb4],0x1
  46f9a8:	01 
  46f9a9:	8b 02                	mov    eax,DWORD PTR [edx]
  46f9ab:	89 44 24 18          	mov    DWORD PTR [esp+0x18],eax
  46f9af:	8b 4a 04             	mov    ecx,DWORD PTR [edx+0x4]
  46f9b2:	89 4c 24 1c          	mov    DWORD PTR [esp+0x1c],ecx
  46f9b6:	8b 42 08             	mov    eax,DWORD PTR [edx+0x8]
  46f9b9:	89 44 24 20          	mov    DWORD PTR [esp+0x20],eax
  46f9bd:	8b 45 40             	mov    eax,DWORD PTR [ebp+0x40]
  46f9c0:	8b 4a 0c             	mov    ecx,DWORD PTR [edx+0xc]
  46f9c3:	8d 54 24 18          	lea    edx,[esp+0x18]
  46f9c7:	89 4c 24 24          	mov    DWORD PTR [esp+0x24],ecx
  46f9cb:	8b 4d 44             	mov    ecx,DWORD PTR [ebp+0x44]
  46f9ce:	51                   	push   ecx
  46f9cf:	50                   	push   eax
  46f9d0:	52                   	push   edx
  46f9d1:	ff 15 fc 98 49 00    	call   DWORD PTR ds:0x4998fc
  46f9d7:	85 db                	test   ebx,ebx
  46f9d9:	0f 84 ce 01 00 00    	je     0x46fbad
  46f9df:	68 ff 00 00 00       	push   0xff
  46f9e4:	e8 a3 9b 00 00       	call   0x47958c
  46f9e9:	8b d0                	mov    edx,eax
  46f9eb:	8d bb a0 00 00 00    	lea    edi,[ebx+0xa0]
  46f9f1:	83 c4 04             	add    esp,0x4
  46f9f4:	89 54 24 2c          	mov    DWORD PTR [esp+0x2c],edx
  46f9f8:	c6 02 00             	mov    BYTE PTR [edx],0x0
  46f9fb:	8a 07                	mov    al,BYTE PTR [edi]
  46f9fd:	84 c0                	test   al,al
  46f9ff:	74 21                	je     0x46fa22
  46fa01:	83 c9 ff             	or     ecx,0xffffffff
  46fa04:	33 c0                	xor    eax,eax
  46fa06:	f2 ae                	repnz scas al,BYTE PTR es:[edi]
  46fa08:	f7 d1                	not    ecx
  46fa0a:	2b f9                	sub    edi,ecx
  46fa0c:	8b c1                	mov    eax,ecx
  46fa0e:	8b f7                	mov    esi,edi
  46fa10:	8b fa                	mov    edi,edx
  46fa12:	c1 e9 02             	shr    ecx,0x2
  46fa15:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46fa17:	8b c8                	mov    ecx,eax
  46fa19:	83 e1 03             	and    ecx,0x3
  46fa1c:	f3 a4                	rep movs BYTE PTR es:[edi],BYTE PTR ds:[esi]
  46fa1e:	8b 74 24 28          	mov    esi,DWORD PTR [esp+0x28]
  46fa22:	52                   	push   edx
  46fa23:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46fa27:	68 dc 0e 49 00       	push   0x490edc
  46fa2c:	51                   	push   ecx
  46fa2d:	e8 9c 9b 00 00       	call   0x4795ce
  46fa32:	8b 54 24 38          	mov    edx,DWORD PTR [esp+0x38]
  46fa36:	52                   	push   edx
  46fa37:	e8 44 9b 00 00       	call   0x479580
  46fa3c:	8b 43 08             	mov    eax,DWORD PTR [ebx+0x8]
  46fa3f:	8d 4c 24 24          	lea    ecx,[esp+0x24]
  46fa43:	50                   	push   eax
  46fa44:	68 d0 0e 49 00       	push   0x490ed0
  46fa49:	51                   	push   ecx
  46fa4a:	e8 7f 9b 00 00       	call   0x4795ce
  46fa4f:	83 c4 1c             	add    esp,0x1c
  46fa52:	8d 54 24 14          	lea    edx,[esp+0x14]
  46fa56:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46fa5a:	52                   	push   edx
  46fa5b:	e8 90 9d 00 00       	call   0x4797f0
  46fa60:	8b 43 18             	mov    eax,DWORD PTR [ebx+0x18]
  46fa63:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46fa67:	50                   	push   eax
  46fa68:	68 c0 0e 49 00       	push   0x490ec0
  46fa6d:	51                   	push   ecx
  46fa6e:	e8 5b 9b 00 00       	call   0x4795ce
  46fa73:	83 c4 0c             	add    esp,0xc
  46fa76:	8d 54 24 14          	lea    edx,[esp+0x14]
  46fa7a:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46fa7e:	52                   	push   edx
  46fa7f:	e8 6c 9d 00 00       	call   0x4797f0
  46fa84:	8b 43 20             	mov    eax,DWORD PTR [ebx+0x20]
  46fa87:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46fa8b:	50                   	push   eax
  46fa8c:	68 b0 0e 49 00       	push   0x490eb0
  46fa91:	51                   	push   ecx
  46fa92:	e8 37 9b 00 00       	call   0x4795ce
  46fa97:	83 c4 0c             	add    esp,0xc
  46fa9a:	8d 54 24 14          	lea    edx,[esp+0x14]
  46fa9e:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46faa2:	52                   	push   edx
  46faa3:	e8 48 9d 00 00       	call   0x4797f0
  46faa8:	8b 83 80 00 00 00    	mov    eax,DWORD PTR [ebx+0x80]
  46faae:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46fab2:	50                   	push   eax
  46fab3:	68 a4 0e 49 00       	push   0x490ea4
  46fab8:	51                   	push   ecx
  46fab9:	e8 10 9b 00 00       	call   0x4795ce
  46fabe:	83 c4 0c             	add    esp,0xc
  46fac1:	8d 54 24 14          	lea    edx,[esp+0x14]
  46fac5:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46fac9:	52                   	push   edx
  46faca:	e8 21 9d 00 00       	call   0x4797f0
  46facf:	56                   	push   esi
  46fad0:	8b cd                	mov    ecx,ebp
  46fad2:	e8 27 26 f9 ff       	call   0x4020fe
  46fad7:	50                   	push   eax
  46fad8:	8d 44 24 18          	lea    eax,[esp+0x18]
  46fadc:	68 90 0e 49 00       	push   0x490e90
  46fae1:	50                   	push   eax
  46fae2:	e8 e7 9a 00 00       	call   0x4795ce
  46fae7:	83 c4 0c             	add    esp,0xc
  46faea:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46faee:	51                   	push   ecx
  46faef:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46faf3:	e8 f8 9c 00 00       	call   0x4797f0
  46faf8:	8b 85 ac 00 00 00    	mov    eax,DWORD PTR [ebp+0xac]
  46fafe:	6a 00                	push   0x0
  46fb00:	6a 00                	push   0x0
  46fb02:	6a ff                	push   0xffffffff
  46fb04:	8b 08                	mov    ecx,DWORD PTR [eax]
  46fb06:	6a 00                	push   0x0
  46fb08:	85 c9                	test   ecx,ecx
  46fb0a:	6a 00                	push   0x0
  46fb0c:	74 4f                	je     0x46fb5d
  46fb0e:	8b 54 24 24          	mov    edx,DWORD PTR [esp+0x24]
  46fb12:	83 ec 7c             	sub    esp,0x7c
  46fb15:	8b fc                	mov    edi,esp
  46fb17:	b9 1f 00 00 00       	mov    ecx,0x1f
  46fb1c:	6a 00                	push   0x0
  46fb1e:	8d b4 24 c4 00 00 00 	lea    esi,[esp+0xc4]
  46fb25:	52                   	push   edx
  46fb26:	8b 94 24 b0 00 00 00 	mov    edx,DWORD PTR [esp+0xb0]
  46fb2d:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46fb2f:	83 ec 10             	sub    esp,0x10
  46fb32:	8b cc                	mov    ecx,esp
  46fb34:	89 11                	mov    DWORD PTR [ecx],edx
  46fb36:	8b 94 24 c4 00 00 00 	mov    edx,DWORD PTR [esp+0xc4]
  46fb3d:	89 51 04             	mov    DWORD PTR [ecx+0x4],edx
  46fb40:	8b 94 24 c8 00 00 00 	mov    edx,DWORD PTR [esp+0xc8]
  46fb47:	89 51 08             	mov    DWORD PTR [ecx+0x8],edx
  46fb4a:	8b 94 24 cc 00 00 00 	mov    edx,DWORD PTR [esp+0xcc]
  46fb51:	89 51 0c             	mov    DWORD PTR [ecx+0xc],edx
  46fb54:	8b c8                	mov    ecx,eax
  46fb56:	e8 93 3a f9 ff       	call   0x4035ee
  46fb5b:	eb 50                	jmp    0x46fbad
  46fb5d:	83 ec 7c             	sub    esp,0x7c
  46fb60:	b9 1f 00 00 00       	mov    ecx,0x1f
  46fb65:	8d b4 24 c0 00 00 00 	lea    esi,[esp+0xc0]
  46fb6c:	8b fc                	mov    edi,esp
  46fb6e:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46fb70:	8b 8c 24 a0 00 00 00 	mov    ecx,DWORD PTR [esp+0xa0]
  46fb77:	6a 00                	push   0x0
  46fb79:	51                   	push   ecx
  46fb7a:	8b 8c 24 b0 00 00 00 	mov    ecx,DWORD PTR [esp+0xb0]
  46fb81:	83 ec 10             	sub    esp,0x10
  46fb84:	8b d4                	mov    edx,esp
  46fb86:	89 0a                	mov    DWORD PTR [edx],ecx
  46fb88:	8b 8c 24 c4 00 00 00 	mov    ecx,DWORD PTR [esp+0xc4]
  46fb8f:	89 4a 04             	mov    DWORD PTR [edx+0x4],ecx
  46fb92:	8b 8c 24 c8 00 00 00 	mov    ecx,DWORD PTR [esp+0xc8]
  46fb99:	89 4a 08             	mov    DWORD PTR [edx+0x8],ecx
  46fb9c:	8b 8c 24 cc 00 00 00 	mov    ecx,DWORD PTR [esp+0xcc]
  46fba3:	89 4a 0c             	mov    DWORD PTR [edx+0xc],ecx
  46fba6:	8b c8                	mov    ecx,eax
  46fba8:	e8 9b 1c f9 ff       	call   0x401848
  46fbad:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  46fbb1:	c6 84 24 b4 00 00 00 	mov    BYTE PTR [esp+0xb4],0x0
  46fbb8:	00 
  46fbb9:	e8 c8 99 00 00       	call   0x479586
  46fbbe:	8b 95 b0 00 00 00    	mov    edx,DWORD PTR [ebp+0xb0]
  46fbc4:	8b 42 4c             	mov    eax,DWORD PTR [edx+0x4c]
  46fbc7:	50                   	push   eax
  46fbc8:	8d 44 24 14          	lea    eax,[esp+0x14]
  46fbcc:	68 90 04 49 00       	push   0x490490
  46fbd1:	50                   	push   eax
  46fbd2:	e8 f7 99 00 00       	call   0x4795ce
  46fbd7:	8b 45 40             	mov    eax,DWORD PTR [ebp+0x40]
  46fbda:	8b 4d 44             	mov    ecx,DWORD PTR [ebp+0x44]
  46fbdd:	8b ad ac 00 00 00    	mov    ebp,DWORD PTR [ebp+0xac]
  46fbe3:	83 c4 0c             	add    esp,0xc
  46fbe6:	8d 91 f3 00 00 00    	lea    edx,[ecx+0xf3]
  46fbec:	83 c0 74             	add    eax,0x74
  46fbef:	8b 4d 00             	mov    ecx,DWORD PTR [ebp+0x0]
  46fbf2:	6a 02                	push   0x2
  46fbf4:	85 c9                	test   ecx,ecx
  46fbf6:	74 2b                	je     0x46fc23
  46fbf8:	83 ec 7c             	sub    esp,0x7c
  46fbfb:	b9 1f 00 00 00       	mov    ecx,0x1f
  46fc00:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  46fc07:	8b fc                	mov    edi,esp
  46fc09:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46fc0b:	8b 8c 24 90 00 00 00 	mov    ecx,DWORD PTR [esp+0x90]
  46fc12:	68 ff 00 00 00       	push   0xff
  46fc17:	51                   	push   ecx
  46fc18:	52                   	push   edx
  46fc19:	50                   	push   eax
  46fc1a:	8b cd                	mov    ecx,ebp
  46fc1c:	e8 03 3b f9 ff       	call   0x403724
  46fc21:	eb 29                	jmp    0x46fc4c
  46fc23:	83 ec 7c             	sub    esp,0x7c
  46fc26:	b9 1f 00 00 00       	mov    ecx,0x1f
  46fc2b:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  46fc32:	8b fc                	mov    edi,esp
  46fc34:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  46fc36:	8b 8c 24 90 00 00 00 	mov    ecx,DWORD PTR [esp+0x90]
  46fc3d:	68 ff ff 00 00       	push   0xffff
  46fc42:	51                   	push   ecx
  46fc43:	52                   	push   edx
  46fc44:	50                   	push   eax
  46fc45:	8b cd                	mov    ecx,ebp
  46fc47:	e8 3e 34 f9 ff       	call   0x40308a
  46fc4c:	8b 8c 24 bc 00 00 00 	mov    ecx,DWORD PTR [esp+0xbc]
  46fc53:	ff 15 00 8f 49 00    	call   DWORD PTR ds:0x498f00
  46fc59:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  46fc5d:	c7                   	.byte 0xc7
  46fc5e:	84                   	.byte 0x84
  46fc5f:	24                   	.byte 0x24
