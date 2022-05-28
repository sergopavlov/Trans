.686
.model flat
.data
_c dd ?
.code
main proc
push 1
push 5
push 2
pop ebx
pop eax
mul ebx
push eax
pop ebx
pop eax
add eax,ebx
push eax
pop _c
main endp
end main
