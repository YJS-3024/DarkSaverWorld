
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

0044af40 <.text+0x49f40>:
  44af40:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  44af46:	6a ff                	push   0xffffffff
  44af48:	68 18 e8 47 00       	push   0x47e818
  44af4d:	50                   	push   eax
  44af4e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  44af55:	83 ec 08             	sub    esp,0x8
  44af58:	57                   	push   edi
  44af59:	8b f9                	mov    edi,ecx
  44af5b:	8b 87 b0 00 00 00    	mov    eax,DWORD PTR [edi+0xb0]
  44af61:	83 78 40 1c          	cmp    DWORD PTR [eax+0x40],0x1c
  44af65:	7c 49                	jl     0x44afb0
  44af67:	68 64 05 49 00       	push   0x490564
  44af6c:	8d 4c 24 0c          	lea    ecx,[esp+0xc]
  44af70:	e8 2f e6 02 00       	call   0x4795a4
  44af75:	8d 4c 24 08          	lea    ecx,[esp+0x8]
  44af79:	6a 00                	push   0x0
  44af7b:	51                   	push   ecx
  44af7c:	8b 8f a0 00 00 00    	mov    ecx,DWORD PTR [edi+0xa0]
  44af82:	c7 44 24 1c 00 00 00 	mov    DWORD PTR [esp+0x1c],0x0
  44af89:	00 
  44af8a:	e8 6d 64 fb ff       	call   0x4013fc
  44af8f:	8d 4c 24 08          	lea    ecx,[esp+0x8]
  44af93:	c7 44 24 14 ff ff ff 	mov    DWORD PTR [esp+0x14],0xffffffff
  44af9a:	ff 
  44af9b:	e8 e6 e5 02 00       	call   0x479586
  44afa0:	5f                   	pop    edi
  44afa1:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  44afa5:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44afac:	83 c4 14             	add    esp,0x14
  44afaf:	c3                   	ret
  44afb0:	56                   	push   esi
  44afb1:	6a 0a                	push   0xa
  44afb3:	e8 d4 e5 02 00       	call   0x47958c
  44afb8:	8b 97 60 03 00 00    	mov    edx,DWORD PTR [edi+0x360]
  44afbe:	8b f0                	mov    esi,eax
  44afc0:	8b 87 bc 00 00 00    	mov    eax,DWORD PTR [edi+0xbc]
  44afc6:	8d 4c 24 0c          	lea    ecx,[esp+0xc]
  44afca:	6a 4b                	push   0x4b
  44afcc:	51                   	push   ecx
  44afcd:	c7 44 24 14 00 00 00 	mov    DWORD PTR [esp+0x14],0x0
  44afd4:	00 
  44afd5:	8b 3c 90             	mov    edi,DWORD PTR [eax+edx*4]
  44afd8:	56                   	push   esi
  44afd9:	e8 15 6d fb ff       	call   0x401cf3
  44afde:	8d 54 24 18          	lea    edx,[esp+0x18]
  44afe2:	57                   	push   edi
  44afe3:	52                   	push   edx
  44afe4:	56                   	push   esi
  44afe5:	e8 e5 6d fb ff       	call   0x401dcf
  44afea:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  44afee:	8b 0d a0 52 49 00    	mov    ecx,DWORD PTR ds:0x4952a0
  44aff4:	83 c4 1c             	add    esp,0x1c
  44aff7:	56                   	push   esi
  44aff8:	50                   	push   eax
  44aff9:	e8 8c 7b fb ff       	call   0x402b8a
  44affe:	56                   	push   esi
  44afff:	e8 7c e5 02 00       	call   0x479580
  44b004:	8b 4c 24 14          	mov    ecx,DWORD PTR [esp+0x14]
  44b008:	83 c4 04             	add    esp,0x4
  44b00b:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44b012:	5e                   	pop    esi
  44b013:	5f                   	pop    edi
  44b014:	83 c4 14             	add    esp,0x14
  44b017:	c3                   	ret
  44b018:	90                   	nop
  44b019:	90                   	nop
  44b01a:	90                   	nop
  44b01b:	90                   	nop
  44b01c:	90                   	nop
  44b01d:	90                   	nop
  44b01e:	90                   	nop
  44b01f:	90                   	nop
  44b020:	cc                   	int3
  44b021:	cc                   	int3
  44b022:	cc                   	int3
  44b023:	cc                   	int3
  44b024:	cc                   	int3
  44b025:	cc                   	int3
  44b026:	cc                   	int3
  44b027:	cc                   	int3
  44b028:	cc                   	int3
  44b029:	cc                   	int3
  44b02a:	cc                   	int3
  44b02b:	cc                   	int3
  44b02c:	cc                   	int3
  44b02d:	cc                   	int3
  44b02e:	cc                   	int3
  44b02f:	cc                   	int3
  44b030:	cc                   	int3
  44b031:	cc                   	int3
  44b032:	cc                   	int3
  44b033:	cc                   	int3
  44b034:	cc                   	int3
  44b035:	cc                   	int3
  44b036:	cc                   	int3
  44b037:	cc                   	int3
  44b038:	cc                   	int3
  44b039:	cc                   	int3
  44b03a:	cc                   	int3
  44b03b:	cc                   	int3
  44b03c:	cc                   	int3
  44b03d:	cc                   	int3
  44b03e:	cc                   	int3
  44b03f:	cc                   	int3
  44b040:	cc                   	int3
  44b041:	cc                   	int3
  44b042:	cc                   	int3
  44b043:	cc                   	int3
  44b044:	cc                   	int3
  44b045:	cc                   	int3
  44b046:	cc                   	int3
  44b047:	cc                   	int3
  44b048:	cc                   	int3
  44b049:	cc                   	int3
  44b04a:	cc                   	int3
  44b04b:	cc                   	int3
  44b04c:	cc                   	int3
  44b04d:	cc                   	int3
  44b04e:	cc                   	int3
  44b04f:	cc                   	int3
  44b050:	cc                   	int3
  44b051:	cc                   	int3
  44b052:	cc                   	int3
  44b053:	cc                   	int3
  44b054:	cc                   	int3
  44b055:	cc                   	int3
  44b056:	cc                   	int3
  44b057:	cc                   	int3
  44b058:	cc                   	int3
  44b059:	cc                   	int3
  44b05a:	cc                   	int3
  44b05b:	cc                   	int3
  44b05c:	cc                   	int3
  44b05d:	cc                   	int3
  44b05e:	cc                   	int3
  44b05f:	cc                   	int3
  44b060:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  44b066:	6a ff                	push   0xffffffff
  44b068:	68 38 e8 47 00       	push   0x47e838
  44b06d:	50                   	push   eax
  44b06e:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  44b075:	83 ec 08             	sub    esp,0x8
  44b078:	53                   	push   ebx
  44b079:	56                   	push   esi
  44b07a:	8b f1                	mov    esi,ecx
  44b07c:	57                   	push   edi
  44b07d:	8b 86 b0 00 00 00    	mov    eax,DWORD PTR [esi+0xb0]
  44b083:	8b 8e 68 03 00 00    	mov    ecx,DWORD PTR [esi+0x368]
  44b089:	8b 50 44             	mov    edx,DWORD PTR [eax+0x44]
  44b08c:	85 d2                	test   edx,edx
  44b08e:	74 14                	je     0x44b0a4
  44b090:	8b 58 48             	mov    ebx,DWORD PTR [eax+0x48]
  44b093:	85 db                	test   ebx,ebx
  44b095:	74 0d                	je     0x44b0a4
  44b097:	3b 48 40             	cmp    ecx,DWORD PTR [eax+0x40]
  44b09a:	7d 08                	jge    0x44b0a4
  44b09c:	8b 3c 8a             	mov    edi,DWORD PTR [edx+ecx*4]
  44b09f:	8b 1c 8b             	mov    ebx,DWORD PTR [ebx+ecx*4]
  44b0a2:	eb 05                	jmp    0x44b0a9
  44b0a4:	83 cf ff             	or     edi,0xffffffff
  44b0a7:	33 db                	xor    ebx,ebx
  44b0a9:	8b 06                	mov    eax,DWORD PTR [esi]
  44b0ab:	57                   	push   edi
  44b0ac:	8b ce                	mov    ecx,esi
  44b0ae:	ff 50 60             	call   DWORD PTR [eax+0x60]
  44b0b1:	85 c0                	test   eax,eax
  44b0b3:	75 4b                	jne    0x44b100
  44b0b5:	68 94 05 49 00       	push   0x490594
  44b0ba:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  44b0be:	e8 e1 e4 02 00       	call   0x4795a4
  44b0c3:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b0c7:	6a 00                	push   0x0
  44b0c9:	51                   	push   ecx
  44b0ca:	8b 8e a0 00 00 00    	mov    ecx,DWORD PTR [esi+0xa0]
  44b0d0:	c7 44 24 24 00 00 00 	mov    DWORD PTR [esp+0x24],0x0
  44b0d7:	00 
  44b0d8:	e8 1f 63 fb ff       	call   0x4013fc
  44b0dd:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  44b0e1:	c7 44 24 1c ff ff ff 	mov    DWORD PTR [esp+0x1c],0xffffffff
  44b0e8:	ff 
  44b0e9:	e8 98 e4 02 00       	call   0x479586
  44b0ee:	5f                   	pop    edi
  44b0ef:	5e                   	pop    esi
  44b0f0:	5b                   	pop    ebx
  44b0f1:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  44b0f5:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44b0fc:	83 c4 14             	add    esp,0x14
  44b0ff:	c3                   	ret
  44b100:	6a 0a                	push   0xa
  44b102:	e8 85 e4 02 00       	call   0x47958c
  44b107:	8d 54 24 10          	lea    edx,[esp+0x10]
  44b10b:	8b f0                	mov    esi,eax
  44b10d:	6a 4c                	push   0x4c
  44b10f:	52                   	push   edx
  44b110:	56                   	push   esi
  44b111:	c7 44 24 1c 00 00 00 	mov    DWORD PTR [esp+0x1c],0x0
  44b118:	00 
  44b119:	e8 d5 6b fb ff       	call   0x401cf3
  44b11e:	8d 44 24 1c          	lea    eax,[esp+0x1c]
  44b122:	57                   	push   edi
  44b123:	50                   	push   eax
  44b124:	56                   	push   esi
  44b125:	e8 a5 6c fb ff       	call   0x401dcf
  44b12a:	8d 4c 24 28          	lea    ecx,[esp+0x28]
  44b12e:	53                   	push   ebx
  44b12f:	51                   	push   ecx
  44b130:	56                   	push   esi
  44b131:	e8 99 6c fb ff       	call   0x401dcf
  44b136:	8b 54 24 34          	mov    edx,DWORD PTR [esp+0x34]
  44b13a:	8b 0d a0 52 49 00    	mov    ecx,DWORD PTR ds:0x4952a0
  44b140:	83 c4 28             	add    esp,0x28
  44b143:	56                   	push   esi
  44b144:	52                   	push   edx
  44b145:	e8 40 7a fb ff       	call   0x402b8a
  44b14a:	56                   	push   esi
  44b14b:	e8 30 e4 02 00       	call   0x479580
  44b150:	8b 4c 24 18          	mov    ecx,DWORD PTR [esp+0x18]
  44b154:	83 c4 04             	add    esp,0x4
  44b157:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  44b15e:	5f                   	pop    edi
  44b15f:	5e                   	pop    esi
  44b160:	5b                   	pop    ebx
  44b161:	83 c4 14             	add    esp,0x14
  44b164:	c3                   	ret
  44b165:	90                   	nop
  44b166:	90                   	nop
  44b167:	90                   	nop
  44b168:	90                   	nop
  44b169:	90                   	nop
  44b16a:	90                   	nop
  44b16b:	90                   	nop
  44b16c:	90                   	nop
  44b16d:	90                   	nop
  44b16e:	90                   	nop
  44b16f:	90                   	nop
  44b170:	cc                   	int3
  44b171:	cc                   	int3
  44b172:	cc                   	int3
  44b173:	cc                   	int3
  44b174:	cc                   	int3
  44b175:	cc                   	int3
  44b176:	cc                   	int3
  44b177:	cc                   	int3
  44b178:	cc                   	int3
  44b179:	cc                   	int3
  44b17a:	cc                   	int3
  44b17b:	cc                   	int3
  44b17c:	cc                   	int3
  44b17d:	cc                   	int3
  44b17e:	cc                   	int3
  44b17f:	cc                   	int3
  44b180:	cc                   	int3
  44b181:	cc                   	int3
  44b182:	cc                   	int3
  44b183:	cc                   	int3
  44b184:	cc                   	int3
  44b185:	cc                   	int3
  44b186:	cc                   	int3
  44b187:	cc                   	int3
  44b188:	cc                   	int3
  44b189:	cc                   	int3
  44b18a:	cc                   	int3
  44b18b:	cc                   	int3
  44b18c:	cc                   	int3
  44b18d:	cc                   	int3
  44b18e:	cc                   	int3
  44b18f:	cc                   	int3
  44b190:	cc                   	int3
  44b191:	cc                   	int3
  44b192:	cc                   	int3
  44b193:	cc                   	int3
  44b194:	cc                   	int3
  44b195:	cc                   	int3
  44b196:	cc                   	int3
  44b197:	cc                   	int3
  44b198:	cc                   	int3
  44b199:	cc                   	int3
  44b19a:	cc                   	int3
  44b19b:	cc                   	int3
  44b19c:	cc                   	int3
  44b19d:	cc                   	int3
  44b19e:	cc                   	int3
  44b19f:	cc                   	int3
  44b1a0:	cc                   	int3
  44b1a1:	cc                   	int3
  44b1a2:	cc                   	int3
  44b1a3:	cc                   	int3
  44b1a4:	cc                   	int3
  44b1a5:	cc                   	int3
  44b1a6:	cc                   	int3
  44b1a7:	cc                   	int3
  44b1a8:	cc                   	int3
  44b1a9:	cc                   	int3
  44b1aa:	cc                   	int3
  44b1ab:	cc                   	int3
  44b1ac:	cc                   	int3
  44b1ad:	cc                   	int3
  44b1ae:	cc                   	int3
  44b1af:	cc                   	int3
  44b1b0:	cc                   	int3
  44b1b1:	cc                   	int3
  44b1b2:	cc                   	int3
  44b1b3:	cc                   	int3
  44b1b4:	cc                   	int3
  44b1b5:	cc                   	int3
  44b1b6:	cc                   	int3
  44b1b7:	cc                   	int3
  44b1b8:	cc                   	int3
  44b1b9:	cc                   	int3
  44b1ba:	cc                   	int3
  44b1bb:	cc                   	int3
  44b1bc:	cc                   	int3
  44b1bd:	cc                   	int3
  44b1be:	cc                   	int3
  44b1bf:	cc                   	int3
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
