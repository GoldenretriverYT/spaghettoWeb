using spaghetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace spaghettoWeb.classes {
    internal class Session {
        public static Random rnd = new();

        public static Class @class = new("Session", new() {


            { "get", new NativeFunction("get", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                if (!Program.sDb.TryGet((string)(args[0] as ClassInstance).hiddenValues["session"], (args[1] as StringValue).value, out string data))
                    throw new RuntimeError(posStart, posEnd, "Session variable not defined.", ctx);
                return new StringValue(data);
            }, new() { "self", "key" }, false) },



            { "set", new NativeFunction("set", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                if (!Program.sDb.TrySet((string)(args[0] as ClassInstance).hiddenValues["session"], (args[1] as StringValue).value, (args[2] as StringValue).value))
                    throw new RuntimeError(posStart, posEnd, "Session does not exist.", ctx);
                return new Number(0);
            }, new() { "self", "key", "value" }, false) }



        }, new() { }, new NativeFunction("ctor", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
            if (args[0] is not ClassInstance && (args[0] as ClassInstance).clazz.name == RequestClass.@class.name) throw new RuntimeError(posStart, posEnd, "Argument 1 must be of type Request.", ctx);
            if (args[1] is not ClassInstance && (args[1] as ClassInstance).clazz.name == ResponseClass.@class.name) throw new RuntimeError(posStart, posEnd, "Argument 2 must be of type Response.", ctx);
            
            var self = (ctx.symbolTable.Get("this") as ClassInstance);

            HttpListenerRequest req = (HttpListenerRequest)(args[0] as ClassInstance).hiddenValues["req"];
            HttpListenerResponse res = (HttpListenerResponse)(args[1] as ClassInstance).hiddenValues["res"];

            if (req.Cookies["spSession"] == null)
            {
                string s = GenerateSession();
                Cookie ck = new Cookie("spSession", s, "/");
                ck.Expires = DateTime.Now.AddDays(7);
                Console.WriteLine("Generating new session: " + s);
                res.SetCookie(ck);
                self.hiddenValues.Add("session", s);
                Program.sDb.SessionData.Add(s, new());
                Program.sDb.SaveSessionDB();
            }else
            {
                self.hiddenValues.Add("session", req.Cookies["spSession"]);
            }

            return ctx.symbolTable.Get("this");
        }, new() { "req", "res" }, true));


        public static string GenerateSession()
        {
            string chars = "abcdef0123456789";
            string o = "";
            for (int i = 0; i < 24; i++) o += chars[rnd.Next(chars.Length)];
            return o;
        }
    }
}
