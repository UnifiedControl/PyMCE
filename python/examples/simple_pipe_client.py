fp = open(r'\\.\pipe\PyMCE', 'r+b', 0)

fp.write('HELLO')

fp.close()