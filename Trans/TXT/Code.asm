.686
.model flat
.data
_a dd ?
_b dd ?
_c dd ?
_d dd ?
_e dd ?
_f dd ?
_g dd ?
_h dd ?
_i dd ?
_j dd ?
_k dd ?
.code
main proc
push 5
pop _a
push 10
pop _b
push 18
pop _c
push _a
push 5
pop ebx
pop eax
add eax,ebx
push eax
push _c
push _b
pop ebx
pop eax
sub eax,ebx
push eax
pop ebx
pop eax
mul ebx
push eax
push 2
pop ebx
pop eax
xor edx, edx
div ebx
push eax
pop _d
push _a
push 2
pop ebx
pop eax
div ebx
push edx
pop _e
push _a
push _b
pop ebx
pop eax
cmp eax,ebx
jg mark0
jmp mark1
mark0:
push 1
jmp mark2
mark1:
push 0
jmp mark2
mark2:
pop _f
push _a
push _b
pop ebx
pop eax
cmp eax,ebx
jl mark3
jmp mark4
mark3:
push 1
jmp mark5
mark4:
push 0
jmp mark5
mark5:
pop _g
push _a
push _b
pop ebx
pop eax
cmp eax,ebx
je mark6
jmp mark7
mark6:
push 1
jmp mark8
mark7:
push 0
jmp mark8
mark8:
pop _h
push _a
push _c
pop ebx
pop eax
cmp eax,ebx
jne mark9
jmp mark10
mark9:
push 1
jmp mark11
mark10:
push 0
jmp mark11
mark11:
pop _i
push _a
push _b
pop ebx
pop eax
cmp eax,ebx
jge mark12
jmp mark13
mark12:
push 1
jmp mark14
mark13:
push 0
jmp mark14
mark14:
pop _j
push _a
push _b
pop ebx
pop eax
cmp eax,ebx
jle mark15
jmp mark16
mark15:
push 1
jmp mark17
mark16:
push 0
jmp mark17
mark17:
pop _k
push _b
pop ebx
mov eax, _a
add eax,ebx
mov _a, eax
push 5
pop ebx
mov eax, _a
sub eax,ebx
mov _a, eax
push 2
pop ebx
mov eax, _c
mul ebx
mov _c, eax
push 2
pop ebx
mov eax, _b
div ebx
mov _b, eax
main endp
end main
