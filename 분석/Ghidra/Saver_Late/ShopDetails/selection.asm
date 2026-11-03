
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

0044aa70 <.text+0x49a70>:
  44aa70:	83 ec 10             	sub    esp,0x10
  44aa73:	56                   	push   esi
  44aa74:	8b f1                	mov    esi,ecx
  44aa76:	57                   	push   edi
  44aa77:	8b 86 4c 03 00 00    	mov    eax,DWORD PTR [esi+0x34c]
  44aa7d:	83 f8 ff             	cmp    eax,0xffffffff
  44aa80:	0f 84 51 01 00 00    	je     0x44abd7
  44aa86:	8b 8e 50 03 00 00    	mov    ecx,DWORD PTR [esi+0x350]
  44aa8c:	85 c9                	test   ecx,ecx
  44aa8e:	0f 84 93 00 00 00    	je     0x44ab27
  44aa94:	85 c0                	test   eax,eax
  44aa96:	0f 8c 3b 01 00 00    	jl     0x44abd7
  44aa9c:	83 f8 1c             	cmp    eax,0x1c
  44aa9f:	0f 8d 32 01 00 00    	jge    0x44abd7
  44aaa5:	8b 8e b0 00 00 00    	mov    ecx,DWORD PTR [esi+0xb0]
  44aaab:	8b 51 44             	mov    edx,DWORD PTR [ecx+0x44]
  44aaae:	85 d2                	test   edx,edx
  44aab0:	74 12                	je     0x44aac4
  44aab2:	8b 79 48             	mov    edi,DWORD PTR [ecx+0x48]
  44aab5:	85 ff                	test   edi,edi
  44aab7:	74 0b                	je     0x44aac4
  44aab9:	3b 41 40             	cmp    eax,DWORD PTR [ecx+0x40]
  44aabc:	7d 06                	jge    0x44aac4
  44aabe:	83 3c 82 ff          	cmp    DWORD PTR [edx+eax*4],0xffffffff
  44aac2:	75 27                	jne    0x44aaeb
  44aac4:	c7 86 4c 03 00 00 ff 	mov    DWORD PTR [esi+0x34c],0xffffffff
  44aacb:	ff ff ff 
  44aace:	8b b6 54 03 00 00    	mov    esi,DWORD PTR [esi+0x354]
  44aad4:	8d 4e 1c             	lea    ecx,[esi+0x1c]
  44aad7:	e8 2a ef 02 00       	call   0x479a06
  44aadc:	c7 46 4c 00 00 00 00 	mov    DWORD PTR [esi+0x4c],0x0
  44aae3:	5f                   	pop    edi
  44aae4:	5e                   	pop    esi
  44aae5:	83 c4 10             	add    esp,0x10
  44aae8:	c2 04 00             	ret    0x4
  44aaeb:	c1 e0 04             	shl    eax,0x4
  44aaee:	8d 84 30 c4 00 00 00 	lea    eax,[eax+esi*1+0xc4]
  44aaf5:	8b c8                	mov    ecx,eax
  44aaf7:	8b 11                	mov    edx,DWORD PTR [ecx]
  44aaf9:	89 54 24 08          	mov    DWORD PTR [esp+0x8],edx
  44aafd:	8b 41 04             	mov    eax,DWORD PTR [ecx+0x4]
  44ab00:	89 44 24 0c          	mov    DWORD PTR [esp+0xc],eax
  44ab04:	8b 41 08             	mov    eax,DWORD PTR [ecx+0x8]
  44ab07:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  44ab0b:	40                   	inc    eax
  44ab0c:	8b 49 0c             	mov    ecx,DWORD PTR [ecx+0xc]
  44ab0f:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  44ab13:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  44ab16:	41                   	inc    ecx
  44ab17:	89 4c 24 14          	mov    DWORD PTR [esp+0x14],ecx
  44ab1b:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  44ab1e:	51                   	push   ecx
  44ab1f:	8d 4c 24 0c          	lea    ecx,[esp+0xc]
  44ab23:	50                   	push   eax
  44ab24:	51                   	push   ecx
  44ab25:	eb 52                	jmp    0x44ab79
  44ab27:	2b 86 c0 00 00 00    	sub    eax,DWORD PTR [esi+0xc0]
  44ab2d:	0f 88 a4 00 00 00    	js     0x44abd7
  44ab33:	83 f8 0c             	cmp    eax,0xc
  44ab36:	0f 8d 9b 00 00 00    	jge    0x44abd7
  44ab3c:	c1 e0 04             	shl    eax,0x4
  44ab3f:	8d 94 30 84 02 00 00 	lea    edx,[eax+esi*1+0x284]
  44ab46:	8b 84 30 84 02 00 00 	mov    eax,DWORD PTR [eax+esi*1+0x284]
  44ab4d:	89 44 24 08          	mov    DWORD PTR [esp+0x8],eax
  44ab51:	8b 4a 04             	mov    ecx,DWORD PTR [edx+0x4]
  44ab54:	89 4c 24 0c          	mov    DWORD PTR [esp+0xc],ecx
  44ab58:	8b 42 08             	mov    eax,DWORD PTR [edx+0x8]
  44ab5b:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  44ab5f:	40                   	inc    eax
  44ab60:	8b 4a 0c             	mov    ecx,DWORD PTR [edx+0xc]
  44ab63:	89 44 24 10          	mov    DWORD PTR [esp+0x10],eax
  44ab67:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  44ab6a:	41                   	inc    ecx
  44ab6b:	89 4c 24 14          	mov    DWORD PTR [esp+0x14],ecx
  44ab6f:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  44ab72:	51                   	push   ecx
  44ab73:	8d 54 24 0c          	lea    edx,[esp+0xc]
  44ab77:	50                   	push   eax
  44ab78:	52                   	push   edx
  44ab79:	ff 15 fc 98 49 00    	call   DWORD PTR ds:0x4998fc
  44ab7f:	53                   	push   ebx
  44ab80:	8b 5c 24 20          	mov    ebx,DWORD PTR [esp+0x20]
  44ab84:	8b cb                	mov    ecx,ebx
  44ab86:	ff 15 cc 8e 49 00    	call   DWORD PTR ds:0x498ecc
  44ab8c:	8b f8                	mov    edi,eax
  44ab8e:	81 c6 44 03 00 00    	add    esi,0x344
  44ab94:	56                   	push   esi
  44ab95:	8b cf                	mov    ecx,edi
  44ab97:	e8 d8 ef 02 00       	call   0x479b74
  44ab9c:	8b f0                	mov    esi,eax
  44ab9e:	8b 07                	mov    eax,DWORD PTR [edi]
  44aba0:	6a 05                	push   0x5
  44aba2:	8b cf                	mov    ecx,edi
  44aba4:	ff 50 2c             	call   DWORD PTR [eax+0x2c]
  44aba7:	8b 4c 24 18          	mov    ecx,DWORD PTR [esp+0x18]
  44abab:	8b 54 24 14          	mov    edx,DWORD PTR [esp+0x14]
  44abaf:	8b 44 24 10          	mov    eax,DWORD PTR [esp+0x10]
  44abb3:	51                   	push   ecx
  44abb4:	8b 4c 24 10          	mov    ecx,DWORD PTR [esp+0x10]
  44abb8:	52                   	push   edx
  44abb9:	8b 57 04             	mov    edx,DWORD PTR [edi+0x4]
  44abbc:	50                   	push   eax
  44abbd:	51                   	push   ecx
  44abbe:	52                   	push   edx
  44abbf:	ff 15 a0 8f 49 00    	call   DWORD PTR ds:0x498fa0
  44abc5:	56                   	push   esi
  44abc6:	8b cf                	mov    ecx,edi
  44abc8:	e8 a7 ef 02 00       	call   0x479b74
  44abcd:	57                   	push   edi
  44abce:	8b cb                	mov    ecx,ebx
  44abd0:	ff 15 6c 8e 49 00    	call   DWORD PTR ds:0x498e6c
  44abd6:	5b                   	pop    ebx
  44abd7:	5f                   	pop    edi
  44abd8:	5e                   	pop    esi
  44abd9:	83 c4 10             	add    esp,0x10
  44abdc:	c2 04 00             	ret    0x4
  44abdf:	90                   	nop
  44abe0:	cc                   	int3
  44abe1:	cc                   	int3
  44abe2:	cc                   	int3
  44abe3:	cc                   	int3
  44abe4:	cc                   	int3
  44abe5:	cc                   	int3
  44abe6:	cc                   	int3
  44abe7:	cc                   	int3
  44abe8:	cc                   	int3
  44abe9:	cc                   	int3
  44abea:	cc                   	int3
  44abeb:	cc                   	int3
  44abec:	cc                   	int3
  44abed:	cc                   	int3
  44abee:	cc                   	int3
  44abef:	cc                   	int3
  44abf0:	cc                   	int3
  44abf1:	cc                   	int3
  44abf2:	cc                   	int3
  44abf3:	cc                   	int3
  44abf4:	cc                   	int3
  44abf5:	cc                   	int3
  44abf6:	cc                   	int3
  44abf7:	cc                   	int3
  44abf8:	cc                   	int3
  44abf9:	cc                   	int3
  44abfa:	cc                   	int3
  44abfb:	cc                   	int3
  44abfc:	cc                   	int3
  44abfd:	cc                   	int3
  44abfe:	cc                   	int3
  44abff:	cc                   	int3
  44ac00:	cc                   	int3
  44ac01:	cc                   	int3
  44ac02:	cc                   	int3
  44ac03:	cc                   	int3
  44ac04:	cc                   	int3
  44ac05:	cc                   	int3
  44ac06:	cc                   	int3
  44ac07:	cc                   	int3
  44ac08:	cc                   	int3
  44ac09:	cc                   	int3
  44ac0a:	cc                   	int3
  44ac0b:	cc                   	int3
  44ac0c:	cc                   	int3
  44ac0d:	cc                   	int3
  44ac0e:	cc                   	int3
  44ac0f:	cc                   	int3
  44ac10:	cc                   	int3
  44ac11:	cc                   	int3
  44ac12:	cc                   	int3
  44ac13:	cc                   	int3
  44ac14:	cc                   	int3
  44ac15:	cc                   	int3
  44ac16:	cc                   	int3
  44ac17:	cc                   	int3
  44ac18:	cc                   	int3
  44ac19:	cc                   	int3
  44ac1a:	cc                   	int3
  44ac1b:	cc                   	int3
  44ac1c:	cc                   	int3
  44ac1d:	cc                   	int3
  44ac1e:	cc                   	int3
  44ac1f:	cc                   	int3
  44ac20:	cc                   	int3
  44ac21:	cc                   	int3
  44ac22:	cc                   	int3
  44ac23:	cc                   	int3
  44ac24:	cc                   	int3
  44ac25:	cc                   	int3
  44ac26:	cc                   	int3
  44ac27:	cc                   	int3
  44ac28:	cc                   	int3
  44ac29:	cc                   	int3
  44ac2a:	cc                   	int3
  44ac2b:	cc                   	int3
  44ac2c:	cc                   	int3
  44ac2d:	cc                   	int3
  44ac2e:	cc                   	int3
  44ac2f:	cc                   	int3
  44ac30:	cc                   	int3
  44ac31:	cc                   	int3
  44ac32:	cc                   	int3
  44ac33:	cc                   	int3
  44ac34:	cc                   	int3
  44ac35:	cc                   	int3
  44ac36:	cc                   	int3
  44ac37:	cc                   	int3
  44ac38:	cc                   	int3
  44ac39:	cc                   	int3
  44ac3a:	cc                   	int3
  44ac3b:	cc                   	int3
  44ac3c:	cc                   	int3
  44ac3d:	cc                   	int3
  44ac3e:	cc                   	int3
  44ac3f:	cc                   	int3
