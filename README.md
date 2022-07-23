# spaghettoWeb
SpaghettoWeb is a slow ass webserver using the spaghetto programming language

# How to use?
Download/build SpaghettoWeb. Run "spaghettoWeb.exe". To put content on the ✨ world wide web ✨, create a new folder called "www" in the same path the executable is in. Then, create a file. For it to load on the / route, you will have to call it `index.html` or `index.spag`.

To then use spaghetto inside your website, use this syntax:
```
(>s
  // your epic spaghetto code here, everything printed by the print function will be inserted here.
<)
```

### Why do my post arguments not appear in req.body?
As of now, only simple url-encoded bodies are supported. If you want, you could manually parse the req.bodyRaw property.

# Quick example page
```
<!DOCTYPE html>
<head>
    <title>Welcome</title>
</head>
<body>
    <h3>Get Form</h3>
    <form method="get" action="">
        <input name="monke" placeholder="Your epic form value"/>
        <input type="submit">
    </form>
    
    <h3>Post Form</h3>
    <form method="post" action="">
        <input name="hello" placeholder="Your epic form value"/>
        <input type="submit">
    </form>
    
    (>s
        print("<h1>Welcome to SpaghettoWeb!</h1>");
        print("<address>Random value: " + Math.random().toString() + "</address>");
        print(req.path);
        
        if(req.args.hasKey("monke")) {
            print("<br>The get form value passed in is: " + req.args#"monke");
        }
        
        if(req.body.hasKey("hello")) {
            print("<br>The post form value passed in is: " + req.body#"hello");
        }
    <)
</body>
```
