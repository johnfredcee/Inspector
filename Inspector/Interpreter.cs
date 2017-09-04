using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspector
{
 
    public class Interpreter
    {
        public class Symbol
        {
            public string name;
        }

        public class Cell
        {
            public dynamic first;
            public dynamic second;
        }

        public static class Entity
        {
            public static bool IsCell(dynamic value)
            {
                return (value is Cell);
            }
            public static dynamic makeCell()
            {
                return new Cell() { first = nil, second = nil };
            }
            public static dynamic makeCell(dynamic car, dynamic cdr)
            {
                return new Cell() { first = car, second = cdr };
            }

            public static dynamic car(dynamic value)
            {
                return (value as Cell).first;
            }

            public static dynamic cdr(dynamic value)
            {
                return (value as Cell).second;
            }
     
            public static dynamic cons(dynamic car, dynamic cdr)
            {
                dynamic place = Entity.makeCell(car, cdr);
                return place;
            }

            public static void append(dynamic list, dynamic appendage)
            {
                dynamic ptr = list;
                for(ptr = list; !Entity.IsNill((ptr as Cell).second); ptr = (ptr as Cell).second)
                {
                    (ptr as Cell).second = Entity.cons(appendage, nil);
                }
            }
            static public bool IsNumber(dynamic value)
            {
                return ((value is Int64) ||
                        (value is double));
            }
            public static bool IsFixnum(dynamic value)
            {
                return (value is Int64);
            }
            public static bool IsFloatnum(dynamic value)
            {
                return (value is double);
            }
            public static bool IsPrimitive(dynamic value)
            {
                return (value is Primitive);
            }
            public static bool IsSymbol(dynamic value)
            {
                return (value is Symbol);
            }
            public static bool IsBoolean(dynamic value)
            {
                return (value is bool);
            }
            public static bool IsNill(dynamic value)
            {
                return ((value == null) || ((IsBoolean(value)) && (!value)));
            }
        }

        public delegate dynamic Primitive(dynamic x, Environment env);

        public static dynamic nil = false;

        public static dynamic t = true;

        public class Environment
        {
            private Environment parent = null;

            public Environment makeChildEnvironment()
            {
                return new Environment() { parent = this };
            }

            public class SymbolComparer : IEqualityComparer<Symbol>
            {
                public bool Equals(Symbol x, Symbol y)
                {
                    return (String.Compare(x.name, y.name, StringComparison.CurrentCultureIgnoreCase) == 0);
                }

                public int GetHashCode(Symbol obj)
                {
                    return obj.name.GetHashCode();
                }
            }

            public Dictionary<Symbol, dynamic> frame = new Dictionary<Symbol, dynamic>(new SymbolComparer());

            public void Intern(string symbolName, dynamic v)
            {
                frame.Add(new Symbol() { name = symbolName }, v);
            }

            public dynamic Resolve(dynamic x)
            {
                dynamic result = null;
                if (x.IsSymbol())
                {
                    Symbol s = (x as Symbol);
                    frame.TryGetValue(s, out result);
                    if ((result == null) && (parent != null))
                    {
                        return parent.Resolve(x);
                    }
                }
                return result ?? nil;
            }
        };

        public static Environment root;

        private static char[] whitespace = new char[] { ' ', '\t', '\n', '\r' };

        private static Environment makeRootEnv()
        {
            Environment result = new Environment();
            Primitive car = (e, env) =>
            {
                return ((e as Cell).first as Cell).first;
            };
            result.Intern("car", car);
            Primitive cdr = (e, env) =>
            {
                return ((e.value as Cell).first as Cell).second;
            };
            result.Intern("cdr", cdr);
            Primitive cons = (e, env) =>
            {
                dynamic v = Entity.makeCell((e as Cell).first, nil);
                dynamic args = ((e.value as Cell).second as Cell).first;
                return v;
            };
            Primitive quote = (e, env) =>
            {
                return e.first;
            };
            result.Intern("quote", quote);
            Primitive isList = (e, env) =>
            {
                dynamic x = Entity.IsCell(e.first);
                return x;
            };
            result.Intern("list?", isList);
            Primitive isNumber = (e, env) => 
            {
                dynamic r = Entity.IsNumber(e.first);
                return r;
            };
            result.Intern("number?", isNumber);
            Primitive isSymbol = (e, env) => 
            {
                dynamic r = Entity.IsSymbol(e.first);
                return r;
            };
            result.Intern("symbol?", isSymbol);
            Primitive isBoolean = (e, env) => 
            {
                dynamic r = Entity.IsBoolean(e);
                return r; 
            };
            result.Intern("boolean?", isBoolean);
            Primitive writeEntity = (e, env) => { 
                Print(e); 
                return t; 
            };
            result.Intern("write", writeEntity);
            Primitive set = (e, env) =>
            {
                dynamic pr = Interpreter.Eval(e, env);

                if (Entity.IsSymbol(pr.first))
                {
                    Cell param = pr as Cell;
                    if (param == null)
                        return nil;
                    Symbol sym = param.first as Symbol;
                    if (sym == null)
                        return nil;
                    dynamic cadr = (param.second as Cell).first;
                    dynamic val = Eval(cadr, env);
                    env.Intern(sym.name, val);
                }
                return t;
            };
            result.Intern("set!", set);
            return result;
        }

        static Interpreter()
        {
            root = makeRootEnv();
        }

        private static LinkedList<string> tokenize(string line)
        {
            var line0 = line.Replace("(", " ( ");
            var line1 = line0.Replace(")", " ) ");
            return new LinkedList<String>(line1.Split(whitespace, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string popToken(ref LinkedList<string> tokens)
        {
            var tok = tokens.First();
            tokens.RemoveFirst();
            return tok;
        }

        private static dynamic readtokens(ref LinkedList<string> tokens)
        {
            dynamic result = Entity.makeCell();
            string token = popToken(ref tokens);
            if (token == "(")
            {
                if (tokens.First() != ")")
                {
                    result = Entity.makeCell();
                    result.first = readtokens(ref tokens);
                    if (tokens.First() != ")")
                    {
                        var currentCell = Entity.makeCell();
                        result.second = currentCell;
                        while (true)
                        {
                            currentCell.first = readtokens(ref tokens);
                            if (tokens.First() == ")")
                                break;
                            var previousCell = currentCell;
                            currentCell = Entity.makeCell();
                            previousCell.second = currentCell;
                        };
                    }
                }
            }
            else if (token == ")")
            {
                InvalidOperationException oe = new InvalidOperationException();
                throw oe;
            }
            else
            {
                result = atom(token);
            }
            return result;
        }

        private static bool tryconvert(Func<string, dynamic> converter, string token, ref dynamic o)
        {
            try
            {
                o = converter(token);
            }
            catch (FormatException)
            {
                o = null;
            }
            return o != null;
        }

        private static dynamic atom(string token)
        {
            dynamic result = null;
            if (tryconvert((t) => { return Convert.ToInt64(t); }, token, ref result))
                return result;
            if (tryconvert((t) => { return double.Parse(token); }, token, ref result))
                return result;
            result.value = new Symbol() { name = token };
            return result;
        }


        public static dynamic Read(string line)
        {
            if (String.IsNullOrEmpty(line))
            {
                FormatException fe = new FormatException();
                throw fe;
            }
            var tokens = tokenize(line);
            return readtokens(ref tokens);
        }

        public static dynamic Eval(dynamic x, Environment env)
        {
            if (x.IsSymbol())
            {
                return env.Resolve(x);
            }
            if (!x.IsCell())
            {
                return x;
            }
            dynamic car = x.Car();
            if (car.IsSymbol())
            {
                dynamic  evalCar = env.Resolve(car);
                if (evalCar.IsPrimitive())
                {
                    Primitive prim = evalCar.value;
                    System.Diagnostics.Debug.WriteLine("Calling {0} ", ((Symbol)car).name);
                    return prim(x.value.second, env);
                }
                else
                {
                    return Entity.makeCell(evalCar, Eval(x.Cdr(), env));
                }
            }
            return Entity.makeCell(Eval(car, env), Eval(x.Cdr(), env));
        }

        public static void Print(dynamic x)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            if (x.IsFixnum())
            {
                long v = (long)x.value;
                sw.Write(v);
            }
            if (x.IsFloatnum())
            {
                double v = (double)x.value;
                sw.Write(v);
            }
            if (x.IsBoolean())
            {
                bool v = (bool)x.value;
                sw.Write(v);
            }
            if (x.IsSymbol())
            {
                string v = (x.value as Symbol).name;
                sw.Write(v);
            }
            if (x.IsCell())
            {
                sw.Write("(");
                Print(x.Car().value);
                if (x.Cdr().IsCell())
                {
                    var current = x.value.Cdr();
                    while (!current.IsNil())
                    {
                        Print(current);
                        current = current.Cdr();
                    }
                }
                sw.Write(")");
            }
            System.Diagnostics.Debug.WriteLine("Inspector: {0} ", sw.ToString());
        }

        public static void Rep(string line)
        {
            var readResult = Read(line);
            var evalResult = Eval(readResult, root);
            Print(evalResult);
            return;
        }
    }
    
}
