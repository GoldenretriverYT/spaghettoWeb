using Microsoft.AspNetCore.Components.Web;
using spaghetto;
using System.Diagnostics;

namespace spaghettoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if(!Directory.Exists("www")) {
                Directory.CreateDirectory("www");
            }
            
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            
            app.MapGet("/{*path}", Run);

            app.UseAuthorization();

            app.Run();
        }

        public static string Run(HttpContext context, string path = "")
        {
            // setup basic response
            context.Response.ContentType = "text/html"; // this can be overrriden later on by the spaghetto code

            string html = "";

            if (File.Exists("www/" + path + ".spagw")) {
                Console.WriteLine("spagw!");
                html = File.ReadAllText("www/" + path + ".spagw");

                // spagw files are spaghettoWeb files. The syntax is as follows:
                // <% CONTROL FLOW %>
                // <%= ESCAPED OUTPUT %>
                // <%- NON-ESCAPED OUTPUT %>
                // Everything outside of those tags are raw html content.

                // Parse spagw
                var nodes = Parser.Parse(html);
                var spagToRun = "";
                var output = "";
                InterpreterResult res = new();

                foreach (var node in nodes) spagToRun += node.GenerateSpaghetto();

                // Define globals
                // (yes, spaghetto usually doesnt do globals defined by default except true/false/null, but
                // we also dont want people to have to import the web library everytime or something like that)
                Interpreter interpreter = new();
                spaghetto.Stdlib.Lang.Lib.Mount(interpreter.GlobalScope);
                spaghetto.Stdlib.IO.Lib.Mount(interpreter.GlobalScope);
                spaghetto.Stdlib.Interop.Lib.Mount(interpreter.GlobalScope);

                interpreter.GlobalScope.Set("send",
                     new SNativeFunction(
                         impl: (Scope scope, List<SValue> args) =>
                         {
                            Debug.WriteLine("Send called!");
                            output += args[0].ToSpagString().Value;
                            return SInt.One;
                         },
                         expectedArgs: new List<string>() { "msg" }));

                Debug.WriteLine(spagToRun);

                try {
                    interpreter.Interpret(spagToRun, ref res);

                    return output;
                } catch (Exception e) {
                    return "Fatal error in spaghetto runtime: " + e.Message;
                }
            } else if (File.Exists("www/" + path)) {
                html = File.ReadAllText("www/" + path);

                // this is not a spagw file, send immediately
                return html;
            } else {
                return "temp 404 page";
            }
        }
    }

    class SpagWNode
    {
        public enum NodeType
        {
            Text,
            SpagCodeOutput,
            SpagCodeControlFlow
        }
        
        public List<SpagWNode> Children;
        public NodeType Type;
        public string Text;
        public bool Escaped = false;
        
        public string GenerateSpaghetto()
        {
            switch(Type) {
                case NodeType.Text:
                    Text = Text.Replace("\"", "\\\"");
                    Text = Text.Replace("\r\n", "\\n\" +\"");
                    Text = Text.Replace("\n", "\\n\" +\"");

                    return "send(\"" + Text + "\");";
                case NodeType.SpagCodeOutput:
                    if (Escaped) {
                        return "send(escape(" + Text + "));";
                    } else {
                        return "send(" + Text + ");";
                    }
                case NodeType.SpagCodeControlFlow:
                    return Text;
            }

            return "-1";
        }
    }

    class Parser
    {
        public static List<SpagWNode> Parse(string input)
        {
            var nodes = new List<SpagWNode>();
            while (input.Length > 0) {
                if (input.StartsWith("<%-")) {
                    // Non-escaped code output
                    var idx = input.IndexOf("%>");
                    if (idx == -1) throw new Exception("Expected %> after <%-");
                    nodes.Add(new SpagWNode() { Type = SpagWNode.NodeType.SpagCodeOutput, Text = input.Substring(3, idx - 3) });
                    input = input.Substring(idx + 2);
                } else if (input.StartsWith("<%=")) {
                    // Escaped code output
                    var idx = input.IndexOf("%>");
                    if (idx == -1) throw new Exception("Expected %> after <%= ");
                    nodes.Add(new SpagWNode() { Type = SpagWNode.NodeType.SpagCodeOutput, Text = input.Substring(3, idx - 3), Escaped = true });
                    input = input.Substring(idx + 2);
                }else if (input.StartsWith("<%")) {
                    // SpagCode
                    var idx = input.IndexOf("%>");
                    if (idx == -1) throw new Exception("Expected %> after <%");
                    nodes.Add(new SpagWNode() { Type = SpagWNode.NodeType.SpagCodeControlFlow, Text = input.Substring(2, idx - 2) });
                    input = input.Substring(idx + 2);
                }  else {
                    // Text
                    var idx = input.IndexOf("<%");
                    if (idx == -1) idx = input.Length;
                    nodes.Add(new SpagWNode() { Type = SpagWNode.NodeType.Text, Text = input.Substring(0, idx) });
                    input = input.Substring(idx);
                }
            }
            return nodes;
        }
    }
}