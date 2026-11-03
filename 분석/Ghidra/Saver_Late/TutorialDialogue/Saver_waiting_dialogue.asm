
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

00472000 <.text+0x71000>:
  472000:	8b 54 24 34          	mov    edx,DWORD PTR [esp+0x34]
  472004:	81 e2 ff 00 00 00    	and    edx,0xff
  47200a:	52                   	push   edx
  47200b:	53                   	push   ebx
  47200c:	50                   	push   eax
  47200d:	57                   	push   edi
  47200e:	e8 66 03 f9 ff       	call   0x402379
  472013:	83 c4 18             	add    esp,0x18
  472016:	8d 4e 10             	lea    ecx,[esi+0x10]
  472019:	53                   	push   ebx
  47201a:	e8 97 75 00 00       	call   0x4795b6
  47201f:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  472023:	51                   	push   ecx
  472024:	57                   	push   edi
  472025:	e8 53 13 f9 ff       	call   0x40337d
  47202a:	8a 10                	mov    dl,BYTE PTR [eax]
  47202c:	8d 4c 24 18          	lea    ecx,[esp+0x18]
  472030:	88 54 24 34          	mov    BYTE PTR [esp+0x34],dl
  472034:	8b 44 24 34          	mov    eax,DWORD PTR [esp+0x34]
  472038:	25 ff 00 00 00       	and    eax,0xff
  47203d:	50                   	push   eax
  47203e:	53                   	push   ebx
  47203f:	51                   	push   ecx
  472040:	57                   	push   edi
  472041:	e8 33 03 f9 ff       	call   0x402379
  472046:	83 c4 18             	add    esp,0x18
  472049:	8d 4e 14             	lea    ecx,[esi+0x14]
  47204c:	53                   	push   ebx
  47204d:	e8 64 75 00 00       	call   0x4795b6
  472052:	8d 54 24 10          	lea    edx,[esp+0x10]
  472056:	52                   	push   edx
  472057:	57                   	push   edi
  472058:	e8 b1 09 f9 ff       	call   0x402a0e
  47205d:	83 c4 08             	add    esp,0x8
  472060:	33 c9                	xor    ecx,ecx
  472062:	66 83 38 01          	cmp    WORD PTR [eax],0x1
  472066:	8b 7c 24 14          	mov    edi,DWORD PTR [esp+0x14]
  47206a:	0f 94 c1             	sete   cl
  47206d:	83 ff ff             	cmp    edi,0xffffffff
  472070:	89 4e 18             	mov    DWORD PTR [esi+0x18],ecx
  472073:	0f 85 9c 00 00 00    	jne    0x472115
  472079:	8b 95 bc 00 00 00    	mov    edx,DWORD PTR [ebp+0xbc]
  47207f:	8d 8d b4 00 00 00    	lea    ecx,[ebp+0xb4]
  472085:	56                   	push   esi
  472086:	52                   	push   edx
  472087:	e8 3a 77 00 00       	call   0x4797c6
  47208c:	8b ce                	mov    ecx,esi
  47208e:	e8 07 05 f9 ff       	call   0x40259a
  472093:	51                   	push   ecx
  472094:	8b cc                	mov    ecx,esp
  472096:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  47209a:	50                   	push   eax
  47209b:	e8 46 75 00 00       	call   0x4795e6
  4720a0:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  4720a6:	6a 00                	push   0x0
  4720a8:	e8 e3 ff f8 ff       	call   0x402090
  4720ad:	8b ce                	mov    ecx,esi
  4720af:	e8 b7 06 f9 ff       	call   0x40276b
  4720b4:	51                   	push   ecx
  4720b5:	8b cc                	mov    ecx,esp
  4720b7:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  4720bb:	50                   	push   eax
  4720bc:	e8 25 75 00 00       	call   0x4795e6
  4720c1:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  4720c7:	6a 01                	push   0x1
  4720c9:	e8 c2 ff f8 ff       	call   0x402090
  4720ce:	8b ce                	mov    ecx,esi
  4720d0:	e8 87 0b f9 ff       	call   0x402c5c
  4720d5:	51                   	push   ecx
  4720d6:	8b cc                	mov    ecx,esp
  4720d8:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  4720dc:	50                   	push   eax
  4720dd:	e8 04 75 00 00       	call   0x4795e6
  4720e2:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  4720e8:	6a 02                	push   0x2
  4720ea:	e8 a1 ff f8 ff       	call   0x402090
  4720ef:	8b ce                	mov    ecx,esi
  4720f1:	e8 0f f9 f8 ff       	call   0x401a05
  4720f6:	51                   	push   ecx
  4720f7:	8b cc                	mov    ecx,esp
  4720f9:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  4720fd:	50                   	push   eax
  4720fe:	e8 e3 74 00 00       	call   0x4795e6
  472103:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  472109:	6a 03                	push   0x3
  47210b:	e8 80 ff f8 ff       	call   0x402090
  472110:	e9 e8 00 00 00       	jmp    0x4721fd
  472115:	8b 85 b8 00 00 00    	mov    eax,DWORD PTR [ebp+0xb8]
  47211b:	8b 0c b8             	mov    ecx,DWORD PTR [eax+edi*4]
  47211e:	85 c9                	test   ecx,ecx
  472120:	74 06                	je     0x472128
  472122:	8b 11                	mov    edx,DWORD PTR [ecx]
  472124:	6a 01                	push   0x1
  472126:	ff 12                	call   DWORD PTR [edx]
  472128:	8b 85 b8 00 00 00    	mov    eax,DWORD PTR [ebp+0xb8]
  47212e:	8b ce                	mov    ecx,esi
  472130:	89 34 b8             	mov    DWORD PTR [eax+edi*4],esi
  472133:	e8 62 04 f9 ff       	call   0x40259a
  472138:	51                   	push   ecx
  472139:	8b cc                	mov    ecx,esp
  47213b:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  47213f:	50                   	push   eax
  472140:	e8 a1 74 00 00       	call   0x4795e6
  472145:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  47214b:	6a 00                	push   0x0
  47214d:	57                   	push   edi
  47214e:	e8 1f 0e f9 ff       	call   0x402f72
  472153:	8b ce                	mov    ecx,esi
  472155:	e8 11 06 f9 ff       	call   0x40276b
  47215a:	51                   	push   ecx
  47215b:	8b cc                	mov    ecx,esp
  47215d:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  472161:	50                   	push   eax
  472162:	e8 7f 74 00 00       	call   0x4795e6
  472167:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  47216d:	6a 01                	push   0x1
  47216f:	57                   	push   edi
  472170:	e8 fd 0d f9 ff       	call   0x402f72
  472175:	8b ce                	mov    ecx,esi
  472177:	e8 e0 0a f9 ff       	call   0x402c5c
  47217c:	51                   	push   ecx
  47217d:	8b cc                	mov    ecx,esp
  47217f:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  472183:	50                   	push   eax
  472184:	e8 5d 74 00 00       	call   0x4795e6
  472189:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  47218f:	6a 02                	push   0x2
  472191:	57                   	push   edi
  472192:	e8 db 0d f9 ff       	call   0x402f72
  472197:	8b ce                	mov    ecx,esi
  472199:	e8 67 f8 f8 ff       	call   0x401a05
  47219e:	51                   	push   ecx
  47219f:	8b cc                	mov    ecx,esp
  4721a1:	89 64 24 30          	mov    DWORD PTR [esp+0x30],esp
  4721a5:	50                   	push   eax
  4721a6:	e8 3b 74 00 00       	call   0x4795e6
  4721ab:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  4721b1:	6a 03                	push   0x3
  4721b3:	57                   	push   edi
  4721b4:	e8 b9 0d f9 ff       	call   0x402f72
  4721b9:	eb 42                	jmp    0x4721fd
  4721bb:	3c 02                	cmp    al,0x2
  4721bd:	75 3e                	jne    0x4721fd
  4721bf:	0f bf ce             	movsx  ecx,si
  4721c2:	51                   	push   ecx
  4721c3:	8b cd                	mov    ecx,ebp
  4721c5:	e8 f2 ff f8 ff       	call   0x4021bc
  4721ca:	8b f8                	mov    edi,eax
  4721cc:	85 ff                	test   edi,edi
  4721ce:	7c 2d                	jl     0x4721fd
  4721d0:	8b 95 b8 00 00 00    	mov    edx,DWORD PTR [ebp+0xb8]
  4721d6:	8b 0c ba             	mov    ecx,DWORD PTR [edx+edi*4]
  4721d9:	85 c9                	test   ecx,ecx
  4721db:	74 06                	je     0x4721e3
  4721dd:	8b 01                	mov    eax,DWORD PTR [ecx]
  4721df:	6a 01                	push   0x1
  4721e1:	ff 10                	call   DWORD PTR [eax]
  4721e3:	6a 01                	push   0x1
  4721e5:	57                   	push   edi
  4721e6:	8d 8d b4 00 00 00    	lea    ecx,[ebp+0xb4]
  4721ec:	e8 c9 78 00 00       	call   0x479aba
  4721f1:	8b 8d c8 00 00 00    	mov    ecx,DWORD PTR [ebp+0xc8]
  4721f7:	57                   	push   edi
  4721f8:	e8 17 0c f9 ff       	call   0x402e14
  4721fd:	53                   	push   ebx
  4721fe:	e8 7d 73 00 00       	call   0x479580
  472203:	83 c4 04             	add    esp,0x4
  472206:	b8 01 00 00 00       	mov    eax,0x1
  47220b:	8b 4c 24 1c          	mov    ecx,DWORD PTR [esp+0x1c]
  47220f:	5f                   	pop    edi
  472210:	5e                   	pop    esi
  472211:	5d                   	pop    ebp
  472212:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  472219:	5b                   	pop    ebx
  47221a:	83 c4 18             	add    esp,0x18
  47221d:	c2 08 00             	ret    0x8
  472220:	cc                   	int3
  472221:	cc                   	int3
  472222:	cc                   	int3
  472223:	cc                   	int3
  472224:	cc                   	int3
  472225:	cc                   	int3
  472226:	cc                   	int3
  472227:	cc                   	int3
  472228:	cc                   	int3
  472229:	cc                   	int3
  47222a:	cc                   	int3
  47222b:	cc                   	int3
  47222c:	cc                   	int3
  47222d:	cc                   	int3
  47222e:	cc                   	int3
  47222f:	cc                   	int3
  472230:	cc                   	int3
  472231:	cc                   	int3
  472232:	cc                   	int3
  472233:	cc                   	int3
  472234:	cc                   	int3
  472235:	cc                   	int3
  472236:	cc                   	int3
  472237:	cc                   	int3
  472238:	cc                   	int3
  472239:	cc                   	int3
  47223a:	cc                   	int3
  47223b:	cc                   	int3
  47223c:	cc                   	int3
  47223d:	cc                   	int3
  47223e:	cc                   	int3
  47223f:	cc                   	int3
  472240:	cc                   	int3
  472241:	cc                   	int3
  472242:	cc                   	int3
  472243:	cc                   	int3
  472244:	cc                   	int3
  472245:	cc                   	int3
  472246:	cc                   	int3
  472247:	cc                   	int3
  472248:	cc                   	int3
  472249:	cc                   	int3
  47224a:	cc                   	int3
  47224b:	cc                   	int3
  47224c:	cc                   	int3
  47224d:	cc                   	int3
  47224e:	cc                   	int3
  47224f:	cc                   	int3
  472250:	cc                   	int3
  472251:	cc                   	int3
  472252:	cc                   	int3
  472253:	cc                   	int3
  472254:	cc                   	int3
  472255:	cc                   	int3
  472256:	cc                   	int3
  472257:	cc                   	int3
  472258:	cc                   	int3
  472259:	cc                   	int3
  47225a:	cc                   	int3
  47225b:	cc                   	int3
  47225c:	cc                   	int3
  47225d:	cc                   	int3
  47225e:	cc                   	int3
  47225f:	cc                   	int3
  472260:	cc                   	int3
  472261:	cc                   	int3
  472262:	cc                   	int3
  472263:	cc                   	int3
  472264:	cc                   	int3
  472265:	cc                   	int3
  472266:	cc                   	int3
  472267:	cc                   	int3
  472268:	cc                   	int3
  472269:	cc                   	int3
  47226a:	cc                   	int3
  47226b:	cc                   	int3
  47226c:	cc                   	int3
  47226d:	cc                   	int3
  47226e:	cc                   	int3
  47226f:	cc                   	int3
  472270:	cc                   	int3
  472271:	cc                   	int3
  472272:	cc                   	int3
  472273:	cc                   	int3
  472274:	cc                   	int3
  472275:	cc                   	int3
  472276:	cc                   	int3
  472277:	cc                   	int3
  472278:	cc                   	int3
  472279:	cc                   	int3
  47227a:	cc                   	int3
  47227b:	cc                   	int3
  47227c:	cc                   	int3
  47227d:	cc                   	int3
  47227e:	cc                   	int3
  47227f:	cc                   	int3
  472280:	cc                   	int3
  472281:	cc                   	int3
  472282:	cc                   	int3
  472283:	cc                   	int3
  472284:	cc                   	int3
  472285:	cc                   	int3
  472286:	cc                   	int3
  472287:	cc                   	int3
  472288:	cc                   	int3
  472289:	cc                   	int3
  47228a:	cc                   	int3
  47228b:	cc                   	int3
  47228c:	cc                   	int3
  47228d:	cc                   	int3
  47228e:	cc                   	int3
  47228f:	cc                   	int3
  472290:	cc                   	int3
  472291:	cc                   	int3
  472292:	cc                   	int3
  472293:	cc                   	int3
  472294:	cc                   	int3
  472295:	cc                   	int3
  472296:	cc                   	int3
  472297:	cc                   	int3
  472298:	cc                   	int3
  472299:	cc                   	int3
  47229a:	cc                   	int3
  47229b:	cc                   	int3
  47229c:	cc                   	int3
  47229d:	cc                   	int3
  47229e:	cc                   	int3
  47229f:	cc                   	int3
  4722a0:	cc                   	int3
  4722a1:	cc                   	int3
  4722a2:	cc                   	int3
  4722a3:	cc                   	int3
  4722a4:	cc                   	int3
  4722a5:	cc                   	int3
  4722a6:	cc                   	int3
  4722a7:	cc                   	int3
  4722a8:	cc                   	int3
  4722a9:	cc                   	int3
  4722aa:	cc                   	int3
  4722ab:	cc                   	int3
  4722ac:	cc                   	int3
  4722ad:	cc                   	int3
  4722ae:	cc                   	int3
  4722af:	cc                   	int3
  4722b0:	cc                   	int3
  4722b1:	cc                   	int3
  4722b2:	cc                   	int3
  4722b3:	cc                   	int3
  4722b4:	cc                   	int3
  4722b5:	cc                   	int3
  4722b6:	cc                   	int3
  4722b7:	cc                   	int3
  4722b8:	cc                   	int3
  4722b9:	cc                   	int3
  4722ba:	cc                   	int3
  4722bb:	cc                   	int3
  4722bc:	cc                   	int3
  4722bd:	cc                   	int3
  4722be:	cc                   	int3
  4722bf:	cc                   	int3
  4722c0:	cc                   	int3
  4722c1:	cc                   	int3
  4722c2:	cc                   	int3
  4722c3:	cc                   	int3
  4722c4:	cc                   	int3
  4722c5:	cc                   	int3
  4722c6:	cc                   	int3
  4722c7:	cc                   	int3
  4722c8:	cc                   	int3
  4722c9:	cc                   	int3
  4722ca:	cc                   	int3
  4722cb:	cc                   	int3
  4722cc:	cc                   	int3
  4722cd:	cc                   	int3
  4722ce:	cc                   	int3
  4722cf:	cc                   	int3
  4722d0:	cc                   	int3
  4722d1:	cc                   	int3
  4722d2:	cc                   	int3
  4722d3:	cc                   	int3
  4722d4:	cc                   	int3
  4722d5:	cc                   	int3
  4722d6:	cc                   	int3
  4722d7:	cc                   	int3
  4722d8:	cc                   	int3
  4722d9:	cc                   	int3
  4722da:	cc                   	int3
  4722db:	cc                   	int3
  4722dc:	cc                   	int3
  4722dd:	cc                   	int3
  4722de:	cc                   	int3
  4722df:	cc                   	int3
  4722e0:	cc                   	int3
  4722e1:	cc                   	int3
  4722e2:	cc                   	int3
  4722e3:	cc                   	int3
  4722e4:	cc                   	int3
  4722e5:	cc                   	int3
  4722e6:	cc                   	int3
  4722e7:	cc                   	int3
  4722e8:	cc                   	int3
  4722e9:	cc                   	int3
  4722ea:	cc                   	int3
  4722eb:	cc                   	int3
  4722ec:	cc                   	int3
  4722ed:	cc                   	int3
  4722ee:	cc                   	int3
  4722ef:	cc                   	int3
  4722f0:	cc                   	int3
  4722f1:	cc                   	int3
  4722f2:	cc                   	int3
  4722f3:	cc                   	int3
  4722f4:	cc                   	int3
  4722f5:	cc                   	int3
  4722f6:	cc                   	int3
  4722f7:	cc                   	int3
  4722f8:	cc                   	int3
  4722f9:	cc                   	int3
  4722fa:	cc                   	int3
  4722fb:	cc                   	int3
  4722fc:	cc                   	int3
  4722fd:	cc                   	int3
  4722fe:	cc                   	int3
  4722ff:	cc                   	int3
  472300:	8d 41 08             	lea    eax,[ecx+0x8]
  472303:	c3                   	ret
  472304:	90                   	nop
  472305:	90                   	nop
  472306:	90                   	nop
  472307:	90                   	nop
  472308:	90                   	nop
  472309:	90                   	nop
  47230a:	90                   	nop
  47230b:	90                   	nop
  47230c:	90                   	nop
  47230d:	90                   	nop
  47230e:	90                   	nop
  47230f:	90                   	nop
  472310:	cc                   	int3
  472311:	cc                   	int3
  472312:	cc                   	int3
  472313:	cc                   	int3
  472314:	cc                   	int3
  472315:	cc                   	int3
  472316:	cc                   	int3
  472317:	cc                   	int3
  472318:	cc                   	int3
  472319:	cc                   	int3
  47231a:	cc                   	int3
  47231b:	cc                   	int3
  47231c:	cc                   	int3
  47231d:	cc                   	int3
  47231e:	cc                   	int3
  47231f:	cc                   	int3
  472320:	8d 41 0c             	lea    eax,[ecx+0xc]
  472323:	c3                   	ret
  472324:	90                   	nop
  472325:	90                   	nop
  472326:	90                   	nop
  472327:	90                   	nop
  472328:	90                   	nop
  472329:	90                   	nop
  47232a:	90                   	nop
  47232b:	90                   	nop
  47232c:	90                   	nop
  47232d:	90                   	nop
  47232e:	90                   	nop
  47232f:	90                   	nop
  472330:	cc                   	int3
  472331:	cc                   	int3
  472332:	cc                   	int3
  472333:	cc                   	int3
  472334:	cc                   	int3
  472335:	cc                   	int3
  472336:	cc                   	int3
  472337:	cc                   	int3
  472338:	cc                   	int3
  472339:	cc                   	int3
  47233a:	cc                   	int3
  47233b:	cc                   	int3
  47233c:	cc                   	int3
  47233d:	cc                   	int3
  47233e:	cc                   	int3
  47233f:	cc                   	int3
  472340:	8d 41 10             	lea    eax,[ecx+0x10]
  472343:	c3                   	ret
  472344:	90                   	nop
  472345:	90                   	nop
  472346:	90                   	nop
  472347:	90                   	nop
  472348:	90                   	nop
  472349:	90                   	nop
  47234a:	90                   	nop
  47234b:	90                   	nop
  47234c:	90                   	nop
  47234d:	90                   	nop
  47234e:	90                   	nop
  47234f:	90                   	nop
  472350:	cc                   	int3
  472351:	cc                   	int3
  472352:	cc                   	int3
  472353:	cc                   	int3
  472354:	cc                   	int3
  472355:	cc                   	int3
  472356:	cc                   	int3
  472357:	cc                   	int3
  472358:	cc                   	int3
  472359:	cc                   	int3
  47235a:	cc                   	int3
  47235b:	cc                   	int3
  47235c:	cc                   	int3
  47235d:	cc                   	int3
  47235e:	cc                   	int3
  47235f:	cc                   	int3
  472360:	8d 41 14             	lea    eax,[ecx+0x14]
  472363:	c3                   	ret
  472364:	90                   	nop
  472365:	90                   	nop
  472366:	90                   	nop
  472367:	90                   	nop
  472368:	90                   	nop
  472369:	90                   	nop
  47236a:	90                   	nop
  47236b:	90                   	nop
  47236c:	90                   	nop
  47236d:	90                   	nop
  47236e:	90                   	nop
  47236f:	90                   	nop
  472370:	cc                   	int3
  472371:	cc                   	int3
  472372:	cc                   	int3
  472373:	cc                   	int3
  472374:	cc                   	int3
  472375:	cc                   	int3
  472376:	cc                   	int3
  472377:	cc                   	int3
  472378:	cc                   	int3
  472379:	cc                   	int3
  47237a:	cc                   	int3
  47237b:	cc                   	int3
  47237c:	cc                   	int3
  47237d:	cc                   	int3
  47237e:	cc                   	int3
  47237f:	cc                   	int3
  472380:	8b 91 bc 00 00 00    	mov    edx,DWORD PTR [ecx+0xbc]
  472386:	33 c0                	xor    eax,eax
  472388:	56                   	push   esi
  472389:	85 d2                	test   edx,edx
  47238b:	57                   	push   edi
  47238c:	7e 19                	jle    0x4723a7
  47238e:	8b 89 b8 00 00 00    	mov    ecx,DWORD PTR [ecx+0xb8]
  472394:	8b 74 24 0c          	mov    esi,DWORD PTR [esp+0xc]
  472398:	8b 39                	mov    edi,DWORD PTR [ecx]
  47239a:	3b 77 04             	cmp    esi,DWORD PTR [edi+0x4]
  47239d:	74 0b                	je     0x4723aa
  47239f:	40                   	inc    eax
  4723a0:	83 c1 04             	add    ecx,0x4
  4723a3:	3b c2                	cmp    eax,edx
  4723a5:	7c f1                	jl     0x472398
  4723a7:	83 c8 ff             	or     eax,0xffffffff
  4723aa:	5f                   	pop    edi
  4723ab:	5e                   	pop    esi
  4723ac:	c2 04 00             	ret    0x4
  4723af:	90                   	nop
  4723b0:	cc                   	int3
  4723b1:	cc                   	int3
  4723b2:	cc                   	int3
  4723b3:	cc                   	int3
  4723b4:	cc                   	int3
  4723b5:	cc                   	int3
  4723b6:	cc                   	int3
  4723b7:	cc                   	int3
  4723b8:	cc                   	int3
  4723b9:	cc                   	int3
  4723ba:	cc                   	int3
  4723bb:	cc                   	int3
  4723bc:	cc                   	int3
  4723bd:	cc                   	int3
  4723be:	cc                   	int3
  4723bf:	cc                   	int3
  4723c0:	8b 44 24 08          	mov    eax,DWORD PTR [esp+0x8]
  4723c4:	8b 54 24 04          	mov    edx,DWORD PTR [esp+0x4]
  4723c8:	50                   	push   eax
  4723c9:	52                   	push   edx
  4723ca:	e8 c9 f9 f8 ff       	call   0x401d98
  4723cf:	c2 08 00             	ret    0x8
  4723d2:	90                   	nop
  4723d3:	90                   	nop
  4723d4:	90                   	nop
  4723d5:	90                   	nop
  4723d6:	90                   	nop
  4723d7:	90                   	nop
  4723d8:	90                   	nop
  4723d9:	90                   	nop
  4723da:	90                   	nop
  4723db:	90                   	nop
  4723dc:	90                   	nop
  4723dd:	90                   	nop
  4723de:	90                   	nop
  4723df:	90                   	nop
  4723e0:	cc                   	int3
  4723e1:	cc                   	int3
  4723e2:	cc                   	int3
  4723e3:	cc                   	int3
  4723e4:	cc                   	int3
  4723e5:	cc                   	int3
  4723e6:	cc                   	int3
  4723e7:	cc                   	int3
  4723e8:	cc                   	int3
  4723e9:	cc                   	int3
  4723ea:	cc                   	int3
  4723eb:	cc                   	int3
  4723ec:	cc                   	int3
  4723ed:	cc                   	int3
  4723ee:	cc                   	int3
  4723ef:	cc                   	int3
  4723f0:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  4723f4:	56                   	push   esi
  4723f5:	57                   	push   edi
  4723f6:	8b f1                	mov    esi,ecx
  4723f8:	33 ff                	xor    edi,edi
  4723fa:	3b c7                	cmp    eax,edi
  4723fc:	89 46 78             	mov    DWORD PTR [esi+0x78],eax
  4723ff:	75 03                	jne    0x472404
  472401:	89 7e 24             	mov    DWORD PTR [esi+0x24],edi
  472404:	3b c7                	cmp    eax,edi
  472406:	89 7e 18             	mov    DWORD PTR [esi+0x18],edi
  472409:	89 be 98 00 00 00    	mov    DWORD PTR [esi+0x98],edi
  47240f:	89 be 9c 00 00 00    	mov    DWORD PTR [esi+0x9c],edi
  472415:	74 13                	je     0x47242a
  472417:	8b 8e c8 00 00 00    	mov    ecx,DWORD PTR [esi+0xc8]
  47241d:	3b cf                	cmp    ecx,edi
  47241f:	74 09                	je     0x47242a
  472421:	e8 35 ff f8 ff       	call   0x40235b
  472426:	85 c0                	test   eax,eax
  472428:	75 ed                	jne    0x472417
  47242a:	5f                   	pop    edi
  47242b:	5e                   	pop    esi
  47242c:	c2 04 00             	ret    0x4
  47242f:	90                   	nop
  472430:	cc                   	int3
  472431:	cc                   	int3
  472432:	cc                   	int3
  472433:	cc                   	int3
  472434:	cc                   	int3
  472435:	cc                   	int3
  472436:	cc                   	int3
  472437:	cc                   	int3
  472438:	cc                   	int3
  472439:	cc                   	int3
  47243a:	cc                   	int3
  47243b:	cc                   	int3
  47243c:	cc                   	int3
  47243d:	cc                   	int3
  47243e:	cc                   	int3
  47243f:	cc                   	int3
  472440:	6a ff                	push   0xffffffff
  472442:	68 40 0b 48 00       	push   0x480b40
  472447:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  47244d:	50                   	push   eax
  47244e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  472455:	51                   	push   ecx
  472456:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  47245a:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  47245e:	55                   	push   ebp
  47245f:	8b 6c 24 18          	mov    ebp,DWORD PTR [esp+0x18]
  472463:	56                   	push   esi
  472464:	8b f1                	mov    esi,ecx
  472466:	8b 4c 24 28          	mov    ecx,DWORD PTR [esp+0x28]
  47246a:	57                   	push   edi
  47246b:	50                   	push   eax
  47246c:	8b 44 24 28          	mov    eax,DWORD PTR [esp+0x28]
  472470:	51                   	push   ecx
  472471:	52                   	push   edx
  472472:	50                   	push   eax
  472473:	55                   	push   ebp
  472474:	8b ce                	mov    ecx,esi
  472476:	89 74 24 20          	mov    DWORD PTR [esp+0x20],esi
  47247a:	e8 61 0b f9 ff       	call   0x402fe0
  47247f:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  472485:	c7 44 24 18 00 00 00 	mov    DWORD PTR [esp+0x18],0x0
  47248c:	00 
  47248d:	e8 06 71 00 00       	call   0x479598
  472492:	68 8c 00 00 00       	push   0x8c
  472497:	c6 44 24 1c 01       	mov    BYTE PTR [esp+0x1c],0x1
  47249c:	c7 06 2c 65 48 00    	mov    DWORD PTR [esi],0x48652c
  4724a2:	c7 86 b8 00 00 00 00 	mov    DWORD PTR [esi+0xb8],0x0
  4724a9:	00 00 00 
  4724ac:	e8 db 70 00 00       	call   0x47958c
  4724b1:	8b f8                	mov    edi,eax
  4724b3:	83 c4 04             	add    esp,0x4
  4724b6:	89 7c 24 30          	mov    DWORD PTR [esp+0x30],edi
  4724ba:	85 ff                	test   edi,edi
  4724bc:	c6 44 24 18 02       	mov    BYTE PTR [esp+0x18],0x2
  4724c1:	74 12                	je     0x4724d5
  4724c3:	8b cf                	mov    ecx,edi
  4724c5:	ff 15 80 8e 49 00    	call   DWORD PTR ds:0x498e80
  4724cb:	c7 07 08 31 48 00    	mov    DWORD PTR [edi],0x483108
  4724d1:	8b cf                	mov    ecx,edi
  4724d3:	eb 02                	jmp    0x4724d7
  4724d5:	33 c9                	xor    ecx,ecx
  4724d7:	6a 00                	push   0x0
  4724d9:	6a 40                	push   0x40
  4724db:	6a 40                	push   0x40
  4724dd:	55                   	push   ebp
  4724de:	c6 44 24 28 01       	mov    BYTE PTR [esp+0x28],0x1
  4724e3:	89 8e bc 00 00 00    	mov    DWORD PTR [esi+0xbc],ecx
  4724e9:	ff 15 78 8e 49 00    	call   DWORD PTR ds:0x498e78
  4724ef:	8b 8e bc 00 00 00    	mov    ecx,DWORD PTR [esi+0xbc]
  4724f5:	6a 00                	push   0x0
  4724f7:	6a 00                	push   0x0
  4724f9:	6a 7d                	push   0x7d
  4724fb:	51                   	push   ecx
  4724fc:	ff 15 9c 99 49 00    	call   DWORD PTR ds:0x49999c
  472502:	8b 4c 24 20          	mov    ecx,DWORD PTR [esp+0x20]
  472506:	83 c4 10             	add    esp,0x10
  472509:	8b c6                	mov    eax,esi
  47250b:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  472512:	5f                   	pop    edi
  472513:	5e                   	pop    esi
  472514:	5d                   	pop    ebp
  472515:	83 c4 10             	add    esp,0x10
  472518:	c2 14 00             	ret    0x14
  47251b:	90                   	nop
  47251c:	90                   	nop
  47251d:	90                   	nop
  47251e:	90                   	nop
  47251f:	90                   	nop
  472520:	cc                   	int3
  472521:	cc                   	int3
  472522:	cc                   	int3
  472523:	cc                   	int3
  472524:	cc                   	int3
  472525:	cc                   	int3
  472526:	cc                   	int3
  472527:	cc                   	int3
  472528:	cc                   	int3
  472529:	cc                   	int3
  47252a:	cc                   	int3
  47252b:	cc                   	int3
  47252c:	cc                   	int3
  47252d:	cc                   	int3
  47252e:	cc                   	int3
  47252f:	cc                   	int3
  472530:	cc                   	int3
  472531:	cc                   	int3
  472532:	cc                   	int3
  472533:	cc                   	int3
  472534:	cc                   	int3
  472535:	cc                   	int3
  472536:	cc                   	int3
  472537:	cc                   	int3
  472538:	cc                   	int3
  472539:	cc                   	int3
  47253a:	cc                   	int3
  47253b:	cc                   	int3
  47253c:	cc                   	int3
  47253d:	cc                   	int3
  47253e:	cc                   	int3
  47253f:	cc                   	int3
  472540:	cc                   	int3
  472541:	cc                   	int3
  472542:	cc                   	int3
  472543:	cc                   	int3
  472544:	cc                   	int3
  472545:	cc                   	int3
  472546:	cc                   	int3
  472547:	cc                   	int3
  472548:	cc                   	int3
  472549:	cc                   	int3
  47254a:	cc                   	int3
  47254b:	cc                   	int3
  47254c:	cc                   	int3
  47254d:	cc                   	int3
  47254e:	cc                   	int3
  47254f:	cc                   	int3
  472550:	cc                   	int3
  472551:	cc                   	int3
  472552:	cc                   	int3
  472553:	cc                   	int3
  472554:	cc                   	int3
  472555:	cc                   	int3
  472556:	cc                   	int3
  472557:	cc                   	int3
  472558:	cc                   	int3
  472559:	cc                   	int3
  47255a:	cc                   	int3
  47255b:	cc                   	int3
  47255c:	cc                   	int3
  47255d:	cc                   	int3
  47255e:	cc                   	int3
  47255f:	cc                   	int3
  472560:	b8 39 00 00 00       	mov    eax,0x39
  472565:	c3                   	ret
  472566:	90                   	nop
  472567:	90                   	nop
  472568:	90                   	nop
  472569:	90                   	nop
  47256a:	90                   	nop
  47256b:	90                   	nop
  47256c:	90                   	nop
  47256d:	90                   	nop
  47256e:	90                   	nop
  47256f:	90                   	nop
  472570:	cc                   	int3
  472571:	cc                   	int3
  472572:	cc                   	int3
  472573:	cc                   	int3
  472574:	cc                   	int3
  472575:	cc                   	int3
  472576:	cc                   	int3
  472577:	cc                   	int3
  472578:	cc                   	int3
  472579:	cc                   	int3
  47257a:	cc                   	int3
  47257b:	cc                   	int3
  47257c:	cc                   	int3
  47257d:	cc                   	int3
  47257e:	cc                   	int3
  47257f:	cc                   	int3
  472580:	56                   	push   esi
  472581:	8b f1                	mov    esi,ecx
  472583:	e8 75 01 f9 ff       	call   0x4026fd
  472588:	f6 44 24 08 01       	test   BYTE PTR [esp+0x8],0x1
  47258d:	74 09                	je     0x472598
  47258f:	56                   	push   esi
  472590:	e8 eb 6f 00 00       	call   0x479580
  472595:	83 c4 04             	add    esp,0x4
  472598:	8b c6                	mov    eax,esi
  47259a:	5e                   	pop    esi
  47259b:	c2 04 00             	ret    0x4
  47259e:	90                   	nop
  47259f:	90                   	nop
  4725a0:	cc                   	int3
  4725a1:	cc                   	int3
  4725a2:	cc                   	int3
  4725a3:	cc                   	int3
  4725a4:	cc                   	int3
  4725a5:	cc                   	int3
  4725a6:	cc                   	int3
  4725a7:	cc                   	int3
  4725a8:	cc                   	int3
  4725a9:	cc                   	int3
  4725aa:	cc                   	int3
  4725ab:	cc                   	int3
  4725ac:	cc                   	int3
  4725ad:	cc                   	int3
  4725ae:	cc                   	int3
  4725af:	cc                   	int3
  4725b0:	6a ff                	push   0xffffffff
  4725b2:	68 76 0b 48 00       	push   0x480b76
  4725b7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4725bd:	50                   	push   eax
  4725be:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  4725c5:	51                   	push   ecx
  4725c6:	56                   	push   esi
  4725c7:	8b f1                	mov    esi,ecx
  4725c9:	89 74 24 04          	mov    DWORD PTR [esp+0x4],esi
  4725cd:	c7 06 2c 65 48 00    	mov    DWORD PTR [esi],0x48652c
  4725d3:	8b 8e bc 00 00 00    	mov    ecx,DWORD PTR [esi+0xbc]
  4725d9:	c7 44 24 10 01 00 00 	mov    DWORD PTR [esp+0x10],0x1
  4725e0:	00 
  4725e1:	85 c9                	test   ecx,ecx
  4725e3:	74 07                	je     0x4725ec
  4725e5:	8b 01                	mov    eax,DWORD PTR [ecx]
  4725e7:	6a 01                	push   0x1
  4725e9:	ff 50 04             	call   DWORD PTR [eax+0x4]
  4725ec:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  4725f2:	c6 44 24 10 00       	mov    BYTE PTR [esp+0x10],0x0
  4725f7:	e8 8a 6f 00 00       	call   0x479586
  4725fc:	8b ce                	mov    ecx,esi
  4725fe:	c7 44 24 10 ff ff ff 	mov    DWORD PTR [esp+0x10],0xffffffff
  472605:	ff 
  472606:	e8 9f f9 f8 ff       	call   0x401faa
  47260b:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  47260f:	5e                   	pop    esi
  472610:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  472617:	83 c4 10             	add    esp,0x10
  47261a:	c3                   	ret
  47261b:	90                   	nop
  47261c:	90                   	nop
  47261d:	90                   	nop
  47261e:	90                   	nop
  47261f:	90                   	nop
  472620:	cc                   	int3
  472621:	cc                   	int3
  472622:	cc                   	int3
  472623:	cc                   	int3
  472624:	cc                   	int3
  472625:	cc                   	int3
  472626:	cc                   	int3
  472627:	cc                   	int3
  472628:	cc                   	int3
  472629:	cc                   	int3
  47262a:	cc                   	int3
  47262b:	cc                   	int3
  47262c:	cc                   	int3
  47262d:	cc                   	int3
  47262e:	cc                   	int3
  47262f:	cc                   	int3
  472630:	cc                   	int3
  472631:	cc                   	int3
  472632:	cc                   	int3
  472633:	cc                   	int3
  472634:	cc                   	int3
  472635:	cc                   	int3
  472636:	cc                   	int3
  472637:	cc                   	int3
  472638:	cc                   	int3
  472639:	cc                   	int3
  47263a:	cc                   	int3
  47263b:	cc                   	int3
  47263c:	cc                   	int3
  47263d:	cc                   	int3
  47263e:	cc                   	int3
  47263f:	cc                   	int3
  472640:	56                   	push   esi
  472641:	8b f1                	mov    esi,ecx
  472643:	e8 7c 02 f9 ff       	call   0x4028c4
  472648:	c7 46 44 14 00 00 00 	mov    DWORD PTR [esi+0x44],0x14
  47264f:	5e                   	pop    esi
  472650:	c3                   	ret
  472651:	90                   	nop
  472652:	90                   	nop
  472653:	90                   	nop
  472654:	90                   	nop
  472655:	90                   	nop
  472656:	90                   	nop
  472657:	90                   	nop
  472658:	90                   	nop
  472659:	90                   	nop
  47265a:	90                   	nop
  47265b:	90                   	nop
  47265c:	90                   	nop
  47265d:	90                   	nop
  47265e:	90                   	nop
  47265f:	90                   	nop
  472660:	cc                   	int3
  472661:	cc                   	int3
  472662:	cc                   	int3
  472663:	cc                   	int3
  472664:	cc                   	int3
  472665:	cc                   	int3
  472666:	cc                   	int3
  472667:	cc                   	int3
  472668:	cc                   	int3
  472669:	cc                   	int3
  47266a:	cc                   	int3
  47266b:	cc                   	int3
  47266c:	cc                   	int3
  47266d:	cc                   	int3
  47266e:	cc                   	int3
  47266f:	cc                   	int3
  472670:	81 ec 8c 00 00 00    	sub    esp,0x8c
  472676:	53                   	push   ebx
  472677:	55                   	push   ebp
  472678:	8b 9c 24 98 00 00 00 	mov    ebx,DWORD PTR [esp+0x98]
  47267f:	56                   	push   esi
  472680:	8b f1                	mov    esi,ecx
  472682:	57                   	push   edi
  472683:	6a 00                	push   0x0
  472685:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  472688:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  47268b:	8b 96 bc 00 00 00    	mov    edx,DWORD PTR [esi+0xbc]
  472691:	83 c1 0a             	add    ecx,0xa
  472694:	83 c0 13             	add    eax,0x13
  472697:	52                   	push   edx
  472698:	51                   	push   ecx
  472699:	50                   	push   eax
  47269a:	8b cb                	mov    ecx,ebx
  47269c:	ff 15 30 8f 49 00    	call   DWORD PTR ds:0x498f30
  4726a2:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  4726a5:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  4726a8:	83 c1 23             	add    ecx,0x23
  4726ab:	8d a8 20 01 00 00    	lea    ebp,[eax+0x120]
  4726b1:	8d 44 24 20          	lea    eax,[esp+0x20]
  4726b5:	89 4c 24 14          	mov    DWORD PTR [esp+0x14],ecx
  4726b9:	50                   	push   eax
  4726ba:	8b cb                	mov    ecx,ebx
  4726bc:	ff 15 38 8f 49 00    	call   DWORD PTR ds:0x498f38
  4726c2:	8b cb                	mov    ecx,ebx
  4726c4:	ff 15 04 8f 49 00    	call   DWORD PTR ds:0x498f04
  4726ca:	89 44 24 44          	mov    DWORD PTR [esp+0x44],eax
  4726ce:	8b 86 b8 00 00 00    	mov    eax,DWORD PTR [esi+0xb8]
  4726d4:	85 c0                	test   eax,eax
  4726d6:	75 6f                	jne    0x472747
  4726d8:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  4726de:	8b 96 b4 00 00 00    	mov    edx,DWORD PTR [esi+0xb4]
  4726e4:	6a 06                	push   0x6
  4726e6:	83 38 00             	cmp    DWORD PTR [eax],0x0
  4726e9:	74 2e                	je     0x472719
  4726eb:	83 ec 7c             	sub    esp,0x7c
  4726ee:	b9 1f 00 00 00       	mov    ecx,0x1f
  4726f3:	8d b4 24 a0 00 00 00 	lea    esi,[esp+0xa0]
  4726fa:	8b fc                	mov    edi,esp
  4726fc:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  4726fe:	8b 8c 24 94 00 00 00 	mov    ecx,DWORD PTR [esp+0x94]
  472705:	68 ff 00 00 00       	push   0xff
  47270a:	52                   	push   edx
  47270b:	51                   	push   ecx
  47270c:	55                   	push   ebp
  47270d:	8b c8                	mov    ecx,eax
  47270f:	e8 10 10 f9 ff       	call   0x403724
  472714:	e9 17 01 00 00       	jmp    0x472830
  472719:	83 ec 7c             	sub    esp,0x7c
  47271c:	b9 1f 00 00 00       	mov    ecx,0x1f
  472721:	8b fc                	mov    edi,esp
  472723:	8d b4 24 a0 00 00 00 	lea    esi,[esp+0xa0]
  47272a:	68 ff ff 00 00       	push   0xffff
  47272f:	52                   	push   edx
  472730:	8b 94 24 9c 00 00 00 	mov    edx,DWORD PTR [esp+0x9c]
  472737:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  472739:	52                   	push   edx
  47273a:	55                   	push   ebp
  47273b:	8b c8                	mov    ecx,eax
  47273d:	e8 48 09 f9 ff       	call   0x40308a
  472742:	e9 e9 00 00 00       	jmp    0x472830
  472747:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  47274a:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  47274d:	51                   	push   ecx
  47274e:	50                   	push   eax
  47274f:	8d 44 24 18          	lea    eax,[esp+0x18]
  472753:	c7 44 24 18 5a 00 00 	mov    DWORD PTR [esp+0x18],0x5a
  47275a:	00 
  47275b:	50                   	push   eax
  47275c:	c7 44 24 24 21 02 00 	mov    DWORD PTR [esp+0x24],0x221
  472763:	00 
  472764:	c7 44 24 20 0a 00 00 	mov    DWORD PTR [esp+0x20],0xa
  47276b:	00 
  47276c:	c7 44 24 28 4a 00 00 	mov    DWORD PTR [esp+0x28],0x4a
  472773:	00 
  472774:	ff 15 fc 98 49 00    	call   DWORD PTR ds:0x4998fc
  47277a:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  472780:	8b 96 b4 00 00 00    	mov    edx,DWORD PTR [esi+0xb4]
  472786:	6a 01                	push   0x1
  472788:	6a 02                	push   0x2
  47278a:	8b 08                	mov    ecx,DWORD PTR [eax]
  47278c:	6a ff                	push   0xffffffff
  47278e:	85 c9                	test   ecx,ecx
  472790:	6a 00                	push   0x0
  472792:	6a 00                	push   0x0
  472794:	74 4e                	je     0x4727e4
  472796:	83 ec 7c             	sub    esp,0x7c
  472799:	b9 1f 00 00 00       	mov    ecx,0x1f
  47279e:	8b fc                	mov    edi,esp
  4727a0:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  4727a7:	68 ff 00 00 00       	push   0xff
  4727ac:	52                   	push   edx
  4727ad:	8b 94 24 a8 00 00 00 	mov    edx,DWORD PTR [esp+0xa8]
  4727b4:	83 ec 10             	sub    esp,0x10
  4727b7:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  4727b9:	8b cc                	mov    ecx,esp
  4727bb:	89 11                	mov    DWORD PTR [ecx],edx
  4727bd:	8b 94 24 bc 00 00 00 	mov    edx,DWORD PTR [esp+0xbc]
  4727c4:	89 51 04             	mov    DWORD PTR [ecx+0x4],edx
  4727c7:	8b 94 24 c0 00 00 00 	mov    edx,DWORD PTR [esp+0xc0]
  4727ce:	89 51 08             	mov    DWORD PTR [ecx+0x8],edx
  4727d1:	8b 94 24 c4 00 00 00 	mov    edx,DWORD PTR [esp+0xc4]
  4727d8:	89 51 0c             	mov    DWORD PTR [ecx+0xc],edx
  4727db:	8b c8                	mov    ecx,eax
  4727dd:	e8 0c 0e f9 ff       	call   0x4035ee
  4727e2:	eb 4c                	jmp    0x472830
  4727e4:	83 ec 7c             	sub    esp,0x7c
  4727e7:	b9 1f 00 00 00       	mov    ecx,0x1f
  4727ec:	8b fc                	mov    edi,esp
  4727ee:	8d b4 24 b0 00 00 00 	lea    esi,[esp+0xb0]
  4727f5:	68 ff ff 00 00       	push   0xffff
  4727fa:	52                   	push   edx
  4727fb:	8b 94 24 a8 00 00 00 	mov    edx,DWORD PTR [esp+0xa8]
  472802:	83 ec 10             	sub    esp,0x10
  472805:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  472807:	8b cc                	mov    ecx,esp
  472809:	89 11                	mov    DWORD PTR [ecx],edx
  47280b:	8b 94 24 bc 00 00 00 	mov    edx,DWORD PTR [esp+0xbc]
  472812:	89 51 04             	mov    DWORD PTR [ecx+0x4],edx
  472815:	8b 94 24 c0 00 00 00 	mov    edx,DWORD PTR [esp+0xc0]
  47281c:	89 51 08             	mov    DWORD PTR [ecx+0x8],edx
  47281f:	8b 94 24 c4 00 00 00 	mov    edx,DWORD PTR [esp+0xc4]
  472826:	89 51 0c             	mov    DWORD PTR [ecx+0xc],edx
  472829:	8b c8                	mov    ecx,eax
  47282b:	e8 18 f0 f8 ff       	call   0x401848
  472830:	8b cb                	mov    ecx,ebx
  472832:	ff 15 00 8f 49 00    	call   DWORD PTR ds:0x498f00
  472838:	5f                   	pop    edi
  472839:	5e                   	pop    esi
  47283a:	5d                   	pop    ebp
  47283b:	b8 01 00 00 00       	mov    eax,0x1
  472840:	5b                   	pop    ebx
  472841:	81 c4 8c 00 00 00    	add    esp,0x8c
  472847:	c2 04 00             	ret    0x4
  47284a:	90                   	nop
  47284b:	90                   	nop
  47284c:	90                   	nop
  47284d:	90                   	nop
  47284e:	90                   	nop
  47284f:	90                   	nop
  472850:	cc                   	int3
  472851:	cc                   	int3
  472852:	cc                   	int3
  472853:	cc                   	int3
  472854:	cc                   	int3
  472855:	cc                   	int3
  472856:	cc                   	int3
  472857:	cc                   	int3
  472858:	cc                   	int3
  472859:	cc                   	int3
  47285a:	cc                   	int3
  47285b:	cc                   	int3
  47285c:	cc                   	int3
  47285d:	cc                   	int3
  47285e:	cc                   	int3
  47285f:	cc                   	int3
  472860:	cc                   	int3
  472861:	cc                   	int3
  472862:	cc                   	int3
  472863:	cc                   	int3
  472864:	cc                   	int3
  472865:	cc                   	int3
  472866:	cc                   	int3
  472867:	cc                   	int3
  472868:	cc                   	int3
  472869:	cc                   	int3
  47286a:	cc                   	int3
  47286b:	cc                   	int3
  47286c:	cc                   	int3
  47286d:	cc                   	int3
  47286e:	cc                   	int3
  47286f:	cc                   	int3
  472870:	cc                   	int3
  472871:	cc                   	int3
  472872:	cc                   	int3
  472873:	cc                   	int3
  472874:	cc                   	int3
  472875:	cc                   	int3
  472876:	cc                   	int3
  472877:	cc                   	int3
  472878:	cc                   	int3
  472879:	cc                   	int3
  47287a:	cc                   	int3
  47287b:	cc                   	int3
  47287c:	cc                   	int3
  47287d:	cc                   	int3
  47287e:	cc                   	int3
  47287f:	cc                   	int3
  472880:	cc                   	int3
  472881:	cc                   	int3
  472882:	cc                   	int3
  472883:	cc                   	int3
  472884:	cc                   	int3
  472885:	cc                   	int3
  472886:	cc                   	int3
  472887:	cc                   	int3
  472888:	cc                   	int3
  472889:	cc                   	int3
  47288a:	cc                   	int3
  47288b:	cc                   	int3
  47288c:	cc                   	int3
  47288d:	cc                   	int3
  47288e:	cc                   	int3
  47288f:	cc                   	int3
  472890:	cc                   	int3
  472891:	cc                   	int3
  472892:	cc                   	int3
  472893:	cc                   	int3
  472894:	cc                   	int3
  472895:	cc                   	int3
  472896:	cc                   	int3
  472897:	cc                   	int3
  472898:	cc                   	int3
  472899:	cc                   	int3
  47289a:	cc                   	int3
  47289b:	cc                   	int3
  47289c:	cc                   	int3
  47289d:	cc                   	int3
  47289e:	cc                   	int3
  47289f:	cc                   	int3
  4728a0:	cc                   	int3
  4728a1:	cc                   	int3
  4728a2:	cc                   	int3
  4728a3:	cc                   	int3
  4728a4:	cc                   	int3
  4728a5:	cc                   	int3
  4728a6:	cc                   	int3
  4728a7:	cc                   	int3
  4728a8:	cc                   	int3
  4728a9:	cc                   	int3
  4728aa:	cc                   	int3
  4728ab:	cc                   	int3
  4728ac:	cc                   	int3
  4728ad:	cc                   	int3
  4728ae:	cc                   	int3
  4728af:	cc                   	int3
  4728b0:	cc                   	int3
  4728b1:	cc                   	int3
  4728b2:	cc                   	int3
  4728b3:	cc                   	int3
  4728b4:	cc                   	int3
  4728b5:	cc                   	int3
  4728b6:	cc                   	int3
  4728b7:	cc                   	int3
  4728b8:	cc                   	int3
  4728b9:	cc                   	int3
  4728ba:	cc                   	int3
  4728bb:	cc                   	int3
  4728bc:	cc                   	int3
  4728bd:	cc                   	int3
  4728be:	cc                   	int3
  4728bf:	cc                   	int3
  4728c0:	cc                   	int3
  4728c1:	cc                   	int3
  4728c2:	cc                   	int3
  4728c3:	cc                   	int3
  4728c4:	cc                   	int3
  4728c5:	cc                   	int3
  4728c6:	cc                   	int3
  4728c7:	cc                   	int3
  4728c8:	cc                   	int3
  4728c9:	cc                   	int3
  4728ca:	cc                   	int3
  4728cb:	cc                   	int3
  4728cc:	cc                   	int3
  4728cd:	cc                   	int3
  4728ce:	cc                   	int3
  4728cf:	cc                   	int3
  4728d0:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  4728d4:	56                   	push   esi
  4728d5:	8b f1                	mov    esi,ecx
  4728d7:	50                   	push   eax
  4728d8:	8d 8e b4 00 00 00    	lea    ecx,[esi+0xb4]
  4728de:	e8 f7 6c 00 00       	call   0x4795da
  4728e3:	8b 4c 24 0c          	mov    ecx,DWORD PTR [esp+0xc]
  4728e7:	89 8e b8 00 00 00    	mov    DWORD PTR [esi+0xb8],ecx
  4728ed:	5e                   	pop    esi
  4728ee:	c2 08 00             	ret    0x8
  4728f1:	90                   	nop
  4728f2:	90                   	nop
  4728f3:	90                   	nop
  4728f4:	90                   	nop
  4728f5:	90                   	nop
  4728f6:	90                   	nop
  4728f7:	90                   	nop
  4728f8:	90                   	nop
  4728f9:	90                   	nop
  4728fa:	90                   	nop
  4728fb:	90                   	nop
  4728fc:	90                   	nop
  4728fd:	90                   	nop
  4728fe:	90                   	nop
  4728ff:	90                   	nop
  472900:	cc                   	int3
  472901:	cc                   	int3
  472902:	cc                   	int3
  472903:	cc                   	int3
  472904:	cc                   	int3
  472905:	cc                   	int3
  472906:	cc                   	int3
  472907:	cc                   	int3
  472908:	cc                   	int3
  472909:	cc                   	int3
  47290a:	cc                   	int3
  47290b:	cc                   	int3
  47290c:	cc                   	int3
  47290d:	cc                   	int3
  47290e:	cc                   	int3
  47290f:	cc                   	int3
  472910:	e8 1b 00 00 00       	call   0x472930
  472915:	e9 36 00 00 00       	jmp    0x472950
  47291a:	90                   	nop
  47291b:	90                   	nop
  47291c:	90                   	nop
  47291d:	90                   	nop
  47291e:	90                   	nop
  47291f:	90                   	nop
  472920:	cc                   	int3
  472921:	cc                   	int3
  472922:	cc                   	int3
  472923:	cc                   	int3
  472924:	cc                   	int3
  472925:	cc                   	int3
  472926:	cc                   	int3
  472927:	cc                   	int3
  472928:	cc                   	int3
  472929:	cc                   	int3
  47292a:	cc                   	int3
  47292b:	cc                   	int3
  47292c:	cc                   	int3
  47292d:	cc                   	int3
  47292e:	cc                   	int3
  47292f:	cc                   	int3
  472930:	b9 59 7c 49 00       	mov    ecx,0x497c59
  472935:	ff 25 30 97 49 00    	jmp    DWORD PTR ds:0x499730
  47293b:	90                   	nop
  47293c:	90                   	nop
  47293d:	90                   	nop
  47293e:	90                   	nop
  47293f:	90                   	nop
  472940:	cc                   	int3
  472941:	cc                   	int3
  472942:	cc                   	int3
  472943:	cc                   	int3
  472944:	cc                   	int3
  472945:	cc                   	int3
  472946:	cc                   	int3
  472947:	cc                   	int3
  472948:	cc                   	int3
  472949:	cc                   	int3
  47294a:	cc                   	int3
  47294b:	cc                   	int3
  47294c:	cc                   	int3
  47294d:	cc                   	int3
  47294e:	cc                   	int3
  47294f:	cc                   	int3
  472950:	68 70 29 47 00       	push   0x472970
  472955:	e8 64 72 00 00       	call   0x479bbe
  47295a:	59                   	pop    ecx
  47295b:	c3                   	ret
  47295c:	90                   	nop
  47295d:	90                   	nop
  47295e:	90                   	nop
  47295f:	90                   	nop
  472960:	cc                   	int3
  472961:	cc                   	int3
  472962:	cc                   	int3
  472963:	cc                   	int3
  472964:	cc                   	int3
  472965:	cc                   	int3
  472966:	cc                   	int3
  472967:	cc                   	int3
  472968:	cc                   	int3
  472969:	cc                   	int3
  47296a:	cc                   	int3
  47296b:	cc                   	int3
  47296c:	cc                   	int3
  47296d:	cc                   	int3
  47296e:	cc                   	int3
  47296f:	cc                   	int3
  472970:	b9 59 7c 49 00       	mov    ecx,0x497c59
  472975:	ff 25 28 97 49 00    	jmp    DWORD PTR ds:0x499728
  47297b:	90                   	nop
  47297c:	90                   	nop
  47297d:	90                   	nop
  47297e:	90                   	nop
  47297f:	90                   	nop
  472980:	cc                   	int3
  472981:	cc                   	int3
  472982:	cc                   	int3
  472983:	cc                   	int3
  472984:	cc                   	int3
  472985:	cc                   	int3
  472986:	cc                   	int3
  472987:	cc                   	int3
  472988:	cc                   	int3
  472989:	cc                   	int3
  47298a:	cc                   	int3
  47298b:	cc                   	int3
  47298c:	cc                   	int3
  47298d:	cc                   	int3
  47298e:	cc                   	int3
  47298f:	cc                   	int3
  472990:	e8 1b 00 00 00       	call   0x4729b0
  472995:	e9 36 00 00 00       	jmp    0x4729d0
  47299a:	90                   	nop
  47299b:	90                   	nop
  47299c:	90                   	nop
  47299d:	90                   	nop
  47299e:	90                   	nop
  47299f:	90                   	nop
  4729a0:	cc                   	int3
  4729a1:	cc                   	int3
  4729a2:	cc                   	int3
  4729a3:	cc                   	int3
  4729a4:	cc                   	int3
  4729a5:	cc                   	int3
  4729a6:	cc                   	int3
  4729a7:	cc                   	int3
  4729a8:	cc                   	int3
  4729a9:	cc                   	int3
  4729aa:	cc                   	int3
  4729ab:	cc                   	int3
  4729ac:	cc                   	int3
  4729ad:	cc                   	int3
  4729ae:	cc                   	int3
  4729af:	cc                   	int3
  4729b0:	b9 58 7c 49 00       	mov    ecx,0x497c58
  4729b5:	ff 25 34 97 49 00    	jmp    DWORD PTR ds:0x499734
  4729bb:	90                   	nop
  4729bc:	90                   	nop
  4729bd:	90                   	nop
  4729be:	90                   	nop
  4729bf:	90                   	nop
  4729c0:	cc                   	int3
  4729c1:	cc                   	int3
  4729c2:	cc                   	int3
  4729c3:	cc                   	int3
  4729c4:	cc                   	int3
  4729c5:	cc                   	int3
  4729c6:	cc                   	int3
  4729c7:	cc                   	int3
  4729c8:	cc                   	int3
  4729c9:	cc                   	int3
  4729ca:	cc                   	int3
  4729cb:	cc                   	int3
  4729cc:	cc                   	int3
  4729cd:	cc                   	int3
  4729ce:	cc                   	int3
  4729cf:	cc                   	int3
  4729d0:	68 f0 29 47 00       	push   0x4729f0
  4729d5:	e8 e4 71 00 00       	call   0x479bbe
  4729da:	59                   	pop    ecx
  4729db:	c3                   	ret
  4729dc:	90                   	nop
  4729dd:	90                   	nop
  4729de:	90                   	nop
  4729df:	90                   	nop
  4729e0:	cc                   	int3
  4729e1:	cc                   	int3
  4729e2:	cc                   	int3
  4729e3:	cc                   	int3
  4729e4:	cc                   	int3
  4729e5:	cc                   	int3
  4729e6:	cc                   	int3
  4729e7:	cc                   	int3
  4729e8:	cc                   	int3
  4729e9:	cc                   	int3
  4729ea:	cc                   	int3
  4729eb:	cc                   	int3
  4729ec:	cc                   	int3
  4729ed:	cc                   	int3
  4729ee:	cc                   	int3
  4729ef:	cc                   	int3
  4729f0:	b9 58 7c 49 00       	mov    ecx,0x497c58
  4729f5:	ff 25 2c 97 49 00    	jmp    DWORD PTR ds:0x49972c
  4729fb:	90                   	nop
  4729fc:	90                   	nop
  4729fd:	90                   	nop
  4729fe:	90                   	nop
  4729ff:	90                   	nop
  472a00:	cc                   	int3
  472a01:	cc                   	int3
  472a02:	cc                   	int3
  472a03:	cc                   	int3
  472a04:	cc                   	int3
  472a05:	cc                   	int3
  472a06:	cc                   	int3
  472a07:	cc                   	int3
  472a08:	cc                   	int3
  472a09:	cc                   	int3
  472a0a:	cc                   	int3
  472a0b:	cc                   	int3
  472a0c:	cc                   	int3
  472a0d:	cc                   	int3
  472a0e:	cc                   	int3
  472a0f:	cc                   	int3
  472a10:	6a ff                	push   0xffffffff
  472a12:	68 b1 0b 48 00       	push   0x480bb1
  472a17:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  472a1d:	50                   	push   eax
  472a1e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  472a25:	51                   	push   ecx
  472a26:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  472a2a:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  472a2e:	53                   	push   ebx
  472a2f:	55                   	push   ebp
  472a30:	56                   	push   esi
  472a31:	8b f1                	mov    esi,ecx
  472a33:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
  472a37:	57                   	push   edi
  472a38:	50                   	push   eax
  472a39:	8b 44 24 2c          	mov    eax,DWORD PTR [esp+0x2c]
  472a3d:	51                   	push   ecx
  472a3e:	8b 4c 24 2c          	mov    ecx,DWORD PTR [esp+0x2c]
  472a42:	52                   	push   edx
  472a43:	50                   	push   eax
  472a44:	51                   	push   ecx
  472a45:	8b ce                	mov    ecx,esi
  472a47:	89 74 24 24          	mov    DWORD PTR [esp+0x24],esi
  472a4b:	e8 90 05 f9 ff       	call   0x402fe0
  472a50:	8d be cc 00 00 00    	lea    edi,[esi+0xcc]
  472a56:	33 db                	xor    ebx,ebx
  472a58:	8b cf                	mov    ecx,edi
  472a5a:	89 5c 24 1c          	mov    DWORD PTR [esp+0x1c],ebx
  472a5e:	c7 86 b8 00 00 00 14 	mov    DWORD PTR [esi+0xb8],0x14
  472a65:	00 00 00 
  472a68:	c7 86 c0 00 00 00 87 	mov    DWORD PTR [esi+0xc0],0x87
  472a6f:	00 00 00 
  472a72:	c7 86 bc 00 00 00 2d 	mov    DWORD PTR [esi+0xbc],0x2d
  472a79:	00 00 00 
  472a7c:	c7 86 c4 00 00 00 e1 	mov    DWORD PTR [esi+0xc4],0xe1
  472a83:	00 00 00 
  472a86:	e8 11 6d 00 00       	call   0x47979c
  472a8b:	c7 07 04 66 48 00    	mov    DWORD PTR [edi],0x486604
  472a91:	6a 48                	push   0x48
  472a93:	c6 44 24 20 01       	mov    BYTE PTR [esp+0x20],0x1
  472a98:	c7 06 98 65 48 00    	mov    DWORD PTR [esi],0x486598
  472a9e:	89 9e 90 00 00 00    	mov    DWORD PTR [esi+0x90],ebx
  472aa4:	e8 e3 6a 00 00       	call   0x47958c
  472aa9:	83 c4 04             	add    esp,0x4
  472aac:	89 44 24 2c          	mov    DWORD PTR [esp+0x2c],eax
  472ab0:	3b c3                	cmp    eax,ebx
  472ab2:	c6 44 24 1c 02       	mov    BYTE PTR [esp+0x1c],0x2
  472ab7:	74 49                	je     0x472b02
  472ab9:	8b ae b8 00 00 00    	mov    ebp,DWORD PTR [esi+0xb8]
  472abf:	6a 04                	push   0x4
  472ac1:	8b 96 ac 00 00 00    	mov    edx,DWORD PTR [esi+0xac]
  472ac7:	68 c8 00 00 00       	push   0xc8
  472acc:	83 ec 10             	sub    esp,0x10
  472acf:	8b 4a 08             	mov    ecx,DWORD PTR [edx+0x8]
  472ad2:	8b 7a 04             	mov    edi,DWORD PTR [edx+0x4]
  472ad5:	8b dc                	mov    ebx,esp
  472ad7:	51                   	push   ecx
  472ad8:	57                   	push   edi
  472ad9:	52                   	push   edx
  472ada:	89 2b                	mov    DWORD PTR [ebx],ebp
  472adc:	8b ae bc 00 00 00    	mov    ebp,DWORD PTR [esi+0xbc]
  472ae2:	8b c8                	mov    ecx,eax
  472ae4:	89 6b 04             	mov    DWORD PTR [ebx+0x4],ebp
  472ae7:	8b ae c0 00 00 00    	mov    ebp,DWORD PTR [esi+0xc0]
  472aed:	89 6b 08             	mov    DWORD PTR [ebx+0x8],ebp
  472af0:	8b ae c4 00 00 00    	mov    ebp,DWORD PTR [esi+0xc4]
  472af6:	89 6b 0c             	mov    DWORD PTR [ebx+0xc],ebp
  472af9:	e8 9c eb f8 ff       	call   0x40169a
  472afe:	33 db                	xor    ebx,ebx
