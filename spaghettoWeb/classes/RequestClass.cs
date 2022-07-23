using spaghetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spaghettoWeb.classes {
    internal class RequestClass {
        public static Class @class = new("Request", new() { }, new() { }, new NativeFunction("ctor", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
            return ctx.symbolTable.Get("this");
        }, new() { "" }, true));
    }
}
