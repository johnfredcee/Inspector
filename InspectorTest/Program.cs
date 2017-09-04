using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inspector;

namespace InspectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Interpreter.Rep("(number? 42)");
            Interpreter.Rep("(number? 4.2)");
            Interpreter.Rep("(list? quote (1 2 3)");
            Interpreter.Rep("(symbol? quote hello)");
        }
    }
}
