PyMCE
=====
**(Windows Only *for now*)**

Python MCE IR Receiver Library

**Note:** We have not started on the python package yet, Work will be started when the Windows PyMCE service
is complete.

**Note:** The PyMCE_Core C# library (which is utilized in the PyMCE Windows Service)  was built off
the "Microsoft MCE Transceiver" plugin source code from the IR-Server-Suite project at
http://github.com/MediaPortal/IR-Server-Suite


PyMCE/service
------------------
*PyMCE Windows Service*

**PyMCE_Console**    
Simple console application for debugging

**PyMCE_Core**    
Core library for connecting to the MCE Receiver    
*Built from the IRSS "Microsoft MCE Transceiver" plugin*

**PyMCE_Debug**   
WPF application for debugging local or service based PyMCE clients

**PyMCE_Service**    
Service to easily provide MCE Receiver communication with other applications

**PyMCE_Tests**    
Unit tests for the PyMCE Core library
