using CSSpaghettoLibBase;
using spaghetto;
using spaghettoWeb.classes;
using System.Net;
using System.Text;

namespace spaghettoWeb {
    internal class Program {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static string runLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static void Main(string[] args) {
            Console.WriteLine("Adding SpaghettoWeb methods");
            SpaghettoBridge bridge = new();
            bridge.Register("Request", RequestClass.@class);
            bridge.Register("Response", ResponseClass.@class);

            bridge.Register("log", new NativeFunction("log", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                Console.WriteLine((args[0] as StringValue).value);
                return new Number(0);
            }, new() { "text" }, false));

            
            // Replace native prints
            Intepreter.globalSymbolTable.Remove("print");

            bridge.Register("print", new NativeFunction("print", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                ((Intepreter.globalSymbolTable.Get("res") as ClassInstance).hiddenValues["res"] as HttpListenerResponse).OutputStream.WriteAsync((args[0] as StringValue).value).Wait();
                return new Number(0);
            }, new() { "text" }, false));

            Intepreter.globalSymbolTable.Remove("printLine");

            Intepreter.globalSymbolTable.Add("printLine", new NativeFunction("printLine", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                Console.WriteLine("[Warn] Using print line in SpaghettoWeb is not recommend. IT WONT INSERT A NEW LINE!");

                ((Intepreter.globalSymbolTable.Get("res") as ClassInstance).hiddenValues["res"] as HttpListenerResponse).OutputStream.WriteAsync((args[0] as StringValue).value).Wait();
                return new Number(0);
            }, new() { "text" }, false));

            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }

        public static async Task HandleIncomingConnections() {
            bool runServer = true;

            while (runServer) {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                string filePath = System.IO.Path.Combine("www", req.Url.AbsolutePath.Substring(1));
                Console.WriteLine("Reading " + filePath);

                List<string> suffixes = new() { "", ".html", ".spag", "index.html", "index.spag", "/index.html", "/index.spag" };

                foreach (string suffix in suffixes) {
                    string pathWithSuffix = filePath + suffix;

                    if (File.Exists(pathWithSuffix)) {
                        ClassInstance spagReq = new(RequestClass.@class, new Position(0, 0, 0, "internal", "internal"), new Position(0, 0, 0, "internal", "internal"), new() { new Number(0) });
                        spagReq.hiddenValues.Add("req", req);

                        ClassInstance spagRes = new(ResponseClass.@class, new Position(0, 0, 0, "internal", "internal"), new Position(0, 0, 0, "internal", "internal"),new() { new Number(0) });
                        spagRes.hiddenValues.Add("res", resp);

                        Intepreter.globalSymbolTable.Add("req", spagReq);
                        Intepreter.globalSymbolTable.Add("res", spagRes);

                        string[] lines = File.ReadAllLines(pathWithSuffix);
                        bool readingSpaghetto = false;
                        string spaghetto = "";

                        foreach(string line in lines) {
                            if (!readingSpaghetto) {
                                if (line.Trim() == "(>s") {
                                    readingSpaghetto = true;
                                    spaghetto = "";
                                }else {
                                    await resp.OutputStream.WriteAsync(line);
                                }
                            }else {
                                if(line.Trim() == "<)") {
                                    readingSpaghetto = false;

                                    Console.WriteLine("Running spaghetto: " + spaghetto);
                                    (RuntimeResult res, SpaghettoException err) = Intepreter.Run("<spaghettoweb>", spaghetto);

                                    if (err != null) {
                                        resp.OutputStream.WriteAsync(GenerateErrorPage("500 - Internal Server Error", "Internal server error occurred."));
                                        Console.WriteLine(err.Message);
                                    }
                                }else {
                                    spaghetto += line;
                                }
                            }
                        }
                        
                        resp.Close();

                        Intepreter.globalSymbolTable.Remove("req");
                        Intepreter.globalSymbolTable.Remove("res");
                        goto finishRequest;
                    }
                }

                await resp.OutputStream.WriteAsync(GenerateErrorPage("404 - Not found", "Specified file not found."));
                resp.Close();

                finishRequest:
                continue;
            }
        }

        public static string GenerateErrorPage(string errorTitle, string errorDescription) {
            return $@"<!DOCTYPE html>
<head>
    <title>{errorTitle}</title>
</head>
<body>
<h1>{errorTitle}</h1>
<h4>{errorDescription}</h4>
<hr>
<address>SpaghettoWeb {GetAppVersion()} on {Environment.OSVersion.Platform} {Environment.OSVersion.Version}</address>
</body>";
        }

        public static string GetAppVersion() {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }

    public static class ExtensionMethods {
        public static byte[] GetBytes(this string str) {
            return Encoding.UTF8.GetBytes(str);
        }

        public static Task WriteAsync(this Stream stream, string str) {
            return stream.WriteAsync(str.GetBytes(), 0, str.Length);
        }
    }
}