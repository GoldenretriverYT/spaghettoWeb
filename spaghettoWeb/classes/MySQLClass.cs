using spaghetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace spaghettoWeb.classes {
    internal class MySQLClass {

        public static Class @class = new("MySQL", new() {
            { "connect", new NativeFunction("connect", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                try {
                    var self = (args[0] as ClassInstance);
                    string conStr = "Server=" + args[1] + ";User=" + args[2] + ";Password=" + args[3] + ";Database=" + args[4];
                    Console.WriteLine("Attempt connect to " + conStr);
                    self.hiddenValues.Add("sqlConnection", new MySqlConnection(conStr));
                    (self.hiddenValues.GetOrNull("sqlConnection") as MySqlConnection).Open();

                    return new Number(0);
                }catch(Exception ex) {
                    throw new RuntimeError(posStart, posEnd, "Failed to connect: " + ex.Message, ctx);
                }
            }, new() { "self", "server", "username", "password", "database" }, false) },
            { "query", new NativeFunction("query", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                var self = (args[0] as ClassInstance);
                var con = (self.hiddenValues.GetOrNull("sqlConnection") as MySqlConnection);
                var sql = (args[1] as StringValue).value;
                var prms = (args[2] as ListValue).value;

                if(con.State != System.Data.ConnectionState.Open) {
                    throw new RuntimeError(posStart, posEnd, "Connect MySQL instance first!", ctx);
                }

                int idx = 0;
                while(sql.IndexOf('?') != -1) {
                    sql = sql.ReplaceFirst("?", "@" + idx);
                    idx++;
                }

                Console.WriteLine("Perform " + sql);

                MySqlCommand cmd = new(sql, con);

                idx = 0;
                foreach(Value val in prms) {
                    cmd.Parameters.AddWithValue("@" + idx, val);
                    Console.WriteLine("Replacing @" + idx + " with " + val);
                    idx++;
                }

                cmd.Prepare();
                using var reader = cmd.ExecuteReader();
                var list = new ListValue(new() {});

                while(reader.Read()) {
                    var dict = new DictionaryValue(new(){});

                    for(int i = 0; i < reader.FieldCount; i++) {
                        dict.value.Add(new StringValue(reader.GetName(i)), (reader[i] is double || reader[i] is int || reader[i] is float || reader[i] is short || reader[i] is byte) ? new Number(reader[i]) : new StringValue(reader[i].ToString()));
                    }

                    list.value.Add(dict);
                }
                
                return list;
            }, new() { "self", "sql", "params" }, false) },
        }, new() {
            
        }, new NativeFunction("ctor", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
            return ctx.symbolTable.Get("this");
        }, new() { }, true));
    }
}
