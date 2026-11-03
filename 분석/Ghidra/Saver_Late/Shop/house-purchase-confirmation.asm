
Saver.exe [SHA256 dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689]:     file format pei-i386


Disassembly of section .text:

004495f0 <.text+0x485f0>:
  4495f0:	6a ff                	push   0xffffffff
  4495f2:	68 c8 e6 47 00       	push   0x47e6c8
  4495f7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4495fd:	50                   	push   eax
  4495fe:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  449605:	51                   	push   ecx
  449606:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  44960a:	8b 54 24 1c          	mov    edx,DWORD PTR [esp+0x1c]
  44960e:	56                   	push   esi
  44960f:	8b f1                	mov    esi,ecx
  449611:	8b 4c 24 24          	mov    ecx,DWORD PTR [esp+0x24]
  449615:	57                   	push   edi
  449616:	50                   	push   eax
  449617:	8b 44 24 24          	mov    eax,DWORD PTR [esp+0x24]
  44961b:	51                   	push   ecx
  44961c:	8b 4c 24 24          	mov    ecx,DWORD PTR [esp+0x24]
  449620:	52                   	push   edx
  449621:	50                   	push   eax
  449622:	51                   	push   ecx
  449623:	8b ce                	mov    ecx,esi
  449625:	89 74 24 1c          	mov    DWORD PTR [esp+0x1c],esi
  449629:	e8 b2 99 fb ff       	call   0x402fe0
  44962e:	33 ff                	xor    edi,edi
  449630:	8d 8e c0 00 00 00    	lea    ecx,[esi+0xc0]
  449636:	89 7c 24 14          	mov    DWORD PTR [esp+0x14],edi
  44963a:	e8 59 ff 02 00       	call   0x479598
  44963f:	8b 4c 24 0c          	mov    ecx,DWORD PTR [esp+0xc]
  449643:	89 be b8 00 00 00    	mov    DWORD PTR [esi+0xb8],edi
  449649:	89 be 90 00 00 00    	mov    DWORD PTR [esi+0x90],edi
  44964f:	89 be bc 00 00 00    	mov    DWORD PTR [esi+0xbc],edi
  449655:	c7 06 2c 4d 48 00    	mov    DWORD PTR [esi],0x484d2c
  44965b:	8b c6                	mov    eax,esi
  44965d:	5f                   	pop    edi
  44965e:	5e                   	pop    esi
  44965f:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  449666:	83 c4 10             	add    esp,0x10
  449669:	c2 14 00             	ret    0x14
  44966c:	90                   	nop
  44966d:	90                   	nop
  44966e:	90                   	nop
  44966f:	90                   	nop
  449670:	cc                   	int3
  449671:	cc                   	int3
  449672:	cc                   	int3
  449673:	cc                   	int3
  449674:	cc                   	int3
  449675:	cc                   	int3
  449676:	cc                   	int3
  449677:	cc                   	int3
  449678:	cc                   	int3
  449679:	cc                   	int3
  44967a:	cc                   	int3
  44967b:	cc                   	int3
  44967c:	cc                   	int3
  44967d:	cc                   	int3
  44967e:	cc                   	int3
  44967f:	cc                   	int3
  449680:	cc                   	int3
  449681:	cc                   	int3
  449682:	cc                   	int3
  449683:	cc                   	int3
  449684:	cc                   	int3
  449685:	cc                   	int3
  449686:	cc                   	int3
  449687:	cc                   	int3
  449688:	cc                   	int3
  449689:	cc                   	int3
  44968a:	cc                   	int3
  44968b:	cc                   	int3
  44968c:	cc                   	int3
  44968d:	cc                   	int3
  44968e:	cc                   	int3
  44968f:	cc                   	int3
  449690:	b8 49 00 00 00       	mov    eax,0x49
  449695:	c3                   	ret
  449696:	90                   	nop
  449697:	90                   	nop
  449698:	90                   	nop
  449699:	90                   	nop
  44969a:	90                   	nop
  44969b:	90                   	nop
  44969c:	90                   	nop
  44969d:	90                   	nop
  44969e:	90                   	nop
  44969f:	90                   	nop
  4496a0:	cc                   	int3
  4496a1:	cc                   	int3
  4496a2:	cc                   	int3
  4496a3:	cc                   	int3
  4496a4:	cc                   	int3
  4496a5:	cc                   	int3
  4496a6:	cc                   	int3
  4496a7:	cc                   	int3
  4496a8:	cc                   	int3
  4496a9:	cc                   	int3
  4496aa:	cc                   	int3
  4496ab:	cc                   	int3
  4496ac:	cc                   	int3
  4496ad:	cc                   	int3
  4496ae:	cc                   	int3
  4496af:	cc                   	int3
  4496b0:	56                   	push   esi
  4496b1:	8b f1                	mov    esi,ecx
  4496b3:	e8 7f 92 fb ff       	call   0x402937
  4496b8:	f6 44 24 08 01       	test   BYTE PTR [esp+0x8],0x1
  4496bd:	74 09                	je     0x4496c8
  4496bf:	56                   	push   esi
  4496c0:	e8 bb fe 02 00       	call   0x479580
  4496c5:	83 c4 04             	add    esp,0x4
  4496c8:	8b c6                	mov    eax,esi
  4496ca:	5e                   	pop    esi
  4496cb:	c2 04 00             	ret    0x4
  4496ce:	90                   	nop
  4496cf:	90                   	nop
  4496d0:	cc                   	int3
  4496d1:	cc                   	int3
  4496d2:	cc                   	int3
  4496d3:	cc                   	int3
  4496d4:	cc                   	int3
  4496d5:	cc                   	int3
  4496d6:	cc                   	int3
  4496d7:	cc                   	int3
  4496d8:	cc                   	int3
  4496d9:	cc                   	int3
  4496da:	cc                   	int3
  4496db:	cc                   	int3
  4496dc:	cc                   	int3
  4496dd:	cc                   	int3
  4496de:	cc                   	int3
  4496df:	cc                   	int3
  4496e0:	6a ff                	push   0xffffffff
  4496e2:	68 e8 e6 47 00       	push   0x47e6e8
  4496e7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4496ed:	50                   	push   eax
  4496ee:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  4496f5:	51                   	push   ecx
  4496f6:	56                   	push   esi
  4496f7:	8b f1                	mov    esi,ecx
  4496f9:	89 74 24 04          	mov    DWORD PTR [esp+0x4],esi
  4496fd:	c7 06 2c 4d 48 00    	mov    DWORD PTR [esi],0x484d2c
  449703:	8d 8e c0 00 00 00    	lea    ecx,[esi+0xc0]
  449709:	c7 44 24 10 00 00 00 	mov    DWORD PTR [esp+0x10],0x0
  449710:	00 
  449711:	e8 70 fe 02 00       	call   0x479586
  449716:	8b ce                	mov    ecx,esi
  449718:	c7 44 24 10 ff ff ff 	mov    DWORD PTR [esp+0x10],0xffffffff
  44971f:	ff 
  449720:	e8 85 88 fb ff       	call   0x401faa
  449725:	8b 4c 24 08          	mov    ecx,DWORD PTR [esp+0x8]
  449729:	5e                   	pop    esi
  44972a:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  449731:	83 c4 10             	add    esp,0x10
  449734:	c3                   	ret
  449735:	90                   	nop
  449736:	90                   	nop
  449737:	90                   	nop
  449738:	90                   	nop
  449739:	90                   	nop
  44973a:	90                   	nop
  44973b:	90                   	nop
  44973c:	90                   	nop
  44973d:	90                   	nop
  44973e:	90                   	nop
  44973f:	90                   	nop
  449740:	cc                   	int3
  449741:	cc                   	int3
  449742:	cc                   	int3
  449743:	cc                   	int3
  449744:	cc                   	int3
  449745:	cc                   	int3
  449746:	cc                   	int3
  449747:	cc                   	int3
  449748:	cc                   	int3
  449749:	cc                   	int3
  44974a:	cc                   	int3
  44974b:	cc                   	int3
  44974c:	cc                   	int3
  44974d:	cc                   	int3
  44974e:	cc                   	int3
  44974f:	cc                   	int3
  449750:	cc                   	int3
  449751:	cc                   	int3
  449752:	cc                   	int3
  449753:	cc                   	int3
  449754:	cc                   	int3
  449755:	cc                   	int3
  449756:	cc                   	int3
  449757:	cc                   	int3
  449758:	cc                   	int3
  449759:	cc                   	int3
  44975a:	cc                   	int3
  44975b:	cc                   	int3
  44975c:	cc                   	int3
  44975d:	cc                   	int3
  44975e:	cc                   	int3
  44975f:	cc                   	int3
  449760:	8b 44 24 04          	mov    eax,DWORD PTR [esp+0x4]
  449764:	56                   	push   esi
  449765:	83 e8 00             	sub    eax,0x0
  449768:	8b f1                	mov    esi,ecx
  44976a:	74 07                	je     0x449773
  44976c:	48                   	dec    eax
  44976d:	74 0b                	je     0x44977a
  44976f:	5e                   	pop    esi
  449770:	c2 04 00             	ret    0x4
  449773:	8b ce                	mov    ecx,esi
  449775:	e8 31 7d fb ff       	call   0x4014ab
  44977a:	8b 06                	mov    eax,DWORD PTR [esi]
  44977c:	8b ce                	mov    ecx,esi
  44977e:	ff 50 04             	call   DWORD PTR [eax+0x4]
  449781:	8b 8e a0 00 00 00    	mov    ecx,DWORD PTR [esi+0xa0]
  449787:	50                   	push   eax
  449788:	e8 e9 7a fb ff       	call   0x401276
  44978d:	5e                   	pop    esi
  44978e:	c2 04 00             	ret    0x4
  449791:	90                   	nop
  449792:	90                   	nop
  449793:	90                   	nop
  449794:	90                   	nop
  449795:	90                   	nop
  449796:	90                   	nop
  449797:	90                   	nop
  449798:	90                   	nop
  449799:	90                   	nop
  44979a:	90                   	nop
  44979b:	90                   	nop
  44979c:	90                   	nop
  44979d:	90                   	nop
  44979e:	90                   	nop
  44979f:	90                   	nop
  4497a0:	cc                   	int3
  4497a1:	cc                   	int3
  4497a2:	cc                   	int3
  4497a3:	cc                   	int3
  4497a4:	cc                   	int3
  4497a5:	cc                   	int3
  4497a6:	cc                   	int3
  4497a7:	cc                   	int3
  4497a8:	cc                   	int3
  4497a9:	cc                   	int3
  4497aa:	cc                   	int3
  4497ab:	cc                   	int3
  4497ac:	cc                   	int3
  4497ad:	cc                   	int3
  4497ae:	cc                   	int3
  4497af:	cc                   	int3
  4497b0:	e9 0f 91 fb ff       	jmp    0x4028c4
  4497b5:	90                   	nop
  4497b6:	90                   	nop
  4497b7:	90                   	nop
  4497b8:	90                   	nop
  4497b9:	90                   	nop
  4497ba:	90                   	nop
  4497bb:	90                   	nop
  4497bc:	90                   	nop
  4497bd:	90                   	nop
  4497be:	90                   	nop
  4497bf:	90                   	nop
  4497c0:	cc                   	int3
  4497c1:	cc                   	int3
  4497c2:	cc                   	int3
  4497c3:	cc                   	int3
  4497c4:	cc                   	int3
  4497c5:	cc                   	int3
  4497c6:	cc                   	int3
  4497c7:	cc                   	int3
  4497c8:	cc                   	int3
  4497c9:	cc                   	int3
  4497ca:	cc                   	int3
  4497cb:	cc                   	int3
  4497cc:	cc                   	int3
  4497cd:	cc                   	int3
  4497ce:	cc                   	int3
  4497cf:	cc                   	int3
  4497d0:	6a ff                	push   0xffffffff
  4497d2:	68 0b e7 47 00       	push   0x47e70b
  4497d7:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4497dd:	50                   	push   eax
  4497de:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  4497e5:	81 ec 80 00 00 00    	sub    esp,0x80
  4497eb:	53                   	push   ebx
  4497ec:	55                   	push   ebp
  4497ed:	56                   	push   esi
  4497ee:	8b f1                	mov    esi,ecx
  4497f0:	57                   	push   edi
  4497f1:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  4497f5:	e8 9e fd 02 00       	call   0x479598
  4497fa:	8b 86 bc 00 00 00    	mov    eax,DWORD PTR [esi+0xbc]
  449800:	8b 8e b8 00 00 00    	mov    ecx,DWORD PTR [esi+0xb8]
  449806:	8b 96 c0 00 00 00    	mov    edx,DWORD PTR [esi+0xc0]
  44980c:	50                   	push   eax
  44980d:	51                   	push   ecx
  44980e:	52                   	push   edx
  44980f:	8d 44 24 1c          	lea    eax,[esp+0x1c]
  449813:	68 3c 05 49 00       	push   0x49053c
  449818:	50                   	push   eax
  449819:	c7 84 24 ac 00 00 00 	mov    DWORD PTR [esp+0xac],0x0
  449820:	00 00 00 00 
  449824:	e8 a5 fd 02 00       	call   0x4795ce
  449829:	8b 4e 44             	mov    ecx,DWORD PTR [esi+0x44]
  44982c:	8b 46 40             	mov    eax,DWORD PTR [esi+0x40]
  44982f:	8b bc 24 b4 00 00 00 	mov    edi,DWORD PTR [esp+0xb4]
  449836:	83 c4 14             	add    esp,0x14
  449839:	8d 69 13             	lea    ebp,[ecx+0x13]
  44983c:	8d 4c 24 14          	lea    ecx,[esp+0x14]
  449840:	51                   	push   ecx
  449841:	8b cf                	mov    ecx,edi
  449843:	8d 98 08 01 00 00    	lea    ebx,[eax+0x108]
  449849:	ff 15 38 8f 49 00    	call   DWORD PTR ds:0x498f38
  44984f:	8b cf                	mov    ecx,edi
  449851:	ff 15 04 8f 49 00    	call   DWORD PTR ds:0x498f04
  449857:	89 44 24 38          	mov    DWORD PTR [esp+0x38],eax
  44985b:	8b 86 ac 00 00 00    	mov    eax,DWORD PTR [esi+0xac]
  449861:	6a 06                	push   0x6
  449863:	83 38 00             	cmp    DWORD PTR [eax],0x0
  449866:	74 25                	je     0x44988d
  449868:	8b 54 24 14          	mov    edx,DWORD PTR [esp+0x14]
  44986c:	83 ec 7c             	sub    esp,0x7c
  44986f:	b9 1f 00 00 00       	mov    ecx,0x1f
  449874:	8d b4 24 94 00 00 00 	lea    esi,[esp+0x94]
  44987b:	8b fc                	mov    edi,esp
  44987d:	6a 00                	push   0x0
  44987f:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  449881:	52                   	push   edx
  449882:	55                   	push   ebp
  449883:	53                   	push   ebx
  449884:	8b c8                	mov    ecx,eax
  449886:	e8 99 9e fb ff       	call   0x403724
  44988b:	eb 26                	jmp    0x4498b3
  44988d:	83 ec 7c             	sub    esp,0x7c
  449890:	b9 1f 00 00 00       	mov    ecx,0x1f
  449895:	8d b4 24 94 00 00 00 	lea    esi,[esp+0x94]
  44989c:	8b fc                	mov    edi,esp
  44989e:	f3 a5                	rep movs DWORD PTR es:[edi],DWORD PTR ds:[esi]
  4498a0:	8b 8c 24 90 00 00 00 	mov    ecx,DWORD PTR [esp+0x90]
  4498a7:	6a 00                	push   0x0
  4498a9:	51                   	push   ecx
  4498aa:	55                   	push   ebp
  4498ab:	53                   	push   ebx
  4498ac:	8b c8                	mov    ecx,eax
  4498ae:	e8 d7 97 fb ff       	call   0x40308a
  4498b3:	8b 8c 24 a0 00 00 00 	mov    ecx,DWORD PTR [esp+0xa0]
  4498ba:	ff 15 00 8f 49 00    	call   DWORD PTR ds:0x498f00
  4498c0:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  4498c4:	c7 84 24 98 00 00 00 	mov    DWORD PTR [esp+0x98],0xffffffff
  4498cb:	ff ff ff ff 
  4498cf:	e8 b2 fc 02 00       	call   0x479586
  4498d4:	8b 8c 24 90 00 00 00 	mov    ecx,DWORD PTR [esp+0x90]
  4498db:	5f                   	pop    edi
  4498dc:	5e                   	pop    esi
  4498dd:	5d                   	pop    ebp
  4498de:	b8 01 00 00 00       	mov    eax,0x1
  4498e3:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  4498ea:	5b                   	pop    ebx
  4498eb:	81 c4 8c 00 00 00    	add    esp,0x8c
  4498f1:	c2 04 00             	ret    0x4
  4498f4:	90                   	nop
  4498f5:	90                   	nop
  4498f6:	90                   	nop
  4498f7:	90                   	nop
  4498f8:	90                   	nop
  4498f9:	90                   	nop
  4498fa:	90                   	nop
  4498fb:	90                   	nop
  4498fc:	90                   	nop
  4498fd:	90                   	nop
  4498fe:	90                   	nop
  4498ff:	90                   	nop
  449900:	cc                   	int3
  449901:	cc                   	int3
  449902:	cc                   	int3
  449903:	cc                   	int3
  449904:	cc                   	int3
  449905:	cc                   	int3
  449906:	cc                   	int3
  449907:	cc                   	int3
  449908:	cc                   	int3
  449909:	cc                   	int3
  44990a:	cc                   	int3
  44990b:	cc                   	int3
  44990c:	cc                   	int3
  44990d:	cc                   	int3
  44990e:	cc                   	int3
  44990f:	cc                   	int3
  449910:	cc                   	int3
  449911:	cc                   	int3
  449912:	cc                   	int3
  449913:	cc                   	int3
  449914:	cc                   	int3
  449915:	cc                   	int3
  449916:	cc                   	int3
  449917:	cc                   	int3
  449918:	cc                   	int3
  449919:	cc                   	int3
  44991a:	cc                   	int3
  44991b:	cc                   	int3
  44991c:	cc                   	int3
  44991d:	cc                   	int3
  44991e:	cc                   	int3
  44991f:	cc                   	int3
  449920:	cc                   	int3
  449921:	cc                   	int3
  449922:	cc                   	int3
  449923:	cc                   	int3
  449924:	cc                   	int3
  449925:	cc                   	int3
  449926:	cc                   	int3
  449927:	cc                   	int3
  449928:	cc                   	int3
  449929:	cc                   	int3
  44992a:	cc                   	int3
  44992b:	cc                   	int3
  44992c:	cc                   	int3
  44992d:	cc                   	int3
  44992e:	cc                   	int3
  44992f:	cc                   	int3
  449930:	cc                   	int3
  449931:	cc                   	int3
  449932:	cc                   	int3
  449933:	cc                   	int3
  449934:	cc                   	int3
  449935:	cc                   	int3
  449936:	cc                   	int3
  449937:	cc                   	int3
  449938:	cc                   	int3
  449939:	cc                   	int3
  44993a:	cc                   	int3
  44993b:	cc                   	int3
  44993c:	cc                   	int3
  44993d:	cc                   	int3
  44993e:	cc                   	int3
  44993f:	cc                   	int3
  449940:	cc                   	int3
  449941:	cc                   	int3
  449942:	cc                   	int3
  449943:	cc                   	int3
  449944:	cc                   	int3
  449945:	cc                   	int3
  449946:	cc                   	int3
  449947:	cc                   	int3
  449948:	cc                   	int3
  449949:	cc                   	int3
  44994a:	cc                   	int3
  44994b:	cc                   	int3
  44994c:	cc                   	int3
  44994d:	cc                   	int3
  44994e:	cc                   	int3
  44994f:	cc                   	int3
  449950:	51                   	push   ecx
  449951:	56                   	push   esi
  449952:	6a 14                	push   0x14
  449954:	e8 33 fc 02 00       	call   0x47958c
  449959:	8b f0                	mov    esi,eax
  44995b:	8d 44 24 08          	lea    eax,[esp+0x8]
  44995f:	68 d8 00 00 00       	push   0xd8
  449964:	50                   	push   eax
  449965:	56                   	push   esi
  449966:	c7 44 24 14 00 00 00 	mov    DWORD PTR [esp+0x14],0x0
  44996d:	00 
  44996e:	e8 80 83 fb ff       	call   0x401cf3
  449973:	8b 4c 24 14          	mov    ecx,DWORD PTR [esp+0x14]
  449977:	83 c4 10             	add    esp,0x10
  44997a:	56                   	push   esi
  44997b:	51                   	push   ecx
  44997c:	8b 0d a0 52 49 00    	mov    ecx,DWORD PTR ds:0x4952a0
  449982:	e8 03 92 fb ff       	call   0x402b8a
  449987:	56                   	push   esi
  449988:	e8 f3 fb 02 00       	call   0x479580
  44998d:	83 c4 04             	add    esp,0x4
  449990:	5e                   	pop    esi
  449991:	59                   	pop    ecx
  449992:	c3                   	ret
  449993:	90                   	nop
  449994:	90                   	nop
  449995:	90                   	nop
  449996:	90                   	nop
  449997:	90                   	nop
  449998:	90                   	nop
  449999:	90                   	nop
  44999a:	90                   	nop
  44999b:	90                   	nop
  44999c:	90                   	nop
  44999d:	90                   	nop
  44999e:	90                   	nop
  44999f:	90                   	nop
  4499a0:	cc                   	int3
  4499a1:	cc                   	int3
  4499a2:	cc                   	int3
  4499a3:	cc                   	int3
  4499a4:	cc                   	int3
  4499a5:	cc                   	int3
  4499a6:	cc                   	int3
  4499a7:	cc                   	int3
  4499a8:	cc                   	int3
  4499a9:	cc                   	int3
  4499aa:	cc                   	int3
  4499ab:	cc                   	int3
  4499ac:	cc                   	int3
  4499ad:	cc                   	int3
  4499ae:	cc                   	int3
  4499af:	cc                   	int3
  4499b0:	cc                   	int3
  4499b1:	cc                   	int3
  4499b2:	cc                   	int3
  4499b3:	cc                   	int3
  4499b4:	cc                   	int3
  4499b5:	cc                   	int3
  4499b6:	cc                   	int3
  4499b7:	cc                   	int3
  4499b8:	cc                   	int3
  4499b9:	cc                   	int3
  4499ba:	cc                   	int3
  4499bb:	cc                   	int3
  4499bc:	cc                   	int3
  4499bd:	cc                   	int3
  4499be:	cc                   	int3
  4499bf:	cc                   	int3
  4499c0:	64 a1 00 00 00 00    	mov    eax,fs:0x0
  4499c6:	6a ff                	push   0xffffffff
  4499c8:	68 28 e7 47 00       	push   0x47e728
  4499cd:	50                   	push   eax
  4499ce:	64 89 25 00 00 00 00 	mov    DWORD PTR fs:0x0,esp
  4499d5:	8b 44 24 14          	mov    eax,DWORD PTR [esp+0x14]
  4499d9:	8b 54 24 18          	mov    edx,DWORD PTR [esp+0x18]
  4499dd:	89 81 b8 00 00 00    	mov    DWORD PTR [ecx+0xb8],eax
  4499e3:	8d 44 24 10          	lea    eax,[esp+0x10]
  4499e7:	89 91 bc 00 00 00    	mov    DWORD PTR [ecx+0xbc],edx
  4499ed:	50                   	push   eax
  4499ee:	81 c1 c0 00 00 00    	add    ecx,0xc0
  4499f4:	c7 44 24 0c 00 00 00 	mov    DWORD PTR [esp+0xc],0x0
  4499fb:	00 
  4499fc:	e8 d9 fb 02 00       	call   0x4795da
  449a01:	8d 4c 24 10          	lea    ecx,[esp+0x10]
  449a05:	c7 44 24 08 ff ff ff 	mov    DWORD PTR [esp+0x8],0xffffffff
  449a0c:	ff 
  449a0d:	e8 74 fb 02 00       	call   0x479586
  449a12:	8b 4c 24 00          	mov    ecx,DWORD PTR [esp+0x0]
  449a16:	64 89 0d 00 00 00 00 	mov    DWORD PTR fs:0x0,ecx
  449a1d:	83 c4 0c             	add    esp,0xc
  449a20:	c2 0c 00             	ret    0xc
  449a23:	90                   	nop
  449a24:	90                   	nop
  449a25:	90                   	nop
  449a26:	90                   	nop
  449a27:	90                   	nop
  449a28:	90                   	nop
  449a29:	90                   	nop
  449a2a:	90                   	nop
  449a2b:	90                   	nop
  449a2c:	90                   	nop
  449a2d:	90                   	nop
  449a2e:	90                   	nop
  449a2f:	90                   	nop
  449a30:	cc                   	int3
  449a31:	cc                   	int3
  449a32:	cc                   	int3
  449a33:	cc                   	int3
  449a34:	cc                   	int3
  449a35:	cc                   	int3
  449a36:	cc                   	int3
  449a37:	cc                   	int3
  449a38:	cc                   	int3
  449a39:	cc                   	int3
  449a3a:	cc                   	int3
  449a3b:	cc                   	int3
  449a3c:	cc                   	int3
  449a3d:	cc                   	int3
  449a3e:	cc                   	int3
  449a3f:	cc                   	int3
  449a40:	cc                   	int3
  449a41:	cc                   	int3
  449a42:	cc                   	int3
  449a43:	cc                   	int3
  449a44:	cc                   	int3
  449a45:	cc                   	int3
  449a46:	cc                   	int3
  449a47:	cc                   	int3
  449a48:	cc                   	int3
  449a49:	cc                   	int3
  449a4a:	cc                   	int3
  449a4b:	cc                   	int3
  449a4c:	cc                   	int3
  449a4d:	cc                   	int3
  449a4e:	cc                   	int3
  449a4f:	cc                   	int3
  449a50:	e8 1b 00 00 00       	call   0x449a70
  449a55:	e9 36 00 00 00       	jmp    0x449a90
  449a5a:	90                   	nop
  449a5b:	90                   	nop
  449a5c:	90                   	nop
  449a5d:	90                   	nop
  449a5e:	90                   	nop
  449a5f:	90                   	nop
  449a60:	cc                   	int3
  449a61:	cc                   	int3
  449a62:	cc                   	int3
  449a63:	cc                   	int3
  449a64:	cc                   	int3
  449a65:	cc                   	int3
  449a66:	cc                   	int3
  449a67:	cc                   	int3
  449a68:	cc                   	int3
  449a69:	cc                   	int3
  449a6a:	cc                   	int3
  449a6b:	cc                   	int3
  449a6c:	cc                   	int3
  449a6d:	cc                   	int3
  449a6e:	cc                   	int3
  449a6f:	cc                   	int3
  449a70:	b9 55 7b 49 00       	mov    ecx,0x497b55
  449a75:	ff 25 30 97 49 00    	jmp    DWORD PTR ds:0x499730
  449a7b:	90                   	nop
  449a7c:	90                   	nop
  449a7d:	90                   	nop
  449a7e:	90                   	nop
  449a7f:	90                   	nop
  449a80:	cc                   	int3
  449a81:	cc                   	int3
  449a82:	cc                   	int3
  449a83:	cc                   	int3
  449a84:	cc                   	int3
  449a85:	cc                   	int3
  449a86:	cc                   	int3
  449a87:	cc                   	int3
  449a88:	cc                   	int3
  449a89:	cc                   	int3
  449a8a:	cc                   	int3
  449a8b:	cc                   	int3
  449a8c:	cc                   	int3
  449a8d:	cc                   	int3
  449a8e:	cc                   	int3
  449a8f:	cc                   	int3
  449a90:	68 b0 9a 44 00       	push   0x449ab0
  449a95:	e8 24 01 03 00       	call   0x479bbe
  449a9a:	59                   	pop    ecx
  449a9b:	c3                   	ret
  449a9c:	90                   	nop
  449a9d:	90                   	nop
  449a9e:	90                   	nop
  449a9f:	90                   	nop
  449aa0:	cc                   	int3
  449aa1:	cc                   	int3
  449aa2:	cc                   	int3
  449aa3:	cc                   	int3
  449aa4:	cc                   	int3
  449aa5:	cc                   	int3
  449aa6:	cc                   	int3
  449aa7:	cc                   	int3
  449aa8:	cc                   	int3
  449aa9:	cc                   	int3
  449aaa:	cc                   	int3
  449aab:	cc                   	int3
  449aac:	cc                   	int3
  449aad:	cc                   	int3
  449aae:	cc                   	int3
  449aaf:	cc                   	int3
  449ab0:	b9 55 7b 49 00       	mov    ecx,0x497b55
  449ab5:	ff 25 28 97 49 00    	jmp    DWORD PTR ds:0x499728
  449abb:	90                   	nop
  449abc:	90                   	nop
  449abd:	90                   	nop
  449abe:	90                   	nop
  449abf:	90                   	nop
  449ac0:	cc                   	int3
  449ac1:	cc                   	int3
  449ac2:	cc                   	int3
  449ac3:	cc                   	int3
  449ac4:	cc                   	int3
  449ac5:	cc                   	int3
  449ac6:	cc                   	int3
  449ac7:	cc                   	int3
  449ac8:	cc                   	int3
  449ac9:	cc                   	int3
  449aca:	cc                   	int3
  449acb:	cc                   	int3
  449acc:	cc                   	int3
  449acd:	cc                   	int3
  449ace:	cc                   	int3
  449acf:	cc                   	int3
  449ad0:	e8 1b 00 00 00       	call   0x449af0
  449ad5:	e9 36 00 00 00       	jmp    0x449b10
  449ada:	90                   	nop
  449adb:	90                   	nop
  449adc:	90                   	nop
  449add:	90                   	nop
  449ade:	90                   	nop
  449adf:	90                   	nop
  449ae0:	cc                   	int3
  449ae1:	cc                   	int3
  449ae2:	cc                   	int3
  449ae3:	cc                   	int3
  449ae4:	cc                   	int3
  449ae5:	cc                   	int3
  449ae6:	cc                   	int3
  449ae7:	cc                   	int3
  449ae8:	cc                   	int3
  449ae9:	cc                   	int3
  449aea:	cc                   	int3
  449aeb:	cc                   	int3
  449aec:	cc                   	int3
  449aed:	cc                   	int3
  449aee:	cc                   	int3
  449aef:	cc                   	int3
  449af0:	b9 54 7b 49 00       	mov    ecx,0x497b54
  449af5:	ff 25 34 97 49 00    	jmp    DWORD PTR ds:0x499734
  449afb:	90                   	nop
  449afc:	90                   	nop
  449afd:	90                   	nop
  449afe:	90                   	nop
  449aff:	90                   	nop
  449b00:	cc                   	int3
  449b01:	cc                   	int3
  449b02:	cc                   	int3
  449b03:	cc                   	int3
  449b04:	cc                   	int3
  449b05:	cc                   	int3
  449b06:	cc                   	int3
  449b07:	cc                   	int3
  449b08:	cc                   	int3
  449b09:	cc                   	int3
  449b0a:	cc                   	int3
  449b0b:	cc                   	int3
  449b0c:	cc                   	int3
  449b0d:	cc                   	int3
  449b0e:	cc                   	int3
  449b0f:	cc                   	int3
  449b10:	68 30 9b 44 00       	push   0x449b30
  449b15:	e8 a4 00 03 00       	call   0x479bbe
  449b1a:	59                   	pop    ecx
  449b1b:	c3                   	ret
  449b1c:	90                   	nop
  449b1d:	90                   	nop
  449b1e:	90                   	nop
  449b1f:	90                   	nop
  449b20:	cc                   	int3
  449b21:	cc                   	int3
  449b22:	cc                   	int3
  449b23:	cc                   	int3
  449b24:	cc                   	int3
  449b25:	cc                   	int3
  449b26:	cc                   	int3
  449b27:	cc                   	int3
  449b28:	cc                   	int3
  449b29:	cc                   	int3
  449b2a:	cc                   	int3
  449b2b:	cc                   	int3
  449b2c:	cc                   	int3
  449b2d:	cc                   	int3
  449b2e:	cc                   	int3
  449b2f:	cc                   	int3
  449b30:	b9 54 7b 49 00       	mov    ecx,0x497b54
  449b35:	ff 25 2c 97 49 00    	jmp    DWORD PTR ds:0x49972c
  449b3b:	90                   	nop
  449b3c:	90                   	nop
  449b3d:	90                   	nop
  449b3e:	90                   	nop
  449b3f:	90                   	nop
  449b40:	cc                   	int3
  449b41:	cc                   	int3
  449b42:	cc                   	int3
  449b43:	cc                   	int3
  449b44:	cc                   	int3
  449b45:	cc                   	int3
  449b46:	cc                   	int3
  449b47:	cc                   	int3
  449b48:	cc                   	int3
  449b49:	cc                   	int3
  449b4a:	cc                   	int3
  449b4b:	cc                   	int3
  449b4c:	cc                   	int3
  449b4d:	cc                   	int3
  449b4e:	cc                   	int3
  449b4f:	cc                   	int3
