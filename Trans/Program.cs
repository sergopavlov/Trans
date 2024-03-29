﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Trans
{
    public class TableContainer
    {
        string path;
        string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789%^+=-*/-><! \t\r\n(){};\0";
        string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        string Didgits = "0123456789";
        string Operations = "%^!><+=-*";
        string Splitters = " \t\r\n(){};";
        Dictionary<(int, int), int> priorities = new();
        public ConstantTable keywordsTable;
        public ConstantTable splittersTable;
        public ConstantTable operationsTable;
        public VariableTable constantsTable;
        public VariableTable identificatorsTable;
        public List<List<(int, int)>> instructions = new();
        public List<(int, int)> Tokens = new List<(int, int)>();
        public bool LexAnaliz()
        {
            bool res = true;
            string text = File.ReadAllText(path);
            text += "\0";
            (int, int)? curtoken;
            int i = 0;
            do
            {
                if (!GenerateToken(text, ref i, out curtoken))
                {
                    res = false;
                }
                else
                {
                    if (curtoken != null)
                    {
                        if (curtoken.Value.Item1 == 5)
                        {
                            break;
                        }
                        Tokens.Add(((int, int))curtoken);
                    }
                }
            } while (res);
            return res;
        }
        public bool SyntaxAnaliz()
        {
            bool res = true;
            int num = 5;
            if (Tokens[0] != keywordsTable.GetToken("int") || Tokens[1] != keywordsTable.GetToken("main") || Tokens[2] != splittersTable.GetToken("(") || Tokens[3] != splittersTable.GetToken(")") || Tokens[4] != splittersTable.GetToken("{"))
            {
                res = false;
                Console.WriteLine("Не найдено определение головной функции");
            }
            else
            {
                List<(int, int)> curVariables = new List<(int, int)>();
                bool flag = false;
                bool isleftvariableused = false;
                bool IsInited = false;
                int stateoferror = 0;
                int curleftindex = 0;
                (int, int) curlefttoken;
                int instrnum = 0;
                while (Tokens[num] != splittersTable.GetToken("}"))
                {
                    curVariables.Clear();
                    if (Tokens[num] == keywordsTable.GetToken("int"))
                    {
                        num++;
                        if (Tokens[num].Item1 == 4)
                        {
                            if (identificatorsTable.Find(Tokens[num].Item2).Type == Lexeme.TYPE.Int)
                            {
                                Console.WriteLine("Повторное объявление переменной");
                                res = false;
                                break;
                            }
                            else
                            {
                                curlefttoken = Tokens[num];
                                identificatorsTable.Find(Tokens[num].Item2).Type = Lexeme.TYPE.Int;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ожидалось название переменной");
                            res = false;
                            break;
                        }
                    }
                    else
                    {
                        if (identificatorsTable.Find(Tokens[num].Item2).Type == Lexeme.TYPE.None)
                        {
                            Console.WriteLine("Неопределенный идентификатор");
                            res = false;
                            break;
                        }
                    }
                    curlefttoken = Tokens[num];
                    curleftindex = num;
                    if (ExpressionAnalysys(ref num, ref curVariables, ref isleftvariableused, ref stateoferror, ref IsInited))
                    {
                        if (isleftvariableused && identificatorsTable.Find(Tokens[curleftindex].Item2).IsInit == false)
                        {
                            Console.WriteLine("Использование неинициализированной переменной");
                            res = false;
                            break;
                        }
                        foreach (var item in curVariables)
                        {
                            if (!identificatorsTable.Find(item.Item2).IsInit)
                                res = false;

                        }
                        if (!res)
                        {
                            Console.WriteLine("Использование неинициализированной переменной");
                            break;
                        }
                        if (IsInited)
                        {
                            identificatorsTable.Find(Tokens[curleftindex].Item2).IsInit = true;
                        }
                        //постфиксная запись
                        Stack<(int, int)> st = new();
                        Queue<(int, int)> q = new();
                        int len = num - curleftindex - 1;
                        for (int i = curleftindex; i < num - 1; i++)
                        {
                            if (Tokens[i].Item1 == 4 || Tokens[i].Item1 == 3)
                            {
                                q.Enqueue(Tokens[i]);
                            }
                            if (Tokens[i].Item1 == 2)
                            {
                                if (st.Count == 0 || st.Peek() == splittersTable.GetToken("("))
                                    st.Push(Tokens[i]);
                                else
                                {
                                    if (priorities[st.Peek()] < priorities[Tokens[i]])
                                        st.Push(Tokens[i]);
                                    else
                                    {
                                        bool iterate = true;
                                        do
                                        {
                                            q.Enqueue(st.Pop());
                                            if (st.Count > 0)
                                            {
                                                if (priorities[st.Peek()] < priorities[Tokens[i]])
                                                {
                                                    iterate = false;
                                                }
                                            }
                                            else
                                                iterate = false;
                                        } while (iterate);
                                        st.Push(Tokens[i]);
                                    }
                                }
                            }
                            if (Tokens[i] == splittersTable.GetToken("("))
                                st.Push(Tokens[i]);
                            if (Tokens[i] == splittersTable.GetToken(")"))
                            {
                                while (st.Peek() != splittersTable.GetToken("("))
                                {
                                    q.Enqueue(st.Pop());
                                }
                                st.Pop();
                            }
                        }
                        while (st.Count > 0)
                            q.Enqueue(st.Pop());
                        instructions.Add(new());
                        while (q.Count > 0)
                        {
                            instructions[instrnum].Add(q.Dequeue());
                        }
                        instrnum++;
                    }
                    else
                    {
                        Console.WriteLine("ОШИБКА");
                        res = false;
                        break;
                        //разобрать ошибку по состоянию
                    }
                }

            }

            return res;
        }
        public bool ExpressionAnalysys(ref int index, ref List<(int, int)> Variables, ref bool isleftvariableused, ref int stateoferror, ref bool IsInited)
        {
            isleftvariableused = false;
            stateoferror = 0;
            int curstate = 0;
            IsInited = false;
            Stack<int> stack = new();
            bool res = true;
            bool flag = true;
            (int, int) curtoken = Tokens[index];
            curtoken.Item2 = -1;
            do
            {
                if (curstate == 9)
                    isleftvariableused = true;
                if (curstate == 4)
                    IsInited = true;
                if (SyntaxTable[curstate].Tokens.Contains(curtoken))
                {
                    if (SyntaxTable[curstate].accept)
                    {
                        index++;
                        curtoken = Tokens[index];
                        if (curtoken.Item1 == 4 && curstate != 6 && curstate != 7)
                        {
                            Variables.Add(curtoken);
                            curtoken.Item2 = -1;
                        }
                        if (curtoken.Item1 == 3)
                            curtoken.Item2 = -1;
                    }
                    if (SyntaxTable[curstate].stack)
                        stack.Push(curstate + 1);
                    if (SyntaxTable[curstate].jump > 0)
                        curstate = SyntaxTable[curstate].jump;
                    else
                    {
                        if (SyntaxTable[curstate].ret)
                        {
                            if (stack.Count > 0)
                            {
                                curstate = stack.Pop();
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                    }
                }
                else
                {
                    if (SyntaxTable[curstate].erorr)
                    {
                        res = false;
                        stateoferror = curstate;
                    }
                    else
                    {
                        curstate++;
                    }
                }
            } while (curstate > 0 && res && flag);
            return res;
        }
        private SyntaxTableElem[] SyntaxTable;
        public enum State
        {
            S,
            Int,
            Identificator,
            Slash,
            Comment1,
            Comment2,
            Commentstar,
            EndComment2,
            Splitter,
            Operation1,//больше
            Operation2,//меньше
            Operation3,//одно/двусимвольные с =
            Operation4,//!
            Operation5,//после двухсимвольного
            Operation6,//^ %
            Operation7,//  /=
            Empty,//пробел ентер
            EndInt,
            EndId,
            EndSplitter,
            EndOperation,
            End,
            EndEnd,
            Fail
        }
        static int countofstates = 18;
        Dictionary<char, State>[] perehod = new Dictionary<char, State>[countofstates];
        public bool GenerateToken(string Text, ref int pointer, out (int, int)? token)
        {
            bool res = true;
            token = null;
            char curchar = Text[pointer];
            if (!AllowedCharacters.Contains(curchar))
                return false;
            pointer++;
            State curstate = perehod[(int)State.S][curchar];
            StringBuilder curword = new();
            while (curstate != State.EndInt && curstate != State.EndOperation && curstate != State.EndSplitter && curstate != State.EndId && curstate != State.End && curstate != State.EndSplitter && curstate != State.Fail && curstate != State.EndEnd && res)
            {
                curword.Append(curchar);
                curchar = Text[pointer];
                if (!AllowedCharacters.Contains(curchar))
                {
                    if (curstate == State.Comment2 || curstate == State.Comment1)
                    {
                        pointer++;
                    }
                    else
                    {
                        res = false;
                    }
                }
                else
                {
                    pointer++;
                    curstate = perehod[(int)curstate][curchar];
                }
            }
            string word = curword.ToString();
            switch (curstate)
            {
                case State.EndInt:
                    var z = constantsTable.Find(word);
                    token = constantsTable.GetToken(z);
                    break;
                case State.EndId:
                    int q;
                    if (keywordsTable.Contains(word, out q))
                    {
                        token = keywordsTable.GetToken(word);
                    }
                    else
                    {
                        z = identificatorsTable.Find(word);
                        token = identificatorsTable.GetToken(z);
                    }
                    break;
                case State.EndSplitter:
                    token = splittersTable.GetToken(word);
                    break;
                case State.EndOperation:
                    token = operationsTable.GetToken(word);
                    break;
                case State.End:
                    break;
                case State.Fail:
                    res = false;
                    break;
                case State.EndEnd:
                    res = true;
                    token = (5, 0);
                    break;
                default:
                    break;
            }
            pointer--;
            return res;
        }
        public TableContainer(string path)
        {
            this.path = path;
            //инициализация таблиц
            keywordsTable = new ConstantTable("TXT/keywords.txt", 0);
            splittersTable = new ConstantTable("TXT/splitters.txt", 1);
            operationsTable = new ConstantTable("TXT/operations.txt", 2);
            constantsTable = new(3, "../../../TXT/ConstantsTable.txt");
            identificatorsTable = new(4, "../../../TXT/IdentificatorsTable.txt");
            //инициализация словаря приоритетов
            priorities.Add(operationsTable.GetToken("+"), 5);
            priorities.Add(operationsTable.GetToken("-"), 5);
            priorities.Add(operationsTable.GetToken("*"), 6);
            priorities.Add(operationsTable.GetToken("/"), 6);
            priorities.Add(operationsTable.GetToken("%"), 6);
            priorities.Add(operationsTable.GetToken("^"), 1);
            priorities.Add(operationsTable.GetToken(">"), 3);
            priorities.Add(operationsTable.GetToken("<"), 3);
            priorities.Add(operationsTable.GetToken("<<"), 4);
            priorities.Add(operationsTable.GetToken(">>"), 4);
            priorities.Add(operationsTable.GetToken("!="), 2);
            priorities.Add(operationsTable.GetToken("=="), 2);
            priorities.Add(operationsTable.GetToken("<="), 3);
            priorities.Add(operationsTable.GetToken(">="), 3);
            priorities.Add(operationsTable.GetToken("="), 0);
            priorities.Add(operationsTable.GetToken("+="), 0);
            priorities.Add(operationsTable.GetToken("*="), 0);
            priorities.Add(operationsTable.GetToken("/="), 0);
            priorities.Add(operationsTable.GetToken("-="), 0);
            priorities.Add(splittersTable.GetToken("("), -1);
            //инициализация синтаксического анализатора
            string[] Syntaxops = new string[]
            {
                "+",
                "-",
                "*",
                "/",
                "%",
                "^",
                ">",
                "<",
                "<<",
                ">>",
                "!=",
                "<=",
                ">=",
                "=="
            };
            SyntaxTable = new SyntaxTableElem[]
            {
                 new SyntaxTableElem(new List<(int, int)>(){ (4,-1)},1,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ (4,-1)},2,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("="),operationsTable.GetToken("+="),operationsTable.GetToken("-="),operationsTable.GetToken("*="),operationsTable.GetToken("/=")},4,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ splittersTable.GetToken(";")},7,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("="),operationsTable.GetToken("+="),operationsTable.GetToken("-="),operationsTable.GetToken("*="),operationsTable.GetToken("/=")},8,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ (4,-1),(3,-1),splittersTable.GetToken("(")},13,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ splittersTable.GetToken(";")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ splittersTable.GetToken(";")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("=")},16,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("+=")},17,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("-=")},18,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("*=")},19,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("/=")},20,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(3,-1)},21,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1)},23,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){ splittersTable.GetToken("(")},25,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("=")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("+=")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("-=")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("*=")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ operationsTable.GetToken("/=")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){(3,-1)},22,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){},29,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1)},24,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){},29,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken("(")},26,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1),(3,-1),splittersTable.GetToken("(")},13,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")")},28,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){},29,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){ },31,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")"),splittersTable.GetToken(";") },33,false,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ },34,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1),(3,-1),splittersTable.GetToken("(")},13,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")"),splittersTable.GetToken(";") },0,false,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("+") },48,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("-") },49,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("*") },50,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("/") },51,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("%") },52,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("^") },53,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">") },54,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<") },55,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<<") },56,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">>") },57,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("!=") },58,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<=") },59,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">=") },60,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("==") },61,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("+") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("-") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("*") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("/") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("%") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("^") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<<") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">>") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("!=") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<=") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">=") },0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("==") },0,true,false,true,true),




            };
            SyntaxTable[22].Tokens.Add(splittersTable.GetToken(";"));
            SyntaxTable[22].Tokens.Add(splittersTable.GetToken(")"));
            SyntaxTable[22].Tokens.Add(splittersTable.GetToken("("));
            SyntaxTable[24].Tokens.Add(splittersTable.GetToken(";"));
            SyntaxTable[24].Tokens.Add(splittersTable.GetToken("("));
            SyntaxTable[24].Tokens.Add(splittersTable.GetToken(")"));
            SyntaxTable[28].Tokens.Add(splittersTable.GetToken(";"));
            SyntaxTable[28].Tokens.Add(splittersTable.GetToken("("));
            SyntaxTable[28].Tokens.Add(splittersTable.GetToken(")"));
            foreach (var item in Syntaxops)
            {
                SyntaxTable[22].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[24].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[29].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[28].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[31].Tokens.Add(operationsTable.GetToken(item));
            }
            //инициализация лексического анализатора
            for (int i = 0; i < countofstates; i++)
            {
                perehod[i] = new Dictionary<char, State>();
            }
            //начальное состояние
            foreach (var item in Letters)
            {
                perehod[(int)State.S].Add(item, State.Identificator);
            }
            foreach (var item in Didgits)
            {
                perehod[(int)State.S].Add(item, State.Int);
            }
            perehod[(int)State.S].Add('%', State.Operation6);
            perehod[(int)State.S].Add('^', State.Operation6);
            perehod[(int)State.S].Add('!', State.Operation4);
            perehod[(int)State.S].Add('>', State.Operation1);
            perehod[(int)State.S].Add('<', State.Operation2);
            for (int i = 5; i < Operations.Length; i++)
            {
                perehod[(int)State.S].Add(Operations[i], State.Operation3);
            }
            foreach (var item in Splitters)
            {
                if (item == ' ' || item == '\t' || item == '\r' || item == '\n')
                    perehod[(int)State.S].Add(item, State.Empty);
                else
                    perehod[(int)State.S].Add(item, State.Splitter);
            }
            perehod[(int)State.S].Add('/', State.Slash);
            perehod[(int)State.S].Add('\0', State.EndEnd);
            //цифра
            foreach (var item in Didgits)
            {
                perehod[(int)State.Int].Add(item, State.Int);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Int].Add(item, State.Fail);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Int].Add(item, State.EndInt);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Int].Add(item, State.EndInt);
            }
            perehod[(int)State.Int].Add('/', State.EndInt);
            perehod[(int)State.Int].Add('\0', State.EndInt);
            //идентификатор
            foreach (var item in Didgits)
            {
                perehod[(int)State.Identificator].Add(item, State.Identificator);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Identificator].Add(item, State.Identificator);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Identificator].Add(item, State.EndId);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Identificator].Add(item, State.EndId);
            }
            perehod[(int)State.Identificator].Add('/', State.EndId);
            perehod[(int)State.Identificator].Add('\0', State.EndId);
            //слэш
            foreach (var item in Didgits)
            {
                perehod[(int)State.Slash].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Slash].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                if (item == '*')
                    perehod[(int)State.Slash].Add(item, State.Comment2);
                else
                {
                    if (item == '=')
                        perehod[(int)State.Slash].Add(item, State.Operation7);
                    else
                        perehod[(int)State.Slash].Add(item, State.EndOperation);

                }
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Slash].Add(item, State.EndOperation);
            }
            perehod[(int)State.Slash].Add('/', State.Comment1);
            perehod[(int)State.Slash].Add('\0', State.EndOperation);
            //splitter
            foreach (var item in Didgits)
            {
                perehod[(int)State.Splitter].Add(item, State.EndSplitter);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Splitter].Add(item, State.EndSplitter);
            }
            foreach (var item in Operations)
            {
                if (item == '*')
                    perehod[(int)State.Splitter].Add(item, State.EndSplitter);
                else
                    perehod[(int)State.Splitter].Add(item, State.EndSplitter);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Splitter].Add(item, State.EndSplitter);
            }
            perehod[(int)State.Splitter].Add('/', State.EndSplitter);
            perehod[(int)State.Splitter].Add('\0', State.EndSplitter);
            //Empty
            foreach (var item in Didgits)
            {
                perehod[(int)State.Empty].Add(item, State.End);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Empty].Add(item, State.End);
            }
            foreach (var item in Operations)
            {
                if (item == '*')
                    perehod[(int)State.Empty].Add(item, State.End);
                else
                    perehod[(int)State.Empty].Add(item, State.End);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Empty].Add(item, State.End);
            }
            perehod[(int)State.Empty].Add('/', State.End);
            perehod[(int)State.Empty].Add('\0', State.End);
            //Comment1
            foreach (var item in Didgits)
            {
                perehod[(int)State.Comment1].Add(item, State.Comment1);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Comment1].Add(item, State.Comment1);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Comment1].Add(item, State.Comment1);
            }
            foreach (var item in Splitters)
            {
                if (item == '\r')
                    perehod[(int)State.Comment1].Add(item, State.End);
                else
                    perehod[(int)State.Comment1].Add(item, State.Comment1);
            }
            perehod[(int)State.Comment1].Add('/', State.Comment1);
            perehod[(int)State.Comment1].Add('\0', State.End);
            //Comment2
            foreach (var item in Didgits)
            {
                perehod[(int)State.Comment2].Add(item, State.Comment2);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Comment2].Add(item, State.Comment2);
            }
            foreach (var item in Operations)
            {
                if (item == '*')
                    perehod[(int)State.Comment2].Add(item, State.Commentstar);
                else
                    perehod[(int)State.Comment2].Add(item, State.Comment2);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Comment2].Add(item, State.Comment2);
            }
            perehod[(int)State.Comment2].Add('/', State.Comment2);
            perehod[(int)State.Comment2].Add('\0', State.Fail);
            //Commentstar
            foreach (var item in Didgits)
            {
                perehod[(int)State.Commentstar].Add(item, State.Comment2);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Commentstar].Add(item, State.Comment2);
            }
            foreach (var item in Operations)
            {
                if (item == '*')
                {
                    perehod[(int)State.Commentstar].Add(item, State.Commentstar);
                }
                else
                {
                    perehod[(int)State.Commentstar].Add(item, State.Comment2);
                }
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Commentstar].Add(item, State.Comment2);
            }
            perehod[(int)State.Commentstar].Add('/', State.EndComment2);
            perehod[(int)State.Commentstar].Add('\0', State.Fail);
            //EndComment2
            foreach (var item in Didgits)
            {
                perehod[(int)State.EndComment2].Add(item, State.End);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.EndComment2].Add(item, State.End);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.EndComment2].Add(item, State.End);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.EndComment2].Add(item, State.End);
            }
            perehod[(int)State.EndComment2].Add('/', State.End);
            perehod[(int)State.EndComment2].Add('\0', State.End);
            //operation1
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation1].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation1].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                if (item == '=' || item == '>')
                {
                    perehod[(int)State.Operation1].Add(item, State.Operation5);
                }
                else
                    perehod[(int)State.Operation1].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation1].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation1].Add('/', State.Fail);
            perehod[(int)State.Operation1].Add('\0', State.EndOperation);
            //operation2
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation2].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation2].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                if (item == '=' || item == '<')
                {
                    perehod[(int)State.Operation2].Add(item, State.Operation5);
                }
                else
                    perehod[(int)State.Operation2].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation2].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation2].Add('/', State.Fail);
            perehod[(int)State.Operation2].Add('\0', State.EndOperation);
            //operation3
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation3].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation3].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                if (item == '=')
                {
                    perehod[(int)State.Operation3].Add(item, State.Operation5);
                }
                else
                    perehod[(int)State.Operation3].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation3].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation3].Add('/', State.Fail);
            perehod[(int)State.Operation3].Add('\0', State.EndOperation);
            //operation4
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation4].Add(item, State.Fail);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation4].Add(item, State.Fail);
            }
            foreach (var item in Operations)
            {
                if (item == '=')
                {
                    perehod[(int)State.Operation4].Add(item, State.Operation5);
                }
                else
                    perehod[(int)State.Operation4].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation4].Add(item, State.Fail);
            }
            perehod[(int)State.Operation4].Add('/', State.Fail);
            perehod[(int)State.Operation4].Add('\0', State.Fail);
            //operation5
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation5].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation5].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Operation5].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation5].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation5].Add('/', State.Fail);
            perehod[(int)State.Operation5].Add('\0', State.Fail);
            //operation6
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation6].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation6].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Operation6].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation6].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation6].Add('/', State.EndOperation);
            perehod[(int)State.Operation6].Add('\0', State.EndOperation);
            //operation7
            foreach (var item in Didgits)
            {
                perehod[(int)State.Operation7].Add(item, State.EndOperation);
            }
            foreach (var item in Letters)
            {
                perehod[(int)State.Operation7].Add(item, State.EndOperation);
            }
            foreach (var item in Operations)
            {
                perehod[(int)State.Operation7].Add(item, State.Fail);
            }
            foreach (var item in Splitters)
            {
                perehod[(int)State.Operation7].Add(item, State.EndOperation);
            }
            perehod[(int)State.Operation7].Add('/', State.EndOperation);
            perehod[(int)State.Operation7].Add('\0', State.EndOperation);
        }
        public void WriteTables()
        {
            constantsTable.WriteToFile();
            identificatorsTable.WriteToFile();

        }
        public void WriteInstructions()
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                for (int j = 0; j < instructions[i].Count; j++)
                {
                    Console.Write($"{instructions[i][j]} ");
                }
                Console.WriteLine();
            }
        }
        public void WriteTokens()
        {
            StringBuilder text = new();
            foreach (var item in Tokens)
            {
                text.Append($"{item.Item1} {item.Item2}\n");
            }
            File.WriteAllText("../../../TXT/Tokens.txt", text.ToString());
        }
        public void WriteAssembler()
        {
            int markcounter = 0;
            string path = "../../../TXT/Code.asm";
            using (FileStream fs = File.OpenWrite(path))
            {
                AddText(fs, ".686\n.model flat\n");
                AddText(fs, ".data\n");
                foreach (var item in identificatorsTable.GetIdList())
                {
                    AddText(fs, $"_{identificatorsTable.Find(item).Name} dd ?\n");
                }
                AddText(fs, ".code\n");

                AddText(fs, "main proc\n");
                foreach (var item in instructions)
                {
                    var variable = item[0];
                    int n = item.Count - 1;
                    for (int i = 1; i < n; i++)
                    {
                        switch (item[i].Item1)
                        {
                            case 3:
                                AddText(fs, $"push {constantsTable.Find(item[i].Item2).Name}\n");
                                break;
                            case 4:
                                AddText(fs, $"push _{identificatorsTable.Find(item[i].Item2).Name}\n");
                                break;
                            case 2:
                                AddText(fs, $"pop ebx\n");
                                AddText(fs, $"pop eax\n");
                                switch (item[i].Item2)
                                {
                                    case 0://+
                                        AddText(fs, $"add eax,ebx\n");
                                        AddText(fs, $"push eax\n");
                                        break;
                                    case 1://-
                                        AddText(fs, $"sub eax,ebx\n");
                                        AddText(fs, $"push eax\n");
                                        break;
                                    case 2://*
                                        AddText(fs, $"mul ebx\n");
                                        AddText(fs, $"push eax\n");
                                        break;
                                    case 3:// /
                                        AddText(fs, $"xor edx, edx\n");
                                        AddText(fs, $"div ebx\n");
                                        AddText(fs, $"push eax\n");
                                        break;
                                    case 4://%
                                        AddText(fs, $"div ebx\n");
                                        AddText(fs, $"push edx\n");
                                        break;
                                    case 5://^
                                        AddText(fs, $"xor eax,ebx\n");
                                        AddText(fs, $"push eax\n");
                                        break;
                                    case 6://>
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"jg mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    case 7://<
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"jl mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    case 9://<<
                                        AddText(fs, $"mov ecx, ebx\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"mov ebx, 2\n");
                                        AddText(fs, $"mul ebx\n");
                                        AddText(fs, $"loop mark{markcounter}\n");
                                        AddText(fs, $"push eax\n");
                                        markcounter++;
                                        break;
                                    case 10://>>
                                        AddText(fs, $"mov ecx, ebx\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"mov ebx, 2\n");
                                        AddText(fs, $"xor edx,edx\n");
                                        AddText(fs, $"div ebx\n");
                                        AddText(fs, $"loop mark{markcounter}\n");
                                        AddText(fs, $"push eax\n");
                                        markcounter++;
                                        break;
                                    case 11://==
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"je mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    case 12://!=
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"jne mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    case 13://>=
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"jge mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    case 14://<=
                                        AddText(fs, $"cmp eax,ebx\n");
                                        AddText(fs, $"jle mark{markcounter}\n");
                                        AddText(fs, $"jmp mark{markcounter + 1}\n");
                                        AddText(fs, $"mark{markcounter}:\n");
                                        AddText(fs, $"push 1\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 1}:\n");
                                        AddText(fs, $"push 0\n");
                                        AddText(fs, $"jmp mark{markcounter + 2}\n");
                                        AddText(fs, $"mark{markcounter + 2}:\n");
                                        markcounter += 3;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    switch (item[n].Item2)
                    {
                        case 8://=
                            AddText(fs, $"pop _{identificatorsTable.Find(variable.Item2).Name}\n");
                            break;
                        case 15://+=
                            AddText(fs, $"pop ebx\n");
                            AddText(fs, $"mov eax, _{identificatorsTable.Find(variable.Item2).Name}\n");
                            AddText(fs, $"add eax,ebx\n");
                            AddText(fs, $"mov _{identificatorsTable.Find(variable.Item2).Name}, eax\n");
                            break;
                        case 16://-=
                            AddText(fs, $"pop ebx\n");
                            AddText(fs, $"mov eax, _{identificatorsTable.Find(variable.Item2).Name}\n");
                            AddText(fs, $"sub eax,ebx\n");
                            AddText(fs, $"mov _{identificatorsTable.Find(variable.Item2).Name}, eax\n");
                            break;
                        case 17://*=
                            AddText(fs, $"pop ebx\n");
                            AddText(fs, $"mov eax, _{identificatorsTable.Find(variable.Item2).Name}\n");
                            AddText(fs, $"mul ebx\n");
                            AddText(fs, $"mov _{identificatorsTable.Find(variable.Item2).Name}, eax\n");
                            break;
                        case 18:///=
                            AddText(fs, $"pop ebx\n");
                            AddText(fs, $"mov eax, _{identificatorsTable.Find(variable.Item2).Name}\n");
                            AddText(fs, $"div ebx\n");
                            AddText(fs, $"mov _{identificatorsTable.Find(variable.Item2).Name}, eax\n");
                            break;
                        default:
                            break;
                    }
                }
                AddText(fs, "");
                AddText(fs, "");
                AddText(fs, "main endp\n");
                AddText(fs, "end main\n");
            }
        }
        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
    }
    public class CheckLL1Grammar
    {
        List<Terminal> Terminals;
        List<NonTerminal> NonTerminals;
        List<Rule> Rules;
        List<List<Terminal>> GuideCharacters = new();
        void NullStringArray()
        {

            foreach (var item in Rules)
            {
                if (item.RightSide.OfType<Terminal>().Count() > 0)
                {
                    item.LeftSide.cannull = NonTerminal.canbenull.no;
                }
            }
            foreach (var item in Rules)
            {
                if (item.iseps)
                {
                    item.LeftSide.cannull = NonTerminal.canbenull.yes;
                }
            }
            while (NonTerminals.Where((T) => T.cannull == NonTerminal.canbenull.undecided).Count() > 0)
            {
                foreach (var rule in Rules.Where((T) => !T.excluded))
                {
                    if (rule.RightSide.OfType<Terminal>().Count() > 0 || rule.RightSide.OfType<NonTerminal>().Where((T) => T.cannull == NonTerminal.canbenull.no).Count() > 0 || rule.iseps)
                    {
                        rule.excluded = true;
                        if (rule.LeftSide.cannull == NonTerminal.canbenull.undecided && Rules.Where((T) => !T.excluded && T.LeftSide == rule.LeftSide).Count() == 0)
                        {
                            rule.LeftSide.cannull = NonTerminal.canbenull.no;
                        }
                    }
                    int i1 = rule.RightSide.Count;
                    for (int i = 0; i < i1; i++)
                    {
                        if (rule.RightSide[i] is NonTerminal)
                            if ((rule.RightSide[i] as NonTerminal).cannull == NonTerminal.canbenull.yes)
                                rule.skipped[i] = true;
                    }

                    bool flag = true;
                    foreach (var item in rule.skipped)
                    {
                        if (!item)
                            flag = false;
                    }
                    if (flag)
                    {
                        rule.LeftSide.cannull = NonTerminal.canbenull.yes;
                        foreach (var item in Rules)
                        {
                            if (item.LeftSide == rule.LeftSide)
                                item.excluded = true;
                        }
                    }

                }

            }
        }
        int[][] PrecedeMatrix;
        void GeneratePrecedeMatrix()
        {
            int n = NonTerminals.Count;
            int m = n + Terminals.Count;
            PrecedeMatrix = new int[n][];
            for (int i = 0; i < n; i++)
            {
                PrecedeMatrix[i] = new int[m];
            }
            foreach (var rule in Rules.Where((T) => !T.iseps))
            {
                bool flag = true;
                for (int i = 0; i < rule.RightSide.Count && flag; i++)
                {
                    if (rule.RightSide[i] is NonTerminal)
                    {
                        PrecedeMatrix[NonTerminals.IndexOf(rule.LeftSide)][NonTerminals.IndexOf(rule.RightSide[i] as NonTerminal)] = 1;
                        if ((rule.RightSide[i] as NonTerminal).cannull == NonTerminal.canbenull.no)
                        {
                            flag = false;
                        }
                    }
                    else
                    {
                        PrecedeMatrix[NonTerminals.IndexOf(rule.LeftSide)][n + Terminals.IndexOf(rule.RightSide[i] as Terminal)] = 1;
                        flag = false;
                    }
                }
            }
            bool flag1 = true;
            while (flag1)
            {
                flag1 = false;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        for (int v = 0; v < m; v++)
                        {
                            if (PrecedeMatrix[i][j] == 1 && PrecedeMatrix[j][v] == 1)
                            {
                                if (PrecedeMatrix[i][v] == 0)
                                    flag1 = true;
                                PrecedeMatrix[i][v] = 1;
                            }
                        }
                    }
                }
            }
            Console.WriteLine("putin");
        }
        int[][] SuccessionMatrix;
        void GenerateSuccessionMatrix()
        {
            int n = NonTerminals.Count;
            int m = n + Terminals.Count;
            SuccessionMatrix = new int[n][];
            for (int i = 0; i < n; i++)
            {
                SuccessionMatrix[i] = new int[m];
            }
            foreach (var rule in Rules.Where((T) => !T.iseps))
            {
                for (int i = 0; i < rule.RightSide.Count - 1; i++)
                {
                    if (rule.RightSide[i] is NonTerminal)
                    {
                        if (rule.RightSide[i + 1] is NonTerminal)
                        {
                            SuccessionMatrix[NonTerminals.IndexOf(rule.RightSide[i] as NonTerminal)][NonTerminals.IndexOf(rule.RightSide[i + 1] as NonTerminal)] = 1;
                        }
                        else
                        {
                            SuccessionMatrix[NonTerminals.IndexOf(rule.RightSide[i] as NonTerminal)][n + Terminals.IndexOf(rule.RightSide[i + 1] as Terminal)] = 1;
                        }
                    }
                }
            }
            bool flag = true;
            while (flag)
            {
                flag = false;
                foreach (var rule in Rules.Where((T) => !T.iseps))
                {
                    if (rule.RightSide[rule.RightSide.Count - 1] is NonTerminal)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            if (SuccessionMatrix[NonTerminals.IndexOf(rule.LeftSide)][j] == 1)
                            {
                                if (SuccessionMatrix[NonTerminals.IndexOf(rule.RightSide[rule.RightSide.Count - 1] as NonTerminal)][j] == 0)
                                    flag = true;
                                SuccessionMatrix[NonTerminals.IndexOf(rule.RightSide[rule.RightSide.Count - 1] as NonTerminal)][j] = 1;
                            }
                        }
                    }
                }
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (SuccessionMatrix[i][j] == 1)
                        {
                            for (int k = 0; k < m; k++)
                            {
                                if (PrecedeMatrix[j][k] == 1)
                                {
                                    if (SuccessionMatrix[i][k] == 0)
                                        flag = true;
                                    SuccessionMatrix[i][k] = 1;
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("putin");
        }
        List<Terminal> GetGuideCharacters(Rule rule)
        {
            int n = NonTerminals.Count;
            int m = n + Terminals.Count;
            List<Terminal> res = new List<Terminal>();
            if (rule.iseps)
            {
                for (int j = n; j < m; j++)
                {
                    if (SuccessionMatrix[NonTerminals.IndexOf(rule.LeftSide)][j] == 1)
                    {
                        res.Add(Terminals[j - n]);
                    }
                }
            }
            else
            {
                bool flag = true;
                for (int i = 0; i < rule.RightSide.Count && flag; i++)
                {
                    if (rule.RightSide[i] is Terminal)
                    {
                        res.Add(rule.RightSide[i] as Terminal);
                        flag = false;
                    }
                    if (rule.RightSide[i] is NonTerminal)
                    {
                        if(i==rule.RightSide.Count-1&&(rule.RightSide[i] as NonTerminal).cannull==NonTerminal.canbenull.yes)
                        {
                            for (int j = n; j < m; j++)
                            {
                                if (SuccessionMatrix[NonTerminals.IndexOf(rule.RightSide[i] as NonTerminal)][j] == 1)
                                {
                                    res.Add(Terminals[j - n]);
                                }
                            }
                        }
                        for (int j = n; j < m; j++)
                        {
                            if (PrecedeMatrix[NonTerminals.IndexOf(rule.RightSide[i] as NonTerminal)][j] == 1)
                            {
                                res.Add(Terminals[j - n]);
                            }
                        }
                        if ((rule.RightSide[i] as NonTerminal).cannull == NonTerminal.canbenull.no)
                        {
                            flag = false;

                        }
                    }
                }
            }
            return res;
        }
        void GenerateGuideCharacters()
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                GuideCharacters.Add(GetGuideCharacters(Rules[i]));
            }
        }
        bool CheckAlternativeRules()
        {
            bool res = true;
            foreach (var nonterm in NonTerminals)
            {
                List<Rule> currules = Rules.Where((T) => T.LeftSide == nonterm).ToList();
                for (int i = 0; i < currules.Count() && res; i++)
                {
                    for (int j = i + 1; j < currules.Count() && res; j++)
                    {
                        if (GuideCharacters[Rules.IndexOf(currules[i])].Intersect(GuideCharacters[Rules.IndexOf(currules[j])]).Count() > 0)
                            res = false;
                    }
                }
            }
            return res;
        }
        public bool Check()
        {
            NullStringArray();
            GeneratePrecedeMatrix();
            GenerateSuccessionMatrix();
            GenerateGuideCharacters();
            return CheckAlternativeRules();
        }
        public CheckLL1Grammar(List<Terminal> terminals, List<NonTerminal> nonTerminals, List<Rule> rules)
        {
            Terminals = terminals;
            NonTerminals = nonTerminals;
            Rules = rules;
        }
    }
    public class Rule
    {
        public bool excluded = false;
        public List<bool> skipped = new();
        public NonTerminal LeftSide;
        public List<Term> RightSide;

        public Rule(NonTerminal leftSide, List<Term> rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
            foreach (var item in rightSide)
            {
                skipped.Add(false);
            }
        }

        public bool iseps
        {
            get
            {
                bool res = false;
                if (RightSide.Count == 1)
                {
                    if (RightSide[0].str == "eps") ;
                    res = true;
                }
                return res;
            }
            init { }

        }
    }
    public abstract class Term
    {
        public Term(string str)
        {
            this.str = str;
        }
        public override int GetHashCode()
        {
            return str.GetHashCode();
        }
        public string str { get; init; }
    }
    public class Terminal : Term
    {

        public Terminal(string str) : base(str)
        {
        }
    }
    public class NonTerminal : Term
    {
        public enum canbenull
        {
            yes, no, undecided
        }
        public canbenull cannull = canbenull.undecided;
        public NonTerminal(string str) : base(str)
        {

        }
    }
    public class Eps : Term
    {
        public Eps() : base("eps")
        {
        }
    }
    public class Lexeme
    {
        public string Name { get; set; } = String.Empty;
        public enum TYPE
        {
            None,
            Int
        };
        public TYPE Type { get; set; } = TYPE.None;
        public bool IsInit { get; set; } = false;
        public int Value { get; set; } = 0;

        public override string ToString()
        {
            return $"{Name} {Type} {Value}";
        }
    }
    public abstract class Table<T>
    {
        protected Table(int tableId)
        {
            TableId = tableId;
        }

        public int TableId { get; init; }
        public abstract (int, int) GetToken(T item);
    }

    public class ConstantTable : Table<string>
    {
        private Dictionary<string, int> stringToInt = new();
        private Dictionary<int, string> intToString = new();
        public ConstantTable(string filepath, int tableId) : base(tableId)
        {
            int counter = 0;
            var lines = File.ReadAllLines(filepath);
            foreach (var line in lines)
            {
                stringToInt.Add(line, counter);
                intToString.Add(counter, line);

                counter++;
            }
        }
        public bool Contains(string name, out int position)
        {
            if (stringToInt.ContainsKey(name))
            {
                position = stringToInt[name];
                return true;
            }
            position = 0;
            return false;
        }

        public bool Contains(int position, out string name)
        {
            if (intToString.ContainsKey(position))
            {
                name = intToString[position];
                return true;
            }
            name = String.Empty;
            return false;
        }

        public override (int, int) GetToken(string item)
        {
            Contains(item, out var x);
            return (TableId, x);
        }
    }

    public class VariableTable : Table<Lexeme>
    {
        private Dictionary<int, string> intToString = new();
        private Dictionary<string, int> stringToInt = new();
        private Dictionary<int, Lexeme> intToLexeme = new();
        private int counter = 0;
        string path;
        public VariableTable(int tableId, string path) : base(tableId)
        {
            this.path = path;
        }
        public List<int> GetIdList()
        {
            List<int> res = new();
            foreach (var item in intToString)
            {
                res.Add(item.Key);
            }
            return res;
        }
        public Lexeme Find(string name)
        {
            if (stringToInt.ContainsKey(name))
            {
                return intToLexeme[stringToInt[name]];
            }

            var lexeme = new Lexeme { Name = name };
            intToLexeme.Add(counter, lexeme);
            intToString.Add(counter, name);
            stringToInt.Add(name, counter);
            counter++;
            return lexeme;
        }
        public Lexeme Find(int id)
        {
            if (intToLexeme.ContainsKey(id))
            {
                return intToLexeme[id];
            }
            return null;
        }
        public int Find(Lexeme lexeme)
        {
            if (stringToInt.ContainsKey(lexeme.Name))
            {
                return stringToInt[lexeme.Name];
            }

            intToLexeme.Add(counter, lexeme);
            intToString.Add(counter, lexeme.Name);
            stringToInt.Add(lexeme.Name, counter);
            counter++;
            return counter - 1;
        }



        public override (int, int) GetToken(Lexeme item)
        {
            if (stringToInt.TryGetValue(item.Name, out var x))
                return (TableId, x);
            return default;
        }

        public override string ToString()
        {
            var s = string.Empty;
            foreach (var e in intToLexeme)
            {
                s += ($"{e.Key}:{e.Value}\n");
            }
            return s;
        }
        public void WriteToFile()
        {
            StringBuilder text = new StringBuilder();
            foreach (var item in intToString)
            {
                text.Append($"{item.Key} {item.Value}\n");
            }
            File.WriteAllText(path, text.ToString());
        }

    }
    public class SyntaxTableElem
    {
        public List<(int, int)> Tokens;

        public SyntaxTableElem(List<(int, int)> tokens, int jump, bool accept, bool stack, bool ret, bool erorr)
        {
            Tokens = tokens;
            this.jump = jump;
            this.accept = accept;
            this.stack = stack;
            this.ret = ret;
            this.erorr = erorr;
        }

        public int jump { get; set; }
        public bool accept { get; set; }
        public bool stack { get; set; }
        public bool ret { get; set; }
        public bool erorr { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //TableContainer x = new("TXT/Program.txt");
            //var res = x.LexAnaliz();
            //var res1 = x.SyntaxAnaliz();
            //x.WriteTables();
            //x.WriteTokens();
            //x.WriteInstructions();
            //x.WriteAssembler();
            List<Terminal> lterm = new() { new Terminal("begin"), new Terminal("d"), new Terminal("s"), new Terminal("comma"), new Terminal("semi"), new Terminal("end") };
            List<NonTerminal> lnterm = new() { new NonTerminal("PROGRAMM"), new NonTerminal("DECLIST"), new NonTerminal("STATELIST"), new NonTerminal("X"), new NonTerminal("Y") };
            List<Rule> rules = new();
            Eps eps = new Eps();
            rules.Add(new Rule(lnterm[0], new List<Term>() { lterm[0], lnterm[1], lterm[3], lnterm[2], lterm[5] }));
            rules.Add(new Rule(lnterm[1], new List<Term>() { lterm[1], lnterm[3] }));
            rules.Add(new Rule(lnterm[3], new List<Term>() { lterm[4], lnterm[1] }));
            rules.Add(new Rule(lnterm[3], new List<Term>() { eps }));
            rules.Add(new Rule(lnterm[2], new List<Term>() { lterm[2], lnterm[4] }));
            rules.Add(new Rule(lnterm[4], new List<Term>() { lterm[4], lnterm[2] }));
            rules.Add(new Rule(lnterm[4], new List<Term>() { eps }));


            CheckLL1Grammar Gram = new(lterm, lnterm, rules);
            var res = Gram.Check();
            Console.WriteLine("Hello world");
        }
    }
}