
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

0044b1c0 <.text+0x4a1c0>:
  44b1c0:	6a ff                	push   0xffffffff
  44b1c2:	68 70 e8 47 00       	push   0x47e870
  44b1c7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  44b1cd:	50                   	push   eax
  44b1ce:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  44b1d5:	83 ec 14             	sub    esp,0x14
  44b1d8:	55                   	push   ebp
  44b1d9:	56                   	push   esi
  44b1da:	8b f1                	mov    esi,ecx
  44b1dc:	57                   	push   edi
  44b1dd:	89 74 24 14          	mov    DWORD PTR [esp+0x14],esi
  44b1e1:	8b 8e 4c 03 00 00    	mov    ecx,DWORD PTR [esi+0x34c]
  44b1e7:	83 f9 ff             	cmp    ecx,0xffffffff
  44b1ea:	0f 84 3a 05 00 00    	je     0x44b72a
  44b1f0:	8b 86 50 03 00 00    	mov    eax,DWORD PTR [esi+0x350]
  44b1f6:	85 c0                	test   eax,eax
  44b1f8:	74 23                	je     0x44b21d
  44b1fa:	8b 86 b0 00 00 00    	mov    eax,DWORD PTR [esi+0xb0]
  44b200:	8b 50 44             	mov    edx,DWORD PTR [eax+0x44]
  44b203:	85 d2                	test   edx,edx
  44b205:	74 11                	je     0x44b218
  44b207:	8b 78 48             	mov    edi,DWORD PTR [eax+0x48]
  44b20a:	85 ff                	test   edi,edi
  44b20c:	74 0a                	je     0x44b218
  44b20e:	3b 48 40             	cmp    ecx,DWORD PTR [eax+0x40]
  44b211:	7d 05                	jge    0x44b218
  44b213:	8b 04 8a             	mov    eax,DWORD PTR [edx+ecx*4]
  44b216:	eb 0e                	jmp    0x44b226
  44b218:	83 c8 ff             	or     eax,0xffffffff
  44b21b:	eb 09                	jmp    0x44b226
  44b21d:	8b 86 bc 00 00 00    	mov    eax,DWORD PTR [esi+0xbc]
  44b223:	8b 04 88             	mov    eax,DWORD PTR [eax+ecx*4]
  44b226:	50                   	push   eax
  44b227:	ff 15 64 9a 49 00    	call   DWORD PTR ds:0x499a64
  44b22d:	8b e8                	mov    ebp,eax
  44b22f:	83 c4 04             	add    esp,0x4
  44b232:	85 ed                	test   ebp,ebp
  44b234:	0f 84 05 05 00 00    	je     0x44b73f
  44b23a:	8b b6 54 03 00 00    	mov    esi,DWORD PTR [esi+0x354]
  44b240:	53                   	push   ebx
  44b241:	8d 4e 1c             	lea    ecx,[esi+0x1c]
  44b244:	e8 bd e7 02 00       	call   0x479a06
  44b249:	68 ff 00 00 00       	push   0xff
  44b24e:	c7 46 4c 00 00 00 00 	mov    DWORD PTR [esi+0x4c],0x0
  44b255:	e8 32 e3 02 00       	call   0x47958c
  44b25a:	8d bd 94 00 00 00    	lea    edi,[ebp+0x94]
  44b260:	8b d8                	mov    ebx,eax
  44b262:	83 c4 04             	add    esp,0x4
  44b265:	80 3f 00             	cmp    BYTE PTR [edi],0x0
  44b268:	74 1a                	je     0x44b284
  44b26a:	83 c9 ff             	or     ecx,0xffffffff
  44b26d:	33 c0                	xor    eax,eax
  44b26f:	f2 ae                	repnz scas al,BYTE PTR es:[edi]
  44b271:	f7 d1                	not    ecx
  44b273:	2b f9                	sub    edi,ecx
  44b275:	8b d1                	mov    edx,ecx
  44b277:	8b f7                	mov    esi,edi
  44b279:	8b fb                	mov    edi,ebx
  44b27b:	c1 e9 02             	shr    ecx,0x2
  44b27e:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44b280:	8b ca                	mov    ecx,edx
  44b282:	eb 1d                	jmp    0x44b2a1
  44b284:	bf 6c 07 49 00       	mov    edi,0x49076c
  44b289:	83 c9 ff             	or     ecx,0xffffffff
  44b28c:	33 c0                	xor    eax,eax
  44b28e:	f2 ae                	repnz scas al,BYTE PTR es:[edi]
  44b290:	f7 d1                	not    ecx
  44b292:	2b f9                	sub    edi,ecx
  44b294:	8b c1                	mov    eax,ecx
  44b296:	8b f7                	mov    esi,edi
  44b298:	8b fb                	mov    edi,ebx
  44b29a:	c1 e9 02             	shr    ecx,0x2
  44b29d:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44b29f:	8b c8                	mov    ecx,eax
  44b2a1:	83 e1 03             	and    ecx,0x3
  44b2a4:	f3 a4                	rep movs BYTE PTR es:[edi],BYTE PTR ds:[esi]
  44b2a6:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b2aa:	e8 e9 e2 02 00       	call   0x479598
  44b2af:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b2b3:	c7 44 24 2c 00 00 00 	mov    DWORD PTR [esp+0x2c],0x0
  44b2ba:	00 
  44b2bb:	e8 d8 e2 02 00       	call   0x479598
  44b2c0:	68 64 07 49 00       	push   0x490764
  44b2c5:	8d 4c 24 24          	lea    ecx,[esp+0x24]
  44b2c9:	c6 44 24 30 01       	mov    BYTE PTR [esp+0x30],0x1
  44b2ce:	e8 d1 e2 02 00       	call   0x4795a4
  44b2d3:	68 5c 07 49 00       	push   0x49075c
  44b2d8:	8d 4c 24 20          	lea    ecx,[esp+0x20]
  44b2dc:	c6 44 24 30 02       	mov    BYTE PTR [esp+0x30],0x2
  44b2e1:	e8 be e2 02 00       	call   0x4795a4
  44b2e6:	8b 74 24 18          	mov    esi,DWORD PTR [esp+0x18]
  44b2ea:	c6 44 24 2c 03       	mov    BYTE PTR [esp+0x2c],0x3
  44b2ef:	8b 8e b0 00 00 00    	mov    ecx,DWORD PTR [esi+0xb0]
  44b2f5:	8b 41 3c             	mov    eax,DWORD PTR [ecx+0x3c]
  44b2f8:	8b 4d 10             	mov    ecx,DWORD PTR [ebp+0x10]
  44b2fb:	0f bf 50 0c          	movsx  edx,WORD PTR [eax+0xc]
  44b2ff:	3b ca                	cmp    ecx,edx
  44b301:	7f 22                	jg     0x44b325
  44b303:	0f bf 40 0a          	movsx  eax,WORD PTR [eax+0xa]
  44b307:	50                   	push   eax
  44b308:	ff 15 54 9a 49 00    	call   DWORD PTR ds:0x499a54
  44b30e:	83 c4 04             	add    esp,0x4
  44b311:	85 c0                	test   eax,eax
  44b313:	74 10                	je     0x44b325
  44b315:	8b 40 58             	mov    eax,DWORD PTR [eax+0x58]
  44b318:	8a 4c 28 18          	mov    cl,BYTE PTR [eax+ebp*1+0x18]
  44b31c:	8b 44 24 20          	mov    eax,DWORD PTR [esp+0x20]
  44b320:	80 f9 01             	cmp    cl,0x1
  44b323:	74 04                	je     0x44b329
  44b325:	8b 44 24 1c          	mov    eax,DWORD PTR [esp+0x1c]
  44b329:	8b 4d 2c             	mov    ecx,DWORD PTR [ebp+0x2c]
  44b32c:	8b 55 28             	mov    edx,DWORD PTR [ebp+0x28]
  44b32f:	50                   	push   eax
  44b330:	51                   	push   ecx
  44b331:	52                   	push   edx
  44b332:	53                   	push   ebx
  44b333:	8d 44 24 20          	lea    eax,[esp+0x20]
  44b337:	68 30 07 49 00       	push   0x490730
  44b33c:	50                   	push   eax
  44b33d:	e8 8c e2 02 00       	call   0x4795ce
  44b342:	83 c4 18             	add    esp,0x18
  44b345:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b349:	51                   	push   ecx
  44b34a:	8b 8e 54 03 00 00    	mov    ecx,DWORD PTR [esi+0x354]
  44b350:	e8 76 73 fb ff       	call   0x4026cb
  44b355:	8a 85 94 01 00 00    	mov    al,BYTE PTR [ebp+0x194]
  44b35b:	8d bd 94 01 00 00    	lea    edi,[ebp+0x194]
  44b361:	84 c0                	test   al,al
  44b363:	74 1a                	je     0x44b37f
  44b365:	83 c9 ff             	or     ecx,0xffffffff
  44b368:	33 c0                	xor    eax,eax
  44b36a:	f2 ae                	repnz scas al,BYTE PTR es:[edi]
  44b36c:	f7 d1                	not    ecx
  44b36e:	2b f9                	sub    edi,ecx
  44b370:	8b d1                	mov    edx,ecx
  44b372:	8b f7                	mov    esi,edi
  44b374:	8b fb                	mov    edi,ebx
  44b376:	c1 e9 02             	shr    ecx,0x2
  44b379:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44b37b:	8b ca                	mov    ecx,edx
  44b37d:	eb 1d                	jmp    0x44b39c
  44b37f:	bf 24 07 49 00       	mov    edi,0x490724
  44b384:	83 c9 ff             	or     ecx,0xffffffff
  44b387:	33 c0                	xor    eax,eax
  44b389:	f2 ae                	repnz scas al,BYTE PTR es:[edi]
  44b38b:	f7 d1                	not    ecx
  44b38d:	2b f9                	sub    edi,ecx
  44b38f:	8b c1                	mov    eax,ecx
  44b391:	8b f7                	mov    esi,edi
  44b393:	8b fb                	mov    edi,ebx
  44b395:	c1 e9 02             	shr    ecx,0x2
  44b398:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  44b39a:	8b c8                	mov    ecx,eax
  44b39c:	83 e1 03             	and    ecx,0x3
  44b39f:	53                   	push   ebx
  44b3a0:	f3 a4                	rep movs BYTE PTR es:[edi],BYTE PTR ds:[esi]
  44b3a2:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b3a6:	68 18 07 49 00       	push   0x490718
  44b3ab:	51                   	push   ecx
  44b3ac:	e8 1d e2 02 00       	call   0x4795ce
  44b3b1:	8b 74 24 24          	mov    esi,DWORD PTR [esp+0x24]
  44b3b5:	83 c4 0c             	add    esp,0xc
  44b3b8:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b3bc:	8b 8e 54 03 00 00    	mov    ecx,DWORD PTR [esi+0x354]
  44b3c2:	52                   	push   edx
  44b3c3:	e8 03 73 fb ff       	call   0x4026cb
  44b3c8:	8b 45 34             	mov    eax,DWORD PTR [ebp+0x34]
  44b3cb:	85 c0                	test   eax,eax
  44b3cd:	74 21                	je     0x44b3f0
  44b3cf:	50                   	push   eax
  44b3d0:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b3d4:	68 08 07 49 00       	push   0x490708
  44b3d9:	50                   	push   eax
  44b3da:	e8 ef e1 02 00       	call   0x4795ce
  44b3df:	83 c4 0c             	add    esp,0xc
  44b3e2:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b3e6:	51                   	push   ecx
  44b3e7:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b3eb:	e8 00 e4 02 00       	call   0x4797f0
  44b3f0:	8b 45 38             	mov    eax,DWORD PTR [ebp+0x38]
  44b3f3:	85 c0                	test   eax,eax
  44b3f5:	74 21                	je     0x44b418
  44b3f7:	50                   	push   eax
  44b3f8:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b3fc:	68 fc 06 49 00       	push   0x4906fc
  44b401:	52                   	push   edx
  44b402:	e8 c7 e1 02 00       	call   0x4795ce
  44b407:	83 c4 0c             	add    esp,0xc
  44b40a:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b40e:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b412:	50                   	push   eax
  44b413:	e8 d8 e3 02 00       	call   0x4797f0
  44b418:	8b 45 3c             	mov    eax,DWORD PTR [ebp+0x3c]
  44b41b:	85 c0                	test   eax,eax
  44b41d:	74 21                	je     0x44b440
  44b41f:	50                   	push   eax
  44b420:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b424:	68 ec 06 49 00       	push   0x4906ec
  44b429:	51                   	push   ecx
  44b42a:	e8 9f e1 02 00       	call   0x4795ce
  44b42f:	83 c4 0c             	add    esp,0xc
  44b432:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b436:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b43a:	52                   	push   edx
  44b43b:	e8 b0 e3 02 00       	call   0x4797f0
  44b440:	8b 45 40             	mov    eax,DWORD PTR [ebp+0x40]
  44b443:	85 c0                	test   eax,eax
  44b445:	74 21                	je     0x44b468
  44b447:	50                   	push   eax
  44b448:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b44c:	68 dc 06 49 00       	push   0x4906dc
  44b451:	50                   	push   eax
  44b452:	e8 77 e1 02 00       	call   0x4795ce
  44b457:	83 c4 0c             	add    esp,0xc
  44b45a:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b45e:	51                   	push   ecx
  44b45f:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b463:	e8 88 e3 02 00       	call   0x4797f0
  44b468:	8b 45 44             	mov    eax,DWORD PTR [ebp+0x44]
  44b46b:	85 c0                	test   eax,eax
  44b46d:	74 21                	je     0x44b490
  44b46f:	50                   	push   eax
  44b470:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b474:	68 cc 06 49 00       	push   0x4906cc
  44b479:	52                   	push   edx
  44b47a:	e8 4f e1 02 00       	call   0x4795ce
  44b47f:	83 c4 0c             	add    esp,0xc
  44b482:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b486:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b48a:	50                   	push   eax
  44b48b:	e8 60 e3 02 00       	call   0x4797f0
  44b490:	8b 45 48             	mov    eax,DWORD PTR [ebp+0x48]
  44b493:	85 c0                	test   eax,eax
  44b495:	74 21                	je     0x44b4b8
  44b497:	50                   	push   eax
  44b498:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b49c:	68 bc 06 49 00       	push   0x4906bc
  44b4a1:	51                   	push   ecx
  44b4a2:	e8 27 e1 02 00       	call   0x4795ce
  44b4a7:	83 c4 0c             	add    esp,0xc
  44b4aa:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b4ae:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b4b2:	52                   	push   edx
  44b4b3:	e8 38 e3 02 00       	call   0x4797f0
  44b4b8:	8b 45 4c             	mov    eax,DWORD PTR [ebp+0x4c]
  44b4bb:	85 c0                	test   eax,eax
  44b4bd:	74 21                	je     0x44b4e0
  44b4bf:	50                   	push   eax
  44b4c0:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b4c4:	68 a8 06 49 00       	push   0x4906a8
  44b4c9:	50                   	push   eax
  44b4ca:	e8 ff e0 02 00       	call   0x4795ce
  44b4cf:	83 c4 0c             	add    esp,0xc
  44b4d2:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b4d6:	51                   	push   ecx
  44b4d7:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b4db:	e8 10 e3 02 00       	call   0x4797f0
  44b4e0:	8b 45 50             	mov    eax,DWORD PTR [ebp+0x50]
  44b4e3:	85 c0                	test   eax,eax
  44b4e5:	74 21                	je     0x44b508
  44b4e7:	50                   	push   eax
  44b4e8:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b4ec:	68 94 06 49 00       	push   0x490694
  44b4f1:	52                   	push   edx
  44b4f2:	e8 d7 e0 02 00       	call   0x4795ce
  44b4f7:	83 c4 0c             	add    esp,0xc
  44b4fa:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b4fe:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b502:	50                   	push   eax
  44b503:	e8 e8 e2 02 00       	call   0x4797f0
  44b508:	8b 45 54             	mov    eax,DWORD PTR [ebp+0x54]
  44b50b:	85 c0                	test   eax,eax
  44b50d:	74 21                	je     0x44b530
  44b50f:	50                   	push   eax
  44b510:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b514:	68 84 06 49 00       	push   0x490684
  44b519:	51                   	push   ecx
  44b51a:	e8 af e0 02 00       	call   0x4795ce
  44b51f:	83 c4 0c             	add    esp,0xc
  44b522:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b526:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b52a:	52                   	push   edx
  44b52b:	e8 c0 e2 02 00       	call   0x4797f0
  44b530:	8b 45 58             	mov    eax,DWORD PTR [ebp+0x58]
  44b533:	85 c0                	test   eax,eax
  44b535:	74 21                	je     0x44b558
  44b537:	50                   	push   eax
  44b538:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b53c:	68 74 06 49 00       	push   0x490674
  44b541:	50                   	push   eax
  44b542:	e8 87 e0 02 00       	call   0x4795ce
  44b547:	83 c4 0c             	add    esp,0xc
  44b54a:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b54e:	51                   	push   ecx
  44b54f:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b553:	e8 98 e2 02 00       	call   0x4797f0
  44b558:	8b 45 5c             	mov    eax,DWORD PTR [ebp+0x5c]
  44b55b:	85 c0                	test   eax,eax
  44b55d:	74 21                	je     0x44b580
  44b55f:	50                   	push   eax
  44b560:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b564:	68 64 06 49 00       	push   0x490664
  44b569:	52                   	push   edx
  44b56a:	e8 5f e0 02 00       	call   0x4795ce
  44b56f:	83 c4 0c             	add    esp,0xc
  44b572:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b576:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b57a:	50                   	push   eax
  44b57b:	e8 70 e2 02 00       	call   0x4797f0
  44b580:	8b 45 60             	mov    eax,DWORD PTR [ebp+0x60]
  44b583:	85 c0                	test   eax,eax
  44b585:	74 21                	je     0x44b5a8
  44b587:	50                   	push   eax
  44b588:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b58c:	68 54 06 49 00       	push   0x490654
  44b591:	51                   	push   ecx
  44b592:	e8 37 e0 02 00       	call   0x4795ce
  44b597:	83 c4 0c             	add    esp,0xc
  44b59a:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b59e:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b5a2:	52                   	push   edx
  44b5a3:	e8 48 e2 02 00       	call   0x4797f0
  44b5a8:	8b 45 64             	mov    eax,DWORD PTR [ebp+0x64]
  44b5ab:	85 c0                	test   eax,eax
  44b5ad:	74 21                	je     0x44b5d0
  44b5af:	50                   	push   eax
  44b5b0:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b5b4:	68 44 06 49 00       	push   0x490644
  44b5b9:	50                   	push   eax
  44b5ba:	e8 0f e0 02 00       	call   0x4795ce
  44b5bf:	83 c4 0c             	add    esp,0xc
  44b5c2:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b5c6:	51                   	push   ecx
  44b5c7:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b5cb:	e8 20 e2 02 00       	call   0x4797f0
  44b5d0:	8b 45 68             	mov    eax,DWORD PTR [ebp+0x68]
  44b5d3:	85 c0                	test   eax,eax
  44b5d5:	74 21                	je     0x44b5f8
  44b5d7:	50                   	push   eax
  44b5d8:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b5dc:	68 30 06 49 00       	push   0x490630
  44b5e1:	52                   	push   edx
  44b5e2:	e8 e7 df 02 00       	call   0x4795ce
  44b5e7:	83 c4 0c             	add    esp,0xc
  44b5ea:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b5ee:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b5f2:	50                   	push   eax
  44b5f3:	e8 f8 e1 02 00       	call   0x4797f0
  44b5f8:	8b 45 6c             	mov    eax,DWORD PTR [ebp+0x6c]
  44b5fb:	85 c0                	test   eax,eax
  44b5fd:	74 21                	je     0x44b620
  44b5ff:	50                   	push   eax
  44b600:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b604:	68 1c 06 49 00       	push   0x49061c
  44b609:	51                   	push   ecx
  44b60a:	e8 bf df 02 00       	call   0x4795ce
  44b60f:	83 c4 0c             	add    esp,0xc
  44b612:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b616:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b61a:	52                   	push   edx
  44b61b:	e8 d0 e1 02 00       	call   0x4797f0
  44b620:	8b 45 74             	mov    eax,DWORD PTR [ebp+0x74]
  44b623:	85 c0                	test   eax,eax
  44b625:	74 21                	je     0x44b648
  44b627:	50                   	push   eax
  44b628:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b62c:	68 04 06 49 00       	push   0x490604
  44b631:	50                   	push   eax
  44b632:	e8 97 df 02 00       	call   0x4795ce
  44b637:	83 c4 0c             	add    esp,0xc
  44b63a:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b63e:	51                   	push   ecx
  44b63f:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b643:	e8 a8 e1 02 00       	call   0x4797f0
  44b648:	8b 45 78             	mov    eax,DWORD PTR [ebp+0x78]
  44b64b:	85 c0                	test   eax,eax
  44b64d:	74 21                	je     0x44b670
  44b64f:	50                   	push   eax
  44b650:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b654:	68 ec 05 49 00       	push   0x4905ec
  44b659:	52                   	push   edx
  44b65a:	e8 6f df 02 00       	call   0x4795ce
  44b65f:	83 c4 0c             	add    esp,0xc
  44b662:	8d 44 24 10          	lea    eax,[esp+0x10]
  44b666:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b66a:	50                   	push   eax
  44b66b:	e8 80 e1 02 00       	call   0x4797f0
  44b670:	8b 45 7c             	mov    eax,DWORD PTR [ebp+0x7c]
  44b673:	85 c0                	test   eax,eax
  44b675:	74 21                	je     0x44b698
  44b677:	50                   	push   eax
  44b678:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b67c:	68 d4 05 49 00       	push   0x4905d4
  44b681:	51                   	push   ecx
  44b682:	e8 47 df 02 00       	call   0x4795ce
  44b687:	83 c4 0c             	add    esp,0xc
  44b68a:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b68e:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b692:	52                   	push   edx
  44b693:	e8 58 e1 02 00       	call   0x4797f0
  44b698:	8b ad 80 00 00 00    	mov    ebp,DWORD PTR [ebp+0x80]
  44b69e:	85 ed                	test   ebp,ebp
  44b6a0:	74 21                	je     0x44b6c3
  44b6a2:	55                   	push   ebp
  44b6a3:	8d 44 24 14          	lea    eax,[esp+0x14]
  44b6a7:	68 c0 05 49 00       	push   0x4905c0
  44b6ac:	50                   	push   eax
  44b6ad:	e8 1c df 02 00       	call   0x4795ce
  44b6b2:	83 c4 0c             	add    esp,0xc
  44b6b5:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b6b9:	51                   	push   ecx
  44b6ba:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  44b6be:	e8 2d e1 02 00       	call   0x4797f0
  44b6c3:	8b 8e 54 03 00 00    	mov    ecx,DWORD PTR [esi+0x354]
  44b6c9:	8d 54 24 14          	lea    edx,[esp+0x14]
  44b6cd:	52                   	push   edx
  44b6ce:	e8 f8 6f fb ff       	call   0x4026cb
  44b6d3:	53                   	push   ebx
  44b6d4:	e8 a7 de 02 00       	call   0x479580
  44b6d9:	83 c4 04             	add    esp,0x4
  44b6dc:	8d 4c 24 1c          	lea    ecx,[esp+0x1c]
  44b6e0:	c6 44 24 2c 02       	mov    BYTE PTR [esp+0x2c],0x2
  44b6e5:	e8 9c de 02 00       	call   0x479586
  44b6ea:	8d 4c 24 20          	lea    ecx,[esp+0x20]
  44b6ee:	c6 44 24 2c 01       	mov    BYTE PTR [esp+0x2c],0x1
  44b6f3:	e8 8e de 02 00       	call   0x479586
  44b6f8:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b6fc:	c6 44 24 2c 00       	mov    BYTE PTR [esp+0x2c],0x0
  44b701:	e8 80 de 02 00       	call   0x479586
  44b706:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b70a:	c7 44 24 2c ff ff ff 	mov    DWORD PTR [esp+0x2c],0xffffffff
  44b711:	ff 
  44b712:	e8 6f de 02 00       	call   0x479586
  44b717:	5b                   	pop    ebx
  44b718:	5f                   	pop    edi
  44b719:	5e                   	pop    esi
  44b71a:	5d                   	pop    ebp
  44b71b:	8b 4c 24 14          	mov    ecx,DWORD PTR [esp+0x14]
  44b71f:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44b726:	83 c4 20             	add    esp,0x20
  44b729:	c3                   	ret
  44b72a:	8b b6 54 03 00 00    	mov    esi,DWORD PTR [esi+0x354]
  44b730:	8d 4e 1c             	lea    ecx,[esi+0x1c]
  44b733:	e8 ce e2 02 00       	call   0x479a06
  44b738:	c7 46 4c 00 00 00 00 	mov    DWORD PTR [esi+0x4c],0x0
  44b73f:	8b 4c 24 20          	mov    ecx,DWORD PTR [esp+0x20]
  44b743:	5f                   	pop    edi
  44b744:	5e                   	pop    esi
  44b745:	5d                   	pop    ebp
  44b746:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44b74d:	83 c4 20             	add    esp,0x20
  44b750:	c3                   	ret
  44b751:	90                   	nop
  44b752:	90                   	nop
  44b753:	90                   	nop
  44b754:	90                   	nop
  44b755:	90                   	nop
  44b756:	90                   	nop
  44b757:	90                   	nop
  44b758:	90                   	nop
  44b759:	90                   	nop
  44b75a:	90                   	nop
  44b75b:	90                   	nop
  44b75c:	90                   	nop
  44b75d:	90                   	nop
  44b75e:	90                   	nop
  44b75f:	90                   	nop
  44b760:	cc                   	int3
  44b761:	cc                   	int3
  44b762:	cc                   	int3
  44b763:	cc                   	int3
  44b764:	cc                   	int3
  44b765:	cc                   	int3
  44b766:	cc                   	int3
  44b767:	cc                   	int3
  44b768:	cc                   	int3
  44b769:	cc                   	int3
  44b76a:	cc                   	int3
  44b76b:	cc                   	int3
  44b76c:	cc                   	int3
  44b76d:	cc                   	int3
  44b76e:	cc                   	int3
  44b76f:	cc                   	int3
  44b770:	cc                   	int3
  44b771:	cc                   	int3
  44b772:	cc                   	int3
  44b773:	cc                   	int3
  44b774:	cc                   	int3
  44b775:	cc                   	int3
  44b776:	cc                   	int3
  44b777:	cc                   	int3
  44b778:	cc                   	int3
  44b779:	cc                   	int3
  44b77a:	cc                   	int3
  44b77b:	cc                   	int3
  44b77c:	cc                   	int3
  44b77d:	cc                   	int3
  44b77e:	cc                   	int3
  44b77f:	cc                   	int3
  44b780:	cc                   	int3
  44b781:	cc                   	int3
  44b782:	cc                   	int3
  44b783:	cc                   	int3
  44b784:	cc                   	int3
  44b785:	cc                   	int3
  44b786:	cc                   	int3
  44b787:	cc                   	int3
  44b788:	cc                   	int3
  44b789:	cc                   	int3
  44b78a:	cc                   	int3
  44b78b:	cc                   	int3
  44b78c:	cc                   	int3
  44b78d:	cc                   	int3
  44b78e:	cc                   	int3
  44b78f:	cc                   	int3
  44b790:	cc                   	int3
  44b791:	cc                   	int3
  44b792:	cc                   	int3
  44b793:	cc                   	int3
  44b794:	cc                   	int3
  44b795:	cc                   	int3
  44b796:	cc                   	int3
  44b797:	cc                   	int3
  44b798:	cc                   	int3
  44b799:	cc                   	int3
  44b79a:	cc                   	int3
  44b79b:	cc                   	int3
  44b79c:	cc                   	int3
  44b79d:	cc                   	int3
  44b79e:	cc                   	int3
  44b79f:	cc                   	int3
  44b7a0:	cc                   	int3
  44b7a1:	cc                   	int3
  44b7a2:	cc                   	int3
  44b7a3:	cc                   	int3
  44b7a4:	cc                   	int3
  44b7a5:	cc                   	int3
  44b7a6:	cc                   	int3
  44b7a7:	cc                   	int3
  44b7a8:	cc                   	int3
  44b7a9:	cc                   	int3
  44b7aa:	cc                   	int3
  44b7ab:	cc                   	int3
  44b7ac:	cc                   	int3
  44b7ad:	cc                   	int3
  44b7ae:	cc                   	int3
  44b7af:	cc                   	int3
  44b7b0:	cc                   	int3
  44b7b1:	cc                   	int3
  44b7b2:	cc                   	int3
  44b7b3:	cc                   	int3
  44b7b4:	cc                   	int3
  44b7b5:	cc                   	int3
  44b7b6:	cc                   	int3
  44b7b7:	cc                   	int3
  44b7b8:	cc                   	int3
  44b7b9:	cc                   	int3
  44b7ba:	cc                   	int3
  44b7bb:	cc                   	int3
  44b7bc:	cc                   	int3
  44b7bd:	cc                   	int3
  44b7be:	cc                   	int3
  44b7bf:	cc                   	int3
  44b7c0:	cc                   	int3
  44b7c1:	cc                   	int3
  44b7c2:	cc                   	int3
  44b7c3:	cc                   	int3
  44b7c4:	cc                   	int3
  44b7c5:	cc                   	int3
  44b7c6:	cc                   	int3
  44b7c7:	cc                   	int3
  44b7c8:	cc                   	int3
  44b7c9:	cc                   	int3
  44b7ca:	cc                   	int3
  44b7cb:	cc                   	int3
  44b7cc:	cc                   	int3
  44b7cd:	cc                   	int3
  44b7ce:	cc                   	int3
  44b7cf:	cc                   	int3
  44b7d0:	cc                   	int3
  44b7d1:	cc                   	int3
  44b7d2:	cc                   	int3
  44b7d3:	cc                   	int3
  44b7d4:	cc                   	int3
  44b7d5:	cc                   	int3
  44b7d6:	cc                   	int3
  44b7d7:	cc                   	int3
  44b7d8:	cc                   	int3
  44b7d9:	cc                   	int3
  44b7da:	cc                   	int3
  44b7db:	cc                   	int3
  44b7dc:	cc                   	int3
  44b7dd:	cc                   	int3
  44b7de:	cc                   	int3
  44b7df:	cc                   	int3
  44b7e0:	cc                   	int3
  44b7e1:	cc                   	int3
  44b7e2:	cc                   	int3
  44b7e3:	cc                   	int3
  44b7e4:	cc                   	int3
  44b7e5:	cc                   	int3
  44b7e6:	cc                   	int3
  44b7e7:	cc                   	int3
  44b7e8:	cc                   	int3
  44b7e9:	cc                   	int3
  44b7ea:	cc                   	int3
  44b7eb:	cc                   	int3
  44b7ec:	cc                   	int3
  44b7ed:	cc                   	int3
  44b7ee:	cc                   	int3
  44b7ef:	cc                   	int3
  44b7f0:	cc                   	int3
  44b7f1:	cc                   	int3
  44b7f2:	cc                   	int3
  44b7f3:	cc                   	int3
  44b7f4:	cc                   	int3
  44b7f5:	cc                   	int3
  44b7f6:	cc                   	int3
  44b7f7:	cc                   	int3
  44b7f8:	cc                   	int3
  44b7f9:	cc                   	int3
  44b7fa:	cc                   	int3
  44b7fb:	cc                   	int3
  44b7fc:	cc                   	int3
  44b7fd:	cc                   	int3
  44b7fe:	cc                   	int3
  44b7ff:	cc                   	int3
  44b800:	cc                   	int3
  44b801:	cc                   	int3
  44b802:	cc                   	int3
  44b803:	cc                   	int3
  44b804:	cc                   	int3
  44b805:	cc                   	int3
  44b806:	cc                   	int3
  44b807:	cc                   	int3
  44b808:	cc                   	int3
  44b809:	cc                   	int3
  44b80a:	cc                   	int3
  44b80b:	cc                   	int3
  44b80c:	cc                   	int3
  44b80d:	cc                   	int3
  44b80e:	cc                   	int3
  44b80f:	cc                   	int3
  44b810:	cc                   	int3
  44b811:	cc                   	int3
  44b812:	cc                   	int3
  44b813:	cc                   	int3
  44b814:	cc                   	int3
  44b815:	cc                   	int3
  44b816:	cc                   	int3
  44b817:	cc                   	int3
  44b818:	cc                   	int3
  44b819:	cc                   	int3
  44b81a:	cc                   	int3
  44b81b:	cc                   	int3
  44b81c:	cc                   	int3
  44b81d:	cc                   	int3
  44b81e:	cc                   	int3
  44b81f:	cc                   	int3
  44b820:	cc                   	int3
  44b821:	cc                   	int3
  44b822:	cc                   	int3
  44b823:	cc                   	int3
  44b824:	cc                   	int3
  44b825:	cc                   	int3
  44b826:	cc                   	int3
  44b827:	cc                   	int3
  44b828:	cc                   	int3
  44b829:	cc                   	int3
  44b82a:	cc                   	int3
  44b82b:	cc                   	int3
  44b82c:	cc                   	int3
  44b82d:	cc                   	int3
  44b82e:	cc                   	int3
  44b82f:	cc                   	int3
  44b830:	cc                   	int3
  44b831:	cc                   	int3
  44b832:	cc                   	int3
  44b833:	cc                   	int3
  44b834:	cc                   	int3
  44b835:	cc                   	int3
  44b836:	cc                   	int3
  44b837:	cc                   	int3
  44b838:	cc                   	int3
  44b839:	cc                   	int3
  44b83a:	cc                   	int3
  44b83b:	cc                   	int3
  44b83c:	cc                   	int3
  44b83d:	cc                   	int3
  44b83e:	cc                   	int3
  44b83f:	cc                   	int3
  44b840:	cc                   	int3
  44b841:	cc                   	int3
  44b842:	cc                   	int3
  44b843:	cc                   	int3
  44b844:	cc                   	int3
  44b845:	cc                   	int3
  44b846:	cc                   	int3
  44b847:	cc                   	int3
  44b848:	cc                   	int3
  44b849:	cc                   	int3
  44b84a:	cc                   	int3
  44b84b:	cc                   	int3
  44b84c:	cc                   	int3
  44b84d:	cc                   	int3
  44b84e:	cc                   	int3
  44b84f:	cc                   	int3
  44b850:	cc                   	int3
  44b851:	cc                   	int3
  44b852:	cc                   	int3
  44b853:	cc                   	int3
  44b854:	cc                   	int3
  44b855:	cc                   	int3
  44b856:	cc                   	int3
  44b857:	cc                   	int3
  44b858:	cc                   	int3
  44b859:	cc                   	int3
  44b85a:	cc                   	int3
  44b85b:	cc                   	int3
  44b85c:	cc                   	int3
  44b85d:	cc                   	int3
  44b85e:	cc                   	int3
  44b85f:	cc                   	int3
  44b860:	cc                   	int3
  44b861:	cc                   	int3
  44b862:	cc                   	int3
  44b863:	cc                   	int3
  44b864:	cc                   	int3
  44b865:	cc                   	int3
  44b866:	cc                   	int3
  44b867:	cc                   	int3
  44b868:	cc                   	int3
  44b869:	cc                   	int3
  44b86a:	cc                   	int3
  44b86b:	cc                   	int3
  44b86c:	cc                   	int3
  44b86d:	cc                   	int3
  44b86e:	cc                   	int3
  44b86f:	cc                   	int3
  44b870:	cc                   	int3
  44b871:	cc                   	int3
  44b872:	cc                   	int3
  44b873:	cc                   	int3
  44b874:	cc                   	int3
  44b875:	cc                   	int3
  44b876:	cc                   	int3
  44b877:	cc                   	int3
  44b878:	cc                   	int3
  44b879:	cc                   	int3
  44b87a:	cc                   	int3
  44b87b:	cc                   	int3
  44b87c:	cc                   	int3
  44b87d:	cc                   	int3
  44b87e:	cc                   	int3
  44b87f:	cc                   	int3
  44b880:	cc                   	int3
  44b881:	cc                   	int3
  44b882:	cc                   	int3
  44b883:	cc                   	int3
  44b884:	cc                   	int3
  44b885:	cc                   	int3
  44b886:	cc                   	int3
  44b887:	cc                   	int3
  44b888:	cc                   	int3
  44b889:	cc                   	int3
  44b88a:	cc                   	int3
  44b88b:	cc                   	int3
  44b88c:	cc                   	int3
  44b88d:	cc                   	int3
  44b88e:	cc                   	int3
  44b88f:	cc                   	int3
  44b890:	cc                   	int3
  44b891:	cc                   	int3
  44b892:	cc                   	int3
  44b893:	cc                   	int3
  44b894:	cc                   	int3
  44b895:	cc                   	int3
  44b896:	cc                   	int3
  44b897:	cc                   	int3
  44b898:	cc                   	int3
  44b899:	cc                   	int3
  44b89a:	cc                   	int3
  44b89b:	cc                   	int3
  44b89c:	cc                   	int3
  44b89d:	cc                   	int3
  44b89e:	cc                   	int3
  44b89f:	cc                   	int3
  44b8a0:	cc                   	int3
  44b8a1:	cc                   	int3
  44b8a2:	cc                   	int3
  44b8a3:	cc                   	int3
  44b8a4:	cc                   	int3
  44b8a5:	cc                   	int3
  44b8a6:	cc                   	int3
  44b8a7:	cc                   	int3
  44b8a8:	cc                   	int3
  44b8a9:	cc                   	int3
  44b8aa:	cc                   	int3
  44b8ab:	cc                   	int3
  44b8ac:	cc                   	int3
  44b8ad:	cc                   	int3
  44b8ae:	cc                   	int3
  44b8af:	cc                   	int3
  44b8b0:	cc                   	int3
  44b8b1:	cc                   	int3
  44b8b2:	cc                   	int3
  44b8b3:	cc                   	int3
  44b8b4:	cc                   	int3
  44b8b5:	cc                   	int3
  44b8b6:	cc                   	int3
  44b8b7:	cc                   	int3
  44b8b8:	cc                   	int3
  44b8b9:	cc                   	int3
  44b8ba:	cc                   	int3
  44b8bb:	cc                   	int3
  44b8bc:	cc                   	int3
  44b8bd:	cc                   	int3
  44b8be:	cc                   	int3
  44b8bf:	cc                   	int3
  44b8c0:	cc                   	int3
  44b8c1:	cc                   	int3
  44b8c2:	cc                   	int3
  44b8c3:	cc                   	int3
  44b8c4:	cc                   	int3
  44b8c5:	cc                   	int3
  44b8c6:	cc                   	int3
  44b8c7:	cc                   	int3
  44b8c8:	cc                   	int3
  44b8c9:	cc                   	int3
  44b8ca:	cc                   	int3
  44b8cb:	cc                   	int3
  44b8cc:	cc                   	int3
  44b8cd:	cc                   	int3
  44b8ce:	cc                   	int3
  44b8cf:	cc                   	int3
