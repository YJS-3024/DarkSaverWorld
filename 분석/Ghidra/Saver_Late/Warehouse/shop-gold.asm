
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

0044a790 <.text+0x49790>:
  44a790:	6a ff                	push   0xffffffff
  44a792:	68 fb e7 47 00       	push   0x47e7fb
  44a797:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  44a79d:	50                   	push   eax
  44a79e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  44a7a5:	81 ec 88 00 00 00    	sub    esp,0x88
  44a7ab:	53                   	push   ebx
  44a7ac:	8b 9c 24 9c 00 00 00 	mov    ebx,DWORD PTR [esp+0x9c]
  44a7b3:	55                   	push   ebp
  44a7b4:	56                   	push   esi
  44a7b5:	8b f1                	mov    esi,ecx
  44a7b7:	8d 44 24 18          	lea    eax,[esp+0x18]
  44a7bb:	57                   	push   edi
  44a7bc:	50                   	push   eax
  44a7bd:	8b cb                	mov    ecx,ebx
  44a7bf:	ff 15 38 8f 49 00    	call   DWORD PTR ds:0x498f38
  44a7c5:	8b cb                	mov    ecx,ebx
  44a7c7:	ff 15 04 8f 49 00    	call   DWORD PTR ds:0x498f04
  44a7cd:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44a7d1:	89 44 24 40          	mov    DWORD PTR [esp+0x40],eax
  44a7d5:	e8 be ed 02 00       	call   0x479598
  44a7da:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  44a7dd:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  44a7e0:	81 c1 0a 01 00 00    	add    ecx,0x10a
  44a7e6:	8d 54 24 10          	lea    edx,[esp+0x10]
  44a7ea:	89 4c 24 18          	mov    DWORD PTR [esp+0x18],ecx
  44a7ee:	8b 8e b0 00 00 00    	mov    ecx,DWORD PTR [esi+0xb0]
  44a7f4:	8d 68 72             	lea    ebp,[eax+0x72]
  44a7f7:	c7 84 24 a0 00 00 00 	mov    DWORD PTR [esp+0xa0],0x0
  44a7fe:	00 00 00 00 
  44a802:	8b 41 4c             	mov    eax,DWORD PTR [ecx+0x4c]
  44a805:	50                   	push   eax
  44a806:	68 90 04 49 00       	push   0x490490
  44a80b:	52                   	push   edx
  44a80c:	e8 bd ed 02 00       	call   0x4795ce
  44a811:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  44a817:	83 c4 0c             	add    esp,0xc
  44a81a:	8b 08                	mov    ecx,DWORD PTR [eax]
  44a81c:	6a 02                	push   0x2
  44a81e:	85 c9                	test   ecx,ecx
  44a820:	74 2f                	je     0x44a851
  44a822:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  44a826:	83 ec 7c             	sub    esp,0x7c
  44a829:	b9 1f 00 00 00       	mov    ecx,0x1f
  44a82e:	8d b4 24 9c 00 00 00 	lea    esi,[esp+0x9c]
  44a835:	8b fc                	mov    edi,esp
  44a837:	68 ff 00 00 00       	push   0xff
  44a83c:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44a83e:	8b 8c 24 94 00 00 00 	mov    ecx,DWORD PTR [esp+0x94]
  44a845:	51                   	push   ecx
  44a846:	52                   	push   edx
  44a847:	55                   	push   ebp
  44a848:	8b c8                	mov    ecx,eax
  44a84a:	e8 d5 8e fb ff       	call   0x403724
  44a84f:	eb 2d                	jmp    0x44a87e
  44a851:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  44a855:	83 ec 7c             	sub    esp,0x7c
  44a858:	b9 1f 00 00 00       	mov    ecx,0x1f
  44a85d:	8d b4 24 9c 00 00 00 	lea    esi,[esp+0x9c]
  44a864:	8b fc                	mov    edi,esp
  44a866:	68 ff ff 00 00       	push   0xffff
  44a86b:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44a86d:	8b 8c 24 94 00 00 00 	mov    ecx,DWORD PTR [esp+0x94]
  44a874:	51                   	push   ecx
  44a875:	52                   	push   edx
  44a876:	55                   	push   ebp
  44a877:	8b c8                	mov    ecx,eax
  44a879:	e8 0c 88 fb ff       	call   0x40308a
  44a87e:	8b cb                	mov    ecx,ebx
  44a880:	ff 15 00 8f 49 00    	call   DWORD PTR ds:0x498f00
  44a886:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44a88a:	c7 84 24 a0 00 00 00 	mov    DWORD PTR [esp+0xa0],0xffffffff
  44a891:	ff ff ff ff 
  44a895:	e8 ec ec 02 00       	call   0x479586
  44a89a:	8b 8c 24 98 00 00 00 	mov    ecx,DWORD PTR [esp+0x98]
  44a8a1:	5f                   	pop    edi
  44a8a2:	5e                   	pop    esi
  44a8a3:	5d                   	pop    ebp
  44a8a4:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44a8ab:	5b                   	pop    ebx
  44a8ac:	81 c4 94 00 00 00    	add    esp,0x94
  44a8b2:	c2 04 00             	ret    0x4
  44a8b5:	90                   	nop
  44a8b6:	90                   	nop
  44a8b7:	90                   	nop
  44a8b8:	90                   	nop
  44a8b9:	90                   	nop
  44a8ba:	90                   	nop
  44a8bb:	90                   	nop
  44a8bc:	90                   	nop
  44a8bd:	90                   	nop
  44a8be:	90                   	nop
  44a8bf:	90                   	nop
  44a8c0:	cc                   	int3
  44a8c1:	cc                   	int3
  44a8c2:	cc                   	int3
  44a8c3:	cc                   	int3
  44a8c4:	cc                   	int3
  44a8c5:	cc                   	int3
  44a8c6:	cc                   	int3
  44a8c7:	cc                   	int3
  44a8c8:	cc                   	int3
  44a8c9:	cc                   	int3
  44a8ca:	cc                   	int3
  44a8cb:	cc                   	int3
  44a8cc:	cc                   	int3
  44a8cd:	cc                   	int3
  44a8ce:	cc                   	int3
  44a8cf:	cc                   	int3
  44a8d0:	cc                   	int3
  44a8d1:	cc                   	int3
  44a8d2:	cc                   	int3
  44a8d3:	cc                   	int3
  44a8d4:	cc                   	int3
  44a8d5:	cc                   	int3
  44a8d6:	cc                   	int3
  44a8d7:	cc                   	int3
  44a8d8:	cc                   	int3
  44a8d9:	cc                   	int3
  44a8da:	cc                   	int3
  44a8db:	cc                   	int3
  44a8dc:	cc                   	int3
  44a8dd:	cc                   	int3
  44a8de:	cc                   	int3
  44a8df:	cc                   	int3
  44a8e0:	cc                   	int3
  44a8e1:	cc                   	int3
  44a8e2:	cc                   	int3
  44a8e3:	cc                   	int3
  44a8e4:	cc                   	int3
  44a8e5:	cc                   	int3
  44a8e6:	cc                   	int3
  44a8e7:	cc                   	int3
  44a8e8:	cc                   	int3
  44a8e9:	cc                   	int3
  44a8ea:	cc                   	int3
  44a8eb:	cc                   	int3
  44a8ec:	cc                   	int3
  44a8ed:	cc                   	int3
  44a8ee:	cc                   	int3
  44a8ef:	cc                   	int3
