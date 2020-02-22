import yaml
import sys
import pprint

print(yaml)

def check_file(fname):
    docs = yaml.load_all(open(fname), Loader = yaml.FullLoader)
    
    for doc in docs:

        pprint.pprint(doc)



check_file(sys.argv[1])