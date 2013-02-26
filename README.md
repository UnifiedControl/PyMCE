PyMCE
=====
**(Windows Only *for now*)**

Python MCE IR Receiver Library

**Note:** The PyMCE_Core C# library (which is utilized in the PyMCE Windows Service)  was built off
the "Microsoft MCE Transceiver" plugin source code from the IR-Server-Suite project at
http://github.com/MediaPortal/IR-Server-Suite


PyMCE/python
------------------
**Work on the python package will start after the windows PyMCE service is complete.**

PyMCE/service
------------------
*PyMCE Windows Service*

###PyMCE_Console    
Simple console application for debugging

###PyMCE_Core    
Core library for connecting to the MCE Receiver    
*Built from the IRSS "Microsoft MCE Transceiver" plugin*

####Compatibility
**Windows 8** Works

**Windows 7** Works

**Windows Vista** Should work / Untested

**Windows XP** Receives data (need to check validity), Learning fails

**Replacement Driver** Should work / Untested



###PyMCE_Debug   
WPF application for debugging local or service based PyMCE clients

###PyMCE_Service    
Service to easily provide MCE Receiver communication to other applications

###PyMCE_Tests    
Unit tests for the PyMCE Core library
