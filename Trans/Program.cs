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
        public ConstantTable keywordsTable = new ConstantTable("TXT/keywords.txt", 0);
        public ConstantTable splittersTable = new ConstantTable("TXT/splitters.txt", 1);
        public ConstantTable operationsTable = new ConstantTable("TXT/operations.txt", 2);
        public VariableTable constantsTable = new(3, "../../../TXT/ConstantsTable.txt");
        public VariableTable identificatorsTable = new(4,"../../../TXT/IdentificatorsTable.txt");
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
                if (!GenerateToken(text,ref i,out curtoken))
                {
                    res = false;
                }
                else
                {
                    if (curtoken!=null)
                    {
                        if (curtoken.Value.Item1==5)
                        {
                            break;
                        }
                        Tokens.Add(((int,int))curtoken);
                    }
                }
            } while (res);
            return res;
        }
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
            while (curstate != State.EndInt && curstate != State.EndOperation && curstate != State.EndSplitter && curstate != State.EndId && curstate != State.End && curstate != State.EndSplitter && curstate != State.Fail && curstate != State.EndEnd&&res)
            {
                curword.Append(curchar);
                curchar = Text[pointer];
                if (!AllowedCharacters.Contains(curchar))
                {
                    res = false;
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
                perehod[(int)State.Commentstar].Add(item, State.Comment2);
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


    public class FiniteAutomat
    {

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
        public VariableTable(int tableId,string path) : base(tableId)
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


    class Program
    {
        static void Main(string[] args)
        {
            TableContainer x = new("TXT/Program.txt");
            x.LexAnaliz();
            x.WriteTables();
            x.WriteTokens();
            Console.WriteLine("Hello world");
        }
    }
}