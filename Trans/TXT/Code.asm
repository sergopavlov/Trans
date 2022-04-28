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
main endp
end main
p mark1
mark0:
push 1
jmp mark2
mark1:
push 0
jmp mark2
mark2:
pop _c
main endp
end main
