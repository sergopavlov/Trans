using System;
using System.Collections.Generic;
using System.IO;
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
                int instrnum=0;
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
                        if(identificatorsTable.Find(Tokens[num].Item2).Type==Lexeme.TYPE.None)
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
                        if (isleftvariableused && identificatorsTable.Find(Tokens[num].Item2).IsInit == false)
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
                            identificatorsTable.Find(Tokens[num].Item2).IsInit = true;
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
                                    }
                                }
                            }
                            if (Tokens[i] == splittersTable.GetToken("("))
                                st.Push(Tokens[i]);
                            if (Tokens[i] == splittersTable.GetToken(")"))
                            {
                                while(st.Peek()!=splittersTable.GetToken("("))
                                {
                                    q.Enqueue(st.Pop());
                                }
                                st.Pop();
                            }
                        }
                        while (st.Count > 0)
                            q.Enqueue(st.Pop());
                        instructions.Add(new());
                        while(q.Count>0)
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
            Empty,//пробел ентер
            EndInt,
            EndId,
            EndSplitter,
            EndOperation,
            End,
            EndEnd,
            Fail
        }
        static int countofstates = 17;
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
                ">="
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
                 new SyntaxTableElem(new List<(int, int)>(){},28,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1)},24,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){},28,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken("(")},26,true,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1),(3,-1),splittersTable.GetToken("(")},13,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")")},0,true,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ },30,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")"),splittersTable.GetToken(";") },32,false,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){ },33,false,true,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){(4,-1),(3,-1),splittersTable.GetToken("(")},13,false,false,false,true),
                 new SyntaxTableElem(new List<(int, int)>(){splittersTable.GetToken(")"),splittersTable.GetToken(";") },0,false,false,true,true),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("+") },46,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("-") },47,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("*") },48,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("/") },49,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("%") },50,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("^") },51,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">") },52,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<") },53,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<<") },54,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">>") },55,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("!=") },56,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken("<=") },57,false,false,false,false),
                 new SyntaxTableElem(new List<(int, int)>(){operationsTable.GetToken(">=") },58,false,false,false,true),
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




            };
            SyntaxTable[22].Tokens.Add(splittersTable.GetToken(";"));
            SyntaxTable[24].Tokens.Add(splittersTable.GetToken(";"));
            foreach (var item in Syntaxops)
            {
                SyntaxTable[22].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[24].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[28].Tokens.Add(operationsTable.GetToken(item));
                SyntaxTable[30].Tokens.Add(operationsTable.GetToken(item));
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
            perehod[(int)State.S].Add('%', State.EndOperation);
            perehod[(int)State.S].Add('^', State.EndOperation);
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
                    perehod[(int)State.Slash].Add(item, State.EndOperation);
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
            TableContainer x = new("TXT/Program.txt");
            var res = x.LexAnaliz();
            var res1 = x.SyntaxAnaliz();
            x.WriteTables();
            x.WriteTokens();
            x.WriteInstructions();
            Console.WriteLine("Hello world");
        }
    }
}