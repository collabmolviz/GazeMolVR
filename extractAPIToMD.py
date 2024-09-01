#Meant to be run from the main UnityMol folder

def getVersion():
	fmain = open("Assets/Scripts/UnityMolMain.cs")
	linesMain = fmain.readlines()
	fmain.close()

	for i in linesMain:
		if "string version =" in i:
			return i.split("string version = ")[1].replace(";", "").replace('"', "")
	return "????"


f = open("Assets/Scripts/API/APIPython.cs")
lines = f.readlines()
f.close()

curComment = ""
proto = ""

print("# UnityMol ",getVersion())

for l in lines:
	if l.strip().startswith("///") and not "summary>" in l:
		newL = l.replace("///", "#")
		curComment += newL.strip()+"\n"
	if l.strip().startswith("public static") and "(" in l and ")" in l:
		proto = l.strip().replace("public static ", "").replace("{", "").replace("void ", "").replace("true", "True").replace("false", "False")
		print("```python\n"+curComment+"\n"+proto+"\n```")
		curComment = ""
	if l.strip().startswith("public static") and not "(" in l:
		curComment = ""
		