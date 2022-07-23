using spaghetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace spaghettoWeb.classes {
    internal class EncryptionClass {
        public static Class @class = new("Encryption", new() { }, new() {
            { "hash", new NativeFunction("hashPassword", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                return new StringValue(BC.HashPassword((args[0] as StringValue).value));
            }, new() { "text" }, true) },
            { "compare", new NativeFunction("compare", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                return new Number(BC.Verify((args[0] as StringValue).value, (args[1] as StringValue).value) ? 1 : 0);
            }, new() { "text", "hash" }, true) }
        }, new NativeFunction("ctor", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
            return ctx.symbolTable.Get("this");
        }, new() { "" }, true));
    }
}
