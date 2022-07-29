using CSSpaghettoLibBase;
using spaghetto;
using spaghettoWeb.classes;
using System.Net;
using System.Text;
using System.Web;
using System.Security.Principal;
using Newtonsoft.Json;

namespace spaghettoWeb {
    internal class Program {
        public static HttpListener listener;
        public static string url = "http://*:8000/";
        public static string runLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static SessionDB sDb;
        public static Config cfg;

        static void Main(string[] args) {
            if(Environment.OSVersion.Platform == PlatformID.Win32NT) {
                if(!IsAdministrator()) {
                    url = "http://localhost:8000/";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($@"===============================================
ATTENTION: Your website will not be available
from outside your device/network, as the server
was not started with administrative privileges.
===============================================");
                    Console.ResetColor();
                }
            }

            if (!Directory.Exists("data")) Directory.CreateDirectory("data");
            if (!Directory.Exists("www")) Directory.CreateDirectory("www");


            if (!File.Exists("data/sessiondb.json"))
            {
                File.Create("data/sessiondb.json").Close();
                File.WriteAllText("data/sessiondb.json", "{}");
            }

            if (!File.Exists("data/config.cfg"))
            {
                File.Create("data/config.cfg").Close();
                File.WriteAllText("data/config.cfg", new ConfigReader("").CreateTemplate<Config>());
            }

            sDb = JsonConvert.DeserializeObject<SessionDB>(File.ReadAllText("data/sessiondb.json"));
            cfg = new ConfigReader(File.ReadAllText("data/config.cfg")).ReadInto<Config>();

            File.WriteAllText("data/config.cfg", new ConfigReader("").GenerateWithData<Config>(cfg));

            Console.WriteLine(cfg.UseHTMLTagsInsteadOfCustomPrefix);

            Console.WriteLine("Adding SpaghettoWeb methods");
            SpaghettoBridge bridge = new();
            bridge.Register("Request", RequestClass.@class);
            bridge.Register("Response", ResponseClass.@class);
            bridge.Register("Encryption", EncryptionClass.@class);
            bridge.Register("MySQL", MySQLClass.@class);
            bridge.Register("Session", Session.@class);

            bridge.Register("log", new NativeFunction("log", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                Console.WriteLine((args[0] as StringValue).value);
                return new Number(0);
            }, new() { "text" }, false));

            
            // Replace native prints
            Intepreter.globalSymbolTable.Remove("print");

            bridge.Register("print", new NativeFunction("print", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                ((Intepreter.globalSymbolTable.Get("res") as ClassInstance).hiddenValues["res"] as HttpListenerResponse).OutputStream.WriteAsync(args[0].ToString()).Wait();
                return new Number(0);
            }, new() { "text" }, false));

            Intepreter.globalSymbolTable.Remove("printLine");

            Intepreter.globalSymbolTable.Add("printLine", new NativeFunction("printLine", (List<Value> args, Position posStart, Position posEnd, Context ctx) => {
                ((Intepreter.globalSymbolTable.Get("res") as ClassInstance).hiddenValues["res"] as HttpListenerResponse).OutputStream.WriteAsync(args[0] + "\n").Wait();
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

        public static bool IsAdministrator() {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
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

                Position intPosStart = new Position(0, 0, 0, "internal", "Value defined from SpaghettoWeb and not from your code");
                Position intPosEnd = new Position(54, 0, 54, "internal", "Value defined from SpaghettoWeb and not from your code");

                List<string> suffixes = new() { "", ".html", ".spag", "index.html", "index.spag", "/index.html", "/index.spag" };

                foreach (string suffix in suffixes) {
                    string pathWithSuffix = filePath + suffix;

                    if (File.Exists(pathWithSuffix)) {
                        ClassInstance spagReq = new(RequestClass.@class, intPosStart, intPosEnd, new() { new Number(0) });
                        spagReq.instanceValues.Add("path", new StringValue(pathWithSuffix));
                        spagReq.instanceValues.Add("method", new StringValue(req.HttpMethod));
                        spagReq.instanceValues.Add("args", new DictionaryValue(new()));
                        spagReq.instanceValues.Add("body", new DictionaryValue(new()));
                        spagReq.instanceValues.Add("bodyRaw", new Number(0));


                        foreach (string s in req.QueryString) {
                            (spagReq.instanceValues.Get("args") as DictionaryValue).value.Add(new StringValue(s).SetPosition(intPosStart, intPosEnd), new StringValue(HttpUtility.UrlDecode(req.QueryString[s])));
                        }

                        if(req.HasEntityBody) {
                            string text;
                            
                            using (var reader = new StreamReader(req.InputStream,
                                                                 req.ContentEncoding)) {
                                text = reader.ReadToEnd();
                            }

                            spagReq.instanceValues.Set("bodyRaw", new StringValue(text));

                            if(req.ContentType == "application/x-www-form-urlencoded") {
                                string[] parts = text.Split("&");

                                foreach(string part in parts) {
                                    string[] urlEncodedData = part.Split("=");
                                    if (urlEncodedData.Length == 0) continue;
                                    if (urlEncodedData.Length == 1) (spagReq.instanceValues.Get("body") as DictionaryValue).value.Add(new StringValue(urlEncodedData[0]).SetPosition(intPosStart, intPosEnd), new Number(0));
                                    if (urlEncodedData.Length == 2) (spagReq.instanceValues.Get("body") as DictionaryValue).value.Add(new StringValue(urlEncodedData[0]).SetPosition(intPosStart, intPosEnd), new StringValue(HttpUtility.UrlDecode(urlEncodedData[1])));
                                }
                            }
                        }

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
                                    await resp.OutputStream.WriteAsync(line + "\n");
                                }
                            }else {
                                if(line.Trim() == "<)") {
                                    readingSpaghetto = false;

                                    Console.WriteLine("Running spaghetto: " + spaghetto);
                                    (RuntimeResult res, SpaghettoException err) = Intepreter.Run("<spaghettoweb>", spaghetto);

                                    if (err != null) {
                                        await resp.OutputStream.WriteAsync(GenerateErrorPage("500 - Internal Server Error", "Internal server error occurred."));
                                        Console.WriteLine(err.Message);
                                    }
                                }else {
                                    spaghetto += line + "\n";
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

        public static TValue GetOrNull<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) {
            if (dict.ContainsKey(key)) return dict[key];
            return default(TValue);
        }


        public static string ReplaceFirst(this string text, string search, string replace) {
            int pos = text.IndexOf(search);
            if (pos < 0) {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}