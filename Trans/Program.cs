using System;
using System.Collections.Generic;
using System.IO;

namespace Trans
{
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
        public VariableTable(int tableId) : base(tableId)
        {

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
    }


    class Program
    {
        static void Main(string[] args)
        {
            ConstantTable keywordsTable = new ConstantTable("TXT/keywords.txt", 0);
            ConstantTable splittersTable = new ConstantTable("TXT/splitters.txt", 1);
            ConstantTable operationsTable = new ConstantTable("TXT/operations.txt", 2);
            VariableTable constantsTable = new(3);
            VariableTable identificatorsTable = new(4);
            Console.WriteLine($"{operationsTable.GetToken("+")}");
        }
    }
}