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
	<)
</body>
```
