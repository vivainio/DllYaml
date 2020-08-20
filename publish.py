from __future__ import print_function

import os,shutil

version = "1.4.0"
def c(s):
    print(">",s)
    err = os.system(s)
    assert not err

shutil.rmtree("bin")
shutil.rmtree("obj")

c("dotnet pack /p:Version=%s" % version)