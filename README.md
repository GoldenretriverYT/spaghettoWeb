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

### How to get body data/query args
Currently, there is no way to get query params by name. As stupid as it sounds, there is no Dictionary/DataCollection type in spaghetto. I will add it soon, and then I will add proper query params support. Currently, you will get those in order in the following list: `req.args`. Also, there is no way at all to read any body data. This will be added very soon aswell. Remember that this server is in very early development stage.

# Quick example page
```
<!DOCTYPE html>
<head>
	<title>Welcome</title>
</head>
<body>
	(>s
		print("<h1>Welcome to SpaghettoWeb!</h1>");
		print("<address>Random value: " + Math.random().toString() + "</address>");
		print(req.path);
		
		if(req.args.size() > 0) {
			print("<br>The first argument passed in is: " + req.args#0);
		}
	<)
</body>
```
