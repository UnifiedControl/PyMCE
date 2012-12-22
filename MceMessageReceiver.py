#
# Copyright (C) 2005 Lars-Peter Voss
#
# This file is part of EventGhost.
#
# EventGhost is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# EventGhost is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with EventGhost; if not, write to the Free Software
# Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
#
# NOTE: File has been modified by http://github.com/fuzeman

from struct import unpack_from
import win32file
import win32event
import time
import win32api


class MceMessageReceiver(object):
    """
    Connect to AlternateMceIrService in a new threading.Thread.  This class is callable, so can be assigned to a thread.
    """

    def __init__(self, callback, finishedEvent, ptr_fmt="i", ptr_len=4, debug=False):
        """
        This initializes the class, and saves the plugin reference for use in the new thread.
        """
        self.callback = callback
        self.finishedEvent = finishedEvent

        self.ptr_fmt = ptr_fmt
        self.ptr_len = ptr_len
        self.debug = debug

        self.file = None

    def _log(self, message):
        if self.debug:
            print message

    def __call__(self):
        """
        This makes the class callable, and is the entry point for starting the processing in a new thread.
        """
        self.keepRunning = True
        self.learn_callback = None
        self.learnTimeout = 250  # 250ms
        self._log("MCE_Vista: thread started")
        while self.keepRunning:
            self.Connect()
            if self.keepRunning:
                self.HandleData()

    def Stop(self):
        """
        This will be called to stop the thread.
        """
        if self.file:
            writeOvlap = win32file.OVERLAPPED()
            writeOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
            msg = "q".encode("ascii")
            win32file.WriteFile(self.file, msg, writeOvlap)
            win32file.CloseHandle(self.file)
            self.file = None
        self.keepRunning = False
        self._log("MCE_Vista: stopping thread")

    def Transmit(self, transmitData):
        """
        This will be called to detect available IR Blasters.
        """
        if not self.file:
            return False
        writeOvlap = win32file.OVERLAPPED()
        writeOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
        win32file.WriteFile(self.file, transmitData, writeOvlap)
        win32event.WaitForSingleObject(writeOvlap.hEvent, win32event.INFINITE)
        return True

    def LearnIR(self):
        raise NotImplementedError()

    def GetDeviceInfo(self):
        """
        This will be called to detect IR device info.
        """
        if not self.file:
            return None
        writeOvlap = win32file.OVERLAPPED()
        writeOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
        self.deviceInfoEvent = win32event.CreateEvent(None, 0, 0, None)
        win32file.WriteFile(self.file, "b".encode("ascii"), writeOvlap)
        if win32event.WaitForSingleObject(self.deviceInfoEvent, 250) == win32event.WAIT_OBJECT_0:
            return self.deviceInfo
        return None

    def TestIR(self):
        """
        This will be called to Transmit a known signal to verify blaster capability.
        """
        if not self.file:
            return None
        writeOvlap = win32file.OVERLAPPED()
        writeOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
        self.deviceTestEvent = win32event.CreateEvent(None, 0, 0, None)
        win32file.WriteFile(self.file, "t".encode("ascii"), writeOvlap)
        if win32event.WaitForSingleObject(self.deviceTestEvent, 250) == win32event.WAIT_OBJECT_0:
            return None
        return None

    def ChangeReceiveMode(self, mode):
        """
        This will be called to detect available IR Blasters.
        """
        if not (mode == "l" or mode == "n"):
            return False  # needs to be normal or learn
        if not self.file:
            return False
        writeOvlap = win32file.OVERLAPPED()
        writeOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
        win32file.WriteFile(self.file, mode, writeOvlap)
        win32event.WaitForSingleObject(writeOvlap.hEvent, win32event.INFINITE)
        return True

    def Connect(self):
        """
        This function tries to connect to the named pipe from AlternateMceIrService.
        If it can't connect, it will periodically retry until the plugin is stopped or the connection is made.
        """
        self._log("MCE_Vista: Connect started")
        self.sentMessageOnce = False
        while self.file is None and self.keepRunning:
            try:
                self.file = win32file.CreateFile(r'\\.\pipe\MceIr', win32file.GENERIC_READ
                                                                    | win32file.GENERIC_WRITE, 0, None,
                                                 win32file.OPEN_EXISTING, win32file.FILE_ATTRIBUTE_NORMAL
                                                                          | win32file.FILE_FLAG_OVERLAPPED, None)
            except Exception, ex:
                if not self.sentMessageOnce:
                    self._log("MCE_Vista: MceIr pipe is not available, app doesn't seem to be running")
                    self._log("    Will continue to try to connect to MceIr")
                    self._log("    Message = %s" % win32api.FormatMessage(win32api.GetLastError()))
                    self._log(ex)
                    self.sentMessageOnce = True
                time.sleep(.25)
        return

    def HandleData(self):
        """
        This runs once a connection to the named pipe is made.
        It receives the ir data and passes it to the plugins IRDecoder.
        """
        if self.sentMessageOnce:
            self._log("MCE_Vista: Connected to MceIr pipe, started handling IR events")
            pass
        nMax = 2048
        self.result = []
        self.freqs = [0]
        self.readOvlap = win32file.OVERLAPPED()
        self.readOvlap.hEvent = win32event.CreateEvent(None, 0, 0, None)
        handles = [self.finishedEvent, self.readOvlap.hEvent]
        self.timeout = win32event.INFINITE
        while self.keepRunning:
            try:
                (hr, data) = win32file.ReadFile(self.file, nMax, self.readOvlap)
            except:
                win32file.CloseHandle(self.file)
                self.file = None
                return
            rc = win32event.WaitForMultipleObjects(handles, False, self.timeout)
            if rc == win32event.WAIT_OBJECT_0:  # Finished event
                self.keepRunning = False
                break
            elif rc == win32event.WAIT_TIMEOUT:  # Learn timeout
                self._log("LearnTimeout: Sending ir code %s"%str(self.result))
                self.learn_callback(self.freqs, self.result)
                self.result = []
                self.timeout = win32event.INFINITE
                rc = win32event.WaitForMultipleObjects(handles, False, self.timeout)
                if rc == win32event.WAIT_OBJECT_0:  # Finished event
                    self.keepRunning = False
                    break
            try:
                nGot = self.readOvlap.InternalHigh
                if nGot == 0:
                    continue
                if nGot % self.ptr_len == 1:  # Query result, not ir code data
                    if data[0] == "b".encode("ascii"):
                        self.deviceInfo = unpack_from(6 * self.ptr_fmt, data[1:nGot])
                        win32event.SetEvent(self.deviceInfoEvent)
                    elif data[0] == "t".encode("ascii"):
                        win32event.SetEvent(self.deviceTestEvent)
                    continue
                    #pull of the header data
                while nGot > 0:
                    header = unpack_from(3 * self.ptr_fmt, data)
                    if header[0] == 1 and header[2] > 0:
                        self.freqs.append(header[2])
                    dataEnd = nGot
                    if nGot > 100 + 3 * self.ptr_len:
                        dataEnd = 100 + 3 * self.ptr_len
                    nGot -= dataEnd
                    val_data = data[3 * self.ptr_len:dataEnd]
                    dataEnd -= 3 * self.ptr_len
                    vals = unpack_from((dataEnd / 4) * "i", val_data)
                    data = data[100 + 3 * self.ptr_len:]
                    for i, v in enumerate(vals):
                        a = abs(v)
                        self.result.append(a)
                        if self.learn_callback is None:  # normal mode
                            if a > 6500:  # button held?
                                if self.CodeValid(self.result):
                                    self._log("Sending ir code %s" % str(self.result))
                                    self.callback(self.result)
                                self.result = []
                    if not self.learn_callback is None:  # learn mode
                        if header[0] == 1:  # one "learn" chunk
                            self.timeout = self.learnTimeout
            except:
                pass
                self._log("MCE_Vista: Handle Data finished")

    def CodeValid(self, code):
        """
        Used to sanity check a code so we don't waste cycles testing it in all the decoders.
        Put any validation of codes (before sending them to IrDecode) in here.
        """
        if len(code) < 5:
            return False
        return True