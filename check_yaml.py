import yaml
import sys
import pprint

def check_file(fname):
    docs = yaml.load_all(open(fname), Loader = yaml.FullLoader)
    

    typecount = 0
    count = 0
    for doc in docs:
        count += 1

        if doc is None:
            print("Skip bad document")
            continue
        typecount += len(doc)
        print(count, "dlls", typecount, "types")

check_file(sys.argv[1])