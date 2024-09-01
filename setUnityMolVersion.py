

newVersion = "1.1.4"


def setUMolMain(newv):
    fmain = open("Assets/Scripts/UnityMolMain.cs")
    linesMain = fmain.readlines()
    fmain.close()

    for i in range(len(linesMain)):
        if "string version =" in linesMain[i]:
            s = linesMain[i].split("string version =")
            linesMain[i] = s[0] + "string version = \""+newv+"\";\n"

    fmainw = open("Assets/Scripts/UnityMolMain.cs", "w")
    for i in linesMain:
        fmainw.write(i)

    fmainw.close()

def setProjSettings(newv):
    fset = open("ProjectSettings/ProjectSettings.asset");
    linesSettings = fset.readlines()
    fset.close()

    for i in range(len(linesSettings)):
        if "productName:" in linesSettings[i]:
            s = linesSettings[i].split("productName:")
            linesSettings[i] = s[0] + "productName: UnityMol_"+newv+"\n"
        if "bundleVersion:" in linesSettings[i]:
            s = linesSettings[i].split("bundleVersion:")
            linesSettings[i] = s[0] + "bundleVersion: "+newv+"\n"

    fsetw = open("ProjectSettings/ProjectSettings.asset", "w")
    for i in linesSettings:
        fsetw.write(i)

    fsetw.close()

setUMolMain(newVersion)
setProjSettings(newVersion)
