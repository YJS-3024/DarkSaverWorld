
분석/Saver_1.2/extracted/Saver_Files/Saver/Saver.exe:     file format pei-i386


Disassembly of section .text:

0041a650 <.text+0x19650>:
  41a650:	60                   	pusha
  41a651:	02 00                	add    al,BYTE PTR [eax]
  41a653:	8b f0                	mov    esi,eax
  41a655:	8d 44 24 0c          	lea    eax,[esp+0xc]
  41a659:	68 88 00 00 00       	push   0x88
  41a65e:	50                   	push   eax
  41a65f:	56                   	push   esi
  41a660:	c7 44 24 18 00 00 00 	mov    DWORD PTR [esp+0x18],0x0
  41a667:	00 
  41a668:	e8 73 2a 02 00       	call   0x43d0e0
  41a66d:	66 8b 0f             	mov    cx,WORD PTR [edi]
  41a670:	8d 54 24 18          	lea    edx,[esp+0x18]
  41a674:	51                   	push   ecx
  41a675:	52                   	push   edx
  41a676:	56                   	push   esi
  41a677:	e8 44 2a 02 00       	call   0x43d0c0
  41a67c:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  41a680:	8b 0d e0 33 45 00    	mov    ecx,DWORD PTR ds:0x4533e0
  41a686:	83 c4 1c             	add    esp,0x1c
  41a689:	56                   	push   esi
  41a68a:	50                   	push   eax
  41a68b:	e8 b0 11 02 00       	call   0x43b840
  41a690:	56                   	push   esi
  41a691:	e8 98 60 02 00       	call   0x44072e
  41a696:	83 c4 04             	add    esp,0x4
  41a699:	5f                   	pop    edi
  41a69a:	5e                   	pop    esi
  41a69b:	59                   	pop    ecx
  41a69c:	c3                   	ret
  41a69d:	90                   	nop
  41a69e:	90                   	nop
  41a69f:	90                   	nop
  41a6a0:	51                   	push   ecx
  41a6a1:	56                   	push   esi
  41a6a2:	57                   	push   edi
  41a6a3:	8b b9 3c 01 00 00    	mov    edi,DWORD PTR [ecx+0x13c]
  41a6a9:	6a 0a                	push   0xa
  41a6ab:	83 c7 04             	add    edi,0x4
  41a6ae:	e8 75 60 02 00       	call   0x440728
  41a6b3:	8b f0                	mov    esi,eax
  41a6b5:	8d 44 24 0c          	lea    eax,[esp+0xc]
  41a6b9:	68 89 00 00 00       	push   0x89
  41a6be:	50                   	push   eax
  41a6bf:	56                   	push   esi
  41a6c0:	c7 44 24 18 00 00 00 	mov    DWORD PTR [esp+0x18],0x0
  41a6c7:	00 
  41a6c8:	e8 13 2a 02 00       	call   0x43d0e0
  41a6cd:	66 8b 0f             	mov    cx,WORD PTR [edi]
  41a6d0:	8d 54 24 18          	lea    edx,[esp+0x18]
  41a6d4:	51                   	push   ecx
  41a6d5:	52                   	push   edx
  41a6d6:	56                   	push   esi
  41a6d7:	e8 e4 29 02 00       	call   0x43d0c0
  41a6dc:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  41a6e0:	8b 0d e0 33 45 00    	mov    ecx,DWORD PTR ds:0x4533e0
  41a6e6:	83 c4 1c             	add    esp,0x1c
  41a6e9:	56                   	push   esi
  41a6ea:	50                   	push   eax
  41a6eb:	e8 50 11 02 00       	call   0x43b840
  41a6f0:	56                   	push   esi
  41a6f1:	e8 38 60 02 00       	call   0x44072e
  41a6f6:	83 c4 04             	add    esp,0x4
  41a6f9:	5f                   	pop    edi
  41a6fa:	5e                   	pop    esi
  41a6fb:	59                   	pop    ecx
  41a6fc:	c3                   	ret
  41a6fd:	90                   	nop
  41a6fe:	90                   	nop
  41a6ff:	90                   	nop
  41a700:	51                   	push   ecx
  41a701:	53                   	push   ebx
  41a702:	56                   	push   esi
  41a703:	8b f1                	mov    esi,ecx
  41a705:	8b 9e 3c 01 00 00    	mov    ebx,DWORD PTR [esi+0x13c]
  41a70b:	83 c3 04             	add    ebx,0x4
  41a70e:	66 83 7b 3a 00       	cmp    WORD PTR [ebx+0x3a],0x0
  41a713:	0f 84 ab 00 00 00    	je     0x41a7c4
  41a719:	8b 86 f8 00 00 00    	mov    eax,DWORD PTR [esi+0xf8]
  41a71f:	85 c0                	test   eax,eax
  41a721:	0f 84 9d 00 00 00    	je     0x41a7c4
  41a727:	57                   	push   edi
  41a728:	6a 0a                	push   0xa
  41a72a:	e8 f9 5f 02 00       	call   0x440728
  41a72f:	8b f8                	mov    edi,eax
  41a731:	8d 44 24 10          	lea    eax,[esp+0x10]
  41a735:	68 81 00 00 00       	push   0x81
  41a73a:	50                   	push   eax
  41a73b:	57                   	push   edi
  41a73c:	c7 44 24 1c 00 00 00 	mov    DWORD PTR [esp+0x1c],0x0
  41a743:	00 
  41a744:	e8 97 29 02 00       	call   0x43d0e0
  41a749:	66 8b 0b             	mov    cx,WORD PTR [ebx]
  41a74c:	8d 54 24 1c          	lea    edx,[esp+0x1c]
  41a750:	51                   	push   ecx
  41a751:	52                   	push   edx
  41a752:	57                   	push   edi
  41a753:	e8 68 29 02 00       	call   0x43d0c0
  41a758:	8b 86 f4 00 00 00    	mov    eax,DWORD PTR [esi+0xf4]
  41a75e:	83 c4 1c             	add    esp,0x1c
  41a761:	83 e8 00             	sub    eax,0x0
  41a764:	74 1d                	je     0x41a783
  41a766:	48                   	dec    eax
  41a767:	74 12                	je     0x41a77b
  41a769:	48                   	dec    eax
  41a76a:	75 1f                	jne    0x41a78b
  41a76c:	8b 86 fc 00 00 00    	mov    eax,DWORD PTR [esi+0xfc]
  41a772:	8b 84 86 00 01 00 00 	mov    eax,DWORD PTR [esi+eax*4+0x100]
  41a779:	eb 14                	jmp    0x41a78f
  41a77b:	8b 86 e8 00 00 00    	mov    eax,DWORD PTR [esi+0xe8]
  41a781:	eb 0c                	jmp    0x41a78f
  41a783:	8b 86 ec 00 00 00    	mov    eax,DWORD PTR [esi+0xec]
  41a789:	eb 04                	jmp    0x41a78f
  41a78b:	8b 44 24 0c          	mov    eax,DWORD PTR [esp+0xc]
  41a78f:	8d 4c 24 0c          	lea    ecx,[esp+0xc]
  41a793:	50                   	push   eax
  41a794:	51                   	push   ecx
  41a795:	57                   	push   edi
  41a796:	e8 25 29 02 00       	call   0x43d0c0
  41a79b:	83 c4 0c             	add    esp,0xc
  41a79e:	66 ff 4b 3a          	dec    WORD PTR [ebx+0x3a]
  41a7a2:	8b 54 24 0c          	mov    edx,DWORD PTR [esp+0xc]
  41a7a6:	8b 0d e0 33 45 00    	mov    ecx,DWORD PTR ds:0x4533e0
  41a7ac:	57                   	push   edi
  41a7ad:	52                   	push   edx
  41a7ae:	e8 8d 10 02 00       	call   0x43b840
  41a7b3:	57                   	push   edi
  41a7b4:	e8 75 5f 02 00       	call   0x44072e
  41a7b9:	83 c4 04             	add    esp,0x4
  41a7bc:	8b ce                	mov    ecx,esi
  41a7be:	e8 4d fa ff ff       	call   0x41a210
  41a7c3:	5f                   	pop    edi
  41a7c4:	5e                   	pop    esi
  41a7c5:	5b                   	pop    ebx
  41a7c6:	59                   	pop    ecx
  41a7c7:	c3                   	ret
  41a7c8:	90                   	nop
  41a7c9:	90                   	nop
  41a7ca:	90                   	nop
  41a7cb:	90                   	nop
  41a7cc:	90                   	nop
  41a7cd:	90                   	nop
  41a7ce:	90                   	nop
  41a7cf:	90                   	nop
  41a7d0:	51                   	push   ecx
  41a7d1:	56                   	push   esi
  41a7d2:	57                   	push   edi
  41a7d3:	8b f9                	mov    edi,ecx
  41a7d5:	6a 0a                	push   0xa
  41a7d7:	e8 4c 5f 02 00       	call   0x440728
  41a7dc:	8b f0                	mov    esi,eax
  41a7de:	8d 44 24 0c          	lea    eax,[esp+0xc]
  41a7e2:	68 8a 00 00 00       	push   0x8a
  41a7e7:	50                   	push   eax
  41a7e8:	56                   	push   esi
  41a7e9:	c7 44 24 18 00 00 00 	mov    DWORD PTR [esp+0x18],0x0
  41a7f0:	00 
  41a7f1:	e8 ea 28 02 00       	call   0x43d0e0
  41a7f6:	8b 8f 3c 01 00 00    	mov    ecx,DWORD PTR [edi+0x13c]
  41a7fc:	8d 44 24 18          	lea    eax,[esp+0x18]
  41a800:	66 8b 51 04          	mov    dx,WORD PTR [ecx+0x4]
  41a804:	52                   	push   edx
  41a805:	50                   	push   eax
  41a806:	56                   	push   esi
  41a807:	e8 b4 28 02 00       	call   0x43d0c0
  41a80c:	8b 4c 24 24          	mov    ecx,DWORD PTR [esp+0x24]
  41a810:	83 c4 1c             	add    esp,0x1c
  41a813:	56                   	push   esi
  41a814:	51                   	push   ecx
  41a815:	8b 0d e0 33 45 00    	mov    ecx,DWORD PTR ds:0x4533e0
  41a81b:	e8 20 10 02 00       	call   0x43b840
  41a820:	56                   	push   esi
  41a821:	e8 08 5f 02 00       	call   0x44072e
  41a826:	83 c4 04             	add    esp,0x4
  41a829:	5f                   	pop    edi
  41a82a:	5e                   	pop    esi
  41a82b:	59                   	pop    ecx
  41a82c:	c3                   	ret
  41a82d:	90                   	nop
  41a82e:	90                   	nop
  41a82f:	90                   	nop
  41a830:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  41a834:	53                   	push   ebx
  41a835:	8b d9                	mov    ebx,ecx
  41a837:	56                   	push   esi
  41a838:	50                   	push   eax
  41a839:	8b 8b b0 00 00 00    	mov    ecx,DWORD PTR [ebx+0xb0]
  41a83f:	e8 fc 1e 01 00       	call   0x42c740
  41a844:	85 c0                	test   eax,eax
  41a846:	89 83 3c 01 00 00    	mov    DWORD PTR [ebx+0x13c],eax
  41a84c:	75 0f                	jne    0x41a85d
  41a84e:	8b                   	.byte 0x8b
  41a84f:	8b                   	.byte 0x8b
